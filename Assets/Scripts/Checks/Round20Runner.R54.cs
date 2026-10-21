using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Racer;
using UnityEngine;

namespace ChastainRush
{
    // Round 54 in the exe (-exeRound54 <dir> [-r54Only aihard,aiscale,linemiss,complete,barriers,elaine,terrace,bounce,a54pics] [-traffic Normal]
    // -logFile <dir>\Player.log):
    //   aihard     Addison Heights on Hard, forward x2 and reverse x2, 3 AI, the player parked off the course: every AI finishes; their lap times
    //              against Dan's (1:12 to 1:15 on the Drifter Twin)
    //   aiscale    one forward race each on Easy and Normal: their laps slower than Hard's, in that order
    //   linemiss   B on Noonday Creek: past the line outside its arch on lap 2 (the lap counts, +5 s), the same on the last lap (the race ends there);
    //              past it 70 m off (a cut, nowhere near it): no lap
    //   complete   C: the Chastain Glen loop, 1 lap, 3 AI; the player finishes (its gates crossed by hand), the COMPLETE RACE button shows, and it
    //              classifies every AI at an estimated finish
    //   barriers   D1: traffic Normal on three races (Chastain Glen reverse, Chastain Lakes, Addison Heights): no traffic car stands pushing at a barrier
    //   elaine     A1: the Elaine jump flat out (Street Classic, Drifter Twin) against the lap's way; its ground without the jump (down Elaine on the
    //              ground) slower; where cars come down off the crest
    //   terrace    A2: the Joel terrace flat out against the lap's way (reverse); short (12 m/s off the edge) and down the bank beside it: slower
    //   bounce     A3: the bounce house flat out against the lap's way (reverse); slow into it (bounced back) and round it over the hill: slower
    //   a54pics    Dan's spots and the new pieces
    public sealed partial class Round20Runner
    {
        IEnumerator StartRoute(string file, string dir, int ai, int laps = 0)
        {
            RaceController.RouteOverride = file; RaceController.DirectionOverride = dir; RaceController.LapsOverride = laps; RaceController.AiCount = ai;
            if (race.State != RaceController.Stage.Free) race.EndRace();
            race.Reload(); race.StartRace();
            while (race.State == RaceController.Stage.Countdown) yield return null;
        }

        // ------------------------------------------------------------ A7: the AI on Hard
        IEnumerator R54AiRace(string dir, int n, int difficulty, List<float> laps)
        {
            string dn = difficulty == 2 ? "Hard" : difficulty == 1 ? "Normal" : "Easy";
            report.AppendLine(); report.AppendLine($"=== A7. Addison Heights {dir} #{n} on {dn}: 3 laps, 3 AI, traffic {(Traffic.Instance ? Traffic.LevelNames[Traffic.Instance.Level] : "none")}, the player parked off the course");
            int prev = RoadDriver.Difficulty; RoadDriver.Difficulty = difficulty;
            yield return StartRoute("addison_heights.json", dir, 3);
            respawn.PlaceAt(RaceRoute.Surface(2880f, 313f) + Vector3.up * .4f, 90f); race.NotifyTeleported();   // Crawford Circle's west leg, past its barrier
            var ai = race.Entrants.Where(e => !e.IsPlayer).ToList();
            Time.timeScale = 2f; Time.maximumDeltaTime = .5f;
            float end = Time.realtimeSinceStartup + 6f * 60f;
            while (Time.realtimeSinceStartup < end && ai.Any(e => !e.Finished)) yield return new WaitForSecondsRealtime(.5f);
            Time.timeScale = 1f;
            foreach (var e in ai)
            {
                laps.AddRange(e.LapTimes.Skip(1));   // the first lap starts from the grid: the flying laps compare with Dan's
                Check($"{dir} #{n} {dn}: {e.Name} ({e.VehicleName}) finishes", e.Finished && !e.Estimated,
                      $"{(e.Finished ? $"total {e.FinalTotal:0.0} s" : $"not finished, laps {e.CompletedLaps}")}, laps {string.Join(" / ", e.LapTimes.Select(x => x.ToString("0.0")))}, missed {e.MissedTotal}, shortcuts {string.Join(", ", e.ShortcutsTaken)}");
            }
            RoadDriver.Difficulty = prev;
            race.EndRace(); RaceController.DirectionOverride = null;
        }
        static string LapStats(List<float> l) => l.Count == 0 ? "no laps" : $"best {l.Min():0.0} s, median {l.OrderBy(x => x).ElementAt(l.Count / 2):0.0} s, slowest {l.Max():0.0} s ({l.Count} flying laps)";
        IEnumerator R54AiHard()
        {
            var laps = new List<float>();
            foreach (var dir in new[] { "forward", "reverse" }) for (int k = 1; k <= 2; k++) yield return Guard(R54AiRace(dir, k, 2, laps));
            report.AppendLine($"  Hard, all races: {LapStats(laps)}");
            Check("A7: on Hard the AI's best flying laps are with Dan's (1:12 to 1:15): best within 1:20", laps.Count > 0 && laps.Min() <= 80f, LapStats(laps));
        }
        IEnumerator R54AiScale()
        {
            var easy = new List<float>(); var normal = new List<float>();
            yield return Guard(R54AiRace("forward", 1, 0, easy));
            yield return Guard(R54AiRace("forward", 1, 1, normal));
            report.AppendLine($"  Easy: {LapStats(easy)}; Normal: {LapStats(normal)}");
            Check("A7: Easy slower than Normal (median flying lap)", easy.Count > 0 && normal.Count > 0 && easy.OrderBy(x => x).ElementAt(easy.Count / 2) > normal.OrderBy(x => x).ElementAt(normal.Count / 2), $"Easy {LapStats(easy)}; Normal {LapStats(normal)}");
        }

