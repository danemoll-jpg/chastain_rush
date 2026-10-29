using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Racer;
using UnityEngine;

namespace ChastainRush
{
    // Round 81 in the exe (-exeRound81 <dir> [-r81Only dan,fshort,dtrain,ecross,bgates,cqueue,junction,ddecks,hwalls,dtrace,fps] [-traffic Heavy] -r20Minutes 300
    // -logFile <dir>\Player.log), started from the repo root (it reads local/round81/r81_lines.json: python Tools/mapgen/r81_lines.py, and
    // local/round80/r80_express.json: python Tools/mapgen/r80_express_lines.py): CHECK IT AS DAN PLAYS IT (TODO.md item 0).
    //   dan       Dan's ten reports of session 2026-10-29_01-57-15-107_610675 from his exact place and heading (BUG-001 in the square race at Whitlock)
    //   fshort    F: the Chastain Glen forward race, the human-like driver from Dan's BUG-003 place into the Rooftop leap three ways: every launch traced (what
    //             was under the car, what the body hit), the solid things within 4 m of the shortcut's line
    //   dtrain    D: hit by the train at Whitlock and at Mill Street in the square race, and at Whitlock in Free Roam: where the car is put back
    //   ecross    E: the Whitlock crossing's post driven through: pictures of what is left, the arm
    //   bgates    B: Dan's BUG-005 / 007 / 008 places (pictures), every gate from a driver's place on its ramp at 3 PM
    //   cqueue    C: Dan's BUG-006 place at 5 PM and 8 AM (needs -traffic Heavy): cars standing on the lanes within 400 m
    //   junction  A: every road through the I-75 / I-575 junction (the Express lanes both ways) by the human-like driver at up to 35 m/s: air, landings, resets
    //   ddecks    H: Round 80's every-Express-deck drive (both ways, up to 33 m/s)
    //   hwalls    H: the walls and flyovers seen from the lanes and from the road below
    //   fps       I: the frame rate (10 s, uncapped) at Marietta Square in a race, I-75 with traffic, Town Center and the Kennesaw summit, at the window's size
    public sealed partial class Round20Runner
    {
        [Serializable] class R81Line { public string name; public bool both; public float[] line; }
        [Serializable] class R81File { public R81Line[] lines; }
        R81File r81lines;
        R81File R81Lines() { if (r81lines == null) { var p = Path.Combine(Environment.CurrentDirectory, "local/round81/r81_lines.json"); if (File.Exists(p)) r81lines = JsonUtility.FromJson<R81File>(File.ReadAllText(p)); } return r81lines; }

        static readonly (string id, Vector3 at, float yaw, string what)[] R81Spots =
        {
            ("BUG-002", new Vector3(1632.23f, 0f, -8518.48f), 140.6f, "Whitlock Avenue's crossing (lap 3 of the square race): the crossing's gear broken"),
            ("BUG-003", new Vector3(2853.66f, 0f, 1131.75f), 248.6f, "Chastain Glen Lane, the Rooftop leap's turn-off"),
            ("BUG-004", new Vector3(1197.69f, 0f, 1669.63f), 348.8f, "beside Bells Ferry Road: the buried house"),
            ("BUG-005", new Vector3(740.59f, 0f, -1137.73f), 168.2f, "the I-575 Express lane's north ramp: a gate"),
            ("BUG-006", new Vector3(744.82f, 0f, -1721.01f), 4.4f, "I-575 by Chastain Road: cars standing on the lane, lumps in the median"),
            ("BUG-007", new Vector3(643.80f, 0f, -2445.95f), 168.9f, "the I-575 Express lane: a gate that should not be there"),
            ("BUG-008", new Vector3(721.83f, 0f, -1987.53f), 196.1f, "where the ramp leaves I-575's general lanes: the gate should be here"),
            ("BUG-009", new Vector3(508.16f, 0f, -3172.83f), 9.7f, "the I-575 Express flyover's north end: a step"),
            ("BUG-010", new Vector3(497.00f, 0f, -3143.00f), 190f, "I-575 southbound's bridge over I-75, its north end"),
        };

