using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Rendering;

// Round 62 (Dan, 2026-10-23 02:23: "I want a track based on the green line ... I have put in pics so you can see what it looks like"): the world
// pieces of the "Noonday Creek Trail" race, from Data/tracks/noonday_creek_trail.json (Tools/mapgen/make_trail_route.py). World, not race layer:
// they stand in Free Roam too (the race is Data/routes/noonday_creek_trail.json). As Dan's photos show it (local/reference_photos/noonday_trail/,
// reference only):
//   the path       dark asphalt 3 m wide (its real width: the standing rule) with a dashed yellow center line, on a level bench with grass
//                  shoulders 11 m across (Dan: four vehicles race and pass), the bench narrower by the creek; no tree within 7.5 m of its middle
//                  (the woods past that solid as everywhere)
//   under I-575    black steel pipe railings both sides (photo 1), the path on the creek's north bank (moved off OSM's line there: the decks
//                  clear it by 6 m and more)
//   the red bridge the rust-red steel truss footbridge over Noonday Creek by Hidden Forest Court (photo 2, OSM's way 392529960): a deck 3.7 m wide
//                  between its two trusses (solid: a real chokepoint, nothing to wedge on: the trusses' insides are flat and the approaches'
//                  gray timber-and-steel rails flare out), concrete abutments
//   the boardwalk  under I-575 the creek crosses the trail's line three times (OSM's way there is a bridge): a concrete boardwalk on piers, black pipe
//                  railings both sides (check 1: the car met the creek's bank). Both are decks from the track file (their line and heights, from the
//                  trail's one height profile: Tools/mapgen/make_trail_route.py); the ground under each cut away to 1.2 m under it
//   the signs      yellow winding-road warning signs before the bends (breakable; Tools/handmade/make_trail_signs.py)
// Also here: the race's mall floors written into the embedded route (EmbedRouteHeights: the route's height codes, MapBuilder.BuildRace).
namespace ChastainRush
{
    public static partial class MapBuilder
    {
        [Serializable] class TrailRail { public string id; public int side; public float s0, s1; }
        [Serializable] class TrailSign { public float[] at, facing; }
        [Serializable] class TrailDeck { public string id, style; public float[] line, y; public float width, s0, s1; }   // style "truss" (two points) or "boardwalk" (a height per point)
        [Serializable] class TrailTread { public float[] line, underpass, tread_y; public float width; public TrailDeck[] decks; public TrailRail[] rails; public TrailSign[] signs; }
        [Serializable] class R62Line { public string id; public float[] points; public float half; }
        [Serializable] class TrailFileJson { public TrailTread trail; public R62Line[] pole_free; }
        const string TrailTrackFile = "Data/tracks/noonday_creek_trail.json";
        static Ridge TrailNow;              // its graded bench and cleared lines (the Chastain Ridge race's shape)
        static TrailFileJson TrailExtra;    // the path, its railings, the red bridge, the signs, the lots' lines kept free of light poles
        static LineSet trailPoleFree;

