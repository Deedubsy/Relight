# RI-02B — Intuitive UI and Visual Identity

**Current amendment — D-UI-10 (2026-09-09):** The owner supplied an implementation request and approved concept image. UI-08 extends this phase with an illustrated catalogue, drag-customisable action bar, real Backpack stacks and paired storage; normal player time is 1× with pause. Earlier UI-01–07 completion evidence remains historical. The image was opened and inspected for UI palette, hierarchy and icons; its city artwork, counts and illustrated capacity are not gameplay requirements.

Design specification, 2026-09-08, D-UI-01. This is implementation-ready planned work, not a description of delivered UI. [PROGRESS](PROGRESS.md) alone owns statuses. The owner requested this phase before the next broad fun assessment or major expansion; this run authorises documentation only.

## Placement and reuse

RI-02B is unused and complements completed RI-02 and the integrated RI-02A city presentation. No RI task is restarted. Under D-UI-02, implement UI-01 through UI-07 after Phase 9 engineering/preparation (P9-05) and before EX-08's broad assessment. P9-H remains the separately required human review/verdict; its completion is not inferred or waived. Existing frozen EX-08D human sessions remain available as baseline observations, not redesigned-UI validation. UI-07 prepares a current-build follow-up in the same evidence directory. P9-05 is the sequencing prerequisite; no Q07 resolution, new framework or game-economy change is required.

Credit RI-02 goals/names/save support, P5 catalogue/commands/routing/inspection/statistics, P6 known-threat navigation, P7 discoveries/item guide and P8 clipboard/library/orders/truck. Phase 12 and RI-13 consume this phase's shell, onboarding, scaling, remapping and save UX evidence instead of scheduling them again; production world art, wider audio, release/platform work and human phase verdicts remain there. P9-03 retains saved names and player-authored pins; this phase only displays existing names and navigates to known targets. RI-02B must integrate P9-03's resulting navigation and saved names/pins; reuse any resulting map infrastructure rather than building a competing system.

## Grounding and actual architecture

Inspected `packages/game/index.html` → `src/main.ts` → `createPanel` in `src/panel.ts`, Phaser `WorldScene` and `CityMapScene` (legacy `MapScene` retained). HTML/CSS presentation and pure TypeScript `packages/sim` already have the required boundary. `session.ts` supplies `queue`, `dispatch`, `setSpeed`, `saveSlot`, `loadSnapshot` and replay; `controls.ts` supplies `BINDINGS`, `bound`, `shortcut`, `MOVEMENT_KEYS`. `view.ts` owns renderer selection/camera state; there is no component framework to replace.

Current `style.css` uses a 420px permanent sidebar, 13px panel body and 11px hints. `worldScene.ts` draws additional corner HUD text. `campaignGuidePanel.ts` opens Discoveries by default, alongside expanded P8 panels. No separate minimap, settings UI or pause-menu shell exists. `controls.ts` explicitly defers rebinding; no game audio integration was found in the inspected game source. Relay audio is conditional reuse only; do not build an audio engine for this phase.

The user-supplied screenshot itself was unavailable; the description was treated as a hypothesis. The actual frozen EX-08D build was started with its hash-validating `serve.py` and opened at `http://127.0.0.1:5180/?rules=exploration-v2&seed=3&view=world&state=/fresh.json`. [Baseline image](evidence/ui-redesign/baseline-opening.png) was captured and visually inspected: 1280×720, seed 3, paused 0:00, world view, default camera 0.65×, empty pockets, Home visible. This is a prepared frozen fresh start, not a naturally played session. It confirms small text, duplicate warning text, competing default-open controls and a build-first heading with supplies relegated to supporting text. Only the first station is shown in Discoveries here; the screenshot description's simultaneous later projects is not a confirmed current disclosure defect. The baseline includes newer blueprint/truck clutter. It does not establish usability, contrast compliance or future implementation success.

The checkout has existing uncommitted P8/P9 files. During this planning run the parallel P9 handoff was updated: PROGRESS now records P9-01 done with its existing report and P9-02 geometry findings. Preserve that completion and its evidence; this planning run did not perform or independently rerun that validation. `REVISED_DEVELOPMENT_PLAN.md` is the historical equivalent of the requested revised-plan filename; `CITY_REBUILD_REPORT.md` and the legacy RI-02A GDD reference preserve integrated city context. No standalone `RELIGHT_CITY_REBUILD_PROMPT.md` or `RELIGHT_REVISED_DEVELOPMENT_PLAN.md` was found. Current UI architecture is the source listed above, not an assumed UI architecture document.

## Design system

Binding decisions: municipal electricity-authority field equipment; enamel signs, service diagrams and switchboard clarity. No grime behind text, scanlines, flicker, decorative gauges, emoji icon system or borrowed assets. Shared CSS tokens belong in existing `packages/game/src/style.css`; reusable DOM primitives and layer/input ownership are to be extracted from `panel.ts` into a **new** `packages/game/src/uiShell.ts`. UI-01 now provides this file; [implementation evidence](evidence/ui-redesign/README.md) records its actual scope and checks. Keep domain panels in their existing modules. Do not copy tokens into Phaser; pass resolved shared presentation values where canvas text remains necessary.

