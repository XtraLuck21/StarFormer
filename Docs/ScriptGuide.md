# Script and Scene Guide

Last updated: 2026-11-10. Beta 0.1.2 is published to root and `/beta/` via `64b35c6`; source checkout at publication: `b93768f`. Deployment and 14 hosted file comparisons passed. See the [release record](Beta-0.1.2.md) for features, isolated Editor evidence and outstanding browser/data checks. The replacement Level02 is now integrated; [Beta-0.1.3.md](Beta-0.1.3.md) records its unpublished release preparation.

## 1. Scenes and build configuration

| Enabled index | Scene | Role / continuation |
| --- | --- | --- |
| 0 | [Navigation](../Assets/Scenes/Navigation.unity) | Interactive startup; Kindergarten, Level 1, Elementary School and Level 2 entrances |
| 1 | [IntroBox](../Assets/Scenes/IntroBox.unity) | Box flag opens an exit to IntroBubble |
| 2 | [IntroBubble](../Assets/Scenes/IntroBubble.unity) | Grounded-Star flag opens passage to IntroBouncyBall; return to IntroBox |
| 3 | [IntroBouncyBall](../Assets/Scenes/IntroBouncyBall.unity) | Bounce/float/compressed-bounce tutorial; return to Bubble, flag to Level01 |
| 4 | [Level01](../Assets/Scenes/Level01.unity) | Advanced map promoted to formal first level; flag loads IntroSpider |
| 5 | [IntroSpider](../Assets/Scenes/IntroSpider.unity) | Continues to IntroFan |
| 6 | [IntroFan](../Assets/Scenes/IntroFan.unity) | Continues to Level02 |
| 7 | [Level02](../Assets/Scenes/Level02.unity) | Promoted chase/dark passage/seesaw/Smiley Face layout; custom raised-flag victory |

The current source names the Fan tutorial `IntroFan` (formerly `FanTutorial`), preserving its scene GUID. Published builds and historical analytics retain the old name. Deploy the local analytics compatibility update with the next game release; this rename does not update the hosted build.

Transitions use scene names rather than adjacent build indices. The actual forward flow is Navigation → IntroBox → IntroBubble → IntroBouncyBall → Level01 → IntroSpider → IntroFan → Level02.

[EditorBuildSettings.asset](../ProjectSettings/EditorBuildSettings.asset) owns the enabled list. [WebGLBuild.cs](../Assets/Editor/WebGLBuild.cs) exports that list. Keep Mainmenu, Level01_Legacy, Level01_Reference, Tutorial, WaterPlayground and Playground disabled. EnemyScene is a separate test scene, not part of this progression.

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

The controller retains the merged Box movement/growth rules, contact separation before reacquisition, and temporary departed-Box/new-Bubble collision exclusion, plus the PR #21 Box-to-BouncyBall recovery safeguards. Do not replace these with global collision-layer changes. Preserve Star's enabled PolygonCollider and the scene/prefab's disabled legacy Capsule where configured.

### Current Level01

