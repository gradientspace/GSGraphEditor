// Copyright Gradientspace Corp. All Rights Reserved.
using g3;
using SkiaSharp;

namespace Gradientspace.UI
{
    public class ImageButton : Button, IWidgetContentExtension
    {
        public string ImagePath { get; init; }

        public bool SmoothResize { get; set; } = false;

        protected float RasterScale = 1.0f;
        protected SKBitmap? Bitmap = null;
        protected bool TryLoadPending = false;

        public ImageButton(string imagePath, float rasterScale = 1.0f)
        {
            ContentExtension = this;
            ImagePath = imagePath;
            RasterScale = rasterScale;
            TryLoadPending = true;
        }


        // IWidgetContentExtension for button content
        public void DrawContent(Widget parentWidget, SKStyleCache StyleCache, SKCanvas Canvas, AxisAlignedBox2f Bounds, bool bIsLocalBounds)
        {
            if (Bitmap == null && TryLoadPending)
            {
                //Bitmap = MakeIconBitmapFromFile(ImagePath, (int)((float)Dimensions.x*RasterScale), (int)( (float)Dimensions.y*RasterScale) );
                Bitmap = MakeIconBitmapFromFile(ImagePath, 0, 0);
                TryLoadPending = false;
            }

            if (Bitmap != null)
            {
                SKPaint paint = new SKPaint();
                if (SmoothResize)
                    paint.FilterQuality = SKFilterQuality.Medium;
                Canvas.DrawBitmap(Bitmap, Conversion.ToSkia(Bounds), paint);
            }
        }



        // is there any reason to support scaling here?
        public static SKBitmap MakeIconBitmapFromFile(string filename, int BitmapWidth = 0, int BitmapHeight = 0)
        {
            if (File.Exists(filename)) {
                using (SKImage image = SKImage.FromEncodedData(filename))
                {
                    int UseWidth = (BitmapWidth == 0) ? (int)image.Width : BitmapWidth;
                    int UseHeight = (BitmapHeight == 0) ? (int)image.Height : BitmapHeight;
                    SKBitmap bitmap = new SKBitmap(UseWidth, UseHeight);
                    using (SKCanvas canvas = new SKCanvas(bitmap))
                    {
                        SKRect sourceRect = new SKRect(0, 0, bitmap.Width, bitmap.Height);
                        SKRect destRect = new SKRect(0, 0, UseWidth, UseHeight); ;
                        canvas.DrawImage(image, sourceRect, destRect);
                        canvas.Flush();
                        canvas.Save();
                    }
                    return bitmap;
                }
            }

            SKBitmap placeholder = new SKBitmap(
                  (BitmapWidth == 0) ? (int)1 : BitmapWidth,
                  (BitmapHeight == 0) ? (int)1 : BitmapHeight);
            SKColor pink = new SKColor(255, 105, 180);
            for (int y = 0; y < placeholder.Width; y++)
                for (int x = 0; x < placeholder.Height; x++)
                    placeholder.SetPixel(x, y, pink);
            return placeholder;
        }

    }
}
