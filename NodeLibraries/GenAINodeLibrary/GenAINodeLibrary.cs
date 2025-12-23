// Copyright Gradientspace Corp. All Rights Reserved.
using Anthropic;
using Anthropic.Models.Messages;
using Anthropic.Services;
using Gradientspace.GenAI;
using Gradientspace.NodeGraph;
using Microsoft.Extensions.AI;


namespace Gradientspace.Nodes.GenAI
{
    [NodeFunctionLibrary("GenerativeAI")]
    public static class GenAINodeLibrary
    {
        public static void Initialize()
        {
        }



        [NodeFunction]
        public static string TextQuery(
            ModelID Model,
            string prompt = "What is the tallest building in Canada?")
        {
            return ModelUtil.RunTextQuery_Blocking(Model, prompt, ModelQueryParams.Default);
        }


    }
}
