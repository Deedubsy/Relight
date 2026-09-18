namespace Relight.Sim
{
    /// <summary>
    /// A machine that had supply lost it (U-D-12). Raised once, on the tick supply disappears, never on the first
    /// tick of a world: <see cref="PowerState.Supplied"/> starts empty and a machine must have been listed there to
    /// lose anything. The reference had no such event — the TS build showed "Power outage" from the HUD's derived
    /// throttle, which is what produced the "outage at start" defect this port fixes.
    /// </summary>
    public sealed record PowerOutageEvent(double T, int MachineId) : SimEvent(T);

    /// <summary>The counterpart: a machine that had no supply now has some.</summary>
    public sealed record PowerRestoredEvent(double T, int MachineId) : SimEvent(T);

    /// <summary>
    /// A generator burned its last unit of coal/fuel (reference flow.ts:1055 <c>push('gen-dry')</c>).
    /// </summary>
    public sealed record GeneratorDryEvent(double T, int MachineId) : SimEvent(T);
}
