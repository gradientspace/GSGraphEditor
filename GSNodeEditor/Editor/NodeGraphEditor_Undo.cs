// Copyright Gradientspace Corp. All Rights Reserved.
using g3;
using Gradientspace.NodeGraph;
using Gradientspace.UI;
using System;
using System.Diagnostics;

namespace GSNodeEditor
{
	public partial class NodeGraphEditor
	{
		// THINGS TO DO FOR UNDO REDO
		//  - some way to expire textentry edit changes after it goes out of focus
		//  - code nodes text


		internal void BeginApplyGraphHistoryChanges()
		{
			begin_graph_edits_internal();
		}

		internal void EndApplyGraphHistoryChanges()
		{
			end_graph_edits_internal();
		}


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


		internal void ApplyChange(NodeConstantValueChange change, bool bApply)
		{
			if (bApply)
				Graph.SetNodeConstantValue(change.NodeIdentifier, change.ToValue.InputName, change.ToValue.Value);
			else
				Graph.SetNodeConstantValue(change.NodeIdentifier, change.ToValue.InputName, change.FromValue.Value);

			// AAAAHHH forcing full node rebuild here because we currently do not have a way
			// to just update the values on the inline widgets. Forcing call to PinWidget.UpdateInlineInfo()
			// does not currently work because the function only builds new input widgets, and possibly will
			// not work for 
			if (Graph.FindNodeFromIdentifier(change.NodeIdentifier).Node is NodeBase baseNode)
				baseNode.PublishNodeModifiedNotification();
		}


		internal void ApplyChange(NodeAddRemoveInputChange change, bool bApply)
		{
			NodeWidget? foundWidget = GraphView.FindNode(change.NodeIdentifier);
			Debug.Assert(foundWidget != null);
			if (bApply)
				add_input_pin_to_node(foundWidget);
			else
				remove_input_pin_from_node(foundWidget);
		}

		internal void ApplyChange(NodeAddRemoveOutputChange change, bool bApply)
		{
			NodeWidget? foundWidget = GraphView.FindNode(change.NodeIdentifier);
			Debug.Assert(foundWidget != null);
			if (bApply)
				add_output_pin_to_node(foundWidget);
			else
				remove_output_pin_from_node(foundWidget);
		}


	}



	public class GraphEditChangeSequence : HistoryChangeSequence
	{
		public NodeGraphEditor? GraphEditor = null;

		public GraphEditChangeSequence(NodeGraphEditor? graphEditor) : base("Graph Edit", null)
		{
			GraphEditor = graphEditor;
		}

		// begin sequence / end sequence

		public override void Apply()
		{
			GraphEditor?.BeginApplyGraphHistoryChanges();
			base.Apply();
			GraphEditor?.EndApplyGraphHistoryChanges();
		}
		public override void Revert()
		{
			GraphEditor?.BeginApplyGraphHistoryChanges();
			base.Revert();
			GraphEditor?.EndApplyGraphHistoryChanges();
		}

		public override IHistoryChange? Simplify()
		{
			if (Changes.Count == 0)
				return null;
			if (Changes.Count == 1) 
				SetNameAndDescription(Changes[0].Name, Changes[0].Description);
			return this;			// cannot replace w/ child edit
		}
	}




	public abstract class BaseNodeGraphEditorChange : BaseHistoryChange
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


	public class NodeConstantValueChange : BaseNodeGraphEditorChange
	{
		public int NodeIdentifier;
		public SerializationUtil.InputConstant FromValue;
		public SerializationUtil.InputConstant ToValue;

		public NodeConstantValueChange(NodeGraphEditor editor, int nodeIdentifier, SerializationUtil.InputConstant fromValue, SerializationUtil.InputConstant toValue)
		{
			Name = "Edit Constant";
			this.GraphEditor = editor;
			this.NodeIdentifier = nodeIdentifier;
			this.FromValue = fromValue;
			this.ToValue = toValue;
		}

		public override void Apply() {
			GraphEditor?.ApplyChange(this, true);
		}
		public override void Revert() {
			GraphEditor?.ApplyChange(this, false);
		}
	}


	public class NodeAddRemoveInputChange : BaseNodeGraphEditorChange
	{
		public int NodeIdentifier;
		public bool bIsRemove = false;
		public NodeAddRemoveInputChange(NodeGraphEditor editor, int nodeIdentifier, bool isRemove)
		{
			Name = (isRemove) ? "Remove Input" : "Add Input";
			this.GraphEditor = editor;
			NodeIdentifier = nodeIdentifier;
			bIsRemove = isRemove;
		}

		public override void Apply() {
			GraphEditor?.ApplyChange(this, bIsRemove ? false : true);
		}
		public override void Revert() {
			GraphEditor?.ApplyChange(this, bIsRemove ? true : false);
		}
	}


	public class NodeAddRemoveOutputChange : BaseNodeGraphEditorChange
	{
		public int NodeIdentifier;
		public bool bIsRemove = false;
		public NodeAddRemoveOutputChange(NodeGraphEditor editor, int nodeIdentifier, bool isRemove)
		{
			Name = (isRemove) ? "Remove Output" : "Add Output";
			this.GraphEditor = editor;
			NodeIdentifier = nodeIdentifier;
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
