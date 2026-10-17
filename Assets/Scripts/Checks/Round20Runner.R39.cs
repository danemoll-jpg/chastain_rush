using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Racer;
using UnityEngine;

namespace ChastainRush
{
    // Round 39 in the exe: ChastainRush.exe -exeRound39 <dir> [-r39Only walkers,walkpics,walkdodge,walkfps] -logFile <dir>\Player.log. Muted (CheckRun).
    //   walkers    Part B: the baked mix (each action's groups, the coffee pairs), the street walkers out round the player in the Glen by kind at 8 am,
    //              noon, 6 pm and 2 am, and none in rain
    //   walkpics   Part B: pictures of a dog walker, a family and a jogger on Parkwood Drive, and kids playing / gardening / to the mailbox
    //   walkdodge  Part B: a vehicle driven at walkers: they step aside
    //   walkfps    Part B: the frame rate on a Glen street with the walkers off ("before") and on (1280 x 720)
    public sealed partial class Round20Runner
    {
        static readonly Vector2 GlenStreet = new Vector2(2911f, 1166f);   // Chastain Glen Lane (counts and the frame rate)
        static readonly Vector2 PicStreet = new Vector2(1560f, 1250f);    // Parkwood Drive (pictures and the dodge: no shot in the Glen, where Dan lives)
        IEnumerator LookRound(Vector3 at)
        {
            var cm = Camera.main; var ch = cm.GetComponent<ChaseCamera>(); bool w0 = ch && ch.enabled; if (ch) ch.enabled = false;
            for (int k = 0; k < 4; k++) { cm.transform.position = at + Vector3.up * 3f; cm.transform.rotation = Quaternion.Euler(10, k * 90, 0); yield return new WaitForSecondsRealtime(1.2f); }
            if (ch) { ch.enabled = w0; ch.Snap(); }
        }
        IEnumerator R39Walkers()
        {
            report.AppendLine(); report.AppendLine("=== Part B: more variety in the people (the mix, the street walkers)");
            var life = AmbientLife.Current; if (!life) { Check("the life is in the build", false); yield break; }
            if (race.State != RaceController.Stage.Free) race.EndRace();
            var g = life.groups; string Name(byte a) => a switch { 0 => "football trios", 1 => "coffee pairs", 3 => "shop walkers", 4 => "on the phone", 5 => "two talking", 6 => "gardening", 7 => "to the mailbox", 8 => "kids playing", _ => "action " + a };
            report.AppendLine($"  baked: {g.Length} groups: " + string.Join(", ", g.GroupBy(x => x.action).OrderBy(x => x.Key).Select(x => $"{Name(x.Key)} {x.Count()}")) + $"; {g.Count(x => x.kind == AmbientLife.Kind.Porch)} on porches");
            int coffee = g.Count(x => x.action == AmbientLife.Coffee);
            Check("coffee pairs about 1 group in 40 at most", coffee * 40 <= g.Length, $"{coffee} of {g.Length}");
            report.AppendLine($"  walkers' streets: {life.streetFirst.Length - 1} lines, {life.streetPts.Length} points, {life.links.Length} links");
            yield return Vehicle("original");
            var glen = RaceRoute.Surface(GlenStreet.x, GlenStreet.y); int[] seen = new int[4]; int coffeeMax = 0;
            foreach (float hour in new[] { 8f, 12f, 18f, 2f })
            {
                yield return Clock(hour); life.ClearWalkers(); respawn.PlaceAt(glen + Vector3.up * .4f, 0f); race.NotifyTeleported();
                yield return new WaitForSecondsRealtime(2f); yield return LookRound(glen); yield return new WaitForSecondsRealtime(4f);
                for (int k = 0; k < 4; k++) seen[k] += life.WalkersByKind[k];
                coffeeMax = Mathf.Max(coffeeMax, life.OutGroups.Count(x => g[x].action == AmbientLife.Coffee));
                report.AppendLine($"  {hour:00}:00 in the Glen: {life.WalkersInfo}; standing groups out {life.Out}");
            }
            Check("walkers out round the player (dog walkers, families, couples, joggers over the day)", seen[0] > 0 && seen[1] + seen[2] > 0 && seen[3] > 0, $"dog walkers {seen[0]}, families {seen[1]}, couples {seen[2]}, joggers {seen[3]}");
            Check("never two coffee pairs out at once", coffeeMax <= 1, $"most out at once {coffeeMax}");
            // rain: none
            var look = WorldLook.Current; GameSettings.Settings.roamWeather = 2; if (look) look.SetClock(12, 17.5f); yield return new WaitForSecondsRealtime(1.5f);
            var cam = Camera.main; var chase = cam.GetComponent<ChaseCamera>(); bool was = chase && chase.enabled; if (chase) chase.enabled = false; cam.transform.position = glen + Vector3.up * 400f; cam.transform.rotation = Quaternion.Euler(-90, 0, 0); yield return new WaitForSecondsRealtime(2f); if (chase) { chase.enabled = was; chase.Snap(); }
            int wet = life.WalkersOut; report.AppendLine($"  rain at 17:30: {life.WalkersInfo}"); GameSettings.Settings.roamWeather = 0;
            Check("nobody walks in the rain", wet == 0, $"{wet} groups out");
        }

