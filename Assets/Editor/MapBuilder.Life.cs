using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Racer;
using UnityEditor;
using UnityEngine;

// Round 38 (Part E, Dan picked "Life in the world"): where Racer's ambient life (Assets/Scripts/World/Life: AmbientLife, Wildlife, WoodlandAmbience,
// UndergroundLife) lives on this map. World, built with the map (the people and animals are lent to these places near the player at run time):
//   people: groups in the yards (a football trio where the front yard is deep and flat, else two talking with mugs), on the porches (by the front
//     door), on the drives (beside the garage), on the pool decks at Chastain Lakes and the Enclave, and walkers on the shops' walks;
//   animals (Racer's species): deer, coyotes and wild turkeys (in threes) in the woods along Noonday Creek and the other woods, songbirds and
//     squirrels at the woods' edges and in the yards' trees, frogs on the ponds' banks; each with its escape (away from the nearest road or trail);
//   the tree cover in 40 m cells (WoodlandAmbience's weight);
//   rats just inside the network's two main entrances and a few open mouths, drips in the junction chambers and the hub (UndergroundLife).
// Kept off: roads, drives (but the drive spots), sidewalks, lots, every race's and shortcut's line (8 m), jumps (30 m), the trails, the stunt park,
// water, the pipes' cuts, buildings; nothing within 25 m of the owner's marked house (privacy: no people pointing at it).
namespace ChastainRush
{
    public static partial class MapBuilder
    {
        static readonly HashSet<string> HouseTypes = new() { "detached", "terrace", "semidetached_house", "house" };
        static string ActionName(byte a) => a switch { 0 => "football trios", 1 => "coffee pairs", 3 => "walkers (shops)", 4 => "on the phone", 5 => "two talking", 6 => "gardening", 7 => "to the mailbox", 8 => "kids playing", _ => "action " + a };

