# Script Guide

Last updated: 2026-10-12. Unity version: `6000.3.22f1`.

Current playable scenes, test scenes and gameplay scripts. Navigation is merged into `main` through PR #9 (`9fc0f95`). Level02 is integrated, and All-Level V1 is published on GitHub Pages as the October 12 interim baseline. For controls and gameplay behavior, see [GameplaySpec.md](GameplaySpec.md). For the current Level 1 handoff and remaining work, see [level-1.md](level-1.md).

## 1. Playable and test scenes

| Scene | Purpose | Relevant features |
| --- | --- | --- |
| [Mainmenu](../Assets/Scenes/Mainmenu.unity) | Startup menu | Tutorial/Level01/Level02 selection, playable star and one-way UI-aligned platforms |
| [Playground](../Assets/Scenes/Playground.unity) | Existing mechanics sandbox | Star, box, bubble, resizing, pressure plate and door |
| [Level01_Reference](../Assets/Scenes/Level01_Reference.unity) | Earlier level layout retained as a reference | Traversal and transformation layout; not the new Alpha Level 1 |
| [Tutorial](../Assets/Scenes/Tutorial.unity) | Current playable tutorial | Movement, jump, three SpikeHazard prefab instances, box possession and resizing, pressure plate, goal, feedback and return-to-menu button |
| [WaterPlayground](../Assets/Scenes/WaterPlayground.unity) | Water integration sandbox | Water detection and water behavior for star, box and bubble; saved instances of the three core prefabs |
| [Level01](../Assets/Scenes/Level01.unity) | Alpha Level 1 graybox in progress | Water routes, box size gates, held pressure plate, height switch, purple hazards that pop bubbles or kill boxes, two bouncing-ball sections, three-region camera, any-form completion, death feedback and return-to-menu button; local/online playtests passed, broader regression remains |
| [Level02](../Assets/Scenes/Level02.unity) | Integrated second level in All-Level V1 | Spider traversal, spike-wall chase, projectile/button and fan sections, staged camera, checkpoints and return-to-menu button |
| [EnemyScene](../Assets/Scenes/EnemyScene.unity) | Teammate test scene present in this checkout | Not reviewed or playtested as part of the Level 1 work |
| [FanScene](../Assets/Scenes/FanScene.unity) | Fan sandbox | Star, fan possession, W toggles airflow, Space leaves the fan (2s repossess cooldown), red blowable obstacles and a box body |

The enabled scenes in [EditorBuildSettings.asset](../ProjectSettings/EditorBuildSettings.asset) are `Mainmenu` (index 0), `Tutorial` (1), `Level01` (2) and `Level02` (3). `Level01_Reference`, `WaterPlayground` and `Playground` remain in the list but are unchecked. Full-scene restart requires the current scene to be included in the enabled build scene list, so temporarily enable a sandbox when testing its reload behavior. Check the active build profile when preparing a build.

## 2. Script map

### Shared character systems

| Script | Attach to / dependencies | Current responsibility | Change here when... |
| --- | --- | --- | --- |
| [PossessableBody.cs](../Assets/Scripts/PossessableBody.cs) | Each body root; `Rigidbody2D`, a root `Collider2D`, and an assigned separate control-visual child | Defines `BodyForm`, including `BouncyBall`; holds movement settings and ownership; exposes grounded/water state; forwards eligible collision contacts to the player controller; logs water transitions | Adding a form identifier or a capability common to bodies |
| [PlayerPossessionController.cs](../Assets/Scripts/PlayerPossessionController.cs) | One `PlayerControl` object; assign an active, configured initial star body | Reads keyboard input; controls the current body; dispatches movement by form; provides accelerated A/D ball steering; deactivates the original star while it inhabits a host; validates live contact before transfer; maintains multi-body separation locks; queues Space/hazard bubble pops; dispatches fan/spike-wall/spider controls; handles death, vertical bounds, scene reload and checkpoint session state; exposes the current body to the camera | Adding a form's movement/input integration, changing transfer rules, camera target access or restart behavior |
| [BodyWaterPhysics.cs](../Assets/Scripts/BodyWaterPhysics.cs) | Each water-enabled body root; `Rigidbody2D` and `PossessableBody` | Caches original gravity/damping; applies water gravity/damping to controlled and unpossessed bodies; restores original settings outside water or on disable | Adding a special water-physics rule or changing damping; swimming input belongs in the controller |
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

