# Relight side of the matrix (from code + doc, 2026-09-04, b666c68 + Step 0)
Legend: built / planned (phase N, source) / not planned / never (doc section)

**Current UI scheduling:** RI-02B follows Phase 9 engineering/preparation (P9-05), under D-UI-02. See [current acceptance mapping](../STANDARDS.md#ui-acceptance-mapping--after-phase-9-d-ui-02). Earlier Phase 12 UI references below retain historical scope; brought-forward portions follow this mapping. No deferred human observation is completed or waived. PROGRESS remains the sole tracker.

## 1 Controls
movement: BUILT WASD 8-way, Shift sprint (stamina), Space dodge (D-B1-5, worldScene addKeys)
single placement w/ ghost+reason: BUILT (canPlace reason "walk closer", "10 steel from the pockets")
belt drag placement: NOT BUILT (dragging = camera only, !onFoot). doc §14 silent. → Phase 5
line placement / rotate: R BUILT; line NOT BUILT → Phase 5
mirror/flip: NOT PLANNED → with blueprints Phase 8
pipette: BUILT Q (also clears hand)
ghost placement (marker w/o item, Factorio ghost): NOT BUILT; no bots (§22) → Phase 8 (Line truck lays kits; ghosts as its order list)
undo/redo: NOT BUILT; command log exists (session record/replay, M6) → Phase 5 command layer
copy/paste: NOT BUILT; doc §14 "Blueprints and copy-paste from minute one" ← disagreement with build (slice has none) and with constitution Phase 8 → report
blueprints: NOT BUILT; §14 minute one, §19 tedium "blueprint from minute 1"; constitution Phase 8 → report doc-vs-constitution; score planned Phase 8
upgrade planner: NOT PLANNED (few machines; Mk1→Gunsmith is a recipe change) → Considered? no: Phase 8 decides after E17
deconstruction planner / area select: NOT BUILT → Phase 8 (one-action retreat, ring editor; "picking up interior turrets" tedium row)
hotbar: BUILT 1–9, 0 [ ] (TOOL_KEY_LINE)
quickbar paging: NOT PLANNED; ~20 buildings §13 → Phase 12 decides on count
rebinding: PLANNED Phase 12 (constitution "remappable keys")
controller: NOT PLANNED → decision
Steam Deck: PLANNED Phase 13 (ROADMAP §5 "Steam Deck verification") but no controller → decision

## 2 Logistics
belt lanes: doc §14 "two-lane"; sim flow.ts "belts are one lane" ← DISAGREEMENT (report, don't resolve) → Phase 5 build-with
belt drag with auto-underground: NOT BUILT; undergrounds Phase 5 → Phase 5
splitter priority: doc §14 yes → Phase 5; splitter filters: doc silent → Phase 5 decides (§19 clear-the-jam names priority only)
inserter filters / stack size: doc §13 "three inserters" (check names) → Phase 5 decides
item hover throughput: NOT BUILT → Phase 5
what's on this belt: items visible on belts BUILT (M2); map-view dashed-empty/jam marker (§4, §19) — Phase 5 data + layout pass
## 3 Information
production/consumption graphs: telemetry per-minute in sim (BUILT, export); no in-game graph; doc §4 puts "the line's rates" behind a debug key ← doc below baseline, report → Phase 12
power graph: HUD one number + brownout toast BUILT (§14); history graph → Phase 12
alerts click-to-jump: toasts with coords BUILT; jump NOT BUILT; §19 "click the edge → the camera lands" → Phase 6 (ROADMAP §5 "alert events land here")
recipe browser: PLANNED Phase 7 (survivor panel = recipe browser)
machine tooltip rates+bottleneck: describeMachine BUILT (partial: what it is, its street); rates/bottleneck reason → Phase 5 with recipes
alt-mode overlay: NOT BUILT → Phase 5 data, Phase 12 art
minimap: NEVER (§4 "It is not a minimap"; the map view is the full screen on M)
chart map with tags: PLANNED Phase 9 (map pins and charting)
## 4 Onboarding
tutorial mode: NEVER (constitution rule 8 "No tutorial screens. §11 is the tutorial")
rules as toasts: BUILT (~30 toasts main.ts)
hints that stop repeating: NOT BUILT → Phase 12 (E23)
in-game reference: build menu with costs BUILT; recipe browser Phase 7
## 5 Look and feel
machine idle/working: PLANNED Phase 12 art (§4 "single idle/active animation")
item movement on belts: BUILT
particles/feedback: PLANNED Phase 12 ("feedback density as the brief")
light: BUILT light map (M5); shadows NEVER (§4 "No shadow casting")
ambient sound / music: PLANNED Phase 12 (audio spec); nothing in game (no audio file)
screen feedback + toggle: NOT PLANNED → decision 5
## 6 Environment
terrain variety: BUILT districts as rubble type/density + river; biomes NEVER beyond districts (§17)
decor/props: PLANNED Phase 12 (facility silhouettes, art)
water: BUILT river inert tiles (§22 no fluids)
day/night: NEVER (§22, §23)
ambient life: NEVER (§22 "People" — survivors are sprites)
weather: NEVER (§22)
## 7 Map and world
exploration/charting: PLANNED Phase 9 (charting); dark blocks drawn navy BUILT
fog: dark = navy mottle BUILT (not fog)
tags/pins: PLANNED Phase 9
long-distance travel: truck Phase 5 (driven), Line truck Phase 8, trams Phase 5
where am I: engineer dot on map BUILT; M returns to the engineer BUILT; names NOT PLANNED (cat C)
## 8 Meta
save/load: snapshot via URL + "Snapshot saved" dev button BUILT (dev); slots/versioning Phase 12; autosave "hourly" (constitution Phase 12) ← below baseline (Factorio default 5 min) → flag
cloud: NOT PLANNED (Phase 13 Steamworks bridge; not stated)
settings (graphics/audio/UI scale/colour-blind): PLANNED Phase 12
localisation: NOT PLANNED → decision
achievements: PLANNED Phase 13 (only measured things)
mods: NOT PLANNED → decision (sim is data-driven recipes.ts, no mod API)
multiplayer: NOT PLANNED → decision (deterministic step(state, commands) makes lockstep cheap to keep open)
replays: command log + replay BUILT (harness/dev); screenshots NOT PLANNED
## 9 Performance
late-game scale: PLANNED Phase 11 targets (313 blocks, 4,000 belt segments, 200 machines, 60 fps, 2019 laptop 1080p)
sim/render separation: BUILT (20 tick/s sim, render loop, 4×/16× speeds setSpeed)
## A Body
pockets grid: BUILT as a list panel (40 stacks), transfer buttons; grid/shift-click NOT BUILT → Phase 12
hand item + clear: BUILT (right-click / Q empty clears; toast "in hand")
reach + out-of-reach: BUILT reach 8, "walk closer" ghost reason
death/respawn: BUILT 10 s, pockets intact, toasts; corpse NEVER (§22 no death penalty)
HP display + regen: BUILT (HUD? check) regen 5 HP/s after 5 s
hit feedback (markers, numbers): NOT BUILT → Phase 12
own light: BUILT behind ?handlamp=1 (M5) ← flag: URL flag not a rule → Gate B/decision
vehicle: PLANNED Phase 5 truck driven, Phase 8 Line truck
## B Threat readouts
wave timer: NEVER (§23 horde/timed waves); equivalent = awake-block ring filling with the bloom timer (§4 map encoding) — check built
next-wave composition: bloom toast names crawler/shade counts BUILT
spawn/direction: born on the segment ridge; red-orange frontage bar BUILT (map)
under-attack alert + jump: toast BUILT; jump Phase 6
off-screen indicators: NOT PLANNED → Phase 6 row
turret range overlay on placement: PLANNED Phase 6 (ROADMAP §5) — Floodlight cone drawn? check
ammo status on turrets (world view): pip on kerb → layout pass; hopper count in describeMachine
threat overlay on map: rot density mottle BUILT
post-attack report: "Block lost — reason" toast BUILT; mothballed machines Phase 6
difficulty readout: Phase 10 slider
## C City navigation
named districts/streets/blocks: NOT PLANNED (blocks are ids "(x,y)"; toasts say "Block (12,7)")
landmarks from afar: facilities silhouettes (§4, §19 "visible 6 blocks out") Phase 12 art; map icon Phase 9/12
compass/north: NOT PLANNED
you-are-here + map↔world link: BUILT (M at the same block; click to walk)
street signs/wayfinding props: NOT PLANNED
player pins: Phase 9
charting of unexplored: Phase 9
distinct district silhouettes: Phase 12 (ambient by district; art per district)
