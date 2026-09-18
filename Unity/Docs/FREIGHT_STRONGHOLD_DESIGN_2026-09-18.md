# Freight Stronghold and the Encounter system — design spec (2026-09-18)

**Status:** design approved section by section in brainstorming; **written spec awaiting owner review**. No code, no `TASKS.md` rows and no `DECISIONS.md` entries exist for this work yet. This document does not change task status; `TASKS.md` alone owns status and `DECISIONS.md` alone holds decisions. Where this spec conflicts with `GAME_DESIGN.md` §"Strongholds" or the progression draft, the amendments in §11 are to be applied when the spec is approved.

Companion visual: the published artifact "Freight Stronghold" (https://claude.ai/artifact/DaVgkVXDhBSamSVvXkt88i).

Verification context: everything below is **code inspection of the port and the reference**. Play Mode was not available; no runtime observation supports any feel, pacing or balance claim in this document.

---

## 1. Purpose

The opening ends with the player defending Home against raids and having no reason to leave. The Freight stronghold is the first thing that pulls them out: a 300-tile corridor north of Home with three guarded key camps, a gated warehouse, a guardian, and a Power core that commissions the first plant. The reference (`gameplayProgress.ts`, `firstRegion.ts`, `gameplayCombat.ts`) hard-codes this as one stronghold with fixed camps; the port has the world sites for it (`FullSites.asset`, 15 `freight:*` records) and nothing that uses them.

Success criteria:
1. A player who has finished the opening is told, in-game, why to go north and where.
2. Every hostile place on the map (key camp, small loot camp, arena, plant squat) is a data row in one system, so later strongholds (Quarry, Wharf) and later content passes are data, not code.
3. Three keys open the warehouse; the guardian falls; the core is carried to Riverside Works; the plant commissions; the campaign visibly moves forward.
4. Kiting the guards from outside their notice range is structurally unrewarding, not merely tuned against.

Non-goals: the campaign ending (owner decision U-Q-02), Quarry and Wharf content, new enemy kinds, new weapons, the Schematics/knowledge track (which Schematics small camps drop is an open owner decision, §10).

---

## 2. The player's journey (approved beats)

| # | Beat | Approved decision |
|---|------|-------------------|
| 1 | Hand-off from the opening | **Pressure-led.** Once the opening's three-turret objective completes, the next major raid is announced as coming from the freight camps ("Raiders from the north — freight district"). Objective: *Find where they're coming from*, with a search area over the West passage camp. |
| 1b | Raid strength coupling | Living encounters sum their `RaidWeight` into `Campaign.RaidPressure`. Major-raid attacker count scales linearly between a **40 % floor** (all freight encounters dead) and **100 %** (all alive). The spawn line and cadence are unchanged. Raids never stop: pressure is permanent, the floor keeps it meaningful. |
| 2 | Discovery | **Layered.** Key camps are `SearchArea`: the objective marks a circle rounded to 20 tiles; the camp resolves to a precise marker when the engineer is within 32 tiles. Small camps are `Proximity`: never pre-marked, resolve at 12 tiles. |
| 3 | Loot | **Physical cache crate.** Each camp holds a `Storage` machine bound to the camp's site; it is opened with the existing inventory panel. Keys stay as marker claims into the quest pouch (unchanged from the reference). |
| 4 | Claiming a key camp | **Occupation.** The key is claimed after the engineer holds within 3 tiles of the marker for 30 s with no living guard of that camp within 20 tiles. Stepping out resets the timer. Building turrets inside the camp ("turret creep") is allowed and is the intended way to hold it under pressure. |
| 5 | Roster | **Escalating.** West passage: skitters, one spitter per 5. Old utility: one spitter per 3 **and dark** — its block lights start unlit, so the patrol's lit-ground rule never helps the player until they light it. Northwood approach: one spitter per 5 plus one **Breaker** (the existing 220 HP row). |
| 6 | Guardian | **Two phases.** At 300 HP (half): windup 1.2 → 0.8 s, recover 2.0 → 1.2 s, and a room-wide alert wakes every body whose `Site` is the arena. |
| 7 | The core | **Two-handed carry.** Picking up the core sets `Engineer.Carrying`; walking only, no sprint, no dash, weapon holstered. It can be dropped (becomes a `Core` site where dropped); death drops it at the engineer's feet. |
| 8 | First plant | **Guided to Riverside Works.** The plant must be *prepared* before the core can be placed: its 8-body squat camp cleared and a repair cost paid. Commissioning costs 30 Steel + 15 Copper and yields 600 kW. |
| 9 | Closure | A one-shot **milestone card** on commissioning ("Riverside Works is lit"); the next major raid after commissioning targets the newest plant. |

**Distances (straight-line, from `FullSites.asset` and the reference plant table):** Home workshop (65,347) → arena (57,40): ~310 tiles, ~55 s at walk 6.0. Arena → Riverside Works plant point (287,417), beside yard:1 (305,394): ~440 tiles, ~75 s at walk with the core (sprint is disabled while carrying). Home → Riverside Works: ~230 tiles. Riverside Works is **not** the yard beside Home (that is Factory yard 0, Founders Court).

---

## 3. Architecture — one Encounter system (Approach 2, approved)

Every hostile place is an **encounter**. One system owns spawning, ticking, discovery, claiming, occupation and death for all of them. A **stronghold** is a named group of encounters plus gates and an arena. A **plant** is a world site with a squat encounter and a commission recipe.

```
Sim/Data/EncounterCatalogue.cs      port-owned defs (the CombatBalance.cs pattern)
  EncounterDef  ── rows: freight:camp:1..3, freight:small:*, freight:arena, plant:riverside:squat
  StrongholdDef ── freight: keys [camp:1..3], gates, arena, guardian, core drop
  PlantDef      ── riverside: site, squat encounter, repair cost, commission recipe, output kW

Sim/Combat/Encounters/
  EncounterState.cs    per-encounter runtime: Discovered, Resolved, Alive count, OccupiedSince, Claimed, CacheId
  EncounterPhase.cs    tick: spawn on discovery, occupation timer, claim, death → RaidPressure
  EncounterQueries.cs  pure reads for UI/objectives (nearest, progress, pressure)
  StrongholdPhase.cs   gates, arena entry, guardian phases, core drop
  Commands/            ClaimKeyCommand (internal), PickUpCoreCommand, DropCoreCommand, PreparePlantCommand, CommissionPlantCommand
```

Rules that hold across the system:
- **Sim-only mutation.** Every state change is a command or a phase tick. The renderer reads queries.
- **Data rows, not branches.** Roster, discovery mode, occupation radius, cache contents, dark-start and the Breaker are all columns on `EncounterDef`. No `if (id == "freight:camp:2")`.
- **`Enemy.Site`.** Enemies get a new `Site` field (string id of the encounter). Squad alert today keys on `Group` only (`EnemyPhase.cs:500-540`); the guardian's room-wide alert and the occupation rule's "no living guard of this camp" both key on `Site`.
- **Layer stays `EnemyLayer.Site`** for all encounter bodies; raid bodies are untouched.
- **Reoccupation lock (kept from the reference):** an encounter whose claim conditions are met while any non-rail machine stands within 20 tiles of its marker cannot be repopulated by fabrication (`FABRICATION.repeatSeconds = 900`, i.e. a cleared camp with nothing built on it refills after 15 min). This is a deliberate outpost-versus-farm choice for the player.

---

## 4. Data

### 4.1 `EncounterDef`

| Column | Type | Meaning |
|---|---|---|
| `Id` | string | `freight:camp:1`, `freight:small:3`, `freight:arena`, `plant:riverside:squat` |
| `Site` | string | `WorldSite.Id` that anchors it (marker, groups, arena rect) |
| `Kind` | enum | `KeyCamp`, `LootCamp`, `Arena`, `Squat` |
| `Discovery` | enum | `SearchArea` (rounded 20, resolve 32) or `Proximity` (resolve 12) |
| `Groups` | (x,y)[] | spawn group centres (from the site's `groups`) |
| `Bodies` | int | total bodies split evenly across groups |
| `SpitterEvery` | int | 0 = none; N = every Nth body is a spitter |
| `Extra` | (kind,count)[] | e.g. `[("breaker",1)]` |
| `DarkStart` | bool | block lights inside the site start unlit |
| `OccupyRadius` / `OccupySeconds` / `GuardRadius` | double/double/double | 3 / 30 / 20 for key camps; 0/0/0 for loot camps (claimed on last kill) |
| `Cache` | (item,count)[] | contents of the cache crate spawned at the marker on resolve |
| `Key` | string? | key id claimed into the quest pouch (`freight:camp:N`); null for loot camps |
| `RaidWeight` | double | contribution to `RaidPressure` while any body lives |
| `RepeatSeconds` | double | 900 for camps; 0 for arena/squat (never refill) |

### 4.2 Freight rows (positions from `FullSites.asset`)

| Id | Name | Marker | Bodies | Groups | Roster | Cache | Notes |
|---|---|---|---|---|---|---|---|
| `freight:camp:1` | West passage | (95,286) | 20 | (72,287)(97,283)(73,266)(100,267) | SpitterEvery 5 | 40 Bullets, 10 Steel | first search area |
| `freight:camp:2` | Old utility | (96,188) | 22 | (72,190)(97,190)(74,168)(100,169) | SpitterEvery 3, DarkStart | 60 Bullets, 6 Wire, 2 Board | dark twist |
| `freight:camp:3` | Northwood approach | (95,103) | 20 | (74,103)(98,103)(74,122)(101,124) | SpitterEvery 5 + 1 Breaker | 60 Bullets, 10 Copper (Schematic slot reserved, §10) | last key |
| `freight:small:1..4` | corridor loot camps | placed on the corridor between camps (exact tiles chosen at implementation from unused `freight:*`/street sites; none may lie within 25 tiles of a key camp marker) | 5–8 | 1–2 | skitters only | ore/plates/bullets 10–30 | `Proximity`, RaidWeight 0.25 |
| `freight:arena` | Warehouse floor | rect (57,40) 28×21 | 60 + guardian | outside (45,35)(45,58)(98,35)(98,61); inside (62,58)(79,46) | `%5==4` spitter; guardian at (77,50) | — | all-or-nothing spawn; 12-tile no-surprise-birth abort |
| `plant:riverside:squat` | Riverside squat | plant point (287,417) | 8 | 2 | skitters | — | `Proximity`, must be cleared to prepare |

`RaidWeight`: key camps 1.0, arena 2.0, plant squat 0.5, small camps 0.25.

Cache contents are starting values inside `OpeningBalance`-style overrides, not catalogue truth; they are the balance knob for the corridor.

### 4.3 `StrongholdDef` — `freight`

- `Keys`: `freight:camp:1..3` (all three required; **three, not "up to four"** — see §11).
- `Gates`: south (65,61) 3×1 and west (56,49) 1×3, as `WorldSiteKind.Gate` sites; both open together when the pouch holds all three keys and the engineer uses either gate.
- `Arena`: `freight:arena`; `Guardian`: existing guardian row (600 HP, 2.7 speed, 25 damage, windup 1.2, recover 2.0; charge 13 t/s for 0.9 s then 2 s recover); `Phase2At = 300`, `Phase2Windup = 0.8`, `Phase2Recover = 1.2`.
- `CoreDrop`: the core item spawns at the guardian's death position as a `Core` site.

### 4.4 `PlantDef` — `riverside`

- `Site`: new `WorldSiteKind.Plant` record "Riverside Works" at (287,417) (from the reference `plants` table; the port's `FullSites.asset` has **no plant records today** and must gain the three).
- `Squat`: `plant:riverside:squat`.
- `RepairCost`: 20 Steel, 10 Concrete (a preparation cost paid from carried inventory at the plant; the number is a starting value).
- `Commission`: 30 Steel + 15 Copper + 1 Power core → 600 kW into the plant's grid island; plant machines join the existing power model as a generator.
- `Prepared` = squat cleared **and** repair paid. Placing the core requires `Prepared`.

Ironworks and Civic Utility get `PlantDef` rows with their sites only; their squats, costs and cores are out of scope.

---

## 5. Systems

### 5.1 Discovery and resolution
- `SearchArea`: on the objective becoming active, `EncounterState.Discovered = true` and the HUD marks a circle centred on the marker rounded to the nearest 20 tiles, radius 20. When the engineer is within 32 tiles, `Resolved = true`: bodies spawn (all-or-nothing, abort and retry next tick if any body would be born within 12 tiles of the engineer), the cache crate is created at the marker, the marker becomes precise.
- `Proximity`: nothing shown until within 12 tiles; then resolve exactly as above.
- Resolution is once per encounter life; a refilled camp (`RepeatSeconds`) re-resolves on next approach.

### 5.2 Occupation and claiming (key camps)
- Each tick while `Resolved && !Claimed`: if the engineer is within `OccupyRadius` of the marker and no enemy with `Site == Id` and `Hp > 0` lies within `GuardRadius`, `OccupiedSince` is set (if unset); otherwise it is cleared.
- When `T − OccupiedSince ≥ OccupySeconds`: claim. The key id enters the quest pouch; `Claimed = true`; a claim event fires for the HUD ("West passage secured — key 1 of 3").
- HUD shows the occupation timer as a ring while inside the radius.
- Loot camps claim on the last kill (no occupation).

### 5.3 Cache crate
- A `Storage` machine created by the sim with a new `Machine.Site` binding so removal/refund rules can exclude it (it is not player-built; it cannot be picked up, only emptied). Opened with the existing inventory panel; drag/transfer rules unchanged.
- The crate persists after the camp is claimed; a refilled camp does not refill the crate.

### 5.4 Gates and arena
- Gates are impassable `Gate` sites until the stronghold opens. Opening is a command issued when the engineer interacts with a gate holding all keys; both gates open; the arena encounter resolves on first entry (12-tile abort rule applies).
- Arena bodies are `Site == freight:arena`. The reference's `inside` rule (squads 4–5 inside the rect) is kept.

### 5.5 Guardian phases
- On damage crossing `Phase2At`: swap windup/recover to phase-2 values; issue a site-wide alert (every living `Site == freight:arena` body gets `OnPlayer` and `LastKnown` = engineer position, `LastKnownUntil = T + MemorySeconds`), fire a `GuardianEnragedEvent` for the renderer (roar/flash).
- Guardian death: fire `GuardianKilledEvent`, spawn the `Core` site at its position, mark the stronghold `Opened`.

### 5.6 Two-handed carry
- `PickUpCoreCommand` (engineer within reach of a `Core` site): removes the site, sets `Engineer.Carrying = "power-core-1"`. While carrying: move speed = walk; sprint and dash inputs ignored; fire ignored; the build tool cannot open (the reason is shown in the HUD).
- `DropCoreCommand`: clears `Carrying`, creates a `Core` site at the engineer's tile. Death: same drop at the death position, before respawn.
- The core never enters the inventory; `Carrying` is a single string field saved with the engineer.

### 5.7 Plant preparation and commissioning
- `PreparePlantCommand` at the plant site: requires the squat encounter claimed and the repair cost in carried inventory; sets `Plant.Prepared`.
- `CommissionPlantCommand`: requires `Prepared`, `Engineer.Carrying == core`, and the commission cost in carried inventory. Consumes all three, sets `Plant.CommissionedAt`, adds the plant's generator to the power model, fires `PlantCommissionedEvent` (the milestone card) and marks the campaign's `NewestPlant`.
- "Never infer stock at Home is carried stock": both commands read the engineer's inventory only.

### 5.8 Raid pressure and targeting
- `RaidPressure = 0.4 + 0.6 × (Σ RaidWeight of encounters with living bodies) / (Σ RaidWeight of all freight encounters)`. Recomputed on encounter resolve and on last-kill; stored, not derived every tick.
- Major raid build (`SiegePlan.Build`) multiplies its attacker count by `RaidPressure`; spawn line, sectors and cadence are unchanged.
- Raid announcement text after the opening names the freight district as the source until the stronghold is `Opened`.
- Target list: Home plus every commissioned plant; the first major raid after a commissioning targets `NewestPlant`, then normal selection resumes.

### 5.9 Objectives
| Order | Objective text | Complete when |
|---|---|---|
| 1 | Find where they're coming from | `freight:camp:1` resolved |
| 2 | Secure the West passage (hold the camp) | key 1 claimed |
| 3 | Push north — Old utility is dark; bring lights | key 2 claimed |
| 4 | Northwood approach — something bigger guards it | key 3 claimed |
| 5 | Open the warehouse | stronghold opened |
| 6 | Bring down the guardian | guardian killed |
| 7 | Carry the core to Riverside Works (walk only — it's heavy) | engineer with core within 8 tiles of the plant |
| 8 | Prepare Riverside Works (clear the squat, pay the repair) | `Prepared` |
| 9 | Commission the plant | `CommissionedAt` set → milestone card |

Objectives reuse the opening's `ObjectiveView` shape; the text lives in the encounter catalogue with the rows it belongs to.

---

## 6. World and site changes

- `WorldSiteKind`: add `Gate`, `Arena`, `Cache`, `Plant`. `Core` already exists.
- `FullSites.asset` is generated; the three plant records and any new small-camp records are added through the city importer (`Editor/World/CityFile.cs`), which already declares `freightGates`, `hasFreightArena`, `freightArena` but consumes none of them. The importer emits `Gate`/`Arena` sites from those fields. **No hand edits to generated assets.**
- Old utility's block lights: the importer tags lights inside the `freight:camp:2` site so `DarkStart` can find them; the light system's existing unlit state is used.

---

## 7. Persistence

- `SaveSchema.Version` 9 → 10. New visited state: `EncounterState` per encounter (Discovered, Resolved, Claimed, OccupiedSince, CacheId, AliveIds), `StrongholdState` (Opened, GuardianPhase2, CoreSiteId), `PlantState` (Prepared, CommissionedAt), `Engineer.Carrying`, `Campaign.RaidPressure`, `Campaign.NewestPlant`, `Enemy.Site`.
- Version-9 saves load with no encounters discovered and `RaidPressure = 1.0`; an engineer mid-corridor in an old save simply discovers camps on approach. No migration of enemy bodies is attempted: v9 site bodies (debug spawns) keep `Site = null` and never count toward occupation.

---

## 8. UI

- HUD: search-area circle; occupation ring with seconds; "carrying" state on the engineer (holstered rifle, slower walk) with a one-line reason when sprint/fire/build is refused; key count in the quest pouch.
- Panels: the cache crate uses the existing inventory panel; the plant uses a small panel showing squat/repair/commission rows with the same `ObjectiveMaterial` have/need layout as the opening.
- Milestone card: a one-shot overlay ("Riverside Works is lit · 600 kW · the north answers"), dismissed by any input, never repeated. It overlays the world and does not move the camera (`uiShell` owns capture).
- Raid banner: source text ("from the freight district") until the stronghold opens.

---

## 9. Testing

Headless (`scratchpad/simtests`, Sim only):
1. Discovery: `SearchArea` marks at 20-rounded centre; resolves at 32; `Proximity` resolves at 12 and never pre-marks.
2. All-or-nothing spawn aborts when a body would land within 12 tiles; retries next tick.
3. Occupation: 30 s inside radius with no living site guard claims; stepping out resets; a living guard 19 tiles away blocks, 21 does not.
4. Loot camp claims on last kill; cache crate exists at the marker and is not removable/refundable.
5. Gates stay impassable with two keys; open with three; arena resolves on entry.
6. Guardian at 299 HP has phase-2 timings and all arena bodies are `OnPlayer`.
7. Carry: sprint/dash/fire/build refused while carrying; drop creates a `Core` site; death drops at the death position.
8. Plant: commission refused unprepared or without carried cost; succeeds with both; generator appears in the power model; `NewestPlant` set.
9. RaidPressure: all alive → 1.0; all dead → 0.4; attacker count scales; spawn line unchanged.
10. Save round-trip at v10; v9 load gives the defaults in §7.
11. `KiteTest` — a **measurement, not an assertion**: a scripted engineer shooting from 18 tiles and retreating; report kills per minute and whether the occupation rule blocks the claim. Recorded as evidence, not pass/fail.

Not verifiable in this session: feel of the 30 s hold, pacing of the corridor, guardian difficulty, milestone card presentation. These are owner playtest items and must be recorded as such, never inferred from headless runs. Debug grants (`Spawn`, `DebugRaidCommand`) may isolate defects but cannot establish progression or balance.

---

## 10. Open decisions (owner)

1. Which Schematic the Northwood approach cache drops (knowledge track); the row ships with a placeholder-free cache of Bullets/Copper until decided.
2. Whether 8–12 further small camps are a later content pass (recommended: yes, after the first playtest).
3. Repair and commission costs are starting values for the owner's playtest.

---

## 11. Documentation amendments on approval

- `GAME_DESIGN.md` §Strongholds: "up to four eligible exterior camps" → exactly three key camps per stronghold; add small loot camps; add occupation, cache crate, carry, plant preparation.
- `docs/Design/RELIGHT_PROGRESSION_AND_WEAPONS_DRAFT.md` line 46 (reference project doc): note superseded by this spec for the port.
- `DECISIONS.md`: entries for the Encounter system (Approach 2), three keys, occupation claim, cache crate, two-handed carry, raid-pressure coupling with the 40 % floor, guardian phases, plant preparation.
- `TASKS.md`: implementation rows from the plan produced by `writing-plans`; GP-W6 is superseded by this direction.
- `TECHNICAL_ARCHITECTURE.md`: `Sim/Combat/Encounters/` and `Sim/Data/EncounterCatalogue.cs`.