        // ---------------------------------------------------------------- dan
        IEnumerator R81Dan()
        {
            report.AppendLine(); report.AppendLine("=== Dan's reports (session 2026-10-29_01-57-15-107_610675), from his exact place and heading (the chase camera, as he saw it)");
            if (race.State != RaceController.Stage.Free) race.EndRace();
            yield return Vehicle("drifter");
            foreach (var sp in R81Spots)
            {
                yield return Clock(sp.id == "BUG-002" ? 6.5f : sp.id == "BUG-003" ? 19.5f : 15f);
                yield return R74Place(sp.at, sp.yaw);
                if (sp.id == "BUG-010") { var p0 = RaceRoute.Surface(sp.at.x, sp.at.z); var f = Quaternion.Euler(0, sp.yaw, 0) * Vector3.forward; yield return Still("81_BUG-010_after_ahead", p0 - f * 6f + Vector3.up * 2.4f, p0 + f * 30f, 60f); }
                yield return Capture($"81_{sp.id}_after");
                var p = car.Body.position;
                report.AppendLine($"  {sp.id} ({sp.what}): at ({p.x:0.0}, {p.y:0.0}, {p.z:0.0}) heading {sp.yaw:0}; picture 81_{sp.id}_after.png");
                Flush();
            }
            yield return Clock(13f);
        }

