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

## Tests
```
dotnet test
```

## Docs
- [Mechanics](docs/MECHANICS.md)
- [Roadmap](docs/ROADMAP.md)
- [Architecture](docs/ARCHITECTURE.md)
- [Decisions](docs/adr/)
