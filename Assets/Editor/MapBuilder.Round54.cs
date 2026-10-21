using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// Round 54 (Dan, 2026-10-21 04:27; reports BUG-001 to BUG-010 of session 2026-10-21_03-39-54-769_591b8d): Addison Heights' new world pieces, from
// Data/tracks/addison_heights.json (Tools/mapgen/make_addison_route.py); the race's side (the shortcuts, the line, the barriers) is Data/routes/addison_heights.json.
// They stand in Free Roam too (world), built in the step "Addison" with Round 53's road crew.
//   A1 the Elaine jump's way: taken out in Round 55 (its grade, cleared line, beds and landing zone were data in the track file)
//   A2 the Joel Drive terrace (BUG-003): a backyard terrace level with the street, held up by a timber retaining wall, 10 m out over the 1 in 2.5 slope;
//      its top and its edge board one solid prism ("sc A2 terrace": a real jump by physics alone), the wall's ties drawn on its sides; landing zone TL
//   A3 the bounce house (BUG-004): an inflatable castle at the bluff's foot behind 1192 and 1047 (Neighborhood/BounceHouse.cs, no collider), balloons on
//      its turrets, its blower; worn tracks from Suholden Circle through the side yard to its arch
//   A6 the sharp-curve signs (BUG-005): a yellow turn sign and a 15 MPH plate on a post, each approach to Embry Lane's west bend
//      (Tools/handmade/make_curve_sign.py, hand-made, committed); they break
namespace ChastainRush
{
    public static partial class MapBuilder
    {
        [Serializable] class R54Terrace { public float[] root, dir; public float len, width, lip, y; }
        [Serializable] class R54Bounce { public float[] front, dir, target, tracks; public float flight, land_yaw; }
        [Serializable] class R54Sign { public string id, face, plate; public float[] at; public float yaw; }
        [Serializable] class R54Level { public string id; public float[] points; public float half, blend; }
        [Serializable] class R54File { public R54Terrace terrace; public R54Bounce bounce_house; public R54Sign[] signs; public R54Level[] level_lines, smooth_lines; }

        // ------------------------------------------------------------ A5: the lap's roads levelled (planning, before the roads are built)
        // Dan (BUG-002, Suholden Circle): "there are lot of these bumps throuhout the course in both idrections. Can we flatten these all out". Measured on
        // the built ground along both lanes' wheel tracks: few creases along the road but many twists (the side-to-side slope changing by 0.2 to 1.4 m
        // in 6 m: the house lots, the drives and the junction stems graded into the road's edges after mapgen levelled it) and three creases at the
        // junctions (Joel / Hayworth, Hayworth / Crawford, Suholden / Kelly). Across the road's width the ground is set to the road's own centerline
        // profile, that profile smoothed over a few meters (a Gaussian, 2.5 m: grades and crests stay, the creases go), blending back over 'blend'
        // the shortcuts' ways where they come down onto the levelled lap (A1 onto Crawford Circle, A2 onto Hayworth Drive): the ground along each line
        // as it is after the levelling, smoothed along it (a Gaussian, 4 m) so it runs onto the road with no ledge, set across its width
        static void R54SmoothJoins(TerrainGrid terrain)
        {
            var d = LoadAddison54(); if (d?.smooth_lines == null) return;
            foreach (var ln in d.smooth_lines)
            {
                var p = ToPoints(ln.points); int n = p.Count; if (n < 3) continue;
                var raw = new float[n]; for (int i = 0; i < n; i++) raw[i] = terrain.Height(p[i].x, p[i].y);
                var cs = new float[n]; for (int i = 1; i < n; i++) cs[i] = cs[i - 1] + (p[i] - p[i - 1]).magnitude;
                var y = new float[n];
                for (int i = 0; i < n; i++)
                {
                    float sw = 0f, sy = 0f;
                    for (int j = 0; j < n; j++) { float k = Mathf.Exp(-0.5f * (cs[j] - cs[i]) * (cs[j] - cs[i]) / 16f); sw += k; sy += raw[j] * k; }
                    y[i] = sy / sw;
                }
                y[n - 1] = raw[n - 1]; y[0] = raw[0];   // its ends stay where they meet the road and the slope
                GradeAlong(terrain, new RidgeGrade { id = ln.id, points = ln.points, y = y, half = ln.half, blend = ln.blend });
            }
        }

