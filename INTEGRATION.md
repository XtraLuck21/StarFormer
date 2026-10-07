# Rolling Ball integration notes

The prototype is intentionally independent from the other team's scripts.

The reusable asset lives at:

`Assets/Prefabs/Characters/RollingBall.prefab`

Its direct runtime dependencies are:

- `Assets/Scripts/Core/ControllableBody.cs`
- `Assets/Scripts/Core/PlayerControlManager.cs`
- `Assets/Scripts/Characters/RollingBallController.cs`
- `Assets/Art/Ball.png`
- `Assets/Art/Star.png`
- `Assets/Physics/BallSurface.physicsMaterial2D`

When merging into a different possession framework, keep the prefab's
Rigidbody2D, CircleCollider2D, physics material, renderer, control marker, and
the movement/bounce tuning values. Adapt only the ownership callbacks and input
delivery points in `RollingBallController`.

The prefab's autonomous motion is part of the character design, not the sample
level. While it is uncontrolled, `RollingBallController.FixedUpdate` applies a
rightward rolling force with a capped speed, allowing every prefab instance to
roll down a slope on its own. Possession stops that autonomous force, and the
player's first horizontal steering input starts the automatic bounce cycle.

Reusable tuning fields:

- `autonomousAcceleration`: how quickly the idle ball begins rolling
- `autonomousMaximumSpeed`: idle rolling speed cap
- `autonomousTorque`: visible rolling rotation
- `bounceSpeed`: vertical speed of controlled ground bounces
- `wallBounceSpeed`: response speed against vertical surfaces
