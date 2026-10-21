using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Racer;
using UnityEngine;

namespace ChastainRush
{
    // Round 55 in the exe (-exeRound55 <dir> [-r55Only eprobe,etimes,line,bounce,erace,epics] [-r55PassOff] [-traffic Normal] -logFile <dir>\Player.log):
    //   eprobe     free roam: flat out from Suholden Circle straight on down Elaine Circle and off its west bend at Dan's spot (BUG-002, x 3053
    //              z 309), heading 262, Street Classic and Drifter Twin: the speed off the edge and where each comes down (for planning the landing)
    //   etimes     free roam (no barriers), Street Classic and Drifter Twin, from Suholden Circle 60 m before the Elaine corner to Hayworth Drive at
    //              z 372: the lap's way; off Dan's bend and down the bank onto the Crawford / Hayworth corner; off the bend, across the corner into
    //              the yards beyond and round house 1143 back to Hayworth (the backyard way Dan described): the numbers for A's report
    //   line       B: forward and reverse races start on Embry Lane's east end: the grid on the road, the first checkpoint, the record name
    //   bounce     C: the bounce house thrown into (reverse race): its voice at the castle, the clip it played
    //   erace      E: Addison Heights forward x2 and reverse x2 and Chastain Lakes forward, Hard, 3 AI, traffic as given: every AI finishes; its
    //              passes and the time it spent close behind a car under 10 m/s (-r55PassOff: as before Round 55, for the before numbers)
    //   elaine     A (option 2): the Elaine jump flat out (Street Classic, Drifter Twin) against the lap's way; through the hedge at the corner, off the
//              bench through the hedge, no jump (over the edge slowly), round the barrier: each slower than the lap
//   epics      Dan's spots and the new line from the grid
    public sealed partial class Round20Runner
    {
        static readonly Vector3 ElaineLip = new Vector3(3052f, 0f, 309.5f);
        static Vector3 ElaineDir => new Vector3(Mathf.Sin(262f * Mathf.Deg2Rad), 0f, Mathf.Cos(262f * Mathf.Deg2Rad));

        IEnumerator R55Probe()
        {
            report.AppendLine(); report.AppendLine("=== probe: off Elaine Circle's west bend flat out (free roam)");
            if (race.State != RaceController.Stage.Free) race.EndRace();
            var lead = Pts(3300f, 302.5f, 3266f, 303f, 3218f, 305f, 3208f, 308f, 3197f, 314.5f, 3182f, 314.5f, 3162f, 310f, 3139f, 307f, 3069f, 307f, 3059f, 308f, 3052f, 309.5f);
            var d = ElaineDir;
            var line = lead.ToList(); for (int k = 4; k <= 130; k += 4) { var q = ElaineLip + d * k; line.Add(new Vector3(q.x, RaceRoute.Surface(q.x, q.z).y, q.z)); }
            var lip = ElaineLip;
            foreach (var vid in new[] { "original", "drifter" })
                foreach (float cap in new[] { 99f, 30f, 27f, 24f })
                {
                    yield return Vehicle(vid);
                    Place(line[0], line[2] - line[0], 20f); yield return new WaitForFixedUpdate();
                    var run = new Run(); float lipV = -1f; Vector3 down = new Vector3(float.NaN, 0f, 0f); float air = 0f, best = 0f; bool past = false;
                    yield return DriveLine(line, cap, 40f, run, null, r =>
                    {
                        var p = car.Body.position;
                        if (!past && Vector3.Dot(Flat(p - lip), d) > 0f) { past = true; lipV = car.Body.linearVelocity.magnitude; }
                        if (car.GroundedWheels == 0) air += Time.fixedDeltaTime; else { if (past && air > .4f && float.IsNaN(down.x)) { down = p; best = air; } air = 0f; }
                        return past && !float.IsNaN(down.x) && Flat(p - down).magnitude > 15f;
                    }, 5f, true);
                    report.AppendLine($"  {Nm(vid),-16} cap {cap:0}: off the edge at {lipV:0.0} m/s; {best:0.00} s in the air; first down at {(float.IsNaN(down.x) ? "-" : $"({down.x:0.0}, {down.y:0.0}, {down.z:0.0}), {Flat(down - lip).magnitude:0} m out")}; ended at ({car.Body.position.x:0}, {car.Body.position.z:0}) {run.end}");
                    Flush();
                }
        }

