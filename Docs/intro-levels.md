# Introductory Levels

Last updated: 2026-10-14.

**Status:** basic gameplay and the complete progression passed developer-reported Unity Play Mode tests. Implemented on `intro-levels` and integrated through [PR #10](https://github.com/XtraLuck21/StarFormer/pull/10), merge `69f3902`. Some additional hints and presentation polish remain. A WebGL release containing this work has not yet been published.

## Purpose and progression

Replace the old Tutorial with two short introductions that teach contact possession through experimentation. First show why both growing and shrinking are useful, then reuse the Box to introduce a Bubble and its different air/water controls.

```text
Mainmenu → IntroBox → IntroBubble → Level01 → Level02
```

The menu's **Introductory Levels** button starts IntroBox; direct Level 1 and Level 2 entries remain available. The old Tutorial scene stays in the repository but is unchecked in the build list.

## IntroBox

Scene: [IntroBox.unity](../Assets/Scenes/IntroBox.unity).

1. Begin as Star and use horizontal movement and a jump to pass the low step.
2. Touch BoxA to possess it. Move into the growth area and hold W to reach the elevated BoxB; contact transfers control upward. The Star cannot bypass this transfer with a direct jump in the tested layout.
3. Move BoxB over the gap and hold S to shrink and drop safely.
4. Walk through the low tunnel. The flag opens the small exit door only for controlled BoxB; continue right through a separate trigger to load IntroBubble.

Boxes cannot jump or exit to Star. Fixed growth/shrink outlines, outward/inward arrows and boxed W/S prompts appear only while the corresponding Box is controlled and inside its activation region. Leave lateral clearance for growth; a Box pressed against a wall cannot expand through it.

## IntroBubble

Scene: [IntroBubble.unity](../Assets/Scenes/IntroBubble.unity).

1. Start controlling EntryBox in a short tunnel that visually continues the prior scene. This is a fresh scene instance, not BoxB physically carried between scenes.
2. Walk out, grow beneath the waiting Bubble and transfer into it on contact.
3. Steer through the air passage with A/D while the activated Bubble rises automatically.
4. Enter the water from the side. Use WASD to swim down, across beneath the hanging divider, and up the other side.
5. Leave the water and automatically rise again. Move above the right platform, press E to pop the Bubble, and land as Star.
6. Walk to the green flag. Only the living, controlled, grounded Star can trigger the goal and load Level01.

Both intro cameras are currently fixed. The right platform provides a landing area before the flag; reaching it as a Bubble does not finish the level.

## Controls and hints

| Controlled form | Movement | Ability / exit |
| --- | --- | --- |
| Star | A/D or Left/Right Arrow | Space jumps |
| Box | A/D while grounded | Hold W to grow, S to shrink; no jump or manual exit |
| Bubble in air | A/D or Left/Right Arrow | Automatic rise; E returns to Star |
| Bubble underwater | WASD, including diagonals | E returns to Star |

R restarts the whole current scene. Return-to-menu buttons remain available. E is a Bubble exit, not a universal exit key; Fan and Spider still use their existing Space behavior.

- **Box:** `BoxGrowHint` also gives EntryBox a growth reminder beneath the Bubble.
- **Air:** `BubbleMovementHint` displays boxed A/D and horizontal arrows. The earlier small-circle/up-arrow graphic was removed; an additional automatic-rise explanation is still pending.
- **Water:** boxed WASD sits directly below the divider. Its activation area covers the pool, so it appears on entry rather than only when the player reaches the icon.
- **Exit:** boxed E appears when the controlled Bubble reaches the safe platform area; it disappears after returning to Star. E remains usable elsewhere too.
- **Readability:** Bubble movement hints use orders 0/1, below the Bubble at 2 and its Star marker at 10. The Bubble has a pale-blue fill and dark-blue outline; the border changes neither its outer size nor its collision shape.

## Scene wiring and shared behavior

- Keep `PlayerControl.Initial Body` assigned to the configured, active StarPlayer in edit mode. IntroBox leaves `Starting Host` empty; IntroBubble assigns EntryBox. The controller hides the original Star while hosted and restores it when the Bubble pops.
- Untouched Bubbles remain motionless until first possession. Once activated, they retain their environmental physics after transferring away. Restarting the scene restores fresh dormant Bubbles; popped Bubbles do not regenerate during the same run. This is shared behavior, not limited to IntroBubble.
- IntroBubble's BubbleBody needs `BodyWaterPhysics`; WaterArea uses a trigger `BoxCollider2D` and `WaterZone`. Waiting no longer depends on a Bubble canopy, while room ceilings still guide an activated Bubble.
- IntroBox uses `IntroFlagGate` for the flag/door and `IntroSceneExit` for loading IntroBubble. IntroBubble uses `IntroStarGoal` with `Next Scene Name = Level01`. `LevelGoal` then advances Level01 to Level02 after G2 opens, accepting any controlled form.
- `GoalFlagVisual.prefab` is visual only. Keep scene-specific goal triggers and controller references separate from the shared flag asset.
- Hint roots stay separate from moving bodies. `BubbleMovementHint` has Air, Water and ExitBubble modes with optional edit-mode preview; `BoxGrowHint` creates its full visuals at runtime. Moving a hint may require adjusting Activation Area Offset to preserve where it activates.

For script details, see [ScriptGuide.md](ScriptGuide.md). For all forms, see [GameplaySpec.md](GameplaySpec.md).

## Verification and remaining work

The developer confirmed box transfer/shrinking, dormant Bubble activation, air/water movement, visual hints/outline, E exit, the grounded-Star flag, and the full menu-to-Level02 flow. Script compilation checks also passed during implementation. These are editor results, not a claim that a new WebGL build has been tested or published.

Remaining work: add the missing teaching hints (including clearer automatic-rise guidance), review readability/framing with new players, and test a new WebGL export before publication. Early Bubble popping or a stranded body can still require R; automatic soft-lock recovery is not implemented. The current intro route does not add a dedicated underwater-hazard lesson. Camera-follow redesign and mobile controls are not part of this completed pass.
