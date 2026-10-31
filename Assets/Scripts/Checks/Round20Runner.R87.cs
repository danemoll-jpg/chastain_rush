using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Racer;
using UnityEngine;

namespace ChastainRush
{
    // Round 87 in the exe (-exeRound87 <dir> [-r87Only p87pics,p87layout,p87drive,p87out,p87mall,p87addison,p87climb] [-r87Old] -r20Minutes 240
    // -logFile <dir>\Player.log), from the repo root, on the check's own save. CHECK IT AS DAN PLAYS IT. -r87Old: Round 86's bumpers, barriers, resets and
    // shrub beds (the before).
    //   p87pics     Dan's eleven places (BUG-001 to 011) in his HIGH MODE races (Addison Heights forward, the Noonday Creek Trail forward), from his
    //               vehicle's spot and heading, the player's camera
    //   p87layout   A: every car course both ways, HIGH MODE: every bumper piece and closure barrier block inside a course's driven width (its paved
    //               half and 1 m) whose face is more than 25 degrees off the line there; the bumper ends inside it
    //   p87drive    A: the human-like driver (the Drifter Twin, Dan's) on the line and 3 m either side: a HIGH lap of Addison Heights and the trail race's
    //               stretch from Ring Road to Barrett Parkway (Dan's BUG-007 to 011); touches of a bumper's or a barrier's end
    //   p87out      B: jumped out over a bumper on three courses (Addison Heights, the Glen loop, Kennesaw): put back on the course, next checkpoint ahead
    //   p87mall     C: resets at five places through Town Center (both floors, both directions, the Carroway shortcut): inside the mall, next checkpoint ahead
    //   p87addison  D: Joel Drive and the Lyndon jump driven through Dan's places: speed through the terrace's bushes, jolts, bumper touches
    //   p87climb    E: the Kennesaw climb at full throttle (the Drifter Twin, the Street Classic, the Skyfin), normal and HIGH: air, off the road
    public sealed partial class Round20Runner
    {
        static readonly (string bug, string file, Vector3 at, float yaw)[] R87Places =
        {
            ("BUG-001", "addison_heights.json", new Vector3(3222.83f, 0, 532.17f), 120.06f), ("BUG-002", "addison_heights.json", new Vector3(3285.78f, 0, 306.19f), 258.79f),
            ("BUG-003", "addison_heights.json", new Vector3(2961.15f, 0, 415.00f), 252.82f), ("BUG-004", "addison_heights.json", new Vector3(2815.27f, 0, 504.51f), 36.3f),
            ("BUG-005", "addison_heights.json", new Vector3(2969.21f, 0, 410.78f), 73.82f), ("BUG-006", "addison_heights.json", new Vector3(2880.68f, 0, 414.07f), 122.87f),
            ("BUG-007", "noonday_creek_trail.json", new Vector3(796.43f, 0, -1685.07f), 102.77f), ("BUG-008", "noonday_creek_trail.json", new Vector3(1307.00f, 0, -1669.81f), 78.15f),
            ("BUG-009", "noonday_creek_trail.json", new Vector3(430.01f, 0, -1512.79f), 143.91f), ("BUG-010", "noonday_creek_trail.json", new Vector3(426.21f, 0, -1500.87f), 158.71f),
        };
        string R87Tag => HighBumpers.Old ? "before" : "after";
        static void R87Old(bool on) { HighBumpers.Old = RaceBarriers.Old = CarRecovery.Old = ShrubBeds.Old = on; }

