
===================
Bugs
===================

-crash if remove a function input that has values wired to it in call nodes
-call and return constants get wiped out if funcdef node is modified
-ask to save on close if graph is modified
-text entry field hotkeys don't always work...
-ClutchKeyInputBehavior is broken by selection support. Need to add handling for InputBehavior Priority.
-CustomDataItem API should be moved from NodeBase to INode


===================
Graph Features
===================
- Local Variables
- improve Functions
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
- by-ref in-to-out dashed lines

===================
Core Editor
==================

- Undo/Redo
    - need to expire low-level textedit changes after it goes out of focus
    - code nodes text
    - does it work for functions?
- Copy/Paste   (do it via temp serialize to json??)
- Mac support (seems to be working
- Linux support
- open graph on drag/drop

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

======================
Error Handling Improvements
======================

**Missing Pins**

Ability to add pins to any node for 'missing' input/outputs, eg if 
a graph is loaded that refers to pins that no longer exist, if a function
changes, etc. Missing Pins are created on Node instances, and then connection
can be made in graph but not executed. Can be discovered in validation pass, never 
need to be explicitly stored (on nodes).

**Missing Nodes**

Extension of Missing Pins, if a graph is loaded that refers to nodes that do not exist,
special MissingNode is created. MissingPins functionality can be used to create connection pins?



=======================
Debugging Improvements
=======================

- graph single-stepping evaluation
- inspecting values on pins during single-step
- Breakpoints that pause graph execution
- Watches on graph wires or input/output pins. Watches would be shown in RHS panel in editor view.


=================
Utility
=================

- Command-Line graph executor   (need argc/argv nodes, maybe a library for them?)
- NodeBuilder functions that can emit a set of dynamically-generated node definitions for existing code