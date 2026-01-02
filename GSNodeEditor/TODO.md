==================
WIP
==================

- add simple error/warning dialog. Use to show file-load errors.

- refreshing node library marks it dirty...

- dynamic switch and static switch  (is static necessary?)
- select node
- some way to have multiple wires into a splitter-like thing where only the one on the active scope is used (related to switch/select)

- comment node (should be easy now that we have multiline entry)

- passthrough toggle on nodes w/ a ref parameter
   - NodeFunction option or a new attribute enables it
   - LibraryFunctionNode provides support
   - visual toggle on nodes w/ this capability

- make struct

- add special type of node for constant + alias, call it 'GraphParameter' - then can expose in custom UI panel...  (categories would be nice)

==================
Versioning
==================

- implement for node types


=============
Python
=============

- function argument default values
- 



===================
Bugs
===================

- exception on delete connection

- possibly need to refresh code nodes after load, if they had an error, because required assemblies might not have been loaded
    - (there is some messy business here...)
- SelectionManager active selection is not updated when deleting selected node, leaving an invalid selection
- Type input on static function node does not result in a type picker
- why isn't int -> Vector3d a type-conversion warning?
- 'Missing' pins do not disappear when disconnected
- Missing pins not necessarily an error when evaluating graph...
-crash if remove a function input that has values wired to it in call nodes
-call and return constants get wiped out if funcdef node is modified
-text entry field hotkeys don't always work...
-ClutchKeyInputBehavior is broken by selection support. Need to add handling for InputBehavior Priority.
-CustomDataItem API should be moved from NodeBase to INode
-undo back to last load/save state does not clear dirty flag

===================
Graph Features
===================

- generic Make Array node that determines type from first pin connection
- be able to drag off an enumerable and make array or list of that type

- make it possible to wire single element into an IEnumerable
- would be nice if DataConversionLibrary could support enumerating through 'to' options for a 'from'...then browser could show explicit conversion options. Would simplify some messy stuff.
- add some heuristics for handling name changes in inputs and outputs, when missing on load
- add pin-name-redirector attributes
- docking branch node  (to simplify common bool/branch pattern)
- nodeconstant default values  (copy from archetype?)
- support for serializing NodeFlags? required if they are used to store edit states...could compare w/ archetype? (but then delta-serialize issues...)
- support for toggling the EnableSequencePins flag on nodes (has to remove sequence connections, and so go through Editor...)
    - does that make sense? another way would be to have some kind of mini-pin that toggles on if it is wired...
- allow Pure nodes to not be re-evaluated, if entire pure-scope is clean...
    - the hard part is handling /inputs/ to constexpr nodes from /outside/ a pure-scope?
- code-functions defined like graph functions (ie separate thing)
- one-liner code expressions?
- Local Variables
- improve Functions
    - visibility toggle for function signature edit panel
    - add Scope tracking during evaluation, fix up return hack so that functions can be nested
    - verify function setups in graph validation
    - prevent wiring between main graph and function graphs
- Scope static analysis (eg to make sure inside of a For loop doesn't connect to outside, prevent wiring errors, etc)
- Explicit Cast node  (do we have this yet?)
- Asset Objects  (eg a DMesh3 that can be loaded in a little mini-graph, on-demand)
- Parallel Sequence, Parallel For, etc
- Graph Async/Await/Locks/etc   (await multiple sequences wires?)
- ThreadSafe/NotThreadSafe tags on nodes?
- Infinite Loop check/detection
- Color input pins by type (by-ref, by-value, python, etcetc)
- Color nodes by type
- overrideable node color?
- show by-ref vs by-value in pin tooltip
- pin contextmenu option to replace all output wires w/ an Alias...
 
 - a way to 'save' state/checkpoints in graph so user can do partial evaluation (eg similar to python notebook)

====================
Library Handling
====================
- support for calling .Initialize() functions of libraries to register types/etc
    - currently hardcoded in NodeGraphViewport.Initialize() and DerivativeCL.Initialize()
- support for enabling/disabling discovered libraries (currently always loads all)



===================
CodeGen
===================
- handle graph functions and function calls
- handle other controlflows
- handle ref and out args in librarynode
- some way to handle multiple sequence output wire going into a single sequence input
   - also manually-created loops...this is effectively a 'goto'...
- some way to allow NodeFuncs to customize code...maybe do it at the Library level? or a side-by-side `_CodeGen` func?


===================
Core Editor
==================

- Undo/Redo
    - need to expire low-level textedit changes after it goes out of focus
    - code nodes text
- Mac support (seems to be working...)
- Linux support

====================
Node Management
====================

- comment/description attributes on nodes? or parse code-file comments?
- collapse set of nodes to cluster
- node commenting
- area commenting


====================
Wiring Improvements
====================

- Linear wires in addition to curves (and maybe others?)
- Interactive curve Tension
- automatic Multi-wiring hints (eg match name/type)
- ctrl+click to delete clicked wire
- wire pin-points
- maybe a marking-menu type UI for these kind of wire edits? on right-click?
- try to show overlapping wires w/ different colors?
- optimize wire hit-testing  (doesn't even do bounding-box check currently...)
- wire marquee select


======================
Code Node Improvements
=======================

- Code Node from File.cs, .py, etc
- in-graph Code Node "objects" that can be referenced by graph nodes (eg to re-use)  ((maybe base on Asset objects?))
- Typescript support


=======================
Debugging Improvements
=======================

- inspecting values on pins during single-step
- Breakpoints that pause graph execution
- Watches on graph wires or input/output pins. Watches would be shown in RHS panel in editor view


=================
Utility
=================

- NodeBuilder functions that can emit a set of dynamically-generated node definitions for existing code