// Copyright Gradientspace Corp. All Rights Reserved.
using g3;
using Gradientspace.NodeGraph;
using Gradientspace.UI;
using SkiaSharp;
using System.Collections.Generic;
using System.Diagnostics;


namespace GSNodeEditor
{
    public class InteractionManager : IPanViewTarget, IContextMenuProviderTarget, IPinCaptureTarget
    {
        public NodeGraphView GraphView { get; set; }
        public NodeGraphViewport GraphViewport { get; set; }

        public bool EnableCaptureDebugging = false;

        NewNodePopupDialog? ActiveNewNodePopupDialog = null;
        SimplePopupMenuDialog? ActiveNodePopupDialog = null;
		Widget? ActivePopupDialog = null;
		SimpleWidgetSource ActivePopupMenuWidgetSet;

        Action? PendingNextFrameAction {
            get;
            set;
        } = null;

        InputBehaviorSet InteractionBehaviors = new InputBehaviorSet();
        InputBehaviorCollectionSet ViewportInteractionSets = new InputBehaviorCollectionSet();
        InputBehaviorCollectionSet UIInteractionSets = new InputBehaviorCollectionSet();

        //public OutputPinExtendedBehavior OutputPinInteraction { get; set; }
        public PinExtendedBehavior PinDrawConnectionBehavior { get; set; }
        public GraphViewClutchKeyInputBehaviors ClutchKeyInputBehaviors { get; set; }
        public GraphViewContextMenuBehavior ContextMenuBehavior { get; set; }
        public NodeSelectionInputBehavior SelectionInputBehavior { get; set; }

        // options
        public bool AutoReplaceExistingConnections { get; set; } = true;
        public bool AutoConnectSequencePath { get; set; } = true;

        public bool ShowConnectionPreviewTypes { get; set; } = true;

        public enum EInteractionState
        {
            NoInteraction,
            PanViewport,

            DragNode,
            DrawConnection,

            NewNodePopupMenu,

            CtrlInteraction,

            UnknownInteraction
        }
        protected EInteractionState interactionState;
        
        public InteractionManager(NodeGraphView graphView, NodeGraphViewport viewport)
        {
            GraphView = graphView;
            GraphViewport = viewport;

            interactionState = EInteractionState.NoInteraction;

            ActivePopupMenuWidgetSet = new SimpleWidgetSource();

            panViewInputBehavior = new PanViewInputBehavior(this);
            InteractionBehaviors.AddBehavior(panViewInputBehavior);
            panViewInputBehavior.OnClickedEvent += RequestShowContextMenu;

            ClutchKeyInputBehaviors = new GraphViewClutchKeyInputBehaviors(GraphViewport.WidgetScene, GraphViewport);
            InteractionBehaviors.AddBehavior(ClutchKeyInputBehaviors);

            SelectionInputBehavior = new NodeSelectionInputBehavior(GraphViewport.SelectionManager);
            InteractionBehaviors.AddBehavior(SelectionInputBehavior);

            ViewportInteractionSets.AddCollection(InteractionBehaviors);
            ViewportInteractionSets.AddCollection(GraphViewport.WidgetScene);

            UIInteractionSets.AddCollection(GraphViewport.ViewportUI.WidgetScene);

            //OutputPinInteraction = new OutputPinExtendedBehavior(this);
            PinDrawConnectionBehavior = new PinExtendedBehavior(this);
        }

        public EInteractionState InteractionState { get { return interactionState; } }
        public bool IsCapturingInput { get { return interactionState != EInteractionState.NoInteraction || ActiveDeviceCapture != InputCaptureRequest.None; } }
        public InputCaptureRequest ActiveCaptureRequest { get { return ActiveDeviceCapture; } }


        // basic

        public enum EInteractionSpace
        {
            UILayer = 0,
            GraphViewport = 1
        }

        // last device state provided to us, is always in untransformed space
        InputDeviceState lastDeviceState;

        InputCaptureRequest ActiveDeviceCapture;
        InputCaptureRequest ActiveHoverCapture;
        EInteractionSpace ActiveCaptureSpace = EInteractionSpace.GraphViewport;

        // map current InputDeviceState into the specified interaction space
        public InputDeviceState GetDeviceStateInSpace(EInteractionSpace space)
        {
            InputDeviceState result = lastDeviceState;
            result.DebugState = (int)space;
            if (space == EInteractionSpace.GraphViewport)
                result.CurrentPosition = GraphViewport.TransformWindowToViewport(result.CurrentPosition);
            else
                result.CurrentPosition = GraphViewport.TransformWindowToUI(result.CurrentPosition);
            return result;
        }

