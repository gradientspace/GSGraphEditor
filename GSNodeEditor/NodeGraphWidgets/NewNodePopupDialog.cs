// Copyright Gradientspace Corp. All Rights Reserved.
using g3;
using Gradientspace.NodeGraph;
using Gradientspace.NodeGraph.CodeNodes;
using Gradientspace.UI;
using SkiaSharp;
using System;
using System.Diagnostics;

namespace GSNodeEditor
{
    public class NewNodePopupDialog : Widget, ISimpleCaptureTarget, IHotkeyTarget
    {
        public WidgetStateStyle Style { get; set; }

        Vector2f _dimensions = new Vector2f(120, 15);

        protected FixedPointAnchor dialogAnchor;
        public Vector2f Position
        {
            get { return dialogAnchor.AnchorOrigin; }
            set { dialogAnchor.AnchorOrigin = value; }
        }

        public bool EnableClickToDismiss { get; set; } = true;

        //! this event is fired if EnableClickToDismiss==true and user clicks outside of popup menu
        public Action? OnDismissDialogClick { get; set; } = null;


        public delegate void NewNodeTypeSelectedEventHandler(NewNodePopupDialog dialog, NodeType nodeType);
        public event NewNodeTypeSelectedEventHandler? OnNewNodeTypeSelected;

        public delegate void CreateFunctionCallEventHandler(NewNodePopupDialog dialog, FunctionDefinitionNode functionNode);
        public event CreateFunctionCallEventHandler? OnCreateFunctionCallSelected;

        public enum NewVariableEventType
        {
            NewGlobal = 0, 
            CreateAlias = 1,
            CreateSplitter = 5
        }
        const string CreateSplitterLabel = "Reroute/Splitter";
        const string CreateAliasLabel = "Create Alias";

        public delegate void NewVariableSelectedEventHandler(NewNodePopupDialog dialog, NodeAndPin? nodeAndPin, NewVariableEventType type);
		public event NewVariableSelectedEventHandler? OnNewVariableSelected;

		public delegate void GetSetVariableSelectedEventHandler(NewNodePopupDialog dialog, VariablesTracker.VariableInfo varInfo, bool bSet);
		public event GetSetVariableSelectedEventHandler? OnGetSetVariableSelected;

        public delegate void GetAliasSelectedEventHandler(NewNodePopupDialog dialog, VariablesTracker.VariableInfo varInfo);
        public event GetAliasSelectedEventHandler? OnGetAliasSelected;

        internal WidgetRelativeBoxAnchor SearchBoxAnchor;
        internal TextEntryField SearchBox;

        internal WidgetRelativeBoxAnchor NodesMenuAnchor;
        // these two should be an array/stack, like ActiveSubCategoryMenuStack
        internal WidgetRelativeBoxAnchor NodesCategoryMenuAnchor;
        internal WidgetRelativeBoxAnchor NodesCategorySubMenuAnchor;

        // list of all nodes, filtered by search box
        internal PopupMenu LinearAllNodesMenu;


        internal class NodesCategory
        {
            public string Label;
            public PopupMenu CategoryMenu;

            public NodesCategory? ParentCategory = null;
            public List<NodesCategory>? ChildCategories = null;

            public NodesCategory(string label, WidgetStateStyle Style)
            {
                Label = label;
                CategoryMenu = new PopupMenu(Style);
                CategoryMenu.EnableClickToDismiss = false;
                CategoryMenu.AnchorPlacement = new AnchorLocation(BoxPoints.BottomLeft);
            }
        }

        internal PopupMenu TopLevelNodeSetsMenu;
        bool bNodeSetsMenuActive = false;

        internal NodesCategory? VariablesCategory = null;
        internal NodesCategory? FunctionsCategory = null;
        internal List<NodesCategory> NodesCategories = new List<NodesCategory>();

        // maybe better if this is NodesCategory[] ?
        internal PopupMenu?[] ActiveSubCategoryMenuStack = [null, null];

