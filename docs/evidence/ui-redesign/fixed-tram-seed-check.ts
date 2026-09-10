import {createCampaign, fixedStops, fixedTramProblem, stateProblem, blockOfTile, ground, passable, fixedPoweredStops, validateCampaignCity} from '../../../packages/sim/src/index';
let failures=0;
for(let seed=1;seed<=32;seed++){try{const st=createCampaign(seed),G=ground(st),stops=fixedStops(st),problem=fixedTramProblem(st)||stateProblem(st);
 const row={seed,stops:stops.map(m=>({x:m.x,y:m.y,block:blockOfTile(st,m.x,m.y)})),powered:fixedPoweredStops(st).length,gateOpen:G.opening!.gate.every(t=>passable(st,t%G.tw,Math.floor(t/G.tw))),problem};
 if(problem||new Set(row.stops.map(s=>s.block)).size!==4||row.powered!==1||!row.gateOpen)failures++;
 console.log(JSON.stringify(row));
}catch(e){failures++;console.log(JSON.stringify({seed,error:String(e)}));}}
console.log(JSON.stringify({seeds:32,failures}));process.exitCode=failures?1:0;
