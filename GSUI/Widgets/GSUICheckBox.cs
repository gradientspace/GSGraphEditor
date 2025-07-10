// Copyright Gradientspace Corp. All Rights Reserved.
using g3;
using SkiaSharp;

namespace Gradientspace.UI
{
    public class Checkbox : Widget, ISimpleCaptureTarget
    {
        public WidgetStateStyle Style { get; set; }

        public event EventHandler? OnToggled;

        Vector2f _dimensions = new Vector2f(15, 15);
        bool _checked = true;

        public Checkbox(WidgetStateStyle? customStyle = null)
        {
            Style = (customStyle != null) ? customStyle : WidgetStateStyle.DefaultStyle;

            SetInputBehavior(new BasicWidgetInputBehavior(this, this)
            {
                EnableHover = false,
                Depth = 0
            });
        }


        public bool Checked
        {
            get { return _checked; }
            set { _checked = value; OnToggled?.Invoke(this, EventArgs.Empty); }
        }

        public Vector2f Dimensions
        {
            get { return _dimensions; }
            set { _dimensions = value; }
        }


        public override IWidgetView CreateDefaultView()
        {
            return new CheckboxView(this);
        }


        protected virtual bool IsCapturing { get; set; }
        public virtual void UpdateCapture(ISimpleCaptureTarget.ECaptureState State, in InputDeviceState deviceState) {
            bool bWasCapturing = IsCapturing;
            IsCapturing = (State == ISimpleCaptureTarget.ECaptureState.Begin || State == ISimpleCaptureTarget.ECaptureState.Update);
            if ( IsCapturing == false && bWasCapturing == true )
            {
                bool bPointerUpHit = GetActiveView()?.HitTest(deviceState.CurrentPosition) ?? false;
                if (bPointerUpHit)
                {
                    Checked = !Checked;
                }
            }
        }
        public virtual void UpdateHover(ISimpleCaptureTarget.EHoverState State, in InputDeviceState deviceState, out bool bContinueHover) { bContinueHover = true; }

    }


    public class CheckboxView : WidgetView
    {
        public Checkbox SourceCheckBox;

        public AxisAlignedBox2f LocalBounds;
        public Vector2f DrawOrigin;

        public CheckboxView(Checkbox sourceCheckBox)
        {
            SourceCheckBox = sourceCheckBox;
        }

        public override Widget GetWidget() { return SourceCheckBox; }

        public override AxisAlignedBox2f BoundsQuery(ILayoutAnchor? RelativeToAnchor = null)
        {
            return (RelativeToAnchor != null) ?
                AnchorLocation.GetAnchoredBounds(LocalBounds, RelativeToAnchor, GetWidget().AnchorPlacement) : LocalBounds;
        }

        public override void UpdateLayout(SKStyleCache StyleCache)
        {
            LocalBounds = new AxisAlignedBox2f(Vector2f.Zero, SourceCheckBox.Dimensions);
        }

        public override void Draw(SKStyleCache StyleCache, SKCanvas Canvas, ILayoutAnchor Anchor)
        {
            DrawOrigin = Anchor.GetOrigin();

            AxisAlignedBox2f PlacedBounds = AnchorLocation.MakeRelativeToAnchor(LocalBounds, SourceCheckBox.AnchorPlacement, DrawOrigin);

            SKStyleCache.CachedSKPaintSet StandardPaints = StyleCache.GetCachedPaintSet(SourceCheckBox.Style.StandardStyle);
            WidgetMargins Margins = SourceCheckBox.Style.BaseMargins;

            Canvas.DrawRect(Conversion.ToSkia(PlacedBounds), StandardPaints.BackgroundPaint);
            if (SourceCheckBox.Checked)
            {
                AxisAlignedBox2f InnerBounds = PlacedBounds;
                InnerBounds.Contract(Margins.TotalWidth);
                Canvas.DrawRect(Conversion.ToSkia(InnerBounds), StandardPaints.ForegroundPaint);
            }
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
