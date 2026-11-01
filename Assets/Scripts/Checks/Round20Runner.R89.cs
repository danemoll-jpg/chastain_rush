using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Racer;
using UnityEngine;

namespace ChastainRush
{
    // Round 89 in the exe (-exeRound89 <dir> [-r89Only p89eggs,p89save,p89map,p89radio] -r20Minutes 90 -logFile <dir>\Player.log), from the repo root, on the
    // check's own save. The crossings (A) use Round 83's check: -exeRound83 <dir> -r83Only lines -r82Lines local/round83/r83_lines_a.json | r83_lines_b.json.
    //   p89save    C: a copy of Dan's goose-egg save put in the check's folder before the run (goose-eggs-v1.json): its found eggs still found, its old
    //              tiers moved onto the 250-egg tiers; 250 eggs in all
    //   p89eggs    C: every new egg (egg-101 .. egg-250), a placed-car probe: settled on a surface (a "low" egg on the lowest one with room over it), the
    //              Street Classic put 16 m back toward the nearest road and driven through it at 9 m/s: taken, without a fall; a picture from the
    //              player's camera of the first egg of each area
    //   p89map     C: the map with the new areas, the GOOSE EGGS line (250)
    //   p89radio   B: a Getaway on the Needle 600 to heat 5 at Town Center / Barrett Parkway, Marietta Square and Kennesaw: every call's street and
    //              the player's road when it was said; a call naming the street the suspect is on must be the player's road, a "near" call only
    //              where the player's road has no line, and the line's clip played (not text only)
    public sealed partial class Round20Runner
    {
        IEnumerator R89Save()
        {
            report.AppendLine(); report.AppendLine("=== C. A copy of Dan's goose-egg save (put in the check's folder before the run)");
            yield return FreeRoam();
            var h = Hunt; if (!h) { Check("the egg hunt is in the build", false); yield break; }
            string copy = Path.Combine(outputDir, "dan_eggs_copy.json");
            var saved = File.Exists(copy) ? JsonUtility.FromJson<EggHunt.Save>(File.ReadAllText(copy)) : null;
            report.AppendLine($"  {h.Total} eggs, {EggAreas.Of(h).Length} areas, tiers {string.Join(", ", h.tiers)}; save {h.SavePath}");
            Check("C: 250 eggs, the tiers rescaled to 250", h.Total == 250 && h.tiers.Last() == 250, $"{h.Total} eggs, tiers {string.Join(", ", h.tiers)}");
            if (saved == null) { Check("C: Dan's save copied in", false, "no " + copy); yield break; }
            int keep = saved.found.Count(id => h.Discovered(id));
            Check("C: a copy of Dan's save keeps its found eggs", keep == saved.found.Count && h.Found == saved.found.Count,
                  $"Dan's copy: {saved.found.Count} found, tiers {string.Join(",", saved.tiers)}; in the game: {h.Found} / {h.Total} found ({keep} of his), tiers reached {string.Join(",", h.TiersReached)}");
            report.AppendLine($"  {h.Summary}");
        }

