using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Racer;
using UnityEngine;

namespace ChastainRush
{
    // Round 46 in the exe (-exeRound46 <dir> [-r46Only speed,boat,boattimes,boatrace,drain,pics] [-traffic Normal] -logFile <dir>\Player.log): the
    // Chastain Lakes shortcuts redone
    //   speed     (design) every offered vehicle flat out from North Lakeside Drive down the boat jump's run-up (the gap between 146 and 147) to
    //             West Chastain Lake's shore, trees out of the way: its speed at the shore
    //   boat      the boat jump on every offered vehicle, from the lap 60 m before the turn-off: flat out straight up the slide (clears the water,
    //             lands upright: speed off the top, air, height, where); at 80 % of that run's speed at the slide's foot; and 10 degrees off the
    //             line into the chute (into the lake?)
    //   boattimes the Street Classic and the Drifter Twin by the boat jump against the lap's own way over the same ground, flat out
    //   boatrace  the full race (2 laps), 3 AI, traffic as given: everyone finishes; who takes the boat jump, who falls in
    //   drain     down the lake drain and through B28's glass under East Chastain Lake (Street Classic): pictures from inside
    //   pics      the dock and the boat from the turn-off, the slide, the flight, the landing
    public sealed partial class Round20Runner
    {
        IEnumerator R46Speed(string[] ids)
        {
            report.AppendLine(); report.AppendLine("=== Round 46 (design): flat out down the boat jump's run-up, North Lakeside Drive to the shore (trees off)");
            if (race.State != RaceController.Stage.Free) race.EndRace();
            var trunks = TreeTrunks.Instance; if (trunks) trunks.gameObject.SetActive(false);
            var pts = new[] { new Vector2(578.5f, 1503f), new Vector2(574.8f, 1450f), new Vector2(571.5f, 1410f), new Vector2(569.4f, 1381f) };
            var line = new List<Vector3>();
            for (int i = 0; i + 1 < pts.Length; i++) for (int k = 0; k < 10; k++) { var q = Vector2.Lerp(pts[i], pts[i + 1], k / 10f); line.Add(RaceRoute.Surface(q.x, q.y)); }
            line.Add(RaceRoute.Surface(pts[^1].x, pts[^1].y));
            foreach (var id in ids)
            {
                yield return Vehicle(id);
                Place(line[0] + Vector3.up * .3f, line[2] - line[0], 18f); yield return new WaitForFixedUpdate();
                var marks = new[] { 1480f, 1450f, 1420f, 1400f, 1390f, 1383f }; var at = new Dictionary<float, float>(); var run = new Run();
                yield return DriveLine(line, 99f, 14f, run, null, r => { var p = car.Body.position; foreach (var m in marks) if (!at.ContainsKey(m) && p.z <= m) at[m] = Flat(car.Body.linearVelocity).magnitude; return p.z < 1382f; }, 6f, false);
                report.AppendLine($"  {Nm(id),-18} " + string.Join("  ", marks.Select(m => at.TryGetValue(m, out var v) ? $"z {m:0}: {v:0.0}" : $"z {m:0}: -")) + $"  (max off the line {run.maxDev:0.0} m)");
            }
            if (trunks) trunks.gameObject.SetActive(true);
        }

        // the boat (Tools/mapgen/make_lakes_route.py: W_LIP, W_BEARING 184, the slide 16.5 m)
        static readonly Vector3 BoatLip = new Vector3(567.3f, 0f, 1352.0f);
        static Vector3 BoatDir => new Vector3(Mathf.Sin(184f * Mathf.Deg2Rad), 0f, Mathf.Cos(184f * Mathf.Deg2Rad));
        static Vector3 SlideFoot => BoatLip - BoatDir * 16.49f;

        sealed class BoatResult { public float footV = -1f, lipV = -1f, lipY, top = float.MinValue, air, up = -1f; public Vector3 land = new Vector3(float.NaN, 0f, 0f); public bool landed, wet, offSide, reached; public string end = ""; }