        // ---------------------------------------------------------------- Dan's places
        IEnumerator R87Pics()
        {
            report.AppendLine(); report.AppendLine($"=== Dan's places, HIGH MODE, his vehicle's spot and heading ({R87Tag})");
            yield return Vehicle("drifter");
            foreach (var g in R87Places.GroupBy(p => p.file))
            {
                R84High(true); yield return StartRoute(g.Key, "forward", 0, 3);
                foreach (var (bug, _, at, yaw) in g)
                {
                    var p = RaceRoute.Surface(at.x, at.z); Place(p, Quaternion.Euler(0, yaw, 0) * Vector3.forward, 0f);
                    yield return new WaitForSecondsRealtime(1.2f); car.Body.linearVelocity = Vector3.zero;
                    yield return Capture($"87_{bug}_{R87Tag}");
                    report.AppendLine($"  {bug} ({at.x:0.0}, {at.z:0.0}) heading {yaw:0}: picture 87_{bug}_{R87Tag}.png; within 25 m: {R87Near(p, 25f)}");
                }
                race.EndRace(); R84High(false);
            }
        }
        // what stands near a place: bumper runs (their ends), closure rows
        string R87Near(Vector3 p, float r)
        {
            var info = HighBumpers.Current; int runs = 0, ends = 0;
            if (info != null) foreach (var s in info.strips) { if (s.Any(q => Flat(q - p).magnitude < r)) runs++; if (Flat(s[0] - p).magnitude < r) ends++; if (Flat(s[^1] - p).magnitude < r) ends++; }
            int bars = race.Barriers ? race.Barriers.Built.Count(b => b.Root && b.Root.GetComponentsInChildren<Renderer>().Any(x => Flat(x.transform.position - p).magnitude < r)) : 0;
            return $"{runs} bumper runs ({ends} ends), {bars} closure barriers";
        }

