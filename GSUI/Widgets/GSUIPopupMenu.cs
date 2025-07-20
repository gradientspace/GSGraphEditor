// Copyright Gradientspace Corp. All Rights Reserved.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using g3;
using SkiaSharp;

namespace Gradientspace.UI
{
    public class MenuItem
    {
        public string Text = "Menu Item";
        public string Tooltip = "";
        public object? CustomData = null;

        public Action? OnClicked = null;
    }


    // todo: make popup able to dismiss itself? would need some way for widgets to self-destroy...
    public class PopupMenu : Widget, ISimpleCaptureTarget
    {
        public WidgetStateStyle Style { get; set; }
        public float ItemSpacing { get; set; }

        public bool EnableClickToDismiss { get; set; } = true;

        //! this event is fired if EnableClickToDismiss==true and user clicks outside of popup menu
        public Action? OnDismissPopupClick { get; set; } = null;

        public delegate void MenuItemSelectedEventHandler(PopupMenu popup, MenuItem selectedItem);
        public event MenuItemSelectedEventHandler? OnMenuItemSelected;

        public delegate void MenuItemHoveredEventHandler(PopupMenu popup, MenuItem? hoveredItem, bool bEnded);
        public event MenuItemHoveredEventHandler? OnMenuItemHovered;

        public PopupMenu(WidgetStateStyle? customStyle = null)
        {
            Style = (customStyle != null) ? customStyle : WidgetStateStyle.DefaultStyle;
            Items = new List<MenuListItem>();
            FilteredItems = new List<MenuListItem>();
            ItemSpacing = 2.0f;

            SetInputBehavior(new BasicWidgetInputBehavior(this, this) { Depth = 9999 });
        }


        public void AddItem(MenuItem item, int sortGroupIndex = 0)
        {
            Items.Add(new MenuListItem() { ItemType = EMenuItemType.StandardEntry, Item = item, SortGroupIndex = sortGroupIndex });
            ClearHighlightedItemIndex();
        }

        public MenuItem AddItem(string text, Action clickedAction, int sortGroupIndex = 0)
        {
            MenuItem item = new MenuItem() { Text = text, OnClicked = clickedAction };
            Items.Add(new MenuListItem() { ItemType = EMenuItemType.StandardEntry, Item = item, SortGroupIndex = sortGroupIndex });
            ClearHighlightedItemIndex();
            return item;
        }

        public void ClearItems()
        {
            Items.Clear();
            ClearHighlightedItemIndex();
        }

        public void SortItems()
        {
            Items.Sort((x,y) =>
            {
                if (x.SortGroupIndex != y.SortGroupIndex)
                    return x.SortGroupIndex.CompareTo(y.SortGroupIndex);
                return x.Item?.Text.CompareTo(y.Item?.Text) ?? 0;
            });
            ClearHighlightedItemIndex();
        }


        public enum EMenuItemType
        {
            StandardEntry, Separator
        }
        public struct MenuListItem
        {
            public EMenuItemType ItemType = EMenuItemType.StandardEntry;
            public MenuItem? Item = null;
            public int SortGroupIndex = 0;
            public MenuListItem() { }
        }

        protected List<MenuListItem> Items;
        protected List<MenuListItem> FilteredItems;
        protected List<MenuListItem> ActiveItems { get { return (HasFilterApplied) ? FilteredItems : Items; } }
        protected bool HasFilterApplied = false;

        public int NumItems { get { return Items.Count; } }

        public IEnumerable<MenuItem> EnumerateItems()
        {
            foreach (MenuListItem item in ActiveItems)
                if (item.ItemType != EMenuItemType.Separator && item.Item != null)
                    yield return item.Item;
        }

        //! this Predicate should be constant
        public void FilterItems(Predicate<MenuItem> filter) {
            FilteredItems.Clear();
            foreach (MenuListItem item in Items)
                if (item.ItemType != EMenuItemType.Separator && item.Item != null && filter(item.Item) == true)
                    FilteredItems.Add(item);
            HasFilterApplied = true;
            ClearHighlightedItemIndex();
        }
        public void ResetFilteredItems() {
            if (HasFilterApplied) {
                FilteredItems.Clear();
                HasFilterApplied = false;
                ClearHighlightedItemIndex();
            }
        }

        //! Force selection of the specified item, ie fires item.OnClicked and OnMenuItemSelected
        //! Use with care as currently there is no safety checking
        public void ExternalSelectItem(MenuItem item)
        {
            item.OnClicked?.Invoke();
            OnMenuItemSelected?.Invoke(this, item);
        }


        // support for a 'highlighted' item. This is a bit weird because the item list may be filtered,
        // so we can't directly index into it and always have to iterate...

        public int HighlightedItemIndex { get; private set; } = -1;

