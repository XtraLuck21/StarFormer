# StarFormer

StarFormer is a 2D puzzle-platformer built in Unity. Play as a star that possesses eligible objects on contact and uses their abilities to solve environmental puzzles. Grow or shrink as a Box, rise and swim as a Bubble, bounce and float as a Bouncy Ball, then learn Spider and Fan abilities.

## Play Beta 0.1.0

[Play StarFormer Beta](https://xtraluck21.github.io/StarFormer/beta/)

Beta 0.1.0 was published on November 2, 2026, from source revision `3a693ca` as gh-pages commit `1105970`. The final WebGL package was rebuilt after version/template synchronization; the developer reported local and hosted playtests passed. [GitHub Pages deployment](https://github.com/XtraLuck21/StarFormer/actions/runs/41410712039) succeeded, and all seven live files matched the tested local export byte for byte. Export: `Builds/WebGL-Beta-0.1.0`. Alpha and all historical publication paths remain unchanged.

Player Version and Analytics `buildVersion` are `beta-0.1.0`; collection remains in `production`, tracking Level01/Level02. The developer confirmed incoming records, and the November 2 dashboard screenshot shows Beta Level01 events, a completion and a death. **Known analytics follow-up:** the dashboard does not yet match this build to the rebuilt Level01 map and displays deaths on a coordinate grid. Data arrival is confirmed; map rendering, Level02-specific data and full metric validation remain separate checks. See [analytics acceptance and follow-ups](Docs/AnalyticsTestPlan.md#beta-010-acceptance-status) and [publishing instructions](Docs/publish_instructions.md).

## Current gameplay and progression

```text
Mainmenu → IntroBox → IntroBubble → IntroBouncyBall → Level01 → IntroSpider → FanTutorial → Level02
```

- IntroBox and IntroBubble teach contact possession, J/K resizing, air ascent, underwater steering and Q pop. Flag-opened passages support return travel between IntroBox/IntroBubble and IntroBubble/IntroBouncyBall without a Level up overlay between those scenes.
- IntroBouncyBall teaches automatic bouncing, floating on water, resuming bounces on land and faster bounces beneath low ceilings. The main route includes the low-ceiling lesson, short contextual headlines and a spike-death reminder. Its final flag transitions into Level01.
- Level01 was rebuilt with a new layout and form progression, replacing the original scene. It includes Star/Box route choices, a one-way lift, reversible SAFE/NORMAL/HARD choices, moving safe-route boards, a falling spike, three fixed-form checkpoints, contextual hints and Home pause/restart UI. The original map remains as disabled `Level01_Legacy`.
- Level01 continues through IntroSpider and FanTutorial to Level02. The menu also offers direct Level 1, Level 2 Intro and Level 2 entries.
- R opens restart choices. Optional dedicated Hint/Controls buttons for Level01 remain deferred. Broader browser/mobile coverage and analytics map/version validation remain follow-ups.

## Published builds and history

- [Alpha 0.1.2](https://xtraluck21.github.io/StarFormer/alpha/) — historical release, October 18, 2026; does not contain the current rebuilt Level01 or integrated Bouncy Ball tutorial.
- [Earlier analytics playtest](https://xtraluck21.github.io/StarFormer/playtest-01/) — preserved older build.
- [Alpha Progress Check submission](https://xtraluck21.github.io/StarFormer/alpha-progress-check/) — frozen October 12 All-Level V2 submission.
- [Legacy root build](https://xtraluck21.github.io/StarFormer/) — preserved separately.

Alpha 0.1.2 source configuration: `5650250`; gameplay/UI changes: `9b35f8e`; publishing commit: `5f62980`. Developer-reported localhost/online tests and deployed payload/version checks passed. Its embedded Player/Analytics version remains `alpha-0.1.2`; Sheet delivery for that version remains separately unconfirmed. Source updates do not change published exports.

## Development references

- Unity editor: `6000.3.22f1`, with Web Build Support.
- Enabled build scenes: Mainmenu (0), IntroBox (1), IntroBubble (2), IntroBouncyBall (3), Level01 (4), IntroSpider (5), FanTutorial (6), Level02 (7). Legacy/reference/sandbox entries stay disabled.
- [Introductory levels](Docs/intro-levels.md) — tutorials, controls, contextual hints and two-way passages.
- [Gameplay specification](Docs/GameplaySpec.md)
- [Script, scene and release reference](Docs/ScriptGuide.md)
- [Level 1](Docs/level-1.md) — rebuilt map, route modes, lifts, checkpoints and Home menu.
- [Level 2](Docs/level-2.md) — integrated route, completion, hints and navigation.
- [Checkpoint guidance](Docs/CheckpointGuidance.md)
- [WebGL analytics implementation](Docs/AnalyticsImplementation.md)
- [WebGL analytics test plan](Docs/AnalyticsTestPlan.md)
- [Publish to GitHub Pages](Docs/publish_instructions.md)

![Historical Tutorial gameplay; predates the introductory levels](Docs/Game_screenshot.png)
