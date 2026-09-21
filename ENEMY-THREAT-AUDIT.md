# Relight — enemy and threat audit

Date: 2026-09-21. Scope: the Unity port at `Unity/Relight/` (commit `9d1a0f90`). Analysis only.

- No gameplay, code, test, `TASKS.md`, `DECISIONS.md` or Linear change was made for this report.
- `Unity/Docs/TASKS.md` still owns task status. `Unity/Docs/DECISIONS.md` still owns decisions. This file is a derived report, like `TASK-REMAINING.md`.
- The reference TypeScript project is described only in §1.10, and is always labelled "reference".
- Every new mechanic in this file is a **proposal**. Nothing here is approved until the owner says so.

## How to read this

**Evidence tags**

| Tag | Meaning |
|---|---|
| `[CODE]` | Read in the Unity source. Not run. |
| `[AUTO]` | Covered by an existing automated test. |
| `[PROBE]` | Measured in a throwaway offline simulation written for this audit (§7.2). Real sim code, test map, not the city. |
| `[MATH]` | Arithmetic on `[CODE]` numbers. Idealised. |
| `[DOC]` | Stated in a design or decision document. |
| `[HYP]` | Suspected. Not reproduced. |
| `[SEEN]` / `[PLAY]` | Seen on screen by the assistant / played by the owner. **Nothing in this audit has either tag.** |

**Finding classes.** The report keeps four kinds of thing apart, as asked:

1. **Confirmed defects** — §2.1
2. **Missing approved functionality** — §2.2
3. **Tuning recommendations** — §6
4. **Optional design proposals** — §6.5 and §8.4

**Words used in this report**

- **Cycle** (or "major cycle") = one full loop from the start of one large raid to the start of the next: assault, recovery, free time, then the next warning. Today a cycle is about 21 minutes. "One small raid per cycle" means one small raid between each pair of large raids. The opening 25 to 30 minutes before the first large raid is not a cycle.
- **District** = the tiles whose nearest substation site is the same one (`ALWAYS_DARK_SPEC.md` §6; the same straight-line rule as `PowerGrid.SubstationOf`, `Sim/Power/PowerNetwork.cs:167`). There are nine. Their real sizes are in §3.3.

Paths are relative to `Unity/Relight/Assets/Relight/` unless they start with `Unity/` or `docs/`.

---

## 0. The short version

- **The world is empty.** The port has no roaming aliens and no camp defenders. The only aliens that ever exist are raiders walking to the Home core. Across a three-hour simulated game the average number of living aliens was **about 2** `[PROBE]`.
- **Raids are the whole threat, and only Home is ever attacked.** There is one major slot and one minor slot.
- **After the first major, the HUD is in a raid state about 80% of the time** (5 min warning, about 6.5 min assault, 5 min recovery, in a cycle of about 21 min). Real free time is about 4 to 5 minutes per cycle `[PROBE]`.
- **Minor raids almost never happen.** They are configured for every 5 to 7 minutes. In three simulated hours only 4 fired, because the major's warning, assault and recovery windows crowd them out `[PROBE]`.
- **Losing the core switches the whole threat system off, for good.** Raiders park at the dead core, the major never ends, and every later raid is skipped. Core repair is blocked while they stand there `[CODE]` `[PROBE]`. This confirms and widens AUD-CMB-07 (REL-44).
- **Turrets are almost never attacked.** An ordinary raider only stops for a weapon within 2 tiles of its path. In every simulated run, zero turrets were lost `[PROBE]`.
- **Defence barely needs the factory.** A maximum-size major (120 bodies) costs 480 rounds. That is under 5 minutes of one Assembler, once every 20 minutes `[MATH]` `[PROBE]`. Coal for lights is the larger running cost.
- **Recommended model: B.** Aliens live in the dark districts and hunt when disturbed; Home is attacked only by announced raids; lighting a district makes it safe for good. This matches `ALWAYS_DARK_SPEC.md` §6 and U-D-16/38, which are already the approved direction. Most of the work is building what is approved, then fixing the raid loop's failure path.

The closing answer to the owner's four questions is in §10.

---

## 1. What actually happens now

### 1.1 The lifecycle, end to end `[CODE]`

| Step | What happens | Where |
|---|---|---|
| Birth | Only the director, the opening encounter and an admin command create bodies. Raiders are born one at a time near an entry tile. | `Sim/Combat/Director/DirectorPhase.cs:239` (major), `:353` (minor), `Threat.cs:155` (opening), `Sim/Admin/AdminCommands.cs:130` (the only `EnemyLayer.Site` birth) |
| March | Raiders follow a distance field to the core at march speed. Lit tiles cost 4 steps instead of 1. | `Sim/Combat/Director/RaidField.cs:128-293` |
| Notice | A raider notices the engineer at 8 tiles (4.8 if the alien stands on a lit tile), then chases greedily. | `CatalogueData.g.cs:246-258`, `EnemyPhase.cs` |
| Fight | It attacks the core, the engineer, or a weapon within 2 tiles. A Breaker stops for any defence within 2 tiles. | `Sim/Combat/Enemies/EnemyPhase.cs:202-217` |
| Death | The body is removed and `EnemyKilledEvent` fires. No loot, no corpse, no saved kill count. | `Enemies.Damage` |
| Leave | Only a retreating body walks back to its origin and is removed there. | `Enemies.Route`, `Leave` |
| Save | Every enemy field, every projectile and the whole director are saved with absolute times. A raid resumes exactly. | `EnemyState.cs`, `DirectorState.cs:221`, `SaveSchema.Version` 9 |

Tick order is Power, Combat, Campaign, then Light. The sim runs at 20 ticks per second.

### 1.2 Enemy types `[CODE]`

| Type | HP | Chase speed | March speed | Damage | Interval | Wind-up | Range | Rounds to kill |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| Skitter | 20 | 5.4 | 2.0 | 5 | 1.0 s | 0.3 s | 1.3 (melee) | 2 |
| Spitter | 50 | 3.9 | 1.44 | 10 | 2.0 s | 0.6 s | 7.0 (ranged) | 5 |
| Breaker | 220 | 2.7 | 1.0 | 15 | 2.0 s | 1.0 s | 1.5 (melee, ×3 on structures) | 22 |

- Skitter and Spitter come from the reference export (`CatalogueData.g.cs:208-211`). The Breaker exists only in code (`Sim/Data/CombatBalance.cs:45-55`), has no asset, and is marked provisional.
- Structure damage is `StructureDps 8` × 1.6; the Breaker multiplies it by 3.
- Spit projectiles fly at 8 tiles/s for 2 s.
- **Every alien is slower than the walking engineer** (6.0 tiles/s, sprint 9.6 for about 3 s). The engineer can always kite, but only gains 0.6 tiles/s on a Skitter, so breaking a 20-tile chase takes about 30 s and 180 tiles of running `[MATH]`.
- Stalker and Howler are decided in U-D-37 and have no code (AUD-CMB-02, REL-39).

### 1.3 Detection, pursuit and disengagement `[CODE]`

| Rule | Value |
|---|---|
| Notice the engineer | 8 tiles; ×0.6 when the alien is uncommitted and on a lit tile |
| Hesitate at a light edge | 0.65 s, uncommitted bodies only |
| Squad alert | bodies within 6 tiles of an engaged body also engage |
| Give up the chase | 20 tiles (raider), 18 (camp resident); memory 6 s |
| Camp resident leash | chase out to 36 tiles, re-engage inside 22 (`SiegeTuning.Fallback`) |
| Sleep | an idle camp resident more than 60 tiles from the engineer is skipped (`EnemyPhase.cs:488`). This is the only distance culling. |
| Walls | block light and turret sight, **not** alien sight (AUD-CMB-08, REL-45) |

### 1.4 Spawning rules `[CODE]`

