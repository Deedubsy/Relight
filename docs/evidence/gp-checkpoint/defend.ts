import assert from 'node:assert/strict';
import {writeFileSync,readFileSync} from 'node:fs';
import {createCampaign,applyCommands,advanceFlow,ground,blockOfTile,machineRunning,rubbleAt,canPlace,findPath,passable,conservation,stateProblem,actionResult,makeSave,workbenchTile,loadState,fixedStops,conveyorDestinations,damageCore,repairCheck,FIRST_CAMPS,activeWeapon,citySight,canStand,equipmentCommand,tickEquipment,type Command,type Item,type Kind} from '../../../packages/sim/src/index';
const prior=JSON.parse(readFileSync('docs/evidence/gp-checkpoint/ordinary-save.json','utf8')),st=loadState(prior),G=ground(st),log:{tick:number;c:Command}[]=prior.log,milestones:unknown[]=[];
const cmd=(c:Command)=>{log.push({tick:st.flow!.tick,c});applyCommands(st,[c]);};
cmd({type:'setSpeed',mult:1});
const samples:number[]=[];function run(sec:number){const before=performance.now();advanceFlow(st,sec,[],Math.ceil(sec*20)+1);samples.push((performance.now()-before)/Math.max(1,sec*20));}
function walk(x:number,y:number){const path=findPath(st,Math.floor(st.engineer.x),Math.floor(st.engineer.y),Math.floor(x),Math.floor(y));assert.ok(path,`reachable ${x},${y}`);for(const t of path){const tx=t%G.tw+.5,ty=Math.floor(t/G.tw)+.5;let ticks=0;while(Math.hypot(tx-st.engineer.x,ty-st.engineer.y)>.23){assert.ok(ticks++<60,`walking blocked ${t}`);const dx=tx-st.engineer.x,dy=ty-st.engineer.y,n=Math.hypot(dx,dy);cmd({type:'walk',dx:dx/n,dy:dy/n});run(.05);}}cmd({type:'walk',dx:0,dy:0});}
function near(x:number,y:number,size=1){for(let radius=1;radius<7;radius++)for(let dy=-radius;dy<=radius;dy++)for(const dx of [-radius,radius])if(!(dx>=0&&dx<size&&dy>=0&&dy<size)&&passable(st,x+dx,y+dy)&&findPath(st,Math.floor(st.engineer.x),Math.floor(st.engineer.y),x+dx,y+dy)){walk(x+dx,y+dy);return;}throw Error('No reachable interaction position');}
function mine(item:Item,count:number){const target=(st.engineer.inv[item]??0)+count;while((st.engineer.inv[item]??0)<target){const home=G.blocks[st.campaign!.homeBlock];let pick:{x:number;y:number}|null=null;for(let y=home.y0;y<home.y1&&!pick;y++)for(let x=home.x0;x<home.x1;x++){const r=rubbleAt(st,x,y);if(r?.type===item&&!r.persistent){pick={x,y};break;}}assert.ok(pick,`bootstrap ${item}`);near(pick.x,pick.y);cmd({type:'mineAt',...pick});const before=st.engineer.inv[item]??0;while((st.engineer.inv[item]??0)<target&&st.flow!.hand.mine)run(.05);assert.ok((st.engineer.inv[item]??0)>before);cmd({type:'mineAt',x:-1,y:-1});}}
function build(kind:Kind){
 const desired=kind==='turret'?[67,364]:kind==='assembler'?[71,363]:[94,355];
 const spots:{x:number;y:number;d:number}[]=[];for(let y=350;y<383;y++)for(let x=60;x<112;x++)spots.push({x,y,d:Math.hypot(x-desired[0],y-desired[1])});spots.sort((a,b)=>a.d-b.d);
 for(const {x,y}of spots)if(canPlace(st,kind,x,y).ok){near(x,y,kind==='turret'?2:3);cmd({type:'construct',edits:[{action:'place',item:kind,x,y,dir:3}]});assert.ok(actionResult(st).ok,actionResult(st).reason);console.log('built',kind,x,y);return st.flow!.machines.at(-1)!;}throw Error('No legal '+kind);
}

