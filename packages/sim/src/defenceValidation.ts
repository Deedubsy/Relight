/** Reject corrupt/partial campaign threat saves instead of silently resetting their schedule. */
import type { SimState } from './types';
import { DEFENCE, defenceMax } from './campaignDefence';
export function defenceProblem(st:SimState):string {
  const d=st.campaign?.defence;
  if((st.campaign?.version??0)<3)return d?'defence requires campaign metadata version 3':'';
  if(!d||![1,2].includes(d.version)||!st.flow||!Array.isArray(d.bases)||!Array.isArray(d.nominations)||!Array.isArray(d.sites)||!Array.isArray(d.history))return 'invalid campaign defence state';
  const number=(n:unknown)=>typeof n==='number'&&Number.isFinite(n);
  const integer=(n:unknown,min=0)=>number(n)&&Number.isInteger(n)&&(n as number)>=min;
  const tile=(n:unknown)=>integer(n)&&(n as number)<st.flow!.tw*st.city!.th;
  const block=(n:unknown)=>integer(n)&&!!st.blocks[n as number];
  if(d.clock&&(!d.clock||d.clock.version!==1||![d.clock.nextStart,d.clock.nextMinor,d.clock.recoveryUntil,d.clock.serial].every(n=>integer(n))||d.clock.legacyCommitment!==undefined&&d.clock.legacyCommitment!==true))return 'invalid active raid clock';
  if(d.major?.origins&&(!Array.isArray(d.major.origins)||d.major.origins.length<1||d.major.origins.length>4||!d.major.origins.every(tile)))return 'invalid assault approaches';
  if(d.major?.total!==undefined&&(d.major.total!==60||!integer(d.major.endsAt)||d.major.endsAt!<=d.major.startsAt||typeof d.major.committed!=='boolean'||d.major.cancelled!==undefined&&(!integer(d.major.cancelled)||d.major.cancelled>60)))return 'invalid active reinforcement window';
  if(!integer(d.nextId,1)||!integer(d.nextDawn)||!integer(d.lastMajorEnd,-1)||!integer(d.lastMinorSlot,-1)||!integer(d.raidsStarted)||!integer(d.majorSpawned)||typeof d.radioUpgrade!=='boolean'||typeof d.notice!=='string')return 'invalid campaign schedule';
  if(new Set(d.bases.map(b=>b.block)).size!==d.bases.length||!d.bases.some(b=>b.block===st.campaign!.homeBlock))return 'invalid campaign base registry';
  for(const b of d.bases)if(!block(b.block)||!integer(b.x)||!integer(b.y)||!integer(b.size,1)||b.x+b.size>st.flow.tw||b.y+b.size>st.city!.th||!number(b.hp)||b.hp<0||b.hp>DEFENCE.coreHp||!integer(b.commissionedAt))return 'invalid campaign base';
  const station=st.campaign!.expansion?.station;
  const stations=[station,st.campaign!.districts?.station].filter(s=>s!==undefined);
  const plants=st.campaign?.progression?.sites.filter(s=>s.kind==='plant'&&s.installed)??[];
  const commissioned=(block:number)=>stations.some(s=>s!.block===block&&s!.restoredAt>=0)||plants.some(s=>s.block===block);
  if(d.bases.some(b=>b.block!==st.campaign!.homeBlock&&!commissioned(b.block))||stations.some(s=>s!.restoredAt>=0&&!d.bases.some(b=>b.block===s!.block))||plants.some(s=>!d.bases.some(b=>b.block===s.block&&b.x===s.x&&b.y===s.y)))return 'campaign bases disagree with restoration';
  if(d.nominations.some(n=>!d.bases.some(b=>b.block===n.block)||!integer(n.at))||new Set(d.nominations.map(n=>n.block)).size!==d.nominations.length)return 'invalid restoration nominations';
  for(const a of [d.major,d.minor])if(a!==null&&(!a||!integer(a.id,1)||a.id>=d.nextId||!d.bases.some(b=>b.block===a.block)||!tile(a.origin)||typeof a.retreat!=='boolean'))return 'invalid campaign attack';
  const op=d.opening;
  if(op!==undefined){
    if(!op||op.version!==1||!['pending','scheduled','active','repelled','lost','skipped'].includes(op.status)||!integer(op.shots)||(op.suppliedAt!==undefined&&!integer(op.suppliedAt))||(op.count!==undefined&&!integer(op.count)))return 'invalid opening encounter';
    const live=op.status==='scheduled'||op.status==='active',done=op.status==='repelled'||op.status==='lost';
    if(op.status==='pending'&&op.id!==undefined)return 'invalid opening encounter';
    if((live||done)&&(!d.clock||!integer(op.id,1)||op.id!>=d.nextId||!tile(op.origin)||!integer(op.startsAt)||!integer(op.scheduledAt)||op.startsAt!<op.scheduledAt!||!integer(op.turretId)))return 'invalid opening encounter record';
    if(op.status==='active'?d.minor?.id!==op.id:d.minor!==null&&d.minor.id===op.id)return 'opening encounter disagrees with its raid';
    if(op.status==='scheduled'&&(d.minor||d.major))return 'opening encounter overlaps a raid';
    if(done&&(!integer(op.endedAt)||op.endedAt!<op.startsAt!))return 'invalid opening encounter record';
  }
  if(d.major&&(!integer(d.major.dawn)||!integer(d.major.startsAt)||d.major.startsAt<d.major.dawn||!integer(d.major.remaining)||d.major.remaining>(d.major.total??60)||!integer(d.major.nextSpawn)))return 'invalid major roster';
  if(d.major?.waiting!==undefined&&(d.version<2||!['approach','minor'].includes(d.major.waiting)||d.major.retreat||st.t<d.major.startsAt||
    (d.major.waiting==='approach'&&d.major.remaining===0)||(d.major.waiting==='minor'&&!d.minor)))return 'invalid deferred assault';
  if(d.majorSpawned>(d.major?.total??60)||(!d.major&&d.majorSpawned!==0)||(d.major&&(d.major.retreat?d.major.remaining!==0:d.major.remaining+d.majorSpawned+(d.major.cancelled??0)!==(d.major.total??60))))return 'inconsistent major roster accounting';
  if(d.warning&&(!integer(d.warning.assault,1)||!block(d.warning.block)||!integer(d.warning.startsAt)||!integer(d.warning.receivedAt)||(d.major&&d.warning.assault===d.major.id&&(d.warning.block!==d.major.block||d.warning.startsAt!==d.major.startsAt))))return 'invalid radio warning';
  if(d.sites.some(s=>!integer(s.id,1)||!block(s.block)||!tile(s.tile)||typeof s.spawned!=='boolean')||new Set(d.sites.map(s=>s.id)).size!==d.sites.length)return 'invalid ruin guards';
  if(d.history.some(h=>!integer(h.id,1)||!block(h.block)||!integer(h.started)||!integer(h.ended)||h.ended<h.started||typeof h.defeated!=='boolean'||!integer(h.spawned)))return 'invalid assault history';
  const attackIds=[...d.history.map(h=>h.id),...[d.major,d.minor].filter(a=>a!==null).map(a=>a!.id),...(op?.id!==undefined&&op.status!=='active'?[op.id]:[])];
  if(new Set(attackIds).size!==attackIds.length||attackIds.some(id=>id>=d.nextId)||d.history.some(h=>h.spawned>60))return 'reused campaign attack identity';
  const r=d.repair;
  if(r&&(typeof r.recommission!=='boolean'||!number(r.remaining)||r.remaining<0||(r.kind==='core'?!d.bases.some(b=>b.block===r.id):r.kind==='machine'?!st.flow.machines.some(m=>m.id===r.id&&defenceMax(m)>0):true)))return 'invalid paid repair';
  for(const m of st.flow.machines)if(m.hp!==undefined&&(!number(m.hp)||m.hp<0||defenceMax(m)===0||m.hp>defenceMax(m)))return 'invalid defence health';
  for(const m of st.flow.machines)if(m.turret){const a=m.turret;if(m.kind!=='turret'||!number(a.angle)||Math.abs(a.angle)>Math.PI+1e-8||a.target!==undefined&&!integer(a.target,1)||a.shot&&(!number(a.shot.x)||!number(a.shot.y)||!number(a.shot.angle)||Math.abs(a.shot.angle)>Math.PI+1e-8||a.shot.x<0||a.shot.y<0||a.shot.x>=st.flow.tw||a.shot.y>=st.city!.th))return 'invalid turret cannon state';}
  const bodies=st.flow.threat?.crawlers??[];
  if(new Set(bodies.map(c=>c.id)).size!==bodies.length||bodies.some(c=>!integer(c.id,1)||c.id>=st.flow!.threat!.next))return 'invalid campaign creature identity';
  if(bodies.filter(c=>c.campaign?.layer==='major').length>d.majorSpawned)return 'major bodies exceed spawned roster';
  for(const c of bodies) {
    if(c.role!==undefined&&!['breaker','conductor'].includes(c.role)||c.signal!==undefined&&(!number(c.signal)||c.signal<0))return 'invalid campaign creature role';
    const m=c.campaign;
    if(!m||!['site','minor','major'].includes(m.layer)||!integer(m.group,1)||!tile(m.origin)||(m.waypoint!==undefined&&!tile(m.waypoint))||!number(c.x)||!number(c.y)||c.x<0||c.y<0||c.x>=st.flow.tw||c.y>=st.city!.th||!number(c.hp)||c.hp<=0)return 'invalid campaign creature';
    if(m.encounter!==undefined&&(m.layer!=='site'||!st.campaign?.progression?.sites.some(s=>s.id===m.encounter&&['heart','furnace','crown'].includes(s.kind))))return 'invalid engineering attacker';
    if((m.patrol!==undefined&&(!tile(m.patrol)||m.layer!=='site'))||(m.patrolStep!==undefined&&(!integer(m.patrolStep)||m.layer!=='site')))return 'invalid campaign patrol';
    if(m.layer==='site'?!d.sites.some(s=>s.id===m.group):m.layer==='minor'?d.minor?.id!==m.group:d.major?.id!==m.group)return 'orphaned campaign creature';
    const group=m.layer==='site'?d.sites.find(s=>s.id===m.group):m.layer==='minor'?d.minor:d.major;
    if(c.to!==group!.block||c.from!==group!.block||(m.layer==='site'&&m.origin!==d.sites.find(s=>s.id===m.group)!.tile))return 'campaign creature target disagrees with group';
    if(m.withdrawing!==undefined&&(d.version<2||m.withdrawing!==true||m.layer==='site'||!(m.layer==='major'?d.major?.retreat:d.minor?.retreat)&&d.bases.find(b=>b.block===c.to)?.hp!==0))return 'invalid campaign withdrawal';
    if(d.version>=2&&m.waypoint!==undefined) {
      const dx=Math.abs(m.waypoint%st.flow.tw-Math.floor(c.x)),dy=Math.abs(Math.floor(m.waypoint/st.flow.tw)-Math.floor(c.y));
      if(dx+dy>1)return 'nonadjacent campaign waypoint';
    }
  }
  return '';
}
