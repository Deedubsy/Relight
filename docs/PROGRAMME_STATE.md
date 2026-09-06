# Relight — programme state

Current handoff: 2026-09-07, first campaign defence slice complete (D-EX-15/16). PROGRESS.md owns task status; DECISIONS.md and the active RELIGHT-design.md own rules.

## Now

- **Phase 4 complete**, confirmed by the owner. Phase 5 has not started (owner correction, D-EX-15). EX-03–08 are an intervening design revision before that phase.
- **Integrated baseline:** branch `codex/tram-expansion` retains adopted documentation in `f20208a`, merges tested main in `0fdbb67`, includes reserved station freight in `f0220a3` and the isolated home opening in `f055cf9`. See [TRAM_INTEGRATION_REPORT.md](TRAM_INTEGRATION_REPORT.md).
- **EX-03 complete:** explicit legacy/new campaign identity, schema 3 for new games, separate browser slots, matching save/replay factories, and separate evidence/document profiles. Existing schema 1/2 saves remain legacy.
- **EX-04A complete:** a house in a court with one physical entrance, reachable starter resources, actual supply transfers and paid factory placements. **EX-04 complete:** the second area now supports hand-supplied station restoration, a route-sized starter kit and radio restoration. See [STATION_RESTORATION_REPORT.md](STATION_RESTORATION_REPORT.md). See [CAMPAIGN_OPENING_REPORT.md](CAMPAIGN_OPENING_REPORT.md).

## What the game currently does

The default URL still opens the legacy game. Its header links to the new Home Court preview; `?rules=exploration-v2&view=world` starts that profile explicitly. It reuses production, power, inventory and walking, with no frontier ammo ring, automatic bloom attacks, contiguous claims, enclosure rewards, passive territory loss or legacy candidate encounters. Its clock uses the adopted 20-minute cycle; the first defence increment now supplies major assaults, minor raids and local ruin guards. Day/night lighting effects remain unimplemented.

The home is a provisional static enclosure around the retained factory frame. Its collision walls are not player-built or destructible defences. The initial stock, finite patches and machine prices are retained for this preview; they are not validated campaign balance or persistent regional extraction. Home and the restored station register separate bases. Unrestored districts permit normal paid construction; local generators and physical pole links supply separate circuits. The radio now names the locked target while powered, retains received warnings during outages and supports a paid precision upgrade.

Paid walls, damaged turrets, disabled/recoverable cores and manual repairs are implemented. Persistent extraction, workshops and distinctive discoveries remain unbuilt. The new campaign now unlocks the tested freight system through its station reward. The player collects and lays the kit; survey paint marks suggested track/stops without placing them. No new-game balance or human fun claim is made.

## Next runnable work

EX-05 is complete for the two-base slice; see [CAMPAIGN_DEFENCE_REPORT.md](CAMPAIGN_DEFENCE_REPORT.md). EX-06 is the next runnable task and supplies specialised extraction/workshop service and the later third-station/resupply milestone. Q07 remains the later endgame decision.

## Verification and workspace

The working runtime is WSL Ubuntu-24.04 with Node 22.18.0 and existing Linux dependencies. The full suite passed 180/180 tests, build/typecheck passed, and browser observation verified wall costs, defeat and paid recovery. Documentation profiles, freshness and the unchanged legacy snapshot passed. Detailed limits are in the defence report. Historical generated blocks, benchmarks and completed task evidence remain unchanged. Campaign metadata version 3 adds base damage, schedule, paid repair and radio records. Earlier previews upgrade deterministically with protected preparation time; their prior command log is not claimed as a full replay under the new initial-state factory. New generated constants live in [CAMPAIGN_RULES.md](CAMPAIGN_RULES.md).

Untracked `.serena/` and `docs.zip` predate this work and remain intact. Integration is local; no push or human play gate is implied.
