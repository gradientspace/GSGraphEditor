// Copyright Gradientspace Corp. All Rights Reserved.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using g3;
using SkiaSharp;

namespace Gradientspace.UI
{
    public class DropDownPicker : Widget, ISimpleCaptureTarget
    {
        public WidgetStateStyle Style { get; set; }

        public delegate void SelectionModifiedEventHandler(DropDownPicker dropdown, int oldIndex, int newIndex);
        public event SelectionModifiedEventHandler? OnSelectionModified;

        Vector2f _dimensions = new Vector2f(60, 15);

        struct Item
        {
            public string Label;
            public int ExternalID;
            public object? ExternalObject;
        }
        List<Item> Items = new List<Item>();

        uint CurrentIndex = 0;

        internal FixedPointAnchor PopupMenuAnchor;
        internal PopupMenu? ActivePopupMenu = null;


        public DropDownPicker(WidgetStateStyle? customStyle = null)
        {
            Style = (customStyle != null) ? customStyle : DefaultWidgetStyles.DefaultTextFieldStyle;

            PopupMenuAnchor = new FixedPointAnchor();

            SetInputBehavior(new BasicWidgetInputBehavior(this, this)
            {
                EnableHover = true,
                Depth = 0
            });
        }

        public void AddItem(string label, int externalID = 0, object? externalObject = null)
        {
            Item newItem = new Item { Label = label, ExternalID = externalID, ExternalObject = externalObject };
            Items.Add(newItem);
        }

        public void AddItems(IEnumerable<string> Items)
        {
            int Counter = 0;
            foreach (string item in Items)
                AddItem(item, Counter++, null);
        }

        public bool HasValidSelection { get { return CurrentIndex < Items.Count; } }

        public int SelectedIndex  { get { return (CurrentIndex < Items.Count) ? (int)CurrentIndex : -1; } }

        public string CurrentText
        {
            get { return (CurrentIndex < Items.Count) ? Items[(int)CurrentIndex].Label : string.Empty; }
        }

        public void SetSelectedIndex(int NewIndex)
        {
            uint UseIndex = (uint)Math.Clamp(NewIndex, 0, Items.Count - 1);
            if (UseIndex != CurrentIndex)
            {
                int PrevIndex = (int)CurrentIndex;
                CurrentIndex = UseIndex;
                OnSelectionModified?.Invoke(this, PrevIndex, (int)CurrentIndex);
            }
        }

        public bool GetItemAtIndex(int Index, out string Label, out int ExternalID, out object? ExternalObject)
        {
            if (Index < 0 || Index >= (int)Items.Count) { Label = string.Empty; ExternalID = -1;  ExternalObject = null; return false; }

            Label = Items[Index].Label;
            ExternalID = Items[Index].ExternalID;
            ExternalObject = Items[Index].ExternalObject;
            return true;
        }



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
            return new DropDownPickerView(this);
        }

        public virtual bool IsFocused { get; set; } = false;
        public virtual bool IsHovered { get; set; }
        public virtual bool IsCapturing { get; set; }
        public virtual void UpdateCapture(ISimpleCaptureTarget.ECaptureState State, in InputDeviceState deviceState)
        {
            bool bWasCapturing = IsCapturing;
            IsCapturing = (State == ISimpleCaptureTarget.ECaptureState.Begin || State == ISimpleCaptureTarget.ECaptureState.Update);
            if (IsCapturing == false && bWasCapturing == true)
            {
                bool bPointerUpHit = GetActiveView()?.HitTest(deviceState.CurrentPosition) ?? false;
                if (bPointerUpHit)
                {
                    AxisAlignedBox2f FieldBounds = this.GetActiveView()?.BoundsQuery(this.GetAnchor()) ?? new AxisAlignedBox2f(deviceState.CurrentPosition);
                    PopupMenuAnchor.AnchorOrigin = FieldBounds.TopRight + new Vector2f(0,2);
                    ActivePopupMenu = new PopupMenu();
                    for ( int i = 0; i < Items.Count; ++i )
                    {
                        int j = i;      // need to force copy of captured i :(
                        ActivePopupMenu.AddItem(Items[i].Label, () => { SetSelectedIndex(j); DismissPopupMenu(); });
                    }
                    // show popup menu
                    ActivePopupMenu.AnchorPlacement = new AnchorLocation(BoxPoints.BottomRight);
                    ActivePopupMenu.AnchorTo(PopupMenuAnchor);
                    ActivePopupMenu.OnDismissPopupClick = () => { DismissPopupMenu(); };
                    ActivePopupMenu.InheritParentDepth = false;
                    ActivePopupMenu.RenderDepth = new WidgetDepth(WidgetDepthLayers.Overlay1);
                    AddChildWidget(ActivePopupMenu);

                }
            }
        }
        public virtual void UpdateHover(ISimpleCaptureTarget.EHoverState State, in InputDeviceState deviceState, out bool bContinueHover)
        {
            IsHovered = (State == ISimpleCaptureTarget.EHoverState.Begin || State == ISimpleCaptureTarget.EHoverState.Update);
            bContinueHover = true;
        }

