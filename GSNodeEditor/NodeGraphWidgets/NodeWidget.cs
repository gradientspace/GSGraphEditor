// Copyright Gradientspace Corp. All Rights Reserved.
using g3;
using Gradientspace.NodeGraph;
using Gradientspace.UI;
using SkiaSharp;
using System;
using System.Diagnostics;
using static System.Runtime.InteropServices.JavaScript.JSType;


namespace GSNodeEditor
{

    public class NodeWidget : Widget, ISimpleCaptureTarget, IDisposable
    {
        public NodeWidgetStyle WidgetStyle { get; set; }

        bool _CompactMode = false;
        public bool CompactMode { get { return _CompactMode; } set { UpdateCompactMode(value); } }

        public NodeGraphView ParentGraphWidget { get; private set; }
        public INodeInfo ParentNodeInfo { get; private set; }

        protected FixedPointAnchor nodeAnchor;
        public Vector2f Position {
            get { return nodeAnchor.AnchorOrigin; }
            set { 
                nodeAnchor.AnchorOrigin = value;
                ParentGraphWidget.NotifyNodeWidgetModified(this);
            }
        }


        public Vector2f Size { get; set; }
        public string Label { get; set; }
        public string VersionLabel { get; set; } = "";
        public int GraphNodeIdentifier { get; set; }


        public enum NodeStates
        {
            Normal = 0,
            Error = 1
        }
        public NodeStates NodeState { get; protected set; } = NodeStates.Normal;

        // todo struct?
        public List<NodeInputPinWidget> InputWidgets;
        public List<RelativeBoxAnchor> InputWidgetAnchors;

        public List<NodeOutputPinWidget> OutputWidgets;
        public List<RelativeBoxAnchor> OutputWidgetAnchors;

        public NodeInputExecPinWidget? InputSequenceWidget = null;
        public RelativeBoxAnchor? InputSequenceWidgetAnchor;
        public NodeOutputExecPinWidget? OutputSequenceWidget = null;
        public RelativeBoxAnchor? OutputSequenceWidgetAnchor;

        public NodeErrorWidget ErrorWidget;
        public RelativeBoxAnchor ErrorWidgetAnchor;

        public NodeWidget(NodeGraphView graphView, INodeInfo node)
        {
            ParentGraphWidget = graphView;
            ParentNodeInfo = node;

            Label = "Node";

            InputWidgets = new List<NodeInputPinWidget>();
            InputWidgetAnchors = new List<RelativeBoxAnchor>();

            OutputWidgets = new List<NodeOutputPinWidget>();
            OutputWidgetAnchors = new List<RelativeBoxAnchor>();

            nodeAnchor = new FixedPointAnchor();
            AnchorTo(nodeAnchor);
            AnchorPlacement = new AnchorLocation(BoxPoints.TopLeft);

            WidgetStyle = NodeWidgetStyles.GetStyleByNodeType(node.Node);

            SetInputBehavior(new BasicWidgetInputBehavior(this, this) { Depth = 0, EnableCapture = false } );

            ErrorWidget = new NodeErrorWidget();
            ErrorWidgetAnchor = new RelativeBoxAnchor(this.GetAnchor());
            ErrorWidgetAnchor.BoxPoint = BoxPoints.TopRight;
            ErrorWidgetAnchor.Offset = new Vector2f(-20, 3);
            ErrorWidget.AnchorTo(ErrorWidgetAnchor);
            ErrorWidget.AnchorPlacement = new AnchorLocation(BoxPoints.CenterBottom);

            InitializeDynamicPinEditWidgets();
        }

        public INode? ParentNode { get { return ParentNodeInfo.Node; } }

        public virtual void InitializeFromNode(INodeInfo nodeInfo)
        {
            GraphNodeIdentifier = nodeInfo.Identifier;

            // for old-version nodes, strip off trailing "_v1p1" and convert to a label we show in the corner
            VersionLabel = "";
            string? overrideLabel = null, version = null;
            if ( nodeInfo.Node is NodeBase baseNode && (baseNode.LibraryNodeType?.IsOldVersion ?? false) ) {
                (overrideLabel, version) = NodeVersion.ParseNodeNameWithVersion(baseNode.GetNodeName());
                VersionLabel = (version != null) ? $"v{version}" : "";
            }

            ParentGraphWidget.UpdateNodeWidgetLabel(this, overrideLabel);

            INode node = nodeInfo.Node!;
            foreach ( INodeInputInfo inputInfo in node.EnumerateInputs() )
            {
                if (inputInfo.IsHidden)     // don't create a widget for hidden inputs
                    continue;

                NodeInputPinWidget inputWidget = new NodeInputPinWidget(inputInfo);
                InputWidgets.Add(inputWidget);
                AddChildWidget(inputWidget);

                RelativeBoxAnchor boxAnchor = new RelativeBoxAnchor(nodeAnchor);
                inputWidget.AnchorTo(boxAnchor);
                InputWidgetAnchors.Add(boxAnchor);
            }

            foreach (INodeOutputInfo outputInfo in node.EnumerateOutputs())
            {
                NodeOutputPinWidget outputWidget = new NodeOutputPinWidget(outputInfo);
                OutputWidgets.Add(outputWidget);
                AddChildWidget(outputWidget);

                RelativeBoxAnchor boxAnchor = new RelativeBoxAnchor(nodeAnchor);
                boxAnchor.BoxPoint = BoxPoints.CenterRight;
                outputWidget.AnchorPlacement = new AnchorLocation(BoxPoints.CenterRight);
                outputWidget.AnchorTo(boxAnchor);
                OutputWidgetAnchors.Add(boxAnchor);
            }
        }



