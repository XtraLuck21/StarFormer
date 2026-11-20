# Gameplay Specification

Gold 0.1.0 was published November 17, 2026 to the main site and `gold/`. Deployment succeeded and all 14 hosted runtime files matched the verified export. [Gold release record](Gold-0.1.0.md)

**Current source:** Kindergarten polish updates the opening tutorials and graduation flow; `gold-level-1-polish` extends the woodland theme to Level01, adds water-route choices and revises reminders, checkpoint placement and shared Collection Book icons after the recorded Gold export. The sections below describe that source state; historical publication evidence remains separate.

## 1. Scenes and progression

```text
Navigation → IntroBox → IntroBubble → IntroBouncyBall → Level01 → IntroSpider → IntroFan → Level02
```

Navigation uses A/D to move and Space to jump. Navigation offers Kindergarten → IntroBox, Level 1 → Level01, Elementary School → IntroSpider and Level 2 → Level02. The Star jumps into the upper entrances and drops onto the lower entrances; click and touch selection are also supported. Bounce/flash feedback finishes before the Entering overlay and scene load. Menu entry starts a fresh checkpoint session. Mainmenu remains a disabled legacy scene.

| Scene | Purpose |
| --- | --- |
| IntroBox | Star movement/jump, contact possession, carrying/docking and separation/recontact through three Boxes, then a flag-opened exit |
| IntroBubble | EntryBox, dormant Bubble, air/water control, Q pop and a grounded-Star flag opening the BouncyBall passage; optional return to IntroBox endpoint |
| IntroBouncyBall | Automatic bounce, water float, low-ceiling rhythm and spike feedback; return passage to Bubble, graduation with Next Level or Main Menu choices |
| Level01 | Woodland entrance with Star entry and Bubble/BouncyBall water routes, followed by two lifts, right-side Star/Box landing choice, SAVE/NORMAL/RUIN upper routes, three fixed-form checkpoints and Home menu |
| IntroSpider / IntroFan | Existing teaching sequence before Level02; Spider exits through authored handoffs, Q leaves Fan and F toggles Fan wind |
| Level02 | Star chase, dark descent, four-gear seesaw, rising shell-release exit, Smiley Face teeth/forehead puzzles and final Box resizing |
| Level01_Legacy | Previous formal Level01, preserved as a disabled backup; not the current map |
| Tutorial / Level01_Reference / WaterPlayground / Playground | Disabled legacy/reference/sandbox scenes; EnemyScene is a separate test scene |

Enabled build indices are Navigation 0, IntroBox 1, IntroBubble 2, IntroBouncyBall 3, Level01 4, IntroSpider 5, IntroFan 6 and Level02 7. Play order follows named destinations, not consecutive build indices.

## 2. Controls

In gameplay scenes, left/right arrow keys also steer every form. Navigation uses A/D for keyboard movement.

| Form | Desktop gameplay | Left thumb | Right thumb |
| --- | --- | --- | --- |
| Star | A/D or arrow keys to move, Space to jump | Steer left or right | Tap Jump |
| Box | A/D to move, hold J or left mouse to grow, K or right mouse to shrink | Steer left or right | Hold Grow (outward arrows) or Shrink (inward arrows). Release to stop resizing. |
| Bubble | A/D in air, WASD underwater, Q or right click to pop | Steer horizontally while rising. Underwater, drag in any direction, including diagonally. | Tap Pop to return to Star |
| BouncyBall | A/D to steer | Steer horizontally. The ball bounces automatically on land and floats on water. | None |
| Spider | A/D to travel along connected surfaces; hold left mouse to aim silk, release to cast; right mouse cancels | Steer along the surface; hold and drag the web skill wheel, release to cast | None. Contact or an authored transition changes form. |
| Fan | A/D to move and face, F or left click to toggle wind, Q or right click to leave | Steer and face left or right | Tap Blow to toggle airflow. Tap Leave to return to Star. |

**Touch layout.** Touch inside the left half of the safe area to set the joystick center, then drag. The resting ring shows where to start and the thumb shows the drag direction. The ring is centered on the initial touch, so part of it can extend past the canvas near an edge. Lifting the thumb or dragging outside the canvas returns movement to neutral. Start another drag inside the movement area to steer again. A second finger can operate a right-hand button while the first keeps steering. On Navigation the movement zone occupies the lower 68% of the left half. The Jump button sits above and to the right of the Collection Book. Players can also tap a destination sign.

