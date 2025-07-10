// Copyright Gradientspace Corp. All Rights Reserved.
using g3;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gradientspace.UI
{

    public enum EWidgetHoverState { 
        BeginHover = 0, 
        UpdateHover = 1, 
        EndHover = 2 
    }


    public interface IWidgetContentExtension
    {
        void DrawContent(Widget parentWidget, SKStyleCache StyleCache, SKCanvas Canvas, AxisAlignedBox2f Bounds, bool bIsLocalBounds);
    }


    public enum WidgetDepthLayers : int
    {
        DeepBackground = -2000,
        Background = -1000,
        Foreground = 0,
        Overlay1 = 1000,
        Overlay2 = 2000,
        Overlay3 = 3000
    }

    /**
     * Defines a Z value for a widget where +Z is toward the viewer, ie larger depth == closer to viewer
     * (is there a better word to use here?)
     */
    public struct WidgetDepth
    {
        public WidgetDepthLayers DepthLayer;
        public float ZShift;

        public WidgetDepth(WidgetDepthLayers depthLayer, float zShift = 0) { DepthLayer = depthLayer; ZShift = zShift; }
        public WidgetDepth(float zShift) { DepthLayer = WidgetDepthLayers.Foreground; ZShift = zShift; }
        public static readonly WidgetDepth Default = new WidgetDepth(WidgetDepthLayers.Foreground, 0);

        //! combined Z value 
        public float Z { get { return (float)DepthLayer + ZShift; } }

        public static float Combine(WidgetDepth parentDepth, WidgetDepth childDepth, float childDivide = 10.0f)
        {
            float depth = parentDepth.Z + childDepth.Z / childDivide;
            return depth;
        }
    }


    public abstract class Widget : IDisposable
    {
        protected List<Widget> childWidgets;
        protected Widget? parentWidget = null;
        protected IWidgetView? activeView = null;

        protected ILayoutAnchor? layoutAnchor = null;
        protected AnchorLocation anchorPlacement = new AnchorLocation() { BoxAnchorPoint = BoxPoints.BottomLeft };

        protected InputBehavior? inputBehavior = null;

        protected WidgetDepth widgetDepth;
        protected bool inheritParentDepth = true;

        public Widget()
        {
            childWidgets = new List<Widget>();
            widgetDepth = WidgetDepth.Default;
        }

        public virtual void Dispose() {
            foreach (Widget widget in childWidgets)
                widget.Dispose();
        }

        public Widget? ParentWidget {  get { return parentWidget; } }
        public event EventHandler? ChildWidgetsModified;

        protected virtual void Child_ChildWidgetsModified(object? sender, EventArgs e) {
            ChildWidgetsModified?.Invoke(this, e);
        }

        public virtual void AddChildWidget(Widget child)
        {
            Debug.Assert(childWidgets.Contains(child) == false);
            Debug.Assert(child.parentWidget == null);
            childWidgets.Add(child);
            child.parentWidget = this;

            // allow ChildWidgetsModified to propagate up to root
            // instead of doing it this way, we could have AddChildWidget() find it's root widget and then post this event?
            child.ChildWidgetsModified += Child_ChildWidgetsModified;

            ChildWidgetsModified?.Invoke(this, EventArgs.Empty);
        }

        public virtual bool RemoveChildWidget(Widget child)
        {
            bool bFound = childWidgets.Remove(child);
            if (bFound)
            {
                child.parentWidget = null;
                child.ChildWidgetsModified -= Child_ChildWidgetsModified;
                ChildWidgetsModified?.Invoke(this, EventArgs.Empty);
            }
            return bFound;
        }

        public virtual void RemoveAllChildWidgets()
        {
            if (childWidgets.Count == 0) return;

            foreach (Widget child in childWidgets) {
                child.parentWidget = null;
                child.ChildWidgetsModified -= Child_ChildWidgetsModified;
            }
            childWidgets.Clear();

            ChildWidgetsModified?.Invoke(this, EventArgs.Empty);
        }

        public int NumChildWidgets {  get { return childWidgets.Count; } }

        public virtual IEnumerable<Widget> EnumerateChildWidgets(bool bRecursive, bool bDepthFirst = true)
        {
            if (bRecursive)
            {
                foreach (Widget child in childWidgets)
                {
                    if (!bDepthFirst) yield return child;

                    foreach (Widget child_child in child.EnumerateChildWidgets(bRecursive))
                        yield return child_child;

                    if ( bDepthFirst ) yield return child;
                }
            }
            else
            {
                foreach (Widget child in childWidgets)
                    yield return child;
            }
        }


        public abstract IWidgetView CreateDefaultView();

        public virtual IWidgetView InitializeView(IWidgetViewFactory? CustomFactory)
        {
            IWidgetView NewView = (CustomFactory != null) ?
                CustomFactory.CreateViewForType(this.GetType()) : CreateDefaultView();
            SetActiveView(NewView);
            return NewView;
        }


        public virtual void SetActiveView(IWidgetView NewView)
        {
            activeView = NewView;
        }
        public virtual void ClearActiveView()
        {
            activeView = null;
        }

        public virtual IWidgetView? GetActiveView() { return activeView; }



        public virtual void AnchorTo(ILayoutAnchor Anchor)
        {
            layoutAnchor = Anchor;
        }
        public virtual void ClearAnchor()
        {
            layoutAnchor = null;
        }
        public virtual bool IsAnchored() { return layoutAnchor != null; }
        public virtual ILayoutAnchor GetAnchor()
        {
            if (layoutAnchor != null)
                return layoutAnchor;
            ILayoutAnchor? ParentAnchor = (parentWidget != null) ? parentWidget.GetAnchor() : null;
            return (ParentAnchor != null) ? ParentAnchor : DefaultWorldAnchor.Instance;
        }

        public AnchorLocation AnchorPlacement
        {
            get { return anchorPlacement; }
            set { anchorPlacement = value; }
        }


        public virtual WidgetDepth RenderDepth
        {
            get { return widgetDepth; }
            set { widgetDepth = value; }
        }
        public virtual bool InheritParentDepth
        {
            get { return inheritParentDepth; }
            set { inheritParentDepth = value; }
        }


        public virtual InputBehavior? InputBehavior { get => inputBehavior; }

        public virtual void SetInputBehavior(InputBehavior inputBehavior)
        {
            this.inputBehavior = inputBehavior;
        }




        public virtual bool GetTooltipStrings(out string? tooltip, out string[]? extendedTooltip)
        {
            tooltip = null;
            extendedTooltip = null;
            return false;
        }


        public Widget FindRoot()
        {
            Widget Cur = this;
            while (Cur.ParentWidget != null)
            {
                Cur = Cur.ParentWidget;
            }
            return Cur;
        }


    }
}
