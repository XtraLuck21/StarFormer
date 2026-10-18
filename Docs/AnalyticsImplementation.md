# WebGL Analytics Implementation

Last updated: 2026-10-18.

## Scope

The analytics layer records anonymous playtest events for `Level01` and
`Level02`. IntroBox, IntroBubble, the old Tutorial and sandbox scenes are not
included in the four formal metrics.

The implementation does not collect names, email addresses, USC IDs, IP
addresses or Google account information. `player_id` is a random UUID stored in
the browser through Unity `PlayerPrefs`. Clearing site data, using a private
window or changing browsers creates a different anonymous player.

## Data flow

```text
Unity gameplay event
  -> AnalyticsManager JSON event
  -> AnalyticsWebGL.jslib persistent browser queue
  -> Google Apps Script Web App
  -> RawEvents_Test in Google Sheets
```

The WebGL transport uses cross-origin `fetch` with `mode: "no-cors"` and
`sendBeacon` during `pagehide`. Events remain in a bounded browser
`localStorage` queue after a network failure and are retried when another event
is sent. Each event has an `event_id`; the Apps Script checks that column before
appending so a retry does not create a second row.

An opaque `no-cors` response cannot prove that Apps Script accepted the event.
Acceptance testing must inspect the Sheet and Apps Script execution history.

## Runtime files

| File | Responsibility |
| --- | --- |
| `Assets/Scripts/Analytics/AnalyticsManager.cs` | Persistent IDs, run/attempt state, active-time counters, form segments and event creation |
| `Assets/Scripts/Analytics/AnalyticsEvent.cs` | JSON schema shared by all events |
| `Assets/Scripts/Analytics/AnalyticsEnums.cs` | Stable death-cause and checkpoint names |
| `Assets/Scripts/Analytics/AnalyticsSettings.cs` | Web endpoint, environment, build label and tracked scenes |
| `Assets/Resources/AnalyticsSettings.asset` | Saved runtime configuration loaded before the first scene |
| `Assets/Plugins/WebGL/AnalyticsWebGL.jslib` | Non-blocking WebGL upload and browser retry queue |
| `AnalyticsBackend/Code.gs` | Google Apps Script receiver, validation, locking and duplicate protection |

`AnalyticsManager` is created by a runtime bootstrap before the first scene and
uses `DontDestroyOnLoad`. Do not add separate copies to Level 1 or Level 2.

## IDs and lifecycle

- `player_id`: persists in the same browser and origin.
- `session_id`: new every time the WebGL page loads.
- `level_run_id`: new when Level 1 or Level 2 starts from the beginning.
- `attempt_id`: new at level start, checkpoint activation and death respawn.

Automatic death reload retains the current Level Run. Pressing R, using the
Restart button, or entering the level again starts a new Run. Returning to the
menu records the active Run as `abandoned`.

Level 1 uses `BouncyBall_Lower` and `BouncyBall_Upper` as checkpoint IDs. Level
2 uses the existing `CP01`, `CP02` and `CP03` IDs. The attempt timer resets when
a checkpoint is activated and again when the player respawns there.

## Formal metrics

1. **Death locations:** use `death.position_x`, `death.position_y`,
   `controlled_form`, `death_cause` and `level_name`.
2. **Time to death after the latest start/checkpoint:** use
   `death.attempt_active_seconds`.
3. **Level completion time:** use `level_complete.level_active_seconds`; this
   includes active time across death reloads in the same Run.
4. **Time in each controlled form:** sum `form_duration.duration_seconds` by
   `level_run_id` and `controlled_form`.

`analytics_snapshot.duration_seconds` is the still-open form segment at the
time of the snapshot. Do not sum snapshots with final `form_duration` rows.
Snapshots are recovery/debug records for interrupted pages.

Active time advances only while a tracked Run is active, the player is not in
a death/completion pause, `Time.timeScale` is positive and the page has focus.

## Event types

| Event | Meaning |
| --- | --- |
| `session_start` | WebGL page or Editor play session started |
| `level_start` | New Level Run began |
| `attempt_start` | Start, checkpoint activation or death respawn began an attempt |
| `checkpoint_reached` | A different checkpoint became active |
| `form_change` | A successful possession or voluntary release changed control |
| `form_duration` | A completed controlled-form time segment |
| `death` | A real player death; Bubble popping is not a death |
| `level_complete` | Level goal completed once |
| `level_end` | Restarted or abandoned without completion |
| `analytics_snapshot` | Periodic best-effort state for interrupted sessions |

Death causes currently include `Spike`, `UnderwaterHazard`, `MovingSpike`,
`SpikeWall`, `OutOfBoundsLow`, `OutOfBoundsHigh`, `Other` and `Unknown`.

## Google Sheet setup

1. Create the team's private Google Sheet.
2. Open **Extensions -> Apps Script**.
3. Copy `AnalyticsBackend/Code.gs` into the bound script.
4. Keep `ANALYTICS_SHEET_NAME = 'RawEvents_Test'` during development.
5. If the script is not bound to the target Sheet, set Script Property
   `STARFORMER_ANALYTICS_SHEET_ID` to its spreadsheet ID.
6. Deploy as a Web App, executing as the Sheet owner and allowing anonymous
   access needed by the public WebGL build.
7. Copy the deployment `/exec` URL into
   `Assets/Resources/AnalyticsSettings.asset` as `endpointUrl`.
8. Create a new deployment version after changing `Code.gs`; editing the source
   alone does not update an existing deployment.

The endpoint is visible in the public WebGL files and must not be treated as a
secret. Do not place API keys, OAuth tokens or account credentials in Unity.

Before a release, update `buildVersion` to the exact source commit or agreed
release label. Use `environment: test` and `RawEvents_Test` until the full test
plan passes.

## Gameplay integration points

- `PlayerPossessionController` begins/resumes Runs, tracks successful form
  changes, records checkpoint changes and routes all deaths through one method.
- Height-limit deaths distinguish the lower and upper boundary.
- `SpikeHazard`, `BubblePopHazard`, `MovingSpikeHazard` and
  `SpikeWallEnemyChase` provide explicit death causes.
- Immediate Level 2 hazards now use zero-delay `KillControlledBody` rather than
  bypassing death handling with `RequestRestart`.
- `LevelGoal` records Level 1 completion before the transition overlay.
- `LevelGoalToMenu` records Level 2 completion before returning to Mainmenu.
- `LevelNavigationUI` records restart/abandonment before loading another scene.

## Editor and release behavior

The Editor prints event JSON when `logPayloadsInEditor` is enabled and does not
contact Google. Uploading is compiled only for WebGL outside the Editor.

If `endpointUrl` is blank, WebGL logs one warning and gameplay continues without
uploading. This is the committed safe default until the team supplies its test
deployment URL.

Use Unity `6000.3.22f1` and the scene/build configuration documented in
`publish_instructions.md`. Source integration does not update GitHub Pages; a
new WebGL export and separate `gh-pages` publication are still required.
