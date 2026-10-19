using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Racer;
using UnityEngine;

namespace ChastainRush
{
    // Round 45 in the exe (-exeRound45 <dir> [-r45Only voices] -logFile <dir>\Player.log): the police voices
    //   voices  the clips in the build's PoliceRadio folder per voice against the script; a Getaway on the Needle 600 to heat 4 (the heat five times
    //           faster in the check): a cop call and an Air one call played, each with the voice set it came from; then one of each heard (the sound
    //           on for that call only, then off)
    public sealed partial class Round20Runner
    {
        IEnumerator R45Voices()
        {
            report.AppendLine(); report.AppendLine("=== Round 45: the police voices (Dan's ElevenLabs cop and helicopter pilot)");
            var built = PoliceRadioVoice.BuiltIn; var files = Directory.Exists(built) ? Directory.GetFiles(built, "*.wav").Select(Path.GetFileNameWithoutExtension).ToList() : new List<string>();
            int pilotFiles = files.Count(f => PoliceRadio.IsPilotCall(new[] { f }) || f.EndsWith("_air")), copFiles = files.Count - pilotFiles;
            Check("the build's PoliceRadio folder has the clips", files.Count > 0, $"{files.Count} clips ({copFiles} cop, {pilotFiles} pilot) in {built}");
            if (race.State != RaceController.Stage.Free) race.EndRace();
            yield return Vehicle("moto");
            respawn.PlaceAt(RaceRoute.Surface(3540f, 900f) + Vector3.up * .4f, 345f); race.NotifyTeleported(); yield return new WaitForSecondsRealtime(1.5f);
            PoliceSetup.Mode = PoliceSetup.Game.Getaway; PoliceSetup.Two = false; PoliceSetup.Minutes = 8; PoliceSetup.Difficulty = 2; GetawayChase.CheckFast = true;
            int n0 = PoliceRadioVoice.Played.Count;
            PoliceSession.Launch(); yield return new WaitForSecondsRealtime(.5f);
            var g = GetawayChase.Current; if (!g) { Check("Getaway starts", false); yield break; }
            while (g && g.State == GetawayChase.Phase.Starting) yield return null;
            var e = new Escapee(); float t0 = Time.time; int topHeat = 1; float heliAt = -1;
            bool Cop() => PoliceRadioVoice.Played.Skip(n0).Any(p => p.Contains("voice cop") && !p.Contains("text only"));
            bool Pilot() => PoliceRadioVoice.Played.Skip(n0).Any(p => p.Contains("voice pilot") && !p.Contains("text only"));
            for (int attempt = 0; attempt < 3 && !(Cop() && Pilot()); attempt++)
            {
                if (attempt > 0) { PoliceSession.Rematch(); yield return new WaitForSecondsRealtime(.5f); g = GetawayChase.Current; while (g && g.State == GetawayChase.Phase.Starting) yield return null; e = new Escapee(); t0 = Time.time; }
                while (g && g.State == GetawayChase.Phase.Running && Time.time - t0 < 240f && !(Cop() && Pilot()))
                {
                    RunnerStep(e, car, Time.deltaTime); yield return null;
                    topHeat = Mathf.Max(topHeat, g.Heat); if (g.Heli && heliAt < 0) heliAt = g.Clock;
                    if (g.Heli && heliAt >= 0 && g.Clock - heliAt > 60f) break;   // the helicopter has had a minute to spot the runner
                }
            }
            StopRunner(car);
            var played = PoliceRadioVoice.Played.Skip(n0).ToList();
            report.AppendLine($"  top heat {topHeat}, the helicopter {(heliAt >= 0 ? $"up at {heliAt:0} s" : "never up")}");
            foreach (var p in played.Take(24)) report.AppendLine("  played: " + p);
            Check("in the Getaway, a cop call is played in the cop's voice", Cop(), played.FirstOrDefault(p => p.Contains("voice cop")) ?? "none");
            Check("in the Getaway at heat 4, an Air one call is played in the pilot's voice (never mixed: every piece from the pilot's set)", Pilot(), played.FirstOrDefault(p => p.Contains("voice pilot")) ?? "none");
            PoliceSession.Stop(); GetawayChase.CheckFast = false; yield return new WaitForSecondsRealtime(6.5f);   // past the radio's 6 s repeat guard
            // heard: one cop call and one Air one call, the sound on for each and off after
            foreach (var call in new[] { new[] { "R14" }, new[] { "U7", "R07" } })
            {
                int n1 = PoliceRadioVoice.Played.Count; CheckRun.Hear(true); PoliceRadio.Speak(call, true);
                float w = 0; yield return new WaitForSecondsRealtime(.3f); while (PoliceRadioVoice.Speaking && w < 8f) { w += Time.unscaledDeltaTime; yield return null; }
                CheckRun.Hear(false);
                report.AppendLine($"  heard (sound on {w + .3f:0.0} s, then off): {string.Join(" | ", PoliceRadioVoice.Played.Skip(n1))}");
            }
        }
    }
}