        //! returns InputDeviceState in the correct transform space for the active hover/capture
        public InputDeviceState CaptureDeviceState {
            get { 
                return GetDeviceStateInSpace(ActiveCaptureSpace);
            }
        }



        // PanViewport behavior implementation

        Vector2f InitialViewportTranslation;
        Vector2f InitialPanPosition;
        public void BeginPan(in InputDeviceState initialPosition)
        {
            interactionState = EInteractionState.PanViewport;
            InitialViewportTranslation = GraphViewport.ViewportTranslation;
            InitialPanPosition = GraphViewport.TransformViewportToWindow(CaptureDeviceState.CurrentPosition);
        }
        public void UpdatePan(in InputDeviceState newPosition)
        {
            Vector2f CurPanPosition = GraphViewport.TransformViewportToWindow(CaptureDeviceState.CurrentPosition);
            Vector2f NewTranslation = InitialViewportTranslation + (CurPanPosition-InitialPanPosition);
            GraphViewport.ViewportTranslation = NewTranslation;
        }
        public void EndPan(in InputDeviceState finalPosition)
        {
            interactionState = EInteractionState.NoInteraction;
        }
        PanViewInputBehavior panViewInputBehavior;


        // DrawConnection behavior implementation

        enum EDrawConnectionType
        {
            FromOutputPin,
            FromInputPin,
            FromOutputSequencePin,
            FromInputSequencePin
        }
        NodeAndPin? ActiveDrawConnectionFrom = null;
        NodeAndPin? ActiveDrawConnectionTo = null;
        NodeAndPin? ActiveDrawConnectionToHover = null;
        EDrawConnectionType ActiveDrawConnectionType = EDrawConnectionType.FromOutputPin;

        Vector2f ConnectionStartPosition;
        Vector2f ConnectionEndPosition;

        void CleanupActivePinCapture()
        {
            ActiveDrawConnectionFrom = ActiveDrawConnectionTo = ActiveDrawConnectionToHover = null;
            interactionState = EInteractionState.NoInteraction;
        }


        bool ArePinTypesCompatible(GraphDataType outputType, GraphDataType inputType)
        {
            if (outputType.CSType == inputType.CSType && inputType.IsDynamic == false )     // what is this IsDynamic check accomplishing?
                return true;
            return GraphView.GetGraph().CanConnectTypes(outputType, inputType);
        }

