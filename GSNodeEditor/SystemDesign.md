

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


**Dynamic Inputs and Outputs**

todo


# Input Pin Constant/Default Values and Inline Widgets

An unwired input pin throws a graph error during evaluation unless it has a default or constant value.
Generally in the code this is referred to as a Constant, not a default. 
Constants generally only work for value/struct types, not for classes.

(note there are probably some bugs around using an object as a Constant,
 ie due to assumption that assignment-is-copy)

**Interface level**

An **INodeInput** implementation provides a 'constant' (ie not-from-the-wire) value via 
``INodeInput.GetConstantValue()``. Implementations can also implement ``.SetConstantValue()`` to
provide a generic setting. These operate on [object] references.

Note that **INodeGraph** provides ``.GetNodeConstantValue()`` and ``.SetNodeConstantValue()`` for convenience, 
which generally are calling the above functions w/o requiring you to find the specific input
(the work by node identifier and input pin name).

**BaseGraph level**

**StandardNodeInputBaseWithConstant** and **StandardNodeInputWithConstant\<T\>** are NodeInput baseclass
implementations that provide internal storage of a Constant (struct/value-type). 

**StandardStringNodeInput** can be used for strings (which are not structs).

**StandardNullableNodeInput** can/should be used for any nullable class-type input.
This means all nullable inputs in NodeLibrary static functions are optional.

**ClassTypeNodeInput** can be used for Type inputs (eg when selecting an array type, etc).

**Custom Type Constants via DefaultTypeInfoLibrary**

Clients can register a default value to use as the input pin Constant via
the ``DefaultTypeInfoLibrary.Instance`` global. The ``RegisterType()`` function is
used to set the default for a Type. This has to happen before graphs are loaded.
It can (for example) be done in the Initialize() function of a NodeLibrary, eg see
``NodeGraphGeometryLibrary.Initialize()`` for an example.

``DefaultTypeInfoLibrary.TypeSupportsInputConstant()`` and ``.GetDefaultConstantValueForType()``
are used by library code to find a suitable Constant for a Type, eg when building
the input pins for a FunctionLibrary node.

**Struct Constants**

Any struct types with parameterless default constructors (ie where = default would work)
have a default value that can be used as the NodeConstant when no other one is defined.
In NodeLibrary functions this will happen automatically.

**Serialization**

When the nodegraph is serialized, the constant values defined for INodeInputs will be
serialized to json. This means those types must be json-serializable! In the case where
an input pin with a Constant is desirable for avoiding wire-requirements, but is not
serializable (eg like for arbitrary structs), ``ENodeInputFlag.ConstantIsTransient`` can
be set on the Input's flags. The serializer will then skip this input (in ``SerializationUtil.TryGetInputConstant()``)

``SerializationUtil.RestoreInputConstants()`` restores the saved Constants. Note there
is some challenge here because sometimes setting a Constant will 

**NodeLibrary functions**

When automatically converting a NodeLibrary static function into a node, the method arguments
are inspected to determine if a Constant can be constructed automatically. For POD types
like int, double, bool, etc, it always happens. Similarly for string and enum, and optional/nullables.
Then DefaultTypeInfoLibrary is checked, and then finally struct types are checked
for a parameterless constructor. This is implemented in 
``FunctionNodeUtils.BuildInputNodeForMethodArgument()``

**Inline-Editable Constants**

A **NodeInputPinWidget** can create sub-widgets which allow certain types to be directly "inline"
edited on the node widget. Eg like a number or string field. EInlineWidgetType defines the
set of types for which this is possible. ``NodeWidget().UpdateAllInlineInfo()`` is the top-level
function that builds all of this, calling UpdateInlineInfo() on each input pin widget.
It's not yet possible to customize this, ie extend it externally - each Type for which
it's possible is hardcoded in **NodeInputPinWidget** (something to improve / todo)



## EnumOptionSet / EnumOptionSetNodeInput

## EnumerableNodeInput




# Connections

Connections are the 'wires' between node pins, along which data or control flows.

At the interface level, **IConnectionInfo** represents a Connection as a pair of (NodeIdentifier,PinName) tuples,
"From" one pin and "To" another. A ConnectionType is also stored, currently there are Data and Sequence types.

*[Note] IConnectionInfo is a struct, not an interface!  it should be renamed...*

There generally is no concept of a Connection as an independent object at the graph level. 
Connections are always stored as (node,pin)->(node,pin), rather than having their own ID/etc like Nodes do.
The **INodeGraph** interfaces and implementations like **BaseGraph** operate on **IConnectionInfo** objects.
This means (eg) to "find" a connection, a search has to be done to find matching nodes/pins.

**Connection State**

There is a concept of the 'State' of a connection, via **EConnectionState**, which can be used to
tag connections as having some kind of error/etc (eg a type mismatch). The **IConnectionInfo** doesn't
have this information, as it is allowed to be transient. It is tracked/stored by the graph implementation (eg BaseGraph)
and queried via ``INodeGraph.GetConnectionState()``

**UI Level / Connection View**

At the Editor/UI level, a **ConnectionView** is created for each **IConnectionInfo** and owned by the **NodeGraphView**.
A **ConnectionView** *does* have an integer ID, which (currently) is used by the Selection system.

A **ConnectionView** is *not* a Widget. All manipulation of the **ConnectionView** is done via **NodeGraphView**.
Similarly Connection rendering and hit-testing is done there (probably need to factor this out...).



# Data Conversions

Custom datatype conversions (ie that will be done automatically when connecting graph pins)
can be provided from within a ``[NodeFunctionLibrary]`` using static functions with the ``[GraphDataTypeConversion]``
attribute. The function must be of the form 
```
public static ToType ConvertFunc(FromType value) { ... }
```
**G3Vector3Functions** contains various scalar-to-vector conversions of this form

A second option is to provide a static function with the ```[GraphDataTypeRegisterFunction]``` attribute, of the form
```
public static void RegisterFunc(DataConversionLibrary library)
```
Inside this function, ``library.AddConversion()`` can be used to add conversions using (eg) lambdas/etc.
**GSPythonConversionsLibrary** is an example of this kind of usage

Both these types of functions are automatically discovered by ``DataConversionLibrary.Build()``. 
Currently a global singleton instance **GlobalDataConversionLibrary** is being used, with
the static ``GlobalDataConversionLibrary.Find()`` being the main way that code finds/applies these conversions.



# Viewport Selection


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




