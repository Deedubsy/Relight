using System;

namespace Relight.Editor
{
    /// <summary>
    /// D-02a. The on-disk shape of <c>Unity/Import/&lt;region&gt;/city.json</c>, as JsonUtility can read it.
    ///
    /// Why these shapes and not WORLD_AND_ASSETS.md §7.2's sketch: this project has no JSON library — only
    /// <c>com.unity.modules.jsonserialize</c> (<see cref="UnityEngine.JsonUtility"/>) — and JsonUtility cannot read
    /// tuple arrays (<c>[[x,y],…]</c>), dictionaries or nulls. The exporter therefore writes every point as
    /// <c>{x,y}</c>, every rect as <c>{x,y,w,h}</c>, every map as a list of <c>{id,…}</c> and never writes null;
    /// the field names and values are otherwise §7.2's. Unknown members in the file are ignored here, so the
    /// exporter may add fields without breaking the importer.
    ///
    /// Every coordinate is REGION-LOCAL and Y-DOWN; nothing in this file is flipped.
    /// </summary>
    [Serializable]
    public sealed class CityFile
    {
        public int schema;
        public string mapId;
        public JRegion region;
        public string sourceSha256;
        public JManifest manifest;
        public int tilePixels;
        public string yAxis;
        public JPoint spawn;
        public JScalars scalars;
        public JTerrain terrain;
        public JBuilding[] buildings;
        public JProp[] props;
        public JRoads roads;
        public JPolyline[] paths;
        public JPolyline[] drives;
        public JTram tram;
        public JSites sites;
        public JCombat combat;
        public JValidation validation;
    }

    [Serializable] public sealed class JPoint { public int x; public int y; }
    [Serializable] public sealed class JRect { public int x; public int y; public int w; public int h; }
    [Serializable] public sealed class JSize { public int width; public int height; }

    /// <summary>A polyline in tile coordinates, exported as <c>{points:[{x,y},…]}</c> (JsonUtility cannot read tuples).</summary>
    [Serializable] public sealed class JPolyline { public JPoint[] points; }

    /// <summary>A baked rail sample: continuous tile coordinates, not a tile index.</summary>
    [Serializable] public sealed class JSample { public float x; public float y; }

    /// <summary>city.json `scalars` — the authored constants the presentation needs (riverfront.ts RIVERFRONT).</summary>
    [Serializable]
    public sealed class JScalars
    {
        public int cell;
        public int roadHalf;
        public int pavement;
        public int setback;
        public int railHalf;
        public float tramRadius;
        public float tramSpeed;
        public float tramDwell;
        public int riverY;
        public JPoint homeOrigin;
        public JCourt court;
        public int homeRaidY;
        public float lightKw;
    }

    [Serializable] public sealed class JCourt { public int x; public int y; public float radius; }

    /// <summary>city.json `roads`: the node graph, its edges, the drawn polylines and the carriageway widths.</summary>
    [Serializable]
    public sealed class JRoads
    {
        public JRoadNode[] nodes;
        public JRoadEdge[] edges;
        public JPolyline[] polylines;
        public int halfWidth;
        public int pavement;
    }

    [Serializable] public sealed class JRoadNode { public string id; public int x; public int y; public string end; }
    [Serializable] public sealed class JRoadEdge { public string from; public string to; }

    /// <summary>city.json `tram`: the authored control polyline plus the baked centreline the art follows.</summary>
    [Serializable]
    public sealed class JTram
    {
        public bool hasControl;
        public JPolyline control;
        public JSample[] baked;
        public float length;
        public float radius;
        public float speed;
        public float dwell;
        public JSite[] stops;
    }

    [Serializable]
    public sealed class JRegion
    {
        public string id;
        public string name;
        public JPoint origin;
        public JSize size;
    }

    [Serializable]
    public sealed class JManifest
    {
        public int version;
        public string sourceHash;
        public string cityId;
    }

    [Serializable]
    public sealed class JTerrain
    {
        /// <summary>Side-car file names, relative to the import folder.</summary>
        public string kind;
        public string variant;
        public string patch;
        public string solid;
        public string[] names;
    }

