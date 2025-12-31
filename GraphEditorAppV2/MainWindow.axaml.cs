// Copyright Gradientspace Corp. All Rights Reserved.
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Shapes;
using Avalonia.Dialogs;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using g3;
using Gradientspace.NodeGraph;
using Gradientspace.NodeGraph.Util;
using Gradientspace.UI;
using GSNodeEditor;
using GSPython;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics.Tracing;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;


namespace GraphEditorAppV2;


public class ActionCommand : ICommand
{
	private readonly Predicate<object?> _canExecute;
	private readonly Action<object?> _execute;

	public ActionCommand(Predicate<object?> canExecute, Action<object?> execute) {
		_canExecute = canExecute;
		_execute = execute;
	}

	// what is this for??
	// https://blog.postsharp.net/wpf-command
	//   "This event occurs when changes occur that affect whether or not the command should execute."
	//   (should just be an internal event that never fires??)
	public event EventHandler? CanExecuteChanged {
		add { } // => CommandManager.RequerySuggested += value;
		remove { } // => CommandManager.RequerySuggested -= value;
	}

	public bool CanExecute(object? parameter)
	{
		return _canExecute(parameter);
	}

	public void Execute(object? parameter)
	{
		_execute(parameter);
	}
}



public partial class MainWindow : Window, SourceCodeEditingSystem.IExternalCodeEditingHandler
{
    public string[]? StartupArguments = null;

    public MainWindow()
    {
        InitializeComponent();

		//Canvas.SetLeft(myButton, 10);
		//Canvas.SetTop(myButton, 900);

		RegisterGlobalKeyBindings();

		this.Loaded += MainWindow_Loaded;
        this.Closing += MainWindow_OnClosing;

        if (RunButton.Parent is Menu MainMenu) {
            MainMenu.Items.Remove(StopButton);
        }
    }

    protected async void MainWindow_OnClosing(object? o, WindowClosingEventArgs e)
	{
        e.Cancel = true;
        this.Closing -= MainWindow_OnClosing;

        bool bCancel = await TrySaveUnsavedGraph();
        if (bCancel == true) {
            this.Closing += MainWindow_OnClosing;
            return;
        }

		// tbd do this at app level?
		PythonSetup.PythonShutdown();

        Close();
        //base.OnClosing(e);
    }

    private void MainWindow_Loaded(object? sender, RoutedEventArgs e)
    {
        PythonSetup.InitializePython();

        SkiaView.InitializeGraph();
        SkiaView.ActiveViewport.SetActiveHostAPI(
            new GraphEditorHostImpl(this));
        GlobalGraphOutput.OnGraphOutputUpdated += GlobalGraphOutput_OnGraphOutputUpdated;
        LogTextArea.Text += "\r\n"; // ugh

        SkiaView.Focus(NavigationMethod.Pointer);

        Option_LoadLastOnStartup.IsChecked = NodeEditorConfig.LoadLastGraphOnStartup;
        Option_EnableGraphDebug.IsChecked = DebugManager.GlobalEnableGraphDebugging;
        Option_EnableDebugSingleStep.IsChecked = DebugManager.Instance.EnableStepByStep;

        UpdateRecentFilesMenu();

        bool bLoadedStartupGraph = false;
        if (StartupArguments != null && StartupArguments.Length > 0) {
            if (File.Exists(StartupArguments[0])) {
                TryLoadGraphFromPath(StartupArguments[0], false);
                bLoadedStartupGraph = true;     // TODO identify failure to load?? (but it's async...)
            }
        }
        if (bLoadedStartupGraph == false && NodeEditorConfig.LoadLastGraphOnStartup) { 
            TryLoadGraphFromPath(NodeEditorConfig.EnumerateRecentFiles().FirstOrDefault(), false);
            bLoadedStartupGraph = true;
        }

        SkiaView.ActiveViewport.OnGraphEvalStarted += ActiveViewport_OnGraphEvalStarted;
        SkiaView.ActiveViewport.OnGraphEvalEnded += ActiveViewport_OnGraphEvalEnded;

        // register as dragdrop handler
        DragDrop.SetAllowDrop(this, true);
        AddHandler(DragDrop.DropEvent, HandleMainWindowDropEvent);

        // register as external code editor
        SourceCodeEditingSystem.SetExternalCodeEditingHandler(this);
    }