        // ------------------------------------------------------------ B: a missed line
        // the player moved by hand across the line's plane, 'side' m off its middle (one physics step from 3 m before it to 3 m past it)
        IEnumerator AcrossLine(RaceGate g, float side)
        {
            var a = g.Position - g.Forward * 3f + g.Right * side; var b = g.Position + g.Forward * 3f + g.Right * side;
            a = RaceRoute.Surface(a.x, a.z); b = RaceRoute.Surface(b.x, b.z);
            Place(a, g.Forward, 0f); yield return new WaitForFixedUpdate(); yield return new WaitForFixedUpdate();
            car.Body.position = b + Vector3.up * 0.4f; car.transform.position = car.Body.position;
            yield return new WaitForFixedUpdate(); yield return new WaitForFixedUpdate();
        }
        IEnumerator R54LineMiss()
        {
            report.AppendLine(); report.AppendLine("=== B. a missed line (Noonday Creek, its own vehicle): the lap still counts, the 5 s penalty");
            yield return StartRoute("noonday_creek.json", "forward", 0, 3);
            var p = race.Player; var g = race.Route.Gate; int n = race.Route.Checkpoints.Count; float wide = g.HalfWidth + 10f;
            yield return AcrossLine(g, 0f);   // the clock starts
            Check("the start line starts the clock", p.ClockStarted);
            // lap 1: every checkpoint met, then past the line outside the arch
            p.NextCheckpoint = n; p.LapAway = true;
            yield return AcrossLine(g, wide);
            Check($"lap 1 counts when the line is passed {wide:0} m off its middle (outside the arch)", p.CompletedLaps == 1 && p.PenaltyTotal == RaceController.MissPenaltySeconds,
                  $"laps {p.CompletedLaps}, penalty {p.PenaltyTotal:0} s, the log: {string.Join(" | ", p.PenaltyLog)}");
            yield return Capture("54_B_line_missed_message");
            // a cut nowhere near the line: no lap
            p.NextCheckpoint = n; p.LapAway = true;
            yield return AcrossLine(g, 70f);
            Check("passing the line's plane 70 m off (a cut) counts nothing", p.CompletedLaps == 1, $"laps {p.CompletedLaps}");
            // lap 2 at the line as usual, then the last lap missed: the race ends there
            yield return AcrossLine(g, 0f);
            Check("lap 2 at the line", p.CompletedLaps == 2, $"laps {p.CompletedLaps}");
            p.NextCheckpoint = n; p.LapAway = true;
            yield return AcrossLine(g, -wide);
            Check("the last lap missed at the line: the race ends there with the penalty", p.Finished && race.State == RaceController.Stage.Finished && p.PenaltyTotal == 2f * RaceController.MissPenaltySeconds,
                  $"finished {p.Finished}, total {p.FinalTotal:0.0} s, penalty {p.PenaltyTotal:0} s, laps {string.Join(" / ", p.LapTimes.Select(x => x.ToString("0.0")))}");
            yield return new WaitForSecondsRealtime(4f); yield return Capture("54_B_results");
            race.EndRace(); RaceController.DirectionOverride = null; RaceController.LapsOverride = 0;
        }

