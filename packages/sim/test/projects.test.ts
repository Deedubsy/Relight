import {suppliedCampaign as createCampaign} from './suppliedCampaignFixture';
// Prepared historical starting stock; current ungranted progression is checked in gameplayCorrections.test.ts.
import {test} from 'node:test';
import {writeFileSync} from 'node:fs';
import assert from 'node:assert/strict';
import {newThreat,ground,findPath,passable,advanceFlow,applyCommands,canPlace,blockOfTile,campaignSite,machineAt,inReach,campaignDiscoveries,campaignAlerts,conservation,loadState,makeSave,stateHash,CAMPAIGN_THREAT,type Crawler,type SimState,type Command,type LoggedCommand} from '../src/index';
const logs = new WeakMap<SimState, LoggedCommand[]>();
function command(st:SimState,commands:Command[]):void { const log=logs.get(st)??[];for(const c of commands)log.push({tick:st.flow!.tick,c});logs.set(st,log);applyCommands(st,commands); }
function walk(st:SimState,x:number,y:number,size=1):void {
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
test('project previews and ordinary paid delivery, power, restoration and kit collection agree and survive load',()=>{
 const st=createCampaign(),site=campaignSite(st,'station')!;
 const checkpoint=(name:string)=>{if(process.env.UI05_FIXTURES)writeFileSync(`${process.env.UI05_FIXTURES}/${name}.json`,JSON.stringify(makeSave(st,{log:logs.get(st)??[],logComplete:true})));};
 let row=campaignDiscoveries(st)[0];assert.ok(row.actions[0].reason);assert.ok(row.circuit!.materials.every(m=>!m.transfer&&m.home>0));
 command(st,[{type:'chestTake',item:'steel',n:12}]);walk(st,site.x,site.y,site.size);
 row=campaignDiscoveries(st)[0];assert.equal(row.circuit!.materials[0].transfer,12);assert.equal(row.circuit!.powered,false);assert.equal(row.actions[0].reason,'');assert.deepEqual(row.actions[0].commands,[{type:'deliverSite',site:'station'}]);
 checkpoint('partial');command(st,row.actions[0].commands);assert.equal(site.delivered.steel,12);assert.equal(site.restoredAt,-1);assert.ok(campaignDiscoveries(st)[0].actions[0].reason);
 const depot=st.flow!.machines.find(m=>m.kind==='depot')!;walk(st,depot.x,depot.y,depot.size);provision(st);
 const gen=generator(st,site.block);walk(st,site.x,site.y,site.size);row=campaignDiscoveries(st)[0];assert.equal(row.circuit!.materials[0].transfer,18);assert.equal(row.actions[0].label,'Deliver and restore');
 checkpoint('powered');command(st,row.actions[0].commands);assert.ok(site.restoredAt>=0);assert.deepEqual(site.delivered,{steel:0,copper:0});row=campaignDiscoveries(st)[0];assert.ok(row.circuit!.restored);const reward=structuredClone(st.campaign!.expansion!.reward);
 command(st,[{type:'restoreSite',site:'station'}]);assert.deepEqual(st.campaign!.expansion!.reward,reward);
 checkpoint('kit');const preview=row.reward!;command(st,row.actions[0].commands);for(const r of preview)assert.equal(st.campaign!.expansion!.reward[r.item as keyof typeof reward],r.remaining-r.collect);assert.ok(conservation(st).ok);
 const radio=campaignSite(st,'radio')!;walk(st,radio.x,radio.y,radio.size);advanceFlow(st,.05);checkpoint('radio');
 row=campaignDiscoveries(st).find(r=>r.id==='radio')!;assert.equal(row.actions[0].reason,'');command(st,row.actions[0].commands);assert.ok(radio.restoredAt>=0);
 row=campaignDiscoveries(st).find(r=>r.id==='radio')!;assert.equal(row.upgrade!.steel,15);const steel=st.engineer.inv.steel;command(st,row.actions[0].commands);assert.equal(st.engineer.inv.steel,steel-15);assert.ok(st.campaign!.defence!.radioUpgrade);assert.ok(conservation(st).ok);
 // Labelled outage fixture tests display only, after paid restoration and collection.
 machineAt(st,...gen)!.inv.coal=0;row=campaignDiscoveries(st)[0];assert.ok(row.circuit!.restored);assert.equal(row.circuit!.powered,false);assert.ok(row.circuit!.materials.every(m=>m.transfer===0));
 checkpoint('outage');assert.equal(stateHash(loadState(makeSave(st))),stateHash(st));
});

test('project read model has concrete, separate radio upgrade costs, full-pocket and prerequisite refusals without mutations',()=>{
 const s=createCampaign(),e=s.campaign!.expansion!,t=s.campaign!.turbine!;s.campaign!.knowledge!.sites.radio=0;t.seenAt=0;
 s.engineer.x=e.radio.x;s.engineer.y=e.radio.y;s.engineer.inv={steel:50,copper:50};let radio=campaignDiscoveries(s).find(r=>r.id==='radio')!;assert.match(radio.actions[0].reason,/first station/);assert.equal(radio.circuit!.materials[0].required,20);
 // Labelled completion/capacity fixture isolates read-only fields, not earned progression.
 e.station.restoredAt=e.radio.restoredAt=0;e.grantedAt=0;e.reward={tram:1,tramstop:2,track:e.route.length};s.engineer.inv={belt:40};s.engineer.x=e.station.x;s.engineer.y=e.station.y;
 const before=stateHash(s),rows=campaignDiscoveries(s);assert.equal(stateHash(s),before);assert.ok(rows.find(r=>r.id===t.id)!.circuit!.materials.some(m=>m.item==='concrete'));
 assert.ok(rows.find(r=>r.id==='station')!.reward!.every(r=>!r.collect));radio=rows.find(r=>r.id==='radio')!;assert.equal(radio.upgrade!.steel,CAMPAIGN_THREAT.radioUpgradeSteel);assert.equal(radio.upgrade!.copper,CAMPAIGN_THREAT.radioUpgradeCopper);
 s.campaign!.defence!.radioUpgrade=true;assert.ok(campaignDiscoveries(s).find(r=>r.id==='radio')!.upgrade!.bought);
});

test('alerts prioritize actual known threats, resolve from state and withhold unknown targets',()=>{
 const s=createCampaign(),d=s.campaign!.defence!,base=d.bases[0];let alerts=campaignAlerts(s);assert.equal(alerts[0].id,'schedule');assert.match(alerts[0].title,/Night 3/);
 // Labelled schedule fixture: no spawned enemy roster is exposed by the query.
 d.minor={id:1,block:base.block,origin:0,retreat:false};alerts=campaignAlerts(s);assert.equal(alerts[0].id,`threat:${base.block}`);assert.match(alerts[0].title,/Base under attack/);const hash=stateHash(s);campaignAlerts(s);assert.equal(stateHash(s),hash);
 s.engineer.lastHit=s.t;assert.equal(campaignAlerts(s)[0].id,'engineer:danger');s.engineer.lastHit=-999;d.minor=null;assert.ok(!campaignAlerts(s).some(a=>a.id.startsWith('threat:')));
 s.campaign!.expansion!.radio.restoredAt=0;d.major={id:2,block:base.block,dawn:0,startsAt:0,origin:0,remaining:60,nextSpawn:0,retreat:false};d.warning=null;
 alerts=campaignAlerts(s);assert.equal(alerts[0].location,null);assert.equal(alerts[0].id,'assault:unknown');assert.ok(!JSON.stringify(alerts).includes('remaining'));
 d.major=null;d.nextDawn=6000;assert.match(campaignAlerts(s)[0].title,/Night 6/);
 const crawler={id:900,hp:12,kind:'crawler',x:s.engineer.x+1,y:s.engineer.y} as Crawler;(s.flow!.threat??=newThreat()).crawlers.push(crawler);assert.equal(campaignAlerts(s)[0].id,'nearby:900');crawler.x+=50;assert.ok(!campaignAlerts(s).some(a=>a.id==='nearby:900'));base.hp-=10;assert.match(campaignAlerts(s)[0].title,/Base damaged/);
});
