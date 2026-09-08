import { writeFileSync } from 'node:fs';
import { createCampaign, registerBase, nominateBase, tickCampaignSchedule, makeSave, loadState, applyCommands, canPlace, machineAt, damageCore } from '../../../packages/sim/src/index';
import { threatOf } from '../../../packages/sim/src/threat';
const out='docs/evidence/p6-02-2026-09-07/';
const save=(name:string,st:ReturnType<typeof createCampaign>)=>writeFileSync(out+name+'.json',JSON.stringify(makeSave(st))+'\n');
// Labelled UI checkpoints: injected restoration/clock/radio receipt, paid turret, no balance or normal progression claim.
const st=createCampaign(3),d=st.campaign!.defence!,station=st.campaign!.expansion!.station;
d.sites=[];d.lastMinorSlot=100000;station.restoredAt=0;st.blocks[station.block].state=2;st.blocks[station.block].subOn=true;
st.campaign!.expansion!.grantedAt=0;st.campaign!.expansion!.radio.restoredAt=0;registerBase(st,station.block);nominateBase(st,station.block);
st.t=2400;tickCampaignSchedule(st,threatOf(st.flow!));save('unknown',st);
const a=d.major!;d.warning={assault:a.id,block:a.block,startsAt:a.startsAt,receivedAt:st.t,approach:'north',composition:'ordinary crawlers'};save('warning',st);
const arrival=loadState(makeSave(st));arrival.t=3298;applyCommands(arrival,[{type:'chestTake',item:'steel',n:80},{type:'chestTake',item:'copper',n:40}]);save('arrival',arrival);
st.t=a.startsAt;tickCampaignSchedule(st,threatOf(st.flow!));save('assault',st);
damageCore(st,d.bases[1],300);save('withdrawal',st);
threatOf(st.flow!).crawlers=[];tickCampaignSchedule(st,threatOf(st.flow!));save('recovery',st);
const local=createCampaign(3),ld=local.campaign!.defence!;ld.sites=[];local.t=300;tickCampaignSchedule(local,threatOf(local.flow!));save('minor',local);
const range=createCampaign(3),core=range.campaign!.defence!.bases[0];range.campaign!.defence!.sites=[];
applyCommands(range,[{type:'chestTake',item:'steel',n:80},{type:'chestTake',item:'copper',n:40}]);
let done=false;for(let y=core.y-6;y<core.y+8&&!done;y++)for(let x=core.x-6;x<core.x+8&&!done;x++)if(canPlace(range,'turret',x,y).ok){range.engineer.x=x+.5;range.engineer.y=y+3.5;applyCommands(range,[{type:'place',item:'turret',x,y}]);done=machineAt(range,x,y)?.kind==='turret';}
if(!done)throw Error('No paid turret fixture');save('range',range);
console.log('Nine UI save fixtures created; assistance declared above.');

range.campaign!.defence!.lastMinorSlot=100000;range.t=2400;tickCampaignSchedule(range,threatOf(range.flow!));range.t=3300;tickCampaignSchedule(range,threatOf(range.flow!));
const turret=range.flow!.machines.find(m=>m.kind==='turret')!,body=threatOf(range.flow!).crawlers[0];body.x=turret.x+1;body.y=turret.y+2.5;
range.engineer.x=turret.x+5;range.engineer.y=turret.y+4;save('enemy',range);
