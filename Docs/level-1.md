# Level 1 Progress

Last updated: 2026-10-07.

Scene: [Level01.unity](../Assets/Scenes/Level01.unity). Level baseline merged into `main` through PR #2, merge commit `d8a0c70`. Core prefab follow-up: `core-form-prefabs`. Design reference: Level 1 Revision 08.

**Status: the graybox and implemented non-ball interactions have passed developer-reported segment tests. Bouncing-ball integration and complete-route validation remain outstanding.** This is a working handoff baseline, not a finished level.

For controls and tuning, see [GameplaySpec.md](GameplaySpec.md). For script responsibilities, see [ScriptGuide.md](ScriptGuide.md).

## 1. Current route and progress

![Level 1 map — Revision 08](Level1_Map.png)

*Design reference; implementation status is listed below.*

| Area | Intended play sequence | Current state |
| --- | --- | --- |
| Start and first pool | Possess A, shrink through the low entrance, sink, then resize to reach U1 | Geometry and existing star/box/bubble behavior tested |
| Underwater traversal | Swim around the central block and purple bubble-pop hazards; recover using spare U1b if needed | Water, obstacles, pop-only hazards and spare bubble placed and tested |
| First pool exit | Choose the upper star route or land beside B and shrink through the lower passage toward R1 | Both route geometries placed; B's route does not require a jump |
| Deep well and receiver | R1 bounces through the well, reaches the receiver beneath its low roof, then transfers to C | Well, receiver and C placed; R1 remains a marker, so this transition is not validated |
| Second pool | Bring C underwater, deliberately leave it on P, and transfer to U2 | Held plate and possession tested; P is offset from U2 |
| Right ascent | U2 swims/rises through G while C keeps P pressed | G is positioned across the local ascent exit and tested |
| Upper box puzzle | Possess D and grow upward to touch H, opening G2 near the goal | Height switch, solid support and latched gate tested |
| Upper return | Transfer to R2 and steer its vertical bouncing through staggered floor/ceiling hazards toward U3 | R2 remains a marker; continuous floor is a temporary layout and the ball challenge is not built |
| Finish | Use U3, pass G2, pop back to the star and reach the flag | Goal detection, completion pause, visible flag and raised completion text tested |

The receiver roof is intended to limit R1's bounce height while the player steers toward C. Ordinary terrain contact should not deactivate the ball; transferring possession to C should stop the abandoned ball. The exact behavior awaits the ball implementation.

## 2. Scene wiring to preserve

| Scene object | Component / reference | Assigned target |
| --- | --- | --- |
| `PlayerControl` | `PlayerPossessionController.initialBody` | `StarPlayer` |
| `PressurePlate_P` | `HeldPressurePlate.targetBox` / `door` | `Box_C` / `Gate_G` |
| `Switch_H` | `BoxHeightSwitch.targetBox` / `door` | `Box_D` / `Gate_G2` |
| `WaterPopHazard01`, `WaterPopHazard02` | `BubblePopHazard.playerController` | `PlayerControl` |
| `LevelGoal` | Player controller / required gate / status text | `PlayerControl` / `Gate_G2` / `LevelStatusText` |

- **P → G is continuous:** only the assigned grounded C can hold it open. Control can transfer away from C while it remains on P. Moving C away closes G.
- **H → G2 is latched:** assigned grounded D touches the overhead trigger once; G2 then remains open until restart. H has no explicit size threshold; its height creates the growth requirement.
- **Purple hazards only pop bubbles:** they release the star and consume the controlled bubble. They do not implement the lethal spikes planned for the ball sections.
- **The goal requires the star:** it checks that the player is alive, the entering star is controlled, and G2 is inactive. It does not independently verify every earlier puzzle.
- The water zones are triggers. Bodies use `BodyWaterPhysics` and the common possession controller for their water behavior.

## 3. Saved layout and tuning reference

These are current prototype coordinates, not final balance requirements. Positions refer to scene roots in world space.

