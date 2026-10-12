using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Racer;
using UnityEngine;

namespace ChastainRush
{
    // Round 21, Part B, checked in the exe (-exeRound21 <dir> -r21Only revlap,fwdlap,revslow,revrace):
    //   revlap   the reverse Noonday Creek lap flat out on each trail vehicle: every launch (the reverse take-offs BJ1R to BJ4R, RJ4 over
    //            New Chastain Road), its air, where it comes down, upright; every dead stop on the ground
    //   fwdlap   the forward lap the same way (the new backs must change nothing forward: launches, air, no stops)
    //   revslow  short of the speed in reverse: the Trail Four slow off RJ4 (onto the road, across, over J4's back, down: no wall) and slow
    //            into BJ1's back (into the gully or onto the deck, and on)
    //   revrace  the reverse race, three AI drivers (trail vehicles), the player parked: all finish, recoveries, mistakes
    public sealed partial class Round20Runner
    {
        IEnumerator LapJumps(string direction, string[] ids)
        {
            report.AppendLine(); report.AppendLine($"=== Round 21: the Noonday Creek lap {direction}, flat out on each trail vehicle: every launch and every dead stop");
            UseRoute("noonday_creek.json", direction); HookJumps();
            float L = race.Route.LapLength;
            var small = Small.Where(ids.Contains).ToArray(); if (small.Length == 0) small = Small;
            int stops = 0, badLand = 0; var seenJumps = new HashSet<string>();
            var launches = new List<(string id, float v)>();
            Action<BigJump, ArcadeVehicle, float> tap = (b, c, v) => { if (c == car) launches.Add((b.id, v)); };
            BigJump.Launched += tap;
            foreach (var id in small)
            {
                yield return Vehicle(id);
                var line = LapSlice(race.Route.GateS, race.Route.GateS + L + 20f);
                Place(line[0], line[2] - line[0], 12f); yield return new WaitForFixedUpdate();
                var run = new Run(); var hist = new Queue<(float t, float v, Vector3 p)>(); var found = new List<string>(); float lastAt = -99f; float t = 0f;
                launches.Clear(); var flights = new List<string>();
                string cur = null; float air = 0f, top = 0f; Vector3 from = Vector3.zero; int done = 0;
                var drive = StartCoroutine(DriveLine(line, 99f, 260f, run, null, null, 4f, false));
                while (!run.reached && run.end == "" && run.time < 259.9f)
                {
                    yield return new WaitForFixedUpdate(); t += Time.fixedDeltaTime;
                    float v = Flat(car.Body.linearVelocity).magnitude; var p = car.Body.position;
                    if (launches.Count > done) { done = launches.Count; cur = launches[done - 1].id; air = 0f; top = p.y; from = p; }
                    if (cur != null)
                    {
                        air += Time.fixedDeltaTime; top = Mathf.Max(top, p.y);
                        if (car.GroundedWheels >= 2 && air > 0.3f)
                        {
                            float up = car.transform.up.y; bool ok = up > 0.7f; if (!ok) badLand++;
                            seenJumps.Add(cur);
                            flights.Add($"{cur} {launches[done - 1].v:0.0} m/s: {air:0.00} s, {Flat(p - from).magnitude:0} m, {top - from.y:0.0} m up{(ok ? "" : $", LANDED BADLY (up {up:0.00})")}");
                            cur = null;
                        }
                    }
                    hist.Enqueue((t, v, p)); while (hist.Count > 0 && hist.Peek().t < t - 0.5f) hist.Dequeue();
                    float vMax = hist.Max(h => h.v);
                    bool inPipe = DrainPipe.Inside(p, out _) != null;
                    if (car.GroundedWheels > 0 && vMax > 10f && v < vMax * 0.45f && t - lastAt > 3f && !inPipe)
                    { lastAt = t; found.Add($"{vMax:0.0} to {v:0.0} m/s in 0.5 s at ({p.x:0.0}, {p.z:0.0}), lap {race.Route.LapStationOf(p):0} m"); }
                }
                yield return drive;
                stops += found.Count + (run.reached ? 0 : 1);
                report.AppendLine($"  {Nm(id),-16} {(run.reached ? $"the lap in {run.time:0.0} s" : "DID NOT FINISH " + run.end + $" at ({car.Body.position.x:0.0}, {car.Body.position.z:0.0})")}, {found.Count} sudden stop(s)");
                report.AppendLine($"      flights: {(flights.Count > 0 ? string.Join("; ", flights) : "none")}");
                foreach (var f in found) report.AppendLine("      stop: " + f);
            }
            BigJump.Launched -= tap;
            var want = direction == "reverse" ? new[] { "BJ1R", "BJ2R", "BJ3R", "BJ4R", "RJ4" } : new[] { "BJ1", "BJ2", "BJ3", "BJ4" };
            var missing = want.Where(w => !seenJumps.Contains(w)).ToList();
            Check($"Noonday Creek {direction}: every trail vehicle round the lap flat out, no dead stop, every big jump launched and landed upright", stops == 0 && badLand == 0 && missing.Count == 0,
                  $"{stops} stop(s), {badLand} bad landing(s){(missing.Count > 0 ? ", never launched: " + string.Join(", ", missing) : "")}");
        }

