using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Racer;
using UnityEngine;

namespace ChastainRush
{
    // Round 41 Part A in the exe (-exeRound41 <dir> -r41Only crypt,cryptcrash,cryptpics,ridgerace [-traffic Normal] -logFile <dir>\Player.log):
    //   crypt       the reverse Chastain Ridge's Crypt shortcut driven flat out (full throttle, braked only for the bends) on every vehicle the route
    //               offers, against the road from the same place to the same place: time saved, the exit jump's air, where it comes down, upright
    //   cryptcrash  a crash: the Drifter Twin and the Street Classic flat out 3.8 m off the line through the crypt's hall (its tombs), against the road
    //   cryptpics   the doors, the stair down, the hall, the catacomb, the exit stair from below, the jump, the landing, the graveyard
    //   ridgerace   Round 40's: 3 reverse laps, 3 AI, traffic as given, then a race where every AI takes every shortcut (sloppy tries as they come)
    public sealed partial class Round20Runner
    {
        RaceShortcut Crypt() => race.Route.Shortcuts.FirstOrDefault(s => s.Id == "rch_crypt");
        static readonly Vector2 CryptDoor = new Vector2(3535.0f, 576.95f), CryptLip = new Vector2(3395.0f, 447.0f);
        static int NearestOn(Vector3[] path, Vector2 q) { int b = 0; float bd = float.MaxValue; for (int i = 0; i < path.Length; i++) { float d = (new Vector2(path[i].x, path[i].z) - q).sqrMagnitude; if (d < bd) { bd = d; b = i; } } return b; }

        // the lap from 60 m before the shortcut to 30 m past its end; or the same through the shortcut's own line (its heights), 'off' m to the right in the hall
        IEnumerator TimedCrypt(bool viaCrypt, float off, Run run, System.Action<float, float, Vector3, float> done, bool atDoor = false)
        {
            var sc = Crypt(); var a = sc.Path[0]; var b = sc.Path[sc.Path.Length - 1]; float sa = AbsStation(a), sb = AbsStation(b); if (sb < sa) sb += race.Route.LapLength;
            var line = LapSlice(sa - 60f, sa).ToList(); int iLip = -1; Vector3 aEnd = a, bEnd = b;
            if (viaCrypt)
            {
                int iDoor = NearestOn(sc.Path, CryptDoor), lip = NearestOn(sc.Path, CryptLip);
                for (int i = 0; i < sc.Path.Length; i++)
                {
                    var p = sc.Path[i];
                    if (off != 0f && i > iDoor + 20 && i < iDoor + 42)   // the hall (40 to 84 m past the doors): toward its tombs (the outside of its bend)
                    {
                        var t = sc.Path[Mathf.Min(i + 1, sc.Path.Length - 1)] - sc.Path[Mathf.Max(i - 1, 0)]; t.y = 0f; var n = Vector3.Cross(Vector3.up, t.normalized);
                        float k = Mathf.Clamp01((i - iDoor - 20) / 3f) * Mathf.Clamp01((iDoor + 42 - i) / 3f); p += n * off * k;
                    }
                    if (i == lip) iLip = line.Count;
                    line.Add(p);
                }
                line.AddRange(LapSlice(sb + 2f, sb + 30f));
            }
            else line.AddRange(LapSlice(sa + 2f, sb + 30f));
            if (atDoor) { int iD = line.FindIndex(q => Vector2.Distance(new Vector2(q.x, q.z), CryptDoor) < 1.5f) - 4; line = line.GetRange(iD, line.Count - iD); aEnd = line[4]; if (iLip >= 0) iLip -= iD; }   // from 8 m before the doors (the approach checked apart)
            Place(line[0], line[2] - line[0], atDoor ? 22f : 15f); yield return new WaitForFixedUpdate();
            var cum = new float[line.Count]; for (int i = 1; i < line.Count; i++) cum[i] = cum[i - 1] + Flat(line[i] - line[i - 1]).magnitude;
            float sLip = iLip >= 0 ? cum[iLip] : float.MaxValue;
            float t0 = -1f, t1 = -1f, clock = 0f, jumpAir = 0f, curAir = 0f, lipSpeed = -1f; Vector3 jumpLand = new Vector3(float.NaN, 0f, 0f); float jumpUp = -1f; bool pastLip = false, landed = false;
            float lastS = 0f;
            var drive = DriveLine(line, 99f, 60f, run, null, r =>
            {
                var p = car.Body.position; clock += Time.fixedDeltaTime;
                if (t0 < 0f && Vector3.Distance(Flat(p), Flat(aEnd)) < 5f) t0 = clock;
                float s = ProjectNear(line, cum, p, lastS, out _); lastS = s;
                if (t0 >= 0f && t1 < 0f && s > cum[cum.Length - 1] - 31f) t1 = clock;   // the shortcut's end (both lines end 30 m past it on the lap)
                if (!pastLip && s >= sLip - .3f) { pastLip = true; lipSpeed = Flat(car.Body.linearVelocity).magnitude; }
                if (pastLip && !landed)
                {
                    if (car.GroundedWheels == 0) curAir += Time.fixedDeltaTime;
                    else if (curAir > .25f) { landed = true; jumpAir = curAir; jumpLand = p; jumpUp = car.transform.up.y; }
                }
                return false;
            }, 5f, true, null);
            yield return drive;
            done(t0 >= 0f && t1 > t0 ? t1 - t0 : -1f, landed ? jumpAir : -1f, jumpLand, jumpUp);
            if (viaCrypt) report.AppendLine($"      off the lip at {lipSpeed:0.0} m/s");
        }

