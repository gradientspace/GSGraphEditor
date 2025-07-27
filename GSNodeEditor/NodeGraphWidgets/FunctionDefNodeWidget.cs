using g3;
using Gradientspace.NodeGraph;
using Gradientspace.NodeGraph.CodeNodes;
using Gradientspace.UI;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace GSNodeEditor
{
    public class FunctionDefnNodeWidgetProvider : INodeWidgetProvider
    {
        public NodeWidget? CreateNewWidget(NodeGraphView Graph, INodeInfo nodeInfo)
        {
            return new FunctionDefNodeWidget(Graph, nodeInfo);
        }
    }

    public class FunctionDefNodeWidget : NodeWidget
    {
        public FunctionDefinitionNode FunctionNode;

        public TextEntryField FunctionNameEntry;
        public VariablesPanelWidget FunctionArgsWidget;
        public WidgetRelativeBoxAnchor FunctionArgsWidgetAnchor;

        public VariablesPanelWidget ReturnArgsWidget;
        public WidgetRelativeBoxAnchor ReturnArgsWidgetAnchor;


        public FunctionDefNodeWidget(NodeGraphView graphView, INodeInfo node) : base(graphView, node)
        {
            Debug.Assert(node.Node is FunctionDefinitionNode);
            FunctionNode = (node.Node as FunctionDefinitionNode)!;

            FunctionArgsWidget = new VariablesPanelWidget("Arguments");
            FunctionArgsWidget.SetNumVariables(2);

            FunctionArgsWidgetAnchor = new WidgetRelativeBoxAnchor(this) {
                BoxPoint = BoxPoints.TopLeft, Offset = new Vector2f(-15, 25)
            };
            FunctionArgsWidget.AnchorTo(FunctionArgsWidgetAnchor);
            FunctionArgsWidget.AnchorPlacement = new AnchorLocation(BoxPoints.TopRight);
            AddChildWidget(FunctionArgsWidget);

            FunctionArgsWidget.OnVariablesChanged += FunctionArgsWidget_OnVariablesChanged;



            FunctionNameEntry = new TextEntryField() {
                Text = FunctionNode.FunctionName, Width = 120   // variable inputs are 65-wide...
            };
            FunctionNameEntry.OnTextModified += FunctionNameEntry_OnTextModified;

            FunctionNameEntry.AnchorTo(FunctionArgsWidgetAnchor);
            FunctionNameEntry.AnchorPlacement = new AnchorLocation(BoxPoints.BottomRight) { Offset = new Vector2f(-4, 0) };
            AddChildWidget(FunctionNameEntry);


            ReturnArgsWidget = new VariablesPanelWidget("Returns") {
                VariablePrefix = "Return"
            };

            ReturnArgsWidgetAnchor = new WidgetRelativeBoxAnchor(FunctionArgsWidget) {
                BoxPoint = BoxPoints.BottomLeft, Offset = new Vector2f(0, 25)
            };
            ReturnArgsWidget.AnchorTo(ReturnArgsWidgetAnchor);
            ReturnArgsWidget.AnchorPlacement = new AnchorLocation(BoxPoints.TopLeft);
            AddChildWidget(ReturnArgsWidget);

            ReturnArgsWidget.OnVariablesChanged += ReturnAgsWidget_OnVariablesChanged;



            // todo need to initialize FunctionArgsWidget with current values in node...
            FunctionNode.UpdateArguments(FunctionArgsWidget.GetVariables());
            FunctionNode.UpdateReturnArguments(ReturnArgsWidget.GetVariables());


            //CodeButton = new CodeFunctionNodeButton(CodeFunctionNodeWidget.ButtonStyle);
            //CodeButton.Dimensions = new Vector2f(20, 22);
            //CodeButtonAnchor = new RelativeBoxAnchor(this.GetAnchor());
            //CodeButtonAnchor.BoxPoint = BoxPoints.BottomRight;
            //CodeButtonAnchor.Offset = new Vector2f(-8, -3);
            //CodeButton.AnchorTo(CodeButtonAnchor);
            //CodeButton.AnchorPlacement = new AnchorLocation(BoxPoints.CenterTop);
            //CodeButton.ContentExtension = this;
            //CodeButton.OnClicked += CodeButton_OnClicked;
            //AddChildWidget(CodeButton);
        }

        private void FunctionNameEntry_OnTextModified(TextEntryField sender, string oldText, string newText)
        {
            FunctionNode.UpdateFunctionName(newText);
        }

        private void FunctionArgsWidget_OnVariablesChanged(VariablesPanelWidget sender)
        {
            FunctionNode.UpdateArguments(FunctionArgsWidget.GetVariables());
        }

        private void ReturnAgsWidget_OnVariablesChanged(VariablesPanelWidget sender)
        {
            FunctionNode.UpdateReturnArguments(ReturnArgsWidget.GetVariables());
        }

        public override IWidgetView CreateDefaultView()
        {
            return new FunctionDefNodeWidgetView(this);
        }

        public override bool HasConfigurableSequencePins { get { return true; } }
        public override void UpdateSequencePins() { 
            SetStandardSequencePinsEnabled(false, true);
        }
    }


    public class FunctionDefNodeWidgetView : NodeWidgetView
    {
        FunctionDefNodeWidget FunctionWidget;

        public FunctionDefNodeWidgetView(NodeWidget nodeWidget) : base(nodeWidget)
        {
            FunctionWidget = (FunctionDefNodeWidget)nodeWidget!;
        }

        public override void UpdateLayout(SKStyleCache StyleCache)
        {
            FunctionWidget.Label = FunctionWidget.FunctionNode.FunctionName;

            base.UpdateLayout(StyleCache);

            FunctionWidget.FunctionArgsWidgetAnchor.UpdateFromParentWidget();
            FunctionWidget.FunctionNameEntry.GetActiveView()?.UpdateLayout(StyleCache);
            FunctionWidget.FunctionArgsWidget.GetActiveView()?.UpdateLayout(StyleCache);

            FunctionWidget.ReturnArgsWidgetAnchor.UpdateFromParentWidget();
            FunctionWidget.ReturnArgsWidget.GetActiveView()?.UpdateLayout(StyleCache);
        }

    }



}
