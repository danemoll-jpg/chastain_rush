using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

// Round 70 (A, Dan, 2026-10-25 23:20: "I want a big ramp on the edge that drops all the way off the mountain ... at the moment just give me a big ramp
// that will clear the mountain"): the summit ramp on Kennesaw Mountain, a Free Roam world piece from Data/tracks/kennesaw_summit_ramp.json
// (Tools/mapgen/make_summit_ramp.py; the why of the place is there). The run-up is the drive's straight along the south flank; past the hairpin round
// the knob's west end, straight on: a graded pad, then a steel stunt ramp out over the north-west face (a real jump: the deck is a held ramp, RampHold,
// and nothing aims the flight), the landings on the graded open ground at the mountain's foot (a catch zone, RampHold), a gravel arrester bed (ShrubBeds)
// and a track on to Old Mountain Road NW. During a race (the Kennesaw Mountain race drives the run-up toward the hairpin) a row of concrete barriers
// stands across the pad's mouth (RaceOnly). The grades, cleared and breakable lines and the catch zone are merged into the mountain's (MapBuilder.Mountain.cs).
namespace ChastainRush
{
    public static partial class MapBuilder
    {
        [Serializable] class SummitRampData { public string id; public float[] dir, origin, deck_s, deck_y, lip, arrester, arrester_strength, race_block, start, run; public float half, angle_deg, radius_m, lip_y, tallest_m, arrester_half, start_yaw, track_to; }
        [Serializable] class SummitRampFile { public RidgeGrade[] grades; public RidgeClear[] clear_lines, break_lines; public CtZone[] landing_zones; public SummitRampData ramp; }
        const string SummitRampTrackFile = "Data/tracks/kennesaw_summit_ramp.json";
        static SummitRampData SummitRamp;

        // the planning: its grades, lines and catch zone into the mountain's (graded with them, trees cleared and made breakable with them)
        static void MergeSummitRamp(Ridge mountain)
        {
            SummitRamp = null;
            string path = Path.Combine(ProjectRoot, SummitRampTrackFile);
            if (mountain == null || !File.Exists(path)) { Debug.Log("[Round70] no " + SummitRampTrackFile + " (or no mountain pieces): no summit ramp"); return; }
            var f = JsonUtility.FromJson<SummitRampFile>(File.ReadAllText(path)); var d = mountain.Data;
            T[] Cat<T>(T[] a, T[] b) => (a ?? new T[0]).Concat(b ?? new T[0]).ToArray();
            d.grades = Cat(d.grades, f.grades); d.clear_lines = Cat(d.clear_lines, f.clear_lines); d.break_lines = Cat(d.break_lines, f.break_lines); d.landing_zones = Cat(d.landing_zones, f.landing_zones);
            SummitRamp = f.ramp;
            Debug.Log($"[Round70] the summit ramp: {f.grades?.Length ?? 0} grades, {f.clear_lines?.Length ?? 0} cleared lines, {f.landing_zones?.Length ?? 0} catch zone merged into the mountain's");
        }

        // Round 71 (the Quarry Air Race, Tools/mapgen/make_air_route.py): its low flight lanes (under I-75 at Noonday Creek, over Daniell Lake) cleared of
        // trees with the mountain's lines (the Trees step reads them); nothing else of the air race is world
        [Serializable] class AirLanesFile { public RidgeClear[] clear_lines; }
        const string AirLanesFile_ = "Data/tracks/quarry_air_race.json";
        static void MergeAirLanes(Ridge mountain)
        {
            string path = Path.Combine(ProjectRoot, AirLanesFile_);
            if (mountain == null || !File.Exists(path)) { Debug.Log("[Round71] no " + AirLanesFile_ + " (or no mountain pieces): no air race lanes cleared"); return; }
            var f = JsonUtility.FromJson<AirLanesFile>(File.ReadAllText(path)); var d = mountain.Data;
            d.clear_lines = (d.clear_lines ?? new RidgeClear[0]).Concat(f.clear_lines ?? new RidgeClear[0]).ToArray();
            Debug.Log($"[Round71] the air race: {f.clear_lines?.Length ?? 0} flight lanes cleared of trees");
        }

