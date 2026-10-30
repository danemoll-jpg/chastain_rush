using System.Collections.Generic;
using Racer;
using UnityEngine;

namespace ChastainRush
{
    // Round 10, item 7 (Dan: "lets make the trees solid"): every tree of the TreeField stops the car at its trunk. Racer gives
    // each tree a hidden static box collider round its trunk ("the trunk colliders are separate invisible boxes",
    // SceneryTrees); with 56,000 trees here that would be 56,000 live colliders, so this keeps a pool of trunk colliders
    // (capsules, the trunk's own radius and height, never the crown) on the trees nearest the car and moves them as the car
    // goes: every 0.25 s or 3 m, the nearest 'pool' trees within 'radius' of the car get one. Hand-placed trees (MapBuilder
    // PlaceKitTree, the pond's weeping trees) have their own static trunk colliders.
    // Round 19 (item 1; Dan: "If we have to make trees breakable that is ok"): the trees beside the Noonday Creek trail listed in
    // TreeField.breakable get a TRIGGER capsule instead ("trunk snap"): a vehicle that touches one snaps it, as fences and mailboxes
    // (FenceChunk): a wood crack, a little speed lost, never pushed back; the tree falls over away from the vehicle and fades
    // (FallingTree), and stays gone until a race starts (FenceChunk.AllRestored). Every other tree on the map is solid as before.
    // Round 86 (C, Dan: "Trees need to be more breakable as we have done in woodstock rush"): Woodstock Rush 1.3's breakable trees (SceneryTrees.Break,
    // RoamWorld's TreeBreaker), for every tree of the field, in races and Free Roam: every trunk capsule is solid; a vehicle fast enough for a trunk
    // just ahead of it (Needed: 7 m/s for a trunk 0.5 m thick or less, 24 m/s for 1 m and more; HIGH MODE 45 % of that) breaks it before it gets
    // there (BreakAhead): the capsule goes, the tree falls the way it was hit with leaf and bark debris and the wood sounds and lies there with no
    // collider (never wrecks anyone), and the vehicle loses 15 to 45 % of its speed (half in HIGH MODE). Slower, the trunk stands and stops you as
    // before. A broken tree stands again once every player is RegrowDistance away (and at a race's start). The old trigger trees ('breakable',
    // the Noonday Creek trail's) are now this system too.
    [RequireComponent(typeof(TreeField))]
    public sealed class TreeTrunks : MonoBehaviour
    {
        public int pool = 160;
        public float radius = 40f;
        const float Cell = 16f;

        public static TreeTrunks Instance { get; private set; }
        public int Active { get; private set; }
        public float LastRefreshMs { get; private set; }
        public int Refreshes { get; private set; }

        TreeField field;
        float[] tx, tz, ty, tr, th;
        readonly Dictionary<Vector2Int, List<int>> grid = new Dictionary<Vector2Int, List<int>>();
        CapsuleCollider[] caps;
        Transform car;
        Vector3 lastAt = new Vector3(1e9f, 0f, 1e9f);
        float nextAt;
        readonly List<(float d, int i)> near = new List<(float, int)>();
        readonly System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();

        public static bool IsTrunk(Collider c) => c is CapsuleCollider && c.name == "trunk";
        public static int Snapped { get; private set; }   // checks: trees snapped since the start
        public static event System.Action<ArcadeVehicle, Vector3> SnappedBy;   // Round 38 (Part B): a tree snapped by this vehicle (a smash for the stunts)
        public const float RegrowDistance = 160f;
        // Woodstock Rush's TreeBreaker.Needed: the speed (m/s) a trunk of this width (m) needs; in HIGH MODE 45 % of it, at least 3.5 m/s
        public static float Needed(float width) => Mathf.Lerp(7f, 24f, Mathf.InverseLerp(.5f, 1f, width));
        public const float HighFactor = .45f, HighFloor = 3.5f;
        public static float Needed(float width, bool high) => high ? Mathf.Max(HighFloor, Needed(width) * HighFactor) : Needed(width);
        public static int Regrown { get; private set; }     // checks
        public static string Last { get; private set; } = "";   // checks: the last break
        public static bool Off = System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-treesSolid") >= 0;   // checks: Round 85's solid trees (the before)
        bool[] broken; float[] brokenAt; readonly List<int> down = new List<int>();

