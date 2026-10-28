using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Racer;
using UnityEngine;

namespace ChastainRush
{
    // Round 78 in the exe (-exeRound78 <dir> [-r78Only dan,rail,train,square,house,open,msq,air] [-r78Lines <file>] [-traffic Normal] -logFile <dir>\Player.log):
    // CHECK IT AS DAN PLAYS IT (TODO.md item 0). Viewpoints: local/round78/r78_lines.json (python Tools/mapgen/r78_lines.py).
    //   dan     Dan's reports (session 2026-10-28_04-03-25-890_8dd649) from his exact position and heading; BUG-001's underpass and BUG-002's overpass driven
    //   rail    every road under a rail bridge and over the railroad, from its approach (the chase camera), each driven through at speed
    //   train   the running train from the Mill Street crossing: the lamps, the gates, the train passing, the gates up; the horn; traffic held; a car on the
    //           tracks hit (a crash and a reset); the minimap
    //   square  Marietta Square's streets (BUG-003): the parked cars off the travel lanes (real models), the lights with housings, lit by phase
    //   house   BUG-005's house and its neighbors
    //   open    Round 77's open items: the stray block gone (its BUG-003), the Green deck's end clear of the physicians' building
    //   msq     the Marietta Square race, three times (1 lap, 3 AI, Normal): the human-like driver beats the train, waits for it, jumps over it
    //   air     the Air Tour flown by the human-like pilot by the arrow; a cockpit picture under the hospital's skybridge
    public sealed partial class Round20Runner
    {
        [Serializable] class R78Poi { public string name, kind; public float x, z, heading, len; }
        [Serializable] class R78Lines { public R78Poi[] rail; }
        R78Lines r78;
        R78Lines R78L() { if (r78 == null) { var p = Path.Combine(Environment.CurrentDirectory, R70Arg("-r78Lines", "local/round78/r78_lines.json")); if (File.Exists(p)) r78 = JsonUtility.FromJson<R78Lines>(File.ReadAllText(p)); } return r78; }
        static Vector3 R78Fwd(float yaw) => new Vector3(Mathf.Sin(yaw * Mathf.Deg2Rad), 0f, Mathf.Cos(yaw * Mathf.Deg2Rad));
        static readonly Vector3 R78Mill = new Vector3(1622.3f, 0f, -8410.2f);

        // drive straight on along a heading for 'secs' with the pad (a person holding the line), measuring the longest spell in the air, the worst jolt
        // (the vertical speed's biggest change in a step) and whether it stopped
        IEnumerator R78DriveThrough(Vector3 from, float yaw, float secs, float throttle, Action<string> say)
        {
            yield return R74Place(from, yaw); R73Pad();
            float t = 0f, air = 0f, airMax = 0f, jolt = 0f, lastVy = 0f, top = 0f; var start = car.Body.position; bool stopped = false; var fwd = R78Fwd(yaw);
            while (t < secs)
            {
                float ang = Vector3.SignedAngle(Flat(car.transform.forward), fwd, Vector3.up);
                R73Set(Mathf.Clamp(ang / 20f, -1f, 1f), throttle, 0f); yield return new WaitForFixedUpdate(); t += Time.fixedDeltaTime;
                var v = car.Body.linearVelocity; top = Mathf.Max(top, v.magnitude);
                if (t > .5f) jolt = Mathf.Max(jolt, Mathf.Abs(v.y - lastVy)); lastVy = v.y;
                if (car.GroundedWheels == 0) { air += Time.fixedDeltaTime; airMax = Mathf.Max(airMax, air); } else air = 0f;
                if (t > 4f && v.magnitude < 1f) stopped = true;
            }
            R73Set(0, 0, 0);
            say($"{(car.Body.position - start).magnitude:0} m in {secs:0} s, top {top:0} m/s, longest in the air {airMax:0.00} s, biggest jolt {jolt:0.0} m/s in a step{(stopped ? ", STOPPED" : "")}");
            lastDrive = (airMax, jolt, stopped, (car.Body.position - start).magnitude);
        }
        (float air, float jolt, bool stopped, float dist) lastDrive;

