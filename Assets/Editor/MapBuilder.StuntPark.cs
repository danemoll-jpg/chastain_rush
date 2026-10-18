using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Racer;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// Round 36 (Part A2, A3): the stunt park on the shopping center's lot (Dan: "change the Kroger parking lot into a stunt course ... a bigger version of
// the stunt course where you can get up on top of the building area with stunts up there. Would like to get big air on these"; "The course should
// seem like someone specifically designed it for the purpose of being a stunt course", inspired by the stunt arena of Rush 2: Extreme Racing USA,
// copied from none). Racer's Red Clay Stunt Park (StuntPark.cs) is the starting point: its profile turtle (one mesh per ramp, no seams), its ramp
// extrusion, its kickers, tables, landings, quarter-pipes, half-pipe, loop, barrel-roll ramp, junk cars, school bus, containers, light poles and
// tire wall are ported here into the map build. The layout and every size are data: Data/tracks/stunt_park.json from
// Tools/mapgen/make_stunt_park.py (which also sizes the landings of the aimed launches off the roofs from their flights).
//   The floor: graded to make_stunt_park.py's floor (one tilted plane, level by the stores' fronts), the lot's asphalt kept, its stalls and paint
//     gone, the park's own paint on it (the start line, chevron lanes). The knoll by Chastain Corner is left as it is, inside the catch fence.
//   The edge: a tire wall with a catch fence on it along the lot's edge where it meets Chastain Corner and Canton Road (openings where the lot's drives
//     were), banners on it. Nothing of a road is touched (the grading fades out before any road outside the lot).
//   The stores, their insides and their roofs: MapBuilder.StuntParkStores.cs.
//   Kept as they were: the gas station and its pumps, the abandoned bank (the grading leaves their pads alone).
// The park is world; the Stunt event and the scoring are game layer (Assets/Scripts/Stunts).
namespace ChastainRush
{
    public static partial class MapBuilder
    {
        [Serializable] class StuntFeature
        {
            public string kind, id, name, note, look, @base = "floor";
            public float u, v, du, dv, width, bank_end, u_top, angle, r, top_flat, h, th, v_design, lip_u, lip_v, side_u, side_v, radius, shift; public int k;
            public float[] profile, atrium;
        }
        [Serializable] class StuntAiLineData { public string id; public string[] next; public float[] weights, points, speeds; }   // Round 38 (Part D)
        [Serializable] class StuntParkData
        {
            public string title; public float[] U, V; public int center_building;
            public float roof_y, front_y, front_u, row_v, apron, plane_y, plane_du, plane_dv, plane_u0, plane_v0;
            public float[] floor, outline, lot_south, canopy, bank; public StuntFeature[] features;
            public float[] market_glass, shelves, shelf_u, checkouts, aisle_kickers, mezz, mezz_ramp, mezz_dropin, skylight; public float shelf_h, shelf_d, checkout_u; public int shop_fronts;
            public float[] start, entrance, openings, resets, lights; public string[] landings;
            public float[] bank_ring, bank_doors, bank_roof_resets; public float bank_roof, bank_parapet;
            public float[] shop_ramp, bank_vault, bank_teller;   // Round 38 (Part C): MapBuilder.StuntParkShops.cs
            public StuntAiLineData[] ai_lines; public float[] ai_grid;   // Round 38 (Part D): the stunt rivals' lines (StuntRival)   // Round 38 (Part A): the bank in the park (MapBuilder.StuntParkBank.cs)
        }

        // the park as the build uses it (null: no park file, the lot stays a parking lot)
        sealed class StuntParkPlan
        {
            public StuntParkData D; public Vector2 U, V; public List<Vector2> FloorUV, Floor, Outline, Ring;
            public readonly List<(Vector2 c, float half, float y)> Pads = new();   // level pads (the loop)
            public readonly List<FenceChunk> Breakables = new();
            public readonly List<string> Log = new();
            public Collider LoopCollider; public Vector3 LoopOrigin, LoopForward, LoopSide, BarrelLip, BarrelForward;
            public Vector2 W(float u, float v) => U * u + V * v;
            public Vector2 UVof(Vector2 p) => new Vector2(Vector2.Dot(p, U), Vector2.Dot(p, V));
            public Vector3 Dir3(float du, float dv) { var d = (U * du + V * dv).normalized; return new Vector3(d.x, 0f, d.y); }
            static float S01(float t) { t = Mathf.Clamp01(t); return t * t * (3f - 2f * t); }
            // make_stunt_park.py's floor_y: the plane, level by the supermarket's front and by the shop row's
            public float FloorUVy(float u, float v)
            {
                float p = D.plane_y + D.plane_du * (u - D.plane_u0) + D.plane_dv * (v - D.plane_v0);
                float w = S01((u - D.front_u - 2f) / D.apron) * S01((D.row_v - 2f - v) / D.apron);
                float y = D.front_y + w * (p - D.front_y);
                foreach (var pad in Pads) { float d = Mathf.Max(Mathf.Abs(u - pad.c.x), Mathf.Abs(v - pad.c.y)) - pad.half; float k = S01(1f - d / 6f); y += k * (pad.y - y); }
                return y;
            }
            public float FloorAt(Vector2 world) { var q = UVof(world); return FloorUVy(q.x, q.y); }
            public float Base(string b, float u, float v) => b == "roof" ? D.roof_y : b == "front" ? D.front_y : b == "bank" ? D.bank_roof : FloorUVy(u, v);
            public bool HasBank => D.bank_ring != null && D.bank_ring.Length >= 6 && D.bank_roof > 0f;   // Round 38: the bank is the park's (built by MapBuilder.StuntParkBank.cs)
            public bool InFloor(Vector2 world) => InPolygon(world, Floor);
            public bool InBuilding(Vector2 world) => InPolygon(world, Ring);
            public bool InPark(Vector2 world) => InPolygon(world, Outline);
        }
        static StuntParkPlan Park;

        static List<Vector2> Pairs(float[] f) { var l = new List<Vector2>(); for (int i = 0; i + 1 < f.Length; i += 2) l.Add(new Vector2(f[i], f[i + 1])); return l; }

        static StuntParkPlan LoadStuntPark(MapData map)
        {
            var path = Path.Combine(ProjectRoot, "Data/tracks/stunt_park.json");
            if (!File.Exists(path)) { Debug.LogWarning("[Round36] " + path + " missing (Tools/mapgen/make_stunt_park.py): no stunt park"); return null; }
            var d = JsonUtility.FromJson<StuntParkData>(File.ReadAllText(path));
            if (d.center_building < 0 || d.center_building >= map.buildings.Length || map.buildings[d.center_building].type != "retail") { Debug.LogWarning("[Round36] the stunt park's building is not the shopping center any more: re-run make_stunt_park.py"); return null; }
            var p = new StuntParkPlan { D = d, U = new Vector2(d.U[0], d.U[1]), V = new Vector2(d.V[0], d.V[1]) };
            p.FloorUV = Pairs(d.floor); p.Floor = p.FloorUV.Select(q => p.W(q.x, q.y)).ToList(); p.Outline = Pairs(d.outline).Select(q => p.W(q.x, q.y)).ToList();
            p.Ring = ToPoints(map.buildings[d.center_building].footprint);
            var loop = d.features.FirstOrDefault(f => f.kind == "loop");
            if (loop != null) { float y0 = p.FloorUVy(loop.u, loop.v); p.Pads.Add((new Vector2(loop.u, loop.v + loop.side_v * loop.shift * .5f), 10f, y0)); }
            return p;
        }