        // from the lap 60 m before the turn-off, along the boat jump's line (rotated 'offDeg' about the slide's foot from 30 m before it), its
        // speed held to 'hold' from 30 m before the slide's foot to its top (0: flat out)
        IEnumerator BoatRun(RaceShortcut sc, float hold, float offDeg, BoatResult res, string trace = null)
        {
            var a = sc.Path[0]; float sa = AbsStation(a);
            var line = LapSlice(sa - 60f, sa).ToList(); line.AddRange(sc.Path);
            var foot = SlideFoot; var rot = Quaternion.Euler(0f, offDeg, 0f);
            if (Mathf.Abs(offDeg) > 0.01f)
                for (int i = 0; i < line.Count; i++)
                {
                    float along = Vector3.Dot(Flat(line[i] - foot), BoatDir);
                    if (along < -30f || Flat(line[i] - foot).magnitude > 140f) continue;
                    var rel = line[i] - foot; rel.y = 0f; var q = foot + rot * rel; line[i] = new Vector3(q.x, line[i].y, q.z);
                }
            var cum = new float[line.Count]; for (int i = 1; i < line.Count; i++) cum[i] = cum[i - 1] + Flat(line[i] - line[i - 1]).magnitude;
            int iFoot = Enumerable.Range(0, line.Count).OrderBy(i => Flat(line[i] - foot).sqrMagnitude).First();
            int iLip = Enumerable.Range(0, line.Count).OrderBy(i => Flat(line[i] - (foot + rot * (BoatLip - foot))).sqrMagnitude).First();
            float[] prof = hold > 0f ? new[] { cum[iFoot] - 30f, hold, cum[iLip], hold } : null;
            Place(line[0], line[2] - line[0], 15f); yield return new WaitForFixedUpdate();
            var run = new Run(); bool past = false, footed = false; float cur = 0f; var lipDir = rot * BoatDir; var lip = foot + rot * (BoatLip - foot);
            var tr = trace != null ? new System.Text.StringBuilder("x\ty\tz\tfwd\tvy\twheels\tpitch\tunder\tcontacts\n") : null;
            var ct = trace != null ? (car.GetComponent<ContactTracer>() ?? car.gameObject.AddComponent<ContactTracer>()) : null;
            yield return DriveLine(line, 99f, 60f, run, null, r =>
            {
                var p = car.Body.position;
                if (tr != null)
                {
                    string c = ct.Take();
                    if (Flat(p - foot).magnitude < 45f || (Vector3.Dot(Flat(p - foot), lipDir) > 0f && Flat(p - foot).magnitude < 30f))
                    {
                        string under = Physics.Raycast(p + Vector3.up * 2f, Vector3.down, out var uh, 6f, ~0, QueryTriggerInteraction.Ignore) ? $"{uh.collider.name} {p.y - uh.point.y:0.00}" : "-";
                        tr.Append($"{p.x:0.00}	{p.y:0.00}	{p.z:0.00}	{car.ForwardSpeed:0.0}	{car.Body.linearVelocity.y:0.0}	{car.GroundedWheels}	{car.transform.eulerAngles.x:0}\t{under}\t{c}\n");
                    }
                }
                if (!footed && Vector3.Dot(Flat(p - foot), lipDir) > 0f && Flat(p - foot).magnitude < 6f) { footed = true; res.footV = Flat(car.Body.linearVelocity).magnitude; }
                if (!past && Vector3.Dot(Flat(p - lip), lipDir) > 0f && Flat(p - lip).magnitude < 8f) { past = true; res.lipV = car.Body.linearVelocity.magnitude; res.lipY = p.y; }
                if (!past && footed && car.GroundedWheels == 0 && Vector3.Dot(Flat(p - foot), lipDir) < 15f && !ShallowWater.InWater(p)) { /* off the side of the boat */ }
                if (past && !res.landed)
                {
                    res.top = Mathf.Max(res.top, p.y);
                    if (car.GroundedWheels == 0 && !ShallowWater.InWater(p + Vector3.up * .1f)) cur += Time.fixedDeltaTime;
                    else if (cur > .3f || ShallowWater.InWater(p + Vector3.up * .1f)) { res.landed = true; res.air = cur; res.land = p; res.up = car.transform.up.y; res.wet = ShallowWater.InWater(p + Vector3.up * .1f) || ShallowWater.InWater(p + Vector3.down * .4f); }
                }
                if (footed && !past && ShallowWater.InWater(p + Vector3.up * .1f)) { res.offSide = true; res.wet = true; res.landed = true; res.land = p; return true; }   // off the boat into the lake
                return res.landed && res.wet && Time.fixedDeltaTime > 0f && (car.Body.position - res.land).magnitude > 6f;
            }, 5f, false, prof);
            res.reached = run.reached; res.end = run.end;
            if (tr != null) File.WriteAllText(Path.Combine(outputDir, $"boat_{trace}.tsv"), tr.ToString());
        }
        // in the lake: in its water, off the boat's side, or down short of where the water ends on the line (77 m out: the arm's shallow tip reads as wading)
        static bool InLake(BoatResult r) => r.wet || r.offSide || (!float.IsNaN(r.land.x) && Flat(r.land - BoatLip).magnitude < 77f);
        string BoatLine(string who, BoatResult r) =>
            $"  {who,-18} at the slide's foot {r.footV:0.0} m/s, off the top at {r.lipV:0.0} m/s; {r.air:0.00} s in the air, {r.top - r.lipY:0.0} m over the top, down at ({r.land.x:0}, {r.land.z:0}) " +
            $"{(float.IsNaN(r.land.x) ? -1f : Flat(r.land - BoatLip).magnitude):0} m out{(r.offSide ? " OFF THE BOAT'S SIDE" : "")}{(r.wet ? " IN THE LAKE" : "")}, upright {r.up:0.00}{(r.reached ? "; on along Lakeside Drive" : "")}";

