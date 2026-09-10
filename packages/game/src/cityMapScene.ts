import {campaignOutages} from '@relight/sim';
import {baseCore,RIVERFRONT,riverfrontRail} from '@relight/sim';
import { uiInput, uiPalette } from './uiShell';
import {navigationTargets,knownDistrict,blockName,machineRunning} from '@relight/sim';
import {navigationView} from './view';
import {dispatch} from './session';
import { campaignDiscoveries, stationRoute, truckRect } from '@relight/sim';
import { transportView } from './view';
/** The map view on a street-first city (D6): every block is the polygon the generator rasterised, drawn from sim
 *  state. The ground — streets, water, lots coloured by state, front edges painted along the street segment they
 *  share — goes into one canvas texture repainted a few times a second; pips, icons, pulses and the engineer are
 *  drawn on top every frame. Same hooks and driver as the lattice MapScene (mapScene.ts), which stays for
 *  ?map=lattice and the lattice-era snapshots. Rework Step 5 (D5/D6). */
import Phaser from 'phaser';
import {
  SimState, SimEvent, DARK, CONTESTED, HELD, INERT, VOID, idxOf, isCandidate, rotOf, rotTier, frontList, FrontEdgeView,
  claimInfo, heldInfo, nearestHeld, facilityList, survivorList, isInterior, poolMax, CityGeom,
  STREET, WATER, segKey, activationCheck, claimNeed, blockNameAt, ground, cityGeomOf, isCampaign, surveyedDistrict, campaignRecruited,
} from '@relight/sim';
import { Session } from './session';
import { SceneHooks, MapView, C } from './mapScene';

/** Layout pass (ROADMAP §2 "the map view as a full-screen overlay"): the inset the map keeps from the canvas edge.
 *  It used to be the inset inside a fixed 648 px square; the square is gone and the map is fitted to the canvas. */
const PAD = 4;
/** Dark lots by rot tier (0–3), plain and inside a well's influence. */
const DARK_TIER = [0x16204a, 0x1c2a5e, 0x243774, 0x30478f];
const DARK_WELL = [0x161a48, 0x1c2260, 0x262d78, 0x333b94];
const STREET_C = 0x0e1326, WATER_C = 0x2f3a4f, OUTSIDE_C = 0x0b0e1a, KIT_WAIT = 0x7a3020;

/** A ring drawn at a block's centre, or (M3: the hopper-empty pulse) at a segment's pip when `x`/`y` are set. */
interface Pulse { i: number; x?: number; y?: number; born: number; dur: number; r0: number; r1: number; color: number; width: number; wake: boolean }
interface HulkMark { i: number; born: number }
interface PoleLine { a: number; b: number }

export class CityMapScene extends Phaser.Scene implements MapView {
  private readonly session: Session;
  private readonly hooks: SceneHooks;
  private geom!: CityGeom;
  private ppt = 1;   // pixels per tile
  private W = 0; private H = 0;
  /** Where the fitted map's top-left sits on the canvas — it is centred, so this is not `PAD` any more. */
  private ox = PAD; private oy = PAD;
  private groundImg!: Phaser.GameObjects.Image;
  private playerLabel!: Phaser.GameObjects.Text;
  private headText!: Phaser.GameObjects.Text;
  private pixOwner!: Int32Array;              // per ground pixel: block id, -1 street, -2 water, -3 outside the canvas
  private ridgePx: Int32Array[] = [];         // per street segment: the ground pixels of its ridge
  private tex!: Phaser.Textures.CanvasTexture;
  private img!: ImageData;
  private lastPaint = -1e9;
  private gFx!: Phaser.GameObjects.Graphics;
  private gEdges!: Phaser.GameObjects.Graphics;
  private labels: Phaser.GameObjects.Text[] = [];
  private cityLabels: Phaser.GameObjects.Text[] = [];
  private facilityLabelsFor = '';
  private survivorLabels: Phaser.GameObjects.Text[] = [];
  private survivorLabelsFor = '';
  private pulses: Pulse[] = [];
  private hulks: HulkMark[] = [];
  private poles: PoleLine[] = [];
  private hover = -1;
  private selectedEdge: number | null = null;
  private edgesCache: FrontEdgeView[] = [];
  private edgesCacheT = -1;
  private focusMark: { i: number; born: number } | null = null;

  constructor(session: Session, hooks: SceneHooks) { super('map'); this.session = session; this.hooks = hooks; }

  create(): void {
    const st = this.session.state;
    this.cameras.main.setBackgroundColor(C.bg);
    this.geom = cityGeomOf(st);
    const g = this.geom;
    this.tex = (this.textures.exists('city-ground') ? this.textures.get('city-ground') : this.textures.createCanvas('city-ground', 8, 8)) as Phaser.Textures.CanvasTexture;
    this.groundImg = this.add.image(PAD, PAD, 'city-ground').setOrigin(0);
    this.gEdges = this.add.graphics();
    this.gFx = this.add.graphics();
    this.playerLabel=this.add.text(0,0,'YOU',{fontFamily:'sans-serif',fontSize:'12px',fontStyle:'bold',color:'#ffffff',backgroundColor:'#102326',padding:{x:5,y:3}}).setOrigin(.5,0).setDepth(50);
    this.headText = this.add.text(PAD + 4, PAD + 2, `${g.tw}×${g.th} · ${st.city!.preset} · seed ${st.seed} · ${st.blocks.length} blocks`, { fontSize: '10px', color: '#5c6a8a' }).setDepth(6);
    this.layout();
    this.scale.on('resize', () => this.layout());

    this.input.on('pointermove', (p: Phaser.Input.Pointer) => this.onMove(p));
    this.input.on('pointerdown', (p: Phaser.Input.Pointer) => this.onDown(p));
    this.input.on('gameout', () => { this.setHover(-1); this.hooks.onHover(null, 0, 0); });
  }

