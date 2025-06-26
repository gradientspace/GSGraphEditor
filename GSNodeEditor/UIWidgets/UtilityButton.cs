using g3;
using Gradientspace.UI;
using SkiaSharp;

namespace GSNodeEditor
{
    public class UtilityButton : Button, IWidgetContentExtension
    {
        public enum ButtonTypes
        {
            Plus,
            Minus
        }
        ButtonTypes ButtonType = ButtonTypes.Plus;

        public UtilityButton(ButtonTypes buttonType = ButtonTypes.Plus) : base(ButtonStyle)
        {
            ContentExtension = this;
            Dimensions = new Vector2f(13, 13);
            ButtonType = buttonType;
        }

        // IWidgetContentExtension for button content
        public void DrawContent(Widget parentWidget, SKStyleCache StyleCache, SKCanvas Canvas, AxisAlignedBox2f Bounds, bool bIsLocalBounds)
        {
            switch (ButtonType)
            {
                case ButtonTypes.Plus: DrawPlus(StyleCache, Canvas, Bounds); break;
                case ButtonTypes.Minus: DrawMinus(StyleCache, Canvas, Bounds); break;
            }
        }

        protected void DrawPlus(SKStyleCache StyleCache, SKCanvas Canvas, AxisAlignedBox2f Bounds)
        {
            AxisAlignedBox2f Box1 = new AxisAlignedBox2f(Bounds.Center, 1, 5);
            AxisAlignedBox2f Box2 = new AxisAlignedBox2f(Bounds.Center, 5, 1);
            SKPaint FillPaint = StyleCache.GetCachedPaint(UtilityButton.IconStyle, SKStyleCache.EPaintType.Foreground);
            Canvas.DrawRect(Conversion.ToSkia(Box1), FillPaint);
            Canvas.DrawRect(Conversion.ToSkia(Box2), FillPaint);
        }

        protected void DrawMinus(SKStyleCache StyleCache, SKCanvas Canvas, AxisAlignedBox2f Bounds)
        {
            AxisAlignedBox2f Box2 = new AxisAlignedBox2f(Bounds.Center, 5, 1);
            SKPaint FillPaint = StyleCache.GetCachedPaint(UtilityButton.IconStyle, SKStyleCache.EPaintType.Foreground);
            Canvas.DrawRect(Conversion.ToSkia(Box2), FillPaint);
        }

        public static readonly WidgetStyle ButtonStandardStyle = new WidgetStyle() { BackgroundColor = Colorf.VideoWhite, ForegroundColor = Colorf.Black };
        public static readonly WidgetStyle ButtonHoverStyle = new WidgetStyle() { BackgroundColor = Colorf.VideoWhite, ForegroundColor = Colorf.Black };
        public static readonly WidgetStyle ButtonPressedStyle = new WidgetStyle() { BackgroundColor = Colorf.VideoWhite, ForegroundColor = Colorf.Black };
        public static readonly WidgetStateStyle ButtonStyle = new WidgetStateStyle(
            ButtonStandardStyle, ButtonHoverStyle, ButtonPressedStyle);

        public static readonly WidgetStyle IconStyle = new WidgetStyle() { BackgroundColor = Colorf.Red, ForegroundColor = Colorf.Black, LineWidth = 0.5f };
    }
}