using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ChastainRush
{
    // Round 93 (STANDING RULE, Dan 2026-11-05 05:05: "Check runs never sit stuck"; a check race's bike stood wedged on its tail against a wall for 20+ minutes):
    //   - the human-like driver resets itself (the reset button) after 5 s with no progress along its line, wheels down or not, with a picture (HumanDrive)
    //   - a race in a check where a racer makes no progress for 2 minutes of game time is ended and reported as STUCK (where, what it was against); every
    //     check loop that waits on a race stops at once (RaceStuck)
    // The player is watched once its clock has started (a player parked off the course for an AI race never starts it); every AI racer always.
    public sealed partial class Round20Runner
    {
        bool stuckFlag;
        public bool RaceStuck => stuckFlag && race != null && race.State == RaceController.Stage.Free;   // the guard ended the race (cleared by the next race or drive)
        public static float StuckEndSeconds = 120f;
        int stuckPics;

        IEnumerator RaceStuckGuard()
        {
            var refPos = new Dictionary<RaceEntrant, (Vector3 p, int cp, int laps, float t)>();
            RaceController last = null; var lastState = RaceController.Stage.Free;
            while (true)
            {
                yield return new WaitForSeconds(1f);   // (game time: a race sped up 2x is watched by its own clock)
                var rc = race; if (rc == null) continue;
                if (rc.State == RaceController.Stage.Countdown || (rc.State == RaceController.Stage.Racing && lastState == RaceController.Stage.Free) || rc != last) { refPos.Clear(); stuckFlag = false; }
                last = rc; lastState = rc.State;
                if (rc.State != RaceController.Stage.Racing) { refPos.Clear(); continue; }
                foreach (var e in rc.Entrants)
                {
                    if (e.Finished || e.Car == null || (e.IsPlayer && !e.ClockStarted)) { refPos.Remove(e); continue; }
                    var p = e.Car.Body.position;
                    if (!refPos.TryGetValue(e, out var r) || (p - r.p).magnitude > 20f || e.NextCheckpoint != r.cp || e.CompletedLaps != r.laps) { refPos[e] = (p, e.NextCheckpoint, e.CompletedLaps, Time.time); continue; }
                    if (Time.time - r.t < StuckEndSeconds) continue;
                    // STUCK: what it stands against, a picture, the race ended
                    var hits = Physics.OverlapBox(p + Vector3.up, new Vector3(3f, 2f, 3f), Quaternion.identity, ~0, QueryTriggerInteraction.Ignore)
                        .Where(c => c.attachedRigidbody != e.Car.Body).Select(c => c.name).Distinct().Take(8).ToList();
                    string where = $"({p.x:0.0}, {p.y:0.0}, {p.z:0.0}), lap station {rc.Route.LapStationOf(p):0} m, lap {e.CompletedLaps + 1}, next checkpoint {e.NextCheckpoint + 1}";
                    if (e.IsPlayer) yield return Capture($"stuck_end_{++stuckPics}");
                    Check($"STUCK: {e.Name} in {rc.Route.Name} made no progress for {StuckEndSeconds:0} s: the race ended", false, $"at {where}; against: {(hits.Count == 0 ? "nothing solid" : string.Join(", ", hits))}{(e.IsPlayer ? $"; picture stuck_end_{stuckPics}.png" : "")}");
                    stuckFlag = true; Time.timeScale = 1f; rc.EndRace(); refPos.Clear(); Flush();
                    break;
                }
            }
        }

        // the driver's own reset (HumanDrive): a picture first, then the reset button, as a player would
        IEnumerator StuckSelfReset(HumanRun run, Vector3 p, float s)
        {
            run.stuck++;
            var hits = Physics.OverlapBox(p + Vector3.up, new Vector3(2.5f, 1.5f, 2.5f), car.transform.rotation, ~0, QueryTriggerInteraction.Ignore)
                .Where(c => c.attachedRigidbody != car.Body && !(c is TerrainCollider)).Select(c => c.name).Distinct().Take(5).ToList();
            float tilt = Vector3.Angle(car.transform.up, Vector3.up);
            string pic = $"stuck_{++stuckPics}";
            run.note += $" stuck at ({p.x:0}, {p.z:0}) s {s:0} (tilted {tilt:0} deg, {car.GroundedWheels} wheels down{(hits.Count > 0 ? ", against " + string.Join(", ", hits) : "")}; {pic}.png): reset;";
            yield return Capture(pic); run.pics.Add(pic);
            if (StuckProbe != null) yield return StuckProbe(p);
            yield return R73Reset(); run.resets++;
        }
    }
}