| Script | Responsibility / important references |
| --- | --- |
| [AdvancedRouteSwitch.cs](../Assets/Scripts/AdvancedRouteSwitch.cs) | SAVE/NORMAL/RUIN state, button labels/rearming, initial Box-or-Star warning, hazard animation and camera ownership. Assign both button sensors/faces/labels, Box D, R2, guard, camera and separate NORMAL/SAFE/HARD roots. No collider/name substitution from the old map. |
| [SafeRouteObstacles.cs](../Assets/Scripts/SafeRouteObstacles.cs) | Three black fixed-size boards: one static, two vertically translating in opposite phase. Shared travel clock; leading-edge protection pauses the pair. Reveals after spike retraction and demonstrates a full alternating cycle. |
| [HardRouteFallingSpike.cs](../Assets/Scripts/HardRouteFallingSpike.cs) | Assigned ceiling tooth falls only in HARD when the current body enters its upper-route horizontal band. Sweeps the actual collider, stays landed and lethal, resets for route changes/retries. Assign route/controller/tooth and upper floor height. |
| [RuinButtonGuard.cs](../Assets/Scripts/RuinButtonGuard.cs) | First upward hit by assigned R2 breaks the cover, spawns short-lived fragments and forces downward velocity. The button arms only after a later landing. RestoreBrokenState supports checkpoints. |
| [AdvancedLevelCamera2D.cs](../Assets/Scripts/AdvancedLevelCamera2D.cs) | Current Level01 regional framing, local position/zoom limits and stabilized BouncyBall framing near checkpoint 2. Route presentations temporarily override it. RegionalCameraFollow2D is the old map's camera, not this scene's active system. |
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
| [IntroStarGoal.cs](../Assets/Scripts/IntroStarGoal.cs) | Shared intro transition used by IntroSpider/IntroFan. IntroBubble now uses flag + passage instead. |
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
| [level2Camera.cs](../Assets/Scripts/StartingStage/level2Camera.cs) | Chase, fall/dark passage, fixed seesaw/tooth views, brief complete-face reveal, closer forehead framing and follow |
| [DarkStarPassage.cs](../Assets/Scripts/StartingStage/DarkStarPassage.cs), [DropBottomCheckpoint.cs](../Assets/Scripts/StartingStage/DropBottomCheckpoint.cs) | Descent darkness, local light recovery, glowing exit and safe bottom landing |
| [SeesawRoomSupplies.cs](../Assets/Scripts/SeesawRoomSupplies.cs), [SeesawRoomStage.cs](../Assets/Scripts/SeesawRoomStage.cs) | Direction-triggered Fan supply, single ordinary ball, blue/green cleanup and staged barrier/platform reveal |
| [SeesawOverloadSequence.cs](../Assets/Scripts/SeesawOverloadSequence.cs), [SeesawBouncyFinale.cs](../Assets/Scripts/SeesawBouncyFinale.cs) | Yellow three-ball loading, floating delayed !, Spider passage and four underside impacts |
| [SeesawRoomExit.cs](../Assets/Scripts/SeesawRoomExit.cs), [SeesawBallShellExit.cs](../Assets/Scripts/SeesawBallShellExit.cs) | Four-gear quake, rising lift, continuous ride and real Star handoff during shell breakup |
| [TeethTriggeredDoor.cs](../Assets/Scripts/TeethTriggeredDoor.cs), [SmileFaceCompletion.cs](../Assets/Scripts/SmileFaceCompletion.cs) | Three tooth arrivals, connected grin/opening eyes, separate camera phases |
| [SmileFaceFinalRoute.cs](../Assets/Scripts/SmileFaceFinalRoute.cs), [SmileRouteCheckpoint.cs](../Assets/Scripts/SmileRouteCheckpoint.cs) | Ordered Spider/Star/Box checkpoints, pure blue/yellow markers and forehead fall-return protection |
| [SmileBoxPressureButton.cs](../Assets/Scripts/SmileBoxPressureButton.cs), [SmileGoalFlag.cs](../Assets/Scripts/SmileGoalFlag.cs) | Assigned final Box opens the flag stage; actual flag contact displays the custom victory overlay |
| [HexagonSurfaceMovement.cs](../Assets/Scripts/spiderFolder/HexagonSurfaceMovement.cs) | Climb real solid surfaces; ignore checkpoint/water trigger volumes |
| [HelpBookUI.cs](../Assets/UI/HelpBookUI.cs) | Configured six-page Level02 Collection Book alongside shared Home; obsolete empty Help component removed |

The promoted scene replaces the old layout, retained as Level02_legacy and excluded from builds. The obsolete right-side BubbleSpawner and CP03 are gone. The final Smile flag does not yet invoke shared completion analytics or a Navigation-return control. See [level-2.md](level-2.md) for mechanics, recovery and remaining acceptance.

## 3. Navigation and interactive menu

