# Script Guide

Last updated: 2026-10-07. Unity version: `6000.3.22f1`.

Current test scenes and gameplay scripts. For controls and gameplay behavior, see [GameplaySpec.md](GameplaySpec.md). For the current Level 1 handoff and remaining work, see [level-1.md](level-1.md).

## 1. Test scenes

| Scene | Purpose | Relevant features |
| --- | --- | --- |
| [Playground](../Assets/Scenes/Playground.unity) | Existing mechanics sandbox | Star, box, bubble, resizing, pressure plate and door |
| [Level01_Reference](../Assets/Scenes/Level01_Reference.unity) | Earlier level layout retained as a reference | Traversal and transformation layout; not the new Alpha Level 1 |
| [Tutorial](../Assets/Scenes/Tutorial.unity) | Current playable tutorial | Movement, jump, three SpikeHazard prefab instances, box possession and resizing, pressure plate, goal and feedback |
| [WaterPlayground](../Assets/Scenes/WaterPlayground.unity) | Water integration sandbox | Water detection and water behavior for star, box and bubble; saved instances of the three core prefabs |
| [Level01](../Assets/Scenes/Level01.unity) | Alpha Level 1 graybox in progress | Water routes, box size gates, held pressure plate, height switch, bubble-pop hazards and star-specific completion; bouncing ball is not integrated |
| [EnemyScene](../Assets/Scenes/EnemyScene.unity) | Teammate test scene present in this checkout | Not reviewed or playtested as part of the Level 1 work |
| [FanScene](../Assets/Scenes/FanScene.unity) | Fan sandbox | Star, fan possession, W toggles airflow, Space leaves the fan (2s repossess cooldown), red blowable obstacles and a box body |

The five scenes other than EnemyScene are enabled in [EditorBuildSettings.asset](../ProjectSettings/EditorBuildSettings.asset). The saved list currently begins with Playground; do not assume Tutorial is the launch scene. Full-scene restart requires the current scene to be included in the enabled build scene list. Check the active build profile when preparing a build.

## 2. Script map

### Shared character systems

| Script | Attach to / dependencies | Current responsibility | Change here when... |
| --- | --- | --- | --- |
| [PossessableBody.cs](../Assets/Scripts/PossessableBody.cs) | Each body root; `Rigidbody2D`, a root `Collider2D`, and an assigned separate control-visual child | Defines `BodyForm`; holds movement settings and ownership; exposes grounded/water state; forwards eligible collision contacts to the player controller; logs water transitions | Adding a form identifier or a capability common to bodies |
| [PlayerPossessionController.cs](../Assets/Scripts/PlayerPossessionController.cs) | One `PlayerControl` object; assign an active, configured initial star body | Reads keyboard input; controls the current body; dispatches movement by form; transfers possession; queues Space/hazard bubble pops through the same flow (`RequestBubblePop`); handles death, vertical bounds and full-scene restart | Adding a form's movement/input integration, changing transfer rules, or changing restart behavior |
| [BodyWaterPhysics.cs](../Assets/Scripts/BodyWaterPhysics.cs) | Each water-enabled body root; `Rigidbody2D` and `PossessableBody` | Caches original gravity/damping; applies water gravity/damping to controlled and unpossessed bodies; restores original settings outside water or on disable | Adding a special water-physics rule or changing damping; swimming input belongs in the controller |
| [WaterZone.cs](../Assets/Scripts/WaterZone.cs) | Each water-region object with `BoxCollider2D` | Makes the collider a trigger, registers enabled water zones, and exposes point-in-water queries | Changing water-region detection; do not put individual character movement here |

### Abilities and visuals

| Script | Attach to / dependencies | Current responsibility | Change here when... |
| --- | --- | --- | --- |
| [BoxResizeAbility.cs](../Assets/Scripts/BoxResizeAbility.cs) | Box root; `PossessableBody`, `Rigidbody2D`, `BoxCollider2D` | Reads W/S only while controlled; resizes within limits; checks solid obstacles; preserves the bottom while grounded and the collider center otherwise | Changing box resizing, size limits or clearance checks |
| [StarVisual.cs](../Assets/Scripts/StarVisual.cs) | Star `Visual` or host `StarMark` child; `SpriteRenderer` | Generates a star sprite at runtime; applies `starColor` and sorting order 10; disposes generated assets on destruction | Changing the star/possession marker's appearance, not its controls or collider |
| [FanBlower.cs](../Assets/Scripts/FanBlower.cs) | Fan root; `Rigidbody2D`, `PossessableBody` | While controlled, A/D set facing and W toggles wind; pushes non-possessable bodies in range with distance falloff; state persists after the player leaves | Changing fan wind rules, range or force |
| [FanVisual.cs](../Assets/Scripts/FanVisual.cs) | Fan `Face` child; `SpriteRenderer` | Generates a ring-and-blades sprite at runtime; spins while blowing and mirrors with facing | Changing the fan face look or spin |
| [FanAirflowVisual.cs](../Assets/Scripts/FanAirflowVisual.cs) | Fan `Airflow` child; optional `FanBlower` | Emits white dots outward while blowing; they fade out at max range | Changing airflow particle look or density |

`BoxResizeAbility` currently requires an upright, unparented box with positive scale and a collider edge radius of zero. The intended box setup uses uniform X/Y scaling. It ignores triggers during clearance checks and does not change mass. Do not parent a box under a level-group object without first updating this constraint.

