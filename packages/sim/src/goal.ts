import {RIFLE} from './equipment';
import {fabricationBlocker} from './fabrication';
import {itemName} from './itemNames';
import {conveyorDestinations} from './directConveyor';
import {DX,DY} from './flow';
import {cityApproach} from './authoredCity';
import {fixedPoweredStops,fixedTramStatus} from './fixedTram';
import { routingDescription, routingStatus } from './routing';
import { campaignDiscoveries } from './campaignGuide';
import { campaignGrid, CAMPAIGN_POWER } from './campaignPower';
import { campaignWarning } from './campaignThreat';
import { DEFENCE,repairCheck,defenceMax, defenceHp, coreDisabledAt } from './campaignDefence';
/** RI-02 — the current-goal line (D-GB-2 (a), constitution rule 8's form; plan §11.2 "prominent current goal with
 *  the reason it matters"). One line, read off the sim's own state every time it is asked: the next §11 constraint
 *  the player has not met, with the numbers that make it matter. No list, no screens, no quest state — the goal
 *  is a pure function of the state, so a save reloads to the same line and a replay never disagrees with it.
 *
 *  Implementation default (RI-02, reversible; not a historical approval): the goals follow §11's rows
 *  (constants.ts HOUR) in schedule order, each skipped once the state meets it, and each direction's claim has a
 *  "hold" form while the block is Contested and a "retake" form after it fell. A second, amber line — the support —
 *  names an immediate shortage (down, no coal, an empty street, a brownout) without replacing the goal (§11.2:
 *  "immediate shortages support that goal rather than constantly replacing it"). `machineStatus` is the readable
 *  running / starved / blocked / idle / off word of the same section. E-hour samples `currentGoal` at 1 Hz through
 *  the logged hour and checks every id against the state it claims (packages/harness goalcheck.ts). */
import { SimState, HELD, DARK, CONTESTED, Edge } from './types';
import { isCampaign } from './rules';
import {
  chestCount, depotRect, Machine, MACHINE_COST, machineKw, SHOT, ASM_OUTPUT_CAP, throttle, rubbleAt, machineAt, accepts,
  recipeOf, recipeOutput, subPowered, poleGrid, inputTile, outputTile, findRubble, asmCanStart, costStr, Item,
  isFieldKind, fieldBlock, powered, invTotal, tramAt, tramRoute, nextOf, BELT_SPACING, inserterPickup,
} from './flow';
import { ground, hqLot, blockOfTile } from './ground';
import { HQ_PATCHES, P_COAL, P_COPPER, P_STEEL, RAIL_YARD_COAL } from './tiles';
import { hqIdx } from './engineer';
import { burnOffS, heldCount, rotOf } from './sim';
import { claimInfo, ammoStatus, edgeCap } from './queries';
import { COAL_MJ, GENERATOR_KW, TURRET_HOPPER, TURRET_RANGE, TURRET_KW } from './recipes';
import { openingEncounter, liveTurrets, turretCapacity, turretSupplier, supplyChainReaches, OPENING_ENCOUNTER } from './openingEncounter';
import { EXCAVATOR_PER_S } from './constants';
import { HOUR_MINE_STEEL, HOUR_CRAFT_MAGS, HOUR_END } from './hour';
import { blockName, hqNeighbourToward, streetName } from './names';
import { heartAt, describeHeart } from './heart';   // RI-06

export type GoalDir = 'east' | 'west' | 'north';
export type GoalId =
  | 'home-factory' | 'home-explore'
  | 'hq-fell' | 'front' | 'mine' | 'craft' | 'coal-line' | 'gen-2' | 'steel-line' | 'copper-line' | 'shot-line' | 'steel-2' | 'gen-3'
  | 'claim-east' | 'contest-east' | 'retake-east' | 'claim-west' | 'contest-west' | 'retake-west' | 'rail-coal'
  | 'gen-4' | 'copper-2' | 'assembler-3' | 'claim-north' | 'contest-north' | 'retake-north' | 'hold';
export type SupportId = 'campaign-threat' | 'down' | 'gen-dry' | 'kit' | 'feed' | 'brownout';
export type LegacyGoalId = Exclude<GoalId, 'home-factory' | 'home-explore'>;
/** §11's row order: the goal ids a state can show, first to last. */
export const GOAL_ORDER: readonly LegacyGoalId[] = [
  'hq-fell', 'front', 'mine', 'craft', 'coal-line', 'gen-2', 'steel-line', 'copper-line', 'shot-line', 'steel-2', 'gen-3',
  'claim-east', 'contest-east', 'retake-east', 'claim-west', 'contest-west', 'retake-west', 'rail-coal',
  'gen-4', 'copper-2', 'assembler-3', 'claim-north', 'contest-north', 'retake-north', 'hold',
];

export interface GoalLine {
  id: GoalId | SupportId;
  /** What to do, one line. */
  text: string;
  /** Why it matters now, with the state's numbers. */
  why: string;
  /** The block the line is about (a claim, a street), for the map's marker; absent when it is about the HQ lot. */
  block?: number;
}
export function turretRecommendation(st:SimState){
 const count=machines(st).filter(m=>m.kind==='turret'&&defenceHp(m)>0).length,more=Math.max(0,3-count),word=['no','one','two','three'][more];
 return {count,complete:count>=3,text:`Larger attacks can approach from any direction. Build ${word} more turret${more===1?'':'s'} and spread your defences around the base. Enemies can attack from any direction, so cover different approaches.`,detail:'Three is a starting recommendation, not guaranteed protection. Keep turrets supplied, cover different approaches and support them with your Rifle. Existing turrets count; disabled turrets need repair.'};
}
export interface NextAction {
  id: string; title: string; text: string; detail: string;
  resources?: {item:string;available:number;required:number}[];
  location?: {x:number;y:number}; shortage?: {item:'steel'|'copper';count:number;home:number}[];
}
export interface Goal { goal: GoalLine; support: GoalLine | null; next?: NextAction }