        // ---------------------------------------------------------------- A: nothing faces you
        sealed class R87Line { public List<Vector3> p = new List<Vector3>(); public List<Vector3> f = new List<Vector3>(); public List<float> half = new List<float>(); }
        List<R87Line> R87Lines()
        {
            var lines = new List<R87Line>(); var roads = RoadLookup.Instance;
            void Add(RaceRoad road, bool lap)
            {
                if (road == null || road.Length <= 0) return; var l = new R87Line();
                for (float s = 0; s < road.Length; s += 2f)
                {
                    var p = road.At(s, out var f); float h = lap ? road.HalfWidth(s) : 3.5f;
                    if (lap && roads && roads.Nearest(p, out _, out float d, out float hh) && d < hh + .5f) h = Mathf.Clamp(hh, 2.5f, 12f);
                    l.p.Add(p); l.f.Add(Flat(f).normalized); l.half.Add(h);
                }
                lines.Add(l);
            }
            Add(race.AiRoad, true); foreach (var sc in race.AiShortcuts) Add(sc.Line, false);
            return lines;
        }
        // the nearest station of any line to q (within its driven width + slack): the angle of 'dir' off that line, -1 if q is outside every driven width
        static float R87Off(List<R87Line> lines, Vector3 q, Vector3 dir, float slack)
        {
            float worst = -1f;
            foreach (var l in lines)
                for (int i = 0; i < l.p.Count; i++)
                {
                    float dx = l.p[i].x - q.x, dz = l.p[i].z - q.z, r = l.half[i] + slack; if (dx * dx + dz * dz > r * r || Mathf.Abs(l.p[i].y - q.y) > 3f) continue;
                    float a = Vector3.Angle(Flat(dir), l.f[i]); a = Mathf.Min(a, 180f - a);
                    worst = worst < 0f ? a : Mathf.Min(worst, a);   // along any line passing here is fine (a junction of two passes)
                }
            return worst;
        }
        IEnumerator R87Layout()
        {
            report.AppendLine(); report.AppendLine($"=== A. Every car course both ways, HIGH MODE ({R87Tag}): bumper pieces and closure barrier blocks inside a course's driven width (paved half + 1 m) more than 25 degrees off the line; bumper ends inside it");
            int badAll = 0, endsAll = 0, barsAll = 0;
            yield return Vehicle("drifter");
            foreach (var file in R86Courses)
                foreach (var dir in new[] { "forward", "reverse" })
                {
                    R84High(true); yield return StartRoute(file, dir, 0, 1);
                    if (race.Route == null || race.Route.Direction != dir) { race.EndRace(); continue; }
                    var lines = R87Lines(); var info = HighBumpers.Current; int bad = 0, ends = 0, bars = 0; var where = new List<string>();
                    if (info != null)
                        foreach (var s in info.strips)
                        {
                            for (int i = 0; i + 1 < s.Length; i++)
                            {
                                var m = (s[i] + s[i + 1]) * .5f; float a = R87Off(lines, m - Vector3.zero, s[i + 1] - s[i], 1f);
                                if (a > 25f) { bad++; if (where.Count < 8) where.Add($"bumper ({m.x:0}, {m.z:0}) {a:0} deg"); }
                            }
                            foreach (var e in new[] { (s[0], s.Length > 1 ? s[1] : s[0]), (s[^1], s.Length > 1 ? s[^2] : s[^1]) })
                                if (R87Off(lines, e.Item1, e.Item1 - e.Item2, 1f) >= 0f) { ends++; if (where.Count < 8) where.Add($"bumper end ({e.Item1.x:0}, {e.Item1.z:0})"); }
                        }
                    if (race.Barriers)
                        foreach (var b in race.Barriers.Built)
                            foreach (var t in b.Root.GetComponentsInChildren<Transform>())
                            {
                                if (t.localScale.x < .6f || !t.GetComponent<Renderer>()) continue;   // the blocks (not the posts)
                                float a = R87Off(lines, t.position, t.right, 1f);
                                if (a > 25f) { bars++; if (where.Count < 8) where.Add($"barrier {b.Data.id} block ({t.position.x:0}, {t.position.z:0}) {a:0} deg"); }
                            }
                    badAll += bad; endsAll += ends; barsAll += bars;
                    // the layout, for top-down plots (Tools/race/plot_layout.py)
                    {
                        var sb = new System.Text.StringBuilder();
                        foreach (var l in lines) { sb.Append("line"); for (int i = 0; i < l.p.Count; i++) sb.Append($" {l.p[i].x:0.0},{l.p[i].z:0.0},{l.half[i]:0.0}"); sb.AppendLine(); }
                        if (info != null) foreach (var st in info.strips) { sb.Append("bump"); foreach (var q in st) sb.Append($" {q.x:0.0},{q.z:0.0}"); sb.AppendLine(); }
                        if (race.Barriers) foreach (var b in race.Barriers.Built) foreach (var t in b.Root.GetComponentsInChildren<Transform>()) if (t.localScale.x >= .6f && t.GetComponent<Renderer>()) sb.AppendLine($"block {b.Data.id} {t.position.x:0.0},{t.position.z:0.0},{t.right.x:0.00},{t.right.z:0.00},{t.localScale.x:0.0}");
                        if (info != null) foreach (var j in info.jumps) sb.AppendLine($"jump {j.line} {j.lipAt.x:0.0},{j.lipAt.z:0.0} {j.landAt.x:0.0},{j.landAt.z:0.0} over {j.over:0}");
                        try { System.IO.File.WriteAllText(System.IO.Path.Combine(outputDir, $"87_layout_{file.Replace(".json", "")}_{dir}_{R87Tag}.txt"), sb.ToString()); } catch { }
                    }
                    report.AppendLine($"  {file} {dir}: {info?.report}; {race.Barriers?.Built.Count ?? 0} closures ({race.Barriers?.Rows.Count ?? 0} along the course, {race.Barriers?.Trimmed ?? 0} blocks left off a line); across the course: {bad} bumper pieces, {ends} bumper ends, {bars} barrier blocks{(where.Count > 0 ? ": " + string.Join("; ", where) : "")}");
                    race.EndRace(); R84High(false); Flush();
                }
            Check("A: no bumper piece or barrier block inside a course's driven width more than 25 degrees off the line", badAll == 0 && barsAll == 0, $"{badAll} bumper pieces, {barsAll} barrier blocks");
            Check("A: no bumper end inside a course's driven width", endsAll == 0, $"{endsAll} ends");
        }

