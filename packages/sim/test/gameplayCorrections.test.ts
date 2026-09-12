import {test} from 'node:test';
import assert from 'node:assert/strict';
import {readFileSync,writeFileSync} from 'node:fs';
import * as S from '../src/index';
import {tickProgression,spawnSiteEnemy} from '../src/progression';
import {threatOf} from '../src/threat';
import {loadSnapshot,parseUrl} from '../../game/src/session';
const run=(s:S.SimState,n:number)=>S.advanceFlow(s,n,[],Math.ceil(n*20)+1);
const send=(s:S.SimState,a:S.ProgressionAction,ok=true)=>{S.applyCommands(s,[{type:'progression',action:a}]);assert.equal(S.actionResult(s).ok,ok,S.actionResult(s).reason);};
// Prepared fixtures isolate later mechanics. Only ordinary-opening.ts proves ungranted progression.
const near=(s:S.SimState,m:{x:number;y:number})=>{s.engineer.x=m.x-1;s.engineer.y=m.y;s.engineer.block=S.blockOfTile(s,m.x-1,m.y);};
function supply(s:S.SimState,inv:Record<string,number>){for(const [k,n] of Object.entries(inv))s.engineer.inv[k]=(s.engineer.inv[k]??0)+n;}
function build(s:S.SimState,k:S.Kind,block=s.campaign!.homeBlock){const b=S.ground(s).blocks[block];for(let y=b.y0;y<b.y1;y++)for(let x=b.x0;x<b.x1;x++)if(S.blockOfTile(s,x,y)===block&&S.canPlace(s,k,x,y).ok){near(s,{x,y});const m=S.place(s,k,x,y);if(m)return m;}throw Error('no '+k);}
function fresh(){const s=S.createCampaign();s.speed=1;return s;}
function prepared(){const s=fresh();supply(s,{steel:1500,copper:1000,coal:300,polymer:100});return s;}
const ordinary=()=>{const s=S.loadState(JSON.parse(readFileSync('../../docs/evidence/gameplay-corrections/ordinary-save.json','utf8')));s.speed=1;return s;};
function roundtrip(s:S.SimState){assert.equal(S.stateProblem(s),'');const copy=S.loadState(S.makeSave(s));assert.equal(S.stateHash(copy),S.stateHash(s));return copy;}

