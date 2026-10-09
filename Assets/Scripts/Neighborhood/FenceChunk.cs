using System.Collections.Generic;
using Racer;
using UnityEngine;

namespace ChastainRush
{
    // Round 7: fences the car smashes through (Round 9: mailboxes too, built the same way by MapBuilder.Breakables). One FenceChunk per ~128 m cell holds a single mesh (one draw call) made of
    // panels; each panel has its own trigger box. A trigger does not push back, so a fence can never stop, flip or launch
    // the car (this is the approach of Racer's BreakableProp: "scenery yields without transferring solver impulses to the
    // car"). Racer's version gives no speed cost and flies the whole visual away; here a hit costs a little speed, and the
    // pieces of the panel (boards, post) fly off separately. The panel's vertices are collapsed in the chunk mesh and put
    // back when RestoreAll runs (a race starts). Wheel rays ignore triggers (ArcadeVehicle), so suspension is unaffected.
    public sealed class FenceChunk : MonoBehaviour
    {
        [System.Serializable] public struct Piece { public int vertexStart, vertexCount; public Vector3 centre; }
        [System.Serializable] public struct Panel { public int firstPiece, pieceCount; public Vector3 centre; }

        public Mesh source;
        public Material material;
        public Piece[] pieces;
        public Panel[] panels;
        public bool[] broken;
        public float debrisDrop = 0.55f;   // Round 9: how far below a piece's centre its debris comes to rest (mailboxes use more: the box sits 1.2 m up)

        // tuning
        public const float MinBreakSpeed = 1.5f;     // slower than this the car just touches the fence
        public const float SpeedCostPerPanel = 0.035f; // fraction of horizontal speed lost per panel broken
        public const float MaxCostPerStep = 0.12f;
        public const int MaxMovingPieces = 48;
        public const float DebrisSeconds = 3.5f;

        static readonly List<FenceChunk> all = new List<FenceChunk>();
        static readonly List<Debris> moving = new List<Debris>();
        static readonly Stack<Debris> pool = new Stack<Debris>();
        public static int BrokenPanels { get; private set; }
        public static int MovingPieceCount => moving.Count;
        static int lastCostFrame = -1; static float costThisStep;

        Mesh live;
        Vector3[] original, current, srcNormals;
        Color[] srcColors;
        int[] srcTris;
        MeshFilter filter;

        sealed class Debris
        {
            public GameObject go; public MeshFilter mf; public MeshRenderer mr;
            public Vector3 vel, spin; public float age, floorY; public Mesh mesh;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ClearStatics() { all.Clear(); moving.Clear(); pool.Clear(); BrokenPanels = 0; }

        void Awake()
        {
            filter = GetComponent<MeshFilter>();
            live = Instantiate(source);
            live.name = source.name + " (live)";
            filter.sharedMesh = live;
            original = source.vertices; srcNormals = source.normals; srcColors = source.colors; srcTris = source.triangles;
            current = (Vector3[])original.Clone();
            broken = new bool[panels.Length];
        }
        void OnEnable() { if (!all.Contains(this)) all.Add(this); }
        void OnDisable() { all.Remove(this); }

        public void Hit(int panelIndex, ArcadeVehicle car)
        {
            if (broken[panelIndex]) return;
            Vector3 v = car.Body.linearVelocity;
            float speed = new Vector2(v.x, v.z).magnitude;
            if (speed < MinBreakSpeed) return;
            broken[panelIndex] = true;
            BrokenPanels++;
            var panel = panels[panelIndex];
            Vector3 dir = new Vector3(v.x, 0f, v.z).normalized;
            for (int k = 0; k < panel.pieceCount; k++)
            {
                var piece = pieces[panel.firstPiece + k];
                SpawnDebris(piece, dir, speed, k);
                for (int i = 0; i < piece.vertexCount; i++) current[piece.vertexStart + i] = piece.centre;
            }
            live.vertices = current;
            live.RecalculateBounds();
            // a little speed lost, at most MaxCostPerStep in one physics step however many panels go at once
            if (lastCostFrame != Time.frameCount) { lastCostFrame = Time.frameCount; costThisStep = 0f; }
            float cost = Mathf.Min(SpeedCostPerPanel, MaxCostPerStep - costThisStep);
            if (cost > 0f)
            {
                costThisStep += cost;
                var vel = car.Body.linearVelocity;
                car.Body.linearVelocity = new Vector3(vel.x * (1f - cost), vel.y, vel.z * (1f - cost));
            }
        }

