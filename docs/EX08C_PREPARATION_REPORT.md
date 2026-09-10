# EX-08C — Post-P7 shared playtest preparation

Prepared 2026-09-08 under the owner's request “OK commit and push then onto EX-08C”. **Preparation complete; human session NOT RUN.** Phase 6/7 implementation and evidence were committed as `8e54ed602da159905b0428f316f6e75cb25ae351` and pushed to `Deedubsy/Relight`, branch `codex/tram-expansion`, before preparation. The default Windows credential helper waited; the successful push used the verified Deedubsy GitHub CLI credentials. No force push or merge.

## Ready build

The verified frozen build runs on **port 5179**. Start with the [fresh discovery opening](http://127.0.0.1:5179/?rules=exploration-v2&seed=3&view=world&state=/fresh.json), or choose an assisted [defence network](http://127.0.0.1:5179/?rules=exploration-v2&seed=3&view=world&state=/network.json) or [Turbine restoration](http://127.0.0.1:5179/?rules=exploration-v2&seed=3&view=world&state=/turbine.json). All open paused, with complete command history. The previous EX-08B/port-5177 checkpoint remains unchanged.

| Start | Sim time / commands | Saved hash / browser-normalized hash | Conditions |
|---|---|---|---|
| Fresh | 00:00 / 0 | `48bce3a6` / `906e54ae` | Ordinary starting stock, empty pockets, no recruits. Recommended for unaided P7 discovery and direct-conveyor investigation. Browser initialization adds ordinary zero-valued bookkeeping fields. |
| Network | 39:50 / 353 | `ef3c6c14` / same | Current-code ordinary prefix from the P7-05 supplied-network workload. Three bases, six supplied turrets, home ammunition line, tram freight, radio and coal extraction. Home core 252.8/300 HP; remote cores full. Natural first dawn in ten simulation seconds, dusk at 55:00. |
| Turbine | 03:17 / 31 | `84cb2486` / same | Current-code replay of the P7-03 pre-commissioning command prefix. Paid concrete production and local Generator prepared; 40 concrete and the other restoration materials are carried. Concrete crew, Lamplighters and Surveyors have already been recruited. Hall remains unrestored; it has not registered a base. |

The assisted openings use ordinary commands, inventories, travel and simulation time. No resource, location, HP, clock or reward is injected. The player did not perform their openings; they cannot establish unaided discovery or construction timing. The network's turret feeds are chest/inserter based and do not independently retest a direct conveyor. Turbine materials are ready for an ordinary local delivery/restoration action, not granted as an unlabelled test fixture.

[prepare.ts](evidence/ex08c-2026-09-08/prepare.ts) verifies the ordinary prefixes, conservation, paused load and saved continuation. [Settings](evidence/ex08c-2026-09-08/settings.json) retain actual stock, machine layouts, state/profile, command counts and provenance. Fresh stock is 200 steel, 100 copper, 50 stone, 40 coal and 20 magazines in the house, plus the initial Generator's 40 coal. All starts use schema 3, campaign metadata 9 and unchanged provisional defaults.

## Frozen provenance

All **181 source/test/build inputs** match the reviewed P7-05 source and the committed work. The production files match its verified build. The playable archive contains **ten files**: six compiled/static production files, three starts and settings. No gameplay source or tuning changed during EX-08C.

- [Source archive](evidence/ex08c-2026-09-08/source.zip): SHA-256 `e0e42957585a39b861cd327459c980c24dbd46689d14955614d0bdaa85700a33`.
- [Playable archive](evidence/ex08c-2026-09-08/playtest-build.zip): SHA-256 `9601506235ef29b020df1abee42c022d342304f859d17a992b181dfd0d2d1af0`.
- [Build manifest](evidence/ex08c-2026-09-08/build-manifest.json) identifies source/served hashes and the commit; [verification manifest](evidence/ex08c-2026-09-08/verification-manifest.json) identifies the preparation evidence and preserved checkpoints.

The [loopback server](evidence/ex08c-2026-09-08/serve.py) verifies archive and file hashes before extracting to a temporary directory; it is independent of later development builds. Restart with `python docs/evidence/ex08c-2026-09-08/serve.py` from the repository. Port 5179 has separate browser save storage. Source can be extracted separately, dependencies installed with Node 22.18.0 / `npm ci`, and rebuilt with `npm run typecheck`.

## Verification

Fresh **37/37 focused tests passed**: discovery journal/recipes, information, Turbine/recruits, construction and routing. [Focused log](evidence/ex08c-2026-09-08/focused.log). P7-05's immediately preceding 271/271 full tests, build/typechecks/lint, unchanged legacy snapshot, twelve paid reward/raid runs, 64-seed sweep and supplied-network review remain evidence on identical source; those broad runs were not repeated for preparation alone.

[Browser checks](evidence/ex08c-2026-09-08/browser-result.json) against the frozen archive pass all **six start/viewport cases** at 1366×900 and 900×900: expected state/tick/log, complete history, pause stability, no horizontal overflow and actual Ctrl+S/O equality. Screenshots were inspected. A separate [live warning check](evidence/ex08c-2026-09-08/warning-result.json) receives the natural dawn warning at 2400 seconds, keeps dusk at 3300, and confirms G/K preserve gameplay state. The [restoration check](evidence/ex08c-2026-09-08/restoration-result.json) commissions through the real button at both widths, spends 40 concrete, retains one base and replays fully. Its first assertion expected the delivery buffer to remain full; the corrected check verifies the existing transfer from deliveries to committed construction accounting. No production change was required.

Four five-second live 1× samples keep the item guide open and use ordinary paid lamp/pole placement/pickup helpers. **72/72 sampled edits succeed**, each sample advances five simulation seconds and repaints light nine times.

| Start / viewport | Mean light paint | Maximum frame interval |
|---|---:|---:|
| Fresh / 1366×900 | 22.9 ms | 34.1 ms |
| Fresh / 900×900 | 22.9 ms | 33.3 ms |
| Network / 1366×900 | 26.6 ms | 33.9 ms |
| Network / 900×900 | **56.3 ms** | **266.8 ms** |

The compact network sample reaches **266.4 ms light paint / 268.9 ms sampled draw work**. Prior P7 and larger-workload stalls remain open. These local headless measurements, partly concurrent with other verification, do not certify reference-laptop/GPU performance or supply a human verdict. No timing threshold is silently passed.

Fresh documentation/profile, freshness, archive/source hashes, preserved evidence, local links and task/blank-record checks are in the [integrity log](evidence/ex08c-2026-09-08/consistency.log). Historical evidence is preserved; configuration freshness is not proof that all historical experiments were regenerated on this source.

## Human scope and handoff

The new [guide](EX08C_SESSION_GUIDE.md) and [blank record](EX08C_SESSION_RECORD.md) cover P5/P6/P7 together while crediting [earlier owner-reported success](PHASE_5_PLAYTEST_REPORT.md). They distinguish unaided fresh discovery from assisted starts, record interventions and activity time, preserve continuations, and keep missing topics explicitly unobserved. They include reward usefulness/optional choices, restoration and recipe comprehension, direct-conveyor loading, warning/arrival/preparation, remote/major defence, paid recovery, repetition, save/load and ordinary interaction stalls.

**EX-08C is complete. EX-08H, P6-H, P7-H and remaining T18 observations are ready for human play**, not completed or waived. P6-AMMO is unresolved pending the owner's retest; a successful automated/new feed cannot diagnose the old failure. Preparation, surviving core damage, provisional balance, performance and unobserved long-term intervals remain limits. Q07 and wider progression keep their dependencies. No Phase 8 implementation or new core rule is implied.

Phase 6/7 are committed and pushed; these new EX-08C preparation files are local changes pending a later commit request. `.serena/` and `docs.zip` remain untouched. The prepared user tab is left paused; opening it does not start or score a human session.