        // ------------------------------------------------------------ the planning: the bench graded, the ground under the red bridge kept clear
        static void PlanTrailRace(TerrainGrid terrain)
        {
            TrailNow = Ridge.Load(TrailTrackFile); trailPoleFree = null;
            string path = Path.Combine(ProjectRoot, TrailTrackFile);
            TrailExtra = File.Exists(path) ? JsonUtility.FromJson<TrailFileJson>(File.ReadAllText(path)) : null;
            if (TrailNow == null || TrailExtra?.trail == null) { Debug.Log("[Round62] no " + TrailTrackFile + ": no Noonday Creek Trail pieces"); return; }
            GradeRidge(terrain, TrailNow);
            // under each deck: the ground at least 1.2 m under it across its width and 1.5 m either side, not within 1.5 m of its ends (it meets the bench there)
            foreach (var dk in TrailExtra.trail.decks ?? new TrailDeck[0])
            {
                var p = ToPoints(dk.line); float hw = dk.width * .5f + 1.5f; int n = 0; float most = 0f; float total = 0f; for (int i = 1; i < p.Count; i++) total += Vector2.Distance(p[i - 1], p[i]);
                var area = Rect.MinMaxRect(p.Min(q => q.x) - hw, p.Min(q => q.y) - hw, p.Max(q => q.x) + hw, p.Max(q => q.y) + hw);
                EditTerrain(terrain, area, (x, z, y) =>
                {
                    if (!DeckAt(dk, new Vector2(x, z), hw, out float top, out float s)) return y;
                    if (s < 1.5f || s > total - 1.5f) return y;
                    float under = top - 1.2f; if (y <= under) return y;
                    n++; most = Mathf.Max(most, y - under); return under;
                });
                Debug.Log($"[Round62] {dk.id}: {total:0} m, its deck {dk.y.Min():0.00} to {dk.y.Max():0.00} m; {n} ground samples under it lowered (by up to {most:0.00} m) to 1.2 m under it");
            }
            Debug.Log($"[Round62] the trail: {TrailNow.Data.grades?.Length ?? 0} graded stretches");
        }
        // a deck's top over q (the nearest point of its line within 'reach'), and how far along it that is; false where q is off it
        static bool DeckAt(TrailDeck dk, Vector2 q, float reach, out float top, out float along)
        {
            var p = ToPoints(dk.line); float best = float.MaxValue, acc = 0f; top = 0f; along = 0f; float total = 0f; for (int i = 1; i < p.Count; i++) total += Vector2.Distance(p[i - 1], p[i]);
            for (int i = 0; i + 1 < p.Count; i++)
            {
                var ab = p[i + 1] - p[i]; float L = ab.magnitude; float t = Mathf.Clamp01(Vector2.Dot(q - p[i], ab) / Mathf.Max(L * L, 1e-6f)); float d = (p[i] + ab * t - q).magnitude;
                if (d < best)
                {
                    best = d; along = acc + t * L;
                    top = dk.style == "truss" ? TrussDeckY(dk.y[0], dk.y[dk.y.Length - 1], along / Mathf.Max(total, 1e-3f)) : Mathf.Lerp(dk.y[i], dk.y[i + 1], t);
                }
                acc += L;
            }
            return best <= reach && along > 0f && along < total;
        }
        static float TrussDeckY(float ya, float yb, float t) => Mathf.Lerp(ya, yb, t) + .4f * Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI);   // a truss bridge's slight camber
        static bool TrailPoleFree(Vector2 p)
        {
            if (TrailExtra?.pole_free == null) return false;
            trailPoleFree = trailPoleFree ?? new LineSet(TrailExtra.pole_free.Select(l => (l.points, l.half)));
            return trailPoleFree.OnAny(p);
        }

