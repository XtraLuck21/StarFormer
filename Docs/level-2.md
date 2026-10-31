# Level 2 Progress

Last updated: 2026-10-18.

Scene: [Level02.unity](../Assets/Scenes/Level02.unity). Current handoff branch: `Level-2`, gameplay baseline `ab68064`. The original Level02 was already included in main and the October 12 All-Level builds; the October 18 mobile/polish follow-up described here is on `Level-2`. A merge of this follow-up into main and a new WebGL publication have not been recorded.

**October 18 wrap-up:** main through analytics PR #13 (`7e50e09`) was merged into Level-2 as `8929f94`, preserving the intro polish, Level 1 fixes and teammate mobile controls. Follow-ups add the final completion screen and corrected Spider/Fan hints (`81adda2`), Restart Level and Retry Checkpoint buttons (`ce77790`), English script comments/Inspector text (`260b9b1`), and removal of the unused brown finish block (`ab68064`). The developer confirmed hint presentation, accepted the top-right button layout, and tested goal completion and return to Mainmenu after deleting the block. A complete checkpoint/mobile/browser regression matrix is still pending.

For controls and form behavior, see [GameplaySpec.md](GameplaySpec.md). For script responsibilities, see [ScriptGuide.md](ScriptGuide.md). For build and publication steps, see [publish_instructions.md](publish_instructions.md).

## 1. Current route and progress

The table describes the authored stage groups and major mechanics; it is not a new balance or route-coverage certification.

| Area | Intended play sequence | Current state |
| --- | --- | --- |
| Opening / `level2Begin` | Start as Star, use the opening boxes and transfer into Spider for surface traversal | Shared contact possession and form controls are integrated; the developer completed a keyboard pass before the latest presentation fixes |
| Spider / chase | Traverse connected surfaces; the height trigger reveals the enemy spike wall, followed by the chase and collapsing blocks | `level2Camera`, `spikeWallSpawn`, `SpikeWallEnemyChase` and `StepCollapseBlock` coordinate the sequence. Q / Leave returns to Star; the small upper-center release hint passed developer testing |
| Drop / `level2Transit` | Pass through the falling-camera section, then continue with the transition area's forms and obstacles | CP01 resumes at the transition entrance; the bubble spawner supplies a replacement after the previous bubble is gone or inactive |
| Fan and bubble traversal | Toggle Fan airflow to move eligible obstacles, leave with Q, and use Bubble for the ascent | Fan hint reads F / Blow and Q / Leave; developer reports the hint is correct. Airflow affects eligible non-possessable rigidbodies, not possessed forms |
| `JumpOrDie` / `ShootOrDie` | Traverse platform hazards and use the possessable spike wall's projectiles with switches/doors | Moving platforms, lethal hazards and firing interactions remain in the scene. Space / Fire remains the shooting action; it was not replaced by Q |
| Later route / `MickeyMiaoMiaoRoom` | Continue through the later platform route toward the final flag | CP02 and CP03 provide later entry points; the camera's remaining-area follow tracks the current body |
| Finish | Touch the flag with the current living controlled body | No gate/plate or Star-only prerequisite. Display Level Complete and wait for Return to Main Menu. The unused brown `wall (20)` is deleted; completion and menu return passed the developer's subsequent test |

## 2. Scene wiring to preserve

| Scene object | Component / reference | Assigned target or behavior |
| --- | --- | --- |
| `PlayerControl` | `PlayerPossessionController.initialBody` | `StarPlayer`; scene-specific checkpoint state can move this initial body on reload |
| `Main Camera` | `level2Camera.player` / `spiderBody` | Scene PlayerControl / authored Spider body |
| `Main Camera` | Enemy-shot / chase-start events | `spikeWallSpawn.BeginEmergence` / `RequestChase`; chase start also activates `cancleSpider` |
| `SpikeWallEnemy` | `SpikeWallEnemyChase.onChaseEnded` | Main Camera → `level2Camera.FocusOnTarget` |
| `Check_transit` | `level2Checkpoint`, ID `CP01` | `respownTransit` |
| `Check_jumpOr` | `level2Checkpoint`, ID `CP02` | Its assigned `respownJump` transform |
| `Check_Mickey` | `level2Checkpoint`, ID `CP03` | Its separate assigned `respownJump` transform; do not select by name alone |
| All three checkpoint objects | Reached / respawn events | Main Camera → `BeginRemainingFollow`; see the unassigned legacy callback note below |
| `bubbleSpawner` | `BubbleSpawner.bubblePrefab` / `spawnPoint` | Shared BubbleBody prefab / unassigned, so the spawner's own position is used |
| `LevelGoal` | Player / required gate / menu scene | PlayerControl / None / `Mainmenu` |
| `LevelGoal` | `waitForMenuButton` | Enabled; `LevelTransitionUI.ShowFinalResult` creates the completion overlay |
| `Canvas/ReturnToMainMenuButton` | One OnClick callback | Its own LevelNavigationUI → `ReturnToMainMenu()` |
| `Canvas/RestartLevelButton` | One OnClick callback | Its own LevelNavigationUI → `RestartLevel()` |
| `Canvas/RetryCheckpointButton` | One OnClick callback | Its own LevelNavigationUI → `RetryCheckpoint()` |
| `Canvas (1)/MobileControls` | `MobileControlsUI.controller` / button references | PlayerControl / authored direction, action and resize buttons; saved Show Mode is AutoDetect |

