using System;
using System.Collections.Generic;
using Racer;
using UnityEngine;

namespace ChastainRush
{
    // Round 30 (Part A): traffic. Racer's (RaceDirector.CreateCars: copies of the player's vehicle as the Street Classic with
    // Racer's traffic bodies, AmbientVehicle; driven by RoadDriver with racing off; recycled while out of sight), here on the
    // whole road graph (TrafficNetwork, TrafficDriver). A game layer: nothing in the world depends on it.
    //   - Settings > GAMEPLAY > Traffic: Off / Light / Normal / Heavy (GameSettings.Options.traffic, default Normal), in free roam
    //     and races alike. A check run has none unless it asks: -traffic Off|Light|Normal|Heavy.
    //   - Cars appear 140 to 400 m from the player where the camera does not see (the first ones from 60 m), and go when they are
    //     more than 460 m away out of sight, or stuck, or at the map's edge, out of sight; they come back somewhere else.
    //   - In a race, the race's own roads are closed to it (every road near the route's line or a closure, for a route on the
    //     ground; an underground route closes only its closures), unless the route says "traffic": true (a race on open roads: Round 33,
    //     only the side roads barred for the race are closed, and the grid is cleared at the start).
    //     A car on a closed road at the start is moved; one that comes to a closed road turns off or round.
    public sealed class Traffic : MonoBehaviour
    {
        public static Traffic Instance { get; private set; }
        public static readonly string[] LevelNames = { "Off", "Light", "Normal", "Heavy" };
        public static readonly int[] LevelCars = { 0, 7, 14, 22 };
        public static int ForcedLevel = -1;   // checks: -traffic <name>
        public static int Horns;              // checks
        public static int KnotsUntied;        // Round 32 (B2): checks
        public static AudioClip HornClip { get; private set; }
        public readonly List<TrafficDriver> Cars = new();
        public Rigidbody PlayerBody => player ? player.Body : null;
        public int Level => ForcedLevel >= 0 ? ForcedLevel : CheckRun.Active ? 0 : Mathf.Clamp(GameSettings.Settings.traffic, 0, 3);
        public int Spawned { get; private set; }
        public int Recycled { get; private set; }

        ArcadeVehicle player; TrafficNetwork net; GameObject template, holder;
        bool[] forbidden; RaceController.Stage lastStage = RaceController.Stage.Free; bool started;
        float nextManage;
        readonly List<int> candidates = new(); readonly List<float> weights = new();
        readonly RaycastHit[] groundHits = new RaycastHit[12];

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            var args = Environment.GetCommandLineArgs(); ForcedLevel = -1; Horns = 0;
            for (int i = 0; i + 1 < args.Length; i++) if (args[i] == "-traffic") ForcedLevel = Mathf.Max(0, Array.IndexOf(LevelNames, args[i + 1]));
            if (Instance || !TrafficNetwork.Instance || !FindAnyObjectByType<ArcadeVehicle>()) return;
            Instance = new GameObject("Traffic").AddComponent<Traffic>();
        }

        System.Collections.IEnumerator Start()
        {
            net = TrafficNetwork.Instance;
            player = null; foreach (var v in FindObjectsByType<ArcadeVehicle>(FindObjectsSortMode.None)) if (v.GetComponent<VehicleInput>() && v.GetComponent<VehicleInput>().enabled) player = v;
            if (!player) player = FindAnyObjectByType<ArcadeVehicle>();
            forbidden = new bool[net.EdgeCount];
            HornClip = MakeHorn();
            yield return null; yield return null;   // after VehicleSwitch has put on the saved vehicle
            started = true;
        }
        void OnDestroy() { if (Instance == this) Instance = null; if (template) Destroy(template); }

        // ------------------------------------------------------------ the cars (Racer's CreateCars, one at a time)