| Object / setting | Saved value | Purpose |
| --- | --- | --- |
| `PressurePlate_P` position | `(42, -5.95)` | Offset from U2 so leaving C correctly requires deliberate positioning |
| `Bubble_U2` position | `(40, -3.7)` | Second-pool escape body |
| `Gate_G` position / scale | `(45, 8.75)` / `(2.1, 0.5)` | Horizontal gate across the right ascent exit |
| `Switch_H` position / scale | `(41, 11)` / `(1, 0.4)` | Growth target above D |
| H support position / scale | `(41, 12.1)` / `(1, 1.8)` | Ordinary solid support connecting the switch to the ceiling |
| `Gate_G2` position / scale | `(-4, 11)` / `(0.3, 4)` | Final gate near U3 and the goal |
| `R1_SpawnMarker` / `R2_SpawnMarker` positions | `(21, 0.5)` / `(33, 9.5)` | Reserved ball integration locations |
| Well floor / receiver-bank surface | `Y = -8` / `Y = 3` | Approximately 11 units of rise; validate against R1's actual bounce capability |
| Upper floor / ceiling underside | `Y = 9` / `Y = 13` | Current space for the future R2 challenge |
| `LevelGoal` position | `(-10.5, 9)` | Final star-only goal |
| Vertical death limits / death delay | `-12` and `16` / `2 seconds` | Level01 controller settings |
| Water speed multipliers | Star `0.3`, Box `0.7`, Bubble `1.3` | Current shared water tuning |
| Star water jump multiplier | `0.75` | Initial jump velocity multiplier |

Keep boxes upright and unparented: the current resizing implementation rejects parented boxes. Do not group them under a level container without changing and testing that constraint.

## 4. Verification completed and its limits

The developer reported successful manual tests of:

- Star, box and bubble behavior in the level's water regions, including possession and bubble popping.
- First-pool geometry, alternate exit routes and box resizing passages.
- Bubble-only hazards and the nearby spare bubble.
- C holding P, G opening while held, and G closing when the condition is no longer met.
- D growing into H and G2 remaining open after activation.
- Goal activation as the star, completion feedback, flag visibility and text placement.
- New StarPlayer, BoxBody and BubbleBody prefab instances in WaterPlayground: movement, possession, box resizing/sinking, bubble swimming and popping, with no reported new Console errors.

Later segments were tested independently while the ball connection was unavailable. These results do not establish that the full intended route is traversable or that puzzles cannot be skipped. This documentation update checked source and saved scene configuration; it did not run a new Unity playtest.

For temporary segment testing, move the **`StarPlayer` root** in Play Mode. Moving its `Visual` child only moves the sprite, leaving physics and possession at the root. Keep the saved `Visual` local position at `(0, 0, 0)` and restore the intended starting setup before committing the scene.

## 5. Remaining work and handoff

1. **Integrate R1 and R2 with the ball owner.** Replace markers with the actual bodies, connect possession/input, and verify bounce activation, steering, ceiling collisions and stopping after transfer. Confirm R1 can reach the receiver and transfer to C without requiring C to jump.
2. **Build and tune the ball hazards.** Add the well's lethal spike arrangement and the upper corridor's staggered floor/ceiling hazards or openings. Preserve safe activation and transfer areas. The current continuous upper floor allows movement that would bypass the intended R2 challenge.
3. **Run the complete route.** Check puzzle order, alternate paths, accidental shortcuts, C staying on P after transfer, recoverability after losing a bubble, and restart after failure. Revise geometry against the actual ball physics.
4. **Finish presentation and integration.** Revisit the overview camera for readable gameplay, provide Level01 death feedback if needed, connect Tutorial → Level01 → Level02, and validate the intended build target.

The Level 1 baseline has merged into `main` through PR #2. The follow-up branch `core-form-prefabs` now contains reusable StarPlayer, BoxBody and BubbleBody assets. Level01 uses these for StarPlayer, Box_A and Bubble_U1; the other boxes and bubbles retain their existing scene setup. WaterPlayground also has saved instances of all three prefabs and an updated initial-body reference.

Prefab extraction and developer-reported cross-scene testing are complete. See [ScriptGuide.md](ScriptGuide.md) for setup requirements. Review this follow-up through its own PR; it does not complete the remaining bouncing-ball work.