    protected override void OnGotFocus(GotFocusEventArgs e)
    {
        SkiaView.Focus(NavigationMethod.Pointer);
    }


	private bool bActiveLogFilterOutput = true;

    private void GlobalGraphOutput_OnGraphOutputUpdated(string? appendedLine, EGraphOutputType OutputType)
    {
        Dispatcher.UIThread.InvokeAsync(() => {

            string prefix = "";
            if (OutputType == EGraphOutputType.GraphError)
                prefix = "[GRAPH_ERROR] ";

            if (bActiveLogFilterOutput && (OutputType != EGraphOutputType.User && OutputType != EGraphOutputType.GraphError))
                return;
            LogTextArea.Text += prefix + appendedLine + "\r\n";
            LogTextAreaScrollView.ScrollToEnd();
        });
    }
    private void UpdateLogWindow()
	{
		Dispatcher.UIThread.InvokeAsync(() => {

			// TODO figure out a way to do this more generically...
			StringBuilder stringBuilder = new StringBuilder();
			if ( GlobalGraphOutput.GetCurrentOutput() is DefaultGraphOutputImpl output )
			{
				foreach ( var lineTuple in output.EnumerateLines()) {
					if (bActiveLogFilterOutput && lineTuple.Item2 != EGraphOutputType.User)
						continue;
					stringBuilder.AppendLine(lineTuple.Item1);
				}
			}
			LogTextArea.Text = stringBuilder.ToString();

			LogTextAreaScrollView.ScrollToEnd();
		});
	}
	private void SetLogFilter_Output(object? sender, RoutedEventArgs e)
	{
		bActiveLogFilterOutput = true;
		UpdateLogWindow();
	}
	private void SetLogFilter_All(object? sender, RoutedEventArgs e)
	{
		bActiveLogFilterOutput = false;
		UpdateLogWindow();
	}
	private void ClearLog_OnClick(object? sender, RoutedEventArgs e)
	{
		GlobalGraphOutput.Clear();
		UpdateLogWindow();
	}


    private string make_truncated_path(string Path, int maxLength)
    {
        if (Path.Length <= maxLength)
            return Path;

        string Filename = System.IO.Path.GetFileName(Path);
        string Directory = System.IO.Path.GetDirectoryName(Path) ?? "";
        int Remaining = maxLength - Filename.Length - 5; 
        if (Remaining < 0)
            return $"(...)\\{Filename}";

        int idx = Directory.IndexOf('\\', Directory.Length - Remaining);
        string ShowDir = (idx > 0) ? Directory.Substring(idx) : Directory.Substring(Directory.Length - Remaining);
        return $"(...){ShowDir}\\{Filename}";
    }

	private void UpdateRecentFilesMenu()
	{
		var MakeItem = (string path) => {
            string ShowPath = make_truncated_path(path, 50);
            //string ShowPath = path;
            Avalonia.Controls.MenuItem TmpItem = new() { Header = ShowPath };
			TmpItem.Click += (object? sender, RoutedEventArgs e) => { TryLoadGraphFromPath(path, true); };
            ToolTip.SetTip(TmpItem, path);
            return TmpItem;
		};
		RecentFilesMenu.Items.Clear();
        foreach (string path in NodeEditorConfig.EnumerateRecentFiles()) {
            Avalonia.Controls.MenuItem item = MakeItem(path);
            RecentFilesMenu.Items.Add(item);
            item.MaxWidth = 1000;
        }
    }


	private void RegisterGlobalKeyBindings()
	{
		// this seems to pre-empt any lower-level handling of space key...breaks text entry!
		//KeyBinding binding = new KeyBinding();
		//binding.Gesture = new KeyGesture(Key.Space);
		//binding.Command = new ActionCommand(
		//	(o) => { return true; },
		//	(o) => { RunGraphEvaluationCommand(); });
		//KeyBindings.Add(binding);
	}
	private void RunGraphEvaluationCommand()
	{
		SkiaView.ActiveViewport.RunGraphEvaluation();
	}
    private void CancelGraphEvaluationCommand()
    {
        SkiaView.ActiveViewport.CancelGraphEvaluation();
    }