        // ---------------------------------------------------------------- dan
        IEnumerator R78Dan()
        {
            report.AppendLine(); report.AppendLine("=== Dan's reports, from his exact position and heading (the chase camera, as he saw it)");
            if (race.State != RaceController.Stage.Free) race.EndRace();
            yield return Vehicle("drifter"); yield return Clock(13f);
            var spots = new (string id, Vector3 at, float yaw, string what)[]
            {
                ("BUG-001", new Vector3(918.88f, 0f, -6512.37f), 189.86f, "Tower Road NW where it dips under the railroad: a narrow trench with sheer walls"),
                ("BUG-002", new Vector3(646.70f, 0f, -5541.05f), 73.73f, "Old 41 Highway NW over the railroad: the road waving up to a boxy hump"),
                ("BUG-003", new Vector3(1720.39f, 0f, -8428.27f), 71.03f, "West Park Square NW: blocky signals, parked cars in the travel lanes"),
                ("BUG-005", new Vector3(1850.19f, 0f, -8843.93f), 146.49f, "a house south-east of the square sunk to its eaves"),
            };
            foreach (var s in spots)
            {
                yield return R74Place(s.at, s.yaw); yield return Capture($"78_{s.id}_after");
                report.AppendLine($"  {s.id} ({s.what}): at ({car.Body.position.x:0.0}, {car.Body.position.y:0.0}, {car.Body.position.z:0.0}) heading {s.yaw:0}; ahead: {R74Ahead(car.Body.position, s.yaw)}; picture 78_{s.id}_after.png");
            }
            // BUG-001: Tower Road driven under the railroad, both ways; BUG-002: Old 41 driven over it, both ways
            foreach (var (nm, from, yaw) in new[] { ("Tower Road westbound", new Vector3(1010f, 0f, -6522f), 271f), ("Tower Road eastbound", new Vector3(835f, 0f, -6525f), 91f),
                                                    ("Old 41 eastbound", new Vector3(560f, 0f, -5536f), 87f), ("Old 41 westbound", new Vector3(760f, 0f, -5556f), 255f) })
            {
                string res = ""; yield return R78DriveThrough(from, yaw, 9f, .75f, x => res = x);
                report.AppendLine($"  {nm}: {res}");
                Check($"dan: {nm} drives through (no stop, no spell in the air over 0.4 s)", !lastDrive.stopped && lastDrive.air < .4f && lastDrive.dist > 80f, res);
            }
            yield return R74Place(new Vector3(960f, 0f, -6524f), 271f); yield return Capture("78_BUG-001_after_road");
            yield return R74Place(new Vector3(600f, 0f, -5538f), 80f); yield return Capture("78_BUG-002_after_road");
        }

        // ---------------------------------------------------------------- rail
        IEnumerator R78Rail()
        {
            report.AppendLine(); report.AppendLine("=== D. Every road under a rail bridge and over the railroad, from its approach (chase camera), then driven through");
            var L = R78L(); if (L?.rail == null) { Check("rail: the lines file", false); yield break; }
            if (race.State != RaceController.Stage.Free) race.EndRace();
            yield return Vehicle("original"); yield return Clock(13f);
            int bad = 0, n = 0;
            foreach (var p in L.rail)
            {
                var at = new Vector3(p.x, 0f, p.z);
                yield return R74Place(at, p.heading); yield return Capture("78_D_" + p.name);
                string res = ""; yield return R78DriveThrough(at, p.heading, Mathf.Clamp(p.len / 14f, 6f, 11f), .65f, x => res = x); n++;
                bool ok = !lastDrive.stopped && lastDrive.air < .4f; if (!ok) bad++;
                report.AppendLine($"  {p.name} ({p.kind}): from ({p.x:0}, {p.z:0}) heading {p.heading:0}: {res}{(ok ? "" : "  <- LOOK")}");
            }
            Check("rail: every under / overpass drives through (no stop, no air over 0.4 s)", bad == 0, $"{bad} of {n}");
        }

