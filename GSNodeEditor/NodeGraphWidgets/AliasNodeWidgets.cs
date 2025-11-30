using g3;
using Gradientspace.NodeGraph;
using Gradientspace.UI;

namespace GSNodeEditor
{
    public class AliasNodeWidgetProvider : INodeWidgetProvider
    {
        public NodeWidget? CreateNewWidget(NodeGraphView Graph, INodeInfo nodeInfo)
        {
            return new AliasNodeWidget(Graph, nodeInfo);
        }
    }


    public class AliasNodeWidget : NodeWidget
    {
        public AliasNodeWidget(NodeGraphView graphView, INodeInfo node) : base(graphView, node)
        {
			this.HideLabel = true;
        }

        public override IWidgetView CreateDefaultView()
        {
			if (ParentNode is GetAliasNode getAliasNode)
				return new GetAliasNodeWidgetView(this);
			else if (ParentNode is CreateAliasNode createAliasNode)
				return new CreateAliasNodeWidgetView(this);
			else
				return new NodeWidgetView(this);
        }
    }



    public class GetAliasNodeWidgetView : NodeWidgetView
    {
        public GetAliasNodeWidgetView(NodeWidget nodeWidget) : base(nodeWidget)
        {
        }


        public override void UpdateLayout(SKStyleCache StyleCache)
        {
            // box anchor (0,0) is at top-left!
            string AliasName = (SourceNodeWidget.Label.Length > 0) ? SourceNodeWidget.Label : "(Get Alias)";
            this.DrawLabel = AliasName;
            this.LabelOrigin = Vector2f.Zero;

            NodeOutputPinWidget OutputPin = SourceNodeWidget.OutputWidgets[0];
            OutputPin.OverrideName = AliasName;
            OutputPin.GetActiveView()?.UpdateLayout(StyleCache);
            AxisAlignedBox2f outputBounds = OutputPin.GetActiveView()?.BoundsQuery(null) ?? AxisAlignedBox2f.Empty;

            float MaxHeight = outputBounds.Height;
            float TopMargin = 4, BottomMargin = 7;
            float LeftMargin = 5, RightMargin = -5;

            AxisAlignedBox2f outputPinBox = new AxisAlignedBox2f(LeftMargin, TopMargin, LeftMargin + outputBounds.Width, TopMargin + outputBounds.Height);
            SourceNodeWidget.OutputWidgetAnchors[0].Box = outputPinBox;

            AxisAlignedBox2f NodeBox = new AxisAlignedBox2f(
                0, 0, LeftMargin + outputBounds.Width + RightMargin, MaxHeight + TopMargin + BottomMargin);
            this.LocalNodeBounds = NodeBox;
            this.ChildBounds = NodeBox;
        }
    }




	public class CreateAliasNodeWidgetView : NodeWidgetView
	{
		public CreateAliasNodeWidgetView(NodeWidget nodeWidget) : base(nodeWidget)
		{
		}

		public override void UpdateLayout(SKStyleCache StyleCache)
		{
			// box anchor (0,0) is at top-left!
			string UseLabel = "";
			this.DrawLabel = UseLabel;
			this.LabelOrigin = Vector2f.Zero;

			NodeInputPinWidget NameInput = SourceNodeWidget.InputWidgets[0];
            NameInput.GetActiveView()?.UpdateLayout(StyleCache);
			AxisAlignedBox2f nameBounds = NameInput.GetActiveView()?.BoundsQuery(null) ?? AxisAlignedBox2f.Empty;

            NodeInputPinWidget ValueInput = SourceNodeWidget.InputWidgets[1];
            ValueInput.GetActiveView()?.UpdateLayout(StyleCache);
            AxisAlignedBox2f valueBounds = ValueInput.GetActiveView()?.BoundsQuery(null) ?? AxisAlignedBox2f.Empty;

            float MaxHeight = Math.Max(nameBounds.Height, valueBounds.Height);
			float TopMargin = 4, BottomMargin = 7;
			float LeftMargin = -5, RightMargin = 5;

			AxisAlignedBox2f valuePinBox = new AxisAlignedBox2f(LeftMargin, TopMargin, LeftMargin + valueBounds.Width, TopMargin + MaxHeight);
			SourceNodeWidget.InputWidgetAnchors[1].Box = valuePinBox;

            AxisAlignedBox2f nameEntryBox = valuePinBox.Translated(valuePinBox.Width, 0);
            nameEntryBox.SetWidth(nameBounds.Width, AxisAlignedBox2f.ScaleMode.ScaleRight);
            SourceNodeWidget.InputWidgetAnchors[0].Box = nameEntryBox;

            AxisAlignedBox2f NodeBox = new AxisAlignedBox2f(0, 0, nameEntryBox.Max.x + RightMargin, nameEntryBox.Max.y + BottomMargin);
			this.LocalNodeBounds = NodeBox;
			this.ChildBounds = NodeBox;
        }

	}

}