`BoxResizeAbility` currently requires an upright, unparented box with positive scale and a collider edge radius of zero. The intended box setup uses uniform X/Y scaling. It ignores triggers during clearance checks and does not change mass. Do not parent a box under a level-group object without first updating this constraint.

**Reusable core prefabs:** [StarPlayer](../Assets/Form/StarPlayer.prefab), [BoxBody](../Assets/Form/BoxBody.prefab), [BubbleBody](../Assets/Form/BubbleBody.prefab), and [BouncyBall](../Assets/Form/BouncyBall.prefab) are available in `Assets/Form`. Each includes its physics components, `PossessableBody`, `BodyWaterPhysics`, and its internally assigned control-visual child. BoxBody also includes `BoxResizeAbility`; BouncyBall includes `BouncyBallAbility` and a shared bouncy physics material.

- Drag the required prefabs into a scene and set their positions. Keep boxes at the scene root to satisfy the resizing constraint above.
- Keep one scene `PlayerControl` with `PlayerPossessionController`; assign the **scene instance** of StarPlayer to `Initial Body`. The controller is not included in these prefabs.
- A BouncyBall instance waits motionless until the controlled body touches it. Once possessed, it bounces automatically on land and uses A/D steering. Do not duplicate or reimplement this behavior in a scene-specific script.
- Level01 uses two instances of the same BouncyBall prefab. `BouncyBall_Lower` is near the original R1 location; `BouncyBall_Upper` is beside D on the upper-right platform.
- Water requires scene objects with `WaterZone`. Canopies, platforms, switches, gates and goal UI remain scene objects. Place hazard prefab instances separately and assign their scene controller references as described below.
- When replacing an existing body, check references to that body, especially `Initial Body` and assigned-box switches. Internal `Control Visual` references are already included in the prefab.
- The star sprite is generated in Play Mode by `StarVisual`; an absent edit-time star preview does not by itself indicate a broken prefab. Change a scene instance's placement locally; apply changes to the prefab asset only when they should become shared defaults.

### Interactions, hazards and level feedback

| Script | Attach to / dependencies | Current responsibility | Change here when... |
| --- | --- | --- | --- |
| [HeavyPressurePlate.cs](../Assets/Scripts/HeavyPressurePlate.cs) | Plate; `BoxCollider2D`, `SpriteRenderer`, assigned separate door object | Checks a dynamic body's mass and supporting contact in the plate area; changes color and disables the door; activation stays latched | Changing weight requirements or plate/door behavior |
| [HeldPressurePlate.cs](../Assets/Scripts/HeldPressurePlate.cs) | Plate; `BoxCollider2D`, `SpriteRenderer`, assigned target box and separate door | Opens the door only while the assigned grounded box supports its feet in the plate area; leaving closes it; Level01 P targets C and controls G | Changing continuous plate occupancy; this plate has no mass threshold and does not latch |
| [BoxHeightSwitch.cs](../Assets/Scripts/BoxHeightSwitch.cs) | Overhead switch; `BoxCollider2D`, `SpriteRenderer`, assigned target box and separate door | Latches the door open when the assigned grounded box touches the switch; Level01 H targets D and controls G2 | Changing height-switch activation; required growth comes from placement, not an explicit size threshold |
| [BubblePopHazard.cs](../Assets/Scripts/BubblePopHazard.cs) | Hazard; `CircleCollider2D`, assigned scene player controller | On trigger enter/stay, requests a pop for a controlled bubble or kills a controlled box; ignores star, bouncing ball and unpossessed bodies | Changing form-specific purple hazard responses; shared pop/death behavior stays in the player controller |
| [SpikeHazard.cs](../Assets/Scripts/SpikeHazard.cs) | Environmental spike; `PolygonCollider2D`, assigned scene controller | Uses a trigger to kill the currently controlled body; unpossessed bodies do not trigger player death | Changing lethal hazard contact behavior |
| [TutorialIntroUI.cs](../Assets/Scripts/TutorialIntroUI.cs) | Tutorial canvas; assigned box, controller, and two TMP text references | Displays the initial controls, first box-possession instructions, death message and completion message | Changing tutorial copy or its UI states |
| [LevelDeathUI.cs](../Assets/Scripts/LevelDeathUI.cs) | Level01 `LevelCanvas`; assign scene `PlayerControl` and `LevelStatusText` | Clears status text on start; observes `IsDead` in Update and displays death/restart instructions once; does not control pause or reload | Changing level death copy or UI wiring |
| [TutorialGoal.cs](../Assets/Scripts/TutorialGoal.cs) | Tutorial goal; `BoxCollider2D`, assigned tutorial box, door and UI | Completes only when the assigned controlled box enters the goal with the door inactive and the player alive; synchronizes its visible physics pose and pauses the game | Changing tutorial-specific completion conditions or pause behavior |
| [LevelGoal.cs](../Assets/Scripts/LevelGoal.cs) | Level goal; `BoxCollider2D`, assigned player controller, required gate and TMP status text | Completes when any living controlled form enters with the required gate inactive; synchronizes the visible physics pose, pauses gameplay and shows completion/restart text; Level01 requires G2 | Changing non-tutorial goal conditions or completion feedback; scene transitions are not implemented here |

