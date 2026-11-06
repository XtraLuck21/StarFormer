# Beta 0.1.2 release preparation

Prepared: November 6, 2026. Source baseline: `main` at `5e7105c`, plus the uncommitted documentation/version changes in this preparation. **Not built, published or accepted as a new Web release by this preparation.** Record the final source revision and any local changes after review; `5e7105c` alone does not identify the future export.

## Release configuration

| Item | Prepared state / next action |
| --- | --- |
| Player Version | `beta-0.1.2` in `ProjectSettings/ProjectSettings.asset` |
| Analytics buildVersion | `beta-0.1.2` in `Assets/Resources/AnalyticsSettings.asset` |
| Collection | Existing endpoint, `production`, collection enabled; no backend deployment performed |
| Editor | Unity `6000.3.22f1` with Web Build Support |
| Startup | `Navigation`, enabled build index 0; legacy `Mainmenu` retained but disabled |
| Enabled gameplay scenes | IntroBox, IntroBubble, IntroBouncyBall, Level01, IntroSpider, IntroFan, Level02, indices 1–7 |
| New export target | `Builds/WebGL-Beta-0.1.2`; preserve completed older exports |
| Intended publication | Replace only `/beta/` in the separate `gh-pages` checkout after validation |
| Last recorded published Beta | 0.1.1, source `9451511`, publication `e81097e`, package verified November 5 |
| Publication / recording readiness | Pending the checks below; no new deployment or recording is claimed |

The template remains `PROJECT:StarFormerAlpha`; its name is historical and its release label comes from Player Version. Current project settings retain 1920 × 1080, Gzip, Decompression Fallback, hashed payload names and disabled Web threads. Confirm Development Build is off in the active Web build configuration. Do not change Company/Product Name for this release: doing so can change the storage namespace.

## Changes since the recorded Beta 0.1.1 export