- **Where:** an entry tile must be 8 to 76 path steps from the core and at or below the raid-line row (`DirectorRules.cs:75-81`). Origins prefer about 24 steps (minor) or 42 steps (major).
- **Safe distance:** at least 28 tiles from the engineer, and not within 1 tile of another body. Births are a bounded search of at most 100 nodes within 6 tiles of the origin.
- **Rate:** one body per 4 s per approach (`SpawnEveryS`).
- **Not checked:** whether the tile is on screen, and whether the tile is lit. Light only changes which entry scores best. The screen shows about 10 tiles up and down and 17.8 left and right, so 28 tiles is always off screen today. There is no zoom control.
- **Observed:** the smallest spawn distance from the engineer in any run was 28.5 tiles `[PROBE]`. The rule holds.
- **On the real city:** Founders Court has **one exit**, a 10-tile mouth at row 426, 71 steps from the core. Every raid is born at about (67,432). `MajorSectors` is 4, but the map offers 1 approach (`Unity/Docs/FOUNDERS-COURT-SPAWN-FIX.md`, measured offline for this audit).

### 1.5 The raid director `[CODE]`

| Item | Configured value | Source |
|---|---|---|
| First major | 25 to 30 min (`FirstMinS 1500` + 0..300) | `CatalogueData.g.cs:246` |
| Later majors | 18 to 22 min, **start to start** (`IntervalMinS 1080` + 0..240) | same |
| Major warning | 300 s | same |
| Major roster | 60 = 40 Skitters + 20 Spitters | same |
| Major shape | 4 waves of 9 / 12 / 18 / 21, starts 110 s apart, 40 s walk-on, 30 s tail = 400 s | `Sim/Data/SiegeTuning.cs:102-109` |
| Growth | +15% of the base roster per **completed** major, capped at ×2 (120). Formula: `min(2.0, 1 + 0.15 × History.Count)` | `SiegePlan.cs:41-42` |
| Breakers | from the 2nd major, from wave 3, 5% of the roster taken from Skitters | `SiegeTuning.Fallback` |
| Recovery | 300 s after a major ends | `RecoveryS` |
| First minor | no earlier than 13 min (`GraceS 780`) | `DirectorPhase.cs:74` |
| Minor timer | every 300 + 0..120 s. The slot is **dropped, not deferred**, if a major exists, a minor exists, the director is reserved, or recovery is running | `DirectorPhase.cs:383-388` |
| Minor size | 6 to 10 bodies. Never scales. | `MinorCountBase 6`, `MinorCountRange 4` |
| Minor warning | 30 to 45 s | `SiegeTuning.Fallback` |
| Caps | 240 living bodies world-wide; 48 living major bodies | `LivingBudget`, `ActiveRaidBudget` |
| Target | the Home core only. There is no site, plant or factory targeting. | `DirectorRules.Target`, `EnemyCoreHook.Rect` |
| Debt | none. A skipped opportunity is gone. | `DirectorPhase.Future` |

- **What ends a major:** `Committed && Remaining == 0 && GroupAlive == 0` (`DirectorPhase.cs:22-58`). There is **no timeout** and no retreat when the fight is lost.
- **Survivors:** they never leave. The design says survivors are not despawned (U-D-38). The code goes further: they also never go home.
- **Overlap:** a live minor is told to retreat when a major commits (`DirectorPhase.cs:179-182`). Minors are blocked while any major object exists, which includes its 300 s warning.
- **Scripted encounters:** the opening encounter reserves the director, and minors wait for the opening to end. The opening is 5 Skitters, 25 s warning, withdraws at 300 s and is purged at 600 s. It is the only caller of `Director.Withdraw`.
- **The engineer's situation is never consulted.** The director does not know if the player is exploring, travelling, rebuilding or mid-fight. The only player-aware rule is the 28-tile birth distance.
- **`SiegeTuning` has no asset.** Every siege number comes from `SiegeTuning.Fallback` (AUD-CMB-06, REL-43). `Tuning - Raids.asset` is stale: it lacks `litNoticeMul` and `litStepCost`, which silently take code defaults.

### 1.6 Counts: configured limits against observed behaviour

| Count | Configured | Observed `[PROBE]` |
|---|---:|---|
| Living bodies, world | cap 240 | peak 38 to 41 with a working defence; mean 1.4 to 2.1 over 3 h |
| Living major bodies | cap 48 | reached only with no defence, where it then sits at 48 forever |
| Bodies per major | 60 growing to 120 | 60, 69, 78, 87, 96, 105, 114, 120 (cap hit at the 8th major, about 2 h 53 min) |
| Bodies per minor | 6 to 10 | 6, 7, 9, 9, 10 |
| Minors in 3 h | about 26 to 36 if "every 5 to 7 min" held | **4** |
| Majors in 3 h | 8 | 8 |
| Active pursuers of the engineer | no cap exists | not measured (the probe parked the engineer) |
| Ambient / per-district population | none exists | 0 |
| Camp defenders | none exist (20 / 22 / 20 are data only) | 0 |

The 240 and 48 caps are never the binding limit in ordinary play today. The binding limits are the roster and the wave plan.

### 1.7 How the populations interact `[CODE]`

- **Wanderers:** do not exist. `ALWAYS_DARK_SPEC.md` §6 defines them (approved direction, provisional numbers); L-03 is unbuilt and blocked behind E-13 and E-05.
- **Site defenders:** the behaviour exists (patrol radius, leash, sleep beyond 60 tiles) but nothing populates a camp. The "Prepare to scout" objective sends the player to an empty camp (AUD-OPN-02, REL-20).
- **Scripted encounters:** the opening only. It reserves the director, so no raid overlaps it.
- **Raids:** the only live population.
- So today there is **no interaction to describe**, because only one of the four populations exists.

### 1.8 Off-screen simulation `[CODE]`

- Every alien is fully ticked every tick, anywhere on the map, except sleeping camp residents.
- Loops that grow with enemy count: a marching yield check that is enemies × enemies (`EnemyPhase.cs:402-410`), a squad alert that is enemies × enemies (`:521-529`), a structure scan that is enemies × all machines (`:202-217`), and turret targeting that is turrets × enemies with a sight test per candidate (`EnemyQueries.cs:81-100`). `EnemyState.Find` is a linear scan.
- At the 240 cap the yield check alone is 57,600 pair tests per tick `[MATH]`. At 48 raiders it is 2,304.
- `EnemyPresenter` draws three renderers per body every frame, map-wide, with no culling `[CODE]`.
- **No Unity or Mono timing with enemies exists.** The reference measured 127 actors at a 2.4 ms median tick in JavaScript. See AUD-PER-04 (REL-64).

### 1.9 What guards against unfair or broken situations

| Risk | Guard today | Gap |
|---|---|---|
| Spawn on top of the player | 28-tile rule, held in every run `[PROBE]` | none found |
| Spawn in view or in light | none | harmless today (28 tiles is off screen); matters once roamers exist |
| Accumulation | 240 and 48 caps; no debt | **survivors never leave and a major never times out** (§2.1 ENM-01) |
| Repeated attacks | start-to-start interval, 300 s recovery, minors blocked around majors | free time shrinks to about 4 min; the interval is not measured from the end of the last assault |
| Unreachable target | a breach field treats the player's defences as chewable | a body walled in by **authored** solids accumulates `Stuck` and waits; nothing acts on `Stuck` for raiders (`EnemyPhase.cs:116, 391, 401, 407`) |
| Dead target | none on the authored city | §2.1 ENM-02 |

### 1.10 Reference project behaviour (kept separate)

This section describes the paused TypeScript reference only. It is evidence, not a requirement.

- The reference has three threat layers; only the GP layer is current. It has **no ambient roamers** either.
- Camps hold 20, 22 and 20 bodies. Only the West passage camp reoccupies, after 900 s, and only when all guards are dead, the reward is collected, the engineer is more than 48 tiles away, the camp is unwatched and no machine stands within 20 tiles.
- The Freight stronghold has 60 defenders plus a 600 HP guardian.
- Raid size never scales: 60 per major, 6 to 10 per minor.
- Reference raiders notice the player at 1.2 tiles. The port uses 8.
- When the reference core reaches 0 HP, the raid **retreats and a recovery begins**. The port does not do this on the authored city (§2.1 ENM-02).
- The reference legacy layer removed a body stuck for 30 s. Its campaign layer did not, and neither does the port.
- The reference's controlled major used 95 turret rounds and 85 rifle rounds, saw at most 8 attackers alive at once, and kept the core at 300.
- `docs/CAMPAIGN_RULES.md` still prints the old day and night numbers and should not be read as current.

---

