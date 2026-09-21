# Ending sizing — can the game be won, on paper?

REL-48 (END-03). Paper only. No code was changed, no test was run, no Unity session was opened.
Written 2026-09-21 against the working tree at commit `52f6c047`, branch `main`.

**Reviewed 2026-09-22** by the coordinating session, by reading only. Spot-checked against the code: the power comment (`PowerNetwork.cs:78-81`), the raid head-count rule (`SiegePlan.cs:41-43`), the live-district test (`LightPhase.cs:68`), the missing Plant site kind (`WorldSites.cs:7-27`) and the absence of a street-light machine in the catalogue. All five hold. The other figures were not re-derived. One correction: the plants-and-cores work already has a tracker row, **E-02 (REL-94)**.

---

## Bottom line

1. **Not winnable without a design change** — but the change is small and already half-written.
2. The approved supply (3 plants + Turbine hall = 2,400 kW) **does not exist in the sim at all**. Only the 300 kW Generator supplies power today.
3. "Fully lit on every street tile" costs **17.5–29 MW** if built from Arc lamps, against 2,400 kW approved. Street lights (2 kW) would cost 2.1–3.6 MW — but street lights are **not a thing the player can build**.
4. Largest ninth surge that holds throttle ≥ 0.5 is **`2 × Supply − Standing`**. There is no ceiling: Generators cost 20 Steel + 5 Copper and can be added without limit, so the surge rule as drafted gates nothing.
5. A 220-body final raid runs **439–478 s (7.3–8.0 min)** with 8 gun turrets in contact — but the code caps a raid at **120 bodies**, so U-D-64 (f)'s 160–220 tier cannot happen yet.

---

## How to read the tags

| Tag | Means |
|---|---|
| **measured** | read from code or a data file; the file:line is given |
| **estimated** | worked out here from measured inputs; the method is stated |
| **approved** | an owner decision (U-D-nn) or the design doc |
| **provisional** | a U-P-nn placeholder the assistant set; not owner-approved |
| **assumed** | neither; named here with the question that settles it |

Reference figures from the paused TypeScript project in `packages/` are labelled **[REF]** and are never mixed with Unity figures.

---

## 1. What the ending asks for

| Item | Value | Status | Source |
|---|---|---|---|
| Campaign complete when every lighting district is live through the Master Switch and the last raid is repelled | — | **approved** | U-D-60, `Unity/Docs/DECISIONS.md:82`; `Unity/Docs/GAME_DESIGN.md` §11.1 |
| Number of lighting districts | **9** | **measured** | 9 substation sites, `Unity/Relight/Assets/Relight/World/Generated/Full/FullSites.asset:29-118` |
| District = tiles nearest one substation site | — | **approved** | U-D-60; `Unity/Docs/ALWAYS_DARK_SPEC.md` §6 |
| Stage holds after 60 s at throttle ≥ 0.5 | 60 s / 0.5 | **provisional** | U-P-13, `Unity/Docs/DECISIONS.md:174` |
| Master Switch site | — | **not defined** | `GAME_DESIGN.md:573` "The site and the amounts are not set". Settled by **F1-01** |
| Master Switch bill of materials | — | **not defined** | same. Settled by **F1-01** |
| Surge size per stage | — | **not defined** | U-P-13 says "not set at all". Settled by **F1-01** |
| Final raid's target | — | **not defined** | Settled by **F1-04** |

---

## 2. (a) Can the approved supply carry nine surges plus a lit city?

### 2.1 The throttle rule, measured

```
c.Load     = min(c.Supply, c.Demand)
c.Throttle = c.Supply > 0 ? min(1, c.Supply / max(1, c.Demand)) : 0
```
**measured** — `Unity/Relight/Assets/Relight/Sim/Power/PowerNetwork.cs:353-354`.

Rearranged, for a stage to hold at throttle ≥ 0.5:

> **Surge ≤ (2 × Supply) − Standing**

