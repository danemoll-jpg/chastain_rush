using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Racer;
using UnityEngine;

namespace ChastainRush
{
    // Round 38 in the exe: ChastainRush.exe -exeRound38 <dir> [-r38Only bankhop,bankthrough,bankpics] -logFile <dir>\Player.log. Muted (CheckRun), its
    // files in <dir>, report round38_check.txt.
    //   bankhop      Part A: K1 (the BANK HOP) flown flat out on the Clay Hopper and the Skywhip 250 (the player's input): launched, onto the bank's roof,
    //                upright; on round the roof to the BANK DROP, launched, down on its landing, upright, rolled on
    //   bankthrough  Part A: the line through the bank, from the bus gap's run-out in at its boarded doors and out of its bay: boards broken, on
    //   bankpics     Part A: pictures (the bank from K1's run-up, from above, the hop in the air, the roof, inside)
    public sealed partial class Round20Runner
    {
        static float BankRoofY => StuntPark.Current && StuntPark.Current.bankRoofY > 1f ? StuntPark.Current.bankRoofY : 328.633f;
        Vector3 BankRoof(float u, float v) { var w = PW(u, v); return new Vector3(w.x, BankRoofY, w.y); }
        string Under(Vector3 at)
        {
            var hit = Physics.RaycastAll(at + Vector3.up, Vector3.down, 6f, ~0, QueryTriggerInteraction.Ignore).Where(h => h.collider.attachedRigidbody != car.Body).OrderBy(h => h.distance).FirstOrDefault();
            return hit.collider ? hit.collider.name : "nothing";
        }

        sealed class HopState { public bool hopped, dropped, done, shot, roofShot; public float vHop = -1f, vDrop = -1f, air, hopAir, dropAir, upRoof = -1f, upLand = -1f, roofY, t, onRoofAt = -1f; public string onRoof = "", onLanding = ""; public List<string> trace = new() { "t,u,v,y,speed,wheels,yaw" }; }
        IEnumerator TrackHop(HopState st, BigJump hop, BigJump drop, string id)
        {
            while (!st.done)
            {
                yield return new WaitForFixedUpdate(); st.t += Time.fixedDeltaTime;
                var bp = car.Body.position; var q = new Vector2(bp.x, bp.z); st.trace.Add($"{st.t:0.00},{Vector2.Dot(q, PU):0.00},{Vector2.Dot(q, PV):0.00},{bp.y:0.00},{car.Body.linearVelocity.magnitude:0.0},{car.GroundedWheels},{car.transform.eulerAngles.y:0}");
                if (car.GroundedWheels == 0) st.air += Time.fixedDeltaTime;
                else
                {
                    if (st.air > .6f && st.hopped && !st.dropped && st.onRoof == "") { st.onRoof = Under(bp); st.upRoof = car.transform.up.y; st.roofY = bp.y; st.hopAir = st.air; st.onRoofAt = st.t; }
                    if (st.air > .6f && st.dropped && st.onLanding == "") { st.onLanding = Under(bp); st.upLand = car.transform.up.y; st.dropAir = st.air; }
                    st.air = 0f;
                }
                if (id == StuntIds[0] && st.hopped && !st.shot && st.air > 1.0f) { st.shot = true; yield return Capture("38_bank_hop_in_the_air"); }
                if (id == StuntIds[0] && st.dropped && !st.roofShot && st.air > .7f) { st.roofShot = true; yield return Capture("38_bank_drop_in_the_air"); }
            }
        }
        IEnumerator R38BankHop()
        {
            report.AppendLine(); report.AppendLine("=== Part A: K1, the BANK HOP, flat out onto the bank's roof, then round the roof and off the BANK DROP (the player's input)");
            if (!StuntPark.Current || StuntPark.Current.bankRoofY < 1f) { Check("the bank is the park's in this build", false); yield break; }
            if (race.State != RaceController.Stage.Free) race.EndRace();
            var hop = BigJump.All.FirstOrDefault(x => x.id == "park-K1"); var drop = BigJump.All.FirstOrDefault(x => x.id == "park-BANKDROP");
            if (!hop || !drop) { Check("the bank hop and the bank drop are in the park", false); yield break; }
            foreach (var id in StuntIds)
            {
                yield return Vehicle(id);
                var a = Floor(3566f, 377.5f);
                Place(a, PDir(1, 0), 25f); yield return new WaitForFixedUpdate();
                var st = new HopState();
                System.Action<BigJump, ArcadeVehicle, float> hook = (bj, c, v) => { if (c != car) return; if (bj == hop) { st.hopped = true; st.vHop = v; } if (bj == drop) { st.dropped = true; st.vDrop = v; } };
                BigJump.Launched += hook; StartCoroutine(TrackHop(st, hop, drop, id));
                // flat out at K1 (its lip sends everyone at its design speed), then, down on the roof, round to the drop as a player would (at a roof's pace)
                var run1 = new Run(); yield return StartCoroutine(DriveInput(new List<Vector3> { a, BankRoof(3601f, 377.5f), BankRoof(3609f, 377.5f) }, 30f, 12f, run1, null, r => st.onRoofAt > 0f));
                var run2 = new Run();
                if (st.onRoofAt > 0f) yield return StartCoroutine(DriveInput(new List<Vector3> { car.Body.position, BankRoof(3603.5f, 382f), BankRoof(3605f, 386f), BankRoof(3605f, 392.5f), Floor(3605f, 412f), Floor(3605f, 440f) }, 5f, 20f, run2));   // (braking into the turn; the drop sends it off at its own speed)
                yield return new WaitForSecondsRealtime(.5f); st.done = true; BigJump.Launched -= hook;
                File.WriteAllLines(Path.Combine(outputDir, $"bankhop_trace_{id}.csv"), st.trace);
                report.AppendLine($"  {Nm(id),-16} hop: {(st.hopped ? $"onto K1's lip at {st.vHop:0.0} m/s, {st.hopAir:0.00} s up, came down on {st.onRoof} at {st.roofY:0.00} m (the bank's roof {BankRoofY:0.00}), upright {st.upRoof:0.00}" : "NEVER LAUNCHED")}; " +
                                    $"drop: {(st.dropped ? $"onto its lip at {st.vDrop:0.0} m/s, {st.dropAir:0.00} s up, came down on {st.onLanding}, upright {st.upLand:0.00}, " + (run2.reached ? "rolled on" : "stopped " + run2.end) : "NEVER LAUNCHED")}");
                Check($"K1 on the {Nm(id)}: launched and landed upright on the bank's roof", st.hopped && st.onRoof.Contains("roof") && st.upRoof > .7f && st.roofY > BankRoofY - .5f);
                Check($"the BANK DROP on the {Nm(id)}: launched off the roof, landed upright on its landing and rolled on", st.dropped && st.onLanding.Contains("landing") && st.upLand > .7f && run2.reached);
            }
        }

