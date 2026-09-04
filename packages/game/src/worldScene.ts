/** World view (Phase 4 M1 + M2, prompt B M1): the city at tile level, on foot. Every tile is drawn from the ground
 *  layer in @relight/sim (ground.ts: block faces rasterised to tiles, streets on the ridges, the river), cached per
 *  32×32 chunk on `chunkKey`; the machines are drawn from `state.flow` (flow.ts). This scene holds only camera,
 *  tool and input state. On foot (the flow layer is up): the camera follows the engineer, the wheel zooms 0.5–3×
 *  about the engineer, M goes back to the map at the engineer's block; hand actions (mine, feed, place, remove)
 *  reach 8 tiles — outside that the cursor says "walk closer". Without the flow layer (?flow=0, the block-only bot
 *  comparisons) the old camera stays: drag or WASD pan, M at the block under the centre.
 *  D-B1-5 direct control: WASD moves the engineer (Shift sprints, Space dodges); there is no click-to-walk here —
 *  the map view's click on a Held or street tile is the only auto-walk. Left-click does what the hand holds: empty
 *  hand → hold on rubble to mine it, click a turret or Generator to feed it; a building in hand → place; the rifle
 *  in hand → hold to fire toward the cursor. Right-click with an empty hand removes with a full refund, with
 *  something in hand clears it. E interacts (the Depot chest, the workbench, a machine, the truck, survivors) and
 *  never mines, places or fires. The hotbar is 1–8 for the buildings and 9 the rifle; B is the build menu, R
 *  rotates, Q pipettes the machine under the cursor (or clears the hand), Esc closes anything. */
import Phaser from 'phaser';
import {
  SimState, idxOf, DARK, CONTESTED, HELD, INERT, VOID, isInterior,
  TILE_PX, RUBBLE_VARIANTS, T_STREET, T_GROUND, T_RUBBLE, T_INERT, T_RIVER, T_DEPOSIT, T_PATCH,
  ground, chunkTiles, chunkKey, CHUNK, describeGround, blockOfTile, inReach, depotRect, REACH, INV_STACKS, invStacks, currentPath,
  Kind, Dir, DX, DY, DIR_NAMES, Machine, MACHINE_SIZE, MACHINE_COST, SHOT, EXCAVATOR_PER_S,
  machineAt, rubbleAt, canPlace, place, remove, rotate, setHandMine, queueCraft, entryDir, describeMachine, outputTile, inputTile,
  handFeed, blockLights, workbenchTile, substationAt, isSubstationTile, poleGrid, flowSummary, TURRET_HOPPER, TURRET_FLASH_S,
  POLE_REACH, ROUNDS_PER_MAG, RIFLE_RANGE, DODGE_COST,
} from '@relight/sim';
import { Session, queue } from './session';
import { View } from './view';

/** GAME-ASSUMPTION: the constitution's 0.5–3× zoom, not §4's 1.0–0.2×; the doc is edited to this range (D-P4-1). */
export const ZOOM_MIN = 0.5, ZOOM_MAX = 3, ZOOM_STEP = 1.15;
const PAN_PX_PER_S = 900;
const CHUNK_PX = CHUNK * TILE_PX;   // 1024 px
const HALF = TILE_PX / 2;
/** Camera follow: the fraction of the gap to the engineer closed per second (the sim moves them 20× a second). */
const FOLLOW_PER_S = 14;

// tileset frames: street, ground, inert, river, then rubble (stone/copper/steel × 5 variants), then deposits (iron/coal × 5),
// then the HQ patches (steel, copper, coal)
const F_STREET = 0, F_GROUND = 1, F_INERT = 2, F_RIVER = 3, F_RUBBLE = 4, F_DEPOSIT = F_RUBBLE + 3 * RUBBLE_VARIANTS, F_PATCH = F_DEPOSIT + 2 * RUBBLE_VARIANTS, F_COUNT = F_PATCH + 3;
const RUBBLE_IDX: Record<string, number> = { stone: 0, copper: 1, steel: 2 };
const DEPOSIT_IDX: Record<string, number> = { iron: 0, coal: 1 };
const RUBBLE_COL = ['#b9bfc9', '#c0682b', '#5f83bd'];
const RUBBLE_DARK = ['#7d848f', '#7d4119', '#3a5580'];
const DEPOSIT_COL = ['#9a4c3a', '#1a1b20'];
/** GAME-ASSUMPTION: flat item colours (steel blue, copper orange, stone grey, coal black, magazine brass) until the art pass. */
const ITEM_COL: Record<string, number> = { steel: 0x7fa0d8, copper: 0xd9743a, stone: 0xc7ccd6, coal: 0x202126, magazine: 0xe6d45a };
const MACHINE_COL: Record<Kind, number> = { excavator: 0x4d5a6a, belt: 0x2a2d36, inserter: 0x5a4a2a, assembler: 0x5a4a6a, depot: 0x0b0e1a,
  turret: 0x3d4452, lamp: 0x6b6f7a, pole: 0x6e5a3a, generator: 0x5a2e2e };
const LIGHT_COL = 0xffe9a0;

export type Tool = 'hand' | 'rifle' | Exclude<Kind, 'depot'>;
/** D-B1-5: the hotbar. GAME-ASSUMPTION: 1 belt, 2 inserter, 3 Excavator, 4 Shot assembler, 5 turret, 6 lamp, 7 pole,
 *  8 Generator, 9 the rifle — the rifle is a hotbar item like any building, and while it is in hand you cannot mine
 *  or place until you clear it (Q, or pick something else). */
export const HOTBAR: readonly Tool[] = ['belt', 'inserter', 'excavator', 'assembler', 'turret', 'lamp', 'pole', 'generator', 'rifle'];
export const TOOL_KEY_LINE = '1 belt · 2 inserter · 3 Excavator · 4 Shot assembler · 5 turret · 6 lamp · 7 pole · 8 Generator · 9 rifle · B build menu · R rotate · Q pipette / clear hand · E interact · right-click removes (empty hand) or clears the hand';

export interface WorldHooks {
  onHoverText(text: string | null, px: number, py: number): void;
  onToast(msg: string, kind?: 'info' | 'bad' | 'good'): void;
  /** E on the Depot: open the chest (the pockets panel). */
  onChest(): void;
}

interface Chunk { key: string; frames: Uint8Array }

export class WorldScene extends Phaser.Scene {
  private blitter!: Phaser.GameObjects.Blitter;
  private bobs: Phaser.GameObjects.Bob[] = [];
  private gOver!: Phaser.GameObjects.Graphics;
  private gMach!: Phaser.GameObjects.Graphics;
  private gEng!: Phaser.GameObjects.Graphics;
  private hudText!: Phaser.GameObjects.Text;
  private depotText!: Phaser.GameObjects.Text;
  private labels: Phaser.GameObjects.Text[] = [];
  private chunks = new Map<number, Chunk>();
  private keys!: Record<'W' | 'A' | 'S' | 'D' | 'UP' | 'LEFT' | 'DOWN' | 'RIGHT' | 'SHIFT' | 'SPACE', Phaser.Input.Keyboard.Key>;
  private firing = false;      // the rifle in hand and the left button held
  private lastAim = '';
  private sprinting = false;
  private hoverTile: { tx: number; ty: number } | null = null;
  private dragging = false;
  private placing = false;
  private mining = false;
  private pending: [number, number] | null = null;
  private lastWalk = '0,0';
  private walkCloserToasted = false;
  private cursor = 'default';
  private snapped = false;
  /** Current tool and the direction the next machine faces. */
  tool: Tool = 'hand';
  dir: Dir = 1;
  /** Tiles drawn last frame; a dev/test number. */
  drawn = 0;
  /** Draw cost, ms: an EMA over frames and the worst since last read (the soak's `world.drawMs`). */
  drawMs = 0; drawWorstMs = 0;

  constructor(private session: Session, private view: View, private hooks: WorldHooks) { super('world'); }

