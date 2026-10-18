using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Racer;
using UnityEngine;

namespace ChastainRush
{
    // Round 43 Part A in the exe (-exeRound43 <dir> [-r43Only plume] -logFile <dir>\Player.log):
    //   plume  A1: a reverse Chastain Ridge race from Dan's BUG-001 spot (lap 3's state: CP3 next): the Drifter Twin and the Street Classic along
    //          the left (southeast-bound) carriageway and the median at race speed: thrown onto the dead end; both covers blow together; a reset
    //          from the left carriageway past the cover: short of the cover, facing it, no penalty; a reset mid-flight: past the landing, no
    //          penalty; pictures from Dan's spot after it blows
    public sealed partial class Round20Runner
    {
        static readonly Vector3 Bug43Cam = new Vector3(3282.25f, 313.79f, 580.35f);   // Dan's BUG-001 camera (pitch 10.17, heading 333.48)

        void CpState(int next)
        {
            var e = race.HumanEntrant(1);
            e.ClockStarted = true; e.StartAt = e.LapStart = race.RaceClock; e.NextCheckpoint = next; e.Finished = false; e.ActiveShortcut = null;
            e.PenaltyTotal = e.LapPenalty = 0f; e.MissedTotal = 0; e.Missed.Clear(); e.Forgiven.Clear();
        }
        // what R / Y does in a race (VehicleRespawn: Recover, then Place)
        bool ResetNow(out Vector3 to, out float yaw)
        {
            to = default; yaw = 0f;
            if (VehicleRespawn.Recover == null || !VehicleRespawn.Recover(respawn, out to, out yaw)) return false;
            respawn.PlaceAt(to, yaw); race.NotifyTeleported(); return true;
        }

