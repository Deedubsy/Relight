import {test} from 'node:test';
import assert from 'node:assert/strict';
import * as S from '../src/index';
import {hurt,inventoryCommand} from '../src/engineer';
import {tickProgression} from '../src/progression';

test('bulk receipt, old allocated pack, sorting and splitting conserve quantities across save/load',()=>{
 const s=S.createCampaign(),e=s.engineer;e.inv={ironore:12,copperore:12,crude:12,fuel:12,polymer:12,shell:12};
 assert.equal(S.pocketUsed(e),6);
 e.pack=Array.from({length:12},()=>({item:'ironore',count:1}));
 const copy=S.loadState(S.makeSave(s));assert.deepEqual(copy.engineer.pack,e.pack);
 assert.equal(S.pocketSlots(copy.engineer).filter(x=>x?.item==='ironore').length,12);
 assert.equal(inventoryCommand(copy.engineer,{type:'sort'}).ok,true);
 const slots=S.pocketSlots(copy.engineer),from=slots.findIndex(x=>x?.item==='ironore'),to=slots.findIndex(x=>!x);
 assert.equal(inventoryCommand(copy.engineer,{type:'split',from,to,item:'ironore',count:12,n:5,layout:JSON.stringify(slots)}).ok,true);
 assert.deepEqual(copy.engineer.inv,e.inv);assert.equal(S.pocketSlots(copy.engineer).filter(x=>x?.item==='ironore').reduce((n,x)=>n+x!.count,0),12);
 assert.equal(S.stackSize('core1'),1);assert.equal(S.stackSize('assembler'),1);
 assert.equal(S.stackSize('magazine'),20);assert.equal(S.stackSize('shell'),20);
});

test('plant readiness follows delivered stock, carried core and installed lifecycle',()=>{
 const s=S.createCampaign(),p=s.campaign!.progression!.sites.find(x=>x.kind==='plant')!;
 assert.match(S.progressionStatus(s,p),/Needs materials/);
 p.delivered={steel:30,copper:15};assert.match(S.progressionStatus(s,p),/carry a recovered/);
 s.engineer.inv.core1=1;assert.match(S.progressionStatus(s,p),/Ready/);
 p.installed='core1';p.restoredAt=0;p.enabled=false;assert.match(S.progressionStatus(s,p),/Offline/);
});

test('displayed encounter blocker stops the same productive clock without losing progress or payment',()=>{
 const s=S.createCampaign(),p=s.campaign!.progression!;
 for(const kind of ['heart','furnace','crown']){
  const site=p.sites.find(x=>x.kind===kind)!;site.seen=true;site.started=true;site.delivered={steel:30,copper:15};site.progress=4;
  assert.ok(S.encounterBlocker(s,site));tickProgression(s,.1);assert.equal(site.progress,4);assert.deepEqual(site.delivered,{steel:30,copper:15});
  assert.match(S.progressionStatus(s,site),/4 \/ (30|40|50) productive seconds/);
 }
});

test('relay damage is attributed, agrees with the ground predicate, and respects cover and shutdown',()=>{
 const s=S.createCampaign(),site=s.campaign!.progression!.sites.find(x=>x.kind==='core')!,e=s.engineer;
 site.seen=true;site.guards=[];e.x=site.x+1.5;e.y=site.y+1.5;e.down=-1;
 assert.ok(S.relayDangerAt(s,site,e.x,e.y));const hp=e.hp;tickProgression(s,.5);assert.equal(e.hp,hp-1);
 assert.equal(e.lastDamageSource,`relay:${site.id}`);assert.match(S.campaignAlerts(s)[0].title,/relay/);
 e.x=site.x+8;e.y=site.y;assert.equal(S.relayDangerAt(s,site,e.x,e.y),false);
 const g=S.ground(s);let covered=false;
 for(let y=site.y-6;y<=site.y+6;y++)for(let x=site.x-6;x<=site.x+6;x++)if(Math.hypot(x+.5-site.x,y+.5-site.y)<7&&!S.citySight(s,x+.5,y+.5,site.x+1.5,site.y+1.5)){assert.equal(S.relayDangerAt(s,site,x+.5,y+.5),false);covered=true;}
 assert.ok(covered||!g.urban,'authored relay has solid cover');
 site.enabled=false;assert.equal(S.relayDangerAt(s,site,site.x+1.5,site.y+1.5),false);
 hurt(s,1);assert.equal(e.lastDamageSource,undefined);
});


