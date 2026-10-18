# Script Guide

Last updated: 2026-10-18. Unity version: `6000.3.22f1`.

Current source includes the introductory sequence and full progression integrated through [PR #10](https://github.com/XtraLuck21/StarFormer/pull/10), merge `69f3902`, on October 14. The developer confirmed the full flow in Unity Play Mode. The October 17 follow-up on `intro-levels` (merged via PR #11, `989d2d1`) adds contextual Star/form hints, larger subtitles, matching intro colors, Restart buttons, a gate-open arrow and timed completion overlays; developer-reported tests passed. The last recorded published WebGL version remains All-Level V2 / Alpha Progress Check, which predates these changes. See [intro-levels.md](intro-levels.md), [GameplaySpec.md](GameplaySpec.md), and [level-1.md](level-1.md).

## 1. Playable and test scenes

| Scene | Purpose | Relevant features |
| --- | --- | --- |
| [Mainmenu](../Assets/Scenes/Mainmenu.unity) | Startup menu | Introductory Levels/Level01/Level02 selection, playable star and one-way UI-aligned platforms |
| [Playground](../Assets/Scenes/Playground.unity) | Existing mechanics sandbox | Star, box, bubble, resizing, pressure plate and door |
| [Level01_Reference](../Assets/Scenes/Level01_Reference.unity) | Earlier level layout retained as a reference | Traversal and transformation layout; not the new Alpha Level 1 |
| [IntroBox](../Assets/Scenes/IntroBox.unity) | First introductory scene | Contextual Star movement/jump hints, growth-based box transfer, shrink gap, tunnel flag and gated exit arrow to IntroBubble |
| [IntroBubble](../Assets/Scenes/IntroBubble.unity) | Second introductory scene | Starting Box host, air/water bubble traversal, air/water form explanations, E exit, Star-only grounded goal and completion overlay to Level01 |
| [Tutorial](../Assets/Scenes/Tutorial.unity) | Legacy tutorial, excluded from current builds | Movement, jump, three SpikeHazard prefab instances, box possession and resizing, pressure plate, goal, feedback and return-to-menu button |
| [WaterPlayground](../Assets/Scenes/WaterPlayground.unity) | Water integration sandbox | Water detection and water behavior for star, box and bubble; saved instances of the three core prefabs |
| [Level01](../Assets/Scenes/Level01.unity) | Alpha Level 1 graybox in progress | Water routes, box size gates, held pressure plate, height switch, pink-red X-marked hazards that pop bubbles or kill boxes, two bouncing-ball sections, three-region camera, any-form goal leading to Level02, death feedback and return-to-menu button; October 18 editor playtests passed with ball checkpoints and U2 regeneration; new browser validation remains pending |
| [Level02](../Assets/Scenes/Level02.unity) | Integrated second level in All-Level V1 | Spider traversal, spike-wall chase, projectile/button and fan sections, staged camera, checkpoints and return-to-menu button |
| [EnemyScene](../Assets/Scenes/EnemyScene.unity) | Teammate test scene present in this checkout | Not reviewed or playtested as part of the Level 1 work |
| [FanScene](../Assets/Scenes/FanScene.unity) | Fan sandbox | Star, fan possession, W toggles airflow, E leaves the fan (2s repossess cooldown), red blowable obstacles and a box body |

The enabled scenes in [EditorBuildSettings.asset](../ProjectSettings/EditorBuildSettings.asset) are `Mainmenu` (0), `IntroBox` (1), `IntroBubble` (2), `Level01` (3) and `Level02` (4). Tutorial, Level01_Reference, WaterPlayground and Playground remain in the list unchecked. Full-scene restart requires the current scene to be enabled; temporarily enable a sandbox when testing reload behavior. The active build profile must include every transition destination.

## 2. Script map

### Shared character systems

| Script | Attach to / dependencies | Current responsibility | Change here when... |
| --- | --- | --- | --- |
| [PossessableBody.cs](../Assets/Scripts/PossessableBody.cs) | Each body root; `Rigidbody2D`, a root `Collider2D`, and an assigned separate control-visual child | Defines `BodyForm`, including `BouncyBall`; holds movement settings and ownership; exposes grounded/water state; forwards eligible collision contacts to the player controller; freezes untouched bubbles until first possession and restores their saved Rigidbody constraints on activation; logs water transitions | Adding a form identifier or a capability common to bodies |
| [PlayerPossessionController.cs](../Assets/Scripts/PlayerPossessionController.cs) | One `PlayerControl` object; assign an active, configured initial star body | Combines keyboard and touch input; controls the current body; dispatches movement by form; provides accelerated A/D ball steering; deactivates the original star while it inhabits a host; validates live contact before transfer; maintains multi-body separation locks and temporary old-box/new-bubble collision exclusions until separation; supports an optional Starting Host while preserving the Initial Body Star; queues E/touch release for Bubble/Spider/Fan and hazard bubble pops independently of Space; dispatches fan/spike-wall/spider controls; handles death, vertical bounds, scene reload and checkpoint session state; optionally restores the last possessed BouncyBall after death in Level01; exposes the current body to the camera | Adding a form's movement/input integration, changing transfer rules, camera target access or restart behavior |
| [BodyWaterPhysics.cs](../Assets/Scripts/BodyWaterPhysics.cs) | Each water-enabled body root; `Rigidbody2D` and `PossessableBody` | Caches original gravity/damping; applies water gravity/damping to controlled and unpossessed bodies; restores original settings outside water or on disable | Adding a special water-physics rule or changing damping; swimming input belongs in the controller |
| [BouncyBallCheckpointSession.cs](../Assets/Scripts/BouncyBallCheckpointSession.cs) | Created on demand by the controller; survives scene reload | Stores the last possessed ball name, authored position and scene path; enabled through PlayerControl Enable Bouncy Ball Checkpoints in Level01 only. Existing positional checkpoints retain priority; explicit restart, menu level entry and forward progression clear both checkpoint types | Changing Level01 death recovery |
| [BubbleRespawnOnPop.cs](../Assets/Scripts/BubbleRespawnOnPop.cs) | Only bubbles that should regenerate after popping; assign the clean bubble prefab | Creates a fresh unpossessed bubble at its authored position after E or a hazard pop; existing ReturnToSpawnOnLeave takes priority if both components are present | Adding or removing a reusable bubble source in a level |
| [WaterZone.cs](../Assets/Scripts/WaterZone.cs) | Each water-region object with `BoxCollider2D` | Makes the collider a trigger, registers enabled water zones, and exposes point-in-water and overlapping-surface queries | Changing water-region detection or shared surface lookup; do not put individual character movement here |

### Abilities and visuals

| Script | Attach to / dependencies | Current responsibility | Change here when... |
| --- | --- | --- | --- |
| [BoxResizeAbility.cs](../Assets/Scripts/BoxResizeAbility.cs) | Box root; `PossessableBody`, `Rigidbody2D`, `BoxCollider2D` | Reads W/S only while controlled; resizes within limits; checks solid obstacles; preserves the bottom while grounded and the collider center otherwise | Changing box resizing, size limits or clearance checks |
| [BouncyBallAbility.cs](../Assets/Scripts/BouncyBallAbility.cs) | Bouncy-ball root; `PossessableBody`, `Rigidbody2D`, root `Collider2D` | Keeps an unpossessed ball frozen; starts automatic bouncing when controlled; calculates the available height below solid ceilings; shortens and accelerates compressed bounces; floats the ball at water surfaces and disables vertical bouncing there | Changing bounce height/rhythm, ceiling response, waiting behavior or water-surface behavior |
| [StarVisual.cs](../Assets/Scripts/StarVisual.cs) | Star `Visual` or host `StarMark` child; `SpriteRenderer` | Generates a star sprite at runtime; applies `starColor` and sorting order 10; disposes generated assets on destruction | Changing the star/possession marker's appearance, not its controls or collider |
| [FanBlower.cs](../Assets/Scripts/FanSet/FanBlower.cs) | Fan root; `Rigidbody2D`, `PossessableBody` | While controlled, A/D set facing and W toggles wind; pushes non-possessable bodies in range with distance falloff; state persists after the player leaves | Changing fan wind rules, range or force |
| [FanVisual.cs](../Assets/Scripts/FanSet/FanVisual.cs) | Fan `Face` child; `SpriteRenderer` | Generates a ring-and-blades sprite at runtime; spins while blowing and mirrors with facing | Changing the fan face look or spin |
| [FanAirflowVisual.cs](../Assets/Scripts/FanSet/FanAirflowVisual.cs) | Fan `Airflow` child; optional `FanBlower` | Emits white dots outward while blowing; they fade out at max range | Changing airflow particle look or density |
| [RegionalCameraFollow2D.cs](../Assets/Scripts/RegionalCameraFollow2D.cs) | Level01 Main Camera; assign the scene `PlayerControl` | Follows the controller's current body; selects lower-left, lower-right or upper view from world-space boundaries; clamps local follow range and smooths position/zoom during region changes | Changing Level01 camera framing, split boundaries, regional zoom or transition timing |

Box-to-Bubble handoff exclusions are managed in PlayerPossessionController independently of possession locks. Only the departed Box and newly controlled Bubble are excluded. Expanded bounds must separate for the configured consecutive physics frames before collision is restored; switching hosts does not abandon tracking. Disabled/destroyed colliders are removed, and controller disable restores remaining active pairs. No collision-layer matrix changes or additional Unity components are needed.

`BoxResizeAbility` currently requires an upright, unparented box with positive scale and a collider edge radius of zero. The intended box setup uses uniform X/Y scaling. It ignores triggers during clearance checks and does not change mass. Do not parent a box under a level-group object without first updating this constraint.

**Reusable core prefabs:** [StarPlayer](../Assets/Form/StarPlayer.prefab), [BoxBody](../Assets/Form/BoxBody.prefab), [BubbleBody](../Assets/Form/BubbleBody.prefab), and [BouncyBall](../Assets/Form/BouncyBall.prefab) are available in `Assets/Form`. Each includes its physics components, `PossessableBody`, `BodyWaterPhysics`, and its internally assigned control-visual child. BoxBody also includes `BoxResizeAbility`; BouncyBall includes `BouncyBallAbility` and a shared bouncy physics material.

- Drag the required prefabs into a scene and set their positions. Keep boxes at the scene root to satisfy the resizing constraint above.
- Keep one scene `PlayerControl` with `PlayerPossessionController`; assign the active, configured **scene instance** of StarPlayer to `Initial Body`. For a fresh IntroBubble start, assign EntryBox to `Starting Host`; IntroBox leaves Starting Host empty. Do not replace Initial Body with a Box: the original Star is restored when E pops the bubble. The controller is not included in these prefabs.
- Preserve serialized BodyForm values: BouncyBall is 7 and Spider is 4. The shared ball prefab was corrected to 7; Level01 instances already had correct overrides. BouncyBallSetup writes `intValue = (int)BodyForm.BouncyBall`. Do not rerun Build Bouncy Ball Feature merely to repair Form: it also recreates art/prefab/scene placements.
- A BouncyBall instance waits motionless until the controlled body touches it. Once possessed, it bounces automatically on land and uses A/D steering. Do not duplicate or reimplement this behavior in a scene-specific script.
- Level01 uses two instances of the same BouncyBall prefab. `BouncyBall_Lower` is near the original R1 location; `BouncyBall_Upper` is beside D on the upper-right platform.
- Water requires scene objects with `WaterZone` and `BodyWaterPhysics` on water-enabled forms. New bubbles wait in place without a canopy; level roofs may still guide movement after activation. Existing canopies elsewhere have not been globally removed. Platforms, switches, gates and goal UI remain scene objects. Place hazard prefab instances separately and assign their scene controller references as described below.
- When replacing an existing body, check references to that body, especially `Initial Body` and assigned-box switches. Internal `Control Visual` references are already included in the prefab.
- The star sprite is generated in Play Mode by `StarVisual`; an absent edit-time star preview does not by itself indicate a broken prefab. Change a scene instance's placement locally; apply changes to the prefab asset only when they should become shared defaults.

### Interactions, hazards and level feedback

| Script | Attach to / dependencies | Current responsibility | Change here when... |
| --- | --- | --- | --- |
| [HeavyPressurePlate.cs](../Assets/Scripts/HeavyPressurePlate.cs) | Plate; `BoxCollider2D`, `SpriteRenderer`, assigned separate door object | Checks a dynamic body's mass and supporting contact in the plate area; changes color and disables the door; activation stays latched | Changing weight requirements or plate/door behavior |
| [HeldPressurePlate.cs](../Assets/Scripts/HeldPressurePlate.cs) | Plate; `BoxCollider2D`, `SpriteRenderer`, assigned target box and separate door | Opens the door only while the assigned grounded box supports its feet in the plate area; leaving closes it; Level01 P targets C and controls G | Changing continuous plate occupancy; this plate has no mass threshold and does not latch |
| [BoxHeightSwitch.cs](../Assets/Scripts/BoxHeightSwitch.cs) | Overhead switch; `BoxCollider2D`, `SpriteRenderer`, assigned target box and separate door | Latches the door open when the assigned grounded box touches the switch; Level01 H targets D and controls G2 | Changing height-switch activation; required growth comes from placement, not an explicit size threshold |
| [BubblePopHazard.cs](../Assets/Scripts/BubblePopHazard.cs) | Hazard; `CircleCollider2D`, assigned scene player controller | On trigger enter/stay, requests a pop for a controlled bubble or kills a controlled box; ignores star, bouncing ball and unpossessed bodies | Changing form-specific X-marked hazard responses; shared pop/death behavior stays in the player controller |
| [SpikeHazard.cs](../Assets/Scripts/SpikeHazard.cs) | Environmental spike; `PolygonCollider2D`, assigned scene controller | Uses a trigger to kill the currently controlled body; unpossessed bodies do not trigger player death | Changing lethal hazard contact behavior |
| [TutorialIntroUI.cs](../Assets/Scripts/TutorialIntroUI.cs) | Tutorial canvas; assigned box, controller, and two TMP text references | Legacy Tutorial UI; displays initial controls, first box-possession instructions, death and completion messages. Disabled on both intro Canvases in favor of IntroFormHintUI | Changing tutorial copy or its UI states |
| [BouncyBallTutorialPrompt.cs](../Assets/Scripts/BouncyBallTutorialPrompt.cs) | Level01 LevelCanvas; assigned tutorial ball and separate TMP prompt | Shows the ball tutorial on first possession for the configured duration; its component and text remain separate from LevelNavigationUI and the return-menu button | Changing Level01 ball tutorial copy or placement |
| [LevelDeathUI.cs](../Assets/Scripts/LevelDeathUI.cs) | Level01 `LevelCanvas`; assign scene `PlayerControl` and `LevelStatusText` | Clears status text on start; observes `IsDead` in Update and displays death/restart instructions once; does not control pause or reload | Changing level death copy or UI wiring |
| [TutorialGoal.cs](../Assets/Scripts/TutorialGoal.cs) | Tutorial goal; `BoxCollider2D`, assigned tutorial box, door and UI | Completes only when the assigned controlled box enters the goal with the door inactive and the player alive; synchronizes its visible physics pose and pauses the game | Changing tutorial-specific completion conditions or pause behavior |
| [LevelGoal.cs](../Assets/Scripts/LevelGoal.cs) | Level01 goal; `BoxCollider2D`, player controller, required gate, next scene name and optional TMP status text | Accepts any current controlled form after G2 opens; validates the destination and invokes LevelTransitionUI for Level up / Entering Level 2, then clears checkpoints and loads Level02 once | Changing Level01 completion conditions or the next-scene destination |

**Reusable hazard prefabs:** core forms and hazards share `Assets/Form` by team convention. Being stored in this folder does not make a hazard possessable.

| Prefab | Included components | Scene setup / behavior |
| --- | --- | --- |
| [SpikeHazard](../Assets/Form/SpikeHazard.prefab) | Sprite renderer, trigger `PolygonCollider2D`, `SpikeHazard` | Assign the scene's `PlayerControl` to `Player Controller`; contact kills the currently controlled form. TutorialSpike01–03 now use this prefab. |
| [WaterPopHazard](../Assets/Form/WaterPopHazard.prefab) | Pink-red sprite, black `Cross_A` / `Cross_B` visual children, trigger `CircleCollider2D`, `BubblePopHazard` | Assign the scene's `PlayerControl` to `Player Controller`; contact pops a controlled bubble or kills a controlled box. The asset name differs from its script name. |

- Both prefab assets intentionally store `Player Controller` as None. Set it on **each scene instance before Play Mode**; do not try to store a reference to a particular scene inside the shared asset.
- If that reference is missing at initialization, the script logs an error and disables itself. Exit Play Mode, assign the reference, then test again.
- `WaterPopHazard` has no water-only check: placing it underwater is a level-design choice. It ignores the star, bouncing ball and unpossessed bodies. Box growth still ignores triggers for clearance, so expanding into this hazard causes death rather than stopping growth.
- The black X uses two centered Square sprite children with Z rotations of +45 and -45 degrees. Both render at order 3 over the pink-red circle at order 2. They have no colliders and do not change the hazard hit area.
- Developer-reported tests passed for spike death/restart, controlled box contact and growth into purple hazards, preserved bubble popping, and black-X readability in Level01.

**Level01 feedback setup:** keep `LevelCanvas` and `LevelStatusText` active. Both `LevelGoal.statusText` and `LevelDeathUI.statusText` reference the same TMP object. Keep its saved text empty. Its top-center anchor/pivot, position `(0, -40)` and size `(1000, 200)` provide space for the three-line death message. The developer confirmed death and historical completion text display after the layout adjustment. Current LevelGoal does not write completion copy into LevelStatusText. It invokes a separate generated LevelTransitionUI overlay, pauses gameplay for a default 1.5 seconds and then advances to Level02. Keep the text reference for LevelDeathUI. Full HD preview helped verify the small hazard markers; fit the Game view to the available panel when checking text near the screen edge.

### Introductory-level scripts and visuals

| Script / asset | Setup and responsibility |
| --- | --- |
| [BoxGrowHint.cs](../Assets/Scripts/BoxGrowHint.cs) | Separate scene root with controller and target Box references. Grow/Shrink modes draw a fixed dashed size guide, arrows and boxed W/S. Visibility requires the controlled target's center inside Activation Area. Shrink hides once the box drops completely below the frame. Full visuals are created in Play Mode; selected gizmos show placement bounds. |
| [BubbleMovementHint.cs](../Assets/Scripts/BubbleMovementHint.cs) | Separate world-space root. Air = A/D, Water = WASD, ExitBubble = E, StarMove = A/D, StarJump = SPACE plus jump arc; these modes require the assigned controlled body inside the activation region (and matching Bubble water state). ContinueRight uses only Flag Gate and appears when that gate opens. Optional edit preview. Continue Arrow Width defaults to 0.08 and affects only ContinueRight; its arrowhead is larger. Keep enum values stable for saved scenes. |
| [IntroFormHintUI.cs](../Assets/Scripts/IntroFormHintUI.cs) | Intro TutorialCanvas; assign local controller plus separate title/subtitle TMP objects. Sequence Box/Bubble selects intro behavior. Observes form/water changes; Box sequence also detects Box-to-Box body changes. Titles persist; subtitles hold 4 seconds and fade over 0.75 seconds by default. Clear text on death/disable. Disable legacy TutorialIntroUI to avoid competing text writes. |
| [LevelTransitionUI.cs](../Assets/Scripts/LevelTransitionUI.cs) | Created at runtime by IntroStarGoal/LevelGoal; no scene attachment required. Builds a 1920×1080-reference overlay (sorting order 32000), pauses time, disables the player controller, intercepts pointer input and clears old UI selection. ShowAndLoad resets checkpoints and loads the destination after a real-time delay. ShowFinalResult instead displays Level Complete / You are a super star now! with a menu button and no automatic timeout; Level02 opts into this mode through LevelGoalToMenu. Returning restores normal time and clears checkpoints. Guards against duplicate active transitions and restores owned state on disable. |
| [CircleVisual.cs](../Assets/Scripts/CircleVisual.cs) | Bubble SpriteRenderer object. Optional Show Outline, Outline Color and Outline Width bake a border inward into the same sprite; Circle Color controls the fill. The circle's outer size and Collider are unchanged. Outline is opt-in, not a new default for all circles. |
| [IntroFlagGate.cs](../Assets/Scripts/IntroFlagGate.cs) | IntroBox flag trigger; assign controller, required BoxB and separate exit door. The required controlled body's contact opens the door, without loading another scene. |
| [IntroSceneExit.cs](../Assets/Scripts/IntroSceneExit.cs) | Separate trigger beyond that door; assign controller, required flag gate and next scene `IntroBubble`. Once the gate is open, contact by the current controlled body loads the destination and clears checkpoints. |
| [IntroStarGoal.cs](../Assets/Scripts/IntroStarGoal.cs) | IntroBubble flag trigger; assign controller and next scene `Level01`. Only the living, controlled, grounded Star completes it. Bubbles and airborne Stars cannot trigger completion. Next Level Label defaults to Level 1 and Transition Seconds to 1.5; the goal invokes LevelTransitionUI after destination validation. |
| [GoalFlagVisual.prefab](../Assets/Form/GoalFlagVisual.prefab) | Reusable dark-green pole and green triangular flag. Visual only; use separate scene triggers for gate/goal behavior. |

Bubble movement hints use Sorting Order 0 for key backgrounds and 1 for strokes, below the bubble at 2 and the Star marker at 10. Growth hints retain their existing order. In IntroBubble, the bubble uses a pale fill and a dark-blue inward border (saved width 0.06). Both intro cameras remain fixed; camera-follow redesign is deferred. Automatic-rise guidance now comes from IntroFormHintUI subtitles. Both intros use warm-gray backgrounds and dark terrain; saved title/subtitle sizes are 48/32. The gate-open continuation arrow uses order 20, independent of the Bubble prompts.

IntroBox uses StarMoveHint, StarJumpHint and ContinueRightHint. Bind ContinueRightHint.Flag Gate to the local IntroBoxFlagTrigger; do not parent the hint under the door that gets disabled. It does not need a target body, player controller or activation area. In each intro, keep TutorialTitle/TutorialInstructions active, clear obsolete saved instructional copy, and assign them to IntroFormHintUI with the local PlayerControl. Do not delete the TMP components. See [intro-levels.md](intro-levels.md) for exact messages, the playable sequence, recovery limits and handoff checks. Scene-specific hint/controller/goal references must be assigned on each instance, not stored as references to another scene.

### October 18 Level01 integration and verification

The `level-1` branch preserves main's E-pop queue, Starting Host, Level02 checkpoint API, menu references and timed completion flow. Ball recovery is opt-in and enabled only in Level01; existing positional checkpoints take precedence. The merge keeps separate LevelNavigationUI and BouncyBallTutorialPrompt components on LevelCanvas, with separate menu-button and tutorial-text bindings.

The developer reports Level 1 testing passed after `1f07b35`, including the former box no longer trapping the new bubble and normal ball behavior. Runtime and Editor script compilation passed. Instructions are usable but further clarity improvements are low priority. A new WebGL build and mobile/browser validation are not recorded.

InputActionState null-reference errors stopped after enabling Reload Domain and Scene and restarting Unity. EditorSettings now saves `m_EnterPlayModeOptions: 0` with options enabled. This successful recovery does not establish the original cause; do not attribute it to the bubble collision fix.

### Level02 integration points

| Script | Responsibility |
| --- | --- |
| [level2Camera.cs](../Assets/Scripts/StartingStage/level2Camera.cs) | Coordinates follow, fixed framing, spider reveal, chase, falling and later follow stages, with references to other camera behaviors |
| [level2Checkpoint.cs](../Assets/Scripts/StartingStage/level2Checkpoint.cs) | Saves a checkpoint ID and respawn position for the controlled body and invokes stage events on arrival and checkpoint respawn |
| [HexagonSurfaceMovement.cs](../Assets/Scripts/spiderFolder/HexagonSurfaceMovement.cs) | Spider traversal along connected surfaces, driven by the shared controller |
| [SpikeShooterAbility.cs](../Assets/Scripts/SpikeWall/SpikeShooterAbility.cs) | Possessable spike-wall facing and projectile firing |
| [SpikeButton.cs](../Assets/Scripts/SpikeWall/SpikeButton.cs) | Projectile/button interaction in the spike-wall section |

`PlayerPossessionController` keeps checkpoint identity, scene and position in session state. Automatic death reload preserves that state, while R clears it. `MainMenuUI.LoadLevel` calls `ResetCheckpointSession()` before loading any selected level, preventing an old checkpoint from turning a menu selection into an unintended resume. IntroSceneExit, IntroStarGoal and LevelGoal also clear checkpoint state on forward progression. Do not assume scene reload alone means a fresh run. Level01 retains its regional camera and G2/flag prerequisite, then loads Level02.

## 3. Navigation and interactive menu

| Script | Attach to / dependencies | Current responsibility |
| --- | --- | --- |
| [MainMenuUI.cs](../Assets/Scripts/MainMenuUI.cs) | `Mainmenu` scene's `MainMenuCanvas` | `OpenIntroLevels()` loads `IntroBox`; `OpenTutorial()` is a compatibility alias for the same route; `OpenLevel1()` loads `Level01`; `OpenLevel2()` loads `Level02`; clears checkpoints and restores time scale to 1 before loading |
| [LevelNavigationUI.cs](../Assets/Scripts/LevelNavigationUI.cs) | Return UI in IntroBox, IntroBubble, Level01 and Level02; legacy Tutorial also retains it. Intro Canvases also host Restart buttons | `ReturnToMainMenu()` restores time scale and loads `Mainmenu`. `RestartLevel()` validates the current scene build index, clears checkpoints, restores time scale and reloads that scene, matching R. `RetryCheckpoint()` reloads while preserving checkpoints (falls back to the scene entrance if none); both buttons record a manual restart rather than a death |
| [MenuUIPlatform.cs](../Assets/Scripts/MenuUIPlatform.cs) | Each menu platform root; requires `BoxCollider2D` and `PlatformEffector2D`; assign target UI rectangle and Main Camera | Converts the target's upper corners to world coordinates, aligns a thin collider with its top edge, and configures one-way collision |

### Button wiring and scene names

- The button displaying `Introductory Levels` calls the local `MainMenuCanvas` component's `MainMenuUI.OpenIntroLevels`. The button object is IntroLevelsButton; its physical platform still uses the legacy name TutorialPlatform.
- `Level1Button.OnClick` calls `MainMenuUI.OpenLevel1` on the same scene object.
- `Level2Button` displays `Level 2` and calls `MainMenuUI.OpenLevel2`. `Level02` is enabled at build index 4.
- `ReturnToMenuButton` in IntroBox, IntroBubble, Level01 and Level02 calls `LevelNavigationUI.ReturnToMainMenu` on that scene's own assigned UI object. Preserve the local reference when copying UI between scenes.
- IntroBox and IntroBubble place `RestartButton` beneath the menu button, labeled Restart (R), with a single callback to the local TutorialCanvas → LevelNavigationUI.RestartLevel. Remove the copied menu callback. Restarting IntroBubble returns to EntryBox in IntroBubble, not IntroBox.
- Level02 now has `ReturnToMainMenuButton`, `RestartLevelButton` and `RetryCheckpointButton` under Canvas. Each calls the matching method on its own LevelNavigationUI component, with one callback. All use top-right anchors `(1, 1)`, pivot `(1, 0.5)`, size `360 × 70`, X `-30`; their accepted Y values are `-57`, `-142` and `-227`. These buttons are already wired, not pending setup.
- Keep the exact scene strings `Mainmenu`, `IntroBox`, `IntroBubble`, `Level01` and `Level02`. Forward progression is name-based: IntroBox exit → IntroBubble, IntroBubble Star goal → Level01, Level01 goal → Level02. Menu entry and forward progression start fresh checkpoint sessions; death recovery can retain a Level01 ball checkpoint or an existing Level02 positional checkpoint.

### Menu star and platform setup

- Keep the scene StarPlayer and `PlayerControl` at the scene root. Assign the scene StarPlayer to `PlayerPossessionController.initialBody`. Reuse existing star movement and jump behavior rather than adding a second movement controller.
- `TitlePlatform`, `TutorialPlatform`, `Level1Platform` and `Level2Platform` each use `MenuUIPlatform`. Their `targetUI` references are `TitleText`, `IntroLevelsButton`, `Level1Button` and `Level2Button`, respectively. All reference Main Camera and use thickness `0.12`.
- Keep platform roots at identity rotation and unit scale. The script positions them during Start and LateUpdate, after forcing the initial Canvas layout update. Edit-mode positions do not represent their aligned runtime positions.
- The collider follows the top of the UI rectangle, not the visible text glyphs. It is a non-trigger collider with Used By Effector enabled. The script sets Use One Way and Use One Way Grouping, a surface arc of 180 degrees, and disables side friction/bounce.
- The star may pass upward through buttons and land on their tops while falling. `MenuTestGround` is a separate solid ground platform below the button stack. Contact with these platforms does not select a level.
- The star initially lands above the title. The accepted layout does not require it to jump back from the introductory-level button to the title. Lower buttons get progressively wider.
- Keep the menu EventSystem's `InputSystemUIInputModule` for mouse input, with **Send Navigation Events disabled**. This prevents movement/jump keys from navigating or submitting UI buttons.

### Rendering and coordinate conversion

- `MainMenuCanvas` uses **Screen Space - Camera**, assigned Main Camera, plane distance **10**, Default sorting layer and order **0**. Canvas Scaler uses Scale With Screen Size, reference **1920 x 1080** and width/height match **0.5**.
- The menu camera is orthographic and faces the XY plane from Z = -10. `MenuUIPlatform` projects onto world Z = 0 and passes `worldCamera` to `RectTransformUtility.WorldToScreenPoint`. It is configured for this camera-backed Canvas, not an Overlay Canvas with a null camera.
- StarPlayer's menu visual uses an unlit sprite material. `StarVisual` sets its sorting order to **10**, above the Canvas on the same Default sorting layer. This allows the star to remain visible while crossing button graphics. Increasing sprite order alone would not put it above a Screen Space - Overlay Canvas.
- Preserve the Canvas camera and the platform conversion together when changing rendering modes. Verify collision alignment and mouse clicks after changing layout or aspect ratio. GraphicRaycaster currently has Blocking Objects set to None.

### Verification and follow-up

On October 14, the developer confirmed the renamed menu entry and complete Mainmenu → IntroBox → IntroBubble → Level01 → Level02 flow in Unity Play Mode. Script compilation checks passed during implementation. On October 17, the developer also confirmed Star/form hints, Box-to-Box subtitles, Bubble explanations, palette/readability changes, current-scene Restart buttons, the thicker continuation arrow and timed completion transitions. These updates still need a new WebGL export/browser test.

Historical developer-reported tests passed for loading Tutorial and Level01, returning to the menu, moving/jumping across menu platforms, and keeping the star visible in front of buttons while clicks still work. The October 11 rebuilt localhost version and published GitHub Pages version subsequently passed developer playtests. No menu-star movement hint or attention animation is included. Level 2 integration is complete and included in All-Level V1. The October 12 editor retest and localhost build test passed according to the developer. Teammate usability feedback and broader resolution/browser coverage remain follow-up work.

## 4. Web build and GitHub Pages release

For the repeatable publishing procedure, follow [publish_instructions.md](publish_instructions.md). This section records release history and troubleshooting. The introductory source updates through October 17 have no new WebGL publication recorded. PR #11 merged the October 17 intro polish. The October 18 Level 1 update is tested on level-1 and awaiting its own PR; neither source update constitutes a new Web publication.

### Last recorded published version: All-Level V2 / Alpha Progress Check

- Date: October 12, 2026 (America/Los_Angeles).
- Latest tested release: [StarFormer](https://xtraluck21.github.io/StarFormer/). This URL can change with future releases.
- Frozen submission: [Alpha Progress Check](https://xtraluck21.github.io/StarFormer/alpha-progress-check/). Do not overwrite this folder.
- Preservation commit: [`5a7f043`](https://github.com/XtraLuck21/StarFormer/commit/5a7f0434ae669ce347fcb5a239f034154d46e509). At preservation time, its 17 exported files matched the root build. The deployment succeeded and the developer confirmed the new URL works.
- Source revision: `d161538cdbbe3affba9094de12facaa637e71c5f`, including the teammate fan-hint fix. Local build-generated configuration/asset changes were also present.
- Output: `Builds/WebGL-All-Level-V2`. The copied publishing files were verified against this output.
- Published commit: [`8c48475`](https://github.com/XtraLuck21/StarFormer/commit/8c48475a6381a21dd67def33446de64466a8bcc0). GitHub Pages reports `built` for that exact commit.
- Scope: Mainmenu, Tutorial, Level01 and Level02. The developer reports the build works. A separate detailed online gameplay test report is not recorded here.
- The old `Local build settings before teammate hint fix` stash was deleted at the developer's request after the new build worked. It was not reapplied. Four build-generated modifications were noted at that historical documentation update; this is not a current working-tree inventory.

### Previous version: All-Level V1

- Historical location: the root URL, since replaced by V2. V1 has no separately preserved live URL in the current publishing layout; its exported files remain in Git history.
- Release: **All-Level V1**, October 12, 2026 (America/Los_Angeles), the initial combined baseline.
- Source baseline: `main` at `773912d73866b401fb15fa1551b1c434b7f829db` (reset checkpoints on menu entry), plus the local editor/build changes described below. The build is not represented as a pristine checkout of that commit.
- Publishing branch: `gh-pages`, root directory. Release commit: [`6f1737a`](https://github.com/XtraLuck21/StarFormer/commit/6f1737af361b2bf30d06d150ece4ba4551126470). The developer committed and pushed it, and the Pages build API reports `built` for that exact commit.
- Included scenes: Mainmenu, Tutorial, Level01 and Level02.
- Tested output: `Builds/WebGL-All-Level-V1`. Editor retesting and localhost playtesting passed according to the developer. Deployment success is confirmed, but a separate detailed online gameplay test report for this release has not been recorded here.
- Previous release: October 11's `Builds/WebGL-Test2`, published as `73877e9`, included Mainmenu, Tutorial and Level01 and passed developer-reported localhost and online testing. Keep it as historical release context, not the current deployment.

### Build configuration

Use Unity `6000.3.22f1` with Web Build Support. Select Web in Build Profiles and confirm Mainmenu (0), IntroBox (1), IntroBubble (2), Level01 (3) and Level02 (4) are enabled. Keep the old Tutorial unchecked. Use the Default Web template, Gzip compression, Decompression Fallback enabled, and a normal build with Development Build unchecked. Thread support remains disabled.

Use Build And Run to test the generated site through localhost. Do not test by directly opening `index.html` as a local file. Keep outputs under the ignored `Builds` directory and outside Assets. Preserve `index.html`, `Build`, `TemplateData` and any other generated runtime files as one set.

### Publishing and updating

Follow [publish_instructions.md](publish_instructions.md) for commands. The root URL serves the latest tested release. Each submitted milestone has a separate folder containing its complete export, including its own `index.html`, `Build/` and `TemplateData/`. Keep `alpha-progress-check/` unchanged and create new folders for future Alpha/Beta submissions.

Update only the root export files when replacing the latest build. Never run a root-wide synchronization with deletion: it would delete preserved version folders. Existing relative asset paths allow the copied build to run inside its own folder without editing its generated HTML.

GitHub Settings → Pages remains **Deploy from a branch → gh-pages → / (root)**. One deployment contains the root and all version folders; the Actions deployment card displays the site base URL, not every version path. Test the latest and new version URLs and confirm the existing submission still loads.

`main` contains Unity source, maintained configuration and documentation. `gh-pages` contains the exported website. Do not merge `gh-pages` into `main`. Updating source alone does not update the hosted game; export and publish a new tested build. Use fixed milestone URLs in submission documents.

### Missing navigation scripts during the first build

The initial build log reported missing or invalid scripts on MainMenuCanvas, the four menu platforms, TutorialCanvas and LevelCanvas. MainMenuUI, MenuUIPlatform and LevelNavigationUI were absent from the editor/player compilation inputs even though their source files and scene references existed. Comparison with the navigation branch showed no missing source merge. Editor Play Mode also reproduced the fault.

Closing and reopening Unity, reimporting the three scripts, and waiting for compilation restored editor behavior. A new Build And Run output then passed localhost testing and was used for publication. The exact cause of the import/compilation inconsistency is unresolved. If this recurs, check the Console and component references, refresh imports and verify editor play before rebuilding. Do not remove and recreate serialized components as the first recovery step. Edit-mode platform placement and effector warnings alone are not proof of failure, because MenuUIPlatform configures its collider in Awake and aligns it in Start/LateUpdate.

### Input System error during the October 12 editor test

The editor log recorded `Map index out of range`, control/binding index errors and `NullReferenceException` inside `InputActionState` while processing keyboard input. Domain Reload was disabled. The diagnostic procedure was to stop Play Mode, enable Reload Domain and Scene, restart Unity and retest. The developer reported that retesting passed and then built All-Level V1, which passed localhost testing. The local EditorSettings diff confirms `m_EnterPlayModeOptions` changed from 1 to 0. This is a successful recovery, not proof of the underlying cause or a gameplay-code fix.

### Configuration state at the All-Level V1 documentation update

At the All-Level V1 documentation update, the source checkout had the following five non-document changes. That historical set was later stashed before pulling the hint fix, and the stash has now been discarded. It is not the current working-tree inventory:

| File | Observed change / handling |
| --- | --- |
| `ProjectSettings/EditorSettings.asset` | Enables Domain Reload as well as Scene Reload for Play Mode. Review as a separate maintained editor-setting change |
| `ProjectSettings/ProjectSettings.asset` | Build-added input-action entry in `preloadedAssets`. Review separately from maintained Web settings; the Input System build processor temporarily adds this entry |
| `Assets/Settings/UniversalRP.asset` | Shader prefilter changes generated during build. Review separately from source configuration |
| `Assets/Settings/UniversalRenderPipelineGlobalSettings.asset` | Generated runtime settings list populated during build. Review separately from source configuration |
| `Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF - Fallback.asset` | Only file mode changed from 100755 to 100644, with no content change |

Gzip and decompression fallback are already present in the source baseline. The previous default Bloom filter completion is no longer an outstanding local change. Stage documentation files explicitly so unrelated configuration or generated asset changes are not included. These local source changes do not alter the already published build unless a new build is exported and deployed.

### Local changes after building

At the October 12 Alpha Progress Check documentation review, `main` still had four unrelated local changes: URP shader prefilter values, the render pipeline runtime-settings list, an Input System Actions reference in `preloadedAssets`, and a TMP fallback font mode change (100755 to 100644; identical content). The installed render-pipeline and Input System packages contain build processors that write the first three kinds of data. The exact reason the preload entry remained, and the origin of the font permission change, have not been established.

These changes belong to the source checkout. Pushing documentation sends only committed changes; pushing `gh-pages` sends the exported website from the publishing checkout. Neither operation commits or clears uncommitted source files. Unity may update tracked assets during imports or builds even without manual gameplay edits.

Review `git diff` and `git diff --summary` after closing Unity. Commit intentional project settings separately; back up or discard confirmed generated-only changes only after review. Do not add these tracked settings to `.gitignore` or include them automatically in a documentation commit. The October 12 documentation update left those four files unchanged; inspect the current working tree instead of assuming they are still present.

### Why the publishing checkout is separate

`StarFormer` on `main` is the editable Unity project. `StarFormer-Pages` on `gh-pages` is the exported static website (`index.html`, Build payloads and TemplateData). GitHub Pages serves these generated files and does not compile the Unity project. Keeping a separate folder avoids switching the open Unity project to a branch containing only website files. Keeping a separate branch separates source history from generated release files and allows a tested website version to remain published while source development continues.

This separation is a workflow choice, not a GitHub Pages requirement. Other workflows can deploy an artifact with GitHub Actions or publish from a configured docs directory. Continue using the existing separate publishing checkout for this project, and do not merge `gh-pages` into `main`.

## Level 2 and mobile integration (October 18)

- [MobileControlUI.cs](../Assets/Scripts/Mobile/MobileControlUI.cs) defines `MobileControlsUI`: Awake resolves AutoDetect/AlwaysShow/NeverShow, then it selects buttons by controlled form, water state and death state. Set AlwaysShow **before Play**, then restore AutoDetect before publishing. There is no custom browser user-agent detector. Scene references to the controller and all action buttons must remain assigned.
- [TouchHoldButton.cs](../Assets/Scripts/Mobile/TouchHoldButton.cs): Left/Right/Up/Down maintain held input. Action/Release/Blow/Restart invoke the controller on pointer down. Pointer up and disable clear held direction state. Mobile restart retains the full-scene semantics and analytics of R.
- `PossessableBody.Owner` exposes the controller to BoxResizeAbility and FanBlower so touch and keyboard input share resize/direction behavior. Fan W and touch Blow both toggle airflow; E/touch Leave releases the host.
- [LevelGoaltoMenu.cs](../Assets/Scripts/LevelGoaltoMenu.cs) defines `LevelGoalToMenu`. `waitForMenuButton` is enabled on Level02's flag. It requires the live current controlled body, validates the menu scene and records completion once. The old delayed-return path remains available to other existing instances. The widened Level02 flag trigger removes the need to overlap only the thin pole; no extra plate or Star-form condition is configured.
- `LevelTransitionUI.ShowFinalResult` shows Level Complete / You are a super star now! and waits for Return to Main Menu; it does not automatically leave Level02. The Spider hint is a single size-3 line at `(-70, 15.2)`. Fan retains W / Blow and E / Leave. The unused finish block `wall (20)` has been deleted.
- `LevelNavigationUI.RetryCheckpoint()` preserves checkpoint state on reload; `RestartLevel()` clears it. Both call TrackManualRestart rather than TrackDeath. All three checkpoint respawn event lists retain an unassigned WallSlideOnDrop.ShowWall callback alongside the valid camera callback; review those entries during stage-recovery testing.
- Mobile `Canvas (1)` is Screen Space - Overlay, scaled against 1920 × 1080 with match 0.5; its buttons are screen-fixed and are not moved by the mobile scripts. AutoDetect is saved in Level02.
- Script comments, Inspector headers/tooltips, logs and default hint strings were standardized to English in `260b9b1`. Thirty scripts changed without renaming serialized fields/methods or changing control flow. The scan covered 96 script/source files; existing serialized scene strings were outside that cleanup.
- See [level-2.md](level-2.md) for the route, wiring, saved tuning, accepted Editor checks and remaining checkpoint/device/build tests. The current wrap-up is on Level-2; no new publication is recorded.
