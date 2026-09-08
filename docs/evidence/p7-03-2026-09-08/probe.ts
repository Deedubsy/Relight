import {createCampaign,ground,rubbleAt,findPath} from '../../../packages/sim/src/index';
for(let seed=1;seed<=32;seed++){
 const s=createCampaign(seed),g=ground(s),t=s.campaign!.turbine!;
 console.log(JSON.stringify({seed,turbine:t,crews:s.campaign!.recruits!.sites.map(x=>[x.kind,x.x,x.y]),stone:g.blocks[s.campaign!.homeBlock].tiles.filter(x=>rubbleAt(s,x%g.tw,Math.floor(x/g.tw))?.type==='stone').length,path:!!findPath(s,Math.floor(s.engineer.x),Math.floor(s.engineer.y),t.x-1,t.y)}));
}
