using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Racer;

// Round 77 (B, Dan's BUG-004 x 1608.76 z -6601.44: "lets make the hospital resemble the hospital"; BUG-005 x 1665.13 z -6716.61 Church Street NW:
// "pretty sure that maybe this is the parking deck. So not businesses"; his photos 2026-10-27 21:55 in local/reference_photos/kennestone/, reference only).
// Kennestone hospital as itself, from Data/tracks/kennestone.json (python Tools/mapgen/make_kennestone.py; OSM's building parts: mapgen keeps buildings
// only, so the campus was one tan box a story high):
//   the towers at their OSM levels on a one-story podium: red brick with a white top over two floors and punched windows (seven floors and up), tan
//   brick with darker piers (three to six), cream (one or two); OSM's building colour where it gives one;
//   the main entrance on the podium's face toward the drop-off loop off Church Street: a tall white portico, KENNESTONE HOSPITAL across it, a glass
//   canopy on white columns over a brick drop-off (photo 03); a blue HOSPITAL sign at the loop's way in;
//   the emergency department (photos 04 / 05): tan with darker brick piers, a white canopy with angled struts over the ambulance bay (OSM's roof there),
//   EMERGENCY over it, ambulances parked, a terraced wall with shrubs between the bay and Church Street below; its roof's helipad stays (Round 63,
//   painted red and white as in photo 05: MapBuilder.Helipads.cs);
//   the skybridges (OSM: a building a floor up over a road): BUG-005's building is the one over Church Street (photos 01 / 02), not a deck: tan stair
//   towers each side, the glass span between with a white X truss, a round emblem, a white arch under it; solid, driven under;
//   people at the entrance and the ER.
// And every parking deck on the map (OSM building=parking, mapgen's type parking; they were dressed as stores): open levels on columns with low walls,
// a ramp lane along each long side (the even levels' ramps on one side, the odd on the other; full-width landings at alternate ends), cars parked, open
// at the street; driven to the roof, whose far end wall is open in its middle third to jump from (a real jump: the game's ramp rule). The ground under each is levelled (planning).
namespace ChastainRush
{
    public static partial class MapBuilder
    {
        [Serializable] class KsPart { public float[] outline; public int levels = 1, min_level; public string colour, name; public long osm; }
        [Serializable] class KsRaised { public int index = -1, levels = 3, min_level = 1; }
        [Serializable] class KsPt { public float x, z; }
        [Serializable] class KsEr { public int index = -1, canopy = -1; public KsPt[] entrances; }
        [Serializable] class KsPortico { public float[] loop; public int on = -1; }
        [Serializable] class KsDeck { public int index, levels; public float[] centre, u; public float hl, hw, fill; }
        [Serializable] class KsData { public int[] owned; public KsPart[] parts; public int podium = -1; public KsRaised[] raised; public KsEr er; public KsPortico portico; public KsDeck[] decks; }
        static KsData KennestoneNow; static readonly HashSet<int> ksOwned = new HashSet<int>(); static readonly Dictionary<int, float> deckBase = new Dictionary<int, float>();
        static bool KennestoneOwns(int i) => ksOwned.Contains(i);
        const float KsFloor = 4.2f, DeckFloor = 4.0f, DeckLane = 7f, DeckLanding = 12f;
        const int SignKennestone = 64, SignEmergency = 65, SignHospital = 66, SignParking = 67;
        static readonly Color KsBrick = new Color(.58f, .30f, .23f), KsTan = new Color(.80f, .66f, .48f), KsTanDark = new Color(.62f, .44f, .30f), KsCream = new Color(.90f, .88f, .82f),
            KsWhite = new Color(.95f, .95f, .93f), KsGlass = new Color(.20f, .32f, .36f), KsRoof = new Color(.62f, .62f, .60f), DeckConcrete = new Color(.66f, .65f, .62f);