  private get st(): SimState { return this.session.state; }
  /** On foot: the flow layer ticks the engineer on the tiles (walk.ts). Without it the old free camera stays. */
  private get onFoot(): boolean { return !!this.st.flow; }

  create(): void {
    this.cameras.main.setBackgroundColor(0x05070f);
    this.makeTileset();
    this.blitter = this.add.blitter(0, 0, 'ground');
    this.gMach = this.add.graphics().setDepth(1);
    this.gOver = this.add.graphics().setDepth(2);
    this.gEng = this.add.graphics().setDepth(3);
    this.depotText = this.add.text(0, 0, 'Depot', { fontSize: '20px', color: '#e8ecf4', fontStyle: 'bold' }).setDepth(3).setOrigin(0.5).setVisible(false);
    this.hudText = this.add.text(8, 8, '', { fontSize: '11px', color: '#c7cfe0', backgroundColor: '#0b0e1acc', padding: { x: 6, y: 4 } }).setScrollFactor(0).setDepth(10);
    const cam = this.cameras.main, G = ground(this.st);
    cam.setBounds(0, 0, G.tw * TILE_PX, G.th * TILE_PX);
    cam.setZoom(1);
    cam.setRoundPixels(true);
    this.keys = this.input.keyboard!.addKeys('W,A,S,D,UP,LEFT,DOWN,RIGHT,SHIFT,SPACE') as WorldScene['keys'];
    this.input.on('pointerdown', (p: Phaser.Input.Pointer) => this.onDown(p));
    this.input.on('pointerup', () => this.onUp());
    this.input.on('gameout', () => { this.onUp(); this.hoverTile = null; this.hooks.onHoverText(null, 0, 0); });
    this.input.on('pointermove', (p: Phaser.Input.Pointer) => this.onMove(p));
    this.input.on('wheel', (p: Phaser.Input.Pointer, _o: unknown, _dx: number, dy: number) => {
      const f = dy > 0 ? 1 / ZOOM_STEP : ZOOM_STEP;
      if (this.onFoot) this.zoomAt(...this.engineerScreen(), f); else this.zoomAt(p.x, p.y, f);
    });
    this.events.on(Phaser.Scenes.Events.WAKE, () => this.onWake());
    this.events.on(Phaser.Scenes.Events.SLEEP, () => { this.onUp(); this.sendWalk(0, 0); this.sendSprint(false); });
    this.onWake();
  }

  private onWake(): void {
    this.hoverTile = null;
    this.lastWalk = '0,0';
    if (this.onFoot) { this.snapped = false; return; }   // the camera lands on the engineer in the first update
    if (this.pending) { this.centreOn(this.pending[0], this.pending[1]); this.pending = null; }
  }

  /** Flat-colour tileset on a canvas texture: one 32 px frame per kind/type/variant. GAME-ASSUMPTION: code-drawn
   *  placeholder frames stand in for the §4 tilesets until the Phase 12 art pass; the frame table is the contract. */
  private makeTileset(): void {
    if (this.textures.exists('ground')) return;
    const tex = this.textures.createCanvas('ground', F_COUNT * TILE_PX, TILE_PX)!;
    const ctx = tex.getContext();
    const rect = (f: number, col: string, x = 0, y = 0, w = TILE_PX, h = TILE_PX) => { ctx.fillStyle = col; ctx.fillRect(f * TILE_PX + x, y, w, h); };
    rect(F_STREET, '#33363f'); rect(F_STREET, '#3b3e48', 1, 1, TILE_PX - 2, TILE_PX - 2);
    rect(F_GROUND, '#463d33'); rect(F_GROUND, '#4c4338', 2, 2, TILE_PX - 4, TILE_PX - 4);
    rect(F_INERT, '#262a34'); ctx.strokeStyle = '#1b1e26'; ctx.lineWidth = 2;
    ctx.beginPath(); ctx.moveTo(F_INERT * TILE_PX + 4, TILE_PX - 4); ctx.lineTo(F_INERT * TILE_PX + TILE_PX - 4, 4); ctx.stroke();
    rect(F_RIVER, '#1f3552'); rect(F_RIVER, '#264263', 0, 10, TILE_PX, 3); rect(F_RIVER, '#264263', 0, 22, TILE_PX, 3);
    // rubble: ground with chunks; more and bigger chunks per variant, in the district's colour
    const chunk = (f: number, k: number, col: string, dark: string, n: number, size: number) => {
      for (let i = 0; i < n; i++) {
        const h = ((k * 7919 + i * 104729 + f * 31) % 977) / 977, v = ((k * 6007 + i * 15485863 + f * 17) % 883) / 883;
        const s = size + ((i * 3 + k) % 3);
        const x = 2 + h * (TILE_PX - 4 - s), y = 2 + v * (TILE_PX - 4 - s);
        ctx.fillStyle = i % 3 === 2 ? dark : col; ctx.fillRect(Math.round(f * TILE_PX + x), Math.round(y), s, s);
      }
    };
    for (let t = 0; t < 3; t++) for (let v = 0; v < RUBBLE_VARIANTS; v++) {
      const f = F_RUBBLE + t * RUBBLE_VARIANTS + v;
      rect(f, '#463d33'); rect(f, '#4c4338', 2, 2, TILE_PX - 4, TILE_PX - 4);
      chunk(f, t * 10 + v, RUBBLE_COL[t], RUBBLE_DARK[t], 3 + v * 3, 3 + v);
    }
    for (let t = 0; t < 2; t++) for (let v = 0; v < RUBBLE_VARIANTS; v++) {
      const f = F_DEPOSIT + t * RUBBLE_VARIANTS + v;
      rect(f, '#3d3a36'); chunk(f, 100 + t * 10 + v, DEPOSIT_COL[t], DEPOSIT_COL[t], 4 + v * 2, 2 + (v >> 1));
    }
    // HQ patches (§11): dense heaps of steel and copper rubble, and the coal seam
    rect(F_PATCH, '#3d3a36'); chunk(F_PATCH, 200, RUBBLE_COL[2], RUBBLE_DARK[2], 14, 4);
    rect(F_PATCH + 1, '#3d3a36'); chunk(F_PATCH + 1, 210, RUBBLE_COL[1], RUBBLE_DARK[1], 14, 4);
    rect(F_PATCH + 2, '#3d3a36'); chunk(F_PATCH + 2, 220, DEPOSIT_COL[1], '#2c2d33', 12, 4);
    tex.refresh();
    for (let f = 0; f < F_COUNT; f++) tex.add(f, 0, f * TILE_PX, 0, TILE_PX, TILE_PX);
  }

  /** The frames of one 32×32 chunk, rebuilt when its `chunkKey` (the state of every block with tiles in it) changes. */
  private chunk(cx: number, cy: number): Uint8Array {
    const st = this.st, G = ground(st), id = cy * G.cw + cx, key = chunkKey(st, cx, cy);
    const hit = this.chunks.get(id);
    if (hit && hit.key === key) return hit.frames;
    const c = chunkTiles(st, cx, cy), frames = new Uint8Array(CHUNK * CHUNK);
    const x0 = cx * CHUNK, y0 = cy * CHUNK;
    for (let i = 0; i < frames.length; i++) {
      const k = c.kind[i];
      let f = F_GROUND;
      if (k === T_STREET) f = F_STREET;
      else if (k === T_INERT) f = F_INERT;
      else if (k === T_RIVER) f = F_RIVER;
      else if (k === T_PATCH) f = F_PATCH + Math.max(0, c.patch[i] - 1);
      else if (k === T_RUBBLE || k === T_DEPOSIT) {
        const lx = i % CHUNK, ly = (i - lx) / CHUNK, o = G.owner[(y0 + ly) * G.tw + x0 + lx], bg = o >= 0 ? G.blocks[o] : null;
        f = k === T_RUBBLE ? F_RUBBLE + RUBBLE_IDX[bg?.rubble ?? 'stone'] * RUBBLE_VARIANTS + Math.max(0, c.variant[i] - 1)
          : F_DEPOSIT + DEPOSIT_IDX[bg?.deposit ?? 'iron'] * RUBBLE_VARIANTS + Math.max(0, c.variant[i] - 1);
      } else if (k === T_GROUND) f = F_GROUND;
      frames[i] = f;
    }
    this.chunks.set(id, { key, frames });
    return frames;
  }