        static void R54LevelLap(TerrainGrid terrain)
        {
            var d = LoadAddison54(); if (d?.level_lines == null) return;
            foreach (var ln in d.level_lines)
            {
                var p = ToPoints(ln.points); int n = p.Count; if (n < 3) continue;
                var raw = new float[n]; for (int i = 0; i < n; i++) raw[i] = terrain.Height(p[i].x, p[i].y);
                var cs = new float[n]; for (int i = 1; i < n; i++) cs[i] = cs[i - 1] + (p[i] - p[i - 1]).magnitude;
                var sm = new float[n];
                for (int i = 0; i < n; i++)
                {
                    float sw = 0f, sy = 0f;
                    for (int j = Mathf.Max(0, i - 12); j <= Mathf.Min(n - 1, i + 12); j++) { float k = Mathf.Exp(-0.5f * (cs[j] - cs[i]) * (cs[j] - cs[i]) / (2.5f * 2.5f)); sw += k; sy += raw[j] * k; }
                    sm[i] = sy / sw;
                }
                // the segments in 8 m cells, for the nearest-segment search
                const float Cell = 8f; float reach = ln.half + ln.blend;
                var cells = new Dictionary<Vector2Int, List<int>>();
                for (int i = 0; i + 1 < n; i++)
                {
                    var lo = Vector2.Min(p[i], p[i + 1]) - Vector2.one * reach; var hi = Vector2.Max(p[i], p[i + 1]) + Vector2.one * reach;
                    for (int cx = Mathf.FloorToInt(lo.x / Cell); cx <= Mathf.FloorToInt(hi.x / Cell); cx++)
                        for (int cz = Mathf.FloorToInt(lo.y / Cell); cz <= Mathf.FloorToInt(hi.y / Cell); cz++)
                        { var key = new Vector2Int(cx, cz); if (!cells.TryGetValue(key, out var l)) cells[key] = l = new List<int>(); l.Add(i); }
                }
                float most = 0f; int moved = 0;
                var area = Rect.MinMaxRect(p.Min(q => q.x) - reach, p.Min(q => q.y) - reach, p.Max(q => q.x) + reach, p.Max(q => q.y) + reach);
                EditTerrain(terrain, area, (x, z, y) =>
                {
                    var q = new Vector2(x, z);
                    if (!cells.TryGetValue(new Vector2Int(Mathf.FloorToInt(x / Cell), Mathf.FloorToInt(z / Cell)), out var segs)) return y;
                    float best = float.MaxValue, target = y;
                    foreach (int i in segs)
                    {
                        var ab = p[i + 1] - p[i]; float t = Mathf.Clamp01(Vector2.Dot(q - p[i], ab) / Mathf.Max(ab.sqrMagnitude, 1e-6f)); float dd = (p[i] + ab * t - q).magnitude;
                        if (dd < best) { best = dd; target = Mathf.Lerp(sm[i], sm[i + 1], t); }
                    }
                    if (best >= reach) return y;
                    float w = best <= ln.half ? 1f : Smooth01(1f - (best - ln.half) / ln.blend), ny = y + w * (target - y);
                    if (Mathf.Abs(ny - y) > 1e-4f) { most = Mathf.Max(most, Mathf.Abs(ny - y)); moved++; }
                    return ny;
                });
                float crease = 0f; for (int i = 3; i + 3 < n; i++) crease = Mathf.Max(crease, Mathf.Abs(raw[i] - (raw[i - 3] + raw[i + 3]) * 0.5f));
                Debug.Log($"[Round54] A5: {ln.id} levelled across {2f * ln.half:0.0} m along {cs[n - 1]:0} m: {moved} samples moved, by up to {most:0.00} m (the worst crease on its centerline before: {crease:0.00} m over +-3 m)");
            }
        }