        public virtual void AddMissingInputPin(string InputName)
        {
            INodeInputInfo inputInfo = new INodeInputInfo();
            inputInfo.InputName = InputName;
            inputInfo.Input = new MissingNodeInput();
            NodeInputPinWidget inputWidget = new NodeInputPinWidget(inputInfo);
            InputWidgets.Add(inputWidget);
            AddChildWidget(inputWidget);

            RelativeBoxAnchor boxAnchor = new RelativeBoxAnchor(nodeAnchor);
            inputWidget.AnchorTo(boxAnchor);
            InputWidgetAnchors.Add(boxAnchor);

            inputWidget.WidgetStyle = PinWidgetStyles.InputOutputStyleSet_Missing;
        }

        public virtual void AddMissingOutputPin(string OutputName)
        {
            INodeOutputInfo outputInfo = new INodeOutputInfo();
            outputInfo.OutputName = OutputName;
            outputInfo.Output = new MissingNodeOutput();
            NodeOutputPinWidget outputWidget = new NodeOutputPinWidget(outputInfo);
            OutputWidgets.Add(outputWidget);
            AddChildWidget(outputWidget);

            RelativeBoxAnchor boxAnchor = new RelativeBoxAnchor(nodeAnchor);
            boxAnchor.BoxPoint = BoxPoints.CenterRight;
            outputWidget.AnchorPlacement = new AnchorLocation(BoxPoints.CenterRight);
            outputWidget.AnchorTo(boxAnchor);
            OutputWidgetAnchors.Add(boxAnchor);

            outputWidget.WidgetStyle = PinWidgetStyles.InputOutputStyleSet_Missing;
        }



        public virtual void Reinitialize()
        {
            foreach (NodeInputPinWidget inputWidget in InputWidgets)
                RemoveChildWidget(inputWidget);
            InputWidgets.Clear();
            InputWidgetAnchors.Clear();
            foreach (NodeOutputPinWidget outputWidget in OutputWidgets)
                RemoveChildWidget(outputWidget);
            OutputWidgets.Clear();
            OutputWidgetAnchors.Clear();

            InitializeFromNode(this.ParentNodeInfo);
        }



        public void SetStandardSequencePinsEnabled(bool bInput, bool bOutput)
        {
            bool bInputExists = (InputSequenceWidget != null);
            if ( bInputExists != bInput )
            {
                if ( bInput )
                {
                    InputSequenceWidget = new NodeInputExecPinWidget(this);
                    AddChildWidget(InputSequenceWidget);

                    InputSequenceWidgetAnchor = new RelativeBoxAnchor(nodeAnchor);
                    InputSequenceWidgetAnchor.BoxPoint = BoxPoints.TopLeft;
                    InputSequenceWidgetAnchor.Offset = new Vector2f(-5, -2);
                    InputSequenceWidget.AnchorPlacement = new AnchorLocation(BoxPoints.TopLeft);
                    InputSequenceWidget.AnchorTo(InputSequenceWidgetAnchor);
                }
                else
                {
                    if (InputSequenceWidget != null) RemoveChildWidget(InputSequenceWidget);
                    InputSequenceWidget = null; InputSequenceWidgetAnchor = null;
                }
            }

            bool bOutputExists = (OutputSequenceWidget != null);
            if ( bOutputExists != bOutput)
            {
                if ( bOutput )
                {
                    OutputSequenceWidget = new NodeOutputExecPinWidget(this);
                    AddChildWidget(OutputSequenceWidget);

                    OutputSequenceWidgetAnchor = new RelativeBoxAnchor(nodeAnchor);
                    OutputSequenceWidgetAnchor.BoxPoint = BoxPoints.TopRight;
                    OutputSequenceWidgetAnchor.Offset = new Vector2f(5, -2);
                    OutputSequenceWidget.AnchorPlacement = new AnchorLocation(BoxPoints.TopRight);
                    OutputSequenceWidget.AnchorTo(OutputSequenceWidgetAnchor);
                }
                else
                {
                    if (OutputSequenceWidget != null) RemoveChildWidget(OutputSequenceWidget);
                    OutputSequenceWidget = null; OutputSequenceWidgetAnchor = null;
                }
            }
        }


