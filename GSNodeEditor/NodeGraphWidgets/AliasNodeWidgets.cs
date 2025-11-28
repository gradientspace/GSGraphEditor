using g3;
using Gradientspace.NodeGraph;
using Gradientspace.UI;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        }

        public override IWidgetView CreateDefaultView()
        {
            if (ParentNode is GetAliasNode getAliasNode)
                return new GetAliasNodeWidgetView(this);
            return new AliasNodeWidgetView(this);
        }
    }

    public class AliasNodeWidgetView : NodeWidgetView
    {
        public AliasNodeWidgetView(NodeWidget nodeWidget) : base(nodeWidget)
        {
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

            SKPaint LabelTextPaint =
                StyleCache.GetCachedPaint(SourceNodeWidget.WidgetStyle.NodeStyle.StandardStyle, SKStyleCache.EPaintType.Text);
            WidgetMargins LabelMargins = SourceNodeWidget.WidgetStyle.NodeStyle.BaseMargins;

            string UseLabel = (SourceNodeWidget.Label.Length > 0) ? SourceNodeWidget.Label : "(Get Alias)";
            TextHeightInfo LabelTextHeightInfo = StyleCache.GetCachedFontHeightInfo(SourceNodeWidget.WidgetStyle.NodeStyle.StandardStyle);
            float LabelWidth = LabelTextPaint.MeasureText(UseLabel);
            this.DrawLabel = UseLabel;

            NodeOutputPinWidget OutputPin = SourceNodeWidget.OutputWidgets[0];
            OutputPin.GetActiveView()?.UpdateLayout(StyleCache);
            AxisAlignedBox2f outputBounds = OutputPin.GetActiveView()?.BoundsQuery(null) ?? AxisAlignedBox2f.Empty;

            float MaxHeight = Math.Max(LabelTextHeightInfo.MaxTotalHeight, outputBounds.Height);
            float TopHeight = 5, BottomHeight = 5;
            float LeftMargin = 5;

            float PinStartOffsetY = TopHeight;
            float PinStartOffsetX = LeftMargin + LabelWidth + LeftMargin;

            AxisAlignedBox2f outputPinBox = new AxisAlignedBox2f(PinStartOffsetX, PinStartOffsetY, PinStartOffsetX + outputBounds.Width, PinStartOffsetY + outputBounds.Height);
            SourceNodeWidget.OutputWidgetAnchors[0].Box = outputPinBox;

            // todo this leaves it a bit high...
            LabelOrigin = new Vector2f(LeftMargin, TopHeight + LabelTextHeightInfo.AboveBaseline);

            AxisAlignedBox2f NodeBox = new AxisAlignedBox2f(
                0, 0, LabelWidth + 2*LeftMargin + outputBounds.Width/2, MaxHeight + TopHeight + BottomHeight);
            this.LocalNodeBounds = NodeBox;
            this.ChildBounds = NodeBox;
            //this.ChildBounds.Contain(inputPinBox);
            //this.ChildBounds.Contain(outputPinBox);
        }

    }

}
