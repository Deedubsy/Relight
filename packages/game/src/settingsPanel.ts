import {preferenceView,saveUiPreferences,quickbarView,DEFAULT_QUICKBAR} from './uiPreferences';
import {applyBindings,BINDINGS,DEFAULT_BINDINGS,FIXED,MODIFIED,bindingProblem,type Binding} from './controls';
import {el} from './uiShell';
/** Browser preferences never enter a save envelope or simulation command. */
export function applyUiPreferences(){
 const p=preferenceView.value;applyBindings(p.bindings);document.documentElement.style.setProperty('--ui-scale',String(p.scale/100));
 document.body.dataset.motion=p.motion;document.body.classList.toggle('ui-compact',innerWidth/(p.scale/100)<1100);document.body.classList.toggle('ui-short',innerHeight/(p.scale/100)<650);
 window.dispatchEvent(new Event('relight:preferences'));
}
export function createSettings(){
 const root=el('section','ui-settings'),status=el('p','action-result');status.setAttribute('role','status');
 const scale=el('select'),motion=el('select');scale.setAttribute('aria-label','Interface scale');motion.setAttribute('aria-label','Interface motion');
 for(const n of [100,125,150]){const o=el('option',undefined,`${n}%`);o.value=String(n);scale.append(o);}
 for(const [v,t] of [['system','Follow system preference'],['reduce','Reduce motion'],['full','Full interface motion']]){const o=el('option',undefined,t);o.value=v;motion.append(o);}
 const save=(change:Parameters<typeof saveUiPreferences>[0])=>{const ok=saveUiPreferences(change);applyUiPreferences();status.textContent=ok?'Settings saved for this browser.':'Settings work for this session. Browser storage is unavailable; retry saving when storage is available.';};
 scale.value=String(preferenceView.value.scale);motion.value=preferenceView.value.motion;scale.onchange=()=>save({scale:Number(scale.value) as 100|125|150});motion.onchange=()=>save({motion:motion.value as 'system'|'reduce'|'full'});
 const sl=el('label',undefined,'Interface scale '),ml=el('label',undefined,'Interface motion ');sl.append(scale);ml.append(motion);root.append(sl,ml,el('p',undefined,'UI scale changes text and controls independently of camera zoom. Settings are shared by this browser; game saves keep their own profile.'));
 const action=el('select'),key=el('input'),apply=el('button',undefined,'Apply binding'),bindingStatus=el('p','action-result');action.setAttribute('aria-label','Action to rebind');key.setAttribute('aria-label','New binding');key.placeholder='Focus here and press a key';key.readOnly=true;
 for(const a of Object.keys(DEFAULT_BINDINGS) as Binding[])if(!FIXED.includes(a)){const o=el('option',undefined,a);o.value=a;action.append(o);}
 let draft='';const current=el('p');const show=()=>{draft='';const a=action.value as Binding;current.textContent=`Current: ${MODIFIED.includes(a)?'Ctrl / Cmd + ':''}${BINDINGS[a].map(k=>k===' '?'Space':k).join(' / ')||'Unassigned'}`;key.value='';};action.onchange=show;show();
 key.onkeydown=e=>{if(e.key==='Tab'||e.key==='Escape')return;e.preventDefault();e.stopPropagation();draft=e.key;key.value=e.key===' '?'Space':e.key;bindingStatus.textContent=bindingProblem(action.value as Binding,draft,preferenceView.value.bindings);};
 apply.onclick=()=>{const a=action.value as Binding,why=bindingProblem(a,draft,preferenceView.value.bindings);bindingStatus.textContent=why;if(why)return;save({bindings:{...preferenceView.value.bindings,[a]:draft}});show();bindingStatus.textContent='Binding applied. Release held keys before returning to the city.';};
 const reset=el('button',undefined,'Restore default bindings');reset.onclick=()=>{save({bindings:{}});show();bindingStatus.textContent='Default bindings restored.';};
 const hints=el('button',undefined,'Restore opening hint for new cities');hints.onclick=()=>save({dismissedHints:[]});
 const slots=el('button',undefined,'Restore default quickbar');slots.onclick=()=>{quickbarView.slots=[...DEFAULT_QUICKBAR];save({quickbar:[...DEFAULT_QUICKBAR]});};
 const retry=el('button',undefined,'Retry saving settings');retry.onclick=()=>save({});
 root.append(el('h3',undefined,'Keyboard bindings'),el('p',undefined,'Ctrl / Cmd shortcuts and blueprint transforms keep their contexts. Escape, Tab navigation and numbered quickbar slots remain fixed; assign slot contents in Build.'),action,current,key,apply,bindingStatus,reset,el('h3',undefined,'Restore and recover'),hints,slots,retry,status);
 return root;
}
