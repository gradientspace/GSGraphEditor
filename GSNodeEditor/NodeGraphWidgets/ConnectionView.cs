using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using g3;
using Gradientspace.NodeGraph;
using SkiaSharp;

namespace GSNodeEditor
{
	/**
     * ConnectionView is the visual/widget-level representation of a graph connection.
     * However note that it is *not* a Widget / WidgetView itself.
     * 
     * Currently the ConnectionViews are owned by the NodeGraphView...
     */
	public class ConnectionView
    {
        public IConnectionInfo ConnectionInfo { get; private set; }

		// IConnectionInfo does not have it's own ID like INodeInfo does. 
        // So we create our own, which is used for transient things like graph selection.
        // !! However note that this ID does not persist between graph save/load!!!
		public int ConnectionID { get; private set; }

        // renderer sets this draw order (kinda hacky)
        public int DrawOrderIndex { get; set; } = 0;

        // this state is updated by the NodeGraphView by querying the NodeGraph
        public EConnectionState ConnectionState { get; set; } = EConnectionState.OK;

        public NodeWidget? FromNode { get; set; } = null;
        public int FromPin { get; set; } = -2;
        public NodeWidget? ToNode { get; set; } = null;
        public int ToPin { get; set; } = -2;

        public bool IsValid {
            get { return FromNode != null && ToNode != null && FromPin >= -1 && ToPin >= -1; }
        }

        private static int ConnectionIDGenerator = 1;
        private const int SequenceIDOffset = 1000000;

        public bool InitializeFromConnection(IConnectionInfo connectionInfo, NodeGraphView Graph)
        {
            ConnectionID = Interlocked.Increment(ref ConnectionIDGenerator);
            if (connectionInfo.ConnectionType == EConnectionType.Sequence)
                ConnectionID += SequenceIDOffset;

			ConnectionInfo = connectionInfo;
            NodeWidget? FoundFrom = Graph.FindNode(connectionInfo.FromNodeIdentifier);
            NodeWidget? FoundTo = Graph.FindNode(connectionInfo.ToNodeIdentifier);
            if (FoundFrom != null && FoundTo != null)
            {
                if (ConnectionInfo.ConnectionType == EConnectionType.Data)
                {
                    FromPin = FoundFrom.FindOutputPinIndexByName(connectionInfo.FromNodeOutputName);
                    ToPin = FoundTo.FindInputPinIndexByName(connectionInfo.ToNodeInputName);
                    if (FromPin >= 0 && ToPin >= 0)
                    {
                        FromNode = FoundFrom; ToNode = FoundTo;
                        return true;
                    }
                    FromPin = -2; ToPin = -2;
                }
                else if (ConnectionInfo.ConnectionType == EConnectionType.Sequence)
                {
                    if (connectionInfo.FromNodeOutputName.Length > 0) {
                        FromPin = FoundFrom.FindOutputPinIndexByName(connectionInfo.FromNodeOutputName);
                        if (FromPin >= 0) {
                            ToPin = -1;
                            FromNode = FoundFrom; ToNode = FoundTo;
                            return true;
                        }
                    } else {
                        ToPin = FromPin = -1;
                        FromNode = FoundFrom; ToNode = FoundTo;
                        return true;
                    }
                }
                else
                    throw new NotImplementedException();
            }

            if (this.IsValid)
                ConnectionState = Graph.GetGraph().GetConnectionState(ConnectionInfo);

            return this.IsValid;
        }



        public Vector2f StartPoint { get; set; }
        public Vector2f EndPoint { get; set; }
        public Vector2f StartTangentPoint { get; set; }
        public Vector2f EndTangentPoint { get; set; }

        public void UpdateLayout( float TangentLen )
        {
            if (!IsValid) return;

            if (ConnectionInfo.ConnectionType == EConnectionType.Data)
            {
                StartPoint = FromNode!.GetOutputPinConnectionPoint(FromPin);
                EndPoint = ToNode!.GetInputPinConnectionPoint(ToPin);
            }
            else
            {
                StartPoint = (FromPin < 0) ?
                    FromNode!.GetOutputSequencePinConnectionPoint() : FromNode!.GetOutputPinConnectionPoint(FromPin);
                EndPoint = ToNode!.GetInputSequencePinConnectionPoint();
            }

            float UseTangentLen = TangentLen;
            float Distance = StartPoint.Distance(EndPoint);
            if ( Distance < 2 * TangentLen )
            {
                UseTangentLen = Distance / 2;
            }

            StartTangentPoint = StartPoint + UseTangentLen * Vector2f.AxisX;
            EndTangentPoint = EndPoint - UseTangentLen * Vector2f.AxisX;
        }


        public static EConnectionType GetConnectionTypeFromID(int ConnectionID)
        {
            return (ConnectionID > SequenceIDOffset) ? EConnectionType.Sequence : EConnectionType.Data;
        }
    }
}
