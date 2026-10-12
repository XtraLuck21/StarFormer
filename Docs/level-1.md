# Level 1 Progress

Last updated: 2026-10-11.

Scene: [Level01.unity](../Assets/Scenes/Level01.unity). Level baseline merged into `main` through PR #2, merge commit `d8a0c70`. Core prefabs merged through PR #3. Hazard prefab follow-up: `core-form-prefabs`, implementation baseline `af9e0aa`. Design reference: Level 1 Revision 08.

**Status: the current Level 1 polish pass is implemented through `5f134cd`. Any controlled form can finish after G2 opens, the final U3 bubble/canopy have been removed, purple hazards kill controlled boxes and display black X markers, the three upper ceiling spikes now sit on the floor, and death feedback is restored. The developer reported successful tests of these changes, upper ball traversal and completion. This polish pass is now merged into `main` through PR #8 (`c63a17c`). Navigation is merged into `main` through PR #9 (`9fc0f95`), including the Level01 menu entry and return button. The rebuilt localhost version and the GitHub Pages release passed developer-reported playtests. Teammate testing and broader route/browser coverage remain follow-up checks.**

For controls and tuning, see [GameplaySpec.md](GameplaySpec.md). For script responsibilities, see [ScriptGuide.md](ScriptGuide.md).

## 1. Current route and progress

![Level 1 map — Revision 08](Level1_Map.png)

*Historical Revision 08 design reference. The saved scene now differs at the finish: U3 and its canopy are removed, any controlled form can complete, and the former ceiling spike group is on the upper floor. The table below describes current behavior; the map image has not been redrawn.*

| Area | Intended play sequence | Current state |
| --- | --- | --- |
| Start and first pool | Possess A, shrink through the low entrance, sink, then resize to reach U1 | Geometry and existing star/box/bubble behavior tested |
| Underwater traversal | Navigate the central block and purple hazards; recover using spare U1b if a bubble pops | Purple hazards pop controlled bubbles and kill controlled boxes, including contact during growth. Black X markers distinguish them from bodies; developer tests passed |
| First pool exit | Choose the upper star route or land beside B and shrink through the lower passage toward R1 | Both route geometries placed; B's route does not require a jump |
| Deep well and receiver | R1 bounces through the well, reaches the receiver beneath its low roof, then transfers to C | `BouncyBall_Lower` is placed near the original R1 marker. Possession, steering, adaptive low-roof bouncing and stopping after transfer work in segment tests; the complete approach to C still needs route validation |
| Second pool | Bring C underwater, deliberately leave it on P, and transfer to U2 | Held plate and possession tested; P is offset from U2 |
| Right ascent | U2 swims/rises through G while C keeps P pressed | G is positioned across the local ascent exit and tested |
| Upper box puzzle | Possess D and grow upward to touch H, opening G2 near the goal | Height switch, solid support and latched gate tested |
| Upper return | Transfer to R2 beside D and steer across the upper return floor toward G2 and the flag | Spikes `(13)`, `(14)` and `(15)` moved from ceiling to floor and now point upward. Revised ball traversal and completion passed developer tests |
| Finish | Pass opened G2 and reach the flag in any controlled form | Star-only restriction removed; `Bubble_U3` and `U3_Canopy` removed. Completion and restart tested, including the bouncing ball |

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
| `LevelCanvas` | `LevelDeathUI.playerController` / `statusText` | `PlayerControl` / `LevelStatusText` |
| `ReturnToMenuButton` | OnClick | Local `LevelCanvas` → `LevelNavigationUI.ReturnToMainMenu` |

