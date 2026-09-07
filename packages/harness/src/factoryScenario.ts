import { createCampaign, openLedger, ground, placeable, canPlace, blockOfTile, passable, campaignSite,
 siteCheck, findRubble, rubbleAt, invStacks, outputTile, type Dir, type SimState, type Machine, type Kind, type RecipeId, type Item, type StationRules } from '@relight/sim';
import { FactoryDriver, buildBlueprint, parseBlueprint, type Blueprint } from './blueprint';

export const LOGISTICS_ASSISTANCE='Labelled logistics fixture: +1200 steel, +600 copper and +500 coal in the home chest before the opening ledger; minor/major attacks deferred and ruin guards removed. All later walks, placements, restoration, transfers and recovery use ordinary commands. Not campaign balance or defence evidence.';
export function factoryScenario(seed:number,assisted=false){
 const st=createCampaign(seed);
 if(assisted){st.stock.steel+=1200;st.stock.copper+=600;st.flow!.store.coal+=500;const def=st.campaign!.defence!;def.nextDawn=1e9;def.lastMinorSlot=1e9;def.sites=[];st.flow!.ledger=openLedger(st);}
 const initial=structuredClone(st),d=new FactoryDriver(st);
 for(const [item,n] of [['steel',assisted?500:200],['copper',assisted?250:100],['coal',assisted?200:20]] as const)d.take(item,n);
 return {st,d,initial,assistance:assisted?LOGISTICS_ASSISTANCE:'Fresh campaign, ordinary home stock, threats active; no stock or position injection.'};
}
export function locateBlueprint(st:SimState,raw:unknown):[number,number] {
 const bp=parseBlueprint(raw),e=st.engineer;
 for(let radius=10;radius<=100;radius+=10)for(let y=Math.floor(e.y)-radius;y<=e.y+radius;y++)for(let x=Math.floor(e.x)-radius;x<=e.x+radius;x++){
  const circuits=new Set(bp.entities.filter(e=>['assembler','inserter','generator'].includes(e.kind)).map(e=>blockOfTile(st,x+e.x,y+e.y)));
  if(circuits.size!==1||circuits.has(-1)||!passable(st,x+7,y+4))continue;
  if(bp.entities.every(e=>!placeable(st,e.kind,x+e.x,y+e.y,e.dir)))return [x,y];
 }throw new Error('No physical site for the blueprint');
}
export function factoryLine(d:FactoryDriver,bp:Blueprint){
 for(const [id,kind] of [['input','chest'],['output','chest'],['assembler','assembler'],['generator','generator']])if(!bp.entities.some(e=>e.id===id&&e.kind===kind))throw new Error(`Factory experiment blueprint needs ${id} (${kind})`);
 const origin=locateBlueprint(d.state,bp),ids=buildBlueprint(d,bp,...origin);
 const get=(id:string)=>d.state.flow!.machines.find(m=>m.id===ids[id])!;
 const generator=get('generator');d.approach(generator.x,generator.y,2);d.send({type:'factory',action:{type:'feed',x:generator.x,y:generator.y}});
 return {origin,ids,input:get('input'),assembler:get('assembler'),output:get('output'),generator};
}
export function nearby(d:FactoryDriver,kind:Exclude<Kind,'depot'>,x:number,y:number,block:number,radius=20):Machine {
 for(let r=1;r<=radius;r++)for(let yy=y-r;yy<=y+r;yy++)for(let xx=x-r;xx<=x+r;xx++)if(blockOfTile(d.state,xx,yy)===block&&canPlace(d.state,kind,xx,yy).ok)return d.place(kind,xx,yy);
 throw new Error(`No ${kind} footprint near ${x},${y}`);
}
export function power(d:FactoryDriver,block:number):Machine {
 const sub=ground(d.state).blocks[block].sub!,m=nearby(d,'generator',sub.x,sub.y,block);
 d.send({type:'factory',action:{type:'feed',x:m.x,y:m.y}});return m;
}
export function restore(d:FactoryDriver,id:'station'|'northStation'):void {
 const s=campaignSite(d.state,id)!;d.approach(s.x,s.y,s.size);d.send({type:'deliverSite',site:id});d.send({type:'restoreSite',site:id});if(s.restoredAt<0)throw new Error(siteCheck(d.state,id));
}
export function railNetwork(d:FactoryDriver,three=true){
 const st=d.state,e=st.campaign!.expansion!,district=st.campaign!.districts!,G=ground(st),generators=[power(d,e.station.block)];restore(d,'station');
 if(three){generators.push(power(d,district.station.block));restore(d,'northStation');}
 const collect=()=>{d.approach(e.station.x,e.station.y,e.station.size);d.send({type:'collectTramKit'});};collect();
 const path=three?[...e.route,...district.route.slice(1)]:e.route;
 for(const t of path){if(!(st.engineer.inv.track>0)&&e.reward.track>0)collect();d.place('track',t%G.tw,Math.floor(t/G.tw));}
 const stops=(three?[...e.stops,district.stop]:e.stops).map(([x,y])=>d.place('tramstop',x,y));
 const tram=d.place('tram',path[0]%G.tw,Math.floor(path[0]/G.tw));return {path,stops,tram,generators};
}
export function configure(d:FactoryDriver,m:Machine,rules:StationRules):void {d.approach(m.x,m.y,m.size);d.send({type:'setStationRules',x:m.x,y:m.y,rules});}
export function recipe(d:FactoryDriver,m:Machine,id:RecipeId):void {d.approach(m.x,m.y,m.size);d.send({type:'factory',action:{type:'setRecipe',x:m.x,y:m.y,recipe:id}});}
export function extract(d:FactoryDriver,item:'coal'|'steel'|'copper'){
 const source=d.state.campaign!.districts!.sources.find(s=>s.item===item)!;
 const generator=power(d,source.block),candidates=[{x:source.x+2,y:source.y+1,dir:1 as Dir}];
 for(let y=source.y-3;y<=source.y+source.size;y++)for(let x=source.x-3;x<=source.x+source.size;x++)for(const dir of [0,1,2,3] as Dir[])candidates.push({x,y,dir});
 const layouts:{x:number;y:number;dir:Dir;cx:number;cy:number;clear:number}[]=[];
 for(const p of candidates){
  if(blockOfTile(d.state,p.x,p.y)!==source.block||!canPlace(d.state,'excavator',p.x,p.y,p.dir).ok)continue;
  const probe={...p,size:3} as Machine,rubble=findRubble(d.state,probe);if(!rubble?.persistent||rubble.type!==item)continue;
  const [ox,oy]=outputTile(probe),cx=ox-(p.dir===3?1:0),cy=oy-(p.dir===0?1:0);
  const reason=placeable(d.state,'chest',cx,cy);if(reason&&reason!=='rubble in the way')continue;
  let clear=0;for(let y=cy;y<cy+2;y++)for(let x=cx;x<cx+2;x++)clear+=rubbleAt(d.state,x,y)?.units??0;
  if(Number.isFinite(clear))layouts.push({...p,cx,cy,clear});
 }
 const p=layouts.sort((a,b)=>a.clear-b.clear)[0];if(!p)throw new Error(`No paid excavation/storage layout at ${item} source`);
 // Some seeded source margins have finite rubble. Clear it by real mining and store surplus at home.
 const home=d.state.flow!.machines.find(m=>m.kind==='depot')!;
 for(let y=p.cy;y<p.cy+2;y++)for(let x=p.cx;x<p.cx+2;x++)while(rubbleAt(d.state,x,y)){
  if(invStacks(d.state.engineer.inv)>30)for(const k of ['steel','copper','coal']){const n=(d.state.engineer.inv[k]??0)-100;if(n>0){d.approach(home.x,home.y,home.size);d.send({type:'factory',action:{type:'chestPut',item:k,n}});}}
  const r=rubbleAt(d.state,x,y)!;d.approach(x,y);d.send({type:'factory',action:{type:'mineAt',x,y}});d.run(Math.min(50,r.units+1));d.send({type:'factory',action:{type:'mineAt',x:-1,y:-1}});
  if((rubbleAt(d.state,x,y)?.units??0)>=r.units)throw new Error('Source margin mining made no progress');
 }
 const excavator=d.place('excavator',p.x,p.y,p.dir),chest=d.place('chest',p.cx,p.cy);return {source,generator,excavator,chest};
}
export function moveCargo(d:FactoryDriver,item:Item,n:number,from:Machine,to:Machine):number {
 const available=Math.min(n,(from.cargo?.[item]??0)+(from.inv[item]??0));if(available<=0)return 0;const moved=d.take(item,available,from);return d.put(item,moved,to);
}
