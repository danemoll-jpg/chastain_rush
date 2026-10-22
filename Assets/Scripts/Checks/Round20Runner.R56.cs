using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Racer;
using UnityEngine;

namespace ChastainRush
{
    // Round 56 in the exe (-exeRound56 <dir> [-r56Only ejump,ebarricade,ejerk,erace,epics] [-traffic Normal] -logFile <dir>\Player.log):
    //   ejump       A: the Elaine jump flat out (Street Classic, Drifter Twin) against the lap's way: the speed off the lip, where it comes down, time
    //               saved; then the good jump turning up Hayworth at the corner instead of the way back (reported); short (15 m/s off the lip), no jump
    //               (over the edge at 8 m/s), round the barrier through 960's yard: each slower than the lap
    //   ebarricade  B: the barricade across Elaine's mouth stands in a forward race (not in free roam or a reverse one), breaks with no speed lost,
    //               and is whole again once the car has gone
    //   ejerk       C: up Elaine's climb at 22 and 28 m/s in both lanes (Drifter Twin, Street Classic): the most the heading swings in one step
    //   erace       Addison Heights forward x2 (the AI takes the jump sometimes) and reverse x1, Hard, 3 AI, traffic as given: every AI finishes
    //   epics       Dan's four spots, the jump from behind, the landing and the run onto Crawford, the way back
    public sealed partial class Round20Runner
    {
        static readonly Vector3 E56Lip = new Vector3(3047f, 0f, 307.8f);   // make_addison_route.py E_LIP

        IEnumerator R56Jump()
        {
            yield return R54Shortcut("f_elaine_jump", "forward", "A. THE ELAINE JUMP (forward): off the shaped lip, down the bank, over the corner onto Crawford's west leg, back round 1143", sc =>
            {
                var path = sc.Path.ToList();
                int iLip = path.FindIndex(q => Flat(q - E56Lip).magnitude < 1.5f);
                return new List<(string, List<Vector3>, float[])>
                {
                    ("short: 15 m/s off the lip", path, Held(sc, 15f)),
                    ("no jump: over the edge at 8 m/s, down through the brush", path, Held(sc, 8f)),
                    ("round the barrier: Elaine north, 960's drive, down the bank to the corner", Dodge(Pts(3218f, 305f, 3208f, 308f, 3197f, 314.5f, 3182f, 314.5f, 3162f, 310f, 3139f, 307f, 3069f, 307f, 3058f, 310f, 3040f, 322f, 3030f, 334f, 3012f, 338f, 3000f, 326f, 2993f, 310f, 2985f, 308f, 2972f, 308.5f, 2958f, 311f, 2948.5f, 313.2f)
                        .Concat(path.Skip(path.FindIndex(q => Flat(q - new Vector3(2948.5f, 0f, 313.2f)).magnitude < 2.5f))).ToList()), null),
                };
            }, null, "56");
            // the good jump at a race speed (24 m/s off the lip, as the AI): down near the corner, on round 1143
            yield return StartRoute("addison_heights.json", "forward", 0);
            var s1 = race.Route.Shortcuts.FirstOrDefault(s => s.Id == "f_elaine_jump");
            if (s1 != null)
                foreach (var vid in new[] { "original", "drifter" })
                {
                    yield return Vehicle(vid);
                    var r = new Run(); float t = -1f; var w = new R54Watch();
                    yield return TimedVia(s1, s1.Path.ToList(), r, x => t = x, () => R54Track(w), Held(s1, 24f));
                    yield return Capture($"56_A_{vid}_24_end");
                    report.AppendLine($"  {Nm(vid),-16} the jump at 24 m/s, on round 1143: {(r.reached && t > 0f ? $"{t:0.0} s" : "did not get through (" + r.end + $" at ({car.Body.position.x:0}, {car.Body.position.z:0}))")}; {R54Line(w)}");
                }
            race.EndRace(); RaceController.DirectionOverride = null;
            // the good jump, then up Hayworth at the corner (the lap) instead of on round 1143: not a cheat (the corner is the lap), reported for Dan
            yield return StartRoute("addison_heights.json", "forward", 0);
            var s2 = race.Route.Shortcuts.FirstOrDefault(s => s.Id == "f_elaine_jump");
            if (s2 == null) { race.EndRace(); yield break; }
            var p2 = s2.Path.ToList(); int iC = p2.FindIndex(q => q.x < 2988f);
            float sCorner = AbsStation(RaceRoute.Surface(2983f, 312f)), sEnd = AbsStation(s2.Path[s2.Path.Length - 1]); if (sEnd < sCorner) sEnd += race.Route.LapLength;
            var viaCorner = p2.Take(iC).ToList(); viaCorner.AddRange(LapSlice(sCorner, sEnd));
            foreach (var vid in new[] { "original", "drifter" })
            {
                yield return Vehicle(vid);
                var r = new Run(); float t = -1f; var w = new R54Watch();
                yield return TimedVia(s2, viaCorner, r, x => t = x, () => R54Track(w));
                report.AppendLine($"  (for Dan) {Nm(vid),-16} the good jump, then right up Hayworth at the corner: {(r.reached && t > 0f ? $"{t:0.0} s" : "did not get through (" + r.end + ")")}; {R54Line(w)}");
            }
            race.EndRace(); RaceController.DirectionOverride = null;
        }

