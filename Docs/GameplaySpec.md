# Gameplay Specification

Touch controls reviewed November 16, 2026. Beta 0.1.3 is published at `/beta/` via `5603d2f`; Pages succeeded and all seven runtime hashes match. Root remains Beta 0.1.2. See [the current release record](Beta-0.1.3.md) and [historical 0.1.2 evidence](Beta-0.1.2.md). Package verification does not establish full gameplay correctness.

## 1. Scenes and progression

```text
Navigation → IntroBox → IntroBubble → IntroBouncyBall → Level01 → IntroSpider → IntroFan → Level02
```

Navigation offers Kindergarten → IntroBox, Level 1 → Level01, Elementary School → IntroSpider and Level 2 → Level02. The Star jumps into the upper entrances and drops onto the lower entrances; click selection is also supported. Bounce/flash feedback finishes before the Entering overlay and scene load. Menu entry starts a fresh checkpoint session. Mainmenu remains a disabled legacy scene.

| Scene | Purpose |
| --- | --- |
| IntroBox | Star movement/jump, automatic contact possession, Box growth/shrink and flag-opened exit |
| IntroBubble | EntryBox, dormant Bubble, air/water control, Q pop and a grounded-Star flag opening the BouncyBall passage; optional return to IntroBox endpoint |
| IntroBouncyBall | Automatic bounce, water float, low-ceiling rhythm and spike feedback; return passage to Bubble, final flag to Level01 |
| Level01 | Former Advanced layout: two lifts, right-side Star/Box landing choice, SAVE/NORMAL/RUIN upper routes, three fixed-form checkpoints and Home menu |
| IntroSpider / IntroFan | Existing teaching sequence before Level02; Spider exits through authored handoffs, Q leaves Fan and F toggles Fan wind |
| Level02 | Star chase, dark descent, four-gear seesaw, rising shell-release exit, Smiley Face teeth/forehead puzzles and final Box resizing |
| Level01_Legacy | Previous formal Level01, preserved as a disabled backup; not the current map |
| Tutorial / Level01_Reference / WaterPlayground / Playground | Disabled legacy/reference/sandbox scenes; EnemyScene is a separate test scene |

Enabled build indices are Navigation 0, IntroBox 1, IntroBubble 2, IntroBouncyBall 3, Level01 4, IntroSpider 5, IntroFan 6 and Level02 7. Play order follows named destinations, not consecutive build indices.

## 2. Controls

| Form | Desktop gameplay | Left thumb | Right thumb |
| --- | --- | --- | --- |
| Star | A/D or arrow keys to move, Space to jump | Steer left or right | Tap Jump |
| Box | A/D to move, hold J or left mouse to grow, K or right mouse to shrink | Steer left or right | Hold Grow (outward arrows) or Shrink (inward arrows). Release to stop resizing. |
| Bubble | A/D in air, WASD underwater, Q or right click to pop | Steer horizontally while rising. Underwater, drag in any direction, including diagonally. | Tap Pop to return to Star |
| BouncyBall | A/D to steer | Steer horizontally. The ball bounces automatically on land and floats on water. | None |
| Spider | A/D to travel along connected surfaces | Steer along the surface | None. Contact or an authored transition changes form. |
| Fan | A/D to move and face, F or left click to toggle wind, Q or right click to leave | Steer and face left or right | Tap Blow to toggle airflow. Tap Leave to return to Star. |

**Touch layout.** Touch inside the left half of the safe area to set the joystick center, then drag. The resting ring shows where to start and the thumb shows the drag direction. The ring is centered on the initial touch, so part of it can extend past the canvas near an edge. Lifting the thumb or dragging outside the canvas returns movement to neutral. Start another drag inside the movement area to steer again. A second finger can operate a right-hand button while the first keeps steering. On Navigation the movement zone occupies the lower 68% of the left half. The Jump button sits above and to the right of the Collection Book. Players can also tap a destination sign.

**Display mode.** All eight build scenes use the `Mobile Form Controls` component. Its saved `Show Mode` is `Auto Detect`, which selects touch on a mobile platform or when Unity detects a touchscreen without a mouse. To preview in Play Mode, select the component on the player controller (on Navigation, the navigation star) and set `Always Show`. Drag or click with the mouse to preview the joystick, action icons and map hints. `Never Show` previews desktop controls. Use two actual touches to test steering and an action together.

The seven gameplay scenes show a form controls card on desktop and replace it with the touch controls on mobile. Navigation teaches Move and Jump beside the star and has no corner card in either mode. All seven gameplay scenes have paged Collection Books. The six form pages use shared keyboard and mouse descriptions on all devices. The possessable SpikeWall in legacy/test content uses Space to fire and has no touch layout in the active route.

