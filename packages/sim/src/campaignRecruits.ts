import {citySight} from './ground';
import { SurveyPlacementError } from './campaignSurvey';
/** P7-01: saved campaign recruits. Existing restoration/cache records keep owning their rewards. */
import { type SimState, HELD, DARK } from './types';
import { idxOf } from './sim';
import { ground, inReach } from './ground';
import { passable } from './walk';
import { isCampaign } from './rules';

export const RECRUITS = { gunsmith:{name:'Gunsmith',reward:'Paid +25% gun turret hopper upgrade',clueRadius:24},railcrew:{name:'Rail crew',reward:'Paid +25% freight capacity upgrade',clueRadius:24}, foreman: {name:'Foreman',reward:'blueprint clipboard and copy/paste',clueRadius:24}, lamplighters: {name:'Lamplighters',reward:'Arc lamp',clueRadius:24}, surveyors: {name:'Surveyors',reward:'permanent district-type map',clueRadius:24}, electricians: { name: 'Electricians', reward: 'Floodlight and Big pole', clueRadius: 24 }, concrete: {name:'Concrete crew',reward:'Mixer, Concrete and Barricade',clueRadius:24} } as const;
export type RecruitKind = keyof typeof RECRUITS;
export interface RecruitSite {
  id: string; kind: RecruitKind; block: number; x: number; y: number;
  seenAt: number; recruitedAt: number; inherited: boolean;
}
export interface RecruitState { version: 1 | 2 | 3 | 4 | 5; sites: RecruitSite[] }
export const recruitId = (seed: number, kind: RecruitKind): string => `${seed}:recruit:${kind}:v1`;