        // ---------------------------------------------------------------- A: the human-like driver touches no end
        IEnumerator R87Drive()
        {
            report.AppendLine(); report.AppendLine($"=== A. HIGH MODE, the human-like driver (Drifter Twin) on the line and 3 m either side ({R87Tag}): touches of a bumper's or barrier's end (within 4 m of it), and of any bumper or barrier");
            yield return Vehicle("drifter"); int endTouches = 0;
            foreach (var (file, from, to) in new[] { ("addison_heights.json", -1f, -1f), ("noonday_creek_trail.json", 3150f, 4700f) })
                foreach (float off in new[] { 0f, -3f, 3f })
                {
                    R84High(true); yield return StartRoute(file, "forward", 0, 1);
                    var r = race.Route; float L = r.LapLength; float s0 = from < 0 ? 0f : from, s1 = to < 0 ? L : to;
                    var line = new List<Vector3>();
                    for (float s = s0 - 60f; s <= s1; s += 4f) { var p = r.PointAt(r.GateS + s, out var f); line.Add(p + Vector3.Cross(Vector3.up, f) * off); }
                    var e = race.Player; e.ClockStarted = true; e.NextCheckpoint = r.Checkpoints.FindIndex(c => c.S > s0 - 60f); if (e.NextCheckpoint < 0) e.NextCheckpoint = r.Checkpoints.Count;
                    Place(RaceRoute.Surface(line[0].x, line[0].z, line[0].y), line[1] - line[0], 12f);
                    yield return new WaitForSecondsRealtime(.3f);
                    var walls = FindObjectsByType<Collider>(FindObjectsSortMode.None).Where(c => c.enabled && c.gameObject.activeInHierarchy && (HighBumpers.IsBumper(c) || (c is BoxCollider && c.name == "Barrier wall"))).ToList();
                    var own = car.GetComponentsInChildren<Collider>().Where(c => !c.isTrigger && c.enabled).ToList();
                    var ends = new List<Vector3>(); var info = HighBumpers.Current; if (info != null) foreach (var s in info.strips) { ends.Add(s[0]); ends.Add(s[^1]); }
                    if (race.Barriers) foreach (var row in race.Barriers.Rows) { ends.Add(row.a); ends.Add(row.b); }
                    int touches = 0, atEnd = 0; float lastTouch = -9f; var at = new List<string>();
                    var run = new HumanRun();
                    yield return HumanDrive(line, 40f, 400f, run, () =>
                    {
                        if (Time.time - lastTouch < .5f) return false;
                        foreach (var c in own) foreach (var w in walls)
                            {
                                if (!c.bounds.Intersects(w.bounds)) continue;
                                if (Physics.ComputePenetration(c, c.transform.position, c.transform.rotation, w, w.transform.position, w.transform.rotation, out _, out float d) && d > .01f)
                                {
                                    touches++; lastTouch = Time.time; var p = car.Body.position; bool end = ends.Any(q => Flat(q - p).magnitude < 4f);
                                    if (end) { atEnd++; if (at.Count < 6) at.Add($"({p.x:0}, {p.z:0}) {w.name}"); }
                                    return false;
                                }
                            }
                        return false;
                    });
                    endTouches += atEnd;
                    report.AppendLine($"  {file} {(off == 0 ? "on the line" : off < 0 ? "3 m left" : "3 m right")} (lap {s0:0} to {s1:0} m): {run.time:0} s, top {run.topSpeed:0} m/s, resets {run.resets}, out-of-bumper resets so far {HighBumpers.OutResets}; touches {touches}, at an end {atEnd}{(at.Count > 0 ? ": " + string.Join("; ", at) : "")}");
                    race.EndRace(); R84High(false); Flush();
                }
            Check("A: the human-like driver touches no bumper or barrier end (line and 3 m either side)", endTouches == 0, $"{endTouches} end touches");
        }

