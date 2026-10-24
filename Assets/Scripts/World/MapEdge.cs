using Racer;
using UnityEngine;

namespace ChastainRush
{
    // Round 66 (3, Dan: "CARS STOP NATURALLY AT THE EDGE ... with a soft invisible push-back so nothing gets through or falls off"): the map's
    // terrain ends at its edge (the scenery past it has no colliders). Every road that leaves the map is closed short of it (MapBuilder.Edges.cs);
    // off the roads, within Zone of the edge, a vehicle on the ground is pushed back in, harder the closer it gets and the faster it goes out, so it
    // comes to a stop before the edge, and an invisible wall stands on the edge itself as the last stop. A vehicle that still ends up past the edge
    // (a jump, a glitch) is put back inside, facing in. The helicopter is not pushed or walled (HelicopterFlight.Boundary.cs turns it back).
    // World, not game layer; made on its own when a scene with terrain loads.
    public sealed class MapEdge : MonoBehaviour
    {
        public const float Zone = 36f, Wall = 4f;
        public static Rect Map { get; private set; }
        BoxCollider[] walls;
        public static int Pushes { get; private set; }     // vehicle steps pushed back (checks)
        public static int PutBack { get; private set; }    // vehicles put back inside (checks)

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            if (FindAnyObjectByType<MapEdge>() || Terrain.activeTerrains.Length == 0) return;
            new GameObject("Map edge").AddComponent<MapEdge>();
        }

        void Start()
        {
            Map = GroundQuery.Extent(); var m = Map;
            // in 200 m pieces, each from under the ground there to 40 m over its highest point (a helicopter's ray down never meets one far above the ground)
            var list = new System.Collections.Generic.List<BoxCollider>();
            void Side(Vector2 a, Vector2 b, Vector2 outward)
            {
                float len = (b - a).magnitude; int nSeg = Mathf.Max(1, Mathf.CeilToInt(len / 200f));
                for (int i = 0; i < nSeg; i++)
                {
                    var p0 = Vector2.Lerp(a, b, (float)i / nSeg); var p1 = Vector2.Lerp(a, b, (float)(i + 1) / nSeg);
                    float lo = float.MaxValue, hi = float.MinValue;
                    for (float t = 0f; t <= 1.001f; t += .05f) { var q = Vector2.Lerp(p0, p1, t) - outward * 1f; if (GroundQuery.TryHeight(q.x, q.y, out float y)) { lo = Mathf.Min(lo, y); hi = Mathf.Max(hi, y); } }
                    if (lo > hi) continue;
                    var c = (p0 + p1) * .5f + outward * (Wall * .5f); float segLen = (p1 - p0).magnitude + 2f * Wall;
                    var size = Mathf.Abs(outward.x) > .5f ? new Vector3(Wall, hi - lo + 100f, segLen) : new Vector3(segLen, hi - lo + 100f, Wall);
                    list.Add(Make(new Vector3(c.x, (lo - 60f + hi + 40f) * .5f, c.y), size));
                }
            }
            Side(new Vector2(m.xMin, m.yMin), new Vector2(m.xMin, m.yMax), Vector2.left); Side(new Vector2(m.xMax, m.yMin), new Vector2(m.xMax, m.yMax), Vector2.right);
            Side(new Vector2(m.xMin, m.yMin), new Vector2(m.xMax, m.yMin), Vector2.down); Side(new Vector2(m.xMin, m.yMax), new Vector2(m.xMax, m.yMax), Vector2.up);
            walls = list.ToArray();
            Debug.Log($"[Round66] map edge: x {m.xMin:0.0}..{m.xMax:0.0}, z {m.yMin:0.0}..{m.yMax:0.0}; pushed back within {Zone:0} m, {walls.Length} wall pieces on the edge");
        }
        BoxCollider Make(Vector3 c, Vector3 size)
        {
            var go = new GameObject("Map edge wall"); go.transform.SetParent(transform, false); go.transform.position = c;
            var b = go.AddComponent<BoxCollider>(); b.size = size; return b;
        }

        // how far inside the edge (negative past it) and the way back in
        public static float Inside(Vector3 p, out Vector3 inward)
        {
            var m = Map; float w = p.x - m.xMin, e = m.xMax - p.x, s = p.z - m.yMin, n = m.yMax - p.z;
            float d = Mathf.Min(Mathf.Min(w, e), Mathf.Min(s, n));
            inward = Vector3.zero;
            if (w < Zone) inward.x += 1f - Mathf.Max(0f, w) / Zone; if (e < Zone) inward.x -= 1f - Mathf.Max(0f, e) / Zone;
            if (s < Zone) inward.z += 1f - Mathf.Max(0f, s) / Zone; if (n < Zone) inward.z -= 1f - Mathf.Max(0f, n) / Zone;
            if (inward.sqrMagnitude < 1e-6f) inward = d == w ? Vector3.right : d == e ? Vector3.left : d == s ? Vector3.forward : Vector3.back;
            inward.Normalize();
            return d;
        }

        float wallCheck; readonly System.Collections.Generic.Dictionary<ArcadeVehicle, bool> flying = new System.Collections.Generic.Dictionary<ArcadeVehicle, bool>();
        void FixedUpdate()
        {
            if (Map.width <= 0f) return;
            foreach (var v in ArcadeVehicle.Live)
            {
                if (!v || !v.Body || v.Body.isKinematic) continue;
                bool flies = v.Flown;
                var p = v.Body.position; float d = Inside(p, out var n);
                if (flies || d >= Zone) continue;
                if (d < -1.5f)
                {
                    // past the edge: put back inside, facing in
                    var at = p + n * (-d + 14f); if (GroundQuery.TryHeight(at.x, at.z, out float y)) at.y = y + 1f;
                    var resp = v.GetComponent<VehicleRespawn>();
                    if (resp) resp.PlaceAt(at, Mathf.Atan2(n.x, n.z) * Mathf.Rad2Deg);
                    else { v.Body.position = at; v.Body.linearVelocity = Vector3.zero; }
                    PutBack++; continue;
                }
                float t = 1f - Mathf.Max(0f, d) / Zone;
                var vel = v.Body.linearVelocity; float vOut = -Vector3.Dot(new Vector3(vel.x, 0f, vel.z), n);
                // the outward speed bled off over what is left of the zone (enough to stop at the edge from any speed), and a gentle push in
                // (eased in over the zone's first 3 m; the push in only in the last 12 m, so a road along the edge is driven as before)
                float need = vOut > 0f ? 1.15f * vOut * vOut / (2f * Mathf.Max(1.5f, d + 1f)) : 0f;
                v.Body.AddForce(n * (need * Mathf.Clamp01((Zone - d) / 3f) + 5f * Mathf.Clamp01(1f - d / 12f)), ForceMode.Acceleration);
                if (t > 0f) Pushes++;
            }
            // the walls never touch a flying helicopter (set when a vehicle starts or stops flying)
            if (Time.time > wallCheck)
            {
                wallCheck = Time.time + .5f;
                foreach (var v in ArcadeVehicle.Live)
                {
                    if (!v) continue; bool flies = v.Flown;
                    if (flying.TryGetValue(v, out bool was) ? was == flies : !flies) continue;
                    flying[v] = flies;
                    foreach (var c in v.GetComponentsInChildren<Collider>()) foreach (var w in walls) if (w) Physics.IgnoreCollision(c, w, flies);
                }
            }
        }
    }
}