        // ------------------------------------------------------------ the step
        static void BuildTrailRace(TerrainGrid terrain, Materials mats, Transform world)
        {
            if (TrailNow == null || TrailExtra?.trail == null) return;
            var tr = TrailExtra.trail; var root = new GameObject("Noonday Creek Trail (Round 62)").transform; root.SetParent(world, false);
            var line = ToPoints(tr.line); var cum = new float[line.Count]; for (int i = 1; i < line.Count; i++) cum[i] = cum[i - 1] + Vector2.Distance(line[i - 1], line[i]);
            var decks = tr.decks ?? new TrailDeck[0];
            bool OnDeck(float s, float pad) => decks.Any(d => s > d.s0 + pad && s < d.s1 - pad);
            // the path (not on a deck: its own), in runs of about 96 m (chunks), and its dashed yellow center line (1.5 m dashes, 3 m gaps)
            var asphalt = new Chunks { Tint = new Color(0.20f, 0.20f, 0.21f) }; var paint = new Chunks { Tint = PaintYellow };
            var run = new List<Vector2>(); int dashes = 0;
            void Flush() { if (run.Count >= 2) { var d = Densify(run, 1f); RibbonGridMax(asphalt.At(d[d.Count / 2]), d, Enumerable.Repeat(tr.width, d.Count).ToArray(), .06f, terrain, 4); } run = new List<Vector2>(); }
            for (int i = 0; i < line.Count; i++)
            {
                if (OnDeck(cum[i], .5f)) { Flush(); continue; }
                run.Add(line[i]); if (run.Count >= 48) { var last = run[run.Count - 1]; Flush(); run.Add(last); }
            }
            Flush();
            for (float s = 2f; s < cum[cum.Length - 1] - 2f; s += 4.5f)
            {
                if (OnDeck(s, -1f)) continue;
                var p0 = PointAtS(line, cum, s); var p1 = PointAtS(line, cum, s + 1.5f); var dd = (p1 - p0).normalized; var nn = new Vector2(-dd.y, dd.x) * .06f;
                var mb = paint.At(p0); mb.Tint = PaintYellow;
                Face(mb, new Vector3(p0.x - nn.x, GroundMax(terrain, p0.x, p0.y, .45f) + .075f, p0.y - nn.y), new Vector3(p0.x + nn.x, GroundMax(terrain, p0.x, p0.y, .45f) + .075f, p0.y + nn.y),
                    new Vector3(p1.x + nn.x, GroundMax(terrain, p1.x, p1.y, .45f) + .075f, p1.y + nn.y), new Vector3(p1.x - nn.x, GroundMax(terrain, p1.x, p1.y, .45f) + .075f, p1.y - nn.y), Vector3.up);
                dashes++;
            }
            asphalt.ToObjects("Noonday trail asphalt", mats.Asphalt, root, false);
            paint.ToObjects("Noonday trail center line", mats.Paint, root, false);
            // the railings: black steel pipe (posts every 2 m, a top rail and a mid rail), 0.6 m off the path's edge; solid
            var rails = new MeshBuilder(); int railM = 0; var black = new Color(0.07f, 0.07f, 0.08f);
            if (tr.rails != null)
                foreach (var r in tr.rails)
                {
                    var pts = new List<Vector3>();
                    for (float s = r.s0; s <= r.s1 + .01f; s += 2f)
                    {
                        var p = PointAtS(line, cum, s); var a = PointAtS(line, cum, s - 1f); var b = PointAtS(line, cum, s + 1f); var t = (b - a).normalized;
                        var q = p + new Vector2(-t.y, t.x) * r.side * (tr.width * .5f + .6f); pts.Add(new Vector3(q.x, Ground(terrain, q.x, q.y), q.y));
                    }
                    for (int i = 0; i < pts.Count; i++)
                    {
                        Post(rails, pts[i] - Vector3.up * .2f, 1.27f, .07f, black);
                        if (i == 0) continue;
                        var a = pts[i - 1]; var b = pts[i];
                        Beam(rails, a + Vector3.up * 1.05f, b + Vector3.up * 1.05f, .07f, .07f, black); Beam(rails, a + Vector3.up * .55f, b + Vector3.up * .55f, .05f, .05f, black);
                        var mid = (a + b) * .5f + Vector3.up * .6f; var g = new GameObject("Trail railing"); g.transform.SetParent(root, false);
                        g.transform.SetPositionAndRotation(mid, Quaternion.LookRotation(b - a)); g.AddComponent<BoxCollider>().size = new Vector3(.15f, 1.3f, (b - a).magnitude + .05f);
                        railM += 2;
                    }
                }
            rails.ToObject("Noonday trail railings", mats.House, root, false);
            foreach (var dk in decks)
            {
                var dp = ToPoints(dk.line);
                if (dk.style == "truss") BuildRedBridge(terrain, mats, root, dp[0], dp[dp.Count - 1], dk.y[0], dk.y[dk.y.Length - 1], dk.width);
                else BuildBoardwalk(terrain, mats, root, dk);
            }
            int signs = BuildWindingSigns(terrain, mats, root, tr.signs);
            Debug.Log($"[Round62] the Noonday Creek Trail: {cum[cum.Length - 1]:0} m of path {tr.width:0.0} m wide, {dashes} yellow dashes, {railM} m of railings, the red bridge " +
                      $"{string.Join(" and ", decks.Select(d => $"{d.id} {d.s1 - d.s0:0} m"))}, {signs} winding-road signs; {TrailNow.TreesRemoved} trees kept off its bench and the lots' lines");
        }