        // ---------------------------------------------------------------- B: out of the bumpers, back on the course
        IEnumerator R87Out()
        {
            report.AppendLine(); report.AppendLine($"=== B. HIGH MODE: thrown out over a bumper (the Street Classic, 14 m/s out and up) on three courses, three places each ({R87Tag})");
            yield return Vehicle("original"); int bad = 0, total = 0;
            foreach (var file in new[] { "addison_heights.json", "chastain_glen_loop.json", "kennesaw_mountain.json" })
            {
                R84High(true); yield return StartRoute(file, "forward", 0, 3);
                var r = race.Route; float L = r.LapLength; var e = race.Player; e.ClockStarted = true; e.StartAt = e.LapStart = race.RaceClock;
                var info = HighBumpers.Current; int done = 0;
                foreach (float frac in new[] { .2f, .35f, .5f, .65f, .8f })
                {
                    if (done >= 3 || info == null) break;
                    float s = L * frac; var p = r.PointAt(r.GateS + s, out var f); var right = Vector3.Cross(Vector3.up, f);
                    // a bumper beside it on the right, and open ground past it
                    var bump = info.strips.SelectMany(x => x).Where(q => Flat(q - p).magnitude < 14f && Vector3.Dot(q - p, right) > 0f).OrderBy(q => Flat(q - p).sqrMagnitude).FirstOrDefault();
                    if (bump == default) continue;
                    int k = r.Checkpoints.FindIndex(c => c.S > s); if (k < 0) k = r.Checkpoints.Count; e.NextCheckpoint = k; e.Forgiven.Clear(); float pen0 = e.PenaltyTotal; int out0 = HighBumpers.OutResets;
                    var start = RaceRoute.Surface(p.x, p.z) + Vector3.up * 1.5f; respawn.PlaceAt(start, Mathf.Atan2(right.x, right.z) * Mathf.Rad2Deg);
                    car.Body.linearVelocity = right * 14f + Vector3.up * 6f; race.NotifyTeleported(); R73Pad(); R73Set(0, 0, 0);
                    float t0 = Time.time; Vector3 landed = Vector3.zero; bool wentOut = false;
                    while (Time.time - t0 < 5f && HighBumpers.OutResets == out0) { yield return new WaitForFixedUpdate(); if (HighBumpers.Outside(car.Body.position) && car.GroundedWheels >= 2) { wentOut = true; landed = car.Body.position; } }
                    yield return new WaitForSecondsRealtime(.5f);
                    var q = car.Body.position; float st = r.LapStationOf(q); float cs = k < r.Checkpoints.Count ? r.Checkpoints[k].S : L; float ahead = Mathf.Repeat(cs - st, L);
                    bool back = HighBumpers.OutResets > out0, ok = !wentOut || (back && !HighBumpers.Outside(q) && ahead > 1f && ahead < .5f * L + 1f && e.NextCheckpoint == k && e.PenaltyTotal == pen0);
                    if (!wentOut) { report.AppendLine($"  {file} at lap {s:0} m: did not end up outside (landed in, or the throw fell short): skipped"); continue; }
                    total++; done++; if (!ok) bad++;
                    report.AppendLine($"  {file} at lap {s:0} m: down outside at ({landed.x:0}, {landed.z:0}), {(back ? $"put back after {Time.time - t0:0.0} s" : "NOT put back")} at lap {st:0} m, {ahead:0} m short of {(k < r.Checkpoints.Count ? $"CP{k + 1}" : "the line")}, penalty {e.PenaltyTotal - pen0:0} s{(ok ? "" : "  <- WRONG")}");
                    if (done == 1) yield return Capture($"87_out_{file.Replace(".json", "")}_{R87Tag}");
                }
                race.EndRace(); R84High(false); Flush();
            }
            Check("B: thrown out over a bumper, put back on the course before the next checkpoint, no penalty", bad == 0 && total >= 6, $"{bad} of {total} wrong");
        }

