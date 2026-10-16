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

        // drive into the hydrant (chunk's panel) at `speed`: along the street from 30 m back, the other way, then in from the road at an angle, until it breaks
        IEnumerator HitHydrant(FenceChunk chunk, int panel, float speed)
        {
            var h = chunk.transform.TransformPoint(chunk.panels[panel].centre);
            var road = RoadLookup.Instance; Vector3 fwd = Vector3.forward, toRoad = Vector3.zero;
            if (road != null && road.NearestPoint(h, out var c, out var tdir)) { fwd = new Vector3(tdir.x, 0, tdir.y).normalized; toRoad = Flat(new Vector3(c.x, 0, c.y) - h).normalized; }
            var froms = new List<Vector3> { -fwd, fwd };
            if (toRoad != Vector3.zero) { froms.Add((toRoad * .6f - fwd).normalized); froms.Add((toRoad * .6f + fwd).normalized); }
            foreach (var from in froms)
            {
                if (chunk.broken[panel]) yield break;
                var a = RaceRoute.Surface(h.x + from.x * 30f, h.z + from.z * 30f); var at = RaceRoute.Surface(h.x, h.z); var b = RaceRoute.Surface(h.x - from.x * 10f, h.z - from.z * 10f);
                Place(a, at - a, speed * .7f); yield return new WaitForSecondsRealtime(.2f);
                yield return StartCoroutine(DriveLine(Straight(a, at).Concat(Straight(at, b).Skip(1)).ToList(), speed, 15f, new Run()));
                yield return new WaitForSecondsRealtime(.3f);
            }
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
                yield return HitHydrant(r.chunk, r.panel, 12f); yield return new WaitForSecondsRealtime(1.2f);
                bool broke = r.chunk.broken[r.panel], open = r.lid.Open;
                report.AppendLine($"  the hydrant at ({h.x:0}, {h.z:0}): broken {broke}, its manhole ({r.lid.lid.position.x:0}, {r.lid.lid.position.z:0}) {(open ? "OPEN" : "closed")}");
                if (!open) continue;
                // the hole's middle: the riser's trigger under it (the cover itself is flying by now)
                var trig = FindObjectsByType<ManholeTrigger>(FindObjectsSortMode.None).FirstOrDefault(tr => tr.owner == r.lid);
                var mid = trig ? trig.transform.position : r.lid.lid.position; var home = new Vector3(mid.x, GroundQuery.Height(mid.x, mid.z), mid.z);
                yield return new WaitForSecondsRealtime(2.5f);   // the burst's water gone
                foreach (float k in new[] { -2f, -1f, 0f, 1f, 2f })
                {
                    var o = home + r.lid.outward * k + Vector3.up * 3f;
                    var hits = Physics.RaycastAll(o, Vector3.down, 20f, ~0, QueryTriggerInteraction.Ignore).OrderBy(x => x.distance).Take(2);
                    report.AppendLine($"    probe {k:+0;-0;0} m along outward: " + string.Join(", ", hits.Select(x => $"{x.collider.name} at {o.y - x.distance - home.y:0.00} m")));
                }
                yield return Still("36d_rigged_manhole_open", home + r.lid.outward * 7f + Vector3.up * 4f + Vector3.Cross(Vector3.up, r.lid.outward) * 3f, home, 60f);
                // drive into the hole: from 20 m back along the riser's way out (the riser climbs toward `outward`), against it, slowly
                var dir = -r.lid.outward; dir.y = 0; dir.Normalize();
                // under the street: below the terrain's surface at the vehicle (the terrain is holed over the riser; its height is the street's)
                float Under() { var q = car.Body.position; return GroundQuery.Height(q.x, q.z) - q.y; }
                var a = RaceRoute.Surface(home.x - dir.x * 20f, home.z - dir.z * 20f);
                var line = Straight(a, home); line.Add(home + dir * 12f + Vector3.down * 8f);
                foreach (float v in new[] { 3.5f, 1.5f, 6f, 10f })
                {
                    // the check's teleport is a reset, and a reset closes the covers (HydrantLinks): knocked open again, as by the hydrant
                    Place(a, dir, Mathf.Min(v, 2f)); yield return new WaitForSecondsRealtime(.2f);
                    if (!r.lid.Open) { r.lid.BlowOpen(HydrantLinks.OpenSeconds); report.AppendLine("    (the teleport closed the cover: blown open again)"); }
                    bool watch = v < 2f; float lastLog = -1f;
                    IEnumerator Watch()
                    {
                        while (watch)
                        {
                            yield return new WaitForFixedUpdate();
                            var q = car.Body.position; float along = Vector3.Dot(Flat(q - home), r.lid.outward);
                            if (Mathf.Abs(along) > 4f || Time.time - lastLog < .15f) continue; lastLog = Time.time;
                            var hits = car.suspensionPoints.Select(sp => { var o = car.transform.TransformPoint(sp); return Physics.Raycast(o, -car.transform.up, out var hh, car.suspensionLength, car.groundMask, QueryTriggerInteraction.Ignore) ? hh.collider.name.Split(' ')[0] + (hh.collider.name.Contains("barrel") ? "B" : "") : "-"; });
                            var under = Physics.OverlapSphere(q - car.transform.up * .6f, .7f, ~0, QueryTriggerInteraction.Ignore).Where(cc => cc.attachedRigidbody != car.Body).Select(cc => cc.name).Distinct();
                            report.AppendLine($"      {along:+0.0;-0.0} m: y {q.y - home.y:+0.00;-0.00}, wheels {car.GroundedWheels} [{string.Join(" ", hits)}], under the body: {string.Join(", ", under)}");
                        }
                    }
                    var wco = StartCoroutine(Watch());
                    var run = new Run(); yield return StartCoroutine(DriveLine(line, v, 16f, run, null, rr => Under() > 3f));
                    watch = false;
                    report.AppendLine($"    at {v:0.0} m/s: {Under():0.0} m under the street, {Flat(car.Body.position - home).magnitude:0.0} m from the hole");
                    if (Under() > 3f) break;
                }
                yield return new WaitForSecondsRealtime(.6f);
                float below = Under(), off = Flat(car.Body.position - home).magnitude; bool inPipe = DrainPipe.InsideAny(car.Body.position);
                yield return Capture("36d_down_the_manhole");
                report.AppendLine($"  drove into it: {below:0.0} m under the street, {off:0.0} m from the manhole, {(inPipe ? "inside the network's pipe" : "not in a pipe")}");
                Check("a rigged hydrant knocked over: its manhole's cover blows off and the vehicle drives down into the network", below > 3f || inPipe, $"{below:0.0} m under the street's surface, {off:0.0} m from the hole");
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
            yield return HitHydrant(s.chunk, s.panel, 12f); yield return new WaitForSecondsRealtime(.3f);
            Check("the geyser's hydrant knocked over: the geyser erupts", s.chunk.broken[s.panel] && s.geyser.Erupting, $"hydrant ({h.x:0}, {h.z:0}) {(s.chunk.broken[s.panel] ? "broken" : "NOT broken")}, grate ({g.x:0}, {g.z:0}) {(s.geyser.Erupting ? "erupting" : "not erupting")}");
            if (s.geyser.Erupting) yield return Still("36d_geyser_erupting", g - s.geyser.along * 18f + Vector3.up * 3f + Vector3.Cross(Vector3.up, s.geyser.along) * 5f, g + Vector3.up * 6f, 60f);
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
