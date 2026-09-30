# disciples-console

Console prototype of the core mechanics of *Disciples II*: global map, leader squads on a 2×3 grid, cities with hiring, turn-based battles.
Game logic is engine-agnostic (.NET Standard 2.1) so it can later be reused with Unity graphics.

## Requirements
- .NET 10 SDK (front-end); .NET 8 runtime for tests
- Windows Terminal or any terminal with UTF-8 and true color

## Run
```
dotnet run --project src/Disciples.Tui
```

Terminal of at least 110×30 is recommended.

## Controls
| Screen | Keys |
|---|---|
| Map | arrows / numpad / Home PgUp End PgDn — move, Enter — open city / camp, S — squad, L — level-up perk, E — end turn, Esc — menu |
| City | Tab — switch panel, arrows — select, Enter — hire / pick / place unit, D — dismiss, B — capital buildings, Esc — cancel / leave |
| Squad | arrows — select, Enter — pick / place unit, S / Esc — back |
| Buildings | arrows — select, Enter — build, B / Esc — back |
| Battle | arrows — target, Enter — act, D — defend, W — wait, A — auto, X — retreat |
| Menus | arrows — select, Enter — confirm, Esc — back |

Walking into a city or a mercenary camp (`▲`) opens it, treasure (`$`) is picked up, a mine (`¤`) starts paying gold each turn; walking into an enemy (`†`) or a guarded hostile city starts a battle. Win by beating all neutral squads and owning every city; lose if the leader dies.

## Tests
```
dotnet test
```

## Docs
- [Mechanics](docs/MECHANICS.md)
- [Roadmap](docs/ROADMAP.md)
- [Architecture](docs/ARCHITECTURE.md)
- [Decisions](docs/adr/)
