# Analytics Dashboard

## Beta 0.1.2 source preparation

Current source target: `beta-0.1.2` (November 6, main baseline `5e7105c` plus local release preparation). Build, publication and production-data acceptance are pending. The seven gameplay scenes are IntroBox, IntroBubble, IntroBouncyBall, Level01, IntroSpider, IntroFan and Level02; Navigation is the startup/menu scene and is not a tracked gameplay level. Preserve historical FanTutorial names/maps for older exports.

All seven current scene hashes differ from the runtime map catalog. Run **Analytics → Export all real level maps** after saving scenes, before building; retain historical PNGs/catalog entries. Separately deploy the refreshed catalog and current IntroFan-compatible backend to the existing Apps Script deployment. Do not change the endpoint or rewrite old rows. Verify `beta-0.1.2` events and exact map matches on the built player; prior Version 9 and 0.1.1 evidence does not certify the new export. See [release preparation](Beta-0.1.2.md) and [current acceptance](AnalyticsTestPlan.md#beta-012-acceptance-status).

Last updated: 2026-11-06. This replaces the earlier individual-player dashboard.
See [AnalyticsAllLevels.md](AnalyticsAllLevels.md) for the seven-scene Unity collector,
v2 migration and runtime map export. This change updates dashboard code/assets;
it does not publish a new game or rewrite Sheet rows.

## Recorded Beta 0.1.1 publication

Beta 0.1.1 was published on November 3, 2026 at [the Beta URL](https://xtraluck21.github.io/StarFormer/beta/), from source `9451511` as `gh-pages` commit `e81097e`. It includes seven-scene v2 collection. The Version 9 dashboard evidence below predates this game release; do not treat its historical map tests as verification of the refreshed 0.1.1 backend catalog or production events. Those remain [acceptance follow-ups](AnalyticsTestPlan.md#beta-011-acceptance-status). The November 4 help-book UI is main-only and is not part of the published 0.1.1 game.

## Scope

The page contains exactly four metric sections, always aggregating all players:

1. Death locations on the matching real map, with red points only.
2. Active travel time from level start to the first checkpoint, between checkpoints, and from the last checkpoint to the finish.
3. Completion-time distribution, mean, median and observed-run completion rate.
4. Total control time and percentage in each form, including Spider and Fan.

Top filters are Source, Build, Level and Map version. There is no Player or Visits
selector, form/cause death filter, individual completion bar, individual death
record, start/checkpoint map marker or separate checkpoint-arrival module.
The server ignores supplied player/entry filters. It always uses normal entries;
known return visits are excluded. Older v1 games did not distinguish return
visits, so historic rows with no entry_reason retain the existing normal default.

## Metric calculations

### 1. Death locations

Deduplicate event_id, select death events, and return valid world X/Y pairs.
No player identity, form or death cause is included in the point response.
PNG bounds and points share one SVG world transform. Pan, zoom and Fit map are
retained; regular refresh preserves zoom. Invalid positions are counted but not
plotted. Out-of-bounds positions expand the view rather than being clamped.

### 2. Checkpoint travel time (redefined November 2)

The owner replaced death timing / respawn counts with time required to reach the
next checkpoint. Each row is one directed route: Start -> CP1, CP1 -> CP2, and
last CP -> Finish. A no-checkpoint tutorial uses Start -> Finish.

For each completed segment in the same player/session/level run:

```
segment active time = destination.level_active_seconds - source.level_active_seconds
```

Level-start events provide the first source, checkpoint_reached provides each
arrival, and level_complete closes the last segment. The matching attempt_start
checkpoint callback is a fallback arrival, not a second traversal. Death respawns
retain the source-arrival clock; do not use attempt_active_seconds or reset the
segment at death. Paused/unfocused/death-screen time is already excluded by the
collector's active clock. Manual restarts create separate runs and do not join.

Average each directed route over its completed samples. Display Route, Average
time (with a comparison bar), and Completed samples. There is no death-time
histogram, respawn histogram, first-try rate or individual-player analysis.
Unfinished routes are excluded, rather than treated as zero. Missing source or
arrival clocks are excluded with a note. A respawn without an observed source
arrival cannot define the full segment start. Duplicate deliveries, checkpoint
callbacks and repeated completions never create extra time samples.

If all run clocks are present, order by monotonic level active time. If a clock
is missing, preserve client timestamp order instead of sorting missing clocks as
zero. This keeps missing endpoints in their correct route and excludes their
durations. Different route rows can have different sample cohorts, so their
averages need not sum to the completed-run average in metric 3.

Example: source at 10 active seconds, three failed 7-second attempts, then a
12-second success gives 33 seconds for the segment, not 12. A separate first-try
12-second traversal produces a two-sample mean of 22.5 seconds. An unfinished
four-retry traversal does not enter that mean.

### 3. Completion time

Count one level_complete per observed run with a matching level_start (or
legacy attempt_start reason level_start). Use level_active_seconds, including
active play before/after automatic death reloads and excluding pauses/focus loss.
Missing starts are excluded from completion statistics and called out. Mean and
median use valid completed durations only; incomplete runs are not zero-duration
completions. Completion rate is completed observed runs / observed starts.
Histogram bins: 0–2, 2–4, 4–6, 6–10, 10–15, 15+ minutes, left-inclusive.

### 4. Form time

Sum form_duration.duration_seconds by controlled_form and divide by the total
for percentages. Do not add analytics_snapshot durations. Preserve all forms
actually reported, including Spider and Fan; never force equal shares or invent
unobserved form time.

## Historical map backgrounds

Old v1 events lack map_version. Do not rewrite events or assign the current map.
getDashboardMap(level, mapVersion, build) resolves legacy only through explicit
release bindings in DashboardData.gs. Sources and publishing commits are recorded
in AnalyticsBackend/Maps/releases.json. PNGs were rendered from disposable Unity
6000.3.22f1 release-source projects, using the same exporter and exact world bounds.

| Build | Source revision | Publishing revision |
| --- | --- | --- |
| beta-0.1.0 | 3a693ca | 1105970 |
| alpha-0.1.2 | 5650250 | 5f62980 |
| alpha-0.1.1 | 765a1ea | a931f23 |
| alpha-0.1.0 | 994a014 | f47d18d |

Bindings cover every gameplay scene actually enabled in the historical releases listed above. Those collectors track only Level01 and Level02; published Beta 0.1.1 instead tracks all seven scenes and supplies v2 map versions. The table describes historical fallback bindings, not a complete release list.
Beta Level01 is the promoted Advanced layout and differs from the Alpha map.
The page labels the matched release; source/publishing revisions remain in the provenance file. Unverified older builds
(e.g. playtest-01-20261018) retain the labeled coordinate grid until their exact
built source is confirmed. A new map-aware event uses its exact map_version.
The initial layout does not reconstruct moving bodies, gate states or later map changes.

Actual Alpha 0.1.2 rows also contain a pre-Beta run with ADV_CP checkpoints.
readDashboardRows_ isolates Alpha 0.1.0–0.1.2 legacy Level01 runs with an ADV_CP
signature into the in-memory map selection legacy-layout-conflict, across their
whole identity/session/run key. It never rewrites the Sheet. Those runs remain
analyzable with a clearly labeled grid, without mixing with the released Alpha
map. Default selection prefers a verified/nonconflicting version when present.
This catches observed conflicts; runs without any distinguishing checkpoint
cannot be retrospectively identified with certainty. Exact future map_version
values eliminate this ambiguity.

Reproducible import after rendering a release in a disposable project:

```sh
python3 AnalyticsBackend/tests/import-release-maps.py \
  --release BUILD SOURCE_COMMIT PUBLISHING_COMMIT /absolute/export/project
```

The importer checks scene/image SHA256 against exported metadata, preserves
existing catalog entries and release bindings, copies only map assets, and leaves
the six-map runtime manifest unchanged. Archive the source and verify the release
provenance before adding a binding.

## Source and deployment

- Code.gs: existing POST receiver and GET/health routes; the current scene whitelist accepts all seven gameplay scenes, including IntroBouncyBall and IntroFan, while retaining historical FanTutorial compatibility.
- DashboardData.gs: aggregate-only API, interval reconstruction, verified legacy
  map bindings, read-only cloud check, 30-second sharded cache (new v3 key namespace).
- Dashboard.html: four metric sections, responsive UI, safe text rendering and
  30-second non-overlapping refresh with stale-response protection.
- MapCatalog.html: current/archived map assets returned on demand, without Drive
  image permissions or external image hosting.

Save the three changed files into the existing Apps Script editor. Verify exact
contents, run verifyAggregateDashboard (read-only), and test the latest saved
code via Deploy -> Test deployments. Updating a saved draft does not update /exec.
For publication, edit the existing Web App deployment, choose New version and
keep the URL, execution identity and audience unchanged. Follow the owner's
requested approval before publishing. Version 6 remains the rollback point.
Git push alone never changes the Apps Script deployment.

## Validation for this revision

- node AnalyticsBackend/tests/backend.test.cjs: ingestion/migration compatibility;
  privacy; all-player/normal-entry enforcement; 0/3 death retries included in travel time; unfinished/missing-time
  intervals; duplicated/out-of-order events; completion distribution; form-time
  snapshot exclusion; cache eviction; exact current and historical map matching, and legacy label/layout-conflict isolation.
- Local browser preview uses conspicuously labeled synthetic data, including
  first-try success, repeated respawns, an unfinished interval and Spider/Fan.
  It never uploads synthetic fixtures to the live receiver.
- Cloud and publication acceptance are recorded below after execution.

### November 2 acceptance (before public publication)

- Saved Dashboard.html, DashboardData.gs and MapCatalog.html to the existing cloud
  project and verified each entire file against local source through editor copy.
- verifyAggregateDashboard completed in the cloud at 13:42 Los Angeles time.
  Production Beta Level01: 264 selected events, 13 observed runs, 2 completions,
  10 deaths, 4 passed interval groups; matching Beta map resolved. Test and all
  eight historical build/level map bindings also resolved.
- The /dev editor-only page read real production data. Beta Level01 showed 10 red
  death points on the newly rebuilt level, completion mean/median 1m28s and
  completion rate 2/13 = 15.4%. ADV_CP3 -> Finish had two verified passes, 0 and 1
  respawns respectively: average/median 0.5, histogram 0:1 and 1:1. Unpassed
  sections were listed separately. No personal chart or Player/Visits selector.
- The owner approved publication to the existing URL. Version 7 was published
  November 2, 2026 at 13:52 Los Angeles time, with execution identity/access
  unchanged; Version 6 remains available for rollback.
  No game rebuild, Git commit/push or Sheet mutation was performed for this revision.


### Version 7 public acceptance

After owner approval, updated the existing deployment at 13:52 on November 2,
2026 (Los Angeles). The deployment confirmation reported Version 7 with the same
AKfycbwvouGhWcYK46treKMiCgguVT5qMNZkds9Ig5Iq56gEYUEypdGokJI9yAGdEzZUdxNY ID.
Execute as Me and Anyone access were retained. Reloading the original /exec URL
showed the four aggregate sections, no Player/Visits selectors, the matching Beta
Level01 background and 10 death dots. The checkpoint interval table included
ADV_CP3 -> Finish average/median 0.5 respawns; completion count/rate and form-time
summary matched the saved-code test. The page still refreshes every 30 seconds.
No game release, ingestion payload, permissions, or Sheet event data was changed.


## Tutorial-map follow-up (November 2)

Root cause: map-version choices were inferred exclusively from recorded events.
At that historical deployment checkpoint, the Beta 0.1.0/Alpha collectors only tracked Level01 and Level02, so tutorial
selections had zero events, an empty map version and no background, even when
map assets existed for other source snapshots.

- Imported exact enabled gameplay scene lists, tracked-scene lists and maps from
  the same verified release source checkouts. Beta has seven gameplay scenes
  (five tutorials plus Level01/02). Each supported Alpha has four gameplay scenes
  (Box/Bubble tutorials plus Level01/02). Do not show Spider/Fan/Ball tutorials
  as if they existed in those Alpha exports.
- Build options include known source-verified releases even with no events in
  the selected environment. For a known release level with no recorded version,
  select its verified legacy map for reference. Do not invent deaths or metrics.
- Return release_collects_level so the page can explicitly explain why a
  tutorial has no analytics. Collection still requires publishing a new game;
  updating this dashboard cannot add instrumentation to an already-built game.
- Added IntroBouncyBall to the dashboard and receiver whitelist. Runtime Unity
  collector/settings and the six-scene local test build are unchanged in this
  follow-up; seven-scene game integration remains a separate game-release task.
- Regression tests cover all five zero-event Beta tutorial maps, zero completion/
  death counts, release collection status and hiding nonexistent Alpha tutorials.
  The cloud check now reads all 19 released scene/build map bindings.

### Tutorial-map publication

Version 8 was published November 2, 2026 at 14:03 Los Angeles time using the same
public deployment URL and audience. Four cloud files were saved and verified
against local source. The expanded cloud check completed at 14:02:01, resolving
all 19 released scene/build map bindings, including zero-event tutorials. Node
regression tests passed. The public /exec page was reloaded and Box tutorial
showed its release-matched background with zero events and a collection-status
notice. No event rows were added/rewritten and no new game was built or published.


## Checkpoint travel-time revision status

Replaces the earlier checkpoint-attempt display following the owner's new metric
definition. Only DashboardData.gs, Dashboard.html, relevant tests and docs change;
no game rebuild, Sheet migration or raw event rewrite is needed. Uses the existing
level active clock and observed arrivals. Saved-code preview and publication are
recorded below after validation. The owner approved this revised metric, and Version 9 was published to the
original URL on November 2, 2026 at 14:19 Los Angeles time.

### Travel-time saved-code acceptance

The /dev page used existing production Beta Level01 events and displayed four
rows: Start -> CP1 17s (5 samples), CP1 -> CP2 28s (5), CP2 -> CP3 24s (3),
CP3 -> Finish 31s (2), rounded to the nearest second. Eleven unfinished segments
were excluded. Other metric sections and maps remained present. The owner then
approved publishing the new definition to the original URL.


### Travel-time publication

Version 9: “Checkpoint travel time across retries; simple route averages”,
November 2 at 14:19 Los Angeles time. Same deployment ID, URL, execution identity
and audience. Version 8 remains a rollback option. No game rebuild or Sheet data
migration/rewrite was required. The published page was reloaded for acceptance.


## Main integration follow-up

The current source integration adds seven-scene runtime collection (including
IntroBouncyBall) against the latest main gameplay. It preserves the production
endpoint/environment/build label and contains no scene/layout/ability edits.
The earlier tutorial-map follow-up's six-scene-runtime limitation describes its
historical deployment step; it is superseded by this source integration. Beta 0.1.1 subsequently published the seven-scene collector on November 3. Verify its actual production tutorial events and refreshed map deployment separately. Static base-map limitations and existing historical release bindings remain valid.
