# Gold 0.1.0 WebGL build

Gold uses main at `c8f2444` with the Player and production Analytics versions set to `gold-0.1.0`. Since Beta 0.1.4, the eight active scenes have gained touch controls and form-specific hints. Navigation supports tapping level signs, mobile steering recovers from interrupted touches, and the Level02 seesaw Fan starts with its wind off until the player presses Blow.

## Build configuration

| Setting | Value |
| --- | --- |
| Editor and target | Unity 6000.3.22f1 with Web Build Support |
| Scenes, in order | Navigation, IntroBox, IntroBubble, IntroBouncyBall, Level01, IntroSpider, IntroFan, Level02 |
| Analytics | Production collection in all seven gameplay scenes |
| Web template | `PROJECT:StarFormerAlpha` reads the Gold label from Player Version |
| Packaging | Gzip, decompression fallback, hashed filenames, Web threads disabled |

## Build and preview

From the project root, close this checkout in Unity or use a separate copy with the Gold settings. The existing release entry point checks scene references, regenerates the analytics maps and validates their scene hashes before building. Map rendering requires a graphics device.

```sh
mkdir -p Logs
"/Applications/Unity/Hub/Editor/6000.3.22f1/Unity.app/Contents/MacOS/Unity" \
  -batchmode -quit -projectPath "$PWD" -buildTarget WebGL \
  -executeMethod BetaReleaseBuild.Build \
  -customBuildPath "$PWD/Builds/WebGL-Gold-0.1.0" \
  -logFile "$PWD/Logs/Gold010-WebGL.log"
```

Serve the complete export from the project root:

```sh
python3 -m http.server 8010 --bind 127.0.0.1 --directory Builds/WebGL-Gold-0.1.0
```

Open [the local game](http://127.0.0.1:8010/) and select **Play in Window**. Before publication, check touch steering, simultaneous actions and rotation on a phone, then play through the Level02 flag and return to Navigation. Check production analytics for the Gold version during the release playtest.

## Local verification on November 17, 2026

Unity reported a successful build. All eight enabled scenes passed the release checks with zero missing scripts. The seven regenerated map images and their catalogs matched the source checkout byte for byte. The complete export contains seven files totaling 24,796,694 bytes.

Desktop Chrome loaded the Unity runtime over local HTTP, displayed `gold-0.1.0` and activated the game canvas after **Play in Window**. Chrome's software WebGL renderer reported unsupported CoreCopy and HDRDebugView shaders during startup. Physical-device rendering, full-level playthroughs and production event delivery remain unverified.
