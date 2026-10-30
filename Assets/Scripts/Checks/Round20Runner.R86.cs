using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Racer;
using UnityEngine;

namespace ChastainRush
{
    // Round 86 in the exe (-exeRound86 <dir> [-r86Only p86jumps,p86reset,p86trees,p86fps] [-steamDeck 1] -r20Minutes 120 -logFile <dir>\Player.log),
    // from the repo root, on the check's own save. CHECK IT AS DAN PLAYS IT.
    //   p86jumps  A: every car course both ways in HIGH MODE: the jumps the bumpers leave room for (HighBumpers.Jumps), each flown by the human-like
    //             driver (the Street Classic) on the line and 3 m either side: bumper touches from 10 m before the lip to 3 s after it is down;
    //             pictures of each course's first jump (the run-in, in the air, landed)
    //   p86reset  B: the Glen loop, Kennesaw and Marietta Square, at every checkpoint: the reset (Y) from 15 m before, at, 15 m past and 60 m past
    //             it (5 m off the line): where it is put (ahead of the next checkpoint?) and any penalty; the Quarry Air Race's resets and crashes
    //   p86trees  C: a thin trunk at 12 m/s, a big one at 12 and at 30 m/s, by the game's input (throttle held); break, speed kept, no wreck;
    //             all back after driving 200 m away
    //   p86fps    C: the frame rate in the woods (Noonday Creek, Kennesaw) standing, then with a dozen trees down in view
    public sealed partial class Round20Runner
    {
        static readonly string[] R86Courses = { "kennesaw_mountain.json", "chastain_glen_loop.json", "noonday_creek_trail.json", "noonday_creek.json", "chastain_ridge.json", "chastain_lakes.json", "addison_heights.json", "marietta_square.json" };