        void SpawnDebris(Piece piece, Vector3 dir, float speed, int salt)
        {
            while (moving.Count >= MaxMovingPieces) Recycle(moving[0]);
            var d = pool.Count > 0 ? pool.Pop() : MakeDebris();
            var verts = new Vector3[piece.vertexCount];
            var sv = original; var colours = srcColors; var tris = srcTris; var nrm = srcNormals;
            var cols = new Color[piece.vertexCount]; var nms = new Vector3[piece.vertexCount];
            for (int i = 0; i < piece.vertexCount; i++)
            {
                verts[i] = sv[piece.vertexStart + i] - piece.centre;
                cols[i] = colours[piece.vertexStart + i]; nms[i] = nrm[piece.vertexStart + i];
            }
            var idx = new List<int>();
            for (int t = 0; t < tris.Length; t += 3)
                if (tris[t] >= piece.vertexStart && tris[t] < piece.vertexStart + piece.vertexCount)
                { idx.Add(tris[t] - piece.vertexStart); idx.Add(tris[t + 1] - piece.vertexStart); idx.Add(tris[t + 2] - piece.vertexStart); }
            var m = new Mesh { name = "fence piece" };
            m.vertices = verts; m.colors = cols; m.normals = nms; m.SetTriangles(idx, 0);
            m.RecalculateBounds();
            d.mesh = m; d.mf.sharedMesh = m; d.mr.sharedMaterial = material;
            d.go.transform.SetPositionAndRotation(transform.TransformPoint(piece.centre), transform.rotation);
            float h = ((salt * 37 + Mathf.Abs(piece.centre.x * 13f + piece.centre.z * 7f)) % 1f);
            d.vel = dir * Mathf.Clamp(speed * 0.16f, 0.8f, 5.5f) * (0.7f + 0.6f * h) + Vector3.up * Mathf.Clamp(speed * 0.04f + 0.6f * h, 0.5f, 1.8f)
                  + Vector3.Cross(Vector3.up, dir) * ((h - 0.5f) * 2.4f);
            d.spin = new Vector3(120f + 160f * h, 80f * (h - 0.5f), 260f * (0.5f - h));
            d.age = 0f;
            d.floorY = transform.TransformPoint(piece.centre).y - debrisDrop;
            d.go.SetActive(true);
            moving.Add(d);
        }

        static Debris MakeDebris()
        {
            var go = new GameObject("fence debris");
            go.SetActive(false);
            var d = new Debris { go = go, mf = go.AddComponent<MeshFilter>(), mr = go.AddComponent<MeshRenderer>() };
            d.mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return d;
        }

        static void Recycle(Debris d)
        {
            moving.Remove(d);
            d.go.SetActive(false);
            if (d.mesh) Destroy(d.mesh);
            pool.Push(d);
        }

        void Update()
        {
            // debris belongs to the static list; only the first chunk alive steps it
            if (all.Count == 0 || all[0] != this) return;
            float dt = Time.deltaTime;
            for (int i = moving.Count - 1; i >= 0; i--)
            {
                var d = moving[i];
                d.age += dt;
                d.vel.y -= 9f * dt;
                var t = d.go.transform;
                var p = t.position + d.vel * dt;
                if (p.y <= d.floorY) { p.y = d.floorY; d.vel = Vector3.MoveTowards(d.vel, Vector3.zero, 9f * dt); d.spin = Vector3.MoveTowards(d.spin, Vector3.zero, 500f * dt); }
                t.position = p;
                t.Rotate(d.spin * dt, Space.Self);
                if (d.age > DebrisSeconds - 0.4f) t.localScale = Vector3.one * Mathf.Clamp01((DebrisSeconds - d.age) / 0.4f);
                if (d.age >= DebrisSeconds) { t.localScale = Vector3.one; Recycle(d); }
            }
        }

        void Restore()
        {
            if (broken == null) return;
            bool any = false;
            for (int i = 0; i < broken.Length; i++) if (broken[i]) { broken[i] = false; any = true; }
            if (!any) return;
            current = (Vector3[])original.Clone();
            live.vertices = current;
            live.RecalculateBounds();
        }

        // Called when a race starts or restarts: every broken fence is whole again, flying pieces vanish.
        public static void RestoreAll()
        {
            for (int i = moving.Count - 1; i >= 0; i--) { moving[i].go.transform.localScale = Vector3.one; Recycle(moving[i]); }
            foreach (var c in all) c.Restore();
            BrokenPanels = 0;
        }

        public static int TotalBroken()
        {
            int n = 0;
            foreach (var c in all) foreach (bool b in c.broken) if (b) n++;
            return n;
        }
    }

}
