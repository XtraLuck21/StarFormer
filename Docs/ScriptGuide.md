# Script Guide

Last updated: 2026-10-06. Unity version: `6000.3.22f1`.

Current test scenes and gameplay scripts. For controls and gameplay behavior, see [GameplaySpec.md](GameplaySpec.md).

## 1. Test scenes

| Scene | Purpose | Relevant features |
| --- | --- | --- |
| [Playground](../Assets/Scenes/Playground.unity) | Existing mechanics sandbox | Star, box, bubble, resizing, pressure plate and door |
| [Level01_Reference](../Assets/Scenes/Level01_Reference.unity) | Earlier level layout retained as a reference | Traversal and transformation layout; not the new Alpha Level 1 |
| [Tutorial](../Assets/Scenes/Tutorial.unity) | Current playable tutorial | Movement, jump, lethal spikes, box possession and resizing, pressure plate, goal and feedback |
| [WaterPlayground](../Assets/Scenes/WaterPlayground.unity) | Water integration sandbox | Water detection and water behavior for star, box and bubble |

All four scenes are enabled in [EditorBuildSettings.asset](../ProjectSettings/EditorBuildSettings.asset). The saved list currently begins with Playground; do not assume Tutorial is the launch scene. Full-scene restart requires the current scene to be included in the enabled build scene list. Check the active build profile when preparing a build.

## 2. Script map

### Shared character systems

| Script | Attach to / dependencies | Current responsibility | Change here when... |
| --- | --- | --- | --- |
| [PossessableBody.cs](../Assets/Scripts/PossessableBody.cs) | Each body root; `Rigidbody2D`, a root `Collider2D`, and an assigned separate control-visual child | Defines `BodyForm`; holds movement settings and ownership; exposes grounded/water state; forwards eligible collision contacts to the player controller; logs water transitions | Adding a form identifier or a capability common to bodies |
| [PlayerPossessionController.cs](../Assets/Scripts/PlayerPossessionController.cs) | One `PlayerControl` object; assign an active, configured initial star body | Reads keyboard input; controls the current body; dispatches movement by form; transfers possession; pops bubbles; handles death, vertical bounds and full-scene restart | Adding a form's movement/input integration, changing transfer rules, or changing restart behavior |
| [BodyWaterPhysics.cs](../Assets/Scripts/BodyWaterPhysics.cs) | Each water-enabled body root; `Rigidbody2D` and `PossessableBody` | Caches original gravity/damping; applies water gravity/damping to controlled and unpossessed bodies; restores original settings outside water or on disable | Adding a special water-physics rule or changing damping; swimming input belongs in the controller |
| [WaterZone.cs](../Assets/Scripts/WaterZone.cs) | Each water-region object with `BoxCollider2D` | Makes the collider a trigger, registers enabled water zones, and exposes point-in-water queries | Changing water-region detection; do not put individual character movement here |

### Abilities and visuals

| Script | Attach to / dependencies | Current responsibility | Change here when... |
| --- | --- | --- | --- |
| [BoxResizeAbility.cs](../Assets/Scripts/BoxResizeAbility.cs) | Box root; `PossessableBody`, `Rigidbody2D`, `BoxCollider2D` | Reads W/S only while controlled; resizes within limits; checks solid obstacles; preserves the bottom while grounded and the collider center otherwise | Changing box resizing, size limits or clearance checks |
| [StarVisual.cs](../Assets/Scripts/StarVisual.cs) | Star `Visual` or host `StarMark` child; `SpriteRenderer` | Generates a star sprite at runtime; applies `starColor` and sorting order 10; disposes generated assets on destruction | Changing the star/possession marker's appearance, not its controls or collider |

`BoxResizeAbility` currently requires an upright, unparented box with positive scale and a collider edge radius of zero. The intended box setup uses uniform X/Y scaling. It ignores triggers during clearance checks and does not change mass. Do not parent a box under a level-group object without first updating this constraint.

### Interactions and tutorial

| Script | Attach to / dependencies | Current responsibility | Change here when... |
| --- | --- | --- | --- |
| [HeavyPressurePlate.cs](../Assets/Scripts/HeavyPressurePlate.cs) | Plate; `BoxCollider2D`, `SpriteRenderer`, assigned separate door object | Checks a dynamic body's mass and supporting contact in the plate area; changes color and disables the door; activation stays latched | Changing weight requirements or plate/door behavior |
| [SpikeHazard.cs](../Assets/Scripts/SpikeHazard.cs) | Environmental spike; `PolygonCollider2D`, assigned scene controller | Uses a trigger to kill the currently controlled body; unpossessed bodies do not trigger player death | Changing lethal hazard contact behavior |
| [TutorialIntroUI.cs](../Assets/Scripts/TutorialIntroUI.cs) | Tutorial canvas; assigned box, controller, and two TMP text references | Displays the initial controls, first box-possession instructions, death message and completion message | Changing tutorial copy or its UI states |
| [TutorialGoal.cs](../Assets/Scripts/TutorialGoal.cs) | Tutorial goal; `BoxCollider2D`, assigned tutorial box, door and UI | Completes only when the assigned controlled box enters the goal with the door inactive and the player alive; synchronizes its visible physics pose and pauses the game | Changing tutorial-specific completion conditions or pause behavior |
