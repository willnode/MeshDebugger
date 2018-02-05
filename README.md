# MeshDebugger v0.5

![Screenshot](Screenshots/Demo.png)

This is an editor tool to visually inspect a mesh. Very helpful if you want to debug your procedural mesh.

## Download

Download the plugin via [releases](releases)

## Features

+ Super simple (Just open the window and select a GameObject)
+ Dynamic update everytime scene repaint (can be turned off for speed too)
+ Depth Culling (Reduces visual complexity)
+ Inspect static mesh with over 65K vertices without lag (and yes, it don't have to be Unity 2017.3 to use it)
+ Many visual choices: Rays (eg. normal/tangent vertices), Heatmap (eg. triangle density) or Numbered GUI (eg. triangle/vert index)
+ Inspect uGUI (Unity UI) normally.
+ Also includes [shaders for visual debugging](Assets/Plugins/MeshDebugger/Shaders)

## Technology

MeshDebugger does this in simple order:

1. Get complete mesh analytics info
2. Draw debug info to a mesh (can be splitted to several mesh if over 65K vert get added)
3. Render it all-at-once via `Graphic.DrawMeshNow()`.

## Runtime Inspect

Because MeshDebugger don't use `Gizmos` or `Handles`, it's possible to bring inspection into runtime build, although it still need several modification because this is editor-oriented tool.

If enough people interested I can make separate repo for `IMGizmos` which makes this Immediate Drawing wonderfully simpler and fast.

## License

MIT