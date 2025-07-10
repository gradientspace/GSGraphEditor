// Copyright Gradientspace Corp. All Rights Reserved.
using g3;
using SkiaSharp;

namespace Gradientspace.UI
{
    public class SVGButton : Button, IWidgetContentExtension
    {
        public string SVGPath { get; init; }

        protected float RasterScale = 1.5f;
        protected SKBitmap? Bitmap = null;
        protected bool TryLoadPending = false;

        public SVGButton(string svgPath, float rasterScale = 1.5f)
        {
            ContentExtension = this;
            SVGPath = svgPath;
            RasterScale = rasterScale;
            TryLoadPending = true;
        }




        // IWidgetContentExtension for button content
        public void DrawContent(Widget parentWidget, SKStyleCache StyleCache, SKCanvas Canvas, AxisAlignedBox2f Bounds, bool bIsLocalBounds)
        {
            if (Bitmap == null && TryLoadPending)
            {
                Bitmap = MakeIconBitmapFromSVG(SVGPath, (int)((float)Dimensions.x*RasterScale), (int)( (float)Dimensions.y*RasterScale) );
                TryLoadPending = false;
            }

            if (Bitmap != null)
                Canvas.DrawBitmap(Bitmap, Conversion.ToSkia(Bounds));
        }



        // todo support aspect ratio something something
        public static SKBitmap MakeIconBitmapFromSVG(string filename, int BitmapWidth = 0, int BitmapHeight = 0)
        {
            if (!File.Exists(filename))
            {
                SKBitmap placeholder = new SKBitmap(
                    (BitmapWidth == 0) ? (int)1 : BitmapWidth,
                    (BitmapHeight == 0) ? (int)1 : BitmapHeight);
                SKColor pink = new SKColor(255, 105, 180);
                for (int y = 0; y < placeholder.Width; y++)
                    for (int x = 0; x < placeholder.Height; x++)
                        placeholder.SetPixel(x, y, pink);
                return placeholder;
            }

            var svg = new SkiaSharp.Extended.Svg.SKSvg();
            svg.Load(filename);

            int UseWidth = (BitmapWidth == 0) ? (int)svg.CanvasSize.Width : BitmapWidth;
            int UseHeight = (BitmapHeight == 0) ? (int)svg.CanvasSize.Height : BitmapHeight;
            SKBitmap bitmap = new SKBitmap(UseWidth, UseHeight);
            using (SKCanvas canvas = new SKCanvas(bitmap))
            {
                canvas.Scale( (float)UseWidth / svg.CanvasSize.Width, (float)UseHeight / svg.CanvasSize.Height );
                canvas.DrawPicture(svg.Picture);
                canvas.Flush();
                canvas.Save();
            }
            return bitmap;
        }

    }
}
