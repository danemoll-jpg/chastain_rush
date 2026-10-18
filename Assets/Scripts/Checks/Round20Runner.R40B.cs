using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Racer;
using UnityEngine;

namespace ChastainRush
{
    // Round 40 Parts B and C in the exe (-exeRound40 <dir> -r40Only lines,median,cantoncop,houses [-hiddenPolice]):
    //   lines      B1: a picture from Dan's BUG-008 spot (Canton Road's carriageways at Worley Drive)
    //   median     B2: pictures of New Chastain Road's median from Dan's BUG-009 spot and one other stretch
    //   cantoncop  B3: a pass at 10+ mph over the limit on Canton Road in front of one of its hidden cruisers starts a chase
    //   houses     C: pictures from Dan's BUG-010 and BUG-011 spots
    public sealed partial class Round20Runner
    {
        // a still from one of Dan's report cameras (position, heading, pitch), the player parked out of the view
        IEnumerator DanView(string name, Vector3 cam, float heading, float pitch, Vector3 parkAt)
        {
            car.Body.position = RaceRoute.Surface(parkAt.x, parkAt.z) + Vector3.up; car.Body.linearVelocity = Vector3.zero; race.NotifyTeleported();
            yield return Still(name, cam, cam + Quaternion.Euler(pitch, heading, 0f) * Vector3.forward * 40f, 60f);
        }

        IEnumerator R40Lines()
        {
            report.AppendLine(); report.AppendLine("=== B1: lines across the road (BUG-008, Canton Road at Worley Drive)");
            if (race.State != RaceController.Stage.Free) race.EndRace();
            yield return Clock(12f);
            yield return DanView("40_bug008_lines", new Vector3(3564.21f, 336.69f, 148.31f), 156.12f, 13.5f, new Vector3(3530f, 0f, 260f));
            yield return Still("40_bug008_from_above", new Vector3(3568f, 360f, 150f), new Vector3(3568f, 330f, 128f), 60f);
            Check("B1: pictures written from Dan's spot", true, "40_bug008_*.png");
        }

        IEnumerator R40Median()
        {
            report.AppendLine(); report.AppendLine("=== B2: New Chastain Road's median");
            if (race.State != RaceController.Stage.Free) race.EndRace();
            yield return Clock(12f);
            yield return DanView("40_bug009_median", new Vector3(3057.61f, 300.59f, 760.38f), 311.83f, 13.5f, new Vector3(3000f, 0f, 700f));
            yield return DanView("40_median_stretch2", new Vector3(3322f, 317f, 492f), 335f, 10f, new Vector3(3380f, 0f, 520f));
            yield return Still("40_median_from_above", new Vector3(3300f, 380f, 520f), new Vector3(3290f, 312f, 545f), 60f);
            // the ground across the median at a few places: its slope and its lumps (the largest departure from a straight line across)
            var sb = new System.Text.StringBuilder();
            foreach (var (x, z) in new[] { (3057.6f, 760.4f), (3200f, 700f), (3300f, 540f), (3330f, 490f) })
            {
                var q = new Vector3(x, 0f, z); var hits = new List<float>();
                for (float o = -12f; o <= 12f; o += .5f)
                {
                    var p = q + new Vector3(o * .87f, 0f, o * .5f);
                    if (Physics.Raycast(p + Vector3.up * 500f, Vector3.down, out var h, 800f, ~0, QueryTriggerInteraction.Ignore)) hits.Add(h.point.y); else hits.Add(float.NaN);
                }
                sb.Append($"({x:0}, {z:0}): " + string.Join(" ", hits.Select(v => float.IsNaN(v) ? "-" : v.ToString("0.00"))) + "; ");
            }
            report.AppendLine("  heights across (24 m, west to east): " + sb);
            Check("B2: pictures written from Dan's spot and another stretch", true, "40_bug009_median.png, 40_median_stretch2.png, 40_median_from_above.png");
        }