        public UtilityButton? AddInputWidget;
        public RelativeBoxAnchor? AddInputWidgetAnchor;
        public UtilityButton? RemoveInputWidget;
        public RelativeBoxAnchor? RemoveInputWidgetAnchor;

        public UtilityButton? AddOutputWidget;
        public RelativeBoxAnchor? AddOutputWidgetAnchor;
        public UtilityButton? RemoveOutputWidget;
        public RelativeBoxAnchor? RemoveOutputWidgetAnchor;

        protected virtual void InitializeDynamicPinEditWidgets()
        {
            const int NodeEdgeInset = 5;
            const int NodeBaseShift = -4;
            const int ButtonSpace = 1;

            // note: the anchor corners don't really make sense here, this is because
            // the bounds being set in the anchor boxes are not relative to the anchor
            // box corners, they are just being set to box2f(0,button_dimensions)

            if (ParentNode is INode_VariableInputs) {
                AddInputWidget = new UtilityButton(UtilityButton.ButtonTypes.Plus);
                AddInputWidgetAnchor = new RelativeBoxAnchor(this.GetAnchor());
                AddInputWidgetAnchor.BoxPoint = BoxPoints.BottomLeft;
                AddInputWidgetAnchor.Offset = new Vector2f(NodeEdgeInset, NodeBaseShift);
                AddInputWidget.AnchorTo(AddInputWidgetAnchor);
                AddInputWidget.AnchorPlacement = new AnchorLocation(BoxPoints.TopLeft);
                AddInputWidget.OnClicked += AddInputOutputWidget_OnClicked;
                AddChildWidget(AddInputWidget);

                RemoveInputWidget = new UtilityButton(UtilityButton.ButtonTypes.Minus);
                RemoveInputWidgetAnchor = new RelativeBoxAnchor(AddInputWidgetAnchor);
                RemoveInputWidgetAnchor.BoxPoint = BoxPoints.TopRight;
                RemoveInputWidgetAnchor.Offset = new Vector2f(ButtonSpace, 0);
                RemoveInputWidget.AnchorTo(RemoveInputWidgetAnchor);
                RemoveInputWidget.AnchorPlacement = new AnchorLocation(BoxPoints.TopLeft);
                RemoveInputWidget.OnClicked += AddInputOutputWidget_OnClicked;
                AddChildWidget(RemoveInputWidget);
            }
            if (ParentNode is INode_VariableOutputs)
            {
                AddOutputWidget = new UtilityButton(UtilityButton.ButtonTypes.Plus);
                AddOutputWidgetAnchor = new RelativeBoxAnchor(this.GetAnchor());
                AddOutputWidgetAnchor.BoxPoint = BoxPoints.BottomRight;
                AddOutputWidgetAnchor.Offset = new Vector2f(-(AddOutputWidget.Dimensions.x+NodeEdgeInset), NodeBaseShift);
                AddOutputWidget.AnchorTo(AddOutputWidgetAnchor);
                AddOutputWidget.AnchorPlacement = new AnchorLocation(BoxPoints.TopRight);
                AddOutputWidget.OnClicked += AddInputOutputWidget_OnClicked;
                AddChildWidget(AddOutputWidget);

                RemoveOutputWidget = new UtilityButton(UtilityButton.ButtonTypes.Minus);
                RemoveOutputWidgetAnchor = new RelativeBoxAnchor(AddOutputWidgetAnchor);
                RemoveOutputWidgetAnchor.BoxPoint = BoxPoints.TopLeft;
                RemoveOutputWidgetAnchor.Offset = new Vector2f(ButtonSpace, 0);
                RemoveOutputWidget.AnchorTo(RemoveOutputWidgetAnchor);
                RemoveOutputWidget.AnchorPlacement = new AnchorLocation(BoxPoints.TopLeft);
                RemoveOutputWidget.OnClicked += AddInputOutputWidget_OnClicked;
                AddChildWidget(RemoveOutputWidget);
            }
        }
        private void AddInputOutputWidget_OnClicked(Button button)
        {
            if (button == RemoveInputWidget)
                ParentGraphWidget.ExecuteGraphEdit( (NodeGraphEditor editor) => { editor.RemoveInputPinFromNode(this); } );
            else if (button == AddInputWidget)
                ParentGraphWidget.ExecuteGraphEdit( (NodeGraphEditor editor) => { editor.AddInputPinToNode(this); } );
            if (button == RemoveOutputWidget)
                ParentGraphWidget.ExecuteGraphEdit( (NodeGraphEditor editor) => { editor.RemoveOutputPinFromNode(this); });
            else if (button == AddOutputWidget)
                ParentGraphWidget.ExecuteGraphEdit( (NodeGraphEditor editor) => { editor.AddOutputPinToNode(this); });
        }


