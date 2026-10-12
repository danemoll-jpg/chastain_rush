using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// Round 20, item 4: Round 18's proposal markers brought back for the Noonday Creek shortcut candidates (N1 to N5, magenta;
// Tools/mapgen/creek_shortcut_candidates.py writes Data/proposals/round20_markers.json). Round 18's note follows.
// Round 18, items 2b and 2c (Dan: "can code layout markers for me on the new trail proposal and different color markers for
// where the proposed pipe is ... Maybe do an a and b proposal laid out with markers that I can see easily?"; "Can it show me
// some good spots for shortcuts"). TEMPORARY: remove with ProposalMarkers.cs, Data/proposals/ and Assets/HandMade/Proposals/
// when Dan has chosen. Nothing here is built into the world: posts every 'spacing' meters along each proposed line (a 6 m post
// and a thin 30 m beam above it, seen over the trees), a post with a colored cap and a floating label at each jump, crossing,
// checkpoint, pipe mouth and candidate. Colors: A orange, B cyan, where A and B share the line both (banded), the pipe
// purple, the reverse shortcut candidates yellow; caps: jumps red, crossings blue, checkpoints green, starts white, pipe
// mouths purple, candidates yellow. Unlit, no colliders, no shadows, no change to the terrain or the trees. Read from
// Data/proposals/round18_markers.json; the label faces from Assets/HandMade/Proposals/ProposalLabels.png (same order).
namespace ChastainRush
{
    public static partial class MapBuilder
    {
        [Serializable] class ProposalLine { public string set, name; public float[] points; public float spacing = 22f; }
        [Serializable] class ProposalMark { public string set, kind, title, text; public float x, z; }
        [Serializable] class ProposalStart { public string name; public float x, z, yaw; }
        [Serializable] class ProposalDoc { public ProposalLine[] lines; public ProposalMark[] marks; public ProposalStart[] starts; }

        static readonly Color PropA = new Color(1f, 0.45f, 0.05f), PropB = new Color(0.1f, 0.85f, 1f), PropPipe = new Color(0.6f, 0.2f, 1f),
            PropRev = new Color(1f, 0.88f, 0.1f), PropN = new Color(1f, 0.25f, 0.85f);
        static readonly Dictionary<string, Color> PropCaps = new Dictionary<string, Color>
        {
            { "start", new Color(0.97f, 0.97f, 0.97f) }, { "checkpoint", new Color(0.16f, 0.62f, 0.25f) }, { "jump", new Color(0.85f, 0.12f, 0.12f) },
            { "crossing", new Color(0.15f, 0.35f, 0.9f) }, { "pipe", PropPipe }, { "info", new Color(0.4f, 0.4f, 0.42f) }, { "shortcut", PropN },
        };
        const int LabelCols = 4; const float BoardW = 6.4f, BoardH = 2.0f;

