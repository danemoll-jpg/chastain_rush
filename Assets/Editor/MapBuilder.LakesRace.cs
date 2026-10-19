using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

// Round 44 (Dan, 2026-10-18 18:10): the world pieces of the "Chastain Lakes" race, from Data/tracks/chastain_lakes.json
// (Tools/mapgen/make_lakes_route.py). World, not race layer: they stand in Free Roam too (the race is Data/routes/chastain_lakes.json):
//   the lake trail   the amenity lot to Bells Ferry Road (photos 2 to 5): concrete with timber edging from the lot past the bathhouse (Round 49:
//                    behind the pool), then gravel with tan brick edging to the footbridge, gravel with grass edges along the dam's crest; Round 50
//                    (B): at its real width (1.5 m, 2 m, 1.5 m: Dan, "i really don't think it needs to be widened. vehicles can also drive in the
//                    grass"), on a level bench with soft banks either side (the grades, in the planning). The footbridge, pier and gazebo are Round 32's, widened
//                    (MapBuilder.ChastainLakes.cs, sizes from this file), the bridge's caution tape and 2x4s and the gazebo's far rail breakable
//   the lake drain   a morning-glory spillway in East Chastain Lake 9 m past the gazebo's end: a concrete bell standing just out of the water,
//                    the water pouring over its lip, its shaft down to LD (Data/pipes/network.json; its roof open under the shaft) where B28
//                    runs under the lake to MH14's riser in North Lakeside Drive. LakeDrain takes a vehicle that comes into the bell down the
//                    shaft and sends it on into B28 (not a ramp: a scripted drop is allowed for it, the standing rule of 2026-10-17)
//   WJ               Round 46: the boat jump (MapBuilder.LakesBoat.cs: a dock, a pontoon party boat, its water slide the ramp; a real
//                    jump, no launch control) and its landing zone (Round 44's pile of dredging spoil is gone)
//   RK, the intake   Round 49: the reverse race's two shortcuts (MapBuilder.LakesKicker.cs, MapBuilder.LakesIntake.cs) and RK's landing zone
//   cleared lines    no trees or back-yard fences on the trail, the shortcuts' ways, the flight's last meters and the landing
namespace ChastainRush
{
    public static partial class MapBuilder
    {
        [Serializable] class LakesExtraData
        {
            public float[] trail, concrete_to, bridge, pier_root, pier_dir, drain, drain_exit, drain_tracks, drain_hint;
            public float trail_w, concrete_w, gravel_w, dam_w, bridge_half, pier_len, pier_half, gazebo_half, drain_r, shaft_r;
            public string drain_chamber;
            public string shut_reverse_of;   // Round 47 (A): the drain shut (a grate over it, its tracks hidden) in a reverse race of this route
            public LakesBoatData boat;   // Round 46: the boat jump (MapBuilder.LakesBoat.cs)
            public LakesIntakeData intake;   // Round 49 (A): the reverse race's intake (MapBuilder.LakesIntake.cs)
            public LakesKickerData kicker;   // Round 49 (B): the reverse race's kicker (MapBuilder.LakesKicker.cs)
            public LakesCarportData carport;   // Round 50 (A): its second jump, the collapsed carport
            public LakesShrubBed[] shrub_beds;   // Round 50 (A): the shrub beds across the free ways (ShrubBeds)
        }
        [Serializable] class LakesTrackJson { public LakesExtraData lakes; }
        const string LakesTrackFile = "Data/tracks/chastain_lakes.json";
        static Ridge LakesNow;               // its cleared lines, grades, pile and landing zone (the Chastain Ridge race's shape)
        static LakesExtraData LakesExtra;    // its trail and lake drain

        static LakesExtraData LoadLakesExtra()
        {
            string path = Path.Combine(ProjectRoot, LakesTrackFile);
            if (!File.Exists(path)) return null;
            return JsonUtility.FromJson<LakesTrackJson>(File.ReadAllText(path))?.lakes;
        }

