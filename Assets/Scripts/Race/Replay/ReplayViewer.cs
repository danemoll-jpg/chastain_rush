using System;
using System.Collections.Generic;
using System.Linq;
using Racer;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ChastainRush
{
    // Round 57 (F): plays a ReplayData back. The live racers and traffic are frozen and hidden while it plays (and put back after); each racer is a
    // puppet, a copy of the player's car made like an AI car (RaceController.SpawnAi) but driven by the recording: kinematic, no contacts, its
    // wheels, steering, lean and rider from the recorded speed and steering (ArcadeVehicle.Puppet), its engine through OtherEngines. Traffic: the
    // live traffic cars by their index. The world's events play at their moments (FenceChunk.Replay, TreeTrunks.ReplaySnap, ManholePlume.Erupt,
    // the switch gates); going back in time puts the world as it was then (everything restored, the events up to then applied quietly). Round 79:
    // the trains, the level crossings, the traffic signals and the airport's circuit plane run on the recorded clock (WorldClock, RailTrains.ReplayAt).
    // Cameras: TV (roadside cameras along the focused racer's way, cut as it passes), Chase (ChaseCamera on a racer), Free (fly anywhere).
    // Controls (pad / keyboard): A / Space play-pause; RT / Right hold fast forward; LT / Left hold rewind; X / S slow motion; Y / C camera;
    // LB RB / Q E racer (in the free camera: down, up); D-pad left right / , . previous / next lap; D-pad up / Home restart; D-pad down / H HUD;
    // View / K save; B / Esc / Start leave. The free camera: left stick / WASD move, right stick / mouse look (right button).
    public sealed class ReplayViewer : MonoBehaviour
    {
        public static ReplayViewer Current { get; private set; }
        public static bool Active => Current != null;
        public static bool HudShown => Current && Current.hud;
        public static readonly List<ArcadeVehicle> EngineCars = new List<ArcadeVehicle>();
        public static float ThrottleOf(ArcadeVehicle c) { var v = Current; if (!v) return 0f; int i = v.puppets.IndexOf(c); return i >= 0 ? v.pedals[i] : 0f; }
        public static Transform GooseTarget { get; private set; }
        public static Vector3 GooseVelocity { get; private set; }

        public enum Cam { TV, Chase, Free }
        public ReplayData Data { get; private set; }
        public float T { get; private set; }            // the replay's clock (race clock seconds)
        public float Rate { get; private set; } = 1f;   // 1, 0.5, 0.25 (slow motion)
        public bool Paused { get; private set; }
        public Cam View { get; private set; } = Cam.TV;
        public int Focus { get; private set; }
        public int TvIndex { get; private set; } = -1;
        public List<Vector3> TvSpots => tv.Select(x => x.at).ToList();
        public ArcadeVehicle PuppetOf(int racer) => racer >= 0 && racer < puppets.Count ? puppets[racer] : null;
        public string Message = ""; float messageUntil;
        Action onClose;
        bool hud, holdFf, holdRw;
        float shownUntil;
        readonly List<ArcadeVehicle> puppets = new List<ArcadeVehicle>();
        readonly List<float> pedals = new List<float>();
        int nextEvent;
        RaceBarriers ownBarriers;   // watched from the REPLAYS page with no race on: the race's barriers for its course
        // what is put back on leaving
        float timeScale; bool chaseWas, viewsWas; Transform chaseTarget; Vector3 chaseScale; float fov; Vector3 camPos; Quaternion camRot;
        sealed class Held { public ArcadeVehicle car; public bool kinematic, detect, enabled; public Vector3 pos, vel, ang; public Quaternion rot; public List<Renderer> shown = new List<Renderer>(); public List<Behaviour> off = new List<Behaviour>(); }
        readonly List<Held> held = new List<Held>();
        readonly List<AudioSource> muted = new List<AudioSource>();
        InputAction aPlay, aSlow, aCam, aPrev, aNext, aLapPrev, aLapNext, aRestart, aHud, aSave, aLeave, aFf, aRw, aMove, aLook, aUp, aDown;
        // TV cameras
        struct Tv { public Vector3 at; public float s; }
        readonly List<Tv> tv = new List<Tv>();
        float tvSince; Vector3 freePos; float freeYaw, freePitch;

        // ---------------------------------------------------------------- open / close
        public static ReplayViewer Open(ReplayData d, Action closed = null)
        {
            if (d == null || d.racers.Count == 0 || Current) return null;
            var go = new GameObject("Replay viewer"); var v = go.AddComponent<ReplayViewer>();
            v.Data = d; v.onClose = closed; v.Begin();
            return v;
        }
        public void Close() { if (Current == this) Destroy(gameObject); }

        void Begin()
        {
            Current = this;
            var race = RaceController.Instance;
            if (race && race.CanSimulateRemaining) race.CompleteRace();   // the AI still racing are classified (they would race on, hidden, through the replay)
            timeScale = Time.timeScale > 0f ? Time.timeScale : 1f;
            var cam = Camera.main;
            if (cam)
            {
                var chase = cam.GetComponent<ChaseCamera>(); var views = cam.GetComponent<CameraViews>();
                chaseWas = chase && chase.enabled; viewsWas = views && views.enabled; chaseTarget = chase ? chase.target : null; chaseScale = chase ? chase.offsetScale : Vector3.one;
                fov = cam.fieldOfView; camPos = cam.transform.position; camRot = cam.transform.rotation;
                if (views) views.enabled = false;
                freePos = camPos; var e = camRot.eulerAngles; freeYaw = e.y; freePitch = Mathf.DeltaAngle(0f, e.x);
            }
            // the puppets: copies of the player's car as it is now (its head shown: first person hides it), before it is hidden
            var player = race ? race.PlayerCar : FindAnyObjectByType<ArcadeVehicle>();
            if (cam && cam.GetComponent<CameraViews>()) cam.GetComponent<CameraViews>().ShowHead();
            for (int i = 0; i < Data.racers.Count; i++) { puppets.Add(MakePuppet(Data.racers[i], player)); pedals.Add(0f); }
            // freeze and hide every live vehicle: the racers (player included) and the traffic
            var live = new List<ArcadeVehicle>();
            if (race) foreach (var e in race.Entrants) if (e.Car) live.Add(e.Car);
            if (player && !live.Contains(player)) live.Add(player);
            var tf = Traffic.Instance; if (tf) foreach (var t in tf.Cars) if (t && t.Car && !live.Contains(t.Car)) live.Add(t.Car);
            foreach (var c in live) Freeze(c, tf != null && tf.Cars.Any(x => x && x.Car == c));
            if (player) foreach (var s in player.GetComponentsInChildren<AudioSource>()) if (!s.mute) { s.mute = true; muted.Add(s); }
            // the race's barriers: up already in this race's own replay; built for the course when watched from the page in free roam
            if (race && race.State == RaceController.Stage.Free)
            {
                var route = race.RouteFor(Data.routeFile, Data.direction.StartsWith("reverse") ? "reverse" : "forward");
                if (route != null) { ownBarriers = new GameObject("Replay barriers").AddComponent<RaceBarriers>(); ownBarriers.Build(route); }
            }
            EngineCars.Clear(); EngineCars.AddRange(puppets.Where(p => p));
            Focus = Mathf.Clamp(Data.focus, 0, Data.racers.Count - 1);
            BuildTv();
            MakeInput();
            T = Data.t0; Seek(Data.t0);
            ApplyPoses();
            SetView(Cam.TV);
            shownUntil = Time.unscaledTime + 6f;
            Debug.Log($"[Replay] watching {Data.Title} ({Data.kind}), {Data.racers.Count} racers, {Data.traffic.Count} traffic, {Data.events.Count} events, {Data.t0:0.0} to {Data.t1:0.0} s, {tv.Count} TV cameras");
        }

        void Freeze(ArcadeVehicle c, bool traffic)
        {
            var h = new Held { car = c, kinematic = c.Body.isKinematic, detect = c.Body.detectCollisions, enabled = c.enabled, pos = c.Body.position, rot = c.Body.rotation };
            if (!c.Body.isKinematic) { h.vel = c.Body.linearVelocity; h.ang = c.Body.angularVelocity; }
            foreach (var b in c.GetComponents<Behaviour>()) if (b.enabled && (b is VehicleInput || b is RoadDriver || b is TrafficDriver || b is VehicleRespawn)) { b.enabled = false; h.off.Add(b); }
            c.enabled = false; if (!c.Body.isKinematic) { c.Body.linearVelocity = c.Body.angularVelocity = Vector3.zero; } c.Body.isKinematic = true; c.Body.detectCollisions = false;
            // traffic cars play their own part in the replay (moved by it), the racers are hidden (their puppets stand in)
            if (!traffic) foreach (var r in c.GetComponentsInChildren<Renderer>()) if (r.enabled) { r.enabled = false; h.shown.Add(r); }
            held.Add(h);
        }

        ArcadeVehicle MakePuppet(ReplayData.Racer r, ArcadeVehicle player)
        {
            if (!player) return null;
            var clone = Instantiate(player.gameObject); clone.name = "Replay " + r.name;
            foreach (var mb in clone.GetComponents<MonoBehaviour>())
            {
                if (mb is ArcadeVehicle || mb is VehicleConfiguration || mb is RiderGestures || mb is VehicleLights) continue;
                // Round 74 (A1, Dan: "airplane - replay, the propeller doesn't move"): a plane's or helicopter's own flight part is kept for the puppet (the
                // Apply below would otherwise Begin the one about to be destroyed, Destroy being deferred: no propeller, no rotors); on a puppet it only
                // turns its propeller / rotors and sounds, from the replay's throttle (PlaneFlight / HelicopterFlight Effects)
                if (mb is PlaneFlight || mb is HelicopterFlight) continue;
                mb.enabled = false; Destroy(mb);
            }
            foreach (var src in clone.GetComponentsInChildren<AudioSource>()) Destroy(src);
            foreach (Transform child in clone.transform) if (child.name == "Headlight") Destroy(child.gameObject);
            var car = clone.GetComponent<ArcadeVehicle>(); var cfg = clone.GetComponent<VehicleConfiguration>();
            if (!string.IsNullOrEmpty(r.rider)) { try { cfg.riderLook = JsonUtility.FromJson<RiderLook>(r.rider); } catch (Exception) { } }
            cfg.Apply(VehicleProfile.IndexOf(r.vehicle) >= 0 ? r.vehicle : "original");
            if (r.hasPaint) cfg.SetPaint(r.paint);
            car.enabled = false; car.Body.isKinematic = true; car.Body.detectCollisions = false; car.Puppet = true;
            foreach (var col in clone.GetComponentsInChildren<Collider>()) col.enabled = false;
            car.Body.interpolation = RigidbodyInterpolation.None;
            return car;
        }

        void OnDestroy()
        {
            if (Current != this) return;
            Current = null; EngineCars.Clear(); GooseTarget = null;
            WorldClock.Replay = float.NaN; if (RailTrains.Instance) RailTrains.Instance.ReplayEnd();   // Round 79: the world's clocks live again
            foreach (var p in puppets) if (p) Destroy(p.gameObject);
            if (ownBarriers) { ownBarriers.Clear(); Destroy(ownBarriers.gameObject); }
            var race = RaceController.Instance; if (race && race.Barriers != null) race.Barriers.ResetGatesForReplay(false);
            FenceChunk.RestoreAll(); ManholePlume.ShutAll();
            foreach (var h in held)
            {
                var c = h.car; if (!c) continue;
                c.Body.isKinematic = h.kinematic; c.Body.detectCollisions = h.detect;
                c.Body.position = h.pos; c.Body.rotation = h.rot; c.transform.SetPositionAndRotation(h.pos, h.rot);
                if (!h.kinematic) { c.Body.linearVelocity = h.vel; c.Body.angularVelocity = h.ang; }
                c.enabled = h.enabled; foreach (var b in h.off) if (b) b.enabled = true; foreach (var r in h.shown) if (r) r.enabled = true;
            }
            Physics.SyncTransforms();
            foreach (var s in muted) if (s) s.mute = false;
            var cam = Camera.main;
            if (cam)
            {
                cam.fieldOfView = fov; cam.transform.SetPositionAndRotation(camPos, camRot);
                var chase = cam.GetComponent<ChaseCamera>(); var views = cam.GetComponent<CameraViews>();
                if (chase) { chase.target = chaseTarget; chase.offsetScale = chaseScale; chase.enabled = chaseWas; if (chaseWas) chase.Snap(); }
                if (views) views.enabled = viewsWas;
            }
            Time.timeScale = timeScale;
            foreach (var a in Actions()) a?.Dispose();
            MenuInput.ConsumeThroughRelease();
            onClose?.Invoke();
        }

        // ---------------------------------------------------------------- input
        IEnumerable<InputAction> Actions() => new[] { aPlay, aSlow, aCam, aPrev, aNext, aLapPrev, aLapNext, aRestart, aHud, aSave, aLeave, aFf, aRw, aMove, aLook, aUp, aDown };
        static InputAction B(string name, params string[] paths) { var a = new InputAction(name, InputActionType.Button); foreach (var p in paths) a.AddBinding(p); a.Enable(); return a; }
        void MakeInput()
        {
            aPlay = B("Replay play", "<Gamepad>/buttonSouth", "<Keyboard>/space");
            aSlow = B("Replay slow", "<Gamepad>/buttonWest", "<Keyboard>/s");
            aCam = B("Replay camera", "<Gamepad>/buttonNorth", "<Keyboard>/c");
            aPrev = B("Replay racer back", "<Gamepad>/leftShoulder", "<Keyboard>/q");
            aNext = B("Replay racer on", "<Gamepad>/rightShoulder", "<Keyboard>/e");
            aLapPrev = B("Replay lap back", "<Gamepad>/dpad/left", "<Keyboard>/comma");
            aLapNext = B("Replay lap on", "<Gamepad>/dpad/right", "<Keyboard>/period");
            aRestart = B("Replay restart", "<Gamepad>/dpad/up", "<Keyboard>/home");
            aHud = B("Replay HUD", "<Gamepad>/dpad/down", "<Keyboard>/h");
            aSave = B("Replay save", "<Gamepad>/select", "<Keyboard>/k");
            aLeave = B("Replay leave", "<Gamepad>/buttonEast", "<Gamepad>/start", "<Keyboard>/escape", "<Keyboard>/backspace");
            aFf = new InputAction("Replay fast", InputActionType.Value); aFf.AddBinding("<Gamepad>/rightTrigger"); aFf.AddBinding("<Keyboard>/rightArrow"); aFf.Enable();
            aRw = new InputAction("Replay rewind", InputActionType.Value); aRw.AddBinding("<Gamepad>/leftTrigger"); aRw.AddBinding("<Keyboard>/leftArrow"); aRw.Enable();
            aUp = B("Replay up", "<Gamepad>/rightShoulder", "<Keyboard>/e"); aDown = B("Replay down", "<Gamepad>/leftShoulder", "<Keyboard>/q");
            aMove = new InputAction("Replay move", InputActionType.Value, expectedControlType: "Vector2"); aMove.AddBinding("<Gamepad>/leftStick");
            aMove.AddCompositeBinding("2DVector").With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s").With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d"); aMove.Enable();
            aLook = new InputAction("Replay look", InputActionType.Value, expectedControlType: "Vector2"); aLook.AddBinding("<Gamepad>/rightStick"); aLook.Enable();
            MenuInput.ConsumeThroughRelease();
        }
        // checks (and the page's keys): the same as the buttons
        public void TogglePause() { if (Paused && T >= Data.t1 - 0.05f) { Restart(); return; } Paused = !Paused; Say(Paused ? "PAUSED" : "PLAY"); }
        public void CycleSlow() { Rate = Rate >= 1f ? 0.5f : Rate >= 0.5f ? 0.25f : 1f; Say(Rate >= 1f ? "NORMAL SPEED" : $"SLOW MOTION x{Rate:0.##}"); }
        public void Restart() { Seek(Data.t0); Paused = false; Say("RESTART"); }
        public void SetView(Cam c)
        {
            View = c; var cam = Camera.main; if (!cam) return; var chase = cam.GetComponent<ChaseCamera>();
            if (c == Cam.Chase && chase && PuppetOf(Focus)) { chase.target = PuppetOf(Focus).transform; chase.offsetScale = Vector3.one; chase.enabled = true; chase.Snap(); cam.fieldOfView = fov; }
            else if (chase) chase.enabled = false;
            if (c == Cam.Free) { freePos = cam.transform.position; var e = cam.transform.rotation.eulerAngles; freeYaw = e.y; freePitch = Mathf.DeltaAngle(0f, e.x); cam.fieldOfView = fov; }
            if (c == Cam.TV) { TvIndex = -1; tvSince = -99f; }
            Say(c == Cam.TV ? "TV CAMERAS" : c == Cam.Chase ? "CHASE CAMERA: " + Data.racers[Focus].name : "FREE CAMERA");
        }
        public void FocusOn(int i)
        {
            Focus = (i % Data.racers.Count + Data.racers.Count) % Data.racers.Count;
            if (View == Cam.Chase) SetView(Cam.Chase); else { TvIndex = -1; Say(Data.racers[Focus].name); }
        }
        public void JumpLap(int d)
        {
            var laps = Data.racers[Focus].laps;
            var starts = new List<float> { Data.t0 }; starts.AddRange(laps.Select(l => l.end).Where(x => x < Data.t1));
            int cur = starts.FindLastIndex(s => s <= T + 0.5f); int to = Mathf.Clamp(cur + d, 0, starts.Count - 1);
            if (d < 0 && T - starts[Mathf.Max(0, cur)] > 2f) to = Mathf.Max(0, cur);   // first back to this lap's start
            Seek(starts[to]); Say(to == 0 ? "THE START" : $"LAP {to + 1}");
        }
        public void SaveIt() { var rec = ReplayRecorder.Instance; Say(rec ? rec.Save(Data) : "SAVE FAILED"); }
        void Say(string s) { Message = s; messageUntil = Time.unscaledTime + 2.2f; shownUntil = Time.unscaledTime + 4f; }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            if (!(MenuInput.Blocked))
            {
                bool any = false;
                if (aLeave.WasPressedThisFrame()) { Close(); return; }
                if (aPlay.WasPressedThisFrame()) { TogglePause(); any = true; }
                if (aSlow.WasPressedThisFrame() && View != Cam.Free) { CycleSlow(); any = true; }
                if (aCam.WasPressedThisFrame()) { SetView((Cam)(((int)View + 1) % 3)); any = true; }
                if (View != Cam.Free) { if (aPrev.WasPressedThisFrame()) { FocusOn(Focus - 1); any = true; } if (aNext.WasPressedThisFrame()) { FocusOn(Focus + 1); any = true; } }
                if (aLapPrev.WasPressedThisFrame()) { JumpLap(-1); any = true; }
                if (aLapNext.WasPressedThisFrame()) { JumpLap(1); any = true; }
                if (aRestart.WasPressedThisFrame()) { Restart(); any = true; }
                if (aHud.WasPressedThisFrame()) { hud = !hud; any = true; }
                if (aSave.WasPressedThisFrame()) { SaveIt(); any = true; }
                holdFf = aFf.ReadValue<float>() > 0.3f; holdRw = aRw.ReadValue<float>() > 0.3f;
                if (holdFf || holdRw || any) shownUntil = Time.unscaledTime + 4f;
            }
            // the clock: fast forward x4, rewind x4, else the chosen rate; the world's own motion (debris, water, falling trees) at that rate too
            float rate = holdRw ? -4f : holdFf ? 4f : Paused ? 0f : Rate;
            Time.timeScale = holdRw ? 1f : Mathf.Max(0f, rate);
            float t = Mathf.Clamp(T + rate * dt, Data.t0, Data.t1);
            if (t < T - 1e-4f) Seek(t); else { T = t; PlayEvents(true); }
            if (T >= Data.t1 && !Paused && rate > 0f) { Paused = true; Say("THE END: A plays again, B leaves"); }
            ApplyPoses();
        }
        void LateUpdate() { UpdateCamera(Time.unscaledDeltaTime); }

        // ---------------------------------------------------------------- the clock and the world
        // back in time (or a jump): the world as it was at t
        public void Seek(float t)
        {
            T = Mathf.Clamp(t, Data.t0, Data.t1);
            FenceChunk.RestoreAll(); ManholePlume.ShutAll();
            var race = RaceController.Instance; var bars = ownBarriers ? ownBarriers : race ? race.Barriers : null; if (bars) bars.ResetGatesForReplay(true);
            nextEvent = 0; PlayEvents(false);
            ApplyPoses();
        }
        void PlayEvents(bool live)
        {
            var ev = Data.events;
            while (nextEvent < ev.Count && ev[nextEvent].t <= T) { Apply(ev[nextEvent], live && T - ev[nextEvent].t < 1f); nextEvent++; }
        }
        void Apply(ReplayData.Event e, bool show)
        {
            switch (e.kind)
            {
                case ReplayData.Kind.Panel: FenceChunk.ReplayBreak(e.p, e.a, e.v, show); break;
                case ReplayData.Kind.Restore: FenceChunk.ReplayRestore(e.p); break;
                case ReplayData.Kind.Tree: if (TreeTrunks.Instance) TreeTrunks.Instance.ReplaySnap(e.a, e.v, show); break;
                case ReplayData.Kind.Plume: if (show || e.f > 1e5f) ManholePlume.ReplayErupt(e.p, e.f); break;
                case ReplayData.Kind.Gate: { var race = RaceController.Instance; var bars = ownBarriers ? ownBarriers : race ? race.Barriers : null; if (bars) bars.OpenGateForReplay(e.a); break; }
                case ReplayData.Kind.Gesture:
                    if (show) { var p = PuppetOf(e.a); var g = p ? p.GetComponent<RiderGestures>() : null; if (g) { if (e.b == (int)RiderGestures.Kind.Celebrate) g.Celebrate(); else g.Wave(); } }
                    break;
            }
        }

        List<ReplayData.Event> clocks;
        void ApplyPoses()
        {
            for (int i = 0; i < puppets.Count; i++)
            {
                var c = puppets[i]; if (!c) continue; var tr = Data.racers[i].track; if (tr.frames.Count == 0) continue;
                var f = ReplayData.At(tr, T);
                c.Body.position = f.p; c.Body.rotation = f.q; c.transform.SetPositionAndRotation(f.p, f.q);
                c.PuppetSpeed = f.speed; c.PuppetSteer = f.steer; pedals[i] = f.throttle;
                if (i == Data.focus) { GooseTarget = c.transform; var f2 = ReplayData.At(tr, T + 0.1f); GooseVelocity = (f2.p - f.p) / 0.1f; }
            }
            // Round 79 (A): the world's clock as it was (the trains, the crossings, the signals, the circuit plane); a replay recorded before keeps the live clock
            if (clocks == null) { clocks = new List<ReplayData.Event>(); foreach (var e in Data.events) if (e.kind == ReplayData.Kind.Clock) clocks.Add(e); }
            if (clocks.Count > 0)
            {
                int k = 0; while (k + 1 < clocks.Count && clocks[k + 1].t <= T) k++;
                var c0 = clocks[k]; WorldClock.Replay = c0.f + (T - c0.t);
                if (RailTrains.Instance) RailTrains.Instance.ReplayAt(WorldClock.Replay + c0.v.x);
            }
            var tf = Traffic.Instance;
            if (tf) for (int i = 0; i < Data.traffic.Count && i < tf.Cars.Count; i++)
                {
                    var c = tf.Cars[i] ? tf.Cars[i].Car : null; var tr = Data.traffic[i]; if (!c || tr.frames.Count == 0) continue;
                    var f = ReplayData.At(tr, T);
                    if (tr.frames.Count > 1 && T > tr.frames[tr.frames.Count - 1].t + 1f) continue;
                    c.Body.position = f.p; c.Body.rotation = f.q; c.transform.SetPositionAndRotation(f.p, f.q);
                }
        }

        // ---------------------------------------------------------------- cameras
        // TV cameras: along the focused racer's own way (the player's at first), every 70 m, 9 m off to one side (the other if blocked) and 3.5 m up,
        // where they can see the way; where nothing outside can (under a roof, in the pipes) one on the way itself, 14 m back along it
        float tvReach = 75f;   // Round 71: how far a TV camera follows its racer (a plane's farther)
        public static List<Vector3> ExtraTv;   // Round 72: a quarry jump's own TV cameras (on the rims, by the kicker, at the landing deck)
        void BuildTv()
        {
            tv.Clear();
            var path = Data.racers[Mathf.Clamp(Data.focus, 0, Data.racers.Count - 1)].track.frames.Select(f => f.p).ToList();
            if (path.Count < 2) return;
            if (ExtraTv != null && ExtraTv.Count > 0)
            {
                tvReach = 400f; var cumJ = new float[path.Count]; for (int i = 1; i < path.Count; i++) cumJ[i] = cumJ[i - 1] + (path[i] - path[i - 1]).magnitude;
                foreach (var at in ExtraTv) { int best = 0; float bd = float.MaxValue; for (int i = 0; i < path.Count; i++) { float dd = (path[i] - at).sqrMagnitude; if (dd < bd) { bd = dd; best = i; } } tv.Add(new Tv { at = at, s = cumJ[best] }); }
                return;
            }
            // Round 71 (the air race): for a plane, cameras in the air beside its way (every 160 m, 30 m off to a side, 6 m up, ahead of it), not on the ground
            var veh = VehicleProfile.Find(Data.racers[Mathf.Clamp(Data.focus, 0, Data.racers.Count - 1)].vehicle); tvReach = veh != null && veh.Flies ? 190f : 75f;
            if (veh != null && veh.Flies)
            {
                var cumA = new float[path.Count]; for (int i = 1; i < path.Count; i++) cumA[i] = cumA[i - 1] + (path[i] - path[i - 1]).magnitude;
                int sideA = 1; float nextA = 40f;
                for (int i = 1; i < path.Count; i++)
                {
                    if (cumA[i] < nextA) continue; nextA = cumA[i] + 160f;
                    var fw = path[Mathf.Min(path.Count - 1, i + 3)] - path[Mathf.Max(0, i - 3)]; fw.y = 0f; if (fw.sqrMagnitude < 1e-3f) continue; fw.Normalize();
                    var at = path[i] + Vector3.Cross(Vector3.up, fw) * (30f * sideA) + Vector3.up * 6f + fw * 60f;
                    float gy = GroundQuery.Height(at.x, at.z); if (!float.IsNaN(gy) && at.y < gy + 2f) at.y = gy + 2f;
                    tv.Add(new Tv { at = at, s = cumA[i] }); sideA = -sideA;
                }
                return;
            }
            var cum = new float[path.Count]; for (int i = 1; i < path.Count; i++) cum[i] = cum[i - 1] + (path[i] - path[i - 1]).magnitude;
            int side = 1; float next = 20f;
            for (int i = 1; i < path.Count; i++)
            {
                if (cum[i] < next) continue; next = cum[i] + 70f;
                var p = path[i]; var fwd = path[Mathf.Min(path.Count - 1, i + 3)] - path[Mathf.Max(0, i - 3)]; fwd.y = 0f; if (fwd.sqrMagnitude < 1e-3f) continue; fwd.Normalize();
                var right = Vector3.Cross(Vector3.up, fwd);
                bool placed = false;
                foreach (int sd in new[] { side, -side })
                    foreach (float off in new[] { 9f, 6f })
                    {
                        var q = p + right * (sd * off) + fwd * 10f;
                        if (!Physics.Raycast(q + Vector3.up * 60f, Vector3.down, out var hit, 120f, ~0, QueryTriggerInteraction.Ignore)) continue;
                        var at = hit.point + Vector3.up * 3.5f;
                        if (Mathf.Abs(at.y - p.y) > 12f) continue;   // not on a roof or a hilltop far above, nor down a bank
                        var aim = p + Vector3.up * 0.8f; var d = aim - at;
                        if (Physics.Raycast(at, d.normalized, d.magnitude - 1.5f, ~0, QueryTriggerInteraction.Ignore)) continue;
                        tv.Add(new Tv { at = at, s = cum[i] }); placed = true; side = -sd; break;
                    }
                if (placed) continue;
                // on the way itself, behind (in a pipe or under a roof)
                int j = i; while (j > 0 && cum[i] - cum[j] < 14f) j--;
                var back = path[j] + Vector3.up * 1.6f; var dd = p + Vector3.up * 0.8f - back;
                if (!Physics.Raycast(back, dd.normalized, dd.magnitude, ~0, QueryTriggerInteraction.Ignore)) tv.Add(new Tv { at = back, s = cum[j] });
            }
        }
        bool Sees(Vector3 from, Vector3 to) { var d = to - from; return !Physics.Raycast(from, d.normalized, Mathf.Max(0f, d.magnitude - 1f), ~0, QueryTriggerInteraction.Ignore); }

        void UpdateCamera(float dt)
        {
            var cam = Camera.main; if (!cam) return;
            var target = PuppetOf(Focus); if (!target) return;
            var tp = target.transform.position + Vector3.up * 0.8f;
            if (View == Cam.Chase) return;   // ChaseCamera follows the puppet
            if (View == Cam.Free)
            {
                var look = aLook.ReadValue<Vector2>(); var mouse = Mouse.current;
                if (mouse != null && mouse.rightButton.isPressed) look += mouse.delta.ReadValue() * 0.06f;
                freeYaw += look.x * 110f * dt; freePitch = Mathf.Clamp(freePitch - look.y * 80f * dt, -85f, 85f);
                var rot = Quaternion.Euler(freePitch, freeYaw, 0f); var mv = aMove.ReadValue<Vector2>();
                float up = (aUp.IsPressed() ? 1f : 0f) - (aDown.IsPressed() ? 1f : 0f);
                var kb = Keyboard.current; float speed = kb != null && kb.leftShiftKey.isPressed ? 40f : 14f;
                freePos += (rot * new Vector3(mv.x, 0f, mv.y) + Vector3.up * up) * speed * dt;
                cam.transform.SetPositionAndRotation(freePos, rot);
                return;
            }
            // TV: keep the camera while it sees the racer within 75 m and the racer has not gone 25 m past it; then the next one ahead that sees it
            float sNow = NearestS(target.transform.position);
            bool keep = TvIndex >= 0 && TvIndex < tv.Count && (tv[TvIndex].at - tp).magnitude < tvReach && Sees(tv[TvIndex].at, tp) && sNow - tv[TvIndex].s < 25f;
            if (!keep && Time.unscaledTime - tvSince > 1.2f || TvIndex < 0)
            {
                int best = -1; float bestKey = float.MaxValue;
                for (int i = 0; i < tv.Count; i++)
                {
                    float dist = (tv[i].at - tp).magnitude; if (dist > tvReach) continue;
                    float ahead = tv[i].s - sNow; float k = ahead >= -5f ? ahead : 200f - ahead;   // the next one ahead first
                    if (k < bestKey && Sees(tv[i].at, tp)) { bestKey = k; best = i; }
                }
                if (best != TvIndex) { TvIndex = best; tvSince = Time.unscaledTime; }
            }
            if (TvIndex < 0)
            {   // no TV camera sees it: a chase view from behind and above, by hand
                var back = target.transform.position - Vector3.ProjectOnPlane(target.transform.forward, Vector3.up).normalized * 8f + Vector3.up * 3.5f;
                cam.transform.position = Vector3.Lerp(cam.transform.position, back, 1f - Mathf.Exp(-6f * dt)); cam.transform.LookAt(tp); cam.fieldOfView = fov; return;
            }
            var at = tv[TvIndex].at; float distance = (tp - at).magnitude;
            cam.transform.position = at;
            var want = Quaternion.LookRotation(tp - at, Vector3.up);
            cam.transform.rotation = Time.unscaledTime - tvSince < 0.05f ? want : Quaternion.Slerp(cam.transform.rotation, want, 1f - Mathf.Exp(-12f * dt));
            cam.fieldOfView = Mathf.Clamp(2f * Mathf.Atan(7f / Mathf.Max(1f, distance)) * Mathf.Rad2Deg, 14f, 62f);   // the car about a third of the picture
        }
        float NearestS(Vector3 p)
        {
            if (tv.Count == 0) return 0f; var path = Data.racers[Mathf.Clamp(Data.focus, 0, Data.racers.Count - 1)].track.frames;
            // the focused racer's own distance along the TV path: its time along the focus racer's track as a proxy (the TV cameras are on it)
            int best = 0; float bd = float.MaxValue; int step = Mathf.Max(1, path.Count / 600);
            for (int i = 0; i < path.Count; i += step) { float d = (path[i].p - p).sqrMagnitude; if (d < bd) { bd = d; best = i; } }
            float s = 0f; for (int i = 1; i <= best; i++) s += (path[i].p - path[i - 1].p).magnitude;
            return s;
        }

        // ---------------------------------------------------------------- the overlay
        void OnGUI()
        {
            float sc = Screen.height / 800f; var m = GUI.matrix; GUI.matrix = Matrix4x4.Scale(new Vector3(sc, sc, 1f)); float w = Screen.width / sc, h = 800f;
            var big = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold, normal = { textColor = Color.white } };
            var small = new GUIStyle(GUI.skin.label) { fontSize = 15, normal = { textColor = new Color(1f, 1f, 1f, .92f) } };
            void Box(Rect r) { GUI.color = new Color(0f, 0f, 0f, .55f); GUI.DrawTexture(r, Texture2D.whiteTexture); GUI.color = Color.white; }
            // always: a small REPLAY tag (blinking dot while it plays)
            string state = Paused ? "II" : holdRw ? "<<" : holdFf ? ">>" : Rate < 1f ? $"x{Rate:0.##}" : ">";
            Box(new Rect(w - 190, 16, 174, 34)); GUI.Label(new Rect(w - 180, 18, 170, 30), $"REPLAY  {state}", big);
            var r = Data.racers[Focus];
            if (hud)
            {
                int lap = 1 + r.laps.Count(l => l.end <= T); lap = Mathf.Min(lap, Data.laps);
                float since = T - (r.laps.Where(l => l.end <= T).Select(l => l.end).DefaultIfEmpty(Mathf.Max(0f, r.laps.Count > 0 ? r.laps[0].start : 0f)).Max());
                var p = PuppetOf(Focus); float mph = p ? Mathf.Abs(p.PuppetSpeed) * 2.23694f : 0f;
                Box(new Rect(16, 16, 380, 118));
                GUI.Label(new Rect(28, 20, 360, 30), Data.Title, big);
                GUI.Label(new Rect(28, 52, 360, 24), $"{r.name}   {VehicleProfile.Find(r.vehicle).Name}   {mph:0} mph", small);
                GUI.Label(new Rect(28, 76, 360, 24), T < 0f ? $"Countdown {-T:0.0}" : r.finishedAt >= 0f && T >= r.finishedAt ? $"Finished  {RaceController.Fmt(r.total)}" : $"Lap {lap} / {Data.laps}   {RaceController.Fmt(Mathf.Max(0f, since))}", small);
                GUI.Label(new Rect(28, 100, 360, 24), $"Race clock {RaceController.Fmt(Mathf.Max(0f, T))}   {(View == Cam.TV ? "TV cameras" : View == Cam.Chase ? "Chase camera" : "Free camera")}", small);
            }
            if (Time.unscaledTime < messageUntil && Message != "") { var ms = new GUIStyle(big) { alignment = TextAnchor.MiddleCenter, fontSize = 26 }; Box(new Rect(w / 2 - 260, 90, 520, 44)); GUI.Label(new Rect(w / 2 - 260, 92, 520, 40), Message, ms); }
            if (hud || Time.unscaledTime < shownUntil)
            {
                bool pad = MenuInput.Controller;
                string keys = pad ? "A play / pause    RT fast forward    LT rewind    X slow motion    Y camera    LB / RB racer    D-pad: < > lap, up restart, down HUD    View save    B leave"
                                  : "Space play / pause    Right fast forward    Left rewind    S slow motion    C camera    Q / E racer    , . lap    Home restart    H HUD    K save    Esc leave";
                if (View == Cam.Free) keys += pad ? "    (free camera: sticks move and look, LB / RB down / up)" : "    (free camera: WASD move, right mouse look, Q / E down / up, Shift fast)";
                var ks = new GUIStyle(small) { alignment = TextAnchor.MiddleCenter, wordWrap = true };
                Box(new Rect(40, h - 70, w - 80, 50)); GUI.Label(new Rect(50, h - 68, w - 100, 46), keys, ks);
                // the time line
                float u = Mathf.InverseLerp(Data.t0, Data.t1, T); GUI.color = new Color(1f, 1f, 1f, .35f); GUI.DrawTexture(new Rect(40, h - 80, w - 80, 4), Texture2D.whiteTexture);
                GUI.color = new Color(.3f, .95f, .81f); GUI.DrawTexture(new Rect(40, h - 80, (w - 80) * u, 4), Texture2D.whiteTexture); GUI.color = Color.white;
            }
            GUI.matrix = m;
        }
    }
}
