/** Never reconstruct a missing reward/guardian record in a current campaign save. */
import type { SimState } from './types';
import { DISCOVERY, discoveryId } from './campaignDiscovery';
import { stalkerHp } from './stalker';
export function discoveryProblem(st: SimState): string {
  const c=st.campaign,d=c?.discovery;
  if ((c?.version??0)<5) return d?'discovery requires campaign metadata version 5':'';
  if (!d || d.version!==1 || !st.flow || !st.city || d.id!==discoveryId(st.seed) || (!st.city.mapId&&d.block!==c!.districts!.workshop.block)) return 'invalid campaign discovery';
  const num=(n:unknown):n is number=>typeof n==='number'&&Number.isFinite(n);
  const int=(n:unknown,min=0):n is number=>num(n)&&Number.isSafeInteger(n)&&n>=min;
  const time=(n:unknown)=>int(n,-1)&&(n as number)<=st.t;
  const xy=(x:unknown,y:unknown)=>num(x)&&num(y)&&x>=0&&y>=0&&x<st.flow!.tw&&y<st.city!.th;
  if (!int(d.x)||!int(d.y)||!xy(d.x,d.y)||!time(d.seenAt)||!time(d.recoveredAt)||(d.recoveredAt>=0&&d.seenAt<0)) return 'invalid discovery location or recovery';
  const S=d.guardian;
  if (!S||!S.cand||!S.sites||!S.stats||!Array.isArray(S.stalkers)||!xy(S.ex,S.ey)||!time(S.shotAt)||!int(S.machineId)) return 'invalid workshop guardian layer';
  if (Object.entries(DISCOVERY.guardian).some(([k,v])=>S.cand[k as keyof typeof S.cand]!==v)) return 'invalid workshop guardian tuning';
  const mem=S.sites[d.block],stats=S.stats;
  if (Object.keys(S.sites).length!==1||!mem||mem.restored!==false||![0,1].includes(mem.spawned)||!time(mem.diedAt)
    ||stats.spawned!==mem.spawned||![0,1].includes(stats.kills)||stats.retired!==0||S.next!==mem.spawned+1
    ||S.stalkers.length!==mem.spawned-stats.kills||(mem.diedAt>=0)!==(stats.kills===1)) return 'inconsistent workshop guardian history';
  if (!['pursuits','attacks','hits','dodged'].every(k=>int(stats[k as keyof typeof stats]))||!num(stats.contactS)||stats.contactS<0||stats.attacks!==stats.hits+stats.dodged) return 'invalid workshop guardian statistics';
  for (const s of S.stalkers) {
    if (!s||s.id!==1||s.kind!=='stalker'||s.site!==d.block||s.hx!==d.x+.5||s.hy!==d.y+.5||!xy(s.x,s.y)||!xy(s.px,s.py)
      ||!num(s.hp)||s.hp<=0||s.hp>stalkerHp(S.cand)||!['guard','investigate','pursue','attack','return'].includes(s.mode)
      ||!time(s.modeT)||!time(s.born)||!num(s.planT)||!int(s.goal,-1)||s.goal>=st.flow.tw*st.city.th||typeof s.warmed!=='boolean'
      ||![s.windup,s.atk,s.lost,s.stuck].every(n=>num(n)&&n>=0)||!Array.isArray(s.dir)||s.dir.length!==2||!s.dir.every(num)
      ||!Array.isArray(s.path)||s.path.some(t=>!int(t)||t>=st.flow!.tw*st.city!.th)) return 'invalid workshop guardian body';
  }
  return '';
}
