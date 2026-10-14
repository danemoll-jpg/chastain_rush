using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// Round 25, Part B (Dan: getting round the pipes faster). From the network as built (MapBuilder.Network.cs):
//   B1  the pipe warp's places (Pipes/PipeWarp.cs: the debug menu's "Pipe warp" row): the hub, every junction, BF's and GP's two
//       pipes, every exit's inside end and outside mouth, every manhole's foot, the middle of each main; each on the floor, facing along
//       the pipe toward the hub where there is one
//   B2  the pipe express (Pipes/PipeExpress.cs; its stretches: network.json 'express', Tools/mapgen/pipe_network.py): at each end of each
//       stretch an EXPRESS band painted round the pipe (cyan, 1.2 m wide, white edges), EXPRESS boards with an arrow on both walls, and
//       arrows on the floor pointing into the stretch
namespace ChastainRush
{
    public static partial class MapBuilder
    {

        static void BuildPipeWarp(Transform root, Network n, Pipes pipes)
        {
            var names = new List<string>(); var at = new List<Vector3>(); var yaw = new List<float>();
            var hub = n.F.chambers.FirstOrDefault(c => c.kind == "hub");
            var hubAt = hub != null ? new Vector3(hub.x, hub.floor, hub.z) : Vector3.zero;
            float Yaw(Vector3 d) { d.y = 0f; return d.sqrMagnitude < 1e-6f ? 0f : Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg; }
            // along a pipe toward the hub: its 'to_hub', else toward the end nearer the hub
            float TubeYaw(NetTubeGeo g, int i)
            {
                int dir = g.J.to_hub;
                if (dir == 0) dir = (g.P[g.P.Length - 1] - hubAt).sqrMagnitude < (g.P[0] - hubAt).sqrMagnitude ? 1 : -1;
                return Yaw(g.T(i) * dir);
            }
            void Add(string name, Vector3 p, float y) { names.Add(name); at.Add(p); yaw.Add(y); }
            if (hub != null && n.Built(hub))
            {
                var u = new Vector3(Mathf.Sin(hub.yaw * Mathf.Deg2Rad), 0f, Mathf.Cos(hub.yaw * Mathf.Deg2Rad));
                Add("THE HUB (its floor)", hubAt - u * 30f, Yaw(u));
            }
            foreach (var c in n.F.chambers.Where(c => c.kind != "hub" && n.Built(c)).OrderBy(c => c.id[0] == 'J' ? 0 : c.id[0] == 'K' ? 1 : 2).ThenBy(c => int.TryParse(new string(c.id.Skip(1).TakeWhile(char.IsDigit).ToArray()), out int k) ? k : 999))
            {
                var C = new Vector3(c.x, c.floor, c.z); float y = Yaw(hubAt - C);
                if (!string.IsNullOrEmpty(c.hub_tube))   // facing the opening of the main that leads to the hub
                {
                    var o = c.openings.FirstOrDefault(x => x.tube == c.hub_tube);
                    if (o != null) { var u3 = new Vector3(Mathf.Sin(c.yaw * Mathf.Deg2Rad), 0f, Mathf.Cos(c.yaw * Mathf.Deg2Rad)); var v3 = new Vector3(u3.z, 0f, -u3.x); y = Yaw(new[] { u3, v3, -u3, -v3 }[o.wall]); }
                }
                Add(c.id == c.name ? c.id : $"{c.id} ({c.name})", C, y);
            }
            // the entrances' two pipes (8 m in past the headwall), facing in
            foreach (var p in pipes.Plans.Where(p => p.J.twin))
                foreach (float bl in p.Lats)
                {
                    var q = p.P(Mathf.Min(p.S1 + 8f, p.L - 1f), bl);
                    Add($"{p.J.id} {(bl < 0f ? "right" : "left")} pipe", new Vector3(q.x, p.Y(Mathf.Min(p.S1 + 8f, p.L - 1f)), q.y), Yaw(new Vector3(p.U.x, 0f, p.U.y)));
                }
            // every exit: its outside mouth (in front of the apron, facing in) and its inside end (8 m along the network's pipe, facing on)
            foreach (var x in n.F.exits)
            {
                var p = pipes.Plans.FirstOrDefault(pp => pp.J.id == x.id); if (p == null) continue;
                string nm = x.id + " " + x.name.Substring(0, 1) + x.name.Substring(1).ToLowerInvariant();
                float sm = Mathf.Max(0.5f, p.WallFrom - 2f); var qm = p.P(sm, 0f);
                Add(nm + ": mouth", new Vector3(qm.x, Ground0(p, sm), qm.y), Yaw(new Vector3(p.U.x, 0f, p.U.y)));
                var t = n.F.tubes.FirstOrDefault(tt => tt.a != null && tt.a.kind == "x" && tt.a.exit == x.id) ?? n.F.tubes.FirstOrDefault(tt => tt.b != null && tt.b.kind == "x" && tt.b.exit == x.id);
                if (t == null || !n.Built(t)) continue;
                var g = n.Geo[t.id]; bool fromA = t.a != null && t.a.kind == "x" && t.a.exit == x.id;
                int i = g.At(fromA ? 8f : g.S[g.S.Length - 1] - 8f);
                Add(nm + ": inside", g.P[i], Yaw(g.T(i) * (fromA ? 1f : -1f)));
            }
            // every manhole's foot (3 m before its riser), facing along the pipe toward the hub
            foreach (var m in n.F.manholes)
            {
                var t = n.F.tubes.First(tt => tt.id == m.tube); if (!n.Built(t)) continue;
                var g = n.Geo[t.id]; int i = Mathf.Max(0, g.RiserFrom - 6);
                Add($"{m.id} manhole foot ({m.road})", g.P[i], Yaw(-g.T(i)));
            }
            // the middle of each main
            foreach (var t in n.F.tubes.Where(t => t.main && n.Built(t)))
            {
                var g = n.Geo[t.id]; int i = g.At(g.S[g.S.Length - 1] * 0.5f);
                Add($"{t.id} {t.name.Split('(')[0].Trim()}: middle", g.P[i], TubeYaw(g, i));
            }
            var pw = root.gameObject.AddComponent<PipeWarp>(); pw.names = names.ToArray(); pw.at = at.ToArray(); pw.yaw = yaw.ToArray();
            Debug.Log($"[Round25] pipe warp: {names.Count} places ({string.Join(", ", names)})");
        }
        static float Ground0(PipePlan p, float s) => p.Y(Mathf.Clamp(s, 0f, p.L));

