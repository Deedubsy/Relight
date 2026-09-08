import { test } from 'node:test';
import assert from 'node:assert/strict';
import { createCampaign, knownCampaignThreat, campaignWarning, campaignCrawlerAction, tickCampaignSchedule,
  tickCampaignThreat, ground, registerBase, nominateBase, applyCommands, machineAt, defenceHp,
  makeSave, loadState, stateHash, damageCore, canPlace, type SimState } from '../src/index';
import { threatOf, crawlerTarget, describeCrawler } from '../src/threat';

/** Explicit restored-base/clock fixtures isolate UI intelligence and action selection, not progression. */
function fixture(network=false):SimState {
  const st=createCampaign(3),d=st.campaign!.defence!;d.sites=[];d.lastMinorSlot=100000;
  if(network){const s=st.campaign!.expansion!.station;s.restoredAt=0;st.blocks[s.block].state=2;st.blocks[s.block].subOn=true;st.campaign!.expansion!.grantedAt=0;st.campaign!.expansion!.radio.restoredAt=0;registerBase(st,s.block);nominateBase(st,s.block);}
  st.t=2400;tickCampaignSchedule(st,threatOf(st.flow!));return st;
}
function spawn(st:SimState){st.t=st.campaign!.defence!.major!.startsAt;tickCampaignSchedule(st,threatOf(st.flow!));return threatOf(st.flow!).crawlers[0];}

test('known threat navigation never exposes an offline network target or reuses a different assault warning',()=>{
  const st=fixture(true),d=st.campaign!.defence!,a=d.major!;
  assert.equal(knownCampaignThreat(st),null);assert.match(campaignWarning(st),/target unknown/);
  d.warning={assault:a.id-1,block:d.bases[0].block,startsAt:900,receivedAt:0};
  assert.equal(knownCampaignThreat(st),null);
  d.warning={assault:a.id,block:a.block,startsAt:a.startsAt,receivedAt:st.t,approach:'north',composition:'ordinary crawlers'};
  const before=stateHash(st),target=knownCampaignThreat(st)!;
  assert.equal(target.block,a.block);assert.equal(target.phase,'warning');assert.equal(stateHash(st),before);
  assert.match(campaignWarning(st),/last received warning.*offline/);
  const saved=loadState(makeSave(st));assert.deepEqual(knownCampaignThreat(saved),target);assert.equal(campaignWarning(saved),campaignWarning(st));
  spawn(st);assert.equal(knownCampaignThreat(st)!.phase,'assault');
  damageCore(st,d.bases[1],300);assert.equal(knownCampaignThreat(st)!.phase,'withdrawal');
  d.major=null;d.majorSpawned=0;threatOf(st.flow!).crawlers=[];
  assert.equal(knownCampaignThreat(st)!.phase,'recovery');assert.match(campaignWarning(st),/core disabled/);
});

test('home guarantee and current minor raid remain actionable during a future major warning',()=>{
  const st=fixture(),d=st.campaign!.defence!;
  assert.equal(knownCampaignThreat(st)!.block,st.campaign!.homeBlock);
  assert.equal(knownCampaignThreat(st)!.phase,'warning');
  const site=st.campaign!.expansion!.station;registerBase(st,site.block);
  d.minor={id:d.nextId++,block:site.block,origin:d.major!.origin,retreat:false};
  assert.equal(knownCampaignThreat(st)!.block,site.block);assert.equal(knownCampaignThreat(st)!.phase,'minor raid');
  assert.match(campaignWarning(st),/Minor raid.*assault at dusk/);
  d.minor.retreat=true;assert.equal(knownCampaignThreat(st)!.phase,'withdrawal');
});

test('immediate wall breach uses the damage tick decision, including blocked withdrawal and saved continuation',()=>{
  const st=fixture(),G=ground(st),gate=G.opening!.gate[1],x=gate%G.tw,y=Math.floor(gate/G.tw);
  applyCommands(st,[{type:'chestTake',item:'steel',n:20}]);
  st.engineer.x=x+.5;st.engineer.y=y+.5;applyCommands(st,[{type:'place',item:'wall',x,y}]);
  const wall=machineAt(st,x,y)!;assert.equal(wall.kind,'wall');
  const c=spawn(st);c.x=x+.5;c.y=y+1.5;c.campaign!.waypoint=gate;
  st.engineer.x=st.campaign!.defence!.bases[0].x;st.engineer.y=st.campaign!.defence!.bases[0].y;
  const hash=stateHash(st),action=campaignCrawlerAction(st,c);assert.equal(stateHash(st),hash);
  assert.equal(action.action,'breach');assert.equal(action.what,'wall');assert.equal(action.destination,'base core');
  assert.equal(crawlerTarget(st,c)!.what,'wall');assert.match(describeCrawler(st,c),/now breaching wall.*destination: base core/);
  const hp=defenceHp(wall);tickCampaignThreat(st,threatOf(st.flow!),.05);assert.ok(defenceHp(wall)<hp);
  const d=st.campaign!.defence!;d.major!.retreat=true;d.major!.remaining=0;
  c.campaign!.withdrawing=true;c.campaign!.waypoint=gate;
  assert.equal(campaignCrawlerAction(st,c).destination,'exit');assert.match(describeCrawler(st,c),/breaching wall.*destination: exit/);
  const saved=loadState(makeSave(st));saved.speed=st.speed;
  assert.deepEqual(campaignCrawlerAction(saved,threatOf(saved.flow!).crawlers[0]),campaignCrawlerAction(st,c));
  tickCampaignThreat(st,threatOf(st.flow!),.05);tickCampaignThreat(saved,threatOf(saved.flow!),.05);assert.equal(stateHash(st),stateHash(saved));
});

test('nearby turret attack and engineer contact supersede the strategic core and match real damage',()=>{
  const st=fixture(),core=st.campaign!.defence!.bases[0];
  applyCommands(st,[{type:'chestTake',item:'steel',n:80},{type:'chestTake',item:'copper',n:40}]);
  let at:number[]|null=null;
  for(let y=core.y-6;y<core.y+8&&!at;y++)for(let x=core.x-6;x<core.x+8&&!at;x++)if(canPlace(st,'turret',x,y).ok)at=[x,y];
  assert.ok(at);const [x,y]=at;st.engineer.x=x+.5;st.engineer.y=y+.5;applyCommands(st,[{type:'place',item:'turret',x,y}]);
  const turret=machineAt(st,x,y)!;assert.equal(turret.kind,'turret');
  const c=spawn(st);c.x=x+turret.size/2;c.y=y+turret.size/2+1.5;st.engineer.x=core.x;st.engineer.y=core.y;
  assert.equal(campaignCrawlerAction(st,c).what,'turret');assert.equal(crawlerTarget(st,c)!.what,'turret');
  const hp=defenceHp(turret);tickCampaignThreat(st,threatOf(st.flow!),.05);assert.ok(defenceHp(turret)<hp);
  st.engineer.x=c.x;st.engineer.y=c.y;const playerHp=st.engineer.hp;
  assert.equal(campaignCrawlerAction(st,c).what,'you');tickCampaignThreat(st,threatOf(st.flow!),.05);assert.ok(st.engineer.hp<playerHp);
  st.campaign!.defence!.major!.retreat=true;
  assert.equal(campaignCrawlerAction(st,c).destination,'exit');assert.notEqual(crawlerTarget(st,c)!.what,'you');
});
