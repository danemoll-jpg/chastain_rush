using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Racer;
using UnityEngine;

namespace ChastainRush
{
    // Round 77 in the exe (-exeRound77 <dir> [-r77Only dan,conn,hosp,deck,rail,railmap,square] [-r77Lines <file>] -logFile <dir>\Player.log): CHECK IT AS DAN
    // PLAYS IT (TODO.md item 0). The lines and viewpoints: local/round77/r77_lines.json (made from the build's data by the round's script).
    //   dan      Dan's seven reports (session 2026-10-27_21-09-31-163_2b90af) from his exact position and heading; at BUG-001 to 003 the ground within 40 m
    //   conn     the Canton Road Connector driven by the human-like driver from BUG-003 to BUG-001 (the Drifter Twin, Dan's vehicle): in the air, roll, resets
    //   hosp     Kennestone from the street: the main entrance (photo 03), the skybridge from Church Street (photo 02), the ER bay, the four sides, the ER's roof
    //   deck     the Green deck (Kennestone's seven levels) driven from the street up every ramp to the roof and off its far side (a real jump)
    //   rail     the railroad: each bridge (from its approach), each end at the map's edge, the level crossings, the underpasses
    //   railmap  the map screen over the line (all of it, then downtown Marietta)
    //   square   Marietta Square from the place of each of Dan's photos, and each side of the square
    public sealed partial class Round20Runner
    {
        [Serializable] class R77Poi { public string name; public float x, z, heading, len; public bool cross; }
        [Serializable] class R77DeckJ { public int index, levels; public float[] centre, u, line; public float hl, hw; }
        [Serializable] class R77Lines { public float[] connector; public R77DeckJ deck; public R77Poi[] rail, square, hospital; }
        R77Lines r77;
        R77Lines R77L() { if (r77 == null) { var a = Environment.GetCommandLineArgs(); int ai = Array.IndexOf(a, "-r77Lines"); var p = Path.Combine(Environment.CurrentDirectory, ai >= 0 && ai + 1 < a.Length ? a[ai + 1] : "local/round77/r77_lines.json"); if (File.Exists(p)) r77 = JsonUtility.FromJson<R77Lines>(File.ReadAllText(p)); } return r77; }
        static List<Vector3> R77Flat(float[] f) { var l = new List<Vector3>(); for (int i = 0; i + 1 < f.Length; i += 2) l.Add(new Vector3(f[i], RaceRoute.Ground(f[i], f[i + 1]).y, f[i + 1])); return l; }

        // ---------------------------------------------------------------- dan
        IEnumerator R77Dan()
        {
            report.AppendLine(); report.AppendLine("=== Dan's seven reports, from his exact position and heading (the chase camera, as he saw it)");
            if (race.State != RaceController.Stage.Free) race.EndRace();
            yield return Vehicle("drifter"); yield return Clock(13f);
            var spots = new (string id, Vector3 at, float yaw, string what)[]
            {
                ("BUG-001", new Vector3(2292.44f, 0f, -5006.93f), 66.37f, "beside the connector where it meets the highway: torn spikes of ground both sides"),
                ("BUG-002", new Vector3(1694.78f, 0f, -5783.19f), 351.57f, "on the connector: steep banks and mounds crowding the ramps"),
                ("BUG-003", new Vector3(1618.42f, 0f, -6110.74f), 29.02f, "on the connector: a split with a mound between, the road dipping, a stray white block"),
                ("BUG-004", new Vector3(1608.76f, 0f, -6601.44f), 279.19f, "by the hospital: plain tan boxes"),
                ("BUG-005", new Vector3(1665.13f, 0f, -6716.61f), 150.92f, "Church Street NW: a building dressed as stores over the street"),
                ("BUG-006", new Vector3(1853.58f, 0f, -9077.33f), 346.96f, "the tracks laid flat across South Marietta Parkway"),
                ("BUG-007", new Vector3(1620.14f, 0f, -8259.97f), 356.52f, "two tracks ending in a building's wall"),
            };
            foreach (var s in spots)
            {
                yield return R74Place(s.at, s.yaw); yield return Capture($"77_{s.id}_after");
                report.AppendLine($"  {s.id} ({s.what}): at ({car.Body.position.x:0.0}, {car.Body.position.y:0.0}, {car.Body.position.z:0.0}) heading {s.yaw:0}; ahead: {R74Ahead(car.Body.position, s.yaw)}; picture 77_{s.id}_after.png");
                if (s.id == "BUG-001" || s.id == "BUG-002" || s.id == "BUG-003")
                {
                    int steep = 0; float worst = 0f; Vector3 wAt = s.at;
                    for (float x = -40f; x <= 40f; x += 1f)
                        for (float z = -40f; z <= 40f; z += 1f)
                        {
                            float h = GroundQuery.Height(s.at.x + x, s.at.z + z), hx = GroundQuery.Height(s.at.x + x + 1f, s.at.z + z), hz = GroundQuery.Height(s.at.x + x, s.at.z + z + 1f);
                            float d = Mathf.Max(Mathf.Abs(hx - h), Mathf.Abs(hz - h)); if (d > 1f) steep++; if (d > worst) { worst = d; wAt = new Vector3(s.at.x + x, h, s.at.z + z); }
                        }
                    report.AppendLine($"    the ground within 40 m: {steep} one-meter steps steeper than 45 degrees; the steepest {Mathf.Atan(worst) * Mathf.Rad2Deg:0} degrees at ({wAt.x:0}, {wAt.z:0})");
                }
            }
        }

