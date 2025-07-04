using g3;
using Gradientspace.NodeGraph;
using Gradientspace.UI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GSNodeEditor
{
	public partial class NodeGraphEditor
	{

		// THINGS TO DO FOR UNDO REDO
		//  - mark_modified_node() not doing anything on _internal calls
		//  - how to call BeginGraphEdits() / EndGraphEdits() ? to do process_modified_nodes(), ValidateDataConnections(), etc
		//  - add/remove pin edits
		//  - node value changes



		// construct an add/remove for a Node, saving necessary state
		internal NodeAddedRemovedChange? make_add_remove_node_change(NodeWidget fromWidget, bool bIsRemove)
		{
			if (ActiveHistory == null)
				return null;

			if (fromWidget.ParentNode == null)
				throw new Exception("NodeGraphEditor.make_add_remove_node_change() - widget's node is null");

			INode graphNode = fromWidget.ParentNode;

			NodeType? nodeType = null;
			List<Tuple<string, object>>? customDataItems = null;
			if (fromWidget.ParentNode is NodeBase baseNode)
			{
				nodeType = baseNode.LibraryNodeType ?? new NodeType(baseNode.GetType());
				baseNode.CollectCustomDataItems(out customDataItems);
			} else
				nodeType = new NodeType(graphNode.GetType());

			List<SerializationUtil.InputConstant>? SavedConstants = null;
			SerializationUtil.SaveInputConstants(graphNode, out SavedConstants);
			
			NodeAddedRemovedChange result = new(this, nodeType, fromWidget.GraphNodeIdentifier, bIsRemove) {
				Position = fromWidget.Position,
				CustomDataItems = customDataItems,
				ConstantValues = SavedConstants
			};

			return result;
		}

		internal void ApplyChange(NodeAddedRemovedChange change, bool bApply)
		{
			if (bApply)
			{
				NodeWidget newWidget = add_node_of_type_internal(change.nodeType, change.Position, change.newNodeIdentifier,
					(INodeInfo nodeInfo) => {
						// deserialize stored node info back onto node after it is created
						if (nodeInfo.Node is NodeBase baseNode) {
							if (change.CustomDataItems != null)
								baseNode.RestoreCustomDataItems(change.CustomDataItems);
						}
						if (change.ConstantValues != null) 
							SerializationUtil.RestoreInputConstants(nodeInfo.Node!, change.ConstantValues);
					});
			}
			else
				remove_node_internal(change.newNodeIdentifier);
		}


		internal void ApplyChange(AddRemoveConnectionChange change, bool bApply)
		{
			if (bApply)
				add_connection_internal(change.Connection);
			else
				remove_connection_internal(change.Connection);
		}

	}


	public abstract class BaseNodeGraphEditorChange : BaseGraphEditChange
	{
		public NodeGraphEditor? GraphEditor = null;
	}

	public class NodeAddedRemovedChange : BaseNodeGraphEditorChange
	{
		public NodeType nodeType;
		public int newNodeIdentifier;
		public bool bIsRemove = false;

		public Vector2f Position;
		public List<Tuple<string, object>>? CustomDataItems = null;
		public List<SerializationUtil.InputConstant>? ConstantValues = null;

		public NodeAddedRemovedChange(NodeGraphEditor editor, NodeType type, int identifier, bool isRemove) {
			Name = (isRemove) ? "Remove Node" : "Add Node";
			this.GraphEditor = editor;
			nodeType = type;
			newNodeIdentifier = identifier;
			bIsRemove = isRemove;
		}

		public override void Apply() {
			GraphEditor?.ApplyChange(this, bIsRemove ? false : true);
		}
		public override void Revert(){
			GraphEditor?.ApplyChange(this, bIsRemove ? true : false);
		}
	}


	public class AddRemoveConnectionChange : BaseNodeGraphEditorChange
	{
		public IConnectionInfo Connection;
		public bool bIsRemove = false;
		public AddRemoveConnectionChange(NodeGraphEditor editor, IConnectionInfo connection, bool isRemove)
		{
			Name = (isRemove) ? "Remove Connection" : "Add Connection";
			this.GraphEditor = editor;
			Connection = connection;
			bIsRemove = isRemove;
		}

		public override void Apply() {
			GraphEditor?.ApplyChange(this, bIsRemove ? false : true);
		}
		public override void Revert() {
			GraphEditor?.ApplyChange(this, bIsRemove ? true : false);
		}
	}

}