        static float NearestS(List<Vector2> line, float[] cum, Vector2 q)
        {
            float best = float.MaxValue, bs = 0f;
            for (int i = 0; i + 1 < line.Count; i++)
            {
                var ab = line[i + 1] - line[i]; float t = Mathf.Clamp01(Vector2.Dot(q - line[i], ab) / Mathf.Max(ab.sqrMagnitude, 1e-6f)); float d = (line[i] + ab * t - q).sqrMagnitude;
                if (d < best) { best = d; bs = cum[i] + t * (cum[i + 1] - cum[i]); }
            }
            return bs;
        }
        static Vector2 PointAtS(List<Vector2> line, float[] cum, float s)
        {
            s = Mathf.Clamp(s, 0f, cum[cum.Length - 1]);
            for (int i = 0; i + 1 < line.Count; i++) if (cum[i + 1] >= s) return Vector2.Lerp(line[i], line[i + 1], (s - cum[i]) / Mathf.Max(cum[i + 1] - cum[i], 1e-6f));
            return line[line.Count - 1];
        }

        // the red truss footbridge (photo 2): a concrete-topped steel deck, two Pratt trusses whose top chords arch a little, a cross brace over the
        // ends, concrete abutments, the approaches' gray timber-and-steel rails flaring out 3 m each end; the deck is a bridge deck (BridgeDecks:
        // VehicleSurfaceContacts' ground), the trusses' insides solid and flat (box colliders the deck's length)
        static void BuildRedBridge(TerrainGrid terrain, Materials mats, Transform root, Vector2 a, Vector2 b, float ya, float yb, float width)
        {
            var go = new GameObject("Red truss footbridge").transform; go.SetParent(root, false);
            var d = (b - a).normalized; var n = new Vector2(-d.y, d.x); float L = Vector2.Distance(a, b), hw = width * .5f;
            Vector3 P(float s, float off, float up) { var q = a + d * s + n * off; return new Vector3(q.x, TrussDeckY(ya, yb, s / L) + up, q.y); }
            var deck = new MeshBuilder(); var steel = new MeshBuilder(); var conc = new MeshBuilder();
            var rust = new Color(0.47f, 0.17f, 0.10f); var rustDark = new Color(0.36f, 0.13f, 0.08f); var deckTop = new Color(0.55f, 0.54f, 0.52f); var concrete = new Color(0.66f, 0.65f, 0.61f);
            int seg = Mathf.Max(8, Mathf.CeilToInt(L / 2f));
            for (int i = 0; i < seg; i++)
            {
                float s0 = L * i / seg, s1 = L * (i + 1) / seg;
                deck.Tint = deckTop; Face(deck, P(s0, -hw, 0f), P(s0, hw, 0f), P(s1, hw, 0f), P(s1, -hw, 0f), Vector3.up);
                deck.Tint = rustDark; Face(deck, P(s0, -hw, -.6f), P(s1, -hw, -.6f), P(s1, hw, -.6f), P(s0, hw, -.6f), Vector3.down);
                foreach (float sg in new[] { -1f, 1f }) { deck.Tint = rustDark; Face(deck, P(s0, sg * hw, -.6f), P(s1, sg * hw, -.6f), P(s1, sg * hw, 0f), P(s0, sg * hw, 0f), new Vector3(n.x, 0, n.y) * sg); }
            }
            // its name starts "BridgeDecks" (VehicleSurfaceContacts takes it as a bridge deck) and is its own: a mesh is saved under its object's name, and
            // two "BridgeDecks" overwrote the map's bridge decks' mesh (Round 62 check: every road bridge without a deck to drive on)
            var dk = deck.ToObject("BridgeDecks red truss footbridge", mats.House, go, true);
            // the trusses: bottom chords along the deck's edges, top chords 2.1 m over it at the ends rising to 2.6 m, verticals every 3 m, diagonals
            int bays = Mathf.Max(4, Mathf.RoundToInt(L / 3f));
            float Top(float s) => 2.1f + .5f * Mathf.Sin(s / L * Mathf.PI);
            foreach (float sg in new[] { -1f, 1f })
            {
                float off = sg * (hw + .12f);
                for (int i = 0; i < bays; i++)
                {
                    float s0 = L * i / bays, s1 = L * (i + 1) / bays;
                    Beam(steel, P(s0, off, .05f), P(s1, off, .05f), .22f, .3f, rust);                           // bottom chord
                    Beam(steel, P(s0, off, Top(s0)), P(s1, off, Top(s1)), .22f, .22f, rust);                    // top chord
                    Beam(steel, P(s0, off, .05f), P(s0, off, Top(s0)), .16f, .16f, rust);                       // vertical
                    if (i < bays / 2) Beam(steel, P(s0, off, Top(s0)), P(s1, off, .2f), .12f, .12f, rust); else Beam(steel, P(s0, off, .2f), P(s1, off, Top(s1)), .12f, .12f, rust);   // diagonals (a Pratt truss: toward the middle)
                    Beam(steel, P(s0, off, 1.05f), P(s1, off, 1.05f), .06f, .06f, rustDark);                   // the rail inside the truss
                }
                Beam(steel, P(L, off, .05f), P(L, off, Top(L)), .16f, .16f, rust);
                // the inside: one smooth solid face the deck's length (box colliders in 6 m pieces along it)
                for (float s = 0f; s < L - .01f; s += 6f)
                {
                    float s1 = Mathf.Min(L, s + 6f); var p0 = P(s, sg * (hw + .1f), .7f); var p1 = P(s1, sg * (hw + .1f), .7f);
                    var g = new GameObject("Red bridge truss"); g.transform.SetParent(go, false);
                    g.transform.SetPositionAndRotation((p0 + p1) * .5f, Quaternion.LookRotation(p1 - p0)); g.AddComponent<BoxCollider>().size = new Vector3(.2f, 1.6f, (p1 - p0).magnitude + .1f);
                }
            }
            // cross braces over the deck at the ends (high: 2.9 m over the deck, clear of any vehicle)
            foreach (float s in new[] { 0f, L }) Beam(steel, P(s, -hw - .12f, Top(s) + .05f), P(s, hw + .12f, Top(s) + .05f), .18f, .18f, rust);
            // the abutments: concrete blocks under the ends, 1.2 m past them into the bank
            foreach (var (s, dir) in new[] { (0f, -1f), (L, 1f) })
            {
                var c = P(s, 0f, 0f); var f = MakeFrame(c, d * dir, 1f); float down = c.y - Mathf.Min(Ground(terrain, (a + d * s).x, (a + d * s).y), c.y - .3f) + 1.5f;
                BoxF(conc, f, -hw - .6f, hw + .6f, -down, -.02f, -2.4f, 1.2f, concrete);
            }
            // the approaches: gray timber-and-steel rails, 4 m along each end, flaring out 1.2 m (nothing square to hit), solid
            var timber = new MeshBuilder(); var gray = new Color(0.62f, 0.62f, 0.60f); var post = new Color(0.55f, 0.56f, 0.57f);
            foreach (var (s, dir) in new[] { (0f, -1f), (L, 1f) })
                foreach (float sg in new[] { -1f, 1f })
                {
                    var e0 = a + d * s + n * sg * (hw + .15f); var e1 = a + d * (s + dir * 4f) + n * sg * (hw + 1.35f);
                    var y0 = TrussDeckY(ya, yb, s / L); var y1 = Ground(terrain, e1.x, e1.y);
                    var p0 = new Vector3(e0.x, y0, e0.y); var p1 = new Vector3(e1.x, y1, e1.y);
                    foreach (var p in new[] { p0, Vector3.Lerp(p0, p1, .5f), p1 }) Post(timber, p - Vector3.up * .3f, 1.35f, .16f, post);
                    foreach (float h in new[] { .45f, .75f, 1.0f }) Beam(timber, p0 + Vector3.up * h, p1 + Vector3.up * h, .08f, .14f, gray);
                    var g = new GameObject("Red bridge approach rail"); g.transform.SetParent(go, false);
                    g.transform.SetPositionAndRotation((p0 + p1) * .5f + Vector3.up * .6f, Quaternion.LookRotation(p1 - p0)); g.AddComponent<BoxCollider>().size = new Vector3(.2f, 1.3f, (p1 - p0).magnitude);
                }
            steel.ToObject("Red bridge trusses", mats.House, go, false);
            conc.ToObject("Red bridge abutments", mats.House, go, true);
            timber.ToObject("Red bridge approach rails", mats.House, go, false);
            Debug.Log($"[Round62] the red truss footbridge: {L:0.0} m from ({a.x:0.0}, {a.y:0.0}) {ya:0.00} m to ({b.x:0.0}, {b.y:0.0}) {yb:0.00} m, {width:0.0} m between its trusses, {bays} bays");
        }

