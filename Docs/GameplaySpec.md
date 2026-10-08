# Gameplay Specification

Last updated: 2026-10-07.

This document records current controls, implemented gameplay and unresolved design decisions. For script responsibilities and integration points, see [ScriptGuide.md](ScriptGuide.md).

Baseline: commit `d5c8970` on `level-1`, checked against source and saved scenes. The developer has reported successful manual testing of Tutorial, the three-form water sandbox and the implemented Level01 segments. Level01 has not been validated as a complete route with bouncing balls. This document does not claim a new automated test or Unity playtest was performed during drafting.

## 1. Current scope

| Feature | Status |
| --- | --- |
| Star, box and bubble; automatic contact possession | Implemented and manually tested |
| Box resizing; pressure plate opening a door | Implemented and manually tested |
| Bubble popping and return to the star | Implemented and manually tested |
| Water behavior for all three current forms | Implemented and manually tested in WaterPlayground |
| Tutorial instructions, lethal spikes, death feedback and completion | Implemented and manually tested in Tutorial |
| R restart; automatic restart after death / vertical out-of-bounds | Implemented |
| Level01 held plate, overhead height switch, bubble-pop hazards and completion UI | Implemented; developer-reported segment tests passed |
| Level01 complete route, including bouncing-ball traversal | In progress; ball integration and full-route testing remain |
| Fan-generated wind and form-specific wind response | Not integrated into the current shared controller |
| Rolling/bouncing ball, possessable spike wall, enemies and spider | Not integrated into the current shared controller |

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

- Left/Right Arrow also work as alternatives to A/D in the current controller. Up/Down Arrow are not implemented as W/S alternatives.
- There is no manual possession key. Contact with an eligible body automatically requests a transfer.
- There is no general key for exiting a box. Returning directly to the star is currently the bubble's pop action.
- R can restart during gameplay, the death pause and the Tutorial and Level01 completion screens.

## 3. Shared possession and failure rules

### Possession

- Only eligible possessable bodies can be taken over. Ground, ordinary platforms, doors, water regions and environmental spikes are not possessable.
- Touching another eligible body transfers control to that existing body at its position; it does not create a copy at the player's previous position.
- The original star is hidden after entering a host. A star marker identifies the controlled host.
- An abandoned box or bubble remains in the scene with its physical behavior and can obstruct a route or participate in interactions.
- The previous body is temporarily blocked as a possession target until the bodies separate, preventing immediate back-and-forth transfers.
- Contact possession remains active underwater. Popping above a box can therefore lead to automatic box possession when the released star lands on it.

### Death and restart

- A hazard using `SpikeHazard` kills the currently controlled body in any implemented form.
- A hazard using `BubblePopHazard` instead pops only the controlled bubble and leaves the star at that position. It does not kill the star or box and does not pop idle bubbles. Level01 currently uses purple markers for these hazards.
- Contact by an unpossessed body does not itself kill the player.
- Death pauses gameplay, then reloads the whole current scene after the configured delay. Tutorial, WaterPlayground and Level01 currently save a delay of **2 seconds**. R bypasses the wait.
- Tutorial displays `You died.` and restart instructions. The sandbox scenes and Level01 do not currently contain this death-feedback UI; Level01 has completion text only.
- Automatic vertical failure occurs when the entire controlled collider is below `minimumHeight` or above `maximumHeight`; Tutorial and WaterPlayground save **-10 / 8**, while Level01 saves **-12 / 16**. This is not a general left/right map-boundary system.
- Restart restores the scene's initial bodies, sizes, switches and consumed bubbles. Checkpoints are not implemented.
- A stranded box or a route blocked by an abandoned body does not trigger automatic soft-lock detection. The current recovery is R restart.

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
- Growth stops when solid geometry blocks the requested size. Grounded resizing preserves the bottom; unsupported resizing preserves the collider center.
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

- Space pops the bubble in both environments and restores the original star at the bubble's current position. `BubblePopHazard` requests the same queued action through `RequestBubblePop`; a requested pop is processed before a pending possession transfer.
- The star retains horizontal and downward velocity, while upward velocity is removed so it can begin falling.
- A star released underwater immediately receives its water physics through the ownership refresh.
- The popped bubble is inactive until the scene restarts; it does not respawn automatically at its original location.
- **Wind: planned.** Fan airflow should push bubbles away; force strength and player resistance are **Undecided**.

## 7. Water rules and current tuning

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
| Box minimum / maximum size | 0.5 / 2 | Multipliers relative to the initial scale |
| Box resize speed | 1 | Size multiplier change per second |

