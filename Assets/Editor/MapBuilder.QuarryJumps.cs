using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

// Round 72 (Dan, 2026-10-26 07:17: "Quarry stunt jumps"; 2026-10-25 02:11: "stunt races that have large ramps to jump you over evel knievel style"):
// the Viking Quarry jumps, world pieces from Data/tracks/quarry_jumps.json (Tools/mapgen/make_quarry_jumps.py: the lines, the kickers and the landing
// decks designed from the flights; the why of the places is there). Each jump: a start gantry over its run-up's head (bunting, three countdown lamps
// the event lights: Stunts/QuarryJumps.cs), a steel kicker on the quarry's south rim (a held deck: a real jump, nothing aims the flight), a steel
// landing deck across the lake sloped a little shallower than the flights come down (a catch zone: RampHold sets a vehicle on its wheels), its foot on
// graded ground, and bleachers with a crowd beside the landing and the kicker. The run-out grades, the cleared lines and the catch zones are merged into
// the mountain's planning (as the summit ramp's, MapBuilder.SummitRamp.cs), so they are graded, cleared and caught with it; built in the MountainRace step.
namespace ChastainRush
{
    public static partial class MapBuilder
    {
        [Serializable] class QjSpot { public float[] at; public float yaw, y; }
        [Serializable] class QjJump { public string id, name; public float[] origin, dir, deck_s, deck_y, land_s, land_y, start; public float half, lip_s, lip_y, land_half, runout_to, start_yaw; public QjSpot gantry; public QjSpot[] stands; }
        [Serializable] class QjSign { public float[] at; public float facing; public string text, jump; }
        [Serializable] class QjAccess { public float[] drive; public float half; public QjSign[] signs, boards; }
        [Serializable] class QjFile { public RidgeGrade[] grades; public RidgeClear[] clear_lines; public CtZone[] landing_zones; public QjJump[] jumps; public QjAccess access; }
        const string QuarryJumpsFile = "Data/tracks/quarry_jumps.json";
        [Serializable] class QtFile { public RidgeGrade[] grades; public RidgeClear[] clear_lines; public float[] divider; public string route_name; }
        const string QuarryTrackFile = "Data/tracks/quarry_track.json";
        static QtFile QuarryTrackNow;
        static QjFile QuarryJumpsNow; static string QuarryJumpsJson;

        static void MergeQuarryJumps(Ridge mountain)
        {
            QuarryJumpsNow = null; QuarryJumpsJson = null;
            string path = Path.Combine(ProjectRoot, QuarryJumpsFile);
            if (mountain == null || !File.Exists(path)) { Debug.Log("[Round72] no " + QuarryJumpsFile + " (or no mountain pieces): no quarry jumps"); return; }
            QuarryJumpsJson = File.ReadAllText(path); var f = QuarryJumpsNow = JsonUtility.FromJson<QjFile>(QuarryJumpsJson); var d = mountain.Data;
            T[] Cat<T>(T[] a, T[] b) => (a ?? new T[0]).Concat(b ?? new T[0]).ToArray();
            d.grades = Cat(d.grades, f.grades); d.clear_lines = Cat(d.clear_lines, f.clear_lines); d.landing_zones = Cat(d.landing_zones, f.landing_zones);
            // Round 73 (C5): the Quarry Track's benches (the two-lane haul road, its hairpins and connectors) and cleared line (Tools/mapgen/make_quarry_track.py)
            QuarryTrackNow = null; string tp = Path.Combine(ProjectRoot, QuarryTrackFile);
            if (File.Exists(tp))
            {
                QuarryTrackNow = JsonUtility.FromJson<QtFile>(File.ReadAllText(tp));
                d.grades = Cat(d.grades, QuarryTrackNow.grades); d.clear_lines = Cat(d.clear_lines, QuarryTrackNow.clear_lines);
                Debug.Log($"[Round73] the Quarry Track: {QuarryTrackNow.grades?.Length ?? 0} benches and its cleared line merged into the mountain's");
            }
            Debug.Log($"[Round72] the quarry jumps: {f.jumps?.Length ?? 0} jumps, {f.grades?.Length ?? 0} grades, {f.clear_lines?.Length ?? 0} cleared lines, {f.landing_zones?.Length ?? 0} catch zones merged into the mountain's");
        }

        static readonly Color[] Bunting = { new Color(.78f, .1f, .12f), new Color(.95f, .95f, .93f), new Color(.1f, .2f, .6f) };

