using g3;
using Gradientspace.GenAI;
using Gradientspace.NodeGraph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gradientspace.Nodes.GenAI
{
    [NodeFunctionLibrary("GenerativeAI")]
    public static class GenAIImageGenNodeLibrary
    {
        [NodeFunction]
        [NodeParameter("ImagePrompt", IsOptional=true)]
        public static PixelImage? ImageGenQuery(
            ModelID Model,
            string TextPrompt = "A realistic fire hydrant on a white background",
            PixelImage? ImagePrompt = null,
            IEnumerable<PixelImage>? ExtraImages = null)
        {
            ImageGenPrompt prompt = new ImageGenPrompt() {
                TextPrompt = TextPrompt,
                Images = PromptUtils.MakeImagesList(ImagePrompt, ExtraImages)
            };
            ImageGenResult result = ModelUtil.RunImageGenQuery_Blocking(Model, prompt, ModelQueryParams.Default);

            if (result.status.Length > 0)
                GlobalGraphOutput.AppendError(result.status);

            return (result.Images != null && result.Images.Length > 0) ? result.Images[0] : null;
        }
    }
}
