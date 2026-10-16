using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Racer;
using UnityEngine;

namespace ChastainRush
{
    // Round 37 Part A in the exe: ChastainRush.exe -exeRound37 <dir> [-r37Only parkramps,parkloop] [-r37ParkContactsOff] -logFile <dir>\Player.log.
    // Muted (CheckRun), its files in <dir>, report round37_check.txt. Every ramp of the stunt park flown flat out on the Skywhip 250 and the Clay
    // Hopper, dry and in rain, with the player's own input (VehicleInput.Scripted, as Dan drives): the speed at the ramp's foot and at its lip (its
    // highest point on the line) against what gravity alone takes over that rise (v_lip = sqrt(v_foot^2 - 2 g dh)); the worst single physics step
    // (energy lost beyond the climb, as m/s). The loop both ways: in, over the top and out. -r37ParkContactsOff: the park's surfaces as Round 36
    // (VehicleSurfaceContacts), to measure "before" in the same build.
    public sealed partial class Round20Runner
    {
        // (name, start, climb direction, length of the run from the start to the lip, base)
        List<(string name, Vector3 start, Vector3 dir, float len, float speed, float approach)> R37Ramps()
        {
            const float run = 30f;
            var l = new List<(string, Vector3, Vector3, float, float, float)>();
            void Add(string name, string bas, float fu, float fv, float du, float dv, float sl, float approach = run, float speed = 30f)
            {
                float su = fu - du * approach, sv = fv - dv * approach;
                var at = bas == "roof" ? Roof(su, sv) : bas == "store" ? Store(su, sv) : Floor(su, sv);
                l.Add((name, at, PDir(du, dv), approach + sl, speed, approach));
            }
            // from Data/tracks/stunt_park.json (the take-offs from their foot; the landings ridden up their slope from its far end, as Dan did)
            Add("K1 kicker", "floor", 3588f, 374f, -1, 0, 5.67f, 10f, 16f);   // (the bank's corner 12 m behind it: from 16 m/s, down on its wheels before the foot)
            Add("K2 table", "floor", 3570f, 374f, -1, 0, 9.02f);
            Add("Barrel-roll ramp", "floor", 3476f, 382f, 1, 0, 12.03f, 11f);   // (the store front's apron ends 12.5 m behind it)
            Add("Barrel-roll landing (back)", "floor", 3489.23f, 380.5f, 1, 0, 5.13f, 10f);
            Add("Bus gap take-off", "floor", 3512f, 388f, 1, 0, 14.73f, 20f);
            Add("Bus gap landing (back)", "floor", 3557.07f, 388f, -1, 0, 12.84f);
            Add("Car gap take-off", "floor", 3600f, 443f, -1, 0, 10.29f, 11f);   // (the half-pipe 12 m behind it)
            Add("Car gap landing (back)", "floor", 3566.11f, 443f, 1, 0, 8.8f);
            Add("Half-pipe west (half way up)", "floor", 3617f, 431f, -1, 0, 4.41f, 4f);
            Add("Half-pipe east (half way up)", "floor", 3625f, 431f, 1, 0, 4.41f, 4f);
            Add("Roof quarter-pipe west (half way up)", "roof", 3391f, 400f, -1, 0, 3.81f, 25f);
            Add("Roof quarter-pipe east (half way up)", "roof", 3585f, 481.5f, 1, 0, 3.43f, 25f);
            Add("Roof table", "roof", 3556f, 481.5f, 1, 0, 6.81f, 25f);
            Add("BIG AIR take-off (to 1 m before its aimed lip)", "roof", 3448.38f, 410f, 1, 0, 5.4f, 30f);
            Add("BIG AIR landing (back)", "floor", 3539.88f, 410f, -1, 0, 16.39f);
            Add("BIG AIR back", "floor", 3493.76f, 410f, 1, 0, 21.22f, 20f);
            Add("Shop row drop take-off", "roof", 3474f, 475.91f, 0, -1, 6.5f, 8f);
            Add("Shop row drop landing (back, BUG-001)", "floor", 3474f, 425.39f, 0, 1, 11.86f);
            Add("Roof gap take-off", "roof", 3503.91f, 481.5f, 1, 0, 6.3f, 25f);
            Add("Roof gap landing (back)", "roof", 3549.4f, 481.5f, -1, 0, 12.75f, 6f);
            Add("Tower gap take-off", "roof", 3440.49f, 452f, 1, 0, 7.0f, 8f, 26f);   // (from Round 36's start, 8 m behind it)
            Add("Tower gap landing (back, BUG-003)", "floor", 3484.14f, 452f, -1, 0, 18.02f);
            Add("Roof ramp (to the roof)", "floor", 3499.4f, 356f, -1, 0, 48f);
            Add("Mezzanine ramp (inside)", "store", 3387f, 367f, 0, 1, 18f, 0f, 12f);   // (its foot at the store's back wall: from 12 m/s on it, as Round 36's inside run)
            return l;
        }

