// Copyright Gradientspace Corp. All Rights Reserved.
using g3;
using Gradientspace.NodeGraph;
using Gradientspace.NodeGraph.CodeNodes;
using Gradientspace.NodeGraph.Geometry;
using Gradientspace.NodeGraph.Image;
using Gradientspace.NodeGraph.Nodes;
using Gradientspace.NodeGraph.PythonNodes;
using Gradientspace.UI;
using GSNodeEditor.NodeGraphWidgets;
using Microsoft.CodeAnalysis;
using SkiaSharp;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Text;

//using Gradientspace.NodeGraph.Testing;
//using Gradientspace.NodeGraph.GeometryBuffersTestLibrary;
//using Gradientspace.NodeGraph.UnrealEngine;


namespace GSNodeEditor
{
    public class NodeGraphViewport : IHotkeyTarget, INodeGraphEditManager, IGraphEditorActions
	{
        public bool EnableAutoSaveBackups { get; set; } = true;

        protected NodeEditorViewportUI viewportUI;
        protected SelectionManager selectionManager;

        protected WidgetScene widgetScene;
        protected SKStyleCache styleCache;

        protected BaseGraph CurrentGraph;

        protected DataFlowGraph? UsingDataFlowGraph = null;
        protected DataFlowGraphEvaluator? UsingDataFlowGraphEvaluator = null;

        protected ExecutionGraph? UsingExecutionGraph = null;
        protected ExecutionGraphEvaluator? UsingExecutionGraphEvaluator = null;
        protected List<NodeBase> CurrentErrorStateNodes = new List<NodeBase>();

        // maybe some of these should be owned by some intermediate data structure not tied explicitly to the Viewport??
        public NodeGraphView CurrentGraphView;
        public InteractionManager InteractionManager;
        protected BaseGraphEditor GraphEditor;
		public GraphStaticAnalyzer GraphAnalysis;
		protected GraphEditHistory EditHistory;

		protected EditorHostAPI? HostAPI;

        public event EventHandler? OnGraphEvalStarted;
        public event EventHandler? OnGraphEvalEnded;

        public void Initialize()
        {
			GlobalGraphOutput.AppendLine($"Default User Files Path is {NodeEditorConfig.DefaultUserFilesPath}", EGraphOutputType.Logging);

            viewportUI = new NodeEditorViewportUI(this);

            // Need to initialize libraries so that types can be registered
            NodeGraphCoreLibrary.Initialize();
            NodeGraphGeometryLibrary.Initialize();
            NodeGraphImageLibrary.Initialize();

            // these just force assemblies to be loaded so that the nodes will show up in the library
            // (should convert this to dynamic-discovery...)
            GeometryViewerNodeLibrary.Initialize();
            //NodeGraphUnrealEngineLibrary.Initialize();
            //NodeGraphTestingLibrary.Initialize();
            GSPythonNodesLibrary.Initialize();

            // force node library to start building
            DefaultNodeLibrary.Instance.BuildAsync();

            //InlinePinWidgetSystem.Instance.RegisterProvider(
            //    typeof(SourceCodeDataType), new SourceCodeInlinePinWidgetProvider());
            NodeWidgetCustomizationSystem.Instance.RegisterProvider(
                typeof(MissingNodeErrorNode), new MissingNodeWidgetProvider());
            NodeWidgetCustomizationSystem.Instance.RegisterProvider(
                typeof(FunctionDefinitionNode), new FunctionDefnNodeWidgetProvider());
            NodeWidgetCustomizationSystem.Instance.RegisterProvider(
                typeof(CodeFunctionNode), new CodeFunctionNodeWidgetProvider());
            NodeWidgetCustomizationSystem.Instance.RegisterProvider(
                typeof(RerouteNode), new RerouteNodeWidgetProvider());
            // todo support base classes...
            NodeWidgetCustomizationSystem.Instance.RegisterProvider(
                typeof(CreateAliasNode), new AliasNodeWidgetProvider());
            NodeWidgetCustomizationSystem.Instance.RegisterProvider(
                typeof(GetAliasNode), new AliasNodeWidgetProvider());
            NodeWidgetCustomizationSystem.Instance.RegisterProvider(
				typeof(PythonFunctionCodeNode), new CodeFunctionNodeWidgetProvider());
            NodeWidgetCustomizationSystem.Instance.RegisterProvider(
                typeof(ImageViewNode), new ImageViewNodeWidgetProvider());
            NodeWidgetCustomizationSystem.Instance.RegisterProvider(
                typeof(StringViewNode), new StringViewNodeWidgetProvider());
            NodeWidgetCustomizationSystem.Instance.RegisterProvider(
                typeof(ObjectViewNode), new StringViewNodeWidgetProvider());

            //UsingDataFlowGraph = MakeInitialDataflowGraph();
            //UsingDataFlowGraphEvaluator = new DataFlowGraphEvaluator(UsingDataFlowGraph);
            //UsingDataFlowGraphEvaluator.EnableDebugPrinting = true;
            //UsingDataFlowGraphEvaluator.EvaluateAllOutputs(null);
            //CurrentGraph = UsingDataFlowGraph;

            UsingExecutionGraph = MakeInitialExecutionGraph();
            UsingExecutionGraphEvaluator = new ExecutionGraphEvaluator(UsingExecutionGraph);
            UsingExecutionGraphEvaluator.EnableDebugPrinting = true;
            CurrentGraph = UsingExecutionGraph;

            RebuildGraphView();

            //RunGraphEvaluation();

            ViewportTranslation = Vector2f.Zero;
            ViewportScale = 1.0f;
            UIScale = 1.0f;

            // possibly figure out how to remove this...
            SystemKeyboardRouter.Instance.OnNewPressedKeyFunc = KeyboardRouter_OnNewPressedKey;
            SystemKeyboardRouter.Instance.PushHotkeyTarget(this);

            TooltipManager.Instance.OnTooltipDrawUpdatePending += Instance_OnTooltipDrawUpdatePending;
        }