**Reusable hazard prefabs:** core forms and hazards share `Assets/Form` by team convention. Being stored in this folder does not make a hazard possessable.

| Prefab | Included components | Scene setup / behavior |
| --- | --- | --- |
| [SpikeHazard](../Assets/Form/SpikeHazard.prefab) | Sprite renderer, trigger `PolygonCollider2D`, `SpikeHazard` | Assign the scene's `PlayerControl` to `Player Controller`; contact kills the currently controlled form. TutorialSpike01–03 now use this prefab. |
| [WaterPopHazard](../Assets/Form/WaterPopHazard.prefab) | Purple sprite, black `Cross_A` / `Cross_B` visual children, trigger `CircleCollider2D`, `BubblePopHazard` | Assign the scene's `PlayerControl` to `Player Controller`; contact pops a controlled bubble or kills a controlled box. The asset name differs from its script name. |

- Both prefab assets intentionally store `Player Controller` as None. Set it on **each scene instance before Play Mode**; do not try to store a reference to a particular scene inside the shared asset.
- If that reference is missing at initialization, the script logs an error and disables itself. Exit Play Mode, assign the reference, then test again.
- `WaterPopHazard` has no water-only check: placing it underwater is a level-design choice. It ignores the star, bouncing ball and unpossessed bodies. Box growth still ignores triggers for clearance, so expanding into this hazard causes death rather than stopping growth.
- The black X uses two centered Square sprite children with Z rotations of +45 and -45 degrees. Both render at order 3 over the purple circle at order 2. They have no colliders and do not change the hazard hit area.
- Developer-reported tests passed for spike death/restart, controlled box contact and growth into purple hazards, preserved bubble popping, and black-X readability in Level01.

**Level01 feedback setup:** keep `LevelCanvas` and `LevelStatusText` active. Both `LevelGoal.statusText` and `LevelDeathUI.statusText` reference the same TMP object. Keep its saved text empty. Its top-center anchor/pivot, position `(0, -40)` and size `(1000, 200)` provide space for the three-line death message. The developer confirmed death and completion text display after the layout adjustment. Full HD preview helped verify the small hazard markers; fit the Game view to the available panel when checking text near the screen edge.

### Level02 integration points

