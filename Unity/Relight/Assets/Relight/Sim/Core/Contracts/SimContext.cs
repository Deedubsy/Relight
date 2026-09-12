using System;

namespace Relight.Sim
{
    /// <summary>
    /// Everything a tick or a command handler needs besides the state: the immutable <see cref="GameData"/> built at
    /// boot, the world geometry injected (never compiled in — U-M-14, R9), and the two explicit layer seams.
    /// Constructed once per simulation; holds no mutable gameplay state.
    /// </summary>
    public sealed class SimContext
    {
        public GameData Data { get; }
        public ICityGeometry Geometry { get; }
        public ITileLayer Tiles { get; }
        public IThreatLayer Threat { get; }

        public SimContext(GameData data, ICityGeometry geometry, ITileLayer tiles = null, IThreatLayer threat = null)
        {
            Data = data ?? throw new ArgumentNullException(nameof(data));
            Geometry = geometry ?? throw new ArgumentNullException(nameof(geometry));
            Tiles = tiles ?? new NoTileLayer();
            Threat = threat ?? new NoThreatLayer();
        }
    }
}
