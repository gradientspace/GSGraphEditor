using g3;
using Gradientspace.NodeGraph;
using Gradientspace.UI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using static Gradientspace.NodeGraph.DataFlowGraph;

namespace GSNodeEditor
{

    public interface INodeGraphEditManager
    {
        void ExecuteGraphEdit(Action<NodeGraphEditor> EditFunc);
    }


    public partial class NodeGraphEditor
    {
        protected NodeGraphView GraphView;

        public NodeGraphEditor(NodeGraphView graphView)
        {
            GraphView = graphView;
        }

        protected INodeGraph Graph { get { return GraphView.GetGraph(); } }



        bool in_graph_edits = false;
        GraphEditHistory? ActiveHistory = null;

        public void BeginGraphEdits(GraphEditHistory? UseHistory = null)
        {
            Debug.Assert(in_graph_edits == false);
            in_graph_edits = true;

            ActiveHistory = UseHistory;
            if (ActiveHistory != null)
                ActiveHistory.BeginChanges("Graph Edit");
		}
        public void EndGraphEdits()
        {
            Debug.Assert(in_graph_edits);

            if (ActiveHistory != null) {
                ActiveHistory.EndChanges();
                ActiveHistory = null;
            }

            process_modified_nodes();

            in_graph_edits = false;

			// HACK TODO
			// re-validate all connections in graph after any edit
			// probably this should be more granular and only check ModifiedNodes...
			(Graph as BaseGraph)?.ValidateDataConnections();
            GraphView.UpdateAllConnections();
		}
        public bool IsInGraphEdits { get { return in_graph_edits; } }


        protected List<INode> ModifiedNodes = new List<INode>();

        protected void mark_modified_node(INode? node)
        {
            if (node != null && ModifiedNodes.Contains(node) == false)
                ModifiedNodes.Add(node);
        }
        protected void process_modified_nodes()
        {
            List<IConnectionInfo> connections = new List<IConnectionInfo>();    // temp array

			// need to call INode_DynamicOutputs.UpdateDynamicOutputs() on modified dynamic nodes,
			// however this update may have to be propagated to a downstream connected INode_DynamicOutputs node,
            // so we do passes until we don't find any more nodes to update
            // TODO: this could result in some weirdness if ModifiedNodes initially contains a node and one of it's downstream node?
            //   Do we actually need to construct an initial ordering first??
			while (ModifiedNodes.Count > 0)
            {
				List<INode> DownstreamModifiedNodes = new List<INode>();

				foreach (INode node in ModifiedNodes) {
                    int identifier = (node as NodeBase)!.GraphIdentifier;       // todo store NodeHandle instead

					INode_DynamicOutputs? dynamic_node = node as INode_DynamicOutputs;
                    if (dynamic_node != null) {
                        dynamic_node.UpdateDynamicOutputs(GraphView.GetGraph());

                        connections.Clear();
                        Graph.FindAllNodeConnections(identifier, ref connections, EConnectionType.Data);        // would be nice to have Graph.EnumerateAllNodeConnections()
						foreach (IConnectionInfo connInfo in connections) {
                            if ( connInfo.FromNodeIdentifier == identifier ) {
                                INodeInfo found = Graph.FindNodeFromIdentifier(connInfo.ToNodeIdentifier);
                                if (found.IsValid 
                                    && found.Node is INode_DynamicOutputs
                                    && DownstreamModifiedNodes.Contains(found.Node!) == false)
                                {
                                    DownstreamModifiedNodes.Add(found.Node!);
                                }
                            }
                        }
					}
				}

                ModifiedNodes = DownstreamModifiedNodes;
			}

            ModifiedNodes.Clear();
        }





        public virtual NodeWidget AddNodeOfType(
            NodeType nodeType, 
            Vector2f AtPosition,
            Action<INodeInfo>? NodeInitializerFunc = null )
        {
            Debug.Assert(IsInGraphEdits);
            NodeWidget newWidget = add_node_of_type_internal(nodeType, AtPosition, -1, NodeInitializerFunc);

			NodeAddedRemovedChange? change = make_add_remove_node_change(newWidget, false);
            ActiveHistory?.AppendChange(change);

			return newWidget;
        }
        protected virtual NodeWidget add_node_of_type_internal(NodeType nodeType, Vector2f AtPosition, int UseSpecifiedNodeIdentifier = -1,
			Action<INodeInfo>? NodeInitializerFunc = null)
        {
			INodeInfo NewNodeInfo = Graph.CreateNewNodeOfType(nodeType, UseSpecifiedNodeIdentifier);
            if (NodeInitializerFunc != null)
                NodeInitializerFunc(NewNodeInfo);
			NodeWidget NewNodeWidget = GraphView.CreateAndInitializeNewNodeWidget(NewNodeInfo);
			NewNodeWidget.Position = AtPosition;
			return NewNodeWidget;
		}


