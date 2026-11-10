# Level 2 Progress

Beta 0.1.3 is being prepared from the Level 2 replacement integrated into `main` by PR #23 (`bf8e5e1`), followed by the hint changes through `002368c`. It is not published. The live root and `/beta/` still serve Beta 0.1.2; historical release evidence remains in [Beta-0.1.2.md](Beta-0.1.2.md).

Last updated: 2026-11-10.

Scene: [Level02.unity](../Assets/Scenes/Level02.unity), formerly NewLevel02. The previous layout is preserved as [Level02_legacy.unity](../Assets/Scenes/Level02_legacy.unity) and excluded from the build. Navigation's Level 2 entrance loads the promoted Level02 by name. The release preparation restores its original replacement-scene GUID, `7c566eddba60a82469938fe898ff5d22`, to match Build Settings; the legacy scene retains `688f3d52829cf43438b7318de504e103`.

For shared controls, see [GameplaySpec.md](GameplaySpec.md). For recovery menus, see [CheckpointGuidance.md](CheckpointGuidance.md). For release configuration and verification limits, see [Beta-0.1.3.md](Beta-0.1.3.md) and [publish_instructions.md](publish_instructions.md).

**Current controls:** A/D moves the active form; Space jumps as Star. J grows Box and K shrinks it; Q pops Bubble or leaves Fan. The current controller does not manually release Spider. Fan retains F as an optional wind toggle; the seesaw supply also starts wind and releases its waiting ball on the first direction input or a direction change. Controls belong to the current possessed body, not the inactive original Star.

## 1. Current route and progress

| Area | Intended play sequence | Design purpose |
| --- | --- | --- |
| Opening spike-wall chase | Move and jump as Star while the moving spike wall closes the route behind | Establish urgency, directional movement and platform timing before the slower exploration section |
| Dark descent | Drop into the lower passage, recover light and follow the glowing doorway | Replace pursuit pressure with spatial observation and environmental guidance |
| Seesaw room | Activate blue/green, reach the yellow mechanism as Spider, then damage the board as BouncyBall to activate purple | Revisit one readable space with different form abilities and progressively exposed routes |
| Rising exit | Complete all four gears, wait through the quake and board the platform | Clearly signal puzzle completion and connect the two rooms without an abrupt location change |
| Smiley Face | Swim as Bubble, take BouncyBall, raise three teeth, climb as Spider and enter the forehead as Star | Combine familiar movement with a character-scale visual transformation and a compact traversal challenge |
| Final Box path | Take the hand's Box, resize through the spike-lined descent, press the roof button and touch the raised flag | Test precision and size control after the larger movement puzzles; separate unlocking the goal from reaching it |

### Opening spike-wall chase

The promoted scene starts with Star movement and platforming before a controlled-body doorway trigger begins the enemy-wall sequence. `ChasePassageTrigger`, `spikeWallSpawn`, `SpikeWallEnemyChase` and `level2Camera` coordinate the reveal, pursuit and camera handoff. The wall advances behind the player, while platforms and gaps require forward momentum and correctly timed jumps. Doorway cues and the route's geometry communicate the onward direction; the drop at the end redirects the player into the lower area.

The design first asks players to act quickly using the most familiar form. Camera framing and the pursuing wall establish a clear direction, so difficulty comes from movement under time pressure rather than searching for the next objective. The chase is an enemy encounter, not the separate possessable SpikeWall firing ability.

### Dark descent and exploration

After the escape, Star falls through a gradual darkness transition. `DarkStarPassage` follows descent progress, reduces the surrounding light and retains localized illumination around the controlled body. Glowing pickups can be absorbed to restore illumination; the door's Star emblem and glow identify the next connection. `DropBottomCheckpoint` saves a grounded Star landing and restores the bottom-passage camera and chase state on recovery.

This change of pace gives the previous jumps a consequence without repeating the same chase. Local light reveals a manageable part of the route, while illuminated pickups and the doorway provide an environmental trail. The player learns to read visible landmarks instead of relying on a large block of instructions.

