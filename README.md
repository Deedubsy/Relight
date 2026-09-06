# Relight

A 2D city-reclamation factory game: explore, restore useful places, connect station factories by tram, and defend a growing network of bases.

**Phase 4 is complete.** The current documentation defines the revised direction adopted on 2026-09-06; most revised mechanics are not built. Individual later features exist, and the local branch differs from fetched main. Start with the handoff before coding.

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

To try the first home opening after starting the dev server, open `?rules=exploration-v2&view=world` or use the header link in the legacy game. This preview has a house, one entrance, starter resources and normal factory building. The second-area station now grants a tram kit, and a nearby radio can be restored. Attacks remain pending. Saves use separate campaign slots; old saves keep their legacy rules. See [the campaign report](docs/CAMPAIGN_OPENING_REPORT.md).

The [station restoration report](docs/STATION_RESTORATION_REPORT.md) records the new opening reward, local power, save upgrades, checks and remaining defence work.
