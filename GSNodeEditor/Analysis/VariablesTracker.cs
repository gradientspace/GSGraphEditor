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

		public struct VariableInfo
		{
			public string Name;
			public Type VariableType;
			public int CreatedAtNodeID;
		}


		public IEnumerable<VariableInfo> EnumerateAllVariables()
		{
			foreach (VariableInfo variableInfo in Variables)
				yield return variableInfo;
		}



		protected List<VariableInfo> Variables = new List<VariableInfo>();


		void add_new_variable(DefineVariableBaseNode variableNode)
		{
			VariableInfo v = new VariableInfo();
			v.Name = variableNode.GetVariableName();
			v.VariableType = variableNode.GetVariableType();
			v.CreatedAtNodeID = variableNode.GraphIdentifier;
			Variables.Add(v);
		}



		void rebuild_internal()
		{
			Variables = new List<VariableInfo>();

			bool bPrintDebug = false;

			GraphTraversalUtils.TraverseAllSequencePaths(Graph, Graph.StartNodeHandle,
				(NodeBase node) => {
					if (node is DefineVariableBaseNode varNode)
						add_new_variable(varNode);
				},
				(NodeBase node, IConnectionInfo connInfo) => { 
					if (bPrintDebug)
						Debug.WriteLine($"PushScope: {node.GetNodeName()}:{connInfo.FromNodeOutputName}"); 
				},
				(NodeBase node, IConnectionInfo connInfo) => { 
					if (bPrintDebug)
						Debug.WriteLine($"PopScope: {node.GetNodeName()}:{connInfo.FromNodeOutputName}"); 
				}
			);
		}


	}
}
