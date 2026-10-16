using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Racer;
using UnityEngine;

namespace ChastainRush
{
    // Round 36 Part A in the exe: ChastainRush.exe -exeRound36 <dir> [-r36Only fps,air,tricks,event,glass,inside,rooframp,pics] -logFile <dir>\Player.log.
    // Muted (CheckRun), its files in <dir>, report round36_check.txt. The stunt park:
    //   air       BIG AIR (off the supermarket's roof) and the shop row drop and the tower gap, flat out on the Clay Hopper and the Skywhip 250: launched,
    //             air time, height over the lip, where it came down (on the landing's slope or table), upright, rolled on
    //   tricks    BIG AIR on the Clay Hopper with the stick: a backflip then a roll, landed and scored (the trick panel's lines; a picture of it)
    //   event     the Stunt event once (a 45 s round, checks only): started on its line, a scored combo counted, the results and the Top 10 (the check's
    //             own stunt-park-v1.json)
    //   glass     through the supermarket's windows and along the shop row's partitions: panes and partitions broken, on inside; a reset puts them back
    //   inside    the supermarket's mezzanine ramp, its deck and the ramp up through the skylight: on the roof at the end
    //   rooframp  the roof ramp from the floor: on the roof at the top
    //   pics      the park from above, the roofs, each big feature, the inside
    //   fps       the frame rate parked at Canton Road by the shops (as Round 35) and inside the park, 1280 x 720 (not measured at 3840 x 2160)
    public sealed partial class Round20Runner
    {
        static readonly Vector2 PU = new Vector2(0.9780962345219478f, 0.20815320322778294f), PV = new Vector2(-0.20815320322778294f, 0.9780962345219478f);
        static Vector2 PW(float u, float v) => PU * u + PV * v;
        static float ParkRoofY => StuntPark.Current && StuntPark.Current.roofY > 1f ? StuntPark.Current.roofY : 323.35f;   // (a scene built before the field: make_stunt_park.py's)
        static float ParkStoreY => StuntPark.Current && StuntPark.Current.storeFloorY > 1f ? StuntPark.Current.storeFloorY : 314.35f;
        static Vector3 PDir(float du, float dv) { var d = (PU * du + PV * dv).normalized; return new Vector3(d.x, 0, d.y); }
        Vector3 Floor(float u, float v) { var w = PW(u, v); return RaceRoute.Surface(w.x, w.y); }
        Vector3 Roof(float u, float v) { var w = PW(u, v); return new Vector3(w.x, ParkRoofY, w.y); }
        Vector3 Store(float u, float v) { var w = PW(u, v); return new Vector3(w.x, ParkStoreY + .02f, w.y); }
        // a straight line of points from a to b (2 m apart) at the heights given
        static List<Vector3> Straight(Vector3 a, Vector3 b, float step = 2f) { var l = new List<Vector3>(); int n = Mathf.Max(2, Mathf.CeilToInt(Vector3.Distance(a, b) / step)); for (int i = 0; i <= n; i++) l.Add(Vector3.Lerp(a, b, i / (float)n)); return l; }

