// Copyright Gradientspace Corp. All Rights Reserved.
using g3;
using Google.GenAI;
using Google.GenAI.Types;
using Gradientspace.NodeGraph;
using Gradientspace.NodeGraph.Image;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gradientspace.GenAI
{
    public static class GeminiUtil
    {
        public static string ProviderID = "gemini";

        public enum EGeminiTextModel
        {
            Gemini_2p5_FlashLite = 0,
            Gemini_2p5_Flash = 1,
            Gemini_2p5_Pro = 2,
            Gemini_3p0_Flash = 3,
            Gemini_3p0_Pro = 4
        }

        public static readonly string[] ModelNames = [
            "gemini-2.5-flash-lite",
            "gemini-2.5-flash",
            "gemini-2.5-pro",
            "gemini-3.0-flash-preview",
            "gemini-3.0-pro-preview" 
        ];


        public static string ModelToString(EGeminiTextModel model)
        {
            return ModelNames[(int)model];
        }


        public static ModelID FindModelID(string modelString)
        {
            modelString = modelString.ToLower().Trim();
            int idx = Array.IndexOf(ModelNames, modelString);
            if (idx == -1)
                return ModelID.Invalid;

            return new ModelID() {
                ProviderID = ProviderID,
                ModelName = modelString,
                Type = ModelType.TextModel,
                InternalModelID = idx,
                ModelAPIType = typeof(GeminiAPIHelper)
            };
        }


        public static async Task<string> SimpleTextQuery(string prompt,
            ModelID useModel, ModelAuthInfo authInfo, ModelQueryParams queryParams)
        {
            try {
                ModelUtil.ValidateQueryInfo(useModel, ProviderID, ModelNames, authInfo, EModelAuthType.APIKey, true);
            } catch (Exception ex) {
                return $"[GeminiUtil.SimpleTextQuery] invalid query - {ex.Message}";
            }

            var client = new Client(apiKey: authInfo.AuthToken);
            string model = useModel.ModelName;

            GenerateContentResponse? response = null;
            try {
                response = await client.Models.GenerateContentAsync(
                    model: model,
                    contents: prompt );
                if (response == null)
                    throw new Exception("Gemini API returned null message...");
            } catch (Exception ex) {
                return $"Gemini API threw exception: {ex.Message}";
            }

            string resultText = response.Candidates?[0].Content?.Parts?[0].Text ?? "(empty response)";
            return resultText;
        }


        public static async Task<string> VisionQuery(VisionPrompt prompt, 
            ModelID useModel, ModelAuthInfo authInfo, ModelQueryParams queryParams)
        {
            try {
                ModelUtil.ValidateQueryInfo(useModel, ProviderID, ModelNames, authInfo, EModelAuthType.APIKey, true);
            } catch (Exception ex) {
                return $"[GeminiUtil.VisionQuery] invalid query - {ex.Message}";
            }

            var client = new Client(apiKey: authInfo.AuthToken);
            string modelString = useModel.ModelName;



            GenerateContentResponse? response = null;
            try {
                List<Content> contentsList = new();

                var textPart = new Part { Text = prompt.TextPrompt };
                contentsList.Add(new Content() { Role = "user", Parts = [ textPart ] } );

                foreach (PixelImage img in prompt.Images ?? []) {
					byte[] imageBytes = ImageUtil.PixelImageToMimeData(img, out string mimeType);
                    var imagePart = new Part {
                        InlineData = new Blob {
                            MimeType = mimeType,
                            Data = imageBytes
                        }
                    };
                    contentsList.Add(new Content() { Role = "user", Parts = [imagePart] });
                }

                response = await client.Models.GenerateContentAsync(
                    model: modelString,
                    contents: contentsList);
                if (response == null)
                    throw new Exception("Gemini API returned null message...");
            } catch (Exception ex) {
                return $"Gemini API threw exception: {ex.Message}";
            }

            string resultText = response.Candidates?[0].Content?.Parts?[0].Text ?? "(empty response)";
            return resultText;
        }

    }



    public class GeminiAPIHelper : IModelAPI
    {
        private GeminiAPIHelper() { }

        public static IEnumerable<ModelID> EnumerateModels()
        {
            for ( int i = 0; i < GeminiUtil.ModelNames.Length; ++i ) {
                yield return new ModelID() {
                    ProviderID = GeminiUtil.ProviderID,
                    ModelName = GeminiUtil.ModelNames[i],
                    Type = ModelType.TextModel,
                    InternalModelID = i,
                    ModelAPIType = typeof(GeminiAPIHelper)
                };
            }
        }

        public static ModelAuthInfo GetModelAuthInfo(ModelID modelID)
        {
            if (SecretsSource.FindSecret(ISecretsSource.GEMINI_API_KEY, out string APIKey) == false)
                return ModelAuthInfo.Invalid;
            return new ModelAuthInfo() { AuthType = EModelAuthType.APIKey, AuthToken = APIKey };
        }

        public static Func<string, Task<string>>? GetSimpleTextQueryFunction(
            ModelID modelID, ModelAuthInfo authInfo, ModelQueryParams queryParams)
        {
            return (string prompt) => GeminiUtil.SimpleTextQuery(prompt, modelID, authInfo, queryParams);
        }

        public static Func<VisionPrompt, Task<string>>? GetVisionQueryFunction(
            ModelID modelID, ModelAuthInfo authInfo, ModelQueryParams queryParams)
        {
            return (VisionPrompt prompt) => GeminiUtil.VisionQuery(prompt, modelID, authInfo, queryParams);
        }
    }
}
