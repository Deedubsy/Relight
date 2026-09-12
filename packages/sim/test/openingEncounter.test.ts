import {test} from 'node:test';
import assert from 'node:assert/strict';
import * as S from '../src/index';

/** GP-OPENING (2026-09-11): the introductory attack, its guidance and its raid-director integration. */
const RIFLE={version:1,next:2,weapons:{'rifle:1':{kind:'rifle' as const,loaded:0,cooldown:0,reload:0}},slots:['rifle:1',null] as (string|null)[],active:0};
function fixture(){
 const s=S.createCampaign(),e=s.engineer;e.inv={steel:200,copper:200};e.equipment=JSON.parse(JSON.stringify(RIFLE));
 // Excavator on real Founders Court rubble so the extraction line keeps running through the whole check.
 const gen=S.addMachine(s,'generator',60,353,0);gen.inv.coal=30;const ex=S.addMachine(s,'excavator',56,349,1);
 S.addMachine(s,'belt',59,350,1);S.addMachine(s,'chest',60,349,0);S.addMachine(s,'pole',59,352,0);
 // Turrets are powered machines (2026-09-11): a second Pole carries the Home network to the turret ground.
 S.addMachine(s,'pole',62,358,0);
 // GP-POWER-FIX (2026-09-11): the opening now asks for the Founders Court substation cable before the rifle; two Poles carry the network to its footprint.
 S.addMachine(s,'pole',67,352,0);S.addMachine(s,'pole',75,352,0);
 const turret=S.addMachine(s,'turret',63,361,2);s.speed=1;
 return {s,e,turret,d:s.campaign!.defence!,T:s.flow!.threat!,core:S.baseCore(s,s.campaign!.homeBlock)!};
}
const step=(s:S.SimState,n=1)=>{for(let i=0;i<n;i++)S.advanceFlow(s,.05,[]);};
const group=(s:S.SimState)=>s.flow!.threat!.crawlers.filter(c=>c.campaign?.group===s.campaign!.defence!.opening!.id);
const inBounds=(s:S.SimState,x:number,y:number)=>{const b=S.ground(s).opening!.bounds;return x>=b.x&&x<b.x+b.size&&y>=b.y&&y<b.y+b.size;};

test('opening encounter constants keep the intro clear of the scheduled director',()=>{
 assert.equal(S.OPENING_ENCOUNTER.guard,S.ACTIVE_RAIDS.warning+S.OPENING_ENCOUNTER.recovery+60);
 assert.ok(S.OPENING_ENCOUNTER.warning>=20&&S.OPENING_ENCOUNTER.warning<=30);
 assert.ok(S.OPENING_ENCOUNTER.count>=4&&S.OPENING_ENCOUNTER.count<=6);
 assert.equal(S.OPENING_ENCOUNTER.recovery,S.ACTIVE_RAIDS.recovery);
});

