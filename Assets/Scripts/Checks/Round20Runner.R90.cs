using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Racer;
using UnityEngine;

namespace ChastainRush
{
    // Round 90 in the exe (-exeRound90 <dir> [-r90Only q90pics,q90short,q90race] [-r90Vehicles moto,original,skyfin] [-r90Laps 3] [-r90Modes normal,high]
    // -r20Minutes 150 -logFile <dir>\Player.log), from the repo root, on the check's own save. CHECK IT AS DAN PLAYS IT: the human-like driver on the
    // game's input (reaction lag, a wandering line, lifts), the Viking Quarry race from its grid.
    //   q90pics   each jump from the driver's seat at the lip, and from its TV camera (Free Roam's: the rim beside the flight, the landing deck) mid-flight
    //   q90short  each jump with a deliberately slow run-up (held at 24 m/s): into the lake, the automatic reset (route lake_reset_s) back on the run-up
    //             before the lip, no penalty, the next checkpoint still the jump's landing
    //   q90race   the race, 3 laps against 3 AI (the bike, a car, the heaviest car), the player in each of them, normal and HIGH MODE: every jump's lip speed,
    //             air time and landing (on the deck, past it, short), resets and where, stuck spots, missed checkpoints; every AI finishes
    public sealed partial class Round20Runner
    {
        const string R90File = "viking_quarry.json";
        static string R90Nm(string id) => VehicleProfile.Find(id)?.Name ?? id;

        // the jumps of the race in its order from the line, with their lap stations
        List<(QuarryJumps.Jump j, float lipS)> R90Jumps()
        {
            var q = QuarryJumps.Instance; var r = race.Route; var list = new List<(QuarryJumps.Jump, float)>();
            if (!q || r == null) return list;
            foreach (var j in q.Jumps) list.Add((j, r.LapStationOf(QuarryJumps.At(j, j.lip_s))));
            return list.OrderBy(x => x.Item2).ToList();
        }
        // where along a jump's own line a point is, and how far off it
        static float R90Along(QuarryJumps.Jump j, Vector3 p, out float lat)
        {
            var o = new Vector3(j.origin[0], 0f, j.origin[1]); var d = QuarryJumps.Forward(j); var n = new Vector3(-j.dir[1], 0f, j.dir[0]);
            lat = Vector3.Dot(Flat(p - o), n); return Vector3.Dot(Flat(p - o), d);
        }
        // the human-like driver's held speed by eye on a run-up: its speed window's middle (a little over)
        float R90Hold(Vector3 p, float extra = .8f)
        {
            var q = QuarryJumps.Instance; if (!q) return 99f;
            foreach (var j in q.Jumps) { float al = R90Along(j, p, out float lat); if (Mathf.Abs(lat) < 9f && al > 20f && al < j.lip_s - 4f) return (j.window.lo + j.window.hi) / 2f + extra; }
            return 99f;
        }
        string R90Arg(string name, string def) { var a = Environment.GetCommandLineArgs(); int i = Array.IndexOf(a, name); return i >= 0 && i + 1 < a.Length ? a[i + 1] : def; }

