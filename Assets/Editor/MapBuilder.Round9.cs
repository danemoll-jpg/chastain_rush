using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// Round 9: Dan's debug report 2026-10-09_14-41-20-911_46947e. Positions below are from his report (game coordinates).
//   BUG-005  the buried house on Chasefield Way: its lot is cut down to the floor level (the terrain itself, so everything on
//            it follows), plus a report-only list of the other buried houses (Data/screenshots/round9/buried_houses.*)
//   BUG-001  the pavilion block by the pool is an open gazebo with six picnic tables; the playground gets a slide and a
//            climbing frame beside the swings
//   BUG-002  a sidewalk from the parking lot to the entrance along New Chastain Road, with benches
//   BUG-003  a wooden fence along New Chastain Road in front of the pool area (breakable, a fence run like the others)
//   BUG-004  the white rail fence carried on to the entrance, and a brick "Chastain Glen" monument either side of it
//   BUG-008  drives planned before the trees (no tree on a drive), mailboxes breakable with most flags down
namespace ChastainRush
{
    public static partial class MapBuilder
    {
        // Dan's report positions that pick a thing (each the nearest of its kind)
        static readonly Vector2 Bug001Playground = new Vector2(2814.27f, 836.23f);   // the car; the block is 15 m ahead of it
        static readonly Vector2 Bug002WalkStart = new Vector2(2794.5f, 790.0f);      // the car stood on the edge of the parking lot
        static readonly Vector2 Bug001Pool = new Vector2(2821.68f, 795.99f);         // Round 12: the car on the pool deck, beside the big building
        static readonly Vector2 Bug005Looking = new Vector2(3061.5f, 1028.5f);       // 15 m ahead of the car along its heading (56 deg)
        const long NcrNorth = 290281670, EntranceOut = 1360406927, EntranceIn = 1161774882;
        const float MonumentZ = 771f;

        sealed class DrivePlan { public List<Vector2> pts; public float[] widths; public bool asphalt; }

        sealed partial class Neighborhood
        {
            public readonly Dictionary<int, DrivePlan> DrivePlans = new Dictionary<int, DrivePlan>();
            public readonly List<(Vector2 a, Vector2 b, float half)> DriveSegs = new List<(Vector2, Vector2, float)>();
            public int TreesMovedOffDrives, TreesDroppedOffDrives;
            public readonly HashSet<int> Replaced = new HashSet<int>();      // generic buildings replaced by something built here
            public int GazeboBuilding = -1;
            public List<Vector2> PoolWalk;                                   // BUG-002
            public readonly List<(Vector2 p, Vector2 face)> Benches = new List<(Vector2, Vector2)>();
            public readonly List<(Vector2 p, Vector2 face)> Monuments = new List<(Vector2, Vector2)>();

            // ------------------------------------------------------------ drives (BUG-008 a, b)

            void PlanDrives()
            {
                foreach (var h in Houses)
                {
                    float u = (h.garageU0 + h.garageU1) / 2f;
                    Vector3 s3 = h.frame.P(u, 0f, h.D / 2 + 0.05f);
                    var S = new Vector2(s3.x, s3.z);
                    Nearest(S, out Vector2 q, out float half, out _, out _);
                    Vector2 toHouse = (S - q).normalized;
                    Vector2 E = q + toHouse * (half + 0.45f);              // the road edge
                    var F2 = new Vector2(h.frame.F.x, h.frame.F.z);
                    float len = (E - S).magnitude;
                    if (len < 2f) continue;
                    Vector2 C1 = S + F2 * Mathf.Clamp(len * 0.55f, 2f, 9f);
                    var pts = new List<Vector2>();
                    int n = Mathf.Max(8, Mathf.CeilToInt(len / 0.75f));
                    for (int i = 0; i <= n; i++) { float t = i / (float)n; pts.Add((1 - t) * (1 - t) * S + 2 * (1 - t) * t * C1 + t * t * E); }
                    float w0 = h.style == HouseStyle.D ? 3.8f : 5.8f;
                    var widths = new float[pts.Count];
                    bool bulb = InBulbZone(E, 1f);   // Round 11: a drive that ends on a cul-de-sac's bulb does not widen at its mouth
                    for (int i = 0; i < pts.Count; i++) { float t = i / (float)n; widths[i] = w0 + (t > 0.78f && !bulb ? (t - 0.78f) / 0.22f * 1.5f : 0f); }
                    DrivePlans[h.index] = new DrivePlan { pts = pts, widths = widths, asphalt = h.dan || Hash01(h.seed, 120) < 0.28f };
                    for (int i = 0; i + 1 < pts.Count; i++) DriveSegs.Add((pts[i], pts[i + 1], widths[i] * 0.5f));
                }
            }

            bool NearestDrive(Vector2 p, out Vector2 foot, out float half, out float dist)
            {
                foot = p; half = 0f; dist = float.MaxValue;
                foreach (var d in DriveSegs)
                {
                    var ab = d.b - d.a; float t = ab.sqrMagnitude < 1e-6f ? 0f : Mathf.Clamp01(Vector2.Dot(p - d.a, ab) / ab.sqrMagnitude);
                    var c = d.a + ab * t; float dd = (c - p).magnitude;
                    if (dd < dist) { dist = dd; foot = c; half = d.half; }
                }
                return dist < float.MaxValue;
            }
            public bool OnDrivePlan(Vector2 p, float margin) => NearestDrive(p, out _, out float half, out float d) && d < half + margin;

            // A tree from the canopy data that stands on a drive is moved sideways off it into the yard (the nearer side
            // first); if neither side has room it is left out. Called by TreeBuilder before RejectTree.
            public Vector2 MoveTree(float x, float z)
            {
                var p = MoveTreeOffDrive(x, z);
                return Present && Bounds.Contains(p) ? MoveTreeOffLine(p) : p;   // Round 13: then off a shortcut's line
            }

            Vector2 MoveTreeOffDrive(float x, float z)
            {
                var p = new Vector2(x, z);
                if (!Present || !Bounds.Contains(p) || !NearestDrive(p, out Vector2 foot, out float half, out float d) || d >= half + 0.5f) return p;
                var away = d > 1e-3f ? (p - foot) / d : Vector2.right;
                foreach (float sgn in new[] { 1f, -1f })
                {
                    var np = foot + away * sgn * (half + 1.3f);
                    if (!TreeBlocked(np) && !OnDrivePlan(np, 0.5f)) { TreesMovedOffDrives++; return np; }
                }
                TreesDroppedOffDrives++;
                return p;   // RejectTree drops it (it is on a drive)
            }

            // ------------------------------------------------------------ the pool side of New Chastain Road (BUG-002, 003, 004)

            Road RoadByOsm(long id) { foreach (var r in map.roads) if (r.osm_id == id) return r; return null; }

