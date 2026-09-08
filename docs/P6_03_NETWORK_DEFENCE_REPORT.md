# P6-03 — supplied network defence evidence

Date: 2026-09-07. Authority: D-EX-31, owner request “Nice! Move onto P6-03”. This completes the bounded evidence task in the [Phase 6 scope review](PHASE_6_SCOPE_REPORT.md). P6-04 engineering readiness is next; human Phase 6 acceptance remains open.

## Workload and changes

The new [defence harness](../packages/harness/src/defenceScenario.ts) starts a fresh campaign with its ordinary 200 steel, 100 copper, 50 stone, 40 stored coal, 40 coal already in the generator and 20 magazines. It uses paid construction, actual reach, hand mining, a home ammunition assembler, persistent coal extraction, generators, restored radio and two stations, six supplied turrets, and a three-stop tram line. Threats run throughout. No inventory, position, clock, HP or schedule injection is used. Initial saves, full ordinary command logs, final states, config and source hashes accompany each run.

This is a scripted maintenance policy, not an unattended whole factory. The engineer mines steel/copper, refuels generators and transfers ammunition between production/platforms and local feeder chests. Inter-base ammunition and repair goods travel by tram. Minor defence is unattended when the engineer stays more than 20 tiles away throughout the raid. Time away means outside the home neighbourhood, not simply outside its central court. Command counts include movement, construction, configuration, mining, transfers and aim; they are not measured human clicks.

Seeds 3/4/5 are the repeatable workload; 8/11/13 are declared geometry holdouts. Seed 8 exposed a real survey defect: extension search could choose a street tile adjoining an INERT block, where normal construction is forbidden. [District generation](../packages/sim/src/campaignDistricts.ts) now selects only buildable DARK/HELD neighbourhood streets. A paid normal-stock seed-8 three-stop regression and 416 extension-tile checks across seeds 1–32 pass. A saved pre-fix seed-8 campaign keeps its existing survey/hash and identical continuation; this change does not rewrite old surveys. Such an old survey may still require a manual reroute. Seed 8 is now a regression case, not untouched statistical validation.

No damage, roster, timing, repair price, weapon or economy tuning was made.

## Recorded outcomes

The final [recorded campaign results](evidence/p6-03-2026-09-07/recorded/campaign/) contain nine runs and **117/117 checks**. Each holds the finite 60-attacker major, handles at least one supplied remote minor without personal fighting, earns the real three-base resupply milestone, recovers a disabled outpost using shipped repair stock, conserves resources, and matches saved and full command replay. Five intermediate checkpoints also verify conservation, saved continuation and interval replay.

| Seed / mode | Network ready (s) | Run (s) | Major (s) | Observed turret / rifle shots | Core HP | Final preparation lead (s) |
|---|---:|---:|---:|---:|---:|---:|
| 3 / turrets | 1776 | 5633 | 245 | 180 / 0 | 300.0 | 63 |
| 4 / turrets | 1611 | 5690 | 244 | 180 / 0 | 300.0 | 3 |
| 5 / turrets | 1816 | 5629 | 247 | 180 / 0 | 267.6 | 100 |
| 8 / turrets | 1517 | 5317 | 246 | 102 / 0 | 81.2 | -111 |
| 11 / turrets | 1882 | 5636 | 245 | 180 / 0 | 201.6 | 58 |
| 13 / turrets | 1503 | 5639 | 244 | 180 / 0 | 300.0 | 99 |
| 3 / support | 1776 | 5633 | 244 | 174 / 12 | 300.0 | 31 |
| 4 / support | 1611 | 5690 | 244 | 180 / 0 | 300.0 | 3 |
| 5 / support | 1816 | 5647 | 246 | 80 / 150 | 267.6 | 100 |

Major duration is actual assault history end minus start, not time since the observer arrived. Turret shots in the table cover the recorded observation window. **Seed 8 starts observation 111 seconds late**, so its 102 observed shots are not the full assault total. The separate total-run shot count includes all defence. Core HP is the value at major completion and includes damage from earlier minors; it must not be described as damage caused solely by the major. All majors start at 3300 seconds and leave the next dawn at 4800 seconds. All runs finish before the next dusk at 5700 seconds; they do not prove indefinite survival.

