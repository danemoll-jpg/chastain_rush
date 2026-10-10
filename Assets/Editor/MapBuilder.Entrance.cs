using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// Round 7 item 6: the entrance. The divided entrance is real (Dan confirmed it from photos): two one-way carriageways with
// a planted island between them. The island is a lens about 55 m long and up to 2 m wide in this data (the carriageways are
// 8 m each here), so the planting is scaled to it: mulch, trimmed round shrubs and one tall tree at the widest point, a
// curb round it (the street curb routine), and the no-parking and no-U-turn signs on posts at its two tips. The white
// three-rail fence along the pond side is the rail run planned in Neighborhood.AddRoadSideRail.
namespace ChastainRush
{
    public static partial class MapBuilder
    {
        // ------------------------------------------------------------ helpers shared by the entrance, pond and Dan's lot

        const int TreeRound = 0, TreeOak = 1, TreeMaple = 2, TreePine = 3, TreePoplar = 4, TreeYoung = 5;
        static readonly float[] KitTrunkFraction = { 0.62f, 0.62f, 0.62f, 0.45f, 0.60f, 0.62f };

        // a tree from the Racer kit (the same meshes the forest uses), placed by hand
        static GameObject PlaceKitTree(HoodCtx c, int species, Vector3 groundPos, float height, float radius, Transform parent, float yaw = 0f)
        {
            var field = c.trees.Field;
            var go = new GameObject("Tree " + TreeField.SpeciesNames[species]);
            go.transform.SetParent(parent, false);
            go.transform.position = groundPos + Vector3.down * 0.1f;
            go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            float trunkH = Mathf.Min(25.5f, height * KitTrunkFraction[species]);
            float crownBase = trunkH * 0.5f;
            float trunkW = 0.18f + 0.018f * height * 0.9f;
            void Part(string name, Mesh mesh, Vector3 scale, float y)
            {
                if (!mesh) return;
                var g = new GameObject(name); g.transform.SetParent(go.transform, false);
                g.transform.localPosition = new Vector3(0f, y, 0f); g.transform.localScale = scale;
                g.AddComponent<MeshFilter>().sharedMesh = mesh;
                var r = g.AddComponent<MeshRenderer>(); r.sharedMaterial = c.mats.Foliage;
            }
            Part("crown", field.crownNear[species], new Vector3(radius, height - crownBase, radius), crownBase);
            Part("trunk", field.trunkNear[species], new Vector3(trunkW, trunkH, trunkW), 0f);
            var col = go.AddComponent<CapsuleCollider>();   // Round 10: solid trees, the trunk only (TreeTrunks does the forest)
            col.direction = 1; col.radius = Mathf.Max(0.12f, trunkW * 0.5f); col.height = Mathf.Max(2.5f, trunkH); col.center = new Vector3(0f, col.height * 0.5f, 0f);
            c.hood.KeepOut(new Vector2(groundPos.x, groundPos.z), Mathf.Max(1.2f, radius * 0.45f));
            return go;
        }

        // a polygon lying on the terrain: ear-clipped, then split until no edge is longer than 'step' so it follows the ground
        static void DrapePoly(MeshBuilder mb, List<Vector2> ring, float step, float lift, TerrainGrid t)
        {
            if (ring.Count < 3) return;
            var r = new List<Vector2>(ring);
            if (PolyArea(r) < 0f) r.Reverse();
            var tris = new List<(Vector2 a, Vector2 b, Vector2 c)>();
            foreach (var tri in EarClip(r)) tris.Add((r[tri.x], r[tri.y], r[tri.z]));
            void Emit(Vector2 a, Vector2 b, Vector2 cc, int depth)
            {
                float l0 = (a - b).magnitude, l1 = (b - cc).magnitude, l2 = (cc - a).magnitude;
                float m = Mathf.Max(l0, Mathf.Max(l1, l2));
                if (m > step && depth < 6)
                {
                    if (m == l0) { var mid = (a + b) / 2; Emit(a, mid, cc, depth + 1); Emit(mid, b, cc, depth + 1); }
                    else if (m == l1) { var mid = (b + cc) / 2; Emit(a, b, mid, depth + 1); Emit(a, mid, cc, depth + 1); }
                    else { var mid = (cc + a) / 2; Emit(a, b, mid, depth + 1); Emit(mid, b, cc, depth + 1); }
                    return;
                }
                int v = mb.Vertex(new Vector3(a.x, Ground(t, a.x, a.y) + lift, a.y));
                mb.Vertex(new Vector3(b.x, Ground(t, b.x, b.y) + lift, b.y));
                mb.Vertex(new Vector3(cc.x, Ground(t, cc.x, cc.y) + lift, cc.y));
                mb.Tri(v, v + 2, v + 1);
            }
            foreach (var tri in tris) Emit(tri.a, tri.b, tri.c, 0);
        }