        // ---------------------------------------------------------------- pictures
        IEnumerator R90Pics()
        {
            report.AppendLine(); report.AppendLine("=== Pictures: each jump from the driver's seat at the lip, then from its TV camera mid-flight (the Drifter Twin, the race's run-up, its window's middle)");
            yield return Clock(13f); R84High(false); yield return Vehicle("drifter");
            yield return StartRoute(R90File, "forward", 0, 3);
            if (race.Route == null) { Check("pics: the Viking Quarry race loads", false); yield break; }
            var views = FindAnyObjectByType<CameraViews>();
            foreach (var (j, lipS) in R90Jumps())
            {
                var line = new List<Vector3>(); for (float s = 30f; s <= j.land_s[j.land_s.Length - 1] + 60f; s += 3f) { var a = QuarryJumps.At(j, s); line.Add(RaceRoute.Ground(a.x, a.z)); }
                Place(RaceRoute.Surface(line[0].x, line[0].z), line[1] - line[0], 20f); yield return new WaitForSecondsRealtime(.4f);
                var run = new HumanRun(); bool seat = false, tv = false; float air = 0f; float lipV = 0f;
                yield return HumanDrive(line, 60f, 40f, run, () =>
                {
                    var p = car.Body.position; float al = R90Along(j, p, out _);
                    if (!seat && al > j.lip_s - 6f) { seat = true; lipV = car.Body.linearVelocity.magnitude; StartCoroutine(R90Seat(views, $"90_{j.id}_driver_at_lip")); }
                    if (car.GroundedWheels == 0 && al > j.lip_s) air += Time.fixedDeltaTime; else if (air > 1f) return true;
                    if (!tv && air > 2.2f) { tv = true; StartCoroutine(R90Tv(j, $"90_{j.id}_tv_midflight")); }
                    return false;
                }, s => R90Hold(car.Body.position));
                yield return new WaitForSecondsRealtime(1.5f);
                report.AppendLine($"  {j.id} {j.name}: lip {lipV:0.0} m/s (window {j.window.lo:0.0} to {j.window.hi:0.0}), {air:0.0} s in the air; pictures 90_{j.id}_driver_at_lip, 90_{j.id}_tv_midflight");
                yield return Capture($"90_{j.id}_landed");
            }
            race.EndRace();
        }
        IEnumerator R90Seat(CameraViews views, string name)
        {
            if (views) views.SetView(CameraViews.View.FirstPerson);
            float ts = Time.timeScale; Time.timeScale = .05f; yield return new WaitForSecondsRealtime(.6f); yield return Capture(name); Time.timeScale = ts;
            if (views) views.SetView(CameraViews.View.Chase);
        }
        IEnumerator R90Tv(QuarryJumps.Jump j, string name)
        {
            // Free Roam's TV camera on the rim beside the flight (its event's replay camera), looking at the car
            var at = QuarryJumps.At(j, j.lip_s + 10f, -30f); float gy = GroundQuery.Height(at.x, at.z); at.y = Mathf.Max(float.IsNaN(gy) ? j.lip_y : gy + 8f, j.water_y + 8f);
            float ts = Time.timeScale; Time.timeScale = .03f;
            yield return Still(name, at, car.Body.position + Vector3.up * .8f, 40f);
            Time.timeScale = ts;
        }

