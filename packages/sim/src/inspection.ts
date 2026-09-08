/** P5-03: read-only factory queries and bounded, saved simulation-time observations. */
import type { SimState } from './types';
import { type Machine, type Item, ITEMS, isItem, machineById, recipeOf, recipeOutput, recipeYield, MACHINE_KW, KIND_LABEL, DIR_NAMES, findRubble, BELT_PER_S, EXCAVATOR_PER_S, INSERTER_PER_S, powered, throttle, SUPPLY_CHEST_CAP, STOP_CAP, TRAM_CAP } from './flow';
import { machineDimensions as footprint } from './footprint';
import { machineStatus } from './goal';
import { ledgerFlows } from './ledger';
import { campaignGrid } from './campaignPower';
import { blockOfTile, inReach } from './ground';
import { blockLabel } from './names';
import { TURRET_RANGE, LAMP_RADIUS, FLOODLIGHT_RANGE, POLE_REACH, BIG_POLE_REACH, COAL_MJ, GENERATOR_KW } from './recipes';
import { routingDescription, SPLITTER_PER_S, UNDERGROUND_HIDDEN } from './routing';

export const INSPECTION_WINDOW_TICKS = 1200, INSPECTION_SAMPLE_TICKS = 20;
type Counts = Partial<Record<Item, number>>;
interface Sample { tick:number; produced:Counts; consumed:Counts }
export interface Observation { produced:Counts; consumed:Counts; samples:Sample[] }
const measured = (m:Machine) => ['mixer','assembler','excavator','belt','inserter','underground','splitter'].includes(m.kind);
const start = (tick:number):Observation => ({produced:{},consumed:{},samples:[{tick,produced:{},consumed:{}}]});
function sample(o:Observation,tick:number):void {
  if(tick-o.samples.at(-1)!.tick>=INSPECTION_SAMPLE_TICKS)o.samples.push({tick,produced:{...o.produced},consumed:{...o.consumed}});
  while(o.samples.length>1&&o.samples[0].tick<tick-INSPECTION_WINDOW_TICKS)o.samples.shift();
}
/** Called before production ticks, never from rendering. Old previews start a fresh observation interval. */
export function sampleInspection(st:SimState):void {
  if(!st.campaign||!st.flow)return;
  const f=st.flow,{sources,sinks}=ledgerFlows(st);
  if(!f.observation)f.observation={produced:sources,consumed:sinks,samples:[{tick:f.tick,produced:{...sources},consumed:{...sinks}}]};
  f.observation.produced=sources;f.observation.consumed=sinks;sample(f.observation,f.tick);
  for(const m of f.machines)if(measured(m)){m.observation??=start(f.tick);sample(m.observation,f.tick);}
}
export function observeOutput(st:SimState,m:Machine,item:Item,n=1):void {
  if(!st.campaign||!st.flow)return;
  const o=m.observation??=start(st.flow.tick);o.produced[item]=(o.produced[item]??0)+n;
}
function rates(o:Observation|undefined,tick:number,produced=o?.produced??{},consumed=o?.consumed??{}) {
  const first=o?.samples.find(s=>s.tick>=tick-INSPECTION_WINDOW_TICKS),seconds=first?(tick-first.tick)/20:0;
  return {seconds,rows:ITEMS.map(item=>({item,produced:produced[item]??0,consumed:consumed[item]??0,
    producedPerMin:seconds>0?((produced[item]??0)-(first!.produced[item]??0))*60/seconds:null,
    consumedPerMin:seconds>0?((consumed[item]??0)-(first!.consumed[item]??0))*60/seconds:null}))};
}
export function factoryStatistics(st:SimState){
  if(!st.flow)return null;
  const {sources,sinks}=ledgerFlows(st);
  return rates(st.flow.observation,st.flow.tick,sources,sinks);
}
/** A pole is a network node; its physical street-owner block need not belong to its connected circuit. */
export function machineCircuit(st:SimState,m:Machine){
  const bi=blockOfTile(st,m.x,m.y);
  if(st.campaign){
    const grid=campaignGrid(st),c=m.kind==='pole'||m.kind==='bigpole'?grid.poles.get(m.id):bi>=0?grid.blocks[bi]:undefined;
    return {scope:'Local circuit',supply:c?.supply??0,demand:c?.demand??0,load:c?.load??0,throttle:c?.throttle??0};
  }
  const p=st.flow!.power;return {scope:'Legacy grid',supply:p.supply,demand:p.demand,load:p.load,throttle:throttle(st)};
}
export function inspectMachine(st:SimState,id:number){
  const m=machineById(st,id);if(!m)return null;
  const circuit=machineCircuit(st,m),bi=blockOfTile(st,m.x,m.y),status=machineStatus(st,m),draw=MACHINE_KW[m.kind];
  const scale=powered(st,m)&&status.state!=='off'?(draw>0?circuit.throttle:1):0;
  const nominal: {item:string;input:number;output:number}[]=[];
  let recipe:string|null=null,capacity:string|null=null;
  if((m.kind==='assembler'||m.kind==='mixer')){
    const r=recipeOf(m);recipe=r.name;
    for(const [item,n] of Object.entries(r.inputs))nominal.push({item,input:n*60/r.seconds,output:0});
    nominal.push({item:recipeOutput(r),input:0,output:recipeYield(r)*60/r.seconds});
  }else if(m.kind==='excavator'){
    const item=m.hold??findRubble(st,m)?.type;if(item)nominal.push({item,input:0,output:EXCAVATOR_PER_S*60});
  }else if(['belt','underground','splitter','inserter'].includes(m.kind)){
    const n=m.kind==='splitter'?SPLITTER_PER_S:m.kind==='inserter'?INSERTER_PER_S:BELT_PER_S;
    capacity=`Nominal transfer ${n*60}/min; power-limited ${Math.round(n*60*scale)}/min. Downstream space still limits delivery.`;
  }else if(m.kind==='generator')capacity=`Capacity ${GENERATOR_KW} kW; current load share ${st.campaign?campaignGrid(st).generation.get(m.id)??0:st.flow!.power.load/Math.max(1,st.flow!.machines.filter(x=>x.kind==='generator'&&(x.inv.coal??0)>0).length)} kW. Coal burns with load (${COAL_MJ} MJ/item).`;
  const contents:{place:string;item:string;count:number}[]=[];
  const add=(place:string,item:string,count:number)=>{if(count>0)contents.push({place,item,count});};
  if(m.kind==='depot'){
    for(const [k,n] of Object.entries(st.stock))if(isItem(k))add('Home stock',k,n);
    for(const [k,n] of Object.entries(st.flow!.store))add('Home stock',k,n);
    add('Line buffer','magazine',st.buffer/10);
  }else for(const [k,n] of Object.entries(m.inv))add(m.kind==='tramstop'?'Platform':'Inventory',k,n);
  for(const [k,n] of Object.entries(m.cargo??{}))add(m.kind==='tramstop'?'Arrivals':'Cargo',k,n);
  if(m.hold)add('Held',m.hold,1);
  const buffer:Counts={};for(const it of m.items)buffer[it.k]=(buffer[it.k]??0)+1;
  for(const [k,n] of Object.entries(buffer))add('Buffered',k,n);
  if((m.kind==='assembler'||m.kind==='mixer'))add('Finished output',recipeOutput(recipeOf(m)),m.out);
  const settings=[`Facing ${DIR_NAMES[m.dir]}`,`Footprint ${footprint(m).join(' × ')} tiles`];
  if(['inserter','underground','splitter'].includes(m.kind))settings.push(routingDescription(st,m));
  const ranges:Partial<Record<Machine['kind'],string>>={arclamp:'Light radius: 6 tiles',excavator:'Extraction: 5 × 5 tiles (one tile around footprint)',inserter:'Pickup/drop: adjacent tile behind/in front',turret:`Weapon range: ${TURRET_RANGE} tiles`,lamp:`Light radius: ${LAMP_RADIUS} tiles`,floodlight:`Light cone range: ${FLOODLIGHT_RANGE} tiles`,pole:`Connection reach: ${POLE_REACH} tiles`,bigpole:`Connection reach: ${BIG_POLE_REACH} tiles`,underground:`Span: up to ${UNDERGROUND_HIDDEN} hidden tiles`,chest:`Storage: ${SUPPLY_CHEST_CAP} items`,tramstop:`Platform/arrivals: ${STOP_CAP} items each`,tram:`Cargo: ${TRAM_CAP} items`};
  return {id,title:KIND_LABEL[m.kind],kind:m.kind,status,location:bi>=0?blockLabel(st,bi,false):'Outside a serviced block',
    reachable:inReach(st,m.x,m.y,...footprint(m)),circuit,draw,scale,recipe,recipeId:m.recipe??'shot',nominal,capacity,contents,settings,range:ranges[m.kind]??null,
    measured:measured(m)?rates(m.observation,st.flow!.tick):null,measurement:(m.kind==='assembler'||m.kind==='mixer')||m.kind==='excavator'?'Completed production':'Transfers out'};
}
export function inspectionProblem(st:SimState):string {
  if(!st.flow)return '';
  for(const o of [st.flow.observation,...st.flow.machines.map(m=>m.observation)]){
    if(o===undefined)continue;
    if(!st.campaign||!o||!Array.isArray(o.samples)||!o.samples.length||o.samples.length>61)return 'invalid observation history';
    const valid=(v:Counts)=>v&&typeof v==='object'&&!Array.isArray(v)&&Object.entries(v).every(([k,n])=>isItem(k)&&Number.isFinite(n)&&n>=0);
    if(!valid(o.produced)||!valid(o.consumed))return 'invalid observation counters';
    let prev=-1;
    for(const s of o.samples){
      if(!s||!Number.isSafeInteger(s.tick)||s.tick<0||s.tick>st.flow.tick||s.tick<=prev||!valid(s.produced)||!valid(s.consumed))return 'invalid observation sample';
      prev=s.tick;
    }
  }
  return '';
}