        //public static WidgetStyle DefaultStyle = new WidgetStyle();
        //public static WidgetStyle DefaultHoverStyle = new WidgetStyle() { BackgroundColor = Colorf.Orange };
        //public static WidgetStyle DefaultPressedStyle = new WidgetStyle() { BackgroundColor = Colorf.LightSteelBlue };

        public static readonly WidgetStyle NewNodeDialogStandardStyle = new WidgetStyle() { BackgroundColor = Colorf.LightSteelBlue, ForegroundColor = Colorf.Black, TextSize = 14, Margins = new WidgetMargins(6,6,3,6) };
        public static readonly WidgetStyle NewNodeDialogHoverStyle = new WidgetStyle() { BackgroundColor = Colorf.Orange, ForegroundColor = Colorf.Black, TextColor = Colorf.Black, TextSize = 14 };
        public static readonly WidgetStyle NewNodeDialogPressedStyle = new WidgetStyle() { BackgroundColor = new Colorf(196, 206, 242), ForegroundColor = Colorf.Black, TextColor = Colorf.Black, TextSize = 14 };
        public static readonly WidgetStateStyle DefaultNewNodeDialogStyle = new WidgetStateStyle(
            NewNodeDialogStandardStyle, NewNodeDialogHoverStyle, NewNodeDialogPressedStyle);

        public static readonly WidgetStyle SearchBoxStandardStyle = new WidgetStyle() { BackgroundColor = Colorf.LightGrey, ForegroundColor = Colorf.LightGrey, TextSize = 12, Margins = new WidgetMargins(4,2) };
        public static readonly WidgetStyle SearchBoxHoverStyle = new WidgetStyle() { BackgroundColor = Colorf.LightSlateGrey, ForegroundColor = Colorf.LightGrey, TextColor = Colorf.Black, TextSize = 12 };
        public static readonly WidgetStyle SearchBoxPressedStyle = new WidgetStyle() { BackgroundColor = Colorf.VideoWhite, ForegroundColor = Colorf.LightGrey, TextColor = Colorf.Black, TextSize = 12 };
        public static readonly WidgetStateStyle SearchBoxStyle = new WidgetStateStyle(
            SearchBoxStandardStyle, SearchBoxHoverStyle, SearchBoxPressedStyle);


        public NewNodePopupDialog(WidgetStateStyle? customStyle = null)
        {
            Style = (customStyle != null) ? customStyle : DefaultNewNodeDialogStyle;

            dialogAnchor = new FixedPointAnchor();
            AnchorTo(dialogAnchor);

            SearchBoxAnchor = new WidgetRelativeBoxAnchor(this);
            SearchBox = new TextEntryField(SearchBoxStyle);
            SearchBox.Dimensions = new Vector2f(this.Width, 18);
            SearchBox.AnchorTo(SearchBoxAnchor);
            SearchBox.EnableClearOnEscape = true;
            SearchBox.KeepFocusOnEnter = true;
            AddChildWidget(SearchBox);
            //SearchBox.Text = "Search";
            SearchBox.OnTextEditingUpdate += SearchBox_OnTextEditingUpdate;
            //SearchBox.OnTextModified += SearchBox_OnTextModified;
            SearchBox.OnEnterKeyPressed = SearchBox_OnEnterKey;

            NodesMenuAnchor = new WidgetRelativeBoxAnchor(this);
            NodesCategoryMenuAnchor = new WidgetRelativeBoxAnchor(this);
            NodesCategoryMenuAnchor.BoxPoint = BoxPoints.TopRight;
            NodesCategorySubMenuAnchor = new WidgetRelativeBoxAnchor(this);
            NodesCategorySubMenuAnchor.BoxPoint = BoxPoints.TopRight;


            LinearAllNodesMenu = new PopupMenu(Style);
            LinearAllNodesMenu.EnableClickToDismiss = false;
            LinearAllNodesMenu.AnchorPlacement = new AnchorLocation(BoxPoints.BottomLeft);
            LinearAllNodesMenu.AnchorTo(NodesMenuAnchor);
            LinearAllNodesMenu.AddItem(new MenuItem() { Text = "Item A" });
            LinearAllNodesMenu.AddItem(new MenuItem() { Text = "Item B" });

            LinearAllNodesMenu.OnMenuItemSelected += NodesMenu_OnMenuItemSelected;

            TopLevelNodeSetsMenu = new PopupMenu(Style);
            TopLevelNodeSetsMenu.EnableClickToDismiss = false;
            TopLevelNodeSetsMenu.AnchorPlacement = new AnchorLocation(BoxPoints.BottomLeft);
            TopLevelNodeSetsMenu.AnchorTo(NodesMenuAnchor);
            TopLevelNodeSetsMenu.OnMenuItemHovered += NodesCategoryMenu_OnMenuItemHovered;

            //AddChildWidget(NodesMenu);
            //AddChildWidget(NodesCategoryMenu);
            UpdateActiveMenu();

            SetInputBehavior(new BasicWidgetInputBehavior(this, this) {
                EnableHover = false,
                Depth = -10
            });

            RenderDepth = new WidgetDepth(WidgetDepthLayers.Overlay1);
        }