Tunable starting tokens (adjust only against rendered evidence; semantics are binding):

| Token | Initial value / rule |
|---|---|
| `--ui-bg`, `--ui-surface`, `--ui-raised` | #151D1E charcoal, #203536 petrol, #2B4546 raised controls; opaque behind body text |
| `--ui-text`, `--ui-secondary` | #F3ECDD warm ivory, #C2CECA readable secondary |
| `--ui-accent`, `--ui-working`, `--ui-danger` | #E3B96F amber/brass, #92D8B5 mint, #FF8A80 red; red only immediate danger/failure |
| `--ui-border` | #69837D; 1px panel separation, 2px selected edge plus word/symbol |
| Typography | Segoe UI/system sans body 17px, line-height 1.45; secondary 16px; short headings 20px/600, title 24px; tabular numeric figures. Industrial monospace only short equipment markings |
| Spacing / radius | 4, 8, 12, 16, 24, 32px grid; 6px corners; panel padding 16px |
| Buttons / slots | at least 40×40 logical px hit target; 48px quickbar slots; primary amber with charcoal text; disabled retains readable text and adjacent reason |
| Focus / selection | 3px ivory focus outline, 2px offset; amber selection border and selected label; keyboard focus distinct from selected tool |
| Tooltips | max 320 logical px wide, wrap; click/focus help as well as hover; clamp 12px inside viewport and avoid anchor/critical warning |
| Layer order | world 0; passive HUD 10; one drawer 20; attached inventory 21; tooltip 30; pause/settings dialog 40; urgent alert strip 50 |
| Motion | 150ms panel fade; one 250ms circuit-close response. Reduced motion: instantaneous state + persistent completed text; no blinking |

Check rendered normal text contrast ≥4.5:1, large text/control boundaries ≥3:1; measure actual foreground/background pairs after opacity/composition. Colours above are candidates, not a compliance claim. Icons: custom simple inline SVG paths on a consistent 24-unit grid: power bolt/socket, production gear/output arrow, transport paired rail lines, supplies crate, restoration open/closed circuit. Every category and consequential action keeps a text label. No image generation is needed for these vector symbols.

## Layout, layers and input contract

| Region | Contents and boundaries |
|---|---|
| Top left | Existing neighbourhood name; one tracked goal, one next unmet action, Show location; max 360 logical px, wrap to three short lines |
| Upper right, below local minimap | `campaignClock` day/night and pause, actual known assault/intelligence; no acceleration controls or permanent 1× label |
| Top right | Collapsible 180×140 logical px minimap and Map button; player, existing terrain representation and discovery-filtered destinations |
| Bottom left | Engineer HP/down state, equipped tool/weapon, actual relevant ammunition, pocket stacks and Inventory access |
| Bottom centre | Stable quickbar; illustrated Build, Backpack and Projects; full Map beside minimap and secondary Menu commands; current bindings shown |
| Right inspection | Single machine/site/truck card, normally closed; pin preserves identity, never adds a second inspection card |
| At object / cursor | One target/action prompt in reach; placement reason near preview; never a wall of help |
| Pause | Resume, save slot/load, export save, settings, controls/help, copy seed/settings link, return to title/start screen with unsaved-work confirmation. Browser exit is ordinary browser close, not an unsupported app quit |
| Management | Production, power, logistics, base condition, construction orders; precise data accessible without developer mode |
| Developer | Coordinates, seed header, tile totals, zoom, legacy/profile links, telemetry/replay diagnostics, hidden entity totals |

Default desktop: full viewport city canvas. One main drawer (Build, Projects, inspection, management or Help) maximum. Only a targeted inventory transfer may attach a second drawer; combined width ≤45% viewport. Build placement closes its catalogue; pinned inspection collapses to a small identity chip while placing. A modal replaces ordinary drawers while preserving return focus. Urgent warnings stay visible above ordinary UI and in pause (with “Paused”); no automatic camera jump or modal opening on attack.

Drawer width 360–420 logical px, max-height viewport minus top/bottom safe areas, internal scroll. Owner feedback after UI-08: menus overlay a stable world viewport. Opening, closing, resizing or switching inventory/interaction panels must not pan or zoom the camera; the follow target uses the full viewport, independent of HUD insets. Normal engineer following and explicit Locate / Return to engineer actions remain. At insufficient width/height, collapse minimap, use a single tabbed transfer drawer, then move ordinary navigation into a labelled overflow button. At 1366×768/150% or long text, use one wide drawer within the viewport with world still visible; do not shrink body text. Quickbar may use two fixed rows preserving slot order. Ultrawide HUD is centred within a 1920 logical px safe span; inspection may sit at that span's right boundary. Tooltips scroll/wrap and never overflow the viewport.

