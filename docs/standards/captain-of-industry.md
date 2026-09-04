# Captain of Industry — standards-audit reference (MaFi Games, EA 31 May 2022; Update 4.2 July 2026)

Sources checked 2026-09-04. Official: wiki.coigame.com, coigame.com (site redirects from captain-of-industry.com), Steam store/news/forums, ideas.captain-of-industry.com, GitHub. Reddit could not be reached this session — every "Reddit" claim below is marked not found.

## 1. Controls
- Camera — WASD/arrows pan; Alt+Q/E rotate; `[`/`]` zoom; middle-click or Alt+LMB free look; RMB-drag pan; Ctrl+4/5/6 save camera positions, 4/5/6 recall; F10 free-cam — https://wiki.coigame.com/Keyboard_controls
- Placement — R rotate, F flip/toggle mode, Q/E lower/raise building height, Shift = place multiple — https://wiki.coigame.com/Keyboard_controls
- Transport placement — pathfinding between two clicked points; Ctrl alternative route, Shift disallow turns (straight line), R snapping, F port-avoidance — https://wiki.coigame.com/Keyboard_controls ; https://coigame.com/post/captain-s-diary-4-deep-dive-into-conveyors-and-pipes
- Pipette/clone — V "clone" tool; C insta-copy — https://wiki.coigame.com/Keyboard_controls
- Ghost/planning — B toggles "planning mode" (buildings placed as plans, not yet constructed); Y toggles price popup — https://wiki.coigame.com/Keyboard_controls
- Undo/redo — NOT in game. Roadmap lists "More quality of life (e.g. undo / redo)" as tentative; ideas board "Undo Function" (110 votes, since July 2022) still "under consideration" — https://coigame.com/Roadmap ; https://ideas.captain-of-industry.com/suggestions/318740/undo-function
- Copy/cut/paste — Ctrl+C copy, Ctrl+X cut (X insta-cut); copies can be rotated/flipped on placement — https://wiki.coigame.com/Keyboard_controls ; https://coigame.com/post/cd-34
- Blueprints — library window (F4), unlocked by research after Captain's Office; folders/subfolders via drag-and-drop; export/import as text strings, whole folders too; names+descriptions on hover; search box added in Update 3 — https://coigame.com/post/cd-34 ; https://coigame.com/post/update3-is-out
- Blueprint sharing — COI Hub (hub.coigame.com) for blueprints, maps, mods — https://coigame.com/post/update2-is-out
- Upgrade / deconstruct / pause — I upgrade tool, Del or Ctrl+D demolish, P pause tool, U unity tool — https://wiki.coigame.com/Keyboard_controls
- Area select — designation tools (M mine, Z dump, N level, H harvest) are drag areas; RMB/Ctrl+LMB clears; max tool area increased in 0.4.14 — https://wiki.coigame.com/Keyboard_controls ; https://coigame.com/post/cd-34
- Hotbar/toolbar — build toolbar grouped by tier with goal-oriented sub-categories (Update 3); no player-customisable hotbar found — https://coigame.com/post/update3-is-out
- Rebinding — key remapping supported (PCGamingWiki) — https://www.pcgamingwiki.com/wiki/Captain_of_Industry
- Controller / Steam Deck — no native controller scheme; FAQ: "game works fine on Linux (Proton) and Steam Deck"; players say "you need to tweak the controls"; ideas board request "under consideration" — https://coigame.com/faq ; https://steamcommunity.com/app/1594320/discussions/0/767436134178455702/ ; https://ideas.captain-of-industry.com/suggestions/525219/better-steam-deck-support ; Steam Deck rating: not found.

## 2. Logistics affordances
- Conveyor types — Flat (units), U-shape (loose), pipes (fluids), each tiers I–III; Flat I = 60/60s, U-shape II = 200/60s; T2/T3 stack items on a single lane (no dual lanes) — https://wiki.coigame.com/Flat_Conveyor ; https://steamcommunity.com/app/1594320/discussions/0/809099441085664103/
- Drag placement with auto elevation — shortest-path preview between endpoints, port snapping on hover, auto pillars ("pillar at most 4 grids away"), ramps, stacking transports on top of each other, angled joins — https://coigame.com/post/captain-s-diary-4-deep-dive-into-conveyors-and-pipes
- Vertical — conveyor lifts (6 tiles on 1×2), stackable balancers on pillars (Update 2); modular ramps any length/height (4.2) — https://coigame.com/post/update2-is-out ; https://coigame.com/Changelog
- Balancers — 8 ports, per-port priority, "strictly even inputs/outputs" toggles — https://wiki.coigame.com/Flat_Balancer
- Sorters — pick products for the arrowed side output, rest go straight; blocks the line if the side output is full — https://wiki.coigame.com/Flat_Sorter
- Throughput display — recipe/transport figures shown "per 60"; machine hover tooltip shows in/out per 60 s — https://steamcommunity.com/app/1594320/discussions/0/809099441085664103/ ; https://coigame.com/post/update3-is-out
- "What's on this belt" — not found (T transports window exists) — https://wiki.coigame.com/Keyboard_controls
- Trucks/excavators as logistics — trucks auto-deliver between storages/ports with keep-full/keep-empty sliders; excavators mine designations; amphibious variants (Update 4); dev: "Trucks do re-route" except advanced-logistics assignments — https://steamcommunity.com/app/1594320/discussions/0/3826425264485614420/ ; https://steamcommunity.com/app/1594320/discussions/0/3815158655056009635/ ; https://coigame.com/post/update4-is-out

