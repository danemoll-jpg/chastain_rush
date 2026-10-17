using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Racer;
using UnityEngine;

namespace ChastainRush
{
    // Round 38 Parts B to D in the exe (-exeRound38 <dir> -r38Only ...):
    //   smash       Part B: on the Clay Hopper in at Chastain Cleaners' window, along the shops through the partitions and their furniture, out of the
    //               pizza window: the smashes in the combo; then the same with a backflip out of the window: BACKFLIP + SMASH, the flight's x2
    //   shoprow     Part C: the shop row end to end (in at the coffee shop's window, west through every shop to the supermarket): what broke
    //   shopramp    Part C: from the supermarket's north end up the shop ramp onto the roof and over the roof gap
    //   vault       Part C: through the bank: the teller counter, the vault door, the cash carts, out of the bay
    //   cpics       Part C: pictures of the rooms, the shop ramp, the vault
    //   cfps        Part C: the frame rate inside (the shop row, the supermarket, the bank) with the new rooms and without them (hidden for "before")
    //   rivals      Part D: a Stunt event with 3 rivals (a 180 s round for the check): each scores, none leaves the park or sticks; pictures
    //   splitai     Part D: split screen's AI player 2 in the stunt park rides the park's lines (BUG-001), scored as a player
    public sealed partial class Round20Runner
    {
        int Broken(params string[] prefixes) => StuntPark.Current.breakables.Where(c => c && prefixes.Any(p => c.name.StartsWith(p))).Sum(c => c.broken.Count(x => x));
        List<Vector3> Pts(params (float u, float v, char b)[] q) => q.Select(x => x.b == 's' ? Store(x.u, x.v) : x.b == 'r' ? Roof(x.u, x.v) : x.b == 'b' ? BankRoof(x.u, x.v) : Floor(x.u, x.v)).ToList();

