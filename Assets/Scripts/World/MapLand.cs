using System.Collections.Generic;
using UnityEngine;

namespace ChastainRush
{
    // Round 76 (A, Marietta Square: one piece of tiles below the old south edge): the map is no longer a rectangle. MapLand is its land: a union of
    // axis-aligned rectangles (the terrain tiles at run time; map.json's extent_m and pieces in the map build) and its outline as straight segments
    // (a side of a rectangle less the parts another rectangle lies against, collinear pieces joined). Distance(p) is how far p is inside the
    // land's edge (negative: outside), Inward(p) the way back in; Bounds the bounding rectangle.
    public sealed class MapLand
    {
        public struct Seg { public Vector2 a, b, inward; }
        public readonly Rect[] Rects; public readonly Seg[] Edge; public readonly Rect Bounds;

        public MapLand(IList<Rect> rects)
        {
            Rects = new Rect[rects.Count]; rects.CopyTo(Rects, 0);
            var segs = new List<Seg>();
            const float Tol = .05f;
            foreach (var r in Rects)
            {
                // each side as (fixed coordinate, from, to, outward); the parts with another rectangle just across it removed
                Side(segs, r, true, r.yMin, r.xMin, r.xMax, -1f, Tol); Side(segs, r, true, r.yMax, r.xMin, r.xMax, 1f, Tol);
                Side(segs, r, false, r.xMin, r.yMin, r.yMax, -1f, Tol); Side(segs, r, false, r.xMax, r.yMin, r.yMax, 1f, Tol);
            }
            Edge = Join(segs, Tol).ToArray();
            var bb = Rects.Length > 0 ? Rects[0] : default;
            foreach (var r in Rects) bb = Rect.MinMaxRect(Mathf.Min(bb.xMin, r.xMin), Mathf.Min(bb.yMin, r.yMin), Mathf.Max(bb.xMax, r.xMax), Mathf.Max(bb.yMax, r.yMax));
            Bounds = bb;
        }

        // horiz: the side runs along x at z = c (outward -1: south, +1: north); else along z at x = c (outward -1: west, +1: east)
        void Side(List<Seg> segs, Rect r, bool horiz, float c, float lo, float hi, float outward, float tol)
        {
            var keep = new List<Vector2> { new Vector2(lo, hi) };
            foreach (var o in Rects)
            {
                if (o == r) continue;
                // another rectangle across this side: its opposite side on the same line, on the outer side
                float oc = horiz ? (outward < 0f ? o.yMax : o.yMin) : (outward < 0f ? o.xMax : o.xMin);
                if (Mathf.Abs(oc - c) > tol) continue;
                float a = horiz ? o.xMin : o.yMin, b = horiz ? o.xMax : o.yMax;
                var next = new List<Vector2>();
                foreach (var k in keep)
                {
                    if (b <= k.x + tol || a >= k.y - tol) { next.Add(k); continue; }
                    if (a > k.x + tol) next.Add(new Vector2(k.x, a));
                    if (b < k.y - tol) next.Add(new Vector2(b, k.y));
                }
                keep = next;
            }
            foreach (var k in keep)
            {
                if (k.y - k.x < tol) continue;
                segs.Add(horiz ? new Seg { a = new Vector2(k.x, c), b = new Vector2(k.y, c), inward = new Vector2(0f, -outward) }
                               : new Seg { a = new Vector2(c, k.x), b = new Vector2(c, k.y), inward = new Vector2(-outward, 0f) });
            }
        }

