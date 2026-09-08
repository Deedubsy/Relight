import assert from 'node:assert/strict';
import {readFileSync} from 'node:fs';
import {createCampaign,ground,findPath,canPlace,loadState,makeSave,stateHash,conservation,advanceFlow,campaignDiscoveries,ITEMS,itemGuide} from '../../../packages/sim/src/index';
for(let seed=1;seed<=64;seed++){
 const s=createCampaign(seed),g=ground(s),c=s.campaign!,t=c.turbine!,start=[Math.floor(s.engineer.x),Math.floor(s.engineer.y)];
 const sites=[...c.recruits!.sites.map(r=>({id:r.kind,x:r.x,y:r.y})),{id:'turbine',x:t.x-1,y:t.y}];
 const paths=sites.map(p=>{const path=findPath(s,start[0],start[1],p.x,p.y);assert.ok(path,`seed ${seed} ${p.id}`);return {id:p.id,tiles:path.length};});
 const routes=[...c.expansion!.route,...c.districts!.route];
 assert.ok(routes.every(q=>!(q%g.tw>=t.x&&q%g.tw<t.x+t.size&&Math.floor(q/g.tw)>=t.y&&Math.floor(q/g.tw)<t.y+t.size)));
 for(const r of c.recruits!.sites){assert.match(canPlace(s,'pole',r.x,r.y).reason,/shelter/);assert.ok(!routes.includes(r.y*g.tw+r.x));}
 assert.equal(stateHash(loadState(makeSave(s))),stateHash(s));assert.equal(c.defence!.bases.length,1);
 const hash=stateHash(s);for(const i of ITEMS)itemGuide(s,i);assert.deepEqual(campaignDiscoveries(s).map(r=>r.id),['station']);assert.equal(stateHash(s),hash);
 console.log(JSON.stringify({seed,paths,corridors:true,reservations:true,save:true,hiddenInformation:true}));
}
for(const file of ['ex08b-2026-09-07/campaign/network.json','p7-02-2026-09-07/concrete-line.json','p7-03-2026-09-08/turbine-complete.json','p7-04-2026-09-08/guide-checkpoint.json']){
 const raw=JSON.parse(readFileSync('docs/evidence/'+file,'utf8')),old=raw.state??raw,s=loadState(raw);
 assert.equal(s.campaign!.version,9);assert.deepEqual(s.flow!.machines,old.flow.machines);assert.deepEqual(s.campaign!.defence,old.campaign.defence);assert.deepEqual(s.campaign!.discovery,old.campaign.discovery);assert.ok(conservation(s).ok);
 const loaded=loadState(makeSave(s));assert.equal(stateHash(loaded),stateHash(s));advanceFlow(s,2,[],41);advanceFlow(loaded,2,[],41);assert.equal(stateHash(loaded),stateHash(s));
 console.log(JSON.stringify({save:file,from:old.campaign.version,to:9,machines:true,defence:true,cache:true,conservation:true,continuation:true}));
}
