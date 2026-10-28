using System.Collections.Generic;
using System.Linq;
using ChastainRush;
using UnityEngine;

namespace Racer
{
    // Racer's RoadNet (0.96 Part E), built from this map's roads (PoliceRoads) instead of Racer's few RaceRoads: each road sampled every 10 m into nodes
    // (on the ground: RaceRoute.Surface, bridges included), consecutive nodes joined, nodes of different roads (or far apart on one road) that meet within
    // 9 m and 5 m of height joined (where a cop may turn), and every road's end joined to the nearest node of another road within 24 m. A* finds the way;
    // "on the network" is within a road's width of one of its nodes. Cops never leave it (CopDriver follows the paths); the runner can.
    public sealed class RoadNet
    {
        public const float Step = 10, JoinDistance = 9, EndJoinDistance = 24, OnRoadDistance = 15;
        public struct Edge { public int to; public float cost; }
        public readonly List<Vector3> P = new();
        public readonly List<int> Road = new();            // index into PoliceRoads
        public readonly List<float> Half = new();          // half width there
        public readonly List<List<Edge>> Adj = new();
        public readonly List<string> RoadNames = new();    // per road (PoliceRoads' names; unnamed: its class)
        public readonly List<string> RoadClasses = new();
        public int[] Component;
        readonly Dictionary<long, List<int>> grid = new(); const float Cell = 24;
        public int Count => P.Count;
        static long Key(int x, int z) => ((long)x << 32) ^ (uint)z;
        static RoadNet cached;
        public static RoadNet Shared => cached ??= Build(PoliceRoads.Instance);
        // Chastain Rush, Round 80 (E): a step the paths may not take now (the Express Lanes against the way they run, or onto them while closed: ExpressLanes)
        public static System.Func<RoadNet, int, int, bool> Blocked;