  /** Layout pass: fit the map to the canvas — the largest whole-tile scale that fits inside `PAD`, centred. The
   *  ground raster is one sample per pixel, so it is rebuilt whenever the fitted size changes; at the old fixed
   *  648 px it was 0.8 px per tile, and a wider canvas simply buys more of them. */
  private layout(): void {
    const g = this.geom, cam = this.cameras.main, reserved=this.session.state.city?.mapId?230*(Number(getComputedStyle(document.documentElement).getPropertyValue('--ui-scale'))||1):0;
    const ppt = Math.min(Math.max(64, cam.width - reserved - 2 * PAD) / g.tw, Math.max(64, cam.height - 2 * PAD - (this.session.state.city?.mapId?130:0)) / g.th);
    const W = Math.max(1, Math.floor(g.tw * ppt)), H = Math.max(1, Math.floor(g.th * ppt));
    this.ppt = ppt;
    if (W !== this.W || H !== this.H) {
      this.W = W; this.H = H;
      // one sample per ground pixel (the narrowest street is 6 tiles, so it always shows)
      this.pixOwner = new Int32Array(W * H);
      for (let py = 0; py < H; py++) for (let px = 0; px < W; px++) {
        const tx = Math.floor(px / ppt), ty = Math.floor(py / ppt);
        let o = -3;
        if (tx < g.tw && ty < g.th) { const k = g.kind[ty * g.tw + tx]; o = k === STREET ? -1 : k === WATER ? -2 : g.owner[ty * g.tw + tx]; }
        this.pixOwner[py * W + px] = o;
      }
      this.ridgePx = g.segs.map(s => {
        const set = new Set<number>();
        for (const t of s.ridge) { const px = Math.floor((t % g.tw) * ppt), py = Math.floor(Math.floor(t / g.tw) * ppt); if (px < W && py < H) set.add(py * W + px); }
        return Int32Array.from(set);
      });
      this.tex.setSize(W, H);
      this.groundImg.setTexture('city-ground');   // re-reads the base frame, so the image takes the new size
      this.img = this.tex.context.createImageData(W, H);
      this.lastPaint = -1e9;   // repaint at the new size on the next frame
    }
    this.ox = Math.round((cam.width - reserved - W) / 2);
    this.oy = Math.round((cam.height - H) / 2)-(this.session.state.city?.mapId?40:0);
    this.groundImg.setPosition(this.ox, this.oy);
    this.headText.setPosition(this.ox + 4, this.oy + 2);
  }

  // --- geometry helpers ---------------------------------------------------------------------------------------
  private px(i: number): number { return this.ox + this.geom.blocks[i].cx * this.ppt; }
  private py(i: number): number { return this.oy + this.geom.blocks[i].cy * this.ppt; }
  private blockAtPixel(x: number, y: number): number {
    const px = Math.floor(x - this.ox), py = Math.floor(y - this.oy);
    if (px < 0 || py < 0 || px >= this.W || py >= this.H) return -1;
    const o = this.pixOwner[py * this.W + px];
    return o >= 0 ? o : -1;
  }
  private segOf(e: FrontEdgeView): number {
    const st = this.session.state;
    const a = idxOf(st, e.from.x, e.from.y), b = idxOf(st, e.to.x, e.to.y);
    return this.geom.segAt.get(segKey(st.blocks.length, a, b)) ?? -1;
  }
  private edgePos(e: FrontEdgeView): { x: number; y: number } {
    const k = this.segOf(e);
    if (k < 0) return { x: this.px(idxOf(this.session.state, e.from.x, e.from.y)), y: this.py(idxOf(this.session.state, e.from.x, e.from.y)) };
    const s = this.geom.segs[k];
    return { x: this.ox + s.mx * this.ppt, y: this.oy + s.my * this.ppt };
  }
  private edges(): FrontEdgeView[] {
    const st = this.session.state;
    if (this.edgesCacheT !== st.t || this.edgesCache.length !== st.ring.length) { this.edgesCache = frontList(st); this.edgesCacheT = st.t; }
    return this.edgesCache;
  }
  private pipAt(px: number, py: number): FrontEdgeView | null {
    let best: FrontEdgeView | null = null, bd = 64;
    for (const e of this.edges()) { const p = this.edgePos(e); const d = (p.x - px) ** 2 + (p.y - py) ** 2; if (d < bd) { bd = d; best = e; } }
    return best;
  }
  private setHover(i: number): void { if (i !== this.hover) { this.hover = i; this.lastPaint = -1e9; } }