/** Presentation derived only from saved facts. No completion flags or commands. */
function campaignNext(st:SimState, tracked?:string|null):NextAction {
  const e=st.engineer, home=depotRect(st), location={x:home.x+home.size/2,y:home.y+home.size/2};
  if(e.down>=0)return {id:'recovery',title:'Recover at Home',text:`Back on your feet in ${Math.ceil(Math.max(0,e.down-st.t))} s`,detail:'Movement and interaction resume after recovery.',location};
  const damagedHome=st.campaign?.defence?.bases.find(b=>b.block===st.campaign!.homeBlock&&b.hp===0);
  if(damagedHome)return {id:'home-recovery',title:'Restore Home power',text:repairCheck(st,damagedHome.x,damagedHome.y)||'Aim at the Home core and press E to repair',detail:`Home is disabled. Carry ${DEFENCE.coreSteel} Steel plates and ${DEFENCE.coreCopper} Copper; after attackers leave, repair the core to restart existing machines. Hand mining still works.`,location:{x:damagedHome.x,y:damagedHome.y}};
  const selected=tracked?campaignDiscoveries(st).find(s=>s.id===tracked):undefined;
  if(selected){const need=selected.needs.find(n=>n.delivered<n.required);return {id:selected.id,title:selected.title,text:need?`Deliver ${need.required-need.delivered} ${itemName(need.item)}`:selected.status,detail:selected.detail,location:cityApproach(st,selected.x+selected.size/2,selected.y+selected.size/2)};}
  if(st.campaign?.progression?.passenger)return {id:'tram-passenger',title:'Riding the tram',text:'E to disembark safely',detail:fixedTramStatus(st)};
  if(['core1','core2','core3'].some(k=>(e.inv[k]??0)>0)){
    const plant=st.campaign?.progression?.sites.find(s=>s.kind==='plant'&&!s.installed);
    if(plant)return {id:plant.id,title:'Choose a regional plant',text:'Deliver preparation materials and install your carried core',detail:'Any compatible uncommissioned plant accepts your core. Inspect its material requirements before activation.',location:cityApproach(st,plant.x,plant.y)};
  }
  const all=machines(st), production=all.filter(m=>['excavator','pumpjack','assembler','assembler2','mixer','foundry','refinery'].includes(m.kind));
  const resources=(price:Readonly<Record<string,number>>)=>Object.entries(price).filter(([,n])=>n>0).map(([item,required])=>({item,required,available:Math.floor(e.inv[item]??0)}));
  const buildStep=(kind:import('./flow').Kind,title:string,instruction:string):NextAction=>{
    const price=MACHINE_COST[kind],packed=(e.inv[kind]??0)>=1,shortage=(['steel','copper'] as const).map(item=>({item,count:packed?0:Math.max(0,price[item]-(e.inv[item]??0)),home:chestCount(st,item)})).filter(s=>s.count>0);
    return {id:'opening-workshop',title,text:instruction,resources:resources(packed?{[kind]:1}:price),detail:`${shortage.length?'Still need '+shortage.map(s=>`${s.count} ${itemName(s.item)} (${Math.min(s.count,s.home)} available at Home)`).join(' + ')+'. Hold left-click on salvage to gather, or collect stored supplies.':'Materials ready in Backpack. Open Build to place it.'} Opening order: 1 Generator → 1 Excavator → 1 Supply chest → Belts into the chest.`,location,shortage};
  };
  const gen=all.find(m=>m.kind==='generator');
  if(!gen)return buildStep('generator','1 · Build a Generator','Place 1 Generator in Founders Court');
  const excavator=all.find(m=>m.kind==='excavator');
  if(!excavator)return buildStep('excavator','2 · Build an Excavator','Place 1 Excavator at a resource patch edge, with a clear side for belts');
  const stores=all.filter(m=>m.kind==='chest');
  if(!stores.length)return buildStep('chest','3 · Build storage','Place 1 Supply chest near the Excavator');
  const destinations=conveyorDestinations(st);
  const receiversOf=(source:Machine)=>[...new Map(all.filter(b=>machineAt(st,b.x-DX[b.dir],b.y-DY[b.dir])?.id===source.id).flatMap(b=>destinations.get(b.id)??[]).map(m=>[m.id,m])).values()];
  if(!receiversOf(excavator).some(m=>m.kind==='chest'))return {id:'opening-workshop',title:'4 · Connect belts to storage',text:'Connect Excavator → Supply chest (resources per Belt)',resources:resources((e.inv.belt??0)>0?{belt:1}:MACHINE_COST.belt),detail:'Use at least 1 Belt; the total depends on the gap. Point its arrow away from the Excavator and into the chest. R rotates. Clear salvage along the route; inserters are not needed.',location:{x:excavator.x,y:excavator.y}};
  if((gen.inv.coal??0)+(gen.inv.fuel??0)<=0)return {id:'opening-workshop',title:'Fuel your Generator',text:'Gather Coal, then open the Generator inventory',resources:resources({coal:1}),detail:'Select Coal in your Backpack, enter the quantity and Load. One Coal starts generation; keep a reserve or connect a fuel supply. The Generator inventory shows remaining fuel.',location:{x:gen.x,y:gen.y}};
  if(!powered(st,excavator))return {id:'opening-power',title:'Connect your extraction power',text:'Place Poles between your Generator and Excavator',resources:resources((e.inv.pole??0)>0?{pole:1}:MACHINE_COST.pole),detail:'Poles connect by visible cables within 8 tiles (Big poles: 12). Machines must be within coverage of a connected Pole or Substation. Belts need no power. Inspect the Excavator for its own network supply.',location:{x:excavator.x,y:excavator.y}};
  const excavatorStatus=machineStatus(st,excavator);
  if(excavatorStatus.state!=='running'&&!excavator.hold)return {id:'opening-workshop',title:'Start your extraction line',text:excavatorStatus.reason,detail:'Inspect the Excavator to check its resource, power and output connection.',location:{x:excavator.x,y:excavator.y}};
  // GP-POWER-FIX (2026-09-11): the Home core and streetlights draw from the block substation; the opening now asks for that cable.
  const homeSub=ground(st).blocks[st.campaign!.homeBlock]?.sub;
  if(homeSub&&campaignGrid(st).blocks[st.campaign!.homeBlock].supply<=0)return {id:'opening-power',title:'Connect Founders Court’s substation',text:'Chain Poles from your network to the Founders Court substation',resources:resources((e.inv.pole??0)>0?{pole:1}:MACHINE_COST.pole),detail:`The Home core draws ${CAMPAIGN_POWER.coreKw} kW and the streetlights run from the block substation, marked here. Poles link within 8 tiles of another Pole, a Generator or the substation footprint; a cable appears once linked and the placement preview shows purple lines to everything in reach. Until it is connected the base has no power.`,location:{x:homeSub.x+homeSub.size/2,y:homeSub.y+homeSub.size/2}};
  // GP-OPENING (2026-09-11): Rifle → ammunition → one prepared turret → introductory attack → automated resupply →
  // three turrets → the first nearby exploration objective. Every branch is read from saved facts (rule 8).
  if(e.equipment&&!Object.keys(e.equipment.weapons).length)return {id:'opening-rifle',title:'Prepare your expedition Rifle',text:`Craft a Rifle at Home workshop · ${RIFLE.seconds} s`,resources:resources({steel:RIFLE.steel,copper:RIFLE.copper}),detail:'Craft Rifle using carried supplies, select it in your Backpack, Equip in slot 1, then select weapon tool 9. Keep bullets for the expedition as well as turret defence.',location};
  if(e.equipment&&!e.equipment.slots.some(Boolean))return {id:'opening-equip',title:'Equip your crafted Rifle',text:'Open Backpack, select Rifle, then Equip in slot 1',detail:'Equipment slots are separate from construction shortcuts. Loaded rounds and cooldown stay with each weapon.',location};
  const opening=openingEncounter(st),turrets=liveTurrets(st),status=opening?.status??'skipped';
  const loadedText=(m:Machine)=>`Loaded ${Math.floor(m.inv.rounds??0)} / ${turretCapacity(m)} bullets`;
  const firstTurret=turrets.find(m=>(m.inv.rounds??0)>0)??turrets[0];
  if(status==='pending'){
    if(!firstTurret&&(e.inv.magazine??0)<1)return {id:'opening-workshop',title:'Make turret ammunition',text:'Open Home Workshop → Craft 10 bullets',resources:resources(SHOT.inputs),detail:`Make 10 bullets (${SHOT.seconds} seconds). Crafting is at the top of Home storage / Backpack when near Home. Stay nearby until it finishes; the bullets appear in your Backpack. A turret holds ${TURRET_HOPPER}, so keep crafting while you gather.`,location};
    if(!firstTurret){const step=buildStep('turret','Prepare your first turret','Prepare your first turret. Build it near your base and fill it with ammunition.');return {...step,detail:`Turrets draw ${TURRET_KW} kW: place it within coverage of a connected Pole. While placing, the ring shows the ${TURRET_RANGE}-tile firing coverage; cover the open ground attackers must cross to reach the Home core. Once it is fully loaded, a small enemy group will test it. ${step.detail}`};}
    if(!powered(st,firstTurret))return {id:'opening-power',title:'Connect your turret power',text:'Place Poles so your turret sits within coverage of your powered network',resources:resources((e.inv.pole??0)>0?{pole:1}:MACHINE_COST.pole),detail:`Turrets draw ${TURRET_KW} kW and hold fire without power. ${machineStatus(st,firstTurret).reason}. Poles connect by visible cables within 8 tiles; the Generator must have fuel. Once it is powered and fully loaded, a small enemy group will test it.`,location:{x:firstTurret.x+firstTurret.size/2,y:firstTurret.y+firstTurret.size/2}};
    const carried=Math.floor(e.inv.magazine??0),missing=Math.max(0,turretCapacity(firstTurret)-Math.floor(firstTurret.inv.rounds??0));
    return {id:'opening-workshop',title:'Prepare your first turret',text:'Prepare your first turret. Build it near your base and fill it with ammunition.',resources:[{item:'magazine',required:missing,available:carried}],detail:`${loadedText(firstTurret)} · ${missing} more to fill${carried<missing?` · craft ${missing-carried} more bullets at Home Workshop (10 per craft)`:''}. It is powered (${TURRET_KW} kW). Open the turret inventory, select bullets, choose a quantity and Load (1 item = 1 bullet); belts can fill it later. Once it is fully loaded, a small enemy group will test it.`,location:{x:firstTurret.x+firstTurret.size/2,y:firstTurret.y+firstTurret.size/2}};
  }
  const guarded=opening?.turret&&defenceHp(opening.turret)>0?opening.turret:firstTurret;
  const turretLocation=guarded?{x:guarded.x+guarded.size/2,y:guarded.y+guarded.size/2}:location;
  if(status==='scheduled'&&opening)return {id:'opening-attack',title:'Small enemy group approaching',text:`Small enemy group approaching from the ${opening.direction}. Stay near your turret and help defend.`,detail:`Arrives in ${opening.secondsLeft} s · about ${opening.count} basic enemies from the ${opening.direction} marker. Your turret fires automatically within ${TURRET_RANGE} tiles${guarded?` · ${loadedText(guarded)}`:''}; use your Rifle on anything that gets past it.`,location:opening.origin??turretLocation};
  if(status==='active'&&opening)return {id:'opening-attack',title:'Defend your turret',text:`Small enemy group attacking from the ${opening.direction}. Stay near your turret and help defend.`,detail:`${guarded?loadedText(guarded)+' · ':''}${opening.shots} bullets fired so far. Reload by hand if it runs dry; the group withdraws once beaten or after ${OPENING_ENCOUNTER.maxDuration/60} minutes.`,location:turretLocation};
  if((status==='repelled'||status==='lost')&&opening&&opening.endedAt!==undefined&&st.t<opening.endedAt+OPENING_ENCOUNTER.ack)return {id:'opening-attack',title:status==='repelled'?'Attack repelled':'Attack over',text:`${status==='repelled'?'Attack repelled. ':''}Your turret used ${opening.shots} bullets. Connect ammunition production to keep it supplied.`,detail:`${guarded?loadedText(guarded)+'. ':''}Each bullet is one ammunition item. An Assembler set to Shot magazines, with a belt into the turret, keeps it filled without hand loading.`,location:turretLocation};
  // Automate replenishment: complete only when a produced magazine has actually arrived through a connected route.
  const supplied=opening?.suppliedAt!==undefined||(!opening&&turrets.some(m=>(m.inv.rounds??0)>0&&turretSupplier(st,m)));
  if(!supplied){
    if(!turrets.length){const step=buildStep('turret',status==='lost'?'Rebuild your turret':'Prepare your first turret','Build a turret near your base and load it with ammunition');return {...step,detail:`${status==='lost'?'The attack disabled your defence. ':''}Turrets draw ${TURRET_KW} kW within Pole coverage; the placement ring shows the ${TURRET_RANGE}-tile coverage. ${step.detail}`};}
    const ammo=production.find(m=>['assembler','assembler2'].includes(m.kind)&&recipeOutput(recipeOf(m))==='magazine');
    if(!ammo)return {id:'opening-ammo',title:'Automate your turret’s ammunition supply',text:'Build 1 Assembler and set it to Shot magazines',resources:resources((e.inv.assembler??0)>0?{assembler:1}:MACHINE_COST.assembler),detail:`A loaded turret runs dry. Feed Steel plates and Copper to a powered Assembler, then run a belt from its output into the turret (or into a chest with an inserter onward). ${SHOT.inputs.steel} Steel + ${SHOT.inputs.copper} Copper make 10 bullets.`,location:turretLocation};
    const ammoStatus=machineStatus(st,ammo),ammoLocation={x:ammo.x+ammo.size/2,y:ammo.y+ammo.size/2};
    if(ammoStatus.state!=='running'&&ammo.out===0)return {id:'opening-ammo',title:'Automate your turret’s ammunition supply',text:ammoStatus.reason,detail:'Inspect your magazine Assembler for its current input, power or output shortage. A loaded turret is only a reserve.',location:ammoLocation};
    const receivers=receiversOf(ammo).filter(m=>['turret','chest','tramstop','depot'].includes(m.kind));
    if(!turrets.some(t=>supplyChainReaches(st,ammo,t)))return {id:'opening-ammo',title:'Automate your turret’s ammunition supply',text:receivers.length?'Extend the route from the Assembler output to a turret':'Connect the Assembler output to your turret',detail:'Point a belt away from the Assembler and into the turret; storage in between needs an inserter onward. A route that ends elsewhere does not supply the turret.',location:ammoLocation};
    if((ammo.observation?.produced.magazine??ammo.out)<=0)return {id:'opening-ammo',title:'Automate your turret’s ammunition supply',text:'Route connected; waiting for the Assembler to produce',detail:'Follow the first magazine along the belt. A connection alone does not mean the turret has ammunition.',location:ammoLocation};
    return {id:'opening-ammo',title:'Automate your turret’s ammunition supply',text:'Magazines produced; waiting for the first one to reach the turret',detail:'The objective completes when a produced magazine enters the turret through the belt. Keep Steel plates, Copper and generator fuel supplied.',location:ammoLocation};
  }
  if(opening?.suppliedAt!==undefined&&st.t<opening.suppliedAt+OPENING_ENCOUNTER.supplyAck)return {id:'opening-ammo',title:'Automatic resupply working',text:'Automatic resupply working. Your production line is replenishing the turret.',detail:'Bullets now arrive without hand loading. Watch the Assembler’s Steel, Copper and power; the reserve is only as deep as its inputs.',location:turretLocation};
  const recommendation=turretRecommendation(st);
  if(!recommendation.complete){const step=buildStep('turret',`Expand your defences (${recommendation.count}/3)`,recommendation.text);return {...step,detail:recommendation.detail+' Materials below are for the next turret. '+step.detail};}
  const empty=turrets.find(m=>(m.inv.rounds??0)<=0);
  if(empty)return {id:'opening-workshop',title:'Load your new turret',text:'Load bullets into the empty turret or extend your ammunition belt to it',detail:`${loadedText(empty)}. A turret without ammunition covers nothing. Belts, inserters or hand loading all work.`,location:{x:empty.x+empty.size/2,y:empty.y+empty.size/2}};
  if((e.inv.magazine??0)<8&&!st.campaign?.progression?.sites.some(s=>s.recovered))return {id:'opening-workshop',title:'Prepare to scout',text:'Carry at least eight bullets for the first camp',detail:'Your turrets are supplied and your Rifle is ready. Check turret ammunition and generator fuel before leaving; the reserve is finite. The nearest freight camp is a short trip from Home.',location};
  const ex=st.campaign?.expansion;
  if(ex&&ex.radio.restoredAt>=0){
    const linked=st.campaign?.fixedTram?fixedPoweredStops(st).length>=2:ex.route.every(t=>machineAt(st,t%st.flow!.tw,Math.floor(t/st.flow!.tw))?.kind==='track')&&ex.stops.every(([x,y])=>{const m=machineAt(st,x,y);return m?.kind==='tramstop'&&powered(st,m);})&&machines(st).some(m=>m.kind==='tram'&&tramRoute(st,m).includes(ex.route[0]));
    if(!linked)return {id:'station',title:'Power your tram stops',text:'Power at least two permanent stops',detail:st.campaign?.fixedTram?fixedTramStatus(st):'Connect and power the existing route.',location:{x:ex.station.x,y:ex.station.y}};
  }
  const sites=campaignDiscoveries(st),progression=st.campaign?.progression;
  if(progression?.gameplay?.region&&!progression.gameplay.strongholds.freight.opened){const g=progression.gameplay,id=g.strongholds.freight.keys.length<3?['freight:camp:1','freight:camp:2','freight:camp:3'].find(id=>!g.claimed.includes(id))!:'freight',info=sites.find(s=>s.id===`gp:${id}`)!;return {id:info.id,title:info.title,text:info.status,detail:info.detail,location:{x:info.x,y:info.y}};}
  if(progression){
    const carryingCore=['core1','core2','core3'].some(k=>(e.inv[k]??0)>0);
    const target=progression.sites.find(s=>carryingCore?s.kind==='plant'&&!s.installed:s.kind==='core'&&!s.recovered);
    const info=target&&sites.find(s=>s.id===target.id);
    if(info)return {id:info.id,title:carryingCore?'Choose a regional plant':info.title,text:carryingCore?'Deliver preparation materials and install your carried core':info.status,detail:carryingCore?'Any compatible uncommissioned plant accepts your core. Inspect its material requirements before activation.':info.detail,location:{x:info.x,y:info.y}};
  }
  const next=sites.find(s=>s.needs.some(n=>n.delivered<n.required))??sites.find(s=>s.actions.some(a=>a.commands.some(c=>c.type==='restoreSite')));
  if(next)return {id:next.id,title:next.title,text:'Supply and restore this known installation',detail:next.detail,location:{x:next.x+next.size/2,y:next.y+next.size/2}};
  return {id:'opening-workshop',title:'Keep your workshop producing',text:'Explore and connect your known destinations',detail:'Your existing production is running. Use Projects for restoration and service details.'};
}

