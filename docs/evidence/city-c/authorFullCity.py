"""Compile declared city blocks and repeatable frontage templates; no random/seeded placement.
The emitted canonical TypeScript is the runtime authority. Re-run only when deliberately editing this blueprint.
"""
from pathlib import Path
import json,math,copy
root=Path(__file__).resolve().parents[3]
# The visual editor applies validated layouts to the canonical source. Do not silently replace them.
assert "RIVERFRONT_ID='riverfront-arc-v4-editor-" not in (root/'packages/sim/src/city/riverfront.ts').read_text(encoding='utf-8'),'Phaser Editor layout is applied. Restore/reconcile it through the editor workflow before regenerating the original blueprint.'
d=json.loads((root/'docs/evidence/city-c/baseline.json').read_text(encoding='utf-8'));old=d['city'];oldbs=d['buildingsData'];oldps=d['props']
C=36;W=864;H=576
xs=[18,72,126,180,234,288,342,378,432,486,540,594,648,666,702,756,792,810,846];ys=[18,72,126,144,198,252,306,324,378,432,450]
# Combined blocks are explicit before road authoring; only their boundary streets survive.
combined=[(18,324,126,432),(234,378,342,432),(378,18,486,126),(666,378,792,432),(18,18,126,72),(756,18,846,126),(810,378,864,450),(486,198,594,252)]
def inner(a,b,r):
 x0,y0,x1,y1=r;mx=(a[0]+b[0])/2;my=(a[1]+b[1])/2
 return x0<mx<x1 and y0<my<y1
roads=[]
for y in ys:
 for a,b in zip(xs,xs[1:]):
  if not any(inner((a,y),(b,y),r)for r in combined):roads.append([[a,y],[b,y]])
for x in xs:
 for a,b in zip(ys,ys[1:]):
  if not any(inner((x,a),(x,b),r)for r in combined):roads.append([[x,a],[x,b]])
# Main southern street joins Home's single neck; dead end is a named turning bulb.
roads += [[[72,432],[72,374]],[[846,450],[852,450]],[[620,126],[620,102]],[[728,72],[728,102]]]
tram=[[72,450],[378,450],[378,126],[666,126],[666,450]]
roadHalf=5;setback=2;railHalf=3

def dist(x,y,a,b):
 dx=b[0]-a[0];dy=b[1]-a[1];t=max(0,min(1,((x-a[0])*dx+(y-a[1])*dy)/(dx*dx+dy*dy)));return math.hypot(x-a[0]-t*dx,y-a[1]-t*dy)
def intersect(a,b,m=0):return a['x']<b['x']+b['w']+m and a['x']+a['w']+m>b['x'] and a['y']<b['y']+b['h']+m and a['y']+a['h']+m>b['y']
def corridor(a,z,r):return dict(x=min(a[0],z[0])-r,y=min(a[1],z[1])-r,w=abs(a[0]-z[0])+2*r,h=abs(a[1]-z[1])+2*r)
def nearroad(b,margin=7):
 # Full road rectangles plus the larger Founders Court turning bulb; tangency is allowed.
 courtDistance=math.hypot(max(b['x']-72,0,72-b['x']-b['w']),max(b['y']-374,0,374-b['y']-b['h']))
 return courtDistance<5.5+(margin-roadHalf) or any(intersect(b,corridor(a,z,margin)) for a,z in roads)
# Same tangent quarter-circles and <=.25 tile samples as riverfrontRail.ts, at tile centres.
railpoints=[]
def straight(a,z):
 n=math.ceil(math.dist(a,z)*4)
 railpoints.extend((a[0]+(z[0]-a[0])*j/n,a[1]+(z[1]-a[1])*j/n) for j in range(n+1))
points=[(x+.5,y+.5) for x,y in tram];start=points[0]
for a,b,c in zip(points,points[1:],points[2:]):
 u=((b[0]-a[0])/math.dist(a,b),(b[1]-a[1])/math.dist(a,b));v=((c[0]-b[0])/math.dist(b,c),(c[1]-b[1])/math.dist(b,c));r=12
 pre=(b[0]-u[0]*r,b[1]-u[1]*r);post=(b[0]+v[0]*r,b[1]+v[1]*r);centre=(pre[0]+v[0]*r,pre[1]+v[1]*r);turn=1 if u[0]*v[1]-u[1]*v[0]>0 else -1;angle=math.atan2(pre[1]-centre[1],pre[0]-centre[0]);n=math.ceil(math.pi*r*2)
 straight(start,pre)
 railpoints.extend((centre[0]+r*math.cos(angle+turn*math.pi/2*j/n),centre[1]+r*math.sin(angle+turn*math.pi/2*j/n)) for j in range(1,n+1));start=post
