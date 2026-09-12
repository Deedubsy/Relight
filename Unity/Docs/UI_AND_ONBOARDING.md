# Relight — UI, Controls and Onboarding (Unity migration reference)

Worker C draft, 2026-09-11. Branch `main` @ `586d4525` plus the uncommitted GP-* working tree described in `Unity/Docs/SOURCE_INVENTORY.md`.

## 1. Purpose, status legend and source authority

### 1.1 Purpose

This document is the single authoritative home for **presentation**: screens and flow, the control bindings, the HUD, inventory/crafting presentation, the exact onboarding sequence as implemented, feedback and readability, player-facing interaction defects, and UI style tokens.

Deliberately **not** covered here:

- Gameplay rules, costs, capacities, recipes, balance — `GAME_DESIGN.md` / `CONTENT_CATALOGUE.md` (Worker A). Where a number appears below it is because a UI string interpolates it; the rule remains Worker A's.
- Unity code architecture, event wiring, sim/UI boundary, save format — `TECHNICAL_ARCHITECTURE.md` (Worker B).
- World data, coordinates, asset inventory, the export schema — `WORLD_AND_ASSETS.md` (companion file).

Scope guard: **Relight only.** The owner's separate machine-survival game is not in scope.

### 1.2 Status legend

| Label | Meaning |
|---|---|
| **Implemented and retained** | Present in the current tree, working, intended for Unity. |
| **Approved but not implemented** | Decided or specified, no working code. |
| **Implemented but needs correction** | Works, but a recorded defect means Unity must not copy it as-is. |
| **Unresolved** | No decision on record. |
| **Retired** | Superseded; historical only. |

Unverified statements are prefixed **UNCERTAIN:**.

### 1.3 Source authority

Three layers, newest wins:

1. **Code** — `packages/game/src/*` and the sim modules it reads. This is actual behaviour and beats every document.
2. **`docs/Implementation/GP_CHECKPOINT.md`** (2026-09-11) — the most recent owner player-test corrections. Where it contradicts the spec, it wins. Also `docs/Implementation/PLAYER_EXPERIENCE_CORRECTIONS.md` (2026-09-10) and `PLAYER_PLAYTEST_FOLLOWUP_02.md` (2026-09-10).
3. **`docs/RI-02B_UI_SPEC.md`** + **`docs/DECISIONS.md` D-UI-01 … D-UI-13** — the design contract. `RI-02B_UI_SPEC.md:5` states plainly: "This is implementation-ready **planned** work, not a description of delivered UI."

`docs/PROGRESS.md` alone owns task status; nothing here should be read as a status claim.

---

## 2. Screens and flow

### 2.1 There is no main menu — **Implemented but needs correction**

`packages/game/index.html` (19 lines) is the entire document shell:

```html
#app > #map (main, aria-label "City world") + #panel (aside)
plus #goal, #tooltip, #toasts (role=status, aria-live=polite)
```

**There is no title screen, no save-slot browser, no "Continue", and no "Load game" screen.** The game boots directly into the world. Entry is by **URL parameter**, parsed in `packages/game/src/session.ts:parseUrl`:

| Param | Values | Effect |
|---|---|---|
| `seed` | number | City seed. |
| `rules` | `exploration-v2` \| `legacy-v1` | Ruleset (`packages/sim/src/rules.ts:4-5`). |
| `state` | URL or `local:<slot>` | Load a snapshot or a save slot. |
| `view` | `map` \| `world` (default **`world`**) | Renderer selection. |
| `city`, `map`, `autoplay`, `flow`, `rifle`, `stalker`, `heart` | various | Diagnostic/fixture switches. |

Save slots are `localStorage` keys: `saveKey = (slot) => 'relight.save.' + slot`, with `profileSlot` prefixing `campaign:` for the campaign profile (`session.ts`). `hasSlot`, `saveSlot(s, slot='1')` and `slotUrl` (→ `?state=local:<slot>`) are the whole slot API. Nothing enumerates slots in the UI.

The closest thing to "New Game" is inside Pause → **Start a new city** (`packages/game/src/panel.ts`):

```ts
const startButton = el('button','primary','Start city');
startButton.onclick = () => {
  if(!seedInput.reportValidity()) return;
  if(window.confirm('Start a new city? Unsaved progress will be lost.'))
    location.href = shareUrl({...session.params, seed:Number(seedInput.value), state:null, autoplay:null});
};
startScreen.append(el('p',undefined,'Choose a seed for a fresh city. Save your current game before starting again.'), seedLabel, startButton);
```

`RI-02B_UI_SPEC.md:56` *does* specify a pause item "return to title/start screen with unsaved-work confirmation" — the confirmation exists, the title screen does not.

**Unity consequence:** a Unity build has no URL bar. The port **must** author a real front-end that does not exist today: title screen, New Game (seed entry + ruleset), Continue (most recent slot), Load (slot list with metadata), Settings, Quit. Status: **Approved but not implemented** (approved in spirit by `RI-02B_UI_SPEC.md:56`; nothing is built). Design it from the pause-action inventory in §2.2 — every command it needs already exists. **The concrete screen specification is §2.6** (Owner decisions 2026-09-11, Q18); the saving behaviour it lists is §2.7 (Q11).

### 2.2 In-game panels: one drawer, one modal — **Implemented and retained**

`packages/game/src/uiShell.ts` (161 lines) owns the whole shell. Structure:

| Element | Class | Rule |
|---|---|---|
| Navigation bar | `nav.ui-navigation` | Brand "RELIGHT"; carries the quickbar/action bar and the drawer buttons. |
| Drawer | `section.ui-drawer` | **Exactly one panel at a time.** Heading + Close button. Registered adapters replace each other. |
| Pause dialog | `section.ui-modal` | `role="dialog"`, `aria-modal`, `<h1>Paused</h1>`, `ui-pause-actions`, `ui-modal-child`. |
| HUD | `aside.ui-hud …` | Appended to `#app`; passive overlays. |

`register(id, …)` throws `Duplicate UI adapter: ${id}` — adapter IDs are unique keys.

Registered drawer adapters (`panel.ts:711-729`): `build`, `inventory` ("Backpack"), `inspection`, `truck` ("Truck cargo"), `route` ("Station route"), `projects`, `help`, `management`, `construction`, `developer`, `settings`, `start`.

Nav actions (`panel.ts:731-738`): `Build (B)`, `Backpack (I)`, `Map (M)`, `Projects (F2)`, `Management`, `Construction`, `Help`, `Pause menu`. Button labels are **re-rendered from the live bindings** on the `relight:preferences` event — never hardcoded letters (`uiShell.ts:drawerButton`; required by `RI-02B_UI_SPEC.md:66`).

Pause actions (`panel.ts:739-751`): `Save (Ctrl+S)`, `Load (Ctrl+O)`, `Download save`, `Copy seed/settings link`, `Settings`, `Controls and item help`, `Developer`, `Start a new city`. Sub-screens open through `pauseChild(id)`, which renders a registered adapter inside the modal with a **"Back to Pause"** button.

**Modal vs overlay:** the drawer and HUD are *overlays* — the world keeps running and stays visible and interactive underneath. `controlsHelp()` says so verbatim:

> "Panels stay live: enemies and machines keep moving. Escape closes the panel; press Escape again to Pause. Closing a panel returns world controls."

Only the pause modal is truly modal (focus trap, speed 0).

### 2.3 The camera must not shift — **Implemented and retained**

`uiShell.ts:45-49`, `sync()` sets `hudInset.right` / `hudInset.bottom` and the CSS variables `--ui-nav-height` and `--ui-drawer-inset`, with the comment:

> "**Reserve overlay layout space for HUD text; the world camera ignores these insets.**"

Panels are absolutely-positioned overlays on `#app`; `#map` is `position:absolute; inset:0` and is never resized by a panel (`style.css:136-137`). This satisfies `CLAUDE.md` ("Menus overlay the world; opening or closing them must not shift the camera") and `RI-02B_UI_SPEC.md:62` ("Opening, closing, resizing or switching inventory/interaction panels must not pan or zoom the camera; the follow target uses the full viewport, independent of HUD insets").

Verified by playtest: "Opening inventory kept the engineer at the same camera position" (`PLAYER_EXPERIENCE_CORRECTIONS.md`, Observed — controls).

**Unity consequence:** UI Toolkit overlays over a full-screen camera; do **not** implement a split viewport or a camera-rect change on panel open.

### 2.4 Time: 1× with pause, no speed controls — **Implemented and retained**

- `uiShell.pause()` records `previousSpeed = 1` and calls `hooks.setSpeed(0)`; `unpause()` restores it. Those are the only two speeds the UI ever sets.
- `packages/game/src/controls.ts` declares `slower` and `faster` as binding names **bound to empty key arrays**, and lists both in `FIXED` (non-rebindable). The rule is enforced in the binding table itself, so no player can create a speed key.
- `RI-02B_UI_SPEC.md:64`: "Pause uses 0; resume always returns to 1 … Old running save speeds normalize to 1, paused saves retain 0 … **No player acceleration controls, speed bindings, URL option or exposed runTicks hook remains.**"
- `RI-02B_UI_SPEC.md:78`: the former `panel.ts` speed buttons are "Removed under UI-08".
- D-UI-10 (`docs/DECISIONS.md:468`): "player speed 0/1 only".
- `RI-02B_UI_SPEC.md:50`: "no acceleration controls or permanent 1× label."

Headless/deterministic runners keep fast execution; that is not player-facing.

### 2.5 Responsive layout — **Implemented and retained**

`packages/game/src/settingsPanel.ts:applyUiPreferences()` sets `--ui-scale`, `body.dataset.motion`, and two layout classes by *effective* size (physical size ÷ scale):

| Class | Threshold | Effect (`style.css:328-371`) |
|---|---|---|
| `ui-compact` | < 1100 px effective width | Goal card narrows to 296 px, time block shifts, **minimap hidden**; with a drawer open the goal card spans the remaining width and hides its `<details>`/buttons. |
| `ui-short` | < 650 px effective height | Goal `<details>` hidden, engineer block compacted to a grid, quickbar slots shrink to 52 px with the `<kbd>` overlaid, nav padding reduced. |

Verified layouts on record: 1366×900 / 100 % and 1100×720 / 125 % (`PLAYER_EXPERIENCE_CORRECTIONS.md`); the spec's target matrix is 1366×768, 1920×1080, 2560×1440 and 3440×1440 at 100/125/150 % (`RI-02B_UI_SPEC.md:196`).

### 2.6 The front end — **Approved but not implemented** (Owner decisions 2026-09-11, Q18)

**Confirmed scope (owner):** the port ships a front end with **New Game, Continue, Load and Settings**. Saves show useful metadata — **day, playtime and save time**. **Continue** selects an appropriate valid recent save. Feedback is clear, and **any action that would overwrite or discard player progress asks for confirmation first**. TASKS.md **C-10** builds it; DECISIONS.md U-Q-18 carries the decision row.

Everything below the confirmed paragraph is the concrete specification this document proposes to satisfy it. Wording is **provisional** until it is on screen; the *behaviour* is not.

#### 2.6.1 Screens