        // the flight itself: flat out along the shortcut's line (a forward race, the clock running), the speed and heading at the lip, then the car every 0.1 s
        // until 25 m past its first touchdown
        IEnumerator R56Probe()
        {
            report.AppendLine(); report.AppendLine("=== A. probe: off the shaped lip flat out");
            yield return StartRoute("addison_heights.json", "forward", 0);
            var sc = race.Route.Shortcuts.FirstOrDefault(s => s.Id == "f_elaine_jump");
            if (sc == null) { race.EndRace(); yield break; }
            var dE = new Vector3(Mathf.Sin(267f * Mathf.Deg2Rad), 0f, Mathf.Cos(267f * Mathf.Deg2Rad));
            foreach (var vid in new[] { "original", "drifter" })
                foreach (float cap in new[] { 99f, 22f, 18f })
                {
                    yield return Vehicle(vid);
                    float sa = AbsStation(sc.Path[0]); var line = LapSlice(sa - 60f, sa).ToList(); line.AddRange(sc.Path);
                    race.StartRace(); while (race.State == RaceController.Stage.Countdown) yield return null;
                    Place(line[0], line[2] - line[0], 15f); yield return new WaitForFixedUpdate();
                    var run = new Run(); bool past = false; float clock = 0f, next = 0f, air = 0f; Vector3 down = new Vector3(float.NaN, 0f, 0f); var sb = new System.Text.StringBuilder();
                    yield return DriveLine(line, cap, 40f, run, null, r =>
                    {
                        var p = car.Body.position; var v = car.Body.linearVelocity; clock += Time.fixedDeltaTime;
                        if (!past && Vector3.Dot(Flat(p - E56Lip), dE) > 0f && Flat(p - E56Lip).magnitude < 10f)
                        {
                            past = true; next = clock;
                            sb.Append($"lip at ({p.x:0.0}, {p.y:0.0}, {p.z:0.0}) {v.magnitude:0.0} m/s heading {Mathf.Atan2(v.x, v.z) * Mathf.Rad2Deg:0} climbing {Mathf.Asin(Mathf.Clamp(v.y / Mathf.Max(v.magnitude, .1f), -1f, 1f)) * Mathf.Rad2Deg:0} deg, wheels {car.GroundedWheels};");
                        }
                        if (past && clock >= next) { next += .1f; sb.Append($" ({p.x:0}, {p.y:0.0}, {p.z:0}) {car.GroundedWheels}w;"); }
                        if (past) { if (car.GroundedWheels == 0) air += Time.fixedDeltaTime; else { if (air > .3f && float.IsNaN(down.x)) down = p; air = 0f; } }
                        return past && (clock - next > 8f || (!float.IsNaN(down.x) && Flat(p - down).magnitude > 25f));
                    }, 5f, true);
                    report.AppendLine($"  {Nm(vid),-16} cap {cap:0}: {(past ? sb.ToString() : "never reached the lip: " + run.end + $" at ({car.Body.position.x:0}, {car.Body.position.z:0})")}");
                    report.AppendLine($"      first down {(float.IsNaN(down.x) ? "-" : $"({down.x:0.0}, {down.z:0.0}), {Flat(down - E56Lip).magnitude:0} m out")}");
                    Flush();
                }
            race.EndRace(); RaceController.DirectionOverride = null;
        }