straight(start,points[-1])
railcells=set()
for x,y in railpoints:
 for yy in range(math.floor(y)-4,math.floor(y)+5):
  for xx in range(math.floor(x)-4,math.floor(x)+5):
   if math.hypot(xx+.5-x,yy+.5-y)<=railHalf+math.sqrt(.5):railcells.add((xx,yy))
def railhit(b):return any((x,y) in railcells for y in range(b['y'],b['y']+b['h']) for x in range(b['x'],b['x']+b['w']))
def door(x,y,w,h,facing):
 return [x+w-1 if facing=='E'else x,y+h//2-1] if facing in ['E','W'] else [x+w//2-1,y+h-1 if facing=='S'else y]
def outside(b):
 x,y=b['door'];return {'N':(x+1,y-1),'S':(x+1,y+1),'E':(x+1,y+1),'W':(x-1,y+1)}[b['facing']]
yards=[dict(x=83,y=350,w=30,h=29),dict(x=305,y=394,w=32,h=25),dict(x=424,y=57,w=36,h=24),dict(x=731,y=390,w=32,h=26)]
drives=[[[126,382],[117,382],[117,344]],[[300,432],[300,386]],[[378,88],[418,88],[418,84],[486,84],[418,84],[418,52],[467,52],[467,84]],[[727,432],[727,386],[767,386],[767,420],[727,420]]]
# Required content retains IDs. Interiors/furniture move together, not independently.
pos={'home-workshop':(65,347),'home-garage':(51,337),'foreman-shelter':(98,393),'court-house':(49,382),'court-garage':(87,335),'court-west-garage':(51,358),'court-east-home':(87,410),
'westridge-garden':(82,157),'westridge-rowan':(99,157),'westridge-ash':(82,178),
'oldtown-orchard':(250,210),'oldtown-courtyard':(266,210),'oldtown-mews':(250,234),
'salvage-workshop':(251,158),'concrete-shelter':(246,397),'riverside-pump':(282,407),'ironworks-hall':(394,63),'ironworks-gunsmith':(396,94),'railcrew-depot':(448,90),'civic-utility':(699,403),'lamplighter-house':(555,265),
'freight-stronghold':(56,39),'quarry-stronghold':(798,41),'wharf-stronghold':(818,420),'quarry-cache':(768,43),'wharf-cache':(818,389),'turbine-hall':(505,402),'civic-dome':(506,208)}
bs=[];moves={}
for b in oldbs:
 if b['id']not in pos:continue
 b=copy.deepcopy(b);x,y=pos[b['id']];dx=x-b['x'];dy=y-b['y'];moves[b['id']]=(dx,dy);b.update(x=x,y=y,facing='S')
 if b.get('door'):b['door']=[b['door'][0]+dx,b['door'][1]+dy]
 else:b['door']=[x+b['w']//2-1,y+b['h']-1]
 if b.get('doors'):b['doors']=[[a+dx,z+dy,w,h]for a,z,w,h in b['doors']]
 if b['id']=='westridge-ash':b['doors']=[[x+b['w']-1,y+4,1,3]]
 if b['id']=='civic-dome':b.update(kind='townhall',name='Civic Town Hall',w=30,h=26,door=[519,233])
 b['parcel']={'id':'plot:'+b['id'],'x':x-2,'y':y-2,'w':b['w']+4,'h':b['h']+4}
 b['visual']={'x':x,'y':y,'w':b['w'],'h':b['h']}
 bs.append(b)
# Deliberate Home parcels bypass ONLY generic combined-area exclusion; common validation still applies.
homeHouses=[('court-northwest-home',31,351,10,9,'E'),('court-west-home',31,374,10,9,'E'),('court-approach-home',49,404,10,9,'E'),('court-east-cottage',85,382,9,7,'W')]
# Added after filler so prior variants and garden selection retain their stable order.
# Validate the Home module's footprint conventions before propagating frontage templates.
for b in bs:
 if b['id'].startswith(('home-','court-','foreman-')):
  assert not nearroad(b) and not railhit(b),('Home reservation',b['id'])
  assert not any(intersect(b,q)for q in yards),('Home yard',b['id'])

def movedpoint(p):
 for b in oldbs:
  if b['id']in moves and b['x']<=p[0]<b['x']+b['w'] and b['y']<=p[1]<b['y']+b['h']:
   dx,dy=moves[b['id']];return[p[0]+dx,p[1]+dy]
 return None
projects={k:movedpoint(p) for k,p in old['projects'].items()};projects.update(station=[272,414],radio=[699,399],northStation=[434,116],heart=[622,242],furnace=[469,90],crown=[782,285]);projects['turbine']=[511,412]
plants=[]
for a in old['plants']:
 b=copy.deepcopy(a);b['x'],b['y']=movedpoint([a['x'],a['y']]);plants.append(b)
cores=[];artifacts=[]
for target,source in [(cores,old['cores']),(artifacts,old['artifacts'])]:
 for a in source:
  b=copy.deepcopy(a);b['x'],b['y']=movedpoint([a['x'],a['y']]);target.append(b)
recruits=[]
for name,x,y in old['recruits']:
 q=movedpoint([x,y]);assert q,name;recruits.append([name,*q])
resources=copy.deepcopy(old['resources']);resourcePos=[(59,352),(59,359),(76,359),(475,57),(785,91),(830,413),(818,91),(803,91),(273,402),(475,63),(777,411)]
for a,(x,y)in zip(resources,resourcePos):a[1:3]=[x,y]
ps=[]
for p in oldps:
 host=next((b for b in oldbs if b['id']in moves and b['x']<=p['x'] and p['x']+p['w']<=b['x']+b['w'] and b['y']<=p['y'] and p['y']+p['h']<=b['y']+b['h']),None)
 if host:
  p=copy.deepcopy(p);dx,dy=moves[host['id']];p['x']+=dx;p['y']+=dy;ps.append(p)
# A few explicit physical exterior props; all others are parcel-bound decoration.
ps += [dict(id='court-brush',x=111,y=383,w=3,h=2,kind='debris',clearable=True),dict(id='oldtown-alley',x=278,y=214,w=2,h=3,kind='debris',clearable=True),dict(id='westridge-gate',x=92,y=182,w=2,h=3,kind='gate'),dict(id='wharf-crane',x=848,y=460,w=3,h=3,kind='crane'),dict(id='freight-cargo-a',x=30,y=35,w=9,h=3,kind='container'),dict(id='freight-cargo-b',x=30,y=45,w=8,h=3,kind='container'),dict(id='freight-loader',x=100,y=48,w=5,h=4,kind='excavator'),dict(id='quarry-loader',x=782,y=78,w=6,h=4,kind='excavator'),dict(id='quarry-rocks',x=769,y=88,w=5,h=3,kind='rock')]
substations=[[78,347],[304,424],[416,94],[720,420],[115,176],[278,178],[98,59],[833,91],[834,408]]
reserved=[*yards,*[dict(x=a[1]-1,y=a[2]-1,w=a[3]+2,h=a[4]+2)for a in resources],*[dict(x=x-2,y=y-2,w=7,h=7)for x,y in projects.values()],*[dict(x=x,y=y,w=3,h=3)for x,y in substations],*ps]
reserved += [dict(x=min(a[0],z[0])-2,y=min(a[1],z[1])-2,w=abs(a[0]-z[0])+4,h=abs(a[1]-z[1])+4)for path in drives for a,z in zip(path,path[1:])]
# Public square and optional small civic greens occupy declared combined plots.
squares=[dict(x=545,y=208,w=38,h=30,name='Civic Square')];reserved+=squares
# Repeatable frontage blocks chosen by district; no rejection-scattered city generation.
def district(x,y):
 if 432<x<648 and 144<y<324:return 'centre'
 if 234<x<360 and 144<y<324 or 396<x<648 and 324<y<450:return 'oldtown'
 if y<126 and x<342:return 'industrial'
 if x>792 and y>324:return 'dock'
 return 'residential'
def add(id,kind,x,y,w,h,facing='S'):
 b=dict(id=id,name={'house':'Neighbourhood home','terrace':'Terrace row','warehouse':'Freight warehouse','shop':'High Street shop','garage':'Service garage','apartment':'Apartment block','office':'Civic offices','department':'Harper & Co','arcade':'Shopping arcade','parking':'Parking structure'}[kind],x=x,y=y,w=w,h=h,kind=kind,facing=facing,variant=len(bs)%3,door=door(x,y,w,h,facing),parcel=dict(id='plot:'+id,x=x-2,y=y-2,w=w+4,h=h+4),visual=dict(x=x,y=y,w=w,h=h))
 if nearroad(b)or railhit(b)or any(intersect(b,a,2)for a in bs+reserved):return False
 if any(r[0]<x+w and x<r[2] and r[1]<y+h and y<r[3] for r in combined):return False
 bs.append(b);return True
for iy,(y0,y1)in enumerate(zip(ys,ys[1:])):
 for ix,(x0,x1)in enumerate(zip(xs,xs[1:])):
  if x1-x0<36 or y1-y0<40:continue
  name=f'block-{ix}-{iy}';zone=district((x0+x1)/2,(y0+y1)/2)
  if zone=='centre':
   kind=['apartment','office','department','arcade','parking'][(ix+iy)%5];w,h={'apartment':(28,22),'office':(28,30),'department':(34,28),'arcade':(24,32),'parking':(32,28)}[kind];add(name,kind,x0+(x1-x0-w)//2,y1-h-7 if kind=='apartment' and y0==252 else y0+(y1-y0-h)//2,w,h)
  elif zone in ['industrial','dock']:
   add(name,'warehouse',x0+10,y0+10,24,16)
   if y1-y0>=54:add(name+'-service','garage',x0+10,y1-19,10,9)
  else:
   for row,y in enumerate([y0+9,y1-19]):
    for col,x in enumerate([x0+9,x1-20]):
     kind='shop'if zone=='oldtown' and row==1 else 'terrace'if zone=='oldtown'else'house';w=12 if kind!='house'else 10;add(name+f'-{row}-{col}',kind,x,y,w,10,'N'if row==0 else'S')
for i,(id,x,y,w,h,facing) in enumerate(homeHouses):
 bs.append(dict(id=id,name=['Court gardener’s home','Court brick home','Approach home','Court cottage'][i],x=x,y=y,w=w,h=h,kind='house',facing=facing,variant=i%3,door=door(x,y,w,h,facing),parcel=dict(id='plot:'+id,x=x-2,y=y-2,w=w+4,h=h+4),visual=dict(x=x,y=y,w=w,h=h)))
for i,yard in enumerate(yards):
 assert not nearroad(yard,roadHalf) and not railhit(yard),('Yard overlaps public pavement or swept rail',i,yard)
# One shared solid-footprint gate for retained, explicit and filler buildings; no Home exemption.
for b in bs:
 assert b['parcel']['x']<=b['visual']['x'] and b['parcel']['y']<=b['visual']['y'] and b['visual']['x']+b['visual']['w']<=b['parcel']['x']+b['parcel']['w'] and b['visual']['y']+b['visual']['h']<=b['parcel']['y']+b['parcel']['h'],('Visual bounds outside plot',b['id'])
 assert not nearroad(b) and not railhit(b),('Road/rail footprint',b['id'],b['x'],b['y'])
 for q in bs:
  if q['id']!=b['id']:assert not intersect(b,q),('Building overlap',b['id'],q['id'])
 for i,q in enumerate(yards):assert not intersect(b,q),('Yard overlap',b['id'],i)
 for item,x,y,w,h,*rest in resources:assert not intersect(b,dict(x=x,y=y,w=w,h=h)),('Resource overlap',b['id'],item,x,y)
 for path in drives:
  for a,z in zip(path,path[1:]):assert not intersect(b,corridor(a,z,2)),('Drive overlap',b['id'],a,z)
 for q in ps:
  # Existing interior furniture/obstacles are intentional and entirely contained in their host.
  contained=b['x']<=q['x'] and b['y']<=q['y'] and q['x']+q['w']<=b['x']+b['w'] and q['y']+q['h']<=b['y']+b['h']
  assert not intersect(b,q) or b.get('enterable') and contained,('Prop overlap',b['id'],q['id'])
# Parcel-bound garden trees and an explicitly solid civic monument.
for i,b in enumerate(bs):
 if b['kind']=='house' and i%2==0:
  p=dict(id=b['id']+':garden-tree',kind='tree',x=b['x']-2,y=b['y']-2,w=2,h=2)
  if not nearroad(p,5) and not railhit(p) and not any(intersect(p,a)for a in bs+reserved+ps):ps.append(p)
ps += [dict(id='civic-memorial',kind='statue',x=562,y=220,w=3,h=3),*[dict(id=f'civic-tree-{i}',kind='tree',x=x,y=y,w=2,h=2)for i,(x,y)in enumerate([(548,212),(578,212),(548,234),(578,234)])]]
# Real, unobstructed entrance paths. Connect to nearest public segment through the parcel apron.
paths=[]
occupied={}
for k in bs+ps:
 for yy in range(k['y']-1,k['y']+k['h']+1):
  for xx in range(k['x']-1,k['x']+k['w']+1):occupied.setdefault((xx,yy),set()).add(k['id'])
for b in bs:
 x,y=outside(b);candidates=[]
 if b['id']=='court-northwest-home':candidates=[(50,[[x,y],[46,y],[46,370],[72,370],[72,374]])]
 for a,z in ([] if candidates else sorted(roads,key=lambda az:dist(x,y,*az))):
  xx=max(min(x,max(a[0],z[0])),min(a[0],z[0]));yy=max(min(y,max(a[1],z[1])),min(a[1],z[1]));route=[[x,y],[xx,y],[xx,yy]]
  if yy==y:route=[[x,y],[xx,yy]]
  if xx==x:route=[[x,y],[xx,yy]]
  blocked=False
  obstacles=[q for q in bs+ps if q['id']!=b['id']]+yards+[dict(x=a[1],y=a[2],w=a[3],h=a[4])for a in resources]
  for v,q in zip(route,route[1:]):
   shape=corridor([v[0]+.5,v[1]+.5],[q[0]+.5,q[1]+.5],.7)
   if any(intersect(shape,k) for k in obstacles):blocked=True;break
  for v,q in zip(route,route[1:]):
   n=max(abs(q[0]-v[0]),abs(q[1]-v[1]))
   for j in range(n+1):
    tx=v[0]+(q[0]-v[0])*j/max(1,n);ty=v[1]+(q[1]-v[1])*j/max(1,n)
    if b['x']<=tx<b['x']+b['w'] and b['y']<=ty<b['y']+b['h'] or occupied.get((int(tx),int(ty)),set())-{b['id']}:blocked=True;break
   if blocked:break
  if not blocked:candidates.append((abs(x-xx)+abs(y-yy),route));break
 if not candidates:raise Exception(('No entrance connection',b['id']))
 b['path']=min(candidates,key=lambda v:v[0])[1]
 # End on the pavement, not as a lighter strip jutting into the carriageway.
 a,z=b['path'][-2:];length=abs(z[0]-a[0])+abs(z[1]-a[1])
 if length>3:b['path'][-1]=[z[0]-int(math.copysign(3,z[0]-a[0]))if z[0]!=a[0]else z[0],z[1]-int(math.copysign(3,z[1]-a[1]))if z[1]!=a[1]else z[1]]
 paths.append(b['path'])
# Validate the full 1.4-tile walking strip for every source of buildings before emitting terrain.
for b in bs:
 obstacles=[q for q in bs+ps if q['id']!=b['id']]+[dict(q,id='yard:'+str(i)) for i,q in enumerate(yards)]+[dict(id='resource:'+a[0],x=a[1],y=a[2],w=a[3],h=a[4]) for a in resources]
 for a,z in zip(b['path'],b['path'][1:]):
  shape=corridor([a[0]+.5,a[1]+.5],[z[0]+.5,z[1]+.5],.7)
  for q in obstacles:assert not intersect(shape,q),('Path width',b['id'],q['id'],a,z)
 # New Home paths also respect neighbours' complete garden plots, not just walls.
 if b['id'] in {a[0] for a in homeHouses}:
  for q in bs:
   if q['id']==b['id']:continue
   for a,z in zip(b['path'],b['path'][1:]):assert not intersect(corridor([a[0]+.5,a[1]+.5],[z[0]+.5,z[1]+.5],.7),q['parcel']),('Home garden path',b['id'],q['id'])
# Explicit graph nodes are shared at each crossing, including the court's tee on the south street.
nodes={}
for a,z in roads:
 for x,y in [a,z]:nodes.setdefault(f'{x},{y}',dict(id=f'{x},{y}',x=x,y=y))
# Split any segment at every authored node on it, including the cul-de-sac junction.
edges=[]
for a,z in roads:
 mid=[v for v in nodes.values()if min(a[0],z[0])<=v['x']<=max(a[0],z[0]) and min(a[1],z[1])<=v['y']<=max(a[1],z[1])];mid.sort(key=lambda v:abs(v['x']-a[0])+abs(v['y']-a[1]));edges += [[v['id'],q['id']]for v,q in zip(mid,mid[1:])]
for n in nodes.values():
 degree=sum(n['id']in e for e in edges)
 if degree==1:n['end']={'72,374':'Founders Court turnaround','852,450':'Wharf loading turnaround','620,102':'North Gardens turnaround','728,102':'East Gardens turnaround'}[n['id']]
city=dict(id='riverfront-arc-v4',version=4,width=W,height=H,cell=C,roadHalf=roadHalf,pavement=2,setback=setback,railHalf=railHalf,tramRadius=12,tramSpeed=40,tramDwell=1.5,riverY=477,homeOrigin=[58,345],court=dict(x=72,y=374,radius=5.5),homeRaidY=391,regions=[['Founders Court',72,372],['Riverside Works',306,414],['Ironworks',430,78],['Civic Utility',728,408],['Westridge Homes',126,230],['Old Town',294,240],['Northwood Freight',75,48],['Ravenholm Quarry',810,60],['East Wharf',831,432]],serviceAreas=[dict(x=432,y=144,w=216,h=180,block=3)],yards=yards,plants=plants,cores=cores,artifacts=artifacts,recruits=recruits,projects=projects,resources=resources,lights=[[64,363],[78,363],[89,360],[108,360],[89,375],[108,375],[299,424],[331,423],[415,108],[465,108],[694,425],[771,425],[539,204],[586,244],[114,191],[280,191],[109,66],[836,110]],lightKw=2,substations=substations,stops=[dict(id=a['id'],name=a['name'],x=x,y=y)for a,(x,y)in zip(old['stops'],[(72,448),(324,448),(432,124),(664,432)])],drives=drives,roads=[[[nodes[a]['x'],nodes[a]['y']],[nodes[z]['x'],nodes[z]['y']]]for a,z in edges],roadNodes=list(nodes.values()),roadEdges=edges,tram=tram,paths=paths,squares=squares)
header='''/** Riverfront v4: explicit compiled planning-grid parcels. See docs/evidence/city-c/authorFullCity.py. */
import type {CityGeom,CityBlock} from './geom';
import type {MapSpec} from '../types';
import {riverfrontRail} from './riverfrontRail';
export const RIVERFRONT_ID='riverfront-arc-v4';
export type BuildingKind='house'|'terrace'|'garage'|'workshop'|'warehouse'|'shop'|'utility'|'civic'|'townhall'|'apartment'|'office'|'department'|'arcade'|'parking';
export interface Parcel {id:string;name:string;x:number;y:number;w:number;h:number;kind:BuildingKind;facing:'N'|'S'|'E'|'W';parcel:{id:string;x:number;y:number;w:number;h:number};visual:{x:number;y:number;w:number;h:number};path:[number,number][];enterable?:boolean;door?:[number,number];doors?:[number,number,number,number][];variant?:number;compound?:string;colour?:number;content?:string;note?:string;}
export interface MapProp {id:string;x:number;y:number;w:number;h:number;kind:'tree'|'debris'|'gate'|'furniture'|'fence'|'tank'|'crane'|'container'|'converter'|'excavator'|'rock'|'statue';clearable?:boolean;}
'''
# Keep array tuple typing for consumers, without widening the immutable schema to any.
runtime=(Path(__file__).with_name('riverfront-runtime.inc')).read_text(encoding='utf-8');definition,tail=runtime.split('// RUNTIME_TAIL\n')
current=(root/'packages/sim/src/city/riverfront.ts').read_text(encoding='utf-8')
assert current[current.index('interface Definition'):current.index('export const RIVERFRONT:Definition=')]==definition and current[current.index('export function lineTiles'):]==tail,'Runtime drift: reconcile riverfront-runtime.inc with current riverfront.ts before regenerating; do not overwrite newer logic.'
text=header+definition+'export const RIVERFRONT:Definition='+json.dumps(city,ensure_ascii=False,indent=1)+';\nexport const RIVERFRONT_BUILDINGS:Parcel[]='+json.dumps(bs,ensure_ascii=False,indent=1)+';\nexport const RIVERFRONT_PROPS:MapProp[]='+json.dumps(ps,ensure_ascii=False,indent=1)+';\n'+tail
(root/'packages/sim/src/city/riverfront.ts').write_text(text,encoding='utf-8');print('Built',len(bs),'parcels',len(edges),'connected road edges')
(root/'docs/evidence/city-c/blueprint.json').write_text(json.dumps(dict(city=city,buildings=bs,props=ps),ensure_ascii=False),encoding='utf-8')
