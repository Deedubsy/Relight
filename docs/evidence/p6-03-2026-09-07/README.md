# P6-03 evidence — 2026-09-07

[Report](../../P6_03_NETWORK_DEFENCE_REPORT.md) supplies conclusions and limitations. [Final measurements](measurements.json) summarize **recorded/campaign/** only: six turret-only seeds (3/4/5/8/11/13), three support seeds (3/4/5), 117 passing checks. Each raw result includes ordinary initial/final saves, full command log, campaign stamp and source hashes. [audit.ts](audit.ts) verifies fresh initial states, current sources, profiles and results; [audit.log](audit.log) records its output.

Reproduce into a new directory ending in `campaign` (existing evidence is never overwritten):

```sh
npm run defence -- --seeds 3,4,5,8,11,13 --out docs/experiments/p6-03-new/campaign
npm run defence -- --seeds 3,4,5 --mode support --out docs/experiments/p6-03-new/campaign
```

[verify.sh](verify.sh) records command output in a new named log and refuses overwrites. Final checks: [suite](suite-recorded.log), [typecheck/build](build-recorded.log), [lint](lint-recorded.log), [legacy snapshot](snapshot-recorded.log), [docsync](docsync.log), [freshness](freshness.log), [survey/save check](survey-check.log), [consistency](consistency.log). Production build retains the existing chunk-size warning.

The directories `pilot-1` through `pilot-4`, `pilot-support`, `pilot-geometry`, `campaign`, `verified`, `final` and `acceptance` are preserved candidate runs with different harness revisions, including explicit failures. Their names do not make them final evidence; **recorded/campaign** is the final source-matched result. Earlier focused/build/time-probe logs record development failures and corrections. No candidate is represented as an independent balance sample.

[check.py](check.py) verifies task order, local references, earlier frozen evidence and final inputs. [build-manifest.json](build-manifest.json) fingerprints source/build, every candidate/result/log and previous evidence. [source.zip](source.zip) and [build.zip](build.zip) freeze the uncommitted implementation separately from the parent commit. The manifest is captured once, with `python check.py --capture`, then verified without that flag. Earlier P6-01/02 checkers describe their own snapshots and are not rerun against changed source.

All workload actions use ordinary stock, reach and commands with live threats. The cut is combined with an explicitly emptied-defence fixture; normal commands pack/refund/rebuild the turrets and remove their feeders. This does not claim a track cut alone exhausts a stocked base. Cargo that can still reach a connected destination may be delivered during a cut. No human verdict, infinite sustainability, reference-machine performance result, commit or push is implied.
