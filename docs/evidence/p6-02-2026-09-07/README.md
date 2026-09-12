# P6-02 evidence — 2026-09-07

See [the implementation report](../../P6_02_INFORMATION_REPORT.md) for scope, results, assistance and limitations. This directory is independent of the preserved Phase 6 review and P6-01 evidence.

- `tests-final.log`: 247/247; `focused-initial.log`: 24/25, missing hover text; `focused-second.log`: corrected 4/4 information tests.
- `typecheck-initial.log`: narrowed comparison failure; `typecheck-second.log` and `typecheck-final.log`: intermediate builds; **`typecheck-browser.log` identifies the final tested production build**.
- `lint.log`, `snapshot.log`, `docsync.log`, `freshness.log`, `consistency.log`: final checks.
- `fixtures.ts`: nine labelled UI saves. `browser.cjs`/`browser-result.json`: real controls at both sizes. `arrival.cjs`/`arrival-result.json`: natural dusk while construction is held, followed by actual movement. `interaction.cjs`/`interaction-result.json`: enemy hover and live fresh-stock lamp replacement measurements. PNGs retain the observed presentation; `browser-failure.png` retains the early test's page-offset mistake.
- `check.py`, `build-manifest.json`, `source.zip`, `build.zip`: final uncommitted source/build and historical integrity. `verify.sh` refuses to overwrite a named verification log.

The initial performance attempt was paused; the final script asserts 1× and advancing simulation time. The report uses only the corrected result. Camera/radio/enemy checkpoints inject restored sites, receipt and clock for isolated UI checks, not balance or human experience. Prior owner observations remain in PHASE_5_PLAYTEST_REPORT.md. No commit/push or human verdict is implied.
