import type Phaser from 'phaser';
import {riverfrontRail,riverfrontRailPose,riverfrontTramPose,TILE_PX as P,type Machine} from '@relight/sim';
const motion=new WeakMap<Machine,{target:number;from:number;at:number}>();
/** Interpolate the 20 Hz state on the same curve; never extrapolate past a station. */
export function cityTramVisualPose(m:Machine,playing:boolean){
 const target=riverfrontTramPose(m),now=performance.now();let v=motion.get(m);
 if(!v||!playing||Math.abs(target.d-v.target)>5){v={target:target.d,from:target.d,at:now};motion.set(m,v);}
 const current=v.from+(v.target-v.from)*Math.min(1,(now-v.at)/50);
 if(v.target!==target.d){v.from=current;v.target=target.d;v.at=now;}
 const pose=riverfrontRailPose(current);return{...pose,angle:pose.angle+(m.run?.fwd===false?Math.PI:0)};
}
export function drawCityRail(g:Phaser.GameObjects.Graphics,view:Phaser.Geom.Rectangle){
 const points=riverfrontRail().samples,visible=(p:{x:number;y:number})=>p.x*P>view.left-4*P&&p.x*P<view.right+4*P&&p.y*P>view.top-4*P&&p.y*P<view.bottom+4*P;
 g.lineStyle(.13*P,0x665b47);let sleeper=-1;
 for(const p of points)if(visible(p)&&Math.floor(p.d*1.25)>sleeper){sleeper=Math.floor(p.d*1.25);const nx=-Math.sin(p.angle)*.7,ny=Math.cos(p.angle)*.7;g.lineBetween((p.x-nx)*P,(p.y-ny)*P,(p.x+nx)*P,(p.y+ny)*P);}
 for(const side of [-.38,.38]){g.lineStyle(.075*P,0xb6bdb1);g.beginPath();let pen=false;for(const p of points){if(!visible(p)){pen=false;continue;}const x=(p.x-Math.sin(p.angle)*side)*P,y=(p.y+Math.cos(p.angle)*side)*P;if(pen)g.lineTo(x,y);else g.moveTo(x,y);pen=true;}g.strokePath();}
}
export function drawCityTram(g:Phaser.GameObjects.Graphics,m:Machine,playing:boolean){
 const p=cityTramVisualPose(m,playing);g.save();g.translateCanvas(p.x*P,p.y*P);g.rotateCanvas(p.angle);
 g.fillStyle(0x172b2e,.7);g.fillRoundedRect(-1.5*P+.1*P,-.6*P+.12*P,3*P,1.2*P,.25*P);
 g.fillStyle(0xa55639);g.fillRoundedRect(-1.5*P,-.6*P,3*P,1.2*P,.25*P);g.lineStyle(.05*P,0xd7bd8c);g.strokeRoundedRect(-1.5*P,-.6*P,3*P,1.2*P,.25*P);
 g.fillStyle(0x758785);g.fillRoundedRect(-1.17*P,-.4*P,2.3*P,.8*P,.12*P);g.fillStyle(0x213e45);for(const x of [-1.4,1.2])g.fillRect(x*P,-.35*P,.18*P,.7*P);
 g.lineStyle(.055*P,0x384a49);for(const x of [-.6,0,.6])g.lineBetween(x*P,-.35*P,x*P,.35*P);
 g.fillStyle(0xf7d493);for(const y of [-.38,.32])g.fillRect(1.4*P,y*P,.08*P,.08*P);g.restore();
}