            static Vector2 PointAtZ(List<Vector2> line, float z, out Vector2 tangent)
            {
                tangent = Vector2.up;
                for (int i = 0; i + 1 < line.Count; i++)
                {
                    var a = line[i]; var b = line[i + 1];
                    if ((a.y - z) * (b.y - z) <= 0f && Mathf.Abs(b.y - a.y) > 1e-4f)
                    {
                        tangent = (b - a).normalized;
                        return Vector2.Lerp(a, b, (z - a.y) / (b.y - a.y));
                    }
                }
                return line[line.Count - 1];
            }

            static float DistToLine(Vector2 p, List<Vector2> line)
            {
                float best = float.MaxValue;
                for (int i = 0; i + 1 < line.Count; i++)
                {
                    var a = line[i]; var ab = line[i + 1] - a; float t = ab.sqrMagnitude < 1e-6f ? 0f : Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
                    best = Mathf.Min(best, (a + ab * t - p).magnitude);
                }
                return best;
            }

            void PlanPoolStreet()
            {
                var ncr = RoadByOsm(NcrNorth); var outR = RoadByOsm(EntranceOut); var inR = RoadByOsm(EntranceIn);
                if (ncr == null || outR == null || inR == null) { Debug.LogWarning("[Round9] New Chastain Road or the entrance lanes not found: no sidewalk, pool fence or monuments"); return; }
                float hw = ncr.width * 0.5f;
                var line = Densify(ToPoints(ncr.points), 2f);
                // the north side of the westbound carriageway (the pool and pond side)
                List<Vector2> Offset(float off)
                {
                    var o = new List<Vector2>();
                    for (int i = 0; i < line.Count; i++)
                    {
                        Vector2 dIn = i > 0 ? (line[i] - line[i - 1]).normalized : (line[1] - line[0]).normalized;
                        Vector2 dOut = i < line.Count - 1 ? (line[i + 1] - line[i]).normalized : dIn;
                        Vector2 t = (dIn + dOut).normalized; var nrm = new Vector2(-t.y, t.x);
                        if (nrm.y < 0f) nrm = -nrm;
                        o.Add(line[i] + nrm * off);
                    }
                    o.Sort((a, b) => a.x.CompareTo(b.x));   // west to east
                    return o;
                }
                var outLine = ToPoints(outR.points); var inLine = ToPoints(inR.points);
                float outHalf = outR.width * 0.5f;
                // --- the sidewalk. Round 11 (BUG-009, BUG-011, Dan: "this sidewalk is in the wrong place. It is the
                // neighborhood sidewalk and should be closer to the lake", "closer to the lake/pool and inside the fence"): it
                // no longer runs along the road. From the parking lot's edge it runs inside the wooden fence along the road,
                // past the pool's fence (2.2 m off it) and along the lake shore (4.5 m off the water), then out to the entrance
                // lane's verge through a gap in the rail fence. Its height follows the ground.
                var fenceAt = Offset(hw + 4.45f);
                var pondC = PondRing.Count > 2 ? RingCentroid(PondRing) : PoolCentre;
                var clubhouse = ClubhouseAt ?? PoolCentre + (PoolCentre - pondC).normalized * (PoolDeckHalfU + 6.0f);   // as BuildPoolSite places it
                bool Blocked(Vector2 p)
                {
                    if (PondRing.Count > 2 && DistToPond(p) < 4.5f) return true;
                    if (PoolDeckHalfU > 0f) { var d = p - PoolCentre; if (Mathf.Abs(Vector2.Dot(d, PoolU)) < PoolDeckHalfU + 0.8f + 2.2f && Mathf.Abs(Vector2.Dot(d, PoolV)) < PoolDeckHalfV + 0.8f + 2.2f) return true; }
                    if ((p - clubhouse).magnitude < 5.5f || InRing(p, ParkingRing)) return true;
                    return false;
                }
                // where the walk meets the entrance's outbound lane: on its west verge, 13 m north of the west monument's line
                var laneEnd = PointAtZ(outLine, MonumentZ + 13f, out var lt);
                var laneW = new Vector2(-lt.y, lt.x); if (laneW.x > 0f) laneW = -laneW;
                var walkEnd = laneEnd + laneW * (outHalf + 1.2f);
                var shore = new List<Vector2>();
                float xLast = walkEnd.x - 12f;
                for (float x = Bug002WalkStart.x + 6f; x <= xLast; x += 2f)
                {
                    float zf = PointAtX(fenceAt, x) + 2.6f, best = zf;
                    for (float z = zf; z < zf + 30f; z += 0.5f) { if (Blocked(new Vector2(x, z))) break; best = z; }
                    shore.Add(new Vector2(x, best));
                }
                if (shore.Count < 4) return;
                // no tight detours (round the clubhouse): each point at the lowest of its neighbours within 14 m
                shore = shore.Select((q, i) => new Vector2(q.x, shore.Where((o, j) => Mathf.Abs(j - i) <= 7).Min(o => o.y))).ToList();
                PoolWalk = new List<Vector2> { Bug002WalkStart };
                PoolWalk.AddRange(shore);
                PoolWalk.Add(walkEnd + laneW * 3f); PoolWalk.Add(walkEnd);
                PoolWalk = Densify(Smooth(Smooth(PoolWalk, 2), 3), 2f);
                // --- benches on the lake side of the walk, facing the lake, along the shore
                foreach (float bx in new[] { 2862f, 2880f, 2898f })
                {
                    int k = PoolWalk.FindIndex(p => p.x >= bx); if (k < 1) continue;
                    var dir = (PoolWalk[k] - PoolWalk[k - 1]).normalized; var north = new Vector2(-dir.y, dir.x); if (north.y < 0f) north = -north;
                    var bp = PoolWalk[k] + north * 1.45f;
                    if (DistToPond(bp) < 2.0f) continue;
                    var toLake = ClosestOnRing(bp, PondRing) - bp;
                    Benches.Add((bp, toLake.normalized));
                    KeepOut(bp, 1.4f);
                }
                var join = PoolWalk[0];
                // --- the wooden fence in front of the pool area, behind the sidewalk, with a gap where the walk comes down
                var fenceLine = Offset(hw + 4.45f).Where(p => p.x >= 2776f && p.x <= 2860f).ToList();
                var run = new FenceRun { kind = FenceKind.Privacy, seed = 9003 };   // Round 11: whole (the walk stays inside it)
                foreach (var p in fenceLine) run.pts.Add(p);
                if (run.pts.Count >= 2) Fences.Add(run);
                // --- the two brick monuments, either side of the entrance (outbound lane on the west, inbound on the east),
                // facing New Chastain Road. Round 11 (BUG-012, Dan: "they should be slanted the other way so that you see them
                // when you are pulling into the neighborhood"): each is turned 15 degrees in, toward the entrance's middle
                // (Round 9 had them turned 15 degrees out)
                var nd = (line[line.Count - 1] - line[0]).normalized; if (nd.x < 0f) nd = -nd;      // east
                var south = new Vector2(nd.y, -nd.x); if (south.y > 0f) south = -south;
                var pw = PointAtZ(outLine, MonumentZ, out var tw); var pe = PointAtZ(inLine, MonumentZ, out var te);
                var westN = new Vector2(-tw.y, tw.x); if (westN.x > 0f) westN = -westN;
                var eastN = new Vector2(-te.y, te.x); if (eastN.x < 0f) eastN = -eastN;
                Vector2 mw = pw + westN * (outHalf + 4.4f), me = pe + eastN * (inR.width * 0.5f + 4.4f);
                Monuments.Add((mw, (south + nd * 0.27f).normalized));
                Monuments.Add((me, (south - nd * 0.27f).normalized));
                foreach (var m in Monuments) KeepOut(m.p, 4.0f);
            }

