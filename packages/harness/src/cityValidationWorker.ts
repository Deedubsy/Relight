/** Separate processes keep each validator's cache and cold-start measurements independent. */
import {cityValidationRun} from './cityValidationRun';
let sample=0;
process.on('message',(seed:number)=>process.send?.({...cityValidationRun(seed),processSample:sample++,pid:process.pid}));
