using System.Collections;
using System.IO;
using System.Linq;
using Racer;
using UnityEngine;

namespace ChastainRush
{
    // Round 43 Parts B to D in the exe (-exeRound43 <dir> -r43Only ghost,winner,trailer -logFile <dir>\Player.log):
    //   ghost    B1: the Ghost on; two laps of the Chastain Glen loop (Street Classic, solo): the first makes the ghost, the second is
    //            raced against it (a picture with it in view); then a race with a reset in its lap: no ghost from that lap
    //   winner   C1: a 1-lap Chastain Glen race with 3 AI drivers: the player wins (a picture of the winner shot, then the results), then
    //            one an AI wins
    //   trailer  D1: Trailer Mode in Free Roam and in a race: the free camera moved (pictures), the HUD gone, a screenshot to the
    //            Screenshots folder, slow motion and back, the race marked as not counting
    public sealed partial class Round20Runner
    {
        IEnumerator StartGlen(int laps, int ai)
        {
            RaceController.RouteOverride = "chastain_glen_loop.json"; RaceController.DirectionOverride = "forward"; RaceController.LapsOverride = laps; RaceController.AiCount = ai;
            if (race.State != RaceController.Stage.Free) race.EndRace();
            race.Reload(); race.StartRace();
            while (race.State == RaceController.Stage.Countdown) yield return null;
        }

        IEnumerator R43Ghost()
        {
            report.AppendLine(); report.AppendLine("=== B1: THE CLEAN-LAP GHOST (Chastain Glen loop, forward, Street Classic, solo)");
            yield return Vehicle("original");
            yield return StartGlen(3, 0);
            var ghost = race.Ghost; if (!ghost) { Check("B1: the ghost is on the race", false); yield break; }
            if (!ghost.Enabled) ghost.Toggle();
            report.AppendLine($"  ghost on: {ghost.Enabled} (files: {ghost.Root})");
            float L = race.Route.LapLength, g0 = race.Route.GateS;
            var line = LapSlice(g0 - 25f, g0 + 2f * L + 15f);
            Place(line[0], line[2] - line[0], 8f); yield return new WaitForFixedUpdate();
            // lap 1 at 22 m/s makes the ghost; lap 2 a little slower, so the ghost draws ahead and is seen apart from the car
            var run = new Run(); bool shot = false, sawGhost = false; string afterLap1 = null; float farthest = 0f;
            yield return DriveLine(line, 22f, 200f, run, null, r => race.HumanEntrant(1).CompletedLaps >= 1, 6f);
            afterLap1 = ghost.Status;
            float st = race.Route.LapStationOf(car.Body.position);
            var line2 = LapSlice(g0 + st, g0 + L + 15f);
            run = new Run();
            yield return DriveLine(line2, 16f, 200f, run, null, r =>
            {
                if (ghost.Showing)
                {
                    sawGhost = true;
                    var gv = GameObject.Find("Personal best clean-lap ghost / visual only");
                    if (gv) { float d = Vector3.Distance(gv.transform.position, car.Body.position); farthest = Mathf.Max(farthest, d); if (!shot && d > 6f && d < 22f) { shot = true; StartCoroutine(Capture("43_racing_the_ghost")); } }
                }
                return race.HumanEntrant(1).CompletedLaps >= 2;
            }, 6f);
            float nearest = farthest;
            var e = race.HumanEntrant(1); var files = Directory.Exists(ghost.Root) ? Directory.GetFiles(ghost.Root, "*.json") : new string[0];
            report.AppendLine($"  laps {string.Join(", ", e.LapTimes.Select(x => x.ToString("0.00")))} (missed {e.MissedTotal}); after lap 1: \"{afterLap1}\"; ghost files {files.Length}; ghost seen on lap 2 {sawGhost}, up to {nearest:0.0} m ahead; picture {shot}");
            Check("B1: the first clean lap makes a ghost; the second lap is raced against it (shown)", files.Length >= 1 && afterLap1 != null && afterLap1.StartsWith("NEW CLEAN-LAP GHOST") && sawGhost);
            Check("B1: the ghost is never solid (no collider on it)", GameObject.Find("Personal best clean-lap ghost / visual only") == null || GameObject.Find("Personal best clean-lap ghost / visual only").GetComponentsInChildren<Collider>(true).Length == 0);
            race.EndRace(); yield return new WaitForSecondsRealtime(.4f);

            // a lap with a reset in it makes no ghost (and leaves the saved one as it was)
            string before = files.Length > 0 ? File.ReadAllText(files[0]) : "";
            yield return StartGlen(1, 0);
            line = LapSlice(g0 - 25f, g0 + L + 15f);
            Place(line[0], line[2] - line[0], 8f); yield return new WaitForFixedUpdate();
            bool reset = false; run = new Run();
            yield return DriveLine(line, 26f, 200f, run, null, r =>
            {
                if (!reset && race.HumanEntrant(1).ClockStarted && race.Route.LapStationOf(car.Body.position) > .4f * L && race.Route.LapStationOf(car.Body.position) < .5f * L) { reset = ResetNow(out _, out _); }
                return race.HumanEntrant(1).Finished;
            }, 6f);
            string after = files.Length > 0 ? File.ReadAllText(files[0]) : "";
            report.AppendLine($"  the race with a reset: reset {reset}, finished {race.HumanEntrant(1).Finished} ({race.HumanEntrant(1).LapTimes.FirstOrDefault():0.00} s); ghost: \"{ghost.Status}\"; saved ghost unchanged {before == after}");
            Check("B1: a lap with a reset makes no ghost", reset && race.HumanEntrant(1).Finished && before == after && ghost.Status.StartsWith("Lap left out"));
            race.EndRace(); RaceController.LapsOverride = 0; yield return new WaitForSecondsRealtime(.4f);
        }