            // Round 11: the height (z) of a west-to-east line at x
            static float PointAtX(List<Vector2> line, float x)
            {
                for (int i = 0; i + 1 < line.Count; i++)
                {
                    var a = line[i]; var b = line[i + 1];
                    if ((a.x - x) * (b.x - x) <= 0f && Mathf.Abs(b.x - a.x) > 1e-4f) return Mathf.Lerp(a.y, b.y, (x - a.x) / (b.x - a.x));
                }
                return x < line[0].x ? line[0].y : line[line.Count - 1].y;
            }

            static Vector2 ClosestOnRing(Vector2 p, List<Vector2> ring)
            {
                Vector2 best = p; float bd = float.MaxValue;
                for (int i = 0; i < ring.Count; i++)
                {
                    var a = ring[i]; var ab = ring[(i + 1) % ring.Count] - a; float t = ab.sqrMagnitude < 1e-6f ? 0f : Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
                    var c = a + ab * t; float d = (c - p).sqrMagnitude; if (d < bd) { bd = d; best = c; }
                }
                return best;
            }

            static List<Vector2> Smooth(List<Vector2> pts, int iterations)
            {
                var P = pts;
                for (int it = 0; it < iterations; it++)
                {
                    var Q = new List<Vector2> { P[0] };
                    for (int i = 0; i + 1 < P.Count; i++) { Q.Add(P[i] * 0.75f + P[i + 1] * 0.25f); Q.Add(P[i] * 0.25f + P[i + 1] * 0.75f); }
                    Q.Add(P[P.Count - 1]); P = Q;
                }
                return P;
            }

            // the rail fence on the entrance's outbound lane, on the pond side: carried on south from the pond to the
            // west monument (BUG-004: "the white fence should extend to the entrance")
            bool RailExtension(Road r, Vector2 p)
            {
                if (r.osm_id != EntranceOut || Monuments.Count == 0) return false;
                return p.x < Monuments[0].p.x + 1.5f && p.y > Monuments[0].p.y + 0.9f;
            }

            // ------------------------------------------------------------ the gazebo (BUG-001)

            void PlanGazebo()
            {
                float best = 30f;
                for (int i = 0; i < map.buildings.Length; i++)
                {
                    var b = map.buildings[i];
                    if (b.type != "pavilion") continue;
                    var ring = ToPoints(b.footprint); if (ring.Count < 3) continue;
                    Vector2 c = Vector2.zero; foreach (var p in ring) c += p; c /= ring.Count;
                    float d = (c - Bug001Playground).magnitude;
                    if (d < best) { best = d; GazeboBuilding = i; }
                }
                if (GazeboBuilding < 0) return;
                Replaced.Add(GazeboBuilding);
                var r2 = ToPoints(map.buildings[GazeboBuilding].footprint);
                Vector2 cc = Vector2.zero; foreach (var p in r2) cc += p; cc /= r2.Count;
                KeepOut(cc, 6.5f);
            }

            // ------------------------------------------------------------ BUG-005: the buried house, and the list of others

            [Serializable] class BuriedRow { public int building; public float x, z, buried_m, floor_y; public string style; public bool regraded; public float after_m; }
            [Serializable] class BuriedFile { public string note; public List<BuriedRow> houses = new List<BuriedRow>(); }

            // how far the ground stands above the floor along the house's walls (the most, sampled every 0.5 m)
            float Burial(HouseSpec h)
            {
                float worst = float.MinValue;
                var r = new Vector2(h.frame.R.x, h.frame.R.z) * h.frame.M; var f = new Vector2(h.frame.F.x, h.frame.F.z);
                for (float u = -h.W / 2; u <= h.W / 2 + 0.01f; u += 0.5f)
                    foreach (float v in new[] { -h.D / 2 - 0.15f, h.D / 2 + 0.15f })
                    { var p = h.footprintCentre + r * u + f * v; worst = Mathf.Max(worst, terrain.Height(p.x, p.y) - h.floorY); }
                for (float v = -h.D / 2; v <= h.D / 2 + 0.01f; v += 0.5f)
                    foreach (float u in new[] { -h.W / 2 - 0.15f, h.W / 2 + 0.15f })
                    { var p = h.footprintCentre + r * u + f * v; worst = Mathf.Max(worst, terrain.Height(p.x, p.y) - h.floorY); }
                return worst;
            }

            public void RegradeAndReport()
            {
                if (!Present) return;
                var file = new BuriedFile { note = "Round 9 (BUG-005): area houses whose walls are buried by more than 0.5 m (ground above the floor level at the walls), before any change. Only the reported house was changed (regraded = true)." };
                HouseSpec target = null; float bd = 12f;
                foreach (var h in Houses) { float d = (h.footprintCentre - Bug005Looking).magnitude; if (d < bd) { bd = d; target = h; } }
                var before = new Dictionary<int, float>();
                foreach (var h in Houses) before[h.index] = Burial(h);
                if (target != null) CutLot(target);
                CutUnderDanDeck();   // Round 10 (BUG-001): no grass through Dan's deck
                GradeShortcuts();      // Round 12: the shortcuts' grass runs (before the prisms' cuts, which go under their tops)
                CutUnderShortcuts();   // Round 10: the ground under the shortcut prisms marked "cut" (MapBuilder.Shortcuts.cs)
                foreach (var h in Houses)
                {
                    float b = before[h.index];
                    if (b <= 0.5f && h != target) continue;
                    file.houses.Add(new BuriedRow { building = h.index, x = h.footprintCentre.x, z = h.footprintCentre.y, buried_m = b, floor_y = h.floorY, style = h.dan ? "Dan" : h.style.ToString(), regraded = h == target, after_m = h == target ? Burial(h) : b });
                }
                file.houses.Sort((a, b) => b.buried_m.CompareTo(a.buried_m));
                string dir = Path.Combine(ProjectRoot, "Data/screenshots/round9");
                Directory.CreateDirectory(dir);
                File.WriteAllText(Path.Combine(dir, "buried_houses.json"), JsonUtility.ToJson(file, true));
                Debug.Log($"[Round9] buried houses (> 0.5 m): {file.houses.Count(h => !h.regraded && h.buried_m > 0.5f)} besides the reported one; reported house {(target != null ? target.index.ToString() : "NOT FOUND")}: " +
                          (target != null ? $"{before[target.index]:0.00} m buried before, {Burial(target):0.00} m after" : ""));
            }