        // drive with the player's own input (VehicleInput.Scripted): along the line at `cap` m/s, the air stick from `air` (seconds in the air -> pitch, roll, spin)
        IEnumerator DriveInput(List<Vector3> line, float cap, float timeout, Run run, System.Func<float, Vector3> air = null, System.Func<Run, bool> stop = null)
        {
            var vi = car.GetComponent<VehicleInput>(); vi.Scripted = true; car.enabled = true;
            var cum = new float[line.Count]; for (int i = 1; i < line.Count; i++) cum[i] = cum[i - 1] + Flat(line[i] - line[i - 1]).magnitude;
            float total = cum[cum.Length - 1], t = 0f, inAir = 0f, stopped = 0f, lastS = 0f;
            while (t < timeout)
            {
                yield return new WaitForFixedUpdate(); float dt = Time.fixedDeltaTime; t += dt;
                var p = car.Body.position; float s = ProjectNear(line, cum, p, lastS, out float dev); lastS = s;
                if (t > .6f) run.maxDev = Mathf.Max(run.maxDev, dev);
                if (s >= total - 1.5f) { run.reached = true; break; }
                if (stop != null && stop(run)) break;
                var here = At(line, cum, s); var fl = Flat(At(line, cum, s + .5f) - here).normalized; var aim = At(line, cum, s + 6f);
                float cte = Vector3.Dot(Flat(p - here), Vector3.Cross(Vector3.up, fl));
                float head = Vector3.SignedAngle(Flat(car.transform.forward).normalized, Flat(aim - here).normalized, Vector3.up);
                float steer = Mathf.Clamp((head - Mathf.Atan2(1.6f * cte, Mathf.Max(car.ForwardSpeed, 1f) + 2f) * Mathf.Rad2Deg) / 18f, -1f, 1f);
                float v = car.ForwardSpeed;
                bool flying = car.GroundedWheels == 0; if (flying) inAir += dt; else { if (inAir > .3f && float.IsNaN(run.firstLanding.x)) { run.firstLanding = p; run.upAtLanding = car.transform.up.y; } inAir = 0f; }
                run.maxAir = Mathf.Max(run.maxAir, inAir);
                var a = flying && air != null ? air(inAir) : Vector3.zero;
                vi.sThrottle = v < cap ? 1f : 0f; vi.sBrake = v > cap + 1.5f ? 1f : 0f; vi.sSteer = flying ? a.z : steer; vi.sPitch = a.x; vi.sSpin = a.y;
                if (t > .5f) run.minSpeed = Mathf.Min(run.minSpeed, Flat(car.Body.linearVelocity).magnitude);
                stopped = car.Body.linearVelocity.magnitude < 1f ? stopped + dt : 0f; if (stopped > 5f) { run.end = "stopped"; break; }
                if (((int)(t / 0.1f)) != ((int)((t - dt) / 0.1f))) run.trail.Add(p);
            }
            run.time = t; vi.sThrottle = vi.sBrake = vi.sSteer = vi.sPitch = vi.sSpin = 0f; vi.Scripted = false;
        }
        static readonly string[] StuntIds = { "stuntbuggy", "stuntbike" };