These are target values: damping and collisions can change achieved motion. Inspector settings saved in the scene take precedence over code defaults. The star water speed C# default and both saved scenes now agree at **0.3**. New scenes must still be configured deliberately.

### Saved body physics

| Body | Mass | Original gravity scale | Original linear damping | Underwater gravity scale | Underwater linear damping |
| --- | --- | --- | --- | --- | --- |
| Star | 1 | 3 | 0 | 3 | 2 |
| Box | 5 | 3 | 0 | 3 | 2 |
| Bubble | 0.2 | -0.3 | 2 | 0 | 0 controlled; 2 unpossessed |

The controlled bubble uses controller acceleration/braking instead of rigidbody damping while swimming. These are current prototype values, not final balance commitments.

## 8. Tutorial interactions

- Opening message: `You are a star.` with Move A/D, Jump Space and Restart R.
- A thin green platform teaches jumping and is visually distinct from the orange possessable box.
- Environmental spikes teach lethal contact and restart.
- First box possession displays `Now you are a StarFormer.` and `Touch special objects to become them.`, with movement, W/S resizing and restart controls.
- The current pressure plate requires mass **3** or greater and a supporting contact within its area. A heavy object merely passing through the trigger in midair is insufficient. Activation disables the assigned door and remains latched until restart.
- Completion requires the assigned controlled box to enter the goal trigger while the door is inactive and the player is alive. This is a tutorial-specific condition, not a rule that every future level must end as a box.
- Completion pauses gameplay and displays `Now get ready to transform!`, `More forms. More possibilities.`, and `Press R to restart.`
- Transition into a new Alpha Level 1 is not implemented yet. The tutorial does not introduce every planned form.

## 9. Level01 interactions and completion

| Object / script | Activation | Persistence |
| --- | --- | --- |
| `PressurePlate_P` / `HeldPressurePlate` | Only the assigned `Box_C`, grounded with its feet touching the sensor area; no mass threshold and no requirement that C remain controlled | Opens `Gate_G` while held; closes when C leaves or the plate is disabled |
| `Switch_H` / `BoxHeightSwitch` | Only the assigned grounded `Box_D` touching the overhead trigger; placement requires growing D to reach it | Opens `Gate_G2` once and stays activated until restart |
| Purple water hazards / `BubblePopHazard` | Contact by the controlled bubble | Consumes that bubble; releases the star; nearby spare U1b provides another opportunity |
| `LevelGoal` | Living, controlled star enters the goal while `Gate_G2` is inactive | Pauses gameplay and displays completion text; R restarts |

- P and U2 are horizontally offset. The intended solution deliberately leaves C pressing P while transferring to U2. G spans the right-hand ascent exit, giving nearby feedback about whether the plate remains held.
- H has ordinary solid support above it. It detects grounded contact, not a particular key press or explicit size value. G2 is near the final goal, separate from G.
- The goal displays `Level 1 complete!` and `Press R to restart.`. It checks G2's current inactive state, not a recorded history of every puzzle or G's state.
- Restart resets both switch types and restores consumed bubbles. There is no automatic Tutorial → Level01 → Level02 transition yet.
- R1/R2 are placement markers, not working bouncing-ball actors. The current upper return floor is continuous; the planned ball hazards and staggered gaps remain unfinished. Passing segment tests is not proof that the final intended route is complete.

## 10. Planned features and unresolved decisions

- **Wind:** fans are the only planned wind source; there is no separate ambient wind system. Form-specific wind response remains unimplemented.
- **Fan controls, agreed design:** A/D moves and sets facing; Space activates airflow; the fan should not enter water. The intended unattended fan retains its last blowing direction. Hold versus toggle activation and the exact unattended activation rule still need confirmation before implementation.
- **Other planned forms:** bouncing ball and a possessable spike wall; spider wall/ceiling movement and its possible time limit remain tentative. Enemies are planned as lethal, non-possessable actors. Their scripts are absent from this baseline.
- **Ball clarification:** bouncing should stop after transferring into another possessable body, not after every ordinary terrain collision.
- **Level scope:** planned Alpha Level 1 uses Star, Box, Bubble and Bouncing Ball. Level 2 is intended to use all approved forms; spider inclusion depends on its confirmed implementation.
- **Future work:** checkpoints and scene-to-scene progression are not implemented; final full-route validation remains pending. Oxygen is not part of the current star water behavior.

Update this document alongside changes to controls, form/environment rules or accepted tuning. Keep proposals marked as planned or Undecided until implemented and verified.
