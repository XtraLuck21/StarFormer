# Introductory Levels

Last updated: 2026-10-31.

**Current release: Alpha 0.1.2 (October 18, 2026)** at [the Alpha URL](https://xtraluck21.github.io/StarFormer/alpha/). Source release configuration is `5650250`, gameplay/UI changes are `9b35f8e`, and the `gh-pages` publication is `5f62980`. The developer reported successful localhost and post-publication tests. These reports do not enumerate every checkpoint, device or alternate route; Alpha 0.1.2 Google Sheets delivery has not been independently verified. The intro sequence and October 17 polish were integrated through PR #10 (`69f3902`) and PR #11 (`989d2d1`); mobile integration followed in PR #14 (`3714a49`).

**Current source (October 31):** the `intro-bubble` branch rebuilds IntroBubble and its world-space hints, adds return travel to the IntroBox exit and a spare underwater Bubble, and uses **J = Grow, K = Shrink, Q = Pop Bubble**. R opens the restart menu. These source changes are not a new published build; the last recorded release remains Alpha 0.1.2.

## Purpose and progression

Replace the old Tutorial with two short introductions that teach contact possession through experimentation. First show why both growing and shrinking are useful, then reuse the Box to introduce a Bubble and its different air/water controls.

```text
Mainmenu → IntroBox → IntroBubble → Level01 → Level02
```

The menu's **Introductory Levels** button starts IntroBox; direct Level 1 and Level 2 entries remain available. The old Tutorial scene stays in the repository but is unchecked in the build list.

## IntroBox

Scene: [IntroBox.unity](../Assets/Scenes/IntroBox.unity).

1. Begin as Star and use horizontal movement and a jump to pass the low step.
2. Touch BoxA to possess it. Move into the growth area and hold J to reach the elevated BoxB; contact transfers control upward. The Star cannot bypass this transfer with a direct jump in the tested layout.
3. Move BoxB over the gap and hold K to shrink and drop safely.
4. Walk through the low tunnel. The flag opens the small exit door only for controlled BoxB. A right-pointing arrow then appears beside the exit; continue right through a separate trigger to load IntroBubble. This scene-to-scene continuation does not show a Level up overlay.

Boxes cannot jump or exit to Star. Fixed growth/shrink outlines, outward/inward arrows and boxed J/K prompts appear only while the corresponding Box is controlled and inside its activation region. Leave lateral clearance for growth; a Box pressed against a wall cannot expand through it.

## IntroBubble

Scene: [IntroBubble.unity](../Assets/Scenes/IntroBubble.unity).

1. Start controlling EntryBox in the bottom-left entry room. The opening and floor extension to the left edge visually connect this room to IntroBox; the body is a fresh scene instance.
2. Grow with J beneath the waiting Bubble and touch it to take possession.
3. Rise automatically in the left air shaft. Use A/D to move right along the solid ceiling and enter the tall left side of the rectangular water tank. Water meets the ceiling without an air gap.
4. Swim down with S, pass beneath the hanging central divider, then swim right and up with WASD. Solid floors, walls and the tank roof constrain the route to the right-side exit shaft.
5. Leave the water and rise to the upper ceiling. Move right above the safe landing floor before pressing Q to pop; the pop hint only appears in that landing region, not immediately upon surfacing.
6. Land as Star, pass beneath the hanging barrier, and jump onto the flag platform. Only the living, controlled, grounded Star triggers the short `Level up!` / `Entering Level 1...` transition to Level01.

`BackupBubble` waits in the right half of the tank at `(1.95, -1.25, 0)`. A Star that falls back into the water can possess it and swim to the exit again. It stays motionless until first possession and is a single spare, not an unlimited respawn source. The pop hint also works while controlling this Bubble.

Both intro cameras remain fixed. Old route geometry and unused legacy objects remain inactive; the active replacement terrain uses the `IB`-prefixed floors, walls and ceilings plus Ground, retaining the gray background, dark terrain and blue water.

### Return to IntroBox

Walking left through IntroBubble's entrance exit loads IntroBox at its endpoint, controlling BoxB at `(7.8, -2.64)` with size `0.5` and the exit door open. This works even when IntroBubble was opened directly. The return setup is consumed once; it is not a complete snapshot of the previous scene. The player can move back into IntroBox or continue forward to IntroBubble again. A full restart still starts the current scene normally.

## Controls and hints

| Controlled form | Movement | Ability / exit |
| --- | --- | --- |
| Star | A/D or Left/Right Arrow | Space jumps |
| Box | A/D while grounded | Hold J to grow, K to shrink; no jump or manual exit |
| Bubble in air | A/D or Left/Right Arrow | Automatic rise; Q returns to Star |
| Bubble underwater | WASD, including diagonals | Q returns to Star |

R opens the restart menu; choose Restart Level for a fresh start, Return to Checkpoint when available, or Cancel. See [CheckpointGuidance.md](CheckpointGuidance.md). IntroBubble hides its standalone Restart button and uses a compact **Main Menu** button. IntroBox retains its explicit full-restart button. Q / Pop exits Bubble; E / Leave remains the Fan/Spider control. Space remains Star jump and SpikeWall fire.

- **Star:** boxed A/D appears in the opening movement region; boxed SPACE and a jump arc appear near the low step. These prompts require control of the assigned Star and disappear after possession.
- **Box:** `BoxGrowHint` also gives EntryBox a growth reminder beneath the Bubble.
- **Air:** after possession, `You are a Bubble` and boxed A/D with horizontal arrows appear on the central dark wall beside the ascent. These are plain high-contrast text/icons, without a card background. The separate Rise in Air card was removed.
- **Water:** boxed WASD, directional arrows and `Move freely underwater` sit inside the left half of the water at `(-0.85, -0.9)`. Activation Area Size `(7, 10)` and Offset `(1.725, 0.65)` preserve coverage while moving the graphic. This hint remains bound to the original BubbleBody.
- **Exit:** boxed Q and black `Pop Bubble / Return to Star` text appear at `(4.5, 4.1)` when the currently possessed Bubble is out of water and inside the safe landing activation area (Size `(1.6, 3)`, Offset `(1.4, -0.6)`). It uses the current body, so BackupBubble also qualifies. It hides after popping. Q is still usable outside this area; the hint is guidance, not an input lock.
- **Continuation:** IntroBox uses `ContinueRight` with its `IntroFlagGate` reference. The arrow appears once the gate opens, including after return travel from IntroBubble. ContinueRight does not use a body or activation area.
- **Readability:** retain the warm-gray background, dark terrain, pale-blue Bubble with dark-blue outline, and blue water. World hints stay fixed on the map; they do not follow the camera or body.

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

IntroBubble disables `IntroFormHintUI`, TutorialTitle and TutorialInstructions; it uses the map hints above. The Bubble rows below describe the retained script capability, not visible messages in the rebuilt scene. IntroBox continues using the top-center messages.

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
- Untouched Bubbles remain motionless until first possession. Once activated, they retain their environmental physics after transferring away. Restarting the scene restores fresh dormant Bubbles. Neither IntroBubble BubbleBody nor BackupBubble regenerates during the same run; Level01 Bubble_U2 is an explicit exception through BubbleRespawnOnPop.
- IntroBubble's BubbleBody needs `BodyWaterPhysics`; WaterArea and IBWaterInlet use trigger `BoxCollider2D` / `WaterZone` components to cover the tank and its tall inlet. Waiting no longer depends on a Bubble canopy, while room ceilings still guide an activated Bubble.
- IntroBox uses `IntroFlagGate` for the flag/door and `IntroSceneExit` for loading IntroBubble. IntroBubble uses `IntroStarGoal` with `Next Scene Name = Level01`, label `Level 1`, and default transition 1.5 seconds. `LevelGoal` advances Level01 to Level02 after G2 opens, accepting any controlled form; its label is `Level 2` with the same default transition duration.
- `GoalFlagVisual.prefab` is visual only. Keep scene-specific goal triggers and controller references separate from the shared flag asset.
- Hint roots stay separate from moving bodies. `BubbleMovementHint` has Air, Water, ExitBubble, StarMove, StarJump, ContinueRight and BubbleIntroduction modes with optional edit-mode preview; `BoxGrowHint` creates its full visuals at runtime. Moving a body-dependent hint may require adjusting Activation Area Offset to preserve where it activates. ContinueRight only needs Flag Gate; Continue Arrow Width affects that mode alone.
- On IntroBox's `TutorialCanvas`, disable the legacy `TutorialIntroUI` component and enable `IntroFormHintUI`. Assign local PlayerControl, TutorialTitle and TutorialInstructions references; choose Box for IntroBox. In IntroBox, keep both TMP objects active and clear obsolete saved instructional text; IntroFormHintUI owns their runtime text. IntroBubble instead keeps both form-message scripts disabled and its old title/instruction objects inactive.
- Each active standalone Restart button calls the local Canvas's `LevelNavigationUI.RestartLevel()`; replace a copied ReturnToMainMenu event rather than leaving both callbacks attached.

For script details, see [ScriptGuide.md](ScriptGuide.md). For all forms, see [GameplaySpec.md](GameplaySpec.md).

## Verification and remaining work

**Historical Alpha verification (October 14–18):** the developer confirmed box transfer/shrinking, dormant Bubble activation, air/water movement, visual hints/outline, E exit, the grounded-Star flag, and the full menu-to-Level02 flow. The October 17 follow-up also confirmed the palette, contextual Star prompts, form/subtitle changes including Box-to-Box transfer, larger subtitles, IntroBubble messages, current-scene Restart buttons, the thicker gate-open arrow, and completion transitions. Script compilation checks passed during implementation. Those were the original Editor results. The integrated Alpha 0.1.2 export has since passed developer-reported localhost and online tests; the accepted IntroBubble preview retains transparency and the original camera.

October 31 source verification: Unity compilation passed for the shared Q pop / J grow / K shrink controls and current hint logic. The developer accepted the rebuilt terrain and contextual hints during iterative Editor checks. The spare-Bubble pop-hint fix compiled; a complete browser/mobile regression of this branch has not been recorded. No new publication is claimed.

Remaining work: verify both Bubble instances can reach the upper platform and trigger the Q hint, recovery after a premature pop, return travel in both directions, J/K hints in both intros, and R menu behavior. The spare Bubble is consumable; early popping before reaching it or consuming both bubbles can still require Restart. Air/water hints remain assigned to the original Bubble; only the pop hint follows any possessed Bubble. Gather new-player readability feedback and broaden device/aspect-ratio and simultaneous-touch coverage. Early Bubble popping or a stranded body can still require Restart; automatic soft-lock recovery and a dedicated underwater-hazard lesson are not implemented. The current mobile layout is accepted for Alpha 0.1.2; further redesign is deferred.

### October 17 implementation record

| Commit | Scope |
| --- | --- |
| `9f220a6` | Intro background and terrain palette |
| `9769a3a` | Contextual Star/form hints and current-scene Restart buttons |
| `850e465` | Box-to-Box message and subtitle readability |
| `7fc77d5` | IntroBubble UI wiring, gate-open arrow with adjustable thickness, and Level 1/2 completion transitions |

PR #11 integrated this polish into main. PR #14 integrated mobile controls, and `9b35f8e` records the Alpha 0.1.2 layout changes. The recorded Alpha 0.1.2 export was published as `5f62980`; see the release record in [ScriptGuide.md](ScriptGuide.md#4-web-build-and-github-pages-release).
