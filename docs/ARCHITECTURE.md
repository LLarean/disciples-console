# Architecture

## Goal
Game logic lives in a presentation-agnostic core that can later be dropped into a Unity project unchanged.
The console app is the first of possibly several front-ends.

```
┌────────────────────────┐     ┌──────────────────────────┐
│ Disciples.Tui          │     │ (future) Unity project   │
│ Terminal.Gui, input    │     │ MonoBehaviours, sprites  │
│ screens, JSON loading  │     │ ScriptableObjects / JSON │
└───────────┬────────────┘     └────────────┬─────────────┘
            │ commands ↓   ↑ state + events │
        ┌───┴───────────────────────────────┴───┐
        │ Disciples.Core (netstandard2.1, C# 9) │
        │ domain model, rules, game session     │
        └───────────────────────────────────────┘
```

## Projects

| Project | Target | Depends on | Responsibility |
|---------|--------|-----------|----------------|
| `Disciples.Core` | netstandard2.1, C# 9 | nothing | Domain model and rules |
| `Disciples.Tui` | net10.0 | Core, Terminal.Gui | Rendering, input, screens, content loading |
| `Disciples.Core.Tests` | net8.0 | Core, xUnit | Rule tests |

## Core rules
- No `System.Console`, no file IO, no `UnityEngine`, no third-party packages.
- C# 9 max: no `record struct`, `required`, file-scoped namespaces, global usings, collection expressions. `record class` and `init` need an `IsExternalInit` shim — avoid unless there is a clear win.
- Randomness only through an injected `IRandom` (seedable in tests, `UnityEngine.Random` adapter later).
- Content is supplied from outside through an `IContentSource`-like interface; Core never knows about JSON.

## Domain style
Pragmatic OOP: small mutable objects that own their state and behaviour. No dogma (Elegant Objects rules do not apply); getters are fine, setters are private unless there is a reason.

Planned patterns:
- **Type Object** — `UnitDefinition` (static data) vs `Unit` (instance with HP, XP).
- **Command** — player intents (`MoveLeader`, `HireUnit`, `DismissUnit`, `SwapSlots`, `EndTurn`) validated and executed by the game session. The same commands will be issued by Unity UI.
- **Events** — the session raises domain events (`LeaderMoved`, `UnitHired`, `UnitDamaged`...) so a front-end can animate instead of diffing state.
- **Strategy** — attack reach/targeting (`melee`, `ranged`, `all`) and damage formulas.
- **State** — console screens (`MapScreen`, `CityScreen`, `BattleScreen`) as a screen stack.

Namespaces inside Core are organised by feature: `Map`, `Units`, `Squads`, `Cities`, `Battle`, `Session`.

## Console front-end
Terminal.Gui v2, see ADR 0003.
- `Shell` (root window) holds a stack of `Screen` views; only the top screen is attached and gets keys via `HandleKey`.
- Flow: key → `Shell.OnKeyDown` → `Screen.HandleKey` → Core command → `Screen.Refresh` pushes state into child views and marks them dirty; Terminal.Gui redraws only changed cells.
- Widgets (`Widgets/`): `Canvas` (custom-drawn panel), `SquadView`, `MapView`, `PartyView`, `HintBar`; colors and terrain glyphs in `Palette`.
- Only the console layer maps domain data to glyphs and colors.

## Content
`content/` at repo root holds JSON data (units, maps), copied to output on build. Loaded by `Disciples.Tui` (`Content/ContentLoader`), converted into Core definitions.
