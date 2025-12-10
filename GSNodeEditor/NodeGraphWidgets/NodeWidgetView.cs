// Copyright Gradientspace Corp. All Rights Reserved.
using g3;
using Gradientspace.NodeGraph;
using Gradientspace.UI;
using SkiaSharp;

namespace GSNodeEditor
{
    public class NodeWidgetView : IWidgetView
    {
        public NodeWidget SourceNodeWidget;
		public int LastDrawOrderIndex { get; set; } = 0;

		public NodeWidgetView(NodeWidget nodeWidget )
        {
            this.SourceNodeWidget = nodeWidget;
        }

        public Widget GetWidget() { return SourceNodeWidget; }

        // all positions are in local coordinates
        public AxisAlignedBox2f ChildBounds { get; set; }
        public AxisAlignedBox2f LocalNodeBounds { get; set; }
        public string DrawLabel { get; set; } = string.Empty;
        public string VersionLabel { get; set; } = string.Empty;
        public Vector2f LabelOrigin { get; set; }

        List<(int, int)> InOutMatches = new List<(int, int)>();

        static SKPaint InOutCurvePaint = new SKPaint() {
            Color = new SKColor(255, 165, 0, 80),
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 2,
            IsAntialias = true,
            PathEffect = SKPathEffect.CreateDash(new float[] { 3, 3 }, 10)
        };

        public virtual AxisAlignedBox2f BoundsQuery(ILayoutAnchor? RelativeToAnchor = null)
        {
            return (RelativeToAnchor != null) ?
                AnchorLocation.GetAnchoredBounds(LocalNodeBounds, RelativeToAnchor, GetWidget().AnchorPlacement) : LocalNodeBounds;
        }

        protected virtual Vector2f getCustomMinDimensions()
        {
            return Vector2f.One;
        }

