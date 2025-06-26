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
    public interface IWidgetSource
    {
        IEnumerable<Widget> CollectRootWidgets();

        event EventHandler WidgetsUpdated;
    }

    public class SimpleWidgetSource : IWidgetSource
    {
        protected List<Widget> RootWidgets = new List<Widget>();

        public event EventHandler? WidgetsUpdated;

        public IEnumerable<Widget> CollectRootWidgets() { return RootWidgets.AsEnumerable<Widget>(); }

        public void NotifyRootWidgetsUpdated()
        {
            WidgetsUpdated?.Invoke(this, EventArgs.Empty);
        }

        public void AddRootWidget(Widget rootWidget)
        {
            if ( RootWidgets.Contains(rootWidget) == false )
            {
                RootWidgets.Add(rootWidget);
                rootWidget.ChildWidgetsModified += RootWidget_ChildWidgetsModified;
                NotifyRootWidgetsUpdated();
            }
        }

        public void RemoveRootWidget(Widget rootWidget)
        {
            if (RootWidgets.Contains(rootWidget))
            {
                RootWidgets.Remove(rootWidget);
                rootWidget.ChildWidgetsModified -= RootWidget_ChildWidgetsModified;
                NotifyRootWidgetsUpdated();
            }
        }

        private void RootWidget_ChildWidgetsModified(object? sender, EventArgs e)
        {
            NotifyRootWidgetsUpdated();
        }

        public void Clear()
        {
            Widget[] removeWidgets = RootWidgets.ToArray();
            foreach(Widget rootWidget in removeWidgets)
            {
                RootWidgets.Remove(rootWidget);
                rootWidget.ChildWidgetsModified -= RootWidget_ChildWidgetsModified;
            }
            NotifyRootWidgetsUpdated();
        }
    }



    public class WidgetScene : IInputBehaviorCollection, IDisposable
    {
        protected struct WidgetSourceSet
        {
            public IWidgetSource Source;
            public List<Widget> RootWidgets = new List<Widget>();
            public WidgetSourceSet(IWidgetSource source) { Source = source; }
        }

        protected struct WidgetInfo
        {
            public IWidgetSource? Source;
            public IWidgetView? View;
            public WidgetInfo(IWidgetSource source, IWidgetView view) {  Source = source; View = view; }    
        }

        protected List<WidgetSourceSet> AllWidgetInfos = new List<WidgetSourceSet>();
        protected Dictionary<Widget, WidgetInfo> RootWidgetViews = new Dictionary<Widget, WidgetInfo>();

        List<Action> PendingUpdates = new List<Action>();

        public bool EnableDrawDebugBounds = false;

        public WidgetScene()
        {

        }

        public virtual void Dispose()
        {
            foreach (WidgetSourceSet set in AllWidgetInfos) {
                foreach (Widget rootWidget in set.RootWidgets)
                    rootWidget.Dispose();
            }
        }

        public IWidgetViewFactory? ActiveCustomFactory { get; set; } = null;

        public void AddSource(IWidgetSource Source)
        {
            WidgetSourceSet info = new WidgetSourceSet(Source);
            foreach (Widget rootWidget in Source.CollectRootWidgets())
            {
                Debug.Assert(rootWidget.ParentWidget == null);      // otherwise it's not a root widget!

                if (rootWidget != null && info.RootWidgets.Contains(rootWidget) == false)
                    info.RootWidgets.Add(rootWidget);
            }
            AllWidgetInfos.Add(info);

            foreach (Widget rootWidget in info.RootWidgets)
            {
                PopulateRootWidget(Source, rootWidget);
            }

            Source.WidgetsUpdated += OnWidgetSourceUpdated;
        }

        protected void OnWidgetSourceUpdated(object? o, EventArgs args)
        {
            IWidgetSource? source = (IWidgetSource)o!;
            if ( source != null)
            {
                // TODO: this is not good, if widgets have been removed from source 
                // they will not be removed by RemoveSource!!
                RemoveSource(source);
                RemoveOrphanWidgets(source);
                PendingUpdates.Add( () => { AddSource(source); } );
            }

        }


        protected void PopulateRootWidget(IWidgetSource source, Widget rootWidget)
        {
            IWidgetView rootView = rootWidget.InitializeView(ActiveCustomFactory);
            RootWidgetViews.Add(rootWidget, new WidgetInfo(source, rootView) );

            if (rootWidget.IsAnchored() == false)
                rootWidget.AnchorTo(sceneAnchor);

            foreach (Widget childWidget in rootWidget.EnumerateChildWidgets(true))
            {
                /*IWidgetView childView =*/ childWidget.InitializeView(ActiveCustomFactory);

                //RootWidgetViews.Add(childWidget, new WidgetInfo(source, childView) );
            }
        }


        public bool RemoveSource(IWidgetSource Source)
        {
            int FoundIndex = -1;
            for (int i = 0; i < AllWidgetInfos.Count && FoundIndex == -1; ++i)
            {
                if (AllWidgetInfos[i].Source == Source)
                    FoundIndex = i;
            }
            if (FoundIndex == -1) return false;

            Source.WidgetsUpdated -= OnWidgetSourceUpdated;

            foreach (Widget rootWidget in AllWidgetInfos[FoundIndex].RootWidgets)
            {
                DestroyRootWidget(rootWidget);
            }
            AllWidgetInfos.RemoveAt(FoundIndex);

            return true;
        }


        protected void DestroyRootWidget(Widget rootWidget)
        {
            RootWidgetViews.Remove(rootWidget);
            foreach (Widget childWidget in rootWidget.EnumerateChildWidgets(true))
            {
                //RootWidgetViews.Remove(childWidget);
                childWidget.ClearActiveView();
            }
        }
        protected void RemoveOrphanWidgets(IWidgetSource source)
        {
            List<Widget> orphans = new List<Widget>();
            foreach (var pair in RootWidgetViews)
            {
                if (pair.Value.Source == source)
                    orphans.Add(pair.Key);
            }
            foreach (var key in orphans)
                RootWidgetViews.Remove(key);
        }


        protected FixedPointAnchor sceneAnchor = new FixedPointAnchor();


        public void UpdateScene()
        {
            if ( PendingUpdates.Count > 0)
                ProcessPendingUpdates();
        }
        protected void ProcessPendingUpdates()
        {
            foreach (Action action in PendingUpdates)
                action();
            PendingUpdates.Clear();
        }



        public void UpdateLayout(SKStyleCache StyleCache)
        {
            // todo probably should try to do based on the dictionary somehow or something...
            foreach (WidgetSourceSet set in AllWidgetInfos)
            {
                foreach (Widget rootWidget in set.RootWidgets)
                    SolveLayout(rootWidget, StyleCache);
            }
        }
        protected void SolveLayout(Widget rootWidget, SKStyleCache StyleCache)
        {
            rootWidget.GetActiveView()?.UpdateLayout(StyleCache);
        }



        protected struct DrawInfo
        {
            public Widget widget;
            public IWidgetView view;
            public ILayoutAnchor anchor;
            public int widgetIndex;   // this is just used to guarantee a stable sort
            public float depth;
        }
        protected struct DrawSet
        {
            public List<DrawInfo> DrawList = new List<DrawInfo>();

            public DrawSet() { }

            public void TryAddWidget(Widget widget, float useZ)
            {
                IWidgetView? view = widget.GetActiveView();
                if (view == null) return;
                DrawInfo di = new DrawInfo();
                di.widget = widget;
                di.view = view;
                di.anchor = widget.GetAnchor();
                di.depth = useZ;

                di.widgetIndex = DrawList.Count;

                DrawList.Add(di);
            }
        }

        public void Draw(SKStyleCache StyleCache, SKCanvas Canvas)
        {
            DrawSet draws = new DrawSet();
            foreach (WidgetSourceSet set in AllWidgetInfos) 
            {
                foreach (Widget rootWidget in set.RootWidgets) 
                {
                    WidgetDepth depth = rootWidget.RenderDepth;

                    draws.TryAddWidget(rootWidget, depth.Z);
                    foreach (Widget childWidget in rootWidget.EnumerateChildWidgets(true, false))
                    {
                        float childZ = (childWidget.InheritParentDepth) ? WidgetDepth.Combine(depth, childWidget.RenderDepth) : childWidget.RenderDepth.Z;
                        draws.TryAddWidget(childWidget, childZ);
                    }
                }
            }

            draws.DrawList.Sort((a, b) =>
            {   // this does a stable sort
                var result = a.depth.CompareTo(b.depth);
                return result != 0 ? result : a.widgetIndex.CompareTo(b.widgetIndex);
            });

            SKPaint BoxOutlinePaint = new SKPaint {
                Color = SKColors.Red,
                StrokeWidth = 1,
                IsStroke = true
            };

            int draw_index_counter = 0;
            foreach (DrawInfo di in draws.DrawList)
            {
                if (EnableDrawDebugBounds)
                {
                    AxisAlignedBox2f viewBounds = di.view.BoundsQuery(di.anchor);
                    Canvas.DrawRect(Conversion.ToSkia(viewBounds), BoxOutlinePaint);
                }

                di.view.Draw(StyleCache, Canvas, di.anchor);
                di.view.LastDrawOrderIndex = draw_index_counter++;
            }
        }



        public bool HitQuery(Vector2f QueryPoint, out WidgetHitResult hitResult, Predicate<Widget>? WidgetPredicateFunc = null)
        {
            hitResult = WidgetHitResult.None;

            WidgetHitResult nearestHitResult = WidgetHitResult.None;
            var query_widget = (Widget widget) => {
				WidgetHitResult tempResult = WidgetHitResult.None;
				IWidgetView? widgetView = widget.GetActiveView();
				if (widgetView != null && widgetView.HitQuery(QueryPoint, out tempResult) )
				{
                    tempResult.HitZDepth = widgetView.LastDrawOrderIndex;
					if (tempResult.HitZDepth >= nearestHitResult.HitZDepth)
						nearestHitResult = tempResult;
				}
			};


            foreach (WidgetSourceSet set in AllWidgetInfos)
            {
                foreach (Widget rootWidget in set.RootWidgets)
                {
                    foreach (Widget childWidget in rootWidget.EnumerateChildWidgets(true))
                    {
                        if (WidgetPredicateFunc == null || WidgetPredicateFunc(childWidget) == true) {
							query_widget(childWidget);
                        }
                    }

                    if ( WidgetPredicateFunc == null || WidgetPredicateFunc(rootWidget) == true ) 
                    {
						query_widget(rootWidget);
                    }
                }
            }

            hitResult = nearestHitResult;
			return hitResult.IsHit;
        }



        public bool ContainsWidget(Widget widget)
        {
            Widget rootWidget = widget.FindRoot();
            foreach (WidgetSourceSet set in AllWidgetInfos)
                if (set.RootWidgets.Contains(rootWidget)) 
                    return true;
            return false;
        }


        public InputCaptureRequest CheckForHoverCapture(in InputDeviceState deviceState)
        {
            InputCaptureRequest BestRequest = InputCaptureRequest.None;

            foreach (WidgetSourceSet set in AllWidgetInfos)
            {
                foreach (Widget rootWidget in set.RootWidgets)
                {
                    foreach (Widget childWidget in rootWidget.EnumerateChildWidgets(true))
                    {
                        InputCaptureRequest captureRequest = childWidget.InputBehavior?.CheckForHover(deviceState) ?? InputCaptureRequest.None;
						if (captureRequest != InputCaptureRequest.None)
							captureRequest.ZDepth = childWidget.GetActiveView().LastDrawOrderIndex;
						BestRequest = BestRequest.SelectCapture(captureRequest);
                    }

                    {
                        InputCaptureRequest captureRequest = rootWidget.InputBehavior?.CheckForHover(deviceState) ?? InputCaptureRequest.None;
						if (captureRequest != InputCaptureRequest.None)
							captureRequest.ZDepth = rootWidget.GetActiveView().LastDrawOrderIndex;
						BestRequest = BestRequest.SelectCapture(captureRequest);
                    }
                }
            }

            return BestRequest;
        }


        public InputCaptureRequest CheckForDeviceCapture(in InputDeviceState deviceState)
        {
            InputCaptureRequest BestRequest = InputCaptureRequest.None;

            foreach (WidgetSourceSet set in AllWidgetInfos)
            {
                foreach (Widget rootWidget in set.RootWidgets)
                {
                    foreach (Widget childWidget in rootWidget.EnumerateChildWidgets(true))
                    {
                        InputCaptureRequest captureRequest = childWidget.InputBehavior?.CheckForCapture(deviceState) ?? InputCaptureRequest.None;
                        if (captureRequest != InputCaptureRequest.None)
                            captureRequest.ZDepth = childWidget.GetActiveView().LastDrawOrderIndex;
						BestRequest = BestRequest.SelectCapture(captureRequest);
                    }

                    {
                        InputCaptureRequest captureRequest = rootWidget.InputBehavior?.CheckForCapture(deviceState) ?? InputCaptureRequest.None;
						if (captureRequest != InputCaptureRequest.None)
							captureRequest.ZDepth = rootWidget.GetActiveView().LastDrawOrderIndex;
						BestRequest = BestRequest.SelectCapture(captureRequest);
                    }
                }
            }

            return BestRequest;
        }

    }





    //internal class WidgetSceneWidget : Widget
    //{
    //    public override IWidgetView CreateDefaultView()
    //    {
    //        return new EmptyView(this);
    //    }

    //    internal class EmptyView : IWidgetView
    //    {
    //        Widget Owner;
    //        public EmptyView(Widget owner) { Owner = owner; }
    //        public AxisAlignedBox2f BoundsQuery(ILayoutAnchor? RelativeToAnchor = null) { return AxisAlignedBox2f.Empty; }
    //        public void Draw(SKStyleCache StyleCache, SKCanvas Canvas, ILayoutAnchor Anchor) { }
    //        public Widget GetWidget() { return owner; }
    //        public bool HitQuery(Vector2f QueryPoint, out WidgetHitResult Result) {
    //            Result = WidgetHitResult.None;
    //            return false;
    //        }
    //        public bool HitTest(Vector2f QueryPoint) { return false; }
    //        public void UpdateLayout(SKStyleCache StyleCache) { }
    //    }
    //}

}