        public MenuItem? HighlightedItem { get { 
                return (HighlightedItemIndex >= 0 && HighlightedItemIndex < ActiveItems.Count) ? ActiveItems[HighlightedItemIndex].Item : null; 
            } 
        }

        public void ClearHighlightedItemIndex()
        {
            HighlightedItemIndex = -1;
        }

        public void SetHighlightedItemIndex(int Index)
        {
            if (Index > 0 && Index < ActiveItems.Count)
                HighlightedItemIndex = Index;
        }

        public void HighlightNextItem(bool bWrap = true)
        {
            HighlightedItemIndex++;
            if (HighlightedItemIndex >= ActiveItems.Count)
                HighlightedItemIndex = 0;
        }
        public void HighlightPreviousItem(bool bWrap = true)
        {
            HighlightedItemIndex--;
            if (HighlightedItemIndex < 0)
                HighlightedItemIndex = ActiveItems.Count-1;
        }

        public override IWidgetView CreateDefaultView()
        {
            return new PopupMenuView(this);
        }


        public virtual bool IsCapturing { get; set; }
        public MenuItem? HoveredItem { get; protected set; } = null;
        public virtual void UpdateCapture(ISimpleCaptureTarget.ECaptureState State, in InputDeviceState deviceState)
        {
            IsCapturing = (State == ISimpleCaptureTarget.ECaptureState.Begin || State == ISimpleCaptureTarget.ECaptureState.Update);
            if (State == ISimpleCaptureTarget.ECaptureState.End)
            {
                WidgetHitResult hitResult = WidgetHitResult.None;
                if (GetActiveView()?.HitQuery(deviceState.CurrentPosition, out hitResult) ?? false)
                {
                    if (hitResult.HitSubItem != null && hitResult.HitSubItem is MenuItem)
                    {
                        MenuItem menuItem = (hitResult.HitSubItem as MenuItem)!;
                        menuItem.OnClicked?.Invoke();
                        OnMenuItemSelected?.Invoke(this, menuItem);
                    }
                    else if ( EnableClickToDismiss )
                    {
                        OnDismissPopupClick?.Invoke();
                    }
                }
            }
        }
        public virtual void UpdateHover(ISimpleCaptureTarget.EHoverState State, in InputDeviceState deviceState, out bool bContinueHover) 
        { 
            if (State == ISimpleCaptureTarget.EHoverState.Begin || State == ISimpleCaptureTarget.EHoverState.Update)
            {
                WidgetHitResult hitResult = WidgetHitResult.None;
                if (GetActiveView()?.HitQuery(deviceState.CurrentPosition, out hitResult) ?? false)
                {
                    MenuItem? NewHoveredItem = (hitResult.HitSubItem as MenuItem);

                    if (NewHoveredItem != HoveredItem)
                    {
                        if (HoveredItem != null)
                        {
                            OnMenuItemHovered?.Invoke(this, HoveredItem, true);
                            HoveredItem = null;
                        }

                        HoveredItem = NewHoveredItem;

                        if (HoveredItem != null) {
                            OnMenuItemHovered?.Invoke(this, HoveredItem, false);
                        }
                    }
                }
            }
            else if (State == ISimpleCaptureTarget.EHoverState.End) {
                OnMenuItemHovered?.Invoke(this, HoveredItem, true);
                HoveredItem = null;
            }

            //bContinueHover = (HoveredItem != null);       // would only want this on Update...
            bContinueHover = true;
        }
    }





    public class PopupMenuView : WidgetView
    {
        public PopupMenu SourceMenu;

        public AxisAlignedBox2f LocalBounds;
        public Vector2f DrawOrigin;

        struct CachedItemInfo
        {
            public MenuItem Item;
            public float TextWidth;
            public AxisAlignedBox2f ItemBounds;
        }
        CachedItemInfo[] CachedItems;
        int NumVisibleItems;

        public PopupMenuView(PopupMenu sourceMenu)
        {
            SourceMenu = sourceMenu;
            CachedItems = new CachedItemInfo[sourceMenu.NumItems];
        }

        public override Widget GetWidget() { return SourceMenu; }

        public override AxisAlignedBox2f BoundsQuery(ILayoutAnchor? RelativeToAnchor = null)
        {
            return (RelativeToAnchor != null) ?
                AnchorLocation.GetAnchoredBounds(LocalBounds, RelativeToAnchor, GetWidget().AnchorPlacement) : LocalBounds;
        }