        IEnumerator R38Smash()
        {
            report.AppendLine(); report.AppendLine("=== Part B: smashing counts for stunts (the player's input on the Clay Hopper)");
            if (!StuntPark.Current) { Check("the stunt park is in the build", false); yield break; }
            if (race.State != RaceController.Stage.Free) race.EndRace();
            yield return Vehicle("stuntbuggy"); var acts = car.GetComponent<ArcadeActivities>();
            var line = Pts((3506f, 452f, 'f'), (3505.5f, 463f, 'f'), (3505.5f, 469.5f, 's'), (3507f, 476f, 's'), (3513f, 480f, 's'), (3525f, 480f, 's'), (3529f, 479f, 's'), (3532f, 476f, 's'), (3532f, 472.2f, 's'), (3532f, 470.5f, 's'), (3532f, 445f, 'f'), (3532f, 425f, 'f'));
            foreach (bool flip in new[] { false, true })
            {
                StuntPark.Current.RestoreBreakables(); Place(line[0], line[1] - line[0], 3f); yield return new WaitForSecondsRealtime(1.2f); acts.NewSession(); yield return new WaitForSecondsRealtime(.5f);
                int s0 = acts.Smashes; float turned = 0f; bool shot = false;
                System.Func<float, Vector3> stick = t => { if (!flip || t < .08f || turned >= 340f) return Vector3.zero; turned += Mathf.Abs(car.transform.InverseTransformDirection(car.Body.angularVelocity).x * Time.fixedDeltaTime * Mathf.Rad2Deg); return new Vector3(-Mathf.Clamp((360f - turned) / 60f, .15f, 1f), 0, 0); };
                var run = new Run(); string before = acts.LastStunt; var seen = new List<string>();
                var pz = BigJump.All.FirstOrDefault(x => x.id == "park-PIZZA"); bool pzl = false; float pzv = 0f; System.Action<BigJump, ArcadeVehicle, float> ph = (bj, c, v) => { if (bj == pz && c == car) { pzl = true; pzv = v; } }; BigJump.Launched += ph; bool lipShot = false;
                var broke = new List<string>(); System.Action<FenceChunk, int, Vector3> pb = (c, i, at) => { var q = new Vector2(at.x, at.z); broke.Add($"{c.name} #{i} at u {Vector2.Dot(q, PU):0.0} v {Vector2.Dot(q, PV):0.0} y {at.y:0.0} (the car at u {Vector2.Dot(new Vector2(car.Body.position.x, car.Body.position.z), PU):0.0} v {Vector2.Dot(new Vector2(car.Body.position.x, car.Body.position.z), PV):0.0}, wheels {car.GroundedWheels})"); }; FenceChunk.PanelBroken += pb;
                var drive = StartCoroutine(DriveInput(line, 8f, 40f, run, stick));
                var combo = new List<string>(); string lastC = "";
                float w = 0f; while (!run.reached && run.end == "" && w < 40f) { yield return null; w += Time.deltaTime; { string c = $"x{acts.ComboLinks} {acts.ComboTotal} wheels {car.GroundedWheels} '{acts.LastResult}'"; if (c != lastC) { lastC = c; combo.Add($"{w:0.00}s {c}"); } } if (!flip && !lipShot && pz && Flat(car.Body.position - pz.lip).magnitude < 1.2f) { lipShot = true; yield return Capture("38_dbg_at_the_pizza_lip"); } var l = acts.LastResult; if (l.StartsWith("SMASH") && (seen.Count == 0 || seen[seen.Count - 1] != l)) seen.Add(l); if (!shot && car.GroundedWheels == 0 && acts.Live != null && acts.Live.names.Any(n => n.StartsWith("SMASH"))) { shot = true; yield return Capture(flip ? "38_smash_backflip_out_of_the_pizza_window" : "38_smash_out_of_the_pizza_window"); } }
                yield return drive; BigJump.Launched -= ph; File.WriteAllLines(Path.Combine(outputDir, $"smash_combo_{(flip ? "flip" : "plain")}.txt"), combo); FenceChunk.PanelBroken -= pb; report.AppendLine("    broke: " + string.Join("; ", broke));
                report.AppendLine($"  the pizza kicker: {(pzl ? $"launched (onto its lip at {pzv:0.0} m/s)" : "NOT launched")}; window panes broken {Broken("StuntParkWindows")}");
                yield return new WaitForSecondsRealtime(ArcadeActivities.ComboGap + 1f);
                report.AppendLine($"  {(flip ? "with a backflip" : "plain")}: {acts.Smashes - s0} smashes ({string.Join(" | ", seen.Distinct().Take(8))}); the flight out of the window: '{acts.LastStunt.Replace("\n", " / ")}' {acts.LastStuntPoints:N0} pts, x{acts.LastFlightTypes}; the combo banked {acts.LastComboTotal:N0} pts x{acts.LastComboLinks}: {acts.LastComboTricks}");
                if (!flip) Check("a run in through a shop window, along the shops and out of the pizza window: the smashes are links of the combo (SM) and the flight out counts SMASH", acts.Smashes - s0 >= 3 && acts.LastComboTricks.Contains("SM") && acts.LastStunt.Contains("SMASH") && acts.LastComboLinks >= 2);
                else Check("a backflip out of the pizza window: BACKFLIP + SMASH, the smash a trick type of its own (the flight's x2)", acts.LastStunt.Contains("BACKFLIP") && acts.LastStunt.Contains("SMASH") && acts.LastFlightTypes >= 2);
            }
            var pts = new[] { "StuntParkWindows", "StuntParkPartitions", "StuntParkFurniture", "StuntParkShopCounters", "StuntParkBankBoards", "StuntParkVault" };
            report.AppendLine("  the points (ArcadeActivities.Value): " + string.Join(", ", pts.Select(n => { var c = StuntPark.Current.breakables.FirstOrDefault(b => b && b.name.StartsWith(n)); var v = ArcadeActivities.Value(c); return c ? $"{v.name} {v.points}" : n + " (none)"; })) + $", TREE {ArcadeActivities.TreePoints}, GAS PUMP 600, HYDRANT 200, STREET LAMP 100, MAILBOX 75, SIGN 75, FENCE 25");
        }

