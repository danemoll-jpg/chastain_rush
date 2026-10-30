using System;
using System.Collections.Generic;
using System.Linq;
using Racer;
using UnityEngine;

namespace ChastainRush
{
    // Round 84 (A3): Racer 1.2's HighBumpers (gutter bumpers, like a bowling alley's), ported. In a HIGH MODE car race (never a normal race, Free
    // Roam, an air race or the Underground race in its pipes) a low rounded red and white bumper (about 1 m, with an unseen collision fence above
    // its road side up to 2.2 m so nothing rides up and over it) runs along both edges of the course line and of every official shortcut, so a
    // vehicle that drifts to the edge bounces back onto the road. Built once per course and direction at its first HIGH MODE start, from the race's
    // own lines (the lap and the shortcuts as the AI drives them: RaceController.AiRoad / AiShortcuts), shown only while a HIGH MODE race runs.
    // - It stands at the road's edge: the first point out from the line where the ground drops away (a cliff, a bench's edge), else just past the
    //   paved width (the map's road half width there, RoadLookup; a trail's or a shortcut's own) + 1.2 m.
    // - Openings wherever another drivable line crosses or joins: a shortcut's mouth, the same course passing again, any of the map's roads (a junction,
    //   a road under a bridge), so no road, shortcut or crossing is closed.
    // - On a landing (Chastain Rush: the map's landing zones and catch slopes, LandingZone) the bumpers stand wider apart, at least LandingHalf from the
    //   line, closing back to the road's edge over LandingTaper metres past the zone (Racer 1.3.0-review2's funnel).
    // - Chastain Rush: the line's heights are the ground's (or a floor's / the route's own): where there is no ground within 1.6 m of the line (a deck
    //   high over it, a ramp's top) no bumper is put, so a jump flies as before and a bridge deck keeps its own parapets.
    // - Solid and drawn only for a HIGH MODE player (split screen: the HIGH player's half only, the other player passes through); the AI rivals pass
    //   through them (their racing line uses the shoulders). On the "Ignore Raycast" layer (split screen: a view's own layer): the suspension and the
    //   cameras do not take them for ground.
    public static class HighBumpers
    {
        const string RootName = "HIGH MODE bumpers";
        public const int Layer = 2;
        public static int Samples { get; private set; }          // checks: bumper stations built
        public static int Openings { get; private set; }         // checks: stations left open for a crossing line
        public static int LandingStations { get; private set; }  // checks: stations on a landing, set wider apart
        public static string Report => $"{Samples} bumper stations, {Openings} open for crossings, {LandingStations} on landings";
        public static bool Shown => current && current.activeSelf;
        static PhysicsMaterial bounce;
        static readonly Dictionary<string, GameObject> built = new();
        static GameObject current;

        public static void Hide() { if (current && current.activeSelf) { current.SetActive(false); Physics.SyncTransforms(); } }
        public static void Apply(RaceController race)
        {
            Hide();
            if (!race || !race.High || race.Route == null || race.Route.Air || race.Route.Underground || race.AiRoad == null) return;
            string key = race.CurrentFile + "|" + race.Route.Direction;
            if (!built.TryGetValue(key, out var root) || !root) built[key] = root = Build(race);
            current = root; root.SetActive(true); PerPlayer(race, root); Physics.SyncTransforms();
        }
        // split screen with one player in HIGH MODE: drawn only in that player's half (the split views' own layers), the other player's vehicle passes
        // through; the AI rivals always pass through
        static void PerPlayer(RaceController race, GameObject root)
        {
            bool split = SplitScreen.Active, h1 = race.HighFor(1), h2 = race.HighFor(2);
            int layer = !split || (h1 && h2) ? Layer : h1 ? SplitSession.View1Layer : SplitSession.View2Layer;
            foreach (Transform t in root.transform) t.gameObject.layer = layer;
            var bumpers = root.GetComponentsInChildren<MeshCollider>(true);
            void Pass(ArcadeVehicle car, bool through) { if (!car) return; foreach (var c in car.GetComponentsInChildren<Collider>(true)) foreach (var b in bumpers) Physics.IgnoreCollision(c, b, through); }
            foreach (var e in race.Entrants)
            {
                if (!e.Car) continue;
                bool human = e.IsPlayer || (e.Seat == 2 && e.IsHuman);
                Pass(e.Car, !human || (e.IsPlayer ? !h1 : !h2));
            }
        }
        public static bool IsBumper(Collider c) => c && c.transform.parent && c.transform.parent.name.StartsWith(RootName);

