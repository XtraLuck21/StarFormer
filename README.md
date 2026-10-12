# StarFormer

StarFormer is a 2D puzzle-platformer built in Unity. Play as a star that can possess objects on contact and use their abilities to explore and solve puzzles. Grow or shrink as a box to fit through passages and activate switches, or become a bubble to rise through the air and swim underwater. Progress comes from switching between forms and combining their strengths with the environment.

## Play the current build

[Play StarFormer in your browser](https://xtraluck21.github.io/StarFormer/)

The current test release includes the interactive main menu, Tutorial and Level 1. Level 2 is a Coming Soon placeholder. Use A/D or Left/Right Arrow to move the menu star, Space to jump, and the mouse to choose a level. Tutorial and Level 1 each include a Return to Main Menu button.

As of October 11, 2026, the developer reports successful local browser and GitHub Pages playtests. Navigation was merged into `main` through PR #9 (`9fc0f95`). The tested Web build is published separately on `gh-pages`, which is not merged into `main`.

## Development and release notes

- Unity editor: `6000.3.22f1`, with Web Build Support.
- Build scenes: `Mainmenu` (0), `Tutorial` (1), `Level01` (2).
- [Gameplay specification](Docs/GameplaySpec.md)
- [Script, scene and Web publishing guide](Docs/ScriptGuide.md)
- [Level 1 progress](Docs/level-1.md)

![StarFormer tutorial gameplay](Docs/Game_screenshot.png)
