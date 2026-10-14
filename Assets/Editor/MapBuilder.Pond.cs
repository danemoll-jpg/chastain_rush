using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using Racer;
using UnityEngine;
using UnityEngine.Rendering;

// Round 7 items 7, 8 and 9: the pond (OSM water index 8, unnamed in the data; West and East Chastain Lake are 2 km away),
// its fountain, weeping trees, retaining walls, docks, culvert headwall and geese, and the pool site (pool, parking,
// playground and a small clubhouse). Where the data has no position (docks, walls, culvert, clubhouse) the choice is
// made here from the shoreline, houses and roads, and is reported as an estimate.
namespace ChastainRush
{
    public static partial class MapBuilder
    {
        static Material PondWaterMaterial, SpraySoftMaterial;

        static Material MakePondWater()
        {
            var m = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard")) { name = "PondWater" };
            m.SetColor("_BaseColor", new Color(0.12f, 0.19f, 0.15f)); m.color = new Color(0.12f, 0.19f, 0.15f);
            m.SetFloat("_Smoothness", 0.30f); m.SetFloat("_Glossiness", 0.30f);
            AssetDatabase.CreateAsset(m, GeneratedDir + "/PondWater.mat");
            return m;
        }

        static Material MakeSprayMaterial()
        {
            // a soft white dot: round, fading to the edge, drawn into a small alpha texture
            const int N = 32;
            var px = new Color[N * N];
            for (int y = 0; y < N; y++) for (int x = 0; x < N; x++)
                {
                    float fx = (x + 0.5f) / N * 2f - 1f, fy = (y + 0.5f) / N * 2f - 1f;
                    float a = Mathf.Clamp01(1f - Mathf.Sqrt(fx * fx + fy * fy)); px[y * N + x] = new Color(1f, 1f, 1f, a * a);
                }
            var tex = new Texture2D(N, N, TextureFormat.RGBA32, false) { name = "SprayDot", wrapMode = TextureWrapMode.Clamp };
            tex.SetPixels(px); tex.Apply();
            AssetDatabase.CreateAsset(tex, GeneratedDir + "/SprayDot.asset");
            var m = new Material(Shader.Find("Sprites/Default")) { name = "FountainSpray" };
            m.mainTexture = tex;
            AssetDatabase.CreateAsset(m, GeneratedDir + "/FountainSpray.mat");
            return m;
        }

        static Vector2 RingCentroid(List<Vector2> ring) { Vector2 c = Vector2.zero; foreach (var p in ring) c += p; return c / ring.Count; }

        // the shoreline sampled every ~1 m with an outward normal
        sealed class Shore { public List<Vector2> p = new List<Vector2>(); public List<Vector2> n = new List<Vector2>(); public List<float> arc = new List<float>(); }
        static Shore MakeShore(List<Vector2> ring)
        {
            var r = new List<Vector2>(ring); if (PolyArea(r) < 0f) r.Reverse();   // counter-clockwise: outward normal is to the right of travel
            r.Add(r[0]);
            var dense = Densify(r, 1.0f);
            var s = new Shore(); float a = 0f;
            for (int i = 0; i < dense.Count - 1; i++)
            {
                var d = (dense[i + 1] - dense[i]); if (d.sqrMagnitude < 1e-6f) continue;
                d.Normalize();
                s.p.Add(dense[i]); s.n.Add(new Vector2(d.y, -d.x));
                if (s.p.Count > 1) a += Vector2.Distance(s.p[s.p.Count - 2], s.p[s.p.Count - 1]);
                s.arc.Add(a);
            }
            return s;
        }

        // ------------------------------------------------------------ the pond