And for the defence to keep firing at full rate (turret cooldown recovers at circuit throttle — **measured**, `Sim/Combat/Turrets/TurretPhase.cs:77-80, 118-122`):

> **Surge ≤ Supply − Standing**

There is **no line capacity and no resistance** in the model — a circuit is a connected component and every node on it shares one throttle (**measured**, `PowerNetwork.cs:353-358`). So "a district on a thin line browns out" has no mechanism today.

### 2.2 Supply

| Source | kW each | Status | Source | In the sim? |
|---|---:|---|---|---|
| Generator (coal/fuel) | **300** | **approved** | `Data/Generated/Tuning/Tuning - Power.asset:22` | **yes** |
| Commissioned plant core | **600** | **approved** | `Tuning - Power.asset:25`; `GAME_DESIGN.md:392` | **no** |
| Restored Turbine hall | **600** | **approved** | `Tuning - Power.asset:26` | **no** |
| Home core | 100 (demand) | **approved** | `Tuning - Power.asset:27` | **no** |
| Radio | 20 (demand) | **approved** | `Tuning - Power.asset:28` | **no** |

> **The single biggest finding in this document.** `PowerNetwork.cs:78-81` states plainly: "No turbine hall or plant supply (:49-50) and no core/radio/encounter demand (:52-55)." **measured**. The whole approved fixed supply of 2,400 kW is data that nothing reads. Plants are not even imported into Unity — `SiteKind` has no Plant member (**measured**, `Sim/World/WorldSites.cs:7-27`) and `RegionImporter` never reads the city file's `plants` array (**measured**, `Editor/World/RegionImporter.cs`, cf. schema field `Sim/World/CityFile.cs:176`).

Approved fixed supply, if it were implemented: 3 × 600 + 600 = **2,400 kW** (**estimated** from the approved per-unit values; the plant count of 3 is **[REF]** `packages/sim/src/progression.ts:17` and is not in Unity).

### 2.3 Standing demand of a lit city

Street tiles per district — **measured (audit, offline from the scene; not re-derived here)**, `ENEMY-THREAT-AUDIT.md:287-297`:

| District | Substation | Street tiles | Arc lamps @113 tiles | kW | Street lights @154 tiles | kW |
|---|---|---:|---:|---:|---:|---:|
| Founders Court | 0 (78,347) | 15,206 | 135 | 1,620 | 99 | 198 |
| Riverside Works | 1 (304,424) | 23,505 | 208 | 2,496 | 153 | 306 |
| Ironworks | 2 (416,94) | 28,516 | 253 | 3,036 | 185 | 371 |
| Civic Utility | 3 (720,420) | 26,598 | 236 | 2,832 | 173 | 346 |
| Westridge Homes | 4 (115,176) | 12,436 | 110 | 1,320 | 81 | 162 |
| Old Town | 5 (278,178) | 16,687 | 148 | 1,776 | 108 | 217 |
| Northwood Freight | 6 (98,59) | 9,018 | 80 | 960 | 59 | 117 |
| Ravenholm Quarry | 7 (833,91) | 23,563 | 209 | 2,508 | 153 | 306 |
| East Wharf | 8 (834,408) | 8,740 | 78 | 936 | 57 | 114 |
| **Total** | | **164,269** | **1,457** | **17,484** | **1,067** | **2,135** |

* Lamp areas are **measured**: Arc lamp 12 kW radius 6 → π·6² = 113 tiles; Street light 2 kW radius 7 → π·7² = 154 tiles (`Tuning - Power.asset:39-40, 43-44`).
* Lamp counts above are the **ideal-packing floor** (**estimated**: street tiles ÷ lamp area, no overlap, no shadow). A realistic packing factor of 0.6 gives **2,420 arc lamps = 29,040 kW** or **1,772 street lights = 3,544 kW** (**estimated**).
* Solid things block light (**approved**, `ALWAYS_DARK_SPEC.md` §5.3), so the real count is above the floor, not below it.

