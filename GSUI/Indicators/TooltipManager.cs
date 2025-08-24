// Copyright Gradientspace Corp. All Rights Reserved.
using g3;
using SkiaSharp;

namespace Gradientspace.UI
{
    // todo: not clear this makes sense to be a static thing anymore. Initially the idea
    // was that Widgets would publish their tooltips directly. However this adds a lot of
    // complexity around placement, so now Widget.GetTooltipStrings is used, and tooltips are
    // updated by the hover-handler at a high enough level that an instance of this type could be used...
    public sealed class TooltipManager
    {
        const double TooltipDelayMilliseconds = 300;
        const double ExtTooltipDelayMilliseconds = 1200;

        // todo: extended tooltip delay...

        private static readonly TooltipManager instance = new TooltipManager();
        public static TooltipManager Instance { get { return instance; } }
        static TooltipManager() {  }

        private TooltipManager() {
            HeartbeatTimer = new System.Timers.Timer(25);
            HeartbeatTimer.AutoReset = true;
            HeartbeatTimer.Elapsed += HeartbeatTimer_Elapsed;
            HeartbeatTimer.Start();

            TooltipStrings = new string[25];
            for (int i = 0; i < TooltipStrings.Length; ++i)
                TooltipStrings[i] = String.Empty;
        }

        System.Timers.Timer HeartbeatTimer;
        private void HeartbeatTimer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            OnUpdateHeartbeat();
        }


        public delegate void OnTooltipDrawUpdatePendingHandler();
        public event OnTooltipDrawUpdatePendingHandler? OnTooltipDrawUpdatePending;


        Widget? ActiveTooltipOwner = null;
        string[] TooltipStrings = [string.Empty];
        int NumCurrentStrings = 0;
        AxisAlignedBox2f TooltipOwnerBounds;
        DateTime LastUpdateTime;
        InputDeviceState LastDeviceState;
        double CurrentDwellMilliseconds = 0;
        bool bHoverDelayElapsed = false;
        bool bExtHoverDelayElapsed = false;

        public void SetActiveTooltipSource(Widget Owner, AxisAlignedBox2f Bounds, InputDeviceState currentDeviceState)
        {
            if (ActiveTooltipOwner != null && ActiveTooltipOwner != Owner) 
                ClearActiveTooltipSource();

            if ( Owner.GetTooltipStrings(out string? tooltip, out string[]? extendedTooltip) )
            {
                TooltipStrings[0] = tooltip!;
                NumCurrentStrings = 1;
                ActiveTooltipOwner = Owner;
                TooltipOwnerBounds = Bounds;
                LastUpdateTime = DateTime.Now;
                LastDeviceState = currentDeviceState;
                CurrentDwellMilliseconds = 0;
                bHoverDelayElapsed = false;

                if (extendedTooltip != null) {
                    TooltipStrings[NumCurrentStrings++] = "";
                    foreach (string line in extendedTooltip) {
						if (NumCurrentStrings < TooltipStrings.Length)
                            TooltipStrings[NumCurrentStrings++] = line;
					}
                }
            }
        }


        public void ClearActiveTooltipSource()
        {
            ActiveTooltipOwner = null;
            bHoverDelayElapsed = false;
            bExtHoverDelayElapsed = false;
        }



        public void OnUpdateDeviceState(InputDeviceState newState)
        {
            bool bMoved = newState.CurrentPosition.Distance(LastDeviceState.CurrentPosition) > 0;
            if ( bMoved && bHoverDelayElapsed == false ) {
                LastDeviceState = newState;
                LastUpdateTime = DateTime.Now;
                CurrentDwellMilliseconds = 0;
            }
        }

        private void OnUpdateHeartbeat()
        {
            double newDwell = (DateTime.Now - LastUpdateTime).TotalMilliseconds;
            if (bHoverDelayElapsed == false) 
            {
                if (CurrentDwellMilliseconds < TooltipDelayMilliseconds && newDwell > TooltipDelayMilliseconds) {
                    OnTooltipDrawUpdatePending?.Invoke();
                    bHoverDelayElapsed = true;
                }
            }
            else if (bExtHoverDelayElapsed == false) 
            {
                if (newDwell > ExtTooltipDelayMilliseconds) {
                    OnTooltipDrawUpdatePending?.Invoke();
                    bExtHoverDelayElapsed = true;
                }
            }
            CurrentDwellMilliseconds = newDwell;
        }