        static void BuildProposalMarkers(TerrainGrid t, Transform parent)
        {
            string path = Path.Combine(ProjectRoot, "Data/proposals/round20_markers.json");
            if (!File.Exists(path)) return;
            var doc = JsonUtility.FromJson<ProposalDoc>(File.ReadAllText(path));
            var root = new GameObject("ProposalMarkers (Round 20, temporary)");
            root.transform.SetParent(parent, false);
            var comp = root.AddComponent<ProposalMarkers>();
            var mats = new Dictionary<Color, Material>();
            Material Mat(Color c)
            {
                if (!mats.TryGetValue(c, out var m))
                {
                    m = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { name = "Proposal" + mats.Count };
                    m.SetColor("_BaseColor", c);
                    AssetDatabase.CreateAsset(m, $"{GeneratedDir}/Proposal{mats.Count}.mat");
                    mats[c] = m;
                }
                return m;
            }
            var groups = new Dictionary<string, Transform>();
            var builders = new Dictionary<(string, Color), MeshBuilder>();
            Transform Group(string set)
            {
                if (!groups.TryGetValue(set, out var g)) { g = new GameObject("Set " + set).transform; g.SetParent(root.transform, false); groups[set] = g; }
                return g;
            }
            MeshBuilder Mb(string set, Color c) { if (!builders.TryGetValue((set, c), out var mb)) builders[(set, c)] = mb = new MeshBuilder(); Group(set); return mb; }
            Color[] SetColors(string set) => set == "A" ? new[] { PropA } : set == "B" ? new[] { PropB } : set == "AB" ? new[] { PropA, PropB } : set == "P" ? new[] { PropPipe } : set == "N" ? new[] { PropN } : new[] { PropRev };

            // a post: banded 1 m at a time in the set's colors, and a thin beam above it
            void Post(string set, Vector2 p, float w, float h, float beam)
            {
                var cols = SetColors(set); float y0 = Ground(t, p.x, p.y) - 0.2f;
                for (int k = 0; k < Mathf.CeilToInt(h); k++)
                    PropBox(Mb(set, cols[k % cols.Length]), new Vector3(p.x, y0 + k, p.y), w, Mathf.Min(1f, h - k));
                if (beam > 0f) PropBox(Mb(set, cols[0]), new Vector3(p.x, y0 + h, p.y), w * 0.4f, beam);
            }
            int posts = 0;
            foreach (var line in doc.lines)
            {
                var pts = new List<Vector2>(); for (int i = 0; i + 1 < line.points.Length; i += 2) pts.Add(new Vector2(line.points[i], line.points[i + 1]));
                bool pipe = line.set == "P";
                float acc = 0f, next = 0f;
                for (int i = 0; i + 1 < pts.Count; i++)
                {
                    float len = Vector2.Distance(pts[i], pts[i + 1]);
                    while (next <= acc + len)
                    {
                        var p = Vector2.Lerp(pts[i], pts[i + 1], (next - acc) / Mathf.Max(len, 1e-4f));
                        if (doc.marks.Any(mk => (new Vector2(mk.x, mk.z) - p).sqrMagnitude < 25f)) { next += line.spacing; continue; }   // none in front of a label
                        if (pipe) Post("P", p, 0.25f, 3f, 0f); else Post(line.set, p, 0.3f, 6f, 24f);
                        posts++; next += line.spacing;
                    }
                    acc += len;
                }
            }

            // the features: a thicker post, a cap in the kind's color, the label board on a pivot that turns to the camera
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/HandMade/Proposals/ProposalLabels.png");
            Material labelMat = null;
            if (tex != null)
            {
                labelMat = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { name = "ProposalLabels" };
                labelMat.SetTexture("_BaseMap", tex); labelMat.mainTexture = tex; labelMat.SetColor("_BaseColor", Color.white);
                labelMat.SetFloat("_Cull", (float)CullMode.Back);   // one-sided, depth-tested
                AssetDatabase.CreateAsset(labelMat, GeneratedDir + "/ProposalLabels.mat");
            }
            else Debug.LogWarning("[Round20] Assets/HandMade/Proposals/ProposalLabels.png missing (Tools/handmade/make_proposal_labels.py): no labels");
            int rows = (doc.marks.Length + LabelCols - 1) / LabelCols;
            var boards = new List<Transform>();
            for (int i = 0; i < doc.marks.Length; i++)
            {
                var mk = doc.marks[i]; var p = new Vector2(mk.x, mk.z); float gy = Ground(t, mk.x, mk.z);
                Post(mk.set, p, 0.5f, 7f, 30f);
                PropBox(Mb(mk.set, PropCaps.TryGetValue(mk.kind, out var cap) ? cap : Color.white), new Vector3(p.x, gy + 6.8f, p.y), 1.2f, 1.2f);
                if (labelMat == null) continue;
                var pivot = new GameObject("Label " + mk.title).transform;
                pivot.SetParent(Group(mk.set), false); pivot.position = new Vector3(p.x, gy + 4.2f, p.y);
                var board = new GameObject("Board"); board.transform.SetParent(pivot, false); board.transform.localPosition = new Vector3(0f, 0f, -0.4f);   // just in front of the post
                float u0 = (i % LabelCols) / (float)LabelCols, u1 = u0 + 1f / LabelCols;
                float v1 = 1f - (i / LabelCols) / (float)rows, v0 = v1 - 1f / rows;
                float iu = 1f / (LabelCols * 512f), iv = 1f / (rows * 160f);
                var mesh = new Mesh { name = "ProposalLabel" + i };
                mesh.vertices = new[] { new Vector3(-BoardW / 2, -BoardH / 2, 0), new Vector3(BoardW / 2, -BoardH / 2, 0), new Vector3(BoardW / 2, BoardH / 2, 0), new Vector3(-BoardW / 2, BoardH / 2, 0) };
                mesh.uv = new[] { new Vector2(u0 + iu, v0 + iv), new Vector2(u1 - iu, v0 + iv), new Vector2(u1 - iu, v1 - iv), new Vector2(u0 + iu, v1 - iv) };
                mesh.triangles = new[] { 0, 3, 2, 0, 2, 1 };   // the face toward -z: the pivot's +z points away from the camera
                mesh.RecalculateNormals(); mesh.RecalculateBounds();
                AssetDatabase.CreateAsset(mesh, $"{GeneratedDir}/Meshes/ProposalLabel{i}.asset");
                board.AddComponent<MeshFilter>().sharedMesh = mesh;
                var r = board.AddComponent<MeshRenderer>(); r.sharedMaterial = labelMat; r.shadowCastingMode = ShadowCastingMode.Off;
                boards.Add(pivot);
            }
            foreach (var kv in builders)
            {
                var go = kv.Value.ToObject($"Proposal_{kv.Key.Item1}_{ColorUtility.ToHtmlStringRGB(kv.Key.Item2)}", Mat(kv.Key.Item2), Group(kv.Key.Item1), false, false);
                GameObjectUtility.SetStaticEditorFlags(go, 0);   // shown and hidden at run time
            }
            comp.groups = groups.Values.Select(g => g.gameObject).ToArray();
            comp.lineNames = doc.lines.Select(l => l.name).ToArray();
            var lp = new List<Vector3>(); var ls = new List<int>();
            foreach (var l in doc.lines) { ls.Add(lp.Count); for (int i = 0; i + 1 < l.points.Length; i += 2) lp.Add(new Vector3(l.points[i], 0f, l.points[i + 1])); }
            comp.lineStarts = ls.ToArray(); comp.linePoints = lp.ToArray(); comp.lineColor = PropN;
            comp.boards = boards.ToArray();
            comp.startNames = doc.starts.Select(s => s.name).ToArray();
            comp.starts = doc.starts.Select(s => new Vector3(s.x, s.z, s.yaw)).ToArray();
            foreach (var g in groups.Values) g.gameObject.SetActive(false);
            Debug.Log($"[Round20] proposal markers (temporary): {posts} posts on {doc.lines.Length} lines, {doc.marks.Length} labeled marks, {doc.starts.Length} go-to points; hidden until the debug menu shows them");
        }

        // an upright box (no bottom), centered on p in x and z, from p.y up by h
        static void PropBox(MeshBuilder mb, Vector3 p, float w, float h)
        {
            float a = w / 2f;
            int b0 = mb.Vertex(p + new Vector3(-a, 0, -a)), b1 = mb.Vertex(p + new Vector3(a, 0, -a)), b2 = mb.Vertex(p + new Vector3(a, 0, a)), b3 = mb.Vertex(p + new Vector3(-a, 0, a));
            int t0 = mb.Vertex(p + new Vector3(-a, h, -a)), t1 = mb.Vertex(p + new Vector3(a, h, -a)), t2 = mb.Vertex(p + new Vector3(a, h, a)), t3 = mb.Vertex(p + new Vector3(-a, h, a));
            void Q(int q0, int q1, int q2, int q3) { mb.Tri(q0, q1, q2); mb.Tri(q0, q2, q3); }
            Q(b0, t0, t1, b1); Q(b1, t1, t2, b2); Q(b2, t2, t3, b3); Q(b3, t3, t0, b0); Q(t0, t3, t2, t1);
        }
    }
}
