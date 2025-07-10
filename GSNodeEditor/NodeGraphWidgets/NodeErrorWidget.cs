// Copyright Gradientspace Corp. All Rights Reserved.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using g3;
using Gradientspace.UI;
using SkiaSharp;

namespace GSNodeEditor
{
    public class NodeErrorWidget : Button, IWidgetContentExtension
    {
        public NodeErrorWidget() : base(ButtonStyle)
        {
            ContentExtension = this;

            Dimensions = new Vector2f(13, 17);
            InheritParentDepth = false;
            RenderDepth = new WidgetDepth(WidgetDepthLayers.Overlay1);

            OnHoverUpdate += NodeErrorWidget_OnHoverUpdate;
        }

        string[] CurrentErrors = new string[0];

        public void SetErrorStrings(List<string>? Errors)
        {
            CurrentErrors = Errors?.ToArray() ?? new string[0];
        }

        bool bHovering = false;
        private void NodeErrorWidget_OnHoverUpdate(Button button, Vector2f HoverPosition, EWidgetHoverState HoverState)
        {
            bHovering = (HoverState == EWidgetHoverState.BeginHover || HoverState == EWidgetHoverState.UpdateHover);
        }

        // IWidgetContentExtension for button content
        public void DrawContent(Widget parentWidget, SKStyleCache StyleCache, SKCanvas Canvas, AxisAlignedBox2f Bounds, bool bIsLocalBounds)
        {
            AxisAlignedBox2f Upper = new AxisAlignedBox2f(Bounds.Center - new Vector2f(0, 2), 2, 4);
            AxisAlignedBox2f Lower = new AxisAlignedBox2f(Bounds.Center + new Vector2f(0, 5), 2, 1.5f);

            SKPaint FillPaint = StyleCache.GetCachedPaint(NodeErrorWidget.IconStyle, SKStyleCache.EPaintType.Background);
            Canvas.DrawRect(Conversion.ToSkia(Upper), FillPaint);
            Canvas.DrawRect(Conversion.ToSkia(Lower), FillPaint);

            if ( bHovering && CurrentErrors.Length > 0)
            {
                SKPaint TextPaint = StyleCache.GetCachedPaint(NodeErrorWidget.TextStyle, SKStyleCache.EPaintType.Text);
                TextHeightInfo heightInfo = StyleCache.GetCachedFontHeightInfo(NodeErrorWidget.TextStyle);

                Vector2f CurPos = Bounds.BottomRight;
                CurPos += new Vector2f(2, -2);
                for (int i = CurrentErrors.Length-1; i >= 0; --i) {
                    string Message = CurrentErrors[i];
                    Canvas.DrawText(Message, Conversion.ToSkia(CurPos), TextPaint);
                    CurPos.y -= (heightInfo.MaxTotalHeight + ButtonStandardStyle.Margins.Top);
                }
            }
        }

        public static readonly WidgetStyle ButtonStandardStyle = new WidgetStyle() { BackgroundColor = Colorf.VideoWhite, ForegroundColor = Colorf.Black };
        public static readonly WidgetStyle ButtonHoverStyle = new WidgetStyle() { BackgroundColor = Colorf.VideoWhite, ForegroundColor = Colorf.Black };
        public static readonly WidgetStyle ButtonPressedStyle = new WidgetStyle() { BackgroundColor = Colorf.VideoWhite, ForegroundColor = Colorf.Black };
        public static readonly WidgetStateStyle ButtonStyle = new WidgetStateStyle(
            ButtonStandardStyle, ButtonHoverStyle, ButtonPressedStyle);

        public static readonly WidgetStyle IconStyle = new WidgetStyle() { BackgroundColor = Colorf.Red, ForegroundColor = Colorf.Black, LineWidth = 0.5f };
        public static readonly WidgetStyle TextStyle = new WidgetStyle() { TextColor = Colorf.VideoWhite, LineWidth = 1.0f, TextSize = 14 };

    }
}
