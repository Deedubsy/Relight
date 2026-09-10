import {readFileSync,writeFileSync,existsSync} from 'node:fs';
import assert from 'node:assert/strict';
import {createCampaign,makeSave,stateHash,loadState,ensureFlow,conservation,CAMPAIGN_RULES,CAMPAIGN_THREAT,DEFENCE,DISTRICT_ECONOMY,DISCOVERY} from '../../../packages/sim/src/index';
import {ground,rubbleAt,parseBlueprint,blueprintTransform,blueprintBounds,placeable,blockOfTile,dimensions,MACHINE_SIZE,actionResult,truckWorkRoute,type SimState,type Blueprint,type Command} from '../../../packages/sim/src/index';
import {FactoryDriver} from '../../../packages/harness/src/blueprint';
import {replayInterval,savedContinuation} from '../../../packages/harness/src/blueprint';
import {stamp} from '../../../packages/harness/src/provenance';
const out='docs/evidence/p9-05-2026-09-09',ref={kind:'campaign',ruleset:'exploration-v2',opening:'culdesac-v1'} as const,provenance=stamp(ref);
const evidence=JSON.parse(readFileSync('docs/evidence/p7-05-2026-09-08/campaign/E-defence-turrets-seed3.json','utf8'));
const initial=createCampaign(3);ensureFlow(initial);
const tick=2390*20,log=evidence.log.filter((l:any)=>l.tick<=tick),network=replayInterval(initial,log,tick);
assert.equal(network.t,2390);assert.equal(network.campaign!.defence!.major,null);assert.equal(network.campaign!.defence!.nextDawn,2400);
assert.ok(network.campaign!.expansion!.radio.restoredAt>=0);assert.ok(network.campaign!.defence!.bases.every(b=>b.hp>0));
const pending=JSON.parse(readFileSync('docs/evidence/p7-03-2026-09-08/turbine-pending.json','utf8')),turbine=replayInterval(initial,pending.log,pending.state.flow.tick);
assert.equal(turbine.campaign!.turbine!.restoredAt,-1);assert.equal(turbine.engineer.inv.concrete,40);
const ammo=parseBlueprint({version:1,name:'Belt-fed ammunition cell',entities:[{id:'input',kind:'chest',x:0,y:0,dir:0},{id:'feed',kind:'inserter',x:2,y:0,dir:1},{id:'assembler',kind:'assembler',x:3,y:0,dir:0,recipe:'shot'},{id:'out',kind:'inserter',x:6,y:1,dir:1},{id:'belt',kind:'belt',x:7,y:1,dir:1},{id:'turret',kind:'turret',x:8,y:1,dir:0}]});
function send(d:FactoryDriver,c:Command){d.send(c);assert.ok(actionResult(d.state).ok,`${c.type}: ${actionResult(d.state).reason}`);}
function site(st:SimState,raw:Blueprint,stationOnly=true){const t=st.campaign!.truck!,station=st.campaign!.expansion!.station;let checked=0;
 for(let rotation=0;rotation<4;rotation++){let bp=raw;for(let i=0;i<rotation;i++)bp=blueprintTransform(bp,'rotate');const b=blueprintBounds(bp);
 for(let r=4;r<=44;r+=4)for(let y=Math.floor(t.y)-r;y<t.y+r;y+=2)for(let x=Math.floor(t.x)-r;x<t.x+r;x+=2){checked++;
 if(stationOnly&&bp.entities.filter(e=>['assembler','inserter'].includes(e.kind)).some(e=>blockOfTile(st,x+e.x,y+e.y)!==station.block))continue;
 if(st.flow!.machines.some(m=>{const [w,h]=dimensions(m.kind,m.dir,MACHINE_SIZE[m.kind]);return m.x<x+b.w&&m.x+w>x&&m.y<y+b.h&&m.y+h>y;}))continue;
 if(!bp.entities.every(e=>!placeable(st,e.kind,x+e.x,y+e.y,e.dir)))continue;
 const rects=bp.entities.map(e=>{const [w,h]=dimensions(e.kind,e.dir,MACHINE_SIZE[e.kind]);return {x:x+e.x,y:y+e.y,w,h};}),route=truckWorkRoute(st,rects,8);if(route!==null)return {x,y,bp,checked,routeSteps:route.length};
 }}throw Error(`No truck-served ${raw.name} site, seed ${st.seed}, stock ${JSON.stringify(st.engineer.inv)}`);
}
function ensureSteel(d:FactoryDriver,n:number){const st=d.state,G=ground(st);let attempts=0;while((st.engineer.inv.steel??0)<n&&attempts++<8){const candidates:{x:number;y:number;distance:number}[]=[];for(let tile=0;tile<G.tw*G.th;tile++){if(!G.patch[tile])continue;const x=tile%G.tw,y=Math.floor(tile/G.tw),r=rubbleAt(st,x,y);if(r?.type==='steel'&&r.units>0)candidates.push({x,y,distance:Math.hypot(x-st.engineer.x,y-st.engineer.y)});}const p=candidates.sort((a,b)=>a.distance-b.distance)[0];assert.ok(p);d.approach(p.x,p.y);send(d,{type:'factory',action:{type:'mineAt',x:p.x,y:p.y}});d.run(n-(st.engineer.inv.steel??0)+1);send(d,{type:'factory',action:{type:'mineAt',x:-1,y:-1}});}assert.ok((st.engineer.inv.steel??0)>=n,'ordinary mining supplied required steel');}


