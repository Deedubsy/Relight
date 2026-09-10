# Open Relight in Phaser Editor

1. Start **Phaser Editor Desktop** and choose **Open Project** (an existing project, not a new template).
2. Select **`E:\Factorio2`**, the repository root. This includes `packages/game` and its shared `packages/sim` dependency.
5. In a terminal in that folder, run:

   ```sh
   npm run editor:dev
   ```

   Keep this terminal running. On this Windows checkout the launcher uses **Ubuntu-24.04 through WSL**, matching the existing Linux dependencies. Windows-mounted WSL folders use 500 ms file polling so editor changes and new public assets are detected. On Linux, or a Windows checkout with native Windows dependencies, it launches Vite directly. No reinstall or engine upgrade is needed. If the editor offers to install dependencies, dismiss that prompt on this already-installed checkout.
4. Press **Play** in Phaser Editor. It opens **http://127.0.0.1:5190/?view=world**, the live Vite game. The existing built preview on 5178 is separate. Port 5190 is strict: an occupied port reports an error rather than silently moving the preview away from Play's configured address. If this preview is already running, use it without starting another copy.
5. Open **`packages/game/public/relight-asset-pack.json`** in the editor to browse the 65 registered SVG building roofs/floors. Their `rf-…` keys match the existing renderer. The `publicroot` marker makes the editor resolve assets from `packages/game/public`, matching Vite's URLs.

## Open and edit the city

Double-click **`packages/game/src/editor/RiverfrontCity.scene`** in Phaser Editor's Files panel. If it is missing, use **Reload Project**. This is the visual scene; `worldScene.ts` remains the running game's code.

The scene contains all 479 building images over a terrain reference, with roads, paths, tram, yards, props, resources and site labels. Teal Rectangle objects show editable building plots. Use the Outline to select a named building; `court_northwest_home` is a useful first example near Home. Coordinates are pixels at **32 pixels per simulation tile**, with 32 px snapping enabled. Keep **Generate Code off**: this scene feeds the simulation map through the importer below, rather than generating a second WorldScene class.

1. Move a background building with X/Y or grid snapping. To move beyond its original plot, move or resize the matching **`PLOT_<building label>` Rectangle** as well. Rectangle edges must land on whole 32 px tiles and contain the building. Existing building scale, origin and rotation stay unchanged; matching roof variants `0`, `1` and `2` are supported.
2. To add a building, duplicate a roof Image or drag a registered `rf-...-roof-...` asset into the scene. Give it a unique label and set origin **0,0**. Its displayed width and height must be whole 32 px tiles (4–64 tiles per side). A two-tile plot margin is supplied automatically, or add a Rectangle named `PLOT_` followed by the image's exact label to choose the plot. New buildings are solid background scenery with boarded entrances; they do not create loot, interiors or campaign content. House textures encode their N/E/W facing; other existing roof textures face south.
3. To remove a background building, delete its Image and its matching plot Rectangle. Buildings marked **FIXED** contain interiors or campaign bindings and cannot move, change their plot or be deleted. Grouping, rotation, arbitrary textures and physics components are rejected.
4. Save the scene (`Ctrl+S`).
5. In a terminal at `E:\Factorio2`, run **`npm run editor:check`**. This checks the draft without changing the game.
6. Run **`npm run editor:apply`** to validate and apply it. A valid move updates the canonical building, wall collision, door, visual footprint, entrance path and named furniture/garden props together. Invalid edits report the building and reason and leave the map unchanged.
7. Reload **http://127.0.0.1:5190/?view=world** and choose **New Game**. The live preview must be running (`npm run editor:dev`). The static 5178 preview requires a rebuild; use 5190 for this workflow.

**Current editing scope:** add/remove background buildings, move existing background buildings, edit plot rectangles and choose matching roof variants. The 22 `FIXED` buildings preserve interiors and campaign bindings; roof variants remain editable. Labels must be unique, and plot labels must match their image labels. The importer creates entrances and pavement connections for new or replotted buildings, then checks actual geometry and reachability. Overlapping plots, roads, resources, factory reservations, obstructed approaches and invalid transforms are rejected before source writes.

The terrain/reference image is a snapshot; paths and attached props shown there do not redraw while dragging. The running game rebuilds imported paths after Apply. You may hide the reference while working; do not transform it. Roads, tram, resources, terrain, props and gameplay sites are still authored references. New artwork integration is a separate next step; adding an arbitrary texture to the pack alone does not define collision or a building archetype. Tiled remains export-only.

