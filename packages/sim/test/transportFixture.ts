import {suppliedCampaign as createCampaign} from './suppliedCampaignFixture';
// Prepared historical starting stock; current ungranted progression is checked in gameplayCorrections.test.ts.
import assert from 'node:assert/strict';
import { ground, findPath, passable, advanceFlow, applyCommands, canPlace, blockOfTile, campaignSite, siteCheck, EXPANSION, campaignThrottle, campaignGrid,
  conservation, loadState, makeSave, stateHash, lockReason, machineAt, tramRoute, inReach, polePlan, type SimState, type Command, type LoggedCommand } from '../src/index';
const logs = new WeakMap<SimState, LoggedCommand[]>();
export function command(st:SimState,commands:Command[]):void { const log=logs.get(st)??[];for(const c of commands)log.push({tick:st.flow!.tick,c});logs.set(st,log);applyCommands(st,commands); }
export function walk(st:SimState,x:number,y:number,size=1):void {
  if(inReach(st,x,y,size))return;
  let target:[number,number]|undefined;
  for(let r=0;r<=5&&!target;r++)for(let yy=y-r;yy<y+size+r&&!target;yy++)for(let xx=x-r;xx<x+size+r;xx++) {
    if(passable(st,xx,yy)&&findPath(st,Math.floor(st.engineer.x),Math.floor(st.engineer.y),xx,yy)?.length){target=[xx,yy];break;}
  }
  assert.ok(target,'reachable approach');
  command(st,[{type:'move',x:target[0]+0.5,y:target[1]+0.5}]);
  for(let k=0;k<120&&st.engineer.target;k++)advanceFlow(st,1);
  assert.ok(inReach(st,x,y,size),'walk arrived within reach');
}
function generator(st:SimState,bi:number):[number,number] {
  const sub=ground(st).blocks[bi].sub!;
  for(let r=3;r<18;r++)for(let y=sub.y-r;y<sub.y+r;y++)for(let x=sub.x-r;x<sub.x+r;x++)if(blockOfTile(st,x,y)===bi&&canPlace(st,'generator',x,y).ok){walk(st,x,y,2);command(st,[{type:'place',item:'generator',x,y},{type:'feed',x,y}]);assert.equal(machineAt(st,x,y)?.kind,'generator');return[x,y];}
  throw new Error('no generator position');
}
function provision(st:SimState):void { command(st,[{type:'chestTake',item:'steel',n:120},{type:'chestTake',item:'copper',n:60},{type:'chestTake',item:'coal',n:20}]); }
function restoreStation(st:SimState):[number,number] {
  const s=campaignSite(st,'station')!;const gen=generator(st,s.block);walk(st,s.x,s.y,s.size);
  command(st,[{type:'deliverSite',site:'station'},{type:'restoreSite',site:'station'}]);assert.ok(s.restoredAt>=0,siteCheck(st,'station'));return gen;
}

/** A paid seed-3 expedition through ordinary commands, with its complete replay log. */
export function expedition(seed=3){const st=createCampaign(seed);provision(st);restoreStation(st);return {st,log:logs.get(st)!};}
export function layKit(st:SimState):void {
 const n=st.campaign!.fixedTram!;assert.equal(n.stops.length,4);assert.ok(st.flow!.machines.some(m=>m.id===n.tram));
}