Ordinary inventory/build/projects/map views preserve the current running simulation and paid construction while paused. Pause uses 0; resume always returns to 1. `session.ts` clamps speed at queue, dispatch and frame application, including manually injected pending commands. Old running save speeds normalize to 1, paused saves retain 0, and loading/pause transitions clear the accumulator. Foreground frames retain the fixed simulation timestep and elapsed-time progression; gaps over 250 ms passed directly to frame are discarded, while main retains its pre-existing 100 ms render-delta cap. No player acceleration controls, speed bindings, URL option or exposed runTicks hook remains. Headless sim and deterministic runners are unchanged.

Input owner: world, placement, drawer or modal. UI pointer-down/up/click/contextmenu cannot reach world actions; wheel on a panel cannot zoom; captured drags end safely outside canvas. Text/select/contenteditable focus blocks global hotkeys and Phaser held movement, sprint, dodge, mining and firing. Clear held keys/pointer gestures on capture, blur, visibility loss and closure; returning focus requires a fresh press. Escape: close help/popover → foremost drawer/modal child → cancel active placement/selection → open Pause; Escape in Pause resumes explicitly. One press does one transition. Restore previous valid focus, otherwise canvas; modal focus is contained with working close/Resume. Labels are rendered from bindings, not hardcoded letters. Preserve Ctrl+C/V/Z/Y/S/O versus plain-key actions and H/V blueprint transforms as explicit contexts. Rebinding rejects overlapping chords in the same context, permits intentional disjoint contexts and offers Restore defaults. Projects uses a new default F2 binding; preserve splitter's existing J accelerator and all current disjoint-context bindings. Display actual defaults in Controls.

UI-only scale, quickbar shortcuts, binding overrides and dismissed hints use a versioned optional `relight.ui.v1` localStorage preference record, next to `session.ts` conventions, outside SimState and replay hashes. Default safely on absent/invalid/unknown entries or unavailable storage. Do not modify old save envelopes for these preferences. Track/pin selection is session presentation state, validated on load and cleared if no longer known. Persistent achievements must use genuine saved gameplay facts, never a UI preference as proof of completion.

## Capability relocation inventory

All entries refer to existing game source; migrate handlers, not just visibility. A shell adapter keeps each usable until its target task moves it.

| Existing source/capability | New destination / owner |
|---|---|
| `panel.ts` header view switch, copy link, slot save/load; `session.ts` snapshot/export paths | Map access; Pause save/settings; UI-01 adapter, UI-06 completion |
| `main.ts` goal/threat controls; `worldScene.ts` key/tool/HP/power/clock corners | Core HUD, contextual hint, G/K known-target navigation; UI-02 |
| Former `panel.ts` speed buttons | Removed under UI-08; Pause remains in Menu / P |
| `createCampaignGuide`, station/radio/later-station/workshop/Turbine/recruit/schematic information | Projects: known entries only, selected inspection, item help; UI-05 |
| `panel.ts` upgradeRadio and core repair buttons | Radio's distinct upgrade card and base inspection/Management; UI-05 |
| `togglePockets`, `openPocketsAt`, pocket/machine counts, take/put, craft controls | Inventory/Home/chest/stop transfer with separate named stores and existing quantities; UI-04 |
| `buildCatalogue`, `toggleBuild`, rifle/tool choice, pipette, rotate, remove, belt paths, undo/redo | Build + stable quickbar and placement controls; UI-03 |
| `openInspectionAt`, `openRoutingAt`, recipe/filter/priority | One identity-based inspection component; UI-04 |
| `factoryStatistics`, buffers, production capacity/actual rates, local power, generators, ammunition, freight | Management + inspection details, never debug-only; UI-04 |
| `openStationRoute`, request targets/export reserves, inbound reservations, route clear/show | Logistics/stop inspection; UI-04 |
| `openTruck`, boarding, cargo loading/unloading | Truck inspection with local legal actions and separate cargo; UI-04 |
| `createClipboardPanel` copy/paste/rotate/mirror/removal preview/apply | Build tools tab + placement toolbar, including cancel/undo; UI-03 |
| `createBlueprintLibraryPanel` folders/name/icon/import/export/delete/update and orders/manual build/pause/resume/cancel | Build library + Management construction orders; UI-03 migration, UI-06 end-to-end |
| `createTruckWorkPanel` supply chest/orders/packed transfers/start/source/retry/pause/resume/stop | Management construction delivery + truck inspection; UI-04 migration, UI-06 end-to-end |
| `panel.ts` legacy territory/front list/ring ordering/facilities/survivors, claim preview and assembler action | Legacy-profile management and contextual panels remain accessible; UI-01 adapter, UI-06 smoke |
| `panel.ts` summaries/telemetry download, replay tools; `main.ts` diagnostics; map coordinates/zoom/tile count | Developer toggle; useful stock/consumption summaries also in Management; UI-06 |
| `tooltip`, `toast`, event descriptions and long keyboard/tutorial paragraphs | Short contextual feedback + recoverable Help; keyed alert inbox for ongoing conditions; UI-02/05 |

Do not hide player-needed circuit load, buffers, loss history, supply shortage or stock scope simply because the old section was marked debug. Legacy-only counters never appear as campaign facts.

## Flow contracts and authoritative data

### Opening and tracking

