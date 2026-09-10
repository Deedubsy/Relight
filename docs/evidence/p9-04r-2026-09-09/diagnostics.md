# Retained P9-04R diagnostic history

All directories below are separate from the final `population`. The P9-04 baseline is untouched.

- `diagnostic-1`: all nine initial representative/standard seeds pass.
- `survey-1`: prior regression seeds 6/29 pass.
- `failed-seeds-1`: 253 of 258 declared failure seeds completed and passed. The process was terminated while 305/1340/7384/8102/9711 searched for a source confined to the preferred urban pad. Its stale lock and session-start record are retained; this is an incomplete diagnostic, not a completed population.
- `pads-2`: extending the search beyond the preferred pad yields one pass and four reported extractor-footprint failures. Street watershed ownership was insufficient for a source.
- `pads-3`: requiring actual district-owned footprint tiles gives 5/5 passes.
- `failed-seeds-final`: all 258 baseline failures pass on the final simulation source.
- `population`: separate complete declared comparison; its summary and checksums own the result.

`paid` retains 11 successful ordinary routes plus seed 842's initial supply-chest refusal. `paid-funded` mines to an insufficient ten-steel target, consuming everything on the chest. `paid-funded-final` uses the actual chest cost plus cargo but retains a failed destination search. `paid-complete` corrects blueprint geometry queries, and `inspect-truck` shows the engineer blocking the narrow exit. `paid-final` mines nine existing home steel and walks clear: freight, physical truck travel/building, conservation, save continuation and replay pass. These are diagnostic-driver corrections; the game's costs, stock and collision rules were not changed.

Browser diagnostics retain original fixtures and their state comparison: ordinary session initialization adds the optional zero `beltDelivered` and `tramMoved` counters. Final fixtures call `ensureFlow` like session creation. The outdated Map / world selector was corrected to the observed Map/World view button. The first log-preservation browser check ran before the rebuilt session bundle was ready; `browser-verified.log` and `browser-result.json` identify the final build's eight passing cases.