**Completion:** the goal checks that the player is alive and the entering body is currently controlled. It validates the destination, records completion once, pauses gameplay and disables the player controller. The generated overlay shows **Level Complete**, **You are a super star now!**, and **Return to Main Menu**. It stays open until clicked/tapped, then clears checkpoints and loads Mainmenu with normal time scale. IntroBubble and Level01 retain their timed next-level overlays.

**Recovery:** Restart Level and R clear both checkpoint systems and reload Level02 from the entrance. Retry Checkpoint reloads while preserving the active checkpoint; without one it starts at the entrance. Automatic death reload also retains the checkpoint. Scene reload resets bodies and puzzle objects; checkpoint events restore selected stage behavior, not a complete snapshot of puzzle state. Returning to Mainmenu and re-entering starts a fresh checkpoint session. Both manual buttons currently use `TrackManualRestart`, so analytics records a manual restart rather than a death and does not distinguish the two button choices.

**Checkpoint callback follow-up:** the saved CP01, CP02 and CP03 `onRespawnHere` lists each contain a `WallSlideOnDrop.ShowWall` callback with an unassigned target (`fileID: 0`), in addition to the valid camera callback. This entry cannot restore a wall. Determine whether it is obsolete or needs a target during checkpoint regression testing; it was not repaired in this documentation pass and is not established as a current gameplay failure.

**Restart scope detail:** `spikeWallSpawn.expressionPlayed` is a separate static presentation flag reset at play-session initialization, not by `ResetCheckpointSession()`. A full-level restart resets the route/checkpoints but can skip the previously played enemy expression. Do not describe Restart Level as resetting every session-level presentation flag.

## 3. Saved layout and tuning reference

Values below describe the saved scene rather than recommended new tuning. World positions are used for gameplay objects; button positions are Canvas anchored coordinates.

| Object / setting | Saved value | Purpose |
| --- | --- | --- |
| Spider hint `cancleSpider` | Text `Q / Leave: return to star`; font 3; position `(-70, 15.2)`; size `10 × 1` | One centered line in the upper middle of the fixed Spider view; no Star-jump subtitle |
| Spider hint reveal / hide delay | `5` units / `3` seconds | Proximity-based visibility |
| FanHint | `F / Blow: wind on/off` and `Q / Leave: return to star`; font 5; size `10 × 2` | Explain wind toggle and release separately |
| Fan hint reveal / hide delay | `4` units / `2` seconds | Proximity-based visibility |
| Opening camera region | X `[-92, -75]`, Y `[10, 18]`, size `2` | Close follow during opening traversal |
| Fixed Spider camera | Position `(-70, 13, -10)`, size `4`; X `[-75, -64]` | Frame the Spider teaching/chase setup |
| Enemy cutscene | Trigger Y `14.5`; position `(-90, 12, -10)`; size `3`; duration `3s` | Reveal the enemy before chasing |
| Chase / drop | Chase starts beyond X `-64`; drop X `[-26, -16]`, start Y `10`, stop Y `-16`, size `10` | Stage-specific framing |
| Spike-wall chase | Speed `5.5`; spike lifetime override `16s`; end delay `0.3s` | Current chase pacing |
| CP01 / CP02 / CP03 respawn positions | Approximately `(-20.54, -25.74)` / `(11.26, 16.69)` / `(57.43, 11.88)` | Scene-authored checkpoint entrances |
| Bubble replacement delay | `0.5s` | Replacement after the current spawned bubble is absent/inactive |
| LevelGoal | Position `(-1.28, 18.22)`; trigger `1.4 × 1.6`, Y offset `0.8` | Reachable flag trigger, widened from the thin pole |
| Three navigation buttons | Anchors `(1, 1)`, pivot `(1, 0.5)`, size `360 × 70`, Pos X `-30` | Top-right alignment |
| Menu / Restart / Retry Pos Y | `-57` / `-142` / `-227` | Accepted spacing; do not replace with the earlier proposed Y values |
| Mobile Canvas | Screen Space - Overlay; reference resolution `1920 × 1080`, match `0.5` | Screen-fixed UI independent of gameplay-camera motion |

Mobile direction buttons are anchored at the lower left and action buttons at the lower right. The mobile scripts switch visibility by form/water/death state; they do not implement camera-follow positioning. No mobile layout change was made after the reported impression of platform-related motion. If actual drift recurs, capture it in motion before changing the Canvas.

