using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// Round 62 (Dan, 2026-10-23: "I want to cut through town center mall, lets create at least a jump or two each way in the mall, and be creative with it").
// The Noonday Creek Trail race runs through Town Center (Tools/mapgen/make_trail_route.py): in Halverson's east doors, along the store's aisle, out its
// glass front onto the upper concourse, over the east escalator's well, off the main entrance's balcony and out the main doors; the reverse the other way
// round, up the east escalator. Its jumps are things a mall has, all real jumps (physics only, the standing rule of 2026-10-17), in the world (they stand
// in Free Roam too), built with the mall (quick step Mall):
//   the car raffle stage   (Round 62) at the top of the east escalator, its ramp up to a lip over the escalator's top. Round 64 took the stage and its
//                          lip away (the escalator rounds over at its top again): the race now comes down the escalator from Halverson's (flat out a
//                          car flies off its top and lands on the lower floor) and turns into the north wing at its foot; the other way it climbs it.
//                          Its "WIN A NEW CAR!" board, balloons and raffle drum stay
//   the mattress mountain  a sale display of stacked mattresses in the middle of Halverson's aisle: a hump 1.1 m high, 12 degrees each side, a flat top;
//                          off it both ways. Pillows on its top break
//   (MapBuilder.MallInside.cs: the main entrance's balcony glass breaks for the drop into the entrance hall, the tower's glass over the main doors breaks)
// Their colliders are "Mall ramp ..." meshes (VehicleSurfaceContacts: a ramp's contacts, nothing slows a car on them).
namespace ChastainRush
{
    public static partial class MapBuilder
    {
        const float MallMattressU = 110.8f, MallMattressH = 1.1f, MallMattressTop = 1.0f, MallMattressHalfW = 3.0f, MallMattressSlope = 12f;
        const float MallStageRamp = 4.31f;   // (Round 62's stage: its length; Round 64 keeps its board and balloons over that stretch)
        const float MallStoreFrontH = 6.2f;   // Halverson's upper front: its opening (MapBuilder.Mall.cs) and glass (MallInside.cs), was 4.2 m

