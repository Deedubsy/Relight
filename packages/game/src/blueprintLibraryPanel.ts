import { blueprintGate, blueprintOrderInfo, exportBlueprintEntry, KIND_LABEL, BLUEPRINT_TEXT_LIMIT, type BlueprintLibraryAction, type BlueprintOrderAction, type Kind } from '@relight/sim';
import { dispatch, type Session } from './session';
const node=<K extends keyof HTMLElementTagNameMap>(tag:K,text='')=>{const n=document.createElement(tag);n.textContent=text;return n;};
/** Controls retain focus and text while the simulation advances. */
export function createBlueprintLibraryPanel(session:Session,root:HTMLElement,toast:(s:string,k:'good'|'bad')=>void){
 const section=node('details');section.className='campaign-guide';section.open=true;section.setAttribute('aria-label','Blueprint library and orders');section.append(node('summary','Blueprint library & orders'));
 const status=node('p'),name=node('input'),folder=node('input'),icon=node('select'),filter=node('select'),entries=node('select'),text=node('textarea'),orders=node('div');
 const label=(title:string,input:HTMLElement)=>{const l=node('label',title);l.style.display='block';input.setAttribute('aria-label',title);input.style.cssText='display:block;width:100%;box-sizing:border-box;min-width:0';l.append(input);section.append(l);};
 name.maxLength=120;name.value='My layout';folder.maxLength=64;text.maxLength=BLUEPRINT_TEXT_LIMIT;text.rows=5;
 for(const [k,v] of Object.entries(KIND_LABEL))if(k!=='depot'){const o=node('option',v);o.value=k;icon.append(o);}icon.value='belt';
 section.append(status);label('Blueprint name',name);label('Blueprint folder',folder);label('Blueprint icon',icon);
 const send=(action:BlueprintLibraryAction)=>{const r=dispatch(session,{type:'blueprintLibrary',action});toast(r.reason,r.ok?'good':'bad');if(r.ok&&(action.type==='save'||action.type==='import')){filter.value='*';libraryKey='';update();entries.value=String(session.state.campaign!.plans!.library.at(-1)!.id);select();}else{update();if(r.ok&&action.type==='load')select();}};
 const order=(action:BlueprintOrderAction)=>{const r=dispatch(session,{type:'blueprintOrder',action});toast(r.reason,r.ok?'good':'bad');update();};
 const metadata=()=>({name:name.value,folder:folder.value,icon:icon.value as Exclude<Kind,'depot'>});
 const row=node('div');row.className='row';row.style.flexWrap='wrap';const gated:HTMLButtonElement[]=[];
 const button=(parent:HTMLElement,title:string,fn:()=>void)=>{const b=node('button',title);b.onclick=fn;parent.append(b);gated.push(b);return b;};
 button(row,'Save clipboard',()=>send({type:'save',...metadata()}));section.append(row);
 label('Filter blueprint folder',filter);label('Saved blueprints',entries);
 const select=()=>{const e=session.state.campaign?.plans?.library.find(e=>e.id===Number(entries.value));if(e){name.value=e.name;folder.value=e.folder;icon.value=e.icon;}};entries.onchange=select;
 const actions=node('div');actions.className='row';actions.style.flexWrap='wrap';
 const selectedButtons=[button(actions,'Load clipboard',()=>send({type:'load',id:Number(entries.value)})),button(actions,'Update name / folder / icon',()=>send({type:'update',id:Number(entries.value),...metadata()})),button(actions,'Delete library entry',()=>send({type:'remove',id:Number(entries.value)})),button(actions,'Export text',()=>{try{text.value=exportBlueprintEntry(session.state,Number(entries.value));text.focus();text.select();}catch(e){toast((e as Error).message,'bad');}})];
 section.append(actions);label('Blueprint import/export text',text);button(section,'Import text',()=>send({type:'import',text:text.value}));
 section.append(node('p','Export text to share a layout. Imports contain settings only. Use Queue ghosts in the clipboard to plan without materials. Paused orders reserve their area and skip automatic work until resumed. Waiting orders can be built from your pockets; cancelling leaves any completed machines in place.'),node('h4','Construction orders'),orders);root.append(section);
 let libraryKey='',orderKey='';filter.onchange=()=>{libraryKey='';update();select();};
 function update(){const st=session.state,p=st.campaign?.plans,why=blueprintGate(st);status.textContent=why||`${p?.library.length??0}/32 saved layouts · ${p?.orders.length??0}/32 order records`;
  for(const b of gated)b.disabled=!!why;
  const key=JSON.stringify([p?.library,filter.value]);if(key!==libraryKey){libraryKey=key;const oldFilter=filter.value,oldEntry=entries.value;filter.replaceChildren();const all=node('option','All folders');all.value='*';filter.append(all);
   for(const f of [...new Set((p?.library??[]).map(e=>e.folder))].sort()){const o=node('option',f||'Unfiled');o.value='folder:'+f;filter.append(o);}filter.value=Array.from(filter.options).some(o=>o.value===oldFilter)?oldFilter:'*';entries.replaceChildren();
   for(const e of p?.library??[])if(filter.value==='*'||filter.value==='folder:'+e.folder){const o=node('option',`[${KIND_LABEL[e.icon]}] ${e.folder||'Unfiled'} / ${e.name} (#${e.id})`);o.value=String(e.id);entries.append(o);}if(Array.from(entries.options).some(o=>o.value===oldEntry))entries.value=oldEntry;if(entries.value!==oldEntry)select();
  }
  for(const b of selectedButtons)b.disabled=!!why||!entries.value;
  const rows=(p?.orders??[]).map(o=>({o,info:o.status==='waiting'?blueprintOrderInfo(st,o):null})),next=JSON.stringify([rows,why]);if(next===orderKey)return;orderKey=next;orders.replaceChildren();
  if(!rows.length)orders.append(node('p','No queued plans.'));
  for(const {o,info} of rows){const card=node('div');card.style.cssText='border:1px solid #3c6478;padding:8px;margin:8px 0';card.append(node('strong',`#${o.id} ${o.blueprint.name} · ${o.paused?'paused':o.status}`));
   card.append(node('p',`At ${o.x}, ${o.y}`+(info?` · ${info.built}/${o.blueprint.entities.length} built. Missing from pockets: ${Object.entries(info.missing).map(([k,n])=>`${n} ${k}`).join(', ')||'none'}. ${info.blockers.join('; ')||'Ready for local construction.'}`:'')));
   const b=node('button',o.status==='waiting'?'Build remaining':'Remove record');b.disabled=!!why||!!o.paused;b.onclick=()=>order({type:o.status==='waiting'?'build':'remove',id:o.id});card.append(b);
   if(o.status==='waiting'){const control=node('button',o.paused?'Resume order':'Pause order');control.disabled=!!why;control.onclick=()=>order({type:o.paused?'resume':'pause',id:o.id});card.append(control);const cancel=node('button','Cancel order');cancel.disabled=!!why;cancel.onclick=()=>order({type:'cancel',id:o.id});card.append(cancel);}orders.append(card);
  }
 }
 return {update};
}
