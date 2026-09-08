from pathlib import Path
def edit(n,a,b):
 p=Path(n);s=p.read_text(encoding='utf-8-sig');assert a in s,(n,a);p.write_bytes(s.replace(a,b,1).encode('utf-8'))
edit('packages/sim/src/campaignRecruits.ts', "HELD, DARK, idxOf } from './types';", "HELD, DARK } from './types';\nimport { idxOf } from './sim';")
edit('packages/sim/src/campaignRecruits.ts','st.campaign?.truck?.boarded','st.engineer.truck')
edit('packages/sim/src/campaignRecruits.ts','recruitDescription(st: SimState,','recruitDescription(_st: SimState,')
edit('packages/sim/src/rules.ts','version: 1 | 2 | 3 | 4 | 5; truck?',"version: 1 | 2 | 3 | 4 | 5 | 6; recruits?: import('./campaignRecruits').RecruitState; truck?")
edit('packages/sim/src/rules.ts','[1,2,3,4,5].includes','[1,2,3,4,5,6].includes')
for n in ['campaign.ts','save.ts']:
 edit('packages/sim/src/'+n,"import { initDiscovery }", "import { initRecruits, recruitsProblem } from './campaignRecruits';\nimport { initDiscovery }" if n=='save.ts' else "import { initRecruits } from './campaignRecruits';\nimport { initDiscovery }")
 edit('packages/sim/src/'+n,'initDistricts(st); initDiscovery(st);','initDistricts(st); initDiscovery(st); initRecruits(st);')
edit('packages/sim/src/save.ts','|| discoveryProblem(st as SimState) ||','|| discoveryProblem(st as SimState) || recruitsProblem(st as SimState) ||')
edit('packages/sim/src/types.ts',"| { type: 'recoverSchematic'; id: string }","| { type: 'recruitSurvivors'; id: string }\n  | { type: 'recoverSchematic'; id: string }")
edit('packages/sim/src/engineer.ts',"case 'recoverSchematic':", "case 'recruitSurvivors': case 'recoverSchematic':")
edit('packages/sim/src/flow.ts',"import { discoveryAt, recoverSchematic }", "import { recruitAt, recruitSurvivors, campaignRecruited } from './campaignRecruits';\nimport { discoveryAt, recoverSchematic }")
edit('packages/sim/src/flow.ts',"export function survivorJoined(st: SimState, name: string): boolean {", "export function survivorJoined(st: SimState, name: string): boolean {\n  if(isCampaign(st) && name==='Electricians' && st.campaign?.recruits)return campaignRecruited(st,'electricians');")
edit('packages/sim/src/flow.ts',"if (kind === 'substation') return '';", "if (kind === 'substation') return '';\n    if(kind==='floodlight'||kind==='bigpole')return campaignRecruited(st,'electricians')?'':'recruit the Electricians at their shelter';")
edit('packages/sim/src/flow.ts',"if (discoveryAt(st,x,y)) return 'workshop records are here';", "if (recruitAt(st,x,y)) return 'a survivor shelter is here';\n    if (discoveryAt(st,x,y)) return 'workshop records are here';")
edit('packages/sim/src/flow.ts',"case 'recoverSchematic': recoverSchematic(st,c.id); break;", "case 'recruitSurvivors': recruitSurvivors(st,c.id); break;\n    case 'recoverSchematic': recoverSchematic(st,c.id); break;")
edit('packages/sim/src/threat.ts',"import { tickDiscovery }", "import { tickRecruits } from './campaignRecruits';\nimport { tickDiscovery }")
edit('packages/sim/src/threat.ts','tickDiscovery(st,dt); tickWeapons','tickDiscovery(st,dt); tickRecruits(st); tickWeapons')
edit('packages/sim/src/index.ts',"export * from './campaignDiscovery';", "export * from './campaignDiscovery';\nexport * from './campaignRecruits';")
edit('packages/game/src/session.ts','(original.campaign?.version ?? 0) < 5','(original.campaign?.version ?? 0) < 6')
edit('packages/game/src/worldScene.ts','persistentSource, discoveryAt,','persistentSource, recruitAt, recruitCheck, recruitDescription, discoveryAt,')
edit('packages/game/src/worldScene.ts','if(h&&discoveryAt(st,h.tx,h.ty))',"if(h){const s=recruitAt(st,h.tx,h.ty);if(s){const why=recruitCheck(st,s.id);if(why)this.hooks.onToast(why,'bad');else{queue(this.session,{type:'recruitSurvivors',id:s.id});this.hooks.onToast('Recruiting the Electricians.');}return;}}\n    if(h&&discoveryAt(st,h.tx,h.ty))")
edit('packages/game/src/worldScene.ts','if(discoveryAt(st,tx,ty))lines.unshift',"const recruit=recruitAt(st,tx,ty);if(recruit)lines.unshift(recruitDescription(st,recruit));\n    if(discoveryAt(st,tx,ty))lines.unshift")
edit('packages/game/src/worldScene.ts','const discovery=st.campaign?.discovery;',"""for(const s of st.campaign?.recruits?.sites??[]) {
      if(s.seenAt<0||s.x<tx0||s.x>tx1||s.y<ty0||s.y>ty1)continue;
      let label=this.labels[li];if(!label){label=this.add.text(0,0,'',{fontSize:'12px',color:'#c5e4ff',backgroundColor:'#0b0e1add',padding:{x:4,y:2}}).setDepth(5);this.labels.push(label);}
      label.setText(s.recruitedAt>=0?'ELECTRICIANS / RECRUITED':'ELECTRICIANS SHELTER / E').setPosition(s.x*TILE_PX,(s.y-1)*TILE_PX).setScale(1/cam.zoom).setVisible(true);li++;
      g.fillStyle(s.recruitedAt>=0?0x526c67:0x97cdff,1);g.fillRect((s.x+.1)*TILE_PX,(s.y+.1)*TILE_PX,.8*TILE_PX,.8*TILE_PX);
    }
    const discovery=st.campaign?.discovery;""")
