using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Racer;
using UnityEngine;

namespace ChastainRush
{
    // Round 14, items 2 and 5 (ChastainRush.exe -exeRound14 <dir> [-r14Vehicles moto,mower,atv,pebble] [-r14Tag before]).
    // Measures, in the exe, why a vehicle loses speed with the throttle held. Each vehicle in turn (swapped by the temporary
    // switch, never saved): (a) from rest at full throttle along the flattest straight of New Chastain Road (700 m westbound,
    // 2915,757 to 2206,761); (b) the steepest 150 m of Chastain Glen Lane from 8 m/s, as Round 13; (c) one whole lap of the
    // race route from a rolling start, full throttle except where the scripted driver must slow for a bend. Every physics step
    // is logged with the change in speed split into what acted on the car in that step: the drive force, gravity along the
    // path, the body's damping, the lateral grip, the springs, impulses on the BODY collider (with what it touched), speed taken
    // by geese or breakables (they scale the velocity), and what is left. TSV traces and a summary go to the output folder.
    public sealed class Round14Runner : MonoBehaviour
    {
        public string outputDir;
        readonly StringBuilder report = new StringBuilder();
        ArcadeVehicle car; VehicleSwitch sw; VehicleConfiguration cfg; Probe probe;
        GooseFlock[] flocks;
        static readonly CultureInfo IC = CultureInfo.InvariantCulture;

        // New Chastain Road westbound, every 10 m (Data/map.json, the flattest 400 m straight with 150 m either side)
        static readonly float[] Straight = {
            2915.4f, 756.6f, 2905.4f, 755.7f, 2895.5f, 754.9f, 2885.5f, 755.0f, 2875.5f, 755.1f, 2865.5f, 755.2f, 2855.5f, 755.3f, 2845.5f, 755.4f, 2835.5f, 755.5f, 2825.5f, 755.7f,
            2815.5f, 755.8f, 2805.5f, 756.1f, 2795.5f, 756.4f, 2785.5f, 756.8f, 2775.5f, 757.1f, 2765.5f, 757.5f, 2755.5f, 757.8f, 2745.5f, 758.1f, 2735.5f, 758.4f, 2725.5f, 758.7f,
            2715.5f, 759.0f, 2705.5f, 759.3f, 2695.5f, 759.6f, 2685.6f, 759.9f, 2675.6f, 760.2f, 2665.6f, 760.5f, 2655.6f, 760.8f, 2645.6f, 761.1f, 2635.6f, 761.4f, 2625.6f, 761.7f,
            2615.6f, 762.0f, 2605.6f, 762.3f, 2595.6f, 762.7f, 2585.6f, 763.0f, 2575.6f, 763.3f, 2565.6f, 763.6f, 2555.6f, 763.8f, 2545.6f, 764.1f, 2535.6f, 764.4f, 2525.6f, 764.7f,
            2515.6f, 764.9f, 2505.6f, 765.2f, 2495.6f, 765.5f, 2485.6f, 765.8f, 2475.6f, 766.1f, 2465.6f, 766.2f, 2455.6f, 766.1f, 2445.6f, 766.1f, 2435.6f, 766.0f, 2425.6f, 765.9f,
            2415.6f, 765.7f, 2405.6f, 765.6f, 2395.6f, 765.5f, 2385.6f, 765.3f, 2375.7f, 765.2f, 2365.7f, 765.0f, 2355.7f, 764.9f, 2345.7f, 764.7f, 2335.7f, 764.6f, 2325.7f, 764.4f,
            2315.7f, 764.3f, 2305.7f, 764.1f, 2295.7f, 764.0f, 2285.7f, 763.8f, 2275.7f, 763.6f, 2265.7f, 763.4f, 2255.7f, 763.2f, 2245.7f, 762.9f, 2235.7f, 762.7f, 2225.7f, 762.5f,
            2215.7f, 762.2f, 2205.8f, 761.0f };

