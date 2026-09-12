import {test} from 'node:test';
import assert from 'node:assert/strict';
import * as S from '../src/index';

/** GP-POWER-FIX (2026-09-11): Generators link to Poles by footprint like every other machine, placement previews the
 *  cables it would make, Pole status names the missing source, the opening asks for the Founders Court substation cable,
 *  and the outage alert starts only once a block has actually carried supply. */
const fresh=()=>{const s=S.createCampaign();s.speed=1;return s;};
const gen=(s:S.SimState,x:number,y:number,coal=20)=>{const g=S.addMachine(s,'generator',x,y,0);g.inv.coal=coal;return g;};
const home=(s:S.SimState)=>s.campaign!.homeBlock;

test('a Pole links to a Generator when within 8 tiles of its footprint, the same rule a machine uses',()=>{
 const s=fresh();gen(s,60,353);
 const cases:[number,number,boolean,string][]=[[60,362,true,'7-tile gap south'],[60,363,false,'8-tile gap south'],[69,353,true,'7-tile gap east'],[67,360,true,'5-tile diagonal gap'],[68,361,false,'6-tile diagonal gap']];
 for(const [x,y,linked,label] of cases){const p=S.addMachine(s,'pole',x,y,0);assert.equal((S.campaignGrid(s).poles.get(p.id)?.supply??0)>0,linked,label);s.flow!.machines.pop();s.flow!.rev++;}
 // a consuming machine at the same 7-tile gap from a Pole joins it; 8 does not
 const p=S.addMachine(s,'pole',62,358,0);const ex=S.addMachine(s,'excavator',62,366,0);assert.equal(S.campaignGrid(s).machines.get(ex.id),S.campaignGrid(s).poles.get(p.id));
 s.flow!.machines.pop();s.flow!.rev++;const far=S.addMachine(s,'excavator',62,367,0);assert.equal(S.campaignGrid(s).machines.get(far.id),undefined);
});

test('placement preview lists every node a Pole or Generator would cable to and nothing when out of reach',()=>{
 const s=fresh();const g=gen(s,60,353);const p=S.addMachine(s,'pole',62,358,0);
 const both=S.powerLinksAt(s,'pole',66,360,1);assert.deepEqual(both.map(l=>l.kind).sort(),['generator','pole']);assert.ok(both.every(l=>(l.circuit?.supply??0)===300));
 assert.deepEqual(S.powerLinksAt(s,'pole',90,390,1),[]);
 assert.deepEqual(S.powerLinksAt(s,'generator',62,364,2).map(l=>l.kind),['pole'],'a Generator ghost previews the Poles that would carry it');
 const sub=S.ground(s).blocks[home(s)].sub!;assert.deepEqual(S.powerLinksAt(s,'pole',sub.x+1,sub.y+sub.size+2,1).map(l=>l.kind),['substation']);
 assert.equal(S.powerLinksAt(s,'excavator',62,362,3).length,1,'a consuming machine previews the one node it joins');
 assert.equal(S.powerLinksAt(s,'excavator',62,340,3).length,0);
 void g;void p;
});

test('Pole status and tooltip name the missing source instead of a bare no-power reading',()=>{
 const s=fresh();const lone=S.addMachine(s,'pole',62,358,0);
 assert.deepEqual(S.machineStatus(s,lone),{state:'off',reason:'no Generator or supplied Pole within reach'});assert.match(S.describeMachine(s,lone),/not connected$/);
 const g=gen(s,60,353,0);
 assert.deepEqual(S.machineStatus(s,lone),{state:'off',reason:'connected · its Generator has no fuel'});assert.match(S.describeMachine(s,lone),/connected · no fuelled source$/);
 g.inv.coal=5;s.flow!.rev++;
 assert.equal(S.machineStatus(s,lone).state,'running');assert.match(S.describeMachine(s,lone),/on the grid$/);
 assert.equal(S.campaignGrid(s).poles.get(lone.id)!.name,'Home network 2','networks are numbered in order; the block substation is network 1');
});

test('the Founders Court outage appears only after the block has carried supply, and the opening asks for the cable',()=>{
 const s=fresh();
 assert.deepEqual(S.campaignOutages(s),[],'a new game has no outage before any source exists');
 s.engineer.inv={steel:100,copper:100};
 const g=gen(s,80,370);const ex=S.addMachine(s,'excavator',84,370,1);ex.hold='steel';S.addMachine(s,'chest',88,371,0);S.addMachine(s,'belt',87,371,1);S.addMachine(s,'pole',83,372,0);
 assert.deepEqual(S.campaignOutages(s),[],'a supplied Generator elsewhere in the block is not a base outage either');
 const step=S.currentGoal(s).next!;assert.equal(step.id,'opening-power');assert.match(step.title,/Connect Founders Court/);
 const sub=S.ground(s).blocks[home(s)].sub!;assert.deepEqual(step.location,{x:sub.x+sub.size/2,y:sub.y+sub.size/2});
 S.addMachine(s,'pole',83,365,0);S.addMachine(s,'pole',82,357,0);
 assert.ok(S.campaignGrid(s).blocks[home(s)].supply>0,'the chain reaches the substation footprint');
 assert.doesNotMatch(S.currentGoal(s).next!.title,/Connect Founders Court/);
 assert.equal(s.campaign!.defence!.bases[0].poweredAt,undefined);S.advanceFlow(s,.05,[]);assert.equal(typeof s.campaign!.defence!.bases[0].poweredAt,'number');
 g.inv.coal=0;s.flow!.rev++;
 assert.equal(S.campaignOutages(s).length,1);assert.equal(S.campaignOutages(s)[0].title,'No power · Founders Court');
 g.inv.coal=1;s.flow!.rev++;assert.deepEqual(S.campaignOutages(s),[]);
 s.campaign!.defence!.bases[0].hp=0;assert.match(S.campaignOutages(s)[0].detail,/core disabled/i);
});

test('a new game raises no Power outage alert; the legacy pool event waits for lost supply and reports Power back',()=>{
 const s=fresh();const power=()=>s.events.filter(e=>e.type==='brownout'||e.type==='power-ok').map(e=>e.type+'@'+e.t);
 const run=(sec:number)=>{for(let i=0;i<sec*20;i++)S.advanceFlow(s,0.05,[]);};
 run(3);assert.deepEqual(power(),[],'the substation standing draw against no Generator is unsourced, not an outage');
 assert.equal(s.power.short,false);assert.equal(s.power.throttle,0,'machines still stay throttled with no supply');
 const g=gen(s,80,370,5);for(const [x,y] of [[83,372],[83,365],[82,357]])S.addMachine(s,'pole',x,y,0);
 run(25);assert.deepEqual(power(),[],'first supply announces nothing');assert.ok(s.campaign!.defence!.bases.some(b=>b.poweredAt!==undefined));
 g.inv.coal=0;run(3);assert.deepEqual(power(),['brownout@28'],'losing the fuelled Generator is a real outage');
 const ev=s.events.find(e=>e.type==='brownout') as {supplyKw:number};assert.equal(ev.supplyKw,0);
 run(20);assert.deepEqual(power(),['brownout@28']);g.inv.coal=5;run(2);assert.deepEqual(power().map(e=>e.split('@')[0]),['brownout','power-ok'],'refuelling after a 20 s outage announces Power back');
});
