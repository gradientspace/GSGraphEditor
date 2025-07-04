using Gradientspace.NodeGraph;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GSNodeEditor
{
    public class BaseGraphEditor : NodeGraphEditor
    {
        protected BaseGraph BaseGraph;

        public BaseGraphEditor(NodeGraphView graphView) : base(graphView)
        {
            BaseGraph = (BaseGraph)base.Graph;
            Debug.Assert(BaseGraph != null);
        }


        public override void AddConnection(
            NodeWidget FromNode, int FromNodePin,
            NodeWidget ToNode, int ToNodePin,
            EConnectionType connectionType,
            bool bReplaceExisting,
            bool bTryAutoConnectionSequence)
        {
            // if a data connection is made to a placeholder node, we need to remove the placeholder node 
            // and replace it with a non-placeholder instance first
            // Not clear if this still needs to be in a BaseGraphEditor subclass...
            if (connectionType == EConnectionType.Data && ToNode.IsPlaceholderNode) {
                AddPlaceholderConnection(FromNode, FromNodePin, ToNode, ToNodePin, bTryAutoConnectionSequence);
                return;
            }

            base.AddConnection(FromNode, FromNodePin, ToNode, ToNodePin, connectionType, bReplaceExisting, bTryAutoConnectionSequence);
        }

        private void AddPlaceholderConnection(
            NodeWidget FromNode, int FromNodePin,
            NodeWidget ToNode, int ToNodePin,
            bool bTryAutoConnectionSequence )
        {
            PlaceholderNodeBase PlaceholderNode = (PlaceholderNodeBase)ToNode.ParentNode!;
            INodeInputInfo inputInfo = ToNode.InputWidgets[ToNodePin].NodeInputInfo;
            GraphDataType incomingType = FromNode.OutputWidgets[FromNodePin].DataType;

            bool bCanReplace = PlaceholderNode.GetPlaceholderReplacementNodeInfo(inputInfo.InputName, incomingType,
                out Type replacementNodeClassType, out string replacementInputName, out Action<INode, GraphDataType>? replacementNodeInitializer);
            NodeType? ReplacementNodeType = bCanReplace ? DefaultNodeLibrary.Instance.FindNodeType(replacementNodeClassType) : null;
            if (ReplacementNodeType != null)
            {
                // remove existing placeholder node
                this.RemoveNode(ToNode);

                // create new replacement node
                NodeWidget newNodeWidget = this.AddNodeOfType(ReplacementNodeType, ToNode.Position,
                    (INodeInfo newNodeInfo) => {
                        replacementNodeInitializer?.Invoke(newNodeInfo.Node!, incomingType);
                    });

                // find the new pin on the replacement node and add the new connection
                ToNodePin = newNodeWidget.FindInputPinIndexByName(replacementInputName);
                AddConnection(FromNode, FromNodePin, newNodeWidget, ToNodePin, EConnectionType.Data, false, bTryAutoConnectionSequence);
            }
            else
                Debug.WriteLine("BaseGraphEditor.AddConnection: failed to replace placeholder node");
        }


    }
}
