using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace ChastainRush
{
    // Round 48 in the exe (-exeRound48 <dir> [-r48Only eggpickup,eggtier,eggmap,eggtravel,eggwaypoint,eggrace,eggpics] -logFile <dir>\Player.log): the goose egg
    // hunt and exploration, on the check's own saves (goose-eggs-v1.json and exploration-map-v1.json in <dir>: CheckRun; Dan's never touched)
    //   eggpickup    the Street Classic driven through an easy egg by the road: counted, saved, the banner shown
    //   eggtier      eggs taken to 10 (the test save): the tier notice and the tier saved; 25 the same; a restart: 0 found, tiers kept
    //   eggmap       the map opened: the fog (explored land only), each area outlined with its count, an area chosen
    //   eggtravel    a place discovered by driving to it, then from far away: chosen on the map, travel, arrived there facing along its road
    //   eggwaypoint  a waypoint set on the map: the beam and the HUD line; arrival clears it
    //   eggrace      in a race the eggs are hidden and none can be taken
    //   eggpics      a few eggs, easy and hard
    public sealed partial class Round20Runner
    {
        EggHunt Hunt => EggHunt.Instance;
        IEnumerator FreeRoam() { if (race.State != RaceController.Stage.Free) race.EndRace(); yield return new WaitForSecondsRealtime(0.5f); }

        IEnumerator R48Pickup()
        {
            report.AppendLine(); report.AppendLine("=== THE EGG HUNT: a pickup by driving through");
            yield return FreeRoam();
            var h = Hunt; if (!h) { Check("the egg hunt is in the build", false); yield break; }
            report.AppendLine($"  {h.Total} eggs in {EggAreas.Of(h).Length} areas; save {h.SavePath}");
            Check("the save is the check's own", h.SavePath != null && h.SavePath.StartsWith(outputDir.TrimEnd('\\', '/')), h.SavePath);
            yield return Vehicle("original");
            // the easy egg nearest the start, approached along its road
            var start = car.Body.position;
            var s = h.sites.Where(x => x.how == "easy" && !h.Discovered(x.id)).OrderBy(x => (x.position - start).sqrMagnitude).First();
            int before = h.Found; int picked = 0; System.Action<EggHunt.Site> on = x => picked++; EggHunt.Picked += on;
            // along its road (RoadLookup's nearest piece), on a line through the egg from 40 m back
            var road = Vector3.right; var rl = RoadLookup.Instance;
            if (rl && rl.NearestPoint(s.position, out var rp, out var tan)) road = new Vector3(tan.x, 0f, tan.y).normalized;
            var a = s.position - road * 40f; var b = s.position + road * 20f;
            var line = new List<Vector3>(); for (int k = 0; k <= 30; k++) { var q = Vector3.Lerp(a, b, k / 30f); line.Add(RaceRoute.Surface(q.x, q.z)); }
            line[20] = new Vector3(s.position.x, line[20].y, s.position.z);
            Place(line[0], line[2] - line[0], 8f); yield return new WaitForFixedUpdate();
            var run = new Run(); bool shot = false;
            yield return DriveLine(line, 14f, 20f, run, null, r => { if (!shot && picked > 0) { shot = true; StartCoroutine(Capture("48_pickup_banner")); } return picked > 0 && (car.Body.position - s.position).magnitude > 12f; }, 5f, false);
            EggHunt.Picked -= on;
            string saved = File.Exists(h.SavePath) ? File.ReadAllText(h.SavePath) : "";
            Check("an egg driven through is counted, saved and shown", h.Found == before + 1 && saved.Contains(s.id) && EggBanner.Last.Contains("GOOSE EGG FOUND"),
                  $"{s.id} ({s.area}: {s.note}); found {before} -> {h.Found}; banner \"{EggBanner.Last}\"");
        }

        IEnumerator R48Tier()
        {
            report.AppendLine(); report.AppendLine("=== THE TIERS (the check's own save)");
            yield return FreeRoam();
            var h = Hunt; if (!h) { Check("the egg hunt is in the build", false); yield break; }
            var got = new List<int>(); System.Action<int> on = t => got.Add(t); EggHunt.TierReached += on;
            foreach (var s in h.sites.Where(x => !h.Discovered(x.id)).Take(System.Math.Max(0, 10 - h.Found))) h.Take(s);
            yield return new WaitForSecondsRealtime(1.2f); yield return Capture("48_tier_notice");
            string saved = File.ReadAllText(h.SavePath);
            Check("10 eggs: the tier 1 notice, and the tier saved", got.Contains(10) && EggBanner.Last.StartsWith("TIER 1 REACHED") && h.TiersReached.Contains(10) && saved.Contains("\"tiers\""),
                  $"tiers raised {string.Join(",", got)}; banner \"{EggBanner.Last}\"; saved tiers {string.Join(",", h.TiersReached)}");
            foreach (var s in h.sites.Where(x => !h.Discovered(x.id)).Take(System.Math.Max(0, 25 - h.Found))) h.Take(s);
            Check("25 eggs: the tier 2 notice", got.Contains(25) && EggBanner.Last.StartsWith("TIER 2 REACHED"), $"banner \"{EggBanner.Last}\"");
            EggHunt.TierReached -= on;
            bool ok = h.Restart();
            Check("a restart of the hunt: no eggs found, the tiers kept", ok && h.Found == 0 && h.TiersReached.Contains(10) && h.TiersReached.Contains(25), $"found {h.Found}, tiers {string.Join(",", h.TiersReached)}");
        }

        IEnumerator R48Map()
        {
            report.AppendLine(); report.AppendLine("=== THE MAP: fog, the egg areas");
            yield return FreeRoam();
            var ex = Exploration.Instance; var m = WorldMap.Instance;
            if (!ex || !m) { Check("the exploration and the map are in the build", false); yield break; }
            // a few eggs found again, so the counts show
            foreach (var s in Hunt.sites.Where(x => x.area == "Chastain Glen").Take(3)) Hunt.Take(s);
            report.AppendLine($"  explored cells {ex.RevealedCount} of {ex.Columns * ex.Rows}");
            Check("driving reveals the map (the fog lifts round where the car has been)", ex.RevealedCount > 0 && ex.Visited(car.Body.position), $"{ex.RevealedCount} cells");
            m.Open(); m.ZoomBy(0.01f); yield return new WaitForSecondsRealtime(0.6f); yield return Capture("48_map_whole_fog_areas");
            m.SelectArea(0); yield return new WaitForSecondsRealtime(0.6f); yield return Capture("48_map_area_chastain_glen");
            m.SelectArea(System.Array.FindIndex(EggAreas.Of(Hunt), a => a.name == "Underground")); yield return new WaitForSecondsRealtime(0.6f); yield return Capture("48_map_area_underground");
            m.Close();
            Check("the map shows the areas (pictures)", EggAreas.Of(Hunt).Length >= 8, string.Join(", ", EggAreas.Of(Hunt).Select(a => $"{a.name} {a.Count(Hunt)}")));
        }

        IEnumerator R48Travel()
        {
            report.AppendLine(); report.AppendLine("=== TRAVEL to a discovered place");
            yield return FreeRoam();
            var ex = Exploration.Instance; var m = WorldMap.Instance; if (!ex || !m) { Check("the exploration and the map are in the build", false); yield break; }
            var d = ex.destinations.FirstOrDefault(x => x.id == "stunt-park") ?? ex.destinations.First(x => !x.fixedY);
            Check("a place not yet driven to can not be traveled to", ex.Discovered(d.id) || !ex.Travel(d), ex.Message);
            // discover it: drive a little at it
            Place(d.position + Quaternion.Euler(0, d.yaw, 0) * Vector3.back * 30f, Quaternion.Euler(0, d.yaw, 0) * Vector3.forward, 6f);
            yield return new WaitForSecondsRealtime(3.5f);
            Check($"driving to {d.title} discovers it", ex.Discovered(d.id));
            // far away, then the map: choose it, travel
            var far = ex.destinations.OrderByDescending(x => (x.position - d.position).sqrMagnitude).First(x => !x.fixedY);
            Place(RaceRoute.Surface(far.position.x, far.position.z), Vector3.forward, 0f); yield return new WaitForSecondsRealtime(1.5f);
            m.Open(); yield return null;
            for (int i = 0; i < 40 && (m.SelectedPlace < 0 || ex.destinations[m.SelectedPlace].id != d.id); i++) m.StepPlace(1);
            m.RequestTravel(m.SelectedPlace); yield return new WaitForSecondsRealtime(0.4f); yield return Capture("48_travel_confirm");
            bool went = m.ConfirmTravel(); yield return new WaitForSecondsRealtime(2f);
            float off = Vector3.ProjectOnPlane(car.Body.position - d.position, Vector3.up).magnitude;
            float yawOff = Mathf.Abs(Mathf.DeltaAngle(car.transform.eulerAngles.y, d.yaw));
            yield return Capture("48_travel_arrived");
            Check($"travel from {far.title} to {d.title}: arrived on its spot, facing along its road", went && off < 30f && yawOff < 30f, $"{off:0.0} m from it, {yawOff:0} degrees off; {ex.Message}");
        }

        IEnumerator R48Waypoint()
        {
            report.AppendLine(); report.AppendLine("=== A WAYPOINT");
            yield return FreeRoam();
            var ex = Exploration.Instance; if (!ex) { Check("the exploration is in the build", false); yield break; }
            var p = car.Body.position + car.transform.forward * 120f;
            ex.SetWaypoint(p); yield return new WaitForSecondsRealtime(1.5f);
            var g = ex.GetComponent<WaypointGuide>();
            yield return Capture("48_waypoint_beam_and_hud");
            Check("a waypoint: the beam and the HUD line", g && g.Active && g.Hud.StartsWith("WAYPOINT"), g ? g.Hud : "no guide");
            Place(RaceRoute.Surface(p.x, p.z) - car.transform.forward * 25f, car.transform.forward, 8f);
            yield return new WaitForSecondsRealtime(4f);
            Check("arriving within 15 m clears it", !ex.Waypoint.HasValue, g ? $"arrivals {g.Arrivals}" : "");
        }

        IEnumerator R48Race()
        {
            report.AppendLine(); report.AppendLine("=== IN A RACE: no eggs");
            RaceController.RouteOverride = "chastain_glen_loop.json"; RaceController.DirectionOverride = "forward"; RaceController.LapsOverride = 0; RaceController.AiCount = 0;
            if (race.State != RaceController.Stage.Free) race.EndRace();
            race.Reload(); race.StartRace(); while (race.State == RaceController.Stage.Countdown) yield return null;
            yield return new WaitForSecondsRealtime(1f);
            int shown = Hunt.GetComponentsInChildren<Transform>(true).Count(t => t.name.StartsWith("Goose egg /") && t.gameObject.activeSelf);
            var s = Hunt.sites.Where(x => !Hunt.Discovered(x.id)).OrderBy(x => (x.position - car.Body.position).sqrMagnitude).First();
            int before = Hunt.Found;
            Place(s.position + Vector3.back * 10f, Vector3.forward, 10f); yield return new WaitForSecondsRealtime(2.5f);
            Check("in a race the eggs are hidden and driving through one takes nothing", !EggHunt.Hunting && shown == 0 && Hunt.Found == before, $"tokens shown {shown}, found {before} -> {Hunt.Found}");
            race.EndRace(); RaceController.DirectionOverride = null;
        }

        IEnumerator R48Pics()
        {
            report.AppendLine(); report.AppendLine("=== pictures of eggs, easy and hard");
            yield return FreeRoam();
            var picks = new List<EggHunt.Site>();
            foreach (var want in new (string area, string how, string note)[] { ("Chastain Glen", "easy", ""), ("Chastain Corner", "hard", "roof"), ("Underground", "hard", "glass"), ("Noonday Creek", "hard", "over"), ("Chastain Lakes", "hard", "dock"), ("East neighborhoods", "hard", "behind") })
            {
                var s = Hunt.sites.FirstOrDefault(x => x.area == want.area && x.how == want.how && (want.note == "" || x.note.Contains(want.note)) && !Hunt.Discovered(x.id));
                if (s != null) picks.Add(s);
            }
            foreach (var s in picks)
            {
                // the car parked on the ground 30 m off (the token shows within 180 m of it), the camera 9 m from the egg
                var off = new Vector3(30f, 0f, 0f); Place(RaceRoute.Surface(s.position.x + off.x, s.position.z + off.z), Vector3.left, 0f);
                yield return new WaitForSecondsRealtime(0.8f);
                var cam = s.position + new Vector3(6f, 3f, -6f);
                yield return Still($"48_egg_{s.id}_{s.how}", cam, s.position, 55f);
                report.AppendLine($"  {s.id} {s.area} ({s.how}, {s.kind}): {s.note} at ({s.position.x:0}, {s.position.y:0.0}, {s.position.z:0})");
            }
            Check("pictures written", picks.Count > 0, "48_egg_*.png");
        }
    }
}
