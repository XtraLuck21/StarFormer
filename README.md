# StarFormer

StarFormer is a 2D puzzle-platformer built in Unity. Play as a star that possesses eligible objects on contact and uses their abilities to solve environmental puzzles. Grow or shrink as a box, rise through the air and swim underwater as a bubble, and leave bodies in useful positions while transferring control.

## Play the published build

- [Play Alpha 0.1.2](https://xtraluck21.github.io/StarFormer/alpha/) — current Alpha; later tested Alpha releases replace this path.
- [Earlier analytics playtest](https://xtraluck21.github.io/StarFormer/playtest-01/) — preserved older build.
- [Alpha Progress Check submission](https://xtraluck21.github.io/StarFormer/alpha-progress-check/) — frozen October 12 All-Level V2 submission.
- [Legacy root build](https://xtraluck21.github.io/StarFormer/) — preserved separately from Alpha.

Alpha **0.1.2** was published on October 18, 2026, with Mainmenu → IntroBox → IntroBubble → Level01 → Level02. It includes the Level 1 Restart Level button, lowered IntroBox movement/jump controls, and compact, semi-transparent IntroBubble controls with the original camera framing. Level 2 retains its completion screen, corrected Spider/Fan hints and Restart/Retry navigation. The Alpha template provides a fitted 16:9 view, resolution scaling and fullscreen/windowed launch choices.

Source release configuration: `5650250`; gameplay/UI changes: `9b35f8e`; publishing commit: `5f62980`. Localhost and online tests passed according to the developer, and the deployed HTML/version and game-data payload were verified. Player and Analytics versions are both `alpha-0.1.2`. Collection remains in `production` for Level01/Level02; current-version Sheet delivery still needs confirmation by the analytics owner.

Publish future Alpha updates only into `alpha/`; preserve the older paths. Source changes do not update the hosted game until rebuilt and published. See the [release history](Docs/ScriptGuide.md#4-web-build-and-github-pages-release) and [publishing instructions](Docs/publish_instructions.md).

## Current branch source (October 31)

The IntroBubble rebuild is newer than the published Alpha: an enclosed water-tank route, contextual map hints, a spare Bubble, and a left exit returning to IntroBox's endpoint as BoxB with its door open. Box now uses **hold J to grow / hold K to shrink**; Bubble uses **Q to pop**. IntroBubble hides its standalone Restart button and uses a smaller Main Menu button; **R opens restart choices**.

These changes require a new build and publication. The existing Alpha links and `alpha-0.1.2` version remain unchanged. See [intro-levels.md](Docs/intro-levels.md) for the route and recovery limits, and [CheckpointGuidance.md](Docs/CheckpointGuidance.md) for restart behavior and the remaining Level01 E/Q reminder mismatch.

## Development references

- Unity editor: `6000.3.22f1`, with Web Build Support.
- Enabled build scenes: `Mainmenu` (0), `IntroBox` (1), `IntroBubble` (2), `Level01` (3), `Level02` (4). The old `Tutorial` remains in the repository but is excluded from builds.
- [Introductory levels](Docs/intro-levels.md) — rebuilt IntroBubble route, current hints/controls, return travel, spare Bubble and mobile layout.
- [Gameplay specification](Docs/GameplaySpec.md)
- [Script, scene and release reference](Docs/ScriptGuide.md)
- [Level 1 progress](Docs/level-1.md) — ball checkpoints, U2 regeneration, possession recovery and the added Restart Level button.
- [Level 2 progress](Docs/level-2.md) — integrated route, final completion UI, hints, responsive navigation and checkpoint follow-ups.
- [WebGL analytics implementation](Docs/AnalyticsImplementation.md) — Level 1/2 event collection and Google Sheets pipeline.
- [WebGL analytics test plan](Docs/AnalyticsTestPlan.md) — recorded evidence and remaining data checks.
- [Publish to GitHub Pages](Docs/publish_instructions.md)

![Historical Tutorial gameplay; predates the introductory levels](Docs/Game_screenshot.png)
