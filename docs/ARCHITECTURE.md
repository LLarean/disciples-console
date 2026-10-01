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
| `Disciples.Core.Tests` | net10.0 | Core, xUnit | Rule tests |

## Core rules
- No `System.Console`, no file IO, no `UnityEngine`, no third-party packages.
- C# 9 max: no `record struct`, `required`, file-scoped namespaces, global usings, collection expressions. `record class` and `init` need an `IsExternalInit` shim — avoid unless there is a clear win.
- Randomness only through an injected `IRandom` (seedable in tests, `UnityEngine.Random` adapter later).
- Content is supplied from outside through an `IContentSource`-like interface; Core never knows about JSON.

## Domain style
Pragmatic OOP: small mutable objects that own their state and behaviour. No dogma (Elegant Objects rules do not apply); getters are fine, setters are private unless there is a reason.

Planned patterns:
- **Type Object** — `UnitDefinition` (static data) vs `Unit` (instance with HP, XP).
- **Command facade** — `GameSession` methods (`TryMove`, `EndTurn`, `Hire`, `Dismiss`, `Build`, `UpgradeCity`, `UseItem`, `Equip`, `BuyItem`, `StartBattle`, `FinishBattle`) validate and execute player intents. The same calls will be issued by Unity UI.
- **Events** — the session queues `GameEvent`s (turn started, city captured, level-up, upgrade, victory...) drained by `TakeEvents()`; battles emit `BattleEvent`s. A `GameEvent` carries display names for text and, where it has them, the objects and positions involved (`Party`, `City`, `Site`, `Unit`, `Item`, `From`, `At`); enemy leaders report every step as `EnemyMoved`. A front-end can animate instead of diffing state.
- **Strategy** — attack reach/targeting (`melee`, `ranged`, `all`) and damage formulas.
- **State** — console screens (`MapScreen`, `CityScreen`, `BattleScreen`) as a screen stack.

Namespaces inside Core are organised by feature: `Content`, `Map`, `Units`, `Items`, `Squads`, `Cities`, `Battles`, `Session`, `Persistence`.

### Content and state (ADR 0004)
- `GameContent` — immutable catalog: unit, terrain, building and item definitions plus `GameRules`. Validated on construction.
- `ItemDefinition` — item type object (potion, artifact, banner). Items have no instances: a party's bag and worn items are lists of definitions, saved as ids.
- `StatBonus` — additions to battle stats. `Unit` stats stay item-free; `Party.BonusFor(unit)` sums the worn items and `Battle` takes the lookup as a delegate, so future battle-wide effects (spells) plug in the same way.
- `GameSession` — mutable game state (map, parties, cities, gold, turn, status) and commands. `Parties` holds every player party; `Party` is the active one (`Select`), which movement, battles, perks and camps act on. City hiring goes to the party standing in the city (`PartyAt`).
- `GameSnapshot` — plain DTO of the state; scenarios and saves use the same format. `SnapshotMapper` captures/restores it against a `GameContent`.
- `SnapshotMigrator` — upgrades an older snapshot to the current `Version` one step at a time before it is restored. A format change adds a step; a renamed content id adds an entry to `ContentAliases` (`content/aliases.json`) instead, which `GameContent` resolves on lookup.
- `Progression` — experience, level-up and tier upgrade rules; building checks are passed in as a delegate.
- `Route` — planned party path with running cost (`GameSession.PlanRoute`); the front-end walks it with `TryMove`.
- `Site` — passable map object handled on step: treasure, gold mine, mana source, mercenary camp, merchant (`WorldMap.Sites`). Mines and mana sources (`IsResource`) have an owner and pay it every turn.
- `Mana` — immutable amount of each of the four mana types, used as stock (`GameSession.Mana`), income per turn (`City.Mana`, `Site.Mana`) and price. A treasure's items go to the visiting party; a merchant's items are its stock.
- `City` — owner, garrison, recruits, buildings and tier. The tier sets the garrison capacity and the healing rate from `GameRules`; a snapshot without a tier restores as tier 1, and a garrison larger than its tier allows is kept as is.
- `Party` — leader, squad and movement. Movement and squad capacity derive from the leader definition plus taken `LeaderPerk`s; one perk per leader level above the first.

### Enemy turn
`GameSession.EndTurn` runs the enemy turn before the new turn starts: the treasury (`EnemyGold`) collects income, enemy cities heal their garrison and visiting leader, a leader standing in a city buys recruits, every enemy `Party` (`WorldMap.Enemies`) moves, and a vacant enemy capital hires a new leader (`GameRules.EnemyLeaderClasses`, `EnemyLeaderLimit`). `Pathfinder` (Dijkstra by terrain cost) leads a party to the nearest target: a player party, a non-capital city it does not own, a treasure or a mine outside the player's land; sites it steps on are plundered. Garrison fights are auto-resolved with `SimpleBattleAi`; an attack on a party makes that party active and is exposed as `GameSession.IncomingAttack` for the front-end to fight.

## Console front-end
Terminal.Gui v2, see ADR 0003.
- `Shell` (root window) holds a stack of `Screen` views; only the top screen is attached and gets keys via `HandleKey`.
- Flow: key → `Shell.OnKeyDown` → `Screen.HandleKey` → Core command → `Screen.Refresh` pushes state into child views and marks them dirty; Terminal.Gui redraws only changed cells.
- `GameFlow` owns content, `SaveStore` and top-level transitions: main menu → map → game end.
- Screens: `MainMenuScreen`, `ClassScreen`, `PauseScreen`, `SlotsScreen`, `PerkScreen`, `CampScreen`, `MerchantScreen`, `LeaderScreen`, `ItemsScreen`, `PotionScreen`, `ConfirmScreen` (on `MenuScreen` base), `MapScreen`, `SquadScreen`, `CityScreen`, `BuildingsScreen`, `BattleScreen`, `GameEndScreen`.
- Widgets (`Widgets/`): `Canvas` (custom-drawn panel), `SquadView`, `MapView`, `PartyView`, `HintBar`; colors and terrain glyphs in `Palette`.
- Only the console layer maps domain data to glyphs and colors.

## Content
`content/` at repo root holds JSON data (units, items, terrains, buildings, rules, aliases, maps), copied to output on build. Saves are in `%LOCALAPPDATA%/DisciplesConsole/saves`. Loaded by `Disciples.Tui` (`Content/ContentLoader`), converted into Core definitions.