### Seesaw room

Four colored gears communicate progress. The first two goals are blue and green. A Fan directs an ordinary, non-possessable red ball toward either end of the seesaw; the board's rotation brings the colored key contacts into their corresponding lock buttons. The first direction input or a turn can start wind and release the waiting ball. It shakes twice before dropping. The single-ball population rule prevents turns from stacking new balls while an existing ordinary ball remains, including a stationary one. F remains available, but repeated F toggling is not required for this supply.

The blue and green contacts can complete independently. Only after both are active does `SeesawRoomStage` remove the initial player barriers, return the board toward neutral, reveal the next controls and extend the connecting platform. If the player is still Fan, the supply returns control to Star, removes the spent Fan and clears loose ordinary balls while retaining or replacing the waiting spawner ball. The initial energy barriers block controlled forms, including Fan; ordinary balls pass through. The separate translucent blue gear guard prevents Spider from bypassing the intended sequence.

Spider then reaches the yellow mechanism. Its activation produces a sustained gust and releases three ordinary balls in sequence. The gust drives them leftward, and their actual board contacts load the seesaw and reveal its cracks. Fall/spike collection and the supply's cleanup rules manage the balls; this is a physical loading sequence, not just a wind animation. An underground cover conceals the lower route until the player enters it as Spider. The yellow floor crack receives a large floating yellow **!** after five seconds; approaching the crack as Spider opens the passage. The earlier blue/green English captions have been removed.

The underground route leads to BouncyBall. Four underside impacts progressively tint the ball and widen the board fracture. On the last impact, the left section separates and settles into the safe left-cliff bridge; the remaining right section reaches the purple contact and starts the last gear. A return barrier and the gear guard discourage bypassing or backtracking into earlier stages.

The room makes each transformation answer a specific problem: Fan supplies force, Spider accesses surfaces and the crack, and BouncyBall damages the underside. Matching colors and key/lock symbols connect distant causes and effects. Staging the barriers and underground cover keeps later tasks from competing for attention before they become relevant.

### Teleportation and transformation

All four gears must be running before the completion quake begins. The room shakes for two seconds; only after that delay does the central platform rise into view. The player boards it and rides continuously upward for about seven seconds to the lower-left Smiley Face entrance. Although this is the transition platform, it does not use an instantaneous destination jump.

If the rider is BouncyBall, `SeesawBallShellExit` cracks and separates its outer shell at the room exit. Control returns to the original Star during the ride. The destination floor supports the landing, and `SMILE_FACE_ENTRY` saves the new entrance. The gradual ride, camera tracking and visible shell removal make the change of room and form legible without asking the player to reconstruct what happened off-screen.

### Smiley Face room

The large face is both an environmental landmark and the puzzle's visual payoff. Star climbs the shortened entry steps to Bubble, moves right into the water, and swims down to the water-surface BouncyBall. The controlled ball remains at the surface until it moves out, then its automatic bouncing can push the teeth from below. Each impact advances a tooth; all three matching arrival zones are required to open the right door.

While BouncyBall works in the mouth, a fixed camera includes the water edge, all three movable teeth and the door. Completion briefly pulls back to reveal the entire character: the grey teeth merge into a continuous grin along the original mouth outline, the eyes open and the pupils track the player. The reveal is finite and does not replay simply because a completed checkpoint is restored.

Beyond the door, the player must physically contact the waiting Spider. There is no forced conversion merely for crossing the doorway. Successful possession saves the door checkpoint and raises the return wall. Spider climbs the right side, passes its checkpoint and reaches the forehead Bubble. Popping it with Q restores Star for the three timed jumping-spike traps. The forehead camera uses a closer head-and-ears composition, showing the Bubble, hazards and left-hand Box rather than the entire lower room. Leaving this area restores follow, including Bubble backtracking. After the forehead checkpoint is saved, falling near the lower right-ear checkpoint automatically returns the player to the safe Star entrance without recording a death or reloading the scene.

