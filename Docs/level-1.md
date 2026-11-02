# Level 1

Last updated: 2026-11-02. Promotion baseline: `8834a4d`; published Beta source: `3a693ca` (gh-pages `1105970`).

## Scene identity and progression

[Level01.unity](../Assets/Scenes/Level01.unity) is the former Advanced layout. Open this scene for further Level 1 work; there is no separate active `Level01_Advanced.unity`.

The previous scene and its original `.meta` are preserved unchanged as [Level01_Legacy.unity](../Assets/Scenes/Level01_Legacy.unity). It is excluded from builds. `Level01_Reference` is a separate, older design reference. Neither backup is the current playable map; the old G/G2, held-plate, ball-checkpoint and U2-regeneration layout must not be used as the specification for this scene.

```text
Mainmenu → IntroBox → IntroBubble → IntroBouncyBall → Level01 → IntroSpider → FanTutorial → Level02
```

- Mainmenu's Level 1 button calls `MainMenuUI.OpenLevel1()` and loads `Level01`.
- IntroBouncyBall's Star/BouncyBall goal also loads `Level01`; IntroBubble first opens a passage to that tutorial.
- Level01 is at enabled build index 4, after IntroBouncyBall; IntroSpider, FanTutorial and Level02 follow at indices 5, 6 and 7. Its GUID is the former Advanced GUID. The old Level01 GUID belongs to the disabled Legacy backup.
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

## Checkpoints and restart

All three markers remain hollow stars: blue before activation, yellow after being reached. `CHECKPOINT SAVED` appears briefly above the marker. Progress only advances 1 → 2 → 3; returning through an earlier point does not overwrite a later save.

| Point / analytics ID | Location | Respawn body | Authored spawn |
| --- | --- | --- | --- |
| 1 / `ADV_CP1` | Lower route after the Box drop | `BoxB_DropShaft` / Box | `(25.3, 1.34)` |
| 2 / `ADV_CP2` | Bottom of the right ascent, below the Bubble shelf | `BallR1_LowRoom` / BouncyBall | `(42.1, 1.32)` |
| 3 / `ADV_CP3` | Left upper landing after the Bubble ascent | Original Star | `(38.8, 11.69)` |

Checkpoint 3 requires Star or Box to land on the left floor; flying over it as Bubble does not save. Respawn form is fixed by the checkpoint, not copied from the entering form. Automatic last-possessed-ball checkpoints are disabled in this scene.

Death and Return to Checkpoint reload the scene, restore the configured body and retain the latest completed route choice, upper lift height, broken RUIN cover and viewed route-presentation history. Loose bodies return to authored placements; the lower reversible door resets. SAFE boards restart a predictable motion cycle and the HARD falling spike rearms. A half-finished cinematic is not saved. This is in-memory session recovery, not a save across application launches.

Restart Level, returning to Mainmenu, a new menu entry and forward level progression clear checkpoint sessions. Restart Level restores NORMAL, the upper lift's initial low position, the intact cover and all original bodies. See [CheckpointGuidance.md](CheckpointGuidance.md) for the menus and recovery matrix.

## Hints and Home menu

| Hint / control | Trigger and behavior |
| --- | --- |
| Water-tank restart lesson | Controlled Bubble contacts the assigned tank ceiling inside its teaching area for 1.5 seconds: `Feel stuck? / Press R to restart`. Sideways movement does not reset qualifying contact. R opens choices rather than immediately restarting. |
| Water-exit pop lesson | Continuous Bubble contact with the assigned water-exit ceiling in its teaching area for 3 seconds: `Press Q / to pop`. |
| Upper-right pop lesson | Continuous Bubble contact with the upper-right ceiling in its teaching area for 3 seconds: the same Q prompt, before choosing a landing side. |
| Pop-hint lifetime | Each pop hint appears once per scene load, remains while contact continues, then hides on leaving/popping/changing bodies. Scene reload resets its lesson state; paused time does not consume the contact timer. |
| Home | Top-right square house outline with a small Star; tooltip `Home · Restart`. Opens a paused menu with Continue Playing, Return to Checkpoint, Restart Level and Main Menu. Checkpoint is disabled before a save; full restart and Main Menu require confirmation. |
| R | Retains the existing restart-choice menu, independent of Home. Opening or cancelling changes no checkpoint state. |

`CheckpointFeedbackUI` suppresses its old generic E-pop reminder when `AdvancedCheckpointController` is present. The formal Level01 uses the scene hints above; renaming it must not enable that duplicate legacy prompt.

## Verification and remaining work

The main merge (`5c2496d`) and scene promotion (`8834a4d`) passed developer-reported Unity playtests. Automated checks in an isolated Unity copy verified compilation, enabled-scene references, both Level01 entry paths, flag completion in Star/Box/Bubble/BouncyBall forms, the IntroSpider → FanTutorial → Level02 continuation, all three checkpoint forms, mechanism retention/reset, and Home/R menu behavior. The original backup and its meta matched the previous scene byte for byte; promotion changed only the Advanced endpoint component within the scene.

Further work remains separate: a dedicated optional Hint button, a dedicated Level01 Controls button, new-player timing/readability feedback, and broader browser/mobile/aspect-ratio coverage. Existing Help UI in other scenes is not a new Level01 Hint system. Beta 0.1.0 is now published and the developer reported local and online playtests passed. Beta Level01 records are visible in the production dashboard, but the rebuilt map is not yet matched: deaths currently appear on a coordinate grid. Correct map/version association and detailed metric checks are tracked in [AnalyticsTestPlan.md](AnalyticsTestPlan.md#beta-010-acceptance-status). See [publish_instructions.md](publish_instructions.md).
