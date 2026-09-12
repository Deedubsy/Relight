import assert from 'node:assert/strict';
import {writeFileSync} from 'node:fs';
import {createCampaign,addMachine,advanceFlow,ground,fixedStops,progressionCommand,tickCampaignSchedule,stateProblem,makeSave,openLedger} from '../../../packages/sim/src/index';
// Declared supplied workload: all fresh camps remain, plus a committed Home raid,
// working production, connected lamps, and physical freight to a commissioned plant.
const s=createCampaign(),p=s.campaign!.progression!,g=p.gameplay!,plant=p.sites.find(x=>x.id==='plant:ironworks')!,core=p.sites.find(x=>x.item==='core1')!;
core.recovered=true;core.enabled=false;g.strongholds.freight={keys:[],opened:true,recovered:true,inherited:true};s.engineer.inv={core1:1,steel:30,copper:15};s.engineer.x=plant.x-.5;s.engineer.y=plant.y+.5;progressionCommand(s,{type:'deliver',id:plant.id});progressionCommand(s,{type:'activate',id:plant.id});s.engineer.x=70.5;s.engineer.y=363.5;
const gen=addMachine(s,'generator',94,355,0);gen.inv.coal=100;
const a=addMachine(s,'assembler',73,363,3);a.inv.steel=100;a.inv.copper=50;
for(let x=69;x<=72;x++)addMachine(s,'belt',x,364,3);
const tower=addMachine(s,'turret',67,364,0);tower.inv.rounds=200;
addMachine(s,'lamp',80,363,0);addMachine(s,'lamp',68,363,0);
const stops=fixedStops(s);stops[0].inv.magazine=120;stops[0].freight={magazine:{request:0,reserve:0,export:true}};stops[2].freight={magazine:{request:120,reserve:0,export:false}};
const d=s.campaign!.defence!,start=(d.clock!.nextStart=900);s.t=start-300;tickCampaignSchedule(s,s.flow!.threat!);s.t=start;tickCampaignSchedule(s,s.flow!.threat!);s.speed=1;s.flow!.ledger=openLedger(s);
advanceFlow(s,45,[],901);assert.equal(stateProblem(s),'');
// Focused night-time workload clock and supplied buffers, never ordinary acquisition evidence.
a.out=0;stops[0].inv.magazine=120;stops[2].cargo={};s.flow!.ledger=openLedger(s);
s.engineer.x=95.5;s.engineer.y=286.5;assert.equal(stateProblem(s),'');
writeFileSync('docs/evidence/gp-checkpoint/workload-save.json',JSON.stringify(makeSave(s,{logComplete:false})));
const madeBefore=a.observation?.produced.magazine??0;const samples:number[]=[];let maxLiving=0,maxEngaged=0;for(let i=0;i<200;i++){const now=performance.now();advanceFlow(s,.05,[],2);samples.push(performance.now()-now);maxLiving=Math.max(maxLiving,s.flow!.threat!.crawlers.length);maxEngaged=Math.max(maxEngaged,s.flow!.threat!.crawlers.filter(c=>c.onPlayer||c.gp&&c.gp.phase!=='idle').length);}
samples.sort((a,b)=>a-b);writeFileSync('docs/evidence/gp-checkpoint/workload-tick.json',JSON.stringify({label:'Supplied combined workload; not acquisition or reference-machine performance certification',ticks:samples.length,medianMs:samples[100],p95Ms:samples[190],maxMs:samples.at(-1),maxLiving,maxEngaged,production:a.observation?.produced,producedDuringSample:(a.observation?.produced.magazine??0)-madeBefore,freight:stops[2].cargo,homeHp:d.bases[0].hp},null,2));
