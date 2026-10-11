using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// Round 19, item 5 (Dan: "Second shortcut the entrance needs to be more obvious. I know I said hidden but I think especially in
// this neighborhood there is too many places a shortcut can be so a clear hint where it is would be good"): R3's turn-off dressed
// as a kid's birthday party, things that could be there, no game arrow or arch:
//   sign      a hand-painted PARTY yard sign with a big arrow toward the gap (Tools/handmade/make_party_sign.py), 3.2 x 1.6 m on
//             two posts, the face 1.0 to 2.6 m up (its capitals 0.67 m tall: readable flat out from 60 m), in the grass strip on
//             the side the line turns to, facing the oncoming reverse-lap driver (the turn-off sign spot, as #2's)
//   balloons  a bunch of seven tied to the sign's post, 3.4 to 4.5 m up; the sign and its balloons break like a mailbox
//             (FenceChunk: a trigger, a little speed lost, never solid; whole again at race start)
//   tracks    two worn tire ruts in the grass from the street's edge through the gap to the slide's foot
//   play set  bunting along the canopy's eaves and from its corners out to the trampoline's rim (wide of the flight), and a
//             bunch of balloons on each front corner of the canopy (look only: nothing there to drive into)
namespace ChastainRush
{
    public static partial class MapBuilder
    {
        static readonly Color[] BalloonColors = { new Color(0.86f, 0.12f, 0.14f), new Color(0.98f, 0.82f, 0.10f), new Color(0.14f, 0.40f, 0.90f), new Color(0.12f, 0.68f, 0.26f),
                                                  new Color(0.95f, 0.42f, 0.70f), new Color(0.98f, 0.52f, 0.08f), new Color(0.56f, 0.24f, 0.78f) };

