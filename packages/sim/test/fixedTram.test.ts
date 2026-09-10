import {test} from 'node:test';
import assert from 'node:assert/strict';
import {readFileSync} from 'node:fs';
import {createCampaign,fixedStops,fixedPoweredStops,ground,blockOfTile,canPlace,place,canPickUp,loadState,stateHash,stateProblem,applyCommands,advanceFlow,tramRoute,stationRoute,conservation,type SimState} from '../src/index';
import {tickCampaignThreat,campaignCrawlerAction,CAMPAIGN_THREAT} from '../src/campaignThreat';
import {threatOf} from '../src/threat';
function power(st:SimState,block:number){
 const G=ground(st),b=G.blocks[block];st.engineer.inv={steel:100,copper:100,coal:100};
 for(let y=b.y0;y<b.y1;y++)for(let x=b.x0;x<b.x1;x++)if(blockOfTile(st,x,y)===block&&canPlace(st,'generator',x,y).ok){const m=place(st,'generator',x,y)!;m.inv.coal=100;return m;}
 throw Error('no generator site');
}
function run(st:SimState,seconds:number){advanceFlow(st,seconds,[],Math.ceil(seconds*20)+1);}
test('four permanent stops exist at repeatable positions; infrastructure cannot be built or packed',()=>{
 const st=createCampaign(),n=st.campaign!.fixedTram!,stops=fixedStops(st);
 assert.equal(stops.length,4);assert.equal(new Set(stops.map(s=>blockOfTile(st,s.x,s.y))).size,4);
 assert.deepEqual(createCampaign().campaign!.fixedTram,n);assert.equal(stateProblem(st),'');
 for(const s of stops)assert.match(canPickUp(st,s.x,s.y).reason,/Permanent/);
 for(const kind of ['tram','tramstop','track'] as const)assert.match(canPlace(st,kind,0,0).reason,/Permanent/);
 assert.equal(fixedPoweredStops(st).length,1);assert.ok(conservation(st).ok);
 const tram=st.flow!.machines.find(m=>m.id===n.tram)!;const before=[tram.x,tram.y];run(st,3);assert.deepEqual([tram.x,tram.y],before);assert.equal(tram.phase,2);
});
test('power any second stop to automatically carry requested cargo; outage parks, reconnect resumes and save continues identically',()=>{
 const st=createCampaign(),n=st.campaign!.fixedTram!,stops=fixedStops(st),home=stops.find(s=>blockOfTile(st,s.x,s.y)===st.campaign!.homeBlock)!,destination=stops.at(-1)!;
 const gen=power(st,blockOfTile(st,destination.x,destination.y));assert.equal(fixedPoweredStops(st).length,2);
 home.inv.steel=7;home.freight={steel:{request:0,reserve:0,export:true}};destination.freight={steel:{request:7,reserve:0,export:false}};
 const tram=st.flow!.machines.find(m=>m.id===n.tram)!;const ledger=conservation(st).unexplained;
 run(st,1);assert.equal(tram.cargo!.steel,7);gen.inv.coal=0;run(st,1);const at=[tram.x,tram.y];run(st,5);assert.deepEqual([tram.x,tram.y],at);assert.equal(tram.cargo!.steel,7);
 gen.inv.coal=100;const saved=loadState(st);saved.speed=st.speed;run(st,40);run(saved,40);
 assert.equal(destination.cargo!.steel,7);assert.equal(stateHash(st),stateHash(saved));assert.deepEqual(conservation(st).unexplained.steel,ledger.steel);
 assert.equal(tramRoute(st,tram).length,n.route.length);assert.match(stationRoute(st,home.id)!.summary,/2\/4 stops powered/);
});
test('all four powered stops participate and unpowered stops are skipped without unloading there',()=>{
 const st=createCampaign(),stops=fixedStops(st),home=stops.find(s=>blockOfTile(st,s.x,s.y)===st.campaign!.homeBlock)!;
 for(const s of stops)if(s!==home)power(st,blockOfTile(st,s.x,s.y));
 assert.equal(fixedPoweredStops(st).length,4);home.inv.steel=9;home.freight={steel:{request:0,reserve:0,export:true}};
 for(const s of stops)if(s!==home)s.freight={steel:{request:3,reserve:0,export:false}};
 run(st,60);for(const s of stops)if(s!==home)assert.equal(s.cargo!.steel,3);
});
test('existing network save adopts its stations and tram once without deleting any stock or machine',()=>{
 const raw=JSON.parse(readFileSync('../../docs/evidence/p9-05-2026-09-09/campaign/network.json','utf8')),original=raw.state??raw.finalState??raw;
 const st=loadState(raw);assert.equal(fixedStops(st).length,4);assert.equal(stateProblem(st),'');
 for(const old of original.flow.machines){const now=st.flow!.machines.find(m=>m.id===old.id);assert.ok(now);assert.deepEqual(now.inv,old.inv);assert.deepEqual(now.cargo,old.cargo);}
 assert.equal(stateHash(loadState(st)),stateHash(st));
 const bad=structuredClone(st);bad.campaign!.fixedTram!.stops[1]=bad.campaign!.fixedTram!.stops[0];assert.throws(()=>loadState(bad),/fixed tram/);
});
test('ruin enemies roam, retain chase beyond notice/home radius and disengage only at escape distance; patrol survives save',()=>{
 const st=createCampaign(),G=ground(st),T=threatOf(st.flow!),site=st.campaign!.defence!.sites[0];
 const c={id:T.next++,kind:'crawler' as const,x:site.tile%G.tw+.5,y:Math.floor(site.tile/G.tw)+.5,hp:12,edge:-1,from:site.block,to:site.block,cls:2 as const,onPlayer:false,escaped:false,born:st.t,stuck:0,campaign:{layer:'site' as const,group:site.id,origin:site.tile}};
 site.spawned=true;T.crawlers.push(c);const before=[c.x,c.y];
 for(let i=0;i<40;i++)tickCampaignThreat(st,T,.05);assert.notDeepEqual([c.x,c.y],before,'idle guard actually patrols');
 st.engineer.x=c.x+4;st.engineer.y=c.y;tickCampaignThreat(st,T,.05);assert.equal(c.onPlayer,true);
 st.engineer.x=c.x+CAMPAIGN_THREAT.guardNotice+2;assert.equal(campaignCrawlerAction(st,c).action,'pursue');
 c.x+=30;st.engineer.x=c.x+4;assert.equal(campaignCrawlerAction(st,c).action,'pursue','home no longer bounds pursuit');
 st.engineer.x=c.x+CAMPAIGN_THREAT.chaseEscape+1;tickCampaignThreat(st,T,.05);assert.equal(c.onPlayer,false);assert.equal(campaignCrawlerAction(st,c).action,'roam');
 assert.equal(stateHash(loadState(st)),stateHash(st));
});
