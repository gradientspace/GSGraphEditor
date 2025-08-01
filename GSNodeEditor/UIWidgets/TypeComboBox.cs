// Copyright Gradientspace Corp. All Rights Reserved.
using g3;
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
            this.Text = selectedType.Name;

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
                    this.Text = selectedType.Name;
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

                this.Text = selectedType.Name;
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


        private void RefreshPopMenuItems(string? useText = null)
        {
            string UsingText = useText ?? this.Text;
            TypePopupMenu.ClearItems();

            bool bAllowContains = false;
            int MaxResults = 10;
            if (UsingText.Length >= 3) {
                List<Type> matches = UpdateActiveTypeMatches(UsingText, bAllowContains, MaxResults);

                foreach (Type t in matches)
                    TypePopupMenu.AddItem(new MenuItem() { Text = t.Name, CustomData = t });
            } else {
                TypePopupMenu.AddItem(new MenuItem() { Text = "(type to search...)" });
            }
        }


        // TODO: this should probably be run async, and maybe be smarter than just max-matches...
        private List<Type> UpdateActiveTypeMatches(string currentText, bool bAllowContains, int MaxResults = 10)
        {
            List<Type> types = new List<Type>();
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] allTypes = assembly.GetTypes();
                foreach (Type type in allTypes)
                {
                    if ( type.IsPublic == false || type.IsAbstract ) continue;

                    if ( type.Name.StartsWith(currentText, StringComparison.InvariantCultureIgnoreCase) ) {
                        types.Add(type);
                        continue;
                    }
                    if ( bAllowContains && type.Name.Contains(currentText) ) {
                        types.Add(type);
                        continue;
                    }
                }

                if (types.Count > MaxResults)
                    break;
            }
            return types;
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