        // ---------------------------------------------------------------- A: room for jumps between the HIGH bumpers
        IEnumerator R86Jumps()
        {
            report.AppendLine(); report.AppendLine("=== A. Every jump on every car course in HIGH MODE, the human-like driver (Street Classic) on the line and 3 m either side");
            int touchedRuns = 0, runs = 0, jumpsAll = 0;
            foreach (var file in R86Courses)
                foreach (var dir in new[] { "forward", "reverse" })
                {
                    R84High(true); yield return Vehicle("original");
                    yield return StartRoute(file, dir, 0, 1);
                    if (race.Route == null || race.Route.Direction != dir) { report.AppendLine($"  {file} {dir}: not raced this way"); race.EndRace(); continue; }
                    var jumps = HighBumpers.Jumps.ToList(); jumpsAll += jumps.Count;
                    var r = race.Route; float L = r.LapLength;
                    report.AppendLine($"  {file} {dir}: {HighBumpers.Report}");
                    // the jumps in the lap's order from the line (the first one is the course's "first jump")
                    var ordered = jumps.OrderBy(j => j.line == 0 ? r.LapStationOf(j.lipAt) : 1e6f + j.lip).ToList();
                    int idx = 0;
                    foreach (var j in ordered)
                    {
                        idx++; string where = j.line == 0 ? $"lap {r.LapStationOf(j.lipAt):0} m" : $"shortcut line {j.line}";
                        report.AppendLine($"    jump {idx} ({where}): lip ({j.lipAt.x:0}, {j.lipAt.z:0}), furthest landing ({j.landAt.x:0}, {j.landAt.z:0}), {Mathf.Max(0f, j.land - j.lip):0} m, up to {j.air:0.0} s and {j.clear:0.0} m over the ground at the fastest");
                        foreach (float off in new[] { 0f, -3f, 3f })
                        {
                            bool pics = idx == 1 && off == 0f; string tag = $"86_{file.Replace(".json", "")}_{dir}_first";
                            var res = new R86JumpRun(); yield return R86FlyJump(j, off, res, pics ? tag : null); runs++;
                            if (res.touch > 0) touchedRuns++;
                            report.AppendLine($"      {(off == 0 ? "on the line" : off < 0 ? "3 m left " : "3 m right")}: top {res.top:0} m/s, in the air {res.air:0.0} s, bumper touches in the air or landing {res.touch}{(res.touch > 0 ? $" at ({res.touchAt.x:0}, {res.touchAt.z:0})" : "")}{(res.note.Length > 0 ? "; " + res.note : "")}");
                        }
                    }
                    race.EndRace(); R84High(false);
                }
            Check("A: no bumper touched in the air or on a landing (every jump, on the line and 3 m either side)", touchedRuns == 0, $"{touchedRuns} of {runs} runs touched a bumper; {jumpsAll} jumps");
        }
        sealed class R86JumpRun { public float top, air; public int touch; public Vector3 touchAt; public string note = ""; }
        IEnumerator R86FlyJump(HighBumpers.Jump j, float off, R86JumpRun res, string pics)
        {
            var line = new List<Vector3>();
            for (float s = j.lip - 140f; s <= j.lip + Mathf.Max(0f, j.land - j.lip) + 70f; s += 4f)
            {
                float q = j.loop ? Mathf.Repeat(s, j.length) : Mathf.Clamp(s, 0f, j.length - .01f);
                var (p, f) = j.at(q); f = Vector3.ProjectOnPlane(f, Vector3.up).normalized;
                line.Add(p + Vector3.Cross(Vector3.up, f) * off);
            }
            var (p0, f0) = j.at(j.loop ? Mathf.Repeat(j.lip - 140f, j.length) : Mathf.Max(0f, j.lip - 140f));
            Place(RaceRoute.Surface(line[0].x, line[0].z, p0.y), line[1] - line[0], 20f);
            yield return new WaitForSecondsRealtime(.3f);
            var bumpers = FindObjectsByType<MeshCollider>(FindObjectsSortMode.None).Where(c => HighBumpers.IsBumper(c) && c.enabled && c.gameObject.activeInHierarchy).ToList();
            var own = car.GetComponentsInChildren<Collider>().Where(c => !c.isTrigger && c.enabled).ToList();
            var run = new HumanRun(); bool flown = false, down = false; float downAt = -1f, airNow = 0f; bool shotIn = false, shotAir = false, shotDown = false; var lipFlat = Flat(j.lipAt);
            float lipS = 140f, landS = 140f + Mathf.Max(0f, j.land - j.lip);
            var cum = Cum(line);
            yield return HumanDrive(line, 40f, 40f, run, () =>
            {
                var p = car.Body.position; float s = ProjectNear(line, cum, p, 0f, out _);
                res.top = Mathf.Max(res.top, car.Body.linearVelocity.magnitude);
                if (pics != null && !shotIn && s > lipS - 40f) { shotIn = true; StartCoroutine(Capture(pics + "_runin")); }
                if (car.GroundedWheels == 0 && s > lipS - 15f) { airNow += Time.fixedDeltaTime; if (airNow > .3f) flown = true; res.air = Mathf.Max(res.air, airNow); if (pics != null && !shotAir && airNow > .6f) { shotAir = true; StartCoroutine(Capture(pics + "_air")); } }
                else { if (flown && !down) { down = true; downAt = Time.time; } airNow = 0f; }
                if (pics != null && down && !shotDown && Time.time - downAt > .5f) { shotDown = true; StartCoroutine(Capture(pics + "_landed")); }
                bool watch = s > lipS - 10f && (!down || Time.time - downAt < 3f);
                if (watch)
                    foreach (var c in own)
                        foreach (var b in bumpers)
                        {
                            if (!c.bounds.Intersects(b.bounds)) continue;
                            if (Physics.ComputePenetration(c, c.transform.position, c.transform.rotation, b, b.transform.position, b.transform.rotation, out _, out float d) && d > .01f)
                            { if (res.touch == 0) res.touchAt = p; res.touch++; goto next; }
                        }
                next:
                return (down && Time.time - downAt > 3f) || s > landS + 60f;
            });
            if (!flown) res.note = "never left the ground (too slow for it, or not a jump at this speed)";
            if (run.resets > 0) res.note += $" reset {run.resets}";
        }

