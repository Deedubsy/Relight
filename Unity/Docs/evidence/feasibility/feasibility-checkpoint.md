# Feasibility checkpoint — can the ending, the resources and the raids fit together?

REL-91 (FEAS-01). Paper only. No code was changed, no test was run, no Unity session was opened.
It decides nothing: it proposes, and the owner decides.
Written 2026-09-21 against the working tree at commit `52f6c047`, branch `main`.

**Reviewed 2026-09-22** by the coordinating session, by reading only. Spot-checked against the code: the power comment, the raid head-count rule, the live-district test, the missing Plant site kind, the rubble-to-iron-ore line, the two relabelled salvage patches and the unread raid tuning fields. All hold. The other figures were not re-derived. Two corrections were made: row R16 (U-D-65 allows Home's three starter patches; the mismatch is the two relabelled salvage patches only), and the plants-and-cores work is tracker row **E-02 (REL-94)**. Of the five new owner questions first proposed here, two were genuinely new and were added to REL-71 as **F1-36** and **F1-37**; the other three were folded into existing rows (see the end of this file).

---

## Bottom line

1. **The late game cannot be built on the systems as they stand** — five load-bearing pieces are data or docs with no code behind them.
2. **No power supply exists except the Generator.** Plants, the Turbine hall and the core are numbers nothing reads. Raid tiers key off "plants running"; plants are not even imported.
3. **Nothing moves freight.** The tram is retired, the truck is Phase C. Every route figure below is a distance the engineer walks carrying 40 stacks.
4. **No factory makes anything.** The four yards are paving rectangles. "A bill drawn from all four factories" has nothing to bind to.
5. Ammunition is comfortable (one Assembler covers the hardest tier 2× over); **copper, lighting and body counts are the squeezes.**

---

## How to read the tags

**measured** = read from code or a data file, with the file:line. **estimated** = worked out here, with the method stated. **[REF]** = from the paused TypeScript project in `packages/`, never mixed with Unity figures. Where a number does not exist, this says **not defined**.

Output 1 (ending sizing) lives in its own file and is summarised, not copied: **[`ending-sizing.md`](ending-sizing.md)**.

---

## Output 1 — Ending sizing (summary; full working in `ending-sizing.md`)

| Question | Answer |
|---|---|
| Can the approved supply carry nine surges plus a lit city? | **No.** The approved 2,400 kW (3 plants + Turbine hall) is data the sim never reads (`Sim/Power/PowerNetwork.cs:78-81`). With Generators only, a lit-and-defended city draws an estimated 5,968 kW. |
| Largest winnable final surge | **`2 × Supply − Standing`**. On a worked 20-Generator grid: **6,290 kW**. There is no ceiling — Generators cost 20 Steel + 5 Copper and can be added without limit. |
| How long is the final raid? | **439–478 s (7.3–8.0 min)** at 220 bodies with 8 gun turrets. The 400 s schedule dominates; the 48-body cap barely bites. But the code caps a raid at **120 bodies**, so the 160–220 tier cannot happen. |
| Which districts can be lit? | All nine, if the player runs ~196 big poles (≈784 Steel + 784 Copper, estimated). Five of nine substations have no plant; three of those are stronghold districts. |
| Verdict | **Not winnable without a design change** — supply must exist in the sim, the lighting model must be decided (F1-35), and raids must reach the approved size and be able to end. |

---

## Output 2 — Resource and route table

### 2.1 Where each resource is

What the sim actually sees, after `World/OpeningResourceLayout.Build` runs at boot (**measured**, `Presentation/WorldBootstrap.cs:142`; the layout deletes the three Home salvage patches, relabels Steel→IronOre and Copper→CopperOre, and adds three Home deposits — `OpeningResourceLayout.cs:18-36`).

| Resource | Where | Tile | Units | Nearest tram stop | Gap (tiles) | Source |
|---|---|---|---:|---|---:|---|
| Iron ore | Home (opening) | 84,352 | 7,680 | T1 | 96 | **measured**, `OpeningResourceLayout.cs:34` |
| Copper ore | Home (opening) | 96,352 | 1,200 | T1 | 98 | **measured**, `OpeningResourceLayout.cs:35` |
| Coal | Home (opening) | 96,369 | 700 | T1 | 82 | **measured**, `OpeningResourceLayout.cs:36` |
| Iron ore | Riverside (was Steel) | 273,402 | 3,000 | T2 | 68 | **measured**, `FullSites.asset:199-208` + relabel `:23` |
| Iron ore | Ironworks | 475,57 | 12,000 | T3 | **79** | **measured**, `FullSites.asset:149-158` |
| Copper ore | Ironworks (was Copper) | 475,63 | 3,000 | T3 | 75 | **measured**, `FullSites.asset:209-218` + relabel `:24` |
| **Copper ore** | **Ravenholm Quarry** | 785,91 | **12,000** | T3 | **355** | **measured**, `FullSites.asset:159-168` |
| **Stone** | **Ravenholm Quarry** | 803,91 | **12,000** | T4 | **368** | **measured**, `FullSites.asset:189-198` |
| **Coal** | **Ravenholm Quarry** | 818,91 | **12,000** | T4 | **374** | **measured**, `FullSites.asset:179-188` |
| **Crude** | **East Wharf** | 830,413 | **12,000** | T4 | **168** | **measured**, `FullSites.asset:169-178` |
| Coal | Civic | 777,411 | 3,000 | T4 | 115 | **measured**, `FullSites.asset:219-228` |

Gaps are **estimated** — straight-line Euclidean from the patch centre to the nearest stop centre, ignoring buildings and the river. They reproduce REL-91's three sanity figures (Quarry ≈355, iron ≈80, crude ≈170).

Tram stop centres, **measured** (`FullSites.asset:389-428`): T1 Founders (73,449), T2 Riverside (325,449), T3 Ironworks (433,125), T4 Civic/East Wharf (665,433).

**World totals** (**estimated**, summing the rows above): Iron ore **22,680**, Copper ore **16,200**, Coal **15,700**, Stone **12,000**, Crude **12,000**.

> **Iron may be effectively unlimited.** `OpeningResourceLayout.cs:31` converts *every Rubble tile in the map* to an IronOre patch, and a non-site minable tile yields `RubbleUnitsPerTile = 300` (**measured**, `Sim/World/Ground.cs:246`, `CatalogueData.g.cs:285`). The number of rubble tiles is **not measured** anywhere. If it is large, iron is free and copper is the only scarce metal. Worth measuring before any balance pass — **REL-30**.

> **The live layout does not quite match U-D-65.** U-D-65 says iron ore only near Ironworks and copper ore only at the Quarry, and lets Home keep its three small starter patches. Home is therefore fine. The mismatch is two authored *salvage* patches that `OpeningResourceLayout.cs:23` relabels to ore at boot: Steel at Riverside (273,402; 3,000) becomes iron ore, and Copper at Ironworks (475,63; 3,000) becomes copper ore. U-D-65 was written from the authored data, before the relabel. Whether those two stay ore is an owner question — **F1-36**; **REL-30**, **REL-35**.

### 2.2 How it reaches other bases — today

| Carrier | State | Source |
|---|---|---|
| **Tram** | **Not ported.** "the tram and the cannon are not ported" | **measured**, `Sim/Production/Machines/MachinePhase.cs:12` |
| Tram stops / track | Retired keys the data validator rejects: `track`, `tramstop`, `tram` | **measured**, `Editor/Data/DataValidator.cs:23`; `Sim/Production/Flow/FlowPhase.cs:280` |
| Tram speed / capacity / dwell / headway in Unity | **not defined** — only the baked centreline `tramLength: 1226.5476` exists | **measured**, `World/Generated/Full/FullGeometry.asset:25318` |
| **Truck** | **Phase C, data only.** `TruckStacks` 200, `TruckMul` 3 exist; no movement or cargo code | **measured**, `CatalogueData.g.cs:278,280`; `Sim/Building/Placement.cs:24`, `Sim/World/Ground.cs:196` |
| **Engineer on foot** | The only carrier. 40 stacks, 6 tiles/s | **measured**, `CatalogueData.g.cs:275,278` |

So today a Quarry copper run is **355 tiles each way on foot at 6 tiles/s = 118 s per leg**, carrying 40 stacks × 50 = 2,000 items (**estimated**). Twelve thousand units of Quarry copper is **6 round trips ≈ 24 minutes of pure walking**, before any fighting.

### 2.3 The freight rate the carriers must sustain

Four factories, each running one line (**estimated** from the LIVE recipes — `OpeningBalance.cs:27` makes Steel and Copper 1 ore → 1 plate in 2 s):

| Build | Ore in, per minute | Items/s across the network |
|---|---:|---:|
| 4 yards × 1 Assembler on `bullet-batch` (100 mags/min each) | 80 iron + 40 copper = **120** | **2.0** |
| 4 yards × 1 Assembler Mk2 (200 mags/min each) | 160 iron + 80 copper = **240** | **4.0** |

Against the carriers' capacity:

| Carrier | Throughput | Verdict against 2.0–4.0 items/s |
|---|---:|---|
| **[REF]** tram, 200-item cap, 40 tiles/s, 1.5 s dwell, one vehicle | round trip 1,208.55 t ÷ 40 × 2 + 8 × 1.5 = **72.4 s** → 200 ÷ 72.4 = **2.8 items/s** | **marginal.** Enough at Mk1, short at Mk2 |
| **[REF]** tram with the 1.25 freight upgrade (cap 250) | **3.5 items/s** | still short at Mk2 |
| **[REF]** tram at the non-city fallback speed of 8 tiles/s | **0.64 items/s** | **fails by 3–6×** |
| **[REF]** truck, 200 stacks = 10,000 items, 18 tiles/s | Quarry round trip 710 t ÷ 18 = 39 s → **256 items/s** | **enormously over-provisioned** |

All **[REF] measured** (`packages/sim/src/flow.ts:244`, `city/riverfront.ts:21-22`, `truck.ts:10`, `engineer.ts:17-18`) and the throughput figures **estimated** from them. **None of it exists in Unity.**

Two things fall out for **F1-21** and **REL-35**:

* At the riverfront tram speed of 40 tiles/s, one 200-item tram is only just enough for four Mk1 lines and not enough for Mk2. If the tram is the answer, its capacity or its vehicle count needs a number.
* A 200-stack truck carries 10,000 items — five times the entire Quarry copper patch in two trips. That is not a logistics puzzle, it is a fetch quest. If the truck is the answer to the Quarry's 355 tiles, its capacity probably wants to come down.

---

## Output 3 — What each factory makes (a proposal, not a decision)

**Measured position first: nothing.** Yard ids and names are generated generically (`Editor/World/RegionImporter.cs:509`), and the only consumer draws them as paved ground (`Presentation/City/CityPresenter.cs:335`). No production, recipe or output is attached to a yard anywhere. No machine is pre-placed in any yard. And the Foundry, Assembler and Assembler Mk2 ship with **no recipe** (U-D-53, **measured** `Sim/Data/OpeningBalance.cs:17-21`).

So `GAME_DESIGN.md:573`'s "a bill of materials drawn from all four factories" has nothing to bind to. **F1-02.**

### A proposal, sized by what is actually near each yard

Yard rects **measured** (`FullSites.asset:429-468`); district names **measured** (`FullSites.asset:469-558`). The proposed outputs are **the assistant's proposal for F1-02** and are not approved.

| Yard | District | What is near it | **Proposed output** | Why this one |
|---|---|---|---|---|
| `yard:0` 83,350 30×29 | Founders Court (Home) | opening iron 7,680, copper 1,200, coal 700 | **Steel plate and Bullets** | It is already the opening's chain; nothing else is in reach |
| `yard:1` 305,394 32×25 | Riverside Works | iron 3,000 at 68 t | **Frames and Boards** (2 Steel → 1 Frame; 1 Steel + 3 Wire → 1 Board) | Iron only; Frame and Board are the only iron-and-copper intermediates that exist |
| `yard:2` 424,57 36×24 | Ironworks | iron 12,000 at 79 t, copper 3,000 at 75 t | **Steel plate and Wire, at volume** | The largest iron patch and the only yard with both ores beside it |
| `yard:3` 731,390 32×26 | Civic Utility | crude 12,000 at 168 t, coal 3,000 at 115 t | **Fuel and Polymer** (Refinery) | The only crude in the world, and Fuel is what keeps Generators running |
| *(no yard)* | Ravenholm Quarry | copper 12,000, coal 12,000, stone 12,000 | **Concrete**, plus the world's bulk copper | U-D-65's unique-resource site, 355 t from any stop — **F1-21** |

Every recipe named above already exists and is **measured** (`CatalogueData.g.cs:158-186`, with the LIVE overrides at `OpeningBalance.cs:27-31`).

### Does a Master Switch bill of this shape fit?

An **illustrative** bill, used only to test affordability. The real bill is **F1-01**'s to set.

Nine stages × (100 Steel + 20 Frame + 10 Board + 5 Polymer + 20 Concrete):

| Total | Ore it costs | Against world stock | Share |
|---|---:|---:|---:|
| 900 Steel + 180 Frame (360 Steel) + 90 Board (90 Steel) = **1,350 Steel** | 1,350 Iron ore | 22,680 | 6 % |
| 270 Wire for the Boards = **135 Copper** | 135 Copper ore | 16,200 | <1 % |
| 45 Polymer | 90 Crude | 12,000 | <1 % |
| 180 Concrete | 360 Stone | 12,000 | 3 % |

**estimated** by arithmetic from the measured recipes. **A bill of this order is comfortably affordable.** The constraint on the Master Switch is not materials — it is that the materials come from four places the player must reach and hold, and that nothing carries them there yet.

---

## Output 4 — Ammunition demand against supply, per raid tier

### 4.1 Demand

Measured inputs: Skitter 20 HP, Spitter 50 HP, Breaker 220 HP (`Enemy - Skitter.asset:21-28`, `Enemy - Spitter.asset:21-28`, `Sim/Data/CombatBalance.cs:45-55`). Gun turret 10 damage per round, never misses once aimed (`Sim/Combat/Turrets/TurretPhase.cs:100-109`). One Magazine item = one round (`Ammo - Bullets.asset:21-25`). Composition 2:1 Skitter:Spitter (U-D-64), Breakers 5 % from wave 2 (`SiegeTuning.cs:107`).

| Tier (U-D-64 f) | Bodies | Total HP | **Rounds** | Batches | Steel | Copper |
|---|---:|---:|---:|---:|---:|---:|
| 0 plants | 60–90 | 1,800–3,470 | **180–347** | 18–35 | 36–70 | 18–35 |
| 1–2 plants | 90–140 | 3,470–5,520 | **347–552** | 35–56 | 70–112 | 35–56 |
| **code ceiling today** | **120** | 4,740 | **474** | 48 | 96 | 48 |
| 3+ plants | 160–220 | 6,330–8,700 | **633–870** | 64–87 | 128–174 | 64–87 |

All **estimated** by arithmetic. The 60-body row reproduces `SiegeTuning.cs:60-65` and the 120-body row reproduces `ENEMY-THREAT-AUDIT.md:412-420`.

Add minor raids: 6–10 bodies every 300–420 s (**measured**, `Tuning - Raids.asset:32-35`), ≈ 4 per 18–22 min cycle, ≈ 25 rounds each → **≈100 rounds per cycle** (**estimated**).

### 4.2 Supply

Measured: `bullet-batch` = 2 Steel + 1 Copper → 10 Magazines in 6 s in an Assembler; Assembler Mk2 has `speedMul 2` (`CatalogueData.g.cs:174-177, 67`). Hand batch is 12 s after the override (`OpeningBalance.cs:28`). Cycle is 1,080–1,320 s (`Tuning - Raids.asset:23-24`).

| Source | Rounds/min | **Rounds per 18–22 min cycle** | Covers the 870-round worst tier? |
|---|---:|---:|---|
| 1 Assembler on `bullet-batch` | 100 | **1,800–2,200** | **yes, 2.1–2.5×** |
| 1 Assembler Mk2 | 200 | 3,600–4,400 | yes, 4–5× |
| Hand crafting at the workshop | 50 | only while the player stands there | no |

**Production throughput is not the constraint.** One Assembler, fed, covers every tier with margin.

### 4.3 What actually constrains it — delivery

| Fact | Value | Source |
|---|---|---|
| Gun turret hopper | **50 rounds** | **measured**, `Turret - Gun turret.asset:21-31` |
| 8 turrets fully loaded | 400 rounds | **estimated** |
| A 220-body raid needs | 870 rounds | **estimated** (§4.1) |
| So mid-fight resupply must deliver | **470 rounds over ~440 s ≈ 1.1 rounds/s** | **estimated** |
| Inserter | 1 item/s → one hopper refill in 50 s | **measured**, `CatalogueData.g.cs:94` |
| Belt / Fast belt | 7.5 / 15 items/s | **measured**, `CatalogueData.g.cs:86,90` |
| Turret at throttle 0.5 | fires at half rate | **measured**, `TurretPhase.cs:118-122` |

One inserter per turret is enough. The real risk is the **power** interaction: during a Master Switch hold at throttle 0.5, 8 turrets deliver 40 dps instead of 80, so the same raid takes twice as long to clear (see `ending-sizing.md` §3.3).

Whole-campaign copper for ammunition, **estimated** at 12 major raids averaging 500 rounds: 6,000 rounds = 600 Copper out of 16,200. **Ammunition is not what makes copper scarce — lighting is** (5,800–9,700 Copper for a city of arc lamps).

---

## Output 5 — Rebuild-risk register

One row per assumption that could force an earlier system to be rebuilt. **Every row names the issue that must honour it.** REL-91 asks that each of those issues' descriptions also say so; that is a Linear write and was outside this run's scope (see *Acceptance items this file cannot satisfy*).

### 5.1 The rows REL-91 named

| # | Assumption at risk | What breaks | Decision | **Issues that must honour it** |
|---:|---|---|---|---|
| R1 | 65 roamers + 12 camps at ~20 + 3 strongholds at 40–60 fit under the 240 world cap | They do not: **533 bodies before any raid** (§5.3). Late raids get starved of bodies, or saves hold far more records than measured | **F1-27** | **REL-42**, **REL-38**, **REL-40**, **REL-64** |
| R2 | A save made today keeps loading as systems are added | Today any scene edit orphans every save. Every new saved field below depends on the answer | **F1-18**, **F1-31** | **REL-63** |
| R3 | The director's new state can be added to the save later | Per-plant nomination, most-plants-ever-running, raid type and raid outcome are all new fields with no defaults | **F1-31** | **REL-38**, **REL-44**, **REL-76** |
| R4 | Lit districts are saved | They are not. `LightState.LiveDistricts` is a transient HashSet cleared on load (**measured**, `Sim/Campaign/Light/LightState.cs:36,45-61`, ":6 It saves nothing"). The ending counts them | — | **REL-46** |
| R5 | One district query serves the ending, roamers, the light board and the raid account | There is no shared query today; each caller would grow its own | — | **REL-81** (INT-09a) |
| R6 | The tram can carry the rate in Output 2 | One 200-item tram gives 2.8 items/s against 2.0–4.0 needed — marginal at Mk1, short at Mk2. And it is not ported at all | **F1-21** | **REL-35** |
| R7 | Navigation on the full map affords 240 moving bodies | **Not measured.** 345,449 walkable tiles (`ENEMY-THREAT-AUDIT.md:258`), plus U-D-59's lit-tile step cost of 4 on every path query | — | **REL-32**, **REL-64** |

### 5.2 Rows this checkpoint adds

| # | Assumption at risk | What breaks | **Issues that must honour it** |
|---:|---|---|---|
| R8 | Plants, the Turbine hall and the core supply power | They do not. `PowerNetwork.cs:78-81`: "No turbine hall or plant supply … and no core/radio/encounter demand". The entire approved 2,400 kW is data nothing reads | **REL-47**, **REL-30**, **REL-94** (E-02, plants and cores) |
| R9 | Plants exist in the world | They are not imported. `SiteKind` has no Plant (**measured**, `Sim/World/WorldSites.cs:7-27`); `RegionImporter` never reads `plants` (schema field at `Sim/World/CityFile.cs:176`). U-D-64 (f) tiers raids by "most plants ever running" — there is nothing to count | **REL-38**, **REL-94** (E-02) |
| R10 | "Live district" means lit | It means **throttle > 0** (**measured**, `Sim/Campaign/Light/LightPhase.cs:68`). A district with one powered lamp is "live". The ending's completion test cannot use this as it stands | **REL-46**, **F1-35** |
| R11 | The player can light every street tile | Street lights are **not a buildable machine** — 26 machines, none is one (**measured**, `CatalogueData.g.cs:42-154`); the 18 that exist are authored sites (`Sim/Campaign/Light/StreetLights.cs:25-39`). Arc lamps cost 17.5–29 MW and 5,800–9,700 Copper for the city | **REL-46**, **F1-35 (1)** |
| R12 | A raid can reach 160–220 bodies | With today's tuning values it tops out at **120**: `total = max(waves, round(60 × growth))`, `growth = min(2.0, …)` (**measured**, `Sim/Combat/Director/SiegePlan.cs:41-43`). And the siege shape is C# constants with no asset to tune | **REL-38**, **REL-43** |
| R13 | A raid ends | It ends only when **every body dies** — `Committed && Remaining == 0 && GroupAlive == 0` (**measured**, `DirectorPhase.cs:34-44`). `EndsAt` cancels the unspawned remainder only (`:213`). The audit observed the director frozen for 9,063 s | **REL-44**, **F1-30** |
| R14 | Raid tuning in the asset takes effect | Seven exported fields are read by no sim code (re-checked by search, 2026-09-22): `windowS`, `spawnEveryS`, `majorSectors`, `majorSkitters`, `minorAfterMajorS`, `breakerStructureMul`, `assaultHistory` (**measured**, cross-checked against `DirectorPhase`/`SiegePlan`). Tuning them changes nothing | **REL-43** |
| R15 | Camps, strongholds and roamers exist | Camps: 3 authored with garrisons of 20/22/20, and **nothing populates them** (`FullSites.asset:229,279,329`; `ENEMY-THREAT-AUDIT.md:152`). Stronghold garrisons and roamers are docs only (U-P-15, U-P-16). REL-91's "12 camps" does not exist | **REL-42**, **REL-40**, **REL-93**, **REL-96** |
| R16 | The live resource layout matches U-D-65 | Not quite. Home's three starter patches are allowed. But two salvage patches are relabelled to ore at boot, so iron ore is also at Riverside (3,000) and copper ore also at Ironworks (3,000), against U-D-65's "only" (§2.1). Owner question **F1-36** | **REL-30**, **REL-35** |
| R17 | Iron ore is a scarce resource | Every rubble tile converts to IronOre at 300 units/tile (**measured**, `OpeningResourceLayout.cs:31`, `Ground.cs:246`). The rubble-tile count is **not measured**. If large, iron is free and only copper is scarce | **REL-30** |

### 5.3 Body budget against the two caps

| Source | Count | Status |
|---|---:|---|
| Roamers, ~1 per 2,500 street tiles × 164,269 street tiles | **66** | **estimated** from U-P-15 (`DECISIONS.md:176`) and the audit's street-tile total |
| Camps — REL-91's figure of 12 at ~20 | **240** | **estimated** from REL-91's own wording; **not in data** |
| Camps — what is actually authored (3, at 20/22/20) | 62 | **measured**, `FullSites.asset:229,279,329` |
| Stronghold garrisons, 3 at 40–60 | **120–180** | **docs only**, U-P-16 (`DECISIONS.md:177`) |
| Active major raid | **48** | **measured**, `Tuning - Raids.asset:30` |

| Scenario | Total | Against the 240 world cap (`Tuning - Raids.asset:31`) |
|---|---:|---|
| Spec as written (66 + 240 + 180 + 48) | **534** | **over by 294** |
| Spec, low end (66 + 240 + 120 + 48) | 474 | over by 234 |
| The audit's own reduced proposal (54 + 62 + 60 + 48) | 224 | fits — `ENEMY-THREAT-AUDIT.md:344` |
| What exists today (0 + 0 spawned + 0 + 48) | 48 | fits; the world is empty |

**The written counts blow the cap by about 2×.** Only the audit's reduced set fits, and that set is a proposal. **F1-27** decides whether sleepers count toward the cap, which changes the answer entirely.

### 5.4 State each later system must save, with a default

REL-91's row R3 and the TASKS FEAS-01 row both ask for this list. Save schema is at **Version 9** (**measured**, `Sim/Persistence/SaveSchema.cs:85`).

| Field | Owner system | Proposed default if missing |
|---|---|---|
| Lit / live district ids | ending, light board | recompute on load from circuit throttle (today it is *cleared* — `LightState.cs:60`) |
| Master Switch stage progress | ending | 0 stages thrown |
| Per-plant nomination (next target) | director, U-D-64 (e) | the newest plant, recomputed |
| Most plants ever running | raid tier, U-D-64 (f) | the current running count |
| Raid type | REL-76 | `rush` |
| Raid outcome (repelled / lost / broke off) | REL-44, F1-30 | `broke off` (a neutral third value) |
| Roamer packs — position, sleeping | REL-42 | recompute; refill 1 per 120 s |
| Camp garrison remaining | REL-40, REL-93 | the authored `Amount` |
| Stronghold state — guardian HP, cleared | REL-40 | uncleared |
| Assaults completed (growth) | director | 0 |

All defaults are **proposals**, and all of them depend on **F1-31** being answered "old saves load and continue".

---

## Open questions for the owner

Each is a question with a recommendation. None is a decision, and none of this is approval.

### Already open in REL-71 — this checkpoint sharpens them

| # | Question | What this checkpoint found | Recommendation |
|---|---|---|---|
| **F1-01** | Master Switch: site, bill of materials, surge size? | A bill of ~1,350 Steel / 135 Copper / 90 Crude / 360 Stone is under 6 % of world stock — materials are not the constraint. Surge size has no ceiling: `2 × Supply − Standing`, and four extra Generators buy 2,400 kW of headroom | Set the surge against **throttle = 1**, not 0.5, so it is a real build-out and does not halve the turrets during the raid it declares. Site: somewhere that forces the player to defend ground they do not already hold |
| **F1-02** | What do the four factories each make? | Nothing. Yards are paving rectangles with no production semantics | Output 3's table: Home = Steel + Bullets, Riverside = Frames + Boards, Ironworks = Steel + Wire at volume, Civic = Fuel + Polymer, Quarry = Concrete + bulk Copper |
| **F1-04** | Is the final raid always Home? | If declared at the first throw it overlaps stages 5–9; at the last throw it lands entirely after the switch-on | Declare at the **last** throw, so the nine stages are a build-up and the raid is the finale, not a distraction |
| **F1-21** | How do the Quarry's resources reach the tram? | 355 tiles. On foot that is 6 round trips ≈ 24 min for the copper alone. A 200-stack truck would clear it in two trips — over-provisioned by 5× | A fifth stop or a shorter truck. If the truck, cut its capacity well below 200 stacks |
| **F1-22** | How long is a large raid meant to last? | 439–478 s at 220 bodies with 8 turrets; the 400 s schedule dominates, not the body count. The design says 2–3 min | If 2–3 min is wanted, cut `MajorWaveGapS` from 110 s, not the head count — the head count is what makes it feel large |
| **F1-27** | Does the 240 cap count sleeping roamers and garrisons? | The written counts total 534 against a 240 cap. Only the audit's reduced set (224) fits | Count awake bodies only; sleepers are records that do not tick — and then measure the record cost (**REL-64**) |
| **F1-30** | What counts as a raid that cannot finish? | Directly load-bearing: a raid ends only when every body dies, and a defence below ~22 dps never finishes one | A neutral third outcome, "broke off": no credit, full interval follows |
| **F1-31** | Must a save made today keep loading? | Ten new saved fields are listed in §5.4, every one of them blocked on this | Old saves load and continue, with neutral defaults — the list in §5.4 is short enough to be cheap |
| **F1-35 (1)** | What does "fully lit on every street tile" mean? | **The single biggest feasibility question in this document.** Arc lamps: 17.5–29 MW and 36–60 % of all the world's copper. Street lights: 2.1–3.6 MW but the player cannot build them | Have the Master Switch **stamp** the district's light mask by rule at zero standing kW, and make the surge the cost. Otherwise the ending is an order of magnitude larger than anything so far discussed |

### New rows — what was written to REL-71 on 2026-09-22

The first draft proposed five new rows. On review, two were genuinely new owner questions and were added. Three were not new questions and were folded in instead.

| Row | Question | Recommendation (not approved) |
|---|---|---|
| **F1-36** (added) | Two salvage patches are relabelled to ore at boot: iron ore appears at Riverside (3,000) and copper ore at Ironworks (3,000). U-D-65 says iron ore only near Ironworks and copper ore only at the Quarry. Do they stay ore? | Keep the Riverside iron (it is small and sits by a tram stop). Decide the Ironworks copper with F1-21, because it is the only copper that is not 355 tiles from a stop |
| **F1-37** (added) | Every rubble tile becomes iron ore at 300 units. Is iron meant to be scarce? The rubble-tile count is not measured | Measure the count first (**REL-30**), then decide. If iron is free, copper is the whole economy and should be designed as such |

| First proposed | Where it went | Why |
|---|---|---|
| Should plants, the Turbine hall and the core supply power? | **Not a question.** It is planned work: tracker row **E-02 (REL-94)**. Recorded as risk rows R8 and R9 | The code comment at `PowerNetwork.cs:78-81` already says the supply arrives with campaign state |
| Should a Master Switch stage hold at throttle 1 rather than 0.5? | Folded into **F1-01** as evidence | 0.5 is provisional value U-P-13, and F1-01 already asks for the surge size |
| Does "live district" mean powered or lit? | Folded into **F1-35 (1)** as evidence | F1-35 (1) already asks what "fully lit" means |

---

## What this does not do

It does not prove balance, and nothing here is an approval. No Unity session was run, no test was executed, and no figure has been seen in a running game. The endgame base load, lamp packing factors, defence layouts and campaign length are **assumptions** about a base nobody has built. There is no owner acceptance verdict on any of it.

## Where this is recorded

* The `Unity/Docs/TASKS.md` log entry of 2026-09-22 points at this file, and the FEAS-01 row carries the pointer.
* Each issue named in the risk register carries a "FEAS-01 checkpoint" note in its Linear description, naming its rows.
* REL-71 lists every open question above with its recommendation; F1-36 and F1-37 are new there.

---

*Companion document: [`ending-sizing.md`](ending-sizing.md) — REL-48, the full arithmetic behind Output 1.*
