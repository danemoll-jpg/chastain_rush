using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

// Round 52 (B, BUG-001 to BUG-003 of session 2026-10-19_19-45-10-079_0a133b, Dan, from Free Roam in the Round 51 exe): houses on the steep lots
// of the new Addison Heights lap. The other neighborhoods' houses (MapBuilder.Neighborhoods.cs) stand at the ground at the foot of their garage
// doors and nothing round them is ever graded (Round 27's settling runs for the Glen and the communities only), so on these hills a house runs into
// the slope behind it (BUG-001's left house, BUG-002), its drive is the map's straight line draped on the bank at any grade, the ground bulging
// through it (BUG-003: 37 % up to house 1135), and its front door stands over the falling ground with no steps (BUG-003). Style E (the split-level)
// also had its upstairs back windows drawn across its one-story wing, over the wing's roof (BUG-001's right house).
// For Dan's houses and the houses that face the lap (Claude's scope, said in TODO.md), and nowhere else:
//   - the floor is set where a drive from the street can reach it at no more than DriveGrade (lowered into the hill, or raised over a downhill lot)
//   - the drive runs from the street's edge straight to the garage doors and the ground under it is graded to it (cut and fill, banks either side)
//   - the lot is cut down round the house as Round 27's, but the yard falls to it gently (about 1 in 3), not in a 40 degree bank (cut only,
//     never on a street, never under another building)
//   - the front door gets a stoop and steps down to the ground; style E's upstairs back and side windows only on its two-story block
// The list goes to Data/screenshots/round52/houses_settled.txt.
namespace ChastainRush
{
    public static partial class MapBuilder
    {
        static readonly HashSet<int> R52DanHouses = new HashSet<int> { 910, 996, 615, 1135, 1559 };   // BUG-001 (left 910, right 996), BUG-002, BUG-003; Round 81 G (BUG-004 x 1198 z 1670, Bells Ferry Road: "buried house") 1559
        const float R52DriveGrade = 0.20f;    // game grade (vertical_scale 1.5: about 13 % in the real world)
        static List<Vector2> r52Lap;
        static List<(Vector2 a, Vector2 b, float half)> r52Roads;
        static List<(List<Vector2> ring, Rect box)> r52Buildings;
        static CellIndex r52RoadIdx, r52BuildingIdx; static readonly List<int> r52Near = new List<int>();   // build speed (after Round 61): the two lists by cell, in their order
        static readonly List<string> R52Log = new List<string>();
        [Serializable] class R52RouteJson { public float[] loop; }
        // the race's cleared line (Data/tracks/addison_heights.json): no tree trunk within 1.5 m of the lap's road edge (Round 52 check: AI drivers coming
        // down Embry Lane's 30 % hill hit trunks standing in the gutter); read with the other races' track files, used by the Trees step
        const string AddisonTrackFile = "Data/tracks/addison_heights.json";
        static Ridge AddisonNow;

        static void R52Load(MapData map)
        {
            if (r52Roads != null) return;
            r52Lap = new List<Vector2>();
            string path = Path.Combine(ProjectRoot, "Data/routes/addison_heights.json");
            if (File.Exists(path)) r52Lap = ToPoints(JsonUtility.FromJson<R52RouteJson>(File.ReadAllText(path)).loop);
            r52Roads = new List<(Vector2, Vector2, float)>();
            foreach (var r in map.roads)
            {
                if (r.@class == "motorway" || r.@class == "motorway_link") continue;
                var p = ToPoints(r.points); for (int i = 0; i + 1 < p.Count; i++) r52Roads.Add((p[i], p[i + 1], r.width * 0.5f));
            }
            r52Buildings = map.buildings.Select(b => { var ring = ToPoints(b.footprint); float x0 = ring.Min(q => q.x), x1 = ring.Max(q => q.x), z0 = ring.Min(q => q.y), z1 = ring.Max(q => q.y); return (ring, Rect.MinMaxRect(x0, z0, x1, z1)); }).ToList();
            r52RoadIdx = new CellIndex(64f); foreach (var (a, b, _) in r52Roads) r52RoadIdx.Add(a, b, 0f);
            r52BuildingIdx = new CellIndex(32f); foreach (var (_, box) in r52Buildings) r52BuildingIdx.Add(box.xMin, box.yMin, box.xMax, box.yMax);   // ids are the building indices
            R52Log.Clear();
        }