> **Street lights are not player-buildable.** The catalogue has 26 machines and none is a street light (**measured**, `Sim/Data/Generated/CatalogueData.g.cs:42-154`). The 18 street lights in the world are authored sites read by id prefix `light:` (**measured**, `Sim/Campaign/Light/StreetLights.cs:25-39`; `FullSites.asset:559-738`). So a player cannot light the city cheaply with the content that exists. Either the ending stamps the mask by rule at zero kW, or the city is lit with Arc lamps at 17.5–29 MW. **This is F1-35 (1).**

Other standing load, **assumed** (a defended four-factory endgame; no such base has been built or played):

| Item | Count | kW each | kW | Source of the kW |
|---|---:|---:|---:|---|
| Gun turrets | 32 (8 per site × 4) | 20 | 640 | `CatalogueData.g.cs:134` |
| Arc lamps at sites | 32 | 12 | 384 | `CatalogueData.g.cs:126` |
| Excavators | 4 | 60 | 240 | `CatalogueData.g.cs:44` |
| Foundries | 8 | 80 | 640 | `CatalogueData.g.cs:52` |
| Assemblers | 4 | 100 | 400 | `CatalogueData.g.cs:62` |
| Home core + radio | 1 each | 100 / 20 | 120 | `Tuning - Power.asset:27-28` |
| **Subtotal** | | | **2,424** | |

The 8 turrets + 8 arc lamps per site figure follows `ENEMY-THREAT-AUDIT.md:430-440`. It is an **assumption**, not a build anyone has played.

### 2.4 The answer to (a)

| Case | Standing (kW) | Approved fixed supply 2,400 kW → throttle with no surge | Verdict |
|---|---:|---:|---|
| Lights only, street lights, ideal packing | 2,135 | 1.00 | holds; surge headroom **2,665 kW** |
| Lights only, street lights, realistic | 3,544 | 0.68 | holds; surge headroom **1,256 kW** |
| Lights only, arc lamps, ideal packing | 17,484 | 0.14 | **fails before any surge** |
| Street lights realistic + defended base | 5,968 | 0.40 | **fails before any surge** |
| Arc lamps realistic + defended base | 31,464 | 0.08 | **fails before any surge** |

All **estimated** from the measured inputs above.

**So: no.** The approved fixed supply alone carries a lit city only in the single cheapest case, and only because street lights cost 2 kW — and the player cannot build street lights. In every realistic case the player must add Generators.

### 2.5 With Generators, how big can the last surge be?

Generators are unlimited in number. Cost after `OpeningBalance` is **20 Steel + 5 Copper** each (**measured**, `Sim/Data/OpeningBalance.cs:15`).

Worked case — the "street lights realistic + defended base" row, standing **5,968 kW**, minus East Wharf's 258 kW because the ninth district is not lit yet → **standing at stage 9 = 5,710 kW** (**estimated**):

| Generators | Supply kW | Surge allowed at throttle ≥ 0.5 (`2S − D`) | Surge allowed at throttle = 1 (`S − D`) |
|---:|---:|---:|---:|
| 20 | 6,000 | **6,290** | 290 |
| 24 | 7,200 | 8,690 | 1,490 |
| 30 | 9,000 | 12,290 | 3,290 |
| 40 | 12,000 | 18,290 | 6,290 |

**The largest winnable final surge is `2 × Supply − Standing`, and on the worked 20-Generator grid that is 6,290 kW.** Its assumptions: the city is lit by street lights at a realistic packing (F1-35 decides whether that is even the model), a four-site defended base draws 2,424 kW (assumed), and the player has built 20 Generators.

Two consequences worth the owner's attention:

