import {createCampaign} from '../../../packages/sim/src/campaign';
import {stateProblem,loadState,stateHash} from '../../../packages/sim/src/save';
import {ground} from '../../../packages/sim/src/ground';
import {findPath,passable} from '../../../packages/sim/src/walk';
import {RIVERFRONT_BUILDINGS} from '../../../packages/sim/src/city/riverfront';
const st=createCampaign(),g=ground(st);console.log('state',stateProblem(st),'spawn',st.engineer.x,st.engineer.y,'machines',st.flow?.machines.length);
try {const copy=loadState(st);console.log('reload',stateHash(st)===stateHash(copy),stateProblem(copy));}catch(e){console.log(String(e));}
for(const b of RIVERFRONT_BUILDINGS.filter(b=>b.enterable)){const [x,y]=b.door!;const path=findPath(st,Math.floor(st.engineer.x),Math.floor(st.engineer.y),x,y);console.log(b.id,!!path,path?.length);}
console.log('track-blocked',st.campaign!.fixedTram!.route.filter(t=>!passable(st,t%g.tw,Math.floor(t/g.tw))).map(t=>[t%g.tw,Math.floor(t/g.tw)]));
