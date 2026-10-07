# StarFormer Rolling Ball Prototype

Standalone Unity 2D prototype for Jingyi's rolling/bouncing body.

## Open

- Unity version: `6000.3.22f1`
- Scene: `Assets/Scenes/RollingBallPrototype.unity`

## Controls

- Move: `A/D` or Left/Right Arrow
- Star jump: `Space`
- Restart: `R`

The ball rolls autonomously while it is not controlled, so placing the prefab
on a slope makes it travel downhill on its own. Touching it transfers control
without launching it upward. The first steering input begins its higher
automatic-bounce cycle. Clear the wall, steer through the safe spike gaps, and
reach the green goal flag.

## Visual direction

- Sky: light blue `#A3D6F0`
- Terrain: warm taupe brown `#A88C70`
- Goal flag: soft green `#59AB5E`
- Rolling ball: soft purple `#9E80DB`
- Hazards: the same warm taupe brown as the terrain

The scene uses simple flat shapes and a small green goal flag so future levels
can keep the same visual language without needing bespoke artwork.

## Team-scene conventions

The prototype follows the same structural ideas used by the team's `Level01`
and `Playground` scenes: a single `PlayerControl` object owns input and body
transfer, each controllable body stores its own movement settings, `StarMark`
is a separate child visual, and scene objects use functional hierarchy names
such as `StartGround`, `LowerFloor`, `StarPlayer`, and `RollingBallBody`.

## Reusable asset

The portable character prefab is:

`Assets/Prefabs/Characters/RollingBall.prefab`

It owns its physics, collider, visuals, tuning values, controlled-star marker,
autonomous rolling, and controlled bouncing behavior. It does not reference
level geometry.
