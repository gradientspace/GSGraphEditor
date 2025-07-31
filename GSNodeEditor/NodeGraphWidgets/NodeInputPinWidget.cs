// Copyright Gradientspace Corp. All Rights Reserved.
using Gradientspace.UI;
using g3;
using Gradientspace.NodeGraph;
using SkiaSharp;
using System.Diagnostics;
using System.Reflection;
using static Gradientspace.NodeGraph.TypeUtils;

namespace GSNodeEditor
{



    public enum EInlineWidgetType
    {
        None,
        Nullable,

        Boolean,
        Integer,
        Float,
        String,
        Enum,
        EnumList,
        Type,

        FromProvider
    }


    public class NodeInputPinWidget : NodePinWidget, ISimpleCaptureTarget
    {
        public INodeInputInfo NodeInputInfo { get; private set; }
        public string InputName { get; set; } = "";

        public EInlineWidgetType InlineType { get; set; } = EInlineWidgetType.None;
        public object? InlineValue { get; set; } = null;
        public Widget? InlineWidget { get; set; } = null;

        public bool ShowConversionWarning { get; private set; } = false;
        public Type? FromTypeConversion { get; private set; } = null;

        public BoxAnchor InlineWidgetAnchor { get; private set; }

        public NodeInputPinWidget(INodeInputInfo sourceInputInfo)
        {
            NodeInputInfo = sourceInputInfo;
            InputName = sourceInputInfo.InputName;
            DataType = sourceInputInfo.DataType;

            WidgetStyle = PinWidgetStyles.DefaultInputStyleSet;

            SetInputBehavior(new ExtendableWidgetInputBehavior(this, this) { Depth = 0 } );

            InlineWidgetAnchor = new BoxAnchor();
        }

        public override bool IsOutputPin { get { return false; } }

        public override string GetDataTypeAsString()
        {
            string? CustomTypeString = DataType.ExtendedTypeInfo?.GetCustomTypeString() ?? null;
            return CustomTypeString ?? TypeUtils.TypeToString(DataType.DataType);
        }


        public override IWidgetView CreateDefaultView()
        {
            return new NodeInputPinWidgetView(this);
        }

        public override bool GetTooltipStrings(out string? tooltip, out string[]? extendedTooltip)
        {
            tooltip = GetDataTypeAsString();
            extendedTooltip = null;
            return true;
        }

        public string GetPinLabel()
        {
            if (InlineType == EInlineWidgetType.Nullable)
                return InputName + "?";
            else
                return InputName;
        }