        static void MallRaceFeatures(MallPlan m, Transform root, MeshBuilder deco, MallBreak br)
        {
            var esc = m.Ramps.FirstOrDefault(r => r.name == "east"); if (esc == null) return;
            var go = new GameObject("Mall race pieces (Round 62)").transform; go.SetParent(root, false);
            // ---- the car raffle display at the escalator's top (Round 64: its stage and lip gone, the escalator rounded over again: the race comes down it
            // from Halverson's and turns into the north wing at its foot; MapBuilder.Mall.cs): the board hanging over the escalator's top, balloons, the drum
            float uA = esc.uTop, uB = uA + MallStageRamp, v0 = esc.v0, v1 = esc.v1; var gold = SC(.86f, .70f, .22f);
            var boardMat = MallBoardMaterial("RaffleBoard");
            float bu = (uA + uB) * .5f, bvc = (v0 + v1) * .5f;
            if (boardMat != null)
            {
                MallBoardPlate(go, m.P(bu - .03f, bvc, m.L1 + 4.4f), m.D3(1, 0), 4.6f, 1.15f, boardMat); MallBoardPlate(go, m.P(bu + .03f, bvc, m.L1 + 4.4f), m.D3(-1, 0), 4.6f, 1.15f, boardMat);
                foreach (float dv in new[] { -2.0f, 2.0f }) SRail(deco, m.P(bu, bvc + dv, m.L1 + 4.97f), m.P(bu, bvc + dv, m.Roof - MallPlan.Slab), .01f, SC(.7f, .7f, .7f));
            }
            var balloon = new[] { SC(.90f, .15f, .15f), SC(.98f, .85f, .20f), SC(.15f, .35f, .90f), SC(.95f, .95f, .95f) };
            for (int k = 0; k < 10; k++)
            {
                float u = Mathf.Lerp(uA + .3f, uB - .3f, (k % 5) / 4f); float v = k < 5 ? v0 - 1.4f : v1 + 1.4f;
                var foot = m.P(u, v, m.L1 + .2f); var top = foot + Vector3.up * (2.6f + .3f * (k % 3));
                SRail(deco, foot, top, .008f, SC(.9f, .9f, .9f)); SCyl(deco, top, top + Vector3.up * .45f, .22f, balloon[k % 4], 8, balloon[k % 4]);
            }
            var drum = m.P(uB + 1.6f, v0 - 3.2f, m.L1);
            MItem(br.iron, drum, m.D3(1, 0), 1.0f, 1.3f, 1.0f, mb => { SBox(mb, drum, m.D3(1, 0), new Vector3(.9f, .75f, .6f), SC(.30f, .20f, .14f)); },
                mb => { SCyl(mb, drum + Vector3.up * 1.05f - m.D3(0, 1) * .35f, drum + Vector3.up * 1.05f + m.D3(0, 1) * .35f, .3f, SC(.85f, .80f, .70f), 10, gold); });
            m.Log.Add($"Round 64: the raffle display at the escalator's top (u {uA:0.0}), no stage");

            // ---- the mattress mountain: a hump across the store's aisle, the same both ways
            float uM = MallMattressU, run = MallMattressH / Mathf.Tan(MallMattressSlope * Mathf.Deg2Rad), hw = MallMattressHalfW, vc = (MallCS + MallCN) * .5f;
            float u0 = uM - MallMattressTop * .5f - run, u3 = uM + MallMattressTop * .5f + run;
            float MY(float u) { float d = Mathf.Abs(u - uM) - MallMattressTop * .5f; return m.L1 + (d <= 0f ? MallMattressH : MallMattressH * Mathf.Clamp01(1f - d / run)); }
            var mcol = new MeshBuilder(); var ticking = SC(.62f, .70f, .84f); var stripe = SC(.30f, .40f, .66f); var piping = SC(.16f, .22f, .44f);   // (check: pale ticking read as the floor: blue-gray quilting, darker stripes)
            const int mn = 24;
            for (int i = 0; i < mn; i++)
            {
                float ua = Mathf.Lerp(u0, u3, i / (float)mn), ub = Mathf.Lerp(u0, u3, (i + 1) / (float)mn);
                Vector3 l0 = m.P(ua, vc - hw, MY(ua)), r0 = m.P(ua, vc + hw, MY(ua)), l1 = m.P(ub, vc - hw, MY(ub)), r1 = m.P(ub, vc + hw, MY(ub));
                var up = Vector3.Cross(r0 - l0, l1 - l0); if (up.y < 0) up = -up;
                deco.Tint = i % 3 == 0 ? stripe : ticking; Face(deco, l0, r0, r1, l1, up); mcol.Tint = ticking; Face(mcol, l0, r0, r1, l1, up);
                foreach (var (v, sgn) in new[] { (vc - hw, -1f), (vc + hw, 1f) })
                {
                    var a = m.P(ua, v, MY(ua)); var b = m.P(ub, v, MY(ub)); var a0 = m.P(ua, v, m.L1); var b0 = m.P(ub, v, m.L1);
                    mcol.Tint = ticking; Face(mcol, a0, b0, b, a, m.D3(0, sgn));
                    // the side: stacked mattresses (a layer every 0.275 m, a piped edge between), each a little in from the one under it
                    for (int L = 0; L < 4; L++)
                    {
                        float y0 = m.L1 + L * .275f, y1 = y0 + .275f; float ya = Mathf.Min(MY(ua), y1), yb = Mathf.Min(MY(ub), y1);
                        if (ya <= y0 + .01f && yb <= y0 + .01f) continue;
                        var q0 = m.P(ua, v, y0); var q1 = m.P(ub, v, y0); var q2 = m.P(ub, v, Mathf.Max(y0, yb)); var q3 = m.P(ua, v, Mathf.Max(y0, ya));
                        deco.Tint = SVary(new[] { SC(.86f, .84f, .78f), SC(.55f, .64f, .80f), SC(.80f, .74f, .62f), SC(.62f, .70f, .84f) }[L], ua, L, .03f); Face(deco, q0, q1, q2, q3, m.D3(0, sgn));
                        if (ya > y1 - .03f && yb > y1 - .03f) { deco.Tint = piping; Face(deco, m.P(ua, v, y1 - .025f) + m.D3(0, sgn) * .01f, m.P(ub, v, y1 - .025f) + m.D3(0, sgn) * .01f, m.P(ub, v, y1 + .015f) + m.D3(0, sgn) * .01f, m.P(ua, v, y1 + .015f) + m.D3(0, sgn) * .01f, m.D3(0, sgn)); }
                    }
                }
            }
            ColliderMeshMall(mcol, "Mall ramp mattress mountain", go);
            // pillows along its top's edges (they break, cost nothing), the banner hanging over it
            for (int k = 0; k < 6; k++)
            {
                float u = uM + (k % 3 - 1) * .6f; float v = k < 3 ? vc - hw + .45f : vc + hw - .45f; var at = m.P(u, v, m.L1 + MallMattressH);
                MItem(br.foam, at, m.D3(1, 0), .7f, .5f, .5f, mb => SBox(mb, at, m.D3(1, 0), new Vector3(.62f, .18f, .42f), k % 2 == 0 ? SC(.96f, .96f, .96f) : SC(.70f, .80f, .95f)));
            }
            var saleMat = MallBoardMaterial("MattressSale");
            if (saleMat != null)
            {
                var c = m.P(uM, vc, m.L1 + 4.6f);
                MallBoardPlate(go, c, m.D3(1, 0), 4.4f, 1.1f, saleMat); MallBoardPlate(go, c, m.D3(-1, 0), 4.4f, 1.1f, saleMat);
                foreach (float dv in new[] { -2.0f, 2.0f }) SRail(deco, m.P(uM, vc + dv, m.L1 + 5.15f), m.P(uM, vc + dv, m.L1 + 7.0f), .01f, SC(.7f, .7f, .7f));
            }
            m.Log.Add($"Round 62: the mattress mountain in Halverson's aisle, u {u0:0.0} to {u3:0.0}, {MallMattressH:0.0} m high, {2f * hw:0.0} m wide, {MallMattressSlope:0} degrees each side");
        }

