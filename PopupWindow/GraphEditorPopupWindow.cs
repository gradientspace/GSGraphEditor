using System;
using System.Runtime.InteropServices;

using Vanara.PInvoke;
using static Vanara.PInvoke.User32;
using static Vanara.PInvoke.Kernel32;
using static Vanara.PInvoke.Gdi32;

using GSNodeEditor;
using Gradientspace.UI;
using SkiaSharp;
using g3;
using System.Diagnostics;


namespace PopupWindow
{
    public class GraphEditorPopupWindow
    {
        protected ViewBuffer ViewportBuffer;
        protected NodeGraphViewport GraphView;

        protected SafeHINSTANCE CurInstanceHandle;

        protected Win32EditorHostAPI HostAPI;

        public bool Launch()
        {
            ViewportBuffer = new ViewBuffer(1, 1);

            GraphView = new NodeGraphViewport();
            GraphView.Initialize();

            // todo this should launch background thread...
            bool bWindowOK = CreateWindow();

            return bWindowOK;
        }


        public bool CreateWindow()
        {
            string WindowClassName = "GraphEditorHostWindow";

            // TODO make better
            // enable DPI awareness. If this is not done, the application is rendered as 96 dpi and bitmap-scaled
            // to the current monitor DPI. *However* doing it this way changes the entire application, so if this
            // DLL is loaded in another app, it will affect that one. 
            // https://learn.microsoft.com/en-us/windows/win32/hidpi/setting-the-default-dpi-awareness-for-a-process
            // https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setprocessdpiawarenesscontext
            // DPI awareness can be modified on the fly so perhaps we could just wrap these functions...
            // https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setthreaddpiawarenesscontext
            // https://learn.microsoft.com/en-us/windows/win32/hidpi/high-dpi-improvements-for-desktop-applications
            //
            // (Note that polyscope/glfw currently sets the process to DPI-aware, so that would have to be fixed)
            SetProcessDpiAwarenessContext(DPI_AWARENESS_CONTEXT.DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2);

            // TODO this is wrong, returns handle for .exe not for our .dll, see 
            // eg https://stackoverflow.com/questions/1749972/determine-the-current-hinstance
            // to do correctly requires saving it in DLLMain or using this __ImageBase thing... https://devblogs.microsoft.com/oldnewthing/20041025-00/?p=37483
            // or maybe these: https://web.archive.org/web/20120322043832/http://www.dotnet247.com/247reference/msgs/13/65259.aspx
            // (relevant because it might mess up the launching application...)
            CurInstanceHandle = GetModuleHandle(null);

            // how?
            WindowProc WindowProc = this.MyWindowProc;

            WNDCLASS WindowClass = new WNDCLASS();
            WindowClass.lpfnWndProc = WindowProc;
            WindowClass.hInstance = CurInstanceHandle;
            WindowClass.lpszClassName = WindowClassName;
            WindowClass.hCursor = LoadCursor(HINSTANCE.NULL, IDC_ARROW);
            RegisterClass(WindowClass);


            SafeHWND WindowHandle = CreateWindowEx(
                0,                              // Optional window styles.
                WindowClassName,                     // Window class
                "Derivative Graph Editor",     // Window text
                WindowStyles.WS_OVERLAPPEDWINDOW,// Window style

                // Size and position
                CW_USEDEFAULT, CW_USEDEFAULT, CW_USEDEFAULT, CW_USEDEFAULT,

                HWND.NULL,       // Parent window    
                HMENU.NULL,       // Menu
                CurInstanceHandle,  // Instance handle
                nint.Zero        // Additional application data
                );

            if (WindowHandle.IsNull)
            {
                return false;
            }

            HostAPI = new Win32EditorHostAPI(WindowHandle);
            GraphView.SetActiveHostAPI(HostAPI);

            SystemKeyboardRouter.Instance.OnTextCopied += OnKeyboardRouterEmittedCopiedText;

            // set a 25ms timer we will use to animate things
            SetTimer(WindowHandle, 37, 25, null);

            ShowWindow(WindowHandle, ShowWindowCommand.SW_SHOW);

            MSG NextMessage = new MSG();
            while (true)
            {
                int Result = GetMessage(out NextMessage, WindowHandle, 0, 0);
                if (Result <= 0)
                    break;

                TranslateMessage(NextMessage);
                DispatchMessage(NextMessage);
            }

            GraphView.Shutdown();
            //Polyscope.Unshow();
            //Polyscope.FrameTick();

            return true;
        }

