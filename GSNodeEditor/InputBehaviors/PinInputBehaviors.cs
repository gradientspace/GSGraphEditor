using Gradientspace.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GSNodeEditor
{
    public interface IPinCaptureTarget
    {
        void BeginPinCapture(in InputDeviceState deviceState, NodePinWidget pinWidget);
        void UpdatePinCapture(in InputDeviceState deviceState);
        void EndPinCapture(in InputDeviceState deviceState);
        void AbortPinCapture();
    }

    public class PinExtendedBehavior : IExtendedInputBehavior
    {
        NodePinWidget? CurrentPin;
        IPinCaptureTarget PinCaptureTarget;

        public PinExtendedBehavior(IPinCaptureTarget target) { PinCaptureTarget = target; }

        public virtual InputCaptureRequest CheckForCapture(in InputDeviceState deviceState, InputCaptureRequest baseRequest)
        {
            NodePinWidget? pinWidget = baseRequest.SourceObject as NodePinWidget;
            if (pinWidget == null) return InputCaptureRequest.None;
            return baseRequest;
        }

        public virtual void BeginCapture(in InputDeviceState deviceState, in InputCaptureRequest fromRequest)
        {
            CurrentPin = fromRequest.SourceObject as NodePinWidget;
            if (CurrentPin != null)
                PinCaptureTarget.BeginPinCapture(deviceState, CurrentPin);
        }

        public virtual void UpdateCapture(in InputDeviceState deviceState)
        {
            PinCaptureTarget.UpdatePinCapture(deviceState);
        }

        public virtual void EndCapture(in InputDeviceState deviceState)
        {
            PinCaptureTarget.EndPinCapture(deviceState);
        }

        public virtual void AbortCapture()
        {
            PinCaptureTarget.AbortPinCapture();
        }

    }


}
