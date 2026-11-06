using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Racer;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace ChastainRush
{
    // Round 94 in the exe (-exeRound94 <dir> [-r94Only c94unlock,a94pics,a94drive,a94ai,d94uturn] [-r94Cars ids] [-r94Planes ids]
    // [-r94Courses chastain_glen_loop.json,addison_heights.json] [-r94Air quarry_air_race.json,air_tour.json] -r20Minutes <from the work> -logFile <dir>\Player.log),
    // from the repo root, on the check's own save. CHECK IT AS DAN PLAYS IT: the garage through the virtual controller, the human-like driver / pilot.
    //   c94unlock  C: the unlock table on a copy of Dan's save (put <dir>\goose-eggs-v1.json there first: his 19 eggs): what is locked, the banner for the
    //              vehicle tier 10 already earned, the garage's padlock and "how to unlock" for each locked one (the vehicle driven never changes to it),
    //              then eggs taken up to 25: tier 25's banner names its plane and it unlocks
    //   a94pics    A, B: each new vehicle in the garage (its picture) and on the road / in the air (chase and seat), "Unlock all" on
    //   a94drive   A, B: each new car a lap of two courses by the human-like driver; each new plane a lap of two air courses by the human-like pilot
    //   a94ai      A, B: AI fields in the new cars (Chastain Glen, 2 laps) and the new planes (Quarry Air Race, 1 lap): every AI finishes
    //   d94uturn   D: Wings & Wheels' U-turn on the bike: the human-like driver, 2 laps: no barrier hit (no reset or stall at the turn), no checkpoint missed
    // The quarry / tube hops (D) are Round 93's a93hops; the planes' times Round 92's f92ai / f92car / f92plane (-exeRound93 / -exeRound92 on this build).
    public sealed partial class Round20Runner
    {
        string R94Arg(string name, string def) { var a = Environment.GetCommandLineArgs(); int i = Array.IndexOf(a, name); return i >= 0 && i + 1 < a.Length ? a[i + 1] : def; }
        static readonly string[] R94CarIds = { "copperhead", "marlow", "falke", "brawler", "meridian", "stiletto", "falker", "tempesta" };
        static readonly string[] R94PlaneIds = { "biplane", "hornet", "jet" };
        void R94Pad() { if (r70pad == null) r70pad = InputSystem.AddDevice<Gamepad>("Round94 virtual pad"); }

        // ---------------------------------------------------------------- C: the unlocks on a copy of Dan's save
        IEnumerator R94Unlock()
        {
            report.AppendLine(); report.AppendLine("=== C: the unlock table on a copy of Dan's save (goose-eggs-v1.json in the check's folder)");
            VehicleUnlocks.UnlockAll = false; if (race.State != RaceController.Stage.Free) race.EndRace();
            var h = EggHunt.Instance; if (!h) { Check("C: the egg hunt runs", false); yield break; }
            report.AppendLine($"  the save: {h.SavePath}; eggs found {h.Found}, tiers {string.Join(", ", h.TiersReached)}");
            report.AppendLine($"  the table: {VehicleUnlocks.Table}");
            var locked = VehicleProfile.All.Where(VehicleUnlocks.Locked).Select(p => p.Id).ToList();
            report.AppendLine($"  locked now: {string.Join(", ", locked.Select(Nm))}");
            bool oldOpen = VehicleProfile.All.Where(p => !VehicleUnlocks.Lockable(p)).All(p => !VehicleUnlocks.Locked(p));
            Check("C: every vehicle from before Round 94 stays unlocked", oldOpen);
            bool tableOk = !VehicleUnlocks.Locked("copperhead") == (h.Found >= 10) && VehicleUnlocks.Locked("biplane") == (h.Found < 25) && VehicleUnlocks.Locked("tempesta") == (h.Found < 250)
                           && new[] { "falke", "brawler", "meridian", "stiletto", "falker" }.All(VehicleUnlocks.Locked);
            Check("C: the save's eggs open exactly their tiers' vehicles (Dan's 19: the Copperhead V8 open, the rest locked)", tableOk, string.Join(", ", locked));
            // HIGH MODE has everything
            R84High(true); bool highAll = VehicleProfile.All.All(p => !VehicleUnlocks.Locked(p)); R84High(false);
            Check("C: HIGH MODE has every vehicle unlocked", highAll);
            // the banner for a vehicle earned before this build (shown once in Free Roam)
            yield return Vehicle("original"); yield return new WaitForSecondsRealtime(6f);
            report.AppendLine($"  the banner in Free Roam: \"{EggBanner.Last}\"; announced {VehicleUnlocks.LastAnnounced}");
            yield return Capture("94_unlock_banner_earlier");
            Check("C: the vehicle tier 10 gave is announced once in Free Roam", h.Found < 10 || EggBanner.Last.Contains("COPPERHEAD"), EggBanner.Last);
            // the garage through the controller: right through every vehicle; locked ones shown with the padlock and how to unlock, never put on
            R94Pad(); var m = GameMenus.Instance ? GameMenus.Instance : GameMenus.Create(); yield return new WaitForSecondsRealtime(1f); if (m) m.CloseForCheck();
            m.OpenPauseForCheck(); yield return new WaitForSecondsRealtime(.6f);
            yield return R70Focus(m, "garage", false); yield return R70Tap(GamepadButton.South); yield return new WaitForSecondsRealtime(.8f);
            yield return R70Focus(m, "vehicle", false);
            var seenLocked = new List<string>(); var putOn = new List<string>(); bool wrong = false;
            for (int i = 0; i < 40; i++)
            {
                yield return R70Tap(GamepadButton.DpadRight); yield return new WaitForSecondsRealtime(.35f);
                string shown = m.GarageShownId, driven = car.GetComponent<VehicleConfiguration>().profileId;
                if (VehicleUnlocks.Locked(driven)) wrong = true;
                if (shown != null && !seenLocked.Contains(shown)) { seenLocked.Add(shown); yield return new WaitForSecondsRealtime(.6f); yield return Capture($"94_garage_locked_{shown}"); }
                if (shown == null && Array.IndexOf(R94CarIds, driven) >= 0 && !putOn.Contains(driven)) { putOn.Add(driven); yield return new WaitForSecondsRealtime(.6f); yield return Capture($"94_garage_{driven}"); }
                if (driven == "original" && i > 5) break;
            }
            m.CloseForCheck(); yield return new WaitForSecondsRealtime(.4f);
            report.AppendLine($"  the garage: locked ones shown {string.Join(", ", seenLocked.Select(Nm))}; new ones put on {string.Join(", ", putOn.Select(Nm))}");
            Check("C: the garage shows every locked vehicle (padlock, how to unlock) and never puts one on", !wrong && locked.All(seenLocked.Contains), $"seen {seenLocked.Count} of {locked.Count}, driven a locked one {wrong}");
            // eggs taken up to tier 25: its banner names the plane and it unlocks
            if (h.Found < 25)
            {
                foreach (var s in h.sites) { if (h.Found >= 25) break; if (!h.Discovered(s.id)) h.Take(s); }
                yield return new WaitForSecondsRealtime(1f);
                report.AppendLine($"  eggs taken to {h.Found}: the banner \"{EggBanner.Last}\"");
                yield return Capture("94_unlock_banner_tier25");
                Check("C: tier 25's banner names the Barnstormer and it unlocks", EggBanner.Last.Contains("Barnstormer") && !VehicleUnlocks.Locked("biplane"), EggBanner.Last);
            }
            yield return Vehicle("original");
        }

        // ---------------------------------------------------------------- A, B: pictures
        IEnumerator R94Pics()
        {
            report.AppendLine(); report.AppendLine("=== A, B: each new vehicle in the garage, on the road / in the air (\"Unlock all\" on)");
            VehicleUnlocks.UnlockAll = true; if (race.State != RaceController.Stage.Free) race.EndRace();
            yield return Clock(14f); R94Pad();
            var m = GameMenus.Instance ? GameMenus.Instance : GameMenus.Create(); yield return new WaitForSecondsRealtime(1f); if (m) m.CloseForCheck();
            var cc = Camera.main ? Camera.main.GetComponent<ChaseCamera>() : null; var views = FindAnyObjectByType<CameraViews>();
            foreach (var id in R94Arg("-r94Cars", string.Join(",", R94CarIds)).Split(',').Concat(R94Arg("-r94Planes", string.Join(",", R94PlaneIds)).Split(',')))
            {
                var p = VehicleProfile.Find(id); if (p.Id != id) { Check($"pics: {id} exists", false); continue; }
                yield return Vehicle(id); yield return new WaitForSecondsRealtime(.6f);
                m.OpenPauseForCheck(); yield return new WaitForSecondsRealtime(.5f); yield return R70Focus(m, "garage", false); yield return R70Tap(GamepadButton.South); yield return new WaitForSecondsRealtime(1.6f);
                yield return Capture($"94_garage_{id}"); m.CloseForCheck(); yield return new WaitForSecondsRealtime(.4f);
                if (p.IsPlane)
                {
                    var pl = car.GetComponent<PlaneFlight>(); var at = new Vector3(1700f, 0f, -7400f); at.y = RaceRoute.Surface(at.x, at.z).y + 120f;
                    if (pl) pl.AirStart(at, 0f, 0f, 60f); yield return new WaitForSecondsRealtime(1.5f); if (cc) cc.Snap();
                }
                else
                {
                    // Chastain Glen Lane in the afternoon, on the road
                    UseRoute("chastain_glen_loop.json", "forward"); var r = race.Route; var q = r.PointAt(r.GateS + 300f, out var f); Place(RaceRoute.Surface(q.x, q.z), f, 0f);
                    yield return new WaitForSecondsRealtime(.6f); if (cc) cc.Snap();
                }
                yield return new WaitForSecondsRealtime(.4f); yield return Capture($"94_road_{id}_chase");
                if (views) { views.SetView(CameraViews.View.FirstPerson); yield return new WaitForSecondsRealtime(.4f); yield return Capture($"94_road_{id}_seat"); views.SetView(CameraViews.View.Chase); }
                var b = car.Body.position; var fw = car.transform.forward; var rt = car.transform.right;
                yield return Still($"94_road_{id}_side", b + rt * (p.IsPlane ? 11f : 5.5f) - fw * (p.IsPlane ? 6f : 2.5f) + Vector3.up * 1.6f, b + Vector3.up * .3f, 50f);
                var rider = car.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "Rider");
                report.AppendLine($"  {p.Name} ({id}): {p.Class}, top {p.Speed} m/s{(p.IsPlane ? $" (air {p.PlaneTop} m/s)" : "")}, engine {p.Engine}; the driver {(rider ? "aboard" : "MISSING")}; pictures 94_garage_{id}, 94_road_{id}_chase / _seat / _side");
                Check($"pics {p.Name}: the model and the driver in place", rider);
            }
            yield return Vehicle("original"); VehicleUnlocks.UnlockAll = false;
        }

        // ---------------------------------------------------------------- A, B: a lap of two courses each by the human-like driver / pilot
        IEnumerator R94Drive()
        {
            var courses = R94Arg("-r94Courses", "chastain_glen_loop.json,addison_heights.json").Split(',');
            report.AppendLine(); report.AppendLine($"=== A: each new car a lap of {string.Join(" and ", courses)} by the human-like driver (solo)");
            VehicleUnlocks.UnlockAll = true; yield return Clock(13f); RoadDriver.Difficulty = 1;
            var table = new System.Text.StringBuilder();
            foreach (var id in R94Arg("-r94Cars", string.Join(",", R94CarIds)).Split(','))
                foreach (var course in courses)
                {
                    yield return Vehicle(id); yield return StartRoute(course, "forward", 0, 1); var r = race.Route; if (r == null) { Check($"drive: {course} loads", false); continue; }
                    var line = new List<Vector3>(r.LeadIn ?? new Vector3[0]); for (float s = 0f; s < r.LapLength + 30f; s += 3f) line.Add(r.PointAt(r.GateS + s, out _));
                    var run = new HumanRun();
                    yield return HumanDrive(line, 60f, 420f, run, () => race.Player.Finished || run.resets > 8);
                    var pe = race.Player; string t = pe.Finished ? RaceController.Fmt(pe.LapTimes[0]) : "NOT finished";
                    report.AppendLine($"  {Nm(id)} on {r.Name}: {t}, missed {pe.MissedTotal}, resets {run.resets}, stuck {run.stuck}, top {run.topSpeed:0} m/s{run.note}");
                    table.AppendLine($"    {r.Name}\t{Nm(id)}\t{t}\ttop {run.topSpeed:0} m/s");
                    Check($"A {Nm(id)}: the human-like driver finishes a lap of {r.Name}", pe.Finished && run.resets <= 2, $"{t}, resets {run.resets}, missed {pe.MissedTotal}");
                    race.EndRace(); Flush(); yield return new WaitForSecondsRealtime(.4f);
                }
            var airs = R94Arg("-r94Air", "quarry_air_race.json,air_tour.json").Split(',');
            report.AppendLine(); report.AppendLine($"=== B: each new plane a lap of {string.Join(" and ", airs)} by the human-like pilot (solo)");
            foreach (var id in R94Arg("-r94Planes", string.Join(",", R94PlaneIds)).Split(','))
                foreach (var air in airs)
                {
                    yield return Vehicle(id);
                    RaceController.RouteOverride = air; RaceController.DirectionOverride = "forward"; RaceController.LapsOverride = 1; RaceController.AiCount = 0;
                    if (race.State != RaceController.Stage.Free) race.EndRace();
                    race.Reload(); var r = race.Route; if (r == null || !r.Air) { Check($"fly: {air} loads", false); continue; }
                    race.StartRace(); R73Pad(); while (race.State == RaceController.Stage.Countdown) { R73Set(0, 0, 0); yield return null; }
                    var note = new System.Text.StringBuilder(); var pl = car.GetComponent<PlaneFlight>(); int c0 = pl ? pl.Crashes : 0;
                    yield return R80HumanFly(480f, 1f, note);
                    var pe = race.Player; string t = pe.Finished ? RaceController.Fmt(pe.LapTimes[0]) : "NOT finished";
                    report.AppendLine($"  {Nm(id)} on {r.Name}: {t}, missed {pe.MissedTotal}, crashes {(pl ? pl.Crashes - c0 : -1)};{note}");
                    table.AppendLine($"    {r.Name}\t{Nm(id)}\t{t}");
                    Check($"B {Nm(id)}: the human-like pilot finishes a lap of {r.Name}", pe.Finished, $"{t}, missed {pe.MissedTotal}");
                    race.EndRace(); Flush(); yield return new WaitForSecondsRealtime(.4f);
                }
            report.AppendLine("  lap times:"); report.Append(table);
            yield return Vehicle("original"); VehicleUnlocks.UnlockAll = false;
        }

        // ---------------------------------------------------------------- A, B: the AI in the new vehicles
        IEnumerator R94Ai()
        {
            report.AppendLine(); report.AppendLine("=== A, B: AI fields in the new vehicles (the player parked off the course), Normal");
            VehicleUnlocks.UnlockAll = true; yield return Clock(13f); int prev = RoadDriver.Difficulty; RoadDriver.Difficulty = 1;
            var fields = new (string course, int laps, string[] ids)[] {
                ("chastain_glen_loop.json", 2, new[] { "copperhead", "marlow", "falke" }), ("chastain_glen_loop.json", 2, new[] { "brawler", "meridian", "stiletto" }),
                ("chastain_glen_loop.json", 2, new[] { "falker", "tempesta", "copperhead" }), ("quarry_air_race.json", 1, new[] { "biplane", "hornet", "jet" }) };
            foreach (var f in fields)
            {
                RaceController.AiVehiclesOverride = f.ids; bool air = f.course.Contains("air");
                yield return Vehicle(air ? "plane" : "original");
                yield return StartRoute(f.course, "forward", 3, f.laps);
                var r = race.Route; if (r == null) { Check($"AI: {f.course} loads", false); continue; }
                var off = r.PointAt(r.GateS + r.LapLength * .5f, out var fw); var side = Vector3.Cross(Vector3.up, fw);
                if (!air) { respawn.PlaceAt(RaceRoute.Surface(off.x + side.x * 40f, off.z + side.z * 40f) + Vector3.up * .4f, 0f); race.NotifyTeleported(); }
                var ai = race.Entrants.Where(e => !e.IsPlayer).ToList();
                Time.timeScale = 2f; Time.maximumDeltaTime = .5f;
                float end = Time.realtimeSinceStartup + 6f * 60f;
                while (Time.realtimeSinceStartup < end && ai.Any(e => !e.Finished) && !RaceStuck) yield return new WaitForSecondsRealtime(.5f);
                Time.timeScale = 1f;
                report.AppendLine($"  {r.Name}: " + string.Join("; ", ai.Select(e => $"{e.Name} {e.VehicleName} {(e.Finished && !e.Estimated ? RaceController.Fmt(e.FinalTotal) : "NOT finished")}, laps {string.Join(" / ", e.LapTimes.Select(v => RaceController.Fmt(v)))}, missed {e.MissedTotal}")));
                Check($"AI {r.Name} in {string.Join(", ", f.ids.Select(Nm))}: every AI finishes", ai.All(e => e.Finished && !e.Estimated), string.Join(", ", ai.Where(e => !e.Finished || e.Estimated).Select(e => e.Name)));
                race.EndRace(); Flush(); yield return new WaitForSecondsRealtime(.5f);
            }
            RaceController.AiVehiclesOverride = null; RoadDriver.Difficulty = prev; VehicleUnlocks.UnlockAll = false;
        }

        // ---------------------------------------------------------------- D: the U-turn on the bike
        IEnumerator R94UTurn()
        {
            report.AppendLine(); report.AppendLine("=== D: Wings & Wheels' U-turn on the bike (the human-like driver, 2 laps): the barrier row 6 m further out round the apex");
            yield return Clock(13f); yield return Vehicle(R94Arg("-r94Bike", "moto"));
            yield return StartRoute("wings_and_wheels.json", "forward", 0, 2);
            var r = race.Route; if (r == null) { Check("D: the car's lap loads", false); yield break; }
            var t = RaceUTurns.Of(r).FirstOrDefault(); var cc = Camera.main ? Camera.main.GetComponent<ChaseCamera>() : null;
            foreach (float back in new[] { 60f, 25f })
            {
                var p = r.PointAt(r.GateS + t.s - back, out var f); Place(RaceRoute.Surface(p.x, p.z), f, 0f); yield return new WaitForSecondsRealtime(.4f); if (cc) cc.Snap(); yield return new WaitForSecondsRealtime(.4f);
                yield return Capture($"94_uturn_{back:000}m");
            }
            race.EndRace(); yield return StartRoute("wings_and_wheels.json", "forward", 0, 2); r = race.Route;
            var line = new List<Vector3>(r.LeadIn ?? new Vector3[0]);
            for (int k = 0; k < 2; k++) for (float s = 0f; s < r.LapLength; s += 3f) line.Add(r.PointAt(r.GateS + s, out _));
            line.Add(r.PointAt(r.GateS + 30f, out _));
            var run = new HumanRun(); int hits = 0; float slowAt = 0f, minV = 99f; Vector3 apex = t.apex;
            yield return HumanDrive(line, 45f, 900f, run, () =>
            {
                var q = car.Body.position; float d = Flat(q - apex).magnitude;
                if (d < 35f) { float v = car.Body.linearVelocity.magnitude; minV = Mathf.Min(minV, v); if (v < 2f) slowAt += Time.fixedDeltaTime; }
                return race.Player.Finished;
            });
            var pe = race.Player;
            hits = run.resets;
            report.AppendLine($"  the apex at ({apex.x:0.0}, {apex.z:0.0}), room {t.room:0} m; laps {string.Join(" / ", pe.LapTimes.Select(v => RaceController.Fmt(v)))}; missed {pe.MissedTotal}; resets {run.resets}, stuck {run.stuck}; slowest round the turn {minV:0.0} m/s, stopped there {slowAt:0.0} s{run.note}");
            Check("D: the bike gets round the U-turn without hitting the barrier (no stop or reset at the turn) and misses no checkpoint", pe.Finished && pe.MissedTotal == 0 && slowAt < 1f, $"missed {pe.MissedTotal}, stopped {slowAt:0.0} s, resets {run.resets}");
            race.EndRace(); Flush();
        }
    }
}
