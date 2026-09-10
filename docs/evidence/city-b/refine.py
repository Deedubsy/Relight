from pathlib import Path
import json
p=Path('packages/sim/src/city/riverfront.ts');s=p.read_text(encoding='utf-8');h=s[:s.index('export const RIVERFRONT:Definition=')];c=json.loads(s.split('export const RIVERFRONT:Definition=')[1].split(';\nexport const RIVERFRONT_BUILDINGS')[0]);bs=json.loads(s.split('export const RIVERFRONT_BUILDINGS:Parcel[]=')[1].split(';\nexport const RIVERFRONT_PROPS')[0]);ps=json.loads(s.split('export const RIVERFRONT_PROPS:MapProp[]=')[1].split(';\nexport function lineTiles')[0]);tail=s[s.index('export function lineTiles'):]
fix={'court-east-home':(49,211),'oldtown-frontage-0':(218,129),'oldtown-frontage-1':(270,152),'oldtown-frontage-2':(286,170),'oldtown-frontage-4':(330,185),'oldtown-frontage-5':(355,196),'mid-frontage-0':(300,145),'market-frontage-0':(170,136),'civic-frontage-0':(383,133),'dock-frontage-0':(405,205),'dock-frontage-2':(445,248),'freight-frontage-0':(63,42),'freight-frontage-1':(83,45)}
for b in bs:
 if b['id']in fix:
  x,y=fix[b['id']];dx=x-b['x'];dy=y-b['y'];b.update(x=x,y=y)
  if'door'in b:b['door']=[b['door'][0]+dx,b['door'][1]+dy]
 if b['id']=='dock-frontage-2':b['h']=10;b['door'][1]=b['y']+9
c['roads'][-1]=[[57,86],[80,89],[106,95]];c['roads'][9]=[[475,216],[485,225],[485,255],[525,255]];c['roads'][0][-2:]=[[527,222],[527,255]]
c['projects']['turbine']=[392,238];bs.append(dict(id='turbine-hall',name='Waterside turbine hall',x=386,y=228,w=14,h=16,kind='utility',enterable=True,door=[390,243],variant=1,content='turbine'))
c['paths']=[[[b.get('door',[b['x']+2,b['y']+b['h']-1])[0]+1,b.get('door',[b['x']+2,b['y']+b['h']-1])[1]+1],[b.get('door',[b['x']+2,b['y']+b['h']-1])[0]+1,b.get('door',[b['x']+2,b['y']+b['h']-1])[1]+4]]for b in bs]
p.write_text(h+'export const RIVERFRONT:Definition='+json.dumps(c,ensure_ascii=False,indent=1)+';\nexport const RIVERFRONT_BUILDINGS:Parcel[]='+json.dumps(bs,ensure_ascii=False,indent=1)+';\nexport const RIVERFRONT_PROPS:MapProp[]='+json.dumps(ps,ensure_ascii=False,indent=1)+';\n'+tail,encoding='utf-8')
