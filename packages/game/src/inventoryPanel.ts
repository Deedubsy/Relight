import {bound,shortcut} from './controls';
import {quickbarView,saveQuickbar,assignQuickbar} from './uiPreferences';
import {weaponRangeText,equipmentCheck,equipmentLabel,itemName as equipmentName,isWeaponItem,RIFLE,HAND_BULLET_SECONDS,type EquipmentAction,type SimState} from '@relight/sim';
import {connectionText} from './machineConnections';
import {hasMachineInventory,machineInventory,machineInventoryDetail,machineTransferPreview,SHOT,nearDepot,itemName,ITEMS,KIND_LABEL,knownEquipment,chestTransferPreview,chestCount,machineById,pocketSlots,pocketUsed,invCap,stackSize,STOP_CAP,SUPPLY_CHEST_CAP,handCraftCheck,type ChestItem,type Kind,type InventoryAction,type PackStack} from '@relight/sim';
import {el} from './uiShell';
import {dispatch,queue,type Session} from './session';
import {baseCore,repairCost,repairCheck,DEFENCE} from '@relight/sim';
import {itemIcon} from './itemIcons';
import {draggable,draggingUi,cancelUiDrag,onUiDragEnd} from './uiDrag';
type StackDrag={side:'pack'|'store';index:number;item:ChestItem;count:number;layout:string;source:number;target:number|null};
const name=(k:string)=>(KIND_LABEL as Record<string,string>)[k]??itemName(k);
const number=(n:number)=>Number.isInteger(n)?String(n):String(Math.round(n*100)/100);
/** Closing the workshop, Escape or any other release cancels every open hand craft; the sim refunds reserved plates. */
export function cancelHandcraft(session:Session):boolean{
 const st=session.state,h=st.flow?.hand;let did=false;
 if(h&&(h.crafting||h.crafts>0)){dispatch(session,{type:'factory',action:{type:'cancelCraft'}});did=true;}
 if(st.engineer.equipment?.craft){dispatch(session,{type:'equipment',action:{type:'cancelRifle'}});did=true;}
 return did;
}
/** GP-HOME-REPAIR (2026-09-11): the Home workshop shows its own base core — HP bar, the repair or recommission cost against
 *  carried materials, and one button that queues the sim's `repairDefence`; availability is the sim's `repairCheck` alone. */
