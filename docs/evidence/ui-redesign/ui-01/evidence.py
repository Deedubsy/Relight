from pathlib import Path
import json
p=Path('docs/evidence/ui-redesign/README.md');s=p.read_text(encoding='utf-8');assert '## UI-01 —' not in s
s+='''
## UI-01 — Shared tokens and shell/input foundations — 2026-09-09

**Implementation complete; automated validation passed; delivered-interface human approval and independent play feedback not recorded.** Owner authorised “Let’s move into RI-02B-UI-01”. [PROGRESS](../../PROGRESS.md) alone owns status. D-UI-03 records the implementation choices. No commit or push.

[Open the current development preview](http://127.0.0.1:5178/?rules=exploration-v2&seed=3&view=world&state=/ui-fresh.json); [ordinary-command prepared construction start](http://127.0.0.1:5178/?rules=exploration-v2&seed=3&view=world&state=/ui-construction.json). Port 5178 is mutable; final bytes are retained in [build.zip](ui-01/build.zip). Frozen post-P9 port 5181 and EX-08D/5180 remain unchanged. These starts reuse exact P9-05 fixture bytes; the supplied construction opening reveals Foreman/station/blueprint information and is not unaided discovery evidence.

### Delivered boundary

The actual entry now mounts a full-viewport canvas, labelled navigation and one scrollable main drawer. Existing domain panels retain their commands, stock scopes and checks. The shell routes Build/clipboard/library, Inventory, Projects, Management/base defence/production, Construction delivery, contextual inspection/cargo/station routes, Help and Navigation. Legacy ring/facilities/summary remain accessible. Developer retains profile links, coordinates and telemetry; player-needed power, buffers, production and loss history stay accessible through management or inspection.

Shared petrol/ivory/brass tokens provide 17px body, 16px secondary text, 40px button targets, clear focus and disabled styling, reduced-motion support and layer ordering. `el` and layer/focus ownership now live in `uiShell.ts`; remaining canvas HUD text reads resolved CSS presentation values. Measured text pairs have minimum **5.60:1**, control-border pairs **4.00:1**, and the primary button **9.32:1**. The candidate border `#69837D` failed against raised controls; `#8DA79F` replaces it after rendered inspection. This measures the declared opaque shared pairs, not every remaining legacy/world label or full accessibility certification.

Pause uses ordinary `setSpeed(0)` and restores the previous nonzero speed (or 1×), with Resume, save/load/download, copy link, controls/item help, Developer and a seed-entry start menu with unsaved-progress confirmation. Its focus stays inside the modal; child Help/start screens close before Pause. Ordinary drawers preserve running speed and paid actions remain possible while paused. Ctrl+S/O stay available outside text fields, including drawer controls. Missing saves show the existing explicit refusal and recovery links without replacing a campaign or changing stored saves.

Input capture clears held movement/sprint and unfinished pointer gestures; typing/select editing, UI clicks and drawer wheel events cannot invoke world actions. A captured belt/copy drag is cancelled before the old pointer-up commit path can run. Returning from a drawer requires a fresh world press; a first click on the city while a drawer is open dismisses that drawer without acting underneath it. Escape closes one layer, then clears placement/selection, then opens Pause; Escape in Pause resumes. Projects uses the shared F2 binding; J remains splitter. Camera framing reserves room for the drawer, existing top HUD and bottom navigation without changing engineer position.

This is the shell foundation. The old build-first objective, repeated canvas guidance and domain-panel content remain for UI-02–05; minimap, final quickbar/categories, structured inspection/projects and the full scaling/remapping/settings matrix are not delivered here. No gameplay costs, reward rules, save schema, generation or simulation/harness source changed. No image generation, replacement framework or new asset system was needed.

### Actual checks and visual review

- **351/351 full tests**, zero failed/skipped, 420.06 seconds: [test log](ui-01/test.log). Simulation/harness/session sources are unchanged from P9-05; no new population or long campaign batch was required.
- Package typecheck/build and lint pass: [typecheck](ui-01/typecheck.log), [final game typecheck/build](ui-01/game-build-final.log), [lint](ui-01/lint.log). The Vite large-chunk advisory remains. Documentation/profile sync, freshness, native whitespace and reference/archive/status checks are linked below.
- [Browser input script](ui-01/browser.cjs), [final log](ui-01/browser-final.log), [results](ui-01/browser-result.json): actual production UI at **1366×768 and 900×768**; text in blueprint name, scroll without zoom, Close while belt selected without placement, Escape hierarchy, modal focus, held W capture/fresh press, one drawer, running/paused semantics, cancellation of an actual affordable belt drag, local save/reload, exact replay, and Pause restoring 4×.
- [Adapter script](ui-01/adapters.cjs), [log](ui-01/adapters.log), [results](ui-01/adapters-result.json): real truck source/order selection and pause, order pause/resume, library export, cargo access, contextual machine inspection, save/load from a drawer control, cancelled fresh-city confirmation, blur/fresh input, legacy management and a missing-slot refusal preserving browser storage. The missing-slot exception is expected and separately recorded; ordinary entries have no page errors. No stock/position/HP injection was used.
- Opened actual [1280×720 seed-3 opening](ui-01/opening-1280.png), [900px Build drawer](ui-01/build-900.png), [Pause](ui-01/pause-1366.png) and [missing-save surface](ui-01/missing-save.png). Opening uses paused tick-zero P9-05 data and camera 0.65×. Canvas area/framing differs from the old 420px sidebar baseline: the engineer is centred in the remaining safe rectangle. Narrow navigation wraps into two rows; the drawer scrolls internally, readable fields stay in bounds, and the engineer remains visible between HUD and drawer. Pause leaves the existing warning readable above it. Existing HUD prose remains prominent and is explicitly UI-02 work.
- Development diagnostics: initial TypeScript build found non-iterable DOM collections (corrected with `Array.from`); initial navigation mount assumed its reference node was still a direct sidebar child (fixed with a dedicated adapter mount). Test-harness diagnostics included a route glob matching the page query, a queued-speed assertion before the next frame, an assumed library ID and inspection before world focus/camera settled. Corrected tests use actual options and normal input/frame boundaries. [Initial build log](ui-01/typecheck-initial.log), [earlier browser log](ui-01/browser.log), [final scripts/results](ui-01/browser-result.json). Final required checks pass; no simulation rule was relaxed.

No fresh performance benchmark or independent-player usability session was run for this bounded foundation step. Existing 116.8/266–300ms findings and reference-performance obligations remain open; these UI checks do not establish a performance fix. Formal P9-H/P6-H/P7-H/P8-H/EX-08H/T18 and P6-AMMO remain separate.

### File changes and reproducibility

Parent commit `8e54ed602da159905b0428f316f6e75cb25ae351`, branch `codex/tram-expansion`, unchanged campaign fingerprint `ba11e896`. [Manifest](ui-01/manifest.json), [source archive](ui-01/source.zip), [exact delta against P9-05](ui-01/source-delta.patch): **206 source inputs, six production files and three unchanged prepared fixtures; nine existing UI files edited plus one new file**. Existing unrelated working-tree changes remain intact. This list is from the P9-05 source comparison, not the accumulated branch diff.

| File (packages/game/) | UI-01 change |
|---|---|
| src/uiShell.ts (new) | Shared DOM primitive, adapters, one drawer, Pause/start children, focus/input ownership and resolved palette |
| src/style.css | Shared tokens, full-viewport shell, drawer/modal/nav layout, contrast, reduced motion and failure surface |
| src/panel.ts | Route existing domain controls through shell; preserve management data, expose repair reasons and Pause actions |
| src/main.ts | Wire shell input/selection hooks, isolated navigation mount, global shortcut boundary and failure presentation |
| src/worldScene.ts | Guard world input; cancel captured drags; use shared HUD palette and camera safe area |
| src/cityMapScene.ts | Guard map world actions while UI owns input |
| src/mapScene.ts | Same guard for legacy map |
| src/controls.ts | Shared F2 Projects binding |
| src/view.ts | Right/bottom HUD inset presentation state |
| index.html | World landmark and accessible feedback region |

Documentation changes: this evidence note, PROGRESS/PROGRAMME_STATE/CLAUDE/README/current phase pointers, D-UI-03 and the specification's implementation note. [Docs/profile check](ui-01/docsync.log), [freshness](ui-01/freshness.log), [native diff check](ui-01/git-diff-check.log), [integrity/reference checker](ui-01/check.py), [verification](ui-01/verification.json).

**Next implementation: RI-02B-UI-02 — core HUD, contextual interaction and truthful objectives.** Other UI packages retain their dependencies; no human gate is closed by this implementation.
'''
p.write_text(s,encoding='utf-8',newline='\n')