        public void Shutdown()
        {
            widgetScene.Dispose();
            viewportUI.Dispose();

            GeometryViewer.Instance.Shutdown();
        }


        public void SetActiveHostAPI(EditorHostAPI hostAPI)
        {
            HostAPI = hostAPI;
            SystemKeyboardRouter.Instance.ClipboardAPI = new EditorClipboardTextAccess() { HostAPI = hostAPI };
            UpdateWindowTitle();
        }
        private void Instance_OnTooltipDrawUpdatePending()
        {
            HostAPI?.RequestRepaint();
        }


        protected void RebuildGraphView()
        {
            if (CurrentGraphView != null) {
                selectionManager.Dispose();
                widgetScene.Dispose();
                widgetScene.RemoveSource(CurrentGraphView.WidgetSource);
                CurrentGraphView.OnNewNodeAdded -= CurrentGraphView_OnNewNodeAdded;
                CurrentGraphView.OnExistingNodeUpdated -= CurrentGraphView_OnExistingNodeUpdated;
                CurrentGraphView.OnNodeWidgetModified -= CurrentGraphView_OnNodeWidgetModified;
            }

            widgetScene = new WidgetScene();
            styleCache = new SKStyleCache();

            CurrentGraphView = new NodeGraphView();
            CurrentGraphView.OnNewNodeAdded += CurrentGraphView_OnNewNodeAdded;
            CurrentGraphView.OnExistingNodeUpdated += CurrentGraphView_OnExistingNodeUpdated;
            CurrentGraphView.OnNodeWidgetModified += CurrentGraphView_OnNodeWidgetModified;

            selectionManager = new SelectionManager(this);
            InteractionManager = new InteractionManager(CurrentGraphView, this);
            widgetScene.AddSource(CurrentGraphView.WidgetSource);

            // populate graph view
            CurrentGraph.ValidateDataConnections();
            CurrentGraphView.ConnectToGraph(CurrentGraph);

            //GraphEditor = new NodeGraphEditor(CurrentGraphView, CurrentGraph);
            GraphEditor = new BaseGraphEditor(CurrentGraphView);
            CurrentGraphView.ActiveEditManager = new(this);

            GraphAnalysis = new GraphStaticAnalyzer( (CurrentGraph as ExecutionGraph)! );
            GraphAnalysis.RebuildAll();

			EditHistory = new GraphEditHistory();
            HistorySystem.SetActiveHistory(EditHistory);
		}


        protected static DataFlowGraph MakeInitialDataflowGraph()
        {
            DataFlowGraph dataflowGraph = new DataFlowGraph();
            DoubleConstantNode Constant1 = dataflowGraph.CreateNewNode<DoubleConstantNode>()!;
            DoubleConstantNode Constant2 = dataflowGraph.CreateNewNode<DoubleConstantNode>()!;
            DoubleAddNode Sum = dataflowGraph.CreateNewNode<DoubleAddNode>()!;
            DoubleAddNode Sum2 = dataflowGraph.CreateNewNode<DoubleAddNode>()!;
            dataflowGraph.AddConnection(Constant1, DoubleConstantNode.ValueOutputName, Sum, Sum.Operand1Name);
            dataflowGraph.AddConnection(Constant2, DoubleConstantNode.ValueOutputName, Sum, Sum.Operand2Name);
            dataflowGraph.AddConnection(Sum, Sum.ValueOutputName, Sum2, Sum2.Operand1Name);
            dataflowGraph.AddConnection(Constant2, DoubleConstantNode.ValueOutputName, Sum2, Sum2.Operand2Name);
            PrintWithFormatNode Print = dataflowGraph.CreateNewNode<PrintWithFormatNode>()!;
            dataflowGraph.AddConnection(Sum2, DoubleConstantNode.ValueOutputName, Print, Print.MakeObjectInputName(0));

            dataflowGraph.SetNodeConstantValue(Constant1, DoubleConstantNode.ValueInputName, 2);
            dataflowGraph.SetNodeConstantValue(Constant2, DoubleConstantNode.ValueInputName, 3);
            dataflowGraph.SetNodeConstantValue(Print, PrintWithFormatNode.FormatName, "Value is {0}");

            return dataflowGraph;
        }