        public void BeginPinCapture(in InputDeviceState deviceState, NodePinWidget pinWidget)
        {
            NodeWidget FromWidget = (pinWidget.ParentWidget as NodeWidget)!;

            if (pinWidget is NodeOutputPinWidget)
            {
                NodeOutputPinWidget outputPinWidget = ((NodeOutputPinWidget)pinWidget)!;
                int PinIndex = FromWidget.FindOutputPinIndexByName(outputPinWidget.OutputName);
                bool bIsControlFlowPin = (outputPinWidget.DataType.CSType == typeof(ControlFlowOutputID));
                ActiveDrawConnectionFrom = new NodeAndPin(FromWidget, outputPinWidget, PinIndex, false) { bIsSequencePin = false };
                ConnectionStartPosition = ActiveDrawConnectionFrom.Node.GetOutputPinConnectionPoint(ActiveDrawConnectionFrom.PinIndex);
                ActiveDrawConnectionType = (bIsControlFlowPin) ? EDrawConnectionType.FromOutputSequencePin : EDrawConnectionType.FromOutputPin;
            }
            else if (pinWidget is NodeOutputExecPinWidget)
            {
                ActiveDrawConnectionFrom = new NodeAndPin(FromWidget, pinWidget, -1, false) { bIsSequencePin = true };
                ConnectionStartPosition = FromWidget.GetOutputSequencePinConnectionPoint();
                ActiveDrawConnectionType = EDrawConnectionType.FromOutputSequencePin;
            }
            if (ActiveDrawConnectionFrom != null)
            {
                ConnectionEndPosition = deviceState.CurrentPosition;
                interactionState = EInteractionState.DrawConnection;
            }
        }
        public void UpdatePinCapture(in InputDeviceState deviceState)
        {
            ActiveDrawConnectionTo = null;
            ActiveDrawConnectionToHover = null;
            ConnectionEndPosition = deviceState.CurrentPosition;

            Type WidgetHitType = typeof(NodeInputPinWidget);
            if (ActiveDrawConnectionType == EDrawConnectionType.FromOutputSequencePin)
                WidgetHitType = typeof(NodeInputExecPinWidget);

            bool bFoundHit = GraphViewport.WidgetScene.HitQuery(deviceState.CurrentPosition, out var hitResult,
                (Widget w) => { return w.GetType() == WidgetHitType; });
            if (!bFoundHit)
                return;

            if (ActiveDrawConnectionType == EDrawConnectionType.FromInputPin || ActiveDrawConnectionType == EDrawConnectionType.FromOutputPin)
                UpdatePinCapture_DataPin(deviceState, hitResult);
            else
                UpdatePinCapture_SequencePin(deviceState, hitResult);

        }
        protected void UpdatePinCapture_DataPin(in InputDeviceState deviceState, in WidgetHitResult hitResult)
        {
            NodeInputPinWidget? hitInputPinWidget = (hitResult.HitWidget as NodeInputPinWidget);
            if (hitInputPinWidget != null)
            {
                if (hitInputPinWidget.NodeInputInfo.IsNodeConstant)     // cannot connect to node-constant pins
                    return;

                NodeWidget nodeWidget = (hitInputPinWidget.ParentWidget as NodeWidget)!;

                int InputPinIndex = nodeWidget.FindInputPinIndexByName(hitInputPinWidget.InputName);
                ActiveDrawConnectionToHover = new NodeAndPin(nodeWidget, hitInputPinWidget, InputPinIndex, true);
                ConnectionEndPosition = ActiveDrawConnectionToHover.Node.GetInputPinConnectionPoint(InputPinIndex);

                if (ArePinTypesCompatible(ActiveDrawConnectionFrom!.DataType, hitInputPinWidget.DataType)) {
                    ActiveDrawConnectionTo = new NodeAndPin(nodeWidget, hitInputPinWidget, InputPinIndex, true);
                }
            }

        }
        protected void UpdatePinCapture_SequencePin(in InputDeviceState deviceState, in WidgetHitResult hitResult)
        {
            NodeInputExecPinWidget? hitInputPinWidget = (hitResult.HitWidget as NodeInputExecPinWidget);
            if (hitInputPinWidget != null)  // && VALIDATE_CONNECTION
            {
                NodeWidget Node = (hitInputPinWidget.ParentWidget as NodeWidget)!;
                ActiveDrawConnectionTo = new NodeAndPin(Node, hitInputPinWidget, -1, true) { bIsSequencePin = true };
                ConnectionEndPosition = ActiveDrawConnectionTo.Node.GetInputSequencePinConnectionPoint();
            }
        }
        public void EndPinCapture(in InputDeviceState deviceState)
        {
            if (ActiveDrawConnectionType == EDrawConnectionType.FromInputPin || ActiveDrawConnectionType == EDrawConnectionType.FromOutputPin)
                EndPinCapture_DataPin(deviceState);
            else
                EndPinCapture_SequencePin(deviceState);
        }
        protected void EndPinCapture_DataPin(in InputDeviceState deviceState)
        {
            if (ActiveDrawConnectionTo != null)
            {
                // todo some kind of validity checking..
                GraphViewport.ExecuteGraphEdit((NodeGraphEditor Editor) =>
                {
                    Editor.AddConnection(
                        ActiveDrawConnectionFrom!.Node, ActiveDrawConnectionFrom.PinIndex,
                        ActiveDrawConnectionTo.Node, ActiveDrawConnectionTo.PinIndex, EConnectionType.Data,
                        AutoReplaceExistingConnections, AutoConnectSequencePath);
                });
                CleanupActivePinCapture();
            }
            else
            {
                ConnectionEndPosition = deviceState.CurrentPosition;
                NodeAndPin? FromNodeCopy = ActiveDrawConnectionFrom;     // required so that lambda doesn't access class value later
                if (ActiveDrawConnectionToHover == null)
                    PendingNextFrameAction = () => { BeginShowNewNodePopupMenu(FromNodeCopy); };

                CleanupActivePinCapture();
            }
        }
        protected void EndPinCapture_SequencePin(in InputDeviceState deviceState)
        {
            if (ActiveDrawConnectionTo != null)
            {
                GraphViewport.ExecuteGraphEdit((NodeGraphEditor Editor) =>
                {
                    Editor.AddConnection(
                        ActiveDrawConnectionFrom!.Node, ActiveDrawConnectionFrom.PinIndex,
                        ActiveDrawConnectionTo.Node, ActiveDrawConnectionTo.PinIndex, EConnectionType.Sequence,
                        AutoReplaceExistingConnections, AutoConnectSequencePath);
                });
                CleanupActivePinCapture();
            }
            else
            {
                ConnectionEndPosition = deviceState.CurrentPosition;
                NodeAndPin? FromNodeCopy = ActiveDrawConnectionFrom;     // required so that lambda doesn't access class value later
                if (ActiveDrawConnectionToHover == null)
                    PendingNextFrameAction = () => { BeginShowNewNodePopupMenu(FromNodeCopy); };
                CleanupActivePinCapture();
            }
        }
        public void AbortPinCapture()
        {
            CleanupActivePinCapture();
        }