        static void BuildLakesRace(MapData map, TerrainGrid terrain, Materials mats, Transform world)
        {
            if (LakesNow == null || LakesExtra == null) { Debug.Log("[Round44] no " + LakesTrackFile + ": no Chastain Lakes race pieces"); return; }
            var root = new GameObject("Chastain Lakes race").transform; root.SetParent(world, false);
            if (LakesNow.Data.landing_zones != null)
                foreach (var z in LakesNow.Data.landing_zones)
                {
                    var go = new GameObject("Landing zone " + z.id); go.transform.SetParent(root, false);
                    go.AddComponent<LandingZone>().outline = ToPoints(z.poly).ToArray();
                }
            BuildBigJumps(terrain, mats, root, LakesNow.Data.big_jumps, "Lake jump pile");   // none since Round 46 (the boat jump)
            BuildLakeBoat(terrain, mats, root, LakesExtra.boat);
            BuildLakeIntake(terrain, mats, root, LakesExtra.intake);   // Round 49
            BuildLakeKicker(terrain, mats, root, LakesExtra.kicker);
            BuildLakeCarport(terrain, mats, root, LakesExtra.carport);   // Round 50
            BuildShrubBeds(terrain, mats, root, LakesExtra.shrub_beds, map, LakesExtra.carport?.foot != null ? new[] { new Vector2(LakesExtra.carport.foot[0], LakesExtra.carport.foot[1]) } : new Vector2[0]);
            BuildLakeTrail(terrain, mats, root, LakesExtra);
            BuildLakeDrain(terrain, mats, root, LakesExtra);
            Debug.Log($"[Round44] Chastain Lakes race: the trail, the lake drain, {LakesNow.Data.big_jumps?.Length ?? 0} pile; {LakesNow.TreesRemoved} trees kept off the trail and the shortcuts' lines");
        }

        // the trail's tread: concrete with timber edging from the lot to the footbridge, gravel with tan brick edging past it; not drawn on the
        // lot (paved) or the bridge (its own deck)
        static void BuildLakeTrail(TerrainGrid terrain, Materials mats, Transform root, LakesExtraData lx)
        {
            var p = ToPoints(lx.trail); if (p.Count < 2) return;
            var bs = new Vector2(lx.bridge[0], lx.bridge[1]); var bn = new Vector2(lx.bridge[2], lx.bridge[3]);
            var lot = Rect.MinMaxRect(1316f, 1076f, 1362f, 1092f);
            int iS = Enumerable.Range(0, p.Count).OrderBy(i => (p[i] - bs).sqrMagnitude).First(), iN = Enumerable.Range(0, p.Count).OrderBy(i => (p[i] - bn).sqrMagnitude).First();
            var concrete = new Chunks { Tint = ConcreteLight }; var gravel = new Chunks { Tint = new Color(0.47f, 0.48f, 0.49f) };
            var timber = new Chunks { Tint = new Color(0.44f, 0.38f, 0.30f) }; var brick = new Chunks { Tint = new Color(0.86f, 0.75f, 0.56f) };
            float w = lx.trail_w;   // Round 50: set per piece below
            List<Vector2> Offset(List<Vector2> l, float d)
            {
                var o = new List<Vector2>();
                for (int i = 0; i < l.Count; i++) { var a = l[Mathf.Max(0, i - 1)]; var b = l[Mathf.Min(l.Count - 1, i + 1)]; var t = (b - a).normalized; o.Add(l[i] + new Vector2(-t.y, t.x) * d); }
                return o;
            }
            void Tread(List<Vector2> run, Chunks surf, Chunks edge, float edgeW)
            {
                if (run.Count < 2) return;
                if (edgeW <= 0f) { var dd = Densify(run, 1f); RibbonGridMax(surf.At(dd[dd.Count / 2]), dd, Enumerable.Repeat(w, dd.Count).ToArray(), 0.06f, terrain, 4); return; }   // Round 50: the dam's gravel, grass edges
                var d = Densify(run, 1f);
                RibbonGridMax(surf.At(d[d.Count / 2]), d, Enumerable.Repeat(w, d.Count).ToArray(), 0.06f, terrain, 4);
                foreach (float s in new[] { -1f, 1f })
                {
                    var e = Offset(d, s * (w / 2f + edgeW / 2f));
                    RibbonGridMax(edge.At(e[e.Count / 2]), e, Enumerable.Repeat(edgeW, e.Count).ToArray(), 0.09f, terrain, 1);
                }
            }
            // Round 49 (C): the concrete walk past the bathhouse ends at 'concrete_to' (photo 2); gravel with tan brick edging from there to the
            // footbridge (photo 3) and on along the dam
            int iC = lx.concrete_to != null && lx.concrete_to.Length >= 2 ? Enumerable.Range(0, iS + 1).OrderBy(i => (p[i] - new Vector2(lx.concrete_to[0], lx.concrete_to[1])).sqrMagnitude).First() : iS;
            var first = p.Take(iC + 1).Where(q => !lot.Contains(q)).ToList();
            var toBridge = p.Skip(iC).Take(iS - iC + 1).ToList();
            var after = p.Skip(iN).ToList();
            // the gravel ends a few meters short of Bells Ferry Road (its shoulder)
            var bf = after.Count > 6 ? after.Take(after.Count - 3).ToList() : after;
            w = lx.concrete_w > 0f ? lx.concrete_w : lx.trail_w; Tread(first, concrete, timber, 0.25f);
            w = lx.gravel_w > 0f ? lx.gravel_w : lx.trail_w; Tread(toBridge, gravel, brick, 0.22f);
            w = lx.dam_w > 0f ? lx.dam_w : lx.trail_w; Tread(bf, gravel, brick, lx.dam_w > 0f ? 0f : 0.22f);
            concrete.ToObjects("Lake trail concrete", mats.Sidewalk, root, false);
            gravel.ToObjects("Lake trail gravel", mats.Driveway, root, false);
            timber.ToObjects("Lake trail timber edging", mats.House, root, false);
            brick.ToObjects("Lake trail brick edging", mats.House, root, false);
            Debug.Log($"[Round44] the lake trail: {Densify(first, 1f).Count} m of concrete past the bathhouse, {Densify(toBridge, 1f).Count} m of gravel to the footbridge, {Densify(bf, 1f).Count} m of gravel along the dam; {lx.concrete_w:0.0} / {lx.gravel_w:0.0} / {lx.dam_w:0.0} m wide");
        }