        //! returns -1 if pin index is not found
        public int FindInputPinIndexByName(string inputName)
        {
            return InputWidgets.FindIndex(x => x.InputName == inputName);
        }
        //! returns -1 if pin index is not found
        public int FindOutputPinIndexByName(string outputName)
        {
            return OutputWidgets.FindIndex(x => x.OutputName == outputName);
        }


        //! returns first input pin that can accept data (ie skips node-constant, hidden, etc)
        public (NodeInputPinWidget?,int) FindFirstDataInput()
        {
            ENodeInputFlags ignoreFlags = ENodeInputFlags.IsNodeConstant | ENodeInputFlags.Hidden;
            for (int i = 0; i < InputWidgets.Count; ++i) {
                ENodeInputFlags flags = InputWidgets[i].NodeInputInfo.Input.GetInputFlags();
                if ((flags & ignoreFlags) != 0)
                    continue;
                return (InputWidgets[i],i);
            }
            return (null,-1);
        }


        public void UpdateAllInlineInfo(INodeGraph Graph)
        {
            foreach (NodeInputPinWidget pin in InputWidgets)
                pin.UpdateInlineInfo(Graph, this.GraphNodeIdentifier);
            UpdateSequenceInlineInfo(Graph);
        }
        public void UpdateInlineInfo(INodeGraph Graph, string inputName)
        {
            int index = FindInputPinIndexByName(inputName);
            if ( index >= 0 )
                InputWidgets[index].UpdateInlineInfo(Graph, this.GraphNodeIdentifier);
        }
        public void UpdateSequenceInlineInfo(INodeGraph Graph)
        {
            InputSequenceWidget?.UpdateInlineInfo(Graph, GraphNodeIdentifier);
            OutputSequenceWidget?.UpdateInlineInfo(Graph, GraphNodeIdentifier);
        }


        public AxisAlignedBox2f Bounds 
        {
            get { return new(Position, Position + Size); }
        }


        public override IWidgetView CreateDefaultView()
        {
            return new NodeWidgetView(this);
        }

        public override bool GetTooltipStrings(out string? tooltip, out string[]? extendedTooltip)
        {
            tooltip = null;
            extendedTooltip = null;
            if (ParentNode == null)
                return false;

            int NumStrings = 2;
            string ShowNamespace = ParentNode!.GetNodeNamespace() ?? "(namespace missing)";
            string? VersionOf = null;
            if (ParentNode is NodeBase baseNode) {
                if ( baseNode.LibraryNodeType != null)
                    ShowNamespace = baseNode.LibraryNodeType.UICategory;
                if (baseNode.LibraryNodeType != null && baseNode.LibraryNodeType.VersionOf != null) {
                    VersionOf = $"{baseNode.LibraryNodeType.VersionOf.ToString()} v{baseNode.LibraryNodeType.Version}";
                    NumStrings++;
                }
            }
            tooltip =  $"[{ShowNamespace}] {ParentNode!.GetNodeName()}";

            extendedTooltip = new string[NumStrings];

            if ( ParentNode is LibraryFunctionNodeBase libNode )
                extendedTooltip[0] = $"{libNode.LibraryClass!.Namespace}.{libNode.LibraryClass!.Name}.{libNode.Function!.Name}";
            else
                extendedTooltip[0] = ParentNode.GetType().ToString();
            extendedTooltip[1] = $"NodeID: {ParentNodeInfo.Identifier}";
            if (VersionOf != null)
                extendedTooltip[2] = $"Version Of: {VersionOf}";
            return true;
        }

        public Vector2f GetInputPinConnectionPoint(int PinIndex)
        {
            return InputWidgets[PinIndex].GetActiveView()?.BoundsQuery(InputWidgetAnchors[PinIndex]).CenterLeft ?? nodeAnchor.GetOrigin();
        }
        public Vector2f GetOutputPinConnectionPoint(int PinIndex)
        {
            return OutputWidgets[PinIndex].GetActiveView()?.BoundsQuery(OutputWidgetAnchors[PinIndex]).CenterRight ?? nodeAnchor.GetOrigin();
        }
        public GraphDataType GetInputPinDataType(int PinIndex)
        {
            return InputWidgets[PinIndex].DataType;
        }
        public GraphDataType GetOutputPinDataType(int PinIndex)
        {
            return OutputWidgets[PinIndex].DataType;
        }


        public Vector2f GetInputSequencePinConnectionPoint()
        {
            return InputSequenceWidget?.GetActiveView()?.BoundsQuery(InputSequenceWidgetAnchor!).CenterLeft ?? nodeAnchor.GetOrigin();
        }
        public Vector2f GetOutputSequencePinConnectionPoint()
        {
            return OutputSequenceWidget?.GetActiveView()?.BoundsQuery(OutputSequenceWidgetAnchor!).CenterRight ?? nodeAnchor.GetOrigin();
        }