        public virtual bool RemoveNode(NodeWidget widget)
        {
			Debug.Assert(IsInGraphEdits);

			int NodeIdentifier = widget.GraphNodeIdentifier;

            // save state of node in change object (returns null if history is not active)
            NodeAddedRemovedChange? change = make_add_remove_node_change(widget, true);

			bool bAllConnectionsOK = true;
            List<IConnectionInfo> connections = new List<IConnectionInfo>();
            foreach (EConnectionType connectionType in Enum.GetValues<EConnectionType>())
            {
                Graph.FindAllNodeConnections(NodeIdentifier, ref connections, connectionType);
                foreach (IConnectionInfo connectionInfo in connections)
                {
                    if (!RemoveConnection(connectionInfo))
                        bAllConnectionsOK = false;
                }
                connections.Clear();
            }

            bool bNodeOK = remove_node_internal(NodeIdentifier);
            if (!bAllConnectionsOK || !bNodeOK)
                throw new Exception("NodeGraphEditor.RemoveNode: failed to remove something...");

            ActiveHistory?.AppendChange(change);

			return bAllConnectionsOK && bNodeOK;
        }
		protected virtual bool remove_node_internal(int NodeIdentifier)
		{
			bool bNodeOK = Graph.RemoveNode(NodeIdentifier);
			bool bNodeViewOK = GraphView.RemoveNode(NodeIdentifier);
            return bNodeOK && bNodeViewOK;
		}




		public virtual void AddConnection(
            NodeWidget FromNode, int FromNodePin,
            NodeWidget ToNode, int ToNodePin,
            EConnectionType connectionType,
            bool bReplaceExisting,
            bool bTryAutoConnectionSequence)
        {
			Debug.Assert(IsInGraphEdits);

			IConnectionInfo NewConnectionInfo = new IConnectionInfo();
            NewConnectionInfo.ConnectionType = connectionType;
            NewConnectionInfo.FromNodeIdentifier = FromNode.GraphNodeIdentifier;
            NewConnectionInfo.ToNodeIdentifier = ToNode.GraphNodeIdentifier;

            if (connectionType == EConnectionType.Data)
            {
                NewConnectionInfo.FromNodeOutputName = FromNode.OutputWidgets[FromNodePin].OutputName;
                NewConnectionInfo.ToNodeInputName = ToNode.InputWidgets[ToNodePin].InputName;
            }
            else
            {
                if (FromNodePin >= 0)
                    NewConnectionInfo.FromNodeOutputName = FromNode.OutputWidgets[FromNodePin].OutputName;
            }

            if (bReplaceExisting)
            {
                List<IConnectionInfo> ExistingConnections = new List<IConnectionInfo>();
                bool bFailedToRemoveExisting = false;
                if (NewConnectionInfo.ConnectionType == EConnectionType.Sequence)
                {
                    Graph.FindConnectionsFrom(NewConnectionInfo.FromNodeIdentifier, NewConnectionInfo.FromNodeOutputName, ref ExistingConnections, EConnectionType.Sequence);
                    if ( ExistingConnections.Count == 1 && FromNode.OutputSequenceWidget != null)
                    {
                        bFailedToRemoveExisting = (RemoveAllConnectionsFromSequencePin(FromNode.OutputSequenceWidget!) == false);
                    }
                } else if (NewConnectionInfo.ConnectionType == EConnectionType.Data)
                {
                    IConnectionInfo ExistingConnection = Graph.FindConnectionTo(NewConnectionInfo.ToNodeIdentifier, NewConnectionInfo.ToNodeInputName, EConnectionType.Data);
                    if (ExistingConnection.IsValid)
                        bFailedToRemoveExisting = (RemoveAllConnectionsToInput(ToNode, NewConnectionInfo.ToNodeInputName) == false);
                }
                if (bFailedToRemoveExisting) {
                    Debug.WriteLine("NodeGraphEditor.AddConnection: failed to remove existing connection");
                    return;
                }
            }


            bool bOK = add_connection_internal(NewConnectionInfo);
            if (!bOK)
                return;
            ActiveHistory?.AppendChange(new AddRemoveConnectionChange(this, NewConnectionInfo, false));

            // try to auto-connect sequence pin if we wired up a data pin and there is no current sequence connection
            if (connectionType == EConnectionType.Data 
                && bTryAutoConnectionSequence
                && FromNode.OutputSequenceWidget != null 
                && ToNode.InputSequenceWidget != null)
            {
                List<IConnectionInfo> ExistingSeqOuts = new List<IConnectionInfo>();
                // TODO: can we be smart about which sequence pin on things like a For node? 
				Graph.FindConnectionsFrom(NewConnectionInfo.FromNodeIdentifier, "", ref ExistingSeqOuts, EConnectionType.Sequence);
				IConnectionInfo ExistingSeqTo = Graph.FindConnectionTo(NewConnectionInfo.ToNodeIdentifier, "", EConnectionType.Sequence);
                if (ExistingSeqOuts.Count == 0 && ExistingSeqTo.IsValid == false)
                {
                    AddConnection(FromNode, -1, ToNode, -1, EConnectionType.Sequence, false, false);
				}
			}
			
        }
        protected virtual bool add_connection_internal(IConnectionInfo connectionInfo)
        {
			bool bOK = Graph.TryAddNewConnection(connectionInfo);
            if (!bOK)
				throw new Exception("NodeGraphEditor.add_connection_internal: Graph.TryAddNewConnection failed for valid new Connection!");
			ConnectionView? NewConnection = GraphView.AddConnection(connectionInfo);
			if (NewConnection == null) {
				throw new Exception("NodeGraphEditor.add_connection_internal: GraphView.AddConnection failed for valid new Connection!");
			}

			INodeInfo FromNodeInfo = Graph.FindNodeFromIdentifier(connectionInfo.FromNodeIdentifier);
			INodeInfo ToNodeInfo = Graph.FindNodeFromIdentifier(connectionInfo.ToNodeIdentifier);
			mark_modified_node(FromNodeInfo.Node);
			mark_modified_node(ToNodeInfo.Node);

			return true;
		}




