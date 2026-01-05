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




    public abstract class SelectModelNodeBase : NodeBase
    {
        public override string? GetNodeNamespace() { return "GenerativeAI"; }

        public const string ModelOutputName = "Model";
        public const string ModelsInputName = "Models";

        protected EnumOptionSet? ModelsEnumList = null;
        protected EnumOptionSetNodeInput? ModelsEnumListInput = null;

        protected abstract EnumOptionSet get_model_list();

        public SelectModelNodeBase()
        {
            string initialSelection = "";

            ModelsEnumList = get_model_list();
            ModelsEnumListInput = new EnumOptionSetNodeInput(ModelsEnumList, initialSelection);
            ModelsEnumListInput.Flags |= ENodeInputFlags.IsNodeConstant;
            AddInput(ModelsInputName, ModelsEnumListInput);
            //ModelsEnumListInput.OnSelectedValueChanged += OnFieldListSelectionUpdated;

            AddOutput(ModelOutputName, new StandardNodeOutput<ModelID>());

            Flags |= ENodeFlags.IsPure;     // set of models should not change during graph evaluation...
        }

        public override void Evaluate(EvaluationContext EvalContext, ref readonly NamedDataMap DataIn, NamedDataMap RequestedDataOut)
        {
            string CurrentSelectedField = ((EnumOptionItem)ModelsEnumListInput?.GetConstantValue().Item1!).ItemString ?? "";
            if (ModelsEnumList!.FindIndexFromLabel(CurrentSelectedField, out int NewSelectedIndex)) {
                ModelID selectedModel = (ModelID)ModelsEnumList.Items[NewSelectedIndex].TransientData!;
                RequestedDataOut.SetItemValueChecked(ModelOutputName, selectedModel);
            }
        }
    }



    public class SelectModelNode : SelectModelNodeBase
    {
        public override string GetDefaultNodeName() { return "Select Text Model"; }
        protected override EnumOptionSet get_model_list()
        {
            return ModelRegistry.GetAllTextGenModels();
        }
    }

    public class SelectImageGenModelNode : SelectModelNodeBase
    {
        public override string GetDefaultNodeName() { return "Select ImageGen Model"; }
        protected override EnumOptionSet get_model_list()
        {
            return ModelRegistry.GetAllImageGenModels();
        }
    }

}