        public void UpdateInlineInfo(INodeGraph Graph, int OwningNodeIdentifier)
        {
            InlineType = EInlineWidgetType.None;
            InlineValue = null;
            ShowConversionWarning = false; FromTypeConversion = null;

            IInlinePinWidgetProvider? UseWidgetProvider = null;

            Type pinType = DataType.DataType;

            (object? defaultValue, bool bDefaultIsDefined) = Graph.GetNodeConstantValue(OwningNodeIdentifier, InputName);
            IConnectionInfo Connection = Graph.FindConnectionTo(OwningNodeIdentifier, InputName);
            if (Connection.IsValid == false && bDefaultIsDefined && defaultValue != null)
            {
                if (pinType == typeof(bool))
                {
                    InlineType = EInlineWidgetType.Boolean;
                }
                else if (pinType == typeof(float) || pinType == typeof(double))
                {
                    InlineType = EInlineWidgetType.Float;
                }
                else if (pinType == typeof(int) || pinType == typeof(short) || pinType == typeof(long))
                {
                    InlineType = EInlineWidgetType.Integer;
                }
                else if (pinType == typeof(string))
                {
                    InlineType = EInlineWidgetType.String;
                }
                else if (pinType.IsEnum)
                {
                    InlineType = EInlineWidgetType.Enum;
                }
                else if (pinType == typeof(EnumOptionItem) && NodeInputInfo.Input is IEnumOptionSetNodeInput )
                {
                    InlineType = EInlineWidgetType.EnumList;
                }
                else if (pinType == typeof(Type))
                {
                    InlineType = EInlineWidgetType.Type;
                }
                else
                {
                    UseWidgetProvider = InlinePinWidgetSystem.Instance.FindProvider(pinType);
                    if (UseWidgetProvider != null)
                        InlineType = EInlineWidgetType.FromProvider;
                }
            }
            else if (Connection.IsValid == false && bDefaultIsDefined && defaultValue == null)
            {
                InlineType = EInlineWidgetType.Nullable;
            }

            if (InlineType == EInlineWidgetType.None && Connection.IsValid == true)
            {
                bool bHaveFromType = Graph.GetNodeOutputType(Connection.FromNodeIdentifier, Connection.FromNodeOutputName, out GraphDataType FromDataType);
                if (bHaveFromType && FromDataType.DataType != pinType && TypeUtils.IsLossyNumericConversion(FromDataType.DataType, pinType)) {
                    ShowConversionWarning = true;
                    FromTypeConversion = FromDataType.DataType;
                }
            }

            if (InlineType == EInlineWidgetType.None && InlineWidget != null)
            {
                RemoveChildWidget(InlineWidget);
                InlineWidget = null;
            }
            else if(InlineType == EInlineWidgetType.Nullable)
            {
                // no inline widget for nullable currently
                InlineWidget = null;
            }
            else if (InlineType != EInlineWidgetType.None && InlineWidget == null)
            {
                if (InlineType == EInlineWidgetType.Boolean)
                {
                    Checkbox checkbox = new Checkbox();
                    if (bDefaultIsDefined && defaultValue != null && defaultValue is bool)
                        checkbox.Checked = (bool)defaultValue;
                    InlineWidgetAnchor.BoxPoint = BoxPoints.CenterRight;
                    checkbox.AnchorTo(InlineWidgetAnchor);
                    checkbox.AnchorPlacement = new AnchorLocation(BoxPoints.CenterRight);
                    checkbox.OnToggled += (object? sender, EventArgs args) => {
                        UpdateInputFromModifiedBoolean(Graph, OwningNodeIdentifier, ((Checkbox)sender!)!.Checked);
                    };
                    InlineWidget = checkbox;
                    AddChildWidget(InlineWidget);
                }
                else if (InlineType == EInlineWidgetType.Float || InlineType == EInlineWidgetType.Integer || InlineType == EInlineWidgetType.String)
                {
                    TextEntryField textEntry = new TextEntryField();
                    textEntry.Width = 40;
                    if (InlineType == EInlineWidgetType.Float)
                        textEntry.ValidationType = TextEntryField.StringValidation.Real;
                    else if (InlineType == EInlineWidgetType.Integer)
                        textEntry.ValidationType = TextEntryField.StringValidation.Integer;
                    else if (InlineType == EInlineWidgetType.String)
                        textEntry.Width = 65;

                    if (bDefaultIsDefined && defaultValue != null) {
                        if (defaultValue is float)
                            textEntry.Text = ((float)defaultValue).ToString("0.0#######");
                        else if (defaultValue is double)
                            textEntry.Text = ((double)defaultValue).ToString("0.0#######");
                        else if ((defaultValue is int) || (defaultValue is short) || (defaultValue is long))
                            textEntry.Text = ((int)defaultValue).ToString();
                        else if (defaultValue is string)
                            textEntry.Text = (string)defaultValue;
                    }

                    InlineWidgetAnchor.BoxPoint = BoxPoints.CenterRight;
                    textEntry.AnchorTo(InlineWidgetAnchor);
                    textEntry.AnchorPlacement = new AnchorLocation(BoxPoints.CenterRight);
                    textEntry.OnTextModified += (TextEntryField sender, string oldText, string newText) => {
                        UpdateInputFromModifiedTextEntry(Graph, OwningNodeIdentifier, newText);
                    };
                    InlineWidget = textEntry;
                    AddChildWidget(InlineWidget);
                }
                else if (InlineType == EInlineWidgetType.Enum)
                {
                    if ( TypeUtils.GetEnumInfo(pinType, out TypeUtils.EnumInfo enumInfo) == false )
                        return;

                    DropDownPicker dropdown = new DropDownPicker();
                    for (int i = 0; i < enumInfo.NumEnumValues; ++i) 
                        dropdown.AddItem(enumInfo.EnumStrings[i], enumInfo.EnumIDs[i], enumInfo.EnumValues[i]);

                    if (bDefaultIsDefined && defaultValue != null) {
                        if (enumInfo.FindIndexForEnumValue(defaultValue, out int SelectIndex))
                            dropdown.SetSelectedIndex(SelectIndex);
                    }

                    InlineWidgetAnchor.BoxPoint = BoxPoints.CenterRight;
                    dropdown.AnchorTo(InlineWidgetAnchor);
                    dropdown.AnchorPlacement = new AnchorLocation(BoxPoints.CenterRight);
                    dropdown.OnSelectionModified += (DropDownPicker dropdown, int oldIndex, int newIndex) => {
                        if (dropdown.GetItemAtIndex(newIndex, out string label, out int externalID, out object? externalObject)) {
                            if (externalObject != null && externalObject.GetType() == pinType)
                                UpdateInputFromModifiedEnum(Graph, OwningNodeIdentifier, externalObject);
                        }
                    };
                    InlineWidget = dropdown;
                    AddChildWidget(InlineWidget);
                }
                else if (InlineType == EInlineWidgetType.EnumList) 
                {
                    IEnumOptionSetNodeInput EnumInput = (IEnumOptionSetNodeInput)NodeInputInfo.Input;     // verified when selecting this type
                    EnumOptionSet EnumList = EnumInput.GetOptionSet();
                    DropDownPicker dropdown = new DropDownPicker();
                    foreach (EnumOptionSet.OptionItem item in EnumList.Items)
                        dropdown.AddItem(item.Label, 0, item.TransientData);

                    if (bDefaultIsDefined && defaultValue != null && defaultValue is EnumOptionItem) {
                        if (EnumList.FindIndexFromLabel( ((EnumOptionItem)defaultValue).ItemString, out int SelectIndex))
                            dropdown.SetSelectedIndex(SelectIndex);
                    }

                    InlineWidgetAnchor.BoxPoint = BoxPoints.CenterRight;
                    dropdown.AnchorTo(InlineWidgetAnchor);
                    dropdown.AnchorPlacement = new AnchorLocation(BoxPoints.CenterRight);
                    dropdown.OnSelectionModified += (DropDownPicker dropdown, int oldIndex, int newIndex) => {
                        if (dropdown.GetItemAtIndex(newIndex, out string label, out int externalID, out object? externalObject)) 
                            UpdateInputFromModifiedEnumList(Graph, OwningNodeIdentifier, label);
                    };
                    InlineWidget = dropdown;
                    AddChildWidget(InlineWidget);
                }
                else if (InlineType == EInlineWidgetType.Type)
                {
                    TypeComboBox typeEntry = new TypeComboBox();
                    typeEntry.Width = 40;

                    if (bDefaultIsDefined && defaultValue != null)
                        typeEntry.SelectedType = (defaultValue as Type) ?? typeof(object);

                    InlineWidgetAnchor.BoxPoint = BoxPoints.CenterRight;
                    typeEntry.AnchorTo(InlineWidgetAnchor);
                    typeEntry.AnchorPlacement = new AnchorLocation(BoxPoints.CenterRight);
                    typeEntry.OnSelectedTypeChanged += (TypeComboBox sender, Type newType) => {
                        UpdateInputFromModifiedTypeSelector(Graph, OwningNodeIdentifier, newType);
                    };
                    InlineWidget = typeEntry;
                    AddChildWidget(InlineWidget);
                }
                else if (InlineType == EInlineWidgetType.FromProvider && UseWidgetProvider != null)
                {
                    Widget? inlineWidget = UseWidgetProvider.CreateNewWidget(Graph, OwningNodeIdentifier, this, defaultValue, bDefaultIsDefined);
                    if (inlineWidget != null)
                    {
                        InlineWidget = inlineWidget;

                        InlineWidgetAnchor.BoxPoint = BoxPoints.CenterRight;
                        InlineWidget.AnchorTo(InlineWidgetAnchor);
                        InlineWidget.AnchorPlacement = new AnchorLocation(BoxPoints.CenterRight);

                        AddChildWidget(InlineWidget);
                    }

                }
            }
        }


