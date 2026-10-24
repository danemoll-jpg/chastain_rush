using System;
using System.Collections.Generic;
using System.Linq;
using ChastainRush;
using UnityEngine;

namespace Racer
{
    // Chastain Rush, Round 39 (Part B, Dan: "it would be cooler to have people walking around the neighborhood sometimes families and sometimes people
    // walking dogs"): people walking the neighborhoods' streets. New (Racer's AmbientLife only walks its roadside residents back and forth on the spot).
    //   the kinds: a dog walker (one person, one or two dogs on leashes: the dogs trot beside, stop to sniff and pull ahead), a family (two adults and
    //     one to three kids, sometimes a stroller), a couple, a jogger (at a run)
    //   the ways: every street of the Glen, Grasmere Court, the Commons, the Enclave, Chastain Lakes and the other neighborhoods (MapBuilder.Life.cs bakes
    //     them: each street's line, the walking line's offset from it, on the sidewalk where there is one, otherwise at the street's edge, and which
    //     street ends meet). A group walks its street, crosses at a corner onto one of the streets that meet there (chosen as it gets there, so its
    //     walk never repeats as a loop), and turns round at a dead end
    //   who is out: more in the morning and the evening, a few dog walkers after dark, nobody in rain or snow (LifeSchedule.Walkers)
    //   the same pool as the standing groups (AmbientLife.PoolSize, WalkerReserve of it kept for the walkers), lent out of sight near a view and taken
    //     back out of sight; the dogs and strollers are a small pool of their own, made the way the people are (primitives, no colliders, LODGroup)
    //   a vehicle coming at them: they step aside together (as the standing groups do); during a race none walks within 5 m of a race line (baked)
    public sealed partial class AmbientLife
    {
        public Vector2[] streetPts = new Vector2[0];   // every walking street's centerline, one after another
        public int[] streetFirst = new int[0];         // line i is points streetFirst[i] .. streetFirst[i + 1] - 1
        public float[] streetOff = new float[0];       // line i's walking line: this far either side of its centerline
        public bool[] streetRace = new bool[0];        // per point: within 5 m of a race or shortcut line (kept off during a race)
        public int[] linkFirst = new int[0];           // per line end (2 i: its start, 2 i + 1: its end): links[linkFirst[e] .. linkFirst[e + 1] - 1]
        public int[] links = new int[0];               // the ends of other lines that meet it
        // Round 64 (Dan, BUG-004: "It would be good to have peopel walking or jogging this trail"): per line, a path (the Noonday Creek Trail) rather than a
        // street: its own mix (joggers, cyclists, dog walkers, couples), walked in races too (it is the race's way: at most TrailInRace groups on it then,
        // and they step aside as everyone does)
        public bool[] streetTrail = new bool[0];
        const int TrailInRace = 2;

        public enum WalkKind { DogWalker, Family, Couple, Jogger, Cyclist }   // Round 64: Cyclist (the trail's)
        sealed class Dog { public Transform root, head, tail, leash; public Transform[] legs; public float size, until, phase; public int mode; public Vector3 at, sniffAt; public int walk = -1; }   // mode 0 beside, 1 sniffing, 2 pulling ahead
        sealed class Stroller { public Transform root; public int walk = -1; }
        sealed class Bike { public Transform root, crank; public Transform[] wheels; public int walk = -1; }   // Round 64
        sealed class Walk
        {
            public WalkKind kind; public int line, dir, side; public float s, speed, len, dodgeAt = -10f, dodgeSide; public Vector3 pos, dodgeDir; public bool placed;
            public readonly List<Person> members = new(); public readonly List<Dog> dogs = new(); public Stroller stroller; public Bike bike; public int adults;
        }
        readonly List<Dog> dogs = new(); readonly List<Stroller> strollers = new(); readonly List<Bike> bikes = new(); readonly Dictionary<int, Walk> walks = new(); int nextWalkId;
        Material[] coats; Material leashMat, strollerMat, wheelMat;
        float[][] cumLen; Dictionary<Vector2Int, List<(int line, int i)>> streetGrid;
        public int WalkersOut => walks.Count;                                   // checks
        public readonly int[] WalkersByKind = new int[5];                      // checks: groups out now, by kind
        public int WalkerDodges { get; private set; }                          // checks
        public string WalkersInfo { get; private set; } = "";
        public static bool WalkersOff;                                        // checks: the frame rate without them
        const int DogPool = 10, StrollerPool = 3, BikePool = 4;