        // the player put on the race's last stretch with every checkpoint passed: crossing the line ends their race
        void LastStretch()
        {
            var e = race.HumanEntrant(1);
            e.ClockStarted = true; e.StartAt = e.LapStart = 0f; e.LapAway = true;   // its clock from GO, as if it had driven the lap e.NextCheckpoint = race.Route.Checkpoints.Count; e.CompletedLaps = race.Route.Laps - 1;
        }
        IEnumerator R43Winner()
        {
            report.AppendLine(); report.AppendLine("=== C1: THE WINNER SHOT (Chastain Glen loop, 1 lap, 3 AI drivers)");
            yield return Vehicle("original");
            float g0 = 0f;
            foreach (bool playerWins in new[] { true, false })
            {
                yield return StartGlen(1, 3); g0 = race.Route.GateS;
                if (!playerWins)
                {
                    float t = 0f; while (!race.Entrants.Any(x => !x.IsPlayer && x.Finished) && t < 240f) { yield return new WaitForSecondsRealtime(.5f); t += .5f; }
                    report.AppendLine($"  first AI over the line after {t:0} s: {race.Entrants.FirstOrDefault(x => !x.IsPlayer && x.Finished)?.Name ?? "none"}");
                }
                LastStretch();
                var line = LapSlice(g0 - 40f, g0 + 25f);
                Place(line[0], line[2] - line[0], 12f); yield return new WaitForFixedUpdate();
                int shots0 = WinnerShot.Shots; var run = new Run();
                yield return DriveLine(line, 16f, 20f, run, null, r => race.State == RaceController.Stage.Finished, 5f);
                bool began = WinnerShot.Shots > shots0 && WinnerShot.Active; string who = WinnerShot.LastWinner;
                yield return new WaitForSecondsRealtime(1.0f);
                string tag = playerWins ? "player" : "ai";
                yield return Capture($"43_winner_shot_{tag}_wins");
                float waited = 0f; while (WinnerShot.Active && waited < 5f) { yield return new WaitForSecondsRealtime(.1f); waited += .1f; }
                yield return new WaitForSecondsRealtime(.6f);
                yield return Capture($"43_results_after_the_shot_{tag}_wins");
                var winner = race.Entrants.Where(x => x.Finished).OrderBy(x => x.FinalTotal).FirstOrDefault();
                bool ok = began && who == winner?.Name && (playerWins ? winner.IsPlayer : !winner.IsPlayer) && !WinnerShot.Active;
                report.AppendLine($"  {(playerWins ? "the player wins" : "an AI wins")}: the shot began {began}, on {who} (winner {winner?.Name}), over after {1f + waited:0.0} s, then the results");
                Check($"C1: {(playerWins ? "the player wins" : "an AI wins")}: the winner shot on the winner, then the results", ok);
                race.EndRace(); yield return new WaitForSecondsRealtime(.4f);
            }
            RaceController.LapsOverride = 0; RaceController.AiCount = 0;
        }

