# Gameplay Specification

Last updated: 2026-10-11.

This document records current controls, implemented gameplay and unresolved design decisions. For script responsibilities and integration points, see [ScriptGuide.md](ScriptGuide.md).

Baseline: Level 1 polish merged into `main` in PR #8 (`c63a17c`), followed by the `navigation` implementation through `1e330f0`. The menu links Tutorial and Level01, both scenes have return buttons, and the menu includes a playable star. Level 1 includes any-form completion, removal of the final U3 bubble and canopy, revised hazards, and death feedback. The developer reported successful manual tests of the latest changes, including upper bouncing-ball traversal, goal completion and death/restart feedback. These reports cover the tested interactions and route segments; they do not establish exhaustive start-to-finish regression coverage.

## 1. Current scope

| Feature | Status |
| --- | --- |
| Star, box and bubble; automatic contact possession | Implemented and manually tested |
| Box resizing; pressure plate opening a door | Implemented and manually tested |
| Bubble popping and return to the star | Implemented and manually tested |
| Water behavior for star, box and bubble | Implemented and manually tested in WaterPlayground |
| Tutorial instructions, lethal spikes, death feedback and completion | Implemented and manually tested in Tutorial |
| R restart; automatic restart after death / vertical out-of-bounds | Implemented |
| Reusable StarPlayer, BoxBody, BubbleBody, SpikeHazard and WaterPopHazard prefabs | Available in `Assets/Form`; developer-reported instance tests passed |
| Level01 held plate, overhead height switch, purple water hazards, any-form completion and death UI | Implemented; developer-reported tests passed |
| Reusable BouncyBall prefab; automatic land bounce, A/D steering, ceiling compression and water-surface floating | Implemented; two Level01 instances and developer-reported segment tests passed |
| Level01 three-region camera with bounded follow and smooth position/zoom transitions | Implemented and tested for lower-left, lower-right, upper and possession changes |
| Level01 complete route, including final bouncing-ball traversal | Upper floor-spike traversal and completion tested; final full-route regression remains |
| Main menu, Tutorial/Level01 selection and return-to-menu buttons | Implemented; developer-reported navigation tests passed |
| Playable menu star and one-way title/button platforms | Implemented; movement, jumping, foreground rendering and button clicks passed developer tests |
| Level 2 menu entry | Coming Soon placeholder with no click action; no Level 2 scene is connected |
| Fan-generated wind and form-specific wind response | Not integrated into the current shared controller |
| Possessable spike wall, enemies and spider | Not integrated into the current shared controller |

`Playground` remains a mechanics sandbox. `Level01_Reference` preserves an earlier level layout; it is not the final Alpha Level 1. `Tutorial` is the current teaching scene, and `WaterPlayground` isolates water behavior. `Level01` is the current Alpha Level 1 graybox; see [level-1.md](level-1.md) for its progress and handoff. An `EnemyScene` also exists in this checkout, but its presence does not establish that enemy gameplay is integrated or tested.

## 2. Controls

Keys below describe the currently controlled form. W/S are held inputs; Space and R respond to a new press.

| Form / environment | A / D | W | S | Space | R |
| --- | --- | --- | --- | --- | --- |
| Star, outside water | Move left/right | No action | No action | Jump when grounded | Restart scene |
| Star, underwater | Slower left/right movement | No action | No action | Reduced jump when grounded | Restart scene |
| Box, outside water | Move left/right while grounded | Grow | Shrink | No action | Restart scene |
| Box, underwater | Slower left/right movement while grounded | Grow | Shrink | No action | Restart scene |
| Bubble, in air | Move left/right | No action | No action | Pop and return to star | Restart scene |
| Bubble, underwater | Swim left/right | Swim up | Swim down | Pop and return to star | Restart scene |
| Bouncing ball, on land | Accelerated steering left/right while automatically bouncing | No action | No action | No action | Restart scene |
| Bouncing ball, touching water | Reduced left/right movement while locked to the water surface | No action | No action | No action; vertical bouncing is disabled | Restart scene |

