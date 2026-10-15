using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

// Round 27 (A1, Dan: "the buried house walls"). A styled house stands at the ground at the foot of its garage doors (+0.16 m), so on a
// slope its back or a side runs into the hill (Round 21's report: 16 community houses buried more than 0.5 m, worst 636 at 2.7 m), and
// its downhill walls show siding all the way down to the ground. Every styled house on the map but Dan's (locked) is swept here:
//   - a wall buried more than SettleFrom: its lot is cut down to just under the floor 1.4 m round it with a bank of about 40 degrees
//     back up to the natural ground (Round 9's CutLot for 1520, cut only, never filled). The floor stays where it is, so the garage
//     and the front door still meet their drive and path. Never on a street or sidewalk (3 m from the road's edge), a shortcut's line or
//     ramp, a pond, the pools and the Enclave's courts, nor below another house's foundation; Dan's lot is never touched.
//   - its floor standing more than SettleFrom over the ground at a wall (a corner "floating" on siding): the walls under the floor are
//     drawn as a block foundation (FoundationTint in BoxNoBottom), and where the back wall stands more than WalkoutFrom over the
//     ground it is a walk-out basement wall: basement windows, and a door where there is room for one.
// The list (id, x, z, before / after) goes to Data/screenshots/round27/houses_settled.txt; Dan's house is never listed.
namespace ChastainRush
{
    public static partial class MapBuilder
    {
        const float SettleFrom = 0.3f, WalkoutFrom = 1.5f;
        static readonly Color FoundationBlock = new Color(0.56f, 0.55f, 0.52f);   // painted block, a little darker than the trim

        sealed partial class Neighborhood
        {
            public readonly HashSet<int> Foundation = new HashSet<int>();
            public readonly Dictionary<int, float> BackExposure = new Dictionary<int, float>();
            readonly List<string> settleLog = new List<string>();

            // how far the floor stands over the lowest ground along the house's walls
            float Exposure(HouseSpec h, out float back)
            {
                float lo = float.MaxValue; back = float.MinValue;
                var r = new Vector2(h.frame.R.x, h.frame.R.z) * h.frame.M; var f = new Vector2(h.frame.F.x, h.frame.F.z);
                for (float u = -h.W / 2; u <= h.W / 2 + 0.01f; u += 0.5f)
                    foreach (float v in new[] { -h.D / 2 - 0.15f, h.D / 2 + 0.15f })
                    {
                        var p = h.footprintCentre + r * u + f * v; float e = h.floorY - terrain.Height(p.x, p.y);
                        lo = Mathf.Min(lo, -e);
                        if (v < 0f && Mathf.Abs(u) < h.W / 2 - 0.7f) back = Mathf.Max(back, e);
                    }
                for (float v = -h.D / 2; v <= h.D / 2 + 0.01f; v += 0.5f)
                    foreach (float u in new[] { -h.W / 2 - 0.15f, h.W / 2 + 0.15f })
                    { var p = h.footprintCentre + r * u + f * v; lo = Mathf.Min(lo, terrain.Height(p.x, p.y) - h.floorY); }
                return -lo;
            }

            bool HoldGround(Vector2 p, HouseSpec self)
            {
                if (RoadClearance(p) < 3f) return true;                         // streets and their sidewalks
                if (Dan != null && Dan != self)
                {
                    var dd = p - Dan.footprintCentre;
                    if (Mathf.Abs(Vector2.Dot(dd, new Vector2(Dan.frame.R.x, Dan.frame.R.z))) < Dan.W / 2 + 6f && Mathf.Abs(Vector2.Dot(dd, new Vector2(Dan.frame.F.x, Dan.frame.F.z))) < Dan.D / 2 + 6f) return true;
                }
                if (InCorridor(p, 2f) || UnderShortcutPrism(p)) return true;    // protected geometry
                if (DistToPond(p) < 2f || OnPoolOrLot(p) || OnPoolJumpYard(p) || AmenityBlocks(p, 1.5f)) return true;
                if (InsideHouse(p, 0.15f, self)) return true;
                return false;
            }

            void CutLotGuarded(HouseSpec h, out int changed, out float deepest)
            {
                float pad = h.floorY - 0.12f;
                var r = new Vector2(h.frame.R.x, h.frame.R.z); var f = new Vector2(h.frame.F.x, h.frame.F.z);
                float hu = h.W / 2 + 1.4f, hv = h.D / 2 + 1.4f;
                const float Bank = 0.85f, Reach = 6f;
                var area = Rect.MinMaxRect(h.footprintCentre.x - hu - hv - Reach, h.footprintCentre.y - hu - hv - Reach, h.footprintCentre.x + hu + hv + Reach, h.footprintCentre.y + hu + hv + Reach);
                int n = 0; float deep = 0f;
                EditTerrain(terrain, area, (x, z, y) =>
                {
                    var p = new Vector2(x, z); var d = p - h.footprintCentre;
                    float ou = Mathf.Max(0f, Mathf.Abs(Vector2.Dot(d, r)) - hu), ov = Mathf.Max(0f, Mathf.Abs(Vector2.Dot(d, f)) - hv);
                    float allowed = pad + Mathf.Sqrt(ou * ou + ov * ov) * Bank;
                    if (y <= allowed + 1e-3f) return y;
                    if (HoldGround(p, h)) return y;
                    // grounding: never below another house's foundation within 1.5 m of it
                    foreach (var o in Houses)
                    {
                        if (o == h) continue;
                        var dh = p - o.footprintCentre; if (dh.sqrMagnitude > 400f) continue;
                        if (Mathf.Abs(Vector2.Dot(dh, new Vector2(o.frame.R.x, o.frame.R.z))) < o.W / 2 + 1.5f && Mathf.Abs(Vector2.Dot(dh, new Vector2(o.frame.F.x, o.frame.F.z))) < o.D / 2 + 1.5f)
                            allowed = Mathf.Max(allowed, o.baseY + 0.15f);
                    }
                    if (y <= allowed + 1e-3f) return y;
                    n++; deep = Mathf.Max(deep, y - allowed);
                    return allowed;
                });
                changed = n; deepest = deep;
            }