        // the run-up, the launch, the flight and the run-out of one aimed launch
        IEnumerator R36Air()
        {
            report.AppendLine(); report.AppendLine("=== Part A3: the big air off the roofs, flat out on the stunt vehicles (aimed launches: every vehicle over the lip at 3 m/s or more leaves at the design speed)");
            if (!StuntPark.Current) { Check("the stunt park is in the build", false); yield break; }
            if (race.State != RaceController.Stage.Free) race.EndRace();
            float roof = ParkRoofY;
            // (name, launch id, start, end of the line past the landing)
            var runs = new[] {
                ("BIG AIR", "park-MEGA", Roof(3398f, 410f), Floor(3570f, 410f)),
                ("Shop row drop", "park-DROP", Roof(3474f, 486f), Floor(3474f, 392f)),
                ("Tower gap", "park-TOWER", Roof(3432f, 452f), Floor(3530f, 452f)),
                ("Roof gap", "park-GAP", Roof(3466f, 481.5f), Roof(3578f, 481.5f)),
            };
            foreach (var (name, jid, a, b) in runs)
            {
                var j = BigJump.All.FirstOrDefault(x => x.id == jid); if (!j) { Check(name + ": its launch is in the park", false); continue; }
                foreach (var id in StuntIds)
                {
                    yield return Vehicle(id);
                    var line = Straight(a, new Vector3(b.x, a.y, b.z)); Place(a, b - a, 4f); yield return new WaitForSecondsRealtime(.3f);
                    float launch = -1f; bool launched = false; System.Action<BigJump, ArcadeVehicle, float> hook = (bj, c, v) => { if (bj == j && c == car) { launch = v; launched = true; } };
                    BigJump.Launched += hook;
                    var traj = new List<string> { "t,x,y,z,vx,vy,vz,wheels,along" }; float tl = 0f;
                    var run = new Run(); float top = -99f, air = 0f, longest = 0f; Vector3 land = new Vector3(float.NaN, 0, 0); float up = -1f; string under = "";
                    var drive = StartCoroutine(DriveInput(line, 30f, 30f, run));
                    while (!run.reached && run.end == "" && run.time < 29.9f)
                    {
                        yield return new WaitForFixedUpdate();
                        if (launched) { top = Mathf.Max(top, car.Body.position.y - j.lip.y); tl += Time.fixedDeltaTime; var bp = car.Body.position; var bv = car.Body.linearVelocity; if (tl < 6f) traj.Add($"{tl:0.00},{bp.x:0.00},{bp.y:0.00},{bp.z:0.00},{bv.x:0.00},{bv.y:0.00},{bv.z:0.00},{car.GroundedWheels},{Vector3.Dot(Flat(bp - j.lip), j.dir):0.00}"); }
                        if (car.GroundedWheels == 0) air += Time.fixedDeltaTime;
                        else
                        {
                            if (launched && air > .8f && float.IsNaN(land.x))
                            {
                                land = car.Body.position; up = car.transform.up.y;
                                var hit = Physics.RaycastAll(land + Vector3.up, Vector3.down, 6f, ~0, QueryTriggerInteraction.Ignore).Where(h => h.collider.attachedRigidbody != car.Body).OrderBy(h => h.distance).FirstOrDefault();
                                under = hit.collider ? hit.collider.name + $" (its face {Vector3.Angle(hit.normal, Vector3.up):0} deg)" : "nothing";
                                if (id == StuntIds[0]) yield return Capture("36_air_" + jid.Substring(5).ToLower() + "_landing");
                            }
                            longest = Mathf.Max(longest, air); air = 0f;
                        }
                        if (id == StuntIds[0] && launched && air > 1.2f && air < 1.2f + Time.fixedDeltaTime * 1.5f) yield return Capture("36_air_" + jid.Substring(5).ToLower() + "_flight");
                    }
                    yield return drive; BigJump.Launched -= hook;
                    File.WriteAllLines(Path.Combine(outputDir, $"flight_{jid.Substring(5).ToLower()}_{id}.csv"), traj);
                    float dist = float.IsNaN(land.x) ? -1f : Flat(land - j.lip).magnitude;
                    bool slope = under.Contains("landing") || under.Contains("Stunt park roof") && jid == "park-GAP";
                    bool ok = launched && !float.IsNaN(land.x) && up > .7f && (run.reached || run.minSpeed > 2f) && under.Contains("Stunt park");
                    report.AppendLine($"  {name,-14} {Nm(id),-16} {(launched ? $"onto the lip at {launch:0.0} m/s, off it at {j.vmax:0} m/s {j.angle:0} deg: {longest:0.00} s in the air, {top:0.0} m over the lip at the top, came down {dist:0.0} m out on {under}, upright {up:0.00}, " + (run.reached ? "rolled on" : "stopped " + run.end) : "NEVER LAUNCHED")}");
                    Check($"{name} on the {Nm(id)}: launched, landed upright on the park (the landing's slope or table) and rolled on", ok);
                    if (name == "BIG AIR") Check($"BIG AIR on the {Nm(id)}: several seconds of air (3 s or more)", longest >= 3f, $"{longest:0.00} s");
                }
            }
        }