        void CreateWalkerKit()
        {
            coats = new[] { Mat(new(.45f, .30f, .17f)), Mat(new(.10f, .09f, .08f)), Mat(new(.90f, .88f, .82f)), Mat(new(.78f, .58f, .30f)), Mat(new(.48f, .48f, .50f)) };
            leashMat = Mat(new(.75f, .12f, .10f)); strollerMat = Mat(new(.16f, .28f, .40f)); wheelMat = Mat(new(.08f, .08f, .08f));
            for (int i = 0; i < DogPool; i++) CreateDog(i);
            for (int i = 0; i < StrollerPool; i++) CreateStroller();
            for (int i = 0; i < BikePool; i++) CreateBike(i);
        }
        void DestroyWalkerKit() { foreach (var m in (coats ?? new Material[0]).Concat(new[] { leashMat, strollerMat, wheelMat })) if (m) Destroy(m); }

        // a dog of capsules and spheres, three sizes, five coats
        void CreateDog(int n)
        {
            var d = new Dog { root = new GameObject("Ambient dog").transform, size = new[] { .65f, 1f, 1.3f }[n % 3], phase = n * 2.37f }; d.root.SetParent(transform, false);
            var coat = coats[(n * 3) % coats.Length]; var dark = hair;
            var body = Part(d.root, "Body", PrimitiveType.Capsule, new(0, .42f, 0), new(.26f, .32f, .26f), coat); body.localRotation = Quaternion.Euler(90, 0, 0);
            d.head = new GameObject("Head").transform; d.head.SetParent(d.root, false); d.head.localPosition = new(0, .55f, .36f);
            Part(d.head, "Skull", PrimitiveType.Sphere, Vector3.zero, new(.2f, .2f, .22f), coat);
            Part(d.head, "Snout", PrimitiveType.Cube, new(0, -.04f, .14f), new(.1f, .09f, .14f), coat);
            Part(d.head, "Nose", PrimitiveType.Sphere, new(0, -.02f, .22f), new(.045f, .04f, .04f), dark);
            foreach (int side in new[] { -1, 1 }) { var ear = Part(d.head, "Ear", PrimitiveType.Cube, new(side * .08f, .1f, -.02f), new(.05f, .1f, .06f), n % 2 == 0 ? dark : coat); ear.localRotation = Quaternion.Euler(0, 0, side * -20); }
            d.legs = new Transform[4]; int k = 0;
            foreach (float z in new[] { .2f, -.2f }) foreach (int side in new[] { -1, 1 })
                {
                    var hip = new GameObject("Leg").transform; hip.SetParent(d.root, false); hip.localPosition = new(side * .085f, .34f, z);
                    Part(hip, "Leg", PrimitiveType.Capsule, new(0, -.17f, 0), new(.07f, .17f, .07f), coat); d.legs[k++] = hip;
                }
            d.tail = new GameObject("Tail").transform; d.tail.SetParent(d.root, false); d.tail.localPosition = new(0, .5f, -.32f);
            var tl = Part(d.tail, "Tail", PrimitiveType.Capsule, new(0, .07f, -.06f), new(.045f, .1f, .045f), coat); tl.localRotation = Quaternion.Euler(-40, 0, 0);
            d.root.localScale = Vector3.one * d.size;
            d.leash = Part(transform, "Leash", PrimitiveType.Cube, Vector3.zero, new(.02f, .02f, 1f), leashMat); d.leash.gameObject.SetActive(false);
            var lod = d.root.gameObject.AddComponent<LODGroup>(); lod.SetLODs(new[] { new LOD(.003f, d.root.GetComponentsInChildren<Renderer>()) }); lod.RecalculateBounds();
            d.root.gameObject.SetActive(false); dogs.Add(d);
        }
        void CreateStroller()
        {
            var s = new Stroller { root = new GameObject("Ambient stroller").transform }; s.root.SetParent(transform, false);
            Part(s.root, "Seat", PrimitiveType.Cube, new(0, .55f, 0), new(.48f, .32f, .62f), strollerMat);
            var hood = Part(s.root, "Hood", PrimitiveType.Sphere, new(0, .72f, -.16f), new(.5f, .42f, .44f), strollerMat);
            Part(s.root, "Handle", PrimitiveType.Cube, new(0, 1.0f, -.52f), new(.5f, .04f, .04f), wheelMat);
            foreach (int side in new[] { -1, 1 }) Part(s.root, "Handle bar", PrimitiveType.Cube, new(side * .23f, .78f, -.42f), new(.03f, .48f, .03f), wheelMat).localRotation = Quaternion.Euler(-25, 0, 0);
            foreach (float z in new[] { .25f, -.25f }) foreach (int side in new[] { -1, 1 }) Part(s.root, "Wheel", PrimitiveType.Cylinder, new(side * .23f, .14f, z), new(.26f, .025f, .26f), wheelMat).localRotation = Quaternion.Euler(0, 0, 90);
            var lod = s.root.gameObject.AddComponent<LODGroup>(); lod.SetLODs(new[] { new LOD(.003f, s.root.GetComponentsInChildren<Renderer>()) }); lod.RecalculateBounds();
            s.root.gameObject.SetActive(false); strollers.Add(s);
        }