        // ------------------------------------------------------------ grading (after the lot's own grading, before anything samples the ground)
        static void GradeStuntPark(MapData map, TerrainGrid t, StuntParkPlan p)
        {
            var d = p.D;
            Vector2 lo = new Vector2(p.Outline.Min(q => q.x), p.Outline.Min(q => q.y)) - Vector2.one * 10f, hi = new Vector2(p.Outline.Max(q => q.x), p.Outline.Max(q => q.y)) + Vector2.one * 10f;
            var centre = (lo + hi) * .5f; float reach = (hi - lo).magnitude * .6f;
            // roads that have no point on the lot keep their ground (the lot's own service roads are graded with it)
            var lot = LotRing.ToList();
            var outside = RoadSegsNear(map, centre, reach, ri => !ToPoints(map.roads[ri].points).Any(q => InPolygon(q, lot)));
            Rect R(float[] r) => Rect.MinMaxRect(r[0], r[2], r[1], r[3]);
            var canopy = R(d.canopy); var bank = R(d.bank);
            float Keep(Rect r, Vector2 q) { float du = Mathf.Max(0f, Mathf.Max(r.xMin - 1f - q.x, q.x - r.xMax - 1f)), dv = Mathf.Max(0f, Mathf.Max(r.yMin - 1f - q.y, q.y - r.yMax - 1f)); return Smooth01(1f - Mathf.Sqrt(du * du + dv * dv) / 6f); }
            float most = 0f; int n = 0;
            EditTerrain(t, Rect.MinMaxRect(lo.x, lo.y, hi.x, hi.y), (x, z, y) =>
            {
                var w2 = new Vector2(x, z);
                if (p.InBuilding(w2)) return Mathf.Min(y, d.front_y - 0.3f);   // under the stores (holed; MapBuilder.StuntParkStores.cs)
                float w;
                if (p.InFloor(w2)) w = 1f;
                else { float dist = RingDist(w2, p.Floor); if (dist >= 6f) return y; w = Smooth01(1f - dist / 6f); }
                var q = p.UVof(w2);
                w *= Smooth01((Clearance(outside, w2) - 1f) / 3f);
                w *= 1f - Mathf.Max(Keep(canopy, q), p.HasBank ? 0f : Keep(bank, q));   // Round 38: the bank's pad is graded with the floor (the bank is the park's)
                if (w <= 1e-3f) return y;
                float ny = y + w * (p.FloorUVy(q.x, q.y) - y); most = Mathf.Max(most, Mathf.Abs(ny - y)); n++;
                return ny;
            });
            p.Log.Add($"floor graded to the plane ({d.plane_du * 100f:0.0} % along the shop row, {d.plane_dv * 100f:0.0} % across it), level {d.front_y:0.00} m by the stores' fronts; {n} samples, largest change {most:0.00} m");
        }

        // ------------------------------------------------------------ Racer's profile turtle (StuntPark.Prof), for the roof ramp built here
        sealed class SProf
        {
            public readonly List<Vector2> pts = new(); public float s, y, a;
            public SProf() { pts.Add(Vector2.zero); }
            SProf Add() { pts.Add(new Vector2(s, y)); return this; }
            public SProf Arc(float r, float da)
            {
                int n = Mathf.Max(2, Mathf.CeilToInt(Mathf.Abs(da) / 3)); float a0 = a * Mathf.Deg2Rad;
                Vector2 c = da > 0 ? new Vector2(s - r * Mathf.Sin(a0), y + r * Mathf.Cos(a0)) : new Vector2(s + r * Mathf.Sin(a0), y - r * Mathf.Cos(a0));
                for (int i = 1; i <= n; i++) { float a1 = (a + da * i / n) * Mathf.Deg2Rad; var q = da > 0 ? c + new Vector2(r * Mathf.Sin(a1), -r * Mathf.Cos(a1)) : c + new Vector2(-r * Mathf.Sin(a1), r * Mathf.Cos(a1)); s = q.x; y = q.y; Add(); }
                a += da; return this;
            }
            public SProf Line(float len) { s += len * Mathf.Cos(a * Mathf.Deg2Rad); y += len * Mathf.Sin(a * Mathf.Deg2Rad); return Add(); }
            public SProf Flat(float len) { a = 0; s += len; return Add(); }
        }

        // ------------------------------------------------------------ colours (Racer's park colours plus the course's paint)
        static Color SC(float r, float g, float b) => new Color(r, g, b);
        static readonly Color SPly = SC(.74f, .60f, .42f), SPlyDark = SC(.55f, .43f, .29f), SSteel = SC(.62f, .63f, .65f), SSteelDark = SC(.36f, .37f, .40f), STire = SC(.07f, .07f, .075f),
            SRed = SC(.80f, .14f, .12f), SWhite = SC(.92f, .91f, .88f), SYellow = SC(.95f, .74f, .10f), SBlack = SC(.08f, .08f, .08f), SBlue = SC(.14f, .36f, .78f), STeal = SC(.08f, .55f, .55f),
            SPurple = SC(.48f, .20f, .64f), SGreen = SC(.16f, .55f, .24f), SOrange = SC(.93f, .45f, .08f), SDirt = SC(.50f, .33f, .22f);
        static float SNoise(float x, float z) { unchecked { int h = (int)(x * 73856093) ^ (int)(z * 19349663); h = (h ^ (h >> 13)) * 1274126177; return ((h & 0xffff) / 65535f) - .5f; } }
        static Color SVary(Color c, float x, float z, float amount = .05f) { float k = 1 + SNoise(x, z) * amount * 2; return new Color(c.r * k, c.g * k, c.b * k, c.a); }
        // a look: the top's two band colours (bands across the ramp, every `band` meters), the sides, the edge stripe
        static (Color a, Color b, float band, Color side, Color edge) Look(string look) => look switch
        {
            "ply" => (SPly, SC(.69f, .55f, .38f), 2.44f, SPlyDark, SRed),
            "table" => (SYellow, SC(.92f, .66f, .08f), 2f, SBlack, SBlack),
            "barrel" => (SRed, SWhite, 1.5f, SRed * .8f, SWhite),
            "launch" => (SRed, SWhite, 1.2f, SSteelDark, SYellow),
            "steel" => (SSteel, SC(.56f, .57f, .60f), 1.5f, SSteelDark, SYellow),
            "mega" => (SBlue, SC(.20f, .44f, .86f), 3f, SC(.10f, .26f, .58f), SWhite),
            "drop" => (STeal, SC(.12f, .62f, .62f), 3f, SC(.05f, .38f, .38f), SWhite),
            "roofgap" => (SPurple, SC(.56f, .28f, .72f), 2f, SC(.32f, .12f, .44f), SWhite),
            "tower" => (SGreen, SC(.22f, .62f, .30f), 3f, SSteelDark, SYellow),
            "bank" => (SC(.14f, .42f, .24f), SC(.18f, .50f, .29f), 3f, SC(.08f, .28f, .15f), SYellow),   // Round 38: the bank drop's landing (money green)
            "dirt" => (SOrange, SC(.98f, .55f, .14f), 2.5f, SC(.62f, .30f, .06f), SWhite),
            _ => (SPly, SPlyDark, 2f, SPlyDark, SRed),
        };