  // --- input --------------------------------------------------------------------------------------------------
  private onMove(p: Phaser.Input.Pointer): void {
    const i = this.blockAtPixel(p.x, p.y);
    this.setHover(i);this.registry.set('knownSiteHover','');
    if (i < 0) { this.hooks.onHover(null, 0, 0); return; }
    const st = this.session.state, b = st.blocks[i];
    if(isCampaign(st)){this.hooks.onHover(null,0,0);const site=campaignDiscoveries(st).find(s=>Math.hypot(this.ox+(s.x+s.size/2)*this.ppt-p.x,this.oy+(s.y+s.size/2)*this.ppt-p.y)<7);this.registry.set('knownSiteHover',site?`${site.title} · ${site.status}`:'');return;}
    const rect = this.game.canvas.getBoundingClientRect();
    if (b.state === DARK) this.hooks.onHover(claimInfo(st, b.x, b.y), rect.left + p.x, rect.top + p.y);
    else if (b.state === HELD) this.hooks.onHover(heldInfo(st, b.x, b.y), rect.left + p.x, rect.top + p.y);
    else this.hooks.onHover(null, 0, 0);
  }
  private onDown(p: Phaser.Input.Pointer): void {
    if(uiInput.blocked)return;
    if(this.session.state.campaign){if(!p.leftButtonDown())return;const st=this.session.state,x=Math.floor((p.x-this.ox)/this.ppt),y=Math.floor((p.y-this.oy)/this.ppt);
      if(p.event.shiftKey){const r=dispatch(this.session,{type:'navigation',action:{type:'pin',x,y,name:''}});this.hooks.onToast(r.reason);if(r.ok)navigationView.targetId=`pin:${st.campaign!.navigation!.pins.at(-1)!.id}`;return;}
      const project=campaignDiscoveries(st).find(s=>Math.hypot(this.ox+(s.x+s.size/2)*this.ppt-p.x,this.oy+(s.y+s.size/2)*this.ppt-p.y)<7);if(project){navigationView.targetId=`site:${project.id}`;return;}
      const target=navigationTargets(st).filter(t=>t.kind!=='district').map(t=>({t,d:Math.hypot(this.ox+t.x*this.ppt-p.x,this.oy+t.y*this.ppt-p.y)})).filter(v=>v.d<=7).sort((a,b)=>a.d-b.d)[0];if(target&&!st.flow?.machines.some(m=>m.kind==='tramstop'&&Math.hypot(this.ox+(m.x+1)*this.ppt-p.x,this.oy+(m.y+1)*this.ppt-p.y)<=8)){navigationView.targetId=target.t.id;return;}
    }
    if(p.leftButtonDown()&&this.session.state.flow){
      const st=this.session.state,near=st.flow!.machines.filter(m=>m.kind==='tramstop').map(m=>({m,d:Math.hypot(this.ox+(m.x+m.size/2)*this.ppt-p.x,this.oy+(m.y+m.size/2)*this.ppt-p.y)})).filter(v=>v.d<=8).sort((a,b)=>a.d-b.d)[0];
      if(near){if(transportView.stopId===near.m.id)transportView.stopId=null;else{transportView.stopId=near.m.id;this.hooks.onStationSelect?.(near.m.x,near.m.y);}this.lastPaint=-1e9;return;}
    }
    transportView.stopId=null;
    const pip = this.pipAt(p.x, p.y);
    if (pip) {
      this.selectedEdge = this.selectedEdge === pip.id ? null : pip.id;
      this.hooks.onPipSelect(this.selectedEdge === null ? null : pip);
      this.lastPaint = -1e9;
      return;
    }
    const st = this.session.state;
    if(isCampaign(st))return;
    const i = this.blockAtPixel(p.x, p.y);
    if (i < 0) return;
    const b = st.blocks[i];
    if (b.state === DARK) {
      // RI-03 (plan §4.1, D-RI-2): the map selects, previews and plans — it charges nothing and grants nothing. The
      // claim is physical: a pole run to the block's substation, the materials delivered there from the pockets (E)
      // and an explicit Activate (E) within reach. The tooltip carries the preview; the click says what it still needs.
      const info = claimInfo(st, b.x, b.y), chk = activationCheck(st, b.x, b.y), need = claimNeed(st);
      const sign = info.frontDelta >= 0 ? '+' : '−';
      const deliver = need.steel + need.copper > 0 ? `deliver ${need.steel} steel + ${need.copper} Cu to it (E)` : 'nothing to deliver (economy off)';
      this.hooks.onToast(`${blockNameAt(st, b.x, b.y)} — rot ${Math.round(info.rot * 100)} % · front ${sign}${Math.abs(info.frontDelta)} · closes ${info.closes} · wake bloom ≈ ${info.wakeBloomCrawlers}. The map claims nothing: string poles (7) to its substation, ${deliver}, then E on the substation to Activate. ${chk.ok ? 'Ready — E at its substation activates it' : `Now: ${chk.reason}`}`, chk.ok ? 'good' : 'info');
    }
  }

  selectEdge(id: number | null): void { this.selectedEdge = id; this.lastPaint = -1e9; }
  hoverBlock(): [number, number] | null { return this.hover >= 0 ? [this.session.state.blocks[this.hover].x, this.session.state.blocks[this.hover].y] : null; }
  markFocus(b: [number, number], now: number): void { const i = idxOf(this.session.state, b[0], b[1]); if (i >= 0) this.focusMark = { i, born: now }; }

