using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Racer;
using UnityEngine;

namespace ChastainRush
{
    // Round 38 Part E in the exe (-exeRound38 <dir> -r38Only life,lifepics,lifefps):
    //   life      people out round the player in the Glen at noon and none at 2 am, animals in the woods by Noonday Creek, a vehicle driven at a
    //             resident (they step out of its way) and past animals (they flee); never inside a race's line
    //   lifepics  pictures: people in the Glen, at Chastain Lakes' pool, on the shops' walks, animals in the woods, a vehicle passing them
    //   lifefps   the frame rate at Canton Road by the shops and on a Glen street, with the life hidden ("before") and on (1280 x 720)
    public sealed partial class Round20Runner
    {
        IEnumerator Clock(float hour) { var look = WorldLook.Current; if (look) look.SetClock(12, hour); GameSettings.Settings.roamWeather = 0; yield return new WaitForSecondsRealtime(1.2f); }
        // a spot: a group of the given kind near (x, z); the camera placed to see it
        int GroupNear(Vector2 at, AmbientLife.Kind kind, float within = 400f)
        {
            var g = AmbientLife.Current.groups; int best = -1; float bd = within * within;
            for (int i = 0; i < g.Length; i++) { if (g[i].kind != kind) continue; float d = (new Vector2(g[i].at.x, g[i].at.z) - at).sqrMagnitude; if (d < bd) { bd = d; best = i; } }
            return best;
        }
        IEnumerator R38Life()
        {
            report.AppendLine(); report.AppendLine("=== Part E: life in the world (Racer's AmbientLife, Wildlife, WoodlandAmbience, UndergroundLife)");
            var life = AmbientLife.Current; var wild = Wildlife.Current;
            if (!life || !wild) { Check("the life is in the build", false); yield break; }
            if (race.State != RaceController.Stage.Free) race.EndRace();
            var g = life.groups;
            report.AppendLine($"  baked: {g.Length} groups ({string.Join(", ", System.Enum.GetValues(typeof(AmbientLife.Kind)).Cast<AmbientLife.Kind>().Select(k => $"{k} {g.Count(x => x.kind == k)}"))}), {wild.habitats.Length} habitats ({string.Join(", ", System.Enum.GetValues(typeof(Wildlife.Species)).Cast<Wildlife.Species>().Select(s => $"{s} {wild.habitats.Count(h => h.species == s)}"))})");
            yield return Vehicle("original");
            // noon in the Glen: people out round the player; 2 am: almost none
            var glen = RaceRoute.Surface(1560f, 1250f);
            yield return Clock(12f); respawn.PlaceAt(glen + Vector3.up * .4f, 0f); race.NotifyTeleported(); yield return new WaitForSecondsRealtime(3f);
            // (a group comes out only out of sight: look round once, as a player would)
            { var cm = Camera.main; var ch = cm.GetComponent<ChaseCamera>(); bool w0 = ch && ch.enabled; if (ch) ch.enabled = false; for (int k = 0; k < 4; k++) { cm.transform.position = glen + Vector3.up * 3f; cm.transform.rotation = Quaternion.Euler(10, k * 90, 0); yield return new WaitForSecondsRealtime(1.2f); } if (ch) { ch.enabled = w0; ch.Snap(); } yield return new WaitForSecondsRealtime(.6f); }
            int noon = life.Out; report.AppendLine($"  noon: {life.Info}"); yield return Clock(2f); yield return new WaitForSecondsRealtime(3f);
            // (groups go only out of view: turn the camera away a moment)
            var cam = Camera.main; var chase = cam.GetComponent<ChaseCamera>(); bool was = chase && chase.enabled; if (chase) chase.enabled = false; cam.transform.position = glen + Vector3.up * 400f; cam.transform.rotation = Quaternion.Euler(-90, 0, 0); yield return new WaitForSecondsRealtime(2f); if (chase) { chase.enabled = was; chase.Snap(); }
            int night = life.Out; yield return Clock(12f);
            // back at noon round the Glen spot (looking round again), for the next part
            respawn.PlaceAt(glen + Vector3.up * .4f, 0f); race.NotifyTeleported();
            { var cm = Camera.main; var ch = cm.GetComponent<ChaseCamera>(); bool w0 = ch && ch.enabled; if (ch) ch.enabled = false; for (int k = 0; k < 4; k++) { cm.transform.position = glen + Vector3.up * 3f; cm.transform.rotation = Quaternion.Euler(10, k * 90, 0); yield return new WaitForSecondsRealtime(1.2f); } if (ch) { ch.enabled = w0; ch.Snap(); } }
            report.AppendLine($"  the Glen: {noon} groups out at noon, {night} at 2 am ({life.Info})");
            Check("people out round the player by day, few at night (LifeSchedule)", noon >= 4 && night < noon / 2, $"{noon} at noon, {night} at 2 am");
            // a vehicle driven at a resident: they step aside
            int gi = life.OutGroups.Where(x => g[x].kind == AmbientLife.Kind.Porch || g[x].kind == AmbientLife.Kind.Yard && g[x].action == 1).OrderBy(x => (g[x].kind == AmbientLife.Kind.Yard ? 0f : 1e6f) + (g[x].at - car.Body.position).sqrMagnitude).DefaultIfEmpty(-1).First();
            report.AppendLine($"  driven at group {gi}{(gi >= 0 ? $" ({g[gi].kind}, {g[gi].count} people, {(g[gi].at - car.Body.position).magnitude:0} m away)" : "")}");
            int d0 = life.Dodges;
            if (gi >= 0)
            {
                var at = g[gi].at; var from = at + Quaternion.Euler(0, g[gi].yaw, 0) * Vector3.forward * 26f; from = RaceRoute.Surface(from.x, from.z);
                respawn.PlaceAt(from + Vector3.up * .4f, Mathf.Atan2(at.x - from.x, at.z - from.z) * Mathf.Rad2Deg); race.NotifyTeleported(); yield return new WaitForSecondsRealtime(2.5f);
                var run = new Run(); var line = Straight(from, at + (at - from).normalized * 10f); bool shot = false;
                var drive = StartCoroutine(DriveLine(line, 9f, 8f, run));
                while (!run.reached && run.end == "" && run.time < 7.9f) { yield return null; if (!shot && life.Dodges > d0) { shot = true; yield return Capture("38_life_resident_steps_aside"); } }
                yield return drive;
            }
            Check("a resident steps out of the way of a vehicle coming at them", life.Dodges > d0, $"{life.Dodges - d0} stepped aside");
            // the woods by Noonday Creek: animals out round the player there; driven at one, it flees
            yield return Clock(9f); int f0 = wild.Flights;
            var woods = wild.habitats.Where(h => h.species == Wildlife.Species.Deer || h.species == Wildlife.Species.Turkey).OrderBy(h => (new Vector2(h.position.x, h.position.z) - new Vector2(2300f, 1250f)).sqrMagnitude).FirstOrDefault();
            if (woods.position != Vector3.zero)
            {
                var stand = RaceRoute.Surface(woods.position.x, woods.position.z); respawn.PlaceAt(stand + Vector3.up * .4f, 0f); race.NotifyTeleported();
                if (chase) chase.enabled = false; for (int k = 0; k < 4; k++) { cam.transform.position = woods.position + Vector3.up * 3f; cam.transform.rotation = Quaternion.Euler(10, k * 90, 0); yield return new WaitForSecondsRealtime(1.2f); } if (chase) { chase.enabled = was; chase.Snap(); }
                var outNow = wild.Out.OrderBy(a => (a.at - car.Body.position).sqrMagnitude).ToList();
                report.AppendLine($"  the woods near ({woods.position.x:0}, {woods.position.z:0}): {outNow.Count} animals out round the player: {string.Join(", ", outNow.Take(6).Select(a => $"{a.species} {(a.at - car.Body.position).magnitude:0} m"))}");
                // one whose approach is clear (the woods' trunks are solid): the nearest with nothing in the way over the last 30 m
                int pick = -1;
                for (int k = 0; k < outNow.Count && pick < 0; k++)
                {
                    var o = outNow[k]; var p0 = RaceRoute.Surface(o.at.x - o.escape.x * 30f, o.at.z - o.escape.z * 30f) + Vector3.up * 1.2f; var d = o.at + Vector3.up * 1.2f - p0;
                    if (!Physics.SphereCastAll(p0, 1.1f, d.normalized, d.magnitude, ~0, QueryTriggerInteraction.Ignore).Any(h => !(h.collider is TerrainCollider) && h.rigidbody != car.Body)) pick = k;
                }
                if (pick >= 0)
                {
                    var target = outNow[pick]; var from = target.at - target.escape * 30f; from = RaceRoute.Surface(from.x, from.z);
                    respawn.PlaceAt(from + Vector3.up * .4f, Mathf.Atan2(target.escape.x, target.escape.z) * Mathf.Rad2Deg); race.NotifyTeleported(); yield return new WaitForSecondsRealtime(1.5f);
                    var run = new Run(); bool shot = false; int f1 = wild.Flights; float closest = 999f, vAt = 0f;
                    var drive = StartCoroutine(DriveLine(Straight(from, target.at + target.escape * 8f), 8f, 9f, run));
                    while (!run.reached && run.end == "" && run.time < 8.9f) { yield return null; float dd = Vector3.Distance(car.Body.position, target.at); if (dd < closest) { closest = dd; vAt = car.Body.linearVelocity.magnitude; } if (!shot && wild.Flights > f1) { shot = true; yield return new WaitForSecondsRealtime(.6f); yield return Capture("38_life_animal_flees"); } }
                    yield return drive;
                    report.AppendLine($"  driven at the {target.species} from {Vector3.Distance(from, target.at):0} m: came within {closest:0.0} m at {vAt:0.0} m/s ({(run.reached ? "past it" : "stopped " + run.end)}); {wild.Flights - f1} fled; {wild.ActiveCount} out");
                }
            }
            Check("animals in the woods round the player; driven past, they flee (Racer's escape)", wild.Flights > f0, $"{wild.Flights - f0} fled, {wild.ActiveCount} out, {wild.Sightings} seen");
            // nothing on a race's line: every group and habitat 8 m or more from each route's lap (as built)
            float worst = float.MaxValue;
            var routeLine = race.Route != null ? race.Route : null; int close = 0;
            if (routeLine != null) foreach (var x in g) { float s = routeLine.LapStationOf(x.at); var p = routeLine.PointAt(routeLine.GateS + s, out _); float d = Vector2.Distance(new Vector2(p.x, p.z), new Vector2(x.at.x, x.at.z)); worst = Mathf.Min(worst, d); if (d < 6f) close++; }
            Check("no group of people within 6 m of the current race route's line", close == 0, routeLine != null ? $"nearest {worst:0.0} m" : "no route");
        }

