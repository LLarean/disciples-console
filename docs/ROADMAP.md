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

## M8 — Opponents and the world
- [x] Enemy leader squads with AI turns (2.5): pathfinding, city capture, auto-resolved garrison battles, attacks on the party
- [x] Leader progression and classes (4.5, 4.6): class choice on new game, leadership/movement perks per level
- [x] Treasure, mines, mercenary camps on the map (1.9, 1.10); merchants wait for items (4.7)
- [x] Path preview (1.6): travel target mode, route with cost, walking along it
- [x] Building effects beyond unit upgrades: temple adds city healing; thieves guild and magic tower wait for thieves and spells

## M9 — Combat depth
- [x] Attack sources, immunities and wards (3.5, 3.7); battles end as a retreat after a round limit
- [x] Status effects: paralysis, poison, petrification, drain (3.10); Haunted Ruins squad on the test map
- [x] Retreat penalty (6.10): the party loses the rest of its movement
- [x] Fog of war (1.7): party and player cities reveal `sightRadius`; explored tiles stay visible and are saved
- [x] Territory (1.8): cities claim land around them, nearest city wins; mines on owned land pass to its owner at turn end

## M10 — Several leaders
- [x] Session holds several player parties, one of them active; commands act on the active party (4.8) — parties block each other's tiles; saves moved to snapshot version 2
- [x] Hire leaders in the capital (5.11); switch between parties on the map — `L` in the capital, `Tab` and `C` (capital) on the map; placeholder cost, no cap on the number of leaders
- [x] Leader death disbands only that party; the game is lost when no leader is left (4.9)

## M11 — Items
- [x] Item definitions in `content/items.json`, leader inventory (4.7) — an unlimited bag saved with the party
- [x] Potions used on units; artifacts and banners with passive bonuses — `I` on the map; one worn artifact (leader) and banner (squad), applied in battle
- [ ] Treasure holds items; merchants sell them (1.10)

## M12 — Cities and opponents
- [ ] City tiers 1–5: garrison size, healing rate, upgrade for gold (5.7)
- [ ] Capital guardian (5.9)
- [ ] Enemy economy: income, hiring, healing; enemy leaders take treasure and mines (2.5)
- [ ] Front-end events: enemy movement step by step, events carry objects and positions instead of names

## M13 — Magic
- [ ] Mana types and mana sources (2.4, 1.9)
- [ ] Spell research in the magic tower, casting on the map (8.1)
- [ ] Rods spread territory (1.8)

## M14 — Remaining classes, effects and races
- [ ] Thief leader with thieves guild actions; leader abilities as level-up picks (4.5, 4.6)
- [ ] Polymorph and fear (3.10); instant auto-resolve of a battle (6.11)
- [ ] Trainers (1.10)
- [ ] Second race with its unit tree and capital buildings (7.3, 7.4)

## Tech debt
- [x] Tests on `net8.0` need the .NET 8 runtime next to the .NET 10 SDK; consider moving them to `net10.0` (Core stays netstandard2.1)
- [x] Splash screen — replaced by the main menu
- [x] Picked unit (city, squad screen) is shown with the cursor frame, not the pick color, while the cursor stays on it
- [x] No confirmation before overwriting a save slot
- [x] Main menu / Quit from the pause menu drop unsaved progress without asking — always asks, saved or not
- [x] Snapshot `Version` is written but there is no migration path; content id changes break saves (M8 renamed `lord` and dropped `maxMovementPoints`) — `SnapshotMigrator` steps and `content/aliases.json`
- [ ] Thief leader class and leader abilities as level-up picks
- [ ] Enemy leaders ignore map sites: they neither take treasure nor capture mines
- [x] Save regenerates the map legend characters — terrains carry a `symbol` in `terrains.json`
- [x] Retreat has no penalty (the party just leaves the battle) — done in M9 (6.10)
- [x] Leader death always loses the game (single party); revisit with several leaders (4.8, 4.9) — done in M10
- [ ] Leaders of one class share a name, so the log cannot tell two Paladins apart; no cap on the number of leaders **(verify)**
- [ ] Items: no leader abilities gate artifacts and banners, potions only heal and work only on the map, the bag is unlimited and is lost with the party **(verify)**; battle cards show power without item bonuses
- [ ] Losing the last leader loses the game even with gold to hire a new one **(verify: the original loses on the capital)**
- [ ] Experience is split evenly among survivors, rounded up **(verify original rule)**
- [ ] Level growth is a flat % of base HP and power; armor, accuracy, initiative don't grow **(verify)**
- [ ] `thieves-guild` and `magic-tower` buildings have no effect
- [ ] Enter on the map opens only owned cities; hostile cities are entered by walking in
- [ ] Enemy leaders have no economy: no income, hiring or healing; the capital is never their target
- [ ] Battles between an enemy leader and a garrison are auto-resolved; only one enemy attacks the party per turn
- [x] Squad screen has no dismiss; unit details don't show immunities or attack source (3.5, 3.7)
