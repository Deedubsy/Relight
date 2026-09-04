# shapez 2 (tobspr Games) — reference notes for the standards audit

Status: Early Access 15 Aug 2024; 1.0 on 23 Apr 2026 (Manufacture mode, Steam Workshop mods, 83 achievements, new visuals); 1.1 on 25 Jun 2026. Steam: Overwhelmingly Positive, 97% of 8,772 reviews. — https://store.steampowered.com/app/2162800/shapez_2__Factory/ ; https://en.wikipedia.org/wiki/Shapez_2

Sources: official wiki (shapez2.wiki.gg), Steam store/news/discussions, Steam review API, press. Reddit and RPS were unreachable from this session; where a claim rests only on them it is marked *not found*.

## 1. Controls
- Camera — WASD pan, Q/E change layer, scroll zoom; zooming out enters Map mode; H "Jump to Vortex"; Space switches Machine/Space view; north-reset hotkey; "Mouse panning" setting — https://shapez2.wiki.gg/wiki/Map ; https://shapez2.wiki.gg/wiki/Changelog (alpha12)
- Belt drag — click-drag; belt follows cursor direction, auto-corners and auto-links to machines; Alt reverses, Ctrl disables auto-orient; C places/removes anchors mid-drag (the "checkpoint"); E/Q while dragging go up/down a layer — https://shapez2.wiki.gg/wiki/Conveyor_Belt ; https://shapez-2.fandom.com/wiki/Belt
- Rotate / mirror — R / Shift+R rotate, F / Shift+F mirror (blueprints mirror if every building has a mirrored variant); Shift+E/Q move a blueprint up/down a layer — https://shapez2.wiki.gg/wiki/Blueprints ; Changelog alpha21
- Pipette — "Pipette/Clone moved from 'G' to 'F'" (alpha23); works over shape/fluid patches to pick the extractor and in empty space — https://shapez2.wiki.gg/wiki/Changelog
- Ghost placement / placement preview — shape previews on open ends, at the end of space belts/pipes and on trains (1.0); "Almost all machines now show a small video during placement… visualizing their throughput" (1.0 notes) — https://api.steampowered.com/ISteamNews/GetNewsForApp/v2/?appid=2162800&count=100&maxlength=0&format=json
- Undo/redo — "cut, copy, paste, undo/redo (50 operations)" on the store page — https://store.steampowered.com/app/2162800/shapez_2__Factory/
- Copy/paste — Ctrl+C clone, Ctrl+X cut, Ctrl+V paste; blueprint auto-copied to clipboard (setting); `.spz2bp` strings shared via Community Vortex — https://shapez2.wiki.gg/wiki/Blueprints
- Blueprint library — B opens library; Ctrl+S save with name, icon (icon set or shape code); folders nested up to 4 deep; right-click edits; drag to 9-slot blueprint toolbar (`toolbar.json`); folders bindable to toolbar (0.1.0-pre1) — https://shapez2.wiki.gg/wiki/Blueprints ; https://shapez2.wiki.gg/wiki/Game_Files ; Changelog
- Upgrade planner — none; belt/machine speed are global research upgrades. Replacing: drag a belt over existing transport without Shift (alpha22.3) — Changelog. Deconstruction — area select then X/Del; "Clear Contents" (I); "Select Connected" (O) — https://shapez2.wiki.gg/wiki/Blueprints
- Area select — hold Shift + left-drag; add/remove with modifiers — https://shapez2.wiki.gg/wiki/Blueprints
- Hotbar — number keys select buildings and cycle variants; Tab switches variant; "Multiple toolbar configurations with hotkey assignment" — Changelog alpha4/alpha9. Paging: *not found* beyond the 9-slot blueprint bar.
- Rebinding — every hotkey reassignable in settings; Codex hotkey text auto-updates to your bindings — https://steamcommunity.com/app/2162800/discussions/0/6929437446240096241/ ; https://shapez2.wiki.gg/wiki/Codex
- Controller / Steam Deck — "Console controllers are not supported"; Deck "works… but is not officially supported" (trackpad cursor); default camera up/down keys added for Deck layouts — https://steamcommunity.com/app/2162800/discussions/0/806849231160779528/ ; Changelog alpha15