  // ------------------------------------------------------------------ camera

  /** World point under a screen point, for the camera's current scroll and zoom (no rotation). */
  private worldAt(sx: number, sy: number, zoom = this.cameras.main.zoom): { x: number; y: number } {
    const cam = this.cameras.main;
    return { x: cam.scrollX + cam.width / 2 + (sx - cam.width / 2) / zoom, y: cam.scrollY + cam.height / 2 + (sy - cam.height / 2) / zoom };
  }

  /** Screen point of a tile-space point (dev hook: the scripted checks aim the mouse with it). */
  screenOf(x: number, y: number): [number, number] {
    const cam = this.cameras.main;
    return [cam.width / 2 + (x * TILE_PX - cam.scrollX - cam.width / 2) * cam.zoom, cam.height / 2 + (y * TILE_PX - cam.scrollY - cam.height / 2) * cam.zoom];
  }

  /** The engineer's screen point (the zoom pivot on foot). */
  private engineerScreen(): [number, number] {
    const cam = this.cameras.main, e = this.st.engineer;
    return [cam.width / 2 + (e.x * TILE_PX - cam.scrollX - cam.width / 2) * cam.zoom, cam.height / 2 + (e.y * TILE_PX - cam.scrollY - cam.height / 2) * cam.zoom];
  }

  zoomAt(sx: number, sy: number, factor: number): void {
    const cam = this.cameras.main;
    const z1 = Math.max(ZOOM_MIN, Math.min(ZOOM_MAX, cam.zoom * factor));
    if (z1 === cam.zoom) return;
    const w = this.worldAt(sx, sy);
    cam.setZoom(z1);
    cam.setScroll(w.x - cam.width / 2 - (sx - cam.width / 2) / z1, w.y - cam.height / 2 - (sy - cam.height / 2) / z1);
  }

  setZoom(z: number): void {
    const [sx, sy] = this.onFoot ? this.engineerScreen() : [this.cameras.main.width / 2, this.cameras.main.height / 2];
    this.zoomAt(sx, sy, z / this.cameras.main.zoom);
  }
  get zoom(): number { return this.cameras.main.zoom; }

  /** Centre the camera on a block (its pole tile). Safe before create(): the request is kept for the first wake.
   *  On foot the camera follows the engineer, so this only holds until the next frame. */
  centreOn(x: number, y: number): void {
    if (!this.cameras?.main) { this.pending = [x, y]; return; }
    const st = this.st, i = idxOf(st, x, y), G = ground(st);
    const p = i >= 0 ? G.blocks[i].pole : [G.tw / 2, G.th / 2];
    this.cameras.main.centerOn((p[0] + 0.5) * TILE_PX, (p[1] + 0.5) * TILE_PX);
  }

  /** The block the map view gets on M: the engineer's on foot, else the one under the camera's centre. */
  focusBlock(): [number, number] {
    const st = this.st;
    let i = -1;
    if (this.onFoot) { const e = st.engineer; i = e.block >= 0 ? e.block : blockOfTile(st, Math.floor(e.x), Math.floor(e.y)); }
    else { const cam = this.cameras.main, m = this.worldAt(cam.width / 2, cam.height / 2); i = blockOfTile(st, Math.floor(m.x / TILE_PX), Math.floor(m.y / TILE_PX)); }
    const b = st.blocks[i] ?? st.blocks[idxOf(st, st.start[0], st.start[1])];
    return [b.x, b.y];
  }

  // ------------------------------------------------------------------ tools

  /** A key from the page's keydown handler (only while this view is up). Returns true when the scene took it. */
  key(k: string): boolean {
    const st = this.st;
    const lower = k.toLowerCase();
    if (!st.flow) return false;
    const slot = '123456789'.indexOf(k);
    if (slot >= 0) { this.setTool(HOTBAR[slot]); return true; }
    if (lower === 'escape') { this.setTool('hand'); return true; }
    if (lower === 'q') {
      // pipette: the machine under the cursor into the hand; nothing there (or the Depot) clears the hand
      const h = this.hoverTile, m = h ? machineAt(st, h.tx, h.ty) : undefined;
      this.setTool(m && m.kind !== 'depot' ? m.kind : 'hand');
      if (m && m.kind !== 'depot') this.dir = m.dir;
      return true;
    }
    if (lower === 'r') {
      const h = this.hoverTile, m = h && this.tool === 'hand' ? machineAt(st, h.tx, h.ty) : undefined;
      if (m && m.kind !== 'depot') { if (this.reachable(m.x, m.y, m.size, true)) rotate(st, m.x, m.y); } else this.dir = ((this.dir + 1) % 4) as Dir;
      return true;
    }
    if (lower === 'e') { this.interact(); return true; }
    return false;
  }

  /** Put a tool (a building, the rifle, or nothing) in the hand. The build menu's picks land here too. */
  setTool(t: Tool): void {
    if (t === this.tool) return;
    if (this.firing) { this.firing = false; this.sendAim(null); }
    this.tool = t;
  }

  /** E interacts (D-B1-5): the Depot opens the chest, the workbench crafts a magazine, a machine reports itself, the
   *  truck is entered or left where it was found, a survivor group on the block answers. E never mines, places or
   *  fires. Everything but the truck needs the thing within reach. */
  private interact(): void {
    const st = this.st, h = this.hoverTile;
    if (!st.flow || !this.onFoot) return;
    const e = st.engineer;
    const wb = workbenchTile(st);
    if (h && h.tx >= wb[0] && h.tx < wb[0] + 2 && h.ty >= wb[1] && h.ty < wb[1] + 2) {
      if (!this.reachable(wb[0], wb[1], 2, true)) return;
      queueCraft(st, 1);
      this.hooks.onToast(`Workbench: crafting a magazine by hand (${SHOT.inputs.steel} steel + ${SHOT.inputs.copper} Cu, ${SHOT.seconds} s) — ${st.flow.hand.crafts} queued`);
      return;
    }
    const m = h ? machineAt(st, h.tx, h.ty) : undefined;
    if (m) {
      if (!this.reachable(m.x, m.y, m.size, true)) return;
      if (m.kind === 'depot') { this.hooks.onChest(); return; }
      this.hooks.onToast(describeMachine(st, m));
      return;
    }
    if (e.truckFound) { queue(this.session, { type: 'enterTruck' }); this.hooks.onToast(e.truck ? 'Out of the truck' : 'In the truck — 3× walk speed, 200 stacks'); return; }
    const b = e.block >= 0 ? st.blocks[e.block] : null;
    const sv = b ? st.survivors.find(v => v.x === b.x && v.y === b.y) : undefined;
    if (sv) { this.hooks.onToast(`${sv.name} (${sv.tag}): "${b!.state === HELD ? "We're in." : 'Light the street and we talk.'}"`); return; }
    this.hooks.onToast('Nothing here to interact with — E opens the Depot chest, the workbench, a machine, the truck, a survivor group');
  }

  /** Top-left tile of the tool's footprint when the pointer is on (tx, ty): 3×3 machines centre on the pointer,
   *  2×2 ones (turret, Generator) take the pointer's tile as their top-left. */
  private footprint(kind: Kind, tx: number, ty: number): [number, number] {
    const off = Math.floor((MACHINE_SIZE[kind] - 1) / 2);
    return [tx - off, ty - off];
  }

  /** D5 reach: hand actions land within 8 tiles of the engineer. Outside, the first refusal is a toast, every one
   *  the "walk closer" cursor and the ghost's reason. Without the flow layer there is no engineer on the tiles. */
  private reachable(tx: number, ty: number, size: number, loud: boolean): boolean {
    if (!this.onFoot || inReach(this.st, tx, ty, size)) return true;
    if (loud && !this.walkCloserToasted) { this.walkCloserToasted = true; this.hooks.onToast(`Walk closer — the engineer reaches ${REACH} tiles (WASD or click the ground)`, 'bad'); }
    return false;
  }

