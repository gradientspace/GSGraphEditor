using Gradientspace.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GSNodeEditor
{
    public interface IContextMenuProviderTarget
    {
        void RequestShowContextMenu(in InputDeviceState deviceState);
    }

    // todo generalize? not really specific to graph view
    public class GraphViewContextMenuBehavior : InputBehavior
    {
        public IContextMenuProviderTarget Target;
        public GraphViewContextMenuBehavior(IContextMenuProviderTarget target)
        {
            Target = target;
        }

        public override InputCaptureRequest CheckForCapture(in InputDeviceState deviceState)
        {
            // could hit-test something here
            if (deviceState.IsRightButtonPress)
                return new InputCaptureRequest(this, this, 9999);
            return InputCaptureRequest.None;
        }
        public override void BeginCapture(in InputDeviceState deviceState, in InputCaptureRequest fromRequest) { }
        public override void UpdateCapture(in InputDeviceState deviceState) { }
        public override void EndCapture(in InputDeviceState deviceState) {
            Target.RequestShowContextMenu(deviceState);
        }
        public override void AbortCapture()
        {
        }

        public override InputCaptureRequest CheckForHover(in InputDeviceState deviceState)
        {
            return InputCaptureRequest.None;
        }
        public override void BeginHover(in InputDeviceState deviceState)
        {
            throw new NotImplementedException();
        }
        public override void EndHover(in InputDeviceState deviceState)
        {
            throw new NotImplementedException();
        }
        public override void UpdateHover(in InputDeviceState deviceState, out bool bContinueHover)
        {
            throw new NotImplementedException();
        }
    }
}
