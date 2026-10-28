using System;
using System.Collections.Generic;
using Racer;
using UnityEngine;

namespace ChastainRush
{
    // Round 78 (A, Dan: "would it be possible to build a running train?"). Freight trains on the main line through Marietta, edge to edge and back:
    // from the south edge on the double track (northbound on line 0, southbound on line 6) and on to Kennesaw (line 3, to the northwest edge) or to the
    // east edge (line 2), at 40 mph (17.9 m/s). The routes and the rails' heights come from the map build (Resources/Railroad.json, MapBuilder.Train.cs).
    // The timetable: the Kennesaw trains leave every 2H, alternately from each end; the Canton trains every H, alternately, half a period after; H is the
    // longest the slower route needs to clear its single track, so two trains never meet on one track. Where every train is comes from the clock alone
    // (cheap when far): a train is made of cars from the train kit only while some of it is within SpawnRadius of the camera. Each car is a kinematic
    // body moved along the rails by its two trucks: solid; a vehicle that hits it is put back (TrainCar). The crossings (RailCrossing) close from 26 s
    // before a train reaches them until its last car is past. Traffic stops at a closed crossing (TrafficDriver), the race AI waits or beats the train
    // (RoadDriver); the minimap draws the cars. World, not game layer; races only shift the clock (Align).
    public sealed class RailTrains : MonoBehaviour
    {
        public const float Speed = 17.9f, SpawnRadius = 1400f, CloseAhead = 26f, Gap = 1.1f;
        public static RailTrains Instance { get; private set; }
        public static bool Off;   // -trainsOff (checks)

        [Serializable] class LineJ { public int src; public float[] x, y, z; }
        [Serializable] class RouteJ { public string name; public int[] lines; public int[] dirs; }
        [Serializable] class DataJ { public LineJ[] lines; public RouteJ[] routes; }

        public sealed class Route { public string name; public Vector3[] p; public float[] cum; public float Length => cum[cum.Length - 1]; public List<(RailCrossing c, float s)> crossings = new(); }
        public readonly List<Route> routes = new();
        public float Offset = 420f;           // the clock's start: trains already out on the map
        public float H { get; private set; }  // the Canton trains' headway (the Kennesaw trains' is 2H)
        public float Clock => Time.time + Offset;
        RailCrossing[] crossings = new RailCrossing[0];
        GameObject[] kinds; float[] kindLen; int locoKinds;   // the kit: locomotives first

        sealed class Train { public int route, index; public float dep, length; public int[] kinds; public float[] at; public List<GameObject> cars; public AudioSource horn, rumble; public int honked = -1; }
        readonly Dictionary<long, Train> live = new();
        readonly Dictionary<int, Stack<GameObject>> pool = new();
        readonly List<long> scratchKeys = new(); readonly HashSet<long> keep = new();
        public int LiveTrains => live.Count;   // checks
        public static int HornsBlown;          // checks

        public static void Boot(GameObject host)
        {
            foreach (var a in Environment.GetCommandLineArgs()) if (a == "-trainsOff") Off = true;
            if (Off || Instance) return;
            var ta = Resources.Load<TextAsset>("Railroad"); if (!ta) return;
            Instance = host.AddComponent<RailTrains>(); Instance.Load(ta.text);
        }

