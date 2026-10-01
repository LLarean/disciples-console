# disciples-console

Console prototype of the core mechanics of *Disciples II*: global map, leader squads on a 2×3 grid, cities with hiring, turn-based battles.
Game logic is engine-agnostic (.NET Standard 2.1) so it can later be reused with Unity graphics.

## Requirements
- .NET 10 SDK
- Windows Terminal or any terminal with UTF-8 and true color

## Run
```
dotnet run --project src/Disciples.Tui
```

Terminal of at least 110×30 is recommended.

## Controls
| Screen | Keys |
|---|---|
| Map | arrows / numpad / Home PgUp End PgDn — move, Enter — open city / camp / merchant / trainer, C — capital, Tab — next leader, T — pick a travel target (Enter — go), G — continue the route, S — squad, I — items, M — spells, R — plant a rod (Archangel), K — thief actions (Thief), L — level-up perk, E — end turn, Esc — menu |
| City | Tab — switch panel, arrows — select, Enter — hire / pick / place unit, D — dismiss, U — upgrade the city tier, B — capital buildings, L — hire a leader (capital), Esc — cancel / leave |
| Squad | arrows — select, Enter — pick / place unit, D — dismiss, S / Esc — back |
| Buildings | arrows — select, Enter — build, B / Esc — back |
| Battle | arrows — target, Enter — act, D — defend, W — wait, A — auto (one action), Q — finish the battle automatically, X — retreat |
| Items | arrows — select, Enter — drink a potion / wear or take off an artifact or banner, Esc — back |
| Spells | arrows — select, Enter — research a spell (needs the Magic Tower in the capital) / cast a known one and pick its target, Esc — back |
| Menus | arrows — select, Enter — confirm, Esc — back |

Walking into a city, a mercenary camp (`▲`), a merchant (`§`) or a trainer (`♦`, sells experience) opens it, treasure (`$`) is picked up, a mine (`¤`) starts paying gold and a mana source (`*`) mana each turn; walking into an enemy (`†`) or a guarded hostile city starts a battle. An Archangel plants rods (`┃`) that claim the land around them, with its mines and mana sources; stepping on a hostile rod breaks it. A Thief, hired once the Thieves Guild stands in the capital, poisons, assassinates or robs a hostile squad next to it. Win by beating all neutral squads and owning every city; lose if the leader dies.

## Tests
```
dotnet test
```

## Docs
- [Mechanics](docs/MECHANICS.md)
- [Roadmap](docs/ROADMAP.md)
- [Architecture](docs/ARCHITECTURE.md)
- [Decisions](docs/adr/)
