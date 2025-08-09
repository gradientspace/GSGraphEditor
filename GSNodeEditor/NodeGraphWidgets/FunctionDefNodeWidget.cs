// Copyright Gradientspace Corp. All Rights Reserved.
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

            WidgetStyle = NodeWidgetStyles.FunctionDefNode;

            FunctionArgsWidget = new VariablesPanelWidget("Arguments");
            FunctionArgsWidget.SetNumVariables(2);

            FunctionArgsWidgetAnchor = new WidgetRelativeBoxAnchor(this) {
                BoxPoint = BoxPoints.TopLeft, Offset = new Vector2f(-15, 0)
            };
            FunctionArgsWidget.AnchorTo(FunctionArgsWidgetAnchor);
            FunctionArgsWidget.AnchorPlacement = new AnchorLocation(BoxPoints.TopRight);
            AddChildWidget(FunctionArgsWidget);


            FunctionNameEntry = new TextEntryField() {
                Text = FunctionNode.FunctionName, Width = 132   // variable inputs are 65-wide...
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

            // listen for interactive changes
            FunctionArgsWidget.OnVariablesChanged += FunctionArgsWidget_OnVariablesChanged;
            ReturnArgsWidget.OnVariablesChanged += ReturnAgsWidget_OnVariablesChanged;
        }


        private bool bIgnoreChanges = false;
        public override void InitializeFromNode(INodeInfo nodeInfo)
        {
            base.InitializeFromNode(nodeInfo);

            // these calls will result in the OnVariablesChanged events firing, which
            // would result in an infinite loop of graph edits...
            bIgnoreChanges = true;
            FunctionArgsWidget.SetVariables(FunctionNode.Arguments);
            ReturnArgsWidget.SetVariables(FunctionNode.ReturnArguments);
            bIgnoreChanges = false;
        }


        private void FunctionNameEntry_OnTextModified(TextEntryField sender, string oldText, string newText)
        {
            if (bIgnoreChanges) return;

            NodeGraphView ParentView = this.ParentGraphWidget;
            ParentView.ExecuteGraphEdit((NodeGraphEditor Editor) => {
                string CurrentName = FunctionNode.FunctionName;
                bool bSuccess = Editor.TryRenameFunction(this.GraphNodeIdentifier, CurrentName, newText);
                if (bSuccess == false)
                    FunctionNameEntry.SilentUpdateText(CurrentName);   // reset string to previous value
            });
        }

        private void FunctionArgsWidget_OnVariablesChanged(VariablesPanelWidget sender)
        {
            if (bIgnoreChanges) return;

            NodeGraphView ParentView = this.ParentGraphWidget;
            ParentView.ExecuteGraphEdit((NodeGraphEditor Editor) => {
                Editor.UpdateFunctionArguments(this.GraphNodeIdentifier, FunctionArgsWidget.GetVariables(), null);
            });
        }

        private void ReturnAgsWidget_OnVariablesChanged(VariablesPanelWidget sender)
        {
            if (bIgnoreChanges) return;

            NodeGraphView ParentView = this.ParentGraphWidget;
            ParentView.ExecuteGraphEdit((NodeGraphEditor Editor) => {
                Editor.UpdateFunctionArguments(this.GraphNodeIdentifier, null, ReturnArgsWidget.GetVariables());
            });
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