        // ---------------------------------------------------------------- train
        IEnumerator R78Train()
        {
            report.AppendLine(); report.AppendLine("=== A. The running train, from the Mill Street crossing by the depot");
            var rt = RailTrains.Instance; if (!rt) { Check("train: RailTrains", false); yield break; }
            if (race.State != RaceController.Stage.Free) race.EndRace();
            yield return Vehicle("original"); yield return Clock(13f);
            var cross = FindObjectsByType<RailCrossing>(FindObjectsSortMode.None).OrderBy(c => Flat(c.center - R78Mill).magnitude).First();
            report.AppendLine($"  {FindObjectsByType<RailCrossing>(FindObjectsSortMode.None).Length} level crossings; the nearest to Mill Street: {cross.name} at ({cross.center.x:0}, {cross.center.z:0}), {cross.arms.Length} arms, {cross.lamps.Length} lamps; routes {string.Join(", ", rt.routes.Select(r => $"{r.name} {r.Length / 1000f:0.0} km ({r.crossings.Count} crossings)"))}; headway {rt.H:0} s");
            // the car stopped 30 m east of the crossing, facing it (westbound); the train told to come in 34 s (a southbound one)
            yield return R74Place(new Vector3(1655f, 0f, -8408.5f), 268f, 1.5f);
            int horns0 = RailTrains.HornsBlown; rt.Align(cross.center, 34f, true);
            float t = 0f; bool lit = false, down = false, passing = false, up = false; int maxHeld = 0; float closedAt = -1f, downAt = -1f, trainAt = -1f, upAt = -1f; var held = new HashSet<TrafficDriver>();
            var side = cross.center + new Vector3(30f, 7f, -24f);
            while (t < 120f)
            {
                yield return null; t += Time.deltaTime;
                if (!lit && cross.Closed) { lit = true; closedAt = t; yield return Capture("78_A_train_lamps_on"); yield return Still("78_A_train_lamps_on_side", side, cross.center + Vector3.up * 2f, 50f); }
                if (!down && cross.ArmAngle < 3f) { down = true; downAt = t; yield return Capture("78_A_train_gates_down"); }
                bool occ = Physics.OverlapSphere(cross.center + Vector3.up * 2.5f, 3f).Any(c => c.GetComponent<TrainCar>());
                if (!passing && occ) { passing = true; trainAt = t; yield return Capture("78_A_train_passing"); yield return Still("78_A_train_passing_side", side, cross.center + Vector3.up * 2f, 55f); yield return Still("78_A_train_passing_high", cross.center + new Vector3(-60f, 25f, 40f), cross.center, 50f); }
                if (Traffic.Instance) foreach (var d in Traffic.Instance.Cars) if (d && d.State == "train") held.Add(d);
                maxHeld = Mathf.Max(maxHeld, held.Count);
                if (passing && !up && !cross.Closed && cross.ArmAngle > 88f) { up = true; upAt = t; yield return Capture("78_A_train_gates_up"); break; }
            }
            report.AppendLine($"  lamps on at {closedAt:0.0} s, gates down at {downAt:0.0} s, the train at the road at {trainAt:0.0} s, gates up at {upAt:0.0} s; horns blown {RailTrains.HornsBlown - horns0}; trains made near the camera {rt.LiveTrains}; traffic cars held at a crossing {held.Count}");
            Check("train: the lamps flash and the gates come down before the train, go up after it", lit && down && passing && up && downAt < trainAt && upAt > trainAt, $"{closedAt:0} / {downAt:0} / {trainAt:0} / {upAt:0}");
            Check("train: the horn is blown on the way to the crossing", RailTrains.HornsBlown > horns0, (RailTrains.HornsBlown - horns0).ToString());
            // the minimap with the train on it
            yield return R74Place(new Vector3(1660f, 0f, -8408.5f), 268f, 1f); rt.Align(cross.center, 6f, true); yield return new WaitForSecondsRealtime(7f); yield return Capture("78_A_train_minimap");
            // a car standing on the tracks when a train comes: a crash and a reset (the player is put back)
            yield return new WaitForSecondsRealtime(40f);
            yield return R74Place(new Vector3(cross.center.x, 0f, cross.center.z + 1.5f), 268f, 1f); int hits0 = TrainCar.Hits; var stood = car.Body.position;
            rt.Align(cross.center, 5f, true); t = 0f; bool moved = false;
            while (t < 14f) { yield return null; t += Time.deltaTime; if (TrainCar.Hits > hits0 && (car.Body.position - stood).magnitude > 6f && car.Body.linearVelocity.magnitude < 1f) { moved = true; break; } }
            yield return Capture("78_A_train_hit_reset");
            report.AppendLine($"  a car on the tracks: train hits {TrainCar.Hits - hits0}; put back {(moved ? "yes" : "NO")} at ({car.Body.position.x:0}, {car.Body.position.z:0})");
            Check("train: a car on the tracks is hit and put back", TrainCar.Hits > hits0 && moved, $"{TrainCar.Hits - hits0} hits");
        }