        // a backflip, then a roll, off BIG AIR on the Clay Hopper; landed, scored, the trick panel's lines
        IEnumerator R36Tricks()
        {
            report.AppendLine(); report.AppendLine("=== Part A1: a backflip + roll off BIG AIR on the Clay Hopper (the stick through the player's input), landed and scored");
            if (!StuntPark.Current) { Check("the stunt park is in the build", false); yield break; }
            yield return Vehicle("stuntbuggy");
            var acts = car.GetComponent<ArcadeActivities>(); if (!acts) { Check("the stunt scoring is on the player's vehicle", false); yield break; }
            var a = Roof(3398f, 410f); var b = Floor(3570f, 410f); var line = Straight(a, new Vector3(b.x, a.y, b.z));
            int tries = 0; bool landed = false; string panel = ""; var lines = new List<string>();
            while (tries < 3 && !landed)
            {
                tries++; Place(a, b - a, 4f); yield return new WaitForSecondsRealtime(1.5f); acts.NewSession(); yield return new WaitForSecondsRealtime(1.1f);
                // the stick: full back (a backflip) until 330 degrees are turned, then full right (a roll) until 330 more, then let go (it levels itself)
                float pitch = 0f, roll = 0f; int phase = 0; bool shot = false;
                System.Func<float, Vector3> stick = t =>
                {
                    var w = car.transform.InverseTransformDirection(car.Body.angularVelocity) * (Time.fixedDeltaTime * Mathf.Rad2Deg);
                    if (t < .06f) return Vector3.zero;
                    // the player's way: full stick, easing off to stop the turn at 360 (a stunt vehicle keeps turning when the stick is let go)
                    float Ease(float turned) => Mathf.Clamp((360f - Mathf.Abs(turned)) / 45f, .2f, 1f);
                    var rate = car.transform.InverseTransformDirection(car.Body.angularVelocity);
                    float Hold(float r) => Mathf.Abs(r) > .15f ? -Mathf.Sign(r) * .11f : 0f;   // the counter-stick that stops a turn (the player's way)
                    if (phase == 0) { pitch += w.x; roll += w.z; if (Mathf.Abs(pitch) >= 335f) phase = 1; return new Vector3(-Ease(pitch), 0, 0); }
                    if (phase == 1) { pitch += w.x; roll += w.z; if (Mathf.Abs(roll) >= 335f) phase = 2; return new Vector3(0, 0, Ease(roll)); }
                    return new Vector3(Hold(rate.x), 0, -Hold(rate.z));
                };
                var run = new Run(); string before = acts.LastStunt;
                var drive = StartCoroutine(DriveInput(line, 30f, 30f, run, stick, r => acts.LastStunt != before && !acts.StuntBusy));
                while (drive != null && !run.reached && run.end == "" && run.time < 29.9f && !(acts.LastStunt != before && !acts.StuntBusy))
                {
                    yield return null;
                    if (!shot && phase == 1 && acts.Live != null) { shot = true; panel = TrickHud.Text(acts); yield return Capture("36_trick_panel_in_the_air"); }
                }
                yield return drive;
                yield return new WaitForSecondsRealtime(.4f); lines.Add(TrickHud.Text(acts));
                yield return Capture("36_trick_panel_landed");
                landed = acts.LastStunt.Contains("BACKFLIP") && acts.LastStunt.Contains("ROLL");
                report.AppendLine($"  try {tries}: turned pitch {pitch:0} deg, roll {roll:0} deg; landed '{acts.LastStunt.Replace("\n", " / ")}' for {acts.LastStuntPoints:N0} pts ({acts.LastStuntAir:0.00} s, {acts.LastStuntHeight:0.0} m up), touched {acts.LastTouchTilt:0} deg from upright{(acts.LastBail != "" ? ", BAILED: " + acts.LastBail : "")}");
                // let the combo bank (5 s on the ground)
                yield return new WaitForSecondsRealtime(ArcadeActivities.ComboGap + .8f);
            }
            report.AppendLine($"  the trick panel in the air: '{panel}'; after landing: '{string.Join(" | ", lines)}'; the last combo banked {acts.LastComboTotal:N0} pts (x{acts.LastComboLinks}, {acts.LastComboTricks}), Top 10 Best Combos #{acts.LastComboPlace}");
            Check("a backflip + roll off BIG AIR, landed on its wheels and scored (with the x2 for two trick types)", landed && acts.LastFlightTypes >= 2, $"{tries} tries");
            Check("the trick panel showed the tricks in the air and the result after", panel.Contains("BACKFLIP") && lines.Any(l => l.Contains("LANDED") || l.Contains("COMBO")), panel);
            Check("the banked combo is on the Top 10 Best Combos (the check's own file)", acts.LastComboPlace > 0 && StuntProgress.FilePath != null && StuntProgress.FilePath.StartsWith(CheckRun.Dir), StuntProgress.FilePath);
        }