        // ------------------------------------------------------------ A: the numbers (nothing built)
        IEnumerator R55Times()
        {
            report.AppendLine(); report.AppendLine("=== A. off Elaine Circle's west bend (Dan's BUG-002 spot): timed against the lap, free roam (no barriers), nothing built there");
            yield return StartRoute("addison_heights.json", "forward", 0);
            var from = RaceRoute.Surface(3242f, 303f); var to = RaceRoute.Surface(3008.5f, 372f);
            float sa = AbsStation(from), sb = AbsStation(to); if (sb < sa) sb += race.Route.LapLength;
            var lead = LapSlice(sa - 60f, sa); var lapWay = LapSlice(sa - 60f, sb + 30f);
            float sJ = AbsStation(RaceRoute.Surface(2984f, 312f)); if (sJ < sa) sJ += race.Route.LapLength;
            var onLap = LapSlice(sJ, sb + 30f);
            race.EndRace();
            List<Vector3> Via(params float[] xz) { var l = lead.ToList(); l.AddRange(Pts(xz)); return l; }
            var elaine = new[] { 3218f, 305f, 3208f, 308f, 3197f, 314.5f, 3182f, 314.5f, 3162f, 310f, 3139f, 307f, 3069f, 307f, 3059f, 308f, 3052f, 309.5f };
            var off = elaine.Concat(new[] { 3032.2f, 306.7f, 3012.4f, 303.9f }).ToArray();
            var turn = Via(off.Concat(new[] { 2997f, 303.5f, 2988f, 308f }).ToArray()); turn.AddRange(onLap);
            var yards = Via(off.Concat(new[] { 2992.6f, 301.1f, 2975f, 299f, 2956f, 299f, 2944f, 306f, 2942f, 318f, 2943f, 334f, 2946f, 352f, 2962f, 360f, 2984f, 364f, 3001f, 366f }).ToArray()); yards.AddRange(LapSlice(AbsStation(RaceRoute.Surface(3006f, 368f)), sb + 30f));
            yards = Dodge(yards);
            foreach (var vid in new[] { "original", "drifter" })
            {
                yield return Vehicle(vid);
                var times = new List<string>(); float lapT = -1f;
                foreach (var (nm, line) in new[] { ("the lap's way (Suholden, Kelly, Crawford, Hayworth)", lapWay), ("off the bend, down the bank, onto Hayworth at the corner", turn), ("off the bend, across the corner, round 1143 back to Hayworth", yards) })
                {
                    Place(line[0], line[2] - line[0], 18f); yield return new WaitForFixedUpdate();
                    var cum = new float[line.Count]; for (int i = 1; i < line.Count; i++) cum[i] = cum[i - 1] + Flat(line[i] - line[i - 1]).magnitude;
                    float t0 = -1f, t1 = -1f, clock = 0f, lastS = 0f, lipV = -1f, air = 0f, most = 0f; Vector3 down = new Vector3(float.NaN, 0f, 0f); bool past = false;
                    var run = new Run();
                    yield return DriveLine(line, 99f, 90f, run, null, r =>
                    {
                        var p = car.Body.position; clock += Time.fixedDeltaTime;
                        if (t0 < 0f && Flat(p - from).magnitude < 6f) t0 = clock;
                        float s = ProjectNear(line, cum, p, lastS, out _); lastS = s;
                        if (t0 >= 0f && t1 < 0f && Flat(p - to).magnitude < 7f) t1 = clock;
                        if (!past && Vector3.Dot(Flat(p - ElaineLip), ElaineDir) > 0f && Flat(p - ElaineLip).magnitude < 8f) { past = true; lipV = car.Body.linearVelocity.magnitude; }
                        if (car.GroundedWheels == 0) air += Time.fixedDeltaTime; else { if (past && air > .4f && float.IsNaN(down.x)) { down = p; most = air; } air = 0f; }
                        return t1 > 0f;
                    }, 5f, true);
                    float t = t0 >= 0f && t1 > t0 ? t1 - t0 : -1f;
                    if (lapT < 0f) lapT = t;
                    times.Add($"{nm}: {(t > 0f ? $"{t:0.0} s{(line == lapWay ? "" : $" ({t - lapT:+0.0;-0.0} s)")}" : $"did not get there ({run.end} at ({car.Body.position.x:0}, {car.Body.position.z:0}))")}" +
                              (past ? $", off the bend at {lipV:0.0} m/s, {most:0.0} s in the air, down at {(float.IsNaN(down.x) ? "-" : $"({down.x:0}, {down.z:0})")}" : ""));
                    yield return Capture($"55_A_{vid}_{times.Count}_end");
                }
                report.AppendLine($"  {Nm(vid)}:"); foreach (var x in times) report.AppendLine("    " + x);
                Flush();
            }
        }

