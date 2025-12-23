// Copyright Gradientspace Corp. All Rights Reserved.
using Anthropic;
using Anthropic.Models.Messages;
using Anthropic.Services;
using g3;
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


        [NodeFunction]
        public static string VisionQuery(
            ModelID Model,
            PixelImage Image,
            string TextPrompt = "Describe this image",
            IEnumerable<PixelImage>? Images = null)
        {
            VisionPrompt prompt = new VisionPrompt() {
                TextPrompt = TextPrompt
            };
            if (Images == null)
                prompt.Images = [Image];
            else if (Image == null && Images != null)
                prompt.Images = Images.ToArray();
            else if (Image != null && Images != null)
                prompt.Images = [Image, .. Images];
            return ModelUtil.RunVisionQuery_Blocking(Model, prompt, ModelQueryParams.Default);
        }

    }
}