        void Awake()
        {
            Instance = this;
            field = GetComponent<TreeField>();
            int n = field.count;
            tx = new float[n]; tz = new float[n]; ty = new float[n]; tr = new float[n]; th = new float[n];
            for (int i = 0; i < n; i++)
            {
                var t = TreeField.Unpack(field.data, i);
                tx[i] = t.x; tz[i] = t.z; ty[i] = t.y;
                tr[i] = Mathf.Max(0.12f, t.trunkW * 0.005f);          // radius: half the trunk's diameter (cm -> m)
                th[i] = Mathf.Max(2.5f, t.trunkH * 0.1f);
                var key = new Vector2Int(Mathf.FloorToInt(t.x / Cell), Mathf.FloorToInt(t.z / Cell));
                if (!grid.TryGetValue(key, out var list)) grid[key] = list = new List<int>();
                list.Add(i);
            }
            broken = new bool[n]; brokenAt = new float[n];
            var root = new GameObject("TrunkColliders").transform; root.SetParent(transform, false);
            caps = new CapsuleCollider[pool];
            for (int k = 0; k < pool; k++) caps[k] = MakeCap(root);
            FenceChunk.AllRestored += RestoreAll;
        }

        CapsuleCollider MakeCap(Transform root)
        {
            var go = new GameObject("trunk"); go.transform.SetParent(root, false);
            var c = go.AddComponent<CapsuleCollider>(); c.direction = 1; c.enabled = false;
            go.AddComponent<TrunkSnap>().owner = this;
            return c;
        }

        void OnDestroy() { if (Instance == this) Instance = null; FenceChunk.AllRestored -= RestoreAll; }

        // a race starts: every broken tree stands again
        void RestoreAll()
        {
            foreach (int i in down) { broken[i] = false; field.SetHidden(i, false); }
            down.Clear(); FallingTree.ClearAll();
            lastAt = new Vector3(1e9f, 0f, 1e9f); foreach (var f in follows) f.lastAt = lastAt;
        }
        public void RegrowAll() => RestoreAll();
        public int Down => down.Count;   // checks
        public float TrunkWidth(int i) => i >= 0 && i < tr.Length ? tr[i] * 2f : 0f;
        public Vector3 TreeAt(int i) => new Vector3(tx[i], ty[i], tz[i]);
        public bool IsDown(int i) => i >= 0 && i < broken.Length && broken[i];

        // Round 86 (C): tree i broken by 'car' going 'dir' at 'speed': gone from the field, its capsules off, falling with debris and the wood sounds
        public bool Break(int i, ArcadeVehicle car, Vector3 dir, float speed)
        {
            if (i < 0 || i >= broken.Length || broken[i]) return false;
            broken[i] = true; brokenAt[i] = Time.time; down.Add(i); Snapped++;
            field.SetHidden(i, true);
            foreach (var c in caps) if (c.enabled && c.GetComponent<TrunkSnap>().tree == i) c.enabled = false;
            foreach (var f in follows) foreach (var c in f.caps) if (c && c.enabled && c.GetComponent<TrunkSnap>().tree == i) c.enabled = false;
            dir.y = 0f; if (dir.sqrMagnitude < 1e-4f) dir = Vector3.forward; dir.Normalize();
            FallingTree.Spawn(field, i, dir, speed);
            SnappedTree?.Invoke(i, dir);   // Round 57 (F): the replay
            BreakSounds.Play(new Vector3(tx[i], ty[i] + 1.2f, tz[i]), Mathf.Max(speed, 18f), BreakSounds.Kind.Wood);
            if (car) SnappedBy?.Invoke(car, new Vector3(tx[i], ty[i], tz[i]));
            return true;
        }