| Script | Responsibility |
| --- | --- |
| [level2Camera.cs](../Assets/Scripts/StartingStage/level2Camera.cs) | Coordinates follow, fixed framing, spider reveal, chase, falling and later follow stages, with references to other camera behaviors |
| [level2Checkpoint.cs](../Assets/Scripts/StartingStage/level2Checkpoint.cs) | Saves a checkpoint ID and respawn position for the controlled body and invokes stage events on arrival and checkpoint respawn |
| [HexagonSurfaceMovement.cs](../Assets/Scripts/spiderFolder/HexagonSurfaceMovement.cs) | Spider traversal along connected surfaces, driven by the shared controller |
| [SpikeShooterAbility.cs](../Assets/Scripts/SpikeWall/SpikeShooterAbility.cs) | Possessable spike-wall facing and projectile firing |
| [SpikeButton.cs](../Assets/Scripts/SpikeWall/SpikeButton.cs) | Projectile/button interaction in the spike-wall section |

`PlayerPossessionController` keeps checkpoint identity, scene and position in session state. Automatic death reload preserves that state, while R clears it. `MainMenuUI.LoadLevel` calls `ResetCheckpointSession()` before loading any selected level (`773912d`), preventing an old checkpoint from turning a menu selection into an unintended resume. Do not assume scene reload alone means a fresh run. Level01 continues to use its own regional camera and G2/flag completion rules.

## 3. Navigation and interactive menu

| Script | Attach to / dependencies | Current responsibility |
| --- | --- | --- |
| [MainMenuUI.cs](../Assets/Scripts/MainMenuUI.cs) | `Mainmenu` scene's `MainMenuCanvas` | `OpenTutorial()` loads `Tutorial`; `OpenLevel1()` loads `Level01`; `OpenLevel2()` loads `Level02`; clears checkpoints and restores time scale to 1 before loading |
| [LevelNavigationUI.cs](../Assets/Scripts/LevelNavigationUI.cs) | Return UI in Tutorial, Level01 and Level02 | `ReturnToMainMenu()` restores time scale to 1 and loads `Mainmenu` |
| [MenuUIPlatform.cs](../Assets/Scripts/MenuUIPlatform.cs) | Each menu platform root; requires `BoxCollider2D` and `PlatformEffector2D`; assign target UI rectangle and Main Camera | Converts the target's upper corners to world coordinates, aligns a thin collider with its top edge, and configures one-way collision |

### Button wiring and scene names

- `TutorialButton.OnClick` calls the local `MainMenuCanvas` component's `MainMenuUI.OpenTutorial`.
- `Level1Button.OnClick` calls `MainMenuUI.OpenLevel1` on the same scene object.
- `Level2Button` displays `Level 2` and calls `MainMenuUI.OpenLevel2`. `Level02` is enabled at build index 3.
- `ReturnToMenuButton` in Tutorial, Level01 and Level02 calls `LevelNavigationUI.ReturnToMainMenu` on that scene's own assigned UI object. Preserve the local reference when copying UI between scenes.
- Keep the scene string `Mainmenu` consistent with the asset name. No goal script automatically advances to another scene. Return buttons remain available during gameplay and completion, and loading a level starts a fresh scene.

### Menu star and platform setup

- Keep the scene StarPlayer and `PlayerControl` at the scene root. Assign the scene StarPlayer to `PlayerPossessionController.initialBody`. Reuse existing star movement and jump behavior rather than adding a second movement controller.
- `TitlePlatform`, `TutorialPlatform`, `Level1Platform` and `Level2Platform` each use `MenuUIPlatform`. Their `targetUI` references are `TitleText`, `TutorialButton`, `Level1Button` and `Level2Button`, respectively. All reference Main Camera and use thickness `0.12`.
- Keep platform roots at identity rotation and unit scale. The script positions them during Start and LateUpdate, after forcing the initial Canvas layout update. Edit-mode positions do not represent their aligned runtime positions.
- The collider follows the top of the UI rectangle, not the visible text glyphs. It is a non-trigger collider with Used By Effector enabled. The script sets Use One Way and Use One Way Grouping, a surface arc of 180 degrees, and disables side friction/bounce.
- The star may pass upward through buttons and land on their tops while falling. `MenuTestGround` is a separate solid ground platform below the button stack. Contact with these platforms does not select a level.
- The star initially lands above the title. The accepted layout does not require it to jump back from Tutorial to the title. Lower buttons get progressively wider.
- Keep the menu EventSystem's `InputSystemUIInputModule` for mouse input, with **Send Navigation Events disabled**. This prevents movement/jump keys from navigating or submitting UI buttons.

