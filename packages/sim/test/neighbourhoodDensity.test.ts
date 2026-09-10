import {test} from 'node:test';
import assert from 'node:assert/strict';
import {readFileSync} from 'node:fs';
import {readLayout} from '../../tools/src/phaserCityModel';
import {RIVERFRONT as C,RIVERFRONT_BUILDINGS as buildings,RIVERFRONT_PROPS as props,createCampaign,findPath,truckFits,stateProblem,makeSave,loadState,stateHash} from '../src/index';
const before=readLayout(readFileSync(new URL('../../../docs/evidence/neighbourhood-density/before-riverfront.ts.txt',import.meta.url),'utf8'));
test('approved starting neighbourhood preserves its original buildings, factory reservations and transport geometry',()=>{
 for(const key of Object.keys(before.city) as (keyof typeof C)[])if(!['id','paths','squares'].includes(key))assert.deepEqual(C[key],before.city[key],key);
 const local=(p:{x:number;y:number})=>p.x<180&&p.y>=324&&p.y<432;
 const removed=before.buildings.filter(b=>local(b)&&!buildings.some(q=>q.id===b.id));assert.equal(removed.length,8);assert.ok(removed.every(b=>b.id.startsWith('block-2-7-')||b.id.startsWith('block-2-8-')));
 for(const b of before.buildings.filter(b=>local(b)&&!removed.includes(b)))assert.deepEqual(buildings.find(q=>q.id===b.id),b,b.id);
 for(const p of before.props.filter(p=>local(p)&&!removed.some(b=>p.id.startsWith(b.id+':'))))assert.deepEqual(props.find(q=>q.id===p.id),p,p.id);
 const coverage=(bs:typeof buildings,y:number)=>bs.filter(b=>b.x>=126&&b.x+b.w<=180&&b.y>=y&&b.y+b.h<=y+54).reduce((n,b)=>n+b.w*b.h,0)/(54*54);
 assert.ok(coverage(buildings,324)>coverage(before.buildings,324)*2);assert.ok(coverage(buildings,378)>coverage(before.buildings,378)*1.8);
});
test('new street and garden routes remain walkable, truck access remains clear and saves retain layout identity',()=>{
 const s=createCampaign();
 for(const[x,y]of [[151,374],[151,405],[35,425],[117,363]])assert.ok(findPath(s,72,374,x,y),`${x},${y}`);
 for(let x=126;x<=180;x++)assert.ok(truckFits(s,x+.5,378.5,1),'shop street '+x);
 for(let y=344;y<=382;y++)assert.ok(truckFits(s,117.5,y+.5,0),'Home service drive '+y);
 assert.equal(stateHash(loadState(makeSave(s))),stateHash(s));const old=makeSave(s);old.state.city!.mapId=before.city.id;assert.match(stateProblem(old.state),/original build/);
});
