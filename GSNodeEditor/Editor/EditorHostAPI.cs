// Copyright Gradientspace Corp. All Rights Reserved.
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

    }
}
