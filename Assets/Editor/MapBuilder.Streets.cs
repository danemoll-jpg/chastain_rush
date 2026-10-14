using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// Round 7: concrete curbs, sidewalks with a grass strip, street lamps, hydrants, the white rail / wooden / picket / iron
// fences (breakable, see Scripts/Neighborhood/FenceChunk.cs) and the planted entrance island with its two signs.
namespace ChastainRush
{
    public static partial class MapBuilder
    {
        // ------------------------------------------------------------ curbs, sidewalks, lamps, hydrants

        static void BuildStreets(HoodCtx c)
        {
            var hood = c.hood;
            var centres = hood.Houses.Select(h => h.footprintCentre).ToList();
            var lampBrown = new Color(0.27f, 0.22f, 0.18f); var lampHead = new Color(0.22f, 0.22f, 0.24f);
            int lampCount = 0, hydrantCount = 0;
            // Round 11 (BUG-006, Dan: "the hydrant should be in my yard instead of the neighbors"): the hydrant that stands in
            // front of a neighbour's lot next to Dan's is put in front of Dan's own lot instead (DanHydrant)
            Vector2 danFront = default; bool danHydrantDone = hood.Dan == null;
            if (hood.Dan != null) { var fp = hood.Dan.frame.P(0f, 0f, hood.Dan.D / 2f); hood.Nearest(new Vector2(fp.x, fp.z), out danFront, out _, out _, out _); }
            foreach (int ri in hood.Area.roads)
            {
                var r = c.map.roads[ri];
                var pts = Densify(ToPoints(r.points), 1f);   // Round 11: 1 m (was 2 m), so curbs and sidewalks follow the ground
                int n = pts.Count; if (n < 2) continue;
                float hw = r.width * 0.5f;
                var nrm = new Vector2[n]; var arc = new float[n];
                for (int i = 0; i < n; i++)
                {
                    Vector2 dirIn = i > 0 ? (pts[i] - pts[i - 1]).normalized : (pts[1] - pts[0]).normalized;
                    Vector2 dirOut = i < n - 1 ? (pts[i + 1] - pts[i]).normalized : dirIn;
                    Vector2 t = (dirIn + dirOut).normalized; if (t.sqrMagnitude < 1e-6f) t = dirOut;
                    nrm[i] = new Vector2(-t.y, t.x);
                    if (i > 0) arc[i] = arc[i - 1] + Vector2.Distance(pts[i - 1], pts[i]);
                }
                foreach (float side in new[] { 1f, -1f })
                {
                    // --- curb: 18 cm wide, 12 cm proud of the road, just outside the pale edge strip; no collider
                    for (int i = 0; i + 1 < n; i++)
                    {
                        int j = i + 1;
                        Vector2 mid = (pts[i] + pts[j]) * 0.5f + (nrm[i] + nrm[j]).normalized * side * (hw + 0.55f);
                        // Round 11: off any other road that does not continue this one (BUG-008), and where a curb return or a
                        // cul-de-sac's ring takes over
                        if (hood.OtherRoadCovers(ri, mid, 0.65f) || hood.InCornerZone(ri, mid, hw) || hood.InBulbZone(mid, 0.6f) || hood.InMedianFill(mid) || c.OnDrive(mid, 0.3f)) continue;
                        var mb = c.Props.At(mid);
                        Vector3 Pt(int k, float off, float dy) { var p = pts[k] + nrm[k] * side * off; return new Vector3(p.x, Ground(c.terrain, p.x, p.y) + dy, p.y); }
                        mb.Tint = CurbConcrete;
                        Face(mb, Pt(i, hw + 0.45f, 0.20f), Pt(j, hw + 0.45f, 0.20f), Pt(j, hw + 0.63f, 0.20f), Pt(i, hw + 0.63f, 0.20f), Vector3.up);
                        mb.Tint = CurbFace;
                        Face(mb, Pt(i, hw + 0.45f, 0.08f), Pt(j, hw + 0.45f, 0.08f), Pt(j, hw + 0.45f, 0.20f), Pt(i, hw + 0.45f, 0.20f), new Vector3(-nrm[i].x * side, 0f, -nrm[i].y * side));
                        Face(mb, Pt(i, hw + 0.63f, -0.1f), Pt(j, hw + 0.63f, -0.1f), Pt(j, hw + 0.63f, 0.20f), Pt(i, hw + 0.63f, 0.20f), new Vector3(nrm[i].x * side, 0f, nrm[i].y * side));
                        c.CurbMetres += 1;
                    }
                    // --- sidewalk: 1.4 m wide, 1.25 m of grass between it and the curb; only along houses, never into the pond.
                    // Round 11 (BUG-001, 003, 004: "the sidewalks arent broken like this"): a grid that stands clear of the lawn,
                    // (Round 12: stopped at every drive it meets, BUG-002); the stretch "along houses" is joined over gaps under 24 m
                    // and stretches under 10 m are left out, so it no longer comes and goes in short pieces; it stops where a
                    // curb return or a cul-de-sac's ring takes over, and on any other road
                    var walkP = new Vector2[n]; var along = new bool[n];
                    for (int i = 0; i < n; i++)
                    {
                        walkP[i] = pts[i] + nrm[i] * side * (hw + 2.58f);
                        foreach (var hc in centres)
                        {
                            var d = hc - pts[i];
                            if (d.sqrMagnitude < 26f * 26f && Vector2.Dot(d, nrm[i] * side) > 2f) { along[i] = true; break; }
                        }
                    }
                    Smooth1D(along, arc, 24f, 10f);
                    var run = new List<Vector2>();
                    void Flush()
                    {
                        if (run.Count >= 3)
                        {
                            var path = Densify(Chaikin(run, 2), 1f);   // the offset of a bent road zig-zags: smoothed
                            // Round 12 (BUG-002): stopped at every drive (OffDrives), so drives run unbroken to the road
                            Edges(path, 0.7f, out var ea, out var eb);
                            c.Walks.Tint = ConcreteLight;
                            foreach (var (pa, pb) in OffDrives(c, ea, eb)) WalkBand(c.Walks.At(pa[pa.Count / 2]), pa, pb, c.terrain);
                            c.WalkMetres += run.Count;
                        }
                        run.Clear();
                    }
                    for (int i = 0; i < n; i++)
                    {
                        var p = walkP[i];
                        bool ok = along[i] && !hood.OtherRoadCovers(ri, p, 0.75f) && !hood.NearOtherRoad(ri, p, 1.8f) && !hood.InCornerZone(ri, p, hw, 1.0f) && !hood.InBulbZone(p, 2.4f)
                                  && hood.DistToPond(p) > 1.5f && !Neighborhood.InRing(p, hood.ParkingRing);
                        if (ok) run.Add(p); else Flush();
                    }
                    Flush();
                    // --- lamps and hydrants in the grass strip, alternating sides along the street
                    float lampPeriod = 56f;
                    for (float s0 = 14f + 20f * Hash01(ri, 140) + (side > 0 ? 0f : lampPeriod * 0.5f); s0 < arc[n - 1] - 8f; s0 += lampPeriod)
                    {
                        int k = Array.FindIndex(arc, a => a >= s0); if (k < 1) continue;
                        Vector2 p = Vector2.Lerp(pts[k - 1], pts[k], Mathf.InverseLerp(arc[k - 1], arc[k], s0)) + nrm[k] * side * (hw + 1.3f);
                        if (c.roadIndex.Covers(p, r, 2.5f) || c.OnDrive(p, 1.2f) || hood.DistToPond(p) < 3f || Neighborhood.InRing(p, hood.ParkingRing)) continue;
                        if (hood.InCornerZone(ri, p, hw) || hood.InBulbZone(p, 1.6f)) continue;   // Round 11: not on a curb return or in a bulb
                        if (hood.InCorridor(p, 1.5f)) { c.ShortcutCleared++; continue; }   // Round 8: no lamp on a shortcut's line
                        bool near = false; foreach (var hc in centres) if ((hc - p).sqrMagnitude < 28f * 28f) { near = true; break; }
                        if (!near) continue;
                        float gy = Ground(c.terrain, p.x, p.y);
                        // Round 10 (item 8): breakable like the mailboxes: the pole in two pieces and the arm with its head, all
                        // coming down to the ground when the car knocks it over
                        var mb = c.Lamps3D.Begin(p);
                        Vector3 top = new Vector3(p.x, gy + 5.65f, p.y);
                        Vector2 toRoad = -nrm[k] * side;
                        c.Lamps3D.Piece(() => Post(mb, new Vector3(p.x, gy - 0.05f, p.y), 2.9f, 0.14f, lampBrown), gy + 0.1f);
                        c.Lamps3D.Piece(() => Post(mb, new Vector3(p.x, gy + 2.85f, p.y), 2.85f, 0.14f, lampBrown), gy + 0.1f);
                        c.Lamps3D.Piece(() =>
                        {
                            Beam(mb, top, top + new Vector3(toRoad.x, 0.25f, toRoad.y) * 1.5f, 0.09f, 0.09f, lampBrown);
                            Beam(mb, top + new Vector3(toRoad.x, 0.2f, toRoad.y) * 1.0f, top + new Vector3(toRoad.x, 0.2f, toRoad.y) * 1.7f, 0.3f, 0.13f, lampHead);
                        }, gy + 0.15f);
                        c.Lamps3D.End(new Vector3(p.x, gy + 1.0f, p.y), new Vector3(toRoad.x, 0f, toRoad.y), 0.7f, 2.2f);
                        hood.KeepOut(p, 0.8f);
                        lampCount++;
                    }
                    for (float s0 = 40f + 30f * Hash01(ri, 141) + (side > 0 ? 0f : 60f); s0 < arc[n - 1] - 10f; s0 += 130f)
                    {
                        int k = Array.FindIndex(arc, a => a >= s0); if (k < 1) continue;
                        Vector2 p = Vector2.Lerp(pts[k - 1], pts[k], Mathf.InverseLerp(arc[k - 1], arc[k], s0)) + nrm[k] * side * (hw + 1.1f);
                        if (c.roadIndex.Covers(p, r, 2.5f) || c.OnDrive(p, 1.5f) || hood.DistToPond(p) < 3f) continue;
                        if (hood.InCornerZone(ri, p, hw) || hood.InBulbZone(p, 1.6f)) continue;   // Round 11: not on a curb return or in a bulb
                        if (hood.InCorridor(p, 1.5f)) { c.ShortcutCleared++; continue; }   // Round 8: no hydrant on a shortcut's line
                        if (!danHydrantDone && (p - danFront).sqrMagnitude < 24f * 24f)
                        {
                            var dh = DanHydrant(c, p);
                            if (dh.HasValue) { p = dh.Value; danHydrantDone = true; }
                        }
                        bool near = false; foreach (var hc in centres) if ((hc - p).sqrMagnitude < 28f * 28f) { near = true; break; }
                        if (!near) continue;
                        // Round 10 (items 8, 9): breakable, and its water starts when it goes (HydrantWater)
                        float hy = Ground(c.terrain, p.x, p.y);
                        var hmb = c.Hydrants3D.Begin(p);
                        c.Hydrants3D.Piece(() => Hydrant(hmb, new Vector3(p.x, hy, p.y), Hash01(ri, 142 + k)), hy + 0.12f);
                        c.Hydrants3D.End(new Vector3(p.x, hy + 0.45f, p.y), Vector3.forward, 0.7f, 1.1f);
                        hood.KeepOut(p, 0.8f);
                        hydrantCount++;
                    }
                }
            }
            c.Lamps = lampCount; c.Hydrants = hydrantCount;
            BuildStreetCorners(c);   // Round 11: curbs and sidewalks round the curb returns and the cul-de-sac bulbs
        }

