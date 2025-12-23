// Copyright Gradientspace Corp. All Rights Reserved.
using g3;
using Gradientspace.NodeGraph;
using Gradientspace.NodeGraph.CodeNodes;
using Gradientspace.NodeGraph.Nodes;
using Gradientspace.GenAI;
using Gradientspace.UI;
using GSNodeEditor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GraphEditorAppV2
{
    internal class NodeWizard
    {
        public string NodeFunctionPrompt = "compute the first N elements of the fibonacci sequence";
        public string GeneratedCode = SourceCodeDataType.MakeDefaultCSharp().CodeText;

        public NodeWizard()
        {
        }

        public async Task RunCodeGeneration()
        {
            string system_prompt = @"
                You are a helpful assistant that generates C# code functions for a node-based graph editor.
                Given a user prompt, provide the appropriate code function that will be used to spawn a C# custom-code node.
                The code should be concise and focused on the requested functionality.
                Return only the code snippet without any additional explanations or commentary.

                If the user prompt does not explicitly contain argument types or return types, try to guess them from context.

                Add an explanatory comment for the function, and comment important blocks of the code, assuming
                the user is a moderately experienced programmer but not an expert.
            
                The generated function should be wrapped in a public static function with an appropriate (but terse) name,
                contained inside a public class named NodeClass.

                The generated static function should not use the async keyword. The function must be synchronous,
                it should wait for any async calls to complete internally.

                The function should only use C# libraries that are included in the standard dotnet SDK (version 8 or above).
                In addition, the geometry3Sharp library in the g3 namespace should be used for any 2D/3D geometry, vector math, mesh processing, and related tasks.

                The function should return void. Any returned values should be returned via 'out' parameters with appropriate names.

                Parameter and Function names should be in Upper Camel-case (also known as Pascal case), unless otherwise specified by the user.

                If necessary, additional local private static functions can be added to the NodeClass and called by 
                the primary static function. 
            ";

            string user_prompt = NodeFunctionPrompt;

            string full_prompt = system_prompt + "\n\nUser Prompt: " + user_prompt;

            await Task.Run(() => {
                ModelID useModel = ModelRegistry.FindModel("haiku");
                ModelQueryParams queryParams = new();
                string result_code = ModelUtil.RunTextQuery_Blocking(
                    useModel, full_prompt, queryParams);
                result_code = strip_csharp_identification(result_code);
                GeneratedCode = result_code;
            });

        }



        public async Task RunCodeEdit()
        {
            string system_prompt = @"
                You are a helpful assistant that makes changes to a C# code function that was generated for a node-based graph editor.
                Given the user prompt and C# function below, make the requested changes to the C# code based on the user prompt.
                The updated code should be concise and focused on the requested functionality.
                Return only the code snippet without any additional explanations or commentary.

                Make the minimal possible set of changes to the existing C# code to satisfy the user prompt.
                Do not change any argument types or names unless the user explicitly requests it.

                The generated Function should be wrapped in a public static Function with an appropriate (but terse) name,
                contained inside a public class named NodeClass.

                The generated static Function should not use the async keyword. The Function must be synchronous,
                it should wait for any async calls to complete internally.

                The Function should only use C# libraries that are included in the standard dotnet SDK (version 8 or above).
                In addition, the geometry3Sharp library in the g3 namespace should be used for any 2D/3D geometry, vector math, mesh processing, and related tasks.

                The Function should return void. Any returned values should be returned via 'out' parameters with appropriate names.

                Parameter and Function names should be in Upper Camel-case (also known as Pascal case), unless otherwise specified by the user.

                If necessary, additional local private static functions can be added to the NodeClass and called by 
                the primary static function. 
            ";

            string user_prompt = NodeFunctionPrompt;
            string code_prompt = GeneratedCode;
            string full_prompt = system_prompt + "\n\nUser Prompt: " + user_prompt + "\n\nCurrent C# Code: " + code_prompt;

            await Task.Run(() => {
                ModelID useModel = ModelRegistry.FindModel("haiku");
                ModelQueryParams queryParams = new();
                string result_code = ModelUtil.RunTextQuery_Blocking(
                    useModel, full_prompt, queryParams);
                result_code = strip_csharp_identification(result_code);
                GeneratedCode = result_code;
            });

        }




        public void EmitNewCodeNode(NodeGraphViewport graphViewport)
        {
            graphViewport.ExecuteGraphEdit((NodeGraphEditor editor) => {
                NodeType codeNodetype = new NodeType(typeof(CodeFunctionNode));

                // try to pick a sane position for new node
                // this could work better if it picked a specific node to position relative to...
                // could also try to pick 'empty' space (ie not colliding w/ any existing nodes)
                AxisAlignedBox2f CurGraphBounds = graphViewport.WidgetScene.BoundsQuery((Widget w) => { return w is NodeWidget; });
                AxisAlignedBox2f ViewportBounds = graphViewport.ViewportBounds;
                // default to viewport center
                Vector2f NodePosition = ViewportBounds.Center; 
                // if graph is on-screen...
                if ( CurGraphBounds.Intersects(ViewportBounds) ) {
                    AxisAlignedBox2f isect = CurGraphBounds.Intersect(ViewportBounds);
                    // intersect graph bounds w/ screen bounds and pick the right-top/middle/bottom pt that is closest to center of window
                    Vector2f RightPos = isect.CenterRight;
                    if ( ViewportBounds.Center.DistanceSquared(RightPos) > ViewportBounds.Center.DistanceSquared(isect.TopRight) )
                        RightPos = isect.TopRight;
                    if ( ViewportBounds.Center.DistanceSquared(RightPos) > ViewportBounds.Center.DistanceSquared(isect.BottomRight) )
                        RightPos = isect.BottomRight;
                    // shift to the right some, to make some space
                    RightPos.x += 50;
                    // make sure we don't go too far right...  (should actually be based on right-edge of node...)
                    float InsetDist = ViewportBounds.Max.x - 250;
                    RightPos.x = Math.Min(RightPos.x, InsetDist);
                    NodePosition = RightPos;
                }

                NodeWidget newWidget = editor.AddNodeOfType(codeNodetype, NodePosition,
                    (INodeInfo info) => {
                        if (info.Node is CodeFunctionNode codeNode) {
                            codeNode.UpdateSourceCode(new SourceCodeDataType(GeneratedCode, SourceCodeDataType.Language.CSharp));
                        }
                    });
            });
        }




        internal static string strip_csharp_identification(string codeblock)
        {
            int idx = codeblock.IndexOf("```");
            if ( idx >= 0 ) {
                int eol = codeblock.IndexOf('\n');
                if ( eol >= 0 )
                    codeblock = codeblock.Substring(eol+1);
            }
            idx = codeblock.LastIndexOf("```");
            if (idx >= 0) {
                codeblock = codeblock.Remove(idx);
            }
            return codeblock;
        }

    }
}
