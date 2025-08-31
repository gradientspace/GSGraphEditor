// Copyright Gradientspace Corp. All Rights Reserved.
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
        public Action? OnEnterKeyPressed = null;

        public bool RenderOnTopWhileEditing = true;
        public bool IsEditable = true;

        Vector2f _dimensions = new Vector2f(60, 15);
        string _text = "";

        public enum StringValidation
        {
            None,
            Integer,
            Real
        }
        public StringValidation ValidationType { get; set; } = StringValidation.None;

        TextEntryFieldChange? activeChange = null;

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
        public float Height {
            get { return _dimensions.y; }
            set { _dimensions.y = value; }
        }

        public override IWidgetView CreateDefaultView()
        {
            return new TextEntryFieldView(this);
        }


		public override bool GetTooltipStrings(out string? tooltip, out string[]? extendedTooltip)
		{
			tooltip = Text;
			extendedTooltip = null;
			return true;
		}

        public virtual void SilentUpdateText(string NewText)
        {
            _text = NewText;
        }


		//! this function can be called externally, to assign focus to the text entry field
		public virtual void BeginStringEdit()
        {
            if (IsEditable == false)
                return;

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
        protected Vector2f StartCaptureLocation = Vector2f.Zero;
        protected bool bShiftDown = false;
        public virtual void UpdateCapture(ISimpleCaptureTarget.ECaptureState State, in InputDeviceState deviceState) {
            bool bWasCapturing = IsCapturing;
            IsCapturing = (State == ISimpleCaptureTarget.ECaptureState.Begin || State == ISimpleCaptureTarget.ECaptureState.Update);
            if (State == ISimpleCaptureTarget.ECaptureState.Begin) {
                StartCaptureLocation = deviceState.CurrentPosition;
                bShiftDown = deviceState.ShiftButton.bDown;
            }

			bool bPointerHit = GetActiveView()?.HitTest(deviceState.CurrentPosition) ?? false;

			if (IsFocused == false)
            {
                if (IsCapturing == false && bWasCapturing == true && bPointerHit)       // released w/ pointer hit
					BeginStringEdit();
            } 
            else
            {
				if (IsCapturing == false && bWasCapturing == true)      // isnt this State == End??
                {
                    if ( bPointerHit )
					    SetSelectionRangeOrCursorLocation(StartCaptureLocation, deviceState.CurrentPosition);
                    end_change();
				} 
                else if (State == ISimpleCaptureTarget.ECaptureState.Update)
                {
					if (bShiftDown) {
						SetSelectionRangeOrCursorLocation(deviceState.CurrentPosition, deviceState.CurrentPosition);
						StartCaptureLocation = deviceState.CurrentPosition;
					} else
						SetSelectionRangeOrCursorLocation(StartCaptureLocation, deviceState.CurrentPosition);
				} 
                else if (State == ISimpleCaptureTarget.ECaptureState.Begin )
                {
					begin_change();
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
        internal void SetSelectionRangeOrCursorLocation(Vector2f startClickLocation, Vector2f endClickLocation)
        {
            if (GetActiveView() is TextEntryFieldView view)
            {
				int startCharIndex = view?.GetCharacterIndexFromPosition(startClickLocation) ?? 0;
                int endCharIndex = startCharIndex;
                if ( Math.Abs(endClickLocation.x - startClickLocation.x) > 1)
                    endCharIndex = view?.GetCharacterIndexFromPosition(endClickLocation) ?? 0;
                if ( startCharIndex == endCharIndex )
				    ActiveStringEdit?.SetCursorLocation(startCharIndex);
                else
					ActiveStringEdit?.SetSelectionRange(startCharIndex, endCharIndex);
			}
		}

        // ITextEntryFocusTarget API

        public virtual bool OnNextKey(KeyState keyState)
        {
            if ( keyState.KeyName == KeyNames.Tab ) {
                SystemKeyboardRouter.Instance.ClearTextEntryFocusTarget(EndFocusType.Commit);
                return true;
            }

            if (ActiveStringEdit != null) 
            {
                // TODO: could we accumulate multiple text-edit key changes? annoying to have to undo every character...

                begin_change();
                bool bResult = ActiveStringEdit.OnNextKey(keyState);
                end_change(!bResult);
                return bResult;
            }
            
            return false;
        }

        public void OnPasteText(string pasteString)
        {
			begin_change();
			if (ActiveStringEdit != null)
                ActiveStringEdit.TryPaste(pasteString);
            end_change();
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
			begin_change();
			if (ActiveStringEdit != null)
                ActiveStringEdit.SelectAll();
            end_change();
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
                    begin_change();
                    ActiveStringEdit.SelectAll();
                    ActiveStringEdit.DeleteSelection();
                    end_change();
                    return true;
                }
            }
            else if (keyState.KeyName == KeyNames.Enter && KeepFocusOnEnter)
            {
                OnEnterKeyPressed?.Invoke();
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



        // undo/redo support

        protected void begin_change()
        {
            Debug.Assert(activeChange == null);
            Debug.Assert(ActiveStringEdit != null);
            activeChange = new TextEntryFieldChange(this);
            activeChange.FromState = ActiveStringEdit.GetCurrentState();

            // do we need to BeginChanges here? seems like it can wait until end_change()...
            HistorySystem.ActiveHistory?.BeginChanges("Edit Text");
        }

        protected void end_change(bool bCancel = false)
        {
            Debug.Assert(activeChange != null);
            Debug.Assert(ActiveStringEdit != null);
			activeChange.ToState = ActiveStringEdit.GetCurrentState();
            // todo check for identity...
            if (activeChange.IsNullChange == false && bCancel == false )
                HistorySystem.ActiveHistory?.AppendChange(activeChange);
            activeChange = null;
			HistorySystem.ActiveHistory?.EndChanges();
		}

		internal void SetCurrentState(ref readonly StringEditor.StringEditorState state)
		{
            ActiveStringEdit?.SetCurrentState(in state);
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
        public AxisAlignedBox2f LastVisibleWorldBounds;

        public SKPaint? LastTextPaint = null;     // hmmm not sure this is safe...

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

            WidgetStyle UseStyle = SourceTextEntry.Style.Select(SourceTextEntry.IsHovered, SourceTextEntry.IsFocused, !SourceTextEntry.IsEditable);
            SKStyleCache.CachedSKPaintSet StandardPaints = StyleCache.GetCachedPaintSet(UseStyle);
            WidgetMargins Margins = SourceTextEntry.Style.BaseMargins;

            SKPaint TextPaint = StyleCache.GetCachedPaint(UseStyle, SKStyleCache.EPaintType.Text);
            LastTextPaint = TextPaint;

            LastVisibleWorldBounds = PlacedBounds;
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
                // todo this should maybe be something based on an accumulation, so that we can
                // force cursor to visible state immediately after clicks/etc
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
            //AxisAlignedBox2f WorldBounds =
            //    AnchorLocation.GetAnchoredBounds(LocalBounds, DrawOrigin, GetWidget().AnchorPlacement);
            //return WorldBounds.Contains(QueryPoint);
            return LastVisibleWorldBounds.Contains(QueryPoint);
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


        public int GetCharacterIndexFromPosition(Vector2f QueryPoint)
        {
            if (LastTextPaint == null)
                return 0;

            AxisAlignedBox2f WorldBounds = 
                AnchorLocation.GetAnchoredBounds(LocalBounds, DrawOrigin, GetWidget().AnchorPlacement);
            WorldBounds.Min.x += TextInfo.TextOrigin.x;
			float LocalClickX = QueryPoint.x - WorldBounds.Min.x;

            // dumb linear search. Conceivably would be better to cache this if the widget is focused?
            // or do a binary search at least? not performance-critical though...
            float cur_offset = 0;
			string ShowText = SourceTextEntry.ActiveText;
            for (int k = 1; k < ShowText.Length; ++k)
            {
				string substring = SourceTextEntry.ActiveText.Substring(0, k);
				float next_offset = LastTextPaint.MeasureText(substring);
                if (LocalClickX < (cur_offset + next_offset)*0.5 )
                    return k-1;
                cur_offset = next_offset;
			}
            return ShowText.Length;
		}


    }



    public class TextEntryFieldChange : BaseHistoryChange
    {
        public TextEntryField? TextField = null;
		public StringEditor.StringEditorState FromState;
		public StringEditor.StringEditorState ToState;

		public TextEntryFieldChange(TextEntryField textField) : base("Edit Text", null) {
            TextField = textField;
        }

		public override void Apply()
        {
            TextField?.SetCurrentState(in ToState);
        }

		public override void Revert()
        {
			TextField?.SetCurrentState(in FromState);
		}

        public bool IsNullChange {
            get {
                return (FromState.CursorLocation == ToState.CursorLocation &&
                    FromState.SelectionStartLocation == ToState.SelectionEndLocation &&
                    FromState.SelectionEndLocation == ToState.SelectionStartLocation &&
                    (String.Compare(FromState.CurrentString, ToState.CurrentString) == 0));
            }
        }
	}


}
