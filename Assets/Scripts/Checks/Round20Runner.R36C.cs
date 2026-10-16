using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Racer;
using UnityEngine;

namespace ChastainRush
{
    // Round 36 Parts C and D in the exe: ChastainRush.exe -exeRound36 <dir> -r36Only hydrants,cfps,rig,geyser -logFile <dir>\Player.log.
    //   hydrants  how many hydrants, pictures of a neighborhood street and of Canton Road with them
    //   cfps      the frame rate parked at Canton Road by the shops (as Round 35's spot)
    //   rig       a rigged hydrant knocked over: its manhole's cover off; then driven into, down the riser into the network
    //   geyser    a geyser's hydrant knocked over, then the geyser driven into: launched (height, air), came down on the street
    public sealed partial class Round20Runner
    {
        IEnumerator R36Hydrants()
        {
            report.AppendLine(); report.AppendLine("=== Part C: the hydrants");
            int n = 0; foreach (var c in FindObjectsByType<HydrantChunk>(FindObjectsSortMode.None)) { var fc = c.GetComponent<FenceChunk>(); if (fc) n += fc.panels.Length; }
            var more = GameObject.Find("MoreHydrants"); int added = 0; if (more) foreach (var fc in more.GetComponentsInChildren<FenceChunk>()) added += fc.panels.Length;
            Check("more hydrants along the streets", added > 100, $"{n} hydrants on the map, {added} of them new this round");
            // pictures: a neighborhood street (the hydrant nearest Hartley Woods Drive's middle) and Canton Road by the shops
            var all = new List<Vector3>(); if (more) foreach (var fc in more.GetComponentsInChildren<FenceChunk>()) foreach (var pn in fc.panels) all.Add(fc.transform.TransformPoint(pn.centre));
            foreach (var (name, at) in new[] { ("36c_hydrants_neighborhood_street", new Vector3(1711f, 0, 1494f)), ("36c_hydrants_canton_road", new Vector3(3500f, 0, 1000f)) })
            {
                if (all.Count == 0) break;
                var h = all.OrderBy(q => Flat(q - at).sqrMagnitude).First();
                var road = RoadLookup.Instance; Vector3 fwd = Vector3.forward; if (road != null && road.NearestPoint(h, out var c, out var tdir)) fwd = new Vector3(tdir.x, 0, tdir.y);
                var eye = h - fwd * 14f + Vector3.Cross(Vector3.up, fwd) * 4f; eye.y = RaceRoute.Surface(eye.x, eye.z).y + 2.2f;
                Place(RaceRoute.Surface(eye.x + fwd.x * 3, eye.z + fwd.z * 3), fwd, 0f); yield return new WaitForSecondsRealtime(.3f);
                yield return Still(name, eye, h + Vector3.up * .5f + fwd * 6f, 60f);
                report.AppendLine($"  {name}: the hydrant at ({h.x:0}, {h.z:0})");
            }
        }
        IEnumerator R36CantonFps()
        {
            report.AppendLine(); report.AppendLine($"=== Part C: frame rate parked at Canton Road by the shops ({Screen.width} x {Screen.height}, not measured at 3840 x 2160)");
            if (race.State != RaceController.Stage.Free) race.EndRace();
            int vs = QualitySettings.vSyncCount, cap = Application.targetFrameRate; QualitySettings.vSyncCount = 0; Application.targetFrameRate = -1;
            respawn.PlaceAt(RaceRoute.Surface(3502.8f, 1166.1f) + Vector3.up * .4f, -13f); race.NotifyTeleported();
            yield return new WaitForSecondsRealtime(3f);
            var frames = new List<float>(); float t0 = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - t0 < 10f) { yield return null; frames.Add(Time.unscaledDeltaTime); }
            var sorted = frames.OrderBy(x => x).ToList(); float p99 = sorted[Mathf.Min(sorted.Count - 1, (int)(sorted.Count * .99f))];
            report.AppendLine($"INFO  Canton Road by the shops: {frames.Count / frames.Sum():0} fps average, slowest 1 % {1f / p99:0} fps");
            QualitySettings.vSyncCount = vs; Application.targetFrameRate = cap;
        }

