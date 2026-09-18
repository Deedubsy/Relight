# Gameplay fun audit — 2026-09-15

Evidence-based assessment of what Relight's Unity port is like to play today, why, and what to change first. Written by the Fable coordinating session at the owner's request. It changes no gameplay code, decisions, saves or task status; `TASKS.md` alone owns status and `DECISIONS.md` alone owns canon. Method and reuse instructions live in [GAMEPLAY_AUDIT_WORKFLOW.md](GAMEPLAY_AUDIT_WORKFLOW.md).

## Evidence and scope

- **Normal-stake play run** (this session, Play Mode in the open editor, Standard ruleset, default seed, no grants, no debug raid, no admin drawer): New Game to the end of the opening guide chain, sim time 0 → 2336 s (39 sim-minutes). Log and nine screenshots: [evidence/gameplay-audit/](evidence/gameplay-audit/README.md). Player actions were submitted as the same sim commands `WorldInput.cs` sends for mouse/keyboard, because Input System events do not fire in an unfocused editor; UI text was read from the live UI Toolkit tree. Walking time is therefore slightly *under*-represented (commands do not mis-click), and hand-craft lock time is exact.
- **Three Opus workers**, one question each, source-inspection only, no Unity control: A economy and automation, B combat and director, C progression, exploration and story. Their figures are quoted with file/symbol references; where the play run contradicted or confirmed a worker, the run wins and the note says so.
- **`/deep-research` workflow** actually invoked (run `wf_1fcffec7-d30`, 103 agents, 21 sources fetched, 105 claims extracted, 25 adversarially verified, 22 confirmed, 3 killed, 14 findings after synthesis). Its findings are reference material in §6; it produced no surviving claims for Mindustry, The Riftbreaker, Core Keeper, Dredge, Pacific Drive or Satisfactory, so those games are **not** cited here. One planned WebFetch fallback for the Riftbreaker/Mindustry attack rhythm was not run; that question stays open in §14.
- **Authority read**: `TASKS.md` (Phase C rows and the C-R proposals), `DECISIONS.md` (U-D-23, U-D-26, U-D-38, U-Q-02, U-Q-04, U-Q-21), `docs/RELIGHT_CONFIRMED_GAMEPLAY.md`, the 2026-09-14/15 handoffs, and the Phaser reference where the port diverged.
- Not covered: audio (single key, deferred), performance, the Scene-authoring tools, and anything after the opening because nothing after the opening is reachable in normal play yet (§3).

## 1. Plain-language assessment

Relight's opening is honest, readable and finishable without help. The guide card always names a next step, the HUD tells the truth about power and stock, hand-mining feels good, and the one authored combat beat (prepared turret → warning → five skitters → "automatic resupply working") lands exactly as designed. That is a genuine achievement for a two-week port and it should be protected.

It is also not yet fun for longer than about twenty minutes, for three reasons that compound:

1. **The player spends a quarter of the opening standing still.** Hand-smelting locks the engineer at Home for 4 s per plate; the taught route to three turrets needs roughly 95 steel and 27 copper, so about ten of the first thirty-nine sim-minutes are a progress bar. Worker A's calculation (698 s to the first turret with 83 % of it locked) matched the run.
2. **The factory never gets a job.** Once the Foundry runs, steel piles up to the 200 cap with nothing to spend it on; the one Assembler makes 100 rounds a minute against a demand of about four. The guide then ends with "Keep your workshop producing · Explore and connect your known destinations" and shows no destination. Automation is reached, then has nothing to prove.
3. **The threat is broken in both directions.** The 60-body major assault can never start on the shipped map (the raid line sits below every candidate origin, so staging fails and the assault silently vanishes with a developer sentence on the HUD), while the *unannounced* minor raid arrives with zero warning from the one western lane and can take the core from 264 to 108 HP against a single turret. Enemies are also trivially kited on foot. So the defence loop currently offers neither tension nor mastery.

Everything past the opening (camps, keys, plants, schematics, the second and third regions, the ending) is planned and largely unresolved, not broken. The playable game is a competent tutorial with a good first fight attached to a factory that has no purpose yet.

## 2. Playable versus planned

