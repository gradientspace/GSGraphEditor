using SkiaSharp;
using g3;

namespace Gradientspace.UI
{
    public static class Conversion
    {
        public static SKPoint ToSkia(Vector2f v)
        {
            return new SKPoint((float)v.x, (float)v.y);
        }
        public static SKPoint ToSkia(Vector2d v)
        {
            return new SKPoint((float)v.x, (float)v.y);
        }

        public static Vector2f FromSkia(SKPoint p)
        {
            return new Vector2f(p.X, p.Y);
        }

        public static SKRect ToSkia(AxisAlignedBox2f box)
        {
            return new SKRect(box.Min.x, box.Min.y, box.Max.x, box.Max.y);
        }
        public static SKRect ToSkia(AxisAlignedBox2f box, Vector2f boxOrigin)
        {
            return new SKRect(boxOrigin.x+box.Min.x, boxOrigin.y+box.Min.y, boxOrigin.x+box.Max.x, boxOrigin.y+box.Max.y);
        }
        public static AxisAlignedBox2f FromSkia(SKRect box)
        {
            return new AxisAlignedBox2f(box.Left, box.Bottom, box.Right, box.Top);
        }

        public static SKRect ToSkia(AxisAlignedBox2d box)
        {
            return new SKRect((float)box.Min.x, (float)box.Min.y, (float)box.Max.x, (float)box.Max.y);
        }

        public static SKColor ToSkia(Colorf c)
        {
            Colorb b = c.ToBytes();
            return new SKColor(b.r, b.g, b.b, b.a);
        }
		public static SKColor ToSkia(Colorb c)
		{
			return new SKColor(c.r, c.g, c.b, c.a);
		}

	}


    public static class SkiaUtil
    {
        public static float GetAbsoluteTextHeight(SKPaint Paint)
        {
            // todo think more about this... useful info: https://stackoverflow.com/questions/27631736/meaning-of-top-ascent-baseline-descent-bottom-and-leading-in-androids-font
            //float TextHeight = MathF.Abs(Paint.FontMetrics.Top) + MathF.Abs(Paint.FontMetrics.Bottom);
            float TextHeight = MathF.Abs(Paint.FontMetrics.Ascent) + MathF.Abs(Paint.FontMetrics.Descent);
            return TextHeight;
        }

        //! compute point on a SKPath.CubicTo curve (t in range [0,1])
		public static Vector2f SkiaCubicPoint(
			Vector2f a, Vector2f ta, Vector2f tb, Vector2f b, float t)
		{
			float mt = 1 - t;
			float mt2 = mt * mt, mt3 = mt * mt * mt;
			float t2 = t * t, t3 = t * t * t;
			float x = mt3 * a.x + 3 * mt2 * t * ta.x + 3 * mt * t2 * tb.x + t3 * b.x;
			float y = mt3 * a.y + 3 * mt2 * t * ta.y + 3 * mt * t2 * tb.y + t3 * b.y;
			return new Vector2f(x, y);
		}
	}

}
