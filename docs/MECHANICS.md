# Mechanics Catalogue

Full list of Disciples II mechanics considered for the console prototype.
Each item has a target milestone (see [ROADMAP.md](ROADMAP.md)) or `Backlog` / `Out`.

Markers:
- **(verify)** — exact original rule is not confirmed; check against the original game before implementing.
- Numbers (stats, costs) are approximations of the original and are tuned in `content/` data, not in code.

## 1. Global map

| # | Mechanic | Milestone |
|---|----------|-----------|
| 1.1 | Tile grid with terrain types: plains, forest, hills, mountains, water, road | M1 |
| 1.2 | Terrain movement cost (road cheapest, mountains/water impassable for land units) | M1 |
| 1.3 | Leader movement points per turn, restored on end of turn | M1 |
| 1.4 | Map objects occupying tiles: capital, cities, enemy squads, treasure, ruins | M1 (capital, city), later others |
| 1.5 | Camera/viewport scrolling when map is larger than the screen | M1 |
| 1.6 | Path preview (planned route with cost) | M8 — cheapest route by terrain cost; steps beyond this turn's movement are marked, the destination is kept between turns |
| 1.7 | Fog of war / exploration | M9 (simplified: explored tiles stay fully visible, enemies included; no separate "currently seen" state **(verify)**) |
| 1.8 | Territory ownership and spreading land (rods, capital aura) | M9 (simplified: fixed radius per capital/city from `rules.json`, no spreading over turns, no rods **(verify)**; mines on owned land change hands at turn end) |
| 1.9 | Resource sources: gold mines, mana sources | M8 — gold mines captured by stepping on them, income per turn; M13 — mana sources work like mines: captured by stepping on them or by owning the land, yield their mana every turn |
| 1.10 | Treasure chests, merchants, mercenary camps, trainers | M8 — treasure (one-time gold) and mercenary camps (hire into the party at unit cost); M11 — treasure also holds items, merchants sell a limited stock at item cost to the party standing there, no selling **(verify)**; trainers in M14 |

## 2. Turn structure and economy

| # | Mechanic | Milestone |
|---|----------|-----------|
| 2.1 | Turn counter, end-turn action | M1 |
| 2.2 | Gold as the only resource | M2 |
| 2.3 | Income per turn from capital and owned cities | M3 |
| 2.4 | Four mana types (life, death, infernal, runic) | M13 — a stock per type; the capital yields the mana of its race, sources yield theirs; the enemy has no mana and only denies sources **(verify)** |
| 2.5 | Multiple players / AI turns | M8 — simplified: enemy leaders walk to the nearest non-capital city or the party; M12 — enemy treasury: income from its cities and mines, healing in its cities, recruits for a leader standing in a city, new leaders in its capital up to a limit; leaders take treasure (wearing found equipment) and mines outside the player's land |

## 3. Units

| # | Mechanic | Milestone |
|---|----------|-----------|
| 3.1 | Unit definition (type object): name, HP, armor, initiative, damage, accuracy, attack kind, reach, cost, size | M2 |
| 3.2 | Unit instance: current HP, experience, level, reference to definition | M2 |
| 3.3 | Unit size: small (1 slot) and large (2 slots: front + back of the same column) | M2 |
| 3.4 | Attack reach: melee (adjacent), ranged (any single target), all targets | M2 (data), M4 (behaviour) |
| 3.5 | Attack source: weapon, fire, water, air, earth, mind, life, death | M9 (simplified; content sources **(verify)**) |
| 3.6 | Healers (heal instead of damage) | M4 |
| 3.7 | Immunities and wards per attack source | M9 (immunity: no damage; ward: absorbs the first hit per battle) |
| 3.8 | Experience and level-up within the same tier (stat growth) | M5 — simplified: flat % growth of HP and power, full heal on level-up |
| 3.9 | Upgrade to next tier along the unit tree, gated by capital buildings | M6 — unit waits with capped xp until the building exists |
| 3.10 | Special effects: paralysis, poison, petrification, drain, polymorph, fear | M9 (simplified: one effect per unit for `effectTurns` turns; poison ticks at turn start; paralysis/petrification skip turns, stone takes no damage; drain heals the attacker; no cures, no effect wards **(verify)**); polymorph, fear — Backlog |

## 4. Leader and squad

