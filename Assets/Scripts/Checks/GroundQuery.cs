using UnityEngine;

namespace ChastainRush
{
    // The map's terrain is a grid of tiles, so Terrain.activeTerrain no longer answers "how high is the ground here".
    // Development checks use this instead; it finds the tile that contains the point.
    public static class GroundQuery
    {
        public static bool TryHeight(float x, float z, out float y)
        {
            foreach (var t in Terrain.activeTerrains)
            {
                var p = t.transform.position;
                var s = t.terrainData.size;
                if (x >= p.x && x <= p.x + s.x && z >= p.z && z <= p.z + s.z)
                {
                    y = t.SampleHeight(new Vector3(x, 0f, z)) + p.y;
                    return true;
                }
            }
            y = 0f;
            return false;
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