        // ------------------------------------------------------------ C: COMPLETE RACE
        IEnumerator R54Complete()
        {
            report.AppendLine(); report.AppendLine("=== C. COMPLETE RACE (the Chastain Glen loop, 1 lap, 3 AI)");
            var menus = GameMenus.Create(); yield return new WaitForSecondsRealtime(1.5f); if (menus) menus.CloseForCheck();   // the menus (never in a check run otherwise)
            yield return StartRoute("chastain_glen_loop.json", "forward", 3, 1);
            var p = race.Player; var r = race.Route;
            yield return new WaitForSecondsRealtime(1.5f);
            yield return AcrossLine(r.Gate, 0f);
            foreach (var cp in r.Checkpoints) yield return AcrossLine(cp, 0f);
            p.LapAway = true;
            yield return AcrossLine(r.Gate, 0f);
            Check("the player has finished, the AI still racing", p.Finished && race.Entrants.Any(e => !e.IsPlayer && !e.Finished), $"player {p.Finished}, AI finished {race.Entrants.Count(e => !e.IsPlayer && e.Finished)}");
            float until = Time.realtimeSinceStartup + 12f; while (WinnerShot.Active && Time.realtimeSinceStartup < until) yield return null;
            yield return new WaitForSecondsRealtime(1f);
            var button = FindObjectsByType<RectTransform>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(t => t.name == "Complete race");
            Check("the COMPLETE RACE button shows (with its glyph)", race.CanSimulateRemaining && menus && menus.CompleteButtonShowing && button && button.GetComponentInChildren<MenuGlyph>(), $"can {race.CanSimulateRemaining}, button {(button ? button.gameObject.activeInHierarchy.ToString() : "missing")}");
            yield return Capture("54_C_complete_race_button");
            race.CompleteRace();
            yield return new WaitForSecondsRealtime(1f);
            var ai = race.Entrants.Where(e => !e.IsPlayer).ToList();
            Check("every AI is classified at an estimated finish, the button gone", ai.All(e => e.Finished) && !race.CanSimulateRemaining && !(button && button.gameObject.activeInHierarchy),
                  string.Join("; ", ai.Select(e => $"{e.Name} {(e.Finished ? RaceController.Fmt(e.FinalTotal) + (e.Estimated ? " (estimated)" : "") : "racing")}")));
            yield return Capture("54_C_results");
            race.EndRace(); RaceController.DirectionOverride = null; RaceController.LapsOverride = 0;
            if (menus) Destroy(menus.gameObject);
        }