        // Round 16 (A, BUG-001): the culvert headwall stood on the bank with its back and sides in the open. The pipes run
        // into the ground, so the bank is raised round the wall only: level with the wall just under its coping behind it and
        // at its wing walls, then down 1 in 3 to the ground. Raise only; never in the pond, fades out 2 to 4 m from roads and
        // drives (so no road moves). Runs right after the beds are cut, before anything samples the ground, so the fence,
        // trees, walk and geese stand on the new bank. Same placement as the wall in BuildPond.
        static void BuryCulvert(MapData map, Neighborhood hood, TerrainGrid terrain)
        {
            if (hood.PondRing.Count < 3 || Beds == null || !Beds.Level.TryGetValue(hood.PondIndex, out float lidar)) return;
            if (GlenCulvertPiped()) { Debug.Log("[Round23+] the pond's culvert is GP, a network entrance (MapBuilder.Pipes.cs): the old headwall's bank is not raised"); return; }
            float level = lidar + PondLift;
            var shore = MakeShore(hood.PondRing);
            int roadShore = 0; float bestRoad = float.MaxValue;
            for (int i = 0; i < shore.p.Count; i++)
            {
                hood.Nearest(shore.p[i], out _, out float half, out _, out float d);
                if (d - half < bestRoad) { bestRoad = d - half; roadShore = i; }
            }
            var o = shore.p[roadShore]; var nOut = shore.n[roadShore];
            var f = MakeFrame(new Vector3(o.x + nOut.x * 0.8f, level - 0.45f, o.y + nOut.y * 0.8f), -nOut, 1f);
            float baseY = level - 0.45f, backTop = baseY + 1.75f, wingTop = baseY + 1.35f;   // body and wing walls (BuildPond)
            const float Slope = 1f / 3f, Plateau = 1.5f, WallHalf = 2.9f, Front = 0.55f;
            var pave = new Pavement(map);
            float beforeBack = Ground(terrain, f.P(0f, 0f, -0.9f).x, f.P(0f, 0f, -0.9f).z);
            int raised = 0; float most = 0f;
            var c3 = f.P(0f, 0f, 0f);
            EditTerrain(terrain, new Rect(c3.x - 12f, c3.z - 12f, 24f, 24f), (x, z, y) =>
            {
                var p = new Vector2(x, z);
                if (InPolygon(p, hood.PondRing)) return y;
                var d3 = new Vector3(x, 0f, z) - f.O;
                float u = Vector3.Dot(d3, f.R), v = Vector3.Dot(d3, f.F);
                if (Mathf.Abs(u) < WallHalf && v > -0.5f) return y;            // the wall itself and the open face between the wings
                // height of the bank against the wall: under the coping behind it, at the wing tops along the wings
                float h = v <= -0.5f ? backTop - 0.08f : Mathf.Lerp(backTop - 0.08f, wingTop - 0.08f, Mathf.InverseLerp(-0.5f, Front, v));
                float du = Mathf.Max(0f, Mathf.Abs(u) - WallHalf), dv = Mathf.Max(0f, -0.5f - Plateau - v) + Mathf.Max(0f, v - Front);
                float target = h - Mathf.Sqrt(du * du + dv * dv) * Slope;
                if (target <= y) return y;
                float w = Mathf.Clamp01((pave.Clearance(p, 6f) - 2f) / 2f);
                float ny = y + (target - y) * w;
                if (ny - y > 1e-3f) { raised++; most = Mathf.Max(most, ny - y); }
                return ny;
            });
            Debug.Log($"[Round16] A (BUG-001): culvert headwall at x {c3.x:0.0} z {c3.z:0.0}, {bestRoad:0.0} m from the street; the bank behind it was {backTop - beforeBack:0.00} m below the coping; {raised} terrain samples raised, by up to {most:0.00} m");
        }