### Rendering and coordinate conversion

- `MainMenuCanvas` uses **Screen Space - Camera**, assigned Main Camera, plane distance **10**, Default sorting layer and order **0**. Canvas Scaler uses Scale With Screen Size, reference **1920 x 1080** and width/height match **0.5**.
- The menu camera is orthographic and faces the XY plane from Z = -10. `MenuUIPlatform` projects onto world Z = 0 and passes `worldCamera` to `RectTransformUtility.WorldToScreenPoint`. It is configured for this camera-backed Canvas, not an Overlay Canvas with a null camera.
- StarPlayer's menu visual uses an unlit sprite material. `StarVisual` sets its sorting order to **10**, above the Canvas on the same Default sorting layer. This allows the star to remain visible while crossing button graphics. Increasing sprite order alone would not put it above a Screen Space - Overlay Canvas.
- Preserve the Canvas camera and the platform conversion together when changing rendering modes. Verify collision alignment and mouse clicks after changing layout or aspect ratio. GraphicRaycaster currently has Blocking Objects set to None.

### Verification and follow-up

Developer-reported tests passed for loading Tutorial and Level01, returning to the menu, moving/jumping across menu platforms, and keeping the star visible in front of buttons while clicks still work. The October 11 rebuilt localhost version and published GitHub Pages version subsequently passed developer playtests. No new movement hint or attention animation is included. Level 2 integration is complete and included in All-Level V1. The October 12 editor retest and localhost build test passed according to the developer. Teammate usability feedback and broader resolution/browser coverage remain follow-up work.

## 4. Web build and GitHub Pages release

### Current published version