        // ---------------------------------------------------------------- fshort (F)
        IEnumerator R81FShort()
        {
            report.AppendLine(); report.AppendLine("=== F. The Rooftop leap from Dan's BUG-003 place (the Chastain Glen race forward, no AI, the human-like driver, Drifter Twin): every launch and stop traced");
            var L = R81Lines(); if (L == null) { Check("fshort: local/round81/r81_lines.json (python Tools/mapgen/r81_lines.py)", false); yield break; }
            yield return Clock(19.5f);
            foreach (var d in L.lines.Where(x => x.name.StartsWith("Rooftop leap")))
            {
                UseRoute("chastain_glen_loop.json", "forward"); yield return Vehicle("drifter");
                RaceController.LapsOverride = 3; RaceController.AiCount = 0; race.StartRace(); R73Pad();
                while (race.State == RaceController.Stage.Countdown) yield return null;
                var line = R73Line(d.line); var f0 = Flat(line[1] - line[0]).normalized;
                yield return R74Place(line[0], Mathf.Atan2(f0.x, f0.z) * Mathf.Rad2Deg);
                var tracer = car.GetComponent<ContactTracer>() ?? car.gameObject.AddComponent<ContactTracer>(); tracer.Take();
                var events = new List<string>(); float lastVy = 0f, airT = 0f; int broken0 = FenceChunk.TotalBroken(); bool wasAir = false; var trace = new StringBuilder("t\tx\ty\tz\tspeed\tvy\twheels\tcontacts\n"); float tt = 0f;
                var watch = StartCoroutine(R73Watch(() =>
                {
                    tt += Time.fixedDeltaTime; var p = car.Body.position; var v = car.Body.linearVelocity; string c = tracer.Take();
                    trace.AppendLine($"{tt:0.00}\t{p.x:0.00}\t{p.y:0.00}\t{p.z:0.00}\t{v.magnitude:0.0}\t{v.y:0.00}\t{car.GroundedWheels}\t{c}");
                    bool air = car.GroundedWheels == 0;
                    if (air && !wasAir) { airT = 0f; }
                    if (air) airT += Time.fixedDeltaTime;
                    if (!air && wasAir && airT > .25f) events.Add($"in the air {airT:0.00} s, landed at ({p.x:0.0}, {p.z:0.0})");
                    if (!air && v.y - lastVy > 2f) { string under = Physics.Raycast(p + Vector3.up, Vector3.down, out var h, 4f, ~0, QueryTriggerInteraction.Ignore) ? h.collider.name : "nothing"; events.Add($"kicked up {v.y - lastVy:0.0} m/s at ({p.x:0.0}, {p.z:0.0}) over {under}; body contacts: {(c.Length > 0 ? c.Substring(0, Mathf.Min(160, c.Length)) : "none")}"); }
                    wasAir = air; lastVy = v.y;
                }, () => true));
                string ft = $"81_F_{new string(d.name.Skip(13).Where(char.IsLetterOrDigit).ToArray())}"; var run = new HumanRun(); var pics = new List<(string, float)> { ($"{ft}_lined_up", 28f), ($"{ft}_kicker", 50f), ($"{ft}_landed", 105f), ($"{ft}_yard", 125f), ($"{ft}_yard2", 145f) };
                yield return HumanDrive(line, 26f, 60f, run, null, null, pics, $"{ft}_air"); StopCoroutine(watch);
                File.WriteAllText(Path.Combine(outputDir, $"{ft}_trace.tsv"), trace.ToString());
                report.AppendLine($"  {d.name}: {(run.reachedEnd ? "through" : "NOT through")} in {run.time:0.0} s, top {run.topSpeed:0} m/s, longest in the air {run.maxAir:0.00} s, resets {run.resets}, fences broken {FenceChunk.TotalBroken() - broken0}{run.note}");
                foreach (var e in events.Take(12)) report.AppendLine($"    {e}");
                Check($"fshort {d.name}: through, no reset", run.reachedEnd && run.resets == 0, run.note);
                race.EndRace(); yield return new WaitForSecondsRealtime(.5f); Flush();
            }
            // the solid things within 4 m of the shortcut's line (fences that do not break among them)
            UseRoute("chastain_glen_loop.json", "forward"); RaceController.AiCount = 0; race.StartRace(); yield return new WaitForSecondsRealtime(1.2f); Physics.SyncTransforms();
            var sc = race.Route != null ? race.Route.Shortcuts.FirstOrDefault(s => s.Id == "sc6_over_house") : null;
            if (sc != null)
            {
                var solid = new Dictionary<string, Vector3>();
                for (int i = 0; i + 1 < sc.Path.Length; i++)
                {
                    var a = sc.Path[i]; var b = sc.Path[i + 1]; var f = Flat(b - a); if (f.sqrMagnitude < .01f) continue;
                    foreach (var c in Physics.OverlapBox((a + b) * .5f + Vector3.up * 1.0f, new Vector3(4f, .7f, f.magnitude * .5f), Quaternion.LookRotation(f), ~0, QueryTriggerInteraction.Ignore))
                    {
                        if (c is TerrainCollider || c.attachedRigidbody == car.Body || c.attachedRigidbody != null) continue;
                        string n = (c.transform.parent ? c.transform.parent.name + "/" : "") + c.name; if (!solid.ContainsKey(n)) solid[n] = c.ClosestPoint(a);
                    }
                }
                report.AppendLine($"  solid things within 4 m of the Rooftop leap's line (0.3 to 1.7 m up): {(solid.Count == 0 ? "none" : string.Join("; ", solid.Select(kv => $"{kv.Key} at ({kv.Value.x:0}, {kv.Value.z:0})")))}");
            }
            race.EndRace();
        }