        // ------------------------------------------------------------ Racer's mesh helpers (StuntPark.Mb), into this build's MeshBuilder
        static void SQuad(MeshBuilder mb, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color col, Vector3 outward) { mb.Tint = col; Face(mb, a, b, c, d, outward); }
        static void SBox(MeshBuilder mb, Vector3 bottom, Vector3 forward, Vector3 size, Color col, Color? top = null)
        {
            forward.y = 0; forward.Normalize(); var right = Vector3.Cross(Vector3.up, forward);
            Vector3 Q(float x, float y, float z) => bottom + right * (x * size.x * .5f) + Vector3.up * (y * size.y) + forward * (z * size.z * .5f);
            SQuad(mb, Q(-1, 0, 1), Q(1, 0, 1), Q(1, 1, 1), Q(-1, 1, 1), col, forward);
            SQuad(mb, Q(1, 0, -1), Q(-1, 0, -1), Q(-1, 1, -1), Q(1, 1, -1), col, -forward);
            SQuad(mb, Q(1, 0, 1), Q(1, 0, -1), Q(1, 1, -1), Q(1, 1, 1), col, right);
            SQuad(mb, Q(-1, 0, -1), Q(-1, 0, 1), Q(-1, 1, 1), Q(-1, 1, -1), col, -right);
            SQuad(mb, Q(-1, 1, 1), Q(1, 1, 1), Q(1, 1, -1), Q(-1, 1, -1), top ?? col, Vector3.up);
        }
        static void SCyl(MeshBuilder mb, Vector3 a, Vector3 b, float r, Color col, int sides = 8, Color? cap = null, bool openA = false)
        {
            var ax = (b - a).normalized; var p = Vector3.Cross(ax, Mathf.Abs(ax.y) < .9f ? Vector3.up : Vector3.right).normalized; var q = Vector3.Cross(ax, p);
            for (int i = 0; i < sides; i++)
            {
                float a0 = i * Mathf.PI * 2 / sides, a1 = (i + 1) * Mathf.PI * 2 / sides;
                var o0 = (p * Mathf.Cos(a0) + q * Mathf.Sin(a0)) * r; var o1 = (p * Mathf.Cos(a1) + q * Mathf.Sin(a1)) * r;
                SQuad(mb, a + o0, a + o1, b + o1, b + o0, col, (o0 + o1).normalized);
                mb.Tint = cap ?? col; TriFace(mb, b, b + o0, b + o1, ax); if (!openA) TriFace(mb, a, a + o1, a + o0, -ax);
            }
        }
        // a collider-only mesh (saved as an asset, as every mesh of the scene)
        static Collider ColliderMesh(MeshBuilder col, string name, Transform parent)
        {
            if (col.Count == 0) return null;
            var g = new GameObject(name); g.transform.SetParent(parent, false);
            var mesh = col.ToMesh(name + " collider");
            mesh = GenAsset(mesh, $"{GeneratedDir}/Meshes/StuntPark_{name.Replace(' ', '_').Replace('(', '_').Replace(')', '_')}_{parent.childCount}.asset");
            var mc = g.AddComponent<MeshCollider>(); mc.sharedMesh = mesh; return mc;
        }
        static void SolidBox(Transform parent, Vector3 bottom, Vector3 forward, Vector3 size, string name)
        {
            var g = new GameObject(name); g.transform.SetParent(parent, false); forward.y = 0;
            g.transform.SetPositionAndRotation(bottom + Vector3.up * size.y * .5f, Quaternion.LookRotation(forward.sqrMagnitude > 1e-6f ? forward.normalized : Vector3.forward));
            g.AddComponent<BoxCollider>().size = size;
        }

        // ------------------------------------------------------------ a ramp: Racer's StuntPark.Ramp, a profile along a lane over its base (one mesh, one collider)
        // The profile's points are (along, up) from the feature's origin; each vertex stands its own height over the base under it (the floor's
        // plane, the roof, the stores' floor), so a ramp on the tilted floor follows it; the sides go down to the base (0.3 m into the floor).
        // bankEnd: the barrel-roll ramp's roll (degrees, raising the left edge about the right, eased in to the lip), as Racer's.
        static Collider ParkRamp(StuntParkPlan p, MeshBuilder vis, List<Vector2> prof, float u0, float v0, float du, float dv, float width, string look, string baseKind, Transform root, string name, float bankEnd = 0f)
        {
            var col = new MeshBuilder(); var L = Look(look);
            var dir = new Vector2(du, dv).normalized; var right = new Vector2(dir.y, -dir.x);
            float lipS = bankEnd > 0f && prof.Count > 2 ? prof[prof.Count - 2].x : 1f;
            Vector3 Edge(int i, float side)
            {
                var q = prof[i]; float b = bankEnd > 0f ? bankEnd * Mathf.SmoothStep(0, 1, Mathf.Clamp01((q.x - 1f) / Mathf.Max(lipS - 1f, .1f))) * Mathf.Deg2Rad : 0f;
                float lat = side * width * .5f, lift = 0f, dy = 0f;
                if (b != 0f) { lift = Mathf.Max(0f, Mathf.Sin(b) * width * .5f - q.y); lat = side * Mathf.Cos(b) * width * .5f; dy = lift - side * Mathf.Sin(b) * width * .5f; }
                float u = u0 + dir.x * q.x + right.x * lat, v = v0 + dir.y * q.x + right.y * lat; var w = p.W(u, v);
                return new Vector3(w.x, p.Base(baseKind, u, v) + q.y + dy, w.y);
            }
            Vector3 Foot(Vector3 e) { var q = p.UVof(new Vector2(e.x, e.z)); return new Vector3(e.x, p.Base(baseKind, q.x, q.y) - (baseKind == "floor" ? .3f : .02f), e.z); }
            var dir3 = p.Dir3(du, dv); var right3 = Vector3.Cross(Vector3.up, dir3);
            for (int i = 0; i + 1 < prof.Count; i++)
            {
                Vector3 l0 = Edge(i, -1), r0 = Edge(i, 1), l1 = Edge(i + 1, -1), r1 = Edge(i + 1, 1);
                var ds = prof[i + 1] - prof[i]; if (ds.sqrMagnitude < 1e-8f) continue;
                var outward = Vector3.Cross(l1 - l0, r0 - l0); if (outward.y < 0) outward = -outward;
                var band = Mathf.FloorToInt((prof[i].x + prof[i + 1].x) * .5f / L.band) % 2 == 0 ? L.a : L.b;
                SQuad(vis, l0, r0, r1, l1, SVary(band, l0.x, l0.z, .03f), outward); SQuad(col, l0, r0, r1, l1, band, outward);
                // the edge stripes along both sides of the top (paint, 0.3 m in)
                if (look != "dirt" || true)
                    foreach (float s in new[] { -1f, 1f })
                    {
                        var a0 = s < 0 ? l0 : r0; var a1 = s < 0 ? l1 : r1; var in0 = Vector3.Lerp(a0, s < 0 ? r0 : l0, .3f / width); var in1 = Vector3.Lerp(a1, s < 0 ? r1 : l1, .3f / width);
                        SQuad(vis, a0 + outward.normalized * .012f, a1 + outward.normalized * .012f, in1 + outward.normalized * .012f, in0 + outward.normalized * .012f, L.edge, outward);
                    }
                if (prof[i].y > .001f || prof[i + 1].y > .001f || bankEnd > 0f)
                {
                    SQuad(vis, Foot(r0), Foot(r1), r1, r0, L.side, right3); SQuad(col, Foot(r0), Foot(r1), r1, r0, L.side, right3);
                    SQuad(vis, Foot(l0), Foot(l1), l1, l0, L.side, -right3); SQuad(col, Foot(l0), Foot(l1), l1, l0, L.side, -right3);
                }
            }
            void End(int i, Vector3 facing)
            {
                var l = Edge(i, -1); var r = Edge(i, 1); if (l.y - Foot(l).y < .35f && r.y - Foot(r).y < .35f && baseKind == "floor") return;
                if (l.y - Foot(l).y < .05f && r.y - Foot(r).y < .05f) return;
                SQuad(vis, Foot(l), Foot(r), r, l, L.side, facing); SQuad(col, Foot(l), Foot(r), r, l, L.side, facing);
            }
            End(0, -dir3); End(prof.Count - 1, dir3);
            return ColliderMesh(col, "Stunt park " + name, root);
        }

        static List<Vector2> ProfOf(float[] f) => Pairs(f ?? new float[0]);

