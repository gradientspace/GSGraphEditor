// Copyright Gradientspace Corp. All Rights Reserved.
using g3;

namespace Gradientspace.UI
{

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

    public struct AnchorLocation
    {
        // todo maybe this should just be x/y lerp parameters?
        public BoxPoints BoxAnchorPoint = BoxPoints.TopLeft;

        public AnchorLocation() { }
        public AnchorLocation(BoxPoints anchorLocation) { BoxAnchorPoint = anchorLocation; }

        public static AxisAlignedBox2f MakeRelativeToAnchor(
            AxisAlignedBox2f Box,
            AnchorLocation BoxPlacement,
            Vector2f AnchorPosition )
        {
            Vector2f BoxPosition = GetBoxPoint(Box, BoxPlacement.BoxAnchorPoint);
            Vector2f Translation = AnchorPosition - BoxPosition;
            Box.Translate(Translation);
            return Box;
        }

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


        public static AxisAlignedBox2f GetAnchoredBounds(AxisAlignedBox2f localBounds, ILayoutAnchor anchor, AnchorLocation location)
        {
            return AnchorLocation.MakeRelativeToAnchor(localBounds, location, anchor.GetOrigin());
        }
        public static AxisAlignedBox2f GetAnchoredBounds(AxisAlignedBox2f localBounds, Vector2f anchorOrigin, AnchorLocation location)
        {
            return AnchorLocation.MakeRelativeToAnchor(localBounds, location, anchorOrigin);
        }

    }


    // struct?
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
            return AnchorLocation.GetBoxPoint(SourceBounds, BoxPoint) + Offset;
        }
    }


    //public class ViewBoundsAnchor : ILayoutAnchor
    //{
    //    public IWidgetView? WidgetView { get; set; } = null;
    //    public BoxPoints BoxPoint { get; set; } = BoxPoints.BottomLeft;
    //    public Vector2f Offset { get; set; } = Vector2f.Zero;

    //    public ViewBoundsAnchor()
    //    {
    //    }

    //    public virtual Vector2f GetOrigin()
    //    {
    //        WidgetView.BoundsQuery(WidgetView.GetWidget().GetAnchor()) ?? 

    //        return AnchorLocation.GetBoxPoint(SourceBounds, BoxPoint) + Offset;
    //    }
    //}


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
            return ParentAnchor.GetOrigin() + AnchorLocation.GetBoxPoint(Box, BoxPoint) + Offset;
        }
    }


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
            return ParentWidget.GetAnchor().GetOrigin() + AnchorLocation.GetBoxPoint(Box, BoxPoint) + Offset;
        }
    }



    public class DynamicRelativeAnchor : ILayoutAnchor
    {
        public ILayoutAnchor ParentAnchor { get; set; }
        public Func<ILayoutAnchor?> ChildAnchorQuery { get; set; }
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

            ILayoutAnchor? childAnchor = ChildAnchorQuery();
            if (childAnchor != null)
                Origin += childAnchor.GetOrigin();
            return Origin;
        }
    }




    // singleton world anchor that can be used in cases where some other anchor is null
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
