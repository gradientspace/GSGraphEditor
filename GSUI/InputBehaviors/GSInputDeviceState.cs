// Copyright Gradientspace Corp. All Rights Reserved.
using g3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gradientspace.UI
{
    public struct InputButtonState
    {
        public bool bPressed = false;
        public bool bDown = false;
        public bool bReleased = false;

        public InputButtonState(bool down = false, bool pressed = false, bool released = false) { bDown = down; bPressed = pressed; bReleased = released; }

        public bool IsChanging { get { return bPressed || bReleased; } }

        public void SetPressed() { bPressed = true; bDown = true; bReleased = false; }
        public void SetReleased() { bPressed = false; bDown = false; bReleased = true; }
        public void SetDown(bool down) { bPressed = false; bDown = down; bReleased = false; }

        public static InputButtonState None = new InputButtonState();
    }

    public struct InputDeviceState
    {
        public enum DeviceTypes { Mouse }
        public DeviceTypes DeviceType = DeviceTypes.Mouse;

        public InputButtonState LeftButton = InputButtonState.None;
        public InputButtonState MiddleButton = InputButtonState.None;
        public InputButtonState RightButton = InputButtonState.None;

        public InputButtonState CtrlButton = InputButtonState.None;
        public InputButtonState AltButton = InputButtonState.None;
        public InputButtonState ShiftButton = InputButtonState.None;
        public InputButtonState CommandButton = InputButtonState.None;

        // [TODO] how to do this?
        //public InputButtonState PlatformCtrlButton {
        //    get { return IsOSX ? CommandButton : CtrlButton; }
        //}


        public Vector2f CurrentPosition = Vector2f.Zero;

        public float WheelDelta = 0.0f;

        public int DebugState = 0;

        public InputDeviceState() { }

        public bool IsLeftButtonPress { get { return LeftButton.bPressed == true && MiddleButton.bDown == false && RightButton.bDown == false; } }
        public bool IsRightButtonPress { get { return LeftButton.bDown == false && MiddleButton.bDown == false && RightButton.bPressed == true; } }
        public bool IsMiddleButtonPress { get { return LeftButton.bDown == false && MiddleButton.bDown == true && RightButton.bPressed == false; } }

    }
}
