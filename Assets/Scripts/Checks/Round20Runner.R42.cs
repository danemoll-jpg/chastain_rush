using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Racer;
using UnityEngine;

namespace ChastainRush
{
    // Round 42 Part B in the exe (-exeRound42 <dir> [-r42Only windows,aisle,plume] [-traffic Normal] -logFile <dir>\Player.log):
    //   windows  B1: Free Roam, the Street Classic and the Drifter Twin driven in through each of the church's front windows and on down the stair:
    //            the glass breaks, the vehicle reaches the stair; then a reset beside them: the glass is whole again
    //   aisle    B2: pictures from Dan's BUG-002 spot (his chase camera) and on the stair; the Drifter Twin driven from the doors down the stair:
    //            how far its wheels are from the drawn stair (the line's own heights)
    //   plume    B3: Free Roam past the manhole plume both ways at 30 m/s: it stays shut; a reverse Chastain Ridge race: it blows
    public sealed partial class Round20Runner
    {
        static readonly Vector3 Bug002Car = new Vector3(3525.42f, 330.14f, 577.74f);   // Dan's BUG-002: the vehicle (heading 267.8), the camera at 3532.41, 333.24, 578.01
        static readonly Vector3 Bug002Cam = new Vector3(3532.41f, 333.24f, 578.01f);
        const float ChFloorY = 330.5f, ChDoorX = 3535.0f, ChDoorZ = 576.95f;
        static readonly float[] ChWindowZ = { 572.25f, 581.65f };   // the middles of the two windows (MapBuilder.ChurchCrypt.cs ChurchWindows: 2.4 m wide, 0.7 m from the doors)

        static int WindowsBroken()
        {
            var root = GameObject.Find("Crypt church windows"); if (!root) return -1;
            return root.GetComponentsInChildren<FenceChunk>().Sum(c => c.broken == null ? 0 : c.broken.Count(b => b));
        }
        static Vector3 Ground(float x, float z, float from = 345f) => Physics.Raycast(new Vector3(x, from, z), Vector3.down, out var h, 60f, ~0, QueryTriggerInteraction.Ignore) ? h.point : new Vector3(x, ChFloorY, z);

        // in through a window at z, heading west, then onto the stair's top and down it
        List<Vector3> WindowLine(float wz)
        {
            var l = Straight(Ground(3565f, wz), Ground(3538f, wz, 334f));
            // straight in through the opening, then across to the stair's top (past the balustrade's end) and down it
            l.Add(new Vector3(3535.5f, ChFloorY, wz));
            Vector3 a = new Vector3(3534.5f, ChFloorY, wz), c0 = new Vector3(3531.5f, ChFloorY, wz), b = new Vector3(3528.5f, ChFloorY - .2f, ChDoorZ), c1 = new Vector3(3530.5f, ChFloorY, ChDoorZ);
            for (int i = 0; i <= 16; i++) { float u = i / 16f, v = 1f - u; l.Add(v * v * v * a + 3f * v * v * u * c0 + 3f * v * u * u * c1 + u * u * u * b); }   // a curve to the stair's top, past the balustrade's end
            foreach (var q in Straight(b, new Vector3(3516f, ChFloorY - 3.5f, ChDoorZ)).Skip(1)) l.Add(q);
            return l;
        }