**Pause and restart.** Tap the house in any gameplay scene to continue, return to a saved checkpoint, restart the level or return to Navigation. Desktop also supports Esc for Home and R for restart choices. In desktop WebGL, the lower-right fullscreen button resumes ordinary Home pause after fullscreen entry succeeds. The first Esc exits fullscreen. In windowed play, Esc closes the current menu layer or opens Home.

## 3. Possession and body behavior

- Contact with an eligible existing host requests possession; there is no possession key or duplicate body spawned at the old location.
- The original Star becomes inactive while hosted. The controlled host displays a centered Star. Q release restores the original Star for Bubble or Fan; Spider handoffs are scene-driven.
- Contacts are validated before transfer. Nearby previous hosts must separate before reacquisition; Box growth cannot continuously flip between touching hosts. An abandoned Box stops receiving horizontal drive.
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
- IntroBubble provides one spare Bubble, not unlimited regeneration. Premature pops can still require restarting.

### BouncyBall

A controlled ball automatically bounces on supporting ground. A/D steering uses acceleration/braking. It checks overhead clearance before launch; a low roof compresses the bounce and accelerates its rhythm. Open space permits the configured full bounce again. Scene-specific heights and timing override prefab defaults; the lower and upper Level01 balls must not be retuned from old Legacy-map values.

IntroBouncyBall overrides bounce height to 3.2 and seconds to apex to 0.8 locally; it does not retune the Level01/Level02 balls. Its aligned water banks and continuous floor lead across separated spikes into a low-ceiling section on the forward route. Contextual headlines teach each behavior without explanatory subtitles; a spike death displays a short warning during the existing death pause.

The ball floats on the highest overlapping water surface, loses vertical motion there and uses reduced horizontal speed. On land, bouncing resumes. Environmental spikes remain lethal. Possessing a ball is not a checkpoint in current Level01.

### Spider, Fan and projectiles

Spider traverses connected surfaces through HexagonSurfaceMovement. Fan retains wind and facing when abandoned; F toggles airflow. Current airflow targets eligible non-possessable obstacles, not Star/Box/Bubble. Expanded form-specific wind behavior remains a separate design decision. SpikeShooterAbility retains Space firing and the existing projectile/button interactions.

## 5. Level01 route and presentation

See [level-1.md](level-1.md) for the authored walkthrough, objects and spawn positions. PR #21 adds bidirectional upper-gap clearance, a wider checkpoint-2 approach and steadier BouncyBall camera framing.

| Mode | Geometry and interaction |
| --- | --- |
| NORMAL | Default floor spikes; selected by neither button initially |
| SAFE | No spikes; three black fixed-length boards form an S-shaped timing route. Rightmost board is stationary; the other two translate vertically in opposite phase. The left board permits low-position jumping and high-position passage below. Anti-crush checks pause the pair together. |
| HARD | Default and additional floor spikes plus aligned ceiling spikes. One ceiling spike drops when the controlled body approaches its upper-corridor trigger band; it remains lethal on the floor until reset. |

SAVE accepts Star, grounded Box D or R2 from below. RUIN is R2-only and protected by a breakable cover: the first upward hit breaks it and forces a descent; a later bounce after landing can press the button. Raising the optional upper platform gives access to the button. R2 can return to SAVE.

The buttons are mutually exclusive. The selected one reads BACK TO NORMAL. After horizontally leaving its band and returning, touching it restores NORMAL. Continuous vertical bouncing and edge shrinking cannot retrigger it. Route switching retains upper-platform height and cover progress.

The first Box/Star arrival below SAVE previews the left hazards. Selecting a mode pauses control for the route animation. SAFE sinks hazards, reveals boards and demonstrates a complete alternating cycle before returning the camera. HARD exposes extra spikes; NORMAL restores default geometry. Ordinary framing uses AdvancedLevelCamera2D with region-specific bounds/zoom.

The optional entrance READY? sign changes to LET'S GO! and replays bounce/sparkles on each separate upward Star contact. The level title/subtitle, glowing forms and timed map hints are implemented. The first tank ceiling gives a desktop R reminder after 1.5 seconds of qualifying contact. Touch mode hides this prompt. Bubble’s Pop button is available throughout possession. Beta 0.1.1 adds an optional star hint above the lower movable-door path after 20 seconds without door movement. Deliberate Box growth or Ball contact activates a brief paused door-lift preview with “A little lift?”; it is shown once per scene load and avoids activation from spawn overlap. Current source also includes paged form Help in Level01.

## 6. Death, checkpoints and menus