## 2. Defects and integration gaps

### 2.1 Confirmed defects

IDs starting `ENM-` are new in this report. They are **not** Linear issues. Each names the existing issue it belongs with.

| ID | Defect | Evidence | Belongs with | Priority |
|---|---|---|---|---|
| ENM-01 | **A major that cannot finish freezes the director for good.** A major ends only when every body is dead. There is no timeout and no retreat. While it stays open, each new major is skipped ("Previous assault cleanup is still unresolved") and minors are blocked. In four runs the director skipped every raid for the rest of the game, up to 7 skips over 2.5 h, with 40 to 48 bodies parked and `Stuck` up to 9,063 s. | `[CODE]` `DirectorPhase.cs:22-58`; `[PROBE]` runs A, A11, D, F | AUD-CMB-07 (REL-44) | **P0** |
| ENM-02 | **A dead core is still a valid target on the authored city.** `EnemyCoreHook.Rect` falls back to the authored `Sites.Core` site when `Home.Hp <= 0`, so `DirectorRules.Target` stays true and raiders never retreat. The reference retreats here. | `[CODE]` `EnemyCoreHook`; `[PROBE]` (the test map has a Core site too) | REL-44 | **P0** |
| ENM-03 | **Core repair is blocked by any raider anywhere on the map.** `Home.AttackersNearby` is `DirectorQueries.Live` major > 0 or minor > 0. With ENM-01 that means the core can never be repaired once it falls, unless the player hunts down every parked body by hand. | `[CODE]` `Combat/Turrets/HomeCore.Turrets.cs:33` | REL-44 | **P0** |
| ENM-04 | **An undefended core falls to the first minor.** 6 to 7 bodies took 300 HP to 0 in about 77 s, 15 minutes into the game. Combined with ENM-01 to 03, a slow new player can lose the core to a minor and never see another raid. | `[PROBE]` runs A, A11, F | REL-44, AUD-OPN-07 (REL-72) | P1 |
| ENM-05 | **`RaidRecord.Defeated` is dead and misleading.** It copies `Retreat`, which a major only gets with no target. It read `False` for every major in every run, including ones the defence won cleanly. Nothing reads it. | `[CODE]` `DirectorPhase.cs:229`; `[PROBE]` | AUD-INT-04 (REL-8) | P2 |
| ENM-06 | **The raid warning text goes stale.** The urgent line shows the frozen notice: a minor reads "inbound … in 37 s" for 45 s after it arrives, and a major reads "in 300 s" through wave 1. `Warning()` checks the minor first, so a live minor hides an announced major. | `[CODE]` `DirectorQueries.cs:48-77`, read by a research agent and spot-checked | AUD-INT-08 (REL-12) | P1 |
| ENM-07 | **Player shots see no targets for one tick after a load.** The target seam is transient, only `EnemyPhase` re-points it, and the weapon phase runs first. `EnemyTests` sets the seam by hand, which hides this. One tick is 50 ms, so the effect is tiny. | `[CODE]` `EnemyPhase.cs:33-34`, `AutosaveController.cs:222-223` | AUD-PER-02 | P3 |
| ENM-08 | **`SaveRelocate` and raiders.** It removes off-map raiders without adjusting the director's counts, never moves `st.Drops`, and drops off-map entries from `Major.Origins`, so waves can arrive from sides that were not announced. | `[CODE]` `Sim/Persistence/SaveRelocate.cs` | AUD-PER-03 | P2 |
| ENM-09 | **Tuning left out of the data hash.** `GameDataHash` omits `Siege` and `Defence`, so changing those numbers never raises the "different balance data" warning on load. | `[CODE]` `Sim/Persistence/GameDataHash.cs` | AUD-CMB-06 (REL-43) | P2 |
| ENM-10 | **`debugAllowed` is saved and never cleared.** A save made after using the debug raid menu keeps debug raids enabled in every autosave made from it. | `[CODE]` `Editor/Debug/DebugRaidMenu.cs:74` | AUD-UI-07 | P3 |

**Note on my own earlier work.** ENM-01 to 03 sit under the GP-W6 failure-readability pass (U-D-63) that I built. That pass made the fallen core readable on the HUD. It did not check that the game can recover from a fallen core, and it cannot.

### 2.2 Missing approved functionality

These are already decided or specified, and have no code. None is a new idea.

| Gap | Authority | Existing issue |
|---|---|---|
| Roaming population in dark districts (3 to 6 per district, refill 1 per 120 s) | `ALWAYS_DARK_SPEC.md` §6, stage L-03 | AUD-CMB-05 (REL-42) |
| Camp defenders and the first camp | U-D-37, GP-W7, D-08 | AUD-EXP-04 (REL-34), AUD-OPN-02 (REL-20) |
| Stalker and Howler; the Breaker's power-line job | U-D-37, spec §5.8 | AUD-CMB-02 (REL-39) |
| The full director: later tiers, other targets, one escalation rule | U-D-16, U-D-37, U-D-38, U-D-47 | AUD-CMB-01 (REL-38) |
| Strongholds | U-D-62 (design), Freight proposal | AUD-CMB-03 (REL-40) |
| The final-tier raid at the Master Switch | U-D-60 | AUD-END-03 to 05 (REL-48 to REL-50) |
| The siege tuning asset | B-05 data pipeline | AUD-CMB-06 (REL-43) |
| Autosave before a raid (`Trigger` exists, nothing calls it) | | AUD-PER-05 (REL-65) |

### 2.3 Integration gaps (things that work alone and do not join up)

- **Turrets are effectively never attacked.** `NearbyStructure` only returns a weapon within 2 tiles of the raider (`EnemyPhase.cs:202-217`). Across about 70 turret-against-major encounters in the probe, **zero turrets were lost**, lit or dark, even with turrets dry. The `SiegeTuning` comment says "four spitters on one turret … kills it in 5 s"; nothing makes four Spitters choose a turret. So repair demand is the core only, and "losing a key defence" cannot happen through enemy action unless a Breaker walks within 2 tiles of it. `[CODE]` `[PROBE]`. Belongs with REL-38 and REL-45.
- **The Spitter and dark-turret rule has less bite than the documents say.** U-D-59 gives a gun turret 6 tiles of sight in the dark against a Spitter's 7. Since Spitters shoot the core and not the turret, the effect is that an unlit turret cannot answer a Spitter shelling the core from 7 tiles. In the probe, 4 dark turrets and 4 lit turrets gave almost the same result. The U-D-47 ammunition sums were not redone after U-D-59. `[PROBE]` `[DOC]`. Teaching gap: AUD-OPN-04 (REL-22).
- **The HUD tells the player little.** There is no threat level, body count, wave index or composition. `DirectorQueries.Live` and `RaidWarning.At` have no consumer, so there is no map marker or arrow. The after-raid account ignores the engineer going down. `StructureDestroyedEvent` has no HUD consumer. There is no audio cue when a raid arrives (the whole game is silent, AUD-ART-01). `[CODE]`. Belongs with AUD-INT-04 (REL-8), AUD-UI-10 (REL-60).
- **In the dark all three alien types look identical** (`EnemyPresenter`). A Breaker cannot be told from a Skitter until it is lit. `[CODE]`. Belongs with AUD-ART-02.
- **Aliens drop nothing, and the Alien branch needs artifacts.** That is correct for roamers (spec §6). It means camps must be the artifact source, as AUD-ECO-01 (REL-25) already says.
- **Tests run on a flat 160×160 map with one core.** Nothing automated covers the real city, more than one major cycle, the 240 and 48 caps, a major overlapping a minor, a save mid-major, a target that becomes unreachable, or relocation with enemies present. `[CODE]`
- **Debug tools cannot force a major, move the director clock, set the assault number or show director state.** That makes every runtime check in §7 slow. `[CODE]`

### 2.4 Document contradictions that affect tuning `[DOC]`