        public virtual bool RemoveConnection(IConnectionInfo connectionInfo)
        {
			Debug.Assert(IsInGraphEdits);

            bool bOK = remove_connection_internal(connectionInfo);
            if (!bOK)
                return false;

            ActiveHistory?.AppendChange(new AddRemoveConnectionChange(this, connectionInfo, true));
            return true;
        }
        protected bool remove_connection_internal(IConnectionInfo connectionInfo)
        {
			INodeInfo FromNodeInfo = Graph.FindNodeFromIdentifier(connectionInfo.FromNodeIdentifier);
			INodeInfo ToNodeInfo = Graph.FindNodeFromIdentifier(connectionInfo.ToNodeIdentifier);

			bool bRemovedFromGraph = Graph.RemoveConnection(connectionInfo);
			bool bRemovedFromView = GraphView.RemoveConnection(connectionInfo);
			Debug.Assert(bRemovedFromGraph && bRemovedFromView);

			if (bRemovedFromGraph) {
				mark_modified_node(FromNodeInfo.Node);
				mark_modified_node(ToNodeInfo.Node);
			}
			return (bRemovedFromGraph && bRemovedFromView);
		}



        /*
         * Edits below are helper functions that call RemoveConnection() to actually make graph changes
         */

        public virtual bool RemoveAllConnectionsToInput(NodeWidget Node, string InputName)
        {
			Debug.Assert(IsInGraphEdits);

			IConnectionInfo graphConnection = Graph.FindConnectionTo(Node.GraphNodeIdentifier, InputName);
            if (graphConnection == IConnectionInfo.Invalid)
                return false;
            return RemoveConnection(graphConnection);
        }
        public virtual bool RemoveAllConnectionsToInput(NodeInputPinWidget inputPinWidget)
        {
			Debug.Assert(IsInGraphEdits);

			NodeWidget? parentWidget = inputPinWidget.ParentWidget as NodeWidget;
            if (parentWidget != null) {
                return RemoveAllConnectionsToInput(parentWidget, inputPinWidget.InputName);
            }
            return false;
        }


