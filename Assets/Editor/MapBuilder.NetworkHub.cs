using System.Collections.Generic;
using System.Linq;
using Racer;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// Round 23 follow-ups, Part C stage 1: THE HUB (Dan, 2026-10-12: "a main hub that is a big open chasm with water flowing in from
// all the sources"). A concrete chasm 110 x 70 m under the woods north of the creek valley (Data/pipes/network.json, chamber
// 'HUB'): its floor 252 m, a mid ledge (262 m) and a top ledge (272 m) all round its walls, the ceiling as high as the ground
// over it allows (1.8 m of cover). The pipes come in at the level of their ledge (Tools/mapgen/pipe_network.py): at the top
// ledge through the outer wall, at the mid ledge through the top ledge's face, at the floor through the mid ledge's face.
//   ramps      floor to the mid ledge along the north side, mid ledge to the top ledge along the east end (20 %)
//   waterfalls every pipe that comes in at a ledge pours its water across the ledge and off its edge, down to the next level and
//              on to the floor (moving sheets, mist, a roar: NetworkWaterfall), and the water runs across the floor to the pool
//   the pool   in the middle of the floor, 30 x 14 m, 0.5 m deep with shallow ends (ShallowWater: it slows, never traps)
//   light      lamps on the walls over the ledges and uplights along the floor; the vehicles' own headlights
//   resets     a DrainPipe for the floor, each ledge side and each ramp (a reset puts you back on the level you were on)
// Nothing on the ledges' edges: falling off is part of the fun (the ramps bring you back up).
namespace ChastainRush
{
    public static partial class MapBuilder
    {
        const float HubTopW = 7f, HubMidW = 9f, HubRampW = 8f, HubRampGrade = 0.2f, HubGrateHalf = 2f;
        // two steel grates in the woods floor over the hub, above the floor either side of the pool: daylight shafts down into it
        static IEnumerable<Vector2> HubGrates(NetChamber c)
        {
            var u = new Vector2(Mathf.Sin(c.yaw * Mathf.Deg2Rad), Mathf.Cos(c.yaw * Mathf.Deg2Rad));
            foreach (float a in new[] { -26f, 26f }) yield return new Vector2(c.x, c.z) + u * a;
        }
        static Material hubPool, waterfallMat;