        static void BuildQuarryJumps(TerrainGrid terrain, Materials mats, Transform root)
        {
            var f = QuarryJumpsNow; if (f == null || f.jumps == null || f.jumps.Length == 0) return;
            var go = new GameObject("The Viking Quarry jumps (Round 72)").transform; go.SetParent(root, false);
            var lamps = new List<Transform>(); int people = 0;
            foreach (var j in f.jumps)
            {
                var o = new Vector2(j.origin[0], j.origin[1]); var d = new Vector2(j.dir[0], j.dir[1]).normalized; var nl = new Vector2(-d.y, d.x);
                var jt = new GameObject("Quarry jump " + j.id + " " + j.name).transform; jt.SetParent(go, false);
                int bk = SteelRamp(terrain, mats, jt, $"sc quarry {j.id} kicker deck", $"Quarry {j.id} kicker steel", o, d, j.deck_s, j.deck_y, j.half, true);
                // Round 73 (C3 (1), Dan's BUG-006: stuck in under a ramp's trestle): the kicker skirted solid to the ground, sides and lip end
                Skirt(terrain, mats, jt, $"Quarry {j.id} kicker skirt", o, d, j.deck_s, j.deck_y, j.half);
                // Round 73 (C4): the landing deck lies on its graded cut in the far bench (its top at the bench's edge): nothing under it
                int bl = SteelRamp(terrain, mats, jt, $"sc quarry {j.id} landing deck", $"Quarry {j.id} landing steel", o, d, j.land_s, j.land_y, j.land_half, false);
                LandingBanner(terrain, mats, jt, j, o, d, nl);
                StartBox(terrain, mats, jt, j, lamps);
                // ---- the start gantry: two steel towers either side of the run-up, a beam with the bunting over it, the countdown lamps on the right tower
                var mb = new MeshBuilder();
                var g = new Vector2(j.gantry.at[0], j.gantry.at[1]); float gw = j.half + 3f, gh = 8f;
                Vector3 G(float lat, float y, float along = 0f) { var q = g + nl * lat + d * along; return new Vector3(q.x, Ground(terrain, q.x, q.y) + y, q.y); }
                var gantryCols = new GameObject("Gantry towers").transform; gantryCols.SetParent(jt, false);
                foreach (float side in new[] { -1f, 1f })
                {
                    var foot = G(side * gw, -0.5f);
                    BoxF(mb, MakeFrame(foot, d, 1f), -.7f, .7f, 0f, gh + .5f, -.7f, .7f, RampSteel);
                    var bc = new GameObject("tower").AddComponent<BoxCollider>(); bc.transform.SetParent(gantryCols, false);
                    bc.transform.SetPositionAndRotation(foot + Vector3.up * (gh / 2f), Quaternion.LookRotation(new Vector3(d.x, 0f, d.y))); bc.size = new Vector3(1.4f, gh, 1.4f);
                }
                Beam(mb, G(-gw, gh), G(gw, gh), .6f, .9f, RampRed);
                // the bunting: two sagging rows of red, white and blue pennants under the beam
                for (int row = 0; row < 2; row++)
                {
                    int nP = 18; float y0 = gh - .6f - row * 1.4f;
                    for (int k = 0; k < nP; k++)
                    {
                        float u0 = -gw + 2f * gw * k / nP, u1 = -gw + 2f * gw * (k + 1) / nP, sag = .9f * Mathf.Sin(Mathf.PI * (k + .5f) / nP);
                        var a = G(u0, y0 - sag); var b = G(u1, y0 - sag); var c = (a + b) * .5f - Vector3.up * 1.1f;
                        mb.Tint = Bunting[(k + row) % 3]; int v0 = mb.Vertex(a), v1 = mb.Vertex(b), v2 = mb.Vertex(c); mb.Tri(v0, v1, v2); mb.Tri(v0, v2, v1);
                    }
                }
                // a star-spangled panel on each tower's face (stars on blue, stripes under)
                foreach (float side in new[] { -1f, 1f })
                {
                    var fr = MakeFrame(G(side * gw, 2.5f, -.8f), -d, 1f);
                    for (int st = 0; st < 7; st++) BoxF(mb, fr, -.65f, .65f, st * .35f, st * .35f + .35f, 0f, .05f, st % 2 == 0 ? Bunting[0] : Bunting[1]);
                    BoxF(mb, fr, -.65f, .65f, 2.45f, 3.4f, 0f, .05f, Bunting[2]);
                    for (int sx = 0; sx < 3; sx++) for (int sy = 0; sy < 2; sy++) BoxF(mb, fr, -.5f + sx * .4f, -.38f + sx * .4f, 2.65f + sy * .4f, 2.77f + sy * .4f, .05f, .08f, Bunting[1]);
                }
                var steel = mb.ToObject($"Quarry {j.id} gantry", mats.House, jt, false); steel.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.On;
                // ---- bleachers (Round 73 C1 / C2, Dan's BUG-004 "Are these supposed to be people? Why are they just blocks?" and BUG-005 "People are
                // literally buried"): on their own level pads (Tools/mapgen/make_quarry_jumps.py grades them), five rows of seats stepping up with a
                // foot board in front of each, and the crowd seated on them: the game's people (AmbientLife's resident: torso, round head and hair,
                // legs, arms, shoes, the same shirts and skins), sitting, some cheering with their arms up, an empty seat here and there
                int sn = 0;
                foreach (var stand in j.stands ?? new QjSpot[0])
                {
                    var c = new Vector2(stand.at[0], stand.at[1]);
                    float lat = Vector2.Dot(c - o, nl); var face = -nl * Mathf.Sign(lat);
                    float gy = stand.y != 0f ? stand.y : Ground(terrain, c.x, c.y);
                    var fr = MakeFrame(new Vector3(c.x, gy, c.y), face, 1f);
                    var bm = new MeshBuilder();
                    for (int row = 0; row < 5; row++)
                    {
                        float v = 2.2f - row * 1.1f, y = .45f + row * .6f;
                        BoxF(bm, fr, -11f, 11f, y - .12f, y, v - .5f, v + .1f, new Color(.62f, .6f, .56f));            // the seat plank
                        BoxF(bm, fr, -11f, 11f, y - .62f, y - .5f, v + .15f, v + .62f, new Color(.55f, .54f, .5f));   // the foot board in front of it
                        BoxF(bm, fr, -11f, 11f, -.3f, y - .12f, v - .55f, v - .45f, new Color(.4f, .41f, .43f));      // the riser, from below the pad
                        for (int seat = 0; seat < 20; seat++)
                        {
                            int hsh = (int)(c.x * 7 + c.y * 13) + row * 31 + seat * 7;
                            if (Hash01(hsh, 1) < .16f) continue;   // an empty seat here and there
                            float u = -10.4f + seat * 1.1f + (Hash01(hsh, 2) - .5f) * .2f;
                            SeatedPerson(bm, fr, u, y, v - .2f, hsh); people++;
                        }
                    }
                    for (int k = -11; k <= 11; k += 2) Beam(bm, fr.P(k, -.3f, -2.7f), fr.P(k, 4.3f, -2.7f), .1f, .1f, RampSteel);
                    Beam(bm, fr.P(-11f, 4.3f, -2.7f), fr.P(11f, 4.3f, -2.7f), .1f, .1f, RampSteel);
                    var bobj = bm.ToObject($"Quarry {j.id} bleachers {sn}", mats.House, jt, false);
                    var center = fr.P(0f, 1.6f, 0f);   // solid: a box over the stand (a child turned to it: the mesh's object stands at the origin)
                    var cc = new GameObject("bleachers solid").AddComponent<BoxCollider>(); cc.transform.SetParent(bobj.transform, false);
                    cc.transform.SetPositionAndRotation(center, Quaternion.LookRotation(new Vector3(face.x, 0f, face.y))); cc.size = new Vector3(22f, 3.2f, 6f);
                    sn++;
                }
                Debug.Log($"[Round72] quarry jump {j.id} {j.name}: kicker {j.deck_s[j.deck_s.Length - 1] - j.deck_s[0]:0} m to its lip at {j.lip_y:0.0} m ({bk} bents), landing deck {j.land_s[j.land_s.Length - 1] - j.land_s[0]:0} m ({bl} bents), {sn} bleachers");
            }
            QuarryAccess(terrain, mats, go, f.access);
            QuarryTrackDivider(terrain, mats, go);
            // the event (game layer): the jumps' data and the countdown lamps
            var ev = new GameObject("Quarry jumps event").AddComponent<QuarryJumps>(); ev.transform.SetParent(go, false);
            ev.json = QuarryJumpsJson; ev.lights = lamps.ToArray();
            Debug.Log($"[Round72] the Viking Quarry jumps: {f.jumps.Length} jumps, {people} people in the bleachers");
        }