        protected static ExecutionGraph MakeEmptyExecutionGraph()
        {
			ExecutionGraph executionGraph = new ExecutionGraph();
			return executionGraph;
		}


		protected static ExecutionGraph MakeInitialExecutionGraph()
        {
            ExecutionGraph executionGraph = new ExecutionGraph();

            DoubleConstantNode Constant1 = executionGraph.CreateNewNode<DoubleConstantNode>()!;
            DoubleConstantNode Constant2 = executionGraph.CreateNewNode<DoubleConstantNode>()!;
            DoubleAddNode Sum = executionGraph.CreateNewNode<DoubleAddNode>()!;
            DoubleAddNode Sum2 = executionGraph.CreateNewNode<DoubleAddNode>()!;
            executionGraph.AddConnection(Constant1, DoubleConstantNode.ValueOutputName, Sum, Sum.Operand1Name);
            executionGraph.AddConnection(Constant2, DoubleConstantNode.ValueOutputName, Sum, Sum.Operand2Name);
            executionGraph.AddConnection(Sum, Sum.ValueOutputName, Sum2, Sum2.Operand1Name);
            executionGraph.AddConnection(Constant2, DoubleConstantNode.ValueOutputName, Sum2, Sum2.Operand2Name);
            PrintWithFormatNode Print = executionGraph.CreateNewNode<PrintWithFormatNode>()!;
            executionGraph.AddConnection(Sum2, DoubleConstantNode.ValueOutputName, Print, Print.MakeObjectInputName(0));

            executionGraph.AddSequenceConnection(executionGraph.StartNodeHandle, "", Print.Handle, "");

            executionGraph.SetNodeConstantValue(Constant1, DoubleConstantNode.ValueInputName, 2);
            executionGraph.SetNodeConstantValue(Constant2, DoubleConstantNode.ValueInputName, 3);
            executionGraph.SetNodeConstantValue(Print, PrintWithFormatNode.FormatName, "Value is {0}");

            return executionGraph;
        }

        /// ViewportTranslation is in Window coordinates/space (Scaled Viewport space) - view transform is Viewport*Scale + Translation
        public Vector2f ViewportTranslation { get; set; }
        /// ViewportScale is scale factor from Viewport to Window coordinates/space - view transform is Viewport*Scale + Translation
        public float ViewportScale { get; set; }
        public AxisAlignedBox2f ViewportBounds { get; protected set; }
        public AxisAlignedBox2f WindowBounds { get; protected set; }
        public WidgetScene WidgetScene { get { return widgetScene; } }

        public float UIScale { get; set; }
        public NodeEditorViewportUI ViewportUI { get { return viewportUI; } }
        public SelectionManager SelectionManager{ get { return selectionManager; } }
        public GraphEditHistory History { get { return EditHistory; } }



        protected InputDeviceState LastDeviceState;

        protected void UpdateDeviceState(InputDeviceState newState)
        {
            LastDeviceState = newState;
        }

        public Vector2f TransformViewportToWindow(Vector2f ViewportPoint)
        {
            return ViewportPoint * ViewportScale + ViewportTranslation;
        }
        public Vector2f TransformWindowToViewport(Vector2f WindowPoint)
        {
            return (WindowPoint - ViewportTranslation) / ViewportScale;
        }

        public Vector2f TransformUIToWindow(Vector2f DevicePoint)
        {
            return DevicePoint * UIScale;
        }
        public Vector2f TransformWindowToUI(Vector2f UIPoint)
        {
            return UIPoint / UIScale;
        }


        public Vector2f TransformViewportToUI(Vector2f ViewportPoint)
        {
            return TransformWindowToUI(TransformViewportToWindow(ViewportPoint));
        }
        public Vector2f TransformUIToViewport(Vector2f UIPoint)
        {
            return TransformWindowToViewport(TransformUIToWindow(UIPoint));
        }


        public void CenterAtViewportPosition(Vector2f ViewportPoint)
        {
            Vector2f CurWindowPos = TransformViewportToWindow(ViewportPoint);
            Vector2f TargetWindowPos = WindowBounds.Center;
            Vector2f Delta = (TargetWindowPos - CurWindowPos);
            ViewportTranslation += Delta;
        }
        public void CenterOnCurrentGraph()
        {
            Vector2d FocusPosition = CurrentGraphView.GetGraphBounds().Center;
            CenterAtViewportPosition((Vector2f)FocusPosition);
        }


        public void UpdateCursor(InputDeviceState newState)
        {
            UpdateDeviceState(newState);

            if ( InteractionManager.IsCapturingInput )
            {
                InteractionManager.OnPointerCaptureMove(LastDeviceState);
            }
            else
            {
                InteractionManager.OnPointerHoverMove(LastDeviceState);
            }
			HostAPI?.RequestRepaint();
		}

