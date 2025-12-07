// Copyright Gradientspace Corp. All Rights Reserved.
using System;
using System.Diagnostics;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using Avalonia.Threading;
using Avalonia.Utilities;
using SkiaSharp;

using GSNodeEditor;
using Gradientspace.UI;
using System.Threading;
using Gradientspace.NodeGraph;
using Avalonia.Win32.Input;
using Avalonia.Interactivity;
using Mujoco.Nodes;
using Gradientspace.NodeGraph.Util;
//using Meshmixer.Nodes;



namespace GraphEditorAppV2
{
	// some pages on trying to get a SKCanvas for an avalonia control/panel:
	//   https://github.com/AvaloniaUI/Avalonia/discussions/12269
	//
	//   https://github.com/AvaloniaUI/Avalonia.Labs/blob/main/src/Avalonia.Labs.Controls/SKCanvasView/SKCanvasView.cs
	//   SKCanvasView from Avalonia Labs - could not figure out how to get this to work...maybe a better choice though?
	//
	//   https://github.com/AvaloniaUI/Avalonia/blob/a6e936d74accdcf7ead6b776f374defb501e4434/samples/RenderDemo/Pages/CustomSkiaPage.cs
	//   (this is what the class below is derived from)
	//
	public class CustomSkiaPage : 
		Control,
		ICustomHitTest      /* provides HitTest() function which lets us capture mouse for entire area */
	{

		// hax
		protected NodeGraphViewport GraphView;
		protected InputDeviceState RawDeviceState;

		private readonly GlyphRun _noSkia;
        public CustomSkiaPage()
        {
            ClipToBounds = true;
            var text = "Current rendering API is not Skia";
            var glyphs = text.Select(ch => Typeface.Default.GlyphTypeface.GetGlyph(ch)).ToArray();
            _noSkia = new GlyphRun(Typeface.Default.GlyphTypeface, 12, text.AsMemory(), glyphs);

			this.IsEnabled = true;
			this.Focusable = true;
		}


		public void InitializeGraph()
		{
            // initialize here so that setup code can log things
            DebugManager.GlobalEnableGraphDebugging = true;
            GlobalGraphOutput.SetCurrentOutput(new DefaultGraphOutputImpl());

            Gradientspace.NodeGraph.SecretsSource.InitializeSecretsSource(new NodeEditorSecretsImpl());

            // load node libraries from settings file
            NodeLibraryUtils.FindAndLoadNodeLibraries(NodeEditorConfig.NodeLibraryPaths);


            GraphView = new NodeGraphViewport();
			GraphView.Initialize();
			// NodeGraphViewport by default sets a kinda hacky backdoor key handler for space key.
			// Remove that in this usage (better fix tbd)
			SystemKeyboardRouter.Instance.OnNewPressedKeyFunc = null;
            SystemKeyboardRouter.Instance.OnTextCopied += KeyboardRouter_OnTextCopied;

        }

        public NodeGraphViewport ActiveViewport { get { return GraphView; } }


		// ICustomHitTest impl - always hit?
		public bool HitTest(Point point) => true;

		//
		// pointer event forwarding
		//

		bool bLeftDown = false;
		bool bRightDown = false;
		bool bMiddleDown = false;


		protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
		{
			if (GraphView.InteractionManager.IsCapturingInput)
				release_pointer(true);
			base.OnPointerCaptureLost(e);
		}

		protected override void OnPointerEntered(PointerEventArgs e)
		{
			//release_pointer(false);		// we can keep capture even if pointer enters/exits
			base.OnPointerEntered(e);
		}
		protected override void OnPointerExited(PointerEventArgs e)
		{
			//release_pointer(true);		// we can keep capture even if pointer enters/exits
			base.OnPointerEntered(e);
		}

		void release_pointer(bool bSendPointerUp)
		{
			bool bReleased = false;
			if (bLeftDown) {
				RawDeviceState.LeftButton.SetReleased();
				bLeftDown = false; bReleased = true;
			} else if (bMiddleDown) {
				RawDeviceState.MiddleButton.SetReleased();
				bMiddleDown = false; bReleased = true;
			} else if (bRightDown ) {
				RawDeviceState.RightButton.SetReleased();
				bRightDown = false; bReleased = true;
			}

			if (bReleased && bSendPointerUp)
				GraphView.OnPointerUp(RawDeviceState);
		}


		protected override void OnPointerPressed(PointerPressedEventArgs e)
		{
			Point mousePos = e.GetCurrentPoint(this).Position;
			RawDeviceState.CurrentPosition = new g3.Vector2f(mousePos.X, mousePos.Y);

			// look into 

			bool bHaveDownEvent = false;
			if (e.Properties.IsLeftButtonPressed)
			{
				Debug.Assert(bLeftDown == false);
				if (bMiddleDown == false && bRightDown == false) {
					RawDeviceState.LeftButton.SetPressed();
					bLeftDown = true;
					bHaveDownEvent = true;
				}
			} 
			else if (e.Properties.IsMiddleButtonPressed)
			{
				Debug.Assert(bMiddleDown == false);
				if (bLeftDown == false && bRightDown == false)
				{
					RawDeviceState.MiddleButton.SetPressed();
					bMiddleDown = true;
					bHaveDownEvent = true;
				}
			} 
			else if (e.Properties.IsRightButtonPressed)
			{
				Debug.Assert(bRightDown == false);
				if (bLeftDown == false && bMiddleDown == false)
				{
					RawDeviceState.RightButton.SetPressed();
					bRightDown = true;
					bHaveDownEvent = true;
				}
			}

			RawDeviceState.CtrlButton.SetDown(e.KeyModifiers.HasFlag(KeyModifiers.Control));
			RawDeviceState.ShiftButton.SetDown(e.KeyModifiers.HasFlag(KeyModifiers.Shift));

			if (bHaveDownEvent) {
				GraphView.OnPointerDown(RawDeviceState);
				e.Handled = true;
			}

			base.OnPointerPressed(e);
		}

		protected override void OnPointerReleased(PointerReleasedEventArgs e)
		{
			Point mousePos = e.GetCurrentPoint(this).Position;
			RawDeviceState.CurrentPosition = new g3.Vector2f(mousePos.X, mousePos.Y);

			//e.InitialPressMouseButton;  // ?

			bool bHaveUpEvent = false;
			if ( bLeftDown )
			{
				if ( e.Properties.IsLeftButtonPressed == false )
				{
					RawDeviceState.LeftButton.SetReleased();
					bLeftDown = false;
					bHaveUpEvent = true;
				}
			} 
			else if (bMiddleDown)
			{
				if (e.Properties.IsMiddleButtonPressed == false)
				{
					RawDeviceState.MiddleButton.SetReleased();
					bMiddleDown = false;
					bHaveUpEvent = true;
				}
			} 
			else if (bRightDown)
			{
				if (e.Properties.IsRightButtonPressed == false)
				{
					RawDeviceState.RightButton.SetReleased();
					bRightDown = false;
					bHaveUpEvent = true;
				}
			}

			RawDeviceState.CtrlButton.SetDown(e.KeyModifiers.HasFlag(KeyModifiers.Control));
			RawDeviceState.ShiftButton.SetDown(e.KeyModifiers.HasFlag(KeyModifiers.Shift));

			if (bHaveUpEvent) {
				GraphView.OnPointerUp(RawDeviceState);
				e.Handled = true;
			}

			base.OnPointerReleased(e);
		}



		protected override void OnPointerMoved(PointerEventArgs e)
		{
			Point mousePos = e.GetCurrentPoint(this).Position;
			RawDeviceState.CurrentPosition = new g3.Vector2f(mousePos.X, mousePos.Y);

			// update down state in case it has changed?
			// (tbh this should never happen right?)
			if (bLeftDown)
				RawDeviceState.LeftButton.SetDown( e.Properties.IsLeftButtonPressed );
			else if (bMiddleDown)
				RawDeviceState.MiddleButton.SetDown(e.Properties.IsMiddleButtonPressed);
			else if (bRightDown)
				RawDeviceState.RightButton.SetDown(e.Properties.IsRightButtonPressed);

			RawDeviceState.CtrlButton.SetDown(e.KeyModifiers.HasFlag(KeyModifiers.Control));
			RawDeviceState.ShiftButton.SetDown(e.KeyModifiers.HasFlag(KeyModifiers.Shift));
			RawDeviceState.AltButton.SetDown(e.KeyModifiers.HasFlag(KeyModifiers.Alt));

			GraphView.UpdateCursor(RawDeviceState);

			e.Handled = true;
			base.OnPointerMoved(e);
		}




		protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
		{
			Point mousePos = e.GetCurrentPoint(this).Position;
			RawDeviceState.CurrentPosition = new g3.Vector2f(mousePos.X, mousePos.Y);

			RawDeviceState.CtrlButton.SetDown(e.KeyModifiers.HasFlag(KeyModifiers.Control));
			RawDeviceState.ShiftButton.SetDown(e.KeyModifiers.HasFlag(KeyModifiers.Shift));
			RawDeviceState.AltButton.SetDown(e.KeyModifiers.HasFlag(KeyModifiers.Alt));

			RawDeviceState.WheelDelta = (float)e.Delta.Y;
			GraphView.OnWheel(RawDeviceState);
			RawDeviceState.WheelDelta = 0;

			e.Handled = true;
			base.OnPointerWheelChanged(e);
		}



		//
		// key even forwarding
		//

		protected string? ToKeyString(KeyEventArgs e)
		{
			if (e.KeySymbol != null)
				return e.KeySymbol;
			string s = e.Key.ToString();
			if (Char.IsAsciiLetterOrDigit(s[0]))
				return s;
			return null;
		}

		protected override void OnKeyDown(KeyEventArgs e)
		{
			if (e.Key == Key.LeftCtrl) {
				RawDeviceState.CtrlButton.SetPressed();
			}  else if (e.Key == Key.LeftShift) {
				RawDeviceState.ShiftButton.SetPressed();
			}  else if (e.Key == Key.LeftAlt) {
				RawDeviceState.AltButton.SetPressed();
			}

			KeyState keyState = ConvertToKeyState(e.Key, ToKeyString(e), RawDeviceState);
			//Debug.WriteLine($"[OnKeyDown] {keyState}");

			if (keyState.IsKnownKey)
				e.Handled = SystemKeyboardRouter.Instance.OnRawKeyDown(keyState);

			InvalidateVisual();
			base.OnKeyDown(e);
		}
		protected override void OnKeyUp(KeyEventArgs e)
		{
			if (e.Key == Key.LeftCtrl) {
				RawDeviceState.CtrlButton.SetReleased();
			}  else if (e.Key == Key.LeftShift) {
				RawDeviceState.ShiftButton.SetReleased();
			}  else if (e.Key == Key.LeftAlt) {
				RawDeviceState.AltButton.SetReleased();
			}

			KeyState keyState = ConvertToKeyState(e.Key, ToKeyString(e), RawDeviceState);
			//Debug.WriteLine($"[OnKeyUp] {keyState}");

			if (keyState.IsKnownKey)
				e.Handled = SystemKeyboardRouter.Instance.OnRawKeyUp(keyState);

			InvalidateVisual();
			base.OnKeyUp(e);
		}

		protected override void OnTextInput(TextInputEventArgs e)
		{
			if (e.Text != null)
			{
				KeyState keyState = KeyState.MakeKeyStateFromCharacter(e.Text[0], RawDeviceState);
				//Debug.WriteLine($"[OnTextInput] {keyState}");

				if (keyState.IsKnownKey)
					e.Handled = SystemKeyboardRouter.Instance.OnCharacter(keyState);
			}

			InvalidateVisual();
			base.OnTextInput(e);
		}


        protected override void OnGotFocus(GotFocusEventArgs e)
        {
            base.OnGotFocus(e);
            GraphView.OnBeginFocus();
        }

        protected override void OnLostFocus(RoutedEventArgs e)
        {
            base.OnLostFocus(e);
            GraphView.OnEndFocus();
        }


		static public KeyState ConvertToKeyState(Key key, string? keyString, InputDeviceState deviceState)
		{
			KeyState keyState = KeyState.Unknown;

			switch (key)
			{
				case Key.Escape: keyState = KeyState.Escape; break;
				case Key.Return: keyState = KeyState.Enter; break;
				case Key.Back: keyState = KeyState.Backspace; break;
				case Key.Delete: keyState = KeyState.Delete; break;
				case Key.Tab: keyState = KeyState.Tab; break;
				case Key.Space: keyState = KeyState.Space; break;

				case Key.LeftShift: keyState = KeyState.Shift; break;
				case Key.LeftAlt: keyState = KeyState.Alt; break;
				case Key.LeftCtrl: keyState = KeyState.Ctrl; break;

				case Key.Left: keyState = KeyState.LeftArrow; break;
				case Key.Right: keyState = KeyState.RightArrow; break;
				case Key.Up: keyState = KeyState.UpArrow; break;
				case Key.Down: keyState = KeyState.DownArrow; break;
                case Key.Home: keyState = KeyState.Home; break;
                case Key.End: keyState = KeyState.End; break;

                default:
				{
					char Character = (keyString != null) ? keyString[0] : '\0';
					if (Char.IsControl(Character) == false)
					{
						keyState.KeyType = KeyType.CharacterKey;
						keyState.KeyName = KeyNames.Unnamed;
						keyState.Character = Character;
					}
				}
				break;
			}

			keyState.ConfigureModifiers(deviceState);

			return keyState;
		}

        private void KeyboardRouter_OnTextCopied(KeyboardRouter sender, string NewCopiedText)
        {
            // TODO: had to disable Wait() here because it was causing a hang on ctrl+c inside node text entry fields...
            TopLevel? topLevel = TopLevel.GetTopLevel(this);
            if (topLevel != null && topLevel.Clipboard != null)
                topLevel.Clipboard.SetTextAsync(NewCopiedText);
                //topLevel.Clipboard.SetTextAsync(NewCopiedText).Wait();
        }



        class SkiaCanvasDrawOp : ICustomDrawOperation
        {
			//static int FrameCounter = 0;

            private readonly IImmutableGlyphRunReference? _noSkia;

			CustomSkiaPage? PageRef = null;

			public SkiaCanvasDrawOp(Rect bounds, GlyphRun noSkia, CustomSkiaPage skiaPage)
            {
                _noSkia = noSkia.TryCreateImmutableGlyphRunReference();
                Bounds = bounds;
				PageRef = skiaPage;
			}
            
            public void Dispose()
            {
                // No-op
            }

            public Rect Bounds { get; }
            public bool HitTest(Point p) => false;
            public bool Equals(ICustomDrawOperation? other) => false;
            static Stopwatch St = Stopwatch.StartNew();
            public void Render(ImmediateDrawingContext context)
            {
				var leaseFeature = context.TryGetFeature<ISkiaSharpApiLeaseFeature>();
				if (leaseFeature == null)
					context.DrawGlyphRun(Brushes.Black, _noSkia!);
				else
				{
					using var lease = leaseFeature.Lease();
					var canvas = lease.SkCanvas;

					canvas.Save();
					if (PageRef != null)
						PageRef.DoSKRepaint(canvas);
					canvas.Restore();
				}

				//int newVal = Interlocked.Increment(ref FrameCounter);
				//System.Diagnostics.Debug.WriteLine($"Render {newVal}");

				// run PostSKRepaint on main thread now that draw is finished
				Dispatcher.UIThread.InvokeAsync(() => {
					PageRef?.PostSKRepaint();

					// possibly invalidate frame if we are animating
					bool bForceImmediateRepaint = DebugManager.Instance.IsDebugging
						|| SystemKeyboardRouter.Instance.HasTextEntryFocusTarget;
					//bForceImmediateRepaint = true;
					if (bForceImmediateRepaint)
						PageRef?.InvalidateVisual();

				}, DispatcherPriority.MaxValue);
			}
        }



		// render thing calls this to forward draw to SKCanvas....is that a good idea??
		internal void PreSKRepaint() {
			GraphView.PreDraw();
		}
		internal void DoSKRepaint(SKCanvas canvas) {
			GraphView.Repaint(canvas);
		}
		internal void PostSKRepaint() {
			GraphView.PostDraw();
		}


		public override void Render(DrawingContext context)
        {
			if (GraphView == null)
				return;

			// run pre-repaint on main thread (could run on draw thread?? or separate thread and await the task inside the DrawOp?)
			PreSKRepaint();

			// add custom draw operation which will run in render thread
			// (do we need to allocate a new one each frame??)
			context.Custom(new SkiaCanvasDrawOp(new Rect(0, 0, Bounds.Width, Bounds.Height), _noSkia, this));

			// invalidate visual which will force next render
			// (doing this in other ways now)
			//Dispatcher.UIThread.InvokeAsync(InvalidateVisual, DispatcherPriority.Render);
		}



		// some skshader code to keep around as a reminder...
		//var sx = Animate(100, 2, 10);
		//var sy = Animate(1000, 5, 15);
		//var lightPosition = new SKPoint(
		//	(float)(Bounds.Width / 2 + Math.Cos(St.Elapsed.TotalSeconds) * Bounds.Width / 4),
		//	(float)(Bounds.Height / 2 + Math.Sin(St.Elapsed.TotalSeconds) * Bounds.Height / 4));
		//using (var sweep =
		//	SKShader.CreateSweepGradient(new SKPoint((int)Bounds.Width / 2, (int)Bounds.Height / 2), colors,
		//		null))
		//using (var turbulence = SKShader.CreatePerlinNoiseFractalNoise(0.05f, 0.05f, 4, 0))
		//using (var shader = SKShader.CreateCompose(sweep, turbulence, SKBlendMode.SrcATop))
		//using (var blur = SKImageFilter.CreateBlur(Animate(100, 2, 10), Animate(100, 5, 15)))
		//using (var paint = new SKPaint {
		//	Shader = shader,
		//	ImageFilter = blur
		//})
		//	canvas.DrawPaint(paint);

		//using (var pseudoLight = SKShader.CreateRadialGradient(
		//	lightPosition,
		//	(float)(Bounds.Width/3),
		//	new[] {
		//		new SKColor(255, 200, 200, 100),
		//		SKColors.Transparent,
		//		new SKColor(40,40,40, 220),
		//		new SKColor(20,20,20, (byte)Animate(100, 200,220)) },
		//	new float[] { 0.3f, 0.3f, 0.8f, 1 },
		//	SKShaderTileMode.Clamp))
		//using (var paint = new SKPaint {
		//	Shader = pseudoLight
		//})
		//	canvas.DrawPaint(paint);


	}
}
