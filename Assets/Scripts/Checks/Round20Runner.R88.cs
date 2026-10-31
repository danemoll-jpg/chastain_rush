using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ChastainRush
{
    // Round 88 in the exe (-exeRound88 <dir> [-r88Only p88climb,p88start] -r20Minutes 60 -logFile <dir>\Player.log), from the repo root, on the check's own save.
    // The crossings (C) use Round 83's check: -exeRound83 <dir> -r83Only lines -r82Lines local/round83/r83_lines_a.json | r83_lines_b.json.
    //   p88climb   A: the Kennesaw climb at full throttle (the Drifter Twin, the Street Classic, the Skyfin), normal and HIGH, from 300 m below the crest to 150 m
    //              above: air, how far off the line, and how far below the line's ground (off the road's level ground: more than 1 m below it)
    //   p88start   B: the Kennesaw race's start line (Dan's bump at x -832 z -5188): from the grid to the first bend at full throttle, and flying through the line
    //              at the end of a lap (from 300 m before it), the same three vehicles, normal and HIGH: no air over 0.3 s within 60 m of the line
    public sealed partial class Round20Runner
    {
        IEnumerator R88Climb()
        {
            report.AppendLine(); report.AppendLine("=== A. The Kennesaw climb at x -398 z -5520 (the run-off now from 80 m before the crest), full throttle by the human-like driver, from 300 m below to 150 m above");
            var spot = new Vector3(-398.6f, 0f, -5524.2f); int bad = 0;
            foreach (bool high in new[] { false, true })
                foreach (var veh in new[] { "drifter", "original", "skyfin" })
                {
                    yield return Vehicle(veh); R84High(high); yield return StartRoute("kennesaw_mountain.json", "forward", 0, 3);
                    var r = race.Route; float s0 = r.LapStationOf(spot); var line = new List<Vector3>();
                    for (float s = s0 - 300f; s <= s0 + 150f; s += 3f) line.Add(r.PointAt(r.GateS + s, out _));
                    race.Player.ClockStarted = true;
                    Place(RaceRoute.Surface(line[0].x, line[0].z), line[1] - line[0], 20f); yield return new WaitForSecondsRealtime(.3f);
                    float air = 0f, maxAir = 0f, worstLat = 0f, worstSigned = 0f, worstAt = 0f, drop = 0f, dropAt = 0f; Vector3 worstP = Vector3.zero; bool pic = false; var cum = Cum(line); float sNow = 0f;
                    var run = new HumanRun();
                    yield return HumanDrive(line, 60f, 60f, run, () =>
                    {
                        var p = car.Body.position; sNow = ProjectNear(line, cum, p, sNow, out float dev);
                        if (Mathf.Abs(sNow - 290f) < 100f)
                        {
                            air = car.GroundedWheels == 0 ? air + Time.fixedDeltaTime : 0f; maxAir = Mathf.Max(maxAir, air);
                            if (Mathf.Abs(dev) > worstLat) { var la = At(line, cum, sNow); var lb = At(line, cum, sNow + 1f); worstLat = Mathf.Abs(dev); worstSigned = (lb.x - la.x) * (p.z - la.z) - (lb.z - la.z) * (p.x - la.x) > 0f ? dev : -dev; worstAt = sNow - 300f; worstP = p; }
                            // below the line's ground: the line's own surface there less where the car is (wheels down only)
                            if (car.GroundedWheels > 0) { var lp = At(line, cum, sNow); float d = RaceRoute.Surface(lp.x, lp.z).y - (p.y - .6f); if (d > drop) { drop = d; dropAt = sNow - 300f; } }
                        }
                        if (!pic && veh == "original" && !high && sNow > 245f) { pic = true; StartCoroutine(Capture("88_climb_runoff")); }
                        return false;
                    }, null, null, null, false);
                    bool ok = maxAir <= .5f && drop < 1f && run.resets == 0; if (!ok) bad++;
                    report.AppendLine($"  {(high ? "HIGH" : "normal")} {Nm(veh)}: top {run.topSpeed:0} m/s, longest air from 100 m before to 90 m past the crest {maxAir:0.00} s, farthest off the line {worstLat:0.0} m ({(worstSigned > 0f ? "left, the outside" : "right")}, {worstAt:+0;-0} m from the crest, at ({worstP.x:0}, {worstP.z:0})), most below the line's ground {drop:0.0} m ({dropAt:+0;-0} m), resets {run.resets}{(ok ? "" : "  <- WRONG")}");
                    race.EndRace(); R84High(false); Flush();
                }
            Check("A: the climb: no air over 0.5 s, never off the road's level ground (3 vehicles, normal and HIGH)", bad == 0, $"{bad} of 6 runs wrong");
        }

        IEnumerator R88Start()
        {
            report.AppendLine(); report.AppendLine("=== B. The Kennesaw race's start line (Dan's bump at x -832 z -5188, lap distance 0): from the grid, and flying through the line at the end of a lap");
            int bad = 0;
            foreach (bool high in new[] { false, true })
                foreach (var veh in new[] { "drifter", "original", "skyfin" })
                    foreach (bool flying in new[] { false, true })
                    {
                        yield return Vehicle(veh); R84High(high); yield return StartRoute("kennesaw_mountain.json", "forward", 0, 3);
                        var r = race.Route; var line = new List<Vector3>(); float from = flying ? -300f : 0f;
                        if (flying) { for (float s = from; s <= 150f; s += 3f) line.Add(r.PointAt(r.GateS + r.LapLength + s, out _)); }
                        else
                        {
                            // from where the player stands on the grid, then the lap to its first bend (150 m)
                            var me = car.Body.position; line.Add(me);
                            for (float s = 3f; s <= 150f; s += 3f) line.Add(r.PointAt(r.GateS + s, out _));
                        }
                        race.Player.ClockStarted = true;
                        if (flying) { Place(RaceRoute.Surface(line[0].x, line[0].z), line[1] - line[0], 20f); yield return new WaitForSecondsRealtime(.3f); }
                        float air = 0f, maxAir = 0f; Vector3 airAt = Vector3.zero; var cum = Cum(line); float sNow = 0f; float lineAt = flying ? 300f : 0f; bool pic = false;
                        var run = new HumanRun();
                        yield return HumanDrive(line, 60f, 60f, run, () =>
                        {
                            var p = car.Body.position; sNow = ProjectNear(line, cum, p, sNow, out _);
                            if (Mathf.Abs(sNow - lineAt) < 60f || (!flying && sNow < 60f)) { air = car.GroundedWheels == 0 ? air + Time.fixedDeltaTime : 0f; if (air > maxAir) { maxAir = air; airAt = p; } }
                            if (!pic && flying && veh == "original" && !high && sNow > lineAt - 25f) { pic = true; StartCoroutine(Capture("88_start_line")); }
                            return false;
                        }, null, null, null, false);
                        bool ok = maxAir <= .3f && run.resets == 0; if (!ok) bad++;
                        report.AppendLine($"  {(high ? "HIGH" : "normal")} {Nm(veh)} {(flying ? "flying through the line" : "from the grid")}: top {run.topSpeed:0} m/s, longest air within 60 m of the line {maxAir:0.00} s{(maxAir > 0.05f ? $" (at ({airAt.x:0.0}, {airAt.z:0.0}))" : "")}, resets {run.resets}{(ok ? "" : "  <- WRONG")}");
                        race.EndRace(); R84High(false); Flush();
                    }
            Check("B: the start line: no air over 0.3 s (3 vehicles, from the grid and flying, normal and HIGH)", bad == 0, $"{bad} of 12 runs wrong");
        }
    }
}
