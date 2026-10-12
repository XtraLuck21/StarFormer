# StarFormer

StarFormer is a 2D puzzle-platformer built in Unity. Play as a star that can possess objects on contact and use their abilities to explore and solve puzzles. Grow or shrink as a box to fit through passages and activate switches, or become a bubble to rise through the air and swim underwater. Progress comes from switching between forms and combining their strengths with the environment.

## Play the current build

[Play StarFormer in your browser](https://xtraluck21.github.io/StarFormer/)

The current test release, **All-Level V2 (October 12, 2026)**, includes the interactive main menu, Tutorial, Level 1 and Level 2. Use A/D or Left/Right Arrow to move the menu star, Space to jump, and the mouse to choose a level. All three playable levels include a Return to Main Menu button.

Level 2 and its navigation are integrated into `main`. The latest release includes the fan-hint fix from source commit `d161538`. Output `Builds/WebGL-All-Level-V2` was published as `gh-pages` commit `8c48475`, and GitHub Pages reports deployment success. The developer reports the build works. Record specific online gameplay checks separately from deployment status. All-Level V1 remains the earlier baseline in the release history.

## Development and release notes

- Unity editor: `6000.3.22f1`, with Web Build Support.
- Build scenes: `Mainmenu` (0), `Tutorial` (1), `Level01` (2), `Level02` (3).
- [Gameplay specification](Docs/GameplaySpec.md)
- [Publish to GitHub Pages: step-by-step instructions](Docs/publish_instructions.md)
- [Script, scene and release reference](Docs/ScriptGuide.md)
- [Level 1 progress](Docs/level-1.md)

![StarFormer tutorial gameplay](Docs/Game_screenshot.png)