        // ---------------------------------------------------------------- short: a slow run-up into the lake, and the reset
        IEnumerator R90Short()
        {
            report.AppendLine(); report.AppendLine("=== A slow run-up at each jump (held at 24 m/s, the Street Classic): short into the lake, the automatic reset back on the run-up, no penalty");
            R84High(false); yield return Vehicle("original"); R73Pad();
            yield return StartRoute(R90File, "forward", 0, 3);
            var r = race.Route; if (r == null) { Check("short: the Viking Quarry race loads", false); yield break; }
            var e = race.Player; e.ClockStarted = true; e.StartAt = e.LapStart = race.RaceClock; float L = r.LapLength;
            int ok = 0, n = 0;
            foreach (var (j, lipS) in R90Jumps())
            {
                n++;
                // the next checkpoint: the first one past the lip (the landing's)
                int next = Enumerable.Range(0, r.Checkpoints.Count).FirstOrDefault(i => Mathf.Repeat(r.Checkpoints[i].S - lipS, L) < L * .5f);
                next = Enumerable.Range(0, r.Checkpoints.Count).OrderBy(i => Mathf.Repeat(r.Checkpoints[i].S - lipS, L)).First();
                e.NextCheckpoint = next; e.Forgiven.Clear(); float pen0 = e.PenaltyTotal; int miss0 = e.MissedTotal, lr0 = RaceController.LakeResets;
                var line = new List<Vector3>(); for (float s = 60f; s <= j.land_s[j.land_s.Length - 1] + 40f; s += 3f) { var a = QuarryJumps.At(j, s); line.Add(RaceRoute.Ground(a.x, a.z)); }
                Place(RaceRoute.Surface(line[0].x, line[0].z), line[1] - line[0], 18f); race.NotifyTeleported(); yield return new WaitForSecondsRealtime(.3f);
                var run = new HumanRun(); bool wet = false; float lipV = 0f, wetAt = -1f; Vector3 splash = default;
                yield return HumanDrive(line, 24f, 30f, run, () =>
                {
                    var p = car.Body.position; float al = R90Along(j, p, out _);
                    if (lipV == 0f && al > j.lip_s - 2f) lipV = car.Body.linearVelocity.magnitude;
                    if (!wet && (car.WaterImmersion > .05f || ShallowWater.InWater(p))) { wet = true; wetAt = Time.time; splash = p; StartCoroutine(Capture($"90_{j.id}_short_splash")); }
                    return RaceController.LakeResets > lr0 || (wet && Time.time - wetAt > 6f);
                }, s => 24f, pressYWhenStuck: false);
                yield return new WaitForSecondsRealtime(1.5f);
                var q = car.Body.position; float at = R90Along(j, q, out float lat); float st = r.LapStationOf(q);
                bool reset = RaceController.LakeResets > lr0;
                bool before = at > 0f && at < j.lip_s - 10f && Mathf.Abs(lat) < 12f && Vector3.Angle(Flat(car.transform.forward), QuarryJumps.Forward(j)) < 30f;
                bool clean = e.PenaltyTotal == pen0 && e.MissedTotal == miss0 && e.NextCheckpoint == next;
                if (wet && reset && before && clean) ok++;
                report.AppendLine($"  {j.id} {j.name}: lip {lipV:0.0} m/s, {(wet ? $"into the lake at ({splash.x:0}, {splash.z:0})" : "NOT in the lake")}, {(reset ? $"reset {(Time.time - wetAt):0.0} s after" : "NO automatic reset")}: put {at:0} m along the run-up (the lip at {j.lip_s:0}), {lat:0.0} m off its middle, lap {st:0} m; penalty {e.PenaltyTotal - pen0:0} s, next CP{e.NextCheckpoint + 1} (was CP{next + 1})");
                yield return Capture($"90_{j.id}_short_after_reset");
            }
            Check("short: every short jump ends in the lake and is put back on its run-up before the lip, facing it, with no penalty", ok == n && n == 3, $"{ok} of {n}");
            race.EndRace();
        }

