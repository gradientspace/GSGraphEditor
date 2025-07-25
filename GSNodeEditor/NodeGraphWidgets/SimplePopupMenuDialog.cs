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
	public class SimplePopupMenuDialog : Widget, ISimpleCaptureTarget, IHotkeyTarget
	{
		public WidgetStateStyle Style { get; set; }

		Vector2f _dimensions = new Vector2f(120, 15);

		protected FixedPointAnchor dialogAnchor;
		public Vector2f Position {
			get { return dialogAnchor.AnchorOrigin; }
			set { dialogAnchor.AnchorOrigin = value; }
		}

		public bool EnableClickToDismiss { get; set; } = true;

		//! this event is fired if EnableClickToDismiss==true and user clicks outside of popup menu
		public Action? OnDismissDialogClick { get; set; } = null;

		public delegate void ItemSelectedEventHandler(SimplePopupMenuDialog dialog, MenuItem item);
		public event ItemSelectedEventHandler? OnItemSelected;

		internal WidgetRelativeBoxAnchor ItemsMenuAnchor;
		internal PopupMenu ItemsMenu;


		public static readonly WidgetStyle DefaultStandardStyle = new WidgetStyle() { BackgroundColor = Colorf.LightSteelBlue, ForegroundColor = Colorf.Black, TextSize = 14, Margins = new WidgetMargins(6, 6, 3, 6) };
		public static readonly WidgetStyle DefaultHoverStyle = new WidgetStyle() { BackgroundColor = Colorf.Orange, ForegroundColor = Colorf.Black, TextColor = Colorf.Black, TextSize = 14 };
		public static readonly WidgetStyle DefaultPressedStyle = new WidgetStyle() { BackgroundColor = new Colorf(196, 206, 242), ForegroundColor = Colorf.Black, TextColor = Colorf.Black, TextSize = 14 };
		public static readonly WidgetStateStyle DefaultStyle = new WidgetStateStyle(
			DefaultStandardStyle, DefaultHoverStyle, DefaultPressedStyle);


		public SimplePopupMenuDialog(WidgetStateStyle? customStyle = null)
		{
			Style = (customStyle != null) ? customStyle : DefaultStyle;

			dialogAnchor = new FixedPointAnchor();
			AnchorTo(dialogAnchor);

			// do we need this intermediate anchor??
			ItemsMenuAnchor = new WidgetRelativeBoxAnchor(this);

			ItemsMenu = new PopupMenu(Style);
			ItemsMenu.EnableClickToDismiss = false;
			ItemsMenu.AnchorPlacement = new AnchorLocation(BoxPoints.BottomLeft);
			ItemsMenu.AnchorTo(dialogAnchor);

			ItemsMenu.OnMenuItemSelected += ItemsMenu_OnMenuItemSelected;

			AddChildWidget(ItemsMenu);

			SetInputBehavior(new BasicWidgetInputBehavior(this, this) {
				EnableHover = false,
				Depth = -10
			});

			RenderDepth = new WidgetDepth(WidgetDepthLayers.Overlay1);
		}


		public void ClearItems()
		{
			ItemsMenu.ClearItems();
		}

		public void AddItem(MenuItem newItem)
		{
			ItemsMenu.AddItem(newItem);
		}

		public void AddItems(IEnumerable<MenuItem> NewItems)
		{
			foreach (MenuItem item in NewItems)
				ItemsMenu.AddItem(item);
		}

		private void ItemsMenu_OnMenuItemSelected(PopupMenu popup, MenuItem selectedItem)
		{
			OnItemSelected?.Invoke(this, selectedItem);
		}


		public Vector2f Dimensions {
			get { return _dimensions; }
			set { _dimensions = value; }
		}
		public float Width {
			get { return _dimensions.x; }
			set { _dimensions.x = value; }
		}

		public override IWidgetView CreateDefaultView()
		{
			return new SimplePopupMenuDialogView(this);
		}



		public virtual bool IsCapturing { get; set; }
		public MenuItem? HoveredItem { get; protected set; } = null;
		public virtual void UpdateCapture(ISimpleCaptureTarget.ECaptureState State, in InputDeviceState deviceState)
		{
			IsCapturing = (State == ISimpleCaptureTarget.ECaptureState.Begin || State == ISimpleCaptureTarget.ECaptureState.Update);
			if (State == ISimpleCaptureTarget.ECaptureState.End) {
				WidgetHitResult hitResult = WidgetHitResult.None;
				if (GetActiveView()?.HitTest(deviceState.CurrentPosition) ?? false) {
					if (EnableClickToDismiss)
						OnDismissDialogClick?.Invoke();
				}
			}
		}
		public virtual void UpdateHover(ISimpleCaptureTarget.EHoverState State, in InputDeviceState deviceState, out bool bContinueHover)
		{
			bContinueHover = false;
		}



		public virtual bool OnKeyChordUpdated(in KeyChord ActiveChord)
		{
			if (ActiveChord.IsSingleSpecialKey(KeyNames.Escape))
			{
				OnDismissDialogClick?.Invoke();     // todo need to probably send this next frame or something?
				return true;
			} 
			else if (ActiveChord.IsSingleSpecialKey(KeyNames.Enter))
			{
				if (ItemsMenu.EnumerateItems().Count() == 1)
					ItemsMenu.ExternalSelectItem(ItemsMenu.EnumerateItems().First());
				else if (ItemsMenu.HighlightedItem != null)
					ItemsMenu.ExternalSelectItem(ItemsMenu.HighlightedItem);
			} else if (ActiveChord.IsSingleSpecialKey(KeyNames.DownArrow))
			{
				ItemsMenu.HighlightNextItem(true);
			} else if (ActiveChord.IsSingleSpecialKey(KeyNames.UpArrow))
			{
				ItemsMenu.HighlightPreviousItem(true);
			}
			return false;
		}


		// populate menu func...

	}





	public class SimplePopupMenuDialogView : WidgetView
	{
		public SimplePopupMenuDialog SourceDialog;

		public AxisAlignedBox2f LocalBounds;
		public Vector2f DrawOrigin;
		public TextRect TextInfo;

		public SimplePopupMenuDialogView(SimplePopupMenuDialog sourceDialog)
		{
			SourceDialog = sourceDialog;
		}

		public override Widget GetWidget() { return SourceDialog; }

		public override AxisAlignedBox2f BoundsQuery(ILayoutAnchor? RelativeToAnchor = null)
		{
			return (RelativeToAnchor != null) ?
				AnchorLocation.GetAnchoredBounds(LocalBounds, RelativeToAnchor, GetWidget().AnchorPlacement) : LocalBounds;
		}

		public override void UpdateLayout(SKStyleCache StyleCache)
		{
			SourceDialog.ItemsMenu.GetActiveView()?.UpdateLayout(StyleCache);

			AxisAlignedBox2f ListBounds = SourceDialog.ItemsMenu.GetActiveView()?.BoundsQuery(null) ?? AxisAlignedBox2f.Empty;

			SourceDialog.ItemsMenuAnchor.Box = new AxisAlignedBox2f(ListBounds);
			SourceDialog.ItemsMenuAnchor.BoxPoint = BoxPoints.TopLeft;
			SourceDialog.ItemsMenuAnchor.Offset = new Vector2f(0, 5);

			AxisAlignedBox2f ItemBounds = SourceDialog.ItemsMenu.GetActiveView()?.BoundsQuery(SourceDialog.ItemsMenu.GetAnchor()) ?? AxisAlignedBox2f.Empty;

			Vector2f Origin = SourceDialog.GetAnchor()?.GetOrigin() ?? Vector2f.Zero;
			//ItemBounds.Translate(-Origin);
			LocalBounds = ItemBounds;
		}

		public override void Draw(SKStyleCache StyleCache, SKCanvas Canvas, ILayoutAnchor Anchor)
		{
			DrawOrigin = Anchor.GetOrigin();
		}


		public override bool HitTest(Vector2f QueryPoint)
		{
			if (SourceDialog.EnableClickToDismiss)
				return true;
			AxisAlignedBox2f WorldBounds =
				AnchorLocation.GetAnchoredBounds(LocalBounds, DrawOrigin, GetWidget().AnchorPlacement);
			return WorldBounds.Contains(QueryPoint);
		}

		public override bool HitQuery(Vector2f QueryPoint, out WidgetHitResult Result)
		{
			Result = new WidgetHitResult();
			if (HitTest(QueryPoint))
			{
				Result = new WidgetHitResult(this, 25);
				return true;
			}
			return false;
		}



	}

}
