# Factorio — baseline reference for the standards audit (researched 2026-09-04)

Sources: official wiki (wiki.factorio.com), Friday Facts (FFF), Steam store, forums. "not found" = no URL located in this pass.

## 1. Controls
- Movement — WASD — https://wiki.factorio.com/Keyboard_bindings
- Rotate — R (Shift+R reverse) — same URL
- Flip/mirror — H horizontal, V vertical; blueprints with rail signals/stops cannot flip — https://wiki.factorio.com/Blueprint (flip arrived in 1.1: https://factorio.com/blog/post/fff-364)
- Pipette — Q "picks items from your inventory used to build the currently selected entity" — Keyboard_bindings
- Ghost placement — Shift+LMB "Build ghost"; Shift = force build, Ctrl+Shift = super-force — Keyboard_bindings, Blueprint page
- Undo — Ctrl+Z, added 0.17: "pressing Ctrl+Z cancels all the ghost entities… marked for deconstruction" — https://www.factorio.com/blog/post/fff-255
- Redo — Ctrl+Y, new in 2.0 with undo/redo history — https://factorio.com/blog/post/fff-412
- Copy/paste/cut — Ctrl+C/V/X (0.17); cut also marks for deconstruction — FFF-255
- Blueprints — Alt+B, area-select then icon setup (4 icons auto-suggested, editable), export string, books — https://wiki.factorio.com/Blueprint
- Blueprint library — "My blueprints" (cross-save, private) vs "Game blueprints" (per-save, shared with force); B key; quickbar can pin library items; since 0.15 — https://wiki.factorio.com/Blueprint_library
- Upgrade planner — Alt+U, 24 from→to pairs, blank = auto-upgrade belts/inserters/assemblers/furnaces, can downgrade; 0.17 — https://wiki.factorio.com/Upgrade_planner
- Deconstruction planner — Alt+D, drag area, whitelist/blacklist, trees/rocks-only toggle, Shift-drag cancels; 0.9 — https://wiki.factorio.com/Deconstruction_planner
- Belt drag — dragging locks to belt direction (1.1) and "automatic underground belt traversal" over obstacles — https://factorio.com/blog/post/fff-363, https://factorio.com/blog/post/fff-364
- Quickbar — 10 bars × 10 slots (100 shortcuts), 1–4 visible; keys 1–0, Shift+1–0 selects a bar, X rotates; "The toolbelt also functioned as inventory space; the quickbar does not"; optional "Pick ghost item if no items are available" — https://wiki.factorio.com/Quickbar
- Shortcut/tool bar — panel right of quickbar for planners/blueprint tools, pinnable (0.17) — https://www.factorio.com/blog/post/fff-280
- Rebinding — Settings > Controls, click and press combo; reset available — Keyboard_bindings
- Controller — 1.1.83 (2023-06-14): "Added controller(gamepad) support… Added contextual hotkey hints" — https://forums.factorio.com/viewtopic.php?t=106660
- Steam Deck — tested on devkit in FFF-368 (5h45 battery, 60 FPS); Twinsen said verification was in progress Mar 2023; an official "Verified" badge: not found — https://factorio.com/blog/post/fff-368, https://forums.factorio.com/viewtopic.php?t=105545

