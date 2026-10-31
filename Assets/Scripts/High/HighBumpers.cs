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
    // - Round 86 (A, Dan: "the first jump i keep clipping a bumper which spins me out. Lets leave more space for bumpers at the end of ramps"): every
    //   jump on a walled line is found from the driven surface along it (the line and 3 m either side, Profile): a flight is simulated from every
    //   station at every speed from 12 m/s to what the course allows there (its bends, the acceleration out of them, the fastest vehicle); where the ground falls
    //   away under it and it clears the ground by JumpClear or more for JumpAir or longer, no bumper stands from PreLip before the lip to RunOut past the furthest landing. Past that the bumpers come back in a
    //   funnel, FunnelOut further out, closing to the road's edge over FunnelLength. Every bumper's ends are rounded (a half dome, Emit), never square.
    public static class HighBumpers
    {
        const string RootName = "HIGH MODE bumpers";
        public const int Layer = 2;
        public static int Samples { get; private set; }          // checks: bumper stations built
        public static int Openings { get; private set; }         // checks: stations left open for a crossing line
        public static int LandingStations { get; private set; }  // checks: stations on a landing, set wider apart
        public static int JumpStations { get; private set; }     // Round 86: stations left open for a jump's flight and landing
        public static int OpenLines, OpenRoads, Stubs, Curved;   // Round 87: stations open for another line's band / another road; stub runs left out; end stations curved out
        public static bool Old;   // Round 87 checks (-r87Old): Round 86's layout (the before pictures)
        public static string Report => $"{Samples} bumper stations, {Openings} open for crossings ({OpenLines} other lines, {OpenRoads} other roads), {LandingStations} on landings, {JumpStations} open for {Jumps.Count} jumps, {Stubs} stubs left out, {Curved} end stations curved out";
        // Round 86 (A): the jumps found at the last build (checks: the line, the lip's and the furthest landing's station, the longest flight, the highest)
        public sealed class Jump { public int line; public float lip, land, air, clear, over; public Vector3 lipAt, landAt; public Func<float, (Vector3 p, Vector3 f)> at; public float length; public bool loop; }
        public static readonly List<Jump> Jumps = new List<Jump>();
        public static string DumpTo;   // checks: where the probed profiles go
        public const float PreLip = 10,   // Round 87: was 6 (the Lyndon trailer's lip)
                           RunOut = 40, FunnelLength = 30, FunnelOut = 8, JumpClear = .8f, JumpAir = .4f, SideOffset = 3, BendGrip = 20f, Accel = 12f, CrossClear = 7f;
        // Round 87 (A): another line's band clear by this past its paved edge; runs shorter than MinRun left out; the distance out changes at most MaxSlope
        // a station (10 degrees); ends curve out by EndCurve over EndLength; under another line's flight the clear widens by FlightCone a meter along it
        public const float OtherClear = 3.5f, MinRun = 10f, MaxSlope = .35f, EndLength = 12f, EndCurve = 2.5f, FlightCone = .15f;
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

        sealed class Line { public Func<float, (Vector3 p, Vector3 f)> at; public Func<float, Vector3, float> half; public float length; public bool loop; public bool walls; public Func<float, float> curv; }
        struct Band { public Vector3 p; public float half, s; public int line; }
        const float Cell = 16, Step = 2;
        public const float LandingHalf = 11, LandingTaper = 20;
        static long Key(int x, int z) => ((long)x << 32) ^ (uint)z;

        static GameObject Build(RaceController race)
        {
            Samples = Openings = LandingStations = JumpStations = 0; OpenLines = OpenRoads = Stubs = Curved = 0; Jumps.Clear();
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
            lines.Add(new Line { at = s => { var p = main.At(s, out var f); return (p, f); }, half = LapHalf, length = main.Length, loop = true, walls = true, curv = main.LineCurvature });
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
                            // Round 87 (A, Dan's BUG-002 "bumpers shouldn't cross each other", BUG-009 to 011 "most are not needed"): another line's band (a
                            // shortcut's mouth, the course's other carriageway, a second pass) keeps every bumper OtherClear past its paved edge, so no run
                            // crosses another line or stands on a median between the course's own lanes (was: only inside its paved half)
                            float dx = b.p.x - w.x, dz = b.p.z - w.z; float reach = Old ? Mathf.Max(b.half - .3f, 2.5f) : b.half + OtherClear; if (!Old && b.line == line) reach = Mathf.Max(reach, new Vector2(w.x - centre.x, w.z - centre.z).magnitude); if (dx * dx + dz * dz > reach * reach) continue;
                            if (b.line != line) { if (Mathf.Abs(Vector3.Dot(Vector3.ProjectOnPlane(b.p - centre, Vector3.up), outward)) < 1.5f) continue; OpenLines++; return true; }
                            float along = Mathf.Abs(b.s - s); if (l.loop) along = Mathf.Min(along, l.length - along);
                            if (along > 30) { OpenLines++; return true; }   // the same line passing this way again (a hairpin's other leg, a second pass)
                            // Round 87 (A, BUG-001's inside corner): on the inside of a bend tighter than the bumper's distance out, the offset folds back
                            // across itself: a bumper nearer to another station of its own line than to its own is left out
                            if (!Old && along > 2.5f) { float own = new Vector2(w.x - centre.x, w.z - centre.z).magnitude; if (Mathf.Sqrt(dx * dx + dz * dz) < own - .6f) { OpenLines++; return true; } }
                        }
                    }
                // Chastain Rush: any of the map's roads here (a junction, a side road, a road under a bridge): open. Round 87 (A, Dan's BUG-001 "why is
                // there no bumper on this corner?"): not the course's own road (on the outside of a corner the bumper stood on the same road's pavement
                // past the line's cut, and was left out): only another road (a closed side road has its barrier across its mouth)
                if (roads && roads.Nearest(w, out var name, out float d, out float h) && d < h - .3f)
                {
                    if (Old || !roads.Nearest(centre, out var own, out float dc, out float hc) || dc > hc + .5f || own != name) { OpenRoads++; return true; }
                }
                return false;
            }

            int mask = race.PlayerCar ? race.PlayerCar.groundMask.value : 1;
            var hits = new RaycastHit[48];
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

            // Round 86 (A): each walled line's jumps: no bumper from PreLip before a lip to RunOut past its furthest landing, then the funnel
            var gap = new Dictionary<int, bool[]>(); var funnel = new Dictionary<int, float[]>();
            float vmax = 12f; foreach (var vp in VehicleProfile.All) if (vp.Class != "Plane" && vp.Class != "Helicopter") vmax = Mathf.Max(vmax, vp.Speed);
            float g = Mathf.Max(1f, -Physics.gravity.y);
            for (int li = 0; li < lines.Count; li++)
            {
                var l = lines[li]; if (!l.walls || l.length <= 0) continue; int n = Mathf.FloorToInt(l.length / Step) + 1;
                var open = new bool[n]; var fun = new float[n]; gap[li] = open; funnel[li] = fun;
                int Wrap(int k) => l.loop ? (k % n + n) % n : k;
                var found = new List<Jump>();
                // the speed the course allows at each station: the line's own bends (its heading over 16 m, at BendGrip sideways; the tightest in the
                // 50 m before it), then no faster than a car gets out of the last bend (Accel), at most vmax
                var bend = new float[n]; var heading = new Vector3[n];
                for (int i = 0; i < n; i++) { var (_, hf) = l.at(Mathf.Min(i * Step, l.length - .01f)); hf.y = 0f; heading[i] = hf.sqrMagnitude < 1e-6f ? Vector3.forward : hf.normalized; }
                for (int i = 0; i < n; i++)
                {
                    int a = l.loop ? Wrap(i - 4) : Mathf.Max(0, i - 4), b = l.loop ? Wrap(i + 4) : Mathf.Min(n - 1, i + 4);
                    float k = Vector3.Angle(heading[a], heading[b]) * Mathf.Deg2Rad / Mathf.Max(Step, 8 * Step);
                    bend[i] = k < 1e-4f ? vmax : Mathf.Min(vmax, Mathf.Sqrt(BendGrip / k));
                }
                var allow = new float[n];
                for (int i = 0; i < n; i++) { float m = vmax; for (int d = 0; d <= 25; d++) { int q = l.loop ? Wrap(i - d) : Mathf.Max(0, i - d); m = Mathf.Min(m, bend[q]); } allow[i] = m; }
                for (int pass = 0; pass < (l.loop ? 2 : 1); pass++)
                    for (int i = 0; i < n; i++) { int q = l.loop ? Wrap(i - 1) : Mathf.Max(0, i - 1); allow[i] = Mathf.Min(allow[i], Mathf.Sqrt(allow[q] * allow[q] + 2f * Accel * Step)); }
                foreach (float off in new[] { 0f, -SideOffset, SideOffset })
                {
                    var y = DrivenProfile(l, off, n, mask, hits);
                    if (DumpTo != null && off == 0f)   // checks: the line's probed ground, station by station
                    {
                        var sb = new System.Text.StringBuilder($"# {race.Route.Name} {race.Route.Direction} line {li} length {l.length:0.0} loop {l.loop}").AppendLine().AppendLine("# s x z line_y ground_y allow");
                        for (int i = 0; i < n; i++) { var (pp, _) = l.at(Mathf.Min(i * Step, l.length - .01f)); sb.Append($"{i * Step:0} {pp.x:0.0} {pp.z:0.0} {pp.y:0.00} {y[i]:0.00} {allow[i]:0.0}").AppendLine(); }
                        try { System.IO.File.AppendAllText(DumpTo, sb.ToString()); } catch { }
                    }
                    float Y(int k) { k = Wrap(k); if (!Old && !l.loop && k >= n) k = n - 1; return k < 0 || k >= n || float.IsNaN(y[k]) ? -1e6f : y[k]; }   // Round 87: past a line's end its last ground (a shortcut's jump landing on the lap past it)
                    int last = n - 1;   // a loop's flights still run on past its end (Y wraps); its lips once only
                    for (float v = 12f; v <= vmax + .01f; v += 4f)
                        for (int i = 1; i < last; i++)
                        {
                            float y0 = Y(i - 1), y1 = Y(i); if (y0 < -1e5f || y1 < -1e5f) continue;
                            int wi = Wrap(i); if (wi >= 0 && wi < n && v > allow[wi] + 2f) continue;   // faster than its bends allow here
                            float dy = Mathf.Clamp(y1 - y0, -Step * .7f, Step * .7f), hyp = Mathf.Sqrt(Step * Step + dy * dy), vh = v * Step / hyp, vy = v * dy / hyp, dt = Step / vh;
                            if (Y(i + 1) >= y1 + vy * dt - .5f * g * dt * dt - .25f) continue;   // the ground follows: still on it
                            float peak = 0f, t = 0f; int j = i + 1;
                            for (; (j - i) * Step < 400f; j++)
                            {
                                t = (j - i) * Step / vh; float clear = y1 + vy * t - .5f * g * t * t - Y(j); peak = Mathf.Max(peak, Mathf.Min(clear, 100f));
                                if (clear <= .05f) break;
                            }
                            if (peak >= JumpClear && t >= JumpAir)
                            {
                                float lip = i * Step, land = j * Step;
                                var jp = found.Find(q => q.lip <= land && q.land >= lip - PreLip);
                                if (jp == null) found.Add(jp = new Jump { line = li, lip = lip, land = land });
                                jp.lip = Mathf.Min(jp.lip, lip); jp.land = Mathf.Max(jp.land, land); jp.air = Mathf.Max(jp.air, t); jp.clear = Mathf.Max(jp.clear, peak);
                            }
                            if (j > i + 1) i = j - 1;
                        }
                }
                // Round 87 (A, Dan's BUG-004: the Lyndon trailer): a stretch where the line itself is in the air (a shortcut whose heights are its flight)
                // is a jump too: from the last station on the ground to the first one back on it
                if (!Old)
                {
                    int airFrom = -1;
                    for (int i = 0; i <= n; i++)
                    {
                        bool air = false;
                        if (i < n)
                        {
                            var (pa, _) = l.at(Mathf.Min(i * Step, l.length - .01f));
                            int k = Physics.RaycastNonAlloc(pa + Vector3.up * .5f, Vector3.down, hits, 40f, mask, QueryTriggerInteraction.Ignore); float under = float.MaxValue;
                            for (int m = 0; m < k; m++) { var h = hits[m]; if (h.collider.attachedRigidbody || h.collider.gameObject.layer == Layer || h.normal.y < .3f) continue; under = Mathf.Min(under, pa.y - h.point.y); }
                            air = under > 2.1f;
                        }
                        if (air && airFrom < 0) airFrom = i;
                        if (!air && airFrom >= 0)
                        {
                            if ((i - airFrom) * Step >= 6f) { float lip = Mathf.Max(0, airFrom - 1) * Step, land = i * Step; if (!found.Exists(q => q.lip <= land && q.land >= lip - PreLip)) found.Add(new Jump { line = li, lip = lip, land = land, air = (land - lip) / 20f }); }
                            airFrom = -1;
                        }
                    }
                }
                foreach (var jp in found)
                {
                    int a = Mathf.FloorToInt((jp.lip - PreLip) / Step), b = Mathf.CeilToInt((jp.land + RunOut) / Step), f = Mathf.CeilToInt(FunnelLength / Step);
                    if (!Old && !l.loop && (b + f) * Step + 20f >= l.length) b = n - 1 + f + 1;   // Round 87 (A, Dan's BUG-004): landing near a shortcut's end (where it rejoins the lap): open to its end, no funnel
                    for (int k = a; k <= b; k++) { int w = Wrap(k); if (w >= 0 && w < n) open[w] = true; }
                    for (int k = 1; k <= f; k++) { int w = Wrap(b + k); if (w >= 0 && w < n) fun[w] = Mathf.Max(fun[w], 1f - (k - 1f) / f); }
                    jp.over = l.loop ? 0f : Mathf.Max(0f, jp.land - l.length); jp.lip = l.loop ? Mathf.Repeat(jp.lip, l.length) : jp.lip; jp.land = l.loop ? Mathf.Repeat(jp.land, l.length) : Mathf.Min(jp.land, l.length - .01f);
                    jp.lipAt = l.at(jp.lip).p; jp.landAt = l.at(jp.land).p; jp.at = l.at; jp.length = l.length; jp.loop = l.loop;
                    Jumps.Add(jp);
                }
            }
            bool Gap(int line, float s) => gap.TryGetValue(line, out var o) && o[Mathf.Clamp(Mathf.FloorToInt(s / Step), 0, o.Length - 1)];
            float Funnel(int line, float s) => funnel.TryGetValue(line, out var o) ? o[Mathf.Clamp(Mathf.FloorToInt(s / Step), 0, o.Length - 1)] : 0f;
            // another line's jump flies over or lands here (a shortcut's jump coming down where it rejoins the lap): no bumper within CrossClear of its
            // flight, from the lip to RunOut past its landing
            bool UnderFlight(int line, Vector3 w)
            {
                var q = new Vector2(w.x, w.z);
                foreach (var jp in Jumps)
                {
                    if (jp.line == line) continue;
                    var a = new Vector2(jp.lipAt.x, jp.lipAt.z); var b = new Vector2(jp.landAt.x, jp.landAt.z); var ab = b - a;
                    if (ab.sqrMagnitude > 1e-4f) b += ab.normalized * (RunOut + (Old ? 0f : jp.over)); ab = b - a;   // Round 87: and past the line's end where it lands beyond it
                    float t = ab.sqrMagnitude < 1e-4f ? 0f : Mathf.Clamp01(Vector2.Dot(q - a, ab) / ab.sqrMagnitude);
                    float clear = Old ? CrossClear : CrossClear + FlightCone * t * ab.magnitude;   // Round 87 (A, Dan's BUG-004 "this area should be open"): wider further along
                    if ((a + ab * t - q).sqrMagnitude < clear * clear && Mathf.Abs(w.y - Mathf.Lerp(jp.lipAt.y, jp.landAt.y, t)) < 12f) return true;
                }
                return false;
            }
            bool UnderFlightAny(Vector3 w) => UnderFlight(-1, w);

            var material = BumperMaterial();
            if (!bounce) bounce = new PhysicsMaterial("HIGH MODE bumper") { bounciness = 0f, bounceCombine = PhysicsMaterialCombine.Minimum,   // Round 85: WallGlance turns a vehicle off it (was a 0.3 bounce)
                 dynamicFriction = 0f, staticFriction = 0f, frictionCombine = PhysicsMaterialCombine.Minimum };
            int chunk = 0; var info = new Info(); infos[root] = info;
            // Round 87 (B): where the course is, for the HIGH MODE reset out of the bumpers: every line's stations (walled or not), how far out its
            // bumpers stand (wider on a jump's flight and landing), and the lead-in from the grid
            for (int li = 0; li < lines.Count; li++)
            {
                var l = lines[li]; if (l.length <= 0) continue;
                for (float s = 0; s < l.length; s += Step)
                {
                    var (p, _) = l.at(s); float half = l.half(s, p), r = half + 1.2f + Mathf.Max(FunnelOut * Funnel(li, s), (LandingHalf - half - 1.2f) * Widen(li, s));
                    if (Gap(li, s) || UnderFlightAny(p)) r = Mathf.Max(r, LandingHalf + 4f);
                    info.Add(p, r);
                }
            }
            var lead = race.Route.LeadIn; if (lead != null) for (int i = 0; i + 1 < lead.Length; i++) { float L = Vector3.Distance(lead[i], lead[i + 1]); for (float t = 0; t < L; t += Step) info.Add(Vector3.Lerp(lead[i], lead[i + 1], t / L), 9f); }

            for (int li = 0; li < lines.Count; li++)
            {
                var l = lines[li]; if (!l.walls || l.length <= 0) continue;
                for (int side = -1; side <= 1; side += 2)
                {
                    int count = Mathf.FloorToInt(l.length / Step), owner = li * 2 + (side > 0 ? 1 : 0);
                    var run = new List<St>(); var runs = new List<List<St>>(); bool firstAtStart = false;
                    for (int i = 0; i <= count + (l.loop ? 1 : 0); i++)
                    {
                        float s = l.loop ? Mathf.Repeat(i * Step, l.length) : Mathf.Min(i * Step, l.length - .01f);
                        var ok = Sample(l, li, s, side, i > count, owner, out var st);
                        if (ok && run.Count > 0 && (run[^1].w - st.w).magnitude > 7) { runs.Add(run); run = new List<St>(); }
                        if (ok) { if (i == 0) firstAtStart = true; run.Add(st); if (i <= count) { Place(st.w, owner, s); Samples++; } }
                        else if (run.Count > 0) { runs.Add(run); run = new List<St>(); }
                    }
                    bool lastAtEnd = run.Count > 0; if (run.Count > 0) runs.Add(run);
                    // Round 87: on a loop, the run over the lap's seam (the start line) is one run, not two ends there
                    if (!Old && l.loop && firstAtStart && lastAtEnd && runs.Count > 1) { var joined = new List<St>(runs[^1]); joined.AddRange(runs[0].Skip(1)); runs[0] = joined; runs.RemoveAt(runs.Count - 1); }
                    bool closed = !Old && l.loop && runs.Count == 1 && firstAtStart && lastAtEnd;   // all the way round: no ends
                    foreach (var r in runs) Finish(r, li, l, closed);
                }
            }
            info.jumps.AddRange(Jumps); info.report = Report;
            Debug.Log($"[High] bumpers for {race.Route.Name} {race.Route.Direction}: {Report}, {chunk} pieces");
            return root;

            // Round 87 (A): one unbroken run of stations: a stub (shorter than MinRun) is left out; the distance out from the line changes at most
            // MaxSlope per station (never a step in or out you can hit: nearer the line where the ground needs it, eased into over the stations before);
            // each end that is not the run's continuation curves out away from the line (EndCurve over EndLength) where the ground and other lines allow
            void Finish(List<St> run, int line, Line l, bool closed = false)
            {
                if (run.Count < 2) return;
                if (!Old && (run.Count - 1) * Step < MinRun) { Stubs++; return; }
                if (!Old)
                {
                    for (int i = 1; i < run.Count; i++) run[i] = run[i].With(Mathf.Min(run[i].edge, run[i - 1].edge + MaxSlope));
                    for (int i = run.Count - 2; i >= 0; i--) run[i] = run[i].With(Mathf.Min(run[i].edge, run[i + 1].edge + MaxSlope));
                    int k = closed ? 0 : Mathf.Min(Mathf.RoundToInt(EndLength / Step), run.Count / 2);
                    foreach (int end in new[] { 0, run.Count - 1 })
                        for (int j = 0; j < k; j++)
                        {
                            int i = end == 0 ? j : run.Count - 1 - j; var st = run[i]; float t = 1f - j / (float)k, add = EndCurve * t * t;
                            // as far out as the ground (no drop) and the other lines let it
                            for (; add > .05f; add -= .25f)
                            {
                                var q = st.p + st.o * (st.edge + add);
                                if (Surface(q, st.y, out float qy) && qy > st.y - 1f && !Open(new Vector3(q.x, st.y, q.z), line, st.s, st.p, st.o)) break;
                            }
                            if (add > .05f) { run[i] = st.With(st.edge + add); Curved++; }
                        }
                }
                var strip = new List<(Vector3 w, Vector3 o)>(run.Count); foreach (var st in run) strip.Add((st.Final, st.o));
                info.strips.Add(strip.ConvertAll(q => q.w + q.o * .5f).ToArray());
                for (int a = 0; a + 1 < strip.Count; a += 39) Emit(root.transform, strip.GetRange(a, Mathf.Min(40, strip.Count - a)), chunk++, material);
            }

            bool Sample(Line l, int line, float s, int side, bool wrap, int owner, out St st)
            {
                st = default; var (p, f) = l.at(s); f = Vector3.ProjectOnPlane(f, Vector3.up); if (f.sqrMagnitude < 1e-4f) return false; f.Normalize();
                if (Gap(line, s)) { if (side < 0 && !wrap) JumpStations++; return false; }   // Round 86: a jump's flight and landing
                if (!Surface(p, p.y, out float roadY) || Mathf.Abs(roadY - p.y) > 1.6f) return false;   // no ground at the line: a gap, a deck over it
                var outward = Vector3.Cross(Vector3.up, f) * side; float half = l.half(s, p), wide = Widen(line, s), reach = Mathf.Lerp(half + 1.2f, Mathf.Max(half + 1.2f, LandingHalf), wide);
                reach = Mathf.Max(reach, half + 1.2f + FunnelOut * Funnel(line, s)); float edge = reach;   // Round 86: past a landing, the funnel
                if (reach > half + 1.25f) LandingStations++;
                bool raised = false;
                for (float d = 2.5f; d <= reach; d += .5f)
                    if (!Surface(p + outward * d, roadY, out float y) || y < roadY - 1f) { edge = Mathf.Max(2.2f, d - .3f); raised = d < half - .5f; break; }   // the ground drops away here
                // Round 87 (D, the Lyndon jump's trailer): the drop inside the line's own width is a raised ramp or deck the line runs up (not a road's
                // edge): no bumper beside it (it stood 2.2 m out at the deck's height, in the way of anything a little off the line)
                if (raised && !Old) return false;
                var w = p + outward * edge; w.y = roadY;
                if (Open(w, line, s, p, outward)) { Openings++; return false; }
                if (UnderFlight(line, w)) { if (side < 0 && !wrap) JumpStations++; return false; }   // Round 86: under another line's jump
                st = new St { p = new Vector3(p.x, roadY, p.z), o = outward, edge = edge, y = roadY, s = s, w = w };
                return wrap || !Duplicate(w, owner, s, l.length);
            }
        }
        struct St { public Vector3 p, o, w; public float edge, y, s; public St With(float e) { var c = this; c.edge = e; return c; } public Vector3 Final => p + o * edge; }

        // Round 87: what a build made, for the HIGH MODE reset (Inside) and the checks (each run's middle line)
        public sealed class Info
        {
            public readonly List<Vector3[]> strips = new List<Vector3[]>(); public readonly List<Jump> jumps = new List<Jump>(); public string report = "";
            readonly Dictionary<long, List<Vector4>> cells = new Dictionary<long, List<Vector4>>();
            public void Add(Vector3 p, float r) { long k = Key(Mathf.FloorToInt(p.x / Cell), Mathf.FloorToInt(p.z / Cell)); if (!cells.TryGetValue(k, out var l)) cells[k] = l = new List<Vector4>(); l.Add(new Vector4(p.x, p.y, p.z, r)); }
            // on the course: within its bumpers' reach (and a meter more) of some station, at its height (within 10 m: a deck over a road is not it)
            public bool Inside(Vector3 q)
            {
                int cx = Mathf.FloorToInt(q.x / Cell), cz = Mathf.FloorToInt(q.z / Cell);
                for (int x = cx - 1; x <= cx + 1; x++) for (int z = cz - 1; z <= cz + 1; z++)
                        if (cells.TryGetValue(Key(x, z), out var l)) foreach (var v in l)
                            {
                                float dx = v.x - q.x, dz = v.z - q.z, r = v.w + 1f;
                                if (dx * dx + dz * dz <= r * r && Mathf.Abs(v.y - q.y) < 10f) return true;
                            }
                return false;
            }
        }
        static readonly Dictionary<GameObject, Info> infos = new Dictionary<GameObject, Info>();
        public static Info Current => current && infos.TryGetValue(current, out var i) ? i : null;
        // Round 87 (B): a HIGH MODE player out of the bumpers (checks: how many were put back)
        public static bool Outside(Vector3 q) { var i = Current; return Shown && i != null && !i.Inside(q); }
        public static int OutResets;
        // Round 86 (A): the driven surface along a line, 'off' metres to its right, one height per Step (NaN: none): from above, the surface nearest
        // to where the last two stations point (so it climbs a ramp, stays on a deck or under it, and falls into a hollow)
        static float[] DrivenProfile(Line l, float off, int n, int mask, RaycastHit[] hits)
        {
            var y = new float[n]; float prev = float.NaN, slope = 0f;
            for (int i = 0; i < n; i++)
            {
                var (p, f) = l.at(Mathf.Min(i * Step, l.length - .01f)); f = Vector3.ProjectOnPlane(f, Vector3.up);
                var q = f.sqrMagnitude < 1e-4f ? p : p + Vector3.Cross(Vector3.up, f.normalized) * off;
                float want = float.IsNaN(prev) ? p.y : prev + slope * Step, top = Mathf.Max(want, p.y) + 10f;
                int k = Physics.RaycastNonAlloc(new Vector3(q.x, top, q.z), Vector3.down, hits, 60f, mask, QueryTriggerInteraction.Ignore);
                float best = float.MaxValue, at = float.NaN;
                for (int m = 0; m < k; m++)
                {
                    var h = hits[m]; if (h.collider.attachedRigidbody || h.normal.y < .3f || h.collider.gameObject.layer == Layer) continue;
                    float d = Mathf.Abs(h.point.y - want); if (h.point.y > want + 1.5f) d += 2f;   // a surface above where it heads: a ramp it climbs, else a roof
                    if (d < best) { best = d; at = h.point.y; }
                }
                if (float.IsNaN(at)) at = p.y;   // nothing found under it (a floor the probe misses): the line's own height there
                y[i] = at;
                if (!float.IsNaN(at)) { slope = float.IsNaN(prev) ? 0f : Mathf.Clamp((at - prev) / Step, -1.2f, 1.2f); prev = at; }
            }
            // a 5-station median: one station's stray hit (a roof edge, a wall top beside the line) is not a ramp
            var med = new float[n]; var w5 = new float[5];
            for (int i = 0; i < n; i++)
            {
                int c = 0; for (int k = -2; k <= 2; k++) { int q = l.loop ? ((i + k) % n + n) % n : Mathf.Clamp(i + k, 0, n - 1); if (!float.IsNaN(y[q])) w5[c++] = y[q]; }
                if (c == 0) { med[i] = y[i]; continue; } System.Array.Sort(w5, 0, c); med[i] = w5[c / 2];
            }
            return med;
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
            // Round 86 (A): each end a rounded half dome (the profile's outer half turned round the end), never a square end to hit
            foreach (int end in new[] { 0, strip.Count - 1 })
            {
                var (w, o) = strip[end]; var nb = strip[end == 0 ? 1 : strip.Count - 2].w;
                var a = Vector3.ProjectOnPlane(w - nb, Vector3.up); a = a.sqrMagnitude < 1e-4f ? Vector3.Cross(o, Vector3.up) : a.normalized;
                var c = w + o * .5f; const int K = 8; int H = Profile.Length - 4; int start = verts.Count;   // the profile's points 4 .. 8: its top to its outer foot
                for (int k = 0; k <= K; k++)
                {
                    float th = Mathf.PI * k / K; var d = o * Mathf.Cos(th) + a * Mathf.Sin(th);
                    for (int j = 4; j < Profile.Length; j++) { var q = Profile[j]; verts.Add(c + d * (q.x - .5f) + Vector3.up * q.y - origin); colors.Add(White); }
                }
                var mid = c + Vector3.up * -.3f - origin;
                for (int k = 0; k < K; k++)
                    for (int j = 0; j + 1 < H; j++)
                    {
                        int i0 = start + k * H + j, i1 = i0 + 1, i2 = i0 + H, i3 = i2 + 1;
                        var nrm = Vector3.Cross(verts[i1] - verts[i0], verts[i2] - verts[i0]); var m = (verts[i0] + verts[i3]) * .5f - mid;
                        if (Vector3.Dot(nrm, m) >= 0) tris.AddRange(new[] { i0, i1, i2, i1, i3, i2 }); else tris.AddRange(new[] { i0, i2, i1, i1, i2, i3 });
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
                cv.Add(wa + Vector3.up * Profile[0].y - origin); cv.Add(wa + Vector3.up * Fence - origin); cv.Add(wb + Vector3.up * Profile[0].y - origin); cv.Add(wb + Vector3.up * Fence - origin);   // Round 85: down to the buried base (it began 0.3 m over the line's height: where the ground beside a trail dips, a body went under it and stuck)
                ct.AddRange(new[] { k, k + 1, k + 2, k + 2, k + 1, k + 3, k, k + 2, k + 1, k + 2, k + 3, k + 1 });
            }
            var solid = new Mesh { name = "HIGH MODE bumper collision" }; solid.indexFormat = cv.Count > 65000 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;
            solid.SetVertices(cv); solid.SetTriangles(ct, 0); solid.RecalculateBounds();
            var collider = go.AddComponent<MeshCollider>(); collider.sharedMesh = solid; collider.sharedMaterial = bounce;
        }
    }
}