        public void OnPointerDown(InputDeviceState newState)
        {
            UpdateDeviceState(newState);

			bool bCaptured = InteractionManager.OnPointerDown(LastDeviceState);

			// currently no concept of Focused widget/item :(
            // So there is no way to know if the new capture is on the focused widget, which is desirable
            // in many cases. For text-entry fields it is critical so we have this hack for now
			bool bIsRepeatClickOnFocusedElement =
                (SystemKeyboardRouter.Instance.TextEntryFocusTarget == InteractionManager.ActiveCaptureRequest.SourceObject);

			if (bCaptured && bIsRepeatClickOnFocusedElement == false)
                SystemKeyboardRouter.Instance.OnChangeWindowFocus(true);

            HostAPI?.RequestRepaint();

            // do we care if we did not capture??
        }

        public void OnPointerUp(InputDeviceState newState)
        {
            UpdateDeviceState(newState);

            if (InteractionManager.IsCapturingInput)
            {
                InteractionManager.OnPointerUp(LastDeviceState);
				HostAPI?.RequestRepaint();
			}
        }

        public void OnWheel(InputDeviceState newState)
        {
            Vector2f CurLocalCursorPos = TransformWindowToViewport(newState.CurrentPosition);

            // only using for zoom right now
            ViewportScale = Math.Clamp(ViewportScale + 0.05f * newState.WheelDelta, 0.1f, 10.0f);

            Vector2f PrevCursorPosInNew = TransformViewportToWindow(CurLocalCursorPos);
            Vector2f Delta = (newState.CurrentPosition - PrevCursorPosInNew);
            ViewportTranslation += Delta;

			HostAPI?.RequestRepaint();
		}


        public void OnEndFocus()
        {
            if (InteractionManager.IsCapturingInput)
            {
                InteractionManager.OnAbortInteraction();
            }
            SystemKeyboardRouter.Instance.OnChangeWindowFocus(true);

			HostAPI?.RequestRepaint();
		}
        public void OnBeginFocus()
        {
            if (InteractionManager.IsCapturingInput)
            {
                InteractionManager.OnAbortInteraction();
            }
            SystemKeyboardRouter.Instance.OnChangeWindowFocus(true);

			HostAPI?.RequestRepaint();
		}


        protected void UpdateWindowTitle()
        {
            if (HostAPI == null) return;
            if (CurrentGraphFilePath.Length == 0)
                HostAPI.SetWindowTitle("(unsaved)");
            else if (CurrentGraphIsSaved)
                HostAPI.SetWindowTitle(CurrentGraphFilePath);
            else
                HostAPI.SetWindowTitle(CurrentGraphFilePath + "*");
        }

        protected void UpdateCurrentFilePath(string filePath)
        {
            CurrentGraphFilePath = filePath;
            UpdateWindowTitle();
        }

        protected void MarkGraphDirty()
        {
            CurrentGraphIsSaved = false;
            UpdateWindowTitle();
			HostAPI?.RequestRepaint();
		}


        const string DefaultExtension = "gg";
        const string DefaultFileFilter = "Node Graphs (*.gg)|*.gg|JSon Node Graphs (*.json)|*.json|All files (*.*)|*.*";
        const string DefaultFileName = "nodegraph.gg";

        public string CurrentGraphFilePath { get; private set; } = "";
        public bool CurrentGraphIsSaved { get; private set; } = false;

        public bool OnKeyChordUpdated(in KeyChord ActiveChord)
        {
            bool bIsSave = ActiveChord.IsChord2(KeyNames.Ctrl, 'S');

            if (ActiveChord.IsSingleSpecialKey(KeyNames.Delete) || ActiveChord.IsSingleSpecialKey(KeyNames.Backspace))
            {
                if ( SelectionManager.HasSelection ) {
                    List<NodeWidget> widgets = SelectionManager.FindSelectedNodeWidgets();
                    ExecuteGraphEdit((NodeGraphEditor Editor) => {
                        foreach ( NodeWidget widget in widgets)
                            Editor.RemoveNode(widget);
                    });
                    return true;
                }
                return false;
            }
            else if (ActiveChord.IsChord3(KeyNames.Ctrl, KeyNames.Shift, 'S') || (bIsSave && CurrentGraphFilePath.Length == 0))
            {
                TrySaveAs();
                return true;
            }
            else if (bIsSave)
            {
                TrySave();
			}
            else if (ActiveChord.IsChord2(KeyNames.Ctrl, 'O'))
            {
                TryOpen();
                return true;
            }
            else if (ActiveChord.IsChord2(KeyNames.Ctrl, 'Z'))
            {
                History.TryStepBackward();
                return true;
            }
            else if (ActiveChord.IsChord2(KeyNames.Ctrl, 'Y'))
            {
                History.TryStepForward();
                return true;
            } 
            else if (ActiveChord.IsChord2(KeyNames.Ctrl, 'C')) 
            {
                CopySelectionToClipboard();
                return true;
            }
            else if (ActiveChord.IsChord2(KeyNames.Ctrl, 'V')) 
            {
                PasteFromClipboard();
                return true;
            }

            return false;
        }

        private bool KeyboardRouter_OnNewPressedKey(KeyboardRouter sender, KeyState[] NewKeyChord)
        {
            if (NewKeyChord.Length == 1 && NewKeyChord[0].IsCharacterKey && NewKeyChord[0].Character == ' ')
            {
                // TODO why are we getting here when this is the case??
                if (sender.HasTextEntryFocusTarget)
                    return false;
                HigherLevelRunGraphEvaluationTemp();
                return true;
            }

            return false;
        }

