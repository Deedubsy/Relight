import {itemName, cityApproach,campaignDiscoveries, itemGuide, ITEMS, type Item } from '@relight/sim';
import {dispatch,type Session} from './session';
import {objectiveView} from './view';

const node=<K extends keyof HTMLElementTagNameMap>(tag:K,text=''):HTMLElementTagNameMap[K]=>{const n=document.createElement(tag);n.textContent=text;return n;};
/** Keep controls and focus stable while the simulation updates their current facts. */
export function createCampaignGuide(session:Session,root:HTMLElement,hooks:{locate(x:number,y:number):void}){
  const journal=node('details'),summary=node('summary','Known projects');journal.className='campaign-guide';journal.open=true;
  const intro=node('p','Explore for nearby clues. Only known places appear here; recruiting and restoring require a local visit.');
  const choice=node('select');choice.setAttribute('aria-label','Known project');
  const list=node('div'),card=node('article'),title=node('h3'),status=node('strong'),detail=node('p'),needs=node('div'),actions=node('div'),feedback=node('p');
  card.className='project-card';status.className='project-status';needs.className='project-circuit';feedback.className='action-result';feedback.setAttribute('role','status');
  const track=node('button','Track project'),locate=node('button','Show project location');
  card.append(title,status,detail,needs,track,locate,actions,feedback);list.append(card);journal.append(summary,intro,choice,list);root.append(journal);
  let selected='',key='',choicesKey='',busy=false,completed=false;
  choice.onchange=()=>{selected=choice.value;key='';feedback.textContent='';updateProject();};
  track.onclick=()=>{if(campaignDiscoveries(session.state).some(r=>r.id===selected)){objectiveView.targetId=selected;updateProject();}};
  locate.onclick=()=>{const row=campaignDiscoveries(session.state).find(r=>r.id===selected);if(row){const p=cityApproach(session.state,row.x+row.size/2,row.y+row.size/2);hooks.locate(p.x,p.y);}else feedback.textContent='Location unavailable.';};
  const recipes=node('details'),recipeSummary=node('summary','Items and recipes');recipes.className='campaign-guide item-guide';
  const label=node('label','Item '),select=node('select');select.setAttribute('aria-label','Item guide');
  for(const item of ITEMS){const o=node('option',itemName(item));o.value=item;select.append(o);}label.append(select);
  const body=node('div');body.className='item-guide-body';recipes.append(recipeSummary,label,body,node('p','Recruit the Foreman to use the blueprint clipboard: Ctrl+C selects a layout and Ctrl+V previews paid construction.'));root.append(recipes);
  let recipeKey='';
  function updateRecipe(){if(!recipes.open)return;const info=itemGuide(session.state,select.value as Item);if(!info)return;const key=JSON.stringify(info);if(key===recipeKey)return;recipeKey=key;body.replaceChildren(node('h3',info.title));
    body.append(node('h4','How to obtain'));
    for(const source of info.sources)body.append(node('p',source));
    for(const r of info.recipes){const card=node('article');card.append(node('strong',`${r.name} · ${r.available?'Available':'Locked'}`),node('p',`${r.inputs.map(i=>`${i.count} ${itemName(i.item)}`).join(' + ')} → ${r.count} ${itemName(info.item)} every ${r.seconds}s`),node('p',`${r.machine} · ${r.kw} kW · nominal ${r.count*60/r.seconds}/min. Supply, power and output space limit actual production.`),node('p',r.provenance));body.append(card);}
    body.append(node('h4','Used by'));const uses=node('ul');for(const use of info.uses)uses.append(node('li',use));body.append(uses,node('h4','Move and store'),node('p',info.transport));
  }
  select.onchange=updateRecipe;recipes.ontoggle=updateRecipe;
  function updateProject(){
    const rows=campaignDiscoveries(session.state),ck=rows.map(r=>r.id+r.title).join('|');
    if(ck!==choicesKey){choicesKey=ck;choice.replaceChildren(...rows.map(r=>{const o=node('option',r.title);o.value=r.id;return o;}));}
    if(!rows.some(r=>r.id===selected))selected=rows.find(r=>r.id===objectiveView.targetId)?.id??rows[0]?.id??'';
    choice.value=selected;const row=rows.find(r=>r.id===selected);card.hidden=!row;if(!row)return;
    track.textContent=objectiveView.targetId===selected?'Tracking project':'Track project';
    const k=JSON.stringify(row);if(k===key)return;key=k;card.dataset.discovery=row.id;
    title.textContent=row.title;status.textContent=row.circuit?.restored?'Restored · materials consumed':row.status;detail.textContent=row.detail;
    const done=!!row.circuit?.restored;if(done&&!completed){card.classList.remove('project-completed');void card.offsetWidth;card.classList.add('project-completed');}completed=done;
    needs.replaceChildren();const c=row.circuit;
    if(c){
      needs.append(node('h4','Restoration service circuit'));
      const circuit=node('ol');circuit.className='service-nodes';
      for(const m of c.materials){const n=node('li',`${c.restored?'✓ Consumed':m.delivered>=m.required?'✓ Delivered':'○ Material'} · ${itemName(m.item)}: ${c.restored?m.required:m.delivered}/${m.required}`);n.className=c.restored||m.delivered>=m.required?'complete':'';circuit.append(n);}
      const power=node('li',`${c.powered?'✓':'○'} Local power · ${c.powered?'online':'offline'}`);power.className=c.powered?'complete':'';circuit.append(power);needs.append(circuit);
      if(c.restored)needs.append(node('p',row.status));
      else {const checklist=node('ul');for(const m of c.materials)checklist.append(node('li',`${itemName(m.item)}: ${m.delivered} delivered; ${m.required-m.delivered} remaining. Carried ${m.carried} · Home storage ${m.home}. Will deliver ${m.transfer}.`));needs.append(checklist,node('p',c.blocker||'Materials and local power are ready.'));}
    }
    if(!c&&row.needs.length)needs.append(node('h4','Required materials'),...row.needs.map(n=>node('p',`${itemName(n.item)}: ${n.delivered}/${n.required} delivered; carrying ${session.state.engineer.inv[n.item]??0}.`)));
    if(row.reward)needs.append(node('h4','Tram kit · collect what fits'),...row.reward.map(r=>node('p',`${itemName(r.item)}: ${r.remaining} remaining · will collect ${r.collect}`)));
    if(row.upgrade)needs.append(node('h4','Radio precision upgrade'),node('p',row.upgrade.bought?'Purchased · approach and composition added to received warnings.':`Separate purchase: ${row.upgrade.steel} Steel plates + ${row.upgrade.copper} Copper from Backpack. Adds approach and composition to received warnings; local power required.`));
    while(actions.children.length>row.actions.length)actions.lastChild!.remove();
    row.actions.forEach((a,i)=>{let box=actions.children[i] as HTMLElement|undefined;if(!box){box=node('div');box.append(node('button'),node('p'));actions.append(box);}const button=box.children[0] as HTMLButtonElement,why=box.children[1] as HTMLElement;
      button.textContent=a.label;button.setAttribute('aria-disabled',String(!!a.reason));why.textContent=a.reason;why.hidden=!a.reason;
      button.onclick=()=>{if(busy)return;const fresh=campaignDiscoveries(session.state).find(r=>r.id===selected)?.actions[i];if(!fresh){feedback.textContent='Action unavailable.';return;}if(fresh.reason){feedback.textContent=fresh.reason;return;}
        busy=true;try{feedback.textContent='';for(const command of fresh.commands){const result=dispatch(session,command);feedback.textContent=result.reason||`${fresh.label} applied.`;if(!result.ok)break;}key='';updateProject();}finally{window.setTimeout(()=>{busy=false;},350);}};
    });
  }
  for(const d of [journal,recipes])d.addEventListener('keydown',e=>{e.stopPropagation();if(e.key==='Escape'){d.open=false;document.querySelector('canvas')?.focus();}});
  return {update(){updateProject();updateRecipe();},
    openDiscoveries(){journal.open=true;journal.scrollIntoView({block:'nearest'});},
    openItem(item:Item='steel'){select.value=item;recipes.open=true;updateRecipe();recipes.scrollIntoView({block:'nearest'});select.focus();},
    close(){journal.open=false;recipes.open=false;}};
}
