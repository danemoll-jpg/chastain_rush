using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Racer;
using UnityEngine;

namespace ChastainRush
{
    // Round 68 in the exe (-exeRound68 <dir> [-r68Only jfly,jai,jhood,jloop] [-r20Vehicles ids] -logFile <dir>\Player.log):
    //   jfly    the descent flat out (the real vehicle, full throttle, braking only for the bends) on the Drifter Twin, the Street Classic, the Needle 600,
    //           the Ridge Scrambler and the Trail Four, and at the AI's held speeds: each jump's lip speed, air time, distance, the most height over the
    //           ground below, the landing (speed, upright, the speed a second later); pictures mid-air on the Drifter Twin
    //   jloop   the whole lap on every vehicle at the AI's speeds (Round 67's mloop): round, never stuck
    //   jai     the race twice on Hard, 3 AI each (Round 67's mai)
    //   jhood   the neighborhood's biggest jumps flown the same way (flat out and at their AI speeds), for the comparison
    public sealed partial class Round20Runner
    {
        // the jumps' lips and headings (Data/tracks/kennesaw_mountain.json, Tools/mapgen/make_mountain_route.py)
        static readonly (string id, string name, float x, float z, float dx, float dz)[] R68Jumps =
        {
            ("J1", "the summit flight", -942.57f, -5708.43f, 0.7314f, 0.682f), ("J2", "the west face", -789.73f, -5414.67f, -0.9975f, -0.0709f),
            ("J3", "the earthworks", -1010.3f, -5326.0f, 0.7658f, 0.6431f),
        };
        // the lips from the route itself (each jump's run-up zone ends 1 m past its lip), so the check follows the generator
        string R68JumpAt(Vector3 p)
        {
            var zs = race.AiZones; if (zs == null) return null;
            foreach (var z in zs.Where(z => z.id.Length > 2 && z.id[0] == 'J' && z.id.Contains("run-up")))
            { var lip = race.Route.PointAt(z.s1 - 1f, out _); if (Flat(p - lip).magnitude < 25f) return z.id.Substring(0, 2); }
            return null;
        }
        float GroundBelow(Vector3 p)
        {
            float best = float.NaN, bd = float.MaxValue;
            foreach (var h in Physics.RaycastAll(p + Vector3.up * 0.5f, Vector3.down, 700f, ~0, QueryTriggerInteraction.Ignore))
                if (h.rigidbody != car.Body && h.distance < bd) { bd = h.distance; best = h.point.y; }
            return float.IsNaN(best) ? RaceRoute.Surface(p.x, p.z).y : best;
        }

        readonly List<string> losses = new List<string>();
        public sealed class R68Flight { public string at; public float lip, air, dist, over, land, up, after; public Vector3 from, to; }