        // the boardwalk under I-575 (photo 1's black pipe railings): a concrete deck 0.45 m thick along its line at its heights, square concrete piers every
        // 6 m down to the ground (outside the vehicles' way: under the deck), the railings along both edges (solid: box colliders 2 m long)
        static void BuildBoardwalk(TerrainGrid terrain, Materials mats, Transform root, TrailDeck dk)
        {
            var go = new GameObject(dk.id).transform; go.SetParent(root, false);
            var p = ToPoints(dk.line); float hw = dk.width * .5f; var deck = new MeshBuilder(); var conc = new MeshBuilder(); var rails = new MeshBuilder();
            var top = new Color(0.60f, 0.60f, 0.58f); var side = new Color(0.52f, 0.52f, 0.50f); var black = new Color(0.07f, 0.07f, 0.08f);
            Vector2 N(int i) { var a = p[Mathf.Max(0, i - 1)]; var b = p[Mathf.Min(p.Count - 1, i + 1)]; var t = (b - a).normalized; return new Vector2(-t.y, t.x); }
            Vector3 V(int i, float off, float up) { var q = p[i] + N(i) * off; return new Vector3(q.x, dk.y[i] + up, q.y); }
            for (int i = 0; i + 1 < p.Count; i++)
            {
                deck.Tint = top; Face(deck, V(i, -hw, 0f), V(i, hw, 0f), V(i + 1, hw, 0f), V(i + 1, -hw, 0f), Vector3.up);
                deck.Tint = side; Face(deck, V(i, -hw, -.45f), V(i + 1, -hw, -.45f), V(i + 1, hw, -.45f), V(i, hw, -.45f), Vector3.down);
                foreach (float sg in new[] { -1f, 1f }) { var nn = N(i) * sg; Face(deck, V(i, sg * hw, -.45f), V(i + 1, sg * hw, -.45f), V(i + 1, sg * hw, 0f), V(i, sg * hw, 0f), new Vector3(nn.x, 0, nn.y)); }
            }
            deck.ToObject("BridgeDecks " + dk.id, mats.House, go, true);   // a bridge deck to VehicleSurfaceContacts (its own name: see BuildRedBridge)
            float acc = 0f, nextPier = 3f;
            for (int i = 0; i + 1 < p.Count; i++)
            {
                float L = Vector2.Distance(p[i], p[i + 1]);
                while (nextPier <= acc + L)
                {
                    float t = (nextPier - acc) / Mathf.Max(L, 1e-4f); var c = Vector2.Lerp(p[i], p[i + 1], t); float y = Mathf.Lerp(dk.y[i], dk.y[i + 1], t) - .45f; float g = Ground(terrain, c.x, c.y) - .3f;
                    if (y - g > .2f) foreach (float sg in new[] { -1f, 1f }) { var q = c + N(i) * sg * (hw - .4f); SBox(conc, new Vector3(q.x, g, q.y), Vector3.forward, new Vector3(.4f, y - g, .4f), side); }
                    nextPier += 6f;
                }
                acc += L;
            }
            conc.ToObject("Boardwalk piers", mats.House, go, false);
            foreach (float sg in new[] { -1f, 1f })
            {
                var pts = new List<Vector3>(); for (int i = 0; i < p.Count; i++) pts.Add(V(i, sg * (hw - .08f), 0f));
                for (int i = 0; i < pts.Count; i++)
                {
                    if (i % 1 == 0) Post(rails, pts[i] - Vector3.up * .05f, 1.12f, .06f, black);
                    if (i == 0) continue;
                    var a = pts[i - 1]; var b = pts[i];
                    Beam(rails, a + Vector3.up * 1.05f, b + Vector3.up * 1.05f, .07f, .07f, black); Beam(rails, a + Vector3.up * .55f, b + Vector3.up * .55f, .05f, .05f, black);
                    var g2 = new GameObject("Boardwalk railing"); g2.transform.SetParent(go, false);
                    g2.transform.SetPositionAndRotation((a + b) * .5f + Vector3.up * .6f, Quaternion.LookRotation(b - a)); g2.AddComponent<BoxCollider>().size = new Vector3(.15f, 1.3f, (b - a).magnitude + .05f);
                }
            }
            rails.ToObject("Boardwalk railings", mats.House, go, false);
            Debug.Log($"[Round62] {dk.id}: a boardwalk {dk.width:0.0} m wide along {p.Count} points, {dk.y.Min():0.00} to {dk.y.Max():0.00} m");
        }

