using Gradientspace.NodeGraph;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace GSNodeEditor
{
	public class VariablesTracker
	{
		public ExecutionGraph Graph { get; protected set; }

		public VariablesTracker(ExecutionGraph graph)
		{
			Graph = graph;
		}


		public void Rebuild()
		{
			rebuild_internal();
		}




		protected struct VariableInfo
		{
			public string Name;
			public int CreatedAtNodeID;
		}

		protected List<VariableInfo> Variables = new List<VariableInfo>();


		void add_new_variable(DefineVariableBaseNode variableNode)
		{
			VariableInfo v = new VariableInfo();
			v.Name = variableNode.GetVariableName();
			v.CreatedAtNodeID = variableNode.GraphIdentifier;
			Variables.Add(v);
		}



		void rebuild_internal()
		{
			Variables = new List<VariableInfo>();

			GraphTraversalUtils.TraverseAllSequencePaths(Graph, Graph.StartNodeHandle,
				(NodeBase node) => {
				if (node is DefineVariableBaseNode varNode)
					add_new_variable(varNode);
				},
				(NodeBase node, IConnectionInfo connInfo) => { Debug.WriteLine($"PushScope: {node.GetNodeName()}:{connInfo.FromNodeOutputName}"); },
				(NodeBase node, IConnectionInfo connInfo) => { Debug.WriteLine($"PopScope: {node.GetNodeName()}:{connInfo.FromNodeOutputName}"); }
				);
		}


	}
}