* **The surge rule as drafted gates nothing.** Four more Generators (80 Steel + 20 Copper) buy 2,400 kW of surge headroom. A player who has powered a lit city can absorb almost any surge. **F1-01.**
* **Holding at exactly 0.5 halves the defence.** Turret cooldown recovers at circuit throttle (**measured**, `TurretPhase.cs:118-122`). Nine 60 s holds at throttle 0.5 = up to 9 minutes of half-rate turret fire, during the raid the ending declares. If that is the intended drama, say so; if not, the bar should be throttle = 1 and the formula becomes `Supply − Standing`.

### 2.6 Fuel — the real ceiling

Burn rate is **measured**: `burn.Timer += shareKw / (coalMj × 1000) × dt`, one unit per whole timer (`Sim/Power/PowerPhase.cs:91-103`). Coal is 4 MJ (`Tuning - Power.asset:23`). Refined **Fuel counts as generator fuel too** (`PowerNetwork.cs:123`: `Inv[Coal] + Inv[Fuel]`), at the same 4 MJ.

| Fuel in the world | Units | MJ | Source |
|---|---:|---:|---|
| Coal, Home opening deposit (96,369) | 700 | 2,800 | **measured**, `World/OpeningResourceLayout.cs:36` |
| Coal, Ravenholm Quarry (818,91) | 12,000 | 48,000 | **measured**, `FullSites.asset:179-188` |
| Coal, Civic (777,411) | 3,000 | 12,000 | **measured**, `FullSites.asset:219-228` |
| Crude, East Wharf (830,413) → 4 Fuel each | 12,000 → 48,000 | 192,000 | **measured**, `FullSites.asset:169-178`; recipe `CatalogueData.g.cs:162` |
| **Total** | | **254,800 MJ** | = 70,778 kW·h |

Whole-world burn time, **estimated** (energy ÷ load):

| Load | Coal only (62,800 MJ) | Coal + refined crude (254,800 MJ) |
|---:|---:|---:|
| 3,544 kW (street-lit city) | 4.9 h | **20.0 h** |
| 5,968 kW (+ defended base) | 2.9 h | **11.9 h** |
| 29,040 kW (arc-lit city) | 0.6 h | **2.4 h** |

**The crude patch at East Wharf is the endgame power source** — it holds three quarters of the world's stored energy, it is the only crude in the world, and it sits inside the East Wharf stronghold district 168 tiles from the nearest tram stop. Nothing in code moves it there yet (§4).

The nine 60 s holds themselves are cheap: at throttle 0.5 on a 6,000 kW grid, load is 6,000 kW, so 9 × 60 s costs **810 fuel units** (**estimated**, 6000/4000 = 1.5 units/s). That is 1.3 % of the world's fuel. Fuel is not what limits the surge; supply capacity is.

---

## 3. (b) How long does the final raid last?

### 3.1 What the code actually does

| Fact | Value | Source |
|---|---|---|
| Waves per major raid | 4 | **measured**, `Sim/Data/SiegeTuning.cs:104` |
| Wave gap / spread / tail | 110 / 40 / 30 s | **measured**, `SiegeTuning.cs:104` |
| Schedule length | 3×110 + 40 + 30 = **400 s** | **measured**, `Sim/Combat/Director/SiegePlan.cs:161-168` |
| Wave weights (growth 0.5) | 1 : 1.5 : 2 : 2.5 | **measured**, `SiegePlan.cs:53-67` |
| Head count | `max(waves, round(60 × growth))`, growth `min(2.0, 1 + 0.15 × assaults)` | **measured**, `SiegePlan.cs:41-43` |
| **Head count ceiling** | **120 bodies** | **estimated** from the line above: 60 × 2.0 |
| Active raid cap | 48 | **measured**, `Tuning - Raids.asset:30`; `Sim/Combat/Director/DirectorPhase.cs:228` |
| World cap | 240 | **measured**, `Tuning - Raids.asset:31`; `DirectorPhase.cs:227` |
| What `EndsAt` does | cancels the **unspawned remainder only** | **measured**, `DirectorPhase.cs:213` |
| **What actually ends a raid** | `Committed && Remaining == 0 && GroupAlive == 0` — **every body must die** | **measured**, `DirectorPhase.cs:34-44` |
| Warning before a major | 300 s | **measured**, `Tuning - Raids.asset:25`; `DirectorPhase.cs:50` |
| Quiet spell after | 300 s | **measured**, `Tuning - Raids.asset:28`; `DirectorPhase.cs:41` |