        IEnumerator R38BankThrough()
        {
            report.AppendLine(); report.AppendLine("=== Part A: the line through the bank (in at its boarded front doors, out of its boarded bay)");
            if (!StuntPark.Current || StuntPark.Current.bankRoofY < 1f) { Check("the bank is the park's in this build", false); yield break; }
            if (race.State != RaceController.Stage.Free) race.EndRace();
            var chunks = StuntPark.Current.breakables.Where(c => c && c.name.StartsWith("StuntParkBankBoards")).ToArray();
            foreach (var id in StuntIds.Concat(new[] { "original" }))
            {
                yield return Vehicle(id); StuntPark.Current.RestoreBreakables(); yield return new WaitForSecondsRealtime(.2f);
                var a = Floor(3560f, 388f); var b = Floor(3634f, 388f);
                Place(a, b - a, 16f); yield return new WaitForFixedUpdate();
                var run = new Run(); yield return StartCoroutine(DriveInput(Straight(a, b), 20f, 20f, run));
                int broken = chunks.Sum(c => c.broken.Count(x => x));
                if (id == StuntIds[0]) yield return Capture("38_bank_through_out_of_the_bay");
                report.AppendLine($"  {Nm(id),-16} {broken} of {chunks.Sum(c => c.broken.Length)} sheets broken, {(run.reached ? "out the far side" : "stopped " + run.end)}, slowest {run.minSpeed:0.0} m/s");
                Check($"through the bank on the {Nm(id)}: both boarded doorways broken and out of the far side", broken >= 4 && run.reached);
            }
            StuntPark.Current.RestoreBreakables(); yield return new WaitForSecondsRealtime(.2f);
            Check("the boards come back with the park's breakables", chunks.Sum(c => c.broken.Count(x => x)) == 0);
        }

        IEnumerator R38BankPics()
        {
            report.AppendLine(); report.AppendLine("=== Part A: pictures");
            if (!StuntPark.Current) yield break;
            yield return Vehicle("stuntbuggy"); Place(Floor(3560f, 377.5f), PDir(1, 0), 0f); yield return new WaitForSecondsRealtime(1f);
            yield return Still("38_bank_from_k1_runup", Floor(3548f, 380f) + Vector3.up * 3f, BankRoof(3600f, 380f) + Vector3.down * 3f, 65f);
            yield return Still("38_bank_from_above", BankRoof(3570f, 340f) + Vector3.up * 28f, BankRoof(3602f, 385f), 60f);
            yield return Still("38_bank_roof", BankRoof(3596f, 362f) + Vector3.up * 2.5f, BankRoof(3605f, 395f), 70f);
            yield return Still("38_bank_drop_landing", BankRoof(3605f, 386f) + Vector3.up * 3f, Floor(3605f, 425f), 65f);
            yield return Still("38_bank_inside", Floor(3594.5f, 388f) + Vector3.up * 2.2f, Floor(3609f, 380f) + Vector3.up * 1.5f, 75f);
            yield return Still("38_bank_east_bay", Floor(3628f, 396f) + Vector3.up * 2.5f, Floor(3609f, 388f) + Vector3.up * 2f, 65f);
            Check("pictures of the bank written", true, "38_bank_*.png");
        }
    }
}