1. The design wants up to four approaches. Founders Court has one mouth.
2. The design says an assault lasts 2 to 3 min and `WindowS` is 150. The built plan runs 400 s.
3. U-D-47's text says Spitters do not outrange turrets. U-D-59 makes them outrange unlit turrets. U-D-59 governs.
4. U-D-47 says escalation follows "assaults survived". The code counts assaults **completed**, including ones where the core fell.
5. Three escalation drivers are on the table: U-D-47, the U-D-37 tiers (90 to 140, then 160 to 220), and the Freight proposal's "camps alive". AUD-CMB-01 already flags this.
6. The guard leash is 12 in the exported `RaidTuning` and 36 / 22 in `SiegeTuning` (U-D-48e). The port uses 36 / 22.
7. The fun audit proposes that minors scale with machine count. U-D-47 forbids exactly that.

---

## 3. How populated should the world be?

The owner asked for separate answers, not one global count. These are assessments. Numbers are provisional.

### 3.1 The inputs that set the scale

| Input | Value | Tag |
|---|---|---|
| Map | 864 × 576 tiles; 345,449 walkable (69%), one connected area | measured offline |
| Districts | 9 (nearest substation); about 38,000 walkable tiles each on average | `[DOC]` `[MATH]` |
| Screen | about 20 × 35.6 tiles, so about 712 tiles | `[CODE]` |
| Engineer | walks 6 tiles/s. No truck or tram exists yet. | `[CODE]` |
| Walk from Home | mouth 12 s; Camp 1 44 s; Riverside yard 50 s; Ironworks 101 s; Civic 121 s; farthest site 160 s | `[MATH]` |
| Alien notice | 8 tiles, so a walking engineer "sweeps" a 16-tile-wide strip: 96 tiles² per second | `[MATH]` |
| Rifle | 10 damage, 2.5 shots/s, 10-round magazine, 1.5 s reload: about 19 dps perfect. **I assume about 12 dps in real hands** (misses, movement, reloads at bad moments). | `[CODE]`, assumption |
| Engineer | 100 HP, regenerates 5 HP/s after 5 s. Death costs 10 s and drops all cargo. | `[CODE]` |
| Performance | 240-body cap; unmeasured on Mono | `[CODE]` |

### 3.2 Ambient population (roamers)

- Spec §6 already fixes the shape: a **fixed number per dark district**, 3 to 6, never more, never joining raids, ignoring structures, dropping nothing.
- World total at 3 to 6 per district: **27 to 54**, falling as districts are lit. Home's district should hold none once its streetlights and substation are on.
- That is one roamer per 6,400 to 12,800 walkable tiles, or one per 9 to 18 screens if spread evenly `[MATH]`.
- Roamers should keep to **streets**, because that is where the player travels. Streets are 47% of walkable ground, which doubles the meeting rate for the same head count.
- **Assessment: 3 to 6 per district is the right order of magnitude.** It gives "something is out there" without a fight on every screen. It costs at most 54 of the 240-body budget.

### 3.3 Per-district population

- Do not make every district equal. Recommended pattern (provisional):
  - Home district: 0 once lit. Before that, 0 to 2, never inside the always-lit Home lot.
  - Districts next to Home (Westridge, Riverside approach): 3.
  - Middle districts (Old Town, Ironworks, Civic): 4 to 5.
  - Far and stronghold districts (Northwood Freight, Ravenholm Quarry, East Wharf): 6.
- This makes distance from Home read as danger, with no timer involved.

**How big a district really is** (measured offline from the scene for this audit, using the nearest-substation rule; not checked in Unity)

| District | Substation | Walkable tiles | Street tiles | Roughly across | Walk across | Screens |
|---|---|---:|---:|---:|---:|---:|
| Founders Court (Home) | 0 (93,350) | 37,808 | 15,206 | 194 tiles | 32 s | 53 |
| Riverside Works | 1 (304,424) | 47,325 | 23,505 | 218 | 36 s | 66 |
| Ironworks | 2 (416,94) | 62,372 | 28,516 | 250 | 42 s | 88 |
| Civic Utility | 3 (720,420) | 51,733 | 26,598 | 227 | 38 s | 73 |
| Westridge Homes | 4 (115,176) | 23,594 | 12,436 | 154 | 26 s | 33 |
| Old Town | 5 (278,178) | 31,700 | 16,687 | 178 | 30 s | 45 |
| Northwood Freight | 6 (98,59) | 21,845 | 9,018 | 148 | 25 s | 31 |
| Ravenholm Quarry | 7 (833,91) | 50,100 | 23,563 | 224 | 37 s | 70 |
| East Wharf | 8 (834,408) | 18,972 | 8,740 | 138 | 23 s | 27 |

- "Roughly across" is the square root of the walkable area. "Screens" is walkable tiles ÷ 712. Walk time is at 6 tiles/s.
- **Districts differ in size by more than 3×** (18,972 to 62,372 walkable tiles). The average is 38,383.
- The borders are straight lines between substations. They cut through city blocks and ignore streets, walls and the river. Nothing in the game shows them.
- **Consequence for roamers:** a flat "3 to 6 per district" gives very uneven crowding. 6 in East Wharf is one alien per 3,160 tiles. 4 in Ironworks is one per 15,600. That is a 5× difference in how often you meet them.
- **Refinement (proposal):** set each district's number from its **street area**, about one roamer per 5,000 street tiles, then raise it with distance from Home. At one per 5,000 that gives Founders 3, Riverside 5, Ironworks 6, Civic 5, Westridge 2, Old Town 3, Northwood 2, Ravenholm 5, East Wharf 2: 33 in all, inside the spec's 27 to 54.
- A district border drawn in the editor would make all of this checkable by eye. That is a small editor-only tool and is **not built** (§8.4).

### 3.4 Enemies met per trip `[MATH]`

Assumes roamers on streets, singles, 8-tile notice, engineer walking. Idealised.

| Trip | Walk time | Expected roamers noticed, at 3 per district | At 6 per district |
|---|---:|---:|---:|
| Home to Riverside yard | 50 s | about 0.6 | about 1.1 |
| Home to Ironworks yard | 101 s | about 1.1 | about 2.3 |
| Home to Civic yard | 121 s | about 1.4 | about 2.7 |
| Home to the farthest site | 160 s | about 1.8 | about 3.6 |

- A lone Skitter dies to 2 rifle rounds in under a second. **Single roamers are not an encounter.** They are atmosphere.
- So the same head count should arrive as **small packs**. See §3.5.
- A truck (×3 speed) cuts every figure above by two thirds, which is the right reward for building it.

### 3.5 Group size and spacing

- **Recommended: packs of 2 to 3 early, 3 to 4 late, with at most 1 Spitter early and 2 late.**
- A pack of 3 Skitters is 60 HP: about 5 s at the assumed 12 dps. A pack of 2 Skitters and 1 Spitter is 90 HP: about 8 s. Both are over before 100 HP of engineer is at real risk, if the player fights. Both punish standing still.
- Packs should sit at least **40 tiles apart** (more than two screens, and well beyond the 6-tile squad alert), so one fight does not chain into the next.
- With packs, a 50 s trip meets a pack about one time in three. A 2-minute trip meets about one. That reads as "the dark is dangerous", not "I am always fighting".

### 3.6 Active pursuers

- There is no cap today, and none is needed today because nothing roams.
- Once roamers and camps exist, the squad alert (6 tiles) can chain a whole camp of 20 onto the engineer.
- **Recommended soft cap on bodies actively chasing the engineer: 4 early, 6 to 8 mid, 10 to 12 late.** Bodies over the cap hold position and stay alert; they are not deleted.
- Basis: at 12 dps the player clears 6 Skitters in about 10 s of kiting and 12 in about 20 s. Four Spitters landing every shot deal 20 dps and kill the engineer in 5 s, so Spitters in any pursuing group should stay at 2 or fewer until better weapons exist (AUD-CMB-04, REL-41, is deliberately last).

### 3.7 Defenders at progression locations

| Location | Reference | Recommended provisional | Why |
|---|---:|---:|---|
| First camp (West passage) | 20 | **10 to 12** | It is the first rifle-only fight and the GP-W7 teaching trip (REL-34). 20 bodies is about 580 HP, 60 to 90 s of real fighting. |
| Second and third camps | 22, 20 | 20 to 22 | As the reference. By then the player has turrets to carry and a supply line. |
| Stronghold | 60 + guardian | 40 to 60 + guardian, behind the soft pursuer cap | U-D-62 is design only and the Freight design is a proposal (F1-03). Not sized here. |

