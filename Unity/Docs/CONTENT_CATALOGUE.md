# Relight — Content Catalogue

**Owner:** Worker A. **Companion to** [GAME_DESIGN.md](GAME_DESIGN.md), which describes the experience and the rules; this file holds the data. Decisions and their IDs are in [DECISIONS.md](DECISIONS.md).

## How to read these tables

Every row carries three things:

- **Source** — `path:line` or `path §section` in the reference project (`/mnt/e/Factorio2`). Constant names are quoted so a Unity implementer can find them.
- **Kind** — `current` (the literal value in code today), `approved` (an explicit owner decision), `provisional` (chosen at the checkpoint, not confirmed, **or** chosen by the Unity implementer for approved-but-unbuilt content). Anything from `docs/Implementation/GP_CHECKPOINT.md` is **provisional** unless a `docs/DECISIONS.md` entry confirms it.
- **Status** — Implemented and retained / Approved but not implemented / Implemented but needs correction / Unresolved / Retired.

`UNCERTAIN:` marks anything this pass could not verify. Where a document disagrees with the code, the code value is given here and the disagreement is listed in [§16](#16-values-that-disagree-between-documents-and-code).

**Runtime content.** Not every row below becomes a Unity data asset. A row is **runtime content** only if its Status is *Implemented and retained*, *Implemented but needs correction*, or *Approved but not implemented* **and** it is in the scoped Unity milestone. Rows whose Status is **Retired**, and the whole of §16, are **historical evidence and never generate runtime content**. [§17](#17-runtime-content-scope-for-b-05) lists the exclusions explicitly and is the authority B-05 generates against.

**Owner decisions 2026-09-11 (Q03, Q06) — what `provisional` now licenses.** A `provisional` value is a **starting value**, not a placeholder waiting for approval. The Unity implementer may change one with a recorded reason and retest result, throughout playtesting, without asking the owner per number (Q03); and for **Approved but not implemented** rows the implementer may *choose* the value in the first place (Q06). Consequently the label **Approved but not implemented** in this file now means "approved Unity scope with no code yet", never "waiting for a per-feature owner go-ahead": the reference project's GP-15–25 checkpoint hold does not gate the port. What still needs an owner decision is a change of **design** — a different mechanic, a removed system, a new progression rule — not a number. Rows the owner fixed directly on 2026-09-11 are marked `approved` and cite **Owner decisions 2026-09-11 (Q-number)**; see [GAME_DESIGN.md §1.4](GAME_DESIGN.md#14-owner-decisions-2026-09-11--what-they-settle) for the index.

Abbreviations: **S** = Steel plates, **Cu** = Copper, **t** = world tiles, **kW** = kilowatts.

---

## 1. Items

Player-facing names from `packages/sim/src/itemNames.ts`; ids and stack sizes from `packages/sim/src/flow.ts` `ITEMS` and `packages/sim/src/engineer.ts` `STACK` / `stackSize`.

| Item id | Player name | Stack | Role | Source | Kind | Status |
|---|---|---|---|---|---|---|
| `steel` | Steel plates | 50 | Primary construction material | `engineer.ts` `STACK` | current | Implemented and retained |
| `copper` | Copper | 50 | Electrical/ammunition material | `engineer.ts` `STACK` | current | Implemented and retained |
| `stone` | Stone | 50 | Concrete input | `engineer.ts` `STACK` | current | Implemented and retained |
| `coal` | Coal | 50 | Generator fuel; Shell input | `engineer.ts` `STACK` | current | Implemented and retained |
| `ironore` | Iron ore | 50 | Foundry input → Steel plates | `engineer.ts` `STACK` | current | Implemented and retained |
| `copperore` | Copper ore | 50 | Foundry input → Copper | `engineer.ts` `STACK` | current | Implemented and retained |
| `iron` | Iron | 50 | Legacy id; no campaign consumer | `engineer.ts` `STACK` | current | Retired (legacy) |
| `crude` | Crude oil | 50 | Refinery input; pumpjack-only | `engineer.ts` `STACK` | current | Implemented and retained |
| `fuel` | Refined fuel | 50 | Generator fuel, 4 MJ like Coal | `engineer.ts` `STACK` | current | Implemented and retained |
| `polymer` | Polymer | 50 | Assembler Mk2, Fast belt | `engineer.ts` `STACK` | current | Implemented and retained |
| `wire` | Wire | 50 | Board and Overclock input | `engineer.ts` `STACK` | current | Implemented and retained |
| `frame` | Frame | 50 | Alien workbench build cost (4) + Overclock module input (2) | `engineer.ts` `STACK`; `flow.ts:218`, `flow.ts:184` | current | Implemented and retained — **these are its consumers** (Q16, §16 row 16) |
| `board` | Board | 50 | Alien workbench build cost (2) + Overclock module input (1) | `engineer.ts` `STACK`; `flow.ts:218`, `flow.ts:184` | current | Implemented and retained — **these are its consumers** (Q16, §16 row 16) |
| `concrete` | Concrete | 50 | Barricade, Turbine hall | `engineer.ts` `STACK` | current | Implemented and retained |
| `magazine` | **Bullets** | **200** | One item = one bullet | `engineer.ts` `STACK`; `flow.ts:156` `ammoUnit` | current (approved model) | Implemented and retained |
| `shell` | Shells | 20 | Cannon ammunition (Arsenal) | `engineer.ts` `STACK` | current | Implemented and retained |
| `alienartifact` | Alien Artifact | 20 | Shared crafting ingredient | `engineer.ts` `STACK` | current | Implemented and retained |
| `overclock` | Overclock Module | 1 | Removable +10 % machine equipment | `engineer.ts` `STACK` | current | Implemented and retained |
| `core1` / `core2` / `core3` | Power core 1/2/3 | 1 | Commissions any prepared plant | `engineer.ts` `stackSize` default | current | Implemented and retained |
| `artifact1` / `artifact2` / `artifact3` | Speed artifact 1/2/3 | 1 | Legacy +10 % upgrade; converts to Overclock | `engineer.ts`; `overclockMigration.ts` | current | Retired in the port |
| `rifle:<n>` etc. | Rifle / Two-barrel shotgun / Arc projector / Plasma lance | 1 | Owned weapon instance | `weaponTypes.ts`; `itemNames.ts` | current | Implemented and retained |
| (packed machine) | e.g. "Excavator" | 1 per slot | A packed existing machine, not a crafted item | `flow.ts` pack/unpack | current | Implemented and retained |

Notes: `rounds` is an internal recipe output id that resolves to `magazine` (Bullets) under `ammoVersion 1`. Weapons never merge by type — each instance is distinct (GP_CHECKPOINT, player-test corrections).

### 1.1 Container capacities

| Container | Capacity | Source | Kind | Status |
|---|---|---|---|---|
| Backpack | 40 slots (`INV_STACKS`) | `engineer.ts` | current | Implemented and retained |
| Supply chest | 200 items total (not stacks) | `flow.ts`; catalogue §1 | current | Implemented and retained |
| Truck | 200 cargo stacks (`TRUCK_STACKS`) | `engineer.ts` | current | Implemented and retained |
| Tram cargo | 200 items, 250 with the Rail crew | `freight.ts`; `RECRUITS.railcrew` | current | Implemented and retained |
| Tram platform stock / arrival cargo | 200 items each, separate inventories | `freight.ts` | current | Implemented and retained |
| Generator fuel | 50 combined Coal + Refined fuel (`GENERATOR_COAL_CAP`) | `flow.ts` | current | Implemented and retained |
| Gun turret hopper | 50 bullets; 62 with the Gunsmith upgrade (`hopperCapacity`, ×1.25 floored) | `constants.ts` `TURRET.hopper`; `progression.ts` | current | Implemented and retained |
| Machine input buffer | 4 crafts' worth per ingredient (`ASM_INPUT_MULT`) | `flow.ts` | current | Implemented and retained |
| Machine output buffer | 5 items (`ASM_OUTPUT_CAP`); **50** for bullets under `ammoVersion 1` | `flow.ts:955` | current | Implemented and retained |
| Field kit | 10 stacks (`KIT_STACKS`) | `engineer.ts` | current | Retired (legacy) |

---

## 2. Resources and mining

| Fact | Value | Source | Kind | Status |
|---|---|---|---|---|
| Hand mining rate | **0.5 items/s** (halved from 1.0 by GP-PLAYTEST-FIX 2, in answer to the owner's report; no verdict on 0.5 is on record) | `flow.ts:210` `HAND_MINE_PER_S` | provisional | **Implemented and retained** — provisional starting point, **retest pending** (U-P-01; chronology GAME_DESIGN.md §12.2.1) |
| Hand mining destination | the backpack, never straight to storage | `flow.ts:1015` | current | Implemented and retained |
| Excavator rate | 0.5 items/s (one item per 2 s) | `constants.ts` `EXCAVATOR_PER_S` | current | Implemented and retained |
| Pumpjack rate | one Crude oil per 2 s | `CURRENT_GAMEPLAY_CATALOGUE.md` §3 | current | Implemented and retained |
| Crude oil | pumpjack only; cannot be hand-mined | `flow.ts:1462` (rubble type `crude` excluded from hand mining) | current | Implemented and retained |
| Patch size | 12,000 units | `progression.ts` `CORRECTIONS.resourceUnits` | provisional | Implemented and retained |
| Legacy start coal patch | 700 | `recipes.ts` `START_COAL_PATCH` | current | Retired (legacy) |
| Mineable types | Steel, Copper, Stone, Coal, Iron ore, Copper ore (finite salvage and patches) | `flow.ts` rubble types | current | Implemented and retained |

---

## 3. Recipes

Base times are per completed craft at full power with inputs available and output space. `packages/sim/src/recipes.ts` `RECIPES` and `packages/sim/src/flow.ts` `ASSEMBLER_RECIPES`.

| Recipe | Inputs | Output | Seconds | Machine | Source | Kind | Status |
|---|---|---|---|---|---|---|---|
| Steel plates | 2 Iron ore | 1 Steel plates | 3 | Foundry | `flow.ts` `ASSEMBLER_RECIPES.iron` | current | Implemented and retained |
| Refined copper | 2 Copper ore | 1 Copper | 3 | Foundry | `flow.ts` `ASSEMBLER_RECIPES.copper` | current | Implemented and retained |
| Refined fuel | 1 Crude oil | 4 Refined fuel | 3 | Refinery | `recipes.ts` "Fuel" | current | Implemented and retained |
| Polymer | 2 Crude oil | 1 Polymer | 3 | Refinery | `recipes.ts` "Polymer" | current | Implemented and retained |
| Wire | 1 Copper | 2 Wire | 1 | Assembler / Mk2 | `recipes.ts` "Wire" | current | Implemented and retained |
| Frame | 2 Steel plates | 1 Frame | 2 | Assembler / Mk2 | `recipes.ts` "Frame" | current | Implemented and retained |
| Board | 3 Wire + 1 Steel plates | 1 Board | 4 | Assembler / Mk2 | `recipes.ts` "Board" | current | Implemented and retained |
| Concrete | 2 Stone | 1 Concrete | 2 | Mixer (Concrete crew) | `recipes.ts` "Concrete" | current | Implemented and retained |
| **Bullet batch** *(internal recipe name `Shot magazine`, `flow.ts:175`)* | 2 Steel plates + 1 Copper | **10 Bullets** | 6 | Assembler Mk1 / Home workshop | `constants.ts` `SHOT_MAGAZINE`; `recipes.ts` | current | Implemented and retained |
| Bullet batch (Mk2) *(internal `Shot magazine`)* | 2 Steel plates + 1 Copper | 10 Bullets | 3 | Assembler Mk2 | `constants.ts` `SHOT_MAGAZINE_MK2_SECONDS` | current | Implemented and retained |
| Shell | 2 Steel plates + 1 Coal | 1 Shell | 3 | Assembler (Arsenal required) | `recipes.ts` "Shell" | current | Implemented and retained |
| **Overclock Module** | 2 Alien Artifacts + 2 Frames + 1 Board + 4 Wire | 1 Overclock | 20 | Alien workbench | `flow.ts` `ASSEMBLER_RECIPES.overclock` | current | Implemented and retained |
| **Hand bullet batch** | 2 Steel plates + 1 Copper | 10 Bullets | **20** | Home workshop, player stationary | `flow.ts:158` `HAND_BULLET_SECONDS`, `:1037` | provisional | **Implemented and retained** — provisional starting point (6 → 12 → 20 s), **retest pending** (U-P-02; GAME_DESIGN.md §12.2.2) |
| **Rifle** | 10 Steel plates + 4 Copper | 1 Rifle instance | 6 | Home workshop (`nearDepot`) | `equipment.ts` `RIFLE` | current | Implemented and retained |
| Alien workbench decode | — | 2 Alien Artifacts per cycle | 15 decode | Alien workbench | `fabrication.ts` `FABRICATION` | provisional | Implemented and retained |

**Multipliers.** Assembler Mk2 = ×2 (`processingMultiplier`); an installed Overclock/artifact = ×1.1; they stack multiplicatively. Throughput: Mk1 on the Shot recipe = `ASSEMBLER_MK1_MAG_PER_MIN = 10` crafts/min = 100 bullets/min; Mk2 = `ASSEMBLER_MAG_PER_MIN = 20` crafts/min = 200 bullets/min; hand = 30 bullets/min.

**Frame and Board consumers — settled.** Owner decisions 2026-09-11 (Q16): Frames and Boards **keep exactly their existing uses** and are not vestigial. Verified in code: the Alien workbench costs `{ steel: 30, copper: 10, frame: 4, board: 2 }` (`flow.ts:218` `MACHINE_COST.alienworkbench`) and the Overclock Module recipe takes `{ alienartifact: 2, frame: 2, board: 1, wire: 4 }` (`flow.ts:184`). No advanced production chain is to be invented to give them a further consumer; further consumers may arrive with other approved content, but none is required. The older "no implemented consumer / retained for future turret work" line in `docs/CURRENT_GAMEPLAY_CATALOGUE.md` §1 predates the Alien workbench and is stale — see §16 row 16.

**Player-facing naming.** The item's player name is **Bullets** (`itemNames.ts:6`); `magazine` is the **internal item id** (`flow.ts:155`) and `Shot magazine` the **internal recipe name** (`flow.ts:175`). Both internal names stay as ids in the port. Player-facing strings that still read "Shot magazines" / "Magazines" — `goal.ts:132`, `:138`, `:140`, `:143`, `:144` and `flow.ts:2142` — are **stale and must not be copied**; Unity says *bullets* / *bullet batch* (GAME_DESIGN.md §4.2, §6.3).

---

## 4. Machines and buildings

Costs `packages/sim/src/flow.ts` `MACHINE_COST`; footprints `MACHINE_SIZE`; demand `MACHINE_KW`. All **current** unless noted. All **Implemented and retained** unless noted.

| Machine | Cost | Size | kW | Unlock | Notes |
|---|---|---|---|---|---|
| Excavator | 10 S | 3×3 | 60 | — | 0.5 items/s from the footprint and its border |
| Pumpjack | 20 S + 10 Cu | 3×3 | 60 | — | Crude oil only |
| Foundry | 30 S + 10 Cu | 3×3 | 80 | — | ore → plate |
| Refinery | 40 S + 20 Cu | 3×3 | 100 | — | fuel / polymer |
| Assembler | 40 S + 20 Cu | 3×3 | 100 | — | Shells need Arsenal |
| Assembler Mk2 | 60 S + 30 Cu + 4 Polymer | 3×3 | 150 | material (Polymer) | ×2 speed |
| Mixer | 20 S + 10 Cu | 3×3 | 60 | Concrete crew | Concrete |
| Alien workbench | 30 S + 10 Cu + 4 Frames + 2 Boards | 3×3 | 80 | Schematic | decode 15 s, Overclock crafting |
| Supply chest | 10 S | 2×2 | 0 | — | 200 items |
| Belt | 1 S / tile | 1×1 | 0 | — | 7.5 items/s (`BELT_PER_S`) |
| Fast belt | 2 S + 1 Cu + 1 Polymer / tile | 1×1 | 0 | — | 15 items/s (`FAST_BELT_PER_S`) |
| Inserter | 1 S + 1 Cu | 1×1 | 10 | — | 1 item/s (`INSERTER_PER_S`); optional |
| Underground belt | 5 S + 2 Cu per endpoint | 1×1 each | 0 | — | spans 4 hidden tiles |
| Priority splitter | 5 S + 2 Cu | 2×1 | 0 | — | balance / priority / filter |
| Generator | 30 S + 10 Cu | 2×2 | −300 (supply) | — | 50-item fuel inventory |
| Pole | 1 S + 1 Cu | 1×1 | 0 | — | 8 t reach (`POLE_REACH`) |
| Big pole | 4 S + 4 Cu | 2×2 | 0 | Electricians | 12 t reach (`BIG_POLE_REACH`) |
| Substation | 50 S + 25 Cu | 3×3 | 0 | — | reach 8 t (`nodeReach`) |
| Lamp | 1 S + 1 Cu | 1×1 | 5 | — | radius 4 (`LAMP_RADIUS`) |
| Arc lamp | 4 S + 4 Cu | 1×1 | 12 | Lamplighters | radius 6 (`ARC_LAMP_RADIUS`) |
| Floodlight | 10 S + 5 Cu | 2×2 | 40 | Electricians | 12 t, 60° cone (`FLOODLIGHT_HALF_ANGLE = π/6`) |
| **Gun turret** | 15 S + 5 Cu | 2×2 | **20** (`TURRET_KW`, provisional) | — | see §6 |
| Cannon | 40 S + 20 Cu | 2×2 | 0 | Arsenal | see §6 |
| Wall | 2 S | 1×1 | 0 | — | 120 HP |
| Barricade | 2 S + 4 Concrete | 1×1 | 0 | Concrete crew | 240 HP |
| Depot (Home) | — | fixed | 0 | world | Home storage + handcrafting |
| Track / Tram stop / Tram | 1 S / 10 S / 20 S + 5 Cu | 1×1 / 2×2 / 1×1 | 0 / 20 / 0 | — | **Retired** as player-buildable in the fixed-tram campaign |

`machineKw(st, m)` returns **0 for a turret in a non-campaign save** — turret power is a campaign-only rule (`flow.ts`).

---

## 5. Weapons

`packages/sim/src/weaponProfiles.ts` `WEAPON_PROFILES` — "Provisional player weapon tuning; turret balance lives in recipes.ts. Distances are world tiles." Damage falls off linearly between `effective` and `max` and is 0 beyond `max` (`weaponDamage`).

| Weapon | Effective | Max | Damage | Rate /s | Pellets | Spread (rad) | Hit radius | Projectile speed | Kind | Status |
|---|---|---|---|---|---|---|---|---|---|---|
| Rifle | 18 | 30 | 10 | 2.5 | 1 | 0 | 0.65 | hitscan | provisional band, current code | Implemented and retained |
| Two-barrel shotgun | 6 | 12 | 6 per pellet | 1.25 | 5 | 0.36 (≈20.6°) | 0.45 | hitscan | provisional | Implemented and retained (Arsenal unlock) |
| Arc projector | 9 | 9 | 10 | 2.5 | 1 | 0 | 0.6 | hitscan | provisional | **Runtime implemented; acquisition route approved Unity scope** (Q06; was GP-17) |
| Plasma lance | 14 | 14 | 25 | 1 | 1 | 0 | 0.45 | 12 t/s travelling bolt | provisional | **Runtime implemented; acquisition route approved Unity scope** (Q06; was GP-18) |

| Weapon fact | Value | Source | Kind | Status |
|---|---|---|---|---|
| Equipped slots | 2 physical; extra weapons use ordinary storage | `equipment.ts` `Equipment.slots` | approved | Implemented and retained |
| Rifle loaded capacity | 10 rounds | `equipment.ts` `RIFLE.capacity` | current | Implemented and retained |
| Rifle reload | 1.5 s, from carried bullets only | `equipment.ts` `RIFLE.reload` | current | Implemented and retained |
| Ammunition contract | one carried bullet per trigger pull, all four weapons | GP_CHECKPOINT "Distinct weapon ranges" | provisional | Implemented and retained |
| Fresh campaign start | **no weapon**; Rifle must be crafted | `equipment.ts` `initEquipment` | approved | Implemented and retained |
| Legacy save migration | `double` if `barrels === 2 \|\| arsenal`, else `rifle`; partial magazine remainder loaded once | `equipment.ts` `initEquipment` | current | Retired in the port (U-M-06) |
| Legacy abstract rifle | `RIFLE_ROUNDS_PER_S = 1.5`, `RIFLE2_ROUNDS_PER_S = 3/1.4`, `RIFLE_RANGE = TURRET_RANGE` (9), `RIFLE_HIT_RADIUS = 1.5` | `engineer.ts` | current | **Retired** — superseded by `WEAPON_PROFILES` |
| GP-15–18 weapon content (Shotgun acquisition, two-barrel Arsenal route, Arc + energy cells, Plasma/M3 gating) | recipes, costs and gates **provisional, implementer-chosen** | `docs/PROGRESS.md` GP-15–18; **Owner decisions 2026-09-11 (Q06)** | provisional | **Approved but not implemented — in Unity scope.** The reference project's GP-15–25 checkpoint hold does not gate the port; no per-feature go-ahead is outstanding (GAME_DESIGN.md §8.1, §5.3) |
| Energy cells — reusable-cell *architecture* (saved integer charge, component-built, workbench recharge) | approved design, unbuilt | `docs/DECISIONS.md` D-GP-START ("approves … D-GP-08 later reusable-cell architecture **without early implementation**"); `docs/EXPLORATION_DEFENCE_PLAN.md` §6 D-GP-08 / GP-17; U-D-22, U-D-25; **Owner decisions 2026-09-11 (Q06)** | approved | **Approved but not implemented — in Unity scope** (E-11). The GP-17 hold is a reference-project state and does not block the port |
| Energy cells — recipes, charge units, recharge demand/time, recurring-ingredient question | **implementer's choice**, recorded with its reason | design draft §8: "Exact recipes, charge units, recharge demand/time … remain open"; U-D-25; **Owner decisions 2026-09-11 (Q06)** | provisional | **Approved but not implemented.** *Correction 2026-09-11:* these numbers were previously listed as **Unresolved** pending an owner go-ahead; Q06 makes them provisional implementer choices. Only a change to the cell *model* (cells replacing bullets, a per-shot Artifact cost) would be a design question, and that framing stays retired |
| Energy cells **replacing** the ballistic economy; any per-shot Artifact cost | not the model | design draft §8 "Artifacts are not a per-shot cost"; GP_CHECKPOINT "Distinct weapon ranges": "this does not approve the later energy economy"; U-D-25 | — | **Retired** (this framing only; the cell *feature* is not retired) |

---

## 6. Ammunition and turrets

### 6.1 Ammunition

| Fact | Value | Source | Kind | Status |
|---|---|---|---|---|
| One ammunition item | **1 bullet** | `flow.ts:156` `ammoUnit`, `ammoVersion === 1` | approved | Implemented and retained |
| Bullet stack | 200 | `engineer.ts` `STACK.magazine` | current | Implemented and retained |
| Craft yield | 10 bullets per craft | `constants.ts` `ROUNDS_PER_MAG` | current | Implemented and retained |
| Assembler bullet output buffer | 50 | `flow.ts:955` | current | Implemented and retained |
| **Full hopper endurance** | **≈ 50 s of continuous fire** (50 rounds ÷ 1 shot/s); **≈ 62 s** with the Gunsmith upgrade | `constants.ts:42` `TURRET.hopper`; `turretTracking.ts:4`; corroborated verbatim by GP_CHECKPOINT second pass, "Turret cadence": "a 50-round hopper now lasts 50 s of continuous fire instead of about 14 s" | derived from current values | Implemented and retained. **Correction:** earlier text in this package said "about 10 seconds" — that is the *introductory encounter's* ten shots (GAME_DESIGN.md §4.3), not the hopper |
| Refill time, one Assembler Mk1 | 30 s for a 50-round hopper (100 bullets/min) | `ASSEMBLER_MK1_MAG_PER_MIN`; §3 | derived | Implemented and retained |
| Bullets per basic Skitter | 2 rounds (20 HP ÷ 10 damage) — **independent of cadence** | `GP_COMBAT` §7.1; `turretTracking.ts` | derived | Implemented and retained |
| Shell stack | 20 | `engineer.ts` `STACK.shell` | current | Implemented and retained |
| Legacy representation | 1 item = 10 rounds | `flow.ts:156` (`ammoVersion` undefined) | current | **Retired** |
| Migration | ×10 conversion of pools, output, reservations and item counters; loaded weapons, turret rounds and line buffers unchanged; residue < 1e-7 rounded, other fractions rejected | GP_CHECKPOINT; `ammunitionMigration.ts` | current | Retired in the port (U-M-06) |

### 6.2 Turrets

| Property | Campaign | Legacy | Source | Kind | Status |
|---|---|---|---|---|---|
| Range | 9 t | 9 t | `constants.ts` `TURRET.range` | current | Implemented and retained |
| Fire rate | **1 shot/s** (5 → 3.5 → 1) | 5 rounds/s | `turretTracking.ts:4` `CAMPAIGN_TURRET_RATE`; `constants.ts:42` `TURRET.roundsPerS` | provisional (campaign) / legacy retired | **Implemented and retained** — provisional starting point, **retest pending** for ordinary raids (U-P-05; GAME_DESIGN.md §12.2.3) |
| Damage per round | 10 | 4 | GP_CHECKPOINT; `constants.ts` `TURRET.roundDmg` | provisional / legacy | Implemented and retained |
| Hopper | 50 (62 with Gunsmith) | 50 | `constants.ts` `TURRET.hopper`; `progression.ts` `hopperCapacity` | current | Implemented and retained |
| Power | 20 kW, holds fire unsupplied | 0 kW | `recipes.ts` `TURRET_KW`; `flow.ts` `machineKw` | provisional (D-GP-TURRET-POWER) | Implemented and retained |
| Health | 100 HP | 100 HP | `campaignDefence.ts` `DEFENCE.turretHp` | current | Implemented and retained |
| Cannon rotation | 360°/s toward target; 80 ms muzzle flash; `TURRET_FLASH_S = 0.5` legacy | — | GP_CHECKPOINT "Turret improvements"; `flow.ts` | provisional | Implemented and retained |
| Cannon (Shell turret) | range 12, damage 50, reload 2 s, capacity 20, 140 HP, 0 kW | — | `progression.ts` `CORRECTIONS.cannon`; `campaignDefence.ts` `defenceMax` | provisional | Implemented and retained (Arsenal) |
| Opening objective | **three** turrets covering different approaches | — | `goal.ts` `turretRecommendation`; owner decision 16 | approved | Implemented and retained |
| Legacy placement rules | `EDGE_TURRETS = 2`, `TURRET_PER_TILES = 16`, `START_TURRETS = 6` | — | `constants.ts` | current | **Retired** (legacy) |

---

## 7. Enemies

Three rosters exist in the reference. **The owner reconciled them on 2026-09-11: the port ships the approved five-type roster of §7.4** — Skitter, Spitter, Stalker, Breaker, Howler — with guardians handled separately (§9.2) and the legacy roster of §7.3 retired with `legacy-v1`. Source: **Owner decisions 2026-09-11 (Q13, Q14, Q15, Q08)**; rationale and the two retained legacy-named mechanics are in [GAME_DESIGN.md §9.1.1](GAME_DESIGN.md#911-the-approved-roster-for-the-port). §7.1–§7.3 remain as provenance and as the ported behaviour baseline; open question A-1 is closed.

### 7.1 Live campaign roster — `packages/sim/src/gameplayCombat.ts:13` `GP_COMBAT`

| Actor | HP | Speed (t/s) | Damage | Interval (s) | Windup (s) | Range (t) | Kind | Status |
|---|---|---|---|---|---|---|---|---|
| Skitter | 20 | 5.4 | 5 | 1 | 0.3 | 1.3 | provisional | Implemented and retained |
| Spitter | 50 | 3.9 | 10 (projectile) | 2 | 0.6 | 7 | provisional | Implemented and retained |
| Guardian (freight) | 600 | 2.7 | 25 (charge) | 4 | 1.2 | 9 | provisional | Implemented and retained |

Shared: projectile speed 8 t/s, projectile life 2 s, notice 8 t, escape 18 t, squad alert radius 6 t, light hesitation 0.65 s, projectile cap 256.

Campaign births with gameplay progress are converted to Skitter/Spitter, roughly **one Spitter in three** (`campaignThreat.ts:198`).

### 7.2 Campaign roles (no-gameplay-progress saves, after the Arsenal) — `campaignThreat.ts:192`

This whole role path is a **legacy-save fallback** and does not come across (Q08); it is listed as provenance for the two mechanics §7.4 inherits.

| Role | Frequency | HP | Special | Source | Kind | Status |
|---|---|---|---|---|---|---|
| Breaker | 1 in 5 | 100 | ×3 structure damage | `progression.ts:17` `CORRECTIONS.breakerHp` | current | **Superseded: 220 is the port value** (Q14, §7.4). The ×3 structure-damage *mechanic* is retained and carries to the approved Breaker |
| Conductor | 1 in 10 | 80 | calls up to 8 reinforcements, one per 8 s | `CORRECTIONS.conductorHp`, `reinforcementSeconds`, `reinforcementLimit` | current | **Retired for the port** (Q08/Q13). The approved Howler alerts existing enemies only; reinforcement belongs to the director (§8) |
| Shade | 1 in 3 | 40 | untargetable on unlit tiles | `CORRECTIONS.shadeHp` | current | **Creature retired for the port; the light-suppression mechanic is retained** as the roster-wide light rule (Q13; GAME_DESIGN.md §9.1.1) |
| Crawler (base) | remainder | 12 | — | `campaignThreat.ts:195` | current | **Retired for the port** (Q08). Campaign births with gameplay progress already convert to Skitter/Spitter |

### 7.3 Legacy roster — `packages/sim/src/enemies.ts` (LEGACY §7)

| Enemy | HP | Ammunition to kill | Speed (t/s) | Size | Kind | Status |
|---|---|---|---|---|---|---|
| Crawler | 12 | 3 rounds | 3 | 1×1 | current | **Retired** with `legacy-v1` |
| Shade | 40 | 10 rounds | 2 | 1×1 | current | **Retired** with `legacy-v1` |
| **Hulk** | 600 | 10 Shells | 0.8 | 3×3 | current | **Retired** with `legacy-v1` |

Thresholds: `SHADE_THRESHOLD = 0.3`, `HULK_THRESHOLD = 0.5`, `SHADE_PER_CRAWLERS = 8`, `HULK_BLOOM_FRACTION = 0.4`, `ROUNDS_PER_CRAWLER = 3`, `ROUNDS_PER_SHADE = 10`, `SHELLS_PER_HULK = 10`, `SHADE_DISABLE_SECONDS = 30`.

**Confirmed retired (Owner decisions 2026-09-11, Q08).** The `legacy-v1` ruleset is retired from the Unity port and preserved in the reference project only, and these three creatures go with it. `SHADE_DISABLE_SECONDS = 30` is the one constant here whose **mechanic** is retained — as the roster-wide light rule of §7.4, not as a Shade.

**Naming reconciliation:** the migration brief's "legacy Crawler/Shade/Conductor" is inaccurate. The legacy third type is the **Hulk**. "Conductor" is a campaign *role* (§7.2), not a legacy enemy. The Stalker is neither: it is a separate RI-04 territorial-guard prototype in `packages/sim/src/stalker.ts` bound to legacy "wells while Dark", with a `guard → investigate → pursue → attack → return` state machine and its parameters in `candidates.ts` `CANDIDATES.stalker`.

### 7.4 The roster the port ships — approved five types (design draft §11; D-GP-09)

**Status: Approved Unity scope.** Source: **Owner decisions 2026-09-11 (Q13, Q14, Q15)**, adopting design draft §11. The five types below are the whole regular roster; no sixth type is added, and guardians (§9.2) are separate and are **not** one of the five. Roles are approved; **HP, damage, interval and speed are provisional starting values** the implementer may tune with a recorded reason (Q03).

| Enemy | HP | Attack | Interval (s) | Speed vs player | Role | Kind | Status |
|---|---|---|---|---|---|---|---|
| Skitter | 20 | 5 melee | 1 | 90 % | common swarm type | approved role, provisional values (match §7.1) | Implemented and retained |
| Spitter | 50 | 10 projectile | 2 | 65 % | ranged support; may target reachable exposed lights | approved role, provisional values (match §7.1) | Implemented and retained |
| Stalker | 60 | 15 lunge | 3 | 110 % | flanker; signalled lunge, pause after a miss | approved role, provisional values | **Approved but not implemented — in Unity scope** (was GP-19) |
| Breaker | **220** | 25 structure / 15 player | 2 | 45 % | slow structure-breaker; invites concentrated fire, pushes through light | approved role, **provisional 220** | **Approved but not implemented — in Unity scope** |
| Howler | 80 | weak melee | **undecided** | 70 % | priority target; alerts *existing* nearby enemies within a bounded radius and cooldown | approved role, provisional values | **Approved but not implemented — in Unity scope** (was GP-20) |

**Correction (2026-09-11) — Breaker HP 100 → 220.** This package previously recorded a live conflict between `CORRECTIONS.breakerHp = 100` (`packages/sim/src/progression.ts:17`, verified in code) and the design draft's 220. **Owner decisions 2026-09-11 (Q14) settle it at 220**, provisional, replacing 100 everywhere this package quotes a Breaker HP; the code value survives only as provenance in §7.2 and as history in §16 row 10. Open question A-2 is closed. At 10 damage per turret round, 220 HP is 22 rounds — Breaker *counts* per raid are the tuning lever (Q03), not the HP.

**Two constraints the implementer must not tune away.** The Howler **never summons new enemies** and must not escalate recursively — the retired Conductor's 8-reinforcement call (§7.2) does not come across, and reinforcement belongs to the director (§8). The Breaker's structure damage is the port's use of the retained ×3 `structureDps` multiplier (§8.2), not a second mechanic.

**Undecided values the implementer chooses and records** (Q06, not further owner decisions): the Howler's damage and interval, which the draft itself leaves open; per-raid composition shares for Stalker, Breaker and Howler; and the restoration-progress point at which each first appears.

Light avoidance across the roster is approved direction in design draft §11; only the Shade rule (`SHADE_DISABLE_SECONDS = 30`) and the 0.65 s hesitation at a lit tile are implemented, and those are the retained mechanics the roster-wide rule is built from.

### 7.5 Campaign workshop guardian

One Stalker lifetime, 5-tile activity perception, 8-tile home leash, 1.2 s visible wind-up, 5 damage (`docs/CAMPAIGN_RULES.md`). **current** — Implemented and retained.

---

## 8. Raid and threat director

`packages/sim/src/campaignThreat.ts`. All **Implemented and retained**; cadence **approved** (D-GP-START's corrected D-GP-11), composition **provisional**.

### 8.1 `ACTIVE_RAIDS` — the campaign director

| Constant | Value | Meaning |
|---|---|---|
| `firstMin` / `firstRange` | 1500 s / 300 s | First major assault at 25–30 active minutes |
| `intervalMin` / `intervalRange` | 1080 s / 240 s | Subsequent majors 18–22 min start-to-start |
| `warning` | 300 s | Announcement lead time |
| `grace` | 780 s | Opening grace |
| `window` | 150 s | Reinforcement window inside one assault |
| `total` | 60 | Finite roster per assault (≈40 Skitter + 20 Spitter) |
| `activeRaidBudget` | 48 | Simultaneously living major raiders |
| `livingBudget` | 240 | World-wide actor cap |
| `minorMin` / `minorRange` | 300 s / 120 s | Harassment every 5–7 minutes |
| `recovery` | 300 s | Quiet period after an assault |

### 8.2 `CAMPAIGN_THREAT` — actor behaviour and site rules

| Constant | Value | Meaning |
|---|---|---|
| `majorCount` | 60 | Roster size |
| `spawnEvery` | 4 s | Reinforcement spacing in the legacy path |
| `minorMin` / `minorMax` | 8 / 12 | Legacy harassment band (**superseded**: live band is 6–10, `6 + raidChoice(seed, raidsStarted, 4)`) |
| `contactDps` | 5 | Damage to the player on contact |
| `structureDps` | 8 | Damage to structures (×3 for a Breaker) |
| `speed` | 2 t/s | Base attacker speed |
| `minorAfterMajor` | 300 s | Harassment pushed back after an assault |
| `guardLeash` / `guardNotice` | 12 t / 8 t | Site guard leash and perception |
| `chaseEscape` | 20 t | Chase break distance |
| `patrolRadius` | 6 t | Idle patrol |
| `radioUpgradeSteel` / `radioUpgradeCopper` | 15 / 10 | Radio precision upgrade price |

### 8.3 Scheduling rules

| Rule | Behaviour | Source |
|---|---|---|
| Determinism | every roll from `raidChoice(seed, serial, range)` | `campaignThreat.ts` |
| Target | one announced base; first is always Home; later targets nominated among Home and commissioned plants | `campaignThreat.ts:55-65` |
| Approaches | best exterior tile per compass sector, up to 4; majors round-robin `origins`; harassment and the intro use one | `campaignThreat.ts:139-154`, `:72` |
| Staging safety | staging tile ≥ 28 t from the engineer, same compass heading, never inside a wall | `campaignThreat.ts:156-167` |
| No debt | a blocked assault is cancelled and re-warned as a fresh opportunity, never queued late or doubled | `campaignThreat.ts:66`, `future(...)` |
| Warnings | powered Radio warns; precision upgrade adds approach + composition; unpowered shows "last received warning (radio offline)" | `campaignThreat.ts:269`, `:325` |
| Phases | `approaching \| warning \| assault \| withdrawal \| minor raid \| recovery` | `campaignThreat.ts:294` |
| History | last 32 assaults retained | `campaignThreat.ts:48-49` |

### 8.4 Opening encounter — `packages/sim/src/openingEncounter.ts` `OPENING_ENCOUNTER`

| Constant | Value | Meaning | Kind |
|---|---|---|---|
| `warning` | 25 s | directional warning before the attack | provisional (matches owner's "~25 s") |
| `count` | 5 | basic attackers from one approach | provisional (owner's "~4–6") |
| `maxDuration` | 300 s | after which the group withdraws (removed at 2×) | provisional |
| `recovery` | 300 s | quiet on both major and minor clocks afterwards | provisional |
| `guard` | 660 s | `ACTIVE_RAIDS.warning (300) + recovery (300) + 60` — the window a major is deferred out of | provisional |
| `ack` | 60 s | "attack repelled / N bullets used" objective duration | provisional |
| `supplyAck` | 45 s | "automatic resupply working" objective duration | provisional |
| `supplyChainReaches` depth | 4 relays (`chest`, `tramstop`, `depot`) | how far a real delivery route may be chained | current |

### 8.5 Later raid tiers — design draft §13, **Approved Unity scope, not implemented**

**Source: Owner decisions 2026-09-11 (Q15, Q06).** The tier ladder is approved content, not a proposal awaiting a go-ahead (the reference project's GP-23 hold does not gate the port). The **counts and cadences below are provisional starting values**; the structural guarantees they must fit inside — one global director, one announced target, multiple approaches for larger attacks, finite reinforcements, readable warnings, recovery without accumulated debt — are approved and listed in [GAME_DESIGN.md §9.3.1](GAME_DESIGN.md#931-what-the-owners-decisions-fix-about-the-director).

| Tier | Cadence | Count | Window |
|---|---|---|---|
| Grace | 12–15 min | — | — |
| Early harassment | every 5–7 min | 6–10 | — |
| Established harassment | every 4–6 min | 10–18 | — |
| Late harassment | every 4–6 min | 16–26 | — |
| First major | 25–30 min | 50–70 | 2–3 min |
| Subsequent majors | 18–22 min | 90–140 | 3–4 min |
| Late majors | — | 160–220 | 4–5 min |

Only the first two rows match the implemented director (`ACTIVE_RAIDS`, §8.1); see §16. Notes for the port:

- **Counts are per assault**, not per wave, minute or site (design draft §13).
- **`activeRaidBudget = 48` and `livingBudget = 240` stay the readability and performance ceiling.** A later tier raises the roster and the overlap of groups inside the window, not the simultaneous-actor cap, until measurement says otherwise.
- **The tier-to-progress mapping is the implementer's provisional choice** (Q06): the draft leaves "the exact mapping of early/established/late tiers to restoration progress" open, and the expected anchor is commissioned plants and strongholds cleared. Record the mapping chosen; it is not a further owner decision.
- **The introductory encounter (§8.4) is not a tier.** It is separate from major progression and triggers on opening readiness, not elapsed time (Q13; GAME_DESIGN.md §4.3).
- The "Grace" row's 12–15 min is the draft's figure; the implemented `ACTIVE_RAIDS.grace = 780` s (13 min) already sits inside it.

---

## 9. Services, recruits and encounters

Also the authoritative home for guardians and camps (§§9.1–9.2).

### 9.1 First-region camps and the freight stronghold — `packages/sim/src/city/gameplaySites.ts`

| Site | Actors | Squads | Extra | Kind | Status |
|---|---|---|---|---|---|
| West passage camp (`freight:camp:1`) | 20 | 4 | repopulates every 900 s under safety conditions; Artifacts only, key stays claimed | provisional | Implemented and retained |
| Old utility camp (`freight:camp:2`) | 22 | 4 | — | provisional | Implemented and retained |
| Northwood approach camp (`freight:camp:3`) | 20 | 4 | — | provisional | Implemented and retained |
| Occupied freight depot (`FREIGHT_ARENA`) | 60 | 6 | + 1 Guardian (600 HP) at a fixed arena tile, behind 2 gates | provisional | Implemented and retained |
| Fourth eligible camp | — | — | not placeable in the first region; documented limitation | — | **Unresolved** |
| Quarry stronghold | provisional 75–100 | — | + 1 Guardian, **1,000 HP provisional** (Q17); site, key camps and rewards implementer-chosen | provisional | **Approved but not implemented — in Unity scope** (Q06/Q17; was GP-21) |
| Wharf stronghold | provisional 100–140 | — | + 1 Guardian, **1,500 HP provisional** (Q17); site, key camps and rewards implementer-chosen | provisional | **Approved but not implemented — in Unity scope** (Q06/Q17; was GP-22) |

Repopulation conditions (`firstRegion.ts:68-73`): all guards dead, reward already taken, ≥ 900 s elapsed, engineer > 48 t away, no player machine within 20 t of a spawn group, and world actors + camp ≤ 240.

### 9.2 Populations and guardians — design draft §§11–12, **Approved Unity scope, values provisional**

Populations (provisional): ordinary street 3–6; alley/ruin 8–14; patrol 4–8; first-region camp 18–26; later camp 28–42; first stronghold 50–70 + guardian; second 75–100; final 100–140; engaged 12–20 early, 20–30 later. Only the first-region camp sizes are implemented (§9.1). The draft's rule that **the guardian is additional to the listed defender total** carries across.

**Guardian starting HP — settled (Owner decisions 2026-09-11, Q17).**

| Guardian | Provisional starting HP | Encounter role | Source | Kind | Status |
|---|---|---|---|---|---|
| Freight | **600** | the first set-piece: a single heavy target that teaches commitment and retreat | `gameplayCombat.ts:15` `GP_COMBAT.guardian.hp = 600` (verified — code and draft agree) | approved value, provisional tuning | Implemented and retained |
| Quarry | **1,000** | a longer engagement that expects prepared ammunition and a second weapon | design draft §12; **Owner decisions 2026-09-11 (Q17)** | approved value, provisional tuning | **Approved but not implemented — in Unity scope** |
| Wharf | **1,500** | the final stronghold's climax, distinct from Quarry's, not a bigger Quarry | design draft §12; **Owner decisions 2026-09-11 (Q17)** | approved value, provisional tuning | **Approved but not implemented — in Unity scope** |

These three are **distinct encounters with preserved roles**, not one encounter scaled three times. Focused tuning is allowed **after** implementation under Q03, and **no further owner approval is needed before their approved encounters are coded**. They are stronghold encounters and are **not** part of the five-type regular roster (§7.4). They also do not silently replace the Junction Heart / Furnace Walker / Blackout Crown engineering encounters of §9.3 (design draft §12).

### 9.3 Services, recruits and encounters

| Item | Cost | Power | Effect | Source | Kind | Status |
|---|---|---|---|---|---|---|
| Commission a regional plant | 30 S + 15 Cu + 1 Power core | — | +600 kW, new defended base | `progression.ts` `CORRECTIONS.plantCost`, `plantKw` | current | Implemented and retained |
| First regional station | 30 S + 15 Cu | local power | unlocks the Truck; registers the base | `expansion.ts` | current | Implemented and retained |
| Later regional station | 40 S + 20 Cu | local power | restores later regional service | `expansion.ts` | current | Implemented and retained |
| Radio | 20 S + 10 Cu | 20 kW (`CAMPAIGN_POWER.radioKw`) | assault warnings | `campaignPower.ts`, `expansion.ts` | current | Implemented and retained |
| Radio precision upgrade | 15 S + 10 Cu | — | adds approach direction + composition | `CAMPAIGN_THREAT.radioUpgrade*` | current | Implemented and retained |
| Repair workshop | 25 S + 15 Cu | 40 kW | repairs nearby equipment from local supplies | `campaignDistricts.ts` | current | Implemented and retained |
| Turbine hall | 60 S + 30 Cu + 40 Concrete | — | +600 kW; no new assault base | `campaignTurbine.ts` | current | Implemented and retained |
| Gunsmith hopper upgrade | 20 S + 10 Cu per turret | — | ×1.25 hopper → 62 rounds | `CORRECTIONS.upgradeCost`, `hopperIncrease` | current | Implemented and retained |
| Rail crew freight upgrade | 20 S + 10 Cu once | — | ×1.25 tram cargo → 250 | `CORRECTIONS.freightIncrease` | current | Implemented and retained |
| Engineering encounter (each) | 30 S + 15 Cu | +100 kW while commissioning | see below | `CORRECTIONS.encounterCost`, `encounterKw` | current | Implemented and retained |
| — Junction Heart / Arsenal | 30 productive s | powered ordinary Poles west and east within 14 t | unlocks two-barrel shotgun, Cannon, Shell recipe | `CORRECTIONS.encounterSeconds[0]` | current | Implemented and retained |
| — Furnace Walker | 40 productive s | two busy powered processing machines within 14 t | 50 Steel delivered to Home | `encounterSeconds[1]` | current | Implemented and retained |
| — Blackout Crown | 50 productive s | two powered lights within 14 t | 50 Copper delivered to Home | `encounterSeconds[2]` | current | Implemented and retained |
| Alien equipment (site hazard) | — | — | range 7 t, 2 DPS | `CORRECTIONS.alienEquipmentRange/Dps` | provisional | Implemented and retained |

Recruits (`campaignRecruits.ts` `RECRUITS`, clue radius 24 t each, free to recruit): **Foreman** (blueprint clipboard, copy/paste, queued construction, truck construction work), **Electricians** (Floodlight, Big pole, Substation via `SURVIVOR_UNLOCKS`), **Concrete crew** (Mixer, Concrete, Barricade), **Lamplighters** (Arc lamp), **Surveyors** (permanent district-type map), **Gunsmith** (paid hopper upgrade), **Rail crew** (paid freight upgrade). All **Implemented and retained**.

---

## 10. Rewards and persistent progression

| Reward | Rule | Source | Kind | Status |
|---|---|---|---|---|
| **Alien Keys** | **3 per stronghold**, from up to 4 eligible camps where geometry allows | `gameplayProgress.ts` `strongholdProgress` → `required: 3`; design draft §2 | approved | Implemented and retained |
| Key IDs | `^<stronghold>:camp:[1-4]$`, must appear in `claimed`; one-time per source, forever | `gameplayProgress.ts` | current | Implemented and retained |
| Quest pouch | claims are persistent and survive death, loss and save/load (`claimed[]`) | `gameplayProgress.ts`; design draft §2 | approved | Implemented and retained |
| **Schematics** | `['overclock', 'arc', 'plasma']`; `learnedSchematics ⊆ recoveredSchematics`; learning is permanent | `gameplayProgress.ts` | current | Overclock implemented; Arc/Plasma **Approved but not implemented** |
| **Alien Artifacts** | shared crafting ingredient; repeatable camps yield Artifacts only | `firstRegion.ts:72`; `FABRICATION.artifactYield = 2` | current | Implemented and retained |
| **Power cores** | 3 cores, 3 plants; **any** core commissions **any** uncommissioned plant; installed cores survive a disabled plant | `progression.ts`; `CURRENT_GAMEPLAY_CATALOGUE.md` §5 | current | Implemented and retained |
| Core install cost | 30 S + 15 Cu + 1 core | `CORRECTIONS.plantCost` | current | Implemented and retained |
| **Home / base core** | 300 HP (`DEFENCE.coreHp`) | `campaignDefence.ts` | current | Implemented and retained |
| Ordinary core/machine repair | 40 HP for 2 S + 1 Cu in 4 s (`repairHp`, `repairSteel`, `repairCopper`, `repairSeconds`) | `campaignDefence.ts` `DEFENCE` | current | Implemented and retained |
| Disabled-core recovery | 10 S + 5 Cu in 12 s (`coreSteel`, `coreCopper`, `coreRepairSeconds`) | `campaignDefence.ts` | current | Implemented and retained |
| Manual repair speed | 4 s normal, 12 s disabled, × the Workshop discovery multiplier | `campaignDefence.ts` `manualRepairSeconds` | current | Implemented and retained |
| Single price source | `repairCost(st, x, y)` is the only repair pricing path (GP-HOME-REPAIR) | `campaignDefence.ts` | approved | Implemented and retained |
| Structure HP | Barricade 240, Wall 120, Turret 100, Cannon 140 | `DEFENCE`, `defenceMax` | current | Implemented and retained |
| Core-search / discovery radii | core search 28 t, discovery 14 t | `CORRECTIONS.coreSearchRadius`, `discoverRadius` | provisional | Implemented and retained |
| Overclock effect | ×1.1 processing on a supported machine; removable and reinstallable | `CORRECTIONS.artifactSpeed`; `processingMultiplier` | current | Implemented and retained |
| Introductory encounter reward | **none** — it is not a core award and grants no claim | `openingEncounter.ts`; design draft §2 | approved | Implemented and retained |

---

## 11. Milestones

| Milestone | Trigger | Source | Kind | Status |
|---|---|---|---|---|
| M0 | opening: first line, first turret, introductory attack | `goal.ts`; design draft §1 | approved | Implemented and retained |
| M1 | automated ammunition supply and a defensible Home | `goal.ts` step 13 | approved | Implemented and retained |
| M2 | **first** commissioned plant | design draft §1; GP_CHECKPOINT | approved | Implemented and retained |
| M3 | **second** commissioned plant; second stronghold; Plasma tier | design draft §1 | approved | Partly **Approved but not implemented** |
| M4 | third plant, city-scale production, final objective | design draft §1 | approved direction | Third plant and city-scale production **in Unity scope** (Q06); the **final objective stays Unresolved** (D-GP-13 / Q07 / Q02) — reaching M4 never sets victory |

`MILESTONE_EQUIPMENT = ['pumpjack', 'refinery', 'assembler2', 'alienworkbench', 'substation']` (`fabrication.ts`) is the equipment set the milestones gate on. **current**, Implemented and retained.

---

## 12. Power constants

| Constant | Value | Source | Kind | Status |
|---|---|---|---|---|
| `GENERATOR_KW` | 300 | `recipes.ts` | current | Implemented and retained |
| `COAL_MJ` | 4 MJ per Coal or Refined fuel item | `recipes.ts` | current | Implemented and retained |
| Generator burn | 4.5 items/min at 300 kW; 1.5/min at 100 kW | `goal.ts:228` `coalBurnPerMin` | current | Implemented and retained |
| `GENERATOR_COAL_CAP` | 50 | `flow.ts` | current | Implemented and retained |
| `CORRECTIONS.plantKw` | 600 per commissioned plant | `progression.ts` | current | Implemented and retained |
| Turbine hall | +600 kW | `campaignTurbine.ts`; `CURRENT_GAMEPLAY_CATALOGUE.md` §4 | current | Implemented and retained |
| `CAMPAIGN_POWER.coreKw` | 100 (Home core demand) | `campaignPower.ts:10` | current | Implemented and retained |
| `CAMPAIGN_POWER.radioKw` | 20 | `campaignPower.ts:10` | current | Implemented and retained |
| `POLE_REACH` | 8 t | `recipes.ts` | current | Implemented and retained |
| `BIG_POLE_REACH` | 12 t | `recipes.ts` | current | Implemented and retained |
| Substation reach | 8 t (`nodeReach` returns `POLE_REACH`) | `campaignPower.ts:20` | current | Implemented and retained |
| Link rule | node centre → linked **footprint**, not centre-to-centre | `campaignPower.ts` `nodesLinked`; D-GP-POWER-FIX | approved | Implemented and retained |
| `TURRET_KW` | 20 | `recipes.ts` | provisional (D-GP-TURRET-POWER) | Implemented and retained |
| `LAMP_KW` / `LAMP_RADIUS` | 5 kW / 4 t | `recipes.ts` | current | Implemented and retained |
| `ARC_LAMP_RADIUS` | 6 t (12 kW) | `flow.ts` | current | Implemented and retained |
| `FLOODLIGHT_KW` / `FLOODLIGHT_RANGE` / `FLOODLIGHT_HALF_ANGLE` | 40 kW / 12 t / π/6 (60° cone) | `recipes.ts` | current | Implemented and retained |
| `BROWNOUT_RULE` | `'proportional'` — every machine on a short circuit runs at the same fraction | `constants.ts`; D-B3-4 | approved | Implemented and retained |
| Conveyor power | belt, fast belt, underground, splitter, wall, barricade, chest, pole, big pole, substation all **0 kW** | `flow.ts` `MACHINE_KW` | approved (D-GP-TURRET-POWER) | Implemented and retained |
| Outage semantics | `BaseCore.poweredAt` stamped on first supply; no outage alert before supply has existed | `campaignDefence.ts` `tickBasePower`; D-GP-POWER-FIX addendum | approved | Implemented and retained |
| `SUBSTATION_KW` | `{ front: 100, interior: 20 }` | `constants.ts` | current | **Retired** (legacy) |
| Opening power budget rationale | core 100 + Assembler 100 + 3 turrets 60 = 260 of one 300 kW Generator | GP_CHECKPOINT "Powered turrets" | provisional | Implemented and retained |

---

## 13. Time constants

| Constant | Value | Source | Kind | Status |
|---|---|---|---|---|
| `TILE_TPS` / `TILE_DT` | 20 Hz / 0.05 s | `flow.ts` | current | Implemented and retained |
| Play speed | fixed 1× with pause | `session.ts`; D-UI-10 | approved | Implemented and retained |
| `CAMPAIGN_RULES.daySeconds` | 1200 s | `rules.ts:7` | current | Implemented and retained |
| `CAMPAIGN_RULES.daylightSeconds` | 900 s (night = last 300 s) | `rules.ts:7` | current | Implemented and retained |
| `campaignClock` | `day = floor(t / 1200) + 1`, `night = elapsed >= 900` | `rules.ts:17-20` | current | Implemented and retained |
| `CAMPAIGN_RULES.firstAssaultNight` | 3 | `rules.ts:7` | current | **Retired / stale** — the director schedules on elapsed active time (§8.1) |
| `CAMPAIGN_RULES.opening` | `'culdesac-v1'` | `rules.ts:7` | current | Implemented and retained |
| `FABRICATION.repeatSeconds` | 900 s camp repopulation | `fabrication.ts` | provisional | Implemented and retained |
| `FABRICATION.decodeSeconds` | 15 s workbench decode | `fabrication.ts` | provisional | Implemented and retained |
| `CORRECTIONS.openingMinorSlots` | `[900, 1080]` | `progression.ts` | provisional | Implemented and retained |
| `HOUR_MINUTES` | 75 | `constants.ts` | current | **Retired** (legacy hour clock) |

---

## 14. Engineer constants

| Constant | Value | Source | Kind | Status |
|---|---|---|---|---|
| `WALK_TILES_PER_S` | 6 | `engineer.ts` | current | Implemented and retained |
| `TRUCK_MULT` | ×3 walk speed | `engineer.ts` | current | Implemented and retained |
| `REACH` | 8 t | `engineer.ts` | current | Implemented and retained |
| `INV_STACKS` | 40 | `engineer.ts` | current | Implemented and retained |
| `ENGINEER_HP` | 100 | `engineer.ts` | current | Implemented and retained |
| `REGEN_HP_PER_S` / `REGEN_AFTER_S` | 5 HP/s after 5 s | `engineer.ts` | current | Implemented and retained |
| `RESPAWN_S` | 10 s | `engineer.ts` | current | Implemented and retained |
| `SPRINT_MULT` / `SPRINT_S` / `STAMINA_REFILL_S` | ×1.6 / 4 s / 6 s | `engineer.ts` | current | Implemented and retained |
| `DODGE_TILES` / `DODGE_S` / `DODGE_COOLDOWN_S` / `DODGE_COST` | 3 t / 0.25 s / 1 s / 0.25 | `engineer.ts` | current | Implemented and retained |
| `RETALIATE_HP_PER_S` | 5 | `engineer.ts` | current | Implemented and retained |

---

## 15. Starting inventory

| Profile | Contents | Source | Kind | Status |
|---|---|---|---|---|
| **Campaign (`exploration-v2`)** | `CAMPAIGN_START_POCKETS = { steel: 20, copper: 5 }` in the backpack. Home storage **empty**. No Generator, no fuel, no ammunition, **no weapon**. | `rules.ts:8-10` (GP-START-POCKETS, D-GP-START-POCKETS) | approved | Implemented and retained |
| Ledger treatment | the stake is placed **before** the conservation ledger opens, so it counts as opening stock | GP_CHECKPOINT "Starting Backpack stake" | current | Implemented and retained |
| First objective reads | Steel plates 20 / 30, Copper 5 / 10 (≈ 30 s of hand mining for the first Generator) | `goal.ts`; GP_CHECKPOINT | current | Implemented and retained |
| Legacy (`legacy-v1`) | `START_CHEST = { steel: 200, copper: 100, stone: 50, coal: 40, magazines: 20 }`, `START_COAL = 40`, `START_TURRETS = 6` | `constants.ts`, `recipes.ts` | current | **Retired** (legacy profile only) |

---

## 16. Values that disagree between documents and code

Recorded rather than reconciled, per the governing rule. "Authoritative" names the value a Unity implementer should use *today*; where that is itself disputed, the row says so.

| # | Subject | Code says | Document says | Authoritative | Note |
|---|---|---|---|---|---|
| 1 | **Turret fire rate** | `CAMPAIGN_TURRET_RATE = 1` shot/s (campaign); `TURRET.roundsPerS = 5` (legacy) | `CURRENT_GAMEPLAY_CATALOGUE.md` §3 defence row: "firing rate 5 rounds/second"; its own header note says 1 shot/s | **1 shot/s** for campaign | The catalogue's body contradicts its own header. Two live rate tables gated on `st.campaign`. 1 shot/s is the value already adopted in answer to the owner's report against 3.5; it is provisional with the **retest pending**, not rejected (U-P-05; GAME_DESIGN.md §12.2.3). |
| 2 | **Turret damage** | 10 per round (campaign); `TURRET.roundDmg = 4` (legacy) | GDD legacy appendix §13 uses 4 | **10** for campaign | Legacy constant left in `constants.ts`. |
| 3 | **Turret fire rate history** | 1 shot/s | `GP_CHECKPOINT.md` "Turret improvements" section says 3.5 shots/s | **1 shot/s** | The checkpoint contains an internally superseded value: its later "second pass" section corrects 3.5 → 1. |
| 4 | **Bullet stack size** | `STACK.magazine = 200` | `CURRENT_GAMEPLAY_CATALOGUE.md` §1 "Shot magazines and Shells: 20 per stack"; design draft §6 same | **200** | The document predates the one-item-one-bullet change (D-GP-PLAYTEST). Shells really are 20. |
| 5 | **Ammunition unit** | 1 item = 1 bullet (`ammoVersion 1`) | `CURRENT_GAMEPLAY_CATALOGUE.md` §1 "Shot magazine — contains 10 rounds" | **1 item = 1 bullet** | Same cause. The *recipe* still yields 10 items. |
| 6 | **First major assault timing** | `ACTIVE_RAIDS.firstMin = 1500` + up to 300 s of active time | `docs/CAMPAIGN_RULES.md` "First major assault: Night 3"; `rules.ts` `firstAssaultNight: 3` | **25–30 active minutes** | D-GP-START corrected D-GP-11 to elapsed-time cadence. The `firstAssaultNight` constant is vestigial. |
| 7 | **Minor raid size** | `6 + raidChoice(seed, …, 4)` → 6–10 | `docs/CAMPAIGN_RULES.md` "8–12 crawlers"; `CAMPAIGN_THREAT.minorMin/Max = 8/12` | **6–10** | The `CAMPAIGN_THREAT` pair is the superseded legacy band; the live band is computed in `campaignThreat.ts`. |
| 8 | **Major roster composition** | 60 total, ≈40 Skitter + 20 Spitter, up to 4 sectors | `docs/CAMPAIGN_RULES.md` "60 crawlers, one per 4 s"; design draft §13 "50–70" | **60, 40/20, 4 sectors** | The "one per 4 s" spacing is the legacy `spawnEvery`; the campaign spaces reinforcement across `window = 150 s`. |
| 9 | **Later raid tiers** | none | design draft §13 proposes 90–140 and 160–220 attacker tiers | **the draft tiers, provisional** | *Resolved 2026-09-11:* not a disagreement any more. Owner decisions 2026-09-11 (Q15, Q06) make the tiers approved Unity scope with the draft's counts as provisional starting values (§8.5). The code simply has not built them yet. |
| 10 | **Breaker HP** | `CORRECTIONS.breakerHp = 100` (`progression.ts:17`, verified) | design draft §11 approves **220** | **220** | *Resolved 2026-09-11:* Owner decisions 2026-09-11 (Q14) adopt **220** as the provisional baseline, replacing 100. A-2 closed. The code value is provenance for the retired role path only (§7.2, §7.4). |
| 11 | **Enemy roster** | Skitter / Spitter / Guardian live; Crawler / Shade / Hulk legacy; breaker/conductor/shade roles | design draft §11 approves Skitter / Stalker / Spitter / Breaker / Howler; D-GP-09 defers reconciliation | **The approved five types** (§7.4), guardians separate | *Resolved 2026-09-11:* Owner decisions 2026-09-11 (Q13, Q08) adopt the five-type roster, retire the legacy roster with `legacy-v1`, and retain two legacy-named mechanics named in GAME_DESIGN.md §9.1.1. A-1 closed. The brief's "Crawler/Shade/Conductor" was itself wrong — legacy's third type is **Hulk**. |
| 12 | **Turret power** | `TURRET_KW = 20` | design draft §6 "Gun turret … no electrical demand currently" | **20 kW** | D-GP-TURRET-POWER supersedes the draft. |
| 13 | **Player rifle model** | `WEAPON_PROFILES.rifle` 18/30 at 2.5/s, damage 10 | `engineer.ts` `RIFLE_RANGE = TURRET_RANGE` (9), `RIFLE_ROUNDS_PER_S = 1.5`; `GP_CHECKPOINT.md` "Rifle and reload follow-up" says 9 tiles, 1.5-tile half-width | **18/30 at 2.5/s** | The checkpoint's reload section predates its own range section by one revision. The `engineer.ts` constants are the retired legacy abstraction. |
| 14 | **Rifle range in the equipment table** | `equipment.ts RIFLE.range = WEAPON_PROFILES.rifle.effective` (18) | `GP_CHECKPOINT.md` snapshot table: "Rifle 10 damage, 9-tile range, 2.5 shots/s" | **18** | Same cause; the snapshot table is stale. |
| 15 | **Assembler Mk1 magazine rate** | 10 crafts/min (6 s each) | `flow.ts` comment block and GDD legacy tables describe Mk2's 20/min as the assembler rate | **10/min Mk1, 20/min Mk2** | `ASSEMBLER_MAG_PER_MIN` is explicitly the Mk2 value (project memory note). |
| 16 | **Frame and Board consumers** | Alien workbench build cost `{ frame: 4, board: 2 }` (`flow.ts:218`) + Overclock recipe `{ frame: 2, board: 1 }` (`flow.ts:184`) — both verified | `CURRENT_GAMEPLAY_CATALOGUE.md` §1: "No implemented consumer at present. Retained for future turret work." | **Workbench/Overclock only — and that is sufficient** | *Resolved 2026-09-11:* the reference document's line is simply stale (it predates the Alien workbench). Owner decisions 2026-09-11 (Q16): Frames and Boards keep exactly these uses, are **not** vestigial, and no advanced production chain is to be invented to give them a further consumer. A-6 closed. |
| 17 | **Foundry "Iron" output** | recipe id `iron` outputs `steel` | item list contains a separate `iron` item with stack 50 | **no separate finished Iron item** | `iron` is a vestigial legacy id. |
| 18 | **Substation availability** | buildable without a recruit; `SURVIVOR_UNLOCKS.Electricians` lists it | `CURRENT_GAMEPLAY_CATALOGUE.md` §3: "Available without the Electricians unlock" | **available without the unlock** | The unlock list also naming it is harmless but confusing. |
| 19 | **Camp count per stronghold** | 3 authored camps in the first region | design draft §2: "4 eligible camps where geometry allows"; 3 keys required | **3 keys from 3 camps in region 1** | The fourth camp is a documented placement limitation, not a rule change. |
| 20 | **Test state** | 318 of 464 sim tests pass | tracker rows read as complete | **318/464, 146 pre-existing legacy-fixture failures** | No human balance verdict exists (U-D-24). |

---

## 17. Runtime content scope (for B-05)

**Purpose.** B-05 generates Unity data assets from this catalogue. This section states which rows it may generate from, so that legacy and historical rows are never turned into shipping content. It is a *scope filter*, not a new data table: it adds no values and changes no row.

### 17.1 The rule

| Row's Status | Runtime content? |
|---|---|
| **Implemented and retained** | **Yes** — generate the asset, flag `provisional` where Kind says so |
| **Implemented but needs correction** | **Yes** — generate, and the correction is a task; never copy the defect silently |
| **Approved but not implemented** | **Only inside the scoped milestone.** Since Owner decisions 2026-09-11 (Q06) the value itself may be an implementer's **provisional** choice recorded with its reason — it no longer needs a per-value owner approval. Outside the scoped milestone, leave a named extension point and no asset |
| **Unresolved** | **No asset.** A number nobody approved must not be invented by the generator |
| **Retired** | **Never.** Historical evidence only |

Two consequences worth stating plainly:

- **No row in [§16](#16-values-that-disagree-between-documents-and-code) is runtime content.** §16 records disagreements between documents and code; it is provenance. Where §16 names an authoritative value, that value already appears in its own live row elsewhere in this catalogue, and *that* row is what B-05 generates from.
- **"Approved but not implemented" is not a licence to generate a value *silently*.** For those rows the generator produces the shape (an item type, a site record, an enum member) only when the milestone scopes it. *Amended 2026-09-11 (Q06):* the accompanying numbers may now be provisional implementer choices rather than owner-approved ones, but they must be written into this catalogue with a Kind of `provisional` and a reason **before** an asset carries them. The generator still never invents a recipe, cost or timing that no catalogue row states.

### 17.2 Explicit exclusion list — never generate runtime content from these

| Where | Rows | Why |
|---|---|---|
| §1 | `iron` (vestigial legacy id, §16 row 17); `artifact1/2/3` speed artifacts (converted to Overclock by `overclockMigration.ts`) | Retired ids |
| §1.1 | Field kit `KIT_STACKS` | Legacy HQ/front/kit model, retired with the territorial economy |
| §2 | Legacy start coal patch `START_COAL_PATCH = 700` | `legacy-v1` only |
| §4 | Track / Tram stop / Tram as **player-buildable** | The campaign tram is fixed; the player powers existing stops |
| §5 | Legacy save migration; legacy abstract rifle (`RIFLE_ROUNDS_PER_S`, `RIFLE2_ROUNDS_PER_S`, `RIFLE_RANGE = TURRET_RANGE`, `RIFLE_HIT_RADIUS`); energy cells as a *replacement* economy / per-shot Artifact cost | Superseded by `WEAPON_PROFILES` (U-M-06); see §5 for the three-part energy-cell status |
| §6.1 | Legacy 1 item = 10 rounds; the `ammunitionMigration` ×10 conversion | The Unity save format starts at integer bullets (U-D-08, U-M-06) |
| §6.2 | The whole **Legacy** column (5 rounds/s, 4 damage, 0 kW); legacy placement rules `EDGE_TURRETS`, `TURRET_PER_TILES`, `START_TURRETS` | Two live rate tables exist in the reference only because `legacy-v1` is kept for evidence fixtures; Unity ships one |
| §7.2, §7.3 | Legacy roster — Crawler, Shade, **Hulk** — and the no-gameplay-progress **role path** (breaker / conductor / shade roles, `CORRECTIONS.breakerHp/conductorHp/shadeHp`, `reinforcementSeconds`, `reinforcementLimit`) | `legacy-v1` is **retired from the port** (Owner decisions 2026-09-11, Q08). The creatures and the role path generate nothing; the two retained *mechanics* (×3 structure damage, light suppression) are generated from their approved §7.4 and §8.2 rows instead |
| §12 | `SUBSTATION_KW` | Legacy front/interior model |
| §13 | `CAMPAIGN_RULES.firstAssaultNight = 3`; `HOUR_MINUTES = 75` | Stale night-based schedule and the retired hour clock (§16 row 6) |
| §15 | Legacy `START_CHEST`, `START_COAL`, `START_TURRETS` | `legacy-v1` starting profile; the campaign profile is `CAMPAIGN_START_POCKETS` |
| §16 | **Every row** | Document-vs-code provenance, not content |

### 17.3 Deliberately scoped approved additions

These are *Approved but not implemented* and may be generated **only** when their task is scoped; until then the port leaves a named extension point and nothing else. **All four gates were owner questions and all four are now answered** (Owner decisions 2026-09-11), so what remains is milestone scope, not approval:

| Item | Task | Gate — status after 2026-09-11 |
|---|---|---|
| Energy cells — reusable-cell architecture (§5) | E-11 (GP-17) | **Open scope, no open approval.** Q06 puts the architecture in Unity scope and makes recipes, charge units and recharge demand/time **provisional implementer choices**; record them in §5 before generating. *(Was: "U-Q-06; numbers Unresolved".)* |
| Later raid tiers (§8.5) | E-16 (GP-23) | **Approved scope** (Q15, Q06). Counts and cadences provisional; the structural guarantees of GAME_DESIGN.md §9.3.1 are not. |
| Approved five-type enemy roster (§7.4) | E-13/E-14 (GP-19/GP-20) | **Approved scope** (Q13, Q14, Q15). Exactly five types; **Breaker 220**; guardians excluded from the roster; combat and population values provisional. *(Was: "U-Q-13 (A-1), U-Q-14 (Breaker HP)".)* |
| Camp populations and guardians (§9.2) | E-15 (GP-21/22) | **Approved scope** (Q06, Q17). Guardian HP Freight 600 / Quarry 1,000 / Wharf 1,500, provisional and needing no further approval before coding. Sites are still authored in Unity outside `Generated/` after handover and checked by the ported validator (U-M-15; WORLD_AND_ASSETS.md §4.7, §4.7.1). |

The most important row that stays **Unresolved** and therefore generates nothing at all is the **ending / campaign completion** (GAME_DESIGN.md §11, Q02): no victory state, no end screen, and no generator may treat `plants === 3` as a completion condition. The owner reviewed this on 2026-09-11 and deliberately left it open, so it is not waiting on a further question being asked. (The other standing Unresolved row, the fourth eligible camp of §9.1, is a placement limitation rather than a design gap.)

### 17.4 What a generated asset must carry

Each generated asset records, from its catalogue row: the **Kind** (`current` / `approved` / `provisional`) as a flag an editor can see and a validator can assert on, and the **Source** string. A `provisional` flag is what makes the permitted tuning auditable (U-M-12 as broadened by Owner decisions 2026-09-11 (Q03), GAME_DESIGN.md §12.2.5): the asset says the value is a starting point, and a change to it is expected to arrive with a recorded reason and a retest result. *Amended 2026-09-11:* that tuning permission now runs **throughout Unity playtesting**, not only through the playable-Home milestone, so the flag matters for the whole port rather than one phase.
