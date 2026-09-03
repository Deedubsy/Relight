/** World view (Phase 4 M1 + M2): the city at tile level. Every tile is drawn from `cellTiles` in @relight/sim, which
 *  derives it from the authoritative block map; the machines are drawn from `state.flow` (flow.ts). This scene holds
 *  only camera and tool state and a tile cache keyed by `cellKey`. Camera: drag or WASD/arrows to pan, wheel to zoom
 *  0.5–3×, E back to the map at the same block. Tools (M2): X Excavator, B belt, I inserter, M Shot assembler,
 *  R rotate, Q/Esc hand, C craft a magazine by hand; left click/drag places, right click removes; with the hand,
 *  holding the left button on rubble mines it, anywhere else drags the camera. M3: T turret, L lamp, P pole,
 *  G Generator; a hand click on a turret or Generator feeds it from the Depot (§11's hand-feed); substations,
 *  streetlights, light discs and pole wires are drawn from the flow layer's M3 queries. */
import Phaser from 'phaser';
import {
  SimState, idxOf, DARK, CONTESTED, HELD, INERT, VOID, isInterior,
  CELL_TILES, TILE_PX, RUBBLE_VARIANTS, T_STREET, T_GROUND, T_RUBBLE, T_INERT, T_RIVER, T_DEPOSIT, T_PATCH,
  cellTiles, cellKey, CellTiles, describeTile,
  Kind, Dir, DX, DY, DIR_NAMES, Machine, MACHINE_SIZE, MACHINE_COST, SHOT, EXCAVATOR_PER_S,
  machineAt, rubbleAt, canPlace, place, remove, rotate, setHandMine, queueCraft, entryDir, describeMachine, outputTile, inputTile,
  handFeed, cellLights, substationAt, isSubstationTile, poleGrid, flowSummary, TURRET_HOPPER, TURRET_FLASH_S,
  SUBSTATION_TILES, POLE_REACH,
} from '@relight/sim';
import { Session } from './session';
import { View } from './view';

/** GAME-ASSUMPTION: the constitution's 0.5–3× zoom, not §4's 1.0–0.2×; the doc is edited to this range (D-P4-1). */
export const ZOOM_MIN = 0.5, ZOOM_MAX = 3, ZOOM_STEP = 1.15;
const PAN_PX_PER_S = 900;
const CELL_PX = CELL_TILES * TILE_PX;   // 1024 px
const HALF = TILE_PX / 2;

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

export type Tool = 'hand' | 'excavator' | 'belt' | 'inserter' | 'assembler' | 'turret' | 'lamp' | 'pole' | 'generator';
const TOOL_KEYS: Record<string, Tool> = { x: 'excavator', b: 'belt', i: 'inserter', m: 'assembler', t: 'turret', l: 'lamp', p: 'pole', g: 'generator', q: 'hand', escape: 'hand' };

export interface WorldHooks { onHoverText(text: string | null, px: number, py: number): void; onToast(msg: string, kind?: 'info' | 'bad' | 'good'): void }

export class WorldScene extends Phaser.Scene {
  private blitter!: Phaser.GameObjects.Blitter;
  private bobs: Phaser.GameObjects.Bob[] = [];
  private gOver!: Phaser.GameObjects.Graphics;
  private gMach!: Phaser.GameObjects.Graphics;
  private hudText!: Phaser.GameObjects.Text;
  private depotText!: Phaser.GameObjects.Text;
  private labels: Phaser.GameObjects.Text[] = [];
  private cells = new Map<number, { key: string; tiles: CellTiles }>();
  private keys!: Record<'W' | 'A' | 'S' | 'D' | 'UP' | 'LEFT' | 'DOWN' | 'RIGHT', Phaser.Input.Keyboard.Key>;
  private hoverTile: { tx: number; ty: number } | null = null;
  private dragging = false;
  private placing = false;
  private mining = false;
  private pending: [number, number] | null = null;
  /** Current tool and the direction the next machine faces. */
  tool: Tool = 'hand';
  dir: Dir = 1;
  /** Tiles drawn last frame; a dev/test number. */
  drawn = 0;

  constructor(private session: Session, private view: View, private hooks: WorldHooks) { super('world'); }

  private get st(): SimState { return this.session.state; }

