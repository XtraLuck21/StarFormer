# Publish StarFormer to GitHub Pages

## Kindergarten polish source changes awaiting release

`gold-polish-kindergarten` through `2cbed98` changes three tutorial scenes, their woodland/UI art, recovery and graduation; it does not publish a new Gold build. Keep the recorded Gold 0.1.0 export intact. Choose the next version/output separately, save the final scenes and regenerate their analytics maps/catalog before exporting. Map/backend and hosted gameplay acceptance remain release work, not completed branch work.

Use the [current branch checklist](AnalyticsTestPlan.md#kindergarten-polish-source-acceptance) alongside the full-game checks below. Historical release records retain their original versions and evidence.

## Gold 0.1.0 published

Gold 0.1.0 was published on November 17, 2026 to the main site and `gold/`. Both serve the verified `Builds/WebGL-Gold-0.1.0/` export. [Main site](https://xtraluck21.github.io/StarFormer/) · [Gold site](https://xtraluck21.github.io/StarFormer/gold/)

The `gh-pages` publishing commit is `8b3075d`. GitHub Pages deployment succeeded, and all 14 hosted runtime files matched the local export by SHA-256. Build settings, reproduction steps and remaining gameplay checks are recorded in the Gold release document. [Deployment](https://github.com/XtraLuck21/StarFormer/actions/runs/44143878818) · [Gold release record](Gold-0.1.0.md)

For this release, the publishing checkout was `StarFormer-Pages` on `gh-pages`. Root `index.html`, `Build/` and `TemplateData/` were synchronized from the export, and the complete export was copied into `gold/`. Staged files were compared byte for byte before committing and pushing. The records and commands below describe earlier Beta releases.

## Beta 0.1.4 release history

The Pages branch recorded Beta 0.1.4 at `beta/` in `b4b979a` and at the root in `e291523`.

See the [Beta 0.1.4 build notes](Beta-0.1.4.md) for Unity settings, the build command and local preview steps.

## Beta 0.1.3 release status

**Beta 0.1.3 was published on November 10, 2026** to the [Beta website](https://xtraluck21.github.io/StarFormer/beta/). [GitHub Pages deployment](https://github.com/XtraLuck21/StarFormer/actions/runs/42806924119) succeeded. All seven hosted runtime files returned HTTP 200 and matched the cleanup export byte for byte by SHA-256. The root remains Beta 0.1.2; Alpha and other historical paths are preserved.

This release updates **only `/StarFormer/beta/`**, retaining the same URL. Root remains Beta 0.1.2; Alpha, Alpha Progress Check, playtest, analytics-test paths and `.nojekyll` must be preserved. The historical release records below remain unchanged.

| Item | Prepared state |
| --- | --- |
| Gameplay baseline | Latest fetched `main`, `002368c`; replacement Level02 integrated through PR #23 |
| Source checkout | `.utmp/Beta013Source`, branch `release/beta-0.1.3` |
| Versions | Player and Analytics `beta-0.1.3`; production collection settings retained |
| Build | Unity `6000.3.22f1`; new `Builds/WebGL-Beta-0.1.3-help-cleanup` export |
| Publishing checkout | `.utmp/Beta013Pages`, branch `publish/beta-0.1.3`, based on live `gh-pages` at `64b35c6` |
| Scope | Replace `beta/` files only; source/docs and publishing commits stay separate |
| Approval | Owner approved November 10; publishing completed at `5603d2f` |
| Review | Merged [source PR #24](https://github.com/XtraLuck21/StarFormer/pull/24) to `main`; merged [publishing PR #25](https://github.com/XtraLuck21/StarFormer/pull/25) to `gh-pages` |

The Help cleanup replacement export is verified: the legacy empty LevelCanvas component is removed, the configured book still opens/closes, and the Pages warning is absent in local Chromium. Initial `Builds/WebGL-Beta-0.1.3` remains preserved; the publishing PR now uses the cleanup export.

The initial main checkout was clean. The promoted Level02 metadata incorrectly duplicated Level02_legacy's GUID. The source branch restores the replacement GUID to match Build Settings, while retaining the legacy scene disabled. `BetaReleaseBuild.Build` validates enabled scene identities and required references, exports seven current Analytics maps and calls the existing map-validating WebGL build script. The StarFormerAlpha template, Gzip/fallback and endpoint are preserved. See [Beta-0.1.3.md](Beta-0.1.3.md) for build evidence, artifact hashes, review findings and remaining gameplay/backend acceptance.

To reproduce in a separate checkout, choose a **new unused output directory** rather than overwriting a verified export:

```powershell
$releaseProject = (Resolve-Path '.utmp/Beta013Source').Path
$releaseOutput = Join-Path (Get-Location) 'Builds/WebGL-Beta-0.1.3-recheck'
if (Test-Path -LiteralPath $releaseOutput) { throw 'Choose an unused export directory.' }
$releaseLog = Join-Path (Get-Location) 'Logs/Beta013-WebGL-recheck.log'
& 'C:/Program Files/Unity/Hub/Editor/6000.3.22f1/Editor/Unity.exe' `
  -batchmode -quit -projectPath $releaseProject -buildTarget WebGL `
  -executeMethod BetaReleaseBuild.Build -customBuildPath $releaseOutput -logFile $releaseLog
```

Keep the active Editor project separate from this batch invocation. A failed export must not be copied to the publishing checkout. Serve the completed package over local HTTP and record actual browser coverage. Local smoke tests must not be described as full gameplay, mobile or production-data acceptance.

For future releases, preserve `beta/.gitattributes` (`* -text`) so Windows Git retains the exact WebGL export bytes. Compare staged and committed Git blobs with the export, then compare hosted files; worktree-only comparison does not detect newline conversion. Historical comparisons use original Git blobs rather than Windows-smudged worktree files.

Before publication, review `git diff --stat` and `git diff --name-only` against `origin/gh-pages`: every changed path must begin `beta/`. Compare the seven publishing files byte for byte with the validated export and confirm every tracked file outside `beta/` retains its baseline hash. Push only the preparation branch and open a draft PR targeting `gh-pages`; this does not authorize merging it. After the owner's final approval, merge normally, wait for Pages, then verify all hosted Beta file hashes and retained historical paths. Apps Script catalog deployment is a separate operation.

## Beta 0.1.2 release status

**Beta 0.1.2 was published on November 6, 2026** to both the [main website](https://xtraluck21.github.io/StarFormer/) and [Beta website](https://xtraluck21.github.io/StarFormer/beta/).

The same `Builds/WebGL-Beta-0.1.2` export was committed and pushed on `gh-pages` as [`64b35c6`](https://github.com/XtraLuck21/StarFormer/commit/64b35c6c5dbe50ffefc05e1590533cebf47888dc). [GitHub Pages deployment](https://github.com/XtraLuck21/StarFormer/actions/runs/42303613672) succeeded. All seven files at each URL (14 files total) returned HTTP 200 and matched the local export byte for byte by SHA-256.

| Item | Recorded state |
| --- | --- |
| Version | Player and Analytics: `beta-0.1.2` |
| Source checkout at publication | Clean main `b93768f` (export has no embedded Git revision stamp) |
| Export | `Builds/WebGL-Beta-0.1.2` |
| Publishing commit | `64b35c6`, November 6 at 14:17:44 PST |
| Previous root / beta versions | 0.1.0 / 0.1.1; both replaced with the same 0.1.2 export |
| Preserved | `alpha/`, `alpha-progress-check/`, `playtest-01/`, `analytics-webgl-test/`, `.nojekyll` and older local exports |
| Remaining acceptance | Hosted gameplay/device checks, production event delivery and map/backend acceptance |

This verifies deployment and package integrity, not a new hosted gameplay playtest, mobile acceptance or production analytics delivery. Analytics map export/backend updates were not performed in this publication; Level02 repairs remain deferred to its replacement scene. See [Beta-0.1.2.md](Beta-0.1.2.md) for the feature summary, file hashes and verification scope.

Last updated: 2026-11-06.

- [Main website](https://xtraluck21.github.io/StarFormer/) — Beta 0.1.2.
- [Beta website](https://xtraluck21.github.io/StarFormer/beta/) — the same Beta 0.1.2 build.
- [Historical Alpha](https://xtraluck21.github.io/StarFormer/alpha/) — Alpha 0.1.2, unchanged.
- [Alpha Progress Check](https://xtraluck21.github.io/StarFormer/alpha-progress-check/) — frozen All-Level V2 submission, unchanged.

Alpha `alpha-0.1.2` was published October 18 as `5f62980`, from source configuration `5650250` and gameplay/UI `9b35f8e`. Export: `Builds/WebGL-Alpha-0.1.2`. [Deployment](https://github.com/XtraLuck21/StarFormer/actions/runs/39173067360), package checks and developer-reported gameplay checks passed. Its Sheet delivery remains separately unconfirmed.

## Beta 0.1.1 release status

**Historical release: published November 3, 2026; deployed files verified November 5.** `/beta/` was replaced by 0.1.2 on November 6; preserve the original export and evidence.

| Item | Recorded state |
| --- | --- |
| Player Version / Analytics buildVersion | Both `beta-0.1.1` |
| Analytics configuration | Existing endpoint, collection enabled, `production`; schema v2 tracks all seven gameplay scenes |
| Changes since 0.1.0 | All-level analytics integration, refreshed maps and Level01 contextual movable-door hint (`2d8738d`) |
| Maps | Runtime catalog, manifest, versioned PNGs and backend catalog refreshed; old maps retained |
| Export folder | `Builds/WebGL-Beta-0.1.1` (completed export; preserve as release evidence) |
| Built source | [`9451511`](https://github.com/XtraLuck21/StarFormer/commit/9451511), before the November 4 UI commits |
| Publishing commit | [`e81097e`](https://github.com/XtraLuck21/StarFormer/commit/e81097e), November 3 at 14:46 Los Angeles time; changed only `beta/` |
| Original URL | `/beta/`, now serving 0.1.2; 0.1.1 remains in the local export and Git history |
| Deployment verification | November 5: all seven live files HTTP 200, byte-identical to the local export and publishing checkout |
| Remaining acceptance | Detailed local/hosted gameplay scenarios, production raw rows and refreshed backend map deployment/rendering are not verified by the package comparison |

The source release-preparation commit was rebased to `2caffd9` on November 5, after the four UI commits had landed. That commit was the integrated main tip at the earlier documentation checkpoint, **not** the built source revision. The historical 0.1.1 export was built from `9451511`; it is no longer the live Beta package.

### Source changes after this export

The November 4–6 help, navigation, Level01 and Spider changes are not in the recorded 0.1.1 export. They are now included in the published beta-0.1.2 package above. The rebased `2caffd9` patch is historical integrated source, not the built revision for 0.1.1. Preserve the completed export and its verification record.

### Remaining analytics acceptance

Publication of the game does not deploy Apps Script. Verify whether the refreshed `AnalyticsBackend/MapCatalog.html` has been deployed to the existing project before claiming new map rendering works. If it has not, synchronize it through the existing backend deployment process while retaining the endpoint, historical maps and recorded rows. Verify production rows carry `beta-0.1.1` and each `map_version` resolves to the matching image. See [the 0.1.1 acceptance checklist](AnalyticsTestPlan.md#beta-011-acceptance-status).

Before a future build, run **Analytics → Export all real level maps** again if gameplay scenes have changed. `Build → Build WebGL` checks scene hashes; the standard Build Profiles button does not call that custom validation. Preserve the completed 0.1.1 export.

## Beta 0.1.0 release status

**Historical release: published November 2, 2026; local and hosted playtests passed according to the developer.** This export includes the fully rebuilt formal Level01 and IntroBouncyBall between IntroBubble and Level01. Box/Bubble and Bubble/BouncyBall support two-way passage travel; BouncyBall's flag starts Level01. Level01 continues through IntroSpider → FanTutorial → Level02.

| Item | Current status / target |
| --- | --- |
| Player Version / Analytics buildVersion | Both `beta-0.1.0` |
| Analytics | Collection enabled; existing endpoint and `production` environment retained; only Level01 and Level02 tracked |
| Export folder | `Builds/WebGL-Beta-0.1.0` |
| Publication | Originally `/beta/` on November 2 and then root via `f6eaf9e`; both URLs now serve 0.1.2 |
| Local build evidence | Developer confirmed success after `3365e29` removed the editor-only Texture2D property from FanPossessionHint |
| Final export | Rebuilt after Analytics/template updates; developer-reported local test passed; all seven deployed files matched the tested export byte for byte |
| Final source / publishing commit | [`3a693ca`](https://github.com/XtraLuck21/StarFormer/commit/3a693ca) / [`1105970`](https://github.com/XtraLuck21/StarFormer/commit/1105970) |
| Deployment / hosted gameplay | [Pages build and deployment succeeded](https://github.com/XtraLuck21/StarFormer/actions/runs/41410712039); developer-reported online test passed |
| Beta analytics | Records reported received; supplied production dashboard screenshot confirms Beta Level01 events; Level02-specific and raw-row metric checks remain open |
| Historical dashboard issue | Initial Beta Level01 grid fallback was subsequently resolved by Version 9 release-map bindings; see AnalyticsDashboard.md |

The existing template folder remains `StarFormerAlpha`; its browser title and launch badge now use Player Version rather than hard-coded Alpha text. Folder/CSS/JS names do not determine the release label.

Use `StarFormer` as the source checkout and the sibling `StarFormer-Pages` checkout on `gh-pages` for exported files. Do not merge `gh-pages` into `main`. Coordinate one publisher per release. For the authorized root-and-Beta release, update root runtime files and `beta/` together; preserve `alpha/`, `playtest-01/`, `alpha-progress-check/`, `analytics-webgl-test/` and other historical paths. GitHub Pages is publicly accessible.

The published 0.1.1 collector tracks all seven gameplay scenes; the historical 0.1.0 and Alpha exports retain their embedded configuration. Production events append to the same Sheet and are separated by `build_version`, not URL path. The November 2 dashboard evidence applies to 0.1.0 only. Continue the 0.1.1 raw-row/map checks in [AnalyticsTestPlan.md](AnalyticsTestPlan.md); successful loading or Editor JSON alone is not delivery evidence.

Commands use macOS/Linux shell syntax and assume the folders are siblings. You need repository push access and Unity **6000.3.22f1** with **Web Build Support**. Stop if a command fails.

The procedure below documents the two-destination publishing layout used for 0.1.2. That deployment is complete; do not rerun these commands merely to update documentation. For a future release, choose its agreed version and a new output folder. Preserve completed local exports. Map/backend acceptance and hosted gameplay checks remain separate from the successful deployment.

## 1. Update source

Beta 0.1.2 was published with a clean source checkout at `b93768f`. For a future source refresh, close Unity and run `git status` in `StarFormer`. Review local changes and commit intended changes or stash them before continuing; do not discard unfamiliar changes. With a clean working tree:

```bash
git switch main
git pull --ff-only origin main
git rev-parse --short HEAD
```

Record the source commit and any intentional local settings used for the build.

## 2. Build and test locally

Open Unity, wait for imports and compilation, and test from `Navigation`. Resolve Console errors. In **File → Build Profiles → Web**, enable only:

```text
0  Navigation
1  IntroBox
2  IntroBubble
3  IntroBouncyBall
4  Level01
5  IntroSpider
6  IntroFan
7  Level02
```

Keep Mainmenu, Level01_Legacy, the old Tutorial, reference scenes and sandboxes unchecked. Level01 is the promoted Advanced scene; do not add a duplicate Advanced entry. Forward progression is name-based, not build-index order. Open **Scene List** to edit or drag the order; Navigation must remain the startup scene.

In **Player Settings → Resolution and Presentation**, choose the **StarFormerAlpha** Web template and set the default canvas size to **1920 × 1080**. Verify that the Player **Version** for the build matches `Assets/Resources/AnalyticsSettings.asset` → `buildVersion`. Keep **Gzip**, **Decompression Fallback on**, **Development Build off**, and **Threads Support off**. Enable **Name Files As Hashes** to give changed build payloads distinct URLs when replacing `beta/`.

The Web template in `Assets/WebGLTemplates/StarFormerAlpha/` fits desktop and portrait views to 16:9. On a landscape phone it fills the available safe viewport and updates the canvas when the browser toolbar or orientation changes. The render buffer follows the canvas aspect ratio. Pixel density is capped at 2× on desktop and 1.5× on touch devices, with maximum render widths of 2560 and 1920 respectively.

Desktop offers **Play Fullscreen** and **Play in Window** when fullscreen is supported. In windowed play, the lower-right icon enters fullscreen. Successful entry from the ordinary Home pause menu resumes play. The first Esc exits fullscreen. Phones get one **Play** button, which requests fullscreen when supported and otherwise plays in the browser viewport. The desktop corner icon stays hidden on phones. Portrait phones show a rotation hint.

The template uses relative asset paths and versions its CSS and JS URLs with the build’s code filename. Keep the complete export together.

Before building, stop Play Mode, save the intended scenes and run **Analytics → Export all real level maps**. The 0.1.2 pre-publication review found all seven maps stale; that release operation did not regenerate them. Review the runtime/backend catalogs and new PNGs, retaining historical maps. Standard Build Profiles does not run the custom hash validator; the custom **Build → Build WebGL** does, but defaults to `Builds/WebGL` unless `-customBuildPath` is provided.

Choose **Build And Run** with a new version-specific output folder. The completed 0.1.2 export is `Builds/WebGL-Beta-0.1.2`; preserve it and choose a new folder for the next release. Rebuild after changes to runtime settings or the template; retain the full final export together. Test through localhost, not by double-clicking `index.html`.

For a local phone test, serve the export from the project root with `python3 -m http.server 8000 --bind 0.0.0.0 --directory Builds/WebGL` (use the chosen export directory). Connect the phone to the same Wi-Fi and open `http://<computer-LAN-IP>:8000/` in its browser.

**Mobile controls verification, November 16, 2026.** At source commit `b6e1780`, Unity Play Mode checks opened all eight build scenes, verified Auto Detect in the saved scene and exercised Always Show and Never Show. Simulated touch input covered all six active forms, Box resize cancellation and a fresh press, simultaneous Fan movement and Blow at the wall, steering after a canvas-edge release, and recovery after checkpoint reload. The local WebGL build was also checked in desktop Chrome and touch emulation. The developer tested the mobile branch on a phone. A recorded end-to-end run on both physical iOS Safari and Android Chrome is still outstanding.

Check all four menu entries: Kindergarten, Level 1, Elementary School and Level 2. Play Navigation → IntroBox → IntroBubble → IntroBouncyBall → Level01 → IntroSpider → IntroFan → Level02. Verify the new first level is loaded both directly and from IntroBouncyBall. Repeat the following checks for the new export; previous general gameplay acceptance does not enumerate every device/scenario:

- Level01: test blue-to-yellow checkpoint stars in order; death/Return to Checkpoint restores Box at ADV_CP1, BouncyBall at ADV_CP2 and Star at ADV_CP3. Earlier points must not overwrite later ones. Ball possession alone is not a save.
- Test completed route selection, raised upper platform and broken RUIN cover surviving retry; loose bodies/lower door reset. Full restart clears everything. Navigation re-entry starts fresh.
- Test Star and Box upper-landing routes, Box growth/shrink, the one-way lift, R2 transfer, and two separate jumps for the protected RUIN button. Preserve Box-to-Bubble escape and fresh-contact reacquisition after separation.
- Test initial SAVE warning as Star and Box; SAFE/NORMAL/HARD changes; horizontal exit before BACK TO NORMAL; no repeat toggles during vertical bouncing. SAFE boards must remain black/fixed-length, move in opposite phase and stop together for anti-crush protection; left-board low/high passages must be traversable. The preview demonstrates the full alternating cycle.
- Test HARD spike reveal/hiding without stray visible edges. Its falling tooth triggers only on controlled upper-route approach, stays landed/lethal and rearms on retry. Recheck existing projectiles in Level02.
- Open Home: tooltip Home · Restart, Continue Playing, disabled checkpoint action before a save, and confirmation/cancel for Restart Level/Main Menu. Test R separately, including death-pause cancellation; no stuck time scale, duplicate modal or held input after resuming.
- On a phone, drag from different points on the left while pressing a right-hand button with a second finger. Test Star Jump, held Box Grow/Shrink, underwater diagonal steering and Bubble Pop. Match the right-hand icons to the map lessons. [Tutorial touch lessons](intro-levels.md#touch-lessons-by-scene)
- IntroBox: verify opening A/D, Space directly above the step and mandatory first possession; then carry C on B, park it on the ledge, shrink to separate and regrow to possess C. Walk left after docking and return: guides/text stay at the station and cargo progress does not reset. Test local dropped-cargo retry, larger black background hints and gold wall text; top-center form messages remain disabled.
- In IntroBubble, verify the ceiling-guided air ascent, water inlet without an air gap, underwater divider route and right exit shaft. Confirm the air, underwater and Pop lessons appear in their intended regions with bindings for the selected input mode; the top-center messages and old cards stay hidden. Verify J grows and K shrinks Box, and that IntroBox/IntroBubble hints match.
- Open/cancel R without reloading, confirm Restart Level returns to the current scene entrance, and test Return to Checkpoint when a checkpoint is available. The old standalone buttons stay disabled and shared Home returns to Navigation. A full restart of IntroBubble starts in EntryBox; walking through its left exit instead returns to IntroBox's endpoint as BoxC with the door open.
- IntroBox's arrow appears only after the flag opens the door; walking right loads IntroBubble without a Level up overlay. Also test direct IntroBubble entry → left exit → IntroBox endpoint → forward return to IntroBubble, then confirm a full restart consumes no old return setup.
- IntroBubble: verify “Bubbles float up automatically in air.” and spaced A/D keycaps. Carry a small and a larger Box across the IntroBox exit and compare width, floor height and camera continuity. Fall into the tank as Star, jump into the backup and repeat at least two pop/recovery cycles; exactly one dormant replacement must return to its original spawn each time. The original Bubble does not regenerate. Known follow-ups: the Pop area can extend over the right water shaft, and popping before reaching the tank can still require Restart; do not mark either resolved without a change and retest.
- In Level01, test the first tank on both input modes. After 1.5 seconds of ceiling contact the R reminder appears on desktop and stays hidden on touch. Tap the house for checkpoint or restart options. Verify both Bubble exits with the persistent Pop action on mobile. Test READY? → LET’S GO! and its bounce feedback.
- IntroBouncyBall: reach the flag as controlled Star/BouncyBall, watch graduation, then wait without input and confirm no automatic load. Test Next Level → Level01 and the still-accessible house → Navigation in separate runs, with keyboard and pointer/touch input. Both exits load once and restore time/control. Ordinary Home still opens Paused with its existing confirmations. Level01 retains Entering Level 2 Intro → IntroSpider; also test IntroSpider → IntroFan → Level02 and their existing transition behavior.
- IntroBubble's grounded-Star flag opens a door; walking right loads IntroBouncyBall without a Level up overlay. Walk back through the ball tutorial's left opening as Star and as BouncyBall: return to IntroBubble as active Star near its flag with the door open. Confirm immediate re-entry works without a loading loop, and a full Bubble restart starts in EntryBox with the door closed.
- IntroBouncyBall: test possession, the automatic high-ledge climb (including holding toward the wall), short water float, land bounce recovery, safe low-ceiling passage and the single spike in the open area before the flag. Revisit each region to confirm its fixed hint and diagram reappear. Check the animated bounce-frequency comparison, graphical movement keys and spike-death reminder. Check that movement keycaps appear only after ball possession, the first text/arc clear the left wall, and shore contact still bounces on both banks despite side overlap with water. Verify the graduation choices at the flag.
- All three Kindergarten scenes use the woodland backdrop, soil/grass and blue water while preserving original form/hazard colors and collision edges. Check contrast, keycap drawing order and the square caption-free book beside Home. Level01 has not received this art pass.
- In Level02, leave the chase door, stop and reapply the stick. Possess the seesaw Fan, steer with wind off, then tap Blow twice to start and stop airflow. Tap Leave to return to Star. Check the gear-room wind icon and touch text. Drive the final Box to the edge, release, steer away and repeat after a checkpoint reload.
- Confirm the shared Level02 Home button fits and its menu has Continue, Return to Checkpoint, Restart Level and Main Menu. Check Return to Checkpoint is disabled without a save; test retry at CP01/CP02/CP03, automatic death recovery at each, and Restart/R after a checkpoint. Review the unassigned ShowWall callbacks described in [level-2.md](level-2.md).
- Reach the Level02 flag: the deleted brown wall (20) should remain absent. Show Level Complete / You are a super star now! and wait for Return to Main Menu, with gameplay paused. Verify one completion event and a fresh entrance on menu re-entry.
- Save all eight scenes with Show Mode set to Auto Detect. On Navigation, verify sign taps, joystick drags and the separate Jump button beside the Collection Book. In a gameplay scene, check desktop card visibility, phone action icons, interrupted drags, focus loss and two-finger input. Record the physical device and browser used.
- Record the browsers/devices and checkpoint scenarios actually exercised. Beta 0.1.0 local/online gameplay acceptance and Level01 dashboard records are now recorded, but a detailed device/checkpoint matrix, Level02-specific delivery and raw-row metric verification remain open. See [AnalyticsTestPlan.md](AnalyticsTestPlan.md).
- Level01 movable-door hint: wait 20 seconds without door movement, activate the Hint button by Box growth or BouncyBall contact, verify “A little lift?” and the paused preview, then confirm time/movement resumes. It must show only once per scene load and must not auto-activate from spawn overlap.
- Verify analytics for all seven gameplay scenes, including completion, backward passage returns, restart/checkpoint actions and matching map versions. Navigation must remain untracked.

Run the additional [beta-0.1.2 checks](Beta-0.1.2.md#unity-and-browser-acceptance-before-recording), especially session-only Closet/book discovery, Help migration, Level01 playtest fixes and latest Spider guidance. Publish only after local tests pass.

## 3. Prepare the publishing checkout

First time only, run this from inside `StarFormer`:

```bash
git clone --branch gh-pages --single-branch https://github.com/XtraLuck21/StarFormer.git ../StarFormer-Pages
```

For every release:

```bash
cd ../StarFormer-Pages
git status
```

Confirm `gh-pages` and a clean working tree, then:

```bash
git pull --ff-only origin gh-pages
release_build_dir='../StarFormer/Builds/WebGL-Beta-0.1.2'
ls -a "$release_build_dir"
```

Use the tested beta-0.1.2 output, not an earlier export with a manually edited version label. Confirm it contains `index.html`, `Build/`, `TemplateData/`, and only exported runtime files.

## 4. Publish the same build to root and Beta

For an explicitly authorized root-and-Beta release, synchronize only the root runtime paths, then the complete `beta/` directory. The 0.1.2 export contains `index.html`, `Build/` and `TemplateData/`; there is no `StreamingAssets/` folder. Review any additional runtime paths before publishing a future export.

Preview from the `StarFormer-Pages` checkout:

```bash
(
  set -eu
  test -f "$release_build_dir/index.html"
  test -d "$release_build_dir/Build"
  test -d "$release_build_dir/TemplateData"
  rsync -avn --delete "$release_build_dir/Build/" Build/
  rsync -avn --delete "$release_build_dir/TemplateData/" TemplateData/
  rsync -avn "$release_build_dir/index.html" index.html
  rsync -avn --delete "$release_build_dir/" beta/
)
```

After reviewing the preview, run the same scoped copies:

```bash
(
  set -eu
  test -f "$release_build_dir/index.html"
  test -d "$release_build_dir/Build"
  test -d "$release_build_dir/TemplateData"
  rsync -av --delete "$release_build_dir/Build/" Build/
  rsync -av --delete "$release_build_dir/TemplateData/" TemplateData/
  rsync -av "$release_build_dir/index.html" index.html
  rsync -av --delete "$release_build_dir/" beta/
  cmp "$release_build_dir/index.html" index.html
  diff -qr "$release_build_dir/Build" Build
  diff -qr "$release_build_dir/TemplateData" TemplateData
  diff -qr "$release_build_dir" beta
  touch .nojekyll
)
```

**Never synchronize with deletion against the publishing root.** Deletion is limited to the intended runtime subdirectories and `beta/`. Preserve `.git`, Alpha, playtest and other historical directories. If a future build uses `StreamingAssets/`, review and synchronize that runtime directory separately at root and include it in the Beta copy; do not leave stale optional assets without review.

## 5. Review, commit and publish

```bash
git status --short
git diff --stat
git add -A -- index.html Build TemplateData beta .nojekyll
git diff --cached --name-status
git diff --cached --stat
```

Only the authorized root runtime paths, `beta/` and, if necessary, `.nojekyll` should change. Historical folders must remain unchanged. Stage any additional reviewed runtime directory explicitly. If unexpected files are staged, correct staging before committing.

The actual 0.1.2 publishing commit was `64b35c6` with this title:

```text
Publish StarFormer Beta 0.1.2 to root and beta pages
```

For a future release, use its version in the commit title, then push `gh-pages`. Do not force-push if another publisher updated the branch. Pages remains **Settings → Pages → Deploy from a branch → gh-pages → / (root)**. One deployment serves both URLs.

## 6. Verify and record

Wait for **GitHub Actions** deployment success. Open both `/StarFormer/` and `/StarFormer/beta/` in a private window on desktop and on a real phone. On desktop check the fullscreen icon and Esc. On a phone check single-tap launch, viewport resizing on rotation, and simultaneous movement and action inputs. Test navigation, changed behavior, and restart/return paths; also confirm the preserved Alpha and historical playtest folders remain unchanged. Compare all hosted runtime files at both new destinations with the exact local export. Deployment success alone does not confirm gameplay correctness.

The 0.1.2 release uses both the root and `/beta/`; keep `/alpha/` unchanged and agree on the destination scope for future releases. Record the release name/date, source commit, publishing commit (`git rev-parse --short HEAD`), URL, Player Version, Analytics build version, and local/online test results. Verify new Sheet rows carry `beta-0.1.2`. Browser sessions already open may continue reporting the old version until reloaded. Return to the source checkout:

```bash
cd ../StarFormer
git status
```

Update release documentation on the working documentation/feature branch, staging document paths explicitly, then have the maintainer review and integrate it into main. Creation of PRs, commits, pushes and merges remains a manual maintainer step; editing these docs does not perform any of those operations. Unity imports/builds can modify tracked rendering settings or input preload data, and file permissions may also change. Review those separately with `git diff` and `git diff --summary`; a successful documentation or website push does not commit these local changes. Do not discard them automatically or hide tracked settings in `.gitignore`.

Source changes do not affect the live game until another build is published. For release history and troubleshooting, see [ScriptGuide.md](ScriptGuide.md#4-web-build-and-github-pages-release).