| Script | Behavior and wiring |
| --- | --- |
| [MainMenuUI.cs](../Assets/Scripts/MainMenuUI.cs) | OpenIntroLevels → IntroBox; OpenTutorial aliases it; OpenLevel1 → Level01; OpenLevel2Intro → IntroSpider; OpenLevel2 → Level02. Clears checkpoint sessions and restores time before loading. |
| [MainMenuButtonUI.cs](../Assets/Scripts/MainMenuButtonUI.cs) / [HomeButtonGraphic.cs](../Assets/Scripts/HomeButtonGraphic.cs) | Shared runtime Home square in all seven gameplay scenes, house/Star graphic, Home · Restart tooltip, Paused menu, conditional checkpoint action, and restart/menu confirmations. Owns/restores pause and blocks underlying input. |
| [RestartMenuUI.cs](../Assets/Scripts/RestartMenuUI.cs) | R/touch restart choices; conditional Return to Checkpoint, Restart Level and Cancel. Escape/R cancels. Delegates recovery to the local player controller. |
| [LevelNavigationUI.cs](../Assets/Scripts/LevelNavigationUI.cs) | Retained navigation callbacks; current visible gameplay menu is shared Home. Full restart clears progress; Retry preserves it. Uses current-scene identity; do not hardcode the old Advanced name. |
| [LevelGoal.cs](../Assets/Scripts/LevelGoal.cs) | Optional required gate: an assigned active gate blocks, an absent gate does not. Validates destination and accepts a living controlled form. Current Level01 uses IntroSpider / Level 2 Intro / 1.5 seconds and no gate. |
| [LevelTransitionUI.cs](../Assets/Scripts/LevelTransitionUI.cs) | Generated overlay; disables player, pauses time, blocks pointer input, clears selection and loads after real-time delay. Clears sessions and restores time. Final-result mode waits for the menu button. |
| [LevelGoaltoMenu.cs](../Assets/Scripts/LevelGoaltoMenu.cs) | Class LevelGoalToMenu: Level02 completion event and final-result overlay with Return to Main Menu. |
| [MenuUIPlatform.cs](../Assets/Scripts/MenuUIPlatform.cs) | Aligns one-way colliders to UI rectangle tops using the menu camera. Contact does not submit buttons. |

Navigation entrance targets are IntroBox, Level01, IntroSpider and Level02. IntroBouncyBall still continues to Level01; do not target Level01_Legacy. The older MainMenuUI/MenuUIPlatform helpers belong to retained Mainmenu and are not the current startup implementation.

| Current navigation/help script | Responsibility |
| --- | --- |
| [NavigationStarController.cs](../Assets/Scripts/NavigationStarController.cs) | A/D and Space movement, contact/click entrance selection, bounce/flash animation, Entering overlay and destination load |
| [NavigationEntrance.cs](../Assets/Scripts/NavigationEntrance.cs) | Scene destination and upper/lower interaction rules; Coming Soon has no scene |
| [NavigationHelpBook.cs](../Assets/Scripts/NavigationHelpBook.cs) | Align book control with its world anchor, block Star input while open and disable entry-time interaction |
| [CollectionBookGraphic.cs](../Assets/UI/CollectionBookGraphic.cs) | Procedural book icon; reusable CollectionBook/ControlsHelp prefabs reference shared content rather than duplicate demo images |
| [HelpBookUnlocks.cs](../Assets/UI/HelpBookUnlocks.cs) | Sole in-memory form discovery record; RecordPossession after successful ownership, Changed event for both views, reset on application/Play launch. No PlayerPrefs access. |
| [ShapeClosetSlot.cs](../Assets/UI/ShapeClosetSlot.cs) | Event-driven per-form visual; all five slot roots remain active, only locked artwork/unlocked icons toggle |
| [IntroSpiderCake.cs](../Assets/Scripts/IntroSpiderCake.cs) / [BubbleMovementHint.cs](../Assets/Scripts/BubbleMovementHint.cs) | Cake/spike reveal, contextual climbing prompts, directional arrows and center death feedback |

Navigation's entering overlay draws above its Collection Book so the entire page dims uniformly. The disabled book color remains the normal color; the common overlay supplies dimming. Navigation UI Events routes pointer/UI input; it is not a gameplay checkpoint or progress manager.

