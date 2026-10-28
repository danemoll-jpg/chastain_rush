using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

// (Round 73 A3: the steel ramp below is gone, replaced by the summit leap; SteelRamp stays for the quarry jumps)
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
        // Round 73 (A3, Dan: "why is it not even at the highest point of the mountain" ... "just a jump off of something that could naturally be
        // there"): Round 70's steel ramp is gone; THE SUMMIT LEAP replaces it (Tools/mapgen/make_summit_leap.py, Data/tracks/kennesaw_summit_leap.json):
        // a gravel track from the summit lot up the north-east ridge, cut through the peak, curving up (the ground itself) to a granite ledge at the
        // summit's height on its south-south-west edge; the catch on the lower flank, a gravel arrester bed and a track to North Saint Marys Lane NW.
        [Serializable] class SummitLeapData { public string id; public float[] arrester, arrester_strength, start, track, summit; public float arrester_half, start_yaw, crest_y; }
        [Serializable] class SummitLeapFile { public RidgeGrade[] grades; public RidgeClear[] clear_lines, break_lines; public CtZone[] landing_zones; public MountainJump jump; public SummitLeapData leap; }
        const string SummitLeapTrackFile = "Data/tracks/kennesaw_summit_leap.json";
        static SummitLeapFile SummitLeap;

        // the planning: its grades, lines and catch zone into the mountain's (graded with them, trees cleared and made breakable with them)
        static void MergeSummitRamp(Ridge mountain)
        {
            SummitLeap = null;
            string path = Path.Combine(ProjectRoot, SummitLeapTrackFile);
            if (mountain == null || !File.Exists(path)) { Debug.Log("[Round73] no " + SummitLeapTrackFile + " (or no mountain pieces): no summit leap"); return; }
            var f = JsonUtility.FromJson<SummitLeapFile>(File.ReadAllText(path)); var d = mountain.Data;
            T[] Cat<T>(T[] a, T[] b) => (a ?? new T[0]).Concat(b ?? new T[0]).ToArray();
            d.grades = Cat(d.grades, f.grades); d.clear_lines = Cat(d.clear_lines, f.clear_lines); d.break_lines = Cat(d.break_lines, f.break_lines); d.landing_zones = Cat(d.landing_zones, f.landing_zones);
            SummitLeap = f;
            Debug.Log($"[Round73] the summit leap: {f.grades?.Length ?? 0} grades, {f.clear_lines?.Length ?? 0} cleared lines, {f.landing_zones?.Length ?? 0} catch zone merged into the mountain's");
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
            // Round 78 (C): the air tour's lanes too (South Marietta Parkway's trees either side of its run under the railroad)
            string tour = Path.Combine(ProjectRoot, "Data/tracks/air_tour.json");
            if (File.Exists(tour)) { var ft = JsonUtility.FromJson<AirLanesFile>(File.ReadAllText(tour)); d.clear_lines = d.clear_lines.Concat(ft.clear_lines ?? new RidgeClear[0]).ToArray(); Debug.Log($"[Round78] the air tour: {ft.clear_lines?.Length ?? 0} flight lanes cleared of trees"); }
        }

        // Round 72: the summit ramp's steel deck as a builder of its own (the quarry jumps' kickers and landing decks too): the deck's driven top on its
        // own collider (an "sc" surface; held: RampHold keeps a vehicle on it), its red edges, underside, kerb rails, two box girders and the
        // trestle bents every 8 m down to the ground, along the line o + d * s, the deck at deck_y over deck_s. Returns the bents.
        static int SteelRamp(TerrainGrid terrain, Materials mats, Transform go, string deckName, string steelName, Vector2 o, Vector2 d, float[] deck_s, float[] deck_y, float half, bool held)
        {
            var n = new Vector2(-d.y, d.x);
            Vector3 W(float s, float lat, float y) { var q = o + d * s + n * lat; return new Vector3(q.x, y, q.y); }
            float Gnd(float s, float lat) { var q = o + d * s + n * lat; return Ground(terrain, q.x, q.y); }
            int m = deck_s.Length; float h = half, thick = 0.5f;
            // ---- the deck: its driven top a collider of its own (a held ramp: RampHold keeps a vehicle on it and its speed up it; registered with
            // VehicleSurfaceContacts as an "sc" surface, so no seam turns a contact against the travel)
            var top = new MeshBuilder(); top.Tint = RampDeck;
            for (int i = 0; i < m; i++) { top.Vertex(W(deck_s[i], -h, deck_y[i])); top.Vertex(W(deck_s[i], h, deck_y[i])); }
            for (int i = 0; i + 1 < m; i++) { int a = 2 * i; top.Tri(a, a + 1, a + 2); top.Tri(a + 1, a + 3, a + 2); }   // facing up
            var deck = top.ToObject(deckName, mats.House, go, false);
            var mc = deck.AddComponent<MeshCollider>(); mc.sharedMesh = deck.GetComponent<MeshFilter>().sharedMesh; mc.sharedMaterial = RampMaterial();
            deck.AddComponent<ShortcutSurface>().held = held;
            // ---- the steel: the deck's edges and underside, the lip's end plate, low edge rails (look only), the trestle bents every 8 m down to the ground
            var mb = new MeshBuilder();
            for (int i = 0; i + 1 < m; i++)
            {
                foreach (float side in new[] { -1f, 1f })
                {
                    // the deck's edge face (red stripe) and its underside
                    var a0 = W(deck_s[i], side * h, deck_y[i]); var a1 = W(deck_s[i + 1], side * h, deck_y[i + 1]);
                    mb.Tint = RampRed; int v0 = mb.Vertex(a0), v1 = mb.Vertex(a1), v2 = mb.Vertex(a1 - Vector3.up * thick), v3 = mb.Vertex(a0 - Vector3.up * thick);
                    mb.Tri(v0, v1, v2); mb.Tri(v0, v2, v3); mb.Tri(v0, v2, v1); mb.Tri(v0, v3, v2);   // both ways
                    // the rail: a low steel kerb on posts (look only: nothing to catch a wheel at speed)
                    if (i % 3 == 0) Beam(mb, a0, a0 + Vector3.up * 0.55f, 0.12f, 0.12f, RampSteel);
                    Beam(mb, a0 + Vector3.up * 0.5f, a1 + Vector3.up * 0.5f, 0.1f, 0.14f, RampSteel);
                }
                mb.Tint = RampSteel; var u0 = W(deck_s[i], -h, deck_y[i] - thick); var u1 = W(deck_s[i + 1], -h, deck_y[i + 1] - thick);
                var u2 = W(deck_s[i + 1], h, deck_y[i + 1] - thick); var u3 = W(deck_s[i], h, deck_y[i] - thick);
                int b0 = mb.Vertex(u0), b1 = mb.Vertex(u1), b2 = mb.Vertex(u2), b3 = mb.Vertex(u3); mb.Tri(b0, b1, b2); mb.Tri(b0, b2, b3);   // facing down
            }
            { int e = m - 1; var a = W(deck_s[e], -h, deck_y[e]); var b = W(deck_s[e], h, deck_y[e]); mb.Tint = RampRed;
              int p0 = mb.Vertex(a), p1 = mb.Vertex(b), p2 = mb.Vertex(b - Vector3.up * 1.2f), p3 = mb.Vertex(a - Vector3.up * 1.2f); mb.Tri(p0, p1, p2); mb.Tri(p0, p2, p3); mb.Tri(p0, p2, p1); mb.Tri(p0, p3, p2); }
            // two box girders under the deck, the length of it
            for (int i = 0; i + 1 < m; i++) foreach (float lat in new[] { -h + 0.9f, h - 0.9f }) Beam(mb, W(deck_s[i], lat, deck_y[i] - thick - 0.4f), W(deck_s[i + 1], lat, deck_y[i + 1] - thick - 0.4f), 0.5f, 0.8f, RampSteel);
            int bents = 0;
            float prevTop = float.NaN;
            for (float s = deck_s[0] + 6f; s <= deck_s[m - 1] - 0.5f; s += 8f)
            {
                int k = Mathf.Clamp(Mathf.RoundToInt(s - deck_s[0]), 0, m - 1); float ty = deck_y[k] - thick - 0.8f;
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
            var steel = mb.ToObject(steelName, mats.House, go, false); steel.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.On;
            return bents;
        }

        static readonly Color RampSteel = new Color(0.36f, 0.38f, 0.40f), RampDeck = new Color(0.30f, 0.31f, 0.32f), RampRed = new Color(0.62f, 0.14f, 0.10f), Gravel = new Color(0.56f, 0.52f, 0.46f);

        static void BuildSummitRamp(TerrainGrid terrain, Materials mats, Transform root)
        {
            var f = SummitLeap; if (f == null || f.jump == null || f.leap == null) return;
            var go = new GameObject("The summit leap (Round 73)").transform; go.SetParent(root, false);
            // the lip: the mountain's own granite (Round 68's overlook look: slabs on its back face, boulders along the kicker's banks)
            MountainJumpLook(terrain, mats, go, f.jump);
            // Round 74 (F): over the kicker the ground does not push on an upright body (BodyGlide, Round 56's): the Drifter Twin, a low cruiser, bottomed
            // out in it at 43 m/s and lost 21 m/s before the lip (the wheels, raycasts, carry it); rectangles every 8 m along the kicker, 22 m across
            {
                var gl = new Vector2(f.jump.lip[0], f.jump.lip[1]); var gd = new Vector2(f.jump.dir[0], f.jump.dir[1]).normalized; var rects = new List<Vector4>();
                for (float k = -2f; k <= f.jump.kicker_m + 30f; k += 8f) { var c = gl - gd * k; rects.Add(new Vector4(c.x - 11f, c.y - 11f, c.x + 11f, c.y + 11f)); }
                var gg = new GameObject("Summit leap kicker body glide"); gg.transform.SetParent(go, false); gg.AddComponent<BodyGlide>().rects = rects.ToArray();
            }
            // gravel on the run-up track (from the lot up the ridge to the kicker's foot) and on the track down to the lane
            var gm = new MeshBuilder(); gm.Tint = Gravel;
            var run = f.grades != null && f.grades.Length > 0 ? ToPoints(f.grades[0].points) : new List<Vector2>();
            var lip = new Vector2(f.jump.lip[0], f.jump.lip[1]);
            // (from 12 m up the track: its first meters run over the bank beside the drive, which the grade leaves as it was to keep the drive whole)
            var onTrack = run.Where(q => (q - lip).magnitude > f.jump.kicker_m + 4f && Vector2.Dot(q - lip, new Vector2(f.jump.dir[0], f.jump.dir[1])) < 0f).Skip(6).ToList();
            if (onTrack.Count >= 2) RibbonGrid(gm, onTrack, Enumerable.Repeat(7f, onTrack.Count).ToArray(), 0.06f, terrain, 4);
            var back = f.leap.track != null ? ToPoints(f.leap.track) : new List<Vector2>();
            if (back.Count >= 2) RibbonGrid(gm, back, Enumerable.Repeat(5f, back.Count).ToArray(), 0.06f, terrain, 3);
            if (gm.Count > 0) gm.ToObject("Summit leap gravel track", mats.Driveway, go, false, false);
            // the gravel arrester bed (a draped gravel ribbon, its drag: ShrubBeds)
            var arr = f.leap.arrester != null ? ToPoints(f.leap.arrester) : new List<Vector2>();
            if (arr.Count >= 2)
            {
                var am = new MeshBuilder(); float ah = f.leap.arrester_half;
                var ga = arr[0]; var gb = arr[arr.Count - 1]; float L = (gb - ga).magnitude; var gd = (gb - ga) / L; var gn = new Vector2(-gd.y, gd.x);
                int cols = Mathf.CeilToInt(2f * ah / 2f), rows = Mathf.CeilToInt(L / 2f);
                for (int i = 0; i <= rows; i++) for (int j = 0; j <= cols; j++)
                {
                    var q = ga + gd * (L * i / rows) + gn * (-ah + 2f * ah * j / cols);
                    am.Tint = Shade(Gravel, 0.85f + 0.2f * Hash01(i * 131 + j, 7)); am.Vertex(new Vector3(q.x, Ground(terrain, q.x, q.y) + 0.06f, q.y));
                }
                for (int i = 0; i < rows; i++) for (int j = 0; j < cols; j++) { int a0 = i * (cols + 1) + j; am.Tri(a0, a0 + 1, a0 + cols + 1); am.Tri(a0 + 1, a0 + cols + 2, a0 + cols + 1); }   // facing up
                var bed = am.ToObject("Summit leap arrester gravel", mats.Driveway, go, false, false);
                var sb = bed.AddComponent<ShrubBeds>(); var sa = new List<Vector2>(); var sbb = new List<Vector2>(); var shh = new List<float>(); var ss = new List<float>();
                for (int i = 0; i + 1 < arr.Count; i++) { sa.Add(arr[i]); sbb.Add(arr[i + 1]); shh.Add(ah); ss.Add(f.leap.arrester_strength != null && i < f.leap.arrester_strength.Length ? f.leap.arrester_strength[i] : 0.3f); }
                sb.a = sa.ToArray(); sb.b = sbb.ToArray(); sb.half = shh.ToArray(); sb.strength = ss.ToArray();
            }
            Debug.Log($"[Round73] the summit leap: the lip at ({f.jump.lip[0]:0.0}, {f.jump.lip[1]:0.0}) {f.jump.lip_y:0.0} m ({f.jump.height:0.0} m over the ground, {f.jump.angle_deg:0} degrees, a {f.jump.kicker_m:0} m kicker), the run-up from ({f.leap.start[0]:0.0}, {f.leap.start[1]:0.0}); the arrester {arr.Count - 1} lengths, {back.Count} track points to the lane");
        }
    }
}
