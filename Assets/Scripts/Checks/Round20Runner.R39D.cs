using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Racer;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ChastainRush
{
    // Round 39 Part D in the exe (-exeRound39 <dir> -r39Only getaway,escape,patrol,hidden,chase2,radio,policefps -hiddenPolice -traffic Normal):
    //   getaway    Getaway, one player on the Needle 600 (the fastest road vehicle), driven as a human would (flat out on the roads toward the open
    //              network away from the cops, cutting corners and across the grass where it is shorter): heat 1 to the helicopter (the heat comes
    //              five times faster in the check), pictures of the cop behind at the start, a roadblock, backups, the helicopter; caught or not
    //   escape     Getaway again: the runner drops into the pipe network (the cops cannot follow): escaped
    //   patrol     Speed Patrol: a speeder clocked by the radar, chased with the lights on and pulled over: points
    //   hidden     a hidden cruiser: a pass at 10+ mph over the limit in front of it starts a chase
    //   chase2     Cop vs Runner for two, split screen, two virtual controllers: the cop catches the runner, the roles swap, two rounds
    //   radio      the radio's lines as text with no clips; a test clip played from a drop-in folder (sound on a moment, then off)
    //   policefps  the frame rate in a heat-5 chase on Canton Road (1280 x 720)
    public sealed partial class Round20Runner
    {
        // ---------- a human-like runner: flat out toward a far road node away from the cops, cutting across where it is shorter
        sealed class Escapee { public List<Vector3> path = new(); public int i; public float replanAt, stuckFor; public Vector3 lastPos; public bool hide; public Vector3? hideAt; public int hides; }
        void RunnerStep(Escapee e, ArcadeVehicle c, float dt)
        {
            var net = RoadNet.Shared; var vi = c.GetComponent<VehicleInput>(); if (!vi) return; vi.Scripted = true;
            var p = c.Body.position; var g = GetawayChase.Current;
            // hiding (as a player who has lost them would): out of sight with no cop within 80 m, off the road 60 m on the side away from the cops, and stop
            var rn = g ? g.Runners.FirstOrDefault() : null;
            if (e.hide && rn != null && !rn.seen && !rn.underground)
            {
                var cops2 = g.Cops.Where(x => x.car && !x.block).Select(x => x.Position).ToList(); float near = cops2.Count == 0 ? 999f : cops2.Min(q => Vector3.Distance(q, p));
                if (near > 80f || e.hideAt != null)
                {
                    if (e.hideAt == null)
                    {
                        var right = Flat(c.transform.right).normalized; var mid = cops2.Count == 0 ? p - c.transform.forward * 50f : cops2.Aggregate(Vector3.zero, (a, b) => a + b) / cops2.Count;
                        float side = Vector3.Dot(right, Flat(mid - p)) > 0 ? -1f : 1f; e.hideAt = p + right * side * 60f + Flat(c.transform.forward).normalized * 20f; e.hides++;
                    }
                    var toH = Flat(e.hideAt.Value - p); float angH = Vector3.SignedAngle(Flat(c.transform.forward), toH, Vector3.up);
                    bool there = toH.magnitude < 5f; vi.sSteer = there ? 0f : Mathf.Clamp(angH / 22f, -1f, 1f); vi.sThrottle = there ? 0f : (c.ForwardSpeed > 14f ? .2f : .8f); vi.sBrake = there || c.ForwardSpeed > 18f ? 1f : 0f;
                    e.stuckFor = 0f; return;
                }
            }
            else if (rn != null && rn.seen && e.hideAt != null) { e.hideAt = null; e.replanAt = 0f; }   // seen again: run
            if (Time.time >= e.replanAt || e.i >= e.path.Count - 1)
            {
                e.replanAt = Time.time + 6f; int here = net.Nearest(p, out _, 200, 40); var cops = g ? g.Cops.Where(x => x.car).Select(x => x.Position).ToList() : new List<Vector3>();
                int best = -1; float bs = float.MinValue;
                if (here >= 0)
                    foreach (int n in net.Near(p, 700))
                    {
                        if (net.Component[n] != net.Component[here]) continue; float d = Vector3.Distance(net.P[n], p); if (d < 300) continue;
                        float away = cops.Count == 0 ? 0 : cops.Min(q => Vector3.Distance(q, net.P[n])); float ahead = Vector3.Dot((net.P[n] - p).normalized, c.transform.forward);
                        float score = away * .6f + ahead * 120f + Random.value * 60f; if (score > bs) { bs = score; best = n; }
                    }
                var nodes = best >= 0 ? net.Path(here, best) : null; e.path.Clear(); if (nodes != null) foreach (int n in nodes) e.path.Add(net.P[n]); e.i = 0;
            }
            if (e.path.Count < 2) { vi.sThrottle = 1; vi.sSteer = 0; vi.sBrake = 0; return; }
            while (e.i < e.path.Count - 1 && Vector3.Distance(Flat(e.path[e.i]), Flat(p)) < 14f) e.i++;
            // a cut: aim at a node further on when the straight line there is short of the road's way round (off-road where it is shorter)
            int aim = e.i; for (int k = e.i + 1; k < Mathf.Min(e.path.Count, e.i + 8); k++) { if (Vector3.Distance(p, e.path[k]) < 70f && !Physics.Linecast(p + Vector3.up, e.path[k] + Vector3.up * 1.2f, ~0, QueryTriggerInteraction.Ignore)) aim = k; }
            var to = Flat(e.path[aim] - p); float ang = Vector3.SignedAngle(Flat(c.transform.forward), to, Vector3.up), v = c.ForwardSpeed;
            vi.sSteer = Mathf.Clamp(ang / 22f, -1f, 1f); vi.sThrottle = Mathf.Abs(ang) > 55f && v > 18f ? 0.3f : 1f; vi.sBrake = Mathf.Abs(ang) > 70f && v > 20f ? 0.6f : 0f;
            e.stuckFor = c.Body.linearVelocity.magnitude < 2f ? e.stuckFor + dt : 0f;
            if (e.stuckFor > 4f) { e.stuckFor = 0f; if (CarRecovery.Find(respawn, out var pos, out var yaw)) { respawn.PlaceAt(pos, yaw); race.NotifyTeleported(); e.replanAt = 0f; } }   // a player would reset
        }
        void StopRunner(ArcadeVehicle c) { var vi = c ? c.GetComponent<VehicleInput>() : null; if (vi) { vi.sThrottle = vi.sSteer = vi.sBrake = 0; vi.Scripted = false; } }

        IEnumerator R39Getaway()
        {
            report.AppendLine(); report.AppendLine("=== Part D: Getaway (one player, the Needle 600 driven flat out as a human would; the heat five times faster in the check)");
            if (race.State != RaceController.Stage.Free) race.EndRace();
            yield return Clock(13f); yield return Vehicle("moto");
            respawn.PlaceAt(RaceRoute.Surface(3540f, 900f) + Vector3.up * .4f, 345f); race.NotifyTeleported(); yield return new WaitForSecondsRealtime(1.5f);
            PoliceSetup.Mode = PoliceSetup.Game.Getaway; PoliceSetup.Two = false; PoliceSetup.Minutes = 8; PoliceSetup.Difficulty = 2; GetawayChase.CheckFast = true;
            PoliceSession.Launch(); yield return new WaitForSecondsRealtime(.5f);
            var g = GetawayChase.Current; if (!g) { Check("Getaway starts", false); yield break; }
            var cop0 = g.Cops.FirstOrDefault(); float back = cop0 != null ? Vector3.Distance(cop0.Position, car.Body.position) : -1;
            Check("Getaway: a cop is behind the runner at the start, lights and siren on", cop0 != null && back > 40 && back < 120 && cop0.lights.Siren, $"{g.Cops.Count} cops, the first {back:0} m back");
            yield return Still("39_getaway_cop_behind_at_start", car.Body.position + car.transform.forward * 8f + Vector3.up * 5f, cop0 != null ? cop0.Position : car.Body.position, 58f);
            while (g && g.State == GetawayChase.Phase.Starting) yield return null;
            var e = new Escapee(); float t0 = Time.time; bool shotBlock = false, shotBackup = false, shotHeli = false; int heat = 1, topHeat = 1; var timeline = new List<string>(); var tries = new List<string>();
            for (int attempt = 0; attempt < 3; attempt++)
            {
            if (attempt > 0)   // caught before the roadblock or the helicopter came: a rematch from where the runner is (RESTART)
            {
                if (shotBlock && shotHeli && topHeat >= 5) break;
                PoliceSession.Rematch(); yield return new WaitForSecondsRealtime(.5f); g = GetawayChase.Current; while (g && g.State == GetawayChase.Phase.Starting) yield return null;
                e = new Escapee(); t0 = Time.time; heat = 1;
            }
            while (g && g.State == GetawayChase.Phase.Running && Time.time - t0 < 300f)
            {
                RunnerStep(e, car, Time.deltaTime); yield return null;
                if (g.Heat != heat) { heat = g.Heat; timeline.Add($"heat {heat} at {g.Clock:0} s ({g.Cops.Count(c => !c.block)} cops)"); }
                if (!shotBackup && g.Cops.Count(c => !c.block) >= 3) { shotBackup = true; var b = g.Cops.Last(c => !c.block); yield return Capture("39_getaway_backups_chase"); report.AppendLine($"  backups: {g.Cops.Count(c => !c.block)} cops at {g.Clock:0} s, the newest {Vector3.Distance(b.Position, car.Body.position):0} m away"); }
                if (!shotBlock && g.Blocks.Count > 0) { shotBlock = true; var b = g.Blocks[0]; yield return Still("39_getaway_roadblock", b.centre - b.tangent * 30f + Vector3.up * 6f, b.centre + Vector3.up, 60f); report.AppendLine($"  a roadblock at {g.Clock:0} s, {Vector3.Distance(b.centre, car.Body.position):0} m from the runner"); }
                if (!shotHeli && g.Heli) { shotHeli = true; yield return new WaitForSecondsRealtime(3f); yield return Capture("39_getaway_helicopter_chase"); report.AppendLine($"  the helicopter at {g.Clock:0} s"); }
                topHeat = Mathf.Max(topHeat, g.Heat);
            }
            var ra = g ? g.Runners.FirstOrDefault() : null; tries.Add($"try {attempt + 1}: {(ra == null ? "-" : ra.caught ? $"CAUGHT after {PoliceKit.Mss(ra.freeSeconds)} at heat {ra.topHeat}" : ra.escaped ? $"escaped after {PoliceKit.Mss(ra.freeSeconds)}" : "running")}");
            }
            report.AppendLine("  " + string.Join("; ", tries));
            StopRunner(car);
            var r = g ? g.Runners.FirstOrDefault() : null;
            report.AppendLine("  " + string.Join("; ", timeline));
            report.AppendLine($"  the radio: {string.Join("  |  ", g ? g.RadioLog.Take(14) : new List<string>())}");
            Check("Getaway: the heat climbs to 5 with backups, a roadblock and the helicopter", topHeat >= 5 && shotBackup && shotBlock && shotHeli, $"top heat {topHeat}, backups {shotBackup}, roadblock {shotBlock}, helicopter {shotHeli}");
            Check("Getaway on Hard at heat 5: the cops catch a human-like runner flat out on the Needle 600 (or the run says how long it lasted)", r != null && r.caught, r == null ? "no runner" : r.caught ? $"CAUGHT after {PoliceKit.Mss(r.freeSeconds)} at heat {r.topHeat}, {r.dodged} cops dodged" : r.escaped ? $"ESCAPED after {PoliceKit.Mss(r.freeSeconds)}" : $"still free after {PoliceKit.Mss(g.Clock)}, bust {r.bust:0.00}");
            yield return new WaitForSecondsRealtime(5f);
            PoliceSession.Stop(); GetawayChase.CheckFast = false;
        }

        IEnumerator R39Escape()
        {
            report.AppendLine(); report.AppendLine("=== Part D: Getaway escapes (Easy on the road, and into the pipe network)");
            if (race.State != RaceController.Stage.Free) race.EndRace();
            yield return Vehicle("moto");
            // on the road, Easy, the same human-like driving: does the escape meter fill?
            respawn.PlaceAt(RaceRoute.Surface(3540f, 900f) + Vector3.up * .4f, 345f); race.NotifyTeleported(); yield return new WaitForSecondsRealtime(1f);
            PoliceSetup.Mode = PoliceSetup.Game.Getaway; PoliceSetup.Two = false; PoliceSetup.Minutes = 5; PoliceSetup.Difficulty = 0;
            PoliceSession.Launch(); yield return new WaitForSecondsRealtime(.5f); var g = GetawayChase.Current;
            while (g && g.State == GetawayChase.Phase.Starting) yield return null;
            // three tries (a rematch from where the runner is): escape is possible when one gets away
            var tries = new List<string>(); bool got = false;
            for (int attempt = 0; attempt < 3 && !got; attempt++)
            {
                if (attempt > 0) { PoliceSession.Rematch(); yield return new WaitForSecondsRealtime(.5f); g = GetawayChase.Current; while (g && g.State == GetawayChase.Phase.Starting) yield return null; }
                var e = new Escapee { hide = true }; float t0 = Time.time; float bestEscape = 0;
                while (g && g.State == GetawayChase.Phase.Running && Time.time - t0 < 150f) { RunnerStep(e, car, Time.deltaTime); yield return null; if (g.Runners.Count > 0) bestEscape = Mathf.Max(bestEscape, g.Runners[0].escape); }
                StopRunner(car); var r = g ? g.Runners.FirstOrDefault() : null; got = r != null && r.escaped && !r.limit;
                tries.Add(r == null ? "no runner" : got ? $"ESCAPED after {PoliceKit.Mss(r.freeSeconds)} (top heat {r.topHeat}, hid {e.hides} times)" : r.caught ? $"caught after {PoliceKit.Mss(r.freeSeconds)} (escape meter best {bestEscape:0.00})" : $"still chased after {PoliceKit.Mss(g.Clock)} (escape meter best {bestEscape:0.00})");
                yield return new WaitForSecondsRealtime(4f);
            }
            Check("Getaway on Easy: a human-like runner on the roads can escape (hiding off the road once out of sight; up to 3 tries)", got, string.Join("; ", tries));
            yield return new WaitForSecondsRealtime(4f); PoliceSession.Stop();
            // underground: a chase, then down into the network (BF, the main entrance west of Bells Ferry Road): out of every cop's sight, they cannot follow
            respawn.PlaceAt(RaceRoute.Surface(1395f, 1160f) + Vector3.up * .4f, 41f); race.NotifyTeleported(); yield return new WaitForSecondsRealtime(1f);
            PoliceSetup.Difficulty = 1; PoliceSession.Launch(); yield return new WaitForSecondsRealtime(.5f); g = GetawayChase.Current;
            while (g && g.State == GetawayChase.Phase.Starting) yield return null;
            yield return new WaitForSecondsRealtime(4f);
            // the runner in the pipe network (as having driven in at BF's mouth): placed on J1's floor (the pipe warp's places)
            var pw = PipeWarp.Instance; int wi = 0; if (pw) for (int k = 0; k < pw.Count; k++) if (pw.names[k].StartsWith("J1")) { wi = k; break; }
            if (!pw || pw.Count == 0) { Check("the pipe network's places are in the build", false); yield break; }
            respawn.PlaceAt(pw.at[wi] + Vector3.up * .3f, pw.yaw[wi]); race.NotifyTeleported();
            float t1 = Time.time; bool under = false;
            while (g && g.State == GetawayChase.Phase.Running && Time.time - t1 < 60f) { yield return null; if (g.Runners.Count > 0 && g.Runners[0].underground) under = true; }
            var ru = g ? g.Runners.FirstOrDefault() : null;
            Check("Getaway: down in the pipe network the cops cannot follow and the runner escapes", under && ru != null && ru.escaped, ru == null ? "no runner" : $"underground {under}, {(ru.escaped ? $"ESCAPED {Time.time - t1:0} s after going down" : ru.caught ? "CAUGHT" : "still chased")}; radio: {string.Join(" | ", g ? g.RadioLog.Skip(Mathf.Max(0, g.RadioLog.Count - 4)) : new List<string>())}");
            yield return new WaitForSecondsRealtime(4f); PoliceSession.Stop();
        }

        IEnumerator R39Patrol()
        {
            report.AppendLine(); report.AppendLine($"=== Part D: Speed Patrol (traffic {(Traffic.Instance ? Traffic.LevelNames[Traffic.Instance.Level] : "none")})");
            if (race.State != RaceController.Stage.Free) race.EndRace();
            yield return Vehicle("original");
            respawn.PlaceAt(RaceRoute.Surface(3540f, 900f) + Vector3.up * .4f, 345f); race.NotifyTeleported(); yield return new WaitForSecondsRealtime(3f);
            PoliceSetup.Mode = PoliceSetup.Game.SpeedPatrol; PoliceSetup.Two = false; PoliceSetup.Minutes = 3; PoliceSetup.CopVehicle = "police";
            PoliceSession.Launch(); yield return new WaitForSecondsRealtime(.5f); var sp = SpeedPatrol.Current;
            if (!sp) { Check("Speed Patrol starts", false); yield break; }
            Check("Speed Patrol: the player's vehicle is the Patrol Car", car.GetComponent<VehicleConfiguration>().profileId == "police", car.GetComponent<VehicleConfiguration>().profileId);
            while (sp.State == SpeedPatrol.Phase.Starting) yield return null;
            yield return new WaitForSecondsRealtime(2f);
            bool forced = sp.ForceSpeeder(1, 25); var s = sp.Speeders.LastOrDefault();
            if (!forced || s == null) { Check("Speed Patrol: a speeder", false, "no traffic car near"); yield break; }
            // after it: the radar on it, the lights within 30 m, close behind until it pulls over
            var vi = car.GetComponent<VehicleInput>(); vi.Scripted = true; float t0 = Time.time; bool lights = false, shot = false;
            while (Time.time - t0 < 90f && !s.caught && !s.gone && s.Car)
            {
                var to = Flat(s.Car.Body.position - car.Body.position); float d = to.magnitude; float ang = Vector3.SignedAngle(Flat(car.transform.forward), to, Vector3.up);
                if (d > 25f && Time.time - t0 > 1f && Mathf.Abs(ang) < 60f) { } // chase
                vi.sSteer = Mathf.Clamp(ang / 20f, -1f, 1f); vi.sThrottle = d > 18f ? 1f : 0.35f; vi.sBrake = d < 11f ? 1f : 0f;   // close behind, not into it (a ram halves the points)
                if (d > 120f) { respawn.PlaceAt(s.Car.Body.position - s.Car.transform.forward * 25f + Vector3.up * .5f, s.Car.transform.eulerAngles.y); race.NotifyTeleported(); }
                if (s.clocked && !lights && d < 28f) { lights = true; SpeedPatrol.ScriptedToggle1 = true; }
                if (s.pursuit && !shot) { shot = true; yield return Capture("39_patrol_pursuit"); }
                yield return null;
            }
            StopRunner(car); var c = sp.CopOf(1);
            Check("Speed Patrol: a speeder clocked by the radar, chased with the lights and pulled over: points", s.caught && c.points > 0, $"clocked {s.clocked} ({s.clockedOver} over), pursuit {s.pursuit}, caught {s.caught}, points {c.points}, catches {c.catches}");
            yield return Capture("39_patrol_caught");
            PoliceSession.Stop(); yield return new WaitForSecondsRealtime(1f);
            Check("Speed Patrol over: the player's own vehicle is back", car.GetComponent<VehicleConfiguration>().profileId == "original", car.GetComponent<VehicleConfiguration>().profileId);
        }

        IEnumerator R39Hidden()
        {
            report.AppendLine(); report.AppendLine("=== Part D: a hidden cruiser (Free Roam, -hiddenPolice)");
            if (race.State != RaceController.Stage.Free) race.EndRace();
            yield return Vehicle("fastback");
            var hp = HiddenPolice.Current; if (!hp) { Check("hidden police are in the build", false); yield break; }
            report.AppendLine($"  {hp.All.Count} hiding places: {string.Join(", ", hp.All.Select(s => $"{s.road} ({s.pos.x:0}, {s.pos.z:0}){(s.fast ? "" : " street")}"))}; active {hp.Active.Count}");
            hp.LastEnd = -999; hp.AttachedAt = -999;
            var spot = hp.Active.FirstOrDefault(s => s.fast) ?? hp.Active.FirstOrDefault(); if (spot == null) { Check("a hidden cruiser placed", false); yield break; }
            var net = RoadNet.Shared; int node = net.Nearest(spot.pos, out _, 60); var t = net.Tangent(node);
            // along the road past it: 80 m before its node to 60 m after, on the road's line there (a path over the network turned off onto other roads)
            var c0 = net.P[node]; var start = RaceRoute.Surface(c0.x - t.x * 80f, c0.z - t.z * 80f); var end = RaceRoute.Surface(c0.x + t.x * 60f, c0.z + t.z * 60f);
            var path = Straight(start, end, 3f).Select(q => RaceRoute.Surface(q.x, q.z)).ToList();
            respawn.PlaceAt(start + Vector3.up * .5f, Mathf.Atan2(t.x, t.z) * Mathf.Rad2Deg); race.NotifyTeleported(); yield return new WaitForSecondsRealtime(2f);
            int limit = PoliceKit.LimitAt(spot.pos, out _); report.AppendLine($"  the cruiser on {spot.road} at ({spot.pos.x:0}, {spot.pos.z:0}), the limit there {limit} mph; {hp.DebugLine}");
            yield return Still("39_hidden_cruiser", spot.pos - spot.forward * 14f + t * 10f + Vector3.up * 3f, spot.pos + Vector3.up, 60f);
            var run = new Run(); bool tripped = false; float speedAt = 0;
            if (path != null && path.Count > 2)
            {
                Place(path[0], path[1] - path[0], PoliceKit.Mps(limit + 22));
                var drive = StartCoroutine(DriveLine(path, PoliceKit.Mps(limit + 24), 25f, run, null, r2 => GetawayChase.Current != null));
                float tq = Time.realtimeSinceStartup; while (!tripped && !run.reached && run.end == "" && run.time < 24.9f && Time.realtimeSinceStartup - tq < 40f) { yield return null; if (GetawayChase.Current && !tripped) { tripped = true; speedAt = PoliceKit.Mph(car.Body.linearVelocity.magnitude); yield return Capture("39_hidden_clocked"); } }
                yield return drive;
            }
            var g = GetawayChase.Current;
            Check("a pass at 10+ mph over the limit in front of a hidden cruiser starts a chase (heat 1, the cruiser pulls out)", tripped && g && g.Embedded, $"{(tripped ? $"CLOCKED at {speedAt:0} mph in a {limit}: {HiddenPolice.Flash}" : "not tripped")}; {hp.DebugLine}");
            yield return new WaitForSecondsRealtime(3f); if (g) yield return Capture("39_hidden_chase");
            GetawayChase.End(); HiddenPolice.Ended(false);
        }

        IEnumerator R39Chase2()
        {
            report.AppendLine(); report.AppendLine("=== Part D: Cop vs Runner for two (split screen, two virtual controllers)");
            if (race.State != RaceController.Stage.Free) race.EndRace();
            yield return Vehicle("original"); Pads();
            respawn.PlaceAt(RaceRoute.Surface(1560f, 1250f) + Vector3.up * .4f, 0f); race.NotifyTeleported(); yield return new WaitForSecondsRealtime(1f);
            SplitScreen.End(); SplitScreen.P1Device = pad1; SplitScreen.P2Device = pad2; SplitScreen.P2Ai = false; SplitScreen.P2Vehicle = "scrambler"; GameSettings.Settings.player2Name = "KYLE";
            PoliceSetup.Mode = PoliceSetup.Game.CopVsRunner; PoliceSetup.Two = true; PoliceSetup.Minutes = 3; PoliceSetup.CopFirst = 1; PoliceSetup.CopVehicle = "police";
            PoliceSession.Launch(); float w = 0; while (!PoliceChase.Current && w < 5f) { w += Time.unscaledDeltaTime; yield return null; }
            var pc = PoliceChase.Current; if (!pc || !SplitScreen.Active) { Check("Cop vs Runner for two starts in split screen", false); yield break; }
            for (int round = 0; round < 2 && pc; round++)
            {
                while (pc.State == PoliceChase.Phase.Starting) { Set(pad1, 0, 0, 0); Set(pad2, 0, 0, 0); yield return null; }
                if (round == 0) yield return Capture("39_chase2_start");
                var copPad = pc.Cop == 1 ? pad1 : pad2; var runPad = pc.Cop == 1 ? pad2 : pad1; var cop = pc.CarOf(pc.Cop); var runner = pc.CarOf(pc.Runner);
                Check($"round {round + 1}: player {pc.Cop} is the cop in the Patrol Car, player {pc.Runner} runs", cop.GetComponent<VehicleConfiguration>().profileId == "police", cop.GetComponent<VehicleConfiguration>().profileId);
                float t0 = Time.time; bool shot = false;
                while (pc && pc.State == PoliceChase.Phase.Running && Time.time - t0 < 90f)
                {
                    // the runner gives up after a short run (stops); the cop drives up behind it and stays close
                    Set(runPad, 0, Time.time - t0 < 3f ? 1f : 0f, Time.time - t0 < 3f ? 0f : 1f);
                    var to = Flat(runner.Body.position - cop.Body.position); float d = to.magnitude, ang = Vector3.SignedAngle(Flat(cop.transform.forward), to, Vector3.up);
                    float want = Mathf.Clamp(d * .6f, 0f, 32f), v = cop.ForwardSpeed;   // close in fast when far, ease down near, stop beside it
                    Set(copPad, Mathf.Clamp(ang / 20f, -1f, 1f), v < want ? 1f : 0f, v > want + 3f || d < 7f ? 1f : 0f);
                    if (!shot && d < 25f) { shot = true; yield return Capture($"39_chase2_round{round + 1}_closing"); }
                    yield return null;
                }
                Set(pad1, 0, 0, 0); Set(pad2, 0, 0, 0);
                Check($"round {round + 1}: the cop catches the runner (the bust meter)", pc && pc.Runs.Count == round + 1 && pc.Runs[round].caught, pc && pc.Runs.Count > round ? pc.Line(pc.Runs[round]) : "no result");
                float wait = 0; while (pc && pc.Runs.Count == round + 1 && pc.State != PoliceChase.Phase.Starting && pc.State != PoliceChase.Phase.Done && wait < 8f) { wait += Time.unscaledDeltaTime; yield return null; }
            }
            Check("two rounds, the roles swapped", pc && pc.Runs.Count == 2 && pc.Runs[0].runner != pc.Runs[1].runner, pc ? $"{pc.Runs.Count} rounds: {string.Join("; ", pc.Runs.Select(r => pc.Line(r)))}" : "-");
            PoliceSession.Stop(); yield return new WaitForSecondsRealtime(1f);
            Check("after the chase: split screen ended, player 1's own vehicle back", !SplitScreen.Active && car.GetComponent<VehicleConfiguration>().profileId == "original", $"split {SplitScreen.Active}, {car.GetComponent<VehicleConfiguration>().profileId}");
        }

        IEnumerator R39Radio()
        {
            report.AppendLine(); report.AppendLine("=== Part D: the police radio (Tools/Audio/police-radio-lines.csv; Dan's voice to come)");
            report.AppendLine($"  the script: {(PoliceRadio.Has("R19") ? "loaded" : "MISSING")}; e.g. \"{PoliceRadio.TextOf("P11", PoliceRadio.StreetId("Canton Road Northeast"))}\", \"{PoliceRadio.TextOf("U3", "P08", PoliceRadio.StreetId("New Chastain Road"))}\", \"{PoliceRadio.TextOf("P06", PoliceRadio.StreetId("Chastain Corner Northeast"), "P07")}\"");
            int n0 = PoliceRadioVoice.Played.Count;
            PoliceRadio.Speak(new[] { "R01" }, true); yield return new WaitForSecondsRealtime(1.5f);
            var played = PoliceRadioVoice.Played.Skip(n0).ToList();
            bool dropInHas = Directory.Exists(PoliceRadioVoice.DropIn) && Directory.GetFiles(PoliceRadioVoice.DropIn, "*.wav").Length > 0;
            report.AppendLine($"  Dan's drop-in folder {(dropInHas ? "has clips" : "has none yet")} ({PoliceRadioVoice.DropIn}); the built-in folder {(Directory.Exists(PoliceRadioVoice.BuiltIn) ? "is there" : "is not there")}");
            Check("with no clip recorded, a radio call is text only (nothing played)", dropInHas || played.All(p => p.Contains("text only")), string.Join(" | ", played));
            // a test clip (a tone, written as R01.wav in the check's own drop-in folder), played once with the sound on, then off
            var dir = Path.Combine(outputDir, "PoliceRadioTest"); Directory.CreateDirectory(dir); WriteToneWav(Path.Combine(dir, "R02.wav"));   // R02: the radio does not repeat R01 within 6 s
            PoliceRadioVoice.DropInOverride = dir; PoliceRadioVoice.Instance?.ClearCache(); CheckRun.Hear(true);
            int n1 = PoliceRadioVoice.Played.Count; PoliceRadio.Speak(new[] { "R02" }, true); yield return new WaitForSecondsRealtime(.4f); bool speaking = PoliceRadioVoice.Speaking; yield return new WaitForSecondsRealtime(2.5f);
            CheckRun.Hear(false); PoliceRadioVoice.DropInOverride = null; PoliceRadioVoice.Instance?.ClearCache();
            var p2 = PoliceRadioVoice.Played.Skip(n1).ToList();
            Check("a clip in the drop-in folder is played (the radio effect on it, the music ducked)", p2.Any(p => p.Contains("drop-in")) && speaking, string.Join(" | ", p2));
        }
        static void WriteToneWav(string path)
        {
            int rate = 24000, n = rate; var data = new short[n]; for (int i = 0; i < n; i++) data[i] = (short)(Mathf.Sin(i * 2 * Mathf.PI * 440 / rate) * 9000 * Mathf.Clamp01(Mathf.Min(i, n - i) / 800f));
            using var w = new BinaryWriter(File.Create(path)); w.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); w.Write(36 + n * 2); w.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt ")); w.Write(16); w.Write((short)1); w.Write((short)1); w.Write(rate); w.Write(rate * 2); w.Write((short)2); w.Write((short)16);
            w.Write(System.Text.Encoding.ASCII.GetBytes("data")); w.Write(n * 2); foreach (var s in data) w.Write(s);
        }

        IEnumerator R39PoliceFps()
        {
            report.AppendLine(); report.AppendLine($"=== Part D: the frame rate in a heat-5 chase at Canton Road ({Screen.width} x {Screen.height}, not measured at 3840 x 2160)");
            if (race.State != RaceController.Stage.Free) race.EndRace();
            yield return Vehicle("moto");
            int vs = QualitySettings.vSyncCount, cap = Application.targetFrameRate; QualitySettings.vSyncCount = 0; Application.targetFrameRate = -1;
            respawn.PlaceAt(RaceRoute.Surface(3502.8f, 1166.1f) + Vector3.up * .4f, 167f); race.NotifyTeleported(); yield return new WaitForSecondsRealtime(2f);
            // before: the same drive with no chase
            var e0 = new Escapee(); var f0 = new List<float>(); float t0 = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - t0 < 10f) { RunnerStep(e0, car, Time.deltaTime); yield return null; f0.Add(Time.unscaledDeltaTime); }
            StopRunner(car); respawn.PlaceAt(RaceRoute.Surface(3502.8f, 1166.1f) + Vector3.up * .4f, 167f); race.NotifyTeleported(); yield return new WaitForSecondsRealtime(1f);
            PoliceSetup.Mode = PoliceSetup.Game.Getaway; PoliceSetup.Two = false; PoliceSetup.Difficulty = 2; PoliceSetup.Minutes = 8;
            PoliceSession.Launch(); yield return new WaitForSecondsRealtime(.5f); var g = GetawayChase.Current; while (g && g.State == GetawayChase.Phase.Starting) yield return null;
            for (int k = 0; k < 4; k++) g.BumpHeat("check");
            var e = new Escapee(); float tw = Time.realtimeSinceStartup; while (Time.realtimeSinceStartup - tw < 12f && g && g.State == GetawayChase.Phase.Running) { RunnerStep(e, car, Time.deltaTime); yield return null; }   // heat 5: backups placed, the helicopter
            var f1 = new List<float>(); t0 = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - t0 < 10f && g && g.State == GetawayChase.Phase.Running) { RunnerStep(e, car, Time.deltaTime); yield return null; f1.Add(Time.unscaledDeltaTime); }
            StopRunner(car);
            string Fps(List<float> f) { if (f.Count < 10) return "too few frames (the chase ended)"; var s = f.OrderBy(x => x).ToList(); float p99 = s[Mathf.Min(s.Count - 1, (int)(s.Count * .99f))]; return $"{f.Count / f.Sum():0} fps average, slowest 1 % {1f / p99:0} fps"; }
            report.AppendLine($"INFO  before (no chase): {Fps(f0)}");
            report.AppendLine($"INFO  heat {(g ? g.Heat : 0)} chase: {Fps(f1)}; {(g ? g.Cops.Count(c => !c.block) : 0)} cops, {(g ? g.Blocks.Count : 0)} roadblocks, helicopter {(g && g.Heli)}");
            QualitySettings.vSyncCount = vs; Application.targetFrameRate = cap; PoliceSession.Stop();
        }
    }
}