        // flies a line and logs every spell in the air over 'minAir' (where it left, how long, how far, the most height over the ground, the landing)
        IEnumerator FlyLine(List<Vector3> line, float cap, float[] prof, List<R68Flight> flights, Run run, Func<Vector3, string> at, Func<R68Flight, float, IEnumerator> midAir = null, float minAir = .5f, float timeout = 200f, string trace = null)
        {
            Place(line[0], line[2] - line[0], 30f); yield return new WaitForFixedUpdate();
            var touch = car.gameObject.GetComponent<Touches>() ?? car.gameObject.AddComponent<Touches>(); touch.seen.Clear(); touch.on = true;
            Time.timeScale = 2f; Time.maximumDeltaTime = .5f;
            var drive = StartCoroutine(DriveLine(line, cap, timeout, run, null, null, 5f, false, prof));
            R68Flight f = null; float air = 0f; bool pic = false; var pending = new List<R68Flight>();
            var hist = new Queue<(float t, float v)>(); float lastLoss = -9f; losses.Clear();
            var tr = trace != null ? new System.Text.StringBuilder("t\tlap_s\tx\ty\tz\tspeed\twheels\tover_ground\tup\n") : null; float tNext = 0f, tt = 0f;
            while (!run.reached && run.end == "" && run.time < timeout - 1f)
            {
                yield return new WaitForFixedUpdate();
                var p = car.Body.position;
                tt += Time.fixedDeltaTime;
                if (tr != null && tt >= tNext) { tNext = tt + .1f; tr.AppendLine($"{tt:0.00}	{race.Route.LapStationOf(p):0.0}	{p.x:0.0}	{p.y:0.0}	{p.z:0.0}	{car.Body.linearVelocity.magnitude:0.0}	{car.GroundedWheels}	{p.y - GroundBelow(p):0.0}	{car.transform.up.y:0.00}"); }
                if (car.GroundedWheels == 0)
                {
                    if (f == null) { f = new R68Flight { from = p, lip = car.Body.linearVelocity.magnitude, at = at(p) }; air = 0f; pic = false; }
                    air += Time.fixedDeltaTime; f.over = Mathf.Max(f.over, p.y - GroundBelow(p));
                    if (midAir != null && !pic && f.at != null && air > 1.6f) { pic = true; Time.timeScale = 0f; yield return midAir(f, air); Time.timeScale = 2f; }
                }
                else if (f != null)
                {
                    if (air >= minAir) { f.air = air; f.dist = Flat(p - f.from).magnitude; f.land = car.Body.linearVelocity.magnitude; f.up = car.transform.up.y; f.to = p; f.after = 999f; flights.Add(f); pending.Add(f); }
                    f = null; air = 0f;
                }
                foreach (var q in pending) q.after = Mathf.Min(q.after, car.Body.linearVelocity.magnitude);
                // a sudden loss on the ground (8 m/s or more in 0.2 s): where, and what the vehicle last touched
                float vNow = car.Body.linearVelocity.magnitude; hist.Enqueue((tt, vNow)); while (hist.Count > 0 && hist.Peek().t < tt - .2f) hist.Dequeue();
                if (car.GroundedWheels > 0 && hist.Count > 0 && hist.Max(h => h.v) - vNow > 8f && tt - lastLoss > 1f)
                {
                    lastLoss = tt; var seen = touch.seen.Skip(Math.Max(0, touch.seen.Count - 3)).ToList();
                    losses.Add($"{hist.Max(h => h.v):0.0} to {vNow:0.0} m/s at ({p.x:0.0}, {p.y:0.0}, {p.z:0.0}), lap {race.Route.LapStationOf(p):0} m: {(seen.Count > 0 ? string.Join("; ", seen) : "nothing touched")}");
                }
                pending.RemoveAll(q => Flat(car.Body.position - q.to).magnitude > 45f);
            }
            yield return drive; touch.on = false; Time.timeScale = 1f;
            if (tr != null) System.IO.File.WriteAllText(System.IO.Path.Combine(outputDir, trace + ".tsv"), tr.ToString());
        }
        float[] R68Shift(float[] prof, float s0)
        {
            if (prof == null) return null; var o = new List<float>();
            for (int i = 0; i + 1 < prof.Length; i += 2) { float s = prof[i] - s0; if (s < -1f) continue; o.Add(s); o.Add(prof[i + 1]); }
            return o.ToArray();
        }
        string FlightTxt(R68Flight f) => $"off at {f.lip:0.0} m/s, {f.air:0.00} s in the air, {f.dist:0} m, up to {f.over:0} m over the ground below, landed at {f.land:0.0} m/s upright {f.up:0.00}, slowest in the next 45 m {f.after:0.0} m/s";

        IEnumerator R68Fly()
        {
            report.AppendLine(); report.AppendLine("=== The descent's jumps: the real vehicle flat out from the summit straight (full throttle, braking only for the bends), and at the AI's held speeds");
            UseRoute(R67File, "forward");
            if (race.Route == null || race.Route.Name != "Kennesaw Mountain") { Check("jfly: the route loads", false, race.Route?.Name ?? "none"); yield break; }
            float s0 = 1950f, s1 = race.Route.LapLength - 5f;
            var line = LapPart(s0, s1); var zones = R68Shift(R62Profile(), s0);
            // the bends only (the AI's zones at the hairpins, not its held run-up speeds): flat out between
            var bends = new List<float>(); var zs = race.AiZones;
            if (zs != null) foreach (var z in zs.Where(z => z.id.StartsWith("the bend")).OrderBy(z => z.s0)) { bends.AddRange(new[] { z.s0 - s0 - .5f, 99f, z.s0 - s0, z.speed, z.s1 - s0, z.speed, z.s1 - s0 + .5f, 99f }); }
            var set = new[] { ("drifter", false), ("original", false), ("moto", false), ("scrambler", false), ("atv", false), ("drifter", true), ("original", true) };
            foreach (var a in Environment.GetCommandLineArgs()) if (a == "-r68Quick") set = new[] { ("drifter", false), ("original", false) };
            foreach (var (vid, ai) in set)
            {
                if (VehicleProfile.IndexOf(vid) < 0) continue;
                yield return Vehicle(vid); FenceChunk.RestoreAll(); yield return new WaitForSecondsRealtime(.2f);
                var flights = new List<R68Flight>(); var run = new Run();
                Func<R68Flight, float, IEnumerator> pic = vid == "drifter" && !ai ? (fl, t) => R68Pic($"68_midair_{fl.at}", fl) : (Func<R68Flight, float, IEnumerator>)null;
                yield return FlyLine(line, ai ? 99f : 80f, ai ? zones : bends.ToArray(), flights, run, R68JumpAt, pic, .5f, 200f, $"68_trace_{(ai ? "ai_" : "")}{vid}");
                string who = (ai ? "AI speeds, " : "flat out, ") + Nm(vid);
                var tch = car.GetComponent<Touches>(); string last = tch != null && tch.seen.Count > 0 ? string.Join(", ", tch.seen.Skip(Math.Max(0, tch.seen.Count - 4))) : "nothing";
                report.AppendLine($"  {who}: {(run.reached ? $"down in {run.time:0.0} s" : $"NOT DOWN ({run.end}) at ({car.Body.position.x:0}, {car.Body.position.z:0}), lap {race.Route.LapStationOf(car.Body.position):0} m; last touched {last}")}; catch-slope landings {RampHold.CatchLandings}");
                foreach (var j in R68Jumps)
                {
                    var f = flights.Where(q => q.at == j.id).OrderByDescending(q => q.air).FirstOrDefault();
                    report.AppendLine($"      {j.id} {j.name,-18} {(f != null ? FlightTxt(f) : "NO FLIGHT")}");
                    bool clean = f != null && f.up > .7f && f.after > f.land * .45f;
                    Check($"jfly {who} {j.id}: flies (4 s or more) and lands clean", f != null && f.air >= 4f && clean, f != null ? $"{f.air:0.00} s, {f.dist:0} m, {f.over:0} m up, after {f.after:0} m/s" : "no flight");
                }
                foreach (var l in losses) report.AppendLine("      loss: " + l);
                foreach (var f in flights.Where(q => q.at == null)) report.AppendLine($"      other air: {f.air:0.00} s at ({f.from.x:0}, {f.from.z:0}), lap {race.Route.LapStationOf(f.from):0} m");
                Check($"jfly {who}: down the mountain, never stuck", run.reached, run.reached ? $"{run.time:0.0} s" : run.end);
            }
        }

