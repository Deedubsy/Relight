import {factoryScenario} from '../../../packages/harness/src/factoryScenario';
import {ground} from '../../../packages/sim/src/index';
const s=factoryScenario(3);console.log(JSON.stringify({t:s.st.t,e:s.st.engineer,m:s.st.flow!.machines,gate:ground(s.st).opening,stations:s.st.campaign!.expansion,dist:s.st.campaign!.districts},null,1));