        static void PlanKennestone(MapData map, TerrainGrid t)
        {
            KennestoneNow = null; ksOwned.Clear(); deckBase.Clear();
            var path = Path.Combine(ProjectRoot, "Data/tracks/kennestone.json"); if (!File.Exists(path)) return;
            var d = JsonUtility.FromJson<KsData>(File.ReadAllText(path)); if (d == null) return;
            KennestoneNow = d; bool Ok(int i) => i >= 0 && i < map.buildings.Length;
            if (d.owned != null) foreach (int i in d.owned) if (Ok(i)) ksOwned.Add(i);
            // the decks' ground levelled at its middle's height (6 m of easing round it)
            int nd = 0;
            if (d.decks != null)
                foreach (var dk in d.decks)
                {
                    if (!Ok(dk.index) || MariettaOwns(dk.index)) continue; ksOwned.Add(dk.index); nd++;
                    var c = new Vector2(dk.centre[0], dk.centre[1]); var U = new Vector2(dk.u[0], dk.u[1]); var V = new Vector2(-U.y, U.x);
                    var hs = new List<float>(); for (int a = -2; a <= 2; a++) for (int b = -2; b <= 2; b++) hs.Add(t.Height(c.x + U.x * dk.hl * a / 2.5f + V.x * dk.hw * b / 2.5f, c.y + U.y * dk.hl * a / 2.5f + V.y * dk.hw * b / 2.5f));
                    hs.Sort(); float y0 = hs[hs.Count / 2]; deckBase[dk.index] = y0;
                    float R = Mathf.Sqrt(dk.hl * dk.hl + dk.hw * dk.hw) + 8f;
                    EditTerrain(t, new Rect(c.x - R, c.y - R, 2f * R, 2f * R), (x, z, y) =>
                    {
                        var q = new Vector2(x, z) - c; float eu = Mathf.Max(0f, Mathf.Abs(Vector2.Dot(q, U)) - dk.hl - 1f), ev = Mathf.Max(0f, Mathf.Abs(Vector2.Dot(q, V)) - dk.hw - 1f);
                        float e = Mathf.Sqrt(eu * eu + ev * ev); if (e >= 6f) return y; return y + Smooth01(1f - e / 6f) * (y0 - y);
                    });
                }
            // people: a few by the main entrance and the emergency department's way in
            if (d.portico?.loop != null) { var lp = new Vector2(d.portico.loop[0], d.portico.loop[1]); KsPeople(t, lp + new Vector2(-6f, 0f), AmbientLife.Talking, 2); KsPeople(t, lp + new Vector2(-4f, 6f), AmbientLife.Walkers, 3); }
            if (d.er?.entrances != null) foreach (var e in d.er.entrances.Take(2)) KsPeople(t, new Vector2(e.x - 5f, e.z), AmbientLife.Talking, 2);
            Debug.Log($"[Round77] B: Kennestone planned: {ksOwned.Count - nd} campus buildings built by its step, {d.parts?.Length ?? 0} tower parts; {nd} parking decks (their ground levelled)");
        }
        static void KsPeople(TerrainGrid t, Vector2 p, byte action, int count)
        {
            var w3 = new Vector3(1f, 0f, 0f) * 6f;
            MariettaPeople.Add(new AmbientLife.Group { at = new Vector3(p.x, Ground(t, p.x, p.y) + .02f, p.y), yaw = 90f, kind = AmbientLife.Kind.Shop, count = (byte)count, action = action, walk = w3 });
        }