        // the built ground along the lap's line from the summit straight down (rays down onto the colliders, every meter): against the designed profile
        IEnumerator R68Prof()
        {
            UseRoute(R67File, "forward"); if (race.Route == null) yield break;
            var sb = new System.Text.StringBuilder("lap_s\tx\tz\tground\n");
            for (float s = 1950f; s < race.Route.LapLength; s += 1f)
            {
                var q = race.Route.PointAt(s, out _);
                float y = Physics.Raycast(new Vector3(q.x, 900f, q.z), Vector3.down, out var hit, 900f, ~0, QueryTriggerInteraction.Ignore) ? hit.point.y : float.NaN;
                sb.AppendLine($"{s:0}	{q.x:0.00}	{q.z:0.00}	{y:0.000}");
            }
            System.IO.File.WriteAllText(System.IO.Path.Combine(outputDir, "68_built_profile.tsv"), sb.ToString());
            report.AppendLine("  the built ground along the line: 68_built_profile.tsv"); yield return null;
        }

        // J3's run-up alone, flat out with no speed profile, every physics step logged (what slows the Drifter Twin and the Trail Four on its kicker)
        IEnumerator R68J3Test()
        {
            report.AppendLine(); report.AppendLine("=== J3's run-up alone (flat out, no profile), every step past lap 3290 m");
            UseRoute(R67File, "forward"); var zs = race.AiZones; var z3 = zs.First(z => z.id.StartsWith("J3")); float sL = z3.s1 - 1f;
            foreach (var vid in new[] { "atv", "drifter", "original" })
            {
                yield return Vehicle(vid); var line = LapPart(sL - 150f, sL + 60f); Place(line[0], line[2] - line[0], 15f); yield return new WaitForFixedUpdate();
                var run = new Run(); var touch = car.gameObject.GetComponent<Touches>() ?? car.gameObject.AddComponent<Touches>(); touch.seen.Clear(); touch.on = true;
                var drive = StartCoroutine(DriveLine(line, 80f, 20f, run, null, null, 5f, false, null)); var sb = new System.Text.StringBuilder(); float last = 0f;
                while (!run.reached && run.end == "" && run.time < 19f)
                {
                    yield return new WaitForFixedUpdate(); var p = car.Body.position; float s = race.Route.LapStationOf(p);
                    if (s < sL - 40f || s > sL + 6f) continue;
                    var trig = Physics.OverlapSphere(p, 2.5f, ~0, QueryTriggerInteraction.Collide).Where(c => c.isTrigger && c.attachedRigidbody != car.Body).Select(c => c.name).Distinct();
                    float v = car.Body.linearVelocity.magnitude;
                    sb.AppendLine($"    {s:0.0} v {v:0.0} fwd {car.ForwardSpeed:0.0} wheels {car.GroundedWheels} water {car.WaterImmersion:0.00} up {car.transform.up.y:0.00} lift {car.SuspensionLift:0.0} {(last - v > 1f ? "LOSS " + (last - v).ToString("0.0") : "")} triggers [{string.Join(",", trig)}] touch {(touch.seen.Count > 0 ? touch.seen[touch.seen.Count - 1] : "-")}");
                    last = v; touch.seen.Clear();
                }
                StopCoroutine(drive); car.enabled = true; touch.on = false;
                report.AppendLine($"  {Nm(vid)}:"); report.Append(sb.ToString());
            }
        }

