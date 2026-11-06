# Checkpoints, restart and pause menus

Last updated: 2026-11-05. This describes the rebuilt Level01 checkpoint behavior retained in published Beta 0.1.1 (source `9451511`, gh-pages `e81097e`, November 3). The later help-book/Mainmenu UI changes are main-only; they do not change the published checkpoint specification.

## Level01 checkpoints

`AdvancedCheckpointController` owns three scene-configured checkpoints. Only the living, currently controlled body can activate one while gameplay is active. Indices advance monotonically: passing an older marker never replaces a later checkpoint.

| Index / ID | Position in route | Restored form |
| --- | --- | --- |
| 1 / `ADV_CP1` | Lower room after the Box drop | Box, using `BoxB_DropShaft` |
| 2 / `ADV_CP2` | Right ascent below the Bubble shelf | BouncyBall, using `BallR1_LowRoom`; bounce up to reach the Bubble |
| 3 / `ADV_CP3` | Upper-left landing after the right ascent | Star, even if Box activated it |

Checkpoint 3 requires a grounded Star or Box near the authored floor. Bubble passing overhead does not activate it. Markers stay hollow: blue before reaching them, yellow afterward, with a temporary `CHECKPOINT SAVED` label. Old reached markers remain yellow. The latest point determines respawn.

`enableBouncyBallCheckpoints` is off in formal Level01. Touching R1 or R2 alone does not create a save. `BouncyBallCheckpointSession` remains available for legacy opt-in scenes; it is not the current first-level checkpoint system.

## Recovery matrix

| State | Death / Return to Checkpoint | Restart Level / fresh entry |
| --- | --- | --- |
| Active checkpoint and reached markers | Retained; restore its fixed host and safe position | Cleared; start as Star at entrance |
| Latest completed NORMAL/SAFE/HARD mode | Retained, without replaying its cinematic | NORMAL |
| Upper one-way platform | Highest completed lift retained | Authored low height |
| RUIN cover | Broken state retained | Intact |
| Viewed SAVE warning and route presentations | Retained | Cleared |
| Lower reversible lift/door | Authored initial state | Authored initial state |
| Loose bodies, sizes and consumed Bubbles | Recreated at authored scene state, then place checkpoint host | Authored initial state |
| SAFE moving-board phase | Predictable fresh cycle | Hidden in NORMAL |
| HARD falling spike | Restored to its ceiling and rearmed when HARD is active | Hidden in NORMAL |
| READY interaction and local timed Bubble hints | Fresh scene-local state | Fresh scene-local state |

The controller captures completed progress after checkpoint activation, including later route/lift/cover changes. It does not save a partial hazard animation. Without a checkpoint, death recovers at the scene entrance. Mainmenu entry, leaving through Home's Main Menu action, and forward progression clear all checkpoint sessions. Nothing here is persistent storage across browser/application launches.

## Level01 Home menu

The top-right house-and-Star square has no permanent text. Hover shows `Home · Restart`. Clicking opens `Paused` and freezes gameplay.

| Choice | Behavior |
| --- | --- |
| Continue Playing | Close the menu and restore play; initially selected |
| Return to Checkpoint | Disabled with `No checkpoint yet` before a save; otherwise reload using the retained recovery state |
| Restart Level | Confirmation before clearing progress and reloading the current scene |
| Main Menu | Confirmation before clearing the session and loading Mainmenu |

Confirmation defaults to Cancel. Cancelling a confirmation returns to Paused; Escape from Paused resumes. The menu blocks background interaction, does not stack over the R menu or a route cinematic, and restores its owned pause when disabled. It does not add Hint/Controls actions.

## R menu and other scenes

- R or a touch Restart action opens `RestartMenuUI`; no hold is required and opening never reloads.
- Return to Checkpoint appears when a current-scene positional or opted-in ball checkpoint exists. Restart Level clears progress. Cancel, Escape or R closes without restarting; Cancel is initially selected.
- The R menu also pauses automatic death recovery. Cancelling restores the remaining death-delay time.
- Home's extra confirmation screens do not replace or change the existing R shortcut.
- IntroBubble hides its standalone Restart button; R remains available. Its left exit returns to IntroBox's endpoint as BoxB with the door open. IntroBouncyBall also returns through its left passage to IntroBubble as Star near the flag with the door open. Both are one-shot scene-travel setups, not checkpoint recovery; restarting IntroBubble later restores its EntryBox start.
- Level02 retains CP01/CP02/CP03 and its existing Return to Main Menu, Restart Level and Retry Checkpoint buttons. Retry/death restore its saved Star position and stage events; full restart clears it. See [level-2.md](level-2.md).

## Bubble hints

Formal Level01 uses assigned ceiling contact and teaching regions, not generic stationary detection. The first tank displays its R lesson after 1.5 seconds. The water-exit and upper-right ceilings each display a once-per-scene-load `Press Q to pop` lesson after 3 seconds of continuous qualifying contact. Sideways movement under the ceiling can still count. Paused time does not advance the pop lessons.

The generic E-pop reminder still exists in the legacy feedback script but is suppressed for scenes with `AdvancedCheckpointController`. It must not appear beside the current Level01 Q hints. This suppression is part of the scene-promotion fix, not an outstanding first-level copy bug.

## Verification

Developer playtests passed for checkpoint implementation and formal Level01 promotion. Isolated Unity regression checks passed for all three respawn forms, non-regressing checkpoint order, death/manual recovery, route/lift/cover retention, full-reset behavior, Home confirmation/cancellation, R menu opening/cancellation, and scene entry/exit clearing.

The developer reported final Beta local and hosted playtests passed. The Level01 analytics screenshot confirms records, not a checkpoint-by-checkpoint metric audit. Continue targeted browser regression, including input/focus loss, narrow windows, real touch devices where controls exist, and pause during the death delay. Editor checks do not establish hosted-build or Google Sheets delivery results. See [AnalyticsTestPlan.md](AnalyticsTestPlan.md) for gameplay checkpoint IDs versus analytics Run/Attempt lifecycle.
