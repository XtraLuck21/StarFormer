# Script and Scene Guide

Last updated: 2026-11-05. Published Beta 0.1.1 uses source `9451511` (gh-pages `e81097e`) with seven-scene analytics, refreshed maps and the Level01 movable-door hint. Current main additionally includes the unpublished November 4 help-book UI. Level01 is the former Advanced scene; this guide replaces the old G/G2 and ball-checkpoint wiring descriptions.

## 1. Scenes and build configuration

| Enabled index | Scene | Role / continuation |
| --- | --- | --- |
| 0 | [Mainmenu](../Assets/Scenes/Mainmenu.unity) | Startup and direct entries |
| 1 | [IntroBox](../Assets/Scenes/IntroBox.unity) | Box flag opens an exit to IntroBubble |
| 2 | [IntroBubble](../Assets/Scenes/IntroBubble.unity) | Grounded-Star flag opens passage to IntroBouncyBall; return to IntroBox |
| 3 | [IntroBouncyBall](../Assets/Scenes/IntroBouncyBall.unity) | Bounce/float/compressed-bounce tutorial; return to Bubble, flag to Level01 |
| 4 | [Level01](../Assets/Scenes/Level01.unity) | Advanced map promoted to formal first level; flag loads IntroSpider |
| 5 | [IntroSpider](../Assets/Scenes/IntroSpider.unity) | Continues to FanTutorial |
| 6 | [FanTutorial](../Assets/Scenes/FanTutorial.unity) | Continues to Level02 |
| 7 | [Level02](../Assets/Scenes/Level02.unity) | Final completion screen with Main Menu action |

Transitions use scene names rather than adjacent build indices. The actual forward flow is Mainmenu → IntroBox → IntroBubble → IntroBouncyBall → Level01 → IntroSpider → FanTutorial → Level02.

[EditorBuildSettings.asset](../ProjectSettings/EditorBuildSettings.asset) owns the enabled list. [WebGLBuild.cs](../Assets/Editor/WebGLBuild.cs) exports that list. Keep Level01_Legacy, Level01_Reference, Tutorial, WaterPlayground and Playground disabled. EnemyScene is a separate test scene, not part of this progression.

The former Advanced scene and GUID now belong to Level01. The previous Level01 scene and meta are preserved as [Level01_Legacy](../Assets/Scenes/Level01_Legacy.unity); Level01_Reference is a different historical reference. There is no second active Level01_Advanced scene. Classes named `Advanced*` are intentionally retained to avoid unrelated refactoring.

## 2. Gameplay scripts

### Shared forms and physics

| Script | Responsibility / setup |
| --- | --- |
| [PlayerPossessionController.cs](../Assets/Scripts/PlayerPossessionController.cs) | One local controller per scene. Assign the configured Star scene instance as Initial Body; Starting Host is optional. Owns contact transfers, input, original-Star restoration, death/reload, checkpoints and analytics. AdvancedCheckpointController takes priority for current Level01 recovery. |
| [PossessableBody.cs](../Assets/Scripts/PossessableBody.cs) | Form identity, readiness, Rigidbody2D/collider access, controlled state, grounding and host lifecycle. Keep serialized BodyForm values stable. |
| [BoxResizeAbility.cs](../Assets/Scripts/BoxResizeAbility.cs) | J/K size changes with bottom anchoring while supported and center anchoring otherwise; checks all growth obstacles before moving permitted lifts. Requires an upright unparented positive-scale Box with zero collider edge radius. |
| [BouncyBallAbility.cs](../Assets/Scripts/BouncyBallAbility.cs) | Dormant ball activation, automatic bounce, overhead-clearance compression and water float. Read scene overrides before changing shared prefab defaults. |
| [WaterZone.cs](../Assets/Scripts/WaterZone.cs) / [BodyWaterPhysics.cs](../Assets/Scripts/BodyWaterPhysics.cs) | Water membership, cached air physics, damping and form-specific water behavior; WaterZone requires a trigger collider. |
| [RatchetingCeiling.cs](../Assets/Scripts/RatchetingCeiling.cs) | Box lift/ball lowering with clearance protection. Lower Level01 door allows ball lowering. Upper lift uses `boxOnlyLift = true`, `ballPushesDown = false`, retaining its height after Box shrinks. |
| [HexagonSurfaceMovement.cs](../Assets/Scripts/spiderFolder/HexagonSurfaceMovement.cs) | Connected-surface Spider movement; shared controller uses Q release. |
| [FanBlower.cs](../Assets/Scripts/FanSet/FanBlower.cs) | F toggles airflow; Q release is handled by the controller. Current targets are eligible non-possessable obstacles. |
| [SpikeShooterAbility.cs](../Assets/Scripts/SpikeWall/SpikeShooterAbility.cs) / [SpikeProjectile.cs](../Assets/Scripts/SpikeWall/SpikeProjectile.cs) | Possessable spike-wall facing/fire and projectile behavior; Space remains Fire. |
| [StarVisual.cs](../Assets/Scripts/StarVisual.cs), [CircleVisual.cs](../Assets/Scripts/CircleVisual.cs), [FormGlowHalo.cs](../Assets/Scripts/FanSet/FormGlowHalo.cs) | Star drawing, optional inward circle outlines and host glow. Sprite styling does not enlarge collision geometry. |
| [MobileControlUI.cs](../Assets/Scripts/Mobile/MobileControlUI.cs) / [TouchHoldButton.cs](../Assets/Scripts/Mobile/TouchHoldButton.cs) | Scene-configured form-specific touch controls; release held input when hidden or focus is lost. AutoDetect for releases; AlwaysShow only for testing. |