        // ---------------------------------------------------------------- conn
        IEnumerator R77Conn()
        {
            report.AppendLine(); report.AppendLine("=== A. The Canton Road Connector driven from BUG-003 to BUG-001 by the human-like driver (Drifter Twin, flat out up to 30 m/s)");
            var L = R77L(); if (L?.connector == null) { Check("conn: the lines file", false); yield break; }
            if (race.State != RaceController.Stage.Free) race.EndRace();
            yield return Vehicle("drifter"); yield return Clock(13f);
            var line = R77Flat(L.connector); var cum = Cum(line); float len = cum[cum.Length - 1];
            var f = Flat(line[2] - line[0]).normalized; respawn.PlaceAt(RaceRoute.Surface(line[0].x, line[0].z) + Vector3.up * .3f, Mathf.Atan2(f.x, f.z) * Mathf.Rad2Deg); race.NotifyTeleported(); yield return new WaitForSecondsRealtime(1.2f);
            var run = new HumanRun(); float maxRoll = 0f; Vector3 rollAt = Vector3.zero;
            var pics = new List<(string, float)> { ("77_conn_1", len * .15f), ("77_conn_2", len * .4f), ("77_conn_3", len * .65f), ("77_conn_4", len * .9f) };
            var watch = StartCoroutine(R73Watch(() => { float roll = Mathf.Abs(Vector3.SignedAngle(Vector3.up, Vector3.ProjectOnPlane(car.transform.up, car.transform.forward), car.transform.forward)); if (car.GroundedWheels >= 3 && roll > maxRoll) { maxRoll = roll; rollAt = car.Body.position; } }, () => run.time > 0f));
            yield return HumanDrive(line, 30f, 150f, run, null, null, pics, "77_conn_air");
            StopCoroutine(watch);
            report.AppendLine($"  {len:0} m: {(run.reachedEnd ? "reached the end" : "DID NOT reach the end")} in {run.time:0.0} s, top {run.topSpeed:0} m/s, longest in the air {run.maxAir:0.00} s, most roll on the ground {maxRoll:0.0} deg at ({rollAt.x:0}, {rollAt.z:0}), resets {run.resets}{run.note}");
            report.AppendLine($"  trace (station:speed, a = in the air):{run.trace}");
            Check("conn: the connector drives as a road (to the end, no reset, no spell in the air over 0.5 s)", run.reachedEnd && run.resets == 0 && run.maxAir < .5f, $"air {run.maxAir:0.00} s, resets {run.resets}");
        }

