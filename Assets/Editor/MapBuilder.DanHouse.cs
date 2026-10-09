using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

// Round 7 item 2: Dan's house, hand-built from three front photos (local/reference_photos/my_house, never copied here).
// Racer built Kyle's house the same way: a hand-authored, named object placed by its own transform, outside the
// generated environment, built by Editor code from simple parts (Racer: CR015Neighborhood / DiscoveryDriveway place
// "Friend across street - blue circle" and author its driveway and fence separately from the rebuilt world).
//
//   * The house MESH is a hand-made asset, Assets/HandMade/DanHouse/DanHouse.asset (committed, in a folder the map
//     builder never clears). It is written by BuildDanAsset below, which is the "authoring" and is only re-run when
//     the design changes; map generation and Build Map only INSTANTIATE it. Regenerating the map cannot touch it.
//   * Its position comes from local/owner_marker.json (the building index mapgen found from the private owner_house.json,
//     git-ignored), so no address or position is committed. The object is named "DanHouse" and keeps the orange beacon
//     so it can be found from anywhere.
//   * Front: red brick, a front gable over a two-car garage with white doors, one window in the gable, white trim, dark
//     shutters, light siding on the upper side wall, grey shingle roof, an entry gable with a fan window, a pedimented
//     door with sidelights, wall lanterns. Dark asphalt driveway (MapBuilder.BuildDrive), a large tree in the front yard.
//   * Round 9, the back, from three back photos (local/reference_photos/my_house/back_*.jpg): light grey siding; the left
//     part (seen from the yard) steps out 1.2 m; a tall siding-clad chimney between it and the rest; a triple window
//     upstairs, a glass door on to the deck, a row of three windows low on the left, the other windows as in the photos;
//     a low wooden deck with blue-grey railings across the right half. The tidy version (Dan): no gazebo frame, trampoline,
//     loose items, overgrowth or vines. Yard: the flat part at the bottom is mown grass (the terrain's own grass); the hill
//     rises from it to the neighbour's fence at the top, with the trees on the slope and pine straw under them.
namespace ChastainRush
{
    public static partial class MapBuilder
    {
        const string HandMadeDir = "Assets/HandMade/DanHouse";
        const float DanW = 14.3f, DanD = 10.6f;