        private void HigherLevelRunGraphEvaluationTemp()
        {
			Debug.WriteLine("Launching Graph Evaluation...");
			// this runs in background-ish...
			RunGraphEvaluation();

			// TODO currently cannot do this because our UE nodes have no way to know if TCP connection is done
			// sending data, need to implement bidirectional communication
			//GC.Collect();
		}


		//static bool ShowCompactMode = false;

		// IGraphEditorActions interface method
		public bool TrySaveAs()
        {
            string configfile = NodeEditorConfig.UserConfigFilePath;

			string UseDirectory = NodeEditorConfig.GetActiveSaveLoadPath();
			string UseFilename = (CurrentGraphFilePath.Length == 0) ? DefaultFileName : Path.GetFileName(CurrentGraphFilePath)!;
            if ( UseFilename.EndsWith(".gg") == false )
                UseFilename = System.IO.Path.ChangeExtension(UseFilename, "gg");

			if (HostAPI != null && HostAPI.ShowBlockingSaveAsDialog(UseFilename, DefaultExtension, "", UseDirectory, out string SelectedFilename))
			{
				// should check if file exists and prompt to replace?
				SaveGraphToFile(SelectedFilename, EnableAutoSaveBackups);
				CurrentGraphIsSaved = true;
				UpdateCurrentFilePath(SelectedFilename);
                return true;
			}
			return false;
		}

		// IGraphEditorActions interface method
		public bool TryNewExecutionGraph()
        {
			UsingExecutionGraph = MakeEmptyExecutionGraph();
			UsingExecutionGraphEvaluator = new ExecutionGraphEvaluator(UsingExecutionGraph);
			UsingExecutionGraphEvaluator.EnableDebugPrinting = true;
			CurrentGraph = UsingExecutionGraph;
			RebuildGraphView();

			CurrentGraphIsSaved = false;
            UpdateCurrentFilePath("");

			return true;
		}

		// IGraphEditorActions interface method
		public bool TrySave()
        {
            if (CanSaveCurrentGraph == false)
                return TrySaveAs();

			SaveGraphToFile(CurrentGraphFilePath, EnableAutoSaveBackups);
			CurrentGraphIsSaved = true;
			UpdateWindowTitle();
			return true;
		}

		// IGraphEditorActions interface method
		public bool TryOpen()
        {
            string InitialPath = NodeEditorConfig.GetActiveSaveLoadPath();
			if (HostAPI != null && HostAPI.ShowBlockingOpenFileDialog(DefaultExtension, DefaultFileFilter, DefaultFileName, InitialPath, out string SelectedFilename))
			{
                if (OpenGraphFile(SelectedFilename))
                    return true;
			}
			return false;
		}

        // IGraphEditorActions interface method
        public bool TryImport()
        {
            string InitialPath = NodeEditorConfig.GetActiveSaveLoadPath();
            if (HostAPI != null && HostAPI.ShowBlockingOpenFileDialog(DefaultExtension, DefaultFileFilter, DefaultFileName, InitialPath, out string SelectedFilename)) {
                if (ImportGraphFromFile(SelectedFilename))
                    return true;
            }
            return false;
        }

        //! returns false if graph is unsaved, ie must do Save-As
        public bool CanSaveCurrentGraph { 
            get { return CurrentGraphFilePath.Length > 0 && File.Exists(CurrentGraphFilePath); } 
        }

		public bool OpenGraphFile(string Filename)
        {
			if (LoadGraphFromFile(Filename))
			{
				CurrentGraphIsSaved = true;
				UpdateCurrentFilePath(Filename);
                GlobalGraphOutput.AppendLine($"Loaded Graph from {Filename}", EGraphOutputType.Logging);
				return true;
			}
			GlobalGraphOutput.AppendLine($"Failed to load Graph from {Filename}", EGraphOutputType.Logging);
			return false;
		}


        public void SaveGraphToFile(string Filename, bool bAutoBackupExisting)
        {
            if ( bAutoBackupExisting && File.Exists(Filename))
            {
                for ( int i = 1; i < 9999; ++i )
                {
                    string baseFilename = Path.GetFileName(Filename);
                    string folder = Path.GetDirectoryName(Filename) ?? "./";
                    string BackupPath = Path.Combine(folder, "backups");
					if (Directory.Exists(BackupPath) == false)
						Directory.CreateDirectory(BackupPath);
					string BackupFilename = Path.Combine(BackupPath, string.Format("{0}.backup{1}", baseFilename, i));
                    if (File.Exists(BackupFilename) == false) {
                        File.Copy(Filename, BackupFilename);
                        break;
                    }
                }
            }

            try {
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    ExecutionGraphSerializer.SaveGraphOptions options = new ExecutionGraphSerializer.SaveGraphOptions();
                    options.LayoutProvider = CurrentGraphView;
                    options.AdditionalTags = new() {
                        { "ViewCenter",  ViewportBounds.Center.ToString() }
                    };
                    ExecutionGraphSerializer.Save(UsingExecutionGraph!, memoryStream, options);
                    memoryStream.Seek(0, SeekOrigin.Begin);
                    File.Delete(Filename);
                    using (FileStream fileStream = File.OpenWrite(Filename)) {
                        memoryStream.CopyTo(fileStream);
                    }

                    NodeEditorConfig.AddToRecentFiles(Filename);
                    NodeEditorConfig.SetLastFilePath(Path.GetDirectoryName(Filename) ?? "");

				}
            } catch (Exception e) {
                GlobalGraphOutput.AppendError($"ERROR SAVING GRAPH to {Filename} : {e.Message}");
            }
        }