        // the Stunt event once, a short round (checks only)
        IEnumerator R36Event()
        {
            report.AppendLine(); report.AppendLine("=== Part A1: the Stunt event once (a 45 s round for the check; 10 minutes in play)");
            if (!StuntPark.Current) { Check("the stunt park is in the build", false); yield break; }
            yield return Vehicle("stuntbike");
            var acts = car.GetComponent<ArcadeActivities>(); StuntEvent.TestLimit = 45f; StuntEvent done = null; System.Action<StuntEvent> fin = e => done = e; StuntEvent.Finished += fin;
            var ev = StuntEvent.Begin(car); yield return new WaitForSecondsRealtime(.5f);
            float d0 = Flat(car.Body.position - StuntPark.StartPos).magnitude; bool held = car.Body.isKinematic;
            yield return Capture("36_event_start");
            while (ev && ev.State == StuntEvent.Phase.Starting) yield return null;
            Check("the Stunt event starts on its line, held 3 s, then GO", d0 < 2f && held, $"{d0:0.0} m from the line, held {held}");
            // a run: the start line west over the mega hill's face and back, then BIG AIR off the roof (moved there, as a reset would)
            var run = new Run(); var a = car.Body.position; var b = Floor(3470f, 410f);
            yield return StartCoroutine(DriveInput(Straight(a, b), 22f, 25f, run));
            report.AppendLine($"  from the line west over the mega hill: {(run.reached ? "reached the stores" : "stopped " + run.end)}, {acts.Tricks} tricks, combo x{acts.ComboLinks}");
            Place(Roof(3398f, 410f), PDir(1, 0), 4f); yield return new WaitForSecondsRealtime(1.5f);
            var aa = Roof(3398f, 410f); var bb = Floor(3570f, 410f); var run2 = new Run();
            float turned = 0f;
            System.Func<float, Vector3> flip = t => { if (t < .3f || turned >= 352f) return Vector3.zero; turned += Mathf.Abs(car.transform.InverseTransformDirection(car.Body.angularVelocity).x * Time.fixedDeltaTime * Mathf.Rad2Deg); return new Vector3(-Mathf.Clamp((360f - turned) / 120f, .12f, 1f), 0, 0); };
            yield return StartCoroutine(DriveInput(Straight(aa, new Vector3(bb.x, aa.y, bb.z)), 30f, 20f, run2, flip));
            yield return new WaitForSecondsRealtime(ArcadeActivities.ComboGap + 1f);
            report.AppendLine($"  BIG AIR with a backflip: '{acts.LastStunt.Replace("\n", " / ")}', event points {ev.Points:N0}");
            yield return Capture("36_event_running");
            float wait = 0f; while (done == null && wait < 60f) { wait += Time.unscaledDeltaTime; yield return null; }
            StuntEvent.Finished -= fin; StuntEvent.TestLimit = -1f;
            if (done == null) { Check("the Stunt event ended at its limit", false); yield break; }
            report.AppendLine("  " + string.Join("\n  ", done.Log));
            Check("the Stunt event ended, counted the combos banked in the park and wrote its Top 10 entry", done.Points > 0 && done.Entry != null && done.Place > 0, $"{done.Points:N0} pts, {MedalUi.Name(done.Medal)}, best combo {done.Best:N0}, Top 10 #{done.Place}");
            StuntEvent.End();
        }

        // through the supermarket's windows, along the shop row's partitions; a reset puts them back
        IEnumerator R36Glass()
        {
            report.AppendLine(); report.AppendLine("=== Part A2b: into the stores");
            if (!StuntPark.Current) { Check("the stunt park is in the build", false); yield break; }
            if (race.State != RaceController.Stage.Free) race.EndRace();
            yield return Vehicle("original");
            var chunks = StuntPark.Current.breakables.Where(c => c).ToArray();
            int Broken(string name) => chunks.Where(c => c.name.StartsWith(name)).Sum(c => c.broken.Count(x => x));
            // the supermarket: from the floor west through its windows, across the store to its back
            var run = new Run(); var a = Floor(3500f, 405f); var b = Store(3420f, 402.5f);
            Place(a, b - a, 12f); yield return new WaitForSecondsRealtime(.2f);
            yield return StartCoroutine(DriveLine(Straight(a, b), 18f, 20f, run));
            int panes = Broken("StuntParkWindows");
            yield return Capture("36_inside_market");
            Check("through the supermarket's windows: the glass broke and the vehicle went on inside", panes > 0 && run.reached, $"{panes} panes broken, {(run.reached ? "on to the back of the store" : "stopped " + run.end)}, slowest {run.minSpeed:0.0} m/s");
            // the shop row: in at the east end's window, west the length of the hall through the partitions
            var run2 = new Run(); var c0 = Floor(3584f, 455f); var c1 = Store(3584f, 480f); var c2 = Store(3462f, 480f);
            Place(c0, c1 - c0, 6f); yield return new WaitForSecondsRealtime(.2f);
            var line = Straight(c0, c1); line.AddRange(Straight(c1, c2).Skip(1));
            yield return StartCoroutine(DriveLine(line, 16f, 40f, run2));
            int parts = Broken("StuntParkPartitions");
            yield return Capture("36_inside_shop_row");
            Check("along the shop row: in through a window and through the partitions to its west end", parts >= 8 && run2.reached, $"{parts} partition panels broken, {(run2.reached ? "reached the west end" : "stopped " + run2.end + $" at u {Vector2.Dot(new Vector2(car.Body.position.x, car.Body.position.z), PU):0}")}");
            // a reset puts them back
            car.GetComponent<VehicleInput>(); StuntPark.Current.RestoreBreakables(); yield return new WaitForSecondsRealtime(.2f);
            Check("the windows and partitions come back (a reset in the park, or leaving it)", Broken("StuntParkWindows") == 0 && Broken("StuntParkPartitions") == 0);
        }

