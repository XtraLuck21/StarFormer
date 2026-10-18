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
- Level02 enables `waitForMenuButton`. On completion it records analytics once, pauses gameplay, disables the player controller and overlays **Level Complete**, **Thanks for playing!**, and **Return to Main Menu**. The player chooses when to leave; there is no timed return.
- IntroBubble and Level01 still use the existing timed Level up / Entering next level overlay.
- The obsolete Level02 Space/cancel hint now says **Press E or tap Leave to release**. Space shooting prompts remain unchanged.
- R and touch Restart clear checkpoint state and restart the current scene from the beginning. Death reload retains the checkpoint. Returning to Mainmenu and selecting a level starts a fresh run. A checkpoint-retry button has not been introduced in this integration.

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

Deferred: top-center form identity hints, broader instruction clarity and the menu-area Restart/Retry Checkpoint button design. Keep button labels consistent with the eventual reset scope.