        public void GiveFocusToSearchBox()
        {
            SearchBox.BeginStringEdit();
        }


        public PopupMenu ActiveMenu
        {
            get { return (bNodeSetsMenuActive) ? TopLevelNodeSetsMenu : LinearAllNodesMenu; }
        }
        void UpdateActiveMenu()
        {
            bool bShouldShowCategoryMenu = (FilterString == string.Empty);
            if ( bShouldShowCategoryMenu != bNodeSetsMenuActive )
            {
                if (bNodeSetsMenuActive == false)
                {
                    RemoveChildWidget(LinearAllNodesMenu);
                    AddChildWidget(TopLevelNodeSetsMenu);
                    bNodeSetsMenuActive = true;
                }
                else
                {
                    RemoveChildWidget(TopLevelNodeSetsMenu);
                    AddChildWidget(LinearAllNodesMenu);
                    bNodeSetsMenuActive = false;
                }
            }
            UpdateVisibleSubCategory(null);
        }
        void UpdateVisibleSubCategory(NodesCategory? category)
        {
            // if incoming category is null, hide any visible
            if (category == null)
            {
                //Debug.WriteLine("Incoming \"null\"");
                for ( int i = ActiveSubCategoryMenuStack.Length-1; i >= 0; --i ) {
                    if (ActiveSubCategoryMenuStack[i] != null)
                        RemoveChildWidget(ActiveSubCategoryMenuStack[i]);
                    ActiveSubCategoryMenuStack[i] = null;
                }
                return;
            }

            NodesCategory? Level0Cat = (category.ParentCategory != null) ? category.ParentCategory : category;
            PopupMenu? Level0Menu = Level0Cat.CategoryMenu;
            NodesCategory? Level1Cat = (category.ParentCategory == null) ? null : category;
            PopupMenu? Level1Menu = Level1Cat?.CategoryMenu ?? null;

            //Debug.WriteLine($"Level0 {Level0Cat.Label}  Level1 {Level1Cat?.Label??"null"}  Incoming {category?.Label??"null"}");

            for ( int i = 1; i >= 0; --i ) {
                PopupMenu? LevelMenu = (i == 0) ? Level0Menu : Level1Menu;
                if (ActiveSubCategoryMenuStack[i] != LevelMenu) {
                    if (ActiveSubCategoryMenuStack[i] != null) {
                        RemoveChildWidget(ActiveSubCategoryMenuStack[i]);
                        ActiveSubCategoryMenuStack[i] = null;
                    }
                    if (LevelMenu  != null) {
                        AddChildWidget(LevelMenu);
                        ActiveSubCategoryMenuStack[i] = LevelMenu;
                    }
                }
            }

        }


        private string FilterString = string.Empty;
        private bool nodes_menu_filter(MenuItem item)
        {
            if (FilterString == string.Empty) return true;
            //return item.Text.StartsWith(FilterString, StringComparison.InvariantCultureIgnoreCase);
            return item.Text.Contains(FilterString, StringComparison.InvariantCultureIgnoreCase);
        }

