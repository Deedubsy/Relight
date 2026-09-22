# Relight — integration audit and remaining work

**Audit date:** 2026-09-21 · **Audited tree:** `main` at `9d1a0f90` · **Auditor:** Claude (assistant), read-only

> **This file is a derived audit report. It is not a status tracker.**
> `Unity/Docs/TASKS.md` stays the only owner of Unity task status, and `Unity/Docs/DECISIONS.md` the only owner of decisions.
> Nothing in either file was changed by this audit. Where this report disagrees with them, section D10 says so and leaves the fix to a later, authorised edit.

**How to read it fast:** section B is the one-page verdict. Section E is the order of work. Section F is what only the owner can do.

Evidence tags used everywhere below:

| Tag | Meaning |
|---|---|
| `[CODE]` | Confirmed by reading the code |
| `[AUTO]` | Confirmed by an automated check that was run |
| `[SEEN]` | Observed in the running game by the assistant |
| `[PLAY]` | Needs a human playtest; no verdict is recorded for it |
| `[HYP]` | Design hypothesis or unresolved decision |

---

## A. Scope, baseline, authority and limits

### A1. What was reviewed

- **Active game:** `Unity/Relight/` only. The TypeScript/Phaser project is the paused reference; none of its status or evidence is used as proof of anything in Unity.
- **Repository:** one git repository. The only `.git` is at the root; there are no nested repositories or submodules.
- **CodeGraph:** not available. There is no `.codegraph/` directory, so code was located with grep and direct reads.
- **Working tree:** clean, apart from `.serena/project.yml`, which was already modified and is not part of this work. There were no staged changes and no untracked source files.
- **Range reviewed in depth:** `origin/main` (`40bf13fc`) to `HEAD` (`9d1a0f90`). That is five commits, 76 files, +4,132 / −154 lines, none pushed.

| Commit | Date | Subject | Task / decision |
|---|---|---|---|
| `6df585ca` | 2026-09-20 | Design text only: light in a fight, the ending, turret alerts, strongholds | U-D-59 to U-D-62 |
| `917f51ef` | 2026-09-21 | Always dark stage 2: light matters | L-02, U-D-59 |
| `a2e134f8` | 2026-09-21 | Dry-turret and wreck alert | E-17, U-D-61 |
| `18ea235a` | 2026-09-21 | Unity check of L-02 and E-17; defence row names its place | L-02, E-17 |
| `9d1a0f90` | 2026-09-21 | Fuel, power and failure readability; HUD right column flows | GP-W6, U-D-63 |

- **Authorship:** these five commits were made in the assistant's session on this machine. Their git author is "Deedubsy" and they carry a Claude co-author trailer. Earlier commits carry the git authors "Daniel", "Deedubsy" and "relight-nightly". This audit cannot tell who or what wrote those, and does not guess.
- **Everything older** (Phases A to C, the UI/UX pass, GP-W1 to W5, L-01) was audited as "existing code", at a lower depth: status, integration and the journey, not line-by-line review.

### A2. Which document wins

Conflicts were settled by explicit owner decisions and their recorded provenance, not by file date.

1. A verbatim owner quote or an owner brief recorded in `DECISIONS.md`.
2. An owner-approved spec that a decision names as the owner of a subject (`ALWAYS_DARK_SPEC.md` for light, under U-D-58).
3. `TASKS.md` for status only.
4. `GAME_DESIGN.md`, `CONTENT_CATALOGUE.md`, `UI_AND_ONBOARDING.md` for anything no decision has touched.
5. Proposals with no decision ID (`FREIGHT_STRONGHOLD_DESIGN_2026-09-18.md`, the C-R rows, most of `RECIPE_REVIEW_2026-09-15.md`) are **proposals**, however recent.

A caution that matters for this whole report: many "owner decisions" approve a **direction** while every **number** in them was chosen by the implementer. `DECISIONS.md` says this itself for U-D-44 to 48, U-D-54, U-D-59 to 62 and all of U-D-63. Those numbers are provisional tuning, not approved design.

### A3. The six states, as found

| State | What is in it |
|---|---|
| Approved and implemented | The Home opening chain (Phase C); always-dark stage 1 and 2 sim rules (U-D-58, U-D-59); dry-turret alerts (U-D-61); warned raids and the siege plan (U-D-45 to 48); hand-craft without a lock (U-D-44); empty default recipes (U-D-53); save slots and autosave (U-D-35) |
| Approved, not implemented | The ending (U-D-60); strongholds as sieges (U-D-62); guardians (U-D-40); Stalker, Howler, full Breaker behaviour; roaming population (L-03); tram, truck, districts, map and light board (D-03 to D-07); sites, plants, cores, keys, schematics (D-08, E-01, E-02); Shotgun, Arc, Plasma, energy cells (U-D-22, U-D-25); audio (U-D-33) |
| Implemented but conflicts with approved intent | See D1: the raid account (U-D-63 e), the defence row repeat counter (U-D-55), whole-map power warnings, the second fuel formula, the terminal objective's text, the uncollectable death pile, unlock notes that gate nothing |
| Proposal or provisional tuning | The Freight stronghold design; C-R01 to C-R07; every U-P value (none has an owner play verdict); the six choices in U-D-63; Cannon dark sight 8 |
| Superseded | Day and night cycle (U-D-58); "bring the sun back" (U-D-60); U-D-10 hand-craft lock (U-D-44); U-D-50 rule (a) (rejected by the owner, replaced by U-D-51); the 46%/52% cap (U-D-52); E-to-equip (U-D-56); click-to-move (U-M-39) |
| Unverified or unresolved | Every `human` row (eight, none ever passed); 27 rows "built, owner acceptance pending"; U-Q-04; the open points listed in F1 |

### A4. Limits of this audit

- **No playthrough was performed.** Not of the opening, not of anything.
- What the assistant saw running in Unity, in total:
  - The first ~25 seconds of a New Game today (HUD, goal card, dock, inventory read by script).
  - Earlier on 2026-09-21: staged scenes for E-17, L-02 and GP-W6. Those used scripts to place machines, add fuel, schedule a raid and move aliens. Under `GAMEPLAY_AUDIT_WORKFLOW.md` a staged scene proves nothing about pacing, and none is claimed.
- L-02's placement rings, turret tint, blind badge, district sweep and guide lines have **never been seen on screen** by anyone on record.
- The enlarged UI size was not checked. A "repelled · nothing lost" raid account was not seen.
- Balance, fun, difficulty and "does it feel fair" are all `[PLAY]`. Passing tests do not answer them.
- The reference project's checks (`npm test` and friends) were **not run**. Nothing in the reference was touched, and its last recorded baseline (318 of 464 on 2026-09-11) is the owner's record, not re-measured here.
- Unavailable tools: the coplay, chrome-devtools and serena MCP servers failed to connect. None was needed.
- Nine read-only sub-agents did the wide reading. Their high-severity claims were spot-checked against the code before being written here; the ones checked are marked in G3. Lower-severity claims are reported as the agent found them and tagged `[CODE]` on the agent's reading.

---

## B. Cohesion verdict

**Short version: Relight today is a well-made first 30 minutes attached to an empty city. The pieces built recently work one by one, but several of them disagree with each other, and the game has no middle and no end.**

The ten problems that matter most, worst first:

1. **The game stops after the opening.** The last objective is a permanent card that says "Use Projects for restoration" and "connect your known destinations". There is no Projects panel, no destinations and nothing to restore. Play continues as endless raids on one base. `[CODE]` → OPN-01
2. **The approved ending is 0% built, and cannot be sized yet.** No Master Switch, no districts, no completion state. Its site, its bill of materials and its surge size have never been set, so nobody can check that the rest of the game produces enough power to win. `[CODE]` `[HYP]` → END-01 to END-05
3. **Dying is broken twice.** `[CODE]`
   - It silently deletes the backpack. The sim spills carried items into a pile that no code draws and no input can collect. The conservation test passes because the ledger still counts the invisible pile. → INT-01
   - Dying while repairing a machine away from Home can **root the engineer for good**, and the lock is saved. This is the one hard softlock found. → INT-12
4. **The middle of the game is a proposal, not a plan.** From "three turrets" to "first plant" the only written design is the Freight document, which has no decision ID. U-D-62 already builds on it. `[HYP]` → F1-03
5. **Light is the new spine, but the warnings about it read the wrong number.** Brownout, "short" and "fuel low" are all computed from the whole map's power total. A starved defence circuit next to a healthy one raises nothing. This is a defect in the assistant's own L-02 and GP-W6 work. `[CODE]` → INT-03
6. **The raid account does not do what U-D-63 says it does.** It counts kills, shots, outages and wrecks from anywhere on the map, skips raids that stage a few seconds late, and can say "repelled" for a raid nobody fought. `[CODE]` → INT-04
7. **The build menu offers dead ends from second zero.** Nothing is gated. The Alien workbench costs 30 Steel, 10 Copper, 4 Frames and 2 Boards and can never run a recipe. Wire, Frames and Boards feed only that. Three weapons exist only through the Admin panel. `[CODE]` → ECO-01, OPN-06
8. **Darkness punishes what the tutorial never teaches.** A Gun turret sees 6 tiles in the dark and a Spitter shoots from 7. The opening teaches one Lamp. The first major assault brings 20 Spitters. Nobody has played this. `[PLAY]` → OPN-04
9. **Home coal is 700 units and nothing replaces it.** That is roughly 78 to 156 minutes of generator time. The next coal is about 680 tiles away and there is no truck or tram. Turrets hold fire without power. `[CODE]` for the numbers, `[HYP]` for the stall → ECO-03
10. **Nothing has ever been accepted by a person.** Eight human gates, none passed. 27 rows are "built, owner acceptance pending". The project keeps building on unplayed ground. → F2

What is genuinely in good shape:

- The opening economy has **no deadlock**: every objective can be met from the starting stake plus hand mining, and there are no circular recipes. `[CODE]`
- Machines are never destroyed, only wrecked and repairable, and the Home core cannot be lost for good. Hand mining and unpowered hand-smelting at the indestructible Depot mean a bootstrap path always exists. The one hard softlock found is INT-12. `[CODE]`
- Save files are handled carefully: atomic writes, a `.bak` fallback, unreadable files quarantined as `.corrupt`, a complete v1 to v9 upgrade chain, and no new saved field in the five audited commits. `[CODE]`
- The sim owns every light rule; the picture never claims a mechanic the sim lacks (one exception, INT-05). `[CODE]`
- Save and hash do not disturb derived light state (`IsReading` guard, pinned by test). `[CODE]` `[AUTO]`
- The automated suite is green: 766 of 766 in Unity EditMode today. `[AUTO]`

---

## C. The journey, start to finish

"Intended" is the approved design where one exists, and says so where it is only a proposal. "Actual" is the code at `9d1a0f90`.