| Screen | Entries / contents | Notes |
|---|---|---|
| **Title** | Game name, build string, then **Continue · New Game · Load · Settings · Quit** in that order | Continue is the default focus when a save exists, New Game when none does. **Continue is shown disabled with the reason "No saved game yet" when no save exists — never hidden** (the disabled-control rule, `RI-02B_UI_SPEC.md:38-43`: disabled controls keep readable text and an adjacent reason). Quit is a desktop-build necessity, not part of the owner's list — **provisional**. |
| **New Game** | Seed field (default 3, matching `parseUrl`'s `Number(q.get('seed') ?? '3')`, `packages/game/src/session.ts:25`), a read-only ruleset line naming the campaign ruleset, **Start** and **Back** | The reference's only new-game control is the Pause → "Start city" seed prompt (`panel.ts`, §2.1); this promotes it to the title screen unchanged. The ruleset is fixed: `parseUrl` defaults a no-`state` session to `CAMPAIGN_RULESET` (`session.ts:31`), and `legacy-v1` is retired for the port (DECISIONS.md U-Q-08). Do **not** expose the diagnostic parameters (`autoplay`, `stalker`, `heart`, `flow`, `view`, `city`, `map`) here; they belong to an editor-only debug menu (§7.0). |
| **Load** | One row per save, newest first, grouped **Manual saves** then **Autosaves** (§2.7) | Row contents in §2.6.2. Row actions: **Load**, **Delete**. Manual rows also offer **Overwrite** from the in-game Save screen, never from here. |
| **Settings** | The same adapter the pause menu opens (§3.8), so there is exactly one settings surface | `panel.ts:743-744` already registers `settings` once and opens it as a pause child; the title screen registers the same document. |

#### 2.6.2 What a save row shows

| Field | Source in the reference | Unity note |
|---|---|---|
| Slot label | Slot name. The reference has one slot, `'1'`, keyed `relight.save.<slot>` with a `campaign:` profile prefix (`session.ts:82-83`) | Unity uses named files under `Application.persistentDataPath` (TECHNICAL_ARCHITECTURE.md §9, Worker B). |
| **Day** | `SaveFile.t` (sim second, `packages/sim/src/save.ts:80`) → `campaignClock`: `day = Math.floor(t / CAMPAIGN_RULES.daySeconds) + 1`, `daySeconds = 1200` (`packages/sim/src/rules.ts:7,18-19`) | Same derivation; show `Day N`, and the in-day clock `HH:MM` exactly as the HUD strip formats it (`packages/game/src/hud.ts:56`). |
| **Playtime** | **Not stored.** `makeSave` writes `version`, `kind`, `savedAt`, `seed`, `tick`, `t`, `hash`, optional `params`, `state`, `log`, `logComplete` (`save.ts:128-134`) and nothing else. Real elapsed time exists only as the live `Session.realElapsed` field (`session.ts:137`, accumulated at `:259`) and is never serialised | The Unity save header **must add a `playSeconds` field**; nothing can be recovered for a save that predates it. Until it exists, a row shows **sim elapsed** — `clockOf(t)` → `H:MM:SS` (`packages/sim/src/queries.ts:196-199`) — labelled *sim time*, never "playtime". Do not present sim elapsed as wall-clock playtime. |
| **Save time** | `SaveFile.savedAt`, an ISO-8601 string from `new Date().toISOString()` (`save.ts:130`) | Render in the player's local time and format; keep the ISO value in the file. |
| Map / seed | `SaveFile.seed` (`save.ts:130`); map id from `state.city.mapId`, currently `riverfront-arc-v4-editor-ac16d9188c05` (`WORLD_AND_ASSETS.md` §2.3) | U-Q-18's default already said "day, playtime and map id". Show the map id only when it differs from the current build's map, with the incompatibility notice below. |
| Compatibility | `authoredProblem(st)` (`packages/sim/src/authoredCity.ts:21`) already produces the player-facing sentence: "This earlier authored city needs its original build. Original save preserved; choose New Game for Riverfront v4" | A row whose map id does not match is shown, **disabled, with that reason** — never silently hidden and never deleted. |

An example row: `Slot 1 · Day 4 · 1:12:30 · saved 11 Sep 2026, 20:14`.

#### 2.6.3 What Continue picks

Continue loads the **most recently written valid save of the current profile**, manual or autosave, ranked by `savedAt` (`save.ts:130`). "Valid" means: the file parses, `isSaveFile` accepts it (`save.ts:91-94` — `kind === 'relight-save'`, a known `version`, a `state` object), `loadState` does not throw (the reference already routes this failure through `loadSnapshot`, `session.ts:94,109`), and the map id matches this build (`authoredCity.ts:21`). If the newest file fails any of these, Continue falls to the next newest and **says so**: "Your most recent save could not be opened; continuing from {label}." If none is valid, Continue stays disabled with the reason.

#### 2.6.4 The confirmations

Confirmation is required wherever progress would be lost or replaced. The reference already confirms two of these; the strings below are the intended Unity wording, modelled on them.

| Action | Reference precedent (verified) | Unity confirmation |
|---|---|---|
| **New Game** while a session is running with unsaved changes | `panel.ts`: `window.confirm('Start a new city? Unsaved progress will be lost.')` | Title **Start a new city?** · body "This session has unsaved progress since {last save time}. Starting a new city discards it." · **Save and start · Start without saving · Cancel**. With no unsaved changes, start without a prompt. |
| **Load / Continue** over a running session | `panel.ts:127`: `window.confirm('Reload from slot 1? Unsaved progress is lost.')` | Title **Load this save?** · body "Your current session has unsaved progress since {last save time}." · **Save and load · Load without saving · Cancel**. |
| **Save** onto an occupied manual slot | none — the reference has one slot and overwrites it silently (`saveSlot`, `session.ts:210-214`) | Title **Overwrite this save?** · body "{slot label} holds Day {N}, saved {time}. It will be replaced." · **Overwrite · Cancel**. |
| **Delete** a save | none | Title **Delete this save?** · body "{slot label} · Day {N} · saved {time}. This cannot be undone." · **Delete · Cancel**. Deleting is never offered for the save currently loaded in a running session. |
| **Quit** with unsaved progress | none | Same shape as New Game: **Save and quit · Quit without saving · Cancel**. |

An autosave is never a confirmation target: autosaves are written by rotation, not by the player (§2.7).

#### 2.6.5 Front end and the running world

The title screen is a **separate document, not an overlay**: no world is running behind it, so §2.3's camera-inset rule does not apply there. Once in the world, every menu remains an overlay under the existing rules (§2.2, §2.3), and the in-game route back to the title is the pause menu's existing "Start a new city" position, restated as **Save and exit to title**.

### 2.7 Saving and autosaves — **Approved but not implemented** (Owner decisions 2026-09-11, Q11)

> **Correction, 2026-09-11.** This reverses the previously recorded default. DECISIONS.md **U-Q-11** stood as "Manual save as in the reference plus save-on-quit (B-11). Interval autosave is not added." The owner has now decided: **manual saves plus rotating autosaves**. Save-on-quit is not the mechanism; it survives only as the *prompt* in §2.6.4. Written in the same style as the 2026-09-11 GP-* corrections recorded in §4.2, §5.3 and §7.0.

**Confirmed scope (owner):** manual saves **plus rotating autosaves**; autosaves live in their **own set and never overwrite a manual slot**; a sensible interval and rotation count are delegated to the implementation; a save writes consistent state, handles write failure safely, and **preserves a recoverable previous save**. Routine implementation detail needs no further approval.

This section owns only the **player-facing side**: how autosaves appear in the Load list, how the player changes interval and count, and what a failed save looks like. The file format, atomic-write mechanism and slot manager are Worker B's (`TECHNICAL_ARCHITECTURE.md` §9; TASKS.md **B-11**).

#### 2.7.1 How autosaves appear

- The Load list has **two groups with headings**: **Manual saves**, then **Autosaves**. Autosaves are never interleaved with manual slots, so an autosave can never be mistaken for one.
- An autosave row is labelled by its rotation position and its metadata: `Autosave 3 · Day 4 · 1:12:30 · saved 11 Sep 2026, 20:14`. The newest carries a **Latest** tag.
- Autosave rows offer **Load** and **Delete** only. There is no "save into an autosave slot" control anywhere, and the in-game Save command never targets the autosave set.
- The moment an autosave is written, a transient notice appears using the existing alert budget (§8, three short transient notices): **"Autosaved · Day 4, 1:12:30."** It is a notice, not an urgent alert, and it never pre-empts a danger indicator.
- Autosaving must not stall play. If the write cannot complete without a visible hitch it is deferred to the next opportunity and the notice is not shown. The reference has no precedent for this — **provisional, and something C-10/B-11 must actually measure rather than assume**.

#### 2.7.2 The Settings → Saving section

New section in the one settings surface (§3.8), below Interface and above Keyboard bindings:

| Control | Values | Default |
|---|---|---|
| **Autosave** | Off · Every 5 minutes · Every 10 · Every 15 · Every 30 | Every 10 minutes — **provisional** (the owner delegated the number) |
| **Autosaves kept** | 3 · 5 · 10 | 5 — **provisional** |

Rules: changing the interval restarts the timer from now, it never triggers an immediate save; lowering "Autosaves kept" deletes the oldest autosaves and **asks first**, naming how many will be removed; turning autosave **Off** is allowed and shows the consequence in plain words — "New games and long sessions will only be saved when you save them." Timing is **active play time**: the timer does not advance while the game is paused at speed 0 (§2.4), so an autosave never lands while the player is reading a menu. These preferences live with the other browser/player preferences, **outside the save file and outside the sim** (`RI-02B_UI_SPEC.md:68`; §3.3).

#### 2.7.3 When a save fails

The reference's whole failure surface is one toast: `catch (e) { toast('Could not save: ' + e.message, 'bad') }` (`packages/game/src/panel.ts:123`), with an empty-slot case at `:126` ("This campaign has no save yet — Ctrl+S saves it"). That is too thin for a decision the player may act on. Unity:

| Case | What the player sees |
|---|---|
| Manual save fails | A **modal**, not a toast: **Could not save** · "{reason, in plain words — for example: there is not enough space on the disk}. **Your previous save is unchanged.**" · **Retry · Choose another slot · Continue playing**. The last is not styled as the safe option. |
| Autosave fails | One transient notice — **"Autosave failed. Your previous saves are unchanged."** — and the rotation is not advanced. After **three consecutive** autosave failures the notice is replaced by one persistent warning strip that stays until a save succeeds or the player dismisses it, which reads "Autosaving is not working. Save manually to protect your progress." Three is **provisional**. |
| Save file cannot be read at load time | The row is shown, disabled, with the reason from the reader (§2.6.2). A corrupt or old-format file is **refused with a message, never migrated** (TASKS.md B-11). |

The player-facing promise behind all of this is the owner's: **a save that fails leaves a recoverable previous save**. The presentation must never claim a save succeeded before the write is confirmed, and the Load list must never show a row for a file that was not completely written.

---

## 3. Controls

**Status: Implemented and retained.** `packages/game/src/controls.ts` is 40 lines and is the complete, authoritative binding table.

### 3.1 Default bindings

```ts
export const DEFAULT_BINDINGS = {
  copy: ['c'], paste: ['v'], mirrorX: ['h'], mirrorY: ['v'],
  north: ['w','ArrowUp'], west: ['a','ArrowLeft'], south: ['s','ArrowDown'], east: ['d','ArrowRight'],
  sprint: ['Shift'], dodge: [' '], pause: ['p'], slower: [], faster: [], map: ['m'],
  threat: ['g'], engineer: ['k'], pockets: ['i','Tab'], build: ['b'], debug: ['`'],
  projects: ['F2'], cancel: ['Escape'], pipette: ['q'], rotate: ['r'],
  recipe: ['t'], interact: ['e'], inspect: ['f'], abort: ['x'],
  save: ['s'], load: ['o'], undo: ['z'], redo: ['y'],
  belt: ['1'], inserter: ['2'], excavator: ['3'], assembler: ['4'], turret: ['5'],
  lamp: ['6'], pole: ['7'], generator: ['8'], rifle: ['9'], floodlight: ['0'],
  bigpole: ['['], substation: [']'], chest: ['c'],
  track: ['l'], tramstop: ['h'], tram: ['v'], arclamp: [], wall: [], mixer: [],
  barricade: [], underground: ['u'], splitter: ['j'],
} as const;
export const MODIFIED: readonly Binding[] = ['copy','paste','save','load','undo','redo'];
export const FIXED: readonly Binding[] = ['slower','faster','cancel','belt','inserter','excavator',
  'assembler','turret','lamp','pole','generator','rifle','floodlight'];
```

Readable summary for the port:

| Group | Binding |
|---|---|
| Movement | `W A S D` + arrow keys; `Shift` sprint; `Space` dodge |
| Interaction | `E` interact, `F` inspect, `Q` pipette, `R` rotate (also Reload, §9), `X` abort, `T` recipe |
| Panels | `B` Build, `I` or `Tab` Backpack, `M` Map, `F2` Projects, `G` threat, `K` engineer, `` ` `` debug, `P` Pause, `Escape` cancel |
| Quickbar | `1`–`0` = slots 1–10 (`QUICKBAR_KEYS`, `uiPreferences.ts:5`) |
| Build shortcuts | `1` belt, `2` inserter, `3` excavator, `4` assembler, `5` turret, `6` lamp, `7` pole, `8` generator, `9` rifle, `0` floodlight, `[` bigpole, `]` substation, `C` chest, `L` track, `H` tramstop, `V` tram, `U` underground, `J` splitter |
| Ctrl-modified (`MODIFIED`) | `Ctrl+C` copy, `Ctrl+V` paste, `Ctrl+S` save, `Ctrl+O` load, `Ctrl+Z` undo, `Ctrl+Y` redo |
| Blueprint transforms | `H` mirror X, `V` mirror Y (blueprint context only) |
| Unbound by design | `slower`, `faster` (§2.4); `arclamp`, `wall`, `mixer`, `barricade` |

Note the deliberate **disjoint-context collisions**: `C` is both Ctrl+C copy and plain-C chest; `V` is paste, mirrorY and tram; `H` is mirrorX and tramstop; `S` is south and Ctrl+S save. `controls.ts` resolves these with three contexts: **`modified`**, **`blueprint`**, **`world`** — a collision is only an error *within* one context.