test('new entry is exploration with the small Backpack stake; explicit legacy entry and state URL do not reinterpret saves',()=>{const s=fresh();assert.deepEqual(s.engineer.inv,{...S.CAMPAIGN_START_POCKETS});/* GP-START-POCKETS */assert.equal(s.stock.steel+s.stock.copper+s.buffer,0);assert.ok(!s.flow!.machines.some(m=>m.kind==='generator'));assert.equal(parseUrl('').ruleset,'exploration-v2');assert.equal(parseUrl('?rules=legacy-v1').ruleset,'legacy-v1');assert.equal(parseUrl('?state=/old.json').ruleset,undefined);roundtrip(s);});
test('unique core and artifact remain at source when full; transfer/attachment/load/packing conserve ownership',()=>{
 const s=prepared(),p=s.campaign!.progression!,core=p.sites.find(x=>x.kind==='core')!,artifact=p.sites.find(x=>x.kind==='artifact')!;
 near(s,core);core.guards=[];s.engineer.inv={steel:4000};send(s,{type:'recover',id:core.id},false);assert.equal(core.recovered,false);
 s.engineer.inv={};send(s,{type:'recover',id:core.id});send(s,{type:'recover',id:core.id},false);assert.equal(s.engineer.inv[core.item!],1);assert.equal(S.currentGoal(s).next!.title,'Choose a regional plant');
 near(s,artifact);send(s,{type:'recover',id:artifact.id});supply(s,{steel:100,copper:50});const m=build(s,'assembler');send(s,{type:'attach',machine:m.id,item:artifact.item!});assert.equal(S.processingMultiplier(m),1.1);roundtrip(s);
 assert.ok(S.remove(s,m.x,m.y));assert.equal(s.engineer.inv[artifact.item!],1);assert.equal(s.engineer.inv.assembler,1);roundtrip(s);
 const bad=structuredClone(s);bad.engineer.inv[core.item!]=2;assert.match(S.stateProblem(bad),/unique reward/);
});
test('actual first-core checkpoint powers chosen plant; supplementary generator burns under real excess load, disconnected grids stay separate and repairs retain core',()=>{
 const s=ordinary(),p=s.campaign!.progression!.sites.find(x=>x.installed)!;supply(s,{steel:1500,copper:1000,coal:50,polymer:100});
 const g=build(s,'generator',p.block);g.inv.coal=30;for(let i=0;i<5;i++)build(s,'assembler2',p.block);
 const grid=S.campaignGrid(s),c=grid.blocks[p.block];assert.equal(c.supply,900);assert.ok(c.demand>600);assert.ok((grid.generation.get(g.id)??0)>0);assert.notEqual(c,grid.blocks[s.campaign!.homeBlock]);
 const before=g.timer;run(s,1);assert.ok(g.timer>before);near(s,p);send(s,{type:'toggle',id:p.id});assert.equal(S.campaignGrid(s).blocks[p.block].supply,300);assert.ok(s.campaign!.defence!.bases.some(b=>b.block===p.block));send(s,{type:'toggle',id:p.id});
 const core=S.baseCore(s,p.block)!;S.damageCore(s,core,core.hp);assert.equal(core.hp,0);assert.equal(p.installed,'core1');assert.equal(S.campaignGrid(s).blocks[p.block].supply,0);
 near(s,p);S.applyCommands(s,[{type:'repairDefence',x:p.x,y:p.y}]);run(s,13);assert.ok(core.hp>0,S.repairCheck(s,p.x,p.y));assert.equal(p.installed,'core1');roundtrip(s);
});
test('each new processor consumes physical inputs, keeps backpressure and produces real items; artifact/Mk2 affect real processing time',()=>{
 for(const [kind,recipe,inputs,out] of [['foundry','iron',{ironore:4},'steel'],['foundry','copper',{copperore:4},'copper'],['refinery','fuel',{crude:10},'fuel'],['refinery','polymer',{crude:10,coal:10},'polymer'],['assembler','shell',{steel:20,coal:20},'shell']] as const){
  const s=prepared();s.campaign!.progression!.arsenal=true;const g=build(s,'generator');g.inv.fuel=20;const m=build(s,kind);assert.equal(S.setRecipe(s,m.x,m.y,recipe),'');m.inv={...inputs};const before=S.conservation(s).unexplained;run(s,25);assert.ok(m.out>0,`${kind} ${recipe} output`);assert.equal(S.recipeOutput(S.recipeOf(m)),out);assert.deepEqual(S.conservation(s).unexplained,before);roundtrip(s);
 }
 const s=prepared(),g=build(s,'generator');g.inv.coal=20;const g2=build(s,'generator');g2.inv.coal=20;const slow=build(s,'assembler'),fast=build(s,'assembler2');slow.inv={steel:10,copper:5};fast.inv={steel:10,copper:5};run(s,3.1);assert.equal(slow.out,0);assert.equal(fast.out,1);
 s.engineer.inv={};const a=s.campaign!.progression!.sites.find(x=>x.kind==='artifact')!;near(s,a);send(s,{type:'recover',id:a.id});near(s,slow);send(s,{type:'attach',machine:slow.id,item:a.item!});const before=slow.timer;run(s,.5);assert.ok(Math.abs(slow.timer-before-.55)<.001);roundtrip(s);
});
test('powered Pumpjack extracts finite crude; hand mining cannot bypass it',()=>{
 const s=prepared(),r=s.campaign!.progression!.resources.find(r=>r.item==='crude')!;near(s,r);S.setHandMine(s,[r.x,r.y]);run(s,2);assert.equal(s.engineer.inv.crude??0,0);
 const g=build(s,'generator',r.block);g.inv.coal=20;let m:S.Machine|undefined;for(let y=r.y-3;y<=r.y+2&&!m;y++)for(let x=r.x-3;x<=r.x+2;x++)if(S.blockOfTile(s,x,y)===r.block&&S.canPlace(s,'pumpjack',x,y).ok&&S.findRubble(s,{kind:'pumpjack',x,y,size:3} as S.Machine)){near(s,{x,y});m=S.place(s,'pumpjack',x,y)??undefined;if(m)break;}assert.ok(m);const before=r.remaining;run(s,3);assert.equal(m.hold,'crude');assert.ok(r.remaining<before);roundtrip(s);
});
test('paid specialist upgrades change whole-round hopper and freight capacity and survive reload',()=>{
 const s=prepared();for(const kind of ['gunsmith','railcrew'] as const){const r=s.campaign!.recruits!.sites.find(r=>r.kind===kind)!;near(s,r);S.applyCommands(s,[{type:'recruitSurvivors',id:r.id}]);assert.ok(S.campaignRecruited(s,kind));}
 const m=build(s,'turret'),before=s.engineer.inv.steel;send(s,{type:'hopper',machine:m.id});assert.equal(s.engineer.inv.steel,before-20);assert.equal(S.hopperCapacity(m,100),125);send(s,{type:'hopper',machine:m.id},false);send(s,{type:'freight'});assert.equal(S.freightCapacity(s,200),250);roundtrip(s);
});
test('passenger rides actual powered service with inventory intact; walking/placement blocked; outage reload exits safely',()=>{
 const s=ordinary(),tram=s.flow!.machines.find(m=>m.id===s.campaign!.fixedTram!.tram)!;run(s,.05);for(let i=0;i<500&&tram.phase===0;i++)run(s,.05);near(s,tram);const inv={...s.engineer.inv};send(s,{type:'board'});assert.equal(S.currentGoal(s).next!.title,'Riding the tram');if(process.env.CORRECTIONS_OUT)writeFileSync(process.env.CORRECTIONS_OUT+'/passenger-save.json',JSON.stringify(S.makeSave(s)));const start=[s.engineer.x,s.engineer.y];S.applyCommands(s,[{type:'walk',dx:1,dy:0}]);run(s,12);assert.notDeepEqual([s.engineer.x,s.engineer.y],start);assert.equal(s.engineer.x,tram.x+.5);assert.deepEqual(s.engineer.inv,inv);
 for(const p of s.campaign!.progression!.sites)if(p.kind==='plant')p.enabled=false;s.flow!.rev++;run(s,1);assert.equal(tram.phase,2);const saved=roundtrip(s);send(saved,{type:'exit'});assert.ok(!saved.campaign!.progression!.passenger);assert.ok(S.passable(saved,Math.floor(saved.engineer.x),Math.floor(saved.engineer.y)));assert.deepEqual(saved.engineer.inv,inv);
});