// ------------------------------------------------------------------ counting what stands

const machines = (st: SimState): readonly Machine[] => st.flow?.machines ?? [];
export const generators = (st: SimState): Machine[] => machines(st).filter(m => m.kind === 'generator');
export const assemblers = (st: SimState): Machine[] => machines(st).filter(m => m.kind === 'assembler');
export const shotAssemblers = (st: SimState): Machine[] => assemblers(st).filter(m => (m.recipe ?? 'shot') === 'shot');
const PATCH_OF = { steel: P_STEEL, copper: P_COPPER, coal: P_COAL } as const;
export type PatchType = keyof typeof PATCH_OF;
/** Whether an Excavator's reach (flow.ts findRubble: one tile around its footprint) covers a tile of the HQ lot's
 *  `type` patch, dug out or not. */
function reachOnPatch(st: SimState, m: Machine, type: PatchType): boolean {
  const p = PATCH_OF[type];
  for (const q of HQ_PATCHES) {
    if (q.type !== p) continue;
    for (let ly = q.ly; ly < q.ly + q.h; ly++) for (let lx = q.lx; lx < q.lx + q.w; lx++) {
      const [tx, ty] = hqLot(st, lx, ly);
      if (tx >= m.x - 1 && tx <= m.x + m.size && ty >= m.y - 1 && ty <= m.y + m.size) return true;
    }
  }
  return false;
}
/** The Excavators placed on the HQ lot's `type` patch (their reach covers one of its tiles, dug out or not): what
 *  §11's patch rows count. A line whose first corner is dug out still stands; a district's Excavator digging the
 *  same kind of rubble elsewhere does not count here. */