  consume(events: SimEvent[], now: number): void {
    const st = this.session.state;
    for (const ev of events) {
      if (ev.type === 'bloom') {
        const i = idxOf(st, ev.x, ev.y); if (i < 0) continue;
        const r1 = 6 + ev.cr * 0.55;
        this.pulses.push({ i, born: now, dur: ev.wake ? 1500 : 900, r0: 3, r1, color: C.crawler, width: ev.wake ? 3 : 1.5, wake: ev.wake });
        if (ev.sh > 0) this.pulses.push({ i, born: now + 120, dur: ev.wake ? 1500 : 900, r0: 2, r1: r1 * 0.7, color: C.shade, width: ev.wake ? 2.5 : 1.5, wake: ev.wake });
        if (ev.hu > 0) this.hulks.push({ i, born: now });
      } else if (ev.type === 'claim') {
        const from = nearestHeld(st, ev.x, ev.y), i = idxOf(st, ev.x, ev.y);
        if (from && i >= 0) this.poles.push({ a: idxOf(st, from.x, from.y), b: i });
      } else if (ev.type === 'hopper-empty') {
        // prompt B M3: the pulse lands on the segment's pip — the same event turns that pip red — not the block's centre
        const i = idxOf(st, ev.x, ev.y), j = idxOf(st, ev.nx, ev.ny); if (i < 0) continue;
        // prompt B M4: in a world-view session the map scene consumes events before its create() ran; no geometry yet, so the pulse sits on the block
        const k = j >= 0 && this.geom ? this.geom.segAt.get(segKey(st.blocks.length, i, j)) ?? -1 : -1;
        const at = k >= 0 ? { x: this.ox + this.geom.segs[k].mx * this.ppt, y: this.oy + this.geom.segs[k].my * this.ppt } : {};
        this.pulses.push({ i, ...at, born: now, dur: 900, r0: 10, r1: 4, color: C.red, width: 2, wake: false });
      } else if (ev.type === 'fall') {
        const i = idxOf(st, ev.x, ev.y); if (i < 0) continue;
        this.pulses.push({ i, born: now, dur: 1200, r0: 14, r1: 4, color: C.red, width: 3, wake: false });
        this.poles = this.poles.filter(l => l.b !== i);
      }
    }
  }

  update(time: number): void {
    if (time - this.lastPaint > 150) { this.paint(time); this.lastPaint = time; }
    this.draw(time);
  }

  // --- the ground: lots by state, streets, water, front edges along their segments ------------------------------
  private paint(now: number): void {
    const st = this.session.state, g = this.geom, n = st.blocks.length;
    const flicker = Math.sin(now / 45) > 0;
    const col = new Int32Array(n), inner = new Uint8Array(n);
    const wellSet = new Set(st.wells.map(([x, y]) => idxOf(st, x, y)));
    const selDark = this.selectedEdge !== null ? this.edges().find(e => e.id === this.selectedEdge) : undefined;
    const selIdx = selDark ? idxOf(st, selDark.to.x, selDark.to.y) : -1;
    for (let i = 0; i < n; i++) {
      const b = st.blocks[i];
      let c: number;
      switch (b.state) {
        case DARK: { if(isCampaign(st)){c=0x222c39;break;} const tier = rotTier(rotOf(st, i)); c = wellSet.has(i) ? C.wellCell : (b.well ? DARK_WELL : DARK_TIER)[tier]; break; }
        case CONTESTED: c = flicker ? C.contested : 0x8a6320; break;
        case HELD: { const inn = isInterior(st, i); inner[i] = inn ? 1 : 0; c = inn ? C.interior : C.held; break; }
        case INERT: c = C.inert; break;
        case VOID: default: c = C.void; break;
      }
      const district=surveyedDistrict(st,i);
      if(district&&[HELD,DARK].includes(b.state))c=({civ:0x676547,res:0x425f79,ind:0x795448,out:0x456653})[district];
      if (i === this.hover || i === selIdx) c = lighten(c, i === selIdx ? 0.35 : 0.22);
      col[i] = st.city?.mapId?0x3c4b40:c;
    }
    const d = this.img.data, W = this.W, H = this.H, own = this.pixOwner;
    for (let p = 0, o4 = 0; p < W * H; p++, o4 += 4) {
      const o = own[p];
      let c = o >= 0 ? col[o] : o === -1 ? (st.city?.mapId?0x72766c:STREET_C) : o === -2 ? (st.city?.mapId?0x24505c:WATER_C) : OUTSIDE_C;
      // §5 interior: a one-pixel white rim inside the lot, like the lattice's white outline
      if (!st.city?.mapId && o >= 0 && inner[o] && ((p % W > 0 && own[p - 1] !== o) || (p >= W && own[p - W] !== o) || (p % W < W - 1 && own[p + 1] !== o) || (p + W < W * H && own[p + W] !== o))) c = C.white;
      d[o4] = (c >> 16) & 255; d[o4 + 1] = (c >> 8) & 255; d[o4 + 2] = c & 255; d[o4 + 3] = 255;
    }
    // front edges: the whole street segment between the Held block and its Dark neighbour, red (dim until its kit arrives)
    for (const e of this.edges()) {
      const k = this.segOf(e); if (k < 0) continue;
      const c = e.kit ? C.edge : KIT_WAIT;
      for (const p of this.ridgePx[k]) { const o4 = p * 4; d[o4] = (c >> 16) & 255; d[o4 + 1] = (c >> 8) & 255; d[o4 + 2] = c & 255; }
    }
    void g;
    this.tex.context.putImageData(this.img, 0, 0);
    const urban = ground(st).urban;
    if (urban) {
      const ctx = this.tex.context;
      for (const s of urban.structures) {
        ctx.fillStyle = st.city?.mapId ? (s.role==='residential'?'#b29a7b':s.role==='civic'?'#86b9a5':'#7e9690') : st.blocks[s.block].state === HELD ? '#b8a886' : '#586a72';
        ctx.fillRect(s.x * this.ppt, s.y * this.ppt, Math.max(1, s.w * this.ppt), Math.max(1, s.h * this.ppt));
      }
    }
    if(st.city?.mapId){const ctx=this.tex.context;for(const y of RIVERFRONT.yards){ctx.fillStyle='#89816a';ctx.fillRect(y.x*this.ppt,y.y*this.ppt,y.w*this.ppt,y.h*this.ppt);}}
    this.tex.refresh();
  }

