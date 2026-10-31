# Gameplay Specification

Last updated: 2026-10-31.

This document records current controls, implemented gameplay and unresolved design decisions. For script responsibilities and integration points, see [ScriptGuide.md](ScriptGuide.md).

**Current release: Alpha 0.1.2 (October 18, 2026)** at [the Alpha URL](https://xtraluck21.github.io/StarFormer/alpha/). Source release configuration is `5650250`, gameplay/UI changes are `9b35f8e`, and the `gh-pages` publication is `5f62980`. The developer reported successful localhost and post-publication tests. These reports do not enumerate every checkpoint, device or alternate route; Alpha 0.1.2 Google Sheets delivery has not been independently verified. The current source includes intro progression/hints from PR #10/#11, Level 1 recovery fixes from PR #12 and Level 2/mobile integration from PR #14. See [intro-levels.md](intro-levels.md) for the Alpha 0.1.2 control layout.

**October 18 Level 1 update:** ball checkpoint recovery, a lower-ball A/D prompt, U2 regeneration, visual/camera polish, the Box-to-Bubble collision fix and corrected BouncyBall prefab configuration are merged into main through PR #12 (`e457e87`). The developer reports Level 1 testing passed. Instruction improvements are deferred as low priority. Alpha 0.1.2 adds a wired Restart Level button and includes these fixes in the published build.

**October 18 Level 2/mobile integration:** PR #14 (`3714a49`) merged the Level-2 follow-up into main, preserving intro polish, Level 1 recovery and analytics. E releases Bubble, Spider and Fan. Level02 uses an explicit final-completion screen, corrected Spider/Fan hints and separate Restart/Retry buttons. Alpha 0.1.1 fixes navigation scaling; Alpha 0.1.2 retains that behavior. See [level-2.md](level-2.md).

Publication is separate from source commits. Alpha 0.1.2 is live at `/alpha/`; All-Level V2 remains historical and preserved at the [Alpha Progress Check URL](https://xtraluck21.github.io/StarFormer/alpha-progress-check/). Preserve that folder, `playtest-01/` and the root during future Alpha replacements. The controls below describe current source unless explicitly labeled historical.

**Current source (October 31):** the `intro-bubble` branch rebuilds IntroBubble and its world-space hints, adds return travel to the IntroBox exit and a spare underwater Bubble, and uses **J = Grow, K = Shrink, Q = Pop Bubble**. R opens the restart menu. These source changes are not a new published build; the last recorded release remains Alpha 0.1.2.

## 1. Current scope

| Feature | Status |
| --- | --- |
| Star, box and bubble; automatic contact possession | Implemented and manually tested |
| Box resizing; pressure plate opening a door | Implemented and manually tested |
| Bubble popping and return to the star | Implemented and manually tested |
| Water behavior for star, box and bubble | Implemented and manually tested in WaterPlayground |
| IntroBox and IntroBubble teaching sequence | Original sequence published in Alpha 0.1.2; current branch rebuilds IntroBubble and updates both intros' resize hints, pending a new export |
| Old Tutorial | Retained as a legacy scene; excluded from the current build |
| R restart-choice menu; automatic recovery after death / vertical out-of-bounds | Implemented in source; see CheckpointGuidance.md for pending verification |
| Reusable StarPlayer, BoxBody, BubbleBody, SpikeHazard and WaterPopHazard prefabs | Available in `Assets/Form`; developer-reported instance tests passed |
| Level01 held plate, overhead height switch, purple water hazards, any-form completion and death UI | Implemented; developer-reported tests passed |
| Reusable BouncyBall prefab; automatic land bounce, A/D steering, ceiling compression and water-surface floating | Implemented; two Level01 instances and developer-reported segment tests passed |
| Level01 three-region camera with bounded follow and smooth position/zoom transitions | Implemented and tested for lower-left, lower-right, upper and possession changes |
| Level01 complete route, including final bouncing-ball traversal | Upper traversal, completion and subsequent localhost/online playtests passed. Broader regression remains |
| Main menu, Introductory Levels/Level01/Level02 selection and return buttons | Connected; full intro-to-Level02 flow tested in Unity Play Mode |
| Playable menu star and one-way title/button platforms | Implemented; movement, jumping, foreground rendering and button clicks passed developer tests |
| Alpha 0.1.2 | Current release at `/alpha/`, published as `5f62980`; Player/Analytics versions aligned; developer-reported local/online tests passed |
| Historical All-Level V2 / Alpha Progress Check | Fan-hint fix included; published as `8c48475` and preserved at `alpha-progress-check/` by `5a7f043`; developer reports the fixed URL works |
| Level02 gameplay, camera stages and checkpoints | Integrated into main via PR #14 and published in Alpha; includes final completion UI, responsive navigation and separate Restart/Retry actions |
| Fan, possessable spike wall and spider | Shared controller integration exists, with associated Level02 gameplay sections |
| Fan airflow | Implemented for movable non-possessable obstacles; possessable bodies are excluded |

`Playground` remains a mechanics sandbox, `Level01_Reference` preserves an earlier layout, and `WaterPlayground` isolates water behavior. `IntroBox` and `IntroBubble` now provide the teaching sequence; the old `Tutorial` is retained but excluded from the enabled build list. `Level01` is the first formal level; see [level-1.md](level-1.md). `Level02` contains staged camera behavior, checkpoints and additional forms; see [level-2.md](level-2.md) for the current route, wiring, tuning and verification limits. EnemyScene and FanScene remain separate test scenes.

## 2. Controls

Keys below describe current source. Hold J/K for Box resizing; use A/D for horizontal movement and WASD for underwater Bubble movement; Q, E, Space and R respond to a new press.

| Form / environment | Movement | Ability / exit |
| --- | --- | --- |
| Star | A/D; slower underwater | Space: grounded jump, reduced underwater |
| Box | A/D while grounded; slower underwater | Hold J: grow; hold K: shrink; no jump or manual exit |
| Bubble in air | A/D; rises automatically | Q: pop and return to Star |
| Bubble underwater | WASD, including diagonals | Q: pop and return to Star |
| Fan | A/D while grounded and facing | W: toggle airflow; E: leave |
| Spike wall | A/D while grounded and facing | Space: fire |
| Spider | A/D along connected surfaces | E: leave |
| Bouncing ball | A/D; automatic land bouncing, slower surface movement on water | No manual jump or release |

R opens the restart menu for all forms. It does not immediately reload the scene.

- Left/Right Arrow also work as alternatives to A/D in the current controller. Up/Down Arrow are not implemented as W/S alternatives.
- There is no manual possession key. Contact with an eligible body automatically requests a transfer.
- There is no general key for exiting a box. Q releases Bubble, while E releases Spider and Fan to the Star; neither key is a universal exit action. Space remains the Star jump / SpikeWall fire action. Touch Pop/Leave remains available through the corresponding mobile actions.
- Mobile controls use AutoDetect (Unity mobile-platform flag, or a touchscreen with no mouse); AlwaysShow is an Editor test override, read at Awake. Direction and Box resize buttons are held inputs; action/release buttons trigger on pointer down, and Fan Blow toggles airflow. See [level-2.md](level-2.md) for form-specific controls and device tests.
- R opens RestartMenuUI with Restart Level and Cancel; Return to Checkpoint is available when a checkpoint exists. Only choosing Restart Level clears checkpoints. Explicit scene Restart/Retry buttons keep their own full-restart/retry actions. IntroBubble hides its standalone Restart button and retains a compact Main Menu button. See [CheckpointGuidance.md](CheckpointGuidance.md).
- IntroBubble and Level01 goals briefly pause gameplay for a default 1.5-second completion overlay before advancing automatically; the player controller is disabled during this transition. IntroBox continues directly into IntroBubble after its flag gate opens and the separate exit is reached.

## 3. Shared possession and failure rules

### Possession

- Only eligible possessable bodies can be taken over. Ground, ordinary platforms, doors, water regions and environmental spikes are not possessable.
- Touching another eligible body transfers control to that existing body at its position; it does not create a copy at the player's previous position.
- The star is the only player identity. When it enters a host, the original `StarPlayer` object is fully inactive, including its collider and rigidbody; a centered star marker identifies the controlled host, including the bouncing ball.
- Transferring between hosts moves control to the touched body. Boxes retain gravity; previously activated bubbles retain their environmental physics; abandoned bouncing balls freeze until possessed again. The controller consumes a previous Spider or gravityBall host on transfer.
- `Starting Host` optionally begins a fresh scene inside an existing body. IntroBubble assigns EntryBox while keeping Initial Body assigned to StarPlayer, so Q can restore the original Star later. This is a new scene setup, not physical transport of BoxB across scenes.
- A contact can cause at most one transfer. Every possessable body already touching or immediately surrounding the new host is blocked until it remains safely separated for several physics frames; only a new collision after separation can transfer control again. The queued transfer also verifies that the source and target are still touching before it executes.
- Box → Bubble temporarily ignores collisions only between the old box and the new bubble, allowing the player to escape a box resting on it. The pair becomes solid again after sufficient separation for consecutive physics frames; controller disable also cleans up active exclusions. Other body/terrain collisions are unaffected.
- Contact possession remains active underwater. Popping above a box can therefore lead to automatic box possession when the released star lands on it.

### Death and restart

- A hazard using `SpikeHazard` kills the currently controlled body in any implemented form.
- A hazard using `BubblePopHazard` pops a controlled bubble and releases the star, but kills a controlled box through the normal death/restart flow. This includes a box growing into the trigger. It ignores the star, bouncing ball and unpossessed bodies. The reusable `WaterPopHazard.prefab` displays a pink-red circle with a centered black X to distinguish it from a possessable bubble. Its behavior is not restricted to water.
- Contact by an unpossessed body does not itself kill the player.
- Death pauses gameplay, then reloads the whole current scene after the configured delay. Tutorial, WaterPlayground and Level01 currently save a delay of **2 seconds**. R opens the restart menu and pauses the automatic recovery countdown.
- Level01 displays `You died.` plus `Returning to checkpoint...` or `Retrying from the start...` according to checkpoint availability. Legacy Tutorial displays `You died.`, `Retrying...` and `Press R for restart options.` Level01 uses `LevelDeathUI` and the existing `LevelStatusText`; the controller still owns death timing and scene reload. The text is blank before play and during normal gameplay. The sandbox scenes do not currently contain this death-feedback UI.
- Automatic vertical failure occurs when the entire controlled collider is below `minimumHeight` or above the active form's upper limit. Tutorial and WaterPlayground save **-10 / 8**. Level01 uses **-12 / 16** for the star, box and bubble, while the bouncing ball uses a separate upper limit of **40** so its 20.25-unit open-air bounce does not cause an unintended death. These values are not a general left/right map-boundary system.
- Level01 enables BouncyBall checkpoints: possession records the ball name and authored spawn position. Automatic death reload resets the scene and resumes in that ball; choosing Restart Level clears both checkpoint systems and starts over. Menu level entry and forward progression also clear both. Missing checkpoint balls fall back to a fresh start. This does not preserve plate/gate state.
- Scene reload restores bodies and puzzle objects. Level02 has scene-specific checkpoints: automatic death reload retains the checkpoint and places the initial star at its saved position, with checkpoint events restoring the relevant stage setup. Restart Level clears checkpoints and restarts from the beginning. Entering any level from Mainmenu or advancing through IntroBox, IntroBubble or the Level01 goal also clears the checkpoint session. This is session state, not a saved game across browser launches.
- A stranded box or a route blocked by an abandoned body does not trigger automatic soft-lock detection. Recovery uses the R menu or explicit scene buttons. IntroBubble also has one spare Bubble in its right water tank.

Reusable hazards require the current scene's player controller to be assigned on each instance. Their prefab assets deliberately leave that scene reference empty; see [ScriptGuide.md](ScriptGuide.md) for setup.

## 4. Star — Xiao

- Moves horizontally and jumps from supporting surfaces. It can steer horizontally while airborne.
- Cannot double-jump or freely swim upward.
- Underwater, retains downward gravity and receives water resistance.
- WaterPlayground and Level01 set horizontal target speed to **30%** of base movement speed.
- WaterPlayground and Level01 set underwater initial jump speed to **75%** of normal initial jump speed. This is a velocity multiplier, not a promise of 75% jump height; gravity and resistance determine the resulting height.
- The WaterPlayground tuning was tested to keep the star from reaching the waiting bubble by jumping directly from the pool bottom. This depends on the current pool/bubble placement, not a universal restriction on contacting bubbles underwater.
- No oxygen timer or drowning mechanic is implemented.
- Leaving water restores normal movement selection and the body's original gravity/damping.
- **Wind: planned.** Fan airflow is intended to push the star; exact strength and movement limits are **Undecided**.

## 5. Box — Xiao

- Moves along supporting surfaces and cannot jump. Directional input does not provide active steering while airborne or sinking before reaching support.
- Holding J grows it; holding K shrinks it. Default size limits are **0.5 to 2 times** the initial X/Y scale; scene overrides apply. IntroBox BoxA allows **3.4 times**, and IntroBubble EntryBox allows **3 times**.
- Growth stops when solid geometry blocks the requested size. Grounded resizing preserves the bottom; unsupported resizing preserves the collider center. Trigger hazards are not solid growth blockers: growing a controlled box into a purple water hazard causes death instead.
- Mass remains constant when resized. The current test box has mass **5**.
- Can activate the weight-based plate, an assigned-box held plate, or an assigned-box overhead switch. Their activation and persistence rules differ; see the interactions below.
- Underwater, sinks with added resistance even when unpossessed. Once grounded, horizontal target speed is **70%** of base speed.
- Resizing works underwater; it does not provide buoyancy or active upward swimming.
- **Wind: planned.** The box should be able to move against fan airflow to reach the fan. Full immunity versus strong resistance is **Undecided**.

## 6. Bubble — Xiao

### Waiting and activation

- An untouched bubble is frozen in place until its first possession; a canopy is no longer needed merely to hold a waiting bubble.
- First possession restores the Rigidbody constraints saved at initialization. Activation persists when control transfers away; leaving a bubble does not freeze it again.
- A new scene load creates fresh dormant bubbles. This shared behavior applies to existing levels as well as the intros.

### Air

- After activation, rises automatically through its negative gravity scale; A/D controls horizontal movement while controlled.
- W/S provide no air action, and there is no intentional descent control in air.
- The controller caps upward velocity for the controlled bubble; the same controller cap is not currently applied to an unpossessed bubble.

### Water

- Automatic rising is disabled while its center is inside water.
- WASD provides four-direction swimming, including diagonal input.
- Swimming target speed is **130%** of the body's configured base horizontal movement speed. Diagonal input is limited to the same maximum magnitude.
- The controlled bubble accelerates toward the requested velocity and gradually brakes to rest when movement keys are released.
- A previously activated, unpossessed underwater bubble has zero gravity and water damping: residual motion gradually fades unless another collision or force moves it.
- Exiting water restores its original air physics and upward acceleration. It does not instantly teleport to the surface or erase every component of momentum.

### Popping

- Q pops the controlled bubble in both environments and restores the original star at the bubble's current position. `BubblePopHazard` requests the same queued action through `RequestBubblePop`; a requested pop is processed before a pending possession transfer. If control leaves a bubble by touching another form instead, the abandoned bubble remains in the scene and continues its unpossessed environmental physics.
- The star retains horizontal and downward velocity, while upward velocity is removed so it can begin falling.
- A star released underwater immediately receives its water physics through the ownership refresh.
- By default, the popped bubble is inactive until the scene restarts. A body with ReturnToSpawnOnLeave instead returns to its spawn; this takes priority over replacement spawning.
- Level01 Bubble_U2 has BubbleRespawnOnPop: Q and hazard-triggered pops create a fresh dormant replacement at its authored spawn position. The consumed original is destroyed after successful replacement; U1/U1b do not have this component.
- **Wind: planned.** Fan airflow should push bubbles away; force strength and player resistance are **Undecided**.

## 7. Bouncing Ball — Jingyi

### Waiting and possession

- Both Level01 balls are instances of the same reusable `BouncyBall` prefab. Its saved Form is `BouncyBall` (7), matching the shared enum; the old prefab value 4 represented Spider. The generator now writes the numeric enum value rather than its display index.
- The lower ball shows an A/D steering prompt for 4 seconds on first possession in the scene. Improved instruction design remains deferred.
- An unpossessed ball is kinematic, frozen and motionless. Touching it with the controlled star, box or bubble transfers control to the ball and starts its land behavior.
- The centered star marker appears only while the ball is controlled; it is not placed above the ball.
- Transferring from the ball into another eligible body immediately stops and freezes the abandoned ball. Ordinary terrain collision does not deactivate a controlled ball.

### Land bouncing and steering

- A controlled ball bounces automatically whenever it reaches supporting ground; Space is not required.
- The default unobstructed bounce height is **20.25 world units** with **2.4 seconds to the apex**.
- A/D or Left/Right Arrow steers the ball horizontally. Acceleration and braking are used instead of replacing horizontal velocity instantly; Level01 currently saves acceleration **35** and braking **28**.
- Before launching, the ball checks the solid space above it. A low ceiling reduces the available bounce height.
- When the rising ball strikes the underside of a solid obstacle, it reverses vertically and adopts the compressed height. Shorter bounces use a faster rhythm, reproducing the effect of a real ball bouncing beneath a low board.
- Returning to open space restores the default bounce target after the next grounded launch.

### Water and hazards

- When the ball overlaps a registered water zone, it is positioned at the highest overlapping water surface and cannot sink.
- Vertical velocity is removed at the surface, so the ball cannot bounce while touching water. A/D remains available at **75%** of its land target speed.
- Reaching land restores automatic bouncing without requiring a new possession.
- `SpikeHazard` kills the controlled bouncing ball using the same death/restart flow as the other forms.
- The ball-specific Level01 upper death limit is **40**. This is a survival boundary, not a bounce-height setting.

## 8. Water rules and current tuning

Water entry/exit uses the body's root rigidbody position, centered in the current setups. Edge contact alone is insufficient. Detection checks enabled water-region colliders and supports multiple zones. There is no partial-submersion buoyancy simulation.

Water physics affects both controlled and unpossessed bodies when they have `BodyWaterPhysics`. Original gravity/damping are cached at initialization and restored outside water. Mass is unchanged.

### Saved WaterPlayground and Level01 movement parameters

| Setting | Value | Meaning |
| --- | --- | --- |
| Star base move speed | 5 | World units per second before environmental scaling/physics |
| Star base jump speed | 10 | Initial upward velocity |
| Star water speed multiplier | 0.3 | Horizontal target speed becomes 1.5 |
| Star water jump multiplier | 0.75 | Initial upward velocity becomes 7.5 |
| Box base move speed | 3 | Grounded horizontal target speed |
| Box water speed multiplier | 0.7 | Underwater grounded target speed becomes 2.1 |
| Bubble base move speed | 2.5 | Base horizontal movement setting |
| Bubble water speed multiplier | 1.3 | Maximum swimming target speed becomes 3.25 |
| Swim acceleration / braking | 12 / 10 | Rate of change of swimming velocity |
| Controlled bubble maximum rise speed | 3 | Air upward-velocity cap |
| Ball base move speed | 7.5 | Land horizontal target speed before acceleration |
| Ball water speed multiplier | 0.75 | Water-surface target speed becomes 5.625 |
| Ball acceleration / braking | 35 / 28 | Horizontal velocity change rates |
| Ball default height / seconds to apex | 20.25 / 2.4 | Unobstructed bounce target and timing |
| Box minimum / maximum size | 0.5 / 2 | Multipliers relative to the initial scale |
| Box resize speed | 1 | Size multiplier change per second |

These are target values: damping and collisions can change achieved motion. Inspector settings saved in the scene take precedence over code defaults. The star water speed C# default and both saved scenes now agree at **0.3**. New scenes must still be configured deliberately.

### Saved body physics

| Body | Mass | Original gravity scale | Original linear damping | Underwater gravity scale | Underwater linear damping |
| --- | --- | --- | --- | --- | --- |
| Star | 1 | 3 | 0 | 3 | 2 |
| Box | 5 | 3 | 0 | 3 | 2 |
| Bubble | 0.2 | -0.3 | 2 | 0 | 0 controlled; 2 unpossessed |
| Bouncing ball | 1.2 | 3 | 0 | 0 while surface-locked | 3 while surface-locked |

The controlled bubble uses controller acceleration/braking instead of rigidbody damping while swimming. These are current prototype values, not final balance commitments.

## 9. Introductory levels

The menu starts IntroBox, followed by IntroBubble. IntroBox teaches Star movement/jump, contact possession, J growth into an elevated Box, K shrinking through a gap, and a BoxB flag that opens the exit. Walking beyond that gate loads IntroBubble without a completion overlay.

IntroBubble now uses a rectangular enclosed water tank: EntryBox → dormant Bubble → air ascent and ceiling passage → underwater descent and traversal beneath a divider → right exit shaft → Q pop above safe ground → Star jump to the flag. The grounded-Star goal advances to Level01. A spare Bubble in the right water tank supports recovery after falling back in as Star. It is not an unlimited respawn source.

IntroBubble disables top-center form messages and uses plain map text with key icons/arrows: `You are a Bubble` beside the ascent, `Move freely underwater` within the left water area, and `Pop Bubble / Return to Star` at the safe landing area. Only the pop hint follows any currently possessed Bubble; air/water hints remain bound to the original Bubble. Box hints in both intros show J for growth and K for shrinking.

The bottom-left entrance also returns to IntroBox's endpoint as BoxB, with its exit door open. This is a one-use destination setup, not a saved snapshot of the whole prior scene. Full restart remains a fresh start of the current scene. IntroBubble hides its standalone Restart button and uses a smaller Main Menu button; R opens restart options.

The fixed cameras and existing mobile layout remain: IntroBubble Size 6 at `(0.5, 0.75, -10)` with 120 × 120 semi-transparent controls; IntroBox Size 7 at `(-3, 0, -10)` with 160 × 160 controls. Full scene wiring, exact hint positions and validation limits are in [intro-levels.md](intro-levels.md). These October 31 source changes require a new tested export before appearing in the hosted Alpha.

## 10. Level01 interactions and completion

| Object / script | Activation | Persistence |
| --- | --- | --- |
| `PressurePlate_P` / `HeldPressurePlate` | Only the assigned `Box_C`, grounded with its feet touching the sensor area; no mass threshold and no requirement that C remain controlled | Opens `Gate_G` while held; closes when C leaves or the plate is disabled |
| `Switch_H` / `BoxHeightSwitch` | Only the assigned grounded `Box_D` touching the overhead trigger; placement requires growing D to reach it | Opens `Gate_G2` once and stays activated until restart |
| Purple water hazards / `BubblePopHazard` | Contact by the controlled bubble or box | Bubble: consume it and release the star. Box: kill the player and restart after the death delay. Idle bodies are ignored |
| `LevelGoal` | Any living, controlled form enters the goal while `Gate_G2` is inactive | Shows Level up / Entering Level 2 for a default 1.5 seconds, then clears checkpoints and loads Level02 once |

- P and U2 are horizontally offset. The intended solution deliberately leaves C pressing P while transferring to U2. G spans the right-hand ascent exit, giving nearby feedback about whether the plate remains held.
- H has ordinary solid support above it. It detects grounded contact, not a particular key press or explicit size value. G2 is near the final goal, separate from G.
- The goal shows `Level up!` / `Entering Level 2...` through `LevelTransitionUI`, then loads `Level02` with normal time scale after clearing checkpoint state. It checks G2's current inactive state, not a recorded history of every puzzle or G's state. The star-only restriction was removed so the bouncing ball can finish directly. `Bubble_U3` and `U3_Canopy` were removed because a final bubble-to-star conversion is no longer required.
- Choosing Restart Level in the R menu restarts the current scene, resets switches and restores consumed bubbles. Normal progression is now IntroBox → IntroBubble → Level01 → Level02; Level01 uses a short automatic completion pause rather than waiting for player input at the goal.
- `BouncyBall_Lower` and `BouncyBall_Upper` are working instances of the shared BouncyBall prefab. The old R1/R2 marker transforms remain as inert references only. Upper-route ceiling spikes `(13)`, `(14)` and `(15)` were moved to the upper return floor and rotated upward because the overhead placement made ball traversal too difficult. The developer tested the revised traversal and completion successfully. Passing segment tests is not proof of exhaustive full-route validation.
- Level01 camera regions use `X = 16` to divide the lower-left and lower-right views and `Y = 8.5` to enter the upper view. Lower view sizes are **10**, keeping the green upper floor visible; the upper view size is **5.25**. The camera follows the current controlled form within a bounded range and smooths movement and zoom during transitions.

## 11. Main menu and navigation

- Level02 completes when the current living controlled body overlaps its final flag trigger. The trigger is 1.4 × 1.6 world units with a local Y offset of 0.8. Its gate reference is empty, so no extra plate or Star-only requirement applies. It records completion once, pauses gameplay, and shows **Level Complete**, **You are a super star now!**, and **Return to Main Menu**. The final screen waits for a click/tap; IntroBubble and Level01 retain their timed next-level transitions.


- `Mainmenu` is the startup scene. Its title is `Star Former`, with Introductory Levels, Level 1 and Level 2 buttons. The Main Camera supplies the solid blue background.
- Mouse clicks on Introductory Levels, Level 1 and Level 2 load `IntroBox`, `Level01` and `Level02`, respectively. `OpenTutorial()` remains a compatibility alias for `OpenIntroLevels()` and also loads IntroBox.
- IntroBox, IntroBubble, Level01 and Level02 each have a menu-return button; IntroBubble now labels its compact button `Main Menu`. `MainMenuUI` and `LevelNavigationUI` restore `Time.timeScale` to 1 before loading the destination. Return navigation is available during gameplay and death pauses; the short completion overlay blocks pointer interaction with the old scene UI and clears its selected control. Entering a level through MainMenuUI clears its checkpoint session and starts a fresh run. Automatic death reload can still resume a Level02 checkpoint.
- The menu reuses StarPlayer and the shared possession controller. A/D or Left/Right Arrow moves the star, Space jumps when grounded, and R opens restart options for the menu scene. The star starts above the title and lands on its platform.
- The three level-selection buttons have physical platforms aligned to their UI rectangles. Separate physical platforms align with the top edges of the title and button rectangles, with a ground platform beneath them.
- These are one-way platforms: the star can jump straight up through them and land on top while falling. It does not need to go around a button's edge to ascend. Walking off an edge allows descent.
- The current title spacing is accepted even though the star cannot jump back to the title from the introductory-level button. Returning to the title is not a menu objective or a requirement for entering a level.
- Landing on a button does not launch its level. Level selection remains a mouse action, independent of the optional star interaction. EventSystem navigation events are disabled so jumping does not submit a selected button.
- The menu Canvas uses Screen Space - Camera with Main Camera. The star renders in front of the buttons, so they do not hide it while it passes through them.
- No additional movement hint or attention animation has been added. The team will test whether new players discover the star interaction before deciding on further guidance.

The enabled build scenes are `Mainmenu` (0), `IntroBox` (1), `IntroBubble` (2), `Level01` (3) and `Level02` (4). Tutorial, reference and sandbox scenes are unchecked. Scene transitions use names; keep Mainmenu first for startup. On October 14, the developer confirmed the complete menu → intro → Level01 → Level02 flow in Unity Play Mode. The October 17 contextual hints, Restart buttons, exit arrow and completion transitions also passed developer-reported Play Mode tests. These source changes are now included in Alpha 0.1.2, with developer-reported localhost and online acceptance.

The October 11 release passed developer-reported localhost and online tests for Mainmenu, Tutorial and Level01. On October 12, the developer retested the integrated version in the editor and tested `Builds/WebGL-All-Level-V1` on localhost successfully. The All-Level V1 deployment `6f1737a` introduced all four scenes at [Play StarFormer](https://xtraluck21.github.io/StarFormer/). GitHub Pages deployment success is verified separately from gameplay testing. This was the initial combined baseline; the last recorded root publication is V2, not an archive of V1.

The initial Web build had missing navigation scripts, causing inactive menu buttons and unaligned platforms. The same issue was confirmed in the editor. Restarting Unity and reimporting the navigation scripts restored editor behavior, and a fresh build passed the browser test. This recovery did not require a gameplay change. The precise cause of the stale import/compilation state was not established.

The initial navigation release completed the first Web publishing and playability check. The website is served from the separate `gh-pages` branch; the integrated introductory and formal levels are now published in Alpha 0.1.2. Teammate usability testing and broader browser/resolution coverage remain open.

## 12. Level02 integration and release follow-up

- **Integrated gameplay:** Level02 includes spider surface traversal, a spike-wall chase, projectile/button interactions and fan obstacles. The shared controller now dispatches fan, spike-wall and spider behavior. These are no longer future-only forms.
- **Camera and checkpoints:** `level2Camera` coordinates follow, fixed, chase and falling sections. `level2Checkpoint` records scene-specific respawn positions and invokes stage events on arrival or respawn. Mainmenu entry and confirmed Restart Level reset the checkpoint session; opening/cancelling R preserves it.
- **Fan behavior:** W / Blow toggles airflow and E / Leave releases the star. An abandoned fan retains its airflow state and facing. Current wind targets non-possessable rigidbody obstacles, not the star, box or bubble. Broader form-specific wind behavior remains a separate design decision.
- **Level-specific rules:** Level01 retains its any-form flag completion after G2 opens. Level02 accepts any living controlled form at the flag without a gate/plate requirement and waits on its completion screen for a menu-button click. The unused brown finish block `wall (20)` was removed; it had a sprite/collider rather than a pressure-plate script. Completion and menu return passed developer testing afterward.
- **Presentation and recovery:** Spider shows one small upper-center line, E / Leave: return to star; Fan shows W / Blow: wind on/off and E / Leave: return to star. Level02 has three top-right navigation buttons. Restart Level clears checkpoints; Retry preserves them. The existing mobile Overlay Canvas stays screen-fixed, and its layout was left unchanged. See [level-2.md](level-2.md) for saved values and the unassigned legacy ShowWall checkpoint callbacks that still need review.
- **Release baseline:** All-Level V1 established the October 12 combined release. All-Level V2 adds the fan-hint fix and is preserved as the [Alpha Progress Check submission](https://xtraluck21.github.io/StarFormer/alpha-progress-check/). Current Alpha 0.1.2 uses the fixed `/alpha/` path. Replace only that folder for subsequent tested Alpha versions, leaving historical submissions, `playtest-01/` and the root intact. Continue collecting feedback and performing broader route, checkpoint, navigation and browser/resolution regression checks. Follow [publish_instructions.md](publish_instructions.md) for future deployments.
- **Deferred usability work:** revisit off-screen button/gate feedback and whether players discover the optional menu star interaction. The current intro hint/presentation pass is complete; new-player readability feedback and browser/resolution testing remain.

Update this document alongside changes to controls, form/environment rules or accepted tuning. Keep proposals distinct from implemented behavior and recorded test results.
