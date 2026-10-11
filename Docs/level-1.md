# Level 1 Progress

Last updated: 2026-10-11.

Scene: [Level01.unity](../Assets/Scenes/Level01.unity). Level baseline merged into `main` through PR #2, merge commit `d8a0c70`. Core prefabs merged through PR #3. Hazard prefab follow-up: `core-form-prefabs`, implementation baseline `af9e0aa`. Design reference: Level 1 Revision 08.

**Status: the reusable bouncing-ball form, both Level01 ball instances, adaptive bounce behavior, water-surface behavior, the completed upper ball challenge and the three-region camera are integrated. Existing non-ball interactions and the newly added systems have passed developer-reported segment tests. Complete start-to-finish route validation remains outstanding.** This is a working handoff baseline, not a finished level.

For controls and tuning, see [GameplaySpec.md](GameplaySpec.md). For script responsibilities, see [ScriptGuide.md](ScriptGuide.md).

## 1. Current route and progress

![Level 1 map — Revision 08](Level1_Map.png)

*Design reference; implementation status is listed below.*

| Area | Intended play sequence | Current state |
| --- | --- | --- |
| Start and first pool | Possess A, shrink through the low entrance, sink, then resize to reach U1 | Geometry and existing star/box/bubble behavior tested |
| Underwater traversal | Swim around the central block and purple bubble-pop hazards; recover using spare U1b if needed | Water, obstacles, pop-only hazards and spare bubble placed and tested |
| First pool exit | Choose the upper star route or land beside B and shrink through the lower passage toward R1 | Both route geometries placed; B's route does not require a jump |
| Deep well and receiver | R1 bounces through the well, reaches the receiver beneath its low roof, then transfers to C | `BouncyBall_Lower` is placed near the original R1 marker. Possession, steering, adaptive low-roof bouncing and stopping after transfer work in segment tests; the complete approach to C still needs route validation |
| Second pool | Bring C underwater, deliberately leave it on P, and transfer to U2 | Held plate and possession tested; P is offset from U2 |
| Right ascent | U2 swims/rises through G while C keeps P pressed | G is positioned across the local ascent exit and tested |
| Upper box puzzle | Possess D and grow upward to touch H, opening G2 near the goal | Height switch, solid support and latched gate tested |
| Upper return | Transfer to R2 and steer its vertical bouncing through the upper floor/ceiling obstacle sequence toward U3 | `BouncyBall_Upper` is placed beside D at the right side of the upper floor. The return floor, canopy obstacles, lethal spikes, landing areas and route to U3 are implemented; full start-to-finish validation remains pending |
| Finish | Use U3, pass G2, pop back to the star and reach the flag | Goal detection, completion pause, visible flag and raised completion text tested |

The receiver roof limits R1's bounce height while the player steers toward C. A controlled ball normally targets a 20.25-unit bounce with 2.4 seconds to its apex. When a low solid ceiling compresses the available height, the ball strikes it and automatically uses a shorter, faster bounce rhythm. Ordinary terrain contact does not deactivate the ball. Transferring possession to another body freezes the abandoned ball in place until it is possessed again.

The ball floats at the surface of any registered water zone. While floating it cannot sink or bounce, but A/D still provides reduced horizontal control. Returning to land restores automatic bouncing.

## 2. Scene wiring to preserve

| Scene object | Component / reference | Assigned target |
| --- | --- | --- |
| `PlayerControl` | `PlayerPossessionController.initialBody` | `StarPlayer` |
| `PressurePlate_P` | `HeldPressurePlate.targetBox` / `door` | `Box_C` / `Gate_G` |
| `Switch_H` | `BoxHeightSwitch.targetBox` / `door` | `Box_D` / `Gate_G2` |
| `WaterPopHazard_01`, `WaterPopHazard_02`, `WaterPopHazard_03` | `BubblePopHazard.playerController` | `PlayerControl` |
| `LevelGoal` | Player controller / required gate / status text | `PlayerControl` / `Gate_G2` / `LevelStatusText` |