        static Texture2D SignTexture(bool uturn)
        {
            const int N = 128;
            var px = new Color[N * N];
            var white = new Color(0.97f, 0.97f, 0.97f); var red = new Color(0.82f, 0.08f, 0.08f); var black = new Color(0.08f, 0.08f, 0.09f);
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    float fx = (x + 0.5f) / N * 2f - 1f, fy = (y + 0.5f) / N * 2f - 1f;   // -1..1, y up
                    Color col = white;
                    float rad = Mathf.Sqrt(fx * fx + fy * fy);
                    if (rad > 0.97f) col = new Color(0.55f, 0.1f, 0.1f);                  // edge of the plate (the corners get a rim)
                    if (rad < 0.86f && rad > 0.70f) col = red;                            // the red ring
                    float slash = (fx - fy) / 1.4142f;                                    // diagonal from top-left to bottom-right
                    if (rad < 0.86f && Mathf.Abs((fx + fy) / 1.4142f) < 0.075f) col = red;
                    if (rad < 0.70f)
                    {
                        if (!uturn)
                        {
                            // a blocky P: stem and bowl
                            bool stem = fx > -0.32f && fx < -0.12f && fy > -0.45f && fy < 0.48f;
                            float bx = fx - 0.0f, by = fy - 0.2f;
                            bool bowl = (fx > -0.12f) && ((bx * bx / 0.09f + by * by / 0.06f) < 1f) && !((bx * bx / 0.02f + by * by / 0.015f) < 1f);
                            if (stem || bowl) col = black;
                        }
                        else
                        {
                            // a U-turn arrow: up on the left, round the top, down on the right with a head
                            bool left = fx > -0.36f && fx < -0.22f && fy > -0.42f && fy < 0.14f;
                            bool right = fx > 0.22f && fx < 0.36f && fy > -0.14f && fy < 0.14f;
                            float ax = fx, ay = fy - 0.14f;
                            bool top = ay > 0f && ax * ax / 0.1f + ay * ay / 0.08f < 1f && ax * ax / 0.048f + ay * ay / 0.04f > 1f;
                            bool head = fy < -0.14f && fy > -0.36f && Mathf.Abs(fx - 0.29f) < (fy + 0.36f) * 0.9f + 0.0f;
                            if (left || right || top || head) col = black;
                        }
                    }
                    if (rad > 0.86f) col = rad > 0.97f ? new Color(0.5f, 0.5f, 0.5f) : white;
                    px[y * N + x] = col;
                }
            var tex = new Texture2D(N, N, TextureFormat.RGBA32, true) { name = uturn ? "SignNoUTurn" : "SignNoParking", wrapMode = TextureWrapMode.Clamp };
            tex.SetPixels(px); tex.Apply();
            AssetDatabase.CreateAsset(tex, $"{GeneratedDir}/{tex.name}.asset");
            return tex;
        }