**Display mode.** All eight build scenes use the `Mobile Form Controls` component. Its saved `Show Mode` is `Auto Detect`, which selects touch on a mobile platform or when Unity detects a touchscreen without a mouse. To preview in Play Mode, select the component on the player controller (on Navigation, the navigation star) and set `Always Show`. Drag or click with the mouse to preview the joystick, action icons and map hints. `Never Show` previews desktop controls. Use two actual touches to test steering and an action together.

The seven gameplay scenes show a form controls card on desktop and replace it with the touch controls on mobile. Navigation teaches Move and Jump beside the star and has no corner card in either mode. All seven gameplay scenes have paged Collection Books. In non-Navigation scenes using `SceneCollectionBookUI`, the entry is a caption-free 88 × 88 square book, matching Home, at the top right beside it. This is applied at runtime, including older text Help entries. Navigation retains its own book presentation. The six form pages use shared keyboard and mouse descriptions on all devices. The possessable SpikeWall in legacy/test content uses Space to fire and has no touch layout in the active route.

**Pause and restart.** During gameplay, tap the house to continue, return to a saved checkpoint, restart the level or return to Navigation. On the Kindergarten graduation screen, the house stays accessible and returns directly to Navigation. Desktop also supports Esc for Home and R for restart choices. In desktop WebGL, the lower-right fullscreen button resumes ordinary Home pause after fullscreen entry succeeds. The first Esc exits fullscreen. In windowed play, Esc closes the current menu layer or opens Home.

## 3. Possession and body behavior

- Contact with an eligible existing host requests possession; there is no possession key or duplicate body spawned at the old location.
- The original Star becomes inactive while hosted. The controlled host displays a centered Star. Q release restores the original Star for Bubble or Fan; Spider handoffs are scene-driven.
- Contacts are validated before transfer. Nearby hosts are locked at a handoff and must remain physically separated for the configured consecutive physics frames before reacquisition. Box-to-Box separation uses actual collider clearance; this is contact rearming, not an elapsed-time possession cooldown. Fan retains its separate release/repossess cooldown. Box growth cannot continuously flip between touching hosts. An abandoned Box stops receiving horizontal drive.
- Box-to-Bubble handoff temporarily ignores that body's old Box collider until separation, allowing escape without disabling unrelated collisions. PR #21 also handles Box-to-BouncyBall recovery so the abandoned Box does not trap the controlled ball.
- Untouched Bubbles wait motionless until first possession. Activated Bubbles retain environmental physics after transfer; abandoned BouncyBalls stop and wait.
- `Starting Host` is optional: IntroBubble starts in EntryBox while retaining StarPlayer as its Initial Body. Scene return setup is distinct from checkpoints.
- Box, Bubble and BouncyBall glow identifies possessable hosts; outlines and a centered Star improve readability. Terrain, doors, water and environmental spikes are not possessable.

## 4. Forms and environment

### Star and Box

Star has gravity, can jump only from support, and cannot freely swim upward. Water reduces its movement and initial jump velocity. No oxygen timer or drowning mechanic is implemented.

Box grows/shrinks without changing mass. Grounded resizing anchors its bottom; unsupported resizing anchors its center. Solid obstacles limit growth. The controller uses its pre-resize grounded result so growth and horizontal movement can happen together. Supported liftable ceilings are moved only after clearance checks accept the requested size; ordinary neighboring hosts are not pushed through by growth.

The lower Level01 door can be raised by Box and lowered by ball landings. The upper platform is Box-only and one-way: shrinking or ball landings cannot lower it. Other pressure-plate and height-switch scripts remain available for legacy/test layouts; they are not the new first-level route.

### Bubble and water

Water membership uses the body's root position inside registered water regions, rather than partial edge overlap. BodyWaterPhysics caches/restores air physics and supports overlapping water zones.

