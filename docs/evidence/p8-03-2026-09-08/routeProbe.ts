import {readFileSync,writeFileSync} from 'node:fs';
import assert from 'node:assert/strict';
import {loadState,stateHash,truckWorkRoute,truckSegmentFits,TRUCK_WORK_RULES} from '../../../packages/sim/src/index';
const st=loadState(JSON.parse(readFileSync(new URL('./truck-ready.json',import.meta.url),'utf8'))),before=stateHash(st),o=st.campaign!.plans!.orders[0];
const began=performance.now(),route=truckWorkRoute(st,[{x:o.x,y:o.y,w:2,h:1}],8),reachableMs=performance.now()-began;assert.ok(route&&route.length);let prior=st.campaign!.truck!;for(const pose of route){assert.ok(truckSegmentFits(st,prior,pose));prior={...prior,...pose};}
const warmStart=performance.now(),warmRoute=truckWorkRoute(st,[{x:o.x,y:o.y,w:2,h:1}],8),warmReachableMs=performance.now()-warmStart;assert.deepEqual(warmRoute,route);
const start=performance.now(),unreachable=truckWorkRoute(st,[{x:0,y:0,w:2,h:1}],8),unreachableMs=performance.now()-start;assert.equal(unreachable,null);assert.equal(stateHash(st),before);
const result={rules:TRUCK_WORK_RULES,reachableSteps:route.length,coldReachableMs:reachableMs,warmReachableMs,unreachableMs,queryMutatedState:false,method:'Local headless query on the paid preparation state; First route includes cold geometry/cache setup; repeated route uses warm geometry. Unreachable map-corner query returns within the configured search bound. Not reference-machine certification.'};writeFileSync(new URL('./route-probe.json',import.meta.url),JSON.stringify(result,null,2));console.log(JSON.stringify(result));