        public virtual bool RemoveAllConnectionsFromOutput(NodeWidget Node, string OutputName)
        {
			Debug.Assert(IsInGraphEdits);

			bool bAllOK = true;
            List<IConnectionInfo> graphConnections = new List<IConnectionInfo>();
            for (int j = 0; j < 2; ++j) {
                EConnectionType connectionType = (j == 0) ? EConnectionType.Data : EConnectionType.Sequence;
                graphConnections.Clear();
                Graph.FindConnectionsFrom(Node.GraphNodeIdentifier, OutputName, ref graphConnections, connectionType);
                foreach (IConnectionInfo c in graphConnections) {
                    if (!RemoveConnection(c))
                        bAllOK = false;
                }
            }
            return bAllOK;
        }
        public virtual bool RemoveAllConnectionsFromOutput(NodeOutputPinWidget outputPinWidget)
        {
			Debug.Assert(IsInGraphEdits);

			NodeWidget? parentWidget = outputPinWidget.ParentWidget as NodeWidget;
            if (parentWidget != null) {
                return RemoveAllConnectionsFromOutput(parentWidget, outputPinWidget.OutputName);
            }
            return false;
        }

        public virtual bool RemoveAllConnectionsFromSequencePin(NodeExecPinWidget pinWidget)
        {
			Debug.Assert(IsInGraphEdits);

			NodeWidget? nodeWidget = pinWidget.ParentWidget as NodeWidget;
            if (nodeWidget == null)
                return false;

            List<IConnectionInfo> graphConnections = new List<IConnectionInfo>();
            if (pinWidget is NodeInputExecPinWidget) {
                graphConnections.Add( Graph.FindConnectionTo(nodeWidget.GraphNodeIdentifier, "", EConnectionType.Sequence) );
            } else if (pinWidget is NodeOutputExecPinWidget) { 
                Graph.FindConnectionsFrom(nodeWidget.GraphNodeIdentifier, "", ref graphConnections, EConnectionType.Sequence);
            }
            bool bAllOK = true;
            foreach (IConnectionInfo c in graphConnections)
            {
                if (c.IsValid && RemoveConnection(c) == false)
                    bAllOK = false;
            }
            return bAllOK;
        }




        /**
         * Variable-pin support
         */ 

        public virtual void AddInputPinToNode(NodeWidget nodeWidget)
        {
			Debug.Assert(IsInGraphEdits);
            INode_VariableInputs? variableNode = (nodeWidget.ParentNode as INode_VariableInputs);
            if ( variableNode != null ) {
                if (variableNode.AddInput())
                    mark_modified_node(nodeWidget.ParentNode);
            }
        }

        public virtual void RemoveInputPinFromNode(NodeWidget nodeWidget)
        {
			Debug.Assert(IsInGraphEdits);

			if (nodeWidget.InputWidgets.Count <= 1) return;
            NodeInputPinWidget lastInput = nodeWidget.InputWidgets.Last();
            IConnectionInfo graphConnection = Graph.FindConnectionTo(nodeWidget.GraphNodeIdentifier, lastInput.InputName);

			INode_VariableInputs? variableNode = (nodeWidget.ParentNode as INode_VariableInputs);
            if ( variableNode != null ) {
                if (variableNode.RemoveInput()) {
                    if (graphConnection.IsValid)
                        RemoveConnection(graphConnection);
                    mark_modified_node(nodeWidget.ParentNode);
                }
			}
        }



        public virtual void AddOutputPinToNode(NodeWidget nodeWidget)
        {
			Debug.Assert(IsInGraphEdits);

			INode_VariableOutputs? variableNode = (nodeWidget.ParentNode as INode_VariableOutputs);
            if ( variableNode != null ) {
                if (variableNode.AddOutput())
                    mark_modified_node(nodeWidget.ParentNode);
            }
        }

        public virtual void RemoveOutputPinFromNode(NodeWidget nodeWidget)
        {
			Debug.Assert(IsInGraphEdits);

			if (nodeWidget.OutputWidgets.Count <= 1) return;
            NodeOutputPinWidget lastOutput = nodeWidget.OutputWidgets.Last();

            List<IConnectionInfo> Connections = new List<IConnectionInfo>();
            if ( lastOutput.IsSequenceOutputPin )
                Graph.FindConnectionsFrom(nodeWidget.GraphNodeIdentifier, lastOutput.OutputName, ref Connections, EConnectionType.Sequence);
            else
                Graph.FindConnectionsFrom(nodeWidget.GraphNodeIdentifier, lastOutput.OutputName, ref Connections, EConnectionType.Data);

			INode_VariableOutputs? variableNode = (nodeWidget.ParentNode as INode_VariableOutputs);
            if ( variableNode != null ) {
                if (variableNode.RemoveOutput()) {
					foreach (IConnectionInfo connection in Connections)
						RemoveConnection(connection);
					mark_modified_node(nodeWidget.ParentNode);
                }
			}


        }

    }


}