        private void OnKeyboardRouterEmittedCopiedText(KeyboardRouter sender, string NewCopiedText)
        {
            if (NewCopiedText.Length > 0)
                Win32WindowUtils.SetClipboardText(NewCopiedText);
        }

        InputDeviceState RawDeviceState;


        Vector2f get_mouse_position(nint lParam)
        {
            short x = (short)((uint)lParam & 0xFFFF);
            short y = (short)(((uint)lParam & 0xFFFF0000) >> 16);
            return new Vector2f((float)x, (float)y);
        }
        void updatestate_button_pressed(ref InputButtonState WhichButton, nint wParam, nint lParam)
        {
            RawDeviceState.CurrentPosition = get_mouse_position(lParam);
            WhichButton.SetPressed();
            RawDeviceState.CtrlButton.SetDown(((uint)wParam & 0x0008) != 0);
            RawDeviceState.ShiftButton.SetDown(((uint)wParam & 0x0004) != 0);
        }
        void updatestate_button_released(ref InputButtonState WhichButton, nint wParam, nint lParam)
        {
            RawDeviceState.CurrentPosition = get_mouse_position(lParam);
            WhichButton.SetReleased();
            RawDeviceState.CtrlButton.SetDown(((uint)wParam & 0x0008) != 0);
            RawDeviceState.ShiftButton.SetDown(((uint)wParam & 0x0004) != 0);
        }
        void updatestate_move(nint wParam, nint lParam)
        {
            RawDeviceState.CurrentPosition = get_mouse_position(lParam);

            RawDeviceState.LeftButton.SetDown( ((uint)wParam & 0x0001) != 0 );
            RawDeviceState.MiddleButton.SetDown( ((uint)wParam & 0x0010) != 0 );
            RawDeviceState.RightButton.SetDown( ((uint)wParam & 0x0002) != 0 );

            RawDeviceState.CtrlButton.SetDown(((uint)wParam & 0x0008) != 0);
            RawDeviceState.ShiftButton.SetDown(((uint)wParam & 0x0004) != 0);
        }
        void updatestate_wheel(nint wParam, nint lParam)
        {
            // this seems to be incorrect...will return different coordinates than a mousemove just before/after ?!?!
            //RawDeviceState.CurrentPosition = get_mouse_position(lParam);

            RawDeviceState.LeftButton.SetDown(((uint)wParam & 0x0001) != 0);
            RawDeviceState.MiddleButton.SetDown(((uint)wParam & 0x0010) != 0);
            RawDeviceState.RightButton.SetDown(((uint)wParam & 0x0002) != 0);

            RawDeviceState.CtrlButton.SetDown(((uint)wParam & 0x0008) != 0);
            RawDeviceState.ShiftButton.SetDown(((uint)wParam & 0x0004) != 0);

            short wheel_steps = (short)(wParam >> 16);
            const float WHEEL_STEPS = 120.0f;       // is this really a constant? what about configurable mouse wheel speed?
            RawDeviceState.WheelDelta = (float)wheel_steps / WHEEL_STEPS;
        }

