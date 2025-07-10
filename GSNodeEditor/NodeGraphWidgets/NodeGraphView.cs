// Copyright Gradientspace Corp. All Rights Reserved.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using g3;
using Gradientspace.NodeGraph;
using Gradientspace.UI;
using SkiaSharp;

namespace GSNodeEditor
{
    public class NodeGraphView : INodeGraphLayoutProvider
    {
        protected INodeGraph? SourceGraph;
        public INodeGraph GetGraph() { return SourceGraph!; }

        protected List<NodeWidget> Nodes;
        protected List<NodeWidget> HighlightNodes;

        protected List<ConnectionView> DataConnections;
        protected List<ConnectionView> SequenceConnections;

        SimpleWidgetSource ActiveWidgetSet;

        NodeWidget? HoveredNode;

        NodeWidget? DraggingNode;
        Vector2f DragStartPosition;
        Vector2f InitialNodePosition;

        SKPaint NodeLabelTextPaint;
        Vector2f LabelMargins;

        SKPaint PinLabelTextPaint;

        SKPaint DataConnectionCurvePaint;
		SKPaint DataConnectionErrorCurvePaint;
		SKPaint SequenceConnectionCurvePaint;
        public float ConnectionTangentLen = 200;

        public delegate void NewNodeEventHandler(object? sender, NodeWidget newNodeWidget);
        public event NewNodeEventHandler? OnNewNodeAdded;
        public event NewNodeEventHandler? OnExistingNodeUpdated;

        public WeakReference<INodeGraphEditManager> ActiveEditManager;