        static void BuildPond(HoodCtx c)
        {
            var hood = c.hood;
            if (hood.PondRing.Count < 3) { c.Log.Append("no pond; "); return; }
            var root = new GameObject("Pond").transform; root.SetParent(c.root, false);
            PondWaterMaterial = MakePondWater(); SpraySoftMaterial = MakeSprayMaterial();
            var ring = hood.PondRing;
            float level = (Beds != null && Beds.Level.TryGetValue(hood.PondIndex, out float lv) ? lv : WaterLevel(ring, c.terrain)) + PondLift;   // Round 15: measured before the bed was cut
            // water surface
            var wmb = new MeshBuilder();
            int v0 = wmb.Count;
            foreach (var p in ring) wmb.Vertex(new Vector3(p.x, level, p.y));
            var rr = new List<Vector2>(ring);
            foreach (var tri in EarClip(rr)) wmb.Tri(v0 + tri.x, v0 + tri.z, v0 + tri.y);
            wmb.ToObject("PondWater", PondWaterMaterial, root, false, false);
            var shore = MakeShore(ring);
            var centre = RingCentroid(ring);
            var props = new MeshBuilder();
            var rng = new System.Random(8);
            float shoreLen = shore.arc[shore.arc.Count - 1];

            // nearest shore point to the street, and the street point beside it (the geese, the culvert, the road-side fence)
            int roadShore = 0; float bestRoad = float.MaxValue; Vector2 roadPt = Vector2.zero; float roadHalf = 4f;
            for (int i = 0; i < shore.p.Count; i++)
            {
                hood.Nearest(shore.p[i], out Vector2 q, out float half, out _, out float d);
                if (d - half < bestRoad) { bestRoad = d - half; roadShore = i; roadPt = q; roadHalf = half; }
            }

            // --- retaining walls (three, 9 to 14 m long, low) where the shore meets back yards, away from the street
            var wallSpots = new List<int>();
            for (int i = 0; i < shore.p.Count; i += 3)
            {
                var o = shore.p[i] + shore.n[i] * 14f;
                float rc = hood.RoadClearance(shore.p[i]);
                bool houseNear = false; foreach (var h in hood.Houses) if ((h.footprintCentre - o).sqrMagnitude < 26f * 26f) { houseNear = true; break; }
                if (houseNear && rc > 16f && Neighborhood.InRing(shore.p[i] + shore.n[i] * 2f, hood.PondRing) == false) wallSpots.Add(i);
            }
            var chosenWalls = new List<int>();
            foreach (float f in new[] { 0.18f, 0.5f, 0.8f })
            {
                if (wallSpots.Count == 0) break;
                int target = Mathf.RoundToInt(f * shore.p.Count);
                int best = wallSpots.OrderBy(s => Mathf.Abs(s - target)).First();
                if (chosenWalls.All(w => Mathf.Abs(w - best) > 40)) chosenWalls.Add(best);
            }
            foreach (int w in chosenWalls)
            {
                int len = 9 + rng.Next(6);
                for (int k = 0; k < len; k++)
                {
                    int i0 = (w + k) % shore.p.Count, i1 = (w + k + 1) % shore.p.Count;
                    var a2 = shore.p[i0] + shore.n[i0] * 0.9f; var b2 = shore.p[i1] + shore.n[i1] * 0.9f;
                    float top = level + 0.55f, bot = Mathf.Min(Ground(c.terrain, a2.x, a2.y), Ground(c.terrain, b2.x, b2.y)) - 0.35f;
                    Beam(props, new Vector3(a2.x, (top + bot) / 2, a2.y), new Vector3(b2.x, (top + bot) / 2, b2.y), 0.36f, top - bot, Shade(new Color(0.55f, 0.53f, 0.49f), 0.92f + 0.12f * Hash01(w, k)));
                }
                hood.KeepOut(shore.p[w], 6f);
            }
            // --- two small docks on the same stretches of shore (a deck on four posts reaching into the water)
            int docks = 0;
            foreach (int w in chosenWalls.Take(2))
            {
                int i = (w + 5) % shore.p.Count;
                var o = shore.p[i]; var n = shore.n[i];
                var f2 = MakeFrame(new Vector3(o.x, level + 0.32f, o.y), -n, 1f);   // front points into the water
                // the dock stands on land at its landward end and reaches 3.2 m into the pond
                BoxF(props, f2, -0.85f, 0.85f, -0.10f, 0.0f, -1.2f, 2.2f, new Color(0.45f, 0.33f, 0.22f));
                for (int pk = 0; pk < 4; pk++)
                {
                    float uu = pk % 2 == 0 ? -0.78f : 0.78f, vv = pk < 2 ? -0.9f : 2.1f;
                    BoxF(props, f2, uu - 0.06f, uu + 0.06f, -1.1f, 0.35f, vv - 0.06f, vv + 0.06f, new Color(0.33f, 0.24f, 0.16f));
                }
                hood.KeepOut(o, 3f);
                docks++;
            }
            // --- weeping trees on the bank (the signature of the pond in the photos)
            int weeping = 0;
            foreach (float f in new[] { 0.06f, 0.24f, 0.37f, 0.58f, 0.7f, 0.9f })
            {
                int i = Mathf.Clamp(Mathf.RoundToInt(f * shore.p.Count), 0, shore.p.Count - 1);
                var p = shore.p[i] + shore.n[i] * (2.4f + 1.5f * (float)rng.NextDouble());
                if (hood.RoadClearance(p) < 5f || hood.FenceOk(p) == false) continue;
                if (hood.OnPoolOrLot(p)) continue;
                float gy = Ground(c.terrain, p.x, p.y);
                float wh = 8.5f + 2f * (float)rng.NextDouble();
                Weeping(props, new Vector3(p.x, gy, p.y), wh, 4.2f + (float)rng.NextDouble(), i);
                var trunk = new GameObject("Weeping tree trunk"); trunk.transform.SetParent(c.root, false);   // Round 10: solid trees
                trunk.transform.position = new Vector3(p.x, gy + wh * 0.21f, p.y);
                var cap = trunk.AddComponent<CapsuleCollider>(); cap.direction = 1; cap.radius = 0.34f; cap.height = wh * 0.42f;
                hood.KeepOut(p, 3.0f);
                weeping++;
            }
            // --- culvert headwall where the pond drains under the street, at the shore nearest the street (Round 23 follow-ups: not when it
            // is GP, the network's second entrance: MapBuilder.Pipes.cs builds that one)
            if (!GlenCulvertPiped())
            {
                var o = shore.p[roadShore]; var nOut = shore.n[roadShore];
                var f3 = MakeFrame(new Vector3(o.x + nOut.x * 0.8f, level - 0.45f, o.y + nOut.y * 0.8f), -nOut, 1f);  // front toward the water
                var concrete = new Color(0.60f, 0.60f, 0.58f);
                BoxF(props, f3, -2.3f, 2.3f, 0f, 1.75f, -0.5f, 0.0f, concrete);
                // wing walls flaring back into the bank
                BoxF(props, f3, -2.9f, -2.3f, 0f, 1.35f, -0.5f, 0.55f, Shade(concrete, 0.93f));
                BoxF(props, f3, 2.3f, 2.9f, 0f, 1.35f, -0.5f, 0.55f, Shade(concrete, 0.93f));
                BoxF(props, f3, -2.9f, 2.9f, 1.75f, 1.95f, -0.55f, 0.05f, Shade(concrete, 1.08f));
                foreach (float cu in new[] { -1.0f, 1.0f })
                {
                    props.Tint = new Color(0.03f, 0.03f, 0.03f);
                    var cc = f3.P(cu, 0.95f, 0.02f); const int seg = 12; float rad = 0.62f;
                    for (int s2 = 0; s2 < seg; s2++)
                    {
                        float a1 = Mathf.PI * 2f * s2 / seg, a2 = Mathf.PI * 2f * (s2 + 1) / seg;
                        TriFace(props, cc, f3.P(cu + Mathf.Cos(a1) * rad, 0.95f + Mathf.Sin(a1) * rad, 0.02f), f3.P(cu + Mathf.Cos(a2) * rad, 0.95f + Mathf.Sin(a2) * rad, 0.02f), f3.F);
                    }
                }
                hood.KeepOut(o, 3.5f);
            }
            // --- pine-straw and mulch on the bank (the geese area, the lots), draped over the real slope
            var straw = new MeshBuilder();
            int patches = 0;
            foreach (float f in new[] { 0.03f, 0.2f, 0.45f, 0.66f, 0.84f })
            {
                int i = Mathf.Clamp(Mathf.RoundToInt(f * shore.p.Count), 0, shore.p.Count - 1);
                var cc = shore.p[i] + shore.n[i] * 5.5f;
                if (hood.RoadClearance(cc) < 7f || !hood.FenceOk(cc)) continue;
                var t = new Vector2(-shore.n[i].y, shore.n[i].x);
                var pr = new List<Vector2>();
                for (int k = 0; k < 14; k++) { float a = Mathf.PI * 2f * k / 14f; float jr = 1f + 0.25f * Mathf.PerlinNoise(f * 40f + k * 0.7f, 3f); pr.Add(cc + t * Mathf.Cos(a) * 7.5f * jr + shore.n[i] * Mathf.Sin(a) * 3.8f * jr); }
                if (pr.Any(q => !hood.FenceOk(q))) continue;
                if (pr.Any(InBerm)) continue;   // Round 24 Part B: none over GP's berm and face (it was draped down over the face)
                straw.Tint = new Color(0.46f, 0.31f, 0.18f);
                DrapePoly(straw, pr, 2.5f, 0.05f, c.terrain);
                patches++;
            }
            props.ToObject("PondProps", c.mats.House, root, false, true);
            straw.ToObject("PondStraw", c.mats.Driveway, root, false, false);

            // --- the fountain
            var fgo = new GameObject("Fountain"); fgo.transform.SetParent(root, false);
            fgo.transform.position = new Vector3(hood.FountainAt.x, level, hood.FountainAt.y);
            var spray = fgo.AddComponent<FountainSpray>(); spray.material = SpraySoftMaterial; spray.waterY = level;
            var head = new MeshBuilder();
            Cyl(head, new Vector3(hood.FountainAt.x, level - 0.02f, hood.FountainAt.y), 0.28f, 0.55f, 0.45f, new Color(0.10f, 0.10f, 0.11f), 10);
            head.ToObject("FountainHead", c.mats.House, root, false, false);
            hood.KeepOut(hood.FountainAt, 2f);

            // --- the geese (and a heron) that live here
            BuildGeese(c, root, level, shore, roadShore, roadPt, centre);
            c.Log.Append($"pond: {ring.Count}-point outline, {shoreLen:0} m of shore, water {level:0.0} m, {chosenWalls.Count} retaining walls, {docks} docks, {weeping} weeping trees, {patches} straw patches, culvert at the shore {bestRoad:0} m from the street; ");
        }

