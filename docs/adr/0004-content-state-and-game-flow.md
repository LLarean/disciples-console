# ADR 0004: Content/state split, snapshot saves and game flow

**Status:** accepted

## Context
The prototype grew from a single hard-wired test map to a playable loop: main menu, new game, save/load, battles with experience, city capture, unit upgrades.
We need a structure that keeps Core engine-agnostic (ADR 0001) and lets Unity reuse it without touching rules.

## Decision
- **Content vs state.** `GameContent` holds immutable definitions (units, terrains, buildings, `GameRules`); it is loaded once and validated (e.g. every `upgradesTo` exists). Mutable state lives in `GameSession` and its objects (map, party, cities, units). State references content by id only.
- **Scenario = snapshot.** A scenario file (`content/maps/*.json`) and a save use the same `GameSnapshot` DTO. `SnapshotMapper.Capture/Restore` converts between snapshot and session; unknown ids fail with `ContentException`. A snapshot carries `Version` for future migrations.
- **Session as command facade.** Front-ends call `GameSession` commands (`TryMove`, `EndTurn`, `Hire`, `Build`, `StartBattle`, `FinishBattle`...) and read state. Side results are queued as `GameEvent`s and drained with `TakeEvents()`; a battle returns a `BattleReport`.
- **Balance in data.** Tunable numbers (heal %, spreads, level growth, xp thresholds and values) are in `content/rules.json` and `content/units.json`.
- **Front-end flow.** `GameFlow` (Tui) owns content, the `SaveStore` and screen transitions (main menu → map → end screen). Screens receive `GameFlow` only when they need to switch the whole game; others get the `GameSession`.
- **Saves.** `SaveStore` keeps 3 JSON slots in `%LOCALAPPDATA%/DisciplesConsole/saves`, written via temp file + move. It belongs to the front-end: Unity will use its own storage with the same snapshot.

## Consequences
- Saves break when content ids change; there is no migration yet (see ROADMAP tech debt).
- Map legend characters are regenerated on save, so a saved map may differ textually from its scenario.
- Unity integration needs: a content loader (JSON or ScriptableObjects → `GameContent`), a save store, and views that react to `GameEvent`s.
