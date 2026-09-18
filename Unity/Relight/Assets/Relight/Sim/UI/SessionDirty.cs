namespace Relight.Sim.UI
{
    /// <summary>
    /// C-10. "Unsaved progress" — the one fact the five confirmations of UI_AND_ONBOARDING.md §2.6.4 turn on.
    ///
    /// The definition is deliberately the plainest one that cannot lie: <b>the sim has ticked or a paused admin edit occurred since the last save
    /// or load</b>. Ticks only run unpaused (SimHost), so reading a menu never makes a session dirty, and a load
    /// counts as a save point because the state on screen is exactly the state in that file. Wall-clock time is
    /// not used: a player who pauses for an hour has lost nothing.
    ///
    /// Engine-free on purpose — it takes the tick count as a number — so the branching the confirmations do can be
    /// asserted without a host. The Unity screens feed it <c>SimHost.TotalTicks</c> and tell it when
    /// <c>AutosaveController.Save</c> / <c>Load</c> / an autosave succeeded.
    /// </summary>
    public sealed class SessionDirty
    {
        private long _ticksAtSave;
        private bool _everSaved;
        private bool _changedWithoutTick;

        public void MarkChanged() => _changedWithoutTick = true;
        private string _savedAtIso = "";

        /// <summary>The tick count at the last successful save or load.</summary>
        public long TicksAtSave => _ticksAtSave;

        /// <summary>True once this session has been saved or loaded at least once.</summary>
        public bool EverSaved => _everSaved;

        /// <summary>The ISO time of that save, or "" — the <c>{last save time}</c> of the §2.6.4 bodies.</summary>
        public string SavedAtIso => _savedAtIso;

        /// <summary>That time in the player's own format, or "" when the session has never been saved.</summary>
        public string SavedAtText => SaveRowFormatter.SavedAtText(_savedAtIso);

        /// <summary>A save, an autosave or a load succeeded: this is the new save point.</summary>
        public void Saved(long totalTicks, string savedAtIso)
        {
            _ticksAtSave = totalTicks < 0 ? 0 : totalTicks;
            _everSaved = true;
            _changedWithoutTick = false;
            _savedAtIso = savedAtIso ?? "";
        }

        /// <summary>A new session started: nothing has been saved, and tick zero is not a save point.</summary>
        public void Reset()
        {
            _ticksAtSave = 0;
            _everSaved = false;
            _changedWithoutTick = false;
            _savedAtIso = "";
        }

        /// <summary>
        /// True when discarding the session would lose something. A never-saved session is dirty the moment it
        /// has ticked at all; a session that has never ticked has nothing to lose either way.
        /// </summary>
        public bool IsDirty(long totalTicks)
        {
            if (_changedWithoutTick) return true;
            if (!_everSaved) return totalTicks > 0;
            return totalTicks > _ticksAtSave;
        }
    }
}
