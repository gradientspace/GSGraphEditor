// Copyright Gradientspace Corp. All Rights Reserved.
using g3;
using Gradientspace.NodeGraph;
using Gradientspace.UI;
using System.Diagnostics;

namespace GSNodeEditor
{
    public class NodeWidget : Widget, ISimpleCaptureTarget, IDisposable
    {
        public NodeWidgetStyle WidgetStyle { get; set; }

        bool _CompactMode = false;
        public bool CompactMode { get { return _CompactMode; } set { UpdateCompactMode(value); } }

        public bool HideLabel { get; set; } = false;



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

        public string Label { get; set; }       // in some cases the View wants to update the Label... (fix this?)
        public string VersionLabel { get; protected set; } = "";
        public int GraphNodeIdentifier { get; protected set; }


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


}
