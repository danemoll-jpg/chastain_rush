using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Racer;
using UnityEngine;

namespace ChastainRush
{
    // Round 27 (D5) in the exe: -exeRound27 <dir> -r27Only race [-r27Laps n] [-r27Minutes m]. The underground race with 3 AI drivers, the
    // player parked on the hub's floor after GO (the AI race it on their own). For the check only, a TEST switch gate stands across the lap on the hub's
    // mid ledge (north side), its plate on the lap 30 m before it (the race itself has no switch shortcut: see TODO.md): the first racer
    // over the plate must open it, and the AI must then get through. Reports each AI driver's laps, finish, missed checkpoints, resets
    // and where it was slowest; the frame rate while the race runs; pictures at the grid, at the test gate and in the pipes.
    public sealed partial class Round27Runner
    {
        IEnumerator Race()
        {
            report.AppendLine("== D5 the underground race (3 AI, the player parked)");
            var race = RaceController.Instance;
            var car = FindAnyObjectByType<ArcadeVehicle>();
            if (!race || !car) { Check("a race controller and the player", false); yield break; }
            int laps = int.TryParse(Arg("-r27Laps"), out var l) ? l : 3;
            float minutes = float.TryParse(Arg("-r27Minutes"), out var m) ? m : 9f;
            RaceController.RouteOverride = "underground.json"; RaceController.DirectionOverride = "forward"; RaceController.LapsOverride = laps;
            RaceController.AiCount = 3; RaceController.AiVehiclesOverride = null;
            if (race.State != RaceController.Stage.Free) race.EndRace();
            race.Reload();
            Check("the Underground route loads", race.Route != null && race.Route.Name == "Underground" && race.Route.Underground, race.Route != null ? $"{race.Route.Name}, lap {race.Route.LapLength:0} m, {race.Route.Checkpoints.Count} checkpoints" : "none");
            // Round 28: the check's TEST switch gate is gone (the route has its own three, S1 to S3: Round28Runner)
            race.StartRace();
            var gates = race.Barriers ? race.Barriers.Gates : null;
            Check("the race layer is built: seals, the switch gates", race.Barriers && race.Barriers.Seals >= 10 && gates != null && gates.Count == 3, race.Barriers ? $"{race.Barriers.Seals} seals, {gates?.Count} switch gates, {race.Barriers.MouthsClosed.Count} mouths closed" : "");
            while (race.State == RaceController.Stage.Countdown) yield return null;
            yield return new WaitForSecondsRealtime(1.5f);
            yield return Shot("d5_grid");
            // the parked player out of the way: its grid spot is on the lap's last corner (a racer leaves it at GO; a parked one blocks it)
            var respawn = car.GetComponent<VehicleRespawn>();
            yield return new WaitForSecondsRealtime(2.5f);
            if (respawn) { respawn.PlaceAt(new Vector3(1870f, 252.5f, 1145f), 90f); race.NotifyTeleported(); }
            var ai = race.Entrants.Where(e => !e.IsPlayer).ToList();
            Check("3 AI drivers on the grid, under the ground", ai.Count == 3 && ai.All(e => e.Car.Body.position.y < 280f), string.Join(", ", ai.Select(e => $"{e.Name} {e.VehicleName} y {e.Car.Body.position.y:0.0}")));
            frames.Clear(); measuring = true;
            float t0 = Time.time, end = t0 + minutes * 60f, nextShot = t0 + 25f; int shots = 0;
            var slowest = ai.ToDictionary(e => e, e => (v: 99f, at: Vector3.zero));
            while (Time.time < end && ai.Any(e => !e.Finished))
            {
                yield return new WaitForSecondsRealtime(0.25f);
                foreach (var e in ai)
                {
                    if (e.Finished || !e.ClockStarted) continue;
                    float v = e.Car.ForwardSpeed; if (v < slowest[e].v && Time.time > t0 + 8f) slowest[e] = (v, e.Car.Body.position);
                }
                if (Time.time >= nextShot && shots < 4)
                {
                    nextShot = Time.time + 40f; shots++;
                    yield return Shot($"d5_race_{shots}");
                }
            }
            measuring = false;
            foreach (var e in ai)
            {
                var drv = e.Driver;
                Check($"{e.Name} ({e.VehicleName}) finishes the {laps} laps", e.Finished,
                      $"laps {e.CompletedLaps}, {(e.Finished ? $"total {e.FinalTotal:0.0} s" : "not finished")}, missed {e.MissedTotal}, laps {string.Join(" / ", e.LapTimes.Select(x => x.ToString("0.0")))}, AI resets {(drv ? drv.RecoveryCount : -1)}, slowest {slowest[e].v:0.0} m/s at ({slowest[e].at.x:0}, {slowest[e].at.y:0}, {slowest[e].at.z:0})");
            }
            if (frames.Count > 30)
            {
                float sum = frames.Sum(); var sorted = frames.OrderBy(x => x).ToList(); float p99 = sorted[Mathf.Min(sorted.Count - 1, (int)(sorted.Count * 0.99f))];
                report.AppendLine($"INFO  frame rate during the race (the camera on the grid in the hub, 3 AI racing): {frames.Count / sum:0} fps average, slowest 1 % {1f / p99:0} fps ({Screen.width} x {Screen.height}, vSync {QualitySettings.vSyncCount}; not measured at 3840 x 2160)");
            }
            race.EndRace();
        }

        readonly List<float> frames = new List<float>();
        bool measuring;
        void Update() { if (measuring) frames.Add(Time.unscaledDeltaTime); }
    }
}