        static void Weeping(MeshBuilder mb, Vector3 b, float height, float radius, int seed)
        {
            var bark = new Color(0.30f, 0.23f, 0.17f); var leafA = new Color(0.50f, 0.60f, 0.24f); var leafB = new Color(0.40f, 0.52f, 0.20f);
            float trunkH = height * 0.42f;
            Cyl(mb, b + Vector3.down * 0.1f, trunkH, 0.34f, 0.2f, bark, 7);
            // a rounded crown cap and long hanging blades round it
            Blob(mb, b + Vector3.up * (trunkH + radius * 0.35f), new Vector3(radius, radius * 0.55f, radius), leafA, leafB, 10, 4, true, seed);
            int blades = 26;
            for (int i = 0; i < blades; i++)
            {
                float a = Mathf.PI * 2f * i / blades + Hash01(seed, i) * 0.4f;
                float rr = radius * (0.65f + 0.4f * Hash01(seed, i + 40));
                Vector3 top = b + new Vector3(Mathf.Cos(a) * rr, trunkH + radius * 0.45f - 0.3f * Hash01(seed, i + 80), Mathf.Sin(a) * rr);
                float drop = 2.8f + 2.2f * Hash01(seed, i + 120);
                Vector3 tang = new Vector3(-Mathf.Sin(a), 0, Mathf.Cos(a));
                Vector3 bottom = top + Vector3.down * drop + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * 0.35f;
                var col = Color.Lerp(leafA, leafB, Hash01(seed, i + 160));
                mb.Tint = col;
                int v = mb.Vertex(top - tang * 0.45f); mb.Vertex(top + tang * 0.45f); mb.Vertex(bottom);
                mb.Tri(v, v + 1, v + 2);
                int w = mb.Vertex(top - tang * 0.45f); mb.Vertex(top + tang * 0.45f); mb.Vertex(bottom);   // the back face has its own vertices (own normals)
                mb.Tri(w, w + 2, w + 1);
            }
        }

        // ------------------------------------------------------------ geese

