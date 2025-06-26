using System;
using System.Collections.Generic;
using SkiaSharp;

namespace GSNodeEditor
{
    public class ViewBuffer
    {
        public int Width;
        public int Height;

        public SKImageInfo BitmapInfo;
        public SKBitmap? BackingBitmap;
        public SKCanvas? BackingCanvas;


        public ViewBuffer(int Width, int Height)
        {
            UpdateSize(Width, Height);
        }


        public void UpdateSize(int Width, int Height)
        {
            if (this.Width == Width && this.Height == Height)
                return;

            this.Width = Width;
            this.Height = Height;
            BitmapInfo = new(Width, Height);
            BackingBitmap = new(BitmapInfo, SKBitmapAllocFlags.None);
            BackingCanvas = new(BackingBitmap);
        }

        public bool IsValid
        {
            get { return BackingCanvas != null; }
        }

        public SKCanvas? Canvas
        {
            get { return BackingCanvas; }
        }

        public nint GetRawPixelBufferPointer()
        {
            return (BackingBitmap != null) ? BackingBitmap.GetPixels() : 0;
        }

    }
}
