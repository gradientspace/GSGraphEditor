// Copyright Gradientspace Corp. All Rights Reserved.
using g3;
using Gradientspace.NodeGraph;
using Gradientspace.NodeGraph.CodeNodes;
using Gradientspace.UI;
using SkiaSharp;
using System.Diagnostics;
using static GSNodeEditor.InteractionManager;

namespace GSNodeEditor
{
    public class CodeFunctionNodeWidgetProvider : INodeWidgetProvider
    {
        public NodeWidget? CreateNewWidget(NodeGraphView Graph, INodeInfo nodeInfo)
        {
            return new CodeFunctionNodeWidget(Graph, nodeInfo);
        }
    }



    public class CodeFunctionNodeButton : Button
    {
		public CodeFunctionNodeButton(WidgetStateStyle? customStyle = null) : base(customStyle) { }

        // todo would be nice to do this w/ a custom tooltip that could show code in mono (small)...
		public override bool GetTooltipStrings(out string? tooltip, out string[]? extendedTooltip)
		{
			tooltip = "[C# Code]";
            extendedTooltip = null;
			if (ParentWidget is CodeFunctionNodeWidget widget) {
                string code = widget.GetCurrentSourceCode().CodeText;
                string[] lines = code.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
                extendedTooltip = lines;
            }
			return true;
		}
	}


    public class CodeFunctionNodeWidget : NodeWidget, IWidgetContentExtension, ISourceCodeProvider
    {
        public INodeWithInlineCode CodeNodeAPI { get; init; }

        public CodeFunctionNodeButton CodeButton;
        public RelativeBoxAnchor CodeButtonAnchor;

        SimplePopupMenuDialog? ActivePopupMenuDialog = null;

        public CodeFunctionNodeWidget(NodeGraphView graphView, INodeInfo node) : base(graphView, node)
        {
            Debug.Assert(node.Node is INodeWithInlineCode);
			CodeNodeAPI = (INodeWithInlineCode)node.Node;

			CodeNodeAPI.OnCompileStatusUpdate += CodeNode_OnCompileStatusUpdate;

            CodeButton = new CodeFunctionNodeButton(CodeFunctionNodeWidget.ButtonStyle);
            CodeButton.Dimensions = new Vector2f(20, 22);
            CodeButtonAnchor = new RelativeBoxAnchor(this.GetAnchor());
            CodeButtonAnchor.BoxPoint = BoxPoints.BottomRight;
            CodeButtonAnchor.Offset = new Vector2f(-8, -3);
            CodeButton.AnchorTo(CodeButtonAnchor);
            CodeButton.AnchorPlacement = new AnchorLocation(BoxPoints.CenterTop);
            CodeButton.ContentExtension = this;
            CodeButton.OnClicked += CodeButton_OnClicked;
            AddChildWidget(CodeButton);

            // explicitly do a status update in case the node already has it's code compiled
            CodeNode_OnCompileStatusUpdate(CodeNodeAPI);
        }

        private void CodeNode_OnCompileStatusUpdate(INodeWithInlineCode codeNode)
        {
            if (codeNode.LastCompileOK)
                ClearNodeErrorState();
            else
                SetNodeErrorState(codeNode.LastCompileMessages);
        }

        private void CodeButton_OnClicked(Button button)
        {
            if ( initCodeEditContextMenu() == false )
                SourceCodeEditingSystem.Instance.BeginCodeEdit(this);
        }

        public override void Dispose()
        {
            base.Dispose();
            SourceCodeEditingSystem.Instance.TerminateCodeEdit(this);
        }

        public virtual SourceCodeDataType GetCurrentSourceCode()
        {
            return CodeNodeAPI.GetInlineSourceCode().MakeDuplicate();
        }
        public virtual void UpdateSourceCode(SourceCodeDataType NewSourceCode)
        {
			CodeNodeAPI.SetInlineSourceCode(NewSourceCode);
        }

		public virtual string GetCodeUINameHint()
        {
            return CodeNodeAPI.GetCodeNameHint();
        }


