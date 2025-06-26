using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using g3;
using SkiaSharp;
using static Gradientspace.UI.ISimpleCaptureTarget;
using static Gradientspace.UI.ITextEntryFocusTarget;


namespace Gradientspace.UI
{
    public class TextEntryField : Widget, ISimpleCaptureTarget, ITextEntryFocusTarget
    {
        public WidgetStateStyle Style { get; set; }

        public delegate void TextModifiedEventHandler(TextEntryField sender, string oldText, string newText);
        public event TextModifiedEventHandler? OnTextModified;

        public delegate void TextEditingStateEventHandler(TextEntryField sender, bool bEditingEnded);
        public event TextEditingStateEventHandler? OnTextEditingStateUpdate;

        public delegate void TextEditUpdateEventHandler(TextEntryField sender, string newText);
        public event TextEditUpdateEventHandler? OnTextEditingUpdate;

        public bool EnableClearOnEscape = false;
        public bool KeepFocusOnEnter = false;
        public bool RenderOnTopWhileEditing = true;

        Vector2f _dimensions = new Vector2f(60, 15);
        string _text = "";

        public enum StringValidation
        {
            None,
            Integer,
            Real
        }
        public StringValidation ValidationType { get; set; } = StringValidation.None;


        public TextEntryField(WidgetStateStyle? customStyle = null)
        {
            Style = (customStyle != null) ? customStyle : DefaultWidgetStyles.DefaultTextFieldStyle;

            SetInputBehavior(new BasicWidgetInputBehavior(this, this)
            {
                EnableHover = true,
                Depth = 0
            });
        }


        public string Text
        {
            get { return _text; }
            set {
                string NewString = value;
                if ( _text != NewString && ValidateNewString(ref NewString) )
                {
                    string oldText = _text;
                    _text = NewString;
                    OnTextModified?.Invoke(this, oldText, _text);
                    OnTextEditingUpdate?.Invoke(this, _text);
                }
            }
        }

        public string ActiveText
        {
            get { return (ActiveStringEdit != null) ? ActiveStringEdit.CurrentString : _text; }
        }

        public bool IsEditing { get { return ActiveStringEdit != null; } }

        public Vector2f Dimensions
        {
            get { return _dimensions; }
            set { _dimensions = value; }
        }
        public float Width
        {
            get { return _dimensions.x; }
            set { _dimensions.x = value; }
        }

        public override IWidgetView CreateDefaultView()
        {
            return new TextEntryFieldView(this);
        }


        //! this function can be called externally, to assign focus to the text entry field
        public virtual void BeginStringEdit()
        {
            ActiveStringEdit = new StringEditor(Text);
            ActiveStringEdit.SelectAll();

            ActiveStringEdit.OnStringEditUpdate += (StringEditor editor, string newString) => {
                OnTextEditingUpdate?.Invoke(this, newString);
            };

            if (ValidationType == StringValidation.Integer)
                ActiveStringEdit.ConfigureForInteger();
            else if (ValidationType == StringValidation.Real)
                ActiveStringEdit.ConfigureForReal();

            IsFocused = true;

            if (RenderOnTopWhileEditing) { 
                // TODO should be saving/restoring current values, not defaults...
                InheritParentDepth = false;
                RenderDepth = new WidgetDepth(WidgetDepthLayers.Overlay1);
            }

            SystemKeyboardRouter.Instance.SetTextEntryFocusTarget(this);
            // capture text entry...

            OnTextEditingStateUpdate?.Invoke(this, false);
        }


        public virtual bool IsFocused { get; set; }
        public virtual bool IsHovered { get; set; }
        public virtual bool IsCapturing { get; set; }
        protected StringEditor? ActiveStringEdit = null;
        public virtual void UpdateCapture(ISimpleCaptureTarget.ECaptureState State, in InputDeviceState deviceState) {
            bool bWasCapturing = IsCapturing;
            IsCapturing = (State == ISimpleCaptureTarget.ECaptureState.Begin || State == ISimpleCaptureTarget.ECaptureState.Update);
            if ( IsCapturing == false && bWasCapturing == true )
            {
                bool bPointerUpHit = GetActiveView()?.HitTest(deviceState.CurrentPosition) ?? false;
                if (bPointerUpHit) {
                    BeginStringEdit();
                }
            }
        }
        public virtual void UpdateHover(ISimpleCaptureTarget.EHoverState State, in InputDeviceState deviceState, out bool bContinueHover) 
        {
            IsHovered = (State == EHoverState.Begin || State == EHoverState.Update);
            bContinueHover = true; 
        }
        internal int GetActiveEditCursorLocation()
        {
            return (ActiveStringEdit != null) ? ActiveStringEdit.CursorLocation : -1;
        }
        internal bool GetSelectionRange(out int StartIndex, out int EndIndex)
        {
            StartIndex = EndIndex = -1;
            if (ActiveStringEdit != null && ActiveStringEdit.HasSelection)
            {
                StartIndex = ActiveStringEdit.SelectionStartLocation;
                EndIndex = ActiveStringEdit.SelectionEndLocation;
                return true;
            }
            return false;
        }

