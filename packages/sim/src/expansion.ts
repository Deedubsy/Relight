/** The second-area restoration: real deliveries, local power and a once-only transport crate. */
import { SimState, HELD } from './types';
import { ground, inReach, blockOfTile } from './ground';
import { take, drop } from './engineer';
import { isCampaign, CampaignSite } from './rules';
import { registerBase } from './campaignDefence';
import { campaignWarning, restorationWindow } from './campaignThreat';
import { campaignThrottle } from './campaignPower';
import { workshopStatus } from './campaignDistricts';
export const EXPANSION = { station: { steel: 30, copper: 15 }, radio: { steel: 20, copper: 10 }, northStation: {steel:40,copper:20}, workshop:{steel:25,copper:15} } as const;
export type SiteId = 'station' | 'radio' | 'northStation' | 'workshop';
export const SITE_IDS: readonly SiteId[] = ['station','radio','northStation','workshop'];
export const SITE_LABELS: Record<SiteId,string> = {station:'first tram station',radio:'radio tower',northStation:'later tram station',workshop:'repair workshop'};
export function initExpansion(st: SimState): void {
  if (!isCampaign(st) || st.campaign!.expansion) return;
  const G = ground(st), bi = G.railYard, sub = G.blocks[bi]?.sub;
  if (!sub) throw new Error('campaign station requires its installation');
  const n = G.base.length, tw = G.tw;
  const clear = (t: number) => t >= 0 && t < n && G.owner[t] !== -2 && !G.urban!.solid[t] && st.flow?.occ[t] === undefined && !G.patch[t];
  const street = (t: number) => clear(t) && G.owner[t] === -1;
  const rect = (x: number, y: number, size: number) => {
    for (let yy = y; yy < y + size; yy++) for (let xx = x; xx < x + size; xx++) {
      const t = yy * tw + xx; if (xx < 0 || xx >= tw || yy < 0 || yy >= G.th || !clear(t)) return false;
      const b = G.blocks[blockOfTile(st, xx, yy)], s = b?.sub;
      if (s && xx >= s.x && xx < s.x + s.size && yy >= s.y && yy < s.y + s.size) return false;
    } return true;
  };
  const gate = G.opening!.gate, centre = gate[Math.floor(gate.length / 2)];
  const starts: number[] = [];
  for (let t = 0; t < n; t++) if (street(t) && G.near[t] === st.campaign!.homeBlock) starts.push(t);
  starts.sort((a,b) => Math.hypot(a % tw - centre % tw, Math.floor(a/tw) - Math.floor(centre/tw)) - Math.hypot(b % tw - centre % tw, Math.floor(b/tw) - Math.floor(centre/tw)) || a-b);
  const start = starts[0]; if (start === undefined) throw new Error('no home street for the tram');
  const prev = new Int32Array(n); prev.fill(-1); prev[start] = start;
  const queue = new Int32Array(n); queue[0] = start; let end = -1, tail = 1;
  for (let head = 0; head < tail; head++) {
    const t = queue[head], x = t % tw, y = Math.floor(t / tw);
    if (G.near[t] === bi && Math.hypot(x-sub.x,y-sub.y) < 24) { end = t; break; }
    for (const [xx,yy] of [[x,y-1],[x+1,y],[x,y+1],[x-1,y]]) { const q=yy*tw+xx; if(xx<0||xx>=tw||yy<0||yy>=G.th||prev[q]!==-1||!street(q))continue;prev[q]=t;queue[tail++]=q; }
  }
  if (end < 0) throw new Error('no street route to the second-area station');
  const route = [end]; while(route[route.length-1]!==start)route.push(prev[route[route.length-1]]);route.reverse();
  const path = new Set(route), stops: [number,number][] = [];
  for (const [t, owner] of [[start,st.campaign!.homeBlock],[end,bi]]) {
    const x=t%tw,y=Math.floor(t/tw); let stop: [number,number]|undefined;
    for(const [xx,yy] of [[x-2,y],[x+1,y],[x,y-2],[x,y+1],[x-2,y-1],[x+1,y-1],[x-1,y-2],[x-1,y+1]]) {
      if (blockOfTile(st,xx,yy)!==owner || !rect(xx,yy,2))continue;
      if([yy*tw+xx,yy*tw+xx+1,(yy+1)*tw+xx,(yy+1)*tw+xx+1].some(q=>path.has(q)))continue;
      stop=[xx,yy];break;
    }
    if(!stop)throw new Error('campaign route has no clear stop pad');stops.push(stop);
  }
  let radio: [number,number]|undefined;
  const pad=G.urban!.places.find(p=>p.block===bi)!.pad;
  for(let y=pad.y;y<pad.y+pad.h&&!radio;y++)for(let x=pad.x;x<pad.x+pad.w;x++)if(rect(x,y,1)&&Math.hypot(x-sub.x,y-sub.y)>=6&&blockOfTile(st,x,y)===bi){radio=[x,y];break;}
  if(!radio)throw new Error('no radio restoration site');
  const site=(x:number,y:number,size:number):CampaignSite=>({block:bi,x,y,size,delivered:{steel:0,copper:0},restoredAt:-1});
  st.campaign!.version=2;
  st.campaign!.expansion={station:site(sub.x,sub.y,sub.size),radio:site(radio[0],radio[1],1),route,stops,reward:{track:0,tramstop:0,tram:0},grantedAt:-1};
}
export function campaignSite(st: SimState,id: SiteId):CampaignSite|undefined { return id==='northStation'?st.campaign?.districts?.station:id==='workshop'?st.campaign?.districts?.workshop:st.campaign?.expansion?.[id]; }
export function campaignSiteAt(st: SimState,x:number,y:number):SiteId|null {
  for(const id of SITE_IDS){const s=campaignSite(st,id);if(s && x>=s.x&&x<s.x+s.size&&y>=s.y&&y<s.y+s.size)return id;}return null;
}
export function siteCheck(st:SimState,id:SiteId):string {
  const s=campaignSite(st,id); if(!s)return 'no restoration site';
  if(id!=='station'&&campaignSite(st,'station')!.restoredAt<0)return 'restore the first station first';
  if(id==='workshop'&&campaignSite(st,'northStation')!.restoredAt<0)return 'restore this district station first';
  if(s.restoredAt>=0)return 'already restored';
  if(st.engineer.down>=0||!inReach(st,s.x,s.y,s.size))return 'walk closer to the installation';
  const need=EXPANSION[id];if(s.delivered.steel<need.steel||s.delivered.copper<need.copper)return 'deliver the missing materials';
  if(campaignThrottle(st,s.block)<=0)return 'needs a fueled local generator or poles connected to a powered substation';
  return '';
}
export function deliverSite(st:SimState,id:SiteId):void {
  const s=campaignSite(st,id);if(!s||s.restoredAt>=0||st.engineer.down>=0||!inReach(st,s.x,s.y,s.size)||(id!=='station'&&campaignSite(st,'station')!.restoredAt<0)||(id==='workshop'&&campaignSite(st,'northStation')!.restoredAt<0))return;
  for(const item of ['steel','copper'] as const)s.delivered[item]+=drop(st.engineer,item,Math.max(0,EXPANSION[id][item]-s.delivered[item]));
}
export function restoreSite(st:SimState,id:SiteId):string {
  const why=siteCheck(st,id);if(why)return why;
  const e=st.campaign!.expansion!,s=campaignSite(st,id)!;
  st.stats.spentSteel=(st.stats.spentSteel??0)+s.delivered.steel;st.stats.spentCopper=(st.stats.spentCopper??0)+s.delivered.copper;
  s.delivered={steel:0,copper:0};s.restoredAt=st.t;st.flow!.rev++;
  if(id==='station'||id==='northStation') { const b=st.blocks[s.block];b.state=HELD;b.subOn=true;b.d=0;registerBase(st,s.block);
    if(id==='station'&&e.grantedAt<0){e.grantedAt=st.t;e.reward={track:e.route.length,tramstop:2,tram:1};}
  }
  return '';
}
export function collectTramKit(st:SimState):number {
  const e=st.campaign?.expansion;if(!e||e.grantedAt<0||!inReach(st,e.station.x,e.station.y,e.station.size)||st.engineer.down>=0)return 0;
  let total=0;for(const item of ['tram','tramstop','track'] as const){const n=take(st.engineer,item,e.reward[item]);e.reward[item]-=n;total+=n;}return total;
}
export function describeSite(st:SimState,id:SiteId):string {
  const s=campaignSite(st,id);if(!s)return '';
  if(id==='workshop'&&s.restoredAt>=0)return workshopStatus(st);
  if(id==='northStation'&&s.restoredAt>=0)return 'Later station restored. Extend the trunk along the survey, build and power a third stop, and supply it from home. Copper extraction and workshop service keep this district productive.';
  if(s.restoredAt>=0){if(id==='station'){const r=st.campaign!.expansion!.reward;return `Station restored. Transport unlocked. Kit remaining: ${r.tram} tram, ${r.tramstop} stops, ${r.track} track. E collects what fits.`;}
    return campaignThrottle(st,s.block)>0?`Radio powered. ${campaignWarning(st)}`:'Radio restored but without power. Its warning service is offline.';}
  return `${id==='station'?'Tram station':id==='radio'?'Radio tower':id==='northStation'?'Later tram station':'Repair workshop'}: ${s.delivered.steel}/${EXPANSION[id].steel} steel, ${s.delivered.copper}/${EXPANSION[id].copper} copper delivered. E delivers and restores when powered. ${siteCheck(st,id)} ${id==='station'||id==='northStation'?restorationWindow(st):''}`;
}