    private void ActiveViewport_OnGraphEvalEnded(object? sender, EventArgs e)
    {
        // note: changing visibility doesn't seem to work here because 
        // the Menu will still draw a little rectangle that can be hovered
        // (Seems like the Button is a child of some kind of MenuItem that is not exposed)
        if (StopButton.Parent is Menu MainMenu) {
            MainMenu.Items.Remove(StopButton);
            MainMenu.Items.Add(RunButton);
        }
    }
    private void ActiveViewport_OnGraphEvalStarted(object? sender, EventArgs e)
    {
        if ( RunButton.Parent is Menu MainMenu) {
            MainMenu.Items.Remove(RunButton);
            MainMenu.Items.Add(StopButton);
        }
    }



    // add tab to a tab control
    //public void MainThing_ClickHandler(object sender, RoutedEventArgs args)
    //{
    //	TabItem newItem = new TabItem();
    //	newItem.Header = "Meep";
    //	MyTabControl.Items.Add(newItem);
    //}


    private async Task<bool> TrySaveUnsavedGraph()
    {
        bool bCancelOp = false;
        if (SkiaView.ActiveViewport.CurrentGraphIsSaved == false) {
            var dialog = new SaveCurrentDialog();
            if (SkiaView.ActiveViewport.CanSaveCurrentGraph == false)
                dialog.HideSaveButton();
            await dialog.ShowDialog(this);
            if (dialog.Selected == SaveCurrentDialog.ESelectedOptions.Save) {
                if (SkiaView.ActiveViewport.TrySave() == false)
                    bCancelOp = true;
            } else if (dialog.Selected == SaveCurrentDialog.ESelectedOptions.SaveAs) {
                if (SkiaView.ActiveViewport.TrySaveAs() == false)
                    bCancelOp = true;
            } else if (dialog.Selected == SaveCurrentDialog.ESelectedOptions.Cancel) { 
                bCancelOp = true;
            }
        }
        return bCancelOp;
    }

