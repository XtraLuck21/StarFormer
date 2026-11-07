# Beta 0.1.2 release record

**Beta 0.1.2 was published on November 6, 2026** to both the [main website](https://xtraluck21.github.io/StarFormer/) and [Beta website](https://xtraluck21.github.io/StarFormer/beta/).

The same `Builds/WebGL-Beta-0.1.2` export was committed and pushed on `gh-pages` as [`64b35c6`](https://github.com/XtraLuck21/StarFormer/commit/64b35c6c5dbe50ffefc05e1590533cebf47888dc). [GitHub Pages deployment](https://github.com/XtraLuck21/StarFormer/actions/runs/42303613672) succeeded. All seven files at each URL (14 files total) returned HTTP 200 and matched the local export byte for byte by SHA-256.

## Release configuration

| Item | Published state |
| --- | --- |
| Player / Analytics version | `beta-0.1.2` |
| Source checkout at publication | Clean `main` at `b93768f`; initial preparation baseline was `5e7105c` |
| Export | `Builds/WebGL-Beta-0.1.2`, built by the owner; preserved unchanged during publishing |
| Publishing commit | `64b35c6c5dbe50ffefc05e1590533cebf47888dc`, November 6 at 14:17:44 PST |
| Deployment | GitHub Pages run `42303613672`, successful |
| Destinations | `/StarFormer/` and `/StarFormer/beta/`, identical copies |
| Replaced versions | Root: Beta 0.1.0; `/beta/`: Beta 0.1.1 |
| Historical paths | Alpha, Alpha Progress Check, playtest and analytics-test folders unchanged; all 59 tracked files outside the two publishing scopes preserved |
| Startup | `Navigation`, enabled index 0; legacy `Mainmenu` disabled |
| Gameplay scenes | IntroBox, IntroBubble, IntroBouncyBall, Level01, IntroSpider, IntroFan, Level02 (1–7) |
| Collection | Existing production endpoint and collection enabled; no backend deployment performed |
| Verification | 14 hosted files HTTP 200 and byte-identical to the export; no new hosted gameplay test |

The clean source checkout is recorded for provenance; the export does not embed a Git revision stamp. The published file hashes below identify the artifact directly. Historical local exports remain preserved.

The template remains `PROJECT:StarFormerAlpha`; its name is historical and the published HTML label is `beta-0.1.2`. Source settings retain Unity `6000.3.22f1`, 1920 × 1080, Gzip, Decompression Fallback, hashed payload names and disabled Web threads. Publication copied the owner-built export without rebuilding or changing runtime settings.

## Changes since the recorded Beta 0.1.1 export

- **Level01 playtest fixes (PR #21, `42e32a4`):** bidirectional travel across the upper platform gap without maximum-size Box snagging; a wider checkpoint-2 approach that still requires player action; steadier camera framing during BouncyBall motion; recovery from Box-to-ball possession without the abandoned Box trapping the ball.
- **Navigation (PR #22, `dc28ff2`):** a separate black/yellow Navigation scene with a playable Star, A/D and Space key hints, upper jump entrances and lower drop entrances, bounce/flash feedback and an Entering overlay before loading. Mouse clicks also select entrances. Kindergarten → IntroBox; Level 1 → Level01; Elementary School → IntroSpider; Level 2 → Level02. Coming Soon has no playable destination.
- **Scene and menu integration:** FanTutorial is now IntroFan with its GUID retained. The forward chain and menu returns use the current scene names. All seven gameplay scenes have the shared top-right house/Star Home menu; return actions load Navigation. Legacy Mainmenu and test/reference scenes remain excluded from the build.
- **Help and collection:** reusable Collection Book in Navigation, aligned with five Shape Closet slots; shared form-demo assets; input blocking while the book or entering sequence is active; one entering overlay dims the book and the rest of Navigation together. Later main changes add per-form discovery pages to six gameplay scenes. Level02 Help migration is deferred as noted below.
- **Spider teaching (`125217b`, `5e7105c`):** updated climbing guidance, contextual prompts and directional arrows; cake-disguised spikes reveal after contact; first-contact cake feedback and subsequent death text share the center hint rather than duplicating the death caption.
- **Recording tooling:** Unity Recorder `5.1.7` is installed. This does not mean a recording preset or video export has been prepared.

## Release review findings

### Session-only shape discovery

Shape Closet and Navigation’s Collection Book now use one `HelpBookUnlocks` in-memory record. `ShapeClosetSlot` updates the five icons from discovery events; the shared `ControlsContent` pages use the same Box, Bubble, BouncyBall, Spider and Fan IDs. Star’s basic help remains available from the start. Locked pages hide the form instructions, icon/demo and show the locked explanation.

| Action | Discovery result |
| --- | --- |
| Select/enter a school or level as Star | No automatic form-group unlock |
| Successfully take control of a form | Reveal only that form’s icon and book page |
| Start in an assigned host or restore a controlled host | The actual controlled form counts as used; waiting bodies do not |
| Finish Kindergarten, Elementary School, Level01 or Level02 | No additional unlock reward; only used forms remain discovered |
| Return to Navigation, change scenes, die, retry a checkpoint or restart a level | Keep discoveries for the current launch |
| Refresh the browser player, reopen the game or start a new Editor Play session | Clear all form discoveries; Star help stays available |

The old `ShapeClosetProgress`/`ShapeClosetView` scripts, serialized view component and four completion-reward hooks were removed. Discovery has no PlayerPrefs read/write or migration. Historical `StarFormer.ShapeCloset.UnlockTier.v1` / `StarFormer.ShapeCloset.UnlockedGroups.v2` values may remain in local storage, but are ignored. No user save or analytics identifier is deleted. There is no Reset Progress UI.

Opening a book cannot discover a form. A successful starting-host transfer counts consistently; the old per-scene `unlockStartingHostPage` opt-in and suppression path were removed. Book/Closet listeners refresh from the shared event instead of polling every frame. Launch reset uses `SubsystemRegistration`, including Editor Play with domain reload disabled.

### Gameplay Help cleanup and deferred Level02 work

IntroBox, IntroBubble, IntroBouncyBall, Level01, IntroSpider and IntroFan contain six-page `HelpBookUI` configurations with form unlock IDs. The obsolete Help components in IntroBox, IntroBubble and IntroSpider had empty pages and missing button references, producing initialization errors. Those three components and their owner-list entries have been removed; the configured books, tutorial canvases, child objects and all other teaching UI are unchanged.

November 6 isolated Unity 6000.3.22f1 Play Mode verification passed for all three cleaned scenes: exactly one configured book per scene; opening and pausing; next/previous pages; close button, backdrop and Escape; restored gameplay time; preservation of an existing pause or non-default speed; disable/re-enable and scene exit with the book open. No runtime errors or Help warnings were reported during this focused check. This is Editor runtime evidence, not Web/browser acceptance.

Level02 retains the old HelpPanelUI serialized layout without pages/content. Its repair and verification are explicitly deferred until the replacement Level02 scene arrives; it was not modified or tested in this cleanup. Do not describe Help as verified in every gameplay scene.

### Analytics follow-up — not completed by publication

At the pre-publication check, all seven runtime map `source_scene_hash` values differed from the saved scenes. Map export was explicitly left out of this release operation; publishing the completed Web build did not resolve or verify map freshness. The custom `Build → Build WebGL` guard rejects stale maps, while standard Build Profiles does not invoke that guard. Treat map export and backend acceptance as outstanding work for a future build; do not overwrite this completed export or relabel old map data.

1. Stop Play Mode and save intended scene changes.
2. Run **Analytics → Export all real level maps** in Unity. Confirm seven maps export without errors.
3. Review `Assets/Resources/AnalyticsMapCatalog.json`, `AnalyticsBackend/Maps/manifest.json`, versioned PNGs and `AnalyticsBackend/MapCatalog.html`. Preserve older map versions, including historical FanTutorial data.
4. Do not edit a gameplay scene after export without exporting again. Prefab/material changes can also require fresh maps even if the scene hash is unchanged.
5. For dashboard acceptance, separately deploy the refreshed catalog and current IntroFan-compatible backend to the existing Apps Script deployment, keeping its endpoint and historical rows. A game build or Git push does not do this.

## Unity and browser acceptance before recording

The package is already published. The following gameplay/device checks remain follow-ups; exclude deferred Level02 repair and Help acceptance until its replacement scene is available. Build-setting steps apply when preparing a future export.

- Open **File → Build Profiles → Scene List**. Navigation must be checked at index 0, followed by the seven gameplay scenes above. The saved shared list already has this order; check any active profile-specific override before building.
- Confirm Player Version and Analytics buildVersion both read `beta-0.1.2`; these values are already changed in source. Resolve Console errors and the release findings above.
- Run all four Navigation destinations by contact and by click; verify one transition/load, Coming Soon stays inactive, keyboard input does not submit Help unexpectedly, and the book dims uniformly with the entering overlay.
- Test Home and Help across every gameplay scene: pause/resume, no-checkpoint state, checkpoint retry, restart confirmation/cancel, return to Navigation and no stuck time scale or input.
- Repeat the Level01 gap in both directions, edge stop/restart and resizing; checkpoint-2 approach; ball camera stability; Box-on-ball recovery before checkpoint 3; and existing route/checkpoint behavior.
- Test IntroSpider climb arrows, first cake contact versus later spike death, release/recovery and IntroSpider → IntroFan → Level02. Confirm existing Box/Bubble/BouncyBall forward and return passages.
- Test individual discovery with old completion saves present, an early exit before possession, skipped basic school, actual possession, completion without extra rewards, cross-scene returns/retries and a new launch. Do not clear unrelated PlayerPrefs or analytics identifiers.
- For a future build, export current maps, then use **Build And Run** into a new version-specific folder; preserve the published `Builds/WebGL-Beta-0.1.2`. Standard Build Profiles does not run the custom map-hash guard. The custom **Build → Build WebGL** menu defaults to `Builds/WebGL` unless launched with `-customBuildPath`; do not confuse its default with the version-specific export target.
- Test the local HTTP player, not a `file://` launch. Check 16:9 framing, fullscreen/windowed loading, all scene links, Help/Home and unlock behavior. Record the exact tested build and browser. Keep mobile claims separate from desktop recording acceptance.
- Check new runtime events use `beta-0.1.2`, `IntroFan` and matching exported map versions; Navigation remains untracked as a gameplay level. Verify actual production rows before claiming delivery. Recording/rehearsal runs use the retained production configuration and may add real data.
- Deployment and hosted payload verification are complete for 0.1.2. Exercise the hosted gameplay before recording and record the browser/scenarios actually tested. Use [publish_instructions.md](publish_instructions.md) for future deployments.

## Verification evidence and limits

The owner completed the Web build, then authorized publication to both URLs. The publishing checkout was synchronized, the two copies were verified, and commit `64b35c6` was pushed to `gh-pages`. Deployment and hosted file checks passed. The earlier isolated session-discovery and tutorial Help results remain recorded in [AnalyticsTestPlan.md](AnalyticsTestPlan.md#session-discovery-regression). This verifies deployment and package integrity, not a new hosted gameplay playtest, mobile acceptance or production analytics delivery. Analytics map export/backend updates were not performed in this publication; Level02 repairs remain deferred to its replacement scene.

## Published artifact SHA-256

These relative paths have the same bytes at the root and under `beta/`. Checked November 6 after deployment.

| File | SHA-256 |
| --- | --- |
| `index.html` | `3148ecdb800859dd39c62238eb5fdfe36b5ef17225c0024bce5604c709f5e046` |
| `TemplateData/alpha.css` | `4148e1c051bc555368d09c3e9a1701e50a7a734fba5fabaa32f9edc7bb18fd93` |
| `TemplateData/alpha.js` | `98c4720054adf740817ffbd1386128f8b819e0c7ff7a106e3d4796758241596c` |
| `Build/e57746da5c241766158c3cecec2234b1.data.unityweb` | `25ffcd53317dbfb697b912ac30027155cfbb235113df688f90d1eb63aa712ff1` |
| `Build/43e1d85759a0811fcc8f42f5bcbb9964.loader.js` | `a688af773440f1138786b18417194e346de6640ad0956fd4bb1d5196f5c2d4ad` |
| `Build/b4558ba17c924fd6e835b75d1c8f1406.wasm.unityweb` | `2a5a028268851f53ecbafa510ec1384c7b1681313d726ffea2d3e221f723cb12` |
| `Build/06a09d92e03272afcc9b14e5486751f3.framework.js.unityweb` | `d14eb45683bfcc46424c48958d6b5aa394515a5d292f01dbed7f21929499be7c` |
