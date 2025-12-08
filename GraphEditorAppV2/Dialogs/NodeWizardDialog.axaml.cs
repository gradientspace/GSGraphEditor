// Copyright Gradientspace Corp. All Rights Reserved.
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Threading;
using GSNodeEditor;
using SkiaSharp;
using System;
using System.Collections.Generic;
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

            ResultCode.IsVisible = false;
            CreateButton.IsEnabled = false;
            EditButton.IsEnabled = false;

            GenerationFeedback.IsVisible = false;
            GenerationErrors.IsVisible = false;

            TickTimer = new DispatcherTimer();
            TickTimer.Interval = TimeSpan.FromSeconds(0.5f);
            TickTimer.Tick += Timer_Tick;
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
        }
        private void Create_OnClick(object? sender, RoutedEventArgs e)
        {
            if (CurWizard != null) {
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

            ResultCode.Text = "";
            CreateButton.IsEnabled = false;

            NodeWizard Wiz = new NodeWizard();
            Wiz.NodeFunctionPrompt = this.PromptText.Text;

            await Wiz.RunCodeGeneration();

            ResultCode.IsVisible = true;
            ResultCode.Text = Wiz.GeneratedCode;
            CreateButton.IsEnabled = true;
            CurWizard = Wiz;

            GenerationFeedback.Text = "Done!";
            GenerationFeedback.IsVisible = false;
            TickTimer.Stop();
        }


        private void CloseDialog()
        {
            NodeWizardDialog.LastPrompt = this.PromptText.Text;
            this.Close();
        }
    }

}