        IEnumerator R43Trailer()
        {
            report.AppendLine(); report.AppendLine("=== D1: TRAILER MODE (Free Roam, then a race)");
            var mode = TrailerMode.Instance; var views = Camera.main ? Camera.main.GetComponent<CameraViews>() : null;
            if (!mode || !views) { Check("D1: Trailer Mode is in the build", false); yield break; }
            if (race.State != RaceController.Stage.Free) race.EndRace();
            yield return Vehicle("drifter");
            foreach (bool inRace in new[] { false, true })
            {
                string tag = inRace ? "race" : "roam";
                if (inRace) { yield return StartGlen(3, 3); var l = LapSlice(race.Route.GateS - 25f, race.Route.GateS + 200f); Place(l[0], l[2] - l[0], 10f); StartCoroutine(DriveLine(l, 18f, 25f, new Run(), null, null, 5f)); }
                else { var a = Ground(3236f, 927f, 400f); Place(a, Vector3.forward, 0f); }
                yield return new WaitForSecondsRealtime(1.5f);
                float fixed0 = Time.fixedDeltaTime;
                mode.Begin(); yield return new WaitForSecondsRealtime(.6f);
                bool hud = TrailerMode.HudHidden;
                // the orbit and flyover cameras, then the free camera flown up and back
                views.SelectTrailerCamera(CameraViews.Shot.Orbit); yield return new WaitForSecondsRealtime(2.5f); yield return Capture($"43_trailer_{tag}_orbit");
                views.SelectTrailerCamera(CameraViews.Shot.Flyover); yield return new WaitForSecondsRealtime(2.5f); yield return Capture($"43_trailer_{tag}_flyover");
                views.SelectTrailerCamera(CameraViews.Shot.Free); yield return new WaitForSecondsRealtime(.3f);
                var p0 = Camera.main.transform.position;
                mode.TestMove = new Vector3(0f, .5f, -1f); mode.TestLook = new Vector2(30f, 5f); yield return new WaitForSecondsRealtime(1.8f); mode.TestMove = Vector3.zero;
                yield return new WaitForSecondsRealtime(.6f);
                float moved = Vector3.Distance(p0, Camera.main.transform.position);
                yield return Capture($"43_trailer_{tag}_free_look");
                int s0 = mode.ScreenshotsTaken; mode.Screenshot(); yield return new WaitForSecondsRealtime(1.5f);
                bool shotFile = mode.ScreenshotsTaken > s0 && mode.LastScreenshot != null && File.Exists(mode.LastScreenshot);
                mode.HoldSlowForTest(true); yield return new WaitForSecondsRealtime(1.0f); float slow = Time.timeScale; mode.HoldSlowForTest(false); yield return new WaitForSecondsRealtime(1.0f);
                views.SelectTrailerCamera(CameraViews.Shot.Chase);
                bool noRecords = !inRace || race.NoRecords;
                mode.End(); yield return new WaitForSecondsRealtime(.4f);
                bool back = !TrailerMode.Active && Mathf.Approximately(Time.timeScale, 1f) && Mathf.Approximately(Time.fixedDeltaTime, fixed0) && !TrailerMode.HudHidden;
                report.AppendLine($"  {(inRace ? "a race" : "Free Roam")}: HUD hidden {hud}; free camera moved {moved:0.0} m; screenshot {(shotFile ? Path.GetFileName(mode.LastScreenshot) : "NONE")}; slow motion {slow:0.00}x; race not counted {noRecords}; after: speed {Time.timeScale:0.00}, step {Time.fixedDeltaTime:0.0000} (was {fixed0:0.0000})");
                Check($"D1: Trailer Mode in {(inRace ? "a race" : "Free Roam")}: clean screen, free camera, screenshot, slow motion, all back after", hud && moved > 3f && shotFile && slow < .4f && noRecords && back);
                if (inRace) race.EndRace();
                yield return new WaitForSecondsRealtime(.4f);
            }
            RaceController.LapsOverride = 0; RaceController.AiCount = 0;
        }
    }
}