        public NodeGraphView()
        {
            Nodes = new List<NodeWidget>();
            HighlightNodes = new List<NodeWidget>();
            HoveredNode = null;

            DataConnections = new List<ConnectionView>();
            SequenceConnections = new List<ConnectionView>();

            ActiveWidgetSet = new SimpleWidgetSource();

            LabelMargins = new(4.0f, 2.0f);
            NodeLabelTextPaint = new()
            {
                Color = SKColors.White,
                IsAntialias = true,
                LcdRenderText = true,
                SubpixelText = true,
                TextSize = 16,
                Typeface = SKTypeface.FromFamilyName(
                    familyName: "Calibri",
                    weight: SKFontStyleWeight.Light, width: SKFontStyleWidth.Condensed, slant: SKFontStyleSlant.Upright)
            };
            PinLabelTextPaint = new()
            {
                Color = SKColors.White,
                IsAntialias = true,
                LcdRenderText = true,
                SubpixelText = true,
                TextSize = 14,
                Typeface = SKTypeface.FromFamilyName(
                    familyName: "Calibri",
                    weight: SKFontStyleWeight.Light, width: SKFontStyleWidth.Condensed, slant: SKFontStyleSlant.Upright)
            };

            DataConnectionCurvePaint = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 3,
                IsAntialias = true,
                Color = SKColors.Black
            };
            DataConnectionErrorCurvePaint = new SKPaint {
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 5,
                IsAntialias = true,
                Color = SKColors.OrangeRed,
                PathEffect = SKPathEffect.CreateDash(new float[] { 5, 5 }, 20)
            };
			SequenceConnectionCurvePaint = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 4,
                IsAntialias = true,
                Color = SKColors.White
            };
        }

        public float DefaultNodeWidth { get; set; } = 25;
        public float DefaultNodeHeight { get; set; } = 5;


        public IWidgetSource WidgetSource { get { return ActiveWidgetSet; } }

        public NodeWidget CreateAndInitializeNewNodeWidget(INodeInfo nodeInfo)
        {
            string Label = nodeInfo.Node!.GetNodeName();

            Type nodeClassType = nodeInfo.Node.GetType();
            INodeWidgetProvider? FoundProvider = NodeWidgetCustomizationSystem.Instance.FindProvider(nodeClassType);
            NodeWidget? CustomWidget = FoundProvider?.CreateNewWidget(this, nodeInfo) ?? null;

            NodeWidget NewNodeWidget = (CustomWidget != null) ? CustomWidget : new NodeWidget(this, nodeInfo);

            NewNodeWidget.Label = Label;
            float NodeWidthFromLabel = (Label.Length > 0) ? (NodeLabelTextPaint.MeasureText(Label) + 2 * LabelMargins.x) : 0;
            float UseWidth = MathF.Max(NodeWidthFromLabel, DefaultNodeWidth);
            NewNodeWidget.Size = new(UseWidth, DefaultNodeHeight);
            Nodes.Add(NewNodeWidget);

            ActiveWidgetSet.AddRootWidget(NewNodeWidget);

            NewNodeWidget.InitializeFromNode(nodeInfo);
            NewNodeWidget.UpdateAllInlineInfo(SourceGraph!);

            UpdateSequencePins(NewNodeWidget);

            // todo this should be supported by interfaces...
            NodeBase? FoundNode = (nodeInfo.Node as NodeBase);
            if (FoundNode != null)
                FoundNode!.OnNodeModified += OnNodeModified;

            OnNewNodeAdded?.Invoke(this, NewNodeWidget);

            return NewNodeWidget;
        }
        protected void UpdateSequencePins(NodeWidget nodeWidget)
        {
            if ((SourceGraph is ExecutionGraph) == false) return;

            bool bHasInputs = true; // (NewNodeWidget.InputWidgets.Count > 0);
            if (nodeWidget.IsPlaceholderNode)
                nodeWidget.SetStandardSequencePinsEnabled(false, false);
            else if (nodeWidget.ParentNode is SequenceStartNode)
                nodeWidget.SetStandardSequencePinsEnabled(false, true);
            else if (nodeWidget.ParentNode is ControlFlowNode)
                nodeWidget.SetStandardSequencePinsEnabled(true, false);
            else
                nodeWidget.SetStandardSequencePinsEnabled(bHasInputs, bHasInputs);
        }

        public IEnumerable<NodeWidget> NodeWidgets { get { return Nodes; } }


        public NodeWidget? FindNode(int GraphNodeIdentifier)
        {
            return Nodes.Find(n => n.GraphNodeIdentifier == GraphNodeIdentifier); 
        }
        public NodeWidget? FindNode(INode Node)
        {
            return Nodes.Find(n => n.ParentNodeInfo.Node == Node);
        }

        //! warning: this does not remove connections to this node!
        public bool RemoveNode(int GraphNodeIdentifier)
        {
            int FoundIndex = Nodes.FindIndex(n => n.GraphNodeIdentifier == GraphNodeIdentifier);
            if (FoundIndex >= 0)
            {
                Widget w = Nodes[FoundIndex];
                ActiveWidgetSet.RemoveRootWidget(w);
                Nodes.RemoveAt(FoundIndex);
                w.Dispose();
                return true;
            }
            return false;
        }

        // called when a node's pins change, eg inputs/outputs added/removed or type changes
        protected virtual void OnNodeModified(NodeBase node)
        {
            NodeWidget? FoundWidget = FindNode(node);
            if ( FoundWidget != null )
            {
                FoundWidget.Reinitialize();
                FoundWidget.UpdateAllInlineInfo(SourceGraph!);

                UpdateSequencePins(FoundWidget);

                OnExistingNodeUpdated?.Invoke(this, FoundWidget);
            }
        }

        public ConnectionView? AddConnection(IConnectionInfo connectionInfo)
        {
            ConnectionView NewConnection = new ConnectionView();
            if ( NewConnection.InitializeFromConnection(connectionInfo, this) )
            {
                if (connectionInfo.ConnectionType == EConnectionType.Data)
                    DataConnections.Add(NewConnection);
                else
                    SequenceConnections.Add(NewConnection);

                OnConnectionModified(connectionInfo);

                return NewConnection;
            }
            return null;
        }

        public ConnectionView? FindConnection(IConnectionInfo connectionInfo)
        {
            return (connectionInfo.ConnectionType == EConnectionType.Data) ?
                DataConnections.Find(c => c.ConnectionInfo == connectionInfo) :
                SequenceConnections.Find(c => c.ConnectionInfo == connectionInfo);
        }

		public ConnectionView? FindConnectionByID(int ConnectionID)
		{
            EConnectionType connectionType = ConnectionView.GetConnectionTypeFromID(ConnectionID);
			return (connectionType == EConnectionType.Data) ?
				DataConnections.Find(c => c.ConnectionID == ConnectionID) :
				SequenceConnections.Find(c => c.ConnectionID == ConnectionID);
		}


		public bool RemoveConnection(IConnectionInfo connectionInfo)
        {
            List<ConnectionView> UseList = (connectionInfo.ConnectionType == EConnectionType.Data) ? DataConnections : SequenceConnections;
            int FoundIndex = UseList.FindIndex(c => c.ConnectionInfo == connectionInfo);
            if ( FoundIndex >= 0 )
            {
                UseList.RemoveAt( FoundIndex );
                OnConnectionModified(connectionInfo);
                return true;
            }
            return false;
        }


        protected void OnConnectionModified(IConnectionInfo connectionInfo)
        {
            if (connectionInfo.ConnectionType == EConnectionType.Data)
            {
                NodeWidget? foundWidget = FindNode(connectionInfo.ToNodeIdentifier);
                if ( foundWidget != null )
                {
                    //foundWidget.UpdateAllInlineInfo(this.SourceGraph!);
                    foundWidget.UpdateInlineInfo(this.SourceGraph!, connectionInfo.ToNodeInputName);
                }
            }
            else if (connectionInfo.ConnectionType == EConnectionType.Sequence)
            {
                NodeWidget? foundFromWidget = FindNode(connectionInfo.FromNodeIdentifier);
                NodeWidget? foundToWidget = FindNode(connectionInfo.ToNodeIdentifier);
                foundFromWidget?.UpdateSequenceInlineInfo(this.SourceGraph!);
                foundToWidget?.UpdateSequenceInlineInfo(this.SourceGraph!);
            }
        }




        public void ConnectToGraph(INodeGraph graph)
        {
            SourceGraph = graph;

            float StartX = 100; float StartY = 200;
            float WidthStep = 150;
            float HeightStep = 50;

            int NodeCounter = 0;
            foreach (INodeInfo NodeInfo in SourceGraph.EnumerateNodes())
            {
                NodeWidget NewNodeWidget = CreateAndInitializeNewNodeWidget(NodeInfo);
                NewNodeWidget.Position = new Vector2f(StartX + NodeCounter * WidthStep, StartY + NodeCounter * HeightStep);
                NodeCounter++;
            }

            foreach (IConnectionInfo connectionInfo in SourceGraph.EnumerateConnections(EConnectionType.Data)) {
                ConnectionView? NewConnection = AddConnection(connectionInfo);
                if (NewConnection == null)
                    throw new Exception("SourceGraph.ConnectToGraph: failed to add connection!");
            }
            foreach (IConnectionInfo connectionInfo in SourceGraph.EnumerateConnections(EConnectionType.Sequence)) {
                ConnectionView? NewConnection = AddConnection(connectionInfo);
                if (NewConnection == null)
                    throw new Exception("SourceGraph.ConnectToGraph: failed to add connection!");
            }

			// run full-graph validation...
			UpdateAllConnections();
		}


        // update cached ConnectionState in each ConnectionView by querying the graph
        public void UpdateAllConnections()
        {
            foreach (ConnectionView c in DataConnections)
                c.ConnectionState = SourceGraph!.GetConnectionState(c.ConnectionInfo);
			foreach (ConnectionView c in SequenceConnections)
				c.ConnectionState = SourceGraph!.GetConnectionState(c.ConnectionInfo);
		}


		public bool HaveHoverHit
        {
            get { return HoveredNode != null; }
        }


        public bool OnBeginDrag(Vector2f CursorPosition)
        {
            if (HaveHoverHit)
            {
                DraggingNode = HoveredNode;
                DragStartPosition = CursorPosition;
                InitialNodePosition = DraggingNode!.Position;
                return true;
            }
            return false;
        }
        public bool OnEndDrag()
        {
            if (InDragAction)
            {
                DraggingNode = null;
                return true;
            }
            return false;
        }
        public bool InDragAction
        {
            get { return DraggingNode != null; }
        }
        public void OnUpdateDrag(Vector2f NewCursorPosition)
        {
            if (InDragAction)
            {
                Vector2f Delta = NewCursorPosition - DragStartPosition;
                Vector2f NewNodePosition = InitialNodePosition + Delta;
                DraggingNode!.Position = NewNodePosition;
            }
        }


        public void UpdateLayout()
        {
            int DrawOrderIndex = 1;
            foreach (ConnectionView Connection in DataConnections) {
                Connection.UpdateLayout(ConnectionTangentLen);
                Connection.DrawOrderIndex = DrawOrderIndex++;
            }
			foreach (ConnectionView Connection in SequenceConnections) { 
                Connection.UpdateLayout(ConnectionTangentLen);
                Connection.DrawOrderIndex = DrawOrderIndex++;
            }
        }

        public void Draw(SKCanvas Canvas)
        {
            foreach (ConnectionView Connection in DataConnections) {
                if ( Connection.ConnectionState == EConnectionState.OK) { 
                    DrawConnection(Canvas, Connection, DataConnectionCurvePaint);
                } else { 
					DrawConnection(Canvas, Connection, DataConnectionErrorCurvePaint);
                    DrawConnectionError(Canvas, Connection);
				}
			}
            foreach (ConnectionView Connection in SequenceConnections)
                DrawConnection(Canvas, Connection, SequenceConnectionCurvePaint);
        }

        protected void DrawConnection(SKCanvas Canvas, ConnectionView Connection, SKPaint UsePaint)
        {
            SKPath Curve = new SKPath();
            Curve.MoveTo(Conversion.ToSkia(Connection.StartPoint));
            Curve.CubicTo(Conversion.ToSkia(Connection.StartTangentPoint), Conversion.ToSkia(Connection.EndTangentPoint), Conversion.ToSkia(Connection.EndPoint));
            Canvas.DrawPath(Curve, UsePaint);

            // draw bounding box (debug)
            //AxisAlignedBox2d bounds = SkiaUtil.SkiaCubicBounds(Connection.StartPoint, Connection.StartTangentPoint, Connection.EndTangentPoint, Connection.EndPoint);
            //Canvas.DrawRect((float)bounds.Min.x, (float)bounds.Min.y, (float)bounds.Width, (float)bounds.Height, new SKPaint() { Color = SKColors.Black, StrokeWidth = 1, IsStroke = true });
		}


        public void DebugDraw(SKCanvas Canvas, Vector2d CursorPosition)
        {
            // temp - draw line to nearest-point on each sequence curve
			//foreach (ConnectionView Connection in SequenceConnections)
			//{
			//	Vector2d NearestPt = SkiaUtil.SkiaCubicNearestPoint(Connection.StartPoint, Connection.StartTangentPoint, Connection.EndTangentPoint, Connection.EndPoint, CursorPosition);
			//	Canvas.DrawLine(Conversion.ToSkia(CursorPosition), Conversion.ToSkia(NearestPt), DataConnectionErrorCurvePaint);
			//}
		}

        protected void DrawConnectionError(SKCanvas Canvas, ConnectionView Connection)
        {
			// draw an X at the midpoint of the curve
			//Vector2f CurveCenterPt = SkiaUtil.SkiaCubicPoint(Connection.StartPoint, Connection.StartTangentPoint, Connection.EndTangentPoint, Connection.EndPoint, 0.5f);
			//float l = 5;
			//Vector2f a = CurveCenterPt - new Vector2f(l), b = CurveCenterPt + new Vector2f(l);
			//Canvas.DrawLine(Conversion.ToSkia(a), Conversion.ToSkia(b), SequenceConnectionCurvePaint);
			//Vector2f c = CurveCenterPt + new Vector2f(-l, l), d = CurveCenterPt + new Vector2f(l, -l);
			//Canvas.DrawLine(Conversion.ToSkia(c), Conversion.ToSkia(d), SequenceConnectionCurvePaint);
		}


		public ConnectionView? ConnectionHitTest(Vector2d CursorPosition, out WidgetHitResult hitResult, Predicate<ConnectionView>? PredicateFunc = null)
		{
            double MaxHitDist = 5;      // temp...
            ConnectionView? Nearest = null;
			foreach (ConnectionView Connection in DataConnections) {
                if (PredicateFunc != null && PredicateFunc(Connection) == false)
                    continue;
                if ( SkiaUtil.SkiaCubicHitTest(Connection.StartPoint, Connection.StartTangentPoint, Connection.EndTangentPoint, Connection.EndPoint, 
                        CursorPosition, out double Distance, MaxHitDist) ) {
                    Nearest = Connection;
                    MaxHitDist = Distance;
				}
			}
            foreach (ConnectionView Connection in SequenceConnections) {
				if (PredicateFunc != null && PredicateFunc(Connection) == false)
					continue;
				if ( SkiaUtil.SkiaCubicHitTest(Connection.StartPoint, Connection.StartTangentPoint, Connection.EndTangentPoint, Connection.EndPoint, 
                        CursorPosition, out double Distance, MaxHitDist) ) {
                    Nearest = Connection;
                    MaxHitDist = Distance;
				}
            }
            hitResult = (Nearest != null) ? new WidgetHitResult(Nearest, Nearest.DrawOrderIndex + 1000) : new WidgetHitResult();
            return Nearest;
		}





		public IEnumerable<NodeWidget> EnumerateNodes(Predicate<NodeWidget>? Filter = null)
        {
            if (Filter == null) {
                foreach (NodeWidget w in Nodes)
                    yield return w;
            } else {
                foreach (NodeWidget w in Nodes)
                    if (Filter(w)) yield return w;
            }
        }



        // INodeGraphLayoutProvider

        public virtual string GetLocationStringForNode(int NodeIdentifier)
        {
            NodeWidget? Found = FindNode(NodeIdentifier);
            if ( Found != null )
                return String.Format("{0} {1}", Found.Position.x, Found.Position.y);
            return "";
        }
        public virtual bool SetNodeLocationFromString(int NodeIdentifier, string locationString)
        {
            NodeWidget? Found = FindNode(NodeIdentifier);
            if (Found == null) return false;

            string[] tokens = locationString.Split(' ');
            if ( tokens.Length == 2 )
            {
                if (float.TryParse(tokens[0], out float x)  && float.TryParse(tokens[1], out float y))
                {
                    Found.Position = new Vector2f(x, y);
                    return true;
                }
            }
            return false;
        }


        public virtual void ExecuteGraphEdit(Action<NodeGraphEditor> EditFunc)
        {
            if (ActiveEditManager.TryGetTarget(out var EditManager))
            {
                EditManager.ExecuteGraphEdit(EditFunc);
            }
            else
                throw new Exception("NodeGraphView.ExecuteGraphEdit: no EditManager available");
        }


    }



    public class NodeLayoutCache : INodeGraphLayoutProvider
    {
        public struct LayoutInfo
        {
            public string locationString;
        }
        Dictionary<int, LayoutInfo> StoredLocationInfo = new Dictionary<int, LayoutInfo>();

        public NodeLayoutCache() { }

        public virtual string GetLocationStringForNode(int NodeIdentifier)
        {
            if (StoredLocationInfo.TryGetValue(NodeIdentifier, out LayoutInfo info))
                return info.locationString;
            return "";
        }

        public virtual bool SetNodeLocationFromString(int NodeIdentifier, string locationString)
        {
            LayoutInfo info = new LayoutInfo();
            if ( StoredLocationInfo.ContainsKey(NodeIdentifier) )
                info = StoredLocationInfo[NodeIdentifier];

            info.locationString = locationString;
            StoredLocationInfo[NodeIdentifier] = info;
            return true;
        }

        public virtual void ApplyToGraphView(NodeGraphView graphView)
        {
            foreach (var pair in StoredLocationInfo)
            {
                graphView.SetNodeLocationFromString(pair.Key, pair.Value.locationString);
            }
        }
    }

}