- Left/Right Arrow also work as alternatives to A/D in the current controller. Up/Down Arrow are not implemented as W/S alternatives.
- There is no manual possession key. Contact with an eligible body automatically requests a transfer.
- There is no general key for exiting a box. Returning directly to the star is currently the bubble's pop action.
- R can restart during gameplay, the death pause and the Tutorial and Level01 completion screens.

## 3. Shared possession and failure rules

### Possession

- Only eligible possessable bodies can be taken over. Ground, ordinary platforms, doors, water regions and environmental spikes are not possessable.
- Touching another eligible body transfers control to that existing body at its position; it does not create a copy at the player's previous position.
- The star is the only player identity. When it enters a host, the original `StarPlayer` object is fully inactive, including its collider and rigidbody; a centered star marker identifies the controlled host, including the bouncing ball.
- Transferring between non-star hosts moves the star's control to the touched body without consuming the previous host. Boxes retain gravity, abandoned bubbles retain their environmental physics, and an abandoned bouncing ball freezes in place until possessed again.
- A contact can cause at most one transfer. Every possessable body already touching or immediately surrounding the new host is blocked until it remains safely separated for several physics frames; only a new collision after separation can transfer control again. The queued transfer also verifies that the source and target are still touching before it executes.
- Contact possession remains active underwater. Popping above a box can therefore lead to automatic box possession when the released star lands on it.

### Death and restart

- A hazard using `SpikeHazard` kills the currently controlled body in any implemented form.
- A hazard using `BubblePopHazard` pops a controlled bubble and releases the star, but kills a controlled box through the normal death/restart flow. This includes a box growing into the trigger. It ignores the star, bouncing ball and unpossessed bodies. The reusable `WaterPopHazard.prefab` displays a purple circle with a centered black X to distinguish it from a possessable bubble. Its behavior is not restricted to water.
- Contact by an unpossessed body does not itself kill the player.
- Death pauses gameplay, then reloads the whole current scene after the configured delay. Tutorial, WaterPlayground and Level01 currently save a delay of **2 seconds**. R bypasses the wait.
- Tutorial and Level01 display `You died.`, `Restarting...`, and `Press R to restart now.` during the death pause. Level01 uses `LevelDeathUI` and the existing `LevelStatusText`; the controller still owns death timing and scene reload. The text is blank before play and during normal gameplay. The sandbox scenes do not currently contain this death-feedback UI.
- Automatic vertical failure occurs when the entire controlled collider is below `minimumHeight` or above the active form's upper limit. Tutorial and WaterPlayground save **-10 / 8**. Level01 uses **-12 / 16** for the star, box and bubble, while the bouncing ball uses a separate upper limit of **40** so its 20.25-unit open-air bounce does not cause an unintended death. These values are not a general left/right map-boundary system.
- Restart restores the scene's initial bodies, sizes, switches and consumed bubbles. Checkpoints are not implemented.
- A stranded box or a route blocked by an abandoned body does not trigger automatic soft-lock detection. The current recovery is R restart.

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
- Holding W grows it; holding S shrinks it. Current size limits are **0.5 to 2 times** the initial X/Y scale.
- Growth stops when solid geometry blocks the requested size. Grounded resizing preserves the bottom; unsupported resizing preserves the collider center. Trigger hazards are not solid growth blockers: growing a controlled box into a purple water hazard causes death instead.
- Mass remains constant when resized. The current test box has mass **5**.
- Can activate the weight-based plate, an assigned-box held plate, or an assigned-box overhead switch. Their activation and persistence rules differ; see the interactions below.
- Underwater, sinks with added resistance even when unpossessed. Once grounded, horizontal target speed is **70%** of base speed.
- Resizing works underwater; it does not provide buoyancy or active upward swimming.
- **Wind: planned.** The box should be able to move against fan airflow to reach the fan. Full immunity versus strong resistance is **Undecided**.

## 6. Bubble — Xiao

### Air

- Rises automatically through its negative gravity scale; A/D controls horizontal movement.
- W/S provide no air action, and there is no intentional descent control in air.
- The controller caps upward velocity for the controlled bubble; the same controller cap is not currently applied to an unpossessed bubble.

### Water