  private tryPlace(tx: number, ty: number, loud: boolean): void {
    const st = this.st;
    if (this.tool === 'hand' || !st.flow) return;
    const kind = this.tool as Kind;
    const [ox, oy] = this.footprint(kind, tx, ty);
    if (!this.reachable(ox, oy, MACHINE_SIZE[kind], loud)) return;
    const c = canPlace(st, kind, ox, oy);
    if (!c.ok) { if (loud) this.hooks.onToast(`No ${kind} here: ${c.reason}`, 'bad'); return; }
    place(st, kind, ox, oy, this.dir);
  }

  private sendWalk(dx: number, dy: number): void {
    const k = `${dx},${dy}`;
    if (k === this.lastWalk || !this.onFoot) return;
    this.lastWalk = k;
    queue(this.session, { type: 'walk', dx, dy });
  }

  private sendSprint(on: boolean): void {
    if (on === this.sprinting || !this.onFoot) return;
    this.sprinting = on;
    queue(this.session, { type: 'sprint', on });
  }

  /** The rifle's aim: the cursor's world position in tiles (fractional), or null on release. */
  private sendAim(at: [number, number] | null): void {
    const k = at ? `${at[0].toFixed(2)},${at[1].toFixed(2)}` : '';
    if (k === this.lastAim) return;
    this.lastAim = k;
    queue(this.session, { type: 'aim', at });
  }

  private aimAt(p: Phaser.Input.Pointer): [number, number] {
    const w = this.worldAt(p.x, p.y);
    return [w.x / TILE_PX, w.y / TILE_PX];
  }

  private onDown(p: Phaser.Input.Pointer): void {
    const st = this.st;
    const w = this.worldAt(p.x, p.y);
    const tx = Math.floor(w.x / TILE_PX), ty = Math.floor(w.y / TILE_PX);
    if (p.rightButtonDown()) {
      if (!st.flow) return;
      if (this.tool !== 'hand') { this.setTool('hand'); return; }   // something in hand: right-click clears it
      const m = machineAt(st, tx, ty);
      if (!m) return;
      if (m.kind === 'depot') { this.hooks.onToast('The Depot stays', 'bad'); return; }
      if (!this.reachable(m.x, m.y, m.size, true)) return;
      const cost = MACHINE_COST[m.kind];
      const held = m.kind === 'turret' ? `, ${m.inv.rounds ?? 0} rounds to the line buffer` : m.kind === 'generator' ? `, ${Math.floor(m.inv.coal ?? 0)} coal to the Depot` : '';
      remove(st, tx, ty);
      this.hooks.onToast(`${m.kind} removed — ${cost.steel} steel${cost.copper ? ` + ${cost.copper} Cu` : ''} back in the Depot${held}`);
      return;
    }
    if (!p.leftButtonDown()) return;
    // D-B1-5: left-click does what the hand holds
    if (this.tool === 'rifle' && st.flow) {
      if (this.onFoot) { this.firing = true; this.sendAim(this.aimAt(p)); }
      return;
    }
    if (this.tool !== 'hand' && st.flow) { this.placing = true; this.tryPlace(tx, ty, true); return; }
    if (this.tool === 'hand' && st.flow) {
      // §11: the tester hand-feeds turrets and the Generator; a click on one moves what the Depot has
      const m = machineAt(st, tx, ty);
      if (m && (m.kind === 'turret' || m.kind === 'generator')) {
        if (!this.reachable(m.x, m.y, m.size, true)) return;
        const fed = handFeed(st, tx, ty);
        if (fed) {
          if (fed.moved > 0) this.hooks.onToast(fed.kind === 'turret' ? `Hand-fed ${fed.moved} magazines into the turret` : `Hand-fed ${fed.moved} coal into the Generator`, 'good');
          else this.hooks.onToast(fed.reason, 'bad');
          return;
        }
      }
      if (rubbleAt(st, tx, ty)) {
        if (!this.reachable(tx, ty, 1, true)) return;
        this.mining = true; setHandMine(st, [tx, ty]); return;
      }
      if (this.onFoot) return;   // no click-to-walk in the world view (D-B1-5): WASD, or the map view's walk-here
    }
    this.dragging = true;
  }

  private onUp(): void {
    this.dragging = false; this.placing = false;
    if (this.mining) { this.mining = false; if (this.st.flow) setHandMine(this.st, null); }
    if (this.firing) { this.firing = false; this.sendAim(null); }
  }

  private onMove(p: Phaser.Input.Pointer): void {
    const cam = this.cameras.main;
    if (this.dragging && p.isDown && !this.onFoot) {
      cam.scrollX -= (p.position.x - p.prevPosition.x) / cam.zoom;
      cam.scrollY -= (p.position.y - p.prevPosition.y) / cam.zoom;
    }
    const w = this.worldAt(p.x, p.y);
    const tx = Math.floor(w.x / TILE_PX), ty = Math.floor(w.y / TILE_PX);
    if (this.hoverTile && this.hoverTile.tx === tx && this.hoverTile.ty === ty) return;
    this.hoverTile = { tx, ty };
    const st = this.st, G = ground(st);
    if (this.placing && p.isDown) this.tryPlace(tx, ty, false);
    if (this.mining && p.isDown && st.flow) setHandMine(st, [tx, ty]);
    if (this.firing && p.isDown) this.sendAim(this.aimAt(p));
    if (tx < 0 || ty < 0 || tx >= G.tw || ty >= G.th) { this.hooks.onHoverText(null, 0, 0); return; }
    const rect = this.game.canvas.getBoundingClientRect();
    const m = st.flow ? machineAt(st, tx, ty) : undefined;
    const bi = blockOfTile(st, tx, ty);
    const lines = [describeGround(st, tx, ty)];
    if (bi >= 0) lines.push(this.blockLine(bi));
    if (m) lines.unshift(describeMachine(st, m));
    else if (st.flow && isSubstationTile(st, tx, ty)) {
      const sub = bi >= 0 ? substationAt(st, st.blocks[bi].x, st.blocks[bi].y) : null;
      if (sub) lines.unshift(`Substation · ${sub.on ? `on · draws ${sub.kw} kW · streetlights lit` : sub.kw === 0 ? 'Dark · string poles to it to claim' : 'off · no power (brownout or the block is unfed)'}`);
    }
    // the "walk closer" cursor: something to do here, out of reach
    const actionable = this.onFoot && (this.tool !== 'hand' || !!m || !!rubbleAt(st, tx, ty));
    const far = actionable && !inReach(st, tx, ty, 1);
    if (far) lines.unshift(`Walk closer (reach ${REACH} tiles)`);
    this.hooks.onHoverText(lines.join('\n'), rect.left + p.x, rect.top + p.y);
    const cur = far ? 'not-allowed' : 'default';
    if (cur !== this.cursor) { this.cursor = cur; this.input.setDefaultCursor(cur); }
  }

  private blockLine(i: number): string {
    const st = this.st, b = st.blocks[i];
    const name = b.name === 'civ' ? 'civic' : b.name === 'res' ? 'residential' : b.name === 'ind' ? 'industrial' : 'outskirts';
    const river = st.lattice ? b.y === st.h - 1 : false;
    const state = river ? 'river' : b.state === DARK ? `Dark · rot ${Math.round(b.d * 100)} %` : b.state === CONTESTED ? 'Contested' : b.state === HELD ? (isInterior(st, i) ? 'Held · interior' : 'Held · front') : b.state === INERT || b.state === VOID ? 'inert' : '?';
    const hq = b.x === st.start[0] && b.y === st.start[1] ? ' · HQ' : '';
    return `block (${b.x},${b.y}) · ${name} · ${state}${hq}`;
  }

  // ------------------------------------------------------------------ frame

