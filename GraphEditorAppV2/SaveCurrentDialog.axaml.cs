// Copyright Gradientspace Corp. All Rights Reserved.
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml.MarkupExtensions;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GraphEditorAppV2
{
    partial class SaveCurrentDialog : Window
    {
        public enum ESelectedOptions
        {
            Save, SaveAs, DontSave, Cancel, NoSelection
        }
        public ESelectedOptions Selected { get; set; } = ESelectedOptions.NoSelection;

        KeyBinding escapeBinding = new KeyBinding();
        KeyBinding leftKey = new KeyBinding();
        KeyBinding rightKey = new KeyBinding();
        KeyBinding dontSaveKey = new KeyBinding();
        KeyBinding saveKey = new KeyBinding();
        KeyBinding saveAsKey = new KeyBinding();

        public SaveCurrentDialog()
        {
            InitializeComponent();

            // todo should make some kind of helper class that simplifies this...

            escapeBinding.Gesture = new KeyGesture(Key.Escape);
            escapeBinding.Command = new ActionCommand(
                (o) => { return true; },
                (o) => { this.Close(); });
            KeyBindings.Add(escapeBinding);

            leftKey.Gesture = new KeyGesture(Key.Left);
            leftKey.Command = new ActionCommand(
                (o) => { return true; },
                (o) => { this.SelectPrev(); });
            KeyBindings.Add(leftKey);

            rightKey.Gesture = new KeyGesture(Key.Right);
            rightKey.Command = new ActionCommand(
                (o) => { return true; },
                (o) => { this.SelectNext(); } );
            KeyBindings.Add(rightKey);

            dontSaveKey.Gesture = new KeyGesture(Key.D, KeyModifiers.Shift);
            dontSaveKey.Command = new ActionCommand(
                (o) => { return true; },
                (o) => { this.DontSave_OnClick(null, new()); });
            KeyBindings.Add(dontSaveKey);

            saveKey.Gesture = new KeyGesture(Key.S, KeyModifiers.Shift);
            saveKey.Command = new ActionCommand(
                (o) => { return true; },
                (o) => { this.SaveOrSaveAs(); });
            KeyBindings.Add(saveKey);

            saveAsKey.Gesture = new KeyGesture(Key.A, KeyModifiers.Shift);
            saveAsKey.Command = new ActionCommand(
                (o) => { return true; },
                (o) => { this.SaveAs_OnClick(null, new()); });
            KeyBindings.Add(saveAsKey);

            this.Loaded += SaveCurrentDialog_Loaded;
        }

        private void SaveCurrentDialog_Loaded(object? sender, RoutedEventArgs e)
        {
            if (SaveButton.IsVisible)
                SaveButton.Focus(NavigationMethod.Directional);
            else
                SaveAsButton.Focus(NavigationMethod.Directional);
        }

        protected override void OnClosing(WindowClosingEventArgs e)
        {
            KeyBindings.Remove(escapeBinding);
            KeyBindings.Remove(rightKey);
            KeyBindings.Remove(leftKey);
            KeyBindings.Remove(dontSaveKey);
            KeyBindings.Remove(saveKey);

            if (Selected == ESelectedOptions.NoSelection)
                Selected = ESelectedOptions.Cancel;
            base.OnClosing(e);
        }

        protected void SelectNext()
        {
            if (SaveButton.IsFocused)
                SaveAsButton.Focus(NavigationMethod.Directional);
            else if (SaveAsButton.IsFocused)
                DontSaveButton.Focus(NavigationMethod.Directional);
            else if (DontSaveButton.IsFocused)
                CancelButton.Focus(NavigationMethod.Directional);
            else if (CancelButton.IsFocused)
                (SaveButton.IsVisible ? SaveButton : SaveAsButton).Focus(NavigationMethod.Directional);
            else
                (SaveButton.IsVisible ? SaveButton : SaveAsButton).Focus(NavigationMethod.Directional);
        }
        protected void SelectPrev()
        {
            if (SaveButton.IsFocused)
                CancelButton.Focus(NavigationMethod.Directional);
            else if (SaveAsButton.IsFocused)
                (SaveButton.IsVisible ? SaveButton : CancelButton).Focus(NavigationMethod.Directional);
            else if (DontSaveButton.IsFocused)
                SaveAsButton.Focus(NavigationMethod.Directional);
            else if (CancelButton.IsFocused)
                DontSaveButton.Focus(NavigationMethod.Directional);
            else
                CancelButton.Focus(NavigationMethod.Directional);
        }

        public void HideSaveButton()
        {
            SaveButton.IsVisible = false;
        }

        private void Save_OnClick(object? sender, RoutedEventArgs e)
        {
            Selected = ESelectedOptions.Save;
            this.Close();
        }
        private void SaveAs_OnClick(object? sender, RoutedEventArgs e)
        {
            Selected = ESelectedOptions.SaveAs;
            this.Close();
        }
        private void DontSave_OnClick(object? sender, RoutedEventArgs e)
        {
            Selected = ESelectedOptions.DontSave;
            this.Close();
        }
        private void Cancel_OnClick(object? sender, RoutedEventArgs e)
        {
            Selected = ESelectedOptions.Cancel;
            this.Close();
        }
        private void SaveOrSaveAs()
        {
            if (SaveButton.IsVisible)
                Save_OnClick(null, new());
            else
                SaveAs_OnClick(null, new());
        }
    }

}
