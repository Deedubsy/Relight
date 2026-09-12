import type {SimState} from './types';
import {isProcessor,recipeOf,recipeOutput,SUPPLY_CHEST_CAP,STOP_CAP,TRAM_CAP} from './flow';
import {invStacks,invCap} from './engineer';
/** v1: item quantities are individual bullets; loaded rounds, shots fired and buffer already were rounds.
 * Floating residue below 1e-7 rounds is numerical drift. Other fractional rounds reject the copy safely. */
export function migrateAmmunition(st:SimState):void {
 const f=st.flow;if(!f||!st.city?.mapId||f.ammoVersion===1)return;
 const whole=(v:number)=>{const r=Math.round(v);if(!Number.isSafeInteger(r)||Math.abs(v-r)>1e-7)throw new Error('Ammunition migration cannot represent fractional bullets safely; original save preserved.');return r;};
 const rounds=(n:number)=>whole(n*10);st.buffer=whole(st.buffer);
 const pool=(p:Partial<Record<string,number>>|undefined)=>{if(p?.magazine!==undefined)p.magazine=rounds(p.magazine);};
 pool(st.engineer.inv);pool(f.store);pool(st.campaign?.truck?.cargo);
 if(st.engineer.pack)for(const c of st.engineer.pack)if(c?.item==='magazine')c.count=rounds(c.count);
 if(invStacks(st.engineer.inv)>invCap(st.engineer))throw new Error('Ammunition migration needs more Backpack space; original save preserved. Store ammunition and retry.');
 let recovered=0;
 for(const m of f.machines){
  pool(m.inv);pool(m.cargo);if(m.kind==='turret'&&m.inv.rounds!==undefined)m.inv.rounds=whole(m.inv.rounds);
  if(isProcessor(m)&&recipeOutput(recipeOf(m))==='magazine')m.out=rounds(m.out);
  // A legacy belt/inserter token is one ten-round item. Keep one bullet on its path;
  // place the other nine in an explicitly named finite Home recovery pool.
  for(const i of m.items)if(i.k==='magazine')recovered+=9;
  if(m.hold==='magazine')recovered+=9;
  for(const [p,cap] of [[m.inv,m.kind==='chest'?SUPPLY_CHEST_CAP:m.kind==='tramstop'?STOP_CAP:Infinity],[m.cargo,m.kind==='tramstop'?STOP_CAP:m.kind==='tram'?TRAM_CAP:Infinity]] as const){
   if(!p)continue;const excess=Math.max(0,Object.values(p).reduce((a,b)=>a+b,0)-cap),move=Math.min(p.magazine??0,excess);if(move){p.magazine-=move;recovered+=move;}
  }
  const rule=m.freight?.magazine;if(rule){rule.request=Math.min(STOP_CAP,rounds(rule.request));rule.reserve=Math.min(STOP_CAP,rounds(rule.reserve));}
  let unreserved=m.cargo?.magazine??0;
  if(m.manifest)m.manifest=m.manifest.flatMap(r=>{if(r.item!=='magazine')return [r];const n=Math.min(unreserved,rounds(r.n));unreserved-=n;return n?[{...r,n}]:[];});
  m.ammoVersion=1;
 }
 // Item-denominated historical counters convert too; true round counters remain untouched.
 for(const p of [f.stats.made,f.stats.minedOf,f.stats.consumed,f.stats.putBack,f.ledger?.base])if(p?.magazine!==undefined)p.magazine*=10;
 if(st.campaign?.districts)st.campaign.districts.supplied.magazine*=10;
 f.ammoRecovery=(f.ammoRecovery??0)+recovered;f.ammoVersion=1;
 if(recovered&&st.campaign?.progression)st.campaign.progression.notice=`${recovered} migrated bullets recovered at Home storage; all ammunition preserved.`;
}

export function ammunitionProblem(st:SimState):string {
 const f=st.flow;if(!f||f.ammoVersion===undefined)return '';
 if(f.ammoVersion!==1)return 'Unknown ammunition version';
 const valid=(n:number|undefined)=>n===undefined||Number.isSafeInteger(n)&&n>=0;
 if(!valid(f.ammoRecovery)||!valid(st.engineer.inv.magazine)||!valid(st.buffer)||!valid(st.campaign?.truck?.cargo.magazine))return 'Invalid bullet quantity';
 for(const m of f.machines)if(m.ammoVersion!==1||!valid(m.inv.magazine)||!valid(m.cargo?.magazine)||m.kind==='turret'&&!valid(m.inv.rounds)||isProcessor(m)&&recipeOutput(recipeOf(m))==='magazine'&&!valid(m.out))return 'Invalid machine bullet quantity';
 return '';
}