        static public KeyState MakeKeyStateFromVK(VK VirtualKey, InputDeviceState deviceState)
        {
            KeyState keyState = KeyState.Unknown;

            switch (VirtualKey)
            {
                case VK.VK_ESCAPE: keyState = KeyState.Escape; break;
                case VK.VK_RETURN: keyState = KeyState.Enter; break;
                case VK.VK_BACK: keyState = KeyState.Backspace; break;
                case VK.VK_DELETE: keyState = KeyState.Delete; break;
                case VK.VK_TAB: keyState = KeyState.Tab; break;
                case VK.VK_SPACE: keyState = KeyState.Space; break;

                case VK.VK_SHIFT: keyState = KeyState.Shift; break;
                case VK.VK_MENU: keyState = KeyState.Alt; break;
                case VK.VK_CONTROL: keyState = KeyState.Ctrl; break;

                case VK.VK_LEFT: keyState = KeyState.LeftArrow; break;
                case VK.VK_RIGHT: keyState = KeyState.RightArrow; break;
                case VK.VK_UP: keyState = KeyState.UpArrow; break;
                case VK.VK_DOWN: keyState = KeyState.DownArrow; break;

                default:
                    {
                        char Character = (char)MapVirtualKey((uint)VirtualKey, MAPVK.MAPVK_VK_TO_CHAR);
                        if (Char.IsControl(Character) == false)
                        {
                            keyState.KeyType = KeyType.CharacterKey;
                            keyState.KeyName = KeyNames.Unnamed;
                            keyState.Character = Character;
                        }
                    }
                    break;
            }

            keyState.ConfigureModifiers(deviceState);

            return keyState;
        }


