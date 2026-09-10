import {doorOutside} from './city/parcelGeometry';
import {RIVERFRONT_BUILDINGS} from './city/riverfront';
/** P9-03: saved annotations and visits; camera/selection stay in the renderer. */
import type {SimState} from './types';
import {isCampaign} from './rules';
import {ground} from './ground';
import {blockName} from './names';
import {campaignDiscoveries,knownSite} from './campaignGuide';
import {campaignSite,SITE_IDS} from './expansion';
import {campaignRecruited} from './campaignRecruits';
import type {ActionResult} from './construction';
export const NAVIGATION_RULES={version:1,nameLength:64,names:80,pins:32} as const;
export interface Navigation {version:1;names:Record<string,string>;pins:{id:number;x:number;y:number;name:string}[];nextId:number;visitedBlocks:number[];visitedSites:string[]}
export type NavigationAction={type:'rename';id:string;name:string}|{type:'pin';x:number;y:number;name:string}|{type:'removePin';id:number};
export interface NavigationTarget {id:string;name:string;defaultName:string;x:number;y:number;visited:boolean;kind:'district'|'place'|'pin'}
export function navigationName(st:SimState,id:string,fallback:string):string{return st.campaign?.navigation?.names[id]??fallback;}
export function initNavigation(st:SimState):void {
 if(isCampaign(st)&&!st.campaign!.navigation)st.campaign!.navigation={version:1,names:{},pins:[],nextId:1,visitedBlocks:[st.campaign!.homeBlock],visitedSites:[]};
}
/** Lightweight knowledge positions: avoid service/power descriptions in the 20 Hz visit tick. */
function knownLocations(st:SimState):{id:string;x:number;y:number;size:number}[]{
 const c=st.campaign!;const sites=SITE_IDS.filter(id=>knownSite(st,id)).map(id=>({id,...campaignSite(st,id)!}));
 const out:{id:string;x:number;y:number;size:number}[]=[...sites,...(c.recruits?.sites??[]).filter(s=>s.seenAt>=0||s.recruitedAt>=0).map(s=>({...s,size:1}))];
 if(c.turbine&&(c.turbine.seenAt>=0||c.turbine.restoredAt>=0))out.push(c.turbine);
 if(c.discovery&&(c.discovery.seenAt>=0||c.discovery.recoveredAt>=0))out.push({...c.discovery,size:1});return out;
}
export function tickNavigation(st:SimState):void {
 if(!isCampaign(st))return;initNavigation(st);const n=st.campaign!.navigation!,G=ground(st),e=st.engineer;
 const tile=Math.floor(e.y)*G.tw+Math.floor(e.x),block=G.near[tile];
 if(block>=0&&!n.visitedBlocks.includes(block)){n.visitedBlocks.push(block);n.visitedBlocks.sort((a,b)=>a-b);}
 for(const s of knownLocations(st))if(Math.hypot(e.x-s.x-s.size/2,e.y-s.y-s.size/2)<=s.size/2+4&&!n.visitedSites.includes(s.id)){n.visitedSites.push(s.id);n.visitedSites.sort();}
}
export function knownDistrict(st:SimState,i:number):boolean {
 if(!isCampaign(st)||!st.blocks[i])return false;
 if(i===st.campaign!.homeBlock||st.campaign?.navigation?.visitedBlocks.includes(i)||campaignRecruited(st,'surveyors'))return true;
 const G=ground(st);return knownLocations(st).some(s=>G.near[s.y*G.tw+s.x]===i);
}
export function navigationTargets(st:SimState):NavigationTarget[]{
 if(!isCampaign(st))return [];const G=ground(st),n=st.campaign!.navigation;
 const out:NavigationTarget[]=campaignDiscoveries(st).map(s=>({id:`site:${s.id}`,name:s.title,defaultName:s.defaultTitle,x:s.x+s.size/2,y:s.y+s.size/2,visited:!!n?.visitedSites.includes(s.id),kind:'place'}));
 const known=new Set([st.campaign!.homeBlock,...(n?.visitedBlocks??[]),...out.map(t=>G.near[Math.floor(t.y)*G.tw+Math.floor(t.x)])]),surveyed=campaignRecruited(st,'surveyors');
 for(const p of G.urban?.places??[])if(surveyed||known.has(p.block))out.push({id:`block:${p.block}`,name:blockName(st,p.block),defaultName:p.name,x:p.pad.x+p.pad.w/2,y:p.pad.y+p.pad.h/2,visited:p.block===st.campaign!.homeBlock||!!n?.visitedBlocks.includes(p.block),kind:'district'});
 for(const p of n?.pins??[])out.push({id:`pin:${p.id}`,name:p.name,defaultName:`Pin ${p.id}`,x:p.x,y:p.y,visited:!!n?.visitedBlocks.includes(G.near[Math.floor(p.y)*G.tw+Math.floor(p.x)]),kind:'pin'});
 if(st.city?.mapId)for(const t of out){if(t.kind!=='place'||st.campaign?.progression?.sites.some(s=>'site:'+s.id===t.id&&s.kind==='core'&&!s.seen))continue;const b=RIVERFRONT_BUILDINGS.find(b=>t.x>b.x&&t.x<b.x+b.w&&t.y>b.y&&t.y<b.y+b.h);if(b?.enterable&&b.door){const p=doorOutside(b);t.x=p.x;t.y=p.y;}}
 return out;
}
// eslint-disable-next-line no-control-regex -- Player names must reject control and bidi override characters.
const textOK=(s:unknown):s is string=>typeof s==='string'&&s.length<=NAVIGATION_RULES.nameLength&&!/[\u0000-\u001f\u007f-\u009f\u202a-\u202e\u2066-\u2069]/u.test(s)&&s===s.trim();
export function navigationCommand(st:SimState,a:NavigationAction):ActionResult {
 try{
  if(!isCampaign(st)||!a)throw Error('Navigation belongs to the exploration campaign.');
  // Validate against known targets before allocating or changing any saved state.
  const current=st.campaign!.navigation,targets=navigationTargets(st);
  if(a.type==='rename'){
   const target=targets.find(t=>t.id===a.id);if(!target)throw Error('Choose a known destination.');
   if(!textOK(a.name))throw Error('Use up to 64 characters without control characters.');
   const name=a.name||target.defaultName;
   if(targets.some(t=>t.id!==a.id&&t.name.toLowerCase()===name.toLowerCase()))throw Error('Another known destination uses that name.');
   if(a.name&&target.kind!=='pin'&&!current?.names[a.id]&&Object.keys(current?.names??{}).length>=NAVIGATION_RULES.names)throw Error('Name limit reached (80). Reset a name first.');
   initNavigation(st);const n=st.campaign!.navigation!;
   if(target.kind==='pin')n.pins.find(p=>`pin:${p.id}`===a.id)!.name=name;
   else if(!a.name||a.name===target.defaultName)delete n.names[a.id];else n.names[a.id]=a.name;
  }else if(a.type==='pin'){
   const G=ground(st);if(!Number.isInteger(a.x)||!Number.isInteger(a.y)||a.x<0||a.y<0||a.x>=G.tw||a.y>=G.th||!knownDistrict(st,G.near[a.y*G.tw+a.x]))throw Error('Place pins inside known districts.');
   if(!textOK(a.name))throw Error('Use up to 64 characters without control characters.');
   if((current?.pins.length??0)>=NAVIGATION_RULES.pins||(current?.nextId??1)>=Number.MAX_SAFE_INTEGER)throw Error('Pin limit reached (32). Remove a pin first.');
   initNavigation(st);const n=st.campaign!.navigation!,id=n.nextId++;n.pins.push({id,x:a.x,y:a.y,name:a.name||`Pin ${id}`});
  }else if(a.type==='removePin'){
   if(!Number.isSafeInteger(a.id)||!current?.pins.some(p=>p.id===a.id))throw Error('Choose an existing pin.');
   current.pins=current.pins.filter(p=>p.id!==a.id);
  }else throw Error('Unknown navigation action.');
  return {ok:true,reason:'Navigation updated.'};
 }catch(e){return {ok:false,reason:(e as Error).message};}
}
export function navigationProblem(st:SimState):string {
 const n=st.campaign?.navigation;if(n===undefined)return '';
 const bad='Invalid saved navigation';
 if(!n||n.version!==1||!n.names||typeof n.names!=='object'||Array.isArray(n.names)||!Array.isArray(n.pins)||!Array.isArray(n.visitedBlocks)||!Array.isArray(n.visitedSites)||!Number.isSafeInteger(n.nextId)||n.nextId<1)return bad;
 if(Object.keys(n).some(k=>!['version','names','pins','nextId','visitedBlocks','visitedSites'].includes(k))||Object.keys(n.names).length>NAVIGATION_RULES.names||n.pins.length>NAVIGATION_RULES.pins||n.visitedBlocks.length>st.blocks.length||n.visitedSites.length>20)return bad;
 if(n.visitedBlocks.some(i=>!Number.isInteger(i)||!st.blocks[i])||new Set(n.visitedBlocks).size!==n.visitedBlocks.length||new Set(n.visitedSites).size!==n.visitedSites.length)return bad;
 const sites=campaignDiscoveries(st),ids=new Set(sites.map(s=>s.id));if(n.visitedSites.some(id=>!ids.has(id)))return bad;
 const G=ground(st);
 if(n.pins.some(p=>!p||Object.keys(p).some(k=>!['id','x','y','name'].includes(k))||!Number.isSafeInteger(p.id)||p.id<1||p.id>=n.nextId||!Number.isInteger(p.x)||!Number.isInteger(p.y)||p.x<0||p.y<0||p.x>=G.tw||p.y>=G.th||!textOK(p.name)||!p.name||!knownDistrict(st,G.near[p.y*G.tw+p.x]))||new Set(n.pins.map(p=>p.id)).size!==n.pins.length)return bad;
 for(const [id,name] of Object.entries(n.names)){
  if(!textOK(name)||!name)return bad;
  if(id.startsWith('block:')){if(!/^block:\d+$/.test(id)||!knownDistrict(st,Number(id.slice(6))))return bad;}
  else if(!id.startsWith('site:')||!ids.has(id.slice(5)))return bad;
 }
 return '';
}