The received warning supports an initial 12–17 second return trip, leaving 857–873 seconds before dusk. `warningResponse` is when the policy notices the received warning after a service cycle, not when the radio first received it. Continuing maintenance afterward is the limiting factor: seed 4 finishes preparation with only 3 seconds to spare and seed 8 returns 111 seconds late. The 244–247 second major durations fall within the provisional 3–5 minute diagnostic target without tuning.

Personal support stands at the known target, takes up to 15 real magazines and aims only at a major attacker within rifle range. Seed 3 uses 174 turret shots plus 12 rifle shots versus 180 turret shots alone, finishing one second earlier. Seed 4 has no firing opportunity under this placement/policy. Seed 5 uses 80 turret shots plus 150 rifle shots versus 180 turret shots alone, also one second earlier. These runs demonstrate a contribution, not lower total ammunition consumption or a human assessment of combat usefulness.

## Supply interruption and recovery

After the major, the policy increases the final stop's request to 200 magazines, 25 steel and 15 copper, stages a track pickup near home with real cargo aboard, and later repairs that segment. On seeds 3/4/8/11 cargo remains reserved aboard the disconnected tram. On seeds 5/13 the loaded tram can still reach its destination on the connected side and legitimately unloads; the broken connection prevents the complete home supply loop until repaired. The check requires retained cargo plus actual deliveries to equal cargo at the cut, then a positive delivery increase after repair. It does not require all cargo to remain aboard when delivery is still physically possible. Global conservation and full replay independently check item accounting.

**The exhaustion drill is deliberate and additional to the track cut.** Through normal commands, the engineer removes the final outpost's feeder arms, repairs any damaged turret before packing it, packs/replaces the turrets empty and stores refunded magazines in the local chest. A real minor raid then disables that core while the engineer is away. This is not evidence that the cut alone naturally exhausts a fully stocked base. Remaining generators receive ordinary fuel trips during the interruption.

After replacing the track, the engineer withdraws exactly **10 steel + 5 copper** from actual previously shipped outpost cargo and performs the paid 12-second core repair, restores feeding arms, resumes deliveries and repairs remaining damage. The final core reaches 300 HP in every run. Recorded recovery spending includes all paid damage repairs in this interval, including preparation to pack damaged turrets; it is not an alternative price for a single core repair. Construction/refunds are separately represented by the full ledger.

The final 200-magazine request deliberately exceeds stocked demand; the remaining request gaps are reported below, rather than treating restored throughput as all demand satisfied. These are endpoint platform-stock gaps, excluding any in-flight reservations, and are not integrated shortage durations. One finite run does not establish sustainable long-term material extraction or self-sufficient factory throughput.

| Seed / mode | Unattended remote holds | Away (s) | Commands / service cycles | Total turret shots | Magazines made | Coal burned | Repair steel / copper |
|---|---:|---:|---:|---:|---:|---:|---:|
| 3 / turrets | 3 | 1021.0 | 524 / 24 | 453 | 438 | 728 | 16 / 8 |
| 4 / turrets | 3 | 1163.0 | 570 / 21 | 455 | 442 | 729 | 22 / 11 |
| 5 / turrets | 3 | 1085.0 | 542 / 21 | 447 | 440 | 726 | 16 / 8 |
| 8 / turrets | 4 | 801.0 | 530 / 30 | 423 | 413 | 677 | 14 / 7 |
| 11 / turrets | 4 | 1144.0 | 552 / 21 | 444 | 438 | 725 | 14 / 7 |
| 13 / turrets | 1 | 963.0 | 541 / 23 | 453 | 443 | 722 | 18 / 9 |
| 3 / support | 3 | 1007.0 | 773 / 24 | 447 | 453 | 728 | 16 / 8 |
| 4 / support | 3 | 1163.0 | 815 / 21 | 455 | 457 | 729 | 22 / 11 |
| 5 / support | 3 | 1084.0 | 788 / 21 | 347 | 455 | 724 | 16 / 8 |

