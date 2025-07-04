using SkiaSharp;
using System.Diagnostics;

using g3;
using Gradientspace.NodeGraph;
using Gradientspace.NodeGraph.Nodes;
using Gradientspace.NodeGraph.Geometry;
using Gradientspace.UI;
using Gradientspace.NodeGraph.CodeNodes;
using Gradientspace.NodeGraph.PythonNodes;

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
		protected GraphEditHistory EditHistory;

		protected EditorHostAPI? HostAPI;

        public void Initialize()
        {
            DebugManager.GlobalEnableGraphDebugging = true;
            GlobalGraphOutput.SetCurrentOutput(new DefaultGraphOutputImpl());

			GlobalGraphOutput.AppendLine($"Default User Files Path is {NodeEditorConfig.DefaultUserFilesPath}", EGraphOutputType.Logging);

            viewportUI = new NodeEditorViewportUI(this);

            // these just force assemblies to be loaded so that the nodes will show up in the library
            NodeGraphCoreLibrary.Initialize();
            NodeGraphGeometryLibrary.Initialize();
            GeometryViewerNodeLibrary.Initialize();
            //NodeGraphUnrealEngineLibrary.Initialize();
            //NodeGraphTestingLibrary.Initialize();
            GSPythonNodesLibrary.Initialize();

			//InlinePinWidgetSystem.Instance.RegisterProvider(
			//    typeof(SourceCodeDataType), new SourceCodeInlinePinWidgetProvider());
			NodeWidgetCustomizationSystem.Instance.RegisterProvider(
                typeof(CodeFunctionNode), new CodeFunctionNodeWidgetProvider());
			NodeWidgetCustomizationSystem.Instance.RegisterProvider(
				typeof(PythonFunctionCodeNode), new CodeFunctionNodeWidgetProvider());

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
            }

            widgetScene = new WidgetScene();
            styleCache = new SKStyleCache();

            CurrentGraphView = new NodeGraphView();
            CurrentGraphView.OnNewNodeAdded += CurrentGraphView_OnNewNodeAdded;
            CurrentGraphView.OnExistingNodeUpdated += CurrentGraphView_OnExistingNodeUpdated;

            selectionManager = new SelectionManager(this);
            InteractionManager = new InteractionManager(CurrentGraphView, this);
            widgetScene.AddSource(CurrentGraphView.WidgetSource);

            // populate graph view
            CurrentGraph.ValidateDataConnections();
            CurrentGraphView.ConnectToGraph(CurrentGraph);

            //GraphEditor = new NodeGraphEditor(CurrentGraphView, CurrentGraph);
            GraphEditor = new BaseGraphEditor(CurrentGraphView);
            CurrentGraphView.ActiveEditManager = new(this);
			EditHistory = new GraphEditHistory();
		}


        protected static DataFlowGraph MakeInitialDataflowGraph()
        {
            DataFlowGraph dataflowGraph = new DataFlowGraph();
            var Constant1 = dataflowGraph.AddNodeOfType<FloatConstantNode>();
            var Constant2 = dataflowGraph.AddNodeOfType<FloatConstantNode>();
            var Sum = dataflowGraph.AddNodeOfType<FloatAddNode>();
            var Sum2 = dataflowGraph.AddNodeOfType<FloatAddNode>();
            dataflowGraph.AddConnection(Constant1, FloatConstantNode.ValueOutputName, Sum, FloatAddNode.Operand1Name);
            dataflowGraph.AddConnection(Constant2, FloatConstantNode.ValueOutputName, Sum, FloatAddNode.Operand2Name);
            dataflowGraph.AddConnection(Sum, FloatAddNode.ValueOutputName, Sum2, FloatAddNode.Operand1Name);
            dataflowGraph.AddConnection(Constant2, FloatConstantNode.ValueOutputName, Sum2, FloatAddNode.Operand2Name);
            var Sink = dataflowGraph.AddNodeOfType<PrintFloatSinkNode>();
            dataflowGraph.AddConnection(Sum2, FloatConstantNode.ValueOutputName, Sink, PrintFloatSinkNode.InputName);

            dataflowGraph.SetNodeConstantValue(Constant1.Identifier, FloatConstantNode.ValueInputName, 2);
            dataflowGraph.SetNodeConstantValue(Constant2.Identifier, FloatConstantNode.ValueInputName, 3);
            dataflowGraph.SetNodeConstantValue(Sink.Identifier, PrintFloatSinkNode.FormatInputName, "Value is {0}");

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

            var Constant1 = executionGraph.AddNodeOfType<FloatConstantNode>();
            var Constant2 = executionGraph.AddNodeOfType<FloatConstantNode>();
            var Sum = executionGraph.AddNodeOfType<FloatAddNode>();
            var Sum2 = executionGraph.AddNodeOfType<FloatAddNode>();
            executionGraph.AddConnection(Constant1, FloatConstantNode.ValueOutputName, Sum, FloatAddNode.Operand1Name);
            executionGraph.AddConnection(Constant2, FloatConstantNode.ValueOutputName, Sum, FloatAddNode.Operand2Name);
            executionGraph.AddConnection(Sum, FloatAddNode.ValueOutputName, Sum2, FloatAddNode.Operand1Name);
            executionGraph.AddConnection(Constant2, FloatConstantNode.ValueOutputName, Sum2, FloatAddNode.Operand2Name);
            var Sink = executionGraph.AddNodeOfType<PrintFloatSinkNode>();
            executionGraph.AddConnection(Sum2, FloatConstantNode.ValueOutputName, Sink, PrintFloatSinkNode.InputName);

            executionGraph.AddSequenceConnection(Sum, "", Sum2, "");
            executionGraph.AddSequenceConnection(Sum2, "", Sink, "");
            executionGraph.AddSequenceConnection(executionGraph.StartNodeHandle, "", Sum, "");

            executionGraph.SetNodeConstantValue(Constant1.Identifier, FloatConstantNode.ValueInputName, 2);
            executionGraph.SetNodeConstantValue(Constant2.Identifier, FloatConstantNode.ValueInputName, 3);
            executionGraph.SetNodeConstantValue(Sink.Identifier, PrintFloatSinkNode.FormatInputName, "Value is {0}");

            return executionGraph;
        }


        public Vector2f ViewportTranslation { get; set; }
        public float ViewportScale { get; set; }
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

        public Vector2f TransformViewportToWindow(Vector2f DevicePoint)
        {
            return DevicePoint * ViewportScale + ViewportTranslation;
        }
        public Vector2f TransformWindowToViewport(Vector2f ViewportPoint)
        {
            return (ViewportPoint - ViewportTranslation) / ViewportScale;
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
            // There is a problem here with focus-change and search-text-type fields.
            // For example if the keyboard focus is on a text field that is being used to
            // dynamically filter a list (eg a popup of class names). If the user tries to
            // click down on the popup, and we do change-focus on click-down, it will 
            // take focus away from the textfield, which may then hide the popup, so
            // then it cannot be clicked. However doing it after is not entirely correct
            // either... seems like the right thing to do would be to do the capture request,
            // and then allow the text-entry-focus to look at the new capture target and
            // decide if it should lose focus or not? Or maybe router does this? Seems like
            // that is the only viable way to handle clicking inside the live text entry...

            //SystemKeyboardRouter.Instance.OnChangeWindowFocus(true);

            UpdateDeviceState(newState);

            bool bCaptured = InteractionManager.OnPointerDown(LastDeviceState);

            if (bCaptured)
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
            ViewportScale = Math.Clamp(ViewportScale + 0.25f * newState.WheelDelta, 0.1f, 10.0f);

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


        const string DefaultExtension = "json";
        const string DefaultFileFilter = "Node Graphs (*.json)|*.json|All files (*.*)|*.*";
        const string DefaultFileName = "nodegraph.json";
        const string DefaultDirectory = "c:\\scratch\\graphs\\";

        public string CurrentGraphFilePath { get; private set; } = "";
        public bool CurrentGraphIsSaved { get; private set; } = false;

        public bool OnKeyChordUpdated(in KeyChord ActiveChord)
        {
            bool bIsSave = ActiveChord.IsChord2(KeyNames.Ctrl, 'S');

            if (ActiveChord.IsSingleSpecialKey(KeyNames.Delete))
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


		static bool ShowCompactMode = false;

		// IGraphEditorActions interface method
		public bool TrySaveAs()
        {
            string configfile = NodeEditorConfig.UserConfigFilePath;

			string UseDirectory = NodeEditorConfig.GetActiveSaveLoadPath();
			string UseFilename = (CurrentGraphFilePath.Length == 0) ? DefaultFileName : Path.GetFileName(CurrentGraphFilePath)!;

			if (HostAPI != null && HostAPI.ShowBlockingSaveAsDialog(DefaultFileName, DefaultExtension, UseFilename, UseDirectory, out string SelectedFilename))
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
                    ExecutionGraphSerializer.Save(UsingExecutionGraph!, memoryStream, CurrentGraphView);
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

        public bool LoadGraphFromFile(string Filename)
        {
            try {
                ExecutionGraph readGraph = new ExecutionGraph();
                using (FileStream fileStream = File.OpenRead(Filename))
                {
                    NodeLayoutCache layoutCache = new NodeLayoutCache();
                    bool bOK = ExecutionGraphSerializer.Restore(fileStream, readGraph, layoutCache);

                    UsingExecutionGraph = readGraph;
                    UsingExecutionGraphEvaluator = new ExecutionGraphEvaluator(UsingExecutionGraph);
                    UsingExecutionGraphEvaluator.EnableDebugPrinting = true;
                    CurrentGraph = UsingExecutionGraph;

                    RebuildGraphView();
                    layoutCache.ApplyToGraphView(CurrentGraphView);

					NodeEditorConfig.AddToRecentFiles(Filename);
					NodeEditorConfig.SetLastFilePath(Path.GetDirectoryName(Filename) ?? "");

					return true;
                }
            } catch (Exception e) {
                GlobalGraphOutput.AppendError($"ERROR SAVING LOADING GRAPH FROM {Filename} : {e.Message}");
            }
            return false;
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



        // INodeGraphEditManager impl
        public void ExecuteGraphEdit(Action<NodeGraphEditor> EditFunc)
        {
            GraphEditor.BeginGraphEdits();
            EditFunc(GraphEditor);
            GraphEditor.EndGraphEdits();

            MarkGraphDirty();
        }


        public bool InGraphEvaluation { get; private set; } = false;
        
        public async void RunGraphEvaluation()
        {
            if (InGraphEvaluation) return;

            InGraphEvaluation = true;

            foreach (NodeBase node in CurrentErrorStateNodes) 
                CurrentGraphView.FindNode(node.GraphIdentifier)?.ClearNodeErrorState();
            CurrentErrorStateNodes.Clear();

            List<Tuple<string, NodeBase?>> Errors = new List<Tuple<string, NodeBase?>>();
            ExecutionGraphEvaluator.EvaluationErrorEvent errorEvent = (string Error, NodeBase? ErrorAtNode) => Errors.Add(new(Error, ErrorAtNode));

            // invalidate before launching graph
            HostAPI?.RequestRepaint();

			Task<bool> GraphExecTask = Task.Run(() =>
            {
                if (UsingDataFlowGraphEvaluator != null)
                {
                    UsingDataFlowGraphEvaluator.EvaluateAllOutputs(null);
                }
                else if (UsingExecutionGraphEvaluator != null)
                {
                    UsingExecutionGraphEvaluator.EnableDebugging = Settings.EnableDebugging.Value;
                    UsingExecutionGraphEvaluator.OnEvaluationErrorEvent += errorEvent;
                    UsingExecutionGraphEvaluator.EvaluateGraph();
                    UsingExecutionGraphEvaluator.OnEvaluationErrorEvent -= errorEvent;
                }
                return true;
            });

            // return to main thread, and then re-enter here when graph exec is done
            bool done_eval = await GraphExecTask;

            foreach (var errInfo in Errors) {
                if (errInfo.Item2 != null) {
                    CurrentGraphView.FindNode(errInfo.Item2.GraphIdentifier)?.SetNodeErrorState(new List<string> { errInfo.Item1 });
                    CurrentErrorStateNodes.Add(errInfo.Item2);
                }
            }

            InGraphEvaluation = false;
        }

        public static implicit operator WeakReference<object>(NodeGraphViewport v)
        {
            throw new NotImplementedException();
        }
    }
}
