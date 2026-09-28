# disciples-console

Console prototype of the core mechanics of *Disciples II*: global map, leader squads on a 2×3 grid, cities with hiring, turn-based battles.
Game logic is engine-agnostic (.NET Standard 2.1) so it can later be reused with Unity graphics.

## Requirements
- .NET 8 SDK
- Windows Terminal or any terminal with UTF-8 and true color

## Run
```
dotnet run --project src/Disciples.ConsoleApp
```

Terminal of at least 110×30 is recommended.

## Controls
| Screen | Keys |
|---|---|
| Map | arrows / numpad / Home PgUp End PgDn — move, Enter — open city, E — end turn, Esc — quit |
| City | Tab — switch panel, arrows — select, Enter — hire / pick / place unit, D — dismiss, B — capital buildings, Esc — leave |
| Battle | arrows — target, Enter — act, D — defend, A — auto, X — retreat |

Walking into a city opens it; walking into an enemy (`†`) starts a battle.

## Tests
```
dotnet test
```

## Docs
- [Mechanics](docs/MECHANICS.md)
- [Roadmap](docs/ROADMAP.md)
- [Architecture](docs/ARCHITECTURE.md)
- [Decisions](docs/adr/)