test('commissioned plant receives a real scheduled assault without radio; enemies arrive and damage it while output is off',()=>{
 const s=ordinary(),p=s.campaign!.progression!.sites.find(x=>x.installed)!,d=s.campaign!.defence!,core=S.baseCore(s,p.block)!;
 near(s,p);send(s,{type:'toggle',id:p.id});s.engineer.x=419;s.engineer.y=636;
 // Prepared clock only: retain the actual recovered core, plant, finite roster and route.
 s.t=d.nextDawn;S.nominateBase(s,p.block);S.tickCampaignSchedule(s,threatOf(s.flow!));assert.equal(d.major!.block,p.block);assert.match(S.campaignWarning(s),/Regional plant 1/);assert.equal(S.knownCampaignThreat(s)?.block,p.block);
 s.t=d.major!.startsAt;const hp=core.hp;for(let i=0;i<150&&core.hp===hp;i++)run(s,1);assert.ok(core.hp<hp,'actual arrivals damage the nominated plant');assert.equal(p.installed,'core1');roundtrip(s);
});
test('truck explicitly recovers a machine with artifact and contents to a chest; interrupted job reload preserves cargo and target',()=>{
 const s=S.loadState(JSON.parse(readFileSync('../../docs/evidence/p9-05-2026-09-09/campaign/network.json','utf8')));s.speed=1;const t=s.campaign!.truck!;assert.ok(t);supply(s,{steel:100,copper:100});
 const r=s.campaign!.recruits!.sites.find(x=>x.kind==='foreman')!;r.seenAt=r.recruitedAt=s.t;
 const find=(kind:S.Kind,range:number)=>{for(let radius=3;radius<range;radius++)for(let y=Math.floor(t.y)-radius;y<=t.y+radius;y++)for(let x=Math.floor(t.x)-radius;x<=t.x+radius;x++)if(S.canPlace(s,kind,x,y).ok&&S.truckWorkRoute(s,[{x,y,w:S.MACHINE_SIZE[kind],h:S.MACHINE_SIZE[kind]}],kind==='chest'?3:8)!==null){near(s,{x,y});return S.place(s,kind,x,y)!;}throw Error('No truck service pad');};
 const dest=find('chest',16),m=find('assembler',24);m.inv={steel:1};const a=s.campaign!.progression!.sites.find(x=>x.kind==='artifact')!;s.engineer.inv={};near(s,a);send(s,{type:'recover',id:a.id});near(s,m);send(s,{type:'attach',machine:m.id,item:a.item!});
 const before=S.conservation(s).unexplained;S.applyCommands(s,[{type:'truckWork',action:{type:'recover',sourceId:dest.id,ids:[m.id]}}]);assert.ok(S.actionResult(s).ok,S.actionResult(s).reason);run(s,.2);const saved=roundtrip(s);saved.speed=1;
 for(let i=0;i<160&&saved.campaign!.truck!.work!.phase!=='complete';i++)run(saved,.5);
 assert.equal(saved.campaign!.truck!.work!.phase,'complete',saved.campaign!.truck!.work!.reason);const chest=saved.flow!.machines.find(x=>x.id===dest.id)!;assert.equal(chest.inv.assembler,1);assert.equal(chest.inv[a.item!],1);assert.equal(chest.inv.steel,1);assert.ok(!saved.flow!.machines.some(x=>x.id===m.id));assert.deepEqual(S.conservation(saved).unexplained,before);roundtrip(saved);
});
test('Cannon consumes Shells against heavy enemies; unlit Shades cannot be shot and lit Shades can',()=>{
 const s=prepared();s.campaign!.progression!.arsenal=true;const cannon=build(s,'cannon');cannon.inv.shell=5;const fake={x:cannon.x+5,y:cannon.y,block:S.blockOfTile(s,cannon.x,cannon.y)} as S.ProgressionSite;
 threatOf(s.flow!);const id=spawnSiteEnemy(s,fake,'crawler','breaker'),enemy=s.flow!.threat!.crawlers.find(x=>x.id===id)!;tickProgression(s,.05);assert.equal(enemy.hp,50);assert.equal(cannon.inv.shell,4);tickProgression(s,2);assert.ok(!s.flow!.threat!.crawlers.some(c=>c.id===id));assert.equal(cannon.inv.shell,3);
 // Shade at an unpowered remote site: same firing system, no artificial visibility flag.
 const plant=s.campaign!.progression!.sites.find(x=>x.kind==='plant')!,gun=build(s,'cannon',plant.block);gun.inv.shell=3;const shadeId=spawnSiteEnemy(s,{x:gun.x+5,y:gun.y,block:plant.block} as S.ProgressionSite,'shade');const shade=s.flow!.threat!.crawlers.find(x=>x.id===shadeId)!;assert.equal(S.litAt(s,Math.floor(shade.x),Math.floor(shade.y)),false);tickProgression(s,3);assert.ok(s.flow!.threat!.crawlers.includes(shade));
 const gen=build(s,'generator',plant.block);gen.inv.coal=20;let lamp:S.Machine|undefined;for(let y=Math.floor(shade.y)-2;y<=shade.y+2&&!lamp;y++)for(let x=Math.floor(shade.x)-2;x<=shade.x+2;x++)if(S.canPlace(s,'lamp',x,y).ok&&S.blockOfTile(s,x,y)===plant.block){lamp=S.place(s,'lamp',x,y)??undefined;if(lamp)break;}assert.ok(lamp);assert.ok(S.litAt(s,Math.floor(shade.x),Math.floor(shade.y)));tickProgression(s,3);assert.ok(!s.flow!.threat!.crawlers.includes(shade));roundtrip(s);
});