        static R54File LoadAddison54()
        {
            string path = System.IO.Path.Combine(ProjectRoot, AddisonTrackFile);
            return System.IO.File.Exists(path) ? JsonUtility.FromJson<R54File>(System.IO.File.ReadAllText(path)) : null;
        }

        static void BuildAddison54(TerrainGrid terrain, Materials mats, Transform root)
        {
            var d = LoadAddison54(); if (d == null) return;
            if (d.terrace != null && d.terrace.root != null) BuildTerrace(terrain, mats, root, d.terrace);
            if (d.bounce_house != null && d.bounce_house.front != null) BuildBounceHouse(terrain, mats, root, d.bounce_house);
            if (d.signs != null) BuildCurveSigns(terrain, mats, root, d.signs);
        }

        // ------------------------------------------------------------ A2: the terrace
        static void BuildTerrace(TerrainGrid terrain, Materials mats, Transform root, R54Terrace t)
        {
            var go = new GameObject("Joel Drive terrace (A2)").transform; go.SetParent(root, false);
            var o = new Vector2(t.root[0], t.root[1]); var dir = new Vector2(t.dir[0], t.dir[1]).normalized; var nl = new Vector2(-dir.y, dir.x);
            float hw = t.width * 0.5f, y = Ground(terrain, o.x, o.y);   // level with the street as built at its root
            float Top(float s) => y + t.lip * Mathf.Clamp01((s - (t.len - 2f)) / 2f);
            var prof = new List<float>(); for (float s = 0f; s <= t.len + 1e-3f; s += 0.5f) { prof.Add(s); prof.Add(Top(s)); }
            var pavers = new Color(0.60f, 0.56f, 0.50f);
            var pr = new ScPrism
            {
                name = "sc A2 terrace", kind = "ramp", no_face = new[] { 0 }, origin = new[] { o.x, o.y }, axis = new[] { dir.x, dir.y }, profile = prof.ToArray(),
                colour = new[] { pavers.r, pavers.g, pavers.b },
                poly = new[] { o + nl * hw, o - nl * hw, o + dir * t.len - nl * hw, o + dir * t.len + nl * hw }.SelectMany(q => new[] { q.x, q.y }).ToArray()
            };
            BuildPrism(pr, terrain, mats, go);
            // the retaining wall's look: timber ties laid flat along its three sides, from the ground up to the top, a cap board at the edge
            var look = new MeshBuilder(); var tie = new Color(0.40f, 0.29f, 0.19f);
            Vector3 W(Vector2 q, float yy) => new Vector3(q.x, yy, q.y);
            void Wall(Vector2 a, Vector2 b, Vector2 outN, float sa, float sb)
            {
                float lo = Mathf.Min(Ground(terrain, a.x, a.y), Ground(terrain, b.x, b.y)) - 0.1f, hi = Mathf.Max(Top(sa), Top(sb));
                int k = 0;
                for (float yy = lo; yy < hi - 0.02f; yy += 0.2f, k++)
                {
                    float y1 = Mathf.Min(yy + 0.2f, hi);
                    look.Tint = Shade(tie, 0.85f + 0.25f * Hash01(k, 5411));
                    Face(look, W(a + outN * 0.03f, yy), W(b + outN * 0.03f, yy), W(b + outN * 0.03f, y1), W(a + outN * 0.03f, y1), new Vector3(outN.x, 0f, outN.y));
                }
            }
            Wall(o + nl * hw, o + nl * hw + dir * t.len, nl, 0f, t.len);
            Wall(o - nl * hw + dir * t.len, o - nl * hw, -nl, t.len, 0f);
            Wall(o + dir * t.len - nl * hw, o + dir * t.len + nl * hw, dir, t.len, t.len);
            Beam(look, W(o + dir * (t.len - 0.1f) - nl * hw, Top(t.len) + 0.02f), W(o + dir * (t.len - 0.1f) + nl * hw, Top(t.len) + 0.02f), 0.2f, 0.06f, Shade(tie, 1.15f));
            // (Round 56, Dan BUG-001 on Joel Drive: "why is this here? and what is it?": the two flower pots at its street corners, one standing in the
            // road, are gone)
            look.ToObject("A2 terrace look", mats.House, go, false);
            Debug.Log($"[Round54] A2: the terrace at ({o.x:0.0}, {o.y:0.0}) {y:0.00} m, {t.len:0} m out at heading {Mathf.Atan2(dir.x, dir.y) * Mathf.Rad2Deg:0}, {t.width:0} m wide, " +
                      $"its edge {Top(t.len) - Ground(terrain, (o + dir * t.len).x, (o + dir * t.len).y):0.0} m over the ground");
        }