SpikeHazard kills the controlled body; unpossessed bodies do not themselves cause player death. BubblePopHazard pops Bubble and kills Box while ignoring other forms; this shared hazard remains available in other/legacy scenes. Death reloads after the configured delay. Optional vertical limits are scene settings: formal Level01 has `useHeightLimit = false`, so the old Legacy -12/16 limits are not its active boundaries.

Current Level01 uses ADV_CP1 → ADV_CP2 → ADV_CP3, restoring Box → BouncyBall → Star. Hollow markers turn blue to yellow and briefly show CHECKPOINT SAVED. Only grounded Star/Box activates the upper landing checkpoint; earlier checkpoints cannot replace later ones.

Retry/death retains the latest completed mode, raised upper platform, broken RUIN cover and viewed camera presentations. Loose bodies and the lower door reset; SAFE motion restarts predictably and the falling trap rearms. Full restart, menu departure/entry and forward progression clear the session. Checkpoint state is not stored across application launches.

The Level01 Home square shows a house outline with a small Star. Its Home · Restart tooltip leads to Paused: Continue Playing, Return to Checkpoint, Restart Level and Main Menu. Checkpoint is disabled without a save; full restart and Main Menu require confirmation. R retains its own restart-choice menu. Opening/cancelling menus changes no progress, and the R menu pauses the death countdown.

The promoted Level02 uses dedicated drop/seesaw recovery and five ordered Smiley Face entries, restoring Star, Spider or the final Box as configured. Its approximately 0.18-second Respawning pause is scene-specific. Home/R retain their checkpoint/full-restart choices; the old standalone buttons are disabled. See [CheckpointGuidance.md](CheckpointGuidance.md) and [level-2.md](level-2.md).

## 7. Completion and navigation

- IntroBox's controlled-Box flag opens the gate; its separate exit loads IntroBubble directly. IntroBubble can also return to IntroBox's endpoint using one-shot BoxB setup.
- IntroBubble requires grounded Star to open its exit door; walking right loads IntroBouncyBall directly, without a Level up overlay. The ball tutorial left exit returns to Bubble as Star at its flag with the door open. This one-shot return setup is not a full scene snapshot.
- IntroBouncyBall accepts controlled Star or BouncyBall at its flag, then shows Entering Level 1 before loading Level01.
- Level01 accepts any controlled living form at its flag, with no gate prerequisite. It shows Entering Level 2 Intro and loads IntroSpider. IntroSpider continues to IntroFan, then Level02.
- Timed transitions disable player control, pause gameplay, clear stale UI selection and clear checkpoint sessions before loading. The default delay is 1.5 seconds in real time.
- In Level02 the assigned final Box presses the roof button. Touching the raised flag with the controlled body records completion and opens the result screen. Tap Return to Main Menu to load Navigation.
- Navigation replaces the old playable Mainmenu as startup and return destination. It uses authored entrance interactions, not the legacy UI-platform landing behavior. Coming Soon has no destination.
- Collection Book uses reusable UI assets and blocks Navigation movement while open. Navigation’s book, configured gameplay books and Shape Closet all read session-only `HelpBookUnlocks`. Successful possession reveals only the controlled form; an assigned starting/recovery host also counts. Star help is always available. Completion adds no form groups. Scene changes, death and level restart retain discoveries; refreshing/reopening the game or starting a new Editor Play session clears them. Old persistent completion saves are ignored. See [session-only discovery](Beta-0.1.2.md#session-only-shape-discovery).

## 8. Validation and remaining work

Unity compilation and isolated checks passed for scene entry/continuation, completion in all four Level01 forms, checkpoint recovery, pause/restart menus, lift behavior and merged possession/input behavior. The developer confirmed the merge and formal-Level01 promotion playtests passed. The developer also accepted the BouncyBall tutorial/passage iterations and confirmed a successful Web build after fix `3365e29`. The final Beta package was subsequently rebuilt after version/template synchronization and published on November 2; the developer confirmed local and hosted playtests passed. That November 2 evidence confirms Beta 0.1.0 Level01 records. Dashboard Version 9 subsequently resolved historical release-map matching; it does not certify 0.1.1 event delivery or its refreshed map deployment.

The November 5 package check applies only to historical Beta 0.1.1. Beta 0.1.2 passed deployment and 14-file hosted comparison on November 6. PR #21/#22 had developer-reported playtest acceptance and discovery/Help passed isolated Editor checks; hosted gameplay still needs the [current acceptance checks](Beta-0.1.2.md). Broader browser/mobile coverage and production analytics/map acceptance remain separate. Script responsibilities are in [ScriptGuide.md](ScriptGuide.md).
