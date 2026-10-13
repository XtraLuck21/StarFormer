# Publish StarFormer to GitHub Pages

- [Latest tested build](https://xtraluck21.github.io/StarFormer/) — updated for each tested release.
- [Alpha Progress Check](https://xtraluck21.github.io/StarFormer/alpha-progress-check/) — frozen All-Level V2 submission, preserved by `5a7f043` on October 12, 2026.

Use `StarFormer` on `main` for Unity source and documentation, and the sibling folder `StarFormer-Pages` on `gh-pages` for exported website files. Do not merge `gh-pages` into `main`. Coordinate one publisher per release.

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
1  Tutorial
2  Level01
3  Level02
```

Use **Default** Web Template, **Gzip** compression, **Decompression Fallback on**, **Development Build off**, and **Threads Support off**.

Choose **Build And Run** with a new output folder, such as `Builds/WebGL-All-Level-V3`. Test through localhost, not by double-clicking `index.html`. Check all three level buttons, changed gameplay/hints, restart/checkpoints, and return navigation. Publish only after local tests pass.

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

Update and commit documentation on `main`, staging document paths explicitly. Unity imports/builds can modify tracked rendering settings or input preload data, and file permissions may also change. Review those separately with `git diff` and `git diff --summary`; a successful documentation or website push does not commit these local changes. Do not discard them automatically or hide tracked settings in `.gitignore`.

Source changes do not affect the live game until another build is published. For release history and troubleshooting, see [ScriptGuide.md](ScriptGuide.md#4-web-build-and-github-pages-release).
