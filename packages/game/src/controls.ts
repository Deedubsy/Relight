/** Shared keyboard bindings. Rebinding UI is later; handlers and displayed shortcuts use this table now. */
export const BINDINGS = {
  north: ['w', 'ArrowUp'], west: ['a', 'ArrowLeft'], south: ['s', 'ArrowDown'], east: ['d', 'ArrowRight'],
  sprint: ['Shift'], dodge: [' '], pause: ['p'], slower: ['-', '_'], faster: ['=', '+'], map: ['m'],
  pockets: ['i', 'Tab'], build: ['b'], debug: ['`'], cancel: ['Escape'], pipette: ['q'], rotate: ['r'],
  recipe: ['t'], interact: ['e'], inspect: ['f'], abort: ['x'], save: ['s'], load: ['o'], undo: ['z'], redo: ['y'],
  belt: ['1'], inserter: ['2'], excavator: ['3'], assembler: ['4'], turret: ['5'], lamp: ['6'], pole: ['7'],
  generator: ['8'], rifle: ['9'], floodlight: ['0'], bigpole: ['['], substation: [']'], chest: ['c'],
  track: ['l'], tramstop: ['h'], tram: ['v'], wall: [], underground: ['u'], splitter: ['j'],
} as const;
export type Binding = keyof typeof BINDINGS;
export const bound = (action: Binding, key: string): boolean => (BINDINGS[action] as readonly string[]).some(k => k.toLowerCase() === key.toLowerCase());
export const shortcut = (action: Binding): string => BINDINGS[action][0]?.toUpperCase() ?? '';
/** Phaser reads these held keys; global key presses above use the same bindings. */
export const MOVEMENT_KEYS = {
  W: shortcut('north'), A: shortcut('west'), S: shortcut('south'), D: shortcut('east'),
  UP: BINDINGS.north[1].replace('Arrow', '').toUpperCase(), LEFT: BINDINGS.west[1].replace('Arrow', '').toUpperCase(),
  DOWN: BINDINGS.south[1].replace('Arrow', '').toUpperCase(), RIGHT: BINDINGS.east[1].replace('Arrow', '').toUpperCase(),
  SHIFT: shortcut('sprint'), SPACE: 'SPACE',
};