test('three distinct paid engineering encounters complete with supplied automated defence, interruptions and saved one-time rewards',()=>{
 const s=prepared();supply(s,{steel:5000,copper:3000});const p=s.campaign!.progression!;threatOf(s.flow!);
 const nearby=(kind:S.Kind,site:S.ProgressionSite,side=0)=>{for(let radius=3;radius<=14;radius++)for(let y=site.y-radius;y<=site.y+radius;y++)for(let x=site.x-radius;x<=site.x+radius;x++)if(S.blockOfTile(s,x,y)===site.block&&(!side||side<0&&x<site.x||side>0&&x>=site.x+site.size)&&S.canPlace(s,kind,x,y).ok){near(s,{x,y});return S.place(s,kind,x,y)!;}throw Error('No nearby '+kind);};
 for(const kind of ['heart','furnace','crown'] as const){const site=p.sites.find(x=>x.kind===kind)!;site.seen=true;for(let i=0;i<3;i++)nearby('generator',site).inv.coal=100;
  // Physical poles join the site's substation to its two feeder positions.
  const sub=S.ground(s).blocks[site.block].sub!;
  for(let y=Math.min(sub.y,site.y)-3;y<=Math.max(sub.y,site.y)+4;y+=3)for(let x=Math.min(sub.x,site.x)-6;x<=Math.max(sub.x,site.x)+8;x+=3)if(S.canPlace(s,'pole',x,y).ok)S.place(s,'pole',x,y);
  nearby('pole',site,-1);nearby('pole',site,1);
  const processors=kind==='furnace'?[nearby('assembler',site),nearby('assembler',site)]:[];for(const m of processors)m.inv={steel:100,copper:50};
  if(kind==='crown'){nearby('lamp',site);nearby('lamp',site);}
  const guns=[nearby(kind==='heart'?'turret':'cannon',site),nearby(kind==='heart'?'turret':'cannon',site)];for(const m of guns)m.inv=m.kind==='turret'?{rounds:50}:{shell:20};
  near(s,site);send(s,{type:'deliver',id:site.id});const delivered={...site.delivered};send(s,{type:'start',id:site.id});run(s,1);send(s,{type:'abort',id:site.id});const progress=site.progress;run(s,1);assert.equal(site.progress,progress);send(s,{type:'start',id:site.id});assert.deepEqual(site.delivered,delivered);roundtrip(s);
  for(let n=0;n<800&&site.restoredAt<0;n++){
   // Prepared sustained production input/output handling for the Furnace's real load condition.
   for(const m of processors){if(m.out){s.buffer+=m.out*10;m.out=0;}}
   const conductors=s.flow!.threat!.crawlers.filter(c=>site.guards?.includes(c.id)&&c.role==='conductor');
   for(const c of conductors)if(!s.flow!.machines.some(m=>m.kind==='cannon'&&Math.hypot(c.x-m.x,c.y-m.y)<9)){const target={...site,x:Math.floor(c.x),y:Math.floor(c.y),block:S.blockOfTile(s,Math.floor(c.x),Math.floor(c.y))};const m=nearby('cannon',target);m.inv.shell=20;}
   near(s,site);if(!site.started)send(s,{type:'start',id:site.id});run(s,.25);
  }
  assert.ok(site.restoredAt>=0,`${kind}: ${site.progress}s, ${site.waves} waves, ${p.notice}`);assert.equal(site.waves,3);assert.deepEqual(site.delivered,delivered);send(s,{type:'start',id:site.id},false);roundtrip(s);
 }
 assert.ok(p.arsenal);assert.equal(s.engineer.barrels,2);
});

