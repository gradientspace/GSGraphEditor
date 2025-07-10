// Copyright Gradientspace Corp. All Rights Reserved.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

using Vanara.PInvoke;
using static Vanara.PInvoke.User32;
using static Vanara.PInvoke.Kernel32;
using static Vanara.PInvoke.Gdi32;

namespace PopupWindow
{
    internal class Win32WindowUtils
    {

        public static string? GetClipboardText()
        {
            if (OpenClipboard(HWND.NULL) == false)
                return null;

            string? resultString = null;

            uint CF_TEXT = 1;
            nint hData = GetClipboardData(CF_TEXT);
            if (hData != 0)
            {
                nint hTextPointer = GlobalLock(hData);
                if (hTextPointer != 0)
                {
                    resultString = Marshal.PtrToStringAnsi(hTextPointer);
                }
                GlobalUnlock(hData);
            }

            CloseClipboard();
            return resultString;
        }



        public static bool SetClipboardText(string Text)
        {
            if (OpenClipboard(HWND.NULL) == false)
                return false;

            bool bSet = false;

            // we never free this memory, we are giving it to windows (apparently?)
            nint GlobalStringMemory = Marshal.StringToHGlobalAnsi(Text);
            if (GlobalStringMemory != 0)
            {
                uint CF_TEXT = 1;
                SetClipboardData(CF_TEXT, GlobalStringMemory);
                bSet = true;
            }

            CloseClipboard();
            return bSet;
        }

    }
}
