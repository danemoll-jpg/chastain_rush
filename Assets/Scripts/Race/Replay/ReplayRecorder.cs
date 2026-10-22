using System;
using System.Collections.Generic;
using System.Linq;
using Racer;
using UnityEngine;

namespace ChastainRush
{
    // Round 57 (F): records every race (RaceData) from the countdown until every racer has finished, the AI are classified, 75 s after the player
    // finished, or the race is left. Racers 25 times a second, traffic 10 times; the world events as they happen. Then it keeps the replay of a new
    // #1 Top 10 lap and race (ReplayData.BestPath) and writes the one the player asked to save. RaceController calls Begin / Lap / Finished.
    [DefaultExecutionOrder(200)]   // after RaceController's FixedUpdate (its clock)
    public sealed class ReplayRecorder : MonoBehaviour
    {
        public static ReplayRecorder Instance { get; private set; }
        public const float RacerHz = 25f, TrafficHz = 10f, AfterPlayer = 75f, MaxSeconds = 1800f;
        RaceController race;
        public ReplayData Current { get; private set; }   // being recorded
        public ReplayData Last { get; private set; }      // the last race's, once its recording stopped (the finish screen's WATCH / SAVE)
        public bool Recording => Current != null;
        readonly List<RaceEntrant> who = new List<RaceEntrant>();
        readonly List<RiderGestures.Kind> gestureWas = new List<RiderGestures.Kind>();
        int step; float lastT = float.NegativeInfinity;
        (float a, float b)? bestLap; bool bestRace, saveAsked;
        public string LastSaved { get; private set; } = "";
        public static string Note = "";   // checks: what the last stop kept

        void Awake() { Instance = this; race = GetComponent<RaceController>(); }
        void OnEnable()
        {
            FenceChunk.PanelBrokenBy += OnPanel; FenceChunk.Restored += OnRestored; TreeTrunks.SnappedTree += OnTree; ManholePlume.Erupted += OnPlume; RaceBarriers.GateOpened += OnGate;
        }
        void OnDisable()
        {
            FenceChunk.PanelBrokenBy -= OnPanel; FenceChunk.Restored -= OnRestored; TreeTrunks.SnappedTree -= OnTree; ManholePlume.Erupted -= OnPlume; RaceBarriers.GateOpened -= OnGate;
        }
        void OnDestroy() { if (Instance == this) Instance = null; }
        void OnApplicationQuit() { if (Recording) Stop("the game closed"); }

        float Now => race.State == RaceController.Stage.Countdown ? -race.CountdownRemaining : race.RaceClock;

        // the race starts (RaceController.StartRace, its racers on the grid)
        public void Begin(string routeFile, string routeJson, string category)
        {
            if (Recording) Stop("a new race");
            var r = race.Route; var d = new ReplayData
            {
                build = ReplayData.BuildId, routeFile = routeFile ?? "", routeName = r.Name, recordName = r.RecordName, direction = race.RecordDirectionTag, category = category ?? "",
                routeHash = ReplayData.Hash(routeJson), date = DateTime.UtcNow.ToString("o"), laps = r.Laps, kind = "race", t0 = -3.2f, focus = 0
            };
            who.Clear(); gestureWas.Clear();
            for (int i = 0; i < race.Entrants.Count; i++)
            {
                var e = race.Entrants[i]; if (!e.Car) continue;
                var cfg = e.Car.GetComponent<VehicleConfiguration>();
                var x = new ReplayData.Racer { name = e.Name, vehicle = cfg ? cfg.profileId : "original", player = e.IsPlayer, seat = e.Seat };
                var look = cfg && cfg.riderLook != null ? cfg.riderLook : RiderLook.Player; x.rider = look != null ? JsonUtility.ToJson(look) : "";
                if (cfg && cfg.Paint.HasValue) { x.hasPaint = true; x.paint = cfg.Paint.Value; }
                x.track.index = d.racers.Count; x.track.dt = 1f / RacerHz; x.track.start = float.NaN;
                if (e.IsPlayer) d.focus = d.racers.Count;
                d.racers.Add(x); who.Add(e); var g = e.Car.GetComponent<RiderGestures>(); gestureWas.Add(g ? g.Current : RiderGestures.Kind.None);
            }
            var tf = Traffic.Instance;
            if (tf) for (int i = 0; i < tf.Cars.Count; i++) d.traffic.Add(new ReplayData.Track { index = i, dt = 1f / TrafficHz, start = float.NaN });
            Current = d; step = 0; lastT = float.NegativeInfinity; bestLap = null; bestRace = false; saveAsked = false; LastSaved = "";
        }

