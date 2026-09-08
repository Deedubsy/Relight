# Relight — programme state

Current handoff: 2026-09-08. **Phase 7 catalogue Q09 is adopted; P7-01–05 are complete under D-EX-35–38; EX-08C playtest preparation is next. Shared playtesting follows P7 under D-EX-34.** [PROGRESS](PROGRESS.md) owns task order; [DECISIONS](DECISIONS.md) and the active [GDD](RELIGHT-design.md) own rules. P6-AMMO remains unresolved awaiting the owner's retest and no longer blocks readiness/preparation under D-EX-32.

## Checkout and current build

Branch `codex/tram-expansion`, HEAD `17939145b5d40afe4dcfc1937a3ec6d34b763df6` (`feat: complete Phase 5 factory and campaign discovery`), previously pushed to `https://github.com/Deedubsy/Relight.git` on the matching branch. Integrated main city/Heart improvements remain. The Phase 6 and current Phase 7 work are uncommitted; no new commit, merge or push was performed.

P6-01 implements same-approach staging, physical withdrawal and stronger saved-lifecycle validation. P6-02 implements range previews, explicit known-threat navigation and immediate-action feedback. P6-03 supplies ordinary-stock live-threat workloads and corrects newly generated tram surveys crossing unbuildable streets. P6-04 changes documentation only. At that review all 171 P6-03 source inputs and six production files matched the archive. P7-01–05 now change source/build; that identity is historical. [Review identity and verification](P6_04_READINESS_REPORT.md).

