# StarFormer

StarFormer is a 2D puzzle-platformer built in Unity. Play as a star that can possess objects on contact and use their abilities to explore and solve puzzles. Grow or shrink as a box to fit through passages and activate switches, or become a bubble to rise through the air and swim underwater. Progress comes from switching between forms and combining their strengths with the environment.

![StarFormer tutorial gameplay](Docs/Game_screenshot.png)
# Spider Form and SpiderScene

This update adds a possessable Spider form with a hexagonal appearance and a dedicated test scene, `SpiderScene`.

## Features

- Attaches to surfaces and moves along slopes, walls, and ceilings.
- Turns around corners using the same movement keys.
- Cannot jump.
- Displays a rolling animation while moving.
- Settles with one edge against the surface when stopped.

## Controls

| Key | Action |
| --- | --- |
| A / Left Arrow | Move in one direction along the surface |
| D / Right Arrow | Move in the opposite direction along the surface |
| R | Restart the scene |

Movement follows the attached surface, so the same key can move the Spider horizontally, vertically, or around a corner.

## SpiderScene

`SpiderScene` provides a dedicated environment for testing the Spider form.

The scene includes angled platforms and tight corners to test surface attachment, rolling animation, and transitions between surfaces.

## Implementation

- Added `HexagonSurfaceMovement.cs` to handle surface detection, attachment, movement, and rolling animation.
- Updated `PossessableBody` and `PlayerPossessionController` to support the Spider form.
- Added a reusable Spider prefab.
- Added `SpiderScene` and its test obstacles.
- Extended forward detection and added an approach-direction check to improve corner transitions.

The Spider uses a `CircleCollider2D` for physical collisions. Its hexagonal appearance and rolling animation are handled by a separate visual object.

## How to Test

1. Open `SpiderScene` in Unity.
2. Enter Play mode.
3. If starting as the Star, touch the Spider to possess it.
4. Use A/D or the arrow keys to move along surfaces.
5. Test both directions through the corners.
6. Release the movement keys to check that the visual settles against the surface.