## 3. Information
- Statistics — Statistics window (O / "0"), products tab with per-product production (green) vs consumption (red) graph; Update 3 rebuilt it with bar and pie charts; Update 4 added productivity stats with monthly/yearly history — https://steamcommunity.com/app/1594320/discussions/0/3815158655060800312/ ; https://coigame.com/post/update3-is-out ; https://coigame.com/post/update4-is-out
- Power graph — not found as a separate screen (electricity is a statistics tab; unverified).
- Alerts — pop-up alerts (no fuel, excavator without area, no path…); v0.7.1 "dismiss all", v0.7.2 mute critical/non-critical independently; click-to-jump: not confirmed (click-target bugs logged) — https://coigame.com/Changelog ; https://steamcommunity.com/app/1594320/discussions/0/603031818788181709/ ; https://github.com/MaFi-Games/Captain-of-Industry-issues/issues/2020
- Recipe browser — F1 recipes window; Ctrl+F search; "what uses this": not found — https://wiki.coigame.com/Keyboard_controls
- Machine tooltips/status — rich floating windows show fuel, maintenance, buffers (Update 3); status text "Output full" / input starvation shown on machines — https://coigame.com/post/update3-is-out ; https://ideas.captain-of-industry.com/suggestions/653805/fix-bug-with-machines-saying-output-full-even-when-connected-an-empty-belts
- Overlays — L resources visualisation; Shift+L terrain inspector (vertical cross-section under cursor, v0.8.4); vehicle-pathfinding overlay (v0.7.0); pollution overlay: not in game, requested — https://coigame.com/Changelog ; https://ideas.captain-of-industry.com/suggestions/706770/polution-overview-more-detail-for-where-the-pollution-is-comming-from
- Map — Tab opens world map; no minimap found; Update 4 added search for machines/buildings — https://wiki.coigame.com/Keyboard_controls ; https://coigame.com/post/update4-is-out

## 4. Onboarding and help
- Tutorial — no guided tutorial; "every time you unlock a mechanic, you get a small info page"; Tutorials window on F2 / graduation-cap icon, improved in 4.2 (section navigation, maximise) — https://steamcommunity.com/app/1594320/discussions/0/3457092683967067379/ ; https://coigame.com/Changelog
- Guide objectives — questline with rewards via "the trophy icon in the upper-left corner" — https://www.thegamer.com/captain-of-industry-tips-getting-started/
- Discoverability failure — new player found R/F keys "after 25 hours" and never found auto-connect — https://steamcommunity.com/app/1594320/discussions/0/689745260424840217/
- Wiki — official wiki.coigame.com; in-game wiki link: not found.

## 5. Look and feel
- Machines/items — animated 3D machines; products rendered on belts (new renderer 10–20× faster in 4.2); pipes/tank trucks coloured by contents; icons on fluid storage — https://coigame.com/Changelog ; https://coigame.com/post/update2-is-out
- Particles/feedback — cloud shadows, improved particles, ghost-placement previews — https://coigame.com/post/update2-is-out
- Lighting — no day/night; a community "Night Mod" adds one — https://coigame.com/Mod/1077/Night-Mod-Day-Night-Cycle
- Music — Ondřej Matějka OST, 32 tracks; players report repetition, dev tried silence gaps "didn't work very well" — https://store.steampowered.com/app/2004660/Captain_of_Industry__Soundtrack/ ; https://steamcommunity.com/app/1594320/discussions/0/3886100869056178047/
- Toggles — frosted-glass UI off (v0.8.1), mute when unfocused (v0.8.3), larger text (v0.7.1) — https://coigame.com/Changelog
- Photo mode — F11, P screenshot, O auto-rotate — https://wiki.coigame.com/Keyboard_controls

## 6. Environment
- Terrain — fully dynamic heightmap with layered materials; mine/dump/level designations; retaining walls; slope limits for vehicles; 8-direction collapse sim — https://store.steampowered.com/app/1594320/Captain_of_Industry/ ; https://coigame.com/post/cd-35 ; https://shapes.inc/fandom/captain-of-industry/terrain-and-mining
- Water — sea level 0; digging below 0 with no ≥1 ground between you and the sea floods instantly; ocean reclaimable by dumping — https://steamcommunity.com/app/1594320/discussions/0/3490879839974220149/ ; https://store.steampowered.com/app/1594320/Captain_of_Industry/
- Groundwater — finite, regenerates by rain — https://wiki.coigame.com/Water
- Weather — Sunny/Cloudy/Rainy/Heavy Rain, changes every 15 days over 3-day blends, drives solar (128→26 kW), crops, wells; drying climate over years — https://wiki.coigame.com/Weather
- Day/night — none (day = 2 s real time) — https://wiki.coigame.com/Time
- Decor — custom surfaces (10 types) and 70+ decals, props removal, billboards (4.1), tree variants, FFT ocean waves — https://coigame.com/post/update2-is-out ; https://coigame.com/Changelog ; https://coigame.com/post/cd-35
- Ambient life — not found.