        // Round 11: a yes / no along a street (at arc lengths 'arc'): gaps of 'no' shorter than 'join' metres between two 'yes'
        // (or between a 'yes' and the street's end) become 'yes', then stretches of 'yes' shorter than 'drop' metres become 'no'
        // Chaikin corner cutting, keeping both ends
        static List<Vector2> Chaikin(List<Vector2> pts, int iterations)
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

        // Round 12 (BUG-002, Dan: "The sidewalks shouldn't cross driveways anywhere"): on a drive as it is drawn (its width
        // taken along each piece, so a flared mouth counts)
        static bool OnDriveDrawn(HoodCtx c, Vector2 p)
        {
            foreach (var dp in c.hood.DrivePlans.Values)
            {
                if ((dp.pts[0] - p).sqrMagnitude > 60f * 60f && (dp.pts[dp.pts.Count - 1] - p).sqrMagnitude > 60f * 60f) continue;
                for (int i = 0; i + 1 < dp.pts.Count; i++)
                {
                    var a = dp.pts[i]; var ab = dp.pts[i + 1] - a; float L2 = ab.sqrMagnitude; if (L2 < 1e-6f) continue;
                    float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / L2);
                    if ((a + ab * t - p).magnitude < Mathf.Lerp(dp.widths[i], dp.widths[i + 1], t) * 0.5f) return true;
                }
            }
            return false;
        }

