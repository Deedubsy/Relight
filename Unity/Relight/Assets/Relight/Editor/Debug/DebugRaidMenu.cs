using Relight.Presentation;
using Relight.Sim;
using UnityEditor;
using UnityEngine;

namespace Relight.Editor
{
    /// <summary>
    /// C-08 editor trigger. Stages a raid on demand so the director's forty-minute clock is not in the way of
    /// testing the raid itself. It is editor-only three times over: the file lives in an Editor-platform assembly
    /// so it cannot ship in a build; the command it submits is refused by the sim unless
    /// <see cref="DirectorState.DebugAllowed"/> has been set, which nothing in a normal game ever sets; and every
    /// answer it produces carries <see cref="DebugRaidHandler.Label"/>, so a debug wave can never be mistaken for
    /// a scheduled one in a log or on screen.
    ///
    /// It goes through <see cref="SimHost.Submit"/> like any other command: the sim alone mutates gameplay
    /// (CLAUDE.md), so this does not reach into <c>SimState</c> to place bodies. Two things it does touch
    /// directly, both editor-only and both named in the report: the permission flag, which is a debug permission
    /// rather than a gameplay fact, and <see cref="Director.Purge"/> for "clear staged raid", which has no command
    /// because no player action can ever perform it.
    /// </summary>
    public static class DebugRaidMenu
    {
        private const string Root = "Relight/Debug/";
        private const int Order = 200;

        [MenuItem(Root + "Trigger raid (editor only)", false, Order)]
        private static void TriggerRaid() => Stage(8, false, -1);

        [MenuItem(Root + "Trigger raid (editor only)", true, Order)]
        private static bool CanTriggerRaid() => Host() != null;

        [MenuItem(Root + "Trigger small raid — skitters only (editor only)", false, Order + 1)]
        private static void TriggerSmallRaid() => Stage(4, true, -1);

        [MenuItem(Root + "Trigger small raid — skitters only (editor only)", true, Order + 1)]
        private static bool CanTriggerSmallRaid() => Host() != null;

        /// <summary>
        /// Clears the wave staged by the menu. Ordinary raids leave on their own; a debug wave is often dropped on
        /// a half-built base and needs to be taken away again without reloading the scene.
        /// </summary>
        [MenuItem(Root + "Clear staged raid (editor only)", false, Order + 20)]
        private static void ClearRaid()
        {
            var host = Host();
            var sim = host == null ? null : host.Simulation;
            if (sim == null) return;
            var group = sim.State.Director.Minor;
            if (group == null || !group.Scripted)
            {
                UnityEngine.Debug.Log("Relight: no staged debug raid to clear (" + DebugRaidHandler.Label + ").");
                return;
            }
            Director.Purge(sim.State, group.Id);
            UnityEngine.Debug.Log("Relight: staged raid cleared (" + DebugRaidHandler.Label + ").");
        }

        [MenuItem(Root + "Clear staged raid (editor only)", true, Order + 20)]
        private static bool CanClearRaid() => Host() != null;

        private static void Stage(int count, bool basic, int sector)
        {
            var host = Host();
            var sim = host == null ? null : host.Simulation;
            if (sim == null)
            {
                UnityEngine.Debug.LogWarning("Relight: no running simulation — enter Play Mode first ("
                    + DebugRaidHandler.Label + ").");
                return;
            }
            // The permission the sim checks. Set here rather than anywhere in the game, so a build that somehow
            // included this file still could not raise it.
            sim.State.Director.DebugAllowed = true;
            host.Submit(new DebugRaidCommand(count, basic, sector));
            UnityEngine.Debug.Log("Relight: debug raid submitted — " + count + (basic ? " basic" : "") + " bodies ("
                + DebugRaidHandler.Label + "). The sim refuses it if a group is already on the map.");
        }

        private static SimHost Host() =>
            Application.isPlaying ? Object.FindAnyObjectByType<SimHost>() : null;
    }
}