        TrafficDriver Make()
        {
            if (!template)
            {
                // a copy of the player's vehicle without the player's parts (as RaceController's AI), kept switched off
                // made under a switched-off holder, so none of the player's own scripts wakes up in it
                holder = new GameObject("Traffic template holder"); holder.SetActive(false); holder.transform.SetParent(transform, false);
                template = Instantiate(player.gameObject, holder.transform); template.name = "Traffic template";
                foreach (var mb in template.GetComponents<MonoBehaviour>())
                {
                    if (mb is ArcadeVehicle || mb is VehicleConfiguration || mb is WaterFeedback) continue;
                    if (mb is VehicleInput || mb is VehicleRespawn) { mb.enabled = false; continue; }
                    DestroyImmediate(mb);
                }
                foreach (var src in template.GetComponents<AudioSource>()) DestroyImmediate(src);
                foreach (Transform child in template.transform) if (child.name == "Headlight") DestroyImmediate(child.gameObject);
            }
            var clone = Instantiate(template, null); clone.name = "Traffic " + (Cars.Count + 1);
            var car = clone.GetComponent<ArcadeVehicle>();
            var cfg = clone.GetComponent<VehicleConfiguration>();
            cfg.riderLook = null; cfg.classicVisual = true;   // Racer: ambient traffic is the Street Classic with its own bodies
            clone.SetActive(true);
            cfg.Apply("original");
            car.enabled = false; car.Body.isKinematic = false;
            car.Body.mass *= .75f;   // Chastain Rush: a little lighter than the Street Classic, so a hit knocks it (arcade)
            clone.AddComponent<AmbientVehicle>().Initialize();
            var driver = clone.AddComponent<TrafficDriver>(); driver.Initialize(this, car);
            clone.SetActive(false);   // in the pool until it is put on a road
            Cars.Add(driver);
            return driver;
        }

        void Update()
        {
            if (!started) return;
            // the pool is made one car a frame, also while a menu is up (the title menu at start): making a car takes a few
            // milliseconds, so none is made while driving unless the setting goes up
            if (Cars.Count < LevelCars[Level]) Make();
            if (Time.timeScale == 0) return;
            var race = RaceController.Instance; var stage = race ? race.State : RaceController.Stage.Free;
            if ((stage == RaceController.Stage.Free) != (lastStage == RaceController.Stage.Free)) { Close(race, stage); }
            lastStage = stage;
            if (Time.time < nextManage) return;
            nextManage = Time.time + .4f;
            Manage();
        }

        void Manage()
        {
            int want = LevelCars[Level];
            var at = player.Body.position; var at2 = new Vector2(at.x, at.z);
            // too many (the setting went down): the ones out of sight go first
            for (int i = Cars.Count - 1; i >= 0 && Cars.Count > want; i--)
                if (!Cars[i] || AmbientVehicle.Offscreen(Cars[i].Car.Body.position) || want == 0) { if (Cars[i]) Destroy(Cars[i].gameObject); Cars.RemoveAt(i); }
            foreach (var d in Cars)
            {
                if (!d.gameObject.activeSelf) continue;
                var p = d.Car.Body.position;
                bool far = SplitScreen.DistanceToPlayers(p, at) > 460f;   // Round 37: the nearer player (split screen)
                if ((far || d.WantsRecycle) && AmbientVehicle.Offscreen(p)) { if (Spawn(d, false)) Recycled++; }
            }
            int placed = 0;
            foreach (var d in Cars) if (!d.gameObject.activeSelf && placed < 3) { if (!Spawn(d, Spawned < want)) break; placed++; }
        }