        void FixedUpdate()
        {
            var d = Current; if (d == null) return;
            if (race.State == RaceController.Stage.Free) { Stop("the race was left"); return; }
            float t = Now; if (t <= lastT) return; lastT = t;
            int racerEvery = Mathf.Max(1, Mathf.RoundToInt(1f / (RacerHz * Time.fixedDeltaTime))), trafficEvery = Mathf.Max(1, Mathf.RoundToInt(1f / (TrafficHz * Time.fixedDeltaTime)));
            if (step % racerEvery == 0)
                for (int i = 0; i < who.Count; i++)
                {
                    var e = who[i]; var x = d.racers[i]; if (!e.Car) continue;
                    if (float.IsNaN(x.track.start)) { x.track.start = t; x.track.dt = racerEvery * Time.fixedDeltaTime; }
                    float throttle = e.Driver ? e.Driver.LastThrottle : PedalOf(e.Car);
                    var c = e.Car; x.track.frames.Add(new ReplayData.Frame { t = t, p = c.Body.position, q = c.Body.rotation, speed = c.ForwardSpeed, steer = c.VisualSteering, throttle = throttle, wheels = (byte)c.GroundedWheels });
                    var g = c.GetComponent<RiderGestures>();
                    if (g && g.Current != gestureWas[i]) { if (g.Current != RiderGestures.Kind.None) d.events.Add(new ReplayData.Event { t = t, kind = ReplayData.Kind.Gesture, a = i, b = (int)g.Current }); gestureWas[i] = g.Current; }
                }
            var tf = Traffic.Instance;
            if (tf && step % trafficEvery == 0)
                for (int i = 0; i < d.traffic.Count && i < tf.Cars.Count; i++)
                {
                    var car = tf.Cars[i] ? tf.Cars[i].Car : null; if (!car) continue; var tr = d.traffic[i];
                    if (float.IsNaN(tr.start)) { tr.start = t; tr.dt = trafficEvery * Time.fixedDeltaTime; }
                    // a recycled car jumps (Traffic moves it where nobody sees): the frames in between are its last pose, then the new one
                    tr.frames.Add(new ReplayData.Frame { t = t, p = car.Body.position, q = car.Body.rotation, speed = car.ForwardSpeed });
                }
            step++;
            d.t1 = t;
            // when to stop: everyone has finished (or been classified), 75 s after the player, 30 minutes
            bool allDone = race.Entrants.All(e => e.Finished);
            float lastReal = race.Entrants.Where(e => e.Finished && !e.Estimated).Select(e => e.FinishedAt - e.PenaltyTotal).DefaultIfEmpty(t).Max();   // the clock when they crossed (FinishedAt counts penalties)
            if (race.State == RaceController.Stage.Finished && (allDone ? t > lastReal + 3f : t - race.PlayerFinishedAt > AfterPlayer)) Stop(allDone ? "every racer finished" : "75 s after the player");
            else if (t - d.t0 > MaxSeconds) Stop("30 minutes");
        }

        // RaceController: a lap counted (rank: the player's lap on the Top 10, 1 = a new #1)
        public void Lap(RaceEntrant e, int lapNo, float start, float end, int rank)
        {
            var d = Current; if (d == null) return; int i = who.IndexOf(e); if (i < 0) return;
            d.racers[i].laps.Add((lapNo, start, end));
            if (e.IsPlayer && rank == 1) { bestLap = (start, end); d.bestTime = end - start; }
        }
        public void Finished(RaceEntrant e, int rank)
        {
            var d = Current; if (d == null) return; int i = who.IndexOf(e); if (i < 0) return;
            d.racers[i].finishedAt = e.FinishedAt; d.racers[i].total = e.FinalTotal;
            if (e.IsPlayer && rank == 1) bestRace = true;
        }