| Class | What | Evidence |
|---|---|---|
| **Implemented and reachable in normal play** | Hand mining and smelting; Generator/Excavator/chest/belt/pole/Foundry/Assembler/turret/rifle; power grid with throttle; the M0 opening chain to three turrets and the "prepare to scout" card; intro attack; minor raids; core damage, repair and recommission; death and respawn; save/load; Scene-authored world with 113 squares and imported buildings | Play run T 0–2336; `OpeningQueries.cs`, `Director*.cs` |
| **In code but disconnected or unreachable** | Major assault (`Commit` never succeeds on this map, §9); light hesitation (`LightHesitateS` has no caller); `Charge` phase; Cannon (buildable, 0 kW, live `shell` recipe, never taught); Wire/Frame/Board recipes feeding a dead chain; eight build cards with no source (Mixer, Refinery, Pumpjack, Mk2, Fast belt, Barricade, Alien workbench, Overclock); `Warn()`/Radio path emits nothing | Worker A §dead cards; Worker B D1, D9, D10, D12 |
| **Explicitly deferred (not defects)** | Stalker/Breaker/Howler (E-13/E-14); full raid director with four sectors and Radio (E-05); Arc/cells (E-11); Plasma/M3 (E-12); audio (D-11 subset proposed in C-R06); art package (U-Q-04/U-D-29) | `TASKS.md` rows named |
| **Port gap that blocks approved future gameplay** | `RegionImporter.cs:437-529` imports nine `SiteKind` values and drops the recruits (7), projects (9), plant sites (3), cores (3) and artifact sources (3) that `city.json` carries. Camps repopulate artifacts only. Nothing in D-08/E-01 can be built on the imported world until this is restored. Classified as a **dependency of D-08/E-01**, not a defect in the current phase | Worker C C-01, C-16; `WorldSites.cs:7-27` |
| **Unresolved decisions** | Ending (U-Q-02/U-D-23); art (U-Q-04); the fourth camp; the M4 objective; whether the engineer has an identity | `DECISIONS.md` §4 |
| **Previously reported problems the run confirms fixed** | The opening completes on the normal stake with no grant (C-OREOPEN); ore is visible-by-distance and minable at the taught spot; power HUD reports supply/need truthfully (C-FLOWPOWER); machine and chest transfers, typed slots and drag work (C-INVENTORYFIX); tooltips and controls match the correction pass. Still open from earlier reports: raw item key "ironore" in notices, identical machine glyphs, "Founders Court" label size | Play run rows T 0–700; screenshots 03–07, 13–15 |

## 3. Strongest parts worth protecting

- **Truthful HUD and guide.** "Need 3 — 3 tiles NE", "5 batches queued. Stay at Home until they finish", "Power 300 kW · Need 62", "Excavator: output full — empty the storage". The game never lies about state. Factorio's NPE retrospective ([fff-342](https://www.factorio.com/blog/post/fff-342)) lists "the guide never does things for the player" as the one rule they kept; Relight already obeys it.
- **The intro attack beat.** Warning strip with direction and countdown, bodies visible on approach, a turret that visibly wins with ten rounds, then the resupply acknowledgement 60 s after the first belt delivery. It is the only authored moment in the game and it works (screenshots 10–12, 14).
- **Hand mining feel.** ~0.5 ore/s with a live counter and a distance hint is a good first interaction; the "+1 ore" notices are the first satisfying feedback and arrive within 30 s of New Game.
- **A simple, legible economy.** One Excavator feeds one Foundry (30/min each); belts, poles and chests behave; the throttle is visible. Nothing here needs redesign, only consumers.
- **No hard deadlock and no lost run.** Worker A found no bootstrap deadlock; hand-copper exists; death costs 10 s and drops nothing; the core is recommissionable. The floor is safe, which makes the ceiling the whole problem.

## 4. The ten most important problems

Ordered by player benefit × urgency. Severity is subjective; class distinguishes implementation defect (I), design weakness (D), presentation (P), missing planned content (M) and unresolved decision (U).