        static readonly Color ExpressCyan = new Color(0.05f, 0.80f, 0.90f);

        static void BuildPipeExpress(Transform root, Materials mats, Network n)
        {
            var list = n.F.express ?? new NetExpress[0];
            var stretches = new List<PipeExpress.Stretch>(); var cyan = new MeshBuilder(); var white = new MeshBuilder(); var boards = new UvMesh(); int rows = NetSignRows.Length;
            if (!netSigns) netSigns = HandMadeSignMaterial(NetSignAtlas, "NetworkSigns", true);
            foreach (var e in list)
            {
                var t = n.F.tubes.FirstOrDefault(tt => tt.id == e.tube); if (t == null || !n.Built(t)) continue;
                var g = n.Geo[t.id]; float R = g.R;
                int i0 = g.At(e.s0), i1 = g.At(e.s1);
                var fl = new List<Vector3>(); for (int i = i0; i <= i1; i++) fl.Add(g.P[i]);
                stretches.Add(new PipeExpress.Stretch { id = e.id, name = e.name, floor = fl.ToArray(), radius = R, top = e.top });
                // the marks at each end: the band round the pipe (from the end 1.2 m outward, away from the stretch), boards, floor arrows
                foreach (var (sEnd, inward) in new[] { (e.s0, 1f), (e.s1, -1f) })
                {
                    Vector3 W(float s, float th, float off) { int j = Mathf.Clamp(g.At(s), 0, g.P.Length - 1); return g.Axis(j) + (g.Side(j) * Mathf.Sin(th) - g.Up(j) * Mathf.Cos(th)) * (R - off); }
                    float a0 = sEnd - inward * 1.6f, a1 = sEnd - inward * 0.4f;   // the band, just outside the stretch
                    const int seg = 40;
                    for (int k = 0; k < seg; k++)
                    {
                        float t0 = Mathf.PI * 2f * k / seg, t1 = Mathf.PI * 2f * (k + 1) / seg; var mid = g.Axis(g.At(sEnd));
                        Face(cyan, W(a0, t0, 0.02f), W(a1, t0, 0.02f), W(a1, t1, 0.02f), W(a0, t1, 0.02f), mid - W(sEnd, (t0 + t1) * 0.5f, 0f));
                        foreach (float edge in new[] { a0, a1 }) { float e0 = edge - 0.06f, e1 = edge + 0.06f; Face(white, W(e0, t0, 0.025f), W(e1, t0, 0.025f), W(e1, t1, 0.025f), W(e0, t1, 0.025f), mid - W(sEnd, (t0 + t1) * 0.5f, 0f)); }
                    }
                    // floor arrows (three chevrons) pointing into the stretch, from the band in
                    for (int c = 0; c < 3; c++)
                    {
                        float sc = sEnd + inward * (1.0f + c * 2.2f), L = 1.1f, Wd = 0.35f, hh = 0.55f / R;
                        Vector3 F(float s, float th) => W(s, th, 0.02f);
                        var ax = g.Axis(g.At(sc));
                        Face(cyan, F(sc - inward * L * 0.5f, hh), F(sc - inward * L * 0.5f + inward * Wd, hh), F(sc + inward * L * 0.5f + inward * Wd, 0f), F(sc + inward * L * 0.5f, 0f), ax - F(sc, 0f));
                        Face(cyan, F(sc + inward * L * 0.5f, 0f), F(sc + inward * L * 0.5f + inward * Wd, 0f), F(sc - inward * L * 0.5f + inward * Wd, -hh), F(sc - inward * L * 0.5f, -hh), ax - F(sc, 0f));
                    }
                    // the boards on both walls, beside the band at eye height, facing across the pipe, the arrow pointing into the stretch
                    if (netSigns)
                    {
                        int j = g.At(sEnd); var al = g.T(j) * inward; var sd = g.Side(j);
                        foreach (float side in new[] { -1f, 1f })
                        {
                            float th = side * 1.15f; var wallPt = W(sEnd - inward * 1.0f, th, 0.03f); var facing = (g.Axis(j) - wallPt); facing.y = 0f; facing.Normalize();
                            var right = Vector3.Cross(Vector3.up, facing).normalized * -1f;   // the viewer's right
                            bool arrowRight = Vector3.Dot(al, right) > 0f;
                            int row = Array.IndexOf(NetSignRows, arrowRight ? "EXPRESS >" : "< EXPRESS"); if (row < 0) continue;
                            float w = 2.6f, h = w * 0.1875f, v0 = 1f - (row + 1) / (float)rows, v1 = 1f - row / (float)rows;
                            var c0 = wallPt + Vector3.up * 0.2f; var a = c0 - right * (w * 0.5f) - Vector3.up * (h * 0.5f); var b = c0 + right * (w * 0.5f) - Vector3.up * (h * 0.5f);
                            boards.Quad(a, b, b + Vector3.up * h, a + Vector3.up * h, new Vector2(0f, v0), new Vector2(1f, v0), new Vector2(1f, v1), new Vector2(0f, v1), facing);
                        }
                    }
                }
            }
            var host = new GameObject("Pipe express").transform; host.SetParent(root, false);
            if (cyan.Count > 0) { var go = cyan.ToObject("Pipe express marks", netStripe(ExpressCyan), host, false, false); GameObjectUtility.SetStaticEditorFlags(go, 0); }
            if (white.Count > 0) { var go = white.ToObject("Pipe express mark edges", netStripe(new Color(0.95f, 0.95f, 0.93f)), host, false, false); GameObjectUtility.SetStaticEditorFlags(go, 0); }
            if (boards.T.Count > 0 && netSigns) boards.ToObject("Pipe express boards", netSigns, host, false);
            var px = host.gameObject.AddComponent<PipeExpress>(); px.stretches = stretches.ToArray();
            px.streak = ExpressStreakMaterial(); px.splash = pipeSplash;
            Debug.Log($"[Round26] pipe express: {stretches.Count} stretches ({string.Join("; ", stretches.Select(st => $"{st.id} {st.name} on {list.First(e => e.id == st.id).tube}, {list.First(e => e.id == st.id).s1 - list.First(e => e.id == st.id).s0:0} m at up to {(st.top > 0f ? st.top : PipeExpress.BoostSpeed):0} m/s"))})");
        }

        static Material ExpressStreakMaterial()
        {
            var path = GeneratedDir + "/ExpressStreak.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path); if (m) return m;
            m = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit")) { name = "ExpressStreak" };
            m.SetColor("_BaseColor", new Color(0.85f, 0.97f, 1f, 0.6f)); MakeTransparent(m);
            AssetDatabase.CreateAsset(m, path); return m;
        }
    }
}
