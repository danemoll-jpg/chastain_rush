using Racer;
using UnityEngine;

namespace ChastainRush
{
    // Round 48: Racer 0.74's Free Roam waypoint (set on the map anywhere in the world): a tall beam at the waypoint (additive, no fog, so it
    // reads by day and by night from far away) with a ring on the ground, and a HUD line with the distance and a direction arrow. Within 15 m:
    // "Destination reached", and the waypoint clears. Straight-line guidance only; nothing in races; not saved. Racer draws its HUD line in its
    // race HUD; here it is drawn by this component (IMGUI, as this game's HUDs), hidden in Trailer Mode.
    public sealed class WaypointGuide : MonoBehaviour
    {
        public const float ArriveDistance = 15f;
        Exploration ex; LineRenderer beam, ring; Material material; float reachedUntil; GUIStyle style;
        public bool Active { get; private set; }
        public float Distance { get; private set; }
        public float Bearing { get; private set; }   // degrees, relative to the vehicle's heading (0 ahead, + right)
        public int Arrivals { get; private set; }
        void Awake()
        {
            ex = GetComponent<Exploration>();
            material = new Material(Shader.Find("Sprites/Default")) { name = "Waypoint beam" };
            beam = Line("Waypoint beam", 2, false, 1.8f); ring = Line("Waypoint ring", 40, true, .45f);
        }
        LineRenderer Line(string name, int count, bool loop, float width)
        {
            var go = new GameObject(name); go.transform.SetParent(transform, false);
            var l = go.AddComponent<LineRenderer>(); l.sharedMaterial = material; l.useWorldSpace = true; l.positionCount = count; l.loop = loop; l.widthMultiplier = width;
            l.alignment = LineAlignment.View; l.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; l.receiveShadows = false; l.enabled = false;
            l.startColor = new Color(1f, .82f, .2f, 1f); l.endColor = new Color(1f, .82f, .2f, loop ? 1f : .25f);
            return l;
        }
        void LateUpdate()
        {
            var car = EggHunt.Player();
            Active = ex && ex.Waypoint.HasValue && car && EggHunt.Hunting;
            beam.enabled = ring.enabled = Active; if (!Active) return;
            var w = ex.Waypoint.Value; var p = car.Body.position;
            var delta = Vector3.ProjectOnPlane(w - p, Vector3.up); Distance = delta.magnitude;
            var fwd = Vector3.ProjectOnPlane(car.transform.forward, Vector3.up);
            Bearing = Vector3.SignedAngle(fwd.sqrMagnitude > 1e-4f ? fwd : Vector3.forward, delta.sqrMagnitude > 1e-4f ? delta : fwd, Vector3.up);
            beam.SetPosition(0, w); beam.SetPosition(1, w + Vector3.up * 160f);
            float pulse = 2.6f + .5f * Mathf.Sin(Time.unscaledTime * 3f);
            for (int i = 0; i < ring.positionCount; i++) { float a = i * Mathf.PI * 2f / ring.positionCount; ring.SetPosition(i, w + new Vector3(Mathf.Cos(a) * pulse, .25f, Mathf.Sin(a) * pulse)); }
            if (Distance <= ArriveDistance) { Arrivals++; ex.ClearWaypoint(); ex.Message = "Destination reached"; reachedUntil = Time.unscaledTime + 3f; beam.enabled = ring.enabled = Active = false; }
        }
        public string Hud => Active ? $"WAYPOINT   {DisplayUnits.Distance(Distance)}" : Time.unscaledTime < reachedUntil ? "DESTINATION REACHED" : "";
        void OnGUI()
        {
            if (TrailerMode.HudHidden || GameMenus.Open || WorldMap.IsOpen) return;
            string s = Hud; if (string.IsNullOrEmpty(s)) return;
            style ??= new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(22 * Screen.height / 1080f), alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            float k = Screen.height / 1080f; var r = new Rect(Screen.width / 2f - 240 * k, 206 * k, 480 * k, 40 * k);
            GUI.color = new Color(0, 0, 0, .55f); GUI.DrawTexture(new Rect(r.x + 60 * k, r.y, r.width - 120 * k, r.height), Texture2D.whiteTexture); GUI.color = new Color(1f, .85f, .3f);
            string arrow = Active ? (Mathf.Abs(Bearing) < 22.5f ? "↑" : Mathf.Abs(Bearing) > 157.5f ? "↓" : Bearing > 0 ? (Bearing < 67.5f ? "↗" : Bearing < 112.5f ? "→" : "↘") : (Bearing > -67.5f ? "↖" : Bearing > -112.5f ? "←" : "↙")) + "  " : "";
            GUI.Label(r, arrow + s, style); GUI.color = Color.white;
        }
        void OnDestroy() { if (material) Destroy(material); }
    }
}
