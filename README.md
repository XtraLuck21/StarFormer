# StarFormer

StarFormer is a 2D puzzle-platformer built in Unity. Play as a star that possesses eligible objects on contact and uses their abilities to solve environmental puzzles. Grow or shrink as a Box, rise and swim as a Bubble, bounce and float as a Bouncy Ball, then learn Spider and Fan abilities.

## Beta 0.1.3

[Play StarFormer - latest Beta](https://xtraluck21.github.io/StarFormer/beta/)

Beta 0.1.3 includes the promoted Level02: the spike-wall chase, dark descent, four-gear seesaw, rising Star handoff, Smiley Face teeth/head route and final Box challenge. It also includes refreshed maps and the obsolete empty Help-component cleanup. Player and Analytics versions are both `beta-0.1.3`.

Publication to `/beta/` is approved and in progress. The main Play link above points to this release destination; the root website remains historical Beta 0.1.2 and Alpha paths are unchanged. See the [Beta 0.1.3 release record](Docs/Beta-0.1.3.md) for artifact hashes, local checks and remaining gameplay/device/backend acceptance.

## Historical Beta 0.1.2

[Original main website - Beta 0.1.2](https://xtraluck21.github.io/StarFormer/) · [Beta website](https://xtraluck21.github.io/StarFormer/beta/)

Beta 0.1.2 was published on November 6, 2026 to both the [main website](https://xtraluck21.github.io/StarFormer/) and [Beta website](https://xtraluck21.github.io/StarFormer/beta/). Player Version and Analytics `buildVersion` are `beta-0.1.2`. The source checkout at publication was clean main `b93768f`; the published artifact is the completed local export.

The same `Builds/WebGL-Beta-0.1.2` export was committed and pushed on `gh-pages` as [`64b35c6`](https://github.com/XtraLuck21/StarFormer/commit/64b35c6c5dbe50ffefc05e1590533cebf47888dc). [GitHub Pages deployment](https://github.com/XtraLuck21/StarFormer/actions/runs/42303613672) succeeded. All seven files at each URL (14 files total) returned HTTP 200 and matched the local export byte for byte by SHA-256. Alpha and other historical version directories were preserved. The root previously served Beta 0.1.0; `/beta/` previously served Beta 0.1.1. Both served Beta 0.1.2 at that publication; `/beta/` is the Beta 0.1.3 destination above.

The release includes interactive Navigation, school/level entrances, Collection Book integration, shared Home menus, the IntroFan rename, Level01 traversal/camera/possession fixes and revised Spider guidance. Shape Closet and the books share session-only per-form discovery: controlling a form reveals its icon/help, scene changes and retries retain it, and a new game launch clears it. Completion grants no additional forms; old persistent unlock saves are ignored. Obsolete Help components were removed from IntroBox, IntroBubble and IntroSpider, with isolated Editor book/pause checks passing.

This verifies deployment and package integrity, not a new hosted gameplay playtest, mobile acceptance or production analytics delivery. Analytics map export/backend updates were not performed in the 0.1.2 publication; its Level02 repairs were deferred and are superseded by the promoted scene in 0.1.3. See the [release record](Docs/Beta-0.1.2.md) and [remaining acceptance checks](Docs/AnalyticsTestPlan.md#beta-012-acceptance-status).

## Historical Beta 0.1.1

Beta 0.1.1 was published to `/beta/` on November 3 from source `9451511` as `gh-pages` commit `e81097e`. It added seven-scene analytics and the Level01 movable-door hint. All seven hosted files matched `Builds/WebGL-Beta-0.1.1` on November 5. That export remains preserved locally and in Git history; `/beta/` was replaced by 0.1.2 on November 6. See [historical release evidence](Docs/publish_instructions.md#beta-011-release-status).

## Historical Beta 0.1.0

Beta 0.1.0 was published November 2 from source `3a693ca` as `gh-pages` commit `1105970`, with developer-reported local/hosted playtests and Level01 production-event evidence. Its collector tracked only Level01/Level02. The dashboard Version 9 record documents historical map matching and aggregate metrics; this is separate from 0.1.1 runtime/data acceptance.

On November 5, `/beta/` served 0.1.1 and the root served 0.1.0 (root publication `f6eaf9e`). Both URLs were updated to 0.1.2 on November 6. See [historical release evidence](Docs/AnalyticsTestPlan.md#beta-010-acceptance-status).

## Current gameplay and progression

```text
Navigation → IntroBox → IntroBubble → IntroBouncyBall → Level01 → IntroSpider → IntroFan → Level02
```

- IntroBox and IntroBubble teach contact possession, J/K resizing, air ascent, underwater steering and Q pop. Flag-opened passages support return travel between IntroBox/IntroBubble and IntroBubble/IntroBouncyBall without a Level up overlay between those scenes.
- IntroBouncyBall teaches automatic bouncing, floating on water, resuming bounces on land and faster bounces beneath low ceilings. The main route includes the low-ceiling lesson, short contextual headlines and a spike-death reminder. Its final flag transitions into Level01.
- Level01 was rebuilt with a new layout and form progression, replacing the original scene. It includes Star/Box route choices, a one-way lift, reversible SAFE/NORMAL/HARD choices, moving safe-route boards, a falling spike, three fixed-form checkpoints, contextual hints and Home pause/restart UI. The original map remains as disabled `Level01_Legacy`.
- Level01 continues through IntroSpider and IntroFan to Level02. Navigation maps Kindergarten to IntroBox, Level 1 to Level01, Elementary School to IntroSpider and Level 2 to Level02; tutorials remain optional.
- R opens restart choices; the shared house/Star Home menu is configured in all seven gameplay scenes and returns to Navigation. Collection Book and per-form Help continue in Beta 0.1.3; Level02 now uses the configured book with its obsolete empty component removed. See the release record for verification coverage.
- Level01 includes the upper-gap, checkpoint-2 approach, ball camera and Box-to-ball recovery fixes from PR #21. IntroSpider adds climbing arrows and cake/spike feedback. These changes are included in Beta 0.1.2 and were absent from the historical Beta 0.1.1 export.

## Published builds and history

- [Alpha 0.1.2](https://xtraluck21.github.io/StarFormer/alpha/) — historical release, October 18, 2026; does not contain the current rebuilt Level01 or integrated Bouncy Ball tutorial.
- [Earlier analytics playtest](https://xtraluck21.github.io/StarFormer/playtest-01/) — preserved older build.
- [Alpha Progress Check submission](https://xtraluck21.github.io/StarFormer/alpha-progress-check/) — frozen October 12 All-Level V2 submission.
- Beta 0.1.0 and 0.1.1 — preserved in their local versioned exports and `gh-pages` history; their former live URLs are reused by later releases.

Alpha 0.1.2 source configuration: `5650250`; gameplay/UI changes: `9b35f8e`; publishing commit: `5f62980`. Developer-reported localhost/online tests and deployed payload/version checks passed. Its embedded Player/Analytics version remains `alpha-0.1.2`; Sheet delivery for that version remains separately unconfirmed. Source updates do not change published exports.

## Development references

- Unity editor: `6000.3.22f1`, with Web Build Support.
- Enabled build scenes: Navigation (0), IntroBox (1), IntroBubble (2), IntroBouncyBall (3), Level01 (4), IntroSpider (5), IntroFan (6), Level02 (7). Mainmenu and legacy/reference/sandbox entries stay disabled.
- [Introductory levels](Docs/intro-levels.md) — tutorials, controls, contextual hints and two-way passages.
- [Gameplay specification](Docs/GameplaySpec.md)
- [Script, scene and release reference](Docs/ScriptGuide.md)
- [Level 1](Docs/level-1.md) — rebuilt map, route modes, lifts, checkpoints and Home menu.
- [Level 2](Docs/level-2.md) — integrated route, completion, hints and navigation.
- [Checkpoint guidance](Docs/CheckpointGuidance.md)
- [WebGL analytics implementation](Docs/AnalyticsImplementation.md)
- [WebGL analytics test plan](Docs/AnalyticsTestPlan.md)
- [Analytics dashboard](Docs/AnalyticsDashboard.md) — four aggregate metrics, verified real maps and Apps Script deployment.
- [All-level analytics and real maps](Docs/AnalyticsAllLevels.md) — all seven gameplay scenes, schema v2, map exports and validation.
- [Publish to GitHub Pages](Docs/publish_instructions.md)

![Historical Tutorial gameplay; predates the introductory levels](Docs/Game_screenshot.png)