- Camps count toward the 240 cap: about 62 for three camps, 60 for a stronghold.
- **Budget check** `[MATH]`: 54 roamers + 62 camp + 60 stronghold + 48 raid = 224, under 240. It fits, with little room. A second stronghold alive at the same time would not fit. Sleeping bodies (beyond 60 tiles) keep the tick cost down but still count.

### 3.8 Replenishment: three options compared

| Option | What it means | For | Against |
|---|---|---|---|
| **Lasting safety** | Cleared means cleared, for ever | Progress is visible and permanent; rewards lighting | A cleared map becomes a walking simulator; travel loses all tension |
| **Breathing room** | Cleared stays clear for a fixed time, then refills at once | Simple; predictable | Feels like a respawn timer; invites farming if anything drops |
| **Gradual repopulation** | Refill one at a time, slowly, only in the dark, only out of sight | Tension returns softly; never a sudden ambush; light is the permanent cure | Needs spawn rules that respect light, sight and distance |

**Recommended mix**, which is what spec §6 already implies:

- **Lit district = lasting safety.** No roamers, ever, while it stays lit. This is the core reward loop of the game.
- **Dark district = gradual repopulation**, 1 per 120 s up to the district's fixed number, only on unlit, unseen tiles at least 20 tiles from the engineer (spec §6). A district cleared of 5 roamers is quiet for 2 minutes and back to full in 10.
- **Camps = lasting safety once cleared and looted.** At most one designated camp reoccupies, under the reference's strict conditions (§1.10).
- **Open question the documents do not answer:** what happens when a lit district goes dark again (brownout, Breaker on the power line). Proposal: repopulation starts only after 300 s of continuous darkness, at the normal refill rate. This is a proposal (§9, decision 6).

---

## 4. Small and large raids

### 4.1 What a cycle looks like today `[PROBE]`

One seed, defended base, from the director's own timestamps:

```
0:00 ─────────── quiet (13 min grace) ─────────── 13:00
13:42 minor (7)      19:41 minor (9)
23:32 MAJOR warned ── 28:32 assault starts ── ~35:00 over ── 40:00 recovery ends
40:00 ── free ── 44:53 next MAJOR warned ...            (cycle ≈ 21 min)
```

- Per cycle: 5 min warning + about 6.5 min assault + 5 min recovery + **about 4 to 5 min free**.
- A minor can only land in the free window, and only if its own independent timer happens to fire there. It did so in 2 of 7 windows.
- The warning minutes are useful build time. But the HUD is counting down, so they do not feel like rest.

### 4.2 Small raids

| Aspect | Now | Assessment |
|---|---|---|
| Frequency | configured 5 to 7 min; **observed about 1 per 45 min** after the first major | The configured number is fiction. The slot is dropped instead of deferred. |
| Warning | 30 to 45 s | Enough only if the player is near Home. From the Ironworks yard the walk home is 101 s. That is acceptable **if** a modest automated defence beats a minor unattended, which is the point of building one. |
| Size | 6 to 10, never scales | Right for early. Meaningless by mid game. The reference's deferred tiers were 10 to 18 and 16 to 26. |
| Composition | Skitters and 2 to 4 Spitters | Fine. |
| Danger | 6 to 7 bodies kill an **undefended** core in 77 s. 4 dark turrets took 32 core damage from a 9-body minor; 8 or more took none. | A minor is a real check on "did you build anything", which is good. It must not be able to end the campaign (ENM-01 to 04). |
| Duration | about 20 to 25 s of fighting once it arrives | Fine. |

### 4.3 Large raids

| Aspect | Now | Assessment |
|---|---|---|
| Frequency | 25 to 30 min, then 18 to 22 min **start to start** | As rosters grow, the assault eats the interval. Schedule from the **end** of the last one. |
| Warning | 300 s | Good. It exceeds the longest walk home (about 172 s corner to corner), so a major can always be attended. |
| Duration | 385 to 428 s | Twice the design's "2 to 3 min" (contradiction 2). 6 to 7 min is reasonable for a set piece; decide it on purpose. |
| Total against simultaneous | 60 to 120 total; **peak alive 38 to 41** | The wave plan, not the 48 cap, sets the peak. The last wave (35% of the roster in 40 s) is the real test. |
| Composition | 2:1 Skitter to Spitter; Breakers 5% from the 2nd major | Reasonable. Breakers matter little while nothing stands within 2 tiles of their path. |
| Wave spacing | 9 / 12 / 18 / 21, 110 s apart | Good shape: readable escalation with gaps to reload. |
| Approach directions | 1 → 2 → 3 → 4 on an open map; **always 1 on the real city** | The biggest unknown in this audit. One 10-tile mouth makes a single kill zone. Every open-map result below is likely **harder** than the city in coverage and **easier** in concentration. Needs a runtime check. |
| Targets | Home core only | Correct for now (U-D-38: first target is always Home). Plants and sites have no targeting and no code. |
| Scaling | +15% per completed major to ×2, reached at about 2 h 53 min. Then flat for ever. | Bounded and not circular, which is good. But it is a pure clock: nothing the player does changes it. |
| Player state | not considered | A major can land mid-expedition. The 300 s warning covers that. A minor cannot be attended from far away, by design. |
| Overlap | major cancels a live minor; minors blocked during warning, assault and recovery | Sound rules. Side effect: minors vanish (§4.2). |
| What ends it | all bodies dead. Nothing else. | **Defect** (ENM-01). |
| Survivors | stay for ever | **Defect** when the defence loses. |
| Recovery after failure | none. The core cannot be repaired while survivors exist. | **Defect** (ENM-03). |

### 4.4 Defensive demand

**From the real numbers** `[CODE]` `[MATH]`: a gun turret fires 1 round/s for 10 damage and never misses once aimed. A Skitter costs 2 rounds, a Spitter 5, a Breaker 22.

| Major | Bodies | Rounds (exact, no overkill) | Steel + copper | One Assembler's time (100 rounds/min) |
|---|---:|---:|---|---:|
| 1st | 60 | 180 | 36 + 18 | 1.8 min |
| 2nd | 69 (3 Breakers) | 267 | 54 + 27 | 2.7 min |
| 4th | 87 | 341 | 69 + 35 | 3.4 min |
| 8th and after | 120 | 480 | 96 + 48 | 4.8 min |
| **All 8 majors + 4 minors, 3 h** | 764 | **2,953** | 591 + 296 | about 30 min |

- The probe's round counts matched this arithmetic exactly (180, 267, 314 … 480) `[PROBE]`.
- **Average demand over three hours is about 16 rounds per minute.** One Assembler makes 100. A supply of 25 rounds/min gave exactly the same outcome as endless ammunition `[PROBE]`, because hoppers (50 rounds each) refill between raids.
- **Volume is not the constraint. Distribution is.** With 8 turrets holding 50 rounds each and no resupply, the turrets facing the approach ran dry during the **first** major while others stayed full, and the core dropped from 300 to 172. The base fell in the second major `[PROBE]`.
- **Turrets needed on an open map, no repairs, endless ammunition** `[PROBE]`:

| Defence | 1st major | 2nd major | Core falls |
|---|---|---|---|
| none | | | 15 min (to a minor) |
| 4 turrets, dark | core 268 → 236 | → 0 | 56 min |
| 4 turrets, lit | 300 → 268 | → 39 | 63 min |
| 8 turrets, dark | no damage | 300 → 205 | 77 min |
| 8 turrets, lit | no damage | 300 → 243 | 140 min |
| 12 turrets, lit | no damage | no damage | never (3 h, through the ×2 cap) |

- Those runs had **no core repair**. A repair is 40 HP in 4 s for 2 steel + 1 copper, so an attentive player with 8 lit turrets holds indefinitely. The table shows the unattended floor.
- **Power** `[MATH]`: 8 turrets (160 kW) + 8 arc lamps (96 kW) = 256 kW. That is most of one 300 kW generator and about **230 coal per hour**, all the time. `TASK-REMAINING.md` AUD-ECO-03 (REL-27) records the Home coal patch as 700 units (unverified there, not re-verified here). If so, **lighting the defence, not feeding it, is what exhausts Home**, in about three hours.
- **Not assumed:** perfect aim, optimal layouts, or a player who is present. The rifle is left out of every table above. Real players will also over-build on one side and leave gaps.