        private void SearchBox_OnTextEditingUpdate(TextEntryField sender, string newText)
        {
            if ( FilterString == newText )
                return;

            FilterString = newText;
            if (FilterString == string.Empty)
            {
                LinearAllNodesMenu.ResetFilteredItems();
            }
            else
            {
                LinearAllNodesMenu.FilterItems(this.nodes_menu_filter);
            }
            UpdateActiveMenu();
        }
        //private void SearchBox_OnTextModified(TextEntryField sender, string oldText, string newText)
        //{
        //    SearchBox_OnTextEditingUpdate(sender, newText);
        //}

        private void SearchBox_OnEnterKey()
        {
            LinearAllNodesMenu.SelectHighlightedItem();
        }

        private void NodesMenu_OnMenuItemSelected(PopupMenu popup, MenuItem selectedItem)
        {
            OnNewNodeTypeSelected?.Invoke(this, (selectedItem.CustomData as NodeType)! );
        }
		//protected virtual void OnNodeSelected()

		private void VariablesMenu_OnMenuItemSelected(PopupMenu popup, MenuItem selectedItem)
		{
            if (selectedItem.CustomData is VariablesTracker.VariableInfo variableInfo)
            {
                if ( variableInfo.CreatedAtNodeID <= 1)
                    OnGetSetVariableSelected?.Invoke(this, variableInfo, variableInfo.CreatedAtNodeID == 1);
                else
                    OnGetAliasSelected?.Invoke(this, variableInfo);

            } 
            else 
            {
                NodeAndPin? nodeAndPin = selectedItem.CustomData as NodeAndPin;

                NewVariableEventType type = NewVariableEventType.NewGlobal;
                if (selectedItem.Text == CreateSplitterLabel)
                    type = NewVariableEventType.CreateSplitter;
                else if (selectedItem.Text == CreateAliasLabel)
                    type = NewVariableEventType.CreateAlias;
                OnNewVariableSelected?.Invoke(this, nodeAndPin, type);
            }
		}

		private void NodesCategoryMenu_OnMenuItemHovered(PopupMenu popup, MenuItem? hoveredItem, bool bEnded)
        {
            NodesCategory? category = hoveredItem?.CustomData as NodesCategory ?? null;
            if (category != null)
                UpdateVisibleSubCategory(category);
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
            return new NewNodePopupDialogView(this);
        }




        public virtual bool IsCapturing { get; set; }
        public MenuItem? HoveredItem { get; protected set; } = null;
        public virtual void UpdateCapture(ISimpleCaptureTarget.ECaptureState State, in InputDeviceState deviceState)
        {
            IsCapturing = (State == ISimpleCaptureTarget.ECaptureState.Begin || State == ISimpleCaptureTarget.ECaptureState.Update);
            if (State == ISimpleCaptureTarget.ECaptureState.End) {
                WidgetHitResult hitResult = WidgetHitResult.None;
                if (GetActiveView()?.HitTest(deviceState.CurrentPosition) ?? false) 
                {
                    if (EnableClickToDismiss)
                        OnDismissDialogClick?.Invoke();
                }
            }
        }
        public virtual void UpdateHover(ISimpleCaptureTarget.EHoverState State, in InputDeviceState deviceState, out bool bContinueHover)
        {
            bContinueHover = false;
        }



        public virtual bool OnKeyChordUpdated(in KeyChord ActiveChord)
        {
            if (ActiveChord.IsSingleSpecialKey(KeyNames.Escape)) {

                if ((SearchBox.IsEditing == false || SearchBox.ActiveText.Length == 0)) {
                    OnDismissDialogClick?.Invoke();     // todo need to probably send this next frame or something?
                    return true;
                }
            } else if (ActiveChord.IsSingleSpecialKey(KeyNames.Enter))
            {
                if (LinearAllNodesMenu.EnumerateItems().Count() == 1)
                    LinearAllNodesMenu.ExternalSelectItem(LinearAllNodesMenu.EnumerateItems().First());
                else if (LinearAllNodesMenu.HighlightedItem != null)
                    LinearAllNodesMenu.ExternalSelectItem(LinearAllNodesMenu.HighlightedItem);
            } else if (ActiveChord.IsSingleSpecialKey(KeyNames.DownArrow)) {
                LinearAllNodesMenu.HighlightNextItem(true);
            } else if (ActiveChord.IsSingleSpecialKey(KeyNames.UpArrow)) {
                LinearAllNodesMenu.HighlightPreviousItem(true);
            }
            return false;
        }


