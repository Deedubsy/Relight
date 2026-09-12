import {test} from 'node:test';
import assert from 'node:assert/strict';
import {createCampaign,ensureFlow,addMachine,repairCheck,currentGoal,buildAffordability,MACHINE_COST,chestTake,chestCount,depotRect,campaignInteraction,stateHash,makeSave,loadState,ground,canPlace,inReach,applyCommands,campaignDiscoveries,navigationTargets,workbenchTile,type SimState} from '../src/index';
const fresh=()=>{const st=createCampaign(3);ensureFlow(st);return st;};
const next=(st:SimState)=>currentGoal(st).next!;
function assembler(st:SimState){st.stock.steel=50;st.stock.copper=50;chestTake(st,'steel',50);chestTake(st,'copper',50);const G=ground(st),p=G.opening!.bounds;for(let y=p.y;y<p.y+p.size;y++)for(let x=p.x;x<p.x+p.size;x++)if(inReach(st,x,y,3)&&canPlace(st,'assembler',x,y).ok){applyCommands(st,[{type:'place',item:'assembler',x,y}]);return st.flow!.machines.find(m=>m.kind==='assembler')!;}throw Error('No legal opening pad');}
test('UI-02 fresh and paused load point to mining with empty Home; partial pockets name only the remainder',()=>{
 const st=fresh();assert.match(next(st).text,/Mine the remaining materials/);assert.deepEqual(next(st).shortage,[{item:'steel',count:MACHINE_COST.excavator.steel,home:chestCount(st,'steel')}]);
 const loaded=loadState(makeSave(st));ensureFlow(loaded);assert.deepEqual(next(loaded),next(st));
 st.engineer.inv.steel=4;assert.equal(next(st).shortage![0].count,6);assert.match(next(st).detail,/6 steel/);
 st.engineer.inv.steel=10;assert.match(next(st).text,/Place an Excavator/);assert.ok(buildAffordability('excavator',st.engineer.inv).ok);
});
test('UI-02 packed machines skip collection, exhausted Home suggests a real known patch, not impossible supplies',()=>{
 const st=fresh();st.engineer.inv={excavator:1};assert.match(next(st).text,/Place an Excavator/);
 st.engineer.inv={};st.stock.steel=0;const n=next(st);assert.match(n.text,/Mine/);assert.ok(n.location);const G=ground(st);assert.equal(G.near[n.location!.y*G.tw+n.location!.x],st.campaign!.homeBlock);
});
test('UI-02 existing machinery is inspected, with saved facts surviving load and removal',()=>{
 const st=fresh(),m=assembler(st);assert.match(next(st).text,/place a Generator/);assert.doesNotMatch(next(st).text,/Collect/);assert.equal(next(st).title,'Power your workshop');
 const loaded=loadState(makeSave(st));ensureFlow(loaded);assert.deepEqual(next(loaded),next(st));
 // Historical-production fixture: genuine saved counters, no presentation achievement bit.
 st.flow!.stats.magsMade=1;st.flow!.stats.handCrafted=0;st.flow!.machines=st.flow!.machines.filter(x=>x.id!==m.id);st.engineer.inv={};
 assert.equal(next(st).title,'Keep your workshop producing');assert.match(next(st).text,/Mine/);
 st.flow!.stats.magsMade=0;assert.equal(next(st).title,'Start your workshop');assert.doesNotMatch(next(st).text,/never/);
});
test('UI-02 restored milestones remain restored while missing machinery needs rebuilding; down takes priority over tracking',()=>{
 const st=fresh(),site=st.campaign!.expansion!.station;site.restoredAt=0;
 assert.equal(next(st).title,'Keep your workshop producing');assert.equal(site.restoredAt,0);
 const tracked=currentGoal(st,'station').next!;assert.match(tracked.text,/Restored/);
 st.engineer.down=st.t+8;assert.equal(currentGoal(st,'station').next!.id,'recovery');assert.equal(campaignInteraction(st),null);
});
test('UI-02 tracking and minimap use known stable identities; invalid IDs fall back without revealing unknown sites',()=>{
 const st=fresh(),before=stateHash(st);assert.equal(next(st).id,'opening-workshop','opening identity is distinct from the later repair workshop');assert.deepEqual(currentGoal(st,'unknown'),currentGoal(st));
 assert.equal(currentGoal(st,'station').next!.id,'station');assert.ok(campaignDiscoveries(st).every(s=>navigationTargets(st).some(t=>t.id===`site:${s.id}`)));
 const t=st.campaign!.turbine!;assert.equal(t.seenAt,-1);assert.ok(!navigationTargets(st).some(s=>s.id===`site:${t.id}`));
 assert.ok(!campaignInteraction(st,{tx:t.x,ty:t.y})?.label.includes('Turbine'));
 assert.equal(stateHash(st),before);
});
test('UI-02 Home, workbench, machine and out-of-reach prompts resolve their actual E action without mutation',()=>{
 const st=fresh(),d=depotRect(st),before=stateHash(st),home=campaignInteraction(st,{tx:d.x,ty:d.y})!;
 assert.equal(home.label,'Open supplies');assert.equal(home.panel,'home');assert.equal(home.reason,'');assert.equal(stateHash(st),before);
 const [x,y]=workbenchTile(st),craft=campaignInteraction(st,{tx:x,ty:y})!;assert.equal(craft.label,'Craft one magazine');assert.deepEqual(craft.commands,[{type:'factory',action:{type:'craft',item:'magazine',count:1}}]);
 st.engineer.x=d.x+50;st.engineer.y=d.y+50;assert.match(campaignInteraction(st,{tx:d.x,ty:d.y})!.reason,/Walk closer/);
});
test('UI-02 site partial delivery remains available before power; repair wins for defences, while a damaged Home opens the workshop that repairs it',()=>{
 const st=fresh(),site=st.campaign!.expansion!.station;st.engineer.x=site.x;st.engineer.y=site.y;st.engineer.inv={};   // GP-START-POCKETS: empty the starting stake to model “no materials”
 assert.ok(campaignInteraction(st,{tx:site.x,ty:site.y})!.reason,'UI-05 explains a restore refusal when no materials can transfer');
 st.engineer.inv.steel=7;const target=campaignInteraction(st,{tx:site.x,ty:site.y})!;assert.equal(target.reason,'');assert.deepEqual(target.commands,[{type:'deliverSite',site:'station'}]);
 const d=st.campaign!.defence!.bases[0];d.hp-=1;st.engineer.x=d.x-.5;st.engineer.y=d.y;const home=campaignInteraction(st,{tx:d.x,ty:d.y})!;assert.equal(home.label,'Open Home workshop & storage');assert.equal(home.panel,'home');assert.deepEqual(home.commands,[]);   // GP-HOME-REPAIR: the Home core is the workshop; its Base core card carries the repair
 st.engineer.inv.steel=2;st.engineer.inv.copper=1;assert.equal(repairCheck(st,d.x,d.y),'','the card’s repair is available from the same spot');
 const wall=addMachine(st,'wall',d.x-2,d.y,0);wall.hp=1;const repair=campaignInteraction(st,{tx:wall.x,ty:wall.y})!;assert.equal(repair.label,'Repair Wall');assert.deepEqual(repair.commands,[{type:'repairDefence',x:wall.x,y:wall.y}]);
});

test('UI-02 machine reach is measured from the whole footprint, independent of which corner is pointed at',()=>{
 const st=fresh(),d=depotRect(st);st.engineer.x=d.x+d.size/2;st.engineer.y=d.y+d.size+8;
 assert.ok(inReach(st,d.x,d.y,d.size));
 assert.equal(campaignInteraction(st,{tx:d.x+d.size-1,ty:d.y+d.size-1})!.reason,'');
 st.engineer.y+=.1;assert.match(campaignInteraction(st,{tx:d.x,ty:d.y})!.reason,/Walk closer/);
});
