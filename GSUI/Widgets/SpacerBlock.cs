// Copyright Gradientspace Corp. All Rights Reserved.
using g3;
using SkiaSharp;

namespace Gradientspace.UI
{
    public class SpacerBlock : Widget
    {
        Vector2f _dimensions = new Vector2f(15, 15);

        public IWidgetContentExtension? ContentExtension { get; set; } = null;

        public SpacerBlock()
        {
        }
        public SpacerBlock(Vector2f DimensionsIn)
        {
            _dimensions = DimensionsIn;
        }

        public Vector2f Dimensions
        {
            get { return _dimensions; }
            set { _dimensions = value; }
        }

        public override IWidgetView CreateDefaultView()
        {
            return new SpacerBlockView(this);
        }
    }


    public class SpacerBlockView : WidgetView
    {
        public SpacerBlock SourceSpacerBlock;

        public AxisAlignedBox2f LocalBounds;
        public Vector2f DrawOrigin;

        public SpacerBlockView(SpacerBlock sourceSpacerBlock)
        {
            SourceSpacerBlock = sourceSpacerBlock;
        }

        public override Widget GetWidget() { return SourceSpacerBlock; }

        public override AxisAlignedBox2f BoundsQuery(ILayoutAnchor? RelativeToAnchor = null)
        {
            return (RelativeToAnchor != null) ?
                AnchorLocation.GetAnchoredBounds(LocalBounds, RelativeToAnchor, GetWidget().AnchorPlacement) : LocalBounds;
        }

        public override void UpdateLayout(SKStyleCache StyleCache)
        {
            LocalBounds = new AxisAlignedBox2f(Vector2f.Zero, SourceSpacerBlock.Dimensions);
        }

        public override void Draw(SKStyleCache StyleCache, SKCanvas Canvas, ILayoutAnchor Anchor)
        {
            DrawOrigin = Anchor.GetOrigin();
            AxisAlignedBox2f PlacedBounds = AnchorLocation.MakeRelativeToAnchor(LocalBounds, SourceSpacerBlock.AnchorPlacement, DrawOrigin);

            if (SourceSpacerBlock.ContentExtension != null)
                SourceSpacerBlock.ContentExtension.DrawContent(SourceSpacerBlock, StyleCache, Canvas, PlacedBounds, false);

        }



        public override bool HitTest(Vector2f QueryPoint)
        {
            return false;
        }
        public override bool HitQuery(Vector2f QueryPoint, out WidgetHitResult Result)
        {
            Result = WidgetHitResult.None;
            return false;
        }


    }
}