        IEnumerator R41Crypt(string[] ids)
        {
            report.AppendLine(); report.AppendLine("=== A: THE CRYPT (reverse race): every vehicle the route offers, flat out (braked only for the bends) through it and along the road");
            yield return StartRevRace();
            var sc = Crypt(); if (sc == null) { Check("the Crypt shortcut is in the reverse race", false); race.EndRace(); yield break; }
            report.AppendLine($"  {sc.Name}: forgives CP {string.Join(", ", race.RouteData.official_shortcuts.First(s => s.id == sc.Id).forgives)}; its line {sc.Path.Length} points");
            var offered = ids.Where(id => race.RouteData == null || race.RouteData.AllowsVehicle(id)).ToArray();
            var slow = new List<string>(); var badLand = new List<string>(); float least = float.MaxValue, most = 0f;
            foreach (var id in offered)
            {
                yield return Vehicle(id);
                float road = -1f, via = -1f, air = -1f, up = -1f; Vector3 land = default;
                var r0 = new Run(); yield return TimedCrypt(false, 0f, r0, (t, a2, l, u) => road = t);
                var r1 = new Run(); yield return TimedCrypt(true, 0f, r1, (t, a2, l, u) => { via = t; air = a2; land = l; up = u; });
                float dLine = float.IsNaN(land.x) ? -1f : Vector3.Distance(Flat(land), Flat(race.Route.PointAt(AbsStation(land), out _)));
                float saved = road > 0f && via > 0f ? road - via : -99f;
                if (saved > -90f) { least = Mathf.Min(least, saved); most = Mathf.Max(most, saved); }
                if (saved < 2f) slow.Add(Nm(id));
                if (air < 0f || up < .7f || !r1.reached) badLand.Add(Nm(id));
                File.WriteAllLines(Path.Combine(outputDir, $"crypt_{id}.csv"), r1.trail.Select(q => $"{q.x:0.00},{q.y:0.00},{q.z:0.00}"));
                File.WriteAllLines(Path.Combine(outputDir, $"road_{id}.csv"), r0.trail.Select(q => $"{q.x:0.00},{q.y:0.00},{q.z:0.00}"));
                report.AppendLine($"  {Nm(id),-18} the road {road:0.0} s; the crypt {via:0.0} s (saves {saved:0.0} s); the exit jump {air:0.00} s in the air, down at ({land.x:0}, {land.z:0}) {dLine:0.0} m from the race line, upright {up:0.00}; " +
                                  $"{(r1.reached ? "on along New Chastain Road" : "STOPPED " + r1.end + $" at ({car.Body.position.x:0.0}, {car.Body.position.y:0.0}, {car.Body.position.z:0.0})")}");
                race.StartRace(); while (race.State == RaceController.Stage.Countdown) yield return null;   // the doors and headstones back for the next one
            }
            race.EndRace(); RaceController.DirectionOverride = null;
            Check("A: the crypt taken flat out and clean saves at least 2 s against the road on every vehicle the route offers", slow.Count == 0, $"saves {least:0.0} to {most:0.0} s" + (slow.Count > 0 ? "; not: " + string.Join(", ", slow) : ""));
            Check("A: every vehicle comes out of the exit jump on its wheels and drives on", badLand.Count == 0, badLand.Count == 0 ? "" : "not: " + string.Join(", ", badLand));
        }

