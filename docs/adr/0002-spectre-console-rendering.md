# ADR 0002: Spectre.Console for rendering

**Status:** superseded by [ADR 0003](0003-terminal-gui-front-end.md)

## Context
The console front-end should look good: panels, borders, colors, tables. Options considered:

| Library | Pros | Cons |
|---------|------|------|
| Spectre.Console | Rich layout, tables, true color, mature, simple API | Not a full TUI; key input and game loop are ours |
| Terminal.Gui v2 | Real windows, focus, mouse | Heavy, own event loop fights a turn-based game loop |
| SadConsole | Pixel-perfect retro look | Renders into a MonoGame window, not a terminal |
| Own buffer renderer | Full control | Everything drawn by hand |

## Decision
Spectre.Console with `Live` display and our own `ReadKey` loop.

## Consequences
- If flicker or redraw cost on large maps becomes a problem, map panel may switch to a custom buffered widget while keeping Spectre for the rest.