> **U-D-64 (f)'s 160–220 tier cannot happen.** The growth cap of 2.0 pins the largest raid at 120 bodies. Raising it is **REL-38**, and the siege shape is C# constants with no asset to tune (**REL-43**).

### 3.2 Rounds and HP per tier

Enemy HP — **measured**: Skitter 20 (`Data/Generated/Enemies/Enemy - Skitter.asset:21-28`), Spitter 50 (`Enemy - Spitter.asset:21-28`), Breaker 220 (`Sim/Data/CombatBalance.cs:45-55`, code only).
Gun turret — **measured**: 10 damage, 1 round/s, never misses once aimed and sighted (`Sim/Combat/Turrets/TurretPhase.cs:100-109`). So 10 dps at throttle 1.
Composition — **approved** 2:1 Skitter:Spitter (U-D-64); Breakers 5 % from wave 2 (**measured**, `SiegeTuning.cs:107`).

| Tier (U-D-64 f) | Bodies | Breakers | Skitter | Spitter | Total HP | Rounds at 10 dmg |
|---|---:|---:|---:|---:|---:|---:|
| 0 plants, low | 60 | 0 | 40 | 20 | 1,800 | **180** |
| 0 plants, high | 90 | 4 | 57 | 29 | 3,470 | **347** |
| 1–2 plants, high | 140 | 7 | 89 | 44 | 5,520 | **552** |
| **code ceiling today** | **120** | 6 | 76 | 38 | 4,740 | **474** |
| 3+ plants, low | 160 | 8 | 101 | 51 | 6,330 | **633** |
| 3+ plants, high | 220 | 11 | 139 | 70 | 8,700 | **870** |

All **estimated** by arithmetic from the measured HP and composition. The 60-body row reproduces the figure already written in `SiegeTuning.cs:60-65` and the 120-body row reproduces `ENEMY-THREAT-AUDIT.md:412-420`, which is a useful cross-check.

### 3.3 Duration

Clear time = total HP ÷ defence dps (**estimated**):

| Turrets in contact | dps @ throttle 1 | 220 bodies | 160 | 120 |
|---:|---:|---:|---:|---:|
| 4 | 40 | 218 s | 158 s | 119 s |
| 8 | 80 | 109 s | 79 s | 59 s |
| 12 | 120 | 73 s | 53 s | 40 s |

Every figure is under the 400 s schedule, so **the schedule dominates**. Duration is:

> **400 s + time to clear the last wave.**

Last wave is 2.5/7 of the roster. At 220 bodies that is 79 bodies ≈ 3,124 HP (**estimated**):

| Defence during the tail | Clear time | **Final raid length** |
|---|---:|---:|
| 8 turrets at throttle 1 | 39 s | **439 s (7.3 min)** |
| 8 turrets at throttle 0.5 (a surge is holding) | 78 s | **478 s (8.0 min)** |
| 4 turrets at throttle 1 | 78 s | 478 s |
| 4 turrets at throttle 0.5 | 156 s | 556 s (9.3 min) |

This brackets the audit's measured 385–428 s at 60–87 bodies (`ENEMY-THREAT-AUDIT.md:398`) — **measured (audit), not re-run here**.

**The 48-body cap barely bites.** At 220 bodies, waves 3 (63) and 4 (79) both exceed 48, so spawning pauses (**measured**, `DirectorPhase.cs:227-228` — it is a pause, not a refill). Because the kill rate (80 dps) exceeds the spawn rate, the pause resolves as fast as bodies die and adds no material time.

