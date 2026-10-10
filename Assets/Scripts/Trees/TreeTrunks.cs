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
            var root = new GameObject("TrunkColliders").transform; root.SetParent(transform, false);
            caps = new CapsuleCollider[pool];
            for (int k = 0; k < pool; k++)
            {
                var go = new GameObject("trunk"); go.transform.SetParent(root, false);
                var c = go.AddComponent<CapsuleCollider>(); c.direction = 1; c.enabled = false;
                caps[k] = c;
            }
        }

        void OnDestroy() { if (Instance == this) Instance = null; }

        void FixedUpdate()
        {
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
                        float dx = tx[i] - p.x, dz = tz[i] - p.z, d = dx * dx + dz * dz;
                        if (d < r2) near.Add((d, i));
                    }
                }
            if (near.Count > pool) near.Sort((a, b) => a.d.CompareTo(b.d));
            int used = Mathf.Min(pool, near.Count);
            for (int k = 0; k < pool; k++)
            {
                var c = caps[k];
                if (k >= used) { if (c.enabled) c.enabled = false; continue; }
                int i = near[k].i;
                c.transform.position = new Vector3(tx[i], ty[i] + th[i] * 0.5f, tz[i]);
                c.radius = tr[i]; c.height = th[i];
                if (!c.enabled) c.enabled = true;
            }
            Active = used; Refreshes++;
            LastRefreshMs = (float)watch.Elapsed.TotalMilliseconds;
        }
    }
}
