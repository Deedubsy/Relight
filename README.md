# Relight

A 2D city-reclamation factory game: explore, restore useful places, connect station factories by tram, and defend a growing network of bases.

**The gameplay audit corrections are implemented.** Start with [the current implementation and playtest guide](docs/Implementation/GAMEPLAY_CORRECTIONS.md) and the [rebuilt fresh campaign](http://127.0.0.1:5178/?view=world). The empty opening, stolen cores, finite defended plants, advanced production, artifacts, passenger tram, explicit truck recovery and engineering encounters are connected. Focused evidence is available; retained regression failures, human playtesting, Q07 and performance/release gates remain open. The older frozen previews below are historical checkpoints.

Phase 4 is complete. P5 implementation and technical review are complete, with successful factory, tram/truck and defence/recovery play reported by the owner. P6-00–04 and P7-01–05 are complete; EX-08C preserves the post-P7 build on port 5179. D-EX-39 now moves shared playtesting after Phase 8. The Phase 8 scope review is complete in docs/PHASE_8_SCOPE_REPORT.md; Q10 adopts the existing truck for queued blueprint construction from a chosen local supply chest. P8-01–04 provide the Foreman clipboard, saved library/ghost orders, physical truck construction delivery and conserved area removal/order controls. P8-05 construction integration and engineering review is complete. EX-08D post-P8 playtest preparation is complete on frozen port 5180; shared human play remains available. D-EX-46 now authorises independent Phase 9 city work; P9-01 validation and its evidence runner are complete (docs/P9_01_CITY_VALIDATION_REPORT.md). P9-02 fixes the seed-6/29 tram-survey failures for new games while preserving existing saves (docs/P9_02_SURVEY_RELIABILITY_REPORT.md); all 32 diagnostic seeds pass. P9-03 saved names, pins and wayfinding are complete (docs/P9_03_WAYFINDING_REPORT.md); P9-04 completed all 10,000 declared seeds, with 258 failures retained. P9-04R now repairs all 258 findings and passes the separate 10,000-seed comparison. P9-05 engineering/preparation is complete; RI-02B-UI-01–07 engineering is complete; current-build human feedback is next and P9-H remains available. Human verdicts remain separate. P6-AMMO awaits the owner’s retest under D-EX-32; formal human verdicts and performance findings remain open. PROGRESS owns execution order. [Phase 8 scope](docs/PHASE_8_SCOPE_REPORT.md). The [port 5179 build](http://127.0.0.1:5179/?rules=exploration-v2&seed=3&view=world&state=/fresh.json) and [guide](docs/EX08C_SESSION_GUIDE.md) are the retained pre-P8 checkpoint. The [post-P8 build](http://127.0.0.1:5180/?rules=exploration-v2&seed=3&view=world&state=/fresh.json) and [EX-08D guide](docs/EX08D_SESSION_GUIDE.md) are ready for shared play. Start with the current handoff before coding.

- [Programme state](docs/PROGRAMME_STATE.md) — actual checkout, reusable work and next task.
- [Progress](docs/PROGRESS.md) — the only executable task list.
- [Constitution](docs/CONSTITUTION.md) and [decisions](docs/DECISIONS.md) — authority, validation and open contracts.
- [Game design](docs/RELIGHT-design.md) — current intended rules, separate from legacy implementation tables.
- [Version 2 development plan](docs/EXPLORATION_DEFENCE_PLAN.md) and [phases](docs/PHASES.md) — migration, remaining scope and exit criteria.
- [Play protocol](docs/EXPLORATION_DEFENCE_PLAYTEST.md) — human evidence needed for the revised loop.
- [Coding-agent guide](CLAUDE.md) — read order and task workflow.
- [Migration report](docs/EXPLORATION_DEFENCE_MIGRATION_REPORT.md) — what this documentation pass changed and checked.

The [Version 1 plan](docs/REVISED_DEVELOPMENT_PLAN.md) and [migration archive](docs/archive/pre-exploration-defence-2026-09-06/INDEX.md) preserve historical direction and evidence. Do not execute both plans.

Packages: sim is pure TypeScript gameplay; game is Phaser rendering/input; harness supplies experiments and bots; tools supplies documentation/evidence checks.

```sh
npm install
npm run dev
npm test
npm run typecheck
npm run lint
npm run docsync:check
npm run freshness:check
```

Use dependencies installed for the operating system running the commands. The currently present esbuild dependency is Linux-specific and cannot run the documentation tools under Windows; this is recorded in the migration report.

`npm run dev` starts the current exploration campaign by default. Open the URL Vite prints with `?view=world`, without a saved state, for the new empty-handed opening. Hand-gather Home salvage and coal, build and fuel production, make Shot and prepare defence before scouting a stolen core. Public trams operate automatically once two permanent stops have power; no tram kit is required. Explicit `?rules=legacy-v1` retains legacy testing, and valid saves follow their own mode. See [the correction guide](docs/Implementation/GAMEPLAY_CORRECTIONS.md).

The [station restoration report](docs/STATION_RESTORATION_REPORT.md) records the new opening reward, local power, save upgrades, checks and its original limitations. The [defence report](docs/CAMPAIGN_DEFENCE_REPORT.md) records the subsequent threat/repair implementation and current limits.