- Automatic rising is disabled while its center is inside water.
- WASD provides four-direction swimming, including diagonal input.
- Swimming target speed is **130%** of the body's configured base horizontal movement speed. Diagonal input is limited to the same maximum magnitude.
- The controlled bubble accelerates toward the requested velocity and gradually brakes to rest when movement keys are released.
- An unpossessed underwater bubble has zero gravity and water damping: residual motion gradually fades unless another collision or force moves it.
- Exiting water restores its original air physics and upward acceleration. It does not instantly teleport to the surface or erase every component of momentum.

### Popping

- Space pops the controlled bubble in both environments and restores the original star at the bubble's current position. `BubblePopHazard` requests the same queued action through `RequestBubblePop`; a requested pop is processed before a pending possession transfer. If control leaves a bubble by touching another form instead, the abandoned bubble remains in the scene and continues its unpossessed environmental physics.
- The star retains horizontal and downward velocity, while upward velocity is removed so it can begin falling.
- A star released underwater immediately receives its water physics through the ownership refresh.
- The popped bubble is inactive until the scene restarts; it does not respawn automatically at its original location.
- **Wind: planned.** Fan airflow should push bubbles away; force strength and player resistance are **Undecided**.

## 7. Bouncing Ball — Jingyi

### Waiting and possession

- Both Level01 balls are instances of the same reusable `BouncyBall` prefab.
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

## 9. Tutorial interactions

- Opening message: `You are a star.` with Move A/D, Jump Space and Restart R.
- A thin green platform teaches jumping and is visually distinct from the orange possessable box.
- Environmental spikes teach lethal contact and restart.
- First box possession displays `Now you are a StarFormer.` and `Touch special objects to become them.`, with movement, W/S resizing and restart controls.
- The current pressure plate requires mass **3** or greater and a supporting contact within its area. A heavy object merely passing through the trigger in midair is insufficient. Activation disables the assigned door and remains latched until restart.
- Completion requires the assigned controlled box to enter the goal trigger while the door is inactive and the player is alive. This is a tutorial-specific condition, not a rule that every future level must end as a box.
- Completion pauses gameplay and displays `Now get ready to transform!`, `More forms. More possibilities.`, and `Press R to restart.`
- `Return to Main Menu` loads `Mainmenu`, where the player can choose Level 1. Completion does not automatically advance to Level01. The tutorial does not introduce every planned form.

## 10. Level01 interactions and completion

| Object / script | Activation | Persistence |
| --- | --- | --- |
| `PressurePlate_P` / `HeldPressurePlate` | Only the assigned `Box_C`, grounded with its feet touching the sensor area; no mass threshold and no requirement that C remain controlled | Opens `Gate_G` while held; closes when C leaves or the plate is disabled |
| `Switch_H` / `BoxHeightSwitch` | Only the assigned grounded `Box_D` touching the overhead trigger; placement requires growing D to reach it | Opens `Gate_G2` once and stays activated until restart |
| Purple water hazards / `BubblePopHazard` | Contact by the controlled bubble or box | Bubble: consume it and release the star. Box: kill the player and restart after the death delay. Idle bodies are ignored |
| `LevelGoal` | Any living, controlled form enters the goal while `Gate_G2` is inactive | Pauses gameplay and displays completion text; R restarts |

- P and U2 are horizontally offset. The intended solution deliberately leaves C pressing P while transferring to U2. G spans the right-hand ascent exit, giving nearby feedback about whether the plate remains held.
- H has ordinary solid support above it. It detects grounded contact, not a particular key press or explicit size value. G2 is near the final goal, separate from G.
- The goal displays `Level 1 complete!` and `Press R to restart.`. It checks G2's current inactive state, not a recorded history of every puzzle or G's state. The star-only restriction was removed so the bouncing ball can finish directly. `Bubble_U3` and `U3_Canopy` were removed because a final bubble-to-star conversion is no longer required.
- Restart resets both switch types and restores consumed bubbles. There is no automatic Tutorial → Level01 → Level02 transition yet.
- `BouncyBall_Lower` and `BouncyBall_Upper` are working instances of the shared BouncyBall prefab. The old R1/R2 marker transforms remain as inert references only. Upper-route ceiling spikes `(13)`, `(14)` and `(15)` were moved to the upper return floor and rotated upward because the overhead placement made ball traversal too difficult. The developer tested the revised traversal and completion successfully. Passing segment tests is not proof of exhaustive full-route validation.
- Level01 camera regions use `X = 16` to divide the lower-left and lower-right views and `Y = 8.5` to enter the upper view. Lower view sizes are **10**, keeping the green upper floor visible; the upper view size is **5.25**. The camera follows the current controlled form within a bounded range and smooths movement and zoom during transitions.

