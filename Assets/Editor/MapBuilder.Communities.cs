using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// Round 22: the rest of the community, dressed the way Chastain Glen was in Round 7. Grasmere Court (part of Chastain Glen),
// Chastain Commons (Weybourne Court and its side streets) and Chastain Enclave (Rotherhithe Landing, Bloombridge Way and their
// side streets) are listed in Data/neighborhood/communities.json (Tools/mapgen/communities_area.py). Their streets and houses
// join the Chastain Glen area (MapBuilder.Neighborhood.cs), so they get the same houses, drives, curbs, sidewalks, mailboxes,
// lamps, hydrants, fences and breakables. Here: each community's own mix of the four house styles (the Enclave's houses are
// larger: grown where the lot allows, more brick, three-car garages), the Enclave's pool and tennis courts (after Dan's
// pictures in local/reference_photos/enclave/, reference only), and a brick monument at the Commons' and the Enclave's entrance.
namespace ChastainRush
{
    public static partial class MapBuilder
    {
        [Serializable] class CommunitiesJson { public CommunityJson[] communities; }
        [Serializable] class CommunityJson { public string key, name; public int[] roads; public int[] houses; public EntranceJson[] entrances; public AmenitiesJson amenities; }
        [Serializable] class EntranceJson { public float x, z, dx, dz, width; public int road; }
        [Serializable] class AmenitiesJson { public PolyItem pool; public PolyItem[] courts; public int clubhouse_building = -1, gazebo_building = -1; }

        // Chastain Glen's rectangle and one per community (one rectangle round them all would take in the streets between)
        sealed class AreaBounds
        {
            public readonly Rect Glen;
            public readonly List<Rect> Parts = new List<Rect>();
            public AreaBounds(Rect glen) { Glen = glen; Parts.Add(glen); }
            public bool Contains(Vector2 p) { foreach (var r in Parts) if (r.Contains(p)) return true; return false; }
        }

        // the Enclave's amenity block: the pool deck (in the pool's frame, with the clubhouse on it), the tennis court block, the
        // pavilion, and a small parking lot off the street
        sealed class EnclaveAmenity
        {
            public List<Vector2> PoolRing;
            public Vector2 DeckC, DU, DV; public float DeckHU, DeckHV, DeckY;     // deck rectangle (centre, axes, half sizes), its level
            public List<List<Vector2>> Courts = new List<List<Vector2>>();
            public Vector2 CourtC, CU, CV; public float CourtHU, CourtHV, CourtY; // the fenced court block, CU along the courts' length
            public int Clubhouse = -1, Pavilion = -1;
            public Vector2 ClubC, ClubFront, ClubU; public float ClubW, ClubD;
            public List<Vector2> Lot, LotDrive;                                  // the lot's corners, the drive from the street into it
            public Vector2 PavC; public float PavR;                              // the pavilion (a keep-out for trees and fences)
            public List<Vector2> Walk; public int WalkSide = -1; public Vector2 Gate, WalkF0, WalkD, WalkO; public float WalkT0, WalkT1, WalkGap;                 // Round 23 follow-ups (BUG-002): the sidewalk between the pool fence and the lot, the fence side it is on
            public bool InRect(Vector2 p, Vector2 c, Vector2 u, Vector2 v, float hu, float hv, float m) { var d = p - c; return Mathf.Abs(Vector2.Dot(d, u)) < hu + m && Mathf.Abs(Vector2.Dot(d, v)) < hv + m; }
            public bool Blocks(Vector2 p, float m) => InRect(p, DeckC, DU, DV, DeckHU, DeckHV, m) || InRect(p, CourtC, CU, CV, CourtHU, CourtHV, m)
                                                       || (Lot != null && (InPolygon(p, Lot) || DistToEdges(p, Lot) < m)) || (LotDrive != null && DistSeg(p, LotDrive[0], LotDrive[LotDrive.Count - 1]) < 3f + m)
                                                       || (PavR > 0f && (p - PavC).magnitude < PavR + m);
            static float DistSeg(Vector2 p, Vector2 a, Vector2 b) { var ab = b - a; float t = ab.sqrMagnitude < 1e-6f ? 0f : Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude); return (a + ab * t - p).magnitude; }
        }

        sealed partial class Neighborhood
        {
            public CommunitiesJson Communities;
            public int EnclaveCommunity = -1, CommonsCommunity = -1, GrasmereCommunity = -1;
            readonly Dictionary<int, int> houseCommunity = new Dictionary<int, int>();
            readonly HashSet<int> communityRoadSet = new HashSet<int>();
            readonly List<Vector2> communityRoadPts = new List<Vector2>();
            public readonly List<(HouseSpec h, float buried)> CommunityBuried = new List<(HouseSpec, float)>();
            public int GrownHouses;
            public EnclaveAmenity Amenity;
            public readonly List<(int community, Vector2 p, Vector2 face)> CommunityMonuments = new List<(int, Vector2, Vector2)>();
            public readonly StringBuilder22 Log22 = new StringBuilder22();

            public string CommunityName(int c) => c <= 0 || Communities == null ? "Chastain Glen" : Communities.communities[c - 1].name;
            int CommunityOf(int idx) => houseCommunity.TryGetValue(idx, out int c) ? c : 0;
            public bool AmenityBlocks(Vector2 p, float margin) => Amenity != null && Amenity.Blocks(p, margin);

            // ---------------------------------------------------------------- load

            void LoadCommunities()
            {
                string path = Path.Combine(ProjectRoot, "Data/neighborhood/communities.json");
                if (!File.Exists(path)) { Debug.LogWarning("[Round22] no Data/neighborhood/communities.json (run Tools/mapgen/communities_area.py): only Chastain Glen is dressed"); return; }
                Communities = JsonUtility.FromJson<CommunitiesJson>(File.ReadAllText(path));
                var roads = new List<int>(Area.roads); var houses = new List<int>(Area.houses);
                var glenRoads = new HashSet<int>(Area.roads); var glenHouses = new HashSet<int>(Area.buildings ?? Area.houses);
                for (int i = 0; i < Communities.communities.Length; i++)
                {
                    var cm = Communities.communities[i]; int id = i + 1;
                    if (cm.key == "enclave") EnclaveCommunity = id;
                    if (cm.key == "commons") CommonsCommunity = id;
                    if (cm.key == "grasmere") GrasmereCommunity = id;
                    foreach (int r in cm.roads) if (!glenRoads.Contains(r) && communityRoadSet.Add(r)) roads.Add(r);
                    foreach (int h in cm.houses) if (!glenHouses.Contains(h) && !houseCommunity.ContainsKey(h)) { houseCommunity[h] = id; houses.Add(h); }
                }
                foreach (int r in communityRoadSet) communityRoadPts.AddRange(ToPoints(map.roads[r].points));
                Area.roads = roads.ToArray(); Area.houses = houses.ToArray();
                Debug.Log("[Round22] communities: " + string.Join(", ", Communities.communities.Select((c, i) => $"{c.name} {c.roads.Length} street pieces, {houseCommunity.Count(kv => kv.Value == i + 1)} houses")));
            }

            void AddCommunityBounds()
            {
                if (Communities == null) return;
                foreach (var cm in Communities.communities)
                {
                    float x0 = float.MaxValue, x1 = float.MinValue, z0 = float.MaxValue, z1 = float.MinValue;
                    foreach (int r in cm.roads) foreach (var p in ToPoints(map.roads[r].points)) { x0 = Mathf.Min(x0, p.x); x1 = Mathf.Max(x1, p.x); z0 = Mathf.Min(z0, p.y); z1 = Mathf.Max(z1, p.y); }
                    if (x0 < x1) Bounds.Parts.Add(Rect.MinMaxRect(x0 - 40f, z0 - 40f, x1 + 40f, z1 + 40f));
                }
            }

            // a road that is not one of the communities' streets but meets one (New Chastain Road at an entrance)
            bool TouchesCommunityRoad(Road r)
            {
                if (communityRoadPts.Count == 0 || r.points == null) return false;
                for (int k = 0; k + 1 < r.points.Length; k += 2)
                {
                    var p = new Vector2(r.points[k], r.points[k + 1]);
                    if (!Bounds.Contains(p)) continue;
                    foreach (var q in communityRoadPts) if ((q - p).sqrMagnitude < 0.36f) return true;
                }
                return false;
            }

            // ---------------------------------------------------------------- houses

            HouseStyle CommunityStyle(int community, float area, float longSide, float shortSide, int seed)
            {
                if (community == GrasmereCommunity) return ChooseStyle(area, longSide, shortSide, seed);   // part of Chastain Glen: its own rule
                float aspect = longSide / Mathf.Max(shortSide, 1f);
                if (community == EnclaveCommunity)
                {
                    // larger houses: no ranches, mostly Dan's style and the colonials, a few plain two-story houses
                    float r = Hash01(seed, 341);
                    if (area >= 175f) return r < 0.5f ? HouseStyle.B : HouseStyle.A;
                    return r < 0.85f ? HouseStyle.A : HouseStyle.D;
                }
                // Chastain Commons: the same styles, its own mix (more plain two-story houses and ranches, fewer colonials)
                float q = Hash01(seed, 331);
                if (aspect >= 1.42f && area < 160f) return HouseStyle.C;
                if (area >= 168f) return q < 0.4f ? HouseStyle.B : HouseStyle.A;
                if (q < 0.45f) return HouseStyle.A;
                if (q < 0.75f) return HouseStyle.D;
                return HouseStyle.C;
            }

            void DressForCommunity(HouseSpec h)
            {
                int s = h.seed;
                if (h.community == CommonsCommunity)
                {
                    // its own colors from the same palettes (another draw), so the street does not read as a copy of Chastain Glen
                    h.sidingC = h.style == HouseStyle.D && Hash01(s, 342) < 0.4f ? PickOf(YellowSiding, s, 343) : PickOf(SidingPalette, s, 344);
                    h.roofC = PickOf(ShingleRoofs, s, 345);
                    h.shutterC = PickOf(ShutterColours, s, 346);
                    h.brick = h.style == HouseStyle.B || (h.style == HouseStyle.A && Hash01(s, 347) < 0.4f) || (h.style == HouseStyle.C && Hash01(s, 348) < 0.35f);
                    h.entryGable = h.style == HouseStyle.A && Hash01(s, 349) < 0.45f;
                }
                else if (h.community == EnclaveCommunity)
                {
                    // brick fronts on most, three-car garages where the facade is wide enough, chimneys on more of them
                    h.brick = h.style == HouseStyle.B || (h.style == HouseStyle.A && Hash01(s, 351) < 0.75f);
                    h.entryGable = h.style == HouseStyle.A && Hash01(s, 352) < 0.7f;
                    h.chimney = Hash01(s, 353) < 0.6f;
                    if ((h.style == HouseStyle.A && h.W >= 14.8f || h.style == HouseStyle.B && h.W >= 15.5f) && Hash01(s, 354) < 0.75f) h.garageBays = 3;
                }
                if (h.brick && h.style == HouseStyle.A && h.sidingC.grayscale < 0.5f) h.sidingC = SidingPalette[0];
            }