Extend campaign branch of `packages/sim/src/goal.ts:currentGoal`; preserve legacy goal IDs/behaviour. Current code treats any excavator/assembler/mixer as a factory and relegates Home supplies to supporting prose. Replace that presentation predicate with a structured next-action result consumed by the HUD; no new quest framework.

Priority: actual down/recovery support; selected known objective (default Start your workshop); next actionable unmet constraint. Use `engineer.inv`, `chestCount`, placed machines and `inspectMachine`/production observations. If a useful machine exists, inspect its current operation before suggesting another. If no useful machine exists but pockets hold a packed excavator or sufficient authoritative build cost, show “Place an Excavator on a resource patch”. Otherwise, when Home has required missing supplies, show “Start your workshop / Collect your starting supplies from Home / Show location”, with “E · Open supplies” only when the real target resolver chooses Home. Partial pockets show only the remaining shortage; exhausted Home points to a known resource source/hand mining, not an impossible collection instruction. The suggested excavator is a useful first option, not a required progression gate.

Prior station restoration or saved actual production can satisfy earlier milestones without repeating them. Use existing restoredAt and saved production counters as evidence; if an older save has no historical fact, say “No working machine” rather than “You never built one”. Demolished equipment produces an operational rebuild/repair suggestion while achieved restoration remains complete. For saves without evidence of past production, current valid machinery is sufficient to skip collection. No inferred forever-complete flag based on briefly selecting a tool. UI-02 narrow tests cover partial inventory, prior machinery, completed restoration, demolition and load.

Projects tracks one known stable ID, defaulting to workshop opening then existing progression hints; allow explicit switch/untrack. Unknown/removed IDs fall back to the current valid opening/known objective. Never list future content just to fill space. Existing map cues, named streets, surveys and nearby discovery cues remain. Hints appear once in context, are dismissible and recoverable in Help; never repeatedly interrupt building/combat. Locating changes camera/marker only, and Return to engineer remains available.

### Building

Use `buildCatalogue.ts`, `CAMPAIGN_KINDS`, `lockReason`, `MACHINE_COST`, `FACTORY_TEXT`; `construction.ts` and ordinary factory commands remain validators. Group actual items: Production (excavator, assembler, mixer), Logistics (belts, underground, splitter, inserter, chest, track/stops/tram), Power (generator, poles/substation, lighting), Defence (turret, wall, barricade); Infrastructure holds blueprint/construction tools, not invented machines. Rifle is equipment. Collapse empty categories. Known locked equipment explains its recruit/requirement; unknown unlock content is omitted under existing discovery knowledge.

Ten stable shortcut slots use 1–0, defaulting to belt, inserter and excavator plus seven empty slots. Existing saved arrangements survive; retired IDs clear individually and undiscovered assignments conceal their identity. Catalogue drag replaces a destination; bar drag swaps. Off-bar removal requires empty UI space, with an explicit preview; world/invalid drops, Escape, blur and pointer cancellation restore the original. Click or a number selects normal paid placement. Add to action bar exposes destination buttons; Delete / right-click removes. These shortcuts never represent another inventory. Badges mean packed machines when present, otherwise buildable from carried materials via `buildStock.buildable`; the tooltip names that meaning. Rifle badges are rounds. No remote Home stock is counted.

`itemIcons.ts` supplies original 64-unit SVG item illustrations, reused by catalogue, dock, backpack and drag ghost. Default slots are 60 logical px; the dock is one compact row. Higher scale/narrow layouts reflow entry controls and preserve all ten slots. Build has four category tabs, an icon/name grid and one details area with real purpose, cost, shortage or lock reason. Undiscovered equipment stays omitted. Blueprint and construction adapters remain in a secondary disclosure.

Reuse WorldScene placement/underground pair/belt path/removal previews, actual dimensions/orientation/connections and sim refusal result. Show one precise reason at the cursor, e.g. actual collision/reach/material refusal. R rotates, Escape cancels, repeated click continues placement until explicitly cancelled (shortage keeps selected shortcut and reports reason); preserve right-click removal and Ctrl history semantics. Blueprint ghosts remain inert orders, with costs and stock/route waiting reasons visible in their own tools.

### Inspection and inventory

`inspection.ts:inspectMachine`, `machineCircuit`, `factoryStatistics`, `goal.ts:machineStatus`, `routing.ts` and `view.ts:inspectionView` are the baseline. Card order: name/location → operating statement → actual useful output → inputs → blockers → legal actions → Details. Selected ID becoming absent yields “Machine removed” and closes stale actions; never retarget by reused coordinates. Out-of-reach inspection is readable but local mutations remain checked by `dispatch`.

Current `machineStatus` returns a primary reason. UI-04 extends a **read-only sim query** for multiple applicable blockers using the existing power/input/output/routing predicates; it must not infer states from prose or alter production transitions. Priority: disabled/destroyed, missing connection/power, missing input/fuel, blocked output, idle/no demand. Show running when actually running, and secondary constraints only if true (e.g. future input shortage labelled separately). Do not invent machine pause toggles or “Paused by you” where none exists. Explain “No local load” as neutral, not red failure. Link missing input to `itemGuide` or a known source; unknown sources stay unknown.

