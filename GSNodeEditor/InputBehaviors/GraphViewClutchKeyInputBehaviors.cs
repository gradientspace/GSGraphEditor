// Copyright Gradientspace Corp. All Rights Reserved.
using Gradientspace.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GSNodeEditor
{
    // this is only doing pin edits now and maybe could just be included in PinInputBehaviors??
    public class GraphViewClutchKeyInputBehaviors : InputBehavior
    {
        WidgetScene WidgetScene { get; }
        //NodeGraphEditor GraphEditor { get; }
        NodeGraphViewport GraphViewport { get; } 

        public GraphViewClutchKeyInputBehaviors(WidgetScene widgetScene, NodeGraphViewport graphViewport)
        {
            WidgetScene = widgetScene;
            //GraphEditor = graphEditor;
            GraphViewport = graphViewport;
        }

        public override InputCaptureRequest CheckForCapture(in InputDeviceState deviceState)
        {
            if ( deviceState.IsLeftButtonPress && deviceState.CtrlButton.bDown )
            {
                if (WidgetScene.HitQuery(deviceState.CurrentPosition, out WidgetHitResult hitResult))
                {
                    if ( hitResult.HitWidget is NodePinWidget )
                        return new InputCaptureRequest(this, this, 9999);
                }
            }
            return InputCaptureRequest.None;
        }


        protected NodeWidget? hitNodeWidget = null;
        protected NodeInputPinWidget? hitInputPinWidget = null;
        protected NodeOutputPinWidget? hitOutputPinWidget = null;
        protected NodeExecPinWidget? hitExecPinWidget = null;

        public override void BeginCapture(in InputDeviceState deviceState, in InputCaptureRequest fromRequest)
        {
            WidgetHitResult hitResult = WidgetHitResult.None;
            if ( WidgetScene.HitQuery(deviceState.CurrentPosition, out hitResult) )
            {
                //if (hitResult.HitWidget is NodeWidget)
                //    hitNodeWidget = (NodeWidget)hitResult.HitWidget;
                //else 
                if (hitResult.HitWidget is NodeInputPinWidget)
                    hitInputPinWidget = (NodeInputPinWidget)hitResult.HitWidget;
                else if (hitResult.HitWidget is NodeOutputPinWidget)
                    hitOutputPinWidget = (NodeOutputPinWidget)hitResult.HitWidget;
                else if (hitResult.HitWidget is NodeExecPinWidget)
                    hitExecPinWidget = (NodeExecPinWidget)hitResult.HitWidget;
            }
        }

        public override void UpdateCapture(in InputDeviceState deviceState)
        {
        }

        public override void EndCapture(in InputDeviceState deviceState)
        {

            WidgetHitResult hitResult = WidgetHitResult.None;
            bool bHit = WidgetScene.HitQuery(deviceState.CurrentPosition, out hitResult);

            if ( hitNodeWidget != null && hitResult.HitWidget == hitNodeWidget )
            {
                NodeWidget removeWidget = hitNodeWidget;
                GraphViewport.ExecuteGraphEdit((NodeGraphEditor editor) => { 
                    editor.RemoveNode(removeWidget); 
                });
            }
            else if (hitInputPinWidget != null && hitResult.HitWidget == hitInputPinWidget)
            {
                NodeInputPinWidget disconnectInputWidget = hitInputPinWidget;
                GraphViewport.ExecuteGraphEdit((NodeGraphEditor editor) => {
                    editor.RemoveAllConnectionsToInput(disconnectInputWidget);
                });
            }
            else if (hitOutputPinWidget != null && hitResult.HitWidget == hitOutputPinWidget)
            {
                NodeOutputPinWidget nodeOutputPinWidget = hitOutputPinWidget;
                GraphViewport.ExecuteGraphEdit((NodeGraphEditor editor) => {
                    editor.RemoveAllConnectionsFromOutput(nodeOutputPinWidget);
                });
            }
            else if (hitExecPinWidget != null && hitResult.HitWidget == hitExecPinWidget)
            {
                NodeExecPinWidget widget = hitExecPinWidget;
                GraphViewport.ExecuteGraphEdit((NodeGraphEditor editor) => {
                    editor.RemoveAllConnectionsFromSequencePin(widget);
                });
            }

            hitNodeWidget = null;  hitInputPinWidget = null;  hitOutputPinWidget = null;
        }

        public override void AbortCapture()
        {
            hitNodeWidget = null; hitInputPinWidget = null; hitOutputPinWidget = null;
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
