import {test} from 'node:test';
import assert from 'node:assert/strict';
import * as S from '../src/index';

/** GP-PLAYTEST-FIX second pass (2026-09-11): storage transfers merge and spill across the whole Backpack; a hand craft
 *  locks the engineer, cancels with an exact refund and never creates resources; the campaign turret fires once a second. */
function home(){
 const s=S.createCampaign(),e=s.engineer;s.speed=1;
 s.stock.steel=400;s.stock.copper=300;s.buffer=200;e.inv={steel:120,copper:30,magazine:50,coal:20};e.pack=S.pocketSlots(e);
 return {s,e};
}
const slots=(e:S.Engineer)=>S.pocketSlots(e);
const layout=(e:S.Engineer)=>JSON.stringify(slots(e));
const act=(s:S.SimState,a:S.FactoryAction)=>{S.applyCommands(s,[{type:'factory',action:a}]);return S.actionResult(s);};
const take=(s:S.SimState,item:string,n:number,slot:number)=>act(s,{type:'chestTake',item:item as S.ChestItem,n,target:{slot,layout:layout(s.engineer)},expected:{total:S.chestCount(s,item as S.ChestItem)}});
const put=(s:S.SimState,item:string,n:number,slot:number,dest?:string)=>act(s,{type:'chestPut',item:item as S.ChestItem,n,target:{item:dest},expected:{total:s.engineer.inv[item]??0,layout:layout(s.engineer),slot}});

test('repeated drops onto a full matching Backpack stack merge into the rest of the Backpack and stop at the true room',()=>{
 const {s,e}=home();const full=slots(e).findIndex(c=>c?.item==='steel'&&c.count===50);assert.ok(full>=0);
 const before=S.pocketUsed(e);
 const r1=take(s,'steel',50,full);assert.equal(r1.ok,true);assert.equal(r1.moved,50,'a full destination stack spills into free room instead of refusing');
 assert.equal(e.inv.steel,170);assert.equal(S.chestCount(s,'steel'),350);
 const r2=take(s,'steel',50,full);assert.equal(r2.moved,50);assert.equal(e.inv.steel,220);
 const cap=S.invCap(e),used=S.pocketUsed(e);assert.ok(used>before&&used<=cap);
 // fill the Backpack, then confirm the transfer moves only what fits and leaves the rest in storage
 let guard=0;while(S.pocketUsed(e)<cap&&guard++<40){const r=take(s,'copper',50,0);if(!r.ok)break;}
 const stockBefore=S.chestCount(s,'steel'),steelBefore=e.inv.steel,room=S.packRoom(slots(e),'steel');
 const r3=take(s,'steel',50,full);
 assert.equal(r3.moved,Math.min(50,room));assert.equal(e.inv.steel,steelBefore+r3.moved);assert.equal(S.chestCount(s,'steel'),stockBefore-r3.moved,'overflow stays in storage');
 assert.equal(JSON.stringify(slots(e)),JSON.stringify(e.pack),'allocation stays consistent with totals');
 assert.equal(S.stateProblem(s),'');
});

test('every stackable item and ammunition merge repeatedly; mismatched drops refuse and change nothing',()=>{
 const {s,e}=home();
 for(const item of ['copper','coal','magazine'] as const){
  const slot=slots(e).findIndex(c=>c?.item===item);assert.ok(slot>=0,item);
  if(item==='coal'){s.flow!.store.coal=100;}
  const invBefore=e.inv[item]??0,storeBefore=S.chestCount(s,item);
  const a=take(s,item,10,slot),b=take(s,item,10,slot);
  assert.equal(a.ok&&b.ok,true,item);assert.equal(e.inv[item],invBefore+20,item);assert.equal(S.chestCount(s,item),storeBefore-20,item);
 }
 const copperSlot=slots(e).findIndex(c=>c?.item==='copper'),inv={...e.inv},store=S.chestCount(s,'steel');
 const bad=take(s,'steel',10,copperSlot);assert.equal(bad.ok,false);assert.match(bad.reason,/compatible/);
 assert.deepEqual(e.inv,inv);assert.equal(S.chestCount(s,'steel'),store);
 // Backpack → storage onto a pooled matching stack keeps working after the pooled stack shows full
 const steelSlot=slots(e).findIndex(c=>c?.item==='steel');
 for(let i=0;i<3;i++){const r=put(s,'steel',10,slots(e).findIndex(c=>c?.item==='steel'),'steel');assert.equal(r.ok,true,String(i));assert.equal(r.moved,10);}
 assert.equal(e.inv.steel,inv.steel!-30);assert.equal(S.chestCount(s,'steel'),store+30);
 const wrong=put(s,'steel',10,steelSlot,'copper');assert.equal(wrong.ok,false);assert.match(wrong.reason,/compatible/);
});