  update(_time: number, delta: number): void {
    const cam = this.cameras.main, dt = Math.min(0.1, delta / 1000);
    const right = this.keys.D.isDown || this.keys.RIGHT.isDown, left = this.keys.A.isDown || this.keys.LEFT.isDown;
    const down = this.keys.S.isDown || this.keys.DOWN.isDown, up = this.keys.W.isDown || this.keys.UP.isDown;
    if (this.onFoot) {
      // WASD walks the engineer (a sim command; walk.ts moves them with collision); the camera follows.
      // D-B1-5: Shift sprints, Space dodges; the rifle's aim follows the cursor while the button is held.
      this.sendWalk((right ? 1 : 0) - (left ? 1 : 0), (down ? 1 : 0) - (up ? 1 : 0));
      this.sendSprint(this.keys.SHIFT.isDown);
      if (Phaser.Input.Keyboard.JustDown(this.keys.SPACE)) queue(this.session, { type: 'dodge' });
      if (this.firing) this.sendAim(this.aimAt(this.input.activePointer));
      const e = this.st.engineer, gx = e.x * TILE_PX - cam.width / 2, gy = e.y * TILE_PX - cam.height / 2;
      if (!this.snapped) { cam.setScroll(gx, gy); this.snapped = true; }
      else { const k = Math.min(1, FOLLOW_PER_S * dt); cam.setScroll(cam.scrollX + (gx - cam.scrollX) * k, cam.scrollY + (gy - cam.scrollY) * k); }
    } else {
      const pan = PAN_PX_PER_S * dt / cam.zoom;
      if (left) cam.scrollX -= pan;
      if (right) cam.scrollX += pan;
      if (up) cam.scrollY -= pan;
      if (down) cam.scrollY += pan;
    }
    const t0 = performance.now();
    this.draw();
    const ms = performance.now() - t0;
    this.drawMs += (ms - this.drawMs) * 0.05; if (ms > this.drawWorstMs) this.drawWorstMs = ms;
  }

  private draw(): void {
    const st = this.st, cam = this.cameras.main, G = ground(st);
    const tl = this.worldAt(0, 0), br = this.worldAt(cam.width, cam.height);
    const tx0 = Math.max(0, Math.floor(tl.x / TILE_PX)), ty0 = Math.max(0, Math.floor(tl.y / TILE_PX));
    const tx1 = Math.min(G.tw - 1, Math.ceil(br.x / TILE_PX)), ty1 = Math.min(G.th - 1, Math.ceil(br.y / TILE_PX));
    let n = 0;
    const cx0 = tx0 >> 5, cy0 = ty0 >> 5, cx1 = tx1 >> 5, cy1 = ty1 >> 5;
    for (let cy = cy0; cy <= cy1; cy++) for (let cx = cx0; cx <= cx1; cx++) {
      const frames = this.chunk(cx, cy);
      const ax = Math.max(tx0, cx * CHUNK), ay = Math.max(ty0, cy * CHUNK), bx = Math.min(tx1, cx * CHUNK + CHUNK - 1), by = Math.min(ty1, cy * CHUNK + CHUNK - 1);
      for (let ty = ay; ty <= by; ty++) for (let tx = ax; tx <= bx; tx++) {
        const frame = frames[(ty - cy * CHUNK) * CHUNK + (tx - cx * CHUNK)];
        let bob = this.bobs[n];
        if (!bob) { bob = this.blitter.create(tx * TILE_PX, ty * TILE_PX, frame); this.bobs.push(bob); }
        else { bob.setPosition(tx * TILE_PX, ty * TILE_PX); bob.setFrame(frame); bob.setVisible(true); }
        n++;
      }
    }
    for (let k = n; k < this.bobs.length; k++) if (this.bobs[k].visible) this.bobs[k].setVisible(false);
    this.drawn = n;

    // the blocks with tiles in view (their bounding boxes meet the window)
    const vis: number[] = [];
    for (let i = 0; i < G.blocks.length; i++) { const b = G.blocks[i]; if (b.tiles.length && b.x1 >= tx0 && b.x0 <= tx1 && b.y1 >= ty0 && b.y0 <= ty1) vis.push(i); }

    this.drawMachines(tx0, ty0, tx1, ty1, vis);

    // GAME-ASSUMPTION: a flat block-state overlay on each lot (Dark navy, Contested amber, Held outline) stands in for
    // rot presence (M4) and the light texture (M5); it is the block map's word on the lot, drawn, not simulated.
    // M1: lots are faces, so the tint runs along each row of the lot's tiles and the Held rim follows its boundary.
    const g = this.gOver;
    g.clear();
    const flicker = Math.sin(performance.now() / 45) > 0 ? 0.3 : 0.15;
    const cls = new Uint8Array(st.blocks.length);   // 1 Dark, 2 Contested, 3 Held front, 4 Held interior
    for (const i of vis) { const s = st.blocks[i].state; cls[i] = s === DARK ? 1 : s === CONTESTED ? 2 : s === HELD ? (isInterior(st, i) ? 4 : 3) : 0; }
    const own = G.owner, tw = G.tw;
    for (let ty = ty0; ty <= ty1; ty++) {
      let run = -1, runX = 0;
      for (let tx = tx0; tx <= tx1 + 1; tx++) {
        const o = tx <= tx1 ? own[ty * tw + tx] : -1, c = o >= 0 ? cls[o] : 0, key = c === 1 || c === 2 ? o : -1;
        if (key !== run) {
          if (run >= 0) { const rc = cls[run]; if (rc === 1) g.fillStyle(0x0b1030, 0.45); else g.fillStyle(0xd99a2b, flicker); g.fillRect(runX * TILE_PX, ty * TILE_PX, (tx - runX) * TILE_PX, TILE_PX); }
          run = key; runX = tx;
        }
      }
    }
    // Held rims: the lot's boundary edges, white for interior, amber for the front
    for (let ty = ty0; ty <= ty1; ty++) for (let tx = tx0; tx <= tx1; tx++) {
      const o = own[ty * tw + tx];
      if (o < 0 || cls[o] < 3) continue;
      const px = tx * TILE_PX, py = ty * TILE_PX;
      g.lineStyle(4 / cam.zoom, cls[o] === 4 ? 0xffffff : 0xe8a93a, 0.6);
      if (ty === 0 || own[(ty - 1) * tw + tx] !== o) g.lineBetween(px, py, px + TILE_PX, py);
      if (ty === G.th - 1 || own[(ty + 1) * tw + tx] !== o) g.lineBetween(px, py + TILE_PX, px + TILE_PX, py + TILE_PX);
      if (tx === 0 || own[ty * tw + tx - 1] !== o) g.lineBetween(px, py, px, py + TILE_PX);
      if (tx === tw - 1 || own[ty * tw + tx + 1] !== o) g.lineBetween(px + TILE_PX, py, px + TILE_PX, py + TILE_PX);
    }
    // one label per block whose pole tile is in view, kept small on screen
    let li = 0;
    for (const i of vis) {
      const p = G.blocks[i].pole;
      if (p[0] < tx0 || p[0] > tx1 || p[1] < ty0 || p[1] > ty1) continue;
      let t = this.labels[li];
      if (!t) { t = this.add.text(0, 0, '', { fontSize: '12px', color: '#c7cfe0', backgroundColor: '#0b0e1aaa', padding: { x: 4, y: 2 } }).setDepth(5); this.labels.push(t); }
      t.setText(this.blockLine(i)).setPosition(p[0] * TILE_PX + 6, p[1] * TILE_PX + 6).setScale(1 / cam.zoom).setVisible(true);
      li++;
    }
    for (let k = li; k < this.labels.length; k++) this.labels[k].setVisible(false);
    if (!st.flow) {
      // GAME-ASSUMPTION: without the flow layer (?flow=0) the HQ is a 6×6-tile slab where the Depot would be
      const d = depotRect(st);
      g.fillStyle(0x0b0e1a, 0.85); g.fillRect(d.x * TILE_PX, d.y * TILE_PX, d.size * TILE_PX, d.size * TILE_PX);
      g.lineStyle(2 / cam.zoom, 0xffffff, 0.8); g.strokeRect(d.x * TILE_PX, d.y * TILE_PX, d.size * TILE_PX, d.size * TILE_PX);
    }
    this.drawGhost(g);
    this.drawEngineer();

    // HUD: scrollFactor 0 still zooms about the camera centre, so pin it to the top-left at screen scale
    const f = this.focusBlock(), fi = idxOf(st, f[0], f[1]);
    this.hudText.setScale(1 / cam.zoom).setPosition(cam.width / 2 + (8 - cam.width / 2) / cam.zoom, cam.height / 2 + (8 - cam.height / 2) / cam.zoom);
    const e = st.engineer;
    const pockets = this.onFoot ? ` · pockets ${invStacks(e.inv)}/${INV_STACKS} stacks${e.inv.kit ? ` · ${e.inv.kit} kit${e.inv.kit === 1 ? '' : 's'}` : ''} · HP ${Math.round(e.hp)}` : '';
    const rounds = Math.floor((e.inv.magazine ?? 0) * ROUNDS_PER_MAG);
    const inHand = this.tool === 'hand' ? 'empty hand (hold left-click on rubble to mine it; click a turret or Generator to feed it; E interacts)'
      : this.tool === 'rifle' ? `rifle (hold left-click to fire toward the cursor, ${RIFLE_RANGE} tiles; Q puts it away) · ${rounds} round${rounds === 1 ? '' : 's'} in the pockets${rounds ? '' : ' — take magazines from the chest (E on the Depot)'}`
      : `${this.tool} → ${DIR_NAMES[this.dir]}`;
    const toolLine = st.flow ? `\nIn hand: ${inHand}${this.ghostReason ? ` · ${this.ghostReason}` : ''}\n${TOOL_KEY_LINE}${this.powerLine()}` : '';
    const moveLine = this.onFoot ? 'M map view (click a Held or street tile there to walk) · WASD move · Shift sprint · Space dodge · Tab/I pockets · wheel zoom' : 'M map view · drag / WASD pan · wheel zoom';
    this.hudText.setText(`World view · ${fi >= 0 ? this.blockLine(fi) : ''} · zoom ${cam.zoom.toFixed(2)}× · ${n} tiles${pockets}\n${moveLine} (${ZOOM_MIN}–${ZOOM_MAX}×) · P pause · - / = speed${toolLine}`);
  }

