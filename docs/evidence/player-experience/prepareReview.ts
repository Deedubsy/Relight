/** Supplied local review setups, not natural campaign progression. Run from repo root. */
import {writeFileSync} from 'node:fs';
import * as S from '../../../packages/sim/src/index';
{

const notes:any[]=[];
for(const kind of ['heart','furnace','crown','relay','artifact']){
 const s=S.createCampaign(),p=s.campaign!.progression!,site=p.sites.find(x=>kind==='relay'?x.item==='core2':kind==='artifact'?x.kind==='artifact':x.kind===kind)!;s.speed=1;
 s.engineer.inv={steel:600,copper:1000,coal:150,magazine:20,shell:40};site.seen=true;
 if(kind==='furnace'||kind==='crown'){p.arsenal=true;s.engineer.barrels=2;}
 const near=(x:number,y:number)=>{s.engineer.x=x-.5;s.engineer.y=y+.5;s.engineer.block=S.blockOfTile(s,s.engineer.x,s.engineer.y);};
 const build=(k:S.Kind,filter=(x:number,y:number)=>true)=>{for(let r=3;r<=12;r++)for(let dy=-r;dy<=r;dy++)for(const dx of [-r,r]){const x=site.x+dx,y=site.y+dy;if(Math.hypot(dx,dy)>14||S.blockOfTile(s,x,y)!==site.block||!filter(x,y))continue;near(x,y);if(S.canPlace(s,k,x,y).ok)return S.place(s,k,x,y)!;}throw Error('No '+kind+' '+k);};
 if(!['artifact','relay'].includes(kind)){
  for(let i=0;i<2;i++)build('generator').inv.coal=50;
  if(kind==='heart'){
   const sub=S.ground(s).blocks[site.block].sub!;Object.assign(s.campaign!.recruits!.sites.find(r=>r.kind==='electricians')!,{seenAt:0,recruitedAt:0});
   for(const side of [-1,1]){const pole=build('pole',x=>side<0?x<site.x:x>=site.x+3);const path=S.findPath(s,sub.x-1,sub.y+sub.size,pole.x-1,pole.y);if(!path)throw Error('No feeder route');if(side>0)continue;for(let i=0;i<path.length;i+=3){const x=path[i]%s.flow!.tw,y=Math.floor(path[i]/s.flow!.tw);near(x,y);if(S.canPlace(s,'bigpole',x,y).ok)S.place(s,'bigpole',x,y);}}
  }
  if(kind==='furnace')for(let n=0;n<2;n++){
   const m=build('assembler',(x,y)=>S.canPlace(s,'belt',x+3,y,1).ok&&S.canPlace(s,'chest',x+4,y,1).ok);m.inv={steel:80,copper:40};S.place(s,'belt',m.x+3,m.y,1);S.place(s,'chest',m.x+4,m.y,1);
  }
  if(kind==='crown'){build('lamp');build('lamp');}
  for(let n=0;n<2;n++)build(kind==='heart'?'turret':'cannon').inv=kind==='heart'?{rounds:50}:{shell:20};
  near(site.x,site.y);S.progressionCommand(s,{type:'deliver',id:site.id});
 }
 if(kind==='relay'){build('generator').inv.coal=30;s.engineer.inv.lamp=2;}
 near(site.x,site.y);s.engineer.x=site.x-4;s.engineer.y=site.y+4;s.engineer.block=S.blockOfTile(s,s.engineer.x,s.engineer.y);s.flow!.ledger=S.openLedger(s);
 writeFileSync(`.tmp-pe-${kind}.json`,JSON.stringify(S.makeSave(s)));
 notes.push({kind,site:site.id,status:S.progressionStatus(s,site),blocker:S.encounterBlocker(s,site),reward:S.progressionReward(site),machines:s.flow!.machines.filter(m=>!['tram','tramstop','track','depot'].includes(m.kind)).map(m=>[m.kind,m.x,m.y])});
}
writeFileSync('docs/evidence/player-experience/setups.json',JSON.stringify(notes,null,2));console.log(notes.map(n=>({kind:n.kind,status:n.status,blocker:n.blocker})));

}
{

const s=S.createCampaign();s.engineer.inv={steel:300,copper:100,coal:100,ironore:100};const stops=S.fixedStops(s);
for(const stop of stops.slice(0,2)){for(let r=3;r<16;r++){let done=false;for(let dy=-r;dy<=r&&!done;dy++)for(const dx of [-r,r]){const x=stop.x+dx,y=stop.y+dy;if(S.blockOfTile(s,x,y)!==S.blockOfTile(s,stop.x,stop.y))continue;if(S.canPlace(s,'generator',x,y).ok){S.place(s,'generator',x,y)!.inv.coal=30;done=true;break;}}if(done)break;}}
const source=stops[0],dest=stops[1];source.inv.ironore=100;source.freight={ironore:{request:0,reserve:0,export:true}};dest.freight={ironore:{request:100,reserve:0,export:false}};
const station=s.campaign!.expansion!.station;s.engineer.x=station.x-1;s.engineer.y=station.y;s.engineer.block=station.block;S.deliverSite(s,'station');const why=S.restoreSite(s,'station');if(why)throw Error(why);S.initTruck(s);const t=s.campaign!.truck!;s.engineer.x=t.x-3;s.engineer.y=t.y;s.engineer.block=S.blockOfTile(s,s.engineer.x,s.engineer.y);s.flow!.ledger=S.openLedger(s);s.speed=1;
writeFileSync('.tmp-pe-transport.json',JSON.stringify(S.makeSave(s)));console.log(S.fixedTramStatus(s),t&&{x:t.x,y:t.y});

}
{

const s=S.createCampaign();s.engineer.inv={steel:300,copper:150,coal:60,magazine:20};const g=S.place(s,'generator',63,358)!;g.inv.coal=30;
for(let line=0;line<3;line++){let done=false;for(let y=365;y<379&&!done;y++)for(let x=60;x<81;x++){const parts:[S.Kind,number][]=[['chest',0],['belt',2],['turret',3]];if(parts.every(([k,dx])=>S.canPlace(s,k,x+dx,y,1).ok)){for(const [k,dx]of parts){const m=S.place(s,k,x+dx,y,1)!;if(k==='chest')m.inv.magazine=10;}done=true;break;}}if(!done)throw Error('No supplied line');}
s.t=899;s.campaign!.defence!.lastMinorSlot=-1;s.engineer.x=70.5;s.engineer.y=359.5;s.engineer.block=0;s.flow!.ledger=S.openLedger(s);s.speed=1;writeFileSync('.tmp-pe-defence.json',JSON.stringify(S.makeSave(s)));console.log(s.flow!.machines.filter(m=>m.kind==='turret').map(m=>[m.x,m.y]));

}