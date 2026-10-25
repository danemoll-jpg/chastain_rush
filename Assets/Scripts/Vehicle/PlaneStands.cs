using System.Collections.Generic;
using System.Linq;
using Racer;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ChastainRush
{
    // Round 69 (TODO.md: "two or three planes parked on the airport's apron (drive up and swap, as the helicopter pads: A / H)"): the game layer of
    // the planes at Cobb County International. Three Sky Hoppers stand on the south apron (map.json aeroways, OSM apron 30655130), nose to the
    // taxiway. In Free Roam, stopped beside one: A (H on the keyboard) takes it (the vehicle swapped where that plane stands); stopped on the ground
    // in the plane near a stand: A / H leaves it for the vehicle you came in. A stand's parked plane is hidden while the player's plane is on it.
    // Built at run time (the heights from the ground under each stand), so no map build step. Also the apron's one plane that now and then taxis
    // out, takes off, flies a circuit round the airport and lands (AirportCircuit, visual only: nothing to collide with).
    public sealed class PlaneStands : MonoBehaviour
    {
        public static readonly (string title, Vector2 at, float yaw)[] Stands =
        {
            ("Apron, west", new Vector2(-3486f, -2074f), 0f), ("Apron, middle", new Vector2(-3446f, -2072f), 0f), ("Apron, east", new Vector2(-3406f, -2070f), 0f),
        };
        readonly List<(Vector3 at, float yaw, Transform shown)> stands = new();
        InputAction take; string lastCar = "original"; int lastColour = -1;
        int near = -1; bool canTake, canLeave;
        public static PlaneStands Instance { get; private set; }
        public bool CanTake => canTake; public bool CanLeave => canLeave; public int Near => near;
        public IReadOnlyList<(Vector3 at, float yaw, Transform shown)> Spots => stands;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot() { if (!Instance && FindAnyObjectByType<HelipadStands>()) new GameObject("Plane stands").AddComponent<PlaneStands>(); }

        void Awake() => Instance = this;
        void Start()
        {
            Physics.SyncTransforms();
            foreach (var (title, at, yaw) in Stands)
            {
                if (!HelicopterFlight.GroundUnder(new Vector3(at.x, 2500f, at.y), out var hit)) continue;
                var go = new GameObject("Parked plane " + title); go.transform.SetPositionAndRotation(hit.point + Vector3.up * PlaneFlight.WheelDrop, Quaternion.Euler(0, yaw, 0));
                var v = VehicleVisual.Build(go.transform, VehicleProfile.Plane, null, RiderLook.Player);
                foreach (var t in v.GetComponentsInChildren<Transform>(true).Where(t => t.name == "Rider" || t.name == "Rotor disc").ToArray()) if (t) Destroy(t.gameObject);
                var p = v.Find("Propeller"); if (p) p.localRotation = Quaternion.Euler(0, 0, 30f + 50f * stands.Count);
                stands.Add((hit.point, yaw, go.transform));
            }
            take = new InputAction("Take the plane", InputActionType.Button); take.AddBinding("<Gamepad>/buttonSouth"); take.AddBinding("<Keyboard>/h"); take.Enable();
            var circuit = new GameObject("Airport circuit").AddComponent<AirportCircuit>(); circuit.transform.SetParent(transform, false);
            Debug.Log($"[Round69] {stands.Count} planes parked on the apron");
        }
        void OnDestroy() { take?.Dispose(); if (Instance == this) Instance = null; }

        static bool Allowed => PoliceKit.FreeRoam && !SplitScreen.Active && !PoliceSession.Running && !StuntEvent.Current && !TrailerMode.Active;

        void Update()
        {
            var car = PoliceKit.Player(1); if (!car) return;
            var cfg = car.GetComponent<VehicleConfiguration>(); var sw = car.GetComponent<VehicleSwitch>();
            var plane = PlaneFlight.On(car) ? car.GetComponent<PlaneFlight>() : null;
            if (!plane && !HelicopterFlight.Flying(car) && cfg) { lastCar = cfg.profileId; if (sw) lastColour = sw.Colour; }
            near = StandAt(car.Body.position);
            for (int i = 0; i < stands.Count; i++)
            {
                var d = car.Body.position - stands[i].at; bool show = !(plane && new Vector2(d.x, d.z).magnitude < 12f && d.y < 20f);
                if (stands[i].shown && stands[i].shown.gameObject.activeSelf != show) stands[i].shown.gameObject.SetActive(show);
            }
            canTake = Allowed && near >= 0 && !plane && !HelicopterFlight.Flying(car) && car.Body.linearVelocity.magnitude < 6f;
            canLeave = Allowed && near >= 0 && plane && !plane.Airborne && !plane.Crashed && car.Body.linearVelocity.magnitude < 4f;
            bool free = !MenuInput.Blocked && !GameMenus.Open && !WorldMap.OwnsInput && Time.timeScale > 0;
            if (free && (canTake || canLeave) && take.WasPressedThisFrame()) Swap(car, sw);
        }
        int StandAt(Vector3 at)
        {
            for (int i = 0; i < stands.Count; i++) { var d = at - stands[i].at; if (new Vector2(d.x, d.z).magnitude < 13f && d.y > -2f && d.y < 6f) return i; }
            return -1;
        }
        public bool Swap(ArcadeVehicle car, VehicleSwitch sw)
        {
            near = StandAt(car.Body.position);
            if (near < 0 || !sw) return false;
            var s = stands[near]; var respawn = car.GetComponent<VehicleRespawn>();
            if (!PlaneFlight.On(car))
            {
                PlaneFlight.PlacedByStand = true; sw.Select(VehicleProfile.Plane.Id);
                if (respawn) respawn.PlaceAt(s.at + Vector3.up * (PlaneFlight.WheelDrop + .1f), s.yaw);
                Debug.Log($"[Round69] took the plane at the stand {near} (came in the {VehicleProfile.Find(lastCar).Name})");
            }
            else
            {
                string back = VehicleProfile.IndexOf(lastCar) >= 0 && !VehicleProfile.Find(lastCar).Flies ? lastCar : "original";
                sw.Select(back); if (lastColour >= 0) sw.SetColour(lastColour);
                var side = Quaternion.Euler(0, s.yaw, 0) * Vector3.right;
                if (respawn) respawn.PlaceAt(s.at + side * 8f + Vector3.up * .3f, s.yaw);
                Debug.Log($"[Round69] left the plane at the stand {near} for the {VehicleProfile.Find(back).Name}");
            }
            if (RaceController.Instance) RaceController.Instance.NotifyTeleported();
            return true;
        }

        static GUIStyle style;
        void OnGUI()
        {
            if (Event.current.type != EventType.Repaint || !(canTake || canLeave) || GameMenus.Open || WorldMap.IsOpen || TrailerMode.HudHidden) return;
            var car = PoliceKit.Player(1); var input = car ? car.GetComponent<VehicleInput>() : null; bool pad = input && input.UsingGamepad;
            if (style == null) { style = new GUIStyle(GUI.skin.label) { fontSize = 24, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter }; style.normal.textColor = Color.white; }
            float s = Mathf.Max(1f, Screen.height / 800f); var old = GUI.matrix; GUI.matrix = Matrix4x4.Scale(new Vector3(s, s, 1f));
            float W = Screen.width / s, H = Screen.height / s; var r = new Rect(W / 2 - 260, H * .70f, 520, 44);
            var c = GUI.color; GUI.color = new Color(0, 0, 0, .5f); GUI.DrawTexture(r, Texture2D.whiteTexture); GUI.color = c;
            GUI.Label(r, (pad ? "A" : "H") + (canTake ? ":  take the plane" : ":  leave the plane"), style);
            GUI.matrix = old;
        }
    }
}
