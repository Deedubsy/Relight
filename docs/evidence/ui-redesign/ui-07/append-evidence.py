from pathlib import Path
out=Path(__file__).resolve().parent
assert '# pass 376' in (out/'test.log').read_text(encoding='utf-8-sig')
p=out.parent/'README.md';s=p.read_text(encoding='utf-8');assert '## UI-07 —' not in s
s+='''
## UI-07 — Actual visual review and targeted refinement (2026-09-09)

Completed the owner's “Continue” under D-UI-09. **Engineering and developer QA are complete; independent fresh-player feedback is pending.** The [frozen UI-07 opening](http://127.0.0.1:5182/?rules=exploration-v2&seed=3&view=world&state=/ui-fresh.json) is the current review build. Restart it with `python docs/evidence/ui-redesign/ui-07/serve.py`. Mutable port 5178 and all previous frozen builds remain separate.

Reviewed the actual UI-06 entry, then made one focused refinement pass. [Before opening](ui-07/before-opening-1280.png) showed the startup notice and core HP label crowding the engineer. The [final opening](ui-07/frozen-opening-1280.png) identifies the engineer with a brief “You” tag, removed after two tiles of ordinary walking. HP now sits above the core building. The fresh paused opening uses the existing clock instead of a load toast; later saved campaigns retain a short acknowledgement. Baseline is unchanged seed 3, time 0:00, 1280×720, camera 0.65, state hash `e13e92a4`, engineer world position `(421.5, 638.5)`. No stock or position was injected.

[Before compact danger](ui-07/before-warning-1366.png) hid the engineer between the alert/objective and lower HUD. The [refined compact warning](ui-07/warning-raid-1366.png) preserves danger, HP, inventory, speed and the engineer together. While a drawer and urgent warning are open on a short logical viewport, the objective card yields its space; full project information remains in Projects. Secondary spacing is reduced without reducing text size. [Before ultrawide danger](ui-07/before-warning-3440.png) covered the Projects heading; [after](ui-07/warning-raid-3440.png), the warning stops before the actual drawer boundary. Camera/HUD reservations include the ultrawide safe margin. This moves only the screen composition, not the engineer. The default ultrawide camera remains 0.85; compact opening remains 0.65. At 1366×768/150%, the closed-panel engineer shifts vertically from about 421px to 379px as the HUD reflows; the baseline 1280×720 composition stays at approximately `(640,361)`.

The staged developer walkthrough uses actual paid commands: collect Home steel/copper, preview/place/repeat/pack equipment, inspect a stalled assembler, inspect a working ammunition line, track a known project, deliver and restore, and inspect warnings. It is deliberately **not** a claim of one continuous unaided playthrough: fresh and network/construction starts reuse the unchanged ordinary-command P9-05 fixtures; delivery uses the prior UI-05 paid/walked checkpoints. Explicit outage/known-threat/resolution and urgent-state injections are presentation fixtures with no combat or replay verdict. Opened actual-game images include [paid resource-patch placement](ui-07/building-placement-1280.png), [stalled assembler](ui-07/inspection-stalled-1280.png), [working ammunition line](ui-07/inspection-working-1280.png), [project requirements/delivery](ui-07/projects-service-1280.png), [150% Projects](ui-07/after-project-1366.png), [urgent compact](ui-07/warning-raid-1366.png) and [urgent ultrawide](ui-07/warning-raid-3440.png). Hierarchy, character identification, contrast, collisions and truthful action/result states were reviewed; no further blocking issue was observed in this bounded pass.

Verification:

- [Full tests](ui-07/test.log): **376/376**, zero failures/skips; [focused guidance/build/preferences](ui-07/focused.log): **17/17**. [Typechecks](ui-07/typecheck.log), [final game build](ui-07/game-build-final.log), [lint](ui-07/lint.log), [docs/profile checks](ui-07/docsync.log), [freshness](ui-07/freshness.log), [diff check](ui-07/git-diff-check.log). Existing bundle-size and light-map performance findings remain open; no broad seed/population batch was run.
- Current-build paid browser results: [building](ui-07/building-result.json), [inspection/inventory/freight/truck](ui-07/inspection-browser-result.json), [Projects/paid restoration/radio/alerts](ui-07/projects-result.json), all at 1280px; [shell/Pause/input/save/replay](ui-07/regression-browser-result.json) at 1366px and 900px. Scripts and corresponding logs are in `ui-07/`. The final world-label adjustment was rebuilt and its affected baseline, machine, project and warning captures refreshed; no unrelated mechanics check was repeated for that label.
- [Eight scaling cases](ui-07/matrix-result.json): 1366×768 at 100/150%, 1920×1080 at 100/125/150%, 2560×1440 at 125%, 3440×1440 at 100/150%. Geometry, visible engineer, focus, scroll, text/wheel isolation and unchanged state/zoom pass. Shared contrast remains at least 5.60:1 for text and 4.00:1 for borders.
- [Twenty urgent cases](ui-07/warnings-result.json): 1366×768 and 3440×1440 at 150%, each covering minor raid, engineer hit, engineer down, nearby hostile, damaged core, disabled core, major countdown, active assault, withdrawal and unavailable target. Warning text, drawer heading/Close, player visibility and HUD separation pass; urgency and “Paused” persist in Pause. [Runner](ui-07/warnings.cjs) explicitly labels injected states, which do not represent earned gameplay outcomes.
- [Frozen-port smoke](ui-07/frozen-result.json) loads all three unchanged P9-05 starts paused with exact replay; fresh opening movement, identity-label dismissal and slot save/reload use ordinary keys and replay exactly. Initial smoke timing assumptions were corrected to await two actually walked tiles and the HUD refresh, rather than a fixed wall-clock delay.

[Manifest](ui-07/manifest.json), [source archive](ui-07/source.zip), [build archive](ui-07/build.zip) and [delta against UI-06](ui-07/source-delta.patch) bind **219 source inputs, five changed game files, six production files and three unchanged prepared starts**. Parent commit `8e54ed602da159905b0428f316f6e75cb25ae351`, branch `codex/tram-expansion`; no commit or push. All simulation source, session/save schema, city generation, costs, harness and campaign fingerprint `ba11e896` remain unchanged. [Integrity checker](ui-07/check.py) and [verification](ui-07/verification.json) bind archives, HTTP files, checks, references and documents, and preserve UI-06/P9/EX-08D evidence.

### Current-build fresh-player follow-up — pending

Recommend one independent 10–15 minute session on the [frozen fresh city](http://127.0.0.1:5182/?rules=exploration-v2&seed=3&view=world&state=/ui-fresh.json). Record screen size and chosen scale; it starts paused. Ask the player to identify their engineer, obtain supplies, place a machine, explain a stall and find a known project. Let them explore before assisting. Record actual hesitation, mistaken actions, the exact help given and whether the player recovered. Do not convert these observations into an invented intuition score.

For a separately labelled later-state check, use the [prepared construction start](http://127.0.0.1:5182/?rules=exploration-v2&seed=3&view=world&state=/ui-construction.json) or [prepared factory/network](http://127.0.0.1:5182/?rules=exploration-v2&seed=3&view=world&state=/ui-network.json). These include scripted ordinary-command assistance. Note any warning seen and what the player thought it meant; no warning during a short session is not a comprehension pass. Saves and settings on port 5182 are separate from the older preview origins.

| Observation | Actual record |
|---|---|
| Participant, date, viewport, scale, start URL | Pending — no independent participant observed |
| Engineer identification and first hesitation | Pending |
| Supplies, placement and mistaken actions | Pending |
| Stall explanation and assistance provided | Pending |
| Project discovery/tracking and any warning interpretation | Pending |
| Player's own feedback and requested refinement | Pending |

This area supplies new-build context to EX-08 without editing old session records. **UI-07 engineering is done; EX-08 remains blocked on EX-08H.** Formal P9-H/P6-H/P7-H/P8-H/EX-08H/T18, P6-AMMO, Q07 and performance findings remain open. Older play observations retain their original build identity. Stop this refinement pass here; use actual new feedback to justify further changes.
'''
p.write_text(s,encoding='utf-8',newline='\n')
print('Appended shared UI-07 evidence and blank current-build fresh-player follow-up.')