        public virtual void UpdateLayout(SKStyleCache StyleCache)
        {
            // TODO: this lays out all the pins by creating an Anchor for each one.
            // Probably could just be using offsets...
            // (this was some of the oldest layout code and is probably crufty)

            // box size is fully determined by code below and so we don't need NodeWidget to have a size...
            //AxisAlignedBox2f InitialBox = new AxisAlignedBox2f(Vector2f.Zero, SourceNodeWidget.Size);
            AxisAlignedBox2f InitialBox = new AxisAlignedBox2f(Vector2f.Zero, getCustomMinDimensions());

            SKPaint LabelTextPaint = 
                StyleCache.GetCachedPaint(SourceNodeWidget.WidgetStyle.NodeStyle.StandardStyle, SKStyleCache.EPaintType.Text);
            WidgetMargins LabelMargins = SourceNodeWidget.WidgetStyle.NodeStyle.BaseMargins;

            SKPaint PinTextPaint =
                StyleCache.GetCachedPaint(PinWidgetStyles.DefaultInputStandardStyle, SKStyleCache.EPaintType.Text);
            WidgetMargins PinMargins = PinWidgetStyles.DefaultInputStandardStyle.Margins;

            string UseLabel = (SourceNodeWidget.Label.Length > 0) ? SourceNodeWidget.Label : "(Node)";
            VersionLabel = SourceNodeWidget.VersionLabel;

            TextHeightInfo LabelTextHeightInfo = StyleCache.GetCachedFontHeightInfo(SourceNodeWidget.WidgetStyle.NodeStyle.StandardStyle);
            float LabelWidth = LabelTextPaint.MeasureText(UseLabel);

            // assuming default sequence pins are the same width...
            float SequencePinWidth = 0;
            if (SourceNodeWidget.InputSequenceWidget != null)
                SequencePinWidth = SourceNodeWidget.InputSequenceWidget.Dimensions.x;
            else if (SourceNodeWidget.OutputSequenceWidget != null)
                SequencePinWidth = SourceNodeWidget.OutputSequenceWidget.Dimensions.x;

            //int NumInputs = SourceNodeWidget.Inputs.Count;
            int NumInputs = SourceNodeWidget.InputWidgets.Count;
            int NumOutputs = SourceNodeWidget.OutputWidgets.Count;

            const float PinNodeEdgeOffset = 5;
            const float PinNodeConstantEdgeOffset = 2;
            const float PinVerticalSpace = 5;

            Span<AxisAlignedBox2f> InputPinBounds = stackalloc AxisAlignedBox2f[NumInputs];
            //Span<AxisAlignedBox2f> InputPinBounds = new AxisAlignedBox2f[NumInputs];      // for debugging because stackalloc prevents hot-recompile...

            // compute max input and output pin text length
            InOutMatches.Clear();
            float MaxInputPinWidth = 0;
            for (int k = 0; k < NumInputs; ++k)
            {
                //float InputTextWidth = PinTextPaint.MeasureText(SourceNodeWidget.InputWidgets[k].InputName);
                //MaxInputPinWidth = MathF.Max(MaxInputPinWidth, InputTextWidth);
                SourceNodeWidget.InputWidgets[k].GetActiveView()?.UpdateLayout(StyleCache);
                AxisAlignedBox2f childBounds = SourceNodeWidget.InputWidgets[k].GetActiveView()?.BoundsQuery(null) ?? AxisAlignedBox2f.Empty;
                InputPinBounds[k] = childBounds;
                float ChildWidth = childBounds.Width;
                if (SourceNodeWidget.InputWidgets[k].NodeInputInfo.IsNodeConstant)
                    ChildWidth += (PinNodeEdgeOffset+PinNodeConstantEdgeOffset);      // otherwise constant val may overlap rhs

                MaxInputPinWidth = MathF.Max(MaxInputPinWidth, ChildWidth);

                // probably should be figured out at Widget level...
                // (possibly even requires querying the node...)
                if (SourceNodeWidget.InputWidgets[k].NodeInputInfo.IsInOut) {
                    for (int j = 0; j < NumOutputs; ++j) {
                        if ( SourceNodeWidget.OutputWidgets[j].OutputName == SourceNodeWidget.InputWidgets[k].InputName ) {
                            InOutMatches.Add(new(k, j));
                            break;
                        }
                    }
                }
            }

            float MaxOutputPinWidth = 0;
            for (int k = 0; k < NumOutputs; ++k)
            {
                //float OutputTextWidth = PinTextPaint.MeasureText(SourceNodeWidget.OutputWidgets[k].OutputName); ;
                //MaxOutputPinWidth = MathF.Max(MaxOutputPinWidth, OutputTextWidth);

                SourceNodeWidget.OutputWidgets[k].GetActiveView()?.UpdateLayout(StyleCache);
                AxisAlignedBox2f childBounds = SourceNodeWidget.OutputWidgets[k].GetActiveView()?.BoundsQuery(null) ?? AxisAlignedBox2f.Empty;
                MaxOutputPinWidth = MathF.Max(MaxOutputPinWidth, childBounds.Width);
            }

            // expand rect to contain label, and make sure label does not overlap sequence pins
            if (InitialBox.Width < (LabelWidth + 2*SequencePinWidth + LabelMargins.TotalWidth) )
            {
                float extra = (LabelWidth + 2*SequencePinWidth + LabelMargins.TotalWidth) - InitialBox.Width;
                InitialBox.Max.x += extra;
                LabelWidth += 2 * SequencePinWidth;
            }
            else
                SequencePinWidth = 0;

            float TotalPinWidth = MaxInputPinWidth + MaxOutputPinWidth;// + 2*PinNodeEdgeOffset;
            if (InitialBox.Width < TotalPinWidth)
            {
                float extra = TotalPinWidth - InitialBox.Width;
                InitialBox.Max.x += extra;
            }

            // truncate too-long label with ...
            //if (NodeBox.Width < LabelWidth + 2 * LabelMargins.x)
            //{
            //    float dotsWidth = LabelTextPaint.MeasureText("...");
            //    long breakAt = LabelTextPaint.BreakText(UseLabel, NodeBox.Width - 2 * LabelMargins.x - dotsWidth);
            //    UseLabel = UseLabel.Substring(0, (int)breakAt) + "...";
            //    LabelWidth = LabelTextPaint.MeasureText(UseLabel);
            //}

            this.DrawLabel = UseLabel;

            // centered
            float LabelX = (InitialBox.Width / 2.0f) - (LabelWidth / 2.0f);       
            float LabelY = LabelMargins.Top + LabelTextHeightInfo.AboveBaseline;
            LabelOrigin = new Vector2f(LabelX + SequencePinWidth, LabelY);

            float PinStartOffsetY = InitialBox.Min.y + (LabelTextHeightInfo.MaxTotalHeight + LabelMargins.TotalHeight);
            float InputPinLeft = InitialBox.Min.x;
            float InputPinRight = InputPinLeft + (MaxInputPinWidth + PinMargins.TotalWidth);

            TextHeightInfo PinTextHeightInfo = StyleCache.GetCachedFontHeightInfo(PinWidgetStyles.DefaultInputStandardStyle);

            // layout input pin boxes
            float CurPinY = PinStartOffsetY;
            float MaxY = CurPinY;
            for (int k = 0; k < NumInputs; ++k)
            {
                // expand pin height for tall widgets. Conceivably we should always use the PinBounds,
                // however this currently makes all pins look ugly, so hack it to only make tall pins look ugly...
                float PinHeight = PinTextHeightInfo.MaxTotalHeight;
                if (InputPinBounds[k].Height > PinHeight*1.5)
                    PinHeight = InputPinBounds[k].Height;

                float BottomY = CurPinY + (PinHeight + PinMargins.TotalHeight);
                bool bIsConstant = SourceNodeWidget.InputWidgets[k].NodeInputInfo.IsNodeConstant;
                float shiftX = (bIsConstant) ? PinNodeConstantEdgeOffset : -PinNodeEdgeOffset;
                AxisAlignedBox2f localPinBox = new AxisAlignedBox2f(InputPinLeft+shiftX, CurPinY, InputPinRight+shiftX, BottomY);
                CurPinY = BottomY + PinVerticalSpace;
                MaxY = Math.Max(MaxY, CurPinY);

                SourceNodeWidget.InputWidgetAnchors[k].Box = localPinBox;
            }

            // now do output pins
            float OutputPinRight = InitialBox.Max.x + PinNodeEdgeOffset;
            float OutputPinLeft = OutputPinRight - (MaxOutputPinWidth + PinMargins.TotalWidth);

            // layout output pin boxes
            CurPinY = PinStartOffsetY;
            for (int k = 0; k < NumOutputs; ++k)
            {
                float BottomY = CurPinY + (PinTextHeightInfo.MaxTotalHeight + PinMargins.TotalHeight);
                AxisAlignedBox2f localPinBox = new AxisAlignedBox2f(OutputPinLeft, CurPinY, OutputPinRight, BottomY);
                CurPinY = BottomY + PinVerticalSpace;
                MaxY = Math.Max(MaxY, CurPinY);

                SourceNodeWidget.OutputWidgetAnchors[k].Box = localPinBox;
            }

            MaxY += 2;      // add a bit more space after the lowest pin

            if ( MaxY > InitialBox.Max.y )
                InitialBox.Max.y = MaxY;

            updateLayout_Customize(StyleCache, ref InitialBox);

            this.LocalNodeBounds = InitialBox;
            this.ChildBounds = InitialBox;

            updateLayout_SequencePins(StyleCache, LocalNodeBounds);
            updateLayout_FeedbackDecorators(StyleCache, LocalNodeBounds);
            updateLayout_VariableInOutWidgets(StyleCache, LocalNodeBounds);
        }