        protected bool RestoreActiveGraphFromStream(Stream stream, bool bIsHotReload = false)
        {
            NodeLayoutCache layoutCache = new NodeLayoutCache();
            ExecutionGraphSerializer.RestoreGraphOptions options = new ExecutionGraphSerializer.RestoreGraphOptions() { LayoutProvider = layoutCache };
            options.AllRestoredTags = new();

            bool bRestoreOK = false;
            ExecutionGraph readGraph = new ExecutionGraph();
            try {
                bRestoreOK = ExecutionGraphSerializer.Restore(stream, readGraph, options);
            } catch (Exception) { throw; }

            UsingExecutionGraph = readGraph;
            UsingExecutionGraphEvaluator = new ExecutionGraphEvaluator(UsingExecutionGraph);
            UsingExecutionGraphEvaluator.EnableDebugPrinting = true;
            CurrentGraph = UsingExecutionGraph;

            RebuildGraphView();
            layoutCache.ApplyToGraphView(CurrentGraphView);

            // center on the loaded graph, or last view-center if it is saved in the graph file
            if (!bIsHotReload) {
                Action centerAction = () => { CenterOnCurrentGraph(); };
                Vector2d FocusPosition = CurrentGraphView.GetGraphBounds().Center;
                if ( options.AllRestoredTags?.TryGetValue("ViewCenter", out string? ViewCenterString) ?? false ) {
                    if ( Vector2d.TryParse(ViewCenterString, out FocusPosition) ) {
                        centerAction = () => { CenterAtViewportPosition((Vector2f)FocusPosition); };
                    }
                }
                InteractionManager.AddPendingNextFrameAction(centerAction);
            }

            return bRestoreOK;
        }

        public bool LoadGraphFromFile(string Filename)
        {
            try {
                using (FileStream fileStream = File.OpenRead(Filename))
                {
                    RestoreActiveGraphFromStream(fileStream);
					NodeEditorConfig.AddToRecentFiles(Filename);
					NodeEditorConfig.SetLastFilePath(Path.GetDirectoryName(Filename) ?? "");
					return true;
                }
            } catch (Exception e) {
                GlobalGraphOutput.AppendError($"ERROR LOADING GRAPH FROM {Filename} : {e.Message}");
            }
            return false;
        }


        public bool ImportGraphFromFile(string Filename)
        {
            bool bResult = false;
            try {
                string FileText = File.ReadAllText(Filename);
                ExecuteGraphEdit((NodeGraphEditor editor) => {
                    bResult = editor.TryImportGraphFromJson(FileText, out List<int>? NewNodeIDs);
                    if ( bResult && NewNodeIDs != null)
                        SelectionManager.SelectNodes(NewNodeIDs, true);
                });
            } catch (Exception e) {
                GlobalGraphOutput.AppendError($"ERROR IMPORTING GRAPH FROM {Filename} : {e.Message}");
            }
            return bResult;
        }


        public void CopySelectionToClipboard()
        {
            if (SelectionManager.HasNodeSelection == false) return;
            List<int> SelectedNodes = new List<int>(SelectionManager.CurrentNodeSelection);

            try {
                using (MemoryStream memStream = new MemoryStream()) {
                    ExecutionGraphSerializer.SaveGraphOptions options = new ExecutionGraphSerializer.SaveGraphOptions();
                    options.IncludeNodeFunc = (NodeBase node) => { return SelectedNodes.Contains(node.GraphIdentifier); };
                    options.LayoutProvider = CurrentGraphView;
                    ExecutionGraphSerializer.Save(UsingExecutionGraph!, memStream, options);
                    memStream.Seek(0, SeekOrigin.Begin);
                    string CopiedText = Encoding.UTF8.GetString(memStream.GetBuffer());
                    HostAPI?.SetSystemClipboardText(CopiedText);
                }
            } catch (Exception e) {
                GlobalGraphOutput.AppendError($"Error saving Graph Selection to Clipboard : {e.Message}");
            }
        }


