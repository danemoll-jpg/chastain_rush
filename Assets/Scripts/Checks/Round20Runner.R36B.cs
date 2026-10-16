using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Racer;
using UnityEngine;

namespace ChastainRush
{
    // Round 36 Part B in the exe: ChastainRush.exe -exeRound36 <dir> -r36Only measure,acts,mappic -logFile <dir>\Player.log.
    //   measure  every speed trap flat out on Racer's four base vehicles (the Street Classic, the Longroof GT, the Needle 600, the Trail Four), 400 m
    //            run-in along its road: the speed through it, and the medal targets made from it (targets.json: gold 95 %, silver 85 %, bronze 72 %)
    //   acts     one of each kind run by script and scored: a speed trap (flat out), a jump (its launch flat out), a smash run (a timed attempt, its
    //            props driven through): the result, the medal, the record written (the check's own activity files)
    //   mappic   the big map with the sites (and the stunt park), a picture
    public sealed partial class Round20Runner
    {
        static readonly string[] BaseIds = { "original", "tourer", "moto", "atv" };
        IEnumerator R36Measure()
        {
            report.AppendLine(); report.AppendLine("=== Part B: the speed traps flat out on the four base vehicles (the medal targets from these runs)");
            var a = car.GetComponent<ArcadeActivities>(); if (!a || a.Sites.Length == 0) { Check("the Free Roam activities are in the build", false); yield break; }
            if (race.State != RaceController.Stage.Free) race.EndRace();
            var lines = new List<string>();
            foreach (var s in a.Sites.Where(x => x.kind == ActivitySite.Kind.Speed))
            {
                var speeds = new float[4];
                for (int k = 0; k < 4; k++)
                {
                    yield return Vehicle(BaseIds[k]);
                    // along the road: RoadLookup's line through the trap, 400 m back, 30 m on
                    var f = Flat(s.forward).normalized; var line = new List<Vector3>();
                    for (float d = -400f; d <= 30f; d += 5f) { var p = s.transform.position + f * d; line.Add(RaceRoute.Surface(p.x, p.z)); }
                    var road = RoadLookup.Instance;
                    if (road != null) for (int i = 0; i < line.Count; i++) if (road.NearestPoint(line[i], out var c, out _)) line[i] = RaceRoute.Surface(c.x, c.y);
                    Place(line[0], line[2] - line[0], 10f); yield return new WaitForSecondsRealtime(.2f);
                    float before = a.LastSpeed; var run = new Run();
                    yield return StartCoroutine(DriveLine(line, 99f, 40f, run));
                    speeds[k] = a.LastSpeed != before ? a.LastSpeed : 0f;
                }
                report.AppendLine($"  {s.title,-28} " + string.Join("   ", BaseIds.Select((id, i) => $"{Nm(id)} {MedalUi.Mph(speeds[i])}")));
                Check($"{s.title}: every base vehicle's crossing was timed", speeds.All(v => v > 5f), string.Join(", ", speeds.Select(v => v.ToString("0.0"))));
                string F(float[] v, float k) => "[" + string.Join(", ", v.Select(x => (x * k).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture))) + "]";
                lines.Add($"  \"{s.id}\": {{\"vg\": {F(speeds, .95f)}, \"vs\": {F(speeds, .85f)}, \"vb\": {F(speeds, .72f)}}}");
            }
            File.WriteAllText(Path.Combine(outputDir, "targets.json"), "{\n" + string.Join(",\n", lines) + "\n}\n");
        }