| # | Problem | Class | Stage | Evidence |
|---|---|---|---|---|
| 1 | **The major assault never starts on the shipped map.** `RaidLineY` = 391 exceeds every scanned origin (max y 377); `Origin` falls back to edge tile (0,360); `Staging` returns −1; `Commit` is blocked and the notice strip prints "Assault start blocked by cleanup, recovery or its approach." after a 300 s countdown the player watched | I | After first turret | Play run T 1770–1943; Worker B D1; `DirectorRules.RaidLineY/Origin/Staging` |
| 2 | **Minor raids arrive with zero warning and can gut the core.** Bodies are born the same tick as the notice; the threat line was showing the stale major countdown; 5 skitters + 2 spitters from the west took the core 264 → 108 with one turret and a non-firing engineer | I/D | After first turret | Play run T 1960–2000, screenshot 13; Worker B D7 |
| 3 | **A quarter of the opening is a locked progress bar.** Hand-craft 4 s/plate, engineer immobile, serial queue; ≈10 min of 39 locked; the "Need 5 Steel — mine ore 14 tiles east" card does not acknowledge the queued batches | D/P | 0–40 min | Worker A P3/P4 (`HandCraft.cs:76-156`, `OpeningQueries.cs:216-221`); play run T 275–420, 700 |
| 4 | **The factory has no consumer.** Chest full at 200 Steel; Assembler 100 rounds/min vs ≈4/min demand (25×); line stalls in 66 s–3 min and stays stalled; nothing to build that the guide names | D | 20–40 min | Worker A P7; play run T 2000–2100, screenshot 15 |
| 5 | **The opening ends by telling the player to stay.** Final card: "Keep your workshop producing · Explore and connect your known destinations" with no destination, distance or reason; nearest camp 72 tiles north, unmarked | D/P | End of opening | Play run T 2336, screenshot 15; Worker C M1 |
| 6 | **Melee enemies cannot catch a walking player.** Commit aims 0.3 s early with 1.5-tile tolerance vs 1.8 tiles walked; notice radius is a literal 1.2 (`EnemyPhase.cs:63`) not the tuned 8; no `Charge`; production machines are impassable *and* never attacked, so a chest ring is an immune wall | I/D | Any fight | Worker B D2, D4, D5; not play-tested (I never fired or fled) |
| 7 | **The generator dies while you are locked.** One coal = 25 s at 160 kW; the guide teaches "one coal"; twice in the run the objective regressed to "Fuel your Generator" with "No power · Home workshop × 667" spam while the engineer was smelting | D/P | 10–25 min | Worker A P2; play run T 900, 1400, screenshot 13 |
| 8 | **Guide numbers contradict the HUD.** "The Home core draws 100 kW" and the v1 second-Generator gate ("exceed one Generator's 300 kW") are untrue: the core is never billed (`PowerGrid.Build:210-253`) and the taught build-out needs 304 kW (two street lights included), a 1 % throttle not a brownout | P | 15–35 min | Worker A P1; play run "Power 600 kW · Need 304" |
| 9 | **Turret placement inside the workshop wall is legal, silent and fatal.** Sight is blocked by walls for turrets but not for enemies; a walled turret takes the 8 dps "unseen" damage; `OpeningOrigin` never checks turret sight; the card never says "outside" | I/P | First turret | Worker B 7.1/7.2, D3, D11, D15 (`ArrayGeometry.Sight`, `Ballistics.cs:66-76`) |
| 10 | **Nothing to discover and no story on the way.** Flat tints, 9 of 113 square labels, 22 enterable buildings unauthored, canon is two sentences, no engineer identity, camps hold artifacts only, commissioning changes nothing visible | M/U | Post-opening | Worker C C-05, C-11, C-12, C-16, C-18 |

Developer-vocabulary leaks (the blocked-assault sentence, "× 667", "ironore", "Opportunity skipped") are folded into 1, 2 and 7 rather than listed separately.

## 5. Core loop and automation

**Do building, exploration and defence reinforce each other?** Not yet. Defence is the only consumer, its demand is tiny, and exploration has no output the factory wants (camps yield artifacts with no recipe). The loop is currently one-directional: mine → smelt → build → wait.

Findings (Worker A, confirmed by the run where noted):

