using System;
using System.Collections.Generic;
using System.Linq;
using Racer;
using UnityEngine;

// Round 64 (Dan, 2026-10-23 21:06, on the Noonday Creek Trail: his green line "is where I wanted to reroute the track", his red lines "is where i want shortcuts").
// Two of Town Center's anchors opened on their lower floor (MapBuilder.Mall.cs MallHollowShell: their walls, floor, ceiling and roof; their portals' and
// fronts' glass: MallInside.cs MallDoorsGlass):
//   Halverson Home   the north anchor: a home store, its portal on the north lot, its front on the north wing. The race goes through it (the course).
//   Carroway         the south anchor: a department store closed for renovation, its front on the center court, its portal on the south lot. The race's
//                    shortcut (each way) goes through it: the floor torn up right across the store, the renovation pit, flooded; on the line a plywood
//                    kicker on each side of it, each the other's landing (a real jump, physics only: about 18 m/s clears it); short and you are in the
//                    water, out up a plank ramp at the pit's far end (each way its own, at the store's two ends). Nothing else crosses it: no free way round.
// World, not race layer (they stand in Free Roam too). Everything breakable costs no speed (FenceChunk.free).
namespace ChastainRush
{
    public static partial class MapBuilder
    {
        const float MallPitV0 = -1372f, MallPitV1 = -1386f, MallPitDepth = 2.4f, MallPitWater = .7f;   // its north and south edges, depth, the water in it
        const float MallKickLen = 4.5f, MallKickH = 1.2f, MallKickHalf = 2.6f, MallExitW = 6f;     // the kickers (on the line, u MallPitU), the ways out
        const float MallPitU = -105.8f;                                                               // the line through Carroway (its portal's middle)

        // the renovation pit, where Carroway's floor is open
        static bool MallPitArea(MallPlan m, int id, float u, float v) => id == m.South && v < MallPitV0 && v > MallPitV1;

        static void MallAnchorStores(MallPlan m, Transform root, MeshBuilder deco, MeshBuilder lights, MallBreak br, Material signs)
        {
            if (m.North < 0 || m.South < 0) return;
            var go = new GameObject("Mall anchors driven through (Round 64)").transform; go.SetParent(root, false);
            HomeStore(m, go, deco, lights, br, signs);
            Carroway(m, go, deco, lights, br, signs);
        }

        // the inside of an anchor's ring in the frame (0.8 m in from its walls)
        static Rect AnchorBox(MallPlan m, int id)
        {
            var uv = m.Rings[id].Select(m.UV).ToList();
            return Rect.MinMaxRect(uv.Min(q => q.x) + .8f, uv.Min(q => q.y) + .8f, uv.Max(q => q.x) - .8f, uv.Max(q => q.y) - .8f);
        }
        static void AnchorLights(MallPlan m, MeshBuilder lights, Rect r)
        {
            float ceil = m.L1 - MallPlan.Slab - .12f;
            for (float u = r.xMin + 4f; u < r.xMax - 2f; u += 10f) for (float v = r.yMin + 4f; v < r.yMax - 2f; v += 10f) { var lp = m.P(u, v, ceil); SBox(lights, lp, m.D3(1, 0), new Vector3(.6f, .1f, 3f), Color.white); m.Lamps.Add(lp); }
        }

