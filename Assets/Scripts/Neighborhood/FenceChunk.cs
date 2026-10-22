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
        [System.Serializable] public struct Piece { public int vertexStart, vertexCount; public Vector3 centre; public float restY; }   // Round 10: restY (world, 0 = centre minus debrisDrop): a lamp's pieces come down to the ground
        [System.Serializable] public struct Panel { public int firstPiece, pieceCount; public Vector3 centre; }

        public Mesh source;
        public Material material;
        public Piece[] pieces;
        public Panel[] panels;
        public bool[] broken;
        public BreakSounds.Kind sound;     // Round 15: what it sounds like when a panel breaks (set by MapBuilder)
        public MeshRenderer[] faces;       // Round 16 (D): a sign's printed face (its own textured quad), hidden and thrown off with its panel
        public int[] facePanel;            // the panel each face belongs to
        public bool free;                  // Round 56 (B): breaking it costs no speed (the Elaine barricade: the shortcut's hint, not an obstacle)
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
        // Round 10: a panel broke (the chunk, its index, its centre in the world); everything was restored (a race started)
        public static event System.Action<FenceChunk, int, Vector3> PanelBroken;
        public static event System.Action AllRestored;
        // Round 36: which vehicle broke it (the smash activities count the player's, Racer's BreakableProp.BrokenByVehicle); one chunk put back
        // on its own (the stunt park's windows and displays, StuntPark.RestoreBreakables)
        public static event System.Action<FenceChunk, int, ArcadeVehicle> PanelBrokenBy;
        public static event System.Action<FenceChunk> Restored;

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
            if (!source || !filter) { Debug.LogWarning($"[FenceChunk] {name} under {(transform.parent ? transform.parent.name : "-")}: no mesh (its asset was overwritten in the map build?): not breakable"); broken = new bool[panels != null ? panels.Length : 0]; enabled = false; return; }   // Round 38
            broken = new bool[panels.Length];
        }
        // Round 61 (C): the chunk's own copy of its mesh is made the first time a panel breaks (it was made for every chunk at load: thousands
        // of meshes copied, and their vertices read, before the world was up)
        void Live()
        {
            if (live) return;
            live = Instantiate(source);
            live.name = source.name + " (live)";
            filter.sharedMesh = live;
            original = source.vertices; srcNormals = source.normals; srcColors = source.colors; srcTris = source.triangles;
            current = (Vector3[])original.Clone();
        }
        void OnEnable() { if (!all.Contains(this)) all.Add(this); }
        void OnDisable() { all.Remove(this); }

        public void Hit(int panelIndex, ArcadeVehicle car)
        {
            if (broken[panelIndex]) return;
            Vector3 v = car.Body.linearVelocity;
            float speed = new Vector2(v.x, v.z).magnitude;
            if (speed < MinBreakSpeed) return;
            Live();
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
            if (faces != null)
                for (int f = 0; f < faces.Length; f++)
                    if (facePanel[f] == panelIndex && faces[f] && faces[f].enabled) { SpawnFace(faces[f], dir, speed, f, pieces[panel.firstPiece].restY); faces[f].enabled = false; }
            BreakSounds.Play(transform.TransformPoint(panel.centre), speed, sound);   // Round 15, item 3
            PanelBroken?.Invoke(this, panelIndex, transform.TransformPoint(panel.centre));
            PanelBrokenBy?.Invoke(this, panelIndex, car);   // Round 36
            // a little speed lost, at most MaxCostPerStep in one physics step however many panels go at once
            if (free) return;
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
            d.floorY = piece.restY != 0f ? piece.restY : transform.TransformPoint(piece.centre).y - debrisDrop;
            d.go.SetActive(true);
            moving.Add(d); FenceDebris.Ensure();
        }

        // Round 16 (D): a sign face flies off like a piece: its quad (scale baked in) with its own material
        void SpawnFace(MeshRenderer face, Vector3 dir, float speed, int salt, float restY)
        {
            while (moving.Count >= MaxMovingPieces) Recycle(moving[0]);
            var d = pool.Count > 0 ? pool.Pop() : MakeDebris();
            var src = face.GetComponent<MeshFilter>().sharedMesh; var t = face.transform; var c = t.position;
            var sv = src.vertices; var verts = new Vector3[sv.Length]; var nms = src.normals;
            for (int i = 0; i < sv.Length; i++) { verts[i] = t.TransformPoint(sv[i]) - c; nms[i] = t.TransformDirection(nms[i]); }
            var m = new Mesh { name = "sign face piece" };
            m.vertices = verts; m.normals = nms; m.uv = src.uv; m.triangles = src.triangles; m.RecalculateBounds();
            d.mesh = m; d.mf.sharedMesh = m; d.mr.sharedMaterial = face.sharedMaterial;
            d.go.transform.SetPositionAndRotation(c, Quaternion.identity);
            float h = (salt * 0.37f + 0.21f) % 1f;
            d.vel = dir * Mathf.Clamp(speed * 0.2f, 1f, 6f) + Vector3.up * Mathf.Clamp(speed * 0.05f + 0.8f * h, 0.6f, 2.2f);
            d.spin = new Vector3(180f + 200f * h, 120f * (h - 0.5f), 220f * (0.5f - h));
            d.age = 0f;
            d.floorY = restY != 0f ? restY : c.y - debrisDrop;   // on the ground beside its post
            d.go.SetActive(true);
            moving.Add(d); FenceDebris.Ensure();
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

        // Round 61 (C): the flying pieces are stepped by one runner (FenceDebris); every chunk on the map had an Update of its own that returned
        // at once on all but the first
        public static void StepDebris(float dt)
        {
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

        // Round 36: this chunk alone (its flying pieces are left to land); true if anything came back
        public bool RestoreOwn() { bool any = broken != null && System.Array.IndexOf(broken, true) >= 0; if (!any) return false; Restore(); Restored?.Invoke(this); return true; }
        void Restore()
        {
            if (broken == null) return;
            bool any = false;
            for (int i = 0; i < broken.Length; i++) if (broken[i]) { broken[i] = false; any = true; }
            if (!any || !live) return;
            current = (Vector3[])original.Clone();
            live.vertices = current;
            live.RecalculateBounds();
            if (faces != null) foreach (var f in faces) if (f) f.enabled = true;   // Round 16 (D)
        }

        // Called when a race starts or restarts: every broken fence is whole again, flying pieces vanish.
        public static void RestoreAll()
        {
            for (int i = moving.Count - 1; i >= 0; i--) { moving[i].go.transform.localScale = Vector3.one; Recycle(moving[i]); }
            foreach (var c in all) c.Restore();
            BrokenPanels = 0;
            AllRestored?.Invoke();
        }

        // Round 57 (F): a race replay breaks the panel whose middle is at 'at' (the one recorded), as a car at 'vel' would; quiet: no pieces, no sound
        // (going back in time puts the world as it was). PanelBroken is raised (a hydrant's water, a pump's blast), PanelBrokenBy is not
        public static bool ReplayBreak(Vector3 at, int panelHint, Vector3 vel, bool show)
        {
            if (!Find(at, panelHint, out var c, out int i)) return false;
            if (c.broken[i]) return true;
            c.Live();
            c.broken[i] = true; BrokenPanels++;
            var panel = c.panels[i]; float speed = new Vector2(vel.x, vel.z).magnitude; var dir = speed > 0.1f ? new Vector3(vel.x, 0f, vel.z) / speed : c.transform.forward;
            for (int k = 0; k < panel.pieceCount; k++)
            {
                var piece = c.pieces[panel.firstPiece + k];
                if (show) c.SpawnDebris(piece, dir, Mathf.Max(speed, 6f), k);
                for (int v = 0; v < piece.vertexCount; v++) c.current[piece.vertexStart + v] = piece.centre;
            }
            c.live.vertices = c.current; c.live.RecalculateBounds();
            if (c.faces != null)
                for (int f = 0; f < c.faces.Length; f++)
                    if (c.facePanel[f] == i && c.faces[f] && c.faces[f].enabled) { if (show) c.SpawnFace(c.faces[f], dir, Mathf.Max(speed, 6f), f, c.pieces[panel.firstPiece].restY); c.faces[f].enabled = false; }
            if (show) BreakSounds.Play(c.transform.TransformPoint(panel.centre), Mathf.Max(speed, 6f), c.sound);
            PanelBroken?.Invoke(c, i, c.transform.TransformPoint(panel.centre));
            return true;
        }
        public static bool ReplayRestore(Vector3 at) => Find(at, 0, out var c, out _) && c.RestoreOwn();   // Round 57 (F): a chunk put back at its moment
        public static bool BrokenAt(Vector3 at) => Find(at, -1, out var c, out int i) && c.broken[i];
        static bool Find(Vector3 at, int hint, out FenceChunk chunk, out int panel)
        {
            chunk = null; panel = -1; float best = 0.3f * 0.3f;
            foreach (var c in all)
            {
                if (c.panels == null || c.broken == null || c.source == null) continue;
                if (hint >= 0 && hint < c.panels.Length && (c.transform.TransformPoint(c.panels[hint].centre) - at).sqrMagnitude < best) { chunk = c; panel = hint; return true; }
            }
            foreach (var c in all)
            {
                if (c.panels == null || c.broken == null || c.source == null) continue;
                for (int i = 0; i < c.panels.Length; i++) { float d = (c.transform.TransformPoint(c.panels[i].centre) - at).sqrMagnitude; if (d < best) { best = d; chunk = c; panel = i; } }
            }
            return chunk != null;
        }

        public static int TotalBroken()
        {
            int n = 0;
            foreach (var c in all) foreach (bool b in c.broken) if (b) n++;
            return n;
        }
    }

}
