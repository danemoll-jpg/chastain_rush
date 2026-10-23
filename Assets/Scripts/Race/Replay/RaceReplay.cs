using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using UnityEngine;

namespace ChastainRush
{
    // Round 57 (F, Dan: "can we go ahead and create race replays? Would want it to automatically replace top 1 laps and races with the ability to save
    // any race you want"; 2026-10-20: "be able to watch your race you just ran and move the camera around differently"). New here (Racer has none).
    // A replay is what ReplayRecorder sampled during one race: every racer's pose, speed, steering and pedal 25 times a second, the traffic's poses
    // 10 times a second, and the world events the race caused (a panel of a fence / mailbox / hydrant / sign / pump / barricade broken, a tree
    // snapped, a manhole plume, a switch gate opened, a rider's wave or celebration, laps and finishes). ReplayViewer plays it back.
    // Files: <the records' folder>/Replays/ (beside race_records.json): best/<track key>.lap.crreplay and .race.crreplay (the #1 Top 10 lap and race
    // per track, direction and laps, as the boards key them; a new #1 replaces it) and saved/<track> <direction> <date time>.crreplay (kept by the
    // player). A gzip'd binary: a header (enough for the REPLAYS page), then the samples.
    public sealed class ReplayData
    {
        public const int Version = 2;   // 2: each frame its own time
        public string build = "", routeFile = "", routeName = "", recordName = "", direction = "", category = "", routeHash = "", date = "", label = "";
        public int laps;
        public string kind = "race";          // "race", "lap" (the #1 lap: only its window plays), "saved"
        public float t0, t1;                  // the window that plays (race clock seconds; the countdown is negative)
        public float bestTime;                // the #1's time (lap or race), 0 for a saved one
        public int focus;                     // the racer the cameras start on (the player)
        public readonly List<Racer> racers = new List<Racer>();
        public readonly List<Track> traffic = new List<Track>();
        public readonly List<Event> events = new List<Event>();

        public struct Frame { public float t; public Vector3 p; public Quaternion q; public float speed, steer, throttle; public byte wheels; }   // t: its race clock time (each its own: the clock is not always even at GO)
        public sealed class Track { public int index; public float start, dt; public readonly List<Frame> frames = new List<Frame>(); }
        public sealed class Racer
        {
            public string name = "", vehicle = "original", rider = ""; public bool hasPaint, player; public Color paint = Color.white; public int seat;
            public readonly Track track = new Track();
            public readonly List<(int lap, float start, float end)> laps = new List<(int, float, float)>();
            public float finishedAt = -1f, total = -1f;
        }
        public enum Kind : byte { Panel = 1, Tree = 2, Plume = 3, Gate = 4, Gesture = 5, Restore = 6 }   // Restore: a chunk put back on its own (the Elaine barricade once clear)
        public struct Event { public float t; public Kind kind; public int a, b; public Vector3 p, v; public float f; }

        public float Duration => t1 - t0;

        // a track's pose at time t (between samples: position lerped, rotation slerped; held at its ends)
        public static Frame At(Track tr, float t)
        {
            var f = tr.frames; if (f.Count == 0) return default;
            if (t <= f[0].t) return f[0]; if (t >= f[f.Count - 1].t) return f[f.Count - 1];
            int lo = 0, hi = f.Count - 1; while (hi - lo > 1) { int mid = (lo + hi) >> 1; if (f[mid].t <= t) lo = mid; else hi = mid; }
            int i = lo; var a = f[i]; var b = f[i + 1]; float k = b.t > a.t ? Mathf.Clamp01((t - a.t) / (b.t - a.t)) : 1f;
            return new Frame { t = t, p = Vector3.Lerp(a.p, b.p, k), q = Quaternion.Slerp(a.q, b.q, k), speed = Mathf.Lerp(a.speed, b.speed, k), steer = Mathf.Lerp(a.steer, b.steer, k),
                               throttle = Mathf.Lerp(a.throttle, b.throttle, k), wheels = k < 0.5f ? a.wheels : b.wheels };
        }

