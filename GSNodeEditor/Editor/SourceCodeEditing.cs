// Copyright Gradientspace Corp. All Rights Reserved.
using Gradientspace.NodeGraph;
using Gradientspace.UI;
using System.Diagnostics;

namespace GSNodeEditor
{
    public interface ISourceCodeProvider
    {
        SourceCodeDataType GetCurrentSourceCode();
        void UpdateSourceCode(SourceCodeDataType NewSourceCode);
        string GetCodeUINameHint();
    }


    public class SourceCodeEditSession : IDisposable
    {
        public WeakReference<ISourceCodeProvider> ProviderRef;

        public string useTempFileName = "";
        public FileSystemWatcher? ActiveFileWatcher = null;
        public Process? ActiveEditorProcess = null;

        public delegate void ModifiedEventHandler(SourceCodeEditSession sender);
        public event ModifiedEventHandler? OnSourceCodeModified;

        public SourceCodeEditSession(ISourceCodeProvider provider)
        {
            ProviderRef = new WeakReference<ISourceCodeProvider>(provider);
        }



        bool write_current_code(string path, SourceCodeDataType code)
        {
            try {
				if (File.Exists(path))
					File.Delete(path);
				File.WriteAllText(path, code.CodeText);
                return true;
			} catch { return false; }
		}


        static Random Generator = new Random( (int)(DateTime.Now.Ticks & 0xFFFFFFFF) );

        static string find_unique_random_file(string folder, string basename, string suffix)
        {
            bool done = false;
            while (!done)
            {
                int num = Generator.Next() % 99999;
                string path = Path.Combine(folder, basename + "_" + num.ToString() + suffix);
                if (File.Exists(path))
                    continue;
                try {
					File.OpenHandle(path, FileMode.CreateNew, FileAccess.Write).Dispose();
				} catch {
                    continue;
                }
				done = true;
				return path;
            }
            return "";
        }



        public void TryLaunchTextEditor()
        {
            // todo probably need to (what?)
            if (ProviderRef.TryGetTarget(out var Provider) == false) {
                SourceCodeEditingSystem.Instance.TerminateSession(this);
                return;
            }

            SourceCodeDataType CurrentCode = Provider.GetCurrentSourceCode();
			string UseSuffix = CurrentCode.DefaultFileSuffix;
			string CodeName = Provider.GetCodeUINameHint();

            // try to make make optional custom subdirectory for code nodes if it doesn't exist.
            string TempPath = Path.GetTempPath();
            string CodeNodesPath = Path.Combine(TempPath, "codenodes");
            try {
                if (Directory.Exists(CodeNodesPath) == false)
                    Directory.CreateDirectory(CodeNodesPath);
            } catch { }

            if (Directory.Exists(CodeNodesPath) == false)
                CodeNodesPath = Path.GetTempPath();

			if (useTempFileName == "")
            {
                string usePath = find_unique_random_file(CodeNodesPath, CodeName, UseSuffix);
                if ( write_current_code(usePath, CurrentCode) )
                {
                    useTempFileName = usePath;
                } 
                else
                {
                    // if that failed, fall back to a guaranteed-unique generated temp file  (could do an intermediate attempt in CodeNodesPath?)
					string generatedPath = Path.GetTempFileName();
					string basePath = Path.GetDirectoryName(generatedPath) ?? Path.GetTempPath();
					string nicepath = Path.Combine(basePath, CodeName + "_" + Path.GetFileNameWithoutExtension(generatedPath) + UseSuffix);
                    if (write_current_code(nicepath, CurrentCode))
                        useTempFileName = nicepath;
                    else
						throw new Exception($"SourceCodeEditSession.TryLaunchTextEditor - could not find writable temp file/path");
				}
            } else
            {
                if (!write_current_code(useTempFileName, CurrentCode))
                    throw new Exception($"SourceCodeEditSession.TryLaunchTextEditor - error writing to temp file {useTempFileName}");
            }

            // start file watcher for the temp file if one does not exist
			if (ActiveFileWatcher == null)
			{
				string WatchFolder = Path.GetDirectoryName(useTempFileName)!;
				ActiveFileWatcher = new FileSystemWatcher(WatchFolder, "*" + UseSuffix);
				ActiveFileWatcher.EnableRaisingEvents = true;
				ActiveFileWatcher.Changed += ActiveFileWatcher_Changed;
			}

			// if process has exited, we are going to need to launch a new editor
			if (ActiveEditorProcess != null && ActiveEditorProcess.HasExited)
                ActiveEditorProcess = null;

            // if editor is active, try to foreground it
            if (ActiveEditorProcess != null) {
                BringProcessToFront(ActiveEditorProcess);
                return;
            }

            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = SourceCodeEditingSystem.GetCodeEditorPath();
            startInfo.Arguments = useTempFileName;
            //startInfo.UseShellExecute = false;
            try {
                ActiveEditorProcess = Process.Start(startInfo);
            } catch (Exception ex) {
                GlobalGraphOutput.AppendError($"Failed to start Code Editor {startInfo.FileName} : {ex.Message}");
            }
            
        }


        public void Dispose()
        {
            Shutdown();
        }

        public void Shutdown()
        {
            if (ActiveEditorProcess != null)
            {
                if (ActiveEditorProcess.HasExited == false)
                    ActiveEditorProcess.Kill();
                ActiveEditorProcess.Dispose();
                ActiveEditorProcess = null;
            }

            if (ActiveFileWatcher != null) {
                ActiveFileWatcher.Changed -= ActiveFileWatcher_Changed;
                ActiveFileWatcher.Dispose();
                ActiveFileWatcher = null;
            }

            if (File.Exists(useTempFileName)) { 
                File.Delete(useTempFileName);
            }
        }