Keep measured observation window, nominal output, power-limited capacity, local circuit supply/load/demand and global production clearly separate. No measurement yet is “Waiting for simulation time”, not zero throughput. Buffers/platform/arrivals/reserved inbound, Home stock, pockets, truck cargo and project delivery never sum into one spendable stock. Existing recipe, filter, priority, station requests, cargo, repair, craft and transfer commands retain capacity/reach/ownership checks. No forecast unless explicitly supported by authoritative data.

Backpack is a temporary window, independently opened with I/Tab. Owner feedback after UI-08: plain Tab closes an open Backpack or paired storage even with a slot or quantity field focused, then restores world focus; holding Tab does not toggle repeatedly. Shift+Tab retains backwards focus navigation; other panels and Pause retain their normal Tab behavior. Home E or targeted chest/station interaction opens Storage & Backpack side by side: the player’s Backpack is on the left and the interactable’s inventory is on the right. At narrow widths, Backpack comes first above Storage; keyboard and reading order match. The pack renders all 40 actual slots (legacy truck 200); small windows scroll. Stack positions use optional authoritative `engineer.pack` allocation reconciled against existing `engineer.inv` quantities. Move/split/merge/swap/sort commands conserve quantities, reject stale layouts and honor occupied-slot capacity. Legacy kits reserve ten cells each; reserved cells cannot be split/moved. Old saves need no mandatory new fields, and legal consumption cannot leave displayed phantom items.

Storage uses its existing quantity pools and actual per-stack slices; incoming transfers use the simulation’s first compatible/free slot. Shift-click, drag and quantity buttons share the ordinary chest transfer command. The command rechecks reach, source totals and selected stack allocation; partial results report the actual amount moved. Station platform/arrival capacities remain separate and the freight adapter is retained. Inventory drops onto the world or action bar cancel. No world item dropping or new capacity is introduced.

### Restoration

Use `campaignGuide.ts:campaignDiscoveries`, `knownSite`, `DiscoveryInfo`, `expansion.ts:campaignSite/siteCheck/deliverSite/restoreSite/collectTramKit`, `campaignTurbine.ts` and recruit/discovery checks. Add structured read-only requirement/power/transfer previews there as needed, shared with the target prompt. Never parse `describeSite` prose into rules.

Each card: benefit, service circuit with material nodes and power node, equivalent readable checklist, carried amounts and identified stored amounts separately, next legal action. Only actual material requirements appear (Turbine includes concrete); no invented dependency wires. Filled nodes reflect delivered stock, not stock elsewhere. Completed restoration consumes/reset delivery buffers in current code: show “Restored · materials consumed” from restoredAt, not 0/N incomplete. A later power outage shows service offline without undoing completion.

At the site, existing action is **Deliver and restore** (`deliverSite` followed by `restoreSite`), not two fictional phases. Preview exact amounts transferred, capped by remaining requirement and carried stock. Partial delivery remains available even without power if sim permits; say “Deliver X; restoration still needs power”. Ready but no transfer reads “Restore”. Remote view offers Track/Show location; never dispatch remotely to bypass locality. Prevent duplicate enqueue per activation gesture and re-read current state on click. Show legal rejection beside the action, available to focus/click even when blocked. Station kit collection shows remaining kit and capacity-limited partial collection. Turbine switches, recruits, workshop repairs and schematic rewards use their distinct commands; do not pretend all sites have the same commissioning timer.

Radio discrepancy resolved by code inspection: `EXPANSION.radio` is currently 20 steel + 10 copper restoration; `CAMPAIGN_THREAT.radioUpgradeSteel/Copper` are 15 steel + 10 copper precision upgrade. These are different actions, not a confirmed price mismatch. Label “Restore radio tower” versus “Upgrade radio precision”; explain the latter adds approach/composition information. Render the constants/checks dynamically, never these prose numbers. Respect power and received-warning persistence. Circuit completion gets one short close animation; optional existing relay sound only, with visual equivalent and reduced motion.

### Alerts and time

`campaignThreat.ts:campaignWarning/knownCampaignThreat`, saved defence schedule/warning and `campaignClock` determine displays. No hardcoded Night 3 in HUD or reusable tutorial text. Separate scheduled assault (amber clock), detected nearby threat (known local entity information), base under attack (red, actionable known location), and production warning (amber, machine location). Future attack date never means “safe”. Do not use hidden roster counts, targets or undiscovered enemies.

Key condition alerts by type + known entity/base ID. Update one row, increment repeat count for transient repeats, and clear when the query says resolved. At most one highest-priority urgent strip + three short transient notices; inbox preserves dismissed event history, marks resolved conditions. Base damage/engineer danger outranks schedule, then production; received radio messages remain historically received during outage. Dismissal does not clear a real danger indicator. Click/focus Locate uses known navigation only; missing target says unavailable. Avoid duplicated `currentGoal.support`/defence/header warnings. Keep critical state visible during ordinary interactions; paused state must be explicit.

## Ordered implementation packages

