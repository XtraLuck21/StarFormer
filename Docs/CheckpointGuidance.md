# Checkpoints, restart and pause menus

Gold 0.1.0 was published November 17, 2026 to the main site and `gold/`. Deployment succeeded and all 14 hosted runtime files matched the verified export. [Gold release record](Gold-0.1.0.md)

## Level01 checkpoints

`AdvancedCheckpointController` owns three scene-configured checkpoints. Only the living, currently controlled body can activate one while gameplay is active. Indices advance monotonically: passing an older marker never replaces a later checkpoint.

| Index / ID | Position in route | Restored form |
| --- | --- | --- |
| 1 / `ADV_CP1` | Lower room after the Box drop | Box, using `BoxB_DropShaft` |
| 2 / `ADV_CP2` | Open ground at the right ascent, left of the Bubble shelf | BouncyBall, using `BallR1_LowRoom`; bounce up to reach the Bubble |
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
| Water ability reminders and replayable lower-door Hint | Fresh scene-local state; door Hint must reach its offer delay again | Fresh scene-local state |

The controller captures completed progress after checkpoint activation, including later route/lift/cover changes. It does not save a partial hazard animation. Without a checkpoint, death recovers at the scene entrance. Navigation entry, leaving through Home's Main Menu action, and forward progression clear all checkpoint sessions. Nothing here is persistent storage across browser/application launches.

## Shared Home menu

Each of the seven enabled gameplay scenes has the top-right house-and-Star square, sized 88 × 88 at a 24-pixel inset on the reference Canvas. It has no permanent text. Hover shows `Home · Restart`. Clicking opens `Paused` and freezes gameplay.

| Choice | Behavior |
| --- | --- |
| Continue Playing | Close the menu and restore play; initially selected |
| Return to Checkpoint | Disabled with `No checkpoint yet` before a save; otherwise reload using the retained recovery state |
| Restart Level | Confirmation before clearing progress and reloading the current scene |
| Main Menu | Confirmation before clearing the session and loading Navigation |

Confirmation defaults to Cancel. Cancelling a confirmation returns to Paused; Escape from Paused resumes. The menu blocks background interaction, does not stack over the R menu or a route cinematic, and restores its owned pause when disabled. It does not add Hint/Controls actions.

### Completion-screen exceptions

After IntroBouncyBall completion, gameplay remains paused on the graduation screen. The house stays visible and clickable above its backdrop, with a `Main Menu` tooltip. It loads Navigation directly instead of opening Paused or asking to abandon an already-completed run. Next Level loads Level01. Both routes clear checkpoint sessions and restore normal time; form discoveries still last for the application session. Escape does not bypass graduation into the ordinary Home menu. See [completion feedback](intro-levels.md#completion-feedback).

Level01 uses the same direct Home exit on its Elementary School acceptance page. Home stays above the backdrop and is clickable during and after the reveal. Next Level loads IntroSpider; neither the reveal nor waiting advances the level automatically. Both exits restore time and clear checkpoint sessions without reporting abandonment of the completed run.

IntroBox's dropped-cargo retry restores just its carrying lesson, not a checkpoint or scene reload. Backward scene passages also remain separate from checkpoints.

## R menu and other scenes

- R or a touch Restart action opens `RestartMenuUI`; no hold is required and opening never reloads.
- Return to Checkpoint appears when a current-scene positional or opted-in ball checkpoint exists. Restart Level clears progress. Cancel, Escape or R closes without restarting; Cancel is initially selected.
- The R menu also pauses automatic death recovery. Cancelling restores the remaining death-delay time.
- Home's extra confirmation screens do not replace or change the existing R shortcut.
- Old standalone menu/restart controls are disabled in favor of Home; R remains available. IntroBubble’s left exit returns to IntroBox's endpoint as BoxC with the door open. IntroBouncyBall also returns through its left passage to IntroBubble as Star near the flag with the door open. Both are one-shot scene-travel setups, not checkpoint recovery; restarting IntroBubble later restores its EntryBox start.
- Promoted Level02 uses DROP_BOTTOM, SEESAW_ROOM and the five Smiley Face entries documented in [level-2.md](level-2.md#2-scene-wiring-to-preserve). Death/Home retry restore the configured Star, Spider or Box entry and its stage progress; later entries cannot regress. A saved forehead entry also enables the lower-right fall-return zone without a death/reload. The scene uses a short Respawning pause; full restart clears its session records. CP01/CP02 remain legacy objects; CP03 has been removed.

## Touch input and checkpoint recovery

In Level01, a BouncyBall entering the narrow right-hand water pocket automatically sees `No ground to bounce. / Press R to restart.`; no ceiling contact, idle wait or Hint click is required. Touch mode substitutes Home > Restart Level. On touch screens, tap the house and choose Return to Checkpoint after a save, or Restart Level to start over. Bubble’s Pop button returns to Star at either exit.

The gameplay joystick clears on death and when the controls are hidden. When the HUD returns after a checkpoint reload, a thumb held inside its movement area can establish a fresh neutral center. To test recovery, keep one thumb down through respawn, drag again, then lift and start a new drag in the opposite direction.

Shape discovery is separate from checkpoint state. A menu return/full level restart clears the applicable checkpoint session but retains discovered forms for this application launch. Death and checkpoint retry also retain discoveries. Only a new game/Editor Play launch clears them; completion no longer grants form groups. See [session-only discovery](Beta-0.1.2.md#session-only-shape-discovery).

## Verification

Checkpoint 2’s marker, trigger and respawn were moved left together in the polish branch; the developer reported the new placement works. Its fixed Ball form and ordered checkpoint rules are unchanged. See [authored positions](level-1.md#checkpoints-and-restart) and [branch acceptance](AnalyticsTestPlan.md#level01-polish-source-acceptance).

Developer playtests passed for checkpoint implementation and formal Level01 promotion. Isolated Unity regression checks passed for all three respawn forms, non-regressing checkpoint order, death/manual recovery, route/lift/cover retention, full-reset behavior, Home confirmation/cancellation, R menu opening/cancellation, and scene entry/exit clearing.

The developer reported final Beta local and hosted playtests passed. The Level01 analytics screenshot confirms records, not a checkpoint-by-checkpoint metric audit. Continue targeted browser regression, including input/focus loss, narrow windows, real touch devices where controls exist, and pause during the death delay. Editor checks do not establish hosted-build or Google Sheets delivery results. See [AnalyticsTestPlan.md](AnalyticsTestPlan.md) for gameplay checkpoint IDs versus analytics Run/Attempt lifecycle.
