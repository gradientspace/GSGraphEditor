// Copyright Gradientspace Corp. All Rights Reserved.
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


        public bool GetVariableInfoAtNode(int NodeID, out VariableInfo varInfo)
        {
            foreach (VariableInfo variableInfo in Variables) {
                if (variableInfo.CreatedAtNodeID == NodeID) {
                    varInfo = variableInfo;
                    return true;
                }
            }
            varInfo = new VariableInfo();
            return false;
        }

        public bool FindVariableByName(string Name, out VariableInfo varInfo)
        {
            foreach (VariableInfo variableInfo in Variables) {
                if ( String.Compare(variableInfo.Name, Name, true) == 0 ) {
                    varInfo = variableInfo;
                    return true;
                }
            }
            varInfo = new VariableInfo();
            return false;
        }


        public bool CanRenameVariable(int NodeIdentifier, string FromName, string ToName)
        {
            if (String.Compare(FromName, ToName, true) == 0)
                return false;

            if (GetVariableInfoAtNode(NodeIdentifier, out var varInfo) == false)
                return false;

            if (FindVariableByName(ToName, out var existingVarWithName)) 
                return false;

            return true;
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

			GraphTraversalUtils.TraverseAllSequencePaths(Graph, 
				(NodeBase node) => {
					if (node is DefineVariableBaseNode varNode)
						add_new_variable(varNode);
				},
				(NodeBase node, IConnectionInfo connInfo, GraphTraversalUtils.ScopeType scopeType, bool bIsOpeningScope) => {
                    string PushPop = bIsOpeningScope ? "Push" : "Pop";
                    if (bPrintDebug)
						Debug.WriteLine($"{PushPop}Scope: {node.GetNodeName()}:{connInfo.FromNodeOutputName}"); 
				}
			);
		}


	}
}
