// Copyright Gradientspace Corp. All Rights Reserved.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Vanara.PInvoke;
using static Vanara.PInvoke.User32;
using static Vanara.PInvoke.Kernel32;
using static Vanara.PInvoke.Gdi32;
using static Vanara.PInvoke.ComDlg32;
using Vanara.InteropServices;
using System.Runtime.InteropServices;
using Vanara.Extensions;



namespace PopupWindow
{
    public class Win32EditorHostAPI : GSNodeEditor.EditorHostAPI
    {
        SafeHWND mainWindowHWND;

        public Win32EditorHostAPI(SafeHWND parentHWND)
        {
            mainWindowHWND = parentHWND;
        }

        public void RequestRepaint()
        {
            InvalidateRgn(mainWindowHWND, HRGN.NULL, false);      // repaint entire window
        }

        public void SetWindowTitle(string NewTitle)
        {
            SetWindowText(mainWindowHWND, NewTitle);
        }


        public bool ShowBlockingSaveAsDialog(string InitialFilename, string Extension, string Filter, string? InitialFolder, out string SelectedFilename)
        {
            SelectedFilename = "";

            // extension should not start with .
            if (Extension.StartsWith("."))
                Extension = Extension.Substring(1);

            // filter should use null as separator, and be terminated with two nulls
            Filter = Filter.Replace("|", "\0") + "\0\0";

            if (InitialFolder == null)
                InitialFolder = Directory.GetCurrentDirectory();

            nint FilenameBuf = StringHelper.AllocChars(1024, CharSet.Auto);
            if (InitialFilename != null)
                StringHelper.Write(InitialFilename, FilenameBuf, out int ByteCount);

            OPENFILENAME ofn = new OPENFILENAME();
            ofn.hwndOwner = mainWindowHWND;
            ofn.lpstrFile = FilenameBuf;
            ofn.nMaxFile = 1024;
            ofn.lpstrFilter = Filter; // "Text Files (*.txt)\0*.txt\0All Files (*.*)\0*.*\0";
            ofn.lpstrDefExt = new StrPtrAuto(Extension);
            if (InitialFolder != null && Directory.Exists(InitialFolder))
                ofn.lpstrInitialDir = new StrPtrAuto(InitialFolder);
            ofn.Flags = OFN.OFN_EXPLORER | OFN.OFN_HIDEREADONLY;
            ofn.lStructSize = (uint)Marshal.SizeOf<OPENFILENAME>();

            bool bSelected = GetSaveFileName(ref ofn);
            //ERR err = CommDlgExtendedError();
            //Win32Error errorAfter = GetLastError();

            if (bSelected)
                SelectedFilename = StringHelper.GetString(FilenameBuf) ?? "";
            StringHelper.FreeString(FilenameBuf);

            return bSelected && SelectedFilename.Length > 0;
        }

        public bool ShowBlockingOpenFileDialog(string Extension, string Filter, string? InitialFilename, string? InitialFolder, out string SelectedFilename)
        {
            SelectedFilename = "";

            // extension should not start with .
            if (Extension.StartsWith("."))
                Extension = Extension.Substring(1);

            // filter should use null as separator, and be terminated with two nulls
            Filter = Filter.Replace("|", "\0") + "\0\0";

            if (InitialFolder == null)
                InitialFolder = Directory.GetCurrentDirectory();

            nint FilenameBuf = StringHelper.AllocChars(1024, CharSet.Auto);
            if (InitialFilename != null)
                StringHelper.Write(InitialFilename, FilenameBuf, out int ByteCount);

            OPENFILENAME ofn = new OPENFILENAME();
            ofn.hwndOwner = mainWindowHWND;
            ofn.lpstrFile = FilenameBuf;
            ofn.nMaxFile = 1024;
            ofn.lpstrFilter = Filter; // "Text Files (*.txt)\0*.txt\0All Files (*.*)\0*.*\0";
            ofn.lpstrDefExt = new StrPtrAuto(Extension);
            if (InitialFolder != null && Directory.Exists(InitialFolder))
                ofn.lpstrInitialDir = new StrPtrAuto(InitialFolder);
            ofn.Flags = OFN.OFN_EXPLORER | OFN.OFN_FILEMUSTEXIST | OFN.OFN_HIDEREADONLY;
            ofn.lStructSize = (uint)Marshal.SizeOf<OPENFILENAME>();
            
            bool bSelected = GetOpenFileName(ref ofn);
            //ERR err = CommDlgExtendedError();
            //Win32Error errorAfter = GetLastError();

            if (bSelected)
                SelectedFilename = StringHelper.GetString(FilenameBuf) ?? "";
            StringHelper.FreeString(FilenameBuf);

            return bSelected && SelectedFilename.Length > 0;
        }

        public virtual void SetSystemClipboardText(string NewText)
        {
            Win32WindowUtils.SetClipboardText(NewText);
        }

        public virtual string? GetSystemClipboardText()
        {
            return Win32WindowUtils.GetClipboardText();
        }

        //public SoundHandle InitSound(string assetPath)
        //{
        //    return AudioPlayer.TryLoadSoundFile(assetPath);
        //}

        //public void PlaySound(SoundHandle audioFileHandle)
        //{
        //    AudioPlayer.QueueSound(audioFileHandle);
        //}

    }
}