        // a place out of sight 140 to 400 m away (near: from 60 m), on a road open to traffic, with room
        bool Spawn(TrafficDriver d, bool near)
        {
            var at = player.Body.position; var at2 = new Vector2(at.x, at.z);
            candidates.Clear(); weights.Clear(); float total = 0;
            for (int e = 0; e < net.EdgeCount; e++)
            {
                if (forbidden[e] || net.Bridge(e) || net.Length[e] < 12) continue;
                var mid = net.Point(e, net.PointCount(e) / 2); if (Near(mid, at2) > 520f) continue;
                candidates.Add(e); total += net.Length[e]; weights.Add(total);
            }
            if (candidates.Count == 0) return false;
            float min = near ? 60f : 140f;
            for (int attempt = 0; attempt < 40; attempt++)
            {
                float r = UnityEngine.Random.value * total; int k = weights.BinarySearch(r); if (k < 0) k = ~k; int e = candidates[Mathf.Min(k, candidates.Count - 1)];
                int n = net.PointCount(e); int i = UnityEngine.Random.Range(1, n); float t = UnityEngine.Random.value;
                var a = net.Point(e, i - 1); var b = net.Point(e, i); var q = Vector2.Lerp(a, b, t);
                float dist = Near(q, at2); if (dist < min || dist > 400f) continue;
                int dir = net.Oneway(e) ? 1 : (UnityEngine.Random.value < .5f ? 1 : -1);
                var tan = (b - a).normalized * dir; var right = new Vector2(tan.y, -tan.x);
                float lane = net.Oneway(e) ? (net.Half(e) >= 4.5f ? net.Half(e) * .45f : 0f) : Mathf.Clamp(net.Half(e) * .5f, 1.3f, 2.6f);
                q += right * lane;
                if (!Ground(q, out float y)) continue;
                var pos = new Vector3(q.x, y + .7f, q.y);
                if (!AmbientVehicle.Offscreen(pos)) continue;
                bool room = true; foreach (var o in Cars) if (o && o != d && o.gameObject.activeSelf && (o.Car.Body.position - pos).sqrMagnitude < 30f * 30f) { room = false; break; }
                if (!room) continue;
                d.gameObject.SetActive(true);
                d.GetComponent<AmbientVehicle>()?.Recycle(pos, true);
                d.Place(e, dir, 0, pos, net.Speed(e) * .7f);
                Spawned++;
                return true;
            }
            return false;
        }
        // Round 37: the distance from a road point to the nearer player (split screen: both; else the one)
        static float Near(Vector2 q, Vector2 p1)
        {
            float d = (q - p1).magnitude; var p2 = SplitScreen.P2Car;
            if (p2) d = Mathf.Min(d, (q - new Vector2(p2.Body.position.x, p2.Body.position.z)).magnitude);
            return d;
        }
        // the ground's height under a road point: the terrain's (traffic is never put on a bridge, so a deck over the road is skipped)
        bool Ground(Vector2 q, out float y)
        {
            y = 0; int n = Physics.RaycastNonAlloc(new Vector3(q.x, 900f, q.y), Vector3.down, groundHits, 1200f, ~0, QueryTriggerInteraction.Ignore);
            bool found = false; for (int i = 0; i < n; i++) if (groundHits[i].collider is TerrainCollider && (!found || groundHits[i].point.y > y)) { y = groundHits[i].point.y; found = true; }
            return found;
        }

        // ------------------------------------------------------------ junctions

