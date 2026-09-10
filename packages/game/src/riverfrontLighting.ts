/** View-sized presentation lighting. Gameplay litAt/lightMask rules remain authoritative and unchanged. */
import {RIVERFRONT_BUILDINGS,doorRect,ground,blockLights,machineAt,cityVisible,FLOODLIGHT_HALF_ANGLE,type SimState} from '@relight/sim';
const openRooms=new WeakMap<SimState,Set<string>>();
/** Shared visual hysteresis: a closed roof never receives the entered-room ambient mask. */
export function interiorOpen(st:SimState,b:typeof RIVERFRONT_BUILDINGS[number]):boolean {let rooms=openRooms.get(st);if(!rooms){rooms=new Set();openRooms.set(st,rooms);}const e=st.engineer;if(b.enterable&&e.x>b.x+1&&e.x<b.x+b.w-1&&e.y>b.y+1&&e.y<b.y+b.h-1)rooms.add(b.id);else if(e.x<b.x-.6||e.x>b.x+b.w+.6||e.y<b.y-.6||e.y>b.y+b.h+.6)rooms.delete(b.id);return rooms.has(b.id);}
export function daylight(t:number):number {const p=((t%1200)+1200)%1200,smooth=(v:number)=>v*v*(3-2*v);return p<780?1:p<900?1-smooth((p-780)/120):p<1110?0:smooth((p-1110)/90);}
export function paintRiverfrontLight(ctx:CanvasRenderingContext2D,st:SimState,x:number,y:number,w:number,h:number,scale:number,flash:{on:boolean;angle:number;range:number;half:number}):void {
 const G=ground(st),day=daylight(st.t),rgb=(a:number[],b:number[],n:number)=>a.map((v,i)=>Math.round(v+(b[i]-v)*n));const outside=rgb([38,49,68],[246,243,231],day),inside=rgb([18,28,43],[76,87,91],day);
 ctx.globalCompositeOperation='source-over';ctx.globalAlpha=1;ctx.fillStyle=`rgb(${outside})`;ctx.fillRect(0,0,w*scale,h*scale);
 for(const b of RIVERFRONT_BUILDINGS){if(!interiorOpen(st,b)||b.x+b.w<x||b.x>x+w||b.y+b.h<y||b.y>y+h)continue;ctx.fillStyle=`rgb(${inside})`;ctx.fillRect((b.x+1-x)*scale,(b.y+1-y)*scale,(b.w-2)*scale,(b.h-2)*scale);}
 const blocked=(tx:number,ty:number)=>{if(tx<0||ty<0||tx>=G.tw||ty>=G.th)return true;if(G.urban?.solid[ty*G.tw+tx])return true;const m=machineAt(st,tx,ty);return !!m&&(m.kind==='wall'||m.kind==='barricade')&&m.hp!==0;};
 // Bounded local ray fans stop light at actual opaque ground walls. No city-wide ray pass.
 const pool=(sx:number,sy:number,r:number,color:string,power:number,angle=0,half=Math.PI)=>{if(sx+r<x||sx-r>x+w||sy+r<y||sy-r>y+h||power<=0)return;ctx.save();ctx.beginPath();ctx.moveTo((sx-x)*scale,(sy-y)*scale);const rays=half<Math.PI?48:72;for(let i=0;i<=rays;i++){const a=angle-half+i*2*half/rays,c=Math.cos(a),s=Math.sin(a);let distance=r;for(let d=.4;d<r;d+=.4)if(blocked(Math.floor(sx+c*d),Math.floor(sy+s*d))){distance=d;break;}ctx.lineTo((sx+c*distance-x)*scale,(sy+s*distance-y)*scale);}ctx.closePath();ctx.clip();ctx.globalCompositeOperation='screen';const grad=ctx.createRadialGradient((sx-x)*scale,(sy-y)*scale,0,(sx-x)*scale,(sy-y)*scale,r*scale);grad.addColorStop(0,`rgba(${color},${power})`);grad.addColorStop(.3,`rgba(${color},${power*.75})`);grad.addColorStop(1,`rgba(${color},0)`);ctx.fillStyle=grad;ctx.fillRect((sx-r-x)*scale,(sy-r-y)*scale,r*2*scale,r*2*scale);ctx.restore();};
 for(let i=0;i<st.blocks.length;i++)for(const l of blockLights(st,i))if(l.lit)pool(l.tx+.5,l.ty+.5,l.r,'255,222,156',.96,('dir' in l&&typeof l.dir==='number')?l.dir*Math.PI/2-Math.PI/2:0,l.kind==='floodlight'?FLOODLIGHT_HALF_ANGLE:Math.PI);
 for(const b of RIVERFRONT_BUILDINGS)if(b.enterable&&b.door&&day>.01){const d=doorRect(b);pool(d.x+d.w/2,d.y+d.h/2,5,'183,198,190',day*.55);}
 for(const s of st.campaign?.progression?.sites??[])if(s.kind==='core'&&s.enabled&&!s.recovered&&cityVisible(st,s.x+1.5,s.y+1.5))pool(s.x+1.5,s.y+1.5,6,'93,175,148',.65);
 if(flash.on&&st.engineer.down<0){pool(st.engineer.x,st.engineer.y,flash.range,'248,236,202',.85,flash.angle,flash.half);pool(st.engineer.x,st.engineer.y,1.3,'193,215,212',.25);}
 ctx.globalAlpha=1;ctx.globalCompositeOperation='source-over';
}