## 2. Logistics affordances
- Belt lanes — one lane per belt (1 in/1 out, 1×1×1); three vertical layers instead; speed tiers 60/90/120/150/180 per minute — https://shapez2.wiki.gg/wiki/Conveyor_Belt
- Space belts — 12 lanes in 3 groups of 4, each lane = 4 belts, 0.5 platform units/tile — https://shapez2.wiki.gg/wiki/Space_Belt
- Drag with auto-corners — belts auto-corner and prefer machine connections; anchors (C) fix a route; Lifts (1 in/1 out) between layers; Belt Launcher/Catcher jump 1–4 tiles — https://shapez2.wiki.gg/wiki/Conveyor_Belt
- Auto-underground — *not found* (community requests "Auto Belt Tunnels": https://shapez-2.nolt.io/749)
- Splitter/merger — created by dragging a belt off/onto another belt; 1→1/2/3 and 1/2/3→1; Overflow (priority) Splitter prefers straight, sides only when blocked; no configurable priority/filter on splitters (requests: https://shapez-2.nolt.io/295) — https://shapez2.wiki.gg/wiki/Conveyor_Belt
- Delete belt cleans up — "Deleting buildings & belts now removes unused splitters & mergers automatically" (alpha10) — https://shapez2.wiki.gg/wiki/Changelog
- Throughput hover — hover a belt/station/space belt for items-per-minute; Belt Reader building reads a belt's rate — https://www.whisperofthehouse.com/shapez-2/space-belts-trains-guide ; Changelog alpha5
- Belt checkpoints — the anchor system above; taught in Skill Academy (alpha10) — Changelog
- Bottleneck reason — reader cannot distinguish starved vs blocked; a 20-vote request asks for it — https://shapez-2.nolt.io/2779

## 3. Information
- Statistics — panel "showing shape delivery statistics and structure counts" (0.0.9-rc1); "improved statistics screen" in 1.0 — Changelog; Steam news API
- Alerts — flashing unspent-research reminder (snoozable 15 min); 5-second unlock notifications; 1.0 "more visually pleasing notification" for unlocks/goals/achievements; click-to-jump on alerts *not found* (only "Jump to Vortex" and click-a-research-to-scroll) — Changelog alpha13/alpha21/0.0.8
- Recipe/shape browser — Codex (G / ? button) with topics, pages and search; shape viewer for milestone shapes — https://shapez2.wiki.gg/wiki/Codex
- Machine tooltips — building stats shown during placement (alpha4); placement videos with throughput (1.0); "tooltips for everything" per players — Changelog; https://steamcommunity.com/app/2162800/discussions/0/6929437211294977097/
- Overlays — "new visualization that shows conflicts (i.e. disconnected outputs)" (alpha22.3); map toggles Grid / Shape Asteroids / Coordinates / Conflicts; throughput or congestion heat-map *not found* — Changelog; https://shapez2.wiki.gg/wiki/Map
- Map/labels — Map Markers (L) with name + shape icon, right-click to edit/move; labels support multiline text (0.1.0-pre1); Compass and Markers in the main HUD; minimap *not found* — https://shapez2.wiki.gg/wiki/Map ; https://shapez2.wiki.gg/wiki/Main_UI

## 4. Onboarding and help
- Tutorial — reworked alpha13 with per-step videos; can be disabled (alpha12); 1.0 rework "explaining cross-layer conveyor belts better"; players still call it slow (~5 h) or "un-intuitive" — Changelog; Steam news API; https://gamermatters.com/shapez-2-early-access-impressions/
- Skill Academy — "Added skill academy showing QoL tips like belt checkpoints" (alpha10); hints e.g. "Switch Variants" — https://shapez2.wiki.gg/wiki/Changelog
- In-game reference — Codex/Knowledge Panel, search added in 1.0 — https://shapez2.wiki.gg/wiki/Codex
- First-time hints — contextual keybinding hints next to the toolbar (alpha12); "tooltip for main goal, showing description of the next unlock" — Changelog
- Unlock video — "Unlock notifications now display videos of new buildings" (alpha10) — Changelog
- Gaps players document — Shift-drag, Alt reverse, balancer bypass, pin button, shape codes are unexplained — https://steamcommunity.com/sharedfiles/filedetails/?id=3616458904

## 5. Look and feel
- Machine animation — "Open Production Buildings with full visibility"; reviewers: "shapes flowing down them in unison" — store page; https://godisageek.com/reviews/shapez-2-early-access-review/
- Item movement — 3D shapes on belts, praised: "very satisfying… watching the shapes getting manipulated" — Steam positive reviews API
- Build/destroy feedback — *not found* (beyond conflict highlights and placement videos)
- Light/shadow — shadows, AO, LOD; 1.0 "new visuals"; 1.2 shadow fix — Changelog; Steam news API
- Sound/music — 80-min electronic OST, randomised tracks, selectable music packs, extended OST DLC; reviews: "calming, melodic" — https://peppsen.bandcamp.com/album/shapez-2-original-game-soundtrack ; Changelog alpha6.2/alpha21
- Screen feedback/toggles — experimental dark mode (1.0); colour-blind patterns; F4 / Shift+F4 screenshot with/without UI — Steam news API; Changelog alpha9

## 6. Environment
- Terrain — space theme: platforms built in a void around shape and fluid asteroids; map-generation options — https://en.wikipedia.org/wiki/Shapez_2 ; https://shapez2.wiki.gg/wiki/Map
- Decor/landmarks — asteroids and the Vortex (praised as "especially lovely") — https://tryhardguides.com/shapez-2-early-access-review/
- Space/void rules — machines only on platforms (Platform Units budget); space belts/rails free-floating, rails cost 0 PU — https://shapez2.wiki.gg/wiki/Space_Belt ; https://shapez2.wiki.gg/wiki/Trains
- Day/night, weather, ambient life — *not found*

## 7. Map and world
- Charting/fog — map shows asteroids; exploration fog *not found*; shapes get less regular further from the hub — https://shapez2.wiki.gg/wiki/Map ; guide 3616458904
- Pins — Map Markers (L / M), name + shape code — https://shapez2.wiki.gg/wiki/Map
- Long distance — space belts (short), trains with colour-routed rails and loaders (long); Vortex delivery by train — https://shapez2.wiki.gg/wiki/Trains
- "Where am I" — Compass, Coordinates overlay, Jump to Vortex (H) — https://shapez2.wiki.gg/wiki/Main_UI ; https://shapez2.wiki.gg/wiki/Map

## 8. Meta
- Saves — savegames folder; "Save slot and backup systems"; autosave with countdown, delayed while mouse held (1.1); EA saves not compatible with 1.0 (EA branch kept) — https://shapez2.wiki.gg/wiki/Game_Files ; Changelog; 1.0 FAQ
- Cloud — Steam Cloud; Shared settings folder synced — store page; Game_Files
- Settings — graphics quality (auto "medium" on Deck), colour modes incl. "RGB (Colorblind)" with patterns, dark mode, 4K/ultrawide; UI scale *not found* — https://shapez2.wiki.gg/wiki/Color_Modes ; 1.0 FAQ
- Localisation — 13 languages (interface + full audio + subtitles) — store page
- Achievements — 83 — store page
- Mods — Steam Workshop + "Shapez Shifter" API at 1.0 — https://www.gamebrief.net/blog/shapez-2-mods-guide-best-mods-2026 ; 1.0 FAQ
- Multiplayer — not planned ("enormous technical undertaking") — 1.0 FAQ
- Screenshots — F4 hotkeys; replays *not found* — Changelog

## 9. Performance expectations
- Building counts — "smooth through 100,000 structures and playable up to 500,000–1,000,000+" (1.0 FAQ); players report 15 fps at 600k, 5 fps at 1.4M — https://steamcommunity.com/app/2162800/discussions/0/4425436358659888434/ ; negative reviews API
- Sim/render — EA launch: "simulation and rendering both run on just a single thread"; multithreaded sim shipped 0.0.9-rc1 "up to 40x"; dev demo 0.8→43.7 FPS on i7-10700; Devlog 027 computes an optimal update order — https://steamcommunity.com/games/2162800/announcements/detail/4469355336712585839 ; CPU thread above
- Time scaling — pause only (P); no speed control in vanilla, mods add 5–25× — https://steamcommunity.com/app/2162800/discussions/0/6929437211295296743/ ; https://thunderstore.io/c/shapez-2/p/yujis/FastForwarder/

## Claim checks
- (a) **Confirmed** (mostly). Steam thread by Shabazza, 15 Aug 2024: "Issues with the belt layout gets highlighted, form preview on all open ends is very helpful", tooltips, world labels, blueprints with "folder structure, description, icons and quick bar" — https://steamcommunity.com/app/2162800/discussions/0/6929437211294977097/ . Splitter cleanup, unlock videos and Skill Academy teaching belt checkpoints are dev changelog entries (alpha10), not player praise in that thread — https://shapez2.wiki.gg/wiki/Changelog
- (b) **Not found.** No "belts full" statistic or complaint located on Steam forums, reviews, wiki or nolt; nearest is the Belt Reader not distinguishing starved from blocked (https://shapez-2.nolt.io/2779). Reddit was unreachable.

## Early reception complaints
1. Repetition/tedium: "brainless copy-pasting", tear down the factory after every task — negative reviews API; https://steamcommunity.com/app/2162800/discussions/0/4629232489537241933
2. Trains and long-distance logistics "underbaked", not fun — positive reviews (Oct 2024); Wikipedia summary
3. Late-game performance: single-threaded at EA launch, low fps past ~300–500k buildings — CPU thread; reviews
4. Tutorial slow/unclear; cross-layer belts, pins, shape codes unexplained — gamermatters; guide 3616458904; reviews
5. Early space/platform limits and 4-tile extractor bottleneck — gamermatters
6. Post-1.0: EA saves incompatible, cosmetic DLC, "no consequence" economy — negative reviews API (Apr–Jun 2026)
