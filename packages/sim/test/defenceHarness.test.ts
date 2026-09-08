import {test} from 'node:test';
import assert from 'node:assert/strict';
import {createCampaign,stateHash,ground,loadState,blockOfTile,conservation,stationRoute,rubbleAt} from '../src/index';
import {FactoryDriver,replayInterval} from '../../harness/src/blueprint';
import {factoryScenario,railNetwork} from '../../harness/src/factoryScenario';
import {DefenceDriver} from '../../harness/src/defenceScenario';

test('defence observation preserves ordinary commands, clock, live raid and replay',()=>{
 const initial=createCampaign(3),a=structuredClone(initial),b=structuredClone(initial),observer=new DefenceDriver(a),ordinary=new FactoryDriver(b);
 for(const d of [observer,ordinary]){d.take('steel',20);d.run(325);}
 assert.equal(stateHash(a),stateHash(b));assert.equal(a.t,325);
 assert.ok(observer.raids.length>0);assert.ok(observer.timeline.some(e=>e.minor));
 assert.deepEqual(observer.log,ordinary.log);assert.equal(stateHash(replayInterval(initial,observer.log,a.flow!.tick)),stateHash(a));
 assert.equal(stateHash(loadState(a)),stateHash({...a,speed:0}));
});

test('time away from home uses elapsed ticks rather than count of fractional samples',()=>{
 const s=createCampaign(3),G=ground(s),d=new DefenceDriver(s);
 // A road just outside the court still belongs to the home neighbourhood. Walk to the actual next region.
 const tile=s.campaign!.expansion!.route.find(t=>blockOfTile(s,t%G.tw,Math.floor(t/G.tw))!==s.campaign!.homeBlock)!;
 assert.ok(tile!==undefined);d.walk(tile%G.tw+.5,Math.floor(tile/G.tw)+.5);const before=d.awaySeconds;
 for(let i=0;i<20;i++)d.run(.1);
 assert.ok(Math.abs(d.awaySeconds-before-2)<1e-6);assert.ok(d.awaySeconds<=s.flow!.tick/20);
});

test('seed 8 survey avoids forbidden street tiles and supports a paid three-stop line from ordinary stock',()=>{
 const s=factoryScenario(8),G=ground(s.st);s.d.take('coal',20);
 const tile=Array.from(G.patch.keys()).find(t=>G.patch[t]&&rubbleAt(s.st,t%G.tw,Math.floor(t/G.tw))?.type==='coal')!;
 s.d.approach(tile%G.tw,Math.floor(tile/G.tw));s.d.send({type:'factory',action:{type:'mineAt',x:tile%G.tw,y:Math.floor(tile/G.tw)}});s.d.run(22);s.d.send({type:'factory',action:{type:'mineAt',x:-1,y:-1}});
 const net=railNetwork(s.d);s.d.run(20);
 assert.equal(stationRoute(s.st,net.stops[2].id)!.interrupted,false);
 assert.equal(net.stops.length,3);assert.equal(conservation(s.st).ok,true);
 const replay=replayInterval(s.initial,s.d.log,s.st.flow!.tick);assert.equal(stateHash(replay),stateHash(s.st));
});