        // the shortcut's line held to 'speed' from 80 m before the lip to 30 m past it (TimedVia's stations: its 60 m lead-in first)
        static float[] Held(RaceShortcut sc, float speed)
        {
            float s = 60f; for (int i = 1; i < sc.Path.Length; i++) { if (Flat(sc.Path[i] - E56Lip).magnitude < 1.5f) break; s += Flat(sc.Path[i] - sc.Path[i - 1]).magnitude; }
            return new[] { s - 80f, speed, s + 30f, speed };
        }

        IEnumerator R56Barricade()
        {
            report.AppendLine(); report.AppendLine("=== B. the barricade across Elaine's mouth");
            var root = GameObject.Find("Elaine barricade (forward race)");
            if (!root) { Check("the barricade is in the scene", false); yield break; }
            var ro = root.GetComponent<RaceOnly>();
            if (race.State != RaceController.Stage.Free) race.EndRace();
            yield return new WaitForSecondsRealtime(.5f);
            bool freeShown = ro.Shown;
            yield return StartRoute("addison_heights.json", "reverse", 0); yield return new WaitForSecondsRealtime(.5f);
            bool revShown = ro.Shown; race.EndRace(); RaceController.DirectionOverride = null;
            yield return StartRoute("addison_heights.json", "forward", 0); yield return new WaitForSecondsRealtime(.5f);
            bool fwdShown = ro.Shown;
            Check("it stands only in a forward Addison Heights race", fwdShown && !revShown && !freeShown, $"forward {fwdShown}, reverse {revShown}, free roam {freeShown}");
            var chunks = root.GetComponentsInChildren<FenceChunk>(true);
            yield return Vehicle("original");
            var at = root.GetComponent<RestoreWhenClear>().at; var d = new Vector3(-0.932f, 0f, 0.3624f);   // the track file's elaine_mouth dir
            var line = new List<Vector3> { RaceRoute.Surface(at.x - d.x * 30f, at.z - d.z * 30f), RaceRoute.Surface(at.x - d.x * 15f, at.z - d.z * 15f), RaceRoute.Surface(at.x, at.z), RaceRoute.Surface(at.x + d.x * 15f, at.z + d.z * 15f), RaceRoute.Surface(at.x + d.x * 25f, at.z + d.z * 25f) };
            line = Dense(line, 2f);
            int b0 = FenceChunk.TotalBroken(); float before = -1f, after = -1f;
            Place(line[0], line[2] - line[0], 22f); yield return new WaitForFixedUpdate();
            var run = new Run();
            yield return DriveLine(line, 22f, 8f, run, null, r =>
            {
                float s = Vector3.Dot(Flat(car.Body.position - at), d);
                if (before < 0f && s > -2.5f) before = Flat(car.Body.linearVelocity).magnitude;
                if (after < 0f && s > 2.5f) after = Flat(car.Body.linearVelocity).magnitude;
                return s > 20f;
            }, 5f, true);
            int broke = FenceChunk.TotalBroken() - b0;
            Check("driven through at 22 m/s: it breaks and costs no speed", broke > 0 && before > 0f && after >= before - 0.3f, $"{broke} barricade(s) broken; {before:0.0} m/s 2.5 m before it, {after:0.0} m/s 2.5 m past it");
            yield return Capture("56_B_broken");
            respawn.PlaceAt(RaceRoute.Surface(at.x + 120f, at.z - 40f) + Vector3.up * .4f, 90f); race.NotifyTeleported();
            yield return new WaitForSecondsRealtime(7f);
            int left = chunks.Sum(c => c.broken == null ? 0 : c.broken.Count(x => x));
            Check("whole again once nobody is near", left == 0, $"{left} still broken 7 s after the car left");
            race.EndRace(); RaceController.DirectionOverride = null;
        }