        // send new constant value to graph, via GraphEditor
        private void UpdateInputFromModifiedTextEntry(INodeGraph Graph, int OwningNodeIdentifier, string newText)
        {
            NodeGraphView? ParentView = FindParentGraphViewChecked();

			if (InlineType == EInlineWidgetType.Float) 
            {
                if ( float.TryParse(newText, out float value)) {
                    ParentView.ExecuteGraphEdit((NodeGraphEditor Editor) => {
                        Editor.SetNodeConstantValue(OwningNodeIdentifier, InputName, value);
                    });
                }
            } 
            else if (InlineType == EInlineWidgetType.Integer) 
            {
                if (int.TryParse(newText, out int value))
                    ParentView.ExecuteGraphEdit((NodeGraphEditor Editor) => {
                        Editor.SetNodeConstantValue(OwningNodeIdentifier, InputName, value);
                    });
            } 
            else if (InlineType == EInlineWidgetType.String ) 
            {
                // kind of gross that we have to do this at such a low level...if we are updating
                // the input on a special VariableNameNodeInput type, we actually want to try a
                // global graph rename...
                if ( this.NodeInputInfo.Input is VariableNameNodeInput variableNameInput ) {
                    TryRenameVariableInput(variableNameInput, OwningNodeIdentifier, newText);
                } else {
                    ParentView.ExecuteGraphEdit((NodeGraphEditor Editor) => {
                        Editor.SetNodeConstantValue(OwningNodeIdentifier, InputName, newText);
                    });
                }
            } 
        }
        private void UpdateInputFromModifiedBoolean(INodeGraph Graph, int OwningNodeIdentifier, bool bNewValue)
        {
			NodeGraphView? ParentView = FindParentGraphViewChecked();
			if (InlineType == EInlineWidgetType.Boolean) {
                ParentView.ExecuteGraphEdit((NodeGraphEditor Editor) => {
                    Editor.SetNodeConstantValue(OwningNodeIdentifier, InputName, bNewValue);
                });
            }
        }
        private void UpdateInputFromModifiedEnum(INodeGraph Graph, int OwningNodeIdentifier, object newEnumValue)
        {
			NodeGraphView? ParentView = FindParentGraphViewChecked();
			if (InlineType == EInlineWidgetType.Enum) {
                ParentView.ExecuteGraphEdit((NodeGraphEditor Editor) => {
                    Editor.SetNodeConstantValue(OwningNodeIdentifier, InputName, newEnumValue);
                });
            }
        }
        private void UpdateInputFromModifiedEnumList(INodeGraph Graph, int OwningNodeIdentifier, string newEnumString)
        {
			NodeGraphView? ParentView = FindParentGraphViewChecked();
			if (InlineType == EInlineWidgetType.EnumList) {
                ParentView.ExecuteGraphEdit((NodeGraphEditor Editor) => {
                    Editor.SetNodeConstantValue(OwningNodeIdentifier, InputName, new EnumOptionItem(newEnumString));
                });
            }
        }
        private void UpdateInputFromModifiedTypeSelector(INodeGraph Graph, int OwningNodeIdentifier, Type newType)
        {
			NodeGraphView? ParentView = FindParentGraphViewChecked();
			if (InlineType == EInlineWidgetType.Type) {
                ParentView.ExecuteGraphEdit((NodeGraphEditor Editor) => {
                    Editor.SetNodeConstantValue(OwningNodeIdentifier, InputName, newType);
                });
            }
        }
        private void TryRenameVariableInput(VariableNameNodeInput variableNameInput, int OwningNodeIdentifier, string NewName)
        {
            NodeGraphView ParentView = FindParentGraphViewChecked();
            ParentView.ExecuteGraphEdit((NodeGraphEditor Editor) => {
                (object? curValue, bool bSet) = variableNameInput.GetConstantValue();
                if (bSet == false)
                    return;
                string curName = (curValue as string)!;
                bool bSuccess = Editor.TryRenameVariable(OwningNodeIdentifier, curName, NewName);
                if (bSuccess == false)
                    (this.InlineWidget as TextEntryField)!.SilentUpdateText(curName);   // reset string to previous value
            });
        }