        // Round 12 (BUG-002): the stretches of a sidewalk (edges a and b, matching polylines) that stay off every drive. A
        // stretch ends exactly where the sidewalk's cross-section first touches a drive's edge, and starts again where it
        // leaves the drive on the other side, so the drive runs unbroken from the garage to the road.
        static List<(List<Vector2> a, List<Vector2> b)> OffDrives(HoodCtx c, List<Vector2> a, List<Vector2> b)
        {
            int n = a.Count; var runs = new List<(List<Vector2>, List<Vector2>)>();
            if (n < 2) return runs;
            bool On(int i, float t)
            {
                if (i >= n - 1) { i = n - 2; t = 1f; }
                var pa = Vector2.Lerp(a[i], a[i + 1], t); var pb = Vector2.Lerp(b[i], b[i + 1], t);
                return OnDriveDrawn(c, pa) || OnDriveDrawn(c, pb) || OnDriveDrawn(c, (pa + pb) * 0.5f);
            }
            float Edge(int i)   // where the section changes between points i and i + 1
            {
                bool s0 = On(i, 0f); float lo = 0f, hi = 1f;
                for (int k = 0; k < 14; k++) { float m = (lo + hi) * 0.5f; if (On(i, m) == s0) lo = m; else hi = m; }
                return s0 ? hi : lo;   // the first point off the drive, or the last one before it
            }
            List<Vector2> ra = null, rb = null;
            for (int i = 0; i < n; i++)
            {
                bool on = On(i, 0f);
                if (!on && ra == null)
                {
                    ra = new List<Vector2>(); rb = new List<Vector2>();
                    if (i > 0) { float t = Edge(i - 1); ra.Add(Vector2.Lerp(a[i - 1], a[i], t)); rb.Add(Vector2.Lerp(b[i - 1], b[i], t)); }
                }
                if (!on) { ra.Add(a[i]); rb.Add(b[i]); }
                else if (ra != null)
                {
                    float t = Edge(i - 1); ra.Add(Vector2.Lerp(a[i - 1], a[i], t)); rb.Add(Vector2.Lerp(b[i - 1], b[i], t));
                    runs.Add((ra, rb)); ra = rb = null;
                }
            }
            if (ra != null) runs.Add((ra, rb));
            runs.RemoveAll(r => r.Item1.Count < 2 || Vector2.Distance(r.Item1[0], r.Item1[r.Item1.Count - 1]) < 0.3f);
            return runs;
        }

