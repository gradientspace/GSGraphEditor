using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gradientspace.UI
{
    public interface IPanViewTarget
    {
        void BeginPan(in InputDeviceState initialPosition);
        void UpdatePan(in InputDeviceState newPosition);
        void EndPan(in InputDeviceState finalPosition);
    }


    public class PanViewInputBehavior : ClickOrDragInputBehavior
    {
        protected IPanViewTarget PanTarget;

        public PanViewInputBehavior(IPanViewTarget panTarget) : base()
        { 
            PanTarget = panTarget;
            Depth = -9999;
        }

        public override bool WantClickOrDragCapture(in InputDeviceState deviceState, out float CaptureDepth)
        {
            CaptureDepth = Depth;
            return deviceState.IsRightButtonPress;
        }


        public override void OnBeginDrag(in InputDeviceState deviceState)
        {
            base.OnBeginDrag(deviceState);
            PanTarget.BeginPan(deviceState);
        }
        public override void OnUpdateDrag(in InputDeviceState deviceState)
        {
            base.OnUpdateDrag(deviceState);
            PanTarget.UpdatePan(deviceState);
        }
        public override void OnEndDrag(in InputDeviceState deviceState)
        {
            base.OnEndDrag(deviceState);
            PanTarget.EndPan(deviceState);
        }

        public override void OnClicked(in InputDeviceState deviceState)
        {
            base.OnClicked(deviceState);
        }

    }
}
