// Copyright Gradientspace Corp. All Rights Reserved.
using g3;

namespace Gradientspace.UI
{
    /// <summary>
    /// Base interface for an Anchor, which is just a thing that returns a point
    /// (maybe eventually will support rotation)
    /// </summary>
    public interface ILayoutAnchor
    {
        Vector2f GetOrigin();
    }

    //! BoxPoints top/bottom are corrected for y-down screen coordinates
    public enum BoxPoints
    {
        TopRight = 0,
        TopLeft = 1,
        BottomRight = 2,
        BottomLeft = 3,
        Center = 4,
        CenterLeft = 5,
        CenterRight = 6,
        CenterBottom = 7,
        CenterTop = 8
    }


    public static class AnchorMath
    {
        /// <summary>
        /// return a point on a Box. Note that this is assuming Y-down, ie "bottom" is Max.y
        /// </summary>
        public static Vector2f GetBoxPoint(AxisAlignedBox2f Box, BoxPoints BoxLocation)
        {
            Vector2f BoxPosition = Box.Center;
            switch (BoxLocation)
            {
                case BoxPoints.TopRight:
                    BoxPosition = new Vector2f(Box.Max.x, Box.Min.y); break;
                case BoxPoints.TopLeft:
                    BoxPosition = new Vector2f(Box.Min.x, Box.Min.y); break;
                case BoxPoints.BottomRight:
                    BoxPosition = new Vector2f(Box.Max.x, Box.Max.y); break;
                case BoxPoints.BottomLeft:
                    BoxPosition = new Vector2f(Box.Min.x, Box.Max.y); break;
                case BoxPoints.CenterLeft:
                    BoxPosition.x = Box.Min.x; break;
                case BoxPoints.CenterRight:
                    BoxPosition.x = Box.Max.x; break;
                case BoxPoints.CenterBottom:
                    BoxPosition.y = Box.Max.y; break;
                case BoxPoints.CenterTop:
                    BoxPosition.y = Box.Min.y; break;
            }
            return BoxPosition;
        }


    }


    /// <summary>
    /// An AnchorLocation is used to specify where on a Box an Anchor point should
    /// be placed. Every Widget has this defined as the .AnchorPlacement field, along with a parent Anchor.
    /// 
    /// The way this is used is that, given the local bounds of a Widget, the AnchorPlacement/AnchorLocation
    /// defines the point on those bounds that should be placed at the parent Anchor's location/origin.
    /// So the world position of the Widget (ie where it is drawn/etc) is going to be defined by
    ///   1) Get the local bounds
    ///   2) compute a point on the local bounds defined by AnchorPlacement/AnchorLocation
    ///   3) translate from that point to the Anchor.GetOrigin()
    ///   
    /// AnchorLocation.MakeRelativeToAnchor() does this calculation, given the external box
    /// </summary>
    public struct AnchorLocation
    {
        // todo maybe this should just be x/y lerp parameters?
        public BoxPoints BoxAnchorPoint = BoxPoints.TopLeft;
        public Vector2f Offset = Vector2f.Zero;

        public AnchorLocation() { }
        public AnchorLocation(BoxPoints anchorLocation) { BoxAnchorPoint = anchorLocation; }

        // todo move to AnchorMath
        // maybe also make a non-static version that doesn't need to take the placement?
        public static AxisAlignedBox2f MakeRelativeToAnchor(
            AxisAlignedBox2f Box,
            AnchorLocation BoxPlacement,
            Vector2f AnchorPosition )
        {
            Vector2f BoxPosition = AnchorMath.GetBoxPoint(Box, BoxPlacement.BoxAnchorPoint);
			Vector2f Translation = AnchorPosition - BoxPosition + BoxPlacement.Offset;
            Box.Translate(Translation);
            return Box;
        }

        // todo move to AnchorMath
        public static AxisAlignedBox2f GetAnchoredBounds(AxisAlignedBox2f localBounds, ILayoutAnchor anchor, AnchorLocation location)
        {
            return AnchorLocation.MakeRelativeToAnchor(localBounds, location, anchor.GetOrigin());
        }
        // todo move to AnchorMath
        public static AxisAlignedBox2f GetAnchoredBounds(AxisAlignedBox2f localBounds, Vector2f anchorOrigin, AnchorLocation location)
        {
            return AnchorLocation.MakeRelativeToAnchor(localBounds, location, anchorOrigin);
        }

    }


    /// <summary>
    /// FixedPointAnchor is defines the anchor point/origin in absolute coordinates,
    /// with an optional Offset
    /// </summary>
    public class FixedPointAnchor : ILayoutAnchor
    {
        public Vector2f AnchorOrigin { get; set; }
        public Vector2f Offset { get; set; }

        public FixedPointAnchor()
        {
            AnchorOrigin = Vector2f.Zero;
            Offset = Vector2f.Zero;
        }

        public virtual Vector2f GetOrigin()
        {
            return AnchorOrigin + Offset;
        }
    }


    /// <summary>
    /// BoxAnchor defines the anchor point/origin with a point on a provided Box,
    /// plus an optional Offset
    /// </summary>
    public class BoxAnchor : ILayoutAnchor
    {
        public AxisAlignedBox2f SourceBounds { get; set; } = AxisAlignedBox2f.Zero;
        public BoxPoints BoxPoint { get; set; } = BoxPoints.BottomLeft;
        public Vector2f Offset { get; set; } = Vector2f.Zero;