        static List<Seg> Join(List<Seg> segs, float tol)
        {
            var left = new List<Seg>(segs); bool merged = true;
            while (merged)
            {
                merged = false;
                for (int i = 0; i < left.Count && !merged; i++)
                    for (int j = i + 1; j < left.Count && !merged; j++)
                    {
                        Seg s = left[i], t = left[j];
                        if ((s.inward - t.inward).sqrMagnitude > 1e-4f) continue;
                        bool horiz = Mathf.Abs(s.inward.x) < .5f;
                        if (horiz ? Mathf.Abs(s.a.y - t.a.y) > tol : Mathf.Abs(s.a.x - t.a.x) > tol) continue;
                        float s0 = horiz ? s.a.x : s.a.y, s1 = horiz ? s.b.x : s.b.y, t0 = horiz ? t.a.x : t.a.y, t1 = horiz ? t.b.x : t.b.y;
                        if (t0 > s1 + tol || s0 > t1 + tol) continue;
                        float a = Mathf.Min(s0, t0), b = Mathf.Max(s1, t1);
                        left[i] = horiz ? new Seg { a = new Vector2(a, s.a.y), b = new Vector2(b, s.a.y), inward = s.inward } : new Seg { a = new Vector2(s.a.x, a), b = new Vector2(s.a.x, b), inward = s.inward };
                        left.RemoveAt(j); merged = true;
                    }
            }
            return left;
        }

        public bool Contains(float x, float z) { foreach (var r in Rects) if (x >= r.xMin && x <= r.xMax && z >= r.yMin && z <= r.yMax) return true; return false; }

        static float SegDist(Vector2 p, Seg s, out Vector2 at)
        {
            var ab = s.b - s.a; float t = Mathf.Clamp01(Vector2.Dot(p - s.a, ab) / Mathf.Max(ab.sqrMagnitude, 1e-6f)); at = s.a + ab * t; return (p - at).magnitude;
        }

        // metres inside the land's edge (negative past it)
        public float Distance(float x, float z) => Distance(x, z, out _);
        public float Distance(float x, float z, out Seg nearest)
        {
            var p = new Vector2(x, z); float best = float.MaxValue; nearest = default;
            foreach (var s in Edge) { float d = SegDist(p, s, out _); if (d < best) { best = d; nearest = s; } }
            if (Edge.Length == 0) return float.MaxValue;
            return Contains(x, z) ? best : -best;
        }

        // the way in: inside, the sides within 'zone' each pull by how near (a corner pulls two ways); outside, toward the nearest point of the edge
        public Vector2 Inward(float x, float z, float zone)
        {
            var p = new Vector2(x, z); var sum = Vector2.zero; float best = float.MaxValue; Vector2 bestAt = p, bestIn = Vector2.up;
            foreach (var s in Edge)
            {
                float d = SegDist(p, s, out var at);
                if (d < best) { best = d; bestAt = at; bestIn = s.inward; }
                if (d < zone) sum += s.inward * (1f - d / zone);
            }
            if (!Contains(x, z)) { var v = bestAt - p; return v.sqrMagnitude > 1e-6f ? v.normalized : bestIn; }
            return sum.sqrMagnitude > 1e-6f ? sum.normalized : bestIn;
        }

        // a point 'inset' inside the land nearest p (for a vehicle put back, or a place to turn toward)
        public Vector2 Nearest(float x, float z, float inset)
        {
            float d = Distance(x, z, out var s); if (d >= inset) return new Vector2(x, z);
            SegDist(new Vector2(x, z), s, out var at); return at + s.inward * inset;
        }

        // the land at run time: every terrain tile of the scene (switched off ones too: FarTerrain draws far tiles its own way), made once per scene
        static MapLand live; static UnityEngine.SceneManagement.Scene liveScene;
        public static MapLand Live
        {
            get
            {
                var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
                if (live == null || liveScene != scene)
                {
                    var rects = new List<Rect>();
                    foreach (var t in Object.FindObjectsByType<Terrain>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                        if (t.terrainData) { var p = t.transform.position; var s = t.terrainData.size; rects.Add(new Rect(p.x, p.z, s.x, s.z)); }
                    if (rects.Count == 0) return null;
                    live = new MapLand(rects); liveScene = scene;
                }
                return live;
            }
        }
    }
}