        // ---------------------------------------------------------------- square
        IEnumerator R78Square()
        {
            report.AppendLine(); report.AppendLine("=== E. Marietta Square's streets: the parked cars off the travel lanes, the lights");
            if (race.State != RaceController.Stage.Free) race.EndRace();
            yield return Vehicle("drifter"); yield return Clock(13f);
            yield return R74Place(new Vector3(1720.39f, 0f, -8428.27f), 71.03f); yield return new WaitForSecondsRealtime(1f);
            var pc = FindObjectsByType<ParkedCars>(FindObjectsSortMode.None).FirstOrDefault(p => p.name == "Marietta parked cars");
            int inLane = 0, n = 0; var rl = RoadLookup.Instance;
            if (pc) for (int i = 0; i < pc.at.Length; i++) { n++; if (rl && rl.Nearest(pc.at[i], out string nm, out float dist, out float hw) && dist < hw + .4f) inLane++; }
            int models = pc ? pc.GetComponentsInChildren<MeshRenderer>().Length : 0;
            report.AppendLine($"  parked cars round the square: {n} spots, {inLane} with their middle on a carriageway; {models} model pieces drawn near the camera");
            Check("square: no parked car stands in a travel lane", pc && n > 0 && inLane == 0, $"{inLane} of {n}");
            Check("square: the parked cars are the traffic models", models > n / 2, models.ToString());
            yield return Capture("78_E_BUG-003_spot");
            // a signal close up, and the two phases
            var ts = FindAnyObjectByType<TrafficSignals>();
            if (ts && ts.lamps.Length > 0)
            {
                var head = ts.lamps[0].transform.position; var fwd = ts.lamps[0].transform.forward;
                yield return Still("78_E_signal_close", head + Vector3.down * 1.5f - fwd * -6f + Vector3.Cross(Vector3.up, fwd) * 2f, head, 45f);
                yield return new WaitForSecondsRealtime(26f); yield return Still("78_E_signal_close_next_phase", head + Vector3.down * 1.5f - fwd * -6f + Vector3.Cross(Vector3.up, fwd) * 2f, head, 45f);
                report.AppendLine($"  signals: {ts.lamps.Length} lamps on {ts.lamps.Length / 3} heads, phased (two pictures 26 s apart)");
            }
            // round the square on the race's line (the travel lanes clear: the human-like driver, no reset)
            foreach (var (nm, x, z, yaw) in new[] { ("78_E_north_side", 1700f, -8412f, 90f), ("78_E_west_side", 1712f, -8400f, 180f), ("78_E_south_side", 1720f, -8514f, 90f), ("78_E_east_side", 1820f, -8520f, 0f) })
            { yield return R74Place(new Vector3(x, 0f, z), yaw); yield return Capture(nm); }
            UseRoute("marietta_square.json", "forward"); var r = race.Route; if (r == null) { Check("square: the race's route", false); yield break; }
            var line = new List<Vector3>(); for (float s = 0f; s <= r.LapLength; s += 3f) line.Add(r.PointAt(r.GateS + s, out _));
            var f0 = Flat(line[1] - line[0]).normalized; respawn.PlaceAt(line[0] + Vector3.up * .3f, Mathf.Atan2(f0.x, f0.z) * Mathf.Rad2Deg); race.NotifyTeleported(); yield return new WaitForSecondsRealtime(1f);
            var run = new HumanRun(); yield return HumanDrive(line, 22f, 200f, run, null, null, new List<(string, float)> { ("78_E_lap_waddell", 200f), ("78_E_lap_deck", 360f), ("78_E_lap_lawrence", 600f), ("78_E_lap_whitlock", 1180f) });
            report.AppendLine($"  a lap of the race's line in Free Roam: {(run.reachedEnd ? "round" : "NOT round")} in {run.time:0} s, resets {run.resets}{run.note}");
            Check("square: a lap of the race's line drives clear (no reset)", run.reachedEnd && run.resets == 0, $"resets {run.resets}");
        }

