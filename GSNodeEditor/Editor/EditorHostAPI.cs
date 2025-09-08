// Copyright Gradientspace Corp. All Rights Reserved.
using Gradientspace.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GSNodeEditor
{
    public interface EditorHostAPI
    {
        void RequestRepaint();

        void SetWindowTitle( string NewTitle );

        //! Filter should be formatted like so: "Text Files (*.txt)|*.txt|All files (*.*)|*.*";
        //! Extension should not not include a dot: "txt"
        bool ShowBlockingSaveAsDialog(
            string InitialFilename,
            string Extension,
            string Filter,
            string? InitialFolder,
            out string SelectedFilename);

        //! Filter should be formatted like so: "Text Files (*.txt)|*.txt|All files (*.*)|*.*";
        //! Extension should not not include a dot: "txt"
        bool ShowBlockingOpenFileDialog(
            string Extension,
            string Filter,
            string? InitialFilename,
            string? InitialFolder,
            out string SelectedFilename);

        void SetSystemClipboardText(string NewText);
        string? GetSystemClipboardText();

    }




    public class EditorClipboardTextAccess : IClipboardTextAccess
    {
        public EditorHostAPI? HostAPI;

        // IClipboardTextAccess API

        public virtual bool GetClipboardText(out string text)
        {
            string? FoundText = HostAPI?.GetSystemClipboardText() ?? null;
            if (FoundText != null && FoundText.Length > 0) {
                text = FoundText;
                return true;
            }
            text = "";
            return false;
        }
        public virtual void SetClipboardText(string newText)
        {
            HostAPI?.SetSystemClipboardText(newText);
        }
    }

}
