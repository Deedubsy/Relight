# Relight — Game Design

**Owner:** Worker A (gameplay, progression, recipes, machines, weapons, enemies, balance).
**Scope:** the experience and the rules. Every number lives in [CONTENT_CATALOGUE.md](CONTENT_CATALOGUE.md); this document links to it rather than repeating tables.

---

## 1. Purpose, status legend and relation to the reference documents

### 1.1 Purpose

This is the design-level description of Relight as it actually exists in the reference TypeScript implementation, plus the owner decisions that are approved but not yet built. It is written so a Unity implementer can rebuild the *behaviour* without reading the TypeScript, and so the owner can see where code and intent disagree.

It deliberately does **not** cover: Unity architecture, save format or tick host ([TECHNICAL_ARCHITECTURE.md](TECHNICAL_ARCHITECTURE.md)); world geometry, map authoring, tile and art assets ([WORLD_AND_ASSETS.md](WORLD_AND_ASSETS.md)); screens, panels, controls, HUD layout and the player-facing wording of the tutorial ([UI_AND_ONBOARDING.md](UI_AND_ONBOARDING.md)). Where a rule has both a mechanical and a presentational half, the mechanical half is here and the presentational half is named and handed over.

### 1.2 Status legend

Every fact below carries one label:

| Label | Meaning |
|---|---|
| **Implemented and retained** | In the reference code today and intended to carry into Unity unchanged. |
| **Approved but not implemented** | The owner approved the direction; no code exists (or only a partial prototype). |
| **Implemented but needs correction** | In the code, but the owner has said it is wrong (usually feel, not correctness). |
| **Unresolved** | No decision exists. Not to be invented by an implementer. |
| **Retired** | Existed once; explicitly superseded. Must not be rebuilt. |

Numbers carry one of three kinds, matching [DECISIONS.md](DECISIONS.md):

- **current** — the literal value in the reference code today.
- **approved target** — an explicit owner decision (e.g. "one bullet per second", "three turrets").
- **provisional** — not confirmed by owner play. Two things carry this tag: values chosen by an agent at the checkpoint (everything in `docs/Implementation/GP_CHECKPOINT.md` unless a `docs/DECISIONS.md` entry confirms it), and values the Unity implementer chooses for approved-but-unbuilt content under Owner decisions 2026-09-11 (Q03, Q06). A provisional value is always recorded with its source and reason; it is never invented silently.

`UNCERTAIN:` marks anything this pass could not verify from code or an owner decision.

One consequence of Owner decisions 2026-09-11 (Q06) for the **Approved but not implemented** label: it now means *approved Unity scope that has no code yet*, not *waiting for a per-feature owner go-ahead*. See §1.4.

### 1.3 Authority order used here

0. **Owner decisions 2026-09-11 (Q-numbers)** — the binding answers to this package's open questions, summarised in §1.4. Where they name a value or a scope, they win over code, checkpoint and draft alike.
1. **Code** = actual current behaviour (`packages/sim/src/**`).
2. **Latest explicit owner decisions** (`docs/DECISIONS.md`: D-GP-START, D-GP-PLAYTEST, D-GP-PLAYTEST-2, D-GP-TURRET-POWER, D-GP-POWER-FIX + addendum, D-GP-START-POCKETS, D-GP-HOME-REPAIR; owner brief 2026-09-11) = intended behaviour.
3. **Checkpoint report** `docs/Implementation/GP_CHECKPOINT.md` = what an agent built and with what provisional numbers.
4. **Design draft** `docs/Design/RELIGHT_PROGRESSION_AND_WEAPONS_DRAFT.md` = accepted direction plus explicitly labelled proposals.
5. **Older documents** (`docs/RELIGHT_CONFIRMED_GAMEPLAY.md`, `docs/CAMPAIGN_RULES.md`, `docs/CURRENT_GAMEPLAY_CATALOGUE.md`, the GDD legacy appendix) = historical unless confirmed by 1–3.