            List<(Vector2 c, List<Vector2> ring)> rawRings;

            // Round 22 (Dan: "Enclave tends to have larger houses"): the footprint's rectangle is grown by up to 14% each way, as
            // far as it stays 4 m from every other house's footprint and 5 m from the street's edge
            void GrowEnclaveHouse(int idx, ref List<Vector2> ring, Vector2 c, Vector2 U, Vector2 V, ref float hl, ref float hw)
            {
                if (rawRings == null)
                {
                    rawRings = new List<(Vector2, List<Vector2>)>();
                    foreach (int k in Area.houses) { var r = ToPoints(map.buildings[k].footprint); rawRings.Add((RingCentroid(r), r)); }
                }
                var self = RingCentroid(ToPoints(map.buildings[idx].footprint));
                for (float g = 1.14f; g > 1.001f; g -= 0.02f)
                {
                    float L = hl * g, Wd = hw * g; bool ok = true;
                    var corners = new List<Vector2> { c + U * L + V * Wd, c - U * L + V * Wd, c - U * L - V * Wd, c + U * L - V * Wd };
                    for (int e = 0; e < 4 && ok; e++)
                        for (float t = 0f; t <= 1.001f && ok; t += 0.125f)
                        {
                            var p = Vector2.Lerp(corners[e], corners[(e + 1) % 4], t);
                            if (RoadClearance(p) < 5f) ok = false;
                            foreach (var o in rawRings)
                            {
                                if (!ok) break;
                                if ((o.c - self).sqrMagnitude < 0.01f || (o.c - p).sqrMagnitude > 900f) continue;
                                if (InPolygon(p, o.ring) || DistToRing(p, o.ring) < 4f) ok = false;
                            }
                        }
                    if (!ok) continue;
                    hl = L; hw = Wd; ring = corners; GrownHouses++;
                    return;
                }
            }

            // ---------------------------------------------------------------- planning (before the fences, drives' owners and trees)

            void PlanCommunities()
            {
                if (Communities == null) return;
                PlanEnclaveAmenity();
                PlanCommunityMonuments();
            }

            void PlanEnclaveAmenity()
            {
                if (EnclaveCommunity < 0) return;
                var am = Communities.communities[EnclaveCommunity - 1].amenities;
                if (am == null || am.pool == null || am.pool.outline == null || am.pool.outline.Length < 6) { Debug.LogWarning("[Round22] the Enclave's pool is not in communities.json: no amenity block"); return; }
                var a = new EnclaveAmenity { PoolRing = ToPoints(am.pool.outline) };
                if (a.PoolRing.Count > 1 && (a.PoolRing[0] - a.PoolRing[a.PoolRing.Count - 1]).sqrMagnitude < 1e-4f) a.PoolRing.RemoveAt(a.PoolRing.Count - 1);
                if (PolyArea(a.PoolRing) < 0f) a.PoolRing.Reverse();
                BestRectangle(a.PoolRing, out Vector2 pc, out Vector2 pu, out Vector2 pv, out float phl, out float phw);
                // the deck: 3.5 m round the pool, grown to take in the clubhouse's footprint
                float u0 = -phl - 3.5f, u1 = phl + 3.5f, v0 = -phw - 3.5f, v1 = phw + 3.5f;
                if (am.clubhouse_building >= 0)
                {
                    a.Clubhouse = am.clubhouse_building; Replaced.Add(a.Clubhouse);
                    var cr = ToPoints(map.buildings[a.Clubhouse].footprint);
                    BestRectangle(cr, out a.ClubC, out var cu, out var cv, out float chl, out float chw);
                    var toPool = (pc - a.ClubC).normalized;
                    a.ClubFront = new[] { cu, -cu, cv, -cv }.OrderByDescending(d => Vector2.Dot(d, toPool)).First();
                    a.ClubU = new Vector2(a.ClubFront.y, -a.ClubFront.x);
                    bool frontAlongU = Mathf.Abs(Vector2.Dot(a.ClubFront, cu)) > 0.7f;
                    a.ClubW = 2f * (frontAlongU ? chw : chl); a.ClubD = 2f * (frontAlongU ? chl : chw);
                    foreach (var p in cr) { var d = p - pc; float uu = Vector2.Dot(d, pu), vv = Vector2.Dot(d, pv); u0 = Mathf.Min(u0, uu - 0.5f); u1 = Mathf.Max(u1, uu + 0.5f); v0 = Mathf.Min(v0, vv - 0.5f); v1 = Mathf.Max(v1, vv + 0.5f); }
                }
                a.DU = pu; a.DV = pv; a.DeckHU = (u1 - u0) / 2f; a.DeckHV = (v1 - v0) / 2f; a.DeckC = pc + pu * ((u0 + u1) / 2f) + pv * ((v0 + v1) / 2f);
                // the tennis courts: the OSM pitches (doubles courts), with 3.7 m of surround at the sides and 5.5 m at the ends
                if (am.courts != null && am.courts.Length > 0)
                {
                    foreach (var ct in am.courts) { var r = ToPoints(ct.outline); if (r.Count > 1 && (r[0] - r[r.Count - 1]).sqrMagnitude < 1e-4f) r.RemoveAt(r.Count - 1); a.Courts.Add(r); }
                    BestRectangle(a.Courts[0], out _, out Vector2 cu0, out Vector2 cv0, out float l0, out float w0);
                    if (w0 > l0) { var t = cu0; cu0 = cv0; cv0 = t; }   // CU along the courts' length
                    float a0 = float.MaxValue, a1 = float.MinValue, b0 = float.MaxValue, b1 = float.MinValue; var o = a.Courts[0][0];
                    foreach (var r in a.Courts) foreach (var p in r) { float uu = Vector2.Dot(p - o, cu0), vv = Vector2.Dot(p - o, cv0); a0 = Mathf.Min(a0, uu); a1 = Mathf.Max(a1, uu); b0 = Mathf.Min(b0, vv); b1 = Mathf.Max(b1, vv); }
                    a.CU = cu0; a.CV = cv0;
                    a.CourtHU = (a1 - a0) / 2f + 5.5f; a.CourtHV = (b1 - b0) / 2f + 3.7f;
                    a.CourtC = o + cu0 * ((a0 + a1) / 2f) + cv0 * ((b0 + b1) / 2f);
                }
                if (am.gazebo_building >= 0)
                {
                    a.Pavilion = am.gazebo_building; Replaced.Add(a.Pavilion);
                    var pr = ToPoints(map.buildings[a.Pavilion].footprint); a.PavC = RingCentroid(pr);
                    foreach (var p in pr) a.PavR = Mathf.Max(a.PavR, (p - a.PavC).magnitude + 1f);
                }
                Amenity = a;
                // the parking lot: off the street nearest the clubhouse, 16 m wide and 11 m deep, its drive 6 m wide
                var from = a.Clubhouse >= 0 ? a.ClubC : a.DeckC;
                Nearest(from, out Vector2 q, out float half, out Vector2 tan, out _, false);
                var n = (from - q).normalized;
                if (Vector2.Dot(n, from - q) > half + 24f)
                {
                    // slid along the street, the side away from the pool first (Dan's overhead: the lot is off the west)
                    var side = new Vector2(-n.y, n.x); if (Vector2.Dot(side, a.DeckC - from) > 0f) side = -side;
                    float near = half + 4.5f, far = near + 11f;
                    foreach (float shift in new[] { 0f, 6f, 12f, 18f, 24f, -6f, -12f })
                    {
                        var o = q + side * shift;
                        Nearest(o, out Vector2 qo, out float ho, out _, out _, false);
                        var no = (o + n * 10f - qo).normalized; var so = new Vector2(-no.y, no.x);
                        float nr = ho + 4.5f, fr = nr + 11f;
                        var lot = new List<Vector2> { qo + no * nr - so * 8f, qo + no * nr + so * 8f, qo + no * fr + so * 8f, qo + no * fr - so * 8f };
                        bool ok = true;
                        for (int k = 0; k < 4 && ok; k++)
                            for (float t = 0f; t <= 1f && ok; t += 0.1f)
                            {
                                var p = Vector2.Lerp(lot[k], lot[(k + 1) % 4], t);
                                if (InsideHouse(p, 2f, null) || OnDrivePlan(p, 0.5f) || RoadClearance(p) < 3f || a.InRect(p, a.DeckC, a.DU, a.DV, a.DeckHU, a.DeckHV, 2.5f)) ok = false;
                            }
                        if (!ok) continue;
                        a.Lot = lot;
                        a.LotDrive = new List<Vector2> { qo + no * (ho + 0.45f), qo + no * (nr + 0.5f) };
                        DriveSegs.Add((a.LotDrive[0], a.LotDrive[1], 3f));
                        Log22.Add($"Enclave parking lot 16 x 11 m off {map.roads[Segs.First(sg => (sg.a - qo).sqrMagnitude < 1e4f).road].name} at ({(lot[0].x + lot[2].x) / 2f:0}, {(lot[0].y + lot[2].y) / 2f:0})");
                        break;
                    }
                    if (a.Lot == null) Log22.Add("Enclave parking lot: no clear 16 x 11 m near the clubhouse, not built");
                    else ExtendEnclaveLot(a);
                }
                // the pool's black iron fence (breakable, white square posts) round the deck
                float hu = a.DeckHU + 0.8f, hv = a.DeckHV + 0.8f;
                var fc = new[] { a.DeckC + a.DU * hu + a.DV * hv, a.DeckC - a.DU * hu + a.DV * hv, a.DeckC - a.DU * hu - a.DV * hv, a.DeckC + a.DU * hu - a.DV * hv };
                if (!PlanEnclaveWalk(a, fc))
                {
                    var run = new FenceRun { kind = FenceKind.Iron, seed = 22, closed = true };
                    run.pts.AddRange(fc); run.pts.Add(fc[0]);
                    Fences.Add(run);
                }
            }