        // Round 86 (C): Woodstock Rush's TreeBreaker for one vehicle: a trunk just ahead that it is fast enough for is broken before it gets there
        static readonly RaycastHit[] ahead = new RaycastHit[16];
        void BreakAhead(ArcadeVehicle v)
        {
            if (!v || !v.isActiveAndEnabled || v.Flown || !v.Body || v.Body.isKinematic) return;
            var vel = v.Body.linearVelocity; var flat = Vector3.ProjectOnPlane(vel, Vector3.up); float speed = flat.magnitude; if (speed < HighFloor) return;
            bool high = HighMode.ForVehicle(v); var dir = flat / speed; float reach = speed * Time.fixedDeltaTime * 2f + 2.6f;
            int n = Physics.SphereCastNonAlloc(v.Body.position + Vector3.up * .5f, .9f, dir, ahead, reach, ~0, QueryTriggerInteraction.Ignore);
            for (int k = 0; k < n; k++)
            {
                var c = ahead[k].collider; if (!IsTrunk(c) || !c.enabled) continue;
                var snap = c.GetComponent<TrunkSnap>(); int i = snap ? snap.tree : -1; if (i < 0 || broken[i]) continue;
                float width = tr[i] * 2f, need = Needed(width, high); if (speed < need) continue;
                if (!Break(i, v, dir, speed)) continue;
                float size = Mathf.InverseLerp(.5f, 1f, width), loss = (.15f + .3f * size) * (high ? .5f : 1f);
                v.Body.linearVelocity = vel - flat * loss; vel = v.Body.linearVelocity; flat = Vector3.ProjectOnPlane(vel, Vector3.up); speed = flat.magnitude;
                Last = $"{v.name} broke a trunk {width:F2} m wide at {speed / (1f - loss):F1} m/s (needs {need:F1}), speed -{loss * 100:F0}%{(high ? " (HIGH)" : "")}";
                if (speed < HighFloor) break;
            }
        }

        // Round 86 (C): a broken tree stands again once every player is RegrowDistance away (and it has been down 3 s)
        float nextRegrow;
        void Regrow()
        {
            if (down.Count == 0 || Time.time < nextRegrow) return; nextRegrow = Time.time + 1f;
            var p1 = car ? car.position : new Vector3(1e9f, 0f, 1e9f); var p2c = SplitScreen.Active ? SplitScreen.P2Car : null;
            float r2 = RegrowDistance * RegrowDistance;
            for (int k = down.Count - 1; k >= 0; k--)
            {
                int i = down[k]; if (Time.time - brokenAt[i] < 3f) continue;
                float d1 = (tx[i] - p1.x) * (tx[i] - p1.x) + (tz[i] - p1.z) * (tz[i] - p1.z);
                float d2 = p2c ? (tx[i] - p2c.transform.position.x) * (tx[i] - p2c.transform.position.x) + (tz[i] - p2c.transform.position.z) * (tz[i] - p2c.transform.position.z) : float.MaxValue;
                if (d1 < r2 || d2 < r2) continue;
                broken[i] = false; field.SetHidden(i, false); FallingTree.Remove(i); down.RemoveAt(k); Regrown++;
                lastAt = new Vector3(1e9f, 0f, 1e9f); foreach (var f in follows) f.lastAt = lastAt;
            }
        }

        // Round 57 (F): a tree snapped (its index, the way it fell); a race replay snaps it again at its moment (quiet: hidden, no fall, no sound)
        public static event System.Action<int, Vector3> SnappedTree;
        public void ReplaySnap(int i, Vector3 dir, bool show)
        {
            if (i < 0 || i >= broken.Length || broken[i]) return;
            broken[i] = true; brokenAt[i] = Time.time; down.Add(i); field.SetHidden(i, true);
            if (show) { FallingTree.Spawn(field, i, dir, 12f); BreakSounds.Play(new Vector3(tx[i], ty[i] + 1f, tz[i]), 12f, BreakSounds.Kind.Wood); }
        }