The hand's Box starts the final descent. Contact possession saves `SMILE_HAND_BOX`. K reduces its footprint for the spike-lined passage; J grows it where there is room. The currently restored layout has six larger spikes along the original contour, replacing the temporary small-spike layout. Passage is determined by real collider clearance, not an exact-size scripted gate; minimum scale is 0.5. The original descent uses normal physics and contains no added transport platform. The specific final spike placement still needs a fresh end-to-end playtest in the new export.

Only the assigned Box can press the roof button. The button lowers and raises a flag, but does not itself win the level. The living controlled body must then touch the fully raised flag to display **Congratulations, / you are now a super star**. This gives the resize challenge a concrete endpoint while preserving a final, readable action after the mechanism responds.

### Gameplay improvements and progression

The camera moves from chase framing to dark-passage guidance, a fixed seesaw view, the rising exit, a fixed tooth room, a short completed-face reveal, the closer forehead view and final Box follow. These changes emphasize the current task rather than treating one wide camera as sufficient for the whole level. Bounds adapt to aspect ratio; real mobile acceptance remains separate from these authored rules.

World-space guidance uses glowing routes, colored gears, key/lock graphics, the dashed ball spawner, the yellow Spider symbol and the delayed yellow exclamation. Blue/green text instructions were intentionally removed to reduce visual clutter. Five Smiley Face checkpoints use pure blue hollow stars before activation and yellow after activation, without black borders or permanent text. The level's quick-respawn option shows **Respawning...** for about 0.18 seconds before scene reload, preserving the active checkpoint. Loading time is additional to that configured pause.

Difficulty progresses from time pressure to exploration, then multi-step cause and effect, and finally precise traversal and resizing. Stage-aware recovery limits repetition of completed mechanisms, while the forehead fall-return zone addresses an accidental setback without weakening the three-trap challenge.

## 2. Scene wiring to preserve

| Scene object / system | Component / reference | Assigned behavior |
| --- | --- | --- |
| PlayerControl | `PlayerPossessionController.initialBody` | Original Star; active host changes through contact possession |
| Navigation Level 2 entrance | `NavigationEntrance.destinationScene` | Promoted `Level02`, not `Level02_legacy` |
| Chase and lower passage | `level2Camera`, `ChasePassageTrigger`, `DarkStarPassage` | Star chase trigger, fall darkness, local light and glowing exit |
| Drop landing | `DropBottomCheckpoint` / `DROP_BOTTOM` | Grounded Star save; restore chase completion and bottom framing |
| Seesaw arrival | `SeesawCheckpoint` / `SEESAW_ROOM` | Enter room supply/camera; preserve completed blue/green stage in session |
| Initial mechanism pair | `SeesawColorContact`, `SeesawRoomStage` | Separate blue/green gear contacts; both required to reveal the next stage |
| Yellow stage | `SeesawOverloadSequence`, `SeesawUnderpassCover` | Three-ball gust, cracks, delayed yellow ! and Spider underpass |
| Last seesaw stage | `SeesawBouncyFinale`, `SeesawRoomExit` | Four underside impacts, purple gear, quake and continuous lift |
| Tooth puzzle | `TeethArrivalTriggerZone[]`, `TeethTriggeredDoor` | Three assigned arrivals open the door; no single tooth completes it |
| Face / camera | `SmileFaceCompletion`, `level2Camera` | Connected grin, opening eyes and separate tooth/reveal/forehead frames |
| Door Spider | `SmileDoorSpiderGate` | Contact possession triggers checkpoint and return wall |
| Final route | `SmileFaceFinalRoute`, `SmileRouteCheckpoint` | Ordered session recovery with fixed Star/Spider/Box entry forms |
| Box button / raised flag | `SmileBoxPressureButton`, `SmileGoalFlag` | Assigned Box activates button; actual raised-flag contact displays victory |
| Home / Collection Book | `MainMenuButtonUI`, configured `HelpBookUI` | House/Star menu and yellow book icons; old text-button canvas disabled |