Reusable prefabs live in Assets/Form: StarPlayer, BoxBody, BubbleBody and BouncyBall. They include internal physics/visual references, not a scene controller. Keep Boxes at scene root for resizing; assign scene instances to switches/goals/hints. BouncyBall's serialized form is 7, Spider's is 4. Do not rerun BouncyBallSetup just to repair one enum value: it also recreates assets/placements.

The controller retains the merged Box movement/growth rules, contact separation before reacquisition, and temporary departed-Box/new-Bubble collision exclusion. Do not replace these with global collision-layer changes. Preserve Star's enabled PolygonCollider and the scene/prefab's disabled legacy Capsule where configured.

### Current Level01

| Script | Responsibility / important references |
| --- | --- |
| [AdvancedRouteSwitch.cs](../Assets/Scripts/AdvancedRouteSwitch.cs) | SAVE/NORMAL/RUIN state, button labels/rearming, initial Box-or-Star warning, hazard animation and camera ownership. Assign both button sensors/faces/labels, Box D, R2, guard, camera and separate NORMAL/SAFE/HARD roots. No collider/name substitution from the old map. |
| [SafeRouteObstacles.cs](../Assets/Scripts/SafeRouteObstacles.cs) | Three black fixed-size boards: one static, two vertically translating in opposite phase. Shared travel clock; leading-edge protection pauses the pair. Reveals after spike retraction and demonstrates a full alternating cycle. |
| [HardRouteFallingSpike.cs](../Assets/Scripts/HardRouteFallingSpike.cs) | Assigned ceiling tooth falls only in HARD when the current body enters its upper-route horizontal band. Sweeps the actual collider, stays landed and lethal, resets for route changes/retries. Assign route/controller/tooth and upper floor height. |
| [RuinButtonGuard.cs](../Assets/Scripts/RuinButtonGuard.cs) | First upward hit by assigned R2 breaks the cover, spawns short-lived fragments and forces downward velocity. The button arms only after a later landing. RestoreBrokenState supports checkpoints. |
| [AdvancedLevelCamera2D.cs](../Assets/Scripts/AdvancedLevelCamera2D.cs) | Current Level01 regional framing and local position/zoom limits. Route presentations temporarily override it. RegionalCameraFollow2D is the old map's camera, not this scene's active system. |
| [AdvancedCheckpoint.cs](../Assets/Scripts/AdvancedCheckpoint.cs) | World trigger, index, fixed respawn host/position, blue/yellow hollow-star marker and saved label. CP3 also requires grounded Star/Box at the upper floor. |
| [AdvancedCheckpointController.cs](../Assets/Scripts/AdvancedCheckpointController.cs) | On PlayerControl; ordered CP1/2/3, scene-path-scoped session, live completed route/lift/cover/cinematic progress and restoration. Calls SetCheckpoint with ADV_CP1/2/3 and suppresses duplicate generic marker feedback. |
| [StarStartGreeting.cs](../Assets/Scripts/StarStartGreeting.cs) | Optional READY? → LET'S GO! sign, Star-only upward contact, reusable bounce/sparkles on separate contacts. No route dependency. |
| [MovableDoorContactHint.cs](../Assets/Scripts/MovableDoorContactHint.cs) | Published in Beta 0.1.1: lower-door idle timer, deliberate contact activation, paused lift preview and once-per-scene-load hint. |
| [BubbleCeilingRestartHint.cs](../Assets/Scripts/BubbleCeilingRestartHint.cs) | First tank's real ceiling contact inside its teaching area; 1.5-second R reminder. Leaving the qualifying state resets the lesson. |
| [BubbleCeilingPopHint.cs](../Assets/Scripts/BubbleCeilingPopHint.cs) | Two instances: water exit and upper-right ceiling. Assigned ceiling contact + area for 3 seconds; Q prompt once per instance per scene load. Pause does not advance the timer. |

