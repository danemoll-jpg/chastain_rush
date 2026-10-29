using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Racer;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace ChastainRush
{
    // Round 80 in the exe (-exeRound80 <dir> [-r80Only dan,whitlock,deck,ddecks,dreplay,cockpit,air,airhuman,lanes,traffic] -r20Minutes 300 -logFile <dir>\Player.log),
    // started from the repo root (it reads local/round80/r80_express.json): CHECK IT AS DAN PLAYS IT (TODO.md item 0).
    //   dan       BUG-001 from Dan's exact place and heading (Whitlock Avenue, the square race, the train passing): what he sees now
    //   whitlock  C: the square race (1 lap, 3 AI), the human-like driver over the tie stack while a train passes (a picture in the air over it), and
    //             the same race waiting at the gates (the time through the crossing each way)
    //   deck      B: 5 square races x 3 AI on Normal, clean and with Normal traffic (the train meets the leader at Mill Street as always): every AI's turning
    //             traced (a loop: more than 300 degrees turned within 60 m), the time from Waddell to Haynes through the deck. Before: run with
    //             -routeDir local/round80/route_before -r80ProjectOld
    //   ddecks    D1: every Express Lanes deck driven both ways by the human-like driver (up to 33 m/s): air and landings
    //   dreplay   D2: the square race over the Mill Street pile, then its replay: the chase camera against the ballast pile at every moment near it
    //   cockpit   D3: both planes' cockpit view in flight (pictures)
    //   air       A: each air course x Easy / Normal / Hard x 2 races, 3 AI pilots, 2 laps, the player parked: their missed rings, mistakes, lap spread
    //   airhuman  A: each air course on Normal and Hard, 1 lap, 3 AI: the human-like pilot (the pad, a lag, a little noise) misses one ring on purpose
    //             and goes back for it: where it finishes
    //   lanes     E: the lanes at 6 AM, noon, 5 PM and midnight (Heavy traffic): the gates (pictures from a driver's place), traffic counted each way;
    //             then a closed gate smashed at 5 PM and the lanes driven the wrong way (oncoming traffic counted)
    //   traffic   F: traffic counted on I-75 and I-575 and the frame rate at I-75 / Barrett Parkway, I-575, the square and Town Center (Normal and Heavy);
    //             -r80TrafficOld gives Round 79's numbers (before)
    public sealed partial class Round20Runner
    {
        static readonly Vector3 R80Dan = new Vector3(1608.55f, 375.95f, -8524.74f), R80Whitlock = new Vector3(1624.6f, 0f, -8521.3f);
        const float R80DanYaw = 102.16f;
        [Serializable] class R80Deck { public string name; public float length; public float[] line; }
        [Serializable] class R80File { public R80Deck[] decks; }

        RailCrossing R80Crossing(Vector3 near) => FindObjectsByType<RailCrossing>(FindObjectsSortMode.None).OrderBy(c => Flat(c.center - near).magnitude).FirstOrDefault();

        // ---------------------------------------------------------------- dan
        IEnumerator R80DanPart()
        {
            report.AppendLine(); report.AppendLine("=== Dan's BUG-001 (session 2026-10-28_17-26-14-935_f93341): Whitlock Avenue, the square race, the train passing, from his place and heading");
            yield return Clock(6f);
            UseRoute("marietta_square.json", "forward"); yield return Vehicle("drifter");
            RaceController.LapsOverride = 1; RaceController.AiCount = 0; race.StartRace(); R73Pad();
            while (race.State == RaceController.Stage.Countdown) yield return null;
            yield return R74Place(R80Dan, R80DanYaw);
            var cross = R80Crossing(R80Whitlock); RailTrains.Instance.Align(cross.center, 2f, true);
            float t = 0f; while (t < 9f) { R73Set(0, 0, 0); yield return new WaitForFixedUpdate(); t += Time.fixedDeltaTime; }   // (the brake held on a stopped car reverses it)
            yield return Capture("80_BUG-001_after");
            report.AppendLine($"  at ({car.Body.position.x:0.0}, {car.Body.position.z:0.0}) heading {car.transform.eulerAngles.y:0}: the crossing {(cross.Closed ? "closed (the train passing)" : "open")}, its arms at {cross.ArmAngle:0} degrees; picture 80_BUG-001_after.png (the tie stack to the right of the road before the tracks, the loading dock past them)");
            Check("dan: the train passing Whitlock Avenue as in Dan's picture", cross.Closed);
            race.EndRace();
        }

        // ---------------------------------------------------------------- whitlock
        // the race's line with the Whitlock jump: off Whitlock's eastbound lane onto the tie stack, over the tracks, down the loading dock's ramp, back onto Whitlock
        static List<Vector3> R80JumpLine(List<Vector3> line)
        {
            // (lined up on the stack from just past the corner, straight on its line 15 m before its foot: a person lines a jump up early)
            const float kz = -8530f, footX = 1596.7f, dockFoot = 1676.3f, lined = footX - 15f;
            int i0 = line.FindIndex(p => p.x > 1546f && p.x < 1560f && Mathf.Abs(p.z + 8524f) < 6f); int i1 = line.FindIndex(p => p.x > 1695f && Mathf.Abs(p.z + 8523f) < 6f);
            if (i0 < 0 || i1 < 0) return line;
            var mid = new List<Vector3>();
            for (float x = line[i0].x + 3f; x < lined; x += 3f) { float u = Mathf.InverseLerp(line[i0].x, lined, x); mid.Add(RaceRoute.Ground(x, Mathf.Lerp(line[i0].z, kz, Mathf.SmoothStep(0f, 1f, u)))); }
            for (float x = lined; x < dockFoot + 4f; x += 3f) mid.Add(new Vector3(x, 0f, kz));
            for (float x = dockFoot + 4f; x < line[i1].x - 2f; x += 3f) { float u = Mathf.InverseLerp(dockFoot + 4f, line[i1].x, x); mid.Add(RaceRoute.Ground(x, Mathf.Lerp(kz, line[i1].z, Mathf.SmoothStep(0f, 1f, u)))); }
            var outL = line.Take(i0 + 1).ToList(); outL.AddRange(mid); outL.AddRange(line.Skip(i1)); return outL;
        }

        IEnumerator R80WhitlockPart()
        {
            report.AppendLine(); report.AppendLine("=== C. The Whitlock Avenue jump: the square race (1 lap, 3 AI, Normal), the human-like driver (Street Classic); a southbound train told to reach Whitlock as it comes");
            yield return Clock(13f);
            var through = new Dictionary<string, float>();
            foreach (var mode in new[] { "jump", "wait" })
            {
                UseRoute("marietta_square.json", "forward"); var r = race.Route; if (r == null) { Check("whitlock: the route", false); yield break; }
                yield return Vehicle("original");
                RaceController.LapsOverride = 1; RaceController.AiCount = 3; RoadDriver.Difficulty = 1; race.StartRace(); R73Pad();
                while (race.State == RaceController.Stage.Countdown) yield return null;
                var line = new List<Vector3>(r.LeadIn ?? new Vector3[0]); for (float s = 0f; s <= r.LapLength; s += 3f) line.Add(r.PointAt(r.GateS + s, out _));
                if (mode == "jump") line = R80JumpLine(line);
                var rt = RailTrains.Instance; var cross = R80Crossing(R80Whitlock);
                var wtrace = new StringBuilder("t\tx\ty\tz\tspeed\twheels\theading\tclosed\n"); float nextW = 0f; bool aligned = false; float alignedAt = -1f, tIn = -1f, tOut = -1f, overTrain = float.NaN, waitedFor = 0f; bool trainUnder = false, closedWhenOver = false;
                Func<float, float> speedAt = s =>
                {
                    var p = car.Body.position; float d = Flat(p - cross.center).magnitude;
                    // on Whitlock past the corner: the train's front over the crossing 2.5 s before the car gets there (jump: its cars under the flight), or 4 s after
                    // (wait: the gates down, the train coming)
                    if (!aligned && p.x > 1545f && p.x < 1590f && p.z < -8512f && cross.Closed) { aligned = true; alignedAt = -2f; }   // the race's own train is there already: left as it is
                    if (!aligned && p.x > 1545f && p.x < 1590f && p.z < -8512f) { aligned = true; alignedAt = Time.time; float eta = (cross.center.x - p.x) / Mathf.Max(8f, car.Body.linearVelocity.magnitude); rt.Align(cross.center, mode == "jump" ? Mathf.Max(.5f, eta - 2.5f) : eta + 4f, true); }
                    if (p.x > 1540f && p.x < 1700f && p.z < -8500f && p.z > -8560f && Time.time >= nextW) { nextW = Time.time + .1f; var v = car.Body.linearVelocity; wtrace.AppendLine($"{Time.time:0.0}\t{p.x:0.0}\t{p.y:0.0}\t{p.z:0.0}\t{v.magnitude:0.0}\t{car.GroundedWheels}\t{car.transform.eulerAngles.y:0}\t{(cross.Closed ? 1 : 0)}"); }
                    if (tIn < 0f && p.x > 1545f && p.x < 1575f && p.z < -8512f) tIn = Time.time;
                    if (tIn > 0f && tOut < 0f && p.x > 1700f && p.z < -8512f) tOut = Time.time;
                    if (mode == "jump") { if (p.x > 1560f && p.x < 1615f && Mathf.Abs(p.z + 8528f) < 8f) return 25f; return 99f; }
                    if (RailTrains.ClosedAhead(p, car.transform.forward, 120f, out float stop, out _, out _)) { if (car.Body.linearVelocity.magnitude < .8f) waitedFor += Time.fixedDeltaTime; return stop < 50f ? 0f : 12f; }
                    return 99f;
                };
                var run = new HumanRun(); Time.timeScale = 1.5f; Time.maximumDeltaTime = .5f;
                var pics = new List<(string, float)> { ($"80_C_{mode}_approach", 1080f + 60f) };
                // over the tracks, every step (the driver's own step does not run in the air): the crossing, and a train car under the car
                var overWatch = StartCoroutine(R73Watch(() =>
                {
                    var p = car.Body.position; if (p.x <= 1619f || p.x >= 1631f || p.z > -8505f || p.z < -8545f || car.GroundedWheels > 0) return;
                    closedWhenOver |= cross.Closed;
                    if (Physics.Raycast(p, Vector3.down, out var hit, 20f, ~0, QueryTriggerInteraction.Ignore) && hit.collider.GetComponentInParent<TrainCar>()) { trainUnder = true; overTrain = float.IsNaN(overTrain) ? hit.distance : Mathf.Min(overTrain, hit.distance); }
                }, () => true));
                yield return HumanDrive(line, 30f, 300f, run, () => race.Player.Finished, speedAt, pics, $"80_C_{mode}_in_the_air");
                StopCoroutine(overWatch);
                Time.timeScale = 1f; File.WriteAllText(Path.Combine(outputDir, $"80_C_{mode}_trace.tsv"), wtrace.ToString());
                float secs = tIn > 0f && tOut > 0f ? tOut - tIn : float.NaN; through[mode] = secs;
                report.AppendLine($"  {mode}: {(race.Player.Finished ? "finished " + RaceController.Fmt(race.Player.FinalTotal) : "NOT finished")}; resets {run.resets}; longest in the air {run.maxAir:0.00} s; " +
                                  $"the train {(alignedAt == -2f ? "already there (the race's own)" : aligned ? "lined up" : "NOT lined up")}; Whitlock from North Marietta Parkway to past the dock {secs:0.0} s (game time); waited at the gates {waitedFor:0.0} s; " +
                                  (mode == "jump" ? $"over the tracks: the crossing {(closedWhenOver ? "closed" : "open")}, a train car under the car {(trainUnder ? $"yes ({overTrain:0.0} m under the car's middle)" : "no")}" : "") + run.note);
                if (mode == "jump") Check("whitlock: over the tracks off the tie stack (over 1.2 s in the air), a train under it, no reset", run.maxAir > 1.2f && trainUnder && run.resets == 0, $"air {run.maxAir:0.00} s, train under {trainUnder}, resets {run.resets}");
                if (mode == "wait") Check("whitlock: on the road the train stops you (no free way round)", waitedFor > 3f, $"waited {waitedFor:0.0} s");
                race.EndRace(); yield return new WaitForSecondsRealtime(1f);
            }
            report.AppendLine($"  the jump against waiting: {(through.TryGetValue("jump", out var tj) ? tj : float.NaN):0.0} s against {(through.TryGetValue("wait", out var tw) ? tw : float.NaN):0.0} s through the stretch");
        }

        // ---------------------------------------------------------------- deck
        IEnumerator R80DeckPart()
        {
            bool before = RoadDriver.ProjectAnywhere; string label = before ? "BEFORE (Round 79's route, the nearest point anywhere)" : "after";
            report.AppendLine(); report.AppendLine($"=== B. The square race's deck, {label}: 5 races x 3 AI on Normal, clean and with Normal traffic; loops (over 300 degrees turned within 60 m) and the time Waddell -> Haynes");
            yield return Clock(13f);
            int prev = RoadDriver.Difficulty; RoadDriver.Difficulty = 1;
            foreach (var cond in new[] { "clean", "traffic" })
            {
                Traffic.ForcedLevel = cond == "traffic" ? 2 : 0;
                int loops = 0; var deckTimes = new List<float>(); var where = new List<string>(); int unfinished = 0;
                for (int k = 0; k < 5; k++)
                {
                    yield return StartRoute("marietta_square.json", "forward", 3, 1);
                    var ai = race.Entrants.Where(e => !e.IsPlayer).ToList();
                    var hist = ai.ToDictionary(e => e, e => new List<(float dist, float head)>());
                    var dist = ai.ToDictionary(e => e, e => 0f); var last = ai.ToDictionary(e => e, e => e.Car.Body.position);
                    var lastHead = ai.ToDictionary(e => e, e => e.Car.transform.eulerAngles.y); var unwrapped = ai.ToDictionary(e => e, e => 0f);
                    var tin = ai.ToDictionary(e => e, e => -1f); var tout = ai.ToDictionary(e => e, e => -1f);
                    bool tracing = true;
                    IEnumerator Trace()
                    {
                        while (tracing)
                        {
                            yield return new WaitForFixedUpdate();
                            foreach (var e in ai)
                            {
                                if (!e.Car || e.Finished) continue; var p = e.Car.Body.position;
                                float step = Flat(p - last[e]).magnitude; last[e] = p; if (step > 8f) { hist[e].Clear(); continue; }   // a reset
                                dist[e] += step; float h = e.Car.transform.eulerAngles.y; unwrapped[e] += Mathf.DeltaAngle(lastHead[e], h); lastHead[e] = h;
                                var hl = hist[e]; hl.Add((dist[e], unwrapped[e])); while (hl.Count > 0 && hl[0].dist < dist[e] - 60f) hl.RemoveAt(0);
                                if (hl.Count > 0 && Mathf.Abs(unwrapped[e] - hl[0].head) > 300f) { loops++; where.Add($"{e.Name} race {k + 1} at ({p.x:0}, {p.z:0})"); hl.Clear(); }
                                if (tin[e] < 0f && Mathf.Abs(p.x - 1904f) < 12f && p.z > -8432f && p.z < -8410f) tin[e] = Time.time;
                                if (tin[e] > 0f && tout[e] < 0f && p.x > 1986f && p.z < -8392f && p.z > -8410f) tout[e] = Time.time;
                            }
                        }
                    }
                    StartCoroutine(Trace());
                    Time.timeScale = 2f; Time.maximumDeltaTime = .5f;
                    yield return RideAlong(ai, 5f * 60f);
                    Time.timeScale = 1f; tracing = false;
                    foreach (var e in ai) { if (tin[e] > 0f && tout[e] > 0f) deckTimes.Add(tout[e] - tin[e]); if (!e.Finished) unfinished++; }
                    race.EndRace(); yield return new WaitForSecondsRealtime(.5f);
                }
                Traffic.ForcedLevel = 0;
                report.AppendLine($"  {cond}: loops {loops}{(where.Count > 0 ? " (" + string.Join("; ", where.Take(8)) + ")" : "")}; Waddell -> Haynes through the deck {deckTimes.Count} times, " +
                                  $"{(deckTimes.Count > 0 ? $"{deckTimes.Average():0.0} s average, {deckTimes.Min():0.0} to {deckTimes.Max():0.0} s" : "-")}; not finished {unfinished}");
                if (!before) Check($"deck {cond}: no AI turns a full circle anywhere", loops == 0, $"{loops}");
            }
            RoadDriver.Difficulty = prev;
        }

        // ---------------------------------------------------------------- ddecks
        IEnumerator R80DDecks()
        {
            report.AppendLine(); report.AppendLine("=== D1. Every Express Lanes deck both ways, the human-like driver up to 33 m/s (Street Classic, no traffic): air and landings");
            var path = Path.Combine(Environment.CurrentDirectory, "local/round80/r80_express.json");
            if (!File.Exists(path)) { Check("ddecks: local/round80/r80_express.json (python Tools/mapgen/r80_express_lines.py)", false); yield break; }
            var f = JsonUtility.FromJson<R80File>(File.ReadAllText(path));
            if (race.State != RaceController.Stage.Free) race.EndRace();
            yield return Vehicle("original"); yield return Clock(13f);
            int bad = 0;
            foreach (var d in f.decks)
                foreach (bool back in new[] { false, true })
                {
                    var line = R73Line(d.line); if (back) line.Reverse();
                    var f0 = Flat(line[2] - line[0]).normalized; yield return R74Place(RaceRoute.Surface(line[0].x, line[0].z), Mathf.Atan2(f0.x, f0.z) * Mathf.Rad2Deg); R73Pad();
                    var run = new HumanRun(); float jolt = 0f, lastVy = 0f, air = 0f, airBest = 0f; Vector3 airFrom = Vector3.zero, airAt = Vector3.zero, joltAt = Vector3.zero;
                    var watch = StartCoroutine(R73Watch(() =>
                    {
                        var p = car.Body.position; float vy = car.Body.linearVelocity.y;
                        if (car.GroundedWheels > 0) { if (Mathf.Abs(vy - lastVy) > jolt) { jolt = Mathf.Abs(vy - lastVy); joltAt = p; } air = 0f; }
                        else { if (air == 0f) airFrom = p; air += Time.fixedDeltaTime; if (air > airBest) { airBest = air; airAt = airFrom; } }
                        lastVy = vy;
                    }, () => true));
                    yield return HumanDrive(line, 33f, 120f, run); StopCoroutine(watch);
                    bool ok = run.reachedEnd && run.maxAir < .5f && jolt < 6f && run.resets == 0; if (!ok) bad++;
                    report.AppendLine($"  {d.name} {(back ? "the other way" : "its own way")}: {(run.reachedEnd ? "through" : "NOT through")} in {run.time:0.0} s, top {run.topSpeed:0} m/s, longest in the air {run.maxAir:0.00} s " +
                                      $"(off at ({airAt.x:0}, {airAt.z:0})), hardest landing {jolt:0.0} m/s in a step (at ({joltAt.x:0}, {joltAt.z:0})), resets {run.resets}{run.note}{(ok ? "" : "  <- LOOK")}");
                }
            Check("ddecks: every Express Lanes deck both ways through, under 0.5 s in the air, no landing over 6 m/s", bad == 0, $"{bad} not");
        }

        // ---------------------------------------------------------------- dreplay
        IEnumerator R80DReplay()
        {
            report.AppendLine(); report.AppendLine("=== D2. The replay's chase camera at the Mill Street pile: the square race over it (1 lap, no AI), then every quarter second near the pile in the replay");
            yield return Clock(13f);
            UseRoute("marietta_square.json", "forward"); var r = race.Route; yield return Vehicle("original");
            RaceController.LapsOverride = 1; RaceController.AiCount = 0; RoadDriver.Difficulty = 1; race.StartRace(); R73Pad();
            while (race.State == RaceController.Stage.Countdown) yield return null;
            var line = new List<Vector3>(r.LeadIn ?? new Vector3[0]); for (float s = 0f; s <= r.LapLength; s += 3f) line.Add(r.PointAt(r.GateS + s, out _));
            line = R78JumpLine(line);
            var pile = new Vector3(1637.5f, 0f, -8403.6f); var near = new List<float>();
            Func<float, float> speedAt = s => { var p = car.Body.position; if (Flat(p - pile).magnitude < 45f) near.Add(race.RaceClock); if (p.x > 1637.5f && p.x < 1747.5f && Mathf.Abs(p.z + 8406f) < 12f) return 26f; return 99f; };
            var run = new HumanRun(); yield return HumanDrive(line, 30f, 300f, run, () => race.Player.Finished, speedAt);
            float w = 0f; while ((ReplayRecorder.Instance && ReplayRecorder.Instance.Recording) && w < 150f) { yield return new WaitForSecondsRealtime(.5f); w += .5f; }
            var data = ReplayRecorder.Instance ? ReplayRecorder.Instance.Last : null; if (data == null || near.Count == 0) { Check("dreplay: a replay past the pile", false, $"{near.Count} moments"); yield break; }
            var v = ReplayViewer.Open(data); if (!v) { Check("dreplay: the viewer", false); yield break; }
            yield return new WaitForSecondsRealtime(1f); v.TogglePause(); v.SetView(ReplayViewer.Cam.Chase);
            var cam = Camera.main; int n = 0, blocked = 0; float worstT = -1f;
            for (float t = near.Min() - 1f; t <= near.Max() + 1f; t += .25f)
            {
                v.Seek(t); v.SetView(ReplayViewer.Cam.Chase); yield return null; yield return null; yield return null;
                var c = cam ? cam.transform.position : Vector3.zero;
                var puppet = FindObjectsByType<ArcadeVehicle>(FindObjectsSortMode.None).Where(a => a.Puppet).OrderBy(a => (a.transform.position - c).sqrMagnitude).FirstOrDefault();
                if (!puppet) continue; var pivot = puppet.transform.position + Vector3.up;
                bool inside = Physics.Linecast(pivot, c, out var hit, 1, QueryTriggerInteraction.Ignore) || Physics.CheckSphere(c, .15f, 1, QueryTriggerInteraction.Ignore);
                n++; if (inside) { blocked++; if (worstT < 0f) worstT = t; }
            }
            v.Seek(near.Min() + (near.Max() - near.Min()) * .4f); v.SetView(ReplayViewer.Cam.Chase); yield return new WaitForSecondsRealtime(.6f); yield return Capture("80_D2_replay_chase_at_the_pile");
            if (worstT >= 0f) { v.Seek(worstT); v.SetView(ReplayViewer.Cam.Chase); yield return new WaitForSecondsRealtime(.6f); yield return Capture("80_D2_replay_chase_blocked"); }
            v.Close(); yield return new WaitForSecondsRealtime(1f); race.EndRace();
            report.AppendLine($"  {n} moments near the pile, the chase camera behind something or inside it at {blocked}; pictures 80_D2_replay_chase_at_the_pile.png{(worstT >= 0f ? ", 80_D2_replay_chase_blocked.png" : "")}");
            Check("dreplay: the replay's chase camera never inside or behind the pile", n > 10 && blocked == 0, $"{blocked} of {n}");
        }

        // ---------------------------------------------------------------- cockpit
        IEnumerator R80Cockpit()
        {
            report.AppendLine(); report.AppendLine("=== D3. The planes' cockpit view in flight (the camera at the pilot's eyes)");
            if (race.State != RaceController.Stage.Free) race.EndRace();
            yield return Clock(13f);
            var views = FindAnyObjectByType<CameraViews>();
            foreach (var id in new[] { "plane", "dart" })
            {
                yield return Vehicle(id); yield return new WaitForSecondsRealtime(.5f);
                var pl = car.GetComponent<PlaneFlight>(); if (!pl) { Check($"cockpit {id}: a plane", false); continue; }
                pl.AirStart(new Vector3(-2300f, 520f, -5200f), 30f, 0f, 70f); R73Pad();
                if (views) views.SetView(CameraViews.View.FirstPerson);
                float t = 0f; while (t < 2.5f) { R73Set(0, .7f, 0); yield return null; t += Time.deltaTime; }
                var cam = Camera.main; var rider = car.GetComponentsInChildren<Transform>(true).FirstOrDefault(x => x.name == "Rider" && x.parent && x.parent.name == "Steering pose");   // the rider's holder (VehicleVisual.Rider)
                var local = rider && cam ? car.transform.InverseTransformPoint(cam.transform.position) - car.transform.InverseTransformPoint(rider.position) : Vector3.zero;
                yield return Capture($"80_D3_cockpit_{id}");
                report.AppendLine($"  {id}: the camera {local.x:0.00} / {local.y:0.00} / {local.z:0.00} m (right / up / forward) from the pilot's seat; picture 80_D3_cockpit_{id}.png");
                Check($"cockpit {id}: the camera at the pilot's head (0.6 to 0.9 m over the seat, ahead of it, beside it under 0.2 m)", local.y > .6f && local.y < .9f && local.z > 0f && Mathf.Abs(local.x) < .2f, $"{local}");
                if (views) views.SetView(CameraViews.View.Chase);
            }
            yield return Vehicle("original");
        }

        // ---------------------------------------------------------------- air (the AI's mistakes)
        IEnumerator R80Air()
        {
            report.AppendLine(); report.AppendLine("=== A. The AI pilots' mistakes: each air course x Easy / Normal / Hard x 2 races, 3 AI, 2 laps, the player parked on the runway");
            yield return Clock(13f); int prev = RoadDriver.Difficulty;
            var csv = new StringBuilder("course,difficulty,race,ai,plane,finished,total,lap1,lap2,missed,wide,late,careful,went_back,back_done,off_laps,crashes\n");
            foreach (var file in R79Courses)
                for (int diff = 0; diff <= 2; diff++)
                {
                    var missed = new List<int>(); var laps = new List<float>(); var spread = new List<float>(); var totals = new List<float>(); int notDone = 0;
                    for (int k = 0; k < 2; k++)
                    {
                        RoadDriver.Difficulty = diff; RaceController.RouteOverride = file; RaceController.DirectionOverride = "forward"; RaceController.LapsOverride = 2; RaceController.AiCount = 3;
                        if (race.State != RaceController.Stage.Free) race.EndRace();
                        race.Reload(); race.StartRace(); R73Pad();
                        while (race.State == RaceController.Stage.Countdown) { R73Set(0, 0, 0); yield return null; }
                        var ai = race.Entrants.Where(e => !e.IsPlayer).ToList();
                        Time.timeScale = 3f; Time.maximumDeltaTime = .5f;
                        float end = Time.realtimeSinceStartup + 9f * 60f;
                        while (Time.realtimeSinceStartup < end && ai.Any(e => !e.Finished)) { R73Set(0, 0, 1f); yield return new WaitForSecondsRealtime(.5f); }
                        Time.timeScale = 1f;
                        foreach (var e in ai)
                        {
                            var pp = e.Car ? e.Car.GetComponent<PlanePilot>() : null; var pf = e.Car ? e.Car.GetComponent<PlaneFlight>() : null;
                            missed.Add(e.MissedTotal); laps.AddRange(e.LapTimes); if (e.LapTimes.Count >= 2) spread.Add(Mathf.Abs(e.LapTimes[1] - e.LapTimes[0]));
                            if (e.Finished && !e.Estimated) totals.Add(e.FinalTotal); else notDone++;
                            csv.AppendLine($"{Path.GetFileNameWithoutExtension(file)},{diff},{k + 1},{e.Name},{e.VehicleName},{(e.Finished && !e.Estimated ? 1 : 0)},{e.FinalTotal:0.0},{(e.LapTimes.Count > 0 ? e.LapTimes[0] : 0f):0.0},{(e.LapTimes.Count > 1 ? e.LapTimes[1] : 0f):0.0}," +
                                           $"{e.MissedTotal},{pp?.Wide},{pp?.Late},{pp?.Careful},{pp?.Backs},{pp?.BacksDone},{pp?.OffLaps},{pf?.Crashes}");
                        }
                        race.EndRace(); yield return new WaitForSecondsRealtime(.5f); Flush();
                    }
                    string dn = diff == 2 ? "Hard" : diff == 1 ? "Normal" : "Easy";
                    report.AppendLine($"  {Path.GetFileNameWithoutExtension(file),-18} {dn,-6}: missed rings per AI a race {(missed.Count > 0 ? missed.Average() : 0):0.0} (0 to {(missed.Count > 0 ? missed.Max() : 0)}), " +
                                      $"laps {(laps.Count > 0 ? $"{laps.Min():0.0} to {laps.Max():0.0} s" : "-")}, one AI's two laps {(spread.Count > 0 ? $"{spread.Average():0.0} s apart on average (up to {spread.Max():0.0})" : "-")}, " +
                                      $"totals {(totals.Count > 0 ? $"{totals.Min():0.0} to {totals.Max():0.0} s" : "-")}, not finished {notDone}");
                }
            File.WriteAllText(Path.Combine(outputDir, "80_A_air_ai.csv"), csv.ToString());
            report.AppendLine("  every AI and race: 80_A_air_ai.csv");
            RoadDriver.Difficulty = prev;
        }

        // ---------------------------------------------------------------- airhuman
        // the human-like pilot: the AI pilot's law worked out (PlanePilot.Advise), put through the pad 0.1 s late with a little noise (the stick's dead
        // zone allowed for, the throttle by the triggers, the rudder by the shoulders), as a good player flies; it misses ring 'miss' on purpose and goes back
        GamepadState r80State;
        IEnumerator R80HumanFly(float limit, float scale, StringBuilder note)
        {
            var pl = car.GetComponent<PlaneFlight>(); var p = race.Player;
            var adv = car.gameObject.AddComponent<PlanePilot>(); adv.Advise = true; adv.Initialize(race, car, p, 0, 0f); R73Pad();
            var q = new Queue<(float t, float pitch, float bank, float rud, float thr, float steer)>(); float t = 0f, seed = (float)r73rng.NextDouble() * 50f; int crashes0 = pl.Crashes;
            // the stick's dead zone is radial (min 0.12, max 0.95): the wanted (bank, pitch) put through it the other way round, as a player's thumb does
            Vector2 Stick(Vector2 c) { float m = c.magnitude; if (m < .01f) return Vector2.zero; m = Mathf.Min(m, 1f); return c / c.magnitude * Mathf.Min(1f, .12f + m * .83f); }
            Time.timeScale = scale; Time.maximumDeltaTime = .5f;
            while (t < limit && !p.Finished)
            {
                yield return new WaitForFixedUpdate(); t += Time.fixedDeltaTime;
                q.Enqueue((t, adv.aPitch, adv.aBank, adv.aRudder, adv.aThrottle, adv.aSteer));
                var c = q.Peek(); while (q.Count > 1 && t - q.Peek().t > .1f) c = q.Dequeue();
                float n1 = (Mathf.PerlinNoise(seed, t * .7f) - .5f) * .14f, n2 = (Mathf.PerlinNoise(seed + 9f, t * .7f) - .5f) * .1f;
                var sv = Stick(new Vector2(Mathf.Clamp(pl.Airborne ? c.bank + n1 : c.steer, -1f, 1f), Mathf.Clamp(c.pitch + n2, -1f, 1f))); float x = sv.x, y = -sv.y;
                float up = c.thr < 0f ? 0f : c.thr > pl.Throttle + .02f ? 1f : 0f, down = c.thr >= 0f && c.thr < pl.Throttle - .05f ? 1f : 0f;
                var st = new GamepadState { leftStick = new Vector2(x, y), rightTrigger = up, leftTrigger = down };
                if (c.rud > .5f) st = st.WithButton(GamepadButton.RightShoulder); else if (c.rud < -.5f) st = st.WithButton(GamepadButton.LeftShoulder);
                InputSystem.QueueStateEvent(r73pad, st);
            }
            Time.timeScale = 1f; Destroy(adv); R73Set(0, 0, 0);
            note.Append($" crashes {pl.Crashes - crashes0};");
        }

        IEnumerator R80AirHuman()
        {
            report.AppendLine(); report.AppendLine("=== A. The human-like pilot (the pad, 0.1 s late, a little noise; Street Classic's pilot in the Sky Hopper) against 3 AI, 1 lap: one ring missed on purpose and gone back for");
            yield return Clock(13f); int prev = RoadDriver.Difficulty;
            foreach (var file in R79Courses)
                foreach (int diff in new[] { 1, 2 })
                {
                    RoadDriver.Difficulty = diff; RaceController.RouteOverride = file; RaceController.DirectionOverride = "forward"; RaceController.LapsOverride = 1; RaceController.AiCount = 3;
                    if (race.State != RaceController.Stage.Free) race.EndRace();
                    race.Reload(); var r = race.Route; if (r == null) { Check($"airhuman {file}: the route", false); continue; }
                    // the ring missed on purpose: the first one from the third on that stands 70 m or more over the ground
                    int missIdx = -1; for (int i = 2; i < r.Checkpoints.Count - 1; i++) { var g = r.Checkpoints[i]; float gy = GroundQuery.Height(g.Position.x, g.Position.z); if (!float.IsNaN(gy) && g.Position.y - gy > 70f) { missIdx = i; break; } }
                    PlanePilot.ForceMiss = missIdx >= 0 ? r.Checkpoints[missIdx].Id : -1;
                    race.StartRace(); R73Pad();
                    while (race.State == RaceController.Stage.Countdown) { R73Set(0, 0, 0); yield return null; }
                    bool skipped = false; var note = new StringBuilder();
                    var watch = StartCoroutine(R73Watch(() => { if (missIdx >= 0 && race.Player.RingSkipped == missIdx) skipped = true; }, () => true));
                    yield return R80HumanFly(900f, 2f, note);
                    StopCoroutine(watch); PlanePilot.ForceMiss = -1;
                    float w = 0f; Time.timeScale = 3f; while (race.Entrants.Any(e => !e.Finished) && w < 200f) { yield return new WaitForSecondsRealtime(.5f); w += .5f; } Time.timeScale = 1f;
                    var order = race.Entrants.Where(e => e.Finished).OrderBy(e => e.FinalTotal).ToList(); int place = order.IndexOf(race.Player) + 1;
                    bool wentBack = skipped && !race.Player.Missed.Contains(missIdx);
                    string dn = diff == 2 ? "Hard" : "Normal";
                    report.AppendLine($"  {r.Name} {dn}: the player {(race.Player.Finished ? $"{RaceController.Fmt(race.Player.FinalTotal)}, place {place} of {race.Entrants.Count}" : "NOT finished")}; ring {missIdx + 1} {(skipped ? "missed" : "NOT missed")}" +
                                      $"{(skipped ? wentBack ? " and gone back for (no penalty)" : ", not gone back for" : "")}; missed {race.Player.MissedTotal};{note} AI: " +
                                      string.Join("; ", race.Entrants.Where(e => !e.IsPlayer).Select(e => { var pp = e.Car ? e.Car.GetComponent<PlanePilot>() : null; return $"{e.Name} {(e.Finished ? RaceController.Fmt(e.FinalTotal) + (e.Estimated ? " est." : "") : "-")} (missed {e.MissedTotal}, back {pp?.BacksDone}/{pp?.Backs}, late {pp?.Late}, careful {pp?.Careful}, off-pace laps {pp?.OffLaps})"; })));
                    if (diff == 1) Check($"airhuman {r.Name} Normal: a ring missed and gone back for, and still the winner", wentBack && place == 1, $"went back {wentBack}, place {place}");
                    else Check($"airhuman {r.Name} Hard: a ring missed and gone back for, and in the pack (not last)", wentBack && place >= 1 && place < race.Entrants.Count, $"went back {wentBack}, place {place}");
                    race.EndRace(); yield return new WaitForSecondsRealtime(1f); Flush();
                }
            RoadDriver.Difficulty = prev;
        }

        // ---------------------------------------------------------------- express
        IEnumerator R80Express()
        {
            report.AppendLine(); report.AppendLine($"=== E. The Express Lanes at four times of day, traffic {(Traffic.Instance ? Traffic.LevelNames[Traffic.Instance.Level] : "none")} (needs -traffic Heavy)");
            if (race.State != RaceController.Stage.Free) race.EndRace();
            var lanes = ExpressLanes.Instance; var net = TrafficNetwork.Instance; if (!lanes || !net) { Check("express: the lanes and the traffic graph", false); yield break; }
            yield return Vehicle("original");
            report.AppendLine($"  {lanes.gates.Length} gates");
            // where to watch from: beside the I-575 lanes, north of the I-75 interchange
            var watchAt = new Vector3(392f, 0f, 418f);
            foreach (var (hour, label) in new[] { (6f, "6 AM"), (12f, "noon"), (17f, "5 PM"), (0f, "midnight") })
            {
                ExpressLanes.ForceHour = hour; ExpressLanes.ApplyNow(); yield return Clock(hour);
                report.AppendLine($"  {label}: the lanes {ExpressLanes.Describe(lanes.Now)}; gates " + string.Join(", ", lanes.gates.Select((g, i) => $"{i + 1} {(g.IsOpen ? "open" : "SHUT")}")));
                // the gates from a driver's place on each ramp (20 m out, facing them)
                for (int i = 0; i < lanes.gates.Length; i++)
                {
                    var g = lanes.gates[i]; var at = g.transform.position; var o = g.transform.forward; var a = g.transform.right;
                    yield return Still($"80_E_{hour:00}h_gate{i + 1}", at + o * 20f + a * 3f + Vector3.up * 1.6f, at + a * 3f + Vector3.up * 1f, 55f);
                }
                // traffic on the lanes for 60 s (game time, at 2x): cars seen on them each way
                yield return R74Place(RaceRoute.Surface(watchAt.x, watchAt.z), 180f);
                var south = new HashSet<TrafficDriver>(); var north = new HashSet<TrafficDriver>(); float t = 0f; Time.timeScale = 2f;
                while (t < 60f)
                {
                    yield return new WaitForSecondsRealtime(.25f); t += .5f;
                    if (!Traffic.Instance) break;
                    foreach (var d in Traffic.Instance.Cars)
                    {
                        if (!d || !d.gameObject.activeSelf || d.Edge < 0 || net.Name(d.Edge) != ExpressLanes.Name) continue;
                        var v = d.Car.Body.linearVelocity; if (v.magnitude < 2f) continue;
                        if (Vector2.Dot(new Vector2(v.x, v.z), ExpressLanes.South) > 0f) south.Add(d); else north.Add(d);
                    }
                }
                Time.timeScale = 1f;
                report.AppendLine($"    traffic on the lanes in 60 s: {south.Count} cars southbound, {north.Count} northbound");
                bool ok = lanes.Now == ExpressLanes.State.Southbound ? north.Count == 0 : lanes.Now == ExpressLanes.State.Northbound ? south.Count == 0 : true;
                Check($"express {label}: traffic on the lanes only the way they run", ok, $"{south.Count} south, {north.Count} north");
                Check($"express {label}: every gate as the schedule says", lanes.gates.All(g => g.IsOpen == lanes.GateOpen(g)));
            }
            // a closed gate smashed at 5 PM (the lanes northbound): one that is used southbound only (shut now), driven through and on down the lanes the wrong way
            ExpressLanes.ForceHour = 17f; ExpressLanes.ApplyNow(); yield return Clock(17f);
            var shut = lanes.gates.FirstOrDefault(g => !g.IsOpen);
            if (!shut) { Check("express: a shut gate at 5 PM", false); }
            else
            {
                var gp = shut.transform.position; var o = shut.transform.forward; var a = shut.transform.right;
                float W = 0f; var trig = shut.arm ? shut.arm.GetComponent<BoxCollider>() : null; if (trig) W = trig.size.x;
                var mid = gp + a * (W * .5f);
                var line = new List<Vector3>(); for (float s = 70f; s >= -40f; s -= 4f) { var q = mid + o * s; line.Add(RaceRoute.Surface(q.x, q.z, gp.y)); }
                // and on along the lanes from where the ramp meets them, southbound (against the traffic)
                var j = mid - o * 40f; int best = -1; float bd = 1e9f; int bi = 0;
                for (int e = 0; e < net.EdgeCount; e++) if (net.Name(e) == ExpressLanes.Name) for (int i = 0; i < net.PointCount(e); i++) { float d = (net.Point(e, i) - new Vector2(j.x, j.z)).sqrMagnitude; if (d < bd) { bd = d; best = e; bi = i; } }
                if (best >= 0)
                {
                    int n = net.PointCount(best); bool fwdSouth = Vector2.Dot(net.Point(best, Mathf.Min(n - 1, bi + 1)) - net.Point(best, Mathf.Max(0, bi - 1)), ExpressLanes.South) > 0f;
                    for (int i = bi; fwdSouth ? i < n : i >= 0; i += fwdSouth ? 1 : -1) { var q = net.Point(best, i); line.Add(RaceRoute.Surface(q.x, q.y, gp.y)); }
                }
                var f0 = Flat(line[2] - line[0]).normalized; yield return R74Place(line[0], Mathf.Atan2(f0.x, f0.z) * Mathf.Rad2Deg); R73Pad();
                var pics = new List<(string, float)> { ("80_E_smash_before", 40f), ("80_E_smash_after", 90f), ("80_E_wrong_way", 200f) };
                int oncoming = 0; var seen = new HashSet<TrafficDriver>();
                var watch = StartCoroutine(R73Watch(() =>
                {
                    if (!Traffic.Instance) return; var p = car.Body.position; var fw = car.transform.forward;
                    foreach (var d in Traffic.Instance.Cars) if (d && d.gameObject.activeSelf && !seen.Contains(d) && (d.Car.Body.position - p).sqrMagnitude < 15f * 15f && Vector3.Dot(d.Car.Body.linearVelocity, fw) < -3f) { seen.Add(d); oncoming++; }
                }, () => true));
                var run = new HumanRun(); yield return HumanDrive(line, 22f, 60f, run, null, null, pics); StopCoroutine(watch);
                report.AppendLine($"  5 PM: gate {Array.IndexOf(lanes.gates, shut) + 1} (shut) driven through: {(shut.Broken ? "BROKEN" : "not broken")}; then {Cum(line)[line.Count - 1]:0} m on the lanes against their way: {oncoming} cars came at the player within 15 m; pictures 80_E_smash_before / _after / 80_E_wrong_way");
                Check("express: a shut gate breaks when driven through", shut.Broken);
            }
            ExpressLanes.ForceHour = float.NaN;
        }

        // ---------------------------------------------------------------- traffic: counts and the frame rate
        static readonly (string name, Vector3 at, float yaw)[] R80Spots =
        {
            ("I-75 at Barrett Parkway", new Vector3(-77f, 0f, -2200f), 300f),
            ("I-575 (Phillip Landrum)", new Vector3(260f, 0f, 1400f), 169f),
            ("Marietta Square", new Vector3(1765.26f, 0f, -8416.87f), 89.6f),
            ("Town Center at Cobb", new Vector3(265.11f, 0f, -1207.29f), 165.25f),
        };
        IEnumerator R80Traffic()
        {
            string mix = Traffic.OldMix ? "BEFORE (Round 79's numbers and mix)" : "after";
            report.AppendLine(); report.AppendLine($"=== F. Traffic, {mix}, {Screen.width} x {Screen.height} {(Screen.fullScreen ? "full screen" : "windowed")}: cars counted on I-75 and I-575 within 400 m, the frame rate parked (10 s, uncapped)");
            if (race.State != RaceController.Stage.Free) race.EndRace();
            yield return Vehicle("original"); yield return Clock(13f);
            var net = TrafficNetwork.Instance;
            int vs = QualitySettings.vSyncCount, cap = Application.targetFrameRate; QualitySettings.vSyncCount = 0; Application.targetFrameRate = -1;
            foreach (int level in new[] { 2, 3 })
            {
                Traffic.ForcedLevel = level;
                foreach (var (name, at, yaw) in R80Spots)
                {
                    respawn.PlaceAt(RaceRoute.Surface(at.x, at.z) + Vector3.up * .4f, yaw); race.NotifyTeleported();
                    yield return new WaitForSecondsRealtime(20f);   // the pool fills (3 cars every 0.4 s, out of sight)
                    var counts = new List<int>(); var all = new List<int>();
                    for (int k = 0; k < 20; k++)
                    {
                        int c = 0, a = 0; var p = car.Body.position;
                        if (Traffic.Instance) foreach (var d in Traffic.Instance.Cars)
                            {
                                if (!d || !d.gameObject.activeSelf) continue; a++;
                                if ((d.Car.Body.position - p).sqrMagnitude < 400f * 400f && d.Edge >= 0 && net.ClassName(d.Edge) == "motorway") c++;
                            }
                        counts.Add(c); all.Add(a); yield return new WaitForSecondsRealtime(1f);
                    }
                    var frames = new List<float>(); float t0 = Time.realtimeSinceStartup;
                    while (Time.realtimeSinceStartup - t0 < 10f) { yield return null; frames.Add(Time.unscaledDeltaTime); }
                    var sorted = frames.OrderBy(x => x).ToList(); float p99 = sorted[Mathf.Min(sorted.Count - 1, (int)(sorted.Count * .99f))];
                    report.AppendLine($"  {Traffic.LevelNames[level],-6} {name,-26}: cars on the motorways within 400 m {counts.Average():0.0} (up to {counts.Max()}), cars out in all {all.Average():0}; " +
                                      $"{frames.Count / frames.Sum():0} fps average, slowest 1 % {1f / p99:0} fps");
                    if (level == 3) yield return Capture($"80_F_{(Traffic.OldMix ? "before" : "after")}_{Screen.width}_{new string(name.Where(char.IsLetterOrDigit).Take(18).ToArray())}");
                    Flush();
                }
            }
            Traffic.ForcedLevel = -1; QualitySettings.vSyncCount = vs; Application.targetFrameRate = cap;
        }
    }
}
