using System.Collections.Generic;
using System.Linq;
using Racer;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ChastainRush
{
    // Round 37 (Part B, Dan: "Lets go ahead and add split screen"): Racer's split screen (SplitScreen.cs: SplitScreen, SplitRace; SplitRoam.cs),
    // ported. Two players on one PC, each with their own device (the keyboard goes with player 1, as Racer), their own vehicle, rider, camera
    // view, resets and HUD; player 2 may be the AI so one person can try it alone. The setup is the title menu's SPLIT SCREEN page
    // (Menus/GameMenus.Split.cs); the HUD of each half is SplitHud.cs. What the two players do (Mode):
    //   Race         any route either way, with or without AI rivals (traffic as Settings > Gameplay sets it): player 2 is a racer under
    //                RaceController's own rules (a second human seat, or an AI driver when "Player 2: AI driver" is on)
    //   Free Roam    from where player 1 is; player 2 beside them (Racer's SplitRoam); an AI player 2 cruises the roads with the traffic driver
    //   Stunt park   Free Roam starting on the stunt park's start line, both scoring tricks
    //   Stunt event  Racer's split-screen Stunt event: each player their own points; with player 2 as the AI it is player 1's alone, on one view
    // Chastain Rush edits: RaceController / GameMenus / the IMGUI HUD stand for Racer's RaceDirector / RaceFlow / RaceMenus / uGUI HUD; Police
    // Chase, Speed Patrol, Getaway, Stunt Races, the campaign and the winner shot are not in this game and are left behind. Single-player is
    // unchanged: every hook checks Active.
    public static class SplitScreen
    {
        public enum Kind { Race, FreeRoam, StuntPark, Stunt }
        public static bool Active => Session != null;
        public static SplitSession Session { get; internal set; }
        public static Kind Mode = Kind.Race;
        public static InputDevice P1Device, P2Device;
        public static bool P2Ai = true;
        // player 2's vehicle and colour (vehicle2.json beside the records, as VehicleSwitch keeps player 1's; a check never reads or writes it)
        public static string P2Vehicle = "atv"; public static int P2Colour = 3;
        // one view (Racer's OneView): the Stunt event with player 2 as the AI is player 1's alone
        public static bool OneView => Active && Mode == Kind.Stunt && P2Ai;
        public static bool Roaming => Active && Mode != Kind.Race;
        public static bool LeftRight => GameSettings.Settings.splitLeftRight;
        public static string NameOf(int player) => player == 1 ? PlayerNames.Player : P2Ai ? "AI" : string.IsNullOrEmpty(GameSettings.Settings.player2Name) ? "Player 2" : GameSettings.Settings.player2Name;
        public static InputDevice DeviceOf(int player) => player == 1 ? P1Device : P2Ai ? null : P2Device;
        public static string DeviceName(InputDevice d) => d == null ? "-" : d is Keyboard ? "Keyboard" : d is Gamepad g ? "Controller " + (Gamepad.all.ToList().IndexOf(g) + 1) : d.displayName;
        public static bool Ready => P1Device != null && (P2Ai || P2Device != null);
        // The two views' cameras while split (empty otherwise), and the two players' vehicles
        public static readonly List<Camera> Views = new();
        public static ArcadeVehicle P1Car => Session ? Session.P1 : null;
        public static ArcadeVehicle P2Car => Session ? Session.P2 : null;
        // the player number of a vehicle (0: neither)
        public static int PlayerOf(ArcadeVehicle car) => !Session || !car ? 0 : car == Session.P1 ? 1 : car == Session.P2 ? 2 : 0;
        // Whether either view shows a point (margin as a fraction of the view); false when not split
        public static bool InAView(Vector3 point, float margin)
        {
            foreach (var cam in Views)
            {
                if (!cam || !cam.isActiveAndEnabled) continue; var v = cam.WorldToViewportPoint(point);
                if (v.z > -8 && v.x > -margin && v.x < 1 + margin && v.y > -margin && v.y < 1 + margin) return true;
            }
            return false;
        }
        // The nearer of the two players to a point (one player: theirs)
        public static float DistanceToPlayers(Vector3 point, Vector3 p1)
        {
            float d = Vector3.Distance(point, p1);
            if (Session && Session.P2) d = Mathf.Min(d, Vector3.Distance(point, Session.P2.Body.position));
            return d;
        }

        public static SplitSession Begin(Kind mode)
        {
            End(); Mode = mode;
            var go = new GameObject("Split screen (Round 37)"); Session = go.AddComponent<SplitSession>(); Session.Begin();
            return Session;
        }
        public static void End() { if (Session) { var s = Session; s.Teardown(); Session = null; Object.Destroy(s.gameObject); } Views.Clear(); }
    }

    // Racer's SplitRace + SplitRoam: player 2's vehicle, the second camera, the views' detail, the devices
    public sealed class SplitSession : MonoBehaviour
    {
        public ArcadeVehicle P1 { get; private set; }
        public ArcadeVehicle P2 { get; private set; }
        public Camera Cam1 => cam1;
        public Camera Cam2 => cam2;
        public CameraViews Views2 => views2;
        public ArcadeActivities Activities2 { get; private set; }
        public TrafficDriver Cruiser { get; private set; }
        public float StartedAt { get; private set; }
        public int MissingPlayer { get; private set; }   // 0 = both devices present
        // Racer 0.92 Part F: the layers a view's own weather is drawn on, each hidden from the other view
        public const int View1Layer = 29, View2Layer = 30;
        public const float LodScale = .6f, FarClip = 1000, ShadowScale = .6f;
        Camera cam1, cam2; ChaseCamera chase1, chase2; CameraViews views2; Rect rect1; float far1, lodBias, shadowDistance = -1; int mask1;
        GameObject holder;

        public void Begin()
        {
            P1 = PlayerOne();
            StartedAt = Time.unscaledTime;
            var input = P1.GetComponent<VehicleInput>(); input.Bind(SplitScreen.P1Device);
            InputSystem.onDeviceChange += DeviceChanged;
            // per-view detail, lowered for the two views only (restored in Teardown); single-player rendering is unchanged (Racer)
            lodBias = QualitySettings.lodBias; if (!SplitScreen.OneView) QualitySettings.lodBias = lodBias * LodScale;
            var urp = Application.isEditor || SplitScreen.OneView ? null : GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (urp) { shadowDistance = urp.shadowDistance; urp.shadowDistance = shadowDistance * ShadowScale; }
            cam1 = Camera.main; chase1 = cam1.GetComponent<ChaseCamera>(); rect1 = cam1.rect; far1 = cam1.farClipPlane; mask1 = cam1.cullingMask;
            if (!SplitScreen.OneView) MakePlayerTwo();
            Cameras();
            gameObject.AddComponent<SplitHud>().session = this;
            if (!SplitScreen.OneView) MiniMap.Boot(gameObject, 2);   // player 2's minimap in their half (gone with the session)
        }

        // the player's own vehicle (VehicleSwitch's player 1, the one the race and the menus drive)
        static ArcadeVehicle PlayerOne()
        {
            foreach (var sw in FindObjectsByType<VehicleSwitch>(FindObjectsSortMode.None)) if (sw.player == 1) return sw.GetComponent<ArcadeVehicle>();
            return FindAnyObjectByType<ArcadeVehicle>();
        }

        // Racer's SplitRoam.CreatePlayerTwo: a copy of player 1's vehicle with only a player's own parts, made under a switched-off holder
        // so none of player 1's scripts wakes up in it, then put on in player 2's vehicle, colour and rider, beside player 1
        void MakePlayerTwo()
        {
            holder = new GameObject("Player 2 holder"); holder.SetActive(false);
            var clone = Instantiate(P1.gameObject, holder.transform); clone.name = "PLAYER 2";
            foreach (var mb in clone.GetComponents<MonoBehaviour>())
            {
                if (mb is ArcadeVehicle || mb is VehicleConfiguration || mb is VehicleInput || mb is VehicleRespawn || mb is RampHold || mb is WaterFeedback
                    || mb is RiderGestures || mb is VehicleLights || mb is VehicleSwitch) continue;
                DestroyImmediate(mb);
            }
            foreach (var src in clone.GetComponents<AudioSource>()) DestroyImmediate(src);
            foreach (Transform child in clone.transform) if (child.name == "Headlight") DestroyImmediate(child.gameObject);   // VehicleLights makes its own
            var sw = clone.GetComponent<VehicleSwitch>(); if (sw) sw.player = 2;
            var cfg = clone.GetComponent<VehicleConfiguration>(); cfg.riderLook = (GameSettings.Settings.rider2 ?? new RiderLook()).Valid();
            clone.transform.SetParent(null, true);
            clone.SetActive(true);
            P2 = clone.GetComponent<ArcadeVehicle>();
            // a human player 2 answers only their own device; the AI's input stays off (the race's driver or the cruiser drives it)
            var input = clone.GetComponent<VehicleInput>(); if (!SplitScreen.P2Ai) input.Bind(SplitScreen.P2Device); input.enabled = !SplitScreen.P2Ai;
            P2.enabled = true; P2.Body.isKinematic = false;
            if (!clone.GetComponent<VehicleAudio>()) clone.AddComponent<VehicleAudio>();
            Destroy(holder); holder = null;
            if (TreeTrunks.Instance) TreeTrunks.Instance.Track(clone.transform);
            // each player their own jumps, speed traps and tricks (Racer: SplitRoam.Activities2)
            Activities2 = clone.AddComponent<ArcadeActivities>(); Activities2.InitializeSecond(ArcadeActivities.Root, 2);
            PlaceBeside(P2, P1.transform, 4.5f);
        }

        // the vehicle put on again (garage, or a race's allowed vehicle): the chase camera's offset follows the profile
        public void Refit() { if (P2 && chase2) { chase2.offset = P2.GetComponent<VehicleConfiguration>().Profile.Camera; chase2.Snap(); } }

        // Racer's SplitRoam.Place: on the ground beside (side metres to the right of) another vehicle, facing the same way; behind it if blocked
        public static void PlaceBeside(ArcadeVehicle car, Transform beside, float side)
        {
            var rot = Quaternion.Euler(0, beside.eulerAngles.y, 0); var respawn = car.GetComponent<VehicleRespawn>();
            foreach (var offset in new[] { rot * new Vector3(side, 0, 0), rot * new Vector3(-side, 0, 0), rot * new Vector3(0, 0, -9), rot * new Vector3(0, 0, 9), rot * new Vector3(side, 0, -9) })
            {
                var at = beside.position + offset; var ground = RaceRoute.Surface(at.x, at.z);
                if (Mathf.Abs(ground.y - beside.position.y) > 2.5f) continue;
                if (Physics.CheckBox(ground + Vector3.up * 1.1f, new Vector3(1.1f, .6f, 2.4f), rot, ~0, QueryTriggerInteraction.Ignore)) continue;
                if (respawn) respawn.PlaceAt(ground + Vector3.up * .3f, rot.eulerAngles.y); else { car.Body.position = ground + Vector3.up; car.Body.rotation = rot; }
                return;
            }
            var p = beside.position + rot * new Vector3(side, 1, 0);
            if (respawn) respawn.PlaceAt(p, rot.eulerAngles.y); else { car.Body.position = p; car.Body.rotation = rot; car.transform.SetPositionAndRotation(p, rot); }
        }

        // the AI player 2 in Free Roam cruises the roads (Racer: the traffic driver on the nearest road, without traffic's recycling)
        public void Cruise()
        {
            if (!P2 || !SplitScreen.P2Ai) return;
            if (Rider2) { Destroy(Rider2); Rider2 = null; }   // Round 38
            if (Cruiser) { DestroyImmediate(Cruiser); Cruiser = null; }
            var net = TrafficNetwork.Instance; var traffic = Traffic.Instance; if (!net || !traffic) return;
            // the nearest point of the nearest road open to traffic (not a bridge), its lane on the side player 2 is heading
            var p = new Vector2(P2.Body.position.x, P2.Body.position.z); int best = -1; float bd = float.MaxValue; Vector2 bq = p, bt = Vector2.up;
            for (int e = 0; e < net.EdgeCount; e++)
            {
                if (net.Bridge(e) || net.Length[e] < 12) continue;
                for (int i = 1; i < net.PointCount(e); i++)
                {
                    Vector2 a = net.Point(e, i - 1), b = net.Point(e, i), ab = b - a; float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(ab.sqrMagnitude, 1e-4f)); var q = a + ab * t; float d = (q - p).sqrMagnitude;
                    if (d < bd) { bd = d; best = e; bq = q; bt = ab.normalized; }
                }
            }
            if (best < 0 || bd > 80f * 80f) return;
            var heading = new Vector2(P2.transform.forward.x, P2.transform.forward.z);
            int dir = net.Oneway(best) || Vector2.Dot(bt, heading) >= 0f ? 1 : -1;
            var tan = bt * dir; var right = new Vector2(tan.y, -tan.x);
            float lane = net.Oneway(best) ? (net.Half(best) >= 4.5f ? net.Half(best) * .45f : 0f) : Mathf.Clamp(net.Half(best) * .5f, 1.3f, 2.6f);
            var at = bq + right * lane; var pos = RaceRoute.Surface(at.x, at.y) + Vector3.up * .7f;
            P2.GetComponent<VehicleInput>().enabled = false;
            Cruiser = P2.gameObject.AddComponent<TrafficDriver>(); Cruiser.Initialize(traffic, P2);
            Cruiser.Place(best, dir, 0, pos, net.Speed(best) * .6f);
        }
        public void StopCruise() { if (Cruiser) { Destroy(Cruiser); Cruiser = null; } if (Rider2) { Destroy(Rider2); Rider2 = null; } }
        // Round 38 (Part D, BUG-001 of session 2026-10-16_21-19-50-140_b75298: "the AI wandered off the course and is just continually running into the
        // wall"): in the stunt park and the Stunt event the AI player 2 rides the park's lines as a stunt rival (StuntRival), its tricks scored by its
        // own ArcadeActivities as a player's
        public StuntRival Rider2 { get; private set; }
        public void StuntRide()
        {
            if (!P2 || !SplitScreen.P2Ai || !StuntPark.Current) return;
            if (Cruiser) { DestroyImmediate(Cruiser); Cruiser = null; }
            if (Rider2) return;
            P2.GetComponent<VehicleInput>().enabled = false;
            if (!StuntPark.Inside(P2.Body.position, 10f) && P1) PlaceBeside(P2, P1.transform, 4.5f);
            Rider2 = P2.gameObject.AddComponent<StuntRival>(); Rider2.Initialize(P2, "AI", 1, System.Environment.TickCount, false);
        }

        void Cameras()
        {
            bool lr = SplitScreen.LeftRight, one = SplitScreen.OneView;
            cam1.rect = one ? rect1 : lr ? new Rect(0, 0, .5f, 1) : new Rect(0, .5f, 1, .5f);
            if (!one)
            {
                var go = new GameObject("Player 2 camera"); cam2 = go.AddComponent<Camera>(); cam2.CopyFrom(cam1);
                var d1 = cam1.GetUniversalAdditionalCameraData(); var d2 = cam2.GetUniversalAdditionalCameraData();
                d2.renderPostProcessing = d1.renderPostProcessing; d2.antialiasing = d1.antialiasing; d2.renderShadows = d1.renderShadows;
                chase2 = go.AddComponent<ChaseCamera>(); chase2.obstructionMask = chase1 ? chase1.obstructionMask : (LayerMask)1;
                cam2.rect = lr ? new Rect(.5f, 0, .5f, 1) : new Rect(0, 0, 1, .5f);
                chase2.enabled = false; chase2.target = P2.transform; chase2.offset = P2.GetComponent<VehicleConfiguration>().Profile.Camera; chase2.enabled = true; chase2.Snap();
                // Racer 0.94 Part B: player 2's own camera views, answering player 2's device (CameraViews.player)
                views2 = go.AddComponent<CameraViews>(); views2.SetPlayer(2);
                cam2.cullingMask = (mask1 & ~(1 << View1Layer)) | (1 << View2Layer);
            }
            var v1 = cam1.GetComponent<CameraViews>(); if (v1) v1.SetPlayer(1);
            cam1.farClipPlane = one ? far1 : Mathf.Min(far1, FarClip); if (cam2) cam2.farClipPlane = cam1.farClipPlane;
            cam1.cullingMask = mask1 & ~(1 << View2Layer);
            // the wide top / bottom halves: the camera tips down a little in long flights so the landing stays in view (Racer)
            float tilt = lr || one ? 0 : 9; if (chase1) chase1.flightTilt = tilt; if (chase2) chase2.flightTilt = tilt;
            SplitScreen.Views.Clear(); SplitScreen.Views.Add(cam1); if (cam2) SplitScreen.Views.Add(cam2);
        }
        public void Relayout()
        {
            bool lr = SplitScreen.LeftRight, one = SplitScreen.OneView;
            if (cam1) cam1.rect = one ? rect1 : lr ? new Rect(0, 0, .5f, 1) : new Rect(0, .5f, 1, .5f);
            if (cam2) cam2.rect = lr ? new Rect(.5f, 0, .5f, 1) : new Rect(0, 0, 1, .5f);
            float tilt = lr || one ? 0 : 9; if (chase1) chase1.flightTilt = tilt; if (chase2) chase2.flightTilt = tilt;
        }
        // the screen rectangle of a player's half, in GUI coordinates (y down); one view: the whole screen
        public Rect GuiRect(int player)
        {
            var cam = player == 1 ? cam1 : cam2; if (!cam) return new Rect(0, 0, Screen.width, Screen.height);
            var r = cam.pixelRect; return new Rect(r.x, Screen.height - r.yMax, r.width, r.height);
        }

        void Update()
        {
            SplitScreen.Views.Clear(); if (cam1) SplitScreen.Views.Add(cam1); if (cam2 && cam2.enabled) SplitScreen.Views.Add(cam2);
            if (!P2) return;
            // a human player 2 drives when player 1 can (the race's countdown and the Stunt event's start hold both: RaceController, StuntEvent)
            if (!SplitScreen.P2Ai)
            {
                var input = P2.GetComponent<VehicleInput>(); var race = RaceController.Instance;
                bool held = race && race.State == RaceController.Stage.Countdown || StuntEvent.Current && StuntEvent.Current.Holds(P2);
                if (input.enabled == held) input.enabled = !held;
            }
        }

        // ---------- devices (Racer's SplitRace.DeviceChanged) ----------
        void DeviceChanged(InputDevice device, InputDeviceChange change)
        {
            if (change == InputDeviceChange.Removed || change == InputDeviceChange.Disconnected)
            {
                int player = device == SplitScreen.P1Device ? 1 : device == SplitScreen.P2Device && !SplitScreen.P2Ai ? 2 : 0; if (player == 0) return;
                MissingPlayer = player; GameMenus.Instance?.PauseForSplit();
            }
            else if ((change == InputDeviceChange.Added || change == InputDeviceChange.Reconnected) && MissingPlayer != 0 && device is Gamepad pad)
            {
                // the missing player's controller is back (or another controller takes its place)
                if (MissingPlayer == 1 && !(SplitScreen.P1Device is Keyboard)) { SplitScreen.P1Device = pad; P1.GetComponent<VehicleInput>().Bind(pad); }
                else if (MissingPlayer == 2 && !(SplitScreen.P2Device is Keyboard)) { SplitScreen.P2Device = pad; if (P2 && !SplitScreen.P2Ai) P2.GetComponent<VehicleInput>().Bind(pad); }
                MissingPlayer = 0; GameMenus.Instance?.RefreshIfOpen();
            }
        }
        public string MissingText => MissingPlayer == 0 ? null : $"PLAYER {MissingPlayer}'S CONTROLLER IS DISCONNECTED\nReconnect it to resume, or end split screen.";

        public void Teardown()
        {
            InputSystem.onDeviceChange -= DeviceChanged;
            if (StuntEvent.Current) StuntEvent.End();
            var race = RaceController.Instance; if (race && race.State != RaceController.Stage.Free) race.EndRace();
            StopCruise();
            if (P2) { if (TreeTrunks.Instance) TreeTrunks.Instance.Untrack(P2.transform); P2.gameObject.SetActive(false); Destroy(P2.gameObject); P2 = null; }
            if (holder) Destroy(holder);
            if (cam1) { cam1.rect = rect1; cam1.farClipPlane = far1; cam1.cullingMask = mask1; if (chase1) chase1.flightTilt = 0; var v1 = cam1.GetComponent<CameraViews>(); if (v1) v1.SetPlayer(0); }
            if (cam2) Destroy(cam2.gameObject);
            if (P1) P1.GetComponent<VehicleInput>().Bind(null);
            QualitySettings.lodBias = lodBias;
            { var urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset; if (urp && shadowDistance >= 0) urp.shadowDistance = shadowDistance; shadowDistance = -1; }
            SplitScreen.Views.Clear();
        }
        void OnDestroy() { InputSystem.onDeviceChange -= DeviceChanged; }
    }
}
