using Anthropic;
using Anthropic.Models.Messages;
using Anthropic.Services;
using Gradientspace.NodeGraph;
using Microsoft.Extensions.AI;


namespace Gradientspace.Nodes.GenAI
{
    [NodeFunctionLibrary("Gradientspace.GenAI")]
    public static class GenAINodeLibrary
    {
        public static void Initialize()
        {
        }

        [NodeFunction]
        public static string ClaudeTest(string prompt = "What is the tallest building in Canada?")
        {
            if (SecretsSource.FindSecret(ISecretsSource.ANTHROPIC_API_KEY, out string APIKey) == false)
                return "[ClaudeTest] No Anthropic API Key provided";

            return AnthropicUtil.SimpleClaudeTextQuery_Blocking(prompt, APIKey);
        }
        
    }
}