        private bool initCodeEditContextMenu()
        {
            if (ActivePopupMenuDialog != null)
                return false;
            SimpleWidgetSource? source = ParentGraphWidget.WidgetSource as SimpleWidgetSource;
            if (source == null) 
                return false;

            SimplePopupMenuDialog newDialog = new SimplePopupMenuDialog();

            newDialog.AddItem(new MenuItem() {
                Text = "Edit in VSCode",
                OnClicked = () => {
                    SourceCodeEditingSystem.Instance.BeginCodeEdit(this);
                }
            });
            newDialog.AddItem(new MenuItem() {
                Text = "Edit in Node Wizard",
                OnClicked = () => {
                    SourceCodeEditingSystem.LaunchExternalCodeEditSession(this);
                }
            });

            AxisAlignedBox2f buttonBox = CodeButton.GetActiveView()?.BoundsQuery(this.CodeButtonAnchor) ?? AxisAlignedBox2f.Empty;
            newDialog.Position = buttonBox.CenterRight + new Vector2f(5,-5);
            newDialog.OnDismissDialogClick = () => { dismissCodeEditContextMenu(); };
            newDialog.OnItemSelected += (SimplePopupMenuDialog dialog, MenuItem item) => {
                dismissCodeEditContextMenu();
            };

            source.AddRootWidget(newDialog);
            SystemKeyboardRouter.Instance.PushHotkeyTarget(newDialog);

            ActivePopupMenuDialog = newDialog;
            return true;
        }

        protected void dismissCodeEditContextMenu()
        {
            if (ActivePopupMenuDialog == null)
                return;

            if (ParentGraphWidget.WidgetSource is SimpleWidgetSource source)
                source.RemoveRootWidget(ActivePopupMenuDialog);

            if (ActivePopupMenuDialog is IHotkeyTarget target)
                SystemKeyboardRouter.Instance.PopHotkeyTarget(target);

            ActivePopupMenuDialog = null;
        }



        public override IWidgetView CreateDefaultView()
        {
            return new CodeFunctionNodeWidgetView(this);
        }

        // IWidgetContentExtension for button content
        public void DrawContent(Widget parentWidget, SKStyleCache StyleCache, SKCanvas Canvas, AxisAlignedBox2f Bounds, bool bIsLocalBounds)
        {
            Debug.Assert(parentWidget is Button && bIsLocalBounds == false);
            SKPaint LinePaint = StyleCache.GetCachedPaint(CodeFunctionNodeWidget.IconStyle, SKStyleCache.EPaintType.Outline);
            
            Vector2f Offset = new Vector2f(3, 0);
            Segment2d Segment1 = new Segment2d(Bounds.TopLeft+Offset, Bounds.BottomLeft+Offset);
            Segment2d Segment2 = new Segment2d(Bounds.TopRight-Offset, Bounds.BottomRight-Offset);
            Canvas.DrawLine( Conversion.ToSkia(Segment1.PointBetween(0.3f)), Conversion.ToSkia(Segment2.PointBetween(0.3f)), LinePaint);
            Canvas.DrawLine( Conversion.ToSkia(Segment1.PointBetween(0.5f)), Conversion.ToSkia(Segment2.PointBetween(0.5f)), LinePaint);
            Canvas.DrawLine( Conversion.ToSkia(Segment1.PointBetween(0.7f)), Conversion.ToSkia(Segment2.PointBetween(0.7f)), LinePaint);
        }


        public static readonly WidgetStyle ButtonStandardStyle = new WidgetStyle() { BackgroundColor = Colorf.DarkYellow, ForegroundColor = Colorf.Black };
        public static readonly WidgetStyle ButtonHoverStyle = new WidgetStyle() { BackgroundColor = Colorf.VideoYellow, ForegroundColor = Colorf.Black };
        public static readonly WidgetStyle ButtonPressedStyle = new WidgetStyle() { BackgroundColor = Colorf.Orange, ForegroundColor = Colorf.Black };
        public static readonly WidgetStateStyle ButtonStyle = new WidgetStateStyle(
            ButtonStandardStyle, ButtonHoverStyle, ButtonPressedStyle);

        public static readonly WidgetStyle IconStyle = new WidgetStyle() { BackgroundColor = Colorf.LightGrey, ForegroundColor = Colorf.Black, LineWidth = 2.0f };
    }




    public class CodeFunctionNodeWidgetView : NodeWidgetView
    {
        public CodeFunctionNodeWidgetView(NodeWidget nodeWidget) : base(nodeWidget)
        {
        }

        public override void UpdateLayout(SKStyleCache StyleCache)
        {
            CodeFunctionNodeWidget CodeWidget = ((CodeFunctionNodeWidget)SourceNodeWidget);
            CodeWidget.Label = CodeWidget.ParentNode!.GetNodeName();

            base.UpdateLayout(StyleCache);

            CodeWidget.CodeButton.GetActiveView()?.UpdateLayout(StyleCache);
            CodeWidget.CodeButtonAnchor.Box = LocalNodeBounds;
        }
    }

}
