namespace Relight.Sim
{
    /// <summary>
    /// C-04's half of the C-05 repair seams declared in <c>Sim/Campaign/HomeCore/HomeCore.cs</c> (coordinator,
    /// 2026-09-14). A defence structure's hit points live in <see cref="TurretState"/>, so the Home folder asks
    /// through these partial methods and this file answers with <see cref="TurretRules"/>:
    /// reference <c>defenceHp</c>/<c>defenceMax</c> for the price card and <c>tickRepair</c>'s machine branch
    /// (<c>m.hp = Math.min(defenceMax(m), defenceHp(m) + DEFENCE.repairHp)</c>) for the heal.
    /// </summary>
    public static partial class HomeCore
    {
        static partial void MachineDefenceHp(GameData d, SimState st, int id, ref double hp, ref double max)
        {
            var m = st.MachineById(id);
            if (m == null) return;
            max = TurretRules.MaxHp(d, m);
            hp = max > 0 ? TurretRules.Hp(d, st, m) : 0;
        }

        static partial void MachineHealDefence(SimContext ctx, SimState st, int id, double amount, ref bool applied)
        {
            applied = TurretRules.TurretRepairHook(ctx, st, id, amount) > 0;
        }
    }

    /// <summary>
    /// Reference <c>repairCheck</c>/<c>tickRepair</c>: a knocked-out core cannot be recommissioned while the raid
    /// that took it down is still on the map (<c>d.major?.block===core.block || d.minor?.block===core.block</c>).
    /// Phase C has one Home block, so before E-18 "a live raid body of either layer" was the whole test — and a
    /// single survivor parked anywhere on the map blocked the repair for ever (ENM-03). E-18 (U-D-64 d): only a raid
    /// body within <see cref="SiegeTuning.CoreThreatTiles"/> of the core's edge blocks it. Site guards never did.
    /// </summary>
    public static partial class Home
    {
        static partial void AttackersNearbyHook(SimContext ctx, SimState st, ref bool near)
        {
            var h = st.Home;
            if (h == null || !h.Placed) return;
            var reach = ctx.Data.Siege.CoreThreatTiles;
            for (var i = 0; i < st.Enemies.Actors.Count; i++)
            {
                var e = st.Enemies.Actors[i];
                if (e.Layer != EnemyLayer.Major && e.Layer != EnemyLayer.Minor) continue;
                // Distance from the body to the nearest point of the core rect.
                var dx = System.Math.Max(System.Math.Max(h.X - e.Pos.X, 0), e.Pos.X - (h.X + h.W));
                var dy = System.Math.Max(System.Math.Max(h.Y - e.Pos.Y, 0), e.Pos.Y - (h.Y + h.H));
                if (dx * dx + dy * dy <= reach * reach) { near = true; return; }
            }
        }
    }
}
