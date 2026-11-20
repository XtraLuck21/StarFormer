# Introductory Levels

**Current source (November 19, 2026):** the three Kindergarten scenes now use the carry/dock lesson, renewable Bubble backup, woodland theme and player-controlled graduation described below. These changes are not a new published build; Level01 art and the Elementary/Level02 layouts are outside this branch.

Historical Beta 0.1.2 was published November 6 at root and `/beta/` via `64b35c6`, with clean source checkout `b93768f`. Navigation, shared Home/Help, IntroFan naming, revised Spider teaching and session discovery are included. Deployment and 14-file hosted comparison passed; see the [release record](Beta-0.1.2.md) for outstanding gameplay/data acceptance.

Touch controls reviewed November 16, 2026.

**Historical published release: Beta 0.1.1 (November 3)** at [the Beta URL](https://xtraluck21.github.io/StarFormer/beta/), source `9451511`, gh-pages `e81097e`. It retains the tutorial sequence described below and adds seven-scene analytics plus the Level01 door hint. The November 4 paged help book, animated form demos and Mainmenu adjustments are integrated into main but not this export. The seven live files matched the recorded export on November 5; detailed 0.1.1 gameplay/data acceptance remains separate. The Beta URL has since served later releases; this paragraph records the 0.1.1/0.1.2 comparison. See [the historical release record](publish_instructions.md#beta-011-release-status).

**Historical published release: Alpha 0.1.2 (October 18, 2026)** at [the Alpha URL](https://xtraluck21.github.io/StarFormer/alpha/). Source release configuration is `5650250`, gameplay/UI changes are `9b35f8e`, and the `gh-pages` publication is `5f62980`. The developer reported successful localhost and post-publication tests. These reports do not enumerate every checkpoint, device or alternate route; Alpha 0.1.2 Google Sheets delivery has not been independently verified. The intro sequence and October 17 polish were integrated through PR #10 (`69f3902`) and PR #11 (`989d2d1`); mobile integration followed in PR #14 (`3714a49`).

**Historical Beta 0.1.0 (November 2):** IntroBouncyBall is integrated after IntroBubble, with two-way passages, shorter bounces, brief contextual lessons and aligned terrain. Player/Analytics versions are `beta-0.1.0`; the final package was rebuilt, published and accepted in developer-reported local/online tests (source `3a693ca`, gh-pages `1105970`). In addition, the integrated IntroBubble keeps its rebuilt tank, map hints, return travel to IntroBox and spare Bubble. Controls are **J = Grow, K = Shrink, Q = Pop Bubble**; Q also releases Spider/Fan and F toggles Fan wind. R opens restart choices. Advanced is now formal Level01; its flag continues through IntroSpider and FanTutorial before Level02. See [the release record](publish_instructions.md#beta-010-release-status) for deployment evidence.

## Purpose and progression

Replace the old Tutorial with three short introductions that teach contact possession through experimentation: Box growth/shrink, Bubble air/water control and pop, then BouncyBall bounce/float and low-ceiling rhythm. Spider and Fan tutorials follow Level01.

```text
Navigation → IntroBox → IntroBubble → IntroBouncyBall → Level01 → IntroSpider → IntroFan → Level02
```

Navigation's **Kindergarten** entrance starts IntroBox; **Level 1** starts Level01, **Elementary School** starts IntroSpider and **Level 2** starts Level02. Schools remain optional. The old Tutorial scene stays in the repository but is unchecked in the build list.

## IntroBox

Current source on `gold-polish-kindergarten` replaces the former two-box route with a three-box carrying lesson. See [the layout and verification notes](IntroBoxCarryPolish.md) and [English concept map](IntroBox-Carry-Map-English.png). This source change has not been published.

1. Begin as Star, practice A/D or arrow-key movement and Space over a low step.
2. A low, tall-sided entry tunnel makes BoxA the first unavoidable possession. The Star cannot jump over A into the later hosts.
3. Move A right into BoxB. BoxC already rests on B, so the existing possession contact locks prevent an immediate second transfer.
4. Carry C right to the receiving ledge. A scene-local support helper matches C's horizontal velocity only while it rests on controlled B; normal possession rules stay in charge.
5. Stop at the ledge and hold K to shrink B. C's forward overhang leaves it supported by the fixed ledge. A visible gap and a short stable-separation check advance the hint.
6. Hold J to regrow B into C. Control changes to C through ordinary contact possession. Move C right and shrink through the low passage to the flag.
7. Controlled C opens the exit door. Continue right into IntroBubble; no Level up overlay interrupts the passage. Returning from IntroBubble restores small C near this flag with the gate open.

English lessons are state-driven world-space callouts near the controlled body/docking area; the old top-center IntroFormHintUI is disabled in this scene. Touch mode substitutes graphical joystick, Jump, Grow and Shrink cues. Dropped or prematurely possessed cargo resets the carrying portion to B+C with a nearby explanation, without replaying the opening jump. R and Home keep the existing full restart/menu behavior. Boxes still cannot jump or manually release Star; their ordinary size limits and shared contact-lock rules are unchanged.

## IntroBubble

Scene: [IntroBubble.unity](../Assets/Scenes/IntroBubble.unity).

1. Enter from the left as EntryBox, inheriting the actual world width of the Box that crossed IntroBox's exit. The entry camera matches the outgoing camera scale and the floor's screen height. The Box begins just inside the left passage with its feet on the floor; holding right continues into the room. Direct entry/restart uses a 0.45-unit Box, matching fully shrunken BoxC. This is a one-shot handoff to a fresh scene host, not persistent scene storage.
2. Hold Grow beneath the waiting Bubble and touch it to take possession. The camera then smoothly pans and widens to the established full-room view over 1.5 seconds, with movement and physics uninterrupted.
3. Rise automatically in the left air shaft. Steer right along the solid ceiling and enter the tall left side of the rectangular water tank. Water meets the ceiling without an air gap.
4. Swim down, pass beneath the hanging central divider, then swim right and up. Solid floors, walls and the tank roof constrain the route to the right-side exit shaft.
5. Leave the water and rise to the upper ceiling. Move right above the safe landing floor before using Pop. The current Pop hint region also overlaps the right water shaft, so its appearance alone does not guarantee a safe landing; see the known follow-ups below.
6. Land as Star, pass beneath the hanging barrier, and jump onto the flag platform. Only the living, controlled, grounded Star opens the exit door. Follow the arrow right through the opening into IntroBouncyBall; no Level up overlay interrupts this passage.

`BackupBubble` waits to the left of the underwater divider at `(0.1, -1.35, 0)`, within jumping reach of Star from the tank floor. Every pop immediately creates a fresh dormant Bubble at this same position, without a use limit. The scene-specific `IntroBubbleBackup` prefab preserves the original pale-blue color, outline, size and water physics. Both swimming and pop hints work with replacement instances.

IntroBox follows the controlled body. IntroBubble holds its matched entry framing while controlling EntryBox, then keeps the original fixed overview after the possession reveal. Old route geometry and unused legacy objects remain inactive; the active replacement terrain uses the `IB`-prefixed floors, walls and ceilings plus Ground, with shared woodland surface art over the existing solids and blue water. The art adds no collision geometry.

### November 19 polish review

The air lesson now contains only `Bubbles float up automatically in air.` on three lines and the original graphical A/D keys with horizontal arrows. It remains fixed on the dark wall. The A/D keys and arrows sit 0.3 units lower to separate them from the text. The fixed water lesson activates for whichever Bubble is controlled and is positioned above-left of the backup spawn.

Desktop Editor play verified first possession, the simplified air lesson, Star jumping into the backup from the tank floor, and two consecutive backup pop/recovery cycles. One pop happened at the spawn and another after swimming beneath the divider and rising through the exit shaft; both replacements appeared at the original spawn and could be possessed again. The water lesson also appeared on the replacements. Touch-device verification remains pending.

Follow-ups, not fixed in this pass:
- The intended safe-landing Pop sequence above is not fully enforced by the current hint area. In Editor play, Pop appeared while the Bubble was still over the right water shaft; pressing Q there dropped Star back into the tank. Guide right toward the landing first, then show the local Pop prompt over safe ground.
- The repeatable backup covers recovery inside the tank. Popping the original Bubble before reaching the water can still require Restart. Feedback mentions accidental pops requiring a restart and slow Bubble movement, but generally does not identify IntroBubble specifically; the explicitly Level 2 report is outside this polish scope.

### Box-to-Bubble continuity verification

Desktop Unity play checked actual IntroBox exit crossings with fully shrunken BoxC and a Box enlarged at the exit. Both retained their apparent and world size on arrival, with the floor at the same screen height. The enlarged incoming Box passed the entrance lintel, reached the first Bubble and possessed it by growing. The camera revealed the full route while the Bubble rose, and the revised text/key spacing remained clear. Direct scene entry also started as the small Box at the left passage. The entrance lintel now clears the full possible incoming Box width (0.45–1.8 units); the floor extends downward to fill the lower entry framing. Browser/mobile verification remains pending.

### Return to IntroBox

Walking left through IntroBubble's entrance exit loads IntroBox at its endpoint, controlling BoxC at `(13.1, -0.78)` with resize multiplier `0.5` (half its authored base scale) and the exit door open. This works even when IntroBubble was opened directly. The return setup is consumed once; it is not a complete snapshot of the previous scene. The player can move back into IntroBox or continue forward to IntroBubble again. A full restart still starts the current scene normally.

## IntroBouncyBall

Scene: [IntroBouncyBall.unity](../Assets/Scenes/IntroBouncyBall.unity).

1. Enter as Star through the left opening and touch the glowing BouncyBall. The existing graphical movement keys appear near the first lesson only after the assigned ball is possessed (`showForStarInArea = false`); they remain hidden while controlling Star.
2. Use the automatic high bounce to climb the 2.2-unit ledge. This scene retains bounce height `3.2` and seconds to apex `0.8`; shared ball tuning is unchanged.
3. Cross a short 2.1-unit water pool with level banks. A canopy brings the ball down to the surface for a brief floating experience. Leaving water restores automatic bouncing without another text lesson. A thin surface edge marks the blue water; floating uses the ball’s bottom-center position, so side overlap alone cannot stop a bounce on the solid bank.
4. Drop onto the continuous floor and pass through the safe low-ceiling section. There are no spikes inside this section. Local NoFriction terrain colliders prevent wall rubbing from suppressing the introductory climb.
5. In the open area after the ceiling, observe the single spike, bounce over it and reach the final flag. The goal opens the Kindergarten graduation screen and waits for a player choice; see [completion feedback](#completion-feedback).

`IntroBouncyBallHints` uses fixed map anchors and repeatable horizontal regions, with boundary hysteresis to avoid flicker. Returning to an area shows its lesson again; there are no one-shot timers or forced tutorial pauses. The entry says `Touch the ball. Reach the high ledge!`, then `You bounce high automatically!` after possession. The water lesson says `A little floating break.` with a floating-ball/wave diagram. The low roof says `Lower ceiling. More bounces per second.` with two animated vertical bounces that differ in height and frequency, not horizontal speed. The final area says `Watch the spike! Bounce over it.` with a jump arc. Spike death displays `Ouch! Spikes aren't for hugs.` during the existing death pause without adding a respawn delay. Explanatory subtitles remain hidden. Form colors and graphical controls are retained; the environment uses the woodland theme below. The entry text and arc share the shifted anchor `(-5.35, 3.7)`, clearing the left wall.

Desktop Unity Play Mode validation (November 19, 2026): possession and high-ledge traversal, water floating/land recovery, low-ceiling passage, returning to earlier hint regions, spike-death feedback/restart, and successful flag transition into Level01 were exercised. The shoreline regression also passed four real-physics cases: left/right solid banks still bounce despite side overlap with water, and left/right blue-water positions float without bouncing. Browser/mobile validation of this revision remains pending.

### Return to IntroBubble

Walking left through IntroBouncyBall's opening returns to IntroBubble as Star at `(8.2, 3.95)` near the flag with its exit door open, even when leaving as BouncyBall. This is a fresh destination scene with one-shot return setup, not a persistent snapshot. The original Star remains active when it is the return body. Walking right can enter the tutorial again; spawning outside the exit trigger avoids an automatic loading loop. Restarting IntroBubble later starts normally in EntryBox with the gate closed.

## IntroSpider and IntroFan updates

Elementary School begins at IntroSpider and continues to IntroFan, then Level02. The saved IntroSpider goal targets `IntroFan`; the renamed scene retains its original GUID. Historical releases/maps still use `FanTutorial`.

Current Spider teaching includes contextual climb prompts, directional wall arrows and a climb-surface lesson. The center prompt changes from `Climb over it` to `No floor, no problem.` after the qualifying climb, with an upward reminder when appropriate. Cake-disguised spikes reveal after contact; the first contact displays `cake is a lie`, while later spike deaths use `you are dead`. The center hint owns this feedback so the separate death caption does not duplicate it. The cake contact memory is session-local, not a persistent achievement.

Gameplay and Navigation Collection Book pages and Closet icons share possession-based discovery for the current launch. Entering as Star or completing a school grants no extra forms; actually controlling an assigned starting host also counts. Scene changes/death/restarts retain discoveries, while a new launch clears them. Star help remains available. Old completion saves are ignored; see [session-only discovery](Beta-0.1.2.md#session-only-shape-discovery). Obsolete Help components with missing button references were removed from IntroBox, IntroBubble and IntroSpider while preserving their configured books and other teaching UI. Focused Editor checks passed for opening, closing, paging and pause restoration; see [verification evidence](AnalyticsTestPlan.md#tutorial-help-cleanup-verification).

## Controls and hints

The following bindings and keycap descriptions apply to desktop play. The touch lessons are listed below.

| Controlled form | Movement | Ability / exit |
| --- | --- | --- |
| Star | A/D or Left/Right Arrow | Space jumps |
| Box | A/D while grounded | Hold J to grow, K to shrink; no jump or manual exit |
| Bubble in air | A/D or Left/Right Arrow | Automatic rise; Q returns to Star |
| Bubble underwater | WASD, including diagonals | Q returns to Star |
| BouncyBall | A/D; automatic land bounce | Floats on water; no manual jump or release |

R opens the restart menu; choose Restart Level for a fresh start, Return to Checkpoint when available, or Cancel. See [CheckpointGuidance.md](CheckpointGuidance.md). The shared top-right Home menu provides Continue, Return to Checkpoint where available, Restart Level and return to Navigation; old standalone menu/restart controls are disabled. Q / Pop exits Bubble; Q or right click leaves Fan. F or left click toggles its wind. Spider moves along surfaces until a handoff. Space remains Star jump and SpikeWall fire.

- **Star:** boxed A/D appears in the opening movement region; boxed SPACE and a jump arc appear near the low step. These prompts require control of the assigned Star and disappear after possession.
- **Box:** `BoxGrowHint` also gives EntryBox a growth reminder beneath the Bubble.
- **Air:** after possession, `Bubbles float up automatically in air.` appears on three lines above boxed A/D and horizontal arrows on the central dark wall beside the ascent. There is no additional title, paragraph or floating icon. Touch mode substitutes the horizontal joystick cue. The lesson stays fixed on the wall without a card background or forced pause.
- **Water:** boxed WASD, directional arrows and `Move freely underwater` sit inside the left half of the water at `(-1.15, 0)`, above-left of the backup. Activation Area Size `(7, 10)` and Offset `(2.025, -0.25)` preserve coverage while moving the graphic. `useControlledBubble` enables the lesson for the original, backup and all replacement Bubbles.
- **Exit:** the Q key and right mouse button icons appear above `Pop Bubble / Return to Star` at `(4.5, 4.1)`. The hint stays visible across the upper exit chamber while the player controls either Bubble out of water (activation size `(7, 3)`, offset `(1.75, -0.6)`). It hides after popping. The player can also pop outside this area.
- **Continuation:** IntroBox uses `ContinueRight` with its `IntroFlagGate` reference. The arrow appears once the gate opens, including after return travel from IntroBubble. ContinueRight does not use a body or activation area.
- **Readability:** use the shared woodland background and dark soil while preserving the pale-blue Bubble, dark-blue outline and blue water. World control hints stay fixed on the map; they do not follow the camera or body.

### Touch lessons by scene

| Scene | What the player sees in touch mode |
| --- | --- |
| IntroBox | The idle joystick introduces movement. The step lesson shows Jump and highlights the right-hand button. Box size lessons use the matching Grow and Shrink icons. |
| IntroBubble | Box resize uses its action icon. The Bubble lesson uses a horizontal joystick cue in air and a multidirectional cue underwater. The upper landing shows Pop for either controlled Bubble. |
| IntroBouncyBall | The movement lesson shows a joystick with horizontal arrows. Steering, water floating and automatic bounce use the same left thumb. |
| IntroSpider | The controls lesson uses the joystick for travel along connected surfaces. Climb and hazard messages remain contextual. |
| IntroFan | The possession lesson shows the same wind and exit icons as the right-hand buttons, labeled “Wind on/off” and “Leave Fan”. |

These lessons follow the same Show Mode as the controls. The joystick and action buttons are built at runtime inside the screen safe area. Adjust their shared layout in MobileFormControls. IntroBubble reveals its fixed overview at `(0.5, 0.75, -10)` with orthographic size 6 after the incoming Box transfers to a Bubble. Its entry framing inherits the outgoing view (direct-entry fallback size 5.3). IntroBox follows the player at camera Y 0.5 with size 5.3. [Form bindings and Editor preview](GameplaySpec.md#2-controls)

### Top-center form messages

IntroBubble disables `IntroFormHintUI`, TutorialTitle and TutorialInstructions; it uses the map hints above. The Bubble rows below describe the retained script capability, not visible messages in the rebuilt scene. IntroBox now uses original visual keycaps and short local contact-rule hints; its top-center messages are disabled.

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
- IntroBouncyBall graduation: `LEVEL UP!`, a backpack Star, `Kindergarten Complete!`, and `You've graduated! / Ready for a new challenge?`. A roughly one-second unscaled fade/pop, Star hop and sparkle reveal runs while gameplay is paused, then the page stays still indefinitely. `Next Level` loads Level01; the top-right house stays above the backdrop, reads `Main Menu` on hover/focus and loads Navigation directly. Keyboard up/down navigation connects these two buttons. Collection Book and gameplay remain blocked.
- Level01 → IntroSpider: `Level up!` and `Entering Level 2 Intro...`; IntroSpider then continues through IntroFan to Level02.
- Graduation validates its destination and starts `LevelTransitionUI` once. It records completion at the flag, prevents duplicate overlays/loads, clears checkpoint sessions on either chosen exit and restores normal time in the destination. Selecting Main Menu after completion does not report abandonment. `graduateStar` links the imported transparent character sprite; text and buttons are native Unity UI. No manually placed completion Canvas is required.
- Other timed level transitions retain their existing labels and real-time delays (normally 1.5 seconds). Their backdrop intercepts pointer input and clears old selection. IntroBouncyBall no longer exposes `Next Level Label` or `Transition Seconds`; it uses `Next Scene Name` and `Graduate Star`. Its graduation UI owns the wait, with Home explicitly raised above the backdrop.

## Woodland theme and readability

IntroBox, IntroBubble and IntroBouncyBall reuse [IntroBouncyBallForest assets and provenance](../Assets/Art/IntroBouncyBallForest/manifest.json): muted sage/teal woodland, dark olive soil, narrow grass edges and subdued blue pool depth. The background stays mid-value so the yellow Star and existing form colors remain distinct. No decorative motion arcs are baked into the background; the visible arcs belong to instructional diagrams.

The backdrop is a 2048 × 1152 image; soil is 512 × 512, grass 256 × 64 and pool depth 256 × 256. Imports use bilinear filtering, no mipmaps and full-rect sprites. Terrain overlays/grass are children of existing solids and gates, with no extra colliders. Grass is drawn inside the existing top edge. Water depth follows existing water bounds. Box/Bubble use `WoodlandCameraBackdrop` to fit the existing camera after movement/zoom; it moves the artwork, not the camera. This is SpriteRenderer decoration, not a Tilemap conversion.

IntroBox's enlarged local writing is black on scenery and gold on walls; resize/control diagrams remain graphical. Bubble uses dark ink on scenery and pale gold on dark walls. BouncyBall uses dark/pale-gold lesson ink and contrasting diagram rims. Keycap drawing order keeps A/D visible above the new terrain art. Form colors, spike silhouettes and their gameplay identities are retained.

The caption-free square Collection Book presentation now applies to all non-Navigation scenes using `SceneCollectionBookUI`, including these tutorials; see [shared book presentation](GameplaySpec.md#2-controls). Level01 also reuses this woodland art, with its own title wall and hint palette; see [Level01 presentation](level-1.md#woodland-presentation).

## Scene wiring and shared behavior

- Keep `PlayerControl.Initial Body` assigned to the configured, active StarPlayer in edit mode. IntroBox leaves `Starting Host` empty; IntroBubble assigns EntryBox. The controller hides the original Star while hosted and restores it when the Bubble pops.
- Untouched Bubbles remain motionless until first possession. Once activated, they retain their environmental physics after transferring away. Restarting the scene restores fresh dormant Bubbles. IntroBubble BubbleBody does not regenerate during the same run; BackupBubble uses BubbleRespawnOnPop to recreate itself at its authored position after every pop, including subsequent replacements. The old U2 regeneration exception belongs to Level01_Legacy; current Level01 renews both entrance-water Bubbles after every Pop, while its upper-ascent Bubble still requires scene reload; see [Level01 routes](level-1.md#playable-route).
- IntroBubble's BubbleBody needs `BodyWaterPhysics`; WaterArea and IBWaterInlet use trigger `BoxCollider2D` / `WaterZone` components to cover the tank and its tall inlet. Waiting no longer depends on a Bubble canopy, while room ceilings still guide an activated Bubble.
- IntroBox uses `IntroFlagGate` for the flag/door and `IntroSceneExit` for loading IntroBubble. IntroBubble uses `IntroFlagGate` with its Star assigned and `requireGroundedBody = true`, then `IntroSceneExit` targeting IntroBouncyBall. Its forward exit stores the return Star/position; IntroBouncyBall's left exit targets IntroBubble with `returnToPreviousExit = true`. `IntroBouncyBallGoal` targets Level01 through the graduation button and references the backpack Star sprite. `LevelGoal` advances current Level01 to IntroSpider without a gate prerequisite, accepting any living controlled form; its label is `Level 2 Intro` with the same transition duration. G2 belongs to the Legacy layout.
- `GoalFlagVisual.prefab` is visual only. Keep scene-specific goal triggers and controller references separate from the shared flag asset.
- Hint roots stay separate from moving bodies. `BubbleMovementHint` has Air, Water, ExitBubble, StarMove, StarJump, ContinueRight and BubbleIntroduction modes with optional edit-mode preview; `BoxGrowHint` creates its full visuals at runtime. Moving a body-dependent hint may require adjusting Activation Area Offset to preserve where it activates. ContinueRight only needs Flag Gate; Continue Arrow Width affects that mode alone.
- In IntroBox and IntroBubble, keep legacy `TutorialIntroUI` and `IntroFormHintUI` disabled. IntroBox uses `IntroBoxCarryLesson`, its local hint label, wall sign and visual controls; IntroBubble uses map hints. Do not re-enable the retained top-center title/subtitle objects as the active lesson.
- Shared Home uses the local player controller and returns to Navigation; retain R as a separate restart-choice shortcut. Do not re-enable legacy standalone buttons over the new Home/Help layout.

For script details, see [ScriptGuide.md](ScriptGuide.md). For all forms, see [GameplaySpec.md](GameplaySpec.md).

## Verification and remaining work

The developer accepted the November 19 woodland, hint, continuity and graduation changes. Final Editor checks verified goal pause, no automatic advance during a 15-second hold, graduation artwork/text bounds, Next Level, and the Home pointer-hit/Navigation path with time restored. See [Kindergarten acceptance checks](AnalyticsTestPlan.md#kindergarten-polish-source-acceptance). The following dated release checks are historical.

Historical November 2 integration: developer playtests accepted the BouncyBall route, hints, terrain alignment and two-way Bubble passage. After the Fan hint texture fix (`3365e29`), the final package was rebuilt with the synchronized versions/template and published as Beta 0.1.0. The developer confirmed local and hosted tests passed. The historical 0.1.0 Level01 production records are visible in the dashboard; Version 9 subsequently resolved historical map matching. Historical 0.1.1 raw-row/map acceptance remains separate from the current 0.1.2 follow-ups and broader device coverage.

Earlier November 1 Level01-promotion verification (before BouncyBall integration): developer playtests and isolated Unity checks passed Mainmenu entries, the then-direct IntroBubble → formal Level01 transition, Level01 completion in all four forms → IntroSpider, then FanTutorial → Level02. Legacy scene/meta preservation and current Level01 checkpoint/Home behavior were also verified. These earlier checks are distinct from the November 2 Beta release acceptance above; detailed mobile coverage remains a follow-up.

**Historical Alpha verification (October 14–18):** the developer confirmed box transfer/shrinking, dormant Bubble activation, air/water movement, visual hints/outline, E exit, the grounded-Star flag, and the full menu-to-Level02 flow. The October 17 follow-up also confirmed the palette, contextual Star prompts, form/subtitle changes including Box-to-Box transfer, larger subtitles, IntroBubble messages, current-scene Restart buttons, the thicker gate-open arrow, and completion transitions. Script compilation checks passed during implementation. Those were the original Editor results. The integrated Alpha 0.1.2 export has since passed developer-reported localhost and online tests; the accepted IntroBubble preview retains transparency and the original camera.

October 31 source verification: Unity compilation passed for the shared Q pop / J grow / K shrink controls and current hint logic. The developer accepted the rebuilt terrain and contextual hints during iterative Editor checks. The spare-Bubble pop-hint fix compiled; a complete browser/mobile regression was not recorded at that checkpoint. No publication was claimed by the October 31 checks; see the subsequent Beta release evidence above.

Next browser/device regression: test IntroBubble ↔ IntroBouncyBall in both directions, the BouncyBall water/land/low-ceiling lessons and spike-death hint, and both graduation exits (Next Level and Home). Verify both Bubble instances can reach the upper platform and trigger the Q hint, recovery after a premature pop, return travel in both directions, J/K hints in both intros, and R menu behavior. Repeat the backup pop/recovery loop; each popped backup should leave exactly one dormant replacement at its original spawn. Early popping before reaching the tank can still require Restart. The desktop air lesson retains its assigned host, while the water lesson, touch Bubble lessons and exit lesson can use the currently controlled Bubble. Gather new-player readability feedback and broaden device/aspect-ratio and simultaneous-touch coverage. Recovery outside the tank and a dedicated underwater-hazard lesson remain unimplemented. On a phone, repeat the route using the joystick, resize buttons and Pop.

### October 17 implementation record

| Commit | Scope |
| --- | --- |
| `9f220a6` | Intro background and terrain palette |
| `9769a3a` | Contextual Star/form hints and current-scene Restart buttons |
| `850e465` | Box-to-Box message and subtitle readability |
| `7fc77d5` | IntroBubble UI wiring, gate-open arrow with adjustable thickness, and Level 1/2 completion transitions |

PR #11 integrated this polish into main. PR #14 integrated mobile controls, and `9b35f8e` records the Alpha 0.1.2 layout changes. The recorded Alpha 0.1.2 export was published as `5f62980`; see the release record in [ScriptGuide.md](ScriptGuide.md#4-web-build-and-github-pages-release).