        // ------------------------------------------------------------ A (option 2): the Elaine jump, its bench, the ways round it
        IEnumerator R55Elaine() => R54Shortcut("f_elaine_jump", "forward", "A. THE ELAINE JUMP (forward, option 2), flat out, timed against the lap's own way over the same ground", sc =>
        {
            float sb = AbsStation(sc.Path[sc.Path.Length - 1]);
            List<Vector3> Then(List<Vector3> l, Vector3 at) { float sa = AbsStation(at); if (sb < sa) sb += race.Route.LapLength; l.AddRange(LapSlice(sa, sb)); return l; }
            var elaine = new[] { 3218f, 305f, 3208f, 308f, 3197f, 314.5f, 3182f, 314.5f, 3162f, 310f, 3139f, 307f, 3069f, 307f, 3059f, 308f, 3052f, 309.5f, 3032.2f, 306.7f, 3012.4f, 303.9f };
            return new List<(string, List<Vector3>, float[])>
            {
                ("off the bend, then straight through the hedge at the bank's foot onto the corner", Then(Pts(elaine.Concat(new[] { 2997f, 303.5f, 2986f, 309f }).ToArray()), RaceRoute.Surface(2986f, 312f)), null),
                ("off the bend, onto the bench, then west through the hedge onto Hayworth at z 330", Then(Pts(elaine.Concat(new[] { 3006f, 309f, 3005f, 318f, 3003f, 326f, 2992f, 331f }).ToArray()), RaceRoute.Surface(2991f, 333f)), null),
                ("no jump: over the edge held to 10 m/s, down the bank, the bench", sc.Path.ToList(), HoldFrom(sc, RaceRoute.Surface(3052f, 309.5f), 10f)),
                ("round the barrier: Elaine north, 960's drive, onto the bench", Then(Dodge(Pts(3218f, 305f, 3208f, 308f, 3197f, 314.5f, 3182f, 314.5f, 3162f, 310f, 3139f, 307f, 3069f, 307f, 3058f, 310f, 3040f, 322f, 3034f, 334f, 3020f, 338f, 3011f, 343f, 3016f, 356f, 3019f, 366f, 3014f, 380f, 3011f, 386f)), RaceRoute.Surface(3011f, 388f)), null),
            };
        }, null, "55");