        // ---------------------------------------------------------------- the race
        IEnumerator R90Races()
        {
            var vehicles = R90Arg("-r90Vehicles", "moto,original,skyfin").Split(','); int laps = int.Parse(R90Arg("-r90Laps", "3"));
            var modes = R90Arg("-r90Modes", "normal,high").Split(',');
            report.AppendLine(); report.AppendLine($"=== The race: {laps} laps against 3 AI ({string.Join(", ", vehicles.Select(R90Nm))}), Normal difficulty, the player driven by the human-like driver in each, {string.Join(" and ", modes)}");
            yield return Clock(13f);
            RaceController.AiVehiclesOverride = vehicles;
            foreach (var mode in modes)
                foreach (var vid in vehicles)
                {
                    bool high = mode == "high"; R84High(high); yield return Vehicle(vid);
                    RoadDriver.Difficulty = 1; yield return StartRoute(R90File, "forward", 3, laps);
                    var r = race.Route; if (r == null) { Check("race: the Viking Quarry race loads", false); yield break; }
                    var jumps = R90Jumps();
                    var line = new List<Vector3>(r.LeadIn ?? new Vector3[0]);
                    for (int k = 0; k < laps; k++) for (float s = 0f; s < r.LapLength; s += 3f) line.Add(r.PointAt(r.GateS + s, out _));
                    line.Add(r.PointAt(r.GateS, out _)); line.Add(r.PointAt(r.GateS + 30f, out _));
                    var run = new HumanRun(); var log = new List<string>(); int cleared = 0, flights = 0, shortN = 0, lr0 = RaceController.LakeResets;
                    QuarryJumps.Jump cur = null; float airT = 0f, lipV = 0f; bool flying = false;
                    Time.timeScale = 2f; Time.maximumDeltaTime = .5f;
                    float t0 = Time.time;
                    yield return HumanDrive(line, 60f, 2400f, run, () =>
                    {
                        var p = car.Body.position;
                        if (cur == null)
                            foreach (var (j, _) in jumps) { float al = R90Along(j, p, out float lat); if (Mathf.Abs(lat) < 12f && al > j.lip_s - 1.5f && al < j.lip_s + 4f && car.Body.linearVelocity.magnitude > 8f) { cur = j; lipV = car.Body.linearVelocity.magnitude; airT = 0f; flying = false; break; } }
                        if (cur != null)
                        {
                            if (car.GroundedWheels == 0) { airT += Time.fixedDeltaTime; if (airT > .5f) flying = true; }
                            bool wet = car.WaterImmersion > .05f || ShallowWater.InWater(p);
                            if (wet || (flying && car.GroundedWheels >= 2) || Time.time - t0 > 2400f)
                            {
                                float al = R90Along(cur, p, out float lat); flights++;
                                string res = wet ? "SHORT (the lake)" : al < cur.land_s[0] - 1f ? "SHORT (the bank)" : al <= cur.land_s[cur.land_s.Length - 1] ? "on the deck" : "past the deck (the catch)";
                                if (!wet && al >= cur.land_s[0] - 1f) cleared++; else shortN++;
                                log.Add($"lap {race.Player.CompletedLaps + 1} {cur.id}: lip {lipV:0.0} m/s (window {cur.window.lo:0.0} to {cur.window.hi:0.0}), {airT:0.0} s, down {al - cur.lip_s:0} m out, {lat:0.0} m off its middle: {res}");
                                cur = null;
                            }
                        }
                        return race.Player.Finished;
                    }, s => R90Hold(car.Body.position));
                    float wait = 0f; while (race.Entrants.Any(x => !x.Finished) && wait < 400f) { yield return new WaitForSecondsRealtime(.5f); wait += 1f; }
                    Time.timeScale = 1f;
                    string tag = $"{(high ? "HIGH" : "normal")} {R90Nm(vid)}";
                    report.AppendLine($"  {tag}: " + string.Join("; ", race.Entrants.Select(x => $"{x.Name} ({x.VehicleName}) {(x.Finished ? RaceController.Fmt(x.FinalTotal) + (x.Estimated ? " est." : "") : "NOT finished")}, laps {string.Join(" / ", x.LapTimes.Select(v => RaceController.Fmt(v)))}, missed {x.MissedTotal}, resets {(x.Driver != null ? x.Driver.RecoveryCount : 0)}")));
                    report.AppendLine($"    the player: {run.resets} resets by Y, {RaceController.LakeResets - lr0} lake resets, stuck {run.stuck}{run.note}; top {run.topSpeed:0} m/s");
                    foreach (var l in log) report.AppendLine("    " + l);
                    report.AppendLine($"    trace:{run.trace}");
                    Check($"race {tag}: the player finishes {laps} laps", race.Player.Finished, $"laps {race.Player.CompletedLaps}");
                    Check($"race {tag}: every jump cleared at race speed (none short)", shortN == 0 && flights >= 3 * laps, $"{cleared} cleared, {shortN} short, {flights} flights of {3 * laps}");
                    Check($"race {tag}: no stuck spot, no reset", run.stuck == 0 && run.resets == 0 && RaceController.LakeResets == lr0, $"stuck {run.stuck}, resets {run.resets}, lake {RaceController.LakeResets - lr0}");
                    Check($"race {tag}: every AI finishes", race.Entrants.Where(x => !x.IsPlayer).All(x => x.Finished && !x.Estimated), string.Join(", ", race.Entrants.Where(x => !x.IsPlayer && (!x.Finished || x.Estimated)).Select(x => x.Name)));
                    race.EndRace(); R84High(false); Flush(); yield return new WaitForSecondsRealtime(.5f);
                }
            RaceController.AiVehiclesOverride = null;
        }
    }
}