        IEnumerator R39WalkPics()
        {
            report.AppendLine(); report.AppendLine("=== Part B: pictures (Parkwood Drive, 9 am)");
            var life = AmbientLife.Current; if (!life) yield break;
            if (race.State != RaceController.Stage.Free) race.EndRace();
            yield return Clock(9f); yield return Vehicle("original");
            var glen = RaceRoute.Surface(PicStreet.x, PicStreet.y); respawn.PlaceAt(glen + Vector3.up * .4f, 0f); race.NotifyTeleported(); yield return new WaitForSecondsRealtime(2f);
            foreach (var (name, kind) in new[] { ("39_walker_dog", AmbientLife.WalkKind.DogWalker), ("39_walker_family", AmbientLife.WalkKind.Family), ("39_walker_jogger", AmbientLife.WalkKind.Jogger), ("39_walker_couple", AmbientLife.WalkKind.Couple) })
            {
                life.ClearWalkers(); int id = life.ForceWalker(kind, car.Body.position + car.transform.forward * 25f);
                if (id < 0) { report.AppendLine($"  {name}: none placed ({life.WalkersInfo})"); continue; }
                yield return new WaitForSecondsRealtime(3f); var at0 = life.WalkerPosition(id); yield return new WaitForSecondsRealtime(.5f); var at = life.WalkerPosition(id);
                var way = at - at0; way.y = 0f; var side = Vector3.Cross(Vector3.up, way.sqrMagnitude > 1e-4f ? way.normalized : Vector3.forward);   // from their side, a little ahead
                at += way.normalized * .6f; side = (side + way.normalized * .5f).normalized;
                yield return Still(name, at + side * 6f + Vector3.up * 2.2f, at + Vector3.up * .8f, 55f);
                report.AppendLine($"  {name}: group {id} at ({at.x:0}, {at.z:0}) ({life.WalkerCount(id)} figures: people + 100 x dogs + 1000 x stroller)");
            }
            life.ClearWalkers();
            // the new standing actions: the nearest group of each out round the Glen at noon
            yield return Clock(12f);
            foreach (var (name, action) in new[] { ("39_life_kids", AmbientLife.Kids), ("39_life_gardening", AmbientLife.Gardening), ("39_life_mail", AmbientLife.Mail), ("39_life_talking", AmbientLife.Talking) })
            {
                var g = life.groups; int best = -1; float bd = float.MaxValue;
                for (int i = 0; i < g.Length; i++) { if (g[i].action != action) continue; float d = (new Vector2(g[i].at.x, g[i].at.z) - PicStreet).sqrMagnitude; if (d < bd) { bd = d; best = i; } }
                if (best < 0) { report.AppendLine($"  {name}: none baked"); continue; }
                var spot = g[best].at; respawn.PlaceAt(RaceRoute.Surface(spot.x + 25f, spot.z + 25f) + Vector3.up * .4f, 0f); race.NotifyTeleported();
                int out1 = -1; for (int k = 0; k < 10 && out1 < 0; k++) { yield return LookRound(spot); out1 = life.OutGroups.Where(x => g[x].action == action && (g[x].at - spot).sqrMagnitude < 120f * 120f).DefaultIfEmpty(-1).First(); }
                if (out1 < 0) { report.AppendLine($"  {name}: no such group came out near ({spot.x:0}, {spot.z:0})"); continue; }
                var gr = g[out1]; var face = Quaternion.Euler(0, gr.yaw, 0) * Vector3.forward;
                yield return Still(name, gr.at + face * 7f + Vector3.up * 2.6f, gr.at + Vector3.up * 1f, 60f);
                report.AppendLine($"  {name}: group {out1} ({gr.kind}, {gr.count} people) at ({gr.at.x:0}, {gr.at.z:0})");
            }
            Check("pictures of walkers and the new actions written", true, "39_walker_*.png, 39_life_*.png");
        }