        // ---------------------------------------------------------------- house
        IEnumerator R78House()
        {
            report.AppendLine(); report.AppendLine("=== F. BUG-005's house and the houses round it");
            if (race.State != RaceController.Stage.Free) race.EndRace();
            yield return Vehicle("drifter"); yield return Clock(13f);
            yield return R74Place(new Vector3(1850.19f, 0f, -8843.93f), 146.49f); yield return Capture("78_F_BUG-005_after");
            yield return R74Place(new Vector3(1840f, 0f, -8830f), 160f); yield return Still("78_F_BUG-005_high", new Vector3(1830f, car.Body.position.y + 25f, -8815f), new Vector3(1858f, car.Body.position.y, -8855f), 55f);
            foreach (var (nm, x, z, yaw) in new[] { ("78_F_street_1", 1900f, -8800f, 200f), ("78_F_street_2", 2200f, -8700f, 90f), ("78_F_street_3", 2500f, -9100f, 270f) })
            { yield return R74Place(new Vector3(x, 0f, z), yaw); yield return Capture(nm); report.AppendLine($"  {nm}: ahead {R74Ahead(car.Body.position, yaw)}"); }
        }

        // ---------------------------------------------------------------- open
        IEnumerator R78Open()
        {
            report.AppendLine(); report.AppendLine("=== G. Round 77's open items");
            if (race.State != RaceController.Stage.Free) race.EndRace();
            yield return Vehicle("drifter"); yield return Clock(13f);
            yield return R74Place(new Vector3(1618.42f, 0f, -6110.74f), 29.02f); yield return Capture("78_G_r77_BUG-003_block_gone");
            int left = FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Count(m => m.name.StartsWith("BusinessBodies") && m.bounds.Contains(new Vector3(1590.5f, m.bounds.center.y, -6036.6f)) && m.bounds.size.x < 5f);
            report.AppendLine($"  Round 77's BUG-003 spot: picture 78_G_r77_BUG-003_block_gone.png (the 1.9 x 3.9 m hut 6854 left out)");
            yield return Still("78_G_green_deck_end", new Vector3(1560f, 420f, -6830f), new Vector3(1595f, 395f, -6866f), 55f);
            report.AppendLine("  the Green deck's west end and the physicians' building: picture 78_G_green_deck_end.png");
        }