        // Round 15 (item 9): the AI drivers' cars get trunk colliders round them too: a pool of ExtraPool each, ExtraRadius
        // round the car, refreshed the same way (every 0.25 s or 3 m). Track / Untrack are called by RaceController.
        public const int ExtraPool = 60; public const float ExtraRadius = 30f;
        sealed class Follow { public Transform t; public CapsuleCollider[] caps; public Vector3 lastAt = new Vector3(1e9f, 0f, 1e9f); public float nextAt; }
        readonly List<Follow> follows = new List<Follow>();
        public void Track(Transform t)
        {
            if (t == null || follows.Exists(f => f.t == t)) return;
            var root = new GameObject("TrunkColliders " + t.name).transform; root.SetParent(transform, false);
            var f = new Follow { t = t, caps = new CapsuleCollider[ExtraPool] };
            for (int k = 0; k < ExtraPool; k++) f.caps[k] = MakeCap(root);
            follows.Add(f);
        }
        public void Untrack(Transform t)
        {
            for (int i = follows.Count - 1; i >= 0; i--)
                if (follows[i].t == t || follows[i].t == null) { if (follows[i].caps.Length > 0 && follows[i].caps[0]) Destroy(follows[i].caps[0].transform.parent.gameObject); follows.RemoveAt(i); }
        }

        void FixedUpdate()
        {
            Regrow();
            for (int i = follows.Count - 1; i >= 0; i--)
            {
                var f = follows[i]; if (f.t == null) { Untrack(null); continue; }
                if (!Off) BreakAhead(f.t.GetComponent<ArcadeVehicle>());   // Round 86 (C): the AI rivals and split screen's player 2 break trees too
                var q = f.t.position;
                if ((q - f.lastAt).sqrMagnitude < 9f && Time.time < f.nextAt) continue;
                f.lastAt = q; f.nextAt = Time.time + 0.25f; Fill(q, ExtraRadius, f.caps, out _);
            }
            if (car == null) { var v = FindAnyObjectByType<ArcadeVehicle>(); if (v == null) return; car = v.transform; }
            if (!Off) { if (!carV || carV.transform != car) carV = car.GetComponent<ArcadeVehicle>(); BreakAhead(carV); }
            var p = car.position;
            // Round 69 follow-up (the plane's frame rate): high over the ground (the plane, the helicopter) no trunk is in reach, so the pool is put
            // away once and not moved; moving its static capsules every few metres at 95 m/s cost PhysX most of its time. Back down, it refills at once.
            if (HighSkip && GroundQuery.TryHeight(p.x, p.z, out float gy) && p.y - gy > HighAbove)
            {
                if (!high) { high = true; Fill(new Vector3(1e7f, 0f, 1e7f), radius, caps, out _); Active = 0; }
                lastAt = new Vector3(1e9f, 0f, 1e9f); return;
            }
            high = false;
            if ((p - lastAt).sqrMagnitude < 9f && Time.time < nextAt) return;
            Refresh(p);
        }
        ArcadeVehicle carV;
        public static bool HighSkip = true; const float HighAbove = 80f; bool high;   // HighSkip off: as before (a check's "before")

        // also called by a reset, so the trees round the new spot are solid before the car is placed there
        public void Refresh(Vector3 p)
        {
            watch.Restart();
            lastAt = p; nextAt = Time.time + 0.25f;
            Fill(p, radius, caps, out int used);
            Active = used; Refreshes++;
            LastRefreshMs = (float)watch.Elapsed.TotalMilliseconds;
        }

        // Round 70 (B): any standing trunk within 'rad' of p (the AI's racing line keeps off the trees beside a road; they have colliders only near a vehicle)
        public bool AnyTrunk(Vector3 p, float rad)
        {
            if (grid == null) return false;
            int r = Mathf.CeilToInt(rad / Cell), cx = Mathf.FloorToInt(p.x / Cell), cz = Mathf.FloorToInt(p.z / Cell);
            for (int x = cx - r; x <= cx + r; x++)
                for (int z = cz - r; z <= cz + r; z++)
                {
                    if (!grid.TryGetValue(new Vector2Int(x, z), out var list)) continue;
                    foreach (int i in list) { if (broken[i]) continue; float dx = tx[i] - p.x, dz = tz[i] - p.z, rr = rad + tr[i]; if (dx * dx + dz * dz < rr * rr) return true; }
                }
            return false;
        }

