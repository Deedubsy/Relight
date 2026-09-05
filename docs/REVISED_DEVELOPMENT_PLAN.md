<!-- Provenance (added by RI-00, 2026-09-05; everything below the rule is the user's plan, verbatim).
     Received as a pasted chat message on 2026-09-05 ("Revised development plan, Version 1.0").
     Execution authorised by the user's next message on 2026-09-05: "Whole plan" (DECISIONS.md D-RI-1).
     This file defines the RI-00 … RI-13 work; docs/PROGRESS.md alone owns their live statuses and the
     T → RI mapping; the adopted rules are integrated into docs/RELIGHT-design.md §28 and the decisions
     into docs/DECISIONS.md D-RI-1 … D-RI-6. The file it says it replaces, RELIGHT_FUN_IMPROVEMENT_PLAN.md,
     was never committed to this repository (checked 2026-09-05: no file, no reference, no history). -->

Revised development plan

Version: 1.0 · 2026-09-05

**Purpose:** integrate neighbourhood restoration, physical expeditions, meaningful infrastructure rewards, a coherent enemy ecosystem, bosses, and improved presentation into the full Relight game.

**Audience:** the AI implementing the existing repository and the human reviewing its results.

**This replaces `RELIGHT_FUN_IMPROVEMENT_PLAN.md` as the improvement plan. Do not execute both plans.** The earlier document-cleanup work remains valid; do not repeat it. Existing completed tasks, code, decisions, and historical evidence must be preserved.

## 1. How the development AI uses this plan

When the user asks you to implement this plan:

1. Read the current repository instructions, `PROGRAMME_STATE.md`, and `PROGRESS.md`.
2. Execute **RI-00** first. Inspect the real repository, reconcile this plan with existing work, and install one unambiguous task sequence.
3. Implement the next eligible task from `PROGRESS.md`. This document defines tasks; **`PROGRESS.md` alone owns their live statuses**.
4. Read only the rules and evidence needed for the selected task. Do not reload the historical archive every session.
5. Build, verify, review, and update the current handoff. Follow the user's requested execution scope: one task if they request the next task; continue through eligible tasks if they request the whole plan.

This document is a development plan, not evidence that anything is implemented or tested. The supplied snapshot was pre-Phase-5: the factory was incomplete and the expedition hybrid was approved but unbuilt. Verify that this is still true before editing.

### Authority and decision labels

| Label | Meaning |
|---|---|
| **Direction** | The integrated direction requested in this conversation: restoration projects, prepared expeditions, infrastructure rewards, enemy variety, bosses, and restrained mystery. |
| **Implementation default** | A concrete starting choice in this plan. Use it when asked to implement the plan unless current user instructions supersede it. It is reversible and not a historical approval or validated design fact. |
| **Tuning candidate** | A value or configuration to test. Keep it separate from existing benchmark configurations until explicitly adopted. |
| **Later candidate** | A concept for a later experiment. Do not fully implement it ahead of its task. |

The user has requested this integrated plan. Do not invent earlier approval messages, backdate decisions, or attribute this plan's detailed defaults to a previous gate. When execution is authorised, record the actual authorising message and which defaults it adopts.

Ask only when a necessary choice cannot be resolved by current instructions or these defaults, or would substantially change scope. Complete independent authorised work before presenting unresolved decisions together. Do not ask the human to choose routine class names, UI padding, or experiment names.

Where this plan deliberately leaves tuning data unspecified (for example a boss packet's unit count or a restoration kit's price), the AI may choose an initial **candidate** based on the existing economy, record its origin, and test it in the candidate profile. Missing candidate numbers do not block the prototype. Changing the established release/benchmark configuration or adding a new mechanic is a separate, visible decision.

## 2. The game we are building

**You are an engineer reclaiming a dead city. Build a factory that can sustain your front, prepare expeditions into occupied streets, restore important infrastructure, and use each recovered capability to undertake a larger project.**

The repeating experience is:

**Choose a destination → prepare supply → establish an approach → restore it → gain a useful capability → choose the next project.**

Blocks remain the units of ownership, construction and frontage. Enclosure remains a major reward. Neighbourhood projects give those block decisions a purpose.

### Experience priorities, in order

1. The player understands a useful goal and chooses how to pursue it.
2. Automation makes a repeated manual job reliably unnecessary during normal operation.
3. Expeditions reward preparation, route choice and engineering judgement.
4. Restoration visibly changes a recognisable place and enables something useful.
5. Enemies create distinct decisions with readable causes and counterplay.
6. Later tools enable larger projects and reduce learned repetition.
7. Different maps and project priorities support different worthwhile plans.

### Preserve these boundaries

- Full-game scope and the city-wide Relight ending remain.
- Keep the deterministic simulation and physical inventories, production and delivery.
- Keep automated defence as the way to hold an established front.
- Keep buildings and productive infrastructure recoverable after territorial loss.
- Keep core recipes short. Use existing materials for restoration wherever possible.
- No enemy loot, experience farming, random equipment drops, survival needs, settlement happiness, or dialogue-tree system.
- No automatic world-wide difficulty escalation simply because the player takes time to build.
- No compulsory reflex-heavy boss fights. Prepared automated defences must remain useful.
- Do not make belts and production machinery routinely destructible.

Replace the old automatic “fourth system means stop” rule with a practical scope test: a feature must strengthen the loop above, have a clear implementation boundary, and justify its interaction and testing cost. This is not permission to add unrelated systems.

## 3. Shared vocabulary — use these terms consistently

| Term | Exact meaning |
|---|---|
| **Block** | Existing street-graph face and unit of ownership. |
| **District type** | Existing terrain/resource identity such as civic, residential, industrial or outskirts. Do not repurpose this field. |
| **Neighbourhood** | A named group of connected blocks associated with a restoration project. Metadata over existing blocks, not another ownership simulation. |
| **Project site** | A specific existing or planned facility the player can restore. |
| **Expedition** | A player-planned trip to inspect, connect, supply or commission a site. Not a separate mission instance. |
| **Commissioning** | The deliberate activation of a prepared installation. It may trigger a local response. |
| **Restoration kit** | A defined delivery requirement using normal items. Not a new currency or generic quest token. |
| **Emergence point** | A visible location from which local enemies originate. It has a stable simulation identity and valid placement. |
| **Conductor** | A regular enemy coordinating a bounded local group of emergence points. |
| **Boss site** | A major project whose commissioning includes a distinct encounter. It does not replace every ordinary claim. |

**Implementation default:** neighbourhoods are non-overlapping connected groups. A small authored opening is allowed; later groups are generated. Initial prototype sizes are tuning candidates, not global map rules.

## 4. State and interaction rules

These defaults resolve ambiguity in the hybrid. Update the GDD and relevant tests when they are adopted; do not bolt a second claim system onto the old one.

### 4.1 Block ownership

Keep `Dark → Contested → Held`. `Interior` remains a derived display/property state when the adjacency rule is satisfied.

- The map selects a destination, previews consequences, and allows planning. **Selecting a Dark block does not charge materials or grant ownership.**
- The engineer establishes a physical power connection to its substation or outskirts installation.
- The required claim materials are delivered to that installation through legitimate inventory interactions.
- The engineer uses an explicit **Activate** interaction within normal interaction reach.
- If the connection, materials and ownership prerequisites are valid, activation consumes the required materials once and starts `Contested`.
- Commissioning triggers its configured response. Successful burn-off changes the block to `Held` using the existing ownership model.

The installation UI must show which prerequisite is missing and why activation is unavailable. Power connection alone must not unexpectedly activate it while the player is still preparing.

**Implementation default:** ordinary activation requires an adjacent Held block. Existing special cases must be explicit. An unheld outskirts block can receive the required craftable substation before activation; remove the circular requirement that it must already be Held.

### 4.2 Field construction

The player needs to prepare a position before claiming it.

- Normal production placement remains on Held lots under the adopted siting rule.
- **Implementation default:** extraction and assembly are both allowed on Held lots. Interior space is valuable through safety, power savings and freed defences; do not add a new interior-only assembly restriction during this pass.
- Field deployment permits poles, unlocked lights, turrets, belts, inserters, chests, and the required outskirts substation on a Dark/Contested block directly adjacent to Held territory.
- Existing reach, footprint, collision, inventory, unlock and price rules still apply.
- Field electrical devices associate with the physically connected upstream grid and contribute real demand. This must not mark their whole block Held or grant abstract power to an isolated position.
- Darkness alone does not become damage to the engineer. Enemy behaviour supplies local danger.

If the current power model cannot represent pre-claim field devices, implement that bounded capability explicitly in RI-03. Do not hide it behind an infinite-power flag.

### 4.3 Avoid double attacks and duplicate costs

Use one commissioning event ID per activation attempt. Its configured initial response replaces or incorporates the ordinary wake bloom for that activation; it must not accidentally fire both the old wake spawn and a new encounter spawn.

Regular awake-block blooms may continue under the normal front rules. Show those separately in telemetry.

Resources are either in an inventory, committed to a restoration stage, or consumed. Never charge both the Depot and the installation for the same activation. Repeated clicks and replayed commands must be idempotent where appropriate.

### 4.4 Projects and ownership have separate records, one activation

A project record tracks preparation, deliveries, encounter progress and rewards. The block record alone owns Dark/Contested/Held. Do not maintain a second project-owned territory state.

- A new ordinary project site's successful activation uses the normal burn-off and marks its project restored when its prerequisites and commissioning finish.
- For the **Junction Heart**, preparation occurs while its central block is Dark and adjacent to Held territory. Both feeder cabinets are on that field-deployable site; its approaches are accessible from prepared neighbouring streets.
- Starting that encounter is also the site's claim activation. The central block becomes Contested, and **boss commissioning replaces its ordinary burn-off completion timer**. Do not make it Held automatically after the old timer while the boss is still active.
- Boss completion transitions that block to Held and grants the project reward. Interruption returns the central block to Dark and clears its active attempt; completed deliveries remain committed, and adjacent Held blocks retain their own state.
- The special occupied site is visibly marked before activation. There is no hidden boss living inside a supposedly secured interior.
- A previously restored facility whose block is later lost follows normal retake/mothball rules. Its completed boss is not resurrected and its unlock is not granted again.

For an ordinary supply-depot project built inside an already Held block, commissioning changes only the project/facility state. It cannot create a second claim or charge claim materials again.

## 5. Neighbourhood restoration and useful rewards

### 5.1 Project model

Use a small project record linked to an existing facility and block IDs:

`projectId`, `siteId`, `neighbourhoodBlockIds`, `requirements`, `deliveredItems`, `stage`, `activationAttemptId`, `rewardId`.

Suggested stages: `discovered`, `preparing`, `ready`, `commissioning`, `restored`, `interrupted`. Availability is derived from actual prerequisites; do not maintain contradictory copies of state.

Projects accept normal materials from player inventories and the same physical delivery systems as other facilities. The first project must be deliverable before trams exist; never require the reward to deliver its own unlock kit.

Completed stages and rewards are recorded once and survive save/load. Follow existing unlock persistence when territory is lost; distinguish owning an unlock from having an operational facility.

### 5.2 Initial reward catalogue

| Project | Material purpose | Reward to demonstrate |
|---|---|---|
| Rail-yard restoration | Prepare, connect and supply the depot | Working freight transport that makes a subsequent delivery easier. |
| Foundry restoration | Recover an industrial installation and maintain its approach | Access to processing deposits and a new resource-supply plan. |
| Riverside generation | Deliver its construction materials and reconnect it | Fuel-free generation changes the coal and power plan. |
| Local supply depot | Establish a physical stocking location | Shorter restocking trips using actual inventory; no global magic stock. |
| Transformer restoration | Supply and secure a late project | Visible progress toward the city-wide Relight. |

Reuse existing facility definitions. A local depot can initially be a named installation using the existing chest/inventory system. Do not introduce a separate settlement economy.

### 5.3 Restoration feedback

Every restored project must provide all three:

1. **Function:** an operational facility, unlock, supply point or endgame contribution.
2. **Presentation:** a recognisable before/after state with a readable commissioning effect and optional audio.
3. **Next opportunity:** enough information to understand a practical use for the reward.

Ordinary enclosure should visibly move the front, identify reusable defences, and display actual released substation demand. Do not invent an unrelated stat bonus.

## 6. Why the enemies are here

**Direction:** the Rot is a colony occupying abandoned service tunnels, cable ducts, substations and industrial structures. Its origin is initially unknown.

Working illumination disrupts the colony locally. Commissioning an installation also creates a disturbance that nearby organisms can detect. Therefore light can suppress local rot while the act of activation provokes a response from an occupied area nearby.

Rules:

- Enemies come from valid, readable emergence points or established occupied sites.
- Do not spawn threats inside secured interiors merely to surprise the player.
- Discovered sites show local activity and likely approaches without requiring exact spawn-count knowledge.
- Local threat intensity comes from district type, existing rot, occupied sites, project state and the player's expansion. Avoid a hidden global aggression meter.
- Once an area becomes secure under the ownership and local-source rules, preserve that safety until a visible territorial breach or explicitly defined encounter event changes it.
- No drops or rewards for repeatedly killing ordinary enemies. Rewards come from restored access, infrastructure and reduced local threat.

## 7. Regular enemy roster

**Scope:** five regular archetypes. Build them in the task order, not all at once. Retain existing internal IDs where practical; “Breaker” can be the display name of the existing Hulk rather than a duplicate implementation.

| Enemy | Job | Behaviour | Counterplay | Readability requirement |
|---|---|---|---|---|
| **Crawler** | Ordinary supply pressure | Approaches exposed lighting and installations from a known edge. Retains existing logistics-focused behaviour unless a task explicitly changes it. | Supply turrets, cover approaches, repair a threatened connection. | Direction and current target are inspectable; warning precedes a consequential breach. |
| **Shade** | Exploit connected darkness | Uses unlit routes toward an installation; existing untargetable-in-darkness rule remains. | Illuminate its route, establish a lit firing position, intercept once revealed. | Give an approaching Shade an identifiable trace or flicker; no unexplained instant blackout. Render effects never imply a tile is mechanically lit when it is not. |
| **Breaker / Hulk** | Challenge an exposed or weakly concentrated front | Slow, conspicuous advance through defensive positions toward an installation. | Concentrated fire, heavy ammunition, better prepared approach geometry. | Announce direction and progress. Avoid invisible targeting of a distant asset. |
| **Stalker** | Make occupied-site scouting and field work consequential | Guards a bounded local territory; reacts to nearby player activity and pursues with a leash. | Scout, choose another approach, retreat to light and prepared turrets, or fight. | Visible territorial cues and a wind-up before its first attack. No unavoidable teleport ambush. |
| **Conductor** | Give an expedition a way to relieve local pressure | Rooted organism linked to a small, explicit set of emergence points. Strengthens those sources while alive. | Attack the source, commission a route that exposes it, or budget for its higher local load. | Show links and an upcoming activation cue. Its destruction visibly removes only its local influence. |

### 7.1 New-enemy implementation defaults

Use existing Crawler combat values as the reference where useful. Store all candidates in one data configuration; do not scatter constants through AI, renderer and tests.

**Stalker state machine:** `guard → investigate → pursue → attack → return`, with normal death handling.

- Its territory belongs to a specific occupied site. There are no wandering Stalkers across every street.
- **Tuning candidates:** perception radius 8 tiles; maximum pursuit distance 16 tiles from its home; health 2× a Crawler; speed 1.2× a Crawler; first-attack wind-up 0.8 seconds.
- Reuse the current player-damage value and a one-second attack interval as a starting candidate. Do not multiply damage by frame rate.
- Movement, firing and construction inside perception can attract it. Use deterministic simulation events; do not infer “noise” from audio playback.
- No attacks during a valid dodge, no attacks after death, no pursuing through impassable terrain, and no permanent player chase beyond the leash.
- Restoring its linked site disables future site-specific Stalker spawns. Existing survivors return or behave according to their remaining valid target; no hidden instant respawn behind the engineer.

**Conductor state machine:** `idle → signal → recover`, with normal death handling.

- Stationary, with visible local links. **Tuning candidates:** health 10× a Crawler; a 20% increase to linked bloom counts; no modification of their damage or movement speed.
- Preserve the existing bloom cap. Multiple Conductors do not stack on one source; use the strongest active modifier.
- Killing one removes its modifier. It does not claim blocks, clear every enemy, give loot, or eliminate normal frontage costs.
- Reward is local and persistent: the destroyed node does not regenerate during that run. A dedicated retake mechanic would be a later design change.
- A living Conductor and its linked sources cannot operate from a Held block. Normal reclamation suppresses that node while its block remains Held; destroying it permanently removes the node. Distinguish suppression from destruction in save data. This preserves safe interiors without making a kill mandatory for every claim.

These values are candidates, not fun claims. Test contact frequency and useful counterplay before changing them. Do not add new status effects to compensate for a weak role.

### 7.2 Enemy combinations and fairness

Introduce each archetype individually before combining them. Example later combinations are Crawler pressure creating a lighting gap, a Shade exploiting that gap, and a Breaker drawing concentrated defence while another approach needs supply.

Consequential failure must have warning and a feasible response. Do not create an instant Crawler-to-Shade loss chain. Test more than one sensible response where the rules permit it; enemies should not all become single-solution locks.

For regular play, retain the recoverable-infrastructure policy. Use the existing agreed turret-damage behaviour until RI-10 reconciles it explicitly; do not quietly give Crawlers a new destructive ability.

## 8. The engineer and rifle

The engineer is valuable while inspecting, preparing and commissioning a position. Once established, the factory and defences maintain it.

Compare the player's options in an encounter: fire, place a turret, deliver ammunition, improve lighting, finish the connection, or retreat. Shooting should have a useful situational role without becoming the cheapest permanent front defence.

- Initially reuse the current rifle, ammunition and knockdown system.
- Test range changes in a separate candidate configuration after the representative expedition exists. Do not default to unlimited range.
- Improve aim readability, hit confirmation and selected-tool visibility alongside functionality.
- Preserve the ability to complete restoration using careful preparation and automated defences; mandatory personal damage checks are prohibited.
- Treat whole-hour shooting/danger caps as provisional guardrails to evaluate, not as measures of satisfaction. Record encounter frequency, duration, outcome and voluntary intervention separately.

A low shooting percentage does not automatically authorise increased enemy density, new roaming rules or unavoidable combat.

## 9. First boss: the Junction Heart

**Direction:** prototype this encounter first. It is the rail-yard restoration project and the test of the integrated game loop.

### 9.1 Layout and purpose

A rooted colony structure occupies the depot's switching installation. Two existing feeder cabinets serve different approaches. The player chooses which cabinet to restore first and where to establish supply and defence.

Use the existing Crawler and a small number of already-introduced Stalkers. Shades, Breakers and Conductors are not prerequisites for the first boss.

The reward is an operational transport capability, not a chest of random items. The next nearby delivery must provide a real opportunity to use it.

### 9.2 Encounter state machine

`discovered → preparing → ready → commissioning → restored`

From `commissioning`, failure of its active conditions leads to `interrupted`, then back to `ready` after repair. Save all transitions deterministically.

**Implementation defaults:**

1. The central installation and two cabinets accept defined restoration materials. Initial amounts are candidate recipe data priced against the actual opening economy.
2. Cabinets can be prepared in either order. Their completion persists; they do not each trigger a duplicate boss activation.
3. The player chooses **Start commissioning** at the central installation when both cabinets are supplied and connected.
4. Commissioning requires both feeders to remain powered. Its effect attacks the colony Heart; this is an engineering encounter, not a requirement to personally shoot a health bar.
5. **Tuning candidate:** 90 seconds of productive commissioning. Progress pauses when either feeder loses power.
6. Predictable reinforcement packets are requested once when productive progress first crosses 25%, 50% and 75%. Show the approach before spawning. The roster and counts are candidate data; retain the global population budget.
7. Each packet is keyed to attempt and threshold. Pausing, loading, or oscillating power must not duplicate it.
8. Attacks target the commissioned position under the enemy rules. Stalkers may target the nearby engineer within their normal territory/leash behaviour.
9. **Tuning candidate:** if the position cannot resume productive commissioning for 60 consecutive seconds, the attempt becomes interrupted. The player may also abort explicitly.
10. Interruption stops untriggered encounter packets. Completed cabinet deliveries remain. Productive commissioning resets for a retry; installation materials are not charged twice. Spent ammunition stays spent.
11. Existing encounter enemies are not duplicated or magically replenished by restarting. Bound the active population and clean up invalid targets normally.
12. At completion, the Heart is destroyed, encounter spawning ends, and the rail installation becomes operational. Resolve remaining enemies normally. Grant its unlock exactly once.

All reaction times above are tuning candidates. Preserve meaningful activity during commissioning; if testing becomes passive waiting, change encounter decisions rather than lengthening the countdown.

### 9.3 What preparation changes

- Two reliable ammunition feeds reduce vulnerability to one starved approach.
- Better lighting creates a more readable, defensible position.
- Securing adjacent blocks reduces competing normal-front demand.
- Nearby physical stock reduces recovery trips.
- A player may attempt commissioning with less preparation, but a failure should expose a comprehensible weakness.

Do not force one exact turret arrangement. The encounter should support at least two materially different workable preparations in testing, such as different supply routing or defence placement.

### 9.4 Boss acceptance

- A prepared factory can complete the encounter without compulsory rifle use.
- Personal intervention can resolve at least one representative recoverable problem.
- Failure preserves infrastructure and completed deliveries; retry does not create duplicate rewards, waves or charges.
- The player can identify the objective, active failure condition and next corrective action.
- The transport reward can actually be used in the next project. A toast alone does not meet completion.

## 10. Later boss candidates

Keep these as later candidates until the Junction Heart has been played and revised. Their implementation belongs to RI-12, not the first prototype.

| Candidate | Distinct encounter principle | Integration |
|---|---|---|
| **Furnace Walker** | A large moving threat follows a visible industrial route; supply and defensive concentration must work across successive positions. | Foundry-region restoration. Reuse Breaker movement/damage where possible; validate its large footprint against actual streets. |
| **Blackout Crown** | Linked colony organs interfere with clearly marked local lighting zones; the player chooses which connection to isolate and restore first. | Late transformer restoration. Reuse Shade/light rules; do not add unexplained global blackouts. |

Before building either, write its short state/transition specification in the relevant GDD section, using the first boss's evidence. Each must differ materially from defending two cabinets for a timer. If a concept adds high implementation cost without a different player decision, redesign or cut that encounter rather than manufacture another boss.

**Full-game target:** a small set of major restoration encounters, initially three concepts. Do not add a boss to every neighbourhood. The city-wide Relight remains the final culmination, not an automatic fourth new enemy archetype.

## 11. Opening, interface and presentation

### 11.1 Opening candidate

Build one representative opening profile with real resources and commands. It may use an authored neighbourhood and fixed seed while the wider map remains procedural. Keep its configuration separate from the old 75-minute benchmark.

| Candidate window | Experience to test |
|---|---|
| 0–3 minutes | Identify a supply need; make and deliver something useful. |
| 3–8 minutes | Establish an affordable physical ammo line and see it maintain a turret. |
| 8–15 minutes | Compare two nearby opportunities and prepare for one. |
| 15–25 minutes | Achieve a small enclosure and understand its practical reward. |
| Following play | Prepare and restore the rail yard, then use the transport reward. Measure its natural completion time before setting a release pacing target. |

Windows are hypotheses, not a script the human must obey. State-driven guidance accommodates alternate claim order, ordinary delays and mistakes. Do not artificially place a distant facility in reach without identifying the opening-profile change and updating that profile's validator.

### 11.2 Minimum presentation accompanies each system

- Screen-space UI readable independently of world zoom; UI scaling and contextual help.
- Prominent current goal with the reason it matters. Immediate shortages support that goal rather than constantly replacing it.
- Stable named destinations and directions; debug coordinates behind a toggle.
- Identifiable engineer, selected tool, machine purpose and working/starved/blocked state.
- Immediate place/remove/invalid-action feedback; visible item delivery and turret fire.
- Enemy silhouettes, target/approach cues, warning before major consequences.
- Commissioning, restoration and enclosure effects tied to real state.
- A small coherent street/ruin treatment and strong facility silhouettes. Do not imply impassable cover where collision does not exist.
- Basic sound and volume/mute controls. No essential information communicated only through sound or colour.
- Early save/load support sufficient for repeatable testing; no requirement to replay the opening after every interruption. Verify existing support before adding it.

Full art production and final audio remain later work. Do not defer all useful feedback to Phase 12.

### 11.3 Construction assistance

Finish the already-planned manual front kit before adding a competing system. Teach one manual supply/defence setup, then allow its learned pattern to be repeated efficiently.

Full blueprints and the Line truck can remain progression rewards. Move their availability earlier if observed repeated construction becomes fatigue. Actual effort and reward usefulness take priority over defending a four-hour manual-work requirement.

## 12. Restrained mystery

Tell the story through infrastructure the player already wants to restore:

- An illuminated sign identifies a place.
- A restored terminal reveals a short operational record.
- Different districts show different failed containment attempts.
- Major facilities add context to the central station and the final Relight.

**Implementation default:** a small data-driven reveal attached to each major project, optional to reread, never required to understand costs or controls. No dialogue trees, branching ending or mandatory journal collection in this plan.

Write a brief internal explanation of what the Rot is and the order of revelations before distributing clues. Do not let different AI tasks invent contradictory origins. Keep the initial mystery open to the player without leaving the writers' underlying explanation undefined.

## 13. Canonical tasks to integrate into PROGRESS.md

The IDs below identify work definitions. During RI-00, reuse an existing task where it already covers the work; otherwise add the RI ID. Record the mapping once. Do not maintain duplicate open tasks for the same implementation or renumber completed T tasks.

| ID | Task | Depends on | Acceptance |
|---|---|---|---|
| **RI-00** | Adopt this plan and reconcile the current repo | User asks to execute the plan | One live task order; GDD and phase scope updated; old improvement plan superseded; current build/limits verified; duplicate work avoided; root CLAUDE.md updated. |
| **RI-01** | Establish the real opening economy and resource accounting | RI-00 | Physical production, transfer, initial stock and consumption reconcile; no unexplained manual-feed totals; relevant early recipes and coal work; legitimate normal automation sustains a short excursion. |
| **RI-02** | Opening guidance and essential presentation | RI-01 | Goal is truthful and stable; character, machines and supplies readable; basic action/restoration feedback; save/load baseline established; main desktop UI usable at different viewport sizes. |
| **RI-03** | Implement physical commissioning and field deployment | RI-01 | One claim path; map planning does not claim; valid physical power/material requirements; paid once; field devices draw real power; outskirts order resolved; state/replay/save consistency. |
| **RI-04** | Enemy origins, Crawler/Shade clarity and Stalker prototype | RI-02, RI-03 | Valid emergence points; local activity readable; Stalker states/leash work; secured interiors respected; representative scouting has understandable counterplay. |
| **RI-05** | Neighbourhood project framework and rail-yard reward | RI-03 | Project stages and deliveries persist correctly; ordinary materials only; an operational minimal transport route makes a real follow-on delivery; no circular unlock dependency. |
| **RI-06** | Junction Heart encounter | RI-04, RI-05 | Meets §9.4; deterministic attempts; interruption/retry/save/load tested; no repeated threshold waves or duplicated costs/unlocks. |
| **RI-07** | Integrate the opening candidate | RI-02, RI-06 | Real automation, route choice, small enclosure, rail project and reward form one playable sequence; measure actual pacing without moving the legacy benchmark. |
| **RI-08** | Playtest and revise the representative loop | RI-07 | Automated robustness evidence plus an honestly recorded human session; fix identified comprehension/repetition/recovery problems; adopted pacing changes recorded explicitly. |
| **RI-09** | Complete remaining Phase 5 factory features | RI-08 for experience-dependent decisions | Remaining scoped recipes, belts, undergrounds, filters, splitters, chests, transport, placement tools, counters and real five-hour logistics validated; do not rebuild completed RI work. |
| **RI-10** | Complete regular enemy ecosystem | RI-08; RI-09 where heavy-supply systems are needed | Breaker and Conductor roles work; full roster introduced gradually; combinations tested; turret-damage semantics reconciled; no compulsory single-solution encounters. |
| **RI-11** | Expand project progression, tools, city identity and mystery | RI-09; RI-10 for threat-dependent sites | Existing facilities/survivors integrated; learned repetition automated; multiple worthwhile project orders; valid generated neighbourhoods; consistent reveals. |
| **RI-12** | Prototype and implement distinct later boss encounters | RI-10, RI-11 | One at a time, distinct decisions; readable local mechanics; recovery and optional personal combat retained; costs justified by play evidence. |
| **RI-13** | Integrate full progression and finish the release roadmap | RI-09–RI-12 | Complete Relight progression, seed fairness, scale, saves, accessibility, final presentation, long-run playtests and existing platform/release obligations. |

RI-08 contains human work. The AI can complete its automated subtask and prepare the session, but cannot mark human observations complete on its own. If the tester is unavailable, report the missing evidence and continue only independent work. Do not silently promote candidate balance or claim fun validation.

### Existing-work mapping and scope corrections

- Reuse relevant **T12/T13** work in RI-01 and RI-03. Physical fronts must replace abstract supply before using a run as evidence of physical expedition balance.
- **T12a** becomes part of RI-02; it is not a second objective system.
- **T12b** remains useful as a baseline; rerun relevant variants after RI-03 and RI-07.
- **T12c** contributes to RI-07 and RI-08; add human comprehension/motivation observations.
- **T19/T18** supply existing human-test definitions where applicable; revise superseded criteria explicitly rather than fake a pass.
- Bring a minimum useful part of **T16 / transport** into RI-05. Complete the remaining transport cases in RI-09.
- Fold **D-GB-3/D-GB-4** into the rifle and enemy tasks; give them actual task ownership.
- RI-10 includes the minimum real Cannon/Shell chain and legitimate Arsenal access needed to test Breaker counterplay. RI-11 expands the remaining facility progression; do not create a circular dependency where Breaker validation waits on an unlock that itself waits on completed Breaker validation.
- Correct T12/T14 promises to implement “every recipe/chain” where the required machine or unlock belongs to a later phase. Early tasks build early dependencies; later recipes remain data until their machines exist.
- Preserve useful obligations from Phases 6–14. A moved feature is owned once in its new task; the original phase points to that result.

## 14. Verification and evidence

### 14.1 Automated evidence

Use existing test/harness infrastructure. Add meaningful checks for:

- Resource conservation across production, transfer, delivery commitment, consumption and refunds.
- Player/bot command parity: no unavailable stock, reach, power, placement or free supply.
- Claim/commissioning transitions and costs, including double activation and invalid prerequisites.
- Save/load and replay through project progress, enemy state, boss thresholds, interruption and rewards.
- Spawn positions, population budgets, Stalker leash, Conductor links and safe-interior behaviour.
- Valid generated project access without requiring the reward to obtain its own prerequisites.
- Ordinary mistakes: delayed automation, alternate first claim, unnecessary spending, missed warning and a misplaced/repaired belt.
- Larger layouts and relevant performance on the actual supported renderer; do not compare software rendering to hardware targets as if they were equivalent.

Keep legacy reproduction tests. Preserve their original configuration and label their scope. Add new candidate scenarios rather than rewriting old observations. When a deliberately adopted rule invalidates a fixture, document why and which invariant the replacement checks.

### 14.2 Human evidence

The first observed session should exercise automation, an expedition, restoration, enclosure and the transport reward. A later session must exercise several consecutive claims/projects to expose repetition.

Record what happened, including:

- Whether the player knows the goal and its benefit.
- What preparation and route they chose, and why.
- Whether automation freed them from routine intervention.
- Whether an encounter created a useful decision.
- Whether failure was understandable and recovery practical.
- Whether they noticed and understood restoration/enclosure.
- Whether they used the reward and independently chose something next.
- What became repetitive, confusing or tiresome.

Record observer interventions. Do not require a particular quote, infer enjoyment from compliance, or label bot telemetry as human behaviour.

For the opening, report time by clearly defined activity: building, travel, preparation, combat, recovery, and idle waiting. Avoid double-counting overlapping categories. Use those measurements diagnostically rather than pursuing arbitrary percentages.

### 14.3 Evidence does not redefine the goal

A green simulation means its scenario passed. It does not prove an expedition is enjoyable or that all seeds and players behave similarly. Bot comparisons assess feasibility, relative cost and failure mechanisms; people assess the experience.

## 15. Documentation changes during RI-00

Update the existing authoritative files rather than create a parallel documentation hierarchy:

- **GDD:** integrate adopted rules from this plan, the shared vocabulary, enemy definitions, project/encounter state transitions, and clear candidate/later labels.
- **DECISIONS:** record the adoption of this direction, explicit changes from map claiming and old constraints, candidate tuning scope, and genuine unresolved decisions.
- **PROGRESS:** own the single execution sequence and acceptance evidence. Mark existing completed work accurately.
- **PHASES:** show which work has moved forward and retain the full release programme without duplicate obligations.
- **PROGRAMME_STATE:** concise current state, limitations, next task and missing evidence.
- **CLAUDE.md:** point to the current task, relevant rules and truthful evidence requirements; remove obsolete blanket restrictions that conflict with the adopted direction. Keep it short and do not paste this entire plan into it.

Archive only superseded plans/instructions where appropriate. Keep historical reports unchanged and linked. Repair references affected by moves. Do not repeat the earlier full-file cleanup.

## 16. Definition of a successful first implementation pass

The first pass ends after a representative restoration loop has been implemented, exercised, and honestly reviewed. It is not the end of full-game development.

- A player establishes a real supply line and can leave it operating.
- A nearby project offers a comprehensible reason to explore.
- Physical preparation and commissioning change the outcome.
- Enemies have readable origins, purposes and counterplay.
- The Junction Heart supports engineering preparation and recoverable failure.
- Restoration and enclosure are perceptible and useful.
- The transport reward works and changes a subsequent action.
- The next project is attractive without prompting the player through a fixed script.
- Remaining limitations and unobserved claims are explicit.

**First executable task: RI-00.** After adoption, proceed through the live tracker. Do not implement all five enemies, all bosses, or the full city before the representative loop is playable.