        // ---------------------------------------------------------------- msq: the Marietta Square race, beat / wait / jump
        IEnumerator R78Msq()
        {
            report.AppendLine(); report.AppendLine("=== B. The Marietta Square race: 1 lap, 3 AI, Normal, the human-like driver (Street Classic); a train told to meet it at Mill Street");
            yield return Clock(13f);
            foreach (var mode in new[] { "beat", "wait", "jump" })
            {
                UseRoute("marietta_square.json", "forward"); var r = race.Route; if (r == null) { Check("msq: the route", false); yield break; }
                yield return Vehicle("original");
                RaceController.LapsOverride = 1; RaceController.AiCount = 3; RoadDriver.Difficulty = 1; race.StartRace();
                while (race.State == RaceController.Stage.Countdown) yield return null;
                var line = new List<Vector3>(r.LeadIn ?? new Vector3[0]); for (float s = 0f; s <= r.LapLength; s += 3f) line.Add(r.PointAt(r.GateS + s, out _));
                if (mode == "jump") line = R78JumpLine(line);
                var rt = RailTrains.Instance; var cross = FindObjectsByType<RailCrossing>(FindObjectsSortMode.None).OrderBy(c => Flat(c.center - R78Mill).magnitude).First();
                bool aligned = false, waited = false, crossedClosed = false; float waitedFor = 0f; int aiWaited = 0, aiBeat = 0; var aiSeen = new HashSet<RaceEntrant>();
                var run = new HumanRun();
                Func<float, float> speedAt = s =>
                {
                    var p = car.Body.position; float d = Flat(p - cross.center).magnitude;
                    if (!aligned && d < 260f && p.x > cross.center.x)
                    {
                        aligned = true; float eta = d / Mathf.Max(8f, car.Body.linearVelocity.magnitude);
                        rt.Align(cross.center, mode == "beat" ? eta + 12f : mode == "wait" ? eta - 1f : eta + 1.5f, true);
                    }
                    if (mode == "jump") { float lipX = 1637.5f; if (p.x > lipX && p.x < lipX + 110f && Mathf.Abs(p.z + 8406f) < 12f) return 26f; return 99f; }
                    if (mode == "wait" && RailTrains.ClosedAhead(p, car.transform.forward, 120f, out float stop, out float tin, out _))
                    { if (car.Body.linearVelocity.magnitude < .8f) { waited = true; waitedFor += Time.fixedDeltaTime; } return stop < 1f ? 0f : Mathf.Sqrt(2f * 5f * Mathf.Max(0f, stop - 1f)); }
                    if (cross.Closed && d < 6f) crossedClosed = true;
                    return 99f;
                };
                Time.timeScale = 1.5f; Time.maximumDeltaTime = .5f;
                var pics = new List<(string, float)> { ($"78_B_{mode}_deck", 300f + 60f), ($"78_B_{mode}_mill_approach", 700f + 60f), ($"78_B_{mode}_mill", 770f + 60f), ($"78_B_{mode}_whitlock", 1150f + 60f) };
                var watch = StartCoroutine(R73Watch(() =>
                {
                    foreach (var e in race.Entrants)
                    {
                        if (e == race.Player || !e.Car) continue; var rd = e.Car.GetComponent<RoadDriver>(); if (!rd) continue;
                        if (rd.Why == "train" && e.Car.Body.linearVelocity.magnitude < 1f && aiSeen.Add(e)) aiWaited++;
                        if (cross.Closed && Flat(e.Car.Body.position - cross.center).magnitude < 5f && !aiSeen.Contains(e)) { aiSeen.Add(e); aiBeat++; }
                    }
                }, () => true));
                yield return HumanDrive(line, 30f, 300f, run, () => race.Player.Finished, speedAt, pics, $"78_B_{mode}_in_the_air");
                StopCoroutine(watch);
                float w = 0f; while (race.Entrants.Any(e => !e.Finished) && w < 120f) { yield return new WaitForSecondsRealtime(.5f); w += .5f; }
                Time.timeScale = 1f;
                report.AppendLine($"  {mode}: {(race.Player.Finished ? "finished " + RaceController.Fmt(race.Player.FinalTotal) : "NOT finished")}; resets {run.resets}{run.note}; longest in the air {run.maxAir:0.00} s; " +
                                  $"waited at the gates {waitedFor:0.0} s; crossed while closed {crossedClosed}; train hits so far {TrainCar.Hits}; AI waited {aiWaited}, AI beat it {aiBeat}; " + string.Join("; ", race.Entrants.Select(e => $"{e.Name} {(e.Finished ? RaceController.Fmt(e.FinalTotal) + (e.Estimated ? " est." : "") : "NOT finished")}")));
                report.AppendLine($"    trace:{run.trace}");
                if (mode == "beat") Check("msq beat: across the closed crossing ahead of the train, finished, no reset", crossedClosed && race.Player.Finished && run.resets == 0, $"crossed closed {crossedClosed}, resets {run.resets}");
                if (mode == "wait") Check("msq wait: stopped at the gates for the train, then on, finished", waited && race.Player.Finished, $"waited {waitedFor:0.0} s");
                if (mode == "jump") Check("msq jump: over the tracks off the ballast pile (over 1.2 s in the air), finished, no reset", run.maxAir > 1.2f && race.Player.Finished && run.resets == 0, $"air {run.maxAir:0.00} s, resets {run.resets}");
                Check($"msq {mode}: every racer finishes", race.Entrants.All(e => e.Finished && !e.Estimated), string.Join(", ", race.Entrants.Where(e => !e.Finished || e.Estimated).Select(e => e.Name)));
                race.EndRace(); yield return new WaitForSecondsRealtime(1f);
            }
        }