function coreCard(session:Session){
 const node=el('article','recipe-card core-card'),head=el('div','recipe-head'),titleBox=el('div'),outName=el('strong'),hpText=el('span','recipe-time'),bar=el('progress','core-hp'),inputs=el('div','recipe-inputs'),actions=el('div','recipe-actions'),button=el('button'),note=el('span','recipe-note');
 node.dataset.recipe='core';titleBox.append(outName,hpText);head.append(itemIcon('depot'),titleBox);note.setAttribute('role','status');actions.append(button,note);node.append(head,bar,inputs,actions);
 const cells=(['steel','copper'] as const).map(item=>{const span=el('span','recipe-input'),count=el('b'),have=el('small');span.append(itemIcon(item),count,have);span.dataset.item=item;inputs.append(span);return {item,span,count,have};});
 let at:{x:number;y:number}|null=null;
 button.onclick=()=>{if(!at)return;const why=repairCheck(session.state,at.x,at.y);if(why){note.textContent=why;return;}queue(session,{type:'repairDefence',x:at.x,y:at.y});note.textContent='Repair started. Stay within reach of the Home; the materials pay for it once.';};
 return {node,update(st:SimState){
  const core=st.campaign?baseCore(st,st.campaign.homeBlock):undefined,cost=core?repairCost(st,core.x,core.y):null;node.hidden=!core||!cost;if(!core||!cost)return;at={x:core.x,y:core.y};
  const r=st.campaign!.defence?.repair,repairing=!!r&&r.kind==='core'&&r.id===core.block;
  outName.textContent=`Base core${cost.hp===0?' · DISABLED':''}`;hpText.textContent=`${Math.ceil(cost.hp)} / ${cost.max} HP${cost.hp===0?' · substation off until recommissioned':cost.hp<cost.max?' · damaged':''}`;
  bar.max=cost.max;bar.value=cost.hp;bar.classList.toggle('disabled',cost.hp===0);
  inputs.hidden=cost.hp>=cost.max;let short=false;for(const c of cells){const need=c.item==='steel'?cost.steel:cost.copper,have=Math.floor(st.engineer.inv[c.item]??0);c.count.textContent=String(need);c.have.textContent=`/${have}`;c.span.classList.toggle('short',have<need);c.span.title=`${name(c.item)} · ${need} required · ${have} in Backpack`;if(have<need)short=true;}
  const why=cost.hp>=cost.max?'':repairCheck(st,core.x,core.y);
  button.hidden=cost.hp>=cost.max;button.disabled=!!why||repairing;
  button.textContent=repairing?`Repairing · ${Math.ceil(r!.remaining)} s left`:cost.recommission?`Recommission core · ${cost.seconds} s`:`Repair +${DEFENCE.repairHp} HP · ${cost.seconds} s`;
  button.title=why||(cost.recommission?'Full recovery kit from the Backpack: the core returns to full HP and the substation comes back on':'Uses carried materials · stay within reach until it finishes');
  if(!repairing)note.textContent=cost.hp>=cost.max?'Intact.':why&&!short?why:short?`Move ${cost.steel} Steel plates and ${cost.copper} Copper into the Backpack (drag from Home storage) to repair.`:'';
 }};
}
type Recipe={output:string;count:number;inputs:Record<string,number>;seconds:(st:SimState)=>number;check:(st:SimState)=>string;progress:(st:SimState)=>{done:number;queued:number;full:boolean}|null;craft:()=>void;cancel:()=>void};
/** GP-PLAYTEST-FIX 3 (2026-09-11): one compact card per workshop recipe — output, ingredients with carried counts, duration,
 *  Craft; while a batch runs the card shows progress, time left and Cancel. Availability comes from the sim checks alone. */
