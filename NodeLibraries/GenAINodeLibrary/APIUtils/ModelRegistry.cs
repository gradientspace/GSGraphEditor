using Gradientspace.NodeGraph;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Gradientspace.GenAI
{
    /// <summary>
    /// ModelRegistry stores the set of 'known' models. These are 
    /// found by searching for classes that implement IModelAPI, and
    /// enumerating their models
    /// </summary>
    public static class ModelRegistry
    {


        public static EnumOptionSet GetAllModels()
        {
            ModelRegistry.Initialize();

            EnumOptionSet options = new EnumOptionSet(
                AllModels!.Count,
                (int i) => {
                    ModelID model = AllModels[i].modelID;
                    string label = $"{model.ModelName}";
                    return new(label, i, model);
                }
            );
            return options;
        }


        /// <summary>
        /// Search for a Model with name that contains modelSearchString (prefers exact match or starts-with)
        /// </summary>
        public static ModelID FindModel(string modelSearchString)
        {
            ModelRegistry.Initialize();

            int foundIndex = AllModels?.FindIndex(
                m => string.Equals(m.modelID.ModelName, modelSearchString, StringComparison.OrdinalIgnoreCase)) ?? -1;
            if (foundIndex < 0) {
                foundIndex = AllModels?.FindIndex(
                    m => m.modelID.ModelName.StartsWith(modelSearchString, StringComparison.OrdinalIgnoreCase)) ?? -1;
            }
            if (foundIndex < 0) {
                foundIndex = AllModels?.FindIndex(
                    m => m.modelID.ModelName.Contains(modelSearchString, StringComparison.OrdinalIgnoreCase)) ?? -1;
            }
            return (foundIndex >= 0) ? AllModels![foundIndex].modelID : ModelID.Invalid;
        }



        ///// internals

        private struct ModelInfo
        {
            public ModelID modelID;
            public Type modelAPIType;
        }

        private static List<ModelInfo>? AllModels = null;

        private static void Initialize()
        {
            if (AllModels != null)
                return;

            // is this the best way to do it?
            var modelAPIs = AppDomain.CurrentDomain.GetAssemblies().SelectMany(assembly => 
                {
                    try
                    {
                        return assembly.GetTypes();
                    }
                    catch (System.Reflection.ReflectionTypeLoadException)
                    {
                        return Array.Empty<Type>();
                    }
                })
                .Where(type => type.IsClass && !type.IsAbstract && typeof(IModelAPI).IsAssignableFrom(type));

            AllModels = new();
            foreach (Type apiType in modelAPIs)
            {
                MethodInfo? enumerateMethod = apiType.GetMethod("EnumerateModels", BindingFlags.Public | BindingFlags.Static);
                IEnumerable<ModelID> models = enumerateMethod?.Invoke(null, null) as IEnumerable<ModelID> ?? Enumerable.Empty<ModelID>();
                foreach (var model in models)
                    AllModels.Add(new() { modelID = model, modelAPIType = apiType });
            }
        }




    }
}
