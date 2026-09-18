namespace Relight.Sim
{
    /// <summary>
    /// The introductory group has been announced. <c>Direction</c> is the compass WORD ("north-east"), the same one
    /// the guide card shows, so the HUD and the card never disagree. Reference campaignThreat.ts:216's
    /// <c>status = 'scheduled'</c> branch, which had no event: the reference re-derived the text every frame.
    /// </summary>
    public sealed record OpeningScheduledEvent(double T, string Direction, double ArrivesAt) : SimEvent(T);

    /// <summary>The bodies are on the map (reference campaignThreat.ts:224 <c>status = 'active'</c>).</summary>
    public sealed record OpeningStartedEvent(double T, string Direction, int Count) : SimEvent(T);

    /// <summary>
    /// The group is gone. <c>Repelled</c> is false when the prepared turret was destroyed (the reference keys this
    /// off the CORE's hit points; see <see cref="OpeningPhase"/> for why the port keys it off the turret).
    /// </summary>
    public sealed record OpeningEndedEvent(double T, bool Repelled, int Shots) : SimEvent(T);

    /// <summary>
    /// U-D-26: the encounter is held until the area is safe rather than cancelled. <c>UntilT</c> is the earliest
    /// second the clock can currently prove it might resume — an estimate, re-derived each tick.
    /// </summary>
    public sealed record OpeningDeferredEvent(double T, double UntilT, string Notice) : SimEvent(T);

    /// <summary>
    /// A produced bullet reached a turret through belts or inserters for the first time (reference
    /// openingEncounter.ts:78 <c>noteTurretSupply</c>). Hand loading never raises this.
    /// </summary>
    public sealed record ResupplyWorkingEvent(double T, int TurretId) : SimEvent(T);
}