**Reusable core prefabs:** [StarPlayer](../Assets/Form/StarPlayer.prefab), [BoxBody](../Assets/Form/BoxBody.prefab), and [BubbleBody](../Assets/Form/BubbleBody.prefab) are available in `Assets/Form`. Each includes its physics components, `PossessableBody`, `BodyWaterPhysics`, and its internally assigned control-visual child; BoxBody also includes `BoxResizeAbility`.

- Drag the required prefabs into a scene and set their positions. Keep boxes at the scene root to satisfy the resizing constraint above.
- Keep one scene `PlayerControl` with `PlayerPossessionController`; assign the **scene instance** of StarPlayer to `Initial Body`. The controller is not included in these prefabs.
- Water requires scene objects with `WaterZone`. Canopies, platforms, switches, gates and goal UI remain scene objects. Place hazard prefab instances separately and assign their scene controller references as described below.
- When replacing an existing body, check references to that body, especially `Initial Body` and assigned-box switches. Internal `Control Visual` references are already included in the prefab.
- The star sprite is generated in Play Mode by `StarVisual`; an absent edit-time star preview does not by itself indicate a broken prefab. Change a scene instance's placement locally; apply changes to the prefab asset only when they should become shared defaults.

### Interactions, hazards and level feedback

| Script | Attach to / dependencies | Current responsibility | Change here when... |
| --- | --- | --- | --- |
| [HeavyPressurePlate.cs](../Assets/Scripts/HeavyPressurePlate.cs) | Plate; `BoxCollider2D`, `SpriteRenderer`, assigned separate door object | Checks a dynamic body's mass and supporting contact in the plate area; changes color and disables the door; activation stays latched | Changing weight requirements or plate/door behavior |
| [HeldPressurePlate.cs](../Assets/Scripts/HeldPressurePlate.cs) | Plate; `BoxCollider2D`, `SpriteRenderer`, assigned target box and separate door | Opens the door only while the assigned grounded box supports its feet in the plate area; leaving closes it; Level01 P targets C and controls G | Changing continuous plate occupancy; this plate has no mass threshold and does not latch |
| [BoxHeightSwitch.cs](../Assets/Scripts/BoxHeightSwitch.cs) | Overhead switch; `BoxCollider2D`, `SpriteRenderer`, assigned target box and separate door | Latches the door open when the assigned grounded box touches the switch; Level01 H targets D and controls G2 | Changing height-switch activation; required growth comes from placement, not an explicit size threshold |
| [BubblePopHazard.cs](../Assets/Scripts/BubblePopHazard.cs) | Hazard; `CircleCollider2D`, assigned scene player controller | Requests a pop when the controlled bubble enters or remains in its trigger; ignores star, box and unpossessed bodies | Changing nonlethal bubble-only hazards; shared pop behavior stays in the player controller |
| [SpikeHazard.cs](../Assets/Scripts/SpikeHazard.cs) | Environmental spike; `PolygonCollider2D`, assigned scene controller | Uses a trigger to kill the currently controlled body; unpossessed bodies do not trigger player death | Changing lethal hazard contact behavior |
| [TutorialIntroUI.cs](../Assets/Scripts/TutorialIntroUI.cs) | Tutorial canvas; assigned box, controller, and two TMP text references | Displays the initial controls, first box-possession instructions, death message and completion message | Changing tutorial copy or its UI states |
| [TutorialGoal.cs](../Assets/Scripts/TutorialGoal.cs) | Tutorial goal; `BoxCollider2D`, assigned tutorial box, door and UI | Completes only when the assigned controlled box enters the goal with the door inactive and the player alive; synchronizes its visible physics pose and pauses the game | Changing tutorial-specific completion conditions or pause behavior |
| [LevelGoal.cs](../Assets/Scripts/LevelGoal.cs) | Level goal; `BoxCollider2D`, assigned player controller, required gate and TMP status text | Completes when the living controlled star enters with the required gate inactive; synchronizes the visible physics pose, pauses gameplay and shows completion/restart text; Level01 requires G2 | Changing non-tutorial goal conditions or completion feedback; scene transitions are not implemented here |

**Reusable hazard prefabs:** all five current prefabs share `Assets/Form` by team convention. Being stored in this folder does not make a hazard possessable.

| Prefab | Included components | Scene setup / behavior |
| --- | --- | --- |
| [SpikeHazard](../Assets/Form/SpikeHazard.prefab) | Sprite renderer, trigger `PolygonCollider2D`, `SpikeHazard` | Assign the scene's `PlayerControl` to `Player Controller`; contact kills the currently controlled form. TutorialSpike01–03 now use this prefab. |
| [WaterPopHazard](../Assets/Form/WaterPopHazard.prefab) | Sprite renderer, trigger `CircleCollider2D`, `BubblePopHazard` | Assign the scene's `PlayerControl` to `Player Controller`; contact pops only the controlled bubble. The asset name differs from its script name. |

- Both prefab assets intentionally store `Player Controller` as None. Set it on **each scene instance before Play Mode**; do not try to store a reference to a particular scene inside the shared asset.
- If that reference is missing at initialization, the script logs an error and disables itself. Exit Play Mode, assign the reference, then test again.
- `WaterPopHazard` has no water-only check: placing it underwater is a level-design choice. It ignores the star, box and unpossessed bubbles.
- Developer-reported tests passed for new hazard instances: spike death/restart in Tutorial and bubble-only popping in Level01.