            // Round 10, item 2: where a shortcut's ramp or landing slope runs below the ground (the crest behind house 1763, the
            // rising front yard under its ramp), the ground is cut down to 6 cm under the prism's top, with a bank of about 40
            // degrees outside it; never within 1.2 m of a house. Cut only, never filled.
            void CutUnderShortcuts()
            {
                if (ShortcutData == null) return;
                foreach (var sc in ShortcutData.shortcuts)
                    foreach (var pr in sc.prisms)
                    {
                        if (!pr.cut) continue;
                        var poly = ToPoints(pr.poly);
                        var O = new Vector2(pr.origin[0], pr.origin[1]); var U = new Vector2(pr.axis[0], pr.axis[1]).normalized;
                        float s0 = float.MaxValue, s1 = float.MinValue; float x0 = float.MaxValue, x1 = float.MinValue, z0 = float.MaxValue, z1 = float.MinValue;
                        foreach (var q in poly) { float sq = Vector2.Dot(q - O, U); s0 = Mathf.Min(s0, sq); s1 = Mathf.Max(s1, sq); x0 = Mathf.Min(x0, q.x); x1 = Mathf.Max(x1, q.x); z0 = Mathf.Min(z0, q.y); z1 = Mathf.Max(z1, q.y); }
                        const float Bank = 0.85f, Reach = 4.5f;   // Round 11: 4.5 m (the cut behind 1763 is up to 3.2 m deep now)
                        int changed = 0; float deepest = 0f;
                        EditTerrain(terrain, Rect.MinMaxRect(x0 - Reach, z0 - Reach, x1 + Reach, z1 + Reach), (x, z, y) =>
                        {
                            var p = new Vector2(x, z);
                            float d = InPolygon(p, poly) ? 0f : DistToRing(p, poly);
                            if (d > Reach) return y;
                            if (InsideHouse(p, 1.2f, null)) return y;
                            float allowed = Profile(pr.profile, Mathf.Clamp(Vector2.Dot(p - O, U), s0, s1)) - 0.06f + d * Bank;
                            if (y <= allowed) return y;
                            changed++; deepest = Mathf.Max(deepest, y - allowed);
                            return allowed;
                        });
                        Debug.Log($"[Round10] {pr.name}: ground cut to its top, {changed} terrain samples, by up to {deepest:0.00} m");
                    }
            }

            void GradeShortcuts()
            {
                if (ShortcutData == null) return;
                // Round 12 (Dan: "It should just be grass"): a shortcut's grass run. Within half_width of its line the ground is
                // set to the profile (cut or filled); outside, banks of about 40 degrees lead back to the natural ground. Never
                // within 0.15 m of a house, and never below a house's foundation within 1.5 m of it.
                foreach (var sc in ShortcutData.shortcuts)
                    foreach (var gr in sc.grades ?? new ScGrade[0])
                    {
                        var line = ToPoints(gr.path); var cum = new float[line.Count];
                        for (int i = 1; i < line.Count; i++) cum[i] = cum[i - 1] + (line[i] - line[i - 1]).magnitude;
                        const float Bank = 0.85f, Reach = 6f;
                        float x0 = float.MaxValue, x1 = float.MinValue, z0 = float.MaxValue, z1 = float.MinValue;
                        foreach (var q in line) { x0 = Mathf.Min(x0, q.x); x1 = Mathf.Max(x1, q.x); z0 = Mathf.Min(z0, q.y); z1 = Mathf.Max(z1, q.y); }
                        float m = gr.half_width + Reach; int changed = 0; float cutM = 0f, fillM = 0f;
                        EditTerrain(terrain, Rect.MinMaxRect(x0 - m, z0 - m, x1 + m, z1 + m), (x, z, y) =>
                        {
                            var p = new Vector2(x, z); float best = float.MaxValue, st = 0f;
                            for (int i = 0; i + 1 < line.Count; i++)
                            {
                                var ab = line[i + 1] - line[i]; float t = Mathf.Clamp01(Vector2.Dot(p - line[i], ab) / Mathf.Max(ab.sqrMagnitude, 1e-6f));
                                float dd = (line[i] + ab * t - p).sqrMagnitude;
                                if (dd < best) { best = dd; st = cum[i] + t * ab.magnitude; }
                            }
                            float d = Mathf.Max(0f, Mathf.Sqrt(best) - gr.half_width);
                            if (d > Reach || InsideHouse(p, 0.15f, null) || RoadClearance(p) < 0.3f) return y;   // never on a street
                            float target = Profile(gr.profile, st);
                            float ny = Mathf.Clamp(y, target - d * Bank, target + d * Bank);
                            ny = Mathf.Lerp(y, ny, Mathf.Clamp01((st - gr.profile[0]) / 6f));   // faded in over the first 6 m, off the street's edge
                            // grounding: within 1.5 m of a house the ground is never cut below its foundation (no gap under a wall)
                            foreach (var h in Houses)
                            {
                                var dh = p - h.footprintCentre; if (dh.sqrMagnitude > 400f) continue;
                                if (Mathf.Abs(Vector2.Dot(dh, new Vector2(h.frame.R.x, h.frame.R.z))) < h.W / 2 + 1.5f && Mathf.Abs(Vector2.Dot(dh, new Vector2(h.frame.F.x, h.frame.F.z))) < h.D / 2 + 1.5f)
                                    ny = Mathf.Max(ny, Mathf.Min(y, h.baseY + 0.15f));
                            }
                            if (Mathf.Abs(ny - y) > 1e-3f) { changed++; cutM = Mathf.Max(cutM, y - ny); fillM = Mathf.Max(fillM, ny - y); }
                            return ny;
                        });
                        Debug.Log($"[Round12] {gr.label}: ground graded to the line's profile, {changed} terrain samples, cut up to {cutM:0.00} m, filled up to {fillM:0.00} m");
                    }
            }