/** Old earned unlocks are imported once, including a formerly Held/fallen Electricians block. */
function newRecruit(st: SimState, kind: RecruitKind): RecruitSite {
  const c = st.campaign;
  if(!c||!st.flow)throw new Error('recruit placement requires a campaign');
  const G = ground(st), original = st.survivors.find(s => s.name === 'Electricians');
  const bi = original ? idxOf(st, original.x, original.y) : -1;
  const inherited = kind==='electricians' && bi >= 0 && (st.blocks[bi].state === HELD || !!st.fallen[bi]);
  // Static geometry defines the location even when loading an older, heavily built save.
  // An existing machine is never moved or destroyed; local reach allows interaction beside it.
  const bare = { ...st, campaign: { ...c, truck: undefined }, flow: { ...st.flow, occ: {}, machines: [] } } as SimState;
  const gate = G.opening!.gate, start = gate[Math.floor(gate.length / 2)];
  const cache = c.discovery!, d = c.districts!, e = c.expansion!;
  const installations = [d.workshop, d.station, e.station, e.radio, ...d.sources, ...(kind==='foreman'&&c.turbine?[c.turbine]:[])];
  const route = new Set([...d.route, ...e.route]);
  const safe = (t: number): boolean => {
    const x = t % G.tw, y = Math.floor(t / G.tw);
    return passable(bare, x, y) && Math.hypot(x-cache.x,y-cache.y)>12
      && (c.defence?.sites ?? []).every(s => Math.hypot(x-s.tile%G.tw,y-Math.floor(s.tile/G.tw))>12);
  };
  const queue = [start], seen = new Set(queue);
  let chosen = -1;
  for (let head = 0; head < queue.length; head++) {
    const t = queue[head], x = t % G.tw, y = Math.floor(t / G.tw), block = G.owner[t];
    if (block >= 0 && block !== c.homeBlock && [HELD,DARK].includes(st.blocks[block].state)
      && (c.recruits?.sites??[]).every(s=>Math.hypot(x-s.x,y-s.y)>24)
      && !route.has(t) && !G.patch[t] && G.rank[t]<0 && safe(t)
      && installations.every(s => Math.hypot(x-s.x,y-s.y)>s.size+4)
      && [...e.stops,d.stop].every(([sx,sy])=>Math.hypot(x-sx,y-sy)>5)) { chosen = t; break; }
    for (const [xx,yy] of [[x,y-1],[x+1,y],[x,y+1],[x-1,y]]) {
      const q = yy*G.tw+xx;
      if(xx<0||yy<0||xx>=G.tw||yy>=G.th||seen.has(q)||!safe(q))continue;
      seen.add(q);queue.push(q);
    }
  }
  if (chosen < 0) throw new SurveyPlacementError(`campaign requires an accessible ${RECRUITS[kind].name} shelter`);
  return { id: recruitId(st.seed,kind), kind,
    block:G.owner[chosen], x:chosen%G.tw, y:Math.floor(chosen/G.tw),
    seenAt:inherited?st.t:-1, recruitedAt:inherited?st.t:-1, inherited };
}
export function initRecruits(st:SimState):void {
  const c=st.campaign;if(!isCampaign(st)||!c||!st.flow||(c.recruits?.version??0)>=3)return;
  c.recruits??={version:1,sites:[]};
  for(const kind of ['electricians','concrete','lamplighters','surveyors'] as RecruitKind[])if(!c.recruits.sites.some(s=>s.kind===kind))c.recruits.sites.push(newRecruit(st,kind));
  c.recruits.version=3;
}
/** Add the new shelter after the existing Turbine has its stable location. */
export function initForeman(st:SimState):void {
  const r=st.campaign?.recruits;if(!r||r.version===5)return;
  for(const kind of ['foreman','gunsmith','railcrew'] as RecruitKind[])if(!r.sites.some(s=>s.kind===kind))r.sites.push(newRecruit(st,kind));r.version=5;
}
export function surveyedDistrict(st:SimState, block:number): SimState['blocks'][number]['name'] | null {
  return campaignRecruited(st,'surveyors') && st.blocks[block] ? st.blocks[block].name : null;
}
export function recruitAt(st: SimState, x: number, y: number): RecruitSite | undefined {
  return st.campaign?.recruits?.sites.find(s => s.x===x && s.y===y);
}
export function campaignRecruited(st: SimState, kind: RecruitKind): boolean {
  return !!st.campaign?.recruits?.sites.some(s => s.kind===kind && s.recruitedAt>=0);
}
export function tickRecruits(st: SimState): void {
  for (const s of st.campaign?.recruits?.sites ?? [])
    if(s.seenAt<0 && citySight(st,st.engineer.x,st.engineer.y,s.x+.5,s.y+.5) && Math.hypot(st.engineer.x-s.x-.5,st.engineer.y-s.y-.5)<=RECRUITS[s.kind].clueRadius)s.seenAt=st.t;
}
export function recruitCheck(st: SimState, id: string): string {
  const s=st.campaign?.recruits?.sites.find(s=>s.id===id);
  if(!s)return 'Unknown survivor shelter.';
  if(s.recruitedAt>=0)return `${RECRUITS[s.kind].name} already recruited; their plans are yours permanently.`;
  if(st.engineer.down>=0 || st.engineer.truckSeat || !inReach(st,s.x,s.y,1))return `Walk closer to the shelter to recruit the ${RECRUITS[s.kind].name}.`;
  return '';
}
export function recruitSurvivors(st: SimState, id: string): string {
  const why=recruitCheck(st,id);if(why)return why;
  const s=st.campaign!.recruits!.sites.find(s=>s.id===id)!;
  s.seenAt=st.t;s.recruitedAt=st.t;
  if(s.kind==='foreman'){st.campaign!.defence!.notice='Foreman recruited: Ctrl+C selects a layout; Ctrl+V previews paid construction. R rotates, H/V mirrors; Esc cancels. Clipboard controls are in the side panel.';return '';}
  if(s.kind==='surveyors'){st.campaign!.defence!.notice='Surveyors recruited: district types visible on the map (M). Discoveries and radio intelligence remain separate.';return '';}
  st.campaign!.defence!.notice=`${RECRUITS[s.kind].name} recruited: ${RECRUITS[s.kind].reward} plans available. Build with carried materials; production needs real power.`;
  return '';
}
export function recruitDescription(_st: SimState, s: RecruitSite): string {
  if(s.kind==='foreman')return s.recruitedAt>=0?'Foreman recruited · Ctrl+C copies complete machines and settings; Ctrl+V previews a paid stamp. R rotates; H/V mirrors; Esc cancels. Materials come from your pockets.':'Foreman shelter · E to recruit in person for the blueprint clipboard and copy/paste.';
  if(s.kind==='lamplighters')return s.recruitedAt>=0?'Lamplighters recruited · Arc lamp: radius 6, 12 kW, 4 steel + 4 copper. Build from the construction menu.':'Lamplighters shelter · E to recruit in person for wider powered lighting.';
  if(s.kind==='surveyors')return s.recruitedAt>=0?'Surveyors recruited · M opens the permanent district-type map. Hidden discoveries and attack intelligence still require exploration and radio.':'Surveyors shelter · E to recruit in person for a permanent district-type map.';
  if(s.kind==='concrete')return s.recruitedAt>=0?'Concrete crew recruited · Mixer: 2 stone → 1 concrete every 2s at 60 kW. Barricade: 2 steel + 4 concrete, 240 HP.':'Concrete crew shelter · E to recruit in person. Unlocks the Mixer and stronger concrete Barricades.';
  if(s.kind==='gunsmith'||s.kind==='railcrew')return `${RECRUITS[s.kind].name} · ${RECRUITS[s.kind].reward}. Existing baseline handling stays available.`;
  return s.recruitedAt>=0 ? 'Electricians recruited · Floodlight and Big pole plans retained. Build with carried materials; lights need connected power.'
    : 'Electricians shelter · E to recruit in person. Unlocks Floodlight and Big pole plans; no block claim or restoration kit required.';
}
export function recruitsProblem(st: SimState): string {
  const c=st.campaign,r=c?.recruits;
  if((c?.version??0)<6)return r?'recruits require campaign metadata version 6':'';
  const current=(c?.version??0)>=7, optional=(c?.version??0)>=8, foreman=(c?.version??0)>=10;
  if(!r || (r.version!==5&&r.version!==(foreman?4:optional?3:current?2:1)) || !Array.isArray(r.sites) || r.sites.length!==(r.version===5?7:foreman?5:optional?4:current?2:1) || new Set(r.sites.map(s=>s?.kind)).size!==r.sites.length)return 'invalid campaign recruits';
  if(new Set(r.sites.map(s=>`${s.x}:${s.y}`)).size!==r.sites.length)return 'overlapping campaign recruits';
  for(const s of r.sites) {
    if(!s || !((r.version===5&&(s.kind==='gunsmith'||s.kind==='railcrew'))||s.kind==='electricians'||(current&&s.kind==='concrete')||(optional&&(s.kind==='lamplighters'||s.kind==='surveyors'))||(foreman&&s.kind==='foreman')) || s.id!==recruitId(st.seed,s.kind) || !Number.isInteger(s.block) || !st.blocks[s.block]
      || !Number.isInteger(s.x) || !Number.isInteger(s.y) || s.x<0 || s.y<0 || s.x>=st.flow!.tw || s.y>=st.city!.th
      || ground(st).owner[s.y*st.flow!.tw+s.x]!==s.block || typeof s.inherited!=='boolean'
      || ![s.seenAt,s.recruitedAt].every(t=>Number.isFinite(t)&&(t===-1||(t>=0&&t<=st.t)))
      || (s.recruitedAt>=0&&(s.seenAt<0||s.seenAt>s.recruitedAt)) || (s.inherited&&(s.recruitedAt<0||s.kind!=='electricians')))return 'invalid campaign recruit record';
  }
  return '';
}
