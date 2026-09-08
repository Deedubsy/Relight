import { campaignDiscoveries, itemGuide, ITEMS, type Item, type Command, type DiscoveryInfo } from '@relight/sim';
import type { Session } from './session';

const node=<K extends keyof HTMLElementTagNameMap>(tag:K,text=''):HTMLElementTagNameMap[K]=>{const n=document.createElement(tag);n.textContent=text;return n;};
/** Keep controls and focus stable while the simulation updates their current facts. */
export function createCampaignGuide(session:Session,root:HTMLElement,send:(c:Command)=>void){
  const journal=node('details'),summary=node('summary','Discoveries');journal.className='campaign-guide';journal.open=true;
  const intro=node('p','Explore for nearby clues. Only known places appear here; recruiting and restoring require a local visit.');
  const list=node('div');journal.append(summary,intro,list);root.append(journal);
  const cards=new Map<string,{root:HTMLElement;title:HTMLElement;status:HTMLElement;detail:HTMLElement;needs:HTMLElement;actions:HTMLElement;key:string}>();
  const recipes=node('details'),recipeSummary=node('summary','Items and recipes');recipes.className='campaign-guide item-guide';
  const label=node('label','Item '),select=node('select');select.setAttribute('aria-label','Item guide');
  for(const item of ITEMS){const o=node('option',item==='magazine'?'Shot magazine':item[0].toUpperCase()+item.slice(1));o.value=item;select.append(o);}label.append(select);
  const body=node('div');body.className='item-guide-body';recipes.append(recipeSummary,label,body,node('p','Blueprints and copy-paste arrive with the Foreman in Phase 8. They are not available in this build.'));root.append(recipes);
  let recipeKey='';
  function updateRecipe(){if(!recipes.open)return;const info=itemGuide(session.state,select.value as Item);if(!info)return;const key=JSON.stringify(info);if(key===recipeKey)return;recipeKey=key;body.replaceChildren(node('h3',info.title));
    body.append(node('h4','How to obtain'));
    for(const source of info.sources)body.append(node('p',source));
    for(const r of info.recipes){const card=node('article');card.append(node('strong',`${r.name} · ${r.available?'Available':'Locked'}`),node('p',`${r.inputs.map(i=>`${i.count} ${i.item}`).join(' + ')} → ${r.count} ${info.item} every ${r.seconds}s`),node('p',`${r.machine} · ${r.kw} kW · nominal ${r.count*60/r.seconds}/min. Supply, power and output space limit actual production.`),node('p',r.provenance));body.append(card);}
    body.append(node('h4','Used by'));const uses=node('ul');for(const use of info.uses)uses.append(node('li',use));body.append(uses,node('h4','Move and store'),node('p',info.transport));
  }
  select.onchange=updateRecipe;recipes.ontoggle=updateRecipe;
  function updateCard(row:DiscoveryInfo){let c=cards.get(row.id);if(!c){c={root:node('article'),title:node('h3'),status:node('strong'),detail:node('p'),needs:node('p'),actions:node('div'),key:''};c.root.dataset.discovery=row.id;c.root.append(c.title,c.status,c.detail,c.needs,c.actions);list.append(c.root);cards.set(row.id,c);}
    const key=JSON.stringify(row);if(key===c.key)return;c.key=key;c.title.textContent=row.title;c.status.textContent=`${row.status} · tile ${row.x}, ${row.y}`;c.detail.textContent=row.detail;
    c.needs.hidden=!row.needs.length;c.needs.textContent=row.needs.map(n=>`${n.item}: ${n.delivered}/${n.required} delivered (${Math.max(0,n.required-n.delivered)} remaining)`).join(' · ');
    while(c.actions.children.length>row.actions.length)c.actions.lastChild!.remove();
    row.actions.forEach((a,i)=>{let b=c!.actions.children[i] as HTMLButtonElement|undefined;if(!b){b=node('button');c!.actions.append(b);}b.textContent=a.label;b.disabled=!!a.reason;b.title=a.reason;b.onclick=()=>{for(const command of a.commands)send(command);};});
  }
  for(const d of [journal,recipes])d.addEventListener('keydown',e=>{e.stopPropagation();if(e.key==='Escape'){d.open=false;document.querySelector('canvas')?.focus();}});
  return {update(){const rows=campaignDiscoveries(session.state),ids=new Set(rows.map(r=>r.id));for(const [id,c] of cards)if(!ids.has(id)){c.root.remove();cards.delete(id);}for(const row of rows)updateCard(row);updateRecipe();},
    openDiscoveries(){journal.open=true;journal.scrollIntoView({block:'nearest'});},
    openItem(item:Item='steel'){select.value=item;recipes.open=true;updateRecipe();recipes.scrollIntoView({block:'nearest'});select.focus();},
    close(){journal.open=false;recipes.open=false;}};
}