        static readonly Color RampSteel = new Color(0.36f, 0.38f, 0.40f), RampDeck = new Color(0.30f, 0.31f, 0.32f), RampRed = new Color(0.62f, 0.14f, 0.10f), Gravel = new Color(0.56f, 0.52f, 0.46f);

        static void BuildSummitRamp(TerrainGrid terrain, Materials mats, Transform root)
        {
            var r = SummitRamp; if (r == null || r.deck_s == null || r.deck_s.Length < 2) return;
            var go = new GameObject("The summit ramp (Round 70)").transform; go.SetParent(root, false);
            var o = new Vector2(r.origin[0], r.origin[1]); var d = new Vector2(r.dir[0], r.dir[1]).normalized; var n = new Vector2(-d.y, d.x);
            Vector3 W(float s, float lat, float y) { var q = o + d * s + n * lat; return new Vector3(q.x, y, q.y); }
            float Gnd(float s, float lat) { var q = o + d * s + n * lat; return Ground(terrain, q.x, q.y); }
            int m = r.deck_s.Length; float h = r.half, thick = 0.5f;
            // ---- the deck: its driven top a collider of its own (a held ramp: RampHold keeps a vehicle on it and its speed up it; registered with
            // VehicleSurfaceContacts as an "sc" surface, so no seam turns a contact against the travel)
            var top = new MeshBuilder(); top.Tint = RampDeck;
            for (int i = 0; i < m; i++) { top.Vertex(W(r.deck_s[i], -h, r.deck_y[i])); top.Vertex(W(r.deck_s[i], h, r.deck_y[i])); }
            for (int i = 0; i + 1 < m; i++) { int a = 2 * i; top.Tri(a, a + 1, a + 2); top.Tri(a + 1, a + 3, a + 2); }   // facing up
            var deck = top.ToObject("sc summit ramp deck", mats.House, go, false);
            var mc = deck.AddComponent<MeshCollider>(); mc.sharedMesh = deck.GetComponent<MeshFilter>().sharedMesh; mc.sharedMaterial = RampMaterial();
            deck.AddComponent<ShortcutSurface>().held = true;
            // ---- the steel: the deck's edges and underside, the lip's end plate, low edge rails (look only), the trestle bents every 8 m down to the ground
            var mb = new MeshBuilder();
            for (int i = 0; i + 1 < m; i++)
            {
                foreach (float side in new[] { -1f, 1f })
                {
                    // the deck's edge face (red stripe) and its underside
                    var a0 = W(r.deck_s[i], side * h, r.deck_y[i]); var a1 = W(r.deck_s[i + 1], side * h, r.deck_y[i + 1]);
                    mb.Tint = RampRed; int v0 = mb.Vertex(a0), v1 = mb.Vertex(a1), v2 = mb.Vertex(a1 - Vector3.up * thick), v3 = mb.Vertex(a0 - Vector3.up * thick);
                    mb.Tri(v0, v1, v2); mb.Tri(v0, v2, v3); mb.Tri(v0, v2, v1); mb.Tri(v0, v3, v2);   // both ways
                    // the rail: a low steel kerb on posts (look only: nothing to catch a wheel at speed)
                    if (i % 3 == 0) Beam(mb, a0, a0 + Vector3.up * 0.55f, 0.12f, 0.12f, RampSteel);
                    Beam(mb, a0 + Vector3.up * 0.5f, a1 + Vector3.up * 0.5f, 0.1f, 0.14f, RampSteel);
                }
                mb.Tint = RampSteel; var u0 = W(r.deck_s[i], -h, r.deck_y[i] - thick); var u1 = W(r.deck_s[i + 1], -h, r.deck_y[i + 1] - thick);
                var u2 = W(r.deck_s[i + 1], h, r.deck_y[i + 1] - thick); var u3 = W(r.deck_s[i], h, r.deck_y[i] - thick);
                int b0 = mb.Vertex(u0), b1 = mb.Vertex(u1), b2 = mb.Vertex(u2), b3 = mb.Vertex(u3); mb.Tri(b0, b1, b2); mb.Tri(b0, b2, b3);   // facing down
            }
            { int e = m - 1; var a = W(r.deck_s[e], -h, r.deck_y[e]); var b = W(r.deck_s[e], h, r.deck_y[e]); mb.Tint = RampRed;
              int p0 = mb.Vertex(a), p1 = mb.Vertex(b), p2 = mb.Vertex(b - Vector3.up * 1.2f), p3 = mb.Vertex(a - Vector3.up * 1.2f); mb.Tri(p0, p1, p2); mb.Tri(p0, p2, p3); mb.Tri(p0, p2, p1); mb.Tri(p0, p3, p2); }
            // two box girders under the deck, the length of it
            for (int i = 0; i + 1 < m; i++) foreach (float lat in new[] { -h + 0.9f, h - 0.9f }) Beam(mb, W(r.deck_s[i], lat, r.deck_y[i] - thick - 0.4f), W(r.deck_s[i + 1], lat, r.deck_y[i + 1] - thick - 0.4f), 0.5f, 0.8f, RampSteel);
            int bents = 0;
            float prevTop = float.NaN;
            for (float s = r.deck_s[0] + 6f; s <= r.deck_s[m - 1] - 0.5f; s += 8f)
            {
                int k = Mathf.Clamp(Mathf.RoundToInt(s - r.deck_s[0]), 0, m - 1); float ty = r.deck_y[k] - thick - 0.8f;
                float gl = Gnd(s, -h + 0.9f), gr = Gnd(s, h - 0.9f); if (ty - Mathf.Max(gl, gr) < 1.0f) continue;
                foreach (float lat in new[] { -h + 0.9f, h - 0.9f })
                {
                    float g = lat < 0 ? gl : gr; Beam(mb, W(s, lat * 1.15f, g - 1.0f), W(s, lat, ty), 0.6f, 0.6f, RampSteel);   // the column, splayed a little at its foot
                    // its footing: a concrete pad
                    var fq = W(s, lat * 1.15f, g); BoxF(mb, MakeFrame(fq, d, 1f), -1.0f, 1.0f, -0.6f, 0.25f, -1.0f, 1.0f, new Color(0.6f, 0.6f, 0.58f));
                }
                // the bracing: an X across the bent every 8 m of height, a strut across under the deck
                Beam(mb, W(s, -h + 0.9f, ty), W(s, h - 0.9f, ty), 0.35f, 0.35f, RampSteel);
                for (float yb = Mathf.Max(gl, gr) + 1.5f; yb + 8f <= ty; yb += 8f)
                {
                    Beam(mb, W(s, -h + 0.9f, yb), W(s, h - 0.9f, yb + 8f), 0.22f, 0.22f, RampSteel); Beam(mb, W(s, h - 0.9f, yb), W(s, -h + 0.9f, yb + 8f), 0.22f, 0.22f, RampSteel);
                }
                // and along the ramp, between this bent and the last, a diagonal on each side
                if (!float.IsNaN(prevTop)) foreach (float lat in new[] { -h + 0.9f, h - 0.9f }) Beam(mb, W(s - 8f, lat, prevTop), W(s, lat, Mathf.Max(lat < 0 ? gl : gr, ty - 12f)), 0.22f, 0.22f, RampSteel);
                prevTop = ty; bents++;
            }
            var steel = mb.ToObject("Summit ramp steel", mats.House, go, false); steel.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.On;
            // ---- the gravel arrester bed (a draped gravel ribbon, its drag: ShrubBeds) and the race-time barriers across the pad's mouth
            var arr = r.arrester != null ? ToPoints(r.arrester) : new List<Vector2>();
            if (arr.Count >= 2)
            {
                var gm = new MeshBuilder(); float ah = r.arrester_half;
                var ga = arr[0]; var gb = arr[arr.Count - 1]; float L = (gb - ga).magnitude; var gd = (gb - ga) / L; var gn = new Vector2(-gd.y, gd.x);
                int cols = Mathf.CeilToInt(2f * ah / 2f), rows = Mathf.CeilToInt(L / 2f);
                for (int i = 0; i <= rows; i++) for (int j = 0; j <= cols; j++)
                {
                    var q = ga + gd * (L * i / rows) + gn * (-ah + 2f * ah * j / cols);
                    gm.Tint = Shade(Gravel, 0.85f + 0.2f * Hash01(i * 131 + j, 7)); gm.Vertex(new Vector3(q.x, Ground(terrain, q.x, q.y) + 0.06f, q.y));
                }
                for (int i = 0; i < rows; i++) for (int j = 0; j < cols; j++) { int a = i * (cols + 1) + j; gm.Tri(a, a + 1, a + cols + 1); gm.Tri(a + 1, a + cols + 2, a + cols + 1); }   // facing up
                var bed = gm.ToObject("Summit ramp arrester gravel", mats.Driveway, go, false, false);
                var sb = bed.AddComponent<ShrubBeds>(); var sa = new List<Vector2>(); var sbb = new List<Vector2>(); var shh = new List<float>(); var ss = new List<float>();
                for (int i = 0; i + 1 < arr.Count; i++) { sa.Add(arr[i]); sbb.Add(arr[i + 1]); shh.Add(ah); ss.Add(r.arrester_strength != null && i < r.arrester_strength.Length ? r.arrester_strength[i] : 0.3f); }
                sb.a = sa.ToArray(); sb.b = sbb.ToArray(); sb.half = shh.ToArray(); sb.strength = ss.ToArray();
            }
            if (r.race_block != null && r.race_block.Length >= 4)
            {
                var ro = new GameObject("Summit ramp race barriers").transform; ro.SetParent(go, false); var only = ro.gameObject.AddComponent<RaceOnly>(); only.any = true;
                var b0 = new Vector2(r.race_block[0], r.race_block[1]); var b1 = new Vector2(r.race_block[2], r.race_block[3]); float L = (b1 - b0).magnitude; var bd = (b1 - b0) / L;
                var bm = new MeshBuilder(); int nb = Mathf.CeilToInt(L / 3.2f);
                var holder = new GameObject("Concrete barriers").transform; holder.SetParent(ro, false);
                for (int i = 0; i < nb; i++)
                {
                    var c = b0 + bd * (L * (i + 0.5f) / nb); float gy = Ground(terrain, c.x, c.y);
                    BoxF(bm, MakeFrame(new Vector3(c.x, gy, c.y), new Vector2(-bd.y, bd.x), 1f), -1.5f, 1.5f, -0.3f, 0.95f, -0.35f, 0.35f, i % 2 == 0 ? new Color(0.85f, 0.85f, 0.82f) : new Color(0.75f, 0.12f, 0.1f));
                    var bc = new GameObject("barrier").AddComponent<BoxCollider>(); bc.transform.SetParent(holder, false);
                    bc.transform.SetPositionAndRotation(new Vector3(c.x, gy + 0.3f, c.y), Quaternion.LookRotation(new Vector3(-bd.y, 0f, bd.x)));
                    bc.size = new Vector3(3.0f, 1.3f, 0.7f);
                }
                bm.ToObject("Summit ramp race barriers look", mats.House, holder, false);
            }
            Debug.Log($"[Round70] the summit ramp: the deck {r.deck_s[m - 1] - r.deck_s[0]:0} m of steel ({r.angle_deg:0} degrees at the lip, R {r.radius_m:0}), its lip at ({r.lip[0]:0.0}, {r.lip[1]:0.0}) {r.lip_y:0.0} m, up to {r.tallest_m:0} m over the ground, {bents} trestle bents; the arrester {arr.Count - 1} lengths; race barriers across its mouth");
        }
    }
}