        // a board's face from Assets/HandMade/Mall/<name>.png (Tools/handmade/make_trail_signs.py): lit, one-sided, depth-tested
        static Material MallBoardMaterial(string name)
        {
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/HandMade/Mall/{name}.png");
            if (tex == null) { Debug.LogWarning($"[Round62] Assets/HandMade/Mall/{name}.png missing (Tools/handmade/make_trail_signs.py): no board"); return null; }
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name };
            mat.SetTexture("_BaseMap", tex); mat.mainTexture = tex; mat.SetColor("_BaseColor", Color.white); mat.SetFloat("_Smoothness", .2f); mat.SetFloat("_Cull", (float)CullMode.Back);
            mat.EnableKeyword("_EMISSION"); mat.SetTexture("_EmissionMap", tex); mat.SetColor("_EmissionColor", new Color(.3f, .3f, .3f)); mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            return GenAsset(mat, $"{GeneratedDir}/{name}.mat");
        }
        // the whole texture on a quad of w x h m, its front facing -facing
        static GameObject MallBoardPlate(Transform parent, Vector3 centre, Vector3 facing, float w, float h, Material mat)
        {
            var go = new GameObject("MallBoard"); go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(centre, Quaternion.LookRotation(facing, Vector3.up));
            float hw = w / 2f, hh = h / 2f; var mesh = new Mesh { name = "MallBoard" };
            mesh.vertices = new[] { new Vector3(-hw, -hh, 0), new Vector3(hw, -hh, 0), new Vector3(hw, hh, 0), new Vector3(-hw, hh, 0) };
            mesh.uv = new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f) };
            mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 }; mesh.RecalculateNormals(); mesh.RecalculateBounds();
            mesh = GenAsset(mesh, $"{GeneratedDir}/Meshes/MallBoard_{mat.name}_{Mathf.Abs(centre.x.GetHashCode() ^ (centre.z * 3.1f).GetHashCode() ^ facing.x.GetHashCode())}.asset");
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = mat; r.shadowCastingMode = ShadowCastingMode.Off;
            return go;
        }
    }
}
