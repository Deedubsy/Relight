namespace Relight.Sim.UI
{
    /// <summary>
    /// C-10. What the title screen asked for, carried across the scene load.
    ///
    /// The front end lives in <c>MainMenu</c> and the session lives in <c>World</c>, so the seed the player typed
    /// (and the save Continue or Load picked) has to survive a <see cref="UnityEngine.SceneManagement.SceneManager"/>
    /// load. A static is the whole mechanism, and it is here rather than in <c>Relight.UI</c> for one hard reason:
    /// <b>Relight.Presentation cannot reference Relight.UI</b> (that would be circular), and
    /// <c>WorldBootstrap.StartSession()</c> — which is in Presentation — is what must read the seed. Both
    /// assemblies already reference Relight.Sim, so this is the only place both sides can see.
    ///
    /// It holds no Unity type, so it is engine-free and testable; it is a one-shot, and
    /// <see cref="Take"/> clears it so a later Play-in-Editor run of <c>World.unity</c> on its own starts with the
    /// scene's authored seed instead of inheriting whatever the last front-end session asked for.
    ///
    /// The two-line <c>WorldBootstrap</c> patch that consumes it is in the C-10 report; until the coordinator
    /// applies it the request is written and simply ignored, which starts the authored seed — never a wrong one.
    /// </summary>
    public static class NewGameRequest
    {
        /// <summary>The seed the New Game screen offers by default (session.ts:25, <c>Number(q.get('seed') ?? '3')</c>).</summary>
        public const int DefaultSeed = 3;

        /// <summary>True when the title screen set a request that nothing has consumed yet.</summary>
        public static bool Pending { get; private set; }

        /// <summary>The seed to start with. Meaningless unless <see cref="Pending"/>.</summary>
        public static int Seed { get; private set; } = DefaultSeed;

        /// <summary>
        /// The save the world scene should load as soon as it has a session, or null for a genuinely new city.
        /// Continue and Load both set it; the name is the store's own (a manual slot name, or an autosave file).
        /// </summary>
        public static string LoadName { get; private set; }

        /// <summary>True when <see cref="LoadName"/> names an autosave file rather than a manual slot.</summary>
        public static bool LoadIsAutosave { get; private set; }

        /// <summary>
        /// A sentence the front end owes the player once the world is up — at present only §2.6.3's "Your most
        /// recent save could not be opened; continuing from {label}." The title screen is taken off screen by the
        /// scene load within a frame or two, so a notice shown only there would not be read. Whatever surfaces
        /// notices in the world reads this once and clears it.
        /// </summary>
        public static string Notice { get; set; } = "";

        /// <summary>Read <see cref="Notice"/> and clear it; "" when there is nothing to say.</summary>
        public static string TakeNotice()
        {
            var text = Notice ?? "";
            Notice = "";
            return text;
        }

        /// <summary>The title screen's New Game.</summary>
        public static void NewCity(int seed)
        {
            Seed = seed;
            LoadName = null;
            LoadIsAutosave = false;
            Pending = true;
        }

        /// <summary>The title screen's Continue or Load: start a session, then adopt this save into it.</summary>
        public static void LoadSave(string name, bool autosave)
        {
            Seed = DefaultSeed;
            LoadName = name;
            LoadIsAutosave = autosave;
            Pending = true;
        }

        /// <summary>Read the request and clear it. Returns false when nothing was asked for.</summary>
        public static bool Take(out int seed, out string loadName, out bool loadIsAutosave)
        {
            seed = Seed;
            loadName = LoadName;
            loadIsAutosave = LoadIsAutosave;
            var had = Pending;
            Pending = false;
            LoadName = null;
            LoadIsAutosave = false;
            return had;
        }

        /// <summary>Forget any request. Called when the player backs out of the New Game screen.</summary>
        public static void Clear()
        {
            Pending = false;
            Notice = "";
            Seed = DefaultSeed;
            LoadName = null;
            LoadIsAutosave = false;
        }
    }
}
