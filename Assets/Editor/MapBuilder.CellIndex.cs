using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Build speed (after Round 61's map extension): items (segments, rings, points) filed by the square cells their box covers, so a question near
// a point looks only at the items near it instead of every item on the map. Near() gives the candidates in the order the items were added, so a
// first-match or best-so-far loop over them answers exactly as the same loop over the whole list did.
namespace ChastainRush
{
    public static partial class MapBuilder
    {
        sealed class CellIndex
        {
            readonly float cell;
            readonly Dictionary<long, List<int>> grid = new Dictionary<long, List<int>>();
            int[] stamp = new int[64]; int query;
            public int Count { get; private set; }
            public CellIndex(float cell) { this.cell = cell; }

            static long Key(int x, int z) => ((long)x << 32) ^ (uint)z;

            // an item covering the box (x0, z0) to (x1, z1); returns its index (0, 1, 2 ... in the order added)
            public int Add(float x0, float z0, float x1, float z1)
            {
                int id = Count++;
                for (int x = Mathf.FloorToInt(x0 / cell), xe = Mathf.FloorToInt(x1 / cell); x <= xe; x++)
                    for (int z = Mathf.FloorToInt(z0 / cell), ze = Mathf.FloorToInt(z1 / cell); z <= ze; z++)
                    {
                        long k = Key(x, z);
                        if (!grid.TryGetValue(k, out var l)) grid[k] = l = new List<int>();
                        l.Add(id);
                    }
                return id;
            }
            public int Add(Vector2 p) => Add(p.x, p.y, p.x, p.y);
            public int Add(Vector2 a, Vector2 b, float pad) => Add(Mathf.Min(a.x, b.x) - pad, Mathf.Min(a.y, b.y) - pad, Mathf.Max(a.x, b.x) + pad, Mathf.Max(a.y, b.y) + pad);
            public int Add(List<Vector2> ring, float pad)
            {
                float x0 = float.MaxValue, z0 = float.MaxValue, x1 = float.MinValue, z1 = float.MinValue;
                foreach (var q in ring) { x0 = Mathf.Min(x0, q.x); z0 = Mathf.Min(z0, q.y); x1 = Mathf.Max(x1, q.x); z1 = Mathf.Max(z1, q.y); }
                return Add(x0 - pad, z0 - pad, x1 + pad, z1 + pad);
            }

            // every item whose box touches a cell the box (x0, z0) to (x1, z1) touches, each once, in the order added
            public List<int> Near(float x0, float z0, float x1, float z1, List<int> into = null)
            {
                into = into ?? new List<int>(); into.Clear();
                if (stamp.Length < Count) System.Array.Resize(ref stamp, Mathf.Max(Count, stamp.Length * 2));
                if (++query == int.MaxValue) { System.Array.Clear(stamp, 0, stamp.Length); query = 1; }
                for (int x = Mathf.FloorToInt(x0 / cell), xe = Mathf.FloorToInt(x1 / cell); x <= xe; x++)
                    for (int z = Mathf.FloorToInt(z0 / cell), ze = Mathf.FloorToInt(z1 / cell); z <= ze; z++)
                        if (grid.TryGetValue(Key(x, z), out var l))
                            foreach (int id in l) if (stamp[id] != query) { stamp[id] = query; into.Add(id); }
                into.Sort();
                return into;
            }
            public List<int> Near(Vector2 p, float r, List<int> into = null) => Near(p.x - r, p.y - r, p.x + r, p.y + r, into);
        }

        // a track file's cleared or breakable lines (points, half width), turned into points once and each line skipped when the point is
        // outside its box padded by its half width: OnAny gives what the loop over every segment of every line gave
        sealed class LineSet
        {
            readonly List<(List<Vector2> p, float half, Rect box)> lines = new List<(List<Vector2>, float, Rect)>();
            public LineSet(IEnumerable<(float[] points, float half)> src)
            {
                foreach (var (points, half) in src)
                {
                    var p = ToPoints(points); if (p.Count == 0) { lines.Add((p, half, Rect.zero)); continue; }
                    float x0 = p.Min(q => q.x), z0 = p.Min(q => q.y), x1 = p.Max(q => q.x), z1 = p.Max(q => q.y), pad = Mathf.Abs(half) + 0.05f;
                    lines.Add((p, half, Rect.MinMaxRect(x0 - pad, z0 - pad, x1 + pad, z1 + pad)));
                }
            }
            public bool OnAny(Vector2 q)
            {
                foreach (var (p, half, box) in lines)
                {
                    if (!box.Contains(q)) continue;
                    for (int i = 0; i + 1 < p.Count; i++) if (SegDist(q, p[i], p[i + 1]) < half) return true;
                }
                return false;
            }
        }
    }
}
