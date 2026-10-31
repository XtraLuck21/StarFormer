# WebGL Analytics Test Plan

Last updated: 2026-10-18.

## Preconditions

- Unity version is `6000.3.22f1`.
- `AnalyticsSettings.asset` uses `environment: test`.
- `endpointUrl` is the latest Google Apps Script `/exec` deployment.
- `buildVersion` identifies the exact source revision being tested.
- The target Sheet contains an empty `RawEvents_Test` tab or no tab yet.
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
- Pop Bubble with Q. Expect Bubble `form_duration` and Star `form_change`, but
  no `death`.
- Pop Bubble on a Bubble-only hazard. Expect no `death`.
- Touch the same water hazard as Box. Expect one `death` with
  `UnderwaterHazard`.
- Press R during normal play and during the death pause. Expect `level_end` with
  `result = restart`; the reloaded level must use a new Run ID.
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
- Complete Level 2. Expect one `level_complete`, then Mainmenu, with no
  additional `abandoned` event for the completed Run.

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

- Publish the tested export at a new test/version path; do not overwrite the
  preserved Alpha Progress Check folder.
- In a private window, complete one Level 1 and one Level 2 route.
- Verify the Sheet can reconstruct both Runs using Session, Run and Attempt IDs.
- Verify every death has a valid level, form, cause and coordinate.
- Verify the sum of final `form_duration` rows is close to completion active
  time. Do not add `analytics_snapshot` durations to that sum.
- Test at least two browsers or machines and confirm two anonymous player IDs.
- Record source commit, deployment commit, fixed URL, browser versions and test
  results before switching the endpoint/table to production.

## Pass criteria

- No Analytics failure blocks movement, possession, death recovery or scene
  navigation.
- Level 1 and Level 2 share one schema and one Session when played consecutively.
- Automatic death reload retains the Run; R creates a new Run.
- Bubble popping never counts as death.
- Immediate Level 2 hazards no longer lose death events.
- Each completed Run produces exactly one `level_complete`.
- Google Sheets contains no duplicate `event_id` rows.

## October 18 final data validation

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