export function patchExcavators(st: SimState, type: PatchType): Machine[] {
  return st.flow ? machines(st).filter(m => m.kind === 'excavator' && reachOnPatch(st, m, type)) : [];
}
/** Excavators digging `type` right now, anywhere: rubble of that type in reach, or a unit of it waiting at the output. */
export function excavatorsOn(st: SimState, type: Item): Machine[] {
  return machines(st).filter(m => m.kind === 'excavator' && (m.hold === type || findRubble(st, m)?.type === type));
}
/** Units left in the HQ lot's patch of `type` (steel, copper or coal), as `rubbleAt` sees them; 0 without a flow layer. */
export function patchLeft(st: SimState, type: 'steel' | 'copper' | 'coal'): number {
  if (!st.flow) return 0;
  const p = type === 'steel' ? P_STEEL : type === 'copper' ? P_COPPER : P_COAL;
  let left = 0;
  for (const q of HQ_PATCHES) {
    if (q.type !== p) continue;
    for (let ly = q.ly; ly < q.ly + q.h; ly++) for (let lx = q.lx; lx < q.lx + q.w; lx++) {
      const [tx, ty] = hqLot(st, lx, ly), r = rubbleAt(st, tx, ty);
      if (r) left += r.units;
    }
  }
  return left;
}
/** Coal the rail yard's heap still holds (RAIL_YARD_COAL minus what was mined there, D-P4-12); 0 without a rail yard. */
export function railCoalLeft(st: SimState): number {
  return ground(st).railYard < 0 || !st.flow ? 0 : Math.max(0, RAIL_YARD_COAL - st.flow.stats.railCoal);
}
/** Coal on hand: the Depot's store, every Generator's hopper and the pockets. */
export function coalOnHand(st: SimState): number {
  const f = st.flow;
  if (!f) return 0;
  let c = f.store.coal + (st.engineer.inv.coal ?? 0);
  for (const m of generators(st)) c += m.inv.coal ?? 0;
  return c;
}
/** Coal the grid burns a minute at the last block tick's load (4 MJ a coal, §11). */
export const coalBurnPerMin = (st: SimState): number => st.flow ? st.flow.power.load / (COAL_MJ * 1000) * 60 : 0;
const chestMags = (st: SimState): number => Math.floor(st.buffer / (st.flow?.ammoVersion===1?1:SHOT.count))+(st.flow?.ammoRecovery??0);
const n = (v: number): string => String(Math.floor(v + 1e-9));
const mins = (s: number): string => `${Math.floor(s / 60)} min`;
const cu = (c: { steel: number; copper: number }): string => costStr(c);

