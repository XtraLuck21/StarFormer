# Publish StarFormer to GitHub Pages

Live game: https://xtraluck21.github.io/StarFormer/

Use `StarFormer` on `main` for Unity source and documentation. Use the sibling folder `StarFormer-Pages` on `gh-pages` for exported website files. Do not merge `gh-pages` into `main`. Coordinate one publisher per release.

Commands below use macOS/Linux shell syntax and assume these folders are siblings. You need repository push access and Unity **6000.3.22f1** with **Web Build Support**.

## 1. Update source

Close Unity. In `StarFormer`, check for local changes:

```bash
git status
```

Commit intended source changes or stash them before continuing. Do not discard unfamiliar changes. With a clean working tree:

```bash
git switch main
git pull --ff-only origin main
git rev-parse --short HEAD
```

Record the source commit. If pulling fails, resolve the problem before building.

## 2. Build and test locally

Open Unity, wait for imports and compilation, and test the changes from `Mainmenu`. Resolve Console errors before building.

In **File → Build Profiles**, select **Web** and enable only:

```text
0  Mainmenu
1  Tutorial
2  Level01
3  Level02
```

Use **Default** Web Template, **Gzip** compression, **Decompression Fallback on**, **Development Build off**, and **Threads Support off**.

Choose **Build And Run** and create a new folder under `Builds`, such as `Builds/WebGL-All-Level-V3`. Test through localhost, not by double-clicking `index.html`. Check all three level buttons, the changed gameplay or hints, restart/checkpoint behavior, and return-to-menu navigation. Publish only after the local test passes.

## 3. Prepare the publishing checkout

First time only, run this from inside `StarFormer`:

```bash
git clone --branch gh-pages --single-branch https://github.com/XtraLuck21/StarFormer.git ../StarFormer-Pages
```

For every release, enter `StarFormer-Pages` and check its state:

```bash
cd ../StarFormer-Pages
git status
```

Confirm the branch is `gh-pages` and the working tree is clean, then:

```bash
git pull --ff-only origin gh-pages
```

## 4. Replace the exported files

Stay in `StarFormer-Pages`. Replace the example folder name below with your tested build folder:

```bash
release_build_dir='../StarFormer/Builds/WebGL-All-Level-V3'
ls "$release_build_dir/index.html" "$release_build_dir/Build"
```

If either path is missing, stop and correct the path. Preview the synchronization first:

```bash
rsync -avni --delete --exclude='.git/' --exclude='.nojekyll' "$release_build_dir/" ./
```

Confirm that it replaces website files only. Then copy:

```bash
rsync -av --delete --exclude='.git/' --exclude='.nojekyll' "$release_build_dir/" ./
touch .nojekyll
```

This removes obsolete build files and preserves Git history. Copy the build folder's **contents**, so `index.html`, `Build/` and `TemplateData/` are directly in the publishing root. Include any other generated runtime files, but no Unity source or cache folders.

## 5. Commit and publish

```bash
git add -A
git diff --cached --stat
git commit -m "deploy: publish All-Level V3"
git push origin gh-pages
```

Adjust the commit message for your release. If a teammate has pushed another release and your push is rejected, stop and coordinate which build should be published. Do not force-push.

The repository already uses **Settings → Pages → Deploy from a branch → gh-pages → / (root)**. No setting change is needed for routine updates.

## 6. Verify and record

Wait for the Pages deployment in **GitHub Actions** to succeed. Open the live game in a private window or refresh without cache. Test navigation, the changed behavior, and restart/return paths again. Deployment success alone does not confirm gameplay correctness.

Record the release name, date, source commit, publishing commit (`git rev-parse --short HEAD`), and local/online test results. Return to the source folder for documentation work:

```bash
cd ../StarFormer
git status
```

Commit documentation on `main`, separately from generated build-setting changes. Updating `main` does not update the live game until another build is published.

For previous release records and troubleshooting, see [ScriptGuide.md](ScriptGuide.md#4-web-build-and-github-pages-release).