        // Round 73 (C1): one of the game's people (AmbientLife's resident, the same shapes and colors) seated at (u, seatY, v) in a stand's frame,
        // facing its front (+v): torso, head and hair (or a cap), thighs on the seat, shins down to the foot board, shoes, arms on the knees or up cheering
        static readonly Color[] CrowdShirts = { new Color(.5f, .18f, .12f), new Color(.17f, .32f, .47f), new Color(.56f, .49f, .19f), new Color(.28f, .39f, .24f), new Color(.85f, .85f, .82f), new Color(.45f, .25f, .50f), new Color(.75f, .12f, .12f), new Color(.12f, .2f, .55f) };
        static readonly Color[] CrowdSkins = { new Color(.62f, .40f, .26f), new Color(.85f, .66f, .52f), new Color(.42f, .27f, .17f), new Color(.74f, .53f, .38f) };
        static readonly Color[] CrowdHair = { new Color(.12f, .08f, .05f), new Color(.35f, .22f, .1f), new Color(.6f, .5f, .3f), new Color(.5f, .5f, .5f), new Color(.08f, .08f, .08f) };
        static void SeatedPerson(MeshBuilder mb, Frame fr, float u, float y, float v, int hsh)
        {
            var shirt = CrowdShirts[(int)(Hash01(hsh, 3) * CrowdShirts.Length) % CrowdShirts.Length]; var skin = CrowdSkins[(int)(Hash01(hsh, 4) * CrowdSkins.Length) % CrowdSkins.Length];
            var hair = CrowdHair[(int)(Hash01(hsh, 5) * CrowdHair.Length) % CrowdHair.Length]; var legs = Hash01(hsh, 6) < .6f ? new Color(.16f, .20f, .26f) : new Color(.45f, .38f, .28f);
            float sc = .92f + .16f * Hash01(hsh, 7);
            Vector3 P(float du, float dy, float dv) => fr.P(u + du * sc, y + dy * sc, v + dv * sc);
            Ellipsoid(mb, P(0f, .45f, -.04f), new Vector3(.21f, .30f, .14f) * sc, fr, shirt);                 // the torso
            Ellipsoid(mb, P(0f, .93f, -.02f), new Vector3(.14f, .16f, .145f) * sc, fr, skin);                  // the head
            if (Hash01(hsh, 8) < .2f) Ellipsoid(mb, P(0f, 1.05f, -.03f), new Vector3(.17f, .06f, .18f) * sc, fr, CrowdShirts[hsh & 7]);   // a cap
            else Ellipsoid(mb, P(0f, 1.0f, -.035f), new Vector3(.148f, .085f, .148f) * sc, fr, hair);         // the hair
            foreach (float side in new[] { -1f, 1f })
            {
                Limb(mb, P(side * .1f, .1f, 0f), P(side * .11f, .1f, .42f), .085f * sc, legs);                 // the thigh, on the seat
                Limb(mb, P(side * .11f, .1f, .42f), P(side * .11f, -.38f, .48f), .07f * sc, legs);             // the shin
                BoxF(mb, MakeFrame(P(side * .11f, -.44f, .52f), new Vector2(fr.F.x, fr.F.z), 1f), -.06f, .06f, 0f, .1f, -.06f, .14f, new Color(.1f, .08f, .06f));   // the shoe
                bool up = Hash01(hsh, side > 0 ? 9 : 10) < .14f;   // cheering
                var sh = P(side * .25f, .7f, -.04f);
                if (up) { var e = P(side * .33f, 1.0f, .02f); var hnd = P(side * .36f, 1.28f, .06f); Limb(mb, sh, e, .065f * sc, shirt); Limb(mb, e, hnd, .055f * sc, skin); Ellipsoid(mb, hnd, Vector3.one * .065f * sc, fr, skin); }
                else { var e = P(side * .27f, .38f, .1f); var hnd = P(side * .18f, .26f, .34f); Limb(mb, sh, e, .065f * sc, shirt); Limb(mb, e, hnd, .055f * sc, skin); }
            }
        }
        static void Ellipsoid(MeshBuilder mb, Vector3 c, Vector3 r, Frame fr, Color col)
        {
            mb.Tint = col; const int rings = 4, seg = 6; var v = new Vector3[rings + 1, seg];
            var R = fr.R.normalized; var F = fr.F.normalized;
            for (int i = 0; i <= rings; i++)
            {
                float ph = Mathf.PI * i / rings, yy = -Mathf.Cos(ph), rr = Mathf.Sin(ph);
                for (int k = 0; k < seg; k++) { float th = Mathf.PI * 2f * k / seg; v[i, k] = c + R * (Mathf.Cos(th) * rr * r.x) + Vector3.up * (yy * r.y) + F * (Mathf.Sin(th) * rr * r.z); }
            }
            for (int i = 0; i < rings; i++) for (int k = 0; k < seg; k++) { int k1 = (k + 1) % seg; var mid = (v[i, k] + v[i + 1, k1]) * .5f; Face(mb, v[i, k], v[i + 1, k], v[i + 1, k1], v[i, k1], mid - c); }
        }
        static void Limb(MeshBuilder mb, Vector3 a, Vector3 b, float r, Color c)
        {
            mb.Tint = c; var ax = (b - a).normalized; var u = Vector3.Cross(ax, Mathf.Abs(ax.y) > .9f ? Vector3.right : Vector3.up).normalized; var w = Vector3.Cross(ax, u); const int seg = 5;
            for (int i = 0; i < seg; i++)
            {
                float t0 = i * Mathf.PI * 2f / seg, t1 = (i + 1) * Mathf.PI * 2f / seg;
                var o0 = u * Mathf.Cos(t0) * r + w * Mathf.Sin(t0) * r; var o1 = u * Mathf.Cos(t1) * r + w * Mathf.Sin(t1) * r;
                Face(mb, a + o0, b + o0, b + o1, a + o1, (o0 + o1).normalized);
            }
        }