- **P → G is continuous:** only the assigned grounded C can hold it open. Control can transfer away from C while it remains on P. Moving C away closes G.
- **H → G2 is latched:** assigned grounded D touches the overhead trigger once; G2 then remains open until restart. H has no explicit size threshold; its height creates the growth requirement.
- **Purple hazards react by controlled form:** a bubble pops and releases the star; a box dies through the normal restart flow, including when growing into the trigger. Stars, bouncing balls and idle bodies are ignored. All three saved hazards use `WaterPopHazard` prefab instances with scene controller references and black X visual children.
- **The goal accepts any controlled form:** it checks that the player is alive, the entering body is controlled, and G2 is inactive. It does not independently verify every earlier puzzle. U3 is no longer required to convert the player back to a star.
- The water zones are triggers. Bodies use `BodyWaterPhysics` and the common possession controller for their water behavior.
- **Death feedback:** `LevelDeathUI` observes the controller's death state and writes `You died.`, `Restarting...`, and `Press R to restart now.` into the shared status text. The controller handles the two-second death pause and R restart. The text remains blank during normal play.
- **Menu navigation:** `Mainmenu` provides the Level 1 entry. The top-right return button loads `Mainmenu` and restores time scale to 1, including when leaving the completion pause. R still restarts Level01. Re-entering Level 1 starts the scene again.
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
| `Gate_G2` position / scale | `(-4, 11)` / `(0.3, 4)` | Final gate before the goal |
| `BouncyBall_Lower` / `BouncyBall_Upper` positions | `(20.75, 0.55)` / `(39.5, 9.55)` | Two scene instances of the same reusable ball prefab; the upper ball was deliberately moved beside D |
| Legacy `R1_SpawnMarker` / `R2_SpawnMarker` positions | `(21, 0.5)` / `(33, 9.5)` | Empty reference transforms retained temporarily; they no longer drive gameplay |
| Well floor / receiver-bank surface | `Y = -8` / `Y = 3` | Approximately 11 units of rise; validate against R1's actual bounce capability |
| Upper floor / ceiling underside | `Y = 9` / `Y = 13` | Vertical bounds used by the implemented upper R2 challenge |
| `BubbleRouteBlocker` position / scale | `(19, 7.125)` / `(0.6, 2.75)` | Ordinary solid wall hanging from the upper platform to block the bubble route without extending to the lower floor |
| `LevelGoal` position | `(-10.5, 9)` | Final goal for any living controlled form after G2 opens |
| `LevelStatusText` anchored position / size | `(0, -40)` / `(1000, 200)` | Top-center anchor and pivot; saved text empty; space for death and completion messages |
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
- Purple hazard contact: controlled boxes die, including when growing into the hazard; controlled bubbles still pop and release the star. The nearby spare bubble remains available.
- Centered black X markers on all three purple hazards, checked at Full HD with Game view scale 1x.
- C holding P, G opening while held, and G closing when the condition is no longer met.
- D growing into H and G2 remaining open after activation.
- Any-form goal completion, including the bouncing ball, after G2 opens; completion feedback and restart after removal of U3 and its canopy.
- Upper return traversal after moving spikes `(13)`, `(14)` and `(15)` to the floor.
- Death text, automatic restart and R restart, with the enlarged/repositioned status text showing all three lines and its default `New Text` placeholder removed.
- Main menu entry into Level01 and return to the menu, including the completion screen, in the subsequent navigation pass.
- Successful playtests of the rebuilt localhost version and the published GitHub Pages version. The online release includes Tutorial and Level01.
- New StarPlayer, BoxBody and BubbleBody prefab instances in WaterPlayground: movement, possession, box resizing/sinking, bubble swimming and popping, with no reported new Console errors.
- A single reusable `BouncyBall` prefab instantiated as `BouncyBall_Lower` and `BouncyBall_Upper`, including centered star marker, waiting state before possession, A/D steering and automatic land bouncing.
- Ball bounce-height compression beneath a low solid roof, with a faster rhythm at shorter heights and restoration of the default height in open space.
- Ball water behavior: surface floating, no sinking, no bouncing in water and automatic land-bounce restoration.
- Ball contact possession in both directions with the star, box and bubble. The current implementation deactivates the original star while hosted, validates live contact before transfer and uses multi-body separation locks; the previous random-transfer reproduction path now requires a fresh manual regression test.
- Lethal spike contact for controlled forms and full-scene restart behavior.
- Three camera regions: enlarged lower-left and lower-right views, centered upper view, bounded local following, smooth position movement, smooth zoom and tracking across possession transfers.

Earlier ball and camera segment tests reported no Console warnings or errors. The latest developer reports also confirm the revised upper traversal, completion and death UI. They do not establish exhaustive coverage of the whole route, every possible ball landing or puzzle shortcuts. The later localhost and online release playtests also passed. These reports do not enumerate every alternate route, browser or resolution, so keep broader regression coverage as follow-up rather than describing the initial Web test as unfinished.

For temporary segment testing, move the **`StarPlayer` root** in Play Mode. Moving its `Visual` child only moves the sprite, leaving physics and possession at the root. Keep the saved `Visual` local position at `(0, 0, 0)` and restore the intended starting setup before committing the scene.

## 5. Decisions in this polish pass

- The bouncing ball could not conveniently reach the former final bubble to return to star form. Remove the star-only goal condition and the now-unnecessary `Bubble_U3` and `U3_Canopy`.
- Controlled boxes could grow through purple water hazards without a consequence. Make contact lethal to controlled boxes while preserving bubble popping; do not turn these triggers into solid platforms.
- Purple circles resembled possessable bubbles. Add a centered black X to the shared hazard prefab to identify danger.
- Ceiling spikes made the upper ball route too difficult. Move spikes `(13)`, `(14)` and `(15)` onto the upper floor, pointing upward, and verify traversal and completion.
- Death paused and restarted the level without explaining what happened. Add `LevelDeathUI`, reuse the completion text object, enlarge/reposition it for three lines and remove its edit-time placeholder.
- A button and its gate can fall outside the same camera view. The team deferred additional visual or camera feedback for this pass.

## 6. Remaining work and handoff

1. **Broader regression coverage.** The developer reports successful localhost and online playtests. Continue testing alternate routes, both ball sections, C remaining on P after transfer, lost-bubble recovery, failure/restart and completion on teammates' computers. Do not treat this initial release as exhaustive coverage of every route or browser.
2. **Level 1 merge completed.** PR #8 (`c63a17c`) integrated the Level 1 gameplay and polish into `main`.
3. **Navigation merged and Web release published.** PR #9 (`9fc0f95`) integrates Mainmenu, Tutorial/Level01 links, return buttons and the optional menu star interaction. [Play the current build](https://xtraluck21.github.io/StarFormer/). GitHub Pages publishes `gh-pages` from its root, with initial deployment commit `73877e9`. Localhost and online testing passed after restoring missing script imports and rebuilding. Level 2 remains a Coming Soon placeholder. Automatic level progression is not implemented.
4. **Deferred polish.** Revisit button/gate visibility with the team. Retain current ball safety bounds and inert R1/R2 markers until further testing justifies changes. Keep the published build updated after future source changes and repeat browser testing for each release. Review and commit the maintained Web configuration separately from generated rendering data, as documented in ScriptGuide.

The Level 1 baseline merged into `main` through PR #2, core prefabs through PR #3, and hazard prefabs were added in a follow-up. PR #8 merged the later Level 1 polish. PR #9 merged navigation into `main`. The exported Web build is maintained separately on `gh-pages`, which must not be merged into `main`. Forms and hazards remain together in `Assets/Form` by team convention. See [ScriptGuide.md](ScriptGuide.md) for scene references and prefab setup.