        static void BuildHub(TerrainGrid terrain, Materials mats, Transform parent, NetChamber c, Network n, NetCells cells)
        {
            var root = cells.Make(parent, "THE HUB");
            var u3 = new Vector3(Mathf.Sin(c.yaw * Mathf.Deg2Rad), 0f, Mathf.Cos(c.yaw * Mathf.Deg2Rad)); var v3 = new Vector3(u3.z, 0f, -u3.x); var up = Vector3.up;
            float hl = c.length * 0.5f, hw = c.width * 0.5f, y0 = c.floor, ym = c.mid, yt = c.top, yc = c.ceiling;
            float iM = c.inset_mid, iF = c.inset_floor;   // the top ledge's face, the mid ledge's face (in from the outer wall)
            var O = new Vector3(c.x, 0f, c.z);
            Vector3 P(float a, float b, float y) => O + u3 * a + v3 * b + up * y;
            var room = new UvMesh();
            void Tiles(float a0, float a1, float b0, float b1, float y, Vector3 facing, System.Func<float, float, bool> skip = null)
            {
                for (float a = a0; a < a1 - 1e-3f; a += 2f)
                    for (float b = b0; b < b1 - 1e-3f; b += 2f)
                    {
                        float aa = Mathf.Min(a + 2f, a1), bb = Mathf.Min(b + 2f, b1);
                        if (skip != null && skip((a + aa) * 0.5f, (b + bb) * 0.5f)) continue;
                        room.Quad(P(a, b, y), P(aa, b, y), P(aa, bb, y), P(a, bb, y), new Vector2(a / 2.4f, 0.02f), new Vector2(aa / 2.4f, 0.02f), new Vector2(aa / 2.4f, 0.04f), new Vector2(a / 2.4f, 0.04f), facing);
                    }
            }
            // the pool's basin in the floor's middle
            const float PoolHA = 15f, PoolHB = 7f, PoolDepth = 0.5f, PoolShelf = 3f;
            bool InPool(float a, float b) => Mathf.Abs(a) < PoolHA && Mathf.Abs(b) < PoolHB;
            // floor, mid ledge band, top ledge band, ceiling
            Tiles(-(hl - iF), hl - iF, -(hw - iF), hw - iF, y0, up, InPool);
            void Band(float inO, float inI, float y)   // the band between the rectangle inset inO and the one inset inI, at y
            {
                Tiles(-(hl - inO), hl - inO, -(hw - inO), -(hw - inI), y, up);            // the -b side, full length
                Tiles(-(hl - inO), hl - inO, hw - inI, hw - inO, y, up);                  // the +b side
                Tiles(-(hl - inO), -(hl - inI), -(hw - inI), hw - inI, y, up);            // the -a end
                Tiles(hl - inI, hl - inO, -(hw - inI), hw - inI, y, up);                  // the +a end
            }
            Band(iM, iF, ym); Band(0f, iM, yt);
            var grates = HubGrates(c).ToList();
            bool InGrate(float a, float b) { var q = new Vector2((P(a, b, 0f)).x, (P(a, b, 0f)).z); return grates.Any(g => Mathf.Abs(q.x - g.x) < HubGrateHalf && Mathf.Abs(q.y - g.y) < HubGrateHalf); }
            Tiles(-hl, hl, -hw, hw, yc, -up, InGrate);   // the ceiling, open under the grates
            // the basin: its bottom, its sides sloping in from the shallow ends (1 in 6 along a), steep at the sides
            {
                var bm = room;
                float yb = y0 - PoolDepth;
                Vector3 B(float a, float b) { float shelf = Mathf.Clamp01((PoolHA - Mathf.Abs(a)) / PoolShelf); return P(a, b, Mathf.Lerp(y0 - 0.02f, yb, shelf)); }
                for (float a = -PoolHA; a < PoolHA - 1e-3f; a += 1f)
                    for (float b = -PoolHB; b < PoolHB - 1e-3f; b += 1f)
                        bm.Quad(B(a, b), B(a + 1f, b), B(a + 1f, b + 1f), B(a, b + 1f), new Vector2(a / 2.4f, 0.03f), new Vector2((a + 1f) / 2.4f, 0.03f), new Vector2((a + 1f) / 2.4f, 0.05f), new Vector2(a / 2.4f, 0.05f), up);
                foreach (float sb in new[] { -1f, 1f })
                    for (float a = -PoolHA; a < PoolHA - 1e-3f; a += 1f)
                        bm.Quad(B(a, sb * PoolHB), B(a + 1f, sb * PoolHB), P(a + 1f, sb * PoolHB, y0), P(a, sb * PoolHB, y0), new Vector2(a / 2.4f, 0.03f), new Vector2((a + 1f) / 2.4f, 0.03f), new Vector2((a + 1f) / 2.4f, 0.05f), new Vector2(a / 2.4f, 0.05f), -v3 * sb);
            }
            // the walls, each level's with its pipes' openings
            var holesBy = new Dictionary<(float inset, int wall), (List<float> t, List<float> y, List<float> r)>();
            foreach (var o in c.openings)
            {
                var key = (o.inset, o.wall);
                if (!holesBy.TryGetValue(key, out var hb)) holesBy[key] = hb = (new List<float>(), new List<float>(), new List<float>());
                hb.t.Add(o.t); hb.y.Add(0f); hb.r.Add(o.d * 0.5f);
            }
            void Walls(float inset, float baseY, float h)
            {
                float a = hl - inset, b = hw - inset;
                (List<float>, List<float>, List<float>) H(int w) => holesBy.TryGetValue((inset, w), out var hb) ? hb : (new List<float>(), new List<float>(), new List<float>());
                var h0 = H(0); var h1 = H(1); var h2 = H(2); var h3 = H(3);
                ChamberWall(room, P(a, -b, baseY), v3, 2f * b, -u3, h, h0.Item1, h0.Item2, h0.Item3, b);
                ChamberWall(room, P(-a, b, baseY), u3, 2f * a, -v3, h, h1.Item1, h1.Item2, h1.Item3, a);
                ChamberWall(room, P(-a, -b, baseY), v3, 2f * b, u3, h, h2.Item1, h2.Item2, h2.Item3, b);
                ChamberWall(room, P(-a, -b, baseY), u3, 2f * a, v3, h, h3.Item1, h3.Item2, h3.Item3, a);
            }
            Walls(0f, yt, yc - yt);        // over the top ledge: the outer walls up to the ceiling
            Walls(iM, ym, yt - ym);        // the top ledge's face, from the mid ledge up
            Walls(iF, y0, ym - y0);        // the mid ledge's face, from the floor up
            // the ramps: floor to mid along the -b side of the floor, mid to top along the +a end of the mid ledge
            void Ramp(Vector3 lo, Vector3 hi, Vector3 across, float w, float yLo, float yHi, string name)
            {
                // top surface, the open side's face and the high end's face (the rest is against a wall)
                int segs = 25;
                for (int k = 0; k < segs; k++)
                {
                    float t0 = k / (float)segs, t1 = (k + 1) / (float)segs;
                    var a0 = Vector3.Lerp(lo, hi, t0) + up * Mathf.Lerp(yLo, yHi, t0); var a1 = Vector3.Lerp(lo, hi, t1) + up * Mathf.Lerp(yLo, yHi, t1);
                    room.Quad(a0, a1, a1 + across * w, a0 + across * w, new Vector2(t0 * 20f, 0.02f), new Vector2(t1 * 20f, 0.02f), new Vector2(t1 * 20f, 0.04f), new Vector2(t0 * 20f, 0.04f), up);
                    var f0 = a0 + across * w; var f1 = a1 + across * w;
                    room.Quad(f0, f1, new Vector3(f1.x, yLo - 0.3f, f1.z), new Vector3(f0.x, yLo - 0.3f, f0.z), new Vector2(t0 * 20f, 0.3f), new Vector2(t1 * 20f, 0.3f), new Vector2(t1 * 20f, 0.25f), new Vector2(t0 * 20f, 0.25f), across);
                }
                var e0 = hi + up * yHi; var dir = (hi - lo).normalized;
                room.Quad(e0, e0 + across * w, new Vector3((e0 + across * w).x, yLo - 0.3f, (e0 + across * w).z), new Vector3(e0.x, yLo - 0.3f, e0.z), new Vector2(0f, 0.3f), new Vector2(3f, 0.3f), new Vector2(3f, 0.25f), new Vector2(0f, 0.25f), dir);
                // resets on it
                var host = new GameObject("Hub " + name + " runtime"); host.transform.SetParent(root, false);
                var dp = host.AddComponent<DrainPipe>(); dp.id = "HUB " + name; dp.halfWidth = w * 0.5f - 0.2f; dp.clearHeight = 6f; dp.radius = 0.1f;
                var fl = new List<Vector3>(); for (int k = 0; k <= 50; k++) { float t = k / 50f; fl.Add(Vector3.Lerp(lo, hi, t) + across * (w * 0.5f) + up * Mathf.Lerp(yLo, yHi, t)); }
                dp.floor = fl.ToArray(); dp.barrelFrom = 0f; dp.barrelTo = 0f;
            }
            // each ramp runs head-on into the ledge it climbs to (its top meets that ledge's face at the ledge's height: you drive straight
            // on; the exe check sent a car off the end of ramps that stopped beside the ledge instead)
            float rampLen = (ym - y0) / HubRampGrade;
            // floor to the mid ledge: along the floor's -b side, up toward +a, into the mid ledge's +a band (its face at a = hl - iF)
            Ramp(O + u3 * ((hl - iF) - rampLen) - v3 * (hw - iF), O + u3 * (hl - iF) - v3 * (hw - iF), v3, HubRampW, y0, ym, "ramp floor to mid ledge");
            // mid to the top ledge: along the mid ledge's +b band, up toward -a, into the top ledge's -a band (its face at a = -(hl - iM))
            Ramp(O + u3 * (-(hl - iM) + rampLen) + v3 * (hw - iM), O + u3 * (-(hl - iM)) + v3 * (hw - iM), -v3, HubRampW, ym, yt, "ramp mid to top ledge");
            // the grates' shafts: square concrete walls from the ceiling up to the ground, a steel grate over each at the ground (solid:
            // a car drives over it), a hole in the terrain under it, daylight down through it (a spotlight and soft beams)
            var grateMb = new MeshBuilder(); var beams = new List<(Vector3 top, float drop)>();
            foreach (var g in grates)
            {
                float gy = Ground(terrain, g.x, g.y); float top = gy + 0.05f;
                var gc = new Vector3(g.x, 0f, g.y); var ex = Vector3.right * HubGrateHalf; var ez = Vector3.forward * HubGrateHalf;
                foreach (var (o, al, inw) in new[] { (gc - ex - ez, Vector3.right, Vector3.forward), (gc + ex - ez, Vector3.forward, Vector3.left), (gc + ex + ez, Vector3.left, Vector3.back), (gc - ex + ez, Vector3.back, Vector3.right) })
                    room.Quad(o + up * yc, o + al * (2f * HubGrateHalf) + up * yc, o + al * (2f * HubGrateHalf) + up * top, o + up * top, new Vector2(0f, 0.3f), new Vector2(1.6f, 0.3f), new Vector2(1.6f, 0.5f), new Vector2(0f, 0.5f), inw);
                SetHoles(terrain, new Rect(g.x - HubGrateHalf - 0.6f, g.y - HubGrateHalf - 0.6f, 2f * HubGrateHalf + 1.2f, 2f * HubGrateHalf + 1.2f), q => Mathf.Abs(q.x - g.x) < HubGrateHalf + 0.3f && Mathf.Abs(q.y - g.y) < HubGrateHalf + 0.3f);
                var steel = new Color(0.26f, 0.27f, 0.28f);
                for (float x = -HubGrateHalf; x <= HubGrateHalf + 1e-3f; x += 0.25f) Beam(grateMb, new Vector3(g.x + x, top, g.y - HubGrateHalf - 0.3f), new Vector3(g.x + x, top, g.y + HubGrateHalf + 0.3f), 0.06f, 0.1f, steel);
                foreach (float z in new[] { -HubGrateHalf - 0.3f, 0f, HubGrateHalf + 0.3f }) Beam(grateMb, new Vector3(g.x - HubGrateHalf - 0.3f, top + 0.02f, g.y + z), new Vector3(g.x + HubGrateHalf + 0.3f, top + 0.02f, g.y + z), 0.12f, 0.12f, steel);
                var gcol = new GameObject("Hub grate (solid)"); gcol.transform.SetParent(root, false); gcol.transform.position = new Vector3(g.x, top - 0.05f, g.y);
                gcol.AddComponent<BoxCollider>().size = new Vector3(2f * HubGrateHalf + 0.6f, 0.2f, 2f * HubGrateHalf + 0.6f);
                var sl = new GameObject("Hub daylight"); sl.transform.SetParent(root, false); sl.transform.SetPositionAndRotation(new Vector3(g.x, yc - 0.5f, g.y), Quaternion.LookRotation(Vector3.down));
                var l = sl.AddComponent<Light>(); l.type = LightType.Spot; l.range = (yc - y0) + 6f; l.spotAngle = 38f; l.intensity = 4f; l.color = new Color(1f, 0.96f, 0.86f); l.shadows = LightShadows.None;
                beams.Add((new Vector3(g.x, yc, g.y), yc - y0));
            }
            if (grateMb.Count > 0) grateMb.ToObject("Hub grates", mats.House, root.parent, false, false);   // not in the hub's cell: seen from the woods
            if (beams.Count > 0)
            {
                ClearMaterials();
                var bv = new List<Vector3>(); var buv = new List<Vector2>(); var bc = new List<Color>(); var bt = new List<int>();
                var sun = new Vector3(0.12f, -1f, 0.08f).normalized; int k = 0;
                foreach (var (top, drop) in beams)
                    for (int j = 0; j < 3; j++)
                        foreach (var side in new[] { Vector3.right, Vector3.forward })
                        {
                            var a0 = top + new Vector3((j - 1) * 1.1f, 0f, (j - 1) * 0.7f); var b0 = a0 + sun * (drop / -sun.y); float w = 1.1f + 0.4f * j;
                            int v0 = bv.Count; bv.Add(a0 - side * w); bv.Add(a0 + side * w); bv.Add(b0 + side * w * 1.6f); bv.Add(b0 - side * w * 1.6f);
                            buv.Add(new Vector2(0, 0)); buv.Add(new Vector2(1, 0)); buv.Add(new Vector2(1, 1)); buv.Add(new Vector2(0, 1));
                            var col = new Color(Hash01(k, 3301), 0f, 0f, 0.7f); for (int q = 0; q < 4; q++) bc.Add(col); k++;
                            bt.Add(v0); bt.Add(v0 + 1); bt.Add(v0 + 2); bt.Add(v0); bt.Add(v0 + 2); bt.Add(v0 + 3);
                        }
                var bm = new Mesh { name = "Hub daylight beams" }; bm.SetVertices(bv); bm.SetUVs(0, buv); bm.SetColors(bc); bm.SetTriangles(bt, 0); bm.RecalculateBounds();
                AssetDatabase.CreateAsset(bm, $"{GeneratedDir}/Meshes/Hub daylight beams.asset");
                var bgo = new GameObject("Hub daylight beams"); bgo.transform.SetParent(root, false); bgo.AddComponent<MeshFilter>().sharedMesh = bm;
                var br = bgo.AddComponent<MeshRenderer>(); br.sharedMaterial = clearShaft; br.shadowCastingMode = ShadowCastingMode.Off;
            }
            room.ToObject("sc HUB", pipeConcrete, root, true);
            // the pool's water
            {
                if (!hubPool)
                {
                    hubPool = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "HubPool" };
                    hubPool.SetColor("_BaseColor", new Color(0.16f, 0.34f, 0.40f)); hubPool.SetFloat("_Smoothness", 0.96f);
                    AssetDatabase.CreateAsset(hubPool, GeneratedDir + "/HubPool.mat");
                }
                float ws = y0 - 0.04f; var wm = new MeshBuilder();
                Face(wm, P(-PoolHA, -PoolHB, ws), P(PoolHA, -PoolHB, ws), P(PoolHA, PoolHB, ws), P(-PoolHA, PoolHB, ws), up);
                wm.ToObject("HUB pool water", hubPool, root, false, false);
                var wgo = new GameObject("HUB pool (water volume)"); wgo.transform.SetParent(root, false);
                var sw = wgo.AddComponent<ShallowWater>(); sw.surfaceY = ws;
                sw.outline = new[] { P(-PoolHA, -PoolHB, 0f), P(PoolHA, -PoolHB, 0f), P(PoolHA, PoolHB, 0f), P(-PoolHA, PoolHB, 0f) }.Select(q => new Vector2(q.x, q.z)).ToArray();
            }
            // waterfalls: from every opening at a ledge, across the ledge, off its edge, on down; then across the floor to the pool
            int falls = 0;
            foreach (var o in c.openings)
            {
                if (o.lvl != "top" && o.lvl != "mid") continue;
                var tin = n.F.tubes.FirstOrDefault(t => t.id == o.tube);
                if (tin == null || tin.b == null || tin.b.chamber != c.id) continue;   // only a pipe that runs into the hub brings water in
                var nrm = new[] { u3, v3, -u3, -v3 }[o.wall]; var along = new[] { v3, u3, v3, u3 }[o.wall];
                float half = (o.wall == 0 || o.wall == 2) ? hl : hw;
                var mouth = O + nrm * (half - o.inset) + along * o.t;
                float yLedge = o.lvl == "top" ? yt : ym;
                var path = new List<(Vector3 from, Vector3 to, bool fall)>();
                var p0 = mouth + up * yLedge; var p1 = mouth - nrm * (o.lvl == "top" ? HubTopW : HubMidW) + up * yLedge;
                path.Add((p0, p1, false));
                if (o.lvl == "top")
                {
                    var p2 = new Vector3(p1.x, ym, p1.z); path.Add((p1, p2, true));
                    var p3 = p2 - nrm * HubMidW; path.Add((p2, p3, false)); p1 = p3;
                }
                var q2 = new Vector3(p1.x, y0, p1.z); path.Add((p1, q2, true));
                var poolEdge = O + up * y0 + Vector3.ClampMagnitude(new Vector3((q2 - O).x, 0f, (q2 - O).z), 1f) * 0f;
                // on across the floor toward the pool's nearest edge
                var rel = q2 - (O + up * y0); float ra = Mathf.Clamp(Vector3.Dot(rel, u3), -PoolHA + 1f, PoolHA - 1f), rb = Mathf.Clamp(Vector3.Dot(rel, v3), -PoolHB + 1f, PoolHB - 1f);
                var target = O + u3 * ra + v3 * rb + up * y0;
                path.Add((q2, target, false));
                BuildFall(root, path, o.d >= 5f ? 2.2f : 1.6f, "HUB waterfall " + o.tube);
                falls++;
            }
            // lamps: on the outer walls over the top ledge, over the mid ledge on the top ledge's face, uplights at the floor's edge
            var lampMb = new MeshBuilder(); int lamps = 0;
            void Lamp(Vector3 at, Vector3 facing, float range, float intensity)
            {
                lampMb.Tint = Color.white; BoxAt(lampMb, at, Vector3.Cross(up, facing), 1.2f, 0.25f, 0.35f);
                var lg = new GameObject("Hub lamp"); lg.transform.SetParent(root, false); lg.transform.position = at + facing * 0.8f;
                var l = lg.AddComponent<Light>(); l.type = LightType.Point; l.range = range; l.intensity = intensity; l.color = new Color(1f, 0.80f, 0.55f); l.shadows = LightShadows.None; lamps++;
            }
            foreach (var (a, b, f) in new[] { (-0.5f, -1f, v3), (0.5f, -1f, v3), (-0.5f, 1f, -v3), (0.5f, 1f, -v3), (-1f, 0f, u3), (1f, 0f, -u3) })
            {
                Lamp(P(a * (hl - 1f) * (Mathf.Abs(b) > 0f ? 0.9f : 1f), b * (hw - 0.3f), yc - 1.2f), f, 30f, 2.2f);
                Lamp(P(a * (hl - iM) * 0.9f, b * (hw - iM - 0.3f), yt - 2.5f), f, 24f, 1.8f);
            }
            foreach (float a in new[] { -0.7f, 0f, 0.7f }) foreach (float b in new[] { -1f, 1f }) Lamp(P(a * (hl - iF), b * (hw - iF - 0.4f), y0 + 0.6f), -v3 * b, 26f, 1.8f);
            var lgo = lampMb.ToObject("HUB lamps", pipeLamp, root, false); lgo.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            // signs: THE HUB on the long walls, the mains' names over their openings
            BuildChamberSigns(root, c, n, O + up * y0, u3, v3, hl, hw);
            // resets: the floor, every ledge side
            void Reset(string name, Vector3 a, Vector3 b, float halfW, float clear)
            {
                var host = new GameObject("Hub " + name + " runtime"); host.transform.SetParent(root, false);
                var dp = host.AddComponent<DrainPipe>(); dp.id = "HUB " + name; dp.halfWidth = halfW; dp.clearHeight = clear; dp.radius = 0.1f; dp.barrelFrom = 0f; dp.barrelTo = 0f;
                var fl = new List<Vector3>(); int k = Mathf.CeilToInt(Vector3.Distance(a, b) / 0.5f); for (int i = 0; i <= k; i++) fl.Add(Vector3.Lerp(a, b, i / (float)k)); dp.floor = fl.ToArray();
            }
            Reset("floor", P(-(hl - iF) - 0.5f, 0f, y0), P(hl - iF + 0.5f, 0f, y0), hw - iF - 0.3f, ym - y0 - 0.6f);
            foreach (var (inO, inI, y, clear, nm) in new[] { (iM, iF, ym, yt - ym - 0.6f, "mid ledge"), (0f, iM, yt, yc - yt - 0.3f, "top ledge") })
            {
                float mid = (inO + inI) * 0.5f, hbw = (inI - inO) * 0.5f - 0.2f;
                Reset(nm + " -b", P(-(hl - inO), -(hw - mid), y), P(hl - inO, -(hw - mid), y), hbw, clear);   // to the corners
                Reset(nm + " +b", P(-(hl - inO), hw - mid, y), P(hl - inO, hw - mid, y), hbw, clear);
                Reset(nm + " -a", P(-(hl - mid), -(hw - inO), y), P(-(hl - mid), hw - inO, y), hbw, clear);
                Reset(nm + " +a", P(hl - mid, -(hw - inO), y), P(hl - mid, hw - inO, y), hbw, clear);
            }
            float least = float.MaxValue;
            for (float a = -hl; a <= hl; a += 4f) for (float b = -hw; b <= hw; b += 4f) { var q = P(a, b, 0f); least = Mathf.Min(least, Ground(terrain, q.x, q.z) - (yc + 0.3f)); }
            Debug.Log($"[Round23+] THE HUB: {c.length:0} x {c.width:0} m, floor {y0:0} m, mid ledge {ym:0} m ({HubMidW:0} m wide), top ledge {yt:0} m ({HubTopW:0} m wide), ceiling {yc:0.0} m; least ground over the ceiling {least:0.0} m; {c.openings.Length} pipes in, {falls} waterfalls, {lamps} lamps, ramps at {HubRampGrade * 100f:0} %, a pool {2f * PoolHA:0} x {2f * PoolHB:0} m");
            cells.Cells.Add((root, new Bounds(O + up * ((y0 + yc) * 0.5f), new Vector3(2f * hl + 10f, yc - y0 + 10f, 2f * hl + 10f))));
        }