        // the yellow winding-road warning signs (Dan's photos 1 and 2): a diamond on a post, breakable (they snap as the curve signs do)
        static int BuildWindingSigns(TerrainGrid terrain, Materials mats, Transform root, TrailSign[] signs)
        {
            if (signs == null || signs.Length == 0) return 0;
            var go = new GameObject("Winding road signs").transform; go.SetParent(root, false);
            var br = new Breakables(); var steel = new Color(0.55f, 0.57f, 0.58f); var back = new Color(0.62f, 0.63f, 0.64f); var md = RoadSignMaterial("WindingRoad");
            const float Side = 0.9f, DiamondY = 2.2f; int n = 0;
            foreach (var s in signs)
            {
                var at = new Vector2(s.at[0], s.at[1]); float gy = Ground(terrain, at.x, at.y); var face = new Vector3(s.facing[0], 0f, s.facing[1]).normalized;
                var mb = br.Begin(at); var foot = new Vector3(at.x, gy - .3f, at.y);
                br.Piece(() => Post(mb, foot, DiamondY + .3f + .1f, .07f, steel), gy + .05f);
                br.Piece(() =>
                {
                    var f = MakeFrame(new Vector3(at.x, gy, at.y), new Vector2(face.x, face.z), 1f); float hd = Side * .7071f; mb.Tint = back;
                    Face(mb, f.P(0f, DiamondY - hd, -.05f), f.P(hd, DiamondY, -.05f), f.P(0f, DiamondY + hd, -.05f), f.P(-hd, DiamondY, -.05f), -face);
                }, gy + .05f);
                if (md != null) br.Face(DiamondPlate(go, new Vector3(at.x, gy + DiamondY, at.y) + face * .01f, -face, Side, md, "Winding road sign"));
                br.End(new Vector3(at.x, gy + 1.3f, at.y), face, .9f, 2.6f, .5f); n++;
            }
            br.ToObjects("Winding road signs", mats.House, go, 1f, BreakSounds.Kind.Sign);
            return n;
        }

