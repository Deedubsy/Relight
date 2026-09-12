import {fabricationCheck} from '@relight/sim';
import {createInventoryPanel} from './inventoryPanel';
import {connectionText} from './machineConnections';
import {hasMachineInventory,itemName,campaignRecruited,inspectMachine,machineById,recipesFor,recipeOf,isProcessor,progressionCheck,knownInputSource,ITEMS,type Item} from '@relight/sim';
import {el,type UiShell} from './uiShell';
import {dispatch,type Session} from './session';
import {inspectionView} from './view';
/** Stable identity, controls and details state; only the read-only facts change between ticks. */
export function createInspectionPanel(session:Session,root:HTMLElement,shell:UiShell,controls:HTMLElement[],hooks:{locate(x:number,y:number):void;item(item:Item):void;inventory(x:number,y:number):void;route(x:number,y:number):void}){
 const dismiss=el('button',undefined,'Dismiss removed inspection'),title=el('h2'),location=el('p','hint'),status=el('p','inspection-status'),connections=el('p','machine-connections'),output=el('section'),inputs=el('section'),blockers=el('section','inspection-blockers'),actions=el('section'),details=el('details','inspection-details'),facts=el('div'),result=el('p','action-result');
 const pin=el('button',undefined,'Pin inspection'),locate=el('button',undefined,'Locate machine'),transfer=el('button',undefined,'Open supplies'),route=el('button',undefined,'Station route'),feed=el('button',undefined,'Load carried bullets'),chip=el('button','ui-hud inspection-chip');chip.hidden=true;document.getElementById('app')!.append(chip);shell.protect(chip);
 actions.append(el('h3',undefined,'Actions'),pin,locate,transfer,route,feed,...controls,result);details.append(el('summary',undefined,'Details: rates, circuit and contents'),output,facts);const inventory=el('section');root.replaceChildren(title,location,status,inventory,blockers,inputs,connections,actions,details,dismiss);const inventoryUi=createInventoryPanel(session,inventory,()=>inspectionView.machineId,el('div'));dismiss.onclick=()=>{inspectionView.pinned=false;shell.close();};
 const machine=()=>inspectionView.machineId===null?null:machineById(session.state,inspectionView.machineId);
 pin.onclick=()=>{inspectionView.pinned=!inspectionView.pinned;};chip.onclick=()=>{shell.open('inspection');root.hidden=false;};
 locate.onclick=()=>{const m=machine();if(m)hooks.locate(m.x+m.size/2,m.y+m.size/2);};
 transfer.onclick=()=>{const m=machine();if(m)hooks.inventory(m.x,m.y);};route.onclick=()=>{const m=machine();if(m)hooks.route(m.x,m.y);};
 const decode=el('button',undefined,'Decode Overclock schematic · 15 powered seconds');decode.onclick=()=>{const m=machine();if(m)result.textContent=dispatch(session,{type:'fabrication',action:{type:'decode',machine:m.id}}).reason;};actions.append(decode);
 const upgrades=el('div','row'),recipeSelect=el('select'),chooseRecipe=el('button',undefined,'Set recipe'),attach=el('button',undefined,'Install carried Overclock'),detach=el('button',undefined,'Remove Overclock'),hopper=el('button',undefined,'Upgrade hopper · 20 steel + 10 copper');recipeSelect.setAttribute('aria-label','Machine recipe');upgrades.append(recipeSelect,chooseRecipe,attach,detach,hopper);actions.append(upgrades);
 const sendUpgrade=(action:import('@relight/sim').ProgressionAction)=>{const r=dispatch(session,{type:'progression',action});result.textContent=r.reason;};
 attach.onclick=()=>{const m=machine(),item=ITEMS.find(k=>(session.state.campaign?.progression?.gameplay?k==='overclock':k.startsWith('artifact'))&&(session.state.engineer.inv[k]??0)>0);if(m&&item)sendUpgrade({type:'attach',machine:m.id,item});};detach.onclick=()=>{const m=machine();if(m)sendUpgrade({type:'detach',machine:m.id});};hopper.onclick=()=>{const m=machine();if(m)sendUpgrade({type:'hopper',machine:m.id});};
 chooseRecipe.onclick=()=>{const m=machine();if(m){const r=dispatch(session,{type:'factory',action:{type:'setRecipe',x:m.x,y:m.y,recipe:recipeSelect.value}});result.textContent=r.reason;}};
 feed.onclick=()=>{const m=machine();if(m){const r=dispatch(session,{type:'factory',action:{type:'feed',x:m.x,y:m.y}});result.textContent=r.reason;}};
 let key='',constraintsKey='',lastId:number|null=null;
 const p=(text:string)=>el('p',undefined,text),num=(n:number|null)=>n===null?'—':Number(n.toFixed(2)).toLocaleString();
 return {update(){
  const id=inspectionView.machineId,info=id===null?null:inspectMachine(session.state,id),m=machine();
  chip.hidden=!inspectionView.pinned||id===null||shell.active()==='inspection'||shell.paused();chip.textContent=info?`Inspect ${info.title} · ${info.status.state}`:'Machine removed · inspection';
  if(!chip.hidden){const clock=document.querySelector('.hud-time')?.getBoundingClientRect(),map=document.querySelector('.hud-minimap')?.getBoundingClientRect();chip.style.top=`${Math.max(clock?.bottom??0,map&&map.top<100?map.bottom:0)+12}px`;}
  if(lastId!==id){lastId=id;details.open=false;result.textContent='';key='';constraintsKey='';}
  pin.textContent=inspectionView.pinned?'Unpin inspection':'Pin inspection';pin.setAttribute('aria-pressed',String(inspectionView.pinned));
  dismiss.hidden=!!info||!inspectionView.pinned;
  if(!info){inventory.hidden=true;root.closest('.ui-drawer')?.classList.remove('storage-window');connections.hidden=true;title.textContent='Machine removed';status.textContent='The selected machine no longer exists.';location.textContent='Select another machine to inspect it.';for(const n of [output,inputs,blockers,actions,details])n.hidden=true;return;}
  for(const n of [output,inputs,blockers,actions,details])n.hidden=false;
  connections.textContent=m?connectionText(m,session.state):'';connections.hidden=!connections.textContent;
  title.textContent=info.title;location.textContent=info.location;status.textContent=`${info.status.state==='running'?'Running':info.status.state==='idle'?'Idle':info.status.state==='off'?'Offline':info.status.state==='blocked'?'Blocked':'Waiting'} · ${info.status.reason}`;status.dataset.state=info.status.state;
  inventory.hidden=!m||!hasMachineInventory(m);if(!inventory.hidden)inventoryUi.update();else root.closest('.ui-drawer')?.classList.remove('storage-window');
  transfer.hidden=!['chest','tramstop','depot'].includes(info.kind);route.hidden=info.kind!=='tramstop';feed.hidden=true;feed.textContent=info.kind==='generator'?'Load carried fuel':info.kind==='cannon'?'Load carried Shells':'Load carried bullets';feed.disabled=!info.reachable;
  decode.hidden=m?.kind!=='alienworkbench';decode.disabled=!m||!!fabricationCheck(session.state,{type:'decode',machine:m.id});decode.title=m?fabricationCheck(session.state,{type:'decode',machine:m.id}):'';upgrades.hidden=!session.state.campaign;const allowed=m&&isProcessor(m)?recipesFor(m):[];if(recipeSelect.dataset.machine!==String(m?.id)){recipeSelect.replaceChildren(...allowed.map(id=>{const o=el('option',undefined,id==='shot'&&session.state.flow?.ammoVersion===1?'10 bullets':recipeOf({kind:m!.kind,recipe:id}).name);o.value=id;return o;}));recipeSelect.dataset.machine=String(m?.id);recipeSelect.value=m?.recipe??'shot';}recipeSelect.hidden=chooseRecipe.hidden=!allowed.length;attach.hidden=detach.hidden=!(session.state.campaign?.progression?.sites.some(s=>s.kind==='artifact'&&s.seen)||m?.artifact)||!m||!isProcessor(m)&&!['excavator','pumpjack'].includes(m.kind);attach.disabled=!m||!info.reachable||!!m.artifact||!ITEMS.some(k=>(session.state.campaign?.progression?.gameplay?k==='overclock':k.startsWith('artifact'))&&(session.state.engineer.inv[k]??0)>0);detach.disabled=!m?.artifact||!info.reachable;detach.hidden=detach.hidden||!m?.artifact;attach.title='Adds 10% processing capacity; inputs, power and output space still limit output.';hopper.hidden=m?.kind!=='turret'||!campaignRecruited(session.state,'gunsmith');hopper.title=m?progressionCheck(session.state,{type:'hopper',machine:m.id}):'';hopper.disabled=!m||!!progressionCheck(session.state,{type:'hopper',machine:m.id});
  const nextKey=JSON.stringify(info);if(nextKey===key)return;key=nextKey;
  const measured=info.measured;output.replaceChildren(el('h3',undefined,'Useful output'));
  if(measured){output.append(p(measured.seconds>0?`${info.measurement} over ${Math.round(measured.seconds)} seconds.`:'Waiting for simulation time — no measured output yet.'));for(const r of measured.rows)if(r.produced>0||info.nominal.some(n=>n.item===r.item&&n.output>0))output.append(p(`${itemName(r.item)}: ${num(r.producedPerMin)}/min measured · ${num(r.produced)} recorded`));}else output.append(p(info.capacity??info.status.reason));
  inputs.replaceChildren(el('h3',undefined,'Inputs'));
  if(info.recipe)inputs.append(p(`Recipe: ${info.recipe}`));
  if(m?.artifact)inputs.append(p('Overclock installed · +10% processing capacity · inputs, power and output space still limit output · 1/1 slot'));
  for(const r of info.nominal.filter(n=>n.input>0))inputs.append(p(`${itemName(r.item)}: ${num(m?.inv[r.item]??0)} buffered · ${num(r.input)}/min nominal input`));
  if(!info.nominal.some(n=>n.input>0))inputs.append(p(info.kind==='generator'?`${num(m?.inv.coal??0)} coal + ${num(m?.inv.fuel??0)} refined fuel in store`:info.kind==='turret'?`${num(m?.inv.rounds??0)} rounds in hopper`:info.kind==='cannon'?`${num(m?.inv.shell??0)} Shells in hopper`:'No recipe inputs.'));
  const ck=JSON.stringify([info.blockers,info.nextInputs,info.status.state,info.reachable,...[...info.blockers,...info.nextInputs].filter(b=>b.item).map(b=>knownInputSource(session.state,b.item!))]);
  if(ck!==constraintsKey){constraintsKey=ck;
  blockers.replaceChildren(el('h3',undefined,'Current constraints'));
  if(!info.blockers.length)blockers.append(p(info.status.state==='running'?'No current blocker.':info.status.reason));
  for(const b of [...info.blockers,...info.nextInputs]){const row=el('div','inspection-constraint');row.append(p(b.text));if(b.item){const item=b.item,help=el('button',undefined,`About ${itemName(item)}`);help.onclick=()=>hooks.item(item);row.append(help);const source=knownInputSource(session.state,item);if(source){const go=el('button',undefined,`Locate ${source.label}`);go.onclick=()=>hooks.locate(source.x,source.y);row.append(go);}}blockers.append(row);}
  if(!info.reachable)blockers.append(p('Out of reach — walk closer to change settings or transfer.'));
  }
  const c=info.circuit;facts.replaceChildren(p(`${c.scope}: ${num(c.supply)} kW supply / ${num(c.demand)} kW demand / ${num(c.load)} kW load / ${num(c.throttle*100)}% throttle`),p(`Machine draw: ${info.draw} kW`));
  for(const r of info.nominal)facts.append(p(`${itemName(r.item)}: nominal ${num(r.input)} in / ${num(r.output)} out per min; power-limited capacity ${num(r.input*info.scale)} in / ${num(r.output*info.scale)} out per min`));
  if(info.capacity)facts.append(p(info.capacity));facts.append(el('h3',undefined,'Separate stores'));
  if(!info.contents.length)facts.append(p('Contents: empty'));for(const c of info.contents)facts.append(p(`${c.place} · ${itemName(c.item)}: ${num(c.count)}`));for(const setting of info.settings)facts.append(p(setting));if(info.range)facts.append(p(info.range));
 }};
}
