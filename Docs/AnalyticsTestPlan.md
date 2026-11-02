# WebGL Analytics Test Plan

Last updated: 2026-11-01.

## Last recorded published release evidence

Alpha 0.1.2 is published at [the Alpha URL](https://xtraluck21.github.io/StarFormer/alpha/) as `gh-pages` commit `5f62980`, with source configuration `5650250`. Player Version and Analytics buildVersion are `alpha-0.1.2`; the release uses `production`. Localhost and online gameplay tests passed according to the developer; deployment and payload/version checks also passed. The historical data validation at the end of this document is separate evidence and does not certify Alpha 0.1.2 Sheet delivery.

Next data check: obtain the existing Sheet link/access from the analytics owner, run Level01/Level02 in the published Alpha, and confirm new `RawEvents_Production` rows contain `build_version = alpha-0.1.2`. Retain older rows and distinguish builds by that field. Inspecting an Editor log or successful page load is insufficient.

## Preconditions for a separate test build

- Unity version is `6000.3.22f1`.
- `AnalyticsSettings.asset` uses `environment: test`.
- `endpointUrl` is the latest Google Apps Script `/exec` deployment.
- `buildVersion` identifies the exact source revision being tested.
- Use `RawEvents_Test` and isolate the run by version/session IDs; preserve any existing test history. The receiver can create the tab if it does not exist.
- These test-build preconditions do not describe the already published production Alpha. Do not replace the live Alpha with a test or deliberately invalid-endpoint build.
- Enabled build scenes, in index order: Mainmenu, IntroBox, IntroBubble, Level01, Level02, IntroSpider, FanTutorial. Legacy/reference/sandbox scenes stay disabled.
- Test current Level01 (former Advanced), not Level01_Legacy. These source scenarios do not describe the old Alpha 0.1.2 map; use a new version label for the new export.

## 1. Editor validation

1. Enter Play Mode from Mainmenu.
2. Confirm the Console prints one `session_start` JSON event.
3. Open Level 1 and confirm `level_start` and `attempt_start` share one
   `level_run_id`.
4. Possess at least two forms and confirm `form_change` and `form_duration`
   events appear only after successful contact.
5. Confirm there are no compiler errors or missing-script messages.

Editor logs validate gameplay wiring and JSON. They do not validate Google
Sheets transport.

## 2. Level 1 scenarios

- Reach each hollow-star marker in order. Expect one `checkpoint_reached` with `ADV_CP1`, `ADV_CP2`, then `ADV_CP3`, and a new `attempt_start` at each accepted save. Possessing R1/R2 alone and revisiting old markers must not create additional checkpoints/attempts.
- Die after each checkpoint. Expect one `death`, the same Run ID across automatic reload, a new Attempt ID and the configured Box / BouncyBall / Star respawn respectively. At CP3, arriving as Box still restores Star; Bubble flying overhead does not save.
- In NORMAL or HARD, die on an ordinary spike. Expect `death_cause = Spike` and the controlled form's coordinates. Trigger the HARD falling spike and test contact during descent and after landing; each death must emit once. Retry rearms the trap.
- Select SAFE/HARD and raise the upper lift/break the RUIN cover, then die or use Return to Checkpoint. Confirm the completed route/lift/cover progress survives while loose bodies, lower door and motion/trap cycles reset. Earlier markers cannot replace a later save.
- Pop Bubble with Q. Expect Bubble `form_duration` and Star `form_change`, not `death`. Check both three-second Q lessons and the first-tank R lesson; no old generic E reminder should appear. Bubble-only hazard behavior is a separate shared/legacy fixture, not a current Level01 terrain prerequisite.
- Open/cancel R and Home during normal play; test R during the death pause. Opening/cancelling must not emit restart `level_end` or create a new Run. Confirm full restart: expect `result = restart`, a new Run and cleared gameplay checkpoints/route progress. Confirm Return to Checkpoint: expect the same restart event category and a new Run, but retained gameplay progress. Manual retry is not a death.
- Home's checkpoint action is disabled before a save. Cancel both full-restart and menu confirmations without changing gameplay/analytics state. Confirm Main Menu from an unfinished run: expect abandonment and cleared checkpoint state.
- During SAVE previews, hazard transitions and paused menus, active timers must not advance. SAFE's full alternating-board demonstration should not add form time.
- Complete as Star, Box, Bubble and BouncyBall in separate runs. Each accepted flag touch emits exactly one `level_complete` before IntroSpider. IntroSpider → FanTutorial remains untracked; Level02 later starts a different Run in the same Session. Returning to Mainmenu after completion must not append abandonment to the completed Level01 Run.

## 3. Level 2 scenarios

- Activate CP01, CP02 and CP03. Each new ID should produce one
  `checkpoint_reached` and one new `attempt_start`.
- Die after each checkpoint. The Level Run must remain unchanged while the
  Attempt ID changes.
- Touch a Moving Spike configured for immediate restart. Expect one
  `death_cause = MovingSpike` row before reload.
- Touch the chasing Spike Wall configured for immediate restart. Expect one
  `death_cause = SpikeWall` row before reload.
- Exercise Star, Box, Bubble, SpikeWall, Spider, Fan, gravityBall and
  BouncyBall when reachable. Confirm form names are strings, not enum numbers.
- Click Restart Level after a checkpoint: expect a manual-restart `level_end`, a new Run ID and the level entrance.
- Click Retry Checkpoint: expect the same manual-restart event category and a new Run ID, but restore the latest gameplay checkpoint (or the entrance if none exists). Neither button emits a death; the schema does not identify which button was used.
- Complete Level 2. Expect one `level_complete` and a paused Level Complete / You are a super star now! overlay. Wait, then click Return to Main Menu. Expect no duplicate completion or additional `abandoned` event for the completed Run.

## 4. Time validation

Use a stopwatch with a tolerance of about one second:

- Play actively for ten seconds: active counters should increase by about ten.
- Leave the browser tab for ten seconds: active counters should not increase by
  ten.
- Wait during the two-second death pause: completion/form active time should
  not include that pause.
- Activate a checkpoint, wait and die: `attempt_active_seconds` should measure
  from that checkpoint, not from level start.
- After several deaths, completion time should retain prior active attempts in
  the same Run.

## 5. Transport and deduplication

1. Build and Run WebGL through localhost; do not open `index.html` directly.
2. Perform a short Level 1 run and inspect `RawEvents_Test`.
3. Confirm `player_id` remains the same after reopening in the same browser,
   while `session_id` changes after a full page reload.
4. Temporarily use an invalid endpoint, create events, restore the valid
   endpoint in a new build/session and verify later traffic does not stop the
   game. The browser queue is bounded to 256 entries.
5. Submit the same JSON twice directly to Apps Script. Confirm one Sheet row is
   stored for that `event_id`.
6. Inspect Apps Script Executions for validation errors and lock timeouts.

Because `no-cors` responses are opaque, a resolved browser request is not proof
of a successful append. The Sheet is the source of truth.

## 6. GitHub Pages acceptance

- Use a separate path for a dedicated analytics test export. Approved Alpha releases replace only `/alpha/`; preserve `playtest-01/`, Alpha Progress Check and the root.
- In a private window, complete one Level 1 and one Level 2 route.
- Verify the Sheet can reconstruct both Runs using Session, Run and Attempt IDs.
- Verify every death has a valid level, form, cause and coordinate.
- Verify the sum of final `form_duration` rows is close to completion active
  time. Do not add `analytics_snapshot` durations to that sum.
- Test at least two browsers or machines and confirm two anonymous player IDs.
- Record source commit, deployment commit, fixed URL, browser versions and test
  results. A test-to-production release changes the saved environment and rebuilds; the existing receiver routes each environment to its table. Alpha 0.1.2 already uses production and does not require a backend or endpoint change.

## Pass criteria

- No Analytics failure blocks movement, possession, death recovery or scene
  navigation.
- Level 1 and Level 2 share one schema and one Session when played consecutively.
- Automatic death reload retains the Run; confirmed Restart Level, Return to Checkpoint and Retry Checkpoint create a new Run. The latter two preserve gameplay checkpoint state; opening/cancelling R does not create a new Run.
- Bubble popping never counts as death.
- Immediate Level 2 hazards no longer lose death events.
- Each completed Run produces exactly one `level_complete`.
- Google Sheets contains no duplicate `event_id` rows.

## Historical October 18 data validation (before Alpha 0.1.2)

Level 1 was verified from the published WebGL test data in `RawEvents_Test`.
One completed run contained Star, Box, Bubble and BouncyBall changes, both
`BouncyBall_Lower` and `BouncyBall_Upper` checkpoints, 12 distinct attempts,
nine Spike deaths with positions and one `level_complete` event. Form-change
and form-duration rows were both present.

Level 2 received an additional Unity 6000.3.22f1 Play Mode integration check.
The check entered Level02 through `MainMenuUI`, exercised the real controller,
CP01, death reload and `LevelGoalToMenu` paths, and captured editor analytics
JSON. The resulting run contained one `level_start`, four distinct attempts,
one CP01 event, one pre-checkpoint MovingSpike death, one post-checkpoint
SpikeWall death, eight form-change values and exactly one `level_complete`.
Both deaths included the expected checkpoint, form, cause and coordinates.

The Level 1 and Level 2 evidence used different `level_run_id` values. The
temporary Play Mode validation runner was removed after the passing run; it is
not production code. Google Sheets transport remains covered by the published
WebGL test, while the Level 2 Play Mode check validates gameplay-to-analytics
wiring without uploading editor events.
