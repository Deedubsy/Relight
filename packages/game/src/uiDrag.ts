import {itemIcon} from './itemIcons';
let cancelCurrent:(()=>void)|null=null,suppress=false;
const endHooks:(()=>void)[]=[];
/** Panels that defer rerendering while a drag is live refresh once it ends, whether it dropped or cancelled. */
export function onUiDragEnd(fn:()=>void){endHooks.push(fn);}
export function cancelUiDrag():boolean{if(!cancelCurrent)return false;cancelCurrent();return true;}
export function draggingUi(){return !!cancelCurrent;}
window.addEventListener('click',e=>{if(suppress){e.preventDefault();e.stopImmediatePropagation();suppress=false;}},true);
/** Pointer capture plus hit testing: inventory and shortcut drags retain separate drop policies. */
export function draggable<T>(node:HTMLElement,start:()=>{item:string;count?:number;value:T}|null,preview:(target:Element|null,value:T)=>string,drop:(target:Element|null,value:T)=>void){
 node.addEventListener('dragstart',e=>e.preventDefault());node.style.touchAction='none';
 node.addEventListener('pointerdown',e=>{if(e.button!==0)return;const payload=start();if(!payload)return;const x=e.clientX,y=e.clientY;let active=false,ghost:HTMLElement|null=null;
 const finish=()=>{node.removeEventListener('pointermove',move);node.removeEventListener('pointerup',up);node.removeEventListener('pointercancel',cancel);node.removeEventListener('lostpointercapture',cancel);window.removeEventListener('blur',cancel);document.removeEventListener('visibilitychange',cancel);if(node.hasPointerCapture(e.pointerId))node.releasePointerCapture(e.pointerId);ghost?.remove();document.querySelectorAll('.drop-target').forEach(n=>n.classList.remove('drop-target'));document.body.classList.remove('ui-dragging');cancelCurrent=null;if(active){suppress=true;setTimeout(()=>suppress=false,250);for(const h of endHooks)h();}};
 const cancel=()=>finish();const move=(p:PointerEvent)=>{if(!active&&Math.hypot(p.clientX-x,p.clientY-y)<7)return;if(!active){active=true;cancelCurrent=cancel;document.body.classList.add('ui-dragging');ghost=document.createElement('div');ghost.className='drag-preview';const icon=itemIcon(payload.item);icon.classList.add('drag-stack');if(payload.count!==undefined){const count=document.createElement('b');count.className='stack-count';count.textContent=String(Math.floor(payload.count));icon.append(count);}ghost.append(icon,document.createElement('span'));ghost.setAttribute('aria-label','Dragging '+payload.item);document.body.append(ghost);}p.preventDefault();document.querySelectorAll('.drop-target').forEach(n=>n.classList.remove('drop-target'));const target=document.elementFromPoint(p.clientX,p.clientY);const label=preview(target,payload.value);ghost!.lastElementChild!.textContent=label;ghost!.style.left=`${p.clientX}px`;ghost!.style.top=`${p.clientY}px`;};
 const up=(p:PointerEvent)=>{const target=document.elementFromPoint(p.clientX,p.clientY);if(active){p.preventDefault();p.stopPropagation();if(p.clientX>=0&&p.clientX<innerWidth&&p.clientY>=0&&p.clientY<innerHeight)drop(target,payload.value);}finish();};
 cancelCurrent=cancel;node.setPointerCapture(e.pointerId);node.addEventListener('pointermove',move);node.addEventListener('pointerup',up);node.addEventListener('pointercancel',cancel);node.addEventListener('lostpointercapture',cancel);window.addEventListener('blur',cancel);document.addEventListener('visibilitychange',cancel);
 });
}