        protected virtual void updateLayout_Customize(SKStyleCache StyleCache, ref AxisAlignedBox2f NodeBounds)
        {
            // this is for subclases to implement
        }

        protected virtual void updateLayout_SequencePins(SKStyleCache StyleCache, AxisAlignedBox2f NodeBounds)
        {
            SourceNodeWidget.InputSequenceWidget?.GetActiveView()?.UpdateLayout(StyleCache);
            SourceNodeWidget.OutputSequenceWidget?.GetActiveView()?.UpdateLayout(StyleCache);

            if (SourceNodeWidget.InputSequenceWidgetAnchor != null)
                SourceNodeWidget.InputSequenceWidgetAnchor.Box = NodeBounds;
            if (SourceNodeWidget.OutputSequenceWidgetAnchor != null)
                SourceNodeWidget.OutputSequenceWidgetAnchor.Box = NodeBounds;
        }

        protected virtual void updateLayout_FeedbackDecorators(SKStyleCache StyleCache, AxisAlignedBox2f NodeBounds)
        {
            if (SourceNodeWidget.ErrorWidget.ParentWidget != null) {
                SourceNodeWidget.ErrorWidget.GetActiveView()?.UpdateLayout(StyleCache);
                SourceNodeWidget.ErrorWidgetAnchor.Box = NodeBounds;
            }
        }