  create(): void {
    this.cameras.main.setBackgroundColor(0x05070f);
    this.makeTileset();
    this.blitter = this.add.blitter(0, 0, 'ground');
    this.gMach = this.add.graphics().setDepth(1);
    this.gOver = this.add.graphics().setDepth(2);
    this.depotText = this.add.text(0, 0, 'Depot', { fontSize: '20px', color: '#e8ecf4', fontStyle: 'bold' }).setDepth(3).setOrigin(0.5).setVisible(false);
    this.hudText = this.add.text(8, 8, '', { fontSize: '11px', color: '#c7cfe0', backgroundColor: '#0b0e1acc', padding: { x: 6, y: 4 } }).setScrollFactor(0).setDepth(10);
    const cam = this.cameras.main;
    cam.setBounds(0, 0, this.st.w * CELL_PX, this.st.h * CELL_PX);
    cam.setZoom(1);
    cam.setRoundPixels(true);
    this.keys = this.input.keyboard!.addKeys('W,A,S,D,UP,LEFT,DOWN,RIGHT') as WorldScene['keys'];
    this.input.on('pointerdown', (p: Phaser.Input.Pointer) => this.onDown(p));
    this.input.on('pointerup', () => this.onUp());
    this.input.on('gameout', () => { this.onUp(); this.hoverTile = null; this.hooks.onHoverText(null, 0, 0); });
    this.input.on('pointermove', (p: Phaser.Input.Pointer) => this.onMove(p));
    this.input.on('wheel', (p: Phaser.Input.Pointer, _o: unknown, _dx: number, dy: number) => this.zoomAt(p.x, p.y, dy > 0 ? 1 / ZOOM_STEP : ZOOM_STEP));
    this.events.on(Phaser.Scenes.Events.WAKE, () => this.onWake());
    this.events.on(Phaser.Scenes.Events.SLEEP, () => this.onUp());
    this.onWake();
  }