Where these disagree the disagreement is recorded, here in prose and in [CONTENT_CATALOGUE.md §16](CONTENT_CATALOGUE.md#16-values-that-disagree-between-documents-and-code). Nothing is silently reconciled.

### 1.4 Owner decisions, 2026-09-11 — what they settle

Source for every row: **Owner decisions 2026-09-11 (Q-numbers)**, the owner's binding answers to the open questions raised by this package (§15) and recorded in [DECISIONS.md](DECISIONS.md) §4. These are authorised; no further approval is sought for them. Each is stated once in the section named below, and this table is the index.

| Q | What the owner settled | Where it is stated |
|---|---|---|
| **Q02** | The ending stays **genuinely unresolved**. No ending is to be invented, and commissioning the third plant must never set victory by itself. Blocks final campaign-completion work only. | §5.3 (M4), §11, §14 row 17 |
| **Q03** | The latest implemented mining / ammunition / turret adjustments are the **provisional starting values**, verified against source. Focused, documented tuning is permitted **throughout Unity playtesting** — not only at the playable-Home milestone — without the owner approving each number. Useful automation is preserved; excessive manual waiting is avoided. | §3 (hands), §6.3–§6.4, §8.2, §12.2, §12.2.5, §15.1 |
| **Q04** | Art proceeds on **known reusable placeholder assets**. Third-party assets with unverified permissions stay out of the Unity build until the owner supplies source and licence. The owner's final art package is still pending and blocks neither foundation work nor a playable milestone. | Not a gameplay rule: WORLD_AND_ASSETS.md and U-M-10 / U-Q-04 own it. Named here and in §15.1 so no gameplay row claims to wait on art. |
| **Q06** | The **full approved gameplay direction is in Unity scope**: unfinished weapons, progression, enemies, strongholds and factory-defence systems. The reference project's "GP-15–25 held for checkpoint review" does **not** block that scope. The implementer may choose documented provisional recipes, costs and combat values; only a genuine unresolved **design** change needs another owner decision. This approves **planning** the work; coding starts on a separate implementation instruction. | §5.2, §5.3, §8.1, §9.1.1, §9.3.1, §13.2, §15.1 |
| **Q08** | The **`legacy-v1` ruleset is retired** from the port and preserved in the reference project only: no second ruleset, no legacy UI, no legacy test framework, and the legacy Crawler / Shade / Hulk roster does not come across. | §2.2, §9.1.1, §13.2, §15.1 |
| **Q13–Q15** | The approved regular roster is exactly **five types — Skitter, Spitter, Stalker, Breaker, Howler**; guardians are handled separately and are not one of the five. Obsolete legacy roster behaviour is excluded unless a named retained mechanic needs it. Intended roles and the later raid tiers are approved; combat and population values are provisional. **Breaker baseline HP is 220** (replacing the code's 100). The existing intended raid structure is preserved. The introductory encounter stays separate from major progression and is timed against actual opening readiness, not a clock. | §9.1.1, §9.2, §9.3.1, §4.3 (encounter), §15.1; CONTENT_CATALOGUE.md §7, §8.5 |
| **Q16** | **Frames and Boards keep their existing Alien workbench and Overclock uses.** They already have consumers; no advanced production chain is to be invented to close an outdated "no consumers" finding. Further consumers may arrive with other approved content. | §6.2, §15.1; CONTENT_CATALOGUE.md §1, §3, §16 row 16 |
| **Q17** | Stronghold guardian **provisional starting HP: Freight 600, Quarry 1,000, Wharf 1,500**, with distinct encounter roles preserved. Focused tuning is allowed after implementation; these values need no further approval before their approved encounters are coded. | §5.2, §15.1; CONTENT_CATALOGUE.md §9.2 |

---

## 2. Game identity and core loop

**Status: Implemented and retained.**

Relight is a single-player 2D top-down game about relighting one abandoned, alien-occupied river city, one block at a time. The player is a lone engineer on foot. The city is authored, not generated: a fixed 864 × 576-tile map with real roads, buildings, a tram line and four factory yards (`packages/sim/src/city/riverfront.ts`; geometry is WORLD_AND_ASSETS.md's subject).

The identity rests on four commitments that every rule below serves:

1. **Everything is physical.** Materials exist in a real inventory somewhere — the engineer's 40-slot backpack, a chest, a machine's buffer, a belt, a truck, a tram platform. Nothing teleports, and no counter stands in for goods (`packages/sim/src/ledger.ts:47` `heldItems` enumerates chest, buffer, ring, pockets, machines, belts, committed). *Implemented and retained.*
2. **Power is finite and local.** A machine runs only if it sits inside the coverage of a pole or substation on a circuit with live supply. There is no global energy pool (`packages/sim/src/campaignPower.ts:21` `campaignGrid`, `:61` `machineThrottle`). *Implemented and retained.*
3. **The sim owns all mutation.** Gameplay changes only through commands into the simulation; presentation never implies a mechanic (`CLAUDE.md` "Work and authority"; U-D-03). *Implemented and retained.*
4. **Play runs at 1× with pause.** No speed control, no fast-forward (`CLAUDE.md` "UI implementation"; U-D-04). *Implemented and retained.*

### 2.1 The loop

```
  explore a dark block  →  find salvage / a camp / a recruit / a core
        ↓                                    ↓
  mine and handcraft  →  build a small line  →  automate it
        ↓                                    ↓
  power it (generator → poles → machines)  →  light it
        ↓                                    ↓
  defend it (turrets fed by that line)  →  survive the announced assault
        ↓                                    ↓
  spend the surplus on the next block: a plant, a station, a recruit
```

The intended pressure is that **defence consumes the same production the player wants to spend on expansion**. Bullets are made from Steel and Copper; so are turrets, poles, generators and every restoration. A player who over-invests in defence stops expanding; a player who ignores it loses a base core and has to recommission it.

### 2.2 Rulesets

Two rulesets exist in the reference code (`packages/sim/src/rules.ts:1-10`):

- `CAMPAIGN_RULESET = 'exploration-v2'` — the live game. Requires save schema 3, city profile `riverside-v1`, opening `culdesac-v1`. **Implemented and retained.**
- `LEGACY_RULESET = 'legacy-v1'` — the earlier territorial "frontage / ring / hour-clock" game, kept only so historical evidence fixtures still run. **Retired** for the port (U-D-20, U-Q-08). Its constants remain physically present in `constants.ts` and `enemies.ts` and are a frequent source of stale numbers in older documents; see [CONTENT_CATALOGUE.md §16](CONTENT_CATALOGUE.md#16-values-that-disagree-between-documents-and-code).

**Owner decisions 2026-09-11 (Q08) settle this.** `legacy-v1` and its obsolete gameplay are retired from the Unity port and preserved **in the reference project only**. Unity ships **one** ruleset (`exploration-v2`): no second rules profile, no legacy UI, no legacy test framework, and no legacy enemy roster (§9.1). Unity therefore has no `rulesetProblem` equivalent to write — there is no second game to keep out — but the *validator idea* survives as a save-consistency check (MIGRATION_MAP.md S-06). Legacy constants are read in this package as provenance only; they never generate runtime content ([CONTENT_CATALOGUE.md §17.2](CONTENT_CATALOGUE.md#172-explicit-exclusion-list--never-generate-runtime-content-from-these)).

`rulesetProblem` (`rules.ts:33-49`) actively rejects legacy frontage, ring, engagements, heart and project state inside a campaign save, which is the code-level guarantee that the two games do not mix.

---

## 3. The engineer

**Status: Implemented and retained** except where noted. Values in [CONTENT_CATALOGUE.md §1 and §14](CONTENT_CATALOGUE.md).

The engineer is the only player-controlled body. There are no followers, squads, crews or garrison units. (Cosmetic work crews, a machine fortress and a five-day raid schedule belong to the owner's *separate* machine-survival game and are **out of scope** — see §13.)

**Physicality.** The engineer walks at `WALK_TILES_PER_S = 6` tiles/s, reaches `REACH = 8` tiles for building, mining and interaction, carries `INV_STACKS = 40` backpack slots, and drives a truck at `TRUCK_MULT = 3` × walk speed with `TRUCK_STACKS = 200` cargo stacks (`packages/sim/src/engineer.ts`). Stack sizes are per item (`STACK`, `stackSize`); weapons and power cores stack to 1.

**Survivability.** `ENGINEER_HP = 100`, regen `REGEN_HP_PER_S = 5` starting `REGEN_AFTER_S = 5` seconds after the last damage, and on death a `RESPAWN_S = 10` recovery at Home during which nothing is carried, mined or fired. Movement options are a sprint (`SPRINT_MULT = 1.6` for `SPRINT_S = 4` s, refilling over `STAMINA_REFILL_S = 6` s) and a dodge (`DODGE_TILES = 3` over `DODGE_S = 0.25` s, `DODGE_COOLDOWN_S = 1`, costing `DODGE_COST = 0.25` stamina). A dodge that overlaps an enemy's committed swing makes it miss (`packages/sim/src/stalker.ts` attack state; `gameplayCombat.ts` windup/charge phases). *Implemented and retained.*

**Hands.** The engineer has exactly two capabilities that do not need a machine:

- **Hand mining** at `HAND_MINE_PER_S = 0.5` items/s into the backpack (`packages/sim/src/flow.ts:210`). 0.5 is not an unanswered complaint: it is the *answer already given* to the owner's "too fast" report on the earlier 1.0, halved by GP-PLAYTEST-FIX 2 on 2026-09-11 (`flow.ts:210` comment; `docs/Implementation/GP_CHECKPOINT.md` second pass → "Hand pacing"). No source records an owner verdict on 0.5, so it is a **provisional starting point; retest pending** (U-P-01). *Implemented and retained at a provisional value.* Full chronology: §12.2.
- **Hand crafting** at the Home workshop: a batch of 10 bullets every `HAND_BULLET_SECONDS = 20` s from 2 Steel + 1 Copper (`flow.ts:158`, `:1037`). 20 s is likewise the answer already given (12 s → 20 s, GP-PLAYTEST-FIX 2), and is a **provisional starting point; retest pending** (U-P-02). *Implemented and retained at a provisional value.* Chronology: §12.2.

Hand crafting **locks the engineer in place**: while a batch runs, the mover, the weapon and the mining hand are all blocked, and cancelling (button, Escape, closing the panel, or leaving Home's reach) releases them at once and refunds the reserved inputs (`flow.ts:1007`, `:1465-1466`; `packages/game/src/panel.ts:712`). This is a **sim rule**, not a UI guard, and is owner decision (7). *Implemented and retained.*

**Weapons are owned items.** Each weapon is a distinct instance with an identity (`WeaponItem = \`${kind}:${serial}\``, `packages/sim/src/weaponTypes.ts`). The engineer has exactly **two physical equipment slots**; further weapons occupy ordinary backpack or storage slots like any other item (`packages/sim/src/equipment.ts:1-20`). Loaded rounds, reload progress and cooldown belong to the instance and survive transfer, storage and save. A fresh campaign starts with **no weapon at all**; the first Rifle is crafted at Home from 10 Steel + 4 Copper in 6 s (`equipment.ts` `RIFLE`), which requires the player to be near the Home depot (`equipmentCheck` → `nearDepot`). This is owner decision (1). *Implemented and retained.*

**Conservation.** `packages/sim/src/ledger.ts` maintains an auditable ledger: `openLedger` snapshots totals, `heldItems` re-counts every physical location, `ledgerFlows` accounts sources and sinks, and `conservation(st, tolerance = 0.01)` asserts the books balance. Transfers, reloads, cancellations and saves must never create or destroy items; a refused operation changes nothing on either side. This is owner decision (4) and U-D-05. *Implemented and retained.*

---

## 4. The opening

The opening has two parts that are easy to confuse: the **guidance chain** (the ordered objectives the goal system offers) and the **introductory encounter** (a one-time scripted attack). Both are sim-side; UI_AND_ONBOARDING.md owns how either is presented.

### 4.0 What the opening is for — the intended experience

**Status: Approved in direction** (owner decision 16 / U-D-17); its numbers are **provisional** (U-P-04).

The sentence the opening has to earn is:

> *"I built a turret, saw it protect me, automated its supply, and could then leave to explore."*

Everything in §4.1–§4.4 exists to produce that sentence, in that order. The agreed sequence (U-D-17; `docs/Implementation/GP_CHECKPOINT.md` "Opening tutorial"; owner brief 2026-09-11):

| # | Beat | What the player must actually experience | Label | Source |
|---|---|---|---|---|
| 1 | **First fully loaded operational Home turret** | Finished, not merely placed: powered, enabled, running **and** full (50/50, or 62/62 upgraded) | **IR** | `readyOpeningTurret` (`openingEncounter.ts`); §4.3 |
| 2 | **A short directional warning** | ~25 s of notice naming a compass direction, with the threat marker on the real exterior origin tile | **IR** | `OPENING_ENCOUNTER.warning = 25`; `openingOrigin`; `COMPASS` |
| 3 | **A small introductory attack from one suitable approach** | Five basic skitters from a single approach the prepared turret actually covers | **IR** | `OPENING_ENCOUNTER.count = 5`; `approachClearance` |
| 4 | **Actual ammunition-consumption feedback** | The acknowledgement names the real number of bullets spent and that one bullet is one item | **IR** (wording **INC**, §4.2) | `goal.ts:132`; owner decisions 5 and 16 |
| 5 | **Automatic replenishment demonstrated through real delivery** | The step clears only when a produced bullet batch reaches a turret along belts/inserters; hand loading never counts | **IR** | `supplyChainReaches`, `noteTurretSupply`; U-D-13 |
| 6 | **Expansion to three turrets covering different approaches** | Guidance points at an approach the existing turrets do not cover | **IR** | `turretRecommendation` (`goal.ts:70`) |
| 7 | **A useful nearby excursion while production continues** | Carry ≥ 8 bullets, reach the nearest freight camp, recover a site — with the base still producing behind the player | **IR** | `goal.ts:151`; §4.2 step 15 |

Labels: **IR** = implemented and retained · **INC** = implemented but needs correction · **ANI** = approved, not implemented.

Four clauses govern the sequence rather than being steps in it:

| Clause | Intended behaviour | Current code | Label |
|---|---|---|---|
| **A temporary active raid defers, it does not cancel** | A raid that happens to be live (or a major warning already running) **postpones** the introductory encounter until the player is safe; the player still gets their tutorial fight | `tickOpeningEncounter` **skips permanently** when a raid is live or the next major is inside `ACTIVE_RAIDS.warning = 300` s (`campaignThreat.ts:209`), and again at `scheduled → active` if a raid has begun. Only the `guard = 660` s case defers (`campaignThreat.ts:210`) | **INC** |
| **A genuinely progressed save may skip it** | A save that has already passed the opening never receives a retroactive tutorial attack | `initOpeningEncounter` marks progressed saves `'skipped'` — correct | **IR** |
| **Deferral must not duplicate encounters or postpone ordinary raids indefinitely** | At most one encounter per campaign; the deferral is bounded, and ordinary raids resume afterwards | `op.id` is a single director id, `openingBlocksRaids` gates the director, `recovery = 300` pushes both clocks once, and the 660 s defer is a single bounded push with a notice. A *re-scheduling* rule with an explicit bound does not exist yet and must be written | **ANI** (the bound) |
| **The fight must be readable and forgiving; three turrets are guidance, not a guarantee** | A small group, an automatic withdrawal, and a recoverable loss | `maxDuration = 300` withdrawal, `2 × maxDuration` hard removal, `lost` offers `Rebuild your turret`; the three-turret detail already says "a starting recommendation, **not guaranteed protection**" (GP_CHECKPOINT "Turret improvements") | **IR** |

**Playable-opening checks** (proposed for C-ACC — full wording in the coordinator proposals):

1. **A short excursion**: leave Home with ≥ 8 bullets, reach the nearest freight camp and return, with production still running on return.
2. **One representative subsequent ordinary attack at the chosen turret rate.** This is the reference project's single largest untested claim: GP_CHECKPOINT's second pass says in its own Checks line, *"Not verified: the first ordinary Home raid at 1 shot/s beyond the intro encounter."*
3. A **controlled** attack check may be staged rather than waited out. The reference's existing dev aids are the `session.ts` URL parameters (`?view=world`, `rules=`, `seed=`, `state=`, `autoplay=`, `stalker=1`, `heart=1` — `packages/game/src/session.ts:21-45`) and the headless defence-scenario harness (`packages/harness/src/defenceScenario.ts`, `defenceCli.ts`). **There is no raid-forcing URL parameter or debug command in the reference** (verified by reading `session.ts`); Unity therefore needs a purpose-built editor-only debug command for this check (**ANI**). Any staged fight is labelled a **controlled check**: it proves the mechanics and the readability of one attack, **not** natural pacing.
4. **No full campaign run is required** for the playable-opening milestone.

### 4.1 Starting state

**Status: Implemented and retained** (D-GP-START-POCKETS).

A new campaign begins in Founders Court, a 24 × 24-tile starter frame with a 4-tile court entrance, at day 1, daylight. The engineer's backpack holds `CAMPAIGN_START_POCKETS = { steel: 20, copper: 5 }` (`packages/sim/src/rules.ts:8-10`). Home storage is **empty**; there is no generator, no fuel, no ammunition and no weapon. The opening stake is placed before the conservation ledger opens, so it counts as opening stock rather than as created matter (GP_CHECKPOINT "Starting Backpack stake").

The first objective therefore reads "Steel plates 20 / 30, Copper 5 / 10" — roughly 30 s of hand mining to afford the first Generator.

### 4.2 The guidance chain

**Status: Implemented and retained.** Source: `packages/sim/src/goal.ts:82-167`. The chain is *guidance*, not a lock: any sensible construction order is legal, and a step the player has already satisfied is skipped.

| # | Objective (`goal.ts` id) | Gate that clears it |
|---|---|---|
| 1 | `opening-workshop` — build a Generator | a Generator exists |
| 2 | `opening-workshop` — build an Excavator | an Excavator exists |
| 3 | `opening-workshop` — build a Supply chest | a chest exists |
| 4 | `opening-workshop` — connect belts Excavator → chest | the Excavator has a chest receiver |
| 5 | `opening-workshop` — fuel the Generator | generator `inv.coal + inv.fuel > 0` |
| 6 | `opening-power` — connect extraction power | `powered(st, excavator)` |
| 7 | `opening-workshop` — start the line | excavator status `running` |
| 8 | `opening-power` — link Founders Court's substation | home block `supply > 0` |
| 9 | `opening-rifle` — craft a Rifle | `equipment.weapons` non-empty |
| 10 | `opening-equip` — equip it in slot 1 | a slot is occupied |
| 11 | `opening-workshop` — craft bullets, build a turret, power it, load it | a Home turret is powered, running and full |
| 12 | `opening-attack` — the introductory encounter (§4.3) | encounter `repelled` / `lost` / `skipped` |
| 13 | `opening-ammo` — automate the turret's ammunition supply | a produced **bullet batch** reaches a turret **through a real delivery route** |
| 14 | `opening-workshop` — build to **three turrets** covering different approaches, each loaded | three live loaded turrets |
| 15 | `opening-workshop` — "prepare to scout": carry ≥ 8 bullets, then the nearest freight camp | a progression site is recovered |

Steps 8, 11 and 13 carry the three lessons the owner asked for explicitly:

- **Step 8** teaches that power is a *connected network*, not a global pool (owner decision 11). The objective text names the 8-tile pole reach, the 12-tile Big pole reach and that "belts need no power".
- **Step 12's acknowledgement** states the actual ammunition consumed: "Your turret used N bullets. Each bullet is one ammunition item." (owner decisions 5 and 16). The same string's *detail* still reads "An Assembler set to **Shot magazines**" (`goal.ts:132`), as do the resupply steps (`goal.ts:138`, `:140`, `:143`, `:144`) and the empty-backpack message (`flow.ts:2142`). Under `ammoVersion 1` the player-facing item is **Bullets** (`itemNames.ts:6`), so those strings are stale: **Implemented but needs correction**. Unity must say *bullets* / *bullet batch* everywhere a player reads it; `magazine` survives only as the **internal item id** and `Shot magazine` only as the **internal recipe name** (§6.3).
- **Step 13** is only satisfied by `supplyChainReaches` (`packages/sim/src/openingEncounter.ts`), which walks belts, undergrounds, splitters and inserters through up to `depth = 4` relay containers (`chest`, `tramstop`, `depot`). Hand-loading a turret **never** records a supply chain (`noteTurretSupply`). This is owner decisions (12) and (16)'s "teach auto replenishment through real delivery". *Implemented and retained.*

The three-turret objective uses `turretRecommendation` (`goal.ts:68`) to point at an approach the existing turrets do not cover, so "three turrets" means three *different* approaches rather than three turrets in a row. **Approved target: three.** *Implemented and retained.*

### 4.3 The introductory encounter (GP-OPENING)

**Status: Implemented and retained**; all its numbers are **provisional** (U-P-04). Source: `packages/sim/src/openingEncounter.ts` and `packages/sim/src/campaignThreat.ts:201-238` (`tickOpeningEncounter`).

**State machine.** `OpeningStatus = 'pending' | 'scheduled' | 'active' | 'repelled' | 'lost' | 'skipped'`, stored on the defence state as `d.opening` with `{ version, status, shots, id?, turretId?, origin?, scheduledAt?, startsAt?, count?, endedAt?, suppliedAt? }`.

**Initialisation** (`initOpeningEncounter`). The encounter is offered only to a genuinely fresh campaign. It starts `pending` when *all* of these hold, otherwise `skipped`:

- `st.t === 0`, **or** no raid history, no raids started, no live major or minor raid, the next scheduled major is at least `OPENING_ENCOUNTER.guard = 660` s away, fewer than 3 live turrets, and no progression site recovered or plant installed.

This is the "recognise early-completed steps and progressed saves" requirement of owner decision (16): a save that has already passed the opening never receives a retroactive tutorial attack.

**`pending` → `scheduled`.** The trigger is `readyOpeningTurret(st)`: the first Home turret that is `machineRunning` (powered, enabled, supplied) **and** `turretFull` (rounds ≥ hopper capacity − ε, i.e. 50 of 50, or 62 with the Gunsmith upgrade). On that trigger:

- If a major or minor raid is live, or the next major is closer than `ACTIVE_RAIDS.warning = 300` s, the encounter is **skipped** with a notice (`campaignThreat.ts:209`). No overlap, ever. **This is the one divergence from the intended experience (§4.0): a *temporary* active raid should defer the introductory encounter until the player is safe, not delete it.** Only a genuinely progressed save should lose it. **Implemented but needs correction**; the Unity rule is in §4.0 and in the proposed raid-behaviour task.
- If the next major is closer than the `guard = 660` s window, the **major is deferred** so that the encounter plus its recovery finishes first, and the player is told by how many active minutes (`campaignThreat.ts:210`). This is the shape the live-raid case should also take, with an explicit bound so nothing is postponed indefinitely.
- `openingOrigin` picks, from the director's ordinary Home approaches, the one the prepared turret covers best (`approachClearance`, the closest the shortest breach path passes to the turret's centre). If no reachable exterior approach exists the encounter is **skipped** rather than spawned somewhere unfair.
- Otherwise: `status = 'scheduled'`, `startsAt = now + OPENING_ENCOUNTER.warning (25 s)`, `count = OPENING_ENCOUNTER.count (5)`, and the threat marker is placed on the exterior origin tile so the warning is directional.

**`scheduled` → `active`.** At `startsAt`, if a raid has begun in the meantime the encounter is **skipped** — the same **Implemented but needs correction** case as above; the intended behaviour is to re-schedule once the raid and its recovery are over. Otherwise it borrows the director's **minor-raid slot** (`d.minor.id === op.id`) so that bodies, retreat, validation and cleanup follow ordinary raid rules, and `count` basic enemies are born at the staging tile. If none can be born (no open ground), it retries for 60 s and then skips. `op.count` is corrected to the number actually born.

**`active` → `repelled` / `lost`.** The encounter ends when its minor-raid slot clears. If the body is still alive after `maxDuration = 300` s it is ordered to withdraw; if it is still present after `2 × maxDuration` it is removed outright so it can never hold the raid slot forever. `status` becomes `'lost'` if the Home core is at 0 HP, otherwise `'repelled'`. On ending, `recoveryUntil` and `nextMinor` are pushed out by `recovery = 300` s and, if no major is live, the next major start is pushed to `recoveryUntil + warning`. This is the "allow recovery" requirement.

**No duplicate encounters or rewards.** `op.id` is a single director id; `openingBlocksRaids` (`scheduled` or `active`) stops the director scheduling anything else; the encounter is scheduled **at most once per campaign** and grants no reward of its own — it is not a core award and does not touch progression claims (owner decision 16; design draft §2 "opening ≠ core award"). *Implemented and retained.*

**Announcement and acknowledgement windows.** `ack = 60` s after the encounter ends the "attack repelled, you used N bullets" objective stays up; `supplyAck = 45` s for "automatic resupply working" after the first belt-delivered bullet batch arrives. Presentation of both is UI_AND_ONBOARDING.md's.

**Numbers.** `OPENING_ENCOUNTER = { warning: 25, count: 5, maxDuration: 300, recovery: 300, guard: 660, ack: 60, supplyAck: 45 }`. The owner asked for "~25 s" and "~4–6 basic enemies": 25 and 5 sit inside that, so the encounter matches the decision, but the values remain **provisional** until confirmed. The source comment explains `count` as "skitters (20 HP each, 10 damage per turret round → one fifth of a full 50-round hopper at 1 round/s)", i.e. the encounter is sized to consume **about 10 of the 50 rounds — roughly 10 seconds of firing** — and make the consumption legible. Note that this "about ten seconds" is the *encounter's* length, **not** the hopper's endurance: a full hopper is ≈ 50 s of continuous fire (§6.3).

**Owner decisions 2026-09-11 (Q13) — the encounter stays separate from major progression.** Its scheduling protections above are confirmed as approved design, not provisional scaffolding: no overlap with a raid, a bounded deferral of the next major, one encounter per campaign, no reward, and no retroactive attack on a progressed save. Two consequences for the port. First, this encounter is **not** the first rung of the major-assault ladder of §9.3.1; changing it does not move `firstMin`, and the director's 25–30 minute first major stands whether or not the encounter fired. Second, its timing is judged **against actual opening readiness, not against a clock**: the live trigger is already `readyOpeningTurret` — a powered, supplied, *full* Home turret — and that readiness test, not an elapsed-time threshold, is what the Unity port must keep. If playtesting shows the encounter arriving too early or too late, the fix is to adjust what counts as ready (and the 25 s warning and count of 5, both provisional and tunable under Q03), not to bolt a fixed timer onto it.

### 4.4 Raid-director interaction, summarised

| Situation | Behaviour | Intended (§4.0) | Source |
|---|---|---|---|
| Raid live, or major within 300 s | encounter **skipped** | **defer** until the raid and its recovery are over, then schedule once — bounded, never indefinite (**INC**; owned by TASKS.md **C-08**, minimal raid behaviour) | `campaignThreat.ts:209` |
| Raid begins during the 25 s warning | encounter **skipped** | **defer**, same rule (**INC**) | `campaignThreat.ts` `scheduled → active` |
| Major within 660 s | major deferred, player told | unchanged (**IR**) | `campaignThreat.ts:210` |
| Encounter scheduled or active | director schedules nothing | unchanged (**IR**) | `openingEncounter.ts` `openingBlocksRaids` |
| Encounter ends | 300 s recovery on both major and minor clocks | unchanged (**IR**) | `campaignThreat.ts:235-238` |
| Progressed save loaded | `status = 'skipped'` at init | unchanged — this is the *only* case that should permanently skip (**IR**) | `openingEncounter.ts` `initOpeningEncounter` |

---

## 5. Progression

The campaign's progression is **four separate persistent currencies plus milestones**, deliberately distinct so that one cannot be farmed into another. Source of the model: `CLAUDE.md` handoff 2026-09-10 and `docs/Design/RELIGHT_PROGRESSION_AND_WEAPONS_DRAFT.md` §§1–3; code in `packages/sim/src/gameplayProgress.ts` and `packages/sim/src/progression.ts`.

### 5.1 The four currencies

| Currency | Grants | Persistence rule | Status |
|---|---|---|---|
| **Alien Key** | *Access.* Three keys open one stronghold. | `claimed[]` holds one-time source IDs; a key is claimed once per source forever. | Implemented and retained |
| **Schematic** | *Knowledge.* Recovered, then learned; learning is permanent and survives death and loss of the item. | `recoveredSchematics ⊆` world, `learnedSchematics ⊆ recoveredSchematics` (validator). | Implemented and retained |
| **Alien Artifact** | *A shared crafting ingredient*, not an upgrade in itself. | An ordinary stacking item (stack 20). | Implemented and retained |
| **Power core** | *Commissions a plant.* Any recovered core can commission any uncommissioned prepared plant. | Installed cores survive the plant being disabled. | Implemented and retained |

Plus **Overclock modules**: finished, removable equipment crafted at the Alien workbench from 2 Artifacts + 2 Frames + 1 Board + 4 Wire in 20 s, giving a supported machine +10 % processing speed; removable and reinstallable without loss (`packages/sim/src/flow.ts` `ASSEMBLER_RECIPES.overclock`; `progression.ts` `processingMultiplier`). **Implemented and retained.**

**Artifact analysis is Retired** (`CLAUDE.md` handoff 2026-09-10) — artifacts are no longer consumed per shot or analysed into upgrades. The legacy `artifact1/2/3` "speed artifact" items still exist as world rewards and convert to Overclock modules on migration (`overclockMigration.ts`); in a Unity build that starts after conversion, the old three-artifact upgrade is **Retired** (U-D "Retired in the port").

### 5.2 Strongholds, camps and keys

**Status: first stronghold Implemented and retained; second and third Approved but not implemented — and in Unity scope** (Owner decisions 2026-09-11 (Q06, Q17); GP-21/22 in the reference project's own list).

Three strongholds are declared: `STRONGHOLDS = ['freight', 'quarry', 'wharf']` (`gameplayProgress.ts:1`). Each needs **3 Alien Keys** (`strongholdProgress` returns `required: 3`), drawn from up to **four eligible exterior camps** where the map geometry allows — the fourth is a documented placement limitation in the first region, not a design change (GP_CHECKPOINT). Key IDs are validated against `^<stronghold>:camp:[1-4]$` and must appear in `claimed`; an `opened` stronghold with fewer than 3 keys and no `inherited` flag is an invalid save.

The **first region** (freight) is fully authored (`packages/sim/src/city/gameplaySites.ts`):

- Three exterior camps — West passage (20 actors), Old utility (22), Northwood approach (20) — each spread over four squads, with roughly one Spitter per five actors.
- The **Occupied freight depot** arena: 60 defenders across six squads plus one **guardian** (600 HP) at a fixed arena position, behind two gates.
- The West passage camp **repopulates** every `FABRICATION.repeatSeconds = 900` s, but only when the engineer is more than 48 tiles away, no player machine is within 20 tiles of a spawn group, and the world actor count plus the camp would stay ≤ 240. Its repeat yields **shared Artifacts only** — "its unique key remains secured" — so key claims are never farmable (`firstRegion.ts:68-73`). *Implemented and retained.*

Quarry and Wharf strongholds are declared in the progression model but their sites, guardians and key camps are **Approved but not implemented**. **Owner decisions 2026-09-11 (Q06, Q17):** they are approved Unity scope, not work waiting on a per-feature go-ahead. The reference project's GP-21/GP-22 "held for checkpoint review" status does not block them. Their populations, layouts and rewards are **provisional** and chosen by the implementer with the reason recorded ([CONTENT_CATALOGUE.md §9.2](CONTENT_CATALOGUE.md#92-proposed-populations-and-guardians--design-draft-1112-approved-but-not-implemented)); only a genuine change of *design* (a different encounter concept, a new progression rule) needs a further owner decision.

**Guardian starting HP — approved provisional values** (Owner decisions 2026-09-11 (Q17); design draft §12 "Stronghold guardians"): **Freight 600** (matching `GP_COMBAT.guardian.hp = 600`, `packages/sim/src/gameplayCombat.ts:15`), **Quarry 1,000**, **Wharf 1,500**. Each keeps a distinct encounter role — Freight a slow charging creature that breaks cover, Quarry an armoured form with an exposed weak point after a heavy attack, Wharf ranged arena control with telegraphed danger zones — and *readable attacks, positioning and preparation*, not HP alone, carry the difficulty (design draft §12). These three numbers need **no further approval before their encounters are coded**, and focused tuning after implementation is permitted (Q03). The guardian is **additional to** its stronghold's defender total, and its support groups come out of that total rather than from an unbounded extra source.

### 5.3 Cores, plants and milestones

**Status: Implemented and retained.**

Three portable **Power cores** are recovered from three occupied relays (freight depot, quarry works, waterfront warehouse). Three prepared **regional plants** (Riverside Works, Ironworks, Civic Utility) each accept any uncommissioned core plus 30 Steel + 15 Copper and then supply **600 kW** and become a defended regional base (`progression.ts` `CORRECTIONS.plants = 3`, `plantKw = 600`, `plantCost`). Commissioning a plant creates a new attack-eligible base; switching a plant off does not remove its eligibility.

Milestones M0–M4 (design draft §1) key to **distinct commissioned plants**, not to time:

| Milestone | Meaning | Status |
|---|---|---|
| M0 | Opening: hand tools, first line, first turret, introductory attack | Implemented and retained |
| M1 | First automated ammunition supply and a defensible Home | Implemented and retained |
| M2 | **First** commissioned plant (first core recovered from a stronghold) | Implemented and retained |
| M3 | **Second** commissioned plant; second stronghold; Plasma schematic tier | Partly Approved but not implemented — **in Unity scope** (Q06); the second stronghold is §5.2's Quarry |
| M4 | Third plant, city-scale production and the final objective | Third plant and city-scale production are **in scope**; the **final objective stays Unresolved** (Q02, §11). Reaching M4 never sets victory |

`SCHEMATICS = ['overclock', 'arc', 'plasma']` (`gameplayProgress.ts:2`) is the knowledge ladder across those milestones. Overclock is implemented end-to-end; Arc and Plasma have runtime weapon behaviour but **no acquisition route** (GP_CHECKPOINT "Distinct weapon ranges": "Arc/Plasma had no existing acquisition implementation"). That is **Approved but not implemented and in Unity scope** (Owner decisions 2026-09-11 (Q06); GP-17/GP-18 in the reference list). Building the two acquisition routes — where each schematic is recovered, what learning it costs, what the weapon is crafted from — is authorised; the recipes and costs are **provisional**, chosen and documented by the implementer, and neither weapon is to remain dead content in the port.

### 5.4 Services, recruits and encounters

**Status: Implemented and retained.** Costs are tabulated in [CONTENT_CATALOGUE.md §9](CONTENT_CATALOGUE.md#9-services-recruits-and-encounters).

- **Recruits** are found and recruited locally at shelters within a 24-tile clue radius; recruiting itself costs nothing, but the upgrades some recruits enable have their own prices (`packages/sim/src/campaignRecruits.ts:10` `RECRUITS`). They grant permanent plans: Foreman (blueprints, queued construction, truck construction work), Electricians (Big pole, Floodlight), Concrete crew (Mixer, Concrete, Barricade), Lamplighters (Arc lamp), Surveyors (district-type map), Gunsmith (paid +25 % turret hopper), Rail crew (paid +25 % tram capacity).
- **Restorations** — first regional station (unlocks the Truck), later station, Radio and its precision upgrade, Repair workshop, Turbine hall (+600 kW) — are paid restorations of existing world installations, not new build-menu entries.
- **Three engineering encounters** (Junction Heart / Arsenal, Furnace Walker, Blackout Crown) each cost 30 Steel + 15 Copper, add 100 kW of commissioning demand and complete over 30 / 40 / 50 **productive** seconds, measured only while conditions hold and defenders are clear. Interrupted progress and paid materials are retained. Arsenal unlocks the two-barrel shotgun, the Cannon and the Shell recipe; Furnace and Crown deliver 50 Steel and 50 Copper to Home respectively.

**Persistent unlocks rule.** All of the above are permanent once earned: claimed source IDs, stronghold access, learned schematics, recruited specialists, paid upgrades and installed cores survive death, base damage and save/load. Damage can *disable* a site without deleting its installed core, equipment or recruits (`CURRENT_GAMEPLAY_CATALOGUE.md` §5; `campaignDefence.ts` recommission path). *Implemented and retained.*

---

## 6. Production and logistics

**Status: Implemented and retained.** All values in [CONTENT_CATALOGUE.md §§3–4](CONTENT_CATALOGUE.md#3-recipes).

### 6.1 Resources

Six raw/basic materials (Steel plates, Copper, Stone, Coal, Iron ore, Copper ore) plus Crude oil. Solid resources come from finite salvage and resource patches, served by hand or by an Excavator; Crude oil is pumpjack-only and cannot be hand-mined. **Deposits are finite** — an Excavator does not create an endless supply, and `CORRECTIONS.resourceUnits = 12000` bounds a patch. Iron ore and Copper ore are *separate items* from the directly usable Steel plates and Copper found in salvage; a Foundry converts 2 ore → 1 plate in 3 s.

### 6.2 Recipes and machines

Ten production recipes across Foundry, Refinery, Assembler, Assembler Mk2, Mixer and the Home workshop, plus the Alien workbench's Overclock module. Assembler Mk2 runs assembler recipes at **2×**; an installed Overclock/artifact multiplies a supported machine by **1.1** (`progression.ts` `processingMultiplier`). Machines buffer up to `ASM_INPUT_MULT = 4` crafts' worth of each input and hold an `ASM_OUTPUT_CAP = 5`-item finished buffer — except bullets under the current ammunition model, where the output cap is 50 (`flow.ts:955`). A blocked output stops production, which is the intended reason to build belts rather than let a machine idle.

**Frames and Boards are not vestigial** (Owner decisions 2026-09-11 (Q16)). Both have real consumers in the code today: the **Alien workbench costs 4 Frames + 2 Boards to build** (`packages/sim/src/flow.ts:218` `MACHINE_COST.alienworkbench = {steel:30, copper:10, frame:4, board:2}`) and the **Overclock Module recipe consumes 2 Frames + 1 Board + 4 Wire + 2 Alien Artifacts** (`flow.ts:184`). The owner's decision is explicit: those uses are kept, and **no advanced production chain is to be invented** merely to answer the outdated "no implemented consumer" note in `docs/CURRENT_GAMEPLAY_CATALOGUE.md` §1 — a note that predates the Alien workbench. Additional consumers may arrive with other approved content (for example a later turret or weapon tier) where that content genuinely needs them; they are never added to give an item a job. Any text in this package that called Frames or Boards vestigial, or that demanded a new consumer, is withdrawn ([CONTENT_CATALOGUE.md §1, §3, §16 row 16](CONTENT_CATALOGUE.md#1-items)).

### 6.3 Ammunition is the spine

**Owner decision (5): one ammunition item = one bullet; stacks hold many. Implemented and retained.**

`flow.ts:156` `ammoUnit(st) = st.flow?.ammoVersion === 1 ? 1 : 10` — a campaign at `ammoVersion 1` counts single bullets. A backpack stack holds `STACK.magazine = 200` bullets (`engineer.ts`). One craft yields 10 bullets: 2 Steel + 1 Copper in 6 s in an Assembler (3 s in Mk2), or in 20 s by hand. A Gun turret hopper holds 50 (62 upgraded). The word "magazine" survives as the **internal item id** (`flow.ts:155` `Item`) and as the **internal recipe name** (`flow.ts:175` `RECIPES.find(r => r.name === 'Shot magazine')`); the player-facing name is **Bullets** (`itemNames.ts:6`). Player-facing text that still says "Shot magazines" or "Magazines" is stale and is listed in §4.2.

#### How long a full turret actually lasts

| Quantity | Value | Source |
|---|---|---|
| Campaign turret cadence | **1 shot/s** | `CAMPAIGN_TURRET_RATE = 1` (`packages/sim/src/turretTracking.ts:4`) |
| Gun turret hopper | **50 rounds** (62 with the Gunsmith upgrade) | `constants.ts:42` `TURRET.hopper = 50`; `progression.ts:50` `hopperCapacity`, `CORRECTIONS.hopperIncrease = 1.25` → `floor(50 × 1.25) = 62` |
| **Full hopper, continuous fire** | **≈ 50 s (≈ 62 s upgraded)**, before any modifier | 50 ÷ 1 = 50 |
| Introductory encounter | ≈ 10 rounds ≈ 10 s of firing | `openingEncounter.ts` source comment; §4.3 |

`docs/Implementation/GP_CHECKPOINT.md` (second pass, "Turret cadence") states the same figure directly: *"a 50-round hopper now lasts 50 s of continuous fire instead of about 14 s."*

**Correction (2026-09-11).** An earlier revision of this section said "a full turret is about 10 seconds of sustained fire at the defended rate". That is wrong by a factor of five: **about ten seconds is the introductory encounter's ten shots**, not the endurance of a full 50-round hopper. Every downstream inference drawn from the ten-second figure — in this document and in `CONTENT_CATALOGUE.md` — has been re-derived from 50 s.

The claim the design can actually support is narrower: **a hand-loaded turret is a finite reserve.** 50 rounds is under a minute of *continuous* fire, and real raids arrive repeatedly and from more than one approach, so a player who only hand-loads spends the rest of the campaign walking bullets to turrets. That is what makes automation (§6.4) the answer — not a shortage inside a single fight.

#### What the cadence change did and did not change

Four consequences follow from the 3.5 → 1 shot/s change, and it is worth stating them precisely because the ten-second error made the fourth one look automatic:

1. **DPS and defensive capacity fell.** At 10 damage per round, one turret went from 35 damage/s to 10 damage/s (`turretTracking.ts:4`; damage per round unchanged, GP_CHECKPOINT "Turret cadence": no damage, enemy-HP or wave compensation was applied). Fewer enemies die per second, so more of them reach the base — that is a **defensive-capacity** change.
2. **Endurance rose.** The same hopper now covers ≈ 50 s instead of ≈ 14 s, so the turret runs *dry* less often (GP_CHECKPOINT, same passage).
3. **Bullets consumed per enemy killed did not change.** A 20 HP skitter still costs two 10-damage rounds whatever the cadence. Rate of fire changes *when* bullets are spent, not *how many* an enemy costs. One item = one bullet (U-D-08) is an **accounting rule** about how ammunition is represented and transferred, not a production-rate rebalance.
4. **Therefore "slower turret ⇒ less ammunition demand ⇒ automation matters less" does not follow**, and no compensating resource drain or manual chore should be invented to restore it. Useful automation rests on real demand over a campaign: how many attacks arrive and how large they are (§9.3), how many turrets cover how many approaches (§4.0 beat 6), how far the delivery route runs, what reserve the player wants to hold before leaving, and how far exploration and expansion take the player from the workshop. Legitimate stockpiling and automatic fuel delivery stay valuable for exactly that reason (U-D-13; §6.4) — the point of a belt into a turret is that the player is *elsewhere*.

### 6.4 Logistics, and why automation must pay

**Owner decision (10): automation must give a useful advantage without the opening being a waiting exercise. Implemented but needs correction.**

Belts move 7.5 items/s (Fast belts 15) and cost 1 Steel per tile; they draw **no power** (`MACHINE_KW.belt = 0`, `fastbelt = 0`; D-GP-TURRET-POWER explicitly keeps conveyors free). Inserters are *optional*: belts load and unload compatible machine and storage inventories directly (D-UI-12). Underground belts (4 hidden tiles) and priority splitters with filtering complete the set.

The intended advantage is arithmetic:

| Route | Bullets per minute | Turret-seconds of fire per minute (at 1 shot/s) | Source |
|---|---|---|---|
| By hand | 30 (10 per 20 s) | 30 | `HAND_BULLET_SECONDS = 20` (`flow.ts:158`) |
| One Assembler Mk1 on the Shot recipe | 100 (`ASSEMBLER_MK1_MAG_PER_MIN = 10` crafts × 10) | 100 | `constants.ts` |
| One Assembler Mk2 | 200 (`ASSEMBLER_MAG_PER_MIN = 20` crafts × 10) | 200 | `constants.ts` |

So one Assembler is ~3.3× the hand rate and Mk2 ~6.7×, while the Assembler also frees the engineer to move (hand crafting locks them in place, §3). At 1 shot/s a single Mk1 keeps **one** turret in continuous fire with 40 bullets/min to spare, and refills a 50-round hopper in 30 s — which is why three turrets on different approaches (§4.0 beat 6) is where a single hand-crafter stops being able to keep up.

**What is actually owed here.** Owner decision 10 asks that automation give a useful advantage *without the opening becoming a waiting exercise* — and the two requirements pull against each other, which is why this row is **Implemented but needs correction** rather than settled. Hand mining (1.0 → 0.5 items/s) and hand bullets (12 s → 20 s) were slowed once already under GP-PLAYTEST-FIX 2 in answer to the owner's report, and the same checkpoint records the *opposite* pressure arriving afterwards: GP-START-POCKETS granted a 20 Steel + 5 Copper starting stake because the owner asked "can the player start with 20 steel and 5 copper just to make the first mining experience not tedious" (`docs/Implementation/GP_CHECKPOINT.md` "Starting Backpack stake"; `rules.ts:8-10`). **No source records an owner verdict on the post-fix values**, so the port carries them as provisional starting points and retests them (§12.2, U-P-01/U-P-02, U-M-12, U-Q-03) rather than inventing new ones — and it does not manufacture demand with added drains or chores (§6.3).

**Fuel comes from real supplies (owner decision 12). Implemented and retained.** A Generator holds `GENERATOR_COAL_CAP = 50` combined Coal/Refined fuel and burns at actual load: `COAL_MJ = 4` MJ per item means a 300 kW Generator at full load burns 4.5 items/min, and 1.5/min at 100 kW (`goal.ts:228` `coalBurnPerMin`). Refuelling by belt or inserter from a coal Excavator is a legitimate and taught route; nothing refuels itself.

**Vehicles.** The Truck (unlocked by restoring the first away-from-Home station) carries 200 stacks, follows road access, and with the Foreman delivers materials for queued blueprint construction from a chosen local Supply chest and performs explicit equipment-recovery jobs — **cargo never teleports home**. The fixed four-stop Tram carries 200 items (250 with the Rail crew) between powered stops; the player powers the existing stops rather than building track.

---

## 7. Power and lighting

**Status: Implemented and retained** (D-GP-TURRET-POWER, D-GP-POWER-FIX and its addendum). Values in [CONTENT_CATALOGUE.md §12](CONTENT_CATALOGUE.md#12-power-constants).

**Owner decision (11): powered machines need an actual connected network. Verified in code.**

- **Supply** comes from Generators (300 kW each, fuelled), commissioned plant cores (600 kW each) and the restored Turbine hall (+600 kW). Plant cores and the Turbine have a finite *capacity*, not a finite fuel stock; only Generators consume fuel.
- **Distribution** is by Pole (`POLE_REACH = 8`), Big pole (`BIG_POLE_REACH = 12`) and Substation (reach 8, footprint 3×3). `nodeReach` (`campaignPower.ts:20`) gives Substations the ordinary pole reach; the Home block substation is what lights Founders Court.
- **The link rule** (D-GP-POWER-FIX) is **node centre to linked footprint**, not centre to centre, so a Generator links across the same gap as a machine. `powerLinksAt` (`campaignPower.ts:73`) supplies the placement preview that shows every link a pole would make.
- **Demand** is `MACHINE_KW` per kind, with `machineKw(st, m)` returning **0 for a turret outside the campaign** — i.e. turret power is a campaign-only rule (`flow.ts`).
- **Brownout is proportional** (`BROWNOUT_RULE = 'proportional'`, D-B3-4): when a circuit's demand exceeds supply every machine on it runs at the same fraction (`campaignThrottle`, `machineThrottle`). There is no priority ordering and no blackout cascade.
- **Conveyors draw no power** (belts, fast belts, undergrounds, splitters, walls, barricades, chests, poles all `0 kW`). Inserters draw 10 kW.
- **Outage semantics** (D-GP-POWER-FIX addendum): `BaseCore.poweredAt` is stamped on first supply (`campaignDefence.ts` `tickBasePower`), and an outage alert can only fire *after* supply has actually existed. A new campaign must never show "power outage" before the player has built a generator.
- **Turrets draw `TURRET_KW = 20` and hold fire without power.** The rationale recorded at the checkpoint: Home core 100 + one Assembler 100 + three turrets 60 = 260 kW of a single 300 kW Generator, so the opening is affordable on one generator but not free. **Provisional** (U-P-06).

**Lighting** is a real mechanic, not decoration: Lamp (5 kW, radius 4), Arc lamp (12 kW, radius 6), Floodlight (40 kW, 12 tiles in a 60° cone), plus authored streetlights on block substations. Lit tiles matter to combat (§8.3) and to the Shade archetype (§9.1).

---

## 8. Combat and defence

### 8.1 Player weapons

**Status: Rifle and two-barrel shotgun Implemented and retained; Arc and Plasma runtime Implemented but with no acquisition route (Approved but not implemented as content). Range bands provisional (U-P-03).**

**Owner decision (14): weapons have distinct effective ranges; player and turret shots are visible; turret cannons rotate. Verified.**

`packages/sim/src/weaponProfiles.ts` is the single source of player weapon tuning ("turret balance lives in recipes.ts. Distances are world tiles"). Each weapon has an **effective** range at full damage and a **maximum** range with linear falloff between them:

```
weaponDamage(kind, distance) =
  0                                            if distance > max
  damage                                       if distance <= effective
  damage * (max - distance) / (max - effective) otherwise
```

Rifle 18/30, two-barrel shotgun 6/12 (five pellets, 0.36 rad spread), Arc projector 9/9 (no falloff band), Plasma lance 14/14 with a travelling bolt at 12 tiles/s. The Gun turret stays at 9. Full table in [CONTENT_CATALOGUE.md §5](CONTENT_CATALOGUE.md#5-weapons).

Rifle, shotgun and Arc are **hitscan**; conventional rays keep travelling past effective range to their maximum, stopping at solid cover or first impact. Plasma uses swept collision, preserves its travel across save and weapon switch, and cannot pass through walls or the locked freight gate (GP_CHECKPOINT "Distinct weapon ranges").

**Ammunition contract.** All four provisional weapon variants use the existing bullet contract: **one carried bullet per trigger pull**. Rifle capacity 10 loaded, reload 1.5 s from carried bullets only; a reload refuses when full or with nothing carried; swapping cancels a reload without consuming unpaid ammunition (`equipment.ts`).

**Energy cells — one status (U-D-25, as amended by Owner decisions 2026-09-11 (Q06)).** Reusable, rechargeable energy cells are **Approved but not implemented and in Unity scope**; their numbers are **provisional and chosen by the implementer**, not blocked on an owner decision. The three parts must not be collapsed into one another:

| Part | Status | Source |
|---|---|---|
| The *architecture* — one reusable cell family with saved integer charge, built from components and recharged at a workbench | **Approved; in Unity scope, not yet implemented** | `docs/DECISIONS.md` D-GP-START approves "D-GP-08 later reusable-cell architecture **without early implementation**"; U-D-22 lists "Arc and cells" among GP-15–25; the GP-15–25 checkpoint hold no longer applies to the port (**Owner decisions 2026-09-11 (Q06)**); `docs/EXPLORATION_DEFENCE_PLAN.md` §6 D-GP-08 and GP-17 |
| The *numbers* — recipes, charge units, recharge demand and time, whether charging needs a recurring ordinary ingredient | **Provisional, implementer-chosen** (was Unresolved) | `docs/Design/RELIGHT_PROGRESSION_AND_WEAPONS_DRAFT.md` §8: "Exact recipes, charge units, recharge demand/time … remain open" — **Owner decisions 2026-09-11 (Q06)** now let the implementer pick documented provisional values rather than wait; owner approval of a final value remains a later step |
| Cells **replacing** the ordinary ballistic economy, and any per-shot Artifact cost | **Retired** | design draft §8 "Artifacts are not a per-shot cost"; GP_CHECKPOINT "Distinct weapon ranges": the provisional variants use the existing bullet contract and "**this does not approve the later energy economy**"; U-D-08 |

Missing approval of a *number* is not retirement of a *feature*. Until the cell lifecycle is built, every weapon in the port uses the bullet contract above; nothing in the Unity data model should make a later cell item impossible (E-11). What still needs an owner decision is only a genuine **design** change — cells replacing the ballistic economy is the one already refused (Retired, above).

### 8.2 Turrets

**Status: Implemented and retained**, with the cadence held as a **provisional starting point; retest pending** (U-P-05, §12.2.3). Owner decision (14) again.

| Property | Campaign value | Kind | Source |
|---|---|---|---|
| Range | 9 tiles | current | `TURRET.range` |
| Rate | **1 shot/s** | **provisional starting point; retest pending** (U-P-05) | `CAMPAIGN_TURRET_RATE = 1` (`turretTracking.ts:4`) |
| Damage | 10 per round | current | `turretTracking.ts` / GP_CHECKPOINT |
| Hopper | 50 rounds (62 with Gunsmith) | current | `TURRET.hopper`, `hopperCapacity` ×1.25 |
| Power | 20 kW, holds fire unpowered | provisional | `TURRET_KW` |
| Health | 100 HP | current | `DEFENCE.turretHp` |

`turretFireRate(st) = st.campaign ? CAMPAIGN_TURRET_RATE : TURRET_ROUNDS_PER_S` — **two live rate tables** exist, gated on whether the save is a campaign. The legacy table (`constants.ts TURRET = { roundsPerS: 5, roundDmg: 4 }`) is the old §13 evidence tuning and must not be carried into Unity as the campaign value.

The campaign cadence went **5 → 3.5** (GP-TURRET, owner direction) **→ 1** (GP-PLAYTEST-FIX 2, in answer to the owner's report against 3.5). 1 shot/s is therefore the *answer already given*, not an outstanding complaint: **no source records an owner verdict on 1 shot/s**, and D-GP-PLAYTEST-2 says only "Rates remain provisional pending owner play". It is a **provisional starting point; retest pending** (U-P-05) — and the specific thing still unverified is an *ordinary* raid at 1 shot/s, which GP_CHECKPOINT lists under "Not verified". Full chronology in §12.2.3.

At 10 damage per round, 1 shot/s is 10 damage/s per turret against 20 HP skitters — two rounds each, unchanged by the cadence. The cadence changed DPS and endurance, not bullets per kill; see §6.3, "What the cadence change did and did not change".

Turret cannons rotate toward their target at 360°/s with an 80 ms muzzle flash, and every resolved shot (player and turret) leaves a brief tracer (`packages/game/src/worldScene.ts:1458`, GP-PLAYTEST-FIX 6). The mechanical half — that a turret acquires, tracks and fires at one target within 9 tiles — is sim; the visible tracer, rotation and flash are presentation and belong to WORLD_AND_ASSETS.md / UI_AND_ONBOARDING.md.

The **Cannon** (Arsenal unlock) is the second defensive weapon: 12-tile range, 50 damage, 2 s reload, 20 Shells, no electrical demand, 140 HP.

### 8.3 Defence structures, repair and light

**Status: Implemented and retained** (D-GP-HOME-REPAIR).

`DEFENCE` (`campaignDefence.ts`) holds every defensive HP and repair price in one place: Barricade 240, Wall 120, Turret 100, base core 300; ordinary repair 40 HP for 2 Steel + 1 Copper in 4 s; disabled-core recovery 10 Steel + 5 Copper in 12 s. `repairCost(st, x, y)` is the **single price source** so the HUD, the goal system and the command path cannot disagree. The Workshop field-repair discovery multiplies repair speed permanently.

The Home core *is* the Home workshop building: aiming at it and pressing the interact key shows core HP and the sim-priced repair (D-GP-HOME-REPAIR). While Home is disabled, hand mining still works, so a player who loses their base is never soft-locked.

**Light is defensive.** Lit tiles suppress the Shade archetype entirely (`enemies.ts`: untargetable on unlit tiles, disabled for `SHADE_DISABLE_SECONDS = 30` in light), and first-region attackers hesitate `GP_COMBAT.hesitate = 0.65` s before crossing a lit tile (`gameplayCombat.ts:96`). Design draft §11 records light avoidance as approved direction for the wider roster.

---

## 9. Enemies and raids

### 9.1 The rosters — three in the reference, one approved for the port

**Resolved by Owner decisions 2026-09-11 (Q13, Q08).** This was the largest naming problem in the project; it is now decided, and §9.1.1 states the outcome. The reference still contains **three overlapping enemy rosters**, described here because the port reads all three as provenance:

**(a) The live first-region roster** — `packages/sim/src/gameplayCombat.ts:13` `GP_COMBAT`. **Implemented and retained.** Three kinds only: `skitter` (20 HP), `spitter` (50 HP, 7-tile projectile at 8 tiles/s), `guardian` (600 HP, 25 damage, 1.2 s windup). Every campaign birth in a save with gameplay progress is converted to a Skitter or Spitter (`campaignThreat.ts:198`), roughly one Spitter in three.

**(b) The legacy roster** — `packages/sim/src/enemies.ts`. **Retired** with the legacy ruleset. Crawler (12 HP), Shade (40 HP, untargetable on unlit tiles), **Hulk** (600 HP, 3×3, needs Shells). *Note:* the migration brief refers to "legacy Crawler/Shade/Conductor". The legacy table's third entry is **Hulk**, not Conductor. "Conductor" exists only as a *role* (`CORRECTIONS.conductorHp = 80`) applied to campaign crawlers once the Arsenal is complete, and only in saves **without** gameplay progress (`campaignThreat.ts:192`). Roles are `breaker` (1 in 5, `CORRECTIONS.breakerHp = 100`, ×3 structure damage), `conductor` (1 in 10, 80 HP, calls `reinforcementLimit = 8` reinforcements one per `reinforcementSeconds = 8`) and `shade` (1 in 3, 40 HP) (`packages/sim/src/progression.ts:17`).

**(c) The approved five-type roster** — design draft §11, D-GP-09. **Approved; in Unity scope, not yet implemented.** Skitter, Spitter, Stalker, Breaker, Howler, with a Stalker prototype existing separately in `packages/sim/src/stalker.ts` as an RI-04 territorial guard (guard → investigate → pursue → attack → return), currently bound to legacy "wells while Dark" rather than to campaign camps.

#### 9.1.1 The approved roster for the port

**Status: Approved — the five regular types are Unity scope; their combat and population values are provisional.** Source: **Owner decisions 2026-09-11 (Q13, Q14, Q15, Q08, Q06)**; values from design draft §11 and the live code where it agrees.

1. **The regular roster is exactly five types: Skitter, Spitter, Stalker, Breaker, Howler.** No sixth type is added, and nothing else is promoted into the roster.
2. **Guardians are handled separately and are not one of the five.** The freight guardian and its Quarry and Wharf successors are stronghold encounters with their own HP and roles (§5.2), not a regular spawn type.
3. **Intended roles are the approved ones** (design draft §11): Skitter the common swarm type; Spitter ranged support that can also target reachable exposed lights; Stalker a flanker with a signalled lunge and a pause after a miss; Breaker a slow structure-breaker that invites concentrated fire and can push through light; Howler a priority target that **alerts existing nearby enemies within a bounded radius and cooldown** and never summons new ones or recursively escalates.
4. **Exact combat and population values are provisional** and may be chosen and documented by the implementer (Q06/Q03). Skitter (20 HP) and Spitter (50 HP) already match between code and draft; Stalker 60 HP, Howler 80 HP and the draft's damage/interval/movement figures are the provisional starting values; the Howler's damage and interval are the draft's own "undecided" entries and are the implementer's to choose and record.
5. **Breaker baseline HP is 220, provisional** — see the correction note below.
6. **Obsolete legacy roster behaviour is excluded** (Q08): Crawler, Shade and Hulk as *creatures* do not come across, and the `legacy-v1` ruleset goes with them (§2.2). Two **mechanics** implemented under legacy names are explicitly retained because approved content needs them, and they are named here as required by Q13:
   - **Structure-breaking damage multiplier.** The `breaker` role's ×3 multiplier on `CAMPAIGN_THREAT.structureDps = 8` (§9.2) is the working implementation of the approved Breaker's "25 damage to structures"; the port keeps the mechanic and re-tunes the number to the approved Breaker.
   - **Light suppression.** `SHADE_DISABLE_SECONDS = 30` and the `hesitate = 0.65` s pause at a lit tile (`gameplayCombat.ts:96`) are the only implemented parts of the approved roster-wide light-avoidance direction (§8.3, design draft §11). The mechanic is retained; the Shade *creature* is not.
   - The **Conductor** role's summoning of up to 8 reinforcements is **not** retained: the approved Howler alerts existing enemies only, and reinforcement belongs to the raid director (§9.3), not to an individual actor.
7. **Migration path.** The port ships the five-type roster as data-driven enemy definitions. The live Skitter/Spitter behaviour (§9.2) is the ported baseline; Stalker, Breaker and Howler are added as approved content with provisional values. Values for all three reference rosters are in [CONTENT_CATALOGUE.md §7](CONTENT_CATALOGUE.md#7-enemies).

**Correction (2026-09-11) — Breaker HP 100 → 220.** Earlier revisions of this package recorded Breaker HP as a live conflict: `CORRECTIONS.breakerHp = 100` in code (`packages/sim/src/progression.ts:17`, verified) against 220 in design draft §11. **Owner decisions 2026-09-11 (Q14) settle it at 220 as the provisional baseline**, replacing the code's 100 wherever this package quotes a Breaker HP. The code value survives only as provenance for the retired no-gameplay-progress role path. As the draft itself warns, "the HP targets must be retuned if the reference damage changes" — 220 HP is 22 turret rounds at 10 damage, so Breaker counts per raid are a tuning matter under Q03, not a reason to revisit the HP.

### 9.2 Enemy behaviour that is implemented

**Status: Implemented and retained.** `gameplayCombat.ts`.

Attacks **commit to an aim point**, so cover, retreat and dodge stay useful: an actor enters `windup` for `windup` seconds with its aim fixed, then `charge`, then `recover`. A Skitter closes to 1.3 tiles and strikes for 5 every 1 s after a 0.3 s windup; a Spitter stops at 7 tiles and launches a 2-second-lived projectile for 10 every 2 s after 0.6 s; the freight guardian charges for 25 at 9 tiles every 4 s after a 1.2 s windup. Actors notice the player at `notice = 8` tiles, lose them at `escape = 18`, and alert squadmates within `alertRadius = 6` when they have line of sight. Spitter projectiles damage base cores on impact.

Damage to structures is `CAMPAIGN_THREAT.structureDps = 8`, tripled for an actor carrying the `breaker` role, and to the player on contact `contactDps = 5`. That ×3 multiplier is the retained structure-breaking mechanic named in §9.1.1: in the port it belongs to the approved **Breaker** (provisional 25 damage to structures, 15 to the player, 2 s interval, 220 HP), not to the retired legacy role.

### 9.3 The raid director

**Status: Implemented and retained** (D-GP-START's corrected D-GP-11 cadence). Source: `packages/sim/src/campaignThreat.ts`. Numbers in [CONTENT_CATALOGUE.md §8](CONTENT_CATALOGUE.md#8-raid-and-threat-director).

There is exactly **one global director**. It issues two things:

- **Major assaults** — a finite roster (`ACTIVE_RAIDS.total = 60`) against **one announced target base**, with a `warning = 300` s announcement, a `window = 150` s reinforcement window, an `activeRaidBudget = 48` cap on simultaneously living raiders and a `livingBudget = 240` cap on world actors. First major at `firstMin = 1500` s + up to `firstRange = 300` s of active time; subsequent majors `intervalMin = 1080` + `intervalRange = 240` s start-to-start. After an assault, `recovery = 300` s of quiet.
- **Minor harassment** — `minorMin = 300` + `minorRange = 120` s apart, `6 + raidChoice(seed, raidsStarted, 4)` attackers, i.e. **6–10**, never during a major, and pushed back by `minorAfterMajor = 300` s after one.

**Owner decision (15): larger attacks use multiple valid exterior approaches to one announced target base. Verified.** `campaignApproaches` scores reachable exterior tiles and keeps the best tile per compass sector (up to four: N/E/S/W, `campaignThreat.ts:143-154`); a major stores `origins` and spawns round-robin across them (`:72` `origins[majorSpawned % origins.length]`). Harassment and the introductory encounter use a single locked approach. Spawns are never placed inside walls, and `campaignStaging` finds a legal staging tile at least 28 tiles from the engineer with the same compass heading, so nothing is born on top of the player.

Design guarantees that the port must preserve:

- **Deterministic scheduling.** `raidChoice(seed, serial, range)` derives every roll from the save seed and a serial, so a reloaded save produces the same schedule.
- **No accumulated debt.** If an assault cannot start (cleanup, recovery, no reachable approach, a live minor raid), it is *cancelled and re-warned* as a fresh future opportunity — it is never queued up to fire late or twice. `future(...)` records the reason for the player.
- **The first target is always Home.** Later targets are nominated among Home and commissioned plants.
- **Warnings are earned.** A powered Radio gives assault warnings; the precision upgrade (15 Steel + 10 Copper) adds approach direction and composition (`campaignThreat.ts:269`). An unpowered radio shows "last received warning (radio offline)". This is owner decision (13)'s "readable network demand/supply and day/time" in its threat half; the HUD half is UI_AND_ONBOARDING.md's.
- **Day/time.** `CAMPAIGN_RULES = { daySeconds: 1200, daylightSeconds: 900 }` with `campaignClock` returning day, night flag and elapsed (`rules.ts:11-21`). `firstAssaultNight: 3` is **stale**: the director schedules the first major on elapsed active time, not on night 3. See §16 of the catalogue.

#### 9.3.1 What the owner's decisions fix about the director

**Source: Owner decisions 2026-09-11 (Q13, Q14, Q15, Q06, Q03).** The structure below is **approved and must be preserved**; the numbers that fill it are **provisional** and tunable under Q03.

Preserved structure — a Unity re-implementation may not quietly drop any of these:

1. **One global director**, never a per-site timer. Four commissioned sites must not multiply interruption frequency by four (design draft §13).
2. **One announced target base per major assault**, with a warning that gives the player time to prepare or travel.
3. **Multiple valid exterior approaches for larger attacks**, one approach for harassment and for the introductory encounter.
4. **First major at roughly 25–30 active minutes** (`firstMin = 1500` s + `firstRange = 300` s) and **later majors roughly 18–22 minutes start-to-start** (`intervalMin = 1080` + `intervalRange = 240` s).
5. **Finite reinforcements per assault** inside a bounded window, with cleanup distinct from the window: survivors stay real after the last group has arrived and are not despawned by a timer.
6. **Readable warnings**, carried by the powered Radio and its precision upgrade.
7. **Recovery without accumulated attack debt**: a cancelled assault is re-warned as a fresh opportunity, suppressed harassment is not released as a burst, and a disabled base gets a workable recovery opportunity rather than a failure spiral.

**Later raid tiers are approved Unity scope** (Q15, Q06), with provisional attacker counts taken from design draft §13 and recorded in [CONTENT_CATALOGUE.md §8.5](CONTENT_CATALOGUE.md#85-later-raid-tiers--design-draft-13-approved-but-not-implemented): established harassment 10–18 and late harassment 16–26 every 4–6 minutes; subsequent majors 90–140 and late majors 160–220 attackers over 3–5 minute windows. The live director implements only the first tier (6–10 harassment, a 60-actor major roster). The draft's own open item — the exact mapping of early/established/late tiers to restoration progress — is a **provisional implementer choice** under Q06, not a further owner decision: tie the tier to commissioned plants and strongholds cleared, record the mapping, and tune it in playtesting. The `activeRaidBudget = 48` and `livingBudget = 240` caps stay the readability and performance ceiling; a later tier raises the roster, not the simultaneous-actor cap, until measurement says otherwise.

**The introductory encounter is not part of this progression** (Q13). It keeps its own scheduling protections (§4.3) and is judged against **actual opening readiness** — the player has a turret placed, fed and understood — rather than against a fixed clock. Its timing must not be treated as the first step of the major-assault ladder, and changing it does not shift `firstMin`.

---

## 10. Survivors and facilities

**Status: Implemented and retained.**

Relight has no survivor population, no morale and no housing. "Survivors" in this game means the seven **specialist recruits** of §5.4: individuals found at shelters who hand over permanent plans and then stop being a gameplay entity. They do not need feeding, do not occupy buildings, do not work machines and cannot be lost.

Facilities are **restored world installations**, not placeables: Home / Founders Court workshop (crafting, storage, the base core), four fixed tram stops, the regional Repair workshop (40 kW, repairs nearby damaged equipment from local supplies), the Radio, the Turbine hall, and the three prepared regional plants. `SURVIVOR_UNLOCKS` in `flow.ts` is the mechanism (currently `{ Electricians: ['floodlight', 'bigpole', 'substation'] }`) by which a recruit adds build-menu entries.

**Out of scope:** cosmetic work crews and any garrisoned population belong to the owner's separate machine-survival game (§13).

---

## 11. Ending and campaign completion — Unresolved

**Status: Unresolved. Do not design this.**

The final objective is open. `docs/EXPLORATION_DEFENCE_PLAN.md` §6.3 **D-GP-13** and question **Q07** record that the campaign's ending has not been decided, and D-GP-START explicitly leaves it open while authorising GP-01–14. The only thing that *is* decided is a negative: **commissioning the third plant does not by itself set victory** (U-D-23).

What exists in code today: `CORRECTIONS.plants = 3` and three strongholds, so the content ladder reaches a natural third-plant plateau with nothing after it. There is no victory state, no end screen, no final encounter and no post-M4 content.

What must not happen: an implementer inventing a completion condition, a score, a timed survival goal or an evacuation ending. This is an owner decision (U-Q-02), and a Unity phase that reaches M4 should stop at a decision gate.

**Owner decisions 2026-09-11 (Q02) — still unresolved, and deliberately so.** The owner reviewed this question on 2026-09-11 and did **not** settle it: the ending remains genuinely open, and nothing in this package may invent one. Two things are now explicit. First, **commissioning the third plant must not automatically trigger victory** — the negative of U-D-23 is reaffirmed, so an implementer must not treat "plants === 3" as a completion condition, an end screen or a credits trigger. Second, the scope of the block is narrow: Q02 blocks **final campaign-completion work only**. Everything else that leads up to it — the third plant itself, city-scale production, the M4 milestone, late raid tiers (§9.3.1), the remaining weapons and strongholds — is approved Unity scope under Q06 and is not waiting on the ending. A Unity phase that reaches M4 stops at a decision gate and hands the question back to the owner.

---

## 12. Balance intent and known feel problems

### 12.1 Intent

- **The opening should be short and pointed, not a waiting exercise.** ~30 s of hand mining to afford the first Generator, then a working extraction line, then a defended turret, then the introductory attack — with automation demonstrably better than hands at every step (owner decision 10).
- **Defence should cost production, not a separate currency.** Bullets, turrets and repairs all come out of Steel and Copper.
- **Every advantage should be visible and legible.** Actual bullets consumed, actual kW demanded and supplied, actual approach announced.
- **Nothing should be farmable into a progression skip.** Repeating camps yield shared Artifacts only; keys and cores are one-time claims.

### 12.2 The three contested values — playtest chronology

Three values are repeatedly described in this package as "still rejected by the owner" **and** as adjustments already made in answer to owner feedback. Both cannot be true without a later source, and no later source exists. The reference tree was searched for owner rejection language: the phrase "too fast" does **not** appear anywhere under `/mnt/e/Factorio2/docs/` — it exists only inside `Unity/Docs/*`, i.e. inside this migration package. Every chronology below therefore ends at "retest pending".

#### 12.2.1 Hand mining — U-P-01

| Stage | Value / build | What is on record | Source |
|---|---|---|---|
| Problem as first reported | `HAND_MINE_PER_S = 1.0` items/s | Owner player-test, 2026-09-11: manual mining is too fast; slower is intended. **UNVERIFIED:** no verbatim owner quote survives in the reference tree — `docs/DECISIONS.md` D-GP-PLAYTEST-2 records the *direction* ("hand mining 0.5 units/s … as provisional opening pacing"), not the wording | `docs/DECISIONS.md` D-GP-PLAYTEST-2; `docs/Implementation/GP_CHECKPOINT.md` "Player-test corrections — 2026-09-11" |
| Adjustment implemented | **1.0 → 0.5 items/s**, GP-PLAYTEST-FIX 2, 2026-09-11 | `flow.ts:210`: "halved from 1 so the opening rewards automation". Quantified: "Hand mining 35 units at 0.5/s ≈ 70 s (was ≈ 35 s); five stationary batches = 100 s (was 60 s); about 3 minutes of hands before walking and placement (was about 1.5)" | `packages/sim/src/flow.ts:210`; GP_CHECKPOINT second pass → "Hand pacing" |
| Retest of the new value | **None.** D-GP-PLAYTEST-2: "Rates remain provisional pending owner play." `CLAUDE.md` current task pointer: "Reload 5178 for owner retest" | `docs/DECISIONS.md` D-GP-PLAYTEST-2; `CLAUDE.md` |
| Later owner feedback that does exist | **GP-START-POCKETS points the other way.** After 0.5 was live the owner asked: *"Can the player start with 20 steel and 5 copper just to make the first mining experience not tedious"*. The answer was a starting stake, not a rate change | GP_CHECKPOINT "Starting Backpack stake"; `docs/DECISIONS.md` D-GP-START-POCKETS; `packages/sim/src/rules.ts:8-10` |
| **Correct label** | **0.5 items/s is a provisional starting point; retest pending.** No source records the owner rejecting 0.5 | |

#### 12.2.2 Hand ammunition — U-P-02

| Stage | Value / build | What is on record | Source |
|---|---|---|---|
| Problem as first reported | Hand batch at `SHOT.seconds = 6` s — the same speed as an Assembler | Owner player-test, 2026-09-11: early ammunition preparation is too fast. **UNVERIFIED:** no verbatim quote in the reference tree | GP_CHECKPOINT "Player-test corrections — 2026-09-11" |
| First adjustment | **6 s → 12 s** per batch of 10 bullets (hand only; Assembler unchanged at 10/6 s) | D-GP-PLAYTEST: "Provisional hand ammunition is 10 bullets/12 s versus Assembler 10/6 s" | `docs/DECISIONS.md` D-GP-PLAYTEST |
| Second adjustment | **12 s → 20 s**, GP-PLAYTEST-FIX 2, 2026-09-11 | `flow.ts:158`: "ten bullets per 20 s by hand, up from 12 s". D-GP-PLAYTEST-2 adopts "hand bullets 10 per 20 s as provisional opening pacing" | `packages/sim/src/flow.ts:158`; `docs/DECISIONS.md` D-GP-PLAYTEST-2 |
| Retest of the new value | **None.** Same "Rates remain provisional pending owner play" / "Reload 5178 for owner retest" | `docs/DECISIONS.md` D-GP-PLAYTEST-2; `CLAUDE.md` |
| **Correct label** | **10 bullets / 20 s is a provisional starting point; retest pending.** The hand rate has already been slowed 3.3× from its original value | |

#### 12.2.3 Basic Gun turret cadence — U-P-05

| Stage | Value / build | What is on record | Source |
|---|---|---|---|
| Starting point | `TURRET.roundsPerS = 5` — the §13 evidence tuning, still live for **non-campaign** saves only | `constants.ts:42` | `packages/sim/src/constants.ts:41-42` |
| First adjustment | **5 → 3.5 shots/s** (210/min), GP-TURRET, 2026-09-11, on owner direction | "The active campaign Gun turret now fires at 3.5 shots/s (210/min), down 30% from 5. Each shot consumes one bullet; damage, 9-tile range and 50-round base hopper remain unchanged" | GP_CHECKPOINT "Turret improvements — 2026-09-11" |
| Problem as reported against 3.5 | 3.5 shots/s | Owner player-test: basic turret firing is too fast. **UNVERIFIED:** no verbatim quote in the reference tree; D-GP-PLAYTEST-2 records only the resulting direction | GP_CHECKPOINT second pass; `docs/DECISIONS.md` D-GP-PLAYTEST-2 |
| Second adjustment | **3.5 → 1 shot/s**, GP-PLAYTEST-FIX 2, 2026-09-11 | `turretTracking.ts:4`: "one bullet per second, down from 3.5, so each shot reads". GP_CHECKPOINT: "210 → 60 rounds/min … a 50-round hopper now lasts 50 s of continuous fire instead of about 14 s. **No damage, enemy HP or wave compensation was added.**" D-GP-PLAYTEST-2 adopts "the basic Gun turret at 1 shot/s with no damage, enemy-HP or wave compensation" | `packages/sim/src/turretTracking.ts:4`; GP_CHECKPOINT second pass → "Turret cadence"; `docs/DECISIONS.md` D-GP-PLAYTEST-2 |
| Retest of the new value | **Partial, and only inside the introductory encounter.** Headless and browser fixtures at 1 shot/s: five skitters, ten shots for ten kills, 40 rounds left, repelled at t = 46 s. The checkpoint's own Checks line states: *"Not verified: the first ordinary Home raid at 1 shot/s beyond the intro encounter"* | GP_CHECKPOINT second pass → "Turret cadence" and "Checks" |
| **Correct label** | **1 shot/s is a provisional starting point; retest pending** for ordinary raids. No source records the owner rejecting 1 shot/s | |

#### 12.2.4 Automation advantage — the one genuinely open feel item

| Item | Status |
|---|---|
| Automation's advantage over hands (owner decision 10) | **Implemented but needs correction** — but the correction owed is a *judgement about the whole opening*, not a rejected number. The arithmetic advantage exists (§6.4); the two pressures on it (slow enough to reward automation vs. not tedious) are both on record and pull in opposite directions (GP-PLAYTEST-FIX 2 vs. GP-START-POCKETS). This is the item C-ACC must produce a human verdict on |

#### 12.2.5 Summary and the rule for the port

| # | Value | Current | Was | Status |
|---|---|---|---|---|
| 1 | Hand mining | `HAND_MINE_PER_S = 0.5` | 1.0 | **Provisional starting point; retest pending** (U-P-01) |
| 2 | Hand ammunition | `HAND_BULLET_SECONDS = 20` (Assembler unchanged at 10 per 6 s) | 6 s → 12 s → 20 s | **Provisional starting point; retest pending** (U-P-02) |
| 3 | Basic turret cadence | `CAMPAIGN_TURRET_RATE = 1` | 5 → 3.5 → 1 | **Provisional starting point; retest pending** for ordinary raids (U-P-05) |
| 4 | Automation's advantage over hands | see §6.4 | — | **Implemented but needs correction** — the open human verdict |

**Rule for the port (DECISIONS.md U-M-12, adopted 2026-09-11; also stated in `../README.md` "Working agreement"): current values are provisional starting points, and focused, documented tuning is permitted during the playable-Home milestone (Phase C).** Each change records what moved, why, and the retest result. Nothing here licenses a redesign or an untracked re-balance: the values carry across as the starting point, and a change without a recorded reason and retest is not a tuning pass. The earlier instruction — "carry these values unchanged until Phase F" — is superseded, because it rested on the belief that the owner had rejected the current values, which no source supports.

**Broadened by Owner decisions 2026-09-11 (Q03).** The tuning permission is no longer limited to Phase C. The owner confirms that the latest implemented mining, ammunition and turret adjustments are **provisional starting values**, and that the implementation AI may make **focused, documented tuning changes to them throughout Unity playtesting** without per-number owner approval. The recording discipline is unchanged and is the whole of the constraint: each change names the value, the reason and the retest result; a change without them is not a tuning pass. What remains an owner matter is a **design** change — removing a mechanic, adding a compensating chore, altering what automation is for — not a number.

Q03 also restates two things the retest chronology above already implies. **Distinguish original feedback from retest.** The owner's original complaints (mining too slow, hand ammunition too slow, the turret too strong) and the later retest verdicts are separate evidence, and §12.2.1–12.2.4 keep them separate; do not collapse them into a single "the owner asked for X". **Preserve useful automation and avoid excessive manual waiting.** These are the two standing acceptance criteria a tuning change is judged against: a value that makes hand work tolerable by making automation pointless fails the first, and a value that restores automation's edge by making the player wait fails the second. §6.4 remains the open human verdict on the balance between them.

### 12.3 Balance values that are provisional across the board

Everything introduced by `docs/Implementation/GP_CHECKPOINT.md` is provisional unless a `docs/DECISIONS.md` entry confirms it — that includes `TURRET_KW = 20`, the weapon range bands, the opening encounter's 25 s / 5 attackers, the major composition of 40 Skitters + 20 Spitters, and the camp populations of 20/22/20. "Provisional" here means *not yet confirmed by owner play*; it does not mean *rejected* (§12.2). The checkpoint says so itself: "Numeric defaults above are not measured final balance."

### 12.4 Verification state of the reference

318 of 464 sim tests pass on the working tree; the 146 failures were sampled and predate the current work (they are legacy-fixture failures). No full campaign soak, no exhaustive automated suite and **no human balance verdict** has been recorded. Nothing in the reference tracker should be read as proof that the balance works (U-D-24).

---

## 13. Out of scope and retired mechanics

### 13.1 Explicitly out of scope — the owner's other game

Relight must not import anything from the owner's **separate machine-survival game**. Specifically excluded:

- **Cosmetic work crews** — Relight's recruits hand over plans and leave; there is no crew that appears and works.
- **A machine fortress** — Relight defends real base cores at Home and at commissioned plants, with player-placed turrets, walls and barricades. There is no fortress structure.
- **A five-day raid schedule** — Relight's director schedules on elapsed active time (first major 25–30 min, then 18–22 min start-to-start), not on days. The stale "First major assault: Night 3" line in `docs/CAMPAIGN_RULES.md` is a *legacy* artifact of Relight's own earlier design, **not** an import from the other game, but it is equally wrong and is flagged in the catalogue.

**No references to the other game's mechanics were found in the Relight sources read for this pass.** The risk is an implementer importing them from conversation, not from the repository.

### 13.2 Retired mechanics — must not be rebuilt

| Mechanic | Why retired | Source |
|---|---|---|
| Territorial frontage, ring, adjacent-block claim economy, hour clock, HQ/front/kit/hopper street model | Superseded by defended factory/plant sites | Owner direction 2026-09-09; `rulesetProblem` rejects the state |
| Artifact analysis (artifacts consumed or analysed into upgrades) | Superseded by Artifacts-as-ingredient + Overclock modules | `CLAUDE.md` handoff 2026-09-10 |
| Fractional pocket magazines (1 item = 10 rounds) | Superseded by one item = one bullet | D-GP-PLAYTEST; `ammoVersion 1` |
| Per-shot artifact cost, Night-3 assault obligation | Superseded by D-GP-08 and the corrected D-GP-11 cadence | D-GP-START |
| The `legacy-v1` ruleset itself, as shipped content | **Retired from the Unity port** and preserved in the reference project only: one ruleset (`exploration-v2`), no second rules profile, no legacy UI, no legacy test framework (§2.2) | Owner decisions 2026-09-11 (Q08); `rules.ts` `LEGACY_RULESET` |
| Legacy enemy roster (Crawler / Shade / Hulk) as campaign content | Belongs to `legacy-v1`; **confirmed retired for the port**. The creatures do not come across; two mechanics implemented under legacy names do, and are named in §9.1.1 | `enemies.ts`; Owner decisions 2026-09-11 (Q08, Q13); U-Q-08 |
| Speed controls and fast-forward in play | Fixed 1× with pause | `CLAUDE.md`; D-UI-10 |
| Legacy abstract rifle model (`RIFLE_ROUNDS_PER_S`, `RIFLE_RANGE = TURRET_RANGE`) | Superseded by `weaponProfiles.ts` | GP_CHECKPOINT "Distinct weapon ranges" |
| Energy cells **replacing** the ballistic economy, and any per-shot Artifact cost | The ordinary bullet contract stays; Artifacts are not a per-shot cost. **Only this framing is retired** — the reusable-cell *architecture* is Approved but not implemented and, under Owner decisions 2026-09-11 (Q06), is in Unity scope with implementer-chosen provisional numbers; the reference project's GP-17 hold does not apply to the port (§8.1) | design draft §8 "Artifacts are not a per-shot cost"; GP_CHECKPOINT "Distinct weapon ranges"; D-GP-START (approves D-GP-08 without early implementation) |

---

## 14. Verification of the 17 owner decisions

Each row states what the owner decided, what the code does, and the verdict. "Verified" means the code implements the decision; "Discrepancy" means it implements something that conflicts; "Not implemented" means no code exists.

| # | Owner decision | Code evidence | Verdict |
|---|---|---|---|
| 1 | Weapons are owned items with coherent equip / action-bar behaviour | `equipment.ts` (two physical slots, per-instance ammo/cooldown, `WeaponItem` identity); fresh campaign gets no weapon (`initEquipment`); GP_CHECKPOINT "Player-test corrections": default Rifle shortcut that *looked* owned removed, drag Backpack → equipment slot or action bar added; `assignQuickbar` gives one tool one slot and collapses saved duplicates | **Verified** (defects fixed at the checkpoint) |
| 2 | Storage/backpack transfers work across all visible slots including rightmost and scrolled | GP_CHECKPOINT 2026-09-11 second pass: rightmost slot 24 and scrolled slot 39 drops checked at 1600×1000 and 1280×720 @1.25; stale per-chunk guard fixed; `uiDrag` end hooks rerender | **Verified** (was a defect, now fixed; browser-checked, not sim-checked) |
| 3 | Repeated matching-stack merges for all stackables | Same pass: three consecutive merges each for Steel, Copper, Coal and **Bullets**; storage is a pooled quantity so puts merge with no per-chunk cap; takes fill chosen slot → other matching stacks → empty slots, moving only what fits | **Verified**. UNCERTAIN: in-browser Coal/Bullet Backpack→storage merges were covered by the sim test only, per the checkpoint's own "not verified" note |
| 4 | Transfers / reloads / cancellations / saves never lose or duplicate items | `ledger.ts` `conservation()`; refused transfers touch neither side; hand-craft cancel refunds reserved inputs (`flow.ts` `cancelCraft`); ammunition migration converts ×10 with sub-1e-7 residue rounded and other fractions rejected safely, overflow recoverable through Home storage | **Verified** |
| 5 | One ammunition item = one bullet; stacks hold many | `flow.ts:156` `ammoUnit`; `STACK.magazine = 200`; turret hopper 50 bullets; hand craft yields 10 | **Verified** |
| 6 | Workshop recipes show icons, names, quantities, progress | `packages/game/src/inventoryPanel.ts:43` — compact card per recipe: output icon/name/quantity, duration, ingredient icons with required/carried counts, Craft, missing-resource note, progress bar, seconds left, Cancel | **Verified**; presentation detail belongs to UI_AND_ONBOARDING.md |
| 7 | Handcrafting requires a stationary player with accessible cancel and correct accounting | `flow.ts:1465-1466` mover/weapon/mining hand blocked; `flow.ts:1007` cancels on down or leaving Home; `panel.ts:712` closing the workshop cancels; HUD prompt "…· Escape cancels"; inputs refunded | **Verified** |
| 8 | Redundant equipment presentation removed from workshop; equipment controls still accessible | GP_CHECKPOINT: the large "Equipment · two slots" block replaced by an equipment strip under the Backpack grid with the two physical slots, Reload and Swap; ownership rules unchanged | **Verified**; presentation belongs to UI_AND_ONBOARDING.md |
| 9 | Manual mining and early ammo preparation feel too fast; slowdown intended, numbers provisional | `HAND_MINE_PER_S = 0.5` (halved from 1.0, `flow.ts:210`), `HAND_BULLET_SECONDS = 20` (6 → 12 → 20 s, `flow.ts:158`). Both slowdowns are the answers *already given* to the report. D-GP-PLAYTEST-2: "Rates remain provisional pending owner play." The only later owner input on hand pacing is GP-START-POCKETS, which asked for the first mining session to be *less tedious* | **Verified — direction implemented.** Both values are **provisional starting points; retest pending** (U-P-01/U-P-02, §12.2). No source records an owner verdict on 0.5 / 20 s |
| 10 | Automation must give a useful advantage without the opening being a waiting exercise | Hand 30 bullets/min vs Assembler 100 vs Mk2 200; hand crafting locks the engineer; opening chain reaches automation by step 13; at 1 shot/s one Mk1 refills a 50-round hopper in 30 s | **Partly verified** — the advantage exists arithmetically. The decision has two halves that pull against each other (slow enough to reward automation vs. not tedious), both on record (GP-PLAYTEST-FIX 2; GP-START-POCKETS), and **no human verdict exists on the post-fix opening**. This is the feel item C-ACC must close, not a rejected number |
| 11 | Powered machines need an actual connected network; conveyors do not consume power | `campaignPower.ts` `campaignGrid` / `machineThrottle`; footprint link rule; `MACHINE_KW` 0 for belt/fastbelt/underground/splitter; `TURRET_KW = 20`; `tickBasePower` outage semantics | **Verified** |
| 12 | Fuel comes from real supplies; legitimate automatic delivery is useful | `GENERATOR_COAL_CAP = 50`, burn follows load (`coalBurnPerMin`), belts/inserters refuel, `supplyChainReaches` requires a real route and `noteTurretSupply` refuses to credit hand loading | **Verified** |
| 13 | Readable network demand/supply and day/time | `campaignGrid` exposes per-block demand/supply/throttle; `campaignClock` gives day/night/elapsed; GP_CHECKPOINT "Top status strip" and HUD "0 / 0 kW · no Generator linked" | **Verified** in sim; HUD layout is UI_AND_ONBOARDING.md's |
| 14 | Distinct weapon ranges; visible player and turret shots; rotating turret cannons; basic turret fire too fast (1 shot/s provisional) | `weaponProfiles.ts` four distinct effective/max bands with falloff; tracers `worldScene.ts:1458`; 360°/s barrel turn and 80 ms flash; `CAMPAIGN_TURRET_RATE = 1` (`turretTracking.ts:4`), reached 5 → 3.5 → 1 in answer to the owner's reports | **Verified** for ranges, visuals **and the cadence direction**. 1 shot/s is a **provisional starting point; retest pending** (U-P-05, §12.2.3) — verified inside the intro encounter, explicitly **not verified for an ordinary raid** (GP_CHECKPOINT "Checks"). **Discrepancy:** `constants.ts TURRET.roundsPerS = 5` and `roundDmg = 4` remain live for non-campaign saves, and `CURRENT_GAMEPLAY_CATALOGUE.md` still prints "5 rounds/second" in its turret row |
| 15 | Larger attacks use multiple valid exterior approaches to one announced target base | `campaignApproaches` keeps the best tile per compass sector (up to 4); majors store `origins` and spawn round-robin; harassment uses one; `campaignStaging` keeps births ≥ 28 tiles from the engineer and out of walls | **Verified** |
| 16 | Opening sequence: first fully loaded operational Home turret → one-time intro attack, ~25 s directional warning, ~4–6 basics from one approach, explain actual ammo consumption, teach auto replenishment through real delivery, guide to three turrets on different approaches, then a nearby exploration objective; coordinate with the director, prevent overlap, allow recovery, recognise early-completed steps and progressed saves, preserve recoverability, no duplicate encounters or rewards | `openingEncounter.ts` + `campaignThreat.ts:201-238` (§4.3); `goal.ts:82-167` (§4.2); `readyOpeningTurret` requires running **and** full; `warning = 25`, `count = 5`; skip/defer rules; `recovery = 300`; `initOpeningEncounter` skips on progressed saves; `supplyChainReaches` / `noteTurretSupply`; `turretRecommendation` for three distinct approaches; step 15 sends the player to the nearest freight camp | **Verified in substance**, with **one clause needing correction**: "coordinate with the director, prevent overlap" is implemented as a *permanent skip* when a raid is live or a major is within 300 s (`campaignThreat.ts:209`), where the intended behaviour is to **defer until safe** and still deliver the encounter — only a genuinely progressed save should lose it (§4.0, §4.4). Numbers **provisional** (U-P-04). UNCERTAIN: the first ordinary Home raid *after* the intro at 1 shot/s was explicitly not play-verified (GP_CHECKPOINT) |
| 17 | The ending (Q07 / D-GP-13) is unresolved — keep it unresolved | No victory state, end screen or final encounter exists; D-GP-13/Q07 open; D-GP-START leaves it open | **Unresolved, correctly** (§11). Reaffirmed by **Owner decisions 2026-09-11 (Q02)**: the ending stays open, commissioning the third plant must not automatically trigger victory, and the block covers final campaign-completion work only |

---

## 15. Open questions for the coordinator

| # | Question | Why it matters | Suggested owner of the answer |
|---|---|---|---|
| A-1 | **Enemy roster reconciliation.** Ship the live Skitter/Spitter/Guardian roster, keep the approved five-type roster (Stalker, Breaker, Howler) as data-driven extension, retire the legacy Crawler/Shade/**Hulk** roster with `legacy-v1`? Note the migration brief's "Crawler/Shade/Conductor" is inaccurate — the legacy third type is Hulk; Conductor is a campaign *role*. | D-GP-09 defers this; the port needs one roster shape up front | Owner (D-GP-09 / U-Q-06) |
| A-2 | **Breaker HP: 100 or 220?** Code uses `CORRECTIONS.breakerHp = 100`; design draft §11 approves 220. | Straight conflict between code and the approved direction | Owner |
| A-3 | **Which raid cadence is canonical?** Code `ACTIVE_RAIDS` (first 1500–1800 s, interval 1080–1320 s, 60 total) vs design draft §13 (first 25–30 min, 50–70; subsequent 18–22 min, 90–140; late 160–220) vs `docs/CAMPAIGN_RULES.md` "Night 3 / 8–12 crawlers". D-GP-START corrected D-GP-11 to the code's cadence, but the later tiers are unbuilt. | The later tiers are a large content decision (GP-22) | Owner |
| A-4 | **Are the three "feel" numbers (mining, hand ammo, turret rate) to be changed before or after the port?** *Superseded by the §12.2 chronology and `../README.md`'s working agreement:* all three are **provisional starting points with the retest still pending**, no owner rejection of the current values is on record, and focused documented tuning is permitted during the playable-Home milestone (proposed U-M-12 replacement). What remains for the owner is the **retest verdict itself**, at C-ACC. | Phase C acceptance is a human verdict on values that have never been played back | Owner (U-Q-03), at C-ACC |
| A-5 | **Do Arc and Plasma need an acquisition route in the first Unity milestone?** Their runtime exists; nothing awards them. | Otherwise two weapons are dead content in the port | Owner (GP-17/18, U-Q-06) |
| A-6 | **Frame and Board currently have no consumer** other than the Alien workbench and the Overclock recipe. The catalogue says they are "retained for future turret work". | Either the advanced turret chain is scoped or two items are vestigial | Owner |
| A-7 | **Is the `legacy-v1` ruleset needed in Unity at all?** Recommendation: no. Its constants are the single largest source of stale numbers in the documentation. | Removes a second rules profile, a second enemy roster and a second turret rate table | Owner (U-Q-08) |
| A-8 | **Stronghold guardian HP.** Code's freight guardian is 600 HP; design draft §12 proposes Freight 600 / Quarry 1,000 / Wharf 1,500. Quarry and Wharf are unbuilt. | Content sizing for GP-21/22 | Owner |
| A-9 | **`docs/CAMPAIGN_RULES.md` is a docsync-generated contract table that is now stale** in at least three rows (Night 3, 8–12 minor crawlers, "60 crawlers one per 4 s"). Should it be regenerated in the reference project before it is used as a migration source? | It will otherwise keep re-seeding wrong numbers | Coordinator |

*Coordinator disposition (2026-09-11):* A-1 → DECISIONS.md U-Q-13; A-2 → U-Q-14; A-3 → U-Q-15; A-4 → U-Q-03; A-5 → folded into U-Q-06; A-6 → U-Q-16; A-7 → U-Q-08; A-8 → U-Q-17; A-9 → not regenerated during the migration — the Unity documents cite code, and `docs/CAMPAIGN_RULES.md` is listed as stale in CONTENT_CATALOGUE.md §16 (U-M-09).
### 15.1 Owner answers, 2026-09-11

The owner answered on 2026-09-11. The table above is kept as the record of what was asked; this subsection records what came back and where the answer now lives. **Nothing in §15 is still open except A-9 (coordinator) and the parts of A-4 that are a human retest verdict.**

| # | Answer | Recorded in |
|---|---|---|
| A-1 | **Resolved (Q13, Q08).** Exactly five regular types — Skitter, Spitter, Stalker, Breaker, Howler. Guardians are separate and are not one of the five. The legacy Crawler/Shade/Hulk roster does not come across, and `legacy-v1` is retired from the port. Two legacy-named *mechanics* are retained and named: the ×3 structure-damage multiplier and light suppression. The question's own correction stands — the legacy third type is **Hulk**; Conductor is a campaign *role*, and its reinforcement summoning is **not** retained. | §9.1.1, §2.2, §13.2 |
| A-2 | **Resolved (Q14): 220**, provisional, replacing the code's 100. Dated correction note recorded. | §9.1.1 correction note; CONTENT_CATALOGUE.md §7.2, §7.4, §16 |
| A-3 | **Resolved (Q15, Q06).** The code cadence is canonical and its structure is approved: one global director, one announced target, multiple approaches for larger attacks, first major ~25–30 active minutes, later majors ~18–22 minutes start-to-start, finite reinforcements, readable warnings, recovery without accumulated debt. The **later tiers are approved Unity scope** with the draft's counts as provisional values. `docs/CAMPAIGN_RULES.md`'s "Night 3 / 8–12 crawlers" is stale and is not a third candidate. | §9.3.1; CONTENT_CATALOGUE.md §8.5 |
| A-4 | **Broadened (Q03).** The three values are confirmed provisional starting values, and focused documented tuning is permitted **throughout Unity playtesting**, not only during the playable-Home milestone. The owner's original feedback and the retest verdicts stay separate evidence. The human retest verdict itself is still owed, at C-ACC. | §12.2.5 |
| A-5 | **Resolved (Q06): yes, in scope.** Arc and Plasma acquisition is approved Unity scope with implementer-chosen provisional recipes and costs; neither weapon may remain dead content. The reference project's GP-17/GP-18 hold does not apply to the port. | §5.3, §8.1 |
| A-6 | **Resolved (Q16): the premise was wrong.** Frames and Boards already have consumers — the Alien workbench build cost and the Overclock module recipe — and keep exactly those uses. No advanced production chain is to be invented, and all "vestigial / needs a consumer" language is withdrawn. | §6.2; CONTENT_CATALOGUE.md §1, §3, §16 |
| A-7 | **Resolved (Q08): no.** `legacy-v1` is retired from the Unity port and preserved in the reference project only — one ruleset, no legacy UI, no legacy test framework, no legacy roster. The validator *idea* survives as a save-consistency check. | §2.2, §13.2 |
| A-8 | **Resolved (Q17): Freight 600, Quarry 1,000, Wharf 1,500**, provisional starting HP, distinct encounter roles preserved, tunable after implementation, and no further approval needed before the approved encounters are coded. | §5.2; CONTENT_CATALOGUE.md §9.2 |
| A-9 | **Unchanged.** Still a coordinator matter; `docs/CAMPAIGN_RULES.md` is not regenerated during the migration and stays listed as stale. | CONTENT_CATALOGUE.md §16 |

Two things the owner's answers deliberately do **not** settle, so neither may be treated as closed: the **ending** (Q02, §11) and the **automation-versus-tedium human verdict** (§6.4, §12.2.4). A third is bounded rather than open: the **art package** (Q04) — proceed on known reusable placeholder assets, keep third-party assets with unverified permissions out until the owner supplies source and licence, and do not let the final art package block foundation work or the playable milestones.