            // Round 23 follow-ups, BUG-002 (Dan, at the pool fence looking over the lot: "there should be a sidewalk between the pool and
            // the parking lot [and] a gate there as well"): a sidewalk fills the ground between the pool fence's side facing the lot and
            // the lot's edge, over the stretch where the two face each other; the fence gets a 3.6 m gateway in the middle of it (a real
            // gap a vehicle drives through), its two gate leaves standing open, folded back inside the fence (iron, breakable like it)
            bool PlanEnclaveWalk(EnclaveAmenity a, Vector2[] fc)
            {
                if (a.Lot == null) return false;
                var lc = (a.Lot[0] + a.Lot[1] + a.Lot[2] + a.Lot[3]) * 0.25f;
                int e = Enumerable.Range(0, 4).OrderByDescending(k => Vector2.Dot(((fc[k] + fc[(k + 1) % 4]) * 0.5f - a.DeckC).normalized, (lc - a.DeckC).normalized)).First();
                var f0 = fc[e]; var f1 = fc[(e + 1) % 4]; float lf = (f1 - f0).magnitude; var fd = (f1 - f0) / lf;
                var o = new Vector2(-fd.y, fd.x); if (Vector2.Dot(o, (f0 + f1) * 0.5f - a.DeckC) < 0f) o = -o;   // out of the deck
                // the lot's edge facing the pool
                int le = Enumerable.Range(0, 4).OrderBy(k => ((a.Lot[k] + a.Lot[(k + 1) % 4]) * 0.5f - a.DeckC).magnitude).First();
                var e0 = a.Lot[le]; var e1 = a.Lot[(le + 1) % 4];
                // where the fence side and the lot's edge face each other: the lot edge's ends projected onto the fence side
                float ta = Vector2.Dot(e0 - f0, fd), tb = Vector2.Dot(e1 - f0, fd);
                float t0 = Mathf.Max(0.3f, Mathf.Min(ta, tb)), t1 = Mathf.Min(lf - 0.3f, Mathf.Max(ta, tb));
                Vector2 Hit(Vector2 from)   // from a point outside the fence, straight out (o) to the lot's edge line
                {
                    var ed = e1 - e0; float den = o.x * ed.y - o.y * ed.x; if (Mathf.Abs(den) < 1e-4f) return from;
                    var w = e0 - from; float s = (w.x * ed.y - w.y * ed.x) / den; return from + o * s;
                }
                var a0 = f0 + fd * t0 + o * 0.15f; var a1 = f0 + fd * t1 + o * 0.15f; var b0 = Hit(a0); var b1 = Hit(a1);
                float g0 = Vector2.Dot(b0 - a0, o), g1 = Vector2.Dot(b1 - a1, o);
                if (t1 - t0 < 6f || Mathf.Min(g0, g1) < 0.6f || Mathf.Max(g0, g1) > 6f)
                {
                    Log22.Add($"Round 23 follow-ups: Enclave sidewalk NOT built: the pool fence and the lot face each other over {t1 - t0:0.0} m, {g0:0.0} to {g1:0.0} m apart");
                    return false;
                }
                a.Walk = new List<Vector2> { a0 - o * 0.15f, a1 - o * 0.15f, b1 + o * 0.3f, b0 + o * 0.3f };   // under the fence line and 0.3 m onto the lot (no seam)
                a.WalkSide = e;
                a.Gate = f0 + fd * ((t0 + t1) * 0.5f);
                a.WalkF0 = f0; a.WalkD = fd; a.WalkO = o; a.WalkT0 = t0; a.WalkT1 = t1; a.WalkGap = (g0 + g1) * 0.5f + 0.15f;
                // the fence, open at the gate: from the gateway's far post round the deck to its near post
                float tm = (t0 + t1) * 0.5f, gh = 1.8f;
                var run = new FenceRun { kind = FenceKind.Iron, seed = 22, closed = false };
                run.pts.Add(f0 + fd * (tm + gh));
                for (int k = 1; k <= 4; k++) run.pts.Add(fc[(e + k) % 4]);
                run.pts.Add(f0 + fd * (tm - gh));
                Fences.Add(run);
                // the two gate leaves, open, folded back against the inside of the fence
                foreach (float sg in new[] { -1f, 1f })
                {
                    var hinge = f0 + fd * (tm + sg * gh) - o * 0.3f;
                    var leaf = new FenceRun { kind = FenceKind.Iron, seed = 23, closed = false };
                    leaf.pts.Add(hinge); leaf.pts.Add(hinge + fd * sg * gh); Fences.Add(leaf);
                }
                Log22.Add($"Round 23 follow-ups: Enclave sidewalk between the pool fence and the lot, {t1 - t0:0.0} m long, {g0:0.0} to {g1:0.0} m wide; a {gh * 2f:0.0} m gateway in the pool fence at ({(f0 + fd * tm).x:0.0}, {(f0 + fd * tm).y:0.0}), its two leaves open inside the fence");
                return true;
            }

            // two brick monuments at each entrance (Chastain Glen's), either side of the way in, facing New Chastain Road
            // Round 23 BUG-001 (Dan, session 2026-10-12_20-55-46-538_f31c32): "parking lot should extend out to here", his
            // vehicle at x 1767.22, z 771.72 on the grass beside the lot. The lot's rectangle grows (away from the street and
            // along it) until it takes in his spot with a margin; it stays flush on the ground, no curb. That lot only.
            static readonly Vector2 EnclaveLotSpot = new Vector2(1767.22f, 771.72f);
            void ExtendEnclaveLot(EnclaveAmenity a)
            {
                Vector2 o = a.Lot[0], sd = (a.Lot[1] - a.Lot[0]).normalized, n = (a.Lot[3] - a.Lot[0]).normalized;
                float w = (a.Lot[1] - a.Lot[0]).magnitude, dp = (a.Lot[3] - a.Lot[0]).magnitude;
                float s = Vector2.Dot(EnclaveLotSpot - o, sd), d = Vector2.Dot(EnclaveLotSpot - o, n);
                foreach (float m in new[] { 4f, 3f, 2f, 1f, 0f })
                {
                    float s0 = Mathf.Min(0f, s - m), s1 = Mathf.Max(w, s + m), d1 = Mathf.Max(dp, d + m);
                    bool ok = true;
                    for (float u = s0; u <= s1 + 0.01f && ok; u += 1f)
                        for (float v = 0f; v <= d1 + 0.01f && ok; v += 1f)
                        {
                            if (u >= 0f && u <= w && v <= dp) continue;   // the lot as it was
                            var p = o + sd * Mathf.Min(u, s1) + n * Mathf.Min(v, d1);
                            if (InsideHouse(p, 2f, null) || OnDrivePlan(p, 0.5f) || RoadClearance(p) < 3f || a.InRect(p, a.DeckC, a.DU, a.DV, a.DeckHU, a.DeckHV, 2.5f)
                                || a.InRect(p, a.CourtC, a.CU, a.CV, a.CourtHU, a.CourtHV, 1.5f) || (a.PavR > 0f && (p - a.PavC).magnitude < a.PavR + 1f)) ok = false;
                        }
                    if (!ok) continue;
                    a.Lot = new List<Vector2> { o + sd * s0, o + sd * s1, o + sd * s1 + n * d1, o + sd * s0 + n * d1 };
                    Log22.Add($"Round 23: Enclave lot extended to Dan's spot ({EnclaveLotSpot.x:0.0}, {EnclaveLotSpot.y:0.0}) with {m:0} m to spare: {s1 - s0:0.0} x {d1:0.0} m (was {w:0} x {dp:0}), corners " + string.Join(" ", a.Lot.Select(q => $"({q.x:0.0}, {q.y:0.0})")));
                    return;
                }
                Log22.Add($"Round 23: Enclave lot NOT extended: the ground out to Dan's spot is blocked (spot at {s:0.0} m along, {d:0.0} m deep in the lot's frame)");
            }

            void PlanCommunityMonuments()
            {
                for (int i = 0; i < Communities.communities.Length; i++)
                {
                    var cm = Communities.communities[i]; int id = i + 1;
                    if (id == GrasmereCommunity || cm.entrances == null || cm.entrances.Length == 0) continue;   // Grasmere Court is part of Chastain Glen
                    var P = new Vector2(cm.entrances[0].x, cm.entrances[0].z);
                    var at = cm.entrances.Where(e => (new Vector2(e.x, e.z) - P).sqrMagnitude < 1f).ToList();
                    var d = Vector2.zero; foreach (var e in at) d += new Vector2(e.dx, e.dz); d.Normalize();
                    var n = new Vector2(-d.y, d.x);
                    var lanes = at.Select(e => { var line = ToPoints(map.roads[e.road].points); if ((line[0] - P).sqrMagnitude > 1f) line.Reverse(); return (Densify(line, 1f), e.width); }).ToList();
                    foreach (float side in new[] { 1f, -1f })
                    {
                        bool placed = false;
                        for (float s = 8f; s <= 40f && !placed; s += 2f)
                        {
                            // how far the way in reaches to either side at this distance in (a divided entrance's two lanes)
                            float lmin = 0f, lmax = 0f;
                            foreach (var (line, w) in lanes) foreach (var p in line) { float t = Vector2.Dot(p - P, d); if (t < s - 4f || t > s + 4f) continue; float l = Vector2.Dot(p - P, n); lmin = Mathf.Min(lmin, l - w / 2f); lmax = Mathf.Max(lmax, l + w / 2f); }
                            for (float extra = 0f; extra <= 4f && !placed; extra += 1f)
                            {
                                float lat = side > 0f ? lmax + 7.4f + extra : lmin - 7.4f - extra;
                                var c = P + d * s + n * lat;
                                var face = (-d - n * side * 0.27f).normalized;
                                var along = new Vector2(face.y, -face.x);
                                bool ok = true;
                                for (float u = -3.6f; u <= 3.61f && ok; u += 0.6f)
                                    foreach (float v in new[] { -0.5f, 0.5f })
                                    {
                                        var p = c + along * u + face * v;
                                        if (AnyRoadClearance(p) < 1.5f || InsideHouse(p, 2.5f, null) || OnDrivePlan(p, 1f) || AmenityBlocks(p, 1f)) { ok = false; break; }
                                    }
                                if (!ok) continue;
                                CommunityMonuments.Add((id, c, face)); KeepOut(c, 4.5f); placed = true;
                            }
                        }
                        if (!placed) Log22.Add($"{cm.name}: no room for the monument on the {(side > 0f ? "right" : "left")} of the entrance");
                    }
                }
            }

            // the distance from p to the edge of the nearest road of the whole map (not only the area's)
            List<(Vector2 a, Vector2 b, float half)> allRoadSegs;
            float AnyRoadClearance(Vector2 p)
            {
                if (allRoadSegs == null)
                {
                    allRoadSegs = new List<(Vector2, Vector2, float)>();
                    foreach (var r in map.roads) { if (r.points == null) continue; var q = ToPoints(r.points); for (int k = 0; k + 1 < q.Count; k++) allRoadSegs.Add((q[k], q[k + 1], r.width * 0.5f)); }
                }
                float best = float.MaxValue;
                foreach (var s in allRoadSegs)
                {
                    var ab = s.b - s.a; float t = ab.sqrMagnitude < 1e-6f ? 0f : Mathf.Clamp01(Vector2.Dot(p - s.a, ab) / ab.sqrMagnitude);
                    best = Mathf.Min(best, (s.a + ab * t - p).magnitude - s.half);
                }
                return best;
            }

