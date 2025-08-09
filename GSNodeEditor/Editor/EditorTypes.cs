// Copyright Gradientspace Corp. All Rights Reserved.
using Gradientspace.NodeGraph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GSNodeEditor
{
	/// <summary>
	/// NodeWidget / PinWidget pair, with additional info
	/// </summary>
	public class NodeAndPin
	{
		public NodeWidget Node;
		public NodePinWidget Pin;
		public int PinIndex;                // could possibly replace w/ a search function in NodeWidget?
		public bool bIsSequencePin = false; // could determine from Pin type?

		public NodeAndPin(NodeWidget node, NodePinWidget pin, int pinIndex, bool isInput) 
		{ 
			Node = node; 
			Pin = pin; 
			PinIndex = pinIndex; 
		}
		
		public GraphDataType DataType { get { return Pin.DataType; } }
	}
}
