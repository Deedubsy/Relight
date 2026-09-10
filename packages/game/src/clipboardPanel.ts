import {shortcut,type Binding} from './controls';
import { blueprintGate, blueprintBounds, dimensions, MACHINE_SIZE, MACHINE_COST, KIND_LABEL, type RemovalPreview, type BlueprintTransform } from '@relight/sim';
import type { Session } from './session';
export type ClipboardAction = 'copy'|'paste'|'queue'|'cancel'|'removeArea'|'applyRemoval'|BlueprintTransform;
const node=<K extends keyof HTMLElementTagNameMap>(tag:K,text='')=>{const n=document.createElement(tag);n.textContent=text;return n;};
/** Stable buttons and a read-only layout thumbnail; sim commands own the saved clipboard. */
export function createClipboardPanel(session:Session,root:HTMLElement,act:(a:ClipboardAction)=>void,removal:()=>RemovalPreview|null){
  const section=node('details');section.className='campaign-guide';section.open=true;section.setAttribute('aria-label','Blueprint clipboard');
  const title=node('summary','Blueprint clipboard'),status=node('p'),tools=node('div');tools.className='row';tools.style.flexWrap='wrap';
  const buttons=new Map<ClipboardAction,HTMLButtonElement>();
  for(const [a,label] of [['copy','Copy area (Ctrl+C)'],['paste','Paste (Ctrl+V)'],['queue','Queue ghosts'],['rotate','Rotate (R)'],['mirrorX','Mirror H'],['mirrorY','Mirror V'],['removeArea','Select removal area'],['applyRemoval','Pack selected machines'],['cancel','Cancel preview']] as const){
    const b=node('button',label);const names:Record<string,string>={copy:'Copy area',paste:'Paste',rotate:'Rotate',mirrorX:'Mirror horizontally',mirrorY:'Mirror vertically'};if(names[a]){const refresh=()=>{b.textContent=`${names[a]} (${a==='copy'||a==='paste'?'Ctrl+':''}${shortcut(a as Binding)})`;};window.addEventListener('relight:preferences',refresh);refresh();}b.onclick=()=>act(a);buttons.set(a,b);tools.append(b);
  }
  const removalStatus=node('p');removalStatus.setAttribute('aria-label','Removal preview');
  const preview=node('div'),facts=node('p');facts.className='hint';
  section.append(title,status,tools,removalStatus,preview,facts,node('p','Drag around whole machines to copy. A click pastes the entire layout using pockets and normal prices. Use the labelled Rotate and Mirror controls while previewing. Escape cancels. Undo and current shortcuts are listed in Help. Removal: drag a whole-machine area, review the returns, then Pack selected machines. Up to 128 machines in a 128 × 128 area; one refusal means none are packed. Current reach and pockets apply. Undo rebuilds settings from pockets; recovered contents stay in pockets. Fixed facilities and the core stay.'));root.append(section);
  let key='';
  return {update(){const st=session.state,bp=st.campaign?.clipboard,why=blueprintGate(st);
    const r=removal();removalStatus.textContent=r?`REMOVAL PREVIEW · ${r.reason} Returns at preview: ${Object.entries(r.items).map(([k,n])=>`${n} ${k}`).join(', ')||'none'}${r.looseRounds?`; ${r.looseRounds} loose rounds to buffer`:''}. Rechecked when packed.`:'No removal selected.';
    for(const [a,b] of buttons)b.disabled=a==='cancel'?false:!!why||(a==='applyRemoval'?!r?.ok:!['copy','removeArea'].includes(a)&&!bp);
    status.textContent=why||(!bp?'Clipboard empty. Copy a nearby layout.':`${bp.name} · ${bp.entities.length} machines · saved with this campaign`);
    const next=JSON.stringify(bp);if(next===key)return;key=next;preview.replaceChildren();facts.textContent='';if(!bp)return;
    const bounds=blueprintBounds(bp),svg=document.createElementNS('http://www.w3.org/2000/svg','svg');
    svg.setAttribute('viewBox',`${bounds.x-1} ${bounds.y-1} ${bounds.w+2} ${bounds.h+2}`);svg.setAttribute('role','img');svg.setAttribute('aria-label',`Clipboard layout, ${bounds.w} by ${bounds.h} tiles`);svg.style.cssText='display:block;width:100%;height:120px;background:#111827';
    const counts=new Map<string,number>(),cost:Record<string,number>={};
    for(const e of bp.entities){const [w,h]=dimensions(e.kind,e.dir,MACHINE_SIZE[e.kind]);counts.set(e.kind,(counts.get(e.kind)??0)+1);
      for(const [item,n] of Object.entries(MACHINE_COST[e.kind]))cost[item]=(cost[item]??0)+n;
      const rect=document.createElementNS(svg.namespaceURI,'rect');for(const [k,v] of Object.entries({x:e.x,y:e.y,width:w,height:h,fill:'#245a70',stroke:'#93ddff','stroke-width':.08}))rect.setAttribute(k,String(v));
      const tip=document.createElementNS(svg.namespaceURI,'title');tip.textContent=`${KIND_LABEL[e.kind]} · ${e.recipe??e.filter??e.priority??''}`;rect.append(tip);svg.append(rect);
      const arrow=document.createElementNS(svg.namespaceURI,'path');arrow.setAttribute('d','M -.2 .2 L 0 -.2 L .2 .2 M 0 -.2 L 0 .3');arrow.setAttribute('transform',`translate(${e.x+w/2} ${e.y+h/2}) rotate(${e.dir*90})`);arrow.setAttribute('fill','none');arrow.setAttribute('stroke','#fff');arrow.setAttribute('stroke-width','.07');svg.append(arrow);
    }
    preview.append(svg);facts.textContent=[...counts].map(([k,n])=>`${n} ${k}`).join(' · ')+'. Raw material price: '+Object.entries(cost).filter(([,n])=>n).map(([k,n])=>`${n} ${k}`).join(' + ')+'. Carried machines replace their material price. Contents are never copied.';
  }};
}
