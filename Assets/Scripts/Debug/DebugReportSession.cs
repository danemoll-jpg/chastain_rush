using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading;
using UnityEngine;

namespace ChastainRush
{
    // Copied from Racer's DebugReportSession (portable, self-contained files; no player-save or publisher dependencies),
    // with these changes for Chastain Rush:
    //  - namespace ChastainRush; Racer's AtomicSave is folded in as a private helper (it was the only other class needed);
    //  - the Bug record drops Racer's race fields (course, direction, mode, lap, checkpoint, road progress, branch,
    //    vehicle profile, conditions, debug-movement flag) and gains this map's: road name and distance from its
    //    centreline, grounded wheels, vertical speed;
    //  - DisplayUnits.Mph is a plain conversion.
    public sealed class DebugReportSession
    {
        [Serializable] public sealed class Bug
        {
            public string id, timestamp, version, buildGuid, scene;
            public Vector3 position, rotation, vehiclePosition;
            public float heading, speedMps, verticalSpeedMps, roadDistance;
            public int groundedWheels;
            public string road, viewpoint, screenshot, comment;
            public string vehicle;   // Round 13: as Racer's report (Bug.vehicle): the vehicle driven
        }
        [Serializable] public sealed class Report
        {
            public int schemaVersion = 1;
            public string started, sessionId, closedAt;
            public bool closed, exported;
            public List<Bug> bugs = new();
        }
        public readonly string DirectoryPath;
        public readonly Report Data = new();
        public int Count => Data.bugs.Count;
        public string Id => Data.sessionId;
        public bool Closed => Data.closed;
        public DebugReportSession(string root)
        {
            Data.started = DateTimeOffset.Now.ToString("o");
            string name = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss-fff");
            DirectoryPath = Path.Combine(root, name + "_" + Guid.NewGuid().ToString("N").Substring(0, 6));
            Data.sessionId = Path.GetFileName(DirectoryPath);
            Directory.CreateDirectory(Path.Combine(DirectoryPath, "Screenshots"));
        }
        DebugReportSession(string directory, Report data) { DirectoryPath = directory; Data = data; }
        // Sessions survive quitting: only Export or an explicit new session ends one. The most recently
        // started session folder with reports is resumed if it is still OPEN. Closed, empty or unreadable
        // newest sessions mean a fresh session; no folder is ever modified here.
        public static DebugReportSession ResumeLatest(string root)
        {
            if (!Directory.Exists(root)) return null;
            var folders = Directory.GetDirectories(root);
            Array.Sort(folders, StringComparer.Ordinal);
            for (int i = folders.Length - 1; i >= 0; i--)
            {
                string json = Path.Combine(folders[i], "bugs.json");
                if (!File.Exists(json)) continue;
                try
                {
                    var data = JsonUtility.FromJson<Report>(File.ReadAllText(json));
                    if (data == null || data.bugs == null || data.sessionId != Path.GetFileName(folders[i])) return null;
                    return !data.closed && data.bugs.Count > 0 ? new DebugReportSession(folders[i], data) : null;
                }
                catch { return null; }
            }
            return null;
        }
        public string NextId => Closed ? throw new InvalidOperationException("This debug session is closed.") : "BUG-" + (Count + 1).ToString("000");
        public void Save(Bug bug)
        {
            if (Closed) throw new InvalidOperationException("Cannot append to a closed debug session.");
            if (!File.Exists(Path.Combine(DirectoryPath, bug.screenshot))) throw new IOException("The screenshot is missing; capture again.");
            if (!Data.bugs.Contains(bug)) Data.bugs.Add(bug);
            Write();
        }
        public void Write()
        {
            AtomicWrite(Path.Combine(DirectoryPath, "bugs.json"), JsonUtility.ToJson(Data, true));
            var md = new StringBuilder("# Chastain Rush bug report\n\nSession: " + Id + "\nStarted: " + Data.started
                + "\nStatus: " + (Closed ? "CLOSED" : "OPEN") + "\nReports: " + Count
                + (Closed ? "\nClosed: " + Data.closedAt + "\nExported: " + Data.exported : "") + "\n\n");
            foreach (var b in Data.bugs)
            {
                md.AppendLine("## " + b.id).AppendLine().AppendLine("Comment:");
                foreach (string line in (b.comment ?? "").Replace("\r", "").Split('\n')) md.AppendLine("> " + line);
                md.AppendLine().AppendLine("```text")
                  .AppendLine("Timestamp: " + b.timestamp)
                  .AppendLine("Position: " + Position(b.position)).AppendLine("Rotation: " + Position(b.rotation))
                  .AppendLine("Heading: " + F(b.heading) + " degrees").AppendLine("Viewpoint: " + b.viewpoint)
                  .AppendLine("Vehicle: " + (string.IsNullOrEmpty(b.vehicle) ? "(unknown)" : b.vehicle))
                  .AppendLine("Vehicle position: " + Position(b.vehiclePosition))
                  .AppendLine("Road: " + (string.IsNullOrEmpty(b.road) ? "(none within 40 m)" : b.road + ", " + F(b.roadDistance) + " m from its centerline"))
                  .AppendLine("Speed: " + F(b.speedMps) + " m/s / " + F(b.speedMps * 3600 / 1609.344) + " mph / vertical " + F(b.verticalSpeedMps) + " m/s")
                  .AppendLine("Wheels on the ground: " + b.groundedWheels)
                  .AppendLine("Version: " + b.version + " / Build: " + b.buildGuid).AppendLine("Scene: " + b.scene)
                  .AppendLine("```").AppendLine()
                  .AppendLine("![" + b.id + "](" + b.screenshot.Replace('\\', '/') + ")").AppendLine();
            }
            AtomicWrite(Path.Combine(DirectoryPath, "BUG_REPORT.md"), md.ToString());
        }
        static string F(double n) => n.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
        static string Position(Vector3 p) => "X=" + F(p.x) + ", Y=" + F(p.y) + ", Z=" + F(p.z);
        public string Export()
        {
            if (Closed) throw new InvalidOperationException("This session is already closed; its history is preserved.");
            string zip = DirectoryPath + "_" + Guid.NewGuid().ToString("N").Substring(0, 8) + ".zip";
            string temporary = zip + ".tmp";
            try
            {
                // Include the final lifecycle state in both the folder and portable archive.
                Data.closed = Data.exported = true; Data.closedAt = DateTimeOffset.Now.ToString("o");
                Write();
                ZipFile.CreateFromDirectory(DirectoryPath, temporary, System.IO.Compression.CompressionLevel.Optimal, true);
                File.Move(temporary, zip);
                return zip;
            }
            catch
            {
                Data.closed = Data.exported = false; Data.closedAt = null;
                try { Write(); } catch { /* Keep the original export error; in-memory reports remain open. */ }
                try { if (File.Exists(temporary)) File.Delete(temporary); } catch { /* Never delete a prior export. */ }
                throw;
            }
        }
        public void Close()
        {
            if (Closed) return;
            Data.closed = true; Data.closedAt = DateTimeOffset.Now.ToString("o");
            try { Write(); }
            catch { Data.closed = false; Data.closedAt = null; throw; }
        }

        // Racer's AtomicSave.Write: replace the file in one step, keeping the previous one as .bak, retrying when a
        // Windows indexer or scanner briefly holds it.
        static void AtomicWrite(string path, string contents)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            for (int attempt = 0; ; attempt++)
            {
                try
                {
                    string temp = path + ".tmp";
                    File.WriteAllText(temp, contents);
                    if (File.Exists(path)) File.Replace(temp, path, path + ".bak"); else File.Move(temp, path);
                    return;
                }
                catch (Exception error) when (error is IOException || error is UnauthorizedAccessException)
                {
                    if (attempt >= 4) throw;
                    Thread.Sleep(10 << attempt);
                }
            }
        }
    }
}