        IEnumerator R56Jerk()
        {
            report.AppendLine(); report.AppendLine("=== C. up Elaine's climb (Dan's BUG-002 at x 3088.66 z 307.72): free roam, straight on, the heading's swing and the body's contacts");
            if (race.State != RaceController.Stage.Free) race.EndRace();
            foreach (var vid in new[] { "drifter", "original" })
            {
                yield return Vehicle(vid);
                foreach (float lane in new[] { 305.5f, 309f })
                    foreach (float v in new[] { 22f, 28f })
                    {
                        var line = Pts(3150f, lane, 3120f, lane, 3090f, lane, 3064f, lane);
                        Place(line[0], line[2] - line[0], v); yield return new WaitForFixedUpdate();
                        var run = new Run(); float lastYaw = car.transform.eulerAngles.y, most = 0f, sideMost = 0f; Vector3 mostAt = Vector3.zero;
                        yield return DriveLine(line, v, 8f, run, null, r =>
                        {
                            var p = car.Body.position; float yaw = car.transform.eulerAngles.y;
                            float dy = Mathf.Abs(Mathf.DeltaAngle(lastYaw, yaw)) / Time.fixedDeltaTime; lastYaw = yaw;
                            float side = Mathf.Abs(Vector3.Dot(car.Body.linearVelocity, car.transform.right));
                            if (p.x < 3135f && p.x > 3068f) { if (dy > most) { most = dy; mostAt = p; } sideMost = Mathf.Max(sideMost, side); }
                            return p.x < 3066f;
                        }, 5f, true);
                        report.AppendLine($"  {Nm(vid),-16} lane z {lane:0.0} at {v:0} m/s: the heading's fastest swing {most:0} deg/s at ({mostAt.x:0.0}, {mostAt.z:0.0}); most sideways speed {sideMost:0.0} m/s; {run.end}");
                    }
            }
        }

        // C again (Dan, after watching the check drive the shortcut: "I saw you we absolutely doing it too"): the whole run-in, from Suholden 60 m before the
        // turn-off, through the barricade, down Elaine and up to the lip, flat out as the shortcut checks drive it: the sharpest swings of the heading
        // (deg/s) and of the sideways speed, where, wheels down, and what the body touched
        IEnumerator R56Trace()
        {
            report.AppendLine(); report.AppendLine("=== C. the run-in traced (a forward race, flat out along the shortcut's line, to the lip)");
            yield return StartRoute("addison_heights.json", "forward", 0);
            var sc = race.Route.Shortcuts.FirstOrDefault(s => s.Id == "f_elaine_jump");
            if (sc == null) { race.EndRace(); yield break; }
            foreach (var vid in new[] { "drifter", "original" })
                for (int rep = 0; rep < 2; rep++)
                {
                    yield return Vehicle(vid);
                    float sa = AbsStation(sc.Path[0]); var line = LapSlice(sa - 60f, sa).ToList();
                    line.AddRange(sc.Path.Where(q => q.x > E56Lip.x + 2f));
                    race.StartRace(); while (race.State == RaceController.Stage.Countdown) yield return null;
                    Place(line[0], line[2] - line[0], 15f + 5f * rep); yield return new WaitForFixedUpdate();
                    var ct = car.GetComponent<ContactTracer>() ?? car.gameObject.AddComponent<ContactTracer>(); ct.Take();
                    var spikes = new List<(float rate, string at)>(); float lastYaw = car.Body.rotation.eulerAngles.y; Vector3 lastV = car.Body.linearVelocity;
                    var run = new Run(); var steps = new System.Text.StringBuilder();
                    yield return DriveLine(line, 99f, 30f, run, null, r =>
                    {
                        var p = car.Body.position; float yaw = car.Body.rotation.eulerAngles.y, dt = Time.fixedDeltaTime;
                        if (rep == 0 && p.x < 3210f && p.x > 3068f) steps.Append($" {p.x:0.0}:{p.y - RaceRoute.Surface(p.x, p.z).y:0.00}/{car.Body.linearVelocity.y:0.0}/{car.GroundedWheels}/{car.transform.eulerAngles.x:0}/{car.transform.eulerAngles.z:0}");
                        float rate = Mathf.DeltaAngle(lastYaw, yaw) / dt; lastYaw = yaw;
                        var v = car.Body.linearVelocity; float side = Vector3.Dot(v - lastV, car.transform.right) / dt; lastV = v;
                        string touch = ct.Take();
                        if (Mathf.Abs(rate) > 25f || Mathf.Abs(side) > 12f)
                            spikes.Add((Mathf.Abs(rate) + Mathf.Abs(side), $"({p.x:0.0}, {p.z:0.0}) {Flat(v).magnitude:0.0} m/s, heading swing {rate:+0;-0} deg/s, sideways {side:+0.0;-0.0} m/s2, wheels {car.GroundedWheels}{(string.IsNullOrEmpty(touch) ? "" : ", body: " + touch.Trim().Replace("	", " "))}"));
                        return p.x < E56Lip.x + 4f;
                    }, 5f, true);
                    var top = spikes.OrderByDescending(x => x.rate).Take(6).ToList();
                    report.AppendLine($"  {Nm(vid),-16} run {rep + 1} (entered at {15f + 5f * rep:0} m/s): {spikes.Count} sharp steps; body contacts let glide on the climb so far {VehicleSurfaceContacts.GlideIgnored}; {run.end}");
                    foreach (var t in top) report.AppendLine("      " + (t.at.Length > 300 ? t.at.Substring(0, 300) : t.at));
                    if (steps.Length > 0) report.AppendLine("      x: height over the road / vertical speed / wheels down / pitch / roll:" + steps);
                    Flush();
                }
            race.EndRace(); RaceController.DirectionOverride = null;
        }

