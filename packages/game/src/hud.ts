import {campaignGrid,CAMPAIGN_RULES} from '@relight/sim';
import {itemName} from '@relight/sim';
import {equipmentLabel,handLocked,HAND_LOCK_TEXT} from '@relight/sim';
import {preferenceView,saveUiPreferences} from './uiPreferences';
import {campaignOutages,baseCore,RIVERFRONT,fixedStops,machineRunning,blockName,blockOfTile,campaignClock,campaignAlerts,currentGoal,pocketUsed,invCap,ENGINEER_HP,ROUNDS_PER_MAG,KIND_LABEL,navigationTargets,ground,T_RIVER,T_STREET,campaignDiscoveries,type NextAction} from '@relight/sim';
import {el,uiInput} from './uiShell';
import {shortcut} from './controls';
import {hudInset,objectiveView,inspectionView,type View} from './view';
import type {Session} from './session';
import type {Panel} from './panel';
import type {WorldScene} from './worldScene';
/** One passive read model over the existing simulation; no second scene or exploration map. */
export function createHud(session:Session,panel:Panel,world:WorldScene,view:View,toggleMap:()=>void,threatControls:HTMLElement){
  const goal=el('section','ui-hud hud-goal'),place=el('div','hud-place'),title=el('h2'),next=el('p'),details=el('details'),summary=el('summary',undefined,'Why this next?'),detail=el('p');
  const locate=el('button',undefined,'Show location'),back=el('button',undefined,`Return to engineer (${shortcut('engineer')})`);
  const resources=el('ul','hud-goal-resources');resources.setAttribute('aria-label','Required resources in Backpack');let resourcesKey='';
  details.append(summary,detail);details.append(locate);goal.append(place,title,next,resources,details,back);goal.setAttribute('aria-label','Current objective');
  const time=el('section','ui-hud hud-time'),clock=el('strong'),warning=el('div','hud-warning');
  time.setAttribute('aria-label','Time and defence');const outages=el('div','hud-outages');outages.setAttribute('aria-label','Power outages');const power=el('div','hud-power'),statusStrip=el('section','ui-hud hud-status-strip');statusStrip.append(clock,power);time.append(warning,outages,threatControls);
  const pockets=el('section','ui-hud hud-engineer'),health=el('strong'),equipment=el('div'),stock=el('div');
  pockets.setAttribute('aria-label','Engineer');pockets.append(health,equipment,stock);
  const map=el('section','ui-hud hud-minimap'),fold=el('button',undefined,'−'),mapButton=el('button',undefined,`Map (${shortcut('map')})`),canvas=el('canvas'),legend=el('div','hud-map-legend','● You  □ Known  ◆ Pin');
  fold.setAttribute('aria-label','Toggle local minimap');
  canvas.width=180;canvas.height=140;canvas.setAttribute('role','img');canvas.setAttribute('aria-label','Known destinations and engineer');
  map.append(fold,mapButton,canvas,legend);mapButton.onclick=toggleMap;
  let expanded=true;const collapse=()=>{canvas.hidden=legend.hidden=!expanded;fold.setAttribute('aria-expanded',String(expanded));};fold.onclick=()=>{expanded=!expanded;collapse();};collapse();
  const prompt=el('div','hud-prompt');prompt.setAttribute('aria-label','Interaction');
  const mining=el('div','hud-prompt hud-mining'),mineTitle=el('strong'),mineDetail=el('div'),mineProgress=el('progress');
  mining.setAttribute('aria-label','Manual mining');mineDetail.setAttribute('role','status');mineProgress.max=1;mineProgress.setAttribute('aria-label','Next mined item');mining.append(mineTitle,mineProgress,mineDetail);mining.hidden=true;
  const identity=el('div','hud-identity','You');identity.setAttribute('aria-label','Your engineer');
  const hint=el('aside','ui-hud hud-opening-hint'),dismiss=el('button',undefined,'Dismiss hint');
  hint.append(el('span',undefined,`${shortcut('interact')} interacts · WASD moves · Build opens with ${shortcut('build')}. Find all controls in Help.`),dismiss);dismiss.onclick=()=>{hint.hidden=true;saveUiPreferences({dismissedHints:['opening']});};
  window.addEventListener('relight:preferences',()=>{hint.hidden=preferenceView.value.dismissedHints.includes('opening');});
  const track=el('section'),label=el('label',undefined,'Track one objective'),select=el('select');select.id='tracked-objective';label.htmlFor=select.id;track.append(label,select);
  document.querySelector('[data-adapter="projects"]')!.prepend(track);
  select.onchange=()=>{objectiveView.targetId=select.value==='auto'?undefined:select.value==='none'?null:select.value;};
  const panelAlert=el('button','panel-alert','Alert');panelAlert.hidden=true;panelAlert.onclick=()=>panel.shell.open('projects');document.querySelector('.ui-drawer-heading')!.append(panelAlert);
  let action:NextAction|undefined,last=-Infinity,optionsKey='';
  locate.onclick=()=>{if(!action?.location)return;if(view.mode==='map')toggleMap();world.viewLocation(action.location.x,action.location.y);document.querySelector<HTMLCanvasElement>('#map > canvas')?.focus();panel.shell.sync();};
  back.onclick=()=>{if(view.mode==='map')toggleMap();world.returnToEngineer();document.querySelector<HTMLCanvasElement>('#map > canvas')?.focus();panel.shell.sync();};
  goal.append(hint);
  for(const node of [goal,time,pockets,map,statusStrip]){document.getElementById('app')!.append(node);panel.shell.protect(node);}document.getElementById('app')!.append(prompt,identity,mining);
  const mapKey=el('details','ui-hud full-map-legend');mapKey.append(el('summary',undefined,'Map & world symbols'),el('p',undefined,'Mint arrow: you · H: Home · P: plant · T: permanent tram stop. Red crossed lightning: power outage. Plant labels show ON, OFF or DAMAGED; stop overlays show power. Purple circle: broad core search, not the exact relay. Amber dot: current goal. Teal doorway: accessible · crossed boards: background · person marker: known survivor · red triangle: known hostile installation. Map clicks inspect; they never walk.'));document.getElementById('app')!.append(mapKey);panel.shell.protect(mapKey);
  const G=ground(session.state);
  const put=(node:HTMLElement,text:string)=>{if(node.textContent!==text)node.textContent=text;};
  return {update(now:number){
    if(now-last<150)return;last=now;mapKey.hidden=view.mode!=='map'||panel.shell.paused()||!!panel.shell.active();goal.hidden=view.mode==='map';const st=session.state,e=st.engineer,c=campaignClock(st),tool=world.selectedTool(),targets=navigationTargets(st),discoveries=campaignDiscoveries(st);
    if(objectiveView.targetId&&!discoveries.some(s=>s.id===objectiveView.targetId))objectiveView.targetId=undefined;
    action=currentGoal(st,objectiveView.targetId).next;
    const bi=blockOfTile(st,Math.floor(e.x),Math.floor(e.y));put(place,bi>=0?blockName(st,bi):'City streets');
    const tracked=objectiveView.targetId!==null||e.down>=0;
    put(title,tracked?action?.title??'Explore':'No tracked objective');put(next,tracked?action?.text??'Choose a known destination':'Track a known objective in Projects.');put(detail,action?.detail??'');details.hidden=!tracked;locate.hidden=!tracked||!action?.location;back.hidden=!world.viewingThreat;
    const needs=tracked?action?.resources??[]:[],needsKey=JSON.stringify(needs);resources.hidden=!needs.length;
    if(needsKey!==resourcesKey){resourcesKey=needsKey;resources.replaceChildren(...needs.map(n=>{const row=el('li',n.available>=n.required?'ready':undefined,`${(KIND_LABEL as Record<string,string>)[n.item]??itemName(n.item)} — ${n.available}/${n.required}`);row.title='Available in your Backpack';return row;}));}
    const key=discoveries.map(s=>s.id+s.title).join('|');if(optionsKey!==key){optionsKey=key;select.replaceChildren(...[{id:'auto',title:'Automatic next action'},{id:'none',title:'Untrack'},...discoveries].map(s=>{const o=el('option',undefined,s.title);o.value=s.id;return o;}));}select.value=objectiveView.targetId===undefined?'auto':objectiveView.targetId===null?'none':objectiveView.targetId;
    const minutes=Math.floor(c.elapsed/CAMPAIGN_RULES.daySeconds*1440);put(clock,`Day ${c.day} · ${String(Math.floor(minutes/60)).padStart(2,'0')}:${String(minutes%60).padStart(2,'0')}${st.speed===0?' · Paused':''}`);
    if(st.campaign&&st.flow){const grid=campaignGrid(st),near=st.flow.machines.filter(m=>grid.machines.has(m.id)).sort((a,b)=>Math.hypot(a.x-e.x,a.y-e.y)-Math.hypot(b.x-e.x,b.y-e.y))[0],selectedNet=inspectionView.machineId===null?undefined:grid.machines.get(inspectionView.machineId),net=inspectionView.machineId!==null?selectedNet:(near&&Math.hypot(near.x-e.x,near.y-e.y)<=12?grid.machines.get(near.id):grid.blocks[bi]);
     // GP-POWER-FIX (2026-09-11): a block circuit with no source yet reads 0 / 0 kW, not the core's unmet demand against nothing.
     const unsourced=!!net&&inspectionView.machineId===null&&net.rated<=0&&net.supply<=0;
     put(power,net&&!unsourced?`${net.name}: ${Math.round(net.demand)} / ${Math.round(net.supply)} kW`:unsourced?'Power: 0 / 0 kW · no Generator linked':'Power: disconnected');power.title=net&&!unsourced?`Requested / available · delivered ${Math.round(net.load)} kW · installed ${net.rated} kW. Select a machine to inspect its own network.`:unsourced?`No source reaches this block yet. Build a Generator, fuel it and chain Poles to the substation; the block core still needs ${Math.round(net!.demand)} kW.`:'Connect a pole to a fuelled generator.';}
    document.documentElement.style.setProperty('--hud-status-height',`${statusStrip.offsetHeight}px`);
    const lostPower=campaignOutages(st);outages.hidden=!lostPower.length;put(outages,lostPower.map(a=>'⚡× '+a.title).join(' · '));outages.title=lostPower.map(a=>a.detail).join(' · ');
    const alerts=campaignAlerts(st),urgent=alerts.find(a=>!a.id.startsWith('outage:')),shade=alerts.find(a=>a.id.startsWith('shade:')); put(warning,urgent?`${urgent.priority>=90?'⚠':'◷'} ${urgent.title}${urgent.id==='schedule'||urgent.title==='Alien relay draining health'?' · '+urgent.detail:''}${shade&&shade!==urgent?' · Shades need powered lighting.':''}`:'');warning.title=urgent?.detail??'';time.classList.toggle('urgent',!!urgent&&urgent.priority>=60);time.classList.toggle('danger',!!urgent&&urgent.priority>=90);
    panelAlert.hidden=!panel.shell.active()||(!lostPower.length&&(!urgent||urgent.priority<90));panelAlert.textContent=lostPower.length?lostPower.map(a=>'⚡× '+a.title).join(' · '):'Alert';panelAlert.title=urgent?`${urgent.title} · ${urgent.detail}`:'';panelAlert.setAttribute('aria-label',urgent?`${urgent.title}. Open alert inbox`:'Alerts');
    document.body.classList.toggle('hud-urgent',!!urgent&&urgent.priority>=60);
    identity.hidden=view.mode!=='world'||e.walked>=2||e.down>=0||!!e.truckSeat||!!panel.shell.active()||panel.shell.paused()||tool.tool!=='hand';
    if(!identity.hidden){const [x,y]=world.screenOf(e.x,e.y);identity.style.left=`${x}px`;identity.style.top=`${y+20}px`;}
    mapButton.textContent=`Map (${shortcut('map')})`;back.textContent=`Return to engineer (${shortcut('engineer')})`;
    (hint.firstChild as HTMLElement).textContent=`${shortcut('interact')} interacts · ${shortcut('north')}/${shortcut('west')}/${shortcut('south')}/${shortcut('east')} moves · ${shortcut('build')} opens Build. Hold left-click to mine.`;
    put(health,e.down>=0?`Engineer down · ${Math.ceil(Math.max(0,e.down-st.t))} s`:`Engineer · ${Math.ceil(e.hp)}/${ENGINEER_HP} HP`);
    put(equipment,e.truckSeat?'Driving truck':tool.tool==='hand'?'':tool.tool==='rifle'?equipmentLabel(st):KIND_LABEL[tool.tool]);put(stock,`Backpack ${pocketUsed(e)}/${invCap(e)}`);
    const interaction=view.mode==='world'&&!uiInput.blocked&&tool.tool==='hand'&&!tool.mode?world.interaction():null;
    put(prompt,(tool.tool!=='hand'||tool.mode)&&view.mode==='world'&&!uiInput.blocked?(tool.reason||`${tool.tool==='rifle'?'Hold left-click to fire':`${shortcut('rotate')} rotates`} · Escape cancels`):interaction?`${shortcut('interact')} · ${interaction.label}${interaction.reason?` · ${interaction.reason}`:''}`:'');prompt.hidden=!prompt.textContent;
    if(tool.ports&&tool.tool==='hand'&&view.mode==='world'&&!uiInput.blocked){put(prompt,`Cyan arrows: IN · Amber arrows: OUT · ${shortcut('inspect')} inspects connections`);prompt.hidden=false;}
    if(handLocked(st)&&view.mode==='world'){put(prompt,`${HAND_LOCK_TEXT} · Escape cancels`);prompt.hidden=false;}   // GP-PLAYTEST-FIX 4
    const mine=view.mode==='world'&&!uiInput.blocked?world.miningFeedback():null;mining.hidden=!mine;
    if(mine){put(mineTitle,mine.title);put(mineDetail,mine.detail);mineProgress.hidden=mine.progress===null;mineProgress.value=mine.progress??0;prompt.hidden=true;identity.hidden=true;}
    const placing=!!tool.point&&!!tool.reason&&(!!tool.mode||(tool.tool!=='hand'&&tool.tool!=='rifle'));prompt.classList.toggle('placement-prompt',placing);
    if(placing){prompt.style.left=`${Math.max(12,Math.min(window.innerWidth-344,tool.point![0]+24))}px`;prompt.style.top=`${Math.max(12,Math.min(window.innerHeight-hudInset.bottom-prompt.offsetHeight-12,tool.point![1]+24))}px`;}else{prompt.style.removeProperty('left');prompt.style.removeProperty('top');}
    hint.hidden=preferenceView.value.dismissedHints.includes('opening')||(e.walked>=2&&(st.flow?.stats.handMined??0)>0)||tool.tool!=='hand'||!!panel.shell.active()||view.mode==='map';
    map.hidden=!!panel.shell.active()||panel.shell.paused()||view.mode==='map';pockets.hidden=view.mode==='map'||!!panel.shell.active();goal.inert=pockets.inert=panel.shell.paused();hint.hidden=hint.hidden||panel.shell.paused();
    const paint=canvas.getContext('2d')!;if(expanded&&!map.hidden){
     const scale=2,ox=90-e.x*scale,oy=70-e.y*scale;
     paint.fillStyle='#101b1e';paint.fillRect(0,0,180,140);
     for(let py=0;py<140;py+=2)for(let px=0;px<180;px+=2){const x=Math.floor((px-ox)/scale),y=Math.floor((py-oy)/scale);if(x<0||y<0||x>=G.tw||y>=G.th)continue;const tile=G.base[y*G.tw+x];paint.fillStyle=tile===T_RIVER?'#28465b':tile===T_STREET?'#64716d':'#273538';paint.fillRect(px,py,2,2);}
     if(st.city?.mapId){paint.fillStyle='#928c76';for(const b of G.urban?.structures??[])paint.fillRect(ox+b.x*scale,oy+b.y*scale,b.w*scale,b.h*scale);paint.strokeStyle='#d9a34b';paint.lineWidth=1;paint.beginPath();st.campaign?.fixedTram?.route.forEach((t,i)=>{const x=ox+(t%G.tw)*scale,y=oy+Math.floor(t/G.tw)*scale;if(i)paint.lineTo(x,y);else paint.moveTo(x,y);});paint.stroke();}
     for(const t of targets){const site=st.campaign?.progression?.sites.find(s=>'site:'+s.id===t.id);if(st.city?.mapId&&(t.kind==='district'||site?.recovered||site?.kind==='plant'))continue;const x=ox+t.x*scale,y=oy+t.y*scale;if(site?.kind==='core'&&!site.seen){paint.strokeStyle='#c99bfa';paint.beginPath();paint.arc(ox+site.searchX*scale,oy+site.searchY*scale,28*scale,0,Math.PI*2);paint.stroke();continue;}if(x<3||x>177||y<3||y>137)continue;paint.strokeStyle=t.kind==='pin'?'#e3b96f':'#b3b7aa';paint.strokeRect(x-3,y-3,6,6);}
     if(st.city?.mapId){paint.font='bold 10px sans-serif';paint.textAlign='center';const mark=(x:number,y:number,label:string,on:boolean)=>{x=ox+x*scale;y=oy+y*scale;if(x<8||x>172||y<8||y>132)return;paint.fillStyle='#102326';paint.fillRect(x-9,y-8,18,15);paint.strokeStyle=on?'#8ae6c1':'#ccad76';paint.strokeRect(x-9,y-8,18,15);paint.fillStyle='#ffffff';paint.fillText(label,x,y+3);};mark(RIVERFRONT.homeOrigin[0]+12,RIVERFRONT.homeOrigin[1]+15,lostPower.some(a=>a.id===`outage:${st.campaign?.homeBlock}`)?'⚡×':'H',!lostPower.some(a=>a.id===`outage:${st.campaign?.homeBlock}`));for(const [i,p]of RIVERFRONT.plants.entries()){const site=st.campaign?.progression?.sites.find(s=>s.id===p.id);mark(p.x,p.y,'P'+(i+1),!!site?.enabled&&!!site?.installed&&baseCore(st,site.block)?.hp!==0);}for(const [i,p]of fixedStops(st).entries())mark(p.x,p.y,'T'+(i+1),machineRunning(st,p));}

     for(const a of lostPower)if(a.location){const x=ox+a.location.x*scale,y=oy+a.location.y*scale;if(x>=8&&x<=172&&y>=8&&y<=132){paint.fillStyle='#ff8b64';paint.font='bold 14px sans-serif';paint.fillText('⚡×',x,y);}}
     if(action?.location){const x=Math.max(6,Math.min(174,ox+action.location.x*scale)),y=Math.max(6,Math.min(134,oy+action.location.y*scale));paint.fillStyle='#efb85e';paint.beginPath();paint.arc(x,y,4,0,Math.PI*2);paint.fill();}
     paint.fillStyle='#0b171c';paint.strokeStyle='#67ffcf';paint.lineWidth=2;paint.beginPath();paint.arc(90,70,11,0,Math.PI*2);paint.fill();paint.stroke();paint.save();paint.translate(90,70);paint.rotate(Math.atan2(e.face[1],e.face[0]));paint.fillStyle='#ffffff';paint.strokeStyle='#101b1e';paint.beginPath();paint.moveTo(7,0);paint.lineTo(-5,-5);paint.lineTo(-3,0);paint.lineTo(-5,5);paint.closePath();paint.fill();paint.stroke();paint.restore();canvas.setAttribute('aria-label','Local neighbourhood, engineer direction and known destination');
    }
    const dock=document.querySelector<HTMLElement>('.ui-navigation')!.getBoundingClientRect(),condition=pockets.getBoundingClientRect();pockets.style.bottom=condition.right+12>dock.left?`${dock.height+24}px`:'20px';
    time.style.top=`${map.getBoundingClientRect().bottom+6}px`;
    time.hidden=!!panel.shell.active()||panel.shell.paused();goal.hidden=view.mode==='map'||panel.shell.paused()||panel.shell.active()==='inventory';
    document.documentElement.style.setProperty('--ui-clock-height',`${time.offsetHeight}px`);
    if(panel.shell.active())details.open=false;
    hudInset.top=Math.max(time.getBoundingClientRect().bottom,goal.getBoundingClientRect().bottom)+12;document.documentElement.style.setProperty('--ui-top-inset',`${time.offsetHeight+24}px`);
  }};
}
