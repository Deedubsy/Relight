import {truckWorkGate,truckWorkDescription,TRUCK_RULES,TRUCK_WORK_RULES,KIND_LABEL,invStacks,type TruckWorkAction} from '@relight/sim';
import {dispatch,type Session} from './session';
const node=<K extends keyof HTMLElementTagNameMap>(tag:K,text='')=>{const n=document.createElement(tag);n.textContent=text;return n;};
export function createTruckWorkPanel(session:Session,root:HTMLElement,toast:(s:string,k:'good'|'bad')=>void){
 const section=node('details');section.className='campaign-guide';section.open=true;section.setAttribute('aria-label','Truck construction');section.append(node('summary','Truck construction'));
 const status=node('p'),cargo=node('p'),source=node('select'),orders=node('select'),buttons=node('div');buttons.className='row';buttons.style.flexWrap='wrap';
 const label=(text:string,input:HTMLElement)=>{const l=node('label',text);l.style.display='block';input.setAttribute('aria-label',text);input.style.cssText='display:block;width:100%;min-width:0;box-sizing:border-box';l.append(input);section.append(l);};
 section.append(status,cargo);label('Truck supply chest',source);label('Truck construction orders',orders);
 const send=(action:TruckWorkAction)=>{const r=dispatch(session,{type:'truckWork',action});toast(r.reason,r.ok?'good':'bad');update();};
 const button=(text:string,fn:()=>void)=>{const b=node('button',text);b.onclick=fn;buttons.append(b);return b;};
 const packed=node('select');for(const [k,v] of Object.entries(KIND_LABEL))if(k!=='depot'){const o=node('option',v);o.value=k;packed.append(o);}packed.value='belt';label('Packed supply machine',packed);
 const transfer=(put:boolean)=>{const chest=session.state.flow?.machines.find(m=>m.id===Number(source.value)&&m.kind==='chest');if(!chest)return;const r=dispatch(session,{type:'factory',action:{type:put?'chestPut':'chestTake',item:packed.value,n:1,x:chest.x,y:chest.y}});toast(r.reason,r.ok?'good':'bad');update();};
 button('Put 1 packed machine',()=>transfer(true));button('Take 1 packed machine',()=>transfer(false));
 const recovery=node('select');recovery.multiple=true;recovery.size=4;label('Equipment to recover',recovery);button('Recover selected equipment to chest',()=>send({type:'recover',sourceId:Number(source.value),ids:Array.from(recovery.selectedOptions).map(o=>Number(o.value))}));
 const start=button('Start truck delivery',()=>send({type:'start',sourceId:Number(source.value),orderIds:orders.value==='all'?(session.state.campaign?.plans?.orders??[]).filter(o=>o.status==='waiting').map(o=>o.id):[Number(orders.value)]}));
 const changeSource=button('Use selected supply chest',()=>send({type:'source',sourceId:Number(source.value)})),retry=button('Retry truck now',()=>send({type:'retry'}));
 const pause=button('Pause truck',()=>send({type:'pause'})),resume=button('Resume truck',()=>send({type:'resume'})),stop=button('Stop truck job',()=>send({type:'stop'}));
 section.append(buttons,node('p',`Select a physical supply chest and waiting plans. Loading occurs within ${TRUCK_WORK_RULES.loadReach} tiles of the chest; building uses cargo within ${TRUCK_RULES.serviceReach} tiles of the truck. The truck stays on streets. Packed machine transfers use your pockets and require standing near the selected chest. Shortages or obstructions retry every ${TRUCK_WORK_RULES.retryTicks/20} seconds. Pause or resume individual plans in Construction orders. Use selected supply chest changes the source of the assigned job and retains cargo. Retry truck now rechecks a waiting job immediately. Repeated Start does not restart an identical job. Boarding pauses automatic work; cargo and built parts remain.`));root.append(section);
 let listKey='';function update(){const st=session.state,t=st.campaign?.truck,w=t?.work,why=truckWorkGate(st);status.textContent=(why?why+'. ':'')+truckWorkDescription(st);cargo.textContent=t?`Cargo ${invStacks(t.cargo)}/${TRUCK_RULES.stacks} stacks: ${Object.entries(t.cargo).filter(([,n])=>n>0).map(([k,n])=>`${n} ${k}`).join(', ')||'empty'}`:'Truck unavailable';
  const recoveryKey=st.flow?.machines.map(m=>m.id).join(',');if(recovery.dataset.key!==recoveryKey){const chosen=Array.from(recovery.selectedOptions).map(o=>o.value);recovery.replaceChildren();for(const m of st.flow?.machines??[])if(!['depot','tram','tramstop','track'].includes(m.kind)){const o=node('option',`${KIND_LABEL[m.kind]} #${m.id} · ${m.x},${m.y}`);o.value=String(m.id);o.selected=chosen.includes(o.value);recovery.append(o);}recovery.dataset.key=recoveryKey;}
  const chests=(st.flow?.machines??[]).filter(m=>m.kind==='chest'),waiting=(st.campaign?.plans?.orders??[]).filter(o=>o.status==='waiting'),key=JSON.stringify([chests.map(m=>[m.id,m.x,m.y]),waiting.map(o=>[o.id,o.blueprint.name])]);
  if(key!==listKey){listKey=key;const selected=source.value,work=orders.value;source.replaceChildren();for(const m of chests){const o=node('option',`Chest #${m.id} at ${m.x}, ${m.y}`);o.value=String(m.id);source.append(o);}if(Array.from(source.options).some(o=>o.value===selected))source.value=selected;else if(w&&chests.some(m=>m.id===w.sourceId))source.value=String(w.sourceId);
   orders.replaceChildren();const all=node('option','All waiting orders');all.value='all';orders.append(all);for(const o of waiting){const option=node('option',`#${o.id} ${o.blueprint.name}`);option.value=String(o.id);orders.append(option);}if(Array.from(orders.options).some(o=>o.value===work))orders.value=work;
  }
  changeSource.disabled=!!why||!w||!source.value;retry.disabled=!!why||!w?.enabled||w.phase!=='waiting';
  start.disabled=!!why||!source.value||!waiting.length;pause.disabled=!w?.enabled;stop.disabled=!w?.enabled;resume.disabled=!!why||!w||w.enabled||w.phase==='complete';
 }
 return {update};
}