edit('packages/game/src/panel.ts','districtGuidance, discoveryDescription, discoveryCheck','districtGuidance, recruitDescription, recruitCheck, discoveryDescription, discoveryCheck')
edit('packages/game/src/panel.ts',"const discoveryStatus = campaign ? el('p','hint') : null;", "const recruitStatus = campaign ? el('p','hint') : null;\n  const recruitButton = campaign ? el('button',undefined,'Recruit Electricians') : null;\n  const discoveryStatus = campaign ? el('p','hint') : null;")
edit('packages/game/src/panel.ts',"section.append(el('h3',undefined,'Optional discovery'),", "section.append(el('h3',undefined,'Survivor shelter'),recruitStatus!,recruitButton!);\n    recruitButton!.onclick=()=>{const s=session.state.campaign?.recruits?.sites[0];if(s)queue(session,{type:'recruitSurvivors',id:s.id});};\n    section.append(el('h3',undefined,'Optional discovery'),")
edit('packages/game/src/panel.ts','if(discoveryStatus&&discoveryButton){',"if(recruitStatus&&recruitButton){const r=s.campaign?.recruits?.sites[0];recruitStatus.textContent=r&&r.seenAt>=0?recruitDescription(s,r):'Explore beyond the home court to find a survivor shelter.';recruitButton.hidden=!r||r.seenAt<0;recruitButton.disabled=!r||!!recruitCheck(s,r.id);recruitButton.title=r?recruitCheck(s,r.id):'';recruitButton.textContent=r&&r.recruitedAt>=0?'Electricians recruited':'Recruit Electricians';}\n    if(discoveryStatus&&discoveryButton){")
# Old preview fixtures explicitly remove all later metadata, as a real old save does.
for p in Path('packages/sim/test').glob('*.test.ts'):
 s=p.read_text();old=s
 import re
 s=re.sub(r'(st\.campaign!\.version=[1-4];)', r'delete st.campaign!.recruits;\1',s)
 s=s.replace('campaign!.version,5','campaign!.version,6')
 if s!=old:p.write_bytes(s.encode())
