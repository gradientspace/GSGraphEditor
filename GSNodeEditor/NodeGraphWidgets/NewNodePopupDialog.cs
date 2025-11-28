// Copyright Gradientspace Corp. All Rights Reserved.
using g3;
using Gradientspace.NodeGraph;
using Gradientspace.NodeGraph.CodeNodes;
using Gradientspace.UI;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        internal WidgetRelativeBoxAnchor NodesCategoryMenuAnchor;
        internal PopupMenu NodesMenu;
        internal PopupMenu? ActiveSubCategoryMenu = null;


        internal class NodesCategory
        {
            public string Label;
            public PopupMenu CategoryMenu;
            public NodesCategory(string label, WidgetStateStyle Style)
            {
                Label = label;
                CategoryMenu = new PopupMenu(Style);
                CategoryMenu.EnableClickToDismiss = false;
                CategoryMenu.AnchorPlacement = new AnchorLocation(BoxPoints.BottomLeft);
            }
        }

        internal PopupMenu NodesCategoryMenu;
        internal NodesCategory? VariablesCategory = null;
        internal NodesCategory? FunctionsCategory = null;
        internal List<NodesCategory> NodesCategories = new List<NodesCategory>();
        bool bCategoryMenuActive = false;


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

            NodesMenu = new PopupMenu(Style);
            NodesMenu.EnableClickToDismiss = false;
            NodesMenu.AnchorPlacement = new AnchorLocation(BoxPoints.BottomLeft);
            NodesMenu.AnchorTo(NodesMenuAnchor);
            NodesMenu.AddItem(new MenuItem() { Text = "Item A" });
            NodesMenu.AddItem(new MenuItem() { Text = "Item B" });

            NodesMenu.OnMenuItemSelected += NodesMenu_OnMenuItemSelected;

            NodesCategoryMenu = new PopupMenu(Style);
            NodesCategoryMenu.EnableClickToDismiss = false;
            NodesCategoryMenu.AnchorPlacement = new AnchorLocation(BoxPoints.BottomLeft);
            NodesCategoryMenu.AnchorTo(NodesMenuAnchor);
            NodesCategoryMenu.OnMenuItemHovered += NodesCategoryMenu_OnMenuItemHovered;

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
            get { return (bCategoryMenuActive) ? NodesCategoryMenu : NodesMenu; }
        }
        void UpdateActiveMenu()
        {
            bool bShouldShowCategoryMenu = (FilterString == string.Empty);
            if ( bShouldShowCategoryMenu != bCategoryMenuActive )
            {
                if (bCategoryMenuActive == false)
                {
                    RemoveChildWidget(NodesMenu);
                    AddChildWidget(NodesCategoryMenu);
                    bCategoryMenuActive = true;
                }
                else
                {
                    RemoveChildWidget(NodesCategoryMenu);
                    AddChildWidget(NodesMenu);
                    bCategoryMenuActive = false;
                }
            }
            UpdateVisibleSubCategory(null);
        }
        void UpdateVisibleSubCategory(NodesCategory? category)
        {
            if (category == null && ActiveSubCategoryMenu != null)
            {
                RemoveChildWidget(ActiveSubCategoryMenu);
                ActiveSubCategoryMenu = null;
                return;
            }

            if (category != null && category.CategoryMenu != ActiveSubCategoryMenu )
            {
                if (ActiveSubCategoryMenu != null)
                {
                    RemoveChildWidget(ActiveSubCategoryMenu);
                }
                ActiveSubCategoryMenu = category.CategoryMenu;
                AddChildWidget(ActiveSubCategoryMenu);
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
                NodesMenu.ResetFilteredItems();
            }
            else
            {
                NodesMenu.FilterItems(this.nodes_menu_filter);
            }
            UpdateActiveMenu();
        }
        //private void SearchBox_OnTextModified(TextEntryField sender, string oldText, string newText)
        //{
        //    SearchBox_OnTextEditingUpdate(sender, newText);
        //}

        private void SearchBox_OnEnterKey()
        {
            NodesMenu.SelectHighlightedItem();
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
            if (category != null && category.CategoryMenu != ActiveSubCategoryMenu)
            {
                UpdateVisibleSubCategory(null);
                UpdateVisibleSubCategory(category);
            }
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
                if (NodesMenu.EnumerateItems().Count() == 1)
                    NodesMenu.ExternalSelectItem(NodesMenu.EnumerateItems().First());
                else if (NodesMenu.HighlightedItem != null)
                    NodesMenu.ExternalSelectItem(NodesMenu.HighlightedItem);
            } else if (ActiveChord.IsSingleSpecialKey(KeyNames.DownArrow)) {
                NodesMenu.HighlightNextItem(true);
            } else if (ActiveChord.IsSingleSpecialKey(KeyNames.UpArrow)) {
                NodesMenu.HighlightPreviousItem(true);
            }
            return false;
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

            var get_node_label = (NodeType nodeType) => {
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
                return (nodeText,null);
            };

            // add a node to the menu set. this will dynamically create it's category if it doesn't exist yet.
            var TryAddToCategory = (NodeType nodeType) =>
            {
                (string nodeLabel, string? nodeHint) = get_node_label(nodeType);

                if ( CategoryMap.TryGetValue(nodeType.UICategory, out NodesCategory? found) )
                {
                    found.CategoryMenu.AddItem(new MenuItem() { Text = nodeLabel, HintText = nodeHint, CustomData = nodeType });
                }
                else
                {
                    NodesCategory newCategory = new NodesCategory(nodeType.UICategory, Style);
                    newCategory.CategoryMenu.AnchorTo(NodesCategoryMenuAnchor);
                    newCategory.CategoryMenu.OnMenuItemSelected += NodesMenu_OnMenuItemSelected;

                    newCategory.CategoryMenu.AddItem(new MenuItem() { Text = nodeLabel, HintText = nodeHint, CustomData = nodeType });
                    CategoryMap.Add(newCategory.Label, newCategory);

                    NodesCategories.Add(newCategory);
                    NodesCategoryMenu.AddItem(new MenuItem() { Text = newCategory.Label, CustomData = newCategory });
                }
            };

            // currently this function is never called more than once on an instance so this is unneccesary...
			NodesMenu.ClearItems();

            // build out the menus for all nodes with a given input type, or just all nodes
            IEnumerable<NodeType> filteredNodes = (bHaveValidFromPin) ?
                Library.EnumerateAllNodesWithFirstAssignablePinType(FromPinGraphDataType) : Library.EnumerateAllNodes();

            foreach (NodeType nodeType in filteredNodes) 
            {
                if (nodeType.Flags.HasFlag(ENodeFlags.Hidden))
                    continue;

                (string nodeLabel, string? nodeHint) = get_node_label(nodeType);
                NodesMenu.AddItem(new MenuItem() { Text = nodeLabel, HintText = nodeHint, CustomData = nodeType });
                TryAddToCategory(nodeType);
            }

            // sort everthing
			NodesMenu.SortItems();
            NodesCategoryMenu.SortItems();
            foreach (var Category in NodesCategories)
                Category.CategoryMenu.SortItems();
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


            NodesCategoryMenu.AddItem(new MenuItem() { Text = VariablesCategory.Label, CustomData = VariablesCategory }, -1);
            NodesCategoryMenu.SortItems();
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
                NodesCategoryMenu.AddItem(new MenuItem() { Text = FunctionsCategory.Label, CustomData = FunctionsCategory }, -2);
                NodesCategoryMenu.SortItems();
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
            SourceDialog.ActiveSubCategoryMenu?.GetActiveView()?.UpdateLayout(StyleCache);

            AxisAlignedBox2f SearchBounds = SourceDialog.SearchBox.GetActiveView()?.BoundsQuery(null) ?? AxisAlignedBox2f.Empty;
            AxisAlignedBox2f ListBounds = SourceDialog.ActiveMenu.GetActiveView()?.BoundsQuery(null) ?? AxisAlignedBox2f.Empty;

            SourceDialog.SearchBoxAnchor.Box = new AxisAlignedBox2f(SearchBounds);
            SourceDialog.NodesMenuAnchor.Box = SourceDialog.SearchBoxAnchor.Box;
            SourceDialog.NodesMenuAnchor.BoxPoint = BoxPoints.BottomLeft;
            SourceDialog.NodesMenuAnchor.Offset = new Vector2f(0, 5);

            SourceDialog.NodesCategoryMenuAnchor.Box = ListBounds;
            // todo this is a hack - should be basing off the laid-out bounds like below...
            SourceDialog.NodesCategoryMenuAnchor.Offset = new Vector2f(5, SearchBounds.Height+5);

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
