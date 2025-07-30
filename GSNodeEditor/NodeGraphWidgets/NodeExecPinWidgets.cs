// Copyright Gradientspace Corp. All Rights Reserved.
using g3;
using Gradientspace.NodeGraph;
using Gradientspace.UI;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace GSNodeEditor
{

    public abstract class NodeExecPinWidget : NodePinWidget, ISimpleCaptureTarget
    {
        // todo promote to NodePinWidget?
        public NodeWidget ParentNodeWidget { get; set; }

        public Vector2f Dimensions { get; set; } = new Vector2f(12, 12);

        public bool Connected { get; set; } = false;

        public NodeExecPinWidget(NodeWidget parentWidget)
        {
            ParentNodeWidget = parentWidget;

            // need to initialize GraphDataType ??

            WidgetStyle = PinWidgetStyles.DefaultSequenceStyleSet;

            SetInputBehavior(new ExtendableWidgetInputBehavior(this, this) { Depth = 0 });
        }

        public override IWidgetView CreateDefaultView()
        {
            return new NodeExecPinWidgetView(this);
        }

        // ISimpleCaptureTarget API
        public void UpdateCapture(ISimpleCaptureTarget.ECaptureState State, in InputDeviceState deviceState)
        {
            IsCapturing = (State == ISimpleCaptureTarget.ECaptureState.Begin || State == ISimpleCaptureTarget.ECaptureState.Update);
        }
        public void UpdateHover(ISimpleCaptureTarget.EHoverState State, in InputDeviceState deviceState, out bool bContinueHover)
        {
            bContinueHover = true;
            IsHovered = (State == ISimpleCaptureTarget.EHoverState.Begin || State == ISimpleCaptureTarget.EHoverState.Update);
        }
        public bool IsHovered { get; private set; }
        public bool IsCapturing { get; private set; }

    }



    public class NodeInputExecPinWidget : NodeExecPinWidget
    {
        public NodeInputExecPinWidget(NodeWidget parentWidget) : base(parentWidget)
        {
        }

        public override bool IsOutputPin { get { return false; } }

        public void UpdateInlineInfo(INodeGraph Graph, int OwningNodeIdentifier)
        {
            IConnectionInfo found = Graph.FindConnectionTo(OwningNodeIdentifier, "", EConnectionType.Sequence);
            Connected = (found != IConnectionInfo.Invalid);
        }
    }
    public class NodeOutputExecPinWidget : NodeExecPinWidget
    {
        public NodeOutputExecPinWidget(NodeWidget parentWidget) : base(parentWidget)
        {
        }

        public override bool IsOutputPin { get { return true; } }

        public void UpdateInlineInfo(INodeGraph Graph, int OwningNodeIdentifier)
        {
            List<IConnectionInfo> connectionInfos = new List<IConnectionInfo>();
            Graph.FindConnectionsFrom(OwningNodeIdentifier, "", ref connectionInfos, EConnectionType.Sequence);
            Connected = (connectionInfos.Count > 0);
        }
    }


    public class NodeExecPinWidgetView : IWidgetView
    {
        NodeExecPinWidget SourcePinWidget;
		public int LastDrawOrderIndex { get; set; } = 0;

		public NodeExecPinWidgetView(NodeExecPinWidget pinWidget)
        {
            this.SourcePinWidget = pinWidget;
        }

        public Widget GetWidget() { return SourcePinWidget; }

        public AxisAlignedBox2f LocalBounds { get; set; }
        public Vector2f DrawOrigin { get; set; }

        public AxisAlignedBox2f BoundsQuery(ILayoutAnchor? RelativeToAnchor = null)
        {
            return (RelativeToAnchor != null) ?
                AnchorLocation.GetAnchoredBounds(LocalBounds, RelativeToAnchor, GetWidget().AnchorPlacement) : LocalBounds;
        }

        public void UpdateLayout(SKStyleCache StyleCache)
        {
            LocalBounds = new AxisAlignedBox2f(Vector2f.Zero, SourcePinWidget.Dimensions);
        }


        public bool HitQuery(Vector2f QueryPoint, out WidgetHitResult Result)
        {
            Result = WidgetHitResult.None;

            AxisAlignedBox2f WorldBounds =
                AnchorLocation.GetAnchoredBounds(LocalBounds, DrawOrigin, GetWidget().AnchorPlacement);
            if (WorldBounds.Contains(QueryPoint) == false)
                return false;

            Result = new WidgetHitResult() { HitWidget = SourcePinWidget, HitZDepth = 5 };
            return true;
        }


        public bool HitTest(Vector2f QueryPoint)
        {
            WidgetHitResult HitResult;
            return HitQuery(QueryPoint, out HitResult);
        }


        public void Draw(SKStyleCache StyleCache, SKCanvas Canvas, ILayoutAnchor Anchor)
        {
            DrawOrigin = Anchor.GetOrigin();

            AxisAlignedBox2f PlacedBounds = AnchorLocation.MakeRelativeToAnchor(LocalBounds, SourcePinWidget.AnchorPlacement, DrawOrigin);

            SKStyleCache.CachedSKPaintSet StandardPaints = StyleCache.GetCachedPaintSet(SourcePinWidget.WidgetStyle.StandardStyle);
            WidgetMargins Margins = SourcePinWidget.WidgetStyle.BaseMargins;

            if ( SourcePinWidget.Connected )
            {
                Canvas.DrawRect(Conversion.ToSkia(PlacedBounds), StandardPaints.ForegroundPaint);
            }
            else
            {
                Canvas.DrawRect(Conversion.ToSkia(PlacedBounds), StandardPaints.BackgroundPaint);
                Canvas.DrawRect(Conversion.ToSkia(PlacedBounds), StandardPaints.OutlinePaint);
            }
        }


    }

}