The old CP01/CP02 objects and some legacy stage objects remain in the scene data. They do not replace the dedicated drop, seesaw and Smiley Face systems above. CP03/Check_Mickey and the obsolete right-side BubbleSpawner have been removed. Do not restore a callback or a spawner merely because it appears in the historical October documentation.

**Recovery:** R and Home preserve their existing restart-choice behavior. Full Restart clears checkpoint sessions; returning to Navigation and entering again starts fresh. Automatic death uses the configured checkpoint, not an arbitrary last-possessed body. Blue/green completion is separately latched for seesaw recovery. Yellow loading, damage/impact progression and loose bodies reset on a seesaw-room retry. Later Smiley Face checkpoints restore the teeth, open door, face pose and closed return wall; button/flag progress is retained in the same scene session once pressed. These are in-memory stage records, not a save across browser refreshes.

| Smiley Face point | Save condition | Recovery |
| --- | --- | --- |
| `SMILE_FACE_ENTRY` | Elevator docks | Star at the lower-left steps |
| `SMILE_SPIDER_BASE` | Contact the waiting Spider after the tooth door opens | Spider at the safe door-side landing |
| `SMILE_RIGHT_HEAD` | Controlled Spider enters the right-side checkpoint | Spider at its recorded climbing position |
| `SMILE_FOREHEAD_ENTRY` | Eligible controlled body enters the forehead Bubble checkpoint after the teeth are solved | Star at `(53.4, 27.25)`; closer forehead framing |
| `SMILE_HAND_BOX` | The assigned final Box is controlled in its checkpoint zone | Original-size Box at its authored hand position; follow camera |

Later route checkpoints cannot be overwritten by earlier ones. The automatic right-ear fall return is enabled only for the forehead checkpoint, does not affect the first climb or left-hand Box route, clears momentum/input and retains puzzle progress without a death.

## 3. Saved layout and tuning reference

| Setting | Current value / behavior | Purpose |
| --- | --- | --- |
| Enabled Level02 identity | GUID `7c566eddba60a82469938fe898ff5d22`; build index 7 | Keep the promoted scene distinct from the disabled legacy scene |
| Fan supply | First direction input or turn; one ordinary ball at a time | Reduce repeated F presses and prevent ball accumulation |
| Waiting ball cue | Two shakes, about 0.6 seconds | Connect the dashed spawner to the next falling ball |
| Yellow entrance reminder | Yellow !, 5-second delay, font 16, slight bob | Point at the crack with minimal text |
| Final underside impacts | 4 | Show progressive damage before the purple contact |
| Completion quake / platform reveal | 2 seconds / about 0.9 seconds | Finish the shake before exposing the exit |
| Upward ride | About 7 seconds | Keep the room transition and shell removal visible |
| Teeth | Three separate arrival triggers; ratchet step 0.5, maximum rise 4 | Preserve distinct cause/effect for each movable tooth |
| Face animation / final hold | About 2.6 seconds / 1.4 seconds | Let the completed expression be read before follow resumes |
| Forehead framing | Separate head/ears bounds, about size 13.4 at 16:9 | Make hazards and landing choices clearer than a full-room view |
| Final Box | Authored near `(32.31, 27.38)`; resize range 0.5-2 | Keep a safe starting position before the original descent |
| Death pause | About 0.18 seconds, then scene reload | Short recovery feedback rather than a two-second freeze |
| Checkpoint presentation | Blue/yellow star outlines; no dark outer border | Match the formal Level01 visual language |

Mobile controls remain form-specific and use the existing AutoDetect release configuration. Input comes from the same controller as keyboard movement, water swimming, Fan direction and Box resize. This documentation does not certify all touch/browser combinations or the post-revert spike clearance.

## 4. Verification completed and its limits

The implementation was reviewed against the promoted scene and current scripts, including the scene identity, control bindings, gear order, single-ball rule, four-impact finale, camera phases and checkpoint restoration branches. The release preparation adds a repeatable eight-scene/reference/map validation before the existing WebGL builder. Record its actual results in [Beta-0.1.3.md](Beta-0.1.3.md).