test('handcrafting holds the engineer in place, cancels with an exact refund and cannot mint resources',()=>{
 const {s,e}=home();const [x,y]=[e.x,e.y];
 assert.equal(S.queueCraft(s,1),'');S.advanceFlow(s,.05,[]);assert.equal(s.flow!.hand.crafting,true);assert.equal(S.handLocked(s),true);
 assert.equal(e.inv.steel,118);assert.equal(e.inv.copper,29);
 e.target=[x+4,y];e.dest=0;for(let i=0;i<40;i++)S.advanceFlow(s,.05,[]);
 assert.ok(Math.hypot(e.x-x,e.y-y)<1e-6,'no movement while crafting');S.setHandMine(s,[Math.floor(x)+1,Math.floor(y)]);assert.equal(s.flow!.hand.mine,null,'no mining while crafting');
 const r=S.cancelCraft(s);assert.equal(r.ok,true);assert.equal(s.flow!.hand.crafting,false);assert.equal(S.handLocked(s),false);
 assert.equal(e.inv.steel,120);assert.equal(e.inv.copper,30);assert.equal(s.flow!.stats.consumed.steel,0);
 assert.equal(S.cancelCraft(s).ok,false);assert.equal(e.inv.steel,120,'repeated cancellation returns nothing more');
 assert.equal(S.queueCraft(s,1),'');for(let i=0;i<Math.round(S.HAND_BULLET_SECONDS/.05)+2;i++)S.advanceFlow(s,.05,[]);
 assert.equal(e.inv.magazine,60,'one completed batch adds ten bullets, one item each');assert.equal(s.flow!.hand.crafting,false);
 assert.equal(S.cancelCraft(s).ok,false,'a finished batch keeps its output and refunds nothing');assert.equal(e.inv.steel,118);
 assert.equal(S.stateProblem(s),'');
});

test('the campaign turret fires one bullet per second',()=>{
 assert.equal(S.CAMPAIGN_TURRET_RATE,1);assert.equal(S.HAND_MINE_PER_S,.5);assert.equal(S.HAND_BULLET_SECONDS,20);
});

test('a Gun turret is a 20 kW machine: unpowered it holds fire and blocks the intro, powered it fires at the circuit throttle',()=>{
 assert.equal(S.TURRET_KW,20);assert.equal(S.MACHINE_KW.turret,S.TURRET_KW);
 const s=S.createCampaign(),e=s.engineer;e.inv={steel:200,copper:200};s.speed=1;
 const gen=S.addMachine(s,'generator',60,353,0);gen.inv.coal=30;
 const turret=S.addMachine(s,'turret',63,361,2);turret.inv.rounds=S.TURRET_HOPPER;
 const step=(n:number)=>{for(let i=0;i<n;i++)S.advanceFlow(s,.05,[]);};
 step(2);
 assert.equal(S.machineRunning(s,turret),false,'no pole reaches the turret: it is disconnected');
 assert.equal(s.campaign!.defence!.opening!.status,'pending','a full but unpowered turret does not start the intro attack');
 assert.match(S.describeMachine(s,turret),/20 kW/);
 const disconnected=S.campaignGrid(s).demand;
 const pole=S.addMachine(s,'pole',62,358,0);step(2);
 assert.equal(S.machineRunning(s,turret),true);
 assert.equal(S.campaignGrid(s).demand-disconnected,S.TURRET_KW,'connecting the turret adds exactly its draw to the grid');assert.ok(S.campaignGrid(s).machines.get(turret.id)!.demand>=S.TURRET_KW);
 assert.equal(s.campaign!.defence!.opening!.status,'scheduled','the powered, loaded turret starts the intro');
 gen.inv.coal=0;step(2);
 assert.equal(S.machineRunning(s,turret),false,'an unsupplied circuit stops the turret');
 assert.ok(pole);
});
