# Beta 0.1.3 release record

**Beta 0.1.3 was published on November 10, 2026** to the [Beta website](https://xtraluck21.github.io/StarFormer/beta/). [GitHub Pages deployment](https://github.com/XtraLuck21/StarFormer/actions/runs/42806924119) succeeded. All seven hosted runtime files returned HTTP 200 and matched the cleanup export byte for byte by SHA-256. The root remains Beta 0.1.2; Alpha and other historical paths are preserved. Historical evidence remains in [Beta-0.1.2.md](Beta-0.1.2.md).

## Release configuration

| Item | Prepared state |
| --- | --- |
| Player / Analytics version | `beta-0.1.3` |
| Gameplay baseline | `main` at `002368c50449ee9316e73b6d089e2e30afdead9a`, including PR #23 and subsequent hint changes |
| Build preparation | `0307f3d` restores Level02 identity; `a2f4a73` adds release validation and version settings |
| Source branch | `release/beta-0.1.3`, isolated at `.utmp/Beta013Source` |
| Export | `Builds/WebGL-Beta-0.1.3-help-cleanup`; replaces the prepared artifact, preserving the initial export |
| Map inputs commit | `538cf4c` updates Level02 after cleanup; original seven-map export `96c1617`; historical versions retained |
| Publishing commits | PR #25 merge `675c690`; exact-byte preservation follow-up `5603d2f` on live `gh-pages` |
| Review | Merged [source/documentation PR #24](https://github.com/XtraLuck21/StarFormer/pull/24) to `main`; merged [publishing PR #25](https://github.com/XtraLuck21/StarFormer/pull/25) to `gh-pages` |
| Publishing branch | `publish/beta-0.1.3`, isolated at `.utmp/Beta013Pages`, originally based on `origin/gh-pages` at `64b35c6` |
| Destination | `gh-pages/beta/` only; no root or Alpha replacement |
| Startup / scenes | Navigation, IntroBox, IntroBubble, IntroBouncyBall, Level01, IntroSpider, IntroFan, Level02 |
| Unity / template | `6000.3.22f1`, WebGL Support installed; `PROJECT:StarFormerAlpha` |
| Runtime settings | Gzip, Decompression Fallback, hashed payload names, Web threads disabled, 1920 × 1080 default canvas |
| Collection | Existing production endpoint, collection enabled and seven tracked gameplay scenes; no backend deployment |
| Publication | Owner approved November 10; live `5603d2f`; deployment run `42806924119` succeeded |

The build runs from the isolated worktree through `BetaReleaseBuild.Build`, which first checks scene identities/references, exports the seven maps and invokes the existing `WebGLBuild.Build` hash guard. The initial export was built from `a2f4a73` and remains preserved at `Builds/WebGL-Beta-0.1.3`. The replacement export starts at `c8f05c1`, which removes the obsolete empty Level02 Help component; refreshed map files are generated inputs during that invocation. Documentation commits made afterwards do not change the exported runtime. The artifact hashes below identify the actual package; it has no embedded Git revision stamp.

## Changes since Beta 0.1.2

- **Promoted Level02:** Navigation and IntroFan load the replacement Level02. The original layout is retained as disabled Level02_legacy. Its duplicated scene GUID is corrected in the release branch: the promoted scene uses `7c566eddba60a82469938fe898ff5d22`, matching Build Settings; the legacy scene retains `688f3d52829cf43438b7318de504e103`.
- **Opening and descent:** Star escapes a pursuing spike wall through platforming, then drops into progressively darker exploration. Local light, glowing pickups and the illuminated doorway replace chase pressure with environmental guidance.
- **Four-gear seesaw:** Fan direction input releases one ordinary ball, with a two-shake anticipation animation and no additional supply while an ordinary ball remains. Blue/green key contacts remove the first barriers together, reclaim a controlled Fan and clear loose balls. Spider starts the yellow three-ball gust/loading stage and reveals the underground passage. Four BouncyBall underside impacts break the board; its left part becomes safe cliff support and the right part activates purple.
- **Completion and lift:** all four gears trigger a two-second quake before the exit platform appears. A continuous upward ride connects to the lower-left Smiley Face entrance. A riding BouncyBall shell cracks away and hands control back to Star.
- **Smiley Face:** the compact Bubble/water route leads to three movable teeth. Their arrival zones open the door. A fixed mouth camera gives way to a brief whole-character grin/eye-opening reveal. The player then contacts Spider manually, closes the return route and climbs to the forehead Bubble and three jumping-spike traps.
- **Final Box route:** a closer head camera returns to follow outside the forehead. The hand's Box uses J to grow and K to shrink through the restored larger-spike descent. The assigned Box presses the roof button, raising the flag; actual flag contact displays the custom congratulations overlay. No extra transport platform is added.
- **Guidance and recovery:** pure blue/yellow checkpoint stars, ordered form-aware Smiley Face saves, a forehead fall-return zone and an approximately 0.18-second Respawning pause reduce repeated traversal. World-space symbols and a large delayed floating yellow exclamation replace the removed blue/green text hints. Home/book icons use the shared gameplay presentation.
- **Help cleanup:** remove only the obsolete empty LevelCanvas Help component and its owner-list entry. Preserve the newer six-page book, buttons and other scene objects.
- **Analytics maps:** all seven maps are freshly exported from saved scenes, with current scene hashes and new image versions. Existing PNGs and backend catalog entries are retained, including historical FanTutorial data. Game publication does not deploy this catalog to Apps Script.

Mechanics, design rationale, scene wiring, exact recovery rules and remaining playtests are described in [level-2.md](level-2.md). The report distinguishes ordinary Fan-supplied balls from the possessable BouncyBall and actual collider clearance from a scripted minimum-size gate.

## Release review findings

### Scene identity and references

The initial main checkout was clean. Investigation found that Level02 and Level02_legacy shared the legacy GUID, while Build Settings already expected the replacement scene GUID. The release fix changes only the promoted scene's metadata. The legacy scene is preserved; the subsequent requested Help cleanup is also applied to the main working copy.

The release verifier opens all eight enabled scenes and checks for missing script components. It also checks Navigation's Level02 destination, the Level01 advanced controller, three tooth arrival triggers and required Smiley Face/seesaw references. Passing these checks establishes serialized wiring, not completion of every gameplay path.

### Level02 Help cleanup

The initial export confirmed an empty-Pages warning from an obsolete disabled Help component. `c8f05c1` removes only that component and its LevelCanvas owner-list entry. The configured six-page Collection Book, buttons and other objects are unchanged. The replacement WebGL package was rebuilt and tested: the book and Home open/close correctly and the Help warning is absent. Focused desktop checks do not establish all-form page or real-device acceptance.

### Remaining gameplay and analytics acceptance

- `SmileGoalFlag` currently displays its own victory overlay and disables the winning body. It does not invoke the shared `AnalyticsManager.TrackLevelComplete` flow or provide a Navigation-return button. Home also expects an enabled controlled body, so return from this overlay needs owner review before release acceptance.
- The restored six-spike final Box layout, complete four-gear route, checkpoint retries and full smile-to-flag sequence still need a fresh end-to-end gameplay test of this exact WebGL export. Source inspection and scene checks do not establish passage clearance or full completion.
- Real mobile gameplay, touch form controls, every Navigation entrance by physical contact and a complete hosted playthrough remain separate acceptance work.
- Fresh map export does not deploy Apps Script. Synchronize the compatible backend/catalog through the existing deployment process, preserving the endpoint and historical rows, then verify production `beta-0.1.3` events and matching map rendering. Do not claim delivery from local test interception.

## Build and browser verification

| Check | Verified result and scope |
| --- | --- |
| Unity build | `WebGL build finished: result=Succeeded`, size 24,782,494 bytes, duration 00:00:51.4957050; batch exit code 0; no C# compile errors |
| Scenes | Eight enabled scenes in the expected order; zero missing script components; required Navigation, Level01, seesaw and Smiley Face references valid |
| Scene identity | Promoted Level02 GUID matches Build Settings; no duplicate GUIDs in project Assets metadata after the fix |
| Analytics maps | Seven scene hashes match saved scene files; runtime catalog and manifest agree; historical backend catalog versions retained, 20 -> 28 (27 -> 28 in the cleanup export) |
| Runtime package | Seven nonempty files; HTML references all four current hashed payloads; three Gzip payloads decode, including valid WebAssembly magic; template CSS/JS unchanged |
| Publishing scope | Seven copied files match export SHA-256; all 66 tracked files outside `beta/` retain their original bytes |
| Local loading | HTTP preview of the exact publishing checkout at `/beta/`; all seven files HTTP 200 in the initial run, with valid cache revalidation on later runs |
| Desktop browser | Headless Chromium 151.0.7922.34, Windows, software WebGL; Navigation renders and click entrances load Level01 and Level02; windowed start and enter/exit fullscreen work |
| Level02 menus | Configured Collection Book Star page and Home menu visibly open; Escape closes each; no-checkpoint Home entry is disabled as expected |
| Portrait presentation | Android/touch emulation at 390 × 844: rotation hint visible and 384 × 216 render canvas retains 16:9; not a physical-phone gameplay test |
| Runtime diagnostics | No JavaScript/runtime errors in these smoke scenarios. the Level02 Help empty-Pages warning is absent after cleanup; initial loads still show Unity filesystem deprecation/software-GPU performance warnings |
| Analytics payloads | Locally intercepted session/level/attempt events use `beta-0.1.3`; Level01 map is `map-cc32b88d66f8-003e13417d1a`, Level02 map is `map-9bdd1ef5f7db-ac006d87430a` |
| Hosted browser | Chromium 151 loads the published `/beta/`, enters Level02, switches fullscreen and opens/closes book/Home; no Help warning or runtime errors in this smoke scenario; analytics intercepted |
| Live deployment | Successful run `42806924119`, commit `5603d2f`; seven hosted Beta file hashes match the export |

The browser used a local analytics response stub and closed test pages after each scenario. Production endpoint/configuration in the package is unchanged; production delivery was not tested. No complete Level02 playthrough or real-device acceptance is claimed.

Cleanup-export evidence: `Logs/Beta013-HelpCleanup-WebGL.log`, `Logs/Beta013HelpCleanup-ArtifactManifest.json`, `Logs/Beta013HelpCleanup-MapVerification.json`, `Logs/Beta013HelpCleanup-PagesPreserved.json`, `Logs/Beta013HelpCleanup-Browser-*.json` and screenshots. Initial-export evidence remains under the original `Beta013-` names. The isolated source checkout also contains `Logs/BetaReleaseSceneVerification.json`. Hosted evidence is in `Logs/Beta013Hosted-Browser-level2menus.json`, screenshots, `Logs/Beta013-HostedVerification.json`, `Logs/Beta013-PagesDeployment.json` and `Logs/Beta013-PagesGitBlobPreservation.json`. These local logs are not runtime publishing files.

## Artifact SHA-256

These files identify the completed export and live `beta/` package. All seven returned HTTP 200 and matched these SHA-256 hashes after deployment.

| File | Bytes | SHA-256 |
| --- | ---: | --- |
| `Build/0ae6b6293d71be7bbf6428d8fc848850.wasm.unityweb` | 12,394,441 | `411891e596fdce80235eb069ed5b6fc91a0b3ffa7e8d68bddc8b37a21c30b52f` |
| `Build/945709f604574d4d30dfc32a40a58952.framework.js.unityweb` | 89,805 | `0483073f2d750fa37a08bdb6a5d7a88278d71274c61f7982c7220062eacfeae0` |
| `Build/d6706a78ff21c01e04b218a5bce4e94d.loader.js` | 47,867 | `922a2a8955e7c991036abde79bb6c3639e3857ee884f6b73e2da42af6c69f466` |
| `Build/df49b1cf252cbe0269a5e57efd208f0f.data.unityweb` | 12,239,812 | `2d275a8d5681b1e8ec1d5114141e34a3f5ad9cb53c01aeed4e50f1be54e3770a` |
| `TemplateData/alpha.css` | 2,518 | `11d261077bb812b9551bfefaeb57b762b031749baf6b23e337c73b38df90afcb` |
| `TemplateData/alpha.js` | 5,775 | `5f0caf2e6846ae7c7c5184ea9110f090316d4359859467eb6d8db244d1784762` |
| `index.html` | 2,276 | `21c82e0c9a6da92e48d345654dedbd992a01245f0816932196c1150b21564347` |

## Publication and follow-up

1. Owner approved publication. Source/documentation PR #24 merged into `main` as `db8cfac`, including the README Play link to `/beta/`.
2. Publishing PR #25 merged normally into `gh-pages` as `675c690`; no force-push or history rewrite. Follow-up `5603d2f` adds `beta/.gitattributes` and preserves the exact exported text bytes after Windows Git normalization was detected.
3. [GitHub Pages deployment](https://github.com/XtraLuck21/StarFormer/actions/runs/42806924119) (run `42806924119`) completed successfully for `5603d2f`.
4. All seven Beta runtime URLs returned HTTP 200 and matched the export. Five historical launch-page hashes match their pre-release Git blobs. All 66 tracked files outside `beta/` retain their original Git blob entries. Keep `beta/.gitattributes` during future copies and validate staged/committed blobs as well as worktree files.
5. Full hosted gameplay, real-device controls and production analytics/backend acceptance remain follow-ups. Publication does not deploy Apps Script or prove event delivery.

See [publish_instructions.md](publish_instructions.md) for the retained deployment history and working procedure.