        (string,string?) get_node_label(NodeType nodeType)
        {
            string nodeText = nodeType.GetNodeTypeUIName();

            if (nodeType.ClassType.IsSubclassOf(typeof(ControlFlowNode)))
                return (nodeText, null);
            if (nodeType.ClassType.IsSubclassOf(typeof(PlaceholderNodeBase)))
                return (nodeText, null);
            if (nodeType.ClassType.IsAssignableTo(typeof(INodeWithInlineCode)))
                return (nodeText, null);

            ENodeInputFlags ignoreFlags = ENodeInputFlags.IsNodeConstant | ENodeInputFlags.Hidden;
            foreach (INodeInputInfo inputInfo in nodeType.NodeArchetype!.EnumerateInputs()) {
                if ((inputInfo.Input.GetInputFlags() & ignoreFlags) != 0)
                    continue;
                if (inputInfo.DataType.CSType == typeof(object))
                    return (nodeText, null);
                string typeStr = TypeUtils.TypeToString(inputInfo.DataType);
                return (nodeText, $"({typeStr})");
            }
            return (nodeText, null);
        }


        // this is called each time the popup is shown. It populates the NodesMenu with a list of
        // all nodes, the NodesCategoryMenu with a list of all categories, and each category with
        // a list of all category-nodes. If an incoming pin is provided (ie menu shown by user wiring
        // off a pin into empty space) then these lists are filtered by the pin type, 
        //
        // The NodesMenu needs to have all possible nodes so that once the user starts typing in the
        // search box, it can be shown with filtering.
        //
        // Clearly lots of scaling issues here. There should be some kind of caching, the submenus don't need to
        // be populated until shown, the filtered all-nodes list should probably only be constructed
        // as needed and with some kind of clamping, etc etc
        //
        // (but note that a new Dialog instance is created every time currently, ie this is only called once
        // and then all the work is thrown away...
        //
        // (possibly this menu should be shown at the Avalonia level...)
		public void PopulateNodeLibrary(NodeLibrary Library, NodeAndPin? FromNodeAndPin = null)
        {
            // determine type of incoming pin, if there is one. Used to filter nodes list
            GraphDataType FromPinGraphDataType = GraphDataType.Default;
            bool bHaveValidFromPin = false;
			if (FromNodeAndPin != null && FromNodeAndPin.bIsSequencePin == false) {
                if (FromNodeAndPin.DataType.CSType != typeof(ControlFlowOutputID)) {
                    FromPinGraphDataType = FromNodeAndPin.DataType;
                    bHaveValidFromPin = true;
                }
			}

			Dictionary<string, NodesCategory> CategoryMap = new Dictionary<string, NodesCategory>();

            var GetMainCategory = (string Label) => {
                if (CategoryMap.TryGetValue(Label, out NodesCategory? found)) 
                    return found;
                
                NodesCategory newCategory = new NodesCategory(Label, Style);
                newCategory.CategoryMenu.AnchorTo(NodesCategoryMenuAnchor);
                newCategory.CategoryMenu.OnMenuItemSelected += NodesMenu_OnMenuItemSelected;
                CategoryMap.Add(newCategory.Label, newCategory);
                NodesCategories.Add(newCategory);
                TopLevelNodeSetsMenu.AddItem(new MenuItem() { Text = newCategory.Label, CustomData = newCategory });
                newCategory.CategoryMenu.OnMenuItemHovered += NodesCategoryMenu_OnMenuItemHovered;
                return newCategory;
            };
            var GetSubCategory = (string MainLabel, string SubLabel) => {
                string CombinedLabel = $"{MainLabel}.{SubLabel}";
                if (CategoryMap.TryGetValue(CombinedLabel, out NodesCategory? found))
                    return found;

                NodesCategory MainCat = GetMainCategory(MainLabel);
                if (MainCat.ChildCategories == null) 
                    MainCat.ChildCategories = new List<NodesCategory>();

                NodesCategory newSubCategory = new NodesCategory(SubLabel, Style);
                newSubCategory.CategoryMenu.AnchorTo(NodesCategorySubMenuAnchor);
                newSubCategory.CategoryMenu.OnMenuItemSelected += NodesMenu_OnMenuItemSelected;
                CategoryMap.Add(CombinedLabel, newSubCategory);
                MainCat.ChildCategories.Add(newSubCategory);
                newSubCategory.ParentCategory = MainCat;
                MainCat.CategoryMenu.AddItem(new MenuItem() { Text = SubLabel, CustomData = newSubCategory });
                return newSubCategory;
            };

            // add a node to the menu set. this will dynamically create it's category(s) if it doesn't exist yet.
            var TryAddToCategory = (NodeType nodeType) =>
            {
                (string nodeLabel, string? nodeHint) = get_node_label(nodeType);

                string? catLabel = apply_category_label_hacks(nodeType.UICategory);
                if (catLabel == null)
                    return;     // ignore this node...
                string? subLabel = null;
                if ( catLabel.Contains('.')) {
                    int idx = catLabel.IndexOf('.');
                    subLabel = catLabel.Substring(idx + 1);
                    catLabel = catLabel.Substring(0, idx);
                }

                NodesCategory foundCategory = (subLabel == null) ? GetMainCategory(catLabel) : GetSubCategory(catLabel, subLabel);
                foundCategory.CategoryMenu.AddItem(new MenuItem() { Text = nodeLabel, HintText = nodeHint, CustomData = nodeType });

            };

            // currently this function is never called more than once on an instance so this is unneccesary...
			LinearAllNodesMenu.ClearItems();

            // build out the menus for all nodes with a given input type, or just all nodes
            IEnumerable<NodeType> filteredNodes = (bHaveValidFromPin) ?
                Library.EnumerateAllNodesWithFirstAssignablePinType(FromPinGraphDataType) : Library.EnumerateAllNodes();

            foreach (NodeType nodeType in filteredNodes) 
            {
                if (nodeType.Flags.HasFlag(ENodeFlags.Hidden))
                    continue;

                (string nodeLabel, string? nodeHint) = get_node_label(nodeType);
                LinearAllNodesMenu.AddItem(new MenuItem() { Text = nodeLabel, HintText = nodeHint, CustomData = nodeType });
                TryAddToCategory(nodeType);
            }

            // sort everthing
			LinearAllNodesMenu.SortItems();
            TopLevelNodeSetsMenu.SortItems();
            foreach (var Category in NodesCategories) {
                Category.CategoryMenu.SortItems();
                if (Category.ChildCategories != null) {
                    foreach (var ChildCategory in Category.ChildCategories)
                        ChildCategory.CategoryMenu.SortItems();
                }
            }
        }
        protected static string? apply_category_label_hacks(string CategoryLabel)
        {
            if (CategoryLabel.StartsWith("Core.Constants"))
                return CategoryLabel.Replace("Core.Constants", "Constants");
            return CategoryLabel;
        }