        // ---------------------------------------------------------------- dtrain (D)
        IEnumerator R81DTrain()
        {
            report.AppendLine(); report.AppendLine("=== D. Hit by a train: the car parked on the crossing as a train comes (the square race, no AI; then Free Roam): where it is put back");
            yield return Clock(13f); yield return Vehicle("original");
            var start = respawn.StartPosition;
            foreach (var (label, near, inRace) in new[] { ("Whitlock, the square race", new Vector3(1624.6f, 0f, -8521.3f), true), ("Mill Street, the square race", new Vector3(1505.5f, 0f, -8440.4f), true), ("Whitlock, Free Roam", new Vector3(1624.6f, 0f, -8521.3f), false) })
            {
                if (inRace) { UseRoute("marietta_square.json", "forward"); RaceController.LapsOverride = 1; RaceController.AiCount = 0; race.StartRace(); R73Pad(); while (race.State == RaceController.Stage.Countdown) yield return null; }
                else if (race.State != RaceController.Stage.Free) race.EndRace();
                var cross = R80Crossing(near); if (!cross) { Check($"dtrain {label}: a crossing", false); continue; }
                var yaw = Mathf.Atan2(cross.roadDir.x, cross.roadDir.z) * Mathf.Rad2Deg;
                yield return R74Place(cross.center, yaw); R73Set(0, 0, 0);
                RailTrains.Instance.Align(cross.center, 2.5f, true);
                Vector3 before = car.Body.position, after = before; float t = 0f; bool moved = false;
                while (t < 25f) { yield return new WaitForFixedUpdate(); t += Time.fixedDeltaTime; var p = car.Body.position; if ((p - before).magnitude > 12f && !moved) { moved = true; after = p; yield return new WaitForSecondsRealtime(1.5f); after = car.Body.position; break; } }
                float toCross = Flat(after - cross.center).magnitude, toStart = Flat(after - start).magnitude, toLine = float.NaN;
                if (inRace && race.Route != null) { float s = race.Route.LapStationOf(after); toLine = Flat(race.Route.PointAt(race.Route.GateS + s, out _) - after).magnitude; }
                yield return Capture($"81_D_{new string(label.Where(char.IsLetter).Take(16).ToArray())}");
                report.AppendLine($"  {label}: {(moved ? $"put back at ({after.x:0}, {after.z:0}), {toCross:0} m from the crossing, {toStart:0} m from the map's spawn{(inRace ? $", {toLine:0.0} m off the race's line" : "")}" : "NOT put back (no hit?)")}");
                Check($"dtrain {label}: put back near the crossing, never at the map's spawn", moved && toCross < 150f && toStart > 500f, $"{toCross:0} m from the crossing");
                if (inRace) race.EndRace(); yield return new WaitForSecondsRealtime(.5f); Flush();
            }
        }

        // ---------------------------------------------------------------- ecross (E)
        IEnumerator R81ECross()
        {
            report.AppendLine(); report.AppendLine("=== E. The Whitlock crossing's post driven through at about 20 m/s (Free Roam, the Street Classic): what is left, the arm");
            if (race.State != RaceController.Stage.Free) race.EndRace();
            yield return Clock(13f); yield return Vehicle("original");
            var cross = R80Crossing(new Vector3(1624.6f, 0f, -8521.3f)); if (!cross || cross.arms.Length == 0) { Check("ecross: the crossing", false); yield break; }
            var hinge = cross.arms[0].parent.position;
            var along = Flat(cross.center - hinge); along = Vector3.Project(along, cross.roadDir).normalized;
            var line = new List<Vector3>(); for (float s = -60f; s <= 25f; s += 3f) line.Add(RaceRoute.Ground(hinge.x + along.x * s, hinge.z + along.z * s));
            yield return R74Place(line[0], Mathf.Atan2(along.x, along.z) * Mathf.Rad2Deg); R73Pad();
            int b0 = FenceChunk.TotalBroken();
            var run = new HumanRun(); yield return HumanDrive(line, 20f, 20f, run, () => FenceChunk.TotalBroken() > b0);
            for (int k = 0; k < 3; k++) { yield return new WaitForSecondsRealtime(k == 0 ? .4f : 1.2f); yield return Still($"81_E_after_{k}", hinge - along * 12f + Vector3.Cross(Vector3.up, along) * 6f + Vector3.up * 3f, hinge + Vector3.up * 1f, 60f); }
            var arm = cross.arms[0]; bool standing = Vector3.Dot(arm.right, Vector3.up) > .5f; float armY = arm.position.y - RaceRoute.Ground(arm.position.x, arm.position.z).y;
            report.AppendLine($"  {FenceChunk.TotalBroken() - b0} panel(s) broken; the arm: its length along {(standing ? "UP (standing)" : "the ground")}, its hinge {armY:0.00} m over the ground; pictures 81_E_after_0..2");
            Check("ecross: the post breaks and its arm lies on the verge (not standing)", FenceChunk.TotalBroken() > b0 && !standing && armY < .6f);
            yield return new WaitForSecondsRealtime(4f); yield return Still("81_E_after_rest", hinge - along * 12f + Vector3.Cross(Vector3.up, along) * 6f + Vector3.up * 3f, hinge + Vector3.up * 1f, 60f);
        }

