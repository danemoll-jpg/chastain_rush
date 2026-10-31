using System.Collections.Generic;
using UnityEngine;

namespace ChastainRush
{
    // Round 6: roads that leave the course are closed while a race runs. The closures are route data (made by
    // Tools/mapgen/make_route.py: one per road that leaves the lap or the lead-in, a little way along it); a closure is
    // skipped only where it would stand across an official shortcut's line (Round 8: it used to be skipped whenever a
    // shortcut started or ended within 16 m of the junction, which opened Chasefield Way next to the Two-house drop). Each barrier is a row of red and white blocks across the road and
    // its verge, with colliders, so the car stops. Built when a race starts, removed in free driving (Racer does the same
    // with its authored course barriers, which are switched on only for the race).
    // Layer note: the barrier is on the default layer like the terrain, so it is solid to the car and to its wheel rays.
    public sealed partial class RaceBarriers : MonoBehaviour
    {
        public const float Height = 1.5f, Depth = 0.9f, Block = 1.5f, Verge = 3f, ShortcutClearM = 3f;   // a shortcut's line this close to a barrier: the barrier is left out
        public int Count => built.Count;
        public int Trimmed { get; private set; }   // Round 73 A1: blocks left out for being on the race's road
        public IReadOnlyList<Barrier> Built => built;
        readonly List<Barrier> built = new List<Barrier>();
        Material red, white;

        public sealed class Barrier
        {
            public RouteClosureJson Data;
            public GameObject Root;
            public Vector3 Centre, Along;     // Along = unit direction of the closed road, away from the course
            public float HalfLength;
        }

        // The skipped ones (an official shortcut starts or ends there) are listed so the report can name them.
        public readonly List<string> Open = new List<string>();