        IEnumerator R42Windows()
        {
            report.AppendLine(); report.AppendLine("=== B1: THE CHURCH'S WINDOWS (Free Roam): in through each window beside the doors and on down the stair");
            if (race.State != RaceController.Stage.Free) race.EndRace();
            if (WindowsBroken() < 0) { Check("B1: the church's windows are in the build", false); yield break; }
            FenceChunk.RestoreAll();
            bool allIn = true, allBroke = true;
            foreach (var id in new[] { "original", "drifter" })
                foreach (var wz in ChWindowZ)
                {
                    yield return Vehicle(id); FenceChunk.RestoreAll(); yield return new WaitForFixedUpdate();
                    int b0 = WindowsBroken(); var line = WindowLine(wz);
                    Place(line[0], line[2] - line[0], 12f); yield return new WaitForFixedUpdate();
                    var run = new Run(); float lowest = float.MaxValue;
                    yield return DriveLine(line, 9f, 20f, run, null, r => { lowest = Mathf.Min(lowest, car.Body.position.y); return false; }, 2.5f);
                    int broke = WindowsBroken() - b0; var p = car.Body.position;
                    bool down = p.x < 3528f && p.y < ChFloorY - .8f;   // on the stair, below the nave's floor
                    allIn &= down; allBroke &= broke > 0;
                    report.AppendLine($"  {Nm(id),-16} the {(wz < ChDoorZ ? "south" : "north")} window: {broke} panes broken; {(run.reached ? "down the stair" : "STOPPED " + run.end)} at ({p.x:0.0}, {p.y:0.00}, {p.z:0.0}), slowest {run.minSpeed:0.0} m/s");
                    if (id == "drifter" && wz < ChDoorZ) yield return Capture("42_window_through");
                }
            Check("B1: each window breaks when driven into", allBroke);
            Check("B1: in through a window, on to the aisle and down the stair (as through the doors)", allIn);
            // a reset beside them: the glass is whole again
            yield return Vehicle("original");
            var l2 = WindowLine(ChWindowZ[0]); Place(l2[0], l2[2] - l2[0], 12f); yield return new WaitForFixedUpdate();
            var r2 = new Run(); yield return DriveLine(l2, 14f, 6f, r2, null, r => car.Body.position.x < 3533f, 4f);
            int before = WindowsBroken();
            yield return new WaitForSecondsRealtime(1.5f);
            respawn.PlaceAt(Ground(3545f, ChDoorZ), 270f); yield return new WaitForSecondsRealtime(1f);   // what R / Y does (VehicleRespawn.Place: Respawned)
            Check("B1: the glass is whole again after a reset beside the church", before > 0 && WindowsBroken() == 0, $"broken before the reset {before}, after {WindowsBroken()}");
            yield return Still("42_windows_outside", new Vector3(3553f, ChFloorY + 2.5f, ChDoorZ - 6f), new Vector3(ChDoorX, ChFloorY + 2.2f, ChDoorZ), 62f);
            FenceChunk.RestoreAll();
            yield return Still("42_windows_whole", new Vector3(3553f, ChFloorY + 2.5f, ChDoorZ - 6f), new Vector3(ChDoorX, ChFloorY + 2.2f, ChDoorZ), 62f);
        }

        IEnumerator R42Aisle()
        {
            report.AppendLine(); report.AppendLine("=== B2: THE AISLE AND THE STAIR: the vehicle on what is drawn");
            if (race.State != RaceController.Stage.Free) race.EndRace();
            FenceChunk.RestoreAll();
            yield return Vehicle("drifter");
            // Dan's spot: the vehicle where his was, the chase camera behind it
            Place(Bug002Car, Quaternion.Euler(0f, 267.8f, 0f) * Vector3.forward, 0f); yield return new WaitForSecondsRealtime(2.5f);
            yield return Capture("42_aisle_bug002_chase");
            yield return Still("42_aisle_bug002_cam", Bug002Cam, Bug002Cam + Quaternion.Euler(13.5f, 267.8f, 0f) * Vector3.forward * 20f, 60f);
            yield return Still("42_aisle_from_doors", new Vector3(ChDoorX - .8f, ChFloorY + 2.4f, ChDoorZ), new Vector3(3520f, ChFloorY - 2.5f, ChDoorZ), 70f);
            yield return Still("42_stair_down", new Vector3(3527f, ChFloorY + 1.6f, ChDoorZ), new Vector3(3510f, ChFloorY - 4f, ChDoorZ), 70f);
            // down the aisle and the stair slowly: the wheels' contact against the drawn stair (the line's heights from the route data)
            var line = Straight(Ground(3545f, ChDoorZ), new Vector3(ChDoorX - .5f, ChFloorY, ChDoorZ));
            foreach (var q in Straight(new Vector3(ChDoorX - 1f, ChFloorY, ChDoorZ), new Vector3(3506f, ChFloorY - 7f, ChDoorZ)).Skip(1)) line.Add(q);
            Place(line[0], line[2] - line[0], 5f); yield return new WaitForFixedUpdate();
            var run = new Run(); float worst = 0f; int n = 0; var rows = new List<string> { "x,ground_y,drawn_y,diff" };
            yield return DriveLine(line, 8f, 25f, run, null, r =>
            {
                var p = car.Body.position;
                var hits = Physics.RaycastAll(p + Vector3.up * .5f, Vector3.down, 4f, ~0, QueryTriggerInteraction.Ignore).Where(q => q.collider.attachedRigidbody != car.Body).OrderBy(q => q.distance).ToList();   // not the vehicle itself
                if (p.x < ChDoorX - 1f && p.x > 3508f && hits.Count > 0)
                {
                    var h = hits[0]; float drawn = StairY(p.x); float d = h.point.y - drawn; worst = Mathf.Max(worst, Mathf.Abs(d)); n++;
                    if (n % 5 == 0) rows.Add($"{p.x:0.00},{h.point.y:0.000},{drawn:0.000},{d:0.000}");
                }
                if (n == 40) StartCoroutine(Capture("42_on_the_stair_chase"));
                return false;
            }, 4f);
            File.WriteAllLines(Path.Combine(outputDir, "aisle_stair_contact.csv"), rows);
            Check("B2: from the doors down the stair the vehicle rides on the drawn floor (within 0.15 m of it)", n > 20 && worst < .15f, $"{n} samples, worst {worst:0.00} m; {(run.reached ? "down" : "stopped " + run.end)}");
        }
        // the stair as drawn (Data/tracks/chastain_ridge.json crypt: the floor from the doors, then down 4.5 m in; the steps sit on this slope)
        static float StairY(float x)
        {
            float s = ChDoorX - x;
            float[] ys = { 330.5f, 330.5f, 330.5f, 330.5f, 330.5f, 330.49f, 330.409f, 330.246f, 330.011f, 329.766f, 329.521f, 329.276f };
            if (s <= 0f) return ys[0]; if (s < ys.Length - 1) { int i = Mathf.FloorToInt(s); return Mathf.Lerp(ys[i], ys[i + 1], s - i); }
            return ys[ys.Length - 1] - (s - (ys.Length - 1)) * .245f;
        }