        IEnumerator R40CantonCop()
        {
            report.AppendLine(); report.AppendLine("=== B3: a hidden cruiser on Canton Road (Free Roam, -hiddenPolice)");
            if (race.State != RaceController.Stage.Free) race.EndRace();
            yield return Vehicle("fastback");
            var hp = HiddenPolice.Current; if (!hp) { Check("hidden police are in the build", false); yield break; }
            report.AppendLine($"  Canton Road's hiding places: {string.Join(", ", hp.Canton.Select(s => $"({s.pos.x:0}, {s.pos.z:0}) facing {Mathf.Atan2(s.forward.x, s.forward.z) * Mathf.Rad2Deg:0}"))}; in use {string.Join(", ", hp.Active.Select(s => s.road))}");
            Check("B3: one of Canton Road's hiding places is in use", hp.Active.Any(hp.Canton.Contains), $"{hp.Canton.Count} Canton Road places");
            hp.LastEnd = -999; hp.AttachedAt = -999;
            foreach (var spot in hp.Canton)
            {
                if (!hp.Active.Contains(spot)) hp.Active.Add(spot);
                // southbound on Canton Road past it, from 200 m north to 120 m south
                var path = Straight(RaceRoute.Surface(3563.2f, spot.pos.z + 200f), RaceRoute.Surface(3563.6f, spot.pos.z - 120f), 3f).Select(q => RaceRoute.Surface(q.x, q.z)).ToList();
                respawn.PlaceAt(path[0] + Vector3.up * .5f, 180f); race.NotifyTeleported(); yield return new WaitForSecondsRealtime(2.5f);
                int limit = PoliceKit.LimitAt(path[path.Count / 2], out _);
                yield return Still($"40_canton_cop_{spot.pos.z:0}", spot.pos - spot.forward * 16f + Vector3.up * 3.5f + Vector3.forward * 8f, spot.pos + Vector3.up, 60f);
                var run = new Run(); bool tripped = false; float speedAt = 0;
                Place(path[0], path[1] - path[0], PoliceKit.Mps(limit + 18));
                var drive = StartCoroutine(DriveLine(path, PoliceKit.Mps(limit + 20), 25f, run, null, r2 => GetawayChase.Current != null));
                float tq = Time.realtimeSinceStartup; while (!tripped && !run.reached && run.end == "" && Time.realtimeSinceStartup - tq < 40f) { yield return null; if (GetawayChase.Current && !tripped) { tripped = true; speedAt = PoliceKit.Mph(car.Body.linearVelocity.magnitude); yield return Capture($"40_canton_clocked_{spot.pos.z:0}"); } }
                yield return drive;
                Check($"B3: speeding past the hidden cruiser at ({spot.pos.x:0}, {spot.pos.z:0}) on Canton Road starts a chase", tripped && GetawayChase.Current, $"{(tripped ? $"CLOCKED at {speedAt:0} mph in a {limit}: {HiddenPolice.Flash}" : "not tripped")}; {hp.DebugLine}");
                GetawayChase.End(); HiddenPolice.Ended(false); hp.LastEnd = -999; yield return new WaitForSecondsRealtime(2f);
            }
        }

        IEnumerator R40Houses()
        {
            report.AppendLine(); report.AppendLine("=== C: the two buried houses");
            if (race.State != RaceController.Stage.Free) race.EndRace();
            yield return Clock(12f);
            yield return DanView("40_bug010_house", new Vector3(1883.40f, 305.90f, 645.06f), 251.19f, 13.5f, new Vector3(1910f, 0f, 680f));
            yield return DanView("40_bug011_house", new Vector3(590.21f, 323.59f, 858.85f), 217.79f, 13.38f, new Vector3(620f, 0f, 900f));
            yield return Still("40_bug010_side", new Vector3(1872.6f, 0f, 635.2f) + new Vector3(-14f, 0f, -10f) + Vector3.up * (RaceRoute.Surface(1872.6f, 635.2f).y + 3f), RaceRoute.Surface(1872.6f, 635.2f) + Vector3.up * 2f, 60f);
            yield return Still("40_bug011_side", RaceRoute.Surface(567.7f, 839.0f) + new Vector3(-16f, 4f, -14f), RaceRoute.Surface(567.7f, 839.0f) + Vector3.up * 1.5f, 60f);
            Check("C: pictures written from Dan's two spots", true, "40_bug010_*.png, 40_bug011_*.png");
        }
    }
}