- Play URL: [StarFormer](https://xtraluck21.github.io/StarFormer/).
- Release: **All-Level V1**, October 12, 2026 (America/Los_Angeles). Retain as the interim baseline while feedback is collected.
- Source baseline: `main` at `773912d73866b401fb15fa1551b1c434b7f829db` (reset checkpoints on menu entry), plus the local editor/build changes described below. The build is not represented as a pristine checkout of that commit.
- Publishing branch: `gh-pages`, root directory. Release commit: [`6f1737a`](https://github.com/XtraLuck21/StarFormer/commit/6f1737af361b2bf30d06d150ece4ba4551126470). The developer committed and pushed it, and the Pages build API reports `built` for that exact commit.
- Included scenes: Mainmenu, Tutorial, Level01 and Level02.
- Tested output: `Builds/WebGL-All-Level-V1`. Editor retesting and localhost playtesting passed according to the developer. Deployment success is confirmed, but a separate detailed online gameplay test report for this release has not been recorded here.
- Previous release: October 11's `Builds/WebGL-Test2`, published as `73877e9`, included Mainmenu, Tutorial and Level01 and passed developer-reported localhost and online testing. Keep it as historical release context, not the current deployment.

### Build configuration

Use Unity `6000.3.22f1` with Web Build Support. Select Web in Build Profiles and confirm Mainmenu (0), Tutorial (1), Level01 (2) and Level02 (3) are the enabled scenes. Use the Default Web template, Gzip compression, Decompression Fallback enabled, and a normal build with Development Build unchecked. Thread support remains disabled.

Use Build And Run to test the generated site through localhost. Do not test by directly opening `index.html` as a local file. Keep outputs under the ignored `Builds` directory and outside Assets. Preserve `index.html`, `Build`, `TemplateData` and any other generated runtime files as one set.

### Publishing and updating

1. Build the intended source revision and test it locally before publishing. Record any local configuration changes required to reproduce it.
2. Use a separate publishing checkout, currently the sibling folder `StarFormer-Pages`, on `gh-pages`. Copy the contents of the tested output into its root, so `index.html` is directly at the publishing root. Keep `.nojekyll` present. Do not overwrite the checkout's `.git` directory or copy Unity source/cache folders into it.
3. Commit and push the build files to `gh-pages`. For later releases, reuse that checkout and replace the previous generated files as a complete set. Check the staged changes before committing and avoid leaving obsolete build payloads behind.
4. GitHub Settings → Pages uses Deploy from a branch, `gh-pages`, `/ (root)`. Wait for the Pages deployment to succeed, then open the published URL.
5. Test menu star movement and jumping, Tutorial, Level 1 and Level 2 selection, return navigation, restart, checkpoint recovery and completion/stage feedback. Keep online checks separate from local checks, and ask teammates to test on their own computers.

`main` contains Unity source, maintained configuration and documentation. `gh-pages` contains the exported website. Do not merge `gh-pages` into `main`. Updating source alone does not update the hosted game: publish a new tested build for each release. GitHub hosts the site without requiring the local editor or localhost server to remain running.

### Missing navigation scripts during the first build

The initial build log reported missing or invalid scripts on MainMenuCanvas, the four menu platforms, TutorialCanvas and LevelCanvas. MainMenuUI, MenuUIPlatform and LevelNavigationUI were absent from the editor/player compilation inputs even though their source files and scene references existed. Comparison with the navigation branch showed no missing source merge. Editor Play Mode also reproduced the fault.

Closing and reopening Unity, reimporting the three scripts, and waiting for compilation restored editor behavior. A new Build And Run output then passed localhost testing and was used for publication. The exact cause of the import/compilation inconsistency is unresolved. If this recurs, check the Console and component references, refresh imports and verify editor play before rebuilding. Do not remove and recreate serialized components as the first recovery step. Edit-mode platform placement and effector warnings alone are not proof of failure, because MenuUIPlatform configures its collider in Awake and aligns it in Start/LateUpdate.

### Input System error during the October 12 editor test

The editor log recorded `Map index out of range`, control/binding index errors and `NullReferenceException` inside `InputActionState` while processing keyboard input. Domain Reload was disabled. The diagnostic procedure was to stop Play Mode, enable Reload Domain and Scene, restart Unity and retest. The developer reported that retesting passed and then built All-Level V1, which passed localhost testing. The local EditorSettings diff confirms `m_EnterPlayModeOptions` changed from 1 to 0. This is a successful recovery, not proof of the underlying cause or a gameplay-code fix.

### Configuration state at the All-Level V1 documentation update

The source checkout has five existing non-document changes, left untouched by this documentation update:

| File | Observed change / handling |
| --- | --- |
| `ProjectSettings/EditorSettings.asset` | Enables Domain Reload as well as Scene Reload for Play Mode. Review as a separate maintained editor-setting change |
| `ProjectSettings/ProjectSettings.asset` | Build-added input-action entry in `preloadedAssets`. Review separately from maintained Web settings; the Input System build processor temporarily adds this entry |
| `Assets/Settings/UniversalRP.asset` | Shader prefilter changes generated during build. Review separately from source configuration |
| `Assets/Settings/UniversalRenderPipelineGlobalSettings.asset` | Generated runtime settings list populated during build. Review separately from source configuration |
| `Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF - Fallback.asset` | Git reports a modification with no textual line diff. Do not include it in the documentation commit without a separate review |

Gzip and decompression fallback are already present in the source baseline. The previous default Bloom filter completion is no longer an outstanding local change. Stage the four documentation files explicitly so unrelated configuration or generated asset changes are not included. These local source changes do not alter the already published build unless a new build is exported and deployed.

### Why the publishing checkout is separate

`StarFormer` on `main` is the editable Unity project. `StarFormer-Pages` on `gh-pages` is the exported static website (`index.html`, Build payloads and TemplateData). GitHub Pages serves these generated files and does not compile the Unity project. Keeping a separate folder avoids switching the open Unity project to a branch containing only website files. Keeping a separate branch separates source history from generated release files and allows a tested website version to remain published while source development continues.

This separation is a workflow choice, not a GitHub Pages requirement. Other workflows can deploy an artifact with GitHub Actions or publish from a configured docs directory. Continue using the existing separate publishing checkout for this project, and do not merge `gh-pages` into `main`.
