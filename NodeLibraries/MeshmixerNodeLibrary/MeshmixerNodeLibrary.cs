using g3;
using Gradientspace.NodeGraph;
using System.Diagnostics;

namespace Meshmixer.Nodes
{
    public static class MeshmixerNodeLibrary
    {
        public static void Initialize()
        {
        }
    }


    public class MeshmixerConnection : IDisposable
    {
        public mm.RemoteControl Connection = null;
        public bool Open()
        {
            Connection = new mm.RemoteControl();
            bool bConnected = Connection.Initialize();
            if (!bConnected)
                Connection = null;
            return bConnected;
        }

        public bool IsConnected => Connection != null;

        public void Close()
        {
            if (IsConnected)
                Connection.Shutdown();
            Connection = null;
        }

        ~MeshmixerConnection() {
            Close();
        }
        public void Dispose() {
            Close();
        }
    }


    [NodeFunctionLibrary("MeshmixerNodes")]
    public unsafe static class MeshmixerLibrary
    {
        [NodeFunction(ReturnName = "Connection")]
        public static MeshmixerConnection ConnectToMeshmixer(
            bool StartIfNeeded = true )
        {
            MeshmixerConnection c = new MeshmixerConnection();

            Process[] all = Process.GetProcesses();
            bool bFound = false;
            foreach (Process p in all) {
                if ( p.ProcessName.Contains("Meshmixer", StringComparison.OrdinalIgnoreCase)) {
                    bFound = true;
                    break;
                }
            }

            if ( !bFound && !StartIfNeeded ) {
                GlobalGraphOutput.AppendError("[ConnectToMeshmixer] Meshmixer is not running and StartIfNeeded=false");
                return c;
            }
            if (!bFound) 
            {
                string MeshmixerPath = "C:\\Program Files\\Autodesk\\Meshmixer\\Meshmixer.exe";
                ProcessStartInfo psi = new ProcessStartInfo {
                    FileName = MeshmixerPath,
                    Arguments = "",
                    UseShellExecute = false, // Crucial for redirecting input/output and hiding the window
                    CreateNoWindow = true,   // Hides the process window
                    RedirectStandardOutput = true, RedirectStandardError = true
                };
                try {
                    Process process = Process.Start(psi);
                    Thread.Sleep(1000);
                } catch (Exception ex) {
                    GlobalGraphOutput.AppendError(ex.Message);
                }
            }

            bool bOK = c.Open();
            if (!bOK) {
                GlobalGraphOutput.AppendError("[ConnectToMeshmixer] failed to connect");
            }
            return c;
        }

        [NodeFunction(ReturnName = "Connection")]
        public static void MMImportFile(ref MeshmixerConnection MM, string FilePath, bool bNewScene = true)
        {
            if (MM == null)
                return;
            if (bNewScene)
                MM.Connection.ClearScene();
            MM.Connection.ImportFile(FilePath);
        }

        [NodeFunction(ReturnName = "Connection")]
        public static void MMPlaneCut(
            ref MeshmixerConnection MM,
            Vector3d Location,
            Vector3d Normal )
        {
            if (MM == null)
                return;
            StoredCommands sc = new StoredCommands();
            sc.AppendBeginToolCommand("planeCut");
            vec3f origin = new vec3f(); origin.x = (float)Location.x; origin.y = (float)Location.y; origin.z = (float)Location.z;
            sc.AppendToolParameterCommand("origin", origin);
            vec3f normal = new vec3f(); normal.x = (float)Normal.x; normal.y = (float)Normal.y; normal.z = (float)Normal.z;
            sc.AppendToolParameterCommand("normal", normal);
            sc.AppendCompleteToolCommand("accept");
            MM.Connection.ExecuteCommands(sc);
        }


    }


}