        public void PasteFromClipboard()
        {
            string? clipboardText = HostAPI?.GetSystemClipboardText() ?? null;
            if (clipboardText != null)
                PasteGraphFromJson(clipboardText);
        }
        public void PasteGraphFromJson(string PastedText)
        {
            try {
                if ( ExecutionGraphSerializer.IsSerializedGraphJSon(PastedText) == false ) {
                    GlobalGraphOutput.AppendLog($"Pasted text was not identified as saved node graph data, ignoring");
                    return;
                }
                ExecuteGraphEdit((NodeGraphEditor editor) => {
                    bool bResult = editor.TryImportGraphFromJson(PastedText, out List<int>? NewNodeIDs);
                    if (bResult && NewNodeIDs != null) {
                        SelectionManager.SelectNodes(NewNodeIDs, true);

                        Vector2f PasteTranslation = new Vector2f(25, 25); 
                        foreach (int NodeID in SelectionManager.CurrentNodeSelection) {
                            NodeWidget? widget = CurrentGraphView.FindNode(NodeID);
                            if (widget != null)
                                widget.Position += PasteTranslation;
                        }
                    }
                });
            } catch (Exception e) {
                GlobalGraphOutput.AppendError($"Error pasting Clipboard as graph nodes : {e.Message}");
            }
        }


        public void AddNewFunction()
        {
            ExecuteGraphEdit((NodeGraphEditor editor) => {
                int NumFunctionNodes = 0;
                foreach (INodeInfo nodeInfo in CurrentGraph.EnumerateNodes()) {
                    if (nodeInfo.Node is FunctionDefinitionNode)
                        NumFunctionNodes++;
                }
                editor.AddNodeOfType(
                    new NodeType(typeof(FunctionDefinitionNode)), Vector2f.Zero, (INodeInfo nodeInfo) => {
                        (nodeInfo.Node as FunctionDefinitionNode)!.UpdateFunctionName($"NewFunction{NumFunctionNodes}");
                });
            });
        }


        public void PreDraw()
        {
			widgetScene.UpdateScene();
			widgetScene.UpdateLayout(styleCache);
			CurrentGraphView.UpdateLayout();
		}
        public void PostDraw()
        {
            InteractionManager.ProcessNextFrameActions();
        }

        public void Repaint(SKCanvas ViewportCanvas)
        {
            //ViewportCanvas.Clear(new SKColor(0xFFA0A0A0));
            ViewportCanvas.Clear(new SKColor(0xFF444444));

            WindowBounds = Conversion.FromSkia(ViewportCanvas.LocalClipBounds);
            ViewportBounds = new AxisAlignedBox2f(
                TransformWindowToViewport(WindowBounds.Min),
                TransformWindowToViewport(WindowBounds.Max));

            SKMatrix InitialMatrix = ViewportCanvas.TotalMatrix;
            SKMatrix ViewportTranslationMatrix = SKMatrix.CreateTranslation(this.ViewportTranslation.x, this.ViewportTranslation.y);
            SKMatrix ViewportScaleMatrix = SKMatrix.CreateScale(ViewportScale, ViewportScale);
            //SKMatrix CameraTransformMatrix = SKMatrix.Concat(ScaleMatrix, TranslationMatrix);
            SKMatrix CameraTransformMatrix = SKMatrix.Concat(ViewportTranslationMatrix, ViewportScaleMatrix);
            ViewportCanvas.SetMatrix(SKMatrix.Concat(InitialMatrix, CameraTransformMatrix));

            //widgetScene.UpdateScene();
            //widgetScene.UpdateLayout(styleCache);
            //CurrentGraphView.UpdateLayout();

            selectionManager.DrawViewport(ViewportCanvas);

            CurrentGraphView.Draw(ViewportCanvas);
            CurrentGraphView.DebugDraw(ViewportCanvas, InteractionManager.GetDeviceStateInSpace(InteractionManager.EInteractionSpace.GraphViewport).CurrentPosition );
			widgetScene.Draw(styleCache, ViewportCanvas);

            InteractionManager.Draw(ViewportCanvas);

            ViewportCanvas.ResetMatrix();

            SKMatrix UIScaleMatrix = SKMatrix.CreateScale(UIScale, UIScale);
            ViewportCanvas.SetMatrix(SKMatrix.Concat(InitialMatrix, UIScaleMatrix));

            selectionManager.DrawUI(ViewportCanvas);

            viewportUI.UpdateLayout(styleCache);
            viewportUI.Draw(styleCache, ViewportCanvas);

            // tooltip always draws on top...
            TooltipManager.Instance.Draw(ViewportCanvas,
                (Vector2f v) => { return TransformWindowToUI(v); });

            ViewportCanvas.ResetMatrix();

            // if debugging we continually repaint to show active node
            if (DebugManager.Instance.IsDebugging) {
                HostAPI?.RequestRepaint();
            }
        }