        IEnumerator R41CryptIn(string[] ids)
        {
            report.AppendLine(); report.AppendLine("=== A: the crypt from 8 m before its doors (22 m/s) to the shortcut's end, flat out: the time, the lip, the jump");
            yield return StartRevRace();
            foreach (var id in ids.Where(i => race.RouteData == null || race.RouteData.AllowsVehicle(i)))
            {
                yield return Vehicle(id); float via = -1f, air = -1f, up = -1f; Vector3 land = default;
                var r1 = new Run(); yield return TimedCrypt(true, 0f, r1, (t, a2, l, u) => { via = t; air = a2; land = l; up = u; }, true);
                report.AppendLine($"  {Nm(id),-18} doors to the end {via:0.0} s; the jump {air:0.00} s, down at ({land.x:0}, {land.z:0}) upright {up:0.00}; {(r1.reached ? "on" : "STOPPED " + r1.end + $" at ({car.Body.position.x:0.0}, {car.Body.position.y:0.0}, {car.Body.position.z:0.0})")}");
                if (!r1.reached) File.WriteAllLines(Path.Combine(outputDir, $"cryptin_{id}.csv"), r1.trail.Select(q => $"{q.x:0.00},{q.y:0.00},{q.z:0.00}"));
                race.StartRace(); while (race.State == RaceController.Stage.Countdown) yield return null;
            }
            race.EndRace(); RaceController.DirectionOverride = null;
        }

        IEnumerator R41CryptCrash()
        {
            report.AppendLine(); report.AppendLine("=== A: a crash in the crypt (flat out 3.8 m off the line through the hall, into its pillars and tombs), against the road");
            yield return StartRevRace();
            var cheap = new List<string>();
            foreach (var id in new[] { "drifter", "original" })
            {
                yield return Vehicle(id);
                float road = -1f, crash = -1f;
                var r0 = new Run(); yield return TimedCrypt(false, 0f, r0, (t, a, l, u) => road = t);
                var r1 = new Run(); yield return TimedCrypt(true, 3.8f, r1, (t, a, l, u) => crash = t);
                if (id == "drifter") yield return Capture("41_crypt_after_crash");
                bool costs = crash < 0f || crash > road;
                if (!costs) cheap.Add(Nm(id));
                report.AppendLine($"  {Nm(id),-18} the road {road:0.0} s; the crypt crashed in the hall {(crash > 0f ? crash.ToString("0.0") + " s" : "never through")} " +
                                  $"({(crash > 0f ? $"{crash - road:+0.0;-0.0} s against the road" : r1.end)}), slowest {r1.minSpeed:0.0} m/s");
                if (!r1.reached) File.WriteAllLines(Path.Combine(outputDir, $"crypt_{id}_crash.csv"), r1.trail.Select(q => $"{q.x:0.00},{q.y:0.00},{q.z:0.00}"));
                race.StartRace(); while (race.State == RaceController.Stage.Countdown) yield return null;
            }
            race.EndRace(); RaceController.DirectionOverride = null;
            Check("A: a crash in the crypt costs more than the road", cheap.Count == 0, cheap.Count == 0 ? "" : "not: " + string.Join(", ", cheap));
        }