## 2. Logistics affordances
- Two lanes — "Belts of all tiers have 2 lanes"; 15/30/45/60 items/s per tier — https://wiki.factorio.com/Belt_transport_system
- Belt spacing — 8 items per tile (4/lane), 1/2/3 px per tick, exact 15 items/s — https://www.factorio.com/blog/post/fff-276
- Auto-underground on drag — see FFF-364 above
- Splitter filter + input/output priority — since 0.16.17; circuit connection 2.0.67 — https://wiki.factorio.com/Splitter
- Inserter filters — 2.0: filter inserter removed, every inserter has 5 filters behind a "use filters" checkbox — https://www.factorio.com/blog/post/fff-393
- Stack size / hand-size override — not found on wiki pages fetched
- Belt read — circuit-connectable since 0.13; 2.0 "whole belt reader" reads the entire transport line through undergrounds, stops at splitters — https://wiki.factorio.com/Transport_belt, https://www.factorio.com/blog/post/fff-405
- Hover throughput — items/s tooltip for machines/belts: not found as a base feature (request from 2016: https://forums.factorio.com/viewtopic.php?t=33729)

## 3. Information
- Production statistics — P key; items/fluids, buildings, pollution, kills tabs; 5 s to 1000 h/all-time; click icons to filter — https://wiki.factorio.com/Production_statistics
- Stats GUI redesign, unified search — https://www.factorio.com/blog/post/fff-337
- Electric network info — click a pole: satisfaction/production/accumulator bars, per-entity graphs, per-network only — https://wiki.factorio.com/Electric_system
- Alerts — global alerts + per-entity warning icons; "Alerts can be clicked to show the position… on the map"; edge arrows; /alerts tuning — https://wiki.factorio.com/Alerts
- Recipe browser — Factoriopedia (2.0): ingredients, "all the recipes using this item as ingredient", Alt+click anything, Alt+←/→ history; before 2.0 this was mod territory (FNEI) — https://www.factorio.com/blog/post/fff-397
- Tooltips — 0.17 tooltip rework: state info, ratios, categorised properties — https://www.factorio.com/blog/post/fff-318
- Status reasons — engine status set: working, no-power, low-power, no-ingredients, full-output, item-ingredient-shortage, no-fuel… — https://lua-api.factorio.com/latest/types/EntityStatus.html
- Alt-mode — Alt toggles recipe/content icons on entities; toggleable from shortcut bar — Keyboard_bindings, https://wiki.factorio.com/Shortcut_bar
- Minimap/map — minimap with buttons above it (production, blueprint library); map tags placed by right-click in map view; Ctrl+Alt-click posts a GPS tag to chat — Production_statistics, https://steamcommunity.com/app/427520/discussions/0/1621724915807722027/, https://wiki.factorio.com/rich_text

## 4. Onboarding and help
- Tutorial — five scripted levels (Crash, Mining outpost, Power plant, Science and automation, Abandoned rail base); optional — https://wiki.factorio.com/Campaign
- Tips and tricks — dependency-unlocked (train tips after railway research), live simulations in the GUI, "Mark as read", categorised searchable index (1.1) — https://factorio.com/blog/post/fff-361; earlier iteration https://www.factorio.com/blog/post/fff-208
- In-game reference — Factoriopedia (FFF-397)
- Hints that stop repeating — tips are marked read and unlock dependents (FFF-361); contextual hotkey hints toggle in Settings>Interface (1.1.83)

## 5. Look and feel
- Machine activity — 2015 complaint that assemblers lacked visible activity state; today status is surfaced via tooltips/status icons — https://forums.factorio.com/viewtopic.php?t=18366, FFF-318
- Item movement — integer-pixel belt movement per tick (FFF-276)
- Visual feedback — FFF-280 "Visual feedback is the King": train path previews, toolbar — https://www.factorio.com/blog/post/fff-280
- Destruction particles — FFF-325 new explosions/particles; FFF-343 decoratives emit particles on impact — https://factorio.com/blog/post/fff-325, https://factorio.com/blog/post/fff-343
- Light/shadow — shadows split from sprites, separate atlases, lights rendered at lower resolution — https://factorio.com/blog/post/fff-227, https://factorio.com/blog/post/fff-281
- Ambient sound — 2.0: zoom-dependent wind, tile/entity ambience, sound accents synced to animation frames, attenuation curves — https://www.factorio.com/blog/post/fff-396
- Music — surface-selected tracks, randomised pauses, procedurally variable interludes — https://www.factorio.com/blog/post/fff-407
- Screen shake/flash + accessibility toggle — not found

## 6. Environment
- Terrain — moisture (grass↔desert) and terrain-type (red desert↔sand) controls, cliffs, trees, 9 presets, exchange strings — https://wiki.factorio.com/Map_generator
- Decoratives — tile placement removes only a share of decoratives (25 % for most tiles) — https://forums.factorio.com/viewtopic.php?t=74425; wiki page not found
- Trees — absorb pollution, block building, 4 wood — https://wiki.factorio.com/Tree
- Water — impassable, landfill removes it, offshore pump, shallow water walkable, defensive barrier — https://wiki.factorio.com/Water
- Day/night — 7-minute day; flashlight/lamps auto at night; solar 60 kW peak, 42 kW average, zero at night — https://wiki.factorio.com/Time, https://wiki.factorio.com/Solar_panel
- Ambient life — fish in water, minable, heal 80 HP — https://wiki.factorio.com/Fish
- Weather — none on Nauvis found; Fulgora night lightning damages buildings — https://wiki.factorio.com/Fulgora
- Landmarks — not found

## 7. Map and world
- Charting — radar 7×7 chunks continuous + 29×29 slow scan; player/roboport chart 5×5; explored area persists — https://wiki.factorio.com/Radar
- Remote view (2.0) — "use remote view for everything you can do locally": open machines, set filters, rotate, deconstruct from the map — https://www.factorio.com/blog/post/fff-380
- Map tags — right-click in map view; GPS tags — see §3
- Travel — rail "faster and provides higher throughput than belts and robot logistics" — https://wiki.factorio.com/Railway; spidertron remote-controlled, crosses water, 46–102 km/h — https://wiki.factorio.com/Spidertron
- "Where am I" — respawn at world centre/spawn; player charts around self — Radar, https://wiki.factorio.com/Character

## 8. Meta
- Saves/autosave — rolling autosaves, default 3 slots — https://wiki.factorio.com/Settings; 10-min default interval — https://steamcommunity.com/app/427520/discussions/0/3828663748252611076/
- Cloud — Steam Cloud listed on store — https://store.steampowered.com/app/427520/Factorio/
- Settings — UI scale slider, quickbar rows/width, technology-GUI pause — Settings wiki; colour-blind mode: not found (community mods only, https://mods.factorio.com/mod/colorblind_ultimate)
- Localisation — 30 languages — Steam store
- Achievements — 88 (59 base + 29 Space Age), Steam and standalone; console script commands and non-freeplay disable them; mods use a separate set — https://wiki.factorio.com/Achievements
- Mods — mods.factorio.com portal + in-game manager; Lua 5.2, data (prototype) stage and control (runtime) stage — https://wiki.factorio.com/Modding
- Multiplayer — deterministic lockstep, 65,535 theoretical, "well over a hundred" seen, latency hiding since 0.12, headless server, LAN/public browser — https://wiki.factorio.com/Multiplayer
- Replays — input-only recording, no seeking, breaks on version/mod change — https://wiki.factorio.com/Replay_system

## 9. Performance expectations
- UPS vs FPS — UPS = game updates/s, FPS = drawn frames; FPS never above UPS; 60 nominal — https://wiki.factorio.com/Tutorial:Diagnosing_performance_issues
- Megabase scale — Ryzen 3900X drops below 60 UPS at "24k assemblers + 58k inserters", "100k active belt lanes", "100k active bots"; 1k SPM on an old i5 stays at 60; "30 and less is … the end of the megabase" — https://forums.factorio.com/viewtopic.php?t=91188; 10k SPM at 60 UPS reported — https://forums.factorio.com/viewtopic.php?t=129332
- Game speed — `/c game.speed` (0.01 min, 1 default) with achievement-disabling warning — https://wiki.factorio.com/Console

## Specific claims
- (a) FFF-191 quickbar — **confirmed**: "simply a shortcut bar to the player's main inventory… item slots can only be filters"; unavailable item ⇒ "you grab a ghost of that item in your cursor" — https://www.factorio.com/blog/post/fff-191
- (b) FFF-280 — **confirmed**: titled "Visual Feedback is the King" (train GUI path previews, tool bar) — https://www.factorio.com/blog/post/fff-280
- (c) FFF-337 — **confirmed**: "Statistics GUI and Mod Debugger", redesign of electric/production stats with unified search — https://www.factorio.com/blog/post/fff-337
- (d) GUI style guide — **corrected**: exists but is community (raiguard), "merely suggestions"; standard windows "must have a close button in the top-right", dialogs "must not have a close button", Back bottom-left, Confirm bottom-right, `inside_shallow_frame_with_padding` = 12 px — https://man.sr.ht/~raiguard/factorio-gui-style-guide/ (official design commentary: https://factorio.com/blog/post/fff-246)
- (e) Threat readout — **confirmed**: pollution as a "blocky red cloud" map overlay + Pollution tab in production stats; evolution only via `/evolution` console command; attacks triggered when the cloud reaches a nest; alerts on damage — https://wiki.factorio.com/Pollution, https://wiki.factorio.com/Enemies, https://wiki.factorio.com/Alerts
- (f) Body — **confirmed/partly**: 80 slots (+10 toolbelt, up to 110 with armour); Ctrl-click transfers all, Shift-click fast-transfers, RMB half stack, MMB filter; reach raised "from 6 to 10" in 0.17; corpse "contains all items… inventory, quickbar, trash slots, and equipment slots", 10 s respawn, corpses never despawn since 2.0.7 — https://wiki.factorio.com/Character, https://wiki.factorio.com/Keyboard_bindings; the "cannot reach" on-screen text: not found

## Early reception complaints
- Machines gave no readable activity state (2015) — https://forums.factorio.com/viewtopic.php?t=18366
- No rates in tooltips (2016) and no missing-ingredient readout in entity info (2019, still merged/open) — https://forums.factorio.com/viewtopic.php?t=33729, https://forums.factorio.com/viewtopic.php?t=66616
- No confirm on window close (2015; rejected by devs 2019) — https://forums.factorio.com/viewtopic.php?t=17933
- 0.17 quickbar backlash: "The old quickbar felt so much better because i could just Shift-click items… onto empty slots" (Mar 2019) — https://steamcommunity.com/app/427520/discussions/0/1836811737971027196/
- Undo could not be redone until 2.0 (Jan 2024 thread) — https://steamcommunity.com/app/427520/discussions/0/4131556827218056835/
- Early-access visual/UI criticism: 0.15 gradient inventory slots caused eye strain (2017) — https://forums.factorio.com/viewtopic.php?t=51599
