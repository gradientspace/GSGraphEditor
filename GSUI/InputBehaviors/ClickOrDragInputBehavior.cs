using Gradientspace.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gradientspace.UI
{
    public class ClickOrDragInputBehavior : InputBehavior
    {
        protected InputDeviceState LastState;

        protected InputDeviceState InitialState;
        float ClickMoveThreshold = 2.5f;
        bool bMovedOutOfClickRange = false;

        public bool Capturing { get; private set; } = false;
        public bool Hovering { get; private set; } = false;

        public delegate void DragEvent(in InputDeviceState deviceState);
        public DragEvent? OnBeginDragEvent;
        public DragEvent? OnUpdateDragEvent;
        public DragEvent? OnEndDragEvent;

        public delegate void ClickEvent(in InputDeviceState deviceState);
        public ClickEvent? OnClickedEvent;

        public ClickOrDragInputBehavior()
        {
        }

        public object? CaptureSourceObject { get; set; } = null;

        public virtual bool WantClickOrDragCapture(in InputDeviceState deviceState, out float CaptureDepth)
        {
            CaptureDepth = Depth;
            return deviceState.IsLeftButtonPress;
        }
        public virtual void OnBeginDrag(in InputDeviceState deviceState)
        {
            OnBeginDragEvent?.Invoke(deviceState);
        }
        public virtual void OnUpdateDrag(in InputDeviceState deviceState)
        {
            OnUpdateDragEvent?.Invoke(deviceState);
        }
        public virtual void OnEndDrag(in InputDeviceState deviceState)
        {
            OnEndDragEvent?.Invoke(deviceState);
        }
        public virtual void OnClicked(in InputDeviceState deviceState)
        {
            OnClickedEvent?.Invoke(deviceState);
        }

        // InputBehavior implementation

        public override InputCaptureRequest CheckForCapture(in InputDeviceState deviceState)
        {
            if (WantClickOrDragCapture(deviceState, out float CaptureDepth))
                return new InputCaptureRequest(CaptureSourceObject, this, CaptureDepth);
            return InputCaptureRequest.None;
        }
        public override void BeginCapture(in InputDeviceState deviceState, in InputCaptureRequest fromRequest)
        {
            LastState = deviceState;
            InitialState = deviceState;
            Capturing = true;
            bMovedOutOfClickRange = false;
        }
        public override void UpdateCapture(in InputDeviceState deviceState)
        {
            LastState = deviceState;
            if ( bMovedOutOfClickRange ) {
                OnUpdateDrag(deviceState);
                return;
            }

            double MoveDistSqr = LastState.CurrentPosition.DistanceSquared(InitialState.CurrentPosition);
            if (MoveDistSqr > ClickMoveThreshold*ClickMoveThreshold) {
                bMovedOutOfClickRange = true;
                OnBeginDrag(InitialState);
                OnUpdateDrag(LastState);
            }
        }
        public override void EndCapture(in InputDeviceState deviceState)
        {
            LastState = deviceState;
            if (bMovedOutOfClickRange)
                OnEndDrag(deviceState);
            else
                OnClicked(deviceState);
            Capturing = false;
        }
        public override void AbortCapture()
        {
            if (bMovedOutOfClickRange)
                OnEndDrag(LastState);
            Capturing = false;
        }


        public override InputCaptureRequest CheckForHover(in InputDeviceState deviceState)
        {
            return InputCaptureRequest.None;
        }
        public override void BeginHover(in InputDeviceState deviceState)
        {
            throw new NotImplementedException();
        }
        public override void UpdateHover(in InputDeviceState deviceState, out bool bContinueHover)
        {
            throw new NotImplementedException();
        }
        public override void EndHover(in InputDeviceState deviceState)
        {
            throw new NotImplementedException();
        }

    }
}
