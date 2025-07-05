

------------
Graph
------------




-----------
Nodes
-----------


**Placeholder Nodes**

eg ForEachPlaceholderNode / ForEachEnumerableNode



**Custom Data**

CollectCustomDataItems / RestoreCustomDataItems


**Dynamic Outputs**



-------------
Connections
-------------

Connections are the 'wires' between node pins, along which data or control flows.

At the interface level, IConnectionInfo represents a Connection as a pair of (NodeIdentifier,PinName) tuples,
"From" one pin and "To" another. A ConnectionType is also stored, currently there are Data and Sequence types.

[Note] IConnectionInfo is a struct, not an interface!  it should be renamed...

There generally is no concept of a Connection as an independent object at the graph level. 
Connections are always stored as (node,pin)->(node,pin), rather than having their own ID/etc like Nodes do.
The INodeGraph interfaces and implementations like BaseGraph operate on IConnectionInfo objects.
This means (eg) to "find" a connection, a search has to be done to find matching nodes/pins.

**Connection State**

There is a concept of the 'State' of a connection, via EConnectionState, which can be used to
tag connections as having some kind of error/etc (eg a type mismatch). The IConnectionInfo doesn't
have this information, as it is allowed to be transient. It is tracked/stored by the graph implementation (eg BaseGraph)
and queried via INodeGraph.GetConnectionState()

**UI Level / Connection View**

At the Editor/UI level, a ConnectionView is created for each IConnectionInfo and owned by the NodeGraphView.
A ConnectionView *does* have an integer ID, which (currently) is used by the Selection system.

A ConnectionView is *not* a Widget. All manipulation of the ConnectionView is done via NodeGraphView.
Similarly Connection rendering and hit-testing is done there (probably need to factor this out...).



--------------------
Viewport Selection
--------------------

SelectionManager tracks active selection sets for Nodes and Connections.
The Selection Sets and SelectionManager API is all done via Node/Connection IDs.

Nodes and Connections are selected via NodeSelectionInputBehavior.
This is where click and marquee-select mouse interaction is controlled.
This InputBehavior manipulates the SelectionManager.

Currently Marquee Select is only allowed for Nodes.
Note that the actual marquee-to-nodes implementation is in SelectionManager,
however for single-clicks, the hit-testing is done by the InputBehavior and
only integer IDs are forwarded to SelectionManager.
(this complicates any kind of filtering/etc and should be centralized in one or the other...)

"Hit Depth" in the InputBehavior is currently hacked, it's -50 for nodes, -75 for connections, and -100 for marquee rect.

SelectionManager implements IHotkeyTarget and the Delete key handling
is currently done here, to delete selected nodes/connections (weird place for it...)




