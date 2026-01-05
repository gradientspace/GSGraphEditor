using g3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gradientspace.GenAI
{
    public class VisionPrompt
    {
        public string TextPrompt = "";
        public PixelImage[]? Images = null;
    }


    public class ImageGenPrompt
    {
        public string TextPrompt = "";
        public PixelImage[]? Images = null;
    }

    public class ImageGenResult
    {
        public string status = "";
        public PixelImage[]? Images = null;
    }

    public static class PromptUtils
    {
        public static PixelImage[]? MakeImagesList(PixelImage? firstImage, IEnumerable<PixelImage>? additionalImages)
        {
            if ( firstImage == null ) {
                if (additionalImages == null) 
                    return null;
                return additionalImages.Where( (PixelImage img) => { return img != null; }).ToArray();
            } else {
                if (additionalImages == null)
                    return [firstImage];
                return [firstImage, .. additionalImages.Where( (PixelImage img) => { return img != null; }) ];
            }
        }
    }
}