        protected virtual void UpdateCompactMode(bool bNewValue)
        {
            _CompactMode = bNewValue;
            foreach (NodeInputPinWidget pin in InputWidgets)
                pin.CompactMode = bNewValue;
            foreach (NodeOutputPinWidget pin in OutputWidgets)
                pin.CompactMode = bNewValue;
        }


        // ISimpleCaptureTarget API
        public void UpdateCapture(ISimpleCaptureTarget.ECaptureState State, in InputDeviceState deviceState) {
            Debug.Assert(false);    // capture should be disabled
        }
        public void UpdateHover(ISimpleCaptureTarget.EHoverState State, in InputDeviceState deviceState, out bool bContinueHover)
        {
            bContinueHover = true;
            IsHovered = (State == ISimpleCaptureTarget.EHoverState.Begin || State == ISimpleCaptureTarget.EHoverState.Update);
        }
        public bool IsHovered { get; private set; }



        // placeholder node utility/support
        public virtual bool IsPlaceholderNode { get { 
                return ParentNodeInfo.Node is PlaceholderNodeBase; } }

        public virtual bool IsPureNode { get { 
                return (ParentNodeInfo.Node!.GetNodeFlags() & ENodeFlags.IsPure) != 0; } }


        // support for custom sequence pins
        public virtual bool HasConfigurableSequencePins { get { return false; } }
        public virtual void UpdateSequencePins() { }


        // not sure this should be something that the node itself tracks...maybe
        // should be done at the graphview level
        public void SetNodeErrorState(List<string>? ErrorMessages)
        {
            if (NodeState == NodeStates.Normal)
                AddChildWidget(ErrorWidget);
            ErrorWidget.SetErrorStrings(ErrorMessages);
            NodeState = NodeStates.Error;
        }
        public void ClearNodeErrorState()
        {
            ErrorWidget.SetErrorStrings(null);
            if (NodeState == NodeStates.Error)
                RemoveChildWidget(ErrorWidget);
            NodeState = NodeStates.Normal;
        }

    }



    public class NodeWidgetView : IWidgetView
    {
        public NodeWidget SourceNodeWidget;
		public int LastDrawOrderIndex { get; set; } = 0;

		public NodeWidgetView(NodeWidget nodeWidget )
        {
            this.SourceNodeWidget = nodeWidget;
        }

        public Widget GetWidget() { return SourceNodeWidget; }

        // all positions are in local coordinates
        public AxisAlignedBox2f ChildBounds { get; set; }
        public AxisAlignedBox2f LocalNodeBounds { get; set; }
        public string DrawLabel { get; set; } = string.Empty;
        public string VersionLabel { get; set; } = string.Empty;
        public Vector2f LabelOrigin { get; set; }

        List<(int, int)> InOutMatches = new List<(int, int)>();

        static SKPaint InOutCurvePaint = new SKPaint() {
            Color = new SKColor(255, 165, 0, 80),
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 2,
            IsAntialias = true,
            PathEffect = SKPathEffect.CreateDash(new float[] { 3, 3 }, 10)
        };

        public virtual AxisAlignedBox2f BoundsQuery(ILayoutAnchor? RelativeToAnchor = null)
        {
            return (RelativeToAnchor != null) ?
                AnchorLocation.GetAnchoredBounds(LocalNodeBounds, RelativeToAnchor, GetWidget().AnchorPlacement) : LocalNodeBounds;
        }