        public void PopulateVariables(GraphStaticAnalyzer GraphAnalysis, NodeAndPin? FromNodeAndPin = null)
        {
            Debug.Assert(VariablesCategory == null);        // currently this is never called more than once...

			Type? FromPinDataType = null;
			if (FromNodeAndPin != null && FromNodeAndPin.bIsSequencePin == false) {
				FromPinDataType = (FromNodeAndPin.DataType.CSType != typeof(ControlFlowOutputID)) ? FromNodeAndPin.DataType.CSType : null;
			}

			VariablesCategory = new NodesCategory("Variables...", Style);
			VariablesCategory.CategoryMenu.AnchorTo(NodesCategoryMenuAnchor);
			VariablesCategory.CategoryMenu.OnMenuItemSelected += VariablesMenu_OnMenuItemSelected;

            if (FromPinDataType != null) {
                VariablesCategory.CategoryMenu.AddItem(new MenuItem() { Text = CreateSplitterLabel, CustomData = FromNodeAndPin }, -10);
            }

            if (FromPinDataType == null) {
                VariablesCategory.CategoryMenu.AddItem(new MenuItem() { Text = "New Global", CustomData = null }, -1);
            } else {
                VariablesCategory.CategoryMenu.AddItem(new MenuItem() { Text = CreateAliasLabel, CustomData = FromNodeAndPin }, -1);
                VariablesCategory.CategoryMenu.AddItem(new MenuItem() { Text = "New Global " + FromNodeAndPin!.Pin.GetDataTypeAsString(), CustomData = FromNodeAndPin }, -1);
            }

            foreach (var varInfo in GraphAnalysis.Variables.EnumerateAllAliases()) {
                VariablesTracker.VariableInfo getInfo = varInfo;
                getInfo.CreatedAtNodeID = 2;
                VariablesCategory.CategoryMenu.AddItem(new MenuItem() { Text = varInfo.Name + " (alias)", CustomData = getInfo });
            }

            foreach (var varInfo in GraphAnalysis.Variables.EnumerateAllVariables()) {
                VariablesTracker.VariableInfo getInfo = varInfo;
                getInfo.CreatedAtNodeID = 0;
                VariablesCategory.CategoryMenu.AddItem(new MenuItem() { Text = varInfo.Name + " (get)", CustomData = getInfo });
                VariablesTracker.VariableInfo setInfo = varInfo;
                setInfo.CreatedAtNodeID = 1;
                VariablesCategory.CategoryMenu.AddItem(new MenuItem() { Text = varInfo.Name + " (set)", CustomData = setInfo });
            }


            TopLevelNodeSetsMenu.AddItem(new MenuItem() { Text = VariablesCategory.Label, CustomData = VariablesCategory }, -1);
            TopLevelNodeSetsMenu.SortItems();
        }


