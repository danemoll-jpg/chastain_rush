using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

// Round 46 (Dan, 2026-10-18 22:19: "I am thinking you come upon a boat off the dock that has a large slide on it that can be used for a
// ramp"): the Chastain Lakes race's boat jump, world (it stays in Free Roam, the slide drivable there too), from the 'boat' entry of
// Data/tracks/chastain_lakes.json (Tools/mapgen/make_lakes_route.py). Replaces Round 44's dredging-spoil pile.
//   the dock    a timber dock on piles from the bank of West Chastain Lake's north shore out to the boat's stern, 5 m wide, solid
//   the boat    a big two-deck pontoon party boat, 20 m by 7 m: three aluminum pontoons on the water, the main deck 1.0 m over it (solid,
//               an open deck on its pontoons: drive off its side and you are in the lake), white rails, a bimini over the stern, the top
//               deck either side of the slide, balloons and bunting (a party); rails, canopy and dressing are look only
//   the slide   its big water slide is the ramp (solid under its chute): from the main deck 3.5 m back from the stern, a curve up to its top over the bow, 4.5 m
//               over the water and 23 degrees there; a 4.6 m chute between low walls (solid, smooth: a crooked run glances off them and
//               loses speed). A real jump off its top (physics alone: no launch control, the standing rule of 2026-10-17)
//   the hint    a PARTY yard sign with balloons on North Lakeside Drive's verge, its arrow to the turn-off (as R3's), worn tracks down
//               the yards
namespace ChastainRush
{
    public static partial class MapBuilder
    {
        [Serializable] class LakesBoatData
        {
            public float[] lip, dir, dock_root, sign, tracks;
            public float lip_y, deck_y, water_y, angle_deg, slide_m, chute, wall, beam, length, dock_w;
        }

        static readonly Color BoatWhite = new Color(0.90f, 0.90f, 0.88f), BoatDeck = new Color(0.62f, 0.60f, 0.56f), BoatPontoon = new Color(0.74f, 0.76f, 0.78f),
                              BoatSlideBlue = new Color(0.16f, 0.48f, 0.86f), BoatSlideYellow = new Color(0.98f, 0.78f, 0.14f), BoatBimini = new Color(0.12f, 0.22f, 0.40f);