        // Round 64: a bicycle of primitives (two wheels, a frame, bars, a saddle), its rider seated over it (StepWalkers)
        void CreateBike(int n)
        {
            var b = new Bike { root = new GameObject("Ambient bike").transform }; b.root.SetParent(transform, false);
            var frameMat = coats[(n * 2 + 1) % coats.Length];
            b.wheels = new Transform[2];
            for (int k = 0; k < 2; k++) { b.wheels[k] = Part(b.root, "Wheel", PrimitiveType.Cylinder, new(0, .34f, k == 0 ? .52f : -.52f), new(.68f, .02f, .68f), wheelMat); b.wheels[k].localRotation = Quaternion.Euler(0, 0, 90); }
            Part(b.root, "Frame", PrimitiveType.Cube, new(0, .6f, 0), new(.05f, .05f, .9f), frameMat).localRotation = Quaternion.Euler(-12, 0, 0);
            Part(b.root, "Down tube", PrimitiveType.Cube, new(0, .5f, .12f), new(.05f, .05f, .7f), frameMat).localRotation = Quaternion.Euler(35, 0, 0);
            Part(b.root, "Seat post", PrimitiveType.Cube, new(0, .68f, -.2f), new(.04f, .5f, .04f), frameMat).localRotation = Quaternion.Euler(-15, 0, 0);
            Part(b.root, "Saddle", PrimitiveType.Cube, new(0, .93f, -.26f), new(.14f, .05f, .26f), wheelMat);
            Part(b.root, "Bars", PrimitiveType.Cube, new(0, 1.0f, .42f), new(.56f, .04f, .04f), wheelMat);
            Part(b.root, "Stem", PrimitiveType.Cube, new(0, .8f, .44f), new(.04f, .42f, .04f), frameMat);
            b.crank = Part(b.root, "Crank", PrimitiveType.Cube, new(0, .34f, 0), new(.06f, .34f, .04f), wheelMat);
            var lod = b.root.gameObject.AddComponent<LODGroup>(); lod.SetLODs(new[] { new LOD(.003f, b.root.GetComponentsInChildren<Renderer>()) }); lod.RecalculateBounds();
            b.root.gameObject.SetActive(false); bikes.Add(b);
        }

