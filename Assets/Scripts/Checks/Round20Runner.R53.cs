using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Racer;
using UnityEngine;

namespace ChastainRush
{
    // Round 53 in the exe (-exeRound53 <dir> [-r53Only sclean,scheat,sshort,sflag,sjunction,srace,srevrace,ahshut,spics] [-traffic Normal]
    // -logFile <dir>\Player.log):
    //   sclean     A (the Lyndon road crew) flat out on its line (Street Classic, Drifter Twin): off the planks, air, where it comes down, any time on the
    //              wet concrete; time against the lap's own way over the same ground (B, the Elaine jump, is not built: TODO.md)
    //   scheat     the ground WITHOUT the stunt (Dan's standing rule of 2026-10-19), each slower than the lap's own way: round the trailer on its left
    //              (the flagger's side) and on its right, through the west yards, through the east yards
    //   sshort     off the planks at 15 and 18 m/s (into the concrete): each slower
    //   sflag      a car driven at the flagger: he steps off the road; and off the launch line he is never touched
    //   sjunction  D: the merge from Lyndon onto Embry at 14 m/s and Embry through the junction at 22 m/s: the biggest change of vertical speed in one
    //              step and the fewest wheels down (before, Round 52's build: 1.24 m/s and 0 wheels on the merge; 1.60 m/s along Embry)
    //   srace      the forward race twice, 3 AI, the player parked off the course (with -traffic Normal): everyone finishes, 0 missed; who takes what
    //   srevrace   the reverse race once, 3 AI: everyone finishes (both shortcuts shut)
    //   ahshut     Round 52's barrier drive, both directions (in a forward race Lyndon is open between Joel Drive and Embry Lane; in reverse it stays shut)
    //   spics      Dan's five spots as he stood (BUG-001 to BUG-005), the crew site and the flagger, the concrete after a drive through it, the house
    public sealed partial class Round20Runner
    {
        static readonly Vector3 R53Lip = new Vector3(2813.0f, 0f, 458.8f), R53Foot = new Vector3(2813.19f, 0f, 449.0f);   // make_addison_route.py T_LIP, T_FOOT
        RaceShortcut AddSc(string id) => race.Route.Shortcuts.FirstOrDefault(s => s.Id == id);
        WetConcrete Slab => FindAnyObjectByType<WetConcrete>();

        sealed class R53Watch { public float lipV = -1f, air, cur, wetTime, wetSpeedIn = -1f, bedMax; public Vector3 land = new Vector3(float.NaN, 0f, 0f); public bool past, landed; }
        void R53Track(R53Watch w, Vector3 lip, Vector3 dir)
        {
            var p = car.Body.position;
            if (!w.past && Vector3.Dot(Flat(p - lip), dir) > 0f && Flat(p - lip).magnitude < 6f) { w.past = true; w.lipV = car.Body.linearVelocity.magnitude; }
            bool down = car.GroundedWheels >= 2;
            if (down && Slab && Slab.Contains(p)) { if (w.wetSpeedIn < 0f) w.wetSpeedIn = Flat(car.Body.linearVelocity).magnitude; w.wetTime += Time.fixedDeltaTime; }
            if (down) w.bedMax = Mathf.Max(w.bedMax, ShrubBeds.CoverageAt(p));
            if (!w.past || w.landed) return;
            if (car.GroundedWheels == 0) w.cur += Time.fixedDeltaTime; else if (w.cur > .25f) { w.landed = true; w.air = w.cur; w.land = p; }
        }
        static string R53Line(R53Watch w, Vector3 lip, Vector3 dir) =>
            $"off it at {w.lipV:0.0} m/s, {w.air:0.00} s in the air, down {(float.IsNaN(w.land.x) ? "-" : $"{Vector3.Dot(Flat(w.land - lip), dir):0} m on at ({w.land.x:0}, {w.land.z:0})")}; " +
            $"on the wet concrete {w.wetTime:0.0} s{(w.wetSpeedIn >= 0f ? $" (in at {w.wetSpeedIn:0.0} m/s)" : "")}; deepest in a shrub bed {w.bedMax:0.00}";

        IEnumerator StartAddisonRace(string dir = "forward") { yield return StartAddison(dir, 0); }