        // ------------------------------------------------------------ building the park (after the shopping center)
        static void BuildStuntPark(MapData map, TerrainGrid t, Materials mats, Transform parent, StuntParkPlan p, ShoppingCenter sc)
        {
            var root = new GameObject("Stunt park (Round 36)").transform; root.SetParent(parent, false);
            var vis = new MeshBuilder(); var props = new MeshBuilder(); var paint = new MeshBuilder();
            var signMat = StuntSignMaterial();
            int ramps = 0;
            foreach (var f in p.D.features)
            {
                switch (f.kind)
                {
                    case "ramp":
                        var c = ParkRamp(p, vis, ProfOf(f.profile), f.u, f.v, f.du, f.dv, f.width, f.look, f.@base, root, f.name, f.bank_end); ramps++;
                        if (f.bank_end > 0f) { var pr = ProfOf(f.profile); float lipS = pr[pr.Count - 2].x; var w = p.W(f.u + f.du * lipS, f.v + f.dv * lipS); p.BarrelLip = new Vector3(w.x, p.Base(f.@base, f.u + f.du * lipS, f.v + f.dv * lipS) + pr[pr.Count - 2].y, w.y); p.BarrelForward = p.Dir3(f.du, f.dv); }
                        if (f.look == "tower") TowerFrame(p, props, f);
                        break;
                    case "climb": RoofRamp(p, vis, props, f, root); ramps++; break;
                    case "loop": BuildParkLoop(p, vis, props, f, root, signMat); break;
                    case "bus": { var w = p.W(f.u, f.v); var at = new Vector3(w.x, p.FloorUVy(f.u, f.v), w.y); ParkBus(props, root, at, p.Dir3(f.du, f.dv)); break; }
                    case "junkcar": { var w = p.W(f.u, f.v); var at = new Vector3(w.x, p.FloorUVy(f.u, f.v), w.y); ParkJunkCar(props, root, at, p.Dir3(f.du, f.dv), f.k); break; }
                    case "bigjump": ParkBigJump(p, f, root); break;
                }
            }
            ParkEdge(map, p, props, root, mats, signMat);
            ParkLights(p, root, mats);
            ParkProps(p, props, root);
            ParkPaint(p, root, signMat);
            ParkPads(p, root, mats);
            ParkSigns(p, props, root, signMat);
            BuildStuntStores(map, t, mats, root, p, sc);   // MapBuilder.StuntParkStores.cs
            if (p.HasBank) BuildParkBank(map, t, mats, root, p, signMat);   // Round 38 (Part A): MapBuilder.StuntParkBank.cs
            vis.ToObject("StuntParkRamps", mats.House, root, false, true);
            props.ToObject("StuntParkProps", mats.House, root, false, true);
            // holes in the ground under the stores (their floors are slabs; MapBuilder.StuntParkStores.cs)
            ParkComponent(p, root);
            p.Log.Add($"{ramps} ramps, {p.D.features.Count(f => f.kind == "bigjump")} aimed launches off the roofs, {p.Breakables.Count} breakable sets");
            Debug.Log("[Round36] stunt park: " + string.Join("; ", p.Log));
            foreach (var l in p.D.landings ?? new string[0]) Debug.Log("[Round36] landing sizing: " + l);
        }

        // the roof ramp: up from the floor to the roof at a steady angle, rounded in and out (Racer's arcs), absolute heights; steel deck on a solid
        // body, guard rails along both sides (it stands 9 m up at its top)
        static void RoofRamp(StuntParkPlan p, MeshBuilder vis, MeshBuilder props, StuntFeature f, Transform root)
        {
            float top = p.D.roof_y; float angle = f.angle, r = f.r;
            float footU = f.u_top - f.du * 50f; SProf prof = null;
            for (int it = 0; it < 6; it++)
            {
                float y0 = p.FloorUVy(footU, f.v), rise = top - y0;
                float arcRise = 2f * r * (1f - Mathf.Cos(angle * Mathf.Deg2Rad)), arcRun = 2f * r * Mathf.Sin(angle * Mathf.Deg2Rad);
                float line = Mathf.Max(0f, (rise - arcRise) / Mathf.Sin(angle * Mathf.Deg2Rad));
                prof = new SProf(); prof.Arc(r, angle).Line(line).Arc(r, -angle).Flat(f.top_flat);
                footU = f.u_top - f.du * prof.s;
            }
            // the profile in absolute height: y over the foot's floor; the ramp's base is the floor (its sides go down to it)
            float yFoot = p.FloorUVy(footU, f.v);
            var abs = prof.pts.Select(q => new Vector2(q.x, q.y + yFoot)).ToList();
            // built with base "floor" but the heights made absolute: subtract the floor under each point
            var lane = new List<Vector2>(); foreach (var q in abs) { float u = footU + f.du * q.x; lane.Add(new Vector2(q.x, q.y - p.FloorUVy(u, f.v))); }
            ParkRamp(p, vis, lane, footU, f.v, f.du, f.dv, f.width, f.look, "floor", root, f.name);
            // guard rails: posts every 2 m and two rails, both sides, where the deck is over 1.5 m up
            var dir = new Vector2(f.du, f.dv); var right = new Vector2(dir.y, -dir.x);
            foreach (float side in new[] { -1f, 1f })
            {
                Vector3 prev = default; bool has = false;
                for (float s = 0; s <= prof.s + .01f; s += 2f)
                {
                    float y = Interp(abs, s); float u = footU + f.du * s + right.x * side * (f.width * .5f - .15f), v = f.v + f.dv * s + right.y * side * (f.width * .5f - .15f);
                    var w = p.W(u, v); var at = new Vector3(w.x, y, w.y);
                    if (y - p.FloorUVy(u, v) < 1.5f) { has = false; continue; }
                    SBox(props, at, p.Dir3(f.du, f.dv), new Vector3(.12f, 1.1f, .12f), SYellow);
                    if (has) { foreach (float h in new[] { .55f, 1.05f }) SRail(props, prev + Vector3.up * h, at + Vector3.up * h, .06f, SYellow); var mid = (prev + at) * .5f; SolidBox(root, mid, at - prev, new Vector3(.2f, 1.2f, (at - prev).magnitude + .05f), "Stunt park roof ramp rail"); }
                    prev = at; has = true;
                }
            }
            p.Log.Add($"roof ramp: {prof.s:0} m from the floor at {yFoot:0.00} m to the roof at {top:0.00} m, {angle:0} degrees");
        }
        static float Interp(List<Vector2> pts, float s) { for (int i = 1; i < pts.Count; i++) if (pts[i].x >= s) return Mathf.Lerp(pts[i - 1].y, pts[i].y, (s - pts[i - 1].x) / Mathf.Max(pts[i].x - pts[i - 1].x, 1e-4f)); return pts[pts.Count - 1].y; }
        static void SRail(MeshBuilder mb, Vector3 a, Vector3 b, float half, Color c)
        {
            var d = b - a; var f = new Vector3(d.x, 0, d.z); if (f.sqrMagnitude < 1e-6f) return; f.Normalize(); var r = Vector3.Cross(Vector3.up, f) * half; var u = Vector3.up * half;
            SQuad(mb, a - r + u, b - r + u, b + r + u, a + r + u, c, Vector3.up); SQuad(mb, a - r - u, b - r - u, b - r + u, a - r + u, c, -r); SQuad(mb, a + r - u, b + r - u, b + r + u, a + r + u, c, r);
        }
        // the tower's scaffold: posts at the corners and along the sides of its table, cross braces (the table's sides are its body)
        static void TowerFrame(StuntParkPlan p, MeshBuilder props, StuntFeature f)
        {
            var prof = ProfOf(f.profile); float ht = prof.Count > 1 ? prof[1].y : 0f; if (ht < 3f) return;
            float tableEnd = prof.Count > 2 ? prof[2].x : 0f; var dir = new Vector2(f.du, f.dv); var right = new Vector2(dir.y, -dir.x);
            for (float s = 0; s <= tableEnd + .01f; s += Mathf.Max(2f, tableEnd / Mathf.Max(1, Mathf.RoundToInt(tableEnd / 4f))))
                foreach (float side in new[] { -1f, 1f })
                {
                    float u = f.u + dir.x * s + right.x * side * (f.width * .5f + .12f), v = f.v + dir.y * s + right.y * side * (f.width * .5f + .12f);
                    var w = p.W(u, v); var y0 = p.FloorUVy(u, v);
                    SBox(props, new Vector3(w.x, y0 - .3f, w.y), p.Dir3(f.du, f.dv), new Vector3(.25f, ht + .3f + 1.1f, .25f), SYellow);
                }
        }