        // ------------------------------------------------------------ A3: the bounce house
        static readonly Color BounceRed = new Color(0.90f, 0.16f, 0.14f), BounceBlue = new Color(0.14f, 0.38f, 0.92f), BounceYellow = new Color(0.98f, 0.82f, 0.12f), BounceGreen = new Color(0.20f, 0.72f, 0.30f);
        static void BuildBounceHouse(TerrainGrid terrain, Materials mats, Transform root, R54Bounce b)
        {
            var go = new GameObject("Bounce house (A3)").transform; go.SetParent(root, false);
            var front = new Vector2(b.front[0], b.front[1]); var inward = new Vector2(b.dir[0], b.dir[1]).normalized;
            const float W2 = 3f, D = 6f, H = 3.2f;
            float gy = Ground(terrain, front.x, front.y), back = Ground(terrain, (front + inward * D).x, (front + inward * D).y), y0 = Mathf.Min(gy, back);
            var centre = new Vector3(front.x + inward.x * D * 0.5f, y0, front.y + inward.y * D * 0.5f);
            // the castle, drawn about its own middle (so it squashes about it): MakeFrame's front looks out of the arch
            var holder = new GameObject("Bounce castle").transform; holder.SetParent(go, false); holder.position = centre;
            var f = MakeFrame(Vector3.zero, -inward, 1f);
            var mb = new MeshBuilder();
            float vF = D * 0.5f, vB = -D * 0.5f;   // the front (the arch) and the back, about the middle
            BoxF(mb, f, -W2, W2, -0.3f, 0.55f, vB, vF, BounceRed);                         // the floor cushion
            BoxF(mb, f, -W2, W2, 0.55f, 2.5f, vB, vB + 0.45f, BounceBlue);                  // the back wall
            foreach (float s in new[] { -1f, 1f })
            {
                BoxF(mb, f, s > 0 ? W2 - 0.45f : -W2, s > 0 ? W2 : -W2 + 0.45f, 0.55f, 2.5f, vB, vF, BounceYellow);   // the side walls
                BoxF(mb, f, s > 0 ? 1.7f : -W2, s > 0 ? W2 : -1.7f, 0.55f, 2.5f, vF - 0.45f, vF, BounceBlue);           // the front either side of the arch
            }
            BoxF(mb, f, -1.7f, 1.7f, 2.0f, 2.6f, vF - 0.45f, vF, BounceRed);                 // over the arch
            BoxF(mb, f, -W2 + 0.3f, W2 - 0.3f, 2.5f, 2.7f, vB + 0.2f, vF - 0.2f, Shade(BounceGreen, 1.1f));   // the netting's top rail (a roof frame)
            foreach (float u in new[] { -W2, W2 })
                foreach (float v in new[] { vB, vF })
                {
                    var c = f.P(u, 0.5f, v);
                    Cyl(mb, c, H + 0.5f - 0.5f, 0.55f, 0.5f, (u > 0) == (v > 0) ? BounceRed : BounceYellow, 12);   // the turrets
                    Cyl(mb, c + Vector3.up * (H - 0.0f), 1.1f, 0.62f, 0.03f, BounceBlue, 12);                         // their cones
                }
            var lookGo = mb.ToObject("Bounce castle look", mats.House, holder, false);
            GameObjectUtility.SetStaticEditorFlags(lookGo, 0); GameObjectUtility.SetStaticEditorFlags(holder.gameObject, 0);   // it squashes
            // balloons on the front turrets (look only), the blower beside it with its hose
            var deco = new MeshBuilder(); var right = new Vector2(-inward.y, inward.x);
            foreach (float s in new[] { -1f, 1f })
            {
                var knot = new Vector3(front.x + right.x * s * W2, y0 + H + 1.0f, front.y + right.y * s * W2);
                for (int i = 0; i < 3; i++)
                {
                    var c = knot + new Vector3(right.x * s * 0.3f * i, 0.9f + 0.25f * i, right.y * s * 0.3f * i) - new Vector3(inward.x, 0f, inward.y) * 0.3f * i;
                    Balloon(deco, c, 0.32f, BalloonColors[(i + (s > 0 ? 3 : 0)) % BalloonColors.Length]);
                    Beam(deco, knot, c - Vector3.up * 0.35f, 0.015f, 0.015f, new Color(0.9f, 0.9f, 0.9f));
                }
            }
            var blow = front - right * (W2 + 1.6f) + inward * 3.5f; float by = Ground(terrain, blow.x, blow.y);
            Cyl(deco, new Vector3(blow.x, by, blow.y), 0.6f, 0.35f, 0.35f, new Color(0.15f, 0.15f, 0.17f), 10);
            Beam(deco, new Vector3(blow.x, by + 0.3f, blow.y), new Vector3((front - right * W2 + inward * 3.5f).x, y0 + 0.3f, (front - right * W2 + inward * 3.5f).y), 0.3f, 0.3f, BounceBlue);
            deco.ToObject("Bounce house deco", mats.House, go, false);
            // the worn tracks in the grass from the street to the arch (the hint)
            if (b.tracks != null && b.tracks.Length >= 4)
            {
                var p = ToPoints(b.tracks); int i0 = p.FindIndex(q => DriveBands.At(q, 0.5f) == null);
                if (i0 >= 0 && p.Count - i0 > 3) WornRuts(terrain, mats, go, p.GetRange(i0, p.Count - i0 - 1), "Worn tracks A3");
            }
            var bh = go.gameObject.AddComponent<BounceHouse>();
            bh.front = new Vector3(front.x, y0, front.y); bh.inward = new Vector3(inward.x, 0f, inward.y); bh.halfWidth = W2; bh.depth = D; bh.height = H; bh.look = holder; bh.minSpeed = 10.5f;   // the final build's check: a bike up the grass reached it at 11 to 13 m/s and was bounced back
            bh.target = new Vector3(b.target[0], Ground(terrain, b.target[0], b.target[2]) + 0.6f, b.target[2]); bh.flight = b.flight; bh.landYaw = b.land_yaw;
            Debug.Log($"[Round54] A3: the bounce house at ({front.x:0.0}, {front.y:0.0}) {y0:0.00} m, its arch facing {Mathf.Atan2(-inward.x, -inward.y) * Mathf.Rad2Deg:0}; " +
                      $"it throws to ({bh.target.x:0.0}, {bh.target.y:0.0}, {bh.target.z:0.0}) in {b.flight:0.0} s, landing heading {b.land_yaw:0}");
        }