        // ---------------------------------------------------------------- bgates (B)
        IEnumerator R81BGates()
        {
            report.AppendLine(); report.AppendLine("=== B. The Express Lanes' gates at 3 PM (northbound): Dan's three places, then every gate from a driver's place 25 m out on its ramp");
            if (race.State != RaceController.Stage.Free) race.EndRace();
            yield return Vehicle("drifter"); ExpressLanes.ForceHour = 15f; ExpressLanes.ApplyNow(); yield return Clock(15f);
            var lanes = ExpressLanes.Instance; if (!lanes) { Check("bgates: the lanes", false); yield break; }
            foreach (var sp in R81Spots.Where(s => s.id == "BUG-005" || s.id == "BUG-007" || s.id == "BUG-008"))
            {
                yield return R74Place(sp.at, sp.yaw); yield return Capture($"81_B_{sp.id}_after");
                var g = lanes.gates.OrderBy(x => Flat(x.transform.position - sp.at).magnitude).FirstOrDefault(); float dg = g ? Flat(g.transform.position - sp.at).magnitude : -1f;
                report.AppendLine($"  {sp.id} ({sp.what}): the nearest gate {dg:0} m away; picture 81_B_{sp.id}_after.png");
            }
            for (int i = 0; i < lanes.gates.Length; i++)
            {
                var g = lanes.gates[i]; var at = g.transform.position; var o = g.transform.forward; var a = g.transform.right;
                report.AppendLine($"  gate {i + 1} at ({at.x:0}, {at.z:0}): used {(g.usableSouth && g.usableNorth ? "both ways" : g.usableSouth ? "southbound" : "northbound")}, {(g.IsOpen ? "open" : "shut")} now");
                yield return Still($"81_B_gate{i + 1}", at + o * 25f + a * 3f + Vector3.up * 1.6f, at + a * 3f + Vector3.up * 1f, 55f);
            }
            ExpressLanes.ForceHour = float.NaN; Flush();
        }

        // ---------------------------------------------------------------- cqueue (C)
        IEnumerator R81CQueue()
        {
            report.AppendLine($"=== C. Dan's BUG-006 place (I-575 by Chastain Road), traffic {(Traffic.Instance ? Traffic.LevelNames[Traffic.Instance.Level] : "none")}: the lanes' cars within 400 m over 2 minutes (game time)");
            if (race.State != RaceController.Stage.Free) race.EndRace();
            var net = TrafficNetwork.Instance; if (!net || !Traffic.Instance) { Check("cqueue: traffic (-traffic Heavy)", false); yield break; }
            yield return Vehicle("drifter");
            var sp = R81Spots.First(s => s.id == "BUG-006");
            foreach (var hour in new[] { 17f, 8f, 12.5f })
            {
                ExpressLanes.ForceHour = hour; ExpressLanes.ApplyNow(); yield return Clock(hour);
                yield return R74Place(sp.at, sp.yaw);
                var standing = new Dictionary<TrafficDriver, float>(); int seen = 0, moving = 0; float t = 0f; Time.timeScale = 2f;
                var seenSet = new HashSet<TrafficDriver>();
                while (t < 120f)
                {
                    yield return new WaitForSecondsRealtime(.25f); t += .5f;
                    foreach (var d in Traffic.Instance.Cars)
                    {
                        if (!d || !d.gameObject.activeSelf || d.Edge < 0 || net.SafeName(d.Edge) != ExpressLanes.Name) continue;
                        if (Flat(d.Car.Body.position - car.Body.position).magnitude > 400f) continue;
                        if (seenSet.Add(d)) seen++;
                        if (d.Car.Body.linearVelocity.magnitude < 1f) standing[d] = (standing.TryGetValue(d, out var s0) ? s0 : 0f) + .5f; else { standing[d] = 0f; moving++; }
                    }
                }
                Time.timeScale = 1f;
                int stuck = standing.Count(kv => kv.Value > 30f);
                yield return Capture($"81_C_{hour:00}h");
                report.AppendLine($"  {hour:0.0} h ({ExpressLanes.Describe(ExpressLanes.StateAt(hour))}): {seen} cars on the lanes within 400 m, {stuck} standing over 30 s at the end; picture 81_C_{hour:00}h.png");
                Check($"cqueue {hour:0.0} h: no car standing on the lanes over 30 s", stuck == 0, $"{stuck} standing");
                Flush();
            }
            ExpressLanes.ForceHour = float.NaN;
        }