        static void BuildPartySign(Neighborhood hood, TerrainGrid terrain, Materials mats, Transform parent, ScShortcut sc, (Vector2 at, Vector2 facing, float turn) s)
        {
            var f = MakeFrame(new Vector3(s.at.x, 0f, s.at.y), s.facing, 1f);
            float lo = float.MaxValue, hi = float.MinValue;
            foreach (float u in new[] { -1.5f, 1.5f }) { var q = f.P(u, 0f, -0.05f); float g = Ground(terrain, q.x, q.z); lo = Mathf.Min(lo, g); hi = Mathf.Max(hi, g); }
            f.O.y = hi;
            bool party = sc.sign_face == "party";
            var br = new Breakables();
            var mb = br.Begin(s.at);
            var post = new Color(0.40f, 0.31f, 0.22f); var edge = new Color(0.93f, 0.91f, 0.86f);
            foreach (float u in new[] { -1.45f, 1.45f }) br.Piece(() => BoxF(mb, f, u - 0.06f, u + 0.06f, lo - hi - 0.3f, 2.8f, -0.1f, -0.01f, post), lo + 0.05f);
            foreach (float u in new[] { -0.8f, 0.8f }) br.Piece(() => BoxF(mb, f, u - 0.8f, u + 0.8f, 1.0f, 2.6f, -0.01f, 0.03f, edge), lo + 0.05f);
            // the balloons, tied to the post on the road's side of the board, on strings
            float pu = -1.45f; var knot = f.P(pu, 2.75f, -0.05f);
            for (int i = 0; i < (party ? 7 : 0); i++)
            {
                float a = i * 0.9f, r = 0.33f + 0.04f * Hash01(i, 991);
                var c = knot + f.Dir(Mathf.Cos(a) * (0.35f + 0.15f * (i % 3)), 0.9f + 0.17f * i, Mathf.Sin(a) * 0.3f);
                int ci = i; br.Piece(() => { Balloon(mb, c, r, BalloonColors[ci % BalloonColors.Length]); Beam(mb, knot, c - Vector3.up * r, 0.015f, 0.015f, new Color(0.9f, 0.9f, 0.9f)); }, lo + 0.05f);
            }
            var mat = PartySignMaterial(s.turn > 0f, party ? "PartySign" : "JumpSign");
            if (mat != null)
            {
                var plate = SignPlate(parent, f.P(0f, 1.8f, 0.035f), -f.F, 1f, mat);
                plate.name = (party ? "Party" : "Jump") + " sign face " + sc.id;
                plate.transform.localScale = new Vector3(3.2f, 1.6f, 1f);
                br.Face(plate);
            }
            br.End(f.P(0f, 2.3f, -0.03f), f.F, 3.6f, 4.8f, 0.9f);
            br.ToObjects((party ? "Party" : "Jump") + " sign " + sc.id, mats.House, parent, 1f, BreakSounds.Kind.Wood);
            Debug.Log($"[Round19] {sc.id}: the {(party ? "PARTY" : "JUMP!")} sign at ({s.at.x:0.0}, {s.at.y:0.0}), facing ({s.facing.x:0.00}, {s.facing.y:0.00}), its arrow {(s.turn > 0f ? "left" : "right")}{(party ? ", seven balloons" : "")}");

            // worn tire tracks in the grass: from where the line leaves the street to the slide's foot
            var p = ToPoints(sc.path); int i0 = p.FindIndex(q => hood.RoadClearance(q) > 0.3f);
            var foot = sc.playground?.slide?.o != null && sc.playground.slide.o.Length >= 2 ? new Vector2(sc.playground.slide.o[0], sc.playground.slide.o[1]) : sc.prisms.Length > 0 ? new Vector2(sc.prisms[0].origin[0], sc.prisms[0].origin[1]) : p[p.Count - 1];   // R3: the slide's foot; R1: the ramp's foot
            int i1 = Enumerable.Range(0, p.Count).OrderBy(k => (p[k] - foot).sqrMagnitude).First();
            if (i0 < 0 || i1 <= i0 + 2) return;
            var ruts = new MeshBuilder(); var worn = new Color(0.43f, 0.35f, 0.23f);
            var line = Densify(p.GetRange(i0, i1 - i0 + 1), 0.5f);
            foreach (float side in new[] { -0.8f, 0.8f })
            {
                Vector3? pa = null, pb = null;
                for (int k = 0; k < line.Count; k++)
                {
                    var t = (line[Mathf.Min(k + 1, line.Count - 1)] - line[Mathf.Max(k - 1, 0)]).normalized; var n = new Vector2(-t.y, t.x);
                    float w = 0.2f + 0.05f * Mathf.Sin(k * 0.9f + side), fade = Mathf.Clamp01(k / 6f);   // fading in from the street
                    var c = line[k] + n * side; var a = c + n * w; var b = c - n * w;
                    var A = new Vector3(a.x, Ground(terrain, a.x, a.y) + 0.025f, a.y); var B = new Vector3(b.x, Ground(terrain, b.x, b.y) + 0.025f, b.y);
                    if (pa.HasValue) { ruts.Tint = Shade(worn, Mathf.Lerp(1.25f, 0.92f + 0.12f * Hash01(k, 993), fade)); Face(ruts, pa.Value, A, B, pb.Value, Vector3.up); }
                    pa = A; pb = B;
                }
            }
            ruts.ToObject("Worn tracks " + sc.id, mats.Driveway, parent, false, false);
        }

        static Material PartySignMaterial(bool left, string name)
        {
            string path = "Assets/HandMade/Shortcut/" + name + (left ? "Left.png" : "Right.png");
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (tex == null) { Debug.LogWarning("[Round19] " + path + " missing (Tools/handmade/make_party_sign.py): no party sign face"); return null; }
            var m = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name };
            m.SetTexture("_BaseMap", tex); m.mainTexture = tex; m.SetColor("_BaseColor", Color.white); m.SetFloat("_Smoothness", 0.1f); m.SetFloat("_Cull", (float)CullMode.Back);
            AssetDatabase.CreateAsset(m, GeneratedDir + "/" + name + ".mat");
            return m;
        }