test('bulk materials transfer through physical storage and truck without changing inventory totals',()=>{
 const s=S.createCampaign(),e=s.engineer;e.inv={steel:20,copper:10,ironore:12,shell:20};
 const chest=S.place(s,'chest',63,362)!;assert.ok(chest);e.x=62.5;e.y=362.5;
 const before=e.inv.ironore;assert.equal(S.chestPut(s,'ironore',7,[chest.x,chest.y]).moved,7);assert.equal(S.chestTake(s,'ironore',7,[chest.x,chest.y]).moved,7);assert.equal(e.inv.ironore,before);
 // Prepared parked truck isolates cargo semantics from the restoration journey.
 s.campaign!.truck={x:e.x+2,y:e.y,dir:0,cargo:{},unlockedAt:0};
 assert.equal(S.truckTransfer(s,'shell',20,true).moved,20);assert.equal(S.truckTransfer(s,'shell',20,false).moved,20);assert.equal(e.inv.shell,20);assert.equal(s.campaign!.truck.cargo.shell??0,0);
});


test('powered equipment, defenders and one-time encounter rewards use the shared progress conditions',()=>{
 const s=S.createCampaign(),p=s.campaign!.progression!;p.arsenal=true;
 for(const kind of ['heart','furnace','crown']){
  const site=p.sites.find(x=>x.kind===kind)!,sub=S.ground(s).blocks[site.block].sub!;
  // Geometry-independent selector fixture; the authored layout is reviewed separately in the browser.
  site.x=sub.x+4;site.y=sub.y+4;site.seen=true;site.guards=[];site.started=true;site.delivered={steel:30,copper:15};
  const gen=S.addMachine(s,'generator',sub.x-3,sub.y,0);gen.inv.coal=30;
  const equipment:S.Machine[]=[];
  if(kind==='heart'){equipment.push(S.addMachine(s,'pole',site.x-1,site.y,0),S.addMachine(s,'pole',site.x+3,site.y,0));}
  if(kind==='furnace')for(let i=0;i<2;i++){const m=S.addMachine(s,'assembler',site.x-4,site.y+i*4,0);m.busy=true;equipment.push(m);}
  if(kind==='crown')for(let i=0;i<2;i++)equipment.push(S.addMachine(s,'lamp',site.x-3,site.y+i*3,0));
  assert.equal(S.encounterBlocker(s,site),'');
  if(kind==='furnace'){equipment[0].busy=false;assert.ok(S.encounterBlocker(s,site));equipment[0].busy=true;}
  site.progress=S.encounterDuration(site)-.05;site.waves=3;
  const stock={...s.stock};tickProgression(s,.1);assert.equal(site.restoredAt,s.t);
  if(kind==='heart')assert.equal(s.engineer.barrels,2);else assert.equal(s.stock[kind==='furnace'?'steel':'copper'],stock[kind==='furnace'?'steel':'copper']+50);
  const after={...s.stock};tickProgression(s,.1);assert.deepEqual(s.stock,after);
 }
});


test('opening guidance does not credit an old factory or an empty receiver as a supplied line',()=>{
 const s=S.createCampaign();const gen=S.addMachine(s,'generator',85,370,0);gen.inv.coal=30;
 const excavator=S.addMachine(s,'excavator',80,370,1);excavator.hold='steel';S.addMachine(s,'belt',83,371,1);S.addMachine(s,'chest',84,371,0);
 const asm=S.addMachine(s,'assembler',88,370,1);asm.busy=true;
 const belt=S.addMachine(s,'belt',91,371,1),chest=S.addMachine(s,'chest',92,371,0),turret=S.addMachine(s,'turret',95,371,0);turret.inv.rounds=20;
 s.flow!.stats.magsMade=100;s.engineer.inv.magazine=2;
 assert.equal(S.currentGoal(s).next!.id,'opening-ammo');
 asm.observation={produced:{magazine:1},consumed:{},samples:[{tick:0,produced:{},consumed:{}}]};
 assert.equal(S.currentGoal(s).next!.id,'opening-ammo');chest.inv.magazine=1;
 assert.notEqual(S.currentGoal(s).next!.id,'opening-ammo');
 belt.dir=3;s.flow!.rev++;assert.equal(S.currentGoal(s).next!.id,'opening-ammo');
 s.campaign!.defence!.bases[0].hp=0;assert.equal(S.currentGoal(s).next!.id,'home-recovery');
});


