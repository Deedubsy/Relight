/** Historical supplied opening for retained subsystem regression tests only.
 * This is a prepared fixture, never campaign entry or evidence of empty-start reachability.
 */
import {createProceduralCampaign as createCampaign,addMachine,hqLot,START_CHEST,START_COAL,openLedger,type SimState} from '../src/index';
export function suppliedCampaign(...args:Parameters<typeof createCampaign>):SimState {
 const st=createCampaign(...args),f=st.flow!;
 st.stock={steel:START_CHEST.steel,copper:START_CHEST.copper,stone:START_CHEST.stone};f.store.coal=START_CHEST.coal;st.buffer=START_CHEST.magazines*10;
 const m=addMachine(st,'generator',...hqLot(st,19,8),0);m.inv.coal=START_COAL;f.ledger=openLedger(st);
 return st;
}
