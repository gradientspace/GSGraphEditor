// Copyright Gradientspace Corp. All Rights Reserved.
using g3;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Gradientspace.UI
{
    // TODO rewrite this as some kind of copy-on-write hierarchy thing? want to
    // support overriding more easily...


    public struct WidgetMargins
    {
        public float Left = 0;
        public float Right = 0;
        public float Bottom = 0;
        public float Top = 0;
        public WidgetMargins() { }
        public WidgetMargins(float Constant) { Left = Right = Top = Bottom = Constant; }
        public WidgetMargins(float Horizontal, float Vertical) { Left = Right = Horizontal; Top = Bottom = Vertical; }
        public WidgetMargins(float left, float right, float bottom, float top) { Left = left; Right = right; Bottom = bottom; Top = top; }

        public float TotalWidth {  get { return Left + Right; } }
        public float TotalHeight { get { return Bottom + Top; } }
    }


    /**
     * Text renders along a baseline, with some parts going below the baseline and some above.
     * The baseline is at zero
     */
    public struct TextHeightInfo
    {
        //! total maximum height of text
        public float MaxTotalHeight;
        public float AboveBaseline;
        public float BelowBaseline;
    }


    public enum EStyleFontFlags
    {
        None = 0,
        Italic = 1 << 1,
    }


    public class WidgetStyle
    {
        public Colorf ForegroundColor { get; set; } = Colorf.DarkRed;
        public Colorf BackgroundColor { get; set; } = Colorf.CornflowerBlue;

        public float LineWidth { get; set; } = 1.0f;

        public Colorf TextColor { get; set; } = Colorf.Black;
        public float TextSize { get; set; } = 11.0f;
        public string FontName { get; set; } = "Arial";
        public EStyleFontFlags FontFlags { get; set; } = EStyleFontFlags.None;

        public WidgetMargins Margins = new WidgetMargins(2);

        public WidgetStyle()
        {
            // set FontName based on platform?
        }

        public static WidgetStyle DefaultStyle = new WidgetStyle();
        public static WidgetStyle DefaultHoverStyle = new WidgetStyle() { BackgroundColor = Colorf.Orange };
        public static WidgetStyle DefaultPressedStyle = new WidgetStyle() { BackgroundColor = Colorf.LightSteelBlue };
        public static WidgetStyle DefaultDisabledStyle = new WidgetStyle() { BackgroundColor = Colorf.Grey };


        // todo clone and operator=

        //! copy members of otherStyle
        public void Copy(WidgetStyle otherStyle)
        {
            ForegroundColor = otherStyle.ForegroundColor;
            BackgroundColor = otherStyle.BackgroundColor;
            LineWidth = otherStyle.LineWidth;
            TextColor = otherStyle.TextColor;
            TextSize = otherStyle.TextSize;
            FontName = otherStyle.FontName;
        }

        //! returns a new WidgetStyle
        public WidgetStyle Duplicate()
        {
            WidgetStyle w = new WidgetStyle();
            w.Copy(this);
            return w;
        }
    }


    public class WidgetStateStyle
    {
        public WidgetStyle StandardStyle { get; set; }
        public WidgetStyle HoverStyle { get; set; }
        public WidgetStyle PressedStyle { get; set; }
        public WidgetStyle DisabledStyle { get; set; }

        public WidgetStateStyle()
        {
            StandardStyle = WidgetStyle.DefaultStyle;
            HoverStyle = WidgetStyle.DefaultHoverStyle;
            PressedStyle = WidgetStyle.DefaultPressedStyle;
            DisabledStyle = WidgetStyle.DefaultDisabledStyle;
        }

        public WidgetStateStyle(WidgetStyle standard, WidgetStyle hovered, WidgetStyle pressed)
        {
            StandardStyle = standard;
            HoverStyle = hovered;
            PressedStyle = pressed;
            DisabledStyle = standard;
        }

        public WidgetStateStyle(WidgetStyle standard, WidgetStyle hovered, WidgetStyle pressed, WidgetStyle disabled)
        {
            StandardStyle = standard;
            HoverStyle = hovered;
            PressedStyle = pressed;
            DisabledStyle = disabled;
        }

        public WidgetMargins BaseMargins { get { return StandardStyle.Margins; } }


        //! returns a new WidgetStateStyle with new Style members, no connection to source style
        public WidgetStateStyle Duplicate()
        {
            return new WidgetStateStyle() {
                StandardStyle = this.StandardStyle.Duplicate(),
                HoverStyle = this.HoverStyle.Duplicate(),
                PressedStyle = this.PressedStyle.Duplicate(),
                DisabledStyle = this.DisabledStyle.Duplicate()
            };
        }

        public WidgetStyle Select(bool bHovered, bool bPressed, bool bDisabled = false)
        {
            if (bDisabled) return DisabledStyle;
            else if (bPressed) return PressedStyle;
            else if (bHovered) return HoverStyle;
            return StandardStyle;
        }


        public static readonly WidgetStateStyle DefaultStyle = new WidgetStateStyle();
    }


    public sealed class DefaultWidgetStyles
    {
        private DefaultWidgetStyles() { }

        public static WidgetStyle DefaultStandardStyle = new WidgetStyle();
        public static WidgetStyle DefaultHoverStyle = new WidgetStyle() { BackgroundColor = Colorf.Orange };
        public static WidgetStyle DefaultPressedStyle = new WidgetStyle() { BackgroundColor = Colorf.LightSteelBlue };


        public static readonly WidgetStyle TextFieldStandardStyle = new WidgetStyle() { BackgroundColor = Colorf.LightGrey, ForegroundColor = Colorf.LightGrey };
        public static readonly WidgetStyle TextFieldDisabledStyle = new WidgetStyle() { BackgroundColor = Colorf.Grey, ForegroundColor = Colorf.LightGrey };
        public static readonly WidgetStyle TextFieldHoverStyle = new WidgetStyle() { BackgroundColor = Colorf.VideoWhite, ForegroundColor = Colorf.LightGrey };
        public static readonly WidgetStyle TextFieldPressedStyle = new WidgetStyle() { BackgroundColor = Colorf.White, ForegroundColor = Colorf.LightGrey };

        public static readonly WidgetStateStyle DefaultTextFieldStyle = new WidgetStateStyle(
            TextFieldStandardStyle, TextFieldHoverStyle, TextFieldPressedStyle, TextFieldDisabledStyle);
    }





    public class SKStyleCache
    {
        //public Dictionary<WidgetStyle, SKWidgetStyleCache> CachedStyles;

        public SKStyleCache()
        {
            //CachedStyles = new Dictionary<WidgetStyle, SKWidgetStyleCache>();
            CachedBasicPaints = new Dictionary<WidgetStyle, CachedSKPaintSet>();

            FontHeightCache = new Dictionary<Tuple<string, float>, TextHeightInfo>();
        }

        //// do we really need this? couldn't we just go from Style directly?
        //public SKWidgetStyleCache GetCachedStyle(WidgetStyle style)
        //{
        //    SKWidgetStyleCache? found = null;
        //    CachedStyles.TryGetValue(style, out found);
        //    if (found == null)
        //    {
        //        found = new SKWidgetStyleCache(style);
        //        CachedStyles.Add(style, found);
        //    }
        //    return found;
        //}

        public enum EPaintType
        {
            Foreground,
            Background,
            Outline,
            Text
        }

        public struct CachedSKPaintSet
        {
            //public int Timestamp;
            public SKPaint ForegroundPaint;
            public SKPaint BackgroundPaint;
            public SKPaint OutlinePaint;
            public SKPaint TextPaint;

            public SKPaint GetPaint(EPaintType type)
            {
                if (type == EPaintType.Background) return BackgroundPaint;
                else if (type == EPaintType.Text) return TextPaint;
                else if (type == EPaintType.Outline) return OutlinePaint;
                else return ForegroundPaint;
            }
        }

        protected Dictionary<WidgetStyle, CachedSKPaintSet> CachedBasicPaints;


        public CachedSKPaintSet GetCachedPaintSet(WidgetStyle style)
        {
            // temporary for now, to try to avoid some weird race condition possibly
            // related to threaded rendering?
            lock (CachedBasicPaints) {

                CachedSKPaintSet found;
                if (CachedBasicPaints.TryGetValue(style, out found)) {
                    // todo Timestamp on WidgetStyle?
                    //if (found.Timestamp != )

                    return found;
                }
                CachedSKPaintSet NewPaint = new CachedSKPaintSet();
                UpdatePaint(ref NewPaint, style);
                CachedBasicPaints.Add(style, NewPaint);
                return NewPaint;
            }
        }


        public SKPaint GetCachedPaint(WidgetStyle style, EPaintType PaintType)
        {
            CachedSKPaintSet PaintSet = GetCachedPaintSet(style);
            return PaintSet.GetPaint(PaintType);
        }

        public void RefreshStyle(WidgetStyle style)
        {
            CachedBasicPaints.Remove(style);
        }



        protected void UpdatePaint(ref CachedSKPaintSet CachedPaints, WidgetStyle style)
        {
            CachedPaints.ForegroundPaint = new SKPaint()
            {
                IsAntialias = true,
                Color = Conversion.ToSkia(style.ForegroundColor)
            };
            CachedPaints.BackgroundPaint = new SKPaint
            {
                IsAntialias = true,
                Color = Conversion.ToSkia(style.BackgroundColor)
            };
            CachedPaints.OutlinePaint = new SKPaint
            {
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 2.0f,
                Color = Conversion.ToSkia(style.ForegroundColor)
            };
            SKFontStyleSlant slantStyle = style.FontFlags.HasFlag(EStyleFontFlags.Italic) ? SKFontStyleSlant.Italic : SKFontStyleSlant.Upright;
            CachedPaints.TextPaint = new SKPaint
            {
                Color = Conversion.ToSkia(style.TextColor),
                IsAntialias = true,
                LcdRenderText = true,
                SubpixelText = true,
                TextSize = style.TextSize,
                Typeface = SKTypeface.FromFamilyName(
                    familyName: style.FontName,
                    weight: SKFontStyleWeight.Normal, width: SKFontStyleWidth.Normal, slant: slantStyle)
            };
        }



        protected Dictionary<Tuple<string, float>, TextHeightInfo> FontHeightCache;

        public TextHeightInfo GetCachedFontHeightInfo(WidgetStyle style)
        {
            Tuple<string, float> HeightKey = new Tuple<string, float>(style.FontName, style.TextSize);
            TextHeightInfo HeightInfo;
            if (FontHeightCache.TryGetValue(HeightKey, out HeightInfo))
                return HeightInfo;

            SKPaint FontPaint = GetCachedPaint(style, EPaintType.Text);
            HeightInfo = MeasureTextHeightInfo(FontPaint);
            FontHeightCache.Add(HeightKey, HeightInfo);
            return HeightInfo;
        }


        public static TextHeightInfo MeasureTextHeightInfo(SKPaint FontPaint)
        {
            SKRect Bounds = SKRect.Empty;
            FontPaint.MeasureText("qypgqMil", ref Bounds);      // is this good?
            TextHeightInfo HeightInfo = new TextHeightInfo()
            {
                MaxTotalHeight = Bounds.Height,
                AboveBaseline = Math.Abs(Bounds.Top),
                BelowBaseline = Math.Abs(Bounds.Bottom)
            };
            return HeightInfo;
        }

    }


}