function recipeCard(r:Recipe){
 const node=el('article','recipe-card'),head=el('div','recipe-head'),titleBox=el('div'),outName=el('strong'),time=el('span','recipe-time'),inputs=el('div','recipe-inputs'),actions=el('div','recipe-actions'),craft=el('button',undefined,'Craft'),note=el('span','recipe-note'),progressRow=el('div','recipe-progress'),bar=el('progress'),left=el('span','recipe-left'),cancel=el('button',undefined,'Cancel');
 node.dataset.recipe=r.output;titleBox.append(outName,time);head.append(itemIcon(r.output),titleBox);note.setAttribute('role','status');actions.append(craft,note);progressRow.append(bar,left,cancel);progressRow.hidden=true;node.append(head,inputs,actions,progressRow);
 const cells=Object.entries(r.inputs).map(([item,need])=>{const span=el('span','recipe-input'),count=el('b'),have=el('small');span.append(itemIcon(item),count,have);span.dataset.item=item;inputs.append(span);return {item,need,span,count,have};});
 craft.onclick=()=>r.craft();cancel.onclick=()=>r.cancel();
 return {node,update(st:SimState){
  const secs=r.seconds(st);outName.textContent=`${name(r.output)} ×${r.count}`;time.textContent=`${secs} s`;
  const missing:string[]=[];for(const c of cells){const have=Math.floor(st.engineer.inv[c.item]??0);c.count.textContent=String(c.need);c.have.textContent=`/${have}`;c.span.classList.toggle('short',have<c.need);c.span.title=`${name(c.item)} · ${c.need} required · ${have} in Backpack`;if(have<c.need)missing.push(`Need ${c.need-have} more ${name(c.item)}`);}
  const p=r.progress(st),reason=r.check(st);inputs.hidden=actions.hidden=!!p;progressRow.hidden=!p;
  if(p){bar.max=secs;bar.value=Math.min(secs,p.done);left.textContent=p.full?'Finished · make Backpack room or Cancel':`${Math.ceil(Math.max(0,secs-p.done))} s left${p.queued>1?` · ${p.queued-1} more queued`:''}`;return;}
  craft.disabled=!!reason;craft.title=reason||`Uses carried materials · ${secs} s · you stay at the workshop until it finishes`;note.textContent=reason?(reason.startsWith('Walk')||!missing.length?reason:missing.join(' · ')):'';
 }};
}
/** Stack presentation reads sim allocations; transfers always recheck the live store, source and reach. */
export function createInventoryPanel(session:Session,root:HTMLElement,target:()=>number|null,freight:HTMLElement,storageOpen:()=>boolean=()=>true){
 root.classList.add('backpack-panel');const pair=el('div','inventory-pair'),notice=el('p','action-result'),info=el('section','item-detail'),art=el('div'),title=el('h3',undefined,'Select a stack'),description=el('p'),quantity=el('input'),ql=el('label',undefined,'Quantity '),transfer=el('button'),split=el('button',undefined,'Split stack'),sort=el('button',undefined,'Sort'),move=el('button',undefined,'Move to slot'),hint=el('p','hint','Drag to move · Shift-click to transfer · Select a stack for actions');notice.setAttribute('role','status');quantity.type='number';quantity.min='1';quantity.step='1';quantity.value='1';quantity.setAttribute('aria-label','Stack quantity');ql.append(quantity);info.append(art,title,description,ql,transfer,split,move,sort);const crafting=el('section','inventory-crafting workshop'),cards=el('div','recipe-cards');crafting.append(el('h3',undefined,'Home workshop'),cards);root.replaceChildren(crafting,pair,info,notice,hint,freight);
 const gear=el('div','equipment-strip'),gearLabel=el('p'),gearButtons=el('div','row'),reload=el('button',undefined,'Reload'),swap=el('button',undefined,'Swap');
 const equipButtons=([0,1] as const).map(slot=>{const b=el('button',undefined,`Equip in slot ${slot+1}`);b.onclick=()=>{if(selected&&isWeaponItem(selected.item))gearSend({type:'equip',slot,item:selected.item});};info.append(b);return b;});
 const unequipButtons=([0,1] as const).map(slot=>{const b=el('button');b.dataset.equipmentSlot=String(slot);b.onclick=()=>gearSend({type:'unequip',slot});draggable(b,()=>{const item=session.state.engineer.equipment?.slots[slot];return item?{item,value:{item}}:null;},target=>target?.closest('[data-inventory-side=pack]')?'Return weapon to Backpack':'Release to cancel',(target,p)=>{if(target?.closest('[data-inventory-side=pack]')&&session.state.engineer.equipment?.slots[slot]===p.item)gearSend({type:'unequip',slot});});gearButtons.append(b);return b;});
 function gearSend(action:EquipmentAction){notice.textContent=dispatch(session,{type:'equipment',action}).reason;selected=null;update();}
 // Backpack shortcuts must run before the shell blocks world keys, including focus on Close.
 window.addEventListener('keydown',event=>{
  if(root.closest('[hidden]')||!root.getClientRects().length||draggingUi()||event.ctrlKey||event.metaKey||event.altKey||
   (event.target instanceof Element&&event.target.closest('input,textarea,select,[contenteditable=true]'))||!session.state.engineer.equipment||!bound('rotate',event.key))return;
  event.preventDefault();event.stopImmediatePropagation();if(!event.repeat)gearSend({type:'reload'});
 },true);
 reload.onclick=()=>gearSend({type:'reload'});swap.onclick=()=>gearSend({type:'swap'});gearButtons.append(reload,swap);gear.append(gearLabel,gearButtons);
 const sides=(['pack','store'] as const).map(side=>{const section=el('section',`inventory-side ${side}`),heading=el('h3'),capacity=el('p','inventory-capacity'),grid=el('div','inventory-grid');grid.dataset.inventorySide=side;grid.setAttribute('role','group');grid.setAttribute('aria-label',side==='pack'?'Backpack slots':'Storage stacks');heading.append(itemIcon(side==='pack'?'backpack':'chest'),document.createTextNode(side==='pack'?'Backpack':'Storage'));section.append(heading,capacity,grid);if(side==='pack')section.append(gear);pair.append(section);return {side,section,capacity,grid,buttons:[] as HTMLButtonElement[]};});
 const bullets=recipeCard({output:'magazine',count:SHOT.count,inputs:{steel:SHOT.inputs.steel,copper:SHOT.inputs.copper},seconds:st=>st.flow?.ammoVersion===1?HAND_BULLET_SECONDS:SHOT.seconds,check:st=>handCraftCheck(st).reason,
  progress:st=>{const h=st.flow?.hand;return h&&h.crafts>0?{done:h.crafting?h.craftProg:0,queued:h.crafts,full:h.full&&h.crafting}:null;},
  craft:()=>{notice.textContent=dispatch(session,{type:'factory',action:{type:'craft',item:'magazine',count:1}}).reason;update();},cancel:()=>{notice.textContent=dispatch(session,{type:'factory',action:{type:'cancelCraft'}}).reason;update();}});
 const rifle=recipeCard({output:'rifle',count:1,inputs:{steel:RIFLE.steel,copper:RIFLE.copper},seconds:()=>RIFLE.seconds,check:st=>equipmentCheck(st,{type:'craftRifle'}),
  progress:st=>{const c=st.engineer.equipment?.craft;return c?{done:c.seconds,queued:1,full:c.seconds>=RIFLE.seconds}:null;},craft:()=>gearSend({type:'craftRifle'}),cancel:()=>gearSend({type:'cancelRifle'})});
 const core=coreCard(session);cards.append(core.node,bullets.node,rifle.node);
 let selected:StackDrag|null=null,moveMode=false,lastContext='',stored:(PackStack|null)[]=[];
 const targetMachine=()=>target()===null?null:machineById(session.state,target()!);
 const storedWeaponKind=(item:import('@relight/sim').WeaponItem)=>session.state.engineer.equipment!.weapons[item]!.kind;
 const storeName=()=>{const m=targetMachine();return m?KIND_LABEL[m.kind]:'Home storage';};
 const storeCount=(item:ChestItem)=>{const id=target(),m=id===null?null:machineById(session.state,id);return id!==null&&!m?0:m?hasMachineInventory(m)?(machineInventory(m)[item]??0):(m.inv[item]??0)+(m.kind==='tramstop'?(m.cargo?.[item]??0):0):chestCount(session.state,item);};
 const snapshot=(side:'pack'|'store',index:number):StackDrag|null=>{const slots=side==='pack'?pocketSlots(session.state.engineer):stored,s=slots[index];if(!s||s.reserved)return null;return {side,index,item:s.item as ChestItem,count:s.count,layout:JSON.stringify(slots),source:side==='pack'?session.state.engineer.inv[s.item]??0:storeCount(s.item as ChestItem),target:target()};};
 function showSelected(p:StackDrag){selected=p;quantity.value=String(Math.max(1,Math.floor(p.count/2)));moveMode=false;details();info.scrollIntoView({block:'nearest'});}
 function details(){const p=selected;art.hidden=!p;art.replaceChildren(...(p?[itemIcon(p.item)]:[]));title.textContent=p?name(p.item):'Select a stack';description.textContent=p?isWeaponItem(p.item)?weaponRangeText(storedWeaponKind(p.item)):!Number.isFinite(p.source)?'Unlimited Home supply · transfer one kit at a time':`${number(p.count)} / ${stackSize(p.item)} in this stack · ${p.side==='pack'?'Backpack':'Storage'}`:'Your carried items stay with the engineer.';ql.hidden=transfer.hidden=split.hidden=move.hidden=!p;if(!p)return;quantity.max=String(Math.floor(p.count));transfer.hidden=!storageOpen();transfer.textContent=p.side==='pack'?`Load into ${storeName()}`:'Take into Backpack';split.hidden=move.hidden=p.side!=='pack';split.disabled=p.count<=1||p.item==='kit';move.disabled=p.item==='kit';}
 function inv(a:InventoryAction){notice.textContent=dispatch(session,{type:'inventory',action:a}).reason;selected=null;moveMode=false;update();}
 function relocate(p:StackDrag,to:number,splitStack=false){inv({type:splitStack?'split':'move',from:p.index,to,item:p.item,count:p.count,layout:p.layout,...(splitStack?{n:Number(quantity.value)}:{})});}
 function send(p:StackDrag,n:number,to?:number){
  if(!storageOpen()||p.target!==target()){notice.textContent='Store changed; select the stack again.';return;}
  const st=session.state,id=target(),m=id===null?null:machineById(st,id);if(id!==null&&!m){notice.textContent='Store removed. Select another store.';return;}
  if(!Number.isSafeInteger(n)||n<=0){notice.textContent='Choose a whole positive quantity.';return;}
  // The Backpack source must still match the picked-up stack; a pooled store only needs to hold the item at all.
  const current=p.side==='pack'?st.engineer.inv[p.item]??0:storeCount(p.item);
  if(p.side==='pack'&&(current!==p.source||JSON.stringify(pocketSlots(st.engineer))!==p.layout)){notice.textContent='Source stack changed. Select it again.';return;}
  if(p.side==='store'&&current<=0){notice.textContent=`${storeName()} no longer holds ${name(p.item)}.`;return;}
  const cells=p.side==='store'?pocketSlots(st.engineer):stored,cell=to===undefined?undefined:cells[to];
  const destination=to===undefined?undefined:p.side==='store'?{slot:to,layout:JSON.stringify(cells)}:{item:cell?.item};
  const requested=Math.min(n,Math.floor(p.count),Number.isFinite(current)?Math.max(1,Math.floor(current)):n),toName=p.side==='pack'?storeName():'Backpack',fromName=p.side==='pack'?'Backpack':storeName();
  const r=dispatch(session,{type:'factory',action:{...(m&&hasMachineInventory(m)?{type:'machineTransfer' as const,id:m.id,put:p.side==='pack'}:{type:p.side==='pack'?'chestPut' as const:'chestTake' as const}),item:p.item,n:requested,target:destination,...(Number.isFinite(p.source)?{expected:{total:p.source,...(p.side==='pack'?{layout:p.layout,slot:p.index}:{})}}:{}),...(m?{x:m.x,y:m.y}:{})}});
  const moved=r.moved??0;notice.textContent=r.ok?`${number(moved)} ${name(p.item)} moved to ${toName}.${moved<requested?` ${number(requested-moved)} stay in ${fromName} (${toName} full).`:''}`:r.reason;selected=null;update();
 }
 transfer.onclick=()=>{if(selected)send(selected,Number(quantity.value));};split.onclick=()=>{if(!selected)return;const to=pocketSlots(session.state.engineer).findIndex(s=>s===null);if(to<0){notice.textContent='Backpack is full; splitting needs an empty slot.';return;}relocate(selected,to,true);};sort.onclick=()=>inv({type:'sort'});move.onclick=()=>{moveMode=true;notice.textContent='Choose a backpack destination slot.';};
 function update(){
  if(root.closest('[hidden]'))return;
  const st=session.state,context=`${storageOpen()}:${target()}`;if(context!==lastContext){cancelUiDrag();lastContext=context;selected=null;notice.textContent='';}
  root.closest('.ui-drawer')?.classList.toggle('storage-window',storageOpen());
  const header=root.closest('.ui-drawer')?.querySelector('header h2');if(header)header.textContent=storageOpen()?`${storeName()} & Backpack`:'Backpack';
  const m=target()===null?null:machineById(st,target()!),items=[...new Set([...ITEMS,...(st.campaign?[]:['kit']),...Object.keys(st.flow?.store??{}),...Object.keys(m?.inv??{}),...Object.keys(m?.cargo??{})])].filter(k=>(k!=='kit'||!st.campaign)&&(!(k in KIND_LABEL)||knownEquipment(st,k as Kind)));
  hint.textContent=m?connectionText(m,st):'Drag to move · Shift-click to transfer · Select a stack for actions';
  stored=[];for(const item of items){let n=storeCount(item as ChestItem);if(!Number.isFinite(n)){stored.push({item,count:1});continue;}while(n>0&&stored.length<600){const count=Math.min(n,stackSize(item));stored.push({item,count});n-=count;}}
  if(!stored.length)stored=Array.from({length:8},()=>null);
  const pack=pocketSlots(st.engineer);for(const side of sides){const isPack=side.side==='pack',cells=isPack?pack:stored;side.section.hidden=!isPack&&!storageOpen();if(!isPack)side.section.querySelector('h3')!.lastChild!.textContent=storeName();side.capacity.textContent=isPack?`${pocketUsed(st.engineer)} / ${invCap(st.engineer)} slots`:target()!==null&&!m?'Store removed':m?hasMachineInventory(m)?machineInventoryDetail(m):m.kind==='tramstop'?`Platform ${Object.values(m.inv).reduce((a,b)=>a+b,0)}/${STOP_CAP} · Arrivals ${Object.values(m.cargo??{}).reduce((a,b)=>a+b,0)}/${STOP_CAP}`:`${Object.values(m.inv).reduce((a,b)=>a+b,0)} / ${SUPPLY_CHEST_CAP} items`:'Home · supplies remain here until collected';
   // A live drag keeps its captured source button; surplus buttons are blanked now and removed once the drag ends.
   if(!draggingUi())while(side.buttons.length>cells.length)side.buttons.pop()!.remove();else for(let i=cells.length;i<side.buttons.length;i++){const b=side.buttons[i];if(b.dataset.content!=='gone'){b.dataset.content='gone';b.replaceChildren();b.setAttribute('aria-label',`${isPack?'Backpack':'Storage'} slot ${i+1}: Empty`);b.title='Empty slot';b.setAttribute('aria-pressed','false');}}
   while(side.buttons.length<cells.length){const i=side.buttons.length,b=el('button','inventory-slot');b.dataset.stackIndex=String(i);b.dataset.side=side.side;side.grid.append(b);side.buttons.push(b);
    b.onclick=e=>{const p=snapshot(side.side,i);if(moveMode&&selected&&isPack){relocate(selected,i);return;}if(p){if(e.shiftKey&&storageOpen())send(p,Math.max(1,Math.floor(p.count)));else showSelected(p);}};
    draggable(b,()=>{const value=snapshot(side.side,i);return value?{item:value.item,count:value.count,value}:null;},(target,p)=>{if(p.side==='pack'&&isWeaponItem(p.item)&&target?.closest('[data-quick-slot], [data-equipment-slot]')){target.closest('[data-quick-slot], [data-equipment-slot]')!.classList.add('drop-target');return 'Equip weapon · action bar selects your active equipment';}const dest=target?.closest<HTMLElement>('[data-inventory-side]');if(dest&&(dest.dataset.inventorySide==='pack'||storageOpen())&&(p.side!==dest.dataset.inventorySide||p.side==='pack')){(target?.closest('.inventory-slot')??dest).classList.add('drop-target');return p.side===dest.dataset.inventorySide?'Move stack':p.side==='pack'?`Load into ${storeName()}`:'Take into Backpack';}return 'Release to cancel';},(target,p)=>{const gearSlot=target?.closest<HTMLElement>('[data-equipment-slot]'),quick=target?.closest<HTMLElement>('[data-quick-slot]');if(p.side==='pack'&&isWeaponItem(p.item)&&(gearSlot||quick)){if(JSON.stringify(pocketSlots(session.state.engineer))!==p.layout){notice.textContent='Backpack changed; try again.';return;}const q=session.state.engineer.equipment!,slot=gearSlot?Number(gearSlot.dataset.equipmentSlot):q.slots.indexOf(null)>=0?q.slots.indexOf(null):q.active;const r=dispatch(session,{type:'equipment',action:{type:'equip',item:p.item,slot:slot as 0|1}});notice.textContent=r.reason;if(r.ok&&quick){quickbarView.slots=assignQuickbar(quickbarView.slots,Number(quick.dataset.quickSlot),'rifle');saveQuickbar(quickbarView.slots);quick.click();}update();return;}const dest=target?.closest<HTMLElement>('[data-inventory-side]');if(!dest){notice.textContent='Drop weapons on equipment or action-bar slots; other items go in inventory slots.';return;}if(dest.dataset.inventorySide!==p.side){send(p,Math.max(1,Math.floor(p.count)),target?.closest<HTMLElement>('[data-stack-index]')?Number(target.closest<HTMLElement>('[data-stack-index]')!.dataset.stackIndex):undefined);}else if(p.side==='pack'){const cell=target?.closest<HTMLElement>('[data-stack-index]');if(cell)relocate(p,Number(cell.dataset.stackIndex));}});
   }
   cells.forEach((cell,i)=>{const unlimited=!isPack&&!!cell&&!Number.isFinite(storeCount(cell.item as ChestItem)),b=side.buttons[i],key=JSON.stringify(cell)+unlimited;if(b.dataset.content!==key){b.dataset.content=key;b.replaceChildren(...(cell?[itemIcon(cell.item),el('span','stack-count',cell.reserved?'Reserved':unlimited?'∞':number(cell.count))]:[]));}b.setAttribute('aria-label',`${isPack?'Backpack':'Storage'} slot ${i+1}: ${cell?`${name(cell.item)} ${cell.reserved?'reserved':unlimited?'unlimited supply':number(cell.count)}`:'Empty'}`);b.title=cell?`${name(cell.item)} · ${cell.reserved?'Kit reserves this slot':unlimited?'Unlimited Home supply':number(cell.count)}`:'Empty slot';b.setAttribute('aria-pressed',String(selected?.side===side.side&&selected.index===i));});
  }
  if(selected){const now=snapshot(selected.side,selected.index);if(!now||now.item!==selected.item||(selected.side==='pack'&&now.count!==selected.count)){selected=null;moveMode=false;}else if(selected.side==='store')selected=now;}
  const eq=st.engineer.equipment;gear.hidden=!eq;gearLabel.textContent=equipmentLabel(st);gearLabel.title=`Equip a carried Rifle, then select its action-bar slot to fire. ${shortcut('rotate')} reloads here or while holding the rifle; ${shortcut('recipe')} swaps while holding it. Bullets must be in your Backpack.`;reload.disabled=!!equipmentCheck(st,{type:'reload'});reload.textContent=`Reload (${shortcut('rotate')})`;reload.title=equipmentCheck(st,{type:'reload'})||'Load carried bullets · 1.5 seconds of running game time';swap.disabled=!!equipmentCheck(st,{type:'swap'});swap.title=equipmentCheck(st,{type:'swap'})||`Swap equipped weapon (${shortcut('recipe')} while holding it)`;unequipButtons.forEach((b,i)=>{const id=eq?.slots[i];b.textContent=`Slot ${i+1}: ${id?equipmentName(id)+' · Unequip':'Empty'}`;b.title=id?`Return ${equipmentName(id)} to the Backpack`:'Drop a carried weapon here to equip it';b.disabled=false;});equipButtons.forEach(b=>b.hidden=!eq||selected?.side!=='pack'||!selected||!isWeaponItem(selected.item));
  details();if(selected&&storageOpen()){const at=m?[m.x,m.y] as [number,number]:undefined,p=m&&hasMachineInventory(m)?machineTransferPreview(st,m.id,selected.item,Math.max(1,Math.floor(selected.count)),selected.side==='pack'):chestTransferPreview(st,selected.item,Math.max(1,Math.floor(selected.count)),selected.side==='pack',at);description.textContent+=p.moved?` · up to ${number(p.moved)} can transfer`:` · ${p.reason}`;}
  const h=st.flow?.hand,busy=!!(h&&h.crafts>0)||!!eq?.craft;crafting.hidden=!!m||!nearDepot(st)||(!storageOpen()&&!busy);if(!crafting.hidden){core.update(st);bullets.update(st);rifle.node.hidden=!eq;if(eq)rifle.update(st);}
 }
 onUiDragEnd(()=>{if(!root.closest('[hidden]'))update();});
 return {update};
}
