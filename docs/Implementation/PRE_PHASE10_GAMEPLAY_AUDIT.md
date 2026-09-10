# Pre-Phase-10 gameplay audit

Historical finding correction (9 September 2026): the owner subsequently rejected restoration of territorial frontage. Finding 3 and the original EX-09C-04 recommendation are superseded by site lifecycle, attacked plant factories and explicit recovery jobs; they are not active implementation instructions. The owner also now includes RI-10/RI-12 in the correction pass. Original observations remain preserved below.

Date: 9 September 2026. **Verdict: Not ready because of foundational blockers.** The running campaign still has the supplied opening, lacks stolen portable cores and core-powered regional plants, and rejects the confirmed territory/commissioning model. The current logistics/UI foundation is reusable. Complete EX-09C-01–09 before Phase 10 content; this audit does not implement those tasks or close human gates.

## Scope and evidence

Authority: [RELIGHT_CONFIRMED_GAMEPLAY.md](../RELIGHT_CONFIRMED_GAMEPLAY.md), §§1–21, read in full. Repository search found exactly one copy, dated 9 September, untracked with no Git history or pre-existing references. The owner's explicit audit instruction establishes its authority. Earlier GDD/decision text is subordinate where inconsistent; the confirmed file was not edited.

Branch `codex/tram-expansion`, HEAD `8e54ed602da159905b0428f316f6e75cb25ae351`, heavily dirty before this audit, including prior gameplay/UI changes. [Source inventory](../evidence/pre-phase10-audit-2026-09-09/source-before.json) records the starting status and hashes. No production edits, rebuild, deploy, commit, push or reset were performed.

Normal documented launch: `npm run dev -w packages/game -- --host 127.0.0.1`, then the printed address with `?rules=exploration-v2&seed=3&view=world`. Actual inspected build: mutable production preview `http://127.0.0.1:5178/`, serving `packages/game/dist`; this is distinct from frozen UI-07/5182. `game/src/session.ts:25,154` parses rules and calls `createCampaign` only for the explicit campaign parameter. The **bare URL starts legacy-v1**, confirmed in [bare-entry.json](../evidence/pre-phase10-audit-2026-09-09/bare-entry.json). Legacy Heart/frontage, bots and recipe tables are not proof of active campaign features. Explicit campaign rejects legacy encounter/bot flags.

Evidence keys used below:

- **W — current walkthrough:** [script and eight recorded check groups](../evidence/pre-phase10-audit-2026-09-09/walkthrough.json), reusing the existing Playwright flow in a separate headless Chrome context at 1920×1080. Actual new campaign, no prepared opening: Home transfer → shortcut drag/use → paid Assembler → shortcut swap → split/merge → exact paused save/load → 1× controls. A separately labelled `/ui-network.json` prepared state demonstrated automatic tram movement, not normal-player reachability. No browser errors. The script used debug hooks to read state/save, not grant opening stock or bypass build/transfer commands.
- **T — current narrow tests:** [focused-tests.txt](../evidence/pre-phase10-audit-2026-09-09/focused-tests.txt): direct conveyors plus fixed tram, 8 passed/1 fixture-path failure from the repo root. [save-test-recheck.txt](../evidence/pre-phase10-audit-2026-09-09/save-test-recheck.txt): the single affected test passed from `packages/sim`, its expected working directory. All nine distinct selected cases passed across these runs; not a clean nine-case single run. Prepared-stock tests establish mechanics, not bootstrap reachability. Coverage includes the full chest→belt→Assembler→belt→chest→belt→turret chain, incompatible/full destinations, second-powered-stop freight, outage/cargo retention/reconnect, all four stops, save adoption and roaming/chase escape.
- **R — reused evidence:** all 26 recorded source/dist SHA-256 entries in [ui08-tram-roaming-source.json](../evidence/ui-redesign/ui08-tram-roaming-source.json) still matched at audit start. Its final typecheck/build and lint results are reused, so another build was unnecessary. Matching is limited to the recorded files, not every transitive input. [UI evidence README](../evidence/ui-redesign/README.md) contains broader earlier transfer-capacity, input isolation, camera, mining, layouts and paid network evidence. Those older runs are credited within their original scope; not rerun or certified wholesale against every current dependency. The old 10,000-seed pass predates the fixed-tram change. The prior 72-case run had 71 passes/one failure followed by a 12-case correction check; it is not a current full-suite pass.
- **S — source trace:** active entry → sim subsystem → downstream consumer/validator, detailed below. Code presence without relevant observation/test is **Unverified**, not a gameplay pass.