        public bool OnPointerDown(InputDeviceState deviceState)
        {
            Debug.Assert(ActiveDeviceCapture == InputCaptureRequest.None);
            ActiveDeviceCapture = InputCaptureRequest.None;
            interactionState = EInteractionState.NoInteraction;

            lastDeviceState = deviceState;

            EndActiveHover();

            EInteractionSpace captureSpace = EInteractionSpace.UILayer;
            InputCaptureRequest captureRequest = UIInteractionSets.CheckForDeviceCapture( GetDeviceStateInSpace(EInteractionSpace.UILayer) );
            if (captureRequest == InputCaptureRequest.None)
            {
                captureRequest = ViewportInteractionSets.CheckForDeviceCapture( GetDeviceStateInSpace(EInteractionSpace.GraphViewport) );
                captureSpace = EInteractionSpace.GraphViewport;
            }

            if (captureRequest == InputCaptureRequest.None) 
                return false;

            interactionState = EInteractionState.UnknownInteraction;
            ActiveCaptureSpace = captureSpace;

            ActiveDeviceCapture = captureRequest;
            ActiveDeviceCapture.SourceBehavior?.BeginCapture(CaptureDeviceState, captureRequest);
            if (EnableCaptureDebugging) {
                Debug.WriteLine("InterationManager: {0} has started capture at priority {1} zdepth {2} - source object {3}", captureRequest.SourceBehavior!.GetType().Name, captureRequest.Priority, captureRequest.ZDepth,
                    captureRequest.SourceObject?.GetType()?.ToString() ?? "(null)");
            }
            return true;
        }

        public void OnPointerCaptureMove(InputDeviceState deviceState)
        {
            lastDeviceState = deviceState;

            if (ActiveDeviceCapture != InputCaptureRequest.None)
                ActiveDeviceCapture.SourceBehavior?.UpdateCapture(CaptureDeviceState);
        }

        public void OnPointerUp(InputDeviceState deviceState)
        {
            lastDeviceState = deviceState;
            if (ActiveDeviceCapture != InputCaptureRequest.None)
            {
                ActiveDeviceCapture.SourceBehavior?.EndCapture(CaptureDeviceState);
                ActiveDeviceCapture = InputCaptureRequest.None;
                interactionState = EInteractionState.NoInteraction;
            }
        }

        public void OnAbortInteraction()
        {
            if (ActiveDeviceCapture != InputCaptureRequest.None)
            {
                ActiveDeviceCapture.SourceBehavior?.AbortCapture();
                ActiveDeviceCapture = InputCaptureRequest.None;
                interactionState = EInteractionState.NoInteraction;
            }
        }


        public void OnPointerHoverMove(InputDeviceState deviceState)
        {
            lastDeviceState = deviceState;

            EInteractionSpace captureSpace = EInteractionSpace.UILayer;
            InputCaptureRequest HoverRequest = UIInteractionSets.CheckForHoverCapture( GetDeviceStateInSpace(EInteractionSpace.UILayer) );
            if (HoverRequest == InputCaptureRequest.None) {
                HoverRequest = ViewportInteractionSets.CheckForHoverCapture( GetDeviceStateInSpace(EInteractionSpace.GraphViewport) );
                captureSpace = EInteractionSpace.GraphViewport;
            }

            if (ActiveHoverCapture != HoverRequest)
            {
                EndActiveHover();
                if ( HoverRequest != InputCaptureRequest.None ) {

                    ActiveCaptureSpace = captureSpace;
                    HoverRequest.SourceBehavior?.BeginHover(CaptureDeviceState);
                    ActiveHoverCapture = HoverRequest;

                    if (HoverRequest.SourceObject is Widget)
                    {
                        Widget w = (Widget)HoverRequest.SourceObject;
                        AxisAlignedBox2f Bounds = w.GetActiveView()?.BoundsQuery(w.GetAnchor()) ?? new AxisAlignedBox2f();
                        Bounds.Min = MapWidgetPointToWindowCoords(w, Bounds.Min);
                        Bounds.Max = MapWidgetPointToWindowCoords(w, Bounds.Max);   // todo this searches twice!
                        TooltipManager.Instance.SetActiveTooltipSource((Widget)HoverRequest.SourceObject, Bounds, lastDeviceState);
                    }
                }
            }
            else if ( ActiveHoverCapture != InputCaptureRequest.None )
            {
                bool bContinueHover = false;
                ActiveHoverCapture.SourceBehavior?.UpdateHover(CaptureDeviceState, out bContinueHover);
                if (bContinueHover == false)
                {
                    EndActiveHover();
                }
            }

            TooltipManager.Instance.OnUpdateDeviceState(lastDeviceState);
        }
        protected void EndActiveHover()
        {
            if (ActiveHoverCapture != InputCaptureRequest.None )
            {
                ActiveHoverCapture.SourceBehavior?.EndHover(CaptureDeviceState);
                ActiveHoverCapture = InputCaptureRequest.None;
                TooltipManager.Instance.ClearActiveTooltipSource();
            }
        }




