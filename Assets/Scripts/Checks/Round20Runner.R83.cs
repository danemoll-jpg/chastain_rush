using System.Collections;
using UnityEngine;

namespace ChastainRush
{
    // Round 83 in the exe (-exeRound83 <dir> [-r83Only spots,lines] [-r82Lines <file>] -r20Minutes 300 -logFile <dir>\Player.log), from the repo root:
    //   spots  a picture from each place this round fixed, on the surface nearest the height given, heading along the road (the chase camera)
    //   lines  Round 82's line drives (R82LinesPart) on the file given with -r82Lines (python Tools/mapgen/r82_lines.py, local/round83/*.json)
    public sealed partial class Round20Runner
    {
        static readonly (string id, Vector3 at, float yaw, string what)[] R83Spots =
        {
            ("A_merge_south", new Vector3(527f, 338f, -3383f), 153f, "A: the I-75 Express Lanes southbound under the I-575 Express flyover, where Round 82's car stuck"),
            ("A_merge_north", new Vector3(513f, 338f, -3332f), 333f, "A: the I-75 Express Lanes northbound at the flyover's crossing"),
            ("A_join", new Vector3(845f, 330f, -3832f), 306f, "A: the I-575 flyover's road joining the I-75 Express Lanes, northbound"),
            ("B_skipspann", new Vector3(-1150f, 340f, 775f), 90f, "B: Skip Spann Connector over I-75, eastbound"),
            ("B_frey", new Vector3(-1379f, 300f, 1610f), 180f, "B: Frey Road under the I-75 decks, southbound"),
            ("C_ramp", new Vector3(2380f, 390f, -4955f), 40f, "C: ramp 437897716 over I-75 beside the Canton Road Connector"),
            ("D_greers", new Vector3(121.42f, 358.76f, -2636.08f), 90.3f, "D: Dan's BUG-005 place: Greers Chapel's bridge and the median beside it"),
            ("D_seam", new Vector3(536f, 330f, -37f), 169f, "D: I-575's Express Lanes at the seam deck, southbound"),
            ("D_wall", new Vector3(754f, 330f, -1385f), 178f, "D: I-575's Express Lanes where the walls were scraped"),
        };

        IEnumerator R83SpotsPart()
        {
            report.AppendLine(); report.AppendLine("=== Round 83's places, a picture from each (the chase camera, on the surface nearest the height given)");
            if (race.State != RaceController.Stage.Free) race.EndRace();
            yield return Vehicle("original"); ExpressLanes.ForceHour = 15f; ExpressLanes.ApplyNow(); yield return Clock(13f);
            foreach (var sp in R83Spots)
            {
                yield return R82Place(sp.at, sp.yaw);
                yield return Capture($"83_{sp.id}");
                var p = car.Body.position;
                report.AppendLine($"  {sp.id} ({sp.what}): at ({p.x:0.0}, {p.y:0.0}, {p.z:0.0}) heading {sp.yaw:0}; picture 83_{sp.id}.png");
                Flush();
            }
            ExpressLanes.ForceHour = float.NaN;
        }
    }
}