        IEnumerator R43Plume()
        {
            report.AppendLine(); report.AppendLine("=== A1: THE PLUME ON BOTH CARRIAGEWAYS (reverse Chastain Ridge, CP3 next as in Dan's BUG-001)");
            var p1 = Plume(); var p2 = ManholePlume.All.FirstOrDefault(p => p.id == "RP2");
            if (p1 == null || p2 == null) { Check("A1: both plume covers (RP, RP2) are in the build", false); yield break; }
            var along = p1.along; var side = Vector3.Cross(Vector3.up, along).normalized;
            float lat2 = Vector3.Dot(p2.transform.position - p1.transform.position, side);
            report.AppendLine($"  covers: RP at ({p1.transform.position.x:0.0}, {p1.transform.position.z:0.0}), RP2 {-lat2:0.0} m left of it; water from {p1.right:0.0} m right of RP to {-lat2 + p2.left:0.0} m left");
            string src = null; Vector3 endAt = Vector3.zero; bool ended = false;
            System.Action<ArcadeVehicle, string, float, float> began = (c, s, u, T) => { if (c == car) src = s; };
            System.Action<ArcadeVehicle, string, Vector3, float> end = (c, s, at, T) => { if (c == car) { ended = true; endAt = at; } };
            GuidedFlight.Began += began; GuidedFlight.Ended += end;
            bool allThrown = true, bothBlew = true; bool pics = false;
            foreach (var id in new[] { "drifter", "original" })
                foreach (var (lane, off) in new[] { ("left carriageway", lat2), ("left carriageway, far edge", lat2 - 2.5f), ("median", lat2 * .5f) })
                {
                    yield return Vehicle(id);
                    yield return StartRevRace(); CpState(2);
                    var a = p1.transform.position - along * 170f + side * off; var b = p1.transform.position + along * 40f + side * off;
                    var line = Straight(Ground(a.x, a.z, p1.transform.position.y + 40f), Ground(b.x, b.z, p1.transform.position.y + 40f));
                    Place(line[0], line[2] - line[0], 20f); yield return new WaitForFixedUpdate();
                    src = null; ended = false; var run = new Run();
                    yield return DriveLine(line, 25f, 20f, run, null, r => src != null, 5f);
                    bool blew = p1.Erupting && p2.Erupting; bothBlew &= blew;
                    if (!pics && blew) { pics = true; yield return Still("43_dan_spot_after_it_blows", Bug43Cam, Bug43Cam + Quaternion.Euler(10.17f, 333.48f, 0f) * Vector3.forward * 50f); }
                    float t = 0f; while (src != null && !ended && t < 9f) { yield return new WaitForFixedUpdate(); t += Time.fixedDeltaTime; }
                    yield return new WaitForSecondsRealtime(.6f);
                    var p = car.Body.position; float dTarget = Vector2.Distance(new Vector2(p.x, p.z), new Vector2(p1.target.x, p1.target.z));
                    bool ok = src != null && ended && dTarget < 30f; allThrown &= ok;
                    report.AppendLine($"  {Nm(id),-16} {lane,-28} into it at {run.speedAt:0.0} m/s: {(src != null ? "thrown by " + src : "NOT THROWN (" + (run.reached ? "drove past" : run.end) + ")")}; " +
                                      $"down at ({p.x:0.0}, {p.y:0.0}, {p.z:0.0}), {dTarget:0} m from the landing; both covers blown {blew}");
                    if (id == "drifter" && lane == "left carriageway") yield return Capture("43_after_the_left_carriageway_throw");
                    race.EndRace(); yield return new WaitForSecondsRealtime(.3f);
                }
            Check("A1 (a): along the left carriageway, its far edge and the median at race speed, both vehicles are thrown onto the dead end", allThrown);
            Check("A1 (a): the two covers blow together", bothBlew);

            // (c) a reset from the left carriageway past the cover, and from the median beside it: short of the cover, facing it, no penalty
            yield return Vehicle("drifter");
            bool shortOk = true;
            foreach (var (where, off, past) in new[] { ("left carriageway, 30 m past the cover", lat2, 30f), ("left carriageway, 90 m past", lat2, 90f), ("median, 20 m past", lat2 * .5f, 20f), ("left carriageway, 60 m short", lat2, -60f) })
            {
                yield return StartRevRace(); CpState(2);
                var q = p1.transform.position + along * past + side * off; var g = Ground(q.x, q.z, p1.transform.position.y + 40f);
                Place(g, along, 0f); yield return new WaitForSecondsRealtime(.4f);
                int next0 = race.NextCheckpoint; float pen0 = race.PenaltyTotal;
                if (!ResetNow(out var to, out float yaw)) { shortOk = false; report.AppendLine($"  {where}: NO SPOT"); continue; }
                yield return new WaitForSecondsRealtime(.4f);
                float shortBy = Vector3.Dot(p1.transform.position - to, along); var fw = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
                float facing = Vector3.Dot(fw, (p1.transform.position - to).normalized);
                bool ok = shortBy > 5f && shortBy < 60f && facing > .8f && race.PenaltyTotal == pen0 && race.NextCheckpoint == next0;
                shortOk &= ok;
                report.AppendLine($"  reset from the {where}: to ({to.x:0.0}, {to.z:0.0}), {shortBy:0} m short of the cover, facing it {facing:0.00}; penalty {race.PenaltyTotal - pen0:+0;-0;0} s, next CP{race.NextCheckpoint + 1}");
                if (where.StartsWith("left carriageway, 30")) yield return Capture("43_reset_short_of_the_cover");
                race.EndRace(); yield return new WaitForSecondsRealtime(.3f);
            }
            Check("A1 (c, d): resets from the left carriageway and the median past the cover (and short of it): short of the cover, facing it, no penalty", shortOk);

            // (c) a reset mid-flight: past the landing, no penalty
            {
                yield return StartRevRace(); CpState(2);
                float sP = AbsStation(p1.transform.position); var line = LapSlice(sP - 150f, sP + 4f);
                Place(line[0], line[2] - line[0], 20f); yield return new WaitForFixedUpdate();
                src = null; var run = new Run();
                yield return DriveLine(line, 25f, 15f, run, null, r => src != null, 5f);
                float t = 0f; while (src != null && t < 1.2f) { yield return new WaitForFixedUpdate(); t += Time.fixedDeltaTime; }
                var fl = car.GetComponent<GuidedFlight>(); bool flying = fl && fl.Flying; float pen0 = race.PenaltyTotal; int next0 = race.NextCheckpoint;
                bool got = ResetNow(out var to, out _); yield return new WaitForSecondsRealtime(.5f);
                float sTo = AbsStation(to), sLand = AbsStation(p1.target);
                bool ok = flying && got && sTo > sLand && race.PenaltyTotal == pen0;
                report.AppendLine($"  reset mid-flight (in the air {flying}, {t:0.0} s after the throw): to ({to.x:0.0}, {to.z:0.0}), lap {sTo:0} m against the landing's {sLand:0} m; penalty {race.PenaltyTotal - pen0:+0;-0;0} s, next CP{next0 + 1} -> CP{race.NextCheckpoint + 1}");
                Check("A1 (c, d): a reset mid-flight: past the landing, no penalty", ok);
                race.EndRace(); yield return new WaitForSecondsRealtime(.3f);
            }
            GuidedFlight.Began -= began; GuidedFlight.Ended -= end;
            RaceController.DirectionOverride = null;
        }
    }
}
