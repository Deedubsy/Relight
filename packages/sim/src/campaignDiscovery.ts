import {citySight} from './ground';
import { SurveyPlacementError } from './campaignSurvey';
/** EX-07: one optional workshop records cache. Tuning and automatic tool fitting are provisional. */
import type { SimState } from './types';
import { ground, inReach } from './ground';
import { passable, findPath } from './walk';
import { newStalkerLayer, spawnStalker, tickStalkers, type StalkerLayer } from './stalker';
import { threatOf, CONTACT_R } from './threat';
import type { StalkerCandidate } from './candidates';

export const DISCOVERY = { repairMultiplier: 0.5, clueRadius: 24, cacheClearance: 3,
  guardian: { perception: 5, leash: 8, hpMul: 2, speedMul: 1.2, windupS: 1.2, attackS: 1.2,
    attackHp: 5, investigateS: 6, lostS: 2, hysteresis: 2, respawnS: 0 } satisfies StalkerCandidate } as const;
export interface DiscoveryState {
  version: 1; id: string; block: number; x: number; y: number;
  seenAt: number; recoveredAt: number; guardian: StalkerLayer;
}
export const discoveryId = (seed: number): string => `${seed}:workshop-field-repair-v1`;

/** A side yard of the workshop, clear of essential installations and the surveyed trunk. */
export function initDiscovery(st: SimState): void {
  const c = st.campaign, d = c?.districts, e = c?.expansion;
  if (!c || !d || !e || c.discovery || !st.flow) return;
  const G = ground(st), w = d.workshop;
  const essential = [w, d.station, e.station, e.radio, ...d.sources,
    ...[...e.stops, d.stop].map(([x,y]) => ({ x, y, size: 2 })),
    ...[...e.route, ...d.route].map(t => ({ x:t%G.tw, y:Math.floor(t/G.tw), size:1 }))];
  const clearance = (x: number, y: number) => essential.every(s => Math.hypot(
    Math.max(s.x-x, 0, x-(s.x+s.size-1)), Math.max(s.y-y, 0, y-(s.y+s.size-1))) > DISCOVERY.guardian.leash+2);
  const tiles = Array.from(G.blocks[w.block].tiles).filter(t => {
    const x=t%G.tw,y=Math.floor(t/G.tw);
    return clearance(x,y) && st.flow!.occ[t] === undefined && passable(st,x,y)
      && (c.defence?.sites??[]).every(s=>Math.hypot(x-s.tile%G.tw,y-Math.floor(s.tile/G.tw))>12);
  }).sort((a,b) => Math.hypot(a%G.tw-w.x,Math.floor(a/G.tw)-w.y)-Math.hypot(b%G.tw-w.x,Math.floor(b/G.tw)-w.y) || a-b);
  const t = tiles.find(t => findPath(st,Math.floor(st.engineer.x),Math.floor(st.engineer.y),t%G.tw,Math.floor(t/G.tw)) !== null);
  if (t === undefined) throw new SurveyPlacementError('workshop discovery requires a reachable optional side yard');
  const guardian = newStalkerLayer(st, DISCOVERY.guardian); guardian.sites = {};
  guardian.sites[w.block] = { restored:false, diedAt:-1, spawned:0 };
  c.discovery = { version:1, id:discoveryId(st.seed), block:w.block, x:t%G.tw, y:Math.floor(t/G.tw), seenAt:-1, recoveredAt:-1, guardian };
  c.version = 5;
  fieldGuardian(st);
}
function fieldGuardian(st: SimState): void {
  const d=st.campaign!.discovery!, S=d.guardian;
  // One lifetime only; an old preview loaded beside the cache waits until the engineer leaves.
  if (d.recoveredAt<0 && S.sites[d.block].spawned===0 && passable(st,d.x,d.y))
    spawnStalker(st,S,d.block,[d.x+.5,d.y+.5]);
}
export function tickDiscovery(st: SimState, dt: number): void {
  const d=st.campaign?.discovery; if (!d) return;
  if (d.seenAt<0 && citySight(st,st.engineer.x,st.engineer.y,d.x+.5,d.y+.5) && Math.hypot(st.engineer.x-d.x-.5,st.engineer.y-d.y-.5)<=DISCOVERY.clueRadius) d.seenAt=st.t;
  fieldGuardian(st);
  tickStalkers(st,threatOf(st.flow!),dt,CONTACT_R,d.guardian,true);
}
export function discoveryAt(st: SimState, x: number, y: number): boolean {
  const d=st.campaign?.discovery; return !!d && x===d.x && y===d.y;
}
export function discoveryCheck(st: SimState, id: string): string {
  const d=st.campaign?.discovery;
  if (!d || d.id!==id) return 'unknown workshop record';
  if (d.recoveredAt>=0) return 'Field-repair tool already fitted. This records cache is empty.';
  if (st.engineer.down>=0 || !inReach(st,d.x,d.y,1)) return 'Walk closer to the workshop records cache.';
  if (d.guardian.stalkers.some(s=>Math.hypot(s.x-d.x-.5,s.y-d.y-.5)<=DISCOVERY.cacheClearance))
    return 'Stalker guarding the records: draw it away or use your rifle or a supplied turret.';
  return '';
}
export function recoverSchematic(st: SimState, id: string): string {
  const why=discoveryCheck(st,id); if (why) return why;
  const d=st.campaign!.discovery!; d.recoveredAt=st.t; d.seenAt=st.t;
  st.campaign!.defence!.notice='Field-repair tool fitted: new manual repairs take half the time, with the same steel and copper cost.';
  return '';
}
export function discoveryDescription(st: SimState): string {
  const d=st.campaign?.discovery; if (!d) return '';
  if (d.recoveredAt>=0) return 'Field-repair tool fitted · 40 HP in 2s for 2 steel + 1 copper; disabled core recovery 6s for 10 steel + 5 copper. Materials still required. Workshop records empty.';
  return `Optional workshop records (${d.x},${d.y}) · field-repair schematic, halves manual repair time; same materials. E to recover. ${d.guardian.stats.kills ? 'Guardian defeated; it will not return.' : 'A Stalker roams this yard and notices you within 5 tiles. Yellow rings warn of strikes. It chases beyond the yard; put more than 20 tiles between you and it to escape, or prepare a supplied turret.'} The station, source pads and workshop service remain accessible without this detour.`;
}
