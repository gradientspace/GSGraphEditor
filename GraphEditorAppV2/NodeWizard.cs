using Gradientspace.NodeGraph;
using Gradientspace.NodeGraph.CodeNodes;
using Gradientspace.NodeGraph.Nodes;
using Gradientspace.Nodes.GenAI;
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
                if ( NodeEditorSecrets.FindSecret(ISecretsSource.ANTHROPIC_API_KEY, out string APIKey) == false ) {
                    GeneratedCode = "// ERROR: No Anthropic API key found";
                    return;
                }

                string result_code = AnthropicUtil.SimpleClaudeTextQuery_Blocking(
                    full_prompt, APIKey, AnthropicUtil.EClaudeModel.Opus, 8192);
                result_code = strip_csharp_identification(result_code);
                GeneratedCode = result_code;
            });

            // claude returns block enclosed in ```csharp / ```
        }



        public void EmitNewCodeNode(NodeGraphViewport graphViewport)
        {
            graphViewport.ExecuteGraphEdit((NodeGraphEditor editor) => {
                NodeType codeNodetype = new NodeType(typeof(CodeFunctionNode));
                g3.Vector2f Position = new g3.Vector2f(500, 0);
                NodeWidget newWidget = editor.AddNodeOfType(codeNodetype, Position,
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
                    codeblock = codeblock.Substring(eol);
            }
            idx = codeblock.LastIndexOf("```");
            if (idx >= 0) {
                codeblock = codeblock.Remove(idx);
            }
            return codeblock;
        }

    }
}
