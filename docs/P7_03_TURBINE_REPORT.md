# P7-03 — Turbine and optional discoveries

Date: 2026-09-08. Authority: adopted Q09 / D-EX-35, next-increment request interpreted under D-EX-37. Branch `codex/tram-expansion`, base HEAD `17939145b5d40afe4dcfc1937a3ec6d34b763df6`; uncommitted work. P7-03 is complete. No human play verdict, commit or push.

## Implemented behaviour

- A saved, optional 4×4 riverside Turbine hall accepts **60 steel, 30 copper and 40 concrete** through ordinary local delivery commands. Partial deliveries survive saving and count as committed materials. Commissioning needs existing local power and spends deliveries once.
- The restored hall supplies **600 kW fuel-free** to its local circuit and circuits physically connected through substations and poles. Its output follows actual demand and reduces coal-generator load first. The local E/button switch stops and restarts supply; a disabled circuit core also suppresses generation. Restoration does not claim the district, grant a kit or register an assault base.
- Lamplighters permanently unlock an ordinary paid **Arc lamp**: 1×1, 4 steel + 4 copper, radius 6, 12 kW. It uses existing lighting, power, construction, inspection and repair systems. It remains optional alongside existing lamps and Electricians equipment.
- Surveyors permanently reveal district types on the M map, with colours and a hover legend. The query returns only a district type. It does not discover shelters, recover the schematic or provide attack intelligence; campaign map rendering excludes the legacy facility/survivor markers and pressure tint.
- Campaign metadata **8**, recruit schema **3**, preserves older recruits, construction, deliveries, factory and defence records. New hall placement respects existing machines when upgrading old saves. New records have stable identities, seen/restored/recruited state and validation. Old upgraded command logs retain the existing incomplete-for-current-replay treatment.

All numerical defaults are provisional. Core gameplay lives in `packages/sim`; browser controls submit ordinary commands.

## Acceptance and evidence

| Requirement | Evidence |
|---|---|
| Fair optional placement | [32-seed probe](evidence/p7-03-2026-09-08/placement-probe.log): reachable riverside halls and four separate shelters. A clear walking ring reserves the hall footprint; placement avoids tram routes, source/installation pads, crew sites and guardian origins. Regression checks explicitly cover 3/4/5 and holdouts 8/11/13, route overlap, blocking and save identity. |
| Obtainable paid restoration | [Ordinary workload](evidence/p7-03-2026-09-08/ordinary.log), [script](evidence/p7-03-2026-09-08/ordinary.ts), [complete save/log](evidence/p7-03-2026-09-08/turbine-complete.json). From starting stock, recruit, mine home stone, produce 40 concrete from 80 stone on a paid powered Mixer line, transport it, fuel a paid local generator, commission the hall and build a paid Arc lamp. 35 commands, 204 simulation seconds, conservation and full replay pass. This is scripted execution, not an unaided progression time. |
| Useful connected service | [Network check](evidence/p7-03-2026-09-08/network.log): a real paid 60-kW Mixer load, paid poles connecting the hall circuit to home, increased actual output, unchanged local generator coal while the Turbine carries demand, local switch/save/resume, and removal of the pole chain returning the circuits to isolation. Extra finite construction stock and deferred raids are labelled fixtures, not balance evidence. |
| Optional rewards and orders | Both Lamplighters→Surveyors and Surveyors→Lamplighters sequences use ordinary walking/recruitment and full replay. Paid Arc construction subtracts materials, produces a radius-6 powered light and reports 12 kW; disabled-home power turns it off. No extra base or schematic reward. |
| Save compatibility | [Compatibility probes](evidence/p7-03-2026-09-08/compatibility.log) load the actual P7-02 production save with its machines/recruits/concrete intact. An explicitly positioned old-save pole on the future hall pad survives; the new hall chooses another site once. A separately labelled disabled-core fixture suppresses and restores output. Partial delivery/current saves and malformed records are regression-tested. |
| Ordinary responsive controls | [Browser record](evidence/p7-03-2026-09-08/browser.json), [script](evidence/p7-03-2026-09-08/browser.cjs). Headless Chrome at 1366×900 and 900×900: actual buttons/E in both recruit orders, M map, Turbine delivery/commissioning and switches, paid Arc build-menu placement, F inspection, full replay and Ctrl+S/O hash equality. Logistics use ordinary commands and the complete [pre-commissioning checkpoint](evidence/p7-03-2026-09-08/turbine-pending.json); no gameplay-state edits. Screenshots were inspected; the survey legend was moved below the objective banner and Turbine controls separated from the survivor heading. |

## Verification

Final full regression run: **266/266 passed** (608.3 seconds). Typecheck/build, lint, unchanged legacy snapshot, docsync, freshness and source/build/reference integrity checks passed. The first full run passed 265/266: an old version-2 fixture retained the newly added Turbine record. Corrected that fixture to represent an actual old save; final rerun is recorded separately. The initial focused run also exposed two test-fixture assumptions (retained new metadata and sparse zero inventory); both were corrected, without weakening production validation.

Required records: [final tests](evidence/p7-03-2026-09-08/tests-final.log), [typechecks/build](evidence/p7-03-2026-09-08/build-final.log), [lint](evidence/p7-03-2026-09-08/lint.log), [unchanged legacy snapshot](evidence/p7-03-2026-09-08/snapshot.log), [docsync](evidence/p7-03-2026-09-08/docsync-check.log), [freshness](evidence/p7-03-2026-09-08/freshness.log), [manifest](evidence/p7-03-2026-09-08/manifest.json) and [integrity/reference checks](evidence/p7-03-2026-09-08/consistency.log). Source and production archives plus a P7-02-relative diff capture this increment. Prior P7-02 evidence/report and the frozen port-5177 checkpoint are preserved.

Freshness checks validate configured hashes and ancestry, not all source changes. The P7-02 factory experiments remain historical source-stamped evidence; no new five-hour factory experiment or reference-machine performance result is claimed here. Existing Vite chunk-size advice and previously recorded light-map stalls remain open.

## Handoff

P7-04 discovery/recipe information is next. Broader P7 integration/readiness remains P7-05. Shared play and the owner's ammo retest follow the refreshed EX-08C preparation under D-EX-34. No human gate is completed or waived by these automated results.