        IEnumerator R38ShopRow()
        {
            report.AppendLine(); report.AppendLine("=== Part C: the shop row end to end on the Skywhip 250 (in at the coffee shop's window, west through every shop)");
            if (!StuntPark.Current) { Check("the stunt park is in the build", false); yield break; }
            if (race.State != RaceController.Stage.Free) race.EndRace();
            yield return Vehicle("stuntbike"); StuntPark.Current.RestoreBreakables(); yield return new WaitForSecondsRealtime(.3f);
            var rooms = new[] { "StuntParkFurniture", "StuntParkShopCounters", "StuntParkDisplays", "StuntParkShelves", "StuntParkAppliances" };
            int total = StuntPark.Current.breakables.Where(c => c && rooms.Any(p => c.name.StartsWith(p))).Sum(c => c.panels.Length);
            var line = Pts((3585f, 452f, 'f'), (3585f, 469.5f, 's'), (3584f, 474.5f, 's'), (3575f, 475f, 's'), (3470f, 475f, 's'), (3450f, 470f, 's'), (3440f, 455f, 's'));
            Place(line[0], line[1] - line[0], 4f); yield return new WaitForSecondsRealtime(.3f);
            var run = new Run(); yield return StartCoroutine(DriveInput(line, 9f, 60f, run));
            yield return Capture("38_shoprow_end_to_end");
            report.AppendLine($"  {(run.reached ? "into the supermarket" : "stopped " + run.end)} in {run.time:0.0} s; broke {Broken("StuntParkPartitions")} partition panels, {Broken(rooms)} of {total} things in the rooms, {Broken("StuntParkWindows")} window panes; slowest {run.minSpeed:0.0} m/s");
            Check("the shop row driven end to end through its rooms (in at the east end, out into the supermarket)", run.reached && Broken("StuntParkPartitions") >= 8);
            StuntPark.Current.RestoreBreakables();
        }

        IEnumerator R38ShopRamp()
        {
            report.AppendLine(); report.AppendLine("=== Part C: the shop ramp, from the supermarket's north end up through the roof and over the roof gap");
            if (!StuntPark.Current) { Check("the stunt park is in the build", false); yield break; }
            if (race.State != RaceController.Stage.Free) race.EndRace();
            var gap = BigJump.All.FirstOrDefault(x => x.id == "park-GAP");
            foreach (var id in StuntIds)
            {
                yield return Vehicle(id); StuntPark.Current.RestoreBreakables(); yield return new WaitForSecondsRealtime(.3f);
                var line = Pts((3437f, 452f, 's'), (3446f, 470f, 's'), (3453f, 480f, 's'), (3459f, 481.5f, 's'), (3497f, 481.5f, 'r'), (3510.5f, 481.5f, 'r'), (3540f, 481.5f, 'r'), (3550f, 481.5f, 'r'));
                Place(line[0], line[1] - line[0], 6f); yield return new WaitForSecondsRealtime(.2f);
                bool launched = false; float top = 0f; System.Action<BigJump, ArcadeVehicle, float> hook = (bj, c, v) => { if (bj == gap && c == car) launched = true; }; BigJump.Launched += hook;
                var run = new Run(); var drive = StartCoroutine(DriveInput(line, 20f, 30f, run)); bool shot = false;
                while (!run.reached && run.end == "" && run.time < 29.9f) { yield return new WaitForFixedUpdate(); top = Mathf.Max(top, car.Body.position.y); if (!shot && id == StuntIds[0] && car.Body.position.y > ParkRoofY - 4f && Vector2.Dot(new Vector2(car.Body.position.x, car.Body.position.z), PU) < 3492f) { shot = true; yield return Capture("38_shop_ramp_climbing"); } }
                yield return drive; BigJump.Launched -= hook;
                float y = car.Body.position.y;
                report.AppendLine($"  {Nm(id),-16} up the ramp: highest {top:0.00} m (the roof {ParkRoofY:0.00}); the roof gap {(launched ? "launched" : "NOT launched")}; ended at {y:0.00} m, {(run.reached ? "on the far side of the gap" : "stopped " + run.end)}");
                Check($"the shop ramp on the {Nm(id)}: up onto the roof and over the roof gap", launched && run.reached && y > ParkRoofY - 1f);
            }
        }

        IEnumerator R38Vault()
        {
            report.AppendLine(); report.AppendLine("=== Part C: through the bank: the teller counter, the vault door, the cash carts, out of the bay");
            if (!StuntPark.Current) { Check("the stunt park is in the build", false); yield break; }
            if (race.State != RaceController.Stage.Free) race.EndRace();
            yield return Vehicle("stuntbuggy"); StuntPark.Current.RestoreBreakables(); var acts = car.GetComponent<ArcadeActivities>();
            var a = Floor(3560f, 388f); var b = Floor(3634f, 388f); Place(a, b - a, 14f); yield return new WaitForSecondsRealtime(.2f); acts.NewSession();
            var run = new Run(); var seen = new List<string>(); var drive = StartCoroutine(DriveInput(Straight(a, b), 16f, 20f, run));
            bool shot = false; while (!run.reached && run.end == "" && run.time < 19.9f) { yield return null; var l = acts.LastResult; if (l.StartsWith("SMASH") && (seen.Count == 0 || seen[seen.Count - 1] != l)) seen.Add(l); if (!shot && Broken("StuntParkVault") > 0) { shot = true; yield return Capture("38_vault_door_burst"); } }
            yield return drive;
            report.AppendLine($"  {(run.reached ? "out of the bay" : "stopped " + run.end)}; broke the boards {Broken("StuntParkBankBoards")}, tellers {Broken("StuntParkTellers")}, the vault door {Broken("StuntParkVault")}, cash carts {Broken("StuntParkVaultCash")}; the panel said: {string.Join(" | ", seen.Distinct())}");
            Check("through the bank's vault door and out of the bay (the door bursts; a smash for the stunts)", run.reached && Broken("StuntParkVault") >= 1 && seen.Any(l => l.Contains("VAULT DOOR")));
            StuntPark.Current.RestoreBreakables();
        }

