import {navigationTargets,NAVIGATION_RULES,type NavigationAction,type NavigationTarget} from '@relight/sim';
import {dispatch,type Session} from './session';
import {navigationView} from './view';
const el=<K extends keyof HTMLElementTagNameMap>(tag:K,text='')=>{const n=document.createElement(tag);n.textContent=text;return n;};
/** Ordinary sim commands for annotations; locate and target selection never move the engineer. */
export function createNavigationPanel(session:Session,root:HTMLElement,locate:(t:NavigationTarget)=>void,toggleMap:()=>void,returnToEngineer:()=>void,captureInput:()=>void){
 const section=el('details');section.className='campaign-guide navigation-panel';section.append(el('summary','Map and navigation'));
 const select=el('select');select.setAttribute('aria-label','Navigation destination');
 const name=el('input');name.maxLength=NAVIGATION_RULES.nameLength;name.setAttribute('aria-label','Destination name');name.placeholder='Choose a name';
 const status=el('p'),feedback=el('p');feedback.setAttribute('role','status');
 const actions=el('div');actions.className='navigation-actions';
 const button=(label:string,fn:()=>void)=>{const b=el('button',label);b.type='button';b.onclick=fn;actions.append(b);return b;};
 const send=(action:NavigationAction)=>{const result=dispatch(session,{type:'navigation',action});feedback.textContent=result.reason;update(true);};
 const target=()=>navigationTargets(session.state).find(t=>t.id===navigationView.targetId);
 button('Map / world',toggleMap);
 const show=button('Show destination',()=>{const t=target();if(t)locate(t);});button('Return to engineer',returnToEngineer);
 const rename=button('Save name',()=>{if(navigationView.targetId)send({type:'rename',id:navigationView.targetId,name:name.value.trim()});});
 const reset=button('Restore default name',()=>{if(navigationView.targetId)send({type:'rename',id:navigationView.targetId,name:''});});
 button('Pin my position',()=>{const e=session.state.engineer;send({type:'pin',x:Math.floor(e.x),y:Math.floor(e.y),name:''});const pin=session.state.campaign?.navigation?.pins.at(-1);if(pin)navigationView.targetId=`pin:${pin.id}`;update(true);});
 const remove=button('Remove pin',()=>{const t=target();if(t?.kind==='pin'){send({type:'removePin',id:Number(t.id.slice(4))});navigationView.targetId=null;update(true);}});
 button('Clear destination',()=>{navigationView.targetId=null;update(true);});
 section.append(el('p','Select a known place or visited district. Shift-click the map to add a pin in a known district; click its marker to select it. Show destination only moves the camera.'),select,status,name,actions,feedback);
 root.insertBefore(section,root.querySelector('.campaign-guide'));
 section.addEventListener('focusin',captureInput);
 section.addEventListener('pointerdown',captureInput);
 // Phaser sees the same document keys; reset held input when the panel captures focus.
 for(const event of ['pointerdown','pointerup','click','wheel','contextmenu'])section.addEventListener(event,e=>e.stopPropagation());
 section.addEventListener('keydown',e=>{e.stopPropagation();if(e.key==='Escape'){section.open=false;document.querySelector('canvas')?.focus();}else if(e.key==='Enter'&&e.target===name){e.preventDefault();rename.click();}});
 select.onchange=()=>{navigationView.targetId=select.value||null;update(true);};
 let key='',chosen='';
 function update(force=false){const rows=navigationTargets(session.state),next=JSON.stringify(rows.map(t=>[t.id,t.name,t.visited]));
  if(next!==key){key=next;select.replaceChildren(el('option','No destination'));(select.firstChild as HTMLOptionElement).value='';for(const t of rows){const o=el('option',`${t.name} · ${t.visited?'Visited':'Known'} · ${t.kind==='pin'?t.id:t.kind}`);o.value=t.id;select.append(o);}}
  const t=rows.find(t=>t.id===navigationView.targetId);if(!t)navigationView.targetId=null;select.value=t?.id??'';
  if(force||chosen!==select.value){chosen=select.value;name.value=t?.name??'';}
  status.textContent=t?`${t.visited?'Visited':'Known — no visit recorded'} · Default: ${t.defaultName} · ${t.id}`:'No destination selected.';
  show.disabled=rename.disabled=reset.disabled=name.disabled=!t;remove.hidden=t?.kind!=='pin';
 }
 return {update,open(){section.open=true;update(true);section.scrollIntoView({block:'nearest'});select.focus();}};
}