        // ---------------------------------------------------------------- C: resets in the mall
        IEnumerator R87Mall()
        {
            report.AppendLine(); report.AppendLine($"=== C. The Noonday Creek Trail: resets (Y) inside Town Center ({R87Tag})");
            yield return Vehicle("drifter"); int bad = 0, total = 0; R73Pad();
            foreach (var dir in new[] { "forward", "reverse" })
            {
                yield return StartRoute("noonday_creek_trail.json", dir, 0, 3);
                var r = race.Route; float L = r.LapLength; var e = race.Player; e.ClockStarted = true; e.StartAt = e.LapStart = race.RaceClock;
                // the lap's stretch with heights of its own (the mall's floors): its stations
                var own = new List<float>(); for (float s = 0; s < L; s += 3f) { var p = r.PointAt(r.GateS + s, out _); if (R87OwnAt(r, p) && p.x > 260f && p.x < 600f && p.z > -1420f && p.z < -1120f) own.Add(s); }   // Town Center's footprint
                var spots = new List<(string what, Vector3 p, Vector3 f, int next, RaceShortcut sc)>();
                if (own.Count > 0)
                    foreach (float frac in new[] { .15f, .4f, .65f, .9f })
                    {
                        float s = own[Mathf.Clamp((int)(own.Count * frac), 0, own.Count - 1)]; var p = r.PointAt(r.GateS + s, out var f);
                        int k = r.Checkpoints.FindIndex(c => c.S > s); spots.Add(($"lap {s:0} m (y {p.y:0.0})", p, f, k < 0 ? r.Checkpoints.Count : k, null));
                    }
                var carro = r.Shortcuts.FirstOrDefault(x => x.Id.StartsWith("t_carroway"));
                if (carro != null)
                {
                    var mid = carro.Path[carro.Path.Length / 2]; var f = Flat(carro.Path[carro.Path.Length / 2 + 1] - mid).normalized;
                    // the next checkpoint after what it forgives
                    int k = -1; float sEnd = r.LapStationOf(carro.Path[^1]); k = r.Checkpoints.FindIndex(c => c.S > sEnd); spots.Add(($"the Carroway renovation shortcut's middle (y {mid.y:0.0})", mid, f, k < 0 ? r.Checkpoints.Count : k, carro));
                }
                foreach (var (what, p, f, k, sc) in spots)
                {
                    e.NextCheckpoint = k; e.Forgiven.Clear(); e.ActiveShortcut = sc; float pen0 = e.PenaltyTotal;
                    var side = Vector3.Cross(Vector3.up, f); var put = p + side * 2f; var g = Physics.Raycast(put + Vector3.up * 2.5f, Vector3.down, out var hit, 6f, ~0, QueryTriggerInteraction.Ignore) ? hit.point : RaceRoute.Surface(put.x, put.z, p.y);
                    respawn.PlaceAt(g + Vector3.up * .3f, Mathf.Atan2(side.x, side.z) * Mathf.Rad2Deg); car.Body.linearVelocity = Vector3.zero; race.NotifyTeleported();
                    yield return new WaitForFixedUpdate(); e.ActiveShortcut = sc;
                    yield return R73Reset(); total++;
                    var q = car.Body.position; float d2 = Flat(q - p).magnitude, dy = q.y - p.y;
                    float st = r.LapStationNear(q), cs = k < r.Checkpoints.Count ? r.Checkpoints[k].S : L, ahead = Mathf.Repeat(cs - st, L);
                    bool ok = d2 < 30f && Mathf.Abs(dy) < 3f && e.PenaltyTotal == pen0 && (sc != null || (ahead > 1f && ahead < .5f * L));
                    if (!ok) bad++;
                    report.AppendLine($"  {dir}, {what}: put at ({q.x:0}, {q.z:0}) y {q.y:0.0}, {d2:0} m from where it was, {dy:+0.0;-0.0} m up; {(sc == null ? $"{ahead:0} m short of CP{k + 1}" : "on the shortcut")}{(ok ? "" : "  <- WRONG")}");
                    if (total == 1 || sc != null) yield return Capture($"87_mall_{dir}_{(sc != null ? "shortcut" : "lap")}_{R87Tag}");
                }
                e.ActiveShortcut = null; race.EndRace(); Flush();
            }
            Check("C: a reset in the mall stays in the mall, before the next checkpoint", bad == 0 && total >= 8, $"{bad} of {total} wrong");
        }
        static bool R87OwnAt(RaceRoute r, Vector3 p)
        {
            if (r.OwnY == null) return false;
            for (int i = 0; i + 1 < r.Path.Length && i + 1 < r.OwnY.Length; i++)
                if (r.OwnY[i] && r.OwnY[i + 1] && Flat(r.Path[i] - p).magnitude < 4f) return true;
            return false;
        }