An applied change gives the map a content-derived `riverfront-arc-v4-editor-…` ID. Existing saves remain intact and require their matching layout; start a new game after changing geometry. Undo edits in Phaser Editor, save and apply again to restore an earlier layout. A scene identical to the current baseline restores that baseline map ID (now the CITY-F city-wide layout). The source snapshot in `maps/phaser/RiverfrontCity.source.txt` and manifest must remain together; they preserve the current authoring baseline and stable editor-to-building IDs. External map changes are detected before applying, and the original Python blueprint generator refuses to overwrite an applied editor layout. Reconcile external changes before continuing; don't bypass those guards.

`npm run editor:scene` is the initial exporter, **not** an update command. It refuses to overwrite an existing scene or manifest. Do not delete your edited scene to rerun it. Keep the scene, manifest, source snapshot, reference SVG and both asset packs with the project.

After adding new SVGs under `packages/game/public/art/riverfront`, run `npm run editor:assets`; it adds missing entries and preserves existing pack edits. Deleted/renamed assets require corresponding pack edits in the editor. If files change outside Phaser Editor, use its **Reload Project** command.

## Configuration and checks

- `phasereditor2d.config.json`: Play URL and resource filtering. Excludes generated builds, historical evidence, snapshots and copied Tiled assets from editor indexing, while leaving source/artwork available.
- `scripts/phaser-editor-dev.mjs`: reads that same URL for Vite's host/port, starts the installed toolchain and forwards shutdown signals.
- `packages/game/public/publicroot`: asset URL root marker.
- `packages/game/public/relight-asset-pack.json`: editor-recognized Phaser asset manifest; no change to the runtime's existing loading sequence.
- `scripts/phaser-editor-assets.mjs`: additive manifest maintenance.
- `packages/game/src/editor/RiverfrontCity.scene`: native Phaser Editor scene (official v5 JSON format, compiler disabled).
- `packages/game/public/relight-editor-pack.json`: terrain reference texture; the existing pack provides building textures.
- `packages/tools/src/phaserCity.ts` and `phaserCityModel.ts`: exporter, validated draft conversion and atomic canonical-source update; no runtime editor dependency.
- `scripts/phaser-editor-city.mjs`: Windows/WSL-aware launcher for scene export, check and apply.

The Windows launcher was exercised successfully through WSL. A real Chrome session loaded the current v4 game from the Play URL, fetched all 65 assets and loaded all 65 through Phaser's actual asset-pack loader without errors. Evidence: `docs/evidence/phaser-editor/browser.json` and `preview.png`. The scripts pass Node syntax checks; configuration/manifest paths are verified. This setup changes no gameplay, map or save format, so no campaign simulation was run.

The original scene integration was checked separately in `docs/evidence/phaser-scene/README.md`: no-op conversion, supported moves and roof variants, unsupported edits, full geometry validation, actual Phaser rendering, and a temporary apply/game/save/restore round trip. The desktop editor is now connected through its MCP server. CITY-F was saved by the native editor and passes the importer with 959 objects: one reference, 479 buildings and 479 plot rectangles. [Current city-wide evidence](evidence/city-density/README.md) includes actual game views, collision/interior walking, save/reload, focused importer checks and the partial broad-suite result.

After this external layout update, use **Reload Project** if the reference terrain still looks stale. Reopen `RiverfrontCity.scene` rather than saving an old cached tab over it. The approved CITY-E scene and source are preserved in `docs/evidence/city-density/before.scene` and `before-riverfront.ts.txt`; the earlier pre-density backups remain in `docs/evidence/neighbourhood-density/`. These are historical backups, not the active editing baseline.

Official references: [project configuration](https://docs.phaser.io/phaser-editor/misc/project-config), [public asset roots](https://docs.phaser.io/phaser-editor/asset-pack-editor/add-file#setting-the-root-folder-for-the-asset-files), [asset packs](https://docs.phaser.io/phaser-editor/asset-pack-editor/asset-pack-file), [scene compiler and Generate Code setting](https://docs.phaser.io/phaser-editor/scene-editor/scene-compiler), [official starter scene](https://github.com/phaserjs/editor-starter-template-vite/blob/main/src/scenes/Level.scene).