            // ---------------------------------------------------------------- grading (before anything samples the ground)

            // the pool deck (with the clubhouse) and the court block are each set level, with banks of about 1 in 2 back to the
            // natural ground within 8 m; never within 1.5 m of a house or 1 m of a street
            void GradeCommunities()
            {
                var a = Amenity; if (a == null) return;
                a.DeckY = MedianIn(a.DeckC, a.DU, a.DV, a.DeckHU, a.DeckHV);
                GradeRect(a.DeckC, a.DU, a.DV, a.DeckHU, a.DeckHV, a.DeckY - 0.04f, "pool deck");
                if (a.Walk != null) GradeWalk(a);
                if (a.Courts.Count > 0)
                {
                    a.CourtY = MedianIn(a.CourtC, a.CU, a.CV, a.CourtHU, a.CourtHV);
                    GradeRect(a.CourtC, a.CU, a.CV, a.CourtHU, a.CourtHV, a.CourtY, "tennis courts");
                }
            }

            // Round 23 follow-ups (BUG-002): the deck stands about half a meter over the lot; the ground from 2 m inside the lot across
            // the sidewalk and through the gate to the deck's edge becomes one even slope (the exe check: a car stopped on the old bank)
            void GradeWalk(EnclaveAmenity a)
            {
                float far = a.WalkGap + 2f, near = -0.8f;   // across: from 2 m into the lot to the deck's edge (0.8 m inside the fence)
                var lotH = new List<float>();
                for (float t = a.WalkT0 - 2f; t <= a.WalkT1 + 2.01f; t += 0.5f) { var q = a.WalkF0 + a.WalkD * t + a.WalkO * far; lotH.Add(terrain.Height(q.x, q.y)); }
                float LotAt(float t) { float f = Mathf.Clamp((t - (a.WalkT0 - 2f)) / 0.5f, 0f, lotH.Count - 1.001f); int i = (int)f; return Mathf.Lerp(lotH[i], lotH[i + 1], f - i); }
                var corners = new[] { a.WalkF0 + a.WalkD * (a.WalkT0 - 2f) + a.WalkO * (far + 1f), a.WalkF0 + a.WalkD * (a.WalkT1 + 2f) + a.WalkO * (far + 1f), a.WalkF0 + a.WalkD * (a.WalkT0 - 2f) + a.WalkO * (near - 0.5f), a.WalkF0 + a.WalkD * (a.WalkT1 + 2f) + a.WalkO * (near - 0.5f) };
                int changed = 0; float most = 0f;
                EditTerrain(terrain, BoundsOf(corners, 1f), (x, z, h) =>
                {
                    var q = new Vector2(x, z); float t = Vector2.Dot(q - a.WalkF0, a.WalkD), cc = Vector2.Dot(q - a.WalkF0, a.WalkO);
                    if (cc < near || cc > far + 1f || t < a.WalkT0 - 1.5f || t > a.WalkT1 + 1.5f) return h;
                    float target = Mathf.Lerp(a.DeckY - 0.04f, LotAt(t), Mathf.InverseLerp(near, far, cc));
                    float w = Smooth01((a.WalkT1 + 1.5f - t) / 1.5f) * Smooth01((t - (a.WalkT0 - 1.5f)) / 1.5f) * Smooth01((far + 1f - cc) / 1f);
                    float nh = Mathf.Lerp(h, target, w); if (Mathf.Abs(nh - h) > 1e-3f) { changed++; most = Mathf.Max(most, Mathf.Abs(nh - h)); }
                    return nh;
                });
                Log22.Add($"Round 23 follow-ups: the ground from the lot across the sidewalk to the pool deck made one slope ({a.DeckY - 0.04f - LotAt((a.WalkT0 + a.WalkT1) * 0.5f):0.00} m up over {far - near:0.0} m at the gate): {changed} terrain samples moved, by up to {most:0.00} m");
            }

            float MedianIn(Vector2 c, Vector2 u, Vector2 v, float hu, float hv)
            {
                var s = new List<float>();
                for (float x = -hu; x <= hu; x += 1.5f) for (float z = -hv; z <= hv; z += 1.5f) { var p = c + u * x + v * z; s.Add(terrain.Height(p.x, p.y)); }
                s.Sort(); return s[s.Count / 2];
            }

            void GradeRect(Vector2 c, Vector2 u, Vector2 v, float hu, float hv, float y, string what)
            {
                const float Reach = 8f, Bank = 0.5f;
                float r = Mathf.Sqrt(hu * hu + hv * hv) + Reach;
                int changed = 0; float most = 0f;
                EditTerrain(terrain, Rect.MinMaxRect(c.x - r, c.y - r, c.x + r, c.y + r), (x, z, h) =>
                {
                    var p = new Vector2(x, z); var dd = p - c;
                    float du = Mathf.Max(0f, Mathf.Abs(Vector2.Dot(dd, u)) - hu), dv = Mathf.Max(0f, Mathf.Abs(Vector2.Dot(dd, v)) - hv);
                    float d = Mathf.Sqrt(du * du + dv * dv);
                    if (d > Reach || InsideHouse(p, 1.5f, null) || RoadClearance(p) < 1f) return h;
                    float nh = d <= 0f ? y : Mathf.Clamp(h, y - d * Bank, y + d * Bank);
                    if (Mathf.Abs(nh - h) > 1e-3f) { changed++; most = Mathf.Max(most, Mathf.Abs(nh - h)); }
                    return nh;
                });
                Log22.Add($"Enclave {what} set level at {y:0.00} m: {changed} terrain samples moved, by up to {most:0.00} m");
            }
        }

        // a tiny list of report lines (kept apart from the Round 7 builder's StringBuilder)
        sealed class StringBuilder22
        {
            readonly List<string> lines = new List<string>();
            public void Add(string s) => lines.Add(s);
            public override string ToString() => string.Join("; ", lines);
        }

        // ------------------------------------------------------------ building

        static readonly Color ChainLink = new Color(0.10f, 0.12f, 0.11f), CourtGreen = new Color(0.20f, 0.42f, 0.26f), CourtBlue = new Color(0.17f, 0.34f, 0.60f), LineWhite = new Color(0.93f, 0.93f, 0.91f);

        static void BuildCommunities(HoodCtx c)
        {
            var hood = c.hood;
            if (hood.Communities == null) return;
            var root = new GameObject("Round22").transform; root.SetParent(c.root, false);
            BuildCommunityMonuments(c, root);
            if (hood.Amenity != null) BuildEnclaveAmenity(c, root);
            // report: houses per community and style, the Enclave's three-car garages and grown footprints, buried walls
            for (int i = 1; i <= hood.Communities.communities.Length; i++)
            {
                var hs = hood.Houses.Where(h => h.community == i).ToList();
                hood.Log22.Add($"{hood.CommunityName(i)}: {hs.Count} houses (A {hs.Count(h => h.style == HouseStyle.A)}, B {hs.Count(h => h.style == HouseStyle.B)}, C {hs.Count(h => h.style == HouseStyle.C)}, D {hs.Count(h => h.style == HouseStyle.D)}; brick fronts {hs.Count(h => h.brick)}; three-car garages {hs.Count(h => h.garageBays == 3)})");
                var front = hs.FirstOrDefault(h => h.style == HouseStyle.A) ?? hs.FirstOrDefault();
                if (front != null) AddView(c, "r22_" + hood.Communities.communities[i - 1].key + "_street", front.footprintCentre, new Vector2(front.frame.F.x, front.frame.F.z), front.nearestRoadPoint, front.index);
            }
            hood.Log22.Add($"Enclave footprints grown where the lot allows: {hood.GrownHouses}");
            hood.Log22.Add($"community houses with walls buried more than 0.5 m (report only): {hood.CommunityBuried.Count}" + (hood.CommunityBuried.Count > 0 ? " (" + string.Join(", ", hood.CommunityBuried.OrderByDescending(b => b.buried).Take(8).Select(b => $"{b.h.index} at ({b.h.footprintCentre.x:0}, {b.h.footprintCentre.y:0}) {b.buried:0.0} m")) + ")" : ""));
            Debug.Log("[Round22] " + hood.Log22);
        }

        static void BuildCommunityMonuments(HoodCtx c, Transform root)
        {
            var hood = c.hood; if (hood.CommunityMonuments.Count == 0) return;
            var solid = new MeshBuilder();
            var mats = new Dictionary<int, Material>();
            foreach (var (id, p, face) in hood.CommunityMonuments)
            {
                var f = MakeFrame(new Vector3(p.x, 0f, p.y), face, 1f);
                float lo = float.MaxValue, hi = float.MinValue;
                foreach (float u in new[] { -3.4f, 0f, 3.4f }) foreach (float v in new[] { -0.4f, 0.4f }) { var q = f.P(u, 0f, v); float g = c.Y(q.x, q.z); lo = Mathf.Min(lo, g); hi = Mathf.Max(hi, g); }
                f.O.y = hi + 0.05f;
                Monument(solid, f, lo - f.O.y - 0.4f);
                var col = new GameObject("Monument collider"); col.transform.SetParent(root, false);
                col.transform.SetPositionAndRotation(f.P(0f, 0.9f, 0f), Quaternion.LookRotation(f.F, Vector3.up));
                col.AddComponent<BoxCollider>().size = new Vector3(6.8f, 2.6f, 0.75f);
                string key = hood.Communities.communities[id - 1].key;
                if (!mats.TryGetValue(id, out var m)) mats[id] = m = HandMadeSignMaterial(key == "enclave" ? "Assets/HandMade/Entrance/ChastainEnclaveSign.png" : "Assets/HandMade/Entrance/ChastainCommonsSign.png", "MonumentSign_" + key, true);
                if (m != null)
                {
                    var plate = SignPlate(root, f.P(0f, 1.32f, 0.37f), -f.F, 1f, m);   // faces the road (the quad's front is -z locally)
                    plate.name = "Monument sign " + key; plate.transform.localScale = new Vector3(2.7f, 1.9f, 1f);
                }
                if (!c.Views.Exists(v => v.label == "r22_" + key + "_monument")) AddView(c, "r22_" + key + "_monument", p, face, p);
                hood.Log22.Add($"{hood.CommunityName(id)} monument at ({p.x:0.0}, {p.y:0.0})");
            }
            solid.ToObject("Round22Monuments", c.mats.House, root, false, true);
        }

