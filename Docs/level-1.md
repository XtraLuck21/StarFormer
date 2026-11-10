# Level 1

Beta 0.1.2 is published at root and `/beta/` via `64b35c6`; source checkout at publication: `b93768f`. Deployment and hosted file checks passed; browser gameplay acceptance remains separate. [Release record](Beta-0.1.2.md).

Last updated: 2026-11-06. Promotion baseline: `8834a4d`; recorded published Beta 0.1.1 source: `9451511` (gh-pages `e81097e`, November 3).

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
| Entrance | Start as Star. `LEVEL 1` and `SMALL STAR. MANY WAYS TO SHINE.` introduce the level. The optional overhead `READY?` changes to `LET'S GO!`; later separate upward Star contacts replay its bounce and sparkle animation. Walking right does not require activating it. |
| Water tank | Possess `BoxA_Start`, travel into the tank, then reach `BubbleU1_WaterEntry` farther inside rather than immediately at the entrance. Bubble swims through water and rises in air. |
| Water exit and Box drop | Steer around the divider, pop above the exit, and reach `BoxB_DropShaft`. The first checkpoint is on the lower route beyond the drop. |
| Lower lift and ball passage | Growing Box lifts the lower `RatchetingCeiling`. The lower ball, `BallR1_LowRoom`, can push this door downward by landing on it. Ceiling compression governs its bounce through the passage. |
| Right ascent | Checkpoint 2 restores the lower ball below the Bubble shelf. It can bounce around the shelf edge to possess `BubbleU2_RightAscent` and rise through the central opening. |
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
- The approach to checkpoint 2 is wider. Its placement still requires reaching the authored route rather than moving the checkpoint left into the easy path.
- BouncyBall camera framing near checkpoint 2 suppresses repeated bounce-driven vertical shake while preserving route transitions.
- Box-to-ball possession after a fall before checkpoint 3 allows recovery instead of leaving the ball trapped under an abandoned Box.

These fixes do not change the fixed checkpoint respawn forms or convert checkpoint state into a persistent save.

## Checkpoints and restart

All three markers remain hollow stars: blue before activation, yellow after being reached. `CHECKPOINT SAVED` appears briefly above the marker. Progress only advances 1 → 2 → 3; returning through an earlier point does not overwrite a later save.

| Point / analytics ID | Location | Respawn body | Authored spawn |
| --- | --- | --- | --- |
| 1 / `ADV_CP1` | Lower route after the Box drop | `BoxB_DropShaft` / Box | `(25.3, 1.34)` |
| 2 / `ADV_CP2` | Bottom of the right ascent, below the Bubble shelf | `BallR1_LowRoom` / BouncyBall | `(42.1, 1.32)` |
| 3 / `ADV_CP3` | Left upper landing after the Bubble ascent | Original Star | `(38.8, 11.69)` |

Checkpoint 3 requires Star or Box to land on the left floor; flying over it as Bubble does not save. Respawn form is fixed by the checkpoint, not copied from the entering form. Automatic last-possessed-ball checkpoints are disabled in this scene.

Death and Return to Checkpoint reload the scene, restore the configured body and retain the latest completed route choice, upper lift height, broken RUIN cover and viewed route-presentation history. Loose bodies return to authored placements; the lower reversible door resets. SAFE boards restart a predictable motion cycle and the HARD falling spike rearms. A half-finished cinematic is not saved. This is in-memory session recovery, not a save across application launches.

Restart Level, returning to Navigation, a new menu entry and forward level progression clear checkpoint sessions. Restart Level restores NORMAL, the upper lift's initial low position, the intact cover and all original bodies. See [CheckpointGuidance.md](CheckpointGuidance.md) for the menus and recovery matrix.

## Hints and Home menu

| Hint / control | Trigger and behavior |
| --- | --- |
| Movable-door hint (Beta 0.1.1) | After 20 seconds without lower-door movement, an optional star sign appears above the route. Deliberate Box growth or Ball contact triggers a short paused lift preview with “A little lift?”. Once per scene load; spawn overlap does not activate it. |
| Water-tank restart lesson | Controlled Bubble contacts the assigned tank ceiling inside its teaching area for 1.5 seconds: `Feel stuck? / Press R to restart`. Sideways movement does not reset qualifying contact. R opens choices rather than immediately restarting. |
| Water-exit pop lesson | Continuous Bubble contact with the assigned water-exit ceiling in its teaching area for 3 seconds: `Press Q / to pop`. |
| Upper-right pop lesson | Continuous Bubble contact with the upper-right ceiling in its teaching area for 3 seconds: the same Q prompt, before choosing a landing side. |
| Pop-hint lifetime | Each pop hint appears once per scene load, remains while contact continues, then hides on leaving/popping/changing bodies. Scene reload resets its lesson state; paused time does not consume the contact timer. |
| Home | Top-right square house outline with a small Star; tooltip `Home · Restart`. Opens a paused menu with Continue Playing, Return to Checkpoint, Restart Level and Main Menu. Checkpoint is disabled before a save; full restart and Main Menu require confirmation. |
| Help | Paged form help is now configured in this scene; possession discovers gameplay pages for the current application session. |
| R | Retains the existing restart-choice menu, independent of Home. Opening or cancelling changes no checkpoint state. |

`CheckpointFeedbackUI` suppresses its old generic E-pop reminder when `AdvancedCheckpointController` is present. The formal Level01 uses the scene hints above; renaming it must not enable that duplicate legacy prompt.

## Verification and remaining work

The main merge (`5c2496d`) and scene promotion (`8834a4d`) passed developer-reported Unity playtests. Automated checks in an isolated Unity copy verified compilation, enabled-scene references, both Level01 entry paths, flag completion in Star/Box/Bubble/BouncyBall forms, the IntroSpider → FanTutorial → Level02 continuation, all three checkpoint forms, mechanism retention/reset, and Home/R menu behavior. The original backup and its meta matched the previous scene byte for byte; promotion changed only the Advanced endpoint component within the scene.

Historical Beta 0.1.1 was published at the Beta URL, which now serves 0.1.2. The 0.1.1 export included the contact-activated movable-door hint but predated paged Help; Beta 0.1.2 includes the later Help and Navigation work. All seven deployed files matched the recorded 0.1.1 export on November 5. This package check does not replace gameplay testing.

For beta-0.1.2, repeat these checks in the exported Web player alongside shared Home/Help, the four PR #21 fixes, and all three checkpoint recoveries. Re-export analytics maps and verify version-specific production rows/maps. See [the release checklist](Beta-0.1.2.md) and [AnalyticsTestPlan.md](AnalyticsTestPlan.md#beta-012-acceptance-status). Historical Beta 0.1.0/0.1.1 evidence does not certify this new export.
