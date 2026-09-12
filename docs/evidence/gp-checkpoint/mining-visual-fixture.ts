import {writeFileSync} from 'node:fs';
import {createCampaign,ground,rubbleAt,passable,makeSave} from '../../../packages/sim/src/index';
const s=createCampaign(),G=ground(s),h=G.blocks[s.campaign!.homeBlock];
let found=false;for(let y=h.y0;y<h.y1&&!found;y++)for(let x=h.x0;x<h.x1&&!found;x++){const r=rubbleAt(s,x,y);if(!r||r.persistent)continue;for(const [dx,dy]of [[-1,0],[1,0],[0,-1],[0,1]])if(passable(s,x+dx,y+dy)){s.engineer.x=x+dx+.5;s.engineer.y=y+dy+.5;writeFileSync('docs/evidence/gp-checkpoint/mining-visual-save.json',JSON.stringify(makeSave(s,{logComplete:false})));writeFileSync('docs/evidence/gp-checkpoint/mining-visual-node.json',JSON.stringify({x,y}));found=true;break;}}
if(!found)throw Error('No accessible mining node');