        // a lit, depth-tested, one-sided material for a hand-made sign face (cut out to its panel when cutout)
        static Material HandMadeSignMaterial(string path, string name, bool cutout)
        {
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (tex == null) { Debug.LogWarning("[Round22] " + path + " missing (Tools/handmade): the sign face is left off"); return null; }
            var m = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name };
            m.SetTexture("_BaseMap", tex); m.mainTexture = tex; m.SetColor("_BaseColor", Color.white);
            m.SetFloat("_Smoothness", 0.2f);
            if (cutout) { m.SetFloat("_AlphaClip", 1f); m.SetFloat("_Cutoff", 0.5f); m.EnableKeyword("_ALPHATEST_ON"); }
            m.SetFloat("_Cull", (float)CullMode.Back);
            AssetDatabase.CreateAsset(m, $"{GeneratedDir}/{name}.mat");
            return m;
        }

        // ------------------------------------------------------------ the Enclave's pool, clubhouse, courts and pavilion

        static void BuildEnclaveAmenity(HoodCtx c, Transform parent)
        {
            var a = c.hood.Amenity;
            var root = new GameObject("EnclaveAmenity").transform; root.SetParent(parent, false);
            var mb = new MeshBuilder();          // deck, pool, clubhouse, pavilion, fence posts: one lit mesh with shadows
            var flat = new MeshBuilder();        // court surfaces, lines, the lot: on the ground (depth-offset material)
            var link = new MeshBuilder();        // the chain-link mesh: thin, no shadows
            var furniture = new Breakables();    // lounge chairs, tables, umbrellas (break like the mailboxes)
            var deckC = new Color(0.80f, 0.76f, 0.68f);
            var f = new Frame { O = new Vector3(a.DeckC.x, a.DeckY, a.DeckC.y), R = new Vector3(a.DU.x, 0, a.DU.y), F = new Vector3(a.DV.x, 0, a.DV.y), M = 1f };
            // --- the deck and the pool (water as Round 15: drive in, drive out up the shallow end)
            float lo = float.MaxValue;
            for (float u = -a.DeckHU; u <= a.DeckHU; u += 1.5f) for (float v = -a.DeckHV; v <= a.DeckHV; v += 1.5f) { var p = f.P(u, 0f, v); lo = Mathf.Min(lo, c.Y(p.x, p.z)); }
            BoxNoBottom(mb, f, -a.DeckHU, a.DeckHU, lo - 0.3f - a.DeckY, 0f, -a.DeckHV, a.DeckHV, deckC, deckC, deckC);
            float surface = BuildSunkPool(c, mb, f, a.PoolRing, a.DeckY, a.DeckHU, a.DeckHV, deckC, root);
            var pr = a.PoolRing;
            mb.Tint = new Color(0.16f, 0.74f, 0.84f);
            int pv0 = mb.Count;
            foreach (var p in pr) mb.Vertex(new Vector3(p.x, surface, p.y));
            foreach (var tri in EarClip(pr)) mb.Tri(pv0 + tri.x, pv0 + tri.z, pv0 + tri.y);
            for (int i = 0; i < pr.Count; i++)
            {
                var p0 = pr[i]; var p1 = pr[(i + 1) % pr.Count]; var d = (p1 - p0).normalized; var outN = new Vector2(d.y, -d.x);
                // coping round the edge, and the dark tile line at the waterline (on the walls, just inside)
                mb.Tint = new Color(0.93f, 0.92f, 0.88f);
                Face(mb, new Vector3(p0.x, a.DeckY + 0.035f, p0.y), new Vector3(p1.x, a.DeckY + 0.035f, p1.y), new Vector3(p1.x + outN.x * 0.35f, a.DeckY + 0.035f, p1.y + outN.y * 0.35f), new Vector3(p0.x + outN.x * 0.35f, a.DeckY + 0.035f, p0.y + outN.y * 0.35f), Vector3.up);
                mb.Tint = new Color(0.10f, 0.18f, 0.40f);
                var inn = -outN * 0.015f;
                Face(mb, new Vector3(p0.x + inn.x, surface - 0.14f, p0.y + inn.y), new Vector3(p1.x + inn.x, surface - 0.14f, p1.y + inn.y), new Vector3(p1.x + inn.x, a.DeckY - 0.01f, p1.y + inn.y), new Vector3(p0.x + inn.x, a.DeckY - 0.01f, p0.y + inn.y), new Vector3(-outN.x, 0f, -outN.y));
            }
            // concrete joints across the deck, every 3 m
            mb.Tint = Shade(deckC, 0.82f);
            for (float u = -a.DeckHU + 3f; u < a.DeckHU; u += 3f)
                foreach (var (s0, s1) in DeckRuns(f, a, u, true)) Face(mb, f.P(u - 0.02f, 0.006f, s0), f.P(u + 0.02f, 0.006f, s0), f.P(u + 0.02f, 0.006f, s1), f.P(u - 0.02f, 0.006f, s1), Vector3.up);
            for (float v = -a.DeckHV + 3f; v < a.DeckHV; v += 3f)
                foreach (var (s0, s1) in DeckRuns(f, a, v, false)) Face(mb, f.P(s0, 0.006f, v - 0.02f), f.P(s1, 0.006f, v - 0.02f), f.P(s1, 0.006f, v + 0.02f), f.P(s0, 0.006f, v + 0.02f), Vector3.up);
            // lounge chairs (striped) along the deck, facing the pool; round tables with red umbrellas.
            // Round 23 follow-ups, BUG-004 (Dan: "far too many loungers here. At most 10. Can have tables with umbrellas as well"):
            // five loungers a side at most, in a group round the middle of the side's free stretch, a table with its umbrella at each
            // end of the group; nothing within 3.5 m of the gate (BUG-002)
            int chairs = 0, tables = 0;
            Color[] stripe = { new Color(0.72f, 0.16f, 0.20f), new Color(0.15f, 0.48f, 0.34f) };
            bool NearGate(Vector2 q) => a.Walk != null && (q - a.Gate).magnitude < 3.5f;
            var chairAt = new List<(float side, float u)>(); var tableAt = new List<(float side, float u)>();
            foreach (float side in new[] { 1f, -1f })
            {
                var ok = new List<float>();
                for (float u = -a.DeckHU + 1.4f; u <= a.DeckHU - 1.4f; u += 0.25f)
                {
                    var p3 = f.P(u, 0f, side * (a.DeckHV - 1.35f)); var p = new Vector2(p3.x, p3.z);
                    if (!InPolygon(p, pr) && DistToEdges(p, pr) >= 1.6f && !ClubhouseNear(a, p, 2f) && !NearGate(p)) ok.Add(u);
                }
                // the longest free stretch on this side
                float bs = 0f, be = -1f, rs = 0f;
                for (int i = 0; i < ok.Count; i++)
                {
                    if (i == 0 || ok[i] - ok[i - 1] > 0.3f) rs = ok[i];
                    if (ok[i] - rs > be - bs) { bs = rs; be = ok[i]; }
                }
                if (be < bs) continue;
                int n = Mathf.Clamp(Mathf.FloorToInt((be - bs - 2f * 1.8f) / 1.4f) + 1, 0, 5);   // loungers 1.4 m apart, room for a table at each end
                float mid = (bs + be) * 0.5f, first = mid - (n - 1) * 0.7f;
                for (int k = 0; k < n; k++) chairAt.Add((side, first + k * 1.4f));
                if (n > 0) { tableAt.Add((side, first - 1.8f)); tableAt.Add((side, first + (n - 1) * 1.4f + 1.8f)); }
            }
            foreach (var (side, u) in chairAt)
                {
                    var p3 = f.P(u, 0f, side * (a.DeckHV - 1.35f)); var p = new Vector2(p3.x, p3.z);
                    var face = new Vector2(a.DV.x, a.DV.y) * -side;
                    var cf = MakeFrame(new Vector3(p.x, a.DeckY, p.y), face, 1f);
                    var m = furniture.Begin(p);
                    var col = stripe[chairs % 2];
                    furniture.Piece(() =>
                    {
                        for (int k = 0; k < 6; k++) BoxF(m, cf, -0.3f, 0.3f, 0.30f, 0.36f, -0.95f + k * 0.27f, -0.95f + k * 0.27f + 0.25f, k % 2 == 0 ? col : LineWhite);
                        Beam(m, cf.P(-0.3f, 0.36f, -0.95f), cf.P(-0.3f, 0.85f, -1.25f), 0.04f, 0.04f, LineWhite); Beam(m, cf.P(0.3f, 0.36f, -0.95f), cf.P(0.3f, 0.85f, -1.25f), 0.04f, 0.04f, LineWhite);
                        BoxF(m, cf, -0.3f, 0.3f, 0.36f, 0.85f, -1.27f, -1.2f, col);
                        foreach (float uu in new[] { -0.28f, 0.28f }) foreach (float vv in new[] { -0.85f, 0.75f }) BoxF(m, cf, uu - 0.025f, uu + 0.025f, 0f, 0.3f, vv - 0.025f, vv + 0.025f, LineWhite);
                    }, a.DeckY);
                    furniture.End(new Vector3(p.x, a.DeckY + 0.45f, p.y), new Vector3(face.x, 0f, face.y), 0.7f, 0.9f, 2.2f);
                    chairs++;
                }
            foreach (var (tside, u) in tableAt)
            {
                foreach (float side in new[] { tside })
                {
                    var p3 = f.P(u, 0f, side * (a.DeckHV - 1.6f)); var p = new Vector2(p3.x, p3.z);
                    if (InPolygon(p, pr) || DistToEdges(p, pr) < 1.6f || ClubhouseNear(a, p, 2f) || NearGate(p) || Mathf.Abs(u) > a.DeckHU - 1.3f) continue;
                    var g = new Vector3(p.x, a.DeckY, p.y);
                    var m = furniture.Begin(p);
                    furniture.Piece(() => { Cyl(m, g + Vector3.up * 0.7f, 0.04f, 0.55f, 0.55f, LineWhite, 10); Cyl(m, g, 0.7f, 0.04f, 0.04f, LineWhite, 5); }, a.DeckY);
                    furniture.Piece(() => { Cyl(m, g, 2.3f, 0.025f, 0.025f, new Color(0.8f, 0.8f, 0.8f), 5); Cyl(m, g + Vector3.up * 2.05f, 0.5f, 1.3f, 0.0f, new Color(0.78f, 0.10f, 0.12f), 8); }, a.DeckY);
                    furniture.End(new Vector3(p.x, a.DeckY + 0.9f, p.y), Vector3.forward, 1.3f, 1.8f, 1.3f);
                    tables++; break;
                }
            }
            // low hedges along the fence, outside it, on the sides away from the clubhouse
            var hedge = new Color(0.13f, 0.31f, 0.15f);
            int shrubs = 0;
            for (int e = 0; e < 4; e++)
            {
                Vector2 Corner(int k) { float su = k == 0 || k == 3 ? 1f : -1f, sv = k < 2 ? 1f : -1f; return a.DeckC + a.DU * su * (a.DeckHU + 1.6f) + a.DV * sv * (a.DeckHV + 1.6f); }
                var p0 = Corner(e); var p1 = Corner((e + 1) % 4); float L = (p1 - p0).magnitude;
                for (float s = 1.5f; s < L - 1.5f; s += 1.3f)
                {
                    var p = Vector2.Lerp(p0, p1, s / L);
                    if (ClubhouseNear(a, p, 2f) || c.hood.RoadClearance(p) < 2f || c.OnDrive(p, 0.8f)) continue;
                    if (a.Walk != null && (InPolygon(p, a.Walk) || DistToEdges(p, a.Walk) < 1f)) continue;   // Round 23 follow-ups: not on the sidewalk to the lot
                    float gy = c.Y(p.x, p.y);
                    Blob(mb, new Vector3(p.x, gy + 0.05f, p.y), new Vector3(0.7f, 0.55f, 0.7f), hedge * 1.15f, hedge * 0.75f, 7, 3, true, shrubs);
                    shrubs++;
                }
            }
            // --- the clubhouse: one story, tan siding, a hip roof with a front gable over a porch on white columns, restroom doors
            if (a.Clubhouse >= 0) BuildEnclaveClubhouse(c, mb, root, a);
            // --- the courts, their fence, lights and the windscreen banner
            if (a.Courts.Count > 0) BuildTennisCourts(c, mb, flat, link, root, a);
            // --- the pavilion by the courts
            if (a.Pavilion >= 0) BuildEnclavePavilion(c, mb, root, a);
            // --- the parking lot off the street
            if (a.Lot != null)
            {
                flat.Tint = new Color(0.21f, 0.21f, 0.22f);
                DrapePoly(flat, a.Lot, 1f, 0.06f, c.terrain, 8);   // Round 23: a 1 m grid (was 2 m): the grass no longer shows through it
                RibbonGridMax(flat, Densify(a.LotDrive, 0.75f), Enumerable.Repeat(6f, Densify(a.LotDrive, 0.75f).Count).ToArray(), 0.05f, c.terrain, 4);
                // Round 23 follow-ups, BUG-002 (Dan: "parking spaces should be on the sides...otherwise this parking lot doesn't make
                // sense since people would be blocking people in"): one row of stalls along each long side, 5 m deep and 2.8 m wide,
                // square to the side, and a clear drive aisle down the middle from the entrance to the far end (no stall blocks another)
                var n = (a.Lot[3] - a.Lot[0]).normalized; var sd = (a.Lot[1] - a.Lot[0]).normalized;
                float lw = (a.Lot[1] - a.Lot[0]).magnitude, ld = (a.Lot[3] - a.Lot[0]).magnitude;
                int stalls = Mathf.FloorToInt((ld - 2f) / 2.8f); float s0 = (ld - stalls * 2.8f) * 0.5f;
                flat.Tint = LineWhite;
                var w = n * 0.06f;
                foreach (var (edge, dir) in new[] { (0f, 1f), (lw, -1f) })   // the side at sd 0 (stalls toward +sd) and the side at sd lw
                {
                    for (int k = 0; k <= stalls; k++)
                        for (int j = 0; j < 5; j++)   // in 1 m pieces, so each stays on the lot's surface
                        {
                            var b0 = a.Lot[0] + n * (s0 + k * 2.8f) + sd * (edge + dir * (0.3f + j * 0.94f)); var b1 = b0 + sd * dir * 0.94f;
                            Face(flat, V3(c, b0 - w, 0.085f), V3(c, b0 + w, 0.085f), V3(c, b1 + w, 0.085f), V3(c, b1 - w, 0.085f), Vector3.up);
                        }
                }
                c.hood.Log22.Add($"Round 23 follow-ups: Enclave lot re-striped: {stalls} stalls along each long side ({lw:0.0} m wide: 5 m stalls each side, a {lw - 10f:0.0} m aisle down the middle)");
            }
            // Round 23 follow-ups, BUG-002: the sidewalk between the pool fence and the lot (pale concrete, flush on the ground)
            if (a.Walk != null)
            {
                c.Walks.Tint = ConcreteLight;
                DrapePoly(c.Walks.At(a.Walk[0]), a.Walk, 0.5f, 0.075f, c.terrain, 8);
            }
            mb.ToObject("EnclavePoolAndClubhouse", c.mats.House, root, false, true);
            flat.ToObject("EnclaveCourtsAndLot", c.mats.Driveway, root, false, false);
            link.ToObject("EnclaveChainLink", c.mats.House, root, false, false);
            furniture.ToObjects("EnclavePoolFurniture", c.mats.House, root, 0.2f, BreakSounds.Kind.Sign);
            AddView(c, "r22_enclave_pool", a.DeckC, a.Clubhouse >= 0 ? (a.DeckC - a.ClubC).normalized : a.DV, a.DeckC);
            if (a.Courts.Count > 0) AddView(c, "r22_enclave_courts", a.CourtC, a.CV, a.CourtC);
            c.hood.Log22.Add($"Enclave pool site: deck {a.DeckHU * 2f:0.0} x {a.DeckHV * 2f:0.0} m, {chairs} lounge chairs, {tables} tables with umbrellas, {shrubs} hedge shrubs, clubhouse {(a.Clubhouse >= 0 ? "on building " + a.Clubhouse : "none")}, lot {(a.Lot != null)}");
        }

        static Vector3 V3(HoodCtx c, Vector2 p, float lift) => new Vector3(p.x, c.Y(p.x, p.y) + lift, p.y);

        static bool ClubhouseNear(EnclaveAmenity a, Vector2 p, float m) => a.Clubhouse >= 0 && a.InRect(p, a.ClubC, a.ClubU, a.ClubFront, a.ClubW / 2f, a.ClubD / 2f, m);

        // the stretches of a deck joint line that are on the deck (not over the pool, not under the clubhouse)
        static IEnumerable<(float, float)> DeckRuns(Frame f, EnclaveAmenity a, float at, bool alongV)
        {
            float lim = alongV ? a.DeckHV : a.DeckHU; float? start = null;
            for (float s = -lim; s <= lim + 0.01f; s += 0.25f)
            {
                var p3 = alongV ? f.P(at, 0f, s) : f.P(s, 0f, at); var p = new Vector2(p3.x, p3.z);
                bool on = !InPolygon(p, a.PoolRing) && DistToEdges(p, a.PoolRing) > 0.4f && !ClubhouseNear(a, p, 0.1f);
                if (on && start == null) start = s;
                if ((!on || s > lim - 0.01f) && start != null) { if (s - start.Value > 0.5f) yield return (start.Value, s); start = null; }
            }
        }

        static void BuildEnclaveClubhouse(HoodCtx c, MeshBuilder mb, Transform root, EnclaveAmenity a)
        {
            float cw = Mathf.Clamp(a.ClubW, 6f, 9f), cd = Mathf.Clamp(a.ClubD, 6f, 9f), ch = 3.0f, porch = 2.4f;
            var f = MakeFrame(new Vector3(a.ClubC.x, a.DeckY, a.ClubC.y), a.ClubFront, 1f);
            float lo = float.MaxValue; foreach (float u in new[] { -cw / 2, cw / 2 }) foreach (float v in new[] { -cd / 2, cd / 2 }) { var q = f.P(u, 0f, v); lo = Mathf.Min(lo, c.Y(q.x, q.z)); }
            var tan = new Color(0.80f, 0.70f, 0.53f); var roof = new Color(0.52f, 0.40f, 0.30f);
            float vb = cd / 2 - porch;   // the body's front wall; the porch is in front of it under the gable
            BoxNoBottom(mb, f, -cw / 2, cw / 2, lo - 0.3f - a.DeckY, ch, -cd / 2, vb, tan, tan, tan);
            HipRoof(mb, f, -cw / 2, cw / 2, -cd / 2, vb, ch, 1.6f, 0.45f, roof, Shade(tan, 1.1f));
            // the front gable over the porch, its triangle in siding with a round vent and white rakes
            Gable(mb, f, vb - 1.6f, cd / 2 + 0.25f, -cw / 2 + 0.3f, cw / 2 - 0.3f, false, ch, 1.7f, 0.3f, 0.25f, roof, tan, false, true, TrimWhite);
            float vy = ch + 0.75f; mb.Tint = TrimWhite;
            for (int k = 0; k < 10; k++) { float t0 = k * Mathf.PI * 2f / 10f, t1 = (k + 1) * Mathf.PI * 2f / 10f; TriFace(mb, f.P(0f, vy, cd / 2 + 0.27f), f.P(Mathf.Cos(t0) * 0.32f, vy + Mathf.Sin(t0) * 0.32f, cd / 2 + 0.27f), f.P(Mathf.Cos(t1) * 0.32f, vy + Mathf.Sin(t1) * 0.32f, cd / 2 + 0.27f), f.F); }
            mb.Tint = new Color(0.25f, 0.23f, 0.21f);
            for (int k = 0; k < 10; k++) { float t0 = k * Mathf.PI * 2f / 10f, t1 = (k + 1) * Mathf.PI * 2f / 10f; TriFace(mb, f.P(0f, vy, cd / 2 + 0.29f), f.P(Mathf.Cos(t0) * 0.24f, vy + Mathf.Sin(t0) * 0.24f, cd / 2 + 0.29f), f.P(Mathf.Cos(t1) * 0.24f, vy + Mathf.Sin(t1) * 0.24f, cd / 2 + 0.29f), f.F); }
            // the porch: a beam on white columns, the porch ceiling
            BoxF(mb, f, -cw / 2 + 0.3f, cw / 2 - 0.3f, ch - 0.3f, ch, cd / 2 - 0.25f, cd / 2, TrimWhite);
            var cols = new List<float>();
            for (int i = 0; i < 4; i++) { float u = Mathf.Lerp(-cw / 2 + 0.55f, cw / 2 - 0.55f, i / 3f); cols.Add(u); BoxF(mb, f, u - 0.12f, u + 0.12f, 0f, ch - 0.3f, cd / 2 - 0.37f, cd / 2 - 0.13f, TrimWhite); BoxF(mb, f, u - 0.18f, u + 0.18f, 0f, 0.3f, cd / 2 - 0.43f, cd / 2 - 0.07f, TrimWhite); }
            mb.Tint = Shade(tan, 1.2f);
            Face(mb, f.P(-cw / 2 + 0.3f, ch - 0.02f, vb), f.P(cw / 2 - 0.3f, ch - 0.02f, vb), f.P(cw / 2 - 0.3f, ch - 0.02f, cd / 2), f.P(-cw / 2 + 0.3f, ch - 0.02f, cd / 2), Vector3.down);
            // two restroom doors and a notice board between them, a window either end
            var hs = new HouseSpec { trimC = TrimWhite, glassC = GlassDark, doorC = new Color(0.62f, 0.52f, 0.38f), shutterC = new Color(0.3f, 0.2f, 0.15f) };
            foreach (float u in new[] { -1.5f, 1.5f })
            {
                FrontQuad(mb, f, u - 0.55f, u + 0.55f, 0f, 2.2f, vb + 0.02f, TrimWhite);
                FrontQuad(mb, f, u - 0.46f, u + 0.46f, 0f, 2.12f, vb + 0.04f, hs.doorC);
                FrontQuad(mb, f, u - 0.14f, u + 0.14f, 1.45f, 1.75f, vb + 0.06f, LineWhite);   // the restroom plate (no lettering)
            }
            FrontQuad(mb, f, -0.6f, 0.6f, 1.0f, 1.9f, vb + 0.03f, new Color(0.42f, 0.30f, 0.20f));
            FrontQuad(mb, f, -0.52f, 0.52f, 1.08f, 1.82f, vb + 0.05f, new Color(0.88f, 0.86f, 0.80f));
            if (cw > 7f) { WindowF(mb, f, -cw / 2 + 0.85f, 1.5f, 0.8f, 0.9f, vb, hs, false, false); WindowF(mb, f, cw / 2 - 0.85f, 1.5f, 0.8f, 0.9f, vb, hs, false, false); }
            // colliders: the body, and each column
            var col = new GameObject("Enclave clubhouse collider"); col.transform.SetParent(root, false);
            col.transform.SetPositionAndRotation(f.P(0f, ch / 2f, (-cd / 2 + vb) / 2f), Quaternion.LookRotation(f.F, Vector3.up));
            col.AddComponent<BoxCollider>().size = new Vector3(cw, ch, vb + cd / 2);
            foreach (float u in cols)
            {
                var pc = new GameObject("Enclave clubhouse column"); pc.transform.SetParent(root, false);
                pc.transform.SetPositionAndRotation(f.P(u, (ch - 0.3f) / 2f, cd / 2 - 0.25f), Quaternion.LookRotation(f.F, Vector3.up));
                pc.AddComponent<BoxCollider>().size = new Vector3(0.3f, ch - 0.3f, 0.3f);
            }
            c.hood.KeepOut(a.ClubC, Mathf.Max(cw, cd) * 0.6f);
        }

        // a hip roof over u0..u1 x v0..v1, the ridge along the longer side, overhang oh; a soffit underneath
        static void HipRoof(MeshBuilder mb, Frame f, float u0, float u1, float v0, float v1, float yE, float rise, float oh, Color roof, Color soffit)
        {
            float hu = (u1 - u0) / 2f, hv = (v1 - v0) / 2f, um = (u0 + u1) / 2f, vm = (v0 + v1) / 2f;
            Vector3 A = f.P(u0 - oh, yE, v0 - oh), B = f.P(u1 + oh, yE, v0 - oh), C = f.P(u1 + oh, yE, v1 + oh), D = f.P(u0 - oh, yE, v1 + oh);
            Vector3 R0, R1;
            if (hu >= hv) { float r = hu - hv; R0 = f.P(um - r, yE + rise, vm); R1 = f.P(um + r, yE + rise, vm); }
            else { float r = hv - hu; R0 = f.P(um, yE + rise, vm - r); R1 = f.P(um, yE + rise, vm + r); }
            mb.Tint = roof;
            if (hu >= hv) { Face(mb, A, B, R1, R0, Vector3.up); Face(mb, C, D, R0, R1, Vector3.up); TriFace(mb, B, C, R1, Vector3.up); TriFace(mb, D, A, R0, Vector3.up); }
            else { Face(mb, B, C, R1, R0, Vector3.up); Face(mb, D, A, R0, R1, Vector3.up); TriFace(mb, A, B, R0, Vector3.up); TriFace(mb, C, D, R1, Vector3.up); }
            mb.Tint = soffit;
            Face(mb, A, B, C, D, Vector3.down);
        }

        static void BuildTennisCourts(HoodCtx c, MeshBuilder mb, MeshBuilder flat, MeshBuilder link, Transform root, EnclaveAmenity a)
        {
            float y = a.CourtY;
            Vector3 P(Vector2 q, float lift) => new Vector3(q.x, y + lift, q.y);
            Vector2 B(float u, float v) => a.CourtC + a.CU * u + a.CV * v;
            void Rect2(MeshBuilder m, Vector2 o, Vector2 U, Vector2 V, float u0, float u1, float v0, float v1, float lift)
                => Face(m, P(o + U * u0 + V * v0, lift), P(o + U * u1 + V * v0, lift), P(o + U * u1 + V * v1, lift), P(o + U * u0 + V * v1, lift), Vector3.up);
            // the green surround over the whole block, the blue courts, the white lines
            flat.Tint = CourtGreen; Rect2(flat, a.CourtC, a.CU, a.CV, -a.CourtHU, a.CourtHU, -a.CourtHV, a.CourtHV, 0.03f);
            int courts = 0;
            foreach (var ring in a.Courts)
            {
                var o = RingCentroid(ring);
                const float HL = 11.885f, HWd = 5.485f, HWs = 4.115f, SL = 6.40f, LW = 0.05f;
                flat.Tint = CourtBlue; Rect2(flat, o, a.CU, a.CV, -HL, HL, -HWd, HWd, 0.045f);
                flat.Tint = LineWhite;
                foreach (float u in new[] { -HL, HL }) Rect2(flat, o, a.CU, a.CV, u - LW, u + LW, -HWd, HWd, 0.06f);                 // baselines
                foreach (float v in new[] { -HWd, -HWs, HWs, HWd }) Rect2(flat, o, a.CU, a.CV, -HL, HL, v - LW, v + LW, 0.06f);   // side lines
                foreach (float u in new[] { -SL, SL }) Rect2(flat, o, a.CU, a.CV, u - LW, u + LW, -HWs, HWs, 0.06f);                 // service lines
                Rect2(flat, o, a.CU, a.CV, -SL, SL, -LW, LW, 0.06f);                                                                  // center service line
                foreach (float u in new[] { -HL, HL }) Rect2(flat, o, a.CU, a.CV, u - Mathf.Sign(u) * 0.2f - LW, u - Mathf.Sign(u) * 0.2f + LW, -LW, LW, 0.06f);
                // the net: posts 0.914 m outside the doubles lines, 1.07 m high at the posts, 0.914 m in the middle
                var post = new[] { o + a.CV * (HWd + 0.914f), o - a.CV * (HWd + 0.914f) };
                foreach (var pp in post) Post(mb, P(pp, 0f), 1.07f, 0.08f, new Color(0.12f, 0.24f, 0.16f));
                var dark = new Color(0.08f, 0.08f, 0.09f);
                for (int s = 0; s < 2; s++)
                {
                    var a0 = s == 0 ? post[0] : o; var a1 = s == 0 ? o : post[1];
                    float h0 = s == 0 ? 1.07f : 0.914f, h1 = s == 0 ? 0.914f : 1.07f;
                    var nrm = new Vector3(a.CU.x, 0f, a.CU.y);
                    link.Tint = dark;
                    Face(link, P(a0, 0.08f), P(a1, 0.08f), P(a1, h1 - 0.06f), P(a0, h0 - 0.06f), nrm);
                    Face(link, P(a0, 0.08f), P(a1, 0.08f), P(a1, h1 - 0.06f), P(a0, h0 - 0.06f), -nrm);
                    link.Tint = LineWhite;
                    Face(link, P(a0, h0 - 0.06f), P(a1, h1 - 0.06f), P(a1, h1), P(a0, h0), nrm);
                    Face(link, P(a0, h0 - 0.06f), P(a1, h1 - 0.06f), P(a1, h1), P(a0, h0), -nrm);
                }
                courts++;
            }
            // the fence: 3 m chain-link on posts round the block, with two gates left open (the south side toward the pool, the
            // west end toward the pavilion) so nothing that lands inside is trapped; Round 23 follow-ups: breakable panels (no solid stretches)
            const float FH = 3.0f;
            var corners = new[] { B(-a.CourtHU, -a.CourtHV), B(a.CourtHU, -a.CourtHV), B(a.CourtHU, a.CourtHV), B(-a.CourtHU, a.CourtHV) };
            Vector2 Out(int e) => ((corners[e] + corners[(e + 1) % 4]) * 0.5f - a.CourtC).normalized;
            var toPool = (a.DeckC - a.CourtC).normalized;
            var toPav = a.Pavilion >= 0 ? (a.PavC - a.CourtC).normalized : Vector2.left;
            int poolSide = Enumerable.Range(0, 4).OrderByDescending(e => Vector2.Dot(Out(e), toPool)).First();             // the side facing the pool
            int gateEnd = Enumerable.Range(0, 4).Where(e => e != poolSide).OrderByDescending(e => Vector2.Dot(Out(e), toPav)).First();   // the side facing the pavilion
            float fenceM = 0f; int fencePanels = 0;
            var fence = new Breakables();   // Round 23 follow-ups (BUG-003): the fence breaks
            for (int e = 0; e < 4; e++)
            {
                var p0 = corners[e]; var p1 = corners[(e + 1) % 4]; float L = (p1 - p0).magnitude; var dir = (p1 - p0) / L;
                var gaps = new List<(float s0, float s1)>();
                if (e == poolSide) gaps.Add((L / 3f - 2f, L / 3f + 2f));
                if (e == gateEnd) gaps.Add((L / 2f - 2f, L / 2f + 2f));
                var stretches = new List<(float s0, float s1)>(); float at = 0f;
                foreach (var g in gaps.OrderBy(g => g.s0)) { stretches.Add((at, g.s0)); at = g.s1; }
                stretches.Add((at, L));
                var outN = new Vector3(Out(e).x, 0f, Out(e).y);
                foreach (var (s0, s1) in stretches)
                {
                    if (s1 - s0 < 0.5f) continue;
                    var q0 = p0 + dir * s0; var q1 = p0 + dir * s1;
                    // Round 23 follow-ups, BUG-003 (Dan: "The fence around the tennis courts [also] should be breakable"): the fence in
                    // panels of about 3 m, each its own breakable piece (FenceChunk, as the pool's iron fence: a trigger, a speed cost, the
                    // pieces fly and fall, a metal sound): its post, the mesh and top rail, and on the pool side its windscreen
                    int posts = Mathf.Max(1, Mathf.CeilToInt((s1 - s0) / 3f));
                    var o2 = new Vector2(outN.x, outN.z);
                    bool banner = e == poolSide && s1 - s0 > 8f && s0 > 0.1f;
                    for (int k = 0; k < posts; k++)
                    {
                        var qa = Vector2.Lerp(q0, q1, k / (float)posts); var qb = Vector2.Lerp(q0, q1, (k + 1) / (float)posts); var qm = (qa + qb) * 0.5f;
                        var m = fence.Begin(qm);
                        fence.Piece(() => Post(m, P(qa, -0.1f), FH + 0.1f, 0.07f, ChainLink));
                        if (k == posts - 1) fence.Piece(() => Post(m, P(qb, -0.1f), FH + 0.1f, 0.07f, ChainLink));
                        fence.Piece(() =>
                        {
                            Beam(m, P(qa, FH - 0.03f), P(qb, FH - 0.03f), 0.05f, 0.05f, ChainLink);
                            m.Tint = ChainLink;
                            float la = Vector2.Dot(qa - p0, dir), lb = Vector2.Dot(qb - p0, dir);
                            for (float sw = la + 0.12f; sw < lb; sw += 0.16f)
                            {
                                var w0 = p0 + dir * sw; var w1 = w0 + dir * 0.012f;
                                Face(m, P(w0, 0f), P(w1, 0f), P(w1, FH), P(w0, FH), outN); Face(m, P(w0, 0f), P(w1, 0f), P(w1, FH), P(w0, FH), -outN);
                            }
                            for (float h = 0.08f; h < FH; h += 0.16f)
                            {
                                Face(m, P(qa, h), P(qb, h), P(qb, h + 0.012f), P(qa, h + 0.012f), outN); Face(m, P(qa, h), P(qb, h), P(qb, h + 0.012f), P(qa, h + 0.012f), -outN);
                            }
                        });
                        // the green windscreen on the side facing the pool, with the banner (one-sided, facing out) on its longest stretch
                        if (e == poolSide)
                        {
                            var ws = new Color(0.11f, 0.29f, 0.18f);
                            fence.Piece(() =>
                            {
                                m.Tint = ws;
                                Face(m, P(qa + o2 * 0.03f, 0.3f), P(qb + o2 * 0.03f, 0.3f), P(qb + o2 * 0.03f, 2.1f), P(qa + o2 * 0.03f, 2.1f), outN);
                                Face(m, P(qa - o2 * 0.03f, 0.3f), P(qb - o2 * 0.03f, 0.3f), P(qb - o2 * 0.03f, 2.1f), P(qa - o2 * 0.03f, 2.1f), -outN);
                            });
                            if (banner && k == posts / 2)
                            {
                                var bm = HandMadeSignMaterial("Assets/HandMade/Enclave/EnclaveWindscreen.png", "EnclaveWindscreen", false);
                                if (bm != null)
                                {
                                    var bc = (q0 + q1) * 0.5f + o2 * 0.06f;
                                    var plate = SignPlate(root, P(bc, 1.2f), -outN, 1f, bm);   // the quad's front is -z locally: it faces out
                                    plate.name = "Enclave windscreen banner"; plate.transform.localScale = new Vector3(6f, 1.5f, 1f);
                                    fence.Face(plate);   // thrown off with the middle panel
                                }
                            }
                        }
                        fence.End(P(qm, FH / 2f), new Vector3(dir.x, 0f, dir.y), 0.9f, FH, (qb - qa).magnitude);
                        fencePanels++;
                    }
                    fenceM += s1 - s0;
                }
            }
            fence.ToObjects("EnclaveCourtFence", c.mats.House, root, 1.2f, BreakSounds.Kind.Iron);
            // light poles: four, outside the two long sides, between the courts (break like the street lamps)
            int lights = 0;
            var longSides = Enumerable.Range(0, 4).OrderByDescending(e => (corners[(e + 1) % 4] - corners[e]).magnitude).Take(2);
            foreach (int e in longSides)
            {
                var p0 = corners[e]; var p1 = corners[(e + 1) % 4]; var outV = Out(e);
                foreach (float t in new[] { 0.34f, 0.66f })
                {
                    var p = Vector2.Lerp(p0, p1, t) + outV * 0.6f; float gy = c.Y(p.x, p.y);
                    var lb = c.Lamps3D.Begin(p);
                    c.Lamps3D.Piece(() => Post(lb, new Vector3(p.x, gy - 0.1f, p.y), 4.2f, 0.16f, ChainLink), gy + 0.1f);
                    c.Lamps3D.Piece(() => Post(lb, new Vector3(p.x, gy + 4.1f, p.y), 3.9f, 0.13f, ChainLink), gy + 0.1f);
                    c.Lamps3D.Piece(() =>
                    {
                        var head = new Vector3(p.x, gy + 8.0f, p.y) - new Vector3(outV.x, 0f, outV.y) * 0.5f;
                        Beam(lb, new Vector3(p.x, gy + 7.9f, p.y), head, 0.08f, 0.08f, ChainLink);
                        BoxF(lb, MakeFrame(head, -outV, 1f), -0.45f, 0.45f, -0.15f, 0.12f, -0.3f, 0.3f, new Color(0.2f, 0.2f, 0.22f));
                    }, gy + 0.1f);
                    c.Lamps3D.End(new Vector3(p.x, gy + 1.2f, p.y), new Vector3(outV.x, 0f, outV.y), 0.6f, 2.4f);
                    lights++;
                }
            }
            c.hood.Log22.Add($"Enclave tennis courts: {courts} courts (the map data's {a.Courts.Count} pitches; Dan's note says two), block {a.CourtHU * 2f:0.0} x {a.CourtHV * 2f:0.0} m at {y:0.00} m, fence {fenceM:0} m in {fencePanels} breakable panels (Round 23 follow-ups) with two open gates, {lights} light poles");
        }

        // the wooden pavilion by the courts: a raised deck with railings (open toward the courts, with a step), posts, a hip roof
        static void BuildEnclavePavilion(HoodCtx c, MeshBuilder mb, Transform root, EnclaveAmenity a)
        {
            var ring = ToPoints(c.map.buildings[a.Pavilion].footprint);
            if (!BestRectangle(ring, out Vector2 ctr, out Vector2 U, out Vector2 V, out float hl, out float hw)) return;
            float lo = float.MaxValue, hi = float.MinValue;
            foreach (var p in ring) { float g = c.Y(p.x, p.y); lo = Mathf.Min(lo, g); hi = Mathf.Max(hi, g); }
            var toCourts = (a.CourtC - ctr).normalized;
            var front = new[] { U, -U, V, -V }.OrderByDescending(d => Vector2.Dot(d, toCourts)).First();
            var f = MakeFrame(new Vector3(ctr.x, hi + 0.5f, ctr.y), front, 1f);
            bool frontAlongU = Mathf.Abs(Vector2.Dot(front, U)) > 0.7f;
            float hu = frontAlongU ? hw : hl, hv = frontAlongU ? hl : hw;   // u along the front, v toward the courts
            var wood = new Color(0.55f, 0.40f, 0.26f); var deck = new Color(0.60f, 0.46f, 0.31f); var roof = new Color(0.36f, 0.30f, 0.26f);
            BoxF(mb, f, -hu, hu, lo - f.O.y - 0.2f, 0f, -hv, hv, deck);
            mb.Tint = Shade(deck, 0.8f);
            for (float v = -hv + 0.15f; v < hv; v += 0.15f) Face(mb, f.P(-hu, 0.004f, v - 0.008f), f.P(hu, 0.004f, v - 0.008f), f.P(hu, 0.004f, v + 0.008f), f.P(-hu, 0.004f, v + 0.008f), Vector3.up);
            float eave = 2.7f;
            var posts = new List<Vector2>();
            foreach (float u in new[] { -hu + 0.15f, hu - 0.15f }) foreach (float v in new[] { -hv + 0.15f, 0f, hv - 0.15f }) posts.Add(new Vector2(u, v));
            foreach (var p in posts)
            {
                BoxF(mb, f, p.x - 0.1f, p.x + 0.1f, 0f, eave, p.y - 0.1f, p.y + 0.1f, wood);
                var col = new GameObject("Pavilion post"); col.transform.SetParent(root, false);
                col.transform.SetPositionAndRotation(f.P(p.x, eave / 2f, p.y), Quaternion.LookRotation(f.F, Vector3.up));
                col.AddComponent<BoxCollider>().size = new Vector3(0.25f, eave, 0.25f);
            }
            // railings: the back and both sides; the front toward the courts has a 1.6 m opening and a step
            void Rail(Vector3 r0, Vector3 r1)
            {
                Beam(mb, r0 + Vector3.up * 0.95f, r1 + Vector3.up * 0.95f, 0.07f, 0.07f, wood);
                float L = (r1 - r0).magnitude;
                for (float s = 0.12f; s < L; s += 0.12f) { var q = Vector3.Lerp(r0, r1, s / L); Beam(mb, q, q + Vector3.up * 0.93f, 0.035f, 0.035f, wood); }
            }
            Rail(f.P(-hu + 0.1f, 0f, -hv + 0.1f), f.P(hu - 0.1f, 0f, -hv + 0.1f));
            Rail(f.P(-hu + 0.1f, 0f, -hv + 0.1f), f.P(-hu + 0.1f, 0f, hv - 0.1f));
            Rail(f.P(hu - 0.1f, 0f, -hv + 0.1f), f.P(hu - 0.1f, 0f, hv - 0.1f));
            Rail(f.P(-hu + 0.1f, 0f, hv - 0.1f), f.P(-0.8f, 0f, hv - 0.1f));
            Rail(f.P(0.8f, 0f, hv - 0.1f), f.P(hu - 0.1f, 0f, hv - 0.1f));
            float stepY = (Mathf.Min(c.Y(f.P(0f, 0f, hv + 0.5f).x, f.P(0f, 0f, hv + 0.5f).z), f.O.y) - f.O.y) / 2f;
            BoxF(mb, f, -0.8f, 0.8f, stepY - 0.3f, stepY, hv, hv + 0.45f, deck);
            BoxF(mb, f, -hu, hu, eave - 0.2f, eave, -hv, hv, wood);
            HipRoof(mb, f, -hu, hu, -hv, hv, eave, 1.5f, 0.4f, roof, Shade(wood, 1.15f));
            // the deck is solid (Dan: the gazebo may be solid)
            var dc = new GameObject("Pavilion deck"); dc.transform.SetParent(root, false);
            dc.transform.SetPositionAndRotation(f.P(0f, (lo - f.O.y) / 2f, 0f), Quaternion.LookRotation(f.F, Vector3.up));
            dc.AddComponent<BoxCollider>().size = new Vector3(hu * 2f, f.O.y - lo, hv * 2f);
            c.hood.KeepOut(ctr, Mathf.Max(hl, hw) + 1f);
            c.hood.Log22.Add($"Enclave pavilion on building {a.Pavilion}: {hu * 2f:0.0} x {hv * 2f:0.0} m deck {f.O.y - lo:0.0} m above the lowest ground");
        }
    }
}
