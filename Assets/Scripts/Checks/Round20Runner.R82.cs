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
    // Round 82 in the exe (-exeRound82 <dir> [-r82Only dan,lines,fence] [-r82Lines <file>] -r20Minutes 300 -logFile <dir>\Player.log), started from the repo root
    // (it reads local/round82/r82_lines.json: python Tools/mapgen/r82_lines.py): CHECK IT AS DAN PLAYS IT (TODO.md item 0).
    //   dan    Dan's eight reports of session 2026-10-29_09-12-54-968_3aae2d from his exact place and heading (BUG-001 in a forward Chastain Glen race)
    //   lines  every line of the file driven by the human-like driver (both ways when it says so): the Express Lanes end to end at up to 35 m/s, the Big Shanty
    //          Express ramp onto the lanes, both roads through every crossing this round changed, the railroad at BUG-008: time in the air (over 0.3 s is a
    //          fault), the hardest landing, resets, stops, wall hits; a picture at each of its picture stations from the chase camera
    //   fence  E: from Dan's BUG-001 place and heading into the 713/1987 fence: a forward race (it breaks, the car goes through) and a reverse race (it holds)
    public sealed partial class Round20Runner
    {
        [Serializable] class R82Line { public string name; public bool both; public float cap; public float[] line; public float[] pics; public string[] picNames; }
        [Serializable] class R82File { public R82Line[] lines; }

        static readonly (string id, Vector3 at, float yaw, string what)[] R82Spots =
        {
            ("BUG-001", new Vector3(2781.91f, 289.88f, 1023.43f), 149.2f, "a forward Chastain Glen race: the board fence between two houses ahead"),
            ("BUG-002", new Vector3(-368.22f, 299.55f, -847.05f), 122.2f, "the Big Shanty Express ramp at its creek bridge's north end: spikes of ground"),
            ("BUG-003", new Vector3(-327.44f, 301.07f, -946.25f), 117.5f, "the ramp's merge into the Express Lanes: a slot under a slab"),
            ("BUG-005", new Vector3(121.42f, 358.76f, -2636.08f), 90.3f, "the Express Lanes under Greers Chapel Drive: bridges lying across the road"),
            ("BUG-006", new Vector3(1505.54f, 371.04f, -4265.14f), 53.9f, "the Express Lanes at Bells Ferry Road: a wall across, torn ground"),
            ("BUG-007", new Vector3(2582.37f, 381.55f, -5261.45f), 288.6f, "from Canton Road's bridge: the dip in the Express Lanes"),
            ("BUG-008", new Vector3(2735.95f, 386.31f, -5227.83f), 57.7f, "the railroad by Atlanta Industrial Drive: a step at the deck's end"),
        };

        // ---------------------------------------------------------------- dan
        IEnumerator R82Dan()
        {
            report.AppendLine(); report.AppendLine("=== Dan's reports (session 2026-10-29_09-12-54-968_3aae2d), from his exact place and heading (the chase camera, as he saw it)");
            yield return Vehicle("drifter");
            foreach (var sp in R82Spots)
            {
                if (sp.id == "BUG-001") { UseRoute("chastain_glen_loop.json", "forward"); RaceController.LapsOverride = 3; RaceController.AiCount = 0; race.StartRace(); R73Pad(); while (race.State == RaceController.Stage.Countdown) yield return null; }
                else if (race.State != RaceController.Stage.Free) race.EndRace();
                yield return Clock(sp.id == "BUG-001" ? 18f : 13f);
                yield return R82Place(sp.at, sp.yaw);
                yield return Capture($"82_{sp.id}_after");
                var p = car.Body.position;
                report.AppendLine($"  {sp.id} ({sp.what}): at ({p.x:0.0}, {p.y:0.0}, {p.z:0.0}) heading {sp.yaw:0}; picture 82_{sp.id}_after.png");
                Flush();
            }
            if (race.State != RaceController.Stage.Free) race.EndRace();
        }

        // Dan's vehicle position (his report's "Vehicle position"), on the surface nearest the height it was at (a deck over a road: the one he was on), heading 'yaw'
        IEnumerator R82Place(Vector3 at, float yaw)
        {
            var hits = Physics.RaycastAll(new Vector3(at.x, at.y + 40f, at.z), Vector3.down, 120f, ~0, QueryTriggerInteraction.Ignore).Where(h => h.rigidbody == null).ToList();
            if (hits.Count == 0) { yield return R74Place(at, yaw); yield break; }
            var p = hits.OrderBy(h => Mathf.Abs(h.point.y - at.y)).First().point;
            respawn.PlaceAt(p + Vector3.up * .3f, yaw); car.Body.linearVelocity = Vector3.zero; race.NotifyTeleported();
            var cam = Camera.main ? Camera.main.GetComponent<ChaseCamera>() : null; if (cam) cam.Snap();
            yield return new WaitForSecondsRealtime(2.5f);
        }

        // ---------------------------------------------------------------- lines
        IEnumerator R82LinesPart()
        {
            string file = "local/round82/r82_lines.json"; var argv = Environment.GetCommandLineArgs();
            for (int i = 0; i + 1 < argv.Length; i++) if (argv[i] == "-r82Lines") file = argv[i + 1];
            var path = Path.Combine(Environment.CurrentDirectory, file);
            report.AppendLine(); report.AppendLine($"=== The lines of {file}, the human-like driver (Street Classic, no traffic, 3 PM): in the air over 0.3 s, a landing over 6 m/s, a reset, a stop or a wall hit is a fault");
            if (!File.Exists(path)) { Check("lines: " + file + " (python Tools/mapgen/r82_lines.py)", false); yield break; }
            var f = JsonUtility.FromJson<R82File>(File.ReadAllText(path));
            if (race.State != RaceController.Stage.Free) race.EndRace();
            yield return Vehicle("original"); ExpressLanes.ForceHour = 15f; ExpressLanes.ApplyNow(); yield return Clock(15f);
            int bad = 0, runs = 0;
            foreach (var d in f.lines)
                foreach (bool back in d.both ? new[] { false, true } : new[] { false })
                {
                    var line = R73Line(d.line); if (back) line.Reverse(); if (line.Count < 3) continue;
                    var cum = Cum(line); float total = cum[cum.Length - 1];
                    var f0 = Flat(line[2] - line[0]).normalized; yield return R74Place(line[0], Mathf.Atan2(f0.x, f0.z) * Mathf.Rad2Deg); R73Pad();
                    var tracer = car.GetComponent<ContactTracer>() ?? car.gameObject.AddComponent<ContactTracer>(); tracer.Take();
                    float jolt = 0f, lastVy = 0f, slow = 0f, tt = 0f; Vector3 joltAt = Vector3.zero; int wallHits = 0; var notes = new List<string>(); bool wasAir = false; float airT = 0f;
                    var watch = StartCoroutine(R73Watch(() =>
                    {
                        tt += Time.fixedDeltaTime; var p = car.Body.position; var v = car.Body.linearVelocity; string c = tracer.Take();
                        if (car.GroundedWheels > 0 && Mathf.Abs(v.y - lastVy) > jolt) { jolt = Mathf.Abs(v.y - lastVy); joltAt = p; }
                        lastVy = v.y;
                        if (c.Contains("Express Lanes wall")) { wallHits++; if (notes.Count < 8) notes.Add($"wall hit at ({p.x:0}, {p.z:0})"); }
                        bool air = car.GroundedWheels == 0; if (air && !wasAir) airT = 0f; if (air) airT += Time.fixedDeltaTime;
                        if (!air && wasAir && airT > .3f && notes.Count < 8) notes.Add($"{airT:0.00} s in the air, landed at ({p.x:0}, {p.z:0})");
                        wasAir = air;
                        if (v.magnitude < 2f && tt > 4f) slow += Time.fixedDeltaTime; else slow = 0f;
                        if (slow > 1.5f && slow < 1.5f + Time.fixedDeltaTime * 1.5f && notes.Count < 8) notes.Add($"stopped at ({p.x:0.0}, {p.y:0.0}, {p.z:0.0}): {(c.Length > 0 ? c.Substring(0, Mathf.Min(200, c.Length)) : "no body contact")}");
                    }, () => true));
                    string tag = $"82_L_{new string(d.name.Where(char.IsLetterOrDigit).Take(26).ToArray())}{(back ? "_back" : "")}";
                    var pics = new List<(string, float)>();
                    if (d.pics != null) for (int k = 0; k < d.pics.Length; k++) { float s = back ? total - d.pics[k] : d.pics[k]; string nm = d.picNames != null && k < d.picNames.Length ? d.picNames[k] : $"{k}"; pics.Add(($"{tag}_{nm}", s)); }
                    pics = pics.OrderBy(x => x.Item2).ToList();
                    float cap = d.cap > 0f ? d.cap : 35f;
                    var run = new HumanRun(); yield return HumanDrive(line, cap, Mathf.Max(60f, total / (cap * .5f) + 30f), run, null, null, pics, tag + "_air"); StopCoroutine(watch);
                    bool ok = run.reachedEnd && run.maxAir < .3f && jolt < 6f && run.resets == 0 && wallHits == 0; if (!ok) bad++; runs++;
                    report.AppendLine($"  {d.name}{(back ? " (the other way)" : "")}, {total:0} m: {(run.reachedEnd ? "through" : "NOT through")} in {run.time:0.0} s, top {run.topSpeed:0} m/s, longest in the air {run.maxAir:0.00} s, " +
                                      $"hardest landing {jolt:0.0} m/s (at ({joltAt.x:0}, {joltAt.z:0})), resets {run.resets}, wall hits {wallHits}{run.note}{(ok ? "" : "  <- LOOK")}");
                    foreach (var n in notes) report.AppendLine("    " + n);
                    Flush();
                }
            Check($"lines: {runs} runs, each through with under 0.3 s in the air, no landing over 6 m/s, no reset, no wall hit", bad == 0, $"{bad} not");
            ExpressLanes.ForceHour = float.NaN;
        }

        // ---------------------------------------------------------------- fence (E)
        IEnumerator R82Fence()
        {
            report.AppendLine(); report.AppendLine("=== E. From Dan's BUG-001 place and heading into the 713/1987 fence at about 10 m/s (Drifter Twin): a forward race, then a reverse race");
            var sp = R82Spots.First(s => s.id == "BUG-001");
            yield return Clock(18f);
            foreach (var dir in new[] { "forward", "reverse" })
            {
                if (race.State != RaceController.Stage.Free) race.EndRace();
                yield return Vehicle("drifter");
                UseRoute("chastain_glen_loop.json", dir); RaceController.LapsOverride = 3; RaceController.AiCount = 0; race.StartRace(); R73Pad();
                while (race.State == RaceController.Stage.Countdown) yield return null;
                var fwd = Quaternion.Euler(0f, sp.yaw, 0f) * Vector3.forward;
                var line = new List<Vector3>(); for (float s = 0f; s <= 30f; s += 2f) line.Add(RaceRoute.Ground(sp.at.x + fwd.x * s, sp.at.z + fwd.z * s));
                yield return R82Place(sp.at, sp.yaw); R73Pad();
                int b0 = FenceChunk.TotalBroken(); var start = car.Body.position;
                var run = new HumanRun(); yield return HumanDrive(line, 10f, 12f, run, null, null, new List<(string, float)> { ($"82_E_{dir}_at_fence", 12f), ($"82_E_{dir}_past", 24f) }, null, false);
                float went = Vector3.Dot(Flat(car.Body.position - start), fwd); int broke = FenceChunk.TotalBroken() - b0;
                yield return Capture($"82_E_{dir}_end");
                report.AppendLine($"  {dir} race: {broke} fence piece(s) broken, the car went {went:0.0} m along Dan's heading (the fence is about 7 m ahead); pictures 82_E_{dir}_at_fence, _past, _end");
                if (dir == "forward") Check("fence forward race: it breaks and the car goes through", broke > 0 && went > 14f, $"{broke} broken, {went:0.0} m");
                else Check("fence reverse race: it holds (no unintended shortcut)", went < 9.5f, $"{broke} broken, {went:0.0} m");
                race.EndRace(); yield return new WaitForSecondsRealtime(.5f); Flush();
            }
        }
    }
}