        // ---------------------------------------------------------------- the file
        static readonly byte[] Magic = { (byte)'C', (byte)'R', (byte)'R', (byte)'P' };
        public void Write(string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            string tmp = path + ".tmp";
            using (var fs = File.Create(tmp))
            using (var gz = new GZipStream(fs, System.IO.Compression.CompressionLevel.Optimal))
            using (var w = new BinaryWriter(gz))
            {
                w.Write(Magic); w.Write(Version);
                WriteHeader(w);
                w.Write(racers.Count);
                foreach (var r in racers)
                {
                    w.Write(r.name); w.Write(r.vehicle); w.Write(r.rider); w.Write(r.hasPaint); w.Write(r.paint.r); w.Write(r.paint.g); w.Write(r.paint.b); w.Write(r.player); w.Write(r.seat);
                    w.Write(r.finishedAt); w.Write(r.total);
                    w.Write(r.laps.Count); foreach (var (lap, s, e) in r.laps) { w.Write(lap); w.Write(s); w.Write(e); }
                    WriteTrack(w, r.track, true);
                }
                w.Write(traffic.Count); foreach (var tr in traffic) WriteTrack(w, tr, false);
                w.Write(events.Count);
                foreach (var e in events) { w.Write(e.t); w.Write((byte)e.kind); w.Write(e.a); w.Write(e.b); W3(w, e.p); W3(w, e.v); w.Write(e.f); }
            }
            if (File.Exists(path)) File.Delete(path);
            File.Move(tmp, path);
        }
        void WriteHeader(BinaryWriter w)
        {
            w.Write(build); w.Write(routeFile); w.Write(routeName); w.Write(recordName); w.Write(direction); w.Write(category); w.Write(routeHash); w.Write(date); w.Write(label);
            w.Write(laps); w.Write(kind); w.Write(t0); w.Write(t1); w.Write(bestTime); w.Write(focus);
        }
        void ReadHeader(BinaryReader r)
        {
            build = r.ReadString(); routeFile = r.ReadString(); routeName = r.ReadString(); recordName = r.ReadString(); direction = r.ReadString(); category = r.ReadString();
            routeHash = r.ReadString(); date = r.ReadString(); label = r.ReadString();
            laps = r.ReadInt32(); kind = r.ReadString(); t0 = r.ReadSingle(); t1 = r.ReadSingle(); bestTime = r.ReadSingle(); focus = r.ReadInt32();
        }
        static void W3(BinaryWriter w, Vector3 v) { w.Write(v.x); w.Write(v.y); w.Write(v.z); }
        static Vector3 R3(BinaryReader r) => new Vector3(r.ReadSingle(), r.ReadSingle(), r.ReadSingle());
        // a pose: position as floats, rotation as four shorts, speed in cm/s, steering, pedal and wheels down in a byte each
        static void WriteTrack(BinaryWriter w, Track tr, bool full)
        {
            w.Write(tr.index); w.Write(tr.start); w.Write(tr.dt); w.Write(tr.frames.Count);
            foreach (var f in tr.frames)
            {
                w.Write(f.t); W3(w, f.p); var q = f.q.normalized; w.Write((short)(q.x * 32767f)); w.Write((short)(q.y * 32767f)); w.Write((short)(q.z * 32767f)); w.Write((short)(q.w * 32767f));
                w.Write((short)Mathf.Clamp(f.speed * 100f, -32767f, 32767f));
                if (full) { w.Write((sbyte)Mathf.RoundToInt(Mathf.Clamp(f.steer, -1f, 1f) * 127f)); w.Write((byte)Mathf.RoundToInt(Mathf.Clamp01(f.throttle) * 255f)); w.Write(f.wheels); }
            }
        }
        static void ReadTrack(BinaryReader r, Track tr, bool full)
        {
            tr.index = r.ReadInt32(); tr.start = r.ReadSingle(); tr.dt = r.ReadSingle(); int n = r.ReadInt32();
            for (int i = 0; i < n; i++)
            {
                var f = new Frame { t = r.ReadSingle(), p = R3(r) };
                f.q = new Quaternion(r.ReadInt16() / 32767f, r.ReadInt16() / 32767f, r.ReadInt16() / 32767f, r.ReadInt16() / 32767f).normalized;
                f.speed = r.ReadInt16() / 100f;
                if (full) { f.steer = r.ReadSByte() / 127f; f.throttle = r.ReadByte() / 255f; f.wheels = r.ReadByte(); } else f.wheels = 4;
                tr.frames.Add(f);
            }
        }
        public static ReplayData Read(string path, bool headerOnly = false)
        {
            using (var fs = File.OpenRead(path))
            using (var gz = new GZipStream(fs, CompressionMode.Decompress))
            using (var r = new BinaryReader(gz))
            {
                var m = r.ReadBytes(4); if (!m.SequenceEqual(Magic)) throw new InvalidDataException("not a replay");
                int ver = r.ReadInt32(); if (ver != Version) throw new InvalidDataException("replay version " + ver);
                var d = new ReplayData(); d.ReadHeader(r);
                if (headerOnly) return d;
                int nr = r.ReadInt32();
                for (int i = 0; i < nr; i++)
                {
                    var x = new Racer { name = r.ReadString(), vehicle = r.ReadString(), rider = r.ReadString(), hasPaint = r.ReadBoolean() };
                    x.paint = new Color(r.ReadSingle(), r.ReadSingle(), r.ReadSingle()); x.player = r.ReadBoolean(); x.seat = r.ReadInt32();
                    x.finishedAt = r.ReadSingle(); x.total = r.ReadSingle();
                    int nl = r.ReadInt32(); for (int k = 0; k < nl; k++) x.laps.Add((r.ReadInt32(), r.ReadSingle(), r.ReadSingle()));
                    ReadTrack(r, x.track, true); d.racers.Add(x);
                }
                int nt = r.ReadInt32(); for (int i = 0; i < nt; i++) { var tr = new Track(); ReadTrack(r, tr, false); d.traffic.Add(tr); }
                int ne = r.ReadInt32();
                for (int i = 0; i < ne; i++) d.events.Add(new Event { t = r.ReadSingle(), kind = (Kind)r.ReadByte(), a = r.ReadInt32(), b = r.ReadInt32(), p = R3(r), v = R3(r), f = r.ReadSingle() });
                return d;
            }
        }

