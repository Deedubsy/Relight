/** EX-05: paid, recoverable defences. All tuning here is provisional, separate from legacy balance. */
import type { SimState } from './types';
import { HELD } from './types';
import { ground, inReach, blockOfTile } from './ground';
import { machineAt, type Machine } from './flow';
import { drop } from './engineer';
import { isCampaign, CAMPAIGN_RULES } from './rules';
import { blockName } from './names';

export const DEFENCE = { wallHp: 120, turretHp: 100, coreHp: 300, repairHp: 40, repairSeconds: 4,
  repairSteel: 2, repairCopper: 1, coreSteel: 10, coreCopper: 5, coreRepairSeconds: 12 } as const;
export interface BaseCore { block: number; x: number; y: number; size: number; hp: number; commissionedAt: number }
export interface Assault { id: number; block: number; dawn: number; startsAt: number; origin: number; remaining: number; nextSpawn: number; retreat: boolean }
export interface MinorRaid { id: number; block: number; origin: number; retreat: boolean }
export interface RadioMessage { assault: number; block: number; startsAt: number; receivedAt: number; approach?: string; composition?: string }
export interface DefenceState {
  version: 1; bases: BaseCore[]; nextId: number; nextDawn: number; lastMajorEnd: number;
  lastMinorSlot: number; nominations: { block: number; at: number }[]; major: Assault | null; minor: MinorRaid | null;
  radioUpgrade: boolean; warning: RadioMessage | null;
  repair: { kind: 'machine' | 'core'; id: number; remaining: number; recommission: boolean } | null;
  sites: { id: number; block: number; tile: number; spawned: boolean }[];
  history: { id: number; block: number; started: number; ended: number; defeated: boolean; spawned: number }[];
  raidsStarted: number; majorSpawned: number; notice: string;
}
export function initDefence(st: SimState): void {
  if (!isCampaign(st) || st.campaign!.defence || !st.flow) return;
  const G = ground(st), home = st.campaign!.homeBlock, depot = st.flow.machines.find(m => m.kind === 'depot')!;
  const bases: BaseCore[] = [{ block: home, x: depot.x, y: depot.y, size: depot.size, hp: DEFENCE.coreHp, commissionedAt: 0 }];
  const station = st.campaign!.expansion?.station;
  if (station && station.restoredAt >= 0) bases.push({ ...station, hp: DEFENCE.coreHp, commissionedAt: station.restoredAt });
  const sites: DefenceState['sites'] = [];
  // A small deterministic set of optional ruin guards, away from the opening and essential installations.
  for (const p of G.urban?.places ?? []) {
    if (sites.length >= 3) break;
    if (p.block === home || p.block === station?.block || Math.hypot(p.pad.x-depot.x,p.pad.y-depot.y) < 45) continue;
    const x = p.pad.x, y = p.pad.y, tile = y*G.tw+x;
    if (!G.urban!.solid[tile] && G.owner[tile] !== -2 && st.flow.occ[tile] === undefined) sites.push({ id: sites.length+1, block: p.block, tile, spawned: false });
  }
  // Old preview saves receive two full cycles of preparation; missed events are never replayed on upgrade.
  st.campaign!.defence = { version: 1, bases, nextId: 1,
    nextDawn: st.t === 0 ? (CAMPAIGN_RULES.firstAssaultNight-1)*1200 : Math.ceil((st.t+2400)/1200)*1200,
    lastMajorEnd: -1, lastMinorSlot: Math.floor(st.t/1200)*2+(st.t%1200>=600?1:st.t%1200>=300?0:-1), nominations: [], major: null, minor: null,
    radioUpgrade: false, warning: null, repair: null, sites, history: [], raidsStarted: 0, majorSpawned: 0, notice: '' };
  st.campaign!.version = 3;
}
export function baseCore(st: SimState, block: number): BaseCore | undefined { return st.campaign?.defence?.bases.find(b => b.block === block); }
export function coreAt(st: SimState, x: number, y: number): BaseCore | undefined {
  return st.campaign?.defence?.bases.find(b => x>=b.x && x<b.x+b.size && y>=b.y && y<b.y+b.size);
}
export function registerBase(st: SimState, block: number): void {
  const d = st.campaign?.defence, site = st.campaign?.expansion?.station;
  if (!d || !site || baseCore(st, block)) return;
  d.bases.push({ block, x: site.x, y: site.y, size: site.size, hp: DEFENCE.coreHp, commissionedAt: st.t });
  nominateBase(st, block);
}
export function nominateBase(st: SimState, block: number): void {
  const d = st.campaign?.defence; if (!d) return;
  d.nominations = d.nominations.filter(n => n.block !== block); d.nominations.push({ block, at: st.t });
}
export function defenceMax(m: Machine): number { return m.kind === 'wall' ? DEFENCE.wallHp : m.kind === 'turret' ? DEFENCE.turretHp : 0; }
export function defenceHp(m: Machine): number { return m.hp ?? defenceMax(m); }
export function damageDefence(st: SimState, m: Machine, amount: number): void {
  if (!isCampaign(st) || !defenceMax(m) || amount<=0) return;
  const was = defenceHp(m); m.hp = Math.max(0, was-amount);
  if (was>0 && m.hp===0) st.flow!.rev++;
}
export function damageCore(st: SimState, core: BaseCore, amount: number): void {
  if (core.hp<=0 || amount<=0) return;
  core.hp = Math.max(0,core.hp-amount);
  if (core.hp===0) {
    st.blocks[core.block].subOn = false; st.flow!.rev++;
    const d = st.campaign!.defence!;
    if (d.major?.block===core.block) { d.major.retreat=true; d.major.remaining=0; }
    if (d.minor?.block===core.block) d.minor.retreat=true;
    d.notice = `${blockName(st,core.block)} core disabled. Layout and supplies remain; repair the core when the attackers leave.`;
  }
}
export function coreDisabledAt(st: SimState, x: number, y: number): boolean { return baseCore(st,blockOfTile(st,x,y))?.hp===0; }
export function repairCheck(st: SimState, x: number, y: number): string {
  const d=st.campaign?.defence; if (!d) return 'defence repair is unavailable';
  if (d.repair) return 'a repair is already in progress';
  const core=coreAt(st,x,y), m=machineAt(st,x,y), max=core?DEFENCE.coreHp:m?defenceMax(m):0, hp=core?.hp??(m?defenceHp(m):0);
  if (!max || hp>=max) return 'nothing damaged here';
  const target=core??m!;
  if (st.engineer.down>=0 || !inReach(st,target.x,target.y,target.size)) return 'walk closer to repair';
  if (core?.hp===0 && (d.major?.block===core.block || d.minor?.block===core.block)) return 'wait for the attackers to leave';
  const steel=core?.hp===0?DEFENCE.coreSteel:DEFENCE.repairSteel, copper=core?.hp===0?DEFENCE.coreCopper:DEFENCE.repairCopper;
  return (st.engineer.inv.steel??0)<steel || (st.engineer.inv.copper??0)<copper ? `repair needs ${steel} steel and ${copper} copper in your pockets` : '';
}
export function startRepair(st: SimState,x:number,y:number):string {
  const why=repairCheck(st,x,y); if(why)return why;
  const core=coreAt(st,x,y), m=machineAt(st,x,y), disabled=core?.hp===0;
  const steel=disabled?DEFENCE.coreSteel:DEFENCE.repairSteel, copper=disabled?DEFENCE.coreCopper:DEFENCE.repairCopper;
  drop(st.engineer,'steel',steel);drop(st.engineer,'copper',copper);
  st.stats.spentSteel=(st.stats.spentSteel??0)+steel;st.stats.spentCopper=(st.stats.spentCopper??0)+copper;
  st.campaign!.defence!.repair={kind:core?'core':'machine',id:core?.block??m!.id,remaining:disabled?DEFENCE.coreRepairSeconds:DEFENCE.repairSeconds,recommission:disabled};
  return '';
}
export function tickRepair(st:SimState,dt:number):void {
  const d=st.campaign?.defence,r=d?.repair;if(!d||!r)return;
  const target=r.kind==='core'?baseCore(st,r.id):st.flow!.machines.find(m=>m.id===r.id);
  if(!target){d.repair=null;return;}
  if(st.engineer.down>=0 || !inReach(st,target.x,target.y,target.size))return; // paid progress pauses; no extra charge on resuming
  if(r.kind==='core' && target.hp===0 && (d.major?.block===r.id || d.minor?.block===r.id))return;
  r.remaining=Math.max(0,r.remaining-dt);if(r.remaining>1e-8)return;
  if(r.kind==='core') {
    const core=target as BaseCore, wasDisabled=core.hp===0;
    if(wasDisabled&&!r.recommission){d.repair=null;d.notice='Core knocked out during repair; a full recovery kit is required.';return;}
    core.hp=wasDisabled?DEFENCE.coreHp:Math.min(DEFENCE.coreHp,core.hp+DEFENCE.repairHp);
    st.blocks[core.block].subOn=true;st.blocks[core.block].state=HELD;
    if(wasDisabled)nominateBase(st,core.block);
  } else { const m=target as Machine; m.hp=Math.min(defenceMax(m),defenceHp(m)+DEFENCE.repairHp); }
  st.flow!.rev++;d.repair=null;
}
export function defenceDescription(st:SimState,x:number,y:number):string {
  const core=coreAt(st,x,y),m=machineAt(st,x,y),max=core?DEFENCE.coreHp:m?defenceMax(m):0;
  if(!max || !isCampaign(st))return '';
  const hp=core?.hp??defenceHp(m!);
  const r=st.campaign?.defence?.repair;
  const repairing=r && (core?r.kind==='core'&&r.id===core.block:r.kind==='machine'&&r.id===m!.id);
  return `${core?'Base core':m!.kind==='wall'?'Wall':'Gun turret'} · ${Math.ceil(hp)}/${max} HP${hp===0?' · DISABLED':''}${repairing?` · repair ${Math.ceil(r.remaining)}s (stay in reach)`:hp<max?` · E repairs: ${core&&hp===0?'10 steel + 5 copper, 12s':'2 steel + 1 copper, 4s / 40 HP'}`:''}`;
}
