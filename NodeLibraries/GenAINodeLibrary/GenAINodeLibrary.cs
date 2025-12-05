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
            AnthropicClient client = new() { 
                APIKey = "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" 
            };

            MessageParam textPromptMessage = new() { 
                Role = Role.User,
                Content = prompt
            };

            MessageCreateParams messageParams = new() { 
                MaxTokens = 1024,
                Model = Model.Claude3_5Haiku20241022,
                Messages = [textPromptMessage]
            };

            Task<Message> task = Task.Run(async () => await client.Messages.Create(messageParams));
            Message message = task.Result;

            string resultText = "";
            foreach (ContentBlock contentBlock in message.Content) 
            {
                if ( contentBlock.Value is TextBlock textBlock ) {
                    resultText = textBlock.Text;
                }
            }


            return resultText;
        }
        
    }
}
