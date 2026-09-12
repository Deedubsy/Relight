import {machineAmmoUnit} from './flow';
import {transferTarget,allocateTransfer,type TransferTarget} from './engineer';
import type {SimState} from './types';
import {ITEMS,GENERATOR_COAL_CAP,ASM_INPUT_MULT,ASM_OUTPUT_CAP,recipeOf,recipeOutput,isProcessor,isItem,machineById,accepts,KIND_LABEL,type Machine,type Item} from './flow';
import {TURRET_HOPPER} from './recipes';
import {hopperCapacity,CORRECTIONS} from './progression';
import {take as pocketTake,drop as pocketDrop,pocketSlots} from './engineer';
import {inReach} from './ground';
import {machineDimensions} from './footprint';
import {itemName} from './itemNames';

export function hasMachineInventory(m:Machine):boolean {
 return isProcessor(m)||['generator','turret','cannon','excavator','pumpjack'].includes(m.kind);
}
/** Whole transferable items only. A turret keeps loose rounds until fired or repacked. */
export function machineInventory(m:Machine):Record<string,number> {
 if(m.kind==='turret')return {magazine:Math.floor((m.inv.rounds??0)/machineAmmoUnit(m))};
 const inv:Record<string,number>={};for(const k of ITEMS)if((m.inv[k]??0)>0)inv[k]=m.inv[k];
 if(isProcessor(m)){const k=recipeOutput(recipeOf(m));inv[k]=(inv[k]??0)+m.out;}
 if(m.hold)inv[m.hold]=(inv[m.hold]??0)+1;
 return inv;
}
export function machineInventoryDetail(m:Machine):string {
 if(m.kind==='turret')return `${m.inv.rounds??0} / ${hopperCapacity(m,TURRET_HOPPER)} rounds · 1 item = ${machineAmmoUnit(m)} round(s) · ${Math.floor((m.inv.rounds??0)%machineAmmoUnit(m))} loose rounds stay loaded`;
 if(m.kind==='generator')return `${(m.inv.coal??0)+(m.inv.fuel??0)} / ${GENERATOR_COAL_CAP} fuel · accepts Coal or Refined fuel`;
 if(m.kind==='cannon')return `${m.inv.shell??0} / ${CORRECTIONS.cannon.capacity} Shells`;
 if(isProcessor(m)){const r=recipeOf(m);return `Inputs: ${Object.entries(r.inputs).map(([k,n])=>`${m.inv[k]??0}/${n*ASM_INPUT_MULT} ${itemName(k)}`).join(' · ')}. Finished: ${m.out}/${m.ammoVersion===1&&r.output==='rounds'?50:ASM_OUTPUT_CAP} ${itemName(recipeOutput(r))}. In-progress ingredients are already committed.`;}
 return `Output: ${m.hold?'1 '+itemName(m.hold):'empty'} / 1 item`;
}
export interface MachineTransfer {type:'machineTransfer';id:number;item:string;n:number;put:boolean;expected?:{total:number;layout?:string;slot?:number};target?:TransferTarget}
export function machineTransferPreview(st:SimState,id:number,item:string,n:number,put:boolean):{moved:number;reason:string} {
 const no=(reason:string)=>({moved:0,reason}),m=machineById(st,id);
 if(st.engineer.down>=0)return no('Wait until you recover');
 if(!m||!hasMachineInventory(m))return no('Machine inventory unavailable');
 if(!isItem(item)||!Number.isSafeInteger(n)||n<=0)return no('Choose a whole positive quantity');
 const [w,h]=machineDimensions(m);if(!inReach(st,m.x,m.y,w,h))return no(`Walk closer to the ${KIND_LABEL[m.kind]}`);
 if(put){const copy={...m,inv:{...m.inv}},have=Math.min(n,Math.floor(st.engineer.inv[item]??0));let moved=0;
  while(moved<have&&accepts(st,copy,item)){if(m.kind==='turret')copy.inv.rounds=(copy.inv.rounds??0)+machineAmmoUnit(m);else copy.inv[item]=(copy.inv[item]??0)+1;moved++;}
  return {moved,reason:moved?'':have<=0?`No ${itemName(item)} in Backpack`:`Cannot load ${itemName(item)}: wrong input or inventory full`};
 }
 const available=Math.min(n,Math.floor(machineInventory(m)[item]??0));
 const e={...st.engineer,inv:{...st.engineer.inv},pack:st.engineer.pack?.map(s=>s?{...s}:null)};
 const moved=pocketTake(e,item,available);return {moved,reason:moved?'':available<=0?`No whole ${itemName(item)} available`:'Backpack is full'};
}
export function machineTransfer(st:SimState,a:MachineTransfer):{ok:boolean;moved:number;reason:string} {
 const m=machineById(st,a.id),slots=pocketSlots(st.engineer),no=(reason:string)=>({ok:false,moved:0,reason});
 if(!m)return no('Machine removed; select another machine');
 if(a.expected){const total=a.put?(st.engineer.inv[a.item]??0):(machineInventory(m)[a.item]??0);
  if(a.put?total!==a.expected.total:total<1)return no(a.put?'Source changed; select the stack again':'That stack has already left the machine');
  const cell=slots[a.expected.slot??-1];if(a.put&&(a.expected.layout!==JSON.stringify(slots)||cell?.item!==a.item||cell.count<a.n))return no('Source stack changed; select it again');
 }
 const check=transferTarget(st.engineer,a.item,a.n,a.target,a.put);if(check.n<=0)return no(check.reason);
 const p=machineTransferPreview(st,a.id,a.item,check.n,a.put);if(!p.moved)return no(p.reason);
 const k=a.item as Item;
 if(a.put){if(m.kind==='turret'){m.inv.rounds=(m.inv.rounds??0)+p.moved*machineAmmoUnit(m);st.flow!.stats.handFed+=p.moved;st.flow!.stats.handFedMags+=p.moved;}else{m.inv[k]=(m.inv[k]??0)+p.moved;if(m.kind==='generator'){st.flow!.stats.handFed+=p.moved;if(k==='coal')st.flow!.stats.handFedCoal+=p.moved;}}pocketDrop(st.engineer,k,p.moved);
  if(a.expected){const i=a.expected.slot!;slots[i]!.count-=p.moved;if(slots[i]!.count<=0)slots[i]=null;st.engineer.pack=slots;}
 }else{pocketTake(st.engineer,k,p.moved);allocateTransfer(st.engineer,k,p.moved,a.target,slots);let left=p.moved;
  if(m.kind==='turret'){m.inv.rounds-=left*machineAmmoUnit(m);left=0;}
  if(isProcessor(m)&&recipeOutput(recipeOf(m))===k){const n=Math.min(left,m.out);m.out-=n;left-=n;}
  if(left&&m.hold===k){m.hold=null;left--;}
  if(left)m.inv[k]-=left;
 }
 return {ok:true,moved:p.moved,reason:`${p.moved} ${itemName(k)} ${a.put?'loaded':'taken'}`};
}