        IEnumerator R56Race()
        {
            var lines = new List<string>(); var behind = new List<float>(); var passes = new List<int>();
            foreach (var (f, d, n) in new[] { ("addison_heights.json", "forward", 1), ("addison_heights.json", "forward", 2), ("addison_heights.json", "reverse", 1) })
                yield return Guard(R55AiRace(f, d, n, lines, behind, passes));
        }

        IEnumerator R56Pics()
        {
            report.AppendLine(); report.AppendLine("=== pictures");
            yield return StartRoute("addison_heights.json", "forward", 0);   // a forward race: the barricade stands
            yield return Vehicle("drifter");
            var v = views();
            foreach (var (nm, x, z, yaw) in new[] { ("bug001_joel_drive", 2963.10f, 411.11f, 264.03f), ("bug002_elaine_climb", 3088.66f, 307.72f, 294.25f), ("bug003_elaine_turnoff", 3205.88f, 303.89f, 287.23f), ("bug004_crawford_west_leg", 2947.19f, 315.69f, 141.4f) })
            {
                var fwd = new Vector3(Mathf.Sin(yaw * Mathf.Deg2Rad), 0f, Mathf.Cos(yaw * Mathf.Deg2Rad));
                if (v) v.SetView(CameraViews.View.FirstPerson);
                Place(RaceRoute.Surface(x, z), fwd, 0f); race.NotifyTeleported(); yield return new WaitForSecondsRealtime(2f);
                yield return Capture($"56_{nm}_dans_view");
                if (v) v.SetView(CameraViews.View.Chase);
                yield return new WaitForSecondsRealtime(1f); yield return Capture($"56_{nm}_chase");
            }
            yield return Still("56_B_the_barricade_from_suholden", RaceRoute.Surface(3222f, 303.5f) + Vector3.up * 2.2f, RaceRoute.Surface(3199.3f, 313.5f) + Vector3.up * 0.6f, 60f);
            race.EndRace(); RaceController.DirectionOverride = null;
            Vector3 S(float x, float z, float up) => RaceRoute.Surface(x, z) + Vector3.up * up;
            yield return Still("56_A_the_jump_from_behind", S(3072f, 307.5f, 6f), S(2985f, 306f, 0f), 60f);
            yield return Still("56_A_the_landing_and_the_corner", S(2960f, 296f, 6f), S(3010f, 307f, 2f), 60f);
            yield return Still("56_A_the_run_onto_crawford", S(2995f, 300f, 4f), S(2945f, 314f, 0f), 60f);
            yield return Still("56_A_the_way_back", S(2944f, 312f, 3.5f), S(2950f, 356f, 1f), 60f);
            yield return Still("56_A_the_way_back_onto_hayworth", S(2950f, 362f, 3.5f), S(3000f, 352f, 1f), 60f);
        }
    }
}
