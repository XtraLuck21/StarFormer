# Analytics: all gameplay scenes and real maps

Initial implementation: November 1, 2026. Main integration: November 2, 2026,
based on fetched `main` revision `c9a43c2`. Historical test/deployment evidence
below retains its original six-scene scope and dates.
No difficulty selection or difficulty analytics are introduced.

## Published Beta 0.1.1 status — November 5 update

Beta 0.1.1 was published on November 3, 2026 at [the Beta URL](https://xtraluck21.github.io/StarFormer/beta/), from source `9451511` as `gh-pages` commit `e81097e`. Its released configuration uses production collection, version `beta-0.1.1`, all seven gameplay scenes and refreshed maps. On November 5, all seven live Beta files returned HTTP 200 and matched `Builds/WebGL-Beta-0.1.1` and the publishing checkout byte for byte. This confirms the deployed package, not a new gameplay playtest or production analytics acceptance. The refreshed backend catalog deployment and 0.1.1 production raw rows remain separately unverified. See [AnalyticsTestPlan.md](AnalyticsTestPlan.md#beta-011-acceptance-status).

The November 4 paged help book, six-form demo animations, Unity Recorder package and Mainmenu layout/behavior changes (`3a03844`, `f05d433`, `55f6b6b`, `f2c5c48`) are integrated into `main` but are **not included in published Beta 0.1.1**.

## Coverage and lifecycle

Track IntroBox, IntroBubble, IntroBouncyBall, Level01, IntroSpider, FanTutorial
and Level02 (all seven enabled gameplay scenes).
Exclude Mainmenu, Tutorial, reference scenes and sandboxes. Keep the existing
anonymous browser ID; a page load creates a session and each fresh scene entry
creates a run. Death respawns retain that run and start a new attempt.

Tutorial forward exits and goals, including IntroBouncyBallGoal, now report
completion once. The existing Level01 and Level02 completion hooks remain.
IntroBubble's backward exit reports `level_end.result = scene_return`, and the
destination run has `entry_reason = return`. Opening/cancelling the restart menu
does not emit events. Confirmed full restarts use `restart_level`; checkpoint
retries use `retry_checkpoint`. Both create new runs under the existing design.

Active time excludes timeScale pauses and focus loss. Completed form-duration
segments are flushed every 30 active seconds, on focus/pause/quit notifications,
on switching, death, completion and exit. Snapshots remain diagnostic; never sum
them with form_duration. A sudden browser/process termination may still lose
the last partial segment or pending network deliveries. Editor tests log only.

## Schema v2 and safe table migration

Append `map_version` and `entry_reason` after the 27 existing v1 columns. Accept
both schema versions 1 and 2. Legacy rows remain unchanged and are interpreted
as `map_version = legacy`, `entry_reason = normal`. A missing new map is
`unmapped`, never silently replaced with the latest image.

`ensureHeader_` validates the complete existing header. For v1 it copies the
sheet to `RawEvents_*_BackupV1_<timestamp>` before appending two columns. It
does not clear rows or change IDs. Unexpected headers stop the migration.
`migrateAnalyticsSheets()` upgrades both sheets under a script lock. A POST
also performs the idempotent migration if necessary. The dashboard itself
does not migrate or edit an existing table.

`event_id` prevents duplicate POST inserts. Dashboard reads also deduplicate
event IDs; run completion is counted once per level_run_id. A completion with
no observed start is shown but excluded from the completion-rate numerator.
Death positions must be explicitly present and finite; old missing positions
are counted as missing positions and are not plotted at (0,0).

## Real maps and matching

Use Unity 6000.3.22f1, menu **Analytics > Export all real level maps** or batch
method `AnalyticsMapExporter.ExportAllBatch`. The tool uses disposable preview
scenes, never saves gameplay scenes, and hides Canvas/TMP instructions. It
renders the actual initial terrain, water, hazards and bodies using an
orthographic camera. Dynamic positions and open doors at death are not replayed.

Outputs:

- `AnalyticsBackend/Maps/<scene>/<map-version>.png`: versioned source images.
- `AnalyticsBackend/Maps/manifest.json`: current map bounds and image metadata.
- `Assets/Resources/AnalyticsMapCatalog.json`: matching runtime version labels.
- `AnalyticsBackend/MapCatalog.html`: JSON with versioned embedded PNGs, for
  Apps Script. Export preserves previously exported versions in this catalog.

Each map version combines a scene-file hash and image hash. Runtime events
carry that exact version; the dashboard filters by build, level and map version
and requests only the selected image through `getDashboardMap`. The image is
served by the same Web App, not a private Drive link or a GitHub Pages dependency.
The client caches images separately from the 30-second data refresh.

Image edges correspond to min_x/max_x/min_y/max_y. SVG uses world X and -Y;
the image rectangle and points share that transform. Aspect ratios are preserved.
Out-of-bounds death positions expand the fit region rather than being clamped.
Dragging, wheel/buttons and Fit map support long and multi-floor scenes.
Legacy/missing-map selections show a labeled coordinate grid; they are never
drawn on the current map. Keep old exports when changing layout.

The WebGLBuild method rejects missing or stale scene hashes. Re-export maps
after scene/layout changes. Direct Unity Build commands should follow the same
export/check procedure. Changes to sprite materials/prefabs should also trigger
a fresh export even when scene YAML is unchanged.

## Dashboard

**November 2 revision:** see [AnalyticsDashboard.md](AnalyticsDashboard.md) for
current aggregate-only requirements, exact metric definitions and historical
release-map bindings. The four sections are death locations, checkpoint travel time
(including active play across death retries), completion time and form usage. Source,
build, level and map filters remain; player/visit/form/cause selectors and
individual records are removed. All players and normal entries are enforced
server-side. Raw player/session/run IDs never reach the browser.

Test/Production caches remain isolated and expire after 30 seconds. Base64
shards respect CacheService limits; missing shards become a full cache miss.
The client does not overlap requests, discards stale filter responses and
preserves map zoom on ordinary refresh. Exact historical release bindings can
now supply backgrounds for known v1 builds; unverified layouts still use the
labeled coordinate grid. Existing event rows are never rewritten.

## Validation

- `node AnalyticsBackend/tests/backend.test.cjs`: validates seven-scene acceptance,
  old/new payloads, backup migration, deduplication, metrics, returns, map/source/
  source isolation, alias privacy, partial cache eviction and exported maps.
- Unity batch `AnalyticsVerification.RunAllBatch`: exports all maps and runs
  Editor-only lifecycle tests for all seven scenes, repeated completion, respawn
  run retention, map metadata, return entry and restart reasons. Results are
  saved to Library/AnalyticsVerification.json. These are not full playthroughs.
- `node AnalyticsBackend/tests/preview-server.cjs`: local dashboard preview with
  clearly marked synthetic fixtures and real map exports. No cloud writes.

Live acceptance remains separate: test the seven actual goals, deaths, checkpoint
recovery, return path, menu pauses, manual restarts, each form, browser focus,
and mobile input in the WebGL build. Match Sheet rows and diagram positions,
then observe refresh without reloading. Verify older builds still upload.

### Recorded local results, November 1

- Unity 6000.3.22f1 compiled the runtime/editor code and exported all six maps.
- Offline lifecycle verification passed with 44 events across the six scenes.
- Backend tests passed: v1/v2 header migration/backup, duplicate POSTs,
  once-per-run completion, source/map/player/visit filtering, invalid positions,
  Unicode cache shards and missing-shard recovery.
- Browser synthetic preview passed all six map switches, single-player counts,
  empty return visits and preserving zoom across refresh. Preview data is
  explicitly labeled synthetic and was not uploaded to Google Sheets.
- WebGL test build succeeded at Builds/WebGL-Analytics-AllLevels-Test
  (19,865,348 bytes; approximately 2 minutes 13 seconds).
- Fixed a pre-existing player-build compile error in FanPossessionHint:
  alphaIsTransparency is now inside UNITY_EDITOR. No fan controls were changed.
- After the owner renewed USC login, all four source files were saved to Apps
  Script and compared with the local source. The v1 Test and Production sheets
  were backed up and extended successfully. Original rows remain unchanged.
- Cloud verifyDashboardSetup passed: schema v2, 12 current/archived map assets,
  six configured levels, 164 test and 5,169 production events at verification.
- The owner approved publication. Web App Version 6 was deployed November 1,
  2026 at 8:13 PM America/Los_Angeles, retaining the existing /exec URL and Anyone
  access. Its anonymous health endpoint reports schema 2 with accepted versions
  [1,2] and the six scene names.
- All 44 Unity-generated offline lifecycle events were accepted into RawEvents_Test
  under build `integration-verification-20261101`; replaying an event returned
  duplicate=true. One separate `legacy-compatibility-check-20261101` v1 event
  was accepted as well. These are verification fixtures, not real playtest results.
- The actual locally served WebGL build loaded successfully. IntroBox recorded
  a real Star-to-Box transfer and both form durations. A real Level01 Spike death
  and automatic start respawn were exercised. Data appeared under the separate
  test build `analytics-all-levels-20261101` and used the matching real map.
- The public dashboard was verified with both existing production data and the
  new test data. Historical rows correctly use a labeled coordinate grid rather
  than the current map. Full manual playthroughs of all six scenes remain a
  separate gameplay acceptance check.
- Online map checks passed for all six scene/version pairs. The actual WebGL
  Level01 death appeared at (-11.19, 0.42), cause Spike, with one automatic
  start respawn. IntroBox normal entries show two runs/one completion in the
  integration fixtures; the backward visit is excluded. IntroBubble separately
  reports its scene return. No verification events were sent to Production.
- At this historical test checkpoint, no new hosted production game had been published and the isolated settings targeted Test. Beta 0.1.1 subsequently completed game publication on November 3, as recorded above.

## Deployment order

1. Review the branch and local checks; do not overwrite or clear old Sheet data.
2. In the existing Apps Script project, update Code.gs and Dashboard.html; add
   DashboardData.gs and an HTML file named MapCatalog containing MapCatalog.html.
3. Save and run migrateAnalyticsSheets. Confirm both backup tabs and v2 headers.
4. Publish a new version of the existing Web App deployment, retaining its URL,
   execution identity and intended public access. Verify /exec?health=1 lists
   schema versions [1,2] and the seven scenes.
5. Build the test WebGL from the test-configured AnalyticsSettings.asset. Choose
   **Test** in the dashboard. Validate new rows and actual deaths/completions.
6. After acceptance, deliberately set environment=production and assign a new
   unique release buildVersion (also update the Player version for publication).
   Rebuild and publish only the agreed game release path; preserve old releases.
7. Verify signed-out dashboard access, Production rows and both old/new builds.
   Commit source, map assets, tests and docs to the repository after review.

The November 3 release used the existing production endpoint/environment and matching Player/Analytics versions `beta-0.1.1`. Maps were re-exported against that release's scenes. The game is now published; verify the refreshed backend catalog deployment and production rows separately. Test overrides belong in a disposable verification copy. Future source edits do not change the hosted game or Apps Script without their respective deployment steps.

## November 2 main integration and scope boundary

- Fetched main advanced from 32d14d5 to c9a43c2 (33 upstream commits). Integrated
  in an isolated worktree so the earlier local working copy remained untouched.
- Kept main's rebuilt Level01, IntroBouncyBall, all scene/prefab/art/UI assets,
  route modes, gameplay rules, package settings and published game versions.
- In shared scripts, only inserted completion/return analytics calls or supplied
  the restart-reason argument. In particular, IntroStarGoal's current transition
  overlay toggle and IntroBouncyBallGoal's existing completion conditions remain
  byte-for-byte unchanged apart from the telemetry calls. Did not import the
  older branch's FanPossessionHint fix; main already has its own upstream change.
- Added IntroBouncyBall to tracked defaults/settings, map export and validation.
  Production endpoint, environment and build label are retained from main.
- Preserved upstream Beta release evidence/current ADV_CP checkpoint documentation
  while merging the analytics schema, four-metric dashboard and rollout docs.
- Unity validation ran in /tmp, never in the authored gameplay checkout. It
  exported seven maps and verified 62 Editor-only events, run continuity on death,
  cumulative checkpoint clocks, once-only completion, focus-loss form flushing,
  return entries and manual-restart classification. No Sheet events were posted.
- Every generated image/version already exists in the live Version 9 catalog;
  backend source is unchanged from that deployment. No cloud deployment is
  required just to store this integration in Git.

Commands used in the integration worktree:

```sh
node AnalyticsBackend/tests/backend.test.cjs
node AnalyticsBackend/tests/integration-scope.test.cjs c9a43c2e6bb26fa7ea070005e83a3fb264809a8d
```

The scope check asserts exact allowed patches to shared gameplay scripts,
unchanged production identity/build options, all seven enabled/tracked/exported
scene names, current scene hashes, and no edits outside analytics-owned files,
analytics docs/README links, and those explicit hook sites. WebGL build evidence
is recorded after the temporary build completes. No hosted game publication is
part of this merge.

Final local validation: Unity 6000.3.22f1 WebGL build succeeded with all eight
existing enabled build entries (Mainmenu plus seven gameplay scenes), 20,026,660
bytes, 3m38s. The output is a temporary verification package, not a hosted release.
Backend regression and analytics-only integration-scope checks passed. No scene,
Prefab, art, ability, camera, UI, package, project-version or build-option edits
were included beyond the explicit telemetry hook lines documented above.
