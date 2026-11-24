# Level 1

**Current source:** `gold-level-1-polish` extends the Kindergarten woodland theme to Level01, adds two entrance-water routes, updates reminders and checkpoint 2, and makes the lower-door hint replayable. Subsequent polish adds the acceptance-letter completion page, revised SAVE timing/contact, direct RUIN activation, saved book-icon cleanup and reminders behind player forms. Gameplay changes through `c57d2aa` are covered by the [source acceptance record](AnalyticsTestPlan.md#level01-polish-source-acceptance). These changes are not part of the recorded Gold 0.1.0 export.

The November 6 Beta 0.1.2 release was published at root and `/beta/` via `64b35c6`; source checkout at publication: `b93768f`. Deployment and hosted file checks passed; browser gameplay acceptance remains separate. [Release record](Beta-0.1.2.md).

Touch controls reviewed November 16, 2026. Promotion baseline: `8834a4d`; recorded published Beta 0.1.1 source: `9451511` (gh-pages `e81097e`, November 3).

## Scene identity and progression

[Level01.unity](../Assets/Scenes/Level01.unity) is the former Advanced layout. Open this scene for further Level 1 work; there is no separate active `Level01_Advanced.unity`.

The previous gameplay layout and its original scene GUID are retained as [Level01_Legacy.unity](../Assets/Scenes/Level01_Legacy.unity). The later shared Collection Book cleanup updated its entry UI; it is no longer a byte-for-byte scene backup. It is excluded from builds. `Level01_Reference` is a separate, older design reference. Neither backup is the current playable map; the old G/G2, held-plate, ball-checkpoint and U2-regeneration layout must not be used as the specification for this scene.

```text
Navigation → IntroBox → IntroBubble → IntroBouncyBall → Level01 → IntroSpider → IntroFan → Level02
```

- Navigation’s Level 1 entrance targets `Level01` through NavigationEntrance; both contact and click paths run the entry feedback before loading.
- IntroBouncyBall's Star/BouncyBall goal opens graduation; choosing Next Level loads `Level01`; IntroBubble first opens a passage to that tutorial.
- Level01 is at enabled build index 4, after IntroBouncyBall; IntroSpider, IntroFan and Level02 follow at indices 5, 6 and 7. Its GUID is the former Advanced GUID. The old Level01 GUID belongs to the disabled Legacy backup.
- The endpoint flag uses `LevelGoal`, with no required gate, destination `IntroSpider`, and the Elementary School admission presentation enabled. Any living controlled form can finish.
- Completion pauses gameplay and opens a woodland-green/gold acceptance letter: `LEVEL UP!`, `Level 1 Complete!`, `Dear Star,`, `You're accepted!`, and `Welcome to Elementary School`. A yellow Star with a teal notebook and pencil emerges from a cream envelope. Native UI text and buttons remain separate from the transparent character sprite.
- A roughly one-second unscaled envelope opening and Star hop settles into a still page. There is no automatic advance: `Next Level →` loads IntroSpider; the top-right Home stays clickable above the backdrop from the start of the reveal and returns directly to Navigation. Its tooltip reads `Main Menu`. Both exits clear checkpoint sessions and restore normal time; completion is recorded once at the flag. Keyboard up/down connects the two buttons. The saved timed-transition fields are only a fallback for other LevelGoal instances.

## Playable route

| Section | Current behavior |
| --- | --- |
| Entrance | Start as Star without a forced Box transfer or READY button. The gold `LEVEL 1` title and cream subtitle sit on a solid woodland wall ending below the subtitle. The entrance slit has 1.30 units of clear height for the 0.80-high Star. |
| Water tank | Walk directly into water as Star. Jump into the entry Bubble for the underwater route, or continue to the submerged BouncyBall. There is no choice card or automatic route solution. First possession of each form briefly explains its ability: REMEMBER? / Bouncy balls float!, or REMEMBER? / Bubbles swim in water and float in air. Ball rises gradually during its reminder. Horizontal steering resumes at the surface; move right onto the dry grassy deck, then bounce through the upper opening. Bubble swims beneath the deck and right wall, then rises in the exit shaft. Entry and pool-floor backup Bubbles use the same blue fill, outline, glow, scale and collider, including replacements after Pop. |
| Water exit and Box drop | Both routes rejoin at `BoxB_DropShaft`: Bubble rises and pops above the exit; Ball clears the upper dividers and drops onto Box B. The first checkpoint is on the lower route beyond the drop. |
| Lower lift and ball passage | Growing Box lifts the lower `RatchetingCeiling`. The lower ball, `BallR1_LowRoom`, can push this door downward by landing on it. Ceiling compression governs its bounce through the passage. |
| Right ascent | Checkpoint 2 sits on the open ground left of the Bubble shelf and restores the lower ball there. It can bounce around the shelf edge to possess `BubbleU2_RightAscent` and rise through the central opening. |
| Upper landing choice | Pop on the left to continue as Star, or on the right to possess the offset `BoxD_UpperTunnel` on its small platform. Grow Box to negotiate the gap toward the left landing, then shrink for the narrow tunnel. The Bubble ascent opening stays clear. Landing on the left as Star or Box activates checkpoint 3. |
| SAVE and upper lift | Star, grounded Box D, or R2 can touch SAVE from below. The nearby thick platform has one centered upward arrow. Box growth raises it; it retains that height when Box shrinks or a ball lands on it. |
| Ball and optional RUIN | `BallR2_UpperReturn` can continue left without choosing RUIN. Raising the platform enables the deliberate higher approach to the recessed RUIN button. R2 activates RUIN on its first contact from below; there is no cover or landing prerequisite. The ball can return to SAVE. |
| Upper return and endpoint | Travel left through the selected NORMAL, SAFE or HARD route to the flag. All forms use the same completion trigger. |

Box, Bubble and BouncyBall use glow to identify possessable hosts. Box has a black outline; Bubble has a pale-blue fill and darker-blue outline. A centered Star marks the controlled host. These visuals do not change the collision size.

## Woodland presentation

Level01 reuses the [Kindergarten woodland assets and provenance](../Assets/Art/IntroBouncyBallForest/manifest.json): sage/teal forest, dark olive soil, narrow grass edges and subdued blue water depth. The backdrop fits the camera; water artwork renders behind terrain and forms. Moving lifts retain a lighter timber tint. World labels use dark ink on scenery and cream on walls; the title is warm gold. The solid title wall has a quiet, untextured face and ends below the subtitle, leaving the entrance movement space open.

The lower-door Hint uses a dark green face, cream label and yellow Star so the icon does not disappear into a yellow background. Its lift preview uses a translucent cream silhouette. Form colors, outlines and hazard identities remain recognizable. The shared Collection Book icon matches Home in size; see [controls and book presentation](GameplaySpec.md#2-controls).

## Route selection and camera presentation

The enum state is `Safe`; its button is labeled `SAVE MY LIFE`.

| State | Upper return route | Selection |
| --- | --- | --- |
| NORMAL | Default floor spikes; SAFE boards and HARD-only hazards hidden | Initial state, or reactivate the selected button after its rearm condition is met |
| SAFE | All spikes hidden. Three black boards form a safe S-shaped timing route: the rightmost is stationary and the other two translate vertically in opposite directions | Touch SAVE from below as Star, grounded Box D or R2 |
| HARD | Default floor spikes plus additional floor and aligned ceiling spikes; one ceiling spike is a proximity-triggered falling trap | R2 touches RUIN directly from below |

- SAVE and RUIN are mutually exclusive. Selecting the other button changes mode directly.
- The selected button reads `BACK TO NORMAL`. SAVE rearms whenever the controlled body stops touching its sensor, regardless of direction. A vertical bounce or Box shrink followed by renewed eligible contact can toggle it again; continuous contact never repeats. RUIN still requires the whole body to leave its horizontal band with a margin before returning.
- Mode changes preserve the upper lift height. Full restart resets it.
- On first arrival under SAVE in NORMAL, Box or Star triggers a shared camera preview of the left hazards. Being in the lower room does not trigger it. Viewing once as Star also counts when later returning as Box.
- Selecting a mode pauses player control and frames the changed route. SAFE uses quick camera transitions with a full reveal: 0.45 seconds to frame the route, a 0.1-second beat, 1.6 seconds to sink spikes, 1.2 seconds to reveal the boards, a 2-second hold on the fully revealed layout, then 0.45 seconds to return the camera (about 5.8 seconds total). It does not wait for an up/down cycle; board motion starts when gameplay resumes. HARD reveals the extra floor and ceiling hazards. NORMAL restores the default layout.
- The two moving SAFE boards keep fixed dimensions and a shared opposite phase. They have no arrows. The shorter left board permits a jump over its low position or passage underneath its high position. If a leading edge would catch a possessable body, both boards pause together without losing their phase.
- The HARD trap activates only for the controlled body approaching its horizontal band in the upper corridor, outside cinematics. It falls quickly, stays visible on the floor and remains lethal. It does not fade out. Route resets and checkpoint retries rearm it.
- `AdvancedLevelCamera2D` handles ordinary regional framing; route presentations temporarily take control and restore it afterward. Editor Game-view Scale only affects the preview display, not the camera's saved zoom.

## November 5 playtest improvements

PR #21 (`42e32a4`) integrates four developer-tested changes, included in the published Beta 0.1.2 export:

- Upper platform edges preserve the gap and equal landing height while using small collision bevels to reduce maximum-size Box snagging in either direction. Recheck both directions, stopping at the edge and resizing during movement.
- The approach to checkpoint 2 was widened in PR #21. The later polish branch also moves the checkpoint left, as described below.
- BouncyBall camera framing near checkpoint 2 suppresses repeated bounce-driven vertical shake while preserving route transitions.
- Box-to-ball possession after a fall before checkpoint 3 allows recovery instead of leaving the ball trapped under an abandoned Box.

These fixes do not change the fixed checkpoint respawn forms or convert checkpoint state into a persistent save.

## Checkpoints and restart

The polish branch moves checkpoint 2’s marker, trigger and Ball respawn 2.4 units left onto open ground, so saving no longer requires entering the small space beneath the Bubble shelf.

All three markers remain hollow stars: blue before activation, yellow after being reached. `CHECKPOINT SAVED` appears briefly above the marker. Progress only advances 1 → 2 → 3; returning through an earlier point does not overwrite a later save.

| Point / analytics ID | Location | Respawn body | Authored spawn |
| --- | --- | --- | --- |
| 1 / `ADV_CP1` | Lower route after the Box drop | `BoxB_DropShaft` / Box | `(25.3, 1.34)` |
| 2 / `ADV_CP2` | Bottom of the right ascent, left of the Bubble shelf | `BallR1_LowRoom` / BouncyBall | `(39.7, 1.32)` |
| 3 / `ADV_CP3` | Left upper landing after the Bubble ascent | Original Star | `(38.8, 11.69)` |

Checkpoint 3 requires Star or Box to land on the left floor; flying over it as Bubble does not save. Respawn form is fixed by the checkpoint, not copied from the entering form. Automatic last-possessed-ball checkpoints are disabled in this scene.

Death and Return to Checkpoint reload the scene, restore the configured body and retain the latest completed route choice, upper lift height and viewed route-presentation history. Loose bodies return to authored placements; the lower reversible door resets. SAFE boards restart a predictable motion cycle and the HARD falling spike rearms. A half-finished cinematic is not saved. This is in-memory session recovery, not a save across application launches.

Restart Level, returning to Navigation, a new menu entry and forward level progression clear checkpoint sessions. Restart Level restores NORMAL, the upper lift's initial low position and all original bodies. See [CheckpointGuidance.md](CheckpointGuidance.md) for the menus and recovery matrix.

## Hints and Home menu

| Hint / control | Trigger and behavior |
| --- | --- |
| Movable-door hint | After 20 seconds without lower-door movement, an optional star sign appears above the route. Clicking the visible sign, deliberate Box growth or Ball contact triggers a short paused lift preview with “A little lift?”. Mouse clicks on the sign do not resize Box. The sign returns after every preview and can be replayed without limit until the real door rises by 0.8 units. Leaving the room does not retire it. Spawn overlap and continued contact after a preview do not automatically activate it. |
| Water-route ability reminders | No choice card, optional Hint button, platform highlight, route arrow or idle/pacing hint is created in this water section. First possession of each form per scene visit automatically shows REMEMBER? with Bouncy balls float! or Bubbles swim in water and float in air. The brief reminder lasts 5 seconds (or through Ball ascent) at a fixed nearby scene anchor, and does not replay on repeat possession. The card renders above water but behind form sprites, so a passing Bubble and its controlled Star remain visible in front of it. Only a Ball trapped in the right-hand water pocket receives a persistent automatic No ground to bounce. / Press R to restart. notice without a REMEMBER? heading (touch: Home > Restart Level). Pauses and cinematics hide the card and pause its timer. |
| Bubble exits | The water exit and upper landing use the same Bubble controls as the rest of Level01. Tap Pop on mobile, or press Q or right-click on desktop. |
| Home | Top-right square house outline with a small Star; tooltip `Home · Restart`. Opens a paused menu with Continue Playing, Return to Checkpoint, Restart Level and Main Menu. Checkpoint is disabled before a save; full restart and Main Menu require confirmation. |
| Collection Book | Caption-free square book icon matching Home, using the shared non-Navigation presentation. Opens paged form help; possession discovers pages for the current application session. |
| R | Retains the existing restart-choice menu, independent of Home. Opening or cancelling changes no checkpoint state. |

`AdvancedCheckpointController` suppresses the legacy E-pop reminder. Level01 has no separate ceiling-triggered Q-pop lessons. [Controls by form](GameplaySpec.md#2-controls)

## Verification and remaining work

Elementary admission update: isolated Unity Play Mode checks passed the real Level01 flag trigger, a 15-second paused hold, stable post-reveal Star, Next Level → IntroSpider, topmost Home pointer hits during and after reveal → Navigation, restored time on both exits, and the original Kindergarten graduation/Next Level path. Text bounds were checked at 1920 × 1080 and 1280 × 960. Physical touch and exported-player verification remain separate.

The branch’s existing Editor checks and developer playtests cover water traversal, reminders, matching renewable Bubbles, checkpoint placement, book presentation and clickable/replayable door help. See [Level01 polish source acceptance](AnalyticsTestPlan.md#level01-polish-source-acceptance) for evidence and remaining browser/device checks. This documentation sync does not rerun gameplay or publish a build.

Retained Editor tools: `StarFormer → Level01 → Verify Water Routes in Play Mode` and `Verify Water Pocket Recovery`. These exercise the final ability-reminders-only design. Earlier experiments with choice cards, optional water hints, platform highlights and route arrows were removed and are not current behavior.

Remaining playtest follow-ups: SAVE/RUIN meaning still needs clearer new-player validation after removing the RUIN cover; reported Ball corner and upper Box-tunnel sticking need reproduction; upper-route recovery after early Bubble Pop or misplaced Box needs a targeted sweep. Automatic possession and the separation required before switching back, form limits and blocked Box growth remain feedback/readability questions, not fixes delivered by this branch.

The main merge (`5c2496d`) and scene promotion (`8834a4d`) passed developer-reported Unity playtests. Automated checks in an isolated Unity copy verified compilation, enabled-scene references, both Level01 entry paths, flag completion in Star/Box/Bubble/BouncyBall forms, the IntroSpider → FanTutorial → Level02 continuation, all three checkpoint forms, mechanism retention/reset, and Home/R menu behavior. The original backup and its meta matched the previous scene byte for byte; promotion changed only the Advanced endpoint component within the scene.

Historical Beta 0.1.1 was published at the Beta URL before 0.1.2. The 0.1.1 export included the contact-activated movable-door hint but predated paged Help; Beta 0.1.2 includes the later Help and Navigation work. All seven deployed files matched the recorded 0.1.1 export on November 5. This package check does not replace gameplay testing.

For the next export, repeat the current branch checks alongside shared Home/Collection Book and all three checkpoint recoveries. Re-export analytics maps and verify version-specific production rows/maps. See [publishing instructions](publish_instructions.md) and [branch acceptance](AnalyticsTestPlan.md#level01-polish-source-acceptance). Historical release evidence does not certify a new export.
