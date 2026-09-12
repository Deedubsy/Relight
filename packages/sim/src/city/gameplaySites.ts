/** Canonical gameplay coordinates, in tiles (editor references multiply by 32).
 * CITY-F buildings, streets, fixed props and resources remain unchanged. */
export const FIRST_CAMPS = [
  {id:'freight:camp:1',name:'West passage camp',x:95,y:286,groups:[[72,287],[97,283],[73,266],[100,267]],count:20},
  {id:'freight:camp:2',name:'Old utility camp',x:96,y:188,groups:[[72,190],[97,190],[74,168],[100,169]],count:22},
  {id:'freight:camp:3',name:'Northwood approach camp',x:95,y:103,groups:[[74,103],[98,103],[74,122],[101,124]],count:20},
] as const;
// Both real warehouse entrances: south three-tile door and west three-tile door.
export const FREIGHT_GATES=[{x:65,y:61,w:3,h:1},{x:56,y:49,w:1,h:3}] as const;
export const FREIGHT_ARENA={x:57,y:40,w:28,h:21,guardian:[77,50],
  groups:[[45,35],[45,58],[98,35],[98,61],[62,58],[79,46]],count:60} as const;
export const inFreightGate=(x:number,y:number)=>FREIGHT_GATES.some(g=>x>=g.x&&x<g.x+g.w&&y>=g.y&&y<g.y+g.h);