Formal Level01 sets `enableBouncyBallCheckpoints = false`. CP1 restores Box B, CP2 restores R1, CP3 restores Star. Restore completed progress without replaying cinematics; reset loose bodies/lower door, SAFE phase and the falling trap. Full restart clears everything. See [level-1.md](level-1.md) and [CheckpointGuidance.md](CheckpointGuidance.md).

### Introductory scenes and retained legacy helpers

| Script / asset | Current responsibility |
| --- | --- |
| [BoxGrowHint.cs](../Assets/Scripts/BoxGrowHint.cs) | Assigned Box/area, dashed size guide and J/K hints. Visual root is separate from the moving host. |
| [BubbleMovementHint.cs](../Assets/Scripts/BubbleMovementHint.cs) | Intro Air/Water/Q-exit, Star movement/jump, Bubble introduction and ContinueRight modes. Q-exit follows any current Bubble in its safe landing area; ordinary assigned-body hints keep their binding. |
| [IntroFormHintUI.cs](../Assets/Scripts/IntroFormHintUI.cs) | IntroBox form title/subtitle and Box-to-Box feedback. Disabled with old title objects in rebuilt IntroBubble, which uses world hints instead. Do not enable TutorialIntroUI alongside it. |
| [IntroFlagGate.cs](../Assets/Scripts/IntroFlagGate.cs) | IntroBox's required BoxB opens its door; IntroBubble requires its grounded Star. Open() supports return travel. |
| [IntroSceneExit.cs](../Assets/Scripts/IntroSceneExit.cs) | Two-way IntroBox ↔ IntroBubble ↔ IntroBouncyBall passages. Return to Box uses BoxB; return to Bubble uses Star near its flag, with the destination gate open. One-shot destination setup is not a full previous-scene snapshot. |
| [IntroStarGoal.cs](../Assets/Scripts/IntroStarGoal.cs) | Shared intro transition used by IntroSpider/FanTutorial. IntroBubble now uses flag + passage instead. |
| [IntroBouncyBallGoal.cs](../Assets/Scripts/IntroBouncyBallGoal.cs) | Living controlled Star/BouncyBall flag completion, 1.5-second Level 1 transition to Level01. |
| [IntroBouncyBallHints.cs](../Assets/Scripts/IntroBouncyBallHints.cs) | Short anchored headlines for bounce/float/land/low ceiling and spike death; subtitle disabled. |
| [BouncyBallMovementHint.cs](../Assets/Scripts/BouncyBallMovementHint.cs) | Map movement-key prompt for the controlled tutorial ball. |
| [IntroSpiderCamera.cs](../Assets/Scripts/IntroSpiderCamera.cs), [IntroSpiderEntranceSeal.cs](../Assets/Scripts/IntroSpiderEntranceSeal.cs), [CameraFocusIntro.cs](../Assets/Scripts/FanSet/CameraFocusIntro.cs) | Existing Spider/Fan introduction camera and entrance behavior; retain local scene wiring. |
| [GoalFlagVisual.prefab](../Assets/Form/GoalFlagVisual.prefab) | Visual pole/flag only; gameplay goals have separate triggers and scene references. |
| [HeldPressurePlate.cs](../Assets/Scripts/HeldPressurePlate.cs), [BoxHeightSwitch.cs](../Assets/Scripts/BoxHeightSwitch.cs) | Retained plate/height-switch interactions for legacy layouts. G/G2 are not formal Level01 prerequisites. |
| [BubbleRespawnOnPop.cs](../Assets/Scripts/BubbleRespawnOnPop.cs), [ReturnToSpawnOnLeave.cs](../Assets/Scripts/FanSet/ReturnToSpawnOnLeave.cs) | Explicit respawn/return opt-ins, not global Bubble rules. Old U2 regeneration belongs to Level01_Legacy. |
| [BouncyBallCheckpointSession.cs](../Assets/Scripts/BouncyBallCheckpointSession.cs) | Retained last-ball recovery for opted-in scenes; disabled for current Level01. |
| [CheckpointFeedbackUI.cs](../Assets/Scripts/CheckpointFeedbackUI.cs) | Generic checkpoint feedback for other scenes; legacy E-pop lesson is suppressed when AdvancedCheckpointController is present. Formal Level01 uses its own marker/text and Q hints. |
| [SpikeHazard.cs](../Assets/Scripts/SpikeHazard.cs), [BubblePopHazard.cs](../Assets/Scripts/BubblePopHazard.cs) | Controlled-body lethal spikes; Bubble-only pop / Box death hazard respectively. Keep scene controller references. |

