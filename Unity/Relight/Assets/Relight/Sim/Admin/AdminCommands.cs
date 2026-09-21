using System;
using System.Collections.Generic;

namespace Relight.Sim
{
    // Temporary testing overrides reset on new game/load. Granted stock and spawned actors save normally.
    public sealed class AdminState
    {
        public bool Invulnerable, FreezeEnemies;
        public int Lighting = -1;
        /// <summary>E-19: large raids survived, as the next booked raid is to be sized by; -1 follows the history.</summary>
        public int RaidNumber = -1;
        public string LastAction = "";
        public int Actions;
    }
    public sealed partial class SimState { public readonly AdminState Admin = new AdminState(); }
    public sealed record AdminCommand(string Action, string Key = "", int Amount = 1, int Direction = 0, int Distance = 8) : Command;

    public sealed class AdminHandler : ICommandHandler
    {
        public bool TryApply(SimContext ctx, SimState st, Command command, out CommandResult result)
        {
            if (!(command is AdminCommand c)) { result=default; return false; }
            result=Execute(ctx,st,c);
            if(result.Accepted) { st.Admin.LastAction=result.Problem; st.Admin.Actions++; }
            return true;
        }
        private static CommandResult Execute(SimContext ctx, SimState st, AdminCommand c)
        {
            var d=ctx.Data;
            switch(c.Action)
            {
                case "grant":
                    if(c.Amount<1 || c.Amount>10000) return CommandResult.Refuse("Choose 1–10,000 items.");
                    if(!Items.TryParse(c.Key,out _) && !d.TryMachine(c.Key,out _)) return CommandResult.Refuse("Unknown item or structure.");
                    var key=new ItemKey(c.Key);
                    var granted=Pockets.Take(d,st.Engineer,key,c.Amount);
                    if(granted<=0) return CommandResult.Refuse("Backpack full. Make space, then try again.");
                    if(Items.TryParse(c.Key,out var item)) Credit(st,item,granted);
                    return CommandResult.Ok($"Admin: added {granted:0}/{c.Amount} {c.Key} to Backpack."+(granted<c.Amount?" Backpack full; remainder was not added.":""));
                case "materials":
                    double sum=0;
                    foreach(var name in new[]{"steel","copper","coal","magazine"})
                    {
                        Items.TryParse(name,out var k);var n=Pockets.Take(d,st.Engineer,new ItemKey(name),name=="magazine"?100:50);
                        Credit(st,k,n);sum+=n;
                    }
                    return sum>0 ? CommandResult.Ok($"Admin: added {sum:0}/250 starter supplies; amounts respect Backpack capacity.") : CommandResult.Refuse("Backpack full.");
                case "weapon":
                    if(!d.TryWeapon(c.Key,out var weapon))return CommandResult.Refuse("Unknown weapon.");
                    var next=c.Key+":"+st.Weapons.Next;
                    var trial=Pockets.Trial(st.Engineer);
                    if(Pockets.Take(d,trial,new ItemKey(next),1)!=1)return CommandResult.Refuse("Backpack full. A weapon needs one slot.");
                    var id=WeaponRules.Create(st,c.Key);Pockets.Take(d,st.Engineer,new ItemKey(id),1);
                    return CommandResult.Ok("Admin: added "+weapon.DisplayName+". Equip it from Backpack.");
                case "spawn": return Spawn(ctx,st,c);
                case "raid":
                    if(c.Amount<1 || c.Amount>100 || c.Direction<0 || c.Direction>3)return CommandResult.Refuse("Choose 1–100 enemies and a valid approach.");
                    var allowed=st.Director.DebugAllowed;st.Director.DebugAllowed=true;
                    try { new DebugRaidHandler().TryApply(ctx,st,new DebugRaidCommand(c.Amount,false,c.Direction),out var raid);return raid; }
                    finally { st.Director.DebugAllowed=allowed; }
                // E-19: the director page. These move the director's CLOCK; the raid itself is the game's own.
                case "major-now":
                    if(st.Director.Major!=null)return CommandResult.Refuse("A large raid is already warned or under way. Use Skip the wait, or clear it first.");
                    // A microsecond short of a full warning: (T + W) - W can round to just above T, and the booking would slip a tick.
                    var warned=Director.PullIn(ctx,st,st.T+d.Raids.WarningS-1e-6);
                    return warned.Length>0 ? CommandResult.Refuse(warned) : CommandResult.Ok($"Admin: a large raid will be warned on the next tick and starts in {d.Raids.WarningS:0} s.");
                case "skip-clock":
                    var pulled=Director.PullIn(ctx,st,st.T+DirectorRules.AdminSkipLeadS);
                    return pulled.Length>0 ? CommandResult.Refuse(pulled) : CommandResult.Ok($"Admin: the next large raid starts in {DirectorRules.AdminSkipLeadS:0} s. Its plan is unchanged.");
                case "raid-number":
                    if(c.Amount < -1 || c.Amount > 50)return CommandResult.Refuse("Choose 0–50 raids survived, or -1 to follow the history.");
                    st.Admin.RaidNumber=c.Amount;
                    return CommandResult.Ok(c.Amount<0?"Admin: raid size follows the history again.":$"Admin: the next large raid booked is sized as if {c.Amount} had been survived ({SiegePlan.TotalFor(ctx,c.Amount)} bodies). A raid already warned keeps its plan.");
                case "clear-enemies":
                    var count=st.Enemies.Actors.Count;st.Enemies.Actors.Clear();st.Enemies.Projectiles.Clear();
                    // Clearing a scheduled assault must also cancel its unspawned remainder.
                    st.Director.Minor=null;st.Director.Major=null;st.Director.MajorSpawned=0;
                    st.Director.NextStart=st.T+d.Raids.RecoveryS+d.Raids.WarningS;
                    st.Director.NextMinor=Math.Max(st.Director.NextMinor,st.T+d.Raids.RecoveryS);
                    return CommandResult.Ok($"Admin: removed {count} enemies and hostile projectiles; active raids cancelled. No kill rewards.");
                case "heal":
                    st.Engineer.Hp=d.Engineer.MaxHp;st.Engineer.Down=-1;st.Engineer.LastHit=st.T;
                    return CommandResult.Ok("Admin: engineer healed and revived.");
                case "invulnerable":st.Admin.Invulnerable=c.Amount!=0;return CommandResult.Ok("Admin: invulnerability "+(st.Admin.Invulnerable?"ON":"OFF")+".");
                case "freeze":st.Admin.FreezeEnemies=c.Amount!=0;return CommandResult.Ok("Admin: enemy movement and attacks "+(st.Admin.FreezeEnemies?"frozen":"resumed")+".");
                case "lighting":
                    if(c.Amount < -1 || c.Amount > 1)return CommandResult.Refuse("Invalid lighting mode.");
                    st.Admin.Lighting=c.Amount;return CommandResult.Ok("Admin: lighting "+(c.Amount<0?"follows the clock":c.Amount==1?"forced to daylight":"forced to night")+". Raid timers unchanged.");
                case "reset-overrides":st.Admin.Invulnerable=false;st.Admin.FreezeEnemies=false;st.Admin.Lighting=-1;st.Admin.RaidNumber=-1;return CommandResult.Ok("Admin: temporary overrides reset. Granted items and spawned enemies remain.");
                case "repair":
                    if(st.Home.Placed){st.Home.Hp=d.Defence.CoreHp;st.Home.DisabledAt=-1;}
                    foreach(var m in st.Machines)TurretRules.TurretRepairHook(ctx,st,m.Id,1000000);
                    st.Rev++;return CommandResult.Ok("Admin: Home core and all placed defences repaired.");
                case "fuel":
                    double fuel=0;
                    foreach(var m in st.Machines)if(PowerGrid.IsSource(d,m))
                    {var n=Math.Max(0,MachineInventory.GeneratorFuelCap(d)-m.Inv[ItemId.Coal]-m.Inv[ItemId.Fuel]);m.Inv.Add(ItemId.Coal,n);fuel+=n;}
                    Credit(st,ItemId.Coal,fuel);return CommandResult.Ok($"Admin: added {fuel:0.#} coal to placed generators.");
                case "ammo":
                    double rounds=0;
                    foreach(var m in st.Machines)if(TurretHopper.IsTurret(d,m))
                    {var n=Math.Max(0,TurretHopper.Capacity(d,m)-m.Rounds);m.Rounds+=n;Credit(st,TurretHopper.Ammo(d,m),n);rounds+=n;}
                    foreach(var w in st.Weapons.Owned)if(d.TryWeapon(w.Kind,out var def))
                    {var n=Math.Max(0,def.Capacity-w.Loaded);w.Loaded+=n;w.Reload=0;w.Cooldown=0;Credit(st,ItemId.Magazine,n);rounds+=n;}
                    return CommandResult.Ok($"Admin: added {rounds:0} rounds to owned weapons and placed turrets.");
                case "home":
                    var home=ctx.Geometry.Spawn;
                    for(var radius=0;radius<=10;radius++)for(var y=(int)home.Y-radius;y<=(int)home.Y+radius;y++)for(var x=(int)home.X-radius;x<=(int)home.X+radius;x++)
                    {
                        if(!Ground.CanStand(ctx,st,x+.5,y+.5,.3))continue;
                        st.Engineer.Pos=new Vec2(x+.5,y+.5);st.Engineer.Vel=Vec2.Zero;st.Engineer.Plan=null;st.Engineer.HasTarget=false;
                        Mining.Stop(st,"");st.Weapons.Firing=false;
                        return CommandResult.Ok("Admin: returned to clear ground near Home.");
                    }
                    return CommandResult.Refuse("No safe space near Home.");
                default:return CommandResult.Refuse("Unknown admin action.");
            }
        }
        private static void Credit(SimState st,ItemId item,double count)
        {
            // Explain only this explicit grant; do not erase any existing conservation discrepancy or fake production.
            if(st.Ledger==null)st.Ledger=new LedgerOpening();
            st.Ledger.Base.Add(item,count);
        }
        private static CommandResult Spawn(SimContext ctx,SimState st,AdminCommand c)
        {
            if(!ctx.Data.TryEnemy(c.Key,out var def))return CommandResult.Refuse("Unknown enemy type.");
            if(c.Amount<1 || c.Amount>100 || c.Direction<0 || c.Direction>3 || c.Distance<3 || c.Distance>30)
                return CommandResult.Refuse("Use 1–100 enemies, 3–30 tiles distance and a valid direction.");
            var limit=Math.Min(c.Amount,Math.Max(0,250-st.Enemies.Actors.Count));
            if(limit==0)return CommandResult.Refuse("Admin spawn limit reached: 250 living enemies.");
            var ex=st.Engineer.Pos.X+Dirs.DX[c.Direction]*c.Distance;var ey=st.Engineer.Pos.Y+Dirs.DY[c.Direction]*c.Distance;
            var spawned=0;
            for(var radius=0;radius<=10 && spawned<limit;radius++)
            for(var y=(int)ey-radius;y<=(int)ey+radius && spawned<limit;y++)
            for(var x=(int)ex-radius;x<=(int)ex+radius && spawned<limit;x++)
            {
                if(Math.Max(Math.Abs(x-(int)ex),Math.Abs(y-(int)ey))!=radius)continue;
                if(!Ground.CanStand(ctx,st,x+.5,y+.5,.3))continue;
                var pos=new Vec2(x+.5,y+.5);var dx=pos.X-st.Engineer.Pos.X;var dy=pos.Y-st.Engineer.Pos.Y;
                if(dx*dx+dy*dy<9)continue;
                var occupied=false;foreach(var enemy in st.Enemies.Actors)if(Math.Abs(enemy.Pos.X-pos.X)<.8 && Math.Abs(enemy.Pos.Y-pos.Y)<.8){occupied=true;break;}
                if(occupied)continue;
                var id=st.Enemies.Next++;var body=new Enemy{Id=id,Kind=c.Key,Hp=def.Hp,Pos=pos,Home=pos,Aim=pos,Origin=y*ctx.Geometry.Width+x,Layer=EnemyLayer.Site,Waypoint=-1};
                st.Enemies.Actors.Add(body);st.Events.Add(new EnemySpawnedEvent(st.T,id,c.Key,pos.X,pos.Y,0));spawned++;
            }
            return spawned>0 ? CommandResult.Ok($"Admin: spawned {spawned}/{c.Amount} {def.DisplayName} on clear nearby ground.") : CommandResult.Refuse("No clear spawn space in that direction. Try another side or distance.");
        }
    }
}
