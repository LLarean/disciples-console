# Roadmap

Milestones are sequential. A milestone is done when every checkbox is ticked, `dotnet build` and `dotnet test` pass, and the feature is manually verified in the console.
Mechanic numbers refer to [MECHANICS.md](MECHANICS.md).

## M0 — Scaffold
- [x] Solution: `Disciples.Core` (netstandard2.1), `Disciples.ConsoleApp` (net8.0 + Spectre.Console), `Disciples.Core.Tests` (xUnit) — console app later replaced, see below
- [x] Docs: mechanics catalogue, roadmap, architecture, ADRs, project `CLAUDE.md`
- [x] Screen loop prototype: full-screen layout (map panel, side panel, hint bar), key input, no flicker
- [x] Front-end moved from Spectre.Console to Terminal.Gui (`Disciples.Tui`, ADR 0003)

## M1 — Map and movement
- [x] Terrain, tile, map model; test map loaded from `content/maps/*.json` (1.1, 9.1)
- [x] Leader on map, movement with cost and movement points (1.2, 1.3)
- [x] Map objects: capital, city (1.4) — display only
- [x] Viewport scrolling (1.5)
- [x] Turn counter and end turn (2.1)

## M2 — Units and squad
- [x] Unit definitions loaded from `content/units.json`; unit instances (3.1–3.3)
- [x] Squad 2×3 with leader, large units, leadership limit (4.1–4.3)
- [x] Swap slots (4.4) — in the city screen
- [ ] Squad screen from the map: inspect units
- [x] Gold (2.2); Empire tier-1 units + neutrals (7.1, 7.2) — placeholder stats

## M3 — Cities (test level target)
- [x] Enter capital/city from the map (5.1)
- [x] City screen: garrison + visiting squad (5.2)
- [x] Hire, dismiss, transfer (5.3–5.5)
- [x] Income and healing per turn (2.3, 5.6)
- [x] **Test level:** walk across the map, enter capital and a city, hire/dismiss/arrange units

## M4 — Battle core
- [x] Battle screen with both 2×3 grids and turn queue
- [x] Initiative order, actions, targeting rules, damage/heal formulas (6.2–6.7)
- [ ] Leader death disbands squad (4.9) — for now a fallen leader is revived with 1 HP
- [x] Debug entry: start a battle vs a preset squad — covered by neutral squads; `A` auto-plays, `X` ends the battle

## M5 — Battle on the map
- [x] Neutral squads on the map trigger battle (6.1, 6.9)
- [x] Gold reward for victory (6.8)
- [ ] Experience, unit level-up (6.8, 3.8)
- [ ] Capture a city (5.8)

## M6 — Capital buildings and unit upgrades
- [x] Capital buildings: list by branch, requirements, build for gold (5.10) — placeholder data, no effect yet
- [ ] Empire unit tree, tier upgrades (3.9, 7.4)

## Tech debt
- [ ] Tests on `net8.0` need the .NET 8 runtime next to the .NET 10 SDK; consider moving them to `net10.0` (Core stays netstandard2.1)
- [ ] Splash screen ("Disciples" title) was not ported from the Spectre version
- [ ] Picked unit in the city is shown with the cursor frame, not the pick color, while the cursor stays on it
