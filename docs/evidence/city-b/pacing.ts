import * as S from '../../../packages/sim/src/index';
import {truckWorkRoute,truckSegmentFits} from '../../../packages/sim/src/truckWork';
import {initTruck} from '../../../packages/sim/src/truck';
import {writeFileSync} from 'node:fs';
import assert from 'node:assert/strict';
const s=S.createCampaign();const home=[35,191] as const;
const distance=(a:readonly number[],b:readonly number[])=>{const p=S.findPath(s,a[0],a[1],b[0],b[1]);assert.ok(p,`${a} -> ${b}`);let last=a[1]*530+a[0],d=0;for(const t of p){d+=Math.hypot(t%530-last%530,Math.floor(t/530)-Math.floor(last/530));last=t;}return d/S.WALK_TILES_PER_S;};
const rows=[['steel',24,187],['copper',28,190],['coal',40,190],['first optional workshop',162,97],['Riverside entrance',171,208],['T1',44,217],['T2',186,225],['Civic entrance',440,199],['freight expedition',37,55],['quarry expedition',482,54],['wharf expedition',503,255]].map(([id,x,y])=>({id,walkSeconds:distance(home,[Number(x),Number(y)])}));
const lastMile=S.RIVERFRONT.plants.map((p,i)=>{const b=S.RIVERFRONT_BUILDINGS.find(b=>b.content===p.id)!,stop=S.RIVERFRONT.stops[i+1];return {plant:p.name,walkToStop:distance([b.door![0]+1,b.door![1]+1],[stop.x+1,stop.y-1])};});
const route=s.campaign!.fixedTram!.route,stopIndices=S.fixedStops(s).map(m=>route.findIndex(t=>{const x=t%530,y=Math.floor(t/530);return (x===m.x-1||x===m.x+2)&&y>=m.y&&y<m.y+2||(y===m.y-1||y===m.y+2)&&x>=m.x&&x<m.x+2;}));
const moving=(stopIndices[3]-stopIndices[0])/S.RIVERFRONT.tramSpeed;
writeFileSync('docs/evidence/city-b/pacing.json',JSON.stringify({walkSpeed:S.WALK_TILES_PER_S,rows,lastMile,stopIndices,tram:{speed:S.RIVERFRONT.tramSpeed,homeToCivicMoving:moving,allPoweredDoorToDoor:distance(home,[44,217])+moving+3*S.RIVERFRONT.tramDwell+lastMile[2].walkToStop,homeCivicOnlyDoorToDoor:distance(home,[44,217])+moving+S.RIVERFRONT.tramDwell+lastMile[2].walkToStop,allPoweredRoundTrip:2*(route.length-1)/S.RIVERFRONT.tramSpeed+6*S.RIVERFRONT.tramDwell,explanation:'A* path length / walk speed; no sprint, combat or loading delay. 1.5 seconds dwell per powered stop; waiting is 0 to one round trip.'}},null,2));
s.campaign!.expansion!.station.restoredAt=0;initTruck(s);assert.ok(s.campaign!.truck);const start={...s.campaign!.truck!},truck=[];
for(const [i,y]of S.RIVERFRONT.yards.entries()){Object.assign(s.campaign!.truck!,start);const begin=performance.now(),p=truckWorkRoute(s,[{x:y.x,y:y.y,w:y.w,h:y.h}],8);let previous=start;for(const q of p??[]){assert.ok(truckSegmentFits(s,previous,q));previous={...previous,...q};}truck.push({yard:i,steps:p?.length??null,milliseconds:performance.now()-begin});}
writeFileSync('docs/evidence/city-b/truck-routes.json',JSON.stringify({spawn:start,routes:truck},null,2));assert.ok(truck.every(r=>r.steps!==null),'Every factory reachable by existing road-sized truck');
