using System;

namespace Relight.Sim
{
    /// <summary>
    /// Everything a tick or a command handler needs besides the state: the immutable <see cref="GameData"/> built at
    /// boot, the world geometry injected (never compiled in — U-M-14, R9), and the two explicit layer seams.
    /// Holds no mutable gameplay state. It is itself immutable: a tuning reload (REL-84,
    /// <see cref="Simulation.ReplaceData"/>) builds a new one around the new data and the same world, so nothing
    /// may cache a context across ticks — read the one handed to each call.
    /// </summary>
    public sealed class SimContext
    {
        public GameData Data { get; }
        public ICityGeometry Geometry { get; }
        public ITileLayer Tiles { get; }
        public IThreatLayer Threat { get; }
        /// <summary>The authored sites of the loaded region (C-01); <see cref="WorldSites.Empty"/> on the synthetic map.</summary>
        public WorldSites Sites { get; }
        /// <summary>The imported region this game runs on, written into every save header; "" on the synthetic map.</summary>
        public string MapId { get; }

        public SimContext(GameData data, ICityGeometry geometry, ITileLayer tiles = null, IThreatLayer threat = null,
            WorldSites sites = null, string mapId = null)
        {
            MapId = mapId ?? "";
            Data = data ?? throw new ArgumentNullException(nameof(data));
            Geometry = geometry ?? throw new ArgumentNullException(nameof(geometry));
            Tiles = tiles ?? new NoTileLayer();
            Threat = threat ?? new NoThreatLayer();
            Sites = sites ?? WorldSites.Empty;
        }
    }
}
