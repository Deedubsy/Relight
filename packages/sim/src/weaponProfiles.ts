/** Provisional player weapon tuning; turret balance lives in recipes.ts. Distances are world tiles. */
export const WEAPON_PROFILES = {
 rifle: {name:'Rifle',effective:18,max:30,damage:10,rate:2.5,pellets:1,spread:0,radius:.65,speed:0},
 double: {name:'Two-barrel shotgun',effective:6,max:12,damage:6,rate:1.25,pellets:5,spread:.36,radius:.45,speed:0},
 arc: {name:'Arc projector',effective:9,max:9,damage:10,rate:2.5,pellets:1,spread:0,radius:.6,speed:0},
 plasma: {name:'Plasma lance',effective:14,max:14,damage:25,rate:1,pellets:1,spread:0,radius:.45,speed:12},
} as const;
export type WeaponKind = keyof typeof WEAPON_PROFILES;
export function weaponRangeText(kind:WeaponKind):string {
 const p=WEAPON_PROFILES[kind];
 return `${p.name} · effective ${p.effective} tiles${p.max>p.effective?` · damage falls to zero at ${p.max} tiles`:kind==='plasma'?` · ${p.speed} tiles/s projectile · ${p.max} tile limit`:` · hard reach limit ${p.max} tiles`}${p.pellets>1?' · five-pellet spread':''}`;
}
export function weaponDamage(kind:WeaponKind,distance:number):number {
 const p=WEAPON_PROFILES[kind];if(distance>p.max)return 0;
 return p.damage*(distance<=p.effective?1:Math.max(0,(p.max-distance)/(p.max-p.effective)));
}