        // the nearest trees within 'rad' of p get the colliders of 'set' (Round 15: shared by the player's pool and the AI pools)
        void Fill(Vector3 p, float rad, CapsuleCollider[] set, out int used)
        {
            float radius = rad; int pool = set.Length; var caps = set;
            near.Clear();
            int r = Mathf.CeilToInt(radius / Cell);
            int cx = Mathf.FloorToInt(p.x / Cell), cz = Mathf.FloorToInt(p.z / Cell);
            float r2 = radius * radius;
            for (int x = cx - r; x <= cx + r; x++)
                for (int z = cz - r; z <= cz + r; z++)
                {
                    if (!grid.TryGetValue(new Vector2Int(x, z), out var list)) continue;
                    foreach (int i in list)
                    {
                        if (broken[i]) continue;   // Round 19: snapped, gone till a race starts
                        float dx = tx[i] - p.x, dz = tz[i] - p.z, d = dx * dx + dz * dz;
                        if (d < r2) near.Add((d, i));
                    }
                }
            if (near.Count > pool) near.Sort((a, b) => a.d.CompareTo(b.d));
            used = Mathf.Min(pool, near.Count);
            for (int k = 0; k < pool; k++)
            {
                var c = caps[k];
                if (k >= used) { if (c.enabled) c.enabled = false; continue; }
                int i = near[k].i;
                c.transform.position = new Vector3(tx[i], ty[i] + th[i] * 0.5f, tz[i]);
                c.radius = tr[i]; c.height = th[i];
                c.GetComponent<TrunkSnap>().tree = i;   // Round 86 (C): every capsule solid (Round 19's trigger trees are gone)
                if (!c.enabled) c.enabled = true;
            }
        }
    }
}

namespace ChastainRush
{
    // Round 19: on each pooled trunk capsule: which tree it stands on now (Round 86: every capsule solid, broken ahead by TreeTrunks.BreakAhead)
    public sealed class TrunkSnap : MonoBehaviour
    {
        public TreeTrunks owner; public int tree = -1;
    }

    // Round 19: a snapped tree falls over (pivoting at its foot, speeding up like a falling tree) away from the vehicle that hit it.
    // Round 86 (C): Woodstock Rush's way (SceneryTrees 1.3): it falls in about 1.5 s with a thud as it lands, and lies there (no collider) until
    // TreeTrunks puts the tree back (Remove); nine pieces of debris (four of bark, five of leaves) fly off the way it was hit and are gone in 2.2 s.
    public sealed class FallingTree : MonoBehaviour
    {
        const int MaxFalling = 60; const float FallTime = 1.5f, FallAngle = 88f, DebrisTime = 2.2f;
        static readonly List<FallingTree> all = new List<FallingTree>();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] static void ClearStatics() => all.Clear();
        Vector3 axis; float age; Quaternion start; int tree; bool thud;
        public static int Count => all.Count;
        sealed class Bit { public Transform t; public Vector3 v, spin; public float size; public bool leaf; }
        readonly List<Bit> bits = new List<Bit>();