        static void BuildKennestone(MapData map, TerrainGrid t, Materials mats, Transform world)
        {
            var d = KennestoneNow; if (d == null) return;
            var root = new GameObject("Kennestone").transform; root.SetParent(world, false);
            var bodies = new MeshBuilder(); var glassy = new MeshBuilder(); var props = new MeshBuilder();
            var signs = new SignBatch(); var signMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(GeneratedDir + "/BusinessSigns.mat") ?? BusinessSignMaterial();
            var log = new System.Text.StringBuilder();
            void Solid(Vector3 centre, Vector3 size, Vector2 along, string name)
            {
                var go = new GameObject(name); go.transform.SetParent(root, false);
                go.transform.SetPositionAndRotation(centre, Quaternion.LookRotation(new Vector3(along.x, 0f, along.y), Vector3.up)); go.AddComponent<BoxCollider>().size = size;
            }
            void SignOn(Frame f, float u, float y, float w, int row, float proud)
            {
                float h = w / 8f; BoxF(bodies, f, u - w / 2f - .15f, u + w / 2f + .15f, y - h / 2f - .15f, y + h / 2f + .15f, Mathf.Max(.02f, proud - .18f), proud, new Color(.85f, .85f, .83f));
                signs.Add(f.P(u, y, proud + .012f), new Vector2(f.F.x, f.F.z), w, h, row);
            }
            var roads = new List<(Vector2 a, Vector2 b, float half)>();
            var c0 = new Vector2(1580f, -6690f);
            foreach (var r in map.roads) { if (r.tunnel || r.points == null) continue; var p = ToPoints(r.points); for (int i = 0; i + 1 < p.Count; i++) if (SegDist(c0, p[i], p[i + 1]) < 400f) roads.Add((p[i], p[i + 1], r.width * .5f)); }
            float RoadGap(Vector2 q) { float g = float.MaxValue; foreach (var s in roads) g = Mathf.Min(g, SegDist(q, s.a, s.b) - s.half); return g; }

            // ---------------- a block: walls round a ring from bottom to top, a flat roof with a parapet; windows per floor on every face over 4 m
            void Block(List<Vector2> ring, float bottom, float top, float groundLo, Color wall, int style, bool collide)
            {
                if (PolyArea(ring) < 0f) ring.Reverse();
                Extrude(bodies, ring, Mathf.Min(bottom, groundLo - .5f), top, wall, KsRoof, true);
                for (int e = 0; e < ring.Count; e++) Parapet(bodies, ring[e], ring[(e + 1) % ring.Count], ring, top, Shade(wall, .9f));
                for (int e = 0; e < ring.Count; e++)
                {
                    Vector2 a = ring[e], b = ring[(e + 1) % ring.Count]; float len = (b - a).magnitude; if (len < 4f) continue;
                    var dd = (b - a) / len; var n = new Vector2(dd.y, -dd.x); var m = (a + b) * .5f; if (InPolygon(m + n * .3f, ring)) n = -n;
                    var f = MakeFrame(new Vector3(m.x, bottom, m.y), n, 1f); float half = len / 2f - .8f; int floors = Mathf.Max(1, Mathf.RoundToInt((top - bottom) / KsFloor));
                    for (int k = 0; k < floors; k++)
                    {
                        float y0 = k * KsFloor; if (bottom + y0 < groundLo - .2f && k == 0) y0 = groundLo - bottom;
                        bool upper = style == 2 && k >= floors - 2;
                        if (style == 0 || upper) { FrontQuad(bodies, f, -half, half, y0 + 1.1f, y0 + 3.1f, .04f, upper ? Shade(KsGlass, 1.3f) : KsGlass); continue; }   // a band of glass
                        // punched windows with pale surrounds every 3 m; on tan, a darker brick pier every 9 m
                        for (float u = -half + 1.2f; u <= half - 1.0f; u += 3f) { FrontQuad(bodies, f, u - .8f, u + .8f, y0 + .95f, y0 + 3.15f, .03f, KsWhite); FrontQuad(bodies, f, u - .65f, u + .65f, y0 + 1.1f, y0 + 3.0f, .05f, KsGlass); }
                    }
                    if (style == 1) for (float u = -half; u <= half; u += 9f) FrontQuad(bodies, f, u - .5f, u + .5f, 0f, top - bottom, .06f, KsTanDark);
                    if (style == 2) FrontQuad(bodies, f, -half - .8f, half + .8f, (floors - 2) * KsFloor - .3f, (floors - 2) * KsFloor, .05f, KsWhite);
                }
                if (collide) { /* the walls are solid through the bodies' collider (Extrude into 'bodies', collider on) */ }
            }

            // ---------------- the podium and the towers
            List<Vector2> podium = d.podium >= 0 && d.podium < map.buildings.Length ? ToPoints(map.buildings[d.podium].footprint) : null; float podTop = 0f;
            if (podium != null)
            {
                if (PolyArea(podium) < 0f) podium.Reverse();
                float lo = podium.Min(p => Ground(t, p.x, p.y)), hi = podium.Max(p => Ground(t, p.x, p.y)), avg = podium.Average(p => Ground(t, p.x, p.y));
                podTop = Mathf.Max(avg + KsFloor + .6f, hi + 3.2f);
                Block(podium, lo - .5f, podTop, lo, KsCream, 0, true);
                log.Append($"podium {podTop - avg:0.0} m over its ground; ");
            }
            int nTowers = 0; float tallest = 0f;
            foreach (var part in d.parts ?? new KsPart[0])
            {
                var ring = ToPoints(part.outline); if (ring.Count < 3) continue;
                var cen = ring.Aggregate(Vector2.zero, (s, q) => s + q) / ring.Count;
                float lo = ring.Min(p => Ground(t, p.x, p.y)), avg = ring.Average(p => Ground(t, p.x, p.y));
                bool onPodium = podium != null && InPolygon(cen, podium) && part.min_level == 0;
                float bottom = onPodium ? podTop - .05f : part.min_level > 0 ? avg + part.min_level * KsFloor : lo - .5f;
                float top = Mathf.Max(bottom + 2f, (onPodium ? avg : avg) + part.levels * KsFloor + .6f);
                if (onPodium && part.levels <= 1) continue;   // a one-level part on the podium is the podium
                int style = part.levels >= 7 ? 2 : part.levels >= 3 ? 1 : 0;
                var wall = style == 2 ? KsBrick : style == 1 ? KsTan : KsCream;
                if (!string.IsNullOrEmpty(part.colour) && ColorUtility.TryParseHtmlString(part.colour, out var oc)) wall = Color.Lerp(oc, KsTan, .4f);
                Block(ring, bottom, top, lo, wall, style, true); nTowers++; tallest = Mathf.Max(tallest, top - avg);
                if (part.min_level > 0) for (int k = 0; k < ring.Count; k += Mathf.Max(1, ring.Count / 4)) Post(bodies, new Vector3(ring[k].x, Ground(t, ring[k].x, ring[k].y) - .3f, ring[k].y), bottom - Ground(t, ring[k].x, ring[k].y) + .3f, .6f, KsTanDark);
            }
            log.Append($"{nTowers} tower blocks (the tallest {tallest:0} m); ");

            // ---------------- the other campus buildings (clinics, offices, services): tan with window bands at their own height
            var special = new HashSet<int>(); if (d.podium >= 0) special.Add(d.podium); if (d.er != null) { special.Add(d.er.index); special.Add(d.er.canopy); }
            if (d.raised != null) foreach (var rr in d.raised) special.Add(rr.index);
            foreach (int i in d.owned ?? new int[0])
            {
                if (special.Contains(i) || i < 0 || i >= map.buildings.Length) continue; var b = map.buildings[i]; var ring = ToPoints(b.footprint); if (ring.Count < 3) continue;
                float lo = ring.Min(p => Ground(t, p.x, p.y)), avg = ring.Average(p => Ground(t, p.x, p.y));
                if (b.type == "roof")
                {
                    // a canopy on posts (a covered drop-off or walk): a white slab 4.5 m up, four posts
                    if (PolyArea(ring) < 0f) ring.Reverse(); float y = avg + 4.5f;
                    Extrude(props, ring, y, y + .5f, KsWhite, KsWhite, true);
                    if (BestRectangle(ring, out var rc, out var rU, out var rV, out float rhl, out float rhw))
                        foreach (var (su, sv) in new[] { (-1, -1), (1, -1), (-1, 1), (1, 1) }) { var q = rc + rU * su * (rhl - .5f) + rV * sv * (rhw - .5f); Post(props, new Vector3(q.x, Ground(t, q.x, q.y) - .2f, q.y), y - Ground(t, q.x, q.y) + .2f, .35f, KsWhite); }
                    continue;
                }
                float H = Mathf.Max(b.height, 2f * KsFloor);
                Block(ring, lo - .5f, avg + H, lo, KsTan, H >= 3f * KsFloor ? 1 : 0, true);
            }

            // ---------------- the main entrance (photo 03): on the podium's face toward the drop-off loop
            if (podium != null && d.portico?.loop != null)
            {
                var lp = new Vector2(d.portico.loop[0], d.portico.loop[1]);
                Vector2 q = podium[0], n = Vector2.up; float bd = float.MaxValue;
                for (int e = 0; e < podium.Count; e++)
                {
                    Vector2 a = podium[e], b = podium[(e + 1) % podium.Count]; if ((b - a).magnitude < 12f) continue; var cp = ClosestOnSeg(lp, a, b); float dd = (cp - lp).magnitude;
                    if (dd < bd) { bd = dd; q = cp; }
                }
                n = (lp - q).normalized;   // (build 1 took the nearest edge's own normal: on the podium's ragged outline it faced north, away from the loop)
                float gy = Ground(t, q.x, q.y) + .05f; var f = MakeFrame(new Vector3(q.x, gy, q.y), n, 1f); var fd = new Vector2(f.F.x, f.F.z);
                // the white frame: two piers, the band across with the name, a row of lit windows above
                BoxF(bodies, f, -6.2f, -4.6f, 0f, 13f, -.2f, 2.2f, KsWhite); BoxF(bodies, f, 4.6f, 6.2f, 0f, 13f, -.2f, 2.2f, KsWhite);
                BoxF(bodies, f, -4.6f, 4.6f, 6.4f, 13f, -.2f, 2.0f, KsWhite);
                for (float u = -3.6f; u <= 3.7f; u += 1.8f) FrontQuad(bodies, f, u - .55f, u + .55f, 9.4f, 12.2f, 2.05f, KsGlass);
                SignOn(f, 0f, 7.6f, 8.6f, SignKennestone, 2.1f);
                FrontQuad(bodies, f, -4.4f, 4.4f, 0f, 6.3f, .05f, KsGlass);   // the doors' glass behind
                // the glass canopy over the drop-off on four white columns
                glassy.Tint = new Color(.70f, .80f, .84f); BoxF(glassy, f, -6.5f, 6.5f, 4.4f, 4.6f, 2.0f, 10.5f, new Color(.70f, .80f, .84f));
                foreach (float u in new[] { -4.8f, -1.6f, 1.6f, 4.8f })
                {
                    var cpos = f.P(u, 0f, 10f); Cyl(bodies, new Vector3(cpos.x, gy - .1f, cpos.z), 4.6f, .32f, .3f, KsWhite, 12);
                    Solid(new Vector3(cpos.x, gy + 2.3f, cpos.z), new Vector3(.6f, 4.6f, .6f), fd, "Kennestone portico column");
                }
                Solid(f.P(-5.4f, 6.5f, 1f), new Vector3(1.6f, 13f, 2.4f), fd, "Kennestone portico pier"); Solid(f.P(5.4f, 6.5f, 1f), new Vector3(1.6f, 13f, 2.4f), fd, "Kennestone portico pier");
                // the brick drop-off under it
                var brick = new List<Vector2> { q + Rot(fd, -7f, 2f), q + Rot(fd, 7f, 2f), q + Rot(fd, 7f, 16f), q + Rot(fd, -7f, 16f) };
                props.Tint = new Color(.58f, .30f, .24f); DrapePoly(props, brick, 1.5f, .06f, t);
                // a blue HOSPITAL sign at the loop's way in from the street
                var sp = lp + fd * 18f + new Vector2(fd.y, -fd.x) * 9f; float sy = Ground(t, sp.x, sp.y);
                Post(props, new Vector3(sp.x, sy - .1f, sp.y), 3.2f, .14f, new Color(.5f, .5f, .52f));
                SignOn(MakeFrame(new Vector3(sp.x, sy, sp.y), fd, 1f), 0f, 3.0f, 3.6f, SignHospital, .12f);
                log.Append($"main entrance at ({q.x:0}, {q.y:0}) facing {Mathf.Atan2(n.x, n.y) * Mathf.Rad2Deg:0} degrees; ");
            }
            Vector2 Rot(Vector2 front, float side, float ahead) => new Vector2(front.y, -front.x) * side + front * ahead;

            // ---------------- the emergency department (photos 04 / 05)
            if (d.er != null && d.er.index >= 0 && d.er.index < map.buildings.Length)
            {
                var ring = ToPoints(map.buildings[d.er.index].footprint); if (PolyArea(ring) < 0f) ring.Reverse();
                float lo = ring.Min(p => Ground(t, p.x, p.y)), avg = ring.Average(p => Ground(t, p.x, p.y));
                Block(ring, lo - .5f, avg + 3f * KsFloor + .6f, lo, new Color(.84f, .74f, .58f), 1, true);
                // the ambulance canopy (OSM's roof over the bay), white, with angled struts and EMERGENCY along its edge
                if (d.er.canopy >= 0 && BestRectangle(ToPoints(map.buildings[d.er.canopy].footprint), out var cc, out var cU, out var cV, out float chl, out float chw))
                {
                    // its outer side: away from the ER's middle
                    var ercc = erc0(ring); if (Vector2.Dot(cc - ercc, cV) < 0f) cV = -cV;
                    float cy = Ground(t, cc.x, cc.y) + 5.2f; var f = new Frame { O = new Vector3(cc.x, cy, cc.y), R = new Vector3(cU.x, 0f, cU.y), F = new Vector3(cV.x, 0f, cV.y), M = 1f };
                    BoxF(bodies, f, -chl, chl, 0f, .7f, -chw, chw + 1.5f, KsWhite);
                    BoxF(bodies, f, -chl, chl, .7f, 2.4f, chw + 1.2f, chw + 1.5f, KsWhite);   // the fascia
                    for (float u = -chl + 2f; u <= chl - 2f; u += Mathf.Max(4f, (2f * chl - 4f) / 4f))
                    {
                        Beam(bodies, f.P(u, 2.4f, chw + 1.3f), f.P(u, 6.5f, -chw), .35f, .35f, KsWhite);   // a strut up and back to the wall
                        var pp = f.P(u, 0f, chw + 1.0f); float g = Ground(t, pp.x, pp.z); Post(bodies, new Vector3(pp.x, g - .2f, pp.z), cy - g + .2f, .4f, KsWhite);
                        Solid(new Vector3(pp.x, (g + cy) / 2f, pp.z), new Vector3(.4f, cy - g, .4f), cU, "Kennestone ER canopy post");
                    }
                    SignOn(f, 0f, 1.55f, Mathf.Min(2f * chl - 2f, 11f), SignEmergency, chw + 1.52f);
                    // the ambulances under it: white boxes with a red band and a light bar
                    for (int k = 0; k < 3; k++)
                    {
                        float u = Mathf.Lerp(-chl + 4f, chl - 4f, k / 2f); var ap = f.P(u, 0f, chw * .2f); float ay = Ground(t, ap.x, ap.z);
                        var af = MakeFrame(new Vector3(ap.x, ay, ap.z), cU, 1f); Ambulance(props, af);
                        Solid(new Vector3(ap.x, ay + 1.4f, ap.z), new Vector3(2.4f, 2.8f, 6.6f), cU, "Kennestone ambulance");
                    }
                    log.Append($"ER canopy {2f * chl:0} m long; ");
                }
                // the terraced wall between the bay and Church Street below (photo 04): two steps of block wall with shrubs, along the street on the ER's side,
                // as far along it as the ER reaches
                var westmost = ring.OrderBy(p => p.x).First(); var erc = erc0(ring);
                var near = roads.Where(s => s.half >= 4f && SegDist(westmost, s.a, s.b) < 45f).OrderBy(s => SegDist(westmost, s.a, s.b)).ToList();
                float zLo = ring.Min(p => p.y) - 8f, zHi = ring.Max(p => p.y) + 8f;
                foreach (var s in near.Take(3))
                {
                    var dir = (s.b - s.a).normalized; var side = new Vector2(dir.y, -dir.x); if (Vector2.Dot(erc - (s.a + s.b) * .5f, side) < 0f) side = -side;
                    for (int k = 0; k < 2; k++)
                    {
                        var a = s.a + side * (s.half + 1.6f + k * 1.4f); var b = s.b + side * (s.half + 1.6f + k * 1.4f); float L = (b - a).magnitude;
                        for (float u = 0f; u < L; u += 3f)
                        {
                            if ((a + dir * u).y < zLo || (a + dir * u).y > zHi) continue;
                            var q = a + dir * u; float g = Ground(t, q.x, q.y); var wf = MakeFrame(new Vector3(q.x, g, q.y), side, 1f);
                            BoxF(props, wf, 0f, Mathf.Min(3f, L - u), -.4f, .75f, -.3f, 0f, new Color(.66f, .60f, .52f));
                            props.Tint = new Color(.22f, .40f, .18f); Cyl(props, wf.P(1.5f, .4f, .5f), .55f, .45f, .25f, new Color(.22f, .40f, .18f), 6);
                        }
                    }
                }
            }
            static Vector2 erc0(List<Vector2> r) => r.Aggregate(Vector2.zero, (s, q) => s + q) / r.Count;

            // ---------------- the skybridges (photos 01 / 02): a floor up over the road
            int nBridges = 0;
            foreach (var rb in d.raised ?? new KsRaised[0])
            {
                if (rb.index < 0 || rb.index >= map.buildings.Length) continue;
                var ring = ToPoints(map.buildings[rb.index].footprint); if (!BestRectangle(ring, out var c, out var U, out var V, out float hl, out float hw)) continue;
                hw = Mathf.Max(hw, 2.6f);
                // where it is over a road (along its length)
                float ua = float.MaxValue, ub = float.MinValue, roadY = float.MinValue;
                for (float u = -hl; u <= hl; u += .5f) { var q = c + U * u; if (RoadGap(q) < .8f) { ua = Mathf.Min(ua, u); ub = Mathf.Max(ub, u); roadY = Mathf.Max(roadY, Ground(t, q.x, q.y)); } }
                if (ua > ub) { ua = -hl * .4f; ub = hl * .4f; roadY = Ground(t, c.x, c.y); }
                ua -= 1f; ub += 1f;
                float bottom = roadY + 5.6f, top = bottom + Mathf.Max(1, rb.levels - rb.min_level) * KsFloor;
                var f = new Frame { O = new Vector3(c.x, 0f, c.y), R = new Vector3(U.x, 0f, U.y), F = new Vector3(V.x, 0f, V.y), M = 1f };
                // tan ends and stair towers down to the ground each side of the road, the glass span between
                foreach (var (u0, u1) in new[] { (-hl, ua), (ub, hl) })
                {
                    if (u1 - u0 < .5f) continue;
                    BoxF(bodies, f, u0, u1, bottom, top, -hw, hw, KsTan);
                    float tu0 = u0 < 0f ? Mathf.Max(u0, u1 - 4.5f) : u0, tu1 = u0 < 0f ? u1 : Mathf.Min(u1, u0 + 4.5f);
                    var tc = c + U * ((tu0 + tu1) / 2f); float tg = Ground(t, tc.x, tc.y);
                    BoxF(bodies, f, tu0, tu1, tg - .5f, top + 1.2f, -hw - .3f, hw + .3f, KsTanDark);
                    Solid(new Vector3(tc.x, (tg + top) / 2f, tc.y), new Vector3(hw * 2f + .6f, top - tg + 1f, tu1 - tu0), U, "Kennestone skybridge tower");
                }
                BoxF(glassy, f, ua, ub, bottom, top, -hw + .1f, hw - .1f, Shade(KsGlass, 1.2f));
                BoxF(bodies, f, ua, ub, bottom - .4f, bottom, -hw, hw, KsWhite); BoxF(bodies, f, ua, ub, top, top + .5f, -hw, hw, KsWhite);
                foreach (float side in new[] { -1f, 1f })
                {
                    float v = side * (hw + .05f); float um = (ua + ub) / 2f;
                    Beam(bodies, f.P(ua, bottom, v), f.P(um, top, v), .25f, .25f, KsWhite); Beam(bodies, f.P(ub, bottom, v), f.P(um, top, v), .25f, .25f, KsWhite);
                    Beam(bodies, f.P(ua, top, v), f.P(um, bottom, v), .25f, .25f, KsWhite); Beam(bodies, f.P(ub, top, v), f.P(um, bottom, v), .25f, .25f, KsWhite);
                    // the round emblem in the middle: a white ring round a blue disc
                    for (int k = 0; k < 20; k++)
                    {
                        float a0 = k * Mathf.PI * 2f / 20f, a1 = (k + 1) * Mathf.PI * 2f / 20f; float yc = (bottom + top) / 2f, vv = side * (hw + .12f);
                        Vector3 P(float a, float r) => f.P(um + Mathf.Cos(a) * r, yc + Mathf.Sin(a) * r, vv);
                        bodies.Tint = KsWhite; Face(bodies, P(a0, 1.5f), P(a1, 1.5f), P(a1, 1.2f), P(a0, 1.2f), f.F * side);
                        bodies.Tint = new Color(.20f, .25f, .50f); TriFace(bodies, f.P(um, yc, vv), P(a1, 1.2f), P(a0, 1.2f), f.F * side);
                    }
                    // the white arch under the span, springing from the towers
                    var arch = new List<Vector3>(); for (int k = 0; k <= 12; k++) { float s = k / 12f; float u = Mathf.Lerp(ua, ub, s); arch.Add(f.P(u, bottom - 2.6f + 2.4f * Mathf.Sin(s * Mathf.PI), side * (hw - .3f))); }
                    for (int k = 0; k + 1 < arch.Count; k++) Beam(bodies, arch[k], arch[k + 1], .35f, .45f, KsWhite);
                }
                var mc = c + U * ((ua + ub) / 2f);
                Solid(new Vector3(mc.x, (bottom + top) / 2f, mc.y), new Vector3(hw * 2f, top - bottom + .4f, ub - ua), U, "Kennestone skybridge");
                nBridges++; log.Append($"skybridge {ub - ua:0} m over the road, its underside {bottom - roadY:0.0} m up; ");
            }

            bodies.ToObject("Kennestone buildings", mats.House, root, true, true);
            glassy.ToObject("Kennestone glass", mats.House, root, false, true);
            props.ToObject("Kennestone grounds", mats.House, root, false, true);
            if (signMat != null && signs.Count > 0) signs.ToObjects(root, signMat, "KennestoneSigns");
            Debug.Log($"[Round77] B: Kennestone built: {log}{nBridges} skybridges, {signs.Count} signs");
            BuildDecks(map, t, mats, world);
        }