Opened and inspected three current screenshots: [opening](../evidence/pre-phase10-audit-2026-09-09/opening.png), [paired inventories](../evidence/pre-phase10-audit-2026-09-09/ui08-storage-backpack.png), [prepared network map](../evidence/pre-phase10-audit-2026-09-09/network-map.png). Home guidance visibly requests starter supplies. Backpack is left, storage right; headings and action bar remain legible at the inspected size; YOU is visible on the map. This is not a new resolution/accessibility matrix or art acceptance.

No full campaign, seed sweep, long-run performance or fresh-player timing experiment was run. Whether preparation fits before the first major assault, local factories feel necessary, and routes reduce player effort remains **Unverified**. Existing human Phase 5 credit is retained; P6-AMMO/P6-H/P7-H/P8-H/P9-H/EX-08H/T18 are not passed by this audit.

## Coverage matrix

Classification describes the audited build and **pre-audit** task coverage. The final column gives concrete ownership added/reconciled now; it does not claim implementation.

| Confirmed group / sections | Classification | Evidence and boundary | Remaining owner / dependency |
|---|---|---|---|
| Player movement, sprint/dodge, health, reach, recovery (§2/11) | Unverified for full group | S: engineer/walk/threat; W proves local reach transfers and normal controls, not knockdown or dodge in combat. Earlier P5/P6 evidence retained. | EX-09C-09 regression, existing human gates |
| Empty opening/onboarding (§3) | Implemented but incorrect | W: empty pockets but 200 steel/100 copper/50 stone at Home, 40 stored coal, 20 magazines collected, prebuilt generator with 40 coal; guidance tells player to collect supplies. | EX-09C-01, before content |
| Resources and item economy (§4) | Partially implemented | Current steel/copper/stone/coal and intermediates; T conserves physical flows. Iron/copper ores, crude/fuel/polymer are absent from active Item universe. P7 deferral lacked concrete completion criteria. | EX-09C-05, before economy validation |
| Automation (§5) | Partially implemented | W paid Assembler; T actual Shot production. Excavator/Mixer/generator/manual craft paths exist; Mk2/Foundry/Pumpjack/Refinery are not placeable. | EX-09C-01/05; reuse P5/P7 |
| Logistics/storage (§6) | Partially implemented | T verifies direct conveyor chain and full/incompatible handling; W real Home transfer. Routing/inserter filters have prior P5 evidence. Fast belt absent. | EX-09C-05/09; no mandatory inserter reintroduced |
| Stolen cores/search/recovery (§7) | Missing from code and roadmap | S: BaseCore is building HP; no portable core item, alien core power/recovery or core search selector. Broad EX-09/Q07 not adequate ownership. | EX-09C-02 |
| Regional plants/local factories (§7–8) | Partially implemented | S: finite connected generators and optional turbine; no chosen-core plant activation, plant attack registry or plant-to-tram factory geography. | EX-09C-03, then 04/09 |
| Electrical distribution and lighting (§8) | Partially implemented | S: real supply/load/throttle and lights; T powers two stops using generators. Current campaign does not spawn Shades, so intended darkness counterplay is absent. | EX-09C-03/08; full load/light combinations unverified |
| Machine artifacts (§9) | Missing from code and roadmap | Repair schematic is a player repair reward, not a machine attachment; no eligible-machine bonus path. | EX-09C-06 |
| Construction/territory (§10) | Implemented but incorrect | S: restoration immediately Held; campaign validator rejects Contested/frontage. Existing paid construction/blueprints are useful but do not provide confirmed commissioning/security. | EX-09C-04 |
| Weapons (§11) | Partially implemented | S: cursor rifle/shared magazines; full combat not replayed here. Active Arsenal rifle upgrade missing; historical rate/barrels code alone is insufficient. | RI-10, explicit Phase 10 scope |
| Defence (§12) | Partially implemented | T supplied gun turret; existing remote factory/repair/raid evidence retained. Active plant targets absent. Cannon/Shell already specifically retained with RI-10. | EX-09C-03/09; RI-10 for heavy chain |
| Enemy ecosystem (§13) | Partially implemented; heavy roster Scheduled for a later phase | T roaming Crawler chase; S Stalker encounter. Shade absent from campaign births; Breaker/Conductor in RI-10. Later bosses specifically RI-12; legacy Heart cannot run in campaign. | EX-09C-08 first integration; RI-10/12 later |
| Exploration/navigation (§14) | Partially implemented | W visible YOU and named known objective; S saved knowledge/pins and prior P9 evidence. No core-search destination. Plant geometry/reachability cannot be assessed before plants exist. | EX-09C-02/03/09; P9-H remains |
| Powered tram freight (§15) | Implemented and verified | W moving four-stop network; T any second powered stop starts requested cargo, outages/save conserve it. No extra encounter/rail-kit gate in fixed path. | EX-09C-03/09 connect actual plants; existing service reused |
| Engineer tram travel / truck (§15) | Partially implemented | Physical driven truck exists with earlier P5 evidence. No passenger tram seat/board/exit path; freight-only movement does not establish reduced walking journeys. | EX-09C-07 |
| Foreman/front kits (§15) | Partially implemented | S truck loads/travels/builds queued blueprints; area removal is engineer operation. No automated truck recovery job. | EX-09C-04 reuses P8 library and conservation |
| Survivors (§16) | Partially implemented | S five campaign recruits with permanent knowledge; no campaign Gunsmith/Rail crew improvement. Substation is already available, not a new Electricians reward. | EX-09C-06, preserve baseline/earned capabilities |
| Restoration facilities (§17) | Partially implemented; wider projects Scheduled for a later phase | Local station/radio/workshop/turbine records/benefits exist; W only prepared network, not full restoration. Generic Foundry/Refinery/Power station labels do not implement them. Transformer/wider orders are retained RI-11 but required explicit AC. | EX-09C-02/03/05; RI-11 later |
| Backpack/catalogue/action bar (§18–19) | Implemented and verified for W flows; Unverified for all edge cases | Shortcut assignment preserves inventory/machine count/hash; use spends real carried materials; swap/save, split/merge and transfer pass. Full/out-of-reach and removal/typing/scrolling have older R evidence only. | Reuse RI-02B, EX-09C-09 regression, Phase 12 remaining accessibility/art/audio |
| HUD/time/input (§20) | Partially implemented | W 1×/pause, readable inspected panels; opening guidance is wrong for intended start. Bare entry starts legacy. Existing map-click isolation R/T source retained. | EX-09C-01/09; Phase 12 release integration |
| Persistence (§20–21) | Implemented and verified for W/T cases; Unverified for future mechanics | W exact paused hash/preferences; T tram migration and cargo continuity. Cores/plants/artifacts/territory save contracts do not exist yet. | Each EX-09C task, then 09; RI-13/Phase 13 broad compatibility |
| Rule authority (§21 and older GDD/Q01/P7) | Conflicting instructions, precedence resolved in this audit | Owner-designated confirmed file supersedes supplied start, no-Contested and limited catalogue assumptions. Unspecified numbers are not decisions. | Updated pointers; PROGRESS owns execution |