        void Load(string json)
        {
            var d = JsonUtility.FromJson<DataJ>(json); if (d?.lines == null || d.routes == null) return;
            var bySrc = new Dictionary<int, LineJ>(); foreach (var l in d.lines) bySrc[l.src] = l;
            foreach (var r in d.routes)
            {
                var pts = new List<Vector3>(); bool ok = true;
                for (int k = 0; k < r.lines.Length; k++)
                {
                    if (!bySrc.TryGetValue(r.lines[k], out var l)) { ok = false; break; }
                    var seq = new List<Vector3>(); for (int i = 0; i < l.x.Length; i++) seq.Add(new Vector3(l.x[i], l.y[i], l.z[i]));
                    if (r.dirs[k] < 0) seq.Reverse();
                    if (pts.Count > 0)
                    {
                        // join: the route so far cut where it comes nearest the next line's start; the next line from its point nearest that cut
                        int bi = pts.Count - 1, bj = 0; float bd = float.MaxValue;
                        for (int i = Mathf.Max(0, pts.Count - 120); i < pts.Count; i++) for (int j = 0; j < Mathf.Min(seq.Count, 120); j++) { float dd = (pts[i] - seq[j]).sqrMagnitude; if (dd < bd) { bd = dd; bi = i; bj = j; } }
                        pts.RemoveRange(bi + 1, pts.Count - bi - 1); seq.RemoveRange(0, bj);
                    }
                    pts.AddRange(seq);
                }
                if (!ok || pts.Count < 2) continue;
                var cum = new float[pts.Count]; for (int i = 1; i < pts.Count; i++) cum[i] = cum[i - 1] + Vector3.Distance(pts[i], pts[i - 1]);
                routes.Add(new Route { name = r.name, p = pts.ToArray(), cum = cum });
            }
            if (routes.Count < 4) { Debug.LogWarning($"[Trains] {routes.Count} routes: none run"); routes.Clear(); return; }
            float tail = 40f * 17.5f / Speed;   // a long train's time to clear a point
            H = Mathf.Max(Mathf.Max(routes[2].Length, routes[3].Length) / Speed + tail + 20f, Mathf.Max(routes[0].Length, routes[1].Length) / Speed * .5f + tail * .5f + 10f);
            Debug.Log($"[Trains] routes {string.Join(", ", routes.ConvertAll(r => $"{r.name} {r.Length / 1000f:0.0} km"))}; headway {H:0} s (Kennesaw {2f * H:0} s)");
        }

        void Start()
        {
            crossings = FindObjectsByType<RailCrossing>(FindObjectsSortMode.None);
            foreach (var c in crossings)
                foreach (var r in routes)
                {
                    float s = Project(r, c.center, out float dist);
                    if (dist < 9f) r.crossings.Add((c, s));
                }
            foreach (var r in routes) r.crossings.Sort((a, b) => a.s.CompareTo(b.s));
            var kit = FindAnyObjectByType<TrainKit>(FindObjectsInactive.Include);
            if (kit)
            {
                var list = new List<GameObject>(); foreach (Transform t in kit.transform) list.Add(t.gameObject);
                list.Sort((a, b) => (a.name.Contains("loco") ? 0 : 1).CompareTo(b.name.Contains("loco") ? 0 : 1));
                kinds = list.ToArray(); kindLen = new float[kinds.Length];
                for (int i = 0; i < kinds.Length; i++) { kindLen[i] = kinds[i].GetComponent<BoxCollider>().size.z; if (kinds[i].name.Contains("loco")) locoKinds = i + 1; }
            }
            Debug.Log($"[Trains] {crossings.Length} crossings, {(kinds == null ? 0 : kinds.Length)} car kinds; crossings on the routes: {string.Join(", ", routes.ConvertAll(r => r.crossings.Count.ToString()))}");
        }

        // ---- the timetable --------------------------------------------------------------------------------------------------------------------------
        // routes 0 / 1 Kennesaw north / south: north at 4H k, south at 4H k + 2H (each line 3 run clear before the next starts: 2H > its time on the route);
        // routes 2 / 3 Canton north / south: north at 2H k + H, south at 2H k (H > a Canton train's time on the route). The two northbound services are H
        // apart on line 0, and the southbound ones join line 6 about H apart (Kennesaw's line 3 run is about H longer than Canton's line 2 run).
        float Depart(int route, long k) => route switch
        {
            0 => 4f * H * k,
            1 => 4f * H * k + 2f * H,
            2 => 2f * H * k + H,
            _ => 2f * H * k,
        };
        float Period(int route) => route < 2 ? 4f * H : 2f * H;

        Train Plan(int route, long k)
        {
            var rng = new System.Random((int)(route * 7919 + k * 104729 % 1000003));
            var tr = new Train { route = route, index = (int)k, dep = Depart(route, k) };
            if (kinds == null || kinds.Length <= locoKinds) { tr.kinds = new int[0]; tr.at = new float[0]; tr.length = 300f; return tr; }
            int loco = rng.Next(locoKinds), n = 22 + rng.Next(10); var ks = new List<int> { loco, loco };
            int block = -1, left = 0;
            for (int i = 0; i < n; i++) { if (left <= 0) { block = locoKinds + rng.Next(kinds.Length - locoKinds); left = 2 + rng.Next(6); } ks.Add(block); left--; }
            tr.kinds = ks.ToArray(); tr.at = new float[ks.Count]; float a = 0f;
            for (int i = 0; i < ks.Count; i++) { tr.at[i] = a + kindLen[ks[i]] * .5f; a += kindLen[ks[i]] + Gap; }
            tr.length = a; return tr;
        }

