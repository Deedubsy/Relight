import {RIVERFRONT_BUILDINGS as bs,RIVERFRONT,lineTiles} from '../../../packages/sim/src/city/riverfront';
const rows=bs.map(b=>({id:b.id,roads:RIVERFRONT.roads.flatMap(p=>lineTiles(p)).filter(t=>t%360>=b.x&&t%360<b.x+b.w&&Math.floor(t/360)>=b.y&&Math.floor(t/360)<b.y+b.h).length})).filter(r=>r.roads);console.log(rows);