| Seed / mode | Loaded at cut | Retained / delivered during cut | Deliveries after repair | Final north request gap: magazines / steel / copper |
|---|---:|---:|---:|---:|
| 3 / turrets | 95 | 95 / 0 | 195 | 110 / 20 / 10 |
| 4 / turrets | 107 | 107 / 0 | 187 | 120 / 20 / 10 |
| 5 / turrets | 95 | 0 / 95 | 100 | 30 / 10 / 5 |
| 8 / turrets | 115 | 115 / 0 | 195 | 120 / 20 / 10 |
| 11 / turrets | 115 | 115 / 0 | 195 | 120 / 20 / 10 |
| 13 / turrets | 95 | 0 / 95 | 100 | 48 / 10 / 5 |
| 3 / support | 95 | 95 / 0 | 195 | 110 / 20 / 10 |
| 4 / support | 107 | 107 / 0 | 187 | 120 / 20 / 10 |
| 5 / support | 95 | 0 / 95 | 100 | 26 / 10 / 5 |

The raw [measurement summary](evidence/p6-03-2026-09-07/measurements.json) also records production consumption, net construction materials, site/repair spending, service durations, command-type counts, remote-raid rounds and exact cargo accounting. Starting stock plus measured mining funds the recorded consumption. Several early home/remote raids damage cores before later resupply; at least one remote supplied raid succeeds unattended per run, not every raid without damage.

## Failures and bottlenecks retained

All earlier candidates and logs remain in the [evidence directory](evidence/p6-03-2026-09-07/). They are not final acceptance results:

- Early service policies exhausted steel or finite home coal. Persistent paid coal extraction, refuelling before each generator visit and a 40-steel/20-copper repair reserve corrected the harness policy; no resources were granted.
- Half-tile target coordinates broke the driver's integer approach scan. The navigation adapter now approaches the known target's containing tile.
- Seed 4 showed that damaged turrets cannot be packed without repair and that repair stock must remain available. Both requirements are now paid parts of the drill.
- Seed 8's illegal surveyed tile prevented normal rail construction. The simulation generation fix and old-save limitation are described above.
- An earlier cut was reached after unloading, leaving no cargo to test. Staging near home ensures a loaded cut. Subsequent seeds 5/13 showed valid delivery on the remaining connected segment, exposing an overly strict retention assertion. Final accounting includes both delivered and retained cargo; earlier failed assertions are retained, not overwritten.
- The first time-away regression confused the court edge with the home neighbourhood boundary; it now walks into an actual neighbouring region. The paid seed-8 rail fixture also needed sufficient ordinarily obtained generator fuel.

Maintenance remains substantial: roughly 17 minutes of initial hand mining, 25–31 minutes to the serviced network checkpoint and hundreds of scripted commands per run. Preparation deadlines, early damage, manual transfers, final freight deficits and limited support benefit need assessment in P6-04 and focused human play. No difficulty or economy change is inferred from these observations.

## Verification and handoff

**250/250 regression tests** pass, including observer/ordinary-driver equivalence, fractional elapsed-time accounting and paid seed-8 transport replay. All-package typechecks/production build, lint, unchanged legacy snapshot, documentation profiles, freshness, local references, task status and source/build/history integrity pass. The [audit](evidence/p6-03-2026-09-07/audit.ts) independently checks nine fresh initial states, campaign/config profiles, matching final source hashes and all 117 workload assertions. Previous P6-02 browser evidence remains historical; no new browser or human session is claimed for this harness and survey change. Existing Vite chunk-size and light/power/reference-machine performance findings remain open.

The [manifest](evidence/p6-03-2026-09-07/build-manifest.json), [source archive](evidence/p6-03-2026-09-07/source.zip) and [build archive](evidence/p6-03-2026-09-07/build.zip) identify this uncommitted source independently of parent commit `17939145b5d40afe4dcfc1937a3ec6d34b763df6` on `codex/tram-expansion`. Reload the existing port-5176 preview to use the rebuilt game. Historical port 5175 and earlier evidence remain unchanged. No commit or push performed.

**P6-04 is the next bounded task:** review engineering readiness and remaining human/performance findings. EX-08B subsequently prepares the shared follow-up build. P6-H, EX-08H and wider Q07 progression retain their dependencies; this evidence does not close those gates.