        // ---------------------------------------------------------------- junction (A)
        IEnumerator R81Junction()
        {
            report.AppendLine(); report.AppendLine("=== A. Every road through the I-75 / I-575 junction, the human-like driver at up to 35 m/s (Street Classic, no traffic); the Express lanes both ways");
            var L = R81Lines(); if (L == null) { Check("junction: local/round81/r81_lines.json", false); yield break; }
            if (race.State != RaceController.Stage.Free) race.EndRace();
            yield return Vehicle("original"); ExpressLanes.ForceHour = 15f; ExpressLanes.ApplyNow(); yield return Clock(15f);
            int bad = 0;
            foreach (var d in L.lines.Where(x => !x.name.StartsWith("Rooftop")))
                foreach (bool back in d.both ? new[] { false, true } : new[] { false })
                {
                    var line = R73Line(d.line); if (back) line.Reverse();
                    var f0 = Flat(line[2] - line[0]).normalized; yield return R74Place(line[0], Mathf.Atan2(f0.x, f0.z) * Mathf.Rad2Deg); R73Pad();
                    float jolt = 0f, lastVy = 0f; Vector3 joltAt = Vector3.zero;
                    var watch = StartCoroutine(R73Watch(() => { float vy = car.Body.linearVelocity.y; if (car.GroundedWheels > 0 && Mathf.Abs(vy - lastVy) > jolt) { jolt = Mathf.Abs(vy - lastVy); joltAt = car.Body.position; } lastVy = vy; }, () => true));
                    string tag = $"81_A_{new string(d.name.Where(char.IsLetterOrDigit).Take(20).ToArray())}{(back ? "_back" : "")}";
                    var run = new HumanRun(); yield return HumanDrive(line, 35f, 120f, run, null, null, new List<(string, float)> { (tag, Cum(line).Last() * .5f) }); StopCoroutine(watch);
                    bool ok = run.reachedEnd && run.maxAir < .3f && jolt < 6f && run.resets == 0; if (!ok) bad++;
                    report.AppendLine($"  {d.name}{(back ? " (the other way)" : "")}: {(run.reachedEnd ? "through" : "NOT through")} in {run.time:0.0} s, top {run.topSpeed:0} m/s, longest in the air {run.maxAir:0.00} s, hardest landing {jolt:0.0} m/s (at ({joltAt.x:0}, {joltAt.z:0})), resets {run.resets}{run.note}{(ok ? "" : "  <- LOOK")}; picture {tag}.png");
                    Flush();
                }
            Check("junction: every road through it at highway speed, under 0.3 s in the air, no landing over 6 m/s, no reset", bad == 0, $"{bad} not");
            ExpressLanes.ForceHour = float.NaN;
        }