        protected virtual void updateLayout_VariableInOutWidgets(SKStyleCache StyleCache, AxisAlignedBox2f NodeBounds)
        {
            if (SourceNodeWidget.AddInputWidget?.ParentWidget != null) {
                SourceNodeWidget.AddInputWidget.GetActiveView()?.UpdateLayout(StyleCache);
                SourceNodeWidget.AddInputWidgetAnchor!.Box = NodeBounds;
            }
            if (SourceNodeWidget.RemoveInputWidget?.ParentWidget != null) {
                SourceNodeWidget.RemoveInputWidget.GetActiveView()?.UpdateLayout(StyleCache);
                SourceNodeWidget.RemoveInputWidgetAnchor!.Box = new AxisAlignedBox2f(Vector2f.Zero, SourceNodeWidget.AddInputWidget!.Dimensions);
            }
            if (SourceNodeWidget.AddOutputWidget?.ParentWidget != null) {
                SourceNodeWidget.AddOutputWidget.GetActiveView()?.UpdateLayout(StyleCache);
                SourceNodeWidget.AddOutputWidgetAnchor!.Box = NodeBounds;
            }
            if (SourceNodeWidget.RemoveOutputWidget?.ParentWidget != null) {
                SourceNodeWidget.RemoveOutputWidget.GetActiveView()?.UpdateLayout(StyleCache);
                SourceNodeWidget.RemoveOutputWidgetAnchor!.Box = new AxisAlignedBox2f(Vector2f.Zero, SourceNodeWidget.AddOutputWidget!.Dimensions);
            }
        }


        public virtual bool HitQuery(Vector2f QueryPoint, out WidgetHitResult Result)
        {
            Result = WidgetHitResult.None;

            QueryPoint -= SourceNodeWidget.GetAnchor().GetOrigin();

            if (ChildBounds.Contains(QueryPoint) == false)
                return false;

            if (LocalNodeBounds.Contains(QueryPoint))
            {
                Result = new WidgetHitResult() { HitWidget = SourceNodeWidget };
                return true;
            }
            return false;
        }

        public virtual bool HitTest(Vector2f QueryPoint)
        {
            WidgetHitResult HitResult;
            return HitQuery(QueryPoint, out HitResult);
        }


