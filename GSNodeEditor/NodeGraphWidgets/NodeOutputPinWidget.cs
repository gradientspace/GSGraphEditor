// Copyright Gradientspace Corp. All Rights Reserved.
using Gradientspace.UI;
using g3;
using Gradientspace.NodeGraph;
using SkiaSharp;

namespace GSNodeEditor
{

    public class NodeOutputPinWidget : NodePinWidget, ISimpleCaptureTarget
    {
        public INodeOutputInfo NodeOutputInfo { get; private set; }
        public string OutputName { get; set; } = "";

        public NodeOutputPinWidget(INodeOutputInfo sourceOutputInfo)
        {
            NodeOutputInfo = sourceOutputInfo;
            OutputName = sourceOutputInfo.OutputName;
            DataType = sourceOutputInfo.DataType;

            WidgetStyle = PinWidgetStyles.DefaultOutputStyleSet;

            SetInputBehavior(new ExtendableWidgetInputBehavior(this, this) { Depth = 0 });

            // set CompactMode on the pin if the input is marked HiddenLabel
            // (maybe not ideal way to do this...)
            if ((sourceOutputInfo.Output.GetOutputFlags() & ENodeOutputFlags.HiddenLabel) != 0)
                CompactMode = true;
        }


        public override bool IsOutputPin { get { return true; } }

        public bool IsSequenceOutputPin { get { return DataType.CSType == typeof(ControlFlowOutputID); } }

        public override string GetDataTypeAsString()
        {
            if (DataType.CSType == typeof(ControlFlowOutputID))
                return "(Exec)";

			string? CustomTypeString = DataType.ExtendedTypeInfo?.GetCustomTypeString() ?? null;
			return CustomTypeString ?? TypeUtils.TypeToString(DataType.CSType);
        }


        public override IWidgetView CreateDefaultView()
        {
            return new NodeOutputPinWidgetView(this);
        }

        public override bool GetTooltipStrings(out string? tooltip, out string[]? extendedTooltip)
        {
            tooltip = GetDataTypeAsString();
            extendedTooltip = null;
            return true;
        }


        // ISimpleCaptureTarget API
        public void UpdateCapture(ISimpleCaptureTarget.ECaptureState State, in InputDeviceState deviceState)
        {
            IsCapturing = (State == ISimpleCaptureTarget.ECaptureState.Begin || State == ISimpleCaptureTarget.ECaptureState.Update);
        }
        public void UpdateHover(ISimpleCaptureTarget.EHoverState State, in InputDeviceState deviceState, out bool bContinueHover)
        {
            bContinueHover = true;
            IsHovered = (State == ISimpleCaptureTarget.EHoverState.Begin || State == ISimpleCaptureTarget.EHoverState.Update);
        }
        public bool IsHovered { get; private set; }
        public bool IsCapturing { get; private set; }


    }



    public class NodeOutputPinWidgetView : IWidgetView
    {
        NodeOutputPinWidget SourcePinWidget;
		public int LastDrawOrderIndex { get; set; } = 0;

		public NodeOutputPinWidgetView(NodeOutputPinWidget pinWidget)
        {
            this.SourcePinWidget = pinWidget;
        }

        public Widget GetWidget() { return SourcePinWidget; }

        public AxisAlignedBox2f LocalBounds { get; set; }
        public Vector2f DrawOrigin { get; set; }

        public AxisAlignedBox2f BoundsQuery(ILayoutAnchor? RelativeToAnchor = null)
        {
            return (RelativeToAnchor != null) ?
                AnchorLocation.GetAnchoredBounds(LocalBounds, RelativeToAnchor, GetWidget().AnchorPlacement) : LocalBounds;
        }

        public void UpdateLayout(SKStyleCache StyleCache)
        {
            SKPaint PinTextPaint =
                StyleCache.GetCachedPaint(SourcePinWidget.WidgetStyle.StandardStyle, SKStyleCache.EPaintType.Text);
            WidgetMargins PinMargins = SourcePinWidget.WidgetStyle.BaseMargins;

            float OutputTextWidth = (SourcePinWidget.CompactMode) ? 5 : PinTextPaint.MeasureText(SourcePinWidget.OutputName);

            TextHeightInfo PinTextHeightInfo = StyleCache.GetCachedFontHeightInfo(SourcePinWidget.WidgetStyle.StandardStyle);
            float OutputPinRight = (OutputTextWidth + PinMargins.TotalWidth);

            LocalBounds = new AxisAlignedBox2f(0, 0, OutputPinRight, PinTextHeightInfo.MaxTotalHeight + PinMargins.TotalHeight);
        }


        public bool HitQuery(Vector2f QueryPoint, out WidgetHitResult Result)
        {
            Result = WidgetHitResult.None;

            AxisAlignedBox2f WorldBounds =
                AnchorLocation.GetAnchoredBounds(LocalBounds, DrawOrigin, GetWidget().AnchorPlacement);
            if (WorldBounds.Contains(QueryPoint) == false)
                return false;

            Result = new WidgetHitResult() { HitWidget = SourcePinWidget, HitZDepth = 4 };
            return true;
        }


        public bool HitTest(Vector2f QueryPoint)
        {
            WidgetHitResult HitResult;
            return HitQuery(QueryPoint, out HitResult);
        }


        public void Draw(SKStyleCache StyleCache, SKCanvas Canvas, ILayoutAnchor Anchor)
        {
            NodePinWidgetStyle UseStyle = SourcePinWidget.WidgetStyle; //PinWidgetStyles.DefaultOutputStyleSet;
            if (SourcePinWidget.IsSequenceOutputPin)
                UseStyle = PinWidgetStyles.OutputStyleSet_ControlFlow;

            SKPaint PinTextPaint = StyleCache.GetCachedPaint(UseStyle.StandardStyle, SKStyleCache.EPaintType.Text);
            WidgetMargins PinMargins = UseStyle.BaseMargins;
            TextHeightInfo PinTextHeightInfo = StyleCache.GetCachedFontHeightInfo(UseStyle.StandardStyle);

            SKPaint OutputPinPaint = StyleCache.GetCachedPaint( UseStyle.Select(SourcePinWidget.IsHovered, false), SKStyleCache.EPaintType.Background);

            DrawOrigin = Anchor.GetOrigin();
            AxisAlignedBox2f PlacedBounds = AnchorLocation.MakeRelativeToAnchor(LocalBounds, SourcePinWidget.AnchorPlacement, DrawOrigin);

            if (SourcePinWidget.IsSequenceOutputPin)
            {
                SKPath p = new SKPath();
                p.MoveTo(Conversion.ToSkia(PlacedBounds.TopLeft));
                p.LineTo(Conversion.ToSkia(PlacedBounds.TopRight));
                p.LineTo(Conversion.ToSkia(PlacedBounds.CenterRight + new Vector2f(5, 0)));
                p.LineTo(Conversion.ToSkia(PlacedBounds.BottomRight));
                p.LineTo(Conversion.ToSkia(PlacedBounds.BottomLeft));
                p.LineTo(Conversion.ToSkia(PlacedBounds.TopLeft));
                Canvas.DrawPath(p, OutputPinPaint);
            }
            else
            {
                Canvas.DrawRect(Conversion.ToSkia(PlacedBounds), OutputPinPaint);
            }
            Vector2f TextCorner = PlacedBounds.Min + new Vector2f(PinMargins.Left, PinMargins.Top + PinTextHeightInfo.AboveBaseline);
            if (SourcePinWidget.CompactMode == false)
                Canvas.DrawText(SourcePinWidget.OutputName, Conversion.ToSkia(TextCorner), PinTextPaint);
        }

    }
}
