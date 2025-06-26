using g3;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gradientspace.UI
{

    public struct WidgetHitResult
    {
        public object? HitWidget = null;
        public object? HitSubItem = null;
        public float HitZDepth = 0;
        public WidgetHitResult() { }
        public WidgetHitResult(object hitWidget, float hitZDepth) { HitWidget = hitWidget; HitZDepth = hitZDepth; }

        public bool IsHit { get { return HitWidget != null; } }

        public static WidgetHitResult None { get; } = new WidgetHitResult();
    }


    public interface IWidgetView
    {
        void UpdateLayout(SKStyleCache StyleCache);
        void Draw(SKStyleCache StyleCache, SKCanvas Canvas, ILayoutAnchor Anchor);
        bool HitTest(Vector2f QueryPoint);
        bool HitQuery(Vector2f QueryPoint, out WidgetHitResult Result);
        AxisAlignedBox2f BoundsQuery(ILayoutAnchor? RelativeToAnchor = null);
        Widget GetWidget();

		//! this is a hack right now...Renderer sets this based on drawing order in last frame,
		//! and then hit-tests return it as their Z-depth, and it's used to determine which
		//! CaptureRequest to return in WidgetScene's IInputBehaviorCollection implementation
		int LastDrawOrderIndex { get; set; }
    }


    public abstract class WidgetView : IWidgetView
    {
        public abstract void UpdateLayout(SKStyleCache StyleCache);
        public abstract void Draw(SKStyleCache StyleCache, SKCanvas Canvas, ILayoutAnchor Anchor);
        public abstract bool HitTest(Vector2f QueryPoint);
        public abstract bool HitQuery(Vector2f QueryPoint, out WidgetHitResult Result);
        public abstract AxisAlignedBox2f BoundsQuery(ILayoutAnchor? RelativeToAnchor = null);
        public abstract Widget GetWidget();


        public int LastDrawOrderIndex { get; set; } = 0;
	}



    public interface IWidgetViewFactory
    {
        public IWidgetView CreateViewForType(Type widgetType);
    }

}