        IEnumerator R38CPics()
        {
            report.AppendLine(); report.AppendLine("=== Part C: pictures");
            if (!StuntPark.Current) yield break;
            StuntPark.Current.RestoreBreakables(); yield return Vehicle("stuntbuggy"); Place(Floor(3560f, 440f), PDir(1, 0), 0f); yield return new WaitForSecondsRealtime(1f);
            float k(int i) => 3592.3f - 13.4f * (i + .5f);
            var shots = new (string, Vector3, Vector3)[] {
                ("38_room_coffee", Store(k(0), 471f) + Vector3.up * 2.2f, Store(k(0), 486f) + Vector3.up * .8f), ("38_room_pack_and_ship", Store(k(1), 471f) + Vector3.up * 2.2f, Store(k(1), 486f) + Vector3.up * .8f),
                ("38_room_lucky_wok", Store(k(2), 471f) + Vector3.up * 2.2f, Store(k(2), 486f) + Vector3.up * .8f), ("38_room_salon", Store(k(3), 471f) + Vector3.up * 2.2f, Store(k(3), 486f) + Vector3.up * .8f),
                ("38_room_pizza_and_kicker", Store(k(4) + 5f, 484f) + Vector3.up * 2.6f, Store(k(4), 470f) + Vector3.up * .5f), ("38_room_dental", Store(k(5), 471f) + Vector3.up * 2.2f, Store(k(5), 486f) + Vector3.up * .8f),
                ("38_room_cleaners", Store(k(6), 471f) + Vector3.up * 2.2f, Store(k(6), 486f) + Vector3.up * .8f), ("38_room_taqueria_and_ramp", Store(k(7) + 5f, 471f) + Vector3.up * 2.5f, Store(k(9), 482f) + Vector3.up * 2f),
                ("38_room_florist", Store(k(8), 471f) + Vector3.up * 2.2f, Store(k(8), 478f) + Vector3.up * .5f), ("38_room_laundry", Store(k(9), 471f) + Vector3.up * 2.2f, Store(k(9), 478f) + Vector3.up * .5f),
                ("38_shop_ramp_from_the_market", Store(3449f, 477f) + Vector3.up * 2f, Store(3497f, 481.5f) + Vector3.up * 6f), ("38_shop_ramp_hatch_from_the_roof", Roof(3510f, 476f) + Vector3.up * 3f, Roof(3490f, 481.5f)),
                ("38_bank_lobby_and_vault", Floor(3594.5f, 386f) + Vector3.up * 2.2f, Floor(3606f, 388f) + Vector3.up * 1.5f), ("38_pizza_window_landing", Floor(3545f, 470f) + Vector3.up * 3f, Floor(3532f, 448f)),
            };
            foreach (var (name, at, look) in shots) yield return Still(name, at, look, 70f);
            Check("pictures of the rooms, the shop ramp and the vault written", true, "38_room_*.png, 38_shop_ramp_*.png, 38_bank_lobby_and_vault.png");
        }

