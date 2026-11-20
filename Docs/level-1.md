# Level 1

The November 6 Beta 0.1.2 release was published at root and `/beta/` via `64b35c6`; source checkout at publication: `b93768f`. Deployment and hosted file checks passed; browser gameplay acceptance remains separate. [Release record](Beta-0.1.2.md).

Touch controls reviewed November 16, 2026. Promotion baseline: `8834a4d`; recorded published Beta 0.1.1 source: `9451511` (gh-pages `e81097e`, November 3).

## Scene identity and progression

[Level01.unity](../Assets/Scenes/Level01.unity) is the former Advanced layout. Open this scene for further Level 1 work; there is no separate active `Level01_Advanced.unity`.

The previous scene and its original `.meta` are preserved unchanged as [Level01_Legacy.unity](../Assets/Scenes/Level01_Legacy.unity). It is excluded from builds. `Level01_Reference` is a separate, older design reference. Neither backup is the current playable map; the old G/G2, held-plate, ball-checkpoint and U2-regeneration layout must not be used as the specification for this scene.

```text
Navigation → IntroBox → IntroBubble → IntroBouncyBall → Level01 → IntroSpider → IntroFan → Level02
```

- Navigation’s Level 1 entrance targets `Level01` through NavigationEntrance; both contact and click paths run the entry feedback before loading.
- IntroBouncyBall's Star/BouncyBall goal also loads `Level01`; IntroBubble first opens a passage to that tutorial.
- Level01 is at enabled build index 4, after IntroBouncyBall; IntroSpider, IntroFan and Level02 follow at indices 5, 6 and 7. Its GUID is the former Advanced GUID. The old Level01 GUID belongs to the disabled Legacy backup.
- The endpoint flag uses `LevelGoal`, with no required gate, destination `IntroSpider`, label `Level 2 Intro`, and a 1.5-second transition. Any living controlled form can finish.
- Completion pauses gameplay, shows `Level up! / Entering Level 2 Intro...`, clears checkpoint sessions and loads the next scene with normal time. The final wait-for-menu completion screen belongs to Level02.

## Playable route

| Section | Current behavior |
| --- | --- |
| Entrance | Start as Star without a forced Box transfer or READY button. The gold `LEVEL 1` title and cream subtitle sit on a solid woodland wall ending below the subtitle. The entrance slit has 1.30 units of clear height for the 0.80-high Star. |
| Water tank | Walk directly into water as Star. Jump into the entry Bubble for the underwater route, or continue to the submerged BouncyBall. The choice card sits inside the pool and retires after selecting either form. First possession of each form briefly explains its ability: REMEMBER / Balls float!, or Bubbles swim in water and float in air. Ball rises gradually during its reminder. Horizontal steering resumes at the surface; move right onto the dry grassy deck, then bounce through the upper opening. Bubble swims beneath the deck and right wall, then rises in the exit shaft. Entry and pool-floor backup Bubbles use the same blue fill, outline, glow, scale and collider, including replacements after Pop. |
| Water exit and Box drop | Both routes rejoin at `BoxB_DropShaft`: Bubble rises and pops above the exit; Ball clears the upper dividers and drops onto Box B. The first checkpoint is on the lower route beyond the drop. |
| Lower lift and ball passage | Growing Box lifts the lower `RatchetingCeiling`. The lower ball, `BallR1_LowRoom`, can push this door downward by landing on it. Ceiling compression governs its bounce through the passage. |
| Right ascent | Checkpoint 2 sits on the open ground left of the Bubble shelf and restores the lower ball there. It can bounce around the shelf edge to possess `BubbleU2_RightAscent` and rise through the central opening. |
| Upper landing choice | Pop on the left to continue as Star, or on the right to possess the offset `BoxD_UpperTunnel` on its small platform. Grow Box to negotiate the gap toward the left landing, then shrink for the narrow tunnel. The Bubble ascent opening stays clear. Landing on the left as Star or Box activates checkpoint 3. |
| SAVE and upper lift | Star, grounded Box D, or R2 can touch SAVE from below. The nearby thick platform has one centered upward arrow. Box growth raises it; it retains that height when Box shrinks or a ball lands on it. |
| Ball and optional RUIN | `BallR2_UpperReturn` can continue left without choosing RUIN. Raising the platform enables the deliberate higher approach to the recessed RUIN button. The first upward R2 hit breaks its cover and sends the ball downward; after landing, a later bounce can activate RUIN. The ball can return to SAVE. |
| Upper return and endpoint | Travel left through the selected NORMAL, SAFE or HARD route to the flag. All forms use the same completion trigger. |

