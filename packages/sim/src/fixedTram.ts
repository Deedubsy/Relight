/** Four permanent campaign platforms; rail and vehicle are city infrastructure. */
import {DARK,HELD,type SimState} from './types';
import { ground, blockOfTile } from './ground';
import { addMachine, machineAt, machineRunning, type Machine } from './flow';
export interface FixedTram { version:1; route:number[]; stops:number[]; tram:number }
export function initFixedTram(st:SimState):void {
  const c=st.campaign,e=c?.expansion,d=c?.districts,f=st.flow;
  if(!c||!e||!d||!f||c.fixedTram)return;
  const G=ground(st),route=[...e.route,...d.route.slice(1)],tiles=new Set(route);
  const pads:[number,number][]=[...e.stops,d.stop];
  const reserved=[e.station,e.radio,d.station,d.workshop,...d.sources,
    ...(c.discovery?[{...c.discovery,size:3}]:[]),...(c.recruits?.sites.map(s=>({...s,size:1}))??[]),...(c.turbine?[c.turbine]:[])];
  const touches=(x:number,y:number,t:number)=>{
    const tx=t%f.tw,ty=Math.floor(t/f.tw);
    return (tx===x-1||tx===x+2)&&ty>=y&&ty<y+2||(ty===y-1||ty===y+2)&&tx>=x&&tx<x+2;
  };
  const clear=(x:number,y:number)=>{
    for(let yy=y;yy<y+2;yy++)for(let xx=x;xx<x+2;xx++){
      const t=yy*f.tw+xx;
      if(xx<0||yy<0||xx>=f.tw||yy>=G.th||G.owner[t]===-2||G.urban?.solid[t]||G.patch[t]||G.rank[t]>=0||G.opening?.gate.includes(t)||tiles.has(t)||f.occ[t]!==undefined)return false;
      if(reserved.some(s=>xx>=s.x&&xx<s.x+s.size&&yy>=s.y&&yy<s.y+s.size))return false;
      if(pads.some(([px,py])=>xx>=px&&xx<px+2&&yy>=py&&yy<py+2))return false;
    }
    const bi=blockOfTile(st,x,y);return bi>=0&&(st.blocks[bi].state===DARK||st.blocks[bi].state===HELD)&&blockOfTile(st,x+1,y+1)===bi;
  };
  const candidates:[number,number][]=[];
  for(const t of route){const x=t%f.tw,y=Math.floor(t/f.tw);
    for(const [xx,yy]of [[x-2,y],[x+1,y],[x,y-2],[x,y+1],[x-2,y-1],[x+1,y-1],[x-1,y-2],[x-1,y+1]])if(clear(xx,yy))candidates.push([xx,yy]);
  }
  const occupiedBlocks=new Set(pads.map(([x,y])=>blockOfTile(st,x,y)));
  const separation=([x,y]:[number,number])=>(occupiedBlocks.has(blockOfTile(st,x,y))?0:10000)+Math.min(...pads.map(([px,py])=>Math.hypot(x-px,y-py)));
  candidates.sort((a,b)=>separation(b)-separation(a)||a[1]-b[1]||a[0]-b[0]);
  if(!candidates.some(([x,y])=>!occupiedBlocks.has(blockOfTile(st,x,y)))){
    // Short surveys can border only the original three districts. Continue along existing
    // streets to a fourth district instead of placing two platforms on Home's power circuit.
    const start=route.at(-1)!,prev=new Map<number,number>([[start,start]]),queue=[start];let found=false;
    for(let head=0;head<queue.length&&!found;head++){
      const t=queue[head],x=t%f.tw,y=Math.floor(t/f.tw);
      if(!occupiedBlocks.has(blockOfTile(st,x,y)))for(const [xx,yy]of [[x-2,y],[x+1,y],[x,y-2],[x,y+1],[x-2,y-1],[x+1,y-1],[x-1,y-2],[x-1,y+1]]){
        if(!clear(xx,yy)||occupiedBlocks.has(blockOfTile(st,xx,yy)))continue;
        const extension=[t];while(extension.at(-1)!==start)extension.push(prev.get(extension.at(-1)!)!);extension.reverse();
        if(extension.some(q=>q%f.tw>=xx&&q%f.tw<xx+2&&Math.floor(q/f.tw)>=yy&&Math.floor(q/f.tw)<yy+2))continue;
        route.push(...extension.slice(1));for(const q of extension)tiles.add(q);candidates.unshift([xx,yy]);found=true;break;
      }
      for(const [xx,yy]of [[x,y-1],[x+1,y],[x,y+1],[x-1,y]]){
        const q=yy*f.tw+xx;
        if(xx<0||yy<0||xx>=f.tw||yy>=G.th||prev.has(q)||tiles.has(q)||G.owner[q]!==-1||G.urban?.solid[q]||f.occ[q]!==undefined&&machineAt(st,xx,yy)?.kind!=='track')continue;
        if(reserved.some(s=>xx>=s.x&&xx<s.x+s.size&&yy>=s.y&&yy<s.y+s.size)||pads.some(([px,py])=>xx>=px&&xx<px+2&&yy>=py&&yy<py+2))continue;
        prev.set(q,t);queue.push(q);
      }
    }
    if(!found)throw new Error('fixed tram requires a clear fourth district platform');
  }
  pads.push(candidates[0]);
  // Existing saves keep all cargo and machinery. Adopt the original platforms where possible;
  // if a player used a reservation, choose the nearest vacant platform on the same district line.
  const stops:Machine[]=[];
  for(const [index,[x,y]]of pads.entries()){
    const existing=machineAt(st,x,y);
    if(existing?.kind==='tramstop'&&existing.x===x&&existing.y===y){stops.push(existing);continue;}
    const vacant=[0,1,f.tw,f.tw+1].every(offset=>f.occ[y*f.tw+x+offset]===undefined&&!G.opening?.gate.includes(y*f.tw+x+offset));
    let at:[number,number]=[x,y];
    if(!vacant){
      const alternate=candidates.filter(([xx,yy])=>clear(xx,yy)&&blockOfTile(st,xx,yy)===blockOfTile(st,x,y))
        .sort((a,b)=>Math.hypot(a[0]-x,a[1]-y)-Math.hypot(b[0]-x,b[1]-y))[0];
      if(!alternate)throw new Error('fixed tram platform reservation is occupied; no clear nearby platform');
      at=alternate;
    }
    stops.push(addMachine(st,'tramstop',...at,0));
    if(index<2)e.stops[index]=at;else if(index===2)d.stop=at;
  }
  for(const t of route)if(f.occ[t]===undefined)addMachine(st,'track',t%f.tw,Math.floor(t/f.tw),0);
  stops.sort((a,b)=>route.findIndex(t=>touches(a.x,a.y,t))-route.findIndex(t=>touches(b.x,b.y,t)));
  const tram=f.machines.find(m=>m.kind==='tram'&&tiles.has(m.y*f.tw+m.x))??addMachine(st,'tram',route[0]%f.tw,Math.floor(route[0]/f.tw),0);
  c.fixedTram={version:1,route,stops:stops.map(s=>s.id),tram:tram.id};
  // A pending old kit is retained for collection, but restoration no longer creates one.
  f.rev++;
}
export function fixedStops(st:SimState):Machine[]{
  const n=st.campaign?.fixedTram;return n?n.stops.map(id=>st.flow!.machines.find(m=>m.id===id)!).filter(Boolean):[];
}
export function fixedPoweredStops(st:SimState):Machine[]{return fixedStops(st).filter(m=>machineRunning(st,m));}
export function fixedTramStatus(st:SimState):string {
  const count=fixedPoweredStops(st).length;
  return `${count}/4 stops powered · ${count<2?'Power at least two stops to start automatic service.':'Automatic service between powered stops.'}`;
}
export function fixedTramProblem(st:SimState):string {
  const n=st.campaign?.fixedTram;if(!n)return '';
  const f=st.flow;if(!f||n.version!==1||!Array.isArray(n.route)||n.route.length<2||!Array.isArray(n.stops)||n.stops.length!==4||new Set(n.stops).size!==4)return 'invalid fixed tram network';
  const tw=f.tw,th=st.city!.th;
  if(new Set(n.route).size!==n.route.length||n.route.some((t,i)=>!Number.isInteger(t)||t<0||t>=tw*th||i>0&&Math.abs(t%tw-n.route[i-1]%tw)+Math.abs(Math.floor(t/tw)-Math.floor(n.route[i-1]/tw))!==1))return 'invalid fixed tram route';
  if(n.stops.some(id=>!Number.isInteger(id)||!f.machines.some(m=>m.id===id&&m.kind==='tramstop'&&n.route.some(t=>{const x=t%tw,y=Math.floor(t/tw);return (x===m.x-1||x===m.x+2)&&y>=m.y&&y<m.y+2||(y===m.y-1||y===m.y+2)&&x>=m.x&&x<m.x+2;}))))return 'invalid fixed tram platform';
  if(!f.machines.some(m=>m.id===n.tram&&m.kind==='tram'&&n.route.includes(m.y*tw+m.x)))return 'invalid automatic tram';
  return '';
}
