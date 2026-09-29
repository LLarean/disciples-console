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
- [x] Squad screen from the map: inspect units, rearrange (`S`)
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
- [x] Leader death (4.9) — simplified: the only party is lost, so the game is lost
- [x] Debug entry: start a battle vs a preset squad — covered by neutral squads; `A` auto-plays, `X` ends the battle

## M5 — Battle on the map
- [x] Neutral squads on the map trigger battle (6.1, 6.9)
- [x] Gold reward for victory (6.8)
- [x] Experience, unit level-up (6.8, 3.8)
- [x] Capture a city (5.8)

## M6 — Capital buildings and unit upgrades
- [x] Capital buildings: list by branch, requirements, build for gold (5.10) — placeholder data, no effect yet
- [x] Empire unit tree, tier upgrades (3.9, 7.4) — placeholder tree and stats

## M7 — Playable skeleton
- [x] Content catalog (`GameContent`) and rules in `content/rules.json` (ADR 0004)
- [x] Scenario and save share one snapshot format (9.1, 9.2)
- [x] Main menu, pause menu, 3 save slots (9.2, 9.4)
- [x] Victory and defeat (9.3): all neutrals beaten and all cities owned / leader dead
- [x] Battle: wait action (6.3)

## Next (candidates)
- [ ] Enemy leader squads with AI turns (2.5)
- [ ] Leader progression and classes (4.5, 4.6)
- [ ] Treasure, mines, merchants on the map (1.9, 1.10)
- [ ] Path preview (1.6)
- [ ] Building effects beyond unit upgrades (thieves guild, magic tower, temple healing)

## Tech debt
- [ ] Tests on `net8.0` need the .NET 8 runtime next to the .NET 10 SDK; consider moving them to `net10.0` (Core stays netstandard2.1)
- [x] Splash screen — replaced by the main menu
- [ ] Picked unit (city, squad screen) is shown with the cursor frame, not the pick color, while the cursor stays on it
- [ ] No confirmation before overwriting a save slot
- [ ] Main menu / Quit from the pause menu drop unsaved progress without asking
- [ ] Snapshot `Version` is written but there is no migration path; content id changes break saves
- [ ] Save regenerates the map legend characters
- [ ] Retreat has no penalty (the party just leaves the battle)
- [ ] Leader death always loses the game (single party); revisit with several leaders (4.8, 4.9)
- [ ] Experience is split evenly among survivors, rounded up **(verify original rule)**
- [ ] Level growth is a flat % of base HP and power; armor, accuracy, initiative don't grow **(verify)**
- [ ] `thieves-guild` and `magic-tower` buildings have no effect
- [ ] Enter on the map opens only owned cities; hostile cities are entered by walking in
- [ ] Squad screen has no dismiss; unit details don't show immunities or attack source (3.5, 3.7)