        // every train out on route r now: (departure index, head's distance along)
        IEnumerable<(long k, float head)> OnRoute(int r, float clock, float maxLen = 700f)
        {
            float L = routes[r].Length, per = Period(r), d0 = Depart(r, 0);
            long k1 = (long)Math.Floor((clock - d0) / per), k0 = (long)Math.Floor((clock - d0 - (L + maxLen) / Speed) / per);
            for (long k = Math.Max(k0, long.MinValue + 1); k <= k1; k++) { float head = (clock - Depart(r, k)) * Speed; if (head >= 0f && head <= L + maxLen) yield return (k, head); }
        }

        // ---- running -------------------------------------------------------------------------------------------------------------------------------
        void FixedUpdate()
        {
            if (routes.Count == 0) return;
            float clock = Time.fixedTime + Offset;
            var cam = Camera.main; var eye = cam ? cam.transform.position : Vector3.zero;
            scratchKeys.Clear(); foreach (var key in live.Keys) scratchKeys.Add(key);
            keep.Clear();
            for (int r = 0; r < routes.Count; r++)
                foreach (var (k, head) in OnRoute(r, clock))
                {
                    long key = ((long)r << 40) ^ (k & 0xFFFFFFFFFFL);
                    live.TryGetValue(key, out var tr);
                    float len = tr != null ? tr.length : 650f;
                    if (!Near(routes[r], head - len, head, eye)) continue;
                    if (tr == null) { tr = Plan(r, k); Spawn(tr); live[key] = tr; }
                    keep.Add(key); Place(tr, head);
                }
            foreach (var key in scratchKeys) if (!keep.Contains(key)) { Despawn(live[key]); live.Remove(key); }
        }

        bool Near(Route r, float s0, float s1, Vector3 eye)
        {
            for (float s = Mathf.Max(0f, s0); s <= Mathf.Min(r.Length, s1) + 1f; s += 60f) { var p = At(r, Mathf.Min(s, r.Length)); if ((p - eye).sqrMagnitude < SpawnRadius * SpawnRadius) return true; }
            return false;
        }

        void Spawn(Train tr)
        {
            tr.cars = new List<GameObject>(tr.kinds.Length);
            for (int i = 0; i < tr.kinds.Length; i++)
            {
                if (!pool.TryGetValue(tr.kinds[i], out var st)) pool[tr.kinds[i]] = st = new Stack<GameObject>();
                GameObject go;
                if (st.Count > 0) go = st.Pop();
                else
                {
                    go = Instantiate(kinds[tr.kinds[i]], transform); go.name = kinds[tr.kinds[i]].name.Replace("Train kit ", "Train ");
                    var rb = go.AddComponent<Rigidbody>(); rb.isKinematic = true; rb.interpolation = RigidbodyInterpolation.Interpolate; rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
                    go.AddComponent<TrainCar>();
                }
                go.SetActive(false); tr.cars.Add(go);
            }
            if (tr.cars.Count > 0)
            {
                var lead = tr.cars[0];
                tr.rumble = Source(lead, RailSounds.Rumble, true, 18f, 420f, .7f);
                tr.horn = Source(lead, RailSounds.Horn, false, 40f, 1400f, 1f);
                if (!tr.rumble.isPlaying) tr.rumble.Play();
            }
        }

        static AudioSource Source(GameObject go, AudioClip clip, bool loop, float min, float max, float vol)
        {
            var src = loop ? go.GetComponent<AudioSource>() : null;
            if (!loop) foreach (var a in go.GetComponents<AudioSource>()) if (a.clip == clip) src = a;
            if (!src || src.clip != clip) { src = go.AddComponent<AudioSource>(); src.clip = clip; src.loop = loop; src.spatialBlend = 1f; src.minDistance = min; src.maxDistance = max; src.rolloffMode = AudioRolloffMode.Logarithmic; src.dopplerLevel = .4f; src.playOnAwake = false; SoundMix.Track(src, vol, SoundMix.Kind.Effects); }
            return src;
        }