  // --- everything on top of the ground --------------------------------------------------------------------------
  private draw(now: number): void {
    const st = this.session.state;
    if(isCampaign(st)){
      const district=surveyedDistrict(st,this.hover),districtName=knownDistrict(st,this.hover)?blockName(st,this.hover):'Uncharted district',name=district?({civ:'Civic',res:'Residential',ind:'Industrial',out:'Outskirts'})[district]:'';
      this.headText.setText(this.registry.get('knownSiteHover') || (this.hover>=0?`${districtName} · ${!knownDistrict(st,this.hover)?'Uncharted':st.campaign?.navigation?.visitedBlocks.includes(this.hover)?'Visited':'Known / no visit recorded'}${name?' · '+name:''}`:'')).setStyle(uiPalette()).setPadding(8).setWordWrapWidth(Math.min(320,this.W-24)).setPosition(this.ox+8,this.oy+this.H-200);
    }
    const g = this.gFx;
    g.clear();
    const throb = 0.5 + 0.5 * Math.sin(now / 600);
    const blinkSlow = Math.floor(now / 400) % 2 === 0;
    const wellSet = new Set(st.wells.map(([x, y]) => idxOf(st, x, y)));
    for (let i = 0; i < st.blocks.length; i++) {
      const b = st.blocks[i], px = this.px(i), py = this.py(i);
      if (b.state === DARK && !isCampaign(st)) {
        if (wellSet.has(i)) { g.lineStyle(1.5, C.wellGlow, 0.35 + 0.55 * throb); g.strokeCircle(px, py, 7); }
        if (b.awake && b.timer >= 0) {   // bloom timer ring: fills as the timer counts down
          const T = st.config.bloomT / (0.5 + b.d);
          const frac = Math.max(0, Math.min(1, 1 - (b.timer - st.t) / T));
          g.lineStyle(1.5, C.mottle[3], 0.8); g.beginPath(); g.arc(px, py, 5, -Math.PI / 2, -Math.PI / 2 + frac * Math.PI * 2, false); g.strokePath();
        }
      } else if (b.state === HELD && !st.city?.mapId) {
        const inner = isInterior(st, i), hq = i === idxOf(st, st.start[0], st.start[1]);
        // §12 finite rubble: a strip above the centre in the district's rubble colour, fading as the pool drains
        const pm = poolMax(st, b.name);
        if (pm > 0 && b.pool > 0) { g.fillStyle(C.rubble[b.name], 0.2 + 0.8 * (b.pool / pm)); g.fillRect(px - 6, py - 7, 12, 2); }
        // §5 machine slot: outline = free interior slot, filled = assembler, red = at risk (block back on the front)
        if (b.machines > 0) {
          const risk = !inner && !hq;
          g.fillStyle(risk ? C.red : C.machine, 1); g.fillRect(px + 3, py + 2, 5, 5);
          if (risk && blinkSlow) { g.lineStyle(1.5, C.red, 1); g.strokeRect(px + 1.5, py + 0.5, 8, 8); }
        } else if (inner) { g.lineStyle(1, C.machine, 0.9); g.strokeRect(px + 3.5, py + 2.5, 4, 4); }
        if (!b.subOn) { g.fillStyle(C.red, 0.9); g.fillRect(px - 2, py - 2, 4, 4); }
      }
    }
    for(const t of navigationTargets(st)){const x=this.ox+t.x*this.ppt,y=this.oy+t.y*this.ppt;g.lineStyle(t.id===navigationView.targetId?3:1,t.kind==='pin'?0xf5c24f:t.visited?0x92d8b5:0x69837d,1);if(t.kind==='pin')g.strokeCircle(x,y,5);else if(t.kind==='district'&&!st.city?.mapId)g.strokeCircle(x,y,3);if(t.id===navigationView.targetId)g.strokeCircle(x,y,9);}
    for(const s of st.campaign?.progression?.sites??[])if(s.kind==='core'&&!s.seen){g.lineStyle(2,0xc99bfa,.8);g.strokeCircle(this.ox+s.searchX*this.ppt,this.oy+s.searchY*this.ppt,28*this.ppt);}
    for(const site of campaignDiscoveries(st).filter(s=>!st.campaign?.progression?.sites.some(p=>p.id===s.id&&(p.recovered||p.kind==='core'&&!p.seen)))){const x=this.ox+(site.x+site.size/2)*this.ppt,y=this.oy+(site.y+site.size/2)*this.ppt;g.lineStyle(1,0xb9e3ff,1);if(st.campaign?.recruits?.sites.some(s=>s.id===site.id)){g.strokeCircle(x,y-3,2);g.lineBetween(x,y-1,x,y+4);g.lineBetween(x-3,y+1,x+3,y+1);}else g.strokeRect(x-3,y-3,6,6);}
    if(st.city?.mapId){const marks=[{name:`HOME · Founders Court${campaignOutages(st).some(a=>a.id===`outage:${st.campaign?.homeBlock}`)?' · NO POWER':''}`,x:RIVERFRONT.court.x,y:RIVERFRONT.court.y+12},...RIVERFRONT.plants.map((p,i)=>({name:`P${i+1} · ${p.name} · ${st.campaign?.progression?.sites.find(s=>s.id===p.id)?.installed?(baseCore(st,st.campaign.progression.sites.find(s=>s.id===p.id)!.block)?.hp===0?'DAMAGED':st.campaign.progression.sites.find(s=>s.id===p.id)?.enabled?'ON':'OFF'):'inactive'}`,x:p.x,y:p.y-5})),...RIVERFRONT.stops.map(s=>({name:s.name,x:s.x,y:s.y+5})),{name:'WESTRIDGE HOMES',x:126,y:230},{name:'OLD TOWN',x:294,y:240},{name:'GRAYWATER RIVER',x:RIVERFRONT.width/2,y:RIVERFRONT.riverY+25},...campaignDiscoveries(st).filter(d=>st.campaign?.recruits?.sites.some(s=>s.id===d.id)).map(d=>({name:d.title,x:d.x,y:d.y+5}))];let i=0;for(const m of marks){const l=this.cityLabels[i]??(this.cityLabels[i]=this.add.text(0,0,'',{fontSize:'12px',color:'#f0e3be',backgroundColor:'#192a29e6',padding:{x:4,y:3}}).setDepth(6));l.setText(m.name).setPosition(Math.max(l.width/2+8,Math.min(this.W-l.width/2-8,this.ox+m.x*this.ppt)),this.oy+m.y*this.ppt).setOrigin(.5).setVisible(true);i++;}}
    for(const outage of campaignOutages(st))if(outage.location){
      const x=this.ox+outage.location.x*this.ppt+20,y=this.oy+outage.location.y*this.ppt-20;
      g.fillStyle(0x211816,1);g.fillCircle(x,y,11);g.lineStyle(2,0xff8b64,1);g.strokeCircle(x,y,11);
      g.beginPath();g.moveTo(x+2,y-8);g.lineTo(x-4,y+1);g.lineTo(x+2,y+1);g.lineTo(x-2,y+8);g.strokePath();g.lineBetween(x-8,y-8,x+8,y+8);
    }
    // start marker
    if(!st.city?.mapId){ const i = idxOf(st, st.start[0], st.start[1]); g.lineStyle(1, C.white, 0.7); g.strokeRect(this.px(i) - 5, this.py(i) - 5, 10, 10); }

    // facilities (§8 skyline) and survivors
    const facs = (isCampaign(st)?[]:facilityList(st)).filter(f => f.visible);
    const key = facs.map(f => `${f.name}${f.held ? 1 : 0}`).join('|');
    for (const f of facs) this.drawFacility(g, f);
    if (key !== this.facilityLabelsFor) {
      this.facilityLabelsFor = key;
      for (const l of this.labels) l.destroy();
      this.labels = facs.map(f => { const i = idxOf(st, f.x, f.y); return this.add.text(this.px(i), this.py(i) + 9, f.name,
        { fontSize: '9px', color: f.held ? '#f5c24f' : '#c7cfe0', backgroundColor: '#0b0e1acc', padding: { x: 2, y: 1 } }).setOrigin(0.5, 0).setDepth(5); });
    }
    const survs = (isCampaign(st)?[]:survivorList(st)).filter(f => f.revealed);
    const skey = survs.map(f => `${f.tag}${f.held ? 1 : 0}`).join('|');
    for (const f of survs) {
      const i = idxOf(st, f.x, f.y), px = this.px(i), py = this.py(i), colr = f.held ? C.facilityHeld : C.survivor;
      g.fillStyle(colr, 1); g.fillCircle(px - 4, py - 5, 2.5); g.fillRoundedRect(px - 7, py - 2, 6, 6, 1.5);
      g.fillStyle(0x000000, 0.55); g.fillRoundedRect(px + 1, py - 8, 8, 9, 2);
    }
    if (skey !== this.survivorLabelsFor) {
      this.survivorLabelsFor = skey;
      for (const l of this.survivorLabels) l.destroy();
      this.survivorLabels = survs.flatMap(f => { const i = idxOf(st, f.x, f.y); return [
        this.add.text(this.px(i) + 5, this.py(i) - 3.5, f.tag, { fontSize: '9px', fontStyle: 'bold', color: f.held ? '#f5c24f' : '#7fd0ff' }).setOrigin(0.5).setDepth(5),
        this.add.text(this.px(i), this.py(i) + 9, f.name, { fontSize: '9px', color: f.held ? '#f5c24f' : '#7fd0ff', backgroundColor: '#0b0e1acc', padding: { x: 2, y: 1 } }).setOrigin(0.5, 0).setDepth(5),
      ]; });
    }

    // hover: a claimable Dark block gets a white ring at its centre, an unclaimable one a red one
    if (!isCampaign(st) && this.hover >= 0 && st.blocks[this.hover].state === DARK) {
      const ok = isCandidate(st, this.hover);
      g.lineStyle(2, ok ? C.hover : C.bad, ok ? 0.95 : 0.6); g.strokeCircle(this.px(this.hover), this.py(this.hover), 6);
    }
    // the block the world view was looking at, for 2.5 s after the toggle: corner brackets on its bounding box
    if (this.focusMark && !st.city?.mapId) {
      const k = (now - this.focusMark.born) / 2500;
      if (k >= 1) this.focusMark = null;
      else {
        const b = this.geom.blocks[this.focusMark.i], x0 = this.ox + b.x0 * this.ppt, y0 = this.oy + b.y0 * this.ppt, x1 = this.ox + (b.x1 + 1) * this.ppt, y1 = this.oy + (b.y1 + 1) * this.ppt, L = 6;
        g.lineStyle(2, C.white, 1 - k);
        g.lineBetween(x0, y0, x0 + L, y0); g.lineBetween(x0, y0, x0, y0 + L); g.lineBetween(x1, y0, x1 - L, y0); g.lineBetween(x1, y0, x1, y0 + L);
        g.lineBetween(x0, y1, x0 + L, y1); g.lineBetween(x0, y1, x0, y1 - L); g.lineBetween(x1, y1, x1 - L, y1); g.lineBetween(x1, y1, x1, y1 - L);
      }
    }

    // pips at each front segment's midpoint: ● green disc, ▲ amber triangle, ✕ red cross that blinks
    const ge = this.gEdges;
    ge.clear();
    const blink = Math.floor(now / 260) % 2 === 0;
    const edges = this.edges();
    for (const e of edges) {
      const p = this.edgePos(e);
      if (e.pip === 'green') { ge.fillStyle(C.green, 1); ge.fillCircle(p.x, p.y, 3.5); ge.lineStyle(1, 0x000000, 0.6); ge.strokeCircle(p.x, p.y, 3.5); }
      else if (e.pip === 'amber') { ge.fillStyle(C.amber, 1); ge.fillTriangle(p.x, p.y - 4.5, p.x + 4.5, p.y + 3.5, p.x - 4.5, p.y + 3.5); ge.lineStyle(1, 0x000000, 0.6); ge.strokeTriangle(p.x, p.y - 4.5, p.x + 4.5, p.y + 3.5, p.x - 4.5, p.y + 3.5); }
      else if (blink) { ge.lineStyle(2.5, C.red, 1); ge.lineBetween(p.x - 4, p.y - 4, p.x + 4, p.y + 4); ge.lineBetween(p.x - 4, p.y + 4, p.x + 4, p.y - 4); }
      if (this.selectedEdge === e.id) { ge.lineStyle(2, C.white, 1); ge.strokeCircle(p.x, p.y, 6.5); }
    }
    if (this.selectedEdge !== null && !edges.some(e => e.id === this.selectedEdge)) { this.selectedEdge = null; this.hooks.onPipSelect(null); this.lastPaint = -1e9; }

    // fx: pole lines, pulses, hulk marks, the engineer
    for (const l of this.poles) {
      g.lineStyle(1, C.pole, 0.55); g.lineBetween(this.px(l.a), this.py(l.a), this.px(l.b), this.py(l.b));
      g.fillStyle(C.pole, 0.8); g.fillCircle(this.px(l.a), this.py(l.a), 1.5); g.fillCircle(this.px(l.b), this.py(l.b), 1.5);
    }
    this.pulses = this.pulses.filter(p => now - p.born < p.dur);
    for (const p of this.pulses) {
      const k = Math.max(0, (now - p.born) / p.dur), r = p.r0 + (p.r1 - p.r0) * k, cx = p.x ?? this.px(p.i), cy = p.y ?? this.py(p.i);
      g.lineStyle(p.width, p.color, 1 - k); g.strokeCircle(cx, cy, r);
      if (p.wake) { g.lineStyle(1, p.color, (1 - k) * 0.5); g.strokeCircle(cx, cy, r * 1.25); }
    }
    this.hulks = this.hulks.filter(h => now - h.born < 1600);
    for (const h of this.hulks) {
      const cx = this.px(h.i), cy = this.py(h.i), a = 1 - (now - h.born) / 1600;
      g.fillStyle(C.hulk, a); g.fillPoints([{ x: cx, y: cy - 6 }, { x: cx + 6, y: cy }, { x: cx, y: cy + 6 }, { x: cx - 6, y: cy }], true);
    }
    // Selected-stop routes use actual track components; no bridge is drawn across a cut.
    if(st.flow){
      const px=(x:number)=>this.ox+x*this.ppt,py=(y:number)=>this.oy+y*this.ppt;
      const route=transportView.stopId===null?null:stationRoute(st,transportView.stopId);
      const fixed=st.campaign?.fixedTram;if(fixed&&!route){g.lineStyle(3,st.city?.mapId?0xefb953:0x779b99,.9);g.beginPath();(st.city?.mapId?riverfrontRail().samples:fixed.route.map(t=>({x:t%st.flow!.tw+.5,y:Math.floor(t/st.flow!.tw)+.5}))).forEach((p,i)=>{if(i===0)g.moveTo(px(p.x),py(p.y));else g.lineTo(px(p.x),py(p.y));});g.strokePath();}
      if(route){g.lineStyle(2,route.interrupted?0xf2b632:0x66dce8,1);for(const path of route.paths){g.beginPath();path.forEach((t,i)=>{const x=px(t%st.flow!.tw+.5),y=py(Math.floor(t/st.flow!.tw)+.5);if(i===0)g.moveTo(x,y);else g.lineTo(x,y);});g.strokePath();}}
      for(const m of st.flow.machines.filter(m=>m.kind==='tramstop')){
        const x=px(m.x+m.size/2),y=py(m.y+m.size/2),status=route?.stops.find(s=>s.machine.id===m.id)?.status??(machineRunning(st,m)?'ready':'unpowered');
        g.lineStyle(m.id===transportView.stopId?2:1,status==='unpowered'?0xff786b:status==='full'?0xf2b632:0x66dce8,1);g.fillStyle(0x101824,1);g.fillCircle(x,y,4);
        if(status==='unpowered'){g.lineBetween(x-4,y-4,x+4,y+4);g.lineBetween(x-4,y+4,x+4,y-4);}else if(status==='full')g.strokeRect(x-4,y-4,8,8);else g.strokeCircle(x,y,4);
        if(m.id===transportView.stopId)g.strokeCircle(x,y,7);
      }
      for(const t of route?.trams??[]){g.fillStyle(t.status==='disconnected'||t.status==='no route'?0xff786b:0xffffff,1);g.fillCircle(px(t.machine.x+.5),py(t.machine.y+.5),2.5);}
      if(st.campaign?.truck){const r=truckRect(st.campaign.truck);g.lineStyle(1,0xf8dc85,1);g.strokeRect(px(r.x),py(r.y),Math.max(3,r.w*this.ppt),Math.max(3,r.h*this.ppt));}
    }
    // D5: the engineer — a white dot (red while knocked down) at its tile, with a ring while walking
    if (st.engineer) {
      const e = st.engineer, ex = this.ox + e.x * this.ppt, ey = this.oy + e.y * this.ppt;
      g.fillStyle(0x081416,.95);g.fillCircle(ex,ey,12);
      g.lineStyle(2,0x67ffcf,.65+.25*throb);g.strokeCircle(ex,ey,13+2*throb);
      g.fillStyle(e.down>=0?C.red:0xffffff,1);g.fillTriangle(ex,ey-10,ex-7,ey+7,ex+7,ey+7);
      g.lineStyle(2,0x173e37,1);g.strokeTriangle(ex,ey-10,ex-7,ey+7,ex+7,ey+7);
      this.playerLabel.setText(e.down>=0?'YOU · DOWN':'YOU').setPosition(Math.max(25,Math.min(this.scale.width-25,ex)),Math.min(this.scale.height-24,ey+18));
    }
  }