function coalWhy(st: SimState): string {
  const burn = coalBurnPerMin(st), have = coalOnHand(st);
  if (burn <= 1e-9) return `${n(have)} coal on hand; the grid is idle`;
  return `the grid burns ${burn.toFixed(1)} coal/min; ${n(have)} coal on hand is ${mins(have / burn * 60)}`;
}
function powerWhy(st: SimState): string {
  const p = st.flow!.power, g = generators(st).length;
  return `${g} Generator${g === 1 ? '' : 's'} give${g === 1 ? 's' : ''} ${n(p.supply)} kW; demand ${n(p.demand)} kW${p.throttle < 1 - 1e-9 ? ` — machines at ${Math.round(p.throttle * 100)} %` : ''}`;
}
function frontWhy(st: SimState): string {
  const a = ammoStatus(st);
  return `${chestMags(st)} magazines in the chest; the front drew ${a.demandMagPerMin1.toFixed(1)} a minute last minute${a.emptyHoppers ? `; ${a.emptyHoppers} empty hopper${a.emptyHoppers === 1 ? '' : 's'}` : ''}`;
}

// ------------------------------------------------------------------ the claims

function claimLine(st: SimState, dir: GoalDir, i: number): GoalLine | null {
  const b = st.blocks[i], name = blockName(st, i);
  if (b.state === CONTESTED) {
    // RI-06: the Heart's commissioning is the encounter's line (objective, active failure condition, next action), not a burn-off clock
    const H = heartAt(st, i);
    if (H && H.attempt >= 0) return { id: `contest-${dir}`, block: i, text: `Commission ${name}: ${describeHeart(st)}`,
      why: 'the Junction Heart: both feeder cabinets powered for the whole productive time; a knocked-out or unpowered feeder pauses it and interrupts it after the stall time — everything on the block is kept for the retry' };
    const left = Math.max(0, b.contestUntil - st.t);
    return { id: `contest-${dir}`, block: i,
             text: `Hold ${name}: Held in ${n(left)} s`,
             why: `the burn-off (${n(burnOffS(rotOf(st, i)))} s at its rot) ends the claim; carry its street kit out — an unkitted street fires nothing` };
  }
  if (b.state !== DARK) return null;
  const ci = claimInfo(st, b.x, b.y), cost = st.config.eco.claimCost;
  const rubble = ground(st).blocks[i].rubble;
  const what = i === ground(st).railYard ? `${n(railCoalLeft(st))} coal in its heap` : rubble ? `${rubble} rubble for the line` : 'no rubble';
  const facts = `rot ${Math.round(ci.rot * 100)} % · front ${ci.frontDelta >= 0 ? '+' : ''}${ci.frontDelta} · closes ${ci.closes} · wake bloom ≈ ${ci.wakeBloomCrawlers}`;
  if (st.fallen[i]) return { id: `retake-${dir}`, block: i, text: `Retake ${name} (${cu(cost)} of wire and frames from the chest)`, why: `it fell; its street is open and its machines stand still until it is Held again · ${facts}` };
  const afford = ci.affordable ? '' : ` — ${n(st.stock.steel)} steel / ${n(st.stock.copper)} Cu in the chest, short`;
  return { id: `claim-${dir}`, block: i, text: `Claim ${name} (${cu(cost)} of wire and frames from the chest)${afford}`, why: `${what} · ${facts}` };
}