**What this means.** "The factory feeds the defence" is approved as the heart of the game, but today one Assembler at 16% duty feeds everything, and 12 turrets make Home permanently safe by about the third hour. Body count cannot fix that, because the 48-alive cap and the wave plan bound it. §6.4 suggests where the demand should come from instead.

---

## 5. Constant threat or breathing room?

### 5.1 Two different things

- **"Enemies exist nearby"** = presence. Things move in the dark. Leaving the light is a choice with a cost.
- **"We are constantly under attack"** = pressure. The base takes damage and demands attention.

A factory game needs plenty of the first and carefully rationed amounts of the second. Today Relight has none of the first and a HUD that implies a lot of the second.

### 5.2 Three models compared

| | **A. Continuous pressure** | **B. Persistent roaming + intermittent raids** | **C. Mostly event-driven** |
|---|---|---|---|
| What it is | Aliens trickle at the base all the time; raids are peaks on top | Aliens live in dark districts and hunt when disturbed; Home is attacked only by announced raids | The world is empty between announced raids and scripted encounters |
| Closest to | tower defence | `ALWAYS_DARK_SPEC.md` §6 + U-D-16/38 | **what the port does today** |
| Tension | high and flat; goes numb | varied: low at Home, rising with distance and darkness | spiky; zero between events |
| Time to build | poor; every layout is built under fire | good inside the light | excellent, to the point of boredom |
| Exploration | punishing; leaving Home means it bleeds | **the main source of danger and interest** | safe and dull; nothing to find or fear |
| Does automation matter? | yes, constantly | yes: it holds Home while you are away | only during events |
| Does lighting matter? | only at the wall | **yes, everywhere: light is safety and territory** | only for turret sight |
| Do defences matter? | constantly | in raids, and at outposts in dark districts | in raids only |
| Fatigue | high | low to medium | low, then boredom |
| Recovery after a loss | hard; pressure never stops | good: quiet floor + safe lit ground | good, if the raid loop has a failure path (it has none today) |
| Readable difficulty | poor; hard to tell why you are losing | good: the player chooses risk by where they go | good but binary |
| Exploit surface | kill-farming if anything drops | boundary camping, spawn blocking with lamps (intended) | skip-the-event tricks (ENM-01 is one) |
| Performance | highest steady body count | 27 to 54 roamers mostly asleep + raid bodies | lowest |

### 5.3 Should enemies keep coming at all times?

**No — not at the base.**

- Continuous attacks punish exactly what the game asks the player to do: stand still and think about belts.
- They also make it impossible to tell whether a change to the defence helped.
- But the **world** should never be empty. Presence should be constant. Pressure should be scheduled and announced.

### 5.4 Recommended default: **Model B**

- It is what the approved documents already describe. It needs **no new decision** to adopt, only the existing rows built (REL-42, REL-34, REL-38).
- It makes light the second spine of the game, which is the direction of U-D-58 to U-D-62.
- Three additions make it work. All three are **proposals**:
  1. **A guaranteed quiet floor** after every major: a fixed stretch with no warning on the HUD and no bodies.
  2. **Minors that actually occur**, placed on purpose after the quiet floor and before the next warning, instead of by an independent timer that usually misses.
  3. **A failure path**: a major that has won or lost ends, the survivors leave, and the player can rebuild.

---

## 6. Recommended model and provisional tuning

### 6.1 How the game stage is detected (proposal)

- **Stage = number of plants commissioned, highest ever reached.**
  - Early: 0 plants (Home only).
  - Mid: 1 to 2 plants.
  - Late: 3 or more, up to the Master Switch.
- **Inside a stage**, majors keep U-D-47's rule: +15% per major, up to that stage's cap.
- **Why this avoids circular scaling:**
  - It never reads machine count, turret count, ammunition stock or power. Building more defence never makes raids bigger (U-D-47).
  - It never reads losses. It only goes up ("highest ever"), so losing on purpose cannot lower the threat, and one bad raid cannot start a death spiral.
  - It is not a pure clock. A player who turtles at Home meets bounded growth to the early cap and no further.
- This settles AUD-CMB-01's three-way conflict as: **tiers set the floor and cap (U-D-37), assaults climb inside the tier (U-D-47), camps alive do not change raid size**. That is a recommendation for owner decision F1-06, not a decision.
- Until plants exist in code (REL-37), only the Early column can be built.

### 6.2 Current values against recommended provisional ranges

**Current** = what the code does. **Recommended** = a starting range for playtest, not a balance claim.

| Setting | Current | Early (0 plants) | Mid (1–2 plants) | Late (3+ plants) |
|---|---|---|---|---|
| Roamers per dark district | 0 (unbuilt; spec says 3–6) | 3 near Home, 4 elsewhere | 4–5 | 5–6 |
| Roamer group size | n/a | 1–2, no Spitters in pairs near Home | 2–3, at most 1 Spitter | 3–4, at most 2 Spitters |
| Pack spacing | n/a | 40 tiles or more | 40 or more | 30 or more |
| Local cap (non-raid bodies within 40 tiles of the engineer) | none | 6 | 9 | 12 |
| Active pursuers of the engineer (soft) | none | 4 | 6–8 | 10–12 |
| World cap | 240 | 240 | 240 | 240 |
| Camp defenders | 0 (data says 20 / 22 / 20) | first camp 10–12 | 20–22 | stronghold 40–60 + guardian (design pending F1-03) |
| Small raid size | 6–10 | 6–10 | 10–18 | 16–26 |
| Small raid composition | Skitters + 2–4 Spitters | same; Spitters at most 30% | at most 35% Spitters | add 1–2 Breakers; Stalkers when built |
| Small raid interval | 5–7 min configured; about 1 per 45 min observed | **1 per major cycle, guaranteed**, after the quiet floor and before the next major's warning | 1–2 per cycle | 2 per cycle |
| Small raid warning | 30–45 s | 45–60 s | 30–45 s | 30–45 s |
| Large raid size | 60 → 120 (+15% per major, cap ×2) | 60 → 90 (cap ×1.5) | 90 → 140 | 160 → 220 (U-D-37) |
| Large raid composition | 2:1 Skitter:Spitter; 5% Breakers from the 2nd | same; Breakers from the 2nd | 8–10% Breakers; Howler when built | 10–12% Breakers; full roster |
| Simultaneous major bodies | 48 | 48 | 48 | 48 |
| Large raid interval | 18–22 min **start to start** | **end of recovery + quiet floor + a short minor slot, then the warning**; about 26–28 min start to start (6.5 assault + 5 recovery + 8 quiet + 2–4 minor slot + 5 warning) | about 24–27 min | about 25–30 min (longer assaults) |
| Large raid warning | 300 s | 300 s | 300 s | 300 s |
| Recovery after a major | 300 s | 300 s | 300 s | 300 s |
| **Quiet floor** (no warning, no bodies) | none; about 4–5 min by accident | **8 min** | 6 min | 5 min |
| Roamer refill | n/a (spec: 1 per 120 s per district) | 1 per 120 s | 1 per 120 s | 1 per 90 s |
| Camp respawn | none | none | one designated camp, reference conditions | same |
| Lit district | n/a | no roamers, lasting | same | same |

**Overlap rules** (all stages):

| Rule | Status |
|---|---|
| One major at a time; a committing major sends a live minor home | current `[CODE]` |
| No minor during a major's warning, assault or recovery | current `[CODE]` |
| Scripted encounters reserve the director | current `[CODE]` |
| Roamers never join a raid and ignore structures | approved, spec §6; unbuilt |
| No raid starts while a stronghold siege is active | **proposal** |
| Only one defended place is under major attack at once; the warning names it | U-D-38 direction; unbuilt |
| A major that has run its plan + 180 s with the core at 0, or 600 s past its plan in any case, ends: survivors retreat to their origin and are removed there | **proposal** (fixes ENM-01) |

### 6.3 Expected demand under the recommended ranges `[MATH]`

Idealised arithmetic. It is a sizing aid, not proof of balance.