  /** D5 on foot: the engineer (a disc, red while down), their path, and the reach ring — faint, brighter while the
   *  cursor is inside it; D-B1-5: the stamina bar under the HP bar (a tick at the one-dodge floor) and, with the
   *  rifle in hand, the aim line to the cursor out to the rifle's range. GAME-ASSUMPTION: a code-drawn disc stands
   *  in for the engineer sprite until the art pass. */
  private drawEngineer(): void {
    const g = this.gEng, st = this.st;
    g.clear();
    if (!this.onFoot) return;
    const e = st.engineer, zoom = this.cameras.main.zoom, ex = e.x * TILE_PX, ey = e.y * TILE_PX;
    const path = currentPath(st);
    if (path) {
      g.lineStyle(2 / zoom, 0xffffff, 0.25);
      let lx = ex, ly = ey;
      const G = ground(st);
      for (let k = path.at; k < path.path.length; k++) { const t = path.path[k], tx = t % G.tw, ty = (t - tx) / G.tw, px = (tx + 0.5) * TILE_PX, py = (ty + 0.5) * TILE_PX; g.lineBetween(lx, ly, px, py); lx = px; ly = py; }
    }
    const h = this.hoverTile, inside = !!h && inReach(st, h.tx, h.ty, 1);
    g.lineStyle(2 / zoom, 0xffffff, inside ? 0.35 : 0.1); g.strokeCircle(ex, ey, REACH * TILE_PX);
    if (this.tool === 'rifle' && e.down < 0) {
      const p = this.input.activePointer, w = this.worldAt(p.x, p.y);
      const dx = w.x - ex, dy = w.y - ey, L = Math.hypot(dx, dy), R = RIFLE_RANGE * TILE_PX;
      if (L > 1e-6) {
        const k = Math.min(1, R / L);
        g.lineStyle(2 / zoom, 0xffd070, this.firing ? 0.8 : 0.3); g.lineBetween(ex, ey, ex + dx * k, ey + dy * k);
        g.lineStyle(1 / zoom, 0xffd070, 0.15); g.strokeCircle(ex, ey, R);
      }
    }
    if (e.down >= 0) { g.fillStyle(0xe05a5a, 0.9); g.fillCircle(ex, ey, 11); return; }
    g.fillStyle(0x0b0e1a, 0.6); g.fillCircle(ex + 2, ey + 3, 11);
    g.fillStyle(e.dash > 0 ? 0xbfe8ff : 0xf5f0e0, 1); g.fillCircle(ex, ey, 10);
    g.fillStyle(0xe8a93a, 1); g.fillCircle(ex, ey - 2, 5);
    if (e.hp < 100) { g.fillStyle(0x1a1d26, 1); g.fillRect(ex - 12, ey + 13, 24, 4); g.fillStyle(0x6fe08a, 1); g.fillRect(ex - 12, ey + 13, 24 * e.hp / 100, 4); }
    if (e.stamina < 1 || e.sprint) {
      g.fillStyle(0x1a1d26, 1); g.fillRect(ex - 12, ey + 18, 24, 3);
      g.fillStyle(e.stamina <= DODGE_COST + 1e-6 ? 0xe0a050 : 0xf0d060, 1); g.fillRect(ex - 12, ey + 18, 24 * e.stamina, 3);
      g.fillStyle(0xffffff, 0.8); g.fillRect(ex - 12 + 24 * DODGE_COST - 0.5, ey + 17, 1, 5);   // one dodge's worth
    }
  }

  private ghostReason = '';

  /** §14 power as one number: what the grid carries against what the Generators can give, the shed count and the
   *  coal left. Drawn every frame from `flow.power` (set by the sim's power section) and the flow summary. */
  private powerLine(): string {
    const st = this.st, f = st.flow;
    if (!f || !st.config.power) return '';
    const p = f.power, fs = flowSummary(st);
    const mw = (kw: number) => (kw / 1000).toFixed(2);
    const state = p.supply <= 0 ? ' · NO POWER: the Generators are out of coal' : p.demand > p.supply + 1e-9 ? ` · BROWNOUT: ${fs.shedMachines} machine${fs.shedMachines === 1 ? '' : 's'} shed` : '';
    return `\nPower ${mw(p.load)} / ${mw(p.supply)} MW (demand ${mw(p.demand)})${state} · Generators ${fs.generatorsBurning}/${fs.generators} burning, ${Math.floor(fs.genCoal)} coal · lamps ${fs.lampsLit}/${fs.lamps} lit · brownout ${Math.round(fs.brownoutS)} s`;
  }

  /** The tool's footprint under the pointer, green when it can go there, red with the reason otherwise. */
  private drawGhost(g: Phaser.GameObjects.Graphics): void {
    const st = this.st, h = this.hoverTile;
    this.ghostReason = '';
    if (!st.flow || !h || this.tool === 'hand' || this.tool === 'rifle') return;   // the rifle draws its aim line, not a footprint
    const kind = this.tool as Kind, size = MACHINE_SIZE[kind];
    const [ox, oy] = this.footprint(kind, h.tx, h.ty);
    const far = this.onFoot && !inReach(st, ox, oy, size);
    const c = canPlace(st, kind, ox, oy);
    this.ghostReason = far ? 'walk closer' : c.ok ? `${c.cost.steel} steel${c.cost.copper ? ` + ${c.cost.copper} Cu` : ''}` : c.reason;
    const col = c.ok && !far ? 0x6fe08a : 0xe05a5a;
    g.fillStyle(col, 0.25); g.fillRect(ox * TILE_PX, oy * TILE_PX, size * TILE_PX, size * TILE_PX);
    g.lineStyle(2 / this.cameras.main.zoom, col, 0.9); g.strokeRect(ox * TILE_PX, oy * TILE_PX, size * TILE_PX, size * TILE_PX);
    if (kind === 'pole') { g.lineStyle(1 / this.cameras.main.zoom, col, 0.6); g.strokeCircle((ox + 0.5) * TILE_PX, (oy + 0.5) * TILE_PX, POLE_REACH * TILE_PX); }
    else if (kind === 'lamp') { g.lineStyle(1 / this.cameras.main.zoom, LIGHT_COL, 0.6); g.strokeCircle((ox + 0.5) * TILE_PX, (oy + 0.5) * TILE_PX, 4 * TILE_PX); }
    else if (kind !== 'turret') this.arrow(g, (ox + size / 2) * TILE_PX, (oy + size / 2) * TILE_PX, this.dir, size * HALF - 4, col, 0.9);
  }

