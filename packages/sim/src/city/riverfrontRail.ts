import {RIVERFRONT} from './riverfront';

export interface RailSample {x:number;y:number;d:number;angle:number}
let cached:{samples:RailSample[];tiles:number[];distances:number[];index:Map<number,number>;reserved:Set<number>;length:number}|undefined;
/** Circular fillets with tangent straights. Logical cells only bind existing freight/stops;
 * art, passengers and vehicle motion all sample continuous distance on this centreline. */
export function riverfrontRail(){
 if(cached)return cached;
 const samples:RailSample[]=[],points=RIVERFRONT.tram.map(([x,y])=>({x:x+.5,y:y+.5})),r=RIVERFRONT.tramRadius;
 const push=(x:number,y:number,angle:number)=>{const p=samples.at(-1);samples.push({x,y,angle,d:p?p.d+Math.hypot(x-p.x,y-p.y):0});};
 const straight=(a:{x:number;y:number},b:{x:number;y:number})=>{const len=Math.hypot(b.x-a.x,b.y-a.y),n=Math.ceil(len*4),angle=Math.atan2(b.y-a.y,b.x-a.x);for(let j=samples.length?1:0;j<=n;j++)push(a.x+(b.x-a.x)*j/n,a.y+(b.y-a.y)*j/n,angle);};
 let from=points[0];
 for(let i=1;i<points.length-1;i++){
  const a=points[i-1],b=points[i],c=points[i+1],l=Math.hypot(b.x-a.x,b.y-a.y),ll=Math.hypot(c.x-b.x,c.y-b.y),u={x:(b.x-a.x)/l,y:(b.y-a.y)/l},v={x:(c.x-b.x)/ll,y:(c.y-b.y)/ll};
  const pre={x:b.x-u.x*r,y:b.y-u.y*r},post={x:b.x+v.x*r,y:b.y+v.y*r},centre={x:pre.x+v.x*r,y:pre.y+v.y*r},turn=Math.sign(u.x*v.y-u.y*v.x),start=Math.atan2(pre.y-centre.y,pre.x-centre.x),n=Math.ceil(Math.PI*r*2);
  straight(from,pre);for(let j=1;j<=n;j++){const angle=start+turn*Math.PI/2*j/n;push(centre.x+r*Math.cos(angle),centre.y+r*Math.sin(angle),angle+turn*Math.PI/2);}from=post;
 }
 straight(from,points.at(-1)!);
 const tw=RIVERFRONT.width,tiles:number[]=[],distances:number[]=[],index=new Map<number,number>();
 const add=(x:number,y:number)=>{const t=y*tw+x;if(tiles.at(-1)!==t){index.set(t,tiles.length);tiles.push(t);}};
 for(const p of samples){const x=Math.floor(p.x),y=Math.floor(p.y),last=tiles.at(-1);if(last!==undefined&&x!==last%tw&&y!==Math.floor(last/tw))add(x,Math.floor(last/tw));add(x,y);}
 let cursor=0;
 for(const t of tiles){const x=t%tw+.5,y=Math.floor(t/tw)+.5;let best=Infinity,at=cursor;for(let j=cursor;j<Math.min(samples.length,cursor+18);j++){const p=samples[j],dd=(x-p.x)**2+(y-p.y)**2;if(dd<best){best=dd;at=j;}}cursor=at;distances.push(Math.max(samples[at].d,(distances.at(-1)??-.02)+.02));}
 distances[distances.length-1]=samples.at(-1)!.d;
 const reserved=new Set<number>();
 for(const p of samples)for(let y=Math.floor(p.y)-4;y<=Math.floor(p.y)+4;y++)for(let x=Math.floor(p.x)-4;x<=Math.floor(p.x)+4;x++)if(Math.hypot(x+.5-p.x,y+.5-p.y)<=RIVERFRONT.railHalf+Math.SQRT1_2)reserved.add(y*tw+x);
 return cached={samples,tiles,distances,index,reserved,length:samples.at(-1)!.d};
}
export function riverfrontRailPose(distance:number):RailSample{
 const {samples,length}=riverfrontRail(),d=Math.max(0,Math.min(length,distance));let lo=0,hi=samples.length-1;while(lo+1<hi){const mid=(lo+hi)>>1;if(samples[mid].d<=d)lo=mid;else hi=mid;}
 const a=samples[lo],b=samples[hi],f=(d-a.d)/(b.d-a.d||1),delta=Math.atan2(Math.sin(b.angle-a.angle),Math.cos(b.angle-a.angle));return{x:a.x+(b.x-a.x)*f,y:a.y+(b.y-a.y)*f,d,angle:a.angle+delta*f};
}
export function riverfrontTramPose(m:{x:number;y:number;timer:number;phase:number;run?:{fwd:boolean}}){const rail=riverfrontRail(),i=rail.index.get(m.y*RIVERFRONT.width+m.x)??0,fwd=m.run?.fwd!==false,p=riverfrontRailPose(rail.distances[i]+(m.phase===0?m.timer*(fwd?1:-1):0));return{...p,angle:p.angle+(fwd?0:Math.PI)};}
