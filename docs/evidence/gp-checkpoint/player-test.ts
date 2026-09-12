import assert from 'node:assert/strict';
import {writeFileSync} from 'node:fs';
import {createCampaign,addMachine,placementGeometryProblem,advanceFlow,campaignApproaches,baseCore,campaignGrid,conservation,openLedger,makeSave,stateProblem,ground,citySight,equipmentCommand,activeWeapon,type Kind,type Dir} from '../../../packages/sim/src/index';
const s=createCampaign(),f=s.flow!,e=s.engineer,G=ground(s),core=baseCore(s,s.campaign!.homeBlock)!;
// Controlled ordinary Home stock, NOT a natural progression/pacing run. Every round and coal unit is finite.
const plan:[Kind,number,number,Dir][]=[['chest',0,0,1],['belt',2,1,1],['assembler',3,0,1],['belt',6,1,1],['turret',7,0,1],['generator',3,4,0],['pole',6,4,0]];
let at:{x:number;y:number}|undefined;
const candidates=[];for(let y=350;y<383;y++)for(let x=60;x<111;x++)candidates.push({x,y,score:Math.hypot(x+7-85,y-374)});candidates.sort((a,b)=>a.score-b.score);
for(const p of candidates)if(plan.every(([k,dx,dy,d])=>!placementGeometryProblem(s,k,p.x+dx,p.y+dy,d))){at=p;break;}assert.ok(at,'legal Home production footprint');
const machines=plan.map(([k,dx,dy,d])=>addMachine(s,k,at!.x+dx,at!.y+dy,d)),source=machines[0],asm=machines[2],turret=machines[4],generator=machines[5];source.inv={steel:30,copper:15};generator.inv.coal=20;turret.inv.rounds=50;
const places=[];for(let y=350;y<383;y++)for(let x=60;x<112;x++)if(!placementGeometryProblem(s,'turret',x,y))places.push({x,y,score:Math.hypot(x-67,y-365)});places.sort((a,b)=>a.score-b.score);assert.ok(places.length);const second=addMachine(s,'turret',places[0].x,places[0].y,0);second.inv.rounds=50;
const q=e.equipment!;q.next=2;q.weapons['rifle:1']={kind:'rifle',loaded:10,reload:0,cooldown:0};q.slots[0]='rifle:1';e.inv.magazine=80;e.x=core.x-2;e.y=core.y+core.size+2;
f.stats.minedOf['rifle:1']=1;s.flow!.ledger=openLedger(s);const d=s.campaign!.defence!;s.t=d.clock!.nextStart-300;s.speed=1;advanceFlow(s,.05,[]);const approaches=campaignApproaches(s,core);console.log('fixture',at,'second',[second.x,second.y],'core',core,'approaches',approaches.map(t=>[t%G.tw,Math.floor(t/G.tw)]),'power',campaignGrid(s).machines.get(asm.id));
s.t=d.major!.startsAt;const seen=new Map<number,{kind:string;origin:number}>(),breaches=new Set<number>();let peak=0;
writeFileSync('docs/evidence/gp-checkpoint/player-test-save.json',JSON.stringify(makeSave(s,{logComplete:false})));
for(let i=0;i<6000&&(!d.history.length||d.major);i++){
 const enemies=f.threat!.crawlers.filter(c=>c.campaign?.layer==='major');for(const c of enemies){seen.set(c.id,{kind:c.gp?.kind??c.kind,origin:c.campaign!.origin});if(Math.hypot(c.x-core.x,c.y-core.y)<8)breaches.add(c.id);}peak=Math.max(peak,enemies.length);
 const target=enemies.filter(c=>citySight(s,e.x,e.y,c.x,c.y)&&Math.hypot(e.x-c.x,e.y-c.y)<9).sort((a,b)=>Math.hypot(a.x-e.x,a.y-e.y)-Math.hypot(b.x-e.x,b.y-e.y))[0];
 const weapon=activeWeapon(s)!;if(!weapon.loaded&&!weapon.reload&&(e.inv.magazine??0)>0)equipmentCommand(s,{type:'reload'});e.aim=target?[target.x,target.y]:null;
 advanceFlow(s,.05,[]);
}
e.aim=null;
const result={label:'Controlled finite two-turret Home check; no natural pacing claim',positions:{line:at,turret:[turret.x,turret.y],second:[second.x,second.y]},participants:seen.size,approaches:[...new Set([...seen.values()].map(v=>v.origin))].map(t=>[t%G.tw,Math.floor(t/G.tw)]),roles:[...seen.values()].reduce((a,v)=>(a[v.kind]=(a[v.kind]??0)+1,a),{} as Record<string,number>),peak,enteredCoreRadius:breaches.size,coreHp:core.hp,roundsFired:f.stats.fired+e.fired,turretRounds:f.stats.fired,playerRounds:e.fired,produced:f.stats.made.magazine,remaining:[turret.inv.rounds,second.inv.rounds,e.inv.magazine],coalBurned:f.stats.coalBurned,history:d.history,pending:d.major,conservation:conservation(s).problems};
writeFileSync('docs/evidence/gp-checkpoint/player-test-result.json',JSON.stringify(result,null,2));console.log(JSON.stringify(result,null,2));assert.equal(stateProblem(s),'');assert.ok(conservation(s).ok,conservation(s).problems.join(';'));assert.ok(seen.size>0);assert.ok(result.approaches.length>=2);