        public BoxAnchor(BoxPoints boxPoint = BoxPoints.BottomLeft)
        {
            BoxPoint = boxPoint;
        }

        public virtual Vector2f GetOrigin()
        {
            return AnchorMath.GetBoxPoint(SourceBounds, BoxPoint) + Offset;
        }
    }


    /// <summary>
    /// RelativeBoxAnchor defines the anchor point/origin based on a parent Anchor
    /// plus a Point on a Box (ie so the Box is defined in the local space of the parent Anchor),
    /// plus an optional Offset
    /// </summary>
    public class RelativeBoxAnchor : ILayoutAnchor
    {
        public ILayoutAnchor ParentAnchor { get; set; }
        public AxisAlignedBox2f Box { get; set; } = AxisAlignedBox2f.Zero;
        public BoxPoints BoxPoint { get; set; } = BoxPoints.BottomLeft;
        public Vector2f Offset { get; set; } = Vector2f.Zero;

        public RelativeBoxAnchor(ILayoutAnchor parent)
        {
            ParentAnchor = parent;
        }

        public virtual Vector2f GetOrigin()
        {
            return ParentAnchor.GetOrigin() + AnchorMath.GetBoxPoint(Box, BoxPoint) + Offset;
        }
    }


    /// <summary>
    /// WidgetRelativeBoxAnchor defines the anchor point/origin based on the Anchor of a parent Widget,
    /// plus a Point on a Box (ie so the Box is defined in the local space of the parent Widget/Anchor),
    /// plus an optional Offset.
    /// 
    /// UpdateFromParentWidget() can be used to automatically update the Box to be the bounds of
    /// the parent Widget (transformed into the appropriate parent-Anchor-relative space)
    /// </summary>
    public class WidgetRelativeBoxAnchor : ILayoutAnchor
    {
        public Widget ParentWidget { get; set; }
        public AxisAlignedBox2f Box { get; set; } = AxisAlignedBox2f.Zero;
        public BoxPoints BoxPoint { get; set; } = BoxPoints.BottomLeft;
        public Vector2f Offset { get; set; } = Vector2f.Zero;

        public WidgetRelativeBoxAnchor(Widget parentWidget)
        {
            ParentWidget = parentWidget;
        }

        public virtual Vector2f GetOrigin()
        {
            return ParentWidget.GetAnchor().GetOrigin() + AnchorMath.GetBoxPoint(Box, BoxPoint) + Offset;
        }

        //! Update the Anchor Box based based on the parent bounds and anchor
        public void UpdateFromParentWidget()
        {
            Box = GetParentAnchorRelativeBounds();
        }

        //! query the local bounds of the Parent widget and then make those bounds relative to the parent Anchor
        public AxisAlignedBox2f GetParentAnchorRelativeBounds()
        {
            AxisAlignedBox2f ParentWidgetLocalBounds = ParentWidget.GetActiveView()!.BoundsQuery(null);
            AxisAlignedBox2f PlacedBounds = AnchorLocation.MakeRelativeToAnchor(ParentWidgetLocalBounds,
                ParentWidget.AnchorPlacement, Vector2f.Zero);
            return PlacedBounds;
        }

    }


    /// <summary>
    /// DynamicRelativeAnchor is a functionally-defined Anchor where the 
    /// origin is defined by a Parent Anchor and an optional Child anchor.
    /// The Child anchor is returned by an optional Query function,
    /// and it's origin is interpreted as being in the local space of the Parent.
    /// An additional Offset is also supported.
    /// </summary>
    public class DynamicRelativeAnchor : ILayoutAnchor
    {
        public ILayoutAnchor ParentAnchor { get; set; }
        public Func<ILayoutAnchor?>? ChildAnchorQuery { get; set; }
        public Vector2f Offset { get; set; } = Vector2f.Zero;

        public DynamicRelativeAnchor(
            ILayoutAnchor parent,
            Func<ILayoutAnchor?> childQuery)
        {
            ParentAnchor = parent;
            ChildAnchorQuery = childQuery;
        }
            
        public virtual Vector2f GetOrigin()
        {
            Vector2f Origin = ParentAnchor.GetOrigin();

            ILayoutAnchor? childAnchor = ChildAnchorQuery?.Invoke() ?? null;
            if (childAnchor != null)
                Origin += childAnchor.GetOrigin();
            Origin += Offset;
            return Origin;
        }
    }




    /// <summary>
    /// DefaultWorldAnchor defines a singleton global/world anchor at (0,0), that can be used 
    /// in places where a non-null parent is required
    /// </summary>
    public sealed class DefaultWorldAnchor : ILayoutAnchor
    {
        private static readonly DefaultWorldAnchor instance = new DefaultWorldAnchor();

        static DefaultWorldAnchor() { }
        private DefaultWorldAnchor() { }

        public static DefaultWorldAnchor Instance {
            get { return instance; }
        }

        public Vector2f GetOrigin()
        {
            return Vector2f.Zero;
        }
    }

}
