// Copyright Gradientspace Corp. All Rights Reserved.
using Anthropic;
using Anthropic.Models.Messages;
using Anthropic.Services;
using Gradientspace.NodeGraph;

namespace Gradientspace.GenAI
{
    public static class AnthropicUtil
    {
        public static string ProviderID = "anthropic";

        public enum EClaudeModel
        {
            Haiku_4p5 = 0,
            Sonnet_4p5 = 1,
            Opus_4p5 = 2
        }

        public static readonly string[] ModelNames = [
            "claude-haiku-4-5",
            "claude-sonnet-4-5",
            "claude-opus-4-5"
        ];

        public static string ModelToString(EClaudeModel model)
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
                TypeOptions = ModelType.TextModel,
                InternalModelID = idx,
                ModelAPIType = typeof(AnthropicAPIHelper)
            };
        }



        public static async Task<string> SimpleTextQuery(string prompt,
            ModelID useModel, ModelAuthInfo authInfo, ModelQueryParams queryParams)
        {
            try {
                ModelUtil.ValidateQueryInfo(useModel, ProviderID, ModelNames, authInfo, EModelAuthType.APIKey, true);
            } catch (Exception ex) {
                return $"[AnthropicUtil.SimpleTextQuery] invalid query - {ex.Message}";
            }

            AnthropicClient client = new() {
                APIKey = authInfo.AuthToken
            };

            MessageParam textPromptMessage = new() {
                Role = Role.User,
                Content = prompt
            };

            Anthropic.Models.Messages.Model InternalUseModel = Model.ClaudeHaiku4_5;
            if (useModel.ModelName.StartsWith("claude-haiku-4-5",StringComparison.OrdinalIgnoreCase))
                InternalUseModel = Model.ClaudeHaiku4_5;
            else if (useModel.ModelName.StartsWith("claude-sonnet-4-5", StringComparison.OrdinalIgnoreCase))
                InternalUseModel = Model.ClaudeSonnet4_5;
            else if (useModel.ModelName.StartsWith("claude-opus-4-5", StringComparison.OrdinalIgnoreCase))
                InternalUseModel = Model.ClaudeOpus4_5;


            MessageCreateParams messageParams = new() {
                MaxTokens = queryParams.MaxTokens,
                Model = InternalUseModel,
                Messages = [textPromptMessage]
            };

            Message? message = null;
            try {
                message = await client.Messages.Create(messageParams);
                if (message == null)
                    throw new Exception("Anthropic API returned null message...");
            } catch (Exception ex) {
                return $"Anthropic API threw exception: {ex.Message}";
            }

            string resultText = "";
            foreach (ContentBlock contentBlock in message!.Content) {
                if (contentBlock.Value is TextBlock textBlock) {
                    resultText = textBlock.Text;
                }
            }

            return resultText;
        }

    }



    public class AnthropicAPIHelper : IModelAPI
    {
        private AnthropicAPIHelper() { }

        public static IEnumerable<ModelID> EnumerateModels()
        {
            for (int i = 0; i < AnthropicUtil.ModelNames.Length; ++i) {
                yield return new ModelID() {
                    ProviderID = AnthropicUtil.ProviderID,
                    ModelName = AnthropicUtil.ModelNames[i],
                    TypeOptions = ModelType.TextModel,
                    InternalModelID = i,
                    ModelAPIType = typeof(AnthropicAPIHelper)
                };
            }
        }

        public static ModelAuthInfo GetModelAuthInfo(ModelID modelID)
        {
            if (SecretsSource.FindSecret(ISecretsSource.ANTHROPIC_API_KEY, out string APIKey) == false)
                return ModelAuthInfo.Invalid;
            return new ModelAuthInfo() { AuthType = EModelAuthType.APIKey, AuthToken = APIKey };
        }

        public static Func<string, Task<string>>? GetSimpleTextQueryFunction(
            ModelID modelID, ModelAuthInfo authInfo, ModelQueryParams queryParams)
        {
            return (string prompt) => AnthropicUtil.SimpleTextQuery(prompt, modelID, authInfo, queryParams);
        }

        public static Func<VisionPrompt, Task<string>>? GetVisionQueryFunction(
            ModelID modelID, ModelAuthInfo authInfo, ModelQueryParams queryParams)
        {
            return null;
        }

        public static Func<ImageGenPrompt, Task<ImageGenResult>>? GetImageGenQueryFunction(
            ModelID modelID, ModelAuthInfo authInfo, ModelQueryParams queryParams)
        {
            return null;
        }
    }

}