        // Round 73 (C3 (1)): solid steel skirts from a ramp deck's edges down into the ground, and its lip end down to the ground under it, with their
        // collider: nothing can be driven in under a ramp or wedged in its trestle
        static void Skirt(TerrainGrid terrain, Materials mats, Transform parent, string name, Vector2 o, Vector2 d, float[] deck_s, float[] deck_y, float half)
        {
            var n = new Vector2(-d.y, d.x); var mb = new MeshBuilder(); mb.Tint = Shade(RampSteel, .9f);
            Vector3 W(float s, float lat, float y) { var q = o + d * s + n * lat; return new Vector3(q.x, y, q.y); }
            float G(float s, float lat) { var q = o + d * s + n * lat; return Ground(terrain, q.x, q.y) - .6f; }
            var n3 = new Vector3(n.x, 0f, n.y); var d3 = new Vector3(d.x, 0f, d.y);
            int m = deck_s.Length;
            for (int i = 0; i + 1 < m; i++)
                foreach (float side in new[] { -1f, 1f })
                {
                    float lat = side * (half + .02f);
                    var a = W(deck_s[i], lat, deck_y[i] - .5f); var b = W(deck_s[i + 1], lat, deck_y[i + 1] - .5f);
                    var a0 = W(deck_s[i], lat, Mathf.Min(G(deck_s[i], lat), deck_y[i] - .5f)); var b0 = W(deck_s[i + 1], lat, Mathf.Min(G(deck_s[i + 1], lat), deck_y[i + 1] - .5f));
                    Face(mb, a0, b0, b, a, n3 * side); Face(mb, a, b, b0, a0, -n3 * side);
                }
            {
                int e = m - 1; var a = W(deck_s[e], -half, deck_y[e] - 1.2f); var b = W(deck_s[e], half, deck_y[e] - 1.2f);
                var a0 = W(deck_s[e], -half, G(deck_s[e], -half)); var b0 = W(deck_s[e], half, G(deck_s[e], half));
                Face(mb, a0, b0, b, a, d3); Face(mb, a, b, b0, a0, -d3);
            }
            var go = mb.ToObject(name, mats.House, parent, false);
            var mc = go.AddComponent<MeshCollider>(); mc.sharedMesh = go.GetComponent<MeshFilter>().sharedMesh;
        }