        public static RoadNet Build(PoliceRoads roads)
        {
            var net = new RoadNet(); if (!roads) return net;
            var watch = System.Diagnostics.Stopwatch.StartNew(); var first = new List<int>(); var last = new List<int>();
            for (int r = 0; r < roads.Count; r++)
            {
                net.RoadNames.Add(string.IsNullOrEmpty(roads.names[r]) ? roads.classes[r] : roads.names[r]); net.RoadClasses.Add(roads.classes[r]);
                var pts = new List<Vector2>(); for (int i = roads.first[r]; i < roads.first[r + 1]; i++) pts.Add(roads.points[i]);
                float len = 0; for (int i = 1; i < pts.Count; i++) len += (pts[i] - pts[i - 1]).magnitude;
                if (len < 12) { first.Add(-1); last.Add(-1); continue; }
                int n = Mathf.Max(1, Mathf.RoundToInt(len / Step)); int start = net.P.Count;
                for (int k = 0; k <= n; k++)
                {
                    var q = At(pts, len * k / n); var p = RaceRoute.Surface(q.x, q.y);
                    net.P.Add(p); net.Road.Add(r); net.Half.Add(roads.halfWidths[r]); net.Adj.Add(new List<Edge>());
                }
                for (int k = 0; k < n; k++) net.Link(start + k, start + k + 1, 1f);
                first.Add(start); last.Add(start + n);
            }
            net.BuildGrid();
            for (int i = 0; i < net.Count; i++)
                foreach (int j in net.Near(net.P[i], JoinDistance))
                {
                    if (j <= i || Mathf.Abs(net.P[i].y - net.P[j].y) > 5) continue;
                    if (net.Road[i] == net.Road[j] && Mathf.Abs(i - j) * Step < 100) continue;
                    net.Link(i, j, net.Road[i] == net.Road[j] ? 1f : 1.6f);   // (Racer: x5 between its parallel loop and highway; here roads meet at their junctions)
                }
            for (int r = 0; r < first.Count; r++)
                foreach (int end in new[] { first[r], last[r] })
                {
                    if (end < 0) continue; int best = -1; float bd = EndJoinDistance;
                    foreach (int j in net.Near(net.P[end], EndJoinDistance)) { if (net.Road[j] == r) continue; float d = Vector3.Distance(net.P[end], net.P[j]); if (d < bd && Mathf.Abs(net.P[end].y - net.P[j].y) < 7) { bd = d; best = j; } }
                    if (best >= 0) net.Link(end, best, 1.2f);
                }
            net.Components();
            Debug.Log($"[Police] road network: {net.Count} nodes on {roads.Count} roads, {net.ComponentCount} parts (largest {net.largestSize} nodes), {watch.ElapsedMilliseconds} ms");
            return net;
        }
        static Vector2 At(List<Vector2> pts, float s)
        {
            for (int i = 1; i < pts.Count; i++) { float l = (pts[i] - pts[i - 1]).magnitude; if (s <= l || i == pts.Count - 1) return Vector2.Lerp(pts[i - 1], pts[i], l < 1e-6f ? 0 : Mathf.Clamp01(s / l)); s -= l; }
            return pts[pts.Count - 1];
        }
        void Link(int a, int b, float scale)
        {
            float d = Vector3.Distance(P[a], P[b]) * scale;
            if (!Adj[a].Any(e => e.to == b)) Adj[a].Add(new Edge { to = b, cost = d });
            if (!Adj[b].Any(e => e.to == a)) Adj[b].Add(new Edge { to = a, cost = d });
        }
        void BuildGrid()
        {
            grid.Clear();
            for (int i = 0; i < P.Count; i++) { long k = Key(Mathf.FloorToInt(P[i].x / Cell), Mathf.FloorToInt(P[i].z / Cell)); if (!grid.TryGetValue(k, out var l)) grid[k] = l = new List<int>(); l.Add(i); }
        }
        public IEnumerable<int> Near(Vector3 p, float radius)
        {
            int cx = Mathf.FloorToInt(p.x / Cell), cz = Mathf.FloorToInt(p.z / Cell), r = Mathf.CeilToInt(radius / Cell);
            float r2 = radius * radius;
            for (int x = cx - r; x <= cx + r; x++)
                for (int z = cz - r; z <= cz + r; z++)
                    if (grid.TryGetValue(Key(x, z), out var l))
                        foreach (int i in l) { var d = P[i] - p; if (d.x * d.x + d.z * d.z <= r2) yield return i; }
        }
        public int Nearest(Vector3 p, out float distance, float search = 400, float maxVertical = 9)
        {
            int best = -1; distance = float.MaxValue;
            for (float r = 30; r <= search + 1; r *= 2)
            {
                foreach (int i in Near(p, r)) { var d = P[i] - p; float h = Mathf.Sqrt(d.x * d.x + d.z * d.z); if (Mathf.Abs(d.y) > maxVertical) continue; if (h < distance) { distance = h; best = i; } }
                if (best >= 0 && distance <= r) break;
            }
            return best;
        }
        public bool OnNet(Vector3 p, out int node, float extra = 3f) { node = Nearest(p, out float d, 60); if (node < 0) return false; return d <= Half[node] + extra + 5f; }
        public float OffNet(Vector3 p) { int n = Nearest(p, out float d, 300); return n < 0 ? 999 : Mathf.Max(0, d - Half[n] - 4f); }
        public Vector3 Tangent(int node)
        {
            int best = -1; float bd = -1; foreach (var e in Adj[node]) if (Road[e.to] == Road[node]) { float d = (P[e.to] - P[node]).sqrMagnitude; if (d > bd) { bd = d; best = e.to; } }
            Vector3 f = best >= 0 ? (best > node ? P[best] - P[node] : P[node] - P[best]) : Vector3.forward; f.y = 0; return f.sqrMagnitude < .0001f ? Vector3.forward : f.normalized;
        }