        // how far p is outside the nearest road's carriageway (negative: on it)
        static float R52RoadClear(Vector2 p)
        {
            float best = float.MaxValue;
            foreach (int ri in r52RoadIdx.Near(p, 30.01f, r52Near))   // (a minimum: the same whatever the order)
            {
                var (a, b, half) = r52Roads[ri];
                if (Mathf.Min(a.x, b.x) - 30f > p.x || Mathf.Max(a.x, b.x) + 30f < p.x || Mathf.Min(a.y, b.y) - 30f > p.y || Mathf.Max(a.y, b.y) + 30f < p.y) continue;
                best = Mathf.Min(best, SegDistStatic(p, a, b) - half);
            }
            return best;
        }

        static bool R52NearOtherBuilding(Vector2 p, int self, float margin)
        {
            foreach (int j in r52BuildingIdx.Near(p, margin + 0.01f, r52Near))
            {
                if (j == self) continue; var (ring, box) = r52Buildings[j];
                if (p.x < box.xMin - margin || p.x > box.xMax + margin || p.y < box.yMin - margin || p.y > box.yMax + margin || ring.Count < 3) continue;
                if (InPolygon(p, ring) || DistToEdgesStatic(p, ring) < margin) return true;
            }
            return false;
        }

        // Dan's houses, and a house whose own street point lies on the lap (its front faces the lap)
        static bool R52Target(MapData map, int idx, Vector2 streetPoint, Vector2 centre)
        {
            R52Load(map);
            if (R52DanHouses.Contains(idx)) return true;
            // Round 78 (F, Dan's BUG-005 x 1850.19 z -8843.93, a house south-east of Marietta Square: "unbury this house"): every house in stage 3's land
            // (the map's pieces outside extent_m) is settled the same way
            if (map.pieces != null) for (int k = 0; k + 3 < map.pieces.Length; k += 4) if (centre.x >= map.pieces[k] && centre.y >= map.pieces[k + 1] && centre.x <= map.pieces[k + 2] && centre.y <= map.pieces[k + 3]) return true;
            if (r52Lap.Count < 2) return false;
            float dStreet = float.MaxValue, dHouse = float.MaxValue;
            for (int i = 0; i + 1 < r52Lap.Count; i++) { dStreet = Mathf.Min(dStreet, SegDistStatic(streetPoint, r52Lap[i], r52Lap[i + 1])); dHouse = Mathf.Min(dHouse, SegDistStatic(centre, r52Lap[i], r52Lap[i + 1])); }
            return dStreet < 8f && dHouse < 45f;
        }

