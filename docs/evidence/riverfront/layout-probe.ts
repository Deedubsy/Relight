import {RIVERFRONT_BUILDINGS as bs,RIVERFRONT_PROPS as ps,RIVERFRONT,createCampaign,ground,findPath,blockOfTile,passable} from '../../../packages/sim/src/index';
const intersects=(a:any,b:any)=>a.x<b.x+b.w&&a.x+a.w>b.x&&a.y<b.y+b.h&&a.y+a.h>b.y;
console.log('buildings',bs.flatMap((a,i)=>bs.slice(i+1).filter(b=>intersects(a,b)).map(b=>[a.id,b.id])));
console.log('yards/buildings',RIVERFRONT.yards.flatMap((a,i)=>bs.filter(b=>intersects(a,b)).map(b=>[i,b.id])));
console.log('props',ps.filter(p=>p.kind!=='furniture'&&p.kind!=='gate'&&p.kind!=='fence').flatMap(a=>bs.filter(b=>intersects(a,b)).map(b=>[a.id,b.id])));
const s=createCampaign(),g=ground(s);console.log('tram terrain blocked',s.campaign!.fixedTram!.route.filter(t=>!passable(s,t%360,Math.floor(t/360))));
console.log('plants',s.campaign!.progression!.sites.filter(p=>p.kind==='plant').map(p=>[p.id,p.block]));
