using g3;
using Gradientspace.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GSNodeEditor
{
    public class NodeSelectionInputBehavior : ClickOrDragInputBehavior
    {
        public SelectionManager SelectionManager { get; set; }

        public NodeSelectionInputBehavior(SelectionManager selectionManager)
        {
            SelectionManager = selectionManager;
            CaptureSourceObject = SelectionManager;
        }


        private NodeWidget? NodeHitTest(in InputDeviceState deviceState)
        {
            bool bHit = SelectionManager.GraphViewport.WidgetScene.HitQuery(deviceState.CurrentPosition, out WidgetHitResult hitResult,
                (Widget w) => { return w is NodeWidget; });
            return (bHit) ? (hitResult.HitWidget as NodeWidget) : null;
        }

        public override bool WantClickOrDragCapture(in InputDeviceState deviceState, out float CaptureDepth)
        {
            CaptureDepth = Depth;
            if (!deviceState.IsLeftButtonPress) return false;
            NodeWidget? hitWidget = NodeHitTest(deviceState);
            // needs to be "behind" widgets...
            CaptureDepth = (hitWidget != null) ? -50 : -100;
            return true;
        }

        public override void OnClicked(in InputDeviceState deviceState)
        {
            base.OnClicked(deviceState);

            NodeWidget? hitWidget = NodeHitTest(deviceState);
            if (hitWidget != null)
            {
                if ( deviceState.ShiftButton.bDown )
                    SelectionManager.Select(hitWidget.GraphNodeIdentifier, false);
                else if (deviceState.CtrlButton.bDown)
                    SelectionManager.Deselect(hitWidget.GraphNodeIdentifier);
                else 
                    SelectionManager.Select(hitWidget.GraphNodeIdentifier, true);
            }
            else
                SelectionManager.ClearSelection();
        }


        enum EDragInteractionTypes
        {
            None,
            DragSingleWidget,
            DragSelectedWidgets,
            DragMarquee
        }
        EDragInteractionTypes DragInteraction = EDragInteractionTypes.None;

        Vector2f InitialCursorPosition;

        List<NodeWidget> ActiveNodes = new List<NodeWidget>();
        List<Vector2f> InitialNodePositions = new List<Vector2f>();

        public override void OnBeginDrag(in InputDeviceState deviceState)
        {
            base.OnBeginDrag(deviceState);
            InitialCursorPosition = deviceState.CurrentPosition;

            DragInteraction = EDragInteractionTypes.None;
            InitialNodePositions.Clear();
            NodeWidget? hitWidget = NodeHitTest(deviceState);
            if (hitWidget != null )
            {
                bool bIsSelected = SelectionManager.IsSelected(hitWidget.GraphNodeIdentifier);
                if ( bIsSelected == false )
                {
                    DragInteraction = EDragInteractionTypes.DragSingleWidget;
                    ActiveNodes.Add(hitWidget);
                    InitialNodePositions.Add(hitWidget.Position);
                }
                else
                {
                    foreach (int NodeID in SelectionManager.CurrentSelection)
                    {
                        NodeWidget? widget = SelectionManager.GraphView.FindNode(NodeID);
                        if ( widget != null )
                        {
                            ActiveNodes.Add(widget);
                            InitialNodePositions.Add(widget.Position);
                        }
                    }
                    if (ActiveNodes.Count > 0)
                        DragInteraction = EDragInteractionTypes.DragSelectedWidgets;
                }
            }
            else
            {
                DragInteraction = EDragInteractionTypes.DragMarquee;
                SelectionManager.BeginMarqueeSelection(deviceState);
            }
        }
        public override void OnUpdateDrag(in InputDeviceState deviceState)
        {
            Vector2f Delta = deviceState.CurrentPosition - InitialCursorPosition;
            if ( DragInteraction == EDragInteractionTypes.DragSingleWidget || DragInteraction == EDragInteractionTypes.DragSelectedWidgets )
            {
                int N = ActiveNodes.Count;
                for ( int i = 0; i < N; ++i )
                {
                    Vector2f NewPosition = InitialNodePositions[i] + Delta;
                    if (Settings.EnableGridSnapping.Value == true) {
                        float SnapStep = Settings.GridSnappingSize.Value;
                        NewPosition.x = (float)Snapping.SnapToIncrement(NewPosition.x, SnapStep);
                        NewPosition.y = (float)Snapping.SnapToIncrement(NewPosition.y, SnapStep);
                    }
                    ActiveNodes[i].Position = NewPosition;
                }
            }
            else if (DragInteraction == EDragInteractionTypes.DragMarquee)
            {
                SelectionManager.UpdateMarqueeSelection(deviceState);
            }

            base.OnUpdateDrag(deviceState);
        }
        public override void OnEndDrag(in InputDeviceState deviceState)
        {
            if (DragInteraction == EDragInteractionTypes.DragMarquee) {
                SelectionManager.UpdateMarqueeSelection(deviceState);
                SelectionManager.CompleteMarqueeSelection();
            }

            DragInteraction = EDragInteractionTypes.None;
            ActiveNodes.Clear();
            InitialNodePositions.Clear();

            base.OnEndDrag(deviceState);
        }



    }
}
