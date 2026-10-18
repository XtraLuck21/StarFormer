# Level 2 and Mobile Controls

Last updated: 2026-10-18.

## Integration scope

The `Level-2` branch integrates main through `7e50e09` (analytics PR #13) with teammate mobile/checkpoint work through `5a8864c`. This is a source integration, not a new WebGL release or deployment.

- IntroBox, IntroBubble and Level01 use main's existing scene objects unchanged, plus the teammate's separate mobile Canvas in each scene. This preserves intro hints, colors, restart buttons, completion transitions, Level 1 ball checkpoints, bubble regeneration and handoff fixes.
- Resources uses main's folder GUID. Main's renderer settings, reload options, build scene list and analytics configuration are preserved. The mobile branch's input-action preload is retained.
- Shared possession code retains main's analytics/death/checkpoint hooks and Box-to-Bubble temporary collision exclusion, while adding touch input and E release for Bubble, Spider and Fan.
- Teammate mobile controls remain in Level02, Level01_Reference and WaterPlayground as well as the intro/formal scenes.

## Controls and visibility

| Form | Direction buttons | Action buttons |
| --- | --- | --- |
| Star | Left / Right | Jump |
| Box | Left / Right | Grow / Shrink (hold) |
| Bubble in air | Left / Right | Pop |
| Bubble in water | Left / Right / Up / Down | Pop |
| Spider | Left / Right | Leave |
| Fan | Left / Right | Blow (toggle), Leave |
| SpikeWall | Left / Right | Fire |
| BouncyBall | Left / Right | None; bouncing is automatic |
| gravityBall | None | None; scripted motion |

`MobileControls` resolves Show Mode in Awake. AutoDetect shows the controls if `Application.isMobilePlatform` is true, or if Input System exposes a touchscreen without a mouse. AlwaysShow forces them on for Editor mouse tests; NeverShow hides them. There is no project-specific browser/OS parser. Browser device detection and simultaneous touches still require actual device validation.

To test in Editor, stop Play, set Show Mode to AlwaysShow, start Play and test the relevant form. Restore AutoDetect before committing/building. Changing Show Mode after Awake does not refresh visibility. Pointer release or disabling a direction button releases its held input.

## Completion and restart

- The Level02 flag's BoxCollider2D is widened from 0.08 to 1.4 units, retaining height 1.6 and Y offset 0.8. It accepts any current living controlled body. `requiredGate` is unassigned, so no final pressure-plate or Star-only requirement is imposed by the goal.
- Level02 enables `waitForMenuButton`. On completion it records analytics once, pauses gameplay, disables the player controller and overlays **Level Complete**, **You are a super star now!**, and **Return to Main Menu**. The player chooses when to leave; there is no timed return.
- IntroBubble and Level01 still use the existing timed Level up / Entering next level overlay.
- FanHint now reads **W / Blow: wind on/off** and **E / Leave: return to star**. Its text area is 10 × 2, reveal distance 4 and hide delay 2 seconds. The Spider exit hint is a single line, **E / Leave: return to star**, using font size 3 in a 10 × 1 text area, center-aligned at (-70, 15.2), in the upper middle of this section’s fixed camera view. Reveal distance remains 5 and hide delay remains 3 seconds. Spider has no independent Space jump; jumping is available as the grounded Star after release. These scene-local changes leave other proximity hints and Space shooting prompts unchanged. Hint visibility/layout still requires Play Mode verification.
- R and touch Restart clear checkpoint state and restart the current scene from the beginning. Death reload retains the checkpoint. Returning to Mainmenu and selecting a level starts a fresh run. `LevelNavigationUI.RestartLevel()` clears checkpoints; `RetryCheckpoint()` retains them and reloads the scene, running the existing checkpoint restoration events. Without a checkpoint, Retry starts at the level entrance. Both manual actions are recorded as manual restarts, not deaths. Scene buttons still need to be added and bound in Unity beneath Return to Main Menu.

## Validation and remaining checks

Evidence before integration:

- The developer ran Level02 with the keyboard and identified the release-hint and completion-feedback issues.
- The developer tested the initial Star touch buttons in Editor; the other forms were not tested in that pass.
- The developer reports a teammate's earlier build worked on a phone. This does not validate the newly integrated build.

Integration checks:

- All 73 runtime and 2 Editor scripts compile with the installed Unity 6000.3.22f1 compiler/references.
- All six mobile scenes have unique serialized object IDs and valid local references; controller/button references are present and AutoDetect is selected or used as the default.
- Main's original objects in IntroBox, IntroBubble and Level01 remain unchanged. Main analytics, BouncyBall prefab, handoff safeguards and critical project settings are retained.

Still required after integration:

1. Retest IntroBox → IntroBubble → Level01 → Level02, especially shared possession, E release, ball recovery, U2 regeneration and restart behavior.
2. Test touch controls for every form, held/released directions and resizing, water entry/exit, form changes, death and respawn. On a real phone, test simultaneous movement/action touches, focus loss and browser overlays.
3. Reach the Level02 flag in Star and another host form. Confirm one completion event, a persistent readable result screen, paused gameplay and a working menu button. Re-enter Level02 and confirm it starts from the beginning.
4. Export a fresh WebGL build and test desktop/mobile browser visibility, layout and console errors. Confirm analytics still arrives as intended using [AnalyticsTestPlan.md](AnalyticsTestPlan.md).

Pending Unity setup: add Restart Level and Retry Checkpoint beneath Return to Main Menu, binding one callback per button to the matching LevelNavigationUI method. Verify Retry before/after a checkpoint, Restart after a checkpoint, and death respawn.

Deferred: top-center form identity hints and broader instruction clarity.
