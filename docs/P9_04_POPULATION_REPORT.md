# P9-04 — campaign population and route evidence

Recorded 2026-09-08 under the owner's “Onto P9-04”. **The 10,000-seed measurement and runner work is complete; the city population validation failed.** There are **9,742 passing seeds and 258 failed seeds (2.58%)**. These are measured outcomes under the existing validator, not silently repaired maps or a human fairness verdict. [PROGRESS](PROGRESS.md) owns the follow-up order; [scope](PHASE_9_SCOPE_REPORT.md) defines the task.

## Declared population and resumption

Every integer seed **1–10,000 inclusive** was attempted on the exploration-v2, culdesac-v1 / riverside-v1 campaign, survey revision 2, metadata 10, campaign fingerprint `bd3b8939`, validator version 1. [Population manifest](evidence/p9-04-2026-09-08/population/manifest.json), [summary](evidence/p9-04-2026-09-08/population/summary.json), [compressed raw population](evidence/p9-04-2026-09-08/population.zip), [all distributions/failures](evidence/p9-04-2026-09-08/analysis.json).

The revised `city:validate` runner supports `--resume`, `--workers` (bounded at 16) and `--limit`. The population, full source digest, runtime and format identity must agree. Each seed is written as a checksummed immutable record through a temporary file; failures remain attempted on resume. A process lock prevents two writers, interrupted temporary files are preserved, and resumption checks completed records before skipping them. Source changes or mismatched/corrupted evidence refuse resumption. Derived summaries can be refreshed; seed records, manifests and session histories are not replaced.

The two-seed CLI check stopped after one record and resumed the missing seed. A separate live process was terminated after its first completed seed; resumption recovered the stale lock, preserved that record and completed all six declared seeds ([interruption record](evidence/p9-04-2026-09-08/interruption.json)). The real population first ran 96 seeds with 12 workers, retaining two failures, then resumed the remaining 9,904 under the identical source. [First batch](evidence/p9-04-2026-09-08/population-first.log), [resume](evidence/p9-04-2026-09-08/population-resume.log), [refusal checks](evidence/p9-04-2026-09-08/cli-refusals.json). Every seed gets a fresh campaign, first query, repeated query and saved/loaded query, with report equality and non-mutation checks. Generator exceptions are retained with their original seed and reason.

## Failures and player impact

| Validator class/reason | Affected seeds | First representative |
|---|---:|---:|
| generation-or-run: campaign requires an accessible Electricians shelter | 4 | 842 |
| generation-or-run: campaign route has no clear stop pad | 3 | 2313 |
| generation-or-run: district expansion requires a reachable trunk extension | 2 | 3610 |
| generation-or-run: no clear district facility pad in block # | 15 | 80 |
| generation-or-run: workshop discovery requires a reachable optional side yard | 136 | 88 |
| rail-placement: rubble in the way | 98 | 102 |

160 campaigns fail during initialization, before ordinary commands can start. Each generation failure class reproduces twice on its first representative; no replacement seed or partial state is presented as playable. The remaining findings are retained physical validation failures. [Failure-path analysis](evidence/p9-04-2026-09-08/failure-analysis.md) identifies the existing selection/footprint paths and candidate repairs.

Six ordinary-stock route probes on **3/4/5 and holdouts 8/11/13** pass: paid generator/station restoration, earned track/stop/tram kit, three-stop construction, five steel delivered by freight, physical cargo-paid truck construction, item conservation, save continuation and exact full replay. [Driver](evidence/p9-04-2026-09-08/paid-routes.ts), [results log](evidence/p9-04-2026-09-08/paid-routes.log). They use the existing home stock with active threats, without stock, position or clock injection.

**18/18 additional powered extraction/storage probes pass**, one fresh campaign per seed and steel/copper/coal source. Each builds its generator, excavator and output chest with ordinary stock and walking, receives at least five real produced units, conserves items and replays exactly. [Driver](evidence/p9-04-2026-09-08/extraction.ts), [results](evidence/p9-04-2026-09-08/extraction.log). These short separate workloads do not establish an autonomous factory or sustained defence.