// ------------------------------------------------------------------ the goal

function goalOf(st: SimState): GoalLine {
  const hq = hqIdx(st);
  if (st.blocks[hq].state !== HELD) return { id: 'hq-fell', block: hq, text: 'The HQ has fallen', why: 'every machine on its lot has stopped and the front has no home' };
  const f = st.flow;
  if (!f) {
    let empty = 0;
    for (const e of st.ring) if (e.hopper <= 1e-9) empty++;
    return { id: 'front', text: `Keep the front fed: ${empty} of ${st.ring.length} hoppers empty`, why: `an empty hopper lets crawlers reach the substation; ${st.config.unfedN} unfed arrivals turn it off` };
  }
  const e = st.engineer, shot = shotAssemblers(st).length, gens = generators(st).length;
  // §11 0:00 — hands: 20 steel, 10 magazines (HOUR_MINE_STEEL, HOUR_CRAFT_MAGS)
  if (shot === 0 && f.stats.handCrafted < HOUR_CRAFT_MAGS) {
    const left = HOUR_CRAFT_MAGS - f.stats.handCrafted, needSteel = SHOT.inputs.steel * left, needCu = SHOT.inputs.copper * left;
    const steel = e.inv.steel ?? 0, copper = e.inv.copper ?? 0;
    if (steel < needSteel && f.stats.handMined < HOUR_MINE_STEEL)
      return { id: 'mine', text: `Mine steel from the HQ patch by hand: ${n(f.stats.handMined)} / ${HOUR_MINE_STEEL} (pockets ${n(steel)} steel)`,
               why: `${left} hand-crafted magazines take ${needSteel} steel + ${needCu} Cu; ${frontWhy(st)}` };
    const cuNote = copper < needCu ? ` (take ${needCu - Math.floor(copper)} Cu from the chest)` : '';
    return { id: 'craft', text: `Craft Shot magazines at the workbench: ${n(f.stats.handCrafted)} / ${HOUR_CRAFT_MAGS}${cuNote}`, why: `${SHOT.seconds} s each from the pockets; ${frontWhy(st)}` };
  }
  // §11 6:00 — the coal Excavator and Generator 2, then the steel and copper Excavators
  if (patchExcavators(st, 'coal').length === 0 && patchLeft(st, 'coal') > 0)
    return { id: 'coal-line', text: `Dig the HQ coal patch: an Excavator (${cu(MACHINE_COST.excavator)}) and a belt to the Depot (${n(patchLeft(st, 'coal'))} coal left)`, why: coalWhy(st) };
  if (gens < 2) return { id: 'gen-2', text: `Build a second Generator (${cu(MACHINE_COST.generator)}) and feed it coal`, why: `${powerWhy(st)}; one ${GENERATOR_KW} kW Generator is the whole grid` };
  if (patchExcavators(st, 'steel').length === 0 && patchLeft(st, 'steel') > 0)
    return { id: 'steel-line', text: `Dig the HQ steel patch: an Excavator (${cu(MACHINE_COST.excavator)}) and a belt to the Depot`, why: `${n(st.stock.steel)} steel in the chest; a magazine takes ${SHOT.inputs.steel}, a claim ${st.config.eco.claimCost.steel}, a Generator ${MACHINE_COST.generator.steel}` };
  if (patchExcavators(st, 'copper').length === 0 && patchLeft(st, 'copper') > 0)
    return { id: 'copper-line', text: `Dig the HQ copper patch: an Excavator (${cu(MACHINE_COST.excavator)}) and a belt to the Depot`, why: `${n(st.stock.copper)} Cu in the chest; a magazine takes ${SHOT.inputs.copper}, a claim ${st.config.eco.claimCost.copper}` };
  // §11 8:00 — the Shot line
  if (shot === 0) return { id: 'shot-line', text: `Place a Shot Assembler (${cu(MACHINE_COST.assembler)}) with inserters from the steel and copper belts and one to the Depot`, why: `it makes a magazine every ${SHOT.seconds} s; ${frontWhy(st)}` };
  // §11 12:00 — the second steel Excavator; 15:00 — Generator 3, then the east claim
  if (patchExcavators(st, 'steel').length < 2 && patchLeft(st, 'steel') > 0)
    return { id: 'steel-2', text: `Add a second steel Excavator on the HQ patch (${cu(MACHINE_COST.excavator)})`, why: `one digs ${EXCAVATOR_PER_S} steel/s; ${n(st.stock.steel)} steel in the chest and the east claim takes ${st.config.eco.claimCost.steel}` };
  if (gens < 3) return { id: 'gen-3', text: `Build a third Generator (${cu(MACHINE_COST.generator)})`, why: powerWhy(st) };
  const east = hqNeighbourToward(st, 'east'), west = hqNeighbourToward(st, 'west'), north = hqNeighbourToward(st, 'north');
  if (east >= 0) { const g = claimLine(st, 'east', east); if (g) return g; }
  // §11 25:00 — the west claim (the rail yard) and its coal
  if (west >= 0) { const g = claimLine(st, 'west', west); if (g) return g; }
  if (west >= 0 && st.blocks[west].state === HELD && railCoalLeft(st) > 0 && !excavatorsOn(st, 'coal').some(m => blockOfTile(st, m.x, m.y) === west))
    return { id: 'rail-coal', block: west, text: `Dig the ${blockName(st, west)} heap: an Excavator (${cu(MACHINE_COST.excavator)}) and a belt to the Depot (${n(railCoalLeft(st))} coal left)`, why: coalWhy(st) };
  // §11 45:00 – 50:00 — Generator 4, the second copper Excavator, the third Assembler
  if (gens < HOUR_END.generators) return { id: 'gen-4', text: `Build a fourth Generator (${cu(MACHINE_COST.generator)})`, why: powerWhy(st) };
  if (patchExcavators(st, 'copper').length < 2 && patchLeft(st, 'copper') > 0)
    return { id: 'copper-2', text: `Add a second copper Excavator on the HQ patch (${cu(MACHINE_COST.excavator)})`, why: `${n(st.stock.copper)} Cu in the chest; ${frontWhy(st)}` };
  if (assemblers(st).length < HOUR_END.assemblers)
    return { id: 'assembler-3', text: `Place a third Assembler (${cu(MACHINE_COST.assembler)})`, why: `${assemblers(st).length} Assemblers stand; ${frontWhy(st)}` };
  // §11 65:00 — the north claim
  if (north >= 0) { const g = claimLine(st, 'north', north); if (g) return g; }
  return { id: 'hold', text: `Hold the ${heldCount(st)} blocks: kit and feed every street`, why: frontWhy(st) };
}

