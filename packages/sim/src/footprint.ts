/** Derived footprints preserve old square-machine saves without adding redundant dimensions. */
export function dimensions(kind: string, dir: number, size: number): [number, number] {
  return kind === 'splitter' ? dir % 2 === 0 ? [2, 1] : [1, 2] : [size, size];
}
export function machineDimensions(m: { kind: string; dir: number; size: number }): [number, number] {
  return dimensions(m.kind, m.dir, m.size);
}