        // ---------------------------------------------------------------- dtrace: the Express decks that stuck, traced (what the body touched, a picture when it stops)
        IEnumerator R81DTrace()
        {
            report.AppendLine(); report.AppendLine("=== H. The Express decks that stopped a car, driven the other way at up to 33 m/s, every step traced");
            var path = Path.Combine(Environment.CurrentDirectory, "local/round80/r80_express.json"); if (!File.Exists(path)) yield break;
            var f = JsonUtility.FromJson<R80File>(File.ReadAllText(path));
            if (race.State != RaceController.Stage.Free) race.EndRace();
            yield return Vehicle("original"); yield return Clock(13f);
            var lines = R81Lines();
            var runs = f.decks.Where(d => d.name.Contains("(620, -455)") || d.name.Contains("(-1414, 1617)") || d.name.Contains("(-355, -856)") || d.name.Contains("(-54, -2128)") || d.name.Contains("(2049, -4662)"))
                .Select(d => (d.name + " the other way", R73Line(d.line).AsEnumerable().Reverse().ToList())).ToList();
            if (lines != null) runs.AddRange(lines.lines.Where(x => x.name.StartsWith("I-75 Express")).Select(x => (x.name, R73Line(x.line))));
            foreach (var (name, line) in runs)
            {
                var f0 = Flat(line[2] - line[0]).normalized; yield return R74Place(line[0], Mathf.Atan2(f0.x, f0.z) * Mathf.Rad2Deg); R73Pad();
                var tracer = car.GetComponent<ContactTracer>() ?? car.gameObject.AddComponent<ContactTracer>(); tracer.Take();
                var trace = new StringBuilder("t\tx\ty\tz\tspeed\tvy\twheels\tcontacts\n"); float tt = 0f, slow = 0f; int shots = 0; var notes = new List<string>(); bool shotDue = false;
                var watch = StartCoroutine(R73Watch(() =>
                {
                    tt += Time.fixedDeltaTime; var p = car.Body.position; var v = car.Body.linearVelocity; string c = tracer.Take();
                    trace.AppendLine($"{tt:0.00}\t{p.x:0.00}\t{p.y:0.00}\t{p.z:0.00}\t{v.magnitude:0.0}\t{v.y:0.00}\t{car.GroundedWheels}\t{c}");
                    if (v.magnitude < 2f && tt > 3f) slow += Time.fixedDeltaTime; else slow = 0f;
                    if (slow > 1f && shots < 2 && !shotDue) { shotDue = true; notes.Add($"stopped at ({p.x:0.0}, {p.y:0.0}, {p.z:0.0}): {(c.Length > 0 ? c.Substring(0, Mathf.Min(220, c.Length)) : "no body contact")}"); }
                }, () => true));
                var run = new HumanRun(); string tag = $"81_T_{new string(name.Where(char.IsLetterOrDigit).Take(24).ToArray())}";
                var co = StartCoroutine(HumanDrive(line, 33f, 40f, run, null, null, null, tag + "_air"));
                float t0 = Time.time; while (Time.time - t0 < 42f && !run.reachedEnd && run.time == 0f) { if (shotDue && shots < 2) { yield return Capture($"{tag}_stop{shots}"); shots++; shotDue = false; } yield return null; }
                StopCoroutine(co); StopCoroutine(watch); R73Set(0, 0, 0);
                File.WriteAllText(Path.Combine(outputDir, tag + "_trace.tsv"), trace.ToString());
                report.AppendLine($"  {name}: {(run.reachedEnd ? "through" : "NOT through")}, longest in the air {run.maxAir:0.00} s, resets {run.resets}{run.note}");
                foreach (var n in notes) report.AppendLine("    " + n);
                Flush();
            }
        }

