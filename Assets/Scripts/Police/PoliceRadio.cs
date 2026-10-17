using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ChastainRush;
using UnityEngine;

namespace Racer
{
    // Chastain Rush, Round 39 (Part D3): Racer's police radio (0.99 Part F), its voice Dan's. Every radio line is a list of the script's IDs (a whole
    // line, or a unit + a phrase + a STREET + an ending); the words on screen are built from the same IDs. The script is Tools/Audio/
    // police-radio-lines.csv (id, text, when, map_names), embedded in the scene by the map build (PoliceRadioScript): R01-R20 whole lines, P01-P15
    // phrases a street follows, U1-U7 units, S01-S08 Speed Patrol, H01-H04 hidden police, D001-D136 the map's streets (map_names: the OSM names each
    // covers). New against Racer: no landmark (L) lines; every place the radio names is the street the suspect or unit is on or heading to (the nearest
    // named street: RoadNet's road names through map_names; off-road, the nearest street ahead); "heading north / south / east / west on" and
    // "turning onto" (P11-P15). The clips: Dan's recordings, <id>.wav (takes R05a, R05b ...), from the drop-in folder first
    // (%USERPROFILE%\AppData\LocalLow\Chastain Rush\Chastain Rush\Sounds\PoliceRadio\, read at run time: no rebuild for new clips), then the built-in
    // SourceArt folder copied beside the exe (PoliceRadio\ in the build's data); none: the line shows as text only. Joined, trimmed and run through
    // Racer's radio effect, one at a time, queued; the music radio ducks under it (LocalRadio), the voice under the music radio's own priority.
    public static class PoliceRadio
    {
        static Dictionary<string, string> display; static Dictionary<string, string> streetOf;   // id -> text; OSM name -> D id
        public static void Load(string csv)
        {
            display = new Dictionary<string, string>(); streetOf = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrEmpty(csv)) return;
            var lines = csv.Replace("\r", "").Split('\n');
            for (int i = 1; i < lines.Length; i++)
            {
                var f = Fields(lines[i]); if (f.Count < 2 || string.IsNullOrEmpty(f[0])) continue;
                string id = f[0].Trim(), text = f[1].Trim(); display[id] = text;
                if (f.Count >= 4 && id.StartsWith("D")) foreach (var n in f[3].Split(';', '|')) { var nm = n.Trim(); if (nm.Length > 0 && !streetOf.ContainsKey(nm)) streetOf[nm] = id; }
            }
        }
        static List<string> Fields(string line)
        {
            var r = new List<string>(); var cur = new System.Text.StringBuilder(); bool q = false;
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (q) { if (c == '"') { if (i + 1 < line.Length && line[i + 1] == '"') { cur.Append('"'); i++; } else q = false; } else cur.Append(c); }
                else if (c == '"') q = true; else if (c == ',') { r.Add(cur.ToString()); cur.Clear(); } else cur.Append(c);
            }
            r.Add(cur.ToString()); return r;
        }
        static void Ensure() { if (display == null) Load(PoliceRadioScript.Instance ? PoliceRadioScript.Instance.csv : ""); }
        public static bool Has(string id) { Ensure(); return display.ContainsKey(id); }

        // the words on screen: the pieces joined ("Suspect heading north on..." + "Canton Road." -> "Suspect heading north on Canton Road.")
        public static string TextOf(params string[] ids)
        {
            Ensure(); var parts = new List<string>();
            foreach (var id in ids) if (id != null && display.TryGetValue(id, out var t)) parts.Add(t);
            if (parts.Count == 0) return "";
            for (int i = 0; i < parts.Count; i++)
            {
                var p = parts[i];
                if (i > 0) { p = p.TrimStart('.'); if (p.StartsWith("and")) p = p; }
                if (i + 1 < parts.Count) { p = p.TrimEnd('.'); if (p.EndsWith(",")) { } }
                parts[i] = p.Trim();
            }
            string s = string.Join(" ", parts).Replace(" ,", ",").Trim();
            if (!(s.EndsWith(".") || s.EndsWith("!") || s.EndsWith("?"))) s += ".";
            return s;
        }
        public static string Unit(int n) => "U" + Mathf.Clamp(n, 1, 6);

        // the street a road name is (its D id), or null (motorways and unnamed roads are not in the script)
        public static string StreetId(string osmName)
        {
            Ensure(); if (string.IsNullOrEmpty(osmName)) return null;
            if (streetOf.TryGetValue(osmName, out var id)) return id;
            // a name the script covers without its suffix ("Canton Road Northeast" -> "Canton Road")
            foreach (var suffix in new[] { " Northeast", " Northwest", " Southeast", " Southwest" }) if (osmName.EndsWith(suffix) && streetOf.TryGetValue(osmName.Substring(0, osmName.Length - suffix.Length), out id)) return id;
            return null;
        }
        // the street at a road network node: its own name if the script has it, else the nearest node within 250 m on a street the script has
        public static string RoadId(RoadNet net, int node)
        {
            if (net == null || node < 0) return null;
            var id = StreetId(net.RoadNames[net.Road[node]]); if (id != null) return id;
            int best = -1; float bd = float.MaxValue;
            foreach (int i in net.Near(net.P[node], 250)) { if (StreetId(net.RoadNames[net.Road[i]]) == null) continue; float d = (net.P[i] - net.P[node]).sqrMagnitude; if (d < bd) { bd = d; best = i; } }
            return best >= 0 ? StreetId(net.RoadNames[net.Road[best]]) : null;
        }
        // the street at a point: the nearest one the script names (off-road: along 'ahead' first, the nearest street ahead)
        public static string PlaceId(RoadNet net, Vector3 p, Vector3 ahead = default)
        {
            if (net == null) return null; var dir = ahead; dir.y = 0;
            if (dir.sqrMagnitude > 1f && net.OffNet(p) > 14f)
            {
                dir.Normalize();
                for (float s = 20f; s <= 400f; s += 20f) { int n = net.Nearest(p + dir * s, out float d, 40f); if (n >= 0 && d < net.Half[n] + 8f) { var id = RoadId(net, n); if (id != null) return id; } }
            }
            int node = net.Nearest(p, out _, 300); return RoadId(net, node);
        }
        // "Suspect heading north on Canton Road": the compass point of a heading (world z is north)
        public static string HeadingPhrase(Vector3 heading)
        {
            heading.y = 0; if (heading.sqrMagnitude < 1f) return null;
            float a = Mathf.Atan2(heading.x, heading.z) * Mathf.Rad2Deg; a = Mathf.Repeat(a, 360f);
            return a < 45f || a >= 315f ? "P11" : a < 135f ? "P13" : a < 225f ? "P12" : "P14";
        }
        // a line with a street in it; the street left out (and the line shown without it) when none is known
        public static string[] With(string phrase, string street, string ending = null) => street == null ? (ending == null ? new[] { phrase } : new[] { phrase, ending }) : ending == null ? new[] { phrase, street } : new[] { phrase, street, ending };

        public static void Speak(string[] ids, bool priority = false) { PoliceRadioVoice.Ensure(); PoliceRadioVoice.Instance?.Enqueue(ids, priority); }
        public static void Clear() { PoliceRadioVoice.Instance?.Flush(); }
    }


    // Racer's PoliceRadioVoice, its clips from Dan's folders (read with UnityWebRequest-free WAV decoding: the studio app writes 16-bit mono WAV)
    public sealed class PoliceRadioVoice : MonoBehaviour
    {
        const int Rate = 44100; const float Gap = .07f, Pad = .025f, Threshold = .012f;
        public static PoliceRadioVoice Instance { get; private set; }
        public static float Duck { get; private set; }
        public static bool Speaking => Instance && Instance.source && Instance.source.isPlaying;
        public static readonly List<string> Played = new();   // checks: one entry per line spoken: time, the IDs, the length, where each piece came from
        struct Item { public string[] ids; public bool priority; public float at; }
        readonly Queue<Item> queue = new(); AudioSource source; bool currentPriority; float busyUntil; readonly Dictionary<string, List<float[]>> cache = new(); readonly Dictionary<string, float> lastSpoken = new();
        readonly System.Random rng = new(7); string lastSources = "";
        public static void Ensure() { if (Instance) return; var g = new GameObject("Police radio voice"); DontDestroyOnLoad(g); g.AddComponent<PoliceRadioVoice>(); }
        void Awake() { Instance = this; source = gameObject.AddComponent<AudioSource>(); source.playOnAwake = false; source.spatialBlend = 0; source.priority = 8; SoundMix.Track(source, .9f, SoundMix.Kind.Effects); }
        void OnDestroy() { if (Instance == this) Instance = null; Duck = 0; }
        public void Flush() { queue.Clear(); if (source) source.Stop(); busyUntil = 0; }
        public void ClearCache() => cache.Clear();
        public void Enqueue(string[] ids, bool priority)
        {
            if (ids == null || ids.Length == 0) return; string key = string.Join("+", ids);
            if (lastSpoken.TryGetValue(key, out var t) && Time.unscaledTime - t < 6) return; lastSpoken[key] = Time.unscaledTime;
            if (priority) { var keep = queue.Where(q => q.priority).ToList(); queue.Clear(); foreach (var k in keep) queue.Enqueue(k); if (source.isPlaying && !currentPriority) { source.Stop(); busyUntil = 0; } }
            else { while (queue.Count >= 3) queue.Dequeue(); }
            queue.Enqueue(new Item { ids = ids, priority = priority, at = Time.unscaledTime });
        }
        void Update()
        {
            float target = source.isPlaying ? 1 : 0; Duck = Mathf.MoveTowards(Duck, target, Time.unscaledDeltaTime * (target > Duck ? 6f : 1.5f));
            if (source.isPlaying || Time.unscaledTime < busyUntil || queue.Count == 0) return;
            var item = queue.Dequeue(); if (Time.unscaledTime - item.at > 8 && !item.priority) return;
            var clip = Build(item.ids); if (!clip) { Played.Add($"{Time.unscaledTime:F1}s {string.Join("+", item.ids)} text only [{lastSources}]"); return; }
            Played.Add($"{Time.unscaledTime:F1}s {string.Join("+", item.ids)} {clip.length:F2}s [{lastSources}]"); currentPriority = item.priority; source.clip = clip; source.Play(); busyUntil = Time.unscaledTime + clip.length + .25f;
        }

        // ---- the clips: the drop-in folder first, then the build's own copy of SourceArt/Audio/PoliceRadio
        public static string DropInOverride;   // checks: their own drop-in folder (never Dan's)
        public static string DropIn => DropInOverride ?? Path.Combine(Application.persistentDataPath, "Sounds", "PoliceRadio");
        public static string BuiltIn => Path.Combine(Application.isEditor ? Path.GetFullPath(Path.Combine(Application.dataPath, "..", "SourceArt", "Audio")) : Application.dataPath, "PoliceRadio");
        List<float[]> Takes(string id, out string from)
        {
            from = "none";
            if (cache.TryGetValue(id, out var list)) { from = list.Count > 0 ? "cached" : "none"; return list; }
            list = new List<float[]>();
            foreach (var folder in new[] { DropIn, BuiltIn })
            {
                if (!Directory.Exists(folder)) continue;
                foreach (var name in new[] { id, id + "a", id + "b", id + "c", id + "d" })
                {
                    var path = Path.Combine(folder, name + ".wav"); if (!File.Exists(path)) continue;
                    var s = Wav(path); if (s != null && s.Length > 0) list.Add(s);
                }
                if (list.Count > 0) { from = folder == DropIn ? "drop-in" : "built-in"; break; }   // the drop-in folder wins
            }
            cache[id] = list; return list;
        }
        // a PCM WAV file (8/16/24/32-bit, any rate and channels) as mono samples at Rate, trimmed of its silence
        static float[] Wav(string path)
        {
            try
            {
                var b = File.ReadAllBytes(path); if (b.Length < 44 || b[0] != 'R' || b[8] != 'W') return null;
                int ch = 1, rate = Rate, bits = 16, fmt = 1, pos = 12; int dataAt = -1, dataLen = 0;
                while (pos + 8 <= b.Length)
                {
                    string tag = System.Text.Encoding.ASCII.GetString(b, pos, 4); int len = BitConverter.ToInt32(b, pos + 4);
                    if (tag == "fmt ") { fmt = BitConverter.ToInt16(b, pos + 8); ch = BitConverter.ToInt16(b, pos + 10); rate = BitConverter.ToInt32(b, pos + 12); bits = BitConverter.ToInt16(b, pos + 22); }
                    else if (tag == "data") { dataAt = pos + 8; dataLen = Mathf.Min(len, b.Length - dataAt); break; }
                    pos += 8 + len + (len & 1);
                }
                if (dataAt < 0 || ch < 1) return null;
                int bytes = bits / 8, frames = dataLen / (bytes * ch); var mono = new float[frames];
                for (int i = 0; i < frames; i++)
                {
                    float s = 0;
                    for (int c = 0; c < ch; c++)
                    {
                        int o = dataAt + (i * ch + c) * bytes;
                        s += fmt == 3 && bits == 32 ? BitConverter.ToSingle(b, o) : bits == 16 ? BitConverter.ToInt16(b, o) / 32768f : bits == 8 ? (b[o] - 128) / 128f : bits == 24 ? ((b[o] | b[o + 1] << 8 | (sbyte)b[o + 2] << 16) / 8388608f) : BitConverter.ToInt32(b, o) / 2147483648f;
                    }
                    mono[i] = s / ch;
                }
                if (rate != Rate) { int n = Mathf.RoundToInt(mono.Length * (float)Rate / rate); var r = new float[n]; for (int i = 0; i < n; i++) { float p = i * (float)rate / Rate; int a0 = Mathf.Min(mono.Length - 1, (int)p); int a1 = Mathf.Min(mono.Length - 1, a0 + 1); r[i] = Mathf.Lerp(mono[a0], mono[a1], p - a0); } mono = r; }
                int first = 0, last = mono.Length - 1; while (first < last && Mathf.Abs(mono[first]) < Threshold) first++; while (last > first && Mathf.Abs(mono[last]) < Threshold) last--;
                first = Mathf.Max(0, first - (int)(Pad * Rate)); last = Mathf.Min(mono.Length - 1, last + (int)(Pad * Rate)); var trimmed = new float[Mathf.Max(0, last - first + 1)]; Array.Copy(mono, first, trimmed, 0, trimmed.Length); return trimmed;
            }
            catch (Exception e) { Debug.LogWarning("[PoliceRadio] " + path + ": " + e.Message); return null; }
        }
        AudioClip Build(string[] ids)
        {
            var parts = new List<float[]>(); int total = 0; var sources = new List<string>();
            foreach (var id in ids)
            {
                var takes = Takes(id, out var from); sources.Add(id + ":" + from); if (takes.Count == 0) continue;
                var s = takes[rng.Next(takes.Count)]; parts.Add(s); total += s.Length + (int)(Gap * Rate);
            }
            lastSources = string.Join(",", sources);
            if (parts.Count == 0) return null;
            var line = new float[total]; int at = 0; foreach (var p in parts) { Array.Copy(p, 0, line, at, p.Length); at += p.Length + (int)(Gap * Rate); }
            var shaped = Radio(line); var clip = AudioClip.Create("Police radio " + string.Join("+", ids), shaped.Length, 1, Rate, false); clip.SetData(shaped, 0); return clip;
        }
        // Racer's radio effect: high-pass 300 Hz, low-pass 3,400 Hz, a light tanh distortion, a click and a burst of static at each end
        float[] Radio(float[] x)
        {
            Biquad(x, 300, true); Biquad(x, 3400, false);
            float peak = 0; foreach (var v in x) peak = Mathf.Max(peak, Mathf.Abs(v)); float gain = peak > 1e-4f ? .8f / peak : 1; float drive = 1.7f, norm = (float)Math.Tanh(drive);
            for (int i = 0; i < x.Length; i++) x[i] = (float)Math.Tanh(drive * x[i] * gain) / norm * .85f;
            int lead = (int)(.16f * Rate), tail = (int)(.12f * Rate); var y = new float[x.Length + lead + tail]; Array.Copy(x, 0, y, lead, x.Length);
            Burst(y, 0, lead, .5f); Burst(y, lead + x.Length, tail, .4f); return y;
        }
        void Burst(float[] y, int start, int length, float click)
        {
            var noise = new float[length]; for (int i = 0; i < length; i++) noise[i] = (float)(rng.NextDouble() * 2 - 1); Biquad(noise, 300, true); Biquad(noise, 3600, false);
            for (int i = 0; i < length; i++)
            {
                float t = i / (float)length, env = Mathf.Clamp01(1 - t) * .22f; float ck = i < (int)(.004f * Rate) ? click * (1 - i / (.004f * Rate)) * (i % 2 == 0 ? 1 : -1) : 0;
                y[start + i] = Mathf.Clamp(y[start + i] + noise[i] * env * 2.4f + ck, -1, 1);
            }
        }
        static void Biquad(float[] x, float hz, bool high)
        {
            double w = 2 * Math.PI * hz / Rate, cos = Math.Cos(w), alpha = Math.Sin(w) / (2 * .7071), a0 = 1 + alpha, a1 = -2 * cos, a2 = 1 - alpha;
            double b0 = high ? (1 + cos) / 2 : (1 - cos) / 2, b1 = high ? -(1 + cos) : 1 - cos, b2 = b0; b0 /= a0; b1 /= a0; b2 /= a0; a1 /= a0; a2 /= a0; double x1 = 0, x2 = 0, y1 = 0, y2 = 0;
            for (int i = 0; i < x.Length; i++) { double v = x[i], o = b0 * v + b1 * x1 + b2 * x2 - a1 * y1 - a2 * y2; x2 = x1; x1 = v; y2 = y1; y1 = o; x[i] = (float)o; }
        }
    }
}