        // the next road at a junction: the same road on, a bigger road, now and then into a neighborhood; never a closed road;
        // at a dead end a small road turns round, a big one (the map's edge) ends there (exit: moved when out of sight)
        public int PickNext(int e, int node, out int dir, out bool exit)
        {
            exit = false; dir = 1;
            var options = net.Adjacent(node); float total = 0; int pick = -1;
            var inDir = InDirection(e, node); int rank = net.Rank(e); string name = net.Name(e);
            Span<float> w = stackalloc float[Mathf.Min(options.Count, 16)];
            for (int k = 0; k < w.Length; k++)
            {
                int o = options[k]; w[k] = 0;
                if (o == e || forbidden[o] || !net.CanEnter(o, node)) continue;
                var outDir = OutDirection(o, node);
                float turn = Vector2.Angle(inDir, outDir);
                float weight = name.Length > 0 && net.Name(o) == name ? 4f : net.Rank(o) >= rank ? 2f : rank >= 3 && net.Rank(o) == 1 ? .35f : 1f;
                if (turn > 150) weight *= .15f;
                w[k] = weight; total += weight;
            }
            if (total > 0)
            {
                float r = UnityEngine.Random.value * total;
                for (int k = 0; k < w.Length; k++) { r -= w[k]; if (w[k] > 0 && r <= 0) { pick = options[k]; break; } }
                if (pick < 0) for (int k = w.Length - 1; k >= 0; k--) if (w[k] > 0) { pick = options[k]; break; }
                dir = net.From(pick) == node ? 1 : -1; return pick;
            }
            // nowhere to go on: turn round on a small two-way road, else stop here
            if (!net.Oneway(e) && rank <= 3 && !forbidden[e]) { dir = net.From(e) == node ? 1 : -1; return e; }
            exit = true; return -1;
        }
        Vector2 InDirection(int e, int node) { int n = net.PointCount(e); return net.To(e) == node ? (net.Point(e, n - 1) - net.Point(e, n - 2)).normalized : (net.Point(e, 0) - net.Point(e, 1)).normalized; }
        Vector2 OutDirection(int e, int node) { int n = net.PointCount(e); return net.From(e) == node ? (net.Point(e, 1) - net.Point(e, 0)).normalized : (net.Point(e, n - 2) - net.Point(e, n - 1)).normalized; }

        // coming in on a smaller road than another one at the junction: stop first
        public bool MinorAt(int e, int node)
        {
            int rank = net.Rank(e), top = 0;
            foreach (int o in net.Adjacent(node)) if (o != e) top = Mathf.Max(top, net.Rank(o));
            return top > rank;
        }
        // Round 32 (B2): of the cars within 25 m that have stood 3 s or more held by one across their way, the first one listed
        // goes (one at a time, so the knot unwinds instead of all of them driving into each other)
        public bool TakesTurn(TrafficDriver me)
        {
            var p = me.Car.Body.position;
            foreach (var o in Cars)
            {
                if (o == me) return true;
                if (o && o.gameObject.activeSelf && o.Jammed > 3f && (o.Car.Body.position - p).sqrMagnitude < 25f * 25f) return false;
            }
            return true;
        }

        // nothing in the junction or about to enter it (other traffic, the player, the AI racers)
        public bool JunctionClear(int node, TrafficDriver me)
        {
            var c = net.NodePos(node); var mine = me.Car.Body.position; float myDist = (new Vector2(mine.x, mine.z) - c).magnitude;
            bool Busy(Rigidbody b)
            {
                if (!b) return false;
                var p = new Vector2(b.position.x, b.position.z); var v = new Vector2(b.linearVelocity.x, b.linearVelocity.z);
                float d = (p - c).magnitude;
                if (d < 8f && v.magnitude > .5f) return true;                                          // in it
                return d > .1f && d < 26f && d < myDist + 2f && Vector2.Dot(v, (c - p) / d) > 2f;    // coming at it, nearer than me
            }
            foreach (var o in Cars) if (o && o != me && o.gameObject.activeSelf && Busy(o.Car.Body)) return false;
            if (Busy(PlayerBody)) return false;
            if (SplitScreen.P2Car && Busy(SplitScreen.P2Car.Body)) return false;   // Round 37
            var race = RaceController.Instance;
            if (race) foreach (var en in race.Entrants) if (!en.IsPlayer && en.Car && Busy(en.Car.Body)) return false;
            return true;
        }

        // ------------------------------------------------------------ races: the race's roads closed to traffic

