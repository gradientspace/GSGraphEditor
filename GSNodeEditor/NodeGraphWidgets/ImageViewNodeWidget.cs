// Copyright Gradientspace Corp. All Rights Reserved.
using g3;
using Gradientspace.NodeGraph;
using Gradientspace.NodeGraph.Image;
using Gradientspace.UI;
using SkiaSharp;

namespace GSNodeEditor.NodeGraphWidgets
{
    public class ImageViewNodeWidgetProvider : INodeWidgetProvider
    {
        public NodeWidget? CreateNewWidget(NodeGraphView Graph, INodeInfo nodeInfo)
        {
            return new ImageViewNodeWidget(Graph, nodeInfo);
        }
    }

    public class ImageViewNodeWidget : NodeWidget
    {
        internal SKImage? CurSKImage = null;

        public int ImageSize { get; set; } = 200;

        public ImageViewNodeWidget(NodeGraphView graphView, INodeInfo node) : base(graphView, node)
        {
            if (ParentNode is ImageViewNode imageNode) {
                imageNode.OnImageUpdate += ImageNode_OnImageUpdate;
            }
        }

        public override IWidgetView CreateDefaultView()
        {
            return new ImageViewNodeWidgetView(this);
        }


        private void ImageNode_OnImageUpdate(PixelImage Image)
        {
            CurSKImage = ImageUtil.PixelImageToSKImage(Image);
            //if (Image.Format != PixelImage.EPixelFormat.Encoded) {
            //    if (Image.Format != PixelImage.EPixelFormat.RGBA8)
            //        throw new NotImplementedException($"ImageViewNodeWidget: unsupported pixel format {Image.Format}");
            //    var info = new SKImageInfo(Image.Width, Image.Height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
            //    CurSKImage = SKImage.FromPixelCopy(info, Image.AccessDataUnsafe());
            //} else {
            //    CurSKImage = SKImage.FromEncodedData(Image.AccessDataUnsafe());
            //}
        }


    }



    public class ImageViewNodeWidgetView : NodeWidgetView
    {
        AxisAlignedBox2f ImageArea;
        SKPaint NoImagePaint;
        SKPaint BitmapPaint;

        const float LeftRightMargin = 10;

        public ImageViewNodeWidgetView(NodeWidget nodeWidget) : base(nodeWidget)
        {
            NoImagePaint = new SKPaint { Color = SKColors.White };
            BitmapPaint = new SKPaint() { FilterQuality = SKFilterQuality.Medium };
        }

        public override void UpdateLayout(SKStyleCache StyleCache)
        {
            base.UpdateLayout(StyleCache);
        }

        protected override Vector2f getCustomMinDimensions()
        {
            if (SourceNodeWidget is ImageViewNodeWidget imageWidget) 
                return new Vector2f(imageWidget.ImageSize + 2*LeftRightMargin, 1.0);
            return base.getCustomMinDimensions();
        }

        protected override void updateLayout_Customize(SKStyleCache StyleCache, ref AxisAlignedBox2f NodeBounds)
        {
            float LeftRightMargin = 10;
            float TopBottomMargin = 10;
            float ImageSize = NodeBounds.Width - 2*LeftRightMargin;

            ImageArea = new AxisAlignedBox2f(
                NodeBounds.Min.x + LeftRightMargin, NodeBounds.Max.y - TopBottomMargin,
                NodeBounds.Max.x - LeftRightMargin, NodeBounds.Max.y - TopBottomMargin + ImageSize);
            NodeBounds.Contain(ImageArea);
            NodeBounds.Max.y += TopBottomMargin;
        }

        protected override void draw_Customize(SKStyleCache StyleCache, SKCanvas Canvas, ILayoutAnchor Anchor)
        {
            if (SourceNodeWidget is ImageViewNodeWidget imageWidget) {
                if (imageWidget.CurSKImage != null) {
                    Canvas.DrawImage(imageWidget.CurSKImage, Conversion.ToSkia(ImageArea), BitmapPaint);
                } else {
                    Canvas.DrawRect(Conversion.ToSkia(ImageArea), NoImagePaint);
                }
            }
        }

    }


}
