// Copyright Gradientspace Corp. All Rights Reserved.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gradientspace.UI
{

    public class BasicWidgetInputBehavior : InputBehavior
    {
        public Widget SourceWidget { get; set; }
        public ISimpleCaptureTarget? CaptureTarget { get; set; }
        public bool EnableCapture { get; set; } = true;
        public bool EnableHover { get; set; } = true;

        public Func<InputDeviceState, bool>? CapturePredicateFunc = null;

        protected InputDeviceState LastState;

        public bool Capturing { get; protected set; } = false;
        public bool Hovering { get; protected set; } = false;

        public BasicWidgetInputBehavior(Widget sourceWidget) {  SourceWidget = sourceWidget; }
        public BasicWidgetInputBehavior(Widget sourceWidget, ISimpleCaptureTarget captureTarget) { SourceWidget = sourceWidget; CaptureTarget = captureTarget; }


        public override InputCaptureRequest CheckForCapture(in InputDeviceState deviceState)
        {
            if (!EnableCapture) return InputCaptureRequest.None;
            if (deviceState.IsLeftButtonPress == false)
                return InputCaptureRequest.None;

            if (CapturePredicateFunc != null && CapturePredicateFunc(deviceState) == false)
                return InputCaptureRequest.None;

            bool bHit = SourceWidget.GetActiveView()?.HitTest(deviceState.CurrentPosition) ?? false;
			if (!bHit)
				return InputCaptureRequest.None;

			int RenderDepth = SourceWidget.GetActiveView()?.LastDrawOrderIndex ?? 0;
            return new InputCaptureRequest(SourceWidget, this, RenderDepth);
        }
        public override void BeginCapture(in InputDeviceState deviceState, in InputCaptureRequest fromRequest)
        {
            LastState = deviceState;
            Capturing = true;
            CaptureTarget?.UpdateCapture(ISimpleCaptureTarget.ECaptureState.Begin, deviceState);
        }
        public override void UpdateCapture(in InputDeviceState deviceState)
        {
            LastState = deviceState;
            CaptureTarget?.UpdateCapture(ISimpleCaptureTarget.ECaptureState.Update, deviceState);
        }
        public override void EndCapture(in InputDeviceState deviceState)
        {
            LastState = deviceState;
            CaptureTarget?.UpdateCapture(ISimpleCaptureTarget.ECaptureState.End, deviceState);
            Capturing = false;
        }
        public override void AbortCapture()
        {
            CaptureTarget?.UpdateCapture(ISimpleCaptureTarget.ECaptureState.Abort, LastState);
            Capturing = false;
        }


        public override InputCaptureRequest CheckForHover(in InputDeviceState deviceState)
        {
            if (!EnableHover) return InputCaptureRequest.None;
            bool bHit = SourceWidget.GetActiveView()?.HitTest(deviceState.CurrentPosition) ?? false;
            if (!bHit)
                return InputCaptureRequest.None;

			int RenderDepth = SourceWidget.GetActiveView()?.LastDrawOrderIndex ?? 0;
            return new InputCaptureRequest(SourceWidget, this, RenderDepth);
        }
        public override void BeginHover(in InputDeviceState deviceState)
        {
            LastState = deviceState;
            Hovering = true;
            CaptureTarget?.UpdateHover(ISimpleCaptureTarget.EHoverState.Begin, deviceState, out _);
        }
        public override void UpdateHover(in InputDeviceState deviceState, out bool bContinueHover)
        {
            LastState = deviceState;
            bContinueHover = true;
            CaptureTarget?.UpdateHover(ISimpleCaptureTarget.EHoverState.Update, deviceState, out bContinueHover);
        }
        public override void EndHover(in InputDeviceState deviceState)
        {
            LastState = deviceState;
            CaptureTarget?.UpdateHover(ISimpleCaptureTarget.EHoverState.End, deviceState, out _);
            Hovering = false;
        }

    }


    /**
     * ExtendableWidgetInputBehavior can forward capture request and begin/update/end to an external
     * IExtendedInputBehavior implementation. The base ISimpleCaptureTarget must request a capture,
     * the extension can only mutate that capture request.
     * 
     * Hover behavior currently cannot be extended...
     */
    public class ExtendableWidgetInputBehavior : BasicWidgetInputBehavior
    {
        public IExtendedInputBehavior? ExtendedBehavior { get; set; } = null;

        public ExtendableWidgetInputBehavior(Widget sourceWidget, ISimpleCaptureTarget captureTarget)
            : base(sourceWidget, captureTarget)
        {
        }
        public ExtendableWidgetInputBehavior(Widget sourceWidget, ISimpleCaptureTarget captureTarget, IExtendedInputBehavior captureBehavior)
            : base(sourceWidget, captureTarget)
        {
            ExtendedBehavior = captureBehavior;
        }

        public override InputCaptureRequest CheckForCapture(in InputDeviceState deviceState)
        {
            InputCaptureRequest baseRequest = base.CheckForCapture(deviceState);
            if ( baseRequest != InputCaptureRequest.None && ExtendedBehavior != null)
            { 
                InputCaptureRequest ModifiedRequest = ExtendedBehavior.CheckForCapture(deviceState, baseRequest);
                return ModifiedRequest;
            }
            return baseRequest;
        }
        public override void BeginCapture(in InputDeviceState deviceState, in InputCaptureRequest fromRequest)
        {
            if ( ExtendedBehavior != null )
            {
                LastState = deviceState;
                Capturing = true;
                ExtendedBehavior.BeginCapture(deviceState, fromRequest);
            }
            else
                base.BeginCapture(deviceState, fromRequest);
        }
        public override void UpdateCapture(in InputDeviceState deviceState)
        {
            if (ExtendedBehavior != null)
            {
                LastState = deviceState;
                ExtendedBehavior.UpdateCapture(deviceState);
            }
            else
                base.UpdateCapture(deviceState);
        }
        public override void EndCapture(in InputDeviceState deviceState)
        {
            if (ExtendedBehavior != null)
            {
                LastState = deviceState;
                ExtendedBehavior.EndCapture(deviceState);
                Capturing = false;
            }
            else
                base.UpdateCapture(deviceState);
        }
        public override void AbortCapture()
        {
            if (ExtendedBehavior != null)
            {
                ExtendedBehavior.AbortCapture();
                Capturing = false;
            }
            else
                base.AbortCapture();
        }
    }


}
