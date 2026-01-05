using g3;
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

        // TODO: replace this with a single function that returns another interface instance for doing these
        // queries, which would allow for default implementations (cannot have static abstract / w default)

        static abstract Func<string, Task<string>>? GetSimpleTextQueryFunction(ModelID modelID, ModelAuthInfo authInfo, ModelQueryParams queryParams);
        static abstract Func<VisionPrompt, Task<string>>? GetVisionQueryFunction(ModelID modelID, ModelAuthInfo authInfo, ModelQueryParams queryParams);
        static abstract Func<ImageGenPrompt, Task<ImageGenResult>>? GetImageGenQueryFunction(ModelID modelID, ModelAuthInfo authInfo, ModelQueryParams queryParams);
    }


    public struct ModelType
    {
        // inputs
        public bool TextInput { get; private set; } = false;
        public bool ImageInput { get; private set; } = false;

        // outputs
        public bool TextOutput { get; private set; } = false;
        public bool ImageOutput { get; private set; } = false;

        public ModelType() { }

        public static readonly ModelType None = new ModelType();
        public static readonly ModelType TextModel = new ModelType() { TextInput = true, TextOutput = true };
        public static readonly ModelType VisionModel = new ModelType() { TextInput = true, ImageInput = true, TextOutput = true};
        public static readonly ModelType ImageGenModel = new ModelType() { TextInput = true, ImageInput = true, ImageOutput = true};
    }

    public struct ModelID
    {
        public string ProviderID = "";
        public string ModelName = "";
        public ModelType TypeOptions = ModelType.None;

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