        // ------------------------------------------------------------ Halverson Home: sofas, beds, tables and lamps either side of the aisle (u -6 to 4)
        static void HomeStore(MallPlan m, Transform go, MeshBuilder deco, MeshBuilder lights, MallBreak br, Material signs)
        {
            var r = AnchorBox(m, m.North); float y = m.L0; const float a0 = -6f, a1 = 4f;
            MQuad(deco, m, a0, r.yMin, a1, r.yMax, y + .012f, SC(.88f, .86f, .82f));   // the aisle's tile
            var rnd = new System.Random(2390); int items = 0;
            var fabric = new[] { SC(.42f, .46f, .52f), SC(.62f, .50f, .40f), SC(.30f, .34f, .30f), SC(.72f, .70f, .66f), SC(.50f, .22f, .20f), SC(.25f, .30f, .45f) };
            var wood = new[] { SC(.45f, .30f, .18f), SC(.62f, .46f, .30f), SC(.28f, .20f, .14f) };
            foreach (var (u0, u1) in new[] { (r.xMin + 3f, a0 - 3f), (a1 + 3f, r.xMax - 3f) })
                for (float u = u0; u <= u1; u += 7f)
                    for (float v = r.yMin + 4f; v <= r.yMax - 4f; v += 7f)
                    {
                        var at = m.P(u, v, y); var f = m.D3(0, 1); int kind = rnd.Next(4); var c = fabric[rnd.Next(fabric.Length)]; var w = wood[rnd.Next(wood.Length)];
                        MQuad(deco, m, u - 2.6f, v - 2.2f, u + 2.6f, v + 2.2f, y + .015f, c * .7f);   // a rug under each display
                        if (kind == 0) MItem(br.foam, at, f, 2.4f, .9f, 1f, mb => { SBox(mb, at, f, new Vector3(2.3f, .45f, .95f), c); SBox(mb, at - f * .4f, f, new Vector3(2.3f, .9f, .2f), c * .9f); });                          // a sofa
                        else if (kind == 1) MItem(br.foam, at, f, 1.8f, 1.0f, 2.2f, mb => { SBox(mb, at, f, new Vector3(1.7f, .55f, 2.1f), SC(.92f, .92f, .90f)); SBox(mb, at - f * 1.0f, f, new Vector3(1.8f, 1.1f, .12f), w); });   // a bed
                        else if (kind == 2) MItem(br.wood, at, f, 2.0f, .8f, 1.0f, mb => { SBox(mb, at + Vector3.up * .7f, f, new Vector3(2f, .08f, 1f), w); foreach (float o in new[] { -.85f, .85f }) SBox(mb, at + m.D3(1, 0) * o, f, new Vector3(.08f, .7f, .8f), w * .85f); });   // a table
                        else MItem(br.glass, at, f, .6f, 1.7f, .6f, mb => { SBox(mb, at, f, new Vector3(.3f, .05f, .3f), SC(.2f, .2f, .2f)); SBox(mb, at, f, new Vector3(.05f, 1.4f, .05f), MSteel); SBox(mb, at + Vector3.up * 1.3f, f, new Vector3(.45f, .35f, .45f), SC(.95f, .90f, .78f)); });   // a lamp
                        items++;
                    }
            AnchorLights(m, lights, r);
            if (signs != null) MallPlate(go, m.P((a0 + a1) * .5f, r.yMax - .5f, y + 5.2f), m.D3(0, -1), 10f, .78f, SignHalversonHome, signs);   // over the portal, seen from the store
            m.Log.Add($"Round 64: Halverson Home's lower floor: {items} displays either side of its aisle (u {a0:0} to {a1:0})");
        }