        // the race's line with the jump: off Mill Street's lane onto the ballast pile's line, over the tracks, down the spoil pile, back onto Mill Street
        static List<Vector3> R78JumpLine(List<Vector3> line)
        {
            const float kz = -8403.6f, lipX = 1637.5f, footX = 1652.5f;
            int i0 = line.FindIndex(p => p.x < 1705f && p.x > 1660f && Mathf.Abs(p.z + 8409f) < 6f); int i1 = line.FindIndex(p => p.x < 1568f && Mathf.Abs(p.z + 8409f) < 8f);
            if (i0 < 0 || i1 < 0) return line;
            var mid = new List<Vector3>();
            for (float x = line[i0].x - 4f; x > footX + 6f; x -= 3f) { float u = Mathf.InverseLerp(line[i0].x, footX + 6f, x); mid.Add(RaceRoute.Ground(x, Mathf.Lerp(line[i0].z, kz, Mathf.SmoothStep(0f, 1f, u)))); }
            for (float x = footX + 6f; x > 1572f; x -= 3f) mid.Add(new Vector3(x, 0f, kz));
            for (float x = 1572f; x > line[i1].x + 2f; x -= 3f) { float u = Mathf.InverseLerp(1572f, line[i1].x, x); mid.Add(RaceRoute.Ground(x, Mathf.Lerp(kz, line[i1].z, Mathf.SmoothStep(0f, 1f, u)))); }
            var outL = line.Take(i0 + 1).ToList(); outL.AddRange(mid); outL.AddRange(line.Skip(i1)); return outL;
        }