        nint MyWindowProc(HWND hwnd, uint msg, nint wParam, nint lParam)
        {
            switch (msg)
            {
                case (uint)WindowMessage.WM_DESTROY:
                    PostQuitMessage(0);
                    return 0;

                // can override cursor here...
                //case (uint)WindowMessage.WM_SETCURSOR:
                //    SetCursor(
                //        LoadCursor(HINSTANCE.NULL, IDC_ARROW));
                //    return 0;

                case (uint)WindowMessage.WM_TIMER:
                    // if we have a text entry target it might have a blinking cursor in which case we need
                    // to animate. Probably should do this on an interval somehow...
                    if (wParam == 37) {     // set above
                        if (SystemKeyboardRouter.Instance.HasTextEntryFocusTarget)
                            InvalidateRgn(hwnd, HRGN.NULL, false);
                    }
                    return 0;


                case (uint)WindowMessage.WM_PAINT:
                    {
                        PAINTSTRUCT ps;
                        HDC hdc = BeginPaint(hwnd, out ps);
                        RECT PaintRect = ps.rcPaint;

                        // always repainting entire window region for now
                        RECT WindowRect;
                        if (GetWindowRect(hwnd, out WindowRect))
                        {
                            PaintRect = WindowRect;
                        }

                        // clear rect...this will cause flickering!
                        //COLORREF BrushColor = new COLORREF(255, 255, 255);
                        //SafeHBRUSH SolidBrush = CreateSolidBrush(BrushColor);
                        //FillRect(hdc, PaintRect, SolidBrush);
                        //DeleteObject(SolidBrush);

                        // todo will this only do partial repaints??
                        ViewportBuffer.UpdateSize(PaintRect.Width, PaintRect.Height);
                        if (ViewportBuffer.IsValid)
                        {
                            GraphView.Repaint(ViewportBuffer.Canvas!);


                            BITMAPINFO bmi = new BITMAPINFO(ViewportBuffer.Width, -ViewportBuffer.Height);

                            // sample from https://stackoverflow.com/questions/30897320/skia-rendering-on-windowsskcanvas-from-hdc
                            // calls bitmap.lockPixels() / bitmap.unlockPixels() but these do not seem to exist in C# API...
                            int BlitResult = SetDIBitsToDevice(hdc,
                                0, 0, (uint)ViewportBuffer.Width, (uint)ViewportBuffer.Height,
                                0, 0,
                                0, (uint)ViewportBuffer.Height,
                                ViewportBuffer.GetRawPixelBufferPointer(),
                                bmi,
                                DIBColorMode.DIB_RGB_COLORS);
                        }

                        EndPaint(hwnd, ps);
                    }
                    return 0;


                case (uint)WindowMessage.WM_LBUTTONDOWN:
                    {
                        SetCapture(hwnd);
                        updatestate_button_pressed(ref RawDeviceState.LeftButton, wParam, lParam);
                        GraphView.OnPointerDown(RawDeviceState);
                        InvalidateRgn(hwnd, HRGN.NULL, false);      // repaint entire window
                    }
                    return 0;

                case (uint)WindowMessage.WM_LBUTTONUP:
                    {
                        updatestate_button_released(ref RawDeviceState.LeftButton, wParam, lParam);
                        GraphView.OnPointerUp(RawDeviceState);
                        ReleaseCapture();
                        InvalidateRgn(hwnd, HRGN.NULL, false);      // repaint entire window
                    }
                    return 0;

                case (uint)WindowMessage.WM_RBUTTONDOWN:
                    {
                        SetCapture(hwnd);
                        updatestate_button_pressed(ref RawDeviceState.RightButton, wParam, lParam);
                        GraphView.OnPointerDown(RawDeviceState);
                        InvalidateRgn(hwnd, HRGN.NULL, false);      // repaint entire window
                    }
                    return 0;

                case (uint)WindowMessage.WM_RBUTTONUP:
                    {
                        updatestate_button_released(ref RawDeviceState.RightButton, wParam, lParam);
                        GraphView.OnPointerUp(RawDeviceState);
                        ReleaseCapture();
                        InvalidateRgn(hwnd, HRGN.NULL, false);      // repaint entire window
                    }
                    return 0;

                case (uint)WindowMessage.WM_MBUTTONDOWN:
                    {
                        SetCapture(hwnd);
                        updatestate_button_pressed(ref RawDeviceState.MiddleButton, wParam, lParam);
                        GraphView.OnPointerDown(RawDeviceState);
                        InvalidateRgn(hwnd, HRGN.NULL, false);      // repaint entire window
                    }
                    return 0;

                case (uint)WindowMessage.WM_MBUTTONUP:
                    {
                        updatestate_button_released(ref RawDeviceState.MiddleButton, wParam, lParam);
                        GraphView.OnPointerUp(RawDeviceState);
                        ReleaseCapture();
                        InvalidateRgn(hwnd, HRGN.NULL, false);      // repaint entire window
                    }
                    return 0;

                case (uint)WindowMessage.WM_MOUSEMOVE:
                    {
                        updatestate_move(wParam, lParam);
                        GraphView.UpdateCursor( RawDeviceState );
                        InvalidateRgn(hwnd, HRGN.NULL, false);      // repaint entire window
                    }
                    return 0;

                case (uint)WindowMessage.WM_MOUSEWHEEL:
                    {
                        updatestate_wheel(wParam, lParam);
                        GraphView.OnWheel(RawDeviceState);
                        RawDeviceState.WheelDelta = 0;
                        InvalidateRgn(hwnd, HRGN.NULL, false);      // repaint entire window
                    }
                    return 0;


                case (uint)WindowMessage.WM_CHAR:
                    {
                        char Character = (char)(uint)wParam;
                        bool bExtended = ((uint)lParam & (1 << 24)) != 0;
                        bool bAltDown = ((uint)lParam & (1 << 29)) != 0;
                        bool bWasDown = ((uint)lParam & (1 << 30)) != 0;
                        bool bIsBeingReleased = ((uint)lParam & (1 << 31)) != 0;
                        //System.Console.WriteLine("WM_CHAR: Key " + Character + "  extended " + bExtended + " alt " + bAltDown + " wasdown " + bWasDown + " releasing " + bIsBeingReleased);

                        KeyState keyState = KeyState.MakeKeyStateFromCharacter(Character, RawDeviceState);
                        if (keyState.IsKnownKey)
                            SystemKeyboardRouter.Instance.OnCharacter(keyState);

                        InvalidateRgn(hwnd, HRGN.NULL, false);      // repaint entire window
                    }
                    return 0;


                case (uint)WindowMessage.WM_KEYDOWN:
                    {
                        VK KeyValue = (VK)wParam;
                        bool bExtended = ((uint)lParam & (1 << 24)) != 0;
                        bool bWasDown = ((uint)lParam & (1 << 30)) != 0;

                        // ignore repeat messages
                        if (!bWasDown)
                        {
                            //System.Console.WriteLine("WM_KEYDOWN: Key " + KeyValue + "  wasdown " + bWasDown);

                            if (KeyValue == VK.VK_CONTROL)
                                RawDeviceState.CtrlButton.SetPressed();
                            else if (KeyValue == VK.VK_SHIFT)
                                RawDeviceState.ShiftButton.SetPressed();
                            else if (KeyValue == VK.VK_MENU)
                                RawDeviceState.AltButton.SetPressed();

                            KeyState keyState = MakeKeyStateFromVK(KeyValue, RawDeviceState);

                            if (keyState.Character == 'V' && RawDeviceState.CtrlButton.bDown)
                            {
                                // handle as ctrl+v hotkey
                                string? PastedText = Win32WindowUtils.GetClipboardText();
                                if (PastedText != null)
                                    SystemKeyboardRouter.Instance.SetCurrentSystemClipboardText(PastedText);
                            }

                            if (keyState.IsKnownKey)
                                SystemKeyboardRouter.Instance.OnRawKeyDown(keyState);

                            InvalidateRgn(hwnd, HRGN.NULL, false);      // repaint entire window
                        }
                    }
                    return 0;

                case (uint)WindowMessage.WM_KEYUP:
                    {
                        VK KeyValue = (VK)wParam;
                        bool bExtended = ((uint)lParam & (1 << 24)) != 0;
                        //System.Console.WriteLine("WM_KEYUP: Key " + KeyValue);

                        if (KeyValue == VK.VK_CONTROL)
                            RawDeviceState.CtrlButton.SetReleased();
                        else if (KeyValue == VK.VK_SHIFT)
                            RawDeviceState.ShiftButton.SetReleased();
                        else if (KeyValue == VK.VK_MENU)
                            RawDeviceState.AltButton.SetReleased();

                        KeyState keyState = MakeKeyStateFromVK(KeyValue, RawDeviceState);
                        if (keyState.IsKnownKey)
                            SystemKeyboardRouter.Instance.OnRawKeyUp(keyState);

                        InvalidateRgn(hwnd, HRGN.NULL, false);      // repaint entire window
                    }
                    return 0;



                case (uint)WindowMessage.WM_SETFOCUS:
                    {
                        GraphView.OnBeginFocus();
                    }
                    return 0;
                case (uint)WindowMessage.WM_KILLFOCUS:
                    {
                        GraphView.OnEndFocus();
                    }
                    return 0;

                case (uint)WindowMessage.WM_SIZE:
                    {
                    }
                    return 0;
                case (uint)WindowMessage.WM_DPICHANGED:
                    {
                        System.Console.WriteLine("DPI changed!");
                    }
                    return 0;
            }

            //WindowMessage WinMsg = (WindowMessage)msg;
            //if ( WinMsg != WindowMessage.WM_SETCURSOR && WinMsg != WindowMessage.WM_NCHITTEST && WinMsg != WindowMessage.WM_NCMOUSEMOVE)
            //    System.Diagnostics.Debug.WriteLine("WINDOW MESSAGE {0}", WinMsg.ToString());

            return DefWindowProc(hwnd, msg, wParam, lParam);
        }



    }





}
