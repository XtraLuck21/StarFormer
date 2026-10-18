# Publish StarFormer to GitHub Pages

Last updated: 2026-10-18.

- [Current Alpha](https://xtraluck21.github.io/StarFormer/alpha/) — fixed path for tested Alpha releases.
- [Legacy root build](https://xtraluck21.github.io/StarFormer/) — preserved during Alpha publication.
- [Alpha Progress Check](https://xtraluck21.github.io/StarFormer/alpha-progress-check/) — frozen All-Level V2 submission, preserved by `5a7f043` on October 12, 2026.

Alpha `alpha-0.1.2` is published at `/alpha/` on October 18, 2026. Source configuration `5650250` includes the Level 1 restart and intro-control changes from `9b35f8e`. Export: `Builds/WebGL-Alpha-0.1.2`; publishing commit: `5f62980`. [GitHub Pages deployment](https://github.com/XtraLuck21/StarFormer/actions/runs/39173067360) succeeded. The deployed HTML/version and game-data payload matched the tested export. The developer reported both localhost and online tests passed. Analytics Sheet delivery for this version is still awaiting confirmation.

The instructions below are the repeatable release procedure, using 0.1.2 as the completed example. For the next release, choose a new version and output folder, update both Player and Analytics versions, rebuild and retest before replacing `/alpha/`.

Use `StarFormer` as the source checkout and the sibling `StarFormer-Pages` checkout on `gh-pages` for exported files. Do not merge `gh-pages` into `main`. Coordinate one publisher per release.

The agreed Alpha URL is https://xtraluck21.github.io/StarFormer/alpha/. This is a mutable release path: later tested Alpha versions replace only `alpha/`, with a new build version each time. Preserve `playtest-01/`, `alpha-progress-check/`, and all other existing paths. The existing site root is not changed by an Alpha-only publication. GitHub Pages is publicly accessible even when the link is shared only with internal testers.

The source Analytics asset now uses `buildVersion: alpha-0.1.2`; the endpoint and `production` environment are unchanged. A new build appends to the existing production sheet, distinguished by `build_version`. Confirm a new event in the actual Sheet when validating data collection; this remains outstanding for Alpha 0.1.2 even though gameplay publication is complete. Do not redeploy the backend or clear old rows as part of publishing the game.

Commands use macOS/Linux shell syntax and assume the folders are siblings. You need repository push access and Unity **6000.3.22f1** with **Web Build Support**. Stop if a command fails.

## 1. Update source

Close Unity. In `StarFormer`, run `git status`. Review local changes and commit intended changes or stash them before continuing; do not discard unfamiliar changes. With a clean working tree:

```bash
git switch main
git pull --ff-only origin main
git rev-parse --short HEAD
```

Record the source commit and any intentional local settings used for the build.

## 2. Build and test locally

Open Unity, wait for imports and compilation, and test from `Mainmenu`. Resolve Console errors. In **File → Build Profiles → Web**, enable only:

```text
0  Mainmenu
1  IntroBox
2  IntroBubble
3  Level01
4  Level02
```

Keep the old Tutorial, reference scenes and sandboxes unchecked. Open **Scene List** to edit or drag the order; Mainmenu must remain the startup scene.

In **Player Settings → Resolution and Presentation**, choose the **StarFormerAlpha** Web template and set the default canvas size to **1920 × 1080**. In the Player settings set **Version** to `alpha-0.1.2`. Keep **Gzip**, **Decompression Fallback on**, **Development Build off**, and **Threads Support off**. Enable **Name Files As Hashes** to give changed build payloads distinct URLs when replacing `alpha/`.

The custom template lives at `Assets/WebGLTemplates/StarFormerAlpha/`. It keeps a 16:9 canvas inside the available safe area, with letterboxing rather than stretched gameplay. The render buffer follows viewport size and device pixel density, capped at 2× / 2560 × 1440 on desktop and 1.5× / 1920 × 1080 on touch/mobile devices. These are upper limits, not forced resolutions on smaller screens. It does not change camera behavior, textures, mobile AutoDetect, or scene UI wiring.

After loading, **Play Fullscreen** requests fullscreen from a user gesture; **Play in Window** starts without it. The fullscreen container includes the canvas and an upper-left toggle, so **Exit Fullscreen** remains accessible. Esc is left to the desktop browser. Unsupported browsers offer **Play**, and rejected fullscreen requests fall back to windowed play. Portrait phones receive a landscape hint. Fullscreen availability still depends on the actual browser.

The template uses relative asset paths and needs no edits after export. The Player Version is used to version its CSS/JS URLs; increment it together with Analytics `buildVersion` for later releases. Keep the complete exported payload together.

Choose **Build And Run** with a new output folder, such as `Builds/WebGL-Alpha-0.1.2`. Test through localhost, not by double-clicking `index.html`. After integrating the October 18 Level 1 update, also check Box-to-Bubble escape and restored contact after separation, ball checkpoint death recovery versus R full restart, and U2 regeneration after E/hazard pops. Check the Introductory Levels, Level 1 and Level 2 buttons, and play the complete Mainmenu → IntroBox → IntroBubble → Level01 → Level02 route. Verify dormant bubbles activate on possession, E pops bubbles while Space does not, hazard-triggered popping still works, hint visibility and drawing order are correct, and goals/reset/return navigation work. Review existing levels for the shared bubble changes. Alpha 0.1.2 localhost and online tests passed according to the developer. Repeat the following regression checks for subsequent exports; the general acceptance report does not enumerate every scenario:

- For 0.1.2, verify the Level01 Restart Level button reloads the level from its entrance after reaching a checkpoint. Confirm Return to Main Menu still works.
- In IntroBox, verify the lowered Left/Right/Jump controls on a real phone. In IntroBubble, verify the compact 120 × 120 controls retain their directional arrangement and 35% background opacity, with readable labels and the original camera framing. Check Box growth/shrink, Bubble swimming in all four directions, Pop and Star jump, including simultaneous touches.
- IntroBox starts with contextual A/D and SPACE prompts; BoxA possession shows the contact explanation, and BoxA → BoxB shows New box. Same little star. Subtitles remain readable at the intended browser sizes.
- IntroBubble explains automatic rise in air, four-direction movement underwater and renewed rise after leaving the water; E exits to Star. Old circle/up-arrow and follow-the-flag text should not appear.
- Restart (R) and R both restart the current scene; IntroBubble returns to EntryBox rather than IntroBox. Return to Main Menu still works during ordinary gameplay.
- IntroBox's arrow appears only after the flag opens the door; walking right loads IntroBubble without a Level up overlay.
- IntroBubble and Level01 show Level up! / Entering Level 1... or Level 2... for about 1.5 seconds, load the correct scene exactly once, and restore movement/time afterward. The outgoing gameplay and pointer interaction with its UI should pause during the overlay.
- The two intros retain the updated gray/dark palette, visible Bubble outline and correct hint/body drawing order.
- In Level02, E / Leave releases Spider and Fan, W / Blow toggles Fan wind, and Space / Fire still fires the possessable spike wall. Confirm the single-line upper-center Spider hint and two-line Fan hint are readable.
- Confirm all three Level02 top-right buttons fit: Return to Main Menu, Restart Level and Retry Checkpoint. Test Retry with no checkpoint and at CP01/CP02/CP03, automatic death recovery at each, and Restart/R after a checkpoint. Review the unassigned ShowWall callbacks described in [level-2.md](level-2.md).
- Reach the Level02 flag: the deleted brown wall (20) should remain absent. Show Level Complete / You are a super star now! and wait for Return to Main Menu, with gameplay paused. Verify one completion event and a fresh entrance on menu re-entry.
- Keep MobileControls on AutoDetect for the release. Test desktop hiding, phone visibility, simultaneous movement/action touches, every form, and release of held input after hiding/focus loss. The mobile Canvas is screen-fixed Overlay UI; check different browser sizes and phone aspect ratios.
- Record the browsers/devices and checkpoint scenarios actually exercised. Alpha 0.1.2 gameplay acceptance is recorded, but a detailed device/checkpoint matrix and current-version Sheet verification are not. See [AnalyticsTestPlan.md](AnalyticsTestPlan.md).

Publish only after local tests pass.

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
release_build_dir='../StarFormer/Builds/WebGL-Alpha-0.1.2'
ls -a "$release_build_dir"
```

Replace the example build name with the tested output. Confirm it contains `index.html`, `Build/`, `TemplateData/`, and only exported runtime files.

## 4. Publish or replace only Alpha

After the new export passes local testing, preview synchronization into the fixed `alpha/` folder from the publishing checkout:

```bash
release_path='alpha'
(
  set -eu
  test -f "$release_build_dir/index.html"
  test -d "$release_build_dir/Build"
  test -d "$release_build_dir/TemplateData"
  mkdir -p "$release_path"
  rsync -avn --delete "$release_build_dir/" "$release_path/"
)
```

Review the preview, then repeat the checked operation without dry-run:

```bash
(
  set -eu
  test "$release_path" = 'alpha'
  test -f "$release_build_dir/index.html"
  test -d "$release_build_dir/Build"
  test -d "$release_build_dir/TemplateData"
  rsync -av --delete "$release_build_dir/" "$release_path/"
  diff -qr "$release_build_dir" "$release_path"
  touch .nojekyll
)
```

Deletion is confined to `alpha/` and removes obsolete payloads there. **Never synchronize with deletion against the publishing root.** Preserve `playtest-01/`, `alpha-progress-check/`, the root player, `.git`, and all other version directories. Include optional `StreamingAssets/` in the complete copy. No files are shared with older builds.

## 5. Review, commit and publish

```bash
git status --short
git diff --stat
git add -A -- alpha .nojekyll
git diff --cached --name-status
git diff --cached --stat
```

Confirm that only `alpha/` and, if needed, `.nojekyll` changed. There must be no changes or deletions inside `playtest-01/`, `alpha-progress-check/`, the root player, or other preserved folders. If unexpected files are staged, stop and correct the staging before committing.

```bash
git commit -m "Publish StarFormer Alpha 0.1.2"
git push origin gh-pages
```

Adjust the message for subsequent Alpha versions. If another publisher has pushed meanwhile, coordinate before retrying; do not force-push.

Pages remains **Settings → Pages → Deploy from a branch → gh-pages → / (root)**. The Actions deployment card shows the base URL; it does not list each version path.

## 6. Verify and record

Wait for **GitHub Actions** deployment success. Open `/StarFormer/alpha/` in a private window on desktop and on a real phone. Check loading, fullscreen entry/exit, resize, orientation changes, crisp text, and every form with simultaneous touch inputs. Test navigation, changed behavior, and restart/return paths; also confirm the old `playtest-01/` and Alpha Progress Check still load unchanged. Deployment success alone does not confirm gameplay correctness.

Use `/alpha/` for this Alpha release and subsequent tested Alpha fixes. Record the release name/date, source commit, publishing commit (`git rev-parse --short HEAD`), URL, Player Version, Analytics build version, and local/online test results. Verify new Sheet rows carry `alpha-0.1.2`. Browser sessions already open may continue reporting the old version until reloaded. Return to the source checkout:

```bash
cd ../StarFormer
git status
```

Update release documentation on the working documentation/feature branch, staging document paths explicitly, then have the maintainer review and integrate it into main. Creation of PRs, commits, pushes and merges remains a manual maintainer step; editing these docs does not perform any of those operations. Unity imports/builds can modify tracked rendering settings or input preload data, and file permissions may also change. Review those separately with `git diff` and `git diff --summary`; a successful documentation or website push does not commit these local changes. Do not discard them automatically or hide tracked settings in `.gitignore`.

Source changes do not affect the live game until another build is published. For release history and troubleshooting, see [ScriptGuide.md](ScriptGuide.md#4-web-build-and-github-pages-release).