        // ---------------------------------------------------------------- B: a reset never skips a checkpoint
        IEnumerator R86Reset()
        {
            report.AppendLine(); report.AppendLine("=== B. Resets (Y, the game's input) just before, at and past every checkpoint: the next checkpoint must still be ahead, no penalty");
            int bad = 0, total = 0, pen = 0; R73Pad();
            foreach (var file in new[] { "chastain_glen_loop.json", "kennesaw_mountain.json", "marietta_square.json" })
            {
                yield return Vehicle("original"); yield return StartRoute(file, "forward", 0, 3);
                var r = race.Route; float L = r.LapLength; var e = race.Player; int n = r.Checkpoints.Count;
                e.ClockStarted = true; e.StartAt = e.LapStart = race.RaceClock;
                report.AppendLine($"  {file}: lap {L:0} m, {n} checkpoints");
                for (int k = 0; k <= n; k++)
                {
                    float cs = k < n ? r.Checkpoints[k].S : L; string name = k < n ? $"CP{k + 1}" : "the line";
                    foreach (float d in new[] { -15f, 0f, 15f, 60f })
                    {
                        e.NextCheckpoint = k; e.Forgiven.Clear(); float pen0 = e.PenaltyTotal; int miss0 = e.MissedTotal;
                        var p = r.PointAt(r.GateS + cs + d, out var f); var side = Vector3.Cross(Vector3.up, f).normalized;
                        var at = RaceRoute.Surface(p.x + side.x * 5f, p.z + side.z * 5f, p.y);
                        respawn.PlaceAt(at + Vector3.up * .3f, Mathf.Atan2(f.x, f.z) * Mathf.Rad2Deg); car.Body.linearVelocity = Vector3.zero; race.NotifyTeleported();
                        yield return new WaitForFixedUpdate();
                        yield return R73Reset(); total++;
                        var q = car.Body.position; float st = r.LapStationOf(q);
                        float ahead = Mathf.Repeat(cs - st, L); if (k == n && ahead < .5f) ahead = L;
                        bool ok = ahead > 1f && ahead < .5f * L + 1f && e.NextCheckpoint == k;
                        bool penal = e.PenaltyTotal > pen0 || e.MissedTotal > miss0; if (penal) pen++;
                        if (!ok) bad++;
                        if (!ok || penal || d == 60f) report.AppendLine($"    {name} {(d < 0 ? $"{-d:0} m before" : d == 0 ? "at it" : $"{d:0} m past")}: put at lap {st:0} m, {ahead:0} m short of {name}, next checkpoint {(e.NextCheckpoint < n ? $"CP{e.NextCheckpoint + 1}" : "the line")}{(penal ? $", PENALTY +{e.PenaltyTotal - pen0:0} s" : "")}{(ok ? "" : "  <- WRONG")}");
                        if (file == "kennesaw_mountain.json" && k == 1 && d == 15f) yield return Capture("86_reset_kennesaw_cp2_after");
                    }
                }
                race.EndRace();
            }
            Check("B: every reset puts the car before the next checkpoint", bad == 0, $"{bad} of {total} wrong");
            Check("B: no penalty from a reset", pen == 0, $"{pen} resets charged a penalty");

            // the Quarry Air Race: the reset button and three crashes in a row before the same ring
            report.AppendLine("  quarry_air_race.json: the reset button and crashes at every ring");
            yield return Vehicle("plane"); yield return StartRoute("quarry_air_race.json", "forward", 0, 1);
            {
                var r = race.Route; float L = r.LapLength; var e = race.Player; int n = r.Checkpoints.Count; e.ClockStarted = true; int airBad = 0;
                var plane = car.GetComponent<PlaneFlight>();
                for (int k = 1; k < n; k++)
                {
                    e.NextCheckpoint = k; float pen0 = e.PenaltyTotal;
                    bool ok1 = race.AirReset(plane, out var pos, out _); float st = r.LapStationOf(pos); float ahead = Mathf.Repeat(r.Checkpoints[k].S - st, L);
                    for (int c = 0; c < 3; c++) race.AirReset(plane);
                    yield return new WaitForFixedUpdate();
                    float st2 = r.LapStationOf(car.Body.position); float ahead2 = Mathf.Repeat(r.Checkpoints[k].S - st2, L);
                    bool ok = ok1 && ahead > 1f && ahead < .5f * L && ahead2 > 1f && ahead2 < .5f * L && e.NextCheckpoint == k && e.PenaltyTotal == pen0;
                    if (!ok) { airBad++; report.AppendLine($"    ring {k + 1}: reset {ahead:0} m short, after 3 crashes {ahead2:0} m short, next ring {e.NextCheckpoint + 1}, penalty {e.PenaltyTotal - pen0:0} s  <- WRONG"); }
                }
                Check("B: air race resets and repeated crashes stay before the next ring, no penalty", airBad == 0, $"{airBad} of {n - 1} rings wrong");
                race.EndRace();
            }
        }

