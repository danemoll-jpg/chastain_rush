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
//     door with sidelights, wall lanterns. Sides and back plain. Dark asphalt driveway (MapBuilder.BuildDrive), a large
//     tree in the front yard, and behind: the real hill under pine straw with a few planted trees.
namespace ChastainRush
{
    public static partial class MapBuilder
    {
        const string HandMadeDir = "Assets/HandMade/DanHouse";
        const float DanW = 14.3f, DanD = 10.6f;

        static Mesh BuildDanAsset()
        {
            Directory.CreateDirectory(Path.Combine(ProjectRoot, HandMadeDir));
            var h = new HouseSpec { W = DanW, D = DanD, floorY = 0f, baseY = -3.0f, seed = 4711 };
            ChooseColoursDanBase(h);
            h.frame = MakeFrame(Vector3.zero, new Vector2(0f, 1f), 1f);
            GarageSpan(h);
            var mb = new MeshBuilder();
            BuildHouse(mb, h);
            // a small back deck with a rail and lattice screen, fudged from the back photos (stained red-brown)
            var f = h.frame; var deck = new Color(0.42f, 0.20f, 0.16f);
            BoxF(mb, f, 0.5f, 5.0f, -0.18f, -0.02f, -DanD / 2 - 3.2f, -DanD / 2, deck);
            for (float u = 0.5f; u <= 5.0f; u += 0.75f) BoxF(mb, f, u - 0.04f, u + 0.04f, -0.02f, 0.95f, -DanD / 2 - 3.2f, -DanD / 2 - 3.12f, deck);
            BoxF(mb, f, 0.5f, 5.0f, 0.9f, 0.98f, -DanD / 2 - 3.2f, -DanD / 2 - 3.1f, deck);
            BoxF(mb, f, 4.92f, 5.0f, -0.02f, 0.95f, -DanD / 2 - 3.2f, -DanD / 2, deck);
            BoxF(mb, f, 4.9f, 5.0f, 0.9f, 0.98f, -DanD / 2 - 3.2f, -DanD / 2, deck);
            var mesh = mb.ToMesh("DanHouse");
            string path = HandMadeDir + "/DanHouse.asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing != null) { EditorUtility.CopySerialized(mesh, existing); AssetDatabase.SaveAssets(); return existing; }
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        static void ChooseColoursDanBase(HouseSpec h) { h.style = HouseStyle.A; ApplyDan(h); h.ring = null; }

        static void BuildDanLot(HoodCtx c)
        {
            var dan = c.hood.Dan;
            if (dan == null) { c.Log.Append("Dan's house: not in the area (no local/owner_marker.json?); "); return; }
            var mesh = BuildDanAsset();
            var go = new GameObject("DanHouse");
            go.transform.SetParent(c.root, false);
            go.transform.SetPositionAndRotation(new Vector3(dan.frame.O.x, dan.floorY, dan.frame.O.z), Quaternion.LookRotation(dan.frame.F, Vector3.up));
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = c.mats.House;
            r.receiveGI = ReceiveGI.LightProbes;
            var box = go.AddComponent<BoxCollider>(); box.center = new Vector3(0f, 2.9f, 0f); box.size = new Vector3(DanW, 6.6f, DanD);
            AddBeacon(dan.footprintCentre, dan.floorY + 8.5f, c.mats, c.root);
            var root = new GameObject("DanLot").transform; root.SetParent(c.root, false);
            var mb = new MeshBuilder(); var straw = new MeshBuilder();
            var f = dan.frame;
            // front yard: the large tree (right of the drive, near the street) and a ring of mulch under it, shrubs along the front
            Vector3 tp = f.P(5.2f, 0f, DanD / 2 + 8.5f);
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
            // the backyard: the hill under pine straw, draped over the real terrain, with a few planted trees
            var yard = new List<Vector2>();
            foreach (var (u, v) in new[] { (-DanW / 2 - 2.5f, -DanD / 2 - 0.6f), (DanW / 2 + 5.5f, -DanD / 2 - 0.6f), (DanW / 2 + 6.5f, -DanD / 2 - 15f), (-DanW / 2 - 3.5f, -DanD / 2 - 17f) })
            { var p = f.P(u, 0f, v); yard.Add(new Vector2(p.x, p.z)); }
            straw.Tint = new Color(0.48f, 0.32f, 0.18f);
            DrapePoly(straw, yard, 2.2f, 0.05f, c.terrain);
            float lo = float.MaxValue, hi = float.MinValue;
            foreach (var p in yard) { float g = c.Y(p.x, p.y); lo = Mathf.Min(lo, g); hi = Mathf.Max(hi, g); }
            var trees = new[] { (TreePine, -3.8f, -DanD / 2 - 7f, 12f), (TreeYoung, 3.0f, -DanD / 2 - 9.5f, 7.5f), (TreePine, 8.5f, -DanD / 2 - 6f, 13f), (TreeOak, -1.2f, -DanD / 2 - 13.5f, 14f), (TreeYoung, 6.2f, -DanD / 2 - 13f, 8f) };
            foreach (var (sp, u, v, hgt) in trees)
            {
                var p = f.P(u, 0f, v);
                PlaceKitTree(c, sp, new Vector3(p.x, c.Y(p.x, p.z), p.z), hgt, sp == TreePine ? 2.8f : sp == TreeYoung ? 2.2f : 5.0f, root, u * 31f);
            }
            mb.ToObject("DanLotProps", c.mats.House, root, false, true);
            straw.ToObject("DanLotStraw", c.mats.Driveway, root, false, false);
            c.Log.Append($"Dan's house: hand-made mesh {mesh.triangles.Length / 3} triangles (Assets/HandMade), backyard hill {hi - lo:0.0} m of relief; ");
        }
    }
}