        // ---------------------------------------------------------------- the streets
        int Lines => streetFirst.Length > 0 ? streetFirst.Length - 1 : 0;
        void PrepareStreets()
        {
            if (cumLen != null) return;
            cumLen = new float[Lines][]; streetGrid = new();
            for (int l = 0; l < Lines; l++)
            {
                int a = streetFirst[l], b = streetFirst[l + 1]; var c = new float[b - a];
                for (int i = 1; i < c.Length; i++) c[i] = c[i - 1] + (streetPts[a + i] - streetPts[a + i - 1]).magnitude;
                cumLen[l] = c;
                for (int i = 0; i < c.Length; i++) { var k = new Vector2Int(Mathf.FloorToInt(streetPts[a + i].x / 64f), Mathf.FloorToInt(streetPts[a + i].y / 64f)); if (!streetGrid.TryGetValue(k, out var lst)) streetGrid[k] = lst = new(); lst.Add((l, i)); }
            }
        }
        // the centerline point and direction of line l at s (clamped)
        Vector2 LinePoint(int l, float s, out Vector2 tan, out bool race)
        {
            var c = cumLen[l]; int a = streetFirst[l]; s = Mathf.Clamp(s, 0f, c[c.Length - 1]);
            int i = 1; while (i < c.Length - 1 && c[i] < s) i++;
            var p0 = streetPts[a + i - 1]; var p1 = streetPts[a + i]; tan = (p1 - p0).sqrMagnitude > 1e-6f ? (p1 - p0).normalized : Vector2.up;
            race = streetRace[a + i - 1] || streetRace[a + i];
            return Vector2.Lerp(p0, p1, Mathf.InverseLerp(c[i - 1], c[i], s));
        }
        Vector3 WalkPoint(Walk w, out Vector3 fwd)
        {
            var c = LinePoint(w.line, w.s, out var tan, out _); var right = new Vector2(tan.y, -tan.x);
            var p = c + right * (streetOff[w.line] * w.side * w.dir); var f2 = tan * w.dir; fwd = new Vector3(f2.x, 0, f2.y);
            return new Vector3(p.x, 0, p.y);
        }
        // the walking surface at p's x, z: the terrain's height there, then the short ray of Ground() onto a curb, sidewalk or bridge deck over it
        // (WalkPoint gives no height: a ray from a guessed height missed the ground and left the walkers far under the map)
        static float GroundAt(Vector3 p) { float y = GroundQuery.Height(p.x, p.z); return float.IsNaN(y) ? p.y : Ground(new Vector3(p.x, y, p.z)); }
        static bool Racing => RaceController.Instance && RaceController.Instance.State != RaceController.Stage.Free;