        IEnumerator R89Eggs()
        {
            report.AppendLine(); report.AppendLine("=== C. Every new egg: a placed-car probe (the Street Classic put 16 m back toward the nearest road, driven through at 9 m/s)");
            yield return FreeRoam(); yield return Clock(13f); yield return Vehicle("original");
            var h = Hunt; if (!h) { Check("the egg hunt is in the build", false); yield break; }
            var news = h.sites.Where(s => int.TryParse(s.id.Substring(4), out int n) && n > 100).ToList();
            int taken = 0, bad = 0; var shotArea = new HashSet<string>(); var net = RoadNet.Shared; var rows = new List<string>();
            int picked = 0; string lastId = null; System.Action<EggHunt.Site> on = x => { picked++; lastId = x.id; }; EggHunt.Picked += on;
            foreach (var s in news)
            {
                bool already = h.Discovered(s.id);
                // the surface the egg sits on (Settle set its height: 0.55 m over it); unsettled: its y still the file's 0
                bool settled = s.position.y > 1f;
                // toward the egg from the nearest road (else from the south), 16 m back; tried round in 45 degree steps until there is ground for the car
                Vector3 dir = Vector3.forward;
                if (net != null) { int nd = net.Nearest(s.position, out float dd, 300); if (nd >= 0 && dd > 2f) dir = Flat(s.position - net.P[nd]).normalized; }
                Vector3? start = null; Vector3 useDir = dir;
                for (int k = 0; k < 8 && start == null && settled; k++)
                {
                    var d = Quaternion.Euler(0f, 45f * ((k + 1) / 2) * (k % 2 == 0 ? 1 : -1), 0f) * dir;
                    var a = s.position - d * 16f;
                    var hits = Physics.RaycastAll(new Vector3(a.x, s.position.y + 2.2f, a.z), Vector3.down, 8f, ~0, QueryTriggerInteraction.Ignore).Where(x => !x.collider.attachedRigidbody && x.collider.name.IndexOf("tree", System.StringComparison.OrdinalIgnoreCase) < 0 && x.collider.name.IndexOf("trunk", System.StringComparison.OrdinalIgnoreCase) < 0).OrderBy(x => x.distance).ToList();
                    if (hits.Count == 0) continue;
                    var hp = hits[0].point; if (Mathf.Abs(hp.y - (s.position.y - .55f)) > 3.5f) continue;
                    // nothing solid between there and the egg at a car's height
                    if (Physics.Linecast(hp + Vector3.up * 1f, s.position + Vector3.up * .5f, out var block, ~0, QueryTriggerInteraction.Ignore) && !block.collider.attachedRigidbody && !(block.collider is TerrainCollider) && block.collider.name.IndexOf("tree", System.StringComparison.OrdinalIgnoreCase) < 0 && block.collider.name.IndexOf("trunk", System.StringComparison.OrdinalIgnoreCase) < 0) continue;
                    start = hp; useDir = d;
                }
                string why = "";
                if (!settled) why = "not settled on a surface";
                else if (start == null) why = "no open ground within 16 m to start from";
                bool got = already;
                if (why == "")
                {
                    var line = new List<Vector3>(); for (int k = 0; k <= 12; k++) { var q = Vector3.Lerp(start.Value, s.position + useDir * 8f, k / 12f); line.Add(new Vector3(q.x, s.position.y, q.z)); }
                    int before = picked; float y0 = start.Value.y;
                    Place(start.Value, useDir, 9f); h.ResetMovement(); yield return new WaitForFixedUpdate();
                    var run = new Run(); float lowest = y0; bool pic = false;
                    yield return DriveLine(line, 9f, 8f, run, null, r =>
                    {
                        lowest = Mathf.Min(lowest, car.Body.position.y);
                        if (!pic && !shotArea.Contains(s.area) && Flat(car.Body.position - s.position).magnitude < 9f) { pic = true; shotArea.Add(s.area); StartCoroutine(Capture($"89_egg_{s.id}_{Safe(s.area)}")); }
                        return picked > before && Flat(car.Body.position - s.position).magnitude > 4f;
                    }, 4f, true);
                    got = h.Discovered(s.id);
                    if (!got) why = $"driven past, not taken (ended {Flat(car.Body.position - s.position).magnitude:0.0} m from it{(run.end != "" ? ", " + run.end : "")})";
                    else if (y0 - lowest > 2.5f) why = $"taken, but the car fell {y0 - lowest:0.0} m";
                }
                if (got) taken++;
                bool ok = got && why == ""; if (!ok) bad++;
                rows.Add($"  {s.id} {s.area} ({s.how}, {s.kind}) at ({s.position.x:0}, {s.position.y:0.0}, {s.position.z:0}): {(ok ? "taken" : why)}{(ok ? "" : "  <- WRONG")}   [{s.note}]");
            }
            EggHunt.Picked -= on;
            foreach (var r in rows) report.AppendLine(r);
            Check("C: every new egg reached by a car and taken (none inside a building or under the ground)", bad == 0, $"{taken} of {news.Count} taken, {bad} wrong");
        }
        static string Safe(string s) => new string(s.Where(char.IsLetterOrDigit).ToArray()).ToLower();

        IEnumerator R89Map()
        {
            report.AppendLine(); report.AppendLine("=== C. The map: the new areas, and the GOOSE EGGS count");
            yield return FreeRoam();
            var m = WorldMap.Instance; var h = Hunt; if (!m || !h) { Check("the map and the egg hunt are in the build", false); yield break; }
            m.Open(); m.ZoomBy(0.01f); yield return new WaitForSecondsRealtime(0.8f); yield return Capture("89_map_whole_areas");
            var areas = EggAreas.Of(h);
            int ki = System.Array.FindIndex(areas, a => a.name == "Kennesaw Mountain"); if (ki >= 0) { m.SelectArea(ki); yield return new WaitForSecondsRealtime(0.8f); yield return Capture("89_map_area_kennesaw"); }
            int ti = System.Array.FindIndex(areas, a => a.name == "Town Center at Cobb"); if (ti >= 0) { m.SelectArea(ti); yield return new WaitForSecondsRealtime(0.8f); yield return Capture("89_map_area_town_center"); }
            m.Close();
            report.AppendLine("  areas: " + string.Join(", ", areas.Select(a => $"{a.name} {a.Count(h)}")));
            Check("C: the map lists 25 areas and the hunt counts 250", areas.Length == 25 && h.Total == 250 && h.Summary.Contains("/ 250"), h.Summary);
        }