        // the two edges of a sidewalk 'half' either side of its centre line
        static void Edges(List<Vector2> centre, float half, out List<Vector2> a, out List<Vector2> b)
        {
            a = new List<Vector2>(); b = new List<Vector2>(); int n = centre.Count;
            for (int i = 0; i < n; i++)
            {
                var d = (centre[Mathf.Min(i + 1, n - 1)] - centre[Mathf.Max(i - 1, 0)]).normalized; var nrm = new Vector2(-d.y, d.x);
                a.Add(centre[i] + nrm * half); b.Add(centre[i] - nrm * half);
            }
        }

        static void Smooth1D(bool[] v, float[] arc, float join, float drop)
        {
            int n = v.Length;
            for (int i = 0; i < n;)
            {
                if (v[i]) { i++; continue; }
                int j = i; while (j < n && !v[j]) j++;
                // a gap at either end of the street counts too (the sidewalk runs on to the junction or the cul-de-sac's ring)
                bool fill = (i > 0 || j < n) && (i == 0 ? arc[j] - arc[0] : j == n ? arc[n - 1] - arc[i - 1] : arc[j] - arc[i - 1]) < join;
                if (fill && (i > 0 || j < n) && !(i == 0 && j == n)) for (int k = i; k < j; k++) v[k] = true;
                i = j;
            }
            for (int i = 0; i < n;)
            {
                if (!v[i]) { i++; continue; }
                int j = i; while (j < n && v[j]) j++;
                if (arc[j - 1] - arc[i] < drop) for (int k = i; k < j; k++) v[k] = false;
                i = j;
            }
        }