        IEnumerator R46Boat(string[] ids)
        {
            report.AppendLine(); report.AppendLine("=== A. THE BOAT JUMP: every offered vehicle from the lap, flat out straight up the slide; at 80 % speed; 10 degrees off the line (a real jump: no launch control)");
            yield return StartLakesRace();
            var sc = LakesSc("w_lake_jump"); if (sc == null) { Check("the boat jump is in the race", false); race.EndRace(); yield break; }
            var vs = ids.Where(i => race.RouteData == null || race.RouteData.AllowsVehicle(i)).ToList();
            var full = new Dictionary<string, BoatResult>(); var notClear = new List<string>(); var notWet80 = new List<string>(); var notWetOff = new List<string>();
            var numbers = new List<string>();
            foreach (var id in vs)
            {
                yield return Vehicle(id);
                var r = new BoatResult(); yield return BoatRun(sc, 0f, 0f, r); full[id] = r;
                report.AppendLine(BoatLine(Nm(id) + " full", r));
                if (!(r.landed && !InLake(r) && r.up > .7f)) notClear.Add(Nm(id));
                race.StartRace(); while (race.State == RaceController.Stage.Countdown) yield return null;
            }
            Check("A: every offered vehicle flat out straight up the slide clears the water and lands on its wheels", notClear.Count == 0, notClear.Count == 0 ? "" : "not: " + string.Join(", ", notClear));
            foreach (var id in vs)
            {
                yield return Vehicle(id);
                var r = new BoatResult(); float hold = 0.8f * Mathf.Max(10f, full[id].footV); yield return BoatRun(sc, hold, 0f, r);
                report.AppendLine(BoatLine(Nm(id) + $" 80% ({hold:0.0})", r));
                if (!InLake(r)) notWet80.Add(Nm(id));
                race.StartRace(); while (race.State == RaceController.Stage.Countdown) yield return null;
            }
            Check("A: at 80 % of its full speed every offered vehicle comes down in the lake", notWet80.Count == 0, notWet80.Count == 0 ? "" : "not: " + string.Join(", ", notWet80));
            int k = 0;
            foreach (var id in vs)
            {
                yield return Vehicle(id);
                float off = (k++ % 2 == 0) ? 10f : -10f; var r = new BoatResult(); yield return BoatRun(sc, 0f, off, r);
                report.AppendLine(BoatLine(Nm(id) + $" {off:+0;-0} deg", r));
                if (!InLake(r)) notWetOff.Add(Nm(id));
                race.StartRace(); while (race.State == RaceController.Stage.Countdown) yield return null;
            }
            Check("A: 10 degrees off the line into the chute, flat out, every offered vehicle comes down in the lake", notWetOff.Count == 0, notWetOff.Count == 0 ? "" : "not: " + string.Join(", ", notWetOff));
            // a reset from the lake puts it on the race's line
            bool reset = ResetNow(out var to, out _); float toLap = reset ? Flat(to - race.Route.PointAt(AbsStation(to), out _)).magnitude : -1f;
            report.AppendLine($"  a reset from where the last run ended: {(reset ? $"to ({to.x:0.0}, {to.z:0.0}), {toLap:0.0} m from the race's line" : "NO SPOT")}");
            race.EndRace(); RaceController.DirectionOverride = null;
        }

        IEnumerator R46BoatTrace()
        {
            report.AppendLine(); report.AppendLine("=== (design) traced runs over the dock and the boat: every body contact (boat_<vehicle>.tsv)");
            yield return StartLakesRace();
            var sc = LakesSc("w_lake_jump"); if (sc == null) { race.EndRace(); yield break; }
            foreach (var id in new[] { "original", "tourer", "pebble", "atv" })
            {
                yield return Vehicle(id);
                var r = new BoatResult(); yield return BoatRun(sc, 0f, 0f, r, id); report.AppendLine(BoatLine(Nm(id), r));
                race.StartRace(); while (race.State == RaceController.Stage.Countdown) yield return null;
            }
            race.EndRace(); RaceController.DirectionOverride = null;
        }

        IEnumerator R46Times()
        {
            report.AppendLine(); report.AppendLine("=== A. the boat jump against the lap's own way over the same ground, flat out (Street Classic, Drifter Twin)");
            yield return StartLakesRace();
            var sc = LakesSc("w_lake_jump"); if (sc == null) { Check("the boat jump is in the race", false); race.EndRace(); yield break; }
            var bad = new List<string>();
            foreach (var id in new[] { "original", "drifter" })
            {
                yield return Vehicle(id);
                float road = -1f, via = -1f; var r0 = new Run(); var r1 = new Run();
                yield return TimedLakes(sc, false, r0, t => road = t);
                race.StartRace(); while (race.State == RaceController.Stage.Countdown) yield return null;
                yield return TimedLakes(sc, true, r1, t => via = t, 60f);
                float saved = road > 0f && via > 0f ? road - via : -99f; if (saved < 1f) bad.Add(Nm(id));
                report.AppendLine($"  {Nm(id),-16} the lap's way {road:0.0} s{(r0.reached ? "" : " (STOPPED " + r0.end + ")")}; the boat jump {via:0.0} s{(r1.reached ? "" : $" (STOPPED {r1.end} at ({car.Body.position.x:0.0}, {car.Body.position.y:0.0}, {car.Body.position.z:0.0}))")}: saves {saved:0.0} s");
                race.StartRace(); while (race.State == RaceController.Stage.Countdown) yield return null;
            }
            race.EndRace(); RaceController.DirectionOverride = null;
            Check("A: the boat jump taken clean and flat out saves time against the lap's own way (both vehicles)", bad.Count == 0, bad.Count == 0 ? "" : "not: " + string.Join(", ", bad));
        }

        IEnumerator R46Race()
        {
            report.AppendLine(); report.AppendLine($"=== the Chastain Lakes race, 2 laps, 3 AI, traffic {(Traffic.Instance ? Traffic.LevelNames[Traffic.Instance.Level] : "none")}, the player parked off the course");
            int tries = 0, falls = 0, clean = 0; var lines = new List<string>();
            for (int raceNo = 0; raceNo < 2; raceNo++)
            {
                yield return StartLakesRace(3);
                respawn.PlaceAt(RaceRoute.Surface(760f, 1530f) + Vector3.up * .4f, 0f); race.NotifyTeleported();
                var ai = race.Entrants.Where(e => !e.IsPlayer).ToList(); var onBoat = new Dictionary<RaceEntrant, int>(); var wetHere = new HashSet<RaceEntrant>();
                Time.timeScale = 2f; Time.maximumDeltaTime = .5f;
                float end = Time.realtimeSinceStartup + 9f * 60f;
                while (Time.realtimeSinceStartup < end && ai.Any(e => !e.Finished))
                {
                    foreach (var e in ai)
                    {
                        if (!e.Car) continue; var p = e.Car.Body.position;
                        bool onIt = p.x > 553f && p.x < 583f && p.z > 1345f && p.z < 1388f;   // on the dock or the boat
                        bool wet = p.x > 540f && p.x < 590f && p.z > 1268f && p.z < 1380f && ShallowWater.InWater(p + Vector3.up * .1f);
                        if (onIt && (!onBoat.TryGetValue(e, out int lap) || lap != e.CompletedLaps)) { onBoat[e] = e.CompletedLaps; tries++; wetHere.Remove(e); }
                        if (wet && onBoat.ContainsKey(e) && !wetHere.Contains(e)) { wetHere.Add(e); falls++; lines.Add($"{e.Name} ({e.VehicleName}) fell in on lap {e.CompletedLaps + 1}"); }
                    }
                    yield return new WaitForSecondsRealtime(.1f);
                }
                Time.timeScale = 1f;
                foreach (var e in ai)
                    Check($"race {raceNo + 1}: {e.Name} ({e.VehicleName}) finishes the 2 laps", e.Finished,
                          $"{(e.Finished ? $"total {e.FinalTotal:0.0} s" : $"not finished, laps {e.CompletedLaps}")}, laps {string.Join(" / ", e.LapTimes.Select(x => x.ToString("0.0")))}, missed {e.MissedTotal}, shortcuts {(e.ShortcutsTaken.Count > 0 ? string.Join(", ", e.ShortcutsTaken) : "none")}");
                race.EndRace(); RaceController.DirectionOverride = null;
            }
            clean = tries - falls;
            report.AppendLine($"INFO  the AI on the boat {tries} times in two races: {clean} cleared it, {falls} fell in{(lines.Count > 0 ? " (" + string.Join("; ", lines) + ")" : "")}");
        }

        IEnumerator R46Drain()
        {
            report.AppendLine(); report.AppendLine("=== B. the lake drain's run under East Chastain Lake in glass (B28), Street Classic");
            yield return StartLakesRace();
            var sc = LakesSc("e_lake_drain"); var dr = Drain(); if (sc == null || dr == null) { Check("the lake drain is in the build", false); race.EndRace(); yield break; }
            yield return Vehicle("original");
            int sw0 = LakeDrain.Swallowed, mh0 = ManholeLid.Bursts; var shots = new HashSet<string>(); var run = new Run(); float glassFrom = -1f, glassTo = -1f;
            yield return TimedLakes(sc, true, run, t => { }, 40f, null, 0f, () =>
            {
                if (LakeDrain.Swallowed == sw0) return;
                var p = car.Body.position;
                bool inGlass = InGlass(p);
                if (inGlass) { if (glassFrom < 0f) glassFrom = Time.time; glassTo = Time.time; }
                if (inGlass && !shots.Contains("a") && p.z > 1150f) { shots.Add("a"); StartCoroutine(Capture("46_drain_aquarium_chase")); }
                if (inGlass && !shots.Contains("b") && p.z > 1215f) { shots.Add("b"); StartCoroutine(Capture("46_drain_aquarium_further")); }
            });
            bool took = LakeDrain.Swallowed > sw0, popped = ManholeLid.Bursts > mh0;
            report.AppendLine($"  down the drain {took}; in glass {(glassFrom >= 0f ? $"for {glassTo - glassFrom:0.0} s" : "NEVER")}; out of MH14 {popped}; {(run.reached ? "on along North Lakeside Drive" : "STOPPED " + run.end)}");
            Check("B: down the drain, through B28's glass under the lake, out of the manhole and on", took && glassFrom >= 0f && popped && run.reached);
            race.EndRace(); RaceController.DirectionOverride = null;
        }

        // inside a glass stretch: within its radius of its axis (PipeAquarium.path, every meter)
        static PipeAquarium[] aquaria;
        static bool InGlass(Vector3 p)
        {
            aquaria ??= FindObjectsByType<PipeAquarium>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var a in aquaria) if (a && a.path != null) foreach (var q in a.path) if ((q - p).sqrMagnitude < a.radius * a.radius * 1.1f) return true;
            return false;
        }

