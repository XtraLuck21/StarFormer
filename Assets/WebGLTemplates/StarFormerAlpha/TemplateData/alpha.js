(() => {
  "use strict";

  const player = document.getElementById("player");
  const viewport = document.getElementById("viewport");
  const frame = document.getElementById("game-frame");
  const canvas = document.getElementById("unity-canvas");
  const launch = document.getElementById("launch");
  const progress = document.getElementById("progress");
  const status = document.getElementById("load-status");
  const fullscreen = document.getElementById("fullscreen");
  const playFullscreen = document.getElementById("play-fullscreen");
  const playWindowed = document.getElementById("play-windowed");
  const rotateHint = document.getElementById("rotate-hint");
  const notice = document.getElementById("notice");
  const build = window.starFormerBuild;
  const touchDevice = /Android|iPhone|iPad|iPod/i.test(navigator.userAgent) ||
    (navigator.maxTouchPoints > 0 && matchMedia("(pointer: coarse)").matches);
  const requestFullscreen = player.requestFullscreen || player.webkitRequestFullscreen;
  const canFullscreen = Boolean(requestFullscreen) &&
    document.fullscreenEnabled !== false && document.webkitFullscreenEnabled !== false;
  let started = false;
  let failed = false;
  let noticeTimer;

  function showNotice(message) {
    clearTimeout(noticeTimer);
    notice.textContent = message;
    notice.hidden = false;
    noticeTimer = setTimeout(() => { notice.hidden = true; }, 6000);
  }

  function resize() {
    const style = getComputedStyle(viewport);
    const width = viewport.clientWidth - parseFloat(style.paddingLeft) - parseFloat(style.paddingRight);
    const height = viewport.clientHeight - parseFloat(style.paddingTop) - parseFloat(style.paddingBottom);
    const cssWidth = Math.max(1, Math.min(width, height * 16 / 9));
    const cssHeight = cssWidth * 9 / 16;
    frame.style.width = `${cssWidth}px`;
    frame.style.height = `${cssHeight}px`;

    // Preserve composition and bound GPU cost on high-density screens.
    const density = Math.min(window.devicePixelRatio || 1, touchDevice ? 1.5 : 2);
    const maxWidth = touchDevice ? 1920 : 2560;
    const renderWidth = Math.max(16, Math.round(Math.min(cssWidth * density, maxWidth) / 16) * 16);
    const renderHeight = renderWidth * 9 / 16;
    if (canvas.width !== renderWidth) canvas.width = renderWidth;
    if (canvas.height !== renderHeight) canvas.height = renderHeight;
    rotateHint.hidden = !started || !touchDevice || width >= height;
  }

  function isFullscreen() {
    return Boolean(document.fullscreenElement || document.webkitFullscreenElement);
  }

  function syncFullscreen() {
    fullscreen.textContent = isFullscreen() ? "Exit Fullscreen" : "Fullscreen";
    fullscreen.setAttribute("aria-pressed", String(isFullscreen()));
    resize();
  }

  function enterFullscreen() {
    if (!canFullscreen) return;
    try {
      // Request immediately inside the user's click, before any asynchronous work.
      Promise.resolve(requestFullscreen.call(player)).catch(() => {
        showNotice("Fullscreen is unavailable. You can keep playing in this window.");
      });
    } catch (_) {
      showNotice("Fullscreen is unavailable. You can keep playing in this window.");
    }
  }

  function start(wantsFullscreen) {
    if (failed || playWindowed.disabled) return;
    if (wantsFullscreen) enterFullscreen();
    started = true;
    launch.hidden = true;
    fullscreen.hidden = !canFullscreen;
    resize();
    canvas.focus();
  }

  function fail(message) {
    failed = true;
    console.error(message);
    launch.hidden = false;
    status.textContent = "The game could not load. Please refresh the page to try again.";
    progress.hidden = true;
    playFullscreen.disabled = true;
    playWindowed.disabled = true;
  }

  playFullscreen.hidden = !canFullscreen;
  if (!canFullscreen) playWindowed.textContent = "Play";
  playFullscreen.addEventListener("click", () => start(true));
  playWindowed.addEventListener("click", () => start(false));
  fullscreen.addEventListener("click", () => {
    if (isFullscreen()) {
      const exit = document.exitFullscreen || document.webkitExitFullscreen;
      try {
        Promise.resolve(exit.call(document)).catch(() => showNotice("Use your browser's fullscreen control to exit."));
      } catch (_) {
        showNotice("Use your browser's fullscreen control to exit.");
      }
    } else {
      enterFullscreen();
    }
    canvas.focus();
  });
  document.addEventListener("fullscreenchange", syncFullscreen);
  document.addEventListener("webkitfullscreenchange", syncFullscreen);
  window.addEventListener("resize", resize);
  window.visualViewport?.addEventListener("resize", resize);
  new ResizeObserver(resize).observe(viewport);
  syncFullscreen();

  build.config.showBanner = (message, type) => {
    if (type === "error") fail(message);
    else {
      console.warn(message);
      showNotice("The browser reported a loading warning. If loading stops, refresh the page.");
    }
  };
  const loader = document.createElement("script");
  loader.src = build.loaderUrl;
  loader.onerror = () => fail("Unity loader download failed.");
  loader.onload = () => {
    Promise.resolve().then(() => createUnityInstance(canvas, build.config, value => {
      progress.value = value;
      if (!failed) status.textContent = `Loading… ${Math.round(value * 100)}%`;
    })).then(() => {
      if (failed) return;
      status.textContent = "Ready for a new adventure?";
      progress.hidden = true;
      playFullscreen.disabled = false;
      playWindowed.disabled = false;
      resize();
    }).catch(fail);
  };
  document.body.appendChild(loader);
})();