        IEnumerator R41CryptPics()
        {
            report.AppendLine(); report.AppendLine("=== A: pictures of the crypt");
            yield return StartRevRace();
            var sc = Crypt(); if (sc == null) { Check("the Crypt shortcut is in the reverse race", false); race.EndRace(); yield break; }
            car.Body.position = RaceRoute.Surface(3600f, 640f) + Vector3.up; car.Body.linearVelocity = Vector3.zero;
            var P = sc.Path; int d = NearestOn(P, CryptDoor), lip = NearestOn(P, CryptLip);
            Vector3 At2(int i) => P[Mathf.Clamp(i, 0, P.Length - 1)];
            var door = At2(d);
            yield return Still("41_crypt_doors", door + new Vector3(22f, 3f, 6f), door + Vector3.up * 3f, 60f);
            yield return Still("41_crypt_aisle_and_stair", door + new Vector3(-1.2f, 2.6f, -2.2f), At2(d + 14) + Vector3.up * .5f, 75f);
            yield return Still("41_crypt_stair_down", At2(d + 4) + Vector3.up * 3.2f, At2(d + 18) + Vector3.up * 1.0f, 70f);
            yield return Still("41_crypt_hall", At2(d + 20) + Vector3.up * 2.6f, At2(d + 30) + Vector3.up * 1.5f, 75f);
            yield return Still("41_crypt_catacomb", At2(d + 55) + Vector3.up * 2.4f, At2(d + 67) + Vector3.up * 1.6f, 70f);
            yield return Still("41_crypt_bend", At2(lip - 27) + Vector3.up * 2.4f, At2(lip - 17) + Vector3.up * 1.6f, 75f);
            yield return Still("41_crypt_exit_stair_from_below", At2(lip - 8) + Vector3.up * 1.8f, At2(lip) + Vector3.up * 1.5f, 70f);
            var L = At2(lip); var f = (At2(lip + 3) - At2(lip - 3)); f.y = 0f; f.Normalize(); var side = Vector3.Cross(Vector3.up, f);
            yield return Still("41_crypt_lip_and_surround", L + f * 9f + side * 7f + Vector3.up * 3f, L + Vector3.up * .8f, 60f);
            yield return Still("41_graveyard", L + f * 22f + side * 26f + Vector3.up * 12f, L + f * 14f, 62f);
            yield return Still("41_crypt_landing", L + f * 70f + side * 10f + Vector3.up * 5f, L + f * 42f, 62f);
            race.EndRace();
            // the jump itself: a clean run on the Street Classic, the chase camera captured over the graveyard
            yield return StartRevRace();
            yield return Vehicle("original");
            bool shot = false; var line = LapSlice(AbsStation(P[0]) - 40f, AbsStation(P[0])).ToList(); line.AddRange(P); line.AddRange(LapSlice(AbsStation(P[P.Length - 1]) + 2f, AbsStation(P[P.Length - 1]) + 30f));
            Place(line[0], line[2] - line[0], 15f); yield return new WaitForFixedUpdate();
            var run = new Run();
            var dr = StartCoroutine(DriveLine(line, 99f, 45f, run, null, r => { if (!shot && car.GroundedWheels == 0 && Vector3.Distance(Flat(car.Body.position), Flat(L)) > 12f && Vector3.Distance(Flat(car.Body.position), Flat(L)) < 30f) { shot = true; StartCoroutine(Capture("41_crypt_jump_chase")); } return false; }, 5f, true, null));
            while (!run.reached && run.end == "" && run.time < 44.9f) yield return new WaitForFixedUpdate();
            yield return dr;
            race.EndRace(); RaceController.DirectionOverride = null;
            Check("A: pictures written", true, "41_crypt_*.png, 41_graveyard.png");
        }
    }
}