All IDs below have prefix RI-02B. Requirements above are shared acceptance, not optional inspiration. File paths in this section are relative to repo root; named new files are explicitly proposed. Each task appends concise evidence to `docs/evidence/ui-redesign/README.md` (new evidence note), with actual command results, images and remaining gaps; do not create seven overlapping reports.

### RI-02B-UI-01 — Shared visual tokens and shell/input foundations

- Problem/result: dashboard layout and split keyboard ownership; real entry uses shared readable controls with safe layers.
- Targets/callers: `packages/game/index.html`, `src/style.css`, `src/main.ts:createPanel` setup and global key handler, `src/panel.ts:createPanel/closeAll`, `src/worldScene.ts` pointer/keyboard handlers, `src/view.ts:hudInset`, `src/controls.ts`; extract new `src/uiShell.ts` as specified.
- Reuse/prerequisite: existing DOM factories, session APIs and Phaser resize; P9-05 and phase planning. Reinspect post-P9 UI changes before extraction. Build shell routing and capability adapters before removing containers.
- State/actions: UI selection/focus/layers only; retain dispatch/queue/setSpeed. Implement Pause/input ownership and safe missing-save error surface. Empty selection has no inspector; lost focus cancels held input; disabled controls retain reasons. Example: “Paused / Resume”.
- Acceptance/check: actual campaign entry mounts shell; every relocation row accessible through adapters; one drawer policy, Escape hierarchy and pointer/wheel/text isolation work. Real browser type in blueprint name, scroll a drawer, click close while placement selected: no movement/fire/build beneath it. Run useful game typecheck/build; narrow input behaviour checks where feasible. No redesigned objective/quickbar/data model yet; UI-02–05 own these.

### RI-02B-UI-02 — Core HUD, interaction and truthful objectives

- Depends UI-01. Problem/result: empty pockets but build-first instructions and duplicate corners; one useful next action plus visible engineer/time.
- Targets: `src/main.ts:updateThreatControls/toggleView`, `src/panel.ts` goal update, `src/worldScene.ts` HUD/target resolution, `src/cityMapScene.ts`, `src/mapScene.ts`, `src/view.ts`; `packages/sim/src/goal.ts:currentGoal`, `campaignGuide.ts`, `campaignThreat.ts`, `rules.ts:campaignClock`.
- Reuse: existing names, discovery rules, G/K and map geometry; bounded minimap rendering from existing map data with a shared discovery filter, no second scene simulation, exploration grid or reveal system. Shared interaction query in sim may be extracted from existing legal target checks; prompt and E handler must choose the same target.
- States/actions: opening contracts above; snapshots loaded paused, partial supplies, prior factory/restoration, removed machine, no known target, radio outage, engineer down. Show location is camera-only; actual E remains existing interaction. Display “Collect your starting supplies from Home” only when valid.
- Acceptance/check: no permanent mixed sidebar; no duplicate canvas HUD; compact/collapsed minimap shows player and only existing known destinations. Run narrow goal/load/work-ahead/discovery tests and walk Home→supplies→build access. Compare opening screenshot with baseline. Full Projects/restoration belongs UI-05; catalogue UI-03; preserve and integrate saved player pins/names from P9-03; do not reimplement their commands.

### RI-02B-UI-03 — Build catalogue, stable quickbar and placement

- Depends UI-01/UI-02. Problem/result: scattered keys/locked controls; find and place useful equipment with truthful cost and stable shortcuts.
- Targets: `src/buildCatalogue.ts:buildCatalogue/toolForKey`, `src/controls.ts`, `src/factoryStrings.ts`, `src/panel.ts:toggleBuild/onPick`, `src/worldScene.ts:setTool/blueprint` and placement/removal handling, `src/clipboardPanel.ts`, `src/blueprintLibraryPanel.ts`; `packages/sim/src/construction.ts` validators, `flow.ts` dimensions/costs and routing previews.
- Reuse/actions: ordinary factory placement/history/path commands, paid blueprint stamps, existing order/library commands; no new economy. Add UI assignment controls and preferences adapter; UI-06 completes settings/remapping interface.
- States/layout: empty/zero stock, material shortfall, unknown unlock, known locked, full pockets, collision/reach, partial ghost order, cancelled drag and stale removal preview; exact invalid reason beside cursor, useful category purpose copy. Unknown slots do not reorder. Preserve tool selection on refusal.
- Acceptance/check: collect stock, select Excavator, preview/place/cancel/rotate, repeat placement, packed-first placement, material-paid placement, locked refusal; change shortcut and ensure inventory unchanged; smoke copy/queue/removal adapters. Focused existing construction tests only for changed contracts; actual screenshot of build/invalid preview. UI-04 owns diagnostics and UI-06 exhaustive relocated-flow check.

### RI-02B-UI-04 — Inspection, inventory and management

