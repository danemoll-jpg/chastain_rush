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
    // Round 57 (F) in the exe: race replays (-exeRound57 <dir> -r57Only replay,replaybest -traffic Normal -logFile <dir>\Player.log)
    //   replay      Addison Heights forward, 3 laps, 3 AI, traffic: the check's own driver, lap 2 by the Elaine jump (through the barricade), lap 3 by
    //               the Lyndon crew jump, a mailbox on lap 1; finish; X on the finish screen watches it: the recording against the live positions,
    //               each panel broken at its moment, every camera (pictures), the controls by a virtual pad and by keys; Y saves it, the REPLAYS page
    //               lists it (picture); the size of the file
    //   replaybest  1-lap races on Addison Heights: a slow one (the first #1 lap and race kept), a fast one (both replaced), a slow one (kept as they were)
    public sealed partial class Round20Runner
    {
        GameMenus r57menus;
        IEnumerator R57Menus() { if (!r57menus) { r57menus = GameMenus.Create(); yield return new WaitForSecondsRealtime(1.5f); if (r57menus) r57menus.CloseForCheck(); } }
        IEnumerator PressKey(Key k) { var kb = Keyboard.current; if (kb == null) yield break; InputSystem.QueueStateEvent(kb, new KeyboardState(k)); yield return null; yield return null; InputSystem.QueueStateEvent(kb, new KeyboardState()); yield return new WaitForSecondsRealtime(0.3f); }

        // the whole race's line for the check's driver: the grid to the gate, lap 1 (past a mailbox), lap 2 by the Elaine jump, lap 3 by the Lyndon crew
        List<Vector3> R57RaceLine(out string mailboxAt)
        {
            var r = race.Route; float G = r.GateS, L = r.LapLength; mailboxAt = "none";
            var line = LapSlice(G - race.RouteData.lead_in_length_m + 4f, G);
            var shortcuts = new[] { (RaceShortcut)null, r.Shortcuts.FirstOrDefault(s => s.Id == "f_elaine_jump"), r.Shortcuts.FirstOrDefault(s => s.Id == "f_lyndon_crew") };
            for (int k = 0; k < 3; k++)
            {
                float a = G + k * L, b = G + (k + 1) * L; var sc = shortcuts[k];
                if (sc == null)
                {
                    var lap = LapSlice(a, b);
                    // a mailbox 2 to 5 m off the lap's middle on a stretch away from the shortcuts: the line eased over it
                    var boxes = FindObjectsByType<FenceChunk>(FindObjectsSortMode.None).Where(c => c.sound == BreakSounds.Kind.Mailbox)
                        .SelectMany(c => c.panels.Select(p => c.transform.TransformPoint(p.centre))).ToList();
                    foreach (var m in boxes.OrderBy(m => Mathf.Abs(AbsStation(m) - (G + 300f))))
                    {
                        float sm = AbsStation(m); var on = race.Route.PointAt(sm, out var f); float lat = Vector3.Dot(Flat(m - on), Vector3.Cross(Vector3.up, f).normalized);
                        if (Mathf.Abs(lat) < 1.5f || Mathf.Abs(lat) > 3.8f || sm < G + 120f || sm > G + L - 120f) continue;
                        if (r.Shortcuts.Any(s => s.Path.Any(q => Flat(q - m).magnitude < 60f))) continue;
                        var side = Vector3.Cross(Vector3.up, f).normalized; float smm = sm;
                        lap = lap.Select(q => { float d = Mathf.Abs(AbsStation(q) - smm); float w = d < 4f ? 1f : d < 30f ? Mathf.SmoothStep(1f, 0f, (d - 4f) / 26f) : 0f; var p = q + side * lat * w; return RaceRoute.Surface(p.x, p.z); }).ToList();
                        mailboxAt = $"({m.x:0.0}, {m.z:0.0}), {lat:0.0} m off the lap";
                        break;
                    }
                    line.AddRange(lap); continue;
                }
                float sa = AbsStation(sc.Path[0]); while (sa < a) sa += L; while (sa >= b) sa -= L;
                float sb = AbsStation(sc.Path[sc.Path.Length - 1]); while (sb < sa) sb += L;
                line.AddRange(LapSlice(a, sa - 2f)); line.AddRange(sc.Path); line.AddRange(LapSlice(sb + 2f, b));
            }
            line.AddRange(LapSlice(G + 3f * L, G + 3f * L + 40f));
            return line;
        }

        IEnumerator R57Replay()
        {
            report.AppendLine(); report.AppendLine($"=== F. RACE REPLAYS: Addison Heights forward, 3 laps, 3 AI, traffic {(Traffic.Instance ? Traffic.LevelNames[Traffic.Instance.Level] : "none")}");
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            if (pad == null) pad = InputSystem.AddDevice<Gamepad>("Round57 virtual pad");
            yield return R57Menus();
            yield return Vehicle("drifter");
            yield return StartRoute("addison_heights.json", "forward", 3, 3);
            var p = race.Player; var line = R57RaceLine(out string mailbox);
            while (race.RaceClock < 4f) yield return null;   // the AI away first (the check's driver does not dodge them)
            report.AppendLine($"  the driver's line: {line.Count} points; a mailbox on lap 1 at {mailbox}");
            var samples = new List<(float t, int who, Vector3 pos)>(); float nextSample = 2f; int broken0 = FenceChunk.BrokenPanels;
            var run = new Run(); int restarts = 0; var stops = new List<string>();
            // the check's driver does not dodge traffic or the AI: stuck (8 s), it goes on 30 m further along its line (as a reset would), at most 6 times
            var lineCum = new float[line.Count]; for (int i = 1; i < line.Count; i++) lineCum[i] = lineCum[i - 1] + Flat(line[i] - line[i - 1]).magnitude;
            var rest = line; float doneS = 0f;
            while (true) {
            yield return DriveLine(rest, 40f, 600f, run, null, x =>
            {
                // sampled after the physics step: the pose the race clock's next value reads (RaceController and the recorder read it in the next FixedUpdate)
                if (race.RaceClock >= nextSample) { nextSample += 3f; for (int i = 0; i < race.Entrants.Count; i++) if (race.Entrants[i].Car) samples.Add((race.RaceClock + Time.fixedDeltaTime, i, race.Entrants[i].Car.Body.position)); }
                return p.Finished;
            }, 5f, false);
            if (p.Finished || run.end != "stopped" || restarts >= 6) break;
            restarts++; var at = car.Body.position; stops.Add($"({at.x:0}, {at.z:0}) at {race.RaceClock:0} s");
            float sNow = ProjectNear(line, lineCum, at, doneS, out _) + 30f; doneS = sNow;
            int k = System.Array.FindIndex(lineCum, c => c >= sNow); if (k < 0 || k + 3 >= line.Count) break;
            rest = line.Skip(k).ToList(); Place(rest[0], rest[2] - rest[0], 8f); run.end = "";
            yield return new WaitForFixedUpdate();
            }
            if (restarts > 0) report.AppendLine($"  the check's driver was stuck {restarts} time(s) and went on 30 m further: {string.Join("; ", stops)}");
            var rec = ReplayRecorder.Instance;
            if (!p.Finished)
            {
                yield return Capture("57_F_driver_stopped");
                var hit = Physics.OverlapSphere(car.Body.position, 4f, ~0, QueryTriggerInteraction.Ignore).Where(c => c.attachedRigidbody != car.Body).Select(c => c.name).Distinct();
                report.AppendLine($"  the check's driver stopped at ({car.Body.position.x:0.0}, {car.Body.position.z:0.0}) at race clock {race.RaceClock:0.0} s, touching: {string.Join(", ", hit)}");
            }
            Check("F: the player finished the 3 laps", p.Finished, $"laps {p.CompletedLaps} ({string.Join(" / ", p.LapTimes.Select(x => x.ToString("0.0")))}), shortcuts {string.Join(", ", p.ShortcutsTaken)}, {run.end}");
            report.AppendLine($"  panels broken in the race: {FenceChunk.BrokenPanels - broken0}; shortcuts taken {string.Join(", ", p.ShortcutsTaken)}");
            car.Body.linearVelocity = Vector3.zero;
            float until = Time.realtimeSinceStartup + 12f; while (WinnerShot.Active && Time.realtimeSinceStartup < until) yield return null;
            yield return new WaitForSecondsRealtime(1f);
            Check("F: WATCH / SAVE REPLAY show on the finish screen", GameMenus.ReplayButtonsShowing);
            yield return Capture("57_F_finish_screen");
            // Y saves (the recording may still run: kept for when it stops), X watches (it stops the recording)
            yield return Press(GamepadButton.North);
            yield return Press(GamepadButton.West);
            yield return new WaitForSecondsRealtime(1f);
            var v = ReplayViewer.Current;
            Check("F: X on the finish screen opens the replay", v != null, ReplayRecorder.Note);
            if (v == null) { race.EndRace(); yield break; }
            var d = v.Data;
            report.AppendLine($"  recorded: {ReplayRecorder.Note}");
            // the recording against the race: every 3 s, every racer
            var offs = new List<(float e, string at)>();
            foreach (var (t, who, pos) in samples)
            {
                if (who >= d.racers.Count) continue; var f = ReplayData.At(d.racers[who].track, t); float e = (f.p - pos).magnitude;
                offs.Add((e, $"{d.racers[who].name} at {t:0.0} s ({pos.x:0}, {pos.z:0}) {f.speed:0} m/s"));
            }
            offs = offs.OrderByDescending(x => x.e).ToList(); float most = offs.Count > 0 ? offs[0].e : 0f, median = offs.Count > 0 ? offs[offs.Count / 2].e : 0f;
            Check("F: the replay's positions match the race (every racer every 3 s) within 0.5 m", most < 0.5f, $"{samples.Count} samples, median {median:0.000} m, the most off: {string.Join("; ", offs.Take(3).Select(x => $"{x.e:0.000} m {x.at}"))}");
            // the puppets where the recording puts them
            if (!v.Paused) v.TogglePause(); v.Seek(40f); yield return null; float pe = 0f;
            for (int i = 0; i < d.racers.Count; i++) { var pu = v.PuppetOf(i); if (pu) pe = Mathf.Max(pe, (pu.transform.position - ReplayData.At(d.racers[i].track, 40f).p).magnitude); }
            Check("F: each racer's puppet stands where the recording puts it", pe < 0.05f, $"{pe:0.000} m");
            // the breakables at their moments
            var panels = d.events.Where(e => e.kind == ReplayData.Kind.Panel).ToList();
            int onTime = 0; var bad = new List<string>();
            foreach (var e in panels.Take(12))
            {
                v.Seek(e.t - 0.6f); yield return null; bool before = FenceChunk.BrokenAt(e.p);
                v.Seek(e.t + 0.1f); yield return null; bool after = FenceChunk.BrokenAt(e.p);
                if (!before && after) onTime++; else bad.Add($"{e.t:0.0} s ({e.p.x:0}, {e.p.z:0}) before {before} after {after}");
            }
            Check($"F: each broken panel is whole just before its moment and broken just after (of {panels.Count})", panels.Count > 0 && onTime == Mathf.Min(12, panels.Count), $"{onTime} on time; {string.Join("; ", bad)}");
            report.AppendLine($"  events: {d.events.Count} ({string.Join(", ", d.events.GroupBy(e => e.kind).Select(g => g.Key + " " + g.Count()))}); panels: {string.Join(", ", panels.GroupBy(e => (BreakSounds.Kind)e.b).Select(g => g.Key + " " + g.Count()))}");
            // the cameras: TV at the Elaine jump, the chase, the free camera
            var jumpAt = panels.Where(e => Flat(e.p - new Vector3(3190f, 0f, 305f)).magnitude < 40f).Select(e => e.t).DefaultIfEmpty(d.t0 + 70f).First();
            v.Seek(jumpAt - 1f); v.SetView(ReplayViewer.Cam.TV); yield return new WaitForSecondsRealtime(1.5f);
            Check("F: TV cameras placed along the way and one showing the racer", v.TvSpots.Count > 10 && v.TvIndex >= 0, $"{v.TvSpots.Count} cameras, showing #{v.TvIndex}");
            yield return Capture("57_F_tv_barricade");
            v.Seek(jumpAt + 3f); yield return new WaitForSecondsRealtime(2.5f); yield return Capture("57_F_tv_elaine_jump");
            v.SetView(ReplayViewer.Cam.Chase); yield return new WaitForSecondsRealtime(1.5f);
            var cam = Camera.main; var chase = cam ? cam.GetComponent<ChaseCamera>() : null;
            Check("F: the chase camera follows the focused racer's puppet", chase && chase.enabled && chase.target == v.PuppetOf(v.Focus).transform);
            yield return Capture("57_F_chase");
            v.FocusOn(v.Focus + 1); yield return new WaitForSecondsRealtime(1.2f); yield return Capture("57_F_chase_other_racer");
            v.SetView(ReplayViewer.Cam.Free); var c0 = cam.transform.position;
            yield return Hold(new GamepadState { leftStick = new Vector2(0f, 1f) }, 1.0f);
            Check("F: the free camera moves with the left stick", (cam.transform.position - c0).magnitude > 5f, $"{(cam.transform.position - c0).magnitude:0.0} m");
            yield return Capture("57_F_free");
            // the controls: the pad
            v.SetView(ReplayViewer.Cam.TV); v.Seek(60f); bool paused0 = v.Paused;
            yield return Press(GamepadButton.South); bool padPause = v.Paused != paused0;
            yield return Press(GamepadButton.South);
            float t0 = v.T; yield return Hold(new GamepadState { rightTrigger = 1f }, 1.0f); float ff = v.T - t0;
            t0 = v.T; yield return Hold(new GamepadState { leftTrigger = 1f }, 1.0f); float rw = v.T - t0;
            yield return Press(GamepadButton.West); float slow = v.Rate;
            yield return Press(GamepadButton.West); yield return Press(GamepadButton.West);
            var view0 = v.View; yield return Press(GamepadButton.North); bool padCam = v.View != view0;
            int f0 = v.Focus; yield return Press(GamepadButton.RightShoulder); bool padRacer = v.Focus != f0;
            yield return Press(GamepadButton.DpadRight); float lapT = v.T;
            yield return Press(GamepadButton.DpadUp); bool restart = Mathf.Abs(v.T - d.t0) < 0.5f;
            bool hud0 = ReplayViewer.HudShown; yield return Press(GamepadButton.DpadDown); bool padHud = ReplayViewer.HudShown != hud0;
            report.AppendLine($"  pad: A pause {padPause}; RT 1 s {ff:+0.0;-0.0} s; LT 1 s {rw:+0.0;-0.0} s; X slow x{slow}; Y camera {padCam}; RB racer {padRacer}; D-pad right to {lapT:0.0} s; D-pad up restart {restart}; D-pad down HUD {padHud}");
            Check("F: the pad's controls (A, RT, LT, X, Y, RB, D-pad)", padPause && ff > 2.5f && rw < -2.5f && slow < 1f && padCam && padRacer && restart && padHud);
            yield return Capture("57_F_hud");
            // the keys
            paused0 = v.Paused; yield return PressKey(UnityEngine.InputSystem.Key.Space); bool keyPause = v.Paused != paused0; yield return PressKey(UnityEngine.InputSystem.Key.Space);
            view0 = v.View; yield return PressKey(UnityEngine.InputSystem.Key.C); bool keyCam = v.View != view0;
            hud0 = ReplayViewer.HudShown; yield return PressKey(UnityEngine.InputSystem.Key.H); bool keyHud = ReplayViewer.HudShown != hud0;
            yield return PressKey(UnityEngine.InputSystem.Key.Home); bool keyRestart = Mathf.Abs(v.T - d.t0) < 0.5f;
            Check("F: the keys (Space, C, H, Home)", keyPause && keyCam && keyHud && keyRestart);
            // B leaves
            yield return Press(GamepadButton.East); yield return new WaitForSecondsRealtime(0.5f);
            Check("F: B leaves the replay (the finish screen back)", !ReplayViewer.Active && race.State == RaceController.Stage.Finished && GameMenus.ReplayButtonsShowing);
            // the saved file (Y before X: kept when the recording stopped)
            var saved = ReplayData.List(false);
            Check("F: Y on the finish screen saved it", saved.Count > 0, saved.Count > 0 ? saved[0].path : "none");
            if (saved.Count > 0) { long kb = new FileInfo(saved[0].path).Length / 1024; report.AppendLine($"  one 3-lap replay with traffic: {kb} KB ({saved[0].path})"); }
            race.EndRace();
            // the REPLAYS page
            if (r57menus) { r57menus.OpenReplaysForCheck(); yield return new WaitForSecondsRealtime(1f); yield return Capture("57_F_replays_page");
                var texts = FindObjectsByType<UnityEngine.UI.Text>(FindObjectsSortMode.None).Where(t => t.gameObject.activeInHierarchy).Select(t => t.text).ToList();
                Check("F: the REPLAYS page lists the saved race and the #1 lap and race", texts.Any(t => t.StartsWith("SAVED")) && texts.Any(t => t.StartsWith("#1 LAP")) && texts.Any(t => t.StartsWith("#1 RACE")), string.Join(" | ", texts.Where(t => t.StartsWith("SAVED") || t.StartsWith("#1")).Take(6)));
                // watch the saved one from the page (free roam: the course's barriers built for it)
                r57menus.WatchFirstSavedForCheck(); yield return new WaitForSecondsRealtime(2f);
                Check("F: a saved replay plays from the page", ReplayViewer.Active, ReplayViewer.Active ? $"{ReplayViewer.Current.Data.label}" : "");
                yield return Capture("57_F_from_page");
                if (ReplayViewer.Current) ReplayViewer.Current.Close(); yield return new WaitForSecondsRealtime(0.5f);
                r57menus.CloseForCheck(); }
        }

        // the #1s: 1-lap races, slow, fast, slow
        IEnumerator R57Best()
        {
            report.AppendLine(); report.AppendLine("=== F. the #1 replays: Addison Heights forward, 1 lap, no AI: slow (the first #1s), fast (replaced), slow again (kept)");
            yield return Vehicle("drifter");
            string cat = null; var times = new List<(float lap, float race)>();
            foreach (var (cap, name) in new[] { (14f, "slow"), (30f, "fast"), (15f, "slow again") })
            {
                yield return StartRoute("addison_heights.json", "forward", 0, 1);
                cat = race.BoardCategory;
                var r = race.Route; var p = race.Player;
                var line = LapSlice(r.GateS - race.RouteData.lead_in_length_m + 4f, r.GateS + r.LapLength + 30f);
                var run = new Run(); Time.timeScale = 2f;
                yield return DriveLine(line, cap, 300f, run, null, x => p.Finished, 5f, false);
                Time.timeScale = 1f;
                race.EndRace(); yield return new WaitForSecondsRealtime(0.5f);
                ReplayData lapD = null, raceD = null;
                try { lapD = ReplayData.Read(ReplayData.BestPath(RecordBoards.LapCategory(cat), false), true); } catch (System.Exception) { }
                try { raceD = ReplayData.Read(ReplayData.BestPath(cat, true), true); } catch (System.Exception) { }
                report.AppendLine($"  {name} ({cap:0} m/s at most): lap {(p.LapTimes.Count > 0 ? p.LapTimes[0].ToString("0.00") : "-")} s, lap rank #{race.LastLapRank}, race rank #{race.RaceRank}; kept: #1 lap {(lapD != null ? lapD.bestTime.ToString("0.00") + " s " + lapD.date : "none")}, #1 race {(raceD != null ? raceD.bestTime.ToString("0.00") + " s " + raceD.date : "none")}; {ReplayRecorder.Note}");
                times.Add((lapD != null ? lapD.bestTime : -1f, raceD != null ? raceD.bestTime : -1f));
            }
            Check("F: the first race's #1 lap and race replays are kept", times[0].lap > 0f && times[0].race > 0f);
            Check("F: the faster race replaced both", times[1].lap > 0f && times[1].lap < times[0].lap - 0.5f && times[1].race < times[0].race - 0.5f, $"{times[0].lap:0.00} -> {times[1].lap:0.00}, {times[0].race:0.00} -> {times[1].race:0.00}");
            Check("F: the slower one after did not", Mathf.Abs(times[2].lap - times[1].lap) < 0.01f && Mathf.Abs(times[2].race - times[1].race) < 0.01f);
            // the #1 lap's replay plays just its lap
            try
            {
                var lapFull = ReplayData.Read(ReplayData.BestPath(RecordBoards.LapCategory(cat), false));
                Check("F: the #1 lap's replay plays that lap (a little either side)", lapFull.kind == "lap" && lapFull.Duration < lapFull.bestTime + 6f && lapFull.Duration > lapFull.bestTime, $"{lapFull.Duration:0.0} s for a {lapFull.bestTime:0.0} s lap");
            }
            catch (System.Exception e) { Check("F: the #1 lap's replay reads", false, e.Message); }
        }
    }
}
