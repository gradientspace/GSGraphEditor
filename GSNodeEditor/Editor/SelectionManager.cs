// Copyright Gradientspace Corp. All Rights Reserved.
using g3;
using Gradientspace.UI;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GSNodeEditor
{
    public class SelectionManager : IHotkeyTarget, IDisposable
    {
        public NodeGraphViewport GraphViewport { get; init; }
        public NodeGraphView GraphView { get { return GraphViewport.CurrentGraphView; } }

        // todo these should probably be HashSet, no?
        protected List<int> SelectedNodes = new List<int>();
        protected List<int> SelectedConnections = new List<int>();

        public SelectionManager(NodeGraphViewport viewport)
        {
            GraphViewport = viewport;

            SystemKeyboardRouter.Instance.PushHotkeyTarget(this);
        }

        public void Dispose()
        {
            SystemKeyboardRouter.Instance.PopHotkeyTarget(this);
        }


        public void BeginTrackedSelectionChanges(string changeName = "Edit Selection")
        {
			GraphViewport.History.BeginChanges(changeName);
			BeginChange();
		}
		public void EndTrackedSelectionChanges()
		{
            EndChange();
            GraphViewport.History.EndChanges();
		}

		public void SelectNode(int NodeID, bool bReplace)
        {
            if (bReplace) 
            {
                SelectedNodes.Clear();
				SelectedConnections.Clear();
				SelectedNodes.Add(NodeID);
            }
            else
            {
                if (SelectedNodes.Contains(NodeID) == false)
                    SelectedNodes.Add(NodeID);
            }
        }

        public void DeselectNode(int NodeID)
        {
            if (SelectedNodes.Contains(NodeID))
                SelectedNodes.Remove(NodeID);
        }


		public void SelectConnection(int ConnectionID, bool bReplace)
		{
			if (bReplace)
			{
				SelectedConnections.Clear();
                SelectedNodes.Clear();
				SelectedConnections.Add(ConnectionID);
			} 
            else
			{
				if (SelectedConnections.Contains(ConnectionID) == false)
					SelectedConnections.Add(ConnectionID);
			}
		}

		public void DeselectConnection(int ConnectionID)
		{
			if (SelectedConnections.Contains(ConnectionID))
				SelectedConnections.Remove(ConnectionID);
		}


		public void ClearSelection()
        {
            SelectedNodes.Clear();
            SelectedConnections.Clear();
        }

		public bool HasNodeSelection { get { return SelectedNodes.Count > 0; } }
		public bool HasConnectionSelection { get { return SelectedConnections.Count > 0; } }
		public bool HasSelection { get { return SelectedNodes.Count > 0 || SelectedConnections.Count > 0; } }

        public bool IsSelectedNode(int NodeID) {  return SelectedNodes.Contains(NodeID); }
		public bool IsSelectedConnection(int ConnectionID) { return SelectedConnections.Contains(ConnectionID); }

		public IEnumerable<int> CurrentNodeSelection { get { return SelectedNodes; } }
		public IEnumerable<int> CurrentConnectionSelection { get { return SelectedConnections; } }


		public List<NodeWidget> FindSelectedNodeWidgets()
        {
            List<NodeWidget> result = new List<NodeWidget>();
            for (int i = 0; i < SelectedNodes.Count; ++i)
            {
                NodeWidget? found = GraphView.FindNode(SelectedNodes[i]);
                if (found != null) result.Add(found);
            }
            return result;
        }


        enum ELassoSelectionModes
        {
            None,
            Rectangle,
            Freeform
        }
        ELassoSelectionModes ActiveLassoMode = ELassoSelectionModes.None;
        // these points will be in viewport space
        List<Vector2f> LassoPoints = new List<Vector2f>();
        int LassoModifyMode = 0;  // 0 = replace, 1 = add, 2 = subtract

        public void BeginMarqueeSelection(in InputDeviceState deviceState)
        {
            GraphViewport.History.BeginChanges("Marquee Select");
            BeginChange();

            ActiveLassoMode = ELassoSelectionModes.Rectangle;
            LassoPoints.Add(deviceState.CurrentPosition);
            LassoPoints.Add(deviceState.CurrentPosition);
            LassoModifyMode = 0;
        }
        public void UpdateMarqueeSelection(in InputDeviceState deviceState)
        {
            LassoPoints[1] = deviceState.CurrentPosition;

            if (deviceState.ShiftButton.bDown)      LassoModifyMode = 1;
            else if (deviceState.CtrlButton.bDown)  LassoModifyMode = 2;
            else                                    LassoModifyMode = 0;

            Debug.Assert(ActiveLassoMode != ELassoSelectionModes.None);
        }
        public void CompleteMarqueeSelection()
        {
            Debug.Assert(ActiveLassoMode != ELassoSelectionModes.None);

            AxisAlignedBox2f LassoBox = new AxisAlignedBox2f(LassoPoints[0]);
            LassoBox.Contain(LassoPoints[1]);

            if (LassoModifyMode == 0)
                SelectedNodes.Clear();

            foreach (NodeWidget widget in GraphView.NodeWidgets)
            {
                AxisAlignedBox2f NodeBounds = widget.GetActiveView()?.BoundsQuery(widget.GetAnchor()) ?? AxisAlignedBox2f.Empty;
                if ( LassoBox.Intersects(NodeBounds) )
                {
                    if (LassoModifyMode == 2) {
                        SelectedNodes.Remove(widget.GraphNodeIdentifier);
                    } else {
                        if (SelectedNodes.Contains(widget.GraphNodeIdentifier) == false) SelectedNodes.Add(widget.GraphNodeIdentifier);
                    }
                }
            }

            ActiveLassoMode = ELassoSelectionModes.None;
            LassoPoints.Clear();

			EndChange();
			GraphViewport.History.EndChanges();
		}




        public void DrawViewport(SKCanvas Canvas)
        {
            SKPaint LinePaint = new SKPaint { Color = SKColors.Goldenrod, StrokeWidth = 3, IsStroke = true };

            foreach (int nodeID in SelectedNodes)
            {
                NodeWidget? Widget = GraphView.FindNode(nodeID);
                AxisAlignedBox2f NodeBounds = Widget?.GetActiveView()?.BoundsQuery(Widget.GetAnchor()) ?? AxisAlignedBox2f.Empty;
                if ( NodeBounds.Area > 0 ) 
                {
                    Canvas.DrawRect(Conversion.ToSkia(NodeBounds), LinePaint);
                }
            }

            LinePaint.StrokeWidth = 8;
			foreach (int connectionID in SelectedConnections)
            {
                ConnectionView? Connection = GraphView.FindConnectionByID(connectionID);
                if (Connection == null) 
                    continue;
				SKPath Curve = new SKPath();
				Curve.MoveTo(Conversion.ToSkia(Connection.StartPoint));
				Curve.CubicTo(Conversion.ToSkia(Connection.StartTangentPoint), Conversion.ToSkia(Connection.EndTangentPoint), Conversion.ToSkia(Connection.EndPoint));
				Canvas.DrawPath(Curve, LinePaint);
			}
        }


        public void DrawUI(SKCanvas Canvas)
        {
            if (ActiveLassoMode == ELassoSelectionModes.Rectangle)
            {
                SKColor LassoColor = SKColors.White;
                SKPaint LassoLinePaint = new SKPaint { Color = LassoColor, StrokeWidth = 1, IsStroke = true };

                AxisAlignedBox2f LassoRect = new AxisAlignedBox2f(GraphViewport.TransformViewportToUI(LassoPoints[0]));
                LassoRect.Contain(GraphViewport.TransformViewportToUI(LassoPoints[1]));
                Canvas.DrawRect(Conversion.ToSkia(LassoRect), LassoLinePaint);
            }
        }



        public bool OnKeyChordUpdated(in KeyChord ActiveChord)
        {
            if (ActiveChord.IsSingleSpecialKey(KeyNames.Delete))
            {
                if (HasNodeSelection) {
                    List<NodeWidget> widgets = FindSelectedNodeWidgets();
                    GraphViewport.ExecuteGraphEdit((NodeGraphEditor Editor) => {
                        foreach (NodeWidget widget in widgets)
                            Editor.RemoveNode(widget);
                    });
                    return true;
                } 
                else if (HasConnectionSelection)
                {
                    foreach (int ConnectionID in SelectedConnections) {
						ConnectionView? Connection = GraphView.FindConnectionByID(ConnectionID);
                        if (Connection != null) {
                            GraphViewport.ExecuteGraphEdit((NodeGraphEditor Editor) => {
                                Editor.RemoveConnection(Connection.ConnectionInfo);
                            });
						}
					}
                }
            }

            return false;
        }



        internal void UpdateSelectionOnUndoRedo(List<int>? NewNodes, List<int>? NewConnections)
        {
            SelectedNodes.Clear();
            if (NewNodes != null)
                SelectedNodes.AddRange(NewNodes);
            SelectedConnections.Clear();
            if (NewConnections != null)
                SelectedConnections.AddRange(NewConnections);
		}

        protected SelectionManagerSelectionChange? ActiveChange = null;
        protected virtual void BeginChange()
        {
			Debug.Assert(ActiveChange == null);
			ActiveChange = new SelectionManagerSelectionChange();
            ActiveChange.Init(this, SelectedNodes, SelectedConnections);
        }
        protected virtual void EndChange()
        {
            Debug.Assert(ActiveChange != null);
            ActiveChange.Complete(SelectedNodes, SelectedConnections);
            if (ActiveChange.IsNoOp() == false)
                GraphViewport.History.AppendChange(ActiveChange);
            ActiveChange = null;
		}
	}


    public class SelectionManagerSelectionChange : BaseHistoryChange
    {
        // todo optimize storage for small # of items (most frequent case)   (and use arrays?)
        public List<int>? PrevNodes = null, NewNodes = null;
        public List<int>? PrevConnections = null, NewConnections = null;

        public SelectionManager? SelectionManager = null;

        public void Init(SelectionManager Manager, List<int> InitialNodes, List<int> InitialConnections)
        {
            SelectionManager = Manager;
			PrevNodes = (InitialNodes.Count > 0) ? new List<int>(InitialNodes) : null;
			PrevConnections = (InitialConnections.Count > 0) ? new List<int>(InitialConnections) : null;
		}
        public void Complete(List<int> FinalNodes, List<int> FinalConnections)
        {
            NewNodes = (FinalNodes.Count > 0) ? new List<int>(FinalNodes) : null;
            NewConnections = (FinalConnections.Count > 0) ? new List<int>(FinalConnections) : null;
        }

        // todo this should move to some utility thing somewhere...
        private static bool is_same_list(List<int>? List1, List<int>? List2)
        {
            bool null1 = (List1 == null), null2 = (List2 == null);
            if (null1 && null2)
                return true;
            if (null1 ^ null2)
                return false;
            // does this detect different sorts??
            foreach ( int diff in List1!.Except<int>(List2!) )
                return false;
			foreach (int diff in List2!.Except<int>(List1!))
				return false;
            return true;
		}

        //! return true if there is no selection change
		public bool IsNoOp()
        {
            return is_same_list(PrevNodes, NewNodes) && is_same_list(PrevConnections, NewConnections);
        }

		public override void Apply()
		{
            Debug.Assert(SelectionManager != null);
            SelectionManager?.UpdateSelectionOnUndoRedo(NewNodes, NewConnections);
		}
		public override void Revert()
		{
			Debug.Assert(SelectionManager != null);
            SelectionManager?.UpdateSelectionOnUndoRedo(PrevNodes, PrevConnections);
		}
	}

}
