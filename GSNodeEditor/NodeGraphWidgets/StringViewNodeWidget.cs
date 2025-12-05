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

            CurLines = BreakLines(text, TextPaint, MaxTextWidth);
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


        // found this code at https://github.com/mono/SkiaSharp/issues/692

        static List<string> BreakLines(string text, SKPaint paint, float width)
        {
            List<string> lines = new List<string>();

            string remainingText = text.Trim();

            do {
                int idx = LineBreak(remainingText, paint, width);
                if (idx == 0) {
                    break;
                }
                string lastLine = remainingText.Substring(0, idx).Trim();
                lines.Add(lastLine);
                remainingText = remainingText.Substring(idx).Trim();
            } while (!string.IsNullOrEmpty(remainingText));
            return lines;
        }

        static int LineBreak(string text, SKPaint paint, float width)
        {
            int idx = 0, last = 0;
            int lengthBreak = (int)paint.BreakText(text, width);

            while (idx < text.Length) {
                int next = text.IndexOfAny(new char[] { ' ', '\n' }, idx);
                if (next == -1) {
                    if (idx == 0) {
                        return lengthBreak; 
                    } else {
                        // Ellipsize if it's the last line
                        if (lengthBreak == text.Length
                        // || text.IndexOfAny (new char [] { ' ', '\n' }, lengthBreak + 1) == -1
                        ) {
                            return lengthBreak;
                        }
                        // Split at the last word;
                        return last;
                    }
                }
                if (text[idx] == '\n') {
                    return idx;
                }
                if (next > lengthBreak) {
                    return idx;
                }
                last = next;
                idx = next + 1;
            }
            return last;
        }





    }


}
