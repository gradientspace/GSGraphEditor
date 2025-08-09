// Copyright Gradientspace Corp. All Rights Reserved.
using Gradientspace.NodeGraph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Gradientspace.NodeGraph.StandardVariables;
using static GSNodeEditor.VariablesTracker;

namespace GSNodeEditor
{
    public class GraphFunctionsTracker
    {
        public ExecutionGraph Graph { get; protected set; }


        public GraphFunctionsTracker(ExecutionGraph graph)
        {
            Graph = graph;
        }

        public void Rebuild()
        {
            rebuild_internal();
        }


        public struct FunctionInfo
        {
            public string Name;
            public string FunctionID;
            public int CreatedAtNodeID;
        }
        protected List<FunctionInfo> Functions = new List<FunctionInfo>();

        public IEnumerable<FunctionInfo> EnumerateAllFunctions()
        {
            foreach (FunctionInfo functionInfo in Functions)
                yield return functionInfo;
        }


        public bool GetFunctionInfoAtNode(int NodeID, out FunctionInfo varInfo)
        {
            foreach (FunctionInfo functionInfo in Functions) {
                if (functionInfo.CreatedAtNodeID == NodeID) {
                    varInfo = functionInfo;
                    return true;
                }
            }
            varInfo = new FunctionInfo();
            return false;
        }


        public bool FindFunctionByName(string Name, out FunctionInfo funcInfo)
        {
            foreach (FunctionInfo functionInfo in Functions) {
                if (String.Compare(functionInfo.Name, Name, true) == 0) {
                    funcInfo = functionInfo;
                    return true;
                }
            }
            funcInfo = new FunctionInfo();
            return false;
        }


        public bool FindFunctionByFunctionID(string FunctionID, out FunctionInfo funcInfo)
        {
            foreach (FunctionInfo functionInfo in Functions) {
                if (String.Compare(functionInfo.FunctionID, FunctionID, true) == 0) {
                    funcInfo = functionInfo;
                    return true;
                }
            }
            funcInfo = new FunctionInfo();
            return false;
        }


        public bool CanRenameFunction(int NodeIdentifier, string FromName, string ToName)
        {
            if (String.Compare(FromName, ToName, true) == 0)
                return false;

            if (GetFunctionInfoAtNode(NodeIdentifier, out var funcInfo) == false)
                return false;

            if (FindFunctionByName(ToName, out var existingFuncWithName))
                return false;

            return true;
        }


        void add_new_function(FunctionDefinitionNode funcNode)
        {
            FunctionInfo f = new FunctionInfo();
            f.Name = funcNode.FunctionName;
            f.FunctionID = funcNode.FunctionID;
            f.CreatedAtNodeID = funcNode.GraphIdentifier;
            Functions.Add(f);
        }

        void rebuild_internal()
        {
            Functions = new List<FunctionInfo>();

            foreach (FunctionDefinitionNode funcDefNode in Graph.EnumerateNodesOfType<FunctionDefinitionNode>()) {
                add_new_function(funcDefNode);
            }
        }
    }
}
