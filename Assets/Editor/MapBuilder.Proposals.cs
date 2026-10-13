using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// Round 23, Part B: the proposal markers brought back for the UNDERGROUND PIPE NETWORK proposal (Tools/mapgen/pipe_network_proposal.py
// writes Data/proposals/round23_network.json; the label faces from Tools/handmade/make_proposal_labels.py). Before: Round 18 (the trail
// and P1), Round 20 (N1 to N5), Round 21 Part C (RA to RC).
// TEMPORARY: remove with ProposalMarkers.cs, Data/proposals/ and Assets/HandMade/Proposals/ when Dan has chosen. Nothing here is
// built into the world: short posts every 'spacing' meters along each proposed line in the line's own color (mains their stripe
// color, the maze branches white, the race lavender, its switch shortcuts amber, the hub's outline magenta), and at every mouth, exit,
// manhole, junction, gate, switch, checkpoint and the rest a tall post with a thin 30 m beam (seen over the trees), a cap in the
// kind's color and a floating label. Underground things are marked on the ground above them. Unlit, no colliders, no shadows, no
// change to the terrain or the trees. Hidden at start; the debug menu shows them ("Proposal markers").
namespace ChastainRush
{
    public static partial class MapBuilder
    {
        [Serializable] class ProposalLine { public string set, name; public float[] points; public float spacing = 22f; public float[] color; public int stage; }
        [Serializable] class ProposalMark { public string set, kind, title, text; public float x, z; }
        [Serializable] class ProposalStart { public string name; public float x, z, yaw; }
        [Serializable] class ProposalDoc { public ProposalLine[] lines; public ProposalMark[] marks; public ProposalStart[] starts; }

        const string ProposalFile = "Data/proposals/round23_network.json";
        // the caps (and the label boards, make_proposal_labels.py) by kind
        static readonly Dictionary<string, Color> PropCaps = new Dictionary<string, Color>
        {
            { "grille", new Color(0.59f, 0.08f, 0.08f) }, { "mouth", new Color(0.97f, 0.97f, 0.97f) }, { "junction", new Color(0.55f, 0.55f, 0.6f) }, { "hub", new Color(0.94f, 0.25f, 0.85f) },
            { "exit", new Color(0.13f, 0.67f, 0.27f) }, { "manhole", new Color(1f, 0.48f, 0f) }, { "outfall", new Color(0.1f, 0.42f, 0.82f) },
            { "aquarium", new Color(0.33f, 0.82f, 1f) }, { "shaft", new Color(0.54f, 0.17f, 0.89f) }, { "flooded", new Color(0.23f, 0.43f, 0.62f) },
            { "sign", new Color(1f, 0.84f, 0f) }, { "gate", new Color(0.82f, 0.06f, 0.06f) }, { "switch", new Color(1f, 0.72f, 0.2f) },
            { "checkpoint", new Color(0.16f, 0.62f, 0.25f) }, { "start", new Color(0.05f, 0.05f, 0.05f) },
        };
        static readonly HashSet<string> MapNamed = new HashSet<string> { "grille", "mouth", "junction", "hub", "exit", "manhole", "outfall" };
        const int LabelCols = 4; const float BoardW = 6.4f, BoardH = 2.0f;