        // ---------------------------------------------------------------- walls (H)
        IEnumerator R81Walls()
        {
            report.AppendLine(); report.AppendLine("=== H. The Express Lanes' walls and flyovers: pictures from the lanes and from the roads below");
            if (race.State != RaceController.Stage.Free) race.EndRace();
            yield return Vehicle("original"); yield return Clock(15f);
            var views = new (string name, Vector3 at, Vector3 look)[]
            {
                ("81_H_I575_lanes_walls", new Vector3(752f, 0f, -1560f), new Vector3(749f, 0f, -1620f)),
                ("81_H_I75_lanes_walls", new Vector3(330f, 0f, -3080f), new Vector3(380f, 0f, -3170f)),
                ("81_H_junction_from_I75", new Vector3(560f, 0f, -3300f), new Vector3(490f, 0f, -3260f)),
                ("81_H_junction_from_north", new Vector3(520f, 0f, -3060f), new Vector3(505f, 0f, -3200f)),
                ("81_H_barrett_deck", new Vector3(30f, 0f, -2330f), new Vector3(70f, 0f, -2520f)),
                ("81_H_EL13_connector", new Vector3(2300f, 0f, -4880f), new Vector3(2380f, 0f, -4930f)),
            };
            foreach (var v in views)
            {
                var a = RaceRoute.Ground(v.at.x, v.at.z) + Vector3.up * 2.2f; var b = RaceRoute.Ground(v.look.x, v.look.z) + Vector3.up * 2f;
                yield return Still(v.name, a, b, 60f);
            }
            int walls = FindObjectsByType<MeshCollider>(FindObjectsSortMode.None).Count(c => c.name.StartsWith("Express Lanes wall"));
            report.AppendLine($"  {walls} wall pieces in the scene; pictures {string.Join(", ", views.Select(v => v.name))}");
            Check("walls: the Express Lanes' walls are in the scene", walls > 0);
        }

        // ---------------------------------------------------------------- fps (I)
        IEnumerator R81Fps()
        {
            report.AppendLine(); report.AppendLine($"=== I. The frame rate at {Screen.width} x {Screen.height} {(Screen.fullScreen ? "full screen" : "windowed")}, traffic {(Traffic.Instance ? Traffic.LevelNames[Traffic.Instance.Level] : "none")}, " +
                                                  $"cores {SystemInfo.processorCount}, {SystemInfo.graphicsDeviceName}: 10 s uncapped at each place");
            int vs = QualitySettings.vSyncCount, cap = Application.targetFrameRate; QualitySettings.vSyncCount = 0; Application.targetFrameRate = -1;
            yield return Vehicle("original"); yield return Clock(13f);
            var spots = new (string name, Vector3 at, float yaw, bool square)[]
            {
                ("Marietta Square in a race (3 AI)", new Vector3(1765.26f, 0f, -8416.87f), 89.6f, true),
                ("I-75 at Barrett Parkway", new Vector3(-77f, 0f, -2200f), 300f, false),
                ("Town Center at Cobb", new Vector3(265.11f, 0f, -1207.29f), 165.25f, false),
                ("the Kennesaw summit", new Vector3(-933.27f, 0f, -5721.73f), 140.7f, false),
            };
            foreach (var sp in spots)
            {
                if (sp.square) { UseRoute("marietta_square.json", "forward"); RaceController.LapsOverride = 1; RaceController.AiCount = 3; race.StartRace(); R73Pad(); while (race.State == RaceController.Stage.Countdown) yield return null; }
                else if (race.State != RaceController.Stage.Free) race.EndRace();
                yield return R74Place(sp.at, sp.yaw); yield return new WaitForSecondsRealtime(sp.square ? 3f : 15f);
                var frames = new List<float>(); float t0 = Time.realtimeSinceStartup;
                while (Time.realtimeSinceStartup - t0 < 10f) { yield return null; frames.Add(Time.unscaledDeltaTime); }
                var sorted = frames.OrderBy(x => x).ToList(); float p99 = sorted[Mathf.Min(sorted.Count - 1, (int)(sorted.Count * .99f))];
                string tag = $"81_I_{Screen.width}{(SteamDeck.On ? "_deck" : "")}_{new string(sp.name.Where(char.IsLetterOrDigit).Take(14).ToArray())}";
                yield return Capture(tag);
                report.AppendLine($"  {sp.name,-34}: {frames.Count / frames.Sum():0} fps average, slowest 1 % {1f / p99:0} fps (Steam Deck setting {(SteamDeck.On ? "on" : "off")}); picture {tag}.png");
                if (sp.square) race.EndRace();
                Flush();
            }
            QualitySettings.vSyncCount = vs; Application.targetFrameRate = cap;
        }
    }
}