        // What the car's body touched in one physics step, and the triggers it entered
        sealed class Probe : MonoBehaviour
        {
            public Vector3 Impulse; public int Contacts; public float MinNormalY = 1f; public bool Terrain;
            public readonly HashSet<string> Touched = new HashSet<string>(), Triggers = new HashSet<string>();
            public void Clear() { Impulse = Vector3.zero; Contacts = 0; MinNormalY = 1f; Terrain = false; Touched.Clear(); Triggers.Clear(); }
            void OnCollisionEnter(Collision c) => Add(c);
            void OnCollisionStay(Collision c) => Add(c);
            void Add(Collision c)
            {
                Impulse += c.impulse; Contacts += c.contactCount;
                for (int i = 0; i < c.contactCount; i++) MinNormalY = Mathf.Min(MinNormalY, c.GetContact(i).normal.y);
                if (c.collider is TerrainCollider) Terrain = true;
                Touched.Add(c.collider is TerrainCollider ? "terrain" : c.collider.name);
            }
            void OnTriggerEnter(Collider o) => Triggers.Add(o.name);
        }

        struct Row
        {
            public float minNy, t, s, x, y, z, v, vf, thr, brk, grade, upY, dv, drive, grav, damp, grip, susp, body, scaled, resid, slip;
            public int wheels, contacts; public bool terrain, wiped; public string touched, triggers;
        }

        IEnumerator Start()
        {
            Directory.CreateDirectory(outputDir);
            Application.runInBackground = true;
            var args = Environment.GetCommandLineArgs();
            string[] ids = { "moto", "mower", "atv", "pebble", "original" }; string tag = "run";
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == "-r14Vehicles") ids = args[i + 1].Split(',');
                if (args[i] == "-r14Tag") tag = args[i + 1];
            }
            report.AppendLine($"Round 14 measurement ({tag}), {DateTime.Now:yyyy-MM-dd HH:mm}, the exe. Physics step {Time.fixedDeltaTime * 1000f:0.0} ms.");
            yield return new WaitForSecondsRealtime(4f);
            car = FindAnyObjectByType<ArcadeVehicle>(); sw = car.GetComponent<VehicleSwitch>(); cfg = car.GetComponent<VehicleConfiguration>();
            probe = car.gameObject.AddComponent<Probe>();
            flocks = FindObjectsByType<GooseFlock>();
            var r = RaceController.Instance.Route;
            report.AppendLine($"Surface contact fix registered surfaces: see Player.log. Lap {r.LapLength:0} m.");

            // the steepest 150 m of the lap (as Round 13)
            float bestRise = 0f, bestS = 0f;
            for (float s = 0f; s < r.LapLength; s += 5f)
            {
                float rise = r.PointAt(s + 150f, out _).y - r.PointAt(s, out _).y;
                if (rise > bestRise) { bestRise = rise; bestS = s; }
            }
            report.AppendLine($"Steepest 150 m of the lap: {bestS:0} to {bestS + 150:0} m, rising {bestRise:0.0} m ({bestRise / 1.5f:0.0}%).");
            report.AppendLine();

            var straight = new List<Vector3>();
            for (int i = 0; i < Straight.Length; i += 2) straight.Add(RaceRoute.Ground(Straight[i], Straight[i + 1]));
            var lap = new List<Vector3>(); for (float s = 0; s <= r.LapLength + 60f; s += 3f) lap.Add(r.PointAt(s, out _));
            var climb = new List<Vector3>(); for (float s = bestS - 10f; s <= bestS + 190f; s += 3f) climb.Add(r.PointAt(s, out _));