        // Round 11 (BUG-006): where the hydrant near Dan's house goes. The one beside his drive on the neighbour's side (his
        // neighbour's drive runs right next to his, so it read as the neighbour's) goes to the other side of his drive, in the
        // grass strip past his mailbox: clearly in his yard. Null if p is not that hydrant.
        static Vector2? DanHydrant(HoodCtx c, Vector2 p)
        {
            var hood = c.hood; var dan = hood.Dan;
            if (!hood.DrivePlans.TryGetValue(dan.index, out var dp)) return null;
            var right = new Vector2(dan.frame.R.x, dan.frame.R.z) * dan.frame.M; var front = new Vector2(dan.frame.F.x, dan.frame.F.z);
            var E = dp.pts[dp.pts.Count - 1]; float half = dp.widths[dp.widths.Length - 1] * 0.5f;
            if (Vector2.Dot(p - E, front) > 1.5f) return null;                       // across the street
            float uE = Vector2.Dot(E - dan.footprintCentre, right), uP = Vector2.Dot(p - dan.footprintCentre, right);
            if (uP > uE) { c.Log.Append("BUG-006 hydrant: already on the house side of Dan's drive; "); return null; }
            Vector2 Strip(float u)
            {
                var g = E + right * (u - uE);
                hood.Nearest(g, out var q, out float hw, out _, out _);
                return q + (g - q).normalized * (hw + 1.1f);
            }
            for (float u = uE + half + 2.6f; u < uE + half + 10f; u += 0.5f)
            {
                var q = Strip(u);
                if (!c.OnDrive(q, 1.5f) && (!hood.DanMailbox.HasValue || (q - hood.DanMailbox.Value).magnitude > 1.6f)) { c.Log.Append($"BUG-006 hydrant: moved from {uE - uP - half:0.0} m off the neighbour's side of Dan's drive to {u - uE - half:0.0} m off its other side, past his mailbox; "); return q; }
            }
            c.Log.Append("BUG-006 hydrant: NO ROOM in front of Dan's lot; ");
            return null;
        }

        static void Hydrant(MeshBuilder mb, Vector3 b, float h)
        {
            var body = h < 0.5f ? new Color(0.62f, 0.58f, 0.50f) : new Color(0.72f, 0.12f, 0.10f);
            var cap = Shade(body, 0.8f);
            Cyl(mb, b + Vector3.down * 0.05f, 0.62f, 0.14f, 0.12f, body, 8);
            Cyl(mb, b + Vector3.up * 0.55f, 0.1f, 0.15f, 0.15f, cap, 8);
            Cyl(mb, b + Vector3.up * 0.65f, 0.08f, 0.1f, 0.0f, cap, 8);
            Beam(mb, b + Vector3.up * 0.38f - Vector3.right * 0.2f, b + Vector3.up * 0.38f + Vector3.right * 0.2f, 0.09f, 0.09f, cap);
            Beam(mb, b + Vector3.up * 0.30f - Vector3.forward * 0.15f, b + Vector3.up * 0.30f + Vector3.forward * 0.15f, 0.12f, 0.12f, cap);
        }

        // ------------------------------------------------------------ fences

        static readonly Color[] WoodTones = { new Color(0.50f, 0.39f, 0.28f), new Color(0.58f, 0.48f, 0.36f), new Color(0.42f, 0.35f, 0.29f), new Color(0.62f, 0.52f, 0.40f) };

        sealed class FencePanelRec { public int first, count; public Vector3 centre, along; public float length, height; }

