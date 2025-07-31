// Copyright Gradientspace Corp. All Rights Reserved.
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
using static Gradientspace.NodeGraph.SerializationUtil;

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
            begin_graph_edits_internal();

			ActiveHistory = UseHistory;
            if (ActiveHistory != null)
                ActiveHistory.BeginChanges( new GraphEditChangeSequence(this) );
		}
        protected virtual void begin_graph_edits_internal()
        {
			Debug.Assert(in_graph_edits == false);
			in_graph_edits = true;
		}
        public void EndGraphEdits()
        {
			if (ActiveHistory != null) {
				ActiveHistory.EndChanges();
				ActiveHistory = null;
			}

            end_graph_edits_internal();
		}
		protected virtual void end_graph_edits_internal()
        {
			Debug.Assert(in_graph_edits);

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
        protected void mark_modified_node(int NodeIdentifier)
        {
            INodeInfo nodeInfo = Graph.FindNodeFromIdentifier(NodeIdentifier);
            if ( nodeInfo.Node != null && ModifiedNodes.Contains(nodeInfo.Node) == false)
                ModifiedNodes.Add(nodeInfo.Node);
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
            if (add_input_pin_to_node(nodeWidget))
                ActiveHistory?.AppendChange(new NodeAddRemoveInputChange(this, nodeWidget.GraphNodeIdentifier, false));
        }
        protected virtual bool add_input_pin_to_node(NodeWidget nodeWidget)
        {
			INode_VariableInputs? variableNode = (nodeWidget.ParentNode as INode_VariableInputs);
			if (variableNode != null) {
                if (variableNode.AddInput()) {
                    mark_modified_node(nodeWidget.ParentNode);
                    return true;
                }
			}
            return false;
		}



        public virtual void RemoveInputPinFromNode(NodeWidget nodeWidget)
        {
			Debug.Assert(IsInGraphEdits);
            if ( remove_input_pin_from_node(nodeWidget) )
				ActiveHistory?.AppendChange(new NodeAddRemoveInputChange(this, nodeWidget.GraphNodeIdentifier, true));
		}
        protected virtual bool remove_input_pin_from_node(NodeWidget nodeWidget)
        {
			if (nodeWidget.InputWidgets.Count <= 1) return false;
            NodeInputPinWidget lastInput = nodeWidget.InputWidgets.Last();
            IConnectionInfo graphConnection = Graph.FindConnectionTo(nodeWidget.GraphNodeIdentifier, lastInput.InputName);

			INode_VariableInputs? variableNode = (nodeWidget.ParentNode as INode_VariableInputs);
            if ( variableNode != null ) {
                if (variableNode.RemoveInput()) {
                    if (graphConnection.IsValid)
                        RemoveConnection(graphConnection);
                    mark_modified_node(nodeWidget.ParentNode);
                    return true;
                }
			}
            return false;
        }



        public virtual void AddOutputPinToNode(NodeWidget nodeWidget)
        {
			Debug.Assert(IsInGraphEdits);
            if (add_output_pin_to_node(nodeWidget))
				ActiveHistory?.AppendChange(new NodeAddRemoveOutputChange(this, nodeWidget.GraphNodeIdentifier, false));
		}
        protected virtual bool add_output_pin_to_node(NodeWidget nodeWidget)
        {
			INode_VariableOutputs? variableNode = (nodeWidget.ParentNode as INode_VariableOutputs);
			if (variableNode != null) {
				if (variableNode.AddOutput()) { 
					mark_modified_node(nodeWidget.ParentNode);
                    return true;
                }
			}
            return false;
		}



        public virtual void RemoveOutputPinFromNode(NodeWidget nodeWidget)
        {
			Debug.Assert(IsInGraphEdits);
			if (nodeWidget.OutputWidgets.Count <= 1) return;

            if (remove_output_pin_from_node(nodeWidget))
				ActiveHistory?.AppendChange(new NodeAddRemoveOutputChange(this, nodeWidget.GraphNodeIdentifier, true));
		}
        protected virtual bool remove_output_pin_from_node(NodeWidget nodeWidget)
        {
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
                    return true;
                }
			}
            return false;
        }



        // constant value edits

        public virtual void SetNodeConstantValue(int NodeIdentifier, string InputName, object NewValue)
        {
			// TODO: currently this function is only called by NodeInputPinWidget.UpdateInputFromModifiedTextEntry(),
			// because we have no way to signal back to the graph except by calling Node.PublishNodeModifiedNotification(),
            // which forces a full rebuild of the node (currently what undo/redo will do - horrible!)


			// TODO should we use SerialiationUtil.TryGetInputConstant here??
			(object? prevValue, bool bIsPrevDefined) = Graph.GetNodeConstantValue(NodeIdentifier, InputName);

            Graph.SetNodeConstantValue(NodeIdentifier, InputName, NewValue);

            // fetch value again in case SetNodeConstantValue modified it in some way
			(object? newValue, bool bIsNewDefined) = Graph.GetNodeConstantValue(NodeIdentifier, InputName);

			// if update was redundant 
			if ( (newValue == null && prevValue == null) 
                || (newValue != null && newValue.Equals(prevValue)) )
				return;

			if (ActiveHistory != null)
            {
				NodeConstantValueChange change = new NodeConstantValueChange(this, NodeIdentifier, 
                    new InputConstant() { InputName = InputName, Value = prevValue },
					new InputConstant() { InputName = InputName, Value = NewValue }
                );
                ActiveHistory.AppendChange(change);
			}

            // TODO: notify graph widget somehow?
		}


        /**
         * Try to rename a variable in the graph defined at NodeIdentifier (must be a DefineVariableBaseNode)
         * that is currently named FromName to a new name ToName.
         * Will fail (return false) if ToName already exists on some other variable
         */
        public virtual bool TryRenameVariable(int NodeIdentifier, string FromName, string ToName)
        {
            if (String.Compare(FromName, ToName, true) == 0)
                return false;

            // somewhere out there this already exists as part of a GraphStaticAnalyzer, but we
            // have no clean way to access it here...
            ExecutionGraph execGraph = (Graph as ExecutionGraph)!;
            VariablesTracker varTracker = new VariablesTracker(execGraph);
            varTracker.Rebuild();

            // make sure we can complete this rename
            if ( varTracker.CanRenameVariable(NodeIdentifier, FromName, ToName) == false) {
                GlobalGraphOutput.AppendLog($"Rename variable {FromName} to {ToName} ignored because a variable named {ToName} already exists");
                return false;
            }

            // update constant value on variable node
            Graph.SetNodeConstantValue(NodeIdentifier, DefineVariableBaseNode.NameInputName, ToName);
            ActiveHistory?.AppendChange(new NodeConstantValueChange(this, NodeIdentifier,
                new InputConstant() { InputName = DefineVariableBaseNode.NameInputName, Value = FromName },
                new InputConstant() { InputName = DefineVariableBaseNode.NameInputName, Value = ToName }));

            // update constant value on all other graph nodes using this variable name
            // (TODO: should this be based on scope? ie could use same local variable name in multiple places....)
            // ((maybe each variable should have a GUID like functions?))
            foreach (INodeInfo nodeInfo in execGraph.EnumerateNodes()) {
                if ( nodeInfo.Node is AccessVariableNode accessNode) {
                    if ( String.Compare(accessNode.GetVariableName(), FromName, true) == 0) {
                        Graph.SetNodeConstantValue(nodeInfo.Identifier, AccessVariableNode.NameInputName, ToName);
                        ActiveHistory?.AppendChange(new NodeConstantValueChange(this, nodeInfo.Identifier,
                            new InputConstant() { InputName = AccessVariableNode.NameInputName, Value = FromName },
                            new InputConstant() { InputName = AccessVariableNode.NameInputName, Value = ToName }));
                    }
                }
            }

            return true;
        }


	}


}