## 7. Map and world
- Island — only Home Island is buildable; maps up to 17 M tiles; Dragontail 22 km², Shattered Isles 26 km² — https://wiki.coigame.com/World_Map ; https://coigame.com/post/update2-is-out ; https://coigame.com/post/update4-is-out
- World map — node graph explored by The Ship (diesel, crew); unknown nodes are grey "?"; bridge upgrade reveals adjacent nodes, radar extends range; red/green enemy flags; villages, mines, oil rigs, wrecks — https://wiki.coigame.com/World_Map
- Map pins — not found.
- Map editor — Update 2, erosion sim, sharing via COI Hub — https://coigame.com/post/update2-is-out
- "Where am I" — saved camera positions (Ctrl+4/5/6) — https://wiki.coigame.com/Keyboard_controls

## 8. Meta
- Saves — Steam Cloud; autosave interval options incl. 3/5 min (v0.7.1e); clock icon on autosave rows (v0.8.4); 30–50 % faster loads (Update 4) — https://www.pcgamingwiki.com/wiki/Captain_of_Industry ; https://coigame.com/Changelog
- Settings — UI scale (120 % cuts off windows), larger-text accessibility, texture quality, frosted-glass off; no colour-blind mode — https://steamcommunity.com/app/1594320/discussions/0/4362374548292477899/ ; https://www.pcgamingwiki.com/wiki/Captain_of_Industry
- Localisation — ~20 languages on Steam, community translation tool; language added once substantially translated — https://store.steampowered.com/app/1594320/Captain_of_Industry/ ; https://coigame.com/faq
- Achievements — none — https://steamcommunity.com/app/1594320/discussions/0/729153699965972839/
- Mods — C# DLL mods, "experimental", in-game COI Hub browser/install since 4.2 — https://github.com/MaFi-Games/Captain-of-industry-modding ; https://coigame.com/Changelog
- Multiplayer — single-player; "open to seriously consider a multiplayer after the release" — https://coigame.com/faq
- Difficulty — 30+ settings, changeable mid-game; Sandbox mode (Update 4) — https://coigame.com/post/update2-is-out ; https://coigame.com/post/update4-is-out
- DLC — Trains Expanded, 9 Mar 2026 — https://store.steampowered.com/app/4349830/Captain_of_Industry__Trains_expanded/

## 9. Performance expectations
- Scale — 65 M-tile terrain limit (262 km²); terrain on one GPU texture; trees 3×, pathfinder 40 % faster (U3); conveyors sim +30 %, rendering 5× (0.7.8); product renderer 10–20× (4.2) — https://coigame.com/post/cd-35 ; https://coigame.com/post/update3-is-out ; https://steamdb.info/patchnotes/19457848/ ; https://coigame.com/Changelog
- Sim/render — fixed-tick sim; v0.8.7b "pure-integer arithmetic… bit-identical across CPU vendors"; terrain fingerprint checked on load — https://coigame.com/Changelog
- Time control — Space pause, 0–3× speed keys — https://wiki.coigame.com/Keyboard_controls
- Real-world — ghost placement dropped to 30–35 fps on a 5800X3D/6950XT in Update 2 — https://steamcommunity.com/app/1594320/discussions/0/4355618117946382629/

## Early reception complaints
1. Steep learning curve / no real tutorial (44 mentions in VaporLens' Steam-review aggregation; "the game does not hold your hand in any way") — https://vaporlens.app/app/1594320/captain_of_industry ; https://steamcommunity.com/app/1594320/discussions/0/3457092683967067379/
2. Truck/vehicle logistics and pathfinding (42 mentions; "No valid destination", trucks idle) — https://vaporlens.app/app/1594320/captain_of_industry ; https://steamcommunity.com/app/1594320/discussions/0/3815158655056009635/
3. Mid/late-game slog (35 mentions; "so much fixing and repairing… progressing is just too slow") — https://steamcommunity.com/app/1594320/discussions/0/796712029630133818/
4. Death-spiral difficulty (35; press: "relatively hard death spirals") — https://www.notebookcheck.com/Captain-of-Industry-im-Test-Wenn-Anno-auf-Terraforming-trifft.1151062.0.html
5. Intrusive alerts ("keeps popping up the same nag alert over and over") — https://steamcommunity.com/app/1594320/discussions/0/603031818788181709/
6. Update 3 UI overhaul: "The entire UI looks the same, the important bits are not picked out"; more clicks for terraforming — https://steamcommunity.com/app/1594320/discussions/0/597400773442181738/
7. Missing undo/redo and multiplayer (34 "missing key features") — https://vaporlens.app/app/1594320/captain_of_industry ; https://coigame.com/Roadmap