- **Bootstrap**: no deadlock. Stake 20 S + 5 Cu buys exactly the Generator; the next step needs 10 S so the player re-enters the hand loop immediately (run T 220). First automation at ≈108 s of gathering is fine; the *felt* first payoff arrives at ≈650 s and is undersold because the card still says "hand-mine" while the chest fills (run T 647–688).
- **Resources without consumers**: steel (200 cap, hit at ≈T 2000), bullets (25× surplus), Wire/Frame/Board (dead chain), Artifacts (no recipe). Copper is the only binding resource: 1,200 units on the starting deposit, ≈40 min for one Excavator, next deposit 700+ tiles away. That is a real pressure but the player is never told.
- **Waiting**: the locked hand-craft (P3); the Queue-5-only workshop button (`WorkshopPanelController.cs:190,232`); the copper shuttle (copper cannot be mined and smelted without walking; copper smelting is never automated because no taught building consumes copper ore).
- **Mandatory manual work after automation**: copper smelting, coal delivery (the Generator has no belt input in the taught chain), core repair (pins the engineer 4 s/patch), hand-loading turrets (which the objective ignores, `OpeningPhase.NoteSupply:258-278`).
- **Fuel**: burn is load-proportional; at the taught 300 kW a Generator eats a coal every 13 s, so a 50-coal hopper lasts 11 min. Nothing tells the player that; the HUD shows fuel count, not minutes.
- **Storage**: Excavator hold 1, Foundry in 4/out 5, Assembler out 50, chest 200. Stalls are visible only on hover except through the problem strip.
- **Machine purpose**: Cannon (40 S + 20 Cu, 0 kW, live `shell` recipe) is a dominant, unexplained building. Every other untaught card is dead.

Each proposed balance change and its behavioural reason is in §11; none is a cost increase.

## 6. What comparable games do, and what transfers

All findings below come from the `/deep-research` run (21 sources, adversarially verified); they are developer intent, not measured outcomes, unless stated. Popularity is not evidence that the cited mechanic caused it.