        // Round 73 (C4: "the landing deck in plain view across the water"): two tall steel posts at the landing deck's front edge with a stars-and-
        // stripes banner between them over the deck, its face toward the kicker
        static void LandingBanner(TerrainGrid terrain, Materials mats, Transform parent, QjJump j, Vector2 o, Vector2 d, Vector2 nl)
        {
            var mb = new MeshBuilder(); float s0 = j.land_s[0] + 1f, w = j.land_half + 1.5f, top = j.land_y[0] + 11f;
            var col = new GameObject("Landing banner posts").transform; col.SetParent(parent, false);
            foreach (float side in new[] { -1f, 1f })
            {
                var q = o + d * s0 + nl * side * w; float gy = Ground(terrain, q.x, q.y);
                BoxF(mb, MakeFrame(new Vector3(q.x, gy - .5f, q.y), d, 1f), -.5f, .5f, 0f, top - gy + .8f, -.5f, .5f, RampRed);
                var bc = new GameObject("post").AddComponent<BoxCollider>(); bc.transform.SetParent(col, false);
                bc.transform.SetPositionAndRotation(new Vector3(q.x, (gy + top) * .5f, q.y), Quaternion.LookRotation(new Vector3(d.x, 0f, d.y))); bc.size = new Vector3(1f, top - gy + 1f, 1f);
            }
            var c = o + d * s0; var fr = MakeFrame(new Vector3(c.x, top - 3.2f, c.y), -d, 1f);   // its face toward the kicker
            BoxF(mb, fr, -w, w, 1.6f, 3.2f, 0f, .2f, Bunting[2]);
            for (int st = 0; st < 4; st++) BoxF(mb, fr, -w, w, st * .4f, st * .4f + .4f, 0f, .2f, st % 2 == 0 ? Bunting[0] : Bunting[1]);
            for (int k = 0; k < 9; k++) { float uc = -w + 1.2f + k * (2f * w - 2.4f) / 8f; BoxF(mb, fr, uc - .25f, uc + .25f, 2.15f, 2.65f, .2f, .26f, Bunting[1]); }
            var g = mb.ToObject($"Quarry {j.id} landing banner", mats.House, parent, false); g.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.On;
        }