        // a balloon: a low sphere a little taller than wide, and its knot
        static void Balloon(MeshBuilder mb, Vector3 c, float r, Color col)
        {
            mb.Tint = col;
            const int rings = 5, seg = 8;
            var v = new Vector3[rings + 1, seg];
            for (int i = 0; i <= rings; i++)
            {
                float ph = Mathf.PI * i / rings; float y = -Mathf.Cos(ph) * r * 1.15f, rr = Mathf.Sin(ph) * r * (i < rings / 2 ? 0.9f : 1f);
                for (int k = 0; k < seg; k++) { float th = Mathf.PI * 2f * k / seg; v[i, k] = c + new Vector3(Mathf.Cos(th) * rr, y, Mathf.Sin(th) * rr); }
            }
            for (int i = 0; i < rings; i++)
                for (int k = 0; k < seg; k++)
                {
                    int k1 = (k + 1) % seg; var mid = (v[i, k] + v[i + 1, k1]) * 0.5f;
                    Face(mb, v[i, k], v[i + 1, k], v[i + 1, k1], v[i, k1], mid - c);
                }
            mb.Tint = Shade(col, 0.7f);
            var kb = c - Vector3.up * r * 1.15f;
            Beam(mb, kb, kb - Vector3.up * 0.07f, 0.06f, 0.06f, Shade(col, 0.7f));
        }

        // the play set dressed for the party: bunting and balloons (look only)
        static void BuildPartyDressing(ScShortcut sc, TerrainGrid terrain, Materials mats, Transform parent)
        {
            var tw = sc.playground?.tower; var tr = sc.playground?.trampoline; if (tw == null || tr == null) return;
            var u2 = new Vector2(tw.u[0], tw.u[1]).normalized; var n2 = new Vector2(-u2.y, u2.x); var D0 = new Vector2(tw.o[0], tw.o[1]);
            Vector3 W(Vector2 q, float y) => new Vector3(q.x, y, q.y);
            var mb = new MeshBuilder(); int flags = 0;
            float hw = tw.half + 0.25f + 0.45f;
            void Bunting(Vector3 a, Vector3 b, float sag)
            {
                float len = Vector3.Distance(a, b); int n = Mathf.Max(2, Mathf.RoundToInt(len / 0.45f));
                Vector3 P(float t) => Vector3.Lerp(a, b, t) - Vector3.up * sag * 4f * t * (1f - t);
                for (int i = 0; i < n; i++)
                {
                    Vector3 p0 = P(i / (float)n), p1 = P((i + 1) / (float)n);
                    Beam(mb, p0, p1, 0.02f, 0.02f, new Color(0.92f, 0.92f, 0.9f));
                    var tip = Vector3.Lerp(p0, p1, 0.5f) - Vector3.up * 0.38f;
                    var side = Vector3.Cross(p1 - p0, Vector3.up).normalized;
                    mb.Tint = BalloonColors[(i + flags) % BalloonColors.Length];
                    TriFace(mb, p0, p1, tip, side); TriFace(mb, p0, p1, tip, -side);
                }
                flags += n;
            }
            foreach (float side in new[] { 1f, -1f })
            {
                var e0 = W(D0 - u2 * 0.45f + n2 * side * hw, tw.roof_y); var e1 = W(D0 + u2 * (tw.len + 0.45f) + n2 * side * hw, tw.roof_y);
                Bunting(e0, e1, 0.2f);
                // out to the trampoline's rim, wide of the flight (which runs down the middle)
                var T = new Vector2(tr.x, tr.z); var rim = T + n2 * side * tr.r * 0.92f;
                Bunting(e1, W(rim, tr.mat_y + 0.9f), 0.9f);
                // a bunch of balloons on the canopy's front corner
                for (int i = 0; i < 5; i++)
                {
                    float a = i * 1.3f; var knot = e1;
                    var c = knot + new Vector3(Mathf.Cos(a) * 0.35f, 0.7f + 0.2f * i, Mathf.Sin(a) * 0.35f);
                    Balloon(mb, c, 0.34f, BalloonColors[(i + (side > 0 ? 0 : 3)) % BalloonColors.Length]);
                    Beam(mb, knot, c - Vector3.up * 0.39f, 0.015f, 0.015f, new Color(0.9f, 0.9f, 0.9f));
                }
            }
            var go = mb.ToObject("Party dressing " + sc.id, mats.House, parent, false);
            go.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            Debug.Log($"[Round19] {sc.id}: the play set dressed for the party: {flags} bunting flags, ten balloons on the canopy");
        }
    