        // ------------------------------------------------------------ B: the line
        IEnumerator R55Line()
        {
            report.AppendLine(); report.AppendLine("=== B. the start / finish on Embry Lane's east end");
            foreach (var dir in new[] { "forward", "reverse" })
            {
                yield return StartRoute("addison_heights.json", dir, 3);
                var r = race.Route; var g = r.Gate;
                var cars = race.Entrants.Where(e => e.Car).Select(e => e.Car.Body.position).ToList();
                float far = cars.Max(p => Flat(p - g.Position).magnitude), off = cars.Max(p => Mathf.Abs(Vector3.Dot(Flat(p - g.Position), g.Right)));
                Check($"{dir}: the line at ({g.Position.x:0}, {g.Position.z:0}) (Embry Lane's east end), every car on the grid within 60 m behind it and on the road",
                      Vector3.Distance(Flat(g.Position), new Vector3(3246.3f, 0f, 457.5f)) < 6f && far < 62f && off < 5f,
                      $"records as \"{r.RecordName}\"; grid {string.Join(" ", cars.Select(p => $"({p.x:0}, {p.z:0})"))}; first checkpoint {r.Checkpoints[0].Label}, {r.Checkpoints.Count} checkpoints");
                if (views()) views().SetView(CameraViews.View.Chase);
                yield return new WaitForSecondsRealtime(1.5f); yield return Capture($"55_B_{dir}_grid");
                race.EndRace();
            }
            RaceController.DirectionOverride = null;
        }
        CameraViews views() => FindAnyObjectByType<CameraViews>();

        // ------------------------------------------------------------ C: the bounce house's sound
        IEnumerator R55Bounce()
        {
            report.AppendLine(); report.AppendLine("=== C. the bounce house's sound (reverse race, Street Classic, flat out into it)");
            yield return StartRoute("addison_heights.json", "reverse", 0);
            var sc = race.Route.Shortcuts.FirstOrDefault(s => s.Id == "r_bounce_house");
            var bh = FindAnyObjectByType<BounceHouse>();
            if (sc == null || !bh) { Check("the bounce house and its shortcut are in the reverse race", false); race.EndRace(); yield break; }
            yield return Vehicle("original");
            { var pl = race.Player; pl.ClockStarted = true; pl.StartAt = pl.LapStart = race.RaceClock; pl.NextCheckpoint = 0; pl.LapAway = true; }
            int t0 = BounceHouse.Throws; bool heard = false;
            var run = new Run();
            yield return TimedLakes(sc, true, run, t => { }, 60f, null, 0f, () => { if (BounceHouse.Throws > t0 && bh.Voice && bh.Voice.isPlaying) heard = true; });
            var v = bh.Voice; var listener = FindAnyObjectByType<AudioListener>();
            Check("C: thrown, and the trampoline's sound plays from the castle", BounceHouse.Throws > t0 && heard && v && Flat(v.transform.position - bh.front).magnitude < 6f,
                  $"throws {BounceHouse.Throws - t0}; playing at the throw {heard}; clip \"{BounceHouse.LastClip}\" (drop-ins: {(SoundDropIns.Instance ? SoundDropIns.Instance.Bounces.Count : 0)}); the voice {(v ? $"{Flat(v.transform.position - bh.front).magnitude:0.0} m from the arch, reaches {v.maxDistance:0} m" : "none")}");
            race.EndRace(); RaceController.DirectionOverride = null;
        }

