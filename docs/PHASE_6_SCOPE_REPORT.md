# Phase 6 — threat and defence scope review

Date: 2026-09-07. Reviewed branch `codex/tram-expansion`, HEAD `17939145b5d40afe4dcfc1937a3ec6d34b763df6`, published to `Deedubsy/Relight`. Authority: D-EX-28, owner request “OK start the phase 6 review”. This review defines remaining work; gameplay and tuning are unchanged.

## Assessment

Phase 6 is completion and hardening of an existing defence loop. EX-05–07 already deliver the adopted three threat layers, shared schedule, radio, wall/turret/core damage, workshop service and personal repair tool. Rebuilding those systems or automatically adding the old RI-10 enemy roster would duplicate work or expand scope.

The first implementation task is **P6-01: attack approach and lifecycle reliability**. A fresh review probe reproduced a locked-origin stall: after dawn selects an origin, placing one paid wall on that tile prevents every major attacker from spawning. After 240 seconds beyond dusk the wall remains undamaged, zero of 60 attackers have spawned, and the assault stays active. Saving and continuing reproduces the same state. The earlier EX-05 report explicitly retained obstructed-origin waiting; this review identifies it as unfinished Phase 6 work, not a newly introduced regression.

The owner [confirmed successful current-build gameplay](PHASE_5_PLAYTEST_REPORT.md), including the two-assembler ammunition line, tram/truck transport and defence/recovery. That is positive human evidence. It does not supply measured warning-to-arrival time, remote-base autonomy, maintenance burden, or every formal exit criterion. Follow-up should target those gaps and changed behaviour rather than discard or repeat the confirmed result.

## Contract and reuse map