        // ------------------------------------------------------------ D1: traffic at the barriers
        IEnumerator R54Barriers()
        {
            report.AppendLine(); report.AppendLine($"=== D1. traffic at the race barriers (traffic {(Traffic.Instance ? Traffic.LevelNames[Traffic.Instance.Level] : "none")}), 90 s of each race at double speed");
            if (!Traffic.Instance || Traffic.Instance.Level == 0) { Check("traffic is on (-traffic Normal)", false); yield break; }
            foreach (var (file, dir, park) in new[] { ("chastain_glen_loop.json", "reverse", new Vector2(3064.1f, 950f)), ("chastain_lakes.json", "forward", new Vector2(900f, 1420f)), ("addison_heights.json", "forward", new Vector2(3060f, 330f)) })
            {
                yield return StartRoute(file, dir, 0);
                respawn.PlaceAt(RaceRoute.Surface(park.x, park.y) + Vector3.up * .4f, 180f); race.NotifyTeleported(); car.enabled = false;
                var bars = race.Barriers.Built.Select(b => b.Centre).ToList();
                int turned0 = Traffic.TurnedAtBarriers; var pushing = new Dictionary<TrafficDriver, float>(); var stuck = new HashSet<string>(); int near = 0;
                Time.timeScale = 2f; Time.maximumDeltaTime = .5f;
                float end = Time.time + 90f;
                while (Time.time < end)
                {
                    yield return new WaitForSeconds(0.25f);
                    foreach (var d in Traffic.Instance.Cars)
                    {
                        if (!d || !d.gameObject.activeSelf) continue;
                        var q = d.Car.Body.position; float bd = bars.Count == 0 ? 99f : bars.Min(b => Flat(b - q).magnitude);
                        if (bd < 8f) near++;
                        bool push = bd < 7f && d.Car.Body.linearVelocity.magnitude < 1f && d.LastThrottle > 0.05f;
                        pushing[d] = push ? (pushing.TryGetValue(d, out var t) ? t : 0f) + 0.25f : 0f;
                        if (pushing[d] >= 4f) stuck.Add($"{d.name} at ({q.x:0}, {q.z:0})");
                    }
                }
                Time.timeScale = 1f;
                Check($"{file} {dir}: no traffic car pushes at a barrier (4 s or more with the throttle on, standing)", stuck.Count == 0,
                      $"{bars.Count} barriers; {Traffic.TurnedAtBarriers - turned0} turned round at one; samples within 8 m of one {near}{(stuck.Count > 0 ? "; pushing: " + string.Join(", ", stuck) : "")}");
                // then cars put on closed roads 30 m beyond a barrier, driving at it: each stops short and turns round (Dan BUG-006)
                var net = TrafficNetwork.Instance; var lines = new List<string>(); bool allOk = true; int tried = 0;
                foreach (var bar in race.Barriers.Built)
                {
                    if (tried >= 3) break;
                    var bc = new Vector2(bar.Centre.x, bar.Centre.z); int e = -1;
                    for (int k = 0; k < net.EdgeCount; k++) if (net.Distance(k, bc) < 3f && net.Length[k] > 40f) { e = k; break; }
                    if (e < 0) continue;
                    var start2 = bc + new Vector2(bar.Along.x, bar.Along.z) * 30f; int n = net.PointCount(e), iS = 0, iB = 0; float dS = 1e9f, dB = 1e9f;
                    for (int k = 0; k < n; k++) { var q = net.Point(e, k); float a1 = (q - start2).sqrMagnitude, b1 = (q - bc).sqrMagnitude; if (a1 < dS) { dS = a1; iS = k; } if (b1 < dB) { dB = b1; iB = k; } }
                    if (iS == iB) continue;
                    int edir = iB > iS ? 1 : -1; var sp = net.Point(e, iS);
                    var d = Traffic.Instance.Cars.FirstOrDefault(c => c); if (!d) break;
                    tried++; d.gameObject.SetActive(true);
                    var pos = RaceRoute.Surface(sp.x, sp.y) + Vector3.up * .7f;
                    d.Place(e, edir, 0f, pos, 8f);
                    int t0 = Traffic.TurnedAtBarriers; float push = 0f, worst = 0f, closest = 99f; Time.timeScale = 1f;
                    for (float t = 0f; t < 16f; t += Time.fixedDeltaTime)
                    {
                        yield return new WaitForFixedUpdate();
                        var q = d.Car.Body.position; closest = Mathf.Min(closest, Flat(q - bar.Centre).magnitude);
                        push = Flat(q - bar.Centre).magnitude < 7f && d.Car.Body.linearVelocity.magnitude < 1f && d.LastThrottle > .05f ? push + Time.fixedDeltaTime : 0f; worst = Mathf.Max(worst, push);
                    }
                    bool turned = Traffic.TurnedAtBarriers > t0, ok = worst < 2f;
                    if (!ok) allOk = false;
                    lines.Add($"{bar.Data.id} ({bar.Data.label}): came within {closest:0.0} m, {(turned ? "turned round" : "did not turn")}, longest pushing {worst:0.0} s");
                }
                Check($"{file} {dir}: a car driven at a barrier on its closed road stops short and turns round, never pushes", tried > 0 && allOk, string.Join("; ", lines));
                car.enabled = true; race.EndRace();
            }
            RaceController.DirectionOverride = null;
        }

        // ------------------------------------------------------------ A1 to A3: the three shortcuts
        sealed class R54Watch { public float air, cur, lipV = -1f, bedMax; public Vector3 firstDown = new Vector3(float.NaN, 0f, 0f); public bool flying, landed; }
        void R54Track(R54Watch w)
        {
            var p = car.Body.position;
            if (car.GroundedWheels >= 2) w.bedMax = Mathf.Max(w.bedMax, ShrubBeds.CoverageAt(p));
            if (car.GroundedWheels == 0) { if (!w.flying && w.lipV < 0f) w.lipV = car.Body.linearVelocity.magnitude; w.flying = true; w.cur += Time.fixedDeltaTime; }
            else { if (w.flying && w.cur > .3f) { w.air = Mathf.Max(w.air, w.cur); if (!w.landed) { w.landed = true; w.firstDown = p; } } w.flying = false; w.cur = 0f; }
        }
        static string R54Line(R54Watch w) => $"off at {w.lipV:0.0} m/s, longest air {w.air:0.00} s, first down at {(float.IsNaN(w.firstDown.x) ? "-" : $"({w.firstDown.x:0}, {w.firstDown.z:0})")}, deepest in a shrub bed {w.bedMax:0.00}";

