import assert from 'node:assert/strict';
import {writeFileSync} from 'node:fs';
import {createCampaign,ground,passable,citySight,activeWeapon,advanceFlow,newCombatActor,firePlayerWeapon,tickPlayerProjectiles,weaponDamage,WEAPON_PROFILES,addMachine,makeSave,loadState,stateProblem,equipmentCommand,spendRound,type Crawler,type WeaponKind,type WeaponItem} from '../../../packages/sim/src/index';
import {rememberShot,observePlayer} from '../../../packages/sim/src/hostileAwareness';
const s=createCampaign(),e=s.engineer,T=s.flow!.threat!,G=ground(s);let origin:[number,number]|undefined;
for(let y=350;y<390&&!origin;y++)for(let x=50;x<110&&!origin;x++)if(Array.from({length:33},(_,i)=>[-2,-1,0,1,2].every(dy=>passable(s,x+i,y+dy))).every(Boolean)&&citySight(s,x+.5,y+.5,x+32.5,y+.5))origin=[x+.5,y+.5];
assert.ok(origin,'outdoor firing lane in actual CITY-F geometry');[e.x,e.y]=origin;
const q=e.equipment!;q.next=5;for(const [i,kind] of (['rifle','double','arc','plasma'] as const).entries()){const id:WeaponItem=`${kind}:${i+1}`;q.weapons[id]={kind,loaded:10,cooldown:0,reload:0};e.inv[id]=1;}e.inv.magazine=40;
function equip(kind:WeaponKind){const id=Object.keys(q.weapons).find(id=>id.startsWith(kind+':')) as WeaponItem;assert.ok(equipmentCommand(s,{type:'equip',item:id,slot:0}).ok);}
function target(distance:number,offset=0){const c:Crawler={id:T.next++,kind:'crawler',x:e.x+distance,y:e.y+offset,hp:50,edge:-1,from:s.campaign!.defence!.sites[0].block,to:s.campaign!.defence!.sites[0].block,cls:2,onPlayer:false,escaped:false,born:s.t,stuck:0,gp:newCombatActor('spitter','range-check',1,e.x+distance,e.y+offset),campaign:{layer:'site',group:s.campaign!.defence!.sites[0].id,origin:s.campaign!.defence!.sites[0].tile}};T.crawlers.push(c);return c;}
const hit=(c:Crawler|any,n:number,p:[number,number])=>{c.hp-=n;rememberShot(s,T,c,p)};
equip('rifle');
for(const [distance,damage] of [[4,10],[18,10],[24,5],[30,0],[31,0]]){T.crawlers=[];const c=target(distance);firePlayerWeapon(s,T,e.x+40,e.y,hit);assert.ok(Math.abs(50-c.hp-damage)<1e-6,`Rifle ${distance}`);}
equip('double');T.crawlers=[];const cluster=[target(4,-.7),target(4),target(4,.7)];firePlayerWeapon(s,T,e.x+15,e.y,hit);assert.ok(cluster.every(c=>c.hp<50),'spread hits three nearby enemies');assert.equal(weaponDamage('double',9),3);
equip('arc');T.crawlers=[];let c=target(9.1);firePlayerWeapon(s,T,e.x+20,e.y,hit);assert.equal(c.hp,50);c.x=e.x+8;firePlayerWeapon(s,T,e.x+20,e.y,hit);assert.equal(c.hp,40);
equip('plasma');T.crawlers=[];c=target(12);firePlayerWeapon(s,T,e.x+20,e.y,hit);assert.equal(c.hp,50);tickPlayerProjectiles(s,T,.5,hit);assert.equal(c.hp,50);assert.ok(T.playerProjectiles?.length);assert.equal(stateProblem(s),'');const restored=loadState(s);assert.equal(restored.flow!.threat!.playerProjectiles!.length,1);tickPlayerProjectiles(s,T,.55,hit);assert.equal(c.hp,25);assert.equal(T.playerProjectiles!.length,0);
// A distant hit grants only its firing position. Moving beyond sight does not update that point.
T.crawlers=[];c=target(24);rememberShot(s,T,c,[e.x,e.y]);const remembered={...c.lastKnown!};e.y+=8;observePlayer(s,c,8,18);assert.deepEqual(c.lastKnown,remembered);s.t+=6.1;observePlayer(s,c,8,18);assert.equal(c.lastKnown,undefined);s.t-=6.1;e.y-=8;
// Real city collision: a placed wall blocks bullets and the travelling Plasma bolt.
const wall=addMachine(s,'wall',Math.floor(e.x+6),Math.floor(e.y),0);T.crawlers=[];c=target(10);firePlayerWeapon(s,T,e.x+20,e.y,hit);tickPlayerProjectiles(s,T,1,hit);assert.equal(c.hp,50);assert.equal(T.playerProjectiles!.length,0);
equip('rifle');firePlayerWeapon(s,T,e.x+20,e.y,hit);assert.equal(c.hp,50);s.flow!.machines.splice(s.flow!.machines.indexOf(wall),1);s.flow!.rev++;
// Ordinary equipment/aim/tick path, brief close-range combat; no campaign fast-forward.
T.crawlers=[];c=target(4);e.aim=[c.x,c.y];s.speed=1;const loaded=activeWeapon(s)!.loaded;advanceFlow(s,.1,[]);e.aim=null;assert.equal(activeWeapon(s)!.loaded,loaded-1);assert.ok(c.hp<50);assert.equal(stateProblem(s),'');
T.crawlers=[];target(18);e.aim=null;s.speed=0;writeFileSync('docs/evidence/gp-checkpoint/weapon-range-save.json',JSON.stringify(makeSave(s,{logComplete:false})));
console.log(JSON.stringify({outdoorOrigin:origin,profiles:WEAPON_PROFILES,checks:'Rifle close/effective/falloff/max; shotgun cluster; Arc limit; Plasma delayed hit/save/cover; finite enemy memory; ordinary aim/ammo path',turretRange:9}));