        // ---------- B: the radio's streets in Getaway chases outside the first map
        IEnumerator R89Radio()
        {
            report.AppendLine(); report.AppendLine("=== B. The police radio's streets: Getaway on the Needle 600 to heat 5 (the heat five times faster in the check), three places outside the first map");
            if (race.State != RaceController.Stage.Free) race.EndRace();
            yield return Clock(13f); yield return Vehicle("moto");
            var places = new (string name, float x, float z, float yaw)[] { ("Town Center / Barrett Parkway", 640f, -1310f, 90f), ("Marietta Square", 1814.5f, -8464.5f, 270f), ("Kennesaw (Burnt Hickory Road)", -1500f, -4900f, 90f) };
            int bad = 0, calls = 0, near = 0, on = 0, textOnly = 0; var net = RoadNet.Shared;
            foreach (var pl in places)
            {
                // the nearest road to the place, facing along it
                var at = new Vector3(pl.x, 0f, pl.z); float yaw = pl.yaw;
                if (net != null) { int nd = net.Nearest(at, out _, 400); if (nd >= 0) { at = net.P[nd]; var t = net.Tangent(nd); yaw = Mathf.Atan2(t.x, t.z) * Mathf.Rad2Deg; } }
                respawn.PlaceAt(RaceRoute.Surface(at.x, at.z) + Vector3.up * .4f, yaw); race.NotifyTeleported(); yield return new WaitForSecondsRealtime(1.5f);
                PoliceSetup.Mode = PoliceSetup.Game.Getaway; PoliceSetup.Two = false; PoliceSetup.Minutes = 6; PoliceSetup.Difficulty = 1; GetawayChase.CheckFast = true;
                var log = new List<string>(); int played0 = PoliceRadioVoice.Played.Count;
                System.Action<string[]> said = ids =>
                {
                    var (st, nr) = PoliceRadio.StreetIn(ids); if (st == null) return;
                    var p = car.Body.position; string road = ""; string roadId = null; float off = 999f;
                    if (net != null) { int nd = net.Nearest(p, out off, 60); if (nd >= 0) { road = net.RoadNames[net.Road[nd]]; roadId = PoliceRadio.StreetId(road); } }
                    bool aboutSuspectOn = ids.Length > 0 && (ids[0] == "P15" || ids[0] == "P11" || ids[0] == "P12" || ids[0] == "P13" || ids[0] == "P14" || ids[0] == "P03");
                    bool isNear = ids.Length > 0 && ids[0] == "P16";
                    string verdict = "";
                    if (aboutSuspectOn && off < 15f && roadId != st) verdict = "WRONG: names a street the suspect is not on";
                    if (isNear && off < 15f && roadId != null) verdict = "WRONG: \"near\" on a street that has a line";
                    calls++; if (isNear) near++; else if (aboutSuspectOn) on++;
                    if (verdict != "") bad++;
                    log.Add($"    {PoliceRadio.TextOf(ids)}   [{string.Join("+", ids)}]  player on {(road == "" ? "(no name)" : road)} ({(roadId ?? "no line")}, {off:0} m off it){(verdict != "" ? "  <- " + verdict : "")}");
                };
                PoliceRadio.Said += said;
                PoliceSession.Launch(); yield return new WaitForSecondsRealtime(.5f);
                var g = GetawayChase.Current; if (!g) { PoliceRadio.Said -= said; Check($"B: Getaway starts at {pl.name}", false); continue; }
                while (g && g.State == GetawayChase.Phase.Starting) yield return null;
                var e = new Escapee(); float t0 = Time.time; int top = 1;
                while (g && g.State == GetawayChase.Phase.Running && Time.time - t0 < 200f)
                {
                    RunnerStep(e, car, Time.deltaTime); yield return null; top = Mathf.Max(top, g.Heat);
                    if (top >= 5 && Time.time - t0 > 120f) break;
                }
                StopRunner(car);
                PoliceRadio.Said -= said;
                var voiced = PoliceRadioVoice.Played.Skip(played0).ToList(); int txt = voiced.Count(x => x.Contains("text only")); textOnly += txt;
                report.AppendLine($"  {pl.name}: top heat {top}, {log.Count} calls with a street, {voiced.Count} lines played ({txt} text only)");
                foreach (var l in log) report.AppendLine(l);
                foreach (var v in voiced.Where(x => x.Contains("text only")).Take(5)) report.AppendLine("    text only: " + v);
                yield return Capture($"89_radio_{Safe(pl.name)}");
                PoliceSession.Stop(); GetawayChase.CheckFast = false; yield return new WaitForSecondsRealtime(2f);
            }
            Check("B: every street call names the suspect's own street (or \"near\" the nearest street with a line where his has none), voiced", bad == 0 && calls > 0 && textOnly == 0,
                  $"{calls} calls with a street ({on} on the suspect's street, {near} \"near\"), {bad} wrong, {textOnly} lines text only");
        }
    }
}