        // ISimpleCaptureTarget API
        public void UpdateCapture(ISimpleCaptureTarget.ECaptureState State, in InputDeviceState deviceState)
        {
            IsCapturing = (State == ISimpleCaptureTarget.ECaptureState.Begin || State == ISimpleCaptureTarget.ECaptureState.Update);
            //if (State == ECaptureState.Begin)
            //{
            //    InitialNodePosition = this.Position; InitialCursorPosition = deviceState.CurrentPosition;
            //}
            //else if (State == ECaptureState.Update)
            //{
            //    Vector2f Delta = deviceState.CurrentPosition - InitialCursorPosition;
            //    Position = InitialNodePosition + Delta;
            //}
        }
        public void UpdateHover(ISimpleCaptureTarget.EHoverState State, in InputDeviceState deviceState, out bool bContinueHover)
        {
            bContinueHover = true;
            IsHovered = (State == ISimpleCaptureTarget.EHoverState.Begin || State == ISimpleCaptureTarget.EHoverState.Update);
        }
        public bool IsHovered { get; private set; }
        public bool IsCapturing { get; private set; }
    }



    public class NodeInputPinWidgetView : IWidgetView
    {
        NodeInputPinWidget SourcePinWidget;
		public int LastDrawOrderIndex { get; set; } = 0;

