using g3;
using SkiaSharp;

namespace Gradientspace.UI
{
    public class Button : Widget, ISimpleCaptureTarget
    {
        public WidgetStateStyle Style { get; set; }

        public delegate void ClickedEventHandler(Button button);
        public event ClickedEventHandler? OnClicked;

        public delegate void HoveredEventHandler(Button button, Vector2f HoverPosition, EWidgetHoverState HoverState);
        public event HoveredEventHandler? OnHoverUpdate;

        Vector2f _dimensions = new Vector2f(15, 15);

        public IWidgetContentExtension? ContentExtension { get; set; } = null;

        public Button(WidgetStateStyle? customStyle = null)
        {
            Style = (customStyle != null) ? customStyle : WidgetStateStyle.DefaultStyle;

            SetInputBehavior(new BasicWidgetInputBehavior(this, this) {
                EnableHover = true,
                Depth = 0
            });
        }


        public Vector2f Dimensions
        {
            get { return _dimensions; }
            set { _dimensions = value; }
        }

        public bool DrawBackground { get; set; } = true;


        public override IWidgetView CreateDefaultView()
        {
            return new ButtonView(this);
        }


        public virtual bool IsCapturing { get; set; }
        public virtual bool IsHovered { get; set; }
        public virtual void UpdateCapture(ISimpleCaptureTarget.ECaptureState State, in InputDeviceState deviceState) {
            bool bWasCapturing = IsCapturing;
            IsCapturing = (State == ISimpleCaptureTarget.ECaptureState.Begin || State == ISimpleCaptureTarget.ECaptureState.Update);
            if ( IsCapturing == false && bWasCapturing == true )
            {
                bool bPointerUpHit = GetActiveView()?.HitTest(deviceState.CurrentPosition) ?? false;
                if (bPointerUpHit)
                    OnClicked?.Invoke(this);
            }
        }
        public virtual void UpdateHover(ISimpleCaptureTarget.EHoverState State, in InputDeviceState deviceState, out bool bContinueHover) 
        {
            IsHovered = (State != ISimpleCaptureTarget.EHoverState.End);
            OnHoverUpdate?.Invoke(this, deviceState.CurrentPosition, (EWidgetHoverState)(int)State);
            bContinueHover = true; 
        }

    }


    public class ButtonView : WidgetView
    {
        public Button SourceButton;

        public AxisAlignedBox2f LocalBounds;
        public Vector2f DrawOrigin;

        public ButtonView(Button sourceButton)
        {
            SourceButton = sourceButton;
        }

        public override Widget GetWidget() { return SourceButton; }

        public override AxisAlignedBox2f BoundsQuery(ILayoutAnchor? RelativeToAnchor = null)
        {
            return (RelativeToAnchor != null) ?
                AnchorLocation.GetAnchoredBounds(LocalBounds, RelativeToAnchor, GetWidget().AnchorPlacement) : LocalBounds;
        }

        public override void UpdateLayout(SKStyleCache StyleCache)
        {
            LocalBounds = new AxisAlignedBox2f(Vector2f.Zero, SourceButton.Dimensions);
        }

        public override void Draw(SKStyleCache StyleCache, SKCanvas Canvas, ILayoutAnchor Anchor)
        {
            DrawOrigin = Anchor.GetOrigin();

            AxisAlignedBox2f PlacedBounds = AnchorLocation.MakeRelativeToAnchor(LocalBounds, SourceButton.AnchorPlacement, DrawOrigin);

            SKStyleCache.CachedSKPaintSet StandardPaints = StyleCache.GetCachedPaintSet(
                SourceButton.Style.Select(SourceButton.IsHovered, SourceButton.IsCapturing) );
            WidgetMargins Margins = SourceButton.Style.BaseMargins;

            if (SourceButton.DrawBackground)
                Canvas.DrawRect(Conversion.ToSkia(PlacedBounds), StandardPaints.BackgroundPaint);

            if (SourceButton.ContentExtension != null)
                SourceButton.ContentExtension.DrawContent(SourceButton, StyleCache, Canvas, PlacedBounds, false);
        }



        public override bool HitTest(Vector2f QueryPoint)
        {
            AxisAlignedBox2f WorldBounds =
                AnchorLocation.GetAnchoredBounds(LocalBounds, DrawOrigin, GetWidget().AnchorPlacement);
            return WorldBounds.Contains(QueryPoint);
        }

        public override bool HitQuery(Vector2f QueryPoint, out WidgetHitResult Result)
        {
            Result = new WidgetHitResult();
            if (HitTest(QueryPoint)) {
                Result = new WidgetHitResult(this, 25);
                return true; 
            }
            return false;
        }


    }
}