        void Despawn(Train tr)
        {
            if (tr.cars == null) return;
            for (int i = 0; i < tr.cars.Count; i++)
            {
                var go = tr.cars[i]; if (!go) continue;
                foreach (var a in go.GetComponents<AudioSource>()) a.Stop();
                go.SetActive(false); pool[tr.kinds[i]].Push(go);
            }
            tr.cars = null;
        }

        void Place(Train tr, float head)
        {
            var r = routes[tr.route];
            for (int i = 0; i < tr.cars.Count; i++)
            {
                var go = tr.cars[i]; float s = head - tr.at[i], half = kindLen[tr.kinds[i]] * .5f, truck = half - 2.6f;
                bool on = s - half > 0f && s + half < r.Length;
                if (go.activeSelf != on) go.SetActive(on);
                if (!on) continue;
                var a = At(r, s + truck); var b = At(r, s - truck); var fwd = a - b; if (fwd.sqrMagnitude < 1e-4f) continue;
                var pos = (a + b) * .5f; var rot = Quaternion.LookRotation(fwd.normalized, Vector3.up);
                var rb = go.GetComponent<Rigidbody>();
                if ((rb.position - pos).sqrMagnitude > 25f) { rb.position = pos; rb.rotation = rot; go.transform.SetPositionAndRotation(pos, rot); }
                else { rb.MovePosition(pos); rb.MoveRotation(rot); }
            }
            // the horn: long, long, short, long, from about 18 s before each crossing
            for (int c = 0; c < r.crossings.Count; c++)
            {
                float to = r.crossings[c].s - head;
                if (to > 300f && to < 330f && tr.honked < c && tr.horn) { tr.honked = c; tr.horn.Play(); HornsBlown++; }
            }
        }

        // ---- the crossings -------------------------------------------------------------------------------------------------------------------------
        void Update()
        {
            if (routes.Count == 0) return;
            float clock = Clock;
            foreach (var c in crossings) if (c) { c.Closed = false; arrive[c] = float.MaxValue; occupied[c] = false; }
            for (int r = 0; r < routes.Count; r++)
            {
                if (routes[r].crossings.Count == 0) continue;
                foreach (var (k, head) in OnRoute(r, clock))
                {
                    float len = TrainLength(r, k);
                    foreach (var (c, s) in routes[r].crossings)
                    {
                        float to = (s - head) / Speed;
                        bool past = head - len - 6f > s;
                        if (!past && to < CloseAhead) { c.Closed = true; if (to < arrive[c]) arrive[c] = to; if (to <= 0f) occupied[c] = true; }
                    }
                }
            }
        }
        readonly Dictionary<RailCrossing, float> arrive = new();
        readonly Dictionary<RailCrossing, bool> occupied = new();
        readonly Dictionary<long, float> lengths = new();
        float TrainLength(int r, long k)
        {
            long key = ((long)r << 40) ^ (k & 0xFFFFFFFFFFL);
            if (live.TryGetValue(key, out var tr)) return tr.length;
            if (!lengths.TryGetValue(key, out float l)) { l = Plan(r, k).length; if (lengths.Count > 400) lengths.Clear(); lengths[key] = l; }
            return l;
        }

        // a closed crossing ahead of a vehicle at pos heading fwd, within look (m): its distance to the stop line (spread + 7.5 m short of the middle
        // of the tracks), how soon the train reaches the road (s; 0 or less: it is on it) and the crossing (for a driver's decision)
        public static bool ClosedAhead(Vector3 pos, Vector3 fwd, float look, out float stopIn, out float trainIn, out RailCrossing which)
        {
            stopIn = trainIn = float.MaxValue; which = null; var I = Instance; if (!I) return false;
            fwd.y = 0f; fwd.Normalize();
            foreach (var c in I.crossings)
            {
                if (!c || !c.Closed) continue;
                var d = c.center - pos; if (Mathf.Abs(d.y) > 7f) continue; d.y = 0f;
                float ahead = Vector3.Dot(d, fwd); if (ahead < 0f || ahead > look) continue;
                if (Mathf.Abs(Vector3.Dot(fwd, c.roadDir)) < .5f) continue;   // not driving along that road
                float lat = Mathf.Abs(Vector3.Cross(fwd, d).y); if (lat > c.half + c.spread + 5f) continue;
                float stop = ahead - (c.spread + 7.5f);
                if (stop < -1.5f) continue;   // already on the tracks: drive on off them
                if (stop < stopIn) { stopIn = stop; which = c; trainIn = I.arrive.TryGetValue(c, out float a) ? a : 0f; }
            }
            return which != null;
        }

