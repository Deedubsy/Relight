# Relight — programme state

Current handoff: 2026-09-07, second-area station and radio restoration (D-EX-14). PROGRESS.md owns task status; DECISIONS.md and the active RELIGHT-design.md own rules.

## Now

- **Phase 4 complete**, confirmed by the owner. Phase 5 and the revised representative loop remain incomplete.
- **Integrated baseline:** branch `codex/tram-expansion` retains adopted documentation in `f20208a`, merges tested main in `0fdbb67`, includes reserved station freight in `f0220a3` and the isolated home opening in `f055cf9`. See [TRAM_INTEGRATION_REPORT.md](TRAM_INTEGRATION_REPORT.md).
- **EX-03 complete:** explicit legacy/new campaign identity, schema 3 for new games, separate browser slots, matching save/replay factories, and separate evidence/document profiles. Existing schema 1/2 saves remain legacy.
- **EX-04A complete:** a house in a court with one physical entrance, reachable starter resources, actual supply transfers and paid factory placements. **EX-04 complete:** the second area now supports hand-supplied station restoration, a route-sized starter kit and radio restoration. See [STATION_RESTORATION_REPORT.md](STATION_RESTORATION_REPORT.md). See [CAMPAIGN_OPENING_REPORT.md](CAMPAIGN_OPENING_REPORT.md).

## What the game currently does

The default URL still opens the legacy game. Its header links to the new Home Court preview; `?rules=exploration-v2&view=world` starts that profile explicitly. It reuses production, power, inventory and walking, with no frontier ammo ring, automatic bloom attacks, contiguous claims, enclosure rewards, passive territory loss or legacy candidate encounters. Its clock uses the adopted 20-minute cycle; major assaults and day/night lighting effects are not implemented.

The home is a provisional static enclosure around the retained factory frame. Its collision walls are not player-built or destructible defences. The initial stock, finite patches and machine prices are retained for this preview; they are not validated campaign balance or persistent regional extraction. Home and the restored station register separate bases. Unrestored districts permit normal paid construction; local generators and physical pole links supply separate circuits. The radio reports powered/offline status; target selection and warnings await EX-05.

Assaults/raids, site creatures, persistent extraction, workshops and discoveries remain unbuilt. The new campaign now unlocks the tested freight system through its station reward. The player collects and lays the kit; survey paint marks suggested track/stops without placing them. No new-game balance or human fun claim is made.

## Next runnable work

EX-05 supplies destructible/player-built defences, core defeat and recovery, site threats, minor raids, major scheduling and truthful radio warnings. The restored radio is ready for that integration; it does not generate fictional alerts now. Q07 remains the later endgame decision.

## Verification and workspace

The working runtime is WSL Ubuntu-24.04 with Node 22.18.0 and existing Linux dependencies. Required checks, browser observations and limitations are in the campaign report. Historical generated blocks, benchmarks and completed task evidence remain unchanged. Campaign metadata version 2 adds station/radio and kit records. Earlier home-only saves upgrade deterministically; their prior command log is not claimed as a full replay under the new initial-state factory. New generated constants live in [CAMPAIGN_RULES.md](CAMPAIGN_RULES.md).

Untracked `.serena/` and `docs.zip` predate this work and remain intact. Integration is local; no push or human play gate is implied.
