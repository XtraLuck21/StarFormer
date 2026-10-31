# Restart menu and checkpoint feedback

Source documentation updated: October 31, 2026 (restart implementation from October 30). Requires rebuilding before the hosted game changes.

- Press R or a TouchHoldButton with Kind = Restart to open the restart menu. No hold is required; opening the menu never reloads the scene.
- Return to Checkpoint appears only when the current scene has an activated positional or Bouncy Ball checkpoint. It preserves that checkpoint and reloads using the existing recovery flow.
- Restart Level clears checkpoint state and starts the current level from its entrance.
- Cancel, Escape, or R closes the menu without changing checkpoint state. Cancel is initially selected. Arrow keys navigate within the menu; Enter/Space selects the focused button.
- The menu pauses gameplay and automatic death recovery. Cancelling restores the previous time scale and remaining death-recovery delay.
- The permanent top panel, route instructions, and hold progress are removed. Activation shows a short CHECKPOINT SAVED notification. World-position checkpoint markers remain.
- Existing scene buttons retain their explicit Restart Level / Retry Checkpoint behavior.

IntroBubble hides its standalone Restart button, so keyboard players use R for these choices. Its left exit is a separate scene-return action to the IntroBox endpoint as BoxB with the door open; it is not Return to Checkpoint.

## Level 1 bubble reminder

Before BouncyBall_Upper (the second checkpoint) has been activated, a controlled Bubble whose Y position stays within 0.05 world units for 2 seconds shows a small "Press E to pop bubble" above the body. Horizontal movement does not reset this timer, so sliding sideways while blocked vertically still shows the reminder. Meaningful vertical movement, changing forms, dying, or pausing hides and resets it. Other forms and scenes do not show the bubble-specific instruction.

**Known mismatch:** the quoted E reminder is still hardcoded in CheckpointFeedbackUI. The shared Bubble input has changed to **Q**. Update that reminder in a separately authorized code change and validate it in Level01; the IntroBubble map hint already displays Q.

The checkpoint session remembers which balls have been activated across retries, so revisiting the lower ball after the upper checkpoint does not re-enable the hint. Full restart or a fresh level entry clears that history. Delay and vertical tolerance can be adjusted on the runtime CheckpointFeedbackUI component. The reminder uses 18-point text; checkpoint markers use 16-point text without pulsing, and the save notification uses 20-point text.

## Verification

Compilation was checked against the project's Unity assembly references. Play Mode and visual verification remain to be done.

1. In IntroBox/IntroBubble or a fresh Level01/Level02, press R. Confirm only Restart Level and Cancel appear. Cancel and verify gameplay resumes without a reload.
2. Activate each Level01 ball and each Level02 checkpoint. Confirm Return to Checkpoint appears. Select it and verify the existing respawn behavior and checkpoint state are preserved.
3. With an active checkpoint, choose Restart Level. Verify the level entrance and that the checkpoint option is absent when reopening the menu.
4. Verify Cancel, Escape, a second R press, mouse/touch buttons, and keyboard navigation. Holding R must not repeatedly toggle the dialog. Background controls must not receive clicks through the backdrop.
5. Open the menu during the death delay and leave it open beyond that delay. Confirm no automatic reload occurs while open; cancelling resumes the remaining delay.
6. Check 16:9 and narrow windows, checkpoint marker readability, main-menu entry, and completion overlays.
7. Before the upper ball checkpoint, move Bubble sideways while keeping its Y fixed for over 2 seconds. Verify the small reminder appears despite horizontal movement, follows the bubble, and hides when moving vertically or popping. Verify continuous vertical travel and other forms/scenes do not show it. Activate the upper ball, retry, then control a Bubble again: the reminder should remain suppressed. A fresh full restart should enable it again. Check that the smaller checkpoint markers and notification do not obscure the route.