        // ITextEntryFocusTarget API

        public bool OnNextKey(KeyState keyState)
        {
            if ( keyState.KeyName == KeyNames.Tab ) {
                SystemKeyboardRouter.Instance.ClearTextEntryFocusTarget(EndFocusType.Commit);
                return true;
            }

            if (ActiveStringEdit != null) 
                return ActiveStringEdit.OnNextKey(keyState);
            
            return false;
        }

        public void OnPasteText(string pasteString)
        {
            if (ActiveStringEdit != null)
                ActiveStringEdit.TryPaste(pasteString);
        }

        public bool GetCurrentSelectedText(out string text)
        {
            if (ActiveStringEdit != null)
                return ActiveStringEdit.GetCurrentSelection(out text);
            text = string.Empty;
            return false;
        }

        public void OnSelectAll()
        {
            if (ActiveStringEdit != null)
                ActiveStringEdit.SelectAll();
        }

        public void OnEndFocus(EndFocusType endType)
        {
            if (endType == EndFocusType.Commit && ActiveStringEdit != null)
            {
                if ( Text != ActiveStringEdit.CurrentString )
                    Text = ActiveStringEdit.CurrentString;
            }
            ActiveStringEdit = null;
            IsFocused = false;

            if (RenderOnTopWhileEditing) { 
                InheritParentDepth = true;
                RenderDepth = WidgetDepth.Default;
            }

            OnTextEditingStateUpdate?.Invoke(this, true);
        }


        public virtual bool TryHandleTextEntryHotkey(KeyState keyState)
        {
            if (keyState.KeyName == KeyNames.Escape && EnableClearOnEscape)
            {
                if (ActiveStringEdit != null && ActiveStringEdit.HasSelection == false)
                {
                    ActiveStringEdit.SelectAll();
                    ActiveStringEdit.DeleteSelection();
                    return true;
                }
            }
            else if (keyState.KeyName == KeyNames.Enter && KeepFocusOnEnter)
            {
                return true;
            }
            return false;
        }



        public virtual bool ValidateNewString(ref string NewString)
        {
            if (ValidationType == StringValidation.None)
                return true;

            if (ValidationType == StringValidation.Integer)
            {
                if (int.TryParse(NewString, out var IntValue))
                    return true;
            }
            else if (ValidationType == StringValidation.Real)
            {
                if (double.TryParse(NewString, out double RealValue))
                {
                    NewString = ((double)RealValue).ToString("0.0#######");
                    return true;
                }
            }
            return false;
        }

    }



    public class TextEntryFieldView : WidgetView
    {
        public TextEntryField SourceTextEntry;

        public AxisAlignedBox2f LocalBounds;
        public Vector2f DrawOrigin;
        public TextRect TextInfo;
        public TextRect FocusedTextInfo;
        public float CursorOffset;
        public float SelectionStartOffset, SelectionEndOffset;

        public TextEntryFieldView(TextEntryField sourceTextEntryField)
        {
            SourceTextEntry = sourceTextEntryField;
        }

        public override Widget GetWidget() { return SourceTextEntry; }

        public override AxisAlignedBox2f BoundsQuery(ILayoutAnchor? RelativeToAnchor = null)
        {
            return (RelativeToAnchor != null) ?
                AnchorLocation.GetAnchoredBounds(LocalBounds, RelativeToAnchor, GetWidget().AnchorPlacement) : LocalBounds;
        }