// ------------------------------------------------------------------ the support line

function supportOf(st: SimState): GoalLine | null {
  const e = st.engineer;
  if (e.down >= 0) return { id: 'down', text: `You are down — back on your feet at the HQ in ${n(Math.max(0, e.down - st.t))} s`, why: 'nothing is carried, mined or fired meanwhile' };
  const f = st.flow;
  if (!f) return null;
  const gens = generators(st);
  if (gens.length > 0 && gens.every(m => (m.inv.coal ?? 0)+(m.inv.fuel??0) <= 0)) {
    const chest = f.store.coal, pockets = e.inv.coal ?? 0;
    return { id: 'gen-dry', text: chest + pockets > 0 ? `Every Generator is out of coal — feed one (E) from ${pockets > 0 ? `the pockets (${n(pockets)})` : `the chest (${n(chest)})`}` : 'Every Generator is out of coal and there is none on hand — dig the coal patch',
             why: 'a dead grid stops every Excavator, inserter and Assembler at once' };
  }
  let kit: Edge | null = null, empty: Edge | null = null;
  for (const ed of st.ring) {
    if (st.blocks[ed.a].state !== HELD) continue;
    if (ed.kit === false) { kit ??= ed; continue; }
    if (ed.hopper <= 1e-9 && edgeCap(st, ed) > 0) empty ??= ed;
  }
  if (kit) return { id: 'kit', block: kit.a, text: `Carry a kit to ${streetName(st, kit.a, kit.b)} (${n(e.inv.kit ?? 0)} in the pockets; the chest has more)`, why: 'an unkitted street has no turrets: its crawlers walk straight to the substation' };
  if (empty) return { id: 'feed', block: empty.a, text: `Feed ${streetName(st, empty.a, empty.b)}: its hopper is empty (pockets ${n(e.inv.magazine ?? 0)} magazines, chest ${chestMags(st)})`, why: `${empty.turrets ?? 0} turret${empty.turrets === 1 ? '' : 's'} on it hold ${TURRET_HOPPER} rounds each; an empty street lets crawlers through` };
  if (throttle(st) < 1 - 1e-9) return { id: 'brownout', text: `Brownout: machines at ${Math.round(throttle(st) * 100)} %`, why: `${powerWhy(st)}; another Generator or coal lifts it` };
  return null;
}

/** The current goal and its support line. Pure; cheap enough for a HUD to call once a second. */
export function currentGoal(st: SimState, tracked?:string|null): Goal {
  if (isCampaign(st)) {
    const next=campaignNext(st,tracked);
    return {next,goal:{id:next.id==='opening-workshop'?'home-factory':next.id==='recovery'?'down':'home-explore',text:next.title,why:next.text},support:{id:'campaign-threat',text:campaignWarning(st),why:'Prepare ammunition and repair defences before the announced assault start.'}};
  }
  return { goal: goalOf(st), support: supportOf(st) };
}

// ------------------------------------------------------------------ machine state words