        static Mesh BuildDanAsset(float deckTop)
        {
            Directory.CreateDirectory(Path.Combine(ProjectRoot, HandMadeDir));
            var h = new HouseSpec { W = DanW, D = DanD, floorY = 0f, baseY = -3.0f, seed = 4711 };
            ChooseColoursDanBase(h);
            h.frame = MakeFrame(Vector3.zero, new Vector2(0f, 1f), 1f);
            GarageSpan(h);
            var mb = new MeshBuilder();
            BuildHouse(mb, h);
            DanBack(mb, h, deckTop);
            var mesh = mb.ToMesh("DanHouse");
            string path = HandMadeDir + "/DanHouse.asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing != null) { EditorUtility.CopySerialized(mesh, existing); AssetDatabase.SaveAssets(); return existing; }
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        static void ChooseColoursDanBase(HouseSpec h) { h.style = HouseStyle.A; ApplyDan(h); h.ring = null; }

        // Round 9: the back of Dan's house, in a frame turned round to face the yard (u to the right as seen from the yard,
        // so the stepped-out part with the chimney is on the left, u < 0, and the deck on the right). deckTop: the deck's
        // height against the floor, from the ground behind the house (MapBuilder.BuildDanLot).
        static void DanBack(MeshBuilder mb, HouseSpec h, float deckTop)
        {
            var f0 = h.frame;
            var b = new Frame { O = f0.O, R = -f0.R, F = -f0.F, M = f0.M };
            float W = h.W, D = h.D, yb = h.baseY - h.floorY, hE = 2 * StoreyH - 0.05f, back = D / 2;
            float riseM = (D / 2 - 0.65f) * 0.52f;
            var siding = h.sidingC; var roof = h.roofC; var trim = h.trimC;
            // the stepped-out part on the left, 1.2 m deeper, with its own roof sloping down from under the main eave
            float w0 = -W / 2, w1 = -W / 2 + 4.6f, step = 1.2f, wallTop = hE - 0.7f;
            BoxNoBottom(mb, b, w0, w1, yb, wallTop, back, back + step, siding, siding, siding);
            mb.Tint = roof;
            Face(mb, b.P(w0 - 0.15f, hE - 0.12f, back - 0.1f), b.P(w1 + 0.15f, hE - 0.12f, back - 0.1f), b.P(w1 + 0.15f, wallTop - 0.2f, back + step + 0.4f), b.P(w0 - 0.15f, wallTop - 0.2f, back + step + 0.4f), Vector3.up);
            mb.Tint = siding;
            TriFace(mb, b.P(w0, wallTop, back), b.P(w0, hE - 0.12f, back), b.P(w0, wallTop, back + step), b.Dir(-1, 0, 0));
            TriFace(mb, b.P(w1, wallTop, back), b.P(w1, hE - 0.12f, back), b.P(w1, wallTop, back + step), b.Dir(1, 0, 0));
            // the siding-clad chimney up the back, beside it, through the eave to above the ridge
            float c0 = w1, c1 = w1 + 1.2f;
            BoxF(mb, b, c0, c1, yb, hE + riseM + 1.0f, back, back + 0.75f, Shade(siding, 0.93f));
            BoxF(mb, b, c0 - 0.06f, c1 + 0.06f, hE + riseM + 1.0f, hE + riseM + 1.12f, back - 0.06f, back + 0.81f, Shade(siding, 0.8f));
            var d = h.detail ?? mb;
            // windows on the stepped-out part: two upstairs, a row of three low on the left and one beside it
            float vw = back + step;
            WindowF(d, b, w0 + 0.85f, 4.1f, 0.95f, 1.4f, vw, h, false);
            WindowF(d, b, w1 - 0.85f, 4.1f, 0.95f, 1.4f, vw, h, false);
            for (int k = 0; k < 3; k++) WindowF(d, b, w0 + 0.75f + k * 0.92f, 1.35f, 0.82f, 1.2f, vw, h, false);
            WindowF(d, b, w1 - 0.85f, 1.35f, 0.95f, 1.25f, vw, h, false);
            // the main back wall: a tall window over a lower one beside the chimney, the triple window upstairs, the glass
            // door on to the deck, a pair of windows low on the right
            WindowF(d, b, c1 + 0.95f, 3.95f, 0.95f, 1.75f, back, h, false);
            WindowF(d, b, c1 + 0.95f, 1.35f, 0.95f, 1.25f, back, h, false);
            for (int k = 0; k < 3; k++) WindowF(d, b, 1.45f + k * 1.1f, 4.2f, 1.0f, 1.45f, back, h, false);
            FrontQuad(d, b, 1.6f, 3.35f, 0f, 2.25f, back + 0.02f, trim);
            FrontQuad(d, b, 1.72f, 2.44f, 0.05f, 2.15f, back + 0.04f, h.glassC);
            FrontQuad(d, b, 2.52f, 3.23f, 0.05f, 2.15f, back + 0.04f, h.glassC);
            WallLantern(d, b, 3.75f, 1.9f, back);
            for (int k = 0; k < 2; k++) WindowF(d, b, 4.75f + k * 1.15f, 1.4f, 1.05f, 1.2f, back, h, false);
            // the deck: low, across the right half, grey-brown boards on posts, blue-grey railings with an opening and two
            // steps down to the yard
            var boards = new Color(0.47f, 0.42f, 0.36f); var rail = new Color(0.42f, 0.50f, 0.56f);
            float d0 = 0.2f, d1 = W / 2 + 0.3f, dv0 = back, dv1 = back + 4.2f, top = deckTop;
            for (float v = dv0; v < dv1 - 0.01f; v += 0.3f) BoxF(mb, b, d0, d1, top - 0.05f, top, v + 0.01f, Mathf.Min(v + 0.29f, dv1), Shade(boards, 0.94f + 0.12f * Hash01((int)(v * 10f), 5)));
            BoxF(mb, b, d0, d1, top - 0.3f, top - 0.05f, dv1 - 0.05f, dv1, Shade(boards, 0.8f));
            foreach (float u in new[] { d0 + 0.1f, (d0 + d1) / 2f, d1 - 0.1f }) foreach (float v in new[] { dv0 + 0.3f, dv1 - 0.1f })
                    BoxF(mb, b, u - 0.08f, u + 0.08f, Mathf.Min(yb, top - 3f), top - 0.05f, v - 0.08f, v + 0.08f, Shade(boards, 0.75f));
            float gap0 = d0 + 0.9f, gap1 = d0 + 2.1f;   // the opening to the steps
            void RailRun(Vector2 a, Vector2 c)
            {
                float len = (c - a).magnitude; if (len < 0.1f) return;
                Vector3 A = b.P(a.x, top, a.y), C = b.P(c.x, top, c.y);
                Beam(mb, A + Vector3.up * 0.92f, C + Vector3.up * 0.92f, 0.12f, 0.06f, rail);
                Beam(mb, A + Vector3.up * 0.12f, C + Vector3.up * 0.12f, 0.06f, 0.06f, rail);
                int n = Mathf.Max(1, Mathf.RoundToInt(len / 0.16f));
                for (int k = 0; k <= n; k++) { var p = Vector3.Lerp(A, C, k / (float)n); Beam(mb, p, p + Vector3.up * 0.92f, 0.045f, 0.045f, rail); }
                Beam(mb, A, A + Vector3.up * 1.0f, 0.1f, 0.1f, rail); Beam(mb, C, C + Vector3.up * 1.0f, 0.1f, 0.1f, rail);
            }
            RailRun(new Vector2(d0, dv0 + 0.05f), new Vector2(d0, dv1));
            RailRun(new Vector2(d0, dv1), new Vector2(gap0, dv1));
            RailRun(new Vector2(gap1, dv1), new Vector2(d1, dv1));
            RailRun(new Vector2(d1, dv1), new Vector2(d1, dv0 + 0.05f));
            for (int k = 1; k <= 2; k++) BoxF(mb, b, gap0, gap1, top - 0.2f * k - 0.05f, top - 0.2f * k, dv1 + 0.3f * (k - 1), dv1 + 0.3f * k, boards);
        }

        static void BuildDanLot(HoodCtx c)
        {
            var dan = c.hood.Dan;
            if (dan == null) { c.Log.Append("Dan's house: not in the area (no local/owner_marker.json?); "); return; }
            // Round 9: the deck's height from the ground behind the house (just above the highest of it, between 0.25 m
            // and 3 cm under the floor); the deck is on the right half of the back as seen from the yard, the garage side (u < 0)
            float deckGround = float.MinValue;
            foreach (float uu in new[] { -DanW / 2, -DanW / 4, -0.3f }) foreach (float vv in new[] { -DanD / 2 - 0.5f, -DanD / 2 - 4.3f })
                { var q = dan.frame.P(uu, 0f, vv); deckGround = Mathf.Max(deckGround, c.Y(q.x, q.z)); }
            float deckTop = Mathf.Clamp(deckGround - dan.floorY + 0.12f, -0.25f, -0.03f);   // never above the door sill
            var mesh = BuildDanAsset(deckTop);
            var go = new GameObject("DanHouse");
            go.transform.SetParent(c.root, false);
            go.transform.SetPositionAndRotation(new Vector3(dan.frame.O.x, dan.floorY, dan.frame.O.z), Quaternion.LookRotation(dan.frame.F, Vector3.up));
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = c.mats.House;
            r.receiveGI = ReceiveGI.LightProbes;
            var box = go.AddComponent<BoxCollider>(); box.center = new Vector3(0f, 2.9f, 0f); box.size = new Vector3(DanW, 6.6f, DanD);
            // Round 9: the stepped-out part of the back (u 2.55..7.15 in the house frame, whose u runs along local -x)
            var wing = go.AddComponent<BoxCollider>(); wing.center = new Vector3(-(DanW / 2 - 2.3f), 2.4f, -DanD / 2 - 0.6f); wing.size = new Vector3(4.6f, 5.6f, 1.2f);
            AddBeacon(dan.footprintCentre, dan.floorY + 8.5f, c.mats, c.root);
            var root = new GameObject("DanLot").transform; root.SetParent(c.root, false);
            var mb = new MeshBuilder(); var straw = new MeshBuilder();
            var f = dan.frame;
            // front yard: the large tree (right of the drive, near the street) and a ring of mulch under it, shrubs along the front
            // Round 9 (BUG-007): the tree stood on the grass strip by the sidewalk: it is moved back into the yard until its
            // mulch ring clears the sidewalk (the sidewalk's back edge is 3.3 m from the road's edge)
            float tv = DanD / 2 + 8.5f;
            while (tv > DanD / 2 + 3.0f) { var q = f.P(5.2f, 0f, tv); if (c.hood.RoadClearance(new Vector2(q.x, q.z)) >= 5.8f) break; tv -= 0.25f; }
            Vector3 tp = f.P(5.2f, 0f, tv);
            float tgy = c.Y(tp.x, tp.z);
            PlaceKitTree(c, TreeMaple, new Vector3(tp.x, tgy, tp.z), 13.5f, 5.6f, root, 20f);
            var ring = new List<Vector2>();
            for (int k = 0; k < 16; k++) { float a = Mathf.PI * 2f * k / 16f; ring.Add(new Vector2(tp.x + Mathf.Cos(a) * 1.7f, tp.z + Mathf.Sin(a) * 1.7f)); }
            straw.Tint = new Color(0.26f, 0.18f, 0.12f);
            DrapePoly(straw, ring, 1.5f, 0.06f, c.terrain);
            for (int i = 0; i < 6; i++)
            {
                float u = Mathf.Lerp(dan.garageU1 + 0.5f, DanW / 2 - 0.6f, (i + 0.5f) / 6f) + (Hash01(i, 7) - 0.5f) * 0.3f;
                Vector3 p = f.P(u, 0f, DanD / 2 + 0.9f); float r2 = 0.5f + 0.25f * Hash01(i, 8);
                var col = PickOf(ShrubGreens, 4711, 300 + i);
                Blob(mb, new Vector3(p.x, c.Y(p.x, p.z) + 0.05f, p.z), new Vector3(r2, r2 * 0.85f, r2), col * 1.15f, col * 0.75f, 8, 4, true, i);
            }
            // the backyard (Round 9): the flat part at the bottom stays mown grass; the pine straw covers the hill from where the
            // ground starts to rise (0.6 m above the floor) back to the end of the yard, and the trees stand on the slope
            float vEnd = -DanD / 2 - 16.5f, vHill = vEnd + 6f;
            for (float vv = -DanD / 2 - 2f; vv > vEnd + 4f; vv -= 0.5f)
            {
                float g = float.MinValue;
                foreach (float uu in new[] { -DanW / 2, 0f, DanW / 2 }) { var q = f.P(uu, 0f, vv); g = Mathf.Max(g, c.Y(q.x, q.z)); }
                if (g > dan.floorY + 0.6f) { vHill = vv; break; }
            }
            var yard = new List<Vector2>();
            foreach (var (u, v) in new[] { (-DanW / 2 - 2.5f, vHill), (DanW / 2 + 5.5f, vHill), (DanW / 2 + 6.5f, vEnd + 1.5f), (-DanW / 2 - 3.5f, vEnd - 0.5f) })
            { var p = f.P(u, 0f, v); yard.Add(new Vector2(p.x, p.z)); }
            straw.Tint = new Color(0.48f, 0.32f, 0.18f);
            DrapePoly(straw, yard, 2.2f, 0.05f, c.terrain);
            float lo = float.MaxValue, hi = float.MinValue;
            foreach (var p in yard) { float g = c.Y(p.x, p.y); lo = Mathf.Min(lo, g); hi = Mathf.Max(hi, g); }
            var trees = new[] { (TreePine, -3.8f, 0.15f, 12f), (TreeYoung, 3.0f, 0.4f, 7.5f), (TreePine, 8.5f, 0.1f, 13f), (TreeOak, -1.2f, 0.85f, 14f), (TreeYoung, 6.2f, 0.8f, 8f) };
            foreach (var (sp, u, t, hgt) in trees)
            {
                var p = f.P(u, 0f, Mathf.Lerp(vHill - 1.5f, vEnd + 1.5f, t));
                PlaceKitTree(c, sp, new Vector3(p.x, c.Y(p.x, p.z), p.z), hgt, sp == TreePine ? 2.8f : sp == TreeYoung ? 2.2f : 5.0f, root, u * 31f);
            }
            mb.ToObject("DanLotProps", c.mats.House, root, false, true);
            straw.ToObject("DanLotStraw", c.mats.Driveway, root, false, false);
            c.Log.Append($"Dan's house: hand-made mesh {mesh.triangles.Length / 3} triangles (Assets/HandMade), backyard hill {hi - lo:0.0} m of relief, mown grass for the first {-(vHill + DanD / 2):0.0} m behind the house, deck {deckTop:+0.00;-0.00} m against the floor, front tree {tv - DanD / 2:0.0} m in front of the house; ");
        }
    }
}
