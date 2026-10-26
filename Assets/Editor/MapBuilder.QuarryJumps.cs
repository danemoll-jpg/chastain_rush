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
        [Serializable] class QjSpot { public float[] at; public float yaw; }
        [Serializable] class QjJump { public string id, name; public float[] origin, dir, deck_s, deck_y, land_s, land_y, start; public float half, lip_s, lip_y, land_half, runout_to; public QjSpot gantry; public QjSpot[] stands; }
        [Serializable] class QjFile { public RidgeGrade[] grades; public RidgeClear[] clear_lines; public CtZone[] landing_zones; public QjJump[] jumps; }
        const string QuarryJumpsFile = "Data/tracks/quarry_jumps.json";
        static QjFile QuarryJumpsNow; static string QuarryJumpsJson;

        static void MergeQuarryJumps(Ridge mountain)
        {
            QuarryJumpsNow = null; QuarryJumpsJson = null;
            string path = Path.Combine(ProjectRoot, QuarryJumpsFile);
            if (mountain == null || !File.Exists(path)) { Debug.Log("[Round72] no " + QuarryJumpsFile + " (or no mountain pieces): no quarry jumps"); return; }
            QuarryJumpsJson = File.ReadAllText(path); var f = QuarryJumpsNow = JsonUtility.FromJson<QjFile>(QuarryJumpsJson); var d = mountain.Data;
            T[] Cat<T>(T[] a, T[] b) => (a ?? new T[0]).Concat(b ?? new T[0]).ToArray();
            d.grades = Cat(d.grades, f.grades); d.clear_lines = Cat(d.clear_lines, f.clear_lines); d.landing_zones = Cat(d.landing_zones, f.landing_zones);
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
                int bl = SteelRamp(terrain, mats, jt, $"sc quarry {j.id} landing deck", $"Quarry {j.id} landing steel", o, d, j.land_s, j.land_y, j.land_half, false);
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
                for (int k = 0; k < 3; k++)
                {
                    var lamp = GameObject.CreatePrimitive(PrimitiveType.Sphere); UnityEngine.Object.DestroyImmediate(lamp.GetComponent<Collider>());
                    lamp.name = $"Countdown lamp {k}"; lamp.transform.SetParent(jt, false); lamp.transform.position = G(gw - .1f, gh - 2.2f - k * 1.1f, -.9f); lamp.transform.localScale = Vector3.one * .8f;
                    lamp.GetComponent<Renderer>().sharedMaterial = mats.House; lamps.Add(lamp.transform);
                }
                // ---- bleachers: five rows of seats stepping up, a back rail, the crowd on them (a body and a head each, shirts of every color)
                int sn = 0;
                foreach (var stand in j.stands ?? new QjSpot[0])
                {
                    var c = new Vector2(stand.at[0], stand.at[1]);
                    // facing the jump's line (square across it)
                    float lat = Vector2.Dot(c - o, nl); var face = -nl * Mathf.Sign(lat);
                    float gy = Ground(terrain, c.x, c.y);
                    for (int dx = -10; dx <= 10; dx += 5) foreach (int dz in new[] { -3, 3 }) { var q = c + new Vector2(face.y, -face.x) * dx + face * dz; gy = Mathf.Min(gy, Ground(terrain, q.x, q.y)); }
                    var fr = MakeFrame(new Vector3(c.x, gy, c.y), face, 1f);
                    var bm = new MeshBuilder();
                    for (int row = 0; row < 5; row++)
                    {
                        float v = 2.2f - row * 1.1f, y = .45f + row * .6f;
                        BoxF(bm, fr, -11f, 11f, y - .12f, y, v - .5f, v + .5f, new Color(.62f, .6f, .56f));        // the seat plank
                        BoxF(bm, fr, -11f, 11f, 0f, y - .12f, v - .55f, v - .45f, new Color(.4f, .41f, .43f));    // the riser
                        for (int seat = 0; seat < 20; seat++)
                        {
                            int hsh = (int)(c.x * 7 + c.y * 13) + row * 31 + seat * 7;
                            if (Hash01(hsh, 1) < .18f) continue;   // an empty seat here and there
                            float u = -10.4f + seat * 1.1f + (Hash01(hsh, 2) - .5f) * .2f;
                            var shirt = Color.HSVToRGB(Hash01(hsh, 3), .35f + .5f * Hash01(hsh, 4), .45f + .5f * Hash01(hsh, 5));
                            var skin = Color.Lerp(new Color(.36f, .24f, .17f), new Color(.92f, .76f, .62f), Hash01(hsh, 6));
                            BoxF(bm, fr, u - .22f, u + .22f, y, y + .7f, v - .15f, v + .15f, shirt);
                            BoxF(bm, fr, u - .12f, u + .12f, y + .75f, y + 1.0f, v - .12f, v + .12f, skin);
                            people++;
                        }
                    }
                    for (int k = -11; k <= 11; k += 2) Beam(bm, fr.P(k, 0f, -2.7f), fr.P(k, 4.3f, -2.7f), .1f, .1f, RampSteel);
                    Beam(bm, fr.P(-11f, 4.3f, -2.7f), fr.P(11f, 4.3f, -2.7f), .1f, .1f, RampSteel);
                    var bobj = bm.ToObject($"Quarry {j.id} bleachers {sn}", mats.House, jt, false);
                    var center = fr.P(0f, 1.6f, 0f);   // solid: a box over the stand (a child turned to it: the mesh's object stands at the origin)
                    var cc = new GameObject("bleachers solid").AddComponent<BoxCollider>(); cc.transform.SetParent(bobj.transform, false);
                    cc.transform.SetPositionAndRotation(center, Quaternion.LookRotation(new Vector3(face.x, 0f, face.y))); cc.size = new Vector3(22f, 3.2f, 6f);
                    sn++;
                }
                Debug.Log($"[Round72] quarry jump {j.id} {j.name}: kicker {j.deck_s[j.deck_s.Length - 1] - j.deck_s[0]:0} m to its lip at {j.lip_y:0.0} m ({bk} bents), landing deck {j.land_s[j.land_s.Length - 1] - j.land_s[0]:0} m ({bl} bents), {sn} bleachers");
            }
            // the event (game layer): the jumps' data and the countdown lamps
            var ev = new GameObject("Quarry jumps event").AddComponent<QuarryJumps>(); ev.transform.SetParent(go, false);
            ev.json = QuarryJumpsJson; ev.lights = lamps.ToArray();
            Debug.Log($"[Round72] the Viking Quarry jumps: {f.jumps.Length} jumps, {people} people in the bleachers");
        }
    }
}
