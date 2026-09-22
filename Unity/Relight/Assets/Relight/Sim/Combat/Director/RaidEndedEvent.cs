namespace Relight.Sim
{
    /// <summary>
    /// REL-85 (CMB-09c, E-19): a raid that reached the ground is over. Raised once per raid by
    /// <c>DirectorPhase</c> at the moment it drops the raid — a large raid that COMMITTED when <c>EndMajor</c>
    /// writes its <see cref="RaidRecord"/> (even if none of its bodies were ever born), a small raid whose bodies
    /// arrived when its last body is gone — and by nothing else. A raid dropped before that (a small raid skipped
    /// for want of open ground, a large raid that could not commit) leaves no event: its
    /// <see cref="RaidNoticeEvent"/> of kind <c>Skipped</c> or its <c>Future</c> notice already says what happened,
    /// and it fought nobody.
    ///
    /// The event is what <see cref="RaidLog"/> closes a line on. Like every event it is drained by the host and
    /// never saved; the large raid's saved history stays the <see cref="RaidRecord"/> list.
    /// </summary>
    /// <param name="RaidId">The group id the raid's bodies carry.</param>
    /// <param name="Major">True for a large raid (<see cref="MajorRaid"/>), false for a small one.</param>
    /// <param name="Scripted">True for the opening's scripted encounter (C-09), which never counts as a raid started.</param>
    /// <param name="Outcome">A <see cref="RaidOutcome"/> as an int: for a large raid the value written to its record;
    /// for a small raid Cleared unless its retreat was called, and then <c>DirectorRules.CalledOff</c>'s answer.</param>
    /// <param name="StartedAt">Sim second the raid began: a large raid's start time, a small raid's arrival.</param>
    public sealed record RaidEndedEvent(double T, int RaidId, bool Major, bool Scripted, int Outcome, double StartedAt)
        : SimEvent(T);
}
