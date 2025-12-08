// Copyright Gradientspace Corp. All Rights Reserved.
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml.MarkupExtensions;
using GSNodeEditor;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GraphEditorAppV2
{
    partial class NodeLibrariesDialog : Window
    {
        KeyBinding escapeBinding = new KeyBinding();
        KeyBinding leftKey = new KeyBinding();
        KeyBinding rightKey = new KeyBinding();
        KeyBinding dontSaveKey = new KeyBinding();
        KeyBinding saveKey = new KeyBinding();
        KeyBinding saveAsKey = new KeyBinding();

        public NodeLibrariesDialog()
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

            this.Loaded += NodeLibrariesDialog_Loaded;

            this.SearchPaths.LostFocus += (sender, e) => {
                ValidatePaths(false);
            };

            ValidatePaths(false);
        }

        private void NodeLibrariesDialog_Loaded(object? sender, RoutedEventArgs e)
        {
            //if (SaveButton.IsVisible)
            //    SaveButton.Focus(NavigationMethod.Directional);
            string AllStrings = "";
            foreach (string path in NodeEditorConfig.NodeLibraryPaths) {
                AllStrings += path + Environment.NewLine;
            }
            SearchPaths.Text = AllStrings;
        }

        protected override void OnClosing(WindowClosingEventArgs e)
        {
            KeyBindings.Remove(escapeBinding);
            KeyBindings.Remove(rightKey);
            KeyBindings.Remove(leftKey);
            KeyBindings.Remove(dontSaveKey);
            KeyBindings.Remove(saveKey);

            //if (Selected == ESelectedOptions.NoSelection)
            //    Selected = ESelectedOptions.Cancel;
            base.OnClosing(e);
        }

        protected void SelectNext()
        {
            //if (SaveButton.IsFocused)
            //    SaveAsButton.Focus(NavigationMethod.Directional);
            //else if (SaveAsButton.IsFocused)
            //    DontSaveButton.Focus(NavigationMethod.Directional);
            //else if (DontSaveButton.IsFocused)
            //    CancelButton.Focus(NavigationMethod.Directional);
            //else if (CancelButton.IsFocused)
            //    (SaveButton.IsVisible ? SaveButton : SaveAsButton).Focus(NavigationMethod.Directional);
            //else
            //    (SaveButton.IsVisible ? SaveButton : SaveAsButton).Focus(NavigationMethod.Directional);
        }
        protected void SelectPrev()
        {
            //if (SaveButton.IsFocused)
            //    CancelButton.Focus(NavigationMethod.Directional);
            //else if (SaveAsButton.IsFocused)
            //    (SaveButton.IsVisible ? SaveButton : CancelButton).Focus(NavigationMethod.Directional);
            //else if (DontSaveButton.IsFocused)
            //    SaveAsButton.Focus(NavigationMethod.Directional);
            //else if (CancelButton.IsFocused)
            //    DontSaveButton.Focus(NavigationMethod.Directional);
            //else
            //    CancelButton.Focus(NavigationMethod.Directional);
        }


        private void Apply_OnClick(object? sender, RoutedEventArgs e)
        {
            ValidatePaths(true);


            //this.Close();
        }
        private void Done_OnClick(object? sender, RoutedEventArgs e)
        {
            //Selected = ESelectedOptions.Cancel;
            this.Close();
        }

        private void OnEscape()
        {
            if ( SearchPaths.IsFocused ) {
                ApplyButton.Focus(NavigationMethod.Directional);
            } else {
                this.Close();
            }
        }


        private void ValidatePaths(bool bApplyChanges) 
        {
            SearchPathErrors.Text = "";
            SearchPathErrors.IsVisible = false;

            string Errors = "";
            string[] NewPaths = SearchPaths.Text?.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? [];
            for (int i = 0; i < NewPaths.Length; ++i ) {
                string path = NewPaths[i];
                if ( System.IO.Directory.Exists(path) == false ) {
                    Errors += $"Directory {path} does not exist {Environment.NewLine}";
                } 
            }
            if ( Errors.Length > 0 ) {
                SearchPathErrors.Text = Errors;
                SearchPathErrors.IsVisible = true;
            }

            if (bApplyChanges) {
                NodeEditorConfig.NodeLibraryPaths = NewPaths.ToList<string>();
                NodeEditorConfig.SaveConfig();
                (this.Owner as MainWindow)?.TryLoadNewLibrariesAndReinstanceGraph();
            }
        }

    }

}
