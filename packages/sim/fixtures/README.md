# Fixtures

## `city3.json`, `city4.json`, `city5.json` — the street-first city (D6)

Written by `npx tsx packages/sim/test/_exportCity.ts` and read by `city.test.ts`: for seeds 3, 4, 5 on the River city preset, the generator's summary (block/segment counts, degree histogram, districts, facilities with their graph hops, area quantiles, validator verdict) and 5 h runs on compact, balanced, spike and cheapest with the engineer teleporting, plus compact with the engineer walking (D5): magazines, hourly held/front/interior, first interior, blocks lost with the lost log, minutes walked per hour, the tick the truck was found. A change to the sim or the generator that moves them is a change to the game's rules: it needs a doc sentence and a changelog line (constitution rule 1) before the fixture is regenerated.

## `lattice/` — the frozen Phase 1 parity fixtures

The six files under `lattice/` are the parity fixtures the TypeScript sim was proved against in Phase 1 (`npm test`: `regression.test.ts`, `power.test.ts`): seeds 3, 4, 5, open and scattered 24×22 lattice maps, four policies, 5 h — magazines, hourly held/front/interior, first interior, blocks lost with the lost log, shells; `power*.json` the same under the power model.

They were exported by the Python reference sim (`frontsim.py`, `export_fixtures.py`, `export_power_fixtures.py`) at commit `52c4ca3`, the last commit that carried it. **Phase 3 (2026-09-03, Gate A `go`) retired the Python sim and froze these files**: nothing regenerates them. The D5/D6 rework (2026-09-03) archived them here: the lattice is no longer the game's map, but the block rules on it are unchanged and these fixtures still pin them bit for bit (the lattice path of `generateMap` stays for that reason). Since D-B3-4 (2026-09-04, proportional brownout, no shedding) the `power*.json` fixtures pin the block rules only up to the minute of the Python run's first shed: `power.test.ts` checks parity on that prefix, then the first-brownout tick, the sample count and the D-B3-4 invariants (no unfed fall, no more blocks lost than Python, nothing lost inside the window); the shed log itself is history.

The fixture `config` block is the Python sim's snake_case config (`configFromPython` in `sim.ts` maps it); the 24×22 map inside is the sketch the port was proved on.