        static void BuildFences(HoodCtx c)
        {
            var cells = new Dictionary<Vector2Int, (MeshBuilder mb, List<FenceChunk.Piece> pieces, List<FenceChunk.Panel> panels, List<FencePanelRec> recs)>();
            var fRoot = new GameObject("Fences").transform; fRoot.SetParent(c.root, false);
            var mats = new Dictionary<FenceKind, Material>();
            int panelsBuilt = 0;
            foreach (var run in c.hood.Fences)
            {
                var kind = run.kind;
                float L = kind == FenceKind.Picket ? 2.0f : 2.4f;
                Color wood = Shade(PickOf(WoodTones, run.seed, 150), 1f);
                // walk the polyline in panels of length L, each between two posts
                var pts = run.pts;
                var arc = new List<float> { 0f };
                for (int i = 1; i < pts.Count; i++) arc.Add(arc[i - 1] + Vector2.Distance(pts[i - 1], pts[i]));
                float total = arc[arc.Count - 1];
                int count = Mathf.Max(1, Mathf.RoundToInt(total / L));
                float step = total / count;
                Vector2 At(float s)
                {
                    int k = arc.FindIndex(a => a >= s); if (k < 1) k = 1; if (k >= pts.Count) k = pts.Count - 1;
                    return Vector2.Lerp(pts[k - 1], pts[k], Mathf.InverseLerp(arc[k - 1], arc[k], s));
                }
                for (int i = 0; i < count; i++)
                {
                    Vector2 a = At(i * step), b = At((i + 1) * step), mid = (a + b) * 0.5f;
                    if (kind != FenceKind.Iron && (!c.hood.FenceOk(mid) || c.OnDrive(mid, 0.6f))) continue;
                    if (c.hood.OnPoolJumpYard(mid)) continue;   // Round 20 follow-up: off R1's pool deck and its line (MapBuilder.R1Pool.cs)
                    if (kind == FenceKind.Rail && c.roadIndex.Covers(mid, NoRoad, 2.5f)) continue;
                    if (OverCut(c.terrain, a, b)) { c.FencesOverCuts++; continue; }   // Round 24 A3 (B0, BUG-001: "fence isnt hanging in mid air"): no panel over a pipe's or the network's cut
                    float ga = Ground(c.terrain, a.x, a.y), gb = Ground(c.terrain, b.x, b.y);
                    var key = new Vector2Int(Mathf.FloorToInt(mid.x / 128f), Mathf.FloorToInt(mid.y / 128f));
                    var ck = ((int)kind) * 1000003 + key.x * 7919 + key.y;
                    if (!cells.TryGetValue(new Vector2Int(key.x * 4 + (int)kind, key.y), out var cell))
                        cells[new Vector2Int(key.x * 4 + (int)kind, key.y)] = cell = (new MeshBuilder(), new List<FenceChunk.Piece>(), new List<FenceChunk.Panel>(), new List<FencePanelRec>());
                    var rec = BuildFencePanel(cell.mb, cell.pieces, kind, new Vector3(a.x, ga, a.y), new Vector3(b.x, gb, b.y), wood, run.seed + i);
                    cell.panels.Add(new FenceChunk.Panel { firstPiece = rec.first, pieceCount = rec.count, centre = rec.centre });
                    cell.recs.Add(rec);
                    panelsBuilt++;
                }
                c.FenceRuns++;
            }
            foreach (var kv in cells)
            {
                var kind = (FenceKind)(kv.Key.x & 3);
                var cell = kv.Value;
                if (cell.panels.Count == 0) continue;
                var go = cell.mb.ToObject($"Fence_{kind}_{kv.Key.x >> 2}_{kv.Key.y}", c.mats.House, fRoot, false, kind == FenceKind.Privacy);
                GameObjectUtility.SetStaticEditorFlags(go, 0);  // the mesh is rewritten at runtime when a panel breaks
                var chunk = go.AddComponent<FenceChunk>();
                chunk.source = go.GetComponent<MeshFilter>().sharedMesh;
                chunk.material = c.mats.House;
                chunk.pieces = cell.pieces.ToArray();
                chunk.panels = cell.panels.ToArray();
                chunk.sound = kind == FenceKind.Iron ? BreakSounds.Kind.Iron : BreakSounds.Kind.Wood;   // Round 15, item 3
                for (int i = 0; i < cell.recs.Count; i++)
                {
                    var rec = cell.recs[i];
                    var pg = new GameObject("p"); pg.transform.SetParent(go.transform, false);
                    pg.transform.SetPositionAndRotation(rec.centre, Quaternion.LookRotation(rec.along, Vector3.up));
                    var bc = pg.AddComponent<BoxCollider>(); bc.isTrigger = true;
                    bc.size = new Vector3(0.9f, rec.height, rec.length);
                    var fp = pg.AddComponent<FencePanel>(); fp.chunk = chunk; fp.index = i;
                }
            }
            c.FencePanels = panelsBuilt;
        }