        // ------------------------------------------------------------ Carroway: racks closed off under plastic, the renovation pit, its kickers and ways out
        static void Carroway(MallPlan m, Transform go, MeshBuilder deco, MeshBuilder lights, MallBreak br, Material signs)
        {
            var r = AnchorBox(m, m.South); float y = m.L0, yb = m.L0 - MallPitDepth, uW = r.xMin - .4f, uE = r.xMax + .4f;   // (the pit to the walls' faces)
            var ply = SPly; var plyDark = SPlyDark; var concrete = SC(.62f, .61f, .58f); var dust = SC(.80f, .78f, .74f);
            // the pit: its floor (a "Mall floor" box), its two edges' faces (concrete, solid), the water
            MallSlab(m, deco, deco, go, Rect.MinMaxRect(uW, MallPitV1, uE, MallPitV0), yb, MallPlan.Slab, SC(.36f, .34f, .30f), concrete, "Mall floor", true);
            MallWall(deco, deco, go, m.W(uW, MallPitV0), m.W(uE, MallPitV0), -m.V, yb - .1f, y, concrete, false, concrete, "Mall pit wall");
            MallWall(deco, deco, go, m.W(uE, MallPitV1), m.W(uW, MallPitV1), m.V, yb - .1f, y, concrete, false, concrete, "Mall pit wall");
            var water = new GameObject("Carroway pit water"); water.transform.SetParent(go, false);
            var sw = water.AddComponent<ShallowWater>(); sw.outline = new[] { m.W(uW, MallPitV1), m.W(uE, MallPitV1), m.W(uE, MallPitV0), m.W(uW, MallPitV0) }; sw.surfaceY = yb + MallPitWater;
            var wmb = new MeshBuilder(); Face(wmb, m.P(uW, MallPitV1, yb + MallPitWater), m.P(uE, MallPitV1, yb + MallPitWater), m.P(uE, MallPitV0, yb + MallPitWater), m.P(uW, MallPitV0, yb + MallPitWater), Vector3.up);
            wmb.ToObject("Carroway pit water surface", MallSeeThrough("MallPitWater", new Color(.22f, .34f, .32f, .78f), .95f), water.transform, false, false);
            // the kickers: plywood on cinder blocks, on the line, each rising to its edge of the pit (the forward one from the north, the reverse one from the south)
            var col = new MeshBuilder();
            foreach (var (edge, dir) in new[] { (MallPitV0, -1f), (MallPitV1, 1f) })   // dir: the way a car crosses the pit off it (-1: south)
            {
                float vStart = edge - dir * MallKickLen;   // where it starts on the floor
                Vector3 P(float u, float v, float h) => m.P(u, v, h);
                float H(float v) => y + MallKickH * Mathf.Clamp01((v - vStart) / (edge - vStart));
                const int n = 6;
                for (int i = 0; i < n; i++)
                {
                    float va = Mathf.Lerp(vStart, edge, i / (float)n), vb = Mathf.Lerp(vStart, edge, (i + 1) / (float)n);
                    Vector3 l0 = P(MallPitU - MallKickHalf, va, H(va)), r0 = P(MallPitU + MallKickHalf, va, H(va)), l1 = P(MallPitU - MallKickHalf, vb, H(vb)), r1 = P(MallPitU + MallKickHalf, vb, H(vb));
                    var up = Vector3.Cross(r0 - l0, l1 - l0); if (up.y < 0) up = -up;
                    deco.Tint = SVary(ply, va, i, .04f); Face(deco, l0, r0, r1, l1, up); col.Tint = ply; Face(col, l0, r0, r1, l1, up);
                    foreach (var (u, sgn) in new[] { (MallPitU - MallKickHalf, -1f), (MallPitU + MallKickHalf, 1f) })
                    { var a = P(u, va, H(va)); var b = P(u, vb, H(vb)); var a0 = P(u, va, y); var b0 = P(u, vb, y); deco.Tint = plyDark; Face(deco, a0, b0, b, a, m.D3(sgn, 0)); col.Tint = plyDark; Face(col, a0, b0, b, a, m.D3(sgn, 0)); }
                }
                // its lip's face straight down into the pit (plywood, then the pit's concrete under the floor)
                deco.Tint = plyDark; Face(deco, P(MallPitU - MallKickHalf, edge, yb), P(MallPitU + MallKickHalf, edge, yb), P(MallPitU + MallKickHalf, edge, y + MallKickH), P(MallPitU - MallKickHalf, edge, y + MallKickH), m.D3(0, dir));
                col.Tint = plyDark; Face(col, P(MallPitU - MallKickHalf, edge, y), P(MallPitU + MallKickHalf, edge, y), P(MallPitU + MallKickHalf, edge, y + MallKickH), P(MallPitU - MallKickHalf, edge, y + MallKickH), m.D3(0, dir));
                // cinder blocks under its sides (looks)
                for (int k = 1; k <= 3; k++)
                {
                    float v = Mathf.Lerp(vStart, edge, k / 4f);
                    foreach (float u in new[] { MallPitU - MallKickHalf + .3f, MallPitU + MallKickHalf - .3f }) SBox(deco, m.P(u, v, y) + m.D3(u < MallPitU ? -1 : 1, 0) * .02f, m.D3(0, 1), new Vector3(.4f, Mathf.Max(.05f, H(v) - y - .04f), .2f), SC(.55f, .55f, .55f));
                }
            }
            ColliderMeshMall(col, "Mall ramp Carroway kickers", go);
            // the ways out of the pit: plank ramps at its two ends, each rising the way the race goes on (forward: south, at its west end; reverse: north, at its east end),
            // from 4 m out from the near wall to the far edge
            var exits = new MeshBuilder();
            foreach (var (u0, u1, lowV0, highV) in new[] { (uW, uW + MallExitW, MallPitV0, MallPitV1), (uE - MallExitW, uE, MallPitV1, MallPitV0) })
            {
                float lowV = lowV0 + (highV - lowV0) * (4f / Mathf.Abs(highV - lowV0));   // (check 2: its foot 4 m out from the pit's wall, so a car in the water lines up and drives on)
                const int n = 8;
                for (int i = 0; i < n; i++)
                {
                    float va = Mathf.Lerp(lowV, highV, i / (float)n), vb = Mathf.Lerp(lowV, highV, (i + 1) / (float)n), ha = Mathf.Lerp(yb, y, i / (float)n), hb = Mathf.Lerp(yb, y, (i + 1) / (float)n);
                    Vector3 l0 = m.P(u0, va, ha), r0 = m.P(u1, va, ha), l1 = m.P(u0, vb, hb), r1 = m.P(u1, vb, hb);
                    var up = Vector3.Cross(r0 - l0, l1 - l0); if (up.y < 0) up = -up;
                    deco.Tint = SVary(ply, va, u0, .04f); Face(deco, l0, r0, r1, l1, up); exits.Tint = ply; Face(exits, l0, r0, r1, l1, up);
                    foreach (var (u, sgn) in new[] { (u0, -1f), (u1, 1f) }) { var a = m.P(u, va, ha); var b = m.P(u, vb, hb); var a0 = m.P(u, va, yb); var b0 = m.P(u, vb, yb); deco.Tint = plyDark; Face(deco, a0, b0, b, a, m.D3(sgn, 0)); exits.Tint = plyDark; Face(exits, a0, b0, b, a, m.D3(sgn, 0)); }
                }
            }
            ColliderMeshMall(exits, "Mall ramp Carroway pit ways out", go);
            // hazard tape on posts along both edges (breakable, nothing slows a car), not across the kickers
            foreach (float edge in new[] { MallPitV0, MallPitV1 })
            {
                float off = edge == MallPitV0 ? .6f : -.6f;
                for (float u = uW + 1f; u < uE - 1f; u += 3f)
                {
                    if (Mathf.Abs(u + 1.5f - MallPitU) < MallKickHalf + 1.6f) continue;
                    var p0 = m.P(u, edge + off, y); var p1 = m.P(u + 3f, edge + off, y);
                    MItem(br.iron, (p0 + p1) * .5f, m.D3(0, 1), 3f, 1.1f, .4f, mb => { SBox(mb, p0, m.D3(0, 1), new Vector3(.06f, 1.0f, .06f), SC(.95f, .55f, .10f)); SBox(mb, (p0 + p1) * .5f + Vector3.up * .9f, p1 - p0, new Vector3(.02f, .08f, 3f), SC(.95f, .82f, .10f)); });
                }
            }
            // the racks: under dust sheets, pushed back either side of the line (u MallPitU +- 5), not in the pit
            var rnd = new System.Random(2393); int racks = 0;
            foreach (var (u0, u1) in new[] { (r.xMin + 3f, MallPitU - 7f), (MallPitU + 7f, r.xMax - 3f) })
                for (float u = u0; u <= u1; u += 6f)
                    for (float v = r.yMin + 3f; v <= r.yMax - 3f; v += 6f)
                    {
                        if (v < MallPitV0 + 3f && v > MallPitV1 - 3f) continue;
                        var at = m.P(u, v, y); var f = m.D3(1, 0); bool sheet = rnd.Next(3) > 0;
                        MItem(br.iron, at, f, 3f, 1.7f, .8f, mb => { foreach (float o in new[] { -1.45f, 1.45f }) SBox(mb, at + f * o, f, new Vector3(.05f, 1.6f, .05f), MSteel); SBox(mb, at + Vector3.up * 1.55f, f, new Vector3(.04f, .04f, 3f), MSteel); },
                            mb => { if (sheet) SBox(mb, at + Vector3.up * .2f, f, new Vector3(.9f, 1.45f, 3.1f), dust); else for (float o = -1.2f; o < 1.25f; o += .3f) SBox(mb, at + f * o + Vector3.up * .5f, f, new Vector3(.55f, 1.0f, .08f), SC(.35f + .3f * Mathf.Abs(o), .3f, .45f)); });
                        racks++;
                    }
            AnchorLights(m, lights, r);
            if (signs != null) MallPlate(go, m.P(MallPitU, r.yMax - .5f, y + 5.2f), m.D3(0, -1), 10f, .78f, SignCarroway, signs);
            m.Log.Add($"Round 64: Carroway's lower floor: the renovation pit v {MallPitV1:0} to {MallPitV0:0} ({MallPitDepth:0.0} m deep, {MallPitWater:0.0} m of water) across u {uW:0} to {uE:0}, " +
                      $"kickers {MallKickH:0.0} m over {MallKickLen:0.0} m at u {MallPitU:0.0}, the ways out at its two ends; {racks} racks");
        }
    }
}
