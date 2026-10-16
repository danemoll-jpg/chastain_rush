using System.Collections;
using System.IO;
using System.Linq;
using Racer;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace ChastainRush
{
    // Round 34 Part C in the exe: ChastainRush.exe -exeRound34 <dir> -r34Only records -logFile <dir>\Player.log. Everything in the
    // check's own folder (its race_records_check.json, top-ten-v1.json and settings.json; Dan's are never read or written). The
    // menus as Dan has them (GameMenus.Create, as the Round 16 check), a virtual gamepad:
    //   a race_records_check.json like Dan's (two Chastain Glen loop bests, no name) is carried onto the boards unnamed;
    //   the welcome asks the name: typed with the pad on the on-screen keyboard (Shift, D, Shift, a, n), Start saves it; the
    //   carried entries take it;
    //   a 1-lap Chastain Glen loop race driven by script: its lap and total go on the boards at the right place, the finish
    //   panel shows the places;
    //   RECORDS from the race-over menu: every route and direction, both boards, left / right; B back.
    // Pictures: 34_name_entry, 34_name_typed, 34_finish_panel, 34_board_laps, 34_board_races, 34_board_<n>.
    public sealed partial class Round20Runner
    {
        Gamepad r34pad;
        IEnumerator PadPress(GamepadButton b)
        {
            InputSystem.QueueStateEvent(r34pad, new GamepadState().WithButton(b));
            yield return null; yield return null;
            InputSystem.QueueStateEvent(r34pad, new GamepadState());
            yield return new WaitForSecondsRealtime(0.3f);
        }
        IEnumerator PadPresses(GamepadButton b, int n) { for (int i = 0; i < n; i++) yield return PadPress(b); }

        IEnumerator R34Records()
        {
            report.AppendLine(); report.AppendLine("=== Part C: Top 10 boards, the player's name, the Records page (the check's own files)");
            // Dan-like bests to carry (C4), in this check's own records file
            string bests = RaceController.RecordsPath();
            File.WriteAllText(bests, "{\n    \"entries\": [\n        { \"key\": \"Chastain Glen loop|forward|3\", \"best_lap\": 42.364479064941406, \"best_total\": 143.61282348632813 },\n" +
                                     "        { \"key\": \"Chastain Glen loop|forward|3|drifter\", \"best_lap\": 44.03461837768555, \"best_total\": 138.6710205078125 }\n    ]\n}\n");
            var boards = RaceController.Boards;
            var glen = RecordBoards.Category("Chastain Glen loop", "forward", 3);
            Check("C4: the bests are carried onto the boards, unnamed, nothing written yet (loading never writes)",
                  boards.Board(glen, false).Count == 2 && boards.Board(glen, true).Count == 2 && boards.Board(glen, false).All(e => e.legacy && string.IsNullOrEmpty(e.name)) && !File.Exists(boards.FilePath),
                  $"lap board {boards.Board(glen, false).Count}, race board {boards.Board(glen, true).Count}, file {(File.Exists(boards.FilePath) ? "WRITTEN" : "not written")}");

            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            r34pad = InputSystem.AddDevice<Gamepad>("Round34 virtual pad");
            var menus = GameMenus.Create();
            yield return new WaitForSecondsRealtime(2f);
            Check("C2: the welcome asks the name on the on-screen keyboard", menus.PageName.EndsWith("/keyboard") && menus.Heading.Contains("NAME"), $"{menus.PageName}: {menus.Heading}");
            yield return Capture("34_name_entry");
            // to the top-left key from wherever the focus is, then Shift (below the bottom row's first key)
            yield return PadPresses(GamepadButton.DpadUp, 6); yield return PadPresses(GamepadButton.DpadLeft, 10);
            yield return PadPresses(GamepadButton.DpadDown, 5);   // 1 > q > a > z > \ > Shift
            yield return PadPress(GamepadButton.South);           // Shift on
            yield return PadPresses(GamepadButton.DpadUp, 3);     // \ > z > a
            yield return PadPresses(GamepadButton.DpadRight, 2);  // s > d
            yield return PadPress(GamepadButton.South);           // D
            yield return PadPresses(GamepadButton.DpadDown, 3);   // c > - > Shift
            yield return PadPress(GamepadButton.South);           // Shift off
            yield return PadPresses(GamepadButton.DpadUp, 3);     // \ > z > a
            yield return PadPress(GamepadButton.South);           // a
            yield return PadPress(GamepadButton.DpadDown);        // z
            yield return PadPresses(GamepadButton.DpadRight, 5);  // x c v b n
            yield return PadPress(GamepadButton.South);           // n
            string typed = menus.KeyboardDraft;
            yield return Capture("34_name_typed");
            yield return PadPress(GamepadButton.Start);           // save
            yield return new WaitForSecondsRealtime(0.5f);
            Check("C2: the name typed with the controller and saved (Start)", typed == "Dan" && PlayerNames.Player == "Dan" && menus.PageName == "title/root",
                  $"typed \"{typed}\", player name \"{PlayerNames.Player}\", page {menus.PageName}, settings {GameSettings.FilePath}");
            Check("C4: the carried entries take the name, saved to the check's own boards file", boards.Board(glen, false).All(e => e.name == "Dan") && File.Exists(boards.FilePath),
                  string.Join(", ", boards.Board(glen, false).Select(e => $"{e.name} {e.seconds:0.000} {e.vehicle}")));

            // FREE ROAM, then a 1-lap Chastain Glen loop race (solo), driven by script from just before the line
            yield return PadPress(GamepadButton.DpadDown); yield return PadPress(GamepadButton.South);
            yield return new WaitForSecondsRealtime(0.5f);
            RaceController.RouteOverride = "chastain_glen_loop.json"; RaceController.DirectionOverride = "forward"; RaceController.LapsOverride = 1;
            int ai = RaceController.AiCount; RaceController.AiCount = 0;
            yield return Vehicle("drifter");
            race.Reload(); race.StartRace();
            while (race.State == RaceController.Stage.Countdown) yield return null;
            var r = race.Route; float L = r.LapLength;
            var line = LapSlice(r.GateS - 25f, r.GateS + L + 15f, 2f);
            Place(line[0], line[1] - line[0], 8f); yield return new WaitForFixedUpdate();
            var run = new Run();
            yield return DriveLine(line, 24f, 240f, run, null, q => race.State == RaceController.Stage.Finished);
            var p = race.Entrants.First(e => e.IsPlayer);
            bool finished = race.State == RaceController.Stage.Finished;
            float lap = p.LapTimes.Count > 0 ? p.LapTimes[0] : 0f, total = p.FinalTotal;
            var lapBoard = boards.Board(glen, false); var raceBoard = boards.Board(RecordBoards.Category("Chastain Glen loop", "forward", 1), true);
            int lapWant = 1 + lapBoard.Count(e => !boards.IsNew(e.id) && e.seconds < lap), lapAt = lapBoard.ToList().FindIndex(e => boards.IsNew(e.id)) + 1;
            Check("C1: the 1-lap race finished; its lap is on the lap board at the right place, named, with vehicle and date", finished && lapAt == lapWant && lapAt == race.LastLapRank && lapBoard[lapAt - 1].name == "Dan" && lapBoard[lapAt - 1].vehicle == "drifter" && !string.IsNullOrEmpty(lapBoard[lapAt - 1].date),
                  $"lap {lap:0.000} s, missed {p.MissedTotal}: place {lapAt} (expected {lapWant}) of {lapBoard.Count}; board: " + string.Join(" | ", lapBoard.Select((e, i) => $"{i + 1}. {RaceController.Fmt((float)e.seconds)} {e.name} {e.vehicle}{(e.legacy ? " legacy" : "")}")));
            Check("C1: its total is on the 1-lap race board (#1, a new board)", finished && raceBoard.Count == 1 && race.RaceRank == 1 && System.Math.Abs(raceBoard[0].seconds - total) < 0.01,
                  $"total {total:0.000} s, rank {race.RaceRank}");
            Check("C2: the finish panel has the places", race.FinishCards.Lap != null && race.FinishCards.Lap.rank == lapAt && race.FinishCards.Race != null && race.FinishCards.Race.rank == 1,
                  race.FinishCards.Lap != null ? race.FinishCards.Lap.headline + " #" + race.FinishCards.Lap.rank + " / " + race.FinishCards.Race.headline + " #" + race.FinishCards.Race.rank : "no cards");
            yield return new WaitForSecondsRealtime(1f);
            yield return Capture("34_finish_panel");
            RaceController.AiCount = ai;

            // RECORDS from the race-over menu: RACE AGAIN, RACE SETUP, FREE ROAM, RECORDS
            yield return PadPress(GamepadButton.Start);
            yield return PadPresses(GamepadButton.DpadDown, 3); yield return PadPress(GamepadButton.South);
            yield return new WaitForSecondsRealtime(0.5f);
            Check("C3: RECORDS opens on the route just raced, best laps", menus.PageName.EndsWith("/records") && menus.FocusedLabel.StartsWith("Chastain Glen loop - Forward"), $"{menus.PageName}: {menus.FocusedLabel}");
            yield return Capture("34_board_laps");
            yield return PadPress(GamepadButton.DpadDown); yield return PadPress(GamepadButton.DpadRight);
            Check("C3: right on the board row: best races", menus.FocusedLabel == "BEST RACES", menus.FocusedLabel);
            yield return Capture("34_board_races");
            yield return PadPress(GamepadButton.DpadUp);
            var tracks = race.RecordTracks(); var seen = new System.Collections.Generic.List<string>();
            for (int i = 0; i < tracks.Count; i++)
            {
                seen.Add(menus.FocusedLabel);
                if (i == 1) yield return Capture("34_board_other_route");
                yield return PadPress(GamepadButton.DpadRight);
            }
            Check("C3: left / right steps through every route and direction and back round", seen.Distinct().Count() == tracks.Count && menus.FocusedLabel == seen[0],
                  string.Join(", ", seen) + $" (routes and directions: {tracks.Count})");
            yield return PadPress(GamepadButton.East);
            Check("C3: B goes back", !menus.PageName.EndsWith("/records"), menus.PageName);
            yield return PadPress(GamepadButton.East);
            race.EndRace();
        }
    }
}