        public virtual void UpdateLayout(SKStyleCache StyleCache)
        {
            // TODO: this lays out all the pins by creating an Anchor for each one.
            // Probably could just be using offsets...
            // (this was some of the oldest layout code and is probably crufty)

            AxisAlignedBox2f InitialBox = new AxisAlignedBox2f(Vector2f.Zero, SourceNodeWidget.Size);

            SKPaint LabelTextPaint = 
                StyleCache.GetCachedPaint(SourceNodeWidget.WidgetStyle.NodeStyle.StandardStyle, SKStyleCache.EPaintType.Text);
            WidgetMargins LabelMargins = SourceNodeWidget.WidgetStyle.NodeStyle.BaseMargins;

            SKPaint PinTextPaint =
                StyleCache.GetCachedPaint(PinWidgetStyles.DefaultInputStandardStyle, SKStyleCache.EPaintType.Text);
            WidgetMargins PinMargins = PinWidgetStyles.DefaultInputStandardStyle.Margins;

            string UseLabel = (SourceNodeWidget.Label.Length > 0) ? SourceNodeWidget.Label : "(Node)";
            VersionLabel = SourceNodeWidget.VersionLabel;

            TextHeightInfo LabelTextHeightInfo = StyleCache.GetCachedFontHeightInfo(SourceNodeWidget.WidgetStyle.NodeStyle.StandardStyle);
            float LabelWidth = LabelTextPaint.MeasureText(UseLabel);

            // assuming default sequence pins are the same width...
            float SequencePinWidth = 0;
            if (SourceNodeWidget.InputSequenceWidget != null)
                SequencePinWidth = SourceNodeWidget.InputSequenceWidget.Dimensions.x;
            else if (SourceNodeWidget.OutputSequenceWidget != null)
                SequencePinWidth = SourceNodeWidget.OutputSequenceWidget.Dimensions.x;

            //int NumInputs = SourceNodeWidget.Inputs.Count;
            int NumInputs = SourceNodeWidget.InputWidgets.Count;
            int NumOutputs = SourceNodeWidget.OutputWidgets.Count;

            const float PinNodeEdgeOffset = 5;
            const float PinNodeConstantEdgeOffset = 2;
            const float PinVerticalSpace = 5;

            // compute max input and output pin text length
            InOutMatches.Clear();
            float MaxInputPinWidth = 0;
            for (int k = 0; k < NumInputs; ++k)
            {
                //float InputTextWidth = PinTextPaint.MeasureText(SourceNodeWidget.InputWidgets[k].InputName);
                //MaxInputPinWidth = MathF.Max(MaxInputPinWidth, InputTextWidth);
                SourceNodeWidget.InputWidgets[k].GetActiveView()?.UpdateLayout(StyleCache);
                AxisAlignedBox2f childBounds = SourceNodeWidget.InputWidgets[k].GetActiveView()?.BoundsQuery(null) ?? AxisAlignedBox2f.Empty;
                float ChildWidth = childBounds.Width;
                if (SourceNodeWidget.InputWidgets[k].NodeInputInfo.IsNodeConstant)
                    ChildWidth += (PinNodeEdgeOffset+PinNodeConstantEdgeOffset);      // otherwise constant val may overlap rhs

                MaxInputPinWidth = MathF.Max(MaxInputPinWidth, ChildWidth);

                // probably should be figured out at Widget level...
                // (possibly even requires querying the node...)
                if (SourceNodeWidget.InputWidgets[k].NodeInputInfo.IsInOut) {
                    for (int j = 0; j < NumOutputs; ++j) {
                        if ( SourceNodeWidget.OutputWidgets[j].OutputName == SourceNodeWidget.InputWidgets[k].InputName ) {
                            InOutMatches.Add(new(k, j));
                            break;
                        }
                    }
                }
            }

            float MaxOutputPinWidth = 0;
            for (int k = 0; k < NumOutputs; ++k)
            {
                //float OutputTextWidth = PinTextPaint.MeasureText(SourceNodeWidget.OutputWidgets[k].OutputName); ;
                //MaxOutputPinWidth = MathF.Max(MaxOutputPinWidth, OutputTextWidth);

                SourceNodeWidget.OutputWidgets[k].GetActiveView()?.UpdateLayout(StyleCache);
                AxisAlignedBox2f childBounds = SourceNodeWidget.OutputWidgets[k].GetActiveView()?.BoundsQuery(null) ?? AxisAlignedBox2f.Empty;
                MaxOutputPinWidth = MathF.Max(MaxOutputPinWidth, childBounds.Width);
            }

            // expand rect to contain label, and make sure label does not overlap sequence pins
            if (InitialBox.Width < (LabelWidth + 2*SequencePinWidth + LabelMargins.TotalWidth) )
            {
                float extra = (LabelWidth + 2*SequencePinWidth + LabelMargins.TotalWidth) - InitialBox.Width;
                InitialBox.Max.x += extra;
                LabelWidth += 2 * SequencePinWidth;
            }
            else
                SequencePinWidth = 0;

            float TotalPinWidth = MaxInputPinWidth + MaxOutputPinWidth;// + 2*PinNodeEdgeOffset;
            if (InitialBox.Width < TotalPinWidth)
            {
                float extra = TotalPinWidth - InitialBox.Width;
                InitialBox.Max.x += extra;
            }

            // truncate too-long label with ...
            //if (NodeBox.Width < LabelWidth + 2 * LabelMargins.x)
            //{
            //    float dotsWidth = LabelTextPaint.MeasureText("...");
            //    long breakAt = LabelTextPaint.BreakText(UseLabel, NodeBox.Width - 2 * LabelMargins.x - dotsWidth);
            //    UseLabel = UseLabel.Substring(0, (int)breakAt) + "...";
            //    LabelWidth = LabelTextPaint.MeasureText(UseLabel);
            //}

            this.DrawLabel = UseLabel;

            // centered
            float LabelX = (InitialBox.Width / 2.0f) - (LabelWidth / 2.0f);       
            float LabelY = LabelMargins.Top + LabelTextHeightInfo.AboveBaseline;
            LabelOrigin = new Vector2f(LabelX + SequencePinWidth, LabelY);

            TextHeightInfo PinTextHeightInfo = StyleCache.GetCachedFontHeightInfo(PinWidgetStyles.DefaultInputStandardStyle);
            float PinStartOffsetY = InitialBox.Min.y + (LabelTextHeightInfo.MaxTotalHeight + LabelMargins.TotalHeight);
            float InputPinLeft = InitialBox.Min.x;
            float InputPinRight = InputPinLeft + (MaxInputPinWidth + PinMargins.TotalWidth);

            // layout input pin boxes
            float CurPinY = PinStartOffsetY;
            float MaxY = CurPinY;
            for (int k = 0; k < NumInputs; ++k)
            {
                float BottomY = CurPinY + (PinTextHeightInfo.MaxTotalHeight + PinMargins.TotalHeight);
                bool bIsConstant = SourceNodeWidget.InputWidgets[k].NodeInputInfo.IsNodeConstant;
                float shiftX = (bIsConstant) ? PinNodeConstantEdgeOffset : -PinNodeEdgeOffset;
                AxisAlignedBox2f localPinBox = new AxisAlignedBox2f(InputPinLeft+shiftX, CurPinY, InputPinRight+shiftX, BottomY);
                CurPinY = BottomY + PinVerticalSpace;
                MaxY = Math.Max(MaxY, CurPinY);

                SourceNodeWidget.InputWidgetAnchors[k].Box = localPinBox;
            }

            // now do output pins
            float OutputPinRight = InitialBox.Max.x + PinNodeEdgeOffset;
            float OutputPinLeft = OutputPinRight - (MaxOutputPinWidth + PinMargins.TotalWidth);

            // layout output pin boxes
            CurPinY = PinStartOffsetY;
            for (int k = 0; k < NumOutputs; ++k)
            {
                float BottomY = CurPinY + (PinTextHeightInfo.MaxTotalHeight + PinMargins.TotalHeight);
                AxisAlignedBox2f localPinBox = new AxisAlignedBox2f(OutputPinLeft, CurPinY, OutputPinRight, BottomY);
                CurPinY = BottomY + PinVerticalSpace;
                MaxY = Math.Max(MaxY, CurPinY);

                SourceNodeWidget.OutputWidgetAnchors[k].Box = localPinBox;
            }

            MaxY += 2;      // add a bit more space after the lowest pin

            if ( MaxY > InitialBox.Max.y )
                InitialBox.Max.y = MaxY;
            this.LocalNodeBounds = InitialBox;
            this.ChildBounds = InitialBox;

            SourceNodeWidget.InputSequenceWidget?.GetActiveView()?.UpdateLayout(StyleCache);
            SourceNodeWidget.OutputSequenceWidget?.GetActiveView()?.UpdateLayout(StyleCache);

            if (SourceNodeWidget.InputSequenceWidgetAnchor != null)
                SourceNodeWidget.InputSequenceWidgetAnchor.Box = LocalNodeBounds;
            if (SourceNodeWidget.OutputSequenceWidgetAnchor != null)
                SourceNodeWidget.OutputSequenceWidgetAnchor.Box = LocalNodeBounds;

            if (SourceNodeWidget.ErrorWidget.ParentWidget != null) {
                SourceNodeWidget.ErrorWidget.GetActiveView()?.UpdateLayout(StyleCache);
                SourceNodeWidget.ErrorWidgetAnchor.Box = LocalNodeBounds;
            }

            if (SourceNodeWidget.AddInputWidget?.ParentWidget != null) {
                SourceNodeWidget.AddInputWidget.GetActiveView()?.UpdateLayout(StyleCache);
                SourceNodeWidget.AddInputWidgetAnchor!.Box = LocalNodeBounds;
            }
            if (SourceNodeWidget.RemoveInputWidget?.ParentWidget != null) {
                SourceNodeWidget.RemoveInputWidget.GetActiveView()?.UpdateLayout(StyleCache);
                SourceNodeWidget.RemoveInputWidgetAnchor!.Box = new AxisAlignedBox2f(Vector2f.Zero, SourceNodeWidget.AddInputWidget!.Dimensions);
            }
            if (SourceNodeWidget.AddOutputWidget?.ParentWidget != null) {
                SourceNodeWidget.AddOutputWidget.GetActiveView()?.UpdateLayout(StyleCache);
                SourceNodeWidget.AddOutputWidgetAnchor!.Box = LocalNodeBounds;
            }
            if (SourceNodeWidget.RemoveOutputWidget?.ParentWidget != null) {
                SourceNodeWidget.RemoveOutputWidget.GetActiveView()?.UpdateLayout(StyleCache);
                SourceNodeWidget.RemoveOutputWidgetAnchor!.Box = new AxisAlignedBox2f(Vector2f.Zero, SourceNodeWidget.AddOutputWidget!.Dimensions);
            }
        }