        public void PopulateFunctions(ExecutionGraph execGraph, NodeAndPin? FromNodeAndPin = null)
        {
            Debug.Assert(FunctionsCategory == null);        // currently this is never called more than once...

            Type? FromPinDataType = null;
            if (FromNodeAndPin != null && FromNodeAndPin.bIsSequencePin == false) {
                FromPinDataType = (FromNodeAndPin.DataType.CSType != typeof(ControlFlowOutputID)) ? FromNodeAndPin.DataType.CSType : null;
            }

            FunctionsCategory = new NodesCategory("Functions...", Style);
            FunctionsCategory.CategoryMenu.AnchorTo(NodesCategoryMenuAnchor);
            FunctionsCategory.CategoryMenu.OnMenuItemSelected += FunctionsMenu_OnMenuItemSelected; ;

            foreach (INodeInfo nodeInfo in execGraph.EnumerateNodes()) {
                if ( nodeInfo.Node is FunctionDefinitionNode funcNode) {
                    // filter on from-pin type if we have one
                    if (FromPinDataType != null && funcNode.Arguments.Count() > 0) {
                        Type firstInputType = funcNode.Arguments.First().ArgType;
                        if (firstInputType.IsAssignableTo(FromPinDataType) == false)
                            continue;
                    }
                    FunctionsCategory.CategoryMenu.AddItem(new MenuItem() { Text = funcNode.FunctionName, CustomData = funcNode });
                }
            }

            if (FunctionsCategory.CategoryMenu.NumItems > 0) {
                TopLevelNodeSetsMenu.AddItem(new MenuItem() { Text = FunctionsCategory.Label, CustomData = FunctionsCategory }, -2);
                TopLevelNodeSetsMenu.SortItems();
            }
        }

        private void FunctionsMenu_OnMenuItemSelected(PopupMenu popup, MenuItem selectedItem)
        {
            if (selectedItem.CustomData is FunctionDefinitionNode functionNode)
                OnCreateFunctionCallSelected?.Invoke(this, functionNode);
        }
    }



    public class NewNodePopupDialogView : WidgetView
    {
        public NewNodePopupDialog SourceDialog;

        public AxisAlignedBox2f LocalBounds;
        public Vector2f DrawOrigin;
        public TextRect TextInfo;