        // ------------------------------------------------------------ A6: the sharp-curve signs
        static readonly Dictionary<string, Material> roadSignMats = new Dictionary<string, Material>();
        static Material RoadSignMaterial(string name)
        {
            if (roadSignMats.TryGetValue(name, out var made) && made != null && AssetDatabase.Contains(made)) return made;   // once per build (Round 54 D2)
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/HandMade/RoadSigns/" + name + ".png");
            if (tex == null) { Debug.LogWarning("[Round54] Assets/HandMade/RoadSigns/" + name + ".png missing (Tools/handmade/make_curve_sign.py)"); return null; }
            var m = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "RoadSign" + name };
            m.SetTexture("_BaseMap", tex); m.mainTexture = tex; m.SetColor("_BaseColor", Color.white); m.SetFloat("_Smoothness", 0.25f); m.SetFloat("_Cull", (float)CullMode.Back);
            return roadSignMats[name] = GenAsset(m, GeneratedDir + "/RoadSign" + name + ".mat");
        }
        // a diamond board: the texture's diamond (its corners at the middles of the square's edges) on a diamond quad, one-sided
        static GameObject DiamondPlate(Transform parent, Vector3 centre, Vector3 facing, float side, Material mat, string name)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(centre, Quaternion.LookRotation(facing, Vector3.up));
            float h = side * 0.7071f;
            var mesh = new Mesh { name = name };
            mesh.vertices = new[] { new Vector3(0, -h, 0), new Vector3(h, 0, 0), new Vector3(0, h, 0), new Vector3(-h, 0, 0) };
            mesh.uv = new[] { new Vector2(0.5f, 0f), new Vector2(1f, 0.5f), new Vector2(0.5f, 1f), new Vector2(0f, 0.5f) };
            mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            mesh = GenAsset(mesh, $"{GeneratedDir}/Meshes/{name}_{Mathf.Abs(centre.x.GetHashCode() ^ centre.z.GetHashCode())}.asset");
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = mat; r.shadowCastingMode = ShadowCastingMode.Off;
            return go;
        }
        static void BuildCurveSigns(TerrainGrid terrain, Materials mats, Transform root, R54Sign[] signs)
        {
            var go = new GameObject("Curve warning signs (A6)").transform; go.SetParent(root, false);
            var br = new Breakables(); var steel = new Color(0.55f, 0.57f, 0.58f); var back = new Color(0.62f, 0.63f, 0.64f);
            const float Side = 0.9f, Plate = 0.6f, DiamondY = 2.45f, PlateY = 1.6f;
            foreach (var s in signs)
            {
                var at = new Vector2(s.at[0], s.at[1]); float gy = Ground(terrain, at.x, at.y);
                var face = new Vector3(Mathf.Sin(s.yaw * Mathf.Deg2Rad), 0f, Mathf.Cos(s.yaw * Mathf.Deg2Rad));   // the way the faces look (toward the driver)
                var mb = br.Begin(at); var foot = new Vector3(at.x, gy - 0.3f, at.y);
                br.Piece(() => Post(mb, foot, DiamondY + 0.3f + 0.1f, 0.07f, steel), gy + 0.05f);
                // the backs (plain aluminum), just behind the faces
                br.Piece(() =>
                {
                    var f = MakeFrame(new Vector3(at.x, gy, at.y), new Vector2(face.x, face.z), 1f); float hd = Side * 0.7071f;
                    mb.Tint = back;
                    Face(mb, f.P(0f, DiamondY - hd, -0.05f), f.P(hd, DiamondY, -0.05f), f.P(0f, DiamondY + hd, -0.05f), f.P(-hd, DiamondY, -0.05f), -face);
                    Face(mb, f.P(-Plate / 2, PlateY - Plate / 2, -0.05f), f.P(Plate / 2, PlateY - Plate / 2, -0.05f), f.P(Plate / 2, PlateY + Plate / 2, -0.05f), f.P(-Plate / 2, PlateY + Plate / 2, -0.05f), -face);
                }, gy + 0.05f);
                var md = RoadSignMaterial(s.face); var mp = RoadSignMaterial(s.plate);
                if (md != null) br.Face(DiamondPlate(go, new Vector3(at.x, gy + DiamondY, at.y) + face * 0.01f, -face, Side, md, "Curve sign " + s.id));
                if (mp != null) { var pl = SignPlate(go, new Vector3(at.x, gy + PlateY, at.y) + face * 0.01f, -face, Plate, mp); pl.name = "Advisory plate " + s.id; br.Face(pl); }
                br.End(new Vector3(at.x, gy + 1.4f, at.y), face, 0.9f, 2.9f, 0.5f);
                Debug.Log($"[Round54] A6: the {s.face} sign and its {s.plate} plate on {s.id} at ({at.x:0.0}, {at.y:0.0}), facing {s.yaw:0}");
            }
            br.ToObjects("Curve warning signs", mats.House, go, 1f, BreakSounds.Kind.Sign);
        }
    }
}
