# Introductory Levels

Last updated: 2026-10-18.

**Current release: Alpha 0.1.2 (October 18, 2026)** at [the Alpha URL](https://xtraluck21.github.io/StarFormer/alpha/). Source release configuration is `5650250`, gameplay/UI changes are `9b35f8e`, and the `gh-pages` publication is `5f62980`. The developer reported successful localhost and post-publication tests. These reports do not enumerate every checkpoint, device or alternate route; Alpha 0.1.2 Google Sheets delivery has not been independently verified. The intro sequence and October 17 polish were integrated through PR #10 (`69f3902`) and PR #11 (`989d2d1`); mobile integration followed in PR #14 (`3714a49`).

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
4. Walk through the low tunnel. The flag opens the small exit door only for controlled BoxB. A right-pointing arrow then appears beside the exit; continue right through a separate trigger to load IntroBubble. This scene-to-scene continuation does not show a Level up overlay.

Boxes cannot jump or exit to Star. Fixed growth/shrink outlines, outward/inward arrows and boxed W/S prompts appear only while the corresponding Box is controlled and inside its activation region. Leave lateral clearance for growth; a Box pressed against a wall cannot expand through it.

## IntroBubble

Scene: [IntroBubble.unity](../Assets/Scenes/IntroBubble.unity).

1. Start controlling EntryBox in a short tunnel that visually continues the prior scene. This is a fresh scene instance, not BoxB physically carried between scenes.
2. Walk out, grow beneath the waiting Bubble and transfer into it on contact.
3. Steer through the air passage with A/D while the activated Bubble rises automatically.
4. Enter the water from the side. Use WASD to swim down, across beneath the hanging divider, and up the other side.
5. Leave the water and automatically rise again. Move above the right platform, press E to pop the Bubble, and land as Star.
6. Walk to the green flag. Only the living, controlled, grounded Star can trigger the goal. A short `Level up!` / `Entering Level 1...` overlay appears before Level01 loads.

Both intro cameras are currently fixed. The right platform provides a landing area before the flag; reaching it as a Bubble does not finish the level.

## Controls and hints

| Controlled form | Movement | Ability / exit |
| --- | --- | --- |
| Star | A/D or Left/Right Arrow | Space jumps |
| Box | A/D while grounded | Hold W to grow, S to shrink; no jump or manual exit |
| Bubble in air | A/D or Left/Right Arrow | Automatic rise; E returns to Star |
| Bubble underwater | WASD, including diagonals | E returns to Star |

R and the **Restart (R)** button restart the whole current scene and clear its checkpoint session. IntroBubble restarts at EntryBox in IntroBubble, not at IntroBox. Each intro places Restart below Return to Main Menu; both buttons use that scene's own `LevelNavigationUI`. E / Pop exits Bubble; E / Leave also exits Fan and Spider in levels that contain those forms. Space remains Star jump and SpikeWall fire, not a release key.

- **Star:** boxed A/D appears in the opening movement region; boxed SPACE and a jump arc appear near the low step. These prompts require control of the assigned Star and disappear after possession.
- **Box:** `BoxGrowHint` also gives EntryBox a growth reminder beneath the Bubble.
- **Air:** `BubbleMovementHint` displays boxed A/D and horizontal arrows. The earlier small-circle/up-arrow graphic stays removed; the top subtitle now explains automatic rise: `You float up. You steer sideways.`
- **Water:** boxed WASD sits directly below the divider. Its activation area covers the pool, so it appears on entry rather than only when the player reaches the icon.
- **Exit:** boxed E appears when the controlled Bubble reaches the safe platform area; it disappears after returning to Star. E remains usable elsewhere too.
- **Continuation:** IntroBox uses `ContinueRight` mode with its `IntroFlagGate` reference. The arrow is hidden at runtime until the gate opens, then remains visible until the scene changes or restarts. This mode does not use a body or activation area. Its saved line width is 0.08, with a larger arrowhead and Sorting Order 20.
- **Readability:** both intros use a warm-gray camera background (`#ADA7A7`) and dark terrain, while retaining distinct form/water colors. Saved top titles use font size 48 and subtitles 32, in dark blue. Bubble movement hints use orders 0/1, below the Bubble at 2 and its Star marker at 10. The Bubble has a pale-blue fill and dark-blue outline; the border changes neither its outer size nor its collision shape.

### Alpha 0.1.2 mobile layout

Both intro mobile Canvases use Screen Space - Overlay and Scale With Screen Size, reference `1920 × 1080`, match `0.5`. Show Mode is saved as AutoDetect. Set AlwaysShow before Play for Editor testing and restore AutoDetect before saving a release. Button visibility follows the controlled form; controls are not all shown simultaneously in play.

IntroBox keeps `160 × 160` buttons and opaque backgrounds. Only Left, Right and Jump were lowered to Y `100`: Left X `223`, Right X `575` (bottom-left anchors), Jump X `-296` (bottom-right anchor). Other action positions retain their existing scene values.

IntroBubble uses `120 × 120` buttons, pivot `(0.5, 0.5)`, and white Image backgrounds with alpha `0.35`. Only the Image background is faded; labels keep their opacity, and the full `120 × 120` button rectangle remains clickable. Direction keys keep their two-dimensional arrangement:

| Button | Anchor | Pos X | Pos Y |
| --- | --- | --- | --- |
| Left | Bottom-left `(0, 0)` | 160 | 160 |
| Right | Bottom-left `(0, 0)` | 440 | 160 |
| Up | Bottom-left `(0, 0)` | 300 | 230 |
| Down | Bottom-left `(0, 0)` | 300 | 90 |
| Jump / Pop / Fire | Bottom-right `(1, 0)` | -300 | 160 |
| Grow | Bottom-right `(1, 0)` | -300 | 230 |
| Shrink | Bottom-right `(1, 0)` | -300 | 90 |
| Blow | Bottom-right `(1, 0)` | -440 | 160 |
| Leave | Bottom-right `(1, 0)` | -160 | 160 |

Buttons are siblings of MobileControls under Canvas; moving MobileControls alone does not reposition them. IntroBubble retains its original fixed camera at `(0.5, 0.75, -10)`, orthographic Size `6`. IntroBox retains `(-3, 0, -10)`, Size `7`. The wider/lower IntroBubble camera experiment was reverted before release. No dedicated bottom control strip or camera-follow redesign was added. Transparency reduces visual obstruction but does not prevent fingers covering gameplay.

### Top-center form messages

`IntroFormHintUI` updates the title/subtitle from the controlled form and Bubble water state. Titles remain visible; subtitles hold for 4 seconds and fade over 0.75 seconds, using scaled game time. A new qualifying state restarts that timer.

| State | Title | Subtitle |
| --- | --- | --- |
| IntroBox starts as Star | You are a star. | Small feet. Big jumps. |
| Star enters BoxA | You are a box. | New body? Just bump into it. |
| BoxA transfers to BoxB | You are a box. | New box. Same little star. |
| IntroBubble starts in EntryBox | You are a box. | None |
| Possess Bubble in air | You are a bubble. | You float up. You steer sideways. |
| Bubble enters water | You are a bubble. | Underwater, every direction is yours. |
| Bubble leaves water | You are a bubble. | Back in the air. Up you go! |
| Return to Star after hosting | You are a star again. | Time to land. |

Box-to-Box feedback detects a changed body reference in the Box sequence; staying in the same Box does not repeatedly replay the subtitle. There is no persistent “follow the flag” text in the scene.

### Completion feedback

- IntroBubble → Level01: `Level up!` and `Entering Level 1...`.
- Level01 → Level02: `Level up!` and `Entering Level 2...`.
- The goal validates its destination and starts `LevelTransitionUI` once. The generated screen-space overlay pauses gameplay and disables the player controller for a default 1.5 seconds measured in real time, then clears checkpoints and loads the next scene with normal time scale. It requires no manually placed completion Canvas or TMP labels.
- The backdrop intercepts pointer input and the old UI selection is cleared during the transition. This is an automatic transition, not a completion screen waiting for input. `Next Level Label` and `Transition Seconds` are configurable on the goal component; `Next Scene Name` remains the actual scene name.

## Scene wiring and shared behavior

- Keep `PlayerControl.Initial Body` assigned to the configured, active StarPlayer in edit mode. IntroBox leaves `Starting Host` empty; IntroBubble assigns EntryBox. The controller hides the original Star while hosted and restores it when the Bubble pops.
- Untouched Bubbles remain motionless until first possession. Once activated, they retain their environmental physics after transferring away. Restarting the scene restores fresh dormant Bubbles. The intro Bubble does not regenerate during the same run; Level01 Bubble_U2 is an explicit exception through BubbleRespawnOnPop.
- IntroBubble's BubbleBody needs `BodyWaterPhysics`; WaterArea uses a trigger `BoxCollider2D` and `WaterZone`. Waiting no longer depends on a Bubble canopy, while room ceilings still guide an activated Bubble.
- IntroBox uses `IntroFlagGate` for the flag/door and `IntroSceneExit` for loading IntroBubble. IntroBubble uses `IntroStarGoal` with `Next Scene Name = Level01`, label `Level 1`, and default transition 1.5 seconds. `LevelGoal` advances Level01 to Level02 after G2 opens, accepting any controlled form; its label is `Level 2` with the same default transition duration.
- `GoalFlagVisual.prefab` is visual only. Keep scene-specific goal triggers and controller references separate from the shared flag asset.
- Hint roots stay separate from moving bodies. `BubbleMovementHint` has Air, Water, ExitBubble, StarMove, StarJump and ContinueRight modes with optional edit-mode preview; `BoxGrowHint` creates its full visuals at runtime. Moving a body-dependent hint may require adjusting Activation Area Offset to preserve where it activates. ContinueRight only needs Flag Gate; Continue Arrow Width affects that mode alone.
- On each `TutorialCanvas`, disable the legacy `TutorialIntroUI` component and enable `IntroFormHintUI`. Assign local PlayerControl, TutorialTitle and TutorialInstructions references; choose Box for IntroBox and Bubble for IntroBubble. Keep both TMP objects active, and clear saved instructional text instead of deleting the text component. The new script owns runtime text; the legacy component must not also write to it.
- Each Restart button calls the local Canvas's `LevelNavigationUI.RestartLevel()`; replace a copied ReturnToMainMenu event rather than leaving both callbacks attached.

For script details, see [ScriptGuide.md](ScriptGuide.md). For all forms, see [GameplaySpec.md](GameplaySpec.md).

## Verification and remaining work

The developer confirmed box transfer/shrinking, dormant Bubble activation, air/water movement, visual hints/outline, E exit, the grounded-Star flag, and the full menu-to-Level02 flow. The October 17 follow-up also confirmed the palette, contextual Star prompts, form/subtitle changes including Box-to-Box transfer, larger subtitles, IntroBubble messages, current-scene Restart buttons, the thicker gate-open arrow, and completion transitions. Script compilation checks passed during implementation. Those were the original Editor results. The integrated Alpha 0.1.2 export has since passed developer-reported localhost and online tests; the accepted IntroBubble preview retains transparency and the original camera.

Remaining work: gather new-player readability feedback and broaden device/aspect-ratio and simultaneous-touch coverage. Early Bubble popping or a stranded body can still require Restart; automatic soft-lock recovery and a dedicated underwater-hazard lesson are not implemented. The current mobile layout is accepted for Alpha 0.1.2; further redesign is deferred.

### October 17 implementation record

| Commit | Scope |
| --- | --- |
| `9f220a6` | Intro background and terrain palette |
| `9769a3a` | Contextual Star/form hints and current-scene Restart buttons |
| `850e465` | Box-to-Box message and subtitle readability |
| `7fc77d5` | IntroBubble UI wiring, gate-open arrow with adjustable thickness, and Level 1/2 completion transitions |

PR #11 integrated this polish into main. PR #14 integrated mobile controls, and `9b35f8e` records the Alpha 0.1.2 layout changes. The current export is published as `5f62980`; see the release record in [ScriptGuide.md](ScriptGuide.md#4-web-build-and-github-pages-release).