- **P → G is continuous:** only the assigned grounded C can hold it open. Control can transfer away from C while it remains on P. Moving C away closes G.
- **H → G2 is latched:** assigned grounded D touches the overhead trigger once; G2 then remains open until restart. H has no explicit size threshold; its height creates the growth requirement.
- **Purple hazards only pop bubbles:** they release the star and consume the controlled bubble. The saved scene has WaterPopHazard prefab instances `_01` and `_03`; `_02` retains its existing scene setup. All three have scene controller references. The upper ball route uses the reusable lethal `SpikeHazard` for its completed hazard layout.
- **The goal requires the star:** it checks that the player is alive, the entering star is controlled, and G2 is inactive. It does not independently verify every earlier puzzle.
- The water zones are triggers. Bodies use `BodyWaterPhysics` and the common possession controller for their water behavior.
- **Ball spikes remain lethal:** `SpikeHazard` kills the currently controlled star, box, bubble or bouncing ball and reloads the scene after the configured death delay.
- **The camera follows the controlled form:** possession changes do not break camera tracking. Level01 uses lower-left, lower-right and upper camera regions with bounded follow, smooth movement and smooth zoom transitions.

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
| `BouncyBall_Lower` / `BouncyBall_Upper` positions | `(20.75, 0.55)` / `(39.5, 9.55)` | Two scene instances of the same reusable ball prefab; the upper ball was deliberately moved beside D |
| Legacy `R1_SpawnMarker` / `R2_SpawnMarker` positions | `(21, 0.5)` / `(33, 9.5)` | Empty reference transforms retained temporarily; they no longer drive gameplay |
| Well floor / receiver-bank surface | `Y = -8` / `Y = 3` | Approximately 11 units of rise; validate against R1's actual bounce capability |
| Upper floor / ceiling underside | `Y = 9` / `Y = 13` | Vertical bounds used by the implemented upper R2 challenge |
| `BubbleRouteBlocker` position / scale | `(19, 7.125)` / `(0.6, 2.75)` | Ordinary solid wall hanging from the upper platform to block the bubble route without extending to the lower floor |
| `LevelGoal` position | `(-10.5, 9)` | Final star-only goal |
| General vertical death limits / death delay | `-12` and `16` / `2 seconds` | Level01 settings for the star, box and bubble |
| Bouncing-ball upper death limit | `40` | Ball-specific exception required by its 20.25-unit open-air bounce; this changes survival bounds, not bounce height |
| Ball default bounce height / seconds to apex | `20.25` / `2.4` | Open-air bounce target before ceiling compression |
| Water speed multipliers | Star `0.3`, Box `0.7`, Bubble `1.3` | Current shared water tuning |
| Star water jump multiplier | `0.75` | Initial jump velocity multiplier |
| Camera lower split / upper split | `X = 16` / `Y = 8.5` | Green lower block divides left/right; the green upper floor divides lower/upper views |
| Camera lower-left / lower-right / upper sizes | `10` / `10` / `5.25` | Lower views include more route context and keep the green upper floor visible; the upper view is tighter |

Keep boxes upright and unparented: the current resizing implementation rejects parented boxes. Do not group them under a level container without changing and testing that constraint.

## 4. Verification completed and its limits

The developer reported successful manual tests of:

- Star, box and bubble behavior in the level's water regions, including possession and bubble popping.
- First-pool geometry, alternate exit routes and box resizing passages.
- Bubble-only hazards and the nearby spare bubble, including a newly placed WaterPopHazard prefab instance that pops the bubble without killing the released star.
- C holding P, G opening while held, and G closing when the condition is no longer met.
- D growing into H and G2 remaining open after activation.
- Goal activation as the star, completion feedback, flag visibility and text placement.
- New StarPlayer, BoxBody and BubbleBody prefab instances in WaterPlayground: movement, possession, box resizing/sinking, bubble swimming and popping, with no reported new Console errors.
- A single reusable `BouncyBall` prefab instantiated as `BouncyBall_Lower` and `BouncyBall_Upper`, including centered star marker, waiting state before possession, A/D steering and automatic land bouncing.
- Ball bounce-height compression beneath a low solid roof, with a faster rhythm at shorter heights and restoration of the default height in open space.
- Ball water behavior: surface floating, no sinking, no bouncing in water and automatic land-bounce restoration.
- Ball contact possession in both directions with the star, box and bubble. The current implementation deactivates the original star while hosted, validates live contact before transfer and uses multi-body separation locks; the previous random-transfer reproduction path now requires a fresh manual regression test.
- Lethal spike contact for controlled forms and full-scene restart behavior.
- Three camera regions: enlarged lower-left and lower-right views, centered upper view, bounded local following, smooth position movement, smooth zoom and tracking across possession transfers.

The ball mechanics and camera regions were tested as individual segments in Unity with no reported Console warnings or errors. These results still do not establish that the complete intended route is traversable, that every ball landing is recoverable or that puzzles cannot be skipped.

For temporary segment testing, move the **`StarPlayer` root** in Play Mode. Moving its `Visual` child only moves the sprite, leaving physics and possession at the root. Keep the saved `Visual` local position at `(0, 0, 0)` and restore the intended starting setup before committing the scene.

## 5. Remaining work and handoff

1. **Run the complete route.** Play from the initial star through the final flag. Check puzzle order, alternate paths, accidental shortcuts, C staying on P after transfer, recoverability after losing a bubble, both completed ball sections, recovery after missed landings and restart after failure. Revise geometry against the actual ball physics only if the complete run exposes a problem.
2. **Confirm the lower receiver approach.** Verify that the lower ball can traverse the well, reach the receiver and transfer to C without requiring C to jump.
3. **Tune ball safety bounds.** The ball currently uses an upper death limit of 40 while other Level01 forms use 16. Retain the exception while the 20.25-unit bounce requires it, then reduce it only if complete-route testing proves a lower value safe.
4. **Clean up and finish integration.** Remove the unused R1/R2 marker transforms after their reference value is no longer needed, provide Level01 death feedback if desired, connect Tutorial → Level01 → Level02, and validate the intended build target.

The Level 1 baseline merged into `main` through PR #2; reusable StarPlayer, BoxBody and BubbleBody assets merged through PR #3. Level01 uses these for StarPlayer, Box_A and Bubble_U1; the other boxes and bubbles retain their existing scene setup. WaterPlayground also has saved instances of all three prefabs and an updated initial-body reference.

The current work adds `BouncyBall` to the reusable forms in `Assets/Form` and keeps both Level01 ball instances linked to that one prefab. `SpikeHazard` and `WaterPopHazard` remain in the same folder by team convention. See [ScriptGuide.md](ScriptGuide.md) for setup responsibilities. The reusable ball asset, completed upper hazard layout and regional camera complete the shared mechanics needed for this pass, but they do not replace complete-route validation.
