# WebGL Analytics Test Plan

Last updated: 2026-10-18.

## Current release evidence

Alpha 0.1.2 is published at [the Alpha URL](https://xtraluck21.github.io/StarFormer/alpha/) as `gh-pages` commit `5f62980`, with source configuration `5650250`. Player Version and Analytics buildVersion are `alpha-0.1.2`; the release uses `production`. Localhost and online gameplay tests passed according to the developer; deployment and payload/version checks also passed. The historical data validation at the end of this document is separate evidence and does not certify Alpha 0.1.2 Sheet delivery.

Next data check: obtain the existing Sheet link/access from the analytics owner, run Level01/Level02 in the published Alpha, and confirm new `RawEvents_Production` rows contain `build_version = alpha-0.1.2`. Retain older rows and distinguish builds by that field. Inspecting an Editor log or successful page load is insufficient.

## Preconditions for a separate test build

- Unity version is `6000.3.22f1`.
- `AnalyticsSettings.asset` uses `environment: test`.
- `endpointUrl` is the latest Google Apps Script `/exec` deployment.
- `buildVersion` identifies the exact source revision being tested.
- Use `RawEvents_Test` and isolate the run by version/session IDs; preserve any existing test history. The receiver can create the tab if it does not exist.
- These test-build preconditions do not describe the already published production Alpha. Do not replace the live Alpha with a test or deliberately invalid-endpoint build.
- Build scenes are Mainmenu, IntroBox, IntroBubble, Level01 and Level02 in that
  order.

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

- Die on an ordinary spike while controlling Star. Expect one `death` with
  `death_cause = Spike` and the Star's world coordinates.
- Possess the lower Bouncy Ball. Expect one `checkpoint_reached` whose ID is
  the ball instance name and a new `attempt_start`.
- Die while controlling that ball. Expect the same Run ID after scene reload,
  a new Attempt ID and Bouncy Ball form at respawn.
- Possess the upper ball and repeat the checkpoint/death test.
- Pop Bubble with E. Expect Bubble `form_duration` and Star `form_change`, but
  no `death`.
- Pop Bubble on a Bubble-only hazard. Expect no `death`.
- Touch the same water hazard as Box. Expect one `death` with
  `UnderwaterHazard`.
- Press R during normal play and during the death pause. Expect `level_end` with
  `result = restart`; the reloaded level must use a new Run ID.
- Click Level01 Restart Level after a ball checkpoint. Expect `level_end` with `result = restart`, a new Run ID and a fresh level entrance with cleared checkpoints; it must not resume the ball.
- Finish the level. Expect exactly one `level_complete`; Level 2 must start with
  a different Run ID in the same Session.

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
- Automatic death reload retains the Run; R, Restart Level and Retry Checkpoint create a new Run. Retry alone preserves gameplay checkpoint state.
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