## Prioritised findings and smallest coherent corrections

### 1. The opening economy still depends on granted supplies

**Confirmed mismatch, high priority; EX-09C-01 before Phase 10.** Expected §3 starts with nothing and reaches production/power/defence through gathering and manual work. Reproduce: new explicit campaign URL without `state`; open Home with E. [opening.json](../evidence/pre-phase10-audit-2026-09-09/opening.json) records the hidden-in-world stock as well as pockets. `campaign.ts:createCampaign → flow.ts:ensureFlow` (around 350–390) creates/fuels the generator and overwrites stock with START_CHEST; `goal.ts:campaignNext` (75–86) selects “Collect your starting supplies”. There is no prebuilt Assembler or turret in this campaign opening; public tram infrastructure is intentional, not an accidental free factory.

Actual bootstrap dependency trace: reachable salvage types include steel/copper/coal/stone; Excavator costs 10 steel, Generator 30 steel+10 copper, turret 15 steel+5 copper, Assembler 40 steel+20 copper (`flow.ts:MACHINE_COST`). Shot costs 2 steel+1 copper (`recipes.ts`), and the workbench uses carried materials; generator consumes coal. These ingredients need no later recruit in the present salvage model. **Inference, not demonstrated empty-start play:** a hand-gathering route appears feasible; simply deleting supplies is not sufficient validation. Introducing ores must preserve an initial usable-metal/manual path so the first Foundry does not require its own output. Revise the whole bootstrap/guidance together; verify legal paid preparation at normal time, then obtain actual pacing observations. Do not inherit the old first-major timing as proven suitable.