  private arrow(g: Phaser.GameObjects.Graphics, cx: number, cy: number, dir: Dir, len: number, col: number, alpha = 1): void {
    const dx = DX[dir], dy = DY[dir], px = -dy, py = dx;   // perpendicular
    const tipx = cx + dx * len, tipy = cy + dy * len, base = len * 0.45, w = 6;
    g.fillStyle(col, alpha);
    g.fillTriangle(tipx, tipy, cx + dx * base + px * w, cy + dy * base + py * w, cx + dx * base - px * w, cy + dy * base - py * w);
  }

  /** Machines from `state.flow`, drawn every frame for the tiles in view: belts with their items, inserters with
   *  their arm, Excavators and assemblers with progress, the Depot. GAME-ASSUMPTION: flat code-drawn shapes stand
   *  in for machine sprites until the art pass; sizes and facings are the flow layer's. */
  private drawMachines(tx0: number, ty0: number, tx1: number, ty1: number, vis: readonly number[]): void {
    const st = this.st, g = this.gMach, f = st.flow;
    g.clear();
    this.depotText.setVisible(false);
    if (!f) return;
    const zoom = this.cameras.main.zoom;
    const now = performance.now(), blink = Math.floor(now / 260) % 2 === 0;
    // M3 light pass under the machines: streetlights and Lamps as warm discs (§13's radius), then each block's
    // substation slab, then the pole wires
    const lit = new Set<number>();
    for (const bi of vis) {
      for (const l of blockLights(st, bi)) {
        const lx = (l.tx + 0.5) * TILE_PX, ly = (l.ty + 0.5) * TILE_PX;
        if (l.lit) { g.fillStyle(LIGHT_COL, 0.11); g.fillCircle(lx, ly, l.r * TILE_PX); }
        if (l.kind === 'lamp') { if (l.lit) lit.add(l.tx * 4096 + l.ty); continue; }
        // streetlight post: a dot, warm when lit, dark with a cross when broken (§13's 3-in-8)
        g.fillStyle(l.broken ? 0x2a2d36 : l.lit ? 0xfff3b0 : 0x8a8f9a, 1); g.fillCircle(lx, ly, 4);
        if (l.broken) { g.lineStyle(1.5, 0xe05a5a, 0.8); g.lineBetween(lx - 4, ly - 4, lx + 4, ly + 4); g.lineBetween(lx - 4, ly + 4, lx + 4, ly - 4); }
      }
      const b = st.blocks[bi], sub = substationAt(st, b.x, b.y);
      if (sub) {
        const sx = sub.tx * TILE_PX, sy = sub.ty * TILE_PX, ss = sub.size * TILE_PX;
        g.fillStyle(0x2b2f3a, 0.95); g.fillRect(sx + 2, sy + 2, ss - 4, ss - 4);
        if (sub.size >= 3) { g.lineStyle(2, 0x4a5060, 1); for (let k = 0; k < 3; k++) g.strokeCircle(sx + ss / 2, sy + 22 + k * 22, 9); }
        g.lineStyle(3 / zoom, sub.on ? 0xb6e36a : sub.kw === 0 ? 0x3a4060 : 0xe05a5a, sub.on ? 0.9 : 0.8); g.strokeRect(sx + 1, sy + 1, ss - 2, ss - 2);
        if (sub.on) { g.fillStyle(0xb6e36a, 0.6 + 0.4 * Math.sin(now / 300)); g.fillCircle(sx + ss - 10, sy + 10, 4); }
      }
    }
    const grid = poleGrid(st);
    g.lineStyle(2 / zoom, 0xc9b98a, 0.8);
    for (const w of grid.links) g.lineBetween(w.x0 * TILE_PX, w.y0 * TILE_PX, w.x1 * TILE_PX, w.y1 * TILE_PX);
    for (const m of f.machines) {
      if (m.x + m.size <= tx0 || m.x > tx1 || m.y + m.size <= ty0 || m.y > ty1) continue;
      const px = m.x * TILE_PX, py = m.y * TILE_PX, sz = m.size * TILE_PX, cx = px + sz / 2, cy = py + sz / 2;
      switch (m.kind) {
        case 'turret': {
          const rounds = m.inv.rounds ?? 0, frac = Math.min(1, rounds / TURRET_HOPPER);
          g.fillStyle(MACHINE_COL.turret, 1); g.fillRect(px + 2, py + 2, sz - 4, sz - 4);
          g.fillStyle(0x262a34, 1); g.fillCircle(cx, cy, 19);
          // barrel along the facing; the street it covers is the nearest one (describeMachine names it)
          const bx = cx + DX[m.dir] * 27, by = cy + DY[m.dir] * 27;
          g.lineStyle(7, 0x9aa3b2, 1); g.lineBetween(cx, cy, bx, by);
          if (m.timer > 0) { g.fillStyle(0xfff0a0, m.timer / TURRET_FLASH_S); g.fillCircle(bx + DX[m.dir] * 5, by + DY[m.dir] * 5, 7); }
          // hopper bar: green → amber → red, blinking outline when empty (the same event turns the map pip red)
          g.fillStyle(0x1a1d26, 1); g.fillRect(px + 6, py + sz - 12, sz - 12, 6);
          g.fillStyle(frac > 0.5 ? 0x6fe08a : frac > 0 ? 0xe8a93a : 0xe05a5a, 1); g.fillRect(px + 6, py + sz - 12, (sz - 12) * frac, 6);
          if (rounds <= 0 && blink) { g.lineStyle(3 / zoom, 0xe05a5a, 1); g.strokeRect(px + 1, py + 1, sz - 2, sz - 2); }
          if (!m.busy) { g.lineStyle(1.5, 0x8a8f9a, 0.6); g.strokeRect(px + 2, py + 2, sz - 4, sz - 4); }
          break;
        }
        case 'lamp': {
          const on = lit.has(m.x * 4096 + m.y);
          g.fillStyle(MACHINE_COL.lamp, 1); g.fillRect(cx - 3, py + 8, 6, TILE_PX - 12);
          g.fillStyle(on ? 0xfff3b0 : 0x3a3a40, 1); g.fillCircle(cx, py + 9, 6);
          if (on) { g.fillStyle(LIGHT_COL, 0.35); g.fillCircle(cx, py + 9, 9); }
          break;
        }
        case 'pole': {
          const on = grid.connected.has(m.id);
          g.fillStyle(MACHINE_COL.pole, 1); g.fillRect(cx - 3, py + 6, 6, TILE_PX - 8);
          g.fillRect(cx - 9, py + 8, 18, 3);
          g.fillStyle(on ? 0xb6e36a : 0x8a8f9a, 1); g.fillCircle(cx - 8, py + 9, 2.5); g.fillCircle(cx + 8, py + 9, 2.5);
          break;
        }
        case 'generator': {
          const coal = m.inv.coal ?? 0;
          g.fillStyle(MACHINE_COL.generator, 1); g.fillRect(px + 2, py + 2, sz - 4, sz - 4);
          g.lineStyle(2, 0x3a1c1c, 1); g.strokeRect(px + 6, py + 6, sz - 12, sz - 12);
          // chimney with a glow while burning
          g.fillStyle(0x2a2a30, 1); g.fillRect(px + sz - 22, py + 6, 12, 22);
          if (m.busy) { g.fillStyle(0xff9a3a, 0.5 + 0.3 * Math.sin(now / 90)); g.fillCircle(px + sz - 16, py + 8, 7); }
          // coal in the hopper: one square per 5 coal, and the burn progress of the current one
          for (let k = 0; k < Math.min(10, Math.ceil(coal / 5)); k++) { g.fillStyle(ITEM_COL.coal, 1); g.fillRect(px + 10 + (k % 5) * 9, py + 12 + Math.floor(k / 5) * 9, 7, 7); }
          g.fillStyle(0x1e1418, 1); g.fillRect(px + 10, py + sz - 16, sz - 20, 8);
          g.fillStyle(0xff9a3a, 1); g.fillRect(px + 10, py + sz - 16, (sz - 20) * Math.min(1, m.timer), 8);
          if (coal <= 0 && blink) { g.lineStyle(3 / zoom, 0xe05a5a, 1); g.strokeRect(px + 1, py + 1, sz - 2, sz - 2); }
          break;
        }
        case 'belt': this.drawBelt(g, m, px, py); break;
        case 'inserter': {
          g.fillStyle(0x3a3220, 1); g.fillRect(px + 2, py + 2, TILE_PX - 4, TILE_PX - 4);
          g.fillStyle(MACHINE_COL.inserter, 1); g.fillCircle(cx, cy, 7);
          const [ix, iy] = inputTile(m), [ox, oy] = outputTile(m);
          const toOut = m.phase === 1;
          const ex = toOut ? ox : ix, ey = toOut ? oy : iy;
          // arm: from the base towards the tile it is reaching into, part-way through the swing
          const prog = m.phase === 0 ? 0 : Math.max(0, Math.min(1, 1 - m.timer / 0.5));
          const reach = m.phase === 0 ? 0.55 : toOut ? 0.25 + prog * 0.5 : 0.75 - prog * 0.5;
          const ax = cx + ((ex + 0.5) * TILE_PX - cx) * reach, ay = cy + ((ey + 0.5) * TILE_PX - cy) * reach;
          g.lineStyle(4, 0xb59a5a, 1); g.lineBetween(cx, cy, ax, ay);
          if (m.hold) { g.fillStyle(ITEM_COL[m.hold], 1); g.fillRect(ax - 4, ay - 4, 8, 8); }
          break;
        }
        case 'excavator': {
          g.fillStyle(MACHINE_COL.excavator, 1); g.fillRect(px + 2, py + 2, sz - 4, sz - 4);
          g.lineStyle(2, 0x2b3440, 1); g.strokeRect(px + 6, py + 6, sz - 12, sz - 12);
          const spin = m.hold ? 0 : (m.timer / (1 / EXCAVATOR_PER_S)) * Math.PI;
          g.lineStyle(5, 0x9fb4cc, 1);
          for (let k = 0; k < 2; k++) { const a = spin + k * Math.PI / 2; g.lineBetween(cx - Math.cos(a) * 14, cy - Math.sin(a) * 14, cx + Math.cos(a) * 14, cy + Math.sin(a) * 14); }
          this.arrow(g, cx, cy, m.dir, sz / 2 - 2, 0xd8dde8, 0.9);
          if (m.hold) { const [ox, oy] = outputTile(m); g.fillStyle(ITEM_COL[m.hold], 1); g.fillRect((ox + 0.5) * TILE_PX - 4 - DX[m.dir] * 10, (oy + 0.5) * TILE_PX - 4 - DY[m.dir] * 10, 8, 8); }
          break;
        }
        case 'assembler': {
          g.fillStyle(MACHINE_COL.assembler, 1); g.fillRect(px + 2, py + 2, sz - 4, sz - 4);
          g.lineStyle(2, 0x33293d, 1); g.strokeRect(px + 6, py + 6, sz - 12, sz - 12);
          this.arrow(g, cx, cy, m.dir, sz / 2 - 2, 0xd8d0e8, 0.7);
          // progress bar and the input/output counts as item squares
          const prog = m.busy ? Math.min(1, m.timer / SHOT.seconds) : 0;
          g.fillStyle(0x1e1826, 1); g.fillRect(px + 10, py + sz - 16, sz - 20, 8);
          g.fillStyle(0xe6d45a, 1); g.fillRect(px + 10, py + sz - 16, (sz - 20) * prog, 8);
          for (let k = 0; k < Math.min(8, m.inv.steel ?? 0); k++) { g.fillStyle(ITEM_COL.steel, 1); g.fillRect(px + 10 + k * 9, py + 10, 7, 7); }
          for (let k = 0; k < Math.min(4, m.inv.copper ?? 0); k++) { g.fillStyle(ITEM_COL.copper, 1); g.fillRect(px + 10 + k * 9, py + 20, 7, 7); }
          for (let k = 0; k < Math.min(5, m.out); k++) { g.fillStyle(ITEM_COL.magazine, 1); g.fillRect(px + sz - 18, py + 10 + k * 9, 7, 7); }
          break;
        }
        case 'depot': {
          g.fillStyle(MACHINE_COL.depot, 0.9); g.fillRect(px, py, sz, sz);
          g.lineStyle(2 / zoom, 0xffffff, 0.8); g.strokeRect(px, py, sz, sz);
          this.depotText.setPosition(cx, cy).setScale(Math.max(1, 1 / zoom)).setVisible(true);
          break;
        }
      }
      // §14: a shed machine is greyed with a cross; it comes back when the supply allows
      if (m.shed) { g.fillStyle(0x0b0e1a, 0.55); g.fillRect(px, py, sz, sz); g.lineStyle(2 / zoom, 0xe05a5a, 0.9); g.lineBetween(px + 4, py + 4, px + sz - 4, py + sz - 4); g.lineBetween(px + 4, py + sz - 4, px + sz - 4, py + 4); }
    }
  }