        // ------------------------------------------------------------ the loop (Racer's BuildLoop): a ribbon round a circle that moves sideways on the way
        // round, so the way out passes beside the way in; its inner face is the road (ArcadeVehicle holds the wheels to it: StuntPark.Loop), rails on
        // both edges, two towers outside, a beam over the top with its board
        static void BuildParkLoop(StuntParkPlan p, MeshBuilder vis, MeshBuilder props, StuntFeature lf, Transform root, Material signMat)
        {
            var o2 = p.W(lf.u, lf.v); var o = new Vector3(o2.x, p.FloorUVy(lf.u, lf.v), o2.y);
            var f = p.Dir3(lf.du, lf.dv); var side = p.Dir3(lf.side_u, lf.side_v);
            p.LoopOrigin = o; p.LoopForward = f; p.LoopSide = side;
            float r = lf.radius, w = lf.width, shift = lf.shift, thick = .3f, rail = .7f; int n = 72;
            var road = new MeshBuilder(); var rails = new MeshBuilder();
            Vector3 Centre(float phi) { float tt = phi / (2 * Mathf.PI); float sm = tt * tt * (3 - 2 * tt); return o + f * (r * Mathf.Sin(phi)) + Vector3.up * (r - r * Mathf.Cos(phi)) + side * (shift * sm); }
            Vector3 In(float phi) => (-f * Mathf.Sin(phi) + Vector3.up * Mathf.Cos(phi)).normalized;
            for (int i = 0; i < n; i++)
            {
                float a0 = i * 2 * Mathf.PI / n, a1 = (i + 1) * 2 * Mathf.PI / n;
                Vector3 c0 = Centre(a0), c1 = Centre(a1), n0 = In(a0), n1 = In(a1);
                Vector3 l0 = c0 - side * (w / 2), r0 = c0 + side * (w / 2), l1 = c1 - side * (w / 2), r1 = c1 + side * (w / 2);
                var tone = (i % 6 < 3) ? SSteel : SSteel * .92f; tone.a = 1;
                SQuad(vis, l0, r0, r1, l1, tone, (n0 + n1) * .5f); SQuad(road, l0, r0, r1, l1, tone, (n0 + n1) * .5f);
                Vector3 lo0 = l0 - n0 * thick, ro0 = r0 - n0 * thick, lo1 = l1 - n1 * thick, ro1 = r1 - n1 * thick;
                SQuad(vis, lo0, ro0, ro1, lo1, SPlyDark, -(n0 + n1)); SQuad(road, lo0, ro0, ro1, lo1, SPlyDark, -(n0 + n1));
                SQuad(vis, l0, l1, lo1, lo0, SRed, -side); SQuad(vis, r0, r1, ro1, ro0, SRed, side);
                if (a0 > .5f && a1 < 2 * Mathf.PI - .5f)
                    foreach (var (e0, e1, s) in new[] { (l0, l1, -side), (r0, r1, side) })
                    {
                        var t0 = e0 + n0 * rail; var t1 = e1 + n1 * rail; var stripe = i % 2 == 0 ? SRed : SWhite;
                        SQuad(vis, e0, e1, t1, t0, stripe, -s); SQuad(vis, e0, e1, t1, t0, stripe * .8f, s);
                        SQuad(rails, e0, e1, t1, t0, stripe, -s); SQuad(rails, e0, e1, t1, t0, stripe, s);
                    }
            }
            p.LoopCollider = ColliderMesh(road, "Stunt park loop", root); ColliderMesh(rails, "Stunt park loop rails", root);
            float lo = -w / 2 - 1.6f, hi = shift + w / 2 + 1.6f, topY = 2 * r + .5f;
            foreach (float s in new[] { lo, hi })
            {
                var at = o + side * s; SBox(props, at, f, new Vector3(.5f, topY + .4f, .5f), SSteel * .85f); SolidBox(root, at, f, new Vector3(.5f, topY + .4f, .5f), "Stunt park loop tower");
                SBox(props, at - f * 1.2f, f, new Vector3(.3f, topY * .7f, .3f), SSteel * .8f); SBox(props, at + f * 1.2f, f, new Vector3(.3f, topY * .7f, .3f), SSteel * .8f);
            }
            var mid = o + side * ((lo + hi) / 2) + Vector3.up * topY; SBox(props, mid, side, new Vector3(.4f, .4f, hi - lo + .5f), SRed);
            // THE LOOP on the beam, facing the way in (one-sided, depth tested)
            if (signMat != null) { var face = -f; SBox(props, mid + Vector3.up * .4f, side, new Vector3(.2f, 1.3f, 8.4f), SRed); AtlasPlate(root, mid + Vector3.up * 1.05f + face * .11f, -face, 8f, .625f, SignLoop, signMat); }
            p.Log.Add($"the loop: radius {r:0.0} m, {shift:0.0} m across, at {o.x:0.0}, {o.z:0.0} on a level pad");
        }

        // ------------------------------------------------------------ the aimed launches (BigJump, as the Chastain Ridge leap): every vehicle over the lip at
        // 3 m/s or more leaves at the design speed and angle, so the flight lands on its slope (make_stunt_park.py sized them)
        static void ParkBigJump(StuntParkPlan p, StuntFeature f, Transform root)
        {
            var w = p.W(f.lip_u, f.lip_v); var lip = new Vector3(w.x, p.Base(f.@base, f.lip_u, f.lip_v) + f.h, w.y);
            var g = new GameObject("Stunt park launch " + f.id); g.transform.SetParent(root, false); g.transform.position = lip;
            var bj = g.AddComponent<BigJump>(); bj.id = "park-" + f.id; bj.look = "stuntpark"; bj.lip = lip; bj.dir = p.Dir3(f.du, f.dv);
            bj.halfWidth = f.width * .5f; bj.vmin = bj.vmax = f.v_design; bj.angle = f.angle; bj.boostFrom = 3f;
            p.Log.Add($"{f.name}: {f.v_design:0} m/s at {f.angle:0} degrees off the lip at {lip.y:0.00} m");
        }

        // ------------------------------------------------------------ props (Racer's JunkCar, Bus, Container, LightPole)
        static void ParkJunkCar(MeshBuilder m, Transform root, Vector3 centre, Vector3 along, int k)
        {
            var rust = new[] { SC(.45f, .24f, .14f), SC(.30f, .34f, .36f), SC(.52f, .44f, .30f), SC(.36f, .20f, .18f), SC(.24f, .30f, .24f) }[k % 5];
            var right = Vector3.Cross(Vector3.up, along);
            SBox(m, centre + Vector3.up * .3f, along, new Vector3(1.75f, .62f, 4.3f), rust);
            SBox(m, centre + Vector3.up * .92f - along * .2f, along, new Vector3(1.55f, .5f, 2.1f), rust * .9f);
            SBox(m, centre + Vector3.up * .97f - along * .2f, along, new Vector3(1.57f, .36f, 1.9f), SC(.12f, .14f, .15f));
            foreach (float a in new[] { -1.35f, 1.35f }) foreach (float s in new[] { -.82f, .82f }) SCyl(m, centre + along * a + right * s + Vector3.up * .32f - right * .1f * Mathf.Sign(s), centre + along * a + right * s + Vector3.up * .32f + right * .12f * Mathf.Sign(s), .32f, STire, 8);
            SolidBox(root, centre, along, new Vector3(1.75f, 1.42f, 4.3f), "Stunt park junk car");
        }
        static void ParkBus(MeshBuilder m, Transform root, Vector3 back, Vector3 along)
        {
            var right = Vector3.Cross(Vector3.up, along); float len = 12, wid = 2.5f, h = 3.2f; var c = back + along * (len / 2);
            SBox(m, c + Vector3.up * .55f, along, new Vector3(wid, h - .55f, len), SYellow, SYellow * .95f);
            foreach (float s in new[] { -1f, 1f })
            {
                SBox(m, c + right * (s * (wid / 2 + .005f)) + Vector3.up * 1.95f, along, new Vector3(.02f, .85f, len - 2.2f), SC(.12f, .14f, .16f));
                foreach (float y in new[] { 1.45f, 1.0f }) SBox(m, c + right * (s * (wid / 2 + .01f)) + Vector3.up * y, along, new Vector3(.02f, .08f, len - .4f), SBlack);
            }
            SBox(m, c + along * (len / 2 + .005f) + Vector3.up * 2.0f, along, new Vector3(wid - .4f, .9f, .02f), SC(.12f, .14f, .16f));
            SBox(m, c + along * (len / 2 + .1f) + Vector3.up * .45f, along, new Vector3(wid, .3f, .2f), SBlack); SBox(m, c - along * (len / 2 + .1f) + Vector3.up * .45f, along, new Vector3(wid, .3f, .2f), SBlack);
            foreach (float a in new[] { -len / 2 + 2.2f, len / 2 - 2.6f }) foreach (float s in new[] { -1f, 1f })
                    SCyl(m, c + along * a + right * (s * (wid / 2 - .25f)) + Vector3.up * .5f, c + along * a + right * (s * (wid / 2 + .02f)) + Vector3.up * .5f, .5f, STire, 10);
            SolidBox(root, c, along, new Vector3(wid, h, len), "Stunt park school bus");
        }
        static void ParkContainer(MeshBuilder m, Transform root, Vector3 bottom, Vector3 along, Color col)
        {
            var right = Vector3.Cross(Vector3.up, along);
            SBox(m, bottom, along, new Vector3(2.44f, 2.59f, 6.06f), col, col * .9f);
            for (float a = -2.8f; a <= 2.81f; a += .4f) foreach (float s in new[] { -1f, 1f }) SBox(m, bottom + along * a + right * (s * 1.23f) + Vector3.up * .1f, along, new Vector3(.03f, 2.4f, .12f), col * .82f);
            SolidBox(root, bottom, along, new Vector3(2.44f, 2.59f, 6.06f), "Stunt park container");
        }
        static void ParkProps(StuntParkPlan p, MeshBuilder m, Transform root)
        {
            // containers in the south-east pocket (some two high), the park's colours
            var cols = new[] { SC(.70f, .16f, .12f), SC(.16f, .34f, .70f), SC(.95f, .70f, .10f), SC(.16f, .55f, .30f), SC(.92f, .45f, .08f) };
            int k = 0;
            foreach (var (u, v, two) in new[] { (3618f, 352f, true), (3618f, 358.6f, false), (3630f, 352f, false), (3630f, 358.6f, true), (3630f, 368f, false) })   // Round 38: the fifth container moved out of the bank's run-out (was 3624, 386)
            {
                var w = p.W(u, v); float y = p.FloorUVy(u, v); var along = p.Dir3(0, 1);
                ParkContainer(m, root, new Vector3(w.x, y, w.y), p.Dir3(1, 0), cols[k % cols.Length]);
                if (two) ParkContainer(m, root, new Vector3(w.x, y + 2.59f, w.y), p.Dir3(1, 0), cols[(k + 2) % cols.Length]);
                k++;
            }
        }