- **Level01 playtest fixes (PR #21, `42e32a4`):** bidirectional travel across the upper platform gap without maximum-size Box snagging; a wider checkpoint-2 approach that still requires player action; steadier camera framing during BouncyBall motion; recovery from Box-to-ball possession without the abandoned Box trapping the ball.
- **Navigation (PR #22, `dc28ff2`):** a separate black/yellow Navigation scene with a playable Star, A/D and Space key hints, upper jump entrances and lower drop entrances, bounce/flash feedback and an Entering overlay before loading. Mouse clicks also select entrances. Kindergarten → IntroBox; Level 1 → Level01; Elementary School → IntroSpider; Level 2 → Level02. Coming Soon has no playable destination.
- **Scene and menu integration:** FanTutorial is now IntroFan with its GUID retained. The forward chain and menu returns use the current scene names. All seven gameplay scenes have the shared top-right house/Star Home menu; return actions load Navigation. Legacy Mainmenu and test/reference scenes remain excluded from the build.
- **Help and collection:** reusable Collection Book in Navigation, aligned with five Shape Closet slots; shared form-demo assets; input blocking while the book or entering sequence is active; one entering overlay dims the book and the rest of Navigation together. Later main changes add per-form discovery pages to six gameplay scenes. Level02 still needs the help migration check below.
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

### Gameplay help migration needs a check

IntroBox, IntroBubble, IntroBouncyBall, Level01, IntroSpider and IntroFan contain six-page `HelpBookUI` configurations with form unlock IDs. IntroBox, IntroBubble and IntroSpider also retain enabled legacy help components on active scene objects with no page content. Level02's saved help component references the current HelpBookUI script GUID but retains the old HelpPanelUI serialized layout with no pages/content.

Inspect these scenes in Play Mode and check the Console. Verify whether the legacy panels still serve a purpose; migrate or disable obsolete components after review. Specifically open Level02 Help and confirm it has real content, page controls and correct pause behavior before claiming help is complete in every scene. The session-discovery regression reproduced missing-button/panel errors from retained legacy gameplay Help components. Level02’s page-content migration remains unverified. These existing Help issues are separate from the passing Closet/Navigation book discovery checks.

### Analytics maps must be regenerated

All seven runtime map `source_scene_hash` values differ from the saved current scenes. The custom `Build → Build WebGL` validation would reject this source until maps are exported again. No map images, hashes or historical bindings were fabricated or refreshed in this documentation pass.

1. Stop Play Mode and save intended scene changes.
2. Run **Analytics → Export all real level maps** in Unity. Confirm seven maps export without errors.
3. Review `Assets/Resources/AnalyticsMapCatalog.json`, `AnalyticsBackend/Maps/manifest.json`, versioned PNGs and `AnalyticsBackend/MapCatalog.html`. Preserve older map versions, including historical FanTutorial data.
4. Do not edit a gameplay scene after export without exporting again. Prefab/material changes can also require fresh maps even if the scene hash is unchanged.
5. For dashboard acceptance, separately deploy the refreshed catalog and current IntroFan-compatible backend to the existing Apps Script deployment, keeping its endpoint and historical rows. A game build or Git push does not do this.

## Unity and browser acceptance before recording

- Open **File → Build Profiles → Scene List**. Navigation must be checked at index 0, followed by the seven gameplay scenes above. The saved shared list already has this order; check any active profile-specific override before building.
- Confirm Player Version and Analytics buildVersion both read `beta-0.1.2`; these values are already changed in source. Resolve Console errors and the release findings above.
- Run all four Navigation destinations by contact and by click; verify one transition/load, Coming Soon stays inactive, keyboard input does not submit Help unexpectedly, and the book dims uniformly with the entering overlay.
- Test Home and Help across every gameplay scene: pause/resume, no-checkpoint state, checkpoint retry, restart confirmation/cancel, return to Navigation and no stuck time scale or input.
- Repeat the Level01 gap in both directions, edge stop/restart and resizing; checkpoint-2 approach; ball camera stability; Box-on-ball recovery before checkpoint 3; and existing route/checkpoint behavior.
- Test IntroSpider climb arrows, first cake contact versus later spike death, release/recovery and IntroSpider → IntroFan → Level02. Confirm existing Box/Bubble/BouncyBall forward and return passages.
- Test individual discovery with old completion saves present, an early exit before possession, skipped basic school, actual possession, completion without extra rewards, cross-scene returns/retries and a new launch. Do not clear unrelated PlayerPrefs or analytics identifiers.
- Export current maps, then use **Build And Run** into `Builds/WebGL-Beta-0.1.2`. Standard Build Profiles does not run the custom map-hash guard. The custom **Build → Build WebGL** menu defaults to `Builds/WebGL` unless launched with `-customBuildPath`; do not confuse its default with the version-specific export target.
- Test the local HTTP player, not a `file://` launch. Check 16:9 framing, fullscreen/windowed loading, all scene links, Help/Home and unlock behavior. Record the exact tested build and browser. Keep mobile claims separate from desktop recording acceptance.
- Check new runtime events use `beta-0.1.2`, `IntroFan` and matching exported map versions; Navigation remains untracked as a gameplay level. Verify actual production rows before claiming delivery. Recording/rehearsal runs use the retained production configuration and may add real data.
- Publish only the tested export following [publish_instructions.md](publish_instructions.md), then verify the hosted payload and gameplay before recording the hosted release. Record the final source revision, publishing commit, date and acceptance results.

## Evidence for this preparation

The initial release-preparation pass inspected versions, scene order, links and map hashes without running Unity or building. The subsequent session-discovery change removes the competing state sources described in that earlier review. Its isolated Unity regression results are recorded in [AnalyticsTestPlan.md](AnalyticsTestPlan.md#session-discovery-regression). No WebGL build, backend update, hosted verification, actual-user progress reset, commit or push is claimed by this follow-up. Earlier developer acceptance of PR #21/#22 does not certify the future Beta 0.1.2 export.
