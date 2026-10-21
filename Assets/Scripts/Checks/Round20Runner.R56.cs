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
        static readonly Vector3 E56Lip = new Vector3(3047f, 0f, 309.3f);

        IEnumerator R56Jump()
        {
            yield return R54Shortcut("f_elaine_jump", "forward", "A. THE ELAINE JUMP (forward): off the shaped lip, down the bank, over the corner onto Crawford's west leg, back round 1143", sc =>
            {
                var path = sc.Path.ToList();
                int iLip = path.FindIndex(q => Flat(q - E56Lip).magnitude < 1.5f);
                return new List<(string, List<Vector3>, float[])>
                {
                    ("short: 15 m/s off the lip", path, HoldFrom(sc, RaceRoute.Surface(E56Lip.x, E56Lip.z), 15f)),
                    ("no jump: over the edge at 8 m/s, down through the brush", path, HoldFrom(sc, RaceRoute.Surface(E56Lip.x, E56Lip.z), 8f)),
                    ("round the barrier: Elaine north, 960's drive, down the bank to the corner", Dodge(Pts(3218f, 305f, 3208f, 308f, 3197f, 314.5f, 3182f, 314.5f, 3162f, 310f, 3139f, 307f, 3069f, 307f, 3058f, 310f, 3040f, 322f, 3030f, 334f, 3012f, 338f, 3000f, 326f, 2993f, 310f, 2985f, 308f, 2972f, 308.5f, 2958f, 311f, 2948.5f, 313.2f)
                        .Concat(path.Skip(path.FindIndex(q => Flat(q - new Vector3(2948.5f, 0f, 313.2f)).magnitude < 2.5f))).ToList()), null),
                };
            }, null, "56");
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
            var at = root.transform.position; var d = new Vector3(-0.6227f, 0f, 0.7824f);
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
            race.EndRace(); RaceController.DirectionOverride = null;
            Vector3 S(float x, float z, float up) => RaceRoute.Surface(x, z) + Vector3.up * up;
            yield return Still("56_A_the_jump_from_behind", S(3070f, 307.5f, 2.4f), S(2985f, 305f, 0f), 60f);
            yield return Still("56_A_the_landing_and_the_corner", S(2960f, 296f, 6f), S(3010f, 307f, 2f), 60f);
            yield return Still("56_A_the_run_onto_crawford", S(2995f, 300f, 4f), S(2945f, 314f, 0f), 60f);
            yield return Still("56_A_the_way_back", S(2944f, 312f, 3.5f), S(2950f, 356f, 1f), 60f);
            yield return Still("56_A_the_way_back_onto_hayworth", S(2950f, 362f, 3.5f), S(3000f, 352f, 1f), 60f);
        }
    }
}
