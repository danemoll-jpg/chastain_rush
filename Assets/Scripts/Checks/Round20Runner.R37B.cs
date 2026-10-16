using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Racer;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace ChastainRush
{
    // Round 37 Part B in the exe: ChastainRush.exe -exeRound37 <dir> -r37Only splitmenu,splitrace,splitai,splitroam,splitpark,splitpics,splitfps
    // [-traffic Normal] -logFile <dir>\Player.log. Two virtual controllers (Input System devices added by the check), each driving its own
    // player's vehicle through that player's own VehicleInput (bound to that device only), as two people would:
    //   splitmenu   the title menu with the controllers: player 1's pad opens SPLIT SCREEN, player 2's pad presses A and joins, player 1 picks
    //               Free Roam and START; split screen runs with two humans
    //   splitrace   a 1-lap race for two on the Chastain Glen loop, no AI rivals: both pads drive their own vehicle round; both finish, both on the
    //               finishing order; player 2's laps on the Top 10 under their name (the check's own boards)
    //   splitai     the same race with player 2 as the AI: it finishes; then Free Roam with the AI cruising the roads
    //   splitroam   Free Roam for two: both drive; the other player's direction and distance on each half; resets are each player's own
    //   splitpark   the stunt park for two: each player off BIG AIR with a backflip, each scored on their own trick panel
    //   splitpics   both layouts (top / bottom, left / right) in a race and in Free Roam
    //   splitfps    the frame rate split screen in Free Roam at Canton Road by the shops and in a Chastain Ridge race (with -traffic Normal)
    public sealed partial class Round20Runner
    {
        Gamepad pad1, pad2;
        void Pads()
        {
            // a check runs minimized and never takes the focus: its virtual controllers must count without it (the game Dan launches is unchanged)
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            if (pad1 == null || !pad1.added) pad1 = InputSystem.AddDevice<Gamepad>("Round37 pad 1");
            if (pad2 == null || !pad2.added) pad2 = InputSystem.AddDevice<Gamepad>("Round37 pad 2");
        }
        static void Set(Gamepad pad, float steer, float throttle, float brake, float pitch = 0f, params GamepadButton[] buttons)
        {
            var st = new GamepadState { leftStick = new Vector2(steer, pitch), rightTrigger = throttle, leftTrigger = brake };
            foreach (var b in buttons) st = st.WithButton(b);
            InputSystem.QueueStateEvent(pad, st);
        }
        IEnumerator PadPress(Gamepad pad, GamepadButton b)
        {
            Set(pad, 0, 0, 0, 0, b); yield return null; yield return null;
            Set(pad, 0, 0, 0); yield return new WaitForSecondsRealtime(0.3f);
        }
        IEnumerator PadStick(Gamepad pad, Vector2 left)
        {
            InputSystem.QueueStateEvent(pad, new GamepadState { leftStick = left }); yield return null; yield return null;
            InputSystem.QueueStateEvent(pad, new GamepadState()); yield return new WaitForSecondsRealtime(0.3f);
        }

        // a pad's driver along a line (the race route's lap, from where the vehicle is): steering and pedals as a player would give them
        sealed class PadDriver { public Gamepad pad; public ArcadeVehicle car; public float cap = 20f; public float pitch; public bool done; }
        void Drive(PadDriver d, RaceRoute route, float ahead = 10f)
        {
            if (d.car == null || d.done) { Set(d.pad, 0, 0, 0); return; }
            var p = d.car.Body.position; float s = route.LapStationOf(p);
            var aim = route.PointAt(route.GateS + s + ahead + .3f * Mathf.Max(0f, d.car.ForwardSpeed), out _);
            var to = aim - p; to.y = 0; var fwd = d.car.transform.forward; fwd.y = 0;
            float ang = Vector3.SignedAngle(fwd, to, Vector3.up), v = d.car.ForwardSpeed;
            // slower into the bends ahead (the lap's heading change over the next 40 m)
            route.PointAt(route.GateS + s + 5f, out var f0); route.PointAt(route.GateS + s + 40f, out var f1);
            float bend = Vector3.Angle(new Vector3(f0.x, 0, f0.z), new Vector3(f1.x, 0, f1.z)); float cap = Mathf.Lerp(d.cap, 9f, Mathf.Clamp01(bend / 80f));
            Set(d.pad, Mathf.Clamp(ang / 25f, -1f, 1f), v < cap ? 1f : 0f, v > cap + 2f ? 1f : 0f, d.pitch);
        }

        SplitSession BeginSplit(SplitScreen.Kind mode, bool p2Ai)
        {
            Pads(); SplitScreen.End();
            SplitScreen.P1Device = pad1; SplitScreen.P2Device = p2Ai ? null : pad2; SplitScreen.P2Ai = p2Ai; SplitScreen.P2Vehicle = "scrambler"; SplitScreen.P2Colour = 1;
            GameSettings.Settings.player2Name = "KYLE";
            return SplitScreen.Begin(mode);
        }

        IEnumerator R37SplitMenu()
        {
            report.AppendLine(); report.AppendLine("=== Part B6: the title menu with two controllers (player 1's pad opens SPLIT SCREEN, player 2's pad joins)");
            Pads(); SplitScreen.End(); SplitScreen.P1Device = null; SplitScreen.P2Device = null; SplitScreen.P2Ai = true;
            var menus = GameMenus.Create(); yield return new WaitForSecondsRealtime(1.5f);
            if (!menus) { Check("the menus", false); yield break; }
            // a check's own settings: the first start's welcome asks for a name (the on-screen keyboard); B leaves it
            if (menus.PageName.Contains("keyboard")) { yield return PadPress(pad1, GamepadButton.East); yield return new WaitForSecondsRealtime(.3f); }
            // the title menu: player 1's pad moves down to SPLIT SCREEN and presses A (MenuInput takes the pad that last pressed)
            yield return PadPress(pad1, GamepadButton.DpadDown);
            int guard = 0; while (!menus.FocusedLabel.StartsWith("SPLIT SCREEN") && guard++ < 12) yield return PadPress(pad1, GamepadButton.DpadDown);
            yield return PadPress(pad1, GamepadButton.South);
            yield return new WaitForSecondsRealtime(.4f);
            bool onSetup = menus.Heading == "SPLIT SCREEN"; string p1 = SplitScreen.DeviceName(SplitScreen.P1Device);
            yield return Capture("37b_menu_setup");
            // player 2's pad presses A: it joins (and does not click player 1's row)
            yield return PadPress(pad2, GamepadButton.South); yield return new WaitForSecondsRealtime(.3f);
            bool joined = !SplitScreen.P2Ai && SplitScreen.P2Device == pad2 && menus.Heading == "SPLIT SCREEN";
            // player 1: Mode to Free Roam (down to the Mode row, right), then down to START and A
            guard = 0; while (!menus.FocusedLabel.StartsWith("Mode") && guard++ < 20) yield return PadPress(pad1, GamepadButton.DpadDown);
            while (SplitScreen.Mode != SplitScreen.Kind.FreeRoam && guard++ < 30) yield return PadStick(pad1, new Vector2(1, 0));
            yield return Capture("37b_menu_setup_joined");
            guard = 0; while (!menus.FocusedLabel.StartsWith("START") && guard++ < 30) yield return PadPress(pad1, GamepadButton.DpadDown);
            yield return PadPress(pad1, GamepadButton.South); yield return new WaitForSecondsRealtime(1f);
            bool running = SplitScreen.Active && SplitScreen.Mode == SplitScreen.Kind.FreeRoam && SplitScreen.P2Car && !GameMenus.Open;
            yield return Capture("37b_menu_started");
            Check("B6: SPLIT SCREEN from the title menu on player 1's controller, player 2 joins with A on theirs, Free Roam started with two humans",
                onSetup && joined && running, $"setup page {onSetup} (player 1: {p1}), player 2 joined {joined} ({SplitScreen.DeviceName(SplitScreen.P2Device)}), running {running}");
            // B goes back: the pause menu (Start), then B resumes
            yield return PadPress(pad2, GamepadButton.Start); yield return new WaitForSecondsRealtime(.3f);
            bool paused = GameMenus.Open && menus.Heading.Contains("SPLIT SCREEN");
            yield return Capture("37b_menu_pause");
            yield return PadPress(pad2, GamepadButton.East); yield return new WaitForSecondsRealtime(.3f);
            Check("B6: either player's Start pauses split screen (its own pause menu), B resumes", paused && !GameMenus.Open, $"paused {paused}, resumed {!GameMenus.Open}");
            SplitScreen.End(); if (GameMenus.Instance) Destroy(GameMenus.Instance.gameObject);
            Time.timeScale = 1f;   // a menu left open pauses the game: the parts after this one drive
            yield return new WaitForSecondsRealtime(.5f);
        }

        IEnumerator R37SplitRace(bool ai)
        {
            report.AppendLine(); report.AppendLine(ai ? "=== Part B1 / B2: a 1-lap race with player 2 as the AI (player 1 on a controller)" : "=== Part B2: a 1-lap race for two, each on their own controller (Chastain Glen loop, no rivals)");
            if (race.State != RaceController.Stage.Free) race.EndRace();
            RaceController.RouteOverride = "chastain_glen_loop.json"; RaceController.DirectionOverride = "forward"; RaceController.LapsOverride = 1; RaceController.AiCount = 0;
            race.Reload();
            var s = BeginSplit(SplitScreen.Kind.Race, ai); yield return new WaitForSecondsRealtime(.5f);
            race.StartRace(); yield return null;
            var e2 = race.HumanEntrant(2);
            report.AppendLine($"  grid: player 1 {Nm(car.GetComponent<VehicleConfiguration>().profileId)}, player 2 {(e2 != null ? e2.Name + " " + e2.VehicleName : "MISSING")}");
            yield return new WaitForSecondsRealtime(1.5f); yield return Capture(ai ? "37b_race_ai_countdown" : "37b_race_countdown");
            float c0 = Time.realtimeSinceStartup; while (race.State == RaceController.Stage.Countdown && Time.realtimeSinceStartup - c0 < 10f) yield return null;
            var d1 = new PadDriver { pad = pad1, car = s.P1 }; var d2 = new PadDriver { pad = pad2, car = s.P2, cap = 18f };
            float t = 0f; bool shot = false; float w0 = Time.realtimeSinceStartup;
            while (t < 240f && Time.realtimeSinceStartup - w0 < 300f && !(race.HumanFinished(1) && (ai ? e2 != null && e2.Finished : race.HumanFinished(2))))
            {
                d1.done = race.HumanFinished(1); d2.done = race.HumanFinished(2) || ai;
                Drive(d1, race.Route); if (!ai) Drive(d2, race.Route);
                yield return null; t += Time.deltaTime;
                if (!shot && t > 25f) { shot = true; yield return Capture(ai ? "37b_race_ai_running" : "37b_race_running"); }
            }
            Set(pad1, 0, 0, 0); Set(pad2, 0, 0, 0);
            yield return new WaitForSecondsRealtime(1.5f); yield return Capture(ai ? "37b_race_ai_results" : "37b_race_results");
            var p1 = race.HumanEntrant(1); e2 = race.HumanEntrant(2);
            report.AppendLine($"  {SplitScreen.NameOf(1)}: {(p1.Finished ? RaceController.Fmt(p1.FinalTotal) : "not finished")}, {SplitScreen.NameOf(2)}: {(e2 != null && e2.Finished ? RaceController.Fmt(e2.FinalTotal) + (e2.Estimated ? " (estimated)" : "") : "not finished")}, in {t:0} s; order {string.Join(", ", race.Ordered().Select(x => x.Name))}");
            if (!ai) report.AppendLine($"  Top 10: player 1 race #{race.RaceRank}, player 2 race #{e2?.RaceRank}; boards {RaceController.Boards != null} in the check's own folder");
            if (ai) Check("B1: player 2 as the AI races the lap in its own half and finishes", p1.Finished && e2 != null && e2.Finished && e2.Driver != null, $"{t:0} s");
            else Check("B2: a 1-lap race for two, each player on their own controller and half: both finish, both on the finishing order and the Top 10 under their names", p1.Finished && e2 != null && e2.Finished && race.Ordered().Count == 2 && e2.RaceRank > 0, $"{t:0} s");
            race.EndRace(); SplitScreen.End(); RaceController.LapsOverride = 0;
            yield return new WaitForSecondsRealtime(.5f);
            if (ai)
            {
                // Free Roam with the AI cruising
                car.GetComponent<VehicleRespawn>().PlaceAt(RaceRoute.Surface(3502.8f, 1166.1f) + Vector3.up * .3f, -13f); yield return new WaitForSecondsRealtime(.3f);   // Canton Road by the shops
                var r = BeginSplit(SplitScreen.Kind.FreeRoam, true);
                yield return new WaitForSecondsRealtime(.3f); SplitSession.PlaceBeside(r.P2, r.P1.transform, 4.5f); r.Cruise();
                var a0 = r.P2.Body.position; yield return new WaitForSecondsRealtime(15f);
                float moved = Vector3.Distance(a0, r.P2.Body.position); yield return Capture("37b_roam_ai");
                Check("B1: in Free Roam player 2 as the AI cruises the roads (the traffic driver)", r.Cruiser && moved > 60f, $"{moved:0} m in 15 s");
                SplitScreen.End();
            }
        }

        IEnumerator R37SplitRoam()
        {
            report.AppendLine(); report.AppendLine("=== Part B2 / B3: Free Roam for two, each on their own controller");
            if (race.State != RaceController.Stage.Free) race.EndRace();
            car.GetComponent<VehicleRespawn>().PlaceAt(RaceRoute.Surface(3108.8f, 1065.0f) + Vector3.up * .3f, 0f); yield return new WaitForSecondsRealtime(.3f);
            var s = BeginSplit(SplitScreen.Kind.FreeRoam, false); yield return new WaitForSecondsRealtime(.5f);
            var route = race.Route; var a1 = s.P1.Body.position; var a2 = s.P2.Body.position;
            // player 1 drives the lap; player 2's pad is left alone: each pad moves its own vehicle only
            var d1 = new PadDriver { pad = pad1, car = s.P1, cap = 15f }; float t = 0f;
            while (t < 8f) { Drive(d1, route); Set(pad2, 0, 0, 0); yield return null; t += Time.deltaTime; }
            float m1 = Vector3.Distance(a1, s.P1.Body.position), m2 = Vector3.Distance(a2, s.P2.Body.position);
            Check("B1: each controller drives only its own player's vehicle (pad 1 drives, pad 2 left alone)", m1 > 40f && m2 < 6f, $"player 1 moved {m1:0} m, player 2 {m2:0.0} m");
            // now both drive
            var d2 = new PadDriver { pad = pad2, car = s.P2, cap = 15f }; t = 0f; a2 = s.P2.Body.position;
            while (t < 10f) { Drive(d1, route); Drive(d2, route); yield return null; t += Time.deltaTime; }
            Set(pad1, 0, 0, 0); Set(pad2, 0, 0, 0);
            m2 = Vector3.Distance(a2, s.P2.Body.position);
            yield return Capture("37b_roam_two");
            // a reset on player 2's controller moves player 2 only (Y)
            float c1 = Time.realtimeSinceStartup; while (s.P1.Body.linearVelocity.magnitude > 1f && Time.realtimeSinceStartup - c1 < 6f) { Set(pad1, 0, 0, s.P1.ForwardSpeed > .7f ? 1 : 0); yield return null; }   // player 1 stops first
            Set(pad1, 0, 0, 0); yield return new WaitForSecondsRealtime(.3f);
            var b1 = s.P1.Body.position; var p2r = s.P2.GetComponent<VehicleRespawn>(); bool reset = false; System.Action h = () => reset = true; p2r.Respawned += h;
            yield return PadPress(pad2, GamepadButton.North); yield return new WaitForSecondsRealtime(.5f); p2r.Respawned -= h;
            bool p1Still = Vector3.Distance(b1, s.P1.Body.position) < 3f;
            Check("B3: each player's own reset (Y on player 2's controller resets player 2 only)", reset && p1Still, $"player 2 reset {reset}, player 1 stayed {p1Still}");
            // each player's own camera view (X on player 2's pad changes player 2's view only)
            var v1 = s.Cam1.GetComponent<CameraViews>(); var v2 = s.Views2; var was1 = v1 ? v1.Current : default; var was2 = v2 ? v2.Current : default;
            yield return PadPress(pad2, GamepadButton.West); yield return new WaitForSecondsRealtime(.4f);
            bool own = v2 && v2.Current != was2 && v1 && v1.Current == was1; string after = v2 ? v2.Current.ToString() : "-";
            yield return Capture("37b_roam_p2_view");
            yield return PadPress(pad2, GamepadButton.West); yield return PadPress(pad2, GamepadButton.West); yield return PadPress(pad2, GamepadButton.West);
            Check("B3: each player's own camera view (X on player 2's controller changes player 2's view only)", own, $"player 2 {was2} -> {after} (then back), player 1 kept {(v1 ? v1.Current.ToString() : "-")}");
            Check("B2: Free Roam for two: both drive their own vehicle", m2 > 40f, $"player 2 moved {m2:0} m in 10 s");
            SplitScreen.End(); yield return new WaitForSecondsRealtime(.3f);
        }

        IEnumerator R37SplitPark()
        {
            report.AppendLine(); report.AppendLine("=== Part B2: the stunt park for two: each player off BIG AIR with a backflip, scored on their own trick panel");
            if (!StuntPark.Current) { Check("the stunt park is in the build", false); yield break; }
            if (race.State != RaceController.Stage.Free) race.EndRace();
            yield return Vehicle("stuntbuggy");
            var s = BeginSplit(SplitScreen.Kind.StuntPark, false); SplitScreen.P2Vehicle = "stuntbike"; s.P2.GetComponent<VehicleSwitch>().Select("stuntbike");
            yield return new WaitForSecondsRealtime(.5f);
            var results = new List<string>(); bool both = true;
            foreach (int player in new[] { 1, 2 })
            {
                var me = player == 1 ? s.P1 : s.P2; var other = player == 1 ? s.P2 : s.P1; var pad = player == 1 ? pad1 : pad2;
                var acts = player == 1 ? ArcadeActivities.Instance : s.Activities2;
                other.GetComponent<VehicleRespawn>().PlaceAt(Floor(3600f, 470f) + Vector3.up * .3f, 0f);   // out of the way
                var a = Roof(3398f, 410f); var b = Roof(3470f, 410f); me.GetComponent<VehicleRespawn>().PlaceAt(a + Vector3.up * .3f, Mathf.Atan2(PDir(1, 0).x, PDir(1, 0).z) * Mathf.Rad2Deg);
                yield return new WaitForSecondsRealtime(1.2f); acts.NewSession(); string before = acts.LastStunt;
                float t = 0f, air = 0f, turned = 0f; bool shot = false;
                while (t < 14f && !(acts.LastStunt != before && !acts.StuntBusy))
                {
                    var p = me.Body.position; var dir = PDir(1, 0); var to = b - p; to.y = 0; float ang = Vector3.SignedAngle(new Vector3(me.transform.forward.x, 0, me.transform.forward.z), to, Vector3.up);
                    air = me.GroundedWheels == 0 ? air + Time.deltaTime : 0f;
                    float pitch = 0f;
                    if (air > .25f && turned < 330f) { turned += Mathf.Abs(me.transform.InverseTransformDirection(me.Body.angularVelocity).x * Time.deltaTime * Mathf.Rad2Deg); pitch = -Mathf.Clamp((360f - turned) / 90f, .25f, 1f); }
                    Set(pad, air > 0f ? 0f : Mathf.Clamp(ang / 25f, -1, 1), 1f, 0f, pitch);
                    if (!shot && air > 1.0f) { shot = true; yield return Capture($"37b_park_p{player}_trick"); }
                    yield return null; t += Time.deltaTime;
                }
                Set(pad, 0, 0, 0);
                yield return new WaitForSecondsRealtime(ArcadeActivities.ComboGap + .5f);
                string got = acts.LastStunt.Replace("\n", " / ");
                bool ok = got.Contains("FLIP") && acts.Player == player;
                both &= ok; results.Add($"player {player} ({SplitScreen.NameOf(player)}, {Nm(me.GetComponent<VehicleConfiguration>().profileId)}): '{got}' {acts.LastStuntPoints:N0} pts");
            }
            report.AppendLine("  " + string.Join("\n  ", results));
            Check("B2: the stunt park for two: a trick each, each scored on their own (Player 1's and player 2's ArcadeActivities)", both);
            SplitScreen.End(); yield return Vehicle("original");
        }

        // the Stunt event for two (a 30 s round, checks only): both held on the start line, both drive, each their own points, both on its Top 10
        IEnumerator R37SplitEvent()
        {
            report.AppendLine(); report.AppendLine("=== Part B2 / B4: the Stunt event for two (a 30 s round for the check; 10 minutes in play)");
            if (!StuntPark.Current) { Check("the stunt park is in the build", false); yield break; }
            if (race.State != RaceController.Stage.Free) race.EndRace();
            yield return Vehicle("stuntbuggy");
            var s = BeginSplit(SplitScreen.Kind.Stunt, false); s.P2.GetComponent<VehicleSwitch>().Select("stuntbike");
            StuntEvent.TestLimit = 30f; StuntEvent done = null; System.Action<StuntEvent> fin = e => done = e; StuntEvent.Finished += fin;
            var ev = StuntEvent.Begin(s.P1, s.P2, s.Activities2); yield return new WaitForSecondsRealtime(.6f);
            bool held = ev.Holds(s.P1) && ev.Holds(s.P2) && ev.Riders.Count == 2;
            yield return Capture("37b_event_start");
            float w0 = Time.realtimeSinceStartup; while (ev.State == StuntEvent.Phase.Starting && Time.realtimeSinceStartup - w0 < 10f) yield return null;
            // both off the start line west over the park's floor: whatever they jump counts for each of them
            var d1 = new PadDriver { pad = pad1, car = s.P1, cap = 22f }; var d2 = new PadDriver { pad = pad2, car = s.P2, cap = 22f };
            w0 = Time.realtimeSinceStartup;
            while (done == null && Time.realtimeSinceStartup - w0 < 70f)
            {
                foreach (var d in new[] { d1, d2 })
                {
                    var p = d.car.Body.position; var aim = Floor(3470f, d == d1 ? 410f : 400f); var to = aim - p; to.y = 0; float ang = Vector3.SignedAngle(new Vector3(d.car.transform.forward.x, 0, d.car.transform.forward.z), to, Vector3.up);
                    Set(d.pad, Mathf.Clamp(ang / 25f, -1, 1), d.car.ForwardSpeed < d.cap && to.magnitude > 25f ? 1f : 0f, to.magnitude < 25f ? 1f : 0f);
                }
                if (Time.realtimeSinceStartup - w0 > 8f && Time.realtimeSinceStartup - w0 < 8.1f) yield return Capture("37b_event_running");
                yield return null;
            }
            Set(pad1, 0, 0, 0); Set(pad2, 0, 0, 0); StuntEvent.Finished -= fin; StuntEvent.TestLimit = -1f;
            if (done == null) { Check("B2: the Stunt event for two ended", false); StuntEvent.End(); SplitScreen.End(); yield break; }
            var r1 = done.Of(1); var r2 = done.Of(2);
            report.AppendLine($"  {SplitScreen.NameOf(1)}: {r1.points:N0} pts, Top 10 #{r1.place}; {SplitScreen.NameOf(2)}: {r2.points:N0} pts, Top 10 #{r2.place}; winner {(done.Winner == 0 ? "a draw" : SplitScreen.NameOf(done.Winner))}");
            Check("B2 / B4: the Stunt event for two: both held 3 s on the line, each their own points, both humans on the Stunt event Top 10 under their names",
                held && r1 != null && r2 != null && r1.entry != null && r2.entry != null && r1.entry.name == SplitScreen.NameOf(1) && r2.entry.name == SplitScreen.NameOf(2), $"held {held}");
            StuntEvent.End(); SplitScreen.End(); yield return Vehicle("original");
        }

        IEnumerator R37SplitPics()
        {
            report.AppendLine(); report.AppendLine("=== Part B1 / B3: pictures of both layouts (37b_layout_*.png)");
            if (race.State != RaceController.Stage.Free) race.EndRace();
            bool lr0 = GameSettings.Settings.splitLeftRight;
            car.GetComponent<VehicleRespawn>().PlaceAt(RaceRoute.Surface(3502.8f, 1166.1f) + Vector3.up * .3f, -13f); yield return new WaitForSecondsRealtime(.4f);
            foreach (bool lr in new[] { false, true })
            {
                GameSettings.Settings.splitLeftRight = lr;
                var s = BeginSplit(SplitScreen.Kind.FreeRoam, false); yield return new WaitForSecondsRealtime(2f);
                yield return Capture($"37b_layout_{(lr ? "left_right" : "top_bottom")}_roam");
                SplitScreen.End(); yield return new WaitForSecondsRealtime(.3f);
                RaceController.RouteOverride = "chastain_glen_loop.json"; RaceController.DirectionOverride = "forward"; RaceController.LapsOverride = 1; RaceController.AiCount = 3; race.Reload();
                BeginSplit(SplitScreen.Kind.Race, false); race.StartRace();
                while (race.State == RaceController.Stage.Countdown) yield return null;
                yield return new WaitForSecondsRealtime(1.2f);
                yield return Capture($"37b_layout_{(lr ? "left_right" : "top_bottom")}_race");
                race.EndRace(); SplitScreen.End(); yield return new WaitForSecondsRealtime(.3f);
            }
            GameSettings.Settings.splitLeftRight = lr0; RaceController.LapsOverride = 0; RaceController.AiCount = 0;
            Check("B1: pictures of both layouts, Free Roam and a race (37b_layout_*.png)", true);
        }

        IEnumerator R37SplitFps()
        {
            report.AppendLine(); report.AppendLine($"=== Part B7: the frame rate split screen ({Screen.width} x {Screen.height}, not measured at 3840 x 2160; traffic {(Traffic.Instance ? Traffic.Instance.Level : 0)})");
            if (race.State != RaceController.Stage.Free) race.EndRace();
            int vs = QualitySettings.vSyncCount, cap = Application.targetFrameRate; QualitySettings.vSyncCount = 0; Application.targetFrameRate = -1;
            IEnumerator Measure(string label)
            {
                yield return new WaitForSecondsRealtime(3f);
                var frames = new List<float>(); float t0 = Time.realtimeSinceStartup;
                while (Time.realtimeSinceStartup - t0 < 10f) { yield return null; frames.Add(Time.unscaledDeltaTime); }
                var sorted = frames.OrderBy(x => x).ToList(); float p99 = sorted[Mathf.Min(sorted.Count - 1, (int)(sorted.Count * 0.99f))];
                // where the time goes (FrameTimingManager: the main thread, the render thread, the GPU; 0 = not measured on this machine)
                FrameTimingManager.CaptureFrameTimings(); var ft = new FrameTiming[1]; uint got = FrameTimingManager.GetLatestTimings(1, ft);
                string where = got > 0 ? $"; main thread {ft[0].cpuMainThreadFrameTime:0.0} ms, render thread {ft[0].cpuRenderThreadFrameTime:0.0} ms, GPU {ft[0].gpuFrameTime:0.0} ms" : "";
                report.AppendLine($"INFO  {label}: {frames.Count / frames.Sum():0} fps average, slowest 1 % {1f / p99:0} fps{where}; {Traffic.Instance?.Cars.Count ?? 0} traffic cars");
            }
            car.GetComponent<VehicleRespawn>().PlaceAt(RaceRoute.Surface(3502.8f, 1166.1f) + Vector3.up * .3f, -13f); yield return new WaitForSecondsRealtime(.3f);
            yield return Measure("one player, Canton Road by the shops");
            BeginSplit(SplitScreen.Kind.FreeRoam, false); yield return Measure("split screen Free Roam, Canton Road by the shops");
            SplitScreen.End();
            RaceController.RouteOverride = "chastain_ridge.json"; RaceController.DirectionOverride = "forward"; RaceController.LapsOverride = 1; RaceController.AiCount = 3; race.Reload();
            {   // the same race for one player, for comparison
                Pads(); SplitScreen.End(); car.GetComponent<VehicleInput>().Bind(pad1); race.StartRace();
                float c0 = Time.realtimeSinceStartup; while (race.State == RaceController.Stage.Countdown && Time.realtimeSinceStartup - c0 < 10f) yield return null;
                var d0 = new PadDriver { pad = pad1, car = car, cap = 18f }; var dr0 = StartCoroutine(DriveBoth(d0, d0, 20f));
                yield return Measure("one player, a Chastain Ridge race (3 AI rivals" + (Traffic.Instance && Traffic.Instance.Level > 0 ? ", traffic" : "") + ")");
                StopCoroutine(dr0); Set(pad1, 0, 0, 0); race.EndRace(); car.GetComponent<VehicleInput>().Bind(null); yield return new WaitForSecondsRealtime(.5f);
            }
            var s = BeginSplit(SplitScreen.Kind.Race, false); race.StartRace();
            while (race.State == RaceController.Stage.Countdown) yield return null;
            var d1 = new PadDriver { pad = pad1, car = s.P1, cap = 18f }; var d2 = new PadDriver { pad = pad2, car = s.P2, cap = 18f };
            var drive = StartCoroutine(DriveBoth(d1, d2, 20f));
            yield return Measure("split screen, a Chastain Ridge race (3 AI rivals" + (Traffic.Instance && Traffic.Instance.Level > 0 ? ", traffic" : "") + ")");
            StopCoroutine(drive); Set(pad1, 0, 0, 0); Set(pad2, 0, 0, 0);
            yield return Capture("37b_fps_ridge_race");
            race.EndRace(); SplitScreen.End(); RaceController.LapsOverride = 0; RaceController.AiCount = 0;
            QualitySettings.vSyncCount = vs; Application.targetFrameRate = cap;
        }
        IEnumerator DriveBoth(PadDriver d1, PadDriver d2, float seconds)
        {
            float t = 0f; while (t < seconds) { Drive(d1, race.Route); Drive(d2, race.Route); yield return null; t += Time.deltaTime; }
        }
    }
}
