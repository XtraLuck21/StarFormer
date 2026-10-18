# Publish StarFormer to GitHub Pages

Last updated: 2026-10-18.

- [Latest tested build](https://xtraluck21.github.io/StarFormer/) — updated for each tested release.
- [Alpha Progress Check](https://xtraluck21.github.io/StarFormer/alpha-progress-check/) — frozen All-Level V2 submission, preserved by `5a7f043` on October 12, 2026.

Current main includes the new intro sequence from PR #10, the October 17 hint/presentation polish from PR #11 (`989d2d1`) and the tested October 18 Level 1 update from PR #12 (`e457e87`). No new WebGL publication is recorded for these updates. The existing Alpha Progress Check build keeps its original Tutorial and controls. Analytics builds must configure and validate the test Google Apps Script endpoint described in `AnalyticsImplementation.md` before publication.

Use `StarFormer` on `main` as the integrated source baseline for a release, and the sibling folder `StarFormer-Pages` on `gh-pages` for exported website files. Do not merge `gh-pages` into `main`. Coordinate one publisher per release.

Each submission needs its own complete build and permanent URL. Never overwrite `alpha-progress-check/` or another submitted folder. Future milestones can use `alpha/`, `beta/`, or a new version-specific name. These are paths within one site, not separate domains or deployments.

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

Use **Default** Web Template, **Gzip** compression, **Decompression Fallback on**, **Development Build off**, and **Threads Support off**.

Choose **Build And Run** with a new output folder, such as `Builds/WebGL-All-Level-V3`. Test through localhost, not by double-clicking `index.html`. After integrating the October 18 Level 1 update, also check Box-to-Bubble escape and restored contact after separation, ball checkpoint death recovery versus R full restart, and U2 regeneration after E/hazard pops. Check the Introductory Levels, Level 1 and Level 2 buttons, and play the complete Mainmenu → IntroBox → IntroBubble → Level01 → Level02 route. Verify dormant bubbles activate on possession, E pops bubbles while Space does not, hazard-triggered popping still works, hint visibility and drawing order are correct, and goals/reset/return navigation work. Review existing levels for the shared bubble changes. The editor flow and October 17 intro polish have passed developer-reported testing; a new localhost/browser test is still required for the exported build. Include these checks:

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
- The October 18 hint/layout and final-goal/menu checks passed in Editor according to the developer; current checkpoint-matrix coverage, a fresh WebGL/phone run and analytics delivery verification remain pending. See [AnalyticsTestPlan.md](AnalyticsTestPlan.md).

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
release_build_dir='../StarFormer/Builds/WebGL-All-Level-V3'
ls -a "$release_build_dir"
```

Replace the example build name with the tested output. Confirm it contains `index.html`, `Build/`, `TemplateData/`, and only exported runtime files.

## 4. Preserve a submission and update latest

### A. New submission only: create a permanent version folder

Skip this part for a routine latest-only update. Choose an unused folder name; `alpha` below is an example for the next milestone, not the existing Alpha Progress Check:

```bash
release_path='alpha'
(
  set -eu
  test -f "$release_build_dir/index.html"
  test -d "$release_build_dir/Build"
  test -d "$release_build_dir/TemplateData"
  mkdir "$release_path"
  cp -R "$release_build_dir/." "$release_path/"
  diff -qr "$release_build_dir" "$release_path"
)
```

`mkdir` deliberately fails if the destination exists. Do not bypass that check to replace an old submission. A partial copy must be reviewed before retrying. Each folder must own all its runtime files, including optional `StreamingAssets/`; it must not load payloads from the mutable root. The current Unity template uses relative paths, so its generated `index.html` needs no manual edits.

### B. Update the root to the latest tested build

For the current export layout, replace only the root player and its two asset directories:

```bash
(
  set -eu
  test -f "$release_build_dir/index.html"
  test -d "$release_build_dir/Build"
  test -d "$release_build_dir/TemplateData"
  rsync -av --delete "$release_build_dir/Build/" ./Build/
  rsync -av --delete "$release_build_dir/TemplateData/" ./TemplateData/
  cp "$release_build_dir/index.html" ./index.html
  touch .nojekyll
)
```

Deletion is limited to `Build/` and `TemplateData/`. **Never synchronize with deletion against the entire publishing root**, because that would remove archived version folders. Preserve `.git`, `.nojekyll`, and every existing milestone directory.

If a future export includes other runtime files such as `StreamingAssets/`, copy them to the matching root paths as well. Review obsolete root runtime files individually. Do not publish an incomplete export or remove version folders during cleanup.

## 5. Review, commit and publish

```bash
git status --short
git diff --stat
git add -A
git diff --cached --name-status
git diff --cached --stat
```

Confirm that only the intended root export and any newly created version folder changed. There must be no changes or deletions inside `alpha-progress-check/` or other previously submitted folders. If unexpected files are staged, stop and correct the staging before committing.

```bash
git commit -m "deploy: publish All-Level V3 and preserve Alpha submission"
git push origin gh-pages
```

Adjust the message to match the release and omit the submission wording for a latest-only update. If another publisher has pushed meanwhile, coordinate before retrying; do not force-push.

Pages remains **Settings → Pages → Deploy from a branch → gh-pages → / (root)**. The Actions deployment card shows the base URL; it does not list each version path.

## 6. Verify and record

Wait for **GitHub Actions** deployment success. Open the root and any new version URL in a private window. Test navigation, changed behavior, and restart/return paths; also confirm the existing Alpha Progress Check still loads. Deployment success alone does not confirm gameplay correctness.

Use a fixed milestone URL in course submissions. The root URL is for general access to the latest version. Record the release name/date, source commit, publishing commit (`git rev-parse --short HEAD`), fixed URL, and local/online test results. Return to the source checkout:

```bash
cd ../StarFormer
git status
```

Update release documentation on the working documentation/feature branch, staging document paths explicitly, then have the maintainer review and integrate it into main. Creation of PRs, commits, pushes and merges remains a manual maintainer step; editing these docs does not perform any of those operations. Unity imports/builds can modify tracked rendering settings or input preload data, and file permissions may also change. Review those separately with `git diff` and `git diff --summary`; a successful documentation or website push does not commit these local changes. Do not discard them automatically or hide tracked settings in `.gitignore`.

Source changes do not affect the live game until another build is published. For release history and troubleshooting, see [ScriptGuide.md](ScriptGuide.md#4-web-build-and-github-pages-release).