const truckSave=JSON.parse(readFileSync('docs/evidence/p8-03-2026-09-08/truck-ready.json','utf8'));
const construction=replayInterval(initial,truckSave.log,truckSave.state.flow.tick),d=new FactoryDriver(construction);
assert.equal(construction.campaign!.recruits!.sites.find(r=>r.kind==='foreman')!.recruitedAt>=0,true);
for(const o of construction.campaign!.plans!.orders)send(d,{type:'blueprintOrder',action:{type:'cancel',id:o.id}});
const supply=construction.flow!.machines.find(m=>m.kind==='chest')!,p=site(construction,ammo);
send(d,{type:'blueprintLibrary',action:{type:'import',text:JSON.stringify({format:'relight-blueprint',version:1,name:p.bp.name,folder:'Defence',icon:'turret',blueprint:p.bp})}});
send(d,{type:'blueprintOrder',action:{type:'queue',x:p.x,y:p.y}});
ensureSteel(d,68-(supply.inv.steel??0)+12);
d.put('steel',68-(supply.inv.steel??0),supply);d.put('copper',27,supply);
assert.equal(supply.inv.steel,68);assert.equal(supply.inv.copper,27);
const constructionLog=[...truckSave.log,...d.log];
assert.ok(construction.t<200);assert.ok(construction.campaign!.defence!.bases.every(b=>b.hp>0));
assert.equal(stateHash(replayInterval(initial,constructionLog,construction.flow!.tick)),stateHash(construction));
console.log('CONSTRUCTION',JSON.stringify({t:construction.t,source:supply,site:p,pockets:construction.engineer.inv,orders:construction.campaign!.plans!.orders}));
// Exercise a clone so the playable start retains its waiting order and real stock.
const probe=new FactoryDriver(structuredClone(construction)),order=probe.state.campaign!.plans!.orders.at(-1)!;
send(probe,{type:'truckWork',action:{type:'start',sourceId:supply.id,orderIds:[order.id]}});
for(let i=0;i<240&&order.status!=='completed';i++)probe.run(.5);
assert.equal(order.status,'completed',probe.state.campaign!.truck!.work!.reason);
const input=probe.state.flow!.machines.find(m=>m.kind==='chest'&&m.id!==supply.id)!;
probe.put('steel',4,input);probe.put('copper',2,input);probe.run(35);
const turret=probe.state.flow!.machines.find(m=>m.kind==='turret')!;assert.equal(turret.inv.rounds,20);
assert.ok(conservation(probe.state).ok);assert.ok(savedContinuation(probe.state).same);
assert.equal(stateHash(replayInterval(initial,[...constructionLog,...probe.log],probe.state.flow!.tick)),stateHash(probe.state));
writeFileSync(out+'/construction-probe.json',JSON.stringify({order,rounds:turret.inv.rounds,t:probe.state.t,conservation:conservation(probe.state),fullReplay:true,sourceId:supply.id,origin:{x:p.x,y:p.y},input:{x:input.x,y:input.y}},null,2));