            // Round 27 (A1): called from RegradeAndReport, before the shortcuts', pools' and courts' grading
            void SettleHouses()
            {
                int cut = 0, found = 0, walk = 0, left = 0;
                var rows = new List<(float key, string line)>();
                foreach (var h in Houses)
                {
                    if (h.dan) continue;   // locked
                    float b0 = Burial(h), e0 = Exposure(h, out _);
                    if (b0 <= SettleFrom && e0 <= SettleFrom) continue;
                    int n = 0; float deep = 0f;
                    if (b0 > SettleFrom) { CutLotGuarded(h, out n, out deep); if (n > 0) cut++; }
                    float b1 = Burial(h), e1 = Exposure(h, out float back);
                    if (e1 > SettleFrom) { Foundation.Add(h.index); found++; }
                    if (back > WalkoutFrom) { BackExposure[h.index] = back; walk++; }
                    if (b1 > SettleFrom) left++;
                    string what = (n > 0 ? $"lot cut ({n} samples, up to {deep:0.0} m)" : b0 > SettleFrom ? "lot NOT cut (street, shortcut, pond or a neighbor beside it)" : "")
                                + (e1 > SettleFrom ? (n > 0 ? "; " : "") + "block foundation" + (back > WalkoutFrom ? $", walk-out basement wall at the back ({back:0.0} m)" : "") : "");
                    rows.Add((Mathf.Max(b0, e0), $"{h.index,5}  x {h.footprintCentre.x,7:0}  z {h.footprintCentre.y,6:0}  {(h.community == 0 ? "Chastain Glen" : "community " + h.community),-13}  style {h.style}  buried {b0,4:0.00} -> {b1,5:0.00} m   floor over ground {e0,4:0.00} -> {e1,4:0.00} m   {what}"));
                }
                var sb = new StringBuilder();
                sb.AppendLine("Round 27 (A1): every styled house on the map swept (Dan's house locked, not listed). A house is settled when a wall is buried");
                sb.AppendLine($"more than {SettleFrom} m (the ground above its floor at the wall: the lot is cut down round it) or its floor stands more than {SettleFrom} m over");
                sb.AppendLine($"the ground at a wall (a block foundation is drawn under the floor; a walk-out basement wall at the back over {WalkoutFrom} m).");
                sb.AppendLine("The floor never moves, so garages and front doors meet their drives as before. Worst first.");
                sb.AppendLine($"{rows.Count} houses changed: {cut} lots cut, {found} block foundations, {walk} walk-out basement walls; {left} still buried more than {SettleFrom} m (see the line).");
                sb.AppendLine();
                foreach (var row in rows.OrderByDescending(r => r.key)) sb.AppendLine(row.line);
                string dir = Path.Combine(ProjectRoot, "Data/screenshots/round27");
                Directory.CreateDirectory(dir);
                File.WriteAllText(Path.Combine(dir, "houses_settled.txt"), sb.ToString());
                Debug.Log($"[Round27] houses settled: {rows.Count} ({cut} lots cut, {found} foundations, {walk} walk-outs, {left} still buried > {SettleFrom} m); list in Data/screenshots/round27/houses_settled.txt");
            }
        }

        // the walk-out basement wall under the back of a house whose back stands more than WalkoutFrom over the ground: low windows,
        // and a door where the wall is tall enough, each set at the ground under it
        static void Walkout(MeshBuilder d, HouseSpec h, float exposure, System.Func<float, float, float> groundAt)
        {
            var f0 = h.frame;
            var b = new Frame { O = f0.O, R = -f0.R, F = -f0.F, M = f0.M };
            float back = h.D / 2, u0 = -h.W / 2 + 0.8f, u1 = h.W / 2 - 0.8f;
            if (h.style == HouseStyle.B) u1 = h.W / 2 - GarageWidth(h) - 0.5f;
            bool door = false;
            int k = 0;
            for (float uc = u0 + 0.6f; uc < u1 - 0.5f; uc += 2.4f, k++)
            {
                var wp = b.P(uc, 0f, back + 0.3f);
                float gy = groundAt(wp.x, wp.z) - h.floorY;   // < 0: the ground under this spot
                if (!door && gy < -2.35f && k % 2 == 1)
                {
                    FrontQuad(d, b, uc - 0.52f, uc + 0.52f, gy + 0.02f, gy + 2.2f, back + 0.02f, h.trimC);
                    FrontQuad(d, b, uc - 0.45f, uc + 0.45f, gy + 0.02f, gy + 2.12f, back + 0.04f, Shade(h.doorC, 1.2f));
                    FrontQuad(d, b, uc - 0.3f, uc + 0.3f, gy + 1.2f, gy + 1.95f, back + 0.055f, h.glassC);
                    door = true; continue;
                }
                if (gy < -1.2f) WindowF(d, b, uc, Mathf.Max(gy + 0.75f, -0.75f), 0.9f, 0.5f, back, h, false, false);
            }
        }
    }
}
