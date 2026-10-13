using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// Round 23, items 2b and 2c (Dan, 2026-10-12 23:19: "this was supposed to be the entrance into the pipe network not just two pipes
// passing under the road"): the network's first junction chamber, J1, at the far end of BF, the twin entrance pipes from the creek
// channel west of Bells Ferry Road (Data/pipes/pipes.json, 'chamber_b' and 'chamber'; MapBuilder.Pipes.cs builds the pipes). World,
// not race: a big concrete room under the ground beyond the road, all of it below the terrain (no holes, nothing seen from above):
//   room      'length_m' along the pipes, 'width_m' across, 'height_m' tall; floor level with the barrels' inverts, the two barrels
//             arriving through round openings in its back wall; stained concrete (the pipes' own material, so the lamps light it),
//             four lamps under the ceiling, the creek's two trickles running across the floor to a grated sump in the middle
//   stubs     the onward pipes, toward where the network proposal takes them (Docs/PIPE-NETWORK-PROPOSAL.md): 5 m round like the
//             mains, 'stub_m' long out of a wall, each ending at a CLOSED steel grille (barred, a hazard-striped crossbar, an amber
//             lamp over it) until Dan has picked the network and it is built. Short and straight back into the room: never a
//             dead end that traps (reverse out, or a reset puts you on the floor facing back)
//   runtime   a DrainPipe for the room (resets on its floor, headlights on) and one for each stub (wall riding, resets)
// No way out on the far side of Bells Ferry Road: the only way in and out is the entrance.
namespace ChastainRush
{
    public static partial class MapBuilder
    {
        [Serializable] class ChamberStubJson { public string wall, name; public float at; }
        [Serializable] class ChamberJson { public string id, name; public float length_m = 22f, width_m = 28f, height_m = 8f, stub_m = 10f; public ChamberStubJson[] stubs; }

        // a mesh with UVs (the pipes' stained concrete needs them), one-sided faces
        sealed class UvMesh
        {
            public readonly List<Vector3> V = new List<Vector3>(); public readonly List<Vector2> U = new List<Vector2>(); public readonly List<int> T = new List<int>();
            public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector2 ua, Vector2 ub, Vector2 uc, Vector2 ud, Vector3 facing)
            {
                if (Vector3.Dot(Vector3.Cross(b - a, c - a), facing) < 0f) { (b, d) = (d, b); (ub, ud) = (ud, ub); }
                int i = V.Count; V.Add(a); V.Add(b); V.Add(c); V.Add(d); U.Add(ua); U.Add(ub); U.Add(uc); U.Add(ud);
                T.Add(i); T.Add(i + 1); T.Add(i + 2); T.Add(i); T.Add(i + 2); T.Add(i + 3);
            }
            public GameObject ToObject(string name, Material m, Transform parent, bool collider)
            {
                var mesh = new Mesh { name = name, indexFormat = IndexFormat.UInt32 };
                mesh.SetVertices(V); mesh.SetUVs(0, U); mesh.SetTriangles(T, 0); mesh.RecalculateNormals(); mesh.RecalculateTangents(); mesh.RecalculateBounds();
                AssetDatabase.CreateAsset(mesh, $"{GeneratedDir}/Meshes/{name}.asset");
                var go = new GameObject(name); go.transform.SetParent(parent, false);
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = m; r.shadowCastingMode = ShadowCastingMode.Off;
                if (collider) { var mc = go.AddComponent<MeshCollider>(); mc.sharedMesh = mesh; mc.sharedMaterial = RampMaterial(); }
                return go;
            }
        }