        static void BuildLakeBoat(TerrainGrid terrain, Materials mats, Transform root, LakesBoatData b)
        {
            if (b == null || b.lip == null || b.lip.Length < 2) return;
            var go = new GameObject("Boat jump").transform; go.SetParent(root, false);
            var L = new Vector2(b.lip[0], b.lip[1]); var d = new Vector2(b.dir[0], b.dir[1]).normalized; var nl = new Vector2(-d.y, d.x);
            var stern = L - d * b.length; var foot = L - d * b.slide_m; var dock = new Vector2(b.dock_root[0], b.dock_root[1]);
            float k = Mathf.Tan(b.angle_deg * Mathf.Deg2Rad), hb = b.beam * 0.5f, hc = b.chute * 0.5f, bow = 0.6f;
            Vector3 W(Vector2 o, float s, float lat, float y) { var q = o + d * s + nl * lat; return new Vector3(q.x, y, q.y); }
            float SlideY(float s) => b.deck_y + k * s * s / (2f * b.slide_m);   // s from the slide's foot

            // noFace: outline edges with no side face (0 the start, 2 the far end): where two surfaces meet flush (the bank and the dock, the
            // dock and the deck, the deck and the slide) an upright face whose top is at the surface was met by a car's nose as a wall (the
            // Street Classic: 36 m/s to 0 at the slide's start)
            ScPrism Board(string kind, Vector2 o, float len, float half, float[] profile, Color c, params int[] noFace) => new ScPrism
            {
                name = $"sc WB {kind}", kind = kind, no_face = noFace, origin = new[] { o.x, o.y }, axis = new[] { d.x, d.y }, profile = profile, colour = new[] { c.r, c.g, c.b },
                poly = new[] { o + nl * half, o - nl * half, o + d * len - nl * half, o + d * len + nl * half }.SelectMany(q => new[] { q.x, q.y }).ToArray()
            };
            // the dock: from the bank (its ground) to the stern (the main deck's height); an open board on piles
            float dl = Vector2.Distance(dock, stern), g0 = Ground(terrain, dock.x, dock.y);
            var dk = Board("deck", dock, dl, b.dock_w * 0.5f, new[] { 0f, Mathf.Max(g0, b.deck_y), dl, b.deck_y }, OldWood, 0, 2); dk.name = "sc WB dock";   // its own name: the collider meshes are saved by name
            BuildPrism(dk, terrain, mats, go);
            // the main deck: an open deck from the stern to the slide's foot, then two strips beside the slide to the bow (never under it: a
            // deck a few centimeters under the slide's surface caught the cars' bodies as they rolled onto it, 37 m/s to 0 in one step)
            float sFoot = b.length - b.slide_m;
            BuildPrism(Board("deck", stern, sFoot, hb, new[] { 0f, b.deck_y, sFoot, b.deck_y }, BoatDeck, 0, 2), terrain, mats, go);
            foreach (float side in new[] { -1f, 1f })
            {
                float a = hc + 0.35f, w = (hb - a) * 0.5f; var o = stern + d * sFoot + nl * side * (a + w);
                var strip = Board("deck", o, b.slide_m + bow, w, new[] { 0f, b.deck_y, b.slide_m + bow, b.deck_y }, BoatDeck); strip.name += side < 0f ? " right" : " left";
                BuildPrism(strip, terrain, mats, go);
            }
            // the slide: from the main deck up to its top, starting 1 m back on the deck at the deck's own height (one surface
            // across the foot, no step and no seam to catch)
            var pr = new List<float> { 0f, b.deck_y }; for (float s = 1f; s <= b.slide_m + 1f + 1e-3f; s += 0.5f) { pr.Add(s); pr.Add(SlideY(Mathf.Min(s - 1f, b.slide_m))); }
            // solid down to the deck (a thin open board let a car pressed into its curve at 3.5 g sink to the board's underside, whose normal
            // points down: 37 m/s to 0 in one step; the bikes, lighter, never did)
            BuildPrism(Board("ramp", foot - d, b.slide_m + 1f, hc, pr.ToArray(), BoatSlideBlue, 0), terrain, mats, go);

            // the chute's walls, solid and smooth: low wedges rising to 'wall' over their first 3 m (a crooked run glances off, no face to stop on)
            var walls = new MeshBuilder { Tint = BoatSlideYellow };
            foreach (float side in new[] { -1f, 1f })
            {
                float lat = side * (hc + 0.06f);
                for (float s = 0f; s < b.slide_m - 1e-3f; s += 0.5f)
                {
                    float s2 = Mathf.Min(s + 0.5f, b.slide_m), h1 = b.wall * Mathf.Clamp01(s / 3f), h2 = b.wall * Mathf.Clamp01(s2 / 3f);
                    var a0 = W(foot, s, lat, SlideY(s)); var a1 = W(foot, s2, lat, SlideY(s2));
                    var t0 = W(foot, s, lat + side * 0.12f, SlideY(s) + h1); var t1 = W(foot, s2, lat + side * 0.12f, SlideY(s2) + h2);
                    var o0 = W(foot, s, lat + side * 0.3f, SlideY(s) - 0.3f); var o1 = W(foot, s2, lat + side * 0.3f, SlideY(s2) - 0.3f);
                    Face(walls, a0, a1, t1, t0, new Vector3(-side * nl.x, 0.3f, -side * nl.y));   // inner face, leaning out a little
                    Face(walls, t0, t1, o1, o0, new Vector3(side * nl.x, 0.6f, side * nl.y));     // outer face
                }
            }
            var wg = walls.ToObject("sc WB slide walls", mats.House, go, false);
            var wm = new Mesh { name = "WB slide walls collider", indexFormat = IndexFormat.UInt32 };
            wm.SetVertices(walls.Verts); wm.SetTriangles(walls.Tris, 0); wm.RecalculateNormals(); wm.RecalculateBounds();
            wm = GenAsset(wm, $"{GeneratedDir}/Meshes/WB slide walls collider.asset");
            var wc = wg.AddComponent<MeshCollider>(); wc.sharedMesh = wm; wc.sharedMaterial = RampMaterial();

            // the look: pontoons, rails, posts, the top deck either side of the slide, the bimini, the slide's frame, the dock's piles, the party
            var look = new MeshBuilder();
            foreach (float lat in new[] { -hb + 0.55f, 0f, hb - 0.55f })
            {
                LookCyl(look, W(stern, 0.4f, lat, b.water_y + 0.05f), W(stern, b.length + bow - 0.9f, lat, b.water_y + 0.05f), 0.42f, BoatPontoon);
                look.Tint = Shade(BoatPontoon, 0.8f);   // the nose cone
                LookCyl(look, W(stern, b.length + bow - 0.9f, lat, b.water_y + 0.08f), W(stern, b.length + bow + 0.3f, lat, b.water_y + 0.32f), 0.3f, BoatPontoon);
            }
            // the deck's fascia (white) round its edge, the space under it dark
            foreach (float side in new[] { -1f, 1f })
            {
                look.Tint = BoatWhite;
                Face(look, W(stern, 0f, side * hb, b.deck_y - 0.27f), W(stern, b.length + bow, side * hb, b.deck_y - 0.27f), W(stern, b.length + bow, side * hb, b.deck_y - 0.02f), W(stern, 0f, side * hb, b.deck_y - 0.02f), new Vector3(side * nl.x, 0f, side * nl.y));
            }
            // rails along the sides (posts every 1.5 m, a top rail), round the bow beside the slide's foot of the top deck; open at the stern (the dock)
            foreach (float side in new[] { -1f, 1f })
            {
                float lat = side * (hb - 0.05f);
                for (float s = 0.3f; s <= b.length + bow - 0.2f; s += 1.5f) Beam(look, W(stern, s, lat, b.deck_y), W(stern, s, lat, b.deck_y + 0.9f), 0.05f, 0.05f, BoatWhite);
                Beam(look, W(stern, 0.3f, lat, b.deck_y + 0.9f), W(stern, b.length + bow - 0.2f, lat, b.deck_y + 0.9f), 0.06f, 0.06f, BoatWhite);
            }
            // the top deck: either side of the slide over its upper half, 2.6 m over the main deck, on white posts, with its own rail
            float topY = b.deck_y + 2.6f, tdFrom = b.length - 9f;
            foreach (float side in new[] { -1f, 1f })
            {
                float a = side * (hc + 0.45f), c = side * (hb - 0.05f);
                var f = new[] { W(stern, tdFrom, a, topY), W(stern, b.length + bow, a, topY), W(stern, b.length + bow, c, topY), W(stern, tdFrom, c, topY) };
                look.Tint = BoatDeck; Face(look, f[0], f[1], f[2], f[3], Vector3.up); Face(look, f[3], f[2], f[1], f[0], Vector3.down);
                foreach (float s in new[] { tdFrom + 0.2f, tdFrom + 4.5f, b.length + bow - 0.3f }) Beam(look, W(stern, s, c, b.deck_y), W(stern, s, c, topY), 0.12f, 0.12f, BoatWhite);
                for (float s = tdFrom; s <= b.length + bow; s += 1.5f) Beam(look, W(stern, s, c, topY), W(stern, s, c, topY + 1f), 0.05f, 0.05f, BoatWhite);
                Beam(look, W(stern, tdFrom, c, topY + 1f), W(stern, b.length + bow, c, topY + 1f), 0.06f, 0.06f, BoatWhite);
                // a ladder up the stern end of each top deck
                for (float y = b.deck_y + 0.35f; y < topY; y += 0.35f) Beam(look, W(stern, tdFrom - 0.05f, c - side * 0.2f, y), W(stern, tdFrom - 0.05f, c - side * 0.7f, y), 0.04f, 0.04f, BoatPontoon);
            }
            // the slide's frame: yellow struts from the main deck up to the chute's underside every 2 m along its upper part
            for (float s = 4f; s <= b.slide_m; s += 2f) foreach (float side in new[] { -1f, 1f })
                    Beam(look, W(foot, s, side * (hc - 0.3f), b.deck_y), W(foot, s, side * (hc - 0.3f), SlideY(s) - 0.25f), 0.12f, 0.12f, BoatSlideYellow);
            // the bimini over the stern half: a navy canopy on four arched poles, 2.5 m up
            float bimFrom = 0.8f, bimTo = tdFrom - 0.4f, bimY = b.deck_y + 2.5f;
            foreach (float s in new[] { bimFrom, bimTo }) foreach (float side in new[] { -1f, 1f }) Beam(look, W(stern, s, side * (hb - 0.15f), b.deck_y), W(stern, s, side * (hb - 0.15f), bimY), 0.05f, 0.05f, BoatPontoon);
            look.Tint = BoatBimini;
            { var c0 = W(stern, bimFrom, -hb + 0.1f, bimY); var c1 = W(stern, bimTo, -hb + 0.1f, bimY); var c2 = W(stern, bimTo, hb - 0.1f, bimY); var c3 = W(stern, bimFrom, hb - 0.1f, bimY);
              Face(look, c0, c1, c2, c3, Vector3.up); Face(look, c3, c2, c1, c0, Vector3.down); }
            // the dock's piles and its two cleats with lines to the boat
            for (float s = 0.5f; s <= dl; s += 2.5f) foreach (float side in new[] { -1f, 1f })
                {
                    var q = dock + (stern - dock).normalized * s + nl * side * (b.dock_w * 0.5f - 0.15f);
                    LookCyl(look, new Vector3(q.x, Ground(terrain, q.x, q.y) - 0.5f, q.y), new Vector3(q.x, b.deck_y + 0.35f, q.y), 0.14f, OldWood);
                }
            // the party: balloon bunches on the top deck's rail corners, bunting along the bimini's edges
            int bn = 0;
            foreach (float side in new[] { -1f, 1f }) foreach (float s in new[] { tdFrom, b.length + bow })
                {
                    var knot = W(stern, s, side * (hb - 0.05f), topY + 1f);
                    for (int i = 0; i < 5; i++) { float a = i * 1.26f + side; var c = knot + new Vector3(Mathf.Cos(a) * 0.35f, 0.8f + 0.18f * i, Mathf.Sin(a) * 0.35f); Balloon(look, c, 0.3f, BalloonColors[(i + bn) % BalloonColors.Length]); Beam(look, knot, c - Vector3.up * 0.33f, 0.012f, 0.012f, Color.white); }
                    bn += 2;
                }
            foreach (float side in new[] { -1f, 1f })
                for (int i = 0; i < 12; i++)
                {
                    float s = Mathf.Lerp(bimFrom, bimTo, (i + 0.5f) / 12f), w = (bimTo - bimFrom) / 12f * 0.4f;
                    look.Tint = BalloonColors[i % BalloonColors.Length];
                    var a = W(stern, s - w, side * (hb - 0.1f), bimY - 0.02f); var c = W(stern, s + w, side * (hb - 0.1f), bimY - 0.02f); var p = W(stern, s, side * (hb - 0.1f), bimY - 0.42f);
                    TriFace(look, a, c, p, new Vector3(side * nl.x, 0f, side * nl.y)); TriFace(look, c, a, p, new Vector3(-side * nl.x, 0f, -side * nl.y));
                }
            var lg = look.ToObject("WB boat look", mats.House, go, false); lg.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.On;

            // the hint: the PARTY sign with its balloons, worn tracks down the yards
            if (b.sign != null && b.sign.Length >= 3) BoatPartySign(terrain, mats, go, new Vector2(b.sign[0], b.sign[1]), b.sign[2]);
            if (b.tracks != null && b.tracks.Length >= 6) WornRuts(terrain, mats, go, ToPoints(b.tracks), "WB worn tracks");
            Debug.Log($"[Round46] the boat jump: dock {dl:0.0} m from ({dock.x:0.0}, {dock.y:0.0}); the boat {b.length:0} x {b.beam:0.0} m, its deck {b.deck_y:0.00} m ({b.deck_y - b.water_y:0.0} m over the water); " +
                      $"the slide {b.slide_m:0.0} m up to {SlideY(b.slide_m):0.00} m ({SlideY(b.slide_m) - b.water_y:0.0} m over the water, {b.angle_deg:0} degrees) at ({L.x:0.0}, {L.y:0.0}), its chute {b.chute:0.0} m between {b.wall:0.0} m walls");
        }

