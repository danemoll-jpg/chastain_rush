using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Racer;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace ChastainRush
{
    // Round 49 in the exe (-exeRound49 <dir> [-r49Only lktrail,lkintake,lkkicker,lkshut,lkfwd,lkrace,lkmap,lkpics] [-traffic Normal] -logFile <dir>\Player.log):
    //   lktrail   C: the lake trail behind the pool, both directions, every offered vehicle at the race's held speeds: no stop
    //   lkintake  A: the intake in a reverse race (Street Classic, Drifter Twin): clean (down the shaft, through B29's glass, out of MH15, on), time
    //             against the lap's own way; off the jetty at 9 m/s (short, into the lake, not taken); a reset from there
    //   lkkicker  B: the kicker (Street Classic, Drifter Twin): flat out (speed at the lip, air, where it comes down), time against the lap's way;
    //             held to 17 m/s up the kicker (into the hollow), a reset from there
    //   lkshut    both closed in a forward race (barriers, clues hidden, the intake's grate, the intake shut), open in a reverse race and Free Roam
    //   lkfwd     the forward race: one lap on the Street Classic, 0 missed
    //   lkrace    the reverse race, 2 laps, 3 AI, twice: everyone finishes; who takes each shortcut and who fails
    //   lkmap     D: the map's pointer with a virtual gamepad (and a virtual mouse): from each corner, a place there chosen with the pointer and
    //             traveled to; a waypoint set with the pointer off-center; a click on a place asks to travel
    //   lkpics    each turn-off and hint, the jetty and the intake, the aquarium run, the manhole past CP9, the kicker, its landing, the trail
    public sealed partial class Round20Runner
    {
        static readonly Vector3 IntakeAt = new Vector3(581.6f, 0f, 1318.0f), IntakeEdge = new Vector3(582.6f, 0f, 1306.5f), KickerLip = new Vector3(1227.9f, 0f, 1001.18f);   // make_lakes_route.py K_LIP (tuning try 2)
        static Vector3 KickerDir => new Vector3(Mathf.Sin(256f * Mathf.Deg2Rad), 0f, Mathf.Cos(256f * Mathf.Deg2Rad));
        static LakeDrain Intake() => LakeDrain.All.FirstOrDefault(d => d && !string.IsNullOrEmpty(d.shutForwardOf));

        IEnumerator R49Trail(string[] ids)
        {
            report.AppendLine(); report.AppendLine("=== C. THE LAKE TRAIL BEHIND THE POOL: both directions, every offered vehicle, at the race's held speeds (the route's AI zones)");
            var bad = new List<string>();
            foreach (var dir in new[] { "forward", "reverse" })
            {
                yield return StartLakes(dir, 0);
                var r = race.Route; var zones = dir == "reverse" ? race.RouteData.ai_zones_reverse : race.RouteData.ai_zones;
                var lot = zones.First(z => z.id == "lot"); var bridge = zones.First(z => z.id == "footbridge");
                float a = dir == "forward" ? lot.s0 - 30f : bridge.s0 - 30f, b = dir == "forward" ? bridge.s1 + 40f : lot.s1 + 30f;
                var line = LapSlice(a, b);
                var cum = new float[line.Count]; for (int i = 1; i < line.Count; i++) cum[i] = cum[i - 1] + Flat(line[i] - line[i - 1]).magnitude;
                var prof = new List<float>();
                foreach (var z in zones.Where(z => z.s1 > a && z.s0 < b).OrderBy(z => z.s0)) prof.AddRange(new[] { Mathf.Max(0f, z.s0 - a), z.speed, Mathf.Min(b - a, z.s1 - a), z.speed });
                foreach (var id in ids.Where(i => race.RouteData == null || race.RouteData.AllowsVehicle(i)))
                {
                    yield return Vehicle(id);
                    race.StartRace(); while (race.State == RaceController.Stage.Countdown) yield return null;
                    Place(line[0], line[2] - line[0], 14f); yield return new WaitForFixedUpdate();
                    var run = new Run(); float slow = 99f, tt = 0f; Vector3 slowAt = Vector3.zero; string slowBy = "";
                    yield return DriveLine(line, 30f, 60f, run, null, rr =>
                    {
                        tt += Time.fixedDeltaTime; float v = Flat(car.Body.linearVelocity).magnitude;
                        if (tt > 0.5f && v < slow)
                        {
                            slow = v; slowAt = car.Body.position;
                            slowBy = string.Join(", ", Physics.OverlapSphere(slowAt, 3.5f, ~0, QueryTriggerInteraction.Ignore).Where(c => c.attachedRigidbody != car.Body && !c.name.StartsWith("Terrain")).Select(c => c.name).Distinct().Take(6));
                        }
                        return false;
                    }, 5f, true, prof.ToArray());
                    bool ok = run.reached && run.minSpeed > 3f;
                    if (!ok) { bad.Add($"{dir} {Nm(id)}"); yield return Capture($"49_trail_{dir}_{id}_slowed"); }
                    report.AppendLine($"  {dir,-8} {Nm(id),-18} {(run.reached ? "through" : "STOPPED " + run.end + $" at ({car.Body.position.x:0}, {car.Body.position.z:0})")} in {run.time:0.0} s, slowest {run.minSpeed:0.0} m/s " +
                                      $"at ({slowAt.x:0.0}, {slowAt.y:0.0}, {slowAt.z:0.0}){(slowBy.Length > 0 ? " by " + slowBy : "")}, most off the line {run.maxDev:0.0} m");
                }
                race.EndRace();
            }
            RaceController.DirectionOverride = null;
            Check("C: the lake trail behind the pool, both directions, every offered vehicle at the race's speeds: no stop", bad.Count == 0, bad.Count == 0 ? "" : "not: " + string.Join(", ", bad));
        }

        IEnumerator R49Intake()
        {
            report.AppendLine(); report.AppendLine("=== A. THE INTAKE (reverse race): clean, timed against the lap's own way; too slow off the jetty");
            yield return StartLakes("reverse", 0);
            var sc = LakesSc("r_lake_intake"); var dr = Intake();
            if (sc == null || dr == null) { Check("A: the intake is in the reverse race and the build", false, $"shortcut {sc != null}, intake {dr != null}"); race.EndRace(); yield break; }
            report.AppendLine($"  {sc.Name}: forgives {string.Join(", ", race.Route.Checkpoints.Where((c, i) => i >= sc.FirstForgiven && i <= sc.LastForgiven).Select(c => c.Label))}");
            var bad = new List<string>();
            foreach (var id in new[] { "original", "drifter" })
            {
                yield return Vehicle(id);
                float road = -1f, via = -1f; var r0 = new Run(); var r1 = new Run();
                yield return TimedLakes(sc, false, r0, t => road = t);
                race.StartRace(); while (race.State == RaceController.Stage.Countdown) yield return null;
                int sw0 = LakeDrain.Swallowed, mh0 = ManholeLid.Bursts; float edgeV = -1f, glassFrom = -1f, glassTo = -1f; bool shot = false;
                yield return TimedLakes(sc, true, r1, t => via = t, 60f, null, 0f, () =>
                {
                    var p = car.Body.position;
                    if (edgeV < 0f && Flat(p - IntakeEdge).magnitude < 1.5f) edgeV = Flat(car.Body.linearVelocity).magnitude;
                    if (LakeDrain.Swallowed > sw0 && InGlass(p)) { if (glassFrom < 0f) glassFrom = Time.time; glassTo = Time.time; if (!shot && id == "original") { shot = true; StartCoroutine(Capture("49_A_aquarium_chase")); } }
                });
                bool took = LakeDrain.Swallowed > sw0, popped = ManholeLid.Bursts > mh0;
                float saved = road > 0f && via > 0f ? road - via : -99f;
                if (!(took && popped && r1.reached && saved > 1f)) bad.Add(Nm(id));
                report.AppendLine($"  {Nm(id),-16} the lap's way {road:0.0} s; the intake {via:0.0} s (off the jetty at {edgeV:0.0} m/s, down the shaft {took}, in glass {(glassFrom >= 0f ? $"{glassTo - glassFrom:0.0} s" : "NEVER")}, " +
                                  $"out of MH15 {popped}, {(r1.reached ? "on along North Lakeside Drive" : "STOPPED " + r1.end + $" at ({car.Body.position.x:0}, {car.Body.position.y:0.0}, {car.Body.position.z:0})")}): saves {saved:0.0} s");
                race.StartRace(); while (race.State == RaceController.Stage.Countdown) yield return null;
            }
            Check("A: the intake taken clean (Street Classic, Drifter Twin): down the shaft, through the glass, out of the manhole past CP9, and it saves time", bad.Count == 0, bad.Count == 0 ? "" : "not: " + string.Join(", ", bad));
            // a miss: off the jetty at 9 m/s (short, into the lake), then a reset
            yield return Vehicle("original");
            { int sw0 = LakeDrain.Swallowed; var run = new Run(); bool wet = false;
              yield return TimedLakes(sc, true, run, t => { }, 60f, IntakeEdge + Vector3.up * 302f, 9f, () => { if (ShallowWater.InWater(car.Body.position + Vector3.up * .1f) && Flat(car.Body.position - IntakeAt).magnitude < 25f) wet = true; });
              bool taken = LakeDrain.Swallowed > sw0;
              yield return Capture("49_A_miss_in_the_lake");
              bool reset = ResetNow(out var to, out _); float toLap = reset ? Flat(to - race.Route.PointAt(AbsStation(to), out _)).magnitude : -1f;
              Check("A: off the jetty too slow (9 m/s): in the lake, not taken; a reset puts it back on the race's line", wet && !taken && reset && toLap < 8f, $"in the lake {wet}, taken {taken}, reset {(reset ? $"to ({to.x:0}, {to.z:0}), {toLap:0.0} m from the line" : "NO SPOT")}"); }
            race.EndRace(); RaceController.DirectionOverride = null;
        }

        IEnumerator R49Kicker()
        {
            report.AppendLine(); report.AppendLine("=== B. THE KICKER (reverse race): flat out, timed against the lap's own way; held to 16 m/s at the kicker's lip");
            yield return StartLakes("reverse", 0);
            var sc = LakesSc("r_kicker_jump"); if (sc == null) { Check("B: the kicker is in the reverse race", false); race.EndRace(); yield break; }
            report.AppendLine($"  {sc.Name}: forgives {string.Join(", ", race.Route.Checkpoints.Where((c, i) => i >= sc.FirstForgiven && i <= sc.LastForgiven).Select(c => c.Label))}");
            var bad = new List<string>();
            foreach (var id in new[] { "original", "drifter" })
            {
                yield return Vehicle(id);
                float road = -1f, via = -1f; var r0 = new Run(); var r1 = new Run();
                yield return TimedLakes(sc, false, r0, t => road = t);
                race.StartRace(); while (race.State == RaceController.Stage.Countdown) yield return null;
                var k = new KickRes(); yield return TimedLakes(sc, true, r1, t => via = t, 60f, null, 0f, () => KickWatch(k, id == "original"));
                float saved = road > 0f && via > 0f ? road - via : -99f;
                bool clean = k.landed && !k.hollow && k.up > .6f;
                if (!(clean && r1.reached && saved > 1f)) bad.Add(Nm(id));
                report.AppendLine($"  {Nm(id),-16} the lap's way {road:0.0} s; the kicker {via:0.0} s: {KickLine(k)}{(r1.reached ? "; on to the lap" : "; STOPPED " + r1.end)}: saves {saved:0.0} s");
                race.StartRace(); while (race.State == RaceController.Stage.Countdown) yield return null;
            }
            Check("B: the kicker flat out (Street Classic, Drifter Twin): clears the hollow, lands on its wheels, on to the lap past CP5, and it saves time", bad.Count == 0, bad.Count == 0 ? "" : "not: " + string.Join(", ", bad));
            yield return Vehicle("original");
            { var k = new KickRes(); var run = new Run();
              yield return TimedLakes(sc, true, run, t => { }, 60f, KickerLip + Vector3.up * 300f, 16f, () => KickWatch(k, false));
              yield return Capture("49_B_short_in_the_hollow");
              bool reset = ResetNow(out var to, out _); float toLap = reset ? Flat(to - race.Route.PointAt(AbsStation(to), out _)).magnitude : -1f;
              Check("B: off the kicker at 16 m/s: short, down in the hollow; a reset puts it back on the race's line", k.landed && k.hollow && reset && toLap < 8f, $"{KickLine(k)}; reset {(reset ? $"to ({to.x:0}, {to.z:0}), {toLap:0.0} m from the line" : "NO SPOT")}"); }
            race.EndRace(); RaceController.DirectionOverride = null;
        }
        sealed class KickRes { public float lipV = -1f, lipY, top = float.MinValue, air, up = -1f, cur; public bool past, landed, hollow, shot; public Vector3 land = new Vector3(float.NaN, 0f, 0f); }
        void KickWatch(KickRes k, bool shots)
        {
            var p = car.Body.position;
            if (!k.past && Vector3.Dot(Flat(p - KickerLip), KickerDir) > 0f && Flat(p - KickerLip).magnitude < 6f) { k.past = true; k.lipV = car.Body.linearVelocity.magnitude; k.lipY = p.y; }
            if (!k.past || k.landed) return;
            k.top = Mathf.Max(k.top, p.y);
            if (shots && !k.shot && car.GroundedWheels == 0 && Vector3.Dot(Flat(p - KickerLip), KickerDir) > 22f) { k.shot = true; StartCoroutine(Capture("49_B_kicker_flight_chase")); }
            if (car.GroundedWheels == 0) k.cur += Time.fixedDeltaTime;
            else if (k.cur > .3f) { k.landed = true; k.air = k.cur; k.land = p; k.up = car.transform.up.y; k.hollow = p.y < 299.0f; }
        }
        static string KickLine(KickRes k) =>
            $"off the lip at {k.lipV:0.0} m/s, {k.air:0.00} s in the air, {k.top - k.lipY:0.0} m over the lip, down at ({k.land.x:0}, {k.land.y:0.0}, {k.land.z:0}) {(float.IsNaN(k.land.x) ? -1f : Flat(k.land - KickerLip).magnitude):0} m out" +
            $"{(k.hollow ? " IN THE HOLLOW" : "")}, upright {k.up:0.00}";

        IEnumerator R49Shut()
        {
            report.AppendLine(); report.AppendLine("=== A / B closed in a forward race, open in a reverse race and Free Roam");
            if (race.State != RaceController.Stage.Free) race.EndRace();
            yield return new WaitForSecondsRealtime(0.5f);
            var only = FindObjectsByType<RaceOnly>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            RaceOnly Named(string n) => only.FirstOrDefault(o => o.name.StartsWith(n));
            var ic = Named("Lake intake clues"); var ig = Named("Lake intake grate"); var kc = Named("RK clues"); var dr = Intake();
            string State() => $"intake clues {(ic ? ic.Shown.ToString() : "MISSING")}, grate {(ig ? ig.Shown.ToString() : "MISSING")}, kicker clues {(kc ? kc.Shown.ToString() : "MISSING")}";
            Check("free roam: both hints shown, no grate on the intake", ic && ic.Shown && ig && !ig.Shown && kc && kc.Shown, State());
            yield return StartLakes("forward", 0); yield return new WaitForSecondsRealtime(0.5f);
            var bars = FindAnyObjectByType<RaceBarriers>();
            var names = bars ? Enumerable.Range(0, bars.transform.childCount).Select(i => bars.transform.GetChild(i).name).ToList() : new List<string>();
            var b1 = race.Route.Closures.FirstOrDefault(c => c.label != null && c.label.StartsWith("the intake's track")); var b2 = race.Route.Closures.FirstOrDefault(c => c.label != null && c.label.StartsWith("the kicker's way"));
            Check("forward race: barriers across the intake's track and the kicker's way; the hints hidden, the intake's grate on and the intake shut",
                  b1 != null && b2 != null && names.Contains("Barrier " + b1.id) && names.Contains("Barrier " + b2.id) && ic && !ic.Shown && kc && !kc.Shown && ig && ig.Shown && dr && RaceOnly.ForwardRaceOf(dr.shutForwardOf)
                  && race.Route.Shortcuts.All(s => !s.Id.StartsWith("r_")), State() + $"; {race.Route.Closures.Count} barriers");
            if (dr)
            {
                yield return Vehicle("original"); int sw0 = LakeDrain.Swallowed;
                Place(dr.mouth + Vector3.up * 3f, Vector3.forward, 0f); yield return new WaitForSecondsRealtime(3f);
                Check("forward race: a vehicle dropped on the intake sits on its grate", LakeDrain.Swallowed == sw0 && car.Body.position.y > dr.mouth.y - 0.5f, $"at {car.Body.position.y - dr.mouth.y:+0.0;-0.0} m from the water");
                yield return Still("49_fwd_intake_grate", dr.mouth + new Vector3(10f, 5f, -10f), dr.mouth, 60f);
            }
            yield return StartLakes("reverse", 0); yield return new WaitForSecondsRealtime(0.5f);
            Check("reverse race: both shortcuts in it, the hints shown, no grate", ic && ic.Shown && kc && kc.Shown && ig && !ig.Shown && race.Route.Shortcuts.Count(s => s.Id.StartsWith("r_")) == 2,
                  State() + $"; shortcuts {string.Join(", ", race.Route.Shortcuts.Select(s => s.Name))}");
            race.EndRace(); RaceController.DirectionOverride = null;
        }

        IEnumerator R49Fwd()
        {
            report.AppendLine(); report.AppendLine("=== the forward race (the trail moved): one lap");
            yield return StartLakes("forward", 0);
            var r = race.Route;
            report.AppendLine($"  {r.Name} {r.Direction}: {r.LapLength:0} m, {r.Checkpoints.Count} checkpoints, shortcuts {string.Join(", ", r.Shortcuts.Select(s => s.Name))}, {r.Closures.Count} barriers");
            Check("forward: its two shortcuts, 18 barriers (16 and the two reverse shortcuts' ways)", r.Shortcuts.Count == 2 && r.Closures.Count == 18);
            race.EndRace();
            yield return R47Lap("forward", "original");
        }

        IEnumerator R49Race()
        {
            report.AppendLine(); report.AppendLine($"=== the reverse race, 2 laps, 3 AI, twice, traffic {(Traffic.Instance ? Traffic.LevelNames[Traffic.Instance.Level] : "none")}, the player parked off the course");
            var lines = new List<string>(); int intakeIn = 0, intakeMiss = 0, kickTries = 0, kickShort = 0;
            System.Action<ArcadeVehicle> took = v => { if (v && Flat(v.Body.position - IntakeAt).magnitude < 12f) intakeIn++; };
            LakeDrain.Took += took;
            for (int raceNo = 0; raceNo < 2; raceNo++)
            {
                yield return StartLakes("reverse", 3);
                respawn.PlaceAt(RaceRoute.Surface(760f, 1530f) + Vector3.up * .4f, 0f); race.NotifyTeleported();
                var ai = race.Entrants.Where(e => !e.IsPlayer).ToList(); var onJetty = new Dictionary<RaceEntrant, int>(); var wet = new HashSet<RaceEntrant>(); var atLip = new Dictionary<RaceEntrant, int>(); var low = new HashSet<RaceEntrant>();
                Time.timeScale = 2f; Time.maximumDeltaTime = .5f;
                float end = Time.realtimeSinceStartup + 9f * 60f;
                while (Time.realtimeSinceStartup < end && ai.Any(e => !e.Finished))
                {
                    foreach (var e in ai)
                    {
                        if (!e.Car) continue; var p = e.Car.Body.position;
                        if (Flat(p - IntakeEdge).magnitude < 4f && (!onJetty.TryGetValue(e, out int l) || l != e.CompletedLaps)) { onJetty[e] = e.CompletedLaps; wet.Remove(e); }
                        if (onJetty.ContainsKey(e) && !wet.Contains(e) && ShallowWater.InWater(p + Vector3.up * .1f) && Flat(p - IntakeAt).magnitude < 25f && Flat(p - IntakeAt).magnitude > 4.5f) { wet.Add(e); intakeMiss++; lines.Add($"{e.Name} ({e.VehicleName}) missed the intake on lap {e.CompletedLaps + 1}"); }
                        if (Flat(p - KickerLip).magnitude < 4f && (!atLip.TryGetValue(e, out int l2) || l2 != e.CompletedLaps)) { atLip[e] = e.CompletedLaps; kickTries++; low.Remove(e); }
                        if (atLip.ContainsKey(e) && !low.Contains(e) && p.y < 297.5f && p.x > 1185f && p.x < 1228f && p.z > 975f && p.z < 1020f) { low.Add(e); kickShort++; lines.Add($"{e.Name} ({e.VehicleName}) came up short off the kicker on lap {e.CompletedLaps + 1}"); }
                    }
                    yield return new WaitForSecondsRealtime(.1f);
                }
                Time.timeScale = 1f;
                foreach (var e in ai)
                    Check($"race {raceNo + 1}: {e.Name} ({e.VehicleName}) finishes the 2 reverse laps", e.Finished,
                          $"{(e.Finished ? $"total {e.FinalTotal:0.0} s" : $"not finished, laps {e.CompletedLaps}")}, laps {string.Join(" / ", e.LapTimes.Select(x => x.ToString("0.0")))}, missed {e.MissedTotal}, shortcuts {(e.ShortcutsTaken.Count > 0 ? string.Join(", ", e.ShortcutsTaken) : "none")}");
                race.EndRace(); RaceController.DirectionOverride = null;
            }
            LakeDrain.Took -= took;
            report.AppendLine($"INFO  the AI in two reverse races: down the intake {intakeIn} times, missed it {intakeMiss}; off the kicker {kickTries} times, short into the hollow {kickShort}{(lines.Count > 0 ? " (" + string.Join("; ", lines) + ")" : "")}");
        }

        // ---------------------------------------------------------------- D: the map's pointer
        IEnumerator R49Map()
        {
            report.AppendLine(); report.AppendLine("=== D. THE MAP: the left stick pans (to every edge), the right stick moves the pointer, A acts under it (a virtual gamepad); a click (a virtual mouse)");
            yield return FreeRoam();
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            pad = InputSystem.AddDevice<Gamepad>("Round49 virtual pad");
            var m = WorldMap.Instance; var ex = Exploration.Instance;
            if (!m || !ex) { Check("D: the map and the exploration are in the build", false); InputSystem.RemoveDevice(pad); yield break; }
            var corners = new[] { ("north-west", new Vector2(0f, 1f)), ("north-east", new Vector2(1f, 1f)), ("south-east", new Vector2(1f, 0f)), ("south-west", new Vector2(0f, 0f)) };
            var free = ex.destinations.Where(d => !d.fixedY).ToList();
            // a place for each corner (the nearest to it), discovered by driving to it (a check's own save)
            var picks = new List<Exploration.Destination>();
            foreach (var (name, uv) in corners)
            {
                var cw = m.WorldPoint(uv); var d = free.OrderBy(x => Flat(x.position - cw).sqrMagnitude).First(x => !picks.Contains(x)); picks.Add(d);
                if (!ex.Discovered(d.id)) { Place(RaceRoute.Surface(d.position.x, d.position.z) + Vector3.up, Quaternion.Euler(0, d.yaw, 0) * Vector3.forward, 0f); yield return new WaitForSecondsRealtime(2f); }
            }
            report.AppendLine($"  the places: {string.Join(", ", picks.Select(p => $"{p.title} ({p.position.x:0}, {p.position.z:0})"))}; discovered {picks.Count(p => ex.Discovered(p.id))} of 4");
            var start = picks.Last(); Place(RaceRoute.Surface(start.position.x, start.position.z) + Vector3.up, Vector3.forward, 0f); yield return new WaitForSecondsRealtime(1f);
            int ci = 0; var notOk = new List<string>();
            foreach (var (name, uv) in corners)
            {
                var d = picks[ci++];
                yield return OpenMapPad(m); if (!WorldMap.IsOpen) { notOk.Add(name + ": the map did not open"); continue; }
                // the left stick held toward the corner: the view pans to the map's edge, then the pointer goes on to the corner
                var stick = new Vector2(uv.x * 2f - 1f, uv.y * 2f - 1f).normalized;
                yield return Hold(new GamepadState { leftStick = stick }, 10f);
                var pw = m.PointerWorld; var cw = m.WorldPoint(uv); float cornerOff = Flat(pw - cw).magnitude;
                yield return Capture($"49_map_corner_{name}");
                // the whole map (LT held), then the pointer onto the corner's place with the right stick
                yield return Hold(new GamepadState { leftTrigger = 1f }, 2.5f);
                float tEnd = Time.realtimeSinceStartup + 15f;
                while (Time.realtimeSinceStartup < tEnd && m.PlaceUnderPointer() != System.Array.IndexOf(ex.destinations, d))
                {
                    var want = m.Normalized(d.position) - m.PointerUV; var s = new Vector2(want.x, want.y) * m.Zoom * 12f;
                    if (s.magnitude > 1f) s.Normalize(); else if (s.magnitude < 0.35f) s = s.normalized * 0.35f;
                    InputSystem.QueueStateEvent(pad, new GamepadState { rightStick = s }); yield return null;
                }
                InputSystem.QueueStateEvent(pad, new GamepadState()); yield return new WaitForSecondsRealtime(0.3f);
                bool onIt = m.PlaceUnderPointer() == System.Array.IndexOf(ex.destinations, d);
                yield return Press(GamepadButton.South); bool asked = m.Confirming;
                yield return Capture($"49_map_{name}_travel_question");
                yield return Press(GamepadButton.South); yield return new WaitForSecondsRealtime(2f);
                float off = Flat(car.Body.position - d.position).magnitude;
                bool ok = cornerOff < 0.03f * m.World.width && onIt && asked && !WorldMap.IsOpen && off < 30f;
                if (!ok) notOk.Add(name);
                report.AppendLine($"  {name,-10} the pointer {cornerOff:0} m from the map's corner after the pan; on {d.title} {onIt}; A asked to travel {asked}; traveled: {off:0} m from it");
                if (WorldMap.IsOpen) m.Close();
            }
            Check("D: from each corner of the map, the left stick brings the corner under the pointer, the pointer chooses a place there (A) and travel takes you to it", notOk.Count == 0, notOk.Count == 0 ? "" : "not: " + string.Join(", ", notOk));
            // a waypoint with the pointer off-center
            yield return OpenMapPad(m);
            ex.ClearWaypoint();
            yield return Hold(new GamepadState { rightStick = new Vector2(0.9f, -0.6f) }, 1.0f);
            var ptr = m.Pointer; var at = m.PointerWorld; var mid = m.WorldPoint(m.Center);
            bool overPlace = m.PlaceUnderPointer() >= 0;
            yield return Press(GamepadButton.North);
            bool wpOk = ex.Waypoint.HasValue && Flat(ex.Waypoint.Value - at).magnitude < 3f && Flat(at - mid).magnitude > 50f;
            yield return Capture("49_map_waypoint_off_center");
            Check("D: Y sets the waypoint where the pointer is, off the view's center", wpOk && !overPlace, $"pointer {ptr.x:0}, {ptr.y:0} from the middle; the waypoint {(ex.Waypoint.HasValue ? $"{Flat(ex.Waypoint.Value - at).magnitude:0.0} m from the pointer's spot, {Flat(ex.Waypoint.Value - mid).magnitude:0} m from the view's center" : "NOT SET")}");
            // the mouse: moved onto a place and clicked, the travel question
            var mouse = InputSystem.AddDevice<Mouse>("Round49 virtual mouse"); mouse.MakeCurrent();
            var tgt = picks[0]; m.ZoomBy(0.01f); yield return null;
            var local = Vector2.Scale((m.Normalized(tgt.position) - m.Center) * m.Zoom, PictureSize(m));
            var screen = RectTransformUtility.WorldToScreenPoint(null, PictureRect(m).TransformPoint(local));
            InputSystem.QueueStateEvent(mouse, new MouseState { position = screen - Vector2.one * 4f, delta = Vector2.one * 4f }); yield return null; yield return null;
            InputSystem.QueueStateEvent(mouse, new MouseState { position = screen, delta = Vector2.one * 4f }); yield return null; yield return null;
            bool hover = m.PlaceUnderPointer() == System.Array.IndexOf(ex.destinations, tgt);
            InputSystem.QueueStateEvent(mouse, new MouseState { position = screen }.WithButton(MouseButton.Left)); yield return null; yield return null;
            InputSystem.QueueStateEvent(mouse, new MouseState { position = screen }); yield return null; yield return null;
            bool clicked = m.Confirming;
            yield return Capture("49_map_mouse_click_travel_question");
            Check("D: the mouse is the pointer: moved onto a place, a click asks to travel there", hover && clicked, $"under the mouse {hover}, asked {clicked}");
            m.CancelTravel(); m.Close(); InputSystem.RemoveDevice(mouse); InputSystem.RemoveDevice(pad);
        }
        static RectTransform PictureRect(WorldMap m) => m.PictureTransform;
        // View opens the map (a second try a moment later: right after it closed, MenuInput's release barrier can still hold the press)
        IEnumerator OpenMapPad(WorldMap m)
        {
            for (int k = 0; k < 3 && !WorldMap.IsOpen; k++) { yield return new WaitForSecondsRealtime(0.4f); yield return Press(GamepadButton.Select); }
            if (!WorldMap.IsOpen) report.AppendLine("  (View did not open the map three times: the check opened it)");
            if (!WorldMap.IsOpen) m.Open();
            yield return new WaitForSecondsRealtime(0.3f);
        }
        static Vector2 PictureSize(WorldMap m) { var r = PictureRect(m); return r ? r.rect.size : new Vector2(1220f, 587f); }

        IEnumerator R49Pics()
        {
            report.AppendLine(); report.AppendLine("=== pictures");
            yield return StartLakes("reverse", 0);
            car.Body.position = RaceRoute.Surface(760f, 1530f) + Vector3.up; car.Body.linearVelocity = Vector3.zero;
            Vector3 G(float x, float z, float up) => RaceRoute.Surface(x, z) + Vector3.up * up;
            var dr = Intake(); var wy = dr ? dr.mouth.y : 300.45f; var ia = new Vector3(IntakeAt.x, wy, IntakeAt.z);
            yield return Still("49_A_turnoff_sign", G(640f, 1263.5f, 1.8f), G(608f, 1268f, 1.2f), 58f);
            yield return Still("49_A_track_to_the_jetty", G(596f, 1276f, 2.2f), ia + Vector3.up * 0.5f, 62f);
            yield return Still("49_A_jetty_and_intake", G(583.5f, 1293f, 2.4f), ia, 62f);
            yield return Still("49_A_intake_from_the_lake", ia + new Vector3(18f, 4f, 12f), ia, 58f);
            yield return Still("49_A_manhole_past_cp9", G(700f, 1494f, 3f), G(672f, 1496.8f, 0.3f), 60f);
            var cp9 = race.Route.Checkpoints.Count >= 9 ? race.Route.Checkpoints[8].Position : G(640f, 1500f, 0f);
            yield return Still("49_A_manhole_and_cp9", G(712f, 1504f, 6f), cp9, 62f);
            var kl = new Vector3(KickerLip.x, 300.5f, KickerLip.z);
            yield return Still("49_B_turnoff_sign", G(1272f, 1060f, 1.8f), G(1262f, 1026f, 1.3f), 58f);
            yield return Still("49_B_between_the_houses", G(1262f, 1016f, 2f), kl, 62f);
            yield return Still("49_B_kicker", kl - KickerDir * 17f + Vector3.up * 1.5f + Vector3.Cross(Vector3.up, KickerDir) * 3f, kl, 60f);
            yield return Still("49_B_over_the_hollow", kl + Vector3.up * 2f, kl + KickerDir * 60f, 62f);
            yield return Still("49_B_landing_past_cp5", G(1140f, 972f, 4f), kl + KickerDir * 58f - Vector3.up * 1f, 62f);
            yield return Still("49_C_trail_behind_the_pool_photo2", G(1315f, 1122f, 1.7f), G(1324f, 1098f, 1.2f), 62f);
            yield return Still("49_C_trail_to_the_footbridge", G(1321f, 1119f, 1.7f), G(1343f, 1139f, 1.0f), 62f);
            yield return Still("49_C_between_pool_house_and_courts_grass", G(1373f, 1082f, 2f), G(1373f, 1128f, 0.5f), 62f);
            yield return Still("49_C_from_above", G(1335f, 1080f, 45f), G(1338f, 1120f, 0f), 60f);
            race.EndRace(); RaceController.DirectionOverride = null;
            Check("pictures written", true, "49_*.png");
        }
    }
}