        protected virtual void DismissPopupMenu()
        {
            if (ActivePopupMenu == null) return;
            this.RemoveChildWidget(ActivePopupMenu); 
            ActivePopupMenu = null;
        }

    }



    public class DropDownPickerView : WidgetView
    {
        public DropDownPicker SourceDropDown;

        public AxisAlignedBox2f LocalBounds;
        public Vector2f DrawOrigin;
        public TextRect TextInfo;

        public DropDownPickerView(DropDownPicker sourceDropDownPicker)
        {
            SourceDropDown = sourceDropDownPicker;
        }

        public override Widget GetWidget() { return SourceDropDown; }

        public override AxisAlignedBox2f BoundsQuery(ILayoutAnchor? RelativeToAnchor = null)
        {
            return (RelativeToAnchor != null) ?
                AnchorLocation.GetAnchoredBounds(LocalBounds, RelativeToAnchor, GetWidget().AnchorPlacement) : LocalBounds;
        }

        public override void UpdateLayout(SKStyleCache StyleCache)
        {
            LocalBounds = new AxisAlignedBox2f(Vector2f.Zero, SourceDropDown.Dimensions);

            string ShowText = SourceDropDown.CurrentText;

            WidgetStyle UseStyle = SourceDropDown.Style.Select(SourceDropDown.IsHovered, SourceDropDown.IsFocused);
            SKPaint TextPaint = StyleCache.GetCachedPaint(UseStyle, SKStyleCache.EPaintType.Text);
            WidgetMargins Margins = SourceDropDown.Style.BaseMargins;

            TextHeightInfo heightInfo = StyleCache.GetCachedFontHeightInfo(UseStyle);
            float TextWidth = TextPaint.MeasureText(ShowText);

            TextInfo.Bounds = LocalBounds;
            TextInfo.TextOrigin = new Vector2f(
                TextInfo.Bounds.Min.x + Margins.Left,
                TextInfo.Bounds.Max.y - Margins.Bottom - heightInfo.BelowBaseline);

            SourceDropDown.ActivePopupMenu?.GetActiveView()?.UpdateLayout(StyleCache);
        }

        public override void Draw(SKStyleCache StyleCache, SKCanvas Canvas, ILayoutAnchor Anchor)
        {
            DrawOrigin = Anchor.GetOrigin();
            AxisAlignedBox2f PlacedBounds = AnchorLocation.MakeRelativeToAnchor(LocalBounds, SourceDropDown.AnchorPlacement, DrawOrigin);

            string ShowText = SourceDropDown.CurrentText;

            WidgetStyle UseStyle = SourceDropDown.Style.Select(SourceDropDown.IsHovered, SourceDropDown.IsFocused);
            SKStyleCache.CachedSKPaintSet StandardPaints = StyleCache.GetCachedPaintSet(UseStyle);
            WidgetMargins Margins = SourceDropDown.Style.BaseMargins;

            SKPaint TextPaint = StyleCache.GetCachedPaint(UseStyle, SKStyleCache.EPaintType.Text);

            Canvas.DrawRect(Conversion.ToSkia(PlacedBounds), StandardPaints.BackgroundPaint);
            Vector2f TextOrigin = PlacedBounds.Min + TextInfo.TextOrigin;

            if (SourceDropDown.IsFocused == false)
            {
                Canvas.Save();
                Canvas.ClipRect(Conversion.ToSkia(PlacedBounds));
            }
            Canvas.DrawText(ShowText, Conversion.ToSkia(TextOrigin), TextPaint);
            if (SourceDropDown.IsFocused == false)
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
            if (HitTest(QueryPoint))
            {
                Result = new WidgetHitResult(this, 25);
                return true;
            }
            return false;
        }


    }
}
