import {test} from 'node:test';
import assert from 'node:assert/strict';
import {createCampaign,addMachine,depotRect,equipmentCommand,tickEquipment,activeWeapon,pocketSlots,applyCommands,actionResult,heldItems,loadState,stateHash,campaignGrid,powered,stepFlow,conservation,openLedger,ground,campaignApproaches,baseCore,placementGeometryProblem,type SimState} from '../src/index';
function home(){const s=createCampaign(),h=depotRect(s);s.engineer.x=h.x-.5;s.engineer.y=h.y+.5;return s;}
test('owned Rifle cycle and exact-target stack transactions conserve state',()=>{
 const s=home(),e=s.engineer;assert.equal(activeWeapon(s),undefined);e.inv={steel:48,copper:4,magazine:30,generator:37};
 s.stock.steel=20;const slots=pocketSlots(e),slot=slots.findIndex(c=>c?.item==='steel');
 const transfer={type:'chestTake' as const,item:'steel',n:20,expected:{total:20},target:{total:48,slot,layout:JSON.stringify(slots)}};
 applyCommands(s,[{type:'factory',action:transfer}]);assert.equal(actionResult(s).moved,2);assert.equal(e.inv.steel,50);assert.equal(s.stock.steel,18);
 const before=JSON.stringify(e);applyCommands(s,[{type:'factory',action:transfer}]);assert.equal(actionResult(s).ok,false);assert.equal(JSON.stringify(e),before);
 assert.ok(equipmentCommand(s,{type:'craftRifle'}).ok);tickEquipment(s,6);assert.ok(equipmentCommand(s,{type:'equip',item:'rifle:1',slot:0}).ok);
 const rounds=heldItems(s).total.magazine;equipmentCommand(s,{type:'reload'});tickEquipment(s,1.5);assert.equal(activeWeapon(s)!.loaded,10);assert.equal(e.inv.magazine,20);
 activeWeapon(s)!.cooldown=.3;equipmentCommand(s,{type:'unequip',slot:0});equipmentCommand(s,{type:'equip',item:'rifle:1',slot:1});assert.equal(activeWeapon(s)!.cooldown,.3);assert.equal(heldItems(s).total.magazine,rounds);
});
test('old ammunition pools migrate once, loaded rounds stay unchanged, transport overflow is recoverable',()=>{
 const s=home();delete s.flow!.ammoVersion;equip(s);s.engineer.inv.magazine=2.7;s.engineer.equipment!.weapons['rifle:1']!.loaded=4;
 const t=addMachine(s,'turret',70,365,0),b=addMachine(s,'belt',73,365,1);t.inv.rounds=17;b.items=[{k:'magazine',p:.3}];
 const old=JSON.stringify(s),n=loadState(s);assert.equal(JSON.stringify(s),old);assert.equal(n.engineer.inv.magazine,27);assert.equal(activeWeapon(n)!.loaded,4);assert.equal(n.flow!.machines.find(m=>m.id===t.id)!.inv.rounds,17);assert.equal(n.flow!.ammoRecovery,9);assert.equal(stateHash(loadState(n)),stateHash(n));
});
function equip(s:SimState){const q=s.engineer.equipment!;q.next=2;q.weapons['rifle:1']={kind:'rifle',loaded:0,reload:0,cooldown:0};q.slots[0]='rifle:1';}
test('cable components merge, split, stop without fuel; conveyors remain free and bullets are batched honestly',()=>{
 const s=home(),G=ground(s);let origin:{x:number;y:number}|undefined;
 const plan=[['generator',0,0],['pole',4,0],['assembler',5,2],['pole',20,0],['generator',22,0],['assembler',21,3]] as const;
 outer:for(let y=330;y<400;y++)for(let x=60;x<160;x++)if(plan.every(([k,dx,dy])=>!placementGeometryProblem(s,k,x+dx,y+dy))&&!G.blocks.some(b=>b.sub&&Math.hypot(b.sub.x-x-12,b.sub.y-y)<30)){origin={x,y};break outer;}
 assert.ok(origin,'clear disconnected network fixture');const {x,y}=origin;
 const [g,a,m,b,h,n]=plan.map(([k,dx,dy])=>addMachine(s,k,x+dx,y+dy,0));g.inv.coal=2;h.inv.coal=2;m.inv={steel:4,copper:2};n.inv={steel:4,copper:2};
 let grid=campaignGrid(s);assert.notEqual(grid.machines.get(m.id),grid.machines.get(n.id));assert.equal(grid.machines.get(m.id)!.supply,300);
 const bridge=addMachine(s,'pole',x+12,y,0);grid=campaignGrid(s);assert.equal(grid.machines.get(m.id),grid.machines.get(n.id));assert.equal(grid.machines.get(m.id)!.supply,600);
 s.flow!.machines=s.flow!.machines.filter(m=>m.id!==bridge.id);s.flow!.rev++;grid=campaignGrid(s);assert.notEqual(grid.machines.get(m.id),grid.machines.get(n.id));g.inv.coal=0;assert.equal(powered(s,m),false);assert.equal(powered(s,n),true);assert.equal(powered(s,addMachine(s,'belt',x+9,y+4,1)),true);
 s.flow!.ledger=openLedger(s);for(let i=0;i<121;i++)stepFlow(s);assert.equal(m.out,0);assert.equal(n.out,10);assert.ok(conservation(s).ok,conservation(s).problems.join(';'));assert.ok(campaignApproaches(s,baseCore(s,s.campaign!.homeBlock)!).length>=2);
});