        IEnumerator R37Weather(int weather)
        {
            var look = WorldLook.Current; var s = GameSettings.Settings;
            if (race.State != RaceController.Stage.Free) race.EndRace();
            s.roamWeather = weather; if (look) look.SetClock(12, 12f);
            yield return new WaitForSecondsRealtime(1.5f);
        }

        IEnumerator R37RampsPart()
        {
            report.AppendLine(); report.AppendLine($"=== Part A: the stunt park's ramps flat out (speed at the foot -> at the lip, against gravity alone over the rise){(System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-r37ParkContactsOff") >= 0 ? "  [-r37ParkContactsOff: AS ROUND 36]" : "")}");
            if (!StuntPark.Current) { Check("the stunt park is in the build", false); yield break; }
            var ramps = R37Ramps(); string only = null; var av = System.Environment.GetCommandLineArgs(); for (int i = 0; i + 1 < av.Length; i++) if (av[i] == "-r37Ramp") only = av[i + 1];
            if (only != null) ramps = ramps.Where(r => only.Split(',').Any(o => r.name.Contains(o))).ToList();
            var csv = new List<string> { "weather,vehicle,ramp,v_foot,v_lip,dh,v_gravity,short,worst_step,touched" };
            var bad = new List<string>();
            foreach (var (wname, w) in new[] { ("dry", 0), ("rain", 1) })
            {
                yield return R37Weather(w);
                report.AppendLine($"  -- {wname} ({WeatherGrip.InEffect})");
                foreach (var id in StuntIds.Reverse())
                    foreach (var r in ramps)
                    {
                        yield return Vehicle(id);
                        var m = new RampMeasure(); yield return R37Drive(r.start, r.dir, r.len, m, r.speed, r.approach);
                        string line = m.ok ? $"{m.vFoot,5:0.0} -> {m.vLip,5:0.0} m/s over {m.dh,4:0.00} m (gravity alone: {m.vGravity,5:0.0}; {(m.Short > 0 ? $"SHORT {m.Short:0.0}" : "kept")}), worst step {m.worst:0.00} m/s" : "NOT MEASURED: " + m.why;
                        report.AppendLine($"  {Nm(id),-14} {r.name,-40} {line}{(m.touched.Count > 0 ? "; body touched " + string.Join(", ", m.touched.Distinct().Take(3)) : "")}");
                        csv.Add($"{wname},{id},{r.name},{m.vFoot:0.00},{m.vLip:0.00},{m.dh:0.00},{m.vGravity:0.00},{m.Short:0.00},{m.worst:0.00},{string.Join(" ", m.touched.Distinct().Take(3))}");
                        if (!m.ok || m.Short > 1f || m.worst > 1.5f) { bad.Add($"{wname} {Nm(id)} {r.name}"); File.WriteAllLines(Path.Combine(outputDir, $"ramp_{wname}_{id}_{new string(r.name.Where(char.IsLetterOrDigit).ToArray())}.csv"), m.trace); }
                    }
            }
            yield return R37Weather(0);
            File.WriteAllLines(Path.Combine(outputDir, "ramps.csv"), csv);
            Check("every ramp of the park keeps its speed onto and up it: at the lip within 1 m/s of what gravity alone leaves, no step losing over 1.5 m/s", bad.Count == 0, bad.Count == 0 ? $"{ramps.Count} ramps x 2 vehicles x dry / rain" : string.Join("; ", bad));
        }

        sealed class RampMeasure { public List<string> trace = new List<string> { "t,s,cte,y,speed,wheels,upy,fwd" }; public bool ok; public string why = ""; public float vFoot, vLip, dh, vGravity, worst; public List<string> touched = new List<string>(); public float Short => vGravity - vLip; }

        // flat out along a straight line (the player's input, steered onto the line), the samples read back for the foot and the lip
        IEnumerator R37Drive(Vector3 start, Vector3 dir, float len, RampMeasure m, float speed = 30f, float approach = 30f)
        {
            var right = Vector3.Cross(Vector3.up, dir);
            Place(start, dir, Mathf.Min(car.topSpeed * .85f, speed)); yield return new WaitForFixedUpdate();
            var vi = car.GetComponent<VehicleInput>(); vi.Scripted = true; car.enabled = true;
            var tap = car.gameObject.GetComponent<Touches>() ?? car.gameObject.AddComponent<Touches>(); tap.seen.Clear(); tap.on = true;
            var ss = new List<float>(); var ys = new List<float>(); var vs = new List<float>(); float t = 0f;
            float y0 = car.Body.position.y;
            while (t < 8f)
            {
                yield return new WaitForFixedUpdate(); t += Time.fixedDeltaTime;
                var p = car.Body.position; float s = Vector3.Dot(Flat(p - start), dir), cte = Vector3.Dot(Flat(p - start), right);
                float head = Vector3.SignedAngle(Flat(car.transform.forward).normalized, dir, Vector3.up);
                vi.sThrottle = 1f; vi.sBrake = 0f; vi.sSteer = car.GroundedWheels == 0 ? 0f : Mathf.Clamp((head - Mathf.Atan2(1.6f * cte, Mathf.Max(car.ForwardSpeed, 1f) + 2f) * Mathf.Rad2Deg) / 18f, -1f, 1f);
                ss.Add(s); ys.Add(p.y); vs.Add(car.Body.linearVelocity.magnitude); m.trace.Add($"{t:0.00},{s:0.00},{cte:0.00},{p.y:0.00},{car.Body.linearVelocity.magnitude:0.00},{car.GroundedWheels},{car.transform.up.y:0.00},{Vector3.Dot(car.transform.forward, dir):0.00}");
                if (s > len + .5f) break;
            }
            vi.sThrottle = vi.sBrake = vi.sSteer = 0f; vi.Scripted = false; tap.on = false;
            m.touched.AddRange(tap.seen.Select(x => x.Split(' ').Skip(1).TakeWhile(w => w != "at").Aggregate((a, b) => a + " " + b)));
            // the approach's level: the samples in the first 10 m; the foot: the last sample before the line rises 0.12 m over it
            // the approach's level: the lowest point of its first stretch (the vehicle is put down 0.8 m up and settles)
            // the approach's level: where the vehicle is 1 m before the ramp's foot (it is put down 0.8 m up and settles on the way)
            int a0 = Mathf.Max(0, ss.FindLastIndex(x => x < approach - 1f)); float level = approach >= 2f ? ys[a0] : y0;
            int foot = -1; for (int i = approach >= 2f ? a0 : 0; i < ss.Count; i++) if (ys[i] > level + .12f) { foot = Mathf.Max(0, i - 2); break; }
            if (foot < 0) { m.why = $"never rose (reached {ss.LastOrDefault():0.0} of {len:0.0} m)"; yield break; }
            int last = ss.FindLastIndex(s => s <= len - .3f); if (last <= foot) { m.why = $"never reached the lip (reached {ss.Max():0.0} of {len:0.0} m)"; yield break; }
            float top = float.MinValue; for (int i = foot; i <= last; i++) top = Mathf.Max(top, ys[i]);
            int lip = foot; for (int i = foot; i <= last; i++) if (ys[i] >= top - .05f) { lip = i; break; }
            float g = -Physics.gravity.y;
            m.vFoot = vs[foot]; m.vLip = vs[lip]; m.dh = ys[lip] - ys[foot];
            m.vGravity = Mathf.Sqrt(Mathf.Max(0f, m.vFoot * m.vFoot - 2f * g * m.dh));
            for (int i = foot + 1; i <= lip; i++)
            {
                float e0 = .5f * vs[i - 1] * vs[i - 1] + g * ys[i - 1], e1 = .5f * vs[i] * vs[i] + g * ys[i];
                if (e1 < e0) m.worst = Mathf.Max(m.worst, vs[i - 1] - Mathf.Sqrt(Mathf.Max(0f, 2f * (e1 - g * ys[i - 1]))));
            }
            m.ok = true;
        }

        // the loop both ways: in on its way in (and on its way out, driven backward through it), the speed at the entry, at the top and out
        IEnumerator R37LoopPart()
        {
            report.AppendLine(); report.AppendLine("=== Part A: the loop, both ways (BUG-002)");
            if (!StuntPark.Current) { Check("the stunt park is in the build", false); yield break; }
            var o = StuntPark.LoopOrigin; var f = StuntPark.LoopForward; var side = StuntPark.LoopSide; float shift = StuntPark.LoopShift, R = StuntPark.LoopRadius;
            var bad = new List<string>();
            foreach (var (wname, w) in new[] { ("dry", 0), ("rain", 1) })
            {
                yield return R37Weather(w);
                foreach (var id in StuntIds.Reverse())
                    foreach (var (way, a, d) in new[] { ("way in", o - f * 30f, f), ("way out (backward)", o + side * shift + f * 30f, -f) })
                    {
                        yield return Vehicle(id);
                        Place(a, d, Mathf.Min(car.topSpeed * .85f, 30f)); yield return new WaitForFixedUpdate();
                        var vi = car.GetComponent<VehicleInput>(); vi.Scripted = true; car.enabled = true;
                        var tap = car.gameObject.GetComponent<Touches>() ?? car.gameObject.AddComponent<Touches>(); tap.seen.Clear(); tap.on = true;
                        var trace = new List<string> { "t,x,y,z,speed,wheels,upx,upy,upz" };
                        float t = 0f, vIn = -1f, vTop = -1f, vOut = -1f, before = -1f, worst = 0f; bool high = false; float prevE = float.NaN, prevV = 0f, prevY = 0f; float g = -Physics.gravity.y;
                        var lineStart = a; var lineDir = d;
                        while (t < 9f)
                        {
                            yield return new WaitForFixedUpdate(); t += Time.fixedDeltaTime;
                            var p = car.Body.position; float v = car.Body.linearVelocity.magnitude; float s = Vector3.Dot(Flat(p - a), d);
                            float yUp = p.y - o.y;
                            if (vIn < 0f && before < 0f && s > 27f) before = v;
                            if (vIn < 0f && s > 30.5f && yUp < 1.5f) vIn = v;
                            if (yUp > 2f * R - 1.5f) { high = true; vTop = vTop < 0 ? v : Mathf.Min(vTop, v); }
                            if (high && yUp < 1.2f && vOut < 0f && Vector3.Dot(car.Body.linearVelocity, d) > 0f) vOut = v;
                            if (vIn > 0f && vOut < 0f && !float.IsNaN(prevE))
                            {
                                float e = .5f * v * v + g * p.y; if (e < prevE) worst = Mathf.Max(worst, prevV - Mathf.Sqrt(Mathf.Max(0f, 2f * (e - g * prevY))));
                            }
                            prevE = .5f * v * v + g * p.y; prevV = v; prevY = p.y;
                            if (vIn > 0f && vOut < 0f) trace.Add($"{t:0.00},{p.x:0.00},{p.y:0.00},{p.z:0.00},{v:0.00},{car.GroundedWheels},{car.transform.up.x:0.00},{car.transform.up.y:0.00},{car.transform.up.z:0.00}");
                            // steer onto the line before and after; on the loop the loop steers (ArcadeVehicle)
                            if (high && vOut > 0f) { lineStart = o + (d == f ? side * shift : Vector3.zero); }
                            var right = Vector3.Cross(Vector3.up, d); float cte = Vector3.Dot(Flat(p - lineStart), right);
                            float head = Vector3.SignedAngle(Flat(car.transform.forward).normalized, d, Vector3.up);
                            vi.sThrottle = 1f; vi.sBrake = 0f; vi.sSteer = high && vOut < 0f ? 0f : Mathf.Clamp((head - Mathf.Atan2(1.6f * cte, Mathf.Max(car.ForwardSpeed, 1f) + 2f) * Mathf.Rad2Deg) / 18f, -1f, 1f);
                            if (vOut > 0f && s > 70f) break;
                        }
                        vi.sThrottle = vi.sSteer = 0f; vi.Scripted = false; tap.on = false;
                        File.WriteAllLines(Path.Combine(outputDir, $"loop_{wname}_{id}_{(d == f ? "in" : "out")}.csv"), trace);
                        string touched = string.Join("; ", tap.seen.Where(x => !x.Contains("loop ")).Distinct().Take(3)) + (tap.seen.Any(x => x.Contains("loop rails")) ? $"; the rails {tap.seen.Count(x => x.Contains("loop rails"))} times" : "");
                        float vTopGravity = vIn > 0 ? Mathf.Sqrt(Mathf.Max(0f, vIn * vIn - 2f * g * 2f * R)) : -1f;
                        bool ok = vIn > 0f && high && vOut > 0f && vOut > vIn - 1.5f;
                        if (!ok) bad.Add($"{wname} {Nm(id)} {way}");
                        report.AppendLine($"  {wname,-4} {Nm(id),-14} {way,-20} {before:0.0} m/s 3 m before, {vIn:0.0} into it, {vTop:0.0} at the top (gravity alone {vTopGravity:0.0}), {(vOut > 0 ? $"{vOut:0.0} out" : "NEVER CAME OUT")}; worst step {worst:0.00} m/s{(touched.Trim(' ', ';').Length > 0 ? "; touched " + touched : "")}");
                    }
            }
            yield return R37Weather(0);
            Check("the loop both ways: in, over the top and out at the speed it went in (within 1.5 m/s)", bad.Count == 0, string.Join("; ", bad));
        }
    }
}