| Per major cycle | Early | Mid | Late |
|---|---|---|---|
| Rounds for the major | 180–300 | 350–600 | 750–1,000 |
| Rounds for minors | 20–35 | 60–120 | 120–200 |
| One Assembler's time (100 rounds/min) | 2–3.5 min | 4–7 min | 9–12 min |
| Steel + copper | about 45–70 + 22–35 | 80–145 + 40–70 | 175–240 + 90–120 |
| Turrets for an unattended hold (open map, from §4.4) | 4–6 lit, with repairs | 8–10 lit | 12+ lit, cannons mixed in |
| Defence + lights power | 100–200 kW | 250–400 kW | 450–700 kW, which needs a plant |
| Coal for that, per hour | 90–180 | 225–360 | 405–630 |
| Core repairs after a thin defence | 0–3 (40 HP each) | 0–3 | 0–3 |
| Turret repairs | near 0 (turrets are not targeted, §2.3) | same | same |

### 6.4 Where defensive demand should come from (proposals)

Raid size is bounded by the 48 cap, so bigger raids cannot make the factory matter. These can:

- **More places to defend.** Each commissioned plant or outpost needs its own turrets, lamps, power and an ammunition line across dark ground. Distance, not volume, is the cost. This is already the direction of U-D-38 and REL-37.
- **Light upkeep.** Lamps already cost more than ammunition (§4.4). Lean into it: coal, then plants, are what hold territory.
- **Turrets that can be hurt.** Let Spitters pick a turret that is shooting at them within their 7-tile range. This creates repair demand, makes walls and dark-sight matter, and makes the U-D-47 comment true. Needs a decision (§9, decision 4).
- **The Breaker's real job** (spec §5.8, REL-39): cut the power line, so the lights go out and the dark turrets shorten their sight.

### 6.5 Exploits considered

| Exploit | Status | Answer |
|---|---|---|
| **Let the core die; all raids stop for ever** | **real today** (ENM-01) `[PROBE]` | the failure path in §6.2 |
| Spawn farming | no incentive: aliens drop nothing | keep it that way for roamers (spec §6). Camp rewards are one-off. |
| Camping the single raid origin so births fail the 28-tile rule | `[HYP]`, untested. A minor is dropped after 30 s of failed staging; a major has a `Cancelled` counter | runtime check (§7.3). If real: let births fall back to the next-best entry tile. |
| Walling the 10-tile mouth | handled: the breach field makes defences chewable `[CODE]` | confirm on the real city |
| Carpeting a district in lamps to stop refills | works, and is **intended**: light is territory, and it costs power | none needed |
| Standing at a district boundary to reset pursuers | possible once roamers exist | leash by distance from the pack's home point, not by district line |
| Delaying progression to keep raids small | bounded by the early cap; the ending (U-D-60) requires progress | acceptable: a turtle-friendly floor |
| Save and reload to reroll a raid | not possible: choices are a pure function of seed and serial, and all times are saved `[CODE]` | none needed |
| Kiting a camp to a turret line | possible; leash is 36 tiles | acceptable, even good play; the pursuer cap limits the size |

---

## 7. Validation

### 7.1 Existing automated checks `[AUTO]`

- Offline run of the sim test project on .NET 8: **740 passed, 1 skipped, 0 failed** (re-run for this audit, 2026-09-21).
- About 83 of these are core combat tests, all on a flat 160×160 map with one core.
- They pin specific numbers: the wave arrays, `RaidChoice` values, turret sight 9 lit / 6 dark, low ammunition at 12 rounds, core repair cost. Any rebalance will break them by design.
- **They prove the rules run as written. They prove nothing about balance.**
- Not run for this audit: the Unity EditMode and PlayMode suites, `Verify`, and any Play Mode session. Unity was not used.

### 7.2 The bounded simulation written for this audit `[PROBE]`

- **What it is:** a throwaway NUnit project outside the repository, compiling the shipped sim source unchanged and ticking `PowerPhase, TurretPhase, EnemyPhase, DirectorPhase, HomeCorePhase, LightPhase` on shipped data (`ReferenceData.Create()`).
- **Location:** `<scratchpad>/enemysim/` (`Probe.cs`, results in `out/*.txt`). It is not in the repository and nothing in the repository was changed to run it.
- **Setup:** flat open 160×160 test map; 8×8 core; turrets on a ring 8 to 14 tiles from the core, each with its own generator and (when lit) four arc lamps; engineer parked far away; 1.5 to 3 simulated hours.
- **Limits — read before trusting a number:**
  - Not the city. Four approaches instead of one. No walls, no buildings, no chokepoint.
  - No player: no rifle, no repairs, no rebuilding, no opening encounter.
  - Idealised resupply (the emptiest turret is topped up first, instantly).
  - One seed (7) for most runs, plus seed 11 for the undefended case.
  - .NET 8, not Unity's Mono. No timing was taken.

| Run | Setup | Result |
|---|---|---|
| A | no defence, 3 h | core down at 14:59 to a 7-body minor; major parks 48 bodies; **7 majors skipped**; mean living 40 |
| A11 | same, seed 11, 1.5 h | core down at 14:55; same freeze |
| F | no defence, engineer 12 tiles from the core | same freeze; the engineer was never noticed |
| B4 / B8 | 4 / 8 dark turrets, endless ammo | core falls at 56 / 77 min; 8 majors complete; 0 turrets lost |
| C4 / C8 / C12 | 4 / 8 / 12 lit turrets, endless ammo | core falls at 63 / 140 min / never; 0 turrets lost |
| D | 8 lit turrets, 50 rounds each, no resupply | core 300 → 172 in the 1st major; down at 52:55; **director freezes**, 6 skips, 40 bodies parked for 2 h |
| E100 / E25 | 8 lit turrets, 100 / 25 rounds per min | identical to endless ammunition |

### 7.3 The owner's scenarios

| Scenario | Mathematical | Probe | Needs runtime (Unity) | Needs a human |
|---|---|---|---|---|
| Slow new player | an undefended core dies to the first minor at about 15 min | confirmed (A, A11, F) | does the opening's prepared turret change this on the city? | is 13 min of grace enough to learn belts **and** build a defence? |
| Exploring far from Home | 300 s warning > 172 s longest walk; a minor cannot be attended beyond about 400 tiles | raids ignore the engineer (F) | none of it: nothing lives away from Home yet | does the trip feel tense or empty? |
| Multiple sites | not modelled; no code | no | blocked on REL-37 / REL-38 | later |
| Poor ammo production | average need is 16 rounds/min | 25/min = endless (E25); **no delivery = loss** (D) | belt and inserter delivery to the far side of a real base | does the dry-turret alert (E-17) arrive in time to act? |
| Power failure or loss of a key defence | brownout shrinks light to 50–100%, so turret sight falls from 9 toward 6 | dark against lit costs 7 to 60 minutes of survival | cut a generator mid-major; Breaker against a wall | can the player tell **why** they are losing? |
| Retreat with pursuers | 0.6 tiles/s advantage: about 30 s and 180 tiles to break a chase | no | chase on real streets; greedy stepping against corners | is running away fun or a slog? |
| Raid just before save/load | all state is absolute-time and saved | no | save mid-wave with globs in flight, reload, compare; ENM-07's one tick | none |
| Long session accumulation | bounded by the caps | **no accumulation with a working defence** (living 0 between raids over 3 h); **permanent accumulation after a loss** | Mono tick time at 48 and 240 bodies (REL-64) | none |
| Recovery after a failed major | impossible: ENM-01 + ENM-03 | confirmed (A, D, F) | confirm on the city, where ENM-02's fallback is the live path | after the fix: does losing feel recoverable? |

### 7.4 Playtest plan (after the P0 fixes; nothing here has been run)

1. **Forced-major tool first.** Add debug actions to force a major, set the assault number and show director state. Without them each check below costs 25 minutes of waiting.
2. **City geometry check (assistant, Unity).** Force majors 1, 2 and 8 on the real city. Record: approaches used, peak bodies alive, where they bunch, time to reach the core, whether a walled mouth is breached.
3. **Failure path check (assistant, Unity).** Let the core fall. Confirm the raid ends, survivors leave, repair works, and the next raid is announced.
4. **Mono timing (assistant, Unity).** Tick time at 48 and at 240 bodies with about 100 machines (REL-64).
5. **Owner session A — slow start.** Play the first 40 minutes without rushing. Note when you first felt safe, first felt threatened, and whether the first minor felt fair.
6. **Owner session B — leave home.** Walk to the first camp and back during the second major cycle. Did Home hold alone? Did you want to go back early?
7. **Owner session C — lose on purpose.** Let a major win. Is the recovery clear? Is it tedious?
8. **Record separately:** code, automated checks, assistant observation, owner play. No owner verdict is implied by any earlier step.

