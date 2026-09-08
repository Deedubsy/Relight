import {readFileSync,readdirSync,writeFileSync,existsSync} from 'node:fs';
import {createHash} from 'node:crypto';
import assert from 'node:assert/strict';
import {createCampaign,stateHash,conservation} from '../../../packages/sim/src/index';
import {evidenceHash,evidenceProfileProblem} from '../../../packages/harness/src/provenance';
const folder='docs/evidence/p6-03-2026-09-07/recorded/campaign',out='docs/evidence/p6-03-2026-09-07/measurements.json';
assert.ok(!existsSync(out),'preserve captured measurement summary');
const names=readdirSync(folder).sort();assert.equal(names.length,9);
const expected=new Set(['turrets-3','turrets-4','turrets-5','turrets-8','turrets-11','turrets-13','support-3','support-4','support-5']);
const rows=[];let count=0;
for(const name of names){const file=folder+'/'+name,a=JSON.parse(readFileSync(file,'utf8')),m=a.measurements,s=a.finalState;
 assert.ok(expected.delete(a.mode+'-'+a.seed));assert.equal(a.checks.length,13);assert.ok(a.checks.every((c:any)=>c.pass));count+=a.checks.length;
 assert.equal(evidenceHash(a.config_ref),a.config_hash);assert.equal(evidenceProfileProblem(a.config_ref,file,a),'');
 assert.equal(stateHash(createCampaign(a.seed)),stateHash(a.initialState));assert.ok(conservation(s).ok);
 for(const [path,hash] of Object.entries(a.source_files))assert.equal(createHash('sha256').update(readFileSync(path)).digest('hex'),hash,path);
 assert.equal(createHash('sha256').update(JSON.stringify(a.source_files)).digest('hex'),a.source_tree_hash);
 const major=m.major,h=major.history[0],i=m.interruption,home=a.initialState.campaign.homeBlock;
 const remote=m.raids.filter((r:any)=>r.block!==home&&r.end>=0&&r.nearestEngineer>20&&r.rifleShots===0&&r.coreAfter>0);
 const stop=s.flow.machines.find((v:any)=>v.id===m.network.stops[2]);
 const counts:Record<string,number>={};for(const entry of a.log){const key=entry.c.type==='factory'?entry.c.action.type:entry.c.type;counts[key]=(counts[key]??0)+1;}
 rows.push({seed:a.seed,mode:a.mode,checks:a.checks.length,setupSeconds:m.setupSeconds,totalSeconds:m.durationSeconds,
 majorSeconds:h.ended-h.started,majorRoundsObserved:major.turretRounds,majorObservationStart:major.start,rifleShots:major.rifleShots,coreHpAtMajorEnd:major.coreHp,
 warningResponse:m.warningTravel.warnedAt,warningTravelSeconds:m.warningTravel.arrivedAt-m.warningTravel.warnedAt,arrivalLeadSeconds:m.warningTravel.leadSeconds,finalPreparationLead:3300-m.preparedAt,
 unattendedRemoteRaids:remote.length,remoteRaidRounds:remote.map((r:any)=>r.roundsSpent),awaySeconds:m.awayFromHomeSeconds,commands:m.manualCommands,commandTypes:counts,
 services:m.services.length,serviceSeconds:m.services.reduce((n:number,v:any)=>n+v.end-v.at,0),turretRoundsTotal:m.stats.fired,magazinesMade:m.stats.magsMade,
 coalBurned:m.stats.coalBurned,handMined:m.stats.handMinedOf,productionConsumed:m.stats.consumed,constructionNet:m.stats.placed,siteAndRepairSpent:{steel:s.stats.spentSteel,copper:s.stats.spentCopper},recovery:m.recovery,
 loadedAtCut:i.cargo,retainedAfterCut:i.afterCargo,deliveredDuringCut:i.afterMoved-i.beforeMoved,resumedDeliveries:i.afterRepairMoved-i.afterMoved,
 cutSeconds:i.repairedAt-i.cutAt,freightTotal:m.freight.moved,repairDelivery:m.repairDelivery,
 finalNorthRequestGap:Object.fromEntries(Object.entries(stop.freight).map(([item,p]:any)=>[item,Math.max(0,p.request-(stop.cargo[item]??0)-(stop.inv[item]??0))])),sourceTree:a.source_tree_hash});
}
assert.equal(expected.size,0);assert.equal(new Set(rows.map(r=>r.sourceTree)).size,1);
writeFileSync(out,JSON.stringify({checksPassed:count,rows},null,2)+'\n');console.log('Verified '+count+' checks in nine ordinary-start results, source/config hashes and fresh initial states.');