        public NewNodePopupDialogView(NewNodePopupDialog sourceNewNodePopupDialog)
        {
            SourceDialog = sourceNewNodePopupDialog;
        }

        public override Widget GetWidget() { return SourceDialog; }

        public override AxisAlignedBox2f BoundsQuery(ILayoutAnchor? RelativeToAnchor = null)
        {
            return (RelativeToAnchor != null) ?
                AnchorLocation.GetAnchoredBounds(LocalBounds, RelativeToAnchor, GetWidget().AnchorPlacement) : LocalBounds;
        }

        public override void UpdateLayout(SKStyleCache StyleCache)
        {
            SourceDialog.SearchBox.GetActiveView()?.UpdateLayout(StyleCache);
            SourceDialog.ActiveMenu.GetActiveView()?.UpdateLayout(StyleCache);
            AxisAlignedBox2f SearchBounds = SourceDialog.SearchBox.GetActiveView()?.BoundsQuery(null) ?? AxisAlignedBox2f.Empty;
            AxisAlignedBox2f MainListBounds = SourceDialog.ActiveMenu.GetActiveView()?.BoundsQuery(null) ?? AxisAlignedBox2f.Empty;

            //foreach (PopupMenu? popupMenu in SourceDialog.ActiveSubCategoryMenuStack)
            //    popupMenu?.GetActiveView()?.UpdateLayout(StyleCache);
            PopupMenu? popupMenu0 = SourceDialog.ActiveSubCategoryMenuStack[0];
            PopupMenu? popupMenu1 = SourceDialog.ActiveSubCategoryMenuStack[1];
            popupMenu0?.GetActiveView()?.UpdateLayout(StyleCache);
            popupMenu1?.GetActiveView()?.UpdateLayout(StyleCache);

            AxisAlignedBox2f ListBoundsL0 = popupMenu0?.GetActiveView()?.BoundsQuery(null) ?? AxisAlignedBox2f.Empty;
            //AxisAlignedBox2f ListBoundsL1 = popupMenu1?.GetActiveView()?.BoundsQuery(null) ?? AxisAlignedBox2f.Empty;

            SourceDialog.SearchBoxAnchor.Box = new AxisAlignedBox2f(SearchBounds);
            SourceDialog.NodesMenuAnchor.Box = SourceDialog.SearchBoxAnchor.Box;
            SourceDialog.NodesMenuAnchor.BoxPoint = BoxPoints.BottomLeft;
            SourceDialog.NodesMenuAnchor.Offset = new Vector2f(0, 5);

            SourceDialog.NodesCategoryMenuAnchor.Box = MainListBounds;
            // todo this is a hack - should be basing off the laid-out bounds like below...
            SourceDialog.NodesCategoryMenuAnchor.Offset = new Vector2f(5, SearchBounds.Height+5);

            // yikes
            SourceDialog.NodesCategorySubMenuAnchor.Box = MainListBounds;
            SourceDialog.NodesCategorySubMenuAnchor.Offset = new Vector2f(ListBoundsL0.Width+10, SearchBounds.Height+5);


            AxisAlignedBox2f ChildBounds = SourceDialog.SearchBox.GetActiveView()?.BoundsQuery(SourceDialog.SearchBox.GetAnchor()) ?? AxisAlignedBox2f.Empty;
            AxisAlignedBox2f WorldListBounds = SourceDialog.ActiveMenu.GetActiveView()?.BoundsQuery(SourceDialog.ActiveMenu.GetAnchor()) ?? AxisAlignedBox2f.Empty;
            ChildBounds.Contain(WorldListBounds);

            Vector2f Origin = SourceDialog.GetAnchor()?.GetOrigin() ?? Vector2f.Zero;
            ChildBounds.Translate(-Origin);
            LocalBounds = ChildBounds;
        }

        public override void Draw(SKStyleCache StyleCache, SKCanvas Canvas, ILayoutAnchor Anchor)
        {
            DrawOrigin = Anchor.GetOrigin();
        }


        public override bool HitTest(Vector2f QueryPoint)
        {
            if (SourceDialog.EnableClickToDismiss)
                return true;
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