        IEnumerator R54Shortcut(string id, string dir, string title, System.Func<RaceShortcut, List<(string name, List<Vector3> line, float[] prof)>> cheats, float[] shortProf = null)
        {
            report.AppendLine(); report.AppendLine($"=== {title}");
            yield return StartRoute("addison_heights.json", dir, 0);
            var sc = race.Route.Shortcuts.FirstOrDefault(s => s.Id == id);
            if (sc == null) { Check($"{id} is in the {dir} race", false); race.EndRace(); yield break; }
            report.AppendLine($"  {sc.Name}: forgives {string.Join(", ", race.Route.Checkpoints.Where((c, i) => i >= sc.FirstForgiven && i <= sc.LastForgiven).Select(c => c.Label))}");
            var bad = new List<string>(); float roadOrig = -1f;
            foreach (var vid in new[] { "original", "drifter" })
            {
                yield return Vehicle(vid);
                float road = -1f, via = -1f; var r0 = new Run(); var r1 = new Run(); var w = new R54Watch();
                race.StartRace(); while (race.State == RaceController.Stage.Countdown) yield return null;
                yield return TimedLakes(sc, false, r0, t => road = t);
                race.StartRace(); while (race.State == RaceController.Stage.Countdown) yield return null;
                { var pl = race.Player; pl.ClockStarted = true; pl.StartAt = pl.LapStart = race.RaceClock; pl.NextCheckpoint = 0; pl.LapAway = true; }   // placed mid-lap: its clock running, so the shortcut counts
                yield return TimedLakes(sc, true, r1, t => via = t, 60f, null, 0f, () => R54Track(w));
                bool taken = race.Player.ShortcutsTaken.Any(s => s.EndsWith(id));
                yield return Capture($"54_{id}_{vid}_clean_end");
                float saved = road > 0f && via > 0f ? road - via : -99f;
                if (vid == "original") roadOrig = road;
                if (!(r1.reached && saved > 0f && taken)) bad.Add(Nm(vid));
                report.AppendLine($"  {Nm(vid),-16} the lap's way {road:0.0} s; the shortcut {via:0.0} s, saves {saved:0.0} s{(taken ? "" : " (NOT counted as taken)")}{(r1.reached ? "" : "; STOPPED " + r1.end + $" at ({car.Body.position.x:0}, {car.Body.position.z:0})")}; {R54Line(w)}");
            }
            Check($"{sc.Name} flat out (Street Classic, Drifter Twin): through, taken, faster than the lap's way", bad.Count == 0, bad.Count == 0 ? "" : "not: " + string.Join(", ", bad));
            yield return Vehicle("original");
            var faster = new List<string>(); int n = 0;
            foreach (var (name, line, prof) in cheats(sc))
            {
                var r = new Run(); float t = -1f; var w = new R54Watch();
                yield return TimedVia(sc, line, r, x => t = x, () => R54Track(w), prof);
                yield return Capture($"54_{id}_other_{++n}_end");
                bool slower = !r.reached || t < 0f || t > roadOrig;
                if (!slower) faster.Add(name);
                report.AppendLine($"  {n}. {name}: {(r.reached && t > 0f ? $"{t:0.0} s ({t - roadOrig:+0.0;-0.0} s against the lap's way)" : "did not get through (" + r.end + $", at ({car.Body.position.x:0}, {car.Body.position.z:0}))")}; {R54Line(w)}, slowest {r.minSpeed:0.0} m/s");
            }
            Check($"{sc.Name}: short, missed, or its ground without the stunt, each slower than the plain lap", faster.Count == 0, faster.Count == 0 ? "" : "faster: " + string.Join("; ", faster));
            race.EndRace(); RaceController.DirectionOverride = null;
        }
        // the shortcut's own line with a speed held from a point on it (a slow run-up)
        static float[] HoldFrom(RaceShortcut sc, Vector3 at, float speed, float lead = 60f)
        {
            float s = lead; for (int i = 1; i < sc.Path.Length; i++) { if (Flat(sc.Path[i] - at).magnitude < 1.5f) break; s += Flat(sc.Path[i] - sc.Path[i - 1]).magnitude; }
            return new[] { s - 25f, speed, s + 30f, speed };
        }

        IEnumerator R54Elaine() => R54Shortcut("f_elaine_jump", "forward", "A1. THE ELAINE JUMP (forward), flat out, timed against the lap's own way over the same ground", sc => new List<(string, List<Vector3>, float[])>
        {
            ("down Elaine Circle on the ground (no jump: held to 13 m/s over the crest), then the way", sc.Path.ToList(), HoldFrom(sc, RaceRoute.Surface(3196f, 306f), 13f)),
            ("from the valley along Elaine west, through the yards (960's, 1007's) to Crawford Circle south of the line", Dodge(Pts(3196f, 307f, 3140f, 308f, 3104f, 307f, 3060f, 308f, 3046f, 300f, 3030f, 276f, 3010f, 250f, 2994f, 236f, 2989f, 228f)), null),
        });
        IEnumerator R54Terrace() => R54Shortcut("r_joel_terrace", "reverse", "A2. THE JOEL TERRACE (reverse), flat out, timed against the lap's own way over the same ground", sc => new List<(string, List<Vector3>, float[])>
        {
            ("short: 12 m/s off the terrace's edge", sc.Path.ToList(), HoldFrom(sc, RaceRoute.Surface(2962.4f, 401.9f), 12f)),
            ("down the bank beside the terrace (east of it), no jump", Dodge(Pts(2944f, 411.5f, 2966f, 409f, 2975f, 398f, 2984f, 380f, 2991f, 360f, 2992f, 345f)), null),
            ("down the bank west of the terrace, no jump", Dodge(Pts(2935f, 411.5f, 2948f, 404f, 2958f, 388f, 2972f, 368f, 2988f, 350f, 2992f, 340f)), null),
        });
        IEnumerator R54Bounce() => R54Shortcut("r_bounce_house", "reverse", "A3. THE BOUNCE HOUSE (reverse), flat out, timed against the lap's own way over the same ground", sc => new List<(string, List<Vector3>, float[])>
        {
            ("slow into the bounce house (9 m/s): bounced back", sc.Path.ToList(), HoldFrom(sc, RaceRoute.Surface(3189f, 189f), 9f)),
            ("round the bounce house: the yards, the bluff's south end and up the hill to Suholden Circle", Dodge(Pts(3146f, 160f, 3175f, 172f, 3212f, 168f, 3238f, 190f, 3256f, 230f, 3262f, 270f, 3264f, 299f)), null),
        });

        // ------------------------------------------------------------ E2: out of MH14 along the street
        IEnumerator R54Mh14()
        {
            report.AppendLine(); report.AppendLine("=== E2. out of MH14 (the Lake drain shortcut's way up onto North Lakeside Drive): turned along the street as it clears it, where it comes down");
            yield return StartLakesRace();
            var sc = LakesSc("e_lake_drain");
            if (sc == null) { Check("the lake drain shortcut is in the build", false); race.EndRace(); yield break; }
            var mh = new Vector3(1180.08f, 0f, 1326.25f); var street = new Vector3(-0.9564f, 0f, 0.2921f);   // network.json MH14: x, z, dir
            var bad = new List<string>();
            foreach (var id in new[] { "original", "drifter" })
            {
                yield return Vehicle(id);
                int st0 = ManholeLid.Straightened, mh0 = ManholeLid.Bursts; var run = new Run(); float outHead = float.NaN; Vector3 down = new Vector3(float.NaN, 0f, 0f); bool up = false;
                yield return TimedLakes(sc, true, run, t => { }, 40f, null, 0f, () =>
                {
                    var p = car.Body.position;
                    if (float.IsNaN(outHead) && ManholeLid.Straightened > st0) { var v = Flat(car.Body.linearVelocity); outHead = Vector3.SignedAngle(street, v, Vector3.up); up = true; }
                    if (up && float.IsNaN(down.x) && car.GroundedWheels >= 2 && Flat(p - mh).magnitude > 3f) down = p;
                });
                float lateral = float.IsNaN(down.x) ? 99f : Mathf.Abs(Vector3.Dot(Flat(down - mh), Vector3.Cross(Vector3.up, street)));
                bool ok = ManholeLid.Bursts > mh0 && !float.IsNaN(outHead) && Mathf.Abs(outHead) < 10f && lateral < 6f;
                if (!ok) bad.Add(Nm(id));
                report.AppendLine($"  {Nm(id),-16} {(ManholeLid.Bursts > mh0 ? "out of MH14" : "NO POP")}; heading off the street's line as it cleared it {outHead:0} deg; first down {(float.IsNaN(down.x) ? "-" : $"at ({down.x:0}, {down.z:0}), {Vector3.Dot(Flat(down - mh), street):0} m along the street, {lateral:0.0} m off its middle")}; {(run.reached ? "on along North Lakeside Drive" : "STOPPED " + run.end)}");
                race.StartRace(); while (race.State == RaceController.Stage.Countdown) yield return null;
            }
            Check("E2: out of MH14 along the street (within 10 deg) and down on the road (within 6 m of the manhole's line)", bad.Count == 0, bad.Count == 0 ? "" : "not: " + string.Join(", ", bad));
            race.EndRace(); RaceController.DirectionOverride = null;
        }

        // ------------------------------------------------------------ pictures
        IEnumerator R54Pics()
        {
            report.AppendLine(); report.AppendLine("=== pictures (free roam)");
            if (race.State != RaceController.Stage.Free) race.EndRace();
            yield return Vehicle("drifter");
            var views = FindAnyObjectByType<CameraViews>();
            foreach (var (nm, x, z, yaw) in new[] { ("bug001_house615", 3219.72f, 504.5f, 188f), ("bug002_suholden", 3142.71f, 242.77f, 21f), ("bug003_terrace", 2921.86f, 410.63f, 120f), ("bug004_bounce", 3146.65f, 162.26f, 56f),
                                                    ("bug005_signs_westbound", 2760f, 515.5f, 270f), ("bug005_signs_northbound", 2648f, 405f, 0f), ("bug007_party_sign", 2997.82f, 1209.19f, 64f), ("bug008_slide", 3020f, 1184.5f, 90f),
                                                    ("bug009_mh15", 700f, 1499f, 270f), ("bug010_mh14", 1196f, 1321f, 287f) })
            {
                var fwd = new Vector3(Mathf.Sin(yaw * Mathf.Deg2Rad), 0f, Mathf.Cos(yaw * Mathf.Deg2Rad));
                if (views) views.SetView(CameraViews.View.FirstPerson);
                Place(RaceRoute.Surface(x, z), fwd, 0f); yield return new WaitForSecondsRealtime(2f);
                yield return Capture($"54_{nm}_dans_view");
                if (views) views.SetView(CameraViews.View.Chase);
                yield return new WaitForSecondsRealtime(1f); yield return Capture($"54_{nm}_chase");
            }
            Vector3 S(float x, float z, float up) => RaceRoute.Surface(x, z) + Vector3.up * up;
            yield return Still("54_A2_terrace_from_below", S(2990f, 372f, 3f), S(2958f, 405f, 2f), 60f);
            yield return Still("54_A2_terrace_side", S(2975f, 412f, 3f), S(2960f, 403f, 0f), 60f);
            yield return Still("54_A3_bounce_house", S(3165f, 168f, 2.5f), S(3192f, 192f, 2f), 60f);
            yield return Still("54_A3_from_above", S(3150f, 140f, 25f), S(3200f, 210f, 0f), 60f);
            yield return Still("54_A1_crest", S(3245f, 303f, 3f), S(3150f, 309f, -6f), 60f);
            yield return Still("54_A1_way", S(3100f, 300f, 6f), S(3085f, 230f, 0f), 60f);
            yield return Still("54_A1_way_end", S(3030f, 200f, 5f), S(2990f, 184f, 0f), 60f);
            yield return Still("54_A1_line", S(2975f, 215f, 4f), S(2987f, 245f, 1f), 60f);
            yield return Still("54_A8_lyndon_hedges", S(2830f, 425f, 18f), S(2812f, 470f, 0f), 60f);
            yield return Still("54_A6_sign_close", S(2715f, 516f, 1.8f), S(2705f, 520.2f, 2f), 45f);
        }
    }
}