        // called from LoadRest's Dress once the house's frame, garage and floor are set (before its base and the settling flags)
        static void Round52Settle(MapData map, TerrainGrid terrain, HouseSpec h)
        {
            h.r52 = true;
            var F = new Vector2(h.frame.F.x, h.frame.F.z).normalized;
            float gMid = (h.garageU0 + h.garageU1) / 2f, gWidth = Mathf.Abs(h.garageU1 - h.garageU0);
            var G3 = h.frame.P(gMid, 0f, h.D / 2f); var G = new Vector2(G3.x, G3.z);   // the garage doors' middle
            float floor0 = h.floorY;
            // the street's edge straight out from the garage doors
            Vector2? R = null;
            for (float t = 0.5f; t < 45f; t += 0.25f) { var p = G + F * t; if (R52RoadClear(p) < 0.15f) { R = p; break; } }
            // the map's drives that run up to this house: re-run from the doors (or dropped, if a new one is made)
            var mine = new List<int>();
            for (int di = 0; di < map.driveways.Length; di++)
            {
                var dp = ToPoints(map.driveways[di].points); if (dp.Count < 2) continue;
                foreach (var e in new[] { dp[0], dp[dp.Count - 1] }) if (InPolygon(e, h.ring) || DistToEdgesStatic(e, h.ring) < 3.5f) { mine.Add(di); break; }
            }
            string driveNote = "no street in front of the garage within 45 m: drive left as it was";
            if (R.HasValue)
            {
                var Rv = R.Value; float L = (Rv - G).magnitude, yR = terrain.Height(Rv.x, Rv.y);
                float g0 = h.floorY - 0.16f;   // the ground at the garage doors
                float g1 = Mathf.Clamp(g0, yR - L * R52DriveGrade, yR + L * R52DriveGrade);
                h.floorY = g1 + 0.16f;
                // the lot cut down round the house first, then the drive graded (it wins next to the garage)
                CutLotR52(terrain, h);
                float width = Mathf.Clamp(Mathf.Max(3.2f, gWidth - 0.4f), 3.2f, 6f), hw = width / 2f + 0.4f;
                GradeDriveR52(terrain, h, G, Rv, g1, yR, hw);
                // the drive itself: from just inside the doors to just into the street
                var a = G - F * 0.2f; var b = Rv + F * 0.6f;
                if (mine.Count > 0)
                {
                    map.driveways[mine[0]].points = new[] { a.x, a.y, b.x, b.y }; map.driveways[mine[0]].width = width;
                    for (int k = 1; k < mine.Count; k++) map.driveways[mine[k]].points = new float[0];
                }
                else map.driveways = map.driveways.Concat(new[] { new Driveway { points = new[] { a.x, a.y, b.x, b.y }, width = width, source = "generated" } }).ToArray();
                driveNote = $"drive {L:0.0} m, {(g1 - yR) / Mathf.Max(L, 0.1f) * 100f:+0;-0} % (was {(g0 - yR) / Mathf.Max(L, 0.1f) * 100f:+0;-0} %)" + (mine.Count > 1 ? $", {mine.Count - 1} more dropped" : "") + (mine.Count == 0 ? ", new" : "");
            }
            else CutLotR52(terrain, h);
            R52Log.Add($"{h.index,5}  x {h.footprintCentre.x,7:0}  z {h.footprintCentre.y,6:0}  style {h.style}{(R52DanHouses.Contains(h.index) ? "  (Dan's)" : "")}  floor {floor0:0.00} -> {h.floorY:0.00}  {driveNote}");
        }

        static bool R52Hold(Vector2 p, HouseSpec h, float roadGap) => R52RoadClear(p) < roadGap || R52NearOtherBuilding(p, h.index, 1.5f);

        // Round 27's CutLotGuarded for these houses: down to just under the floor 2 m round the house, a slope of about 1 in 3 back up to the
        // natural ground; cut only; never within 3 m of a street's edge or 1.5 m of another building
        static void CutLotR52(TerrainGrid terrain, HouseSpec h)
        {
            float pad = h.floorY - 0.12f;
            var r = new Vector2(h.frame.R.x, h.frame.R.z); var f = new Vector2(h.frame.F.x, h.frame.F.z);
            float hu = h.W / 2 + 2.0f, hv = h.D / 2 + 2.0f;
            // Round 52 check: Round 27's 40 degree bank 1.4 m off the wall still read as a house sunk in a trench from the yards and the street;
            // a yard that falls gently to the house (about 1 in 3) shows its walls
            const float Bank = 0.33f, Reach = 14f;
            var c = h.footprintCentre; float ext = hu + hv + Reach;
            EditTerrain(terrain, Rect.MinMaxRect(c.x - ext, c.y - ext, c.x + ext, c.y + ext), (x, z, y) =>
            {
                var p = new Vector2(x, z); var d = p - c;
                float ou = Mathf.Max(0f, Mathf.Abs(Vector2.Dot(d, r)) - hu), ov = Mathf.Max(0f, Mathf.Abs(Vector2.Dot(d, f)) - hv);
                float allowed = pad + Mathf.Sqrt(ou * ou + ov * ov) * Bank;
                if (y <= allowed + 1e-3f || R52Hold(p, h, 3f)) return y;
                return allowed;
            });
        }

        // the drive's ground: a straight grade from the garage doors (g1) to the street's edge (yR), cut and filled, with banks of about 40 degrees
        // either side back to the ground as it is
        static void GradeDriveR52(TerrainGrid terrain, HouseSpec h, Vector2 G, Vector2 R, float g1, float yR, float hw)
        {
            var dir = R - G; float L = dir.magnitude; if (L < 0.5f) return; dir /= L; var n = new Vector2(-dir.y, dir.x);
            const float Bank = 0.85f, BankW = 5f;
            float x0 = Mathf.Min(G.x, R.x) - hw - BankW, x1 = Mathf.Max(G.x, R.x) + hw + BankW, z0 = Mathf.Min(G.y, R.y) - hw - BankW, z1 = Mathf.Max(G.y, R.y) + hw + BankW;
            EditTerrain(terrain, Rect.MinMaxRect(x0, z0, x1, z1), (x, z, y) =>
            {
                var p = new Vector2(x, z); var d = p - G;
                float t = Vector2.Dot(d, dir), lat = Mathf.Abs(Vector2.Dot(d, n));
                if (t < -0.3f || t > L + 0.3f || lat > hw + BankW) return y;
                if (R52RoadClear(p) < 0f || InPolygon(p, h.ring) || R52NearOtherBuilding(p, h.index, 1.0f)) return y;
                float yd = Mathf.Lerp(g1, yR, Mathf.Clamp01(t / L));
                if (lat <= hw) return yd;
                float k = (lat - hw) * Bank;
                return Mathf.Clamp(y, yd - k, yd + k);
            });
        }

        // the front door's stoop and steps down to the ground in front of it (Round 52 houses only); y in the frame is from the floor
        static void StoopR52(MeshBuilder mb, Frame f, float u0, float u1, float v, HouseSpec h)
        {
            if (!h.r52 || Terrain0 == null) return;
            var at = f.P((u0 + u1) / 2f, 0f, v + 1.0f);
            float drop = h.floorY - Terrain0.Height(at.x, at.z);
            if (drop < 0.25f) return;
            var c = new Color(0.66f, 0.64f, 0.61f);
            BoxF(mb, f, u0, u1, -drop - 0.3f, -0.12f, v, v + 1.0f, c);   // the stoop under the doorstep
            int n = Mathf.Min(24, Mathf.CeilToInt(drop / 0.18f)); float rise = drop / n;
            for (int k = 1; k < n; k++)
            {
                float top = -k * rise, va = v + 1.0f + (k - 1) * 0.3f;
                var q = f.P((u0 + u1) / 2f, 0f, va + 0.15f);
                float ground = Terrain0.Height(q.x, q.z) - h.floorY;
                if (top < ground + 0.05f) break;   // the ground has come up to meet the steps
                BoxF(mb, f, u0, u1, ground - 0.3f, top, va, va + 0.3f, c);
            }
        }

        static void Round52Report()
        {
            if (R52Log.Count == 0) return;
            var sb = new StringBuilder();
            sb.AppendLine("Round 52 (B): Dan's three spots and the houses facing the Addison Heights lap (MapBuilder.Round52.cs). Each: floor before -> after,");
            sb.AppendLine($"its drive from the street's edge to the garage doors (grade, + = up to the house; at most {R52DriveGrade * 100:0} % in the game). Lot cut round each,");
            sb.AppendLine("a stoop and steps at each front door; style E's upstairs back windows only over its two-story block.");
            sb.AppendLine($"{R52Log.Count} houses.");
            sb.AppendLine();
            foreach (var l in R52Log) sb.AppendLine(l);
            string dir = Path.Combine(ProjectRoot, "Data/screenshots/round52"); Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, "houses_settled.txt"), sb.ToString());
            Debug.Log($"[Round52] {R52Log.Count} houses settled on the Addison Heights lap; list in Data/screenshots/round52/houses_settled.txt");
            r52Roads = null;
        }
    }
}
