using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Racer;
using UnityEngine;

namespace ChastainRush
{
    // Round 35 Part B in the exe: ChastainRush.exe -exeRound35 <dir> -r35Only radio -radio -traffic Normal -logFile <dir>\Player.log
    // (muted: CheckRun). A whole Underground race and a whole Chastain Ridge race, 3 AI, the camera (the listener) riding with the
    // first AI car through the pipes, warps and the hub; every frame: is the radio's voice really playing (isPlaying and not culled to a
    // virtual voice). A gap is allowed only across a song change. -r35RadioOld puts the radio back as it was (priority 180, through the
    // listener's low-pass) to show the cause.
    public sealed partial class Round20Runner
    {
        IEnumerator R35Radio()
        {
            report.AppendLine(); report.AppendLine("=== Part B: the radio through whole races");
            var radio = LocalRadio.Current;
            if (!radio) { Check("Part B: the radio is on in this run", false, "no radio: run with -radio"); yield break; }
            float waited = 0f; while (waited < 30f && !(LocalRadio.Source && LocalRadio.Source.isPlaying)) { waited += 0.25f; yield return new WaitForSecondsRealtime(0.25f); }
            if (Environment.GetCommandLineArgs().Contains("-r35RadioOld")) { LocalRadio.Source.priority = 180; LocalRadio.Source.bypassListenerEffects = false; report.AppendLine("      (BEFORE: the radio's voice as it was, priority 180, through the listener's effects)"); }
            report.AppendLine($"      voices: {AudioSettings.GetConfiguration().numRealVoices} real, {AudioSettings.GetConfiguration().numVirtualVoices} virtual; the radio's priority {LocalRadio.Source.priority}, bypasses the listener's effects {LocalRadio.Source.bypassListenerEffects}");
            yield return RadioRace("underground.json", "the Underground race", Vector3.zero);
            yield return RadioRace("chastain_ridge.json", "the Chastain Ridge race", RaceRoute.Surface(3420f, 985f) + Vector3.up * 0.4f);
        }

        IEnumerator RadioRace(string file, string label, Vector3 park)
        {
            RaceController.RouteOverride = file; RaceController.DirectionOverride = "forward"; RaceController.LapsOverride = 3;
            RaceController.AiCount = 3; RaceController.AiVehiclesOverride = null;
            if (race.State != RaceController.Stage.Free) race.EndRace();
            race.Reload(); race.StartRace();
            var chase = Camera.main.GetComponent<ChaseCamera>(); var own = chase ? chase.target : null;
            var ai = race.Entrants.Where(e => !e.IsPlayer).ToList(); var rider = ai[0];
            if (park != Vector3.zero) { while (race.State == RaceController.Stage.Countdown) yield return null; respawn.PlaceAt(park, 0f); race.NotifyTeleported(); }
            if (chase) { chase.target = rider.Car.transform; chase.Snap(); }
            Time.timeScale = 2f; Time.maximumDeltaTime = 0.5f;
            int frames = 0, bad = 0, gaps = 0, songGaps = 0, songs = 0, maxSources = 0; float longest = 0f, gapStart = -1f, lastChange = -99f; string gapSong = null, song = radio(), longestWhere = "";
            float t0 = Time.realtimeSinceStartup, end = t0 + 10f * 60f, nextCount = 0f;
            string radio() => LocalRadio.Current ? LocalRadio.Current.CurrentPath : null;
            while (Time.realtimeSinceStartup < end && ai.Any(e => !e.Finished))
            {
                yield return null; frames++;
                var src = LocalRadio.Source; bool real = src && src.isPlaying && !src.isVirtual; float now = Time.realtimeSinceStartup;
                if (radio() != song) { song = radio(); songs++; lastChange = now; }   // the radio names the next song before the old voice stops
                if (!real) { bad++; if (gapStart < 0f) { gapStart = now; gapSong = radio(); } }
                else if (gapStart >= 0f)
                {
                    float g = now - gapStart; bool change = radio() != gapSong || lastChange >= gapStart - 1f;
                    if (change) songGaps++; else { gaps++; if (g > longest) { longest = g; var p = rider.Car.Body.position; longestWhere = $"at ({p.x:0}, {p.y:0}, {p.z:0}), {(src && src.isVirtual ? "culled" : "stopped")}"; } }
                    gapStart = -1f;
                }
                if (now >= nextCount) { nextCount = now + 2f; maxSources = Mathf.Max(maxSources, FindObjectsByType<AudioSource>(FindObjectsSortMode.None).Count(s => s.isPlaying)); }
            }
            Time.timeScale = 1f;
            if (chase) { chase.target = own; chase.Snap(); }
            Check($"Part B, {label}: the radio's voice really playing every frame (gaps only across song changes)", gaps == 0 && ai.All(e => e.Finished),
                  $"{frames} frames over {Time.realtimeSinceStartup - t0:0} s, {bad} not playing ({songGaps} song changes with a gap, {songs} songs started); {gaps} other gaps, the longest {longest:0.00} s {longestWhere}; up to {maxSources} sounds playing at once; AI finished {ai.Count(e => e.Finished)} of {ai.Count}");
            race.EndRace();
            yield return new WaitForSecondsRealtime(1f);
        }
    }
}