        // ------------------------------------------------------------ E: the AI passing
        IEnumerator R55AiRace(string file, string dir, int n, List<string> lines, List<float> behind, List<int> passes)
        {
            report.AppendLine(); report.AppendLine($"=== E. {file} {dir} #{n} on Hard: 3 laps, 3 AI, traffic {(Traffic.Instance ? Traffic.LevelNames[Traffic.Instance.Level] : "none")}, passing {(RoadDriver.PassFix ? "Round 55" : "as before (-r55PassOff)")}");
            int prev = RoadDriver.Difficulty; RoadDriver.Difficulty = 2;
            yield return StartRoute(file, dir, 3);
            var park = file.StartsWith("addison") ? new Vector2(2880f, 313f) : new Vector2(900f, 1420f);
            respawn.PlaceAt(RaceRoute.Surface(park.x, park.y) + Vector3.up * .4f, 90f); race.NotifyTeleported(); car.enabled = false;
            var ai = race.Entrants.Where(e => !e.IsPlayer).ToList();
            Time.timeScale = 2f; Time.maximumDeltaTime = .5f;
            float end = Time.realtimeSinceStartup + 6f * 60f;
            while (Time.realtimeSinceStartup < end && ai.Any(e => !e.Finished)) yield return new WaitForSecondsRealtime(.5f);
            Time.timeScale = 1f;
            foreach (var e in ai)
            {
                var d = e.Driver;
                behind.Add(d ? d.BehindSeconds : 0f); passes.Add(d ? d.Passes : 0);
                Check($"{file} {dir} #{n}: {e.Name} ({e.VehicleName}) finishes", e.Finished && !e.Estimated,
                      $"{(e.Finished ? $"total {e.FinalTotal:0.0} s" : $"not finished, laps {e.CompletedLaps}")}, laps {string.Join(" / ", e.LapTimes.Select(x => x.ToString("0.0")))}; shortcuts {string.Join(", ", e.ShortcutsTaken)}; passes {(d ? d.Passes : 0)}, close behind a car under 10 m/s {(d ? d.BehindSeconds : 0f):0.0} s, resets {(d ? d.RecoveryCount : 0)}");
            }
            car.enabled = true; RoadDriver.Difficulty = prev;
            race.EndRace(); RaceController.DirectionOverride = null;
        }
        IEnumerator R55Race()
        {
            var lines = new List<string>(); var behind = new List<float>(); var passes = new List<int>();
            bool off = System.Environment.GetCommandLineArgs().Contains("-r55PassOff"); RoadDriver.PassFix = !off;
            if (System.Environment.GetCommandLineArgs().Contains("-r55FwdOnly")) { foreach (var (f, d, n) in new[] { ("addison_heights.json", "forward", 1), ("addison_heights.json", "forward", 2) }) yield return Guard(R55AiRace(f, d, n, lines, behind, passes)); yield break; }
            var plan = off ? new[] { ("addison_heights.json", "forward", 1), ("addison_heights.json", "reverse", 1), ("chastain_lakes.json", "forward", 1) }
                           : new[] { ("addison_heights.json", "forward", 1), ("addison_heights.json", "forward", 2), ("addison_heights.json", "reverse", 1), ("addison_heights.json", "reverse", 2), ("chastain_lakes.json", "forward", 1) };
            foreach (var (f, d, n) in plan) yield return Guard(R55AiRace(f, d, n, lines, behind, passes));
            report.AppendLine($"  all races ({(off ? "as before" : "Round 55")}): {passes.Sum()} passes by {passes.Count} AI drivers ({(passes.Count > 0 ? passes.Average() : 0):0.0} each), close behind a car under 10 m/s {behind.Sum():0} s in all ({(behind.Count > 0 ? behind.Average() : 0):0.0} s each)");
            RoadDriver.PassFix = true;
        }

        // ------------------------------------------------------------ pictures
        IEnumerator R55Pics()
        {
            report.AppendLine(); report.AppendLine("=== pictures (free roam)");
            if (race.State != RaceController.Stage.Free) race.EndRace();
            yield return Vehicle("drifter");
            var v = views();
            foreach (var (nm, x, z, yaw) in new[] { ("bug001_hayworth_crawford", 2977.66f, 308.73f, 13.5f), ("bug002_elaine_bend", 3053.14f, 309.33f, 271f), ("bug003_kelly_suholden", 3136.36f, 97.41f, 6.4f) })
            {
                var fwd = new Vector3(Mathf.Sin(yaw * Mathf.Deg2Rad), 0f, Mathf.Cos(yaw * Mathf.Deg2Rad));
                if (v) v.SetView(CameraViews.View.FirstPerson);
                Place(RaceRoute.Surface(x, z), fwd, 0f); yield return new WaitForSecondsRealtime(2f);
                yield return Capture($"55_{nm}_dans_view");
                if (v) v.SetView(CameraViews.View.Chase);
                yield return new WaitForSecondsRealtime(1f); yield return Capture($"55_{nm}_chase");
            }
            Vector3 S(float x, float z, float up) => RaceRoute.Surface(x, z) + Vector3.up * up;
            yield return Still("55_A_the_drop_from_the_bend", S(3058f, 309.6f, 2.2f), S(2985f, 302f, 0f), 60f);
            yield return Still("55_A_the_bank_from_the_corner", S(2965f, 300f, 4f), S(3035f, 308f, 8f), 60f);
        }
    }
}