    [Serializable]
    public sealed class JBuilding
    {
        public string id;
        public string name;
        public string kind;
        public string facing;
        public JRect rect;
        public JRect parcel;
        public JRect visual;
        public JRect doorRect;
        public JRect[] doors;
        /// <summary>The approach path from the door to the street (riverfront.ts building.path).</summary>
        public JPolyline path;
        /// <summary>The door tile itself; meaningless unless <see cref="hasDoor"/>.</summary>
        public JPoint door;
        public bool enterable;
        public bool hasDoor;
        public int variant;
        public string roofKey;
        /// <summary>Campaign compound this building belongs to, or "" (never null: JsonUtility cannot read null).</summary>
        public string compound;
        /// <summary>Recruit/site content kind, or "".</summary>
        public string content;
        /// <summary>Authored note text, or "".</summary>
        public string note;
        /// <summary>`isFixed`, not `fixed`: `fixed` is a C# keyword and JsonUtility binds by exact field name.</summary>
        public bool isFixed;
    }

    [Serializable]
    public sealed class JProp
    {
        public string id;
        public string kind;
        public JRect rect;
        public bool clearable;
    }

    [Serializable]
    public sealed class JSites
    {
        public bool hasCore;
        public JCore core;
        public JSubstation[] substations;
        /// <summary>Authored kerb street lights (C-11); city.json `sites.lights` as [{x,y},…].</summary>
        public JPoint[] lights;
        /// <summary>Campaign power plants (riverfront.ts plants).</summary>
        public JSite[] plants;
        /// <summary>Campaign relay cores (riverfront.ts cores).</summary>
        public JSite[] cores;
        /// <summary>Campaign artifact sites (riverfront.ts artifacts).</summary>
        public JSite[] artifacts;
        public JResource[] resources;
        public JSite[] stops;
        /// <summary>Campaign project markers (riverfront.ts projects); id + tile.</summary>
        public JProject[] projects;
        /// <summary>Recruit markers (riverfront.ts recruits); kind + tile.</summary>
        public JRecruit[] recruits;
        public JLabel[] labels;
        public JRect[] yards;
        /// <summary>Paved civic forecourts and service alleys (riverfront.ts serviceAreas).</summary>
        public JServiceArea[] serviceAreas;
        /// <summary>Paved squares and forecourts (riverfront.ts squares).</summary>
        public JSquare[] squares;
    }

    [Serializable] public sealed class JProject { public string id; public int x; public int y; }
    [Serializable] public sealed class JRecruit { public string kind; public int x; public int y; }
    [Serializable] public sealed class JServiceArea { public int x; public int y; public int w; public int h; public int block; }
    [Serializable] public sealed class JSquare { public int x; public int y; public int w; public int h; public string name; }

    [Serializable]
    public sealed class JCore
    {
        public string id;
        public string name;
        public JRect rect;
        public JRect door;
    }

    [Serializable] public sealed class JSubstation { public int x; public int y; public int size; }

    [Serializable]
    public sealed class JResource
    {
        public string item;
        public int x;
        public int y;
        public int w;
        public int h;
        public int units;
    }

    [Serializable] public sealed class JSite { public string id; public string name; public int x; public int y; }
    [Serializable] public sealed class JLabel { public string name; public int x; public int y; }

    [Serializable]
    public sealed class JCombat
    {
        public JCamp[] firstCamps;
        /// <summary>Both freight warehouse entrances (gameplaySites.ts FREIGHT_GATES).</summary>
        public JRect[] freightGates;
        /// <summary>False when the region does not touch the arena; `freightArena` is then meaningless, not null.</summary>
        public bool hasFreightArena;
        public JArena freightArena;
        public JRaidLine raidLine;
    }

    /// <summary>gameplaySites.ts FREIGHT_ARENA — the stronghold rect, its guardian tile and its spawn groups.</summary>
    [Serializable]
    public sealed class JArena
    {
        public JRect rect;
        public JPoint guardian;
        public int count;
        public JPoint[] groups;
    }

    [Serializable]
    public sealed class JCamp
    {
        public string id;
        public string name;
        public int x;
        public int y;
        public int count;
        public int spawnRadius;
        public JPoint[] groups;
    }

    /// <summary>The opening raid approach row (ground.ts G.opening.gate, homeRaidY).</summary>
    [Serializable] public sealed class JRaidLine { public int y; public int x; public int w; }

    [Serializable]
    public sealed class JValidation
    {
        public int buildings;
        public int props;
        public int land;
        public int accessible;
        public int tiles;
    }
}