            // Round 10 (BUG-001): the ground behind Dan's house stood within a few centimetres of the deck's boards (the deck is
            // capped 3 cm under the door sill), so the terrain's surface, simplified at a distance (heightmapPixelError), showed
            // through as a grassy patch. The deck's height is set here from the natural ground (sampled every 0.25 m), then the
            // ground under the deck and 0.2 m round it is cut to 0.3 m under the boards, with a short bank outside (cut only).
            public float DanDeckTop = float.NaN;   // against the floor; read by BuildDanLot
            void CutUnderDanDeck()
            {
                var h = Dan; if (h == null) return;
                float u0 = -DanW / 2 - 0.3f, u1 = -0.2f, v0 = -DanD / 2 - 4.2f, v1 = -DanD / 2;   // the deck in the house frame
                float g = float.MinValue;
                for (float u = u0; u <= u1 + 0.01f; u += 0.25f) for (float v = v0; v <= v1 + 0.01f; v += 0.25f) { var q = h.frame.P(u, 0f, v); g = Mathf.Max(g, terrain.Height(q.x, q.z)); }
                DanDeckTop = Mathf.Clamp(g - h.floorY + 0.12f, -0.25f, -0.03f);   // never above the door sill
                float pad = h.floorY + DanDeckTop - 0.3f;
                var r = new Vector2(h.frame.R.x, h.frame.R.z) * h.frame.M; var f = new Vector2(h.frame.F.x, h.frame.F.z);
                float cu = (u0 + u1) / 2f, cv = (v0 + v1) / 2f, hu = (u1 - u0) / 2f + 0.2f, hv = (v1 - v0) / 2f + 0.2f;
                var centre = h.footprintCentre + r * cu + f * cv;
                const float Bank = 0.85f;
                int changed = 0; float deepest = 0f;
                var area = Rect.MinMaxRect(centre.x - 14f, centre.y - 14f, centre.x + 14f, centre.y + 14f);
                EditTerrain(terrain, area, (x, z, y) =>
                {
                    var d = new Vector2(x, z) - centre;
                    float ou = Mathf.Max(0f, Mathf.Abs(Vector2.Dot(d, r)) - hu), ov = Mathf.Max(0f, Mathf.Abs(Vector2.Dot(d, f)) - hv);
                    float allowed = pad + Mathf.Sqrt(ou * ou + ov * ov) * Bank;
                    if (y <= allowed) return y;
                    changed++; deepest = Mathf.Max(deepest, y - allowed);
                    return allowed;
                });
                Debug.Log($"[Round10] BUG-001: Dan's deck {DanDeckTop:+0.00;-0.00} m against the floor (highest ground under it {g - h.floorY:+0.00;-0.00}); ground under it cut to 0.30 m below the boards, {changed} terrain samples, by up to {deepest:0.00} m");
            }

            // Cuts the ground of one lot down to the house: a level pad (the house rectangle and 1.4 m round it) just under the
            // floor, and a bank rising from its edge at about 40 degrees back to the natural ground. Only cuts, never fills, so the
            // drive, the front yard and the street are untouched. Everything built later samples the new ground (shrubs, the drive,
            // the mailbox, fences, trees), so it all sits on it.
            void CutLot(HouseSpec h)
            {
                float pad = h.floorY - 0.12f;
                var r = new Vector2(h.frame.R.x, h.frame.R.z); var f = new Vector2(h.frame.F.x, h.frame.F.z);
                float hu = h.W / 2 + 1.4f, hv = h.D / 2 + 1.4f;
                const float Bank = 0.85f, Reach = 6f;
                var area = Rect.MinMaxRect(h.footprintCentre.x - hu - hv - Reach, h.footprintCentre.y - hu - hv - Reach, h.footprintCentre.x + hu + hv + Reach, h.footprintCentre.y + hu + hv + Reach);
                int changed = 0; float deepest = 0f;
                EditTerrain(terrain, area, (x, z, y) =>
                {
                    var d = new Vector2(x, z) - h.footprintCentre;
                    float ou = Mathf.Max(0f, Mathf.Abs(Vector2.Dot(d, r)) - hu), ov = Mathf.Max(0f, Mathf.Abs(Vector2.Dot(d, f)) - hv);
                    float outside = Mathf.Sqrt(ou * ou + ov * ov);
                    float allowed = pad + outside * Bank;
                    if (y <= allowed) return y;
                    changed++; deepest = Mathf.Max(deepest, y - allowed);
                    return allowed;
                });
                Debug.Log($"[Round9] BUG-005: house {h.index} (style {h.style}, floor {h.floorY:0.00}): its lot cut to {pad:0.00} m, {changed} terrain samples lowered, by up to {deepest:0.00} m");
            }
        }

        // Changes the terrain's own heights (every tile a sample of which lies in 'area'; tiles share their edge samples, so they
        // stay joined). 'height' gets the world x, z and the current height and returns the new one.
        static void EditTerrain(TerrainGrid g, Rect area, Func<float, float, float, float> height)
        {
            foreach (var tile in g.Tiles)
            {
                var data = tile.terrainData; int res = data.heightmapResolution; var size = data.size; var pos = tile.transform.position;
                int x0 = Mathf.Clamp(Mathf.FloorToInt((area.xMin - pos.x) / size.x * (res - 1)), 0, res - 1), x1 = Mathf.Clamp(Mathf.CeilToInt((area.xMax - pos.x) / size.x * (res - 1)), 0, res - 1);
                int z0 = Mathf.Clamp(Mathf.FloorToInt((area.yMin - pos.z) / size.z * (res - 1)), 0, res - 1), z1 = Mathf.Clamp(Mathf.CeilToInt((area.yMax - pos.z) / size.z * (res - 1)), 0, res - 1);
                if (x1 <= x0 || z1 <= z0) continue;
                if (area.xMax < pos.x || area.xMin > pos.x + size.x || area.yMax < pos.z || area.yMin > pos.z + size.z) continue;
                var hts = data.GetHeights(x0, z0, x1 - x0 + 1, z1 - z0 + 1);
                bool any = false;
                for (int j = 0; j <= z1 - z0; j++)
                    for (int i = 0; i <= x1 - x0; i++)
                    {
                        float wx = pos.x + (x0 + i) / (float)(res - 1) * size.x, wz = pos.z + (z0 + j) / (float)(res - 1) * size.z;
                        float y = pos.y + hts[j, i] * size.y;
                        float ny = height(wx, wz, y);
                        if (Mathf.Abs(ny - y) > 1e-4f) { hts[j, i] = Mathf.Clamp01((ny - pos.y) / size.y); any = true; }
                    }
                if (any) data.SetHeights(x0, z0, hts);
            }
        }

        // ------------------------------------------------------------ breakable props (mailboxes), FenceChunk per 128 m cell