The controlling requirements are [Phase 6](PHASES.md#phase-6--threat-complete), active GDD §§5/7/13/19, Q01/02/03/06 and the current [standards](STANDARDS.md). The source paths below identify inspected implementations; historical reports retain their original dates, build identities and limitations.

| Requirement | Current implementation and evidence | Remaining acceptance |
|---|---|---|
| Site creatures stay local; optional encounter has counterplay | `campaignDefence.ts:initDefence`, `campaignThreat.ts:tickCampaignThreat`, `campaignDiscovery.ts`; ordinary ruin guards plus the leashed, telegraphed Stalker and once-only schematic. [EX-07](CAMPAIGN_DISCOVERY_REPORT.md) covers rifle, turret, dodge, living distraction and essential-route bypass. | Credit existing mechanics. Extend geometry checks to changed layouts and encounter/defence interactions; do not require an unapproved new enemy. |
| Two shared minor opportunities; supplied bases normally handle raids unattended | `campaignThreat.ts:tickCampaignSchedule` selects one base per opportunity, spawns 8–12 crawlers, discards occupied slots, suppresses raids during major combat and its recovery exclusion. Existing tests verify bounded scheduling. | A normally supplied outpost handling actual raids while the engineer is away is not established by scheduler fixtures or the assisted factory soak. Measure supply use and failures. |
| One locked major target, restoration arbitration and full rest intervals | `campaignThreat.ts` locks at dawn, coalesces nominations, starts a finite roster at dusk, records actual completion/withdrawal and derives the next window. `campaignDistricts.ts` earns one-cycle rest through real reserved deliveries. | Obstructed origins/exits and a minor still withdrawing at dusk need adversarial lifecycle coverage. Cover restorations before/after lock, ineligible cores and saves at transitions across all three bases. Preserve promised windows and target identity. |
| Valid origins and physical paths | `campaignOrigin`, `field`, `route`, `walkField` select reachable ground, prefer open routes and breach paid defences. Existing test seals the home mouth and observes a breach. | Fresh geometry passes 96 base/seed probes, but player construction after lock can prevent spawning. Ensure deterministic progress or an explicit recoverable deferral, without invalid spawns, erased rosters or target rerolls. |
| State-grounded radio and useful preparation lead | `radioPowered`, `receiveWarning`, `campaignWarning`, `restorationWindow`; restored radio permanently unlocks network eligibility; received warnings survive outages; upgrade reports approach/composition. | Retain current text. Add actionable navigation and off-screen direction using only known information; confirm no target/approach leak before unlock or through an outage. Observe actual return/preparation time. |
| Usable walls/turrets and protected production/cargo | `campaignDefence.ts` and `campaignThreat.ts`; wall breach, 100-HP turrets, disabled cores, paid recovery; `threat.ts:tickWeapons` reuses real ammunition. [EX-05](CAMPAIGN_DEFENCE_REPORT.md) and current tests cover retained layouts/stock and a full 60-crawler defence with ordinary purchased turrets. | Carry intact machinery/cargo, damage and conservation through network stress runs. Full-roster test is hand-fed and clock-advanced, not an automated supply-network result. |
| Personal fighting, repair and workshop support | `campaignDefence.ts` paid paused repair jobs; `campaignDistricts.ts` powered/material-consuming workshop, paused during major assaults and local raids; `campaignDiscovery.ts` field tool. | Retain current equipment and recovery costs as provisional values. Measure repair workload, short supply, disabled-base recovery and route repair with threats active. |
| Defence readability, standards 3.3/3.4/B.5–B.9 | `worldScene.ts` shows HP/damage/ammo states and enemy hover/target lines; `panel.ts` displays sim warning text. | Turret placement/selection range rings and actionable threat navigation are missing. Campaign hover target reports the strategic core even while a crawler damages a nearby turret or breaches a wall. Expose the immediate action and strategic destination distinctly. |
| Save/replay and protected legacy profile | `defenceValidation.ts`, existing save/session plumbing; tests cover repairs, warnings, guardian wind-up, old-preview initialization and command replay. | Extend existing validation to lifecycle changes, validate active attack/body consistency and prevent duplicated/dropped rosters across blocked-origin, withdrawal and resupply transitions. Keep legacy behaviour unchanged. |

Source references: [threat controller](../packages/sim/src/campaignThreat.ts), [defence and repair](../packages/sim/src/campaignDefence.ts), [district service and milestone](../packages/sim/src/campaignDistricts.ts), [discovery](../packages/sim/src/campaignDiscovery.ts), [save validation](../packages/sim/src/defenceValidation.ts), [weapons and hover targets](../packages/sim/src/threat.ts), [world rendering](../packages/game/src/worldScene.ts), [panel](../packages/game/src/panel.ts), [goal text](../packages/sim/src/goal.ts).

## Remaining increments

These definitions supply acceptance; [PROGRESS](PROGRESS.md) alone supplies status and execution order. The present authorisation is the review, not implementation of all increments.

### P6-01 — attack approach and lifecycle reliability

Resolve construction on a locked spawn tile and validate physical withdrawal. Prefer a deterministic reachable staging/approach fallback that preserves the locked base and truthful warning direction; specify the fallback before coding. If no valid approach exists, represent a recoverable deferral explicitly and preserve roster accounting. Do not teleport through geometry or remove attackers merely to finish a test.

Acceptance: a normally paid wall or protected machine placed on the locked origin cannot hold the scheduler active indefinitely without a recoverable transition. Test removal/restoration of the obstruction, a blocked retreat, minor withdrawal at dusk, disabled/recommissioned cores, pre/post-lock nominations and the earned one-cycle milestone. Save/load at each transition must preserve target, roster, warnings, paid jobs and rest interval with no duplicate attack. Cover home and both stations, with seeds 3/4/5 plus geometry holdouts 8/11/13; retain a broader creation/origin sweep. Existing approved clock/roster settings are not silently retuned.

### P6-02 — defence information and navigation

Draw turret range on placement and selected inspection from the same sim constant used by weapon targeting. Add a keyboard-accessible action to view a known threatened base and an off-screen direction indicator; do not move the engineer or interrupt construction automatically. Distinguish minor raid, warning, active assault, withdrawal and disabled-core recovery. Preserve received intelligence through outages without exposing a new hidden target.

Expose an enemy's immediate attack/breach/withdrawal action alongside its strategic destination. Reuse existing HP, ammo, damage and repair display. Verify actual controls and readability at 1366×900 and 900×900, save/load, radio-offline states and a remote attack while building/travelling. Text/shapes must work without audio or colour alone. Retained standards 3.3/3.4/B.5/B.7 map here; their historical missing cells are not separate implementation tasks.

### P6-03 — supplied network defence evidence

Extend the current harness with a campaign workload using ordinary starting stock, reach, commands and live threat timing. Build on P5 blueprint/logistics and EX-05–07 combat; do not relabel the assisted five-hour soak. Use seeds 3/4/5 for repeatable runs and 8/11/13 as declared holdouts. A finite assisted construction fixture may isolate a failure, but remains separate from the normal-start run.

Demonstrate an unattended supplied-base minor defence while the engineer is elsewhere, an actionable major warning followed by travel/preparation and defence, real home-to-outpost ammunition/repair delivery, and a deliberately interrupted route followed by recovery. Exercise the earned third-base rest milestone without advancing a promised window. Record actual duration, ammunition, material consumption, core/defence damage, manual interventions, freight shortfalls and time away from production. Compare turret-only preparedness with a declared personal-support run where useful. Report failures and bottlenecks before proposing tuning; 3–5 minutes is the adopted initial major-duration target, not a reason to alter measurements. Require conservation and saved/full replay continuation.

### P6-04 — engineering readiness and focused follow-up scope

Review P6-01–03 evidence against every row above, run required integration checks on the final code, and list any remaining failures. Carry normal light/power-change responsiveness and the reference-machine performance gap forward. Define the focused human observations still needed, crediting the owner's successful Phase 5 session. EX-08B owns the subsequent shared source/build/start freeze and guide refresh; do not prepare duplicate or retrospective sessions.

### P6-H — human Phase 6 observations and verdict

On the refreshed build, record warning comprehension, time to return and prepare, unattended minor-defence experience, personal contribution, recovery and recurring maintenance burden. This may share EX-08H's representative session; reuse actual observations and state missing items explicitly. Record the owner's Phase 6 verdict separately from automation. Retain remaining Phase 5 closeout decisions without inventing their approval. Timing remains diagnostic unless a scored threshold is agreed before play.

## Retained and deferred scope

- **RI-10:** its useful campaign wall/turret damage, origins and enemy readability map to EX-05/07 and P6-01/02/03. D-B4-3's unharmed waypoint is a legacy limitation; current campaign turret damage is already required by decided Q06. Do not reintroduce that limitation or demand approval again. The legacy profile remains unchanged.
- **Breaker, Conductor, Cannon/Shell and Arsenal:** historical RI-10 candidates, not requirements automatically unlocked by this review. The active first kit is wall/gun turret/rifle plus workshop/field repair. Additional archetypes, heavy equipment and counterplay need the wider progression reconciliation; Conductor-linked blooms conflict with the retired frontier model.
- **Heart and later bosses:** reuse remains possible under EX-09/RI-12 after their revised encounter/progression scope is defined. They are not prerequisites for completing the current three-layer defence loop.
- **Q07:** still owns full project progression, endgame kit and campaign length. It does not block the current-loop fixes, feedback or defence evidence defined here. No new enemy, difficulty curve, day length, loot or daylight combat mechanic is adopted.
- **EX-08B/H, EX-08 and T18:** the confirmed owner play result remains valid. The old frozen EX-08A protocol was not followed retroactively. Refresh preparation after the planned threat changes and share follow-up evidence where criteria overlap. Formal gaps remain explicit rather than blocking independent engineering review.
- **Performance:** light-map repaint stalls and reference-laptop/GPU validation remain open. The owner reported successful play but supplied no timing measurements that close those findings. Normal interaction checks accompany changed UI; wider scale/release obligations remain EX-10.

## Verification and provenance

Fresh review checks on HEAD `1793914`:

- **25/25 existing tests passed:** campaign defence (10), discovery (9), districts (6), through WSL Ubuntu-24.04 / Node 22.18.0. [Raw log](evidence/phase6-review-2026-09-07/focused.log).
- **96/96 origin checks passed:** three registered base positions on each seed 1–32. Registry restoration is injected solely to query geometry; this is not paid progression, path-change resilience or combat balance evidence.
- **Locked-origin stall reproduced:** the review-only [probe](evidence/phase6-review-2026-09-07/approach-probe.ts) uses normal chest withdrawal and paid wall placement, but explicitly relocates the engineer, advances the clock and suppresses unrelated threats. At 240 seconds beyond dusk: zero spawned, 60 remaining, wall 120 HP, matching saved continuation. [Raw result](evidence/phase6-review-2026-09-07/approach-probe.log). This is a confirmed open finding, not an acceptance pass.
- Documentation profile, freshness, links, task consistency and retained-evidence integrity checks are recorded in [the review evidence directory](evidence/phase6-review-2026-09-07/). Production code/build and 352 previously protected files are unchanged.

The previous P5 full-suite/build/lint results remain historical evidence in [P5-05](P5_05_INTEGRATION_REPORT.md); no fresh full suite, build, lint, browser or campaign-duration experiment was required or claimed for this review. Implementation of P6-01–04 and human Phase 6 exit remain outstanding. No commit, merge or push performed.