        static GameObject SignPlate(Transform parent, Vector3 centre, Vector3 facing, float size, Material mat)
        {
            var go = new GameObject("SignPlate"); go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(centre, Quaternion.LookRotation(facing, Vector3.up));
            var mesh = new Mesh { name = "SignQuad" };
            float h = size / 2f;
            mesh.vertices = new[] { new Vector3(-h, -h, 0), new Vector3(h, -h, 0), new Vector3(h, h, 0), new Vector3(-h, h, 0) };
            mesh.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) };  // the plate faces -z locally, so u runs with +x
            mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh, $"{GeneratedDir}/Meshes/SignQuad_{parent.childCount}_{mat.name}_{Mathf.Abs(centre.x.GetHashCode() ^ centre.z.GetHashCode())}.asset");
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = mat; r.shadowCastingMode = ShadowCastingMode.Off;
            return go;
        }

        // ------------------------------------------------------------ the entrance island

        static void BuildEntrance(HoodCtx c)
        {
            // the carriageways round the island: OSM 1161774882 runs north from the crossover (the entrance spur's inbound lane),
            // OSM 1360406930 is the outbound one that loops round the island's far end
            Road inbound = null, outbound = null;
            foreach (int ri in c.hood.Area.roads)
            {
                var r = c.map.roads[ri];
                if (r.osm_id == 1161774882) inbound = r;
                else if (r.osm_id == 1360406930) outbound = r;
            }
            if (inbound == null || outbound == null) { c.Log.Append("entrance island: carriageways not found; "); return; }
            var E = Densify(ToPoints(inbound.points), 1f);
            var Wp = Densify(ToPoints(outbound.points), 1f);
            // keep the part of the inbound piece north of the crossover (z above the service road, ~800)
            E = E.Where(p => p.y >= 800f).ToList();
            float hwE = inbound.width * 0.5f + 0.63f, hwW = outbound.width * 0.5f + 0.63f;
            List<Vector2> Inner(List<Vector2> pts, float off)
            {
                var o = new List<Vector2>();
                for (int i = 0; i < pts.Count; i++)
                {
                    Vector2 dIn = i > 0 ? (pts[i] - pts[i - 1]).normalized : (pts[1] - pts[0]).normalized;
                    Vector2 dOut = i < pts.Count - 1 ? (pts[i + 1] - pts[i]).normalized : dIn;
                    Vector2 t = (dIn + dOut).normalized; if (t.sqrMagnitude < 1e-6f) t = dOut;
                    o.Add(pts[i] + new Vector2(-t.y, t.x) * off);   // left of travel: both carriageways keep the island on their left
                }
                return o;
            }
            var eIn = Inner(E, hwE); var wIn = Inner(Wp, hwW);
            // axis and width of the lens: for each inbound inner point, the nearest outbound inner point
            var mids = new List<Vector2>(); var gaps = new List<float>();
            foreach (var e in eIn)
            {
                float best = float.MaxValue; Vector2 bp = e;
                foreach (var w in wIn) { float d = (w - e).magnitude; if (d < best) { best = d; bp = w; } }
                // signed: positive when the carriageways have not crossed
                float sgn = Vector2.Dot(bp - e, new Vector2(-1f, 0f)) >= -0.01f ? 1f : -1f;
                mids.Add((e + bp) * 0.5f); gaps.Add(best * sgn);
            }
            // mulch strip down the middle of the lens, as wide as the lens allows
            var ribPts = new List<Vector2>(); var ribW = new List<float>();
            for (int i = 0; i < mids.Count; i++) { if (gaps[i] > 0.5f) { ribPts.Add(mids[i]); ribW.Add(Mathf.Max(0.2f, gaps[i] - 0.25f)); } }
            if (ribPts.Count >= 2)
            {
                c.Ground.Tint = new Color(0.26f, 0.18f, 0.12f);
                RibbonVar(c.Ground.At(ribPts[ribPts.Count / 2]), ribPts, ribW.ToArray(), 0.055f, c.terrain);
            }
            int widest = 0; for (int i = 0; i < gaps.Count; i++) if (gaps[i] > gaps[widest]) widest = i;
            c.Log.Append($"entrance island {mids.Count} m long, widest {gaps[widest]:0.0} m; ");
            var parent = new GameObject("EntranceIsland").transform; parent.SetParent(c.root, false);
            { var ax0 = (mids[Mathf.Min(widest + 4, mids.Count - 1)] - mids[Mathf.Max(widest - 4, 0)]).normalized; AddView(c, "island", mids[widest], ax0, mids[widest]); }
            // the tall tree near the widest point, trimmed round shrubs along the lens
            if (gaps[widest] > 0.8f)
                PlaceKitTree(c, TreeMaple, new Vector3(mids[widest].x, Ground(c.terrain, mids[widest].x, mids[widest].y), mids[widest].y), 14.5f, 4.6f, parent, 40f);
            var mb = c.Props.At(mids[widest]);
            int shrubs = 0;
            for (int i = 3; i < mids.Count - 3; i += 6)
            {
                if (Mathf.Abs(i - widest) < 3 || gaps[i] < 0.9f) continue;
                float r = Mathf.Clamp(gaps[i] * 0.5f, 0.45f, 0.95f);
                Vector2 ax = (mids[Mathf.Min(i + 2, mids.Count - 1)] - mids[Mathf.Max(i - 2, 0)]).normalized;
                var m2 = c.Props.At(mids[i]);
                var g = ShrubGreens[(i / 6) % 3];
                // two overlapping domes along the lens: a trimmed hedge-like round shrub
                for (int k = -1; k <= 1; k += 2)
                {
                    Vector2 p = mids[i] + ax * (k * r * 0.55f);
                    Blob(m2, new Vector3(p.x, Ground(c.terrain, p.x, p.y) + 0.02f, p.y), new Vector3(r, r * 0.8f, r), g * 1.2f, g * 0.7f, 10, 4, true, i + k);
                }
                shrubs++;
            }
            // signs on posts at the two tips of the island, facing the traffic that leaves and enters
            var texP = SignTexture(false); var texU = SignTexture(true);
            Material SignMat(Texture2D tx)
            {
                var m = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Texture")) { name = "Sign_" + tx.name };
                m.SetTexture("_BaseMap", tx); m.SetColor("_BaseColor", Color.white); m.mainTexture = tx;
                AssetDatabase.CreateAsset(m, $"{GeneratedDir}/{m.name}.mat");
                return m;
            }
            var mp = SignMat(texP); var mu = SignMat(texU);
            var back = c.mats.Concrete;
            int[] tips = { 2, mids.Count - 3 };
            for (int ti = 0; ti < 2; ti++)
            {
                int i = Mathf.Clamp(tips[ti], 0, mids.Count - 1);
                // pick the nearest point with some width
                int j = i; for (int s = 0; s < 12 && gaps[j] < 0.7f; s++) j = Mathf.Clamp(j + (ti == 0 ? 1 : -1), 0, mids.Count - 1);
                Vector2 p = mids[j];
                float gy = Ground(c.terrain, p.x, p.y);
                var m3 = c.Props.At(p);
                Post(m3, new Vector3(p.x, gy - 0.05f, p.y), 2.35f, 0.07f, new Color(0.55f, 0.57f, 0.58f));
                Vector2 outward = ti == 0 ? Vector2.down : Vector2.up;           // tip 0 is the south end, tip 1 the north end
                var f3 = new Vector3(outward.x, 0f, outward.y);
                SignPlate(parent, new Vector3(p.x, gy + 2.0f, p.y) + f3 * 0.045f, -f3, 0.5f, mp);
                SignPlate(parent, new Vector3(p.x, gy + 1.42f, p.y) + f3 * 0.045f, -f3, 0.5f, mu);
                // grey backs
                SignPlate(parent, new Vector3(p.x, gy + 2.0f, p.y) - f3 * 0.045f, f3, 0.5f, back);
                SignPlate(parent, new Vector3(p.x, gy + 1.42f, p.y) - f3 * 0.045f, f3, 0.5f, back);
                c.hood.KeepOut(p, 0.7f);
            }
            c.Log.Append($"entrance: {shrubs} shrubs, tall tree, 2 signs; ");
        }
    }
}