See [intro-levels.md](intro-levels.md) for hint placement, return travel, spare Bubble and original mobile layouts. These scene-specific references must not point into another scene.

### Level02 integration points

| Script | Responsibility |
| --- | --- |
| [level2Camera.cs](../Assets/Scripts/StartingStage/level2Camera.cs) | Follow/fixed framing, Spider reveal, chase, falling and later follow stages |
| [level2Checkpoint.cs](../Assets/Scripts/StartingStage/level2Checkpoint.cs) | CP01/CP02/CP03 positions and stage events on checkpoint arrival/respawn |
| [HexagonSurfaceMovement.cs](../Assets/Scripts/spiderFolder/HexagonSurfaceMovement.cs), [SpiderGooglyEyes.cs](../Assets/Scripts/spiderFolder/SpiderGooglyEyes.cs) | Spider traversal and eyes; preserve the merged Level02 behavior |
| [SpikeButton.cs](../Assets/Scripts/SpikeWall/SpikeButton.cs) | Existing projectile/button interactions |
| [BubbleSpawner.cs](../Assets/Scripts/transitStage/BubbleSpawner.cs) | Replacement Bubble in the transition section |
| [HelpPanelUI.cs](../Assets/UI/HelpPanelUI.cs) | Main-only: now declares `HelpBookUI` for the paged Mainmenu help book; published 0.1.1 uses the earlier `HelpPanelUI`. Other scene configurations still need migration/regression checks. |
| [Spritesequenceplayer.cs](../Assets/UI/Spritesequenceplayer.cs) | Main-only: `SpriteSequencePlayer` plays the six-form help demonstrations using unscaled time; not in published 0.1.1. |

Level02 geometry and its positional checkpoint system were not replaced by Advanced. Its separate navigation buttons and final flag remain. Existing callback and device follow-ups are recorded in [level-2.md](level-2.md).

## 3. Navigation and interactive menu

| Script | Behavior and wiring |
| --- | --- |
| [MainMenuUI.cs](../Assets/Scripts/MainMenuUI.cs) | OpenIntroLevels → IntroBox; OpenTutorial aliases it; OpenLevel1 → Level01; OpenLevel2Intro → IntroSpider; OpenLevel2 → Level02. Clears checkpoint sessions and restores time before loading. |
| [MainMenuButtonUI.cs](../Assets/Scripts/MainMenuButtonUI.cs) / [HomeButtonGraphic.cs](../Assets/Scripts/HomeButtonGraphic.cs) | Level01 runtime Home square, house/Star graphic, Home · Restart tooltip, Paused menu, conditional checkpoint action, and restart/menu confirmations. Owns/restores pause and blocks underlying input. |
| [RestartMenuUI.cs](../Assets/Scripts/RestartMenuUI.cs) | R/touch restart choices; conditional Return to Checkpoint, Restart Level and Cancel. Escape/R cancels. Delegates recovery to the local player controller. |
| [LevelNavigationUI.cs](../Assets/Scripts/LevelNavigationUI.cs) | Existing standalone Menu/Restart/Retry callbacks in other scenes. Full restart clears progress; Retry preserves it. Uses current-scene identity; do not hardcode the old Advanced name. |
| [LevelGoal.cs](../Assets/Scripts/LevelGoal.cs) | Optional required gate: an assigned active gate blocks, an absent gate does not. Validates destination and accepts a living controlled form. Current Level01 uses IntroSpider / Level 2 Intro / 1.5 seconds and no gate. |
| [LevelTransitionUI.cs](../Assets/Scripts/LevelTransitionUI.cs) | Generated overlay; disables player, pauses time, blocks pointer input, clears selection and loads after real-time delay. Clears sessions and restores time. Final-result mode waits for the menu button. |
| [LevelGoaltoMenu.cs](../Assets/Scripts/LevelGoaltoMenu.cs) | Class LevelGoalToMenu: Level02 completion event and final-result overlay with Return to Main Menu. |
| [MenuUIPlatform.cs](../Assets/Scripts/MenuUIPlatform.cs) | Aligns one-way colliders to UI rectangle tops using the menu camera. Contact does not submit buttons. |