        public override void UpdateLayout(SKStyleCache StyleCache)
        {
            LocalBounds = AxisAlignedBox2f.Empty;

            if ( CachedItems.Length < SourceMenu.NumItems )
                CachedItems = new CachedItemInfo[SourceMenu.NumItems];

            // derp, ignores separators...
            NumVisibleItems = 0;
            foreach (MenuItem item in SourceMenu.EnumerateItems())
            {
                CachedItems[NumVisibleItems++].Item = item;
            }
            if (NumVisibleItems == 0) return;

            SKPaint TextPaint = StyleCache.GetCachedPaint(SourceMenu.Style.StandardStyle, SKStyleCache.EPaintType.Text);

            TextHeightInfo heightInfo = StyleCache.GetCachedFontHeightInfo(SourceMenu.Style.StandardStyle);
            float TextHeight = heightInfo.MaxTotalHeight;
            float TotalYHeight = TextHeight + SourceMenu.Style.BaseMargins.TotalHeight;

            float MaxWidth = 0;
            for ( int i = 0; i < NumVisibleItems; ++i)
            {
                CachedItems[i].TextWidth = TextPaint.MeasureText(CachedItems[i].Item.Text);
                MaxWidth = MathF.Max(CachedItems[i].TextWidth, MaxWidth);
            }
            MaxWidth += SourceMenu.Style.BaseMargins.TotalWidth;

            float CurY = 0;
            for (int i = 0; i < NumVisibleItems; ++i)
            {
                CachedItems[i].ItemBounds = new AxisAlignedBox2f(0, CurY, MaxWidth, CurY + TotalYHeight);
                CurY += TotalYHeight;

                LocalBounds.Contain(CachedItems[i].ItemBounds);

                // spacing between items
                CurY += SourceMenu.ItemSpacing;
            }

        }


        public override void Draw(SKStyleCache StyleCache, SKCanvas Canvas, ILayoutAnchor Anchor)
        {
            // todo this should be cached unless menu items change or style changes...
            //UpdateLayout(StyleCache);

            //DrawOrigin = Anchor.GetOrigin();
            AxisAlignedBox2f PlacedBounds = AnchorLocation.MakeRelativeToAnchor(LocalBounds, GetWidget().AnchorPlacement, Anchor.GetOrigin());
            DrawOrigin = PlacedBounds.TopLeft;

            if (NumVisibleItems == 0) return;

            SKStyleCache.CachedSKPaintSet StandardPaints = StyleCache.GetCachedPaintSet(SourceMenu.Style.StandardStyle);
            SKPaint HoveredPaint = StyleCache.GetCachedPaint(SourceMenu.Style.HoverStyle, SKStyleCache.EPaintType.Background);
            WidgetMargins Margins = SourceMenu.Style.BaseMargins;
            TextHeightInfo heightInfo = StyleCache.GetCachedFontHeightInfo(SourceMenu.Style.StandardStyle);
            float TextHeight = heightInfo.MaxTotalHeight;

            SKMatrix SaveMatrix = Canvas.TotalMatrix;
            Canvas.Translate(Conversion.ToSkia(DrawOrigin));

            MenuItem? HoveredItem = SourceMenu.HoveredItem;
            MenuItem? HighlightedItem = SourceMenu.HighlightedItem;
            for ( int i = 0; i < NumVisibleItems; ++i )
            {
                bool bHovered = ( CachedItems[i].Item == HoveredItem || CachedItems[i].Item == HighlightedItem);

                Canvas.DrawRect( Conversion.ToSkia(CachedItems[i].ItemBounds), 
                    (bHovered) ? HoveredPaint : StandardPaints.BackgroundPaint );

                Vector2f TextOrigin = new Vector2f(
                    CachedItems[i].ItemBounds.Min.x + Margins.Left,
                    CachedItems[i].ItemBounds.Max.y - Margins.Bottom - heightInfo.BelowBaseline);
                Canvas.DrawText( CachedItems[i].Item.Text, Conversion.ToSkia(TextOrigin), StandardPaints.TextPaint );
            }

            Canvas.SetMatrix(SaveMatrix);
        }



        public override bool HitTest(Vector2f QueryPoint)
        {
            if (SourceMenu.EnableClickToDismiss)
                return true;

            Vector2f LocalPoint = QueryPoint - DrawOrigin;
            return LocalBounds.Contains(LocalPoint);
        }


        public override bool HitQuery(Vector2f QueryPoint, out WidgetHitResult Result)
        {
            Result = new WidgetHitResult();

            Vector2f LocalPoint = QueryPoint - DrawOrigin;
            if (SourceMenu.EnableClickToDismiss == false)
            {
                if (CachedItems.Length == 0 || LocalBounds.Contains(LocalPoint) == false)
                    return false;
            }

            Result.HitWidget = this;

            int NumItems = CachedItems.Length;
            for (int i = 0; i < NumItems; ++i)
            {
                if (CachedItems[i].ItemBounds.Contains(LocalPoint))
                {
                    Result.HitSubItem = CachedItems[i].Item;
                    return true;
                }
            }

            // hit widget but did not hit an item
            // (maybe this should be configurable?)
            return true;
        }
    }

}