        // short of the speed in reverse: no wall, no trap
        IEnumerator RevSlow()
        {
            report.AppendLine(); report.AppendLine("=== Round 21: short of the speed in reverse (the Trail Four held slow into a reverse take-off): it rolls off, comes down short, and drives on");
            UseRoute("noonday_creek.json", "reverse"); HookJumps();
            float L = race.Route.LapLength;
            var bad = new List<string>();
            foreach (var (jid, cap) in new[] { ("RJ4", 8f), ("RJ4", 18f), ("BJ1R", 10f), ("BJ2R", 10f), ("BJ3R", 10f), ("BJ4R", 10f) })
            {
                var j = BigJump.All.FirstOrDefault(b => b.id == jid);
                if (j == null) { bad.Add(jid + " missing"); continue; }
                yield return Vehicle("atv");
                float sj = race.Route.LapStationOf(j.lip);
                var line = LapSlice(race.Route.GateS + sj - 70f, race.Route.GateS + sj + 70f);
                Place(line[0], line[2] - line[0], cap); yield return new WaitForFixedUpdate();
                var run = new Run(); ClearHook();
                var drive = StartCoroutine(DriveLine(line, cap, 40f, run, null, null, 4f, false));
                while (!run.reached && run.end == "" && run.time < 39.9f) yield return null;
                yield return drive;
                bool ok = run.reached;
                if (!ok) bad.Add($"{jid} at {cap:0} m/s");
                report.AppendLine($"  {jid} held to {cap:0} m/s: {(hookAt == j ? $"launched at {hookLaunch:0.0}" : "no launch (rolled off)")}, {(ok ? $"on along the trail ({run.time:0.0} s for 140 m, slowest {run.minSpeed:0.0} m/s)" : "STOPPED " + run.end + $" at ({car.Body.position.x:0.0}, {car.Body.position.y:0.0}, {car.Body.position.z:0.0})")}");
            }
            Check("short in reverse: slow into every reverse take-off, the Trail Four always drives on (no wall, no trap)", bad.Count == 0, bad.Count > 0 ? "not: " + string.Join(", ", bad) : "");
            // pictures
            var r4 = BigJump.All.FirstOrDefault(b => b.id == "RJ4");
            if (r4 != null)
            {
                var f = r4.dir; car.Body.position = r4.lip + Vector3.Cross(Vector3.up, f) * 40f + Vector3.up * 5f; car.Body.linearVelocity = Vector3.zero;
                yield return Still("rj4_from_the_reverse_approach", r4.lip - f * 45f + Vector3.up * 2f, r4.lip + f * 20f, 60f);
                yield return Still("rj4_side_over_the_road", r4.lip + f * 14f + Vector3.Cross(Vector3.up, f) * 38f + Vector3.up * 8f, r4.lip + f * 14f, 60f);
            }
            foreach (var id in new[] { "BJ1R", "BJ3R" })
            {
                var j = BigJump.All.FirstOrDefault(b => b.id == id); if (j == null) continue;
                yield return Still(id.ToLower() + "_from_the_reverse_approach", j.lip - j.dir * 22f + Vector3.up * 2f, j.lip + j.dir * 10f, 60f);
            }
        }