test('opening follows generator, excavator, storage, connection, turret, crafting and loading',()=>{
 const s=S.createCampaign();s.engineer.inv={steel:100,copper:100};
 const title=()=>S.currentGoal(s).next!.title;
 assert.match(title(),/1 · Build a Generator/);assert.match(S.currentGoal(s).next!.text,/30 Steel plates/);
 s.engineer.inv={generator:1};assert.deepEqual(S.currentGoal(s).next!.shortage,[]);assert.match(S.currentGoal(s).next!.text,/packed machine ready/);s.engineer.inv={steel:100,copper:100};
 const gen=S.addMachine(s,'generator',80,370,0);gen.inv.coal=10;
 assert.match(title(),/2 · Build an Excavator/);
 const ex=S.addMachine(s,'excavator',84,370,1);ex.hold='steel';
 assert.match(title(),/3 · Build storage/);
 S.addMachine(s,'chest',88,371,0);assert.match(title(),/4 · Connect belts/);
 S.addMachine(s,'belt',87,371,1);assert.match(title(),/5 · Build your first turret/);
 const t=S.addMachine(s,'turret',92,370,0);assert.match(title(),/6 · Make turret ammunition/);
 s.engineer.inv.magazine=1;assert.match(title(),/7 · Load your turret/);
 t.inv.rounds=10;assert.equal(S.currentGoal(s).next!.id,'opening-ammo');
});

test('machine quantities conserve fuel, buffered inputs, finished output and whole turret magazines',()=>{
 const s=S.createCampaign(),e=s.engineer;e.x=81;e.y=370;e.inv={coal:20,steel:20,copper:20,magazine:10};
 const gen=S.addMachine(s,'generator',80,370,0),asm=S.addMachine(s,'assembler',84,370,0),turret=S.addMachine(s,'turret',80,374,0);
 const transfer=(m:S.Machine,item:string,n:number,put:boolean)=>S.machineTransfer(s,{type:'machineTransfer',id:m.id,item,n,put});
 assert.equal(transfer(gen,'coal',7,true).moved,7);assert.equal(gen.inv.coal,7);assert.equal(e.inv.coal,13);
 assert.equal(transfer(gen,'coal',3,false).moved,3);assert.equal(e.inv.coal,16);
 assert.equal(transfer(gen,'steel',1,true).ok,false);assert.equal(transfer(gen,'coal',.5,true).ok,false);
 assert.equal(transfer(asm,'steel',20,true).moved,8);assert.equal(transfer(asm,'steel',3,false).moved,3);
 asm.out=3;assert.equal(transfer(asm,'magazine',2,false).moved,2);assert.equal(asm.out,1);
 assert.equal(transfer(turret,'magazine',2,true).moved,2);assert.equal(turret.inv.rounds,20);
 turret.inv.rounds=17;assert.equal(transfer(turret,'magazine',2,false).moved,1);assert.equal(turret.inv.rounds,7);
 assert.equal(transfer(turret,'magazine',1,false).ok,false);
 const before=JSON.stringify([e.inv,gen.inv]);e.x=300;e.y=300;
 assert.equal(transfer(gen,'coal',1,true).ok,false);assert.equal(JSON.stringify([e.inv,gen.inv]),before);
 const restored=S.loadState(S.makeSave(s));assert.equal(S.machineById(restored,turret.id)!.inv.rounds,7);
 e.x=81;e.y=370;e.inv={coal:10};e.pack=undefined;
 const slots=S.pocketSlots(e),slot=slots.findIndex(c=>c?.item==='coal');
 S.applyCommands(s,[{type:'factory',action:{type:'machineTransfer',id:gen.id,item:'coal',n:2,put:true,expected:{total:10,layout:JSON.stringify(slots),slot}}}]);
 assert.equal(S.actionResult(s).ok,true);assert.equal(e.inv.coal,8);
 S.applyCommands(s,[{type:'factory',action:{type:'machineTransfer',id:gen.id,item:'coal',n:2,put:true,expected:{total:10,layout:JSON.stringify(slots),slot}}}]);
 assert.equal(S.actionResult(s).ok,false);assert.equal(e.inv.coal,8);
 e.inv={core1:40};e.pack=undefined;const coal=gen.inv.coal;
 assert.equal(transfer(gen,'coal',1,false).ok,false);assert.equal(gen.inv.coal,coal);

});

test('power outage persists without fuel and clears when actual local generation returns',()=>{
 const s=S.createCampaign();assert.ok(S.campaignOutages(s).some(a=>a.title==='No power · Founders Court'));
 const g=S.addMachine(s,'generator',80,370,0);g.inv.coal=1;
 assert.equal(S.campaignOutages(s).some(a=>a.id==='outage:'+s.campaign!.homeBlock),false);
 g.inv.coal=0;assert.ok(S.campaignOutages(s).length);
 g.inv.coal=1;s.campaign!.defence!.bases[0].hp=0;assert.match(S.campaignOutages(s)[0].detail,/core disabled/);
});
