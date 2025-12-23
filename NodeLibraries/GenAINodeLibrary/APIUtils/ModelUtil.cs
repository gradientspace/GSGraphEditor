using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gradientspace.GenAI
{



    public static class ModelUtil
    {

        public static ModelAuthInfo FindDefaultAuthInfo(ModelID modelID)
        {
            if (modelID.ModelAPIType == null)
                throw new Exception($"ModelUtil.FindDefaultAuthInfo: ModelAPIType is null for ModelID '{modelID.ProviderID}:{modelID.ModelName}'");
            var getAuthInfoMethod = modelID.ModelAPIType.GetMethod("GetModelAuthInfo", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
            if (getAuthInfoMethod == null)
                throw new Exception($"ModelUtil.FindDefaultAuthInfo: GetModelAuthInfo method not found for ModelAPIType '{modelID.ModelAPIType.Name}'");
            var authInfo = (ModelAuthInfo)getAuthInfoMethod.Invoke(null, new object[] { modelID })!;
            return authInfo;
        }



        public static async Task<string> RunTextQuery(ModelID modelID, string prompt, ModelQueryParams queryParams)
        {
            if (modelID.IsValid == false)
                throw new Exception("ModelUtil.RunTextQuery: Invalid ModelID");
            if (modelID.ModelAPIType == null)
                throw new Exception($"ModelUtil.RunTextQuery: ModelAPIType is null for ModelID '{modelID.ProviderID}:{modelID.ModelName}'");

            ModelAuthInfo authInfo = ModelUtil.FindDefaultAuthInfo(modelID);

            var getSimpleTextQueryMethod = modelID.ModelAPIType.GetMethod("GetSimpleTextQueryFunction", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
            if (getSimpleTextQueryMethod == null)
                throw new Exception($"ModelUtil.RunTextQuery: GetSimpleTextQueryFunction method not found for ModelAPIType '{modelID.ModelAPIType.Name}'");
            
            var queryFunction = (Func<string, Task<string>>)getSimpleTextQueryMethod.Invoke(null, [modelID, authInfo, queryParams])!;
            string result = await queryFunction(prompt);          
            return result;
        }

        public static string RunTextQuery_Blocking(ModelID modelID, string prompt, ModelQueryParams queryParams)
        {
            try {
                Task<string> result = Task.Run(async () => await RunTextQuery(modelID, prompt, queryParams));
                return result.Result;
            } catch (Exception ex) {
                return $"[ModelUtil.RunTextQuery_Blocking] Exception: {ex.Message}";
            }
        }



        public static bool ValidateQueryInfo(
            ModelID modelID, string validProvider, IEnumerable<string> validModels,
            ModelAuthInfo authInfo, EModelAuthType validAuthType,
            bool bThrow = true)
        {
            if ( modelID.ProviderID != validProvider ) {
                if (bThrow)  throw new Exception($"ModelUtil.ValidateQueryInfo: Invalid ProviderID '{modelID.ProviderID}', expected '{validProvider}'");
                return false;
            }
            
            if (validModels.Contains(modelID.ModelName) == false) {
                if (bThrow) throw new Exception($"ModelUtil.ValidateQueryInfo: Invalid ModelName '{modelID.ModelName}' for ProviderID '{modelID.ProviderID}'");
                return false;
            }

            if (authInfo.AuthType != validAuthType) {
                if (bThrow) throw new Exception($"ModelUtil.ValidateQueryInfo: Invalid AuthType '{authInfo.AuthType}', expected '{validAuthType}'");
                return false;
            }

            if (validAuthType == EModelAuthType.APIKey) {
                if (authInfo.AuthToken.Length == 0) {
                    if (bThrow) throw new Exception($"ModelUtil.ValidateQueryInfo: Invalid AuthToken (zero length)");
                    return false;
                }
            }

            return true;
        }
    }
}