Also fix the bare entry route to select current gameplay while keeping an explicit legacy route. Otherwise new testing can accidentally certify obsolete mechanics.

### 2. “Core” names conceal a missing progression loop

**Confirmed missing integration, foundational; EX-09C-02/03 before Phase 10.** Expected §7: alien bases use stolen cores; approximate search → discover/fight/recover → choose plant → restore finite regional power and defend it. `campaignDefence.ts:13,38,60` defines BaseCore as block/x/y/size/HP/commissionedAt, registered for Home and restored station bases. `expansion.ts:restoreSite` (98–105) consumes delivered metal and registers station health objects. `flow.ts:Item/ITEMS` (140) has no portable core. The exact base HP indicator shown in W is not stolen-core discovery. `campaignGuide.ts` knowledge targets have no core-search record. No other inventory/command subsystem supplies the missing loop.

`campaignPower.ts:campaignGrid` has useful finite 300 kW coal generators plus one restored optional 600 kW turbine and real demand/throttle. `campaignTurbine.ts` restores that turbine using materials, not a recovered core, and does not register an attackable base. `campaignThreat.ts:eligible` (97) selects registered living base-health objects; remote major eligibility is currently radio-gated. Neither a Power station map label nor a working turbine establishes regional plant attacks.

Add distinct saved portable-core/alien-power/plant records and ordinary interactions, reuse physical inventory and the existing circuit calculation, then register each active plant as an actual target. Make source versus generator capacity/demand readable. Generate physically usable plant sites near tram branches with buildable factory/defence ground and actual last-mile fuel/ammo/repair routes. Verify one complete ordinary transfer/activation/supply connection and interruption; incentive strength and geography across the campaign remain play/seed work. Do not invent a plant count, simultaneous raids or numerical distances as owner requirements.

### 3. Security, commissioning and attack eligibility use the old contract

**Confirmed mismatch, foundational; EX-09C-04 before Phase 10.** Expected §10 has Dark→Contested→Held, commissioned security, derived interiors/frontage and recoverable factories. `expansion.ts:restoreSite` immediately writes HELD/subOn; `rules.ts:43` explicitly rejects any campaign Contested block, ring or engagement. `sim.ts` campaign stepping and `project.ts:syncProjects` bypass the legacy commissioning/front system. Existing legacy functions cannot simply be called without changing validators, persistence and active threat integration.

Use explicit security transitions and derived exposed approaches, keeping plant attack eligibility independent of local light/restoration/security. Preserve saved machinery/recruits and physical repair after loss. Extend `truckWork.ts:TruckWork` (target is only source/build) with conserved recovery using existing blueprints/packing rules; current engineer area removal is not automated Line truck recovery. Verify loss/reclaim, commissioning interruption, enclosed approaches, active-plant attacks and truck cargo capacity. The exact commissioning completion/retry policy must be specified in that task; the confirmed direction does not choose its duration or attack concurrency. Do not reinstate the entire old frontier-pressure economy by implication.

### 4. Missing production chains would invalidate later delivery requirements