        // the light poles: 11 m floodlights, breakable lamps (NightLamps lights them at night, as the lots' poles); they come back with the park
        static void ParkLights(StuntParkPlan p, Transform root, Materials mats)
        {
            var lamps = new Breakables(); var centre = p.W(3540f, 410f); var dark = SC(.20f, .20f, .22f);
            var pts = Pairs(p.D.lights);
            foreach (var q in pts)
            {
                var w = p.W(q.x, q.y); float gy = p.FloorUVy(q.x, q.y); var n = (centre - w).normalized;
                var lb = lamps.Begin(w);
                lamps.Piece(() => Post(lb, new Vector3(w.x, gy - .1f, w.y), 5.6f, .22f, dark), gy + .1f);
                lamps.Piece(() => Post(lb, new Vector3(w.x, gy + 5.5f, w.y), 5.6f, .18f, dark), gy + .1f);
                lamps.Piece(() =>
                {
                    var head = new Vector3(w.x, gy + 10.9f, w.y) + new Vector3(n.x, 0f, n.y) * .7f;
                    Beam(lb, new Vector3(w.x, gy + 10.85f, w.y), head, .1f, .1f, dark);
                    BoxF(lb, MakeFrame(head, n, 1f), -1.1f, 1.1f, -.25f, .25f, -.45f, .45f, dark);
                }, gy + .1f);
                lamps.End(new Vector3(w.x, gy + 1.2f, w.y), new Vector3(n.x, 0, n.y), .7f, 2.4f);
            }
            var lr = lamps.ToObjects("StuntParkLights", mats.House, root, 1f, BreakSounds.Kind.Lamp);
            p.Breakables.AddRange(lr.GetComponentsInChildren<FenceChunk>());
        }

        // ------------------------------------------------------------ the edge: tire wall, catch fence, banners (Racer's TireWall, the fence new)
        static void ParkEdge(MapData map, StuntParkPlan p, MeshBuilder m, Transform root, Materials mats, Material signMat)
        {
            var d = p.D;
            // the barrier line, 1.2 m inside the lot's edge: the south edge, the east edge, round the north-east corner to the shop row
            var line = new List<Vector2>();
            foreach (var q in Pairs(d.lot_south)) line.Add(new Vector2(q.x, q.y + 1.2f));
            line.Add(new Vector2(3638.8f, 362f)); line.Add(new Vector2(3638.8f, 447.6f)); line.Add(new Vector2(3621.6f, 467.2f)); line.Add(new Vector2(3593f, 468.4f));
            var opens = Pairs(d.openings); var roads = RoadSegsNear(map, p.W(3540f, 400f), 220f, ri => map.roads[ri].@class != "service" || !ToPoints(map.roads[ri].points).Any(q => p.InPark(q)));
            bool Open(Vector2 uv)
            {
                for (int i = 0; i + 1 < opens.Count; i += 2) { var a = opens[i]; var b = opens[i + 1]; var ab = b - a; float t = Mathf.Clamp01(Vector2.Dot(uv - a, ab) / ab.sqrMagnitude); if (Vector2.Distance(uv, a + ab * t) < 3.5f) return true; }
                return Clearance(roads, p.W(uv.x, uv.y)) < .6f;   // never on a road
            }
            float r = .34f, hgt = 1f, fence = 3.2f; int stacks = 0, fenceRuns = 0, banners = 0;
            for (int s = 0; s + 1 < line.Count; s++)
            {
                Vector2 a = line[s], b = line[s + 1]; float len = Vector2.Distance(a, b); int n = Mathf.Max(1, Mathf.RoundToInt(len / (2 * r + .02f)));
                var runStart = -1; Vector3 prevPost = default; bool prevOk = false;
                for (int i = 0; i <= n; i++)
                {
                    var uv = Vector2.Lerp(a, b, (i + .5f) / (n + 1)); bool open = Open(uv);
                    var w = p.W(uv.x, uv.y); float y = Ground(Terrain0, w.x, w.y);
                    var c = new Vector3(w.x, y - .05f, w.y);
                    if (!open && i < n)
                    {
                        var col = i % 6 == 0 ? SRed : i % 6 == 3 ? SWhite : (i % 2 == 0 ? STire : STire * 1.25f);
                        SCyl(m, c, c + Vector3.up * hgt, r, col, 8, SC(.03f, .03f, .03f), true); stacks++;
                        // the catch fence on top: a post every 3 m, three rails, an invisible solid face (3.2 m)
                        if (i % 4 == 0)
                        {
                            var top = c + Vector3.up * (hgt + fence);
                            SBox(m, c + Vector3.up * hgt, p.Dir3(b.x - a.x, b.y - a.y), new Vector3(.1f, fence, .1f), SSteelDark);
                            if (prevOk) { foreach (float hh in new[] { hgt + .1f, hgt + fence * .5f, hgt + fence - .05f }) SRail(m, new Vector3(prevPost.x, prevPost.y - (hgt + fence) + hh, prevPost.z), new Vector3(top.x, top.y - (hgt + fence) + hh, top.z), .035f, SSteel); }
                            prevPost = top; prevOk = true;
                        }
                    }
                    else prevOk = false;
                    if (!open && runStart < 0) runStart = i;
                    if ((open || i == n) && runStart >= 0)
                    {
                        int end = open ? i - 1 : n - 1;
                        if (end > runStart)
                        {
                            var ua = Vector2.Lerp(a, b, (runStart + .5f) / (n + 1)); var ub = Vector2.Lerp(a, b, (end + .5f) / (n + 1));
                            // solid in pieces of 10 m, the tire wall and the fence over it
                            float L = Vector2.Distance(ua, ub);
                            for (float q = 0; q < L - .01f; q += 10f)
                            {
                                float e = Mathf.Min(L, q + 10f); var m0 = Vector2.Lerp(ua, ub, (q + e) * .5f / L); var wm = p.W(m0.x, m0.y); float ym = Ground(Terrain0, wm.x, wm.y);
                                SolidBox(root, new Vector3(wm.x, ym - .05f, wm.y), p.Dir3(ub.x - ua.x, ub.y - ua.y), new Vector3(2 * r + .1f, hgt + fence, e - q + .1f), "Stunt park tire wall and fence");
                            }
                            fenceRuns++;
                            // banners on the fence's inside face every 30 m (the park's name, ALL AIR)
                            if (signMat != null)
                                for (float q = 12f; q < L - 8f; q += 30f)
                                {
                                    var m0 = Vector2.Lerp(ua, ub, q / L); var wm = p.W(m0.x, m0.y); float ym = Ground(Terrain0, wm.x, wm.y);
                                    var inward = (p.W(3540f, 410f) - wm).normalized; var along = (p.W(ub.x, ub.y) - p.W(ua.x, ua.y)).normalized; var n3 = new Vector3(along.y, 0, -along.x); if (Vector3.Dot(n3, new Vector3(inward.x, 0, inward.y)) < 0) n3 = -n3;
                                    var at = new Vector3(wm.x, ym + hgt + 1.6f, wm.y) + n3 * .1f;
                                    SBox(m, at - Vector3.up * .45f - n3 * .03f, n3, new Vector3(7.4f, .9f, .04f), SBlack);
                                    AtlasPlate(root, at + n3 * .005f, -n3, 7.2f, .5625f, banners % 2 == 0 ? SignPark : SignAllAir, signMat); banners++;
                                }
                        }
                        runStart = -1;
                    }
                }
            }
            p.Log.Add($"the edge: {stacks} tire stacks, {fenceRuns} runs of catch fence, {banners} banners, openings at the lot's four drives");
        }
        static TerrainGrid Terrain0;   // the terrain, for the builders that sample it here

