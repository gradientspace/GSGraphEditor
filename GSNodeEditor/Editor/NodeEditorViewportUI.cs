using g3;
using Gradientspace.UI;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GSNodeEditor
{
    public class NodeEditorViewportUI : IDisposable
    {
        protected IGraphEditorActions ActionsTarget;
        protected WidgetScene widgetScene;
        protected SimpleWidgetSource widgets;

        protected Button DebugButton { get; set; }
        protected FixedPointAnchor DebugButtonAnchor;

		protected Button NewButton { get; private set; }
		protected Button OpenButton { get; private set; }
		protected Button SaveButton { get; private set; }
		protected Button SaveAsButton { get; private set; }

		public NodeEditorViewportUI(IGraphEditorActions actionsTarget)
        {
            ActionsTarget = actionsTarget;
			widgetScene = new WidgetScene();
            widgets = new SimpleWidgetSource();

			int ButtonSize = 48;
			Vector2f ButtonDim = new Vector2f(ButtonSize);
			AxisAlignedBox2f ButtonRect = new AxisAlignedBox2f(ButtonSize);

			DebugButton = new SVGButton("c:\\scratch\\bug.svg") { Dimensions = ButtonDim };
            DebugButtonAnchor = new FixedPointAnchor();
            DebugButtonAnchor.AnchorOrigin = Vector2f.Zero;
            DebugButton.AnchorPlacement = new AnchorLocation(BoxPoints.TopLeft);
            DebugButton.AnchorTo(DebugButtonAnchor);
            DebugButton.OnClicked += DebugButton_OnClicked;
            widgets.AddRootWidget(DebugButton);

			NewButton = new ImageButton(Path.Combine(NodeEditorPaths.AssetsPath, "newIcon.png")) { Dimensions = ButtonDim, SmoothResize = true, DrawBackground = false };
			NewButton.AnchorPlacement = new AnchorLocation(BoxPoints.TopLeft);
			NewButton.AnchorTo(new WidgetRelativeBoxAnchor(DebugButton) { Box = ButtonRect, BoxPoint = BoxPoints.TopRight, Offset = new Vector2f(2, 0) });
			NewButton.OnClicked += NewButton_OnClicked;
			widgets.AddRootWidget(NewButton);

			OpenButton = new ImageButton(Path.Combine(NodeEditorPaths.AssetsPath, "openIcon.png")) { Dimensions = ButtonDim, SmoothResize = true, DrawBackground = false };
			OpenButton.AnchorPlacement = new AnchorLocation(BoxPoints.TopLeft);
			OpenButton.AnchorTo(new WidgetRelativeBoxAnchor(NewButton) { Box = ButtonRect, BoxPoint = BoxPoints.TopRight, Offset = new Vector2f(2, 0) });
			OpenButton.OnClicked += LoadButton_OnClicked;
			widgets.AddRootWidget(OpenButton);

			SaveButton = new ImageButton(Path.Combine(NodeEditorPaths.AssetsPath, "saveIcon.png")) { Dimensions = ButtonDim, SmoothResize = true, DrawBackground = false };
			SaveButton.AnchorPlacement = new AnchorLocation(BoxPoints.TopLeft);
			SaveButton.AnchorTo(new WidgetRelativeBoxAnchor(OpenButton) { Box = ButtonRect, BoxPoint = BoxPoints.TopRight, Offset = new Vector2f(2, 0) });
			SaveButton.OnClicked += SaveButton_OnClicked;
			widgets.AddRootWidget(SaveButton);

			SaveAsButton = new ImageButton(Path.Combine(NodeEditorPaths.AssetsPath, "saveAsIcon.png")) { Dimensions = ButtonDim, SmoothResize = true, DrawBackground = false };
			SaveAsButton.AnchorPlacement = new AnchorLocation(BoxPoints.TopLeft);
			SaveAsButton.AnchorTo(new WidgetRelativeBoxAnchor(SaveButton) { Box = ButtonRect, BoxPoint = BoxPoints.TopRight, Offset = new Vector2f(2,0) });
			SaveAsButton.OnClicked += SaveAsButton_OnClicked;
			widgets.AddRootWidget(SaveAsButton);

			widgetScene.AddSource(widgets);
        }

        public WidgetScene WidgetScene { get { return widgetScene; } }



        private void DebugButton_OnClicked(Button button) {
            Settings.EnableDebugging.Value = !Settings.EnableDebugging.Value;
        }

        private void NewButton_OnClicked(Button button) {
            ActionsTarget.TryNewExecutionGraph();
		}

        private void LoadButton_OnClicked(Button button) {
            ActionsTarget.TryOpen();
		}

		private void SaveButton_OnClicked(Button button) {
            ActionsTarget.TrySave();
		}

		private void SaveAsButton_OnClicked(Button button) {
			ActionsTarget.TrySaveAs();
		}

		public void Dispose()
        {
            widgetScene.Dispose();
        }


        public void UpdateLayout(SKStyleCache styleCache)
        {
            widgetScene.UpdateScene();
            widgetScene.UpdateLayout(styleCache);
        }

        public void Draw(SKStyleCache styleCache, SKCanvas canvas)
        {
            widgetScene.Draw(styleCache, canvas);
        }

    }
}