| # | Stage | Intended | Actual | Gap | Evidence |
|---|---|---|---|---|---|
| 0 | New Game | Start in the dark at Founders Court with 20 Steel and 5 Copper, no weapon (U-D-ORE-01, U-D-58) | Exists. Home core 300 HP, Depot, goal card, dock, clock, "In light" cue | First card asks the player to smelt Steel while holding 20 Steel; the dock numbers mean "could build", which only the tooltip explains | `[SEEN]` first 25 s · `WorldBootstrap.cs:122` |
| 1 | Gather and hand-craft | Mine ore by hand, hand-smelt five plates at the workshop (U-D-44: no lock) | Exists, tested | Copper is never taught, yet the Generator spends the whole stake and the next six builds need copper | `[CODE]` `OpeningQueries.cs:223-229, 889-922` |
| 2 | First line: power, excavator, chest, belts, Foundry | Guided chain; chest deadlocks get their own rows (U-D-54) | Exists, tested | Objective fuel sentence uses its own formula ("about 13 s" vs the HUD's "about 10 s") | `[CODE]` `OpeningQueries.cs:248-257` |
| 3 | Light | Link the court substation, place one Lamp (spec §5.6) | Exists, tested | Substation is the dearest opening item (50 Steel, 25 Copper) before any copper automation; district notice will read "Substation 0 connected" | `[CODE]` `HudViewModel.cs:447-451` |
| 4 | Arm | Craft and equip the Rifle, hand-make bullets, build, power and fill a turret | Exists, tested | One Lamp (radius 4) is taught for a turret of range 9 | `[CODE]` · feel is `[PLAY]` |
| 5 | Opening attack | 25 s warning, 5 Skitters, "repelled" needs the turret and the core alive (U-D-17, U-D-26, U-D-54) | Exists, tested | Losing never blocks the chain (good). Core-down has almost no consequence | `[CODE]` `OpeningPhase.cs:195` |
| 6 | Automate | Second Generator, Assembler on Bullets, belt-fed turret, three turrets | Exists, tested | "Second Generator" text says Home draws power; Home draws 0 kW | `[CODE]` `OpeningQueries.cs:495` |
| 7 | Hold Home | Warned minor raids, first major at 25 to 30 min, escalating (U-D-45 to 48) | Exists. Dry-turret row, brownout notice, low-fuel row, raid account | Warnings read whole-map totals; account mis-attributes; "× 2" counter; `Tuning - Siege.asset` missing so the sim runs on fallback numbers | `[CODE]` D1 |
| 8 | "Prepare to scout" | Leave Home with a reason and a target | One card: carry eight bullets, mentions "the nearest freight camp" | **The chain ends here.** The camp has no defenders, no reward, no objective | `[CODE]` `OpeningQueries.cs:592-601` |
| 9 | Explore the city | Districts discovered, surveyed, named; light-aware travel (D-03, D-04) | The 864×576 city is imported and walkable. Only mining works out there | No discovery, no map, no wayfinding beyond the goal card's Locate button | `[CODE]` |
| 10 | First expedition: Freight key camps | **Proposal only** (Freight design): three camps, three keys, raid pressure by camps alive | Missing. Camp sites are read only for distance and raid approach | Needs an owner decision before it can be "intended" | `[HYP]` |
| 11 | Freight guardian, first core | Guardian 600 HP (U-D-40, provisional). Charge phases and the two-handed core carry are proposal | Missing | Proposed carry is ~440 tiles, unarmed, through dark districts, before the truck exists | `[HYP]` |
| 12 | First plant | Any core commissions any plant for 30 Steel, 15 Copper, 600 kW (reference, retained) | Missing. Cores are item rows with no source and no use | The proposed 10-Concrete repair needs Stone from the far corner of the map, a Mixer and a recruit | `[CODE]` `[HYP]` |
| 13 | Transport | Truck from the first restored station; four-stop tram, 200 items (D-05, D-06) | Missing. Rail is drawn; stops are markers | Nothing teaches either; no objective exists | `[CODE]` |
| 14 | Recruits, restorations, Arsenal | Seven recruits, paid restorations, Arsenal unlocks Cannon and Shells | Missing. Build menu shows the unlock words as notes; Cannon and Shells are free from the start | No order or region gate is written anywhere | `[CODE]` `BuildCatalogue.cs:93-101` |
| 15 | Weapons and cells | Shotgun, Arc, Plasma, energy cells — built **last** (U-D-62) | Data rows only; Admin panel is the only source | No acquisition route is designed for any of them | `[CODE]` |
| 16 | Quarry siege, Wharf two fronts | U-D-62: lit forward base under siege; Wharf declares a raid on a plant while you fight | Missing. Layouts, garrisons, key camps unset | Quarry is authored with no streetlights | `[HYP]` |
| 17 | Light the districts | Nine lighting districts, each "connected" (U-D-60) | 9 substation lots exist as power nodes; "district live" exists only as an unsaved flag meaning throttle above 0 | Five of the nine sit where no plant is; what "connected" costs is never stated | `[CODE]` `[HYP]` |
| 18 | The ending | Build the Master Switch, throw it, survive staged surges and one final raid; stages stay lit; completion screen; play continues with raids ended | **Nothing exists** | Site, bill of materials and surge size unset; "all four factories" has no definition; final raid's "one target" vs the Home-core trip rule is unreconciled | `[CODE]` grep finds no switch, surge, district query, victory or end screen |

### C1. Progression and dependency map

```
HAND (no power)                     POWERED HOME                         REMOTE (no transport exists)
iron/copper/coal by hand ─┐
                          ├─ Home workshop ─ Steel, Copper, Bullets, Rifle
20 Steel + 5 Copper stake ┘        │
                                   ├─ Generator(coal) ─ Excavator ─ Chest ─ Foundry ─ plates 1:1
                                   ├─ Substation lot ─ streetlights ─ Lamp ─ turret sight 9 (6 dark)
                                   ├─ Assembler ─ Bullets ─ belt ─ Turret ×3
                                   │      └─ Wire ─ Frame ─ Board ─ Alien workbench ─✗ (no runnable recipe)
                                   │                                     └─ Artifact ✗ ─ Overclock ✗ (no effect)
                                   └─ Cannon + Shells (should need Arsenal; gate not applied)
                                                                         Stone (803,91)  ─ Mixer ─ Concrete ─ Barricade
                                                                         Crude (830,413) ─ Polymer ─ Assembler Mk2, fast belt
                                                                         Coal 3,000 at (777,411); 12,000 at (818,91)

NOT IN THE SIM AT ALL
camps ─ keys ─ gate ─ guardian ─ core ─ plant(600 kW) ─ station ─ truck ─ tram
recruits ─ Arsenal / Concrete crew / Electricians ─ unlock gates
Shotgun · Arc · Plasma · energy cells · Stalker · Howler · Breaker power-line detour · roamers
9 lighting districts ─ Master Switch ─ staged throw ─ final raid ─ completion
```

What the map shows:

- **No circular unlocks.** Hand mining plus unpowered hand-smelting breaks the "plates need power, power needs plates" loop. `[CODE]`
- **No economic softlock** in what is built. The one hard softlock is a state bug, not an economy one (INT-12). `[CODE]`
- **Four unreachable item groups:** Alien artifacts, the Overclock module, Power cores, and the three later weapons. `[CODE]`
- **One dead production branch** the player can pay for: Wire → Frame → Board → Alien workbench. `[CODE]`
- **One orphan recipe:** `bullet-batch-mk2` names a station no machine has. `[CODE]`
- **Costs before ingredients** (in the design, not yet in code): the proposed 10-Concrete plant repair, the Turbine hall's 40 Concrete, a Master Switch bill "from all four factories" when no factory has a defined product. `[HYP]`
- **Pointless or redundant rewards** (design): Furnace Walker and Blackout Crown give 50 plates for a cost of 45 plus 100 kW standing demand; Refined fuel is worth the same 4 MJ as coal; the Surveyors' map overlaps the light board. `[HYP]`
- **Three unreconciled raid-escalation drivers:** restoration progress (`GAME_DESIGN.md` §9.3.1), assaults survived (U-D-47, implemented), camps alive (Freight proposal). "Final-tier raid" in U-D-60 is therefore undefined. `[HYP]`

### C2. Failure cases

| Failure | What happens now | Verdict |
|---|---|---|
| Engineer dies | Respawn at Home in 10 s, full HP, weapons kept. **Backpack spilled into an invisible, uncollectable pile.** Nothing on screen says the engineer went down; only a (silent) audio cue | Defect, INT-01 and UI-10 `[CODE]` |
| Engineer dies while repairing a distant machine | The repair is not cleared. After respawn the engineer is rooted, the repair pauses because the machine is out of reach, and the only Cancel button is in that machine's drawer, which needs reach. The lock is saved | **Hard softlock**, INT-12 `[CODE]`, not reproduced |
| Turret runs dry | Defence row names the place; clears on reload | Works; "× 2" counter defect, INT-02 `[CODE]` |
| Power loss | Turrets hold fire; lights go out; outage alert once the player has ever had power | Works; warning reads the wrong total, INT-03 `[CODE]` |
| Generator out of coal | "Fuel low" row at under 120 s, then "Power · no fuel" | Works for one circuit only, INT-03 |
| Machine wrecked | Stays as a repairable wreck (2 Steel + 1 Copper per 40 HP). Belts and poles cannot be damaged | No softlock `[CODE]`. Reported twice, UI-03 |
| Home core at 0 HP | Recommission for 10 Steel + 5 Copper, 12 s; refused while attackers are near. Hand craft, mining and power keep working. The goal card says "Restore Home power… the repair restarts every machine", which is false: nothing stopped | Barely matters, and the card lies (OPN-07); refusal could last if aliens linger, CMB-07 `[HYP]` |
| Base left unattended | Raids keep coming to Home; alerts only show while the HUD is watching; nothing pulls the player back except the HUD rows | Cannot be judged until there is somewhere to go `[PLAY]` |
| Transport interrupted | No transport exists | n/a |
| Save and reload mid-progress | Objective chain is derived from live state, so it resumes correctly. Defence rows, guide-line "already taught" flags and an open raid tally are per session and are lost. Autosaves are timed only (every 5 min, 3 slots, plus one on quit); none before a raid or on death. *Since 2026-09-23 (REL-65): also one when a raid's warning opens and one when it ends, never while the engineer is down.* **Any edit to the city scene makes every older save refuse to load** | See D8: PER-02, PER-03, PER-05 |
| Resources run out | Everything is finite, enemies drop nothing. Home coal 700 is the practical limit | ECO-03 |

---

## D. Remaining tasks

**Priority scale**

- **P0** — blocks a coherent playable game, or destroys the player's progress.
- **P1** — the game tells the player something false, or a core loop is broken.
- **P2** — real but contained.
- **P3** — tidy-up, or worth doing only when the file is open anyway.

**"Authorised?" values**

- **Row exists** — a `TASKS.md` row covers it. The phase rule still applies: engineering may start early only where the row's dependencies are all `done`.
- **Needs go-ahead** — a fix to work that is built but not owner-accepted. The owner told this audit not to fix anything, so each needs a word to start.
- **Needs decision** — cannot start until the owner chooses.

Required work is D1 to D10. Optional work is D11.

---

### D1. Gameplay integration and correctness

#### AUD-INT-01 — Death makes the backpack vanish
- **Priority:** P0. Unrecoverable loss of player resources, with no message.
- **Class:** Defect (integration gap between sim and UI).
- **Now:** `DeathCache.Spill` drops every carried non-weapon item into a pile (`Sim/Actor/DeathCache.cs:94-116`). `CollectCacheCommand` and `CargoDroppedEvent` are referenced only by that file and by `Tests/Sim/Actor/LedgerTests.cs`. Nothing in `UI/`, `Presentation/` or `Input/` draws a pile, reads `st.Drops` or issues the command. `Ledger.cs:83-87` counts the pile, so conservation tests pass. `[CODE]`, spot-checked.
- **Outcome:** a dropped pile is visible in the world, the player is told where it is, and interacting within reach collects it.
- **Steps:** (1) presenter for `st.Drops.Caches`; (2) E-interact target that issues `CollectCacheCommand`; (3) a notice on `CargoDroppedEvent` with the place name; (4) goal card "Recover at Home" row mentions the pile; (5) PlayMode test: die, walk back, collect, inventory restored.
- **Files:** `Sim/Actor/DeathCache.cs`, `Presentation/` (new presenter), `UI/Hud/GameplayDock.cs` (world target), `Sim/UI/Hud/HudViewModel.cs`, `Sim/Campaign/Opening/OpeningQueries.cs`.
- **Depends on:** nothing.
- **Accept when:** after a death the pile is drawn; the HUD names it; collecting restores every item; the pile survives save and load; no pile is left when emptied.
- **Verify:** sim test exists; add PlayMode test; owner dies once on purpose in C-ACC.
- **Canonical:** newly identified. Relates to U-D-06 (respawn, claims persist) and C-13.
- **Authorised?:** Needs go-ahead.

#### AUD-INT-02 — Defence row shows "× 2" when a raid starts or ends
- **Priority:** P1. Breaks the owner's U-D-55 rule on standing rows, in the feature built to be trusted during a fight.
- **Class:** Defect (E-17, the assistant's own commit `a2e134f8`).
- **Now:** `HudViewModel.cs:381-385` detects change on `"!" + Text` but posts `Text`. `HudNotices.Post` (`HudNotices.cs:69-70`) raises `Repeats` when key and text match on a live row. A flip of `DirectorQueries.RaidExpected` with a dry turret therefore reads "× 2", then "× 3". `[CODE]` spot-checked; `[SEEN]` as "1 turret dry, 1 wreck × 2" on 2026-09-21.
- **Outcome:** a kind change on unchanged text never counts as a repeat.
- **Steps:** either let `Post` accept a kind-only change without counting, or `Clear` then `Post`. Add the missing test.
- **Files:** `Sim/UI/Hud/HudViewModel.cs`, `Sim/UI/Hud/HudNotices.cs`, `Tests/Sim/UI/HudViewModelTests.cs`.
- **Depends on:** nothing.
- **Accept when:** test: dry turret, then raid warned, then raid over → `Repeats == 1` throughout.
- **Verify:** sim test; one staged on-screen check.
- **Canonical:** E-17, U-D-55, U-D-61. Recorded as "untraced" in the TASKS log of 2026-09-21; this audit traced it.
- **Authorised?:** Needs go-ahead.

#### AUD-INT-03 — Power warnings read the whole map, not the circuit in trouble
- **Priority:** P1. Light is the defence; these are the warnings that protect it.
- **Class:** Defect (L-02 `917f51ef` and GP-W6 `9d1a0f90`, both the assistant's).
- **Now:** `PowerQueries.Network` (`Sim/Power/PowerQueries.cs:62-70`) sums demand, supply and fuel over **all** circuits. Three warnings use it: the brownout notice (`PowerAlertSource.cs:119`), the strip's " · short" (`:154`), and "Fuel low" (`:126-128`). Worked example: turret circuit with 2 coal at 300 kW lasts 27 s; a second circuit with 50 coal at 100 kW makes the average 520 s, so no warning appears. Also: `DefenceBrownedOut` walks machines only, so a district lit only by streetlights never raises the brownout notice; and the brownout row lives 10 s while low fuel stands until fixed (`HudViewModel.cs:232, 238`). `[CODE]` spot-checked. Today's single-circuit opening hides all of this.
- **Outcome:** each warning is raised by the worst circuit that has defence or light on it, and names the place.
- **Steps:** (1) per-circuit summaries from `PowerGrid.Of`; (2) low fuel = minimum fuel seconds over circuits with load; (3) brownout and "short" = any circuit with a light, streetlight or powered turret at throttle between 0 and 1; (4) brownout stands while true; (5) name the place with the same label rule as the defence row.
- **Files:** `Sim/Power/PowerQueries.cs`, `Sim/UI/Hud/PowerAlertSource.cs`, `Sim/UI/Hud/HudViewModel.cs`, `Tests/Sim/UI/PowerReadabilityTests.cs`.
- **Depends on:** AUD-INT-09 helps (one place-name rule).
- **Accept when:** tests for two circuits with uneven fuel, two circuits with one short, and a streetlight-only brownout all raise the right row.
- **Verify:** sim tests; staged two-circuit scene on screen.
- **Canonical:** L-02 (spec §5.7), GP-W6, U-D-63 (b)(c). Newly identified.
- **Authorised?:** Needs go-ahead.

#### AUD-INT-04 — The raid account claims more than it saw
- **Priority:** P1. U-D-63 (e) says "only causes supported by actual events". The code does not do that.
- **Class:** Defect (GP-W6, the assistant's).
- **Now:** `[CODE]`
  - A minor raid counts as "seen arriving" only within 2 s of `StartsAt` (`RaidAccountSource.cs:84`). The director retries staging for up to `MinorStageRetryS` and never moves `StartsAt` (`DirectorPhase.cs:324-329`). A raid that spawns 5 s late never gets an account. The test `ARaidJoinedPartWayGetsNoAccountAtAll` pins this as intended, because "loaded mid-fight" and "spawned late" look the same to the code.
  - `EnemyKilledEvent` has no raid id. Kills, `Stats.Fired`, `PowerOutageEvent` and wrecks **anywhere on the map** are tallied.
  - Likely: a major committing over a live minor closes the minor as "Raid repelled · nothing lost".
  - A core already disabled before the raid yields "repelled · nothing lost", against U-D-54.
  - A mid-raid core repair undercounts "core lost N HP".
- **Outcome:** the account only reports what the raid did, and never says "repelled" falsely.
- **Steps:** (1) director stamps the real spawn time, or the source watches the `Spawned` edge; (2) tag kills by `Enemy.Group`/raid id; (3) restrict outage and wreck clauses to the raid's target place; (4) "repelled" requires the core operational at close; (5) close a superseded minor with no account; (6) drive the tests through the real director.
- **Files:** `Sim/UI/Hud/RaidAccountSource.cs`, `Sim/Combat/Director/DirectorPhase.cs`, `Sim/Combat/Enemies/EnemyEvents.cs`, `Tests/Sim/UI/RaidAccountTests.cs`.
- **Depends on:** nothing now. Becomes urgent the moment a second place exists (AUD-EXP-04).
- **Accept when:** director-driven tests cover: late spawn, major over minor, kill and outage elsewhere, core dead beforehand.
- **Verify:** sim tests; one unscripted raid watched on screen.
- **Canonical:** GP-W6, U-D-63 (e), U-D-54. Newly identified.
- **Authorised?:** Needs go-ahead.

#### AUD-INT-05 — Drawing the screen can change what the sim decides
- **Priority:** P1 to confirm, then P1 or P3 depending on the result. The project's constitution says the sim alone mutates gameplay.
- **Class:** Validation gap (suspected defect).
- **Now:** `LightingPresenter.cs:173` calls `LightQueries.Mask`, which calls `LightPhase.Ensure`, which rebuilds the lit mask and bumps `Builds`, a route-cache key. Tick order is Power → Combat → Campaign, with light last, and `Simulation.Apply` can land commands between ticks (`InventoryPanelController.cs:1297, 1326`, `AdminPanelController.cs:57`). So after a hand-refuel, a rendered game can read a fresh mask one tick before a headless run does. Since L-02 the mask decides turret targets, perception, hesitation and routes. Two agents reached this independently by reading; **nobody reproduced it**. `[CODE]` mechanism, `[HYP]` consequence.
- **Outcome:** presentation reads the mask and never builds it; replay with and without a presenter hashes the same.
- **Steps:** (1) write the failing test first: same commands, one run calling `LightQueries.Mask` between ticks, compare hashes; (2) if it diverges, give presentation a read-only accessor and move the rebuild to a fixed point in the tick; (3) add a save → load → hash test with a lit target at the edge of reach.
- **Files:** `Sim/Campaign/Light/LightQueries.cs`, `LightPhase.cs`, `Presentation/Light/LightingPresenter.cs`, `Sim/Combat/Turrets/TurretPhase.cs:59`, replay tests.
- **Depends on:** nothing.
- **Accept when:** the new replay test passes; no presentation code path reaches `Ensure`.
- **Verify:** automated.
- **Canonical:** L-02; B-12 replay-consistency check. Newly identified.
- **Authorised?:** Needs go-ahead (investigation is safe and cheap).

#### AUD-INT-06 — "One source per figure" is not true yet
- **Priority:** P2. It is GP-W6's own acceptance line, and the owner's fun audit named contradicting numbers as a top-ten problem.
- **Class:** Defect.
- **Now:** `[CODE]`
  - `OpeningQueries.cs:248-257` computes generator fuel time itself and says "about 13 s"; `PowerQueries.FuelTimeText(13.3)` says "about 10 s". It also reads a different fuel-cap source.
  - `GameplayDock.cs:143-144` appends " · N rounds" after a description that already ends "N / cap rounds".
  - `HudViewModel.FuelText` is tested but has no reader; only the tooltip shows the estimate.
  - Only one figure in the game has a parity test.
- **Outcome:** each of fuel time, slot size, turret rounds and core HP has one producer and a parity test.
- **Steps:** route the objective sentence through `FullSlotSeconds` and `FuelTimeText`; remove the dock's extra suffix; delete or paint `FuelText`; add parity tests.
- **Files:** `Sim/Campaign/Opening/OpeningQueries.cs`, `UI/Hud/GameplayDock.cs`, `Sim/UI/Hud/HudViewModel.cs`, tests.
- **Depends on:** nothing.
- **Accept when:** parity tests exist and pass for the four figures.
- **Verify:** automated.
- **Canonical:** GP-W6, U-D-63 (a); fun-audit problem 8.
- **Authorised?:** Needs go-ahead.

#### AUD-INT-07 — "Substation 0 connected", said again on every refuel
- **Priority:** P2.
- **Class:** Defect (L-02).
- **Now:** the district notice prints the raw site name, and the importer names sites "Substation N" (`HudViewModel.cs:447-451`, `Editor/World/RegionImporter.cs:449`). `18ea235a` fixed naming for the defence row only, in a private helper. The event fires on every throttle 0 → above 0 edge, so refuelling a dry Generator announces "connected" again, with the sweep. `[CODE]`
- **Outcome:** the notice names the place, and fires once per real connection.
- **Steps:** share the label rule (see AUD-INT-09); distinguish "first ever live" from "power came back".
- **Files:** `Sim/UI/Hud/HudViewModel.cs`, `Sim/UI/Hud/DefenceAlertSource.cs`, `Sim/Campaign/Light/LightPhase.cs`.
- **Depends on:** AUD-INT-09.
- **Accept when:** test on the real city's site names; a starved belt-fed Generator does not repeat the notice.
- **Verify:** sim test.
- **Canonical:** L-02, spec §5.4.
- **Authorised?:** Needs go-ahead.

#### AUD-INT-08 — "Base under attack" for five minutes after the attack
- **Priority:** P2.
- **Class:** Validation gap (likely defect, predates the recent commits).
- **Now:** `DirectorQueries.Warning` returns kind "recovery" with 0 s left while `T < RecoveryUntil` (300 s). `HudViewModel.ThreatLine` (`:489-516`) treats any kind with 0 s left as urgent. The account row "repelled · nothing lost" now sits in the same column. Read, not run; no test names "recovery". `[CODE]` likely.
- **Outcome:** recovery reads as recovery.
- **Steps:** test the threat line in recovery and withdrawal; give those kinds their own wording.
- **Files:** `Sim/UI/Hud/HudViewModel.cs`, `Sim/Combat/Director/DirectorQueries.cs`.
- **Accept when:** tests pin the text for every warning kind.
- **Verify:** sim test; watch one major end on screen.
- **Canonical:** GP-W3. Newly identified.
- **Authorised?:** Needs go-ahead.

#### AUD-INT-09 — One district query, one sight-line rule
- **Priority:** P2 now, **P0 for the ending**: U-D-60 defines a district as "the tiles nearest each substation", and that rule does not exist as a query.
- **Class:** Integration gap (duplicated logic).
- **Now:** "nearest substation" is written three times (`PowerNetwork.cs:167-185`, `OpeningQueries.cs:825`, `DefenceAlertSource.PlaceOf`). Line of sight is written twice (`LightRules.StampWindow` has its own `LineClear`; `Sim/Combat/Sightline.cs`). `[CODE]`
- **Outcome:** one `Districts` query (tile → district, district → name, district → substation) and one sight-line rule.
- **Files:** new `Sim/World/Districts.cs`; the three callers; `LightRules.cs`.
- **Depends on:** nothing. Feeds AUD-INT-03, AUD-INT-07, AUD-END-01, AUD-EXP-06.
- **Accept when:** the three call sites use it; existing tests still pass; a test pins all nine districts on the real city.
- **Verify:** automated.
- **Canonical:** D-04, D-07, F-05b. Newly identified as a shared prerequisite.
- **Authorised?:** Needs go-ahead.

#### AUD-INT-10 — Small gaps in the blind and defence signals
- **Priority:** P3.
- **Class:** Defect.
- **Now:** `[CODE]` "Blind" needs `Target == 0`, so a turret shooting one alien while an unlit Spitter shells it gets no badge (`TurretQueries.cs:120-146`). A place whose turrets are all wrecked before any ran dry gets no defence row (`DefenceAlertSource.cs:75, 93`). `MachineActivityVisual` runs the full blind query per turret per frame.
- **Outcome:** the badge matches the guide line that teaches it; wrecked-only places are reported.
- **Files:** `Sim/Combat/Turrets/TurretQueries.cs`, `Sim/UI/Hud/DefenceAlertSource.cs`, `Presentation/Flow/MachineActivityVisual.cs`.
- **Accept when:** tests for "blind while firing" and "all turrets wrecked".
- **Canonical:** L-02, E-17.
- **Authorised?:** Needs go-ahead.

#### AUD-INT-11 — A turret inside the workshop walls never fires
- **Priority:** P2. Legal, silent and fatal, and it has been open since 2026-09-14.
- **Class:** Defect (open recorded finding; fun-audit problem 9).
- **Now:** recorded in the TASKS log of 2026-09-14 and in `GAMEPLAY_FUN_AUDIT.md`. This audit found no placement rule or message that addresses it, but did not reproduce it. `[CODE]` absence only.
- **Outcome:** either placement refuses it with a reason, or the turret's hover says why it cannot see.
- **Steps:** reproduce first (check the premise); then choose refusal or explanation.
- **Files:** `Sim/Building/Placement.cs`, `Sim/Combat/Turrets/TurretQueries.cs`.
- **Accept when:** a test places a turret inside the rect and gets a refusal or a stated reason.
- **Canonical:** fun-audit problem 9; TASKS log 2026-09-14.
- **Authorised?:** Needs go-ahead.

#### AUD-INT-12 — Dying during a machine repair roots the engineer for good
- **Priority:** P0. The only hard softlock found, and it is written into the save.
- **Class:** Defect (state transition; GP-W5 era, not one of the five audited commits).
- **Now:** `[CODE]` spot-checked, **not reproduced in the running game**.
  - `Engineer.TakeDamage` (`Sim/Actor/Movement/EngineerMovement.cs:31-50`) clears the route and spills cargo but leaves `st.Home.RepairKind` set.
  - `Home.RepairLocked` (`HomeCore.cs:189`) is false only while the engineer is down, so it turns true again on respawn, and `EngineerMovement.cs:99` then blocks all movement.
  - `HomeCorePhase.cs:48` pauses the repair while the machine is out of reach, so it never finishes and never aborts.
  - Cancel exists in two places. The workshop core card shows it only for a **core** repair (`HomeQueries.cs:41`). The machine's own drawer shows it (`InventoryPanelController.cs:540-541`), and opening that drawer needs reach.
  - The repair state is saved, so within 15 minutes all three autosave slots hold the lock.
- **Outcome:** going down always ends a running repair, and a rooted engineer can always cancel.
- **Steps:** (1) reproduce first: repair a turret away from the spawn, take lethal damage; (2) clear the repair in `TakeDamage` (decide whether the paid patch is refunded); (3) see AUD-INT-13 for the cancel route; (4) on load, clear a machine repair whose machine is out of reach.
- **Files:** `Sim/Actor/Movement/EngineerMovement.cs`, `Sim/Campaign/HomeCore/HomeCore.cs`, `HomeCorePhase.cs`, `Tests/Sim/Campaign/HomeCore/HomeCoreTests.cs`.
- **Depends on:** nothing.
- **Accept when:** a sim test kills the engineer mid-repair and then walks them; a save made in the locked state loads unlocked.
- **Verify:** sim test; one on-screen reproduction before and after.
- **Canonical:** newly identified. Relates to GP-W5 (U-D-48 repair), U-D-44 (the repair is the one remaining hand lock).
- **Authorised?:** Needs go-ahead.

#### AUD-INT-13 — "Escape cancels" is promised and does nothing
- **Priority:** P1. It is the instruction on screen while the player is pinned in a fight.
- **Class:** Implemented behaviour that conflicts with intent (UI promises what input does not do).
- **Now:** the HUD and the workshop both print "Repairing — Cancel to move. · Escape cancels" (`HudViewModel.cs:50`, `WorkshopText.cs:49`; two tests pin the sentence). `UI/EscapeChain.cs` has no link that sends `CancelRepairCommand`; the only senders are the two panel buttons. Escape closes the drawer, which hides the only Cancel button. Per agent 9, W/A/S/D also closes the drawer while rooted (`UiShell.cs:141-150`), not re-checked. `[CODE]`
- **Outcome:** Escape cancels a running repair before it closes anything, as the text says.
- **Steps:** add an Escape link above "close drawer" that sends `CancelRepairCommand` while `Home.RepairLocked`; PlayMode test.
- **Files:** `UI/EscapeChain.cs`, `UI/UiShell.cs`, `Tests/Play/`.
- **Depends on:** nothing. Pairs with AUD-INT-12.
- **Accept when:** PlayMode test: start a repair, press Escape once, the engineer can walk.
- **Verify:** PlayMode test; C-ACC.
- **Canonical:** U-D-44, `UI_AND_ONBOARDING.md` Escape order. Newly identified.
- **Authorised?:** Needs go-ahead.

#### AUD-INT-14 — Picking up a wreck and placing it again repairs it for free
- **Priority:** P3. **Class:** Design hypothesis (exploit or escape hatch).
- **Now:** per agent 9, damage is keyed by machine id and a re-placed machine gets a new id (`Sim/Building/Placement.cs:67, 100-125`); no wreck test on pick-up. Not re-checked by the assistant. `[CODE]` on the agent's reading.
- **Outcome:** an owner ruling; if it is an exploit, carry integrity with the packed item or refuse pick-up of a wreck.
- **Canonical:** GP-W5, U-D-48. Newly identified.
- **Authorised?:** Needs decision (F1-17).
- **Confirmed in code 2026-09-23 (REL-115):** `Placement.Remove` has no wreck test and returns the machine and its contents; placing it again gives full hit points. Since REL-115 every machine can be a wreck and keeps its items (U-D-69 f), so a pick-up also gets round the wreck's lock on its contents. Nothing changed; still F1-17.

---

### D2. Opening, onboarding and guidance

#### AUD-OPN-01 — The last objective promises a game that is not there
- **Priority:** P0 for the first playable milestone. It is the last thing the player reads.
- **Class:** Implemented behaviour that conflicts with intent (UI promises what the sim lacks).
- **Now:** `OpeningQueries.cs:598-601` returns a permanent card: "Keep your workshop producing… Explore and connect your known destinations… Use Projects for restoration and service details." No Projects panel, destinations or restoration exist. A test pins the text. `[CODE]`
- **Outcome:** the terminal card says only true things, and points at the next real goal.
- **Steps:** short term, reword to what exists (hold Home, light more ground, scout). Properly, replace it with the first expedition objective (AUD-EXP-04) and the exploration objective (D-10).
- **Files:** `Sim/Campaign/Opening/OpeningQueries.cs`, `Tests/Sim/Campaign/Opening/OpeningObjectiveTests.cs`.
- **Depends on:** the owner's word on GP-W7 for the proper fix. The rewording depends on nothing.
- **Accept when:** no objective text names a feature that does not exist; a test scans objective text for a deny-list ("Projects", "destinations", "restoration") until those exist.
- **Verify:** sim test; C-ACC.
- **Canonical:** GP-W7, D-10; fun-audit problems 5 and 10.
- **Authorised?:** Rewording needs go-ahead. The real fix needs the owner's word on GP-W7.

#### AUD-OPN-02 — "Prepare to scout" sends the player to an empty camp
- **Priority:** P0 (same milestone). Folded into AUD-EXP-04; listed so the text is not forgotten.
- **Class:** Integration gap.
- **Now:** `OpeningQueries.cs:592` asks for eight bullets and mentions "the nearest freight camp". Camp sites are read only for distance and raid approach; their defender counts are never read. C-ACC's excursion step may not be passable as written. `[CODE]`
- **Canonical:** GP-W7, D-08, C-ACC.
- **Authorised?:** Needs the owner's word (GP-W7).

#### AUD-OPN-03 — Copper is never taught
- **Priority:** P2.
- **Class:** Validation gap.
- **Now:** the Generator costs the whole stake (20 Steel, 5 Copper). Poles, Foundry (5), Rifle (4), turret (5), Assembler (20) and substation (25) all need hand-mined, hand-smelted copper. No objective row covers it; only the shortage hint does. `[CODE]` for the facts; whether players stall is `[PLAY]`.
- **Outcome:** decided after a playtest, not before.
- **Steps:** watch for it in C-ACC. If players stall, add one row or fold copper into the first smelt.
- **Files:** `Sim/Campaign/Opening/OpeningQueries.cs`.
- **Canonical:** U-D-ORE-01, C-ACC. Newly identified.
- **Authorised?:** Observation only; a change needs go-ahead.

#### AUD-OPN-04 — The dark-sight rule is never taught before it bites
- **Priority:** P1 to **play**, not to change.
- **Class:** Validation gap.
- **Now:** Gun turret sight is 9 lit, 6 dark (U-D-59, U-P-11, owner-set as provisional). Spitter range is 7. The opening teaches one Lamp of radius 4. The first major has 20 Spitters in 60. Power budget at that step is about 265 of 300 kW. `[CODE]` numbers; fairness is `[PLAY]`.
- **Outcome:** an owner verdict on "does dark sight feel fair", recorded in L-ACC.
- **Steps:** run L-ACC through the first major assault, unassisted.
- **Canonical:** L-ACC, U-P-11, U-P-12.
- **Authorised?:** Owner playtest.

#### AUD-OPN-05 — Small wrong sentences in the opening
- **Priority:** P3.
- **Class:** Defect.
- **Now:** `[CODE]` "Ore processing, Home and ammunition production exceed one Generator's 300 kW" (`OpeningQueries.cs:495`), but Home draws 0 kW and the load passes 300 kW only later. The first card asks for Steel while the player holds 20 Steel `[SEEN]`. The guide line says aliens "look for a dark way in", but the 0.65 s hesitation re-routes nothing.
- **Files:** `Sim/Campaign/Opening/OpeningQueries.cs`, `Sim/UI/Hud/HudViewModel.cs`.
- **Accept when:** each sentence is true of the sim.
- **Authorised?:** Needs go-ahead.

#### AUD-OPN-06 — Unlock notes that gate nothing
- **Priority:** P1. The player can spend the opening stake on machines that do nothing.
- **Class:** Implemented behaviour that conflicts with intent (catalogue says "Implemented and retained" for gates that are absent).
- **Now:** `BuildCatalogue.cs:93-101` lists every row; the Unlock text shows as a "Catalogue note". `SetRecipeCommand.cs:29-31` says the Arsenal gate "is not applied yet". Cannon, Shells, Mixer, Barricade, Big pole, Arc lamp, Floodlight, Alien workbench and Assembler Mk2 are available at second 0. `[CODE]`
- **Outcome:** proper fix is E-01 (progression state). Until then, the build menu must not offer machines that cannot work.
- **Steps:** interim: hide or mark "not yet usable" any machine with no runnable recipe and any row whose gate has no source. Proper: E-01.
- **Files:** `Sim/UI/Build/BuildCatalogue.cs`, `UI/Build/BuildPanelController.cs`.
- **Depends on:** the interim step is a **proposal** and needs the owner's yes. E-01 depends on D-08.
- **Accept when:** no buildable row leads to a machine with zero runnable recipes.
- **Canonical:** E-01, F-01, `CONTENT_CATALOGUE.md` §4.
- **Authorised?:** Interim needs a decision. E-01 row exists.

#### AUD-OPN-07 — The core-down goal card is false
- **Priority:** P1. While the core is down this card replaces the whole objective chain.
- **Class:** Implemented behaviour that conflicts with intent (`UI_AND_ONBOARDING.md` §7.3 row 0b and defect U-9 require truthful core-down guidance).
- **Now:** `OpeningQueries.cs:211-216` titles the card "Restore Home power" and says "hold E; the repair restarts every machine that was running". `CoreOperational` is read only by `OpeningPhase.cs:195` and `OpeningQueries.cs:204`: power, production, turrets, the Depot and respawn ignore it, so nothing stopped and nothing restarts. The real interaction is a press plus a button, not a hold. `[CODE]` on agent 9's reading; the two readers of `CoreOperational` match agent 5's independent finding.
- **Outcome:** the card says what a dead core costs and how to fix it, truthfully. What a dead core *should* cost is AUD-CMB-07.
- **Files:** `Sim/Campaign/Opening/OpeningQueries.cs`, its tests.
- **Depends on:** AUD-CMB-07 for the final wording; a truthful interim needs nothing.
- **Accept when:** no sentence on the card describes an effect the sim lacks.
- **Verify:** sim test on the text; C-ACC.
- **Canonical:** C-05, U-D-54, UI defect U-9.
- **Authorised?:** Needs go-ahead.

---

### D3. Progression, economy, crafting and automation

#### AUD-ECO-01 — The Alien branch is dead weight
- **Priority:** P1 (paired with AUD-OPN-06).
- **Class:** Integration gap (dead content).
- **Now:** `[CODE]` spot-checked. Alien artifacts come only from `alien-decode`, which has no inputs; `ProductionRules.cs:48-69` offers only recipes with inputs, and `MachinePhase.cs:178-179` says decode is not ported. The Overclock module needs artifacts and its speed effect is not applied. Wire, Frame and Board feed only this branch. Power cores 1 to 3 have no source and no use.
- **Outcome:** artifacts come from camps (reference design), decode is a campaign action, Overclock applies its ×1.1.
- **Steps:** delivered by E-01 to E-03 and D-08. Until then see AUD-OPN-06.
- **Files:** `Sim/Production/Machines/ProductionRules.cs`, `MachinePhase.cs`, `Sim/Data/Generated/CatalogueData.g.cs` (via the exporter, not by hand).
- **Depends on:** D-08, E-01.
- **Accept when:** a test obtains an artifact through play actions and the module changes a machine's rate.
- **Canonical:** E-01, E-03, F-01, U-D-39.
- **Authorised?:** Row exists.

#### AUD-ECO-02 — Orphan recipe `bullet-batch-mk2`
- **Priority:** P3.
- **Class:** Defect (data).
- **Now:** its station "Assembler Mk2" matches no machine; the Mk2's station is "Assembler" with a speed multiplier. Already noted in `RECIPE_REVIEW_2026-09-15.md:31`. `[CODE]`
- **Steps:** remove or re-station in the exporter; add a `DataValidator` rule "every recipe's station exists".
- **Canonical:** RECIPE_REVIEW (proposal), GP-W8.
- **Authorised?:** Needs go-ahead; GP-W8 covers it.

#### AUD-ECO-03 — Home coal runs out before anything can replace it
- **Priority:** P1 to measure.
- **Class:** Validation gap (balance).
- **Now:** yard coal is 700 units = 2,800 MJ = 156 min at 300 kW or 78 min at 600 kW. A coal Excavator left running empties the patch in about 23 min. Next coal is about 680 tiles east. No truck, no tram. Turrets hold fire without power; hand tools still work, so it is a stall, not a lock. `[CODE]` numbers, `[HYP]` outcome.
- **Outcome:** a measured answer: how long does Home last in ordinary play, and is that before or after the player can fetch more.
- **Steps:** (1) headless scripted run of the full opening plus 90 min, logging coal; (2) owner session; (3) only then choose: bigger patch, second patch in walking range, or bring the first plant forward.
- **Files:** `World/OpeningResourceLayout.cs:34-36` if a change is approved.
- **Depends on:** AUD-EXP-04 changes the answer.
- **Accept when:** a recorded run shows coal lasting past the first point where the player can get more.
- **Canonical:** GP-W8, U-D-ORE-01, F-02.
- **Authorised?:** Measuring is safe. Changing the patch needs a decision.
- **Update 2026-09-23 (REL-27, U-P-27):** U-D-69 (c) chose numbers, and the yard pile is now **2,100** Coal (was 700), in `OpeningResourceLayout` and the World scene. The REL-88 runner, rerun: power out at minute 233.3 at 600 kW (was 77.8) and 466.7 at 300 kW (was 155.6). The edge Excavator still reaches 3 of 9 tiles (702 Coal). Step (1) is met by that runner; step (2), the owner session, is still owed, so the accept line is not yet shown in play: today the only other coal is East Wharf on foot, and nothing hauls coal to Home until GP-W7.

#### AUD-ECO-04 — Three machines still ship with a default recipe
- **Priority:** P3. **Class:** Proposal requiring approval.
- **Now:** U-D-53 emptied the Foundry, Assembler and Mk2. Refinery, Mixer and Alien workbench keep defaults. The decision row records this as open. `[CODE]`
- **Canonical:** U-D-53. **Authorised?:** Needs decision (F1-08).

#### AUD-ECO-05 — Old saves load their Foundry idle
- **Priority:** P3. **Class:** Defect (open recorded finding, 2026-09-16).
- **Now:** a v9 save made before U-D-53 loads with no recipe set and no message. Also `GameDataRegistry.BuildLegacy` (`:88`) applies DarkWorld only, so a version-0 save gets no Breaker and no derived machine integrity. `[CODE]`
- **Outcome:** either a one-line notice on load or a migration.
- **Canonical:** U-D-53, GP-OPEN-1. **Authorised?:** Needs go-ahead.

#### AUD-ECO-06 — Production balance pass
- **Priority:** P2. **Class:** Missing approved feature.
- **Now:** GP-W8 is `todo` and waits on GP-W7. The fun audit's "the factory has no consumer" is only partly answered: turrets consume bullets, nothing consumes plates at scale. `[HYP]`
- **Canonical:** GP-W8, F-02. **Authorised?:** Row exists; waits on GP-W7.

---

### D4. Exploration, world content and transport

These are existing `TASKS.md` rows. They are listed so the execution plan in E is complete; their full task text lives in `TASKS.md` and is not copied.

#### AUD-EXP-01 — `CityValidator` (finish D-02b)
- **Priority:** P0 on the critical path. It is the authoring gate for D-08 and E-15, and D-03 and D-04 wait on it.
- **Class:** Missing approved feature.
- **Now:** the full city imports; the validator is not built. D-02a's idempotent re-import acceptance has no recorded check. `[CODE]` per TASKS and code search.
- **Outcome:** the ported `validateRiverfront` rule set runs at import and fails loudly.
- **Files:** `Editor/World/RegionImporter.cs`, new `Editor/World/CityValidator.cs`.
- **Accept when:** as the D-02b row says.
- **Canonical:** D-02b. **Authorised?:** Row exists; its dependencies are done.

#### AUD-EXP-02 — Full-map navigation (D-03)
- **Priority:** P0 path. E-05, D-05 and everything after depend on it.
- **Class:** Missing approved feature. **Now:** missing. **Depends on:** AUD-EXP-01.
- **Note from this audit:** every mask rebuild flushes the whole route cache and each field allocates three map-sized arrays on 864×576. Budget this in D-03, do not discover it in F-07. `[CODE]`
- **Canonical:** D-03. **Authorised?:** Row exists.

#### AUD-EXP-03 — Districts, discovery and the exploration objective (D-04, D-10)
- **Priority:** P0 path. **Class:** Missing approved feature. **Now:** missing; only district labels exist.
- **Note:** U-D-60 says the nine *lighting* districts are **not** D-04's region list. Build AUD-INT-09 first so the two ideas do not get merged by accident.
- **Canonical:** D-04, D-10. **Authorised?:** Row exists; waits on D-02b.

#### AUD-EXP-04 — The first expedition (GP-W7, first slice of D-08)
- **Priority:** P0. This is the smallest thing that gives the game a second act.
- **Class:** Missing approved feature; its detailed design is a proposal.
- **Now:** GP-W7 is `todo` and "needs the owner's word". It covers restoring the importer's omitted site kinds and the first camp's encounter, reward and follow-through. The sim has no Project, Plant or Artifact site kind (`Sim/World/WorldSites.cs`); `Editor/World/CityFile.cs:184` parses nine `projects` that never reach the sim. `[CODE]`
- **Outcome:** one camp, with defenders that use the authored counts, a reward the player can use, an objective that sends them there and one that brings them back.
- **Steps:** (1) owner decides how much of the Freight proposal to adopt (F1-03); (2) site kinds through the importer; (3) camp guards; (4) reward; (5) objectives replacing AUD-OPN-01 and AUD-OPN-02; (6) a recorded unassisted run.
- **Depends on:** the owner's word; AUD-INT-01 (players will die out there); AUD-INT-04 (a second place makes the account wrong).
- **Accept when:** as the GP-W7 row says, plus: C-ACC's excursion step is passable.
- **Canonical:** GP-W7, D-08, C-ACC. **Authorised?:** Needs the owner's word.

#### AUD-EXP-05 — Tram and truck (D-05, D-06)
- **Priority:** P1 path. E-02, E-04 and E-15 all need the truck. **Class:** Missing approved feature. **Now:** missing; rail and four stops are imported as picture and markers.
- **Note:** nothing in the approved design teaches either. Write the objective with the feature.
- **Canonical:** D-05, D-06. **Authorised?:** Row exists; waits on D-03.

#### AUD-EXP-06 — City map and light board (D-07)
- **Priority:** P1 path; the ending reads from it. **Class:** Missing approved feature.
- **Blocker:** whether named pins stay on the map is open by U-D-60's own text (F1-05).
- **Canonical:** D-07, U-D-60. **Authorised?:** Row exists; one decision open.

#### AUD-EXP-07 — Sites, plants, cores, keys (rest of D-08, E-01, E-02)
- **Priority:** P1 path. **Class:** Missing approved feature. **Now:** 72 site records imported as data only.
- **Canonical:** D-08, E-01, E-02. **Authorised?:** Rows exist.

---

### D5. Combat, defence, power and darkness

#### AUD-CMB-01 — Full raid director, and one escalation rule (E-05, E-16)
- **Priority:** P1 path. **Class:** Missing approved feature, plus an unresolved conflict.
- **Now:** the siege director exists (GP-W3, W4) but E-05 is still plain `todo`. Two schemes disagree: U-D-47 escalates 15% per assault capped at 2× (about 120 bodies); U-D-37 sets later tiers of 90 to 140 and 160 to 220. The Freight proposal adds a third driver. Radio-gated warnings (`GAME_DESIGN.md` §9.3, U-D-60) are called "retired" in `DirectorPhase.cs:8, 281` with no decision behind the word. `[CODE]`
- **Outcome:** one written escalation rule; "final-tier raid" defined; radio gating decided.
- **Canonical:** E-05, E-16, U-D-37, U-D-47, U-P-07. **Authorised?:** Rows exist; two decisions open (F1-06, F1-07).

#### AUD-CMB-02 — Roster: Stalker, Howler, and the Breaker's real job (E-13, E-14)
- **Priority:** P1 path. **Class:** Missing approved feature.
- **Now:** the Breaker body ships and the siege plan spawns it, but spec §5.8 (go for the power line) has no code, and its role text "pushes through light" describes behaviour nothing implements. E-13 is plain `todo` although part of it shipped. Approach sounds (U-D-62) are absent. `[CODE]`
- **Canonical:** E-13, E-14, U-D-48 (f), spec §5.8. **Authorised?:** Rows exist; blocked behind E-05 → D-03 → D-02b.
- **Note 2026-09-23 (REL-115):** GP-W5's Breaker, which bit any machine it walked past, is gone under U-D-69 (h); every raider now goes for turrets and cannons only and breaks other machines only when they block its path. Poles now have hit points, which §5.8 would need. Whether §5.8's power-line detour is an exception to (h) is F1-38.

#### AUD-CMB-03 — Strongholds (E-06, E-07, E-15)
- **Priority:** P1 path. **Class:** Missing approved feature; content design is a proposal or unset.
- **Now:** nothing authored for Freight, Quarry or Wharf. U-D-62 sets direction only.
- **Canonical:** E-06, E-07, E-15, U-D-40, U-D-62. **Authorised?:** Rows exist; layouts and garrisons need decisions.

#### AUD-CMB-04 — Weapons and energy cells, last (E-09 to E-12)
- **Priority:** P2 (U-D-62 orders these last). **Class:** Missing approved feature.
- **Now:** no acquisition route exists in design or code. `CONTENT_CATALOGUE.md` marks Shotgun acquisition "Implemented and retained"; it is not. `[CODE]`
- **Canonical:** E-09 to E-12, U-D-22, U-D-25. **Authorised?:** Rows exist.

#### AUD-CMB-05 — Roaming population in dark districts (L-03)
- **Priority:** P2. **Class:** Missing approved feature. **Now:** no code. **Depends on:** E-13.
- **Canonical:** L-03, spec §6. **Authorised?:** Row exists.

#### AUD-CMB-06 — The siege tuning asset does not exist
- **Priority:** P2. **Class:** Defect (open recorded finding, 2026-09-15).
- **Now:** no `*Siege*.asset` under `Assets/`; `DataValidator` reports a missing reference and the sim runs on `SiegeTuning.Fallback`. Spot-checked. `[CODE]`
- **Outcome:** the asset is generated, or the fallback is declared the source and the validator stops complaining.
- **Canonical:** GP-W3/W4. **Authorised?:** Needs go-ahead.

#### AUD-CMB-07 — Does losing the Home core matter?
- **Priority:** P2. **Class:** Design hypothesis.
- **Now:** at 0 HP only two things change: the opening attack cannot be "repelled", and a "Restore Home power" row appears. Recommission is refused while attackers are near, with no timeout. `[CODE]`. Whether aliens can linger forever is `[HYP]`. The 2026-09-21 staged scene saw aliens "standing at the core for some minutes" `[SEEN]`, under a script, so it proves nothing about ordinary play.
- **Also, per agent 9 (plausible, not re-checked):** "attackers nearby" is defined as any live major or minor raid body **anywhere on the map**, and ordinary raid bodies never withdraw on their own; only the opening encounter calls `Director.Withdraw`. After AUD-INT-01 the player has no bullets, so they may be unable to clear them.
- **Steps:** reproduce unscripted; bring the question to the owner with the ending's trip rule (the Home core trips the Master Switch, so by then it must matter).
- **Canonical:** U-D-54, U-D-60. **Authorised?:** Needs decision after reproduction.

#### AUD-CMB-08 — Walls block light and turret sight, but not alien sight
- **Priority:** P3. **Class:** Design hypothesis.
- **Now:** enemies use `Geometry.Sight`, which covers authored geometry only; light and turrets are blocked by player walls. `[CODE]`. May be intended; nobody has written it down.
- **Canonical:** spec §5.3. **Authorised?:** Needs decision.

---

### D6. The ending (U-D-60)

Verified from the decision row and `GAME_DESIGN.md` §11.1: the campaign is complete when **every lighting district is live through the Master Switch and the raid declared by the last throw is repelled**. Nine districts, defined as the tiles nearest each of nine substation sites. The player chooses the moment. Each stage puts a surge on the circuit feeding that district. A stage holds after 60 s at throttle 0.5 or better (U-P-13, the assistant's placeholder). The switch trips if that circuit dies or the Home core is disabled; finished stages stay lit. No defeat screen, no score, no timer. Play continues with raids ended. Plants, cores and strongholds are **not** named as requirements.

#### AUD-END-01 — Sim groundwork the ending needs
- **Priority:** P0 for the ending; can start early because it depends on nothing.
- **Class:** Missing approved feature (prerequisites).
- **Now:** no Project site kind; no district query; "district live" is an unsaved flag meaning throttle above 0; no completion state in the save. `[CODE]`
- **Steps:** AUD-INT-09; a Project site kind through the importer; saved per-district state (save schema 10); a test that nine live districts still leave enemies a route (spec §5.2 guarantees one).
- **Canonical:** F-05a, F-05b, D-08. **Authorised?:** Rows exist; F-05a formally waits on E-ACC (see D10-07).

#### AUD-END-02 — Set the three missing numbers (F-05a)
- **Priority:** P0 decision. **Class:** Unresolved decision.
- **Now:** Master Switch site, bill of materials and surge size are all unset. "Drawn from all four factories" has nothing to bind to: no document gives any of the four yards a product. `[HYP]`
- **Outcome:** the owner picks a site and approves a bill and a surge; the four factories get products first, or the wording changes.
- **Canonical:** F-05a, U-D-60, U-P-13. **Authorised?:** Needs decision (F1-01, F1-02).

#### AUD-END-03 — Can the game be won? A sizing check
- **Priority:** P0 validation, before F-05b is built.
- **Class:** Validation gap.
- **Now:** supply in the design is three plants at 600 kW, a 600 kW Turbine hall and 300 kW coal Generators on finite coal. Five of the nine substations sit in districts with no plant, three of them inside or beside strongholds. The Quarry has no streetlights. A final raid of 160 to 220 bodies under an active cap of 48 has no stated duration, against a switch-on of at least nine minutes. `[HYP]`
- **Steps:** a spreadsheet or headless model: nine surges, the lit area's standing demand, the raid's length. Answer "is this winnable with the approved content" on paper first.
- **Canonical:** F-05b, E-16, U-D-37. **Authorised?:** Safe to do now.

#### AUD-END-04 — Who picks the final raid's target?
- **Priority:** P1 decision. **Class:** Unresolved decision.
- **Now:** the final raid has "one target" through the existing director; the trip rule names the Home core. The documents do not say the target is Home. `[HYP]`
- **Canonical:** U-D-60. **Authorised?:** Needs decision (F1-04).

#### AUD-END-05 — Throw, stages, final defence, completion (F-05b, F-05c)
- **Priority:** P0 for a finishable game. **Class:** Missing approved feature. **Depends on:** AUD-END-01 to 04, E-16.
- **Also unstated:** what "fully lit on every street tile" means inside an emitter-based light model (do buildings still shadow; what does a later brownout do); why the guaranteed dark route is kept in a post-game with no raids.
- **Canonical:** F-05b, F-05c. **Authorised?:** Rows exist.

---

### D7. UI, feedback, accessibility and controls

#### AUD-UI-01 — The right column can outgrow the screen
- **Priority:** P2. **Class:** Validation gap (GP-W6).
- **Now:** worked out from the USS, not seen. In compact mode (under 1100 wide or 800 tall, which includes 1280×720) the engineer block is at top 70 and the stack at top 140; with a weapon equipped the block is about 120 px and should run under the stack. The stack has no max-height. `.drawer-open` hides `#problems` only. Labels are pickable, so the column eats world clicks. Dead `#alerts{top:96px}` and `#problems{bottom:160px}` rules remain in `Hud.uss`. The 720p check on 2026-09-21 had no weapon equipped. `[CODE]` arithmetic; `[SEEN]` only for the no-weapon case.
- **Steps:** screenshots at 1280×720 with a weapon, at UI scale 1.25 and 1.5, with three notices and three problems.
- **Files:** `UI/Hud/Hud.uss`, `UI/Styles/industrial.uss`, `UI/Hud/Hud.uxml`.
- **Accept when:** no overlap in those captures; a PlayMode layout assertion covers compact plus weapon.
- **Canonical:** GP-W6, U-D-63 (f). **Authorised?:** Checking is safe; fixing needs go-ahead.

#### AUD-UI-02 — Three rows is not enough, and nothing says so
- **Priority:** P2. **Class:** Integration gap.
- **Now:** `HudNotices.MaxRows` is 3 and there are nine places. Standing defence rows and the 130-character low-fuel sentence can fill it. `HudNotices.Dismiss` has no caller. Three higher-ranked problem groups push "Low power" off. No "+N more". `HudProblem.MachineId` has no consumer, so no row can locate its machine. `[CODE]`
- **Outcome:** an overflow line, and rows that can be clicked to locate. Click-to-locate is a **proposal**.
- **Canonical:** GP-W6 (d), U-D-55. **Authorised?:** Overflow needs go-ahead; locate needs decision.

#### AUD-UI-03 — A wreck is reported twice, in two vocabularies
- **Priority:** P3. **Class:** Defect (E-17 and GP-W6 overlap).
- **Now:** the problems row says "2 × Gun turret: disabled (0 hp) — walk to it and repair it"; the defence row says "2 wrecks", and counts walls. `[CODE]` `[SEEN]`
- **Canonical:** E-17, GP-W6. **Authorised?:** Needs go-ahead.

#### AUD-UI-04 — Five PlayMode tests fail, unexplained
- **Priority:** P1. A red baseline hides the next regression.
- **Class:** Validation gap.
- **Now:** 88 tests, 78 pass, 5 fail, 5 skipped; identical before and after the five audited commits. `[AUTO]`
  - `ControlsCorrectionTests.AMovementKeyClosesTheOpenDrawerAndTheEngineerWalksWithoutPressingItAgain`
  - `ControlsCorrectionTests.RealWorldCarriesRegionAndRelocatesHomeSave` (map id `…-editor-ac16d9188c05` vs `…-scene-3e06b23bd921a1ae`)
  - `OpeningUiPlayTests.U6_InteractingWithTheCoreOpensOneDrawerWithTheCoreCardFirst`
  - `InventoryLayoutTests.TheRecipeCardsAreAllTheSameHeight`
  - `PanelPlayTests.TheBuildCardsAreAllTheSameHeight`
- **Diagnosis (agent 9, by reading; none re-run): all five are stale tests, not broken features.**
  - *Movement key closes the drawer:* `ClosedByMovement` is true for one frame; the test waits two (`ControlsCorrectionTests.cs:534-539`). Confirmed by reading.
  - *Recipe cards* and *build cards same height:* stale selectors. The controllers rename the card root to `card-<key>` (`WorkshopPanelController.cs:220`, `BuildPanelController.cs:317`). **Until fixed, nothing checks the equal-height contract.** Confirmed by reading.
  - *U6 core drawer:* the test opens `inventory-panel`, which since U-D-55 opens the Backpack alone, so the core card is never painted. Plausible; needs the running game.
  - *Relocates Home save:* the fixture `slot-phasec-int.json` carries an older map id, so the map refusal fires first. The code behaves as designed; the design is the risk (AUD-PER-03).
- **Steps:** fix the three tests; re-point the U6 test at the real path (press E at the core); regenerate the fixture after AUD-PER-03 is decided.
- **Canonical:** GP-UX-1, GP-UX-6, GP-UX-7, C-ACC. **Authorised?:** Needs go-ahead.

#### AUD-UI-05 — Player text: mojibake, raw keys, two words for one thing
- **Priority:** P2. **Class:** Defect.
- **Now:** "Â·" appears in the hover text of the inserter, splitter, underground belt and conveyor (`Sim/Production/Flow/FlowQueries.cs:186, 192, 201, 203`), spot-checked. Seven sites fall back to a raw key when a display name is missing. "bullets" and "rounds" are both used. A machine with no recipe reads "idle", the same as one between crafts. `[CODE]`
- **Steps:** fix the four strings; a test that scans player text for non-ASCII mojibake and raw keys; pick one word.
- **Canonical:** GP-W6 acceptance ("no identifier leaks"), U-D-53. **Authorised?:** Needs go-ahead.

#### AUD-UI-06 — Day-and-night leftovers
- **Priority:** P3. **Class:** Superseded design still visible.
- **Now:** `UI/Admin/AdminPanel.uxml:52-53` says "Daylight", "Night", "Follow clock". `MouseFlashlightPresenter.cs:50-51, 86` keeps the `nightOnly` test the spec removed. `LightQueries.IsNight` is dead. `Scenes/World.unity` still serialises `nightDarkness: 0.86`; harmless today, but a `FormerlySerializedAs` would revive it. `[CODE]`
- **Canonical:** U-D-58, spec §3. **Authorised?:** Needs go-ahead; the scene edit needs the editor out of Play Mode.

#### AUD-UI-07 — Admin tools vanish in a release build
- **Priority:** P2 decision. **Class:** Unresolved decision (U-D-63 f, implementer's reading).
- **Now:** `Available => Application.isEditor || Debug.isDebugBuild`. No flag or preference overrides it. The sim still accepts `AdminCommand` in any build, so this is a UI gate only. The hidden branch cannot be reached by any test. `[CODE]`
- **Canonical:** U-D-63 (f), C-ADMIN. **Authorised?:** Needs decision (F1-09).

#### AUD-UI-08 — Things built and never looked at
- **Priority:** P1 validation. **Class:** Validation gap.
- **Now:** L-02 rings, tint, blind badge, sweep and guide lines; the enlarged UI size; a clean "repelled" account; GP-UX-7 to 9 ("Nothing in this row has been seen running"). The placement tint has no line-of-sight test, so lit ground behind a building tints as full range; the sweep staggers lamp heads while the ground lights at once. `[CODE]`
- **Canonical:** L-02, L-ACC, GP-UX-7 to 9. **Authorised?:** Safe to check.
- **Fixed 2026-09-23 (REL-116):** the placement tint now tests line of sight. `LightPreview.LitVisible` is `LitWithin` filtered by the turret's own sweep and `Sightline.Clear`, so lit ground behind a building is no longer tinted as full range and the tint stops where the coverage outline does. Two sim tests and one Play Mode capture: [evidence/rel-116](Unity/Docs/evidence/rel-116/README.md). The staggered sweep in this row is fixed too (REL-117, below).
- **Fixed 2026-09-23 (REL-117):** the ground now lights in step with the lamp heads. `Sim/UI/LightSweep.cs` holds the timing — picture only, beside `DarknessLook` — as a front leaving the substation at the speed `CityPresenter` already staggered the heads (30 tiles a second, now one shared constant), with a soft edge; `LightingPresenter` diffs the mask on a `DistrictLitEvent` and holds back **only** the tiles that build newly lit, so nothing already lit flickers. The sim still lights the whole district on one tick — turret sight and alien hesitation are unchanged — and `LightReliefTests.TheSimLightsTheWholeDistrictOnTheTickTheEventFires` holds that rule. Six tests and a slowed three-frame capture: [evidence/rel-117](Unity/Docs/evidence/rel-117/README.md). REL-11 is untouched: a refuel still replays the heads' stagger, but no longer the ground.
- **Fixed 2026-09-23 (REL-118):** the two once-only guide lines (light hesitation, blind turret) are now retired by time **on screen**, not by the wall clock, so a raid's own Danger rows delay the lesson in the overflow instead of swallowing it. Sim code and two tests; the ranking is untouched. This row's other findings are separate issues: REL-119 (the raid account), REL-116 (the tint's missing line of sight) and REL-117 (the sweep) are done; REL-120 is next.

#### AUD-UI-09 — Older open UI findings still open
- **Priority:** P3. **Class:** Defect (recorded 2026-09-14, none closed).
- **Now:** volume sliders drive nothing *(fixed 2026-09-23 by REL-67: each slider now sets its own group’s loudness)*; the tooltip registry leaks; the alerts block sits under an open drawer; badge shapes are illegible at 14 px; the 152 px card height "is arithmetic rather than a measured tallest card". Not re-verified by this audit.
- **Canonical:** TASKS log 2026-09-14, 2026-09-16, 2026-09-21. **Authorised?:** Needs go-ahead.

#### AUD-UI-10 — The worst events have no on-screen alert
- **Priority:** P1. `UI_AND_ONBOARDING.md` §8 ranks base damage and engineer danger above every other alert; with no audio (AUD-ART-01) they are currently signalled by nothing. *(2026-09-23: REL-67’s placeholders now sound `core.disabled`, `engineer.down` and `engineer.up`; the on-screen rows are still missing.)*
- **Class:** Missing approved feature.
- **Now:** per agent 9, `HudViewModel.Intake` (`:347-411`) has no case for core damaged, core disabled, repair aborted, engineer down or engineer up. The text of `CoreRepairAbortedEvent` is never shown and the aborted patch is not refunded. `[CODE]` on the agent's reading; consistent with the assistant's own grep that `CargoDroppedEvent` has no consumer.
- **Outcome:** each of those events raises a row, ranked as §8 says.
- **Files:** `Sim/UI/Hud/HudViewModel.cs`, `Sim/UI/Hud/HudNotices.cs`, tests.
- **Depends on:** AUD-UI-02 (three rows is already too few). Pairs with AUD-INT-01.
- **Accept when:** sim tests post each event and read the row.
- **Canonical:** `UI_AND_ONBOARDING.md` §8, C-05, C-13. **Authorised?:** Needs go-ahead.

---

### D8. Persistence, recovery and reliability

#### AUD-PER-01 — Tests and play sessions write into the owner's saves
- **Priority:** P1. It has destroyed an owner autosave at least once.
- **Class:** Defect (open recorded finding, 2026-09-20; assigned to L-02, which recorded it as not done).
- **Now:** Play Mode and PlayMode tests use the real `…/LocalLow/DefaultCompany/Relight/saves`. Every assistant session today had to back up and restore the folder by hand. A PlayMode run also rewrites `Unity/Docs/evidence/phase-b/b14-r6-drag.json`. `[SEEN]`
- **Outcome:** tests and editor Play sessions use a separate save root.
- **Files:** `Presentation/Persistence/AutosaveController.cs`, `Sim/Persistence/*`, the PlayMode test fixtures.
- **Accept when:** a full PlayMode run leaves the owner's folder and the evidence file byte-identical.
- **Canonical:** TASKS log 2026-09-20; L-02. **Authorised?:** Needs go-ahead.

#### AUD-PER-02 — What the HUD forgets on load
- **Priority:** P3. **Class:** Integration gap.
- **Now:** defence rows (`DefenceAlertSource._raised`), guide-line "already taught" flags and an open raid tally are per session. After a load a standing "3 wrecks" row is gone until state changes, and guide lines teach again. Save schema stays at 9 by design. `[CODE]`
- **Outcome:** standing rows are re-derived on the first refresh after load. No schema change needed.
- **Canonical:** E-17, L-02 §5.6. **Authorised?:** Needs go-ahead.

#### AUD-PER-03 — Any edit to the city scene makes every save unloadable
- **Priority:** P1 product risk. Low cost today (one player), severe the day a tester has a long save.
- **Class:** Design hypothesis, with a confirmed mechanism.
- **Now:** spot-checked. The map id is `baseline + "-scene-" + Fingerprint(...)`, a SHA-256 over width, height, spawn, every tile's kind, patch and solid flag, and every site's id, kind, position, size, item and amount (`World/Authoring/SceneWorld.cs:121, 132-140`). `SaveSerializer.MapProblem` refuses any save whose id differs (`Sim/Persistence/SaveSerializer.cs:290-295`). Moving one building, or changing one ore amount, orphans every save. The failing test `RealWorldCarriesRegionAndRelocatesHomeSave` is this rule firing on an old fixture. `[CODE]` `[AUTO]`
- **Outcome:** an owner ruling on what a save is bound to. Options: keep the strict hash during development; bind to a hand-bumped map version; or load with a relocation and validation pass (the relocation code the test expects already exists).
- **Files:** `World/Authoring/SceneWorld.cs`, `Sim/Persistence/SaveSerializer.cs`, `Presentation/Persistence/AutosaveController.cs:204-217`.
- **Depends on:** the decision (F1-18). Every D-phase authoring task will trip this.
- **Accept when:** the rule is written in `DECISIONS.md` and a test pins it.
- **Canonical:** U-D-SCENE-01, U-D-35, C-10. Newly identified as a risk. **Authorised?:** Needs decision.
- **Built 2026-09-23 (REL-63), the save rule only.** The owner's rule is U-D-69 (b); it is spelled out in `TECHNICAL_ARCHITECTURE.md` §9.1 and pinned by the offline test `SaveMapBindingTests.DuringDevelopmentASceneEditOrphansEverySave`. The strict check is unchanged; the evidence fixture `slot-phasec-int.json` is not regenerated (it is evidence, and `RealWorldCarriesRegionAndRelocatesHomeSave` already asserts today's refusal). The enemy audit's ENM-08 (`SaveRelocate` and live raiders), added to the same Linear issue, is not part of this and stays open.

#### AUD-PER-04 — Mask and route cost on the real runtime
- **Priority:** P2. **Class:** Validation gap.
- **Now:** the only timing test asserts under 50 ms wall-clock for stamping, on .NET 8 offline (18.28 ms recorded). Mono is unmeasured. The per-light stamp cache in the spec was not built. `DefenceAlertSource.Refresh` and `CollectProblems` scan every machine every 150 ms, the latter O(n²). Fine at 50 machines. `[CODE]`
- **Canonical:** spec §7.1, F-07, E-16. **Authorised?:** Measuring is safe.
- **Measured 2026-09-22 (REL-64):** `Unity/Docs/evidence/rel-64/`. Mono (editor, Debug): 240 awake raiders mean 3.9 ms per tick; 360-light mask 38.7 ms mean, 49.8 worst; one cold route field 22–25 ms. Found: REL-121 (turret cost), REL-122 (HUD scan at 219 machines), REL-123 (route cache past 32 fields).

#### AUD-PER-05 — Event autosaves are built and never called
- **Priority:** P2. **Class:** Missing approved feature (`UI_AND_ONBOARDING.md` §2.7; architecture §9.4.2 "before a wave, after a milestone").
- **Now:** spot-checked. `AutosaveScheduler.Trigger` exists and its own comment says "nothing calls it yet" (`Sim/Persistence/AutosaveScheduler.cs:98-110`). Saves are timed only: every 5 minutes into 3 slots, plus `auto-quit.json`. Combined with AUD-INT-12, a bad state reaches every slot in 15 minutes. `[CODE]`
- **Outcome:** an autosave when a raid is warned and when an objective completes; never while the engineer is down or rooted.
- **Files:** `Presentation/Persistence/AutosaveController.cs`, `Sim/Persistence/AutosaveScheduler.cs`.
- **Accept when:** a test shows a save on the raid-warned edge and none during a down or locked state.
- **Canonical:** U-D-35, C-10. **Authorised?:** Needs go-ahead; which events is the owner's call.
- **Built 2026-09-23 (REL-65):** an event save when a raid's warning opens and when a raid ends, the moments TECHNICAL_ARCHITECTURE.md §9.4.2 already records. None while the engineer is down: the save waits until they are up. Each resets the five-minute timer. "Site restored or plant commissioned" has no sim event yet. The timed ring and `auto-quit.json` are unchanged. Sim tests only; no owner play.

#### AUD-PER-06 — Load-time messages go to the log, not the player
- **Priority:** P3. **Class:** Integration gap.
- **Now:** per agent 9, the save-upgrade sentence and the `.bak` recovery sentence are written with `Debug.Log` only. `[CODE]` on the agent's reading.
- **Canonical:** C-10. **Authorised?:** Needs go-ahead.

---

### D9. Art, audio and atmosphere

#### AUD-ART-01 — There is no sound at all
- **Priority:** P1 for a game about hearing things in the dark. **Class:** Missing approved feature.
- **Now:** `AudioCueRouter` is wired; `World.unity:87621` has `cues: []`; no audio files exist. Alerts, the relief cues and approach sounds (U-D-62) are all silent. `[CODE]`
- **Canonical:** D-11, F-04, U-D-33. **Authorised?:** Rows exist; D-11 waits on D-09.
- **Built 2026-09-23 (REL-67, U-D-69 (d), U-P-28):** placeholder sounds made in code now play for every routed cue and interface key when no clip is set; each alien kind in the game has its own approach sound, heard to 48 tiles and panned by where it is; the four volume sliders work and preview their group. Code and automated tests only, plus a Play Mode wiring check by the assistant; **no one has listened to it yet**. The real clip set, the mixer and D-11’s voice limits remain.

#### AUD-ART-02 — Art is procedural and blocked on the owner
- **Priority:** P2. **Class:** Missing approved feature, blocked.
- **Now:** the city renders procedurally; no SVG art, no rail vehicles, frame time unprofiled. F-03 is blocked on U-Q-04. The lamp glow is 1.8 tiles against a lit radius of 7, so the picture understates the lit area; the mask carries the truth. Hard-edged light pools and the diamond dark gap in the court are the owner's open call. `[CODE]`
- **Canonical:** D-09, F-03, U-Q-04, spec §10. **Authorised?:** Blocked on the owner.

---

### D10. Documents and decisions

None of these were fixed. Each is a discrepancy for a later, authorised edit.

| ID | Discrepancy | Where |
|---|---|---|
| AUD-DOC-01 | D-01a and D-02a read `todo` but were delivered 2026-09-14; done rows depend on them | `TASKS.md` Phase D |
| AUD-DOC-02 | E-05 and E-13 read plain `todo` although the siege director and the Breaker shipped | `TASKS.md` Phase E |
| AUD-DOC-03 | GP-UX rows still say Play Mode was unavailable; it has since run, with three of their tests failing | `TASKS.md` UI/UX pass |
| AUD-DOC-04 | F-04's acceptance still says "Ambience changes with day/night"; F-05 still lists U-Q-02 as open | `TASKS.md` Phase F |
| AUD-DOC-05 | F-05a says the "imported `projects` site type"; the sim has no such kind | `TASKS.md` F-05a |
| AUD-DOC-06 | U-P-02 says hand bullets take 20 s; the owner-approved ore opening and the code ship 12 s; `EngineerTuningAsset.cs:39` still defaults to 20 | `DECISIONS.md`, `GAME_DESIGN.md` L121 and others |
| AUD-DOC-07 | U-P-07 (150 s window) and U-P-08 (Breaker "not yet built") are stale; U-P-10 flags itself stale | `DECISIONS.md` |
| AUD-DOC-08 | `GAME_DESIGN.md` never mentions U-D-45 to 57 or U-D-61 to 63; §3 still describes the hand-craft lock; §4.1 says "daylight"; §8.3 describes the Shade; §15.1 calls the ending unresolved | `GAME_DESIGN.md` |
| AUD-DOC-09 | Day-and-night prose survives in `CONTENT_CATALOGUE.md:496`, `TECHNICAL_ARCHITECTURE.md` (six places), `UI_AND_ONBOARDING.md` ("Day 4"), `PHASE_C_HANDOFF.md:357`, `ADMIN_MENU_2026-09-15.md:14` | several |
| AUD-DOC-10 | `CONTENT_CATALOGUE.md` marks as "Implemented and retained" things the port lacks: recruit and Arsenal gates, Shotgun acquisition, artifacts from camps, Overclock ×1.1, cores and plants, Gunsmith hopper, truck, tram | `CONTENT_CATALOGUE.md` §3 to §11 |
| AUD-DOC-11 | `CONTENT_CATALOGUE.md` §3 and §4 still show reference costs and 2:1 smelting; the owner-approved ore opening is 1:1 with cheaper Generator, Foundry and chest | `CONTENT_CATALOGUE.md` |
| AUD-DOC-12 | Four supersessions carry no back-note on the older entry (U-M-30(4), U-M-37(1), U-M-35/40, the 09-14 "collapsible workshop" paragraph) | `DECISIONS.md` |
| AUD-DOC-13 | U-D-12 lists `TURRET_KW = 20` as confirmed; U-P-06 says the agent chose it | `DECISIONS.md` |
| AUD-DOC-14 | The Freight proposal says "GP-W6 is superseded"; U-D-63 records the owner authorising GP-W6 | `FREIGHT_STRONGHOLD_DESIGN_2026-09-18.md` |
| AUD-DOC-15 | Real open questions are not in the U-Q table: radio gating, the D-07 pins, the three default recipes, the Master Switch numbers, the four spec §10 items | `DECISIONS.md` §U-Q |
| AUD-DOC-16 | Work with no table row, only a log line: C-NODEART, C-BUILDTIPS, the first-attack origin fix, the D55 substation, the hint card, streetlight lamps | `TASKS.md` |
| AUD-DOC-17 | The `TASKS.md` top banner stops at 2026-09-15 and still says changes are uncommitted | `TASKS.md` |
| AUD-DOC-18 | Under `GAMEPLAY_AUDIT_WORKFLOW.md` an audit run adds one log line to `TASKS.md`. This audit did **not**, because the owner asked for this file only. The line is owed if the owner wants it | `TASKS.md` §8 |

- **Priority:** P2 as a batch. Stale "Implemented and retained" labels (AUD-DOC-10) are the dangerous ones: they make missing features look done.
- **Class:** Documentation. **Authorised?:** Needs go-ahead; status edits belong to whoever closes the work.

---

### D11. Optional improvements (not required for completion)

Each is a proposal. None is approved. None should be built without the owner's yes.

| ID | Idea | Why it might be worth it |
|---|---|---|
| AUD-OPT-01 | Click a HUD problem or defence row to move the camera to it | `HudProblem.MachineId` already exists and is unused |
| AUD-OPT-02 | A raid history the player can reopen | The account vanishes after 20 s |
| AUD-OPT-03 | The low-fuel row names where coal is | It currently says only "keep it fed by belt" |
| AUD-OPT-04 | Dock numbers explained on the dock, not only in the tooltip | "20" on a belt slot reads as stock; it means "could build 20" |
| AUD-OPT-05 | A `DataValidator` rule that every buildable has at least one runnable recipe | Would have caught the Alien workbench and the orphan recipe |
| AUD-OPT-06 | Index the repository with CodeGraph | The owner's global rules prefer it; indexing is the owner's call |

---

## E. Order of work

Smallest coherent playable game first. Each milestone ends in something a person can play and judge.

### M0 — Stop building on unplayed ground (owner time, no code)
1. **C-ACC**: the full opening, unassisted, including a deliberate death.
2. **L-ACC**: through the first major assault in the dark.
3. Decisions F1-03 (the middle of the game) and F1-09 (Admin in builds).

Why first: 27 rows are waiting on a person. Every fix below is cheaper if M0 finds the problem first.

### M1 — "An honest opening" (small; no new features)
`AUD-INT-12` repair-lock softlock (reproduce first) → `AUD-INT-13` Escape cancels → `AUD-INT-01` death pile → `AUD-UI-10` alerts for core and engineer → `AUD-OPN-07` truthful core-down card → `AUD-INT-02` "× 2" → `AUD-INT-03` per-circuit warnings → `AUD-INT-04` account → `AUD-INT-06` one source → `AUD-UI-05` text → `AUD-OPN-01` truthful last card (reworded) → `AUD-OPN-06` interim (if approved) → `AUD-PER-01` save isolation → `AUD-UI-04` PlayMode baseline to green → `AUD-INT-05` replay test.

Exit: the first 30 to 60 minutes never lie to the player, never delete their things, and the test baseline is green.

### M2 — "A reason to leave, and a reason to come back" (the second act)
F1-18 save-binding rule (before any city authoring) → `AUD-EXP-01` CityValidator → `AUD-INT-09` districts query → `AUD-PER-05` event autosaves → `AUD-EXP-04` first expedition (GP-W7) → `AUD-ECO-03` coal measurement → `AUD-ECO-06` balance (GP-W8) → `AUD-ART-01` first sounds (D-11 subset).

Exit: C-ACC's excursion step is passable; one camp, one reward, one return. This is the first build worth calling a game loop rather than a tutorial.

### M3 — "The city opens"
`AUD-EXP-02` navigation → `AUD-EXP-03` districts and exploration objective → `AUD-CMB-01` full director and one escalation rule → `AUD-EXP-05` tram, truck → `AUD-EXP-07` sites, plants, cores, with `AUD-ECO-01` and E-01 gates → `AUD-EXP-06` map and light board.

Exit: D-ACC.

### M4 — "The fights"
`AUD-CMB-02` roster → `AUD-CMB-03` Freight, then Quarry and Wharf → `AUD-CMB-05` roamers → `AUD-CMB-04` weapons and cells last.

Exit: E-ACC (second plant commissioned, one major raid repelled).

### M5 — "Switch the city on"
`AUD-END-03` sizing check (do this on paper **during M3**, not here) → `AUD-END-02` numbers → `AUD-END-01` groundwork → `AUD-END-05` throw, stages, completion.

Exit: a person finishes the game. F-ACC.

### Running alongside
- `AUD-END-03` and `AUD-END-02` as soon as possible: if the ending cannot be won with three plants, M3's content changes.
- `AUD-DOC-*` whenever a file is open anyway.
- `AUD-PER-04` measurements before M3 adds load.

---

## F. What only the owner can do

### F1. Decisions

| ID | Decision | Blocks |
|---|---|---|
| F1-01 | Master Switch: site, bill of materials, surge size | AUD-END-02, F-05a |
| F1-02 | What do the four factories each make? ("all four factories" has no meaning yet) | AUD-END-02 |
| F1-03 | The Freight stronghold design: adopt, amend or replace? It is the only written middle of the game and has no decision ID. Includes the 10-Concrete repair and the 440-tile core carry. **Decided 2026-09-22 (U-D-69 a):** adopted whole; later decisions win where they clash, and the clashes are listed when GP-W7 starts; built in the fourth batch | AUD-EXP-04, AUD-CMB-03, GP-W7 |
| F1-04 | The final raid's target: is it always Home? | AUD-END-04 |
| F1-05 | Does the light board remove D-07's named pins? | AUD-EXP-06 |
| F1-06 | Radio-gated raid warnings: kept for majors, or retired? The code says "retired"; no decision does | AUD-CMB-01 |
| F1-07 | One escalation rule: U-D-47's +15% per assault, or U-D-37's tiers? **Decided 2026-09-21 (U-D-64 f):** plants pick the tier, assaults climb inside it | AUD-CMB-01, E-16 |
| F1-08 | Default recipes for Refinery, Mixer, Alien workbench | AUD-ECO-04 |
| F1-09 | Admin tools in release builds: hidden, or behind a flag? | AUD-UI-07 |
| F1-10 | Review of U-D-63's six implementer choices (120 s threshold, rank order, account wording, Admin gate) | GP-W6 acceptance |
| F1-11 | Interim: hide machines that cannot work until gates exist? | AUD-OPN-06 |
| F1-12 | Start GP-W7 and GP-W8? **Decided 2026-09-22 (U-D-69 a) for GP-W7:** yes, the whole Freight plan, in the fourth batch. GP-W8 is not answered | M2 |
| F1-13 | U-Q-04: art package and tileset licence | F-03 |
| F1-14 | Spec §10: overlay strength, tram-stop lighting, the court's dark centre, hard-edged light pools | L-ACC |
| F1-15 | C-R01 to C-R07: proposals from another session; accept, fold in or drop | Phase C |
| F1-16 | Should alien sight be blocked by player walls? | AUD-CMB-08 |
| F1-17 | Picking up a wreck and re-placing it repairs it for free: exploit or escape hatch? **Wider since 2026-09-23 (REL-115):** every machine can now be a wreck and keeps its items (U-D-69 f), so a pick-up also gets round the wreck's lock on its contents | AUD-INT-14, REL-115 |
| F1-18 | What is a save bound to? Today any scene edit orphans every save. **Decided 2026-09-22 (U-D-69 b), for development only:** the strict map check stays; a map change may stop old saves loading and the player starts a new game | AUD-PER-03, all Phase D authoring |
| F1-19 | What should a dead Home core cost the player? Today: almost nothing. **Decided 2026-09-21 (U-D-64 d, U-D-66 part 6):** the raid ends, survivors walk off, the player repairs, the next raid waits a full interval | AUD-CMB-07, AUD-OPN-07, the ending's trip rule |
| F1-20 | Which moments deserve an event autosave? **Already recorded (TECHNICAL_ARCHITECTURE.md §9.4.2; reconciled on REL-65 2026-09-21, not a new owner decision):** raid start, raid resolved, site restored or plant commissioned. The first two built 2026-09-23 (REL-65) | AUD-PER-05 |

**Added 2026-09-21, after this audit.** F1-21 to F1-26 came from `ENEMY-THREAT-AUDIT.md` and U-D-64 to U-D-67. F1-27 to F1-35 came from the backlog reconciliation. All are open. Linear REL-71 carries the full text, the evidence and a recommendation for each; none is approved.

| ID | Decision | Blocks |
|---|---|---|
| F1-21 | How do the Quarry's resources reach the tram? It is about 355 tiles from any stop | REL-35, REL-30 |
| F1-22 | How long is a large raid meant to last? Design 2 to 3 minutes; the game does about 6.5 | REL-38, REL-75 |
| F1-23 | One way into Home, or several? | REL-76 |
| F1-24 | Can small raids hit a powered mining outpost? | REL-38 |
| F1-25 | What may aliens do to the tram and the truck? | REL-35 |
| F1-26 | Should raids come from the side of a living stronghold? | REL-40, REL-38 |
| F1-27 | Does the 240 cap count sleeping roamers and garrisons? | REL-42, REL-98 |
| F1-28 | What happens when the first small raid reaches the Home core's floor? **Decided 2026-09-22 (U-D-68 b):** the raid breaks off and the HUD says the core held | part of REL-75, REL-72 |
| F1-29 | When does a stronghold fight begin and end, and may it hold back a large-raid warning without limit? **Decided 2026-09-22 (U-D-69 e):** no other limit; the fight ends about 30 s after the engineer leaves or goes down | part of REL-75, REL-93, REL-96 |
| F1-30 | What counts as a raid that cannot finish, and what outcome is recorded? **Answered in part 2026-09-22 (U-D-68 a):** nothing the player places may wall a raid off; raiders attack it (E-24, built 2026-09-23 in REL-115). The recorded outcome for any other stuck raid is still open; the neutral interim stands | part of REL-44, REL-80 |
| F1-31 | Must a Unity save made today keep loading as systems are added? **Decided 2026-09-22 (U-D-69 b), for development only:** no; a clean break is allowed while the game is unreleased | REL-44, 42, 35, 38, 76, 43, 63 |
| F1-32 | Home coal: numbers, or coal reaches Home sooner? (after the REL-88 measurement) **Decided 2026-09-22 (U-D-69 c):** numbers, a bigger pile or a slower burn. **Built 2026-09-23:** a bigger pile, 2,100 Coal (U-P-27) | REL-27 |
| F1-33 | Roamers before alien roles (U-D-66), or after the roster (L-03's dependency)? | REL-42 |
| F1-34 | Add a standalone Windows build row (F-10 suggested) and try a first build early? | REL-107 |
| F1-35 | The ending's words: what "fully lit on every street tile" means with lamp light, and why a dark route is kept after raids end | REL-50 |
| F1-36 | Two salvage patches are relabelled to ore at boot (iron ore at Riverside, copper ore at Ironworks). U-D-65 says iron ore only near Ironworks and copper ore only at the Quarry. Do they stay ore? (added 2026-09-22 from the FEAS-01 paper) | REL-30, REL-35 |
| F1-37 | Every rubble tile becomes iron ore at 300 units. Is iron meant to be scarce? Measure the rubble-tile count first (added 2026-09-22 from the FEAS-01 paper) | REL-30 |
| F1-38 | Do Breakers still go for the power line? U-D-59 (spec §5.8, “Opportunist”) sends a raid's Breaker up to 6 tiles off its path to break a generator, pole or lamp. U-D-69 (h) says raiders break a machine only when it blocks their path. Is the power-line detour an exception to (h), or dropped? Nothing of §5.8 is built, so nothing changed (added 2026-09-23 from REL-115) | E-14, REL-71 |

### F2. Human playtests owed

No formal verdict is recorded for any of these. That is not the same as "never played": the owner played the Unity opening informally on 2026-09-14 and 2026-09-15, and those sessions produced the correction pass, the UI/UX pass and the gameplay improvement pass recorded in `TASKS.md`. What is missing is a recorded acceptance verdict against each gate's written checks. An agent cannot mark them done. (Wording corrected 2026-09-21; Linear REL-101 to REL-105 and REL-110 to REL-112 own the gates.)

| Gate | What it covers | Can run now? |
|---|---|---|
| B-ACC | Project inspection (deferred by the owner 2026-09-14) | Yes |
| C-ACC | Full opening; six correction-pass retests; U-P-01, 02, 05 | Yes, except the excursion step (needs GP-W7) |
| L-ACC | Always-dark: is dark sight fair, overlay strength, lit streets (U-P-10) | Yes |
| GP-W1, W3 to W6, GP-UX-1 to 9, GP-OPEN-1, GP-PLAYTEST-1, L-01, L-02, E-17, D55 | Owner acceptance of built work | Yes |
| D-ACC, E-ACC, F-08, F-09, F-ACC | Later phases | No |

Things only play can answer: does copper stall new players; does the first major in the dark feel fair; does Home coal last; does wreck repair get tedious (F-08); is automation better than hand-feeding (`GAME_DESIGN.md` §6.4).

---

## G. Checks run and evidence

### G1. Automated checks run on 2026-09-21 against `9d1a0f90`

| Check | Result | Notes |
|---|---|---|
| Unity EditMode (`Relight.Sim.Tests`), via Unity CLI | **766 / 766 passed**, 11.3 s | Run during this audit |
| Offline sim tests (`dotnet test`, scratch project) | **740 passed, 0 failed, 1 skipped** of 741 | Skipped: the long 20-minute replay test |
| Offline UI + PlayMode compile check (`dotnet build`) | **0 errors** | Compile only |
| Unity PlayMode | **78 passed, 5 failed, 5 skipped** of 88 | Run earlier the same day on the identical tree; the five failures are the pre-existing baseline (AUD-UI-04) |
| `git status` after every Unity session | Clean except `.serena/project.yml` | Owner autosaves restored from backup and verified with `diff -rq` |

### G2. Not run

- The reference project's `npm test`, `typecheck`, `lint`, `docsync:check`, `freshness:check`. The reference was not touched.
- Any performance measurement under Mono.
- Any playthrough, assisted or not.
- A standalone Windows build.

### G3. Claims spot-checked against code by the assistant

| Claim | Result |
|---|---|
| Death pile has no UI, presenter or input | Confirmed by grep across the whole `Assets/Relight` tree |
| "× 2" cause | Confirmed, `HudViewModel.cs:381-385` with `HudNotices.cs:69-70` |
| Power warnings sum all circuits | Confirmed, `PowerQueries.cs:62-70` |
| Late-staged minor raid gets no account | Confirmed, `RaidAccountSource.cs:84` with `DirectorPhase.cs:324` |
| Mojibake in belt hover text | Confirmed, `FlowQueries.cs:186-203` |
| Siege tuning asset missing | Confirmed, no `*Siege*.asset` |
| `alien-decode` cannot run | Confirmed, `CatalogueData.g.cs:186`, `ProductionRules.cs:48` |
| Dock count = carried + affordable builds | Confirmed, `GameplayDock.cs:86-90, 125` |
| Minor raids are warned | Confirmed, `DirectorPhase.cs:390` (fun-audit problem 2 is fixed) |
| Death leaves a running repair set; respawn re-locks movement; a paused repair never ends | Confirmed by reading `EngineerMovement.cs:31-50, 80-100`, `HomeCore.cs:189`, `HomeCorePhase.cs:48`. **Not reproduced in the running game** |
| The workshop core card cannot cancel a machine repair | Confirmed, `HomeQueries.cs:41` (`inProgress` is Core-only) |
| "Escape cancels" has no Escape link | Confirmed: `CancelRepairCommand` is sent only by two panel buttons; `EscapeChain.cs` has no repair link |
| The map id is a content hash and a mismatch refuses the save | Confirmed, `SceneWorld.cs:121, 132-140`, `SaveSerializer.cs:290-295` |
| Event autosave hook has no caller | Confirmed; the code's own comment says so (`AutosaveScheduler.cs:100`) |

**Not re-checked (reported on a sub-agent's reading only):** the wreck pick-up repair (INT-14), "attackers nearby" meaning the whole map (CMB-07), the missing HUD cases for core and engineer events (UI-10), W/A/S/D closing the drawer while rooted (INT-13), load messages going to the log (PER-06), and the U6 test diagnosis (UI-04).

### G4. The 2026-09-15 fun audit's ten problems, today

| # | Problem then | Now |
|---|---|---|
| 1 | Major assault never starts | Fixed (raid line; Verify C20 passes) `[AUTO]` |
| 2 | Minor raids arrive unwarned | Fixed (GP-W3) `[CODE]` |
| 3 | Hand-craft lock | Removed (U-D-44) `[CODE]` |
| 4 | Factory has no consumer | Partly: turrets eat bullets; nothing else. GP-W8 open |
| 5 | Opening ends by telling you to stay | **Still true** (AUD-OPN-01) |
| 6 | Melee aliens cannot catch the player | Not re-checked. `[PLAY]` |
| 7 | Generator dies while locked in a craft | Moot (no lock); low-fuel row added, with INT-03's flaw |
| 8 | Guide numbers contradict the HUD | **Still true in one place** (AUD-INT-06) |
| 9 | Turret inside the workshop wall | **Still open** (AUD-INT-11) |
| 10 | Nothing to discover, no story | **Still true** (AUD-EXP-04) |

### G5. Evidence files

- Captures (scratch, not in the repository): `audit-start.png` (New Game, 23 s), `w6-1920.png`, `w6-a.png`, `w6-b.png`, `w6-account.png`.
- Sub-agent notes (scratch): `01-intended-journey.md` to `09-failure-persistence.md`.
- One open disagreement between sub-agents, not resolved here: the world's total iron ore was counted as about 27,700 by one and 22,680 by another. Both agree Home's yard holds 7,680. Neither figure changes a finding.
- In the repository: `Unity/Docs/evidence/` (unchanged by this audit), `Unity/Docs/GAMEPLAY_FUN_AUDIT.md`, `Unity/Docs/GAMEPLAY_AUDIT_WORKFLOW.md`.
