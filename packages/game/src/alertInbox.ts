import {campaignAlerts,blockName,type CampaignAlert} from '@relight/sim';
import type {Session} from './session';
import {el} from './uiShell';
interface Entry {alert:CampaignAlert;active:boolean;dismissed:boolean;count:number;row:HTMLElement;text:HTMLElement;locate:HTMLButtonElement;dismiss:HTMLButtonElement}
/** Session presentation history; dismissing never changes authoritative conditions or the urgent HUD. */
export function createAlertInbox(session:Session,root:HTMLElement,locate:(x:number,y:number)=>void){
 const inbox=el('details','alert-inbox'),summary=el('summary',undefined,'Alerts and received messages'),intro=el('p',undefined,'Current conditions and session history. Dismissed danger stays visible on the HUD.'),list=el('div');inbox.append(summary,intro,list);root.append(inbox);
 const entries=new Map<string,Entry>();
 function put(a:CampaignAlert,active:boolean,repeat=false){let e=entries.get(a.id);if(!e){const row=el('article'),text=el('p'),loc=el('button',undefined,'Locate alert'),dismiss=el('button',undefined,'Dismiss');row.append(text,loc,dismiss);list.prepend(row);e={alert:a,active,dismissed:false,count:0,row,text,locate:loc,dismiss};entries.set(a.id,e);
   loc.onclick=()=>{const current=campaignAlerts(session.state).find(c=>c.id===a.id);if(current?.location)locate(current.location.x,current.location.y);else loc.textContent='Location unavailable';};
   dismiss.onclick=()=>{e!.dismissed=true;render(e!);};
  }if(active&&!e.active)e.dismissed=false;e.alert=a;e.active=active;if(repeat)e.count++;render(e);
 }
 function render(e:Entry){e.text.textContent=`${e.active?'Active':e.alert.id.startsWith('message:')?'Message':'Resolved'}${e.dismissed?' · dismissed':''} · ${e.alert.title}${e.count>1?` ×${e.count}`:''}. ${e.alert.detail}`;e.row.classList.toggle('resolved',!e.active);e.locate.textContent=e.active&&e.alert.location?'Locate alert':'Location unavailable';e.locate.setAttribute('aria-disabled',String(!e.active||!e.alert.location));e.dismiss.hidden=e.dismissed;}
 return {element:inbox,record(message:string,kind:string){put({id:`message:${kind}:${message}`,title:message,detail:'',priority:0,location:null},false,true);},update(){
   const alerts=campaignAlerts(session.state),ids=new Set(alerts.map(a=>a.id));for(const e of entries.values())if(e.active&&!ids.has(e.alert.id)){e.active=false;render(e);}for(const a of alerts)put(a,true);
   const w=session.state.campaign?.defence?.warning;if(w){const id=`message:radio:${w.assault}`;put({id,title:`Received radio warning · ${blockName(session.state,w.block)}`,detail:`Received at ${Math.floor(w.receivedAt)} s${w.approach?` · ${w.approach} · ${w.composition}`:''}. Retained during power outages.`,priority:0,location:null},false);}
   summary.textContent=`Alerts and received messages · ${alerts.length} active`;
 }};
}