test('a fully loaded first turret schedules one warned introductory attack that is repelled, counted and never repeated',()=>{
 const {s,e,turret,d,core}=fixture();const G=S.ground(s);
 assert.equal(d.opening?.status,'pending');
 assert.match(S.currentGoal(s).next!.title,/Prepare your first turret/);
 assert.deepEqual(S.currentGoal(s).next!.resources,[{item:'magazine',required:S.TURRET_HOPPER,available:0}]);
 turret.inv.rounds=S.TURRET_HOPPER-1;step(s);assert.equal(d.opening!.status,'pending');
 assert.match(S.currentGoal(s).next!.detail,/1 more to fill/);
 const before={...d.clock!};
 turret.inv.rounds=S.TURRET_HOPPER;step(s);
 const op=d.opening!;assert.equal(op.status,'scheduled');assert.equal(op.turretId,turret.id);assert.equal(op.count,S.OPENING_ENCOUNTER.count);
 assert.equal(op.startsAt,op.scheduledAt!+S.OPENING_ENCOUNTER.warning);assert.equal(d.clock!.nextStart,before.nextStart);assert.equal(d.notice,'');
 assert.equal(S.stateProblem(s),'');
 const ox=op.origin!%G.tw,oy=Math.floor(op.origin!/G.tw);assert.equal(inBounds(s,ox,oy),false,'spawns outside the perimeter');
 const view=S.openingEncounter(s)!;assert.match(S.campaignWarning(s)!,new RegExp(`^Small enemy group approaching from the ${view.direction}\\. Stay near your turret and help defend\\.`));
 assert.deepEqual(S.knownCampaignThreat(s),{block:s.campaign!.homeBlock,phase:'approaching',x:ox+.5,y:oy+.5});
 assert.match(S.campaignAlerts(s)[0].title,new RegExp(`Small enemy group approaching · from the ${view.direction}`));
 const goal=S.currentGoal(s).next!;assert.equal(goal.id,'opening-attack');assert.equal(goal.text,`Small enemy group approaching from the ${view.direction}. Stay near your turret and help defend.`);assert.deepEqual(goal.location,{x:ox+.5,y:oy+.5});
 // Save/load during the warning keeps one scheduled encounter.
 const mid=S.loadState(S.makeSave(s));assert.equal(S.stateProblem(mid),'');assert.equal(mid.campaign!.defence!.opening!.id,op.id);assert.equal(mid.campaign!.defence!.opening!.status,'scheduled');
 // Unloading the turret does not cancel a scheduled attack.
 turret.inv.rounds=0;step(s);assert.equal(d.opening!.status,'scheduled');turret.inv.rounds=S.TURRET_HOPPER;
 const t0=s.t;while(d.opening!.status==='scheduled'&&s.t<t0+60)step(s);
 assert.equal(d.opening!.status,'active');assert.ok(Math.abs(s.t-op.startsAt!)<.1);
 const born=group(s);assert.equal(born.length,S.OPENING_ENCOUNTER.count);assert.ok(born.every(c=>c.gp?.kind==='skitter'&&!inBounds(s,c.x,c.y)));
 assert.equal(d.minor!.id,op.id);assert.equal(d.raidsStarted,0);assert.equal(S.stateProblem(s),'');
 assert.match(S.currentGoal(s).next!.title,/Defend your turret/);assert.match(S.campaignWarning(s)!,/attacking from the/);
 const active=S.loadState(S.makeSave(s));assert.equal(S.stateProblem(active),'');assert.equal(active.campaign!.defence!.opening!.status,'active');assert.equal(active.campaign!.defence!.minor!.id,op.id);
 e.x=core.x-3;e.y=core.y-3;const rounds=turret.inv.rounds;const fired=s.flow!.stats.fired;
 let i=0;while(d.opening!.status==='active'&&i<20000){step(s);i++;}
 assert.equal(d.opening!.status,'repelled');assert.equal(group(s).length,0);assert.equal(core.hp,S.baseCore(s,s.campaign!.homeBlock)!.hp);
 assert.ok(d.opening!.shots>0);assert.equal(d.opening!.shots,rounds-turret.inv.rounds,'shots counted are the rounds actually consumed');assert.equal(d.opening!.shots,s.flow!.stats.fired-fired);
 assert.equal(d.clock!.recoveryUntil,s.t+S.OPENING_ENCOUNTER.recovery);assert.equal(d.clock!.nextStart,before.nextStart);assert.ok(d.clock!.nextMinor>=s.t+S.OPENING_ENCOUNTER.recovery);
 assert.equal(d.history.length,0);assert.equal(d.raidsStarted,0);assert.equal(d.minor,null);
 const after=S.currentGoal(s).next!;assert.equal(after.title,'Attack repelled');assert.equal(after.text,`Attack repelled. Your turret used ${d.opening!.shots} bullets. Connect ammunition production to keep it supplied.`);
 // Refilling, unloading, another turret or a reload never retrigger the encounter.
 turret.inv.rounds=S.TURRET_HOPPER;step(s);assert.equal(d.opening!.status,'repelled');assert.equal(d.minor,null);
 const t2=S.addMachine(s,'turret',70,362,2);t2.inv.rounds=S.TURRET_HOPPER;step(s);assert.equal(d.opening!.status,'repelled');
 const done=S.loadState(S.makeSave(s));assert.equal(S.stateProblem(done),'');step(done,3);assert.equal(done.campaign!.defence!.opening!.status,'repelled');assert.equal(done.campaign!.defence!.minor,null);assert.equal(done.campaign!.defence!.opening!.shots,d.opening!.shots);
 // The victory message yields to the replenishment objective after the acknowledgement window.
 s.t+=S.OPENING_ENCOUNTER.ack;step(s);
 assert.equal(S.currentGoal(s).next!.id,'opening-ammo');assert.match(S.currentGoal(s).next!.title,/Automate your turret’s ammunition supply/);
});

