from pathlib import Path
import json,re
out=Path(__file__).resolve().parent;root=out.parents[2]
read=lambda p:json.loads(p.read_text(encoding='utf-8'))
a=read(out/'analysis.json');c=read(out/'comparison.json');s=read(out/'population/summary.json');m=read(out/'manifest.json')
assert a['complete'] and a['passed']==10000 and s['sourceUnchanged'] and s['failed']==0
assert len(c['repairedSeeds'])==258 and not c['regressions'] and not c['baseGeneratorAttemptChanges']
log=(out/'test-final.log').read_text(encoding='utf-8');assert '# pass 351' in log and '# fail 0' in log
assert '# pass 24' in (out/'focused-final.log').read_text(encoding='utf-8')
classes='\n'.join(f"| {v['class']} | {v['baseline']} | {v['nowPassed']} |" for v in c['classes'])
metrics='\n'.join(f"| {label} | {a['metrics'][key]['min']} | {a['metrics'][key]['median']} | {a['metrics'][key]['p95']} | {a['metrics'][key]['max']} |" for label,key in [('Surveyed route tiles','routeTiles'),('Reachable walking tiles','reachableTiles')])
timings='\n'.join(f"| {label} | {a['timings'][key]['wallMs']['n']} | {a['timings'][key]['wallMs']['median']/1000:.3f} | {a['timings'][key]['wallMs']['p95']/1000:.3f} | {a['timings'][key]['wallMs']['max']/1000:.3f} |" for label,key in [('First sample in worker process','coldProcess'),('Later samples in worker process','warmProcess')])
refs='evidence/p9-04r-2026-09-09/'
report=f"""# P9-04R — composed campaign survey repairs

Recorded 2026-09-09 under the owner's “Ok continue with p9-04r”. **All 10,000 declared seeds now pass the unchanged city validator. All 258 P9-04 failures are repaired, with no regression among its 9,742 passing seeds.** [Paired comparison]({refs}comparison.json), [new population summary]({refs}population/summary.json). The [failed P9-04 baseline](P9_04_POPULATION_REPORT.md) and its original rows remain intact. [PROGRESS](PROGRESS.md) owns status; P9-05 is next for engineering review and human preparation.

## Repair and compatibility

Fresh campaigns use **survey revision 3**, metadata 10, validator version 1 and campaign fingerprint **`ba11e896`**. The base-city generator, original seed and its bounded jitter attempts are unchanged. Selection tries deterministic alternatives on that same terrain; there are no per-seed overrides, replacement seeds or waived validator findings.

The initial route now selects its home and terminal stop footprints together and accepts them only after the later district composition succeeds. Both stages reject ordinary rubble on every stop tile. The district search reserves workshop, sources, route and stops before accepting its endpoint, and then checks the optional cache, all recruits and the Turbine. A failed placement candidate is discarded with its campaign metadata, guardian events and flow revision; unrelated errors still propagate. Existing safety distances, safe recruit traversal, guardian behavior, resource identities and gameplay costs remain intact. Searches follow stable tile/neighbour ordering over the finite city graph, rather than retrying a campaign seed.

Facility placement first tries the preferred urban pad, then the rest of the same district with the same full-footprint rules. All facility tiles must belong to the district itself; a watershed street label cannot masquerade as extractor ground. Coal chooses the nearest distinct district with a usable source footprint. This resolves the thin early-station pad cases as well as later district composition failures. [Exact source delta against P9-04]({refs}source-delta.patch).

Existing revision-1/2 saved surveys retain their recorded layouts, machines, inventory and hashes. They are not regenerated on load. Their historical command logs survive loading and resaving, while `logComplete=false` prevents a false replay claim against the revision-3 fresh factory. Unknown revisions still refuse load. The browser check exposed and repaired the previous session path that discarded an incomplete historical log; a dedicated resave regression covers it. Current revision-3 saves retain complete replay provenance.

## Complete paired population

Seeds **1–10,000 inclusive** were attempted separately with 12 workers, immutable checksummed seed records, the existing runner and the unchanged validator. Each initialized state is queried, queried again and saved/loaded/queried, with repeatability and non-mutation checks. [Manifest]({refs}population/manifest.json), [raw population archive]({refs}population.zip), [integrity check]({refs}population-integrity.log), [distributions]({refs}analysis.json).

| P9-04 failure class | Previously failed seeds | Now passing |
|---|---:|---:|
{classes}

Every seed has base-generator retry metadata, and all 10,000 retry indices agree with the baseline. Among previously initialized maps, {len(c['composedMeasurementChangesAmongPreviouslyInitialized'])} have changed composed measurements and {len(c['routeLengthChangesAmongPreviouslyInitialized'])} have a changed route length; these are fresh-game differences, not changes to stored saves. These measurements do not include exact stop coordinates, so a shifted stop can retain the same route length and measured interaction distances.

| Composed-map metric — 10,000 samples | Min | Median | P95 | Max |
|---|---:|---:|---:|---:|
{metrics}

The full analysis also retains each site's interaction steps, home salvage and source extractor availability. Cardinal connectivity does not establish safe combat passage or human fairness.

| Query-run timing | Samples | Median seconds | P95 seconds | Max seconds |
|---|---:|---:|---:|---:|
{timings}

First-process and later-process samples are separate; these are concurrent measurements, not a cold-OS-cache experiment or reference-machine performance certification. Phase timings and session runtime/CPU metadata remain in the raw records. The population source digest covers its actual sim/harness inputs and remained unchanged for the entire run. The final whole-project source/build snapshot separately includes the browser session resave fix.

## Ordinary commands and browser checks

**12 paid freight/truck probes pass:** seeds 3/4/5, holdouts 8/11/13 and failure representatives 80/88/102/842/2313/3610. Each uses ordinary home stock, walking, paid generators/restorations, earned transport kit, three built stops, five steel delivered by tram, and a physical supply chest funding truck construction. All conserve items, preserve pockets during truck work, save/continue consistently and replay their full command logs exactly. [Driver]({refs}paid-routes.ts), [initial results]({refs}paid.log), [completed seed-842 driver]({refs}paid-final.ts), [seed-842 result]({refs}paid-final/seed-842.json).

Seed 102 now builds its 74-track/three-stop line, delivers freight and completes truck construction in **165 simulation seconds**, without mining the former 300-stone stop obstruction. Its older 639-second failed freight/defence probe remains unchanged in P9-04. This short successful run is not a sustained defence verdict.

Seed 842's longer 114-track route needs **nine additional steel**, mined from existing finite home salvage. The final probe also walks the engineer off the narrow truck approach before automatic work; the truck then travels three route steps and completes the paid job. Earlier driver attempts retained insufficient supply-chest/cargo budgets and an engineer-blocked route. The initial destination search also used pocket affordability where cargo-funded blueprint geometry was required. Their failed saves and diagnostics remain available; no stock, position or threat assistance was added to make the final run pass.

Two additional ordinary visits target the original discovery/shelter failures directly: seed 88 reaches the optional cache with its guardian refusal still active, and seed 842 reaches and recruits the Electricians. Both retain full health, conserve items, continue identically after saving and replay exactly. [Visit results]({refs}visits.log).

**36 powered extraction probes pass:** fresh steel/copper/coal scenarios for the six baseline seeds plus 80/305/1340/7384/8102/9711. Each places a real generator, excavator and output chest, receives at least five produced units, conserves stock and matches save continuation/full replay. [Driver]({refs}extraction.ts), [results]({refs}extraction.log).

**78 campaign experiment checks pass** across E-chain/E-coal/E-tram/E-logistics on 3/4/5. Prior campaign measurement bytes are [archived]({refs}previous-campaign.zip) before promotion. Sim/harness sources match the recorded experiment hashes. The experiment manifest predates the one-line browser session log-preservation fix; its exact previous file is retained and the verification checks that specific delta. Full tests and browser cases separately verify the final session code. [Experiment log]({refs}campaign.log), [promotion record]({refs}campaign-promotion.json).

**Eight browser cases pass** at 1366px and 900px: fresh 102/305/842 and an older paid revision-2 save. Map/world switching and local save/reload preserve state hashes; current saves replay exactly; the old save preserves all **96 historical commands** without claiming new-factory replay. No page errors or document overflow were detected. [Browser record]({refs}browser-result.json), [world screenshot]({refs}world-102.png), [map screenshot]({refs}map-305.png). Screenshots were inspected. Initial browser fixture checks exposed two optional zero flow counters initialized by ordinary session startup; final fixtures use the same `ensureFlow` initialization. A stale pre-rebuild session check and an outdated button selector are retained as driver diagnostics. No human play is inferred.

## Verification and handoff

**351/351 full tests and 24/24 focused tests pass.** Types/build, lint, docsync, freshness, native diff and source/archive/reference checks pass. [Full suite]({refs}test-final.log), [focused]({refs}focused-final.log), [types/build]({refs}typecheck-final.log), [lint]({refs}lint.log), [docsync]({refs}docsync.log), [freshness]({refs}freshness.log), [verification]({refs}verification.json). The existing Vite chunk-size advisory remains. The full suite was repeated after the session log fix; its earlier run remains retained.

The first repair diagnostic completed 253 of the 258 failed seeds, then was stopped after five preferred-pad searches took excessive time. A subsequent wider-pad trial retained four invalid extractor-footprint findings before district ownership was enforced. The final separate **258/258** failure-set rerun and complete **10,000/10,000** comparison pass. These preliminary results are not mixed into the final population. [Diagnostic history]({refs}diagnostics.md).

[Manifest]({refs}manifest.json) archives **{len(m['source_files'])} source inputs and {len(m['build_files'])} production files**, with [source]({refs}source.zip) and [build]({refs}build.zip) archives. P9-04 and earlier P9/P8/EX-08 reports and evidence remain preserved, including old E-variance/preset results and the unrelated pre-existing fixture change. The frozen EX-08D checkpoint remains on port 5180.

**P9-04R is complete; P9-05 is next.** RI-02B remains after P9-05. Human ten-seed/shared play, P6-AMMO retest, reference performance and wider Q07 progression remain separate. No human verdict, commit or push is claimed.
"""
(root/'docs/P9_04R_REPAIR_REPORT.md').write_text(report,encoding='utf-8',newline='\n')
print('P9-04R report written from the completed passing comparison and final test logs.')
