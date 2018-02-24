# Manual (v0.6)

![The Window](Screenshots/Window.png)

After importing the plugin to your project, you can open Mesh Debugger window to start inspecting any selected object.

![Window Location](Screenshots/PopTheWindow.png)

## Selecting Object to Inspect

The first row shows which object and mesh that currently being inspected. It'll automatically change, depending on what object that currently being selected in the scene.

> Up to this version you can't lock the selected mesh and there's no plan for supporting multiple inspection in the same time.

## Configurations

+ `Static`: Turn this on if currently inspected mesh won't change.
+ `Depth Culling`: Reduce complexity by enable Z-Depth on visual cues.
+ `Equalize`: Keep visual cues scales in Screen Space (no matter the distance).
+ `Partial Debug`: Only inspect vertex/triangle at given fraction.

## Rays

![Ray](Screenshots/Ray.png)

For displaying vector values like normal direction:

+ `Ray Size`: Size of the ray.
+ `Vertex Rays`: Show Normal/Tangent/Bitangent of each vertex.
+ `Additional Rays`: Show Vertex to Triangle or Normal direction of each triange.

> Vertex to Triangle generates line between each vertex to triangle median. Not a finished feature.

## Heatmap

![Maps](Screenshots/Maps.png)

For displaying scalar values like vertex index:

+ `Use Heatmap`: Show display scalar as number (image on left) or color indicator (image on right)?
+ `Debug Vertices`: Shows:
  - `Index`: Index of each vertex
  - `Shared`: How many triangles reference that vertex (also useful for detecting orphaned vertices)
  - `Duplicated`: How many vertices have the same position
+ `Debug Triangles`: Shows:
  - `Index`: Index of each triangle
  - `Area`: Calculated area surface of each triangle
  - `Submesh`: Submesh index of each triangle

## Mesh Features Info

This tool also shows some statistics about the inspected mesh including vertices, indices, vertex channels, etc. We also open to discuss about what's info else should be included here.