        protected void ConfigurePinBehavior(Widget? pinWidget)
        {
            if (pinWidget == null) return;
            ExtendableWidgetInputBehavior? fwdBehavior = (pinWidget.InputBehavior as ExtendableWidgetInputBehavior);
            if (fwdBehavior != null)
                fwdBehavior.ExtendedBehavior = this.InteractionManager.PinDrawConnectionBehavior;
        }
        public void ConfigureNewNodeWidget(NodeWidget newNodeWidget)
        {
            // configure InteractionManager derived behavior in output pins of new node
            foreach (NodeOutputPinWidget outputWidget in newNodeWidget.OutputWidgets)
            {
                //ExtendableWidgetInputBehavior? fwdBehavior = (outputWidget.InputBehavior as ExtendableWidgetInputBehavior);
                //if (fwdBehavior != null)
                //    fwdBehavior.ExtendedBehavior = this.InteractionManager.OutputPinInteraction;
                ConfigurePinBehavior(outputWidget);
            }

            ConfigurePinBehavior(newNodeWidget.OutputSequenceWidget);
        }
        private void CurrentGraphView_OnNewNodeAdded(object? sender, NodeWidget newNodeWidget)
        {
            ConfigureNewNodeWidget(newNodeWidget);
        }
        private void CurrentGraphView_OnExistingNodeUpdated(object? sender, NodeWidget newNodeWidget)
        {
            ConfigureNewNodeWidget(newNodeWidget);
        }
        private void CurrentGraphView_OnNodeWidgetModified(object? sender, NodeWidget newNodeWidget)
        {
            MarkGraphDirty();
        }



        // INodeGraphEditManager impl   
        // all graph edits go through this function  
        // (search)  BeginGraphEdit  BeginChange EditGraph
        public void ExecuteGraphEdit(Action<NodeGraphEditor> EditFunc)
        {
            GraphEditor.BeginGraphEdits(this.History);
            EditFunc(GraphEditor);
            GraphEditor.EndGraphEdits();

            // should run in task...
            GraphAnalysis.RebuildAll();

            MarkGraphDirty();
        }


        public bool InGraphEvaluation { get; private set; } = false;
        
        public async void RunGraphEvaluation()
        {
            if (InGraphEvaluation) {
                if (DebugManager.Instance.IsWaitingForStep)
                    DebugManager.Instance.SignalInteractiveStep();
                return;
            }

            InGraphEvaluation = true;

            foreach (NodeBase node in CurrentErrorStateNodes) 
                CurrentGraphView.FindNode(node.GraphIdentifier)?.ClearNodeErrorState();
            CurrentErrorStateNodes.Clear();

            List<Tuple<string, NodeBase?>> Errors = new List<Tuple<string, NodeBase?>>();
            ExecutionGraphEvaluator.EvaluationErrorEvent errorEvent = (string Error, NodeBase? ErrorAtNode) => Errors.Add(new(Error, ErrorAtNode));

            // invalidate before launching graph
            HostAPI?.RequestRepaint();

            OnGraphEvalStarted?.Invoke(this, EventArgs.Empty);

            Task<bool> GraphExecTask = Task.Run(() =>
            {
                if (UsingDataFlowGraphEvaluator != null)
                {
                    UsingDataFlowGraphEvaluator.EvaluateAllOutputs(null);
                }
                else if (UsingExecutionGraphEvaluator != null)
                {
                    UsingExecutionGraphEvaluator.EnableDebugging = NodeEditorRuntimeSettings.EnableDebugging.Value;
                    UsingExecutionGraphEvaluator.OnEvaluationErrorEvent += errorEvent;
                    UsingExecutionGraphEvaluator.EvaluateGraph();
                    UsingExecutionGraphEvaluator.OnEvaluationErrorEvent -= errorEvent;
                }
                return true;
            });

            // return to main thread, and then re-enter here when graph exec is done
            bool done_eval = await GraphExecTask;

            OnGraphEvalEnded?.Invoke(this, EventArgs.Empty);

            foreach (var errInfo in Errors) {
                if (errInfo.Item2 != null) {
                    CurrentGraphView.FindNode(errInfo.Item2.GraphIdentifier)?.SetNodeErrorState(new List<string> { errInfo.Item1 });
                    CurrentErrorStateNodes.Add(errInfo.Item2);
                }
            }

            InGraphEvaluation = false;
        }

        public void CancelGraphEvaluation()
        {
            if (!InGraphEvaluation)
                return;
            if (UsingExecutionGraphEvaluator != null) {
                UsingExecutionGraphEvaluator.PendingCancel = true;
            }
        }


        public static implicit operator WeakReference<object>(NodeGraphViewport v)
        {
            throw new NotImplementedException();
        }



        public void RebuildGraphLibraryWithActiveGraph()
        {
            // if the current graph is saved, it's safer to just reload it
            if ( File.Exists(CurrentGraphFilePath) && CurrentGraphIsSaved ) {
                DefaultNodeLibrary.ForceFullRebuild();
                LoadGraphFromFile(CurrentGraphFilePath);
                return;
            }

            // otherwise try serialize/deserialize, but this seems to break missing nodes, currently...

            MemoryStream savedStream = new MemoryStream();

            try {
                // serialize current graph
                ExecutionGraphSerializer.SaveGraphOptions options = new ExecutionGraphSerializer.SaveGraphOptions();
                options.LayoutProvider = CurrentGraphView;
                ExecutionGraphSerializer.Save(UsingExecutionGraph!, savedStream, options);
                savedStream.Seek(0, SeekOrigin.Begin);

                // do we need to clear current graph?

                // rebuild library
                DefaultNodeLibrary.ForceFullRebuild();

                RestoreActiveGraphFromStream(savedStream, true);
            } catch (Exception e) {
                GlobalGraphOutput.AppendError($"ERROR REBUILDING GRAPH LIBRARY : {e.Message}");
            }
        }

    }
}