		public NodeInputPinWidgetView(NodeInputPinWidget pinWidget)
        {
            this.SourcePinWidget = pinWidget;
        }

        public Widget GetWidget() { return SourcePinWidget; }

        public AxisAlignedBox2f LocalBounds { get; set; }
        public Vector2f DrawOrigin { get; set; }

        public AxisAlignedBox2f BoundsQuery(ILayoutAnchor? RelativeToAnchor = null)
        {
            return (RelativeToAnchor != null) ?
                AnchorLocation.GetAnchoredBounds(LocalBounds, RelativeToAnchor, GetWidget().AnchorPlacement) : LocalBounds;
        }

        public void UpdateLayout(SKStyleCache StyleCache)
        {
            SKPaint PinTextPaint =
                StyleCache.GetCachedPaint(SourcePinWidget.WidgetStyle.StandardStyle, SKStyleCache.EPaintType.Text);
            WidgetMargins PinMargins = SourcePinWidget.WidgetStyle.BaseMargins;

            float InputTextWidth = (SourcePinWidget.CompactMode) ? 5 : PinTextPaint.MeasureText(SourcePinWidget.GetPinLabel());
            UpdateWidthForInlineWidgets(ref InputTextWidth);

            TextHeightInfo PinTextHeightInfo = StyleCache.GetCachedFontHeightInfo(SourcePinWidget.WidgetStyle.StandardStyle);
            float InputPinRight = (InputTextWidth + PinMargins.TotalWidth);

            //float PinHeight = (SourcePinWidget.CompactMode) ? 5 : PinTextHeightInfo.MaxTotalHeight;
            float PinHeight = PinTextHeightInfo.MaxTotalHeight;
            LocalBounds = new AxisAlignedBox2f(0, 0, InputPinRight, PinHeight + PinMargins.TotalHeight);

            // update any child inline widget layout, if it exists
            SourcePinWidget.InlineWidget?.GetActiveView()?.UpdateLayout(StyleCache);
        }

        void UpdateWidthForInlineWidgets(ref float CurWidth)
        {
            if (SourcePinWidget.InlineWidget == null) return;

            if (SourcePinWidget.InlineType == EInlineWidgetType.Boolean)
            {
                CurWidth += 20;
            }
            else if (SourcePinWidget.InlineWidget is TextEntryField)
            {
                CurWidth += (SourcePinWidget.InlineWidget as TextEntryField)!.Dimensions.x;
            }
            else if (SourcePinWidget.InlineWidget is DropDownPicker)
            {
                CurWidth += (SourcePinWidget.InlineWidget as DropDownPicker)!.Dimensions.x;
            }
            else if (SourcePinWidget.InlineType == EInlineWidgetType.FromProvider)
            {
                if (SourcePinWidget.InlineWidget is IInlinePinWidget)
                    CurWidth += (SourcePinWidget.InlineWidget as IInlinePinWidget)!.RequiredWidth;
                else
                    CurWidth += 20;
            }
        }