Mainmenu Level1Button and IntroBouncyBall's goal both load formal Level01; IntroBubble's opened passage leads to IntroBouncyBall. Do not point either at Level01_Legacy. Direct Level2IntroButton and Level2Button remain separate entries. Preserve local callback targets when copying UI.

The playable menu uses Screen Space - Camera, the assigned orthographic camera, reference resolution 1920 × 1080 and UI-aligned one-way platforms. The Star renders above that Canvas. Keep EventSystem navigation events disabled so gameplay movement/jump does not submit buttons. Preserve the camera/coordinate conversion together when changing menu layout.

Level01 uses Home instead of the old LevelCanvas standalone Restart arrangement. IntroBox keeps its separate Restart button; IntroBubble hides that button and keeps compact Main Menu plus R. Level02 retains Menu/Restart/Retry. A reload alone does not mean a fresh session: death and Retry preserve progress; Restart Level and menu/new-level entry clear it.

## 4. Web build and GitHub Pages release

Beta 0.1.1 was published on November 3, 2026 at [the Beta URL](https://xtraluck21.github.io/StarFormer/beta/), from source `9451511` as `gh-pages` commit `e81097e`. On November 5, all seven live Beta files returned HTTP 200 and matched `Builds/WebGL-Beta-0.1.1` and the publishing checkout byte for byte. This confirms the deployed package, not a new gameplay playtest or production analytics acceptance. Player/Analytics versions are `beta-0.1.1`; the released source tracks all seven gameplay scenes (IntroBox, IntroBubble, IntroBouncyBall, Level01, IntroSpider, FanTutorial and Level02), excluding Mainmenu. Current first-level checkpoint IDs remain ADV_CP1/2/3.

The November 4 paged help book, six-form demo animations, Unity Recorder package and Mainmenu layout/behavior changes (`3a03844`, `f05d433`, `55f6b6b`, `f2c5c48`) are integrated into `main` but are **not included in published Beta 0.1.1**. The release-preparation patch was rebased to `2caffd9` on November 5, but the November 3 export was built from `9451511`. Shared version labels do not make current main identical to the published game.

Historical Beta 0.1.0 used source `3a693ca`, publishing commit `1105970`, and Level01/Level02-only analytics. Developer-reported local/online tests and the November 2 Level01 dashboard screenshot apply to that older export. Dashboard Version 9 subsequently resolved its map matching. The root URL still serves 0.1.0 via `f6eaf9e`; `/beta/` now serves 0.1.1. The earlier Web compiler fix is `3365e29`.

Historical Alpha 0.1.2 (October 18) used gameplay/UI `9b35f8e`, source configuration `5650250`, and publication `5f62980`; its developer-reported gameplay checks and unverified Sheet delivery remain separate evidence.

Use [publish_instructions.md](publish_instructions.md) for future releases. Preserve completed exports and historical paths; use a new agreed version/output for the next build. Do not merge gh-pages into source. See [AnalyticsImplementation.md](AnalyticsImplementation.md) and [AnalyticsTestPlan.md](AnalyticsTestPlan.md) for 0.1.1 production event/map acceptance; Editor JSON and package verification do not prove delivery.

## 5. Verification and remaining work

The developer confirmed the merge and scene-promotion playtests. Isolated Unity checks passed compilation, scene/reference validation, Mainmenu/IntroBubble entries, all four Level01 completion forms, the two new intermediate destinations, checkpoint forms/state retention, full-reset behavior and Home/R menu behavior. The original Level01 scene/meta were preserved unchanged as Legacy; other production scene files were unchanged by promotion.

Outstanding work is scoped separately: a dedicated Level01 Controls panel, new-player timing/readability, broader browser/mobile/aspect-ratio coverage, 0.1.1 analytics delivery/map validation and cross-scene regression of the unpublished help-book UI. The contact-activated Level01 door hint is already included in published 0.1.1. The scene rename must not re-enable the legacy E reminder. Do not mark source changes as deployed without a tested export and publication.