        public virtual bool HitQuery(Vector2f QueryPoint, out WidgetHitResult Result)
        {
            Result = WidgetHitResult.None;

            QueryPoint -= SourceNodeWidget.GetAnchor().GetOrigin();

            if (ChildBounds.Contains(QueryPoint) == false)
                return false;

            if (LocalNodeBounds.Contains(QueryPoint))
            {
                Result = new WidgetHitResult() { HitWidget = SourceNodeWidget };
                return true;
            }
            return false;
        }

        public virtual bool HitTest(Vector2f QueryPoint)
        {
            WidgetHitResult HitResult;
            return HitQuery(QueryPoint, out HitResult);
        }


        public virtual void Draw(SKStyleCache StyleCache, SKCanvas Canvas, ILayoutAnchor Anchor)
        {
            WidgetStateStyle UseStateStyle = (SourceNodeWidget.NodeState == NodeWidget.NodeStates.Error) ?
                NodeWidgetStyles.NodeErrorStyleSet : SourceNodeWidget.WidgetStyle.NodeStyle;
            if (DebugManager.Instance.IsNodeActive(SourceNodeWidget.GraphNodeIdentifier))
                UseStateStyle = NodeWidgetStyles.NodeDebugStyleSet;
            WidgetStyle NodeFillStyle = UseStateStyle.Select(SourceNodeWidget.IsHovered, false);
            SKPaint NodePaint = StyleCache.GetCachedPaint(NodeFillStyle, SKStyleCache.EPaintType.Background);

            SKPaint LabelTextPaint =
                StyleCache.GetCachedPaint(SourceNodeWidget.WidgetStyle.NodeStyle.StandardStyle, SKStyleCache.EPaintType.Text);
            WidgetMargins LabelMargins = SourceNodeWidget.WidgetStyle.NodeStyle.BaseMargins;

            Vector2f DrawOrigin = Anchor.GetOrigin();
            AxisAlignedBox2f PlacedBounds = AnchorLocation.MakeRelativeToAnchor(LocalNodeBounds, SourceNodeWidget.AnchorPlacement, DrawOrigin);

            SKMatrix InitialMatrix = Canvas.TotalMatrix;
            Canvas.Translate( Conversion.ToSkia(PlacedBounds.Min) );

            AxisAlignedBox2f DrawBounds = LocalNodeBounds;
            SKRect Rect = Conversion.ToSkia(DrawBounds);
            SKSize Radius = new(5, 5);

            Canvas.DrawRoundRect(Rect, Radius, NodePaint);

            string UseLabel = DrawLabel;
            Canvas.DrawText(UseLabel, LabelOrigin.x, LabelOrigin.y, LabelTextPaint);

            // draw little curves beween in and out pins for inout fields
            if (InOutMatches.Count > 0) {
                foreach ((int k, int j) in InOutMatches) {
                    Vector2f Start = SourceNodeWidget.InputWidgetAnchors[k].Box.CenterLeft;
                    Vector2f End = SourceNodeWidget.OutputWidgetAnchors[j].Box.CenterRight;
                    float d = (End.x-Start.x) * 0.75f;
                    SKPath Curve = new SKPath();
                    Curve.MoveTo(Conversion.ToSkia(Start));
                    Curve.CubicTo(new SKPoint(Start.x+d, Start.y), new SKPoint(End.x-d, End.y), Conversion.ToSkia(End));
                    Canvas.DrawPath(Curve, InOutCurvePaint);
                }
            }

            // todo same code as InputPinWidget, w/ different offset - should refactor...
            if (VersionLabel.Length > 0) {
                SKPaint DataTypeTextPaint = new SKPaint { Color = SKColors.White, IsAntialias = true, LcdRenderText = true, SubpixelText = true, TextSize = 10 };
                TextHeightInfo DataTypeTextHeightInfo = SKStyleCache.MeasureTextHeightInfo(DataTypeTextPaint);
                SKPaint WarningDataTypeFillPaint = new SKPaint { Color = SKColors.DarkOrange };
                const float Margin = 3;
                string TypeText = VersionLabel;
                SKRect Bounds = SKRect.Empty;
                float Width = DataTypeTextPaint.MeasureText(TypeText, ref Bounds);
                Bounds.Left -= (Margin + 2); Bounds.Right += (Margin + 1); Bounds.Bottom += Margin; Bounds.Top -= Margin;
                SKMatrix CurMatrix = Canvas.TotalMatrix;
                SKPoint Offset = Conversion.ToSkia(new Vector2f(LocalNodeBounds.Width - Width, LocalNodeBounds.Height+3));
                Canvas.Translate(Offset);
                Canvas.DrawRoundRect(Bounds, 8.0f, 8.0f, WarningDataTypeFillPaint);
                Canvas.DrawText(TypeText, new SKPoint(0, 0), DataTypeTextPaint);
                Canvas.SetMatrix(CurMatrix);
            }



            Canvas.SetMatrix(InitialMatrix);
        }




    }
}
