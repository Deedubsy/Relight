/** P7-02: concrete production is a saved campaign capability, never a legacy recipe override. */
import type { SimState } from './types';
import { ASM_OUTPUT_CAP, ASM_INPUT_MULT, MACHINE_SIZE, recipeOf } from './flow';
import { isCampaign } from './rules';
export function concreteProblem(st:SimState):string {
  const f=st.flow;if(!f)return '';
  const count=(n:unknown)=>typeof n==='number'&&Number.isSafeInteger(n)&&n>=0;
  if(f.stats.placed.concrete!==undefined&&(!isCampaign(st)||(st.campaign?.version??0)<7||!count(f.stats.placed.concrete)))return 'invalid concrete construction counter';
  for(const m of f.machines) {
    if(m.kind!=='mixer'&&m.kind!=='barricade')continue;
    if(!isCampaign(st)||(st.campaign?.version??0)<7)return 'concrete machines require campaign metadata version 7';
    if(m.size!==MACHINE_SIZE[m.kind]||!Number.isInteger(m.x)||!Number.isInteger(m.y)||m.x<0||m.y<0||m.x+m.size>f.tw||m.y+m.size>st.city!.th
      ||!Number.isInteger(m.dir)||m.dir<0||m.dir>3||m.recipe!==undefined||!m.inv||!Array.isArray(m.items)||m.items.length||m.hold!==null)return 'invalid concrete machine';
    if(m.kind==='mixer') {
      const r=recipeOf(m);
      if(!count(m.out)||m.out>ASM_OUTPUT_CAP||typeof m.busy!=='boolean'||(m.busy&&m.out>=ASM_OUTPUT_CAP)
        ||!Number.isFinite(m.timer)||m.timer<0||m.timer>=r.seconds||(!m.busy&&m.timer!==0)
        ||Object.entries(m.inv).some(([k,n])=>k!=='stone'||!count(n)||n>r.inputs.stone*ASM_INPUT_MULT))return 'invalid Mixer production';
    }else if(Object.keys(m.inv).length||m.out!==0||m.busy||m.timer!==0)return 'invalid Barricade contents';
  }
  return '';
}
