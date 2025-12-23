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
    public static class GenAIProviderNodes
    {


    }



    public class SelectModelNode : NodeBase
    {
        public override string GetDefaultNodeName() { return "Select Model"; }

        public const string ModelOutputName = "Model";
        public const string ModelsInputName = "Models";

        protected EnumOptionSet? ModelsEnumList = null;
        protected EnumOptionSetNodeInput? ModelsEnumListInput = null;

        public SelectModelNode()
        {
            string initialSelection = "";

            ModelsEnumList = ModelRegistry.GetAllModels();
            ModelsEnumListInput = new EnumOptionSetNodeInput(ModelsEnumList, initialSelection);
            ModelsEnumListInput.Flags |= ENodeInputFlags.IsNodeConstant;
            AddInput(ModelsInputName, ModelsEnumListInput);
            //ModelsEnumListInput.OnSelectedValueChanged += OnFieldListSelectionUpdated;

            AddOutput(ModelOutputName, new StandardNodeOutput<ModelID>());
        }

        public override void Evaluate(EvaluationContext EvalContext, ref readonly NamedDataMap DataIn, NamedDataMap RequestedDataOut)
        {
            string CurrentSelectedField = ((EnumOptionItem)ModelsEnumListInput?.GetConstantValue().Item1!).ItemString ?? "";
            if (ModelsEnumList!.FindIndexFromLabel(CurrentSelectedField, out int NewSelectedIndex)) 
            {
                ModelID selectedModel = (ModelID)ModelsEnumList.Items[NewSelectedIndex].TransientData!;
                RequestedDataOut.SetItemValueChecked(ModelOutputName, selectedModel);
            }
        }
    }

}
