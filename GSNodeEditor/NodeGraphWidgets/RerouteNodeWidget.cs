// Copyright Gradientspace Corp. All Rights Reserved.
using g3;
using Gradientspace.NodeGraph;
using Gradientspace.NodeGraph.Nodes;
using Gradientspace.UI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GSNodeEditor
{
    public class RerouteNodeWidgetProvider : INodeWidgetProvider
    {
        public NodeWidget? CreateNewWidget(NodeGraphView Graph, INodeInfo nodeInfo)
        {
            return new RerouteNodeWidget(Graph, nodeInfo);
        }
    }

    public class RerouteNodeWidget : NodeWidget
    {
        public RerouteNodeWidget(NodeGraphView graphView, INodeInfo node) : base(graphView, node)
        {
        }

        public override IWidgetView CreateDefaultView()
        {
            return new RerouteNodeWidgetView(this);
        }
    }


    public class RerouteNodeWidgetView : NodeWidgetView
    {

        public RerouteNodeWidgetView(NodeWidget nodeWidget) : base(nodeWidget)
        {
        }

        public override void UpdateLayout(SKStyleCache StyleCache)
        {
            // box anchor (0,0) is at top-left!

            // currently drawing Reroute as the input and output pins directly adjacent (ie making a square)
            // with small top and bottom rounded-margins

            NodeInputPinWidget InputPin = SourceNodeWidget.InputWidgets[0];
            InputPin.GetActiveView()?.UpdateLayout(StyleCache);
            AxisAlignedBox2f inputBounds = InputPin.GetActiveView()?.BoundsQuery(null) ?? AxisAlignedBox2f.Empty;

            NodeOutputPinWidget OutputPin = SourceNodeWidget.OutputWidgets[0];
            OutputPin.GetActiveView()?.UpdateLayout(StyleCache);
            AxisAlignedBox2f outputBounds = OutputPin.GetActiveView()?.BoundsQuery(null) ?? AxisAlignedBox2f.Empty;

            float MaxPinHeight = Math.Max(inputBounds.Height, outputBounds.Height);

            this.DrawLabel = "";
            float LeftRightExtend = 0;
            float TopHeight = 7, BottomHeight = 7;

            float PinStartOffsetY = TopHeight;
            float PinCenterX = inputBounds.Width;

            AxisAlignedBox2f inputPinBox = new AxisAlignedBox2f(PinCenterX - inputBounds.Width, PinStartOffsetY, PinCenterX, PinStartOffsetY + inputBounds.Height);
            SourceNodeWidget.InputWidgetAnchors[0].Box = inputPinBox;

            AxisAlignedBox2f outputPinBox = new AxisAlignedBox2f(PinCenterX, PinStartOffsetY, PinCenterX + outputBounds.Width, PinStartOffsetY + outputBounds.Height);
            SourceNodeWidget.OutputWidgetAnchors[0].Box = outputPinBox;

            AxisAlignedBox2f NodeBox = new AxisAlignedBox2f(
                inputPinBox.Min.x + LeftRightExtend, 0, outputPinBox.Max.x - LeftRightExtend, MaxPinHeight + TopHeight + BottomHeight);
            this.LocalNodeBounds = NodeBox;
            this.ChildBounds = NodeBox;
            //this.ChildBounds.Contain(inputPinBox);
            //this.ChildBounds.Contain(outputPinBox);
        }

    }

}