        // a waterfall's way: moving water along the flat legs, falling sheets on the drops (NetworkWaterfall: the roar, the mist)
        static void BuildFall(Transform parent, List<(Vector3 from, Vector3 to, bool fall)> path, float half, string name)
        {
            if (!waterfallMat)
            {
                waterfallMat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit")) { name = "NetworkWaterfall" };
                waterfallMat.SetTexture("_BaseMap", pipeWater.GetTexture("_BaseMap")); waterfallMat.SetColor("_BaseColor", new Color(0.80f, 0.90f, 0.95f, 0.75f));
                MakeTransparent(waterfallMat); waterfallMat.SetFloat("_Cull", 0f);
                AssetDatabase.CreateAsset(waterfallMat, GeneratedDir + "/NetworkWaterfall.mat");
            }
            var go = new GameObject(name); go.transform.SetParent(parent, false);
            var sv = new List<Vector3>(); var suv = new List<Vector2>(); var st = new List<int>();   // the flat runs (pipeWater, PipeStream)
            var fv = new List<Vector3>(); var fuv = new List<Vector2>(); var ft = new List<int>();   // the falls
            var line = new List<Vector3>(); var bottoms = new List<Vector3>(); float run = 0f;
            foreach (var (a, b, fall) in path)
            {
                var d = b - a; if (d.magnitude < 0.05f) continue;
                if (fall)
                {
                    var flat = path.First(p => !p.fall && p.to == a).from; var side = Vector3.Cross(Vector3.up, (a - flat).normalized).normalized;
                    int v0 = fv.Count; float h = a.y - b.y; var lip = (a - flat).normalized * 0.4f;
                    fv.Add(a - side * half + Vector3.up * 0.04f); fv.Add(a + side * half + Vector3.up * 0.04f); fv.Add(b + lip + side * half * 1.3f); fv.Add(b + lip - side * half * 1.3f);
                    fuv.Add(new Vector2(0f, 0f)); fuv.Add(new Vector2(1f, 0f)); fuv.Add(new Vector2(1f, h / 2f)); fuv.Add(new Vector2(0f, h / 2f));
                    ft.Add(v0); ft.Add(v0 + 1); ft.Add(v0 + 2); ft.Add(v0); ft.Add(v0 + 2); ft.Add(v0 + 3);
                    bottoms.Add(b + lip);
                    continue;
                }
                var dir = new Vector3(d.x, 0f, d.z).normalized; var sd = Vector3.Cross(Vector3.up, dir).normalized; int steps = Mathf.Max(1, Mathf.CeilToInt(d.magnitude / 0.5f));
                int first = sv.Count;
                for (int i = 0; i <= steps; i++)
                {
                    var c = Vector3.Lerp(a, b, i / (float)steps) + Vector3.up * 0.04f; float hh = half * (0.85f + 0.15f * Mathf.Sin(run * 0.8f));
                    sv.Add(c - sd * hh); sv.Add(c + sd * hh); suv.Add(new Vector2(0f, -run / 3f)); suv.Add(new Vector2(1f, -run / 3f)); line.Add(c);
                    if (i > 0) { int k = first + (i - 1) * 2; st.Add(k); st.Add(k + 1); st.Add(k + 2); st.Add(k + 1); st.Add(k + 3); st.Add(k + 2); }
                    run += i < steps ? d.magnitude / steps : 0f;
                }
            }
            if (st.Count > 0)
            {
                var n0 = Vector3.Cross(sv[st[1]] - sv[st[0]], sv[st[2]] - sv[st[0]]); if (n0.y < 0f) for (int i = 0; i < st.Count; i += 3) { int x = st[i + 1]; st[i + 1] = st[i + 2]; st[i + 2] = x; }
                var m = new Mesh { name = name + " runs", indexFormat = IndexFormat.UInt32 }; m.SetVertices(sv); m.SetUVs(0, suv); m.SetTriangles(st, 0); m.RecalculateNormals(); m.RecalculateBounds();
                AssetDatabase.CreateAsset(m, $"{GeneratedDir}/Meshes/{name} runs.asset");
                var r = new GameObject(name + " runs"); r.transform.SetParent(go.transform, false); r.AddComponent<MeshFilter>().sharedMesh = m;
                var mr = r.AddComponent<MeshRenderer>(); mr.sharedMaterial = pipeWater; mr.shadowCastingMode = ShadowCastingMode.Off;
                var ps = r.AddComponent<PipeStream>(); ps.line = line.ToArray(); ps.water = pipeWater; ps.splash = pipeSplash;
            }
            if (ft.Count > 0)
            {
                var m = new Mesh { name = name + " falls" }; m.SetVertices(fv); m.SetUVs(0, fuv); m.SetTriangles(ft, 0); m.RecalculateNormals(); m.RecalculateBounds();
                AssetDatabase.CreateAsset(m, $"{GeneratedDir}/Meshes/{name} falls.asset");
                var f = new GameObject(name + " falls"); f.transform.SetParent(go.transform, false); f.AddComponent<MeshFilter>().sharedMesh = m;
                var mr = f.AddComponent<MeshRenderer>(); mr.sharedMaterial = waterfallMat; mr.shadowCastingMode = ShadowCastingMode.Off;
                var wf = go.AddComponent<NetworkWaterfall>(); wf.material = waterfallMat; wf.bottoms = bottoms.ToArray(); wf.splash = pipeSplash;
            }
        }