        // the start line and the painted lanes on the floor: decals of the sign atlas lying flat (the checkered line, chevrons pointing the way)
        static void ParkPaint(StuntParkPlan p, Transform root, Material signMat)
        {
            if (signMat == null) return;
            var st = p.D.start; var dir = p.Dir3(st[2], st[3]); var right = Vector3.Cross(Vector3.up, dir);
            Vector3 OnFloor(float u, float v, float lift = .07f) { var w = p.W(u, v); return new Vector3(w.x, p.FloorUVy(u, v) + lift, w.y); }
            // the start line: the checkered band across the lane, 12 m wide, 1 m deep
            FloorDecal(root, OnFloor(st[0] + 1.5f, st[1]), dir, right, 1.0f, 12f, SignChecker, signMat);
            // chevrons along the lines, pointing the way (the atlas' chevrons point "right": along the decal's +x)
            foreach (var (u, v, du, dv, len) in new[] { (3608f, 403.5f, -1f, 0f, 30f), (3545f, 410f, -1f, 0f, 0f), (3560f, 374f, -1f, 0f, 0f), (3518f, 356f, -1f, 0f, 14f), (3505f, 460f, 1f, 0f, 30f), (3470f, 404f, -1f, 0f, 12f) })
            {
                if (len <= 0f) continue;
                var d3 = p.Dir3(du, dv); var r3 = Vector3.Cross(Vector3.up, d3);
                FloorDecal(root, OnFloor(u, v), d3, r3, len, len / 12.8f, SignChevrons, signMat);
            }
            // SMASH IN! in front of the supermarket's windows and of the shop row, readable by a rider heading for them
            FloorDecal(root, OnFloor(p.D.front_u + 7f, 404f), p.Dir3(0, 1), p.Dir3(-1, 0), 14f, 1.1f, SignSmash, signMat, true);
            FloorDecal(root, OnFloor(3510f, p.D.row_v - 7f), p.Dir3(1, 0), p.Dir3(0, 1), 14f, 1.1f, SignSmash, signMat, true);   // Round 38: west of the pizza window's landing
        }
        // the course's paint: under every floor feature a pad in its colour (darker), 2.5 m round it, with a white border (an arena's painted zones)
        static void ParkPads(StuntParkPlan p, Transform root, Materials mats)
        {
            var mb = new MeshBuilder(); int pads = 0;
            void Pad(float u0, float v0, float du, float dv, float len, float wid, Color col)
            {
                var dir = new Vector2(du, dv).normalized; var right = new Vector2(dir.y, -dir.x);
                Vector3 P(float s, float l, float lift) { float u = u0 + dir.x * s + right.x * l, v = v0 + dir.y * s + right.y * l; var w = p.W(u, v); return new Vector3(w.x, p.FloorUVy(u, v) + lift, w.y); }
                float hw = wid * .5f;
                for (float s = -2.5f; s < len + 2.5f - .01f; s += 2f)
                {
                    float s1 = Mathf.Min(len + 2.5f, s + 2f);
                    for (float l = -hw - 2.5f; l < hw + 2.5f - .01f; l += 2f)
                    {
                        float l1 = Mathf.Min(hw + 2.5f, l + 2f);
                        mb.Tint = col; Face(mb, P(s, l, .05f), P(s1, l, .05f), P(s1, l1, .05f), P(s, l1, .05f), Vector3.up);
                    }
                    // the border's two long sides
                    mb.Tint = SWhite; Face(mb, P(s, -hw - 2.5f, .055f), P(s1, -hw - 2.5f, .055f), P(s1, -hw - 2.2f, .055f), P(s, -hw - 2.2f, .055f), Vector3.up);
                    Face(mb, P(s, hw + 2.2f, .055f), P(s1, hw + 2.2f, .055f), P(s1, hw + 2.5f, .055f), P(s, hw + 2.5f, .055f), Vector3.up);
                }
                pads++;
            }
            foreach (var f in p.D.features)
            {
                if (f.kind == "ramp" && f.@base == "floor" && f.look != "tower") { var pr = ProfOf(f.profile); var c = Look(f.look).side; Pad(f.u, f.v, f.du, f.dv, pr[pr.Count - 1].x, f.width, new Color(c.r * .8f, c.g * .8f, c.b * .8f)); }
                if (f.kind == "loop") Pad(f.u - 8f, f.v + f.side_v * f.shift * .5f, 1, 0, 16f, f.width + f.shift + 2f, SRed * .7f);
            }
            mb.ToObject("StuntParkFloorPaint", mats.Paint, root, false, false);
            p.Log.Add($"{pads} painted pads on the floor");
        }

        // a sign-atlas row lying flat on the floor: `along` is the decal's text direction, `across` its height direction (up the text)
        static void FloorDecal(Transform root, Vector3 centre, Vector3 along, Vector3 across, float w, float h, int row, Material mat, bool readable = false)
        {
            var go = new GameObject("StuntParkDecal"); go.transform.SetParent(root, false); go.transform.position = centre;
            // the quad lies flat facing up; its text runs along `along`, the text's top toward `across`
            float v1 = 1f - row * 160f / 4096f, v0 = 1f - (row + 1) * 160f / 4096f; float hw = w / 2f, hh = h / 2f;
            var mesh = new Mesh { name = "StuntParkDecal" };
            along.y = 0; across.y = 0; along.Normalize(); across.Normalize();
            mesh.vertices = new[] { -along * hw - across * hh, along * hw - across * hh, along * hw + across * hh, -along * hw + across * hh };
            mesh.uv = new[] { new Vector2(0.004f, v0 + 0.002f), new Vector2(0.996f, v0 + 0.002f), new Vector2(0.996f, v1 - 0.002f), new Vector2(0.004f, v1 - 0.002f) };
            bool up = Vector3.Cross(mesh.vertices[2] - mesh.vertices[0], mesh.vertices[1] - mesh.vertices[0]).y > 0;
            mesh.triangles = up ? new[] { 0, 2, 1, 0, 3, 2 } : new[] { 0, 1, 2, 0, 2, 3 };
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            mesh = GenAsset(mesh, $"{GeneratedDir}/Meshes/StuntParkDecal_{root.childCount}_{row}.asset");
            go.AddComponent<MeshFilter>().sharedMesh = mesh; var mr = go.AddComponent<MeshRenderer>(); mr.sharedMaterial = mat; mr.shadowCastingMode = ShadowCastingMode.Off;
        }