  private onWake(): void {
    this.hoverTile = null;
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

  private frameOf(c: CellTiles, i: number): number {
    switch (c.kind[i]) {
      case T_STREET: return F_STREET;
      case T_GROUND: return F_GROUND;
      case T_INERT: return F_INERT;
      case T_RIVER: return F_RIVER;
      case T_RUBBLE: return F_RUBBLE + RUBBLE_IDX[c.rubble ?? 'stone'] * RUBBLE_VARIANTS + (c.variant[i] - 1);
      case T_DEPOSIT: return F_DEPOSIT + DEPOSIT_IDX[c.deposit ?? 'iron'] * RUBBLE_VARIANTS + (c.variant[i] - 1);
      case T_PATCH: return F_PATCH + Math.max(0, c.patch[i] - 1);
      default: return F_GROUND;
    }
  }

  private cell(x: number, y: number): CellTiles {
    const st = this.st, i = idxOf(st, x, y), key = cellKey(st, x, y);
    const hit = this.cells.get(i);
    if (hit && hit.key === key) return hit.tiles;
    const tiles = cellTiles(st, x, y);
    this.cells.set(i, { key, tiles });
    return tiles;
  }

  // ------------------------------------------------------------------ camera

  /** World point under a screen point, for the camera's current scroll and zoom (no rotation). */
  private worldAt(sx: number, sy: number, zoom = this.cameras.main.zoom): { x: number; y: number } {
    const cam = this.cameras.main;
    return { x: cam.scrollX + cam.width / 2 + (sx - cam.width / 2) / zoom, y: cam.scrollY + cam.height / 2 + (sy - cam.height / 2) / zoom };
  }

  zoomAt(sx: number, sy: number, factor: number): void {
    const cam = this.cameras.main;
    const z1 = Math.max(ZOOM_MIN, Math.min(ZOOM_MAX, cam.zoom * factor));
    if (z1 === cam.zoom) return;
    const w = this.worldAt(sx, sy);
    cam.setZoom(z1);
    cam.setScroll(w.x - cam.width / 2 - (sx - cam.width / 2) / z1, w.y - cam.height / 2 - (sy - cam.height / 2) / z1);
  }

  setZoom(z: number): void { this.zoomAt(this.cameras.main.width / 2, this.cameras.main.height / 2, z / this.cameras.main.zoom); }
  get zoom(): number { return this.cameras.main.zoom; }

  /** Centre the camera on a block's lot. Safe before create(): the request is kept for the first wake. */
  centreOn(x: number, y: number): void {
    if (!this.cameras?.main) { this.pending = [x, y]; return; }
    this.cameras.main.centerOn((x + 0.5) * CELL_PX, (y + 0.5) * CELL_PX);
  }

  /** The block under the camera's centre: what the map view gets on E. */
  focusBlock(): [number, number] {
    const cam = this.cameras.main;
    const m = this.worldAt(cam.width / 2, cam.height / 2);
    return [Math.max(0, Math.min(this.st.w - 1, Math.floor(m.x / CELL_PX))), Math.max(0, Math.min(this.st.h - 1, Math.floor(m.y / CELL_PX)))];
  }

  // ------------------------------------------------------------------ tools

  /** A key from the page's keydown handler (only while this view is up). Returns true when it was a tool key. */
  key(k: string): boolean {
    const st = this.st;
    const lower = k.toLowerCase();
    if (!st.flow) return false;
    if (lower in TOOL_KEYS) { this.tool = TOOL_KEYS[lower]; return true; }
    if (lower === 'r') {
      const h = this.hoverTile, m = h && this.tool === 'hand' ? machineAt(st, h.tx, h.ty) : undefined;
      if (m && m.kind !== 'depot') rotate(st, m.x, m.y); else this.dir = ((this.dir + 1) % 4) as Dir;
      return true;
    }
    if (lower === 'c') {
      queueCraft(st, 1);
      this.hooks.onToast(`Crafting a magazine by hand (${SHOT.inputs.steel} steel + ${SHOT.inputs.copper} Cu, ${SHOT.seconds} s) — ${st.flow.hand.crafts} queued`);
      return true;
    }
    return false;
  }

  /** Top-left tile of the tool's footprint when the pointer is on (tx, ty): 3×3 machines centre on the pointer,
   *  2×2 ones (turret, Generator) take the pointer's tile as their top-left. */
  private footprint(kind: Kind, tx: number, ty: number): [number, number] {
    const off = Math.floor((MACHINE_SIZE[kind] - 1) / 2);
    return [tx - off, ty - off];
  }

  private tryPlace(tx: number, ty: number, loud: boolean): void {
    const st = this.st;
    if (this.tool === 'hand' || !st.flow) return;
    const kind = this.tool as Kind;
    const [ox, oy] = this.footprint(kind, tx, ty);
    const c = canPlace(st, kind, ox, oy);
    if (!c.ok) { if (loud) this.hooks.onToast(`No ${kind} here: ${c.reason}`, 'bad'); return; }
    place(st, kind, ox, oy, this.dir);
  }

  private onDown(p: Phaser.Input.Pointer): void {
    const st = this.st;
    const w = this.worldAt(p.x, p.y);
    const tx = Math.floor(w.x / TILE_PX), ty = Math.floor(w.y / TILE_PX);
    if (p.rightButtonDown()) {
      if (!st.flow) return;
      const m = machineAt(st, tx, ty);
      if (!m) return;
      if (m.kind === 'depot') { this.hooks.onToast('The Depot stays', 'bad'); return; }
      const cost = MACHINE_COST[m.kind];
      const held = m.kind === 'turret' ? `, ${m.inv.rounds ?? 0} rounds to the line buffer` : m.kind === 'generator' ? `, ${Math.floor(m.inv.coal ?? 0)} coal to the Depot` : '';
      remove(st, tx, ty);
      this.hooks.onToast(`${m.kind} removed — ${cost.steel} steel${cost.copper ? ` + ${cost.copper} Cu` : ''} back in the Depot${held}`);
      return;
    }
    if (!p.leftButtonDown()) return;
    if (this.tool !== 'hand' && st.flow) { this.placing = true; this.tryPlace(tx, ty, true); return; }
    if (this.tool === 'hand' && st.flow) {
      // §11: the tester hand-feeds turrets and the Generator; a click on one moves what the Depot has
      const fed = handFeed(st, tx, ty);
      if (fed) {
        if (fed.moved > 0) this.hooks.onToast(fed.kind === 'turret' ? `Hand-fed ${fed.moved} magazines into the turret` : `Hand-fed ${fed.moved} coal into the Generator`, 'good');
        else this.hooks.onToast(fed.reason, 'bad');
        return;
      }
    }
    if (st.flow && rubbleAt(st, tx, ty)) { this.mining = true; setHandMine(st, [tx, ty]); return; }
    this.dragging = true;
  }

  private onUp(): void {
    this.dragging = false; this.placing = false;
    if (this.mining) { this.mining = false; if (this.st.flow) setHandMine(this.st, null); }
  }

  private onMove(p: Phaser.Input.Pointer): void {
    const cam = this.cameras.main;
    if (this.dragging && p.isDown) {
      cam.scrollX -= (p.position.x - p.prevPosition.x) / cam.zoom;
      cam.scrollY -= (p.position.y - p.prevPosition.y) / cam.zoom;
    }
    const w = this.worldAt(p.x, p.y);
    const tx = Math.floor(w.x / TILE_PX), ty = Math.floor(w.y / TILE_PX);
    if (this.hoverTile && this.hoverTile.tx === tx && this.hoverTile.ty === ty) return;
    this.hoverTile = { tx, ty };
    const st = this.st;
    if (this.placing && p.isDown) this.tryPlace(tx, ty, false);
    if (this.mining && p.isDown && st.flow) setHandMine(st, [tx, ty]);
    const bx = Math.floor(tx / CELL_TILES), by = Math.floor(ty / CELL_TILES);
    if (bx < 0 || by < 0 || bx >= st.w || by >= st.h) { this.hooks.onHoverText(null, 0, 0); return; }
    const rect = this.game.canvas.getBoundingClientRect();
    const m = st.flow ? machineAt(st, tx, ty) : undefined;
    const lines = [describeTile(st, tx, ty), this.blockLine(bx, by)];
    if (m) lines.unshift(describeMachine(st, m));
    else if (st.flow && isSubstationTile(st, tx, ty)) {
      const sub = substationAt(st, bx, by);
      if (sub) lines.unshift(`Substation · ${sub.on ? `on · draws ${sub.kw} kW · streetlights lit` : sub.kw === 0 ? 'Dark · string poles to it to claim' : 'off · no power (brownout or the block is unfed)'}`);
    }
    this.hooks.onHoverText(lines.join('\n'), rect.left + p.x, rect.top + p.y);
  }

  private blockLine(x: number, y: number): string {
    const st = this.st, b = st.blocks[idxOf(st, x, y)];
    const name = b.name === 'civ' ? 'civic' : b.name === 'res' ? 'residential' : b.name === 'ind' ? 'industrial' : 'outskirts';
    const state = y === st.h - 1 ? 'river' : b.state === DARK ? `Dark · rot ${Math.round(b.d * 100)} %` : b.state === CONTESTED ? 'Contested' : b.state === HELD ? (isInterior(st, idxOf(st, x, y)) ? 'Held · interior' : 'Held · front') : b.state === INERT || b.state === VOID ? 'inert' : '?';
    const hq = x === st.start[0] && y === st.start[1] ? ' · HQ' : '';
    return `block (${x},${y}) · ${name} · ${state}${hq}`;
  }

  // ------------------------------------------------------------------ frame

  update(_time: number, delta: number): void {
    const cam = this.cameras.main, dt = Math.min(0.1, delta / 1000);
    const pan = PAN_PX_PER_S * dt / cam.zoom;
    if (this.keys.A.isDown || this.keys.LEFT.isDown) cam.scrollX -= pan;
    if (this.keys.D.isDown || this.keys.RIGHT.isDown) cam.scrollX += pan;
    if (this.keys.W.isDown || this.keys.UP.isDown) cam.scrollY -= pan;
    if (this.keys.S.isDown || this.keys.DOWN.isDown) cam.scrollY += pan;
    this.draw();
  }

  private draw(): void {
    const st = this.st, cam = this.cameras.main;
    const tl = this.worldAt(0, 0), br = this.worldAt(cam.width, cam.height);
    const tx0 = Math.max(0, Math.floor(tl.x / TILE_PX)), ty0 = Math.max(0, Math.floor(tl.y / TILE_PX));
    const tx1 = Math.min(st.w * CELL_TILES - 1, Math.ceil(br.x / TILE_PX)), ty1 = Math.min(st.h * CELL_TILES - 1, Math.ceil(br.y / TILE_PX));
    let n = 0;
    for (let ty = ty0; ty <= ty1; ty++) {
      const cy = Math.floor(ty / CELL_TILES), ly = ty - cy * CELL_TILES;
      for (let tx = tx0; tx <= tx1; tx++) {
        const cx = Math.floor(tx / CELL_TILES), lx = tx - cx * CELL_TILES;
        const c = this.cell(cx, cy);
        const frame = this.frameOf(c, ly * CELL_TILES + lx);
        let bob = this.bobs[n];
        if (!bob) { bob = this.blitter.create(tx * TILE_PX, ty * TILE_PX, frame); this.bobs.push(bob); }
        else { bob.setPosition(tx * TILE_PX, ty * TILE_PX); bob.setFrame(frame); bob.setVisible(true); }
        n++;
      }
    }
    for (let k = n; k < this.bobs.length; k++) if (this.bobs[k].visible) this.bobs[k].setVisible(false);
    this.drawn = n;

    this.drawMachines(tx0, ty0, tx1, ty1);

    // GAME-ASSUMPTION: a flat block-state overlay on each lot (Dark navy, Contested amber, Held outline) stands in for
    // rot presence (M4) and the light texture (M5); it is the block map's word on the lot, drawn, not simulated
    const g = this.gOver;
    g.clear();
    const cx0 = Math.floor(tx0 / CELL_TILES), cy0 = Math.floor(ty0 / CELL_TILES), cx1 = Math.floor(tx1 / CELL_TILES), cy1 = Math.floor(ty1 / CELL_TILES);
    const flicker = Math.sin(performance.now() / 45) > 0 ? 0.3 : 0.15;
    let li = 0;
    for (let cy = cy0; cy <= cy1; cy++) for (let cx = cx0; cx <= cx1; cx++) {
      const i = idxOf(st, cx, cy), b = st.blocks[i];
      const px = cx * CELL_PX + 4 * TILE_PX, py = cy * CELL_PX + 4 * TILE_PX, lot = 24 * TILE_PX;
      if (cy !== st.h - 1) {
        if (b.state === DARK) { g.fillStyle(0x0b1030, 0.45); g.fillRect(px, py, lot, lot); }
        else if (b.state === CONTESTED) { g.fillStyle(0xd99a2b, flicker); g.fillRect(px, py, lot, lot); }
        else if (b.state === HELD) {
          g.lineStyle(4 / cam.zoom, isInterior(st, i) ? 0xffffff : 0xe8a93a, 0.6); g.strokeRect(px, py, lot, lot);
        }
      }
      // faint cell grid on the street centre lines
      g.lineStyle(1 / cam.zoom, 0xffffff, 0.07);
      g.strokeRect(cx * CELL_PX, cy * CELL_PX, CELL_PX, CELL_PX);
      // one label per visible cell, kept small on screen
      let t = this.labels[li];
      if (!t) { t = this.add.text(0, 0, '', { fontSize: '12px', color: '#c7cfe0', backgroundColor: '#0b0e1aaa', padding: { x: 4, y: 2 } }).setDepth(5); this.labels.push(t); }
      t.setText(this.blockLine(cx, cy)).setPosition(cx * CELL_PX + 4 * TILE_PX + 6, cy * CELL_PX + 4 * TILE_PX + 6).setScale(1 / cam.zoom).setVisible(true);
      li++;
    }
    for (let k = li; k < this.labels.length; k++) this.labels[k].setVisible(false);
    if (!st.flow) {
      // GAME-ASSUMPTION: without the flow layer (?flow=0) the HQ is a 6×6-tile slab at the start lot's centre
      const [sx, sy] = st.start;
      g.fillStyle(0x0b0e1a, 0.85); g.fillRect(sx * CELL_PX + 13 * TILE_PX, sy * CELL_PX + 13 * TILE_PX, 6 * TILE_PX, 6 * TILE_PX);
      g.lineStyle(2 / cam.zoom, 0xffffff, 0.8); g.strokeRect(sx * CELL_PX + 13 * TILE_PX, sy * CELL_PX + 13 * TILE_PX, 6 * TILE_PX, 6 * TILE_PX);
    }
    this.drawGhost(g);

    // HUD: scrollFactor 0 still zooms about the camera centre, so pin it to the top-left at screen scale
    const f = this.focusBlock();
    this.hudText.setScale(1 / cam.zoom).setPosition(cam.width / 2 + (8 - cam.width / 2) / cam.zoom, cam.height / 2 + (8 - cam.height / 2) / cam.zoom);
    const toolLine = st.flow
      ? `\nTool: ${this.tool === 'hand' ? 'hand (hold on rubble to mine it; click a turret or Generator to feed it; drag to pan)' : `${this.tool} → ${DIR_NAMES[this.dir]}`}${this.ghostReason ? ` · ${this.ghostReason}` : ''}\nX Excavator · B belt · I inserter · M assembler · T turret · L lamp · P pole · G Generator · R rotate · Q hand · C craft a magazine · right-click removes${this.powerLine()}`
      : '';
    this.hudText.setText(`World view · ${this.blockLine(f[0], f[1])} · zoom ${cam.zoom.toFixed(2)}× · ${n} tiles\nE map view · drag / WASD pan · wheel zoom (${ZOOM_MIN}–${ZOOM_MAX}×) · space pause · 1 2 3 speed${toolLine}`);
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
    if (!st.flow || !h || this.tool === 'hand') return;
    const kind = this.tool as Kind, size = MACHINE_SIZE[kind];
    const [ox, oy] = this.footprint(kind, h.tx, h.ty);
    const c = canPlace(st, kind, ox, oy);
    this.ghostReason = c.ok ? `${c.cost.steel} steel${c.cost.copper ? ` + ${c.cost.copper} Cu` : ''}` : c.reason;
    const col = c.ok ? 0x6fe08a : 0xe05a5a;
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
  private drawMachines(tx0: number, ty0: number, tx1: number, ty1: number): void {
    const st = this.st, g = this.gMach, f = st.flow;
    g.clear();
    this.depotText.setVisible(false);
    if (!f) return;
    const zoom = this.cameras.main.zoom;
    const now = performance.now(), blink = Math.floor(now / 260) % 2 === 0;
    // M3 light pass under the machines: streetlights and Lamps as warm discs (§13's radius), then each cell's
    // substation slab, then the pole wires
    const lit = new Set<number>();
    const cx0 = Math.floor(tx0 / CELL_TILES), cy0 = Math.floor(ty0 / CELL_TILES), cx1 = Math.floor(tx1 / CELL_TILES), cy1 = Math.floor(ty1 / CELL_TILES);
    for (let cy = cy0; cy <= cy1; cy++) for (let cx = cx0; cx <= cx1; cx++) {
      for (const l of cellLights(st, cx, cy)) {
        const lx = (l.tx + 0.5) * TILE_PX, ly = (l.ty + 0.5) * TILE_PX;
        if (l.lit) { g.fillStyle(LIGHT_COL, 0.11); g.fillCircle(lx, ly, l.r * TILE_PX); }
        if (l.kind === 'lamp') { if (l.lit) lit.add(l.tx * 4096 + l.ty); continue; }
        // streetlight post: a dot, warm when lit, dark with a cross when broken (§13's 3-in-8)
        g.fillStyle(l.broken ? 0x2a2d36 : l.lit ? 0xfff3b0 : 0x8a8f9a, 1); g.fillCircle(lx, ly, 4);
        if (l.broken) { g.lineStyle(1.5, 0xe05a5a, 0.8); g.lineBetween(lx - 4, ly - 4, lx + 4, ly + 4); g.lineBetween(lx - 4, ly + 4, lx + 4, ly - 4); }
      }
      const sub = substationAt(st, cx, cy);
      if (sub) {
        const sx = sub.tx * TILE_PX, sy = sub.ty * TILE_PX, ss = SUBSTATION_TILES * TILE_PX;
        g.fillStyle(0x2b2f3a, 0.95); g.fillRect(sx + 2, sy + 2, ss - 4, ss - 4);
        g.lineStyle(2, 0x4a5060, 1); for (let k = 0; k < 3; k++) g.strokeCircle(sx + ss / 2, sy + 22 + k * 22, 9);
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