Earlier isolated Unity checks exercised real wind-ball contacts, blue/green cleanup, the yellow gust and underpass, four underside strikes, the quake/lift/shell handoff, tooth collisions, camera transitions, checkpoint deaths, forehead fall return, and the final Box/button/flag sequence. These are implementation evidence from earlier working states, not acceptance of the final Beta 0.1.3 Web export. In particular, the six larger spikes were restored after the temporary minimum-size small-spike test, and the last hint change was a presentation-only adjustment.

The November 10 release build and eight-scene validation passed. Local Chromium loaded Navigation, Level01 and the promoted Level02, switched fullscreen, and opened/closed the Level02 Home and Collection Book. Portrait touch emulation retained the 16:9 canvas and rotation hint. The initial export emitted a legacy empty-Help initialization warning; the obsolete LevelCanvas component and its owner-list entry have since been removed while preserving the configured six-page book. The rebuilt cleanup export passed the same focused browser checks with no Help warning or JavaScript/runtime errors. The final current-layout playthrough, real-device controls and production analytics/map acceptance remain open. A local intercepted request does not prove delivery to the live Sheet.

**Known end-state limitation:** `SmileGoalFlag` currently displays its own congratulatory overlay and stops the winning body. It does not call the shared completion-analytics/Navigation-return flow. Home also requires an enabled player controller. Do not claim the final smile flag records `level_complete` or offers Return to Main Menu until that integration is implemented and tested. This release documentation does not silently change that gameplay behavior.

## 5. Historical October polish decisions

- Use Q / Leave for Spider and Fan, Q / Pop for Bubble. Keep Space for Star jump and SpikeWall fire.
- Use a short upper-center Spider release hint; omit the redundant Star-jump line. Keep the accepted Fan hint.
- Finish on touching the flag in any living controlled form. Do not require a final pressure plate or an unprompted return to Star.
- Keep an explicit completion screen with the celebratory subtitle and a user-selected return to Mainmenu.
- Provide separate Restart Level and Retry Checkpoint actions beneath Return to Main Menu. Preserve their accepted top-right layout and existing Y positions.
- Remove only the unused brown finish block. It was a sprite/collider named `wall (20)`, not a scripted pressure plate.
- Keep mobile button positions unchanged for now. Standardize script comments, Inspector labels, tooltips and diagnostics in English without changing field/method names or gameplay logic.
- Defer top-center form identity labels and broader instruction polish.

Historical Beta 0.1.0 also includes this level (source `3a693ca`, gh-pages `1105970`). Local and online playtests passed according to the developer; the supplied analytics screenshot covers Level01 only, so Level02-specific data and map validation remain open.


## 6. Remaining work and handoff

1. Review the final six-spike Box descent in the exported player. Verify shrinking, both death recovery and manual checkpoint retry, the pressure button, retained flag state and actual flag contact.
2. Review the custom final victory overlay's missing shared completion event and Navigation-return action before public release. This is distinct from the legacy LevelGoal object elsewhere in the scene.
3. Repeat opening chase -> dark passage -> seesaw -> lift -> face on desktop; test interruptions, full restart and Navigation re-entry. Inspect retained legacy CP01/CP02 callbacks separately rather than treating their presence as current-route coverage.
4. Verify Home/Help/R interactions and all form controls on real phones, including multi-touch, focus loss, portrait/fullscreen transitions and held-input release. The obsolete empty Help component has been removed. Keep real-device input, pause ownership and all-form page coverage separate from the focused desktop book check.
5. Keep the seven freshly exported maps and historical entries; export again after subsequent gameplay scene changes. The game build does not deploy the Apps Script backend or update its image store. Verify current `map_version` bindings and production `beta-0.1.3` rows separately.
6. Publish only after approval, using the isolated publishing branch targeting `gh-pages` and updating `beta/` only. Preserve root Beta 0.1.2 and historical Alpha paths. See [the new release record](Beta-0.1.3.md) for the exact build, hashes, commits and acceptance status.