## 11. Main menu and navigation

- `Mainmenu` is the startup scene. Its title is `Star Former`, with Tutorial, Level 1 and `Level 2 (Coming Soon)` buttons. The Main Camera supplies the solid blue background.
- Mouse clicks on Tutorial and Level 1 load `Tutorial` and `Level01`. The Level 2 button currently has Interactable enabled but no OnClick listeners, so clicking it does not load a scene.
- Tutorial and Level01 each have a top-right `Return to Main Menu` button. `MainMenuUI` and `LevelNavigationUI` restore `Time.timeScale` to 1 before loading the destination. Return navigation is available during gameplay and on completion screens. Loading a scene starts it again rather than resuming prior progress.
- The menu reuses StarPlayer and the shared possession controller. A/D or Left/Right Arrow moves the star, Space jumps when grounded, and R restarts the menu. The star starts above the title and lands on its platform.
- Tutorial, Level 1 and Level 2 buttons become progressively wider toward the bottom. Separate physical platforms align with the top edges of the title and button rectangles, with a ground platform beneath them.
- These are one-way platforms: the star can jump straight up through them and land on top while falling. It does not need to go around a button's edge to ascend. Walking off an edge allows descent.
- The current title spacing is accepted even though the star cannot jump back to the title from Tutorial. Returning to the title is not a menu objective or a requirement for entering a level.
- Landing on a button does not launch its level. Level selection remains a mouse action, independent of the optional star interaction. EventSystem navigation events are disabled so jumping does not submit a selected button.
- The menu Canvas uses Screen Space - Camera with Main Camera. The star renders in front of the buttons, so they do not hide it while it passes through them.
- No additional movement hint or attention animation has been added. The team will test whether new players discover the star interaction before deciding on further guidance.

The enabled build scenes are `Mainmenu` (0), `Tutorial` (1) and `Level01` (2). Reference and sandbox scenes are excluded from the saved build list. There is no automatic level progression or connected Level 2 entry yet.

The developer reported successful tests of menu scene selection, return buttons, star movement and jumping, and the final foreground-rendering change. Teammate usability testing and web-build verification remain separate from these editor tests.

## 12. Planned features and unresolved decisions

- **Wind:** fans are the only planned wind source; there is no separate ambient wind system. Form-specific wind response remains unimplemented.
- **Fan controls, agreed design:** A/D moves and sets facing; Space activates airflow; the fan should not enter water. The intended unattended fan retains its last blowing direction. Hold versus toggle activation and the exact unattended activation rule still need confirmation before implementation.
- **Other planned forms:** a possessable spike wall remains planned; spider wall/ceiling movement and its possible time limit remain tentative. Enemies are planned as lethal, non-possessable actors.
- **Ball route:** the shared ball behavior and both Level01 ball-route layouts are implemented. The revised upper floor-spike section and completion passed developer tests; a final start-to-finish regression remains.
- **Button/gate feedback:** a button and its gate may be outside the same camera view. The team deferred additional visual links or camera feedback for this pass.
- **Level scope:** the current Alpha Level 1 uses Star, Box, Bubble and Bouncing Ball. Level 2 is intended to use all approved forms; spider inclusion depends on its confirmed implementation.
- **Navigation follow-up:** connect the Level 2 placeholder after the team finishes that scene and resolves its camera conflicts. Teammate testing will assess the optional menu star interaction. Checkpoints and automatic scene-to-scene progression are not implemented. Oxygen is not part of the current star water behavior.

Update this document alongside changes to controls, form/environment rules or accepted tuning. Keep proposals marked as planned or Undecided until implemented and verified.
