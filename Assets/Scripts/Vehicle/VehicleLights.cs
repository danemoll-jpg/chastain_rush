using UnityEngine;

namespace Racer
{
    // 0.72 vehicle lamps (visual only): one forward spot light lighting the road ahead, and the vehicle's own headlamp and
    // tail-lamp materials glowing. Level 0 (day) = lights off and lamps exactly as before; it rises at dusk and night.
    // No shadow-casting: the player's lamp is "important" so it always lights the ground near the player.
    public sealed class VehicleLights : MonoBehaviour
    {
        public static float Level;
        Light lamp; ArcadeVehicle car;
        static readonly int Emission = Shader.PropertyToID("_EmissionColor");
        static float shown = -1;

        void Start()
        {
            car = GetComponent<ArcadeVehicle>();
            var box = GetComponent<BoxCollider>();
            float front = box ? box.center.z + box.size.z * .5f : 1.2f, height = box ? box.center.y + box.size.y * .1f : .6f;
            lamp = new GameObject("Headlight").AddComponent<Light>(); lamp.transform.SetParent(transform, false);
            lamp.transform.localPosition = new Vector3(0, height + .35f, front); lamp.transform.localRotation = Quaternion.Euler(7, 0, 0);
            lamp.type = LightType.Spot; lamp.range = 48; lamp.spotAngle = 74; lamp.innerSpotAngle = 34; lamp.color = new Color(1f, .95f, .84f);
            lamp.shadows = LightShadows.None; lamp.enabled = false;
            bool player = GetComponent<VehicleInput>();   // Chastain Rush: no RaceDirector; the player is the car with input
            lamp.renderMode = player ? LightRenderMode.ForcePixel : LightRenderMode.Auto;
        }
        void LateUpdate()
        {
            if (!lamp) return;
            bool pipe = car && ChastainRush.DrainPipe.InsideAny(car.transform.position);   // Chastain Rush, Round 19: headlights on in a drainage pipe
            bool on = (Level > .02f || pipe) && car && car.gameObject.activeInHierarchy;
            lamp.enabled = on; if (on) lamp.intensity = Mathf.Max(9f * Level, pipe ? 7f : 0f);
            // The lamp materials are shared by every vehicle: set once per frame for all.
            if (Mathf.Abs(shown - Level) > .005f) { shown = Level; Lamps(Level); }
        }
        static Material head, tail;
        static void Lamps(float level)
        {
            if (!head || !tail)
                foreach (var r in FindObjectsByType<Renderer>(FindObjectsSortMode.None))
                    foreach (var m in r.sharedMaterials)
                    {
                        if (!m) continue;
                        if (m.name.StartsWith("Garage headlamps")) head = m; else if (m.name.StartsWith("Garage tail lamps")) tail = m;
                    }
            foreach (var (m, c, k) in new[] { (head, new Color(1f, .93f, .75f), 3.2f), (tail, new Color(1f, .05f, .04f), 2.6f) })
            {
                if (!m || !m.HasProperty(Emission)) continue;
                if (level > .02f) { m.EnableKeyword("_EMISSION"); m.SetColor(Emission, c * (k * level)); m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None; }
                else { m.DisableKeyword("_EMISSION"); m.SetColor(Emission, Color.black); }
            }
        }
    }
}