**The floor that matters.** Spawn HP rate at 220 bodies is 8,700 ÷ 400 = **21.75 HP/s** (**estimated**). A defence below that never finishes: the raid has no timeout, so it runs forever. That is under **3 gun turrets at throttle 1**, or **5 at throttle 0.5**. The audit already logs this as P0 defect ENM-01, with the director observed frozen for up to 9,063 s (`ENEMY-THREAT-AUDIT.md:200`) — **measured (audit)**. Fixing it is **REL-44**; what a stalled raid counts as is **F1-30**.

### 3.4 Against a switch-on of at least nine minutes

Switch-on = 9 stages × 60 s = **540 s** (**provisional**, U-P-13). Nothing in code implements a Master Switch stage.

| When the last raid is declared | Raid arrives | Raid ends | Overlap with the 540 s switch-on |
|---|---:|---:|---|
| At the first throw (T=0) | T+300 | T+739 … T+1,018 | live for the last 240 s — stages 5–9 |
| At the ninth throw (T=540) | T+840 | T+1,279 … T+1,558 | none |

U-D-60 says "the raid declared by the last throw", which reads as the second row. The timing is **not defined** in code or decisions. **F1-04** (the target) and **F1-22** (how long a raid is meant to last — the design says 2–3 min, the game does 6.5–8) both change this answer.

---

## 4. (c) Which substations sit where no plant is, and what feeds them?

Plant sites exist **[REF] only** — `packages/sim/src/city/riverfront.ts:117-134`. They are not imported into Unity (§2.2).

| # | Substation (x,y) | District | Nearest plant | Distance | Has a plant? |
|---:|---|---|---|---:|---|
| 0 | 78,347 | Founders Court (Home) | — (Home workshop at 65,347) | 13 | **no** — but the Home core is here |
| 1 | 304,424 | Riverside Works | `plant:riverside` (287,417) | 18 | yes |
| 2 | 416,94 | Ironworks | `plant:ironworks` (402,78) | 21 | yes |
| 3 | 720,420 | Civic Utility | `plant:civic` (707,415) | 14 | yes |
| 4 | 115,176 | Westridge Homes | Riverside | 296 | **no** |
| 5 | 278,178 | Old Town | Ironworks | 159 | **no** |
| 6 | 98,59 | Northwood Freight | Ironworks | 305 | **no** — stronghold district |
| 7 | 833,91 | Ravenholm Quarry | Ironworks | 431 | **no** — stronghold district |
| 8 | 834,408 | East Wharf | Civic | 127 | **no** — stronghold district |

Substation positions **measured** (`FullSites.asset:29-118`); district names **measured** from the region labels (`FullSites.asset:469-558`); plant positions **[REF] measured**; distances **estimated** (straight-line Euclidean, ignoring buildings and the river).

This reproduces REL-48's own premise: **five of nine sit in districts with no plant, three of them inside strongholds.** Substation 0 is the sixth without a plant, but it has the Home core.

### What feeds them

**Nothing, until the player runs poles.** A substation site joins a circuit only when a placed reach node falls in its group — **measured**, `PowerNetwork.cs` union-find over reach nodes. Reaches are **measured** (`Tuning - Power.asset:29-31`): Pole 8, Big pole 12, Substation 8.

Big pole costs 4 Steel + 4 Copper (**measured**, `CatalogueData.g.cs:114`). Straight-line minimum spanning tree over the nine sites, **estimated**:

