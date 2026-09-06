# Campaign separation and first home opening

2026-09-06. Authorised by the owner through D-EX-13, following the committed station-freight baseline `f0220a3`. This report covers EX-03 and EX-04A, the home-only subset of EX-04. It does not complete the revised representative loop or a phase gate.

## Result

A new session explicitly selects `exploration-v2`. The unconfigured/default game remains legacy. The new profile starts at Home Court with a house supply inventory, starter patches, factory space and a physical boundary with one four-tile entrance. The engineer can leave and return through the same opening and place ordinary machines using carried materials. State-driven guidance changes after the first production machine is placed.

Gameplay shares the existing walking, collision, inventory, machine, belt and power implementation. The new profile disables legacy frontier-ring construction/demand, automatic bloom pressure, contiguous claims/commissioning, passive territory loss, enclosure rewards, legacy projects and opt-in Heart/Stalker candidates. No replacement assault is fabricated. The displayed planning clock uses the adopted 20-minute cycle with 15 minutes of daylight.

## Compatibility and evidence boundary

| Concern | Contract |
|---|---|
| Legacy identity | Missing ruleset means `legacy-v1`; untouched saves retain schema 1, freight saves schema 2. |
| New game identity | Schema 3, explicit `exploration-v2`, versioned opening metadata and home block. Unsupported or mixed metadata is rejected. |
| Browser saves | New campaign slot 1 uses a separate key from legacy slot 1. Share/load URLs carry the profile. |
| Load failure | A visible error and explicit new-session links replace automatic fallback. Invalid saves never silently become a fresh game. |
| Replay | Session replay chooses the matching initial-state factory. Logged inventory actions reproduce the same state hash. |
| Generated docs | `docsync` checks both profiles by default; `--profile=legacy-v1` and `--profile=exploration-v2` select one. New constants are generated into CAMPAIGN_RULES.md. |
| Evidence | Legacy ConfigRefs and hashes remain unchanged. Campaign ConfigRefs fingerprint campaign rules and geometry and require campaign experiment/snapshot directories. Freshness rejects path/profile and state/profile mismatches. |
| Existing baselines | Historical generated tables, evidence files and snapshot expectations are preserved. The legacy goal oracle rejects campaign states rather than applying the old hour's acceptance. |

Campaign artifact destinations are `docs/experiments/campaign/` and `packages/game/public/snapshots/campaign/`; this increment adds routing/validation, not a full campaign benchmark generator. The focused tests construct campaign fixtures independently of the retained legacy fixtures. Day/night clock continuation is tested; assault scheduling state belongs to EX-05 and does not exist yet.

## Provisional opening and remaining work

The 24-by-24 retained factory frame sits inside a static court boundary. The mouth prefers a clear eastern approach, then south, west and north. The same collision mask controls walking and placement. These are reversible geometry defaults, not owner-approved wall strength or defensive balance. The boundary is not yet player-built or destructible. Existing starter inventory, machine costs, finite patches and generator/substation power costs are reused for this preview.

EX-04 continues with hand-supplied second-area restoration, tram-kit access and the radio opportunity. Station base registration, remote construction, regional persistent extraction, workshops, discoveries, site creatures, raids, major assaults and defence damage/repair remain unimplemented. Legacy locked-item descriptions still exist in the shared build catalogue; the preview header explicitly identifies unavailable progression. The complete free-roaming logistics/defence loop is not ready for its human gate.

## Automated verification

Runtime: WSL Ubuntu-24.04, Node 22.18.0, existing Linux dependencies. Windows-native dependency repair remains outside this increment.

| Check | Result | Evidence |
|---|---|---|
| Simulation suite | 165/165 passed | [test log](evidence/campaign-opening-2026-09-06/test.log) |
| Focused campaign tests | 4/4 passed; final rerun includes logged inventory replay | [focused log](evidence/campaign-opening-2026-09-06/focused.log) |
| TypeScript and production game build | Passed after explicitly limiting the legacy goal oracle's type domain | [build log](evidence/campaign-opening-2026-09-06/typecheck.log) |
| Lint | Passed | [lint log](evidence/campaign-opening-2026-09-06/lint.log) |
| Generated documentation | Both profiles passed | [docsync log](evidence/campaign-opening-2026-09-06/docsync.log) |
| Evidence freshness | Passed; historical artifacts untouched | [freshness log](evidence/campaign-opening-2026-09-06/freshness.log) |
| Legacy snapshot | Matched retained seed-3 three-hour snapshot | [snapshot log](evidence/campaign-opening-2026-09-06/snapshot.log) |
| Document links, tracker and preserved generated blocks | Passed | [consistency log](evidence/campaign-opening-2026-09-06/consistency.log) |

The full suite ran before the final goal-oracle typing/label cleanup; focused campaign tests and the required static/build checks were rerun after the relevant changes. The production bundle retains Vite's large-chunk advisory. No benchmark expectations were changed to obtain passing checks.

The geometry checks cover seeds 3, 4, 5, 8 and 13: exactly one entrance, solid perimeter, paths out and back, reachable steel/copper, accessible house stock, real paid assembler placement, conservation and identical geometry after load. This is representative coverage, not a guarantee for every seed.

## Browser observations

On seed 3, the new profile rendered the house and court. House interaction opened actual inventory; taking 50 steel and 50 copper enabled a paid assembler placement, and guidance changed to exploration. The first browser check exposed obsolete front-edge HUD labels; these were removed from the campaign HUD and block label. The final HUD labels magazine production capacity rather than claiming an idle machine produces its rated output.

Saving preserved the built assembler. The automation timed out clicking Load in the original tab; a fresh tab successfully opened the same saved campaign, paused, with the assembler and exploration goal intact. A separate attempt to open it under legacy rules displayed the intended profile-mismatch error with explicit fresh-start links. The browser timeout is not claimed as a successful original-tab Load interaction.

These are agent-operated checks, not a human playtest or evidence of enjoyable pacing. The owner approved the direction and this implementation scope; no human phase gate was completed. Work remains local with no push. Pre-existing `.serena/` and `docs.zip` are unchanged.