- Depends UI-01/UI-03. Problem/result: numbers obscure actionable stalls and stock scope; one predictable card plus separate transfer stores.
- Targets: `src/panel.ts:openInspection/openPocketsAt/openTruck/openStationRoute`, `src/view.ts:inspectionView/transportView`, `src/factoryStrings.ts`, `src/truckWorkPanel.ts`, `src/campaignGuidePanel.ts` item links; `packages/sim/src/inspection.ts`, `goal.ts:machineStatus`, `routing.ts`, `campaignPower.ts`, `truck.ts` and `itemGuide.ts:itemGuide`.
- Reuse/actions: inspection identity and observation window, recipe/routing, chest/cargo, freight forms and factoryStatistics. Add multi-blocker read-only sim query, not a second production engine. UI-01 layer shell is required.
- States/layout: removed ID, out of reach, empty/full store, no measured time, multiple blockers, no local load, powered running, disabled core, interrupted truck route. Example “Missing steel” + “Local circuit: no supply” only when both actual conditions apply. Transfer preview keeps pockets/Home/platform/arrivals/cargo separate.
- Acceptance/check: inspect a working/stalled machine, restore input/power and see warning clear; mutate recipe/filter through legal commands; out-of-reach transfer refuses without stock loss; locate known source; sample station requests and truck cargo. Narrow `inspection.test.ts`/routing/conservation tests if queries/actions change. Open working and stalled screenshots. Project circuits/alert grouping belong UI-05, broad production redesign is excluded.

### RI-02B-UI-05 — Projects, service circuits and actionable alerts

- Depends UI-02/UI-04. Problem/result: future-project walls and ambiguous requirements; one tracked known project with real delivery, power and completion.
- Targets: `src/campaignGuidePanel.ts:createCampaignGuide`, `src/panel.ts` defence/project controls and toast, `src/main.ts:describe/updateThreatControls`, `src/worldScene.ts` site interaction; `packages/sim/src/campaignGuide.ts:campaignDiscoveries`, `expansion.ts`, `campaignTurbine.ts`, `campaignRecruits.ts`, `campaignDiscovery.ts`, `campaignThreat.ts` read models/checks.
- Reuse/actions: existing GuideAction command sequences, site deliver/restore, upgradeRadio, collectTramKit, recruit/discovery/Turbine/repair commands; shared structured transfer/requirement queries. UI tracking cannot bypass local on-foot checks.
- States/layout: undiscovered omitted, remote read-only, partial delivery without power, ready, already restored with emptied buffers, service outage, reward partly collected, upgrade already bought, live/resolved threat and unavailable location. Circuit/checklist and preview copy follow contracts above.
- Acceptance/check: track known site remotely; reach site, preview partial transfer, deliver, supply power, restore and collect limited kit; verify distinct radio costs; show a warning from a labelled prepared known-threat state and clear it through actual resolved state. Narrow duplicate-activation/availability/load tests, screenshot project card and inspect it. No new projects, audio engine, attack schedule or balance.

### RI-02B-UI-06 — Scaling, accessibility, settings and full integration

- Depends UI-02/UI-03/UI-04/UI-05. Problem/result: viewport collisions, inaccessible help and fragile preferences; every relocated control remains usable at PC scales.
- Targets: `src/style.css`, proposed `src/uiShell.ts`, `src/controls.ts`, `src/session.ts` optional preference boundary, `src/main.ts`, `src/worldScene.ts`, all four existing domain panel modules plus `src/panel.ts`. Existing `packages/sim/src/save.ts` remains unchanged unless a demonstrated gameplay-schema requirement exists; UI preferences need none.
- Reuse/actions: slot/profile save/load/export, normal-speed pause and map switches; complete scale 100/125/150%, reduced-motion setting respecting OS by default, binding editor/reset, shortcut/hint persistence and recovery. No audio/mute control pretending sound exists; future audio plugs into settings.
- States/layout: missing/malformed/storage-denied prefs, old save without prefs, profile mismatch, empty save slot, long labels, keyboard-only, maximum scale, modal focus and blurred held keys. Save failure is actionable; load preserves existing unsaved-progress confirmation. Draft text never invokes world shortcuts.
- Acceptance/check: 1366×768 at 100/150%, 1920×1080 at 100/125/150%, 2560×1440 at 125%, 3440×1440 at 100/150%; verify actual text size/contrast and focus, scroll/reflow, no world-input leaks. Exercise every relocation row once, including library import/export, orders/manual completion, truck source/retry, freight and legacy profile smoke. Run repository code checks once at meaningful integration checkpoint, narrow preference/save/input regressions; save/reload ordinary current campaign and old saved profile. Record performance regressions without claiming old light-map stalls fixed.

### RI-02B-UI-07 — Actual visual review and targeted refinement

- Depends UI-06. Problem/result: functional code can still be confusing; inspect the delivered interface and fix observed blocking findings.
- Targets: actual `packages/game` entry and only affected UI modules above; `docs/evidence/ui-redesign/README.md`, screenshots and a short current-build follow-up guide/observation area there. No parallel tracker or repeated task reports.
- Reuse/prerequisite: UI-06 verified build and baseline seed/state; record exact source identity and prepared-state provenance. Reuse frozen fresh checkpoint if compatible, otherwise disclose migration/difference. Never overwrite frozen EX-08D or its blank human record.
- Acceptance/check: walkthrough collect supplies → useful placement → working/stalled inspection → known project track/delivery → representative warning. Capture and **open** redesigned opening at baseline 1280×720/0:00/seed3/camera0.65 plus build, machine and project images and required scaling samples. Record camera adaptations if shell changes visible area. Check character identification, hierarchy, legibility, collision, truthful actions and every urgent indicator. Fix blocking observed issues, rerun only affected checks; one targeted refinement pass then stop when acceptance is met.
- Recommend one brief independent fresh-player session: identify engineer, obtain supplies, place machine, explain a stall, find a project; record actual hesitation, mistakes and assistance, no invented intuition score. If nobody is available, feedback remains pending, separate from developer walkthrough, without blocking verified implementation. Existing formal human gates remain human. No broad simulation batches or content expansion. Provide EX-08 with new-build evidence/follow-up context; old session observations retain their original build.

