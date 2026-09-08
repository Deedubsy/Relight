# Relight

A 2D city-reclamation factory game: explore, restore useful places, connect station factories by tram, and defend a growing network of bases.

**Phase 5 implementation is complete, with successful current-build gameplay reported by the owner.** The [Phase 6 review](docs/PHASE_6_SCOPE_REPORT.md) credits EX-05–07. [P6-01](docs/P6_01_RELIABILITY_REPORT.md) completes attack approach/lifecycle reliability; P6-02 defence information and navigation is complete; P6-03 supplied-network defence evidence is complete; P6-04 engineering review is complete; EX-08B remains the completed Phase 6 checkpoint. D-EX-34 moves shared playtesting after P7; the Phase 7 scope review is complete in docs/PHASE_7_SCOPE_REPORT.md, with catalogue Q09 adopted under D-EX-35 and P7-01–05 complete (saved recruits, concrete construction, paid Turbine generation and optional lighting/surveying, discovery journal and item guide); EX-08C playtest preparation is next. EX-08C will refresh preparation after P7. P6-AMMO awaits the owner’s retest under D-EX-32. Formal human closeout details and known performance findings remain open. [Open the retained Phase 6 checkpoint](http://127.0.0.1:5177/?rules=exploration-v2&seed=3&view=world&state=/network.json) or read the [guide](docs/EX08B_SESSION_GUIDE.md). Start with the current handoff before coding.

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

To try the first home opening after starting the dev server, open `?rules=exploration-v2&view=world` or use the header link in the legacy game. This preview has a house, one entrance, starter resources and normal factory building. The second-area station now grants a tram kit, and a nearby radio can be restored. The first defence slice includes walls, paid repairs, recoverable cores, minor raids, planned major assaults and truthful radio warnings. Saves use separate campaign slots; old saves keep their legacy rules. See [the campaign report](docs/CAMPAIGN_OPENING_REPORT.md).

The [station restoration report](docs/STATION_RESTORATION_REPORT.md) records the new opening reward, local power, save upgrades, checks and its original limitations. The [defence report](docs/CAMPAIGN_DEFENCE_REPORT.md) records the subsequent threat/repair implementation and current limits.