        // ---------- A* ----------
        readonly List<float> g = new(); readonly List<int> came = new(); readonly List<byte> state = new();
        public List<int> Path(int from, int to, float maxCost = 20000)
        {
            if (from < 0 || to < 0 || from >= Count || to >= Count) return null;
            if (from == to) return new List<int> { from };
            if (Component[from] != Component[to]) return null;
            int n = Count; if (g.Count != n) { g.Clear(); came.Clear(); state.Clear(); for (int i = 0; i < n; i++) { g.Add(0); came.Add(0); state.Add(0); } }
            for (int i = 0; i < n; i++) { g[i] = float.MaxValue; came[i] = -1; state[i] = 0; }
            g[from] = 0; var goal = P[to];
            var heap = new SortedSet<(float f, int node)>(); heap.Add((Vector3.Distance(P[from], goal), from));
            while (heap.Count > 0)
            {
                var top = heap.Min; heap.Remove(top); int cur = top.node;
                if (state[cur] == 2) continue; state[cur] = 2;
                if (cur == to) break;
                foreach (var e in Adj[cur])
                {
                    if (state[e.to] == 2 || Blocked != null && Blocked(this, cur, e.to)) continue; float ng = g[cur] + e.cost; if (ng >= g[e.to] || ng > maxCost) continue;
                    g[e.to] = ng; came[e.to] = cur; heap.Add((ng + Vector3.Distance(P[e.to], goal), e.to));
                }
            }
            if (came[to] < 0) return null;
            var path = new List<int>(); for (int c = to; c >= 0; c = came[c]) { path.Add(c); if (c == from) break; }
            path.Reverse(); return path;
        }
        void Components()
        {
            Component = new int[Count]; for (int i = 0; i < Count; i++) Component[i] = -1; int c = 0; var stack = new Stack<int>();
            for (int i = 0; i < Count; i++)
            {
                if (Component[i] >= 0) continue; Component[i] = c; stack.Push(i);
                while (stack.Count > 0) { int a = stack.Pop(); foreach (var e in Adj[a]) if (Component[e.to] < 0) { Component[e.to] = c; stack.Push(e.to); } }
                c++;
            }
            ComponentCount = c;
            // Chastain Rush, Round 61 (C): the largest part counted once here (it was counted again on every call: the build's own log line asked
            // for it once per node, 142,000 times over the stage 2 map: 12 minutes at start-up)
            var sizes = new int[c]; foreach (int k in Component) sizes[k]++;
            largest = -1; largestSize = 0; for (int k = 0; k < c; k++) if (sizes[k] > largestSize) { largestSize = sizes[k]; largest = k; }
        }
        int largest = -1, largestSize;
        public int ComponentCount { get; private set; }
        public int LargestComponent => Component == null || Count == 0 ? -1 : largest;
        public int Ahead(int node, Vector3 heading, float metres)
        {
            int cur = node; float walked = 0; Vector3 dir = heading; dir.y = 0; if (dir.sqrMagnitude < .01f) dir = Tangent(node); dir.Normalize(); int prev = -1;
            while (walked < metres)
            {
                int best = -1; float bestDot = -2;
                foreach (var e in Adj[cur]) { if (e.to == prev) continue; var d = P[e.to] - P[cur]; d.y = 0; if (d.sqrMagnitude < .01f) continue; float dot = Vector3.Dot(d.normalized, dir); if (dot > bestDot) { bestDot = dot; best = e.to; } }
                if (best < 0 || bestDot < -.2f) break;
                var step = P[best] - P[cur]; step.y = 0; walked += step.magnitude; dir = Vector3.Lerp(dir, step.normalized, .6f).normalized; prev = cur; cur = best;
            }
            return cur;
        }
        public List<int> Around(int node, float radius, float spacing)
        {
            var list = new List<int>(); foreach (int i in Near(P[node], radius)) { if (Component[i] != Component[node]) continue; if (list.All(j => Vector3.Distance(P[i], P[j]) >= spacing)) list.Add(i); }
            return list;
        }
    }
}
