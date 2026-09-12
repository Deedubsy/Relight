/** Shared player-facing names. Save/recipe identifiers stay unchanged. */
const names: Record<string, string> = {
 overclock:'Overclock Module', alienartifact:'Alien Artifact',
 steel:'Steel plates', copper:'Copper', stone:'Stone', coal:'Coal', iron:'Iron',
 ironore:'Iron ore', copperore:'Copper ore', crude:'Crude oil', fuel:'Refined fuel',
 polymer:'Polymer', shell:'Shells', magazine:'Bullets', rounds:'Rounds',
 wire:'Wire', frame:'Frame', board:'Board', concrete:'Concrete',
 core1:'Power core 1', core2:'Power core 2', core3:'Power core 3',
 artifact1:'Speed artifact 1', artifact2:'Speed artifact 2', artifact3:'Speed artifact 3',
};
export const itemName = (id: string): string => names[id] ?? (/^rifle:\d+$/.test(id)?'Rifle':/^double:\d+$/.test(id)?'Two-barrel shotgun':/^arc:\d+$/.test(id)?'Arc projector':/^plasma:\d+$/.test(id)?'Plasma lance':undefined) ?? id.charAt(0).toUpperCase()+id.slice(1);
