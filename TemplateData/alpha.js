(() => {
  "use strict";

  const player = document.getElementById("player");
  const viewport = document.getElementById("viewport");
  const frame = document.getElementById("game-frame");
  const canvas = document.getElementById("unity-canvas");
  canvas.addEventListener("contextmenu", event => event.preventDefault());

  // Unity receives TouchEvents on the canvas. A drag can continue into the page's
  // letterbox while its finger is still down, even when no UI release follows.
  const canvasTouches = new Map();
  function cancelCanvasTouches(ended, remaining) {
    if (!ended.length) return;
    const cancel = new Event("touchcancel", { bubbles: true, cancelable: true });
    Object.assign(cancel, {
      changedTouches: ended,
      touches: remaining,
      targetTouches: remaining.filter(touch => touch.target === canvas)
    });
    canvas.dispatchEvent(cancel);
  }

  function trackCanvasTouches(event) {
    if (!event.isTrusted) return;
    const live = Array.from(event.touches);
    const bounds = canvas.getBoundingClientRect();
    const ended = [];
    for (const touch of event.changedTouches) {
      if (event.type === "touchstart" && touch.target === canvas) {
        canvasTouches.set(touch.identifier, touch);
      } else if (canvasTouches.has(touch.identifier)) {
        if (event.type === "touchmove") canvasTouches.set(touch.identifier, touch);
        else {
          canvasTouches.delete(touch.identifier);
          if (event.target !== canvas) ended.push(touch);
        }
      }
    }
    for (const [id, touch] of canvasTouches) {
      if (!live.some(finger => finger.identifier === id) ||
          touch.clientX <= bounds.left || touch.clientX >= bounds.right ||
          touch.clientY <= bounds.top || touch.clientY >= bounds.bottom) {
        canvasTouches.delete(id);
        ended.push(touch);
      }
    }
    if (ended.length) cancelCanvasTouches(ended, live.filter(touch =>
      !ended.some(finger => finger.identifier === touch.identifier)));
  }

  for (const type of ["touchstart", "touchmove", "touchend", "touchcancel"])
    window.addEventListener(type, trackCanvasTouches, { capture: true, passive: true });

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
    (navigator.platform === "MacIntel" && navigator.maxTouchPoints > 1) ||
    (navigator.maxTouchPoints > 0 && matchMedia("(pointer: coarse)").matches);
  const requestFullscreen = player.requestFullscreen || player.webkitRequestFullscreen;
  const canFullscreen = Boolean(requestFullscreen) &&
    document.fullscreenEnabled !== false && document.webkitFullscreenEnabled !== false;
  let started = false;
  let failed = false;
  let noticeTimer;
  let cornerRequestPending = false;
  let shownExitHint = false;

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
    const landscapeTouch = touchDevice && width >= height;
    const cssWidth = Math.max(1, landscapeTouch ? width : Math.min(width, height * 16 / 9));
    const cssHeight = landscapeTouch ? Math.max(1, height) : cssWidth * 9 / 16;
    frame.style.width = `${cssWidth}px`;
    frame.style.height = `${cssHeight}px`;

    // Preserve composition and bound GPU cost on high-density screens.
    const density = Math.min(window.devicePixelRatio || 1, touchDevice ? 1.5 : 2);
    const maxWidth = touchDevice ? 1920 : 2560;
    const renderWidth = Math.max(16, Math.round(Math.min(cssWidth * density, maxWidth) / 16) * 16);
    const renderHeight = Math.max(1, Math.round(renderWidth * cssHeight / cssWidth));
    if (canvas.width !== renderWidth) canvas.width = renderWidth;
    if (canvas.height !== renderHeight) canvas.height = renderHeight;
    rotateHint.hidden = !started || !touchDevice || width >= height;
  }

  function isFullscreen() {
    return Boolean(document.fullscreenElement || document.webkitFullscreenElement);
  }

  // Capture before the Unity loader registers keyboard listeners. Some browsers
  // handle Escape themselves; others deliver keydown before leaving fullscreen.
  window.addEventListener("keydown", event => {
    if (event.key !== "Escape") return;
    if (isFullscreen()) {
      event.stopImmediatePropagation();
      if (!event.repeat) {
        const exit = document.exitFullscreen || document.webkitExitFullscreen;
        if (exit) Promise.resolve(exit.call(document)).catch(() => {});
      }
    } else if (event.repeat) {
      // A held exit key must not become a fresh menu press in the smaller window.
      event.stopImmediatePropagation();
    }
  }, true);

  function syncFullscreen() {
    const active = isFullscreen();
    fullscreen.hidden = !started || failed || touchDevice || !canFullscreen || active;
    if (active && cornerRequestPending) {
      window.starFormerCornerFullscreenEntries = (window.starFormerCornerFullscreenEntries || 0) + 1;
      cornerRequestPending = false;
    }
    if (active && started && !touchDevice && !shownExitHint) {
      shownExitHint = true;
      showNotice("Press Esc to exit fullscreen.");
    } else if (!active && notice.textContent === "Press Esc to exit fullscreen.") {
      notice.hidden = true;
    }
    resize();
    if (started) canvas.focus();
  }

  function fullscreenFailed() {
    cornerRequestPending = false;
    showNotice("Fullscreen is unavailable. You can keep playing in this window.");
    if (started) canvas.focus();
  }

  function enterFullscreen() {
    if (!canFullscreen) return;
    try {
      Promise.resolve(requestFullscreen.call(player)).catch(fullscreenFailed);
    } catch (_) {
      fullscreenFailed();
    }
  }

  function start(wantsFullscreen) {
    if (failed || playWindowed.disabled) return;
    if (wantsFullscreen) enterFullscreen();
    started = true;
    launch.hidden = true;
    syncFullscreen();
  }

  function fail(message) {
    failed = true;
    fullscreen.hidden = true;
    console.error(message);
    launch.hidden = false;
    status.textContent = "The game could not load. Please refresh the page to try again.";
    progress.hidden = true;
    playFullscreen.disabled = true;
    playWindowed.disabled = true;
  }

  playFullscreen.hidden = touchDevice || !canFullscreen;
  if (touchDevice || !canFullscreen) playWindowed.textContent = "Play";
  playFullscreen.addEventListener("click", () => start(true));
  playWindowed.addEventListener("click", () => start(touchDevice && canFullscreen));
  fullscreen.addEventListener("click", () => {
    if (fullscreen.hidden || cornerRequestPending) return;
    cornerRequestPending = true;
    enterFullscreen();
  });
  document.addEventListener("fullscreenerror", fullscreenFailed);
  document.addEventListener("webkitfullscreenerror", fullscreenFailed);
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
