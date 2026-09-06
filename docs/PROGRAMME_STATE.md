# Relight — programme state

Current handoff: 2026-09-06, authorised baseline integration and first station-freight increment. PROGRESS.md owns task status; DECISIONS.md and the active RELIGHT-design.md own rules.

## Now

- **Phase 4 complete**, as confirmed by the owner (D-EX-10). Phase 5 and the revised representative loop remain incomplete; forward-built RI features do not complete later phases.
- **EX-01 complete:** the baseline comparison reproduced failures on the old checkout and passing focused checks on newer main. See [BASELINE_INTEGRATION_REPORT.md](BASELINE_INTEGRATION_REPORT.md).
- **Integration authorised and performed (D-EX-12):** branch `codex/tram-expansion` retains the adopted documentation in `f20208a`, then merges tested main `b32e4c3` in `0fdbb67`. City geometry, shared collision masks, inventory and Heart fixes are now in this checkout. No push occurred.
- **First freight increment EX-06A:** station request targets, explicit exports above reserves, destination-reserved cargo, return freight, in-flight capacity accounting across trams, parked loaded trams on disconnected routes, UI and versioned save metadata. See [TRAM_INTEGRATION_REPORT.md](TRAM_INTEGRATION_REPORT.md) for actual verification and limitations.
- **EX-03 remains in progress:** schema compatibility for freight is implemented; separate campaign/evidence profiles must still precede replacing continuous frontage or introducing new campaign constants.

## What the game currently does

Existing unconfigured tram routes keep their legacy transfer behaviour. At a powered tram stop, E opens station controls; applying requests opts that route into selective loading. Configure supply and receiving stations explicitly. Local belts/hands put exports on the platform and take deliveries from arrivals. Save schema 2 preserves the new metadata; it does not identify a completed Version 2 campaign. Existing untouched saves remain schema 1. Older builds reject schema 2 rather than silently ignoring ownership metadata.

The new cul-de-sac, station base registration, remote construction semantics, day/night assaults, radio progression, concentrated persistent extraction, workshops and discoveries are still unbuilt. Legacy frontier pressure still runs. No new-game playtest, balance or fun claim is made.

## Next runnable work

Complete EX-03's gameplay/evidence profile routing and explicit new-session contract, then EX-04's cul-de-sac and second-area restoration. EX-06A contributes transport to EX-06; specialised factories and workshops remain within the latter's unfinished acceptance. The six core contracts are already adopted (D-EX-11); Q07 is the later endgame decision. Further task-level tuning must retain its provenance.

## Verification and workspace

The working test runtime is WSL Ubuntu-24.04 with Node 22.18.0 and the existing Linux dependencies. Windows-native dependencies were not repaired. Required checks and logs are in the integration report; old generated evidence is preserved. The GDD is pinned to LF so its generated-block parser remains stable on checkout.

Untracked `.serena/` and `docs.zip` predate this work and remain intact. Integration work is local. Human play validation is still outstanding.