        // use style cache?
        public void Draw(
            SKCanvas Canvas,
            Func<Vector2f,Vector2f> DrawTransform)
        {
            if (ActiveTooltipOwner == null || NumCurrentStrings == 0 || bHoverDelayElapsed == false)
                return;

            SKPaint MainTextPaint = new SKPaint
            {
                Color = SKColors.Black,
                IsAntialias = true,
                LcdRenderText = true,
                SubpixelText = true,
                TextSize = 12,
                Typeface = SKTypeface.FromFamilyName(
                    familyName: "Arial",
                    weight: SKFontStyleWeight.Normal, width: SKFontStyleWidth.Normal, slant: SKFontStyleSlant.Upright)
            };

            SKPaint ExtendedTextPaint = new SKPaint {
                Color = SKColors.Black,
                IsAntialias = true,
                LcdRenderText = true,
                SubpixelText = true,
                TextSize = 10,
                Typeface = SKTypeface.FromFamilyName(
                    familyName: "Arial",
                    weight: SKFontStyleWeight.Normal, width: SKFontStyleWidth.Normal, slant: SKFontStyleSlant.Italic)
            };

            SKPaint FillPaint = new SKPaint
            {
                Color = SKColors.AntiqueWhite
            };
            SKPaint LinePaint = new SKPaint
            {
                Color = SKColors.Black,
                StrokeWidth = 1,
                IsStroke = true
            };

            Vector2f DevicePos = LastDeviceState.CurrentPosition;

            TextHeightInfo mainHeightInfo = SKStyleCache.MeasureTextHeightInfo(MainTextPaint);
            TextHeightInfo extHeightInfo = SKStyleCache.MeasureTextHeightInfo(ExtendedTextPaint);

            float LineSpacing = 2.0f;
            float MarginWidth = 4.0f;

            int NumDrawStrings = (bExtHoverDelayElapsed) ? NumCurrentStrings : 1;

            float MaxWidth = MainTextPaint.MeasureText(TooltipStrings[0]);
            for (int i = 1; i < NumDrawStrings; ++i)
                MaxWidth = Math.Max(MaxWidth, ExtendedTextPaint.MeasureText(TooltipStrings[i]));

            float TotalYHeight = mainHeightInfo.MaxTotalHeight 
                + (NumDrawStrings-1)*extHeightInfo.MaxTotalHeight
                + (NumDrawStrings-1)*LineSpacing;

            AxisAlignedBox2f TextBox = new AxisAlignedBox2f(new Vector2f(0,0), new Vector2f(MaxWidth, -TotalYHeight));
            TextBox.Expand(MarginWidth);

            Vector2f ShowPosition = new Vector2f(DevicePos.x, TooltipOwnerBounds.Min.y);
            Vector2f DrawOrigin = DrawTransform(ShowPosition);
            DrawOrigin.y -= (MarginWidth + 5);
            TextBox.Translate(DrawOrigin);
            DrawOrigin.y += 1;
            DrawOrigin.x += 2;

            Canvas.DrawRect(Conversion.ToSkia(TextBox), FillPaint);
            Canvas.DrawRect(Conversion.ToSkia(TextBox), LinePaint);

            Vector2f TextLineOrigin = DrawOrigin - mainHeightInfo.BelowBaseline;
            for (int i = NumDrawStrings-1; i >= 0; --i)
            {
                string Message = TooltipStrings[i];
                if (i == 0) {
                    Canvas.DrawText(Message, Conversion.ToSkia(TextLineOrigin), MainTextPaint);
                    TextLineOrigin.y -= (mainHeightInfo.MaxTotalHeight + LineSpacing);
                } else {
                    Canvas.DrawText(Message, Conversion.ToSkia(TextLineOrigin), ExtendedTextPaint);
                    TextLineOrigin.y -= (extHeightInfo.MaxTotalHeight + LineSpacing);
                }
            }

        }

    }
}