        // ---------------------------------------------------------------- painted signs (Assets/HandMade/Network/NetworkSigns.png)
        // rows of the atlas, top to bottom (Tools/handmade/make_network_signs.py writes them in this order)
        static readonly string[] NetSignRows = { "M1 TRUNK", "M5 ENCLAVE MAIN", "M3 NOONDAY MAIN", "M4 GLEN MAIN", "M2 LAKES MAIN", "M6 NELSON MAIN", "THE HUB",
                                                  "J1", "J2", "J3", "J4", "J5", "J6", "J7", "J8", "J9", "J10", "J12", "J13" };
        const string NetSignAtlas = "Assets/HandMade/Network/NetworkSigns.png";
        static Material netSigns;

        static void BuildChamberSigns(Transform root, NetChamber c, Network n, Vector3 C, Vector3 u3, Vector3 v3, float hl, float hw)
        {
            if (!netSigns) netSigns = HandMadeSignMaterial(NetSignAtlas, "NetworkSigns", true);
            if (!netSigns) { Debug.LogWarning("[Round23+] no network sign atlas (run Tools/handmade/make_network_signs.py)"); return; }
            var mesh = new UvMesh(); int rows = NetSignRows.Length; int signs = 0;
            void Plate(Vector3 center, Vector3 facing, float w, float h, int row)
            {
                var right = Vector3.Cross(Vector3.up, facing).normalized * -1f;   // seen from in front (facing toward the viewer), left to right
                float v0 = 1f - (row + 1) / (float)rows, v1 = 1f - row / (float)rows;
                var a = center - right * (w * 0.5f) - Vector3.up * (h * 0.5f); var b = center + right * (w * 0.5f) - Vector3.up * (h * 0.5f);
                mesh.Quad(a, b, b + Vector3.up * h, a + Vector3.up * h, new Vector2(0f, v0), new Vector2(1f, v0), new Vector2(1f, v1), new Vector2(0f, v1), facing);
                signs++;
            }
            bool hub = c.kind == "hub";
            foreach (var o in c.openings)
            {
                var tube = n.F.tubes.FirstOrDefault(t => t.id == o.tube);
                if (tube == null || !tube.main) continue;
                int row = System.Array.IndexOf(NetSignRows, tube.name.Split('(')[0].Trim().ToUpperInvariant());
                if (row < 0) continue;
                var nrm = new[] { u3, v3, -u3, -v3 }[o.wall]; var along = new[] { v3, u3, v3, u3 }[o.wall];
                float half = (o.wall == 0 || o.wall == 2) ? hl : hw;
                float baseY = hub ? (o.lvl == "top" ? c.top : o.lvl == "mid" ? c.mid : c.floor) - c.floor : (o.high ? c.drop : 0f);
                float top = hub ? (o.lvl == "top" ? c.ceiling - c.floor : o.lvl == "mid" ? c.top - c.floor : c.mid - c.floor) : c.height;
                float y = Mathf.Min(baseY + o.d + 0.95f, top - 0.6f);
                Plate(C + nrm * (half - o.inset - 0.03f) + along * o.t + Vector3.up * y, -nrm, 4.8f, 0.9f, row);
            }
            // the chamber's own name on its walls (the hub: THE HUB on both long walls, over the top ledge)
            int nameRow = System.Array.IndexOf(NetSignRows, hub ? "THE HUB" : c.id);
            if (nameRow >= 0)
            {
                if (hub) foreach (float sb in new[] { -1f, 1f }) Plate(C + v3 * (sb * (hw - 0.03f)) + Vector3.up * (c.ceiling - c.floor - 2.2f), -v3 * sb, 12f, 2.25f, nameRow);
                else
                {
                    // on the wall with the fewest openings, high up
                    int w = Enumerable.Range(0, 4).OrderBy(k => c.openings.Count(o => o.wall == k)).First();
                    var nrm = new[] { u3, v3, -u3, -v3 }[w]; float half = (w == 0 || w == 2) ? hl : hw;
                    Plate(C + nrm * (half - 0.03f) + Vector3.up * (c.height - 1.6f), -nrm, 4.8f, 0.9f, nameRow);
                }
            }
            if (signs > 0) mesh.ToObject("Signs " + c.id, netSigns, root, false);
        }
    }
}
