# WebGL Analytics Test Plan

For the seven-scene analytics v2 integration, real maps and test/production
rollout, see [AnalyticsAllLevels.md](AnalyticsAllLevels.md).

Last updated: 2026-11-06.

## Beta 0.1.2 acceptance status

Beta 0.1.2 was published on November 6, 2026 to both the [main website](https://xtraluck21.github.io/StarFormer/) and [Beta website](https://xtraluck21.github.io/StarFormer/beta/). The same `Builds/WebGL-Beta-0.1.2` export was committed and pushed on `gh-pages` as [`64b35c6`](https://github.com/XtraLuck21/StarFormer/commit/64b35c6c5dbe50ffefc05e1590533cebf47888dc). [GitHub Pages deployment](https://github.com/XtraLuck21/StarFormer/actions/runs/42303613672) succeeded. All seven files at each URL (14 files total) returned HTTP 200 and matched the local export byte for byte by SHA-256. The source checkout at publication was clean main `b93768f`; Player/Analytics versions are `beta-0.1.2`, using the existing production endpoint. This verifies deployment and package integrity, not a new hosted gameplay playtest, mobile acceptance or production analytics delivery. Analytics map export/backend updates were not performed in this publication; Level02 repairs remain deferred to its replacement scene. See the [release record](Beta-0.1.2.md).

| Check | Status / required evidence |
| --- | --- |
| Version and scene list | Published configuration: Navigation 0, seven gameplay scenes 1–7, IntroFan at 6 |
| Map freshness | Pre-publication review found all seven maps stale; no export/backend update in this publication; outstanding for future build/data acceptance |
| Closet/book behavior | Unified session-only discovery; inspect the regression evidence below and repeat on the Web export |
| Help migration | Obsolete components removed from IntroBox, IntroBubble and IntroSpider; focused Editor Help checks passed (below). Level02 migration deferred to its replacement scene |
| Gameplay | Recheck PR #21 Level01 fixes, Navigation contact/click entries, shared Home/Help, current Spider hints and forward/return passages |
| Web build and publication | Complete: `Builds/WebGL-Beta-0.1.2` published to root and `/beta/` via `64b35c6`; deployment successful and all 14 hosted files matched |
| Data and dashboard | Pending; actual rows must carry beta-0.1.2 and the matching exported map version; verify IntroFan and historical FanTutorial separately |

Navigation may participate in session initialization but must not create a tracked gameplay Run. Each selected gameplay destination must create its own Run; verify forward completions, backward returns, death, restart/retry, abandonment and paused/focus time as below. Collection Book pause must not add active gameplay time. Recording/rehearsal on the release configuration can add production events; keep that fact in mind when interpreting the dataset.

Use [Beta-0.1.2.md](Beta-0.1.2.md#unity-and-browser-acceptance-before-recording) for the gameplay/recording checklist. Leave the historical evidence below attached to its original release.

### Session discovery regression

The executable [SessionDiscoveryVerification.cs](../Assets/Editor/SessionDiscoveryVerification.cs) check runs only in a disposable project copy. Set its Company Name to `StarFormerVerification` and use a separate Product Name; then launch Unity with `-batchmode -projectPath <copy> -executeMethod SessionDiscoveryVerification.RunBatch` (without `-quit`). It writes `starformer-session-discovery-results.txt` to the system temporary directory and exits with the test result. This isolates the Company/Product storage namespace. It exercises the real Navigation slot/book bindings and player ownership handoff, not a replacement unlock model. Old completion keys are seeded and restored by the test. No actual user progress is cleared.

Acceptance cases: five locked icons and locked form pages on fresh launch; Star help always available; direct Level02 entry and completion grant no unused forms; early Kindergarten exit grants none; individual possession updates the corresponding icon/page; death, checkpoint retry, level restart and Navigation returns retain discoveries; skipped basic school reveals only Spider/Fan after controlling them; a second Editor Play session clears discoveries even with domain reload disabled. Build/browser startup, mobile behavior and Web storage handling still need exported-player acceptance. November 6 isolated Unity 6000.3.22f1 execution passed these discovery checks, including two Play sessions with domain reload disabled. That earlier run separately reported missing-button/panel errors from legacy gameplay Help components; it did not certify those panels or the Level02 migration. Ownership tests invoke the real successful handoff after initialization; they do not replace manual collision/movement playtests.

### Tutorial Help cleanup verification

A subsequent November 6 isolated Unity 6000.3.22f1 Play Mode check covered IntroBox, IntroBubble and IntroSpider after removing only their obsolete Help components. Each scene had exactly one six-page book and no runtime errors or Help warnings. The check exercised the configured open/close/backdrop/next/previous button callbacks and simulated Escape input, confirmed the gameplay clock stopped while open and advanced after close, preserved an existing pause and non-default time scale, and checked disable/re-enable plus scene exit while open. Scene diffs also confirmed all other objects, components and serialized book content were unchanged. The isolated copy used a separate Company/Product storage namespace; user progress was untouched.

This focused check did not test Level02, export analytics maps, build WebGL or verify browser behavior. Level02 work is deferred to the incoming replacement scene.

## Beta 0.1.1 acceptance status

Beta 0.1.1 was published on November 3, 2026 at [the Beta URL](https://xtraluck21.github.io/StarFormer/beta/), from source `9451511` as `gh-pages` commit `e81097e`. The completed export is `Builds/WebGL-Beta-0.1.1`. Player/Analytics versions are `beta-0.1.1`; the released source enables production collection for all seven gameplay scenes and contains refreshed maps and the Level01 door hint.

On November 5, all seven live Beta files returned HTTP 200 and matched `Builds/WebGL-Beta-0.1.1` and the publishing checkout byte for byte. This confirms the deployed package, not a new gameplay playtest or production analytics acceptance.

| Check | Evidence / status |
| --- | --- |
| Build and publication | Complete: owner confirmation, source/publishing record and live package comparison |
| Live version and payload | Passed November 5: all seven files match the completed 0.1.1 export |
| Detailed desktop/touch gameplay | Not newly exercised in this documentation update |
| Production event delivery and raw-row metrics | Not yet verified for 0.1.1; 0.1.0 evidence does not certify this release |
| Refreshed backend map deployment/rendering | Not verified; the game deployment does not deploy Apps Script |

Remaining checks against the published export:

- Confirm new event rows use `beta-0.1.1`. Play Mainmenu → IntroBox → IntroBubble → IntroBouncyBall → Level01 → IntroSpider → FanTutorial → Level02. Each gameplay scene should create its own run and completion; Mainmenu stays untracked.
- Test backward passages and `scene_return` / `entry_reason = return`, full restart, checkpoint retry, death, pause/focus loss and form-duration totals. Check actual Sheet rows and duplicate handling.
- Test the Level01 20-second door hint, deliberate contact activation, paused preview/resume and once-per-scene-load behavior on desktop and touch.
- Verify the refreshed backend map catalog is deployed; synchronize it if needed. Confirm new `map_version` values resolve correctly, historical builds keep their maps and unknown versions still show a coordinate grid.
- Record the browsers/devices and scenarios actually tested. Preserve the 0.1.1 export and keep historical evidence separate.

The November 4 paged help book, six-form demo animations, Unity Recorder package and Mainmenu layout/behavior changes (`3a03844`, `f05d433`, `55f6b6b`, `f2c5c48`) are integrated into `main` but are **not included in published Beta 0.1.1**. Those runtime features are now included in 0.1.2; their current acceptance belongs to the 0.1.2 checks above, not the historical 0.1.1 package.

## Beta 0.1.0 acceptance status

Historical evidence below refers to the November 2 export; both root and `/beta/` now serve 0.1.2. The map-binding follow-up was subsequently resolved by dashboard Version 9; retain the original observation and distinguish it from current 0.1.1 map checks.

Beta 0.1.0 was published on November 2, 2026, from source revision `3a693ca` as gh-pages commit `1105970`. The final WebGL package was rebuilt after version/template synchronization; the developer reported local and hosted playtests passed. [Deployment](https://github.com/XtraLuck21/StarFormer/actions/runs/41410712039) succeeded at [the Beta URL](https://xtraluck21.github.io/StarFormer/beta/). All seven hosted files matched the tested export. Player/Analytics versions are `beta-0.1.0`; the existing production endpoint and Level01/Level02 tracking configuration are unchanged.

| Check | Evidence / status |
| --- | --- |
| Final build and local gameplay | Passed, developer reported after rebuilding with the synchronized versions/template |
| Pages deployment and payload | Passed; gh-pages `1105970`, all seven live files matched the export |
| Hosted gameplay | Passed, developer reported; browser/device versions and an exhaustive scenario matrix were not supplied |
| Beta records received | Developer confirmed; screenshot shows production Beta Level01 records, completion and death |
| Level02-specific delivery and raw-row audit | Still to verify; not demonstrated by the Level01 screenshot |
| Matching real map | Initially showed a grid; Version 9 subsequently supplied release-matched maps (see AnalyticsDashboard.md) |
| Metric correctness, deduplication and broad device coverage | Remain targeted checks below; not certified by aggregate dashboard counts |

Screenshot evidence supplied November 2: Production / `beta-0.1.0` / Level 1 / All players / Normal entries shows 2 anonymous players, 7 runs, 79 events, 1 of 7 observed starts completed (14.3%), 1 death, 13 attempts, 3 abandoned and 0 scene returns. This is a temporary filtered sample; the header's 5,334 production events is not the Beta-only count. No raw Sheet rows were directly inspected in this documentation update.

### Historical dashboard map/version follow-up

1. Inspect Beta `RawEvents_Production` rows, their build/scene identifiers and any map metadata alongside the deployed dashboard's resolver. Determine whether the issue is a missing map asset, missing build-to-map association or missing version metadata; the screenshot alone does not establish the cause.
2. Add the map corresponding to released source `3a693ca`, preserving world coordinates, scale and orientation. Associate Beta Level01 with that map, not Level01_Legacy or the old Alpha layout. Retain historical map associations and the unknown-version grid fallback.
3. Use a known spike death to compare its raw coordinates with the plotted position on the correct map. Check each displayed map/scene separately. Level02 needs its own verified association; do not infer it from Level01.
4. Account for SAFE/NORMAL/HARD and moving/falling obstacles. If their runtime state is absent, label the map as a base-layout reference rather than claiming an exact obstacle-state replay. Review existing payloads before deciding whether future telemetry/schema changes are needed.
5. After the dashboard update, confirm the same Beta filters show the correct map and consistent event totals; switching to Alpha must retain the historical map, and an unknown version must still use the grid. Preserve all recorded events.

For the historical 0.1.0 collector only, verify Level01 and Level02 Runs, Session/Attempt IDs, checkpoint events, deduplication and active-time calculations against actual rows. IntroBouncyBall is untracked in 0.1.0 and must not create a tracked level Run; passage backtracking must not create spurious Level01 completion/death records. Record the browsers/devices and scenarios actually tested.

## Historical Alpha 0.1.2 release evidence

Alpha 0.1.2 is published at [the Alpha URL](https://xtraluck21.github.io/StarFormer/alpha/) as `gh-pages` commit `5f62980`, with source configuration `5650250`. Player Version and Analytics buildVersion are `alpha-0.1.2`; the release uses `production`. Localhost and online gameplay tests passed according to the developer; deployment and payload/version checks also passed. The historical data validation at the end of this document is separate evidence and does not certify Alpha 0.1.2 Sheet delivery.

Historical Alpha follow-up: obtain the existing Sheet link/access from the analytics owner, run Level01/Level02 in the published Alpha, and confirm new `RawEvents_Production` rows contain `build_version = alpha-0.1.2`. Retain older rows and distinguish builds by that field. Inspecting an Editor log or successful page load is insufficient.

## Preconditions for a separate test build

- Unity version is `6000.3.22f1`.
- `AnalyticsSettings.asset` uses `environment: test`.
- `endpointUrl` is the latest Google Apps Script `/exec` deployment.
- `buildVersion` identifies the exact source revision being tested.
- Use `RawEvents_Test` and isolate the run by version/session IDs; preserve any existing test history. The receiver can create the tab if it does not exist.
- These test-build preconditions do not describe the production Beta release configuration or the published Alpha. Keep test and deliberately invalid-endpoint exports separate from both release paths.
- Enabled build scenes, in index order: Navigation, IntroBox, IntroBubble, IntroBouncyBall, Level01, IntroSpider, IntroFan, Level02. Legacy/reference/sandbox scenes stay disabled.
- Test current Level01 (former Advanced), not Level01_Legacy. These source scenarios do not describe the old Alpha 0.1.2 map; use a new version label for the new export.

## 1. Editor validation

1. Enter Play Mode from Navigation.
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
- Complete as Star, Box, Bubble and BouncyBall in separate runs. Each accepted flag touch emits exactly one `level_complete` before IntroSpider. In the new source, IntroSpider → IntroFan is also tracked; Level02 later starts a different Run in the same Session. Returning to Navigation after completion must not append abandonment to the completed Level01 Run.

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

- Use a separate path for a dedicated analytics test export. This Beta release uses only `/beta/`; preserve `/alpha/`, `playtest-01/`, Alpha Progress Check and the root.
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

## 7. Dashboard acceptance

After deploying `AnalyticsBackend/Dashboard.html` and the updated `Code.gs`:

1. Open the Web App `/exec` URL and confirm the dashboard loads without a
   Google Sheet share link.
2. Open `/exec?health=1` and confirm the receiver still reports `ok: true`.
3. Compare the selected build/level counts with `RawEvents_Production`.
4. Confirm death points, completion-time distributions, checkpoint travel-time
   averages and form-duration totals match representative source rows.
5. Create a new production event and keep the page open. Confirm it appears
   after the 30-second cache and refresh cycle without redeploying.
6. Change Build and Level while a refresh is running. Confirm the page does not
   issue overlapping requests or display a stale selection.
7. Test the intended access policy in a signed-out/private window.

See [AnalyticsDashboard.md](AnalyticsDashboard.md) for definitions and the
manual deployment checklist.

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


## November 2: aggregate dashboard acceptance

See [AnalyticsDashboard.md](AnalyticsDashboard.md) for current four-metric
calculations, release-map provenance and cloud test results. The normal-entry
view always combines all players. Regression tests cover first-try success,
multiple respawns before passing, incomplete/missing-start intervals, out-of-order
and duplicate uploads, completion distributions and exact historical backgrounds.
The /dev preview uses real production reads; synthetic browser fixtures remain
local and are never uploaded. The owner approved publication; Web App Version 7 was released November 2,
2026 at 13:52 Los Angeles time using the original URL.


## Main integration verification (November 2)

This source adds the new IntroBouncyBall scene to the earlier six-scene collector.
The Editor-only verification exercises all seven scenes without POST requests,
including run identity across death respawns, checkpoint clocks, return exits,
manual-restart reasons and once-only completion. Backend tests validate the four
current metrics, historical maps and zero-event tutorial previews. Integration
scope validation compares gameplay/assets/settings to the fetched main baseline;
only literal analytics-hook insertions are allowed in shared gameplay scripts.
Evidence and commands are recorded in AnalyticsAllLevels.md after execution.