        // the morning-glory spillway: the bell (a concrete ring just out of the water, its lip flaring down to the lake bed), the shaft down to LD's
        // open roof, a collar under the roof's hole; the terrain cut away inside the bell; LakeDrain (the vehicles' drop) and the hint
        static void BuildLakeDrain(TerrainGrid terrain, Materials mats, Transform root, LakesExtraData lx)
        {
            if (lx.drain == null || lx.drain.Length < 2) return;
            var net = Net; NetChamber ld = null;
            if (net != null && net.F.chambers != null) ld = net.F.chambers.FirstOrDefault(c => c.id == lx.drain_chamber);
            if (ld == null || !net.Built(ld)) { Debug.LogWarning($"[Round44] the lake drain's chamber {lx.drain_chamber} is not built: no drain"); return; }
            var c2 = new Vector2(lx.drain[0], lx.drain[1]);
            float wy = Beds != null ? WaterSurfaceAt(c2) : float.NaN; if (float.IsNaN(wy)) wy = Ground(terrain, c2.x, c2.y) + 0.5f;
            float bed = Ground(terrain, c2.x, c2.y), ceil = ld.floor + ld.height, rim = wy + 0.25f, R = lx.drain_r, r = lx.shaft_r;
            var go = new GameObject("Lake drain"); go.transform.SetParent(root, false);
            var concrete = new Color(0.66f, 0.65f, 0.61f); var wet = new Color(0.36f, 0.37f, 0.35f); var dark = new Color(0.20f, 0.21f, 0.21f);
            var mb = new MeshBuilder(); var sh = new MeshBuilder();   // the bell (look only: LakeDrain takes a vehicle before it reaches the lip), the shaft (solid)
            const int N = 40;
            Vector3 At(float rad, float a, float y) => new Vector3(c2.x + Mathf.Cos(a) * rad, y, c2.y + Mathf.Sin(a) * rad);
            for (int i = 0; i < N; i++)
            {
                float a0 = i * Mathf.PI * 2f / N, a1 = (i + 1) * Mathf.PI * 2f / N;
                var o0 = new Vector3(Mathf.Cos(a0), 0f, Mathf.Sin(a0)); var o1 = new Vector3(Mathf.Cos(a1), 0f, Mathf.Sin(a1)); var om = (o0 + o1).normalized;
                // the lip: flat on top from the shaft to the rim's outer edge, rounded down outside to the bed
                mb.Tint = concrete; Face(mb, At(r, a0, rim), At(r, a1, rim), At(R, a1, rim), At(R, a0, rim), Vector3.up);
                mb.Tint = wet; Face(mb, At(R, a0, rim), At(R, a1, rim), At(R + 0.35f, a1, rim - 0.45f), At(R + 0.35f, a0, rim - 0.45f), (om + Vector3.up).normalized);
                Face(mb, At(R + 0.35f, a0, rim - 0.45f), At(R + 0.35f, a1, rim - 0.45f), At(R + 1.4f, a1, bed - 0.3f), At(R + 1.4f, a0, bed - 0.3f), (om * 2f + Vector3.up).normalized);
                // the shaft: wet near the top, dark below; to just under LD's ceiling
                float yMid = rim - 2.5f;
                sh.Tint = wet; Face(sh, At(r, a0, rim), At(r, a1, rim), At(r, a1, yMid), At(r, a0, yMid), -om);
                sh.Tint = dark; Face(sh, At(r, a0, yMid), At(r, a1, yMid), At(r, a1, ceil - 0.05f), At(r, a0, ceil - 0.05f), -om);
                // the collar under LD's roof: covers its square hole round the shaft (a hair under the ceiling)
                mb.Tint = new Color(0.55f, 0.54f, 0.50f); Face(mb, At(r, a0, ceil - 0.03f), At(r, a1, ceil - 0.03f), At(6.2f, a1, ceil - 0.03f), At(6.2f, a0, ceil - 0.03f), Vector3.down);
                // rungs of a ladder down one side (iron, look only)
                if (i == 3) for (float y = rim - 0.6f; y > ceil + 0.4f; y -= 0.45f) Beam(mb, At(r - 0.15f, a0 + 0.02f, y), At(r - 0.15f, a1 - 0.02f, y), 0.04f, 0.04f, new Color(0.25f, 0.22f, 0.20f));
            }
            mb.ToObject("Lake drain bell", mats.House, go.transform, false, true);
            sh.ToObject("Lake drain shaft", mats.House, go.transform, true, false);
            // the terrain cut away inside the bell (the lake bed over the shaft) and under the flare's inner edge
            int cut = SetHoles(terrain, new Rect(c2.x - R, c2.y - R, 2f * R, 2f * R), q => (q - c2).magnitude < r + 0.4f);
            // the drop
            var ex = new Vector3(lx.drain_exit[0], lx.drain_exit[1], lx.drain_exit[2]);
            var d = go.AddComponent<LakeDrain>();
            d.mouth = new Vector3(c2.x, wy, c2.y); d.radius = R + 1.5f; d.shaftRadius = r; d.floorY = ld.floor; d.exit = ex;   // the pull a little wider than the lip
            d.sprayMaterial = SpraySoftMaterial != null ? SpraySoftMaterial : MakeSprayMaterial();
            // the hint at the turn-off: worn tracks across the lawn to the pier
            var clues = go.transform;
            if (!string.IsNullOrEmpty(lx.shut_reverse_of))
            {
                // Round 47 (A): in a reverse race of its route the drain is shut: a steel grate over the bell at its rim (solid, drivable), the tracks hidden
                d.shutReverseOf = lx.shut_reverse_of;
                var hide = new GameObject("Lake drain clues (hidden in a reverse race)"); hide.transform.SetParent(go.transform, false);
                hide.AddComponent<RaceOnly>().hideInReverseOf = lx.shut_reverse_of; clues = hide.transform;
                var show = new GameObject("Lake drain grate (a reverse race only)"); show.transform.SetParent(go.transform, false);
                show.AddComponent<RaceOnly>().showInReverseOf = lx.shut_reverse_of;
                var gm = new MeshBuilder(); var steel = new Color(0.30f, 0.31f, 0.32f); float gy = rim + 0.06f;
                for (float u = -R + 0.4f; u < R; u += 0.6f)   // bars both ways inside the rim, a ring round it
                {
                    float w = Mathf.Sqrt(Mathf.Max(0f, R * R - u * u));
                    Beam(gm, new Vector3(c2.x + u, gy, c2.y - w), new Vector3(c2.x + u, gy, c2.y + w), 0.06f, 0.06f, steel);
                    Beam(gm, new Vector3(c2.x - w, gy + 0.08f, c2.y + u), new Vector3(c2.x + w, gy + 0.08f, c2.y + u), 0.06f, 0.06f, steel);
                }
                var gl = gm.ToObject("Lake drain grate", mats.House, show.transform, false); gl.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.On;
                var plate = new MeshBuilder(); plate.Tint = new Color(0.16f, 0.17f, 0.18f);   // the solid plate under the bars, the bell's width: what is driven on
                for (int i = 0; i < N; i++)
                {
                    float a0 = i * Mathf.PI * 2f / N, a1 = (i + 1) * Mathf.PI * 2f / N;
                    TriFace(plate, new Vector3(c2.x, gy - 0.02f, c2.y), At(R, a1, gy - 0.02f), At(R, a0, gy - 0.02f), Vector3.up);
                }
                plate.ToObject("Lake drain grate plate", mats.House, show.transform, true, false);
            }
            if (lx.drain_tracks != null && lx.drain_tracks.Length >= 6) WornRuts(terrain, mats, clues, ToPoints(lx.drain_tracks), "Lake drain worn tracks");
            Debug.Log($"[Round44] the lake drain at ({c2.x:0.0}, {c2.y:0.0}): the water {wy:0.00} m, the bell's rim {rim:0.00} m, {R:0.0} m across its lip, the shaft {2f * r:0.0} m wide " +
                      $"down {rim - ceil:0.0} m to {ld.id}'s roof ({ceil:0.00} m), its floor {ld.floor:0.00} m; {cut} terrain cells cut; on into B28 at ({ex.x:0.0}, {ex.y:0.0}, {ex.z:0.0})");
        }

        // the lake's water surface at a point (the bed's level + its depth, as the Water step draws it)
        static float WaterSurfaceAt(Vector2 p)
        {
            for (int i = 0; i < Beds.Rings.Count; i++)
                if (Beds.Rings[i] != null && Beds.Rings[i].Count > 2 && InPolygon(p, Beds.Rings[i]) && Beds.Level.TryGetValue(i, out float lv)) return lv + PondLift;
            return float.NaN;
        }
    }
}