## Verification and handoff

Documentation run: docsync:check, freshness:check, unique IDs/dependency/local path checks. Actual baseline image is evidence only of the inspected old interface; no production UI or simulation changes in this run. Implementation: use existing typecheck/build and required repository checks at meaningful checkpoints, not after every cosmetic change. Narrow tests target stale objectives, illegal transfers, duplicate commands, missing preference defaults and input leakage. No colour snapshots or tests mirroring component markup. Label prepared/debug situations; explicitly record blocked browser/actions rather than substitute concept art. Keep existing performance and human gates open.

After P9-05, start **RI-02B-UI-01** with `/next` using `.claude/commands/next.md` (or ask the agent to implement RI-02B-UI-01 following that file). Read CLAUDE → PROGRAMME_STATE → PROGRESS → this spec/D-UI-01/D-UI-02, set only that task in_progress, implement its bounded scope, verify, append evidence, update tracker/handoff and release dependent rows only when their blockers clear. `npm run dev -w packages/game -- --host 127.0.0.1` is the existing development launch command; use a free port and preserve frozen checkpoints. No commit, push, merge or deploy without a new explicit request.

## Implementation notes

- 2026-09-09 — D-UI-03 / UI-01: shared tokens, shell/adapters and input/Pause foundation delivered. Candidate border changed to `#8DA79F` after its original raised-control contrast failed; measured final shared text/border pairs exceed the stated thresholds. Other token starting values and gameplay rules remain. Current objective/HUD and domain content await UI-02–05; full preferences/scaling remains UI-06. [Evidence](evidence/ui-redesign/README.md).

- 2026-09-09 — D-UI-04 / UI-02: core HUD, bounded known-destination minimap, shared sim interaction selection and structured campaign next actions delivered. Opening/known-objective tracking is presentation only; catalogue, deeper inspection/restoration and settings remain in their own tasks. [Evidence](evidence/ui-redesign/README.md).

- 2026-09-09 — D-UI-05 / UI-03: grouped discovered catalogue, stable customisable quickbar and cursor placement feedback delivered; existing paid construction/blueprint adapters preserved. Browser-only quickbar preferences begin `relight.ui.v1`; full settings/scaling remains UI-06. [Evidence](evidence/ui-redesign/README.md).

- 2026-09-09 — D-UI-06 / UI-04: operating-first inspection, multiple sim blockers, identity-preserving pin/removal handling and separate inventory/cargo previews delivered; settings/freight/truck commands preserved. Projects/service circuits remain UI-05. [Evidence](evidence/ui-redesign/README.md).

Manual mining feedback (owner follow-up): empty-hand rubble hover explains holding left-click and the current mining rate. While held, show real simulation progress toward the next item, collected amounts for that hold and current Backpack totals. Retain the receipt briefly on release. Explain pause, range, capacity, depleted rubble and machine-only sources; menus cancel mining and hide its feedback. This is presentation of existing commands and rules.

### RI-02B-UI-08 — Approved implementation amendment

The canonical PROGRESS row owns status. Scope is the owner’s supplied action bar/catalogue/Backpack design, paired storage, quiet local HUD and authoritative normal player clock. Evidence, narrow tests and inspected screenshots are appended in the existing UI evidence README; no new live tracker or unrelated phase is created.

Owner lighting follow-up (D-UI-11): campaign flashlight follows the world mouse position, with soft range/angle falloff and a small origin glow. UI input capture holds its previous direction. Only the visible lighting surface is recomposed as the pointer moves; static city lighting remains cached. Home is fully lit through the shared sim lighting rules.

D-UI-12 supersedes mandatory inserter extraction in campaign connection guidance. Always-visible cyan IN / amber OUT arrows identify usable machine/storage sides; hover and placement expand to every edge tile, with exact inserter pickup/drop tile outlines. Hover legend, inspection, catalogue and storage hints explain conveyor direction and automatic transfers. Recipe machines export finished items, not ingredients. Legacy simulation retains its prior extraction rules.


### D-UI-13 owner follow-up

Map clicks inspect stations or select known destinations; Shift-click retains pin creation. No map click issues walking commands. The main map uses a white player pointer, dark backing, mint halo and YOU label; the local minimap uses the same high-contrast treatment. Four permanent platforms show powered/unpowered status and their route. Their freight inspection reports automatic service and the number of powered stops. Build catalogue excludes public tram infrastructure.
