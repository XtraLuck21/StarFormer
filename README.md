# StarFormer

StarFormer is a 2D puzzle-platformer built in Unity. Play as a star that can possess objects on contact and use their abilities to explore and solve puzzles. Grow or shrink as a box to fit through passages and activate switches, or become a bubble to rise through the air and swim underwater. Progress comes from switching between forms and combining their strengths with the environment.

## Play the current build

[Play StarFormer in your browser](https://xtraluck21.github.io/StarFormer/)

The current test release, **All-Level V1 (October 12, 2026)**, includes the interactive main menu, Tutorial, Level 1 and Level 2. Use A/D or Left/Right Arrow to move the menu star, Space to jump, and the mouse to choose a level. All three playable levels include a Return to Main Menu button.

Level 2 and its navigation are integrated into `main`. All-Level V1 was built from source commit `773912d` with local editor/build configuration changes and passed developer-reported editor and localhost testing. The exported build was published in `gh-pages` commit `6f1737a`, and GitHub Pages reports deployment success. This is the agreed interim baseline while the team collects feedback before further changes. Deployment success is separate from exhaustive online playtesting. See the publishing guide for the exact build and verification record.

## Development and release notes

- Unity editor: `6000.3.22f1`, with Web Build Support.
- Build scenes: `Mainmenu` (0), `Tutorial` (1), `Level01` (2), `Level02` (3).
- [Gameplay specification](Docs/GameplaySpec.md)
- [Script, scene and Web publishing guide](Docs/ScriptGuide.md)
- [Level 1 progress](Docs/level-1.md)

![StarFormer tutorial gameplay](Docs/Game_screenshot.png)