        // a closed crossing within r m of p (the resets: never back onto the tracks with a train coming)
        public static bool NearClosed(Vector3 p, float r)
        {
            var I = Instance; if (!I) return false;
            foreach (var c in I.crossings) { if (!c || !c.Closed) continue; var d = c.center - p; d.y = 0f; if (d.magnitude < r + c.spread) return true; }
            return false;
        }

        // ---- races: the clock moved on so the next train reaches a crossing (nearest the given point) in 'seconds' -------------------------------------
        public float Align(Vector3 near, float seconds, bool southbound = false)
        {
            RailCrossing best = null; float bd = float.MaxValue;
            foreach (var c in crossings) { var dv = c.center - near; dv.y = 0f; float dd = dv.sqrMagnitude; if (dd < bd) { bd = dd; best = c; } }
            if (!best) return 0f;
            float now = Clock, want = now + seconds, shift = float.MaxValue;
            for (int r = 0; r < routes.Count; r++)
                foreach (var (c, s) in routes[r].crossings)
                {
                    if (c != best || southbound && (r == 0 || r == 2)) continue;   // (southbound: routes 1 and 3; a train from the north reaches Mill Street before Whitlock Avenue)
                    // every departure that reaches s in [want, want + period]: the smallest wait from the wanted time
                    float per = Period(r); long k = (long)Math.Ceiling((want - s / Speed - Depart(r, 0)) / per);
                    for (int j = 0; j < 2; j++) { float t = Depart(r, k + j) + s / Speed; if (t >= want && t - want < shift) shift = t - want; }
                }
            if (shift == float.MaxValue) return 0f;
            Offset += shift;   // trains jump on by 'shift' seconds
            Debug.Log($"[Trains] clock moved on {shift:0.0} s: a train reaches {best.name} in {seconds:0} s");
            return shift;
        }

        // ---- the minimap ---------------------------------------------------------------------------------------------------------------------------
        public static void ForEachCar(Action<Vector3, Vector3> f)
        {
            var I = Instance; if (!I) return;
            foreach (var tr in I.live.Values) if (tr.cars != null) foreach (var go in tr.cars) if (go && go.activeSelf) { var t = go.transform; float h = go.GetComponent<BoxCollider>().size.z * .5f; f(t.position - t.forward * h, t.position + t.forward * h); }
        }

        // ---- helpers -------------------------------------------------------------------------------------------------------------------------------
        public static Vector3 At(Route r, float s)
        {
            s = Mathf.Clamp(s, 0f, r.Length); int lo = 0, hi = r.cum.Length - 1;
            while (hi - lo > 1) { int m = (lo + hi) >> 1; if (r.cum[m] <= s) lo = m; else hi = m; }
            float u = (s - r.cum[lo]) / Mathf.Max(1e-4f, r.cum[hi] - r.cum[lo]); return Vector3.Lerp(r.p[lo], r.p[hi], u);
        }

        static float Project(Route r, Vector3 q, out float dist)
        {
            dist = float.MaxValue; float best = 0f;
            for (int i = 0; i + 1 < r.p.Length; i++)
            {
                Vector2 a = new(r.p[i].x, r.p[i].z), b = new(r.p[i + 1].x, r.p[i + 1].z), p = new(q.x, q.z); var ab = b - a;
                float u = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(ab.sqrMagnitude, 1e-6f)); float dd = (a + ab * u - p).magnitude;
                if (dd < dist) { dist = dd; best = r.cum[i] + u * ab.magnitude; }
            }
            return best;
        }
    }
}