| Reference lesson | Source | Evidence type | Transfers to Relight? | Feasible adaptation with existing systems |
|---|---|---|---|---|
| Automation must be reached quickly; 30–45 min of hand play is too slow | Factorio [fff-241](https://factorio.com/blog/post/fff-241) | Developer postmortem of the campaign | Yes, directly | Relight reaches automation at ≈10 min but re-enters hand play until ≈40 min. Let the queue run while walking, or shorten the 5-plate ceremony (§11 A1–A2) |
| Objectives need in-world purpose; recipes should unlock through technology; the guide never acts for the player; never block "the hard way" | Factorio [fff-241](https://factorio.com/blog/post/fff-241), [fff-329](https://factorio.com/blog/post/fff-329), [fff-342](https://www.factorio.com/blog/post/fff-342) | Developer | Yes | Relight already never acts for the player. "Prepare to scout" should name the camp and what is in it (§11 B1) |
| Rebuilding your first base 1–3 times is valuable; never force a restart | Factorio [fff-329](https://factorio.com/blog/post/fff-329) | Developer | Partly (Relight has one Home) | Keep death cheap; make the *layout* worth revisiting by letting the major actually arrive so a second turret line matters |
| Early drop-off is severe; only ~11 % ever launched a rocket | Factorio [fff-342](https://www.factorio.com/blog/post/fff-342), [fff-245](https://www.factorio.com/blog/post/fff-245) | Developer-reported telemetry | Yes as a warning | Relight's drop-off point is the end of the opening card (§4 #5) |
| Show the long-term goal early; split progression cliffs | Factorio [fff-257](https://www.factorio.com/blog/post/fff-257), [forum t=65070](https://forums.factorio.com/viewtopic.php?t=65070) | Developer + player thread | Yes | One sentence at New Game about the three plants and the line, plus a visible far marker (§11 B2) |
| Bounded pollution absorption so enemy pressure tracks production | Factorio [fff-283](https://factorio.com/blog/post/fff-283), [wiki](https://wiki.factorio.com/Pollution) | Developer + documentation | Only in principle; Relight has no pollution and U-D-38 fixes the director structure | Scale minor-raid count with placed machines rather than clock (§11 C3, needs a design decision) |
| Fewer, larger, telegraphed attacks; exploration must not reduce pressure | Factorio [fff-445](https://www.factorio.com/blog/post/fff-445), forum t=134297, t=135461 | Developer + player threads | Yes, and the director already intends it (majors with 300 s warning) | Fix the major so it actually arrives (§11 F1); give minors a 30 s warning (§11 F2) |
| Legibility of enemy response via stats and tooltips | Factorio [fff-286](https://www.factorio.com/blog/post/fff-286), forum t=67694 | Developer + player thread | Yes | Turret tooltip: range ring, rounds-per-kill; threat line: count and type (§11 F4) |
| Knowledge-only reward; the log records only what was seen; pull not push | Outer Wilds [GDC 2021 talk](https://gdcvault.com/play/1027008/Independent-Games-Summit-Sparking-Curiosity), [slides](https://media.gdcvault.com/GDC+2021/beachum_gdc_2021(1).pdf), [Game Developer](https://www.gamedeveloper.com/design/live-die-repeat-how-i-outer-wilds-i-piques-curiosity-in-an-ambivalent-solar-system) | Developer talk | Partially; Relight's rewards are material, but its *clues* can work this way | Interior lines and square names that point at the next place (§11 S1–S2), recorded when seen |
| Tiered surface/mid/hidden clues with prev/next pointers | Same Outer Wilds sources | Developer | Yes | Each camp's loot note names the next camp (§11 S2) |
| A tiny early craft list is an invisible tutorial | Subnautica [GDC 2018](https://gdcvault.com/play/1025745/The-Design-of-Subnautica), [talk video](https://www.youtube.com/watch?v=7R-x9NSBS2Y), [Game Developer](https://www.gamedeveloper.com/design/how-subnautica-adds-structure-to-a-sandbox) | Developer | Yes, directly | Hide the eight dead build cards until their source exists (§11 A5) |
| Scale-dependence (synthesis) | Own interpretation of the above | Interpretation | — | Factorio's "the factory is the reward" needs throughput Relight will never have; Relight's automation payoff must be *defence held* and *place restored*, not scale |

Refuted or dropped by verification (do not reuse): Minecraft-clone framing of Factorio's origin; the continuous-factory campaign as a positive model; placed unlocks as exploration bait. Open (no surviving evidence): Mindustry and Riftbreaker attack pacing; the smallest satisfying automation payoff; run-based dread loops (Dredge, Pacific Drive); the ammo-logistics chore threshold.

## 7. Progression and pacing

Approved milestones (Worker C, from `RELIGHT_CONFIRMED_GAMEPLAY.md` and E-01):

| Milestone | Gain | Why the player wants it | Changes next session? | Cost understood? | Arrives in time? |
|---|---|---|---|---|---|
| M0 opening | Three turrets, Assembler, rifle | Told to | Yes, it is the whole game so far | Yes (cards) | Yes, but ≈40 min with 10 locked |
| M1 first camp / key | Access to the next region | Not stated in-game; the card says "carry eight bullets" | Unknown: no reward text | No: the camp is 72–270 tiles away with no marker | Not reachable in this build's guide |
| M2 plant commissioning | Workbench, Pumpjack, Refinery | Unlocks the dead cards | Should, but commissioning changes nothing visible (C-18) | 30 S + 15 Cu + core understates the walk and the guardian | Planned |
| M3 schematics (Arc/Plasma) | New weapons | Dominant upgrades with no acquisition route yet (E-11/E-12) | Planned | — | Deferred |
| M4 ending | Nothing | Unresolved (U-D-23) | — | — | Unresolved |

Pacing observed: 0–5 min good; 5–25 min alternates 80 s locks with 30 s walks; 25–40 min builds the turret line and watches one fight; after 40 min there is no next step the game names. Distances from Home (72,372): camp 1 89 tiles / 15 s walk, camp 3 270 tiles, the Quarry 795 tiles / 132 s. Walking is cheap in time but there is nothing to see: flat tints, nine labels, no landmarks. The world is big and empty rather than slow.

Dominant upgrades and arbitrary gates: the Cannon (§5) is the only dominant building and it is invisible; the three-turret gate (U-Q-21 cap) removes the intro attack if the player builds three turrets first (Worker C C-04); "carry eight bullets" is a gate with no consequence attached.

## 8. Enemies, combat and defence

Observed (play run): one turret with line of sight kills the intro five with 10 rounds; the unannounced minor cost 27 rounds and 156 core HP against a passive engineer (upper bound for a player who fights). The major countdown was shown and then cancelled.

Calculated (Worker B; assumptions: skitter 20 HP, spitter 50 HP, turret 10 dmg/1 s, range 9, hopper 50):

- Skitter 2 rounds, spitter 5. One turret kills two skitters on approach and beats up to 3 skitters or 2 spitters standing still.
- Intro 10 rounds; minor 18–29 rounds; two minors empty a hopper without resupply.
- Major 60 bodies = 1,800 HP = 180 rounds against 150 rounds in three hoppers, 2.5 s spawn spacing; 12 HP/s inflow vs 30 dps from three turrets in one lane. Drain, not danger, *if it ever arrived and came in one lane.*
- Core 300 HP falls in 37.5 s to a single unopposed body. Recommission (10 S + 5 Cu, 12 s) is cheaper than seven patches (14 S + 7 Cu, 28 s): a perverse incentive to let the core die.
- Rifle range 18 > turret 9 > spitter 7: a standing player out-ranges everything but reloads by hand every 10 rounds.

Roles and counterplay: two of five enemy types exist; both walk at 2 tiles/s (the per-type 5.4/3.9 speeds are unused); neither charges; the spitter's only distinction is range. There is no combination that changes a decision. Walls block enemy paths but not enemy or turret sight, and do block the player's rifle (D15), so walls hurt the player more than the enemy. Light avoidance is dead code. Camps have guardians on paper (E-13) and artifacts only in practice. Failure recovery is cheap, which is right; but "there is essentially no failure" (Worker B).

Dominant strategies confirmed by calculation, not yet by play: one turret solves every reachable encounter; a chest ring is an immune wall; walking defeats melee; the factory can be ignored after one Assembler fills 150 rounds.

Fairness: every raid so far came from the west edge tile (0,360). The guide's "Enemies can attack from any direction" is false on this map.

## 9. Story and motivation

Canon is two sentences in `docs/RELIGHT_CONFIRMED_GAMEPLAY.md:17-19` plus the pitch line; nothing else is approved. The port has not invented more, correctly (U-D-23). What the player gets today: a name ("Founders Court"), a goal ("Smelt your first Steel plates"), and no reason. Curiosity has nothing to attach to: the buildings are enterable but blank, the squares are unnamed, the camps are unlabeled, and commissioning a plant (when it exists) changes nothing on screen.

Where story can emerge cheaply through existing systems (Worker C §4; all are content, not code, and none establishes canon beyond what the owner approves):

- Name the 104 unnamed squares from the city generator's district data.
- One line per enterable interior (22), recorded to the log when seen.
- A shelter clue at each dropped recruit point, once C-01 is restored.
- Camp loot notes that name the *next* camp (the Outer Wilds prev/next pattern).
- Radio lines when E-05 lands.
- Lights on in the region when a plant is commissioned (the lighting system is real; 7 of 18 lights are wired, C-17).
- Named encounters ("the western lane") so warnings carry place.

Ending options A "The Grid", B "The Last Core", C "The Line Reopens" are Worker C's proposals with tradeoffs, recorded here for the owner and **not** adopted. Option C is the cheapest to show early (a broken tram line is already in the map) if the owner wants a visible long-term goal at New Game.

## 10. Archetype stress test

| Player | What happens | Where it goes wrong |
|---|---|---|
| Newcomer following the guide | Finishes the opening in ≈40 min, loses power twice, wins one fight, is told to keep producing | Never learns to take ore from the chest, place a turret outside, or budget coal; the last card gives no independent goal |
| Cautious stockpiler | Fills the chest, builds three turrets before the intro, watches the Assembler stall | Rewarded: nothing punishes waiting, and three early turrets can skip the intro (C-04) |
| Efficient optimiser | Finds the Cannon, rings Home with chests, walks away from melee | Every dominant strategy is free and unexplained; nothing pushes back |
| Early explorer | Leaves at minute 10 with a rifle | Worst served: no marker, no landmark, artifacts with no use, and the base is safe anyway so leaving costs nothing and gains nothing |
| Recovering from failure | Core dies, recommission for 10 S + 5 Cu | Well served mechanically, but no next-raid timing is shown so recovery has no urgency |

Sensible behaviour that makes the game worse: waiting (it is safe), stockpiling (steel has no consumer), and staying (the guide says so).

## 11. Prioritised improvements

Each item: smallest useful change → what it improves in behaviour → how to check. Dependencies in brackets. None is a cost increase; the two balance changes carry their design reason.

**Fix before the next meaningful playtest** (each blocks a truthful read of pacing or defence)

- **F1 Major assault can commit.** Data or rule: either move `RaidLineY` inside the scanned origin band or let `Staging` accept the fallback origin. Behaviour: the 300 s countdown resolves into the fight the player prepared for. Check: one normal run reaches T ≈1950 and 60 bodies arrive; `RaidFixture` gains a site so the test can prove it (Worker B D18). [none]
- **F2 Minor raids get a warning.** Emit the notice ≥30 s before spawn and clear the stale major line. Behaviour: the player can reach the turret line before contact. Check: threat line shows count and countdown before the first body exists. [none]
- **F3 Developer sentences off the HUD.** "Assault start blocked…", "× 667", "ironore", "Opportunity skipped". Behaviour: trust. Check: grep the live UI tree during a run. [none]
- **F4 Walled turret warned.** `OpeningOrigin` or placement refuses/flags a turret with no sight to the approach; the card says "outside the walls, facing the marker". Behaviour: first turret works for everyone. Check: place inside the workshop, expect a refusal or a red card. [none]
- **F5 Fuel minutes on the HUD** and "one coal" replaced by "fill it" in the card. Behaviour: no more objective regressions while locked. Check: run to T 1500 with no "Fuel your Generator" regression. [none]
- **A1 Hand-craft does not lock movement** (or the queue continues while the engineer is within Home). Design reason: waiting teaches nothing after the first batch; Factorio's campaign postmortem names hand play beyond a few minutes as the drop-off cause. Behaviour: the 80 s locks become walks to the chest. Check: locked time under 3 min in a 40-min run. [design decision; touches `HandCraft.cs`]

**Improve during the current port phase**

- **A2 Cards acknowledge queued batches and carried ore** (`OpeningQueries` material-source text). [none]
- **A3 "Take ore from your chest"** step, and the objective counts hand-loading (`NoteSupply`). [none]
- **A4 Guide numbers match the grid**: drop the "core draws 100 kW" line or bill the core; retitle the second-Generator gate as headroom for expansion. [decision: bill the core or not]
- **A5 Hide build cards with no source** (Subnautica's short list). [none]
- **A6 Copper on the belt**: teach a second Excavator on copper into the Foundry with a copper recipe, so the only binding resource is automated and the 25× bullet surplus has a sink. Design reason: gives steel a consumer and the factory a second lane to defend. [none]
- **B1 The scout card names the camp**, its distance and what is there. [none]
- **B2 One sentence of purpose at New Game** plus a far marker (the tram line or the first plant). [owner approves the sentence; U-Q-02 stays open]
- **C1 Chest ring immunity**: production machines attackable or passable (one or the other). [U-D-38 compatible]
- **C2 Notice radius uses the tuned 8 tiles**; melee tolerance covers a walking target. Design reason: fleeing should be a choice with a cost, not a free win. [none]
- **C3 Repair incentive**: recommission costs at least as much as the patches it replaces. [balance; design reason above]

**Incorporate into later planned phases**

- **D-08/E-01 prerequisite: restore the dropped site kinds** in `RegionImporter` (recruits, projects, plants, cores, artifact sources). Nothing in M1–M3 can be authored on the imported world until this lands. [D-08]
- **E-05**: minors scale with placed machines, not clock; four sectors so "any direction" becomes true; Radio lines; named lanes. [E-05, F1, F2]
- **E-13/E-14**: a charging enemy and a structure-breaker are the first two roles that would force a decision. [E-05]
- **C-17/C-18**: lights on commissioning; region visibly changes. [E-01]
- **Environmental storytelling items** from §9 as content passes. [owner canon]

**Optional, needs stronger evidence**

- Load-proportional burn is a good pressure if the player can see it; consider a Generator belt input only if coal delivery still dominates after F5.
- The Cannon: either teach it as the M2 reward or cut it from the opening registry.
- A "why I stopped" survey question in the C-ACC owner playtest, to test the §1 twenty-minute claim.

## 12. Subjective scores

Subjective, 1–5, with confidence; playable build versus planned design. Not targets.

| Dimension | Playable | Planned | Confidence | Note |
|---|---|---|---|---|
| Clarity | 4 | 4 | High | Best part of the build |
| First 10 minutes | 3.5 | 4 | High | Locks aside, it works |
| Automation payoff | 2 | 4 | High | Reached, then purposeless |
| Defence tension | 1.5 | 3.5 | High for playable; medium for planned (depends on E-05) | Major never arrives; minors unwarned |
| Exploration | 1 | 3 | High for playable; low for planned (content unwritten) | Nothing to find yet |
| Story | 1 | ? | High for playable; ending unresolved | Two sentences of canon |
| Would play tomorrow | 2 | 3.5 | Medium | See §16 |

## 13. Investigation table

| Question | Current evidence | Hypothesis | Research needed | Local check | Conclusion / confidence |
|---|---|---|---|---|---|
| Is the opening finishable on the normal stake? | C-OREOPEN handoff (grant-free line) | Yes | None | Play run 0–2336 | Yes; high |
| How much of it is waiting? | Worker A 698 s / 83 % locked | ≈25 % of the opening | Factorio fff-241 on hand-play length | Counted queued batches ≈10 of 39 min | Confirmed; high |
| Does the factory get a purpose? | Worker A 25× surplus | No | Subnautica short list; fff-342 drop-off | Chest full at T 2000, Assembler idle | Confirmed; high |
| Does the major assault happen? | Worker B D1 static analysis | No on this map | fff-445 on telegraphed raids | Watched T 1770–1943 live | Confirmed live; high |
| Are minors fair? | Worker B D7 | Zero warning | — | T 1960 raid unannounced, core −156 | Confirmed; high (damage is an upper bound) |
| Can enemies be kited? | Worker B D4/D5 | Yes | — | Not tested (engineer never fled) | Calculated only; medium |
| Is the power text true? | Worker A P1 | Core never billed | — | HUD Need 62/140/304 vs cards | Confirmed; high |
| Is the street-light throttle real? | Worker A unverified | ~1 % | — | Need 304 > 300 after second Generator | Real but negligible; high |
| Why leave Home? | Worker C M1 | No reason given | Outer Wilds pull-not-push | Final card has no destination | Confirmed; high |
| Did the port drop approved content? | Worker C C-01 | Yes, five site kinds | — | `RegionImporter.cs:437-529` vs `city.json` | Confirmed by source; high; classed as D-08/E-01 dependency |
| How do Mindustry/Riftbreaker pace attacks? | None survived verification | Waves scale with map/time | Fallback WebFetch not run | — | Open |
| Smallest satisfying automation payoff? | None | "Defence held without me" | Open | A6 would test it | Open |

## 14. Remaining uncertainties and next-playtest checklist

Uncertain: subjective feel of the 4 s lock with a mouse in hand (commands hide fidget); whether a human notices the minor before it bites; whether spitters out-range turrets in other layouts; kiting in practice; audio's contribution (none yet); anything about M1+ (unreachable).

Next playtest (owner, C-ACC or after F1–F5), normal stake, no grants:

1. Time from New Game to first turret, and total locked time (expect < 3 min after A1).
2. Did the major arrive at ≈T 1950 and was the countdown honest?
3. Did the first minor show a warning before the first body existed?
4. Where did you place the first turret, and did the game tell you if it could not see?
5. Did power fail while you were locked?
6. What did you do after the last card, and why?
7. Did you fire the rifle, and did reloading feel like a chore?
8. Did any sentence on screen read like developer text?
9. One line: why would you play tomorrow, and what made you stop?

## 15. Sources

Reference-game sources are linked inline in §6. Project sources: `TASKS.md`, `DECISIONS.md`, `docs/RELIGHT_CONFIRMED_GAMEPLAY.md`, `ORE_OPENING_2026-09-15.md`, `FLOW_POWER_FIXES_2026-09-15.md`, `INVENTORY_FIXES_2026-09-15.md`, `HOME_OPENING_REPAIR_PLAN.md`, `UNITY_PHASER_AUDIT_2026-09-14.md`; code references as cited (`Relight.Sim`: `OpeningQueries.cs`, `OpeningPhase.cs`, `HandCraft.cs`, `Mining.cs`, `Placement.cs`, `PowerGrid.cs`, `PowerNetwork.cs`, `DirectorRules.cs`, `EnemyPhase.cs`, `RaidField.cs`, `Ballistics.cs`, `ArrayGeometry.cs`, `WorldSites.cs`, `RegionImporter.cs`, `WorkshopPanelController.cs`). Worker reports and the deep-research findings file are session scratch and are summarised, not stored, in this document; the play log is stored under evidence.

## 16. Why would someone keep playing Relight tomorrow, and what currently makes them stop?

They would come back because the game is honest and the first fight is good: they know what every number means, the turret they placed won, and the belt they laid kept it fed. That trust is rare and real.

They stop at the end of the opening card, around forty minutes in, because the game has just told them to stay home and keep producing steel that nothing consumes, the only big attack it promised never came, and the world outside has no name, no marker and no reason. Fix the assault, warn the minors, unlock the engineer's feet, give steel a second lane, and point at one place worth walking to. Then the question becomes interesting.

## Audit delivery checks

- No `.cs`, UXML, USS, asset, scene or save changed; Play Mode exited, World + GameUI scenes restored, `Assets/Temp` removed; screenshots moved out of `Assets`.
- No debug grants, debug raids or admin drawer in the play run.
- `TASKS.md` status unchanged; one §8 log line added.
- Proposals (C-R rows, ending options, balance changes) labelled as proposals; no new canon.