        // ---------------------------------------------------------------- who walks, near which view
        static readonly WalkKind[] Morning = { WalkKind.DogWalker, WalkKind.DogWalker, WalkKind.DogWalker, WalkKind.Jogger, WalkKind.Jogger, WalkKind.Jogger, WalkKind.Couple, WalkKind.Family };
        static readonly WalkKind[] Midday = { WalkKind.Family, WalkKind.Family, WalkKind.Family, WalkKind.Couple, WalkKind.Couple, WalkKind.DogWalker, WalkKind.DogWalker, WalkKind.Jogger };
        static readonly WalkKind[] Evening = { WalkKind.DogWalker, WalkKind.DogWalker, WalkKind.DogWalker, WalkKind.Family, WalkKind.Family, WalkKind.Couple, WalkKind.Couple, WalkKind.Jogger };
        static WalkKind PickKind(float hour, int n) => hour < 6 || hour >= 21 ? WalkKind.DogWalker : (hour < 9 ? Morning : hour < 17 ? Midday : Evening)[n & 7];
        static readonly WalkKind[] TrailMix = { WalkKind.Jogger, WalkKind.Cyclist, WalkKind.DogWalker, WalkKind.Jogger, WalkKind.Cyclist, WalkKind.Couple, WalkKind.DogWalker, WalkKind.Cyclist };   // Round 64
        bool IsTrail(int line) => streetTrail != null && line < streetTrail.Length && streetTrail[line];
        int TrailOut() => walks.Values.Count(w => IsTrail(w.line));
        public int TrailWalkers => TrailOut();   // checks
        void PickWalkers(float hour, bool wet, int day, bool underground = false)
        {
            if (Lines == 0) { WalkersInfo = "no streets"; return; }
            PrepareStreets();
            bool racing = Racing;
            // too far, out of their hours, or on a race's way during a race: taken back (when out of sight)
            foreach (var id in walks.Keys.ToList())
            {
                var w = walks[id]; LinePoint(w.line, w.s, out _, out bool onRace);
                bool keep = !underground && NearestView(w.pos) < Far && (!racing || !onRace) && !wet;
                if (keep || InView(w.pos + Vector3.up)) continue;
                Release(id);
            }
            int target = underground || wet || WalkersOff ? 0 : LifeSchedule.Walkers(hour);
            int near = walks.Values.Count(w => NearestView(w.pos) < Near);
            for (int tries = 0; tries < 6 && near < target; tries++)
            {
                var v = views[UnityEngine.Random.Range(0, views.Count)];
                if (TrySpawn(PickKind(hour, nextWalkId * 7 + day * 3 + (int)hour), v, 45f, Near - 10f, false, racing) >= 0) near++;
            }
            Array.Clear(WalkersByKind, 0, WalkersByKind.Length); foreach (var w in walks.Values) WalkersByKind[(int)w.kind]++;
            WalkersInfo = $"walkers {walks.Count} (target {target} near a view): dogs {WalkersByKind[0]}, families {WalkersByKind[1]}, couples {WalkersByKind[2]}, joggers {WalkersByKind[3]}, cyclists {WalkersByKind[4]}; on the trail {TrailOut()}";
        }
        void Release(int id)
        {
            var w = walks[id];
            foreach (var p in w.members) { p.walker = -1; p.root.gameObject.SetActive(false); p.root.localScale = p.adult; p.root.localRotation = Quaternion.identity; }
            foreach (var d in w.dogs) { d.walk = -1; d.root.gameObject.SetActive(false); d.leash.gameObject.SetActive(false); }
            if (w.stroller != null) { w.stroller.walk = -1; w.stroller.root.gameObject.SetActive(false); }
            if (w.bike != null) { w.bike.walk = -1; w.bike.root.gameObject.SetActive(false); }
            walks.Remove(id);
        }
        // a group of this kind on a street point between rMin and rMax from 'near' (out of sight unless 'anywhere'); its id, or -1
        int TrySpawn(WalkKind kind, Vector3 near, float rMin, float rMax, bool anywhere, bool racing)
        {
            var c0 = new Vector2Int(Mathf.FloorToInt(near.x / 64f), Mathf.FloorToInt(near.z / 64f)); int reach = Mathf.CeilToInt(rMax / 64f);
            var cand = new List<(int line, int i)>();
            for (int dx = -reach; dx <= reach; dx++) for (int dz = -reach; dz <= reach; dz++) if (streetGrid.TryGetValue(c0 + new Vector2Int(dx, dz), out var l)) cand.AddRange(l);
            if (cand.Count == 0) return -1;
            int seed = nextWalkId * 7919 + Time.frameCount;
            for (int k = 0; k < 12; k++)
            {
                var (line, i) = cand[(int)((uint)(seed + k * 104729) % (uint)cand.Count)];
                int a = streetFirst[line]; var p = streetPts[a + i]; float d = Vector2.Distance(p, new Vector2(near.x, near.z));
                if (d < rMin || d > rMax || (racing && streetRace[a + i])) continue;
                bool trail = IsTrail(line); if (trail && racing && TrailOut() >= TrailInRace) continue;
                var kk = trail && !anywhere ? TrailMix[(int)((uint)(seed + k * 31) % (uint)TrailMix.Length)] : kind;   // (a check's forced kind stays)
                var w = new Walk { kind = kk, line = line, s = cumLen[line][i], dir = (seed + k) % 2 == 0 ? 1 : -1, side = (seed >> 3) % 2 == 0 ? 1 : -1 };
                w.pos = WalkPoint(w, out _); w.pos.y = GroundAt(w.pos);
                if (!anywhere && InView(w.pos + Vector3.up)) continue;
                if (!Fill(w, seed + k)) return -1;
                int id = nextWalkId++; walks[id] = w;
                foreach (var pp in w.members) pp.walker = id; foreach (var dd in w.dogs) dd.walk = id; if (w.stroller != null) w.stroller.walk = id; if (w.bike != null) w.bike.walk = id;
                return id;
            }
            return -1;
        }
        // the group's figures from the pools (none taken unless all are there)
        bool Fill(Walk w, int seed)
        {
            int adults = w.kind == WalkKind.Family || w.kind == WalkKind.Couple ? 2 : 1, kids = w.kind == WalkKind.Family ? 1 + seed % 3 : 0, nDogs = w.kind == WalkKind.DogWalker ? 1 + (seed >> 2) % 2 : 0;
            bool pram = w.kind == WalkKind.Family && (seed >> 4) % 3 == 0, cyc = w.kind == WalkKind.Cyclist;
            var freeP = people.Where(p => p.group < 0 && p.walker < 0).Take(adults + kids).ToList(); var freeD = dogs.Where(d => d.walk < 0).Take(nDogs).ToList(); var freeS = pram ? strollers.FirstOrDefault(x => x.walk < 0) : null;
            var freeB = cyc ? bikes.FirstOrDefault(x => x.walk < 0) : null;
            if (freeP.Count < adults + kids || freeD.Count < nDogs || (pram && freeS == null) || (cyc && freeB == null)) return false;
            w.adults = adults;
            w.speed = w.kind == WalkKind.Cyclist ? 5.5f : w.kind == WalkKind.Jogger ? 2.8f : w.kind == WalkKind.Family ? 1.05f : w.kind == WalkKind.DogWalker ? 1.15f : 1.25f;
            if (cyc) { w.bike = freeB; freeB.root.position = w.pos; freeB.root.gameObject.SetActive(true); }
            for (int k = 0; k < freeP.Count; k++)
            {
                var p = freeP[k]; p.root.localScale = k < adults ? p.adult : p.adult * (.55f + .1f * (k % 2)); p.prop.gameObject.SetActive(false); p.rake.gameObject.SetActive(false);
                p.root.position = w.pos; p.dodgeAt = -10f; p.root.gameObject.SetActive(true); w.members.Add(p);
            }
            foreach (var d in freeD) { d.mode = 0; d.until = Time.time + 2f; d.at = w.pos; d.root.position = w.pos; d.root.gameObject.SetActive(true); d.leash.gameObject.SetActive(true); w.dogs.Add(d); }
            if (pram) { w.stroller = freeS; freeS.root.position = w.pos; freeS.root.gameObject.SetActive(true); }
            return true;
        }