        protected Vector2f MapWidgetPointToWindowCoords(Widget w, Vector2f point)
        {
            if (GraphViewport.WidgetScene.ContainsWidget(w))
                return GraphViewport.TransformViewportToWindow(point);
            else if (GraphViewport.ViewportUI.WidgetScene.ContainsWidget(w))
                return GraphViewport.TransformUIToWindow(point);
            return point;
        }




        public void Draw(SKCanvas Canvas)
        {
            if (interactionState == EInteractionState.DrawConnection)
            {
                // yikes, this is a mess

                var ValidConnectionCurvePaint = new SKPaint {
                    Style = SKPaintStyle.Stroke,
                    StrokeWidth = 3,
                    IsAntialias = true,
                    Color = SKColors.Orange
                };
                var InvalidConnectionCurvePaint = new SKPaint {
                    Style = SKPaintStyle.Stroke,
                    StrokeWidth = 1,
                    IsAntialias = true,
                    Color = SKColors.Red
                };

                bool bHaveInvalidConection = (ActiveDrawConnectionTo == null && ActiveDrawConnectionToHover != null);

                SKPaint UseCurvePaint = bHaveInvalidConection ? InvalidConnectionCurvePaint : ValidConnectionCurvePaint;

                SKPath Curve = new SKPath();
                Curve.MoveTo(Conversion.ToSkia(ConnectionStartPosition));

                float UseTangentLen = GraphView.ConnectionTangentLen;
                float Distance = ConnectionStartPosition.Distance(ConnectionEndPosition);
                if (Distance < 2 * UseTangentLen) {
                    UseTangentLen = Distance / 2;
                }

                Vector2f StartTangent = ConnectionStartPosition + UseTangentLen * Vector2f.AxisX;
                Vector2f EndTangent = ConnectionEndPosition - UseTangentLen * Vector2f.AxisX;
                Curve.CubicTo(Conversion.ToSkia(StartTangent), Conversion.ToSkia(EndTangent), Conversion.ToSkia(ConnectionEndPosition));
                Canvas.DrawPath(Curve, UseCurvePaint);

                SKPaint DataTypeTextPaint = new SKPaint
                {
                    Color = SKColors.White,
                    IsAntialias = true, LcdRenderText = true, SubpixelText = true,
                    TextSize = 12
                    //Typeface = SKTypeface.FromFamilyName(
                    //    familyName: style.FontName,
                    //    weight: SKFontStyleWeight.Normal, width: SKFontStyleWidth.Normal, slant: SKFontStyleSlant.Upright)
                };
                SKPaint ValidDataTypeFillPaint = new SKPaint
                {
                    Color = SKColors.Black
                };
                SKPaint InvalidDataTypeFillPaint = new SKPaint
                {
                    Color = SKColors.DarkRed
                };
                SKPaint UseFillPaint = (bHaveInvalidConection) ? InvalidDataTypeFillPaint : ValidDataTypeFillPaint;

                SKPaint WarningDataTypeFillPaint = new SKPaint
                {
                    Color = SKColors.DarkOrange
                };
                bool bHaveLossyConversion = (ActiveDrawConnectionFrom != null && ActiveDrawConnectionTo != null &&
                    ActiveDrawConnectionFrom.bIsSequencePin == false && ActiveDrawConnectionTo.bIsSequencePin == false &&
                    TypeUtils.IsLossyNumericConversion(ActiveDrawConnectionFrom.DataType.CSType, ActiveDrawConnectionTo.DataType.CSType));
                if (bHaveLossyConversion)
                    UseFillPaint = WarningDataTypeFillPaint;

                const float Margin = 3;

                TextHeightInfo DataTypeTextHeightInfo = SKStyleCache.MeasureTextHeightInfo(DataTypeTextPaint);
                if (ShowConnectionPreviewTypes && ActiveDrawConnectionFrom != null && ActiveDrawConnectionFrom.bIsSequencePin == false)
                {
                    string TypeText = ActiveDrawConnectionFrom.Pin.GetDataTypeAsString();
                    SKRect Bounds = SKRect.Empty;
                    float Width = DataTypeTextPaint.MeasureText(TypeText, ref Bounds);
                    Bounds.Left -= (Margin+2); Bounds.Right += (Margin+1); Bounds.Bottom += Margin; Bounds.Top -= Margin;
                    SKMatrix CurMatrix = Canvas.TotalMatrix;
                    SKPoint Offset = Conversion.ToSkia(ConnectionStartPosition + new Vector2f(0, -8));
                    Canvas.Translate(Offset);
                    Canvas.DrawRoundRect(Bounds, 8.0f, 8.0f, UseFillPaint);
                    Canvas.DrawText(TypeText, new SKPoint(0,0), DataTypeTextPaint);
                    Canvas.SetMatrix(CurMatrix);
                }
                if (ShowConnectionPreviewTypes && ActiveDrawConnectionToHover != null && ActiveDrawConnectionToHover.bIsSequencePin == false)
                {
                    string TypeText = ActiveDrawConnectionToHover.Pin.GetDataTypeAsString();
                    SKRect Bounds = SKRect.Empty;
                    float Width = DataTypeTextPaint.MeasureText(TypeText, ref Bounds);
                    Bounds.Left -= (Margin + 2); Bounds.Right += (Margin + 1); Bounds.Bottom += Margin; Bounds.Top -= Margin;
                    SKMatrix CurMatrix = Canvas.TotalMatrix;
                    SKPoint Offset = Conversion.ToSkia(ConnectionEndPosition + new Vector2f(-Width, 8+DataTypeTextHeightInfo.MaxTotalHeight));
                    Canvas.Translate(Offset);
                    Canvas.DrawRoundRect(Bounds, 8.0f, 8.0f, UseFillPaint);
                    Canvas.DrawText(TypeText, new SKPoint(0, 0), DataTypeTextPaint);
                    Canvas.SetMatrix(CurMatrix);
                }
            }

            //ProcessNextFrameActions();
		}


