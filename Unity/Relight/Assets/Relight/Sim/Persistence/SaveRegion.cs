using System.Globalization;
using System.Text;

namespace Relight.Sim
{
    /// <summary>
    /// Correction pass C6. Which imported region a save was made on, and where that region sits in the whole city.
    ///
    /// <see cref="SaveHeader.MapId"/> already says <i>which city</i> a save belongs to, and it is the whole answer
    /// while there is one map. It stops being the whole answer the moment the same city is imported twice at two
    /// different origins: the Home crop (78x367 at (43,91)) and the whole 864x576 city carry the same
    /// <c>mapId</c> — they are the same authored city — but every coordinate in a Home-crop save is region-local,
    /// so loading one on the full map would drop the engineer 43 tiles west and 91 tiles north of where they stood.
    ///
    /// So a schema-4 save records the region as well as the map: its id, its origin in whole-city tiles, and its
    /// size. The size is not decoration. Several saved values are TILE INDICES (<c>y * Width + x</c>) rather than
    /// coordinates — <see cref="GroundState.DugTiles"/>, <see cref="OpeningState.Origin"/>,
    /// <see cref="MajorRaid.Origin"/>/<c>Origins</c>, <see cref="MinorRaid.Origin"/>, <see cref="Enemy.Origin"/>
    /// and <see cref="Enemy.Waypoint"/> — and an index cannot be moved without knowing the width it was formed
    /// against. Contract C6 names <c>{id, originX, originY}</c>; <see cref="Width"/> and <see cref="Height"/> are
    /// added because without them <see cref="SaveRelocate"/> could not honestly move those fields, and a save that
    /// does not carry them is refused rather than guessed at.
    ///
    /// It lives in the header, beside <c>mapId</c>, and not in the state: the state's checksum (contract H1) must go
    /// on meaning exactly what it meant in v3, and a region is a fact about where the save was taken, not a part of
    /// the simulation it describes.
    /// </summary>
    public sealed class SaveRegion
    {
        /// <summary>"This save does not say" — a synthetic-map save, or an older file whose map could not be placed.</summary>
        public static readonly SaveRegion Unknown = new SaveRegion("", 0, 0, 0, 0);

        public SaveRegion(string id, int originX, int originY, int width = 0, int height = 0)
        {
            Id = id ?? "";
            OriginX = originX;
            OriginY = originY;
            Width = width;
            Height = height;
        }

        /// <summary>The imported region's id, as the exporter wrote it ("home", "full"); "" when unknown.</summary>
        public string Id { get; }
        /// <summary>The region's origin in whole-city tiles. Home is (43, 91); the full city is (0, 0).</summary>
        public int OriginX { get; }
        public int OriginY { get; }
        /// <summary>The region's size in tiles, 0 when the save does not record it (see the class remarks).</summary>
        public int Width { get; }
        public int Height { get; }

        /// <summary>True when the save actually named a region.</summary>
        public bool Known => Id.Length > 0;

        /// <summary>True when tile INDICES saved against this region can be re-formed (it recorded its size).</summary>
        public bool HasSize => Width > 0 && Height > 0;

        /// <summary>Same origin — the only thing that decides whether a save has to be moved at all.</summary>
        public bool SameOrigin(SaveRegion other)
            => other != null && other.OriginX == OriginX && other.OriginY == OriginY;

        /// <summary>The region a running game is on, from its sites and its geometry; <see cref="Unknown"/> without one.</summary>
        public static SaveRegion Of(SimContext ctx)
        {
            if (ctx == null) return Unknown;
            var sites = ctx.Sites;
            var map = ctx.Geometry;
            if (sites == null || string.IsNullOrEmpty(sites.RegionId)) return Unknown;
            return new SaveRegion(sites.RegionId, sites.OriginX, sites.OriginY,
                map == null ? 0 : map.Width, map == null ? 0 : map.Height);
        }

        /// <summary>The header member, canonical (keys in ordinal order, as every object this build writes is).</summary>
        public string ToCanonicalJson()
        {
            var sb = new StringBuilder(96);
            sb.Append("{\"height\":").Append(Height.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"id\":").Append(CanonicalJsonWriter.QuoteString(Id));
            sb.Append(",\"originX\":").Append(OriginX.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"originY\":").Append(OriginY.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"width\":").Append(Width.ToString(CultureInfo.InvariantCulture));
            sb.Append('}');
            return sb.ToString();
        }

        /// <summary>Read the header member. A missing, null or malformed member is <see cref="Unknown"/>, never a throw.</summary>
        public static SaveRegion Read(JsonValue member)
        {
            if (member == null || !member.IsObject) return Unknown;
            var id = member.TextOf(SaveSchema.RegionId, "");
            if (string.IsNullOrEmpty(id)) return Unknown;
            return new SaveRegion(id,
                (int)member.NumberOf(SaveSchema.RegionOriginX),
                (int)member.NumberOf(SaveSchema.RegionOriginY),
                (int)member.NumberOf(SaveSchema.RegionWidth),
                (int)member.NumberOf(SaveSchema.RegionHeight));
        }

        public override string ToString()
            => Known
                ? Id + " (" + OriginX.ToString(CultureInfo.InvariantCulture) + ","
                  + OriginY.ToString(CultureInfo.InvariantCulture) + ")"
                : "an unrecorded region";
    }
}