        // Round 73 (C3 (3): "the start marked: drive into a lit start box to begin"): a white-lined box on the ground at the start with a red-and-white
        // line across its front, a lamp post at each corner (the three countdown lamps on the front right one, lit by the event)
        static void StartBox(TerrainGrid terrain, Materials mats, Transform parent, QjJump j, List<Transform> lamps)
        {
            var o = new Vector2(j.start[0], j.start[1]); var d = new Vector2(j.dir[0], j.dir[1]).normalized; var nl = new Vector2(-d.y, d.x);
            var mb = new MeshBuilder(); const float hw = 4.5f, hl = 6f;
            Vector3 G(float lat, float along, float up) { var q = o + nl * lat + d * along; return new Vector3(q.x, Ground(terrain, q.x, q.y) + up, q.y); }
            void Strip(float l0, float a0, float l1, float a1, Color c)
            {
                mb.Tint = c; var p0 = G(l0, a0, .08f); var p1 = G(l1, a0, .08f); var p2 = G(l1, a1, .08f); var p3 = G(l0, a1, .08f); Face(mb, p0, p1, p2, p3, Vector3.up);
            }
            var white = new Color(.95f, .95f, .92f);
            Strip(-hw, -hl, -hw + .4f, hl, white); Strip(hw - .4f, -hl, hw, hl, white); Strip(-hw, -hl, hw, -hl + .4f, white);
            for (int k = 0; k < 9; k++) Strip(-hw + k * 2f * hw / 9f, hl - .6f, -hw + (k + 1) * 2f * hw / 9f, hl, k % 2 == 0 ? Bunting[0] : white);
            foreach (var (lat, al) in new[] { (-hw - .8f, -hl), (hw + .8f, -hl), (-hw - .8f, hl), (hw + .8f, hl) })
            {
                var b = G(lat, al, -.3f); Post(mb, b, 4.3f, .18f, RampSteel);
                Post(mb, b + Vector3.up * 4.2f, .35f, .5f, new Color(1f, .95f, .7f));   // the lamp head (bright)
            }
            for (int k = 0; k < 3; k++)
            {
                var lamp = GameObject.CreatePrimitive(PrimitiveType.Sphere); UnityEngine.Object.DestroyImmediate(lamp.GetComponent<Collider>());
                lamp.name = $"Countdown lamp {k}"; lamp.transform.SetParent(parent, false); lamp.transform.position = G(hw + .8f, hl + .1f, 3.4f - k * .9f); lamp.transform.localScale = Vector3.one * .7f;
                lamp.GetComponent<Renderer>().sharedMaterial = mats.House; lamps.Add(lamp.transform);
            }
            var go = mb.ToObject($"Quarry {j.id} start box", mats.House, parent, false); go.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
        }