        // up the mezzanine ramp, along the deck, up the skylight ramp onto the roof
        IEnumerator R36Inside()
        {
            report.AppendLine(); report.AppendLine("=== Part A2b: the supermarket's mezzanine and the ramp up through the skylight onto the roof");
            if (!StuntPark.Current) { Check("the stunt park is in the build", false); yield break; }
            yield return Vehicle("stuntbuggy");
            var a = Store(3387f, 367f); var b = Store(3387f, 433f); var c = Roof(3426f, 433f);
            var line = Straight(a, new Vector3(b.x, b.y + 4.5f, b.z)); line.AddRange(Straight(new Vector3(b.x, b.y + 4.5f, b.z), c).Skip(1));
            Place(a, b - a, 3f); yield return new WaitForSecondsRealtime(.3f);
            var run = new Run(); yield return StartCoroutine(DriveLine(line, 10f, 40f, run));
            float y = car.Body.position.y; yield return Capture("36_inside_skylight_top");
            File.WriteAllLines(Path.Combine(outputDir, "mezzanine_trail.csv"), run.trail.Select(q => $"{q.x:0.00},{q.y:0.00},{q.z:0.00}"));
            Check("up the mezzanine ramp, along the deck and up the skylight ramp: on the roof", run.reached && Mathf.Abs(y - ParkRoofY - .5f) < 1f, $"ended at {y:0.00} m (the roof at {ParkRoofY:0.00}), {(run.reached ? "reached" : "stopped " + run.end)}");
        }
        IEnumerator R36RoofRamp()
        {
            report.AppendLine(); report.AppendLine("=== Part A3: the roof ramp from the floor");
            if (!StuntPark.Current) { Check("the stunt park is in the build", false); yield break; }
            yield return Vehicle("original");
            var a = Floor(3530f, 356f); var c = Roof(3420f, 356f); var line = Straight(a, new Vector3(3448.9f * PU.x + 356f * PV.x, ParkRoofY, 3448.9f * PU.y + 356f * PV.y)); line.AddRange(Straight(line[line.Count - 1], c).Skip(1));
            Place(a, c - a, 8f); yield return new WaitForSecondsRealtime(.2f);
            var run = new Run(); yield return StartCoroutine(DriveLine(line, 16f, 30f, run));
            Check("the roof ramp: from the floor up onto the supermarket's roof", run.reached && car.Body.position.y > ParkRoofY - .2f, $"ended at {car.Body.position.y:0.00} m, {(run.reached ? "on the roof" : "stopped " + run.end)}");
        }