        // ------------------------------------------------------------ the route's height codes (RouteJson.loop_y / loop_rev_y) made real
        // Tools/mapgen/make_trail_route.py writes, where the lap runs on a building's floors, -(20000 + 1000 k) + offset: k 0 the mall's lower floor,
        // 1 its upper floor, 2 the east escalator bank's surface under that point (MallPlan), 3 the red footbridge's deck, 4 the deck of the road bridge
        // under that point (map.json's bridges as the planning left them); RouteJson.CodeGround stays (the world's ground)
        static string EmbedRouteHeights(string json, string file, MapData map)
        {
            if (string.IsNullOrEmpty(json) || !json.Contains("-2")) return json;
            RouteJson j; try { j = JsonUtility.FromJson<RouteJson>(json); } catch { return json; }
            int done = 0, decks = 0;
            string Fix(string text, string key, float[] loop, float[] ys)
            {
                if (ys == null || loop == null || ys.Length * 2 != loop.Length || !ys.Any(v => v < -15000f)) return text;
                if (Mall == null) { Debug.LogWarning($"[Round62] {file}: {key} has the mall's floor codes but there is no mall: its points take the ground"); }
                var outY = new float[ys.Length];
                for (int i = 0; i < ys.Length; i++)
                {
                    float v = ys[i]; if (v > -15000f) { outY[i] = v; continue; }
                    int k = Mathf.RoundToInt((-v - 20000f) / 1000f); float off = v + 20000f + 1000f * k;
                    var p = new Vector2(loop[2 * i], loop[2 * i + 1]);
                    if (k == 3) { float ty = TrailDeckTop(p); outY[i] = float.IsNaN(ty) ? RouteJson.CodeGround : ty + off; decks++; continue; }
                    if (k == 4) { float dy = RoadDeckTop(map, p); outY[i] = float.IsNaN(dy) ? RouteJson.CodeGround : dy + off; decks++; continue; }
                    if (Mall == null) { outY[i] = RouteJson.CodeGround; continue; }
                    outY[i] = (k == 0 ? Mall.L0 : k == 1 ? Mall.L1 : MallRampSurface(Mall, p)) + off; done++;
                }
                var arr = "[" + string.Join(",", outY.Select(v => v.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture))) + "]";
                return Regex.Replace(text, "\"" + key + "\"\\s*:\\s*\\[[^\\]]*\\]", "\"" + key + "\": " + arr);
            }
            json = Fix(json, "loop_y", j.loop, j.loop_y);
            json = Fix(json, "loop_rev_y", j.loop_rev, j.loop_rev_y);
            if (done > 0 || decks > 0) Debug.Log($"[Round62] {file}: {done} of its lap points on Town Center's floors (lower {Mall?.L0 ?? 0f:0.00} m, upper {Mall?.L1 ?? 0f:0.00} m, the east escalator), {decks} on bridge decks");
            return json;
        }
        // the trail's deck top under p (the red bridge, the boardwalk), NaN where there is none
        static float TrailDeckTop(Vector2 p)
        {
            if (TrailExtra?.trail?.decks == null) return float.NaN;
            foreach (var dk in TrailExtra.trail.decks) if (DeckAt(dk, p, dk.width * .5f + 1f, out float top, out _)) return top;
            return float.NaN;
        }
        // a road bridge's deck top under p (the nearest deck within its half width and 1 m), NaN where there is none
        static float RoadDeckTop(MapData map, Vector2 p)
        {
            float best = float.MaxValue, y = float.NaN;
            foreach (var br in map.bridges)
            {
                var q = ToPoints(br.points); if (q.Count < 2 || br.deck_y == null || br.deck_y.Length != q.Count) continue;
                float half = br.width * .5f + 1f, lift = (RoadLift.TryGetValue(br.@class, out var l) ? l : .06f) * vScale;
                for (int i = 0; i + 1 < q.Count; i++)
                {
                    var ab = q[i + 1] - q[i]; float t = Mathf.Clamp01(Vector2.Dot(p - q[i], ab) / Mathf.Max(ab.sqrMagnitude, 1e-6f)); float d = (q[i] + ab * t - p).magnitude;
                    if (d < half && d < best) { best = d; y = Mathf.Lerp(br.deck_y[i], br.deck_y[i + 1], t) + lift; }
                }
            }
            return y;
        }
        // the east escalator bank's surface at p (its profile along u from its foot; the floor past its ends)
        static float MallRampSurface(MallPlan m, Vector2 p)
        {
            var rp = m.Ramps.FirstOrDefault(r => r.name == "east"); if (rp == null) return m.L1;
            float s = (m.UV(p).x - rp.uFoot) * rp.dir;
            if (s <= 0f) return m.L0; if (s >= rp.run) return m.L0 + rp.prof[rp.prof.Count - 1].y;
            return m.L0 + Interp(rp.prof, s);
        }
    }
}
