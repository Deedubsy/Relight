namespace Relight.Data
{
    /// <summary>
    /// The catalogue's status for a row (CONTENT_CATALOGUE.md §17.1): `current` is implemented and retained,
    /// `approved` is an accepted addition that is not implemented yet, `provisional` is a starting point that
    /// Unity playtesting is expected to retune (U-M-12, U-D-28).
    /// </summary>
    public enum ContentKind
    {
        Current = 0,
        Approved = 1,
        Provisional = 2,
    }

    /// <summary>Conversion between <see cref="ContentKind"/> and the lower-case strings the sim records carry.</summary>
    public static class ContentKinds
    {
        public static string ToText(ContentKind kind)
        {
            switch (kind)
            {
                case ContentKind.Approved: return "approved";
                case ContentKind.Provisional: return "provisional";
                default: return "current";
            }
        }

        public static ContentKind Parse(string text)
        {
            switch (text)
            {
                case "approved": return ContentKind.Approved;
                case "provisional": return ContentKind.Provisional;
                default: return ContentKind.Current;
            }
        }
    }
}