        // ---------------------------------------------------------------- D: Addison Heights' places
        IEnumerator R87Addison()
        {
            report.AppendLine(); report.AppendLine($"=== D. Addison Heights forward, HIGH MODE, the human-like driver (Drifter Twin) through Dan's places ({R87Tag})");
            yield return Vehicle("drifter"); int jolts = 0, touchesAll = 0; float worstDrag = 0f;
            // Joel Drive (BUG-006, BUG-005 / 003) on the lap, then the Lyndon jump (BUG-004) on its shortcut
            for (int leg = 0; leg < 2; leg++)
                foreach (float off in new[] { 0f, -2f, 2f })
                {
                    R84High(true); yield return StartRoute("addison_heights.json", "forward", 0, 3);
                    var r = race.Route; var line = new List<Vector3>(); string name;
                    if (leg == 0)
                    {
                        name = "Joel Drive"; float a = r.LapStationOf(new Vector3(2880f, 0, 414f)), b = r.LapStationOf(new Vector3(2969f, 0, 411f)); if (b < a) b += r.LapLength;
                        for (float s = a - 180f; s <= b + 60f; s += 3f) { var p = r.PointAt(r.GateS + s, out var f); line.Add(p + Vector3.Cross(Vector3.up, f) * off); }
                    }
                    else
                    {
                        name = "the Lyndon jump"; var sc = r.Shortcuts.First(x => x.Id == "f_lyndon_crew"); float a = r.LapStationOf(sc.Path[0]);
                        for (float s = a - 150f; s < a; s += 3f) line.Add(r.PointAt(r.GateS + s, out _));
                        for (int i = 0; i < sc.Path.Length; i++) { var f = i + 1 < sc.Path.Length ? sc.Path[i + 1] - sc.Path[i] : sc.Path[i] - sc.Path[i - 1]; line.Add(sc.Path[i] + Vector3.Cross(Vector3.up, Flat(f).normalized) * (off * .5f)); }
                        float b = r.LapStationOf(sc.Path[^1]); for (float s = b + 3f; s < b + 90f; s += 3f) line.Add(r.PointAt(r.GateS + s, out _));
                    }
                    var e = race.Player; e.ClockStarted = true;
                    Place(RaceRoute.Surface(line[0].x, line[0].z, line[0].y), line[1] - line[0], 15f); yield return new WaitForSecondsRealtime(.3f);
                    var walls = FindObjectsByType<Collider>(FindObjectsSortMode.None).Where(c => c.enabled && c.gameObject.activeInHierarchy && (HighBumpers.IsBumper(c) || (c is BoxCollider && c.name == "Barrier wall"))).ToList();
                    var own = car.GetComponentsInChildren<Collider>().Where(c => !c.isTrigger && c.enabled).ToList();
                    int touches = 0, jolt = 0; var vys = new Queue<float>(); float lastJolt = -9f; float last = -9f, prevVy = 0f, minBush = 99f, bushIn = -1f; var notes = new List<string>(); bool pic = false;
                    var run = new HumanRun();
                    yield return HumanDrive(line, 40f, 120f, run, () =>
                    {
                        var p = car.Body.position; float vy = car.Body.linearVelocity.y, sp = Flat(car.Body.linearVelocity).magnitude;
                        vys.Enqueue(vy); if (vys.Count > Mathf.RoundToInt(.1f / Time.fixedDeltaTime)) prevVy = vys.Dequeue();
                        if (car.GroundedWheels >= 2 && vys.Count > 2 && Mathf.Abs(vy - prevVy) > 3f && Time.time - lastJolt > .5f) { jolt++; lastJolt = Time.time; if (notes.Count < 5) notes.Add($"jolt ({p.x:0}, {p.z:0}) {vy - prevVy:+0.0;-0.0} m/s"); }
                        float cov = ShrubBeds.CoverageAt(p); if (cov > 0f && car.GroundedWheels >= 2) { if (bushIn < 0f) bushIn = sp; minBush = Mathf.Min(minBush, sp); }
                        if (Time.time - last > .5f)
                            foreach (var c in own) foreach (var w in walls)
                                {
                                    if (!c.bounds.Intersects(w.bounds)) continue;
                                    if (Physics.ComputePenetration(c, c.transform.position, c.transform.rotation, w, w.transform.position, w.transform.rotation, out _, out float d) && d > .01f)
                                    { touches++; last = Time.time; if (notes.Count < 5) notes.Add($"touch ({p.x:0}, {p.z:0}) {w.name}"); goto done; }
                                }
                        done:
                        if (!pic && off == 0f && leg == 1 && Flat(p - new Vector3(2815f, 0, 504f)).magnitude < 12f) { pic = true; StartCoroutine(Capture($"87_lyndon_landing_{R87Tag}")); }
                        return false;
                    });
                    touchesAll += touches; jolts += jolt;
                    if (bushIn > 0f) worstDrag = Mathf.Max(worstDrag, bushIn - minBush);
                    report.AppendLine($"  {name} {(off == 0 ? "on the line" : off < 0 ? $"{-off:0} m left" : $"{off:0} m right")}: {run.time:0} s, top {run.topSpeed:0} m/s, longest air {run.maxAir:0.0} s, resets {run.resets}, stuck {run.stuck}; jolts (3 m/s up or down in 0.1 s, wheels down) {jolt}; bumper / barrier touches {touches}{(notes.Count > 0 ? ": " + string.Join("; ", notes) : "")}; in a shrub bed: {(bushIn > 0f ? $"{bushIn:0} -> {minBush:0} m/s" : "never")}");
                    race.EndRace(); R84High(false); Flush();
                }
            report.AppendLine($"  shrub beds on the course's road at Dan's BUG-003 / 005: coverage {ShrubBeds.CoverageAt(RaceRoute.Surface(2967.67f, 417.02f)):0.00} and {ShrubBeds.CoverageAt(RaceRoute.Surface(2962.48f, 408.83f)):0.00} (0: no drag)");
            Check("D: no slow-down from the shrub beds on Joel Drive", ShrubBeds.CoverageAt(RaceRoute.Surface(2967.67f, 417.02f)) == 0f && ShrubBeds.CoverageAt(RaceRoute.Surface(2962.48f, 408.83f)) == 0f, $"speed lost in a bed at most {worstDrag:0.0} m/s");
            Check("D: no bumper or barrier touched on Joel Drive and the Lyndon jump", touchesAll == 0, $"{touchesAll} touches");
            Check("D: no jolt on Joel Drive and the Lyndon jump", jolts == 0, $"{jolts} jolts");
        }