        // the same race trimmed to [a, b] (the #1 lap's replay: the lap with a little either side, every racer as they were then)
        public ReplayData Window(float a, float b, string asKind)
        {
            var c = new ReplayData { build = build, routeFile = routeFile, routeName = routeName, recordName = recordName, direction = direction, category = category, routeHash = routeHash,
                                     date = date, label = label, laps = laps, kind = asKind, t0 = Mathf.Max(t0, a), t1 = Mathf.Min(t1, b), bestTime = bestTime, focus = focus };
            float pad = 1f;
            Track Cut(Track src)
            {
                var o = new Track { index = src.index, dt = src.dt };
                o.frames.AddRange(src.frames.Where(f => f.t >= c.t0 - pad && f.t <= c.t1 + pad)); o.start = o.frames.Count > 0 ? o.frames[0].t : src.start;
                return o;
            }
            foreach (var r in racers)
            {
                var x = new Racer { name = r.name, vehicle = r.vehicle, rider = r.rider, hasPaint = r.hasPaint, paint = r.paint, player = r.player, seat = r.seat, finishedAt = r.finishedAt, total = r.total };
                x.laps.AddRange(r.laps); var t = Cut(r.track); x.track.index = t.index; x.track.start = t.start; x.track.dt = t.dt; x.track.frames.AddRange(t.frames);
                c.racers.Add(x);
            }
            foreach (var tr in traffic) c.traffic.Add(Cut(tr));
            c.events.AddRange(events);   // every event: those before the window set the world as it was when it opens
            return c;
        }

        // ---------------------------------------------------------------- where they live
        public static string Folder => Path.Combine(Path.GetDirectoryName(RaceController.RecordsPath()), "Replays");
        public static string BestFolder => Path.Combine(Folder, "best");
        public static string SavedFolder => Path.Combine(Folder, "saved");
        public static string Safe(string s) { foreach (var c in Path.GetInvalidFileNameChars()) s = s.Replace(c, '_'); return s.Replace('|', '_').Replace('/', '_'); }
        public static string BestPath(string key, bool race) => Path.Combine(BestFolder, Safe(key) + (race ? ".race" : ".lap") + ".crreplay");
        public static string BuildId => Application.buildGUID ?? "";
        public static string Hash(string text)
        {
            unchecked { ulong h = 1469598103934665603UL; foreach (char ch in text ?? "") { h ^= ch; h *= 1099511628211UL; } return h.ToString("x16"); }
        }

        // the files there, newest first (headers only; a file that cannot be read is skipped)
        public static List<(string path, ReplayData head)> List(bool best)
        {
            var list = new List<(string, ReplayData)>();
            var dir = best ? BestFolder : SavedFolder;
            if (!Directory.Exists(dir)) return list;
            foreach (var f in Directory.GetFiles(dir, "*.crreplay"))
                try { list.Add((f, Read(f, true))); } catch (Exception e) { Debug.LogWarning($"[Replay] {Path.GetFileName(f)}: {e.Message}"); }
            return list.OrderByDescending(x => x.Item2.date).ToList();
        }
        // what the page says about a replay recorded on another build or another version of its course
        public string Staleness(string currentRouteHash)
        {
            if (!string.IsNullOrEmpty(currentRouteHash) && routeHash != currentRouteHash) return "the course has changed since: the replay may not fit it";
            if (build != BuildId) return "recorded on an older build: the world may differ";
            return "";
        }
        // Round 62: by the name its records are kept under now (a renamed route: "Noonday Creek" is shown as the Noonday Creek Extension), else as saved
        public string Title => $"{(RaceController.RecordTitles.TryGetValue(recordName ?? "", out var now) ? now : string.IsNullOrEmpty(routeName) ? RaceController.RecordTitle(recordName) : routeName)} {(direction.StartsWith("reverse") ? "reverse" : "forward")}{(direction.Contains("-") ? " (" + direction.Substring(direction.IndexOf('-') + 1) + ")" : "")}";
        public string When { get { return DateTime.TryParse(date, null, System.Globalization.DateTimeStyles.RoundtripKind, out var d) ? d.ToLocalTime().ToString("MMM d, yyyy h:mm tt") : date; } }
    }
}