### 3.2 Rebinding rules — `bindingProblem`

`controls.ts:bindingProblem` rejects, with these exact player-facing messages:

| Condition | Message |
|---|---|
| Rebinding a `FIXED` action | "Escape and numbered quickbar slots stay fixed; edit slot contents in Build." |
| Key outside the allowed set | (rejected) — allowed: `[a-zA-Z]`, `Arrow*`, `F1`–`F12`, `Shift`, `Space`, and `` - = [ ] ; ' , . / ` `` |
| Escape or Tab | "This key is reserved for interface navigation." |
| Same-context duplicate | "Already used by X in this context." |

The Settings panel (`packages/game/src/settingsPanel.ts`, 29 lines) exposes an action `<select>` (excluding `FIXED`), a read-only key-capture input, an Apply button and live `bindingProblem` text, plus **Restore default bindings**.

### 3.3 Preferences storage

`packages/game/src/uiPreferences.ts` — key **`relight.ui.v1`** in `localStorage`:

```ts
export interface UiPreferences {
  version: 1;
  quickbar: (QuickTool|null)[];       // 10 entries
  scale: 100|125|150;
  motion: 'system'|'reduce'|'full';
  bindings: BindingOverrides;
  dismissedHints: string[];           // only 'opening' is accepted
}
export const DEFAULT_QUICKBAR: readonly (QuickTool|null)[] =
  ['belt','inserter','excavator',null,null,null,null,null,'rifle',null];
```

`parseUiPreferences` defaults safely on missing/corrupt/unknown data (`uiPreferences.ts:14`: "Missing and corrupt preferences use safe defaults"). `saveUiPreferences` **updates memory even when storage is denied** (`:18`), and the UI says so: "Settings work for this session. Browser storage is unavailable; retry saving when storage is available." Settings also offers **Retry saving settings**, **Restore opening hint for new cities** and **Restore default quickbar**.

Preferences are explicitly **outside SimState and replay hashes** (`RI-02B_UI_SPEC.md:68`). Unity: `PlayerPrefs` or a versioned JSON file under `Application.persistentDataPath`, never inside the save.

### 3.4 Drag rules

`packages/game/src/uiDrag.ts` (18 lines):

- Pointer-capture drag with a **7 px** movement threshold before a drag starts.
- `.drag-preview` ghost element carrying `aria-label = 'Dragging ' + item`; the icon and stack count are centred on the cursor with the drop instruction alongside (`PLAYER_PLAYTEST_FOLLOWUP_02.md`). Native HTML5 dragging is suppressed.
- `.drop-target` highlights the legal target (`style.css`: `outline: 2px dashed var(--ui-accent)`).
- Cancels on `pointercancel`, `lostpointercapture`, window `blur` and `visibilitychange`.
- A **250 ms click suppressor** runs after a drag so the release does not also click through.
- `onUiDragEnd` hooks let panels re-render after a drop or a cancel.

### 3.5 Escape order — **Implemented and retained**

`uiShell.ts:114-120`, one press does exactly one transition:

1. `cancelUiDrag()` — **drag cancels first** (mandated by `CLAUDE.md`: "uiShell owns input capture and panel replacement; Escape cancels drag first").
2. Close an open slot menu.
3. Close an open nav `<details>`.
4. `closeChild()` — leave a pause sub-screen back to Pause.
5. `unpause()` — Escape in Pause resumes explicitly.
6. `closeDrawer()`.
7. `hooks.cancelSelection()` — cancel an active placement/selection.
8. `pause()`.

Tab is special: **Inventory owns Tab while the Backpack or a paired storage window is open** and closes it, even from a quantity field; holding Tab does not repeat-toggle; Shift+Tab still navigates focus backwards; inside the modal Tab cycles a focus trap (`RI-02B_UI_SPEC.md:126`; `uiShell.ts`).

### 3.6 Input ownership

`RI-02B_UI_SPEC.md:66` is implemented: UI pointer-down/up/click/contextmenu cannot reach world actions; wheel on a panel cannot zoom; captured drags end safely outside the canvas; text/select focus blocks global hotkeys and held movement, sprint, dodge, mining and firing; held keys and pointer gestures clear on capture, blur, visibility loss and closure, and returning focus requires a fresh press.

### 3.7 Gamepad — **out of initial scope** (Owner decisions 2026-09-11, Q19)

**Confirmed scope (owner):** the initial Unity port targets **desktop keyboard and mouse**. **Gamepad support is out of initial scope.** The input architecture stays extensible, but no unused controller interface is implemented.

Reference position, unchanged: no gamepad, controller or `navigator.getGamepads` code exists anywhere in `packages/game`. The question is now answered rather than open (DECISIONS.md U-Q-19; TASKS.md **DEF-04**, deferred, trigger: the owner asks for it).

What this forbids, concretely, so the port does not half-build it:

- **No controller glyphs and no controller prompts** anywhere — not in the HUD prompt (`hud-prompt`, §4), not on the quickbar `<kbd>` overlays (§5.5), not in the opening hint (§7.2), not in Help. Every prompt in the reference renders from the live keyboard bindings (`uiShell.ts:drawerButton`, `hud.ts`, §3.3) and keeps doing exactly that.
- **No "Controller" tab, section or empty state in Settings** (§3.8). An absent feature is not advertised. This is the same rule `RI-02B_UI_SPEC.md` applies to audio — "No audio/mute control pretending sound exists" — applied to input.
- **No controller-shaped compromises to mouse design.** The ten quickbar slots, pointer drag-and-drop transfers (§3.4) and the inspect-only map click model (§8, D-UI-13) stay as they are; they were the three things a pad would have forced open, and none of them is now in question.

What "extensible" means and nothing more: the Input System **actions** are defined by intent (`Move`, `Interact`, `Inspect`, `Rotate`, `Cancel`, quickbar 1–10, panel toggles), with only the Keyboard/Mouse control scheme and binding set populated. Adding a Gamepad scheme later is then new bindings against existing actions, not a rewrite. Do not author a Gamepad scheme, a device-lost/rebind UI or glyph assets now.

### 3.8 The settings surface — **Approved but not implemented** (Owner decisions 2026-09-11, Q19, Q11, Q09)

There is **one** settings document, opened from both the title screen and the pause menu (§2.6.1). The reference already has exactly one — `createSettings()` in `packages/game/src/settingsPanel.ts` (29 lines), registered once and opened as a pause child (`panel.ts:743-744`). Its current contents, verified line by line:

| Section | Controls (reference) | Source |
|---|---|---|
| *(unheaded, first)* | **Interface scale** `<select>` 100 / 125 / 150 %; **Interface motion** `<select>` "Follow system preference" / "Reduce motion" / "Full interface motion"; the note "UI scale changes text and controls independently of camera zoom. Settings are shared by this browser; game saves keep their own profile." | `settingsPanel.ts:12-17` |
| **Keyboard bindings** | The note "Ctrl / Cmd shortcuts and blueprint transforms keep their contexts. Escape, Tab navigation and numbered quickbar slots remain fixed; assign slot contents in Build."; an action `<select>` excluding `FIXED`; a `Current: …` line; a read-only key-capture input placeholdered "Focus here and press a key"; **Apply binding**; live `bindingProblem` text; **Restore default bindings** | `settingsPanel.ts:18-23,27` |
| **Restore and recover** | **Restore opening hint for new cities** · **Restore default quickbar** · **Retry saving settings**, with the storage-denied status line | `settingsPanel.ts:24-26,15` |

The Unity settings document keeps all three sections and **adds two**, in this order: Interface · **Audio** · **Saving** · Keyboard bindings · Restore and recover.

#### 3.8.1 Audio section (Q09)

Audio is in scope (§12). Volume control is the owner's "sensible volume controls", and nothing more elaborate:

| Control | Range | Default | Note |
|---|---|---|---|
| **Master volume** | 0–100 % slider, plus a **Mute all** checkbox | 80 % — **provisional** | Mute is a separate state, so unmuting restores the slider value rather than guessing one. |
| **Effects** | 0–100 % | 100 % — **provisional** | Weapons, impacts, machines, crafting and construction (§12 categories 1–4). |
| **Interface** | 0–100 % | 100 % — **provisional** | UI interaction cues and raid warnings (§12 categories 5–6). Warnings ride the interface bus deliberately: a player who turns effects down must still hear a raid warning. |
| **Ambience** | 0–100 % | 60 % — **provisional** | Environmental beds (§12 category 7). |

Four buses, no per-cue control, no music bus — the owner excluded a bespoke soundtrack. Each slider plays a short representative cue from its own bus on release so the setting is audible while being made; the Interface slider uses the standard UI confirm cue, and the Ambience slider does not re-trigger on every drag. Changes apply immediately and persist as preferences, **outside the save and outside the sim** (§3.3). Accessibility: sliders are keyboard-operable with arrow keys, expose their percentage as text, and every audio cue in §12 has a visible counterpart already — nothing in the game is signalled by sound alone.

#### 3.8.2 Saving section (Q11)

Specified in §2.7.2 (autosave interval and autosaves kept). It sits here, not on the Load screen, so there is one place for preferences.

#### 3.8.3 What does not appear

