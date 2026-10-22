using UnityEngine;

namespace ChastainRush
{
    // The map's terrain is a grid of tiles, so Terrain.activeTerrain no longer answers "how high is the ground here".
    // Development checks use this instead; it finds the tile that contains the point.
    public static class GroundQuery
    {
        public static bool TryHeight(float x, float z, out float y)
        {
            var t = TileAt(x, z, out var p);
            if (t) { y = t.SampleHeight(new Vector3(x, 0f, z)) + p.y; return true; }
            y = 0f;
            return false;
        }

        // Round 61 (C, the map extension stage 2): the tile under (x, z) from a grid of the tiles made once (and again when a tile it holds is
        // gone or switched off). It searched Terrain.activeTerrains (a new array of every tile) on every call: with 304 tiles the police road
        // network's 142,000 heights took 12 minutes at start-up, and every height asked for in a frame (the AI, traffic, walkers, resets) cost it too.
        static Terrain[,] tiles; static Vector3[,] corners; static float gx0, gz0, tw = 1f, td = 1f; static int nx, nz;
        public static Terrain TileAt(float x, float z, out Vector3 corner)
        {
            for (int pass = 0; pass < 2; pass++)
            {
                if (tiles == null) BuildTiles();
                int i = Mathf.FloorToInt((x - gx0) / tw), j = Mathf.FloorToInt((z - gz0) / td);
                // on a shared edge the tile below / to the west answers, as the search did (its edge sample is the same)
                if (i == nx && i > 0 && x <= gx0 + nx * tw + 1e-3f) i--; if (j == nz && j > 0 && z <= gz0 + nz * td + 1e-3f) j--;
                if (i < 0 || j < 0 || i >= nx || j >= nz) { corner = default; return null; }
                var t = tiles[i, j];
                if (t && t.isActiveAndEnabled) { corner = corners[i, j]; return t; }
                if (t == null && pass == 0 && nx * nz > 0 && !Stale()) { corner = default; return null; }   // a hole in the grid: no tile there
                tiles = null;   // a tile gone or switched off: made again
            }
            corner = default; return null;
        }
        static bool Stale() { foreach (var t in tiles) if (t != null && (!t || !t.isActiveAndEnabled)) return true; return Terrain.activeTerrains.Length != Count(); }
        static int Count() { int c = 0; foreach (var t in tiles) if (t) c++; return c; }
        static void BuildTiles()
        {
            var all = Terrain.activeTerrains;
            if (all.Length == 0) { tiles = new Terrain[0, 0]; nx = nz = 0; return; }
            float x0 = float.MaxValue, z0 = float.MaxValue, x1 = float.MinValue, z1 = float.MinValue;
            foreach (var t in all) { var p = t.transform.position; var s = t.terrainData.size; x0 = Mathf.Min(x0, p.x); z0 = Mathf.Min(z0, p.z); x1 = Mathf.Max(x1, p.x + s.x); z1 = Mathf.Max(z1, p.z + s.z); tw = s.x; td = s.z; }
            gx0 = x0; gz0 = z0; nx = Mathf.Max(1, Mathf.RoundToInt((x1 - x0) / tw)); nz = Mathf.Max(1, Mathf.RoundToInt((z1 - z0) / td));
            tiles = new Terrain[nx, nz]; corners = new Vector3[nx, nz];
            foreach (var t in all)
            {
                var p = t.transform.position; int i = Mathf.Clamp(Mathf.RoundToInt((p.x - x0) / tw), 0, nx - 1), j = Mathf.Clamp(Mathf.RoundToInt((p.z - z0) / td), 0, nz - 1);
                tiles[i, j] = t; corners[i, j] = p;
            }
        }

        public static float Height(float x, float z) => TryHeight(x, z, out var y) ? y : float.NaN;

        // Bounding rectangle (x, z, width, depth) of all terrain tiles.
        public static Rect Extent()
        {
            Rect r = default;
            bool first = true;
            foreach (var t in Terrain.activeTerrains)
            {
                var p = t.transform.position;
                var s = t.terrainData.size;
                var tr = new Rect(p.x, p.z, s.x, s.z);
                if (first) { r = tr; first = false; }
                else r = Rect.MinMaxRect(Mathf.Min(r.xMin, tr.xMin), Mathf.Min(r.yMin, tr.yMin), Mathf.Max(r.xMax, tr.xMax), Mathf.Max(r.yMax, tr.yMax));
            }
            return r;
        }
    }
}