        sealed class Breakables
        {
            sealed class Cell { public MeshBuilder mb = new MeshBuilder(); public List<FenceChunk.Piece> pieces = new List<FenceChunk.Piece>(); public List<FenceChunk.Panel> panels = new List<FenceChunk.Panel>(); public List<(Vector3 c, Vector3 along, float w, float h, float d)> trig = new List<(Vector3, Vector3, float, float, float)>(); }
            readonly Dictionary<Vector2Int, Cell> cells = new Dictionary<Vector2Int, Cell>();
            Cell cur; int first;
            public int Count;
            public MeshBuilder Begin(Vector2 p)
            {
                var key = new Vector2Int(Mathf.FloorToInt(p.x / 128f), Mathf.FloorToInt(p.y / 128f));
                if (!cells.TryGetValue(key, out cur)) cells[key] = cur = new Cell();
                first = cur.pieces.Count;
                return cur.mb;
            }
            public void Piece(Action build, float restY = 0f)   // Round 10: restY, where its debris comes to rest (world y)
            {
                int v0 = cur.mb.Count; build(); int cnt = cur.mb.Count - v0;
                Vector3 centre = Vector3.zero; foreach (var v in cur.mb.VertexRange(v0, cnt)) centre += v; centre /= Mathf.Max(cnt, 1);
                cur.pieces.Add(new FenceChunk.Piece { vertexStart = v0, vertexCount = cnt, centre = centre, restY = restY });
            }
            public void End(Vector3 centre, Vector3 along, float width, float height, float depth = -1f)   // Round 10: depth along 'along' (default: the width)
            {
                cur.panels.Add(new FenceChunk.Panel { firstPiece = first, pieceCount = cur.pieces.Count - first, centre = centre });
                cur.trig.Add((centre, along, width, height, depth > 0f ? depth : width));
                Count++;
            }
            public Transform ToObjects(string name, Material mat, Transform parent, float debrisDrop)
            {
                var root = new GameObject(name).transform; root.SetParent(parent, false);
                foreach (var kv in cells)
                {
                    var cell = kv.Value; if (cell.panels.Count == 0) continue;
                    var go = cell.mb.ToObject($"{name}_{kv.Key.x}_{kv.Key.y}", mat, root, false, false);
                    GameObjectUtility.SetStaticEditorFlags(go, 0);   // the mesh is rewritten at runtime when one breaks
                    var chunk = go.AddComponent<FenceChunk>();
                    chunk.source = go.GetComponent<MeshFilter>().sharedMesh; chunk.material = mat;
                    chunk.pieces = cell.pieces.ToArray(); chunk.panels = cell.panels.ToArray(); chunk.debrisDrop = debrisDrop;
                    for (int i = 0; i < cell.trig.Count; i++)
                    {
                        var t = cell.trig[i];
                        var pg = new GameObject("m"); pg.transform.SetParent(go.transform, false);
                        pg.transform.SetPositionAndRotation(t.c, Quaternion.LookRotation(t.along, Vector3.up));
                        var bc = pg.AddComponent<BoxCollider>(); bc.isTrigger = true; bc.size = new Vector3(t.w, t.h, t.d);
                        var fp = pg.AddComponent<FencePanel>(); fp.chunk = chunk; fp.index = i;
                    }
                }
                return root;
            }
        }

        // ------------------------------------------------------------ building it

        static void BuildRound9(HoodCtx c)
        {
            var hood = c.hood;
            var root = new GameObject("Round9").transform; root.SetParent(c.root, false);
            var solid = new MeshBuilder();
            // --- BUG-002: the sidewalk and its benches
            if (hood.PoolWalk != null)
            {
                c.Walks.Tint = ConcreteLight;
                foreach (var piece in Runs(hood.PoolWalk, 96f))
                {
                    var w = new float[piece.Count]; for (int k = 0; k < w.Length; k++) w[k] = 1.4f;
                    RibbonGridMax(c.Walks.At(piece[piece.Count / 2]), piece, w, WalkLift, c.terrain, 2);   // Round 11: the sidewalk material, clear of the lawn
                }
                float len = 0f; for (int i = 1; i < hood.PoolWalk.Count; i++) len += Vector2.Distance(hood.PoolWalk[i - 1], hood.PoolWalk[i]);
                c.WalkMetres += Mathf.RoundToInt(len);
                foreach (var (p, face) in hood.Benches) Bench(c.Props.At(p), MakeFrame(new Vector3(p.x, c.Y(p.x, p.y), p.y), face, 1f));
                c.Log.Append($"BUG-002 sidewalk {len:0} m from the parking lot to the entrance, {hood.Benches.Count} benches; ");
                AddView(c, "r9_walk", hood.PoolWalk[hood.PoolWalk.Count / 2], Vector2.right, hood.PoolWalk[hood.PoolWalk.Count / 2]);
            }
            // --- BUG-004: the monuments
            if (hood.Monuments.Count > 0)
            {
                var signMat = MonumentSignMaterial();
                foreach (var (p, face) in hood.Monuments)
                {
                    var f = MakeFrame(new Vector3(p.x, 0f, p.y), face, 1f);
                    float lo = float.MaxValue, hi = float.MinValue;
                    foreach (float u in new[] { -3.4f, 0f, 3.4f }) foreach (float v in new[] { -0.4f, 0.4f }) { var q = f.P(u, 0f, v); float g = c.Y(q.x, q.z); lo = Mathf.Min(lo, g); hi = Mathf.Max(hi, g); }
                    f.O.y = hi + 0.05f;   // stands on the highest ground under it; the brick goes down below the lowest
                    Monument(solid, f, lo - f.O.y - 0.4f);
                    var col = new GameObject("Monument collider"); col.transform.SetParent(root, false);
                    col.transform.SetPositionAndRotation(f.P(0f, 0.9f, 0f), Quaternion.LookRotation(f.F, Vector3.up));
                    col.AddComponent<BoxCollider>().size = new Vector3(6.8f, 2.6f, 0.75f);
                    if (signMat != null)
                    {
                        var plate = SignPlate(root, f.P(0f, 1.32f, 0.37f), -f.F, 1f, signMat);   // faces the road (the quad's front is -z locally)
                        plate.name = "Monument sign";
                        plate.transform.localScale = new Vector3(2.7f, 1.9f, 1f);
                    }
                }
                c.Log.Append($"BUG-004 two brick monuments at ({hood.Monuments[0].p.x:0.0}, {hood.Monuments[0].p.y:0.0}) and ({hood.Monuments[1].p.x:0.0}, {hood.Monuments[1].p.y:0.0}); ");
                AddView(c, "r9_monument", hood.Monuments[0].p, hood.Monuments[0].face, hood.Monuments[0].p);
            }
            // --- BUG-001: the gazebo
            if (hood.GazeboBuilding >= 0) BuildGazebo(c, solid, root);
            solid.ToObject("Round9Solid", c.mats.House, root, false, true);
        }

        static void Bench(MeshBuilder mb, Frame f)
        {
            var wood = new Color(0.45f, 0.32f, 0.20f); var iron = new Color(0.09f, 0.09f, 0.10f);
            foreach (float u in new[] { -0.75f, 0.75f })
            {
                BoxF(mb, f, u - 0.04f, u + 0.04f, 0f, 0.44f, 0.15f, 0.2f, iron);
                BoxF(mb, f, u - 0.04f, u + 0.04f, 0f, 0.86f, -0.24f, -0.19f, iron);
                BoxF(mb, f, u - 0.04f, u + 0.04f, 0.4f, 0.44f, -0.22f, 0.2f, iron);
            }
            for (int k = 0; k < 3; k++) BoxF(mb, f, -0.9f, 0.9f, 0.44f, 0.48f, 0.2f - k * 0.14f - 0.11f, 0.2f - k * 0.14f, wood);
            for (int k = 0; k < 2; k++) BoxF(mb, f, -0.9f, 0.9f, 0.58f + k * 0.16f, 0.68f + k * 0.16f, -0.24f, -0.2f, wood);
        }

