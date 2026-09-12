import {DX,DY,inputTile,outputTile,machineDimensions,splitterPorts,type Machine,type SimState,type Dir} from '@relight/sim';
export type Port={x:number;y:number;dir:Dir;mode:'in'|'out'};
/** Conveyor loading points follow the footprint: arrow toward it loads, arrow away unloads. */
export function machinePorts(m:Machine):Port[]{
 const [w,h]=machineDimensions(m),ports:Port[]=[];
 const add=(x:number,y:number,dir:Dir,mode:'in'|'out')=>ports.push({x,y,dir,mode});
 if(m.kind==='inserter'){const a=inputTile(m),b=outputTile(m);add(a[0]+.5+DX[m.dir]*.5,a[1]+.5+DY[m.dir]*.5,m.dir,'in');add(b[0]+.5-DX[m.dir]*.5,b[1]+.5-DY[m.dir]*.5,m.dir,'out');return ports;}
 
 if(m.kind==='splitter'){for(const front of [false,true])for(const [x,y] of splitterPorts(m,front))add(x+.5-DX[m.dir]*(front?.5:-.5),y+.5-DY[m.dir]*(front?.5:-.5),m.dir,front?'out':'in');return ports;}
 if(m.kind==='underground'){const front=m.underground==='output';add(m.x+.5+DX[m.dir]*(front?.5:-.5),m.y+.5+DY[m.dir]*(front?.5:-.5),m.dir,front?'out':'in');return ports;}
 const both=['chest','tramstop','assembler','assembler2','foundry','refinery','mixer','turret','cannon','generator','depot'].includes(m.kind),takes=both||['excavator','pumpjack','belt','fastbelt'].includes(m.kind);
 if(!takes)return ports;
 for(let d=0;d<4;d++){const dir=d as Dir,n=d%2?h:w;for(let i=0;i<n;i++){
  const x=d===1?m.x+w:d===3?m.x:m.x+i+.5,y=d===0?m.y:d===2?m.y+h:m.y+i+.5;
  if(!['excavator','pumpjack'].includes(m.kind)&&(!['belt','fastbelt'].includes(m.kind)||d!==m.dir))add(x,y,((d+2)%4) as Dir,'in');
  if(both||['excavator','pumpjack'].includes(m.kind))add(x,y,dir,'out');
  else if(['belt','fastbelt'].includes(m.kind)&&d===m.dir)add(x,y,dir,'out');
 }}return ports;
}
export function connectionText(m:Machine,st?:SimState):string{
 const text:Partial<Record<Machine['kind'],string>>={
 chest:'IN: conveyor points into any side. OUT: conveyor touches any side and points away. Items transfer automatically.',
 assembler:'IN: conveyor feeds recipe materials on any side. OUT: conveyor points away from any side to take finished items.',
 mixer:'IN: conveyor feeds recipe materials on any side. OUT: conveyor points away from any side to take finished items.',
 tramstop:'IN: conveyors load the platform. OUT: conveyors unload arrivals first, then platform stock. Point away to unload.',
 inserter:'IN: cyan pickup tile. OUT: amber drop tile. Rotate with R. Optional filtered transfer; requires power.',
 excavator:'OUT: conveyor touching any side and pointing away takes mined material. No item input.',
 turret:'IN: conveyor delivers bullets. OUT: conveyor pointing away retrieves individual bullets.',
 generator:'IN: conveyor delivers Coal or Refined fuel. OUT: conveyor pointing away retrieves unused fuel.',
 depot:'IN: conveyor delivers supplies. OUT: conveyor pointing away takes stored materials or bullets.',
 belt:'IN: rear and side feeds. Place its rear against storage or a machine to take items automatically. OUT: arrow points downstream.',
 splitter:'IN: two rear tiles, including direct machine/storage pickup. OUT: two front tiles, following the arrows.',
 underground:m.underground==='output'?'OUT: front arrow to the next conveyor.':'IN: rear arrow, including direct machine/storage pickup; items travel to the paired exit.'};
 if(st&&!st.campaign&&['chest','assembler','mixer','tramstop'].includes(m.kind))return 'IN: belt or inserter. OUT: this legacy mode requires an inserter.';
 return text[m.kind]??(['assembler2','foundry','refinery'].includes(m.kind)?text.assembler:m.kind==='pumpjack'?text.excavator:m.kind==='fastbelt'?text.belt:m.kind==='cannon'?'IN: conveyor supplies Shells. OUT: conveyor pointing away retrieves unused Shells.':'')??'';
}