**Confirmed partial implementation and inadequate task coverage; EX-09C-05 as explicit prerequisite.** Expected §§4–6/17 require ores, Foundry, item crude, Pumpjack/Refinery, fuel/polymer, Mk2 and fast belts. `flow.ts:Kind/CAMPAIGN_KINDS/ITEMS` and `ASSEMBLER_RECIPES` are the active placement/production universe. `recipes.ts:RECIPES` includes Mk2/Fuel/Polymer/Shell data but no corresponding active machine/item paths for most of them. `city/generate.ts` facility names are geography. The generated MACHINE_RECIPES table even still calls Mixer data-only, although `recipeOf(m.kind==='mixer')` and P7 implement it; do not use that stale table as sole proof.

P7 scope report defers Foundry/refining to EX-09/Q07, but broad “wider projects” lacked explicit end-to-end acceptance. Complete the actual chains, unlocks, consumers, connections, diagnostics and accounting before asking Phase 10 deliveries/long-run longevity to depend on them. Revalidate bootstrap. Cannon/Shell and Arsenal already had concrete RI-10 ownership and remain Phase 10 work, not a newly reported release-blocking UI bug. Correct stale active catalogue/generated descriptions during the implementing task without overwriting historical evidence.

### 5. Optional machine rewards and specialist contributions are incomplete

**Confirmed gaps; EX-09C-06 prerequisite.** Expected §9 artifacts attach to actual eligible machines and remain separate from cores. `campaignDiscovery.ts` grants a repair schematic affecting manual repair time; it has no machine attachment/bonus path. No active artifact item/selector/command was found. Existing EX-07 completion proves its original optional reward, not this feature.

`campaignRecruits.ts:RECRUITS` lists Foreman/Lamplighters/Surveyors/Electricians/Concrete crew. Their persistence is reusable. Gunsmith/Rail crew names in old catalogue/generation do not grant active campaign improvements. Substation already exists as baseline; relocking it, direct belt loading or automatic tram service would contradict retained owner instructions. Define useful additional handling/freight contributions and truthful electrical provenance, preserving earned access. A concrete specialist benefit and artifact tuning proposal may require a narrow owner choice; no percentage or replacement reward is approved here. Verify actual changed machine/transport behaviour and saved uniqueness, not an unlock toast alone.

### 6. Tram freight works; engineer tram travel is absent

**Confirmed partial integration; EX-09C-07 before revised campaign validation.** Expected §15 tram travel reduces repeated walking. `fixedTram.ts → flow.ts:tickTram → freight.ts` is active physical four-stop automatic service. W/T confirm second-powered-stop operation, cargo, outages and migration; older two-stop/rail-kit/encounter gates do not govern this path. `truck.ts` and the command union provide truck seating/exit, but no equivalent tram passenger operation or saved seat. The tram's machine motion and freight inventories therefore cannot establish engineer travel.

Add local visible boarding/disembarking and actual passenger position/input handling on the existing service. Verify a useful journey, an unpowered destination and save/load without trapped access or duplication. Reuse existing freight; do not build a second network or add another activation gate.

### 7. Campaign threats do not yet exercise the full confirmed counterplay

**Confirmed partial integration; EX-09C-08 first, RI-10/12 later.** Expected §§8/13 includes darkness-sensitive Shades and major engineering encounters. `threat.ts:236` delegates campaign stepping to `tickCampaignThreat`; `campaignThreat.ts:birth` (101) emits Crawlers only. `tickDiscovery` adds the Stalker. Shared `targetable`/rifle code excludes unlit Shades, but the active campaign does not spawn them. Mechanical light-dependent counterplay is therefore missing despite legacy Shade code. `heart.ts:enableHeart` (104) returns null for exploration-v2; legacy `?heart=1` is explicitly rejected by the campaign session.

Integrate bounded campaign Shade encounters and a first meaningful restoration engineering encounter with paid preparation, automation viability and saved retries, reusing appropriate Heart components. RI-10 specifically owns Breaker/Conductor and Cannon/Shell; RI-12 owns later distinct bosses. Those are correctly scheduled future content, now with explicit active-campaign acceptance. Their old numerical candidates and named bosses do not decide Q07's final threat. Preserve the now-tested roaming/chase behaviour and do not allow unexplained secured-interior spawns.

### 8. Roadmap gates could either hide gaps or deadlock their own reconciliation

