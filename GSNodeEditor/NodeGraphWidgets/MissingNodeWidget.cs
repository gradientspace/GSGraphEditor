// Copyright Gradientspace Corp. All Rights Reserved.
using Gradientspace.NodeGraph;


namespace GSNodeEditor
{
    public class MissingNodeWidgetProvider : INodeWidgetProvider
    {
        public NodeWidget? CreateNewWidget(NodeGraphView Graph, INodeInfo nodeInfo)
        {
            return new MissingNodeWidget(Graph, nodeInfo);
        }
    }

    // NodeWidget type for MissingNodeErrorNode (should not be spawned otherwise...)
    public class MissingNodeWidget : NodeWidget
    {
        public MissingNodeWidget(NodeGraphView graphView, INodeInfo node) : base(graphView, node)
        {
            WidgetStyle = NodeWidgetStyles.MissingNode;

            if ( node.Node is MissingNodeErrorNode missingNode )
            {
                SetNodeErrorState([
                    $"Node {missingNode.NodeName} was not found",
                    $"Type:    {missingNode.NodeClassType}",
                    $"Variant: {missingNode.NodeClassVariant}"
                ]);
            } else
                SetNodeErrorState(["Node Type was not found"]);
        }
    }

}
