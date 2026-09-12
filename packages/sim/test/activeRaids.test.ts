import {test} from 'node:test';
import assert from 'node:assert/strict';
import {createCampaign,tickCampaignSchedule,loadState,stateProblem,campaignWarning,ACTIVE_RAIDS} from '../src/index';
test('saved major commitment is Home-first, bounded and next start is anchored to assault start',()=>{
  let s=createCampaign(),d=s.campaign!.defence!,T=s.flow!.threat!,start=d.clock!.nextStart;
  assert.ok(start>=1500&&start<=1800);assert.equal(d.clock!.nextMinor,780);
  s.t=start-300;tickCampaignSchedule(s,T);assert.equal(d.major!.block,s.campaign!.homeBlock);
  assert.match(campaignWarning(s),/Home|Founders/);assert.ok(!campaignWarning(s).includes('unknown'));
  const original=JSON.stringify(d.major);s=loadState(s);d=s.campaign!.defence!;T=s.flow!.threat!;assert.equal(JSON.stringify(d.major),original);
  s.t=start;tickCampaignSchedule(s,T);assert.equal(d.major!.committed,true);
  const next=d.clock!.nextStart;assert.ok(next-start>=1080&&next-start<=1320);
  for(let t=start+1;t<=start+150;t++){s.t=t;tickCampaignSchedule(s,T);assert.ok(T.crawlers.filter(c=>c.campaign?.layer==='major').length<=ACTIVE_RAIDS.activeRaidBudget);}
  assert.equal(d.clock!.nextStart,next);assert.equal(d.major!.remaining,0);assert.equal(d.majorSpawned+(d.major!.cancelled??0),60);
  assert.ok(T.crawlers.some(c=>c.campaign?.layer==='major'));assert.equal(stateProblem(s),'');
  s.t=next;tickCampaignSchedule(s,T);assert.ok(d.clock!.nextStart>next+1000);assert.match(d.notice,/skipped/);assert.ok(d.major);
});
test('blocked opportunities do not create debt and old committed finite rosters migrate once',()=>{
  const s=createCampaign(),d=s.campaign!.defence!,T=s.flow!.threat!;
  s.t=d.clock!.nextStart-300;tickCampaignSchedule(s,T);const a=d.major!;delete d.clock;delete a.total;delete a.endsAt;delete a.committed;
  const old=JSON.stringify(s),m=loadState(s),md=m.campaign!.defence!;
  assert.equal(JSON.stringify(s),old);assert.equal(md.clock!.legacyCommitment,true);assert.deepEqual(md.major,a);
  assert.equal(loadState(m).campaign!.defence!.clock!.nextStart,md.clock!.nextStart);
  const fresh=createCampaign(),fd=fresh.campaign!.defence!;fd.clock!.recoveryUntil=fd.clock!.nextStart+60;fresh.t=fd.clock!.nextStart-300;
  tickCampaignSchedule(fresh,fresh.flow!.threat!);assert.equal(fd.major,null);assert.ok(fd.clock!.nextStart>fresh.t+1000);assert.match(fd.notice,/skipped/);
});
