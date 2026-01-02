// Copyright Gradientspace Corp. All Rights Reserved.
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Threading;
using Gradientspace.GenAI;
using Gradientspace.NodeGraph;
using GSNodeEditor;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GraphEditorAppV2
{
    partial class NodeWizardDialog : Window
    {
        KeyBinding escapeBinding = new KeyBinding();
        KeyBinding leftKey = new KeyBinding();
        KeyBinding rightKey = new KeyBinding();

        required public NodeGraphViewport GraphViewport { get; set; }

        NodeWizard? CurWizard = null;
        DispatcherTimer TickTimer = null;
        int AnimatedBulletCount = 0;

        static string LastPrompt = "compute the first N elements of the fibonacci sequence";
        static string LastCode = "";


        List<ModelID> AvailableModels;
        ModelID ActiveModel = ModelID.Invalid;
        internal struct ModelOption
        {
            public ModelID modelID;
            public override string ToString() { return modelID.ModelName; }
        }


        ISourceCodeProvider? CodeTarget = null;

        public NodeWizardDialog()
        {
            InitializeComponent();

            // todo should make some kind of helper class that simplifies this...

            escapeBinding.Gesture = new KeyGesture(Key.Escape);
            escapeBinding.Command = new ActionCommand(
                (o) => { return true; },
                (o) => { this.OnEscape(); });
            KeyBindings.Add(escapeBinding);

            //leftKey.Gesture = new KeyGesture(Key.Left);
            //leftKey.Command = new ActionCommand(
            //    (o) => { return true; },
            //    (o) => { this.SelectPrev(); });
            //KeyBindings.Add(leftKey);

            //rightKey.Gesture = new KeyGesture(Key.Right);
            //rightKey.Command = new ActionCommand(
            //    (o) => { return true; },
            //    (o) => { this.SelectNext(); } );
            //KeyBindings.Add(rightKey);

            this.Loaded += NodeWizardDialog_Loaded;
            this.Closing += NodeWizardDialog_Closing;

            ResultCode.IsVisible = true;
            CreateButton.IsEnabled = false;
            EditButton.IsEnabled = false;

            GenerationFeedback.IsVisible = false;
            GenerationErrors.IsVisible = false;

            TickTimer = new DispatcherTimer();
            TickTimer.Interval = TimeSpan.FromSeconds(0.5f);
            TickTimer.Tick += Timer_Tick;

            // set up model-picker dropdown
            AvailableModels = ModelRegistry.GetAvailableModels( (ModelID m) => { return m.Type.Text == true; } );
            int select_index = (AvailableModels.Count > 0) ? 0 : -1;
            string LastActiveModel = NodeEditorConfig.LastNodeWizardModel;
            foreach ( ModelID model in AvailableModels ) {
                ModelOption option = new ModelOption() { modelID = model };
                ModelSelector.Items.Add(option);
                if (model.IDString == LastActiveModel)
                    select_index = ModelSelector.Items.Count-1;
            }
            if ( select_index >= 0 ) {
                ActiveModel = AvailableModels[select_index];
                ModelSelector.SelectedIndex = select_index;
            }
            ModelSelector.SelectionChanged += ModelSelector_SelectionChanged;
        }


        private void ModelSelector_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count == 0) return;
            if (e.AddedItems[0] is ModelOption newOption) {
                ActiveModel = newOption.modelID;
            }
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            if (GenerationFeedback.IsVisible) {
                AnimatedBulletCount = (AnimatedBulletCount+1)%3;
                GenerationFeedback.Text = "Working" + new string('.', AnimatedBulletCount+1);
            }
        }

        private void NodeWizardDialog_Loaded(object? sender, RoutedEventArgs e)
        {
            PromptText.Text = NodeWizardDialog.LastPrompt;
            ResultCode.Text = NodeWizardDialog.LastCode;
        }

        protected override void OnClosing(WindowClosingEventArgs e)
        {
            KeyBindings.Remove(escapeBinding);
            KeyBindings.Remove(rightKey);
            KeyBindings.Remove(leftKey);

            //if (Selected == ESelectedOptions.NoSelection)
            //    Selected = ESelectedOptions.Cancel;
            base.OnClosing(e);
        }


        private void Generate_OnClick(object? sender, RoutedEventArgs e)
        {
            RunGeneration();
        }
        private void Edit_OnClick(object? sender, RoutedEventArgs e)
        {
            RunEdit();
        }
        private void Create_OnClick(object? sender, RoutedEventArgs e)
        {
            if (CodeTarget != null) {
                UpdateCodeTarget();
                CloseDialog();
            }else if (CurWizard != null) {
                CurWizard.EmitNewCodeNode(GraphViewport);
                CloseDialog();
            }
        }
        private void Cancel_OnClick(object? sender, RoutedEventArgs e)
        {
            //Selected = ESelectedOptions.Cancel;
            CloseDialog();
        }

        private void OnEscape()
        {
            if (PromptText.IsFocused ) {
                GenerateButton.Focus(NavigationMethod.Directional);
            } else {
                CloseDialog();
            }
        }


        private async void RunGeneration() 
        {
            GenerationFeedback.IsVisible = true;
            GenerationFeedback.Text = "Working...";
            AnimatedBulletCount = 0;
            TickTimer.Start();

            CreateButton.IsEnabled = false;

            NodeWizard Wiz = new NodeWizard();
            Wiz.UseModelName = ActiveModel.ModelName;
            Wiz.NodeFunctionPrompt = this.PromptText.Text ?? "";

            await Wiz.RunCodeGeneration();

            ResultCode.IsVisible = true;
            ResultCode.Text = Wiz.GeneratedCode;
            CreateButton.IsEnabled = true;
            EditButton.IsEnabled = true;
            CurWizard = Wiz;

            GenerationFeedback.Text = "Done!";
            //GenerationFeedback.IsVisible = false;
            TickTimer.Stop();
        }


        private async void RunEdit()
        {
            GenerationFeedback.IsVisible = true;
            GenerationFeedback.Text = "Working...";
            AnimatedBulletCount = 0;
            TickTimer.Start();

            CreateButton.IsEnabled = false;

            NodeWizard Wiz = new NodeWizard();
            Wiz.UseModelName = ActiveModel.ModelName;
            Wiz.NodeFunctionPrompt = this.PromptText.Text ?? "";
            Wiz.GeneratedCode = this.ResultCode.Text ?? "";

            await Wiz.RunCodeEdit();

            ResultCode.IsVisible = true;
            ResultCode.Text = Wiz.GeneratedCode;
            CreateButton.IsEnabled = true;
            CurWizard = Wiz;

            GenerationFeedback.Text = "Done!";
            //GenerationFeedback.IsVisible = false;
            TickTimer.Stop();
        }


        private void CloseDialog()
        {
            this.Close();
        }
        private void NodeWizardDialog_Closing(object? sender, WindowClosingEventArgs e)
        {
            NodeWizardDialog.LastPrompt = this.PromptText.Text ?? "";
            NodeWizardDialog.LastCode = this.ResultCode.Text ?? "";

            NodeEditorConfig.LastNodeWizardModel = this.ActiveModel.IDString;
        }

        public void InitializeFromCodeProvider(ISourceCodeProvider provider)
        {
            SourceCodeDataType codeData = provider.GetCurrentSourceCode();
            ResultCode.Text = codeData.CodeText;
            NodeWizardDialog.LastCode = ResultCode.Text;

            CodeTarget = provider;

            CreateButton.Content = "Apply Changes";
            CreateButton.IsEnabled = true;
            EditButton.Content = "Request Edit";
            EditButton.IsEnabled = true;
            GenerateButton.IsVisible = false;
        }
        public void UpdateCodeTarget()
        {
            if (CodeTarget == null) return;
            SourceCodeDataType codeData = CodeTarget.GetCurrentSourceCode().MakeDuplicate();
            codeData.CodeText = ResultCode.Text ?? "";
            CodeTarget.UpdateSourceCode(codeData);
        }


    }

}
