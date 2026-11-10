# Beta 0.1.3 release preparation

**Prepared November 10, 2026; not published.** The proposed update replaces only the [Beta website](https://xtraluck21.github.io/StarFormer/beta/). The root website remains Beta 0.1.2 and historical Alpha paths are preserved. [Beta-0.1.2.md](Beta-0.1.2.md) is the unchanged historical release record.

## Release configuration

| Item | Prepared state |
| --- | --- |
| Player / Analytics version | `beta-0.1.3` |
| Gameplay baseline | `main` at `002368c50449ee9316e73b6d089e2e30afdead9a`, including PR #23 and subsequent hint changes |
| Build preparation | `0307f3d` restores Level02 identity; `a2f4a73` adds release validation and version settings |
| Source branch | `release/beta-0.1.3`, isolated at `.utmp/Beta013Source`; original main checkout left unchanged |
| Export | `Builds/WebGL-Beta-0.1.3`; a new export, preserving previous builds |
| Map inputs commit | `96c1617`; seven fresh maps and catalogs, with historical versions retained |
| Publishing commit | `8c40e40` on the preparation branch; not merged to live `gh-pages` |
| Review | Draft [source/documentation PR #24](https://github.com/XtraLuck21/StarFormer/pull/24) to `main`; draft [publishing PR #25](https://github.com/XtraLuck21/StarFormer/pull/25) to `gh-pages` |
| Publishing branch | `publish/beta-0.1.3`, isolated at `.utmp/Beta013Pages`, based on `origin/gh-pages` at `64b35c6` |
| Destination | `gh-pages/beta/` only; no root or Alpha replacement |
| Startup / scenes | Navigation, IntroBox, IntroBubble, IntroBouncyBall, Level01, IntroSpider, IntroFan, Level02 |
| Unity / template | `6000.3.22f1`, WebGL Support installed; `PROJECT:StarFormerAlpha` |
| Runtime settings | Gzip, Decompression Fallback, hashed payload names, Web threads disabled, 1920 × 1080 default canvas |
| Collection | Existing production endpoint, collection enabled and seven tracked gameplay scenes; no backend deployment |
| Publication | Requires the owner's final approval before merging/pushing to live `gh-pages` |

The build runs from the isolated worktree through `BetaReleaseBuild.Build`, which first checks scene identities/references, exports the seven maps and invokes the existing `WebGLBuild.Build` hash guard. The build invocation starts at `a2f4a73`; the freshly exported map files are generated inputs during that invocation. Documentation commits made afterwards do not change the exported runtime. The artifact hashes below identify the actual package; it has no embedded Git revision stamp.

## Changes since Beta 0.1.2

- **Promoted Level02:** Navigation and IntroFan load the replacement Level02. The original layout is retained as disabled Level02_legacy. Its duplicated scene GUID is corrected in the release branch: the promoted scene uses `7c566eddba60a82469938fe898ff5d22`, matching Build Settings; the legacy scene retains `688f3d52829cf43438b7318de504e103`.
- **Opening and descent:** Star escapes a pursuing spike wall through platforming, then drops into progressively darker exploration. Local light, glowing pickups and the illuminated doorway replace chase pressure with environmental guidance.
- **Four-gear seesaw:** Fan direction input releases one ordinary ball, with a two-shake anticipation animation and no additional supply while an ordinary ball remains. Blue/green key contacts remove the first barriers together, reclaim a controlled Fan and clear loose balls. Spider starts the yellow three-ball gust/loading stage and reveals the underground passage. Four BouncyBall underside impacts break the board; its left part becomes safe cliff support and the right part activates purple.
- **Completion and lift:** all four gears trigger a two-second quake before the exit platform appears. A continuous upward ride connects to the lower-left Smiley Face entrance. A riding BouncyBall shell cracks away and hands control back to Star.
- **Smiley Face:** the compact Bubble/water route leads to three movable teeth. Their arrival zones open the door. A fixed mouth camera gives way to a brief whole-character grin/eye-opening reveal. The player then contacts Spider manually, closes the return route and climbs to the forehead Bubble and three jumping-spike traps.
- **Final Box route:** a closer head camera returns to follow outside the forehead. The hand's Box uses J to grow and K to shrink through the restored larger-spike descent. The assigned Box presses the roof button, raising the flag; actual flag contact displays the custom congratulations overlay. No extra transport platform is added.
- **Guidance and recovery:** pure blue/yellow checkpoint stars, ordered form-aware Smiley Face saves, a forehead fall-return zone and an approximately 0.18-second Respawning pause reduce repeated traversal. World-space symbols and a large delayed floating yellow exclamation replace the removed blue/green text hints. Home/book icons use the shared gameplay presentation.
- **Analytics maps:** all seven maps are freshly exported from saved scenes, with current scene hashes and new image versions. Existing PNGs and backend catalog entries are retained, including historical FanTutorial data. Game publication does not deploy this catalog to Apps Script.

Mechanics, design rationale, scene wiring, exact recovery rules and remaining playtests are described in [level-2.md](level-2.md). The report distinguishes ordinary Fan-supplied balls from the possessable BouncyBall and actual collider clearance from a scripted minimum-size gate.

## Release review findings

### Scene identity and references

The initial main checkout was clean. Investigation found that Level02 and Level02_legacy shared the legacy GUID, while Build Settings already expected the replacement scene GUID. The release fix changes only the promoted scene's metadata. The original checkout and the legacy scene are preserved.

The release verifier opens all eight enabled scenes and checks for missing script components. It also checks Navigation's Level02 destination, the Level01 advanced controller, three tooth arrival triggers and required Smiley Face/seesaw references. Passing these checks establishes serialized wiring, not completion of every gameplay path.

### Remaining gameplay and analytics acceptance

- `SmileGoalFlag` currently displays its own victory overlay and disables the winning body. It does not invoke the shared `AnalyticsManager.TrackLevelComplete` flow or provide a Navigation-return button. Home also expects an enabled controlled body, so return from this overlay needs owner review before release acceptance.
- Level02 contains a configured six-page Collection Book alongside a legacy disabled empty Help component. The local WebGL smoke test confirmed its empty-Pages initialization warning. The configured book opens and closes correctly in the tested Star page; this is not complete Help cleanup or all-form page acceptance.
- The restored six-spike final Box layout, complete four-gear route, checkpoint retries and full smile-to-flag sequence still need a fresh end-to-end gameplay test of this exact WebGL export. Source inspection and scene checks do not establish passage clearance or full completion.
- Real mobile gameplay, touch form controls, every Navigation entrance by physical contact and a complete hosted playthrough remain separate acceptance work.
- Fresh map export does not deploy Apps Script. Synchronize the compatible backend/catalog through the existing deployment process, preserving the endpoint and historical rows, then verify production `beta-0.1.3` events and matching map rendering. Do not claim delivery from local test interception.

## Build and browser verification

| Check | Verified result and scope |
| --- | --- |
| Unity build | `WebGL build finished: result=Succeeded`, size 24,784,059 bytes, duration 00:07:54.3005852; batch exit code 0; no C# compile errors |
| Scenes | Eight enabled scenes in the expected order; zero missing script components; required Navigation, Level01, seesaw and Smiley Face references valid |
| Scene identity | Promoted Level02 GUID matches Build Settings; no duplicate GUIDs in project Assets metadata after the fix |
| Analytics maps | Seven scene hashes match saved scene files; runtime catalog and manifest agree; historical backend catalog versions retained, 20 -> 27 |
| Runtime package | Seven nonempty files; HTML references all four current hashed payloads; three Gzip payloads decode, including valid WebAssembly magic; template CSS/JS unchanged |
| Publishing scope | Seven copied files match export SHA-256; all 66 tracked files outside `beta/` retain their original bytes |
| Local loading | HTTP preview of the exact publishing checkout at `/beta/`; all seven files HTTP 200 in the initial run, with valid cache revalidation on later runs |
| Desktop browser | Headless Chromium 151.0.7922.34, Windows, software WebGL; Navigation renders and click entrances load Level01 and Level02; windowed start and enter/exit fullscreen work |
| Level02 menus | Configured Collection Book Star page and Home menu visibly open; Escape closes each; no-checkpoint Home entry is disabled as expected |
| Portrait presentation | Android/touch emulation at 390 × 844: rotation hint visible and 384 × 216 render canvas retains 16:9; not a physical-phone gameplay test |
| Runtime diagnostics | No JavaScript/runtime errors in these smoke scenarios. Level02 emits the legacy Help empty-Pages warning; initial loads also show Unity filesystem deprecation/software-GPU performance warnings |
| Analytics payloads | Locally intercepted session/level/attempt events use `beta-0.1.3`; Level01 map is `map-cc32b88d66f8-003e13417d1a`, Level02 map is `map-c29073455c6b-ac006d87430a` |
| Live deployment | Not performed. Pages is configured for legacy branch publication from `gh-pages`, `/`; live branch remains `64b35c6` during preparation |

The browser used a local analytics response stub and closed test pages after each scenario. Production endpoint/configuration in the package is unchanged; production delivery was not tested. No complete Level02 playthrough or real-device acceptance is claimed.

Local evidence: `Logs/Beta013-WebGL.log`, `Logs/Beta013-ArtifactManifest.json`, `Logs/Beta013-MapVerification.json`, `Logs/Beta013-PagesPreserved.json`, `Logs/Beta013-Browser-*.json` and screenshots. The isolated source checkout also contains `Logs/BetaReleaseSceneVerification.json`. These local logs are not runtime publishing files.

## Artifact SHA-256

These files identify the completed export and its byte-identical `beta/` copy. They have not yet been checked on the public Beta URL.

| File | Bytes | SHA-256 |
| --- | ---: | --- |
| `Build/0ae6b6293d71be7bbf6428d8fc848850.wasm.unityweb` | 12,394,441 | `411891e596fdce80235eb069ed5b6fc91a0b3ffa7e8d68bddc8b37a21c30b52f` |
| `Build/0fd4f92376723dc7c1fe554888f6097f.data.unityweb` | 12,241,377 | `fae75b8cb032cf56b2b0425001bb0579a867a006cf8e1272cdd681a89a2e5c19` |
| `Build/945709f604574d4d30dfc32a40a58952.framework.js.unityweb` | 89,805 | `0483073f2d750fa37a08bdb6a5d7a88278d71274c61f7982c7220062eacfeae0` |
| `Build/d6706a78ff21c01e04b218a5bce4e94d.loader.js` | 47,867 | `922a2a8955e7c991036abde79bb6c3639e3857ee884f6b73e2da42af6c69f466` |
| `TemplateData/alpha.css` | 2,518 | `11d261077bb812b9551bfefaeb57b762b031749baf6b23e337c73b38df90afcb` |
| `TemplateData/alpha.js` | 5,775 | `5f0caf2e6846ae7c7c5184ea9110f090316d4359859467eb6d8db244d1784762` |
| `index.html` | 2,276 | `070d1522ada85acf2a850a3966bee31e248d71582dda6d955e758a3202ae9629` |

## Publication steps

1. Review source/documentation PR #24 and publishing PR #25. Source preparation commits are `0307f3d`, `a2f4a73`, `96c1617` and documentation commit `9397730`; publishing commit is `8c40e40`. Confirm the identified gameplay limitations and backend acceptance have an owner decision.
2. Merge the source pull request into `main` after review. This does not publish the site.
3. Obtain the owner's explicit final publication approval. Only then merge the publishing pull request into `gh-pages`; do not force-push. Its diff must contain only `beta/`.
4. Wait for GitHub Pages to finish, request all seven `/StarFormer/beta/` files and compare their SHA-256 hashes with this export. Confirm root, Alpha and historical paths retain their previous bytes.
5. Run the hosted gameplay/device and production-analytics acceptance above. Record the deployment URL/run and actual outcomes before changing this record to published.

See [publish_instructions.md](publish_instructions.md) for the retained deployment history and working procedure.
