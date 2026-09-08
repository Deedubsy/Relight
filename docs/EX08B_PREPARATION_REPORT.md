# EX-08B — shared factory and Phase 6 playtest preparation

Prepared 2026-09-07 under D-EX-33, owner request “Onto the next one then” after EX-08B was named next. **Preparation complete; the new human session has not run.** P6-04 engineering review is complete. Prior successful owner factory/tram/truck/defence play remains credited, while P6-AMMO awaits retesting under D-EX-32.

## Ready build and starts

[Open the focused defence start](http://127.0.0.1:5177/?rules=exploration-v2&seed=3&view=world&state=/network.json) or [open the fresh construction start](http://127.0.0.1:5177/?rules=exploration-v2&seed=3&view=world&state=/fresh.json). Both use the same frozen build and open paused. Port **5177** is separate from the current development preview on 5176 and historical EX-08A archive on 5175. It therefore also has separate browser save storage.

The [verified loopback server](evidence/ex08b-2026-09-07/serve.py) serves only the frozen archive and checks every file hash before extraction. Restart from the repository with `python docs/evidence/ex08b-2026-09-07/serve.py`. No external deployment, commit or push is involved.

| Start | State / browser hash | Conditions and purpose |
|---|---|---|
| Fresh, seed 3 | `ab6af3ec` / `6e541bc4` | Tick 0, empty pockets, normal supplies and first generator. The browser adds ordinary zero-valued bookkeeping fields before any gameplay. Use for construction or direct-conveyor reproduction; no progression assistance. |
| Network, seed 3 | `aa2be5bc` / same | Tick 47800, time 39:50, a **353-command ordinary paid opening replay** from the P6-03 turret-only seed-3 run. Three bases, six supplied turrets, a home ammo assembler, tram freight, restored radio and persistent coal extraction. Home core about 253/300 HP; remote cores full. Use for warning/defence/maintenance follow-up without repeating the opening. |

The network checkpoint is an explicitly scripted setup: the player did not perform its opening, and it cannot measure unaided construction time or discovery. No resource, position, HP, attack or clock value was injected. [prepare.ts](evidence/ex08b-2026-09-07/prepare.ts) replays the original logged commands through ordinary simulation time, verifies conservation and saved continuation, and serializes a complete prefix. First radio lock is ten simulation seconds after resuming; dusk remains 55:00. The prebuilt line uses chest/inserter turret feeds, so a direct-conveyor retest still requires the relevant arrangement. Workshop/optional discovery are unfinished; existing damage and supplies are preserved.

The fresh start retains house stock 200 steel, 100 copper, 50 stone, 40 coal and 20 magazines, plus 40 coal already in the generator. Neither save is the historical EX-08A state. [Settings](evidence/ex08b-2026-09-07/settings.json) record actual inventories, layouts, profiles, defaults and preparation provenance. The in-game “Scenario B” label denotes loading a saved file, not an unlabelled grant of stock.

## Frozen provenance

Parent commit `17939145b5d40afe4dcfc1937a3ec6d34b763df6`, branch `codex/tram-expansion`, plus the already verified uncommitted Phase 6 implementation. All **171 source/test/build inputs** match P6-04's reviewed source. The six compiled production files are unchanged; the new served archive adds the two starts and settings, for **nine files**. No gameplay or tuning change was made.

- [Source archive](evidence/ex08b-2026-09-07/source.zip): SHA-256 `f5da586aae5eec9d9ec65929bb58c0b506837d15d92dc7f11905c06ee6ed24ed`.
- [Playable archive](evidence/ex08b-2026-09-07/playtest-build.zip): SHA-256 `762cda6232088a093feff34a7250cfa0b46a6ba1ac88732dbdadd7fdf4d59916`.
- [Build manifest](evidence/ex08b-2026-09-07/build-manifest.json) identifies every archived source/served file. [Preparation verification manifest](evidence/ex08b-2026-09-07/verification-manifest.json) identifies the new checks/documents and preserved historical evidence.

The source archive includes workspace manifests, lockfile, sources, tests and fixtures; dependencies are installed separately. Rebuild in a separate extracted directory with Node 22.18.0, `npm ci`, then `npm run typecheck`. The archive server does not depend on later edits to the development build.

## Verification

Fresh **51/51 focused tests** pass: campaign information/lifecycle, ordinary defence harness and paid seed-8 route, construction, routing, inspection and transport. Both starts pass source/profile, ordinary-prefix replay, conservation and saved-continuation checks and load paused. Browser tests against the frozen archive verify each start at **1366×900 and 900×900**, matching expected state hash, tick, complete command history, no horizontal overflow, paused stability and actual Ctrl+S/Ctrl+O save/load. Screenshots are visually reviewed. A separate live 1× check reaches the natural dawn at 40:00, receives the actual radio target for 55:00 and confirms G/K preserve gameplay state. Its initial attempt paused as the displayed clock reached 40:00 before warning receipt; the final check waits for actual receipt and still asserts the unchanged 2400/3300-second schedule. Both scripts/logs are retained.

Normal light/power interactions are newly measured on both starts and viewports. Setup and repeated lamp/pole placement/pickup use paid ordinary helper commands with command recording; gameplay time advances live at 1× during each five-second sample. Local headless Chrome 153.0.8010.27, 11th Gen Intel(R) Core(TM) i7-11700K @ 3.60GHz, about 63.8 GiB RAM. This is a local measurement, not a human or reference-laptop/GPU result.

| Start / viewport | Successful edits | Live sim seconds | Changed paints | Mean paint ms | Maximum frame interval ms |
|---|---:|---:|---:|---:|---:|
| fresh / 1366×900 | 20/20 | 5 | 9 | 12.2 | 16.9 |
| fresh / 900×900 | 20/20 | 5 | 9 | 26.9 | 116.8 |
| network / 1366×900 | 20/20 | 5 | 9 | 12.9 | 16.9 |
| network / 900×900 | 20/20 | 5 | 9 | 11.6 | 17.3 |

**The performance finding remains open:** the final compact fresh-start sample reaches a 116.8 ms frame interval, a 124.5 ms changed light-paint maximum and 125.7 ms maximum sampled draw work. Other samples being faster does not erase that stall. The first attempt also measured a 133.5 ms frame interval before a network setup produced no changed light paint and failed its coverage assertion. That initial script/log is retained. The final setup explicitly runs the approach at 1× before placing the devices; all final edits succeed, all samples change the light map and simulation advances five seconds. No performance expectation was weakened and no timing threshold is silently passed.

P6-04's immediately preceding **250/250 full tests**, typecheck/production build, lint and unchanged legacy snapshot remain verified evidence on identical source/build, not new reruns here. No new production source required a rebuild. Fresh documentation profiles/freshness, archive entries and hashes, referenced paths, blank-record status and task consistency are checked in the [evidence directory](evidence/ex08b-2026-09-07/).

## Prepared human scope and limits

The new [player/observer guide](EX08B_SESSION_GUIDE.md) and [blank shared record](EX08B_SESSION_RECORD.md) supersede the old preparation for this follow-up. Earlier [EX-08A report](EX08_PREPARATION_REPORT.md), [guide](EX08_SESSION_GUIDE.md), [blank record](EX08_SESSION_RECORD.md), [original protocol](EXPLORATION_DEFENCE_PLAYTEST.md) and their archives remain unchanged historical artifacts. The current guide reconciles their relevant coverage with P5/P6 and explicitly credits the [owner's earlier successful play](PHASE_5_PLAYTEST_REPORT.md).

The diagnostic scope is direct ammo loading; actual warning comprehension/arrival/preparation; supplied remote minor defence; major/personal support; real freight/repair recovery; repeated maintenance and further exploration; save/load and normal interaction stalls. The two-assembler belted-input Shot exercise is retained with a fresh ordinary start and explicit unscored timing/assistance recording if additional detail is needed. It is not mandated as a repeat of the already confirmed success. Neither the old 75-minute enclosure target nor debug-built output is substituted for that exercise.

Both documents fix settings, assistance handling, continuation/export rules, missing-observation labels and separate owner decisions before this session. No observation row, player quote, timing result, D-SA-2/C3/D-B2-2 position or human Phase 5/6 verdict is filled by automation. The network start cannot establish fresh-opening experience; the long-term rest interval and later nominated/scheduled attacks may need saved continuation. Telemetry is partly legacy-oriented, so milestone saves and human notes remain necessary. Old saved surveys are preserved and old defence command histories may be incomplete for current-code replay.

P6-AMMO is unresolved: a passing automated direct feed or the new supplied checkpoint does not diagnose the reported old failure. Current HP, costs, pacing, personal-support value, maintenance, broad seed fairness and performance/reference hardware remain explicit limits. No new engine, weapon, progression, balance or release decision is adopted.

## Handoff

EX-08B is complete. **EX-08H, P6-H and the remaining T18 human details are ready for observation**, not completed or waived. P6-AMMO awaits the owner's retest; EX-08 assessment depends on actual observations. Q07 and later EX-09/10 work retain their dependencies. The frozen test is left paused for the user; opening it does not start a human session. Earlier code and this preparation remain uncommitted, with `.serena/` and `docs.zip` untouched.
