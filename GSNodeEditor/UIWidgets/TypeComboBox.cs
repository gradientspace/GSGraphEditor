// Copyright Gradientspace Corp. All Rights Reserved.
using g3;
using Gradientspace.NodeGraph;
using Gradientspace.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace GSNodeEditor
{
    public class TypeComboBox : TextEntryField
    {
        protected FixedPointAnchor PopupMenuAnchor;
        internal PopupMenu TypePopupMenu;
        internal bool bPopupMenuVisible;

        public delegate void TypeModifiedEventHandler(TypeComboBox sender, Type newType);
        public event TypeModifiedEventHandler? OnSelectedTypeChanged;

        public TypeComboBox(WidgetStateStyle? customStyle = null) : base(customStyle)
        {
            this.Text = TypeUtils.TypeToString(selectedType);

            OnTextEditingUpdate += TypeComboBox_OnTextEditingUpdate;
            OnTextEditingStateUpdate += TypeComboBox_OnTextEditingStateUpdate;

            PopupMenuAnchor = new FixedPointAnchor();
            TypePopupMenu = new PopupMenu();
            TypePopupMenu.AnchorPlacement = new AnchorLocation(BoxPoints.BottomRight);
            TypePopupMenu.AnchorTo(PopupMenuAnchor);
            TypePopupMenu.InheritParentDepth = false;
            TypePopupMenu.RenderDepth = new WidgetDepth(WidgetDepthLayers.Overlay1);
            TypePopupMenu.OnMenuItemSelected += TypePopupMenu_OnMenuItemSelected;
            bPopupMenuVisible = false;
        }


        protected Type selectedType = typeof(object);
        public Type SelectedType
        {
            get { return selectedType; }
            set {
                if (selectedType != value) {
                    selectedType = value;
                    this.Text = TypeUtils.TypeToString(selectedType);
                    OnSelectedTypeChanged?.Invoke(this, selectedType);
                }
            }
        }

        public override bool GetTooltipStrings(out string? tooltip, out string[]? extendedTooltip)
        {
            tooltip = selectedType.FullName!;
            extendedTooltip = null;
            return true;
        }

        private void DismissActivePopupMenu()
        {
            if ( bPopupMenuVisible ) {
                RemoveChildWidget(TypePopupMenu);
                TypePopupMenu.ClearItems();
                bPopupMenuVisible = false;

                this.Text = TypeUtils.TypeToString(selectedType);
            }
        }
        private void ShowPopupMenu()
        {
            if (!bPopupMenuVisible)
            {
                // AAHH have to do this first because AddChildWidget() will destroy all Views ?!?  Probably a current hack in WidgetScene that needs to improve...
                AxisAlignedBox2f TextEntryBounds = this.GetActiveView()?.BoundsQuery(this.GetAnchor()) ?? new AxisAlignedBox2f();
                PopupMenuAnchor.AnchorOrigin = TextEntryBounds.TopRight + new Vector2f(0, 2);

                RefreshPopMenuItems();

                AddChildWidget(TypePopupMenu);

                bPopupMenuVisible = true;
            }
        }



        public override bool TryHandleTextEntryHotkey(KeyState keyState)
        {
            if (keyState.KeyName == KeyNames.Enter && TypePopupMenu.ActiveSelectedItem != null) {
                MenuItem activeItem = TypePopupMenu.ActiveSelectedItem;
                this.OnEndFocus(ITextEntryFocusTarget.EndFocusType.Cancel);     // losefocus -> TextEditingStateUpdate -> hidepopup -> clear ActiveSelectedItem...
                activeItem.OnClicked?.Invoke();              // this is always null
                this.TypePopupMenu_OnMenuItemSelected(TypePopupMenu, activeItem);
                return true;
            }
            return base.TryHandleTextEntryHotkey(keyState);
        }

        public override bool OnNextKey(KeyState keyState)
        {
            if (base.OnNextKey(keyState))
                return true;

            // support arrow keys moving highlight up/down
            if (keyState.KeyName == KeyNames.DownArrow || keyState.KeyName == KeyNames.UpArrow) {
                if (keyState.KeyName == KeyNames.DownArrow)
                    TypePopupMenu.HighlightNextItem();
                else
                    TypePopupMenu.HighlightPreviousItem();
            }

            return false;
        }


        private void TypeComboBox_OnTextEditingStateUpdate(TextEntryField sender, bool bEditingEnded)
        {
            if (bEditingEnded)
                DismissActivePopupMenu();
            else
                ShowPopupMenu();
        }

        private void TypePopupMenu_OnMenuItemSelected(PopupMenu popup, MenuItem selectedItem)
        {
            if (selectedItem.CustomData != null)
            {
                Type selectedType = (selectedItem.CustomData as Type)!;
                this.SelectedType = selectedType;
            }
            DismissActivePopupMenu();
        }

        private void TypeComboBox_OnTextEditingUpdate(TextEntryField sender, string newText)
        {
            RefreshPopMenuItems(newText);
        }


        List<GlobalTypeCache.NamedType> MatchesList = new List<GlobalTypeCache.NamedType>();

        private void RefreshPopMenuItems(string? useText = null)
        {
            string SearchText = useText ?? this.Text;
            TypePopupMenu.ClearItems();

            // only an insane person would give a type a name shorter than 3 characters...
            if (SearchText.Length < 3) {
                TypePopupMenu.AddItem(new MenuItem() { Text = "(type to search...)" });
                return;
            }

            // todo can we fold array and <T> searching into GlobalTypeCache.FindType() functions?
            // 

            int MaxResults = 10;
            if ( SearchText.EndsWith("[]") ) 
            {
                SearchText = SearchText.Substring(0, SearchText.Length-2);      // chop off array [] suffix
                GlobalTypeCache.FindTypesByPrefixMatch(SearchText, ref MatchesList, MaxResults);
                foreach (var match in MatchesList) {
                    Type useType = match.type.MakeArrayType();
                    TypePopupMenu.AddItem(new MenuItem() { Text = match.name + "[]", CustomData = useType });
                }
            }
            else if (SearchText.StartsWith("list<", StringComparison.InvariantCultureIgnoreCase)) 
            {
                Type listType = typeof(List<>);
                SearchText = SearchText.Remove(0, 5);
                if (SearchText.EndsWith(">"))
                    SearchText = SearchText.Remove(SearchText.Length-1, 1);
                if (SearchText.Length == 0)
                    return;
                GlobalTypeCache.FindTypesByPrefixMatch(SearchText, ref MatchesList, MaxResults);
                foreach (var match in MatchesList) {
                    try {
                        // sometimes this throws an exception?? what are the constraints on <T> for a List<T> ??
                        Type useType = listType.MakeGenericType(match.type);
                        string useName = TypeUtils.TypeToString(useType);
                        TypePopupMenu.AddItem(new MenuItem() { Text = useName, CustomData = useType });
                    } catch { }
                }
            }
            else 
            {
                GlobalTypeCache.FindTypesByPrefixMatch(SearchText, ref MatchesList, MaxResults);
                foreach (var match in MatchesList)
                    TypePopupMenu.AddItem(new MenuItem() { Text = match.name, CustomData = match.type });
            }

        }



        public override IWidgetView CreateDefaultView()
        {
            return new TypeComboBoxView(this);
        }
    }



    public class TypeComboBoxView : TextEntryFieldView
    {
        TypeComboBox ComboBox;

        public TypeComboBoxView(TypeComboBox sourceTextEntry) : base(sourceTextEntry) { ComboBox = sourceTextEntry; }

        public override void UpdateLayout(SKStyleCache StyleCache)
        {
            base.UpdateLayout(StyleCache);

            if (ComboBox.bPopupMenuVisible)
                ComboBox.TypePopupMenu.GetActiveView()?.UpdateLayout(StyleCache);
        }
    }

}