        IEnumerator R36Acts()
        {
            report.AppendLine(); report.AppendLine("=== Part B: one of each kind, by script: scored and medaled");
            var a = car.GetComponent<ArcadeActivities>(); if (!a || a.Sites.Length == 0) { Check("the Free Roam activities are in the build", false); yield break; }
            if (race.State != RaceController.Stage.Free) race.EndRace();
            report.AppendLine($"  {a.Sites.Length} sites: " + string.Join(", ", a.Sites.Select(s => $"{s.title} ({s.kind})")));
            // a speed trap: the first, flat out on the Needle 600
            var trap = a.Sites.First(s => s.kind == ActivitySite.Kind.Speed); yield return Vehicle("moto");
            var f = Flat(trap.forward).normalized; var line = new List<Vector3>(); for (float d = -350f; d <= 30f; d += 5f) { var p = trap.transform.position + f * d; line.Add(RaceRoute.Surface(p.x, p.z)); }
            var road = RoadLookup.Instance; if (road != null) for (int i = 0; i < line.Count; i++) if (road.NearestPoint(line[i], out var c, out _)) line[i] = RaceRoute.Surface(c.x, c.y);
            int awards = a.Awards; Place(line[0], line[2] - line[0], 10f); yield return new WaitForSecondsRealtime(.2f);
            yield return StartCoroutine(DriveLine(line, 99f, 40f, new Run()));
            yield return Capture("36b_speed_trap_result");
            Check($"speed trap ({trap.title}): scored and medaled", a.Awards > awards && a.LastMedal >= 0, $"{MedalUi.Mph(a.LastSpeed)}, {MedalUi.Name(a.LastMedal)}, '{a.Feedback?.Replace("\n", " / ")}'");
            // a jump: BIG AIR (the stunt park), timed attempt, on the Clay Hopper
            var js = a.Sites.FirstOrDefault(s => s.id == "jump-park-mega") ?? a.Sites.First(s => s.kind == ActivitySite.Kind.Jump);
            yield return Vehicle("stuntbuggy"); a.Select(js); a.BeginAttempt();
            awards = a.Awards; var ra = Roof(3398f, 410f); var rb = Floor(3570f, 410f);
            Place(ra, rb - ra, 4f); yield return new WaitForSecondsRealtime(.5f);
            yield return StartCoroutine(DriveInput(Straight(ra, new Vector3(rb.x, ra.y, rb.z)), 30f, 25f, new Run()));
            yield return Capture("36b_jump_result");
            Check($"jump ({js.title}): a timed attempt, landed, scored and medaled", a.Awards > awards && a.LastJumpAward > 0, $"{MedalUi.Feet(a.LastJumpAward)}, medal {MedalUi.Name(a.LastMedal)}, '{a.Feedback?.Replace("\n", " / ")}' {a.LastJumpDiagnostic}");
            // a smash run: the pumps, a timed attempt: drive past each prop
            var sm = a.Sites.FirstOrDefault(s => s.id == "smash-pumps") ?? a.Sites.First(s => s.kind == ActivitySite.Kind.Smash);
            yield return Vehicle("original"); a.Select(sm); a.BeginAttempt(); awards = a.Awards;
            var props = Enumerable.Range(0, sm.PropCount).Select(i => sm.PropAt(i)).ToList();
            var at = props[0]; foreach (var q in props.Skip(1)) { }
            var route = new List<Vector3>(); var startP = props.Aggregate(Vector3.zero, (s2, q) => s2 + q) / props.Count;
            foreach (var q in props.OrderBy(q => Vector3.Dot(q - startP, Vector3.right))) route.Add(RaceRoute.Surface(q.x, q.z));
            var first = route[0] - (route[route.Count - 1] - route[0]).normalized * 25f; route.Insert(0, RaceRoute.Surface(first.x, first.z));
            var last = route[route.Count - 1] + (route[route.Count - 1] - route[1]).normalized * 15f; route.Add(RaceRoute.Surface(last.x, last.z));
            Place(route[0], route[1] - route[0], 6f); yield return new WaitForSecondsRealtime(.2f);
            yield return StartCoroutine(DriveLine(route, 9f, 25f, new Run()));
            float wait = 0f; while (a.AttemptActive && wait < sm.Seconds + 2f) { wait += Time.deltaTime; yield return null; }
            yield return Capture("36b_smash_result");
            Check($"smash run ({sm.title}): a timed attempt, its props counted, scored and medaled", a.Awards > awards, $"{a.SmashCount} of {sm.PropCount}, '{a.Feedback?.Replace("\n", " / ")}', medal {MedalUi.Name(a.LastMedal)}");
            Check("the results are kept (the check's own activities-v1.json and activity-records-v2.json)", File.Exists(Path.Combine(CheckRun.Dir, "activities-v1.json")) && File.Exists(Path.Combine(CheckRun.Dir, "activity-records-v2.json")));
        }

        IEnumerator R36MapPic()
        {
            report.AppendLine(); report.AppendLine("=== Part B: the map with the sites");
            var map = WorldMap.Instance; if (!map) { Check("the map is there", false); yield break; }
            map.Open();
            yield return new WaitForSecondsRealtime(1.2f);
            yield return Capture("36b_map_with_sites");
            Check("the map opened with the sites on it", WorldMap.IsOpen && WorldMap.Places.Count > 5, $"{WorldMap.Places.Count} places");
            map.Close(); yield return new WaitForSecondsRealtime(.5f);
        }
    }
}