        // ---------------------------------------------------------------- walking
        float lastWalkStep;
        void StepWalkers()
        {
            float dt = Mathf.Clamp(Time.time - lastWalkStep, 0f, .2f); lastWalkStep = Time.time;
            if (walks.Count == 0) return;
            var cars = DrainPipe.Vehicles; bool racing = Racing;
            foreach (var kv in walks)
            {
                var w = kv.Value; if (NearestView(w.pos) > 220f) continue;
                // along the street; at its end, onto a street that meets it (or back the way they came)
                w.s += w.dir * w.speed * dt; float len = cumLen[w.line][cumLen[w.line].Length - 1];
                if (w.s < 0f || w.s > len)
                {
                    int end = 2 * w.line + (w.s > len ? 1 : 0), n = linkFirst[end + 1] - linkFirst[end], pick = -1;
                    for (int k = 0; k < n && pick < 0; k++)
                    {
                        int o = links[linkFirst[end] + (int)((uint)(kv.Key * 31 + w.line * 7 + k + (int)Time.time) % (uint)n)]; int ol = o / 2;
                        if (racing && streetRace[streetFirst[ol] + (o % 2 == 0 ? 0 : cumLen[ol].Length - 1)]) continue;
                        pick = o;
                    }
                    if (pick < 0) { w.dir = -w.dir; w.s = Mathf.Clamp(w.s, 0f, len); }
                    else { w.line = pick / 2; bool atStart = pick % 2 == 0; w.dir = atStart ? 1 : -1; w.s = atStart ? 0f : cumLen[w.line][cumLen[w.line].Length - 1]; }
                }
                var target = WalkPoint(w, out var fwd);
                // a vehicle coming at them: they step aside together, then carry on
                ArcadeVehicle nearCar = null; float nd = 18f; foreach (var c in cars) { if (!c || !c.Body) continue; float d = Vector3.Distance(c.Body.position, w.pos); if (d < nd) { nd = d; nearCar = c; } }
                if (nearCar && Time.time - w.dodgeAt > 4f)
                {
                    var v = nearCar.Body.linearVelocity; v.y = 0; var to = w.pos - nearCar.Body.position; to.y = 0;
                    if (v.magnitude > 3f && nd < 6f + v.magnitude * .8f && Vector3.Dot(v.normalized, to.normalized) > .6f)
                    { var side = Vector3.Cross(Vector3.up, v.normalized); w.dodgeSide = Vector3.Dot(side, to) < 0 ? -1f : 1f; w.dodgeAt = Time.time; w.dodgeDir = side; WalkerDodges++; Dodges++; }
                }
                float since = Time.time - w.dodgeAt; bool dodging = since < 3.5f;
                if (dodging) target += w.dodgeDir * w.dodgeSide * 3.4f * (since < .5f ? since / .5f : since < 2f ? 1f : 1f - (since - 2f) / 1.5f);
                // the group's middle follows its walking line (crossing at a corner at a walk)
                var flat = new Vector3(target.x - w.pos.x, 0, target.z - w.pos.z); float maxMove = (dodging ? 7f : w.speed * 1.6f) * dt;
                w.pos += flat.magnitude > maxMove ? flat.normalized * maxMove : flat; w.pos.y = Ground(w.pos + Vector3.up * 2f);
                var heading = flat.sqrMagnitude > 1e-4f && !dodging ? flat.normalized : fwd; var right = Vector3.Cross(Vector3.up, heading);
                bool run = w.kind == WalkKind.Jogger; float t = Time.time;
                for (int i = 0; i < w.members.Count; i++)
                {
                    var p = w.members[i]; Vector3 off;
                    if (w.kind == WalkKind.Couple || (w.kind == WalkKind.Family && i < 2)) off = right * (i == 0 ? -.38f : .38f) + heading * (i == 0 ? .1f : -.1f);
                    else if (w.kind == WalkKind.Family) { int k = i - 2; off = -heading * (.9f + .55f * k) + right * ((k % 2 == 0 ? .45f : -.45f) + Mathf.Sin(t * 1.3f + k) * .25f); }
                    else off = Vector3.zero;
                    var at = w.pos + off; at.y = Ground(at + Vector3.up * 2f) + (w.bike != null ? .55f : 0f); p.root.position = at;   // (a cyclist sits on the saddle)
                    var look = nearCar && nd < 12f && nearCar.Body.linearVelocity.magnitude > 2f && !run ? Vector3.Slerp(heading, (nearCar.Body.position - at).normalized, .35f) : heading; look.y = 0;
                    if (look.sqrMagnitude > 1e-4f) p.root.rotation = Quaternion.LookRotation(look);
                    float ph = t + p.phase, swing = run ? Mathf.Sin(ph * 9f) * 40f : Mathf.Sin(ph * (i >= w.adults ? 7f : 5f)) * 22f;
                    if (w.bike != null) swing = -55f + Mathf.Sin(ph * 7f) * 28f;   // pedaling, seated
                    p.leftLeg.localRotation = Quaternion.Euler(w.bike != null ? swing : swing, 0, 0); p.rightLeg.localRotation = Quaternion.Euler(w.bike != null ? -110f - swing : -swing, 0, 0);
                    bool pushing = w.stroller != null && i == 0, leading = w.kind == WalkKind.DogWalker && i == 0;
                    p.arm.localRotation = Quaternion.Euler(pushing ? -62f : leading ? -38f : run ? -swing * 1.2f - 40f : -swing * .6f, 0, 0);
                    p.otherArm.localRotation = Quaternion.Euler(run ? swing * 1.2f - 40f : swing * .5f, 0, 0);
                }
                if (w.bike != null)
                {
                    var bp = w.pos; bp.y = Ground(bp + Vector3.up * 2f); w.bike.root.position = bp; w.bike.root.rotation = Quaternion.LookRotation(heading);
                    float spin = Time.time * w.speed / .34f * Mathf.Rad2Deg; foreach (var wh in w.bike.wheels) wh.localRotation = Quaternion.Euler(spin, 0, 90); w.bike.crank.localRotation = Quaternion.Euler(spin * .4f, 0, 0);
                    foreach (var p in w.members) { p.arm.localRotation = Quaternion.Euler(-70f, 0, 0); p.otherArm.localRotation = Quaternion.Euler(-70f, 0, 0); }
                }
                if (w.stroller != null) { var lead = w.members[0].root; w.stroller.root.position = lead.position + heading * .95f; w.stroller.root.rotation = Quaternion.LookRotation(heading); var sp = w.stroller.root.position; sp.y = Ground(sp + Vector3.up * 2f); w.stroller.root.position = sp; }
                for (int j = 0; j < w.dogs.Count; j++) StepDog(w.dogs[j], w, j, heading, right, dt, dodging);
            }
        }
        void StepDog(Dog d, Walk w, int j, Vector3 heading, Vector3 right, float dt, bool dodging)
        {
            var owner = w.members[0].root; float t = Time.time + d.phase;
            // its moods: trotting beside, stopping to sniff, pulling ahead
            if (Time.time > d.until && !dodging)
            {
                uint h = (uint)(Mathf.FloorToInt(t * 3.1f) * 2654435761u ^ (uint)(j * 97 + 13)); float r = (h & 0xffff) / 65535f;
                d.mode = r < .3f ? 1 : r < .55f ? 2 : 0; d.until = Time.time + (d.mode == 1 ? 1.5f + 2f * r : 2.5f + 3f * r);
                if (d.mode == 1) d.sniffAt = d.at;
            }
            float side = (j == 0 ? .75f : -.75f) * Mathf.Max(.8f, d.size);
            Vector3 goal = d.mode == 1 ? d.sniffAt : owner.position + heading * (d.mode == 2 ? 2.4f : 1.1f) + right * side;
            if (dodging) goal = owner.position + heading * .8f + right * side;
            var to = goal - d.at; to.y = 0; float speed = (d.mode == 1 ? 0f : w.speed * (d.mode == 2 ? 1.6f : 1.4f)) + to.magnitude * 1.5f;
            float leash = 3.2f; var fromOwner = d.at - owner.position; fromOwner.y = 0;
            if (fromOwner.magnitude > leash) { d.at = owner.position + fromOwner.normalized * leash; if (d.mode == 1) { d.mode = 0; d.until = Time.time + 1f; } }   // the leash draws it on
            if (to.magnitude > .02f) d.at += to.normalized * Mathf.Min(to.magnitude, speed * dt);
            d.at.y = Ground(d.at + Vector3.up * 2f); d.root.position = d.at;
            var face = d.mode == 1 ? heading * .3f + to * .1f : to.sqrMagnitude > .01f ? to : heading; face.y = 0;
            if (face.sqrMagnitude > 1e-4f) d.root.rotation = Quaternion.Slerp(d.root.rotation, Quaternion.LookRotation(face), .25f);
            bool moving = to.magnitude > .15f && d.mode != 1; float sw = moving ? Mathf.Sin(t * 12f / Mathf.Max(.7f, d.size)) * 30f : 0f;
            for (int k = 0; k < 4; k++) d.legs[k].localRotation = Quaternion.Euler((k == 0 || k == 3) ? sw : -sw, 0, 0);
            d.head.localRotation = Quaternion.Euler(d.mode == 1 ? 38f + Mathf.Sin(t * 9f) * 6f : d.mode == 2 ? -8f : Mathf.Sin(t * 1.7f) * 5f, Mathf.Sin(t * .8f) * 12f, 0);
            d.tail.localRotation = Quaternion.Euler(0, Mathf.Sin(t * (moving ? 14f : 8f)) * 30f, 0);
            // the leash, from the walker's hand to the collar
            var hand = owner.position + Vector3.up * (.95f * owner.localScale.y) + owner.right * .26f + owner.forward * .25f; var collar = d.root.TransformPoint(new Vector3(0, .5f, .26f));
            var span = collar - hand; float L = span.magnitude;
            if (L > .05f) { d.leash.position = (hand + collar) * .5f; d.leash.rotation = Quaternion.LookRotation(span); d.leash.localScale = new Vector3(.02f, .02f, L); }
        }