        // a brick entrance monument after the photo: a raised middle with an arched top for the sign, lower wings either side
        // whose tops sweep down, a darker coping. Local frame: u along the wall, v toward the road, y up from O.
        static void Monument(MeshBuilder mb, Frame f, float yBottom)
        {
            var brick = new Color(0.55f, 0.25f, 0.18f); var cap = new Color(0.47f, 0.21f, 0.15f);
            var mid = new List<Vector2> { new Vector2(-1.65f, yBottom), new Vector2(1.65f, yBottom), new Vector2(1.65f, 2.1f) };
            for (int k = 0; k <= 10; k++) { float a = Mathf.PI * k / 10f; mid.Add(new Vector2(Mathf.Cos(a) * 1.45f, 2.1f + Mathf.Sin(a) * 0.55f)); }
            mid.Add(new Vector2(-1.65f, 2.1f));
            ExtrudeUY(mb, f, mid, -0.35f, 0.35f, brick);
            foreach (float s in new[] { -1f, 1f })
            {
                var wing = new List<Vector2>();
                wing.Add(new Vector2(1.6f * s, yBottom)); wing.Add(new Vector2(3.4f * s, yBottom)); wing.Add(new Vector2(3.4f * s, 1.15f));
                for (int k = 1; k <= 6; k++) { float t = k / 6f; wing.Add(new Vector2((3.4f - 1.8f * t) * s, 1.15f + 0.6f * t * t)); }
                if (s < 0f) wing.Reverse();
                ExtrudeUY(mb, f, wing, -0.3f, 0.3f, brick);
                BoxF(mb, f, s > 0 ? 3.15f : -3.45f, s > 0 ? 3.45f : -3.15f, 1.1f, 1.25f, -0.36f, 0.36f, cap);
            }
            BoxF(mb, f, -1.75f, -1.35f, 2.05f, 2.2f, -0.42f, 0.42f, cap);
            BoxF(mb, f, 1.35f, 1.75f, 2.05f, 2.2f, -0.42f, 0.42f, cap);
            // the darker brick ring round the sign
            BoxF(mb, f, -1.45f, 1.45f, 0.28f, 0.36f, 0.35f, 0.39f, cap);
        }

        // a polygon in (u, y), extruded along v from v0 to v1 (front at v1 faces the road)
        static void ExtrudeUY(MeshBuilder mb, Frame f, List<Vector2> poly, float v0, float v1, Color col)
        {
            var r = new List<Vector2>(poly); if (PolyArea(r) < 0f) r.Reverse();
            mb.Tint = col;
            foreach (var t in EarClip(r))
            {
                TriFace(mb, f.P(r[t.x].x, r[t.x].y, v1), f.P(r[t.y].x, r[t.y].y, v1), f.P(r[t.z].x, r[t.z].y, v1), f.F);
                TriFace(mb, f.P(r[t.x].x, r[t.x].y, v0), f.P(r[t.y].x, r[t.y].y, v0), f.P(r[t.z].x, r[t.z].y, v0), -f.F);
            }
            for (int i = 0; i < r.Count; i++)
            {
                var a = r[i]; var b = r[(i + 1) % r.Count];
                var e = b - a; var outward = f.Dir(e.y, -e.x, 0f);
                Face(mb, f.P(a.x, a.y, v0), f.P(b.x, b.y, v0), f.P(b.x, b.y, v1), f.P(a.x, a.y, v1), outward);
            }
        }

        static Material MonumentSignMaterial()
        {
            const string path = "Assets/HandMade/Entrance/ChastainGlenSign.png";
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (tex == null) { Debug.LogWarning("[Round9] " + path + " missing (Tools/handmade/make_entrance_sign.py): monuments without the sign"); return null; }
            // lit and depth-tested (hidden by terrain and anything in front, like the brick), one-sided, cut out to the arched panel
            var m = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "MonumentSign" };
            m.SetTexture("_BaseMap", tex); m.mainTexture = tex; m.SetColor("_BaseColor", Color.white);
            m.SetFloat("_Smoothness", 0.25f);
            m.SetFloat("_AlphaClip", 1f); m.SetFloat("_Cutoff", 0.5f); m.EnableKeyword("_ALPHATEST_ON");
            m.SetFloat("_Cull", (float)CullMode.Back);
            AssetDatabase.CreateAsset(m, GeneratedDir + "/MonumentSign.mat");
            return m;
        }