        // ---------------------------------------------------------------- hosp
        IEnumerator R77Hosp()
        {
            report.AppendLine(); report.AppendLine("=== B. Kennestone hospital from the street (the chase camera)");
            var L = R77L(); if (L?.hospital == null) { Check("hosp: the lines file", false); yield break; }
            if (race.State != RaceController.Stage.Free) race.EndRace();
            yield return Vehicle("drifter"); yield return Clock(13f);
            foreach (var v in L.hospital) { yield return R74Place(new Vector3(v.x, 0f, v.z), v.heading); yield return Capture(v.name); report.AppendLine($"  {v.name}: at ({v.x:0}, {v.z:0}) heading {v.heading:0}; ahead: {R74Ahead(car.Body.position, v.heading)}"); }
            // the skybridge over Church Street: solid, and room under it to drive (rays up from the road under it)
            var sky = FindObjectsByType<BoxCollider>(FindObjectsSortMode.None).Where(b => b.name == "Kennestone skybridge").ToList();
            float clear = float.MaxValue;
            foreach (var b in sky) { var p = b.bounds.center; float g = GroundQuery.Height(p.x, p.z); clear = Mathf.Min(clear, b.bounds.min.y - g); }
            report.AppendLine($"  skybridges: {sky.Count}, the lowest underside {clear:0.0} m over the road under it");
            Check("hosp: the skybridges stand (solid) with at least 4.5 m under them", sky.Count >= 1 && clear >= 4.5f, $"{sky.Count}, {clear:0.0} m");
            // drive under the one over Church Street, south to north (photo 02's view)
            yield return R74Place(new Vector3(1684.5f, 0f, -6815f), 349.5f); R73Pad();
            var start = car.Body.position; float t = 0f, minY = float.MaxValue; bool blocked = false;
            while (t < 9f) { R73Set(0f, .7f, 0f); yield return new WaitForFixedUpdate(); t += Time.fixedDeltaTime; if (t > 4f && car.Body.linearVelocity.magnitude < 1f) blocked = true; }
            R73Set(0, 0, 0); yield return Capture("77_B_under_skybridge_after");
            report.AppendLine($"  driven up Church Street under the skybridge: {(car.Body.position - start).magnitude:0} m in 9 s{(blocked ? " (STOPPED)" : "")}");
            Check("hosp: Church Street drives under the skybridge", !blocked && (car.Body.position - start).magnitude > 60f, $"{(car.Body.position - start).magnitude:0} m");
        }

        // ---------------------------------------------------------------- deck
        IEnumerator R77Deck()
        {
            report.AppendLine(); report.AppendLine("=== B. The Green deck (Kennestone, seven levels): from the street up every ramp to the roof and off its far side (the human-like driver, the Street Classic)");
            var L = R77L(); if (L?.deck?.line == null) { Check("deck: the lines file", false); yield break; }
            if (race.State != RaceController.Stage.Free) race.EndRace();
            yield return Vehicle("original"); yield return Clock(13f);
            var line = R77Flat(L.deck.line); var cum = Cum(line); float len = cum[cum.Length - 1];
            var f = Flat(line[1] - line[0]).normalized; respawn.PlaceAt(RaceRoute.Surface(line[0].x, line[0].z) + Vector3.up * .3f, Mathf.Atan2(f.x, f.z) * Mathf.Rad2Deg); race.NotifyTeleported(); yield return new WaitForSecondsRealtime(1.2f);
            float y0 = car.Body.position.y, top = y0, airMax = 0f, airNow = 0f; float roofY = y0 + (L.deck.levels - 1) * 4.0f;
            var run = new HumanRun();
            var pics = new List<(string, float)> { ("77_B_deck_ramp1", cum[3] + 6f), ("77_B_deck_level3", len * .45f), ("77_B_deck_roof", len - 40f) };
            var watch = StartCoroutine(R73Watch(() => { top = Mathf.Max(top, car.Body.position.y); }, () => true));
            // slow inside (8 m/s), then the roof's run at its gap at 16 m/s
            yield return HumanDrive(line, 18f, 240f, run, () => car.GroundedWheels == 0 && car.Body.position.y > roofY - 3f, s => s < len - 75f ? 7.5f : 18f, pics);
            // off the edge: hold the throttle, time the flight
            R73Pad(); float t = 0f; bool shot = false;
            while (t < 6f) { R73Set(0f, 1f, 0f); yield return new WaitForFixedUpdate(); t += Time.fixedDeltaTime; if (car.GroundedWheels == 0) { airNow += Time.fixedDeltaTime; airMax = Mathf.Max(airMax, airNow); if (!shot && airNow > .5f) { shot = true; yield return Capture("77_B_deck_jump"); } } else if (airNow > .3f) break; else airNow = 0f; }
            R73Set(0, 0, 0); yield return new WaitForSecondsRealtime(1.5f); yield return Capture("77_B_deck_landed");
            StopCoroutine(watch);
            report.AppendLine($"  the deck's ground {y0:0.0} m, its roof about {roofY:0.0} m; the car's highest {top:0.0} m; {(run.reachedEnd ? "to the end of the line" : $"stopped at {run.sEnd:0} of {len:0} m")} in {run.time:0} s, resets {run.resets}{run.note}");
            report.AppendLine($"  off the roof's gap: {airMax:0.00} s in the air; landed at ({car.Body.position.x:0}, {car.Body.position.y:0.0}, {car.Body.position.z:0}), upright {Vector3.Dot(car.transform.up, Vector3.up) > .6f}");
            report.AppendLine($"  trace:{run.trace}");
            Check("deck: driven to the roof (within 1.5 m of its height)", top >= roofY - 1.5f, $"{top - y0:0.0} of {roofY - y0:0.0} m");
            Check("deck: off the roof's far side, a real jump (over 1.5 s in the air)", airMax > 1.5f, $"{airMax:0.00} s");
        }