        // one panel between two posts; the first post belongs to it. Returns its piece range.
        static FencePanelRec BuildFencePanel(MeshBuilder mb, List<FenceChunk.Piece> pieces, FenceKind kind, Vector3 a, Vector3 b, Color wood, int seed)
        {
            Vector3 d = b - a; float len = new Vector2(d.x, d.z).magnitude;
            Vector3 along = new Vector3(d.x, 0f, d.z).normalized;
            Vector3 side = Vector3.Cross(Vector3.up, along);
            float lo = Mathf.Min(a.y, b.y) - 0.12f;
            var rec = new FencePanelRec { first = pieces.Count, length = len };
            void Piece(Action build)
            {
                int v0 = mb.Count;
                build();
                int cnt = mb.Count - v0;
                // the piece's centre: the average of its vertices
                Vector3 centre = Vector3.zero;
                var verts = ((MeshBuilder)mb).VertexRange(v0, cnt);
                foreach (var v in verts) centre += v;
                centre /= Mathf.Max(cnt, 1);
                pieces.Add(new FenceChunk.Piece { vertexStart = v0, vertexCount = cnt, centre = centre });
            }
            Vector3 P(float s, float y) => new Vector3(a.x + along.x * s, y, a.z + along.z * s);
            switch (kind)
            {
                case FenceKind.Privacy:
                {
                    float H = 1.85f;
                    for (int k = 0; k < 3; k++)
                    {
                        float y0 = lo + k * (H / 3f), y1 = y0 + H / 3f - 0.015f;
                        var col = Shade(wood, 0.92f + 0.14f * Hash01(seed, 160 + k));
                        Piece(() => Beam(mb, P(0.1f, (y0 + y1) / 2), P(len - 0.02f, (y0 + y1) / 2), 0.05f, y1 - y0, col, side));
                    }
                    Piece(() => Post(mb, P(0f, lo - 0.1f), H + 0.2f, 0.13f, Shade(wood, 0.82f)));
                    rec.height = H; rec.count = 4;
                    break;
                }
                case FenceKind.Picket:
                {
                    float H = 1.05f;
                    var col = Shade(wood, 1.12f);
                    Piece(() =>
                    {
                        int n = Mathf.Max(6, Mathf.RoundToInt(len / 0.17f));
                        for (int k = 0; k < n; k++)
                        {
                            float s = 0.12f + (len - 0.14f) * (k + 0.5f) / n;
                            Beam(mb, P(s, lo), P(s, lo + H - (k % 2 == 0 ? 0f : 0.0f)), 0.085f, 0.03f, Shade(col, 0.96f + 0.08f * Hash01(seed, 170 + k)), side);
                        }
                        Beam(mb, P(0.05f, lo + 0.25f), P(len, lo + 0.25f), 0.03f, 0.07f, Shade(col, 0.8f), side);
                        Beam(mb, P(0.05f, lo + 0.75f), P(len, lo + 0.75f), 0.03f, 0.07f, Shade(col, 0.8f), side);
                    });
                    Piece(() => Post(mb, P(0f, lo - 0.1f), H + 0.1f, 0.1f, Shade(wood, 0.85f)));
                    rec.height = H; rec.count = 2;
                    break;
                }
                case FenceKind.Rail:
                {
                    var white = new Color(0.94f, 0.94f, 0.92f);
                    for (int k = 0; k < 3; k++)
                    {
                        float y = lo + 0.42f + k * 0.40f;
                        Piece(() => Beam(mb, P(0.05f, y), P(len, y), 0.05f, 0.12f, white, side));
                    }
                    Piece(() => Post(mb, P(0f, lo - 0.1f), 1.45f, 0.14f, white));
                    rec.height = 1.35f; rec.count = 4;
                    break;
                }
                default: // Iron: slim black pickets, a top and bottom rail, a white post
                {
                    var black = new Color(0.07f, 0.07f, 0.08f); float H = 1.5f;
                    Piece(() =>
                    {
                        int n = Mathf.Max(8, Mathf.RoundToInt(len / 0.13f));
                        for (int k = 0; k < n; k++) { float s = 0.14f + (len - 0.16f) * (k + 0.5f) / n; Beam(mb, P(s, lo), P(s, lo + H), 0.022f, 0.022f, black, side); }
                    });
                    Piece(() =>
                    {
                        Beam(mb, P(0.05f, lo + 0.18f), P(len, lo + 0.18f), 0.03f, 0.05f, black, side);
                        Beam(mb, P(0.05f, lo + H - 0.12f), P(len, lo + H - 0.12f), 0.03f, 0.05f, black, side);
                    });
                    Piece(() => Post(mb, P(0f, lo - 0.1f), H + 0.15f, 0.14f, new Color(0.93f, 0.93f, 0.91f)));
                    rec.height = H; rec.count = 3;
                    break;
                }
            }
            rec.centre = P(len * 0.5f, lo + rec.height * 0.5f);
            rec.along = along;
            // panel centres are stored in the object's local space by the chunk (the chunk is at the origin)
            return rec;
        }
    }
}