        // ---------------------------------------------------------------- C: trees break as in Woodstock Rush
        IEnumerator R86Trees()
        {
            report.AppendLine(); report.AppendLine($"=== C. Breakable trees (Woodstock Rush's numbers: {TreeTrunks.Needed(.5f):0} m/s for 0.5 m, {TreeTrunks.Needed(1f):0} m/s for 1 m), Free Roam, throttle held through the game's input");
            if (race.State != RaceController.Stage.Free) race.EndRace();
            yield return Vehicle("original");
            var field = FindAnyObjectByType<TreeField>(); var tt = TreeTrunks.Instance;
            if (!field || !tt) { Check("C: the tree field", false); yield break; }
            // widths on the map
            var widths = new List<float>(); for (int i = 0; i < field.count; i += 37) widths.Add(tt.TrunkWidth(i)); widths.Sort();
            report.AppendLine($"  trunks on the map: {widths.First():0.00} to {widths.Last():0.00} m, median {widths[widths.Count / 2]:0.00} m (needs {TreeTrunks.Needed(widths[widths.Count / 2]):0.0} m/s)");
            int thin = PickTree(field, tt, w => w <= .45f, out var thinYaw), big = PickTree(field, tt, w => w >= .68f, out var bigYaw);
            if (thin < 0 || big < 0) { Check("C: a clear thin and a clear big tree found", false, $"thin {thin}, big {big}"); yield break; }
            var cases = new[] { (thin, thinYaw, 12f, true, "thin"), (big, bigYaw, 12f, false, "big"), (big, bigYaw, 30f, true, "big") };
            int fails = 0; var downIdx = new List<int>();
            foreach (var (i, yaw, speed, shouldBreak, what) in cases)
            {
                float w = tt.TrunkWidth(i); var t = tt.TreeAt(i); var fwd = Quaternion.Euler(0, yaw, 0) * Vector3.forward;
                var start = RaceRoute.Surface(t.x - fwd.x * 45f, t.z - fwd.z * 45f);
                Place(start, fwd, speed); tt.Refresh(start); yield return new WaitForFixedUpdate();
                R73Pad(); float hit = -1f, before = speed, after = -1f; bool wrecked = false; float t0 = Time.time; int s0 = TreeTrunks.Snapped;
                while (Time.time - t0 < 8f)
                {
                    yield return new WaitForFixedUpdate();
                    var v = Flat(car.Body.linearVelocity); float d = Flat(t - car.Body.position).magnitude;
                    // hold the speed by the game's input until the tree (throttle and brake), straight at it
                    R73Set(Mathf.Clamp(Vector3.SignedAngle(Flat(car.transform.forward), Flat(t - car.Body.position), Vector3.up) / 20f, -1f, 1f) * (d > 6f ? 1f : 0f), v.magnitude < speed - .5f ? 1f : v.magnitude < speed + .5f ? .5f : 0f, v.magnitude > speed + 3f ? .5f : 0f);
                    if (d < 8f && hit < 0f) before = v.magnitude;
                    if (hit < 0f && (TreeTrunks.Snapped > s0 || d < 3.2f)) hit = Time.time;
                    if (hit > 0f && Time.time - hit > .5f) { after = v.magnitude; wrecked = car.transform.up.y < .5f; break; }
                }
                R73Set(0, 0, 0);
                bool broke = tt.IsDown(i); if (broke) downIdx.Add(i);
                bool ok = broke == shouldBreak && !wrecked && (!broke || after > before * .45f);
                if (!ok) fails++;
                report.AppendLine($"  {what} trunk {w:0.00} m (needs {TreeTrunks.Needed(w):0.0} m/s) at {before:0.0} m/s: {(broke ? "BROKE" : "held")}, speed 0.5 s after {after:0.0} m/s, {(wrecked ? "WRECKED" : "upright")}; {TreeTrunks.Last}");
                yield return Capture($"86_tree_{what}_{speed:0}");
                if (broke) { yield return new WaitForSecondsRealtime(1.2f); yield return Capture($"86_tree_{what}_{speed:0}_fallen"); }
            }
            Check("C: a thin trunk breaks at 12 m/s, a big one holds at 12 and breaks at 30, no wreck", fails == 0, $"{fails} of 3 wrong");
            // drive away: every broken tree stands again past 160 m
            int downNow = tt.Down; var away = car.Body.position + car.transform.forward * 220f;
            var road = RoadLookup.Instance; if (road && road.NearestPoint(away, out var c, out _)) away = RaceRoute.Surface(c.x, c.y); else away = RaceRoute.Surface(away.x, away.z);
            Place(away, car.transform.forward, 0f); yield return new WaitForSecondsRealtime(5f);
            bool allBack = downIdx.All(i => !tt.IsDown(i));
            Check("C: the broken trees stand again 160 m away", allBack, $"down before {downNow}, now {tt.Down}, regrown {TreeTrunks.Regrown}");
        }
        // a tree of the given width with a clear, gentle 45 m run-in to it (no other trunk within 2.5 m of the path, nothing built on it)
        int PickTree(TreeField field, TreeTrunks tt, System.Func<float, bool> ok, out float yaw)
        {
            yaw = 0f; var centre = new Vector3(1000f, 0f, 700f);
            var order = Enumerable.Range(0, field.count).Where(i => ok(tt.TrunkWidth(i))).OrderBy(i => Flat(tt.TreeAt(i) - centre).sqrMagnitude).Take(3000);
            foreach (int i in order)
            {
                var t = tt.TreeAt(i);
                for (int h = 0; h < 8; h++)
                {
                    float y = h * 45f; var f = Quaternion.Euler(0, y, 0) * Vector3.forward; bool clear = true;
                    for (float d = 3f; d <= 48f && clear; d += 2f)
                    {
                        var q = t - f * d; if (tt.AnyTrunk(q, 2.5f)) { clear = false; break; }
                        var g = RaceRoute.Surface(q.x, q.z); if (Mathf.Abs(g.y - t.y) > 4f || ShallowWater.InWater(g + Vector3.up * .1f)) { clear = false; break; }
                        if (Physics.CheckBox(g + Vector3.up * 1.2f, new Vector3(1.4f, .8f, 1.2f), Quaternion.LookRotation(f), ~0, QueryTriggerInteraction.Ignore)) { if (Physics.OverlapBox(g + Vector3.up * 1.2f, new Vector3(1.4f, .8f, 1.2f), Quaternion.LookRotation(f), ~0, QueryTriggerInteraction.Ignore).Any(cc => !(cc is TerrainCollider) && cc.attachedRigidbody != car.Body && !TreeTrunks.IsTrunk(cc))) { clear = false; break; } }
                    }
                    if (clear) { yaw = y; return i; }
                }
            }
            return -1;
        }