Seed **102** demonstrates the rubble class. A real stop command refuses without consuming stock; the selected footprint contains 300 finite stone. Building track first frees pocket space, then ordinary mining clears the footprint in 305 seconds including handling. The three-stop network and truck delivery complete and replay exactly. **Freight remains zero in the final 639-second scenario**: unattended home and later-station bases have HP 0 and their stops have no power throttle, despite remaining generator fuel. [Diagnostic record](evidence/p9-04-2026-09-08/rubble-findings.json), [full save](evidence/p9-04-2026-09-08/paid-rubble/save-102.json). This is a retained failed freight/defence scenario, not an impassable-stop proof or a defence pass.

The initial probe carried a full tram kit in all 40 pocket slots, so mining made no progress; that state/log is retained and replays exactly. Subsequent driver errors using depot coordinates and trying to store packed track at the depot are also retained. The final probe uses the ordinary track-first order. These corrections change the diagnostic driver, not gameplay or the original population results.

## Distributions and timings

| Composed-map metric | Samples | Min | Median | P95 | Max |
|---|---:|---:|---:|---:|---:|
| Surveyed route tiles | 9840 | 11 | 69 | 113 | 260 |
| Reachable walking tiles | 9840 | 508932 | 535668 | 550318 | 561851 |

The analysis includes each site's cardinal approach distance, home salvage units/reachable tiles by resource, and extractor-footprint availability. Each metric reports its own denominator: failed initialization has no composed-map measurement. Cardinal distance is connectivity, not travel time or a safe route. Quantiles use nearest rank. No legacy frontage/enclosure ratios or hour targets are substituted.

Base-city retry indices are recorded for all 10,000 seeds, including deterministic base-generation diagnostics for campaigns that threw during later composition. Median retry index **0**, P95 **2**, maximum **8**; zero means the first jitter attempt succeeded. Failed composition is not hidden by rerolling the campaign. Exact retry histogram and diagnostic coverage are in the analysis and `retry-metadata` records.

| Query-run timing | Samples | Median seconds | P95 seconds | Max seconds |
|---|---:|---:|---:|---:|
| First sample in worker process | 24 | 7.323 | 8.685 | 9.019 |
| Later samples in worker process | 9976 | 3.525 | 4.785 | 8.740 |

Cold means the first sample in a new worker process, not a cold OS disk cache. Later process samples are reported separately, and generation/first-query/repeat-query/save-load/loaded-query phases each retain their own timing. Runs include concurrent workers and other checks; this is not a paired cache benchmark or reference-machine performance certification. Per-seed time includes generation, copying and invariance checks, never paid gameplay. Session wall times, CPU and runtime are retained in the population session records.

## Verification and handoff

**338/338 full tests pass** (1659.75 seconds under concurrent load), plus **9/9 focused tests**. Types/build, lint, docsync, freshness and native diff checks pass. [Full tests](evidence/p9-04-2026-09-08/test.log), [focused](evidence/p9-04-2026-09-08/focused.log), [types/build](evidence/p9-04-2026-09-08/typecheck.log), [lint](evidence/p9-04-2026-09-08/lint.log), [docsync](evidence/p9-04-2026-09-08/docsync.log), [freshness](evidence/p9-04-2026-09-08/freshness.log). The Vite chunk-size advisory remains.

[204 source inputs and six production files](evidence/p9-04-2026-09-08/manifest.json), [source archive](evidence/p9-04-2026-09-08/source.zip), [build archive](evidence/p9-04-2026-09-08/build.zip), [delta against P9-03](evidence/p9-04-2026-09-08/source-delta.patch), [population integrity](evidence/p9-04-2026-09-08/population-integrity.log) and [verification record](evidence/p9-04-2026-09-08/verification.json) identify the evidence. Simulation and game source, the compiled game and campaign config are unchanged from P9-03. Its browser evidence remains applicable to those identical files; no new UI/browser or human play is claimed here. Prior P9/P8/EX-08 checkpoints, current campaign experiment results, old E-variance and older preset evidence remain preserved.

**Next: P9-04R, repair the demonstrated generation/survey failures before P9-05.** Preserve saved layouts and logs, version new generation behavior, reproduce each failure class with ordinary probes where a campaign can start, and retain a separate complete comparison population. This failed baseline must remain intact. P9-05 engineering/human preparation and RI-02B remain subsequent work; human ten-seed/shared play, P6-AMMO and performance observations remain open. No human approval, phase verdict, commit or push is inferred.