        static void BuildGazebo(HoodCtx c, MeshBuilder mb, Transform root)
        {
            var ring = ToPoints(c.map.buildings[c.hood.GazeboBuilding].footprint);
            if (!BestRectangle(ring, out Vector2 ctr, out Vector2 U, out Vector2 V, out float hl, out float hw)) return;
            float lo = float.MaxValue, hi = float.MinValue;
            foreach (var p in ring) { float g = c.Y(p.x, p.y); lo = Mathf.Min(lo, g); hi = Mathf.Max(hi, g); }
            float floorY = hi + 0.1f;
            var f = new Frame { O = new Vector3(ctr.x, floorY, ctr.y), R = new Vector3(U.x, 0f, U.y), F = new Vector3(V.x, 0f, V.y), M = 1f };
            var wood = new Color(0.52f, 0.38f, 0.25f); var roof = new Color(0.30f, 0.26f, 0.24f); var slab = new Color(0.62f, 0.61f, 0.58f);
            BoxF(mb, f, -hl, hl, lo - floorY - 0.3f, 0f, -hw, hw, slab);
            float eave = 2.75f, rise = 1.7f, oh = 0.45f;
            // six posts (corners and the middle of the long sides), a beam round the top
            var posts = new List<Vector2>();
            foreach (float u in new[] { -hl + 0.2f, 0f, hl - 0.2f }) foreach (float v in new[] { -hw + 0.2f, hw - 0.2f }) posts.Add(new Vector2(u, v));
            foreach (var p in posts)
            {
                BoxF(mb, f, p.x - 0.13f, p.x + 0.13f, 0f, eave, p.y - 0.13f, p.y + 0.13f, wood);
                var col = new GameObject("Gazebo post"); col.transform.SetParent(root, false);
                col.transform.SetPositionAndRotation(f.P(p.x, eave / 2f, p.y), Quaternion.LookRotation(f.F, Vector3.up));
                col.AddComponent<BoxCollider>().size = new Vector3(0.3f, eave, 0.3f);
            }
            BoxF(mb, f, -hl, hl, eave - 0.25f, eave, -hw, -hw + 0.3f, wood);
            BoxF(mb, f, -hl, hl, eave - 0.25f, eave, hw - 0.3f, hw, wood);
            BoxF(mb, f, -hl, -hl + 0.3f, eave - 0.25f, eave, -hw, hw, wood);
            BoxF(mb, f, hl - 0.3f, hl, eave - 0.25f, eave, -hw, hw, wood);
            // a hip roof: ridge along u
            float ru = Mathf.Max(0.3f, hl - hw);
            Vector3 A = f.P(-hl - oh, eave, -hw - oh), B = f.P(hl + oh, eave, -hw - oh), C = f.P(hl + oh, eave, hw + oh), D = f.P(-hl - oh, eave, hw + oh);
            Vector3 R0 = f.P(-ru, eave + rise, 0f), R1 = f.P(ru, eave + rise, 0f);
            mb.Tint = roof;
            Face(mb, A, B, R1, R0, Vector3.up); Face(mb, C, D, R0, R1, Vector3.up);
            TriFace(mb, B, C, R1, Vector3.up); TriFace(mb, D, A, R0, Vector3.up);
            // underside of the roof (seen from inside), a shade lighter
            mb.Tint = Shade(wood, 1.15f);
            Face(mb, A, R0, R1, B, Vector3.down); Face(mb, C, R1, R0, D, Vector3.down);
            TriFace(mb, B, R1, C, Vector3.down); TriFace(mb, D, R0, A, Vector3.down);
            // six picnic tables, three along by two across
            int tables = 0;
            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 2; j++)
                {
                    float u = Mathf.Lerp(-hl, hl, (i + 0.5f) / 3f), v = Mathf.Lerp(-hw, hw, (j + 0.5f) / 2f);
                    var tf = new Frame { O = f.P(u, 0f, v), R = f.R, F = f.F, M = 1f };
                    PicnicTable(mb, tf);
                    tables++;
                }
            AddView(c, "r9_gazebo", ctr, V, ctr);
            c.Log.Append($"BUG-001 gazebo on building {c.hood.GazeboBuilding} ({hl * 2f:0.0} x {hw * 2f:0.0} m), {tables} picnic tables; ");
        }

        static void PicnicTable(MeshBuilder mb, Frame f)
        {
            var wood = new Color(0.58f, 0.43f, 0.28f); var dark = Shade(wood, 0.8f);
            BoxF(mb, f, -0.9f, 0.9f, 0.72f, 0.77f, -0.38f, 0.38f, wood);
            foreach (float v in new[] { -0.68f, 0.68f }) BoxF(mb, f, -0.9f, 0.9f, 0.42f, 0.46f, v - 0.13f, v + 0.13f, wood);
            foreach (float u in new[] { -0.7f, 0.7f })
            {
                Beam(mb, f.P(u, 0f, -0.7f), f.P(u, 0.75f, -0.1f), 0.07f, 0.07f, dark);
                Beam(mb, f.P(u, 0f, 0.7f), f.P(u, 0.75f, 0.1f), 0.07f, 0.07f, dark);
                BoxF(mb, f, u - 0.04f, u + 0.04f, 0.38f, 0.42f, -0.82f, 0.82f, dark);
            }
        }

        // BUG-001: "make this a regular playground": the swings, a slide and a climbing frame on the mulch pad
        static void BuildPlayground(MeshBuilder pk, List<Vector2> rg, TerrainGrid terrain)
        {
            if (!BestRectangle(rg, out Vector2 ctr, out Vector2 U, out Vector2 V, out float hl, out float hw)) return;
            float gy = Ground(terrain, ctr.x, ctr.y);
            var f = new Frame { O = new Vector3(ctr.x, gy, ctr.y), R = new Vector3(U.x, 0, U.y), F = new Vector3(V.x, 0, V.y), M = 1f };
            var blue = new Color(0.15f, 0.30f, 0.6f); var yellow = new Color(0.92f, 0.74f, 0.12f); var red = new Color(0.78f, 0.16f, 0.12f); var green = new Color(0.20f, 0.55f, 0.25f);
            // swings (as before, at one end)
            float su = -hl + 2.4f;
            foreach (float sx in new[] { -1.8f, 1.8f }) foreach (float sz in new[] { -0.6f, 0.6f }) BoxF(pk, f, su + sx - 0.05f, su + sx + 0.05f, 0f, 2.3f, sz - 0.05f, sz + 0.05f, blue);
            BoxF(pk, f, su - 1.85f, su + 1.85f, 2.2f, 2.35f, -0.05f, 0.05f, blue);
            foreach (float sx in new[] { -0.8f, 0.8f })
            {
                BoxF(pk, f, su + sx - 0.2f, su + sx + 0.2f, 0.45f, 0.5f, -0.2f, 0.2f, new Color(0.1f, 0.1f, 0.1f));
                foreach (float cz in new[] { -0.17f, 0.17f }) BoxF(pk, f, su + sx - 0.01f, su + sx + 0.01f, 0.5f, 2.2f, cz - 0.01f, cz + 0.01f, new Color(0.35f, 0.35f, 0.35f));
            }
            // climbing frame: a 2 m cube of bars in the middle
            float cu = 0.6f;
            for (int i = 0; i <= 2; i++) for (int j = 0; j <= 2; j++)
                    BoxF(pk, f, cu - 1f + i - 0.04f, cu - 1f + i + 0.04f, 0f, 2.0f, -1f + j - 0.04f, -1f + j + 0.04f, i == 1 && j == 1 ? yellow : green);
            for (int lvl = 1; lvl <= 2; lvl++)
                for (int k = 0; k <= 2; k++)
                {
                    BoxF(pk, f, cu - 1f, cu + 1f, lvl - 0.04f, lvl + 0.04f, -1f + k - 0.03f, -1f + k + 0.03f, green);
                    BoxF(pk, f, cu - 1f + k - 0.03f, cu - 1f + k + 0.03f, lvl - 0.04f, lvl + 0.04f, -1f, 1f, green);
                }
            // slide: a platform on four posts, a ladder up one side and a chute down the other
            float pu = hl - 3.6f, ph = 1.5f;
            foreach (float a in new[] { -0.5f, 0.5f }) foreach (float b in new[] { -0.5f, 0.5f }) BoxF(pk, f, pu + a - 0.05f, pu + a + 0.05f, 0f, ph + 0.9f, b - 0.05f, b + 0.05f, red);
            BoxF(pk, f, pu - 0.55f, pu + 0.55f, ph - 0.06f, ph, -0.55f, 0.55f, yellow);
            BoxF(pk, f, pu - 0.55f, pu + 0.55f, ph + 0.85f, ph + 0.95f, -0.55f, 0.55f, red);   // a little roof
            for (int k = 0; k < 5; k++) BoxF(pk, f, pu - 0.62f - 0.05f, pu - 0.62f, 0.3f * k + 0.3f, 0.3f * k + 0.34f, -0.3f, 0.3f, yellow);
            foreach (float b in new[] { -0.32f, 0.32f }) Beam(pk, f.P(pu - 0.75f, 0f, b), f.P(pu - 0.6f, ph, b), 0.06f, 0.06f, red);
            // the chute: from the platform edge down to the ground 2.6 m out, with low sides
            Vector3 top = f.P(pu + 0.55f, ph, 0f), bottom = f.P(pu + 3.0f, 0.25f, 0f);
            Beam(pk, top, bottom, 0.6f, 0.05f, yellow, f.Dir(0, 0, 1));
            foreach (float b in new[] { -0.3f, 0.3f }) Beam(pk, top + f.Dir(0, 0.1f, b), bottom + f.Dir(0, 0.1f, b), 0.04f, 0.2f, yellow, f.Dir(0, 0, 1));
        }
    }
}