        static void BuildProposalMarkers(TerrainGrid t, Transform parent)
        {
            string path = Path.Combine(ProjectRoot, ProposalFile);
            if (!File.Exists(path)) return;
            var doc = JsonUtility.FromJson<ProposalDoc>(File.ReadAllText(path));
            var root = new GameObject("ProposalMarkers (Round 23, temporary)");
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
            Color LineColor(ProposalLine l) => l.color != null && l.color.Length >= 3 ? new Color(l.color[0], l.color[1], l.color[2]) : Color.white;

            // a post (banded 1 m at a time in its colors) and a thin beam above it
            void Post(string set, Color[] cols, Vector2 p, float w, float h, float beam)
            {
                float y0 = Ground(t, p.x, p.y) - 0.2f;
                for (int k = 0; k < Mathf.CeilToInt(h); k++)
                    PropBox(Mb(set, cols[k % cols.Length]), new Vector3(p.x, y0 + k, p.y), w, Mathf.Min(1f, h - k));
                if (beam > 0f) PropBox(Mb(set, cols[0]), new Vector3(p.x, y0 + h, p.y), w * 0.4f, beam);
            }
            int posts = 0;
            foreach (var line in doc.lines)
            {
                var pts = new List<Vector2>(); for (int i = 0; i + 1 < line.points.Length; i += 2) pts.Add(new Vector2(line.points[i], line.points[i + 1]));
                var col = LineColor(line);
                var cols = line.set == "R" ? new[] { col, Color.black } : new[] { col };   // the race banded, so it reads apart from the main under it
                float h = line.set == "M" ? 3.5f : line.set == "R" ? 5f : 2.5f, w = line.set == "M" ? 0.35f : 0.25f;
                float acc = 0f, next = line.set == "R" ? line.spacing * 0.5f : 0f;
                for (int i = 0; i + 1 < pts.Count; i++)
                {
                    float len = Vector2.Distance(pts[i], pts[i + 1]);
                    while (next <= acc + len)
                    {
                        var p = Vector2.Lerp(pts[i], pts[i + 1], (next - acc) / Mathf.Max(len, 1e-4f));
                        if (doc.marks.Any(mk => (new Vector2(mk.x, mk.z) - p).sqrMagnitude < 25f)) { next += line.spacing; continue; }   // none in front of a label
                        Post(line.set, cols, p, w, h, 0f);
                        posts++; next += line.spacing;
                    }
                    acc += len;
                }
            }

            // the labeled points: a thicker post, a cap in the kind's color, the label board on a pivot that turns to the camera
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/HandMade/Proposals/ProposalLabels.png");
            Material labelMat = null;
            if (tex != null)
            {
                labelMat = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { name = "ProposalLabels" };
                labelMat.SetTexture("_BaseMap", tex); labelMat.mainTexture = tex; labelMat.SetColor("_BaseColor", Color.white);
                labelMat.SetFloat("_Cull", (float)CullMode.Back);   // one-sided, depth-tested
                AssetDatabase.CreateAsset(labelMat, GeneratedDir + "/ProposalLabels.mat");
            }
            else Debug.LogWarning("[Round23] Assets/HandMade/Proposals/ProposalLabels.png missing (Tools/handmade/make_proposal_labels.py): no labels");
            int rows = (doc.marks.Length + LabelCols - 1) / LabelCols;
            var boards = new List<Transform>();
            for (int i = 0; i < doc.marks.Length; i++)
            {
                var mk = doc.marks[i]; var p = new Vector2(mk.x, mk.z); float gy = Ground(t, mk.x, mk.z);
                var cap = PropCaps.TryGetValue(mk.kind, out var cc) ? cc : Color.white;
                Post(mk.set, new[] { cap, Color.white }, p, 0.5f, 7f, 30f);
                PropBox(Mb(mk.set, cap), new Vector3(p.x, gy + 6.8f, p.y), 1.2f, 1.2f);
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
            // the map layer: every line in its color (mains wide, the race and its shortcuts on top), the names of the mains, the race
            // and the branches; the exits, manholes, mouths, junctions and the hub by name (zoomed in)
            var order = doc.lines.OrderBy(l => l.set == "B" ? 0 : l.set == "H" ? 1 : l.set == "M" ? 2 : 3).ToArray();
            comp.lineNames = order.Select(l => l.name).ToArray();
            comp.lineColors = order.Select(LineColor).ToArray();
            comp.lineWidths = order.Select(l => l.set == "M" ? 5f : l.set == "R" ? 2.5f : 2f).ToArray();
            comp.lineNamed = order.Select(l => l.set != "H" && !(l.set == "R" && l.name.StartsWith("S"))).ToArray();
            var lp = new List<Vector3>(); var ls = new List<int>();
            foreach (var l in order) { ls.Add(lp.Count); for (int i = 0; i + 1 < l.points.Length; i += 2) lp.Add(new Vector3(l.points[i], 0f, l.points[i + 1])); }
            comp.lineStarts = ls.ToArray(); comp.linePoints = lp.ToArray();
            var named = doc.marks.Where(m => MapNamed.Contains(m.kind)).ToArray();
            comp.markNames = named.Select(m => m.title).ToArray();
            comp.markPoints = named.Select(m => new Vector3(m.x, 0f, m.z)).ToArray();
            comp.markColors = named.Select(m => PropCaps.TryGetValue(m.kind, out var c) ? Color.Lerp(c, Color.white, 0.25f) : Color.white).ToArray();
            comp.legend = "<color=#F040D8>Round 23 proposal:</color> <color=#E62929>M1</color> <color=#FF8C0D>M5</color> <color=#26BF40>M3</color> <color=#FFD91A>M4</color> " +
                          "<color=#2E73FF>M2</color> <color=#9E47F2>M6</color> mains, <color=#EEEEEE>B</color> maze, <color=#D9BFFF>race</color>, <color=#FFB833>switch shortcuts</color>; zoom in for exits and manholes";
            comp.boards = boards.ToArray();
            comp.startNames = doc.starts.Select(s => s.name).ToArray();
            comp.starts = doc.starts.Select(s => new Vector3(s.x, s.z, s.yaw)).ToArray();
            foreach (var g in groups.Values) g.gameObject.SetActive(false);
            Debug.Log($"[Round23] proposal markers (temporary): {posts} posts on {doc.lines.Length} lines, {doc.marks.Length} labeled marks, {doc.starts.Length} go-to points; hidden until the debug menu shows them");
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