	private void Exit_OnClick(object? sender, RoutedEventArgs e)
	{
		if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktopApp)
			desktopApp.Shutdown();
		// otherwise on mobile??
	}
	private async void New_OnClick(object? sender, RoutedEventArgs e)
	{
        bool bCanceled = await TrySaveUnsavedGraph();
        if (!bCanceled) {
            SkiaView.ActiveViewport.TryNewExecutionGraph();
            SkiaView.Focus(NavigationMethod.Pointer);
        }
	}
	private async void Open_OnClick(object? sender, RoutedEventArgs e)
	{
        bool bCanceled = await TrySaveUnsavedGraph();
        if (!bCanceled) {
            if (SkiaView.ActiveViewport.TryOpen())
                UpdateRecentFilesMenu();
            SkiaView.Focus(NavigationMethod.Pointer);
        }
    }
    private void Import_OnClick(object? sender, RoutedEventArgs e)
    {
        SkiaView.ActiveViewport.TryImport();
        SkiaView.Focus(NavigationMethod.Pointer);
    }
    private void Save_OnClick(object? sender, RoutedEventArgs e)
	{
		if (SkiaView.ActiveViewport.TrySave())
			UpdateRecentFilesMenu();
		SkiaView.Focus(NavigationMethod.Pointer);
	}
	private void SaveAs_OnClick(object? sender, RoutedEventArgs e)
	{
		if (SkiaView.ActiveViewport.TrySaveAs())
			UpdateRecentFilesMenu();
		SkiaView.Focus(NavigationMethod.Pointer);
	}


    private void HandleMainWindowDropEvent(object? sender, DragEventArgs e)
    {
        if (e.Data.GetFiles() is { } fileNames) {
            Uri pathURI = fileNames.First().Path;
            if (pathURI.IsFile) {
                string FilePath = pathURI.LocalPath;
                if (System.IO.Path.Exists(FilePath)) {
                    TryLoadGraphFromPath(FilePath, true);
                }
            }
            e.Handled = true;
        }
    }



    private async void TryLoadGraphFromPath(string? path, bool bIsInteractive)
	{
        bool bCanceled = false;
        if (bIsInteractive) {
            bCanceled = await TrySaveUnsavedGraph();
            if (bCanceled)
                return;
        }

		if (path != null && File.Exists(path) )
			SkiaView.ActiveViewport.OpenGraphFile(path);
	}


    private void OpenCurrentFolder_OnClick(object? sender, RoutedEventArgs e)
    {
        // todo could fall back to default user graph folder...
        if (File.Exists(SkiaView.ActiveViewport.CurrentGraphFilePath) ) {
            System.Diagnostics.Process.Start("explorer.exe", "/select," + SkiaView.ActiveViewport.CurrentGraphFilePath);
        }
    }
    private void OpenCurrentFile_OnClick(object? sender, RoutedEventArgs e)
    {
        if ( File.Exists(SkiaView.ActiveViewport.CurrentGraphFilePath) )
            System.Diagnostics.Process.Start("explorer.exe", SkiaView.ActiveViewport.CurrentGraphFilePath);
    }

    private void Copy_OnClick(object? sender, RoutedEventArgs e)
    {
        SkiaView.ActiveViewport.CopySelectionToClipboard();
        SkiaView.Focus(NavigationMethod.Pointer);
    }
    private async void Paste_OnClick(object? sender, RoutedEventArgs e)
    {
        // get clipboard text
        if (Clipboard == null)
            return;
        string? text = await Clipboard.GetTextAsync();
        if (text != null) {
            SkiaView.ActiveViewport.PasteGraphFromJson(text);
            SkiaView.Focus(NavigationMethod.Pointer);
        }
    }


    private void NewFunction_OnClick(object? sender, RoutedEventArgs e)
    {
        SkiaView.ActiveViewport.AddNewFunction();
        SkiaView.Focus(NavigationMethod.Pointer);
    }

    private async void NodeWizard_OnClick(object? sender, RoutedEventArgs e)
    {
        var dialog = new NodeWizardDialog() { GraphViewport = SkiaView.ActiveViewport };
        await dialog.ShowDialog(this);
    }


    private void GenerateCode_OnClick(object? sender, RoutedEventArgs e)
    {
        ExecutionGraph? graph = SkiaView.ActiveViewport.CurrentGraphView.GetGraph() as ExecutionGraph;
        ExecutionGraphCodeGen codeGen = new ExecutionGraphCodeGen(graph!);
        string result = codeGen.GenerateCode();
        File.WriteAllTextAsync("c:\\scratch\\CODEGEN.cs", result);
    }


    private void GraphDebugging_OnToggle(object? sender, RoutedEventArgs e)
	{
        NodeEditorRuntimeSettings.EnableDebugging.Value = !NodeEditorRuntimeSettings.EnableDebugging.Value;
		//DebugManager.GlobalEnableGraphDebugging = !DebugManager.GlobalEnableGraphDebugging;
		Option_EnableGraphDebug.IsChecked = DebugManager.GlobalEnableGraphDebugging;
        NodeEditorConfig.SaveConfig();
    }
    private void GraphDebugSingleStep_OnToggle(object? sender, RoutedEventArgs e)
    {
        DebugManager.Instance.EnableStepByStep = !DebugManager.Instance.EnableStepByStep;
        Option_EnableDebugSingleStep.IsChecked = DebugManager.Instance.EnableStepByStep;
    }
    private void LoadLastOnStartup_OnToggle(object? sender, RoutedEventArgs e)
	{
		NodeEditorConfig.LoadLastGraphOnStartup = !NodeEditorConfig.LoadLastGraphOnStartup;
		Option_LoadLastOnStartup.IsChecked = NodeEditorConfig.LoadLastGraphOnStartup;
		NodeEditorConfig.SaveConfig();
	}


    protected void DebugDelay_OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Avalonia.Controls.MenuItem menuItem && menuItem.Tag is string tag) {
            if (tag == "debugdelay_0")
                DebugManager.DebugDelayMS = 1;      // sleep at least 1 ms means render thread will update
            else if (tag == "debugdelay_100ms")
                DebugManager.DebugDelayMS = 100;
            else if (tag == "debugdelay_500ms")
                DebugManager.DebugDelayMS = 500;
            else if (tag == "debugdelay_1s")
                DebugManager.DebugDelayMS = 1000;
            else if (tag == "debugdelay_2s")
                DebugManager.DebugDelayMS = 2000;
        }
    }

    protected void Run_OnClick(object? sender, RoutedEventArgs e)
    {
        RunGraphEvaluationCommand();
    }
    protected void Stop_OnClick(object? sender, RoutedEventArgs e)
    {
        CancelGraphEvaluationCommand(); 
    }


    private void OpenSettingsFolder_OnClick(object? sender, RoutedEventArgs e)
    {
        string ConfigDir = System.IO.Path.GetDirectoryName(NodeEditorConfig.UserConfigFilePath) ?? "";
        if (Directory.Exists(ConfigDir)) {
            System.Diagnostics.Process.Start("explorer.exe", ConfigDir);
        }
    }
    private void EditSettingsFile_OnClick(object? sender, RoutedEventArgs e)
    {
        if (File.Exists(NodeEditorConfig.UserConfigFilePath)) {
            System.Diagnostics.Process.Start("explorer.exe", NodeEditorConfig.UserConfigFilePath);
        }
    }
    private async void ReloadSettings_OnClick(object? sender, RoutedEventArgs e)
    {
        bool bCanceled = await TrySaveUnsavedGraph();
        if (!bCanceled) {
            // reload the config file
            NodeEditorConfig.LoadConfig();

            // find and load all node libraries
            NodeLibraryUtils.FindAndLoadNodeLibraries(NodeEditorConfig.NodeLibraryPaths);

            // reload / re-instance active graph
            SkiaView.ActiveViewport.RebuildGraphLibraryWithActiveGraph();
            SkiaView.Focus(NavigationMethod.Pointer);
        }
    }
    public void TryLoadNewLibrariesAndReinstanceGraph()
    {
        if (NodeLibraryUtils.FindAndLoadNodeLibraries(NodeEditorConfig.NodeLibraryPaths) > 0) {
            SkiaView.ActiveViewport.RebuildGraphLibraryWithActiveGraph();
        }
    }

    private void EditAPIKeys_OnClick(object? sender, RoutedEventArgs e)
    {
        if (File.Exists(NodeEditorSecrets.UserSecretsFilePath)) {
            System.Diagnostics.Process.Start("explorer.exe", NodeEditorSecrets.UserSecretsFilePath);
        }
    }


    protected async void NodeLibraries_OnClick(object? sender, RoutedEventArgs e)
    {
        var dialog = new NodeLibrariesDialog();
        await dialog.ShowDialog(this);
    }
    protected async void RefreshNodeLibraries_OnClick(object? sender, RoutedEventArgs e)
    {
        bool bCanceled = await TrySaveUnsavedGraph();
        if (!bCanceled) {
            NodeLibraryUtils.FindAndLoadNodeLibraries(NodeEditorConfig.NodeLibraryPaths);
            SkiaView.ActiveViewport.RebuildGraphLibraryWithActiveGraph();
        }
    }


    protected override void OnTextInput(TextInputEventArgs e)
	{
		// handle space-to-evaluate hotkey at window level so that it works even if skia page does not have focus
		// (todo: figure out cleaner way to handle that as we will want other hotkeys...)
		if (e.Text == " ")
		{
			e.Handled = true;
			RunGraphEvaluationCommand();
		}
		base.OnTextInput(e);
	}

    //protected override void OnKeyUp(KeyEventArgs e)
    //{
    //	if (e.Key == Key.Space) {
    //		RunGraphEvaluationCommand();
    //	}
    //}




    // SourceCodeEditingSystem.IExternalCodeEditingHandler implementation
    public async void BeginCodeEditingSession(ISourceCodeProvider provider)
    {
        var dialog = new NodeWizardDialog() { GraphViewport = SkiaView.ActiveViewport };
        dialog.InitializeFromCodeProvider(provider);
        await dialog.ShowDialog(this);
    }


}