        IEnumerator R86Fps()
        {
            report.AppendLine(); report.AppendLine($"=== C. The frame rate in the woods ({Screen.width} x {Screen.height}, not measured at 3840 x 2160; Steam Deck setting {(SteamDeck.On ? "ON" : "off")})");
            if (race.State != RaceController.Stage.Free) race.EndRace();
            int vs = QualitySettings.vSyncCount, cap = Application.targetFrameRate; QualitySettings.vSyncCount = 0; Application.targetFrameRate = -1;
            var tt = TreeTrunks.Instance; var field = FindAnyObjectByType<TreeField>();
            IEnumerator Measure(string label)
            {
                yield return new WaitForSecondsRealtime(2f);
                var frames = new List<float>(); float t0 = Time.realtimeSinceStartup;
                while (Time.realtimeSinceStartup - t0 < 6f) { yield return null; frames.Add(Time.unscaledDeltaTime); }
                var sorted = frames.OrderBy(x => x).ToList(); float p99 = sorted[Mathf.Min(sorted.Count - 1, (int)(sorted.Count * 0.99f))];
                report.AppendLine($"INFO  {label}: {frames.Count / frames.Sum():0} fps average, slowest 1 % {1f / p99:0} fps; trees down {tt.Down}, falling / lying {FallingTree.Count}");
            }
            foreach (var (name, file) in new[] { ("Noonday Creek woods", "noonday_creek.json"), ("Kennesaw Mountain trail", "kennesaw_mountain.json") })
            {
                yield return StartRoute(file, "forward", 0, 1); var r = race.Route; var p = r.PointAt(r.GateS + r.LapLength * .35f, out var f); race.EndRace();
                tt.RegrowAll(); Place(RaceRoute.Surface(p.x, p.z), f, 0f); tt.Refresh(car.Body.position);
                yield return Measure($"{name} ({p.x:0}, {p.z:0}), standing");
                // a dozen trees in view ahead (25 to 80 m), broken the way the car faces
                var ahead = new List<int>(); var fw = Flat(car.transform.forward).normalized;
                for (int i = 0; i < field.count && ahead.Count < 400; i++) { var t = tt.TreeAt(i); var d = Flat(t - car.Body.position); float a = Vector3.Dot(d, fw); if (a > 20f && a < 80f && Mathf.Abs(Vector3.Dot(d, Vector3.Cross(Vector3.up, fw))) < a * .5f) ahead.Add(i); }
                foreach (int i in ahead.OrderBy(i => Flat(tt.TreeAt(i) - car.Body.position).sqrMagnitude).Take(12)) tt.Break(i, null, fw, 20f);
                yield return new WaitForSecondsRealtime(.4f); yield return Capture($"86_fps_{file.Replace(".json", "")}_falling{(SteamDeck.On ? "_deck" : "")}");
                yield return Measure($"{name}, 12 trees down in view");
                tt.RegrowAll();
            }
            QualitySettings.vSyncCount = vs; Application.targetFrameRate = cap;
        }
    }
}
