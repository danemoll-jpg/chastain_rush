using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Racer;
using UnityEngine;

namespace ChastainRush
{
    // Round 21, checked in the exe (ChastainRush.exe -exeRound21 <dir> [-r21Only r1,six,races] -logFile <dir>\Player.log), with this
    // runner's shared parts. Part A:
    //   r1     (Round20Runner.Follow.cs, widened for Round 21) R1 with the near step: the speed window from 15 m/s, the AI clean and sloppy
    //   six    #6 still runs clean beside the near step (forward): an AI driver made to take it, every vehicle
    //   races  whole races with three AI drivers, the player parked: the Chastain Glen loop reverse on Easy, Normal and Hard, forward
    //          on Normal, Noonday Creek forward on Normal. How often each AI mistake happened, R1 taken / sloppy / in the pool, the
    //          shortcut choices with their chances, recoveries and penalties. And no fixed AI chance on any shortcut but N5.
    public sealed partial class Round20Runner
    {
        IEnumerator Six(string[] ids)
        {
            report.AppendLine(); report.AppendLine("=== Round 21: #6 (forward) still clean beside R1's near step: an AI driver made to take it, each vehicle");
            UseRoute("chastain_glen_loop.json", "forward");
            Time.timeScale = 4f; Time.maximumDeltaTime = 1f; int prev = RaceController.AiCount; var bad = new List<string>();
            foreach (var id in ids)
            {
                var r = new AiResult(); yield return AiThrough("sc6_over_house", null, id, null, r);
                bool ok = r.took && r.back && r.given && r.recoveries == 0;
                if (!ok) bad.Add(Nm(id));
                report.AppendLine($"  AI {Nm(id),-17} {(ok ? "OK  " : "FAIL")} took it {(r.took ? "yes" : "no")}, CP forgiven {(r.given ? "yes" : "no")}, recoveries {r.recoveries}{(r.back ? ", back on the lap" : ", ended at " + r.where)}");
            }
            RaceController.AiCount = prev; RaceController.AiVehiclesOverride = null; Time.timeScale = 1f; Time.maximumDeltaTime = 0.3333333f;
            Check("#6 forward: every vehicle over 1641 and back on the lap, forgiven, no rescue (R1's step reaches 0.9 m into its corridor)", bad.Count == 0, bad.Count > 0 ? "not: " + string.Join(", ", bad) : "");
        }

        IEnumerator Races()
        {
            report.AppendLine(); report.AppendLine("=== Round 21: whole races, three AI drivers each, the player parked off the course");
            // the AI shortcut rule: no fixed chance on any shortcut but N5 (part of the lap)
            {
                var fixedOnes = new List<string>(); var lines = new List<string>();
                foreach (var file in new[] { "chastain_glen_loop.json", "noonday_creek.json" })
                {
                    UseRoute(file, "forward"); var j = race.RouteData;   // the copy embedded in the scene (the exe's)
                    foreach (var sc in j.official_shortcuts ?? new RouteShortcutJson[0])
                    {
                        lines.Add($"{sc.id} {(sc.ai_take > 0f ? $"fixed {sc.ai_take:0.00}" : "the rule")}{(sc.lap_option ? " (a lap option)" : "")}");
                        if (sc.ai_take > 0f && sc.id != "n5_ford_leap") fixedOnes.Add(sc.id);
                    }
                }
                float front = Mathf.Clamp(.12f, .06f, .65f), last = Mathf.Clamp(.12f + .34f + .12f, .06f, .65f);
                report.AppendLine($"  AI chances: {string.Join("; ", lines)}. The rule (ShortcutStrategy.Probability): {front:P0} at the front, {last:P0} when last and 200 m behind (each driver's own lean +/-4%), kept to 6 to 65%");
                Check("the AI shortcut rule: no shortcut has a fixed AI chance except N5 (part of the lap)", fixedOnes.Count == 0, fixedOnes.Count > 0 ? string.Join(", ", fixedOnes) : "");
            }
            int prevDiff = RoadDriver.Difficulty;
            var plan = new List<(string file, string dir, int diff)> { ("chastain_glen_loop.json", "reverse", 0), ("chastain_glen_loop.json", "reverse", 1), ("chastain_glen_loop.json", "reverse", 2), ("chastain_glen_loop.json", "forward", 1), ("noonday_creek.json", "forward", 1) };
            var argv = Environment.GetCommandLineArgs();
            for (int i = 0; i + 1 < argv.Length; i++) if (argv[i] == "-r21Races") plan = argv[i + 1].Split(';').Select(x => { var q = x.Split(':'); return (q[0], q[1], int.Parse(q[2])); }).ToList();
            var total = new int[DriverVariation.Kinds.Length]; int stuckRaces = 0;
            foreach (var (file, dir, diff) in plan) yield return OneRace(file, dir, diff, total, () => stuckRaces++);
            RoadDriver.Difficulty = prevDiff;
            report.AppendLine($"  All races: {string.Join(", ", DriverVariation.Kinds.Select((k, i) => $"{k} {total[i]}"))}");
            Check("the AI makes mistakes (every kind seen at least once over the races) and every AI driver finished every race", total.All(n => n > 0) && stuckRaces == 0, stuckRaces > 0 ? $"{stuckRaces} race(s) with a driver that did not finish" : "");
        }

