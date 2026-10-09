using System.Collections.Generic;
using UnityEngine;

namespace ChastainRush
{
    // Round 6: roads that leave the course are closed while a race runs. The closures are route data (made by
    // Tools/mapgen/make_route.py: one per road that leaves the lap or the lead-in, a little way along it); a closure is
    // skipped where an official shortcut starts or ends. Each barrier is a row of red and white blocks across the road and
    // its verge, with colliders, so the car stops. Built when a race starts, removed in free driving (Racer does the same
    // with its authored course barriers, which are switched on only for the race).
    // Layer note: the barrier is on the default layer like the terrain, so it is solid to the car and to its wheel rays.
    public sealed class RaceBarriers : MonoBehaviour
    {
        public const float Height = 1.5f, Depth = 0.9f, Block = 1.5f, Verge = 3f, ShortcutClearM = 16f;
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
                var contact = c.contact != null && c.contact.Length >= 2 ? new Vector2(c.contact[0], c.contact[1]) : new Vector2(c.x, c.z);
                bool shortcutEnd = false;
                foreach (var sc in route.Shortcuts)
                    if (Dist2(contact, sc.Start) < ShortcutClearM || Dist2(contact, sc.End) < ShortcutClearM) shortcutEnd = true;
                if (shortcutEnd) { Open.Add($"{c.id} {c.label}"); continue; }
                built.Add(MakeBarrier(c));
            }
        }

        public void Clear()
        {
            for (int i = transform.childCount - 1; i >= 0; i--) Destroy(transform.GetChild(i).gameObject);
            built.Clear();
        }

        static float Dist2(Vector2 a, Vector3 b) => Vector2.Distance(a, new Vector2(b.x, b.z));

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
                float gy = RaceRoute.Ground(p.x, p.z).y;
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
                float gy = RaceRoute.Ground(p.x, p.z).y;
                var post = GameObject.CreatePrimitive(PrimitiveType.Cube);
                post.transform.SetParent(root.transform, false);
                post.transform.SetPositionAndRotation(new Vector3(p.x, gy + 1.6f, p.z), Quaternion.LookRotation(along, Vector3.up));
                post.transform.localScale = new Vector3(0.35f, 3.4f, 0.35f);
                post.GetComponent<Renderer>().sharedMaterial = red;
                Destroy(post.GetComponent<Collider>());
            }
            return new Barrier { Data = c, Root = root, Centre = new Vector3(c.x, RaceRoute.Ground(c.x, c.z).y, c.z), Along = along, HalfLength = half };
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
