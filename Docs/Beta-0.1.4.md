# Beta 0.1.4 WebGL build

This local build uses gameplay commit `7c7cb07`. Contact with the raised smile flag in Level02 records level completion and opens the Super Star results screen. Its Return to Main Menu button loads Navigation.

## Unity settings

| Setting | Value |
| --- | --- |
| Editor and target | Unity 6000.3.22f1 with Web Build Support |
| Player version and analytics build version | `beta-0.1.4` |
| Web template | `PROJECT:StarFormerAlpha` |
| Packaging | Gzip with decompression fallback, hashed filenames, Web threads disabled |
| Scenes in build order | Navigation, IntroBox, IntroBubble, IntroBouncyBall, Level01, IntroSpider, IntroFan, Level02 |
| Analytics | Production collection, seven tracked scenes |

## Rebuild on macOS

Run the following from the StarFormer project root with that checkout closed in Unity. To keep the editor open, run it from a separate checkout containing these settings.

```sh
"/Applications/Unity/Hub/Editor/6000.3.22f1/Unity.app/Contents/MacOS/Unity" \
  -batchmode -quit -projectPath "$PWD" -buildTarget WebGL \
  -executeMethod BetaReleaseBuild.Build \
  -customBuildPath "$PWD/Builds/WebGL-Beta-0.1.4" \
  -logFile "$PWD/Beta014-WebGL.log"
```

The release method checks the eight scenes, renders the seven analytics maps and verifies the scene hashes before building. Map rendering requires a graphics device. The browser package is the complete `Builds/WebGL-Beta-0.1.4/` directory.

## Local preview

```sh
python3 -m http.server 8014 --bind 127.0.0.1 --directory Builds/WebGL-Beta-0.1.4
```

Open [the local game](http://127.0.0.1:8014/) and select **Play in Window**. To check the Level02 fix in the browser, press the final roof button with the Box, touch the raised flag, then select **Return to Main Menu**. Navigation should accept input again.

## Verification on November 10, 2026

Unity built the eight configured scenes with zero missing scripts. All seven current map entries matched their scene hashes and PNGs. The export contains seven files totaling 24,776,423 bytes. Local Chrome loaded the Unity runtime and **Play in Window** activated the game canvas. The flag-to-Navigation interaction was previously exercised in Unity Play Mode. Full-level browser playthrough and mobile testing are pending.
