# Introductory Levels

Beta 0.1.2 was published November 6 at root and `/beta/` via `64b35c6`, with clean source checkout `b93768f`. Navigation, shared Home/Help, IntroFan naming, revised Spider teaching and session discovery are included. Deployment and 14-file hosted comparison passed; see the [release record](Beta-0.1.2.md) for outstanding gameplay/data acceptance.

Last updated: 2026-11-06.

**Historical published release: Beta 0.1.1 (November 3)** at [the Beta URL](https://xtraluck21.github.io/StarFormer/beta/), source `9451511`, gh-pages `e81097e`. It retains the tutorial sequence described below and adds seven-scene analytics plus the Level01 door hint. The November 4 paged help book, animated form demos and Mainmenu adjustments are integrated into main but not this export. The seven live files matched the recorded export on November 5; detailed 0.1.1 gameplay/data acceptance remains separate. The Beta URL now serves 0.1.2. See [the historical release record](publish_instructions.md#beta-011-release-status).

**Historical published release: Alpha 0.1.2 (October 18, 2026)** at [the Alpha URL](https://xtraluck21.github.io/StarFormer/alpha/). Source release configuration is `5650250`, gameplay/UI changes are `9b35f8e`, and the `gh-pages` publication is `5f62980`. The developer reported successful localhost and post-publication tests. These reports do not enumerate every checkpoint, device or alternate route; Alpha 0.1.2 Google Sheets delivery has not been independently verified. The intro sequence and October 17 polish were integrated through PR #10 (`69f3902`) and PR #11 (`989d2d1`); mobile integration followed in PR #14 (`3714a49`).

**Historical Beta 0.1.0 (November 2):** IntroBouncyBall is integrated after IntroBubble, with two-way passages, shorter bounces, brief contextual lessons and aligned terrain. Player/Analytics versions are `beta-0.1.0`; the final package was rebuilt, published and accepted in developer-reported local/online tests (source `3a693ca`, gh-pages `1105970`). In addition, the integrated IntroBubble keeps its rebuilt tank, map hints, return travel to IntroBox and spare Bubble. Controls are **J = Grow, K = Shrink, Q = Pop Bubble**; Q also releases Spider/Fan and F toggles Fan wind. R opens restart choices. Advanced is now formal Level01; its flag continues through IntroSpider and FanTutorial before Level02. See [the release record](publish_instructions.md#beta-010-release-status) for deployment evidence.

## Purpose and progression

Replace the old Tutorial with three short introductions that teach contact possession through experimentation: Box growth/shrink, Bubble air/water control and pop, then BouncyBall bounce/float and low-ceiling rhythm. Spider and Fan tutorials follow Level01.

```text
Navigation → IntroBox → IntroBubble → IntroBouncyBall → Level01 → IntroSpider → IntroFan → Level02
```

Navigation's **Kindergarten** entrance starts IntroBox; **Level 1** starts Level01, **Elementary School** starts IntroSpider and **Level 2** starts Level02. Schools remain optional. The old Tutorial scene stays in the repository but is unchecked in the build list.

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
6. Land as Star, pass beneath the hanging barrier, and jump onto the flag platform. Only the living, controlled, grounded Star opens the exit door. Follow the arrow right through the opening into IntroBouncyBall; no Level up overlay interrupts this passage.

`BackupBubble` waits in the right half of the tank at `(1.95, -1.25, 0)`. A Star that falls back into the water can possess it and swim to the exit again. It stays motionless until first possession and is a single spare, not an unlimited respawn source. The pop hint also works while controlling this Bubble.

Both intro cameras remain fixed. Old route geometry and unused legacy objects remain inactive; the active replacement terrain uses the `IB`-prefixed floors, walls and ceilings plus Ground, retaining the gray background, dark terrain and blue water.

### Return to IntroBox

Walking left through IntroBubble's entrance exit loads IntroBox at its endpoint, controlling BoxB at `(7.8, -2.64)` with size `0.5` and the exit door open. This works even when IntroBubble was opened directly. The return setup is consumed once; it is not a complete snapshot of the previous scene. The player can move back into IntroBox or continue forward to IntroBubble again. A full restart still starts the current scene normally.

## IntroBouncyBall

Scene: [IntroBouncyBall.unity](../Assets/Scenes/IntroBouncyBall.unity).

1. Enter as Star through the left opening. The left boundary is inset from the camera edge, keeping the entrance readable.
2. Touch the glowing BouncyBall; it bounces automatically. This scene alone uses bounce height `3.2` and seconds to apex `0.8`.
3. Cross the water between aligned, equally sized banks. The ball floats instead of bouncing on the surface; returning to land resumes bouncing.
4. Cross the separated floor spikes and enter the low-ceiling section on the forward route. The continuous floor prevents falling out of the map. The previous four-spike dead end is removed; the compressed-bounce lesson is part of progression.
5. Reach the final flag as controlled Star or BouncyBall to show `Entering Level 1...` and load Level01.

`IntroBouncyBallHints` shows short headlines at map-related anchors: `A little more bounce?`, `Born to bounce.`, `On water, we float!`, `Dry land. Bounce time!` and `Small space. Speedy bounces!`. Explanatory subtitles stay hidden; movement keys remain on the map. Spike death displays `Ouch! Spikes aren't for hugs.` during the existing death pause without adding a respawn delay. Low ceilings shorten the bounce cycle through the existing ball physics; this is not a global movement-speed boost.

### Return to IntroBubble

Walking left through IntroBouncyBall's opening returns to IntroBubble as Star at `(8.2, 3.95)` near the flag with its exit door open, even when leaving as BouncyBall. This is a fresh destination scene with one-shot return setup, not a persistent snapshot. The original Star remains active when it is the return body. Walking right can enter the tutorial again; spawning outside the exit trigger avoids an automatic loading loop. Restarting IntroBubble later starts normally in EntryBox with the gate closed.

## IntroSpider and IntroFan updates

Elementary School begins at IntroSpider and continues to IntroFan, then Level02. The saved IntroSpider goal targets `IntroFan`; the renamed scene retains its original GUID. Historical releases/maps still use `FanTutorial`.

Current Spider teaching includes contextual climb prompts, directional wall arrows and a climb-surface lesson. The center prompt changes from `Climb over it` to `No floor, no problem.` after the qualifying climb, with an upward reminder when appropriate. Cake-disguised spikes reveal after contact; the first contact displays `cake is a lie`, while later spike deaths use `you are dead`. The center hint owns this feedback so the separate death caption does not duplicate it. The cake contact memory is session-local, not a persistent achievement.

Gameplay and Navigation Collection Book pages and Closet icons share possession-based discovery for the current launch. Entering as Star or completing a school grants no extra forms; actually controlling an assigned starting host also counts. Scene changes/death/restarts retain discoveries, while a new launch clears them. Star help remains available. Old completion saves are ignored; see [session-only discovery](Beta-0.1.2.md#session-only-shape-discovery). Obsolete Help components with missing button references were removed from IntroBox, IntroBubble and IntroSpider while preserving their configured books and other teaching UI. Focused Editor checks passed for opening, closing, paging and pause restoration; see [verification evidence](AnalyticsTestPlan.md#tutorial-help-cleanup-verification).

## Controls and hints

| Controlled form | Movement | Ability / exit |
| --- | --- | --- |
| Star | A/D or Left/Right Arrow | Space jumps |
| Box | A/D while grounded | Hold J to grow, K to shrink; no jump or manual exit |
| Bubble in air | A/D or Left/Right Arrow | Automatic rise; Q returns to Star |
| Bubble underwater | WASD, including diagonals | Q returns to Star |
| BouncyBall | A/D; automatic land bounce | Floats on water; no manual jump or release |

R opens the restart menu; choose Restart Level for a fresh start, Return to Checkpoint when available, or Cancel. See [CheckpointGuidance.md](CheckpointGuidance.md). The shared top-right Home menu provides Continue, Return to Checkpoint where available, Restart Level and return to Navigation; old standalone menu/restart controls are disabled. Q / Pop exits Bubble; Q / Leave releases Fan/Spider and F toggles Fan wind. Space remains Star jump and SpikeWall fire.

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

- IntroBox ↔ IntroBubble ↔ IntroBouncyBall: passage-based scene loads without a Level up overlay; return travel uses the destination endpoint setup described above.
- IntroBouncyBall → Level01: `Level up!` and `Entering Level 1...`.
- Level01 → IntroSpider: `Level up!` and `Entering Level 2 Intro...`; IntroSpider then continues through IntroFan to Level02.
- The goal validates its destination and starts `LevelTransitionUI` once. The generated screen-space overlay pauses gameplay and disables the player controller for a default 1.5 seconds measured in real time, then clears checkpoints and loads the next scene with normal time scale. It requires no manually placed completion Canvas or TMP labels.
- The backdrop intercepts pointer input and the old UI selection is cleared during the transition. This is an automatic transition, not a completion screen waiting for input. `Next Level Label` and `Transition Seconds` are configurable on the goal component; `Next Scene Name` remains the actual scene name.

## Scene wiring and shared behavior

- Keep `PlayerControl.Initial Body` assigned to the configured, active StarPlayer in edit mode. IntroBox leaves `Starting Host` empty; IntroBubble assigns EntryBox. The controller hides the original Star while hosted and restores it when the Bubble pops.
- Untouched Bubbles remain motionless until first possession. Once activated, they retain their environmental physics after transferring away. Restarting the scene restores fresh dormant Bubbles. Neither IntroBubble BubbleBody nor BackupBubble regenerates during the same run. The old U2 regeneration exception belongs to Level01_Legacy; current Level01 recreates popped Bubbles on scene reload.
- IntroBubble's BubbleBody needs `BodyWaterPhysics`; WaterArea and IBWaterInlet use trigger `BoxCollider2D` / `WaterZone` components to cover the tank and its tall inlet. Waiting no longer depends on a Bubble canopy, while room ceilings still guide an activated Bubble.
- IntroBox uses `IntroFlagGate` for the flag/door and `IntroSceneExit` for loading IntroBubble. IntroBubble uses `IntroFlagGate` with its Star assigned and `requireGroundedBody = true`, then `IntroSceneExit` targeting IntroBouncyBall. Its forward exit stores the return Star/position; IntroBouncyBall's left exit targets IntroBubble with `returnToPreviousExit = true`. `IntroBouncyBallGoal` targets Level01 with label `Level 1` and a 1.5-second transition. `LevelGoal` advances current Level01 to IntroSpider without a gate prerequisite, accepting any living controlled form; its label is `Level 2 Intro` with the same transition duration. G2 belongs to the Legacy layout.
- `GoalFlagVisual.prefab` is visual only. Keep scene-specific goal triggers and controller references separate from the shared flag asset.
- Hint roots stay separate from moving bodies. `BubbleMovementHint` has Air, Water, ExitBubble, StarMove, StarJump, ContinueRight and BubbleIntroduction modes with optional edit-mode preview; `BoxGrowHint` creates its full visuals at runtime. Moving a body-dependent hint may require adjusting Activation Area Offset to preserve where it activates. ContinueRight only needs Flag Gate; Continue Arrow Width affects that mode alone.
- On IntroBox's `TutorialCanvas`, disable the legacy `TutorialIntroUI` component and enable `IntroFormHintUI`. Assign local PlayerControl, TutorialTitle and TutorialInstructions references; choose Box for IntroBox. In IntroBox, keep both TMP objects active and clear obsolete saved instructional text; IntroFormHintUI owns their runtime text. IntroBubble instead keeps both form-message scripts disabled and its old title/instruction objects inactive.
- Shared Home uses the local player controller and returns to Navigation; retain R as a separate restart-choice shortcut. Do not re-enable legacy standalone buttons over the new Home/Help layout.

For script details, see [ScriptGuide.md](ScriptGuide.md). For all forms, see [GameplaySpec.md](GameplaySpec.md).

## Verification and remaining work

Historical November 2 integration: developer playtests accepted the BouncyBall route, hints, terrain alignment and two-way Bubble passage. After the Fan hint texture fix (`3365e29`), the final package was rebuilt with the synchronized versions/template and published as Beta 0.1.0. The developer confirmed local and hosted tests passed. The historical 0.1.0 Level01 production records are visible in the dashboard; Version 9 subsequently resolved historical map matching. Historical 0.1.1 raw-row/map acceptance remains separate from the current 0.1.2 follow-ups and broader device coverage.

Earlier November 1 Level01-promotion verification (before BouncyBall integration): developer playtests and isolated Unity checks passed Mainmenu entries, the then-direct IntroBubble → formal Level01 transition, Level01 completion in all four forms → IntroSpider, then FanTutorial → Level02. Legacy scene/meta preservation and current Level01 checkpoint/Home behavior were also verified. These earlier checks are distinct from the November 2 Beta release acceptance above; detailed mobile coverage remains a follow-up.

**Historical Alpha verification (October 14–18):** the developer confirmed box transfer/shrinking, dormant Bubble activation, air/water movement, visual hints/outline, E exit, the grounded-Star flag, and the full menu-to-Level02 flow. The October 17 follow-up also confirmed the palette, contextual Star prompts, form/subtitle changes including Box-to-Box transfer, larger subtitles, IntroBubble messages, current-scene Restart buttons, the thicker gate-open arrow, and completion transitions. Script compilation checks passed during implementation. Those were the original Editor results. The integrated Alpha 0.1.2 export has since passed developer-reported localhost and online tests; the accepted IntroBubble preview retains transparency and the original camera.

October 31 source verification: Unity compilation passed for the shared Q pop / J grow / K shrink controls and current hint logic. The developer accepted the rebuilt terrain and contextual hints during iterative Editor checks. The spare-Bubble pop-hint fix compiled; a complete browser/mobile regression was not recorded at that checkpoint. No publication was claimed by the October 31 checks; see the subsequent Beta release evidence above.

Next browser/device regression: test IntroBubble ↔ IntroBouncyBall in both directions, the BouncyBall water/land/low-ceiling lessons and spike-death hint, and its final Level01 transition. Verify both Bubble instances can reach the upper platform and trigger the Q hint, recovery after a premature pop, return travel in both directions, J/K hints in both intros, and R menu behavior. The spare Bubble is consumable; early popping before reaching it or consuming both bubbles can still require Restart. Air/water hints remain assigned to the original Bubble; only the pop hint follows any possessed Bubble. Gather new-player readability feedback and broaden device/aspect-ratio and simultaneous-touch coverage. Early Bubble popping or a stranded body can still require Restart; automatic soft-lock recovery and a dedicated underwater-hazard lesson are not implemented. The current mobile layout is accepted for Alpha 0.1.2; further redesign is deferred.

### October 17 implementation record

| Commit | Scope |
| --- | --- |
| `9f220a6` | Intro background and terrain palette |
| `9769a3a` | Contextual Star/form hints and current-scene Restart buttons |
| `850e465` | Box-to-Box message and subtitle readability |
| `7fc77d5` | IntroBubble UI wiring, gate-open arrow with adjustable thickness, and Level 1/2 completion transitions |

PR #11 integrated this polish into main. PR #14 integrated mobile controls, and `9b35f8e` records the Alpha 0.1.2 layout changes. The recorded Alpha 0.1.2 export was published as `5f62980`; see the release record in [ScriptGuide.md](ScriptGuide.md#4-web-build-and-github-pages-release).