        static void Ambulance(MeshBuilder mb, Frame f)
        {
            BoxF(mb, f, -1.15f, 1.15f, .45f, 2.9f, -3.3f, 1.2f, KsWhite);       // the box
            BoxF(mb, f, -1.1f, 1.1f, .45f, 2.1f, 1.2f, 3.2f, KsWhite);          // the cab
            FrontQuad(mb, f, -.95f, .95f, 1.35f, 2.0f, 3.21f, KsGlass);
            BoxF(mb, f, -1.17f, 1.17f, 1.4f, 1.75f, -3.3f, 1.2f, new Color(.80f, .10f, .08f));   // the red band
            BoxF(mb, f, -.9f, .9f, 2.1f, 2.3f, 2.2f, 2.6f, new Color(.85f, .15f, .10f));        // the light bar
            foreach (float u in new[] { -1.0f, 1.0f }) foreach (float v in new[] { -2.4f, 2.2f }) BoxF(mb, f, u - .15f, u + .15f, 0f, .8f, v - .4f, v + .4f, new Color(.08f, .08f, .08f));
        }

        // ---------------- the parking decks (every OSM building=parking on the map)
        static void BuildDecks(MapData map, TerrainGrid t, Materials mats, Transform world)
        {
            var d = KennestoneNow; if (d?.decks == null) return;
            var root = new GameObject("Parking decks").transform; root.SetParent(world, false);
            var signs = new SignBatch(); var signMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(GeneratedDir + "/BusinessSigns.mat") ?? BusinessSignMaterial();
            int built = 0, ramps = 0, cars = 0;
            foreach (var dk in d.decks)
            {
                if (!deckBase.TryGetValue(dk.index, out float y0)) continue;
                var c = new Vector2(dk.centre[0], dk.centre[1]); var U = new Vector2(dk.u[0], dk.u[1]); var V = new Vector2(-U.y, U.x);
                float hl = dk.hl, hw = dk.hw; int N = Mathf.Clamp(dk.levels, 2, 9);
                bool twoLanes = 2f * hw >= 2f * DeckLane + 6f && 2f * hl >= 2f * DeckLanding + 24f;
                if (!twoLanes) N = 2;   // narrow or short: one ramp to the roof
                var go = new GameObject($"Parking deck {dk.index}"); go.transform.SetParent(root, false);
                var mb = new MeshBuilder(); var cols = new List<(Vector3 c, Vector3 size, Quaternion rot)>();
                var f = new Frame { O = new Vector3(c.x, y0, c.y), R = new Vector3(U.x, 0f, U.y), F = new Vector3(V.x, 0f, V.y), M = 1f };
                var rot = Quaternion.LookRotation(f.F, Vector3.up);
                void Box(float u0, float u1, float yb, float yt, float v0, float v1, Color col, bool solid = true)
                {
                    BoxF(mb, f, u0, u1, yb, yt, v0, v1, col);
                    if (solid) cols.Add((f.P((u0 + u1) / 2f, (yb + yt) / 2f, (v0 + v1) / 2f), new Vector3(u1 - u0, yt - yb, v1 - v0), rot));
                }
                // the ramp lanes: lane A along the v = -hw side (the even ramps, from end A), lane B along v = +hw (the odd ramps, from end B); the parking
                // floor between them; each floor's landing across the whole width at its end, so from one ramp to the next is two easy quarter turns
                // across the deck's end (build 1's lanes side by side needed a 3.5 m U-turn no car makes)
                float aIn = -hw + DeckLane, bIn = twoLanes ? hw - DeckLane : hw;   // the lanes' inner edges (no lane B: the floor runs to the far side)
                float H = DeckFloor, slab = .35f; var wallC = Shade(DeckConcrete, .95f); var curbC = new Color(.95f, .80f, .15f);
                // the ground floor: a concrete slab just over the levelled ground (the wheels on it, flush at the edges)
                BoxF(mb, f, -hl, hl, -.3f, .04f, -hw, hw, Shade(DeckConcrete, .9f));
                int gapEnd = (N - 1) % 2 == 0 ? 1 : -1;   // the roof's jump: through the end wall away from where the last ramp arrives
                for (int k = 1; k < N; k++)
                {
                    float y = k * H; bool endA = k % 2 == 0; bool roof = k == N - 1;
                    float l0 = endA ? -hl : hl - DeckLanding, l1 = endA ? -hl + DeckLanding : hl;   // this floor's landing (u)
                    Box(-hl, hl, y - slab, y, aIn, bIn, DeckConcrete);                     // the parking floor
                    Box(l0, l1, y - slab, y, -hw, aIn, DeckConcrete);                      // the landing over lane A
                    if (twoLanes) Box(l0, l1, y - slab, y, bIn, hw, DeckConcrete);         // and over lane B
                    // low walls: both long sides, both ends (on the roof, the far end's middle third left open), the lanes' inner edges outside the landing
                    Box(-hl, hl, y, y + 1.0f, -hw, -hw + .25f, wallC); Box(-hl, hl, y, y + 1.0f, hw - .25f, hw, wallC);
                    foreach (int e in new[] { -1, 1 })
                    {
                        float u0 = e < 0 ? -hl : hl - .25f, u1 = e < 0 ? -hl + .25f : hl;
                        if (roof && e == gapEnd) { Box(u0, u1, y, y + 1.0f, -hw, -hw / 3f, wallC); Box(u0, u1, y, y + 1.0f, hw / 3f, hw, wallC); BoxF(mb, f, e < 0 ? -hl + .6f : hl - 1.0f, e < 0 ? -hl + 1.0f : hl - .6f, y - .02f, y + .02f, -hw / 3f, hw / 3f, curbC); }
                        else Box(u0, u1, y, y + 1.0f, -hw, hw, wallC);
                    }
                    float cu0 = endA ? -hl + DeckLanding : -hl, cu1 = endA ? hl : hl - DeckLanding;
                    Box(cu0, cu1, y, y + .9f, aIn, aIn + .2f, curbC);
                    if (twoLanes) Box(cu0, cu1, y, y + .9f, bIn - .2f, bIn, curbC);
                    // columns along the long sides and the lanes' inner edges
                    for (float u = -hl + 4f; u <= hl - 3f; u += 8.5f) foreach (float v in twoLanes ? new[] { -hw + .4f, aIn + .4f, bIn - .4f, hw - .4f } : new[] { -hw + .4f, aIn + .4f, hw - .4f })
                        Box(u - .3f, u + .3f, y - H + .04f, y - slab, v - .3f, v + .3f, Shade(DeckConcrete, .85f));
                    // parked cars: a row along each lane's inner edge, nose in (every 2.9 m, about half the spaces), not on the landings' turning room
                    if (!roof)
                        for (float u = -hl + DeckLanding + 2f; u <= hl - DeckLanding - 2f; u += 2.9f)
                            foreach (float v in twoLanes ? new[] { aIn + 2.9f, bIn - 2.9f } : new[] { aIn + 2.9f })
                            {
                                if (Hash01(dk.index * 131 + k * 17 + Mathf.RoundToInt(u * 3f + v), 77) > .55f) continue;
                                var p = f.P(u, y, v); ParkedCar(mb, MakeFrame(new Vector3(p.x, y0 + y, p.z), V * Mathf.Sign(-v), 1f), dk.index * 97 + k * 31 + (int)(u * 7f) + (int)v); cars++;
                            }
                }
                // the ramps: ramp k from floor k to k + 1, in lane A from end A (even k) or lane B from end B (odd k)
                for (int k = 0; k + 1 < N; k++)
                {
                    bool fromA = k % 2 == 0 || !twoLanes; float v0 = fromA ? -hw : bIn, v1 = fromA ? aIn : hw;
                    float ua = -hl + DeckLanding, ub = hl - DeckLanding;
                    float ys = k * H, ye = (k + 1) * H;
                    float uStart = fromA ? ua : ub, uEnd = fromA ? ub : ua;
                    if (!twoLanes) { uStart = -hl + 4f; uEnd = hl - DeckLanding; }
                    var pa = f.P(uStart, ys, (v0 + v1) / 2f); var pb = f.P(uEnd, ye, (v0 + v1) / 2f); var along = pb - pa; float len = along.magnitude;
                    var fwd = along.normalized; var r = Quaternion.LookRotation(fwd, (Vector3.up - fwd * fwd.y).normalized);
                    var mid = (pa + pb) / 2f - (r * Vector3.up) * (slab / 2f);
                    cols.Add((mid, new Vector3(DeckLane - .3f, slab, len + .6f), r));
                    var side = r * Vector3.right * ((DeckLane - .3f) / 2f); var nrm = r * Vector3.up; var ext = along.normalized * .3f;
                    Vector3 A0 = pa - ext - side, A1 = pa - ext + side, B0 = pb + ext - side, B1 = pb + ext + side;
                    mb.Tint = DeckConcrete; Face(mb, A0, B0, B1, A1, nrm); Face(mb, A0 - nrm * slab, A1 - nrm * slab, B1 - nrm * slab, B0 - nrm * slab, -nrm);
                    Face(mb, A0, A0 - nrm * slab, B0 - nrm * slab, B0, -(r * Vector3.right)); Face(mb, A1, B1, B1 - nrm * slab, A1 - nrm * slab, r * Vector3.right);
                    // a low wall each side of the ramp (the deck's own long walls are at the floors' heights only)
                    foreach (float sd in new[] { -1f, 1f }) { var o = r * Vector3.right * sd * ((DeckLane - .3f) / 2f); Beam(mb, pa + o + Vector3.up * .45f, pb + o + Vector3.up * .45f, .2f, .9f, wallC); cols.Add(((pa + pb) / 2f + o + nrm * .45f, new Vector3(.2f, .9f, len), r)); }
                    ramps++;
                }
                // PARKING over the way in (the lanes' end A at the ground), on the outer face
                float sh = Mathf.Min(H * .85f, 3.2f);
                if (signMat != null) signs.Add(f.P(-hl - .05f, sh, (-hw + aIn) / 2f), new Vector2(-U.x, -U.y), 4.8f, .6f, SignParking);
                // the solid parts
                mb.ToObject($"Parking deck {dk.index} structure", mats.House, go.transform, false, true);
                foreach (var (cc, size, rr) in cols) { var o = new GameObject("deck part"); o.transform.SetParent(go.transform, false); o.transform.SetPositionAndRotation(cc, rr); o.AddComponent<BoxCollider>().size = size; }
                built++;
            }
            if (signMat != null && signs.Count > 0) signs.ToObjects(root, signMat, "ParkingDeckSigns");
            Debug.Log($"[Round77] B: {built} parking decks built ({ramps} ramps, {cars} parked cars)");
        }

        static void ParkedCar(MeshBuilder mb, Frame f, int seed)
        {
            var paints = new[] { new Color(.85f, .85f, .86f), new Color(.12f, .12f, .14f), new Color(.55f, .10f, .10f), new Color(.20f, .30f, .55f), new Color(.55f, .56f, .58f), new Color(.75f, .72f, .62f) };
            var col = paints[(int)(Hash01(seed, 5) * paints.Length) % paints.Length];
            BoxF(mb, f, -.9f, .9f, .3f, 1.0f, -2.2f, 2.2f, col);
            BoxF(mb, f, -.8f, .8f, 1.0f, 1.5f, -1.2f, .9f, Shade(col, .9f));
            FrontQuad(mb, f, -.75f, .75f, 1.02f, 1.45f, .91f, KsGlass);
        }
    }
}