The retained Phase 6 checkpoint is served separately on **port 5177**: [focused defence start](http://127.0.0.1:5177/?rules=exploration-v2&seed=3&view=world&state=/network.json), paused at 39:50 from a 353-command ordinary-stock scripted opening; [fresh construction start](http://127.0.0.1:5177/?rules=exploration-v2&seed=3&view=world&state=/fresh.json), paused at 00:00. The default checkpoint credits earlier play without making the user rebuild the opening; it is not unaided progression evidence. [Guide](EX08B_SESSION_GUIDE.md), [blank record](EX08B_SESSION_RECORD.md), [build/start provenance](EX08B_PREPARATION_REPORT.md). Restart with `python docs/evidence/ex08b-2026-09-07/serve.py`. Port 5176 stays the development preview and 5175 the historical archive. Browser slots are origin-specific. Existing saves keep their profiles/surveys; older defence logs may be incomplete for current-code replay.

Untracked `.serena/` and `docs.zip` predate this work and remain untouched.

## Implementation and owner evidence

Phase 4 is complete. P5-01–05 factory implementation, EX-09B readiness and retained RI-09 technical review are complete. The owner confirmed successful current-build testing of the two-assembler ammunition line, tram/truck transport and defence/recovery. [PHASE_5_PLAYTEST_REPORT.md](PHASE_5_PLAYTEST_REPORT.md) records that result; exact timing, assistance and individual formal closeout positions were not supplied. Credit this evidence without marking unobserved human criteria passed.

P6-00–04 review/implementation/evidence are complete. [P6-01](P6_01_RELIABILITY_REPORT.md), [P6-02](P6_02_INFORMATION_REPORT.md) and [P6-03](P6_03_NETWORK_DEFENCE_REPORT.md) identify their checkpoints and limits. [P6-04](P6_04_READINESS_REPORT.md) maps all nine threat requirements to implementation, validation and remaining observations. Earlier EX/P5 reports remain historical evidence of their original builds, not instructions to repeat superseded tasks.

The owner later reported a direct conveyor failing to load a turret during pre-P6 play. [P6-AMMO](P6_AMMO_INVESTIGATION.md) records 16 passing ordinary-stock loading probes but no reproduction or fix. Under D-EX-32 the owner explicitly asks to proceed and will retest afterward. That supersedes the investigation report's earlier pre-review hold; the original fault, affected save and causal diagnosis remain unresolved.

## Next work and boundaries

1. **P7-00 is complete:** [Phase 7 scope review](PHASE_7_SCOPE_REPORT.md) reconciles current source, legacy facilities/recruits and standards. Fresh opening/discovery/district baseline: 19/19 tests passed; no gameplay source changed.
2. **P7-D / Q09 is adopted:** the owner accepted the Electricians, concrete/Barricade chain, Turbine hall, optional lighting/surveying and discovery/recipe UI catalogue. The reviewed deferred boundaries are also adopted; P7-01–05 are implemented and verified; P7-05 integration review is complete; EX-08C post-P7 playtest preparation is the next runnable increment.
3. **P7-01–05** implement and verify the adopted scope, then **EX-08C** prepares a new shared build/guide/blank record. EX-08B stays the completed pre-P7 checkpoint, with its archives and port 5177 untouched.
4. **EX-08H / P6-H / P7-H / T18** follow EX-08C. Carry the ammo retest and confirmed earlier factory/transport observations forward. No human task is passed or waived by this sequencing change.

Q07 remains open for full project progression/endgame and dependent EX-09/10 work. The existing P6-AMMO investigation resumes from owner retest or an affected save. Existing performance findings and D-SA-2/C3/D-B2-2 positions remain open. Phase 6 technical readiness is not full phase approval.

## Final validation and retained findings

P7-05 technical review passed **271/271 tests**, 12 combined paid reward/raid runs across six seeds and two orders, 64-seed placement/knowledge checks, four real-save migrations, 13/13 supplied-network defence checks and fresh browser controls/save/replay. Build/typecheck/lint, legacy snapshot and docs/archive checks passed; all 181 P7-04 source inputs remain unchanged. [P7-05 review](P7_05_REVIEW_REPORT.md) records surviving home cores at 256.4–296 HP, failed preliminary preparation and a 133.5-ms frame hitch. Phase 7 implementation/automated review are complete; EX-08C preparation is next and human observations remain outstanding.

P7-04 passed **271/271 tests**, build/typechecks/lint, unchanged legacy snapshot, documentation checks and browser controls/save/replay at 1366/900 widths. [P7-04 report](P7_04_INFORMATION_REPORT.md) records saved installation knowledge (metadata 9), the known-site journal/map and truthful nine-item recipe/use guide. All 80 live paid lamp/pole edits succeeded with the guide open; the compact powered sample reached a 300.2-ms frame interval. These measurements do not close performance acceptance. EX-08C playtest preparation is next.

P7-03 passed **266/266 tests**, typecheck/build/lint, unchanged legacy snapshot, 32-seed placement, ordinary-stock concrete-to-Turbine full replay, connected generation/isolation, old-save migration and browser controls/save/replay at 1366/900 widths. [P7-03 report](P7_03_TURBINE_REPORT.md) records the 600-kW fuel-free hall, paid Arc lamps, district-only survey, metadata-8 compatibility and provisional defaults. Source/build archives and documentation integrity checks identify this checkpoint. EX-08C playtest preparation is next; shared play remains after P7.

P7-02 passed 262/262 tests plus 19 final focused checks, build/typechecks/lint, unchanged legacy snapshot, docs and archive verification. Browser checks at 1366/900 widths cover local Concrete recruitment, paid Mixer/Barricade construction, a real stone-conveyor production line, recipe inspection and save/full replay. [P7-02 report](P7_02_CONCRETE_REPORT.md) records provisional tuning, two-shelter seed coverage and version-7 compatibility.

The earlier P7-01 checkpoint passed 255/255 tests, package typechecks/production build, lint, legacy snapshot and documentation checks. Browser button/E recruitment, paid equipment, replay and Ctrl+S/O passed at 1366/900 widths. [P7-01 report](P7_01_DISCOVERIES_REPORT.md) records stable recruits, old earned-unlock migration and 32-seed reachability. These checks do not supply human play or reference-machine performance acceptance.

EX-08B freshly passed **51 focused tests**, both paused/save-loaded starts at 1366×900 and 900×900, a natural dawn warning with G/K state preservation, four five-second live lamp/pole edit samples and documentation/artifact checks. Final source is unchanged. The compact fresh-start sample still reached **116.8 ms frame interval / 124.5 ms light paint**; all 80 sampled edits succeeded. This is measured local headless Chrome behaviour, not reference-machine certification. Earlier EX-08A documents and archives are preserved; the B guide/record are new files.

P6-04 freshly reran **250/250 tests**, all-package typechecks/production build, lint, the unchanged legacy three-hour snapshot and docs/freshness checks. Source/build/evidence hashes, local references and task consistency passed. The nine P6-03 workloads' **117/117 checks**, sixteen ammo probes and P6-02 browser/performance measurements are retained source-matched evidence, not new reruns in P6-04.

Major assaults held in all nine workload runs and lasted 244–247 seconds. The scripted policy nevertheless returned 111 seconds late on seed 8 and had only 3 seconds spare on seed 4. Manual mining/transfers/fuel service, early core damage, endpoint freight gaps and limited ammunition economy from personal support remain observations to assess, not excuses to silently retune. The recovery drill deliberately removes feeders and replaces turrets empty through ordinary commands; it does not claim a track cut alone exhausts a stocked base. Runs end before the second major dusk and do not prove indefinite factory autonomy. Fresh seed-8 survey generation is fixed; older saved surveys are not automatically rewritten.

Performance remains open: earlier larger-workload light-mask paints averaged about 24 ms, with frame intervals reaching 116.8/150.1 ms. P6-02's smaller live lamp tests averaged 16.1/14.1 ms paints with a 31.6 ms setup maximum. Those different workloads do not close the larger stalls or reference-laptop/GPU gap. The existing Vite large-chunk advisory remains. No fresh browser/human session or reference-machine result is claimed by P6-04.