function checked(c:Command){cmd(c);assert.ok(actionResult(st).ok,actionResult(st).reason);}
function reload(){cmd({type:'aim',at:null});checked({type:'equipment',action:{type:'reload'}});run(1.55);}
function fight(source:string){
 const T=st.flow!.threat!,e=st.engineer;let ticks=0;
 while(T.crawlers.some(c=>c.gp?.source===source)){
  if(e.down>=0){milestones.push({step:'combat knockdown; earned keys and guardian damage retained',t:st.t,source});if(e.downs>2)throw Error('Repeated scripted combat knockdown');cmd({type:'aim',at:null});cmd({type:'walk',dx:0,dy:0});run(11);const [wx,wy]=workbenchTile(st);walk(wx,wy);checked({type:'factory',action:{type:'craft',item:'magazine',count:12}});run(73);near(66,62);}
  if(ticks++>16000)throw Error('Fight timeout '+source);
  const enemies=T.crawlers.filter(c=>c.gp?.source===source).sort((a,b)=>Math.hypot(a.x-e.x,a.y-e.y)-Math.hypot(b.x-e.x,b.y-e.y)),target=enemies.find(c=>citySight(st,e.x,e.y,c.x,c.y))??enemies[0],dist=Math.hypot(target.x-e.x,target.y-e.y),w=activeWeapon(st)!;
  if(w.loaded===0&&!w.reload)cmd({type:'equipment',action:{type:'reload'}});
  cmd({type:'aim',at:[target.x,target.y]});
  let best:{dx:number;dy:number;score:number}|null=null;
  for(const [dx,dy] of [[0,0],[1,0],[-1,0],[0,1],[0,-1],[.707,.707],[-.707,.707],[.707,-.707],[-.707,-.707]]){
   const x=e.x+dx*.7,y=e.y+dy*.7;if(!canStand(st,x,y))continue;
   const nearest=Math.min(...enemies.map(c=>Math.hypot(c.x-x,c.y-y))),d=Math.hypot(target.x-x,target.y-y),visible=citySight(st,x,y,target.x,target.y);
   const score=(nearest<3?(3-nearest)*30:0)+Math.abs(d-(w.reload>0?9:7))+(visible?0:5);
   if(!best||score<best.score)best={dx,dy,score};
  }
  if(!citySight(st,e.x,e.y,target.x,target.y)&&dist>9){cmd({type:'move',x:target.x,y:target.y});run(.05);continue;}
  if(best)cmd({type:'walk',dx:best.dx,dy:best.dy});
  if(target.gp?.phase==='windup'&&target.gp.kind==='guardian')cmd({type:'dodge'});
  run(.05);
 }
 cmd({type:'walk',dx:0,dy:0});cmd({type:'aim',at:null});milestones.push({step:'defeated '+source,t:st.t,hp:e.hp,rounds:e.fired});console.log(milestones.at(-1));
}

const tower=st.flow!.machines.find(m=>m.kind==='turret')!,generator=st.flow!.machines.find(m=>m.kind==='generator')!;
// Ordinary continuation: carry the expedition's real remaining reserve back to defend Home.
near(tower.x,tower.y);if((tower.inv.rounds??0)<=40)checked({type:'factory',action:{type:'feed',x:tower.x,y:tower.y}});
near(generator.x,generator.y);checked({type:'factory',action:{type:'feed',x:generator.x,y:generator.y}});
near(tower.x,tower.y);milestones.push({step:'returned to Home on foot and replenished actual fuel/ammo',t:st.t,inv:{...st.engineer.inv}});
let supplied=false;
for(let y=360;y<375&&!supplied;y++)for(let x=62;x<77&&!supplied;x++)for(const dir of [0,1,2,3] as const){
 const dx=[0,1,0,-1][dir],dy=[-1,0,1,0][dir],bx=x+dx,by=y+dy,cx=x-dx,cy=y-dy;
 const receiver=st.flow!.machines.find(m=>bx>=m.x&&bx<m.x+m.size&&by>=m.y&&by<m.y+m.size&&(m.kind==='turret'||m.kind==='belt'));
 if(!receiver||!canPlace(st,'belt',x,y,dir).ok||!canPlace(st,'chest',cx-(dx>0?1:0),cy-(dy>0?1:0),dir).ok)continue;
 const sx=cx-(dx>0?1:0),sy=cy-(dy>0?1:0);near(sx,sy,2);checked({type:'construct',edits:[{action:'place',item:'chest',x:sx,y:sy,dir}]});near(x,y);checked({type:'construct',edits:[{action:'place',item:'belt',x,y,dir}]});
 near(sx,sy,2);checked({type:'factory',action:{type:'chestPut',x:sx,y:sy,item:'magazine',n:Math.min(6,Math.floor(st.engineer.inv.magazine??0))}});supplied=true;milestones.push({step:'physical chest/conveyor ammunition resupply',chest:[sx,sy],belt:[x,y]});break;
}
assert.ok(supplied);near(tower.x,tower.y);
const end=st.campaign!.defence!.major!.endsAt!+120;
while(st.t<end){const enemy=st.flow!.threat!.crawlers.filter(c=>c.campaign?.layer==='major').sort((a,b)=>Math.hypot(a.x-st.engineer.x,a.y-st.engineer.y)-Math.hypot(b.x-st.engineer.x,b.y-st.engineer.y))[0];if(enemy){const w=activeWeapon(st)!;if(!w.loaded&&!w.reload)cmd({type:'equipment',action:{type:'reload'}});cmd({type:'aim',at:citySight(st,st.engineer.x,st.engineer.y,enemy.x,enemy.y)&&Math.hypot(st.engineer.x-enemy.x,st.engineer.y-enemy.y)<9?[enemy.x,enemy.y]:null});}run(.05);}
cmd({type:'aim',at:null});writeFileSync('docs/evidence/gp-checkpoint/defence-observed.json',JSON.stringify({milestones,state:st}));assert.ok(st.campaign!.defence!.bases[0].hp>0,'ordinary supplied Home survived the first major');
milestones.push({step:'first Home major survived with ordinary supplied turret/Rifle',t:st.t,hp:st.campaign!.defence!.bases[0].hp,history:st.campaign!.defence!.history,ammo:st.engineer.inv.magazine});
assert.equal(stateProblem(st),'');assert.ok(conservation(st).ok,conservation(st).problems.join(';'));
writeFileSync('docs/evidence/gp-checkpoint/defended-save.json',JSON.stringify(makeSave(st,{log,logComplete:true})));writeFileSync('docs/evidence/gp-checkpoint/defended-result.json',JSON.stringify(milestones,null,2));console.log(milestones);