        // ---------------------------------------------------------------- E: the Kennesaw climb
        IEnumerator R87Climb()
        {
            report.AppendLine(); report.AppendLine($"=== E. The Kennesaw climb at x -398 z -5520, full throttle by the human-like driver, from 300 m below to 150 m above");
            var spot = new Vector3(-398.6f, 0f, -5524.2f); int bad = 0;
            foreach (bool high in new[] { false, true })
                foreach (var veh in new[] { "drifter", "original", "skyfin" })
                {
                    yield return Vehicle(veh); R84High(high); yield return StartRoute("kennesaw_mountain.json", "forward", 0, 3);
                    var r = race.Route; float s0 = r.LapStationOf(spot); var line = new List<Vector3>();
                    for (float s = s0 - 300f; s <= s0 + 150f; s += 3f) line.Add(r.PointAt(r.GateS + s, out _));
                    race.Player.ClockStarted = true;
                    Place(RaceRoute.Surface(line[0].x, line[0].z), line[1] - line[0], 20f); yield return new WaitForSecondsRealtime(.3f);
                    float air = 0f, maxAir = 0f, worstLat = 0f; bool pic = false; var cum = Cum(line); float sNow = 0f;
                    var run = new HumanRun();
                    yield return HumanDrive(line, 60f, 60f, run, () =>
                    {
                        var p = car.Body.position; sNow = ProjectNear(line, cum, p, sNow, out float dev);
                        bool near = Mathf.Abs(sNow - 300f) < 70f;
                        if (near) { air = car.GroundedWheels == 0 ? air + Time.fixedDeltaTime : 0f; maxAir = Mathf.Max(maxAir, air); worstLat = Mathf.Max(worstLat, Mathf.Abs(dev)); }
                        if (!pic && veh == "original" && !high && sNow > 295f) { pic = true; StartCoroutine(Capture("87_climb_spot")); }
                        return false;
                    }, null, null, null, false);
                    bool ok = maxAir <= .5f && worstLat < 6f && run.resets == 0; if (!ok) bad++;
                    report.AppendLine($"  {(high ? "HIGH" : "normal")} {Nm(veh)}: top {run.topSpeed:0} m/s, longest air within 70 m of the spot {maxAir:0.00} s, farthest off the line there {worstLat:0.0} m, resets {run.resets}{(ok ? "" : "  <- WRONG")}");
                    race.EndRace(); R84High(false); Flush();
                }
            Check("E: the climb: no air over 0.5 s, never off the road (3 vehicles, normal and HIGH)", bad == 0, $"{bad} of 6 runs wrong");
        }
    }
}
