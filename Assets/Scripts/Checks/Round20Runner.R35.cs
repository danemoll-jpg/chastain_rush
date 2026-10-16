using System.Collections;
using UnityEngine;

namespace ChastainRush
{
    // Round 35 Part A in the exe: ChastainRush.exe -exeRound35 <dir> -r35Only line -logFile <dir>\Player.log. The Underground race's
    // line (the start: one line) on M5's straight: how straight the 60 m before it are, and a picture of it from 30 m before it.
    // The 3-lap race with the AI: -exeRound28 <dir> -r28Only race.
    public sealed partial class Round20Runner
    {
        IEnumerator R35Line()
        {
            report.AppendLine(); report.AppendLine("=== Part A: the Underground race's line on a straight");
            RaceController.RouteOverride = "underground.json"; RaceController.DirectionOverride = "forward"; RaceController.LapsOverride = 3;
            RaceController.AiCount = 0;
            if (race.State != RaceController.Stage.Free) race.EndRace();
            race.Reload(); race.StartRace();
            while (race.State == RaceController.Stage.Countdown) yield return null;
            var r = race.Route; float g = r.GateS;
            r.PointAt(g, out var fLine); float worst = 0f;
            for (float d = 5f; d <= 60f; d += 5f) { r.PointAt(g - d, out var f); worst = Mathf.Max(worst, Vector3.Angle(new Vector3(f.x, 0, f.z), new Vector3(fLine.x, 0, fLine.z))); }
            r.PointAt(g + 15f, out var fAfter);
            Check("the line has 60 m of straight before it (heading within 3 degrees of the line's) and 15 m after it", worst < 3f && Vector3.Angle(fAfter, fLine) < 3f,
                  $"line at lap station {g:0.0} m of {r.LapLength:0.0} m (lap unchanged), widest heading change in the 60 m before it {worst:0.0} deg, 15 m after {Vector3.Angle(fAfter, fLine):0.0} deg, run-in from the grid {r.RunInLength:0} m");
            var at = r.PointAt(g - 30f, out _); var line = r.PointAt(g, out _);
            yield return Still("35_line_from_30m", at + Vector3.up * 1.6f, line + Vector3.up * 1.6f, 62f);
            race.EndRace(); RaceController.AiCount = 3;
        }
    }
}