        // File.ReadAllText tries to exclusive-lock the file, which won't work with some text editors like VSCode
        static string SharedReadAllText(string file)
        {
            using var fileStream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var textReader = new StreamReader(fileStream);
            return textReader.ReadToEnd();
        }

        private void ActiveFileWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            if (ProviderRef.TryGetTarget(out var Provider) == false) {
                SourceCodeEditingSystem.Instance.TerminateSession(this);
                return;
            }

            if (Path.Exists(useTempFileName) && e.FullPath == useTempFileName)
            {
                int iters = 0;
                string NewCode = "";
                while (NewCode.Length == 0 && iters < 25) {
                    NewCode = SharedReadAllText(useTempFileName);
                    if (NewCode.Length == 0)
                        Thread.Sleep(20);       // some IDEs implement "save" by actually doing "delete" or "clear" and then "write". So we might need to wait for the file to update.
                }
                if (NewCode.Length == 0) {
                    Debugger.Break();       // how does this happen??
                    return;
                }

                SourceCodeDataType CurrentCode = Provider.GetCurrentSourceCode();

                if (NewCode != CurrentCode.CodeText)
                {
                    CurrentCode.CodeText = NewCode;
                    Provider.UpdateSourceCode(CurrentCode);
                    OnSourceCodeModified?.Invoke(this);
                }
            }
        }



        /// this block is Windows-specific
        public static void BringProcessToFront(Process process)
        {
            const int SW_RESTORE = 9;
            IntPtr handle = process.MainWindowHandle;
            if (IsIconic(handle)) {
                ShowWindow(handle, SW_RESTORE);
            }
            SetForegroundWindow(handle);
        }
        [System.Runtime.InteropServices.DllImport("User32.dll")]
        private static extern bool SetForegroundWindow(IntPtr handle);
        [System.Runtime.InteropServices.DllImport("User32.dll")]
        private static extern bool ShowWindow(IntPtr handle, int nCmdShow);
        [System.Runtime.InteropServices.DllImport("User32.dll")]
        private static extern bool IsIconic(IntPtr handle);
///
    }


    public sealed class SourceCodeEditingSystem
    {
        // these are windows-specific...need OS branching here...
        public static string VSCodeUserPath = "%LOCALAPPDATA%\\Programs\\Microsoft VS Code\\Code.exe";
        public static string VSCodeSystemPath64 = "C:\\Program Files\\Microsoft VS Code\\Code.exe";
        public static string VSCodeSystemPath32 = "C:\\Program Files\\Microsoft VS Code\\Code.exe";
        public static string NotepadPath = "C:\\Windows\\System32\\notepad.exe";

        public static string GetCodeEditorPath()
        {
            string SettingsPath = NodeEditorConfig.CodeTextEditorPath;

            string[] TryPaths = [SettingsPath, VSCodeUserPath, VSCodeSystemPath64, VSCodeSystemPath32, NotepadPath];
            foreach (string path in TryPaths) {
                string EXEPath = Environment.ExpandEnvironmentVariables(path);
                if (File.Exists(EXEPath))
                    return EXEPath;
            }
            return "";
        }


        // singleton pattern
        static SourceCodeEditingSystem() { }
        private SourceCodeEditingSystem() { }
        private static readonly SourceCodeEditingSystem instance = new SourceCodeEditingSystem();
        public static SourceCodeEditingSystem Instance {
            get { 
                return instance;
            }
        }


        List<SourceCodeEditSession> ActiveSessions = new List<SourceCodeEditSession>();


        public void BeginCodeEdit(ISourceCodeProvider provider)
        {
            SourceCodeEditSession? ExistingSession = FindSession(provider);
            if (ExistingSession != null)
            {
                ExistingSession.TryLaunchTextEditor();
            }
            else
            {
                SourceCodeEditSession NewEditSession = new SourceCodeEditSession(provider);
                NewEditSession.TryLaunchTextEditor();
                ActiveSessions.Add(NewEditSession);
            }
        }

        public void TerminateCodeEdit(ISourceCodeProvider provider)
        {
            SourceCodeEditSession? ExistingSession = FindSession(provider);
            if (ExistingSession != null)
            {
                ExistingSession.Shutdown();
                ActiveSessions.Remove(ExistingSession);
            }
        }

        public SourceCodeEditSession? FindSession(ISourceCodeProvider provider)
        {
            foreach (SourceCodeEditSession session in ActiveSessions) 
            {
                if (session.ProviderRef.TryGetTarget(out var Provider))
                    if (Provider == provider)
                        return session;
            }
            return null;
        }


        public void TerminateSession(SourceCodeEditSession session)
        {
            session.Shutdown();
            ActiveSessions.Remove(session);
        }

        public void TerminateAllCodeEdits()
        {
            foreach (SourceCodeEditSession session in ActiveSessions)
                session.Shutdown();
            ActiveSessions.Clear();
        }








        // this is a gross hack to allow the CodeNode widgets to launch an
        // in-app code editing wizard

        public interface IExternalCodeEditingHandler
        {
            void BeginCodeEditingSession(ISourceCodeProvider provider);
        }
        private static IExternalCodeEditingHandler? CurrentExternalHandler = null;
        public static void SetExternalCodeEditingHandler(IExternalCodeEditingHandler? handler) {
            CurrentExternalHandler = handler;
        }
        public static void LaunchExternalCodeEditSession(ISourceCodeProvider provider) {
            CurrentExternalHandler?.BeginCodeEditingSession(provider);
        }



    }

}
