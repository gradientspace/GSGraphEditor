// Copyright Gradientspace Corp. All Rights Reserved.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using g3;
using SkiaSharp;

namespace Gradientspace.UI
{
	public class GSUITableLayout : Widget
	{
		protected int NumRows = 2;
		protected int NumColumns = 2;
		Widget?[,] Table = new Widget[2,2];

		public int Rows { get { return NumRows; } }
		public int Columns { get { return NumColumns; } }

		public Vector2f Spacing = new Vector2f(2, 2);
		public Vector2f Border = new Vector2f(4, 4);

		public IWidgetContentExtension? BackgroundExtension { get; set; } = null;


		internal WidgetRelativeBoxAnchor TableAnchor;

		public GSUITableLayout()
		{
			TableAnchor = new WidgetRelativeBoxAnchor(this);
			TableAnchor.BoxPoint = BoxPoints.TopLeft;
		}

		public void SetDimensions(int numRows, int numColumns)
		{
			if (numRows == 0 || numColumns == 0)
				throw new ArgumentOutOfRangeException("Rows and Columns must be > 0");
			if (numRows == NumRows && numColumns == NumColumns)
				return;
			NumRows = numRows;
			NumColumns = numColumns;
			update_table();
		}

		private void update_table()
		{
			int curRows = Table.GetLength(0), curCols = Table.GetLength(1);
			if (NumRows == curRows && NumColumns == curCols)
				return;
			Widget?[,] newTable = new Widget[NumRows, NumColumns];
            for (int r = 0; r < Math.Min(curRows, NumRows); ++r) {
                for (int c = 0; c < Math.Min(curCols, NumColumns); ++c)
                    newTable[r, c] = Table[r, c];
            }
            // remove extra rows
            for (int r = NumRows; r < curRows; ++r ) {
                for (int c = 0; c < curCols; ++c) {
                    RemoveChildWidget(Table[r, c]);
                    Table[r, c] = null;
                }
            }
            // remove extra columns
            for ( int c = NumColumns; c < curCols; ++c) {
                for (int r = 0; r < curRows; ++r ) {
                    RemoveChildWidget(Table[r, c]);
                    Table[r, c] = null;
                }
            }

			Table = newTable;
		}



		public void SetWidget(int row, int col, Widget widget)
		{
			if (row < 0 || row >= NumRows || col < 0 || col >= NumColumns)
				throw new ArgumentOutOfRangeException($"row/column {row},{col} is out of valid range, table size is {NumRows}x{NumColumns}");

			Table[row,col] = widget;
			AddChildWidget(widget);
			widget.AnchorPlacement = new AnchorLocation(BoxPoints.TopLeft);
			widget.AnchorTo(TableAnchor);
		}


		public Widget? GetWidget(int row, int col)
		{
			if (row < 0 || row >= NumRows || col < 0 || col >= NumColumns)
				throw new ArgumentOutOfRangeException($"row/column {row},{col} is out of valid range, table size is {NumRows}x{NumColumns}");
			return Table[row, col];
		}


        public (bool found, int row, int col) FindWidget(Widget widget)
        {
            for (int r = 0; r < NumRows; ++r)
                for (int c = 0; c < NumColumns; ++c)
                    if (Table[r, c] == widget)
                        return new(true, r, c);
            return (false, -1, -1);
        }


		public IEnumerable<(int, int, Widget)> EnumerateWidgets()
		{
			for (int r = 0; r < NumRows; ++r)
				for (int c = 0; c < NumColumns; ++c)
					if (Table[r, c] != null)
						yield return new(r, c, Table[r, c]!);
		}

		public override IWidgetView CreateDefaultView()
		{
			return new GSUITableLayoutView(this);
		}
	}


	public class GSUITableLayoutView : WidgetView
	{
		public GSUITableLayout Source;

		public AxisAlignedBox2f LocalBounds;
		public Vector2f DrawOrigin;