        public bool Forbidden(int e) => forbidden != null && e >= 0 && forbidden[e];
        public int ClosedRoads { get; private set; }
        // the race's barriers in the direction raced (Round 39: the reverse Chastain Ridge has its own)
        static IEnumerable<RouteClosureJson> RaceClosures(RaceController race, RouteJson rd) => race && race.Route != null ? (IEnumerable<RouteClosureJson>)race.Route.Closures : rd?.closures ?? new RouteClosureJson[0];
        void Close(RaceController race, RaceController.Stage stage)
        {
            Array.Clear(forbidden, 0, forbidden.Length); ClosedRoads = 0;
            var rd = race ? race.RouteData : null;
            if (stage != RaceController.Stage.Free && rd != null && !rd.traffic)
            {
                bool surface = rd.loop_y == null || rd.loop_y.Length == 0;
                var marks = new List<(Vector2 p, float r)>();
                if (surface)
                {
                    void Line(float[] pts) { if (pts == null) return; for (int i = 0; i + 1 < pts.Length; i += 2) marks.Add((new Vector2(pts[i], pts[i + 1]), 14f)); }
                    if (race.Route != null && race.Route.OwnReverse) Line(rd.loop_rev); else { Line(rd.loop); Line(rd.lead_in); }   // Round 39: the reverse lap of its own
                }
                foreach (var c in RaceClosures(race, rd)) marks.Add((new Vector2(c.x, c.z), 10f));
                for (int e = 0; e < net.EdgeCount; e++)
                {
                    foreach (var (p, r) in marks) if (net.Distance(e, p) < r) { forbidden[e] = true; ClosedRoads++; break; }
                }
                // the race starts from the grid: every car on a closed road is moved now (the countdown hides it)
                foreach (var d in Cars) if (d && d.gameObject.activeSelf && (forbidden[Mathf.Max(0, d.Edge)] || OnClosed(d))) Spawn(d, false);
            }
            else if (stage != RaceController.Stage.Free && rd != null && rd.traffic)
            {
                // Round 33 (a race on open roads, the Chastain Ridge): traffic keeps driving the course; only the side roads barred for the
                // race are closed to it (the pieces at each barrier), so no car drives into a barrier, and the grid is cleared for the start
                var barred = RaceClosures(race, rd);
                for (int e = 0; e < net.EdgeCount; e++)
                    foreach (var c in barred) if (net.Distance(e, new Vector2(c.x, c.z)) < 6f) { forbidden[e] = true; ClosedRoads++; break; }
                var grid = race.Route != null ? race.Route.GridPosition : Vector3.zero;
                foreach (var d in Cars)
                    if (d && d.gameObject.activeSelf && (forbidden[Mathf.Max(0, d.Edge)] || OnClosed(d) || (race.Route != null && (d.Car.Body.position - grid).sqrMagnitude < 130f * 130f))) Spawn(d, false);
            }
            Debug.Log($"[Traffic] {(stage == RaceController.Stage.Free ? "free roam: every road open" : $"race {rd?.name}: {ClosedRoads} road pieces closed to traffic{(rd != null && rd.traffic ? " (open roads: only the barred side roads)" : "")}")}");
        }
        bool OnClosed(TrafficDriver d)
        {
            var p = d.Car.Body.position; var q = new Vector2(p.x, p.z);
            for (int e = 0; e < net.EdgeCount; e++) if (forbidden[e] && net.Distance(e, q) < 8f) return true;
            return false;
        }

        // ------------------------------------------------------------ the horn (synthesized, as this game's other sounds)

        static AudioClip MakeHorn()
        {
            const int rate = 22050; float seconds = .55f; var data = new float[(int)(rate * seconds)];
            for (int i = 0; i < data.Length; i++)
            {
                float t = i / (float)rate, env = Mathf.Clamp01(t / .02f) * Mathf.Clamp01((seconds - t) / .06f);
                float a = Mathf.Sin(2 * Mathf.PI * 415 * t), b = Mathf.Sin(2 * Mathf.PI * 523 * t);
                float x = (a + b) * .5f; x = Mathf.Clamp(x * 2.2f, -1, 1) * .55f + (Mathf.Sin(2 * Mathf.PI * 830 * t) + Mathf.Sin(2 * Mathf.PI * 1046 * t)) * .06f;
                data[i] = x * env * .7f;
            }
            var clip = AudioClip.Create("Car horn", data.Length, 1, rate, false); clip.SetData(data, 0); return clip;
        }
    }
}
