import {createState,citySpec,ensureFlow,ground,initExpansion,campaignConfig} from '../../../packages/sim/src/index';
for(const seed of [305,1340,7384,8102,9711]){
 const config=campaignConfig(),st=createState(citySpec(seed,'river',config),config,seed,'exploration-v2');ensureFlow(st);initExpansion(st);
 const G=ground(st),e=st.campaign!.expansion!;
 const b=G.blocks[e.station.block],pad=G.urban!.places.find(p=>p.block===e.station.block)!.pad;
 console.log(JSON.stringify({seed,station:e.station,radio:e.radio,pad,bounds:[b.x0,b.y0,b.x1,b.y1]}));
}
