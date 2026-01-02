using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gradientspace.GenAI
{
    public interface IModelAPI
    {
        static abstract IEnumerable<ModelID> EnumerateModels();

        static abstract ModelAuthInfo GetModelAuthInfo(ModelID modelID);

        static abstract Func<string, Task<string>>? GetSimpleTextQueryFunction(ModelID modelID, ModelAuthInfo authInfo, ModelQueryParams queryParams);
        static abstract Func<VisionPrompt, Task<string>>? GetVisionQueryFunction(ModelID modelID, ModelAuthInfo authInfo, ModelQueryParams queryParams);
    }


    public struct ModelType
    {
        public bool Text { get; private set; } = false;
        public bool Image { get; private set; } = false;
        public ModelType() { }
        public ModelType(bool text, bool image) { Text = text; Image = image; }

        public static readonly ModelType None = new ModelType(false, false);
        public static readonly ModelType TextModel = new ModelType(true, false);
        public static readonly ModelType ImageModel = new ModelType(false, true);
        public static readonly ModelType MultiModal = new ModelType(true, true);
    }

    public struct ModelID
    {
        public string ProviderID = "";
        public string ModelName = "";
        public ModelType Type = ModelType.None;

        public int InternalModelID = -1;
        public Type? ModelAPIType = null;

        public ModelID() { }

        public readonly bool IsValid { get { return ProviderID.Length > 0 && ModelName.Length > 0; } }
        public readonly string IDString { get { return $"{ProviderID}::{ModelName}"; } }

        public static readonly ModelID Invalid = new ModelID();

        public override string ToString()
        {
            return IDString;
        }
    }



    public enum EModelAuthType
    {
        APIKey = 0
    }
    public struct ModelAuthInfo
    {
        public EModelAuthType AuthType = EModelAuthType.APIKey;
        public string AuthToken = "";

        public ModelAuthInfo() { }

        public bool IsValid { get { return AuthToken.Length > 0; } }

        public static ModelAuthInfo Invalid = new ModelAuthInfo();
    }



    public struct ModelQueryParams
    {
        public int MaxTokens = 4096;

        public ModelQueryParams() { }

        public static readonly ModelQueryParams Default = new();
    }

}