Box, Bubble and BouncyBall use glow to identify possessable hosts. Box has a black outline; Bubble has a pale-blue fill and darker-blue outline. A centered Star marks the controlled host. These visuals do not change the collision size.

## Route selection and camera presentation

The enum state is `Safe`; its button is labeled `SAVE MY LIFE`.

| State | Upper return route | Selection |
| --- | --- | --- |
| NORMAL | Default floor spikes; SAFE boards and HARD-only hazards hidden | Initial state, or return to the currently selected button after leaving its horizontal band |
| SAFE | All spikes hidden. Three black boards form a safe S-shaped timing route: the rightmost is stationary and the other two translate vertically in opposite directions | Touch SAVE from below as Star, grounded Box D or R2 |
| HARD | Default floor spikes plus additional floor and aligned ceiling spikes; one ceiling spike is a proximity-triggered falling trap | R2 touches RUIN from below after its breakable cover is cleared |

- SAVE and RUIN are mutually exclusive. Selecting the other button changes mode directly.
- The selected button reads `BACK TO NORMAL`. The whole controlled body must leave its horizontal band with a margin and return before that button can toggle NORMAL. Vertical bounces, held contact and shrinking at the edge do not rearm it.
- Mode changes preserve the upper lift height and broken-cover state. Full restart resets both.
- On first arrival under SAVE in NORMAL, Box or Star triggers a shared camera preview of the left hazards. Being in the lower room does not trigger it. Viewing once as Star also counts when later returning as Box.
- Selecting a mode pauses player control and frames the changed route. SAFE sinks the spikes, reveals the boards, then demonstrates a full exchange in both directions before restoring control. HARD reveals the extra floor and ceiling hazards. NORMAL restores the default layout.
- The two moving SAFE boards keep fixed dimensions and a shared opposite phase. They have no arrows. The shorter left board permits a jump over its low position or passage underneath its high position. If a leading edge would catch a possessable body, both boards pause together without losing their phase.
- The HARD trap activates only for the controlled body approaching its horizontal band in the upper corridor, outside cinematics. It falls quickly, stays visible on the floor and remains lethal. It does not fade out. Route resets and checkpoint retries rearm it.
- `AdvancedLevelCamera2D` handles ordinary regional framing; route presentations temporarily take control and restore it afterward. Editor Game-view Scale only affects the preview display, not the camera's saved zoom.

## November 5 playtest improvements

PR #21 (`42e32a4`) integrates four developer-tested changes, included in the published Beta 0.1.2 export:

- Upper platform edges preserve the gap and equal landing height while using small collision bevels to reduce maximum-size Box snagging in either direction. Recheck both directions, stopping at the edge and resizing during movement.
- The approach to checkpoint 2 was widened in PR #21. Following later playtest feedback, the marker, trigger and Ball respawn were moved 2.4 units left onto open ground, so saving no longer requires entering the small space beneath the Bubble shelf.
- BouncyBall camera framing near checkpoint 2 suppresses repeated bounce-driven vertical shake while preserving route transitions.
- Box-to-ball possession after a fall before checkpoint 3 allows recovery instead of leaving the ball trapped under an abandoned Box.

These fixes do not change the fixed checkpoint respawn forms or convert checkpoint state into a persistent save.

## Checkpoints and restart

All three markers remain hollow stars: blue before activation, yellow after being reached. `CHECKPOINT SAVED` appears briefly above the marker. Progress only advances 1 → 2 → 3; returning through an earlier point does not overwrite a later save.