        IEnumerator R38CFps()
        {
            report.AppendLine(); report.AppendLine($"=== Part C: the frame rate inside, parked ({Screen.width} x {Screen.height}, not measured at 3840 x 2160): without the new rooms (hidden: 'before') and with them");
            if (!StuntPark.Current) yield break;
            if (race.State != RaceController.Stage.Free) race.EndRace();
            int vs = QualitySettings.vSyncCount, cap = Application.targetFrameRate; QualitySettings.vSyncCount = 0; Application.targetFrameRate = -1;
            var news = new[] { "StuntParkFurniture", "StuntParkShopCounters", "StuntParkDisplays", "StuntParkShelves", "StuntParkAppliances", "StuntParkVault", "StuntParkVaultCash", "StuntParkTellers" };
            var roots = StuntPark.Current.GetComponentsInChildren<Transform>(true).Where(t => news.Contains(t.name)).Select(t => t.gameObject).ToList();
            float yawW = Mathf.Atan2(PDir(-1, 0).x, PDir(-1, 0).z) * Mathf.Rad2Deg, yawE = Mathf.Atan2(PDir(1, 0).x, PDir(1, 0).z) * Mathf.Rad2Deg;
            var spots = new List<(string, Vector3, float)> { ("the shop row, looking west along the shops", Store(3586f, 474f), yawW), ("inside the supermarket", Store(3446f, 402f), yawW), ("the bank's lobby", Floor(3594.5f, 388f), yawE) };
            foreach (bool on in new[] { false, true })
            {
                foreach (var g in roots) g.SetActive(on);
                foreach (var (label, at, yaw) in spots)
                {
                    respawn.PlaceAt(at + Vector3.up * .4f, yaw); race.NotifyTeleported(); yield return new WaitForSecondsRealtime(3f);
                    var frames = new List<float>(); float t0 = Time.realtimeSinceStartup;
                    while (Time.realtimeSinceStartup - t0 < 10f) { yield return null; frames.Add(Time.unscaledDeltaTime); }
                    var sorted = frames.OrderBy(x => x).ToList(); float p99 = sorted[Mathf.Min(sorted.Count - 1, (int)(sorted.Count * 0.99f))];
                    report.AppendLine($"INFO  {(on ? "after " : "before")} {label}: {frames.Count / frames.Sum():0} fps average, slowest 1 % {1f / p99:0} fps");
                }
            }
            QualitySettings.vSyncCount = vs; Application.targetFrameRate = cap;
        }

        IEnumerator R38Rivals()
        {
            report.AppendLine(); report.AppendLine("=== Part D: a Stunt event with 3 rivals (Normal; a 180 s round for the check, 10 minutes in play)");
            if (!StuntPark.Current) { Check("the stunt park is in the build", false); yield break; }
            if (race.State != RaceController.Stage.Free) race.EndRace();
            yield return Vehicle("stuntbuggy");
            GameSettings.Settings.nameAsked = true; GameSettings.Settings.playerName = "CHECK";   // (the check's own settings: no welcome page)
            var menus = GameMenus.Instance ? GameMenus.Instance : GameMenus.Create(); yield return new WaitForSecondsRealtime(1.5f);
            if (GameMenus.Open) menus.SendMessage("CloseMenu", SendMessageOptions.DontRequireReceiver); yield return new WaitForSecondsRealtime(.5f);
            report.AppendLine($"  the menus: {(GameMenus.Open ? "STILL OPEN: " + menus.Heading : "closed")}");
            StuntEvent.RivalCount = 3; StuntEvent.RivalDifficulty = 1; StuntEvent.TestLimit = 180f; StuntEvent done = null; System.Action<StuntEvent> fin = e => done = e; StuntEvent.Finished += fin;
            var ev = StuntEvent.Begin(car); yield return new WaitForSecondsRealtime(.6f);
            bool held = ev.Rivals.Count == 3 && ev.Rivals.All(r => ev.Holds(r.car));
            yield return Still("38_rivals_on_the_start_line", car.Body.position + PDir(-1, 0) * 14f + Vector3.up * 4f, car.Body.position, 60f);
            Check("3 rivals on stunt vehicles on the start line beside the player, held with them", held, string.Join(", ", ev.Rivals.Select(r => r.Name + " " + Nm(r.car.GetComponent<VehicleConfiguration>().profileId))));
            while (ev && ev.State == StuntEvent.Phase.Starting) yield return null;
            // the player sits on the start line; the camera follows the rivals: a picture of one mid-trick
            var cam = Camera.main; var chase = cam.GetComponent<ChaseCamera>(); bool wasChase = chase && chase.enabled; int pics = 0; float t = 0f, nextPic = 20f; float lastHud = 0f;
            while (done == null && t < 260f)
            {
                yield return null; t += Time.deltaTime;
                var flying = ev.Rivals.FirstOrDefault(r => r && r.car.GroundedWheels == 0 && Time.time < r.LineUntil && !r.LastBail);
                if (flying && t > nextPic && pics < 3)
                {
                    if (chase) chase.enabled = false; var rc = flying.car.Body.position; var back = -flying.car.Body.linearVelocity; back.y = 0; back = back.sqrMagnitude > .1f ? back.normalized : Vector3.back;
                    cam.transform.position = rc + back * 11f + Vector3.up * 4f; cam.transform.LookAt(rc); yield return Capture($"38_rival_mid_trick_{++pics}"); if (chase) { chase.enabled = wasChase; chase.Snap(); } nextPic = t + 25f;
                }
                if (t - lastHud > 60f) { lastHud = t; yield return Capture($"38_rivals_hud_{(int)t}s"); }
            }
            StuntEvent.Finished -= fin; StuntEvent.TestLimit = -1f;
            yield return new WaitForSecondsRealtime(1.5f); yield return Capture("38_rivals_results");
            if (done == null) { Check("the Stunt event with rivals ended", false); StuntEvent.End(); yield break; }
            foreach (var r in done.Standings) report.AppendLine($"  {done.NameOf(r),-8} {r.points,8:N0} pts, best combo {r.best:N0}, tricks {r.Tricks}, bails {r.Bails}" + (r.rival ? $", flights {r.rival.Flights}, smashes {r.rival.Smashes}, recoveries {r.rival.Recoveries}, outside the park {r.rival.Outside:0.0} s; lines {string.Join(" ", r.rival.LinesTaken)}" : ""));
            foreach (var r in done.Standings.Where(x => x.rival)) File.WriteAllText(Path.Combine(outputDir, $"rival_{r.name}.log"), r.rival.Log);
            var rv = done.Standings.Where(x => x.rival).ToList();
            Check("every rival scored", rv.Count == 3 && rv.All(x => x.points > 0), string.Join(", ", rv.Select(x => $"{x.name} {x.points:N0}")));
            Check("no rival left the park (never 2 s outside it)", rv.All(x => x.rival.Outside < 2f), string.Join(", ", rv.Select(x => $"{x.name} {x.rival.Outside:0.0} s")));
            Check("no rival stuck on a wall (each back on its line within seconds, at most 4 recoveries in the round)", rv.All(x => x.rival.Recoveries <= 4), string.Join(", ", rv.Select(x => $"{x.name} {x.rival.Recoveries}")));
            Check("the results list everyone by points; the rivals are not on the Top 10", done.Standings.Count == 4 && rv.All(x => x.entry == null));
            StuntEvent.End(); StuntEvent.RivalCount = 0;
            if (GameMenus.Open) GameMenus.Instance.SendMessage("CloseMenu", SendMessageOptions.DontRequireReceiver);
        }