        // ------------------------------------------------------------ clean runs
        IEnumerator R53Clean()
        {
            report.AppendLine(); report.AppendLine("=== THE LYNDON ROAD CREW, flat out on its line, timed against the lap's own way over the same ground");
            yield return StartAddisonRace();
            var dirA = Flat(R53Lip - R53Foot).normalized;
            foreach (var (scId, lip, dir) in new[] { ("f_lyndon_crew", R53Lip, dirA) })
            {
                var sc = AddSc(scId); if (sc == null) { Check($"{scId} is in the forward race", false); continue; }
                report.AppendLine($"  {sc.Name}: forgives {string.Join(", ", race.Route.Checkpoints.Where((c, i) => i >= sc.FirstForgiven && i <= sc.LastForgiven).Select(c => c.Label))}");
                var bad = new List<string>();
                foreach (var id in new[] { "original", "drifter" })
                {
                    yield return Vehicle(id);
                    float road = -1f, via = -1f; var r0 = new Run(); var r1 = new Run(); var w = new R53Watch();
                    race.StartRace(); while (race.State == RaceController.Stage.Countdown) yield return null;
                    yield return TimedLakes(sc, false, r0, t => road = t);
                    race.StartRace(); while (race.State == RaceController.Stage.Countdown) yield return null;
                    yield return TimedLakes(sc, true, r1, t => via = t, 60f, null, 0f, () => R53Track(w, lip, dir));
                    if (id == "original") yield return Capture($"53_{scId}_clean_end");
                    float saved = road > 0f && via > 0f ? road - via : -99f;
                    bool ok = r1.reached && saved > 0f && w.wetTime < 0.05f && w.bedMax < 0.3f;
                    if (!ok) bad.Add(Nm(id));
                    report.AppendLine($"  {Nm(id),-16} the lap's way {road:0.0} s; the shortcut {via:0.0} s, saves {saved:0.0} s{(r1.reached ? "" : "; STOPPED " + r1.end + $" at ({car.Body.position.x:0}, {car.Body.position.z:0})")}; " +
                                      $"most off its line {r1.maxDev:0.0} m; {R53Line(w, lip, dir)}");
                }
                Check($"{sc.Name} flat out (Street Classic, Drifter Twin): through clean (never on the concrete or in a bed), faster than the lap's way", bad.Count == 0, bad.Count == 0 ? "" : "not: " + string.Join(", ", bad));
            }
            race.EndRace(); RaceController.DirectionOverride = null;
        }

        // the line with its points near 'at' moved 'side' m to the left of 'dir' (eased in and out over 12 m): round the trailer, not up it
        static List<Vector3> R53Beside(Vector3[] path, Vector3 foot, Vector3 dir, float len, float side)
        {
            var left = Vector3.Cross(dir, Vector3.up).normalized; var o = new List<Vector3>();
            foreach (var q in path)
            {
                float s = Vector3.Dot(Flat(q - foot), dir);
                float w = s < -14f ? 0f : s < -2f ? Mathf.SmoothStep(0f, 1f, (s + 14f) / 12f) : s <= len + 3f ? 1f : s < len + 15f ? Mathf.SmoothStep(1f, 0f, (s - len - 3f) / 12f) : 0f;
                if (Flat(q - foot).magnitude > 60f) w = 0f;
                var p = q + left * side * w; o.Add(RaceRoute.Surface(p.x, p.z));
            }
            return o;
        }

