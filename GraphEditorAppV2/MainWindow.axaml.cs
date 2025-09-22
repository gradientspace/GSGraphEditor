// Copyright Gradientspace Corp. All Rights Reserved.
using Avalonia.Controls;
using Avalonia.Dialogs;
using Avalonia.Input;
using Avalonia.Interactivity;
using GSNodeEditor;
using System;
using System.ComponentModel.Design;
using System.Diagnostics.Tracing;
using System.Windows.Input;

using Avalonia.Platform.Storage;
using System.Collections.Generic;
using Gradientspace.NodeGraph;
using System.Linq;
using System.Runtime.CompilerServices;
using Gradientspace.UI;
using System.IO;
using Avalonia.Threading;
using GSPython;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Shapes;
using System.Text;
using System.Threading.Tasks;
using g3;


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



public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

		//Canvas.SetLeft(myButton, 10);
		//Canvas.SetTop(myButton, 900);

		RegisterGlobalKeyBindings();

		this.Loaded += MainWindow_Loaded;
        this.Closing += MainWindow_OnClosing;
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

		UpdateRecentFilesMenu();
		if (NodeEditorConfig.LoadLastGraphOnStartup)
			TryLoadGraphFromPath( NodeEditorConfig.EnumerateRecentFiles().FirstOrDefault(), false );
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

	private void UpdateRecentFilesMenu()
	{
		var MakeItem = (string path) => {
			Avalonia.Controls.MenuItem TmpItem = new() { Header = path };
			TmpItem.Click += (object? sender, RoutedEventArgs e) => { TryLoadGraphFromPath(path, true); };
			return TmpItem;
		};
		RecentFilesMenu.Items.Clear();
		foreach (string path in NodeEditorConfig.EnumerateRecentFiles())
			RecentFilesMenu.Items.Add(MakeItem(path));
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

    private void GenerateCode_OnClick(object? sender, RoutedEventArgs e)
    {
        ExecutionGraph? graph = SkiaView.ActiveViewport.CurrentGraphView.GetGraph() as ExecutionGraph;
        ExecutionGraphCodeGen codeGen = new ExecutionGraphCodeGen(graph!);
        string result = codeGen.GenerateCode();
        File.WriteAllTextAsync("c:\\scratch\\CODEGEN.cs", result);
    }


    private void GraphDebugging_OnToggle(object? sender, RoutedEventArgs e)
	{
		DebugManager.GlobalEnableGraphDebugging = !DebugManager.GlobalEnableGraphDebugging;
		Option_EnableGraphDebug.IsChecked = DebugManager.GlobalEnableGraphDebugging;
	}
	private void LoadLastOnStartup_OnToggle(object? sender, RoutedEventArgs e)
	{
		NodeEditorConfig.LoadLastGraphOnStartup = !NodeEditorConfig.LoadLastGraphOnStartup;
		Option_LoadLastOnStartup.IsChecked = NodeEditorConfig.LoadLastGraphOnStartup;
		NodeEditorConfig.SaveConfig();
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
	
}