        IEnumerator R68Pic(string name, R68Flight f)
        {
            var p = car.Body.position; var d = Flat(car.Body.linearVelocity).normalized; var side = Vector3.Cross(Vector3.up, d).normalized;
            var cam = Camera.main; var chase = cam.GetComponent<ChaseCamera>(); bool was = chase && chase.enabled; if (chase) chase.enabled = false;
            float f0 = cam.fieldOfView; cam.fieldOfView = 65f;
            // from behind, high over the cleared corridor (the woods either side stand 30 m tall): the ground far below in the picture
            cam.transform.position = p - d * 38f + Vector3.up * 14f; cam.transform.LookAt(p + d * 30f - Vector3.up * 18f);
            yield return null; yield return Capture(name);
            cam.transform.position = p - d * 10f + Vector3.up * 3f; cam.transform.LookAt(p + d * 35f - Vector3.up * 16f);
            yield return null; yield return Capture(name + "_behind");
            cam.fieldOfView = f0; if (chase) { chase.enabled = was; chase.Snap(); }
            report.AppendLine($"  {name}: taken {Flat(p - f.from).magnitude:0} m out, {p.y - GroundBelow(p):0} m over the ground");
        }

        // ------------------------------------------------------------ the neighborhood's biggest jumps, for the comparison
        IEnumerator R68Hood()
        {
            report.AppendLine(); report.AppendLine("=== The neighborhood's biggest jumps, flown the same way (the Drifter Twin and the Street Classic): at their AI speeds, and flat out (60 m/s cap) where the line allows");
            var list = new (string route, string dir, string sc, string label)[]
            {
                ("chastain_ridge.json", "forward", null, "the Ridge leap (Chastain Ridge)"),
                ("chastain_lakes.json", "forward", "w_lake_jump", "the Boat jump (Chastain Lakes)"),
                ("chastain_lakes.json", "reverse", "r_kicker_jump", "the Kicker jump and carport (Chastain Lakes, reverse)"),
                ("addison_heights.json", "forward", "f_elaine_jump", "the Elaine jump (Addison Heights)"),
                ("chastain_glen_loop.json", "forward", "sc6_over_house", "the Rooftop leap (Chastain Glen)"),
                ("noonday_creek_trail.json", "forward", "t_creek", "the Creek leap (Noonday Creek Trail)"),
                ("noonday_creek_trail.json", "reverse", "t_parkway_rev", "the Parkway leap (Noonday Creek Trail, reverse)"),
                ("noonday_creek_trail.json", "forward", "t_carroway", "the Carroway renovation (Noonday Creek Trail)"),
            };
            foreach (var vid in new[] { "drifter", "original" })
            {
                yield return Vehicle(vid);
                foreach (var (route, dir, scId, label) in list)
                {
                    UseRoute(route, dir); if (race.Route == null) { report.AppendLine($"  {label}: no route"); continue; }
                    List<Vector3> line; float[] prof;
                    if (scId == null)
                    {
                        // the Ridge leap: on the lap, its lip at (3150, 620)
                        float sL = race.Route.LapStationOf(RaceRoute.Surface(3150f, 620f)); line = LapPart(sL - 260f, sL + 200f); prof = R68Shift(R62Profile(), sL - 260f);
                    }
                    else
                    {
                        var sc = race.Route.Shortcuts.FirstOrDefault(s => s.Id == scId); if (sc == null) { report.AppendLine($"  {label}: no shortcut {scId}"); continue; }
                        float sIn = race.Route.LapStationOf(sc.Path[0]); var before = LapPart(sIn - 120f, sIn); float off = LineLen(before);
                        line = before.Concat(sc.Path).ToList(); prof = sc.AiProfile?.Select((v, i) => i % 2 == 0 ? v + off : v).ToArray();
                    }
                    foreach (bool flat in new[] { false, true })
                    {
                        FenceChunk.RestoreAll(); var flights = new List<R68Flight>(); var run = new Run();
                        yield return FlyLine(line, flat ? 60f : 99f, flat ? null : prof, flights, run, p => "x", null, .4f, 80f);
                        var f = flights.OrderByDescending(q => q.air).FirstOrDefault();
                        report.AppendLine($"  {Nm(vid),-14} {label,-52} {(flat ? "flat out" : "AI speed"),-8}: {(f != null ? FlightTxt(f) : "no flight")}");
                    }
                }
            }
        }
    }
}
