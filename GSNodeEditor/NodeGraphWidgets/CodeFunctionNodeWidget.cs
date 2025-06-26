using g3;
using Gradientspace.NodeGraph;
using Gradientspace.NodeGraph.CodeNodes;
using Gradientspace.UI;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices.Marshalling;
using System.Text;
using System.Threading.Tasks;

namespace GSNodeEditor
{
    public class CodeFunctionNodeWidgetProvider : INodeWidgetProvider
    {
        public NodeWidget? CreateNewWidget(NodeGraphView Graph, INodeInfo nodeInfo)
        {
            return new CodeFunctionNodeWidget(Graph, nodeInfo);
        }
    }


    public class CodeFunctionNodeWidget : NodeWidget, IWidgetContentExtension, ISourceCodeProvider
    {
        public INodeWithInlineCode CodeNodeAPI { get; init; }

        public Button CodeButton;
        public RelativeBoxAnchor CodeButtonAnchor;

        public CodeFunctionNodeWidget(NodeGraphView graphView, INodeInfo node) : base(graphView, node)
        {
            Debug.Assert(node.Node is CodeFunctionNode);
			CodeNodeAPI = (INodeWithInlineCode)node.Node;

			CodeNodeAPI.OnCompileStatusUpdate += CodeNode_OnCompileStatusUpdate;

            CodeButton = new Button(CodeFunctionNodeWidget.ButtonStyle);
            CodeButton.Dimensions = new Vector2f(20, 22);
            CodeButtonAnchor = new RelativeBoxAnchor(this.GetAnchor());
            CodeButtonAnchor.BoxPoint = BoxPoints.BottomRight;
            CodeButtonAnchor.Offset = new Vector2f(-8, -3);
            CodeButton.AnchorTo(CodeButtonAnchor);
            CodeButton.AnchorPlacement = new AnchorLocation(BoxPoints.CenterTop);
            CodeButton.ContentExtension = this;
            CodeButton.OnClicked += CodeButton_OnClicked;
            AddChildWidget(CodeButton);
        }

        private void CodeNode_OnCompileStatusUpdate(bool bCompileOK, List<string>? Errors)
        {
            if (bCompileOK)
                ClearNodeErrorState();
            else
                SetNodeErrorState(Errors);
        }

        private void CodeButton_OnClicked(Button button)
        {
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
            Segment2f Segment1 = new Segment2f(Bounds.TopLeft+Offset, Bounds.BottomLeft+Offset);
            Segment2f Segment2 = new Segment2f(Bounds.TopRight-Offset, Bounds.BottomRight-Offset);
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
