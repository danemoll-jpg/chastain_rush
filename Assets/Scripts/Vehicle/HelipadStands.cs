using System.Linq;
using Racer;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ChastainRush
{
    // Round 63 (Dan: "each with a parked helicopter to take (walk-up not needed: drive onto the pad, press a button to swap, as the game's vehicle
    // swap does)"): the game layer of the helipads (MapBuilder.Helipads.cs builds the pads and fills this list). Each pad shows a parked helicopter.
    // In Free Roam, stopped on a pad: A (H on the keyboard) takes the helicopter (the vehicle swapped where the pad's helicopter stands, as the
    // garage swaps it); landed on a pad in the helicopter: A / H leaves it for the vehicle you came in. A pad's parked helicopter is hidden while
    // the player's helicopter is on it. The pads are also places on the map to travel to (found when you come within 150 m of one, at any height).
    public sealed class HelipadStands : MonoBehaviour
    {
        [System.Serializable] public sealed class Pad { public string id, title; public Vector3 centre; public float yaw, half; }
        public Pad[] pads = new Pad[0];
        Transform[] parked; InputAction take; string lastCar = "original"; int lastColour = -1; float nextLook;
        int near = -1; bool canTake, canLeave;
        public static HelipadStands Instance { get; private set; }
        public int Near => near;
        public bool CanTake => canTake; public bool CanLeave => canLeave;

        void Awake() => Instance = this;
        void Start()
        {
            parked = new Transform[pads.Length];
            for (int i = 0; i < pads.Length; i++)
            {
                var p = pads[i]; var go = new GameObject("Parked helicopter " + p.title);
                go.transform.SetPositionAndRotation(p.centre + Vector3.up * HelicopterFlight.SkidDrop, Quaternion.Euler(0, p.yaw, 0));
                var v = VehicleVisual.Build(go.transform, VehicleProfile.Helicopter, null, RiderLook.Player);
                foreach (var t in v.GetComponentsInChildren<Transform>(true).Where(t => t.name == "Rider" || t.name == "Rotor disc").ToArray()) if (t) Destroy(t.gameObject);
                var rotor = v.Find("Main rotor"); if (rotor) rotor.localRotation = Quaternion.Euler(0, 35, 0);   // parked, blades fore and aft-ish
                parked[i] = go.transform;
            }
            take = new InputAction("Take the helicopter", InputActionType.Button); take.AddBinding("<Gamepad>/buttonSouth"); take.AddBinding("<Keyboard>/h"); take.Enable();
        }
        void OnDestroy() { take?.Dispose(); if (Instance == this) Instance = null; }

        static bool Allowed => PoliceKit.FreeRoam && !SplitScreen.Active && !PoliceSession.Running && !StuntEvent.Current && !TrailerMode.Active;

        void Update()
        {
            var car = PoliceKit.Player(1); if (!car) return;
            var ex = Exploration.Instance;
            if (ex && Time.unscaledTime > nextLook)
            {
                nextLook = Time.unscaledTime + 1f;
                foreach (var p in pads)
                {
                    if (!ex.destinations.Any(d => d.id == p.id))
                        ex.destinations = ex.destinations.Concat(new[] { new Exploration.Destination { id = p.id, title = p.title, position = p.centre, yaw = p.yaw, fixedY = true } }).ToArray();
                    if (Vector2.Distance(new Vector2(p.centre.x, p.centre.z), new Vector2(car.Body.position.x, car.Body.position.z)) < 150f) ex.Find(p.id);
                }
            }
            var cfg = car.GetComponent<VehicleConfiguration>(); var sw = car.GetComponent<VehicleSwitch>();
            bool heli = HelicopterFlight.Flying(car); var hf = heli ? car.GetComponent<HelicopterFlight>() : null;
            if (!heli && cfg) { lastCar = cfg.profileId; if (sw) lastColour = sw.Colour; }
            near = PadAt(car.Body.position);
            for (int i = 0; i < pads.Length; i++)
            {
                var d = car.Body.position - pads[i].centre;
                if (parked != null && parked[i]) { bool show = !(heli && new Vector2(d.x, d.z).magnitude < 14f && d.y < 40f); if (parked[i].gameObject.activeSelf != show) parked[i].gameObject.SetActive(show); }
            }
            canTake = Allowed && near >= 0 && !heli && car.Body.linearVelocity.magnitude < 8f;
            canLeave = Allowed && near >= 0 && heli && hf && hf.Landed && !hf.Crashed;
            bool free = !MenuInput.Blocked && !GameMenus.Open && !WorldMap.OwnsInput && Time.timeScale > 0;
            if (free && (canTake || canLeave) && take.WasPressedThisFrame()) Swap(car, sw);
        }

        int PadAt(Vector3 at)
        {
            for (int i = 0; i < pads.Length; i++) { var d = at - pads[i].centre; if (new Vector2(d.x, d.z).magnitude < pads[i].half + 1f && d.y > -2f && d.y < 5f) return i; }
            return -1;
        }
        // the swap itself (the button, or a check)
        public bool Swap(ArcadeVehicle car, VehicleSwitch sw)
        {
            near = PadAt(car.Body.position);   // where the vehicle is now (a fast travel may have moved it since the last frame)
            if (near < 0 || !sw) return false;
            var p = pads[near]; var respawn = car.GetComponent<VehicleRespawn>();
            if (!HelicopterFlight.Flying(car))
            {
                sw.Select(VehicleProfile.Helicopter.Id);
                if (respawn) respawn.PlaceAt(p.centre + Vector3.up * (HelicopterFlight.SkidDrop - .45f), p.yaw);
                Debug.Log($"[Round63] took the helicopter at {p.title} (came in the {VehicleProfile.Find(lastCar).Name})");
            }
            else
            {
                string back = VehicleProfile.IndexOf(lastCar) >= 0 && !VehicleProfile.Find(lastCar).Flies ? lastCar : "original";
                sw.Select(back); if (lastColour >= 0) sw.SetColour(lastColour);
                // beside the parked helicopter, at the pad's edge, facing out
                var f = Quaternion.Euler(0, p.yaw, 0) * Vector3.forward;
                if (respawn) respawn.PlaceAt(p.centre + f * (p.half - 3f) + Vector3.up * .1f, p.yaw);
                Debug.Log($"[Round63] left the helicopter at {p.title} for the {VehicleProfile.Find(back).Name}");
            }
            if (RaceController.Instance) RaceController.Instance.NotifyTeleported();
            return true;
        }

        static GUIStyle style;
        void OnGUI()
        {
            if (Event.current.type != EventType.Repaint || !(canTake || canLeave) || GameMenus.Open || WorldMap.IsOpen || TrailerMode.HudHidden) return;
            var car = PoliceKit.Player(1); bool pad = KeyHints.Pad;   // Round 74 (B)
            if (style == null) { style = new GUIStyle(GUI.skin.label) { fontSize = 24, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter }; style.normal.textColor = Color.white; }
            float s = Mathf.Max(1f, Screen.height / 800f); var old = GUI.matrix; GUI.matrix = Matrix4x4.Scale(new Vector3(s, s, 1f));
            float W = Screen.width / s, H = Screen.height / s; var r = new Rect(W / 2 - 260, H * .70f, 520, 44);
            var c = GUI.color; GUI.color = new Color(0, 0, 0, .5f); GUI.DrawTexture(r, Texture2D.whiteTexture); GUI.color = c;
            GUI.Label(r, (pad ? "A" : "H") + (canTake ? ":  take the helicopter" : ":  leave the helicopter"), style);
            GUI.matrix = old;
        }
    }
}
