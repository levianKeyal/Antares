# Changelog

## Unreleased

### Added

- New `Ground` workflow for `Rectangle`, `Circle`, and `Freehand`.
- Tileable and non-tileable material support for ground meshes.
- `Tile Size` control for ground materials.
- Automatic merging for touching ground meshes that share the same material.
- `Tiled` placement improvements for floor-like layouts.
- Expanded previews for area tools, brush, line, and ground.
- Scene view toolbar styling and mode buttons for faster tool switching.

### Changed

- `Circle` now draws from one side of the diameter to the other instead of using the click point as the center.
- `Brush` now uses a dynamic brush radius tied to `Tile Scale` when `Tiled` is enabled.
- `Freehand` and `Line` snapping behavior was refined to support continuous placement.
- Consecutive perimeter placement was adjusted to better follow existing edges.
- Ground previews and final meshes were aligned to use consistent UV mapping and material handling.

### Fixed

- Fixed several preview and instancing cases where tools could stop spawning after hover preview changes.
- Fixed circle and freehand ground meshes not applying the selected material correctly.
- Fixed mesh merge cases that could visually break tiled materials or remove the last drawn piece.
- Fixed several scope/name collision issues introduced during editor tool expansion.

## Baseline

The last stable version before these additions included the core editor spawner with:

- rectangle, circle, freehand, line, and brush drawing
- prefab lists
- random seed support
- perimeters and consecutive placement
- eraser support
- scene view previews