        public static void Spawn(TreeField field, int i, Vector3 dir, float speed = 12f)
        {
            Remove(i);
            while (all.Count >= MaxFalling) { var o = all[0]; all.RemoveAt(0); if (o) Destroy(o.gameObject); }
            var t = TreeField.Unpack(field.data, i);
            int sp = Mathf.Min(t.species, TreeField.Species - 1);
            float radius = t.radius * 0.05f, crownH = t.crownH * 0.01f, trunkH = t.trunkH * 0.1f, trunkW = t.trunkW * 0.01f;
            var go = new GameObject("Falling tree");
            go.transform.SetPositionAndRotation(new Vector3(t.x, t.y, t.z), Quaternion.Euler(0f, t.yaw * (360f / 65536f), 0f));
            GameObject Part(Transform parent, Mesh mesh, Vector3 scale, Vector3 at)
            {
                if (!mesh) return null;
                var g = new GameObject("part"); g.transform.SetParent(parent, false);
                g.transform.localPosition = at; g.transform.localScale = scale;
                g.AddComponent<MeshFilter>().sharedMesh = mesh;
                var r = g.AddComponent<MeshRenderer>(); r.sharedMaterial = field.material;
                return g;
            }
            Part(go.transform, field.crownNear[sp], new Vector3(radius, crownH, radius), new Vector3(0f, trunkH * 0.5f, 0f));
            Part(go.transform, field.trunkNear[sp], new Vector3(trunkW, trunkH, trunkW), Vector3.zero);
            var ft = go.AddComponent<FallingTree>(); ft.tree = i;
            dir.y = 0f; if (dir.sqrMagnitude < 1e-4f) dir = Vector3.forward; dir.Normalize();
            ft.axis = Vector3.Cross(Vector3.up, dir); ft.start = go.transform.rotation;
            // the debris: bark low, leaves through the crown (Woodstock Rush: the bush crown and the young trunk, here the young tree's)
            var rng = new System.Random(Mathf.RoundToInt(t.x * 31f + t.z * 17f)); float R() => (float)rng.NextDouble();
            var at0 = new Vector3(t.x, t.y + 1.2f, t.z); int young = TreeField.Species - 1;
            for (int k = 0; k < 9; k++)
            {
                bool leaf = k >= 4; float size = leaf ? .6f + R() * .6f : Mathf.Clamp(trunkW * .6f, .15f, .5f);
                var v = dir * (speed * (.25f + .2f * R())) + ft.axis * ((R() - .5f) * 6f) + Vector3.up * (2f + 4f * R());
                var g = new GameObject("debris"); g.transform.position = at0 + Vector3.up * (leaf ? (trunkH + crownH) * .5f * R() : .3f);
                var mesh = leaf ? field.crownNear[young] : field.trunkNear[young];
                if (mesh) { g.AddComponent<MeshFilter>().sharedMesh = mesh; g.AddComponent<MeshRenderer>().sharedMaterial = field.material; }
                g.transform.localScale = leaf ? Vector3.one * size * .35f : new Vector3(size, size * 2.5f, size) * .5f;
                ft.bits.Add(new Bit { t = g.transform, v = v, spin = new Vector3(R(), R(), R()) * 400f, size = size, leaf = leaf });
            }
            all.Add(ft);
        }
        // the tree stands again (TreeTrunks): its fallen copy goes
        public static void Remove(int i)
        {
            for (int k = all.Count - 1; k >= 0; k--) if (all[k] && all[k].tree == i) { var o = all[k]; all.RemoveAt(k); Destroy(o.gameObject); }
        }
        public static void ClearAll() { foreach (var o in all) if (o) Destroy(o.gameObject); all.Clear(); }
        void OnDestroy() { foreach (var b in bits) if (b.t) Destroy(b.t.gameObject); bits.Clear(); }

        void Update()
        {
            float dt = Time.deltaTime; age += dt;
            float f = Mathf.Clamp01(age / FallTime);
            transform.rotation = Quaternion.AngleAxis(6f + (FallAngle - 6f) * f * f, axis) * start;
            if (f >= 1f && !thud) { thud = true; BreakSounds.Play(transform.position + Vector3.up, 12f, BreakSounds.Kind.Wood); }
            for (int k = bits.Count - 1; k >= 0; k--)
            {
                var b = bits[k]; if (!b.t) { bits.RemoveAt(k); continue; }
                if (age > DebrisTime) { Destroy(b.t.gameObject); bits.RemoveAt(k); continue; }
                b.v += Physics.gravity * dt; b.t.position += b.v * dt; b.t.rotation = Quaternion.Euler(b.spin * age);
                if (b.leaf) b.t.localScale = Vector3.one * b.size * .35f * (1f - age / 2.4f);
            }
        }
    }
}