        IEnumerator R36Pics()
        {
            report.AppendLine(); report.AppendLine("=== pictures (36_*.png)");
            if (!StuntPark.Current) yield break;
            float roof = ParkRoofY;
            Place(Floor(3628f, 403.5f), PDir(-1, 0), 0f); yield return new WaitForSecondsRealtime(.6f);
            yield return Still("36_park_from_above", Floor(3660f, 300f) + Vector3.up * 120f, Floor(3510f, 410f), 60f);
            yield return Still("36_park_from_the_entrance", Floor(3650f, 404f) + Vector3.up * 14f, Floor(3480f, 410f), 62f);
            yield return Still("36_roofs", Roof(3600f, 520f) + Vector3.up * 40f, Roof(3440f, 440f), 60f);
            yield return Still("36_big_air_run_up", Roof(3396f, 410f) + Vector3.up * 3f, Roof(3460f, 410f), 62f);
            yield return Still("36_big_air_landing", Floor(3560f, 430f) + Vector3.up * 8f, Floor(3510f, 410f) + Vector3.up * 3f, 60f);
            yield return Still("36_loop", Floor(3530f, 440f) + Vector3.up * 5f, Floor(3550f, 457f) + Vector3.up * 4f, 60f);
            yield return Still("36_half_pipe", Floor(3621f, 395f) + Vector3.up * 5f, Floor(3621f, 440f), 60f);
            yield return Still("36_barrel_roll_and_bus", Floor(3460f, 372f) + Vector3.up * 5f, Floor(3530f, 386f), 60f);
            yield return Still("36_car_gap", Floor(3610f, 450f) + Vector3.up * 5f, Floor(3570f, 443f), 60f);
            yield return Still("36_tower_and_drop", Floor(3510f, 440f) + Vector3.up * 8f, Floor(3470f, 452f) + Vector3.up * 5f, 62f);
            yield return Still("36_roof_gap", Roof(3495f, 481.5f) + Vector3.up * 4f, Roof(3530f, 481.5f), 62f);
            yield return Still("36_roof_ramp", Floor(3530f, 372f) + Vector3.up * 4f, Floor(3460f, 356f) + Vector3.up * 5f, 62f);
            yield return Still("36_inside_aisles", Store(3446f, 402f) + Vector3.up * 2.5f, Store(3400f, 402f) + Vector3.up * 1f, 64f);
            yield return Still("36_inside_mezzanine", Store(3420f, 380f) + Vector3.up * 3f, Store(3387f, 420f) + Vector3.up * 4f, 64f);
            yield return Still("36_inside_shop_row", Store(3586f, 481f) + Vector3.up * 2f, Store(3500f, 481f) + Vector3.up * 1.5f, 64f);
            yield return Still("36_stores_front", Floor(3492f, 432f) + Vector3.up * 3f, Store(3455f, 404f) + Vector3.up * 3f, 64f);
        }

        IEnumerator R36Fps()
        {
            report.AppendLine(); report.AppendLine($"=== frame rate, parked ({Screen.width} x {Screen.height}, not measured at 3840 x 2160)");
            if (race.State != RaceController.Stage.Free) race.EndRace();
            int vs = QualitySettings.vSyncCount, cap = Application.targetFrameRate; QualitySettings.vSyncCount = 0; Application.targetFrameRate = -1;
            var spots = new List<(string, Vector3, float)> { ("Canton Road by the shops (Round 35's spot)", RaceRoute.Surface(3502.8f, 1166.1f), -13f) };
            if (StuntPark.Current) { spots.Add(("in the park, at its start line looking west", Floor(3628f, 403.5f), Mathf.Atan2(PDir(-1, 0).x, PDir(-1, 0).z) * Mathf.Rad2Deg)); spots.Add(("inside the supermarket", Store(3446f, 402f), Mathf.Atan2(PDir(-1, 0).x, PDir(-1, 0).z) * Mathf.Rad2Deg)); }
            foreach (var (label, at, yaw) in spots)
            {
                respawn.PlaceAt(at + Vector3.up * .4f, yaw); race.NotifyTeleported();
                yield return new WaitForSecondsRealtime(3f);
                var frames = new List<float>(); float t0 = Time.realtimeSinceStartup;
                while (Time.realtimeSinceStartup - t0 < 10f) { yield return null; frames.Add(Time.unscaledDeltaTime); }
                var sorted = frames.OrderBy(x => x).ToList(); float p99 = sorted[Mathf.Min(sorted.Count - 1, (int)(sorted.Count * 0.99f))];
                report.AppendLine($"INFO  {label}: {frames.Count / frames.Sum():0} fps average, slowest 1 % {1f / p99:0} fps");
            }
            QualitySettings.vSyncCount = vs; Application.targetFrameRate = cap;
        }
    }
}
