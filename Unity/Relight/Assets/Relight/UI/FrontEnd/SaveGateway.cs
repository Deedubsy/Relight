using System;
using System.Collections.Generic;
using Relight.Sim;
using Relight.Sim.UI;

namespace Relight.UI.FrontEnd
{
    /// <summary>
    /// C-10. The Load and Save screens' one way in to the save store. It exists so that the title screen — which
    /// runs in <c>MainMenu</c>, where there is no <c>SimHost</c> and therefore no
    /// <c>AutosaveController</c> — and the pause menu, which has both, read exactly the same list in exactly the
    /// same order.
    ///
    /// It adds no rule of its own. Listing is <see cref="SaveCatalogue"/>'s, refusal text is the store's, and the
    /// map comparison is <see cref="SaveSerializer.MapProblem"/>'s. The only piece of behaviour here is deleting
    /// an autosave, because <see cref="AutosaveStore"/> has no delete: the file and its <c>.bak</c> are removed
    /// through the same <see cref="IFileSystem"/> the store writes with, and the index is then rebuilt from the
    /// files that remain — without that rebuild the ring would keep offering the slot it just lost and would
    /// mis-order which one to overwrite next. The report carries the four-line
    /// <c>AutosaveStore.Delete(fileName)</c> patch that would let this be deleted; it is written this way now
    /// because <c>Sim/Persistence/*</c> is coordinator-owned this wave.
    /// </summary>
    public sealed class SaveGateway
    {
        private readonly IFileSystem _fs;

        public SaveGateway(IFileSystem fs, string rootDirectory, string profile = null)
        {
            _fs = fs ?? new SystemFileSystem();
            Store = new SaveStore(_fs, rootDirectory, profile);
        }

        /// <summary>Wrap a store an <c>AutosaveController</c> already built, so in-game screens share its state.</summary>
        public SaveGateway(SaveStore store, IFileSystem fs = null)
        {
            _fs = fs ?? new SystemFileSystem();
            Store = store;
        }

        public SaveStore Store { get; }

        /// <summary>Manual slots, newest first (§2.7.1's first group).</summary>
        public List<SaveRow> Manual() => SaveCatalogue.Manual(Store == null ? null : Store.List());

        /// <summary>Autosaves, newest first (§2.7.1's second group).</summary>
        public List<SaveRow> Autosaves()
            => SaveCatalogue.Autosaves(Store == null ? null : Store.Autosaves.ListRecoverable());

        /// <summary>Both groups in one list, for <see cref="ContinuePicker"/> (§2.6.3).</summary>
        public List<SaveRow> All()
        {
            var rows = Manual();
            rows.AddRange(Autosaves());
            return rows;
        }

        /// <summary>Delete one row's file. Returns "" or the reason it could not be deleted.</summary>
        public string Delete(SaveRow row)
        {
            if (Store == null || row == null) return "there is no save to delete";
            return row.IsAutosave ? Store.Autosaves.Delete(row.Name) : Store.Delete(row.Name);
        }
    }
}
