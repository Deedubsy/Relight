import {readFileSync} from 'node:fs';
import {loadState,truckSegmentFits,truckWorkRoute,placementGeometryProblem,ground} from '../../../packages/sim/src/index';
const st=loadState(JSON.parse(readFileSync(new URL('paid-complete/failure-save-842.json',import.meta.url),'utf8'))),t=st.campaign!.truck!,G=ground(st);
console.log(JSON.stringify({truck:t,engineer:st.engineer,neighbours:[{...t,x:t.x+1},{...t,x:t.x-1},{...t,y:t.y+1},{...t,y:t.y-1},...([0,1,2,3] as const).map(dir=>({...t,dir}))].map(p=>({pose:p,fits:truckSegmentFits(st,t,p)}))}));