        // ---------------------------------------------------------------- rail
        IEnumerator R77Rail()
        {
            report.AppendLine(); report.AppendLine("=== C. The railroad: each bridge from its approach, each end, level crossings, the underpasses");
            var L = R77L(); if (L?.rail == null) { Check("rail: the lines file", false); yield break; }
            if (race.State != RaceController.Stage.Free) race.EndRace();
            yield return Vehicle("drifter"); yield return Clock(13f);
            int crossings = 0; var seen = new HashSet<string>();
            foreach (var p in L.rail)
            {
                if (p.cross && crossings++ >= 8) continue;
                string nm = "77_C_" + p.name; if (!seen.Add(nm)) continue;
                yield return R74Place(new Vector3(p.x, 0f, p.z), p.heading); yield return Capture(nm);
                report.AppendLine($"  {p.name}{(p.len > 0f ? $" ({p.len:0} m)" : "")}: at ({p.x:0}, {p.z:0}) heading {p.heading:0}; ahead: {R74Ahead(car.Body.position, p.heading)}");
            }
            // the line's own parts in the scene
            int track = GameObject.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None).Count(m => m.name.StartsWith("Railroad track_"));
            int bridges = GameObject.FindObjectsByType<MeshCollider>(FindObjectsSortMode.None).Count(m => m.name.StartsWith("Railroad bridge_"));
            report.AppendLine($"  the scene: {track} track pieces, {bridges} bridge pieces");
            Check("rail: the railroad is in the scene with its bridges", track > 20 && bridges > 0, $"{track}, {bridges}");
        }
        IEnumerator R77RailMap()
        {
            report.AppendLine(); report.AppendLine("=== C. The map screen over the line");
            var map = WorldMap.Instance; if (!map) { Check("railmap: the map", false); yield break; }
            foreach (var (name, at, z) in new[] { ("whole", new Vector3(-2300f, 0f, -3800f), 1f), ("marietta", new Vector3(1700f, 0f, -8500f), 6f), ("north", new Vector3(-2000f, 0f, -3000f), 3f) })
            {
                yield return R74Place(at, 0f, 1.2f); map.Open(); yield return new WaitForSecondsRealtime(1f);
                map.ZoomForCheck(z); yield return new WaitForSecondsRealtime(.8f); yield return Capture($"77_C_map_{name}"); map.Close(); yield return new WaitForSecondsRealtime(.4f);
                report.AppendLine($"  map {name} zoom {z}: picture 77_C_map_{name}.png");
            }
        }

        // ---------------------------------------------------------------- square
        IEnumerator R77Square()
        {
            report.AppendLine(); report.AppendLine("=== D. Marietta Square from the place of each of Dan's photos (the chase camera)");
            var L = R77L(); if (L?.square == null) { Check("square: the lines file", false); yield break; }
            if (race.State != RaceController.Stage.Free) race.EndRace();
            yield return Vehicle("drifter"); yield return Clock(13f);
            foreach (var v in L.square) { yield return R74Place(new Vector3(v.x, 0f, v.z), v.heading); yield return Capture(v.name); report.AppendLine($"  {v.name}: at ({v.x:0}, {v.z:0}) heading {v.heading:0}; ahead: {R74Ahead(car.Body.position, v.heading)}"); }
            string[] solid = { "Marietta fountain rim", "Marietta liberty bell", "Marietta play train", "Marietta pavilion" };
            foreach (var s in solid) { int n = FindObjectsByType<BoxCollider>(FindObjectsSortMode.None).Count(b => b.name == s); report.AppendLine($"  {s}: {n} colliders"); Check($"square: {s} is solid", n > 0, n.ToString()); }
        }
    }
}