        public void ProcessNextFrameActions()
        {
			if (interactionState == EInteractionState.NoInteraction && PendingNextFrameAction != null)
			{
				PendingNextFrameAction();
				PendingNextFrameAction = null;
			}
		}





        public void BeginShowNewNodePopupMenu(NodeAndPin? FromNodeAndPin = null)
        {
            DismissActivePopupDialogs();

            Vector2f UIPopupLocation = GetDeviceStateInSpace(EInteractionSpace.UILayer).CurrentPosition;
            Vector2f ViewportPopupLocation = GetDeviceStateInSpace(EInteractionSpace.GraphViewport).CurrentPosition;

            ActiveNewNodePopupDialog = new NewNodePopupDialog();
            ActivePopupDialog = ActiveNewNodePopupDialog;
            ActiveNewNodePopupDialog.Position = UIPopupLocation;
            ActiveNewNodePopupDialog.OnDismissDialogClick = () => { DismissActivePopupDialogs(); };
            ActiveNewNodePopupDialog.PopulateNodeLibrary(DefaultNodeLibrary.Instance, FromNodeAndPin);
            ActiveNewNodePopupDialog.OnNewNodeTypeSelected += (NewNodePopupDialog dialog, NodeType nodeType) => {
                OnNewNodePopupItemSelected(nodeType, ViewportPopupLocation, FromNodeAndPin);
            };
            ActiveNewNodePopupDialog.PopulateVariables(GraphViewport.GraphAnalysis, FromNodeAndPin);
            ActiveNewNodePopupDialog.PopulateFunctions( (GraphView.GetGraph() as ExecutionGraph)!, FromNodeAndPin);
            ActiveNewNodePopupDialog.OnGetSetVariableSelected += (NewNodePopupDialog dialog, VariablesTracker.VariableInfo varInfo, bool bSet) => {
                OnGetSetVariableSelected(varInfo, bSet, ViewportPopupLocation, FromNodeAndPin);
            };
            ActiveNewNodePopupDialog.OnNewVariableSelected += (NewNodePopupDialog dialog, NodeAndPin? nodeAndPin, int type) => {
                // note: do not use outer FromNodeAndPin here, the event sends null for types that can't be used as a variable
                OnNewVariableSelected(ViewportPopupLocation, nodeAndPin, type);
            };
            ActiveNewNodePopupDialog.OnCreateFunctionCallSelected += (NewNodePopupDialog dialog, FunctionDefinitionNode funcNode) => {
                OnNewFunctionCallSelected(ViewportPopupLocation, FromNodeAndPin, funcNode);
            };

            ActivePopupMenuWidgetSet.AddRootWidget(ActiveNewNodePopupDialog);

            //GraphViewport.WidgetScene.AddSource(ActivePopupMenuWidgetSet);
            GraphViewport.ViewportUI.WidgetScene.AddSource(ActivePopupMenuWidgetSet);

            ActiveNewNodePopupDialog.GiveFocusToSearchBox();
            SystemKeyboardRouter.Instance.PushHotkeyTarget(ActiveNewNodePopupDialog);

            //interactionState = EInteractionState.NewNodePopupMenu;
        }

