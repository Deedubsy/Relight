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
    /// Phase C has one Home block, so "a live raid body of either layer" is the whole test.
    /// </summary>
    public static partial class Home
    {
        static partial void AttackersNearbyHook(SimContext ctx, SimState st, ref bool near)
        {
            DirectorQueries.Live(st, out var major, out var minor, out _);
            near = major > 0 || minor > 0;
        }
    }
}