  private drawBelt(g: Phaser.GameObjects.Graphics, m: Machine, px: number, py: number): void {
    const st = this.st, d = m.dir, e = entryDir(st, m);
    const cx = px + HALF, cy = py + HALF, w = 20;
    g.fillStyle(MACHINE_COL.belt, 1);
    // body: from the entry side to the centre, then from the centre to the exit side
    const seg = (dir: Dir, from: number, to: number) => {   // along dir, from/to in [-1,1] × half tile
      const ax = cx + DX[dir] * from * HALF, ay = cy + DY[dir] * from * HALF, bx = cx + DX[dir] * to * HALF, by = cy + DY[dir] * to * HALF;
      const x0 = Math.min(ax, bx) - (DX[dir] ? 0 : w / 2), y0 = Math.min(ay, by) - (DY[dir] ? 0 : w / 2);
      g.fillRect(x0, y0, DX[dir] ? Math.abs(bx - ax) : w, DY[dir] ? Math.abs(by - ay) : w);
    };
    seg(e, -1, 0); seg(d, 0, 1);
    g.fillStyle(0x3a3e4a, 1); g.fillRect(cx - w / 2, cy - w / 2, w, w);
    this.arrow(g, cx, cy, d, HALF - 3, 0x9c9d4e, 0.8);
    for (const it of m.items) {
      let x: number, y: number;
      // straight: the whole tile along d; corner: the first half along the entry direction to the centre, then along d
      if (e === d) { x = cx + DX[d] * (-1 + it.p * 2) * HALF; y = cy + DY[d] * (-1 + it.p * 2) * HALF; }
      else if (it.p >= 0.5) { x = cx + DX[d] * (it.p - 0.5) * 2 * HALF; y = cy + DY[d] * (it.p - 0.5) * 2 * HALF; }
      else { x = cx + DX[e] * (-1 + it.p * 2) * HALF; y = cy + DY[e] * (-1 + it.p * 2) * HALF; }
      g.fillStyle(ITEM_COL[it.k], 1); g.fillRect(x - 4, y - 4, 8, 8);
      g.lineStyle(1, 0x000000, 0.5); g.strokeRect(x - 4, y - 4, 8, 8);
    }
  }
}
