using System;
using System.Collections.Generic;
using Relight.Sim.UI;
using UnityEngine.UIElements;

namespace Relight.UI.FrontEnd
{
    /// <summary>
    /// C-10. Paints a list of <see cref="SaveRow"/> into a container using <c>SaveRowItem.uxml</c>. The title
    /// screen's Load list, the pause menu's Load list and the pause menu's Save list are the same list with a
    /// different primary action, so they are the same code (UI_AND_ONBOARDING.md §2.6.2, §2.7.1).
    ///
    /// Two rules are enforced here and nowhere else:
    /// <list type="bullet">
    /// <item><b>A row is never hidden and never deleted by the game.</b> A save that cannot be opened is shown
    ///       with its reason and its <i>Load</i> disabled. Its <i>Delete</i> stays enabled on purpose: the
    ///       prohibition in §2.6.2 is on the game quietly removing a file, not on the player choosing to. A
    ///       player who cannot delete a broken save has a Load screen that can never be cleaned up.</item>
    /// <item><b>The reason shown is the store's own text</b>, via
    ///       <see cref="SaveRowFormatter.DisabledReason"/> — which for a map mismatch is
    ///       <see cref="SaveSerializer.MapProblem"/>'s sentence. No comparison is made here.</item>
    /// </list>
    /// </summary>
    public static class SaveListView
    {
        /// <summary>Dimmed row: the save is there, it simply will not open in this build.</summary>
        public const string DisabledClass = "row-disabled";

        /// <summary>The newest autosave (§2.7.1's Latest tag).</summary>
        public const string LatestClass = "row-latest";

        /// <summary>
        /// Fill <paramref name="container"/> with one element per row and return how many were added.
        /// <paramref name="onPrimary"/> is Load (or Overwrite, on the Save screen); a null
        /// <paramref name="onDelete"/> hides the Delete button entirely, which is what the Save screen wants.
        /// <paramref name="deleteBlocked"/> returns "" when Delete is allowed and the player-facing reason when
        /// it is not (§2.6.4: never for the save this session is running).
        /// </summary>
        public static int Paint(
            VisualTreeAsset template,
            VisualElement container,
            IReadOnlyList<SaveRow> rows,
            string currentMapId,
            double daySeconds,
            SaveRow latest,
            string primaryText,
            Action<SaveRow> onPrimary,
            Action<SaveRow> onDelete,
            Func<SaveRow, string> deleteBlocked = null,
            Func<SaveRow, string> primaryBlocked = null)
        {
            if (container == null) return 0;
            container.Clear();
            if (template == null || rows == null) return 0;

            for (var i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                if (row == null) continue;
                var tree = template.Instantiate();
                // Instantiate() wraps the authored root in a TemplateContainer; it must not take a share of the
                // list's height, or every row would stretch.
                tree.style.flexGrow = 0;
                var element = tree.Q<VisualElement>("save-row") ?? tree;

                var line = tree.Q<Label>("row-line");
                if (line != null) line.text = SaveRowFormatter.Line(row, daySeconds, ReferenceEquals(row, latest));

                var reason = SaveRowFormatter.DisabledReason(row, currentMapId);
                var detail = tree.Q<Label>("row-detail");
                if (detail != null)
                {
                    detail.text = SaveRowFormatter.Detail(row, currentMapId);
                    detail.EnableInClassList("hidden", detail.text.Length == 0);
                }

                var reasonLabel = tree.Q<Label>("row-reason");
                if (reasonLabel != null)
                {
                    reasonLabel.text = reason;
                    reasonLabel.EnableInClassList("hidden", reason.Length == 0);
                }

                element.EnableInClassList(DisabledClass, reason.Length != 0);
                element.EnableInClassList(LatestClass, ReferenceEquals(row, latest));

                var primary = tree.Q<Button>("row-primary");
                if (primary != null)
                {
                    primary.text = primaryText;
                    // By default the primary action is Load, so the store's own refusal is what disables it.
                    // The in-game Save screen passes its own test instead: a save made on another map cannot be
                    // LOADED here, but it can perfectly well be OVERWRITTEN, and disabling it there would be a
                    // refusal the sim never made.
                    var blockedPrimary = primaryBlocked == null ? reason : primaryBlocked(row) ?? "";
                    primary.SetEnabled(blockedPrimary.Length == 0);
                    primary.tooltip = blockedPrimary;
                    var captured = row;
                    primary.clicked += () => onPrimary?.Invoke(captured);
                }

                var delete = tree.Q<Button>("row-delete");
                if (delete != null)
                {
                    if (onDelete == null)
                    {
                        delete.AddToClassList("hidden");
                    }
                    else
                    {
                        var blocked = deleteBlocked == null ? "" : deleteBlocked(row) ?? "";
                        delete.SetEnabled(blocked.Length == 0);
                        delete.tooltip = blocked;
                        var captured = row;
                        delete.clicked += () => onDelete(captured);
                    }
                }

                container.Add(tree);
            }
            return container.childCount;
        }
    }
}