        // ---------------------------------------------------------------- air
        IEnumerator R78Air()
        {
            report.AppendLine(); report.AppendLine("=== C. The Air Tour flown by the human-like pilot by the arrow alone (race.Target); a cockpit picture under the skybridge");
            yield return Clock(13f);
            RaceController.RouteOverride = "air_tour.json"; RaceController.DirectionOverride = "forward"; RaceController.LapsOverride = 1; RaceController.AiCount = 0;
            if (race.State != RaceController.Stage.Free) race.EndRace();
            race.Reload(); var r = race.Route; if (r == null) { Check("air: the route", false); yield break; }
            report.AppendLine($"  {r.Name}: {r.LapLength / 1000f:0.0} km, {r.Checkpoints.Count} rings");
            race.StartRace(); R73Pad();
            var pl = car.GetComponent<PlaneFlight>(); if (!pl) { Check("air: the plane", false); yield break; }
            var events = new List<string>(); void OnLog(string l) { events.Add($"{race.RaceClock,7:0.0}  {l}"); } race.Logged += OnLog;
            while (race.State == RaceController.Stage.Countdown) { R73Set(0, 0, 0); yield return null; }
            var p = race.Player; float t = 0f, bankCmd = 0f, pitchCmd = 0f; var seen = new HashSet<int>(); int crashes0 = pl.Crashes; bool cockpit = false;
            float seed = (float)r73rng.NextDouble() * 50f; var views = FindAnyObjectByType<CameraViews>();
            Time.timeScale = 2f; Time.maximumDeltaTime = .5f;
            while (t < 1100f && !p.Finished)
            {
                yield return new WaitForFixedUpdate(); float dt = Time.fixedDeltaTime; t += dt;
                var tgt = race.Target; if (tgt == null) continue;
                Vector3 aimAt = tgt.Position; float dist = (tgt.Position - car.Body.position).magnitude;
                int idx = tgt.Id; if (!seen.Contains(idx) && dist < 450f) { seen.Add(idx); yield return Capture($"78_air_ring_{(idx == 0 ? "line" : idx.ToString())}_ahead"); }
                if (!cockpit && tgt.Label != null && tgt.Label.Contains("skybridge") && dist < 70f && views) { cockpit = true; views.SetView(CameraViews.View.FirstPerson); Time.timeScale = .5f; yield return new WaitForSecondsRealtime(.15f); yield return Capture("78_air_under_skybridge_cockpit"); Time.timeScale = 2f; views.SetView(CameraViews.View.Chase); }
                var to = aimAt - car.Body.position; var fw = car.transform.forward;
                float yawErr = Vector3.SignedAngle(Flat(fw), Flat(to), Vector3.up) + (Mathf.PerlinNoise(seed, t * .3f) - .5f) * 4f;
                if (Flat(to).magnitude > 600f)
                {
                    float hiGround = float.MinValue; var dirF = Flat(to).normalized;
                    for (float k = 0f; k <= 600f; k += 50f) { var w = car.Body.position + dirF * k; float gh = GroundQuery.Height(w.x, w.z); if (!float.IsNaN(gh)) hiGround = Mathf.Max(hiGround, gh); }
                    float floorY = hiGround + 55f; if (aimAt.y < floorY) { aimAt.y = floorY; to = aimAt - car.Body.position; }
                }
                // near a ring, aim along the route's own line through it (the line's height), as a pilot lines up on the ring's frame
                if (dist < 600f) { float sR = r.LapStationOf(car.Body.position); var ahead = r.PointAt(r.GateS + sR + Mathf.Clamp(car.Body.linearVelocity.magnitude * 1.6f, 40f, 140f), out _); if ((ahead - car.Body.position).magnitude < dist) { aimAt = ahead; to = aimAt - car.Body.position; } }
                float climb = Mathf.Atan2(to.y, Flat(to).magnitude) * Mathf.Rad2Deg, pitchNow = pl.PitchDeg;
                float bankWant = Mathf.Clamp(yawErr * 2.0f, -65f, 65f), roll = pl.RollDeg;
                bankCmd += (Mathf.Clamp((bankWant - roll) / 18f, -1f, 1f) - bankCmd) * Mathf.Clamp01(dt / .2f);
                float pitchWant = Mathf.Clamp(climb, -28f, 28f); pitchCmd += (Mathf.Clamp((pitchWant - pitchNow) / 10f, -1f, 1f) - pitchCmd) * Mathf.Clamp01(dt / .2f);
                if (!pl.Airborne) { bankCmd = Mathf.Clamp(yawErr / 30f, -1f, 1f) * .4f; pitchCmd = pl.Airspeed > 32f ? .7f : 0f; }
                R73Set(bankCmd, 1f, 0f, -pitchCmd);
            }
            Time.timeScale = 1f; race.Logged -= OnLog; R73Set(0, 0, 0);
            foreach (var e in events.Where(x => x.Contains("CHECKPOINT") || x.Contains("PENALTY") || x.Contains("RING") || x.Contains("CRASH") || x.Contains("LAP") || x.Contains("FINISH"))) report.AppendLine("  " + e);
            report.AppendLine($"  {(p.Finished ? "finished the lap in " + RaceController.Fmt(p.FinalTotal) : "DID NOT finish")} ({t:0} s of game time); missed {p.MissedTotal}, penalty {p.PenaltyTotal:0} s; crashes {pl.Crashes - crashes0}{(pl.Crashes > crashes0 ? " (last: " + pl.CrashReason + ")" : "")}; cockpit picture under the skybridge: {cockpit}");
            Check("air: the tour flown by the arrow alone, finished", p.Finished, $"next ring {p.NextCheckpoint + 1}");
            Check("air: the cockpit picture under the skybridge", cockpit);
            race.EndRace();
        }
    }
}