            foreach (var id in ids)
            {
                var p = VehicleProfile.Find(id);
                report.AppendLine($"=== {p.Name} ({p.Id}, {p.Class}): top speed {p.Speed} m/s, acceleration {p.Acceleration} m/s2, body {p.Size}, mass {p.Mass} kg");
                // (a) the straight, from rest
                var rows = new List<Row>();
                yield return Drive(id, straight, 0f, 60f, false, rows);
                Summarise($"(a) New Chastain Road straight, from rest", rows, straight, true);
                Write($"r14_{tag}_{id}_straight.tsv", rows);
                // (b) the climb from 8 m/s
                rows = new List<Row>();
                yield return Drive(id, climb, 8f, 40f, false, rows);
                Summarise($"(b) steepest 150 m of Chastain Glen Lane from 8 m/s", rows, climb, false);
                Write($"r14_{tag}_{id}_climb.tsv", rows);
                // (c) the lap
                rows = new List<Row>();
                yield return Drive(id, lap, 15f, 240f, true, rows);
                Summarise($"(c) one lap, rolling start at 15 m/s", rows, lap, false);
                Write($"r14_{tag}_{id}_lap.tsv", rows);
                report.AppendLine();
                File.WriteAllText(Path.Combine(outputDir, $"round14_{tag}.txt"), report.ToString());
            }
            sw.Select("original");
            File.WriteAllText(Path.Combine(outputDir, $"round14_{tag}.txt"), report.ToString());
            yield return new WaitForSecondsRealtime(0.5f);
            Application.Quit(0);
        }

        bool done;
        IEnumerator Drive(string id, List<Vector3> line, float v0, float maxTime, bool bends, List<Row> rows)
        {
            done = false;
            var respawn = car.GetComponent<VehicleRespawn>();
            var f0 = Flat(line[1] - line[0]).normalized;
            respawn.PlaceAt(line[0] + Vector3.up * 0.3f, Mathf.Atan2(f0.x, f0.z) * Mathf.Rad2Deg);
            yield return new WaitForFixedUpdate();
            sw.Select(id);
            yield return new WaitForSecondsRealtime(0.5f);
            respawn.PlaceAt(line[0] + Vector3.up * 0.3f, Mathf.Atan2(f0.x, f0.z) * Mathf.Rad2Deg);
            if (RaceController.Instance) RaceController.Instance.NotifyTeleported();
            for (int i = 0; i < 25; i++) yield return new WaitForFixedUpdate();   // settle on the springs
            car.enabled = false;
            car.Body.linearVelocity = f0 * v0 + Vector3.up * car.Body.linearVelocity.y;
            float time = 0f, stuck = 0f, travelled = 0f;
            var end = line[line.Count - 1]; var endDir = Flat(end - line[line.Count - 2]).normalized;
            float prevSide = Vector3.Dot(car.transform.position - end, endDir);
            float mass = car.Body.mass, damping = car.Body.linearDamping;
            Vector3 prevPos = car.transform.position;
            while (time < maxTime)
            {
                float dt = Time.fixedDeltaTime;
                var pos = car.transform.position; var vel = car.Body.linearVelocity; float speed = vel.magnitude;
                float target = bends ? TargetSpeed(line, pos) : 999f;
                float thr = speed < target - 0.5f ? 1f : speed < target + 1.5f ? 0.3f : 0f;
                float brk = speed > target + 3f ? Mathf.Clamp01((speed - target) / 6f) : 0f;
                // predicted contributions along the direction of travel for this step (ArcadeVehicle.Simulate's own sums)
                Vector3 u = speed > 0.3f ? vel / speed : car.transform.forward;
                car.Simulate(thr, brk, Steer(line, pos), dt);
                int wheels = car.GroundedWheels; bool grounded = wheels >= 2; bool wiped = cfg.WipedOut;
                Vector3 fwd = car.transform.forward;
                float vf = Vector3.Dot(vel, fwd);
                float driveAcc = 0f;
                if (grounded)
                {
                    if (brk > 0.05f && vf > 0.6f) driveAcc = -car.braking * brk;
                    else driveAcc = thr * car.acceleration * Mathf.Clamp01(1 - Mathf.Max(0, vf) / car.topSpeed);
                    if (thr < 0.05f && brk < 0.05f) driveAcc -= vf * car.coastingDrag;
                }
                float drive = grounded ? driveAcc * Vector3.Dot(fwd, u) * dt : 0f;
                Vector3 right = car.transform.right;
                float side = Vector3.Dot(vel, right);
                float grip = grounded ? -Mathf.Clamp(side * car.lateralGrip, -car.maxGripAcceleration, car.maxGripAcceleration) * Vector3.Dot(right, u) * dt : 0f;
                float susp = car.SuspensionLift * Vector3.Dot(car.transform.up, u) * dt;
                float grav = Vector3.Dot(Physics.gravity, u) * dt;
                float damp = -speed * (1f - 1f / (1f + dt * damping));
                float goose0 = flocks.Sum(f => f ? f.TotalSpeedLost : 0f); int panels0 = FenceChunk.BrokenPanels;
                probe.Clear();
                yield return new WaitForFixedUpdate();
                time += dt;
                var nv = car.Body.linearVelocity; float nspeed = nv.magnitude;
                float body = Vector3.Dot(probe.Impulse, u) / mass;
                float goose = flocks.Sum(f => f ? f.TotalSpeedLost : 0f) - goose0; int panels = FenceChunk.BrokenPanels - panels0;
                float dv = nspeed - speed;
                float scaled = (goose > 0f || panels > 0) ? -speed * (goose + panels * FenceChunk.SpeedCostPerPanel) : 0f;
                float ahead = GroundQuery.Height(pos.x + u.x * 2f, pos.z + u.z * 2f), behind = GroundQuery.Height(pos.x - u.x * 2f, pos.z - u.z * 2f);
                float grade = float.IsNaN(ahead) || float.IsNaN(behind) ? 0f : (ahead - behind) / 4f * 100f;
                travelled += Flat(car.transform.position - prevPos).magnitude; prevPos = car.transform.position;
                var row = new Row
                {
                    t = time, s = travelled, x = pos.x, y = pos.y, z = pos.z, v = speed, vf = vf, thr = thr, brk = brk, grade = grade, upY = car.transform.up.y,
                    dv = dv, drive = drive, grav = grav, damp = damp, grip = grip, susp = susp, body = body, scaled = scaled, wheels = wheels,
                    minNy = probe.MinNormalY, contacts = probe.Contacts, terrain = probe.Terrain, wiped = wiped, slip = Vector3.SignedAngle(Flat(fwd), Flat(vel), Vector3.up),
                    touched = string.Join("|", probe.Touched), triggers = string.Join("|", probe.Triggers)
                };
                row.resid = dv - (drive + grav + damp + grip + susp + body + scaled);
                rows.Add(row);
                stuck = nspeed < 1f ? stuck + dt : 0f;
                if (stuck > 3f || Vector3.Angle(car.transform.up, Vector3.up) > 75f) break;
                float sideEnd = Vector3.Dot(car.transform.position - end, endDir);
                if (travelled > 150f && prevSide < 0f && sideEnd >= 0f && Flat(car.transform.position - end).magnitude < 20f) { done = true; break; }
                prevSide = sideEnd;
            }
            car.enabled = true;
        }

        // the slowest speed the bends ahead allow (lateral 14 m/s2), less what braking at 14 m/s2 can take off before them
        float TargetSpeed(List<Vector3> line, Vector3 pos)
        {
            int near = Nearest(line, pos); float best = 999f, dist = 0f;
            for (int k = near; k < line.Count - 4 && dist < 160f; k++)
            {
                if (k > near) dist += Flat(line[k] - line[k - 1]).magnitude;
                if (k < 3) continue;
                var a = Flat(line[k] - line[k - 3]); var b = Flat(line[k + 3] - line[k]);
                float ang = Vector3.Angle(a, b) * Mathf.Deg2Rad; float len = (a.magnitude + b.magnitude) * 0.5f;
                float radius = ang < 1e-3f ? 1e4f : len / ang;
                float vb = Mathf.Sqrt(14f * radius);
                best = Mathf.Min(best, Mathf.Sqrt(vb * vb + 2f * 14f * dist));
            }
            return best;
        }

        static int Nearest(List<Vector3> line, Vector3 pos)
        {
            int near = 0; float bd = float.MaxValue;
            for (int i = 0; i < line.Count; i++) { float d = Flat(line[i] - pos).sqrMagnitude; if (d < bd) { bd = d; near = i; } }
            return near;
        }

        float Steer(List<Vector3> line, Vector3 pos)
        {
            float v = Mathf.Max(car.ForwardSpeed, 1f);
            int near = Nearest(line, pos);
            float look = 6f + 0.35f * v; int k = near; float acc = 0f;
            while (k < line.Count - 1 && acc < look) { acc += Flat(line[k + 1] - line[k]).magnitude; k++; }
            float ang = Vector3.SignedAngle(Flat(car.transform.forward), Flat(line[k] - pos), Vector3.up);
            return Mathf.Clamp(ang / 20f, -1f, 1f);
        }

        void Summarise(string label, List<Row> rows, List<Vector3> line, bool fromRest)
        {
            if (rows.Count == 0) { report.AppendLine($"  {label}: no data"); return; }
            var last = rows[rows.Count - 1];
            report.AppendLine($"  {label}: {(done ? "finished" : "DID NOT FINISH")} in {last.t:0.0} s over {last.s:0} m; top {rows.Max(q => q.v):0.0} m/s; average {last.s / last.t:0.0} m/s; slowest after 1 s {rows.Where(q => q.t > 1f).Select(q => q.v).DefaultIfEmpty(0f).Min():0.0} m/s");
            if (fromRest)
            {
                foreach (float f in new[] { 20f, 30f, 40f, 50f, 55f })
                {
                    var hit = rows.FirstOrDefault(q => q.v >= f);
                    report.Append(hit.t > 0f ? $"    {f:0} m/s at {hit.t:0.0} s ({hit.s:0} m)" : $"    {f:0} m/s not reached");
                }
                report.AppendLine();
                // the same speed on flat, smooth ground by the motor's own formula and the body's damping
                float v = 0f, t = 0f, s = 0f, dt = Time.fixedDeltaTime, top = car.topSpeed, acc = car.acceleration, d = car.Body.linearDamping;
                var marks = new List<string>();
                foreach (float f in new[] { 20f, 30f, 40f, 50f, 55f })
                {
                    while (v < f && t < 60f) { v += acc * Mathf.Clamp01(1 - v / top) * dt; v /= 1f + dt * d; s += v * dt; t += dt; }
                    marks.Add(t < 60f ? $"{f:0} m/s at {t:0.0} s ({s:0} m)" : $"{f:0} m/s never");
                }
                report.AppendLine("    flat, smooth ground (formula): " + string.Join("   ", marks));
            }
            // the speed lost with the throttle held, by cause
            var held = rows.Where(q => q.thr > 0.95f && q.dv < 0f).ToList();
            float lost = held.Sum(q => q.dv);
            report.AppendLine($"  Speed lost with the throttle held: {-lost:0.0} m/s in {held.Count} of {rows.Count(q => q.thr > 0.95f)} full-throttle steps. Of the losing steps, summed by cause (negative = took speed):");
            report.AppendLine($"    drive {held.Sum(q => q.drive):+0.0;-0.0}, gravity {held.Sum(q => q.grav):+0.0;-0.0}, damping {held.Sum(q => q.damp):+0.0;-0.0}, lateral grip {held.Sum(q => q.grip):+0.0;-0.0}, springs {held.Sum(q => q.susp):+0.0;-0.0}, BODY contacts {held.Sum(q => q.body):+0.0;-0.0}, geese / breakables {held.Sum(q => q.scaled):+0.0;-0.0}, other {held.Sum(q => q.resid):+0.0;-0.0}");
            var all = rows.Where(q => q.thr > 0.95f).ToList();
            report.AppendLine($"  Over every full-throttle step: body contact in {all.Count(q => q.contacts > 0)} steps ({all.Count(q => q.terrain)} with the terrain), body impulse {all.Sum(q => q.body):+0.0;-0.0} m/s; fewer than 2 wheels down in {all.Count(q => q.wheels < 2)} steps; wiped out in {all.Count(q => q.wiped)}; triggers: {string.Join(", ", rows.Where(q => q.triggers.Length > 0).Select(q => q.triggers).Distinct().Take(12))}");
            var touched = rows.Where(q => q.touched.Length > 0).SelectMany(q => q.touched.Split('|')).GroupBy(n => n).OrderByDescending(g => g.Count()).Take(8).Select(g => $"{g.Key} x{g.Count()}");
            report.AppendLine($"  Body touched: {string.Join(", ", touched)}. Contact corrections so far: terrain {VehicleSurfaceContacts.TerrainCorrections}, ramps / decks {VehicleSurfaceContacts.RampCorrections}.");
            // the biggest drops: runs of consecutive full-throttle steps that lose speed
            var events = new List<(int a, int b, float loss)>();
            for (int i = 0; i < rows.Count; i++)
            {
                if (!(rows[i].thr > 0.95f && rows[i].dv < 0f)) continue;
                int j = i; float l = 0f;
                while (j < rows.Count && rows[j].thr > 0.95f && rows[j].dv < 0.02f) { l += rows[j].dv; j++; }
                events.Add((i, j - 1, l)); i = j;
            }
            foreach (var e in events.OrderBy(e => e.loss).Take(6))
            {
                var seg = rows.GetRange(e.a, e.b - e.a + 1);
                var q0 = rows[e.a];
                report.AppendLine($"    drop {-e.loss:0.0} m/s over {seg.Count * Time.fixedDeltaTime:0.00} s at ({q0.x:0},{q0.z:0}) {q0.v:0.0} m/s: grade {seg.Average(q => q.grade):0.0}%, wheels min {seg.Min(q => q.wheels)}, body contact {seg.Count(q => q.contacts > 0)} steps ({seg.Count(q => q.terrain)} terrain, lowest contact normal y {seg.Where(q => q.contacts > 0).Select(q => q.minNy).DefaultIfEmpty(1f).Min():0.00}), "
                    + $"by cause: drive {seg.Sum(q => q.drive):+0.0;-0.0} gravity {seg.Sum(q => q.grav):+0.0;-0.0} damping {seg.Sum(q => q.damp):+0.0;-0.0} grip {seg.Sum(q => q.grip):+0.0;-0.0} springs {seg.Sum(q => q.susp):+0.0;-0.0} body {seg.Sum(q => q.body):+0.0;-0.0} scaled {seg.Sum(q => q.scaled):+0.0;-0.0} other {seg.Sum(q => q.resid):+0.0;-0.0}"
                    + $"{(seg.Any(q => q.wiped) ? ", WIPED OUT" : "")}{(seg.Any(q => q.triggers.Length > 0) ? ", triggers " + string.Join("|", seg.Select(q => q.triggers).Where(n => n.Length > 0).Distinct()) : "")}");
            }
        }

        void Write(string name, List<Row> rows)
        {
            var sb = new StringBuilder("t\ts\tx\ty\tz\tspeed\tfwd\tthrottle\tbrake\tgrade_pct\twheels\tbody_contacts\tterrain\ttouched\ttriggers\twiped\tup_y\tmin_normal_y\tslip_deg\tdv\tdrive\tgravity\tdamping\tgrip\tsprings\tbody\tscaled\tother\n");
            foreach (var q in rows)
                sb.AppendLine(string.Join("\t", new[] { q.t, q.s, q.x, q.y, q.z, q.v, q.vf, q.thr, q.brk, q.grade }.Select(f => f.ToString("0.###", IC)))
                    + $"\t{q.wheels}\t{q.contacts}\t{(q.terrain ? 1 : 0)}\t{q.touched}\t{q.triggers}\t{(q.wiped ? 1 : 0)}\t"
                    + string.Join("\t", new[] { q.upY, q.minNy, q.slip, q.dv, q.drive, q.grav, q.damp, q.grip, q.susp, q.body, q.scaled, q.resid }.Select(f => f.ToString("0.####", IC))));
            File.WriteAllText(Path.Combine(outputDir, name), sb.ToString());
        }

        static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }
    }
}