| Span | Tiles | Big poles at 12 |
|---|---:|---:|
| sub0 → sub1 (Home → Riverside) | 239 | 20 |
| sub0 → sub4 (Home → Westridge) | 175 | 15 |
| sub4 → sub6 (Westridge → Northwood) | 118 | 10 |
| sub4 → sub5 (Westridge → Old Town) | 163 | 14 |
| sub5 → sub2 (Old Town → Ironworks) | 162 | 14 |
| sub1 → sub3 (Riverside → Civic) | 416 | 35 |
| sub3 → sub8 (Civic → East Wharf) | 115 | 10 |
| sub2 → sub7 (Ironworks → Quarry) | 417 | 35 |
| **Total** | **1,805** | **151** |

* 151 big poles = **604 Steel + 604 Copper** (straight-line floor, **estimated**).
* Real routes go round buildings; at a 1.3× detour factor, **≈196 poles = 784 Steel + 784 Copper** (**estimated**).
* Plus nine Substations at 50 Steel + 25 Copper each = **450 Steel + 225 Copper** (**measured** cost, **assumed** that the player builds one on each site — nothing says the site markers are pre-built).

Copper in the world is **16,200 units** (§ resource table in the checkpoint document). The grid alone therefore spends **6–9 %** of all copper. That is affordable. Lighting the city with Arc lamps at 4 Copper each would spend **5,800–9,700 Copper — 36–60 % of all the copper in the world** (**estimated**). That is the other reason the lighting model (F1-35) decides feasibility.

---

## 5. The three surge numbers the ending still needs

| Number | Today | Who settles it |
|---|---|---|
| Master Switch **site** | not defined | **F1-01** |
| Master Switch **bill of materials** | not defined; "drawn from all four factories" has nothing to bind to — no yard produces anything (**measured**, `Editor/World/RegionImporter.cs:509`, `Presentation/City/CityPresenter.cs:335`) | **F1-01**, **F1-02** |
| **Surge size** per stage | not defined | **F1-01** — this document gives the envelope: `2 × Supply − Standing` |

---

## 6. Verdict

> ### Not winnable without a design change.

Three things must change before the ending can be built, and none of them is a tuning number:

1. **Supply must exist in the sim.** Plants, the Turbine hall and the core are data that `PowerNetwork` never reads (`PowerNetwork.cs:78-81`). Until they supply power, the only source is the Generator and the approved 2,400 kW is fiction. → **REL-47**, and the plants-and-cores row **E-02 (REL-94)**, which is already planned (`PowerNetwork.cs:78-81` says the supply "arrives with" campaign state).
2. **The lighting model must be decided.** "Fully lit on every street tile" costs 17.5–29 MW with Arc lamps and cannot be done with street lights, because street lights are not buildable. Either the ending stamps the mask by rule at zero kW, or the surge and supply numbers are an order of magnitude larger than anything discussed. → **F1-35 (1)**, **REL-46**.
3. **Raids must be able to reach the approved size, and must be able to end.** With today's tuning values a raid tops out at 120 bodies (`SiegePlan.cs:41-43`), and a raid ends only when every body dies (`DirectorPhase.cs:34-44`). → **REL-38**, **REL-43**, **REL-44**.

**If those three land, it is winnable only if named provisional values move** — specifically U-P-13's throttle floor. At 0.5 the rule gates nothing (four extra Generators buy 2,400 kW of headroom) while halving the defence for nine minutes. At throttle 1 the rule becomes `Surge ≤ Supply − Standing`, which is a real build-out and does not brown out the turrets that the same raid is testing.

---

## What this does not do

It does not prove balance. Only play does that. No Unity session was run, no test was executed, and no figure here has been seen in a running game. The 2,424 kW endgame base load, the lamp packing factors and the four-turret-per-approach defence are **assumptions** about a base nobody has built. There is no owner acceptance verdict on any of it.

## Where this is recorded

The `Unity/Docs/TASKS.md` log entry of 2026-09-22 points at this file (REL-48 accept-when 1). The surge envelope and the open questions it sharpens are noted on REL-71.

---

*Companion document: `feasibility-checkpoint.md` (REL-91) — resources, routes, factory outputs, ammunition and the rebuild-risk register.*