        IEnumerator R38LifePics()
        {
            report.AppendLine(); report.AppendLine("=== Part E: pictures (noon)");
            var life = AmbientLife.Current; var wild = Wildlife.Current; if (!life || !wild) yield break;
            if (race.State != RaceController.Stage.Free) race.EndRace();
            yield return Clock(12f); yield return Vehicle("original");
            foreach (var (name, at, kind) in new[] { ("38_life_glen_yard", new Vector2(1560f, 1250f), AmbientLife.Kind.Yard), ("38_life_glen_porch", new Vector2(1500f, 1210f), AmbientLife.Kind.Porch), ("38_life_lakes_pool", new Vector2(1343f, 1110f), AmbientLife.Kind.Pool), ("38_life_shops", new Vector2(3480f, 1060f), AmbientLife.Kind.Shop) })
            {
                int g0 = GroupNear(at, kind, 600f); if (g0 < 0) { report.AppendLine($"  {name}: no {kind} group near"); continue; }
                var spot = life.groups[g0].at; respawn.PlaceAt(RaceRoute.Surface(spot.x, spot.z + 20f) + Vector3.up * .4f, 180f); race.NotifyTeleported();
                // look round (a group comes out only out of sight), then at the nearest group of that kind that is out
                var cam = Camera.main; var chase = cam.GetComponent<ChaseCamera>(); bool was = chase && chase.enabled; if (chase) chase.enabled = false;
                int gi = -1;
                for (int k = 0; k < 12 && gi < 0; k++)
                {
                    cam.transform.position = spot + Vector3.up * 3f; cam.transform.rotation = Quaternion.Euler(10, k * 90, 0); yield return new WaitForSecondsRealtime(1.2f);
                    gi = life.OutGroups.Where(x => life.groups[x].kind == kind && (life.groups[x].at - spot).sqrMagnitude < 150f * 150f).OrderBy(x => (life.groups[x].at - spot).sqrMagnitude).DefaultIfEmpty(-1).First();
                }
                if (chase) chase.enabled = was;
                if (gi < 0) { report.AppendLine($"  {name}: no {kind} group came out near ({spot.x:0}, {spot.z:0}) ({life.Info})"); continue; }
                var gr = life.groups[gi]; var face = Quaternion.Euler(0, gr.yaw, 0) * Vector3.forward;
                yield return Still(name, gr.at + face * 7f + Vector3.up * 2.6f, gr.at + Vector3.up * 1f, 60f);
                report.AppendLine($"  {name}: group {gi} ({kind}, {gr.count} people, action {gr.action}) at ({gr.at.x:0}, {gr.at.z:0}); {life.Info}");
            }
            // animals in the woods (by day): the nearest one out round the player there
            var hab = wild.habitats.Where(h => h.species == Wildlife.Species.Deer).OrderBy(h => (new Vector2(h.position.x, h.position.z) - new Vector2(2300f, 1250f)).sqrMagnitude).FirstOrDefault();
            if (hab.position != Vector3.zero)
            {
                respawn.PlaceAt(RaceRoute.Surface(hab.position.x, hab.position.z) + Vector3.up * .4f, 0f); race.NotifyTeleported();
                var cam = Camera.main; var chase = cam.GetComponent<ChaseCamera>(); bool was = chase && chase.enabled; if (chase) chase.enabled = false;
                for (int k = 0; k < 4; k++) { cam.transform.position = hab.position + Vector3.up * 3f; cam.transform.rotation = Quaternion.Euler(10, k * 90, 0); float w = 0f; while (w < 1.2f) { yield return null; w += Time.unscaledDeltaTime; } }
                if (chase) chase.enabled = was;
                var a = wild.Out.OrderBy(x => (x.at - hab.position).sqrMagnitude).FirstOrDefault();
                if (a.at != Vector3.zero)
                {
                    var side = Vector3.Cross(Vector3.up, a.escape).normalized;
                    yield return Still("38_life_woods_animals", a.at + side * 7f + Vector3.up * 1.8f, a.at + Vector3.up * .6f, 55f);
                    report.AppendLine($"  38_life_woods_animals: a {a.species} at ({a.at.x:0}, {a.at.z:0})");
                }
            }
            Check("pictures of people and animals written", true, "38_life_*.png");
        }

