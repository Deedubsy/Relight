import {test} from 'node:test';
import assert from 'node:assert/strict';
import * as S from '../src/index';

/** GP-HOME-REPAIR (2026-09-11): the Home base core is the workshop building, so its health and repair live in the Home
 *  workshop card. `repairCost` is the single price the card, the Management panel and the hover text read. */
const run=(s:S.SimState,sec:number)=>S.advanceFlow(s,sec,[],Math.ceil(sec*20)+1);
function atHome(){const s=S.createCampaign();s.speed=1;const core=S.baseCore(s,s.campaign!.homeBlock)!;s.engineer.x=core.x-0.5;s.engineer.y=core.y+core.size/2;assert.ok(S.inReach(s,core.x,core.y,core.size));return {s,core};}

test('repairCost prices a damaged Home core at 2 steel + 1 copper and a disabled one at the 10 + 5 recovery kit, and names it as the Home core',()=>{
 const {s,core}=atHome();
 assert.equal(S.repairCost(s,core.x,core.y)?.hp,300);assert.equal(S.repairCheck(s,core.x,core.y),'nothing damaged here');
 core.hp=100;const dmg=S.repairCost(s,core.x,core.y)!;assert.deepEqual({...dmg},{kind:'core',hp:100,max:300,steel:2,copper:1,seconds:4,recommission:false,home:true});
 assert.match(S.defenceDescription(s,core.x,core.y),/100\/300 HP · E opens the Home workshop — repair it there \(2 steel \+ 1 copper, 4s \/ 40 HP\)$/);
 core.hp=0;const dis=S.repairCost(s,core.x,core.y)!;assert.deepEqual({...dis},{kind:'core',hp:0,max:300,steel:10,copper:5,seconds:12,recommission:true,home:true});
 assert.match(S.defenceDescription(s,core.x,core.y),/DISABLED · E opens the Home workshop — repair it there \(10 steel \+ 5 copper, 12s\)$/);
 assert.equal(S.repairCost(s,core.x+core.size+5,core.y),null,'an ordinary tile has nothing to repair');
});

test('the starting Backpack stake is exactly one recovery kit: a disabled Home core recommissions from the workshop and turns the substation back on',()=>{
 const {s,core}=atHome();core.hp=0;s.blocks[core.block].subOn=false;
 assert.equal(S.repairCheck(s,core.x,core.y),'');
 S.applyCommands(s,[{type:'repairDefence',x:core.x,y:core.y}]);
 assert.equal(s.engineer.inv.steel,10);assert.equal(s.engineer.inv.copper??0,0,'the kit is paid once, up front');
 assert.equal(s.campaign!.defence!.repair?.remaining,12);assert.equal(S.repairCheck(s,core.x,core.y),'a repair is already in progress');
 run(s,12.1);assert.equal(core.hp,300);assert.equal(s.blocks[core.block].subOn,true);assert.equal(s.campaign!.defence!.repair,null);
 assert.equal(S.repairCheck(s,core.x,core.y),'nothing damaged here');
});

test('a machine repair keeps its own price and the E-repairs hover text',()=>{
 const s=S.createCampaign();s.speed=1;const w=S.addMachine(s,'wall',s.engineer.x+2|0,s.engineer.y|0,0);w.hp=10;
 const c=S.repairCost(s,w.x,w.y)!;assert.equal(c.kind,'machine');assert.equal(c.home,false);assert.equal(c.steel,2);assert.equal(c.copper,1);assert.equal(c.max,S.DEFENCE.wallHp);
 assert.match(S.defenceDescription(s,w.x,w.y),/Wall · 10\/120 HP · E repairs: 2 steel \+ 1 copper, 4s \/ 40 HP$/);
});