        // the boards (one-sided, depth tested, on posts or gantries), facing the way a rider comes
        static void ParkSigns(StuntParkPlan p, MeshBuilder m, Transform root, Material signMat)
        {
            if (signMat == null) return;
            void Board(float u, float v, float h, float faceDu, float faceDv, int row, float w, string baseKind = "floor", bool posts = true)
            {
                var wpt = p.W(u, v); float y0 = p.Base(baseKind, u, v); var face = p.Dir3(faceDu, faceDv); var right = Vector3.Cross(Vector3.up, face);
                float bh = w / 12.8f;
                var at = new Vector3(wpt.x, y0 + h, wpt.y);
                if (posts) foreach (float s in new[] { -w * .42f, w * .42f }) { SBox(m, new Vector3(wpt.x, y0 - .3f, wpt.y) + right * s - face * .1f, face, new Vector3(.22f, h + .3f + bh * .5f, .22f), SSteelDark); SolidBox(root, new Vector3(wpt.x, y0 - .3f, wpt.y) + right * s - face * .1f, face, new Vector3(.22f, h + .3f, .22f), "Stunt park sign post"); }
                SBox(m, at - Vector3.up * (bh * .5f + .12f) - face * .12f, face, new Vector3(w + .3f, bh + .24f, .1f), SBlack);
                AtlasPlate(root, at - face * .06f, -face, w, bh, row, signMat);
            }
            var d = p.D;
            Board(3633.5f, 418f, 5.2f, 1, 0, SignPark, 14f);                     // the entrance, toward Canton Road
            Board(3633.5f, 418f, 5.2f, -1, 0, SignPark, 14f, "floor", false);    // its back, toward the park (the same posts)
            Board(3510f, 365.5f, 2.2f, 1, 0, SignRoofAccess, 7f);                // the roof ramp's foot, facing east
            Board(3440f, 422f, 2.0f, -1, 0, SignBigAir, 6.4f, "roof");           // on the roof: the run-up to BIG AIR
            Board(3440f, 398f, 2.0f, -1, 0, SignBigAir, 6.4f, "roof");
            Board(3471f, 389.5f, 2.0f, -1, 0, SignBarrel, 5.2f);                 // the barrel roll, facing the way in (west)
            Board(3509f, 394.5f, 2.0f, -1, 0, SignBus, 5.2f);                    // the bus jump
            Board(3604f, 449.5f, 2.0f, 1, 0, SignCarGap, 5.2f);                  // the car gap, facing east
            Board(3621f, 409.2f, 2.6f, 0, -1, SignHalfPipe, 6.4f);               // the half-pipe's south mouth
            Board(3440f, 461f, 1.8f, -1, 0, SignTower, 5.2f, "roof");            // the tower gap, on the roof
            Board(3504f, 489.5f, 1.8f, -1, 0, SignRoofGap, 5.2f, "roof");        // the roof gap
            Board(3482f, 489.5f, 1.8f, 0, 1, SignShopDrop, 5.2f, "roof");        // the shop row drop, facing north (the run-up)
            if (p.HasBank) Board(3596.8f, 389.5f, 1.6f, 0, -1, SignBankDrop, 4.4f, "bank");   // Round 38: on the bank's roof, beside the drop's kicker (west of it), facing a rider heading north (BANK HOP: on its wall)
            // START: a gantry over the start line (two posts and the board across the lane, facing the way the riders come from the entrance)
            var st = d.start; Board(st[0] + 3f, st[1], 4.6f, 1, 0, SignStart, 10f);
        }

        // ------------------------------------------------------------ the run-time component (Assets/Scripts/Stunts/StuntPark.cs)
        static void ParkComponent(StuntParkPlan p, Transform root)
        {
            var c = root.gameObject.AddComponent<StuntPark>(); var d = p.D;
            c.outline = p.Outline.ToArray();
            var o = p.W(d.plane_u0, d.plane_v0); c.floorOrigin = new Vector3(o.x, d.plane_y, o.y);
            c.floorSlope = new Vector2(d.plane_du * p.U.x + d.plane_dv * p.V.x, d.plane_du * p.U.y + d.plane_dv * p.V.y);
            var s = p.W(d.start[0], d.start[1]); var sd = p.Dir3(d.start[2], d.start[3]);
            c.startPos = new Vector3(s.x, p.FloorUVy(d.start[0], d.start[1]) + .3f, s.y); c.startYaw = Mathf.Atan2(sd.x, sd.z) * Mathf.Rad2Deg;
            var e = p.W(d.entrance[0], d.entrance[1]); c.entrance = new Vector3(e.x, p.FloorUVy(d.entrance[0], d.entrance[1]), e.y); c.entranceRadius = d.entrance[2];
            var spots = new List<Vector3>(); var yaws = new List<float>();
            for (int i = 0; i + 3 < d.resets.Length; i += 4)
            {
                float u = d.resets[i], v = d.resets[i + 1]; var w = p.W(u, v); var dd = p.Dir3(d.resets[i + 2], d.resets[i + 3]);
                bool roof = v > d.row_v + .5f && u > 3451f && u < 3592f, inside = p.InBuilding(w);
                spots.Add(new Vector3(w.x, roof ? d.roof_y : inside ? d.front_y + .02f : p.FloorUVy(u, v), w.y)); yaws.Add(Mathf.Atan2(dd.x, dd.z) * Mathf.Rad2Deg);
            }
            var br = d.bank_roof_resets ?? new float[0];   // Round 38: on the bank's roof
            for (int i = 0; i + 3 < br.Length; i += 4) { var w = p.W(br[i], br[i + 1]); var dd = p.Dir3(br[i + 2], br[i + 3]); spots.Add(new Vector3(w.x, d.bank_roof, w.y)); yaws.Add(Mathf.Atan2(dd.x, dd.z) * Mathf.Rad2Deg); }
            c.resetSpots = spots.ToArray(); c.resetYaws = yaws.ToArray();
            c.loopCollider = p.LoopCollider; c.loopOrigin = p.LoopOrigin; c.loopForward = p.LoopForward; c.loopSide = p.LoopSide;
            var lf = d.features.FirstOrDefault(f => f.kind == "loop"); if (lf != null) { c.loopRadius = lf.radius; c.loopShift = lf.shift; c.loopWidth = lf.width; }
            c.barrelLip = p.BarrelLip; c.barrelForward = p.BarrelForward; c.roofY = d.roof_y; c.storeFloorY = d.front_y; c.bankRoofY = p.HasBank ? d.bank_roof : 0f;
            c.breakables = p.Breakables.Where(b => b).Distinct().ToArray();
            // Round 38 (Part D): the rivals' lines and their places on the start line
            c.aiLines = (d.ai_lines ?? new StuntAiLineData[0]).Select(l => new StuntPark.AiLine { id = l.id, next = l.next, weights = l.weights, speeds = l.speeds,
                points = Enumerable.Range(0, l.points.Length / 3).Select(i => new Vector3(l.points[3 * i], l.points[3 * i + 1], l.points[3 * i + 2])).ToArray() }).ToArray();
            var grid = Pairs(d.ai_grid ?? new float[0]); c.aiGrid = grid.Select(q => { var uv = p.UVof(q); return new Vector3(q.x, p.FloorUVy(uv.x, uv.y) + .3f, q.y); }).ToArray();
            p.Log.Add($"{c.aiLines.Length} rival lines ({c.aiLines.Sum(l => l.points.Length)} points), {c.aiGrid.Length} rival places on the start line");
        }

        // ------------------------------------------------------------ the sign atlas (Tools/handmade/make_stunt_signs.py; its row order)
        const int SignPark = 0, SignBigAir = 1, SignRoofAccess = 2, SignSmash = 3, SignLoop = 4, SignBarrel = 5, SignHalfPipe = 6, SignTower = 7, SignRoofGap = 8, SignBus = 9,
                  SignCarGap = 10, SignStart = 11, SignChevrons = 12, SignChecker = 13, SignAllAir = 14, SignShopDrop = 15, SignDropIn = 16, SignUpToRoof = 17, SignChevronsRed = 18, SignBankHop = 19, SignBankDrop = 20;
        static Material StuntSignMaterial()
        {
            const string path = "Assets/HandMade/StuntPark/StuntSigns.png";
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (tex == null) { Debug.LogWarning("[Round36] " + path + " missing (Tools/handmade/make_stunt_signs.py): no boards"); return null; }
            // lit and depth-tested (hidden by anything in front), one-sided: the store signs' material
            var m = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "StuntSigns" };
            m.SetTexture("_BaseMap", tex); m.mainTexture = tex; m.SetColor("_BaseColor", Color.white); m.SetFloat("_Smoothness", 0.2f); m.SetFloat("_Cull", (float)CullMode.Back);
            m = GenAsset(m, GeneratedDir + "/StuntSigns.mat");
            return m;
        }
    }
}