        static void BuildChamber(TerrainGrid terrain, Materials mats, Transform parent, PipePlan p)
        {
            var c = p.J.chamber;
            var root = new GameObject("Chamber " + c.id + " (" + c.name + ")").transform; root.SetParent(parent, false);
            float y0 = p.Y(p.L), s0 = p.L, s1 = p.L + c.length_m, hw = c.width_m * 0.5f, h = c.height_m, R = p.R, a = R + 0.4f;
            var up = Vector3.up; var u3 = new Vector3(p.U.x, 0f, p.U.y); var n3 = new Vector3(p.Nl.x, 0f, p.Nl.y);
            Vector3 W(float s, float lat, float y) { var q = p.P(s, lat); return new Vector3(q.x, y, q.y); }
            var room = new UvMesh();

            // floor and ceiling, in 2 m tiles
            for (float s = s0; s < s1 - 1e-3f; s += 2f)
                for (float l = -hw; l < hw - 1e-3f; l += 2f)
                {
                    float sb = Mathf.Min(s + 2f, s1), lb = Mathf.Min(l + 2f, hw);
                    room.Quad(W(s, l, y0), W(sb, l, y0), W(sb, lb, y0), W(s, lb, y0), new Vector2(s / 2.4f, 0.02f), new Vector2(sb / 2.4f, 0.02f), new Vector2(sb / 2.4f, 0.04f), new Vector2(s / 2.4f, 0.04f), up);
                    room.Quad(W(s, l, y0 + h), W(sb, l, y0 + h), W(sb, lb, y0 + h), W(s, lb, y0 + h), new Vector2(s / 2.4f, 0.5f), new Vector2(sb / 2.4f, 0.5f), new Vector2(sb / 2.4f, 0.52f), new Vector2(s / 2.4f, 0.52f), -up);
                }

            // a wall from 'o' along 'along' for 'width' m, facing 'inward', with round openings (radius R, their bottoms on the floor)
            // centered 'holes' m along it: each opening in a square of half side a, the rest plain
            float vScale = 1f / (2f * Mathf.PI * 2.5f);   // the grime texture's v runs round a 2.5 m pipe: low on the wall is the stained band
            void Wall(Vector3 o, Vector3 along, float width, Vector3 inward, List<float> holes)
            {
                holes.Sort();
                Vector3 P(float t, float v) => o + along * t + up * v;
                Vector2 Uv(float t, float v) => new Vector2(t / 2.4f, Mathf.Clamp(v, 0f, 2.5f) * vScale + (v > 2.5f ? 0.25f : 0f));
                void Rect(float t0, float t1, float v0, float v1) { if (t1 - t0 < 1e-3f || v1 - v0 < 1e-3f) return; room.Quad(P(t0, v0), P(t1, v0), P(t1, v1), P(t0, v1), Uv(t0, v0), Uv(t1, v0), Uv(t1, v1), Uv(t0, v1), inward); }
                float done = 0f;
                foreach (float hc in holes)
                {
                    Rect(done, hc - a, -0.6f, h);
                    Rect(hc - a, hc + a, R + a, h);
                    const int seg = 48;
                    for (int k = 0; k < seg; k++)
                    {
                        float th0 = Mathf.PI * 2f * k / seg, th1 = Mathf.PI * 2f * (k + 1) / seg;
                        Vector2 Cq(float th) => new Vector2(hc + Mathf.Sin(th) * R, R - Mathf.Cos(th) * R);
                        Vector2 Sq(float th)
                        {
                            var d = new Vector2(Mathf.Sin(th), -Mathf.Cos(th));
                            float kx = Mathf.Abs(d.x) > 1e-4f ? a / Mathf.Abs(d.x) : float.MaxValue, ky = Mathf.Abs(d.y) > 1e-4f ? a / Mathf.Abs(d.y) : float.MaxValue;
                            float kk = Mathf.Min(kx, ky); return new Vector2(hc + d.x * kk, R + d.y * kk);
                        }
                        Vector2 c0 = Cq(th0), c1 = Cq(th1), q0 = Sq(th0), q1 = Sq(th1);
                        room.Quad(P(c0.x, c0.y), P(c1.x, c1.y), P(q1.x, q1.y), P(q0.x, q0.y), Uv(c0.x, c0.y), Uv(c1.x, c1.y), Uv(q1.x, q1.y), Uv(q0.x, q0.y), inward);
                    }
                    done = hc + a;
                }
                Rect(done, width, -0.6f, h);
            }
            // the onward stubs by wall: back (the barrels), right (-lateral), left (+lateral), far
            var right = new List<float>(); var left = new List<float>(); var far = new List<float>();
            var stubs = new List<(Vector3 q, Vector3 dir, string name)>();
            foreach (var st in c.stubs ?? new ChamberStubJson[0])
            {
                if (st.wall == "right") { right.Add(st.at); stubs.Add((W(s0 + st.at, -hw, y0), -n3, st.name)); }
                else if (st.wall == "left") { left.Add(c.length_m - st.at); stubs.Add((W(s0 + st.at, hw, y0), n3, st.name)); }
                else { far.Add(st.at + hw); stubs.Add((W(s1, st.at, y0), u3, st.name)); }
            }
            var back = new List<float>(); foreach (float bl in p.Lats) back.Add(bl + hw);
            Wall(W(s0, -hw, y0), n3, c.width_m, u3, back);                 // back wall, from the right corner across to the left
            Wall(W(s0, -hw, y0), u3, c.length_m, n3, right);               // right wall, from the back along to the far end
            Wall(W(s1, hw, y0), -u3, c.length_m, -n3, left);               // left wall, from the far end back
            Wall(W(s1, -hw, y0), n3, c.width_m, -u3, far);                 // far wall
            room.ToObject("sc Chamber " + c.id + " room", pipeConcrete, root, true);

            // the onward stubs: a short round pipe each, closed at its end by a steel grille, an amber lamp over it
            var steel = new Color(0.30f, 0.32f, 0.34f); var stripeRed = new Color(0.78f, 0.1f, 0.08f); var stripeWhite = new Color(0.92f, 0.92f, 0.9f);
            var grilleMb = new MeshBuilder(); var lampMb = new MeshBuilder();
            int si = 0;
            foreach (var (q, dir, name) in stubs)
            {
                var side = Vector3.Cross(up, dir).normalized; float len = c.stub_m;
                var barrel = new UvMesh(); const int arc = 48;
                Vector3 Ring(float d, float th) => q + dir * d + up * R + side * (Mathf.Sin(th) * R) - up * (Mathf.Cos(th) * R);
                for (float d = 0f; d < len - 1e-3f; d += 0.5f)
                    for (int k = 0; k < arc; k++)
                    {
                        float th0 = Mathf.PI * 2f * k / arc, th1 = Mathf.PI * 2f * (k + 1) / arc, d1 = Mathf.Min(d + 0.5f, len);
                        var ctr = q + dir * (d + 0.25f) + up * R; var mid = Ring(d + 0.25f, (th0 + th1) * 0.5f);
                        barrel.Quad(Ring(d, th0), Ring(d1, th0), Ring(d1, th1), Ring(d, th1), new Vector2(d / 2.4f, k / (float)arc), new Vector2(d1 / 2.4f, k / (float)arc), new Vector2(d1 / 2.4f, (k + 1) / (float)arc), new Vector2(d / 2.4f, (k + 1) / (float)arc), ctr - mid);
                    }
                // the end behind the grille: a dark round plate
                for (int k = 0; k < arc; k++)
                {
                    float th0 = Mathf.PI * 2f * k / arc, th1 = Mathf.PI * 2f * (k + 1) / arc; var ctr = q + dir * len + up * R;
                    barrel.Quad(ctr, Ring(len, th0), Ring(len, th1), ctr, new Vector2(0.5f, 0.1f), new Vector2(0.5f, 0.1f), new Vector2(0.5f, 0.1f), new Vector2(0.5f, 0.1f), -dir);
                }
                string id = $"{c.id} stub {si} ({name})";
                barrel.ToObject("sc Pipe " + id, pipeConcrete, root, true);
                // the grille, 1.5 m before the end: upright bars every 0.32 m within the circle, two crossbars (the middle one striped)
                float gd = len - 1.5f; var gc = q + dir * gd + up * R;
                for (float x = -R + 0.25f; x <= R - 0.25f + 1e-3f; x += 0.32f)
                {
                    float half = Mathf.Sqrt(Mathf.Max(0f, R * R - x * x)) - 0.05f;
                    var b0 = gc + side * x - up * half; var b1 = gc + side * x + up * half;
                    grilleMb.Tint = steel; Beam(grilleMb, b0, b1, 0.07f, 0.07f, steel, dir);
                }
                foreach (float yy in new[] { -0.9f, 1.4f })
                {
                    float half = Mathf.Sqrt(R * R - yy * yy) - 0.05f;
                    Beam(grilleMb, gc + up * yy - side * half, gc + up * yy + side * half, 0.12f, 0.12f, steel, up);
                }
                {   // the hazard-striped bar across the middle, 0.6 m segments
                    float half = R - 0.1f; int segs = Mathf.CeilToInt(2f * half / 0.6f);
                    for (int k = 0; k < segs; k++)
                    {
                        float x0 = -half + 2f * half * k / segs, x1 = -half + 2f * half * (k + 1) / segs;
                        Beam(grilleMb, gc - dir * 0.12f + up * 0.25f + side * x0, gc - dir * 0.12f + up * 0.25f + side * x1, 0.22f, 0.32f, (k & 1) == 0 ? stripeRed : stripeWhite, up);
                    }
                }
                var gcol = new GameObject("Grille " + id + " (closed)"); gcol.transform.SetParent(root, false);
                gcol.transform.SetPositionAndRotation(gc, Quaternion.LookRotation(dir, up));
                var bc = gcol.AddComponent<BoxCollider>(); bc.size = new Vector3(2f * R, 2f * R, 0.3f);
                // its amber lamp, over the grille on the crown
                var lc = q + dir * (gd - 0.6f) + up * (2f * R - 0.25f);
                lampMb.Tint = Color.white; BoxAt(lampMb, lc, side, 0.6f, 0.18f, 0.3f);
                var lg = new GameObject("Grille lamp " + id); lg.transform.SetParent(root, false); lg.transform.position = lc - up * 0.4f - dir * 0.3f;
                var l = lg.AddComponent<Light>(); l.type = LightType.Point; l.range = 9f; l.intensity = 1.8f; l.color = new Color(1f, 0.55f, 0.12f); l.shadows = LightShadows.None;
                // runtime: resets, wall riding, headlights
                var host = new GameObject("Pipe " + id + " runtime"); host.transform.SetParent(root, false);
                var dp = host.AddComponent<DrainPipe>(); dp.id = c.id + "-" + si; dp.halfWidth = R * 0.8f; dp.clearHeight = 2f * R; dp.radius = R;
                var fl = new List<Vector3>(); for (float d = 0f; d <= gd - 0.4f + 1e-3f; d += 0.5f) fl.Add(q + dir * d); dp.floor = fl.ToArray();
                dp.barrelFrom = 0f; dp.barrelTo = fl.Count - 1;
                si++;
            }
            if (grilleMb.Count > 0) { var g = grilleMb.ToObject("Chamber " + c.id + " grilles", mats.House, root, false); g.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off; }

            // four lamps under the ceiling
            foreach (var (fs, fl2) in new[] { (0.25f, -0.5f), (0.25f, 0.5f), (0.75f, -0.5f), (0.75f, 0.5f) })
            {
                var lc = W(s0 + c.length_m * fs, hw * fl2, y0 + h - 0.15f);
                lampMb.Tint = Color.white; BoxAt(lampMb, lc, u3, 1.2f, 0.2f, 0.35f);
                var lg = new GameObject("Chamber lamp"); lg.transform.SetParent(root, false); lg.transform.position = lc - up * 0.5f;
                var l = lg.AddComponent<Light>(); l.type = LightType.Point; l.range = 16f; l.intensity = 1.5f; l.color = new Color(1f, 0.82f, 0.58f); l.shadows = LightShadows.None;
            }
            var lgo = lampMb.ToObject("Chamber " + c.id + " lamps", pipeLamp, root, false); lgo.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;

            // the creek's trickles: from each barrel across the floor to a grated sump in the middle
            var sump = W((s0 + s1) * 0.5f, 0f, y0);
            var sv = new List<Vector3>(); var suv = new List<Vector2>(); var stt = new List<int>(); var line = new List<Vector3>();
            foreach (float bl in p.Lats)
            {
                var from = W(s0, bl, y0); var to = sump; float L2 = Vector3.Distance(from, to); var dd = (to - from).normalized; var sd = Vector3.Cross(up, dd);
                int n0 = sv.Count, steps = Mathf.Max(2, Mathf.CeilToInt(L2 / 0.5f));
                for (int i = 0; i <= steps; i++)
                {
                    float t = i / (float)steps; var m = Vector3.Lerp(from, to, t); float half = 0.42f + 0.06f * Mathf.Sin(t * 9f);
                    sv.Add(m - sd * half + up * 0.03f); sv.Add(m + sd * half + up * 0.03f); suv.Add(new Vector2(0f, t * L2 / 3f)); suv.Add(new Vector2(1f, t * L2 / 3f));
                    line.Add(m + up * 0.05f);
                    if (i > 0) { int k = n0 + (i - 1) * 2; stt.Add(k); stt.Add(k + 2); stt.Add(k + 1); stt.Add(k + 1); stt.Add(k + 2); stt.Add(k + 3); }
                }
            }
            { var nn = Vector3.Cross(sv[stt[1]] - sv[stt[0]], sv[stt[2]] - sv[stt[0]]); if (nn.y < 0f) for (int i = 0; i < stt.Count; i += 3) { int x = stt[i + 1]; stt[i + 1] = stt[i + 2]; stt[i + 2] = x; } }
            var smesh = new Mesh { name = "Chamber " + c.id + " trickles", indexFormat = IndexFormat.UInt32 };
            smesh.SetVertices(sv); smesh.SetUVs(0, suv); smesh.SetTriangles(stt, 0); smesh.RecalculateNormals(); smesh.RecalculateBounds();
            AssetDatabase.CreateAsset(smesh, $"{GeneratedDir}/Meshes/Chamber {c.id} trickles.asset");
            var sgo = new GameObject("Chamber " + c.id + " trickles"); sgo.transform.SetParent(root, false);
            sgo.AddComponent<MeshFilter>().sharedMesh = smesh; var smr = sgo.AddComponent<MeshRenderer>(); smr.sharedMaterial = pipeWater; smr.shadowCastingMode = ShadowCastingMode.Off;
            var ps = sgo.AddComponent<PipeStream>(); ps.line = line.ToArray(); ps.water = pipeWater; ps.splash = pipeSplash;
            var sumpMb = new MeshBuilder(); sumpMb.Tint = new Color(0.08f, 0.08f, 0.09f);
            Face(sumpMb, sump + (u3 + n3) * 1.5f + up * 0.02f, sump + (u3 - n3) * 1.5f + up * 0.02f, sump + (-u3 - n3) * 1.5f + up * 0.02f, sump + (-u3 + n3) * 1.5f + up * 0.02f, up);
            for (float x = -1.35f; x <= 1.36f; x += 0.3f) { sumpMb.Tint = steel; BoxAt(sumpMb, sump + n3 * x + up * 0.05f, u3, 2.9f, 0.06f, 0.08f); }
            sumpMb.ToObject("Chamber " + c.id + " sump grate", mats.House, root, false);

            // runtime: the room (resets on its floor along its middle, headlights on)
            var rh = new GameObject("Chamber " + c.id + " runtime"); rh.transform.SetParent(root, false);
            var rp = rh.AddComponent<DrainPipe>(); rp.id = c.id; rp.halfWidth = hw - 0.5f; rp.clearHeight = h; rp.radius = 0.1f; rp.barrelFrom = 0f; rp.barrelTo = 0f;
            var rf = new List<Vector3>(); for (float s = s0 - 1f; s <= s1 + 1e-3f; s += 0.5f) rf.Add(W(s, 0f, y0)); rp.floor = rf.ToArray();

            // the ground over it (report): the least cover over the ceiling's top (0.3 m slab)
            float least = float.MaxValue; Vector2 at = default;
            for (float s = s0; s <= s1 + 1e-3f; s += 1f) for (float l = -hw; l <= hw + 1e-3f; l += 1f) { var g = p.P(s, l); float cv = Ground(terrain, g.x, g.y) - (y0 + h + 0.3f); if (cv < least) { least = cv; at = g; } }
            if (least < 0.5f) Debug.LogError($"[Round23] chamber {c.id}: the ground over it is only {least:0.00} m at x {at.x:0}, z {at.y:0}");
            Debug.Log($"[Round23] chamber {c.id}: {c.length_m:0} x {c.width_m:0} x {h:0} m, floor {y0:0.00} m, center ({W((s0 + s1) * 0.5f, 0f, 0f).x:0.0}, {W((s0 + s1) * 0.5f, 0f, 0f).z:0.0}); " +
                      $"{stubs.Count} onward stubs of {c.stub_m:0} m ending at closed grilles ({string.Join(", ", stubs.ConvertAll(t => t.name))}); least ground over its ceiling {least:0.0} m");
        }
    }
}