        // Round 19 (6): R1's ramp drawn as the neighborhood kids' giant homemade ramp: the plywood top is the shortcut's prism (an
        // open board, BuildPrism); under it a frame of posts and cross braces (look only), cinder blocks along its foot
        static void BuildKidsRamp(ScShortcut sc, TerrainGrid terrain, Materials mats, Transform parent)
        {
            if (sc.prisms == null || sc.prisms.Length == 0) return;
            var pr = sc.prisms[0]; var O = new Vector2(pr.origin[0], pr.origin[1]); var U = new Vector2(pr.axis[0], pr.axis[1]).normalized; var N = new Vector2(-U.y, U.x);
            float len = pr.profile[pr.profile.Length - 2];
            var mb = new MeshBuilder(); Color lumber = new Color(0.66f, 0.54f, 0.36f), block = new Color(0.55f, 0.55f, 0.53f);
            Vector3 At(float s, float lat, float dy) { var q = O + U * s + N * lat; return new Vector3(q.x, Profile(pr.profile, s) + dy, q.y); }
            float G(Vector3 v) => Ground(terrain, v.x, v.z);
            int posts = 0;
            foreach (float lat in new[] { -2.85f, 0f, 2.85f })
            {
                Vector3? prev = null;
                for (float s = 1.5f; s <= len - 0.2f; s += 1.5f)
                {
                    var top = At(Mathf.Min(s, len - 0.15f), lat, -0.27f); float g = G(top) - 0.1f;
                    if (top.y - g < 0.25f) continue;
                    Post(mb, new Vector3(top.x, g, top.z), top.y - g, 0.1f, lumber); posts++;
                    if (prev.HasValue && lat != 0f) Beam(mb, new Vector3(prev.Value.x, G(prev.Value) + 0.1f, prev.Value.z), top, 0.05f, 0.1f, Shade(lumber, 0.9f));   // a brace on the side
                    prev = top;
                }
            }
            // stringers under the board's edges and middle, cross members every 1.5 m
            foreach (float lat in new[] { -2.85f, 0f, 2.85f }) for (float s = 0.3f; s + 0.7f <= len; s += 0.7f) Beam(mb, At(s, lat, -0.33f), At(s + 0.7f, lat, -0.33f), 0.06f, 0.14f, Shade(lumber, 0.85f));
            for (float s = 1.5f; s <= len - 0.2f; s += 1.5f) Beam(mb, At(s, -2.9f, -0.33f), At(s, 2.9f, -0.33f), 0.05f, 0.12f, Shade(lumber, 0.8f));
            // cinder blocks along the foot
            for (float lat = -2.6f; lat <= 2.61f; lat += 1.3f)
            {
                var q = O - U * 0.45f + N * lat; var c = new Vector3(q.x, Ground(terrain, q.x, q.y), q.y);
                var f = new Frame { O = c, F = new Vector3(U.x, 0f, U.y), R = new Vector3(-N.x, 0f, -N.y), M = 1f };
                BoxF(mb, f, -0.2f, 0.2f, -0.05f, 0.2f, -0.1f, 0.1f, Shade(block, 0.9f + 0.15f * Hash01(Mathf.RoundToInt(lat * 10f), 997)));
            }
            mb.ToObject("R1 kids ramp frame", mats.House, parent, false);
            Debug.Log($"[Round19] {sc.id}: the kids' ramp's frame: {posts} posts, braces, cinder blocks along its {len:0.0} m");
        }
    }
}