        // ---------------------------------------------------------------- for the checks
        // a group of this kind on the street nearest 'near' (anywhere, even in view); its id or -1
        public int ForceWalker(WalkKind kind, Vector3 near) { PrepareStreets(); return TrySpawn(kind, near, 0f, 60f, true, Racing); }
        public Vector3 WalkerPosition(int id) => walks.TryGetValue(id, out var w) ? w.pos : Vector3.zero;
        public int WalkerCount(int id) => walks.TryGetValue(id, out var w) ? w.members.Count + w.dogs.Count * 100 + (w.stroller != null ? 1000 : 0) : -1;
        public void ClearWalkers() { foreach (var id in walks.Keys.ToList()) Release(id); }
    }

    public static partial class LifeSchedule
    {
        // Round 39: how many walking groups are about near a view: most in the morning and the evening, fewer in the middle of the day, a few dog
        // walkers after dark, none from midnight to five (and none in rain or snow: AmbientLife.PickWalkers)
        public static int Walkers(float hour) => hour >= 6.5f && hour < 9.5f ? 7 : hour >= 17f && hour < 20.5f ? 8 : hour >= 9.5f && hour < 17f ? 4 : hour >= 20.5f && hour < 23.5f ? 2 : hour >= 5f && hour < 6.5f ? 2 : 0;
    }
}