**Confirmed documentation defect, corrected in this audit.** Before changes, EX-09 was supposed to resolve Q07 while blocked by Q07; RI-10–12 waited for EX-09, which broadly promised their wider progression. EX-09/RI-11 titles did not concretely own core search, finite plants, artifacts or plant factories. PHASES immediate handoff still named P5/EX-08B; top-level pointers still treated the older GDD as current authority.

The task changes below make implementation dependencies reviewable without waiving prior human gates. No endgame requirement, raid interval, core count or artifact percentage was invented. The audit is complete; the underlying findings remain open implementation tasks.

## Task and document changes

[PROGRESS](../PROGRESS.md) remains the only task/status list:

- EX-09C records this completed audit. EX-09C-01–09 now own the concrete prerequisites above; **EX-09C-01 is next**. Ordering follows the table and its dependencies; 07 depends on plants and 08 on territory, while 09 joins all prerequisite evidence.
- EX-09Q prepares Q07 after foundations and EX-08; EX-09QH is the actual human decision. Neither prerequisite implementation nor decision preparation waits on the decision it is meant to produce.
- RI-10–12 retain their original IDs/history, with explicit campaign acceptance and prerequisites EX-09C-09, EX-08 and EX-09QH, plus their content dependencies. They no longer wait for their own EX-09 assessment. EX-09 now assesses completed wider progression; EX-10 waits for that implementation and validates the full campaign. RI-13 follows EX-10 for Phases 11–14.
- Existing human tasks/statuses and completed evidence were preserved. EX-09C-09 prepares applicable new-build observations rather than treating an old prepared opening as a pass of the revised economy.

Updated [PROGRAMME_STATE](../PROGRAMME_STATE.md), [PHASES](../PHASES.md), [EXPLORATION_DEFENCE_PLAN](../EXPLORATION_DEFENCE_PLAN.md), [RELIGHT-design](../RELIGHT-design.md), [DECISIONS](../DECISIONS.md), [CONSTITUTION](../CONSTITUTION.md), and [CLAUDE](../../CLAUDE.md) with targeted authority/next-action/supersession wording. Old generated GDD blocks and historical completions are retained. The confirmed document and production source remain unchanged. Validation is recorded in the evidence directory and final audit check note below.

## What the player would notice after the proposed fixes

You would gather and build your first powered workshop yourself; find stolen cores by scouting broad areas; choose which plant to restore; and supply each new factory/defence site by the existing freight network. Securing territory would have a visible commissioning step and change exposed approaches. You could ride the tram, recover equipment with the truck, and obtain machine artifacts and specialist upgrades that do something useful. Later production chains and distinct threats would then give Phase 10's deliveries and projects a real economy to use.

Legitimate later scope remains: RI-10 heavy defence/regular enemy expansion, RI-11 wider projects and decided Q07 progression, RI-12 later bosses, EX-10 full campaign; Phase 11 reference-machine scale; Phase 12 remaining art/audio/accessibility/release integration while reusing RI-02B; Phase 13 long-run balance/seeds/migration/packaging; Phase 14 release gates and explicitly authorised external actions. This audit establishes neither fun, scale performance nor release readiness.

Open choices: Q07 final progression/requirements; specific specialist improvement and artifact/commissioning proposals where the confirmed document gives direction but no implementation choice. These do not block EX-09C-01. Unspecified raid intervals/concurrency, core totals and artifact percentages remain unspecified.

## Final audit checks

- [Documentation consistency](../evidence/pre-phase10-audit-2026-09-09/docsync.txt): passed for campaign profile and retained generated tables.
- [Freshness](../evidence/pre-phase10-audit-2026-09-09/freshness.txt): failed with 12 historical stale/unstamped results; retained unchanged. This does not imply current campaign simulation evidence passes.
- [Consistency inventory](../evidence/pre-phase10-audit-2026-09-09/consistency.json): modified-document Markdown targets exist; new/reconciled dependency paths contain no cycle; all 144 captured source/confirmed-design files remain byte-identical. Recorded preview source/assets also still match the prior 26-entry manifest. Existing tests were not edited.
- Current W/T results are bounded as stated above. No full-suite, first-major human pacing, large population or reference-machine performance pass is claimed.