        public void Build(RaceRoute route)
        {
            Clear();
            Open.Clear();
            if (route == null) return;
            if (red == null) { red = Unlit(new Color(0.85f, 0.08f, 0.06f)); white = Unlit(new Color(0.96f, 0.96f, 0.96f)); }
            foreach (var c in route.Closures)
            {
                bool blocks = false;
                var along = new Vector2(c.dx, c.dz).normalized; var across = new Vector2(-along.y, along.x);
                float half = c.width_m * 0.5f + Verge;
                Vector2 a = new Vector2(c.x, c.z) - across * half, b = new Vector2(c.x, c.z) + across * half;
                foreach (var sc in route.Shortcuts)
                    for (int i = 0; i + 1 < sc.Path.Length && !blocks; i++)
                    {
                        // along the line's segments, every 0.5 m (a path can have long straight segments between its points)
                        Vector2 p0 = new Vector2(sc.Path[i].x, sc.Path[i].z), p1 = new Vector2(sc.Path[i + 1].x, sc.Path[i + 1].z);
                        int n = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(p0, p1) / 0.5f));
                        for (int k = 0; k <= n && !blocks; k++) if (DistToSegment(Vector2.Lerp(p0, p1, k / (float)n), a, b) < ShortcutClearM) blocks = true;
                    }
                if (blocks) { Open.Add($"{c.id} {c.label}"); continue; }
                // Round 73 A1 (Dan's BUG-001 / BUG-002 on Kennesaw Mountain: barriers straight across the race's own road): a closure the race's line
                // (the lap or the lead-in) passes through is never built; one whose end reaches the line's road loses the blocks within LineClearM of it
                if (OnLine(route, a, b, c.x, c.z) < 1.5f) { OnCourse.Add($"{c.id} {c.label}"); Debug.LogWarning($"[RaceBarriers] closure {c.id} '{c.label}' is on the race's line: not built"); continue; }
                built.Add((!Old ? AlongCourse(c, route) : null) ?? MakeBarrier(c, route));
            }
            // Round 24 A2 (A3): every pipe mouth this race does not go through is closed too (PipeMouths, from the map build)
            MouthsClosed.Clear();
            var mouths = PipeMouths.Instance;
            if (mouths != null)
            {
                var lines = new List<Vector3[]> { route.Path };
                foreach (var sc in route.Shortcuts) lines.Add(sc.Path);
                foreach (int m in mouths.Unused(lines)) { MakeMouthBarrier(mouths, m); MouthsClosed.Add(mouths.ids[m]); }
            }
            BuildUnderground(route);   // Round 27: the underground race's seals, railings and switch gates (RaceBarriers.Underground.cs)
        }

        public readonly List<string> MouthsClosed = new List<string>();   // Round 24 A2: for the checks and the log
        public readonly List<string> OnCourse = new List<string>();       // Round 73 A1: closures left out because the race drives through them
        public const float LineClearM = 6f;                                // Round 73 A1: no block within this of the race's line

        // Round 73 A1: the nearest the race's own line (the lap, the lead-in) comes to the segment a-b, lines more than 8 m above or below the
        // ground at (x, z) not counting (a bridge over the closed road, a pipe under it)
        static float OnLine(RaceRoute route, Vector2 a, Vector2 b, float x, float z)
        {
            float gy = RaceRoute.Surface(x, z).y, best = float.MaxValue;
            void Scan(Vector3[] path)
            {
                if (path == null) return;
                for (int i = 0; i + 1 < path.Length; i++)
                {
                    Vector2 p0 = new Vector2(path[i].x, path[i].z), p1 = new Vector2(path[i + 1].x, path[i + 1].z);
                    if (Mathf.Abs(path[i].y - gy) > 8f && Mathf.Abs(path[i + 1].y - gy) > 8f) continue;
                    if (Vector2.Distance(p0, a) > 60f && Vector2.Distance(p0, b) > 60f) continue;
                    int n = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(p0, p1) / 0.5f));
                    for (int k = 0; k <= n; k++) best = Mathf.Min(best, DistToSegment(Vector2.Lerp(p0, p1, k / (float)n), a, b));
                }
            }
            Scan(route.Path); Scan(route.LeadIn);
            return best;
        }

        // a row of blocks across a pipe's mouth, on its floor, 0.6 m in (no posts: the cut's walls frame it)
        void MakeMouthBarrier(PipeMouths pm, int m)
        {
            var along = pm.inner[m] - pm.mouth[m]; along.y = 0f; along.Normalize();
            var lateral = Vector3.Cross(Vector3.up, along);
            float half = pm.half[m];
            var root = new GameObject("Mouth barrier " + pm.ids[m]); root.transform.SetParent(transform, false);
            int blocks = Mathf.Max(2, Mathf.RoundToInt(half * 2f / Block));
            float bw = half * 2f / blocks;
            var c = pm.mouth[m] + along * 0.6f; var run = new List<Vector3>();
            for (int i = 0; i < blocks; i++)
            {
                var p = c + lateral * (-half + bw * (i + 0.5f));
                var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.transform.SetParent(root.transform, false);
                cube.transform.SetPositionAndRotation(new Vector3(p.x, c.y + Height * 0.5f - 0.5f, p.z), Quaternion.LookRotation(along, Vector3.up));
                cube.transform.localScale = new Vector3(bw, Height + 1f, Depth);
                var rd = cube.GetComponent<Renderer>();
                rd.sharedMaterial = i % 2 == 0 ? red : white;
                rd.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                Destroy(cube.GetComponent<Collider>());
                run.Add(new Vector3(p.x, c.y, p.z));
            }
            Solid(root.transform, run, bw, along);
        }

        // Round 85 (bumpers and barriers that bounce you back on course): a barrier's blocks were a collider each, standing at their own ground
        // heights, so a vehicle sliding along the row met each block's corner. Now one box for each unbroken run of blocks: one flat face along
        // the row (from 1 m under the lowest block's ground to the barrier's height over the highest), so WallGlance turns a glancing vehicle along it.
        void Solid(Transform root, List<Vector3> run, float bw, Vector3 along)
        {
            if (run.Count == 0) return;
            Vector3 a = run[0], b = run[run.Count - 1]; float lo = float.MaxValue, hi = float.MinValue;
            foreach (var q in run) { lo = Mathf.Min(lo, q.y - 1f); hi = Mathf.Max(hi, q.y + Height); }
            var go = new GameObject("Barrier wall"); go.transform.SetParent(root, false);
            var mid = (a + b) * .5f; go.transform.SetPositionAndRotation(new Vector3(mid.x, (lo + hi) * .5f, mid.z), Quaternion.LookRotation(along, Vector3.up));
            var box = go.AddComponent<BoxCollider>(); box.size = new Vector3(new Vector2(b.x - a.x, b.z - a.z).magnitude + bw, hi - lo, Depth);
            run.Clear();
        }

        public void Clear()
        {
            for (int i = transform.childCount - 1; i >= 0; i--) Destroy(transform.GetChild(i).gameObject);
            built.Clear(); Gates.Clear(); Seals = Rails = 0; OnCourse.Clear(); Trimmed = 0; Rows.Clear();
        }

        static float DistToSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            var ab = b - a; float t = ab.sqrMagnitude < 1e-6f ? 0f : Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
            return (a + ab * t - p).magnitude;
        }

        Barrier MakeBarrier(RouteClosureJson c, RaceRoute route)
        {
            var along = new Vector3(c.dx, 0f, c.dz).normalized;
            var lateral = Vector3.Cross(Vector3.up, along);          // across the closed road
            float half = c.width_m * 0.5f + Verge;
            var root = new GameObject("Barrier " + c.id); root.transform.SetParent(transform, false);
            int blocks = Mathf.Max(2, Mathf.RoundToInt(half * 2f / Block));
            float bw = half * 2f / blocks;
            var run = new List<Vector3>();   // Round 85: the blocks of an unbroken run (x, ground y, z), one collider for them
            for (int i = 0; i < blocks; i++)
            {
                float off = -half + bw * (i + 0.5f);
                Vector3 p = new Vector3(c.x, 0f, c.z) + lateral * off;
                var pl = new Vector2(p.x, p.z);
                if (OnLine(route, pl - new Vector2(lateral.x, lateral.z) * bw * 0.5f, pl + new Vector2(lateral.x, lateral.z) * bw * 0.5f, p.x, p.z) < LineClearM) { Trimmed++; Solid(root.transform, run, bw, along); continue; }   // Round 73 A1
                float gy = RaceRoute.Surface(p.x, p.z).y;
                run.Add(new Vector3(p.x, gy, p.z));
                // each block stands on the ground under it and reaches 1 m below it, so a slope leaves no gap
                var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.transform.SetParent(root.transform, false);
                cube.transform.SetPositionAndRotation(new Vector3(p.x, gy + Height * 0.5f - 0.5f, p.z), Quaternion.LookRotation(along, Vector3.up));
                cube.transform.localScale = new Vector3(bw, Height + 1f, Depth);
                var rd = cube.GetComponent<Renderer>();
                rd.sharedMaterial = i % 2 == 0 ? red : white;
                rd.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                Destroy(cube.GetComponent<Collider>());
            }
            Solid(root.transform, run, bw, along);
            // a tall post at each end so the closure reads from far down the street
            foreach (float side in new[] { -1f, 1f })
            {
                Vector3 p = new Vector3(c.x, 0f, c.z) + lateral * (side * half);
                if (OnLine(route, new Vector2(p.x, p.z), new Vector2(p.x, p.z), p.x, p.z) < LineClearM) continue;   // Round 73 A1
                float gy = RaceRoute.Surface(p.x, p.z).y;
                var post = GameObject.CreatePrimitive(PrimitiveType.Cube);
                post.transform.SetParent(root.transform, false);
                post.transform.SetPositionAndRotation(new Vector3(p.x, gy + 1.6f, p.z), Quaternion.LookRotation(along, Vector3.up));
                post.transform.localScale = new Vector3(0.35f, 3.4f, 0.35f);
                post.GetComponent<Renderer>().sharedMaterial = red;
                Destroy(post.GetComponent<Collider>());
            }
            return new Barrier { Data = c, Root = root, Centre = new Vector3(c.x, RaceRoute.Surface(c.x, c.z).y, c.z), Along = along, HalfLength = half };
        }

        // Round 87 (A, Dan's BUG-008 on Barrett Parkway: "It should be out of the way not have an edge sticking in the middle of the road"; and "barriers
        // and bumpers shouldn't be at an angle that would cause you to run into them head on when following a course"): a closure was a row square to
        // the CLOSED road, so where that road leaves at a shallow angle (a fork, a ramp, a turn lane) the row ran out across the course's own lanes and
        // the blocks trimmed off the line left a zigzag with ends in the road. Now the row stands across the closed road's mouth ALONG the course: at
        // the course's paved edge plus SetBack (the course's road there, else a 4 m half width), following the course's curve, as long as the closed
        // road (its width and the verges) meets that edge, never within the course's paved half and 1.2 m of any pass of the course's line. A closure
        // more than MouthReach from the lap (a lead-in's), or one whose mouth cannot be found, keeps Round 85's row.
        public static bool Old;   // checks (-r87Old)
        public const float SetBack = 2.2f, MouthReach = 45f;
        Barrier AlongCourse(RouteClosureJson c, RaceRoute route)
        {
            var roads = RoadLookup.Instance; var at = new Vector3(c.x, 0f, c.z);
            float sC = route.LapStationOf(at); var pC = route.PointAt(route.GateS + sC, out var fC);
            float toLap = new Vector2(pC.x - c.x, pC.z - c.z).magnitude;
            if (toLap > MouthReach) return null;
            if (route.LeadIn != null && route.LeadIn.Length > 1 && OnPath(route.LeadIn, new Vector2(c.x, c.z)) < toLap) return null;   // the lead-in's
            float side = Mathf.Sign(Vector3.Dot(at - pC, Vector3.Cross(Vector3.up, fC))); if (side == 0f) side = 1f;
            var along = new Vector2(c.dx, c.dz).normalized; var c2 = new Vector2(c.x, c.z); float roadHalf = c.width_m * .5f + Verge;
            float Half(Vector3 p) => roads && roads.Nearest(p, out _, out float d, out float h) && d < h + .5f ? h : 4f;
            // the row's stations along the course: within the closed road (and its verges) where it crosses the course's edge
            var pts = new List<float>();
            for (float u = -MouthReach; u <= MouthReach; u += .5f)
            {
                var p = route.PointAt(route.GateS + sC + u, out var f); var right = Vector3.Cross(Vector3.up, f);
                var q = p + right * side * (Half(p) + SetBack); var q2 = new Vector2(q.x, q.z);
                float across = Mathf.Abs(Vector2.Dot(q2 - c2, new Vector2(-along.y, along.x)));   // from the closed road's middle line
                if (across <= roadHalf) pts.Add(u);
            }
            if (pts.Count < 2) return null;
            // the unbroken stretch nearest the closure
            int best = 0; for (int i = 1; i < pts.Count; i++) if (Mathf.Abs(pts[i]) < Mathf.Abs(pts[best])) best = i;
            int i0 = best, i1 = best; while (i0 > 0 && pts[i0 - 1] > pts[i0] - .6f) i0--; while (i1 + 1 < pts.Count && pts[i1 + 1] < pts[i1] + .6f) i1++;
            float u0 = pts[i0], u1 = pts[i1], length = u1 - u0;
            if (length < 2f) return null;
            int blocks = Mathf.Max(2, Mathf.RoundToInt(length / Block)); float bw = length / blocks;
            var root = new GameObject("Barrier " + c.id); root.transform.SetParent(transform, false);
            var run = new List<Vector3>(); Vector3 runAlong = Vector3.zero;
            Vector3 first = Vector3.zero, last = Vector3.zero; bool any = false;
            for (int i = 0; i < blocks; i++)
            {
                float u = u0 + bw * (i + .5f);
                var p = route.PointAt(route.GateS + sC + u, out var f); var right = Vector3.Cross(Vector3.up, f); float hp = Half(p);
                var q = p + right * side * (hp + SetBack);
                // never on any pass of the course (its paved half and 1.2 m), nor across an official shortcut's line
                bool clear = OnLine(route, new Vector2(q.x, q.z), new Vector2(q.x, q.z), q.x, q.z) >= hp + 1.2f && !NearShortcut(route, new Vector2(q.x, q.z));
                if (!clear) { Trimmed++; SolidAlong(root.transform, run, bw, runAlong); continue; }
                float gy = RaceRoute.Surface(q.x, q.z).y;
                // a run of blocks is one straight box: a new one where the course bends more than 6 degrees from the run's start
                if (run.Count > 0 && Vector3.Angle(runAlong, f) > 6f) SolidAlong(root.transform, run, bw, runAlong);
                if (run.Count == 0) runAlong = f;
                run.Add(new Vector3(q.x, gy, q.z));
                var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.transform.SetParent(root.transform, false);
                cube.transform.SetPositionAndRotation(new Vector3(q.x, gy + Height * 0.5f - 0.5f, q.z), Quaternion.LookRotation(right * side, Vector3.up));
                cube.transform.localScale = new Vector3(bw + .05f, Height + 1f, Depth);
                var rd = cube.GetComponent<Renderer>(); rd.sharedMaterial = i % 2 == 0 ? red : white; rd.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                Destroy(cube.GetComponent<Collider>());
                if (!any) first = q; last = q; any = true;
            }
            SolidAlong(root.transform, run, bw, runAlong);
            if (!any) { Destroy(root); return null; }
            // a tall post at each end, a little further out (it reads from far down the street; never nearer the road than the row)
            foreach (var e in new[] { first, last })
            {
                var pe = route.PointAt(route.GateS + route.LapStationOf(e), out var fe); var o = e - pe; o.y = 0f; o = o.sqrMagnitude > 1e-4f ? o.normalized : Vector3.Cross(Vector3.up, fe) * side;
                var q = e + o * .4f; float gy = RaceRoute.Surface(q.x, q.z).y;
                var post = GameObject.CreatePrimitive(PrimitiveType.Cube); post.transform.SetParent(root.transform, false);
                post.transform.SetPositionAndRotation(new Vector3(q.x, gy + 1.6f, q.z), Quaternion.LookRotation(o, Vector3.up));
                post.transform.localScale = new Vector3(0.35f, 3.4f, 0.35f); post.GetComponent<Renderer>().sharedMaterial = red; Destroy(post.GetComponent<Collider>());
            }
            Rows.Add(new Row { id = c.id, a = first, b = last });
            return new Barrier { Data = c, Root = root, Centre = new Vector3(c.x, RaceRoute.Surface(c.x, c.z).y, c.z), Along = new Vector3(along.x, 0f, along.y), HalfLength = length * .5f };
        }
        public sealed class Row { public string id; public Vector3 a, b; }
        public readonly List<Row> Rows = new List<Row>();   // checks: each closure row along the course, end to end
        // one box for a straight run of blocks laid along the course
        void SolidAlong(Transform root, List<Vector3> run, float bw, Vector3 f)
        {
            if (run.Count == 0) return;
            Vector3 a = run[0], b = run[run.Count - 1]; float lo = float.MaxValue, hi = float.MinValue;
            foreach (var q in run) { lo = Mathf.Min(lo, q.y - 1f); hi = Mathf.Max(hi, q.y + Height); }
            var dir = new Vector3(b.x - a.x, 0f, b.z - a.z); if (dir.sqrMagnitude < 1e-4f) dir = f; dir.y = 0f; dir.Normalize();
            var go = new GameObject("Barrier wall"); go.transform.SetParent(root, false);
            var mid = (a + b) * .5f; go.transform.SetPositionAndRotation(new Vector3(mid.x, (lo + hi) * .5f, mid.z), Quaternion.LookRotation(Vector3.Cross(dir, Vector3.up), Vector3.up));
            var box = go.AddComponent<BoxCollider>(); box.size = new Vector3(new Vector2(b.x - a.x, b.z - a.z).magnitude + bw, hi - lo, Depth);
            run.Clear();
        }
        static float OnPath(Vector3[] path, Vector2 q)
        {
            float best = float.MaxValue;
            for (int i = 0; i + 1 < path.Length; i++) best = Mathf.Min(best, DistToSegment(q, new Vector2(path[i].x, path[i].z), new Vector2(path[i + 1].x, path[i + 1].z)));
            return best;
        }
        static bool NearShortcut(RaceRoute route, Vector2 q)
        {
            foreach (var sc in route.Shortcuts) if (sc.Path != null && OnPath(sc.Path, q) < ShortcutClearM + .75f) return true;
            return false;
        }

        static Material Unlit(Color col)
        {
            var sh = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
            var m = new Material(sh);
            m.SetColor("_BaseColor", col); m.color = col;
            return m;
        }
    }
}