export type MachineState = 'running' | 'starved' | 'blocked' | 'idle' | 'off';
export interface MachineStatus { state: MachineState; reason: string }
/** §11.2: a machine's working / starved / blocked state in one word, with the reason (never colour alone). */
export function machineStatus(st: SimState, m: Machine): MachineStatus {
  if(isCampaign(st)&&((defenceMax(m)>0&&defenceHp(m)<=0)||coreDisabledAt(st,m.x,m.y)))return {state:'off',reason:'disabled — repair the defence or base core'};
  const bi = blockOfTile(st, m.x, m.y), b = bi >= 0 ? st.blocks[bi] : null;
  const field = bi >= 0 && isFieldKind(m.kind) && fieldBlock(st, bi);   // RI-03: the field kit runs on the claim front
  if (!b || (!isCampaign(st) && b.state !== HELD && !field)) return { state: 'off', reason: 'block not Held' };
  if (machineKw(st, m) > 0 && !powered(st, m)) return { state: 'off', reason: isCampaign(st)?campaignGrid(st).machines.has(m.id)?'Connected network has no active supply':'Connect a Pole within 8 tiles to a fuelled Generator':field ? 'no connected pole in reach' : 'no power' };
  return machineOperationStatus(st,m);
}
/** Operating predicates without the independent location/power gate; shared by multi-constraint inspection. */
export function machineOperationStatus(st:SimState,m:Machine):MachineStatus {
  const bi=blockOfTile(st,m.x,m.y),b=bi>=0?st.blocks[bi]:null;
  switch (m.kind) {
    case 'barricade': case 'wall': return {state:'idle',reason:`${Math.ceil(defenceHp(m))} HP`};
    case 'cannon': return (m.inv.shell??0)>0?{state:'idle',reason:'ready; waiting for target'}:{state:'starved',reason:'needs shells'};
    case 'turret': {
      if ((m.inv.rounds ?? 0) < 1) return { state: 'starved', reason: 'empty hopper' };
      return m.out > 0 || m.timer > 0 ? { state: 'running', reason: 'firing' } : { state: 'idle', reason: 'nothing in range' };
    }
    case 'generator': {
      if ((m.inv.coal ?? 0)+(m.inv.fuel??0) <= 0) return { state: 'starved', reason: 'out of coal' };
      if(isCampaign(st))return (campaignGrid(st).generation.get(m.id)??0)>0?{state:'running',reason:'supplying local circuit'}:{state:'idle',reason:'no local load'};
      return m.busy ? { state: 'running', reason: 'burning' } : { state: 'idle', reason: 'no load' };
    }
    case 'pumpjack': case 'excavator': {
      if (m.hold) return { state: 'blocked', reason: `output blocked (${itemName(m.hold)})` };
      const r = findRubble(st, m);
      return r ? { state: 'running', reason: `digging ${itemName(r.type)}` } : { state: 'starved', reason: 'nothing in reach' };
    }
    case 'mixer': case 'alienworkbench': case 'foundry': case 'refinery': case 'assembler2': case 'assembler': {
      if(m.kind==='alienworkbench'){if(m.decode)return {state:'running',reason:`decoding ${Math.ceil(m.decode.progress)}/15 s`};const why=fabricationBlocker(st);if(why)return {state:'blocked',reason:why};}
      if (m.busy) return { state: 'running', reason: `making ${itemName(recipeOutput(recipeOf(m)))}` };
      if (m.out >= ASM_OUTPUT_CAP) return { state: 'blocked', reason: 'output full' };
      if (!asmCanStart(m)) {
        const r = recipeOf(m), short = Object.keys(r.inputs).filter(k => (m.inv[k] ?? 0) < r.inputs[k]);
        return { state: 'starved', reason: `needs ${short.map(itemName).join(', ')}` };
      }
      return { state: 'idle', reason: 'ready' };
    }
    case 'inserter': {
      const [sx, sy] = inputTile(m), [dx, dy] = outputTile(m);
      const src = machineAt(st, sx, sy), dst = machineAt(st, dx, dy);
      if(isCampaign(st)){
        if(m.phase===1&&m.hold)return m.timer<=1e-9&&(!dst||!accepts(st,dst,m.hold,.5))?{state:'blocked',reason:dst?`${dst.kind} cannot accept ${itemName(m.hold)}`:'no destination; held item retained'}:{state:'running',reason:`carrying ${itemName(m.hold)}`};
        if(m.phase===2)return {state:'running',reason:'returning to pickup'};
        const pick=inserterPickup(st,m);if(pick)return {state:'idle',reason:`ready to pick up ${itemName(pick.item)}`};
        const waiting=inserterPickup(st,m,true);if(waiting)return {state:'blocked',reason:`destination cannot accept ${itemName(waiting.item)}`};
      }
      if (!src || !dst) return { state: 'starved', reason: !src ? 'nothing behind it' : 'nothing in front' };
      if (m.phase === 1 && m.hold && m.timer <= 1e-9 && !accepts(st, dst, m.hold, 0.5)) return { state: 'blocked', reason: `${dst.kind} full` };
      if (m.phase !== 0) return { state: 'running', reason: m.hold ? `carrying ${itemName(m.hold)}` : 'swinging back' };
      return { state: 'starved', reason: m.filter?`no matching ${itemName(m.filter)} to pick up`:'nothing to pick up' };
    }
    case 'underground': case 'splitter': return {state:routingStatus(st,m),reason:routingDescription(st,m)};
    case 'fastbelt': case 'belt': {
      const lead=m.items.at(-1),dst=nextOf(st,m);
      if(isCampaign(st)&&lead&&lead.p>=1-BELT_SPACING/2-1e-9&&(!dst||!accepts(st,dst,lead.k)))return {state:'blocked',reason:dst?`output cannot accept ${itemName(lead.k)}`:'no output connection'};
      return m.items.length?{state:'running',reason:`${m.items.length} items moving`}:{state:'idle',reason:'empty'};
    }
    case 'arclamp': case 'lamp': case 'floodlight': return { state: 'running', reason: 'lit' };
    case 'pole': case 'bigpole': {
      if(isCampaign(st)){const c=campaignGrid(st).poles.get(m.id);return (c?.supply??0)>0?{state:'running',reason:'connected to a supplied circuit'}:(c?.rated??0)>0?{state:'off',reason:'connected · its Generator has no fuel'}:{state:'off',reason:'no Generator or supplied Pole within reach'};}
      return poleGrid(st).connected.has(m.id)?{state:'running',reason:'on the grid'}:{state:'idle',reason:'not connected'};
    }
    case 'substation': return (isCampaign(st)?campaignGrid(st).blocks[bi].throttle>0:!!b&&subPowered(st, b)) ? { state: 'running', reason: 'on' } : { state: 'off', reason: 'no circuit supply' };
    case 'depot': return { state: 'idle', reason: `${chestMags(st)} ammunition items at Home` };
    // RI-05
    case 'chest': { const n = invTotal(m.inv); return n > 0 ? { state: 'idle', reason: `${n} item${n === 1 ? '' : 's'}` } : { state: 'idle', reason: 'empty' }; }
    case 'track': return { state: 'idle', reason: tramAt(st, m.x, m.y) ? 'a tram on it' : 'rail' };
    case 'tramstop': { const a = invTotal(m.cargo), q = invTotal(m.inv); return a + q > 0 ? { state: 'idle', reason: `${q} waiting, ${a} arrived` } : { state: 'idle', reason: 'empty' }; }
    case 'tram': { const path = tramRoute(st, m); return path.length < 2 ? { state: 'starved', reason: 'no route' } : m.phase === 1 ? { state: 'idle', reason: 'at a stop' } : { state: 'running', reason: `${invTotal(m.cargo)} aboard` }; }
  }
}