        public virtual void Draw(SKStyleCache StyleCache, SKCanvas Canvas, ILayoutAnchor Anchor)
        {
            WidgetStateStyle UseStateStyle = (SourceNodeWidget.NodeState == NodeWidget.NodeStates.Error) ?
                NodeWidgetStyles.NodeErrorStyleSet : SourceNodeWidget.WidgetStyle.NodeStyle;
            if (DebugManager.Instance.IsNodeActive(SourceNodeWidget.GraphNodeIdentifier))
                UseStateStyle = NodeWidgetStyles.NodeDebugStyleSet;
            WidgetStyle NodeFillStyle = UseStateStyle.Select(SourceNodeWidget.IsHovered, false);
            SKPaint NodePaint = StyleCache.GetCachedPaint(NodeFillStyle, SKStyleCache.EPaintType.Background);

            SKPaint LabelTextPaint =
                StyleCache.GetCachedPaint(SourceNodeWidget.WidgetStyle.NodeStyle.StandardStyle, SKStyleCache.EPaintType.Text);
            WidgetMargins LabelMargins = SourceNodeWidget.WidgetStyle.NodeStyle.BaseMargins;

            Vector2f DrawOrigin = Anchor.GetOrigin();
            AxisAlignedBox2f PlacedBounds = AnchorLocation.MakeRelativeToAnchor(LocalNodeBounds, SourceNodeWidget.AnchorPlacement, DrawOrigin);

            SKMatrix InitialMatrix = Canvas.TotalMatrix;
            Canvas.Translate( Conversion.ToSkia(PlacedBounds.Min) );

            AxisAlignedBox2f DrawBounds = LocalNodeBounds;
            SKRect Rect = Conversion.ToSkia(DrawBounds);
            SKSize Radius = new(5, 5);

            Canvas.DrawRoundRect(Rect, Radius, NodePaint);

            if ( SourceNodeWidget.HideLabel == false ) {
                string UseLabel = DrawLabel;
                Canvas.DrawText(UseLabel, LabelOrigin.x, LabelOrigin.y, LabelTextPaint);
            }

            // draw little curves beween in and out pins for inout fields
            if (InOutMatches.Count > 0) {
                foreach ((int k, int j) in InOutMatches) {
                    Vector2f Start = SourceNodeWidget.InputWidgetAnchors[k].Box.CenterLeft;
                    Vector2f End = SourceNodeWidget.OutputWidgetAnchors[j].Box.CenterRight;
                    float d = (End.x-Start.x) * 0.75f;
                    SKPath Curve = new SKPath();
                    Curve.MoveTo(Conversion.ToSkia(Start));
                    Curve.CubicTo(new SKPoint(Start.x+d, Start.y), new SKPoint(End.x-d, End.y), Conversion.ToSkia(End));
                    Canvas.DrawPath(Curve, InOutCurvePaint);
                }
            }

            // todo same code as InputPinWidget, w/ different offset - should refactor...
            if (VersionLabel.Length > 0) {
                SKPaint DataTypeTextPaint = new SKPaint { Color = SKColors.White, IsAntialias = true, LcdRenderText = true, SubpixelText = true, TextSize = 10 };
                TextHeightInfo DataTypeTextHeightInfo = SKStyleCache.MeasureTextHeightInfo(DataTypeTextPaint);
                SKPaint WarningDataTypeFillPaint = new SKPaint { Color = SKColors.DarkOrange };
                const float Margin = 3;
                string TypeText = VersionLabel;
                SKRect Bounds = SKRect.Empty;
                float Width = DataTypeTextPaint.MeasureText(TypeText, ref Bounds);
                Bounds.Left -= (Margin + 2); Bounds.Right += (Margin + 1); Bounds.Bottom += Margin; Bounds.Top -= Margin;
                SKMatrix CurMatrix = Canvas.TotalMatrix;
                SKPoint Offset = Conversion.ToSkia(new Vector2f(LocalNodeBounds.Width - Width, LocalNodeBounds.Height+3));
                Canvas.Translate(Offset);
                Canvas.DrawRoundRect(Bounds, 8.0f, 8.0f, WarningDataTypeFillPaint);
                Canvas.DrawText(TypeText, new SKPoint(0, 0), DataTypeTextPaint);
                Canvas.SetMatrix(CurMatrix);
            }

            draw_Customize(StyleCache, Canvas, Anchor);

            Canvas.SetMatrix(InitialMatrix);
        }


        protected virtual void draw_Customize(SKStyleCache StyleCache, SKCanvas Canvas, ILayoutAnchor Anchor)
        {

        }

    }


}
