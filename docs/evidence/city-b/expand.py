from pathlib import Path
import json
p=Path('packages/sim/src/city/riverfront.ts');old=p.read_text(encoding='utf-8');tail=old[old.index('export function lineTiles'):]
d=json.loads(Path('docs/evidence/city-b/baseline-map.json').read_text());c=d['city'];bs=d['buildings'];ps=d['props']
def delta(x,y):return (0 if x<85 else 70 if x<145 else 100 if x<235 else 170,20 if y<96 else 65)
def point(p):dx,dy=delta(*p);return [p[0]+dx,p[1]+dy]
def obj(b):dx,dy=delta(b['x'],b['y']);b['x']+=dx;b['y']+=dy
c.update(id='riverfront-arc-v2',version=2,width=530,height=300,riverY=261,court={'x':37,'y':198,'radius':5.5},homeRaidY=218)
c['homeOrigin']=point(c['homeOrigin'])
for key in ['lights','substations','tram']:c[key]=[point(v) for v in c[key]]
c['regions']=[[n,*point([x,y])] for n,x,y in c['regions']]
for key in ['yards','plants','stops','cores','artifacts']:
 for b in c[key]:obj(b)
c['roads']=[[point(v) for v in path] for path in c['roads']]
c['resources']=[[item,*point([x,y]),w,h,n]for item,x,y,w,h,n in c['resources']]
c['recruits']=[[n,*point([x,y])]for n,x,y in c['recruits']]
c['projects']={k:point(v) for k,v in c['projects'].items()}
for b in bs:
 dx,dy=delta(b['x'],b['y']);obj(b)
 if 'door'in b:b['door']=[b['door'][0]+dx,b['door'][1]+dy]
 if b['id']=='court-garage':b['kind']='garage'
 if b['id'] in ['civic-homes','civic-terrace']:b['kind']='terrace'
 b['variant']=sum(map(ord,b['id']))%3