const starts=[];
for(const [name,st,commands,description] of [['construction',construction,constructionLog,'Scripted ordinary paid opening, recruited Foreman, restored second-area station, local stocked chest and queued ammunition cell. Home has no added defence; start early and pause while learning.'],['fresh',initial,[],'Untouched ordinary tick-zero campaign; no assistance.'],['network',network,log,'Scripted opening replayed from ordinary starting stock and full paid command history; player did not perform the opening. No resource/position/clock/HP injection. Not an opening-time or unaided discovery observation.'],['turbine',turbine,pending.log,'Optional assisted restoration start: ordinary paid mining, concrete production, travel and local generator prepared; crew discoveries already known. Do not use for unaided discovery.']] as const){
 const save=makeSave(st,{log:[...commands],logComplete:true,params:{ruleset:'exploration-v2',seed:3,view:'world'}}),loaded=loadState(save);ensureFlow(loaded);
 assert.ok(conservation(structuredClone(st)).ok);assert.ok(savedContinuation(st).same);assert.equal(loadState(save).speed,0);
 const file=out+'/campaign/'+name+'.json';assert.ok(!existsSync(file));writeFileSync(file,JSON.stringify({...save,...provenance},null,1)+'\n');
 starts.push({name,file,description,hash:save.hash,browserNormalizedHash:stateHash(loaded),tick:st.flow!.tick,t:st.t,logEntries:commands.length,radioRestored:st.campaign!.expansion!.radio.restoredAt>=0,engineer:st.engineer,bases:st.campaign!.defence!.bases,stock:st.stock,store:st.flow!.store,buffer:st.buffer,machines:st.flow!.machines.map(m=>({id:m.id,kind:m.kind,x:m.x,y:m.y,dir:m.dir,inv:m.inv,cargo:m.cargo})),nextDawn:st.campaign!.defence!.nextDawn});
}
const settings={...provenance,session:'P9-H / shared P5–P8 observations',human_play:'not_run',starts,defaultStart:'fresh',speed:'1x; pauses/interventions recorded',difficulty:'unchanged default campaign rules',config:initial.config,clock:CAMPAIGN_RULES,threat:CAMPAIGN_THREAT,defence:DEFENCE,districts:DISTRICT_ECONOMY,discovery:DISCOVERY};
assert.ok(!existsSync(out+'/settings.json'));writeFileSync(out+'/settings.json',JSON.stringify(settings,null,2)+'\n');console.log(JSON.stringify(starts.map(s=>({name:s.name,t:s.t,hash:s.hash,browser:s.browserNormalizedHash,logEntries:s.logEntries,bases:s.bases.map(b=>({block:b.block,hp:b.hp}))})),null,2));
console.log('PASS: four starts and ordinary-command prefixes verified, conserved, load paused and continue deterministically.');

for(const seed of [3,4,5,8,11,13,80,88,102,842]){
 const st=createCampaign(seed);ensureFlow(st);const save=makeSave(st,{log:[],logComplete:true,params:{ruleset:'exploration-v2',seed,view:'world'}});
 assert.equal(stateHash(loadState(save)),save.hash);writeFileSync(out+'/campaign/fresh-'+seed+'.json',JSON.stringify(save)+'\n');
}
console.log('PASS: ten declared fresh review seeds, unassisted and paused.');