        IEnumerator R42Plume()
        {
            report.AppendLine(); report.AppendLine("=== B3: THE MANHOLE PLUME: shut in Free Roam, blows in the reverse race");
            var pl = Plume(); if (pl == null) { Check("the plume is in the build", false); yield break; }
            if (race.State != RaceController.Stage.Free) race.EndRace(); yield return new WaitForSecondsRealtime(.5f);
            yield return Vehicle("drifter");
            int th0 = ManholePlume.Throws; bool ever = false;
            foreach (float sign in new[] { 1f, -1f })   // the way it runs, then the other way
            {
                var a = pl.transform.position - pl.along * (120f * sign); var b = pl.transform.position + pl.along * (60f * sign);
                var line = Straight(Ground(a.x, a.z, pl.transform.position.y + 30f), Ground(b.x, b.z, pl.transform.position.y + 30f));
                Place(line[0], line[2] - line[0], 25f); yield return new WaitForFixedUpdate();
                var run = new Run(); yield return DriveLine(line, 30f, 15f, run, null, r => { ever |= pl.Erupting; return false; }, 5f);
                report.AppendLine($"  Free Roam {(sign > 0 ? "the way it runs" : "the other way")}: past it at {run.speedAt:0} m/s, {(run.reached ? "through" : "stopped " + run.end)}; erupted {pl.Erupting}");
            }
            Check("B3: Free Roam, past it both ways at speed: the cover stays shut", !ever && ManholePlume.Throws == th0, $"erupted {ever}, thrown {ManholePlume.Throws - th0}");
            yield return StartRevRace();
            bool shut = !pl.Erupting; float blewAt = -1f;
            float sP = AbsStation(pl.transform.position); var rl = LapSlice(sP - 200f, sP + 40f);
            Place(rl[0], rl[2] - rl[0], 20f); yield return new WaitForFixedUpdate();
            var r2 = new Run(); yield return DriveLine(rl, 30f, 20f, r2, null, r => { if (blewAt < 0f && pl.Erupting) blewAt = Vector3.Distance(Flat(car.Body.position), Flat(pl.transform.position)); return false; }, 5f);
            Check("B3: the reverse Chastain Ridge race: shut at the start, it blows ahead of the first racer", shut && blewAt > 20f, $"shut at the start {shut}; it blew with the racer {blewAt:0} m away");
            race.EndRace(); RaceController.DirectionOverride = null; yield return new WaitForSecondsRealtime(.5f);
            Check("B3: after the race it is shut", !pl.Erupting);
        }
    }
}
