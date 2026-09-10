import * as S from '../../../packages/sim/src/index';import {writeFileSync}from'node:fs';const s=S.createCampaign(),g=S.ground(s),bs=S.RIVERFRONT_BUILDINGS,ps=S.RIVERFRONT_PROPS,c=S.RIVERFRONT,W=g.tw;
const hit=(a:any,b:any)=>a.x<b.x+b.w&&a.x+a.w>b.x&&a.y<b.y+b.h&&a.y+a.h>b.y;
const roads=c.roads.flatMap(p=>S.lineTiles(p)),track=s.campaign!.fixedTram!.route;
const overlaps=bs.flatMap((a,i)=>bs.slice(i+1).filter(b=>hit(a,b)).map(b=>[a.id,b.id]));const roadsInBuildings=bs.map(b=>({id:b.id,n:roads.filter(t=>t%W>=b.x&&t%W<b.x+b.w&&Math.floor(t/W)>=b.y&&Math.floor(t/W)<b.y+b.h).length})).filter(r=>r.n);
const yards=bs.flatMap(b=>c.yards.map((p,i)=>hit(b,p)?[b.id,i]:null).filter(Boolean));const coreProps=s.campaign!.progression!.sites.flatMap(a=>ps.filter(b=>hit({...a,w:3,h:3},b)).map(b=>[a.id,b.id]));
const unreachable=bs.filter(b=>b.enterable&&!S.findPath(s,35,191,b.door![0]+1,b.door![1]+1)).map(b=>b.id);
const results={state:S.stateProblem(s),buildings:bs.length,overlaps,roadsInBuildings,yards,coreProps,unreachable,trackBlocked:track.filter(t=>!S.passable(s,t%W,Math.floor(t/W))).map(t=>[t%W,Math.floor(t/W)])};writeFileSync('docs/evidence/city-b/layout.json',JSON.stringify(results,null,2));console.log(results);
