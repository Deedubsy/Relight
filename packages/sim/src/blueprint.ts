/** P8-01: paid, local blueprint stamps. The clipboard is configuration, never contents. */
import type { SimState } from './types';
import { MACHINE_SIZE, type Dir, type Machine } from './flow';
import { dimensions, machineDimensions } from './footprint';
import { inReach } from './ground';
import { campaignRecruited } from './campaignRecruits';
import { parseBlueprint, type Blueprint, type BlueprintEntity } from './blueprintFormat';
import { constructionCheck, type ActionResult, type BuildEdit, type TilePoint } from './construction';

export type BlueprintTransform = 'rotate' | 'mirrorX' | 'mirrorY';
export function blueprintGate(st:SimState):string {
  return !st.campaign || !campaignRecruited(st,'foreman') ? 'Recruit the Foreman to use the blueprint clipboard.'
    : st.engineer.down>=0 ? 'Wait until you recover to use construction tools.' : '';
}
export function blueprintBounds(bp:Blueprint):{x:number;y:number;w:number;h:number} {
  const x=Math.min(...bp.entities.map(e=>e.x)), y=Math.min(...bp.entities.map(e=>e.y));
  return {x,y,w:Math.max(...bp.entities.map(e=>e.x+dimensions(e.kind,e.dir,MACHINE_SIZE[e.kind])[0]))-x,
    h:Math.max(...bp.entities.map(e=>e.y+dimensions(e.kind,e.dir,MACHINE_SIZE[e.kind])[1]))-y};
}
function normalise(bp:Blueprint):Blueprint {
  const b=blueprintBounds(bp);
  return parseBlueprint({...bp,entities:bp.entities.map(e=>({...e,x:e.x-b.x,y:e.y-b.y}))});
}
function configuration(m:Machine,id:string,x:number,y:number):BlueprintEntity {
  if(m.kind==='depot')throw new Error('The home supply installation cannot be copied.');
  return {id,kind:m.kind,x,y,dir:m.dir,
    ...(['assembler','assembler2','foundry','refinery'].includes(m.kind)?{recipe:m.recipe??'shot'}:{}),
    ...(m.filter?{filter:m.filter}:{}), ...(m.kind==='splitter'?{priority:m.priority??'balanced'}:{}),
    ...(m.underground?{underground:m.underground}:{}), ...(m.freight?{freight:structuredClone(m.freight)}:{})};
}
export function blueprintCopy(st:SimState,from:TilePoint,to:TilePoint):Blueprint {
  const why=blueprintGate(st);if(why)throw new Error(why);
  if(!from||!to||![from.x,from.y,to.x,to.y].every(Number.isSafeInteger))throw new Error('Invalid selection coordinates.');
  const x=Math.min(from.x,to.x),y=Math.min(from.y,to.y),w=Math.abs(to.x-from.x)+1,h=Math.abs(to.y-from.y)+1;
  if(w>128||h>128)throw new Error('Select an area no larger than 128 × 128 tiles.');
  const entities:BlueprintEntity[]=[];
  for(const m of st.flow!.machines){const [mw,mh]=machineDimensions(m);
    if(m.x>=x+w||m.x+mw<=x||m.y>=y+h||m.y+mh<=y)continue;
    if(m.x<x||m.y<y||m.x+mw>x+w||m.y+mh>y+h)throw new Error('Select the whole footprint of every machine.');
    if(!inReach(st,m.x,m.y,mw,mh))throw new Error('Walk closer to copy these machines.');
    entities.push(configuration(m,`machine${entities.length}`,m.x-x,m.y-y));
  }
  if(!entities.length)throw new Error('No complete machines in this selection.');
  // Track goes down before any overlaid vehicle, independent of machine insertion order.
  entities.sort((a,b)=>Number(a.kind==='tram')-Number(b.kind==='tram'));
  return normalise(parseBlueprint({version:1,name:'Copied layout',entities}));
}
export function blueprintTransform(raw:Blueprint,operation:BlueprintTransform):Blueprint {
  if(!['rotate','mirrorX','mirrorY'].includes(operation))throw new Error('Invalid blueprint transform.');
  const bp=normalise(parseBlueprint(raw)),b=blueprintBounds(bp);
  return parseBlueprint({...bp,entities:bp.entities.map(e=>{
    const [w,h]=dimensions(e.kind,e.dir,MACHINE_SIZE[e.kind]);
    const mirrored=operation!=='rotate';
    return {...e,x:operation==='rotate'?b.h-e.y-h:operation==='mirrorX'?b.w-e.x-w:e.x,
      y:operation==='rotate'?e.x:operation==='mirrorY'?b.h-e.y-h:e.y,
      dir:(operation==='rotate'?(e.dir+1)%4:operation==='mirrorX'?(4-e.dir)%4:(2-e.dir+4)%4) as Dir,
      ...(mirrored&&e.priority&&e.priority!=='balanced'?{priority:e.priority==='left'?'right' as const:'left' as const}:{})};
  })});
}
export function blueprintEdits(st:SimState,x:number,y:number):BuildEdit[] {
  const why=blueprintGate(st);if(why)throw new Error(why);
  if(!Number.isSafeInteger(x)||!Number.isSafeInteger(y))throw new Error('Invalid blueprint destination.');
  if(!st.campaign!.clipboard)throw new Error('Copy a layout first (Ctrl+C).');
  return parseBlueprint(st.campaign!.clipboard).entities.map(({id:_id,kind,...e})=>({action:'place',item:kind,...e,x:x+e.x,y:y+e.y}));
}
export function blueprintCheck(st:SimState,x:number,y:number):ActionResult {
  try {return constructionCheck(st,blueprintEdits(st,x,y));}
  catch(e){return {ok:false,reason:(e as Error).message};}
}
export function clipboardProblem(st:SimState):string {
  const bp=st.campaign?.clipboard;if(bp===undefined)return '';
  if((st.campaign?.version??0)<10||!campaignRecruited(st,'foreman'))return 'clipboard requires recruited Foreman and campaign metadata version 10';
  try {parseBlueprint(bp);return ''; }catch(e){return `invalid clipboard: ${(e as Error).message}`;}
}