        static void BuildLife(MapData map, TerrainGrid t, Transform parent, Neighborhood hood, RestNeighborhoods rest, Businesses biz, ChastainLakes lakes, CreekTrack creek, Ridge ridge, TreeField trees, OwnerMarker owner)
        {
            var root = new GameObject("Life (Round 38)").transform; root.SetParent(parent, false);
            var pave = new Pavement(map);
            // ---------------- what life keeps off (as the hydrants: MapBuilder.Hydrants.cs)
            var lines = new List<List<Vector2>>();
            foreach (var f in Directory.GetFiles(Path.Combine(ProjectRoot, "Data/routes"), "*.json"))
            {
                var rj = JsonUtility.FromJson<RouteJson>(File.ReadAllText(f));
                if (rj.loop != null && rj.loop.Length > 3) lines.Add(ToPoints(rj.loop));
                if (rj.loop_rev != null && rj.loop_rev.Length > 3) lines.Add(ToPoints(rj.loop_rev));   // Round 39: a reverse lap of its own
                if (rj.lead_in != null && rj.lead_in.Length > 3) lines.Add(ToPoints(rj.lead_in));
                if (rj.official_shortcuts != null) foreach (var sc in rj.official_shortcuts) if (sc.path != null) lines.Add(ToPoints(sc.path));
            }
            var lineSegs = new Dictionary<Vector2Int, List<(Vector2 a, Vector2 b)>>();
            foreach (var l in lines) for (int i = 0; i + 1 < l.Count; i++) { var k = new Vector2Int(Mathf.FloorToInt(l[i].x / 64f), Mathf.FloorToInt(l[i].y / 64f)); if (!lineSegs.TryGetValue(k, out var ls)) lineSegs[k] = ls = new List<(Vector2, Vector2)>(); ls.Add((l[i], l[i + 1])); }
            bool NearLine(Vector2 p, float d)
            {
                var c = new Vector2Int(Mathf.FloorToInt(p.x / 64f), Mathf.FloorToInt(p.y / 64f));
                for (int dx = -1; dx <= 1; dx++) for (int dz = -1; dz <= 1; dz++) if (lineSegs.TryGetValue(c + new Vector2Int(dx, dz), out var ls)) foreach (var s in ls) if (SegDist(p, s.a, s.b) < d) return true;
                return false;
            }
            var jumps = UnityEngine.Object.FindObjectsByType<BigJump>(FindObjectsSortMode.None).Select(j => new Vector2(j.lip.x, j.lip.z)).ToList();
            var lots = (map.parking ?? new Area[0]).Select(a => ToPoints(a.outline)).Where(r => r.Count > 2).ToList(); lots.Add(LotRing.ToList());
            var bgrid = new Dictionary<Vector2Int, List<List<Vector2>>>();
            foreach (var b in map.buildings) { var ring = ToPoints(b.footprint); if (ring.Count < 3) continue; var k = new Vector2Int(Mathf.FloorToInt(ring[0].x / 64f), Mathf.FloorToInt(ring[0].y / 64f)); if (!bgrid.TryGetValue(k, out var l)) bgrid[k] = l = new List<List<Vector2>>(); l.Add(ring); }
            float BuildingDist(Vector2 p)
            {
                var c = new Vector2Int(Mathf.FloorToInt(p.x / 64f), Mathf.FloorToInt(p.y / 64f)); float best = 99f;
                for (int dx = -1; dx <= 1; dx++) for (int dz = -1; dz <= 1; dz++) if (bgrid.TryGetValue(c + new Vector2Int(dx, dz), out var l)) foreach (var r in l) { if (InPolygon(p, r)) return -1f; best = Mathf.Min(best, RingDist(p, r)); }
                return best;
            }
            var roadGrid = new Dictionary<Vector2Int, List<(Vector2 a, Vector2 b, float half)>>();
            foreach (var r in map.roads)
            {
                if (r.points == null || r.points.Length < 4) continue; var pts = ToPoints(r.points);
                for (int i = 0; i + 1 < pts.Count; i++) { var k = new Vector2Int(Mathf.FloorToInt(pts[i].x / 64f), Mathf.FloorToInt(pts[i].y / 64f)); if (!roadGrid.TryGetValue(k, out var l)) roadGrid[k] = l = new List<(Vector2, Vector2, float)>(); l.Add((pts[i], pts[i + 1], r.width * .5f)); }
            }
            // the nearest road edge within ~100 m: its point and the distance past its edge
            float RoadEdge(Vector2 p, out Vector2 q)
            {
                var c = new Vector2Int(Mathf.FloorToInt(p.x / 64f), Mathf.FloorToInt(p.y / 64f)); float best = float.MaxValue; q = p;
                for (int dx = -2; dx <= 2; dx++) for (int dz = -2; dz <= 2; dz++) if (roadGrid.TryGetValue(c + new Vector2Int(dx, dz), out var l))
                            foreach (var s in l) { var ab = s.b - s.a; float u = ab.sqrMagnitude < 1e-6f ? 0f : Mathf.Clamp01(Vector2.Dot(p - s.a, ab) / ab.sqrMagnitude); var x = s.a + ab * u; float d = (x - p).magnitude - s.half; if (d < best) { best = d; q = x; } }
                return best;
            }
            var ownerAt = owner != null ? new Vector2(owner.x, owner.z) : new Vector2(-1e6f, -1e6f);
            var why = new Dictionary<string, int>();
            bool Ok(Vector2 p, bool drive = false, float lineGap = 5f)   // 5 m from a race or shortcut line (its middle: off the street's edge as the pavement check already keeps them)
            {
                string no = null;
                if (!drive && pave.On(p, .8f)) no = "pavement";
                else if (RoadEdge(p, out _) < (drive ? 3.0f : 2.5f)) no = "a road";
                else if (lots.Any(l => InPolygon(p, l) || RingDist(p, l) < 1.5f)) no = "a lot";
                else if (biz != null && biz.RejectTree(p.x, p.y)) no = "a business lot, walk or drive";
                else if (BuildingDist(p) < .8f) no = "a building";
                else if (NearLine(p, lineGap)) no = "a race or shortcut line";
                else if (jumps.Any(j => (j - p).sqrMagnitude < 30f * 30f)) no = "a jump";
                else if (creek != null && creek.RejectTree(p.x, p.y) || ridge != null && ridge.RejectTree(p.x, p.y)) no = "a trail, a jump's landing";
                else if (Park != null && Park.InPark(p)) no = "the stunt park";
                else if (Beds.Wet(p.x, p.y)) no = "water";
                else if (Net != null && Net.RejectTree(p.x, p.y)) no = "a pipe's cut";
                else if ((p - ownerAt).sqrMagnitude < 25f * 25f) no = "the owner's house";
                if (no != null) { why[no] = why.TryGetValue(no, out int n) ? n + 1 : 1; return false; }
                return true;
            }
            bool Flat(Vector2 p, float r) { float y0 = Ground(t, p.x, p.y); foreach (var d in new[] { new Vector2(r, 0), new Vector2(-r, 0), new Vector2(0, r), new Vector2(0, -r) }) if (Mathf.Abs(Ground(t, p.x + d.x, p.y + d.y) - y0) > r * .3f) return false; return true; }   // (lawns up to about 17 degrees: the Glen's lots slope)
            Vector3 At(Vector2 p) => new Vector3(p.x, Ground(t, p.x, p.y), p.y);
            float Yaw(Vector2 d) => Mathf.Atan2(d.x, d.y) * Mathf.Rad2Deg;

            // ---------------- the people's groups
            var groups = new List<AmbientLife.Group>(); var taken = new List<Vector2>();
            bool Free(Vector2 p, float gap) { foreach (var q in taken) if ((q - p).sqrMagnitude < gap * gap) return false; return true; }
            void Add(Vector2 p, Vector2 face, AmbientLife.Kind kind, int count, int action, Vector2 walk = default)
            { groups.Add(new AmbientLife.Group { at = At(p), yaw = Yaw(face), kind = kind, count = (byte)count, action = (byte)action, walk = new Vector3(walk.x, 0, walk.y) }); taken.Add(p); }
            int houses = 0, coffees = 0;
            for (int bi = 0; bi < map.buildings.Length; bi++)
            {
                var b = map.buildings[bi]; if (!HouseTypes.Contains(b.type)) continue;
                var ring = ToPoints(b.footprint); if (ring.Count < 3) continue;
                var c = Vector2.zero; foreach (var q in ring) c += q; c /= ring.Count;
                float edge = RoadEdge(c, out var rq); if (edge > 70f) continue;
                var n = (rq - c); if (n.sqrMagnitude < 1e-4f) continue; n.Normalize(); var side = new Vector2(n.y, -n.x);
                // the front wall where the ray from the middle toward the road leaves the house
                float s = 0f; while (s < 40f && InPolygon(c + n * (s + .25f), ring)) s += .25f; var front = c + n * s;
                float depth = RoadEdge(front, out _); houses++;
                uint h = Mix(bi, 38);
                // Round 39 (Part B, Dan: "far too many people drinking coffee ... some variety other than people just standing around"): the coffee
                // pair only at about 1 house in 40 (and AmbientLife lets one out at a time), a porch group at a third of the houses (was every one), and
                // the rest doing something: two talking, someone gardening, someone walking down to the mailbox, kids playing tag
                bool coffee = Mix(bi, 39) % 40 == 0;
                // the porch: by the front door, facing the street: one on the phone, two talking, or one walking down to the mailbox and back
                var porch = front + n * 1.4f;
                if (h % 3 == 0 && Ok(porch) && Flat(porch, 1.2f) && Free(porch, 10f))
                {
                    int what = (int)((h >> 2) % 3);
                    if (coffee) { Add(porch, n, AmbientLife.Kind.Porch, 2, AmbientLife.Coffee); coffees++; coffee = false; }
                    else if (what == 2 && depth >= 6f) { var box = front + n * (depth - 1.2f); Add((porch + box) * .5f, n, AmbientLife.Kind.Porch, 1, AmbientLife.Mail, (box - porch) * .5f); }
                    else Add(porch, n, AmbientLife.Kind.Porch, what == 1 ? 2 : 1, what == 1 ? AmbientLife.Talking : AmbientLife.Gesture);
                }
                // the yard: half way to the street; a football trio where it is deep and flat, kids playing tag, two talking; or someone gardening
                // by the house's front
                if (depth >= 8f)
                {
                    var yard = front + n * Mathf.Min(depth * .5f, 12f) + side * ((h >> 3) % 2 == 0 ? 3f : -3f);
                    bool trio = depth >= 14f && Flat(yard, 3.5f) && (h >> 5) % 3 == 0; int what = (int)((h >> 9) % 4);
                    if (!trio && what == 1) yard = front + n * 2.2f + side * ((h >> 3) % 2 == 0 ? 3.5f : -3.5f);   // the flower bed along the front
                    if (Ok(yard) && Flat(yard, what == 1 ? 1f : 1.5f) && Free(yard, 14f))
                    {
                        if (trio) Add(yard, side, AmbientLife.Kind.Yard, 3, AmbientLife.Football);
                        else if (coffee) { Add(yard, n, AmbientLife.Kind.Yard, 2, AmbientLife.Coffee); coffees++; coffee = false; }
                        else if (what == 1) Add(yard, n, AmbientLife.Kind.Yard, 1, AmbientLife.Gardening);
                        else if (what == 2) Add(yard, n, AmbientLife.Kind.Yard, 2, AmbientLife.Talking);
                        else if (Flat(yard, 3f)) Add(yard, n, AmbientLife.Kind.Yard, 2 + (int)((h >> 11) % 2), AmbientLife.Kids);
                        else Add(yard, n, AmbientLife.Kind.Yard, 2, AmbientLife.Talking);
                    }
                }
                // the drive: a point of the drive by the house (paved, not the road): two talking by the garage, or kids playing
                for (float off = -9f; off <= 9f; off += 1.5f)
                {
                    var d = front + n * 3.5f + side * off;
                    if (!pave.On(d, 0f) || RoadEdge(d, out _) < 4f) continue;
                    if (Ok(d, true) && Free(d, 12f) && (h >> 7) % 2 == 0) Add(d, side, AmbientLife.Kind.Drive, 2, (h >> 13) % 3 == 1 && Flat(d, 2.5f) ? AmbientLife.Kids : AmbientLife.Talking);
                    break;
                }
            }
            int yards = groups.Count(g => g.kind == AmbientLife.Kind.Yard), porches = groups.Count(g => g.kind == AmbientLife.Kind.Porch), drives = groups.Count(g => g.kind == AmbientLife.Kind.Drive);
            // the pool decks: Chastain Lakes' (its pool's rectangle) and the Enclave's (communities.json): two talking on each side
            var pools = new List<List<Vector2>>(); if (lakes != null) pools.Add(lakes.Pool); if (CoursonLapPool != null) pools.Add(CoursonLapPool);   // Round 39 (A4): the Courson Street pool
            try
            {
                var cj = File.ReadAllText(Path.Combine(ProjectRoot, "Data/neighborhood/communities.json")); var cm = JsonUtility.FromJson<CommunitiesJson>(cj);
                foreach (var co in cm.communities ?? new CommunityJson[0]) if (co.amenities != null && co.amenities.pool != null && co.amenities.pool.outline != null && co.amenities.pool.outline.Length >= 6) pools.Add(ToPoints(co.amenities.pool.outline));
            }
            catch (Exception e) { Debug.LogWarning("[Round38] life: the Enclave's pool not read: " + e.Message); }
            int poolGroups = 0;
            foreach (var ring in pools)
            {
                var c = Vector2.zero; foreach (var q in ring) c += q; c /= ring.Count;
                for (int i = 0; i < ring.Count; i++)
                {
                    var a = ring[i]; var b = ring[(i + 1) % ring.Count]; var mid = (a + b) * .5f; var o = (mid - c).normalized; var p = mid + o * 1.8f;
                    if (Beds.Wet(p.x, p.y) || BuildingDist(p) < .8f || NearLine(p, 8f)) continue;
                    bool cf = Mix(i * 31 + (int)p.x, 40) % 40 == 0; if (cf) coffees++;
                    Add(p, -o, AmbientLife.Kind.Pool, 2, cf ? AmbientLife.Coffee : AmbientLife.Talking); poolGroups++;   // Round 39: talking (the rare coffee pair)
                }
            }
            // the shops' walks: walkers along each walk's long side, every 30 m
            int shops = 0;
            if (biz != null)
                foreach (var w in biz.Walks)
                {
                    if (w.Count < 3) continue;
                    BestRectangle(w, out var wc, out var wu, out var wv, out float hl, out float hw);
                    if (hl < hw) { (wu, wv) = (wv, wu); (hl, hw) = (hw, hl); }
                    if (hl < 4f) continue;
                    for (float s = -hl + 4f; s <= hl - 4f; s += 30f)
                    {
                        var p = wc + wu * s; float half = Mathf.Min(6f, hl - Mathf.Abs(s) - 1f);
                        if (NearLine(p, 5f) || Park != null && Park.InPark(p) || BuildingDist(p) < 1.2f || !Free(p, 18f)) continue;   // (off the shop fronts: their walkers pass 0.8 m or more from the wall)
                        Add(p, wu, AmbientLife.Kind.Shop, 2, (shops % 3 == 2) ? AmbientLife.Talking : AmbientLife.Walkers, wu * half); shops++;   // Round 39: talking, not coffee
                    }
                }
            if (Mall != null) groups.AddRange(Mall.Shoppers);   // Round 60 (A): Town Center's shoppers (MapBuilder.MallInside.cs; built before the life)
            var life = root.gameObject.AddComponent<AmbientLife>(); life.groups = groups.ToArray();
            string mix = string.Join(", ", groups.GroupBy(g => g.action).OrderBy(g => g.Key).Select(g => $"{ActionName(g.Key)} {g.Count()}"));
            // ---------------- Round 39 (Part B): the neighborhoods' streets for the walkers (AmbientLife.Walkers.cs): each street's centerline, its walking
            // line's offset (on the sidewalk where there is one, otherwise at the street's edge), its points within 5 m of a race or shortcut line, and
            // which street ends meet (within 4 m)
            var streets = new List<(List<Vector2> pts, float off)>(); int onWalks = 0;
            var streetRoads = new HashSet<int>();
            for (int ri = 0; ri < map.roads.Length; ri++) if (hood.IsStreetRoad(ri) || hood.IsAreaRoad(ri)) streetRoads.Add(ri);
            if (rest?.Data?.neighborhoods != null) foreach (var nb in rest.Data.neighborhoods) if (nb.roads != null) foreach (int ri in nb.roads) if (ri >= 0 && ri < map.roads.Length) streetRoads.Add(ri);
            foreach (int ri in streetRoads.OrderBy(i => i))
            {
                var r = map.roads[ri]; if (r.tunnel || r.points == null || r.points.Length < 4 || r.@class == "service") continue;
                var pts = Densify(ToPoints(r.points), 3f); float half = r.width * .5f;
                // a sidewalk: its band 1.9 to 3.3 m past the pavement's edge (MapBuilder.Round11.cs) on either side at most of the street's points
                int hits = 0, tries = 0;
                for (int i = 1; i < pts.Count; i += 3) { var tg = (pts[i] - pts[i - 1]).normalized; var nr = new Vector2(tg.y, -tg.x); tries++; if (DriveBands.At(pts[i] + nr * (half + 2.6f)) == "sidewalk" || DriveBands.At(pts[i] - nr * (half + 2.6f)) == "sidewalk") hits++; }
                bool walk = tries > 0 && hits * 2 >= tries; if (walk) onWalks++;
                streets.Add((pts, half + (walk ? 2.6f : .7f)));
            }
            // Round 64 (Dan, BUG-004: "It would be good to have peopel walking or jogging this trail"): the Noonday Creek Trail's path, walked on its right half
            // (its own mix, AmbientLife.Walkers.cs; never kept off as a race's way: a few walk it in races too and step aside)
            int streetCount = streets.Count;
            if (TrailExtra?.trail?.line != null && TrailExtra.trail.line.Length >= 4) streets.Add((Densify(ToPoints(TrailExtra.trail.line), 3f), Mathf.Max(.4f, TrailExtra.trail.width * .5f - .7f)));
            // Round 67 (A5): park visitors on Kennesaw Mountain's trails (the battlefield park's: within its box), the race's trail among them
            int parkTrails = 0;
            foreach (var tl in map.trails ?? new TrailLine[0])
            {
                var tp = ToPoints(tl.points); if (tp.Count < 2 || !tp.Any(q => q.x > -2400f && q.x < 0f && q.y > -7600f && q.y < -4900f)) continue;
                streets.Add((Densify(tp, 3f), Mathf.Max(.4f, tl.width * .5f - .7f))); parkTrails++;
            }
            // Round 70 (E, Dan: "I thought there were supposed to be joggers and walkers on the kennesaw mnt trail"): the park's lots walked too (the
            // summit lot, the visitor center's): round each lot 2 m in from its edge, as a path
            int parkLots = 0;
            foreach (var a in map.parking ?? new Area[0])
            {
                var ring = ToPoints(a.outline); if (ring.Count < 3 || !ring.All(q => q.x > -2400f && q.x < 0f && q.y > -7600f && q.y < -4900f)) continue;
                Vector2 c = Vector2.zero; foreach (var q in ring) c += q; c /= ring.Count;
                var loop = ring.Select(q => q + (c - q).normalized * Mathf.Min(2f, (c - q).magnitude * .3f)).ToList(); if ((loop[0] - loop[loop.Count - 1]).sqrMagnitude > .01f) loop.Add(loop[0]);
                streets.Add((Densify(loop, 3f), .4f)); parkLots++;
            }
            var sp = new List<Vector2>(); var first = new List<int>(); var offs = new List<float>(); var race = new List<bool>(); var trailLine = new List<bool>();
            for (int l = 0; l < streets.Count; l++) { var (pts, off) = streets[l]; bool tr = l >= streetCount; first.Add(sp.Count); offs.Add(off); trailLine.Add(tr); foreach (var q in pts) { sp.Add(q); race.Add(!tr && NearLine(q, off + 5f)); } }
            first.Add(sp.Count);
            var linkFirst = new List<int>(); var linkList = new List<int>();
            for (int l = 0; l < streets.Count; l++)
                for (int e = 0; e < 2; e++)
                {
                    linkFirst.Add(linkList.Count); var at = e == 0 ? streets[l].pts[0] : streets[l].pts[streets[l].pts.Count - 1];
                    for (int o = 0; o < streets.Count; o++)
                    {
                        if (o == l) continue; var op = streets[o].pts;
                        if ((op[0] - at).sqrMagnitude < 16f) linkList.Add(2 * o); else if ((op[op.Count - 1] - at).sqrMagnitude < 16f) linkList.Add(2 * o + 1);
                    }
                }
            linkFirst.Add(linkList.Count);
            life.streetPts = sp.ToArray(); life.streetFirst = first.ToArray(); life.streetOff = offs.ToArray(); life.streetRace = race.ToArray(); life.linkFirst = linkFirst.ToArray(); life.links = linkList.ToArray();
            life.streetTrail = trailLine.ToArray();
            // what Round 38's rule made coffee pairs of the same places: every two-person porch (all of these), every yard group but the football trios,
            // every drive and pool-deck group, a third of the shops' walkers
            int coffeeR38 = groups.Count(g => g.kind == AmbientLife.Kind.Porch || g.kind == AmbientLife.Kind.Drive || g.kind == AmbientLife.Kind.Pool || (g.kind == AmbientLife.Kind.Yard && g.action != AmbientLife.Football) || (g.kind == AmbientLife.Kind.Shop && g.action == AmbientLife.Talking));
            Debug.Log($"[Round39] life: the mix {mix}; coffee pairs {coffees} of {groups.Count} groups (1 in {(coffees > 0 ? groups.Count / coffees : 0)}; Round 38's rule on these places: {coffeeR38}); walkers' streets {streets.Count} ({onWalks} on a sidewalk, the rest at the street's edge), " +
                      $"{sp.Count} points ({race.Count(x => x)} within 5 m of a race line), {linkList.Count} links between street ends; [Round67] {parkTrails} of Kennesaw Mountain's trails walked, [Round70] and {parkLots} of its lots");

            // ---------------- the animals' habitats
            var habitats = new List<Wildlife.Habitat>(); var animalAt = new Dictionary<Wildlife.Species, List<Vector2>>();
            bool Spaced(Wildlife.Species s, Vector2 p, float gap) { if (!animalAt.TryGetValue(s, out var l)) return true; foreach (var q in l) if ((q - p).sqrMagnitude < gap * gap) return false; return true; }
            Vector2 Escape(Vector2 p) { RoadEdge(p, out var q); var e = p - q; return e.sqrMagnitude > .01f ? e.normalized : Vector2.up; }   // away from the nearest road
            void Hab(Wildlife.Species s, Vector2 p, Vector2 esc) { habitats.Add(new Wildlife.Habitat { species = s, position = At(p) + Vector3.up * .03f, escape = new Vector3(esc.x, 0, esc.y) }); if (!animalAt.TryGetValue(s, out var l)) animalAt[s] = l = new List<Vector2>(); l.Add(p); }
            // the tree cover grid (40 m cells) from the trees as planted
            // Round 59: the grid over the whole map; the animals' places from the core's trees only (the map extension's trees follow them in
            // trees.bin: the core's habitats stay as they were)
            var lext = MapExtent(map);
            float cell = 40f; int gw = Mathf.CeilToInt(lext.width / cell), gh = Mathf.CeilToInt(lext.height / cell); var count = new int[gw * gh];
            var tf = trees; var treeList = new List<Vector2>();
            for (int i = 0; i < tf.count; i++) { var tr = TreeField.Unpack(tf.data, i); int x = Mathf.Clamp((int)((tr.x - lext.xMin) / cell), 0, gw - 1), z = Mathf.Clamp((int)((tr.z - lext.yMin) / cell), 0, gh - 1); count[z * gw + x]++; if (tr.x >= 0f && tr.z >= 0f) treeList.Add(new Vector2(tr.x, tr.z)); }
            var cover = count.Select(n => (byte)Mathf.Clamp(n * 255 / 28, 0, 255)).ToArray();
            float Cover(Vector2 p) { int x = Mathf.Clamp((int)((p.x - lext.xMin) / cell), 0, gw - 1), z = Mathf.Clamp((int)((p.y - lext.yMin) / cell), 0, gh - 1); return cover[z * gw + x] / 255f; }
            var rng = new System.Random(3838);
            foreach (int i in Enumerable.Range(0, treeList.Count).OrderBy(_ => rng.Next()))
            {
                var tr = treeList[i]; float cv = Cover(tr); var p = tr + new Vector2((float)rng.NextDouble() * 6f - 3f, (float)rng.NextDouble() * 6f - 3f);
                float road = RoadEdge(p, out _), bd = BuildingDist(p);
                if (cv >= .45f && road > 14f && bd > 15f)
                {
                    // the woods: deer, coyotes, turkeys (three together), squirrels, songbirds
                    if (!Ok(p, false, 10f) || !Flat(p, 1f)) continue;
                    var e = Escape(p); int pick = rng.Next(10);
                    if (pick < 2 && Spaced(Wildlife.Species.Deer, p, 70f)) Hab(Wildlife.Species.Deer, p, e);
                    else if (pick == 2 && Spaced(Wildlife.Species.Coyote, p, 110f)) Hab(Wildlife.Species.Coyote, p, e);
                    else if (pick == 3 && Spaced(Wildlife.Species.Turkey, p, 160f)) { for (int k = 0; k < 3; k++) { var q = p + new Vector2(k * 2.2f - 2.2f, (k % 2) * 1.6f); if (Ok(q)) Hab(Wildlife.Species.Turkey, q, e); } }
                    else if (pick < 7 && Spaced(Wildlife.Species.Squirrel, p, 45f)) Hab(Wildlife.Species.Squirrel, p, e);
                    else if (Spaced(Wildlife.Species.Bird, p, 45f)) Hab(Wildlife.Species.Bird, p, e);
                }
                else if (bd > 4f && bd < 30f && road > 8f)
                {
                    // the yards' trees: a songbird or a squirrel under one now and then
                    if (!Ok(p) || !Flat(p, .8f)) continue;
                    var e = Escape(p); bool bird = rng.Next(2) == 0;
                    if (bird && Spaced(Wildlife.Species.Bird, p, 70f)) Hab(Wildlife.Species.Bird, p, e);
                    else if (!bird && Spaced(Wildlife.Species.Squirrel, p, 70f)) Hab(Wildlife.Species.Squirrel, p, e);
                }
            }
            // frogs on the ponds' banks (up to three a pond, 12 m apart)
            int frogs = 0;
            if (map.water != null)
                foreach (var wa in map.water)
                {
                    var ring = ToPoints(wa.outline); if (ring.Count < 4) continue; var c = Vector2.zero; foreach (var q in ring) c += q; c /= ring.Count; int n = 0;
                    for (int i = 0; i < ring.Count && n < 3; i += Mathf.Max(1, ring.Count / 6))
                    {
                        var o = (ring[i] - c).normalized; var p = ring[i] + o * 1.6f;
                        if (Beds.Wet(p.x, p.y) || !Spaced(Wildlife.Species.Frog, p, 12f)) continue;
                        if (RoadEdge(p, out _) < 6f || BuildingDist(p) < 2f || NearLine(p, 6f) || Park != null && Park.InPark(p)) continue;
                        Hab(Wildlife.Species.Frog, p, o); n++; frogs++;
                    }
                }
            var wild = root.gameObject.AddComponent<Wildlife>(); wild.habitats = habitats.ToArray();
            AudioClip[] Clips(params string[] names) => names.Select(nm => AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Wildlife/" + nm)).Where(c => c).ToArray();
            wild.birdCalls = Clips("bird-1.wav", "bird-2.wav"); wild.squirrelCalls = Clips("squirrel-1.wav", "squirrel-2.wav"); wild.frogCalls = Clips("frog-1.wav", "frog-2.wav");
            wild.deerCalls = Clips("Deer-original-1.wav", "Deer-original-2.wav"); wild.coyoteCalls = Clips("ANMLWdog-coyote_howling-Elevenlabs.mp3");
            var amb = root.gameObject.AddComponent<WoodlandAmbience>(); amb.cover = cover; amb.origin = Vector2.zero; amb.cell = cell; amb.width = gw; amb.height = gh;

            // ---------------- underground: rats inside the main entrances and a few open mouths, drips in the chambers
            int ratSites = 0, drips = 0;
            try
            {
                var nj = JsonUtility.FromJson<LifeNetJson>(File.ReadAllText(Path.Combine(ProjectRoot, "Data/pipes/network.json")));
                var tubes = (nj.tubes ?? new LifeTube[0]).Where(tb => tb.pts != null && tb.pts.Length >= 6).ToDictionary(tb => tb.id, tb => Enumerable.Range(0, tb.pts.Length / 3).Select(i => new Vector3(tb.pts[3 * i], tb.pts[3 * i + 1], tb.pts[3 * i + 2])).ToList());
                var ratTubes = new List<(List<Vector3> pts, string name)>();
                foreach (var id in new[] { "BF-R", "BF-L", "GP-L", "GP-R" }) if (tubes.TryGetValue(id, out var pl)) ratTubes.Add((pl, id));
                foreach (var ex in nj.exits ?? new LifeExit[0]) if (ex.id == "X_SHOPS" || ex.id == "X_TRAIL" || ex.id == "X_BRIDGE") if (ex.tube != null && tubes.TryGetValue(ex.tube, out var pl) && ex.a != null && ex.a.Length >= 2)
                        { var a = new Vector3(ex.a[0], 0, ex.a[1]); var l = pl.ToList(); if ((Flat3(l[0]) - Flat3(a)).sqrMagnitude > (Flat3(l[l.Count - 1]) - Flat3(a)).sqrMagnitude) l.Reverse(); ratTubes.Add((l, ex.id)); }
                foreach (var (pts, name) in ratTubes)
                {
                    // along the tube from its outer end: the entrance 9 m in, the rats 10 to 16 m in, their refuges 22 m further, beside the wall
                    var cum = new List<float> { 0f }; for (int i = 1; i < pts.Count; i++) cum.Add(cum[i - 1] + Vector3.Distance(pts[i - 1], pts[i]));
                    Vector3 P(float s, out Vector3 f) { int i = 1; while (i < pts.Count - 1 && cum[i] < s) i++; float u = Mathf.InverseLerp(cum[i - 1], cum[i], s); f = (pts[i] - pts[i - 1]).normalized; return Vector3.Lerp(pts[i - 1], pts[i], u); }
                    if (cum[cum.Count - 1] < 45f) continue;
                    var go = new GameObject("Drain entrance rats " + name); go.transform.SetParent(root, false);
                    var ul = go.AddComponent<UndergroundLife>(); var e0 = P(9f, out var ef); ul.entrance = e0 + Vector3.up * .5f; ul.forward = new Vector3(ef.x, 0, ef.z).normalized;
                    var homes = new List<Vector3>(); var refuges = new List<Vector3>();
                    for (int i = 0; i < 4; i++)
                    {
                        var p = P(12f + i * 2.1f, out var f); var side = Vector3.Cross(Vector3.up, new Vector3(f.x, 0, f.z)).normalized; int sgn = i % 2 == 0 ? 1 : -1;
                        homes.Add(p + side * sgn * (1.0f + i * .2f) + Vector3.up * .14f);
                        var r = P(12f + i * 2.1f + 22f, out var f2); var side2 = Vector3.Cross(Vector3.up, new Vector3(f2.x, 0, f2.z)).normalized; refuges.Add(r + side2 * sgn * 1.7f + Vector3.up * .13f);
                    }
                    ul.homes = homes.ToArray(); ul.refuges = refuges.ToArray(); ratSites++;
                }
                foreach (var ch in nj.chambers ?? new LifeChamber[0])
                {
                    if (ch.kind != "junction" && ch.kind != "hub") continue;
                    var go = new GameObject("Quiet drips " + ch.id); go.transform.SetParent(root, false);
                    var ul = go.AddComponent<UndergroundLife>(); ul.cave = true; ul.entrance = new Vector3(ch.x, ch.floor + Mathf.Max(2f, ch.height * .6f), ch.z); drips++;
                }
            }
            catch (Exception e) { Debug.LogWarning("[Round38] life: the network not read: " + e.Message); }
            Debug.Log($"[Round38] life: {groups.Count} groups of people ({yards} yards, {porches} porches, {drives} drives at {houses} houses; {poolGroups} on the pool decks; {shops} on the shops' walks), " +
                      $"{habitats.Count} animal habitats ({string.Join(", ", animalAt.Select(kv => kv.Key + " " + kv.Value.Count))}), tree cover {gw} x {gh} cells of {cell:0} m, rats at {ratSites} entrances, drips in {drips} chambers; " +
                      "kept off: " + string.Join(", ", why.OrderByDescending(kv => kv.Value).Select(kv => $"{kv.Key} {kv.Value}")));
        }
        static Vector3 Flat3(Vector3 v) { v.y = 0; return v; }
        [Serializable] class LifeTube { public string id; public float[] pts; }
        [Serializable] class LifeExit { public string id, tube; public float[] a; }
        [Serializable] class LifeChamber { public string id, kind; public float x, z, floor, height; }
        [Serializable] class LifeNetJson { public LifeTube[] tubes; public LifeExit[] exits; public LifeChamber[] chambers; }
    }
}
