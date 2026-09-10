/** Shared keyboard bindings. Handlers and displayed shortcuts share live, validated bindings. */
export const DEFAULT_BINDINGS = {
  copy: ['c'], paste: ['v'], mirrorX: ['h'], mirrorY: ['v'],
  north: ['w', 'ArrowUp'], west: ['a', 'ArrowLeft'], south: ['s', 'ArrowDown'], east: ['d', 'ArrowRight'],
  sprint: ['Shift'], dodge: [' '], pause: ['p'], slower: [], faster: [], map: ['m'],
  threat: ['g'], engineer: ['k'], pockets: ['i', 'Tab'], build: ['b'], debug: ['`'], projects: ['F2'], cancel: ['Escape'], pipette: ['q'], rotate: ['r'],
  recipe: ['t'], interact: ['e'], inspect: ['f'], abort: ['x'], save: ['s'], load: ['o'], undo: ['z'], redo: ['y'],
  belt: ['1'], inserter: ['2'], excavator: ['3'], assembler: ['4'], turret: ['5'], lamp: ['6'], pole: ['7'],
  generator: ['8'], rifle: ['9'], floodlight: ['0'], bigpole: ['['], substation: [']'], chest: ['c'],
  track: ['l'], tramstop: ['h'], tram: ['v'], arclamp: [], wall: [], mixer: [], barricade: [], underground: ['u'], splitter: ['j'],
} as const;
export type Binding = keyof typeof DEFAULT_BINDINGS;
export const BINDINGS:Record<Binding,string[]>=Object.fromEntries(Object.entries(DEFAULT_BINDINGS).map(([k,v])=>[k,[...v]])) as Record<Binding,string[]>;
export const MODIFIED:readonly Binding[]=['copy','paste','save','load','undo','redo'];
export const FIXED:readonly Binding[]=['slower','faster','cancel','belt','inserter','excavator','assembler','turret','lamp','pole','generator','rifle','floodlight'];
export type BindingOverrides=Partial<Record<Binding,string>>;
const context=(a:Binding)=>MODIFIED.includes(a)?'modified':['mirrorX','mirrorY'].includes(a)?'blueprint':'world';
export function bindingProblem(action:Binding,key:string,overrides:BindingOverrides):string {
 if(!Object.prototype.hasOwnProperty.call(DEFAULT_BINDINGS,action)||FIXED.includes(action))return 'Escape and numbered quickbar slots stay fixed; edit slot contents in Build.';
 if(!/^(?:[a-zA-Z]|Arrow(?:Up|Down|Left|Right)|F(?:[1-9]|1[0-2])|Shift| |[-=\[\]`;,.\/])$/.test(key))return 'Use one letter, arrow, F-key, Shift, Space or supported punctuation.';
 if(key==='Escape'||key==='Tab')return 'This key is reserved for interface navigation.';
 const keys=(a:Binding)=>[overrides[a]??DEFAULT_BINDINGS[a][0],...DEFAULT_BINDINGS[a].slice(1)].filter((k):k is string=>!!k);
 for(const other of Object.keys(DEFAULT_BINDINGS) as Binding[]){if(other===action)continue;const c=context(action),d=context(other),overlap=c===d||(c==='blueprint'&&d==='world'&&!['chest','tramstop','tram'].includes(other))||(d==='blueprint'&&c==='world'&&!['chest','tramstop','tram'].includes(action));
  if(overlap&&keys(other).some(k=>k.toLowerCase()===key.toLowerCase()))return `Already used by ${other} in this context.`;
 }return '';
}
export function parseBindings(raw:unknown):BindingOverrides {
 const out:BindingOverrides={};if(!raw||typeof raw!=='object'||Array.isArray(raw))return out;
 for(const [a,k] of Object.entries(raw))if(typeof k==='string'&&!bindingProblem(a as Binding,k,out))out[a as Binding]=k;
 return out;
}
export function applyBindings(overrides:BindingOverrides):void {
 for(const a of Object.keys(DEFAULT_BINDINGS) as Binding[])BINDINGS[a]=[...(DEFAULT_BINDINGS[a] as readonly string[])];
 for(const [a,k] of Object.entries(parseBindings(overrides)))BINDINGS[a as Binding]=[k!,...DEFAULT_BINDINGS[a as Binding].slice(1)];
}
export const bound=(action:Binding,key:string):boolean=>BINDINGS[action].some(k=>k.toLowerCase()===key.toLowerCase());
export const shortcut=(action:Binding):string=>BINDINGS[action][0]===' '?'Space':BINDINGS[action][0]?.toUpperCase()??'';
const code=(key:string)=>({ArrowUp:38,ArrowDown:40,ArrowLeft:37,ArrowRight:39,Shift:16,' ':32,'-':189,'=':187,'[':219,']':221,'`':192,';':186,',':188,'.':190,'/':191} as Record<string,number>)[key]??(/^F\d+$/.test(key)?111+Number(key.slice(1)):key.toUpperCase().charCodeAt(0));
export const movementKeys=()=>({W:code(BINDINGS.north[0]),A:code(BINDINGS.west[0]),S:code(BINDINGS.south[0]),D:code(BINDINGS.east[0]),UP:38,LEFT:37,DOWN:40,RIGHT:39,SHIFT:code(BINDINGS.sprint[0]),SPACE:code(BINDINGS.dodge[0])});
export const MOVEMENT_KEYS=movementKeys();
