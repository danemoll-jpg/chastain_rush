using System.Collections;
using System.Linq;
using Racer;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace ChastainRush
{
    public sealed partial class Round20Runner
    {
        // ---------------------------------------------------------------- 2. the map, with a virtual gamepad

        Gamepad pad;
        IEnumerator Press(GamepadButton b)
        {
            InputSystem.QueueStateEvent(pad, new GamepadState().WithButton(b));
            yield return null; yield return null;
            InputSystem.QueueStateEvent(pad, new GamepadState());
            yield return new WaitForSecondsRealtime(0.3f);
        }
        IEnumerator Hold(GamepadState s, float seconds)
        {
            InputSystem.QueueStateEvent(pad, s);
            yield return new WaitForSecondsRealtime(seconds);
            InputSystem.QueueStateEvent(pad, new GamepadState());
            yield return new WaitForSecondsRealtime(0.2f);
        }

        IEnumerator MapCheck()
        {
            report.AppendLine(); report.AppendLine("=== 2. The map with a virtual gamepad: View opens it (the game pauses), triggers zoom, the left stick pans, A shows the shortcuts and pipes, the right stick's click centers, B closes; free roam, then a race");
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;   // a check runs without window focus
            pad = InputSystem.AddDevice<Gamepad>("Round20 virtual pad");
            var map = WorldMap.Instance;
            if (!map) { Check("the map is there", false); yield break; }
            UseRoute("noonday_creek.json", "forward");
            yield return Vehicle("drifter");
            var at = race.Route.PointAt(race.Route.GateS + 300f, out var fw); Place(at, fw, 0f);
            yield return new WaitForSecondsRealtime(1f);
            int ai0 = RaceController.AiCount;
            foreach (var where in new[] { "free roam", "a race" })
            {
                if (where == "a race")
                {
                    RaceController.AiCount = 3; RaceController.AiVehiclesOverride = null;
                    race.StartRace();
                    while (race.State != RaceController.Stage.Racing) yield return null;
                    yield return new WaitForSecondsRealtime(6f);   // the racers spread out a little
                }
                string tag = where == "a race" ? "race" : "free";
                yield return Press(GamepadButton.Select);
                bool open = WorldMap.IsOpen, paused = Time.timeScale == 0f;
                float clock0 = race.RaceClock; yield return new WaitForSecondsRealtime(0.8f); bool held = race.RaceClock == clock0;
                yield return Capture($"2_map_{tag}_open");
                float z0 = map.Zoom; yield return Hold(new GamepadState { rightTrigger = 1f }, 1.0f); float z1 = map.Zoom;
                yield return Hold(new GamepadState { leftTrigger = 1f }, 0.4f); float z2 = map.Zoom;
                var c0 = map.Center; yield return Hold(new GamepadState { leftStick = new Vector2(1f, 0.5f) }, 0.8f); var c1 = map.Center;
                yield return Press(GamepadButton.DpadRight); bool layers = map.ShowLayers;   // Round 49: the D-pad's right (A chooses under the pointer)
                yield return Capture($"2_map_{tag}_zoomed_panned_layers");
                yield return Press(GamepadButton.DpadLeft);   // Round 49: the D-pad's left (was the right stick's click)
                var mine = map.Normalized(car.Body.position); bool centered = (map.Center - mine).magnitude < 0.01f;
                bool noCapture = !DeveloperHud.Instance || (!DeveloperHud.Instance.Capturing && !DeveloperHud.Instance.CommentOpen);
                yield return Press(GamepadButton.East);
                bool closed = !WorldMap.IsOpen, resumed = Time.timeScale > 0f;
                float clock1 = race.RaceClock; yield return new WaitForSecondsRealtime(0.5f); bool runs = where == "free roam" || race.RaceClock > clock1;
                Check($"the map in {where}: View opens it and the game pauses{(where == "a race" ? " (the race clock stops)" : "")}; RT / LT zoom; the left stick pans; A shows the shortcuts and pipes; the right stick's click centers on the player; no bug capture; B closes and the game goes on",
                      open && paused && (where == "free roam" || held) && z1 > z0 * 1.5f && z2 < z1 && (c1 - c0).magnitude > 0.005f && layers && centered && noCapture && closed && resumed && runs,
                      $"open {open}, paused {paused}{(where == "a race" ? $", race clock held {held}" : "")}, zoom {z0:0.0} -> {z1:0.0} -> {z2:0.0}, center moved {(c1 - c0).magnitude * map.World.width:0} m, layers {layers}, centered {centered}, no capture {noCapture}, closed {closed}, running again {resumed}{(where == "a race" ? $", race clock running {runs}" : "")}");
                // the same button closes it too (as in Woodstock Rush)
                yield return Press(GamepadButton.Select); bool again = WorldMap.IsOpen; yield return Press(GamepadButton.Select);
                Check($"the map in {where}: View opens and closes it", again && !WorldMap.IsOpen && Time.timeScale > 0f);
                map.SetLayers(false);
            }
            race.EndRace(); RaceController.AiCount = ai0;
            InputSystem.RemoveDevice(pad);
        }
    }
}