        static float PedalOf(ArcadeVehicle c) { var vi = c.GetComponent<VehicleInput>(); return vi && vi.enabled ? Mathf.Max(0f, vi.Throttle) : 0f; }
        void Add(ReplayData.Event ev) { var d = Current; if (d == null || race.State == RaceController.Stage.Free || ReplayViewer.Active) return; ev.t = Now; d.events.Add(ev); }
        // the panel's own middle in the world (a chunk's object may stand at the origin: its mesh is in world coordinates)
        void OnPanel(FenceChunk c, int panel, ArcadeVehicle car) => Add(new ReplayData.Event { kind = ReplayData.Kind.Panel, a = panel, b = (int)c.sound, p = c.transform.TransformPoint(c.panels[panel].centre), v = car ? car.Body.linearVelocity : Vector3.zero });
        void OnRestored(FenceChunk c) { if (c.panels != null && c.panels.Length > 0) Add(new ReplayData.Event { kind = ReplayData.Kind.Restore, a = 0, p = c.transform.TransformPoint(c.panels[0].centre) }); }
        void OnTree(int tree, Vector3 dir) => Add(new ReplayData.Event { kind = ReplayData.Kind.Tree, a = tree, v = dir });
        void OnPlume(ManholePlume m, float seconds) => Add(new ReplayData.Event { kind = ReplayData.Kind.Plume, p = m.transform.position, f = seconds });
        void OnGate(int gate) => Add(new ReplayData.Event { kind = ReplayData.Kind.Gate, a = gate });

        // the finish screen's SAVE REPLAY (and the viewer's): now if the recording has stopped, else when it does
        public string Save(ReplayData d = null)
        {
            if (d == null && Recording) { saveAsked = true; return "SAVED WHEN THE RACE ENDS"; }
            d = d ?? Last; if (d == null) return "NOTHING TO SAVE";
            try
            {
                var s = d.Window(d.t0, d.t1, "saved"); s.label = $"{s.Title} {DateTime.Now:yyyy-MM-dd HH.mm.ss}";
                string path = System.IO.Path.Combine(ReplayData.SavedFolder, ReplayData.Safe(s.label) + ".crreplay");
                if (d.kind == "saved" && System.IO.File.Exists(path)) return "ALREADY SAVED";
                s.Write(path); LastSaved = path; Debug.Log($"[Replay] saved {path} ({new System.IO.FileInfo(path).Length / 1024} KB)");
                return "REPLAY SAVED";
            }
            catch (Exception ex) { Debug.LogWarning("[Replay] save failed: " + ex.Message); return "SAVE FAILED"; }
        }

        public void Stop(string why)
        {
            var d = Current; if (d == null) return; Current = null; Last = d;
            var notes = new List<string>();
            try
            {
                if (bestRace && !race.NoRecords) { var path = ReplayData.BestPath(d.category, true); var c = d.Window(d.t0, d.t1, "race"); c.bestTime = d.racers[d.focus].total; c.Write(path); notes.Add("#1 race kept " + path); }
                if (bestLap.HasValue && !race.NoRecords) { var path = ReplayData.BestPath(RecordBoards.LapCategory(d.category), false); var (a, b) = bestLap.Value; var c = d.Window(a - 2f, b + 2f, "lap"); c.Write(path); notes.Add("#1 lap kept " + path); }
            }
            catch (Exception ex) { notes.Add("keeping failed: " + ex.Message); }
            if (saveAsked) notes.Add(Save(d));
            Note = $"stopped ({why}) at {d.t1:0.0} s; {d.racers.Sum(r => r.track.frames.Count)} racer frames, {d.traffic.Sum(r => r.frames.Count)} traffic frames, {d.events.Count} events" + (notes.Count > 0 ? "; " + string.Join("; ", notes) : "");
            Debug.Log("[Replay] " + Note);
        }
    }
}
