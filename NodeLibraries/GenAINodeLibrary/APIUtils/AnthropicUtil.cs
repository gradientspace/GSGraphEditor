// Copyright Gradientspace Corp. All Rights Reserved.
using Anthropic;
using Anthropic.Models.Messages;
using Anthropic.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gradientspace.Nodes.GenAI
{
    public static class AnthropicUtil
    {
        public enum EClaudeModel
        {
            Haiku,
            Sonnet,
            Opus
        }

        public static async Task<string> SimpleClaudeTextQuery(string prompt, string apiKey, EClaudeModel UseModel = EClaudeModel.Opus, int MaxTokens = 4096)
        {
            AnthropicClient client = new() {
                APIKey = apiKey
            };

            MessageParam textPromptMessage = new() {
                Role = Role.User,
                Content = prompt
            };


            Anthropic.Models.Messages.Model InternalUseModel = Model.ClaudeHaiku4_5;
            //Anthropic.Models.Messages.Model InternalUseModel = Model.ClaudeSonnet4_5;
            switch (UseModel) {
                case EClaudeModel.Haiku:
                    InternalUseModel = Model.ClaudeHaiku4_5; break;
                case EClaudeModel.Sonnet:
                    InternalUseModel = Model.ClaudeSonnet4_5; break;
                case EClaudeModel.Opus:
                    InternalUseModel = Model.ClaudeOpus4_5; break;
            };

            MessageCreateParams messageParams = new() {
                MaxTokens = MaxTokens,
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
        public static string SimpleClaudeTextQuery_Blocking(string prompt, string apiKey, EClaudeModel UseModel = EClaudeModel.Opus, int MaxTokens = 4096)
        {
            Task<string> result = Task.Run(async () => await SimpleClaudeTextQuery(prompt, apiKey, UseModel, MaxTokens));
            return result.Result;
        }

    }
}