		public GSUITableLayoutView(GSUITableLayout source)
		{
			Source = source;
		}

		public override Widget GetWidget() { return Source; }

		public override AxisAlignedBox2f BoundsQuery(ILayoutAnchor? RelativeToAnchor = null)
		{
			return (RelativeToAnchor != null) ?
				AnchorLocation.GetAnchoredBounds(LocalBounds, RelativeToAnchor, GetWidget().AnchorPlacement) : LocalBounds;
		}

		public override void UpdateLayout(SKStyleCache StyleCache)
		{
			int Rows = Source.Rows, Cols = Source.Columns;
			float[] maxHeights = new float[Rows];
			for (int i = 0; i < Rows; ++i) maxHeights[i] = 0;   // ugh why no easy array init...
			float[] maxWidths = new float[Cols];
			for (int i = 0; i < Cols; ++i) maxWidths[i] = 0;        

			// update layout of all children and get max row/col dimensions
			foreach ((int r, int c, Widget widget) in Source.EnumerateWidgets())
			{
				widget.GetActiveView()?.UpdateLayout(StyleCache);
				AxisAlignedBox2f widgetBounds = widget.GetActiveView()?.BoundsQuery(null) ?? AxisAlignedBox2f.Empty;
				maxHeights[r] = Math.Max(widgetBounds.Height, maxHeights[r]);
				maxWidths[c] = Math.Max(widgetBounds.Width, maxWidths[c]);
			}

			float TotalWidth = maxWidths.Sum(), TotalHeight = maxHeights.Sum();

			float BorderX = Source.Border.x, BorderY = Source.Border.y;
			float SpacingX = Source.Spacing.x, SpacingY = Source.Spacing.y;
			LocalBounds = new AxisAlignedBox2f(0, 0, 
				TotalWidth + (Cols-1)*SpacingX + 2*BorderX, 
				TotalHeight + (Rows-1)*SpacingY + 2*BorderY);

			float offsetX = BorderX, offsetY = BorderY;
			for ( int r = 0; r < Rows; ++r ) 
			{
				offsetX = BorderY;
				for (int c = 0; c < Cols; ++c)
				{
					float left = offsetX;
					float top = offsetY;
					Widget? widget = Source.GetWidget(r, c);
					if ( widget != null )
					{
						widget.AnchorPlacement = new AnchorLocation(BoxPoints.TopLeft) { Offset = new Vector2f(left, top) };
						
						// should not need to update layout of widget again as layout should always be in local space??
						//widget.GetActiveView()?.UpdateLayout(StyleCache);
					}
					offsetX += maxWidths[c] + SpacingX;
				}
				offsetY += maxHeights[r] + SpacingY;
			}

            // update the box used for the table anchor
            Source.TableAnchor.UpdateFromParentWidget();
        }


		public override void Draw(SKStyleCache StyleCache, SKCanvas Canvas, ILayoutAnchor Anchor)
		{
			// should do this via a
			// draw a background?
			DrawOrigin = Anchor.GetOrigin();
			AxisAlignedBox2f PlacedBounds = AnchorLocation.MakeRelativeToAnchor(LocalBounds, Source.AnchorPlacement, DrawOrigin);

			if (Source.BackgroundExtension != null)
				Source.BackgroundExtension.DrawContent(Source, StyleCache, Canvas, PlacedBounds, false);

            // debug drawing...
            //Canvas.DrawRect(Conversion.ToSkia(PlacedBounds), new SKPaint() { Color = SKColors.LightGray, IsStroke = true });
            //Canvas.DrawCircle(Conversion.ToSkia(DrawOrigin), 5.0f, new SKPaint() { Color = SKColors.Red });
        }



        public override bool HitTest(Vector2f QueryPoint)
		{
			return false;
		}

		public override bool HitQuery(Vector2f QueryPoint, out WidgetHitResult Result)
		{
			Result = WidgetHitResult.None;
			return false;
		}
	}


}
