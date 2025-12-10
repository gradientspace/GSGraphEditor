// Copyright Gradientspace Corp. All Rights Reserved.
using g3;
using Gradientspace.NodeGraph;
using Gradientspace.NodeGraph.Nodes;
using Gradientspace.UI;
using SkiaSharp;


namespace GSNodeEditor.NodeGraphWidgets
{
    public class StringViewNodeWidgetProvider : INodeWidgetProvider
    {
        public NodeWidget? CreateNewWidget(NodeGraphView Graph, INodeInfo nodeInfo)
        {
            return new StringViewNodeWidget(Graph, nodeInfo);
        }
    }

    public class StringViewNodeWidget : NodeWidget
    {
        internal String CurString = "";

        public int StringAreaWidth { get; set; } = 200;

        public StringViewNodeWidget(NodeGraphView graphView, INodeInfo node) : base(graphView, node)
        {
            if (ParentNode is StringViewNode StringNode) {
                StringNode.OnStringUpdate += StringNode_OnStringUpdate;
            }
        }

        public override IWidgetView CreateDefaultView()
        {
            return new StringViewNodeWidgetView(this);
        }

        private void StringNode_OnStringUpdate(string String)
        {
            CurString = String;
        }

    }



    public class StringViewNodeWidgetView : NodeWidgetView
    {
        AxisAlignedBox2f StringArea;
        List<string> CurLines = [];

        const float LeftRightMargin = 10;
        const float TopBottomMargin = 10;
        const float TextMargin = 4;

        public StringViewNodeWidgetView(NodeWidget nodeWidget) : base(nodeWidget)
        {
        }

        public override void UpdateLayout(SKStyleCache StyleCache)
        {
            base.UpdateLayout(StyleCache);
        }

        protected override Vector2f getCustomMinDimensions()
        {
            if (SourceNodeWidget is StringViewNodeWidget StringWidget) 
                return new Vector2f(StringWidget.StringAreaWidth + 2*LeftRightMargin, 1.0);
            return base.getCustomMinDimensions();
        }

        protected override void updateLayout_Customize(SKStyleCache StyleCache, ref AxisAlignedBox2f NodeBounds)
        {
            float StringAreaWidth = NodeBounds.Width - 2*LeftRightMargin;
            float MaxTextWidth = StringAreaWidth - 2*TextMargin;

            SKPaint TextPaint = 
                StyleCache.GetCachedPaint(DefaultWidgetStyles.DefaultTextFieldStyle.StandardStyle, SKStyleCache.EPaintType.Text);

            string text = "";
            if (SourceNodeWidget is StringViewNodeWidget StringWidget)
                text = StringWidget.CurString;

            CurLines = TextLayoutUtils.BreakLines(text, TextPaint, MaxTextWidth);
            int N = Math.Max(1, CurLines.Count);

            SKFontMetrics TextMetrics = TextPaint.FontMetrics;
            float LineHeight = TextMetrics.Bottom - TextMetrics.Top;
            float TextHeight = N * LineHeight - TextMetrics.Leading;
            TextHeight += TextMargin;

            StringArea = new AxisAlignedBox2f(
                NodeBounds.Min.x + LeftRightMargin, NodeBounds.Max.y - TopBottomMargin,
                NodeBounds.Max.x - LeftRightMargin, NodeBounds.Max.y - TopBottomMargin + TextHeight);
            NodeBounds.Contain(StringArea);
            NodeBounds.Max.y += TopBottomMargin;
        }

        protected override void draw_Customize(SKStyleCache StyleCache, SKCanvas Canvas, ILayoutAnchor Anchor)
        {
            SKPaint TextPaint =
                StyleCache.GetCachedPaint(DefaultWidgetStyles.DefaultTextFieldStyle.StandardStyle, SKStyleCache.EPaintType.Text);
            SKPaint BGPaint =
                StyleCache.GetCachedPaint(DefaultWidgetStyles.DefaultTextFieldStyle.StandardStyle, SKStyleCache.EPaintType.Background);

            SKFontMetrics TextMetrics = TextPaint.FontMetrics;
            float LineHeight = TextMetrics.Bottom - TextMetrics.Top;

            Canvas.DrawRect(Conversion.ToSkia(StringArea), BGPaint);
            float textX = StringArea.Min.x + TextMargin, textY = StringArea.Min.y + LineHeight - TextMetrics.Leading;
            for (int i = 0; i < CurLines.Count; i++) {
                Canvas.DrawText(CurLines[i], textX, textY, TextPaint);
                textY += LineHeight;
            }
        }

    }


}