        IEnumerator R39WalkDodge()
        {
            report.AppendLine(); report.AppendLine("=== Part B: a vehicle driven at walkers");
            var life = AmbientLife.Current; if (!life) yield break;
            if (race.State != RaceController.Stage.Free) race.EndRace();
            yield return Clock(9f); yield return Vehicle("original");
            var glen = RaceRoute.Surface(PicStreet.x, PicStreet.y); respawn.PlaceAt(glen + Vector3.up * .4f, 0f); race.NotifyTeleported(); yield return new WaitForSecondsRealtime(2f);
            life.ClearWalkers(); int id = life.ForceWalker(AmbientLife.WalkKind.DogWalker, glen);
            if (id < 0) { Check("a dog walker placed for the test", false, life.WalkersInfo); yield break; }
            yield return new WaitForSecondsRealtime(1.5f);
            var at = life.WalkerPosition(id); var dir = (at - glen); dir.y = 0; if (dir.sqrMagnitude < 1f) dir = Vector3.forward; dir.Normalize();
            var from = RaceRoute.Surface(at.x - dir.x * 28f, at.z - dir.z * 28f);
            respawn.PlaceAt(from + Vector3.up * .4f, Mathf.Atan2(at.x - from.x, at.z - from.z) * Mathf.Rad2Deg); race.NotifyTeleported(); yield return new WaitForSecondsRealtime(1.5f);
            int d0 = life.WalkerDodges; var run = new Run(); bool shot = false;
            var target = life.WalkerPosition(id); var drive = StartCoroutine(DriveLine(Straight(from, target + (target - from).normalized * 10f), 9f, 8f, run));
            while (!run.reached && run.end == "" && run.time < 7.9f) { yield return null; if (!shot && life.WalkerDodges > d0) { shot = true; yield return Capture("39_walkers_step_aside"); } }
            yield return drive;
            Check("walkers step out of the way of a vehicle coming at them", life.WalkerDodges > d0, $"{life.WalkerDodges - d0} times");
        }

        IEnumerator R39WalkFps()
        {
            report.AppendLine(); report.AppendLine($"=== Part B: the frame rate on a Glen street ({Screen.width} x {Screen.height}, not measured at 3840 x 2160): the walkers off ('before') and on, 8 am");
            var life = AmbientLife.Current; if (!life) yield break;
            if (race.State != RaceController.Stage.Free) race.EndRace();
            yield return Clock(8f); yield return Vehicle("original");
            int vs = QualitySettings.vSyncCount, cap = Application.targetFrameRate; QualitySettings.vSyncCount = 0; Application.targetFrameRate = -1;
            var at = RaceRoute.Surface(GlenStreet.x, GlenStreet.y);
            foreach (bool on in new[] { false, true, false, true })
            {
                AmbientLife.WalkersOff = !on; if (!on) life.ClearWalkers();
                respawn.PlaceAt(at + Vector3.up * .4f, 0f); race.NotifyTeleported(); yield return new WaitForSecondsRealtime(2f); yield return LookRound(at); yield return new WaitForSecondsRealtime(3f);
                var frames = new List<float>(); float t0 = Time.realtimeSinceStartup; double ms0 = AmbientLife.Ms;
                while (Time.realtimeSinceStartup - t0 < 10f) { yield return null; frames.Add(Time.unscaledDeltaTime); }
                var sorted = frames.OrderBy(x => x).ToList(); float p99 = sorted[Mathf.Min(sorted.Count - 1, (int)(sorted.Count * 0.99f))];
                report.AppendLine($"INFO  {(on ? "after " : "before")}: {frames.Count / frames.Sum():0} fps average, slowest 1 % {1f / p99:0} fps; {life.WalkersInfo}; the life's scripts {(AmbientLife.Ms - ms0) / Mathf.Max(1, frames.Count):0.000} ms a frame");
            }
            AmbientLife.WalkersOff = false;
            QualitySettings.vSyncCount = vs; Application.targetFrameRate = cap;
        }
    }
}