        IEnumerator R53Cheat()
        {
            report.AppendLine(); report.AppendLine("=== THE SHORTCUT'S GROUND WITHOUT THE JUMP (Street Classic, flat out): each must be slower than the lap's own way");
            yield return StartAddisonRace();
            yield return Vehicle("original");
            var faster = new List<string>();
            var A = AddSc("f_lyndon_crew");
            if (A == null) { Check("the Lyndon road crew is in the forward race", false); race.EndRace(); yield break; }
            var dirA = Flat(R53Lip - R53Foot).normalized;
            float roadA = -1f;
            { var r = new Run(); race.StartRace(); while (race.State == RaceController.Stage.Countdown) yield return null; yield return TimedLakes(A, false, r, t => roadA = t); }
            report.AppendLine($"  the lap's own way past Lyndon Drive {roadA:0.0} s");
            var lyn = A.Path;
            var runs = new List<(string name, RaceShortcut sc, float road, List<Vector3> line)>
            {
                ("round the trailer on its left (between it and the west curb, past the flagger), on through the concrete", A, roadA, R53Beside(lyn, R53Foot, dirA, 9.8f, 2.6f)),
                ("round the trailer on its right (between it and the east curb), on through the concrete", A, roadA, R53Beside(lyn, R53Foot, dirA, 9.8f, -2.4f)),
                ("through the west yards (the shrub beds) beside the concrete", A, roadA, Dodge(R53Beside(lyn, R53Foot, dirA, 50f, 9.5f))),
                ("through the east yards (round the parked truck and roller, the shrub beds) beside the concrete", A, roadA, Dodge(R53Beside(lyn, R53Foot, dirA, 50f, -10.5f))),
            };
            int n = 0;
            foreach (var (name, sc, road, line) in runs)
            {
                var r = new Run(); float t = -1f; var w = new R53Watch();
                race.StartRace(); while (race.State == RaceController.Stage.Countdown) yield return null;
                yield return TimedVia(sc, line, r, x => t = x, () => R53Track(w, R53Lip, dirA));
                yield return Capture($"53_cheat_{++n}_end");
                bool slower = !r.reached || t < 0f || t > road;
                if (!slower) faster.Add(name);
                report.AppendLine($"  {n}. {name}: {(r.reached && t > 0f ? $"{t:0.0} s ({t - road:+0.0;-0.0} s against the lap's way)" : "did not get through (" + r.end + $", at ({car.Body.position.x:0}, {car.Body.position.z:0}))")}; " +
                                  $"on the wet concrete {w.wetTime:0.0} s, deepest in a bed {w.bedMax:0.00}, slowest {r.minSpeed:0.0} m/s, longest air {r.maxAir:0.0} s");
            }
            Check("every way over the shortcut's ground without the jump is slower than the plain lap", faster.Count == 0, faster.Count == 0 ? "" : "faster: " + string.Join("; ", faster));
            race.EndRace(); RaceController.DirectionOverride = null;
        }

        IEnumerator R53Short()
        {
            report.AppendLine(); report.AppendLine("=== SHORT / MISSED (Street Classic): each must lose more than the shortcut saves (slower than the lap's own way)");
            yield return StartAddisonRace();
            yield return Vehicle("original");
            var A = AddSc("f_lyndon_crew");
            if (A == null) { Check("the Lyndon road crew is in the forward race", false); race.EndRace(); yield break; }
            var dirA = Flat(R53Lip - R53Foot).normalized; bool all = true;
            float roadA = -1f; { var r = new Run(); race.StartRace(); while (race.State == RaceController.Stage.Countdown) yield return null; yield return TimedLakes(A, false, r, t => roadA = t); }
            foreach (float v in new[] { 15f, 18f })
            {
                var r = new Run(); float t = -1f; var w = new R53Watch();
                race.StartRace(); while (race.State == RaceController.Stage.Countdown) yield return null;
                var lip = A.Path.OrderBy(q => Flat(q - R53Lip).sqrMagnitude).First();
                yield return TimedLakes(A, true, r, x => t = x, 60f, lip, v, () => R53Track(w, R53Lip, dirA));
                yield return Capture($"53_short_A_{v:0}");
                bool slower = !r.reached || t < 0f || t > roadA; all &= slower;
                report.AppendLine($"  A off the planks held to {v:0} m/s: {(r.reached && t > 0f ? $"{t:0.0} s ({t - roadA:+0.0;-0.0} s against the lap's way {roadA:0.0} s)" : "not through: " + r.end)}; {R53Line(w, R53Lip, dirA)}; ruts in the slab {(Slab ? Slab.RutsMade : 0)}");
            }
            Check("short off the planks (15 and 18 m/s): each slower than the lap's own way", all);
            race.EndRace(); RaceController.DirectionOverride = null;
        }

        IEnumerator R53Flag()
        {
            report.AppendLine(); report.AppendLine("=== THE FLAGGER (free roam)");
            if (race.State != RaceController.Stage.Free) race.EndRace();
            yield return Vehicle("original");
            var f = FindAnyObjectByType<Flagger>(); if (!f) { Check("the flagger is there", false); yield break; }
            var home = f.transform.position; var dirA = Flat(R53Lip - R53Foot).normalized;
            float off = Vector3.Dot(Flat(home - R53Foot), Vector3.Cross(Vector3.up, dirA)) * -1f;   // + : left of the launch line
            report.AppendLine($"  at ({home.x:0.0}, {home.z:0.0}): {Vector3.Dot(Flat(home - R53Foot), dirA):+0.0;-0.0} m along the trailer from its foot, {off:0.0} m left of its middle (its edge 1.3 m)");
            // the launch line: past him at speed, never into him
            var line = Pts(2812.3f, 425f, 2813.2f, 440f, R53Foot.x, R53Foot.z, R53Lip.x, R53Lip.z);
            int d0 = f.Dodges; var r = new Run(); float nearest = 99f;
            Place(line[0], line[2] - line[0], 10f);
            yield return DriveLine(line, 20f, 10f, r, null, x => { nearest = Mathf.Min(nearest, Flat(car.Body.position - f.transform.position).magnitude); return false; });
            Check("A: the launch line passes the flagger, 2 m or more from him", nearest > 2f, $"nearest {nearest:0.0} m; he stepped aside {f.Dodges - d0} times");
            // at him: he steps off the road onto the lawn
            yield return new WaitForSecondsRealtime(4.5f);
            var at = Pts(2811.0f, 420f, home.x, home.z - 2f, home.x, home.z + 6f);
            d0 = f.Dodges; Place(at[0], at[2] - at[0], 8f); var r2 = new Run(); float near2 = 99f;
            yield return DriveLine(at, 14f, 6f, r2, null, x => { near2 = Mathf.Min(near2, Flat(car.Body.position - f.transform.position).magnitude); return false; });
            yield return Capture("53_flagger_steps_aside");
            Check("A: a car driven at the flagger: he steps off the road", f.Dodges > d0 && near2 > 1.0f, $"stepped aside {f.Dodges - d0} times; nearest {near2:0.0} m");
        }

        IEnumerator R53Junction()
        {
            report.AppendLine(); report.AppendLine("=== D. THE LYNDON / EMBRY JUNCTION (free roam; Round 52's build: merge 1.24 m/s, 0 wheels down; Embry 1.60 m/s)");
            if (race.State != RaceController.Stage.Free) race.EndRace();
            yield return Vehicle("drifter");
            foreach (var (name, line, v) in new[] { ("the merge from Lyndon onto Embry at 14 m/s", Pts(2811.0f, 490f, 2811.2f, 503f, 2812.4f, 510f, 2815.5f, 514.5f, 2821f, 516.2f, 2840f, 517.5f, 2860f, 518.5f), 14f),
                                                    ("Embry Lane through the junction at 22 m/s", Pts(2760f, 514.8f, 2790f, 515.9f, 2811f, 516.4f, 2830f, 517.2f, 2860f, 518.6f), 22f) })
            {
                Place(line[0], line[2] - line[0], v); var r = new Run(); float most = 0f, prev = car.Body.linearVelocity.y; Vector3 at = default; int fewest = 4;
                yield return DriveLine(line, v, 10f, r, null, x =>
                {
                    var p = car.Body.position; float vy = car.Body.linearVelocity.y;
                    if (p.z > 500f && p.z < 530f && p.x > 2795f && p.x < 2830f) { if (Mathf.Abs(vy - prev) > most) { most = Mathf.Abs(vy - prev); at = p; } fewest = Mathf.Min(fewest, car.GroundedWheels); }
                    prev = vy; return false;
                });
                Check($"D: {name}: all wheels down, no jolt over 0.6 m/s", fewest >= 3 && most < 0.6f, $"biggest change of vertical speed in one step {most:0.00} m/s at ({at.x:0.0}, {at.z:0.0}); fewest wheels down {fewest}; slowest {r.minSpeed:0.0} m/s");
            }
        }

        IEnumerator R53Race(string dir, int n)
        {
            report.AppendLine(); report.AppendLine($"=== the {dir} race #{n}: 3 laps, 3 AI, traffic {(Traffic.Instance ? Traffic.LevelNames[Traffic.Instance.Level] : "none")}, the player parked off the course");
            yield return StartAddison(dir, 3);
            respawn.PlaceAt(RaceRoute.Surface(2880f, 313f) + Vector3.up * .4f, 90f); race.NotifyTeleported();
            var ai = race.Entrants.Where(e => !e.IsPlayer).ToList(); var wet = new HashSet<string>(); var lines = new List<string>();
            Time.timeScale = 2f; Time.maximumDeltaTime = .5f;
            float end = Time.realtimeSinceStartup + 9f * 60f;
            while (Time.realtimeSinceStartup < end && ai.Any(e => !e.Finished))
            {
                foreach (var e in ai)
                {
                    if (!e.Car || e.Car.GroundedWheels < 2 || !Slab || !Slab.Contains(e.Car.Body.position)) continue;
                    string k = e.Name + " lap " + (e.CompletedLaps + 1); if (wet.Add(k)) lines.Add($"{e.Name} ({e.VehicleName}) in the wet concrete, lap {e.CompletedLaps + 1}");
                }
                yield return new WaitForSecondsRealtime(.1f);
            }
            Time.timeScale = 1f;
            foreach (var e in ai)
                Check($"{dir} #{n}: {e.Name} ({e.VehicleName}) finishes, 0 missed", e.Finished && e.MissedTotal == 0,
                      $"{(e.Finished ? $"total {e.FinalTotal:0.0} s" : $"not finished, laps {e.CompletedLaps}")}, laps {string.Join(" / ", e.LapTimes.Select(x => x.ToString("0.0")))}, missed {e.MissedTotal}, " +
                      $"shortcuts {(e.ShortcutsTaken.Count > 0 ? string.Join(", ", e.ShortcutsTaken) : "none")}");
            report.AppendLine($"INFO  {(lines.Count > 0 ? string.Join("; ", lines) : "no AI driver in the wet concrete")}");
            race.EndRace(); RaceController.DirectionOverride = null;
        }

        IEnumerator R53Pics()
        {
            report.AppendLine(); report.AppendLine("=== pictures (free roam)");
            if (race.State != RaceController.Stage.Free) race.EndRace();
            yield return Vehicle("drifter");
            var views = FindAnyObjectByType<CameraViews>();
            foreach (var (nm, x, z, yaw) in new[] { ("bug001", 2809.71f, 512.68f, 61.8f), ("bug002", 2812.09f, 419.97f, 2.4f), ("bug003", 2810.37f, 511.52f, 24.9f), ("bug004", 3201.59f, 328.73f, 80f), ("bug005", 3194.96f, 306.83f, 285.7f) })
            {
                var fwd = new Vector3(Mathf.Sin(yaw * Mathf.Deg2Rad), 0f, Mathf.Cos(yaw * Mathf.Deg2Rad));
                if (views) views.SetView(CameraViews.View.FirstPerson);
                Place(RaceRoute.Surface(x, z), fwd, 0f); yield return new WaitForSecondsRealtime(2f);
                yield return Capture($"53_{nm}_dans_view");
                if (views) views.SetView(CameraViews.View.Chase);
                yield return new WaitForSecondsRealtime(1f); yield return Capture($"53_{nm}_chase");
            }
            Vector3 S(float x, float z, float up) => RaceRoute.Surface(x, z) + Vector3.up * up;
            yield return Still("53_A_crew_site", S(2806f, 428f, 3.2f), S(2813f, 452f, 1.5f), 60f);
            yield return Still("53_A_flagger_close", S(2810.5f, 438f, 1.7f), S(2810f, 447f, 1.6f), 45f);
            yield return Still("53_A_trailer_side", S(2803f, 455f, 2.5f), S(2813f, 455f, 1.0f), 60f);
            yield return Still("53_A_concrete", S(2812f, 500f, 4.5f), S(2812f, 472f, 0f), 60f);
            yield return Still("53_A_from_above", S(2830f, 440f, 22f), S(2812f, 478f, 0f), 60f);
            // a drive through the slab (its ruts), then the slab again
            Place(S(2811.8f, 455f, 0f), Vector3.forward, 6f); var r = new Run(); yield return DriveLine(Pts(2811.8f, 455f, 2811.4f, 495f), 8f, 12f, r);
            yield return Still("53_A_concrete_ruts", S(2812f, 500f, 4.5f), S(2812f, 472f, 0f), 60f);
            yield return Still("53_C_house_1089", S(3188f, 322f, 3f), S(3212f, 332f, 2f), 60f);
            yield return Still("53_D_junction", S(2800f, 498f, 4f), S(2815f, 516f, 0f), 60f);
            Check("pictures written", true, "53_*.png");
        }
    }
}