        // drive into the hydrant at h (along the street, 30 m run-in), at `speed`
        IEnumerator HitHydrant(Vector3 h, float speed)
        {
            var road = RoadLookup.Instance; Vector3 fwd = Vector3.forward; if (road != null && road.NearestPoint(h, out _, out var tdir)) fwd = new Vector3(tdir.x, 0, tdir.y).normalized;
            var a = RaceRoute.Surface(h.x - fwd.x * 30f, h.z - fwd.z * 30f); var b = RaceRoute.Surface(h.x + fwd.x * 10f, h.z + fwd.z * 10f);
            var line = Straight(a, b); line[line.Count / 2] = h;
            Place(a, b - a, speed * .7f); yield return new WaitForSecondsRealtime(.2f);
            yield return StartCoroutine(DriveLine(Straight(a, RaceRoute.Surface(h.x, h.z)).Concat(Straight(RaceRoute.Surface(h.x, h.z), b).Skip(1)).ToList(), speed, 15f, new Run()));
        }
        IEnumerator R36Rig()
        {
            report.AppendLine(); report.AppendLine("=== Part D1: a rigged hydrant: the manhole's cover blows off, the way down into the network");
            var links = HydrantLinks.Instance; if (!links || links.rigs.Length == 0) { Check("the rigged hydrants are in the build", false); yield break; }
            if (race.State != RaceController.Stage.Free) race.EndRace();
            report.AppendLine($"  {links.rigs.Length} rigged hydrants, {links.spouts.Length} geysers");
            yield return Vehicle("original");
            bool any = false;
            foreach (var r in links.rigs.Take(3))
            {
                var h = r.chunk.transform.TransformPoint(r.chunk.panels[r.panel].centre);
                yield return HitHydrant(h, 12f); yield return new WaitForSecondsRealtime(1.2f);
                bool broke = r.chunk.broken[r.panel], open = r.lid.Open;
                report.AppendLine($"  the hydrant at ({h.x:0}, {h.z:0}): broken {broke}, its manhole ({r.lid.lid.position.x:0}, {r.lid.lid.position.z:0}) {(open ? "OPEN" : "closed")}");
                if (!open) continue;
                var m = r.lid.transform.position; var home = r.lid.lid.position;
                yield return Still("36d_rigged_manhole_open", home + Vector3.up * 5f + Vector3.Cross(Vector3.up, r.lid.outward) * 6f, home, 60f);
                // drive into the hole: from 20 m back along the riser's way out (the riser climbs toward `outward`), against it, slowly
                var dir = -r.lid.outward; dir.y = 0; dir.Normalize();
                var a = RaceRoute.Surface(home.x - dir.x * 20f, home.z - dir.z * 20f);
                var line = Straight(a, home); line.Add(home + dir * 12f + Vector3.down * 8f);
                Place(a, dir, 4f); yield return new WaitForSecondsRealtime(.2f);
                var run = new Run(); yield return StartCoroutine(DriveLine(line, 7f, 14f, run, null, rr => car.Body.position.y < home.y - 4f));
                float below = home.y - car.Body.position.y; bool inPipe = DrainPipe.InsideAny(car.Body.position);
                yield return Capture("36d_down_the_manhole");
                report.AppendLine($"  drove into it: {below:0.0} m under the street, {(inPipe ? "inside the network's pipe" : "not in a pipe")}");
                Check("a rigged hydrant knocked over: its manhole's cover blows off and the vehicle drives down into the network", below > 3f || inPipe, $"{below:0.0} m under the street");
                any = true; break;
            }
            if (!any) Check("a rigged hydrant knocked over opens its manhole", false);
        }
        IEnumerator R36Geyser()
        {
            report.AppendLine(); report.AppendLine("=== Part D2: a geyser");
            var links = HydrantLinks.Instance; if (!links || links.spouts.Length == 0) { Check("the geysers are in the build", false); yield break; }
            if (race.State != RaceController.Stage.Free) race.EndRace();
            yield return Vehicle("original");
            var s = links.spouts[0]; var h = s.chunk.transform.TransformPoint(s.chunk.panels[s.panel].centre); var g = s.geyser.transform.position;
            yield return HitHydrant(h, 10f); yield return new WaitForSecondsRealtime(.3f);
            Check("the geyser's hydrant knocked over: the geyser erupts", s.geyser.Erupting, $"hydrant ({h.x:0}, {h.z:0}), grate ({g.x:0}, {g.z:0})");
            // drive into it along the street at 15 m/s
            var d = s.geyser.along; var a = RaceRoute.Surface(g.x - d.x * 45f, g.z - d.z * 45f); var b = RaceRoute.Surface(g.x + d.x * 90f, g.z + d.z * 90f);
            bool launched = false; System.Action<Geyser, ArcadeVehicle> hook = (gy, c) => { if (gy == s.geyser && c == car) launched = true; }; Geyser.Launched += hook;
            if (!s.geyser.Erupting) s.geyser.Erupt();
            Place(a, d, 14f); yield return new WaitForSecondsRealtime(.1f);
            var run = new Run(); float top = -99f, air = 0f, longest = 0f; Vector3 land = new Vector3(float.NaN, 0, 0); bool shot = false;
            var drive = StartCoroutine(DriveLine(Straight(a, b), 15f, 20f, run));
            while (!run.reached && run.end == "" && run.time < 19.9f)
            {
                yield return new WaitForFixedUpdate();
                if (launched) top = Mathf.Max(top, car.Body.position.y - g.y);
                if (car.GroundedWheels == 0) air += Time.fixedDeltaTime; else { if (launched && air > .8f && float.IsNaN(land.x)) land = car.Body.position; longest = Mathf.Max(longest, air); air = 0f; }
                if (launched && !shot && air > .9f) { shot = true; yield return Capture("36d_geyser_launch"); }
            }
            yield return drive; Geyser.Launched -= hook;
            bool onStreet = false; if (!float.IsNaN(land.x) && RoadLookup.Instance != null && RoadLookup.Instance.NearestPoint(land, out var c2, out _)) onStreet = Vector2.Distance(c2, new Vector2(land.x, land.z)) < 6f;
            Check("the geyser throws the vehicle high and it comes down on the street", launched && top > 8f && onStreet, $"{top:0.0} m up, {longest:0.00} s in the air, came down {(float.IsNaN(land.x) ? -1 : Flat(land - g).magnitude):0} m on, {(onStreet ? "on the street" : "OFF the street")}");
        }
    }
}