| # | Mechanic | Milestone |
|---|----------|-----------|
| 4.1 | Squad grid 2×3: front line and back line, 3 columns | M2 |
| 4.2 | Leader is a unit placed in the squad grid | M2 |
| 4.3 | Leadership limits the number of units under the leader **(verify: whether the leader itself counts; large unit counts as 2)** | M2 |
| 4.4 | Swap / move units between slots, respecting large units | M2 |
| 4.5 | Leader classes: warrior, scout, mage, thief — differ in stats and movement | M8 — Paladin, Ranger, Archmage chosen on new game; thief in Backlog |
| 4.6 | Leader progression: level-up picks (leadership, movement, abilities) | M8 — simplified: one pick per level, +1 leadership (up to 6) or +`movementPerk` movement; abilities in Backlog **(verify: starting leadership and pick list)** |
| 4.7 | Leader inventory: artifacts, banners, travel items, potions, scrolls | M11 — simplified: unlimited bag; healing potions drunk on the map; one worn artifact (bonus to the leader) and one banner (bonus to the squad) add armor, power %, initiative, accuracy in battle; no ability requirements **(verify)**; travel items and scrolls in Backlog |
| 4.8 | Several leaders per player | M10 — one party is active; parties cannot share a tile; an attacked party becomes active |
| 4.9 | Squad loses leader → squad is disbanded | M4, M10 — the party is removed with its survivors; the game is lost with the last leader; M8 — enemy squads are disbanded |

## 5. Capital and cities

| # | Mechanic | Milestone |
|---|----------|-----------|
| 5.1 | Enter capital / own city by stepping on its tile | M3 |
| 5.2 | City screen: garrison grid (2×3) + visiting squad grid | M3 |
| 5.3 | Hire tier-1 units for gold (capital: race units; city: none or local list) | M3 |
| 5.4 | Dismiss a unit (no refund) | M3 |
| 5.5 | Transfer units between garrison and visiting squad | M3 |
| 5.6 | Heal units in own city (per turn) | M3 |
| 5.7 | City tiers 1–5 with garrison size and healing rate | M12 — tier adds a garrison slot and `cityTierHealPercent` healing; upgraded for gold (`cityUpgradeCosts`); the capital is always at the top tier; numbers are placeholders **(verify)** |
| 5.8 | Capture neutral/enemy city (battle vs garrison) | M5 — empty garrison is captured on entry |
| 5.9 | Capital guardian (strong unit guarding the capital) | M12 — a unit flagged `guardian` in the garrison: cannot leave it or be dismissed, fully restored every turn, takes a garrison slot; stays dead once slain **(verify)** |
| 5.10 | Capital buildings: unit-tree branches, magic tower, temple | M6; M8 — temple adds `healBonusPercent` to healing in player cities **(verify: original temple heals/resurrects for gold)** |
| 5.11 | Hire leaders in capital | M10 — any leader class for its `cost`; the party appears in the capital, which must have no visitor; leader limit **(verify)** |

## 6. Battle

| # | Mechanic | Milestone |
|---|----------|-----------|
| 6.1 | Battle starts when squads meet on the map | M5 |
| 6.2 | Turn order by initiative with small random spread, re-rolled per round | M4 |
| 6.3 | Actions: attack, defend (armor bonus until next turn), wait (move to end of round, once per round), retreat | M4 — retreat has no penalty |
| 6.4 | Melee targeting: only the enemy's nearest non-empty line; back-line melee attacker can act only if its own front line in that column is empty **(verify: column adjacency rule)** | M4 |
| 6.5 | Ranged: any single enemy; all-targets: every enemy | M4 |
| 6.6 | Hit chance by accuracy, damage with small random spread, armor reduces damage by % | M4 |
| 6.7 | Healing target selection (single / all allies) | M4 |
| 6.8 | Victory/defeat, experience distributed to survivors | M5 — even split **(verify)** |
| 6.9 | Battle vs neutral squads standing on the map | M5 |
| 6.10 | Retreat mechanics (unit leaves battle, squad flees) | M9 (simplified: whole squad flees, party loses the rest of its movement; per-unit escape **(verify)**) |
| 6.11 | Auto-battle | M14 |

## 7. Races and content

| # | Mechanic | Milestone |
|---|----------|-----------|
| 7.1 | Empire: tier-1 units (Squire, Archer, Apprentice, Acolyte) and one leader | M2 |
| 7.2 | Neutral units for enemy squads (incl. at least one large unit) | M2 |
| 7.3 | Legions of the Damned, Mountain Clans, Undead Hordes | M14 (one more race), rest Backlog |
| 7.4 | Full unit trees per race | M6 (Empire), Backlog |

## 8. Magic

| # | Mechanic | Milestone |
|---|----------|-----------|
| 8.1 | Spell research in capital, casting on the map | M13 — research needs the Magic Tower in the capital and costs mana, one spell per turn; a known spell is cast for mana once per turn at an explored tile: damage hits every unit of a hostile squad ignoring armor (immune and warded units are spared), healing restores the player's squad; a squad wiped by a spell gives no gold or experience **(verify)** |
| 8.2 | Summons | Out |

## 9. Meta

| # | Mechanic | Milestone |
|---|----------|-----------|
| 9.1 | Test level loaded from data | M1 |
| 9.2 | Save / load | M7 — 3 slots, JSON snapshot (ADR 0004) |
| 9.3 | Scenario goals (victory conditions) | M7 — fixed: beat all neutrals and enemy leaders, own all cities |
| 9.4 | Main menu | M7 — main and pause menus |