| Form | Direction buttons | Action buttons |
| --- | --- | --- |
| Star | Left / Right | Jump |
| Box | Left / Right | Grow / Shrink (hold) |
| Bubble in air | Left / Right | Pop |
| Bubble in water | Left / Right / Up / Down | Pop |
| Spider | Left / Right | Leave |
| Fan | Left / Right | Blow (toggle), Leave |
| SpikeWall | Left / Right | Fire |
| BouncyBall | Left / Right | None; automatic bouncing |
| gravityBall | None | None; scripted motion |

Show Mode is resolved at Awake: AutoDetect uses `Application.isMobilePlatform`, or an Input System touchscreen without a mouse; AlwaysShow enables Editor mouse testing; NeverShow hides controls. There is no custom browser user-agent parser. Set the mode before Play, and restore AutoDetect before saving a release configuration. Direction/resize inputs are held; action/release inputs trigger on pointer down. Pointer up or disabling a button clears its held direction. Real-device multi-touch and detection still need testing on the newly integrated build.

## 4. Verification completed and its limits

Developer-reported evidence:

- A full keyboard pass before the presentation fixes identified the old release hints and unclear ending.
- Initial Star mobile buttons passed Editor testing. A teammate's older build reportedly worked on a phone; this is not phone validation of the current integrated revision.
- Fan hint presentation was confirmed correct; the single-line, smaller, upper-center Spider hint passed testing after the scene was reloaded.
- Restart/Retry buttons were added and their top-right layout accepted. Exhaustive behavior coverage for every checkpoint has not been separately reported.
- After removing the brown `wall (20)`, the developer confirmed goal completion and Return to Main Menu both work.

Source checks during implementation and this documentation review:

- Runtime scripts compiled with the installed Unity 6000.3.22f1 compiler; the merge pass also compiled the Editor scripts.
- The English cleanup scanned 96 script/source files and translated 30 scripts; the follow-up scan found no Chinese characters in those sources. Identifiers and executable code structure were preserved. This does not claim every serialized scene string was translated.
- The saved scene has the three local navigation callbacks, AutoDetect, the accepted hint values and no `wall (20)` object.
- The original integration preserved main's intro/Level01 scene content while adding the teammate mobile Canvas, and retained analytics, ball recovery and Box-to-Bubble handoff safeguards.

A new WebGL export, current phone/browser playtest, end-to-end analytics delivery check, and a complete post-integration intro-to-Level02 run are not recorded. The last documented publication remains the historical All-Level V2 / Alpha Progress Check build, which predates these source updates.

## 5. Decisions in this polish pass

- Use Q / Leave for Spider and Fan, Q / Pop for Bubble. Keep Space for Star jump and SpikeWall fire.
- Use a short upper-center Spider release hint; omit the redundant Star-jump line. Keep the accepted Fan hint.
- Finish on touching the flag in any living controlled form. Do not require a final pressure plate or an unprompted return to Star.
- Keep an explicit completion screen with the celebratory subtitle and a user-selected return to Mainmenu.
- Provide separate Restart Level and Retry Checkpoint actions beneath Return to Main Menu. Preserve their accepted top-right layout and existing Y positions.
- Remove only the unused brown finish block. It was a sprite/collider named `wall (20)`, not a scripted pressure plate.
- Keep mobile button positions unchanged for now. Standardize script comments, Inspector labels, tooltips and diagnostics in English without changing field/method names or gameplay logic.
- Defer top-center form identity labels and broader instruction polish.

## 6. Remaining work and handoff

1. Test Retry before any checkpoint and at CP01, CP02 and CP03; verify position, camera state and the reachable onward route. Check the unassigned ShowWall callbacks and automatic death recovery at each stage.
2. After a checkpoint, verify Restart Level and R return to the entrance and clear it, then Retry also returns to the entrance. Test Mainmenu re-entry. Treat the one-time enemy expression separately from checkpoint recovery.
3. Test every mobile form, held input release, box resizing, Bubble water transitions, Fan Blow/Leave, Spider Leave and SpikeWall Fire. On a real phone, include simultaneous movement/action touches, focus loss, aspect ratios and browser overlays.
4. Retest IntroBox → IntroBubble → Level01 → Level02 after integration, including Level01 handoff/ball/U2 recovery. Check the Level02 goal in another controlled form as well as Star, and confirm one completion analytics event.
5. Export a new WebGL build, test it locally and on desktop/mobile browsers, and verify analytics using [AnalyticsTestPlan.md](AnalyticsTestPlan.md). Follow [publish_instructions.md](publish_instructions.md) only after validation; source changes alone do not publish a build.
6. Prepare the Level-2-to-main handoff separately. This documentation update does not create a PR, merge, commit or push.