All seven gameplay scenes use Home with their local player controller. Legacy standalone buttons are disabled; R remains available. Death/Retry retains gameplay checkpoints; Restart and menu/new-level entry clears them. Discovery survives these scene operations but clears on the next launch. The old persistent completion classes, view binding and goal reward hooks are removed; completion does not unlock unused forms. Navigation’s shared content now includes matching unlock IDs. See [the release specification](Beta-0.1.2.md#session-only-shape-discovery).

## 4. Web build and GitHub Pages release

Beta 0.1.2 was published on November 6, 2026 to both the [main website](https://xtraluck21.github.io/StarFormer/) and [Beta website](https://xtraluck21.github.io/StarFormer/beta/). The same `Builds/WebGL-Beta-0.1.2` export was committed and pushed on `gh-pages` as [`64b35c6`](https://github.com/XtraLuck21/StarFormer/commit/64b35c6c5dbe50ffefc05e1590533cebf47888dc). [GitHub Pages deployment](https://github.com/XtraLuck21/StarFormer/actions/runs/42303613672) succeeded. All seven files at each URL (14 files total) returned HTTP 200 and matched the local export byte for byte by SHA-256. This verifies deployment and package integrity, not a new hosted gameplay playtest, mobile acceptance or production analytics delivery. Analytics map export/backend updates were not performed in this publication; Level02 repairs remain deferred to its replacement scene.

### Historical releases

Beta 0.1.1 was published on November 3, 2026 at [the Beta URL](https://xtraluck21.github.io/StarFormer/beta/), from source `9451511` as `gh-pages` commit `e81097e`. On November 5, all seven live Beta files returned HTTP 200 and matched `Builds/WebGL-Beta-0.1.1` and the publishing checkout byte for byte. This confirms the deployed package, not a new gameplay playtest or production analytics acceptance. Player/Analytics versions are `beta-0.1.1`; the released source tracks all seven gameplay scenes (IntroBox, IntroBubble, IntroBouncyBall, Level01, IntroSpider, FanTutorial and Level02), excluding Mainmenu. Current first-level checkpoint IDs remain ADV_CP1/2/3.

The November 4 paged help book, six-form demo animations, Unity Recorder package and Mainmenu layout/behavior changes (`3a03844`, `f05d433`, `55f6b6b`, `f2c5c48`) were integrated after the historical Beta 0.1.1 export; their runtime changes are included in Beta 0.1.2. Unity Recorder remains Editor tooling. The release-preparation patch was rebased to `2caffd9` on November 5, but the November 3 export was built from `9451511`. Shared version labels do not make current main identical to the published game.

Historical Beta 0.1.0 used source `3a693ca`, publishing commit `1105970`, and Level01/Level02-only analytics. Developer-reported local/online tests and the November 2 Level01 dashboard screenshot apply to that older export. Dashboard Version 9 subsequently resolved its map matching. Root previously served 0.1.0 via `f6eaf9e`, and `/beta/` previously served 0.1.1; both were replaced with 0.1.2 on November 6. The earlier Web compiler fix is `3365e29`.

Historical Alpha 0.1.2 (October 18) used gameplay/UI `9b35f8e`, source configuration `5650250`, and publication `5f62980`; its developer-reported gameplay checks and unverified Sheet delivery remain separate evidence.

Use [publish_instructions.md](publish_instructions.md) for future releases. Preserve completed exports and historical paths; use a new agreed version/output for the next build. Do not merge gh-pages into source. See [AnalyticsImplementation.md](AnalyticsImplementation.md) and [AnalyticsTestPlan.md](AnalyticsTestPlan.md) for version-specific production event/map acceptance; Editor JSON and package verification do not prove delivery.

## 5. Verification and remaining work

The developer confirmed the merge and scene-promotion playtests. Isolated Unity checks passed compilation, scene/reference validation, Mainmenu/IntroBubble entries, all four Level01 completion forms, the two new intermediate destinations, checkpoint forms/state retention, full-reset behavior and Home/R menu behavior. The original Level01 scene/meta were preserved unchanged as Legacy; other production scene files were unchanged by promotion.

Post-publication follow-up: verify session-only discovery in the exported player, repeat the cleaned tutorial Help checks in the exported player (Level02 Help work is deferred), regenerate all seven maps, and validate the beta-0.1.2 Web player and backend data. Broader browser/mobile coverage and new-player readability remain separate checks. Previous release acceptance does not certify latest main; see [Beta-0.1.2.md](Beta-0.1.2.md).

The isolated [session-discovery regression](../Assets/Editor/SessionDiscoveryVerification.cs) checks real ownership/UI paths, completion without group rewards, retries and a second Play session with domain reload disabled. Execution instructions, historical Help findings and the subsequent three-scene cleanup verification are in [AnalyticsTestPlan.md](AnalyticsTestPlan.md#session-discovery-regression).
