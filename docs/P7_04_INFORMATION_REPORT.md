# P7-04 — Discovery and recipe information

Date: 2026-09-08. Authority: adopted Q09 / D-EX-38, owner request “OK move onto P7-04”. Branch `codex/tram-expansion`, base HEAD `17939145b5d40afe4dcfc1937a3ec6d34b763df6`. P7-04 is complete; work remains uncommitted. No human verdict, commit or push.

## Implemented behaviour

- **Discoveries** shows remembered installations, discovered/recruited shelters, the known Turbine and workshop records. Cards report partial deliveries and remaining materials, permanent capabilities and actual powered, generating, switched-off or workshop service state. Local buttons submit the existing ordinary delivery, restoration, recruitment and switch commands.
- Fresh campaigns retain the opening station clue. Approaching an installation within a provisional **24 tiles**, restoring it, or restoring the preceding route installation remembers its location. Recruits, the Turbine and workshop records retain their own exploration rules. Known locations receive map markers and hover information; world labels respect knowledge. The journal does not query hidden attack targets or turn Surveyors into a discovery checklist.
- Campaign metadata **9** saves installation knowledge. Older saves retain the four installation locations their previous panels already exposed, alongside earned recruits, machines, supplies and restoration history. Invalid knowledge is rejected. Older migrated logs retain their existing replay limitations.
- **Items and recipes** covers steel, copper, stone, coal, magazines, wire, frames, boards and concrete. It derives implemented recipe quantities, yield, duration and power from simulation definitions, names sources and unlock provenance, and lists real consumers and transport. Known restoration costs appear only after their sites are known. Frames and Boards explicitly have no implemented consumer; production is optional. Magazines distinguish one crafted magazine from its ten rounds.
- The guide identifies Concrete crew recruitment as the Mixer unlock and Foreman blueprints/copy-paste as unavailable until Phase 8. Obsolete campaign block-claim interaction and Rifle Mk2 prompts are removed. Existing legacy behaviour remains separate.
- Controls retain DOM identity and keyboard focus while facts update. Native details/select controls support keyboard use; Escape closes the guide. Read-only browsing leaves simulation state unchanged. Gameplay remains in `packages/sim`.

## Validation

**271/271 regression tests passed** in 405.2 seconds. All-package typechecks/production build, lint, unchanged legacy snapshot, both-profile docsync and freshness checks passed. The reference generator was typechecked again after its metadata/guide update.

[Five focused tests](evidence/p7-04-2026-09-08/focused.log) cover remembered exploration, malformed knowledge, actual item recipes/consumers, read-only queries, real P7-03 save migration, service/switch state, partial delivery information and ordinary-command replay. Position/partial-stock fixtures are explicitly labelled. A fresh current-code replay of the prior 35-command paid concrete/Turbine workload creates the [current checkpoint](evidence/p7-04-2026-09-08/guide-checkpoint.json); it does not edit gameplay state or claim unaided progression.

[Browser checks](evidence/p7-04-2026-09-08/browser.json), driven by the [recorded script](evidence/p7-04-2026-09-08/browser.cjs), passed at **1366×900 and 900×900** in headless Chrome. They exercise all nine item selections, locked/earned Concrete information, ordinary travel and recruitment, known map information, actual Turbine off/on buttons, paused browsing hash equality, full replay and Ctrl+S/O save equality. No page errors or horizontal overflow were observed. Screenshots were inspected at both sizes, including the [compact recipe](evidence/p7-04-2026-09-08/locked-recipe-900.png), [known map](evidence/p7-04-2026-09-08/known-map-900.png) and [service panel](evidence/p7-04-2026-09-08/known-services-1366.png).

Two initial browser assertions were test assumptions: the opening had already revealed the nearby Electricians, and a fresh game ran at 1× while a hash comparison assumed pause. The script now compares against saved discovered recruits and explicitly pauses before read-only comparisons. Neither required changing game behaviour. The retained `failure.txt`/`failure.png` describe the second unsuccessful attempt, not the final run.

## Live responsiveness and limits

Four five-second 1× samples kept the item guide open while ordinary paid lamp/pole helpers repeatedly removed and replaced equipment. Each sample made **20 successful edits**, advanced five simulation seconds and repainted light nine times. Headless Chrome on this host is an observation, not reference-machine performance acceptance.

| Start / width | Mean frame interval | p95 | Maximum frame interval | Mean light paint |
|---|---:|---:|---:|---:|
| Fresh / 1366 | 16.73 ms | 16.9 ms | 33.2 ms | 20.07 ms |
| Powered / 1366 | 16.73 ms | 16.9 ms | 33.3 ms | 22.04 ms |
| Fresh / 900 | 16.73 ms | 16.8 ms | 33.3 ms | 21.93 ms |
| Powered / 900 | 18.13 ms | 16.9 ms | **300.2 ms** | 22.42 ms |

The compact powered hitch remains unexplained; the measurement does not isolate its cause or close earlier light-map stalls. Existing Vite chunk-size advice remains. These samples use the fresh opening and the paid Turbine checkpoint, not the larger supplied-defence workload or a certified reference laptop/GPU.

## Evidence and handoff

Required records: [full tests](evidence/p7-04-2026-09-08/tests.log), [build/typechecks](evidence/p7-04-2026-09-08/build.log), [final tool typecheck](evidence/p7-04-2026-09-08/tools-final.log), [lint](evidence/p7-04-2026-09-08/lint.log), [legacy snapshot](evidence/p7-04-2026-09-08/snapshot.log), [docsync](evidence/p7-04-2026-09-08/docsync-check.log), [freshness](evidence/p7-04-2026-09-08/freshness.log), [manifest](evidence/p7-04-2026-09-08/manifest.json) and [archive/reference verification](evidence/p7-04-2026-09-08/consistency.log). Source/build archives and a P7-03-relative diff identify this increment. Prior P7-03 evidence/report and the retained port-5177 Phase 6 checkpoint are preserved.

Freshness checks validate configured hashes and ancestry, not complete source identity. Historical factory experiments remain their original source-stamped evidence; no new five-hour balance experiment or human play result is claimed.

**P7-05 discovery integration and engineering review is next.** Shared play and the original conveyor-ammo retest follow P7 and refreshed EX-08C preparation. Human gates remain outstanding.