        IEnumerator R46Pics()
        {
            report.AppendLine(); report.AppendLine("=== pictures");
            yield return StartLakesRace();
            car.Body.position = RaceRoute.Surface(760f, 1530f) + Vector3.up; car.Body.linearVelocity = Vector3.zero;
            Vector3 G(float x, float z, float up) => RaceRoute.Surface(x, z) + Vector3.up * up;
            var lip = new Vector3(BoatLip.x, 305f, BoatLip.z);
            yield return Still("46_boat_from_the_turnoff", G(596f, 1504f, 2.2f), G(575f, 1470f, 1f), 62f);
            yield return Still("46_dock_and_boat", G(574f, 1394f, 2.5f), lip + Vector3.up * .5f, 62f);
            yield return Still("46_boat_from_the_lake", lip + new Vector3(-22f, 4f, -6f), SlideFoot + Vector3.up * 302f + new Vector3(0f, 0f, 0f), 58f);
            yield return Still("46_slide_up", SlideFoot + new Vector3(0f, 303f, 0f) - BoatDir * 4f, lip + Vector3.up * 1f, 70f);
            yield return Still("46_landing_from_the_road", G(585f, 1258f, 3f), G(561f, 1272f, 0.5f), 62f);
            yield return Still("46_party_sign", G(605f, 1504f, 1.6f), G(589f, 1496.6f, 1.6f), 55f);
            var sc = LakesSc("w_lake_jump"); bool shot = false, shot2 = false;
            if (sc != null)
            {
                yield return Vehicle("original"); race.StartRace(); while (race.State == RaceController.Stage.Countdown) yield return null;
                var r = new BoatResult(); var watch = StartCoroutine(BoatShots(() => shot, v => shot = v, () => shot2, v => shot2 = v));
                yield return BoatRun(sc, 0f, 0f, r); StopCoroutine(watch);
            }
            race.EndRace(); RaceController.DirectionOverride = null;
            Check("pictures written", true, "46_*.png");
        }
        IEnumerator BoatShots(System.Func<bool> a, System.Action<bool> setA, System.Func<bool> b, System.Action<bool> setB)
        {
            while (true)
            {
                var p = car.Body.position;
                if (!a() && Vector3.Dot(Flat(p - SlideFoot), BoatDir) > 6f && Vector3.Dot(Flat(p - SlideFoot), BoatDir) < 14f) { setA(true); yield return Capture("46_up_the_slide_chase"); }
                if (!b() && car.GroundedWheels == 0 && Vector3.Dot(Flat(p - BoatLip), BoatDir) > 30f) { setB(true); yield return Capture("46_boat_jump_flight_chase"); }
                yield return new WaitForFixedUpdate();
            }
        }
    }
}