        // Round 8: every goose_honk*.wav / .ogg / .mp3 in Assets/Audio/Geese (Dan's clips). Referenced from the scene, so they
        // go into the exe; dropping in new clips needs Build Map (or Build Executable) again.
        public const string GooseAudioDir = "Assets/Audio/Geese";
        static AudioClip[] LoadHonkClips()
        {
            var list = new List<AudioClip>();
            if (AssetDatabase.IsValidFolder(GooseAudioDir))
                foreach (var guid in AssetDatabase.FindAssets("t:AudioClip", new[] { GooseAudioDir }))
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid), file = System.IO.Path.GetFileName(path).ToLowerInvariant(), ext = System.IO.Path.GetExtension(file);
                    if (!file.StartsWith("goose_honk") || (ext != ".wav" && ext != ".ogg" && ext != ".mp3")) continue;
                    var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                    if (clip) list.Add(clip);
                }
            list.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            Debug.Log($"[Pond] goose honk clips: {list.Count} ({string.Join(", ", list.Select(x => x.name))})" + (list.Count == 0 ? " - the geese are silent until clips are added to " + GooseAudioDir : ""));
            return list.ToArray();
        }

        static void BuildGeese(HoodCtx c, Transform root, float level, Shore shore, int roadShore, Vector2 roadPt, Vector2 pondCentre)
        {
            var hood = c.hood;
            var go = new GameObject("Geese"); go.transform.SetParent(root, false);
            var gf = go.AddComponent<GooseFlock>();
            gf.material = c.mats.House;
            gf.pondOutline = hood.PondRing.ToArray();
            gf.waterY = level;
            gf.honkClips = LoadHonkClips();
            Vector3 At(Vector2 p, float? y = null) => new Vector3(p.x, y ?? Ground(c.terrain, p.x, p.y), p.y);
            // bank beside the street (the geese on the bank in the photos, and the ones that wander across the road)
            Vector2 bankA = shore.p[roadShore] + shore.n[roadShore] * 4.0f;
            // the street point next to the pond: the geese walk out on to the road here
            Vector2 roadZoneC = roadPt;
            // a bank on the far side of the pond and open water near the fountain and in the middle
            int farIdx = 0; float far = 0f;
            for (int i = 0; i < shore.p.Count; i++) { float d = (shore.p[i] - shore.p[roadShore]).sqrMagnitude; if (d > far && hood.RoadClearance(shore.p[i]) > 10f) { far = d; farIdx = i; } }
            Vector2 bankB = shore.p[farIdx] + shore.n[farIdx] * 3.5f;
            Vector2 waterF = hood.FountainAt + (pondCentre - hood.FountainAt).normalized * 9f;
            if (!Neighborhood.InRing(waterF, hood.PondRing)) waterF = pondCentre;
            gf.zones = new[]
            {
                new GooseFlock.Zone { kind = GooseFlock.ZoneKind.Bank, centre = At(bankA), radius = 5.5f },
                new GooseFlock.Zone { kind = GooseFlock.ZoneKind.Road, centre = At(roadZoneC), radius = 5.0f },
                new GooseFlock.Zone { kind = GooseFlock.ZoneKind.Water, centre = new Vector3(waterF.x, level, waterF.y), radius = 9f },
                new GooseFlock.Zone { kind = GooseFlock.ZoneKind.Bank, centre = At(bankB), radius = 5f },
                new GooseFlock.Zone { kind = GooseFlock.ZoneKind.Water, centre = new Vector3(pondCentre.x, level, pondCentre.y), radius = 14f },
            };
            gf.gooseZones = new[] { 0, 0, 0, 1, 1, 1, 2, 2, 2, 3, 3, 4, 4, 4 };
            // the heron: at the water's edge on a bank away from the street
            int hIdx = (farIdx + shore.p.Count / 5) % shore.p.Count;
            var hp = shore.p[hIdx] + shore.n[hIdx] * 0.35f;
            gf.heronPost = At(hp);
            gf.heron = true;
            hood.KeepOut(bankA, 1f);
            AddView(c, "geese_road", roadZoneC, (hood.FountainAt - roadZoneC).normalized, roadZoneC);
            AddView(c, "geese_bank", bankA, (hood.FountainAt - bankA).normalized, bankA);
            AddView(c, "fountain", hood.FountainAt, Vector2.up, hood.FountainAt);
            c.Log.Append($"geese: {gf.gooseZones.Length} + heron, bank {Vector2.Distance(bankA, roadZoneC):0} m from the street zone; ");
        }

        // ------------------------------------------------------------ pool, parking, playground, clubhouse

        // Round 15 (item 2d): the deck top with the pool's hole, the pool's walls and floor, the bed cut into the terrain under it, and
        // its ShallowWater. Returns the water surface height.
        static float BuildSunkPool(HoodCtx c, MeshBuilder mb, Frame f, List<Vector2> ring, float deckY, float hu, float hv, Color deckC, Transform root)
        {
            float surface = deckY - 0.08f, deep = surface - BedDepth, top = deckY - 0.12f;
            // the shallow end: the longest side; n points into the pool
            int si = 0; float sl = 0f;
            for (int i = 0; i < ring.Count; i++) { float l = Vector2.Distance(ring[i], ring[(i + 1) % ring.Count]); if (l > sl) { sl = l; si = i; } }
            Vector2 A = ring[si], e = (ring[(si + 1) % ring.Count] - A).normalized, n = new Vector2(-e.y, e.x);   // CCW ring: left of an edge is inside
            float L = (top - deep) / PondSlope;
            float Bed(Vector2 p) => Mathf.Lerp(top, deep, Mathf.Clamp01(Vector2.Dot(p - A, n) / L));
            // spans of a line (o + dir * t) inside the ring
            List<float> Cross(Vector2 o, Vector2 dir)
            {
                var ts = new List<float>();
                for (int i = 0; i < ring.Count; i++)
                {
                    var a = ring[i]; var b = ring[(i + 1) % ring.Count]; var ab = b - a;
                    float den = dir.x * ab.y - dir.y * ab.x; if (Mathf.Abs(den) < 1e-6f) continue;
                    var ao = a - o; float t = (ao.x * ab.y - ao.y * ab.x) / den, k = (ao.x * dir.y - ao.y * dir.x) / den;
                    if (k >= 0f && k < 1f) ts.Add(t);
                }
                ts.Sort(); return ts;
            }
            const float Step = 0.25f;
            // deck top: rows along u, the parts outside the pool
            var U = new Vector2(f.R.x, f.R.z); var V = new Vector2(f.F.x, f.F.z); var O = new Vector2(f.O.x, f.O.z);
            mb.Tint = deckC;
            for (float v = -hv; v < hv - 1e-4f; v += Step)
            {
                float v1 = Mathf.Min(v + Step, hv);
                var ts = Cross(O + V * ((v + v1) * 0.5f), U);
                var cuts = new List<float> { -hu };
                for (int i = 0; i + 1 < ts.Count; i += 2) { cuts.Add(Mathf.Clamp(ts[i], -hu, hu)); cuts.Add(Mathf.Clamp(ts[i + 1], -hu, hu)); }
                cuts.Add(hu);
                for (int i = 0; i + 1 < cuts.Count; i += 2)
                    if (cuts[i + 1] - cuts[i] > 1e-3f)
                        Face(mb, f.P(cuts[i], 0f, v), f.P(cuts[i + 1], 0f, v), f.P(cuts[i + 1], 0f, v1), f.P(cuts[i], 0f, v1), Vector3.up);
            }
            // walls (facing in) from the deck down past the deep floor, and the floor in rows along the shallow side
            var tile = new Color(0.55f, 0.82f, 0.86f);
            mb.Tint = tile;
            for (int i = 0; i < ring.Count; i++)
            {
                var a = ring[i]; var b = ring[(i + 1) % ring.Count];
                var d = (b - a).normalized; var inward = new Vector3(-d.y, 0f, d.x);
                Face(mb, new Vector3(a.x, deep - 0.05f, a.y), new Vector3(b.x, deep - 0.05f, b.y), new Vector3(b.x, deckY, b.y), new Vector3(a.x, deckY, a.y), inward);
            }
            float far = 0f; foreach (var p in ring) far = Mathf.Max(far, Vector2.Dot(p - A, n));
            for (float d0 = 0f; d0 < far - 1e-4f; d0 += Step)
            {
                float d1 = Mathf.Min(d0 + Step, far);
                var ts = Cross(A + n * ((d0 + d1) * 0.5f), e);
                for (int i = 0; i + 1 < ts.Count; i += 2)
                {
                    Vector3 P(float t, float dd) { var q = A + e * t + n * dd; return new Vector3(q.x, Bed(q) + 0.02f, q.y); }
                    Face(mb, P(ts[i], d0), P(ts[i + 1], d0), P(ts[i + 1], d1), P(ts[i], d1), Vector3.up);
                }
            }
            // the bed in the terrain (the car drives on the terrain), 0.3 m in from the walls
            int cut = 0;
            EditTerrain(c.terrain, BoundsOf(ring, 1f), (x, z, y) =>
            {
                var p = new Vector2(x, z);
                if (!InPolygon(p, ring) || DistToEdges(p, ring) < 0.3f) return y;
                float b = Bed(p); if (b >= y) return y;
                cut++; return b;
            });
            var go = new GameObject("Pool water"); go.transform.SetParent(root, false);
            var w = go.AddComponent<ShallowWater>(); w.outline = ring.ToArray(); w.surfaceY = surface;
            c.Log.Append($"pool water: surface {surface:0.00} m (8 cm under the deck), 0.5 m deep, shallow end along its {sl:0.0} m side rising 1 in 7 over {L:0.0} m, {cut} terrain samples cut; ");
            return surface;
        }

        static void BuildPoolSite(HoodCtx c)
        {
            var hood = c.hood;
            if (hood.PoolRing == null || hood.PoolDeckHalfU <= 0f) { c.Log.Append("pool: not in the map data, deferred; "); return; }
            var root = new GameObject("PoolSite").transform; root.SetParent(c.root, false);
            var mb = new MeshBuilder();
            // deck: a level slab at the median ground height of the site, walls down to the lowest ground under it
            var samples = new List<float>(); float lo = float.MaxValue;
            for (float u = -hood.PoolDeckHalfU; u <= hood.PoolDeckHalfU; u += 1.5f)
                for (float v = -hood.PoolDeckHalfV; v <= hood.PoolDeckHalfV; v += 1.5f)
                { var p = hood.PoolCentre + hood.PoolU * u + hood.PoolV * v; float g = Ground(c.terrain, p.x, p.y); samples.Add(g); lo = Mathf.Min(lo, g); }
            samples.Sort();
            float deckY = samples[samples.Count / 2] + 0.12f;
            var f = new Frame { O = new Vector3(hood.PoolCentre.x, deckY, hood.PoolCentre.y), R = new Vector3(hood.PoolU.x, 0, hood.PoolU.y), F = new Vector3(hood.PoolV.x, 0, hood.PoolV.y), M = 1f };
            var deckC = new Color(0.78f, 0.72f, 0.64f);
            var pr = new List<Vector2>(hood.PoolRing); if (pr.Count > 1 && pr[0] == pr[pr.Count - 1]) pr.RemoveAt(pr.Count - 1);
            if (PolyArea(pr) < 0f) pr.Reverse();
            // Round 15 (item 2d): the pool is water a vehicle sinks into, Racer's way (its 0.85 / 0.86 pools): 0.5 m deep, and the
            // floor at one end (the longest side, full width) rises at 1 in 7 to the ground under the deck, so any vehicle can
            // drive out. The deck gets a hole for it; the surface is 8 cm under the deck; walls and floor in the pool's colour.
            BoxNoBottom(mb, f, -hood.PoolDeckHalfU, hood.PoolDeckHalfU, lo - 0.3f - deckY, 0f, -hood.PoolDeckHalfV, hood.PoolDeckHalfV, deckC, deckC, deckC);
            float poolSurface = BuildSunkPool(c, mb, f, pr, deckY, hood.PoolDeckHalfU, hood.PoolDeckHalfV, deckC, root);
            mb.Tint = new Color(0.16f, 0.74f, 0.82f);
            int pv = mb.Count;
            foreach (var p in pr) mb.Vertex(new Vector3(p.x, poolSurface, p.y));
            foreach (var tri in EarClip(pr)) mb.Tri(pv + tri.x, pv + tri.z, pv + tri.y);
            // coping: a pale rim round the pool
            mb.Tint = new Color(0.93f, 0.92f, 0.88f);
            for (int i = 0; i < pr.Count; i++)
            {
                var a = pr[i]; var b = pr[(i + 1) % pr.Count]; var d = (b - a).normalized; var nrm = new Vector2(d.y, -d.x);
                Face(mb, new Vector3(a.x, deckY + 0.035f, a.y), new Vector3(b.x, deckY + 0.035f, b.y), new Vector3(b.x + nrm.x * 0.35f, deckY + 0.035f, b.y + nrm.y * 0.35f), new Vector3(a.x + nrm.x * 0.35f, deckY + 0.035f, a.y + nrm.y * 0.35f), Vector3.up);
            }
            // lounge chairs and umbrellas round the deck
            var rng = new System.Random(21);
            Color[] chair = { new Color(0.18f, 0.52f, 0.42f), new Color(0.78f, 0.30f, 0.32f) };
            for (int i = 0; i < 8; i++)
            {
                float u = Mathf.Lerp(-hood.PoolDeckHalfU + 1.2f, hood.PoolDeckHalfU - 1.2f, (i % 4) / 3f);
                float v = (i < 4 ? 1f : -1f) * (hood.PoolDeckHalfV - 1.3f);
                var cf = new Frame { O = f.P(u, 0f, v), R = f.R, F = f.F, M = 1f };
                BoxF(mb, cf, -0.32f, 0.32f, 0.22f, 0.32f, -0.9f, 0.9f, chair[i % 2]);
                BoxF(mb, cf, -0.28f, 0.28f, 0.0f, 0.22f, -0.7f, -0.6f, new Color(0.9f, 0.9f, 0.9f));
                BoxF(mb, cf, -0.28f, 0.28f, 0.0f, 0.22f, 0.6f, 0.7f, new Color(0.9f, 0.9f, 0.9f));
            }
            for (int i = 0; i < 3; i++)
            {
                var p = f.P(Mathf.Lerp(-hood.PoolDeckHalfU + 2.0f, hood.PoolDeckHalfU - 2.0f, i / 2f), 0f, i % 2 == 0 ? hood.PoolDeckHalfV - 2.4f : -hood.PoolDeckHalfV + 2.4f);
                Cyl(mb, p, 2.2f, 0.04f, 0.04f, new Color(0.7f, 0.7f, 0.7f), 5);
                Cyl(mb, p + Vector3.up * 2.05f, 0.5f, 1.4f, 0.0f, new Color(0.72f, 0.08f, 0.14f), 8);
            }
            // clubhouse: a small peach building with a front-gable porch on four white columns, on the side away from the pond
            var away = (hood.PoolCentre - RingCentroid(hood.PondRing)).normalized;
            Vector2 cb = hood.PoolCentre + away * (hood.PoolDeckHalfU + 6.0f);
            float cw = 6.4f, cd = 4.8f, ch = 2.8f;
            float clubY = deckY, clubLo = deckY - 0.6f;
            var front = -away;
            if (hood.ClubhouseAt.HasValue)
            {
                // Round 12 (BUG-001): on the footprint of the big building that is no longer built, its porch toward the pool,
                // its floor on the highest ground under it, its walls down to the lowest
                cb = hood.ClubhouseAt.Value; front = hood.ClubhouseFront;
                var fr = MakeFrame(new Vector3(cb.x, 0f, cb.y), front, 1f); float hi = float.MinValue; clubLo = float.MaxValue;
                foreach (float u in new[] { -cw / 2, 0f, cw / 2 }) foreach (float v in new[] { -cd / 2, 0f, cd / 2 + 1.9f }) { var q = fr.P(u, 0f, v); float g = Ground(c.terrain, q.x, q.z); hi = Mathf.Max(hi, g); clubLo = Mathf.Min(clubLo, g); }
                clubY = hi + 0.1f; clubLo -= 0.3f;
            }
            var cf2 = MakeFrame(new Vector3(cb.x, clubY, cb.y), front, 1f);
            Color peach = new Color(0.86f, 0.70f, 0.54f), roof = new Color(0.62f, 0.45f, 0.38f);
            var hs = new HouseSpec { trimC = TrimWhite, shutterC = new Color(0.3f, 0.2f, 0.15f), doorC = new Color(0.12f, 0.10f, 0.08f), glassC = GlassDark };
            BoxNoBottom(mb, cf2, -cw / 2, cw / 2, clubLo - clubY, ch, -cd / 2, cd / 2, peach, peach, peach);
            Gable(mb, cf2, -cw / 2, cw / 2, -cd / 2, cd / 2, true, ch, 1.6f, 0.5f, 0.4f, roof, peach, true, true, TrimWhite);
            // porch: a gable over four columns on the pool side
            Gable(mb, cf2, cd / 2 - 0.5f, cd / 2 + 2.0f, -1.9f, 1.9f, false, ch, 1.3f, 0.2f, 0.3f, roof, peach, false, true, TrimWhite);
            for (int i = 0; i < 4; i++) { float u = Mathf.Lerp(-1.8f, 1.8f, i / 3f); BoxF(mb, cf2, u - 0.1f, u + 0.1f, 0f, ch, cd / 2 + 1.7f, cd / 2 + 1.9f, TrimWhite); }
            FrontQuad(mb, cf2, -0.55f, 0.55f, 0f, 2.1f, cd / 2 + 0.02f, hs.doorC);
            WindowF(mb, cf2, -2.1f, 1.4f, 0.9f, 1.0f, cd / 2, hs, false, false);
            WindowF(mb, cf2, 2.1f, 1.4f, 0.9f, 1.0f, cd / 2, hs, false, false);
            hood.KeepOut(cb, 5f);
            // collider for the clubhouse only (the deck stays free to drive over: it is a pool deck behind an iron fence)
            var col = new GameObject("Clubhouse collider"); col.transform.SetParent(root, false);
            col.transform.SetPositionAndRotation(cf2.P(0f, ch / 2 - 0.3f, 0f), Quaternion.LookRotation(cf2.F, Vector3.up));
            col.AddComponent<BoxCollider>().size = new Vector3(cw, ch + 0.6f, cd);
            mb.ToObject("PoolDeckAndClubhouse", c.mats.House, root, false, true);

            // parking lot: asphalt on the OSM outline; playground: wood chips with a swing set
            var pk = new MeshBuilder();
            if (hood.ParkingRing != null)
            {
                pk.Tint = new Color(0.20f, 0.20f, 0.21f);
                DrapePoly(pk, hood.ParkingRing.Take(hood.ParkingRing.Count - 1).ToList(), 3f, 0.05f, c.terrain);
                // a few stall lines
                pk.Tint = new Color(0.9f, 0.9f, 0.88f);
            }
            if (hood.PlaygroundRing != null)
            {
                pk.Tint = new Color(0.45f, 0.34f, 0.22f);
                var rg = hood.PlaygroundRing.Take(hood.PlaygroundRing.Count - 1).ToList();
                DrapePoly(pk, rg, 3f, 0.05f, c.terrain);
                BuildPlayground(pk, rg, c.terrain);   // Round 9 (BUG-001): swings, a slide and a climbing frame
            }
            pk.ToObject("ParkingAndPlayground", c.mats.Driveway, root, false, false);
            BuildPoolRoad(c);   // Round 11 (BUG-010)
            c.Log.Append($"pool site: OSM pool {hood.PoolRing.Count - 1} points, deck {deckY:0.0} m (ground {lo:0.0}..{samples[samples.Count - 1]:0.0}), clubhouse {(hood.ClubhouseAt.HasValue ? $"on building {hood.ClubhouseBuilding}'s footprint (BUG-001: that building not built)" : "placed by estimate (no footprint in the data)")}, parking {(hood.ParkingRing != null)}, playground {(hood.PlaygroundRing != null)}; ");
        }
    }
}