---

## 8. Prioritised follow-up

**No Linear issue was created or changed.** "Belongs with" names the existing issue that should absorb the work. New issues would only be needed where marked.

### 8.1 Confirmed defects

| Priority | Item | Belongs with |
|---|---|---|
| P0 | ENM-01, 02, 03 — the raid failure path: timeout or retreat, no dead-core target, local (not map-wide) repair block | **REL-44** (AUD-CMB-07). Widen its scope; its `[HYP]` is now confirmed. |
| P1 | ENM-04 — a minor can end the campaign; falls out of the P0 fix, then check the opening | REL-44, REL-72 (AUD-OPN-07) |
| P1 | ENM-06 — stale warning text; minor hides major | REL-12 (AUD-INT-08) |
| P2 | ENM-05 — `Defeated` is dead; give the raid record a real outcome | REL-8 (AUD-INT-04) |
| P2 | ENM-08 — relocation with raiders | AUD-PER-03 |
| P2 | ENM-09 — Siege and Defence missing from the data hash | REL-43 (AUD-CMB-06) |
| P3 | ENM-07 — one-tick target seam after load | AUD-PER-02 |
| P3 | ENM-10 — `debugAllowed` saved | AUD-UI-07 |

### 8.2 Missing approved functionality (build order)

| Order | Item | Existing issue |
|---|---|---|
| 1 | Siege tuning asset, so numbers can be tuned without code | REL-43 |
| 2 | First camp populated; the scouting objective leads somewhere | REL-34, REL-20 |
| 3 | Roaming population in dark districts | REL-42 (blocked by E-13 / E-05: REL-39, REL-38) |
| 4 | Full director: tiers, stage detection, other targets | REL-38, REL-37 |
| 5 | Stalker, Howler, Breaker's power-line job | REL-39 |
| 6 | Strongholds | REL-40 (needs F1-03) |
| 7 | Final raid sizing and target | REL-48, REL-49, REL-50 |

### 8.3 Tuning recommendations (no new issues; they ride on the rows above)

| Item | Rides on |
|---|---|
| Schedule majors from the end of recovery; add a quiet floor | REL-38 |
| Guarantee minors inside the free window; scale minors by stage | REL-38 |
| Stage = plants commissioned, highest ever | REL-38, decision F1-06 |
| First camp at 10 to 12 defenders | REL-34 |
| Roamer packs, spacing and the pursuer soft cap | REL-42 |
| Redo the U-D-47 ammunition sums after U-D-59 | REL-38, REL-30 (AUD-ECO-06) |
| Measure coal for lit defence against the Home patch | REL-27 (AUD-ECO-03) |
| Mono timing with enemies before roamers add load | REL-64 (AUD-PER-04) |
| Autosave when a major is announced | REL-65 (AUD-PER-05) |

### 8.4 Optional design proposals (would need new issues only if approved)

| Proposal | Why |
|---|---|
| Spitters may target a turret that is firing on them | makes repairs, walls and dark sight matter (§6.4) |
| Debug tools: force major, set assault number, show director state | makes every runtime check cheap (§7.4) |
| A raid direction marker from the unused `RaidWarning.At` | the player is told a compass word only |
| Tests on the real city map and across more than one major cycle | the suite cannot see ENM-01 today |
| Repopulation delay when a lit district goes dark again | the documents are silent |
| ~~Editor gizmo that draws the nine district borders, names and sizes in the Scene view~~ **Built 2026-09-21 at the owner's request**, with an in-game F8 overlay and a roamer-density preview (`Assets/Editor/DistrictGizmo.cs`, `Presentation/City/DistrictMap.cs`, `DistrictOverlay.cs`). Developer tools only: no gameplay, nothing saved. Its figures equal §3.3's for all nine districts | districts are invisible today and differ in size by 3× (§3.3) |
| Roamer numbers set by street area, not a flat number per district | a flat number gives a 5× difference in crowding (§3.3) |
| A second exit from Founders Court, or accept one approach on purpose | contradiction 1 (§2.4) |

---

## 9. Owner decisions needed

Existing decisions are in REL-71 (F1-01 to F1-20). These are the ones this audit touches or adds. Each has a recommendation; none is decided.

| # | Decision | Recommendation | Existing |
|---|---|---|---|
| 1 | **What should happen when the core falls?** | The raid ends, survivors walk home, the core can be repaired, the next major waits a full interval. No defeat screen (already decided). | REL-44 |
| 2 | **Which rule drives raid size?** | Tiers set floor and cap, assaults climb inside the tier, camps alive do not count (§6.1). | F1-06 |
| 3 | **Is radio gating retired?** | Out of scope here; only noting that the director ignores it. | F1-07 |
| 4 | **May aliens attack turrets on purpose?** Today they almost never do. | Yes, Spitters only, and only a turret that is shooting at them. | new |
| 5 | **How long is a major meant to last?** Design says 2 to 3 min; built is about 6.5 min. | Keep about 6 to 7 min and correct the documents. | new (doc contradiction 2) |
| 6 | **What happens when a lit district goes dark again?** | Roamers return only after 300 s of continuous dark, at the normal refill rate. | new |
| 7 | **One approach or several at Home?** | Test one approach first (§7.4 step 2). Decide after seeing it. | new (doc contradiction 1) |
| 8 | **Adopt a quiet floor and guaranteed minors?** | Yes: 8 / 6 / 5 min floors, one minor per cycle early. | new |
| 9 | **First camp size** | 10 to 12 instead of the reference's 20. | REL-34 |
| 10 | **Freight stronghold design** | Unchanged: still a proposal. | F1-03 |
| 11 | **Who picks the final raid's target?** | Unchanged. | F1-04, REL-49 |

---

## 10. The recommendation

**How populated should the world feel?**

- **Never empty, never crowded.** A fixed handful of aliens in every dark district: 3 near Home, up to 6 far away, 27 to 54 in the whole city.
- They travel in **small packs of 2 to 4**, at least two screens apart. A two-minute walk through the dark meets about one pack. A truck meets fewer.
- **A lit district holds none, for good.** Light is how the player takes the city back, and it is the only permanent cure.
- Camps and strongholds are separate, fixed, and stay cleared.
- Today the honest answer is that the world holds **nothing at all**, and that gap matters more than any number in this report.

**How often should enemies threaten us?**

- **At Home: only by announced raids.** Never a trickle.
- A **large raid about every 26 to 28 minutes early**, scheduled from the end of the last one, with five minutes' warning.
- A **small raid once per cycle** early, rising to twice, arriving after the quiet floor with under a minute's warning. It should be something a modest automated defence beats alone.
- **Away from Home: whenever you leave the light**, at a level you choose by how far you go.

**How large should small and major attacks be?**

- **Small:** 6 to 10 early, 10 to 18 mid, 16 to 26 late.
- **Major:** 60 growing to 90 early, 90 to 140 mid, 160 to 220 late, in four escalating waves, with **never more than 48 alive at once**.
- Size should follow **plants commissioned** and **assaults completed** — never machine count, never losses.
- Size alone will not make the factory matter, because the defence is cheap to feed. **More places to defend, light that costs coal, and turrets that can be hurt** will.

**When should the player get breathing room?**

- **The first 13 minutes** (as now).
- **Five minutes of recovery after every major** (as now), then a **guaranteed quiet floor** — 8 minutes early, 6 mid, 5 late — with nothing on the HUD and nothing alive near Home.
- **Any lit district, always.**
- **After a lost raid:** the attackers leave, the core can be repaired, and the clock restarts. Today losing ends the threat system for ever, and that is the first thing to fix.

**None of this is proven.** The defects in §2.1 are confirmed in code and in an offline simulation on a test map. Everything about feel, pacing and fairness needs the real city, the Unity runtime and the owner's hands.