test('replenishment completes only when produced magazines reach the turret by a connected route, then asks for three turrets',()=>{
 const {s,d,turret}=fixture();
 d.opening={version:1,status:'repelled',shots:10,id:d.nextId++,turretId:turret.id,origin:328367,scheduledAt:0,startsAt:25,endedAt:40,count:5};
 s.t=200;turret.inv.rounds=40;assert.equal(S.stateProblem(s),'');
 const goal=()=>S.currentGoal(s).next!;
 assert.equal(goal().text,'Build 1 Assembler and set it to Shot magazines');
 const gen=S.addMachine(s,'generator',56,365,0);gen.inv.coal=30;S.addMachine(s,'pole',60,364,0);
 const asm=S.addMachine(s,'assembler',58,361,1);asm.inv.steel=8;asm.inv.copper=8;step(s);
 assert.ok(S.powered(s,asm),'fixture assembler is powered');
 assert.equal(goal().text,'Connect the Assembler output to your turret');
 const away=S.addMachine(s,'belt',61,362,0);s.flow!.rev++;
 assert.match(goal().text,/Connect the Assembler output|Extend the route/);
 away.dir=1;S.addMachine(s,'belt',62,362,1);s.flow!.rev++;
 assert.ok(S.supplyChainReaches(s,asm,turret));assert.match(goal().text,/waiting for the Assembler to produce|waiting for the first one/);
 assert.equal(d.opening.suppliedAt,undefined);
 // Hand loading is not automation.
 turret.inv.rounds=S.TURRET_HOPPER;step(s);assert.equal(d.opening.suppliedAt,undefined);turret.inv.rounds=40;
 let i=0;while(d.opening.suppliedAt===undefined&&i<6000){step(s);i++;}
 assert.notEqual(d.opening.suppliedAt,undefined,'a produced magazine arrived by belt');
 assert.ok(turret.inv.rounds>40);assert.ok((asm.observation?.produced.magazine??0)>0);
 assert.equal(goal().title,'Automatic resupply working');assert.equal(goal().text,'Automatic resupply working. Your production line is replenishing the turret.');
 s.t+=S.OPENING_ENCOUNTER.supplyAck;step(s);
 assert.match(goal().title,/Expand your defences \(1\/3\)/);assert.match(goal().text,/^Larger attacks can approach from any direction\. Build two more turrets and spread your defences around the base\./);
 assert.ok(goal().resources!.some(r=>r.item==='steel'),'lists the next turret’s materials');
 const t2=S.addMachine(s,'turret',70,362,2);assert.match(goal().title,/Expand your defences \(2\/3\)/);assert.match(goal().text,/Build one more turret and/);
 const t3=S.addMachine(s,'turret',75,362,2);assert.equal(goal().title,'Load your new turret');
 t2.inv.rounds=10;t3.inv.rounds=10;assert.equal(goal().title,'Prepare to scout');
 const copy=S.loadState(S.makeSave(s));assert.equal(S.stateProblem(copy),'');assert.equal(copy.campaign!.defence!.opening!.suppliedAt,d.opening.suppliedAt);
});

test('progressed saves skip the introductory attack while fresh saves keep it pending',()=>{
 const fresh=fixture().s;fresh.t=120;const save=S.makeSave(fresh);delete (save as any).state.campaign.defence.opening;
 assert.equal(S.loadState(save).campaign!.defence!.opening!.status,'pending');
 const veteran=fixture().s;veteran.t=500;veteran.campaign!.defence!.history.push({id:1,block:0,started:100,ended:400,defeated:false,spawned:12});
 const vsave=S.makeSave(veteran);delete (vsave as any).state.campaign.defence.opening;
 const loaded=S.loadState(vsave);assert.equal(loaded.campaign!.defence!.opening!.status,'skipped');
 const t=S.machineById(loaded,fixture().turret.id)!;t.inv.rounds=S.TURRET_HOPPER;loaded.speed=1;step(loaded,3);
 assert.equal(loaded.campaign!.defence!.opening!.status,'skipped');assert.ok(!loaded.campaign!.defence!.minor);
 const armed=fixture().s;armed.t=300;for(const x of [70,75])S.addMachine(armed,'turret',x,362,2);
 const asave=S.makeSave(armed);delete (asave as any).state.campaign.defence.opening;
 assert.equal(S.loadState(asave).campaign!.defence!.opening!.status,'skipped');
});

test('an imminent first major assault is deferred by the explained guard, and a near-warning one skips the intro',()=>{
 const a=fixture();a.d.clock!.nextStart=a.s.t+400;a.turret.inv.rounds=S.TURRET_HOPPER;step(a.s);
 assert.equal(a.d.opening!.status,'scheduled');assert.equal(a.d.clock!.nextStart,a.d.opening!.scheduledAt!+S.OPENING_ENCOUNTER.guard);assert.match(a.d.notice,/deferred .* introductory attack/);
 const b=fixture();b.d.clock!.nextStart=b.s.t+S.ACTIVE_RAIDS.warning-1;b.turret.inv.rounds=S.TURRET_HOPPER;step(b.s);
 assert.equal(b.d.opening!.status,'skipped');assert.equal(b.d.minor,null);assert.equal(S.stateProblem(b.s),'');
});
