import {initFabrication} from './fabrication';
import {initActiveRaidClock} from './campaignThreat';
import {initOpeningEncounter} from './openingEncounter';
import {initFirstRegion,tickFirstRegion} from './firstRegion';
import {initEquipment} from './equipment';
import {migrateOverclocks} from './overclockMigration';
import {RIVERFRONT,RIVERFRONT_ID,riverfrontSpec} from './city/riverfront';
import {riverfrontRail} from './city/riverfrontRail';
import {createState} from './sim';
import {campaignConfig} from './campaign';
import {CAMPAIGN_RULESET,type CampaignSite} from './rules';
import {ensureFlow,addMachine,type Item} from './flow';
import {ground,blockOfTile} from './ground';
import {initDefence} from './campaignDefence';
import {initKnowledge} from './campaignGuide';
import {initNavigation} from './navigation';
import {recruitId,type RecruitKind} from './campaignRecruits';
import {newStalkerLayer} from './stalker';
import {DISCOVERY,discoveryId} from './campaignDiscovery';
import {CORRECTIONS,type ProgressionSite} from './progression';
import {initGameplayProgress} from './gameplayProgress';
/** All bindings refer to the authored definition. Old saves never enter this constructor. */
export function createRiverfrontCampaign(){
 const st=createState(riverfrontSpec(),campaignConfig(),3,CAMPAIGN_RULESET),c=st.campaign!;c.authored={version:4,cleared:[],opened:[],visited:[]};const f=ensureFlow(st),G=ground(st);
 const site=(xy:[number,number],size=1):CampaignSite=>({x:xy[0],y:xy[1],size,block:blockOfTile(st,...xy),delivered:{steel:0,copper:0},restoredAt:-1});
 const route=riverfrontRail().tiles,split=route.indexOf((RIVERFRONT.stops[2].y+2)*G.tw+RIVERFRONT.stops[2].x),p=RIVERFRONT.projects;
 c.expansion={surveyVersion:3,station:site(p.station),radio:site(p.radio),route:route.slice(0,split+1),stops:RIVERFRONT.stops.slice(0,2).map(s=>[s.x,s.y]),reward:{track:0,tramstop:0,tram:0},grantedAt:-1};
 c.districts={version:1,station:site(p.northStation),workshop:site(p.workshop),route:route.slice(split),stop:[RIVERFRONT.stops[2].x,RIVERFRONT.stops[2].y],sources:RIVERFRONT.resources.filter(r=>r[5]===3000).map(([item,x,y])=>[item,x,y]).map(([item,x,y])=>({x:x as number,y:y as number,item:item as 'steel'|'copper'|'coal',size:3,block:blockOfTile(st,x as number,y as number)})),repair:null,repairs:0,supplied:{steel:0,copper:0,magazine:0},visits:0,lastVisit:-1,resuppliedAt:-1};
 initDefence(st);c.defence!.sites=[];
 const guardian=newStalkerLayer(st,DISCOVERY.guardian),records=site(p.records);guardian.sites={[records.block]:{restored:false,diedAt:-1,spawned:0}};
 c.discovery={version:1,id:discoveryId(st.seed),...records,seenAt:-1,recoveredAt:-1,guardian};
 c.recruits={version:5,sites:RIVERFRONT.recruits.map(([kind,x,y])=>({id:recruitId(3,kind as RecruitKind),kind:kind as RecruitKind,x,y,block:blockOfTile(st,x,y),seenAt:-1,recruitedAt:-1,inherited:false}))};
 c.turbine={...site(p.turbine,4),id:`${st.seed}:turbine:v1`,seenAt:-1,enabled:false,delivered:{steel:0,copper:0,concrete:0}};
 // Only the canonical line creates track. No expansion survey or old kit spawns run here.
 for(const t of route)addMachine(st,'track',t%G.tw,Math.floor(t/G.tw),0);
 const stops=RIVERFRONT.stops.map(s=>addMachine(st,'tramstop',s.x,s.y,0)),tram=addMachine(st,'tram',route[0]%G.tw,Math.floor(route[0]/G.tw),0);
 c.fixedTram={version:1,route,stops:stops.map(s=>s.id),tram:tram.id};
 const sites:ProgressionSite[]=[];
 const add=(kind:ProgressionSite['kind'],s:{id:string;name:string;x:number;y:number},item?:Item)=>{sites.push({...s,kind,size:3,block:blockOfTile(st,s.x,s.y),seen:kind==='plant',item,recovered:false,enabled:true,delivered:{},restoredAt:-1,progress:0,started:false,waves:0,attempt:0,searchX:s.x+9,searchY:s.y+7});};
 RIVERFRONT.plants.forEach(s=>add('plant',s));RIVERFRONT.cores.forEach((s,i)=>add('core',s,`core${i+1}` as Item));RIVERFRONT.artifacts.forEach((s,i)=>add('artifact',s,`artifact${i+1}` as Item));
 for(const [kind,name]of [['heart','Junction Heart / Arsenal'],['furnace','Furnace Walker'],['crown','Blackout Crown']] as const){const [x,y]=p[kind];add(kind,{id:`riverfront:${kind}`,name,x,y});}
 c.progression={version:1,sites,resources:RIVERFRONT.resources.filter(r=>['ironore','copperore','crude','stone'].includes(r[0])||r[0]==='coal'&&r[5]===12000).map(([item,x,y])=>({x,y,block:blockOfTile(st,x,y),item:item as Item,remaining:CORRECTIONS.resourceUnits})),arsenal:false,freightUpgrade:false,notice:''};
 f.ammoVersion=1;for(const m of f.machines)m.ammoVersion=1;c.version=10;initGameplayProgress(st);migrateOverclocks(st);initEquipment(st,true);initFirstRegion(st,true);initFabrication(st,true);initActiveRaidClock(st);initOpeningEncounter(st);tickFirstRegion(st);initKnowledge(st,true);initNavigation(st);f.rev++;return st;
}
export const isRiverfront=(st:{city?:{mapId?:string}})=>st.city?.mapId===RIVERFRONT_ID;
