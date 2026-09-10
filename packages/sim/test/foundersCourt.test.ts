import {test} from 'node:test';
import assert from 'node:assert/strict';
import * as S from '../src/index';
import {validateRiverfront} from '../src/city/validateRiverfront';
import {corridorRect,overlaps,doorRect,doorOutside,type Facing} from '../src/city/parcelGeometry';
import {invalidateGround} from '../src/ground';
import {daylight,interiorOpen} from '../../game/src/riverfrontLighting';

test('full corridor intersections catch crossings with no footprint corner or centre on the road',()=>{
 const building={x:0,y:0,w:20,h:20},corridor=corridorRect([-10,5],[30,5],1);
 assert.ok(overlaps(building,corridor));
 assert.equal(overlaps({x:0,y:6,w:20,h:10},corridor),false,'boundary contact is allowed');
});
test('all four doors, approach targets, collision and roof entry agree without replacing navigation',()=>{
 const b=S.RIVERFRONT_BUILDINGS.find(b=>b.id==='westridge-rowan')!,saved=structuredClone(b);
 try{for(const facing of ['N','S','E','W'] as Facing[]){b.facing=facing;delete b.door;const d=doorRect(b);b.door=[d.x,d.y];const p=doorOutside(b);b.path=[[Math.floor(p.x),Math.floor(p.y)],[Math.floor(p.x),Math.floor(p.y)]];const s=S.createCampaign();invalidateGround(s);
  for(let y=d.y;y<d.y+d.h;y++)for(let x=d.x;x<d.x+d.w;x++)assert.ok(S.passable(s,x,y),facing+' doorway');
  assert.ok(S.findPath(s,Math.floor(p.x),Math.floor(p.y),b.x+5,b.y+4),facing+' route');
  assert.ok(!S.passable(s,b.x,b.y),facing+' solid wall');
  Object.assign(s.engineer,p);assert.equal(interiorOpen(s,b),false);Object.assign(s.engineer,{x:b.x+5,y:b.y+4});S.tickAuthored(s);assert.ok(s.campaign!.authored!.visited.includes(b.id));assert.equal(interiorOpen(s,b),true);Object.assign(s.engineer,{x:b.x-2,y:b.y-2});assert.equal(interiorOpen(s,b),false);
 }}finally{Object.assign(b,saved);}
});
test('four explicit Home houses and retained paths validate; yard capacity and saved map isolation remain',()=>{
 const s=S.createCampaign(),v=validateRiverfront(s);assert.deepEqual(v.errors,[]);assert.equal(v.buildings,S.RIVERFRONT_BUILDINGS.length);
 for(const[id,x,y,w,h,facing]of [['court-northwest-home',31,351,10,9,'E'],['court-west-home',31,374,10,9,'E'],['court-approach-home',49,404,10,9,'E'],['court-east-cottage',85,382,9,7,'W']] as const){const b=S.RIVERFRONT_BUILDINGS.find(b=>b.id===id)!;assert.deepEqual([b.x,b.y,b.w,b.h,b.facing],[x,y,w,h,facing]);assert.ok(!b.enterable);assert.ok(!S.passable(s,x,y));const p=doorOutside(b);assert.ok(v.reachable[Math.floor(p.y)*v.width+Math.floor(p.x)]);}
 assert.deepEqual(S.RIVERFRONT.yards[0],{x:83,y:350,w:30,h:29});s.engineer.inv={steel:100,copper:100};s.engineer.x=86.5;s.engineer.y=357.5;assert.ok(S.place(s,'assembler',87,358,1));assert.ok(S.findPath(s,72,374,117,363));
 assert.equal(S.stateProblem(s),'');assert.equal(S.stateHash(S.loadState(S.makeSave(s))),S.stateHash(s));const old=S.makeSave(s);old.state.city!.mapId='riverfront-arc-v3';assert.match(S.stateProblem(old.state),/original build/);assert.equal(old.state.city!.mapId,'riverfront-arc-v3');
});
test('opening daylight and cycle seam are bright; dusk/dawn smooth and night/interiors retained',()=>{
 assert.equal(daylight(0),1);assert.equal(daylight(100),1);assert.equal(daylight(1050),0);assert.equal(daylight(1200),1);assert.ok(daylight(840)>.4&&daylight(840)<.6);assert.ok(daylight(1155)>.4&&daylight(1155)<.6);assert.ok(Math.abs(daylight(1199.99)-daylight(0))<.001);
});