No **Controller** section (§3.7), no **Graphics** section beyond what the port actually implements (the reference has none; resolution and window mode are Unity-provided and are the port's choice to expose or not — **unresolved detail**, not an owner decision), and no control that pretends a feature exists.

---

## 4. HUD

**Status: Implemented and retained**, except the status strip (see 4.2). `packages/game/src/hud.ts` (101 lines) creates every element.

| Element | Class | Contents |
|---|---|---|
| Goal card | `hud-goal` | Place name, objective title, next action text, a **"Required resources in Backpack"** list, a `<details>` labelled **"Why this next?"**, and buttons **"Show location"** and **"Return to engineer (K)"**. |
| Time / threat | `hud-time` | Warning line, outage badge `hud-outages` rendered as `⚡× …`, threat controls (`#threat-controls`). |
| Status strip | `hud-status-strip` | Clock + power: `Day N · HH:MM[ · Paused]` and `Name: D / S kW`. |
| Engineer | `hud-engineer` | `Engineer · N/ENGINEER_HP HP` or `Engineer down · N s`; equipment label; `Backpack used/cap`. |
| Minimap | `hud-minimap` | 180 × 140 canvas, fold button, `Map (M)` button, legend `● You  □ Known  ◆ Pin`. |
| Prompt | `hud-prompt` | One target/action prompt when in reach. |
| Mining | `hud-mining` | Title + `<progress>` + detail. |
| Identity | `hud-identity` | "You". |
| Opening hint | `hud-opening-hint` | Dismissible; text in §7.2. |
| Objective tracker | (prepended into Projects) | A **"Track one objective"** `<select>`. |
| Alert button | `panel-alert` | Rendered into the drawer heading. |
| Map legend | `full-map-legend` | `<details>` **"Map & world symbols"** with a complete symbol key, ending **"Map clicks inspect; they never walk."** |

Power readings come from `hud.ts:58-60` (GP-POWER-FIX): normally `Name: D / S kW`; with no source, `'Power: 0 / 0 kW · no Generator linked'`; when the circuit is unreachable, `'Power: disconnected'`.

Hand-craft lock (`hud.ts:75`, GP-PLAYTEST-FIX 4): `handLocked(st)` renders `${HAND_LOCK_TEXT} · Escape cancels` — the player-facing string is **"Handcrafting — Cancel to move. · Escape cancels"**.

### 4.1 Region contract

`RI-02B_UI_SPEC.md:49-58` fixes the regions: top-left goal card (max 360 px, three short lines); top-centre status strip; top-right collapsible 180 × 140 minimap + Map button; upper-right clock/threat below the minimap; bottom-left engineer HP / equipped weapon / ammunition / Backpack; bottom-centre the stable quickbar with Build, Backpack, Projects, Map and menu commands; right-side single inspection card (normally closed; pinning never adds a second card); one prompt at the cursor.

Layer order (`style.css:127`, matching `RI-02B_UI_SPEC.md:40`): world 0, HUD 10, drawer 20, attached inventory 21, tooltip 30, modal 40, urgent alert 50.

### 4.2 The 2026-09-11 HUD strip decision (GP-HUD-STRIP) — **Implemented but needs correction (owner retest open)**

Owner report, quoted in `docs/Implementation/GP_CHECKPOINT.md`:

> "The header bar with the day count, time count and kw display is full width. I feel like it should be smaller."

Fix, **in `style.css` only** — no markup or logic change (`style.css:520-528`):

```css
.ui-shell .hud-status-strip {
  position:fixed; top:10px; left:calc(332px * var(--ui-scale)); right:auto;
  width:max-content; max-width:calc(100vw - calc(568px * var(--ui-scale)));
  …
}
.hud-status-strip strong { white-space:nowrap; }
/* GP-HUD-STRIP (2026-09-11): the strip hugs its clock and power text
   instead of spanning goal card to minimap */
@media(max-width:1000px){ … left:calc(332px * var(--ui-scale)); max-width:calc(100vw - calc(332px * var(--ui-scale)) - 10px); }
@media(max-width:700px){ … left:8px; max-width:calc(100vw - 16px); top:8px; }
```

So: the strip now **hugs its text** (`width:max-content`), is offset 332 px × scale from the left (clear of the goal card) and capped 568 px × scale short of the right edge (clear of the minimap), with breakpoints at ≤ 1000 px and ≤ 700 px. `RI-02B_UI_SPEC.md:50` was amended to match: "The day · clock · power status strip sits top-centre beside the goal card and hugs its text (`width:max-content`, capped short of the minimap), never spanning the gap."

**GP_CHECKPOINT.md records "Owner browser retest open" for this change** — it has not been signed off, and it will not be: under the owner's 2026-09-11 decision (Q20) the browser retest is not performed and the fix is carried into Unity and verified there, as the content-width check in **§9.1**. Implement the *intent* (a compact, content-width status strip), not the pixel offsets above.

---

## 5. Inventory and transfers (presentation)

**Status: Implemented and retained** unless noted. Source: `packages/game/src/inventoryPanel.ts` (129 lines). **All quantity, capacity, stacking and conservation *rules* belong to `GAME_DESIGN.md`; this section describes only how they are shown and driven.**

### 5.1 Layout

`.backpack-panel` contains, top to bottom:

1. The **Home workshop** crafting section (§6).
2. `.inventory-pair` — Backpack on the **left**, Storage on the **right**. At narrow widths Backpack comes **first, above** Storage, and keyboard/reading order matches (`RI-02B_UI_SPEC.md:126`). `style.css`: `.inventory-pair {grid-template-columns:1fr}` normally, `.storage-window .inventory-pair {grid-template-columns:1fr 1fr}`.
3. `.item-detail` — quantity input, **Transfer**, **Split stack**, **Move to slot**, **Sort**.
4. A notice element with `role="status"`.
5. The hint: **"Drag to move · Shift-click to transfer · Select a stack for actions"**.

The drawer heading becomes `${storeName()} & Backpack` when a store is open; the drawer widens to `min(860px × scale, 100vw − 24px)` (`style.css`, `.storage-window`) and gets a distinctive border while `body[data-panel=inventory]`.

### 5.2 Two different models on screen at once

| Side | Model | Presentation |
|---|---|---|
| Backpack | **True slots** (`engineer.pack` allocation reconciled against `engineer.inv`) | The pack renders **all 40 actual slots** (legacy truck 200); small windows scroll. Legacy kits reserve ten cells each and cannot be split or moved. |
| Storage | **A pooled quantity** | Presented as **50-item chunks**. Because a chunk is presentation, "a 'full' chunk is never a refusal" (`RI-02B_UI_SPEC.md:128`). |

This asymmetry is the single most important thing to preserve: dropping a Backpack stack onto any compatible storage stack **merges into the pool**; dropping a storage stack onto a Backpack stack or empty slot fills that slot first, then other matching stacks, then empties, moving only what fits and leaving the rest in storage with a notice.

### 5.3 Transfer across all visible and scrolled slots — **Implemented and retained (was a defect)**

The second-pass fix in `GP_CHECKPOINT.md` explicitly covers "drops onto **rightmost slot 24** and **scrolled slot 39**" — both previously failed. Unity must accept a drop on **every** slot including the last column and any slot only reachable by scrolling. See §9, defect U-1.

### 5.4 Transfer messages (exact strings)

- `"N Item moved to X. M stay in Y (X full)."`
- `"Store changed; select the stack again."`
- `"Store removed. Select another store."`
- `"Source stack changed. Select it again."`
- `"Backpack is full; splitting needs an empty slot."`
- `"Choose a whole positive quantity."`

Shift-click, drag and the quantity buttons all share the **same ordinary chest transfer command**, which re-checks reach, source totals and the selected allocation, and reports the **actual** amount moved on a partial result (`RI-02B_UI_SPEC.md:128`).

### 5.5 Action-bar (quickbar) slot rules — **Implemented and retained (was a defect)**

Ten stable slots, keys `1`–`0`, default `belt, inserter, excavator, –, –, –, –, –, rifle, –`.

- **A tool never occupies two slots.** All three assignment paths — catalogue drag, the "Add to action bar" destination buttons, and a Backpack weapon drop — route through `assignQuickbar` (`uiPreferences.ts:25`), which **swaps**: the displaced tool takes the vacated slot.
- Saved arrangements holding one tool several times **collapse to the first occurrence on load** (`uiPreferences.ts:10`, GP-HOME-REPAIR).
- Off-bar removal requires empty UI space with an explicit preview; world/invalid drops, Escape, blur and pointer cancellation restore the original.
- Right-click or Shift+F10 opens a slot menu; Delete/Backspace clears a slot.
- Badges mean **packed machines** when present, otherwise **buildable from carried materials**; the tooltip names that meaning. Rifle badges are **rounds**. No remote Home stock is ever counted (`RI-02B_UI_SPEC.md:112`).
- Slot size: 60 logical px default (`RI-02B_UI_SPEC.md:114`); `style.css` renders `var(--slot)` squares with the key `<kbd>` at top-left and the count bottom-right, dropping to 52 px × scale in `ui-short`.

### 5.6 Weapon equip

Dropping a weapon onto `[data-quick-slot]` or `[data-equipment-slot]` equips it and routes through `assignQuickbar`. Any other drop target refuses with:

> "Drop weapons on equipment or action-bar slots; other items go in inventory slots."

A small **equipment strip** sits under the Backpack grid: owned/equipped label, the two physical equipped slots (click or drag back to Backpack), **Reload** and **Swap** (`RI-02B_UI_SPEC.md:235`).

### 5.7 Ammunition semantics (presentation)

- In a turret inventory, **1 item = 1 bullet** when loading (`goal.ts:126`), and the same on unloading — there is no whole-magazine rounding to disclose. *(The older rule in `PLAYER_PLAYTEST_FOLLOWUP_02.md` — "turrets convert whole magazines to rounds and only return whole magazines; loose rounds stay loaded and are disclosed" — is **Retired**: it predates `ammoVersion 1` / U-D-08. Recorded here so the port does not reintroduce the disclosure UI it required.)*
- The HUD/goal shows `Loaded N / cap bullets` via `loadedText()`.
- Machine inspection shows **Backpack on the left, machine inventory on the right** for Generator, Excavator, Pumpjack, processor, Turret and Cannon; select a stack, enter Quantity, then **Load** or **Take**; Shift-click and drag move a whole available stack.

### 5.8 Conservation

Move/split/merge/swap/sort commands conserve quantities, reject stale layouts and honour occupied-slot capacity; legal consumption cannot leave a displayed phantom item (`RI-02B_UI_SPEC.md:126`). Buffers, platform/arrivals, reserved inbound, Home stock, pockets, truck cargo and project delivery **never sum into one spendable stock** (`:124`). Focused tests cover chest/truck conservation (`PLAYER_EXPERIENCE_CORRECTIONS.md`, 7 focused tests; `PLAYER_PLAYTEST_FOLLOWUP_02.md`, 10 focused tests).

---

## 6. Workshop and crafting presentation

**Status: Implemented and retained.**

The Backpack/storage drawer opens with a compact **Home workshop** section of recipe cards (`inventoryPanel.ts:recipeCard`, GP-PLAYTEST-FIX 3), replacing an older large "Equipment · two slots" block. Each card shows:

- output icon, name and quantity — e.g. **"Bullets ×10"**, **"Rifle ×1"**;
- the duration;
- one ingredient chip per input with icon, required and carried counts (`2 /20`), short chips highlighted;
- a **Craft** button and a concise missing-resource note;
- while a craft runs: a progress bar, remaining seconds and **Cancel**.

Names and tooltips remain alongside icons throughout.

The section's **first card is the Base core card** (`coreCard`, GP-HOME-REPAIR): "Base core · DISABLED", an HP `<progress>`, steel/copper chips against carried counts from the sim's `repairCost`, and one button queuing `repairDefence` — disabled with the `repairCheck` reason, or showing a countdown (`Recommission core · N s`, `Repair +N HP · N s`, "Repairing · N s left"), plus a note pointing at Home storage when the Backpack is short. **`E` on the Home always opens this drawer, damaged or not.**

Handcrafting locks the engineer in place: the sim refuses walking, dashing, aiming/firing and hand mining while a craft runs, and the HUD prompt reads **"Handcrafting — Cancel to move. · Escape cancels"**. Cancel on the card, Escape, closing the drawer (`cancelHandcraft` runs on drawer leave, GP-PLAYTEST-FIX 4), leaving workshop reach, or going down all cancel the batch and return its reserved ingredients — completed output stays, and a second cancel returns nothing. Factory machines keep running independently; movement and firing resume on the same tick the lock clears.

**Build panel** (`buildPanel.ts`, `buildCatalogue.ts`): four category tabs — **Production | Logistics | Power | Defence** — over a draggable catalogue grid, plus an item-detail block showing purpose (from `FACTORY_TEXT.purpose`), `Cost:`, and stock/lock status; "Add to action bar" with ten destination buttons; a `.placement-toolbar` carrying the selection name, `Rotate (R)` / `Reload (R)` and `Cancel (Esc)`; and a **"Blueprints and construction tools"** `<details>` holding "Use equipped weapon", history, clipboard and library. Undiscovered equipment is **omitted**, not greyed; known-locked equipment explains its recruit/requirement.

**Projects panel** (`campaignGuidePanel.ts`, 64 lines): a "Known projects" `<details>` introduced by

> "Explore for nearby clues. Only known places appear here; recruiting and restoring require a local visit."

then a project `<select>` and a `project-card` with title, status, detail, a **"Restoration service circuit"** `<ol class="service-nodes">` of ✓/○ material and power rows, an equivalent checklist of carried/home/will-deliver amounts, **"Tram kit · collect what fits"**, **"Radio precision upgrade"**, Track/Locate buttons, and dynamic action buttons carrying `aria-disabled` plus a reason. It ends with an **"Items and recipes"** `<details>` item guide closing on:

> "Recruit the Foreman to use the blueprint clipboard: Ctrl+C selects a layout and Ctrl+V previews paid construction."

---

## 7. The onboarding sequence, exactly as implemented

**Status: Implemented and retained** (GP-OPENING, 2026-09-11), with the divergences flagged in §7.9 and the two corrections in §7.0.

### 7.0 What the sequence has to make the player feel

The gameplay rule is `GAME_DESIGN.md` §4.0 — this is the presentation side of the same contract, repeated here only as the acceptance target for the screens below. The sentence the opening has to earn:

> *"I built a turret, saw it protect me, automated its supply, and could then leave to explore."*

| # | Beat | What must be on screen | Label | Where |
|---|---|---|---|---|
| 1 | First **fully loaded** operational Home turret | `Loaded N / cap · M more to fill …`, ending "Once it is fully loaded, a small enemy group will test it." | **IR** | §7.3 row 14 |
| 2 | Short directional warning | **Small enemy group approaching** naming a compass direction, plus the threat marker on the real origin tile and `Arrives in N s` | **IR** | §7.4 |
| 3 | Small introductory attack from one approach | **Defend your turret**, ~5 basics, withdrawal after 5 minutes stated in the detail | **IR** | §7.4 |
| 4 | Actual ammunition-consumption feedback | **Attack repelled** — "Your turret used N bullets" with the real count, for 60 s | **IR** (detail wording **INC**, §7.5) | §7.4 |
| 5 | Automatic replenishment through **real delivery** | The five resupply texts, then **Automatic resupply working** for 45 s — reached only by belt/inserter delivery | **IR** (wording **INC**, §7.5) | §7.5 |
| 6 | Three turrets covering different approaches | `Expand your defences (N/3)` pointing at an uncovered approach, with "not guaranteed protection" in the detail | **IR** | §7.6 |
| 7 | A useful nearby excursion while production continues | `Prepare to scout` → the nearest freight camp, with the base still producing | **IR** | §7.7 |

Labels: **IR** = implemented and retained · **INC** = implemented but needs correction · **ANI** = approved, not implemented.

**Two corrections the port must make** (both are gameplay rules with a visible consequence, so they are stated in full in `GAME_DESIGN.md` §4.0 and §4.4):

| # | Intended | Current | Label |
|---|---|---|---|
| 7.0-a | A **temporary** active raid **defers** the introductory encounter until the player is safe; the notice should say *when* it will come, not that it is gone. Only a genuinely progressed save loses it permanently | A live raid, or a major inside 300 s, **skips** the encounter outright with a notice (`campaignThreat.ts:209`), as does a raid starting during the 25 s warning. Deferral must be bounded so ordinary raids are never postponed indefinitely | **INC** — owned by TASKS.md **C-08** (minimal raid behaviour: an active raid defers the intro once, bounded) |
| 7.0-b | Player-facing ammunition wording is **bullets** everywhere | Five goal strings and one Backpack message still say "Shot magazines" / "Magazines" / "magazine" (§7.5) | **INC** |

**Playable-opening checks** (proposed for C-ACC): a **short excursion** (leave with ≥ 8 bullets, reach the nearest freight camp, return with production still running) and **one representative subsequent ordinary attack at the chosen turret rate** — the reference explicitly never verified the first ordinary Home raid at 1 shot/s (GP_CHECKPOINT second pass, "Checks"). A **controlled** attack check may be staged with debug tooling instead of waiting out the director; the reference's dev aids are the `session.ts` URL parameters (`?view=world`, `rules=`, `seed=`, `state=`, `autoplay=`, `stalker=1`, `heart=1`; `packages/game/src/session.ts:21-45`) and the headless defence-scenario harness (`packages/harness/src/defenceScenario.ts`), and **no raid-forcing parameter exists**, so Unity needs an editor-only debug command for it (**ANI**). A staged fight is reported as a **controlled check** — it proves readability and mechanics, **not** natural pacing. No full campaign run is required.

### 7.1 Entry

New Game means a **bare exploration entry**: no title screen, no cutscene, no tutorial modal. The player lands in the world at `?view=world` (the default) under `rules=exploration-v2`, at Founders Court. `CLAUDE.md`: "Normal new-game entry is to use exploration-v2"; "Current preview is rebuilt mutable port 5178, **bare exploration entry**."

**Starting stake** (`packages/sim/src/rules.ts:8-10`, GP-START-POCKETS):

```ts
/** GP-START-POCKETS (2026-09-11, owner request): the campaign Backpack opens with a small salvage stake so the first hand-mining session
 *  is short — 20 of the Generator's 30 steel and 5 of its 10 copper. Home storage stays empty; no Generator, fuel or ammunition is granted. */
export const CAMPAIGN_START_POCKETS = { steel: 20, copper: 5 } as const;
```

(The values are Worker A's rule; quoted here because they set the very first goal text.)

### 7.2 The only hint overlay

`hud.ts:32` creates a dismissible `hud-opening-hint` with a **Dismiss hint** button. The text is rendered from live bindings, so with defaults it reads:

> "`E` interacts · WASD moves · Build opens with `B`. Find all controls in Help."

and `hud.ts:69` updates it, once movement or mining is still undemonstrated, to:

> "`E` interacts · `W`/`A`/`S`/`D` moves · `B` opens Build. Hold left-click to mine."

Dismissal writes `dismissedHints: ['opening']` to `relight.ui.v1`. `PLAYER_EXPERIENCE_CORRECTIONS.md` scope F: "Hints persist until movement/mining are demonstrated or dismissed." Settings offers **"Restore opening hint for new cities"**.

### 7.3 The objective chain

Everything else is delivered through the **single goal card**, driven by `campaignNext()` in `packages/sim/src/goal.ts`. It is a priority list, not a script: each frame it returns the first unmet step. Exact strings, in order (`goal.ts:82-167`):

| Order | `title` | `text` | Note |
|---|---|---|---|
| 0a | `Recover at Home` | `Back on your feet in N s` | Engineer down; pre-empts everything. |
| 0b | `Restore Home power` | `Aim at the Home core and press E to repair` (or the `repairCheck` reason) | Disabled Home takes priority. |
| 0c | *(tracked project passthrough)* | `Deliver N <item>` or the project status | When the player has tracked a project. |
| 0d | `Riding the tram` | `E to disembark safely` | |
| 1 | `1 · Build a Generator` | `Place 1 Generator in Founders Court` | |
| 2 | `2 · Build an Excavator` | `Place 1 Excavator at a resource patch edge, with a clear side for belts` | |
| 3 | `3 · Build storage` | `Place 1 Supply chest near the Excavator` | |
| 4 | `4 · Connect belts to storage` | `Connect Excavator → Supply chest (resources per Belt)` | detail: "Use at least 1 Belt; the total depends on the gap. Point its arrow away from the Excavator and into the chest. R rotates. Clear salvage along the route; **inserters are not needed**." |
| 5 | `Fuel your Generator` | `Gather Coal, then open the Generator inventory` | |
| 6 | `Connect your extraction power` | `Place Poles between your Generator and Excavator` | |
| 7 | `Start your extraction line` | *(the machine's own status reason)* | |
| 8 | `Connect Founders Court’s substation` | `Chain Poles from your network to the Founders Court substation` | GP-POWER-FIX. The substation is at tile **(78, 347)** — see `WORLD_AND_ASSETS.md` §4.5. |
| 9 | `Prepare your expedition Rifle` | `Craft a Rifle at Home workshop · N s` | |
| 10 | `Equip your crafted Rifle` | `Open Backpack, select Rifle, then Equip in slot 1` | |
| 11 | `Make turret ammunition` | `Open Home Workshop → Craft 10 bullets` | |
| 12 | `Prepare your first turret` | `Prepare your first turret. Build it near your base and fill it with ammunition.` | Placement ring shows the `TURRET_RANGE` coverage; turret draws `TURRET_KW` (20) kW. |
| 13 | `Connect your turret power` | `Place Poles so your turret sits within coverage of your powered network` | |
| 14 | `Prepare your first turret` (loading) | `Loaded N / cap · M more to fill …` | detail ends "Once it is fully loaded, a small enemy group will test it." |

Every `buildStep` detail ends with the same reminder:

> "Opening order: 1 Generator → 1 Excavator → 1 Supply chest → Belts into the chest."

and, when materials are short, begins "Still need N <item> (M available at Home). Hold left-click on salvage to gather, or collect stored supplies." — otherwise "Materials ready in Backpack. Open Build to place it."

### 7.4 The opening encounter

`packages/sim/src/openingEncounter.ts` (98 lines) is the state machine:

```ts
export type OpeningStatus = 'pending' | 'scheduled' | 'active' | 'repelled' | 'lost' | 'skipped';
export const OPENING_ENCOUNTER = { warning:25, count:5, maxDuration:300, recovery:300,
                                   guard:660, ack:60, supplyAck:45 } as const;
export const COMPASS: Record<string,string> = { N:'north', NE:'north-east', E:'east', SE:'south-east',
  S:'south', SW:'south-west', W:'west', NW:'north-west' };
```

Player-facing strings (`goal.ts:130-132`):

| Status | `title` | `text` |
|---|---|---|
| `scheduled` | **Small enemy group approaching** | "Small enemy group approaching from the {direction}. Stay near your turret and help defend." |
| `active` | **Defend your turret** | "Small enemy group attacking from the {direction}. Stay near your turret and help defend." |
| `repelled` | **Attack repelled** | "Attack repelled. Your turret used N bullets. Connect ammunition production to keep it supplied." |
| `lost` | **Attack over** | "Your turret used N bullets. Connect ammunition production to keep it supplied." |
| `skipped` | *(no card)* | Progressed saves never see it. **INC:** `skipped` is also reached when a raid happens to be live or a major is within 300 s — a *temporary* condition that should defer rather than cancel (§7.0-a). When the port adds deferral, the presentation gains a **deferred** notice that says when the encounter will arrive; there is no such string today. |

`{direction}` comes from `COMPASS`. The `scheduled` detail adds "Arrives in N s · about 5 basic enemies from the {direction} marker. Your turret fires automatically within TURRET_RANGE tiles… use your Rifle on anything that gets past it." The `active` detail adds "…the group withdraws once beaten or after **5 minutes**" (`maxDuration/60`).

### 7.5 Automated resupply

`Automate your turret’s ammunition supply` has **five** distinct texts (`goal.ts:138-144`). They are quoted here **as they are in the tree**; rows 1 and 5 (and the detail strings at `goal.ts:132`, `:140`, `:143`, plus `flow.ts:2142` "No Shot magazines in Backpack. Open Home workshop to craft them.") are **Implemented but needs correction** — the player-facing item is **Bullets** (`itemNames.ts:6`) under `ammoVersion 1`, and `magazine` / `Shot magazine` survive only as the **internal item id** and **internal recipe name** (`flow.ts:155`, `:175`). The port must use the intended wording:

| # | Current string (do not copy) | Intended Unity wording |
|---|---|---|
| 1 | `Build 1 Assembler and set it to Shot magazines` | `Build 1 Assembler and set it to Bullets` |
| 2 | *(the Assembler's own status reason — not running)* | unchanged; its detail "Inspect your magazine Assembler…" becomes "Inspect your bullet Assembler…" |
| 3 | `Extend the route from the Assembler output to a turret` / `Connect the Assembler output to your turret` | unchanged |
| 4 | `Route connected; waiting for the Assembler to produce` | unchanged; its detail "Follow the first magazine along the belt" becomes "Follow the first bullets along the belt" |
| 5 | `Magazines produced; waiting for the first one to reach the turret` | `Bullets produced; waiting for the first batch to reach the turret` |
| — | `goal.ts:132` detail: "An Assembler set to **Shot magazines**, with a belt into the turret…" | "An Assembler set to **Bullets**, with a belt into the turret…" |
| — | `flow.ts:2142`: "No **Shot magazines** in Backpack. Open Home workshop to craft them." | "No **Bullets** in Backpack. Open Home workshop to craft them." |

then, for 45 s (`supplyAck`):

> **Automatic resupply working** — "Automatic resupply working. Your production line is replenishing the turret."

`noteTurretSupply` records `suppliedAt` **only for belt/inserter delivery** — the comment in `openingEncounter.ts` is explicit: "hand loading never records a supply chain". `supplyChainReaches` walks belts, undergrounds, splitters and inserters through up to **4** relay stores (`RELAYS = ['chest','tramstop','depot']`).

### 7.6 Three-turret guidance

`turretRecommendation` (`goal.ts:70`) — title `Expand your defences (N/3)`:

> "Larger attacks can approach from any direction. Build {word} more turret{s} and spread your defences around the base. Enemies can attack from any direction, so cover different approaches."

detail:

> "Three is a starting recommendation, **not guaranteed protection**. Keep turrets supplied, cover different approaches and support them with your Rifle. Existing turrets count; disabled turrets need repair."

Then `Load your new turret` — "Load bullets into the empty turret or extend your ammunition belt to it".

### 7.7 Exploration objective

`Prepare to scout` — "Carry at least eight bullets for the first camp", detail: "Your turrets are supplied and your Rifle is ready. Check turret ammunition and generator fuel before leaving; the reserve is finite. The nearest freight camp is a short trip from Home."

The chain then hands off to the freight-camp / plant / core steps sourced from `campaignDiscoveries()`, and finally to the terminal state:

> **Keep your workshop producing** — "Explore and connect your known destinations." / "Your existing production is running. Use Projects for restoration and service details."

### 7.8 Recovery paths and progressed saves

- `initOpeningEncounter` marks progressed saves **`'skipped'`**. Its comment: "*Fresh campaigns wait for the first full turret; progressed saves never receive a beginner attack.*" This case is correct and stays (**IR**) — it is the *only* one that should permanently skip; see §7.0-a for the case that should not.
- If the attack is **lost**, the chain offers `Rebuild your turret` with "The attack disabled your defence." prefixed to the usual detail.
- A disabled Home outranks everything except engineer recovery (`goal.ts:84`).
- `campaignGuide.ts` refuses to invent progress: "Only known locations. No hidden counters, locations, roster or attack query enters this model." `SITE_CLUE_RADIUS = 24`. Status strings: `'Restored · core disabled'`, `'Restored · powered'`, `'Restored · no power'`, `'Discovered · awaiting restoration'`. Action labels: `'Deliver and restore'`, `'Deliver carried materials'`, `'Restore'`. Blockers: `'Local power is still required to restore.'`, `'More carried materials are required to finish.'`, and `local()` → `'Walk closer on foot to interact.'`
- `RI-02B_UI_SPEC.md:104`: for an older save with no historical fact, say "**No working machine**", never "You never built one".

### 7.9 Divergences from the spec

| Spec says | Code does | Label |
|---|---|---|
| `RI-02B_UI_SPEC.md:102` — default objective "Start your workshop", with "Collect your starting supplies from Home" | The chain opens on `1 · Build a Generator`; the starting stake (`CAMPAIGN_START_POCKETS`) removes the Home-collection step entirely | **Retired** (superseded by GP-START-POCKETS, 2026-09-11) |
| `RI-02B_UI_SPEC.md:56` — pause offers "return to title/start screen" | Only "Start a new city" (a seed prompt + `location.href`) exists; no title screen | **Approved but not implemented** (§2.1) |
| `RI-02B_UI_SPEC.md:112` — quickbar defaults to belt, inserter, excavator "plus seven empty slots" | Slot 9 is **rifle** by default (`DEFAULT_QUICKBAR`) | **Implemented and retained** (code wins) |
| `RI-02B_UI_SPEC.md:110` — mandatory inserter extraction | D-UI-12 supersedes it; conveyors load and unload directly, and the onboarding detail says "inserters are not needed" | **Implemented and retained** |

---

## 8. Feedback and readability — what actually exists

| Feedback | Exists? | Detail |
|---|---|---|
| **Mining feedback** | **Implemented and retained** | `hud-mining`: title + `<progress>` + detail. Empty-hand rubble hover explains holding left-click and the current mining rate; while held it shows real simulation progress toward the next item, collected amounts for that hold, and current Backpack totals; the receipt is retained briefly on release; pause, range, capacity, depleted rubble and machine-only sources are explained; **menus cancel mining and hide its feedback** (`RI-02B_UI_SPEC.md:222`). |
| **Shot visibility (tracers)** | **Implemented and retained** | "Player and turret shots draw a brief tracer from the actual muzzle toward the resolved hit or range end with an impact ring; tracers are **renderer-only readbacks of resolved sim shots and never add damage**" (`RI-02B_UI_SPEC.md:239`). |
| **Turret rotation** | **Implemented and retained** | Coordinator correction (2026-09-11): the sim tracks a per-turret muzzle angle in `packages/sim/src/turretTracking.ts` (`TURRET_TURN_SPEED = 2π` rad/s, shortest-path rotation, the base never turns) and the renderer draws the barrel from that angle at `packages/game/src/worldScene.ts:1717` with the tracer leaving the real muzzle at `:1470`. Owner decision U-D-15; Unity turret prefab = fixed base + rotating cannon child (MIGRATION_MAP.md S-31). |
| **Power outage / connection messages** | **Implemented but needs correction** | Persistent **"No power · Founders Court"** (or the affected site name) while a circuit has no generation or its core is disabled; the clock shows it separately from combat/schedule alerts; open drawers retain an **outage badge**; local and full maps show crossed lightning and the full map labels Home **NO POWER**; supplying fuel or repairing/reconnecting clears it; uncommissioned plants are **not** reported as failed bases. HUD strip: `'Power: 0 / 0 kW · no Generator linked'` / `'Power: disconnected'`. See defect U-3. |
| **Power placement preview** | **Implemented and retained** | GP-POWER-FIX: a Pole/Big pole/Generator ghost draws **purple lines** to every node `powerLinksAt` would cable to; a consuming machine draws one line to the single node it would join; nothing when out of reach. Node ghosts preview even while the cursor reads "walk closer" or a shortfall, so a chain can be planned before walking; a consuming machine's preview needs a placeable ghost. The cursor reason names the linked nodes and whether the network is supplied (`RI-02B_UI_SPEC.md:116`). |
| **Conveyor direction** | **Implemented and retained** | D-UI-12: always-visible **cyan IN / amber OUT** arrows on usable machine/storage sides; hover and placement expand to every edge tile with exact inserter pickup/drop tile outlines; hover legend, inspection, catalogue and storage hints explain conveyor direction and automatic transfers. |
| **Network demand / supply** | **Implemented and retained** | `Name: D / S kW` in the status strip; inspection separates measured observation window, nominal output, power-limited capacity, local circuit supply/load/demand and global production. "No measurement yet" reads **"Waiting for simulation time"**, not zero throughput. "No local load" is neutral, not a red failure (`RI-02B_UI_SPEC.md:122-124`). |
| **Day / time** | **Implemented and retained** | `Day N · HH:MM[ · Paused]` in the status strip. Paused state must be explicit (`RI-02B_UI_SPEC.md:144`). No hardcoded "Night 3" in HUD text (`:142`). |
| **Flashlight** | **Implemented and retained** | D-UI-11: a mouse-aimed visibility beam — provisional 12-tile reach, 60° width, soft falloff, small origin glow. It **only improves visibility**: no power, no rot clearing, no shade vulnerability. UI input capture holds its previous direction; only the visible lighting surface recomposes as the pointer moves; static city lighting stays cached. Home's complete lot is fully lit in both the render mask and sim light queries, including old saves. |
| **Map** | **Implemented and retained** | D-UI-13: the map is for **inspection and pins**. White player pointer on dark backing with a mint halo and a **YOU** label; the minimap uses the same treatment. **Clicking the map never issues a walk command** — Shift-click creates a pin. The legend `<details>` "Map & world symbols" ends "Map clicks inspect; they never walk." |
| **Discovery cue vocabulary** | **Implemented and retained** | Teal accessible doors, crossed boards, a known-survivor marker and known-hostile warning triangles; a collapsible map legend explains the existing symbols (`PLAYER_EXPERIENCE_CORRECTIONS.md` scope G). |
| **Alerts** | **Implemented and retained** | At most one highest-priority urgent strip plus three short transient notices; a keyed inbox preserves dismissed history and marks resolved conditions; one row updates with a repeat count rather than stacking. Base damage / engineer danger outranks schedule, then production. Dismissal never clears a real danger indicator (`RI-02B_UI_SPEC.md:144`). |
| **Audio** | **Does not exist in the reference; approved for Unity** | The reference has no audio files and no audio engine (`RI-02B_UI_SPEC.md:17,194`; `WORLD_AND_ASSETS.md` §9). **Correction, 2026-09-11:** audio is now **in scope** for the Unity port by owner decision (Q09), reversing the recorded "silence with a hook point" default. The cue inventory, its seven categories and the source system behind each cue are **§12**; the volume controls are §3.8.1; asset sourcing and licensing are `WORLD_AND_ASSETS.md` §9.2. Every cue is a **presentation readback of a resolved sim fact** — the same rule tracers already follow — and nothing in the game is signalled by sound alone. |

Accessibility contract (`RI-02B_UI_SPEC.md:38-43`): 3 px ivory focus outline at 2 px offset, keyboard focus visually distinct from selected tool; amber selection border **plus a selected label**; disabled controls keep readable text and an adjacent reason; tooltips max 320 px, wrap, available on click/focus as well as hover, clamped 12 px inside the viewport; text contrast ≥ 4.5:1 (large/boundaries ≥ 3:1); reduced motion gives instantaneous state with persistent completed text and **no blinking**. `style.css:43-44` currently animates a red pip with a `blink` keyframe — **Implemented but needs correction** against that no-blinking rule, though `@media (prefers-reduced-motion: reduce)` disables toast animation only (`style.css:66`). **UNCERTAIN:** whether the pip blink is also suppressed under `motion: 'reduce'`.

---

## 9. Known interaction defects and playtest feedback

Source: `docs/Implementation/GP_CHECKPOINT.md` (2026-09-11) unless noted. "Fixed in tree?" means fixed in the current uncommitted working tree.

| # | Defect / feedback | Source report | Fixed in tree? | Required Unity behaviour |
|---|---|---|---|---|
| U-1 | **Drops onto the rightmost slot (24) and a scrolled slot (39) failed.** | GP-PLAYTEST-FIX second pass | **Yes** | Every inventory slot must be a valid drop target, including the last column and any slot reached only by scrolling. Hit-test in layout space, not by a cached visible-slot list. Add a regression test that drops on the last slot and on a slot below the fold. |
| U-2 | **Dragging the gun to slot 1 also filled slots 5 and 9.** Owner: *"When I drag the gun to the first slot, it adds it to slot 5 and 9 as well."* | GP-HOME-REPAIR, owner quote | **Yes** | One tool, one slot. All assignment paths go through a single `AssignQuickbar` that **swaps** (displaced tool takes the vacated slot). On load, collapse duplicates to the first occurrence. **Owner browser retest is still open** for this fix. |
| U-3 | **"Power outage" toast appears at game start.** Owner retest: *"The no power message still shows up when the game starts."* Traced to a **legacy brownout step in `sim.ts`** producing a second toast path after the primary fix. | GP-POWER-FIX, owner retest | **Partially** — primary path fixed; the second path was traced and addressed, but the owner retest is recorded as **open** | A fresh campaign must not show any power-failure alert before the player has built a generator. Unity should have exactly **one** power-alert producer. Do not port two. |
| U-4 | **`R` (reload) was ignored while the Backpack was open.** | GP-PLAYTEST-FIX "Rifle and reload follow-up" | **Yes** — `inventoryPanel.ts:66-70` handles the `rotate` binding as Reload **before** the shell blocks world keys | Panel-owned keys must be resolvable inside the panel. Note the checkpoint's own caveat: *"The owner's exact original interaction is unknown; these are reproduced control gaps."* |
| U-5 | **Full-width header strip.** Owner: *"The header bar with the day count, time count and kw display is full width. I feel like it should be smaller."* | GP-HUD-STRIP | **Yes** (CSS only) | Content-width status strip (§4.2). **Owner browser retest open.** |
| U-6 | **Base repair was hard to find when Home had no health.** Owner: *"When the base has no health left, its hard to repair…"* | GP-HOME-REPAIR | **Yes** — the Base core card is now the first card in the Home workshop drawer, and `E` on Home always opens that drawer | Repair must be reachable in one interaction from the damaged object, not buried in a management screen. Screenshots 70–72 in the session scratchpad; **owner browser retest open**. |
| U-7 | **Rifle ownership / equip drag, and chest drop-target validation.** | Player-test corrections, 2026-09-11 (first pass) | **Yes** | Weapons only onto equipment/action-bar slots, with the explicit refusal message (§5.6). |
| U-8 | **Hand-mining pacing** and workshop-card size. | Second pass | **Yes** | Compact recipe cards (§6); hand-crafting locks movement with a visible prompt. |
| U-9 | **Misleading machine-level recovery suggestion** after a lost-power session. | `PLAYER_EXPERIENCE_CORRECTIONS.md`, "Observed — ordinary failure" | **Yes** — replaced by Home-core repair guidance | When the base core is down, the objective must name the core repair, not a machine-level fix. |
| U-10 | **Duplicate warning text and competing default-open panels** in the pre-redesign baseline. | `RI-02B_UI_SPEC.md:19` (baseline image) | **Yes** (whole RI-02B redesign) | One drawer, one warning strip, no default-open panel. Avoid duplicating `currentGoal.support` with defence/header warnings (`:144`). |
| U-11 | **Heart wiring: a ~29-tile break near the tram corridor that a 12-tile Big pole cannot bridge.** | `PLAYER_EXPERIENCE_CORRECTIONS.md`, "Practical findings" | **No** — the blocker is now *explicit*, the route problem is unsolved | **Unresolved design question**, not a UI bug. Flagged here because the *symptom* is player-facing. Belongs to Worker A / the owner. |
| U-12 | **Furnace / Crown payoff unproven.** | Same | **No** | **Unresolved.** Worker A. |

**Open owner retests on port 5178** (mutable preview): GP-HOME-REPAIR (base core repair + action bar, U-2/U-6) and GP-HUD-STRIP (top status strip, U-5). `CLAUDE.md`: "Reload 5178 for owner retest."

### 9.1 The three browser retests become Unity checks — Owner decisions 2026-09-11 (Q20)

**Confirmed scope (owner):** carry the intended fixes into Unity and **verify them there**. No further browser development, and no completion of the historical browser retests, is required before progressing. Evidence status is preserved honestly: **an unperformed test is never recorded as passed.**

| Item | Browser evidence status — unchanged | The Unity check it becomes |
|---|---|---|
| **U-2** (GP-HOME-REPAIR, action bar) | Fixed in the working tree; **owner browser retest not performed** | **One tool occupies one action-bar slot.** Assign the same tool by catalogue drag, by an "Add to action bar" destination button and by a Backpack weapon drop; after each, exactly one slot holds it and any displaced tool sits in the vacated slot. Load an arrangement holding one tool several times and assert it collapses to the first occurrence. |
| **U-6** (GP-HOME-REPAIR, core repair) | Fixed in the working tree; **owner browser retest not performed** | **Core repair is reachable in one interaction from the damaged core.** Interact with a damaged Home core and assert the repair control is present and actionable in the surface that opens — no intervening screen, and the same for an undamaged core (the drawer still opens). |
| **U-5** (GP-HUD-STRIP) | Fixed in `style.css` only; **owner browser retest not performed** | **The HUD status strip is content-width.** At each layout in the target matrix (§2.5), assert the strip's measured width equals its content width and that it does not span the gap between the goal card and the minimap, and does not overlap either. Port the *intent*, not the pixel offsets of §4.2. |

The Unity checks are C-12's (TASKS.md); C-ACC is not held for the browser verdicts, and a later negative owner verdict becomes a new corrective row rather than a retrospective failure. Until a Unity check has actually run, its row says so — these three are **not yet verified anywhere**.

---

## 10. UI assets and style tokens

### 10.1 Design tokens — **Implemented and retained**

`packages/game/src/style.css:120-132` is the shared token block (RI-02B). Legacy names at `:129-130` are adapters so older domain panels keep working.

```css
:root {
  --ui-scale:1;
  --ui-bg:#151d1e; --ui-surface:#203536; --ui-raised:#2b4546;
  --ui-text:#f3ecdd; --ui-secondary:#c2ceca; --ui-accent:#e3b96f;
  --ui-working:#92d8b5; --ui-danger:#ff8a80; --ui-border:#8da79f;
  --ui-space-1..8: 4/8/12/16/24/32px * scale;
  --ui-radius:6px * scale;
  --ui-body:17px; --ui-small:16px; --ui-heading:20px; --ui-title:24px  (all * scale);
  --ui-world:0; --ui-hud:10; --ui-drawer:20; --ui-inventory:21;
  --ui-tooltip:30; --ui-modal:40; --ui-alert:50;
  --ui-nav-height:64px * scale; --ui-drawer-inset:0px * scale;
  font-family:"Segoe UI",system-ui,sans-serif; font-size:var(--ui-body); line-height:1.45;
}
```

| Token | Value | Semantics (binding per `RI-02B_UI_SPEC.md:31-41`) |
|---|---|---|
| `--ui-bg` / `--ui-surface` / `--ui-raised` | `#151d1e` / `#203536` / `#2b4546` | Charcoal ground, petrol panel, raised control. **Opaque behind body text.** |
| `--ui-text` / `--ui-secondary` | `#f3ecdd` / `#c2ceca` | Warm ivory, readable secondary. |
| `--ui-accent` | `#e3b96f` | Amber/brass — primary actions, selection, focus. |
| `--ui-working` | `#92d8b5` | Mint — working/OK. |
| `--ui-danger` | `#ff8a80` | Red — **immediate danger/failure only**. |
| `--ui-border` | `#8da79f` | 1 px panel separation, 2 px selected edge **plus a word or symbol**. Changed from the spec's candidate `#69837D` after that failed raised-control contrast (`RI-02B_UI_SPEC.md:214`). |
| Hit targets | ≥ 40 × 40 logical px; quickbar slots 48 px (spec) / 60 px default (UI-08) | |
| Motion | 150 ms panel fade; one 250 ms circuit-close response; reduced motion = instantaneous + persistent completed text, no blinking | |

Design identity (`RI-02B_UI_SPEC.md:25`): "municipal electricity-authority field equipment; enamel signs, service diagrams and switchboard clarity. **No grime behind text, scanlines, flicker, decorative gauges, emoji icon system or borrowed assets.**"

Note the file carries **two** `:root` blocks: the legacy one at `:1-8` (`--bg:#0b0e1a`, `--panel:#121a30`, `"IBM Plex Sans"`) and the RI-02B one at `:120` which overrides it. The legacy block still requests **IBM Plex Sans, which is never shipped** — see `WORLD_AND_ASSETS.md` §9. **Implemented but needs correction.**

### 10.2 Icons

All item and tool icons are **inline SVG path strings inside `packages/game/src/itemIcons.ts`** — `iconMarkup` renders them into a 64 × 64 viewBox, with a long alias table mapping item IDs to icons. There are no icon image files anywhere. `RI-02B_UI_SPEC.md:114`: "`itemIcons.ts` supplies original 64-unit SVG item illustrations, reused by catalogue, dock, backpack and drag ghost." `:43`: "custom simple inline SVG paths on a consistent 24-unit grid … Every category and consequential action keeps a text label. No image generation is needed for these vector symbols."

**Migration cost:** Unity must extract these path strings into real assets. Recommended: a one-off script emitting one `.svg` per icon, imported either through the **Vector Graphics** package (keeps them crisp at any UI scale) or rasterised to a sprite atlas at 3× the largest use (64 px slot art → 192 px sprites). The inventory is in `WORLD_AND_ASSETS.md` §8.

### 10.3 Unity UI recommendation

Build the interface in **UXML + USS via UI Builder** (Unity UI Toolkit), not uGUI:

- The current UI *is* a retained-mode DOM tree with a cascading stylesheet. UXML/USS is a near-1:1 target: `section.ui-drawer` → a `VisualElement` with the `ui-drawer` USS class; the token block → USS custom properties (`--ui-accent` etc. work natively in USS); `--ui-scale` → the panel settings' scale mode plus the same `calc()`-style multiplications.
- The three responsive states (default / `ui-compact` / `ui-short`) map to USS classes toggled from one resize handler, exactly as `settingsPanel.applyUiPreferences()` does today.
- `[hidden]` → `element.style.display`/`DisplayStyle.None`; `role`/`aria-*` → Unity's accessibility hierarchy where supported.

**Worker B owns the wiring** — how UI Toolkit binds to the ported sim, event routing, the adapter registry, and the input-ownership layer belong in `TECHNICAL_ARCHITECTURE.md`. This section only asserts the *presentation technology* recommendation and the token values it must reproduce.

---

## 11. Open questions for the coordinator

1. **Front-end screens do not exist (§2.1).** A Unity build needs a title screen, New Game, Continue, Load-slot list and Settings — none of which have ever been designed or built. Who specifies them, and is the slot list allowed to show metadata (day, seed, playtime) that no save currently surfaces in the UI?
2. **Gamepad support (§3.7).** No gamepad code, no binding scheme, no decision on record. Is pad support in scope for the Unity port? If yes, several rules need answers: the ten quickbar slots, drag-and-drop transfers, and the map's inspect-only click model are all mouse-shaped.
3. **Two owner retests are still open (§9).** GP-HOME-REPAIR (U-2, U-6) and GP-HUD-STRIP (U-5) are marked "Owner browser retest open" on port 5178. Should the Unity port treat the current fixes as settled behaviour, or wait for the retest verdicts?
4. **Turret rotation (§8).** *Resolved by the coordinator:* rotation exists (sim `turretTracking.ts`, renderer `worldScene.ts:1717`); recorded as U-D-15 and MIGRATION_MAP.md S-31. No owner question remains.
5. **Reduced motion and the blinking pip (§8).** `style.css:43` animates a red pip with `@keyframes blink`, against the spec's explicit "no blinking" reduced-motion rule. Is the pip an intentional exception, or a defect to fix in the port?
6. **Font (§10.1).** `"IBM Plex Sans"` is requested but never shipped, so the real font is whatever the OS provides. Unity must bundle a licensed TMP/UI Toolkit font. Which one — IBM Plex Sans (Open Font Licence, so shippable), or a new choice with the owner's art package?
7. **Icon extraction (§10.2).** Vector (Unity Vector Graphics package) or rasterised atlas? This affects how the UI behaves at 150 % scale and on high-DPI displays, and it is cheaper to decide before the extraction script is written.
8. **The starting stake changed the opening (§7.9).** `CAMPAIGN_START_POCKETS` (2026-09-11) removed the spec's "Collect your starting supplies from Home" step. Confirm the Unity port follows the current code, not `RI-02B_UI_SPEC.md:102`.
9. **Audio in scope?** There is no audio at all. If Unity is expected to ship with sound, that is a whole new content workstream with no existing spec beyond "future audio plugs into settings" (`RI-02B_UI_SPEC.md:194`).

*Coordinator disposition (2026-09-11):* Q1 → DECISIONS.md U-Q-18 (C-10 builds the minimal set); Q2 → U-Q-19; Q3 → U-Q-20 (C-12 ports the fixes as intended behaviour); Q4 → resolved, rotation exists (U-D-15, MIGRATION_MAP.md S-31); Q5 → U-M-16 (defect; reduced motion wins); Q6 → U-M-16; Q7 → U-M-10; Q8 → confirmed, code wins (U-D-18, the starting stake); Q9 → U-Q-09.

*Owner decisions, 2026-09-11 — Q1, Q2, Q3 and Q9 are now **answered**, not open:*

| Was | Now | Recorded in |
|---|---|---|
| Q1 front-end screens | **Answered (Q18).** New Game, Continue, Load, Settings; day, playtime and save time shown; Continue picks an appropriate valid recent save; confirmation before anything that overwrites or discards progress | §2.6, and §2.7 for the saving behaviour behind it |
| Q2 gamepad | **Answered (Q19).** Desktop keyboard and mouse only; gamepad out of initial scope; input architecture stays extensible with no unused controller interfaces, glyphs, prompts or Settings section | §3.7 |
| Q3 open owner retests | **Answered (Q20).** The fixes are carried into Unity and verified there as three plain checks; no further browser development is required; unperformed tests stay recorded as unperformed | §9.1 |
| Q9 audio in scope | **Answered (Q09) — reverses the recorded default.** Audio is **in scope**, proportionate, with reusable properly-sourced assets and sensible volume controls. Voice acting, a bespoke soundtrack and an elaborate audio framework are explicitly **not** required | §12, §3.8.1, `WORLD_AND_ASSETS.md` §9.2 |

Q5 (blinking pip), Q6 (font), Q7 (icon extraction) and Q8 (starting stake) are unchanged and still stand on their coordinator dispositions.

**Still open and not settled by these decisions:** the ending (U-Q-02) — this document must not present commissioning the third plant, or any other current step, as victory; §7's chain ends on the truthful terminal message "Keep your workshop producing" and nothing further.

---

## 12. Audio cue inventory — **Approved but not implemented** (Owner decisions 2026-09-11, Q09)

**Confirmed scope (owner):** audio is **in scope**. Required coverage is the seven categories below: player and turret weapons; impacts and appropriate enemy cues; machine operation; crafting and construction feedback; raid warnings; UI interactions; environmental ambience. Implementation is **proportionate**, using **reusable, properly sourced assets**, with sensible volume controls (§3.8.1). Explicitly **not required**: voice acting, a bespoke soundtrack, an elaborate audio framework.

This reverses DECISIONS.md **U-M-24** ("Audio defaults to silence with a hook point") and answers **U-Q-09**. TASKS.md **F-04** moves off option (a); the deliverables its option (b)/(c) branch already names — a cue map, an asset list with licences, mixer and Settings entries — are what §12 and `WORLD_AND_ASSETS.md` §9.2 now begin.

### 12.1 The rules the cue map obeys

1. **A cue is a readback, never a mechanic.** `CLAUDE.md`: "Renderer-only effects cannot imply mechanics the sim does not implement." Every cue below fires from an already-resolved sim fact, exactly as tracers do ("tracers are renderer-only readbacks of resolved sim shots and never add damage", `RI-02B_UI_SPEC.md:239`). No cue may consume a resource, gate an action or change timing, and `Relight.Sim` never references audio.
2. **Nothing is signalled by sound alone.** Every cue in the tables has a named visible counterpart in this document. A player at zero volume loses nothing.
3. **Sound follows the clock.** At speed 0 (§2.4) continuous cues stop and one-shots do not queue up to replay on resume. Menus do not duck the world beyond what the mixer does.
4. **Budgeted like alerts.** Concurrent one-shots of the same kind are capped and coalesced the way repeated alerts are (§8: "one row updates with a repeat count rather than stacking"). A turret firing at the campaign rate of one round per second (`packages/sim/src/turretTracking.ts:4`, `CAMPAIGN_TURRET_RATE = 1`) is one cue per shot; a legacy-rate turret is not, and must voice-limit.
5. **Provisional.** Every cue *name* and mix choice below is provisional. The **coverage** is the owner's decision and is not.

### 12.2 Category 1 — Player and turret weapons

| Cue | Source system (verified) | Visible counterpart |
|---|---|---|
| Player weapon shot, one per pellet-group, varied per weapon kind | `firePlayerWeapon` (`packages/sim/src/playerBallistics.ts:33`) resolves the shot and calls `noteShot`, which pushes a `PlayerShot {x0,y0,x1,y1,t,hit}` onto `ThreatState.playerShots` (`:13-15`). The weapon kind is `activeWeapon(st)?.kind` (`:34`); profiles and pellet counts are `WEAPON_PROFILES` (`packages/sim/src/weaponProfiles.ts`) | The tracer drawn from the same `playerShots` record (§8) |
| Plasma bolt launch and travel | The `plasma` branch of `firePlayerWeapon` pushes a `PlayerProjectile` (`playerBallistics.ts:37`); `tickPlayerProjectiles` (`:45`) advances it | The bolt itself |
| Turret shot | `packages/sim/src/threat.ts:334` — the single line that spends the round: `m.inv.rounds-=1; m.out++; m.timer=TURRET_SHOT_FLASH; f.stats.fired++`, with the shot recorded as `a.shot={x,y,angle,t}` at `:336` | Muzzle flash for `TURRET_SHOT_FLASH = 0.08` s and the tracer (`turretTracking.ts:7`; `worldScene.ts:1712`) |
| Turret traverse, a short servo cue gated to actual rotation | `aimTurret` (`turretTracking.ts:13-18`) rotates at `TURRET_TURN_SPEED = 2π` rad/s and returns alignment; the angle lives on `TurretTracking.angle` | The barrel drawn from that angle (`worldScene.ts:1717`) |
| Dry turret / empty click, at most once per target acquisition | `threat.ts:333` — the shot is skipped when `(m.inv.rounds??0)<1` | `Loaded N / cap bullets` in the goal card (§5.7) |
| Reload | The `rotate` binding handled as Reload inside the Backpack (`packages/game/src/inventoryPanel.ts:66-70`, §9 U-4) and the placement toolbar's `Reload (R)` (§6) | The equipment strip's Reload control |

### 12.3 Category 2 — Impacts and enemy cues

| Cue | Source system (verified) | Visible counterpart |
|---|---|---|
| Hit on a body vs. a miss ending on geometry | The same `noteShot` carries `hit:boolean` (`playerBallistics.ts:11,13`), set from whether `cast` returned a target (`:40-41`) | The impact ring at the resolved end point (§8) |
| Damage applied to an enemy | The `impact` callback invoked at `playerBallistics.ts:42` with `weaponDamage(kind, distance)` (`weaponProfiles.ts`) | Health/label in the threat readout (`campaignThreat.ts:493`) |
| Enemy death | `damage(st,T,c)` returning true in the turret path (`threat.ts:337`), which also increments `T.stats.turretKills` | The body leaving the world |
| Per-type enemy vocalisation, distinct for the live roster | `CombatActor.kind` ∈ `skitter` · `spitter` · `guardian` (`packages/sim/src/gameplayCombat.ts:106` validates exactly this set); the player-facing names are "Skitter", "Spitter", "Freight guardian" (`campaignThreat.ts:493`) | The hostile's own label and marker |
| Spitter wind-up and projectile | `a.phase='windup'` with `a.until=st.t+def.windup` (`gameplayCombat.ts:55`); the projectile is pushed at `:51` | "attack winding up — move behind cover" in the threat readout (`campaignThreat.ts:493`) |
| Guardian charge | `a.kind==='guardian'` → `a.phase='charge'` (`gameplayCombat.ts:81`) | "charging along marked line" (`campaignThreat.ts:493`) |
| Enemy strikes the engineer / a structure | `damageDefence` and `damageCore` (`packages/sim/src/campaignDefence.ts:77,82`), called from `campaignThreat.ts:436,487,489` | The engineer-danger alert at priority 100 and the base-damage alert at 80 (`campaignAlerts.ts:24,27`) |
| Engineer down | The `engineer-down` sim event (`packages/sim/src/types.ts`, `SimEvent` union) and the `Engineer down` alert (`campaignAlerts.ts:24`) | `Engineer down · N s` in the HUD (§4) and the `Recover at Home` goal (§7.3 row 0a) |

**Roster note.** The owner's approved regular roster is five types — Skitter, Spitter, Stalker, Breaker, Howler — with guardians separate (Q13). Only **Skitter, Spitter and Guardian** exist as live campaign actors today (`gameplayCombat.ts:106`); **Stalker** exists as a separate diagnostic system (`packages/sim/src/stalker.ts`, `?stalker=1`), **Breaker** exists only as the legacy `role` flag (`threat.ts:65`; `campaignThreat.ts:192`), and **Howler appears nowhere in the reference** — a repository-wide search for it returns nothing. Cues for Stalker, Breaker and Howler are therefore authored against content that does not exist yet (TASKS.md E-13/E-14) and must not be recorded as ported.

### 12.4 Category 3 — Machine operation

| Cue | Source system (verified) | Visible counterpart |
|---|---|---|
| Per-kind running loop (Generator, Excavator, Assembler, Mixer, Pumpjack), attenuated by distance and voice-limited per kind | `machineRunning` — the exported alias of `running` (`packages/sim/src/flow.ts:666`) — is the single authority on whether a machine is working; `threat.ts:323` already gates turret behaviour on it | The machine's own animation and its inspection status |
| Machine starts / stops | The same predicate changing state | The status reason shown in inspection and used verbatim as goal text (§7.3 row 7) |
| Machine throttled by a power shortfall | `machineThrottle(st,m)` (used at `threat.ts:321`) | `Name: D / S kW` in the status strip (§4) |
| Power lost on a circuit | `campaignOutages` (`packages/sim/src/campaignAlerts.ts:17`), title `No power · {place}` | The persistent outage strip and the `⚡× …` badge (§8) |
| Generator runs dry | The `gen-dry` sim event (`types.ts`, `SimEvent`: "a Generator burned its last coal") | The Generator's own blocker text in the production warning (`campaignAlerts.ts:31`) |
| Belt/inserter movement bed, one ambient loop per dense cluster rather than per entity | The flow layer's belt advance (`advanceFlow`, `packages/sim/src/flow.ts`) | The moving items themselves |

### 12.5 Category 4 — Crafting and construction feedback

| Cue | Source system (verified) | Visible counterpart |
|---|---|---|
| Hand-mining strike loop and per-unit collection tick | `{type:'mineAt'}` → `setHandMine(st,[c.x,c.y])` (`flow.ts:2324`); progress is the hand state's `prog` (`flow.ts:1008`) | `hud-mining` title, `<progress>` and detail (§8) |
| Hand-craft start, the progress bed, completion, and cancel | `{type:'craft'; item; count?}` (`packages/sim/src/types.ts:203`); the stationary lock is `flow.ts:1465` and the both-hands rule `flow.ts:1008` | The recipe card's progress bar and remaining seconds, and the prompt "Handcrafting — Cancel to move. · Escape cancels" (§6) |
| Craft refused | The card's missing-resource note; `flow.ts:2142` for the ammunition case | The note itself |
| Placement confirmed | `{type:'place'; item; x; y; dir?}` (`types.ts:204`) and `{type:'construct'; edits}` (`:186`) | The ghost resolving into a built machine |
| Rotate / pipette / cancel placement | The `rotate`, `pipette` and `cancel` bindings (`packages/game/src/controls.ts`, §3.1) | The `.placement-toolbar` controls `Rotate (R)` and `Cancel (Esc)` (§6) |
| Removal and undo / redo | `{type:'removeArea'}` (`types.ts:178`) and `{type:'undoBuild'\|'redoBuild'}` (`:189`) | The removal preview and the build history |
| Core repair queued and completed | `{type:'repairDefence'; x; y}` (`types.ts:219`) → `startRepair` (`flow.ts:2340`); the interaction that offers it is `interaction.ts:30` | The Base core card's HP `<progress>` and its countdown strings (§6) |

### 12.6 Category 5 — Raid warnings

All raid audio hangs off **one** producer, `campaignAlerts()` (`packages/sim/src/campaignAlerts.ts`), which already assigns a numeric priority to every condition. Cues map to priority bands, so a new alert needs no new audio wiring:

| Band | Alerts at that priority (verified) | Cue |
|---|---|---|
| 100 | `Engineer down` / `Alien relay draining health` / `Engineer under attack` (`campaignAlerts.ts:24`) | Urgent personal sting, one shot, never looped |
| 90 | The threat alert while `approaching` or attacking — `Small enemy group approaching · from the {direction}`, `Small enemy group attacking`, `Base under attack` (`:28`) | **The raid warning.** A readable two-stage cue: an approach warning on entry to `approaching`, and a distinct attack-begins cue on entry to the active phase |
| 86 / 85 | `Shade · needs powered lighting` (`:25`), `Nearby threat · {kind} #{id}` (`:26`) | Short proximity cue, voice-limited to one at a time |
| 80 | `Base disabled/damaged · {place}` (`:27`), `Base core disabled` recovery phase (`:28`) | Structure-damage cue |
| 75 | `No power · {place}` (`:17`) | Outage cue (also category 3) |
| 65 / 60 | `Assault · location unavailable` (`:29`); `Assault in N min` (`:28`) | Low, non-urgent notice |
| 50 | `Major assault · N active min` / `Major assault · Night N` (`:30`) | Schedule tick — **no cue**; this is a standing readout, not an event |

**Truthfulness rules, which the audio must not break.** One major attack announces **one** target base (Q13–Q15 context; `campaignAlerts.ts:28` produces a single `threat:{block}` row from the one `target`). So: exactly one warning cue per announced attack, played once, never one per approach sector and never repeated while the same alert stands. The direction the cue accompanies is the real one — `openingDirection(st)` (`:28`) and the `COMPASS` table (§7.4) — and audio adds no directional information the HUD does not show. The introductory encounter (§7.4) is the same alert at priority 90 with `intro` set (`:28`) and uses the same cue: it must not sound more dangerous than the ordinary raid it is teaching.

### 12.7 Category 6 — UI interactions

Owned by this document. The cue list is deliberately short; a click on everything is noise.

| Cue | Trigger (verified) | Note |
|---|---|---|
| Panel open / close | `shell.open(id)` and `closeDrawer()` (`packages/game/src/uiShell.ts`); exactly one drawer at a time (§2.2) | One pair of cues for every drawer adapter — do not author twelve |
| Pause / resume | `uiShell.pause()` / `unpause()` (§2.4) | Resume is the only cue that plays while entering speed 1 |
| Escape step | The Escape chain (`uiShell.ts:114-120`, §3.5) | One quiet cue for "something was cancelled", not one per rung |
| Button confirm | Any primary action button | The shared Interface-bus reference cue (§3.8.1) |
| Refused action | `bindingProblem` messages (`controls.ts`, §3.2), the weapon-drop refusal (§5.6), disabled controls that carry a reason (§8 accessibility contract) | One refusal cue, distinct from confirm, never louder |
| Drag pick-up / drop / cancel | `uiDrag.ts` — the 7 px threshold starts a drag; `pointercancel`, `lostpointercapture`, `blur` and `visibilitychange` cancel it (§3.4) | Cancel and drop are different cues so a lost drag is audible |
| Transfer completed, including a partial move | The single ordinary chest transfer command shared by shift-click, drag and the quantity buttons (§5.4) | A partial result uses the refusal-adjacent cue, matching the message "N Item moved to X. M stay in Y (X full)." |
| Quickbar slot select | `QUICKBAR_KEYS` 1–0 (`uiPreferences.ts:5`, §3.1) | Very short; this fires constantly in play |
| Toast / alert arrival | `panel.toast(text, 'good'\|'bad')` (`packages/game/src/panel.ts:122-123`) | Two cues only, matching the two existing toast kinds |
| Autosave written / autosave failed | §2.7.1, §2.7.3 | The failure cue is the refusal cue, not an urgent alert |

Objective changes get **no** cue of their own. The goal card rewrites itself continuously from `campaignNext()` (§7.3) — a priority list re-evaluated every frame, not a script of events — so a cue keyed to it would fire on ordinary churn.

### 12.8 Category 7 — Environmental ambience

| Bed | Source system (verified) | Note |
|---|---|---|
| Day / night ambience, cross-faded | `daylight(t)` (`packages/game/src/riverfrontLighting.ts:6`), a smooth 0–1 curve over the 1200-second day, and `campaignClock(st)` (`packages/sim/src/rules.ts:18-19`, `daylightSeconds = 900`) | Cross-fade on the same curve the lighting already uses, so sound and light change together |
| Outdoor city bed vs. interior | `cityVisible` / building `enterable` (`packages/sim/src/authoredCity.ts:11`) and the interior/outside colour split (`riverfrontLighting.ts:8`) | Two beds, switched on the same test the renderer uses |
| Riverside bed | `RIVERFRONT.riverY = 477` (`WORLD_AND_ASSETS.md` §2.3) | Positional, faded by distance from the river edge |
| Tram pass-by | The fixed four-stop tram (`packages/sim/src/fixedTram.ts`, `riverfrontRail.ts`) — geometry and motion are already sim-side | Positional, from the tram's actual position; never a timed loop |
| Street-light hum near lit lamps | `litAt(st,tx,ty)` (`packages/sim/src/flow.ts:1855`), the sim's own lit-tile query | Only where the **sim** says a tile is lit; the flashlight is a visibility beam only (D-UI-11, §8) and must stay silent |

Ambience is one or two beds at a time plus positional one-shots. No music bed, and no stinger that implies a state the HUD does not show.

### 12.9 What §12 does not decide

Asset sourcing, formats, licensing and where the files live are `WORLD_AND_ASSETS.md` §9.2. The mixer, `AudioSource` pooling, the presentation-side event routing and the assertion that `Relight.Sim` never references audio are Worker B's (`TECHNICAL_ARCHITECTURE.md`). Volume controls are §3.8.1.
