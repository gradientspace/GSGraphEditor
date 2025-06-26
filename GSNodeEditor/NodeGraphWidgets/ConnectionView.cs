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
    public class ConnectionView
    {
        public IConnectionInfo ConnectionInfo { get; private set; }

        // this state is updated by the NodeGraphView by querying the NodeGraph
        public EConnectionState ConnectionState { get; set; } = EConnectionState.OK;

        public NodeWidget? FromNode { get; set; } = null;
        public int FromPin { get; set; } = -2;
        public NodeWidget? ToNode { get; set; } = null;
        public int ToPin { get; set; } = -2;

        public bool IsValid {
            get { return FromNode != null && ToNode != null && FromPin >= -1 && ToPin >= -1; }
        }

        public bool InitializeFromConnection(IConnectionInfo connectionInfo, NodeGraphView Graph)
        {
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
    }
}