        sealed class Line { public Func<float, (Vector3 p, Vector3 f)> at; public Func<float, Vector3, float> half; public float length; public bool loop; public bool walls; }
        struct Band { public Vector3 p; public float half, s; public int line; }
        const float Cell = 16, Step = 2;
        public const float LandingHalf = 11, LandingTaper = 20;
        static long Key(int x, int z) => ((long)x << 32) ^ (uint)z;

        static GameObject Build(RaceController race)
        {
            Samples = Openings = LandingStations = 0;
            var root = new GameObject(RootName + " " + race.Route.Name + " " + race.Route.Direction) { layer = Layer };
            var roads = RoadLookup.Instance; var rd = race.RouteData; bool trail = rd != null && rd.trail;
            var lines = new List<Line>();
            var main = race.AiRoad;
            // the lap's paved half width: the map's road under the line, else the lap's own (a trail's, a lot, the mall)
            float LapHalf(float s, Vector3 p)
            {
                float own = main.HalfWidth(s);
                if (trail || !roads || !roads.Nearest(p, out _, out float d, out float h) || d > h + .5f) return own;
                return Mathf.Clamp(h, 2.5f, 12f);
            }
            lines.Add(new Line { at = s => { var p = main.At(s, out var f); return (p, f); }, half = LapHalf, length = main.Length, loop = true, walls = true });
            foreach (var sc in race.AiShortcuts)
            {
                var r = sc.Line; if (!r || r.points == null || r.points.Length < 2) continue;
                lines.Add(new Line { at = s => { var p = r.At(s, out var f); return (p, f); }, half = (_, _) => 3.5f, length = r.Length, walls = true });
            }

            // every line's driven band (centre, half width), for the openings
            var grid = new Dictionary<long, List<Band>>();
            for (int li = 0; li < lines.Count; li++)
            {
                var l = lines[li]; if (l.length <= 0) continue;
                for (float s = 0; s < l.length; s += Step)
                {
                    var (p, _) = l.at(s); long k = Key(Mathf.FloorToInt(p.x / Cell), Mathf.FloorToInt(p.z / Cell));
                    if (!grid.TryGetValue(k, out var list)) grid[k] = list = new List<Band>(); list.Add(new Band { p = p, half = l.half(s, p), s = s, line = li });
                }
            }
            bool Open(Vector3 w, int line, float s, Vector3 centre, Vector3 outward)
            {
                var l = lines[line]; int cx = Mathf.FloorToInt(w.x / Cell), cz = Mathf.FloorToInt(w.z / Cell);
                for (int x = cx - 1; x <= cx + 1; x++) for (int z = cz - 1; z <= cz + 1; z++)
                    {
                        if (!grid.TryGetValue(Key(x, z), out var list)) continue;
                        foreach (var b in list)
                        {
                            if (Mathf.Abs(b.p.y - w.y) > 4) continue;
                            float dx = b.p.x - w.x, dz = b.p.z - w.z; float reach = Mathf.Max(b.half - .3f, 2.5f); if (dx * dx + dz * dz > reach * reach) continue;
                            if (b.line != line) { if (Mathf.Abs(Vector3.Dot(Vector3.ProjectOnPlane(b.p - centre, Vector3.up), outward)) < 1.5f) continue; return true; }
                            float along = Mathf.Abs(b.s - s); if (l.loop) along = Mathf.Min(along, l.length - along);
                            if (along > 30) return true;   // the same line passing this way again (a hairpin's other leg, a second pass)
                        }
                    }
                // Chastain Rush: any of the map's roads here (a junction, a side road, a road under a bridge): open
                if (roads && roads.Nearest(w, out _, out float d, out float h) && d < h - .3f) return true;
                return false;
            }

            int mask = race.PlayerCar ? race.PlayerCar.groundMask.value : 1;
            var hits = new RaycastHit[16];
            bool Surface(Vector3 q, float nearY, out float y)
            {
                y = 0; int n = Physics.RaycastNonAlloc(new Vector3(q.x, nearY + 2.5f, q.z), Vector3.down, hits, 6, mask, QueryTriggerInteraction.Ignore);
                float best = float.MaxValue;
                for (int i = 0; i < n; i++) { var h = hits[i]; if (h.collider.attachedRigidbody || h.normal.y < .5f) continue; float d = Mathf.Abs(h.point.y - nearY); if (d < best) { best = d; y = h.point.y; } }
                return best < float.MaxValue;
            }
            // a bumper already standing here from another line or another pass of this one: not built again
            var placed = new Dictionary<long, List<(Vector3 w, int owner, float s)>>();
            bool Duplicate(Vector3 w, int owner, float s, float length)
            {
                int cx = Mathf.FloorToInt(w.x / 4), cz = Mathf.FloorToInt(w.z / 4);
                for (int x = cx - 1; x <= cx + 1; x++) for (int z = cz - 1; z <= cz + 1; z++)
                        if (placed.TryGetValue(Key(x, z), out var l)) foreach (var o in l)
                            {
                                if ((o.w - w).sqrMagnitude >= 1.2f) continue;
                                float along = Mathf.Abs(o.s - s); along = Mathf.Min(along, length - along);
                                if (o.owner != owner || along > 30) return true;
                            }
                return false;
            }
            void Place(Vector3 w, int owner, float s) { long k = Key(Mathf.FloorToInt(w.x / 4), Mathf.FloorToInt(w.z / 4)); if (!placed.TryGetValue(k, out var l)) placed[k] = l = new List<(Vector3, int, float)>(); l.Add((w, owner, s)); }

            // each walled line's landings (Chastain Rush: inside a landing zone, then a taper): 1 inside, falling to 0 over LandingTaper metres past it
            var widen = new Dictionary<int, float[]>();
            for (int li = 0; li < lines.Count; li++)
            {
                var l = lines[li]; if (!l.walls || l.length <= 0) continue; int count = Mathf.FloorToInt(l.length / Step) + 1; var w = new float[count];
                float since = float.MaxValue;
                for (int pass = 0; pass < (l.loop ? 2 : 1); pass++)
                    for (int i = 0; i < count; i++)
                    {
                        var (p, _) = l.at(Mathf.Min(i * Step, l.length - .01f));
                        if (LandingZone.Contains(p)) since = 0; else if (since < float.MaxValue) since += Step;
                        w[i] = since <= 0 ? 1 : since <= LandingTaper ? 1 - since / LandingTaper : 0;
                    }
                widen[li] = w;
            }
            float Widen(int line, float s) { if (!widen.TryGetValue(line, out var w)) return 0; return w[Mathf.Clamp(Mathf.FloorToInt(s / Step), 0, w.Length - 1)]; }

            var material = BumperMaterial();
            if (!bounce) bounce = new PhysicsMaterial("HIGH MODE bumper") { bounciness = .3f, bounceCombine = PhysicsMaterialCombine.Maximum, dynamicFriction = .05f, staticFriction = .05f, frictionCombine = PhysicsMaterialCombine.Minimum };
            int chunk = 0;
            for (int li = 0; li < lines.Count; li++)
            {
                var l = lines[li]; if (!l.walls || l.length <= 0) continue;
                for (int side = -1; side <= 1; side += 2)
                {
                    var strip = new List<(Vector3 w, Vector3 o)>(); int count = Mathf.FloorToInt(l.length / Step);
                    for (int i = 0; i <= count + (l.loop ? 1 : 0); i++)
                    {
                        float s = l.loop ? Mathf.Repeat(i * Step, l.length) : Mathf.Min(i * Step, l.length - .01f);
                        int owner = li * 2 + (side > 0 ? 1 : 0);
                        var ok = Sample(l, li, s, side, i > count, owner, out var w, out var o);
                        if (ok && strip.Count > 0 && (strip[^1].w - w).magnitude > 7) ok = false;
                        if (ok) { strip.Add((w, o)); if (i <= count) { Place(w, owner, s); Samples++; } }
                        if ((!ok || strip.Count >= 40 || i == count + (l.loop ? 1 : 0)) && strip.Count >= 2) { Emit(root.transform, strip, chunk++, material); var last = strip[^1]; strip.Clear(); if (ok) strip.Add(last); }
                        else if (!ok) strip.Clear();
                    }
                }
            }
            Debug.Log($"[High] bumpers for {race.Route.Name} {race.Route.Direction}: {Report}, {chunk} pieces");
            return root;

            bool Sample(Line l, int line, float s, int side, bool wrap, int owner, out Vector3 w, out Vector3 outward)
            {
                w = outward = default; var (p, f) = l.at(s); f = Vector3.ProjectOnPlane(f, Vector3.up); if (f.sqrMagnitude < 1e-4f) return false; f.Normalize();
                if (!Surface(p, p.y, out float roadY) || Mathf.Abs(roadY - p.y) > 1.6f) return false;   // no ground at the line: a gap, a deck over it
                outward = Vector3.Cross(Vector3.up, f) * side; float half = l.half(s, p), wide = Widen(line, s), reach = Mathf.Lerp(half + 1.2f, Mathf.Max(half + 1.2f, LandingHalf), wide), edge = reach;
                if (reach > half + 1.25f) LandingStations++;
                for (float d = 2.5f; d <= reach; d += .5f)
                    if (!Surface(p + outward * d, roadY, out float y) || y < roadY - 1f) { edge = Mathf.Max(2.2f, d - .3f); break; }   // the ground drops away here
                w = p + outward * edge; w.y = roadY;
                if (Open(w, line, s, p, outward)) { Openings++; return false; }
                return wrap || !Duplicate(w, owner, s, l.length);
            }
        }
        // the bumpers' red and white come from the vertex colors: the game's vertex-lit shader
        static Material BumperMaterial()
        {
            var shader = Shader.Find("ChastainRush/VertexLit") ?? Shader.Find("Universal Render Pipeline/Simple Lit") ?? Shader.Find("Universal Render Pipeline/Lit");
            return shader ? new Material(shader) { name = "HIGH MODE bumper" } : null;
        }
        // the rounded profile (out from the road's edge, height above the road), buried below the road so it meets uneven ground
        static readonly Vector2[] Profile = { new(0, -1.4f), new(0, .5f), new(.1f, .76f), new(.28f, .92f), new(.5f, .98f), new(.72f, .92f), new(.9f, .76f), new(1f, .5f), new(1f, -1.4f) };
        const float Fence = 2.2f;   // the unseen fence on the road side, so a vehicle hitting at an angle cannot ride up and over
        static readonly Color Red = new(.86f, .16f, .12f, 0), White = new(.95f, .94f, .9f, 0);   // alpha 0: the vertex-lit shader's sway (trees) off
        static void Emit(Transform root, List<(Vector3 w, Vector3 o)> strip, int index, Material material)
        {
            var go = new GameObject("Bumper " + index) { layer = Layer }; go.transform.SetParent(root, false);
            var origin = strip[0].w; var verts = new List<Vector3>(); var colors = new List<Color>(); var tris = new List<int>();
            int P = Profile.Length;
            for (int i = 0; i + 1 < strip.Count; i++)
            {
                var c = (index * 40 + i) / 3 % 2 == 0 ? Red : White; int start = verts.Count;
                for (int k = 0; k < 2; k++) { var (w, o) = strip[i + k]; foreach (var q in Profile) { verts.Add(w + o * q.x + Vector3.up * q.y - origin); colors.Add(c); } }
                var (w0, o0) = strip[i]; var centre = w0 + o0 * .5f + Vector3.up * -.3f - origin;
                for (int j = 0; j + 1 < P; j++)
                {
                    int a = start + j, b = start + j + 1, a2 = start + P + j, b2 = start + P + j + 1;
                    var n = Vector3.Cross(verts[b] - verts[a], verts[a2] - verts[a]); var m = (verts[a] + verts[b]) * .5f - centre;
                    if (Vector3.Dot(n, m) >= 0) tris.AddRange(new[] { a, b, a2, b, b2, a2 }); else tris.AddRange(new[] { a, a2, b, b, a2, b2 });
                }
            }
            foreach (int end in new[] { 0, strip.Count - 1 })
            {
                var (w, o) = strip[end];
                for (int face = 0; face < 2; face++)
                {
                    int start = verts.Count; verts.Add(w + o * .5f + Vector3.up * -.2f - origin); colors.Add(White);
                    foreach (var q in Profile) { verts.Add(w + o * q.x + Vector3.up * q.y - origin); colors.Add(White); }
                    for (int j = 0; j < P; j++) { int a = start + 1 + j, b = start + 1 + (j + 1) % P; if (face == 0) tris.AddRange(new[] { start, a, b }); else tris.AddRange(new[] { start, b, a }); }
                }
            }
            go.transform.position = origin;
            var mesh = new Mesh { name = "HIGH MODE bumper" }; mesh.indexFormat = verts.Count > 65000 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;
            mesh.SetVertices(verts); mesh.SetColors(colors); mesh.SetTriangles(tris, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>(); if (material) renderer.sharedMaterial = material; renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var cv = new List<Vector3>(verts); var ct = new List<int>(tris);
            for (int i = 0; i + 1 < strip.Count; i++)
            {
                int k = cv.Count; var (wa, _) = strip[i]; var (wb, _) = strip[i + 1];
                cv.Add(wa + Vector3.up * .3f - origin); cv.Add(wa + Vector3.up * Fence - origin); cv.Add(wb + Vector3.up * .3f - origin); cv.Add(wb + Vector3.up * Fence - origin);
                ct.AddRange(new[] { k, k + 1, k + 2, k + 2, k + 1, k + 3, k, k + 2, k + 1, k + 2, k + 3, k + 1 });
            }
            var solid = new Mesh { name = "HIGH MODE bumper collision" }; solid.indexFormat = cv.Count > 65000 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;
            solid.SetVertices(cv); solid.SetTriangles(ct, 0); solid.RecalculateBounds();
            var collider = go.AddComponent<MeshCollider>(); collider.sharedMesh = solid; collider.sharedMaterial = bounce;
        }
    }
}