        public override void UpdateLayout(SKStyleCache StyleCache)
        {
            LocalBounds = new AxisAlignedBox2f(Vector2f.Zero, SourceTextEntry.Dimensions);

            string ShowText = SourceTextEntry.ActiveText;

            WidgetStyle UseStyle = SourceTextEntry.Style.Select(SourceTextEntry.IsHovered, SourceTextEntry.IsFocused);
            SKPaint TextPaint = StyleCache.GetCachedPaint(UseStyle, SKStyleCache.EPaintType.Text);
            WidgetMargins Margins = SourceTextEntry.Style.BaseMargins;

            TextHeightInfo heightInfo = StyleCache.GetCachedFontHeightInfo(UseStyle);
            float TextWidth = TextPaint.MeasureText(ShowText);

            TextInfo.Bounds = LocalBounds;
            TextInfo.TextOrigin = new Vector2f(
                TextInfo.Bounds.Min.x + Margins.Left,
                TextInfo.Bounds.Max.y - Margins.Bottom - heightInfo.BelowBaseline);

            FocusedTextInfo = TextInfo;
            FocusedTextInfo.Bounds.Max.x = Math.Max(LocalBounds.Max.x, LocalBounds.Min.x + TextWidth + Margins.TotalWidth);

            int CursorLocation = SourceTextEntry.GetActiveEditCursorLocation();
            if ( CursorLocation <= 0 ) {
                CursorOffset = 0;
            } else {
                string substring = SourceTextEntry.ActiveText.Substring(0, CursorLocation);
                CursorOffset = TextPaint.MeasureText(substring);
            }

            SelectionStartOffset = SelectionEndOffset = -1;
            if ( SourceTextEntry.GetSelectionRange(out var StartIndex, out var EndIndex) )
            {
                string startString = SourceTextEntry.ActiveText.Substring(0, StartIndex);
                string endString = SourceTextEntry.ActiveText.Substring(StartIndex, EndIndex - StartIndex);
                SelectionStartOffset = TextPaint.MeasureText( SourceTextEntry.ActiveText.Substring(0, StartIndex) );
                SelectionEndOffset = TextPaint.MeasureText( SourceTextEntry.ActiveText.Substring(0, EndIndex) );
            }
        }

        public override void Draw(SKStyleCache StyleCache, SKCanvas Canvas, ILayoutAnchor Anchor)
        {
            DrawOrigin = Anchor.GetOrigin();

            string ShowText = SourceTextEntry.ActiveText;

            AxisAlignedBox2f PlacedBounds = AnchorLocation.MakeRelativeToAnchor(LocalBounds, SourceTextEntry.AnchorPlacement, DrawOrigin);

            if (SourceTextEntry.IsFocused)
            {
                PlacedBounds.Max.x = PlacedBounds.Min.x + FocusedTextInfo.Bounds.Width;
            }

            WidgetStyle UseStyle = SourceTextEntry.Style.Select(SourceTextEntry.IsHovered, SourceTextEntry.IsFocused);
            SKStyleCache.CachedSKPaintSet StandardPaints = StyleCache.GetCachedPaintSet(UseStyle);
            WidgetMargins Margins = SourceTextEntry.Style.BaseMargins;

            SKPaint TextPaint = StyleCache.GetCachedPaint(UseStyle, SKStyleCache.EPaintType.Text);

            Canvas.DrawRect(Conversion.ToSkia(PlacedBounds), StandardPaints.BackgroundPaint);
            Vector2f TextOrigin = PlacedBounds.Min + TextInfo.TextOrigin;


            if (SourceTextEntry.IsFocused)
            {
                if (SelectionStartOffset >= 0)
                {
                    AxisAlignedBox2f SelectionRect = PlacedBounds;
                    SelectionRect.Contract(1.0f);
                    SelectionRect.Min.x += 1;
                    SelectionRect.Max.x = SelectionRect.Min.x + SelectionEndOffset;
                    SelectionRect.Min.x += SelectionStartOffset;
                    Canvas.DrawRect(Conversion.ToSkia(SelectionRect), StandardPaints.ForegroundPaint);
                }

                Vector2f CursorBottom = new Vector2f(TextOrigin.x + CursorOffset, PlacedBounds.Min.y + 1);
                Vector2f CursorTop = new Vector2f(TextOrigin.x + CursorOffset, PlacedBounds.Max.y - 1);
                // blink the cursor using this kinda hacky method...
                if ( (DateTime.Now.Ticks / 5000000) % 2  == 0 )
                    Canvas.DrawLine(Conversion.ToSkia(CursorBottom), Conversion.ToSkia(CursorTop), StandardPaints.TextPaint);
            }

            if (SourceTextEntry.IsFocused == false)
            {
                Canvas.Save();
                Canvas.ClipRect(Conversion.ToSkia(PlacedBounds));
            }
            Canvas.DrawText(ShowText, Conversion.ToSkia(TextOrigin), TextPaint);
            if (SourceTextEntry.IsFocused == false)
            {
                Canvas.Restore();
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
