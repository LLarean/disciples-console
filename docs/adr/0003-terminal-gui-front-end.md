# ADR 0003: Terminal.Gui for the console front-end

**Status:** accepted, supersedes [ADR 0002](0002-spectre-console-rendering.md)

## Context
With Spectre.Console `Live` every key press re-rendered the whole screen; on the full map with cards and panels the redraw became noticeably slow and flickery.
Terminal.Gui v2 redraws only dirty cells from its own screen buffer, which removes the lag.
The concern from ADR 0002 — its event loop fighting a turn-based loop — does not apply: the game is fully input-driven, so the Terminal.Gui loop is the game loop.

## Decision
- Front-end project `Disciples.Tui` on Terminal.Gui 2.x.
- Keep the schematic look of the Spectre version: custom-drawn cards and panels on `Canvas` views, not stock widgets.
- One root `Shell` window with a stack of `Screen` views; only the top screen is attached and receives keys. No nested modal `Run` calls.

## Consequences
- Terminal.Gui 2.5 ships only `net10.0`, so the front-end requires the .NET 10 SDK.
- `Disciples.Core` is unaffected: still netstandard2.1 / C# 9 (ADR 0001), so Unity compatibility holds.
- Mouse support comes for free if ever needed.