  private drawFacility(g: Phaser.GameObjects.Graphics, f: { name: string; x: number; y: number; held: boolean }): void {
    const i = idxOf(this.session.state, f.x, f.y), px = this.px(i) - 12, py = this.py(i) - 12, col = f.held ? C.facilityHeld : C.facility;
    g.fillStyle(col, f.held ? 1 : 0.9);
    switch (f.name) {
      case 'Foundry': g.fillRect(px + 5, py + 12, 14, 7); g.fillRect(px + 15, py + 4, 3, 8); break;
      case 'Arsenal': g.fillRect(px + 5, py + 13, 6, 6); g.fillRect(px + 12, py + 13, 6, 6); g.fillRect(px + 8.5, py + 6, 6, 6); break;
      case 'Turbine hall': g.fillRect(px + 4, py + 12, 16, 7); g.fillCircle(px + 12, py + 12, 6); break;
      case 'Refinery': g.fillCircle(px + 8, py + 13, 4.5); g.fillCircle(px + 16, py + 13, 4.5); g.fillRect(px + 11, py + 6, 2, 8); break;
      case 'Power station': g.fillRect(px + 5, py + 11, 14, 8); g.fillRect(px + 7, py + 4, 3, 8); g.fillRect(px + 14, py + 4, 3, 8); break;
      case 'Tram depot': g.fillRect(px + 4, py + 10, 16, 8); g.fillRect(px + 6, py + 6, 12, 4); break;
      default: g.fillRect(px + 7, py + 7, 10, 10);
    }
  }
}

function lighten(c: number, k: number): number {
  const r = (c >> 16) & 255, g = (c >> 8) & 255, b = c & 255;
  const f = (v: number) => Math.min(255, Math.round(v + (255 - v) * k));
  return (f(r) << 16) | (f(g) << 8) | f(b);
}
