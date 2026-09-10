import {createCampaign,stateProblem,conservation} from '../../../packages/sim/src/index';
const s=createCampaign();console.log(JSON.stringify({problem:stateProblem(s),machines:s.flow?.machines.length,sites:s.campaign?.progression?.sites.map(x=>[x.name,x.x,x.y]),ledger:conservation(s).problems}));