        IEnumerator RevRace()
        {
            report.AppendLine(); report.AppendLine("=== Round 21: the reverse Noonday Creek race, three AI drivers (trail vehicles), the player parked");
            int prevDiff = RoadDriver.Difficulty; var total = new int[DriverVariation.Kinds.Length]; int unfinished = 0;
            UseRoute("noonday_creek.json", "reverse");
            report.AppendLine($"  reverse: {race.Route.Checkpoints.Count} checkpoints in travel order: {string.Join(", ", race.Route.Checkpoints.Select(g => $"CP{g.Id} at {g.S:0} m ({g.Label})"))}; the start / finish at ({race.Route.Gate.Position.x:0}, {race.Route.Gate.Position.z:0}), the grid at ({race.Route.GridPosition.x:0}, {race.Route.GridPosition.z:0}) facing {race.Route.GridYaw:0} deg");
            yield return OneRace("noonday_creek.json", "reverse", 1, total, () => unfinished++);
            RoadDriver.Difficulty = prevDiff;
            Check("the reverse Noonday Creek race: three AI drivers in trail vehicles, all finish", unfinished == 0, $"mistakes: {string.Join(", ", DriverVariation.Kinds.Select((k, i) => $"{k} {total[i]}"))}");
        }
        // what a trail vehicle touches round BJ1's lip, both ways (a diagnosis)
        IEnumerator Bj1Diag()
        {
            report.AppendLine(); report.AppendLine("=== Round 21: BJ1 contacts round the lip, forward and reverse");
            var j = BigJump.All.First(b => b.id == "BJ1");
            var tap = car.gameObject.GetComponent<Touches>() ?? car.gameObject.AddComponent<Touches>();
            foreach (var dir in new[] { "forward", "reverse" })
            {
                UseRoute("noonday_creek.json", dir); HookJumps();
                float sj = race.Route.LapStationOf(j.lip) + race.Route.GateS;
                foreach (var id in new[] { "drifter", "atv" })
                {
                    yield return Vehicle(id);
                    var line = LapSlice(sj - 120f, sj + 40f);
                    Place(line[0], line[2] - line[0], 20f); yield return new WaitForFixedUpdate();
                    var run = new Run(); tap.seen.Clear(); var log = new List<string>();
                    var drive = StartCoroutine(DriveLine(line, 99f, 30f, run, null, null, 4f));
                    while (!run.reached && run.end == "" && run.time < 29.9f)
                    {
                        float a = Vector3.Dot(Flat(car.Body.position - j.lip), j.dir) * (dir == "forward" ? 1f : -1f);
                        tap.on = a > -14f && a < 6f;
                        if (tap.on) log.Add($"{a:0.0}:{Flat(car.Body.linearVelocity).magnitude:0.0}/{car.GroundedWheels}/{car.Body.position.y - j.lip.y:0.00}");
                        yield return new WaitForFixedUpdate();
                    }
                    yield return drive; tap.on = false;
                    report.AppendLine($"  {dir} {Nm(id)}: {(run.reached ? "on" : "STOPPED")}; trace (m from lip: speed/wheels/height over lip): {string.Join(" ", log.Where((x, i) => i % 3 == 0))}");
                    report.AppendLine("    touched: " + string.Join("; ", tap.seen.Distinct().Take(10)));
                }
            }
        }
    }
}
