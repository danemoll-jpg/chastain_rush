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
                built.Add(MakeBarrier(c));
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

        // a row of blocks across a pipe's mouth, on its floor, 0.6 m in (no posts: the cut's walls frame it)
        void MakeMouthBarrier(PipeMouths pm, int m)
        {
            var along = pm.inner[m] - pm.mouth[m]; along.y = 0f; along.Normalize();
            var lateral = Vector3.Cross(Vector3.up, along);
            float half = pm.half[m];
            var root = new GameObject("Mouth barrier " + pm.ids[m]); root.transform.SetParent(transform, false);
            int blocks = Mathf.Max(2, Mathf.RoundToInt(half * 2f / Block));
            float bw = half * 2f / blocks;
            var c = pm.mouth[m] + along * 0.6f;
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
            }
        }

        public void Clear()
        {
            for (int i = transform.childCount - 1; i >= 0; i--) Destroy(transform.GetChild(i).gameObject);
            built.Clear(); Gates.Clear(); Seals = Rails = 0;
        }

        static float DistToSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            var ab = b - a; float t = ab.sqrMagnitude < 1e-6f ? 0f : Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
            return (a + ab * t - p).magnitude;
        }

        Barrier MakeBarrier(RouteClosureJson c)
        {
            var along = new Vector3(c.dx, 0f, c.dz).normalized;
            var lateral = Vector3.Cross(Vector3.up, along);          // across the closed road
            float half = c.width_m * 0.5f + Verge;
            var root = new GameObject("Barrier " + c.id); root.transform.SetParent(transform, false);
            int blocks = Mathf.Max(2, Mathf.RoundToInt(half * 2f / Block));
            float bw = half * 2f / blocks;
            for (int i = 0; i < blocks; i++)
            {
                float off = -half + bw * (i + 0.5f);
                Vector3 p = new Vector3(c.x, 0f, c.z) + lateral * off;
                float gy = RaceRoute.Surface(p.x, p.z).y;
                // each block stands on the ground under it and reaches 1 m below it, so a slope leaves no gap
                var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.transform.SetParent(root.transform, false);
                cube.transform.SetPositionAndRotation(new Vector3(p.x, gy + Height * 0.5f - 0.5f, p.z), Quaternion.LookRotation(along, Vector3.up));
                cube.transform.localScale = new Vector3(bw, Height + 1f, Depth);
                var rd = cube.GetComponent<Renderer>();
                rd.sharedMaterial = i % 2 == 0 ? red : white;
                rd.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            // a tall post at each end so the closure reads from far down the street
            foreach (float side in new[] { -1f, 1f })
            {
                Vector3 p = new Vector3(c.x, 0f, c.z) + lateral * (side * half);
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

        static Material Unlit(Color col)
        {
            var sh = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
            var m = new Material(sh);
            m.SetColor("_BaseColor", col); m.color = col;
            return m;
        }
    }
}
