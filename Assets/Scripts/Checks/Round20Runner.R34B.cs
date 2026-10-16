using System.Collections;
using System.IO;
using Racer;
using UnityEngine;

namespace ChastainRush
{
    // Round 34 Part B in the exe: ChastainRush.exe -exeRound34 <dir> -r34Only radio -radio -logFile <dir>\Player.log (muted; the radio
    // only plays in a check run with -radio). Mid-song, the window loses and gets back focus five times (LocalRadio.Hold, what
    // OnApplicationFocus calls; the fifth time the audio device is also reset, which stops every voice), then the game's pause
    // (the menus' AudioListener.pause and time stopped): the same song each time, within 1 s of where it was.
    public sealed partial class Round20Runner
    {
        IEnumerator R34Radio()
        {
            report.AppendLine(); report.AppendLine("=== Part B: the radio keeps its song when the game loses focus");
            var radio = LocalRadio.Current;
            if (!radio) { Check("Part B: the radio is on in this run", false, "no radio: run with -radio"); yield break; }
            float waited = 0f;
            while (waited < 60f && !(LocalRadio.Source && LocalRadio.Source.isPlaying && LocalRadio.Source.time > 5f && LocalRadio.Source.clip && LocalRadio.Source.clip.length - LocalRadio.Source.time > 45f))
            {
                if (LocalRadio.Source && LocalRadio.Source.isPlaying && LocalRadio.Source.clip && LocalRadio.Source.clip.length - LocalRadio.Source.time <= 45f) { radio.Next(); yield return new WaitForSecondsRealtime(1f); }   // a song with room for the test
                waited += 0.25f; yield return new WaitForSecondsRealtime(0.25f);
            }
            if (!LocalRadio.Source || !LocalRadio.Source.isPlaying) { Check("Part B: a song playing", false, radio.Status); yield break; }
            string song = radio.CurrentPath; int same = 0; var lines = new System.Collections.Generic.List<string>();
            for (int i = 1; i <= 5; i++)
            {
                float before = LocalRadio.Source.time;
                radio.Hold(true);   // the window loses focus
                yield return new WaitForSecondsRealtime(1.5f);
                bool device = i == 5;
                if (device) AudioSettings.Reset(AudioSettings.GetConfiguration());   // the audio device reset as well: every voice stopped
                radio.Hold(false);   // focus back
                float t = 0f; while (t < 5f && !LocalRadio.Source.isPlaying) { t += 0.05f; yield return new WaitForSecondsRealtime(0.05f); }
                float after = LocalRadio.Source.time; bool ok = radio.CurrentPath == song && LocalRadio.Source.isPlaying && Mathf.Abs(after - before) < 1f;
                if (ok) same++;
                lines.Add($"{i}{(device ? " (and the device reset)" : "")}: {before:0.0} s -> {after:0.0} s {(radio.CurrentPath == song ? "same song" : "OTHER SONG " + Path.GetFileName(radio.CurrentPath))}{(LocalRadio.Source.isPlaying ? "" : ", NOT PLAYING")}");
                yield return new WaitForSecondsRealtime(2f);
            }
            Check("Part B: focus lost and back five times (the fifth with the audio device reset): the same song, within 1 s of where it was", same == 5, $"{Path.GetFileName(song)}; " + string.Join("; ", lines));
            // the game's own pause: what the menus do (time stopped, AudioListener paused; the radio ignores the listener's pause)
            float p0 = LocalRadio.Source.time; float ts = Time.timeScale;
            Time.timeScale = 0f; AudioListener.pause = true;
            yield return new WaitForSecondsRealtime(2f);
            Time.timeScale = ts; AudioListener.pause = false;
            yield return new WaitForSecondsRealtime(1f);
            Check("Part B: the game's pause and back: the same song, still playing", radio.CurrentPath == song && LocalRadio.Source.isPlaying, $"{p0:0.0} s -> {LocalRadio.Source.time:0.0} s, {(radio.CurrentPath == song ? "same song" : "OTHER SONG")}");
        }
    }
}