| Point / analytics ID | Location | Respawn body | Authored spawn |
| --- | --- | --- | --- |
| 1 / `ADV_CP1` | Lower route after the Box drop | `BoxB_DropShaft` / Box | `(25.3, 1.34)` |
| 2 / `ADV_CP2` | Bottom of the right ascent, left of the Bubble shelf | `BallR1_LowRoom` / BouncyBall | `(39.7, 1.32)` |
| 3 / `ADV_CP3` | Left upper landing after the Bubble ascent | Original Star | `(38.8, 11.69)` |

Checkpoint 3 requires Star or Box to land on the left floor; flying over it as Bubble does not save. Respawn form is fixed by the checkpoint, not copied from the entering form. Automatic last-possessed-ball checkpoints are disabled in this scene.

Death and Return to Checkpoint reload the scene, restore the configured body and retain the latest completed route choice, upper lift height, broken RUIN cover and viewed route-presentation history. Loose bodies return to authored placements; the lower reversible door resets. SAFE boards restart a predictable motion cycle and the HARD falling spike rearms. A half-finished cinematic is not saved. This is in-memory session recovery, not a save across application launches.

Restart Level, returning to Navigation, a new menu entry and forward level progression clear checkpoint sessions. Restart Level restores NORMAL, the upper lift's initial low position, the intact cover and all original bodies. See [CheckpointGuidance.md](CheckpointGuidance.md) for the menus and recovery matrix.

## Hints and Home menu

| Hint / control | Trigger and behavior |
| --- | --- |
| Movable-door hint (Beta 0.1.1) | After 20 seconds without lower-door movement, an optional star sign appears above the route. Clicking the visible sign, deliberate Box growth or Ball contact triggers a short paused lift preview with “A little lift?”. Mouse clicks on the sign do not resize Box. Once per scene load; spawn overlap does not activate it. |
| Water-route ability reminders | No choice card, optional Hint button, platform highlight, route arrow or idle/pacing hint is created in this water section. First possession of each form per scene visit automatically shows REMEMBER? with Bouncy balls float! or Bubbles swim in water and float in air. The brief reminder lasts 5 seconds (or through Ball ascent) at a fixed nearby scene anchor, and does not replay on repeat possession. Only a Ball trapped in the right-hand water pocket receives a persistent automatic No ground to bounce. / Press R to restart. notice without a REMEMBER? heading (touch: Home > Restart Level). Pauses and cinematics hide the card and pause its timer. |
| Bubble exits | The water exit and upper landing use the same Bubble controls as the rest of Level01. Tap Pop on mobile, or press Q or right-click on desktop. |
| Home | Top-right square house outline with a small Star; tooltip `Home · Restart`. Opens a paused menu with Continue Playing, Return to Checkpoint, Restart Level and Main Menu. Checkpoint is disabled before a save; full restart and Main Menu require confirmation. |
| Help | Paged form help is now configured in this scene; possession discovers gameplay pages for the current application session. |
| R | Retains the existing restart-choice menu, independent of Home. Opening or cancelling changes no checkpoint state. |