test('actual core/plant checkpoint receives freight materials and runs a direct-conveyor ammunition factory into a turret',()=>{
 const s=ordinary(),p=s.campaign!.progression!.sites.find(x=>x.installed)!;supply(s,{steel:300,copper:150});const stops=S.fixedStops(s),home=stops.find(m=>S.blockOfTile(s,m.x,m.y)===s.campaign!.homeBlock)!,remote=stops.find(m=>S.blockOfTile(s,m.x,m.y)===p.block)!;
 const parts:[S.Kind,number][]=[['chest',0],['belt',2],['assembler',3],['belt',6],['chest',7],['belt',9],['turret',10]],b=S.ground(s).blocks[p.block];let at:{x:number;y:number}|undefined;
 for(let y=b.y0;y<b.y1&&!at;y++)for(let x=b.x0;x<b.x1;x++)if(parts.every(([k,dx])=>S.blockOfTile(s,x+dx,y)===p.block&&S.canPlace(s,k,x+dx,y,1).ok)){at={x,y};break;}assert.ok(at,'usable plant factory strip');
 const built=parts.map(([k,dx])=>{near(s,{x:at!.x+dx,y:at!.y});return S.place(s,k,at!.x+dx,at!.y,1)!;}),source=built[0],turret=built.at(-1)!;
 near(s,home);assert.ok(S.setStationRules(s,home.x,home.y,{steel:{request:0,reserve:0,export:true},copper:{request:0,reserve:0,export:true}}));assert.equal(S.chestPut(s,'steel',20,[home.x,home.y]).moved,20);assert.equal(S.chestPut(s,'copper',10,[home.x,home.y]).moved,10);
 near(s,remote);assert.ok(S.setStationRules(s,remote.x,remote.y,{steel:{request:20,reserve:0,export:false},copper:{request:10,reserve:0,export:false}}));const baseline=S.conservation(s).unexplained;run(s,45);assert.equal(remote.cargo!.steel,20);assert.equal(remote.cargo!.copper,10);
 // Local carried last-mile delivery from actual arrivals to the factory's supply chest.
 near(s,remote);assert.equal(S.chestTake(s,'steel',20,[remote.x,remote.y]).moved,20);assert.equal(S.chestTake(s,'copper',10,[remote.x,remote.y]).moved,10);near(s,source);S.chestPut(s,'steel',20,[source.x,source.y]);S.chestPut(s,'copper',10,[source.x,source.y]);run(s,35);assert.ok(turret.inv.rounds>0,'tram-supplied factory loads connected turret without inserters');for(const k of S.ITEMS)assert.ok(Math.abs(S.conservation(s).unexplained[k]-baseline[k])<1e-8,k);roundtrip(s);
 const artifact=s.campaign!.progression!.sites.find(x=>x.kind==='artifact')!;near(s,artifact);send(s,{type:'recover',id:artifact.id});near(s,built[2]);send(s,{type:'attach',machine:built[2].id,item:artifact.item!});roundtrip(s);
 if(process.env.CORRECTIONS_OUT){writeFileSync(process.env.CORRECTIONS_OUT+'/factory-save.json',JSON.stringify(S.makeSave(s)));}
});