        IEnumerator R38SplitAi()
        {
            report.AppendLine(); report.AppendLine("=== Part D: split screen's AI player 2 in the stunt park (BUG-001: it cruised off the course into a wall)");
            if (!StuntPark.Current) { Check("the stunt park is in the build", false); yield break; }
            if (race.State != RaceController.Stage.Free) race.EndRace();
            yield return Vehicle("stuntbuggy");
            var s = BeginSplit(SplitScreen.Kind.StuntPark, true); s.P2.GetComponent<VehicleSwitch>()?.Select("stuntbike");
            car.GetComponent<VehicleRespawn>().PlaceAt(StuntPark.StartPos, StuntPark.Current.startYaw); SplitSession.PlaceBeside(s.P2, car.transform, 4.5f);
            s.StuntRide(); yield return new WaitForSecondsRealtime(.5f);
            var acts2 = s.Activities2; acts2.NewSession(); float t = 0f, outside = 0f, moved = 0f; var last = s.P2.Body.position; bool shot = false;
            while (t < 90f) { yield return null; t += Time.deltaTime; moved += Vector3.Distance(last, s.P2.Body.position); last = s.P2.Body.position; if (!StuntPark.Inside(last, 2f)) outside += Time.deltaTime; if (!shot && t > 40f) { shot = true; yield return Capture("38_split_ai_player2_in_the_park"); } }
            var rider = s.Rider2;
            report.AppendLine($"  player 2 (AI) on the {Nm(s.P2.GetComponent<VehicleConfiguration>().profileId)}: {moved:0} m in 90 s, {outside:0.0} s outside the park, recoveries {(rider ? rider.Recoveries : -1)}, lines {(rider ? string.Join(" ", rider.LinesTaken) : "-")}; its own scoring: {acts2.Tricks} tricks, best combo {acts2.BestCombo:N0}");
            Check("the AI player 2 rides the park's lines in the stunt park (never out of the park, never stuck), its tricks scored as a player's", rider && moved > 400f && outside < 2f && rider.Recoveries <= 3 && (acts2.Tricks > 0 || acts2.BestCombo > 0));
            SplitScreen.End(); yield return new WaitForSecondsRealtime(.3f); yield return Vehicle("original");
        }
    }
}