        static readonly string[] DiffName = { "Easy", "Normal", "Hard" };

        IEnumerator OneRace(string file, string dir, int diff, int[] total, Action unfinished)
        {
            UseRoute(file, dir); RoadDriver.Difficulty = diff;
            RaceController.AiCount = 3; RaceController.AiVehiclesOverride = null;
            yield return Vehicle(race.RouteData.vehicles != null && race.RouteData.vehicles.Length > 0 ? race.RouteData.vehicles[0] : "original");
            race.StartRace();
            while (race.State == RaceController.Stage.Countdown) yield return null;
            var park = race.Route.PointAt(race.Route.GateS + 300f, out var pf); park += Vector3.Cross(Vector3.up, pf).normalized * 40f;
            respawn.PlaceAt(RaceRoute.Ground(park.x, park.z) + Vector3.up * 0.5f, 0f); car.Body.isKinematic = true; race.NotifyTeleported();
            Time.timeScale = 3f; Time.maximumDeltaTime = 1f;
            var ais = race.Entrants.Where(e => !e.IsPlayer).ToList();
            var decisions = new List<string>(); var lastDec = ais.ToDictionary(e => e, e => 0);
            var inPool = ais.ToDictionary(e => e, e => false); var poolFalls = ais.ToDictionary(e => e, e => 0);
            float time = 0f, limit = file.Contains("noonday") ? 720f : 480f;
            while (time < limit && !ais.All(e => e.Finished))
            {
                yield return new WaitForFixedUpdate(); time += Time.fixedDeltaTime;
                foreach (var e in ais)
                {
                    var d = e.Driver;
                    if (d.Shortcuts.Decisions != lastDec[e]) { lastDec[e] = d.Shortcuts.Decisions; decisions.Add($"{e.Name}: {d.Shortcuts.Diagnostic}"); }
                    bool p = InR1Pool(e.Car.Body.position) && e.Car.Body.position.y < PoolRim() + 0.3f;
                    if (p && !inPool[e]) poolFalls[e]++;
                    inPool[e] = p;
                }
            }
            Time.timeScale = 1f; Time.maximumDeltaTime = 0.3333333f;
            report.AppendLine($"  {race.Route.Name}, {dir}, {DiffName[diff]}: {race.Route.Laps} laps, {(ais.All(e => e.Finished) ? "all finished" : "NOT ALL FINISHED")} ({time:0} s of race time)");
            if (!ais.All(e => e.Finished)) unfinished();
            foreach (var e in race.Ordered().Where(x => !x.IsPlayer))
            {
                var d = e.Driver; var c = d.Variation.Counts;
                for (int i = 0; i < c.Length; i++) total[i] += c[i];
                report.AppendLine($"    {e.Name,-5} {e.VehicleName,-16} {(e.Finished ? RaceController.Fmt(e.FinalTotal) : "-")}{(e.Estimated ? " (estimated)" : "")} penalties +{e.PenaltyTotal:0} s, recoveries {d.RecoveryCount}, shortcuts: {(e.ShortcutsTaken.Count > 0 ? string.Join(", ", e.ShortcutsTaken) : "none")}" +
                                  $"{(d.SlipsTaken > 0 ? $", sloppy tries {d.SlipsTaken}" : "")}{(poolFalls[e] > 0 ? $", in R1's pool {poolFalls[e]}x" : "")}; mistakes: {string.Join(", ", DriverVariation.Kinds.Select((k, i) => c[i] > 0 ? $"{k} {c[i]}" : null).Where(x => x != null).DefaultIfEmpty("none"))}");
            }
            foreach (var line in decisions) report.AppendLine("      " + line);
            car.Body.isKinematic = false; race.EndRace(); yield return null;
        }

        float PoolRim()
        {
            foreach (var h in FindObjectsByType<PoolHold>(FindObjectsSortMode.None)) return h.rimY;
            return 1e9f;
        }
    }
}