test('a recovered core can activate either alternative regional plant without commissioning the first',()=>{
 for(const index of [1,2]){const s=fresh();supply(s,{steel:30,copper:15});const sites=s.campaign!.progression!.sites,core=sites.find(x=>x.item==='core1')!,plant=sites.filter(x=>x.kind==='plant')[index];near(s,core);core.guards=[];send(s,{type:'recover',id:core.id});near(s,plant);send(s,{type:'deliver',id:plant.id});send(s,{type:'activate',id:plant.id});assert.equal(plant.installed,'core1');assert.ok(!sites.find(x=>x.kind==='plant')!.installed);assert.equal(S.campaignGrid(s).blocks[plant.block].plantSupply,600);roundtrip(s);}
});
test('fast belts move more actual chest freight than standard belts, conserving their items',()=>{
 const moved:number[]=[];
 for(const kind of ['belt','fastbelt'] as const){const s=prepared(),b=S.ground(s).blocks[s.campaign!.homeBlock];let at:{x:number;y:number}|undefined;for(let y=b.y0;y<b.y1&&!at;y++)for(let x=b.x0;x<b.x1;x++)if(S.canPlace(s,'chest',x,y).ok&&S.canPlace(s,kind,x+2,y,1).ok&&S.canPlace(s,'chest',x+3,y).ok){at={x,y};break;}assert.ok(at);near(s,at);const source=S.place(s,'chest',at.x,at.y)!;near(s,{x:at.x+2,y:at.y});S.place(s,kind,at.x+2,at.y,1);near(s,{x:at.x+3,y:at.y});const dest=S.place(s,'chest',at.x+3,at.y)!;source.inv.steel=100;const before=S.conservation(s).unexplained;run(s,5);moved.push(dest.inv.steel??0);assert.deepEqual(S.conservation(s).unexplained,before);roundtrip(s);}
 assert.ok(moved[1]>moved[0]*1.5,JSON.stringify(moved));
});
test('older campaign migration preserves earned stock and log but does not promise new-opening replay; legacy keeps its own mode',async()=>{
 const old=JSON.parse(readFileSync('../../docs/evidence/p9-05-2026-09-09/campaign/network.json','utf8')),original=old.state??old;const inv=structuredClone(original.engineer.inv),stock=structuredClone(original.stock);const fetchBefore=globalThis.fetch;
 try{globalThis.fetch=async()=>new Response(JSON.stringify(old));const loaded=await loadSnapshot('/old.json');assert.equal(loaded.state.ruleset,'exploration-v2');assert.deepEqual(loaded.state.engineer.inv,inv);assert.deepEqual(loaded.state.stock,stock);assert.ok(loaded.state.campaign!.progression);assert.equal(loaded.logComplete,false);roundtrip(loaded.state);
 const cfg=S.protoCalibrated(S.DEFAULT_CONFIG),legacy=S.createState(S.citySpec(3,'river',cfg),cfg,3);globalThis.fetch=async()=>new Response(JSON.stringify(S.makeSave(legacy)));const other=await loadSnapshot('/legacy.json');assert.ok(!other.state.campaign);assert.equal(other.state.ruleset??'legacy-v1','legacy-v1');
 }finally{globalThis.fetch=fetchBefore;}
});
