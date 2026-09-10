import {test} from 'node:test';
import assert from 'node:assert/strict';
import {readFileSync} from 'node:fs';
import {readLayout} from '../../tools/src/phaserCityModel';
import {RIVERFRONT as C,RIVERFRONT_BUILDINGS as buildings,RIVERFRONT_PROPS as props,createCampaign,makeSave,loadState,stateHash,stateProblem,findPath,truckFits,doorOutside} from '../src/index';
import {validateRiverfront} from '../src/city/validateRiverfront';
import {connectEntrance} from '../../tools/src/phaserCityPlots';
const before=readLayout(readFileSync(new URL('../../../docs/evidence/city-density/before-riverfront.ts.txt',import.meta.url),'utf8'));
test('city-wide density preserves the approved start, campaign bindings and all road/tram/factory geometry',()=>{
 for(const k of Object.keys(before.city) as (keyof typeof C)[])if(!['id','paths','squares'].includes(k))assert.deepEqual(C[k],before.city[k],k);
 const retained=before.buildings.filter(b=>!b.id.startsWith('block-')||['department','arcade','office','parking','apartment'].includes(b.kind));
 for(const b of retained)assert.deepEqual(buildings.find(q=>q.id===b.id),b,b.id);
 for(const p of before.props.filter(p=>!p.id.startsWith('block-')))assert.deepEqual(props.find(q=>q.id===p.id),p,p.id);
 const removed=before.buildings.filter(b=>!buildings.some(q=>q.id===b.id));assert.ok(removed.every(b=>b.id.startsWith('block-')&&!b.enterable));
 const area=(bs:typeof buildings,x0:number,y0:number,x1:number,y1:number)=>bs.filter(b=>b.x>=x0&&b.y>=y0&&b.x+b.w<=x1&&b.y+b.h<=y1).reduce((n,b)=>n+b.w*b.h,0);
 for(const [name,x0,y0,x1,y1]of [['Northwood',126,18,342,126],['Westridge',18,198,234,306],['Old Town',288,144,342,306],['East Gardens',702,144,756,378],['Riverside',432,324,648,432]] as const)assert.ok(area(buildings,x0,y0,x1,y1)>area(before.buildings,x0,y0,x1,y1)*1.4,name+' gains substantial built area');
});
test('every dense block entrance is reachable and campaign routes, truck streets and saves remain usable',()=>{
 const s=createCampaign(),v=validateRiverfront(s);assert.deepEqual(v.errors,[]);assert.equal(v.buildings,479);
 for(const b of buildings){const p=doorOutside(b);assert.ok(v.reachable[Math.floor(p.y)*C.width+Math.floor(p.x)],b.id);}
 for(const[x,y]of [[300,386],[418,88],[727,420],[151,405],[510,418]])assert.ok(findPath(s,72,374,x,y),`${x},${y}`);
 for(const[x,y,dir]of [[126.5,378.5,1],[300.5,390.5,0],[418.5,70.5,0],[745.5,420.5,1]] as const)assert.ok(truckFits(s,x,y,dir));
 assert.equal(stateHash(loadState(makeSave(s))),stateHash(s));const old=makeSave(s);old.state.city!.mapId=before.city.id;assert.match(stateProblem(old.state),/original build/);
});
test('a middle courtyard home has an outward apron before joining the public street',()=>{
 const b=buildings.find(b=>b.id==='density-9-0-2')!,layout={city:C,buildings,props},path=connectEntrance(b,layout);
 assert.ok(path.length>=3);assert.deepEqual(path[0],b.path[0]);assert.ok(path[1][0]>path[0][0],'east-facing door steps out into the court before turning');
});
