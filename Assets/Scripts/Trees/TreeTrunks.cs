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
        public const float SnapSpeedCost = 0.05f;          // fraction of horizontal speed lost per tree (a fence panel: 0.035)
        bool[] breakable, broken;

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
            breakable = new bool[n]; broken = new bool[n];
            if (field.breakable != null) foreach (int i in field.breakable) if (i >= 0 && i < n) breakable[i] = true;
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

        // a race starts: every snapped tree stands again
        void RestoreAll()
        {
            for (int i = 0; i < broken.Length; i++) if (broken[i]) { broken[i] = false; field.SetHidden(i, false); }
            lastAt = new Vector3(1e9f, 0f, 1e9f); foreach (var f in follows) f.lastAt = lastAt;
        }

        // Round 19: a vehicle touched breakable tree i (its trigger capsule c)
        public void Snap(int i, Collider c, Rigidbody body)
        {
            if (i < 0 || i >= broken.Length || broken[i] || !breakable[i] || body == null) return;
            var car = body.GetComponent<ArcadeVehicle>(); if (car == null) return;
            broken[i] = true; Snapped++;
            field.SetHidden(i, true);
            if (c) c.enabled = false;
            var v = body.linearVelocity; float speed = new Vector2(v.x, v.z).magnitude;
            var dir = new Vector3(tx[i] - body.position.x, 0f, tz[i] - body.position.z);
            if (speed > 1f) dir = Vector3.Lerp(dir.normalized, new Vector3(v.x, 0f, v.z) / speed, 0.6f);
            FallingTree.Spawn(field, i, dir);
            BreakSounds.Play(new Vector3(tx[i], ty[i] + 1f, tz[i]), Mathf.Max(6f, speed), BreakSounds.Kind.Wood);
            body.linearVelocity = new Vector3(v.x * (1f - SnapSpeedCost), v.y, v.z * (1f - SnapSpeedCost));
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
            for (int i = follows.Count - 1; i >= 0; i--)
            {
                var f = follows[i]; if (f.t == null) { Untrack(null); continue; }
                var q = f.t.position;
                if ((q - f.lastAt).sqrMagnitude < 9f && Time.time < f.nextAt) continue;
                f.lastAt = q; f.nextAt = Time.time + 0.25f; Fill(q, ExtraRadius, f.caps, out _);
            }
            if (car == null) { var v = FindAnyObjectByType<ArcadeVehicle>(); if (v == null) return; car = v.transform; }
            var p = car.position;
            if ((p - lastAt).sqrMagnitude < 9f && Time.time < nextAt) return;
            Refresh(p);
        }

        // also called by a reset, so the trees round the new spot are solid before the car is placed there
        public void Refresh(Vector3 p)
        {
            watch.Restart();
            lastAt = p; nextAt = Time.time + 0.25f;
            Fill(p, radius, caps, out int used);
            Active = used; Refreshes++;
            LastRefreshMs = (float)watch.Elapsed.TotalMilliseconds;
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
                bool snap = breakable[i];   // Round 19: a breakable tree's capsule is a trigger (TrunkSnap)
                if (c.isTrigger != snap) { c.isTrigger = snap; c.name = snap ? "trunk snap" : "trunk"; }
                c.GetComponent<TrunkSnap>().tree = i;
                if (!c.enabled) c.enabled = true;
            }
        }
    }
}

namespace ChastainRush
{
    // Round 19: on each pooled trunk capsule; when it is a breakable tree's trigger, a vehicle touching it snaps the tree
    public sealed class TrunkSnap : MonoBehaviour
    {
        public TreeTrunks owner; public int tree = -1;
        Collider own;
        void Awake() { own = GetComponent<Collider>(); }
        void OnTriggerEnter(Collider other) { if (own.isTrigger && owner != null && other.attachedRigidbody != null) owner.Snap(tree, own, other.attachedRigidbody); }
    }

    // Round 19: a snapped tree falls over (pivoting at its foot, speeding up like a falling tree, about 1.3 s to the ground) away
    // from the vehicle that hit it, lies there a moment, then shrinks away. Its own crown and trunk meshes (the near kit), the
    // field's material; at most MaxFalling at once (the oldest goes). Look only: no collider
    public sealed class FallingTree : MonoBehaviour
    {
        const int MaxFalling = 10; const float FallTime = 1.3f, LieTime = 1.2f, FadeTime = 1.0f, FallAngle = 84f;
        static readonly List<FallingTree> all = new List<FallingTree>();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] static void ClearStatics() => all.Clear();
        Vector3 axis; float age; Quaternion start;

        public static void Spawn(TreeField field, int i, Vector3 dir)
        {
            while (all.Count >= MaxFalling) { var o = all[0]; all.RemoveAt(0); if (o) Destroy(o.gameObject); }
            var t = TreeField.Unpack(field.data, i);
            int sp = Mathf.Min(t.species, TreeField.Species - 1);
            float radius = t.radius * 0.05f, crownH = t.crownH * 0.01f, trunkH = t.trunkH * 0.1f, trunkW = t.trunkW * 0.01f;
            var go = new GameObject("Falling tree");
            go.transform.SetPositionAndRotation(new Vector3(t.x, t.y, t.z), Quaternion.Euler(0f, t.yaw * (360f / 65536f), 0f));
            void Part(Mesh mesh, Vector3 scale, float y)
            {
                if (!mesh) return;
                var g = new GameObject("part"); g.transform.SetParent(go.transform, false);
                g.transform.localPosition = new Vector3(0f, y, 0f); g.transform.localScale = scale;
                g.AddComponent<MeshFilter>().sharedMesh = mesh;
                var r = g.AddComponent<MeshRenderer>(); r.sharedMaterial = field.material;
            }
            Part(field.crownNear[sp], new Vector3(radius, crownH, radius), trunkH * 0.5f);
            Part(field.trunkNear[sp], new Vector3(trunkW, trunkH, trunkW), 0f);
            var ft = go.AddComponent<FallingTree>();
            dir.y = 0f; if (dir.sqrMagnitude < 1e-4f) dir = Vector3.forward;
            ft.axis = Vector3.Cross(Vector3.up, dir.normalized); ft.start = go.transform.rotation;
            all.Add(ft);
        }

        void Update()
        {
            age += Time.deltaTime;
            float f = Mathf.Clamp01(age / FallTime);
            transform.rotation = Quaternion.AngleAxis(FallAngle * f * f, axis) * start;
            float fade = Mathf.Clamp01((age - FallTime - LieTime) / FadeTime);
            if (fade > 0f) transform.localScale = Vector3.one * (1f - fade);
            if (fade >= 1f) { all.Remove(this); Destroy(gameObject); }
        }
    }
}
