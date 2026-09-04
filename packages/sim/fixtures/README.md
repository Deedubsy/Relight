# Frozen fixtures

These six files are the parity fixtures the TypeScript sim was proved against in Phase 1 (`npm test`: `regression.test.ts`, `power.test.ts`): seeds 3, 4, 5, open and scattered maps, four policies, 5 h — magazines, hourly held/front/interior, first interior, blocks lost with the lost log, shells; `power*.json` the same under the power model.

They were exported by the Python reference sim (`frontsim.py`, `export_fixtures.py`, `export_power_fixtures.py`) at commit `52c4ca3`, the last commit that carried it. **Phase 3 (2026-09-03, Gate A `go`) retired the Python sim and froze these files**: nothing regenerates them, and a change to the sim that breaks them is a change to the game's rules, which needs a doc sentence and a changelog line (constitution rule 1), not a new fixture. If a rule change is deliberate, the test that fails is edited to state the new expectation next to the run that justifies it.

The fixture `config` block is the Python sim's snake_case config (`configFromPython` in `sim.ts` maps it); the 24×22 map inside is the sketch the port was proved on, the canonical map is 24×24 (D-P1-1).
