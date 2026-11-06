# StarFormer

StarFormer is a 2D puzzle-platformer built in Unity. Play as a star that possesses eligible objects on contact and uses their abilities to solve environmental puzzles. Grow or shrink as a Box, rise and swim as a Bubble, bounce and float as a Bouncy Ball, then learn Spider and Fan abilities.

## Published Beta 0.1.1

[Play StarFormer Beta 0.1.1](https://xtraluck21.github.io/StarFormer/beta/)

Beta 0.1.1 was published on November 3, 2026 at [the Beta URL](https://xtraluck21.github.io/StarFormer/beta/), from source `9451511` as `gh-pages` commit `e81097e`. Player Version and Analytics `buildVersion` are both `beta-0.1.1`. The release adds schema-v2 analytics for all seven gameplay scenes, refreshed analytics maps and the Level01 contextual movable-door hint. After 20 seconds without door movement, the optional star hint can be activated by deliberate player contact to preview lifting the door.

On November 5, all seven live Beta files returned HTTP 200 and matched `Builds/WebGL-Beta-0.1.1` and the publishing checkout byte for byte. This confirms the deployed package, not a new gameplay playtest or production analytics acceptance. Detailed browser/device gameplay, production event delivery and the refreshed backend map deployment remain separately unverified. See [the release record](Docs/publish_instructions.md#beta-011-release-status) and [acceptance follow-ups](Docs/AnalyticsTestPlan.md#beta-011-acceptance-status).

## Integrated source changes awaiting publication

The November 4 paged help book, six-form demo animations, Unity Recorder package and Mainmenu layout/behavior changes (`3a03844`, `f05d433`, `55f6b6b`, `f2c5c48`) are integrated into `main` but are **not included in published Beta 0.1.1**.

The November 5 rebase/push moved the release-preparation patch from `9451511` to `2caffd9` on top of those UI commits. `2caffd9` identifies the integrated source, not the source used for the November 3 export. Source settings still say `beta-0.1.1`; that label alone does not mean current main matches the hosted game. Use a new agreed version and fresh export for the next publication.

## Historical Beta 0.1.0

Beta 0.1.0 was published November 2 from source `3a693ca` as `gh-pages` commit `1105970`, with developer-reported local/hosted playtests and Level01 production-event evidence. Its collector tracked only Level01/Level02. The dashboard Version 9 record documents historical map matching and aggregate metrics; this is separate from 0.1.1 runtime/data acceptance.

`/beta/` now serves 0.1.1. The [root URL](https://xtraluck21.github.io/StarFormer/) still serves Beta 0.1.0 (root publication `f6eaf9e`), verified by its live page version on November 5. See [historical release evidence](Docs/AnalyticsTestPlan.md#beta-010-acceptance-status).

## Current gameplay and progression

```text
Mainmenu → IntroBox → IntroBubble → IntroBouncyBall → Level01 → IntroSpider → IntroFan → Level02
```

- IntroBox and IntroBubble teach contact possession, J/K resizing, air ascent, underwater steering and Q pop. Flag-opened passages support return travel between IntroBox/IntroBubble and IntroBubble/IntroBouncyBall without a Level up overlay between those scenes.
- IntroBouncyBall teaches automatic bouncing, floating on water, resuming bounces on land and faster bounces beneath low ceilings. The main route includes the low-ceiling lesson, short contextual headlines and a spike-death reminder. Its final flag transitions into Level01.
- Level01 was rebuilt with a new layout and form progression, replacing the original scene. It includes Star/Box route choices, a one-way lift, reversible SAFE/NORMAL/HARD choices, moving safe-route boards, a falling spike, three fixed-form checkpoints, contextual hints and Home pause/restart UI. The original map remains as disabled `Level01_Legacy`.
- Level01 continues through IntroSpider and IntroFan to Level02. The menu also offers direct Level 1, Level 2 Intro and Level 2 entries.
- R opens restart choices. The Level01 contact-activated door hint is included in Beta 0.1.1; a dedicated Level01 Controls panel remains deferred. Broader browser/mobile coverage and 0.1.1 analytics delivery/map validation remain follow-ups.

## Published builds and history

- [Alpha 0.1.2](https://xtraluck21.github.io/StarFormer/alpha/) — historical release, October 18, 2026; does not contain the current rebuilt Level01 or integrated Bouncy Ball tutorial.
- [Earlier analytics playtest](https://xtraluck21.github.io/StarFormer/playtest-01/) — preserved older build.
- [Alpha Progress Check submission](https://xtraluck21.github.io/StarFormer/alpha-progress-check/) — frozen October 12 All-Level V2 submission.
- [Root Beta 0.1.0](https://xtraluck21.github.io/StarFormer/) — remains at 0.1.0; use `/beta/` for the current 0.1.1 release.

Alpha 0.1.2 source configuration: `5650250`; gameplay/UI changes: `9b35f8e`; publishing commit: `5f62980`. Developer-reported localhost/online tests and deployed payload/version checks passed. Its embedded Player/Analytics version remains `alpha-0.1.2`; Sheet delivery for that version remains separately unconfirmed. Source updates do not change published exports.

## Development references

- Unity editor: `6000.3.22f1`, with Web Build Support.
- Enabled build scenes: Mainmenu (0), IntroBox (1), IntroBubble (2), IntroBouncyBall (3), Level01 (4), IntroSpider (5), IntroFan (6), Level02 (7). Legacy/reference/sandbox entries stay disabled.
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