        public bool HitQuery(Vector2f QueryPoint, out WidgetHitResult Result)
        {
            Result = WidgetHitResult.None;

            AxisAlignedBox2f WorldBounds =
                AnchorLocation.GetAnchoredBounds(LocalBounds, DrawOrigin, GetWidget().AnchorPlacement);
            if (WorldBounds.Contains(QueryPoint) == false)
                return false;

            Result = new WidgetHitResult() { HitWidget = SourcePinWidget, HitZDepth = 5 };
            return true;
        }


        public bool HitTest(Vector2f QueryPoint)
        {
            WidgetHitResult HitResult;
            return HitQuery(QueryPoint, out HitResult);
        }


        public void Draw(SKStyleCache StyleCache, SKCanvas Canvas, ILayoutAnchor Anchor)
        {
            SKPaint PinTextPaint = StyleCache.GetCachedPaint(SourcePinWidget.WidgetStyle.StandardStyle, SKStyleCache.EPaintType.Text);
            WidgetMargins PinMargins = SourcePinWidget.WidgetStyle.BaseMargins;
            TextHeightInfo PinTextHeightInfo = StyleCache.GetCachedFontHeightInfo(SourcePinWidget.WidgetStyle.StandardStyle);

            NodePinWidgetStyle UseStyle = (SourcePinWidget.ShowConversionWarning) ? PinWidgetStyles.InputStyleSet_Conversion : SourcePinWidget.WidgetStyle;
            if (SourcePinWidget.DataType.IsDynamic)
                UseStyle = PinWidgetStyles.InputStyleSet_Dynamic;

            SKPaint InputPinPaint = StyleCache.GetCachedPaint(UseStyle.Select(SourcePinWidget.IsHovered, false), SKStyleCache.EPaintType.Background);

            DrawOrigin = Anchor.GetOrigin();
            AxisAlignedBox2f PlacedBounds = AnchorLocation.MakeRelativeToAnchor(LocalBounds, SourcePinWidget.AnchorPlacement, DrawOrigin);

            SourcePinWidget.InlineWidgetAnchor.SourceBounds = PlacedBounds;

            Canvas.DrawRect(Conversion.ToSkia(PlacedBounds), InputPinPaint);
            Vector2f TextCorner = PlacedBounds.Min + new Vector2f(PinMargins.Left, PinMargins.Top + PinTextHeightInfo.AboveBaseline);
            if (SourcePinWidget.CompactMode == false)
                Canvas.DrawText(SourcePinWidget.GetPinLabel(), Conversion.ToSkia(TextCorner), PinTextPaint);

            // TODO this should be refactored out somewhere...
            if ( SourcePinWidget.ShowConversionWarning && SourcePinWidget.FromTypeConversion != null && SourcePinWidget.IsHovered )
            {
                SKPaint DataTypeTextPaint = new SKPaint { Color = SKColors.White, IsAntialias = true, LcdRenderText = true, SubpixelText = true, TextSize = 10 };
                TextHeightInfo DataTypeTextHeightInfo = SKStyleCache.MeasureTextHeightInfo(DataTypeTextPaint);
                SKPaint WarningDataTypeFillPaint = new SKPaint { Color = SKColors.DarkOrange };
                const float Margin = 3;
                string TypeText = TypeUtils.TypeToString(SourcePinWidget.DataType.DataType) + " -> " + TypeUtils.TypeToString(SourcePinWidget.FromTypeConversion!);
                SKRect Bounds = SKRect.Empty;
                float Width = DataTypeTextPaint.MeasureText(TypeText, ref Bounds);
                Bounds.Left -= (Margin + 2); Bounds.Right += (Margin + 1); Bounds.Bottom += Margin; Bounds.Top -= Margin;
                SKMatrix CurMatrix = Canvas.TotalMatrix;
                SKPoint Offset = Conversion.ToSkia(PlacedBounds.BottomRight + new Vector2f(3, -3));
                Canvas.Translate(Offset);
                Canvas.DrawRoundRect(Bounds, 8.0f, 8.0f, WarningDataTypeFillPaint);
                Canvas.DrawText(TypeText, new SKPoint(0, 0), DataTypeTextPaint);
                Canvas.SetMatrix(CurMatrix);
            }
        }


    }

}