        // a round tube, look only
        static void LookCyl(MeshBuilder mb, Vector3 a, Vector3 b, float r, Color c, int seg = 10)
        {
            mb.Tint = c; var ax = (b - a).normalized; var u = Vector3.Cross(ax, Mathf.Abs(ax.y) > 0.9f ? Vector3.right : Vector3.up).normalized; var v = Vector3.Cross(ax, u);
            for (int i = 0; i < seg; i++)
            {
                float t0 = i * Mathf.PI * 2f / seg, t1 = (i + 1) * Mathf.PI * 2f / seg;
                var o0 = u * Mathf.Cos(t0) * r + v * Mathf.Sin(t0) * r; var o1 = u * Mathf.Cos(t1) * r + v * Mathf.Sin(t1) * r;
                Face(mb, a + o0, b + o0, b + o1, a + o1, (o0 + o1).normalized);
                TriFace(mb, b, b + o1, b + o0, ax); TriFace(mb, a, a + o0, a + o1, -ax);
            }
        }

        // R3's PARTY yard sign (MapBuilder.PartyHint.cs) at the boat jump's turn-off: two posts, the hand-painted face, seven balloons; breaks
        // like a mailbox (FenceChunk). 'yawDeg': the way it faces (toward the oncoming driver); its arrow points left
        static void BoatPartySign(TerrainGrid terrain, Materials mats, Transform parent, Vector2 at, float yawDeg)
        {
            var facing = new Vector2(Mathf.Sin(yawDeg * Mathf.Deg2Rad), Mathf.Cos(yawDeg * Mathf.Deg2Rad));
            var f = MakeFrame(new Vector3(at.x, 0f, at.y), facing, 1f);
            float lo = float.MaxValue, hi = float.MinValue;
            foreach (float u in new[] { -1.5f, 1.5f }) { var q = f.P(u, 0f, -0.05f); float g = Ground(terrain, q.x, q.z); lo = Mathf.Min(lo, g); hi = Mathf.Max(hi, g); }
            f.O.y = hi;
            var br = new Breakables(); var mb = br.Begin(at);
            var post = new Color(0.40f, 0.31f, 0.22f); var edge = new Color(0.93f, 0.91f, 0.86f);
            foreach (float u in new[] { -1.45f, 1.45f }) br.Piece(() => BoxF(mb, f, u - 0.06f, u + 0.06f, lo - hi - 0.3f, 2.8f, -0.1f, -0.01f, post), lo + 0.05f);
            foreach (float u in new[] { -0.8f, 0.8f }) br.Piece(() => BoxF(mb, f, u - 0.8f, u + 0.8f, 1.0f, 2.6f, -0.01f, 0.03f, edge), lo + 0.05f);
            var knot = f.P(-1.45f, 2.75f, -0.05f);
            for (int i = 0; i < 7; i++)
            {
                float a = i * 0.9f, r = 0.33f + 0.04f * Hash01(i, 991);
                var c = knot + f.Dir(Mathf.Cos(a) * (0.35f + 0.15f * (i % 3)), 0.9f + 0.17f * i, Mathf.Sin(a) * 0.3f);
                int ci = i; br.Piece(() => { Balloon(mb, c, r, BalloonColors[ci % BalloonColors.Length]); Beam(mb, knot, c - Vector3.up * r, 0.015f, 0.015f, new Color(0.9f, 0.9f, 0.9f)); }, lo + 0.05f);
            }
            var mat = PartySignMaterial(true, "PartySign");
            if (mat != null)
            {
                var plate = SignPlate(parent, f.P(0f, 1.8f, 0.035f), -f.F, 1f, mat);
                plate.name = "Party sign face WB"; plate.transform.localScale = new Vector3(3.2f, 1.6f, 1f);
                br.Face(plate);
            }
            br.End(f.P(0f, 2.3f, -0.03f), f.F, 3.6f, 4.8f, 0.9f);
            br.ToObjects("Party sign WB", mats.House, parent, 1f, BreakSounds.Kind.Wood);
            Debug.Log($"[Round46] the boat jump's PARTY sign at ({at.x:0.0}, {at.y:0.0}), facing {yawDeg:0}, its arrow left, seven balloons");
        }
    }
}
