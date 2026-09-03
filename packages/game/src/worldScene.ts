/** World view (Phase 4 M1): the city at tile level. Every tile is drawn from `cellTiles` in @relight/sim, which
 *  derives it from the authoritative block map; this scene holds only camera state and a cache keyed by
 *  `cellKey`. Camera: drag or WASD/arrows to pan, wheel to zoom 0.5–3×, E back to the map at the same block. */
import Phaser from 'phaser';
import {
  SimState, idxOf, DARK, CONTESTED, HELD, INERT, VOID, isInterior,
  CELL_TILES, TILE_PX, RUBBLE_VARIANTS, T_STREET, T_GROUND, T_RUBBLE, T_INERT, T_RIVER, T_DEPOSIT,
  cellTiles, cellKey, CellTiles, describeTile,
} from '@relight/sim';
import { Session } from './session';
import { View } from './view';

/** GAME-ASSUMPTION: the constitution's 0.5–3× zoom, not §4's 1.0–0.2×; the doc is edited to this range (D-P4-1). */
export const ZOOM_MIN = 0.5, ZOOM_MAX = 3, ZOOM_STEP = 1.15;
const PAN_PX_PER_S = 900;
const CELL_PX = CELL_TILES * TILE_PX;   // 1024 px

// tileset frames: street, ground, inert, river, then rubble (stone/copper/steel × 5 variants), then deposits (iron/coal × 5)
const F_STREET = 0, F_GROUND = 1, F_INERT = 2, F_RIVER = 3, F_RUBBLE = 4, F_DEPOSIT = F_RUBBLE + 3 * RUBBLE_VARIANTS, F_COUNT = F_DEPOSIT + 2 * RUBBLE_VARIANTS;
const RUBBLE_IDX: Record<string, number> = { stone: 0, copper: 1, steel: 2 };
const DEPOSIT_IDX: Record<string, number> = { iron: 0, coal: 1 };
const RUBBLE_COL = ['#b9bfc9', '#c0682b', '#5f83bd'];
const RUBBLE_DARK = ['#7d848f', '#7d4119', '#3a5580'];
const DEPOSIT_COL = ['#9a4c3a', '#1a1b20'];

export interface WorldHooks { onHoverText(text: string | null, px: number, py: number): void }

export class WorldScene extends Phaser.Scene {
  private blitter!: Phaser.GameObjects.Blitter;
  private bobs: Phaser.GameObjects.Bob[] = [];
  private gOver!: Phaser.GameObjects.Graphics;
  private hudText!: Phaser.GameObjects.Text;
  private labels: Phaser.GameObjects.Text[] = [];
  private cells = new Map<number, { key: string; tiles: CellTiles }>();
  private keys!: Record<'W' | 'A' | 'S' | 'D' | 'UP' | 'LEFT' | 'DOWN' | 'RIGHT', Phaser.Input.Keyboard.Key>;
  private hoverTile: { tx: number; ty: number } | null = null;
  private dragging = false;
  private pending: [number, number] | null = null;
  /** Tiles drawn last frame; a dev/test number. */
  drawn = 0;

  constructor(private session: Session, private view: View, private hooks: WorldHooks) { super('world'); }

  private get st(): SimState { return this.session.state; }

  create(): void {
    this.cameras.main.setBackgroundColor(0x05070f);
    this.makeTileset();
    this.blitter = this.add.blitter(0, 0, 'ground');
    this.gOver = this.add.graphics().setDepth(2);
    this.hudText = this.add.text(8, 8, '', { fontSize: '11px', color: '#c7cfe0', backgroundColor: '#0b0e1acc', padding: { x: 6, y: 4 } }).setScrollFactor(0).setDepth(10);
    const cam = this.cameras.main;
    cam.setBounds(0, 0, this.st.w * CELL_PX, this.st.h * CELL_PX);
    cam.setZoom(1);
    cam.setRoundPixels(true);
    this.keys = this.input.keyboard!.addKeys('W,A,S,D,UP,LEFT,DOWN,RIGHT') as WorldScene['keys'];
    this.input.on('pointerdown', () => { this.dragging = true; });
    this.input.on('pointerup', () => { this.dragging = false; });
    this.input.on('gameout', () => { this.dragging = false; this.hoverTile = null; this.hooks.onHoverText(null, 0, 0); });
    this.input.on('pointermove', (p: Phaser.Input.Pointer) => this.onMove(p));
    this.input.on('wheel', (p: Phaser.Input.Pointer, _o: unknown, _dx: number, dy: number) => this.zoomAt(p.x, p.y, dy > 0 ? 1 / ZOOM_STEP : ZOOM_STEP));
    this.events.on(Phaser.Scenes.Events.WAKE, () => this.onWake());
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
    const bx = Math.floor(tx / CELL_TILES), by = Math.floor(ty / CELL_TILES);
    if (bx < 0 || by < 0 || bx >= st.w || by >= st.h) { this.hooks.onHoverText(null, 0, 0); return; }
    const rect = this.game.canvas.getBoundingClientRect();
    this.hooks.onHoverText(`${describeTile(st, tx, ty)}\n${this.blockLine(bx, by)}`, rect.left + p.x, rect.top + p.y);
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
    // GAME-ASSUMPTION: the HQ is a 6×6-tile slab at the start lot's centre until M2 places the Depot proper
    const [sx, sy] = st.start;
    g.fillStyle(0x0b0e1a, 0.85); g.fillRect(sx * CELL_PX + 13 * TILE_PX, sy * CELL_PX + 13 * TILE_PX, 6 * TILE_PX, 6 * TILE_PX);
    g.lineStyle(2 / cam.zoom, 0xffffff, 0.8); g.strokeRect(sx * CELL_PX + 13 * TILE_PX, sy * CELL_PX + 13 * TILE_PX, 6 * TILE_PX, 6 * TILE_PX);

    // HUD: scrollFactor 0 still zooms about the camera centre, so pin it to the top-left at screen scale
    const f = this.focusBlock();
    this.hudText.setScale(1 / cam.zoom).setPosition(cam.width / 2 + (8 - cam.width / 2) / cam.zoom, cam.height / 2 + (8 - cam.height / 2) / cam.zoom);
    this.hudText.setText(`World view · ${this.blockLine(f[0], f[1])} · zoom ${cam.zoom.toFixed(2)}× · ${n} tiles\nE map view · drag / WASD pan · wheel zoom (${ZOOM_MIN}–${ZOOM_MAX}×) · space pause · 1 2 3 speed`);
  }
}