`AdvancedCheckpointController` suppresses the legacy E-pop reminder. Level01 has no separate ceiling-triggered Q-pop lessons. [Controls by form](GameplaySpec.md#2-controls)

## Verification and remaining work

Ability-reminders-only update: Unity Play Mode input checks passed first-contact REMEMBER? text for Bubble and Bouncy Ball, gradual Ball flotation, reminder expiry and no replay on repeat Bubble possession, both routes to Box B, and no optional UI while waiting, pacing or bouncing. The pocket automatically displays its persistent restart notice, adapts to simulated touch controls, and hides when R opens the restart menu. Ball, Bubble and pocket reminder captures were reviewed. The resource checker reported zero errors (six existing class-name warnings). Physical touch-device testing remains separate.

Earlier visual-hint iteration (removed in favor of ability reminders only): Unity Play Mode input verification passed both water routes, matching renewable Bubbles, early-Pop recovery, hints while pacing, the platform highlight, underwater arrows, and the automatic desktop/simulated-touch pocket warning. A focused bouncing test verified that arrow mesh positions remain unchanged across multiple bounces, no solution text card appears, opacity fades before the 8-second expiry, and the cue disappears. Rendered platform/exit/underwater cues were reviewed. Physical touch-device testing remains separate.

Earlier nearby-text-card iteration (superseded by fixed scene cues): Unity Play Mode verification passed a continuously swimming Bubble receiving a Hint after repeated left/right traversals, clicked Star/Bubble hint cards positioned near (and not over the center of) the player, both routes to Box B, and the nearby automatic pocket warning in desktop/simulated touch modes. Rendered Star and Bubble cards were reviewed. Familiar-form switches no longer reset the idle clock; first-time form discovery and new-area exploration still do.

Automatic pocket-recovery exception: targeted Unity Play Mode verification passed immediate notice on entering the right-hand pocket, continued visibility beyond the normal Hint delay with no star button or close button, desktop/touch wording, and hiding during the R restart menu. `StarFormer → Level01 → Verify Water Pocket Recovery` runs this focused check.

Optional-hint update, before the automatic pocket-recovery exception: Unity Play Mode input tests passed the in-pool choice card disappearing on selection and staying gone after Pop, first-use-only Ball/Bubble capability reminders, no unsolicited solution or restart text, delayed Hint offers, mouse-click opening and closing, form-specific Star/Bubble/Ball help, touch-mode recovery wording, pause hiding, and both water routes to Box B. The choice and Bubble-reminder captures were visually reviewed. Physical touch-device testing remains separate.

Earlier November 19 feedback iteration (automatic route cards have since been replaced by optional hints): the final Unity Play Mode run passed gradual flotation (submerged possession stays below the surface, ascent lasts more than 1.5 seconds, and the REMEMBER lesson appears), matching Bubble visuals/colliders before and after Pop, both routes to Box B, and the deliberately missed upper route into the right-hand pocket. Pocket guidance was checked in desktop and simulated touch modes; R opened the restart menu and hid the lesson. Rendered captures were reviewed for the rise lesson and pocket recovery card. Physical touch-device testing remains separate.

Initial November 19 water-route verification (automatic recovery prompts have since been replaced by optional hints): Unity Play Mode input verification in an isolated batch-mode copy passed Star entry through the slit, ball flotation and dry-deck bouncing through the upper opening to Box B, early Bubble Pop and replacement, re-possession, the underwater passage and exit to Box B, and the idle R reminder without ceiling contact. The final run includes the adjusted entry-Bubble clearance. Editor-rendered captures of Star entry, ball flotation, the upper exit and recovery hints were also reviewed. This verifies the revised entrance segment; exported Web and touch-device gameplay acceptance remain separate. Run `StarFormer → Level01 → Verify Water Routes in Play Mode` to repeat the checks.

The main merge (`5c2496d`) and scene promotion (`8834a4d`) passed developer-reported Unity playtests. Automated checks in an isolated Unity copy verified compilation, enabled-scene references, both Level01 entry paths, flag completion in Star/Box/Bubble/BouncyBall forms, the IntroSpider → FanTutorial → Level02 continuation, all three checkpoint forms, mechanism retention/reset, and Home/R menu behavior. The original backup and its meta matched the previous scene byte for byte; promotion changed only the Advanced endpoint component within the scene.

Historical Beta 0.1.1 was published at the Beta URL before 0.1.2. The 0.1.1 export included the contact-activated movable-door hint but predated paged Help; Beta 0.1.2 includes the later Help and Navigation work. All seven deployed files matched the recorded 0.1.1 export on November 5. This package check does not replace gameplay testing.

For beta-0.1.2, repeat these checks in the exported Web player alongside shared Home/Help, the four PR #21 fixes, and all three checkpoint recoveries. Re-export analytics maps and verify version-specific production rows/maps. See [the release checklist](Beta-0.1.2.md) and [AnalyticsTestPlan.md](AnalyticsTestPlan.md#beta-012-acceptance-status). Historical Beta 0.1.0/0.1.1 evidence does not certify this new export.