        protected void OnNewNodePopupItemSelected(NodeType nodeType, Vector2f Location, NodeAndPin? FromNode)
        {
            PendingNextFrameAction = () => { AppendNewNodeAtLocation(nodeType, Location, FromNode); };
            DismissActivePopupDialogs();
        }


        protected void OnGetSetVariableSelected(VariablesTracker.VariableInfo varInfo, bool bSet, Vector2f Location, NodeAndPin? FromNode)
        {
            PendingNextFrameAction = () => {
                Type useNodeType = (bSet) ? typeof(SetGlobalVariableNode) : typeof(GetGlobalVariableNode);
                NodeWidget? NewWidget = AppendNewNodeAtLocation(
                    new NodeType(useNodeType), Location, FromNode,
                    (INodeInfo nodeInfo) => {
                        if (nodeInfo.Node is AccessVariableNode varNode)
                            varNode.Initialize(varInfo.Name, varInfo.VariableType, true);
					});

            };
            DismissActivePopupDialogs();
        }

        protected void OnNewVariableSelected(Vector2f Location, NodeAndPin? FromNode, int type)
        {
			PendingNextFrameAction = () => {
                Type useNodeType = typeof(CreateGlobalVariableNode);
				NodeWidget? NewWidget = AppendNewNodeAtLocation(
					new NodeType(useNodeType), Location, FromNode,
					(INodeInfo nodeInfo) => {
						if (nodeInfo.Node is CreateGlobalVariableNode varNode && FromNode != null)
                            varNode.Initialize(FromNode.Pin.DataType.CSType);
					});

			};
            DismissActivePopupDialogs();
		}


        protected void OnNewFunctionCallSelected(Vector2f Location, NodeAndPin? FromNode, FunctionDefinitionNode funcNode)
        {
            PendingNextFrameAction = () => {
                NodeWidget? NewWidget = AppendNewNodeAtLocation(
                    new(typeof(FunctionCallNode)), Location, FromNode,
                    (INodeInfo nodeInfo) => {
                        if (nodeInfo.Node is FunctionCallNode callNode)
                            callNode.LinkToFunction(funcNode);
                    });
            };
            DismissActivePopupDialogs();
        }


		protected void DismissActivePopupDialogs()
        {
            if (ActivePopupDialog == null)
                return;

            //GraphViewport.WidgetScene.RemoveSource(ActivePopupMenuWidgetSet);
            GraphViewport.ViewportUI.WidgetScene.RemoveSource(ActivePopupMenuWidgetSet);
            ActivePopupMenuWidgetSet.Clear();
            ActivePopupDialog = null;

			// this should work but it doesn't update something in widgetscene...
			//if (ActiveNewNodePopupDialog != null)
			//    ActivePopupMenuWidgetSet.RemoveRootWidget(ActiveNewNodePopupDialog);

			//interactionState = EInteractionState.NoInteraction;

			// somehow need to handle the case where we remove widget that is actively capturing...
			EndActiveHover();

            if (ActiveNewNodePopupDialog != null) {
                SystemKeyboardRouter.Instance.PopHotkeyTarget(ActiveNewNodePopupDialog!);
                ActiveNewNodePopupDialog = null;
            }
            if (ActiveNodePopupDialog != null) {
				SystemKeyboardRouter.Instance.PopHotkeyTarget(ActiveNodePopupDialog!);
				ActiveNodePopupDialog = null; 
			}
        }