for q in ps:obj(q)
# Match the shorter court to the workshop and a modest residential frontage.
c['roads'][1]=[[37,220],[37,198]]
bs.extend([{'id':'court-east-home','name':'Court cottage','x':41,'y':205,'w':9,'h':8,'kind':'house','door':[44,212],'variant':1},{'id':'court-west-garage','name':'Maintenance garage','x':16,'y':188,'w':10,'h':7,'kind':'garage','door':[19,194],'variant':2}])
# Author new street rows in the inserted neighbourhoods, not random filler.
newroads=[[[77,210],[107,210],[132,210],[152,225]],[[83,187],[106,185],[132,187],[157,190]],[[59,130],[87,141],[118,141],[158,149]],[[180,114],[204,135],[222,151],[245,158]],[[246,113],[270,129],[292,143]],[[310,156],[344,157],[376,158],[407,177]],[[411,145],[447,145],[469,156]],[[402,233],[432,240],[463,247]],[[169,49],[191,45],[213,45],[242,50]],[[354,54],[380,57],[410,57],[438,68]],[[56,75],[75,79],[99,82]]]
c['roads']+=newroads
specs=[('row','house',[(86,197),(100,197),(115,197),(129,197),(91,174),(108,173),(126,174)]),('westridge','house',[(64,115),(80,123),(96,124),(114,124),(131,128)]),('market','shop',[(176,124),(194,145),(211,154),(237,141)]),('oldtown','terrace',[(250,115),(272,115),(294,129),(306,170),(329,170),(352,171)]),('civic','shop',[(407,131),(427,131),(447,131)]),('riverside','workshop',[(146,240),(170,242),(200,241)]),('iron','warehouse',[(181,28),(209,26),(350,32),(379,32),(406,33)]),('dock','warehouse',[(405,221),(435,221),(455,233)]),('freight','garage',[(63,61),(83,63)]),('mid','garage',[(312,142),(338,140),(361,141)])]
for prefix,kind,points in specs:
 for i,(x,y) in enumerate(points):
  w,h={'house':(10,9),'shop':(12,9),'terrace':(18,10),'workshop':(16,11),'warehouse':(20,14),'garage':(10,7)}[kind];bs.append(dict(id=f'{prefix}-frontage-{i}',name={'house':'Detached home','shop':'Neighbourhood shop','terrace':'Terrace homes','workshop':'Waterside workshop','warehouse':'Freight warehouse','garage':'Service garage'}[kind],x=x,y=y,w=w,h=h,kind=kind,door=[x+w//2-1,y+h-1],variant=i%3))
# Civic square is a real public paved parcel, apart from the utility factory.
c['squares']=[{'x':417,'y':156,'w':25,'h':17,'name':'Civic Square'}]
for ident in ['freight-stronghold','quarry-stronghold','wharf-stronghold']:
 b=next(b for b in bs if b['id']==ident);b['w']=30;b['h']=23;b['door']=[b['x']+9,b['y']+22];b['doors']=[[b['x'],b['y']+10,1,3]];b['compound']=b['content'];
 # Cargo, cover and conversion pylons leave both doors and core approaches open.
 for j,(dx,dy,w,h,kind)in enumerate([(3,3,6,3,'container'),(20,3,6,3,'container'),(22,14,3,3,'converter'),(3,16,4,2,'debris')]):ps.append(dict(id=ident+f':prop{j}',x=b['x']+dx,y=b['y']+dy,w=w,h=h,kind=kind))
# External cargo aprons and processing equipment establish compound identity.
for ident,x,y,w,h,kind in [('freight-cargo-a',15,66,9,3,'container'),('freight-cargo-b',48,67,8,3,'container'),('freight-loader',61,39,6,4,'excavator'),('quarry-processor',503,67,9,4,'container'),('quarry-excavator',478,65,7,4,'excavator'),('wharf-cargo-a',491,215,9,3,'container'),('wharf-cargo-b',506,215,9,3,'container'),('quarry-boulder',464,28,4,3,'rock')]:ps.append(dict(id=ident,x=x,y=y,w=w,h=h,kind=kind))
# Primary entrance paths are explicit, walkable paving; background shutters stay blocked.
c['paths']=[]
for b in bs:
 door=b.get('door',[b['x']+2,b['y']+b['h']-1]);c['paths'].append([[door[0]+1,door[1]+1],[door[0]+1,door[1]+4]])
head='''/** Riverfront Arc v2. Explicit authored positions; no runtime random placement. */
import type {CityGeom,CityBlock} from './geom';
import type {MapSpec} from '../types';
export const RIVERFRONT_ID='riverfront-arc-v2';
export type BuildingKind='house'|'terrace'|'garage'|'workshop'|'warehouse'|'shop'|'utility'|'civic';
export interface Parcel {id:string;name:string;x:number;y:number;w:number;h:number;kind:BuildingKind;enterable?:boolean;door?:[number,number];doors?:[number,number,number,number][];variant?:number;compound?:string;colour?:number;content?:string;note?:string;}
export interface MapProp {id:string;x:number;y:number;w:number;h:number;kind:'tree'|'debris'|'gate'|'furniture'|'fence'|'tank'|'crane'|'container'|'converter'|'excavator'|'rock';clearable?:boolean;}
interface Definition {id:string;version:number;tramSpeed:number;tramDwell:number;width:number;height:number;riverY:number;court:{x:number;y:number;radius:number};homeRaidY:number;homeOrigin:[number,number];lightKw:number;lights:[number,number][];substations:[number,number][];regions:[string,number,number][];yards:{x:number;y:number;w:number;h:number}[];plants:{id:string;name:string;x:number;y:number}[];stops:{id:string;name:string;x:number;y:number}[];tram:[number,number][];roads:[number,number][][];paths:[number,number][][];squares:{x:number;y:number;w:number;h:number;name:string}[];resources:[string,number,number,number,number,number][];cores:{id:string;name:string;x:number;y:number}[];artifacts:{id:string;name:string;x:number;y:number}[];recruits:[string,number,number][];projects:Record<string,[number,number]>;}
'''
tail=tail.replace('bank=196+','bank=RIVERFRONT.riverY+').replace('for(let y=126;y<=142;y++)for(let x=29;x<=45;x++)if(Math.hypot(x-37,y-134)<=8)', 'for(let y=192;y<=204;y++)for(let x=31;x<=43;x++)if(Math.hypot(x-RIVERFRONT.court.x,y-RIVERFRONT.court.y)<=RIVERFRONT.court.radius)').replace('sqy:id===0?110','sqy:id===0?175').replace('start:[43,132],target:[115,150]','start:[43,197],target:[185,215]')
tail=tail.replace('for(let t=0;t<n;t++)if(owner[t]>=0)',"for(const path of RIVERFRONT.paths)for(const t of lineTiles(path))if(kind[t]!==2){kind[t]=1;owner[t]=-1;}\n for(let t=0;t<n;t++)if(owner[t]>=0)")
p.write_text(head+'export const RIVERFRONT:Definition='+json.dumps(c,ensure_ascii=False,indent=1)+';\nexport const RIVERFRONT_BUILDINGS:Parcel[]='+json.dumps(bs,ensure_ascii=False,indent=1)+';\nexport const RIVERFRONT_PROPS:MapProp[]='+json.dumps(ps,ensure_ascii=False,indent=1)+';\n'+tail,encoding='utf-8')
Path('docs/evidence/city-b/plan.json').write_text(json.dumps({'baseline':{'width':360,'height':230,'buildings':46},'target':{'width':530,'height':300,'buildings':len(bs)},'routeTargets':'Home resources unchanged; Riverside 20-30s; later districts 45-90s uninterrupted walking, preserved tram freight'},indent=2))
