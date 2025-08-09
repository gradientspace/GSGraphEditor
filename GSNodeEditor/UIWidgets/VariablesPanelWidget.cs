// Copyright Gradientspace Corp. All Rights Reserved.
using g3;
using Gradientspace.NodeGraph;
using Gradientspace.UI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GSNodeEditor
{
    public class VariablesPanelWidget : GSUITableLayout
    {
        public UtilityButton AddVariableWidget;
        public WidgetRelativeBoxAnchor AddVariableWidgetAnchor;
        public UtilityButton RemoveVariableWidget;
        public RelativeBoxAnchor RemoveVariableWidgetAnchor;

        const int FieldWidth = 65;
        public string VariablePrefix = "Arg";

        public VariablesPanelWidget(string LabelName = "Variables") : base()
        {
            SetDimensions(1, 2);

            // create header row - just use a text field + spacer for now (currently cannot span columns...)
            TextEntryField variablesField = new TextEntryField();
            variablesField.Width = FieldWidth;
            variablesField.Text = LabelName;
            variablesField.IsEditable = false;
            SetWidget(0, 0, variablesField);
            SetWidget(0, 1, new SpacerBlock(new Vector2f(FieldWidth, variablesField.Height)));

            const float ButtonDim = 13;
            const float ButtonEdgeInset = -2.5f*ButtonDim;
            const float ButtonSpace = 1;

            AddVariableWidget = new UtilityButton(UtilityButton.ButtonTypes.Plus) { Dimensions = new Vector2f(ButtonDim) };
            AddVariableWidgetAnchor = new WidgetRelativeBoxAnchor(this) {
                BoxPoint = BoxPoints.BottomRight, Offset = new Vector2f(ButtonEdgeInset, 0)
            };
            AddVariableWidget.AnchorTo(AddVariableWidgetAnchor);
            AddVariableWidget.AnchorPlacement = new AnchorLocation(BoxPoints.TopLeft);
            AddVariableWidget.OnClicked += AddVariableWidget_OnClicked;
            AddChildWidget(AddVariableWidget);

            RemoveVariableWidget = new UtilityButton(UtilityButton.ButtonTypes.Minus) { Dimensions = new Vector2f(ButtonDim) };
            RemoveVariableWidgetAnchor = new RelativeBoxAnchor(AddVariableWidgetAnchor) {
                Box = new AxisAlignedBox2f(AddVariableWidget.Dimensions.x, AddVariableWidget.Dimensions.y),
                BoxPoint = BoxPoints.TopRight, Offset = new Vector2f(ButtonSpace, 0)
            };
            RemoveVariableWidget.AnchorTo(RemoveVariableWidgetAnchor);
            RemoveVariableWidget.AnchorPlacement = new AnchorLocation(BoxPoints.TopLeft);
            RemoveVariableWidget.OnClicked += RemoveVariableWidget_OnClicked;
            AddChildWidget(RemoveVariableWidget);
        }

        public int NumVariables { get { return Rows-1; } }

        private void RemoveVariableWidget_OnClicked(Button button)
        {
            if (NumVariables > 0)
                SetNumVariables(NumVariables-1);
        }

        private void AddVariableWidget_OnClicked(Button button)
        {
            SetNumVariables(NumVariables+1);
        }

        int CurrentNumVariables = 0;

        public void SetNumVariables(int newNumVariables)
        {
            if (CurrentNumVariables == newNumVariables)
                return;
            SetDimensions(newNumVariables+1, 2);

            // initialize new rows if any were created
            if (newNumVariables > CurrentNumVariables) {
                for ( int i = CurrentNumVariables; i < newNumVariables; ++i ) {

                    TextEntryField nameField = new TextEntryField();
                    nameField.Width = FieldWidth;
                    nameField.Text = $"{VariablePrefix}{i}";
                    SetWidget(i+1, 0, nameField);
                    nameField.OnTextModified += NameField_OnTextModified;

                    TypeComboBox typeCombo = new TypeComboBox();
                    typeCombo.Width = FieldWidth;
                    SetWidget(i+1, 1, typeCombo);
                    typeCombo.OnSelectedTypeChanged += TypeCombo_OnSelectedTypeChanged;
                }
            }
            Debug.Assert(newNumVariables == Rows-1);
            if (CurrentNumVariables != newNumVariables) {
                CurrentNumVariables = newNumVariables;
                OnVariablesChanged?.Invoke(this);
            }
        }


        public delegate void VariablesModifiedEventHandler(VariablesPanelWidget sender);
        public event VariablesModifiedEventHandler? OnVariablesChanged;

        private void NameField_OnTextModified(TextEntryField sender, string oldText, string newText)
        {
            (bool found, int row, int col) = FindWidget(sender);
            if (found && (GetWidget(row, 1) is TypeComboBox typePicker))
                OnVariablesChanged?.Invoke(this);
        }

        private void TypeCombo_OnSelectedTypeChanged(TypeComboBox sender, Type newType)
        {
            (bool found, int row, int col) = FindWidget(sender);
            if (found && (GetWidget(row, 0) is TextEntryField textEntry))
                OnVariablesChanged?.Invoke(this);
        }


        public List<FunctionDefinitionNode.FunctionArg> GetVariables()
        {
            List<FunctionDefinitionNode.FunctionArg> args = new List<FunctionDefinitionNode.FunctionArg>();
            for ( int r = 0; r < NumVariables; ++r ) {
                TextEntryField? textEntry = GetWidget(r+1, 0) as TextEntryField;
                TypeComboBox? typeCombo = GetWidget(r+1, 1) as TypeComboBox;
                if ( textEntry != null && typeCombo != null)
                    args.Add(new() { ArgName = textEntry.Text, ArgType = typeCombo.SelectedType, DefaultValue = null });
            }
            return args;
        }


        public void SetVariables( IEnumerable<FunctionDefinitionNode.FunctionArg> FromArgs )
        {
            List<FunctionDefinitionNode.FunctionArg> args = FromArgs.ToList();
            SetNumVariables(args.Count);
            for (int r = 0; r < NumVariables; ++r) {
                TextEntryField? textEntry = GetWidget(r+1, 0) as TextEntryField;
                if (textEntry != null)
                    textEntry.Text = args[r].ArgName;

                TypeComboBox? typeCombo = GetWidget(r+1, 1) as TypeComboBox;
                if (typeCombo != null)
                    typeCombo.SelectedType = args[r].ArgType;

                // default value??
            }
        }


        public override IWidgetView CreateDefaultView()
        {
            return new VariablesPanelView(this);
        }

    }


    public class VariablesPanelView : GSUITableLayoutView
    {
        public VariablesPanelWidget Variables;

        public VariablesPanelView(VariablesPanelWidget widget) : base(widget)
        {
            Variables = widget;
        }

        public override void UpdateLayout(SKStyleCache StyleCache)
        {
            base.UpdateLayout(StyleCache);

            Variables.AddVariableWidgetAnchor.UpdateFromParentWidget();
            Variables.AddVariableWidget.GetActiveView()?.UpdateLayout(StyleCache);
            Variables.RemoveVariableWidget.GetActiveView()?.UpdateLayout(StyleCache);
        }
    }

}