        // Round 73 (C3 (3)): the quarry jumps drive (a gravel track behind the three start boxes, between the airport's service roads), the QUARRY
        // JUMPS boards at its turn-offs and each start box's board
        static void QuarryAccess(TerrainGrid terrain, Materials mats, Transform parent, QjAccess a)
        {
            if (a == null || a.drive == null || a.drive.Length < 4) return;
            var t = new GameObject("Quarry jumps drive and signs").transform; t.SetParent(parent, false);
            var pts = ToPoints(a.drive); var gm = new MeshBuilder(); gm.Tint = Gravel;
            RibbonGrid(gm, pts, Enumerable.Repeat(a.half * 2f, pts.Count).ToArray(), .06f, terrain, 3);
            gm.ToObject("Quarry jumps drive gravel", mats.Driveway, t, false, false);
            var mb = new MeshBuilder(); var turn = RoadSignMaterial("QuarryJumps");
            foreach (var sg in a.signs ?? new QjSign[0])
            {
                var f = new Vector3(Mathf.Sin(sg.facing * Mathf.Deg2Rad), 0f, Mathf.Cos(sg.facing * Mathf.Deg2Rad));
                Board(mb, t, terrain, new Vector3(sg.at[0], 0f, sg.at[1]), f, 2.6f, 1.3f, 1.2f, turn);
            }
            foreach (var b in a.boards ?? new QjSign[0])
            {
                var f = new Vector3(Mathf.Sin(b.facing * Mathf.Deg2Rad), 0f, Mathf.Cos(b.facing * Mathf.Deg2Rad));
                Board(mb, t, terrain, new Vector3(b.at[0], 0f, b.at[1]), f, 2.4f, 1.5f, 1.0f, RoadSignMaterial("QuarryBoard_" + b.jump));
            }
            mb.ToObject("Quarry jumps sign posts", mats.House, t, false);
            Debug.Log($"[Round73] the quarry jumps drive: {pts.Count} points, {a.signs?.Length ?? 0} turn-off boards, {a.boards?.Length ?? 0} start boards");
        }

        // Round 73 (C5): the Quarry Track's divider: a row of concrete barriers down the middle of the two-lane haul road and the plateau track, standing
        // only while the Quarry Track is raced (RaceOnly showInForwardOf): each climb keeps to its own lane
        static void QuarryTrackDivider(TerrainGrid terrain, Materials mats, Transform parent)
        {
            var t = QuarryTrackNow; if (t == null || t.divider == null || t.divider.Length < 4) return;
            var ro = new GameObject("Quarry Track divider").transform; ro.SetParent(parent, false);
            var only = ro.gameObject.AddComponent<RaceOnly>(); only.reverse = false; only.showInForwardOf = string.IsNullOrEmpty(t.route_name) ? "Quarry Track" : t.route_name;
            var p = ToPoints(t.divider); var bm = new MeshBuilder(); int n = 0;
            var holder = new GameObject("Concrete barriers").transform; holder.SetParent(ro, false);
            for (int i = 0; i + 1 < p.Count; i++)
            {
                var a = p[i]; var b = p[i + 1]; var c = (a + b) * .5f; var dir = (b - a).normalized; float L = (b - a).magnitude; float gy = Ground(terrain, c.x, c.y);
                BoxF(bm, MakeFrame(new Vector3(c.x, gy, c.y), new Vector2(-dir.y, dir.x), 1f), -L * .5f, L * .5f, -.3f, .95f, -.35f, .35f, i % 2 == 0 ? new Color(.85f, .85f, .82f) : new Color(.75f, .12f, .1f));
                var bc = new GameObject("barrier").AddComponent<BoxCollider>(); bc.transform.SetParent(holder, false);
                bc.transform.SetPositionAndRotation(new Vector3(c.x, gy + .3f, c.y), Quaternion.LookRotation(new Vector3(-dir.y, 0f, dir.x))); bc.size = new Vector3(L, 1.4f, .7f);
                n++;
            }
            bm.ToObject("Quarry Track divider look", mats.House, holder, false);
            Debug.Log($"[Round73] the Quarry Track's divider: {n} barriers (race time only)");
        }
    }
}