        IEnumerator R38LifeFps()
        {
            report.AppendLine(); report.AppendLine($"=== Part E: the frame rate, parked ({Screen.width} x {Screen.height}, not measured at 3840 x 2160): the life hidden ('before') and on (noon)");
            var life = AmbientLife.Current; if (!life) yield break;
            if (race.State != RaceController.Stage.Free) race.EndRace();
            yield return Clock(12f); yield return Vehicle("original");
            int vs = QualitySettings.vSyncCount, cap = Application.targetFrameRate; QualitySettings.vSyncCount = 0; Application.targetFrameRate = -1;
            var root = life.gameObject;
            var spots = new List<(string, Vector3, float)> { ("Canton Road by the shops (Round 35's spot)", RaceRoute.Surface(3502.8f, 1166.1f), -13f), ("a Chastain Glen street", RaceRoute.Surface(1560f, 1250f), 0f) };
            foreach (bool on in new[] { false, true, false, true })
            {
                root.SetActive(on);
                foreach (var (label, at, yaw) in spots)
                {
                    respawn.PlaceAt(at + Vector3.up * .4f, yaw); race.NotifyTeleported(); yield return new WaitForSecondsRealtime(4f);
                    var frames = new List<float>(); float t0 = Time.realtimeSinceStartup; double ms0 = AmbientLife.Ms + Wildlife.Ms;
                    while (Time.realtimeSinceStartup - t0 < 10f) { yield return null; frames.Add(Time.unscaledDeltaTime); }
                    double perFrame = (AmbientLife.Ms + Wildlife.Ms - ms0) / Mathf.Max(1, frames.Count);
                    var sorted = frames.OrderBy(x => x).ToList(); float p99 = sorted[Mathf.Min(sorted.Count - 1, (int)(sorted.Count * 0.99f))];
                    report.AppendLine($"INFO  {(on ? "after " : "before")} {label}: {frames.Count / frames.Sum():0} fps average, slowest 1 % {1f / p99:0} fps{(on ? $"; {life.Out} groups out, {Wildlife.Current?.ActiveCount ?? 0} animals; the life's scripts {perFrame:0.000} ms a frame" : "")}");
                }
            }
            root.SetActive(true);
            QualitySettings.vSyncCount = vs; Application.targetFrameRate = cap;
        }
    }
}