- Air: activated Bubble rises automatically; A/D steers. W/S do not provide air descent. The controlled upward speed is capped.
- Water: automatic rise is disabled; WASD supports diagonals with a capped total speed, acceleration and braking. Unpossessed activated Bubbles retain water damping.
- Q pops the controlled Bubble and returns Star at its position. Horizontal/downward velocity is retained; upward velocity is removed. Pop requests are processed before pending possession transfers.
- By default, a popped Bubble stays inactive until scene reload. ReturnToSpawnOnLeave and BubbleRespawnOnPop are explicit opt-ins, not universal behavior. The old Bubble_U2 regeneration rule belongs to the Legacy map, not the promoted Level01.
- Level01’s entry and pool-floor backup Bubbles both regenerate after every Pop using the same `Level01WaterBubble` prefab, preserving appearance and collider size. This does not enable regeneration for its upper-ascent Bubble. See [the water routes](level-1.md#playable-route).
- IntroBubble provides a renewable backup beside the underwater divider. Each backup pop creates a fresh dormant instance at its original spawn, including when a replacement is popped again. The original Bubble does not regenerate; popping it before reaching the tank can still require restarting. See [the recovery route](intro-levels.md#introbubble).

### BouncyBall

A controlled ball automatically bounces on supporting ground. A/D steering uses acceleration/braking. It checks overhead clearance before launch; a low roof compresses the bounce and accelerates its rhythm. Open space permits the configured full bounce again. Scene-specific heights and timing override prefab defaults; the lower and upper Level01 balls must not be retuned from old Legacy-map values.

IntroBouncyBall overrides bounce height to 3.2 and seconds to apex to 0.8 locally; it does not retune the Level01/Level02 balls. Its route demonstrates a high ledge, a short water float, a safe low ceiling, then one spike in an open area before the flag. Fixed diagrams and short hints repeat when their horizontal region is revisited, without forced pauses. The low-ceiling lesson explicitly describes more bounces per second and compares vertical bounce animations. Local NoFriction terrain colliders keep wall contact from suppressing the initial climb; shared bounce tuning is unchanged; the water-support lookup correction below is shared. A spike death displays a short warning during the existing death pause.

The ball floats on the highest eligible water surface when its bottom-center lies horizontally inside the water bounds and its collider reaches the surface. Side overlap alone does not suppress bouncing on the bank. Floating removes vertical motion and uses reduced horizontal speed. The Level01 submerged entrance ball opts into gradual ascent (`waterRiseSpeed = 2` units/second): physical upward velocity accelerates and eases toward the surface, holding horizontal movement during ascent and restoring steering afterward. The shared default is zero, retaining immediate flotation elsewhere. On land, bouncing resumes. Environmental spikes remain lethal. Possessing a ball is not a checkpoint in current Level01.

### Spider, Fan and projectiles

SpiderSilkAbility adds a short wall grapple to every Spider body, including scene-authored spiders. Hold the web skill button and drag its wheel to aim, then release to cast. Releasing in the center dead zone or over the upper CANCEL target cancels without consuming cooldown. The movement joystick remains independent. Desktop also supports holding left mouse to aim at the cursor, releasing to cast, and right mouse to cancel. The skill wheel and cancel target appear only in mobile control mode.

A faint full-range wash and fine broken boundary show the 3-unit reach. A solid cyan shaft joins a broad arrowhead, stopping at the first solid hit. Two translucent side rails curve continuously into the arrow shoulders, with a small inset detail at the neck. The tip remains exposed; there are no detached barbs, dashed shaft or gaps between the tail and head. The indicator is cyan in empty space, turns yellow on contact with any solid obstacle. Contact feedback uses only this color change, with no reticle, sparks or pulse. Silk travels at 14 units/second and pulls the spider at 8 units/second when it hits a climbable solid surface. Triggers and self-colliders are ignored; other bodies, hazards and non-climbable solids cannot anchor the grapple. Body sweeps stop the pull at obstructions. Reaching a wall or ceiling hands control back to surface climbing. Cooldown is 0.7 seconds; missed/completed strands fade over 0.25 seconds. Pulling times out after 1.5 seconds. The Spider prefab exposes these values for tuning. Losing control, death or disabling the body clears the strand and restores gravity. Pause freezes the pull and cancels pending aim; interrupted gestures never auto-cast.

Spider traverses connected surfaces through HexagonSurfaceMovement. Fan retains wind and facing when abandoned; F toggles airflow. Current airflow targets eligible non-possessable obstacles, not Star/Box/Bubble. Expanded form-specific wind behavior remains a separate design decision. SpikeShooterAbility retains Space firing and the existing projectile/button interactions.

## 5. Level01 route and presentation

See [level-1.md](level-1.md) for the authored walkthrough, objects and spawn positions. PR #21 adds bidirectional upper-gap clearance, a wider checkpoint-2 approach and steadier BouncyBall camera framing; the polish branch additionally moves checkpoint 2 left onto open ground.

| Mode | Geometry and interaction |
| --- | --- |
| NORMAL | Default floor spikes; selected by neither button initially |
| SAFE | No spikes; three black fixed-length boards form an S-shaped timing route. Rightmost board is stationary; the other two translate vertically in opposite phase. The left board permits low-position jumping and high-position passage below. Anti-crush checks pause the pair together. |
| HARD | Default and additional floor spikes plus aligned ceiling spikes. One ceiling spike drops when the controlled body approaches its upper-corridor trigger band; it remains lethal on the floor until reset. |

SAVE accepts Star, grounded Box D or R2 from below. RUIN is R2-only and protected by a breakable cover: the first upward hit breaks it and forces a descent; a later bounce after landing can press the button. Raising the optional upper platform gives access to the button. R2 can return to SAVE.

The buttons are mutually exclusive. The selected one reads BACK TO NORMAL. SAVE can toggle again after contact ends in any direction and the body touches it again, including vertical bounce or shrink/grow cycles. Held contact does not repeat. RUIN retains its horizontal leave-and-return requirement. Route switching retains upper-platform height and cover progress.

The first Box/Star arrival below SAVE previews the left hazards. Selecting a mode pauses control for the route animation. SAFE fully animates sinking hazards and emerging boards, holds the completed layout for 2 seconds, then returns the camera and control without waiting for a movement cycle; camera transitions remain quick; see [presentation timing](level-1.md#route-selection-and-camera-presentation). HARD exposes extra spikes; NORMAL restores default geometry. Ordinary framing uses AdvancedLevelCamera2D with region-specific bounds/zoom.

The entrance starts as Star with no READY button or forced Box. Gold title and cream subtitle sit on a solid woodland wall. Bubble and BouncyBall offer underwater and upper-deck routes. First possession shows a brief nearby REMEMBER? ability reminder at a fixed scene anchor; no route arrows, platform highlights or optional water hint buttons remain. A Ball entering the right-hand water pocket automatically receives No ground to bounce. / Press R to restart. (touch: Home > Restart Level), without a REMEMBER? heading or ceiling-contact requirement.

The lower-door Hint appears after an idle delay and accepts mouse clicks as well as deliberate Box/Ball contact. Its paused lift animation can be replayed without limit until the real door is raised; continued overlap does not cause automatic replays. Clicking the sign does not resize Box. See [hint timing and lifecycle](level-1.md#hints-and-home-menu).

## 6. Death, checkpoints and menus

SpikeHazard kills the controlled body; unpossessed bodies do not themselves cause player death. BubblePopHazard pops Bubble and kills Box while ignoring other forms; this shared hazard remains available in other/legacy scenes. Death reloads after the configured delay. Optional vertical limits are scene settings: formal Level01 has `useHeightLimit = false`, so the old Legacy -12/16 limits are not its active boundaries.

Current Level01 uses ADV_CP1 → ADV_CP2 → ADV_CP3, restoring Box → BouncyBall → Star. Hollow markers turn blue to yellow and briefly show CHECKPOINT SAVED. Only grounded Star/Box activates the upper landing checkpoint; earlier checkpoints cannot replace later ones.

Retry/death retains the latest completed mode, raised upper platform, broken RUIN cover and viewed camera presentations. Loose bodies and the lower door reset; SAFE motion restarts predictably and the falling trap rearms. Full restart, menu departure/entry and forward progression clear the session. Checkpoint state is not stored across application launches.

The Level01 Home square shows a house outline with a small Star. Its Home · Restart tooltip leads to Paused: Continue Playing, Return to Checkpoint, Restart Level and Main Menu. Checkpoint is disabled without a save; full restart and Main Menu require confirmation. R retains its own restart-choice menu. Opening/cancelling menus changes no progress, and the R menu pauses the death countdown.

The promoted Level02 uses dedicated drop/seesaw recovery and five ordered Smiley Face entries, restoring Star, Spider or the final Box as configured. Its approximately 0.18-second Respawning pause is scene-specific. Home/R retain their checkpoint/full-restart choices; the old standalone buttons are disabled. See [CheckpointGuidance.md](CheckpointGuidance.md) and [level-2.md](level-2.md).

## 7. Completion and navigation

- IntroBox's controlled BoxC opens the gate; its separate exit loads IntroBubble directly, preserving the outgoing Box width and floor/camera framing. IntroBubble can also return to IntroBox's endpoint using one-shot BoxC setup.
- IntroBubble requires grounded Star to open its exit door; walking right loads IntroBouncyBall directly, without a Level up overlay. The ball tutorial left exit returns to Bubble as Star at its flag with the door open. This one-shot return setup is not a full scene snapshot.
- IntroBouncyBall accepts controlled Star or BouncyBall at its flag, records completion and opens the animated Kindergarten graduation screen. Gameplay stays paused until Next Level loads Level01 or the accessible house loads Navigation. There is no automatic transition; see [completion feedback](intro-levels.md#completion-feedback).
- Level01 accepts any controlled living form at its flag, with no gate prerequisite. It opens the Elementary School acceptance letter and waits for Next Level to load IntroSpider; Home returns directly to Navigation and stays above the completion backdrop. A brief envelope/Star reveal finishes before the page rests; there is no automatic load. IntroSpider continues to IntroFan, then Level02.
- Other timed transitions disable player control, pause gameplay, clear stale UI selection and clear checkpoint sessions before loading. The default delay is 1.5 seconds in real time.
- In Level02 the assigned final Box presses the roof button. Touching the raised flag with the controlled body records completion and opens the result screen. Tap Return to Main Menu to load Navigation.
- Navigation replaces the old playable Mainmenu as startup and return destination. It uses authored entrance interactions, not the legacy UI-platform landing behavior. Coming Soon has no destination.
- Collection Book uses reusable UI assets and blocks Navigation movement while open. Navigation’s book, configured gameplay books and Shape Closet all read session-only `HelpBookUnlocks`. Successful possession reveals only the controlled form; an assigned starting/recovery host also counts. Star help is always available. Completion adds no form groups. Scene changes, death and level restart retain discoveries; refreshing/reopening the game or starting a new Editor Play session clears them. Old persistent completion saves are ignored. See [session-only discovery](Beta-0.1.2.md#session-only-shape-discovery).

## 8. Validation and remaining work

Level01 branch evidence and remaining checks are recorded in [Level01 polish source acceptance](AnalyticsTestPlan.md#level01-polish-source-acceptance). Current Kindergarten source was accepted in developer playtests, including graduation and its two exits. See [Kindergarten evidence and remaining acceptance](AnalyticsTestPlan.md#kindergarten-polish-source-acceptance); prior release evidence below does not certify this branch on WebGL/mobile.

Unity compilation and isolated checks passed for scene entry/continuation, completion in all four Level01 forms, checkpoint recovery, pause/restart menus, lift behavior and merged possession/input behavior. The developer confirmed the merge and formal-Level01 promotion playtests passed. The developer also accepted the BouncyBall tutorial/passage iterations and confirmed a successful Web build after fix `3365e29`. The final Beta package was subsequently rebuilt after version/template synchronization and published on November 2; the developer confirmed local and hosted playtests passed. That November 2 evidence confirms Beta 0.1.0 Level01 records. Dashboard Version 9 subsequently resolved historical release-map matching; it does not certify 0.1.1 event delivery or its refreshed map deployment.

The November 5 package check applies only to historical Beta 0.1.1. Beta 0.1.2 passed deployment and 14-file hosted comparison on November 6. PR #21/#22 had developer-reported playtest acceptance and discovery/Help passed isolated Editor checks; hosted gameplay still needs the [current acceptance checks](Beta-0.1.2.md). Broader browser/mobile coverage and production analytics/map acceptance remain separate. Script responsibilities are in [ScriptGuide.md](ScriptGuide.md).