        protected NodeWidget? AppendNewNodeAtLocation(
            NodeType nodeType, 
            Vector2f Postion, 
            NodeAndPin? FromNode = null,
            Action<INodeInfo>? NodeInitializerFunc = null)
        {
            NodeWidget? NewNode = null;
            GraphViewport.ExecuteGraphEdit((NodeGraphEditor Editor) =>
            {
                NewNode = Editor.AddNodeOfType(nodeType, Postion, NodeInitializerFunc);

                if (FromNode != null)
                {
                    // TODO: support automatically connecting data pin(s) when adding Sequence connection
                    if (FromNode.bIsSequencePin)
                    {
                        Editor.AddConnection(FromNode.Node, -1, NewNode, -1, EConnectionType.Sequence, true, false);
                    }
                    else
                    {
                        GraphDataType OutputDataType = FromNode.Node.OutputWidgets[FromNode.PinIndex].DataType;
                        Type OutputType = OutputDataType.CSType;
                        (NodeInputPinWidget? FirstDataInput, int FirstDataIndex) = NewNode.FindFirstDataInput();

						if ( OutputType == typeof(ControlFlowOutputID) )
                        {
                            Editor.AddConnection(FromNode.Node, FromNode.PinIndex, NewNode, -1, EConnectionType.Sequence, true, false);
                        }
                        else if (FirstDataInput != null && Editor.Graph.CanConnectTypes(OutputDataType, FirstDataInput.DataType) )
                        {
                            Editor.AddConnection(FromNode.Node, FromNode.PinIndex, NewNode, FirstDataIndex, EConnectionType.Data,
                                AutoReplaceExistingConnections, AutoConnectSequencePath);
                        }
                    }
                }
            });

            return NewNode;

		}


        public void RequestShowContextMenu(in InputDeviceState deviceState)
        {
			Type WidgetHitType = typeof(NodeWidget);
			bool bClickHitNode = GraphViewport.WidgetScene.HitQuery(deviceState.CurrentPosition, out var hitResult,
				(Widget w) => { return w.GetType().IsSubclassOf(WidgetHitType); });

            bool bIsMaxOneNodeSelected = 
                GraphViewport.SelectionManager.HasSelection == false 
                || GraphViewport.SelectionManager.CheckSelectionRequirement(1, 0);

			if (bClickHitNode && bIsMaxOneNodeSelected)
				PendingNextFrameAction = () => { BeginShowNodeContextMenu( (hitResult.HitWidget as NodeWidget)! ); };
			else
                PendingNextFrameAction = () => { BeginShowNewNodePopupMenu(null); };
        }




        public void BeginShowNodeContextMenu(NodeWidget nodeWidget)
        {
            DismissActivePopupDialogs();

			Vector2f UIPopupLocation = GetDeviceStateInSpace(EInteractionSpace.UILayer).CurrentPosition;
			Vector2f ViewportPopupLocation = GetDeviceStateInSpace(EInteractionSpace.GraphViewport).CurrentPosition;

			ActiveNodePopupDialog = new SimplePopupMenuDialog();
			ActivePopupDialog = ActiveNodePopupDialog;
			ActiveNodePopupDialog.Position = UIPopupLocation;
			ActiveNodePopupDialog.OnDismissDialogClick = () => { DismissActivePopupDialogs(); };
            ActiveNodePopupDialog.OnItemSelected += (SimplePopupMenuDialog dialog, MenuItem item) => {
                DismissActivePopupDialogs();
            };

            ActiveNodePopupDialog.AddItem(new MenuItem() {
                Text = "Delete Node",
                OnClicked = () => {
                    GraphViewport.ExecuteGraphEdit((NodeGraphEditor Editor) => { Editor.RemoveNode(nodeWidget); });
                }
            });

            if (nodeWidget is FunctionDefNodeWidget functionNodeWidget) {
                ActiveNodePopupDialog.AddItem(new MenuItem() {
                    Text = "Add Return",
                    OnClicked = () => {
                        GraphViewport.ExecuteGraphEdit((NodeGraphEditor Editor) => { 
                            Editor.AddNodeOfType( new(typeof(FunctionReturnNode)), 
                                nodeWidget.GetActiveView()!.BoundsQuery(null).CenterRight, 
                                (INodeInfo newNode) => {
                                    (newNode.Node as FunctionReturnNode)!.LinkToFunction(functionNodeWidget.FunctionNode);
                                }); 
                        });
                    }
                });
            }

			ActiveNodePopupDialog.AddItem(new MenuItem() {
				Text = "Log Node Info",
				OnClicked = () => {
                    GlobalGraphOutput.AppendLog($"{nodeWidget.ParentNode!.ToString()} - NodeID {nodeWidget.GraphNodeIdentifier}");
				}
			});

			ActivePopupMenuWidgetSet.AddRootWidget(ActiveNodePopupDialog);
			GraphViewport.ViewportUI.WidgetScene.AddSource(ActivePopupMenuWidgetSet);
			SystemKeyboardRouter.Instance.PushHotkeyTarget(ActiveNodePopupDialog);

		}

	}
}
