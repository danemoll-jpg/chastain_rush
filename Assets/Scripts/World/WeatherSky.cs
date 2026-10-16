using System.Collections.Generic;
using UnityEngine;

namespace Racer
{
    // 0.74 sky details driven by WeatherEffects (visual only, no colliders):
    //  LightningBolts - a forked bolt from the cloud base to the ground in a chosen direction and distance, shown for the
    //                   strike's one or two pulses (visible at any time of day; independent of the "Lightning flashes"
    //                   setting, which only governs the full-screen flash);
    //  MoonDisc       - the moon with the day's phase, where the night light comes from;
    //  DawnMist       - light ground mist: soft flat puffs lying in the low ground around the camera (Dawn).
    // Chastain Rush, Round 35 (Part C): Racer's WeatherSky, ported; the bolt's top lifted to this map's height (SkyClouds.Lift).
    public sealed class LightningBolts : MonoBehaviour
    {
        const int Strips = 6;
        readonly List<LineRenderer> lines = new();
        Material material; float shownAt = -100, strength, daylight; int pulses;
        public int Shown { get; private set; }
        public Vector3 LastBase { get; private set; }
        public static bool TestHold;
        public string Describe() => string.Join("; ", lines.ConvertAll(l => $"{l.name} on={l.enabled} n={l.positionCount} w={l.widthMultiplier:F1} b={l.bounds.center}/{l.bounds.size} layer={l.gameObject.layer} active={l.gameObject.activeInHierarchy}"));

        void Awake()
        {
            var asset = Resources.Load<Material>("LightningBolt");
            material = asset ? new Material(asset) { name = "Lightning bolt" } : new Material(Shader.Find("Sprites/Default"));
            for (int i = 0; i < Strips; i++)
            {
                var go = new GameObject(i == 0 ? "Bolt main channel" : "Bolt branch"); go.transform.SetParent(transform, false);
                var l = go.AddComponent<LineRenderer>(); l.sharedMaterial = material; l.useWorldSpace = true; l.textureMode = LineTextureMode.Stretch;
                l.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; l.receiveShadows = false; l.alignment = LineAlignment.View; l.numCapVertices = 0; l.enabled = false;
                lines.Add(l);
            }
        }
        // A new bolt: azimuth (degrees), horizontal distance from the camera (m), strength 0..1 (near = 1), pulse count.
        public void Strike(Vector3 cam, float azimuth, float distance, float s, int pulseCount, float cloudBase, float day = 0)
        {
            var dir = Quaternion.Euler(0, azimuth, 0) * Vector3.forward;
            var foot = cam + dir * distance; foot.y = cam.y - 40;
            if (Physics.Raycast(new Vector3(foot.x, 700 + SkyClouds.Lift, foot.z), Vector3.down, out var h, 1400, ~0, QueryTriggerInteraction.Ignore)) foot.y = h.point.y;
            // the channel shows from just under the lowest clouds (410 m), so nearer clouds never hide it (cloudBase caps it)
            var top = new Vector3(foot.x + Random.Range(-60f, 60f), Mathf.Min(cloudBase, Mathf.Clamp(cam.y + 230, 330 + SkyClouds.Lift, 395 + SkyClouds.Lift)), foot.z + Random.Range(-60f, 60f));   // Chastain Rush: + SkyClouds.Lift
            LastBase = foot;
            // 0.76: thick enough to read at any distance (about twice the 0.74 width)
            float width = Mathf.Clamp(distance * .011f, 3.5f, 14f);
            var main = Channel(top, foot, 14, distance * .035f);
            Set(lines[0], main, width, 1);
            int branches = Random.Range(2, Strips);
            for (int i = 1; i < Strips; i++)
            {
                if (i > branches) { lines[i].enabled = false; lines[i].positionCount = 0; continue; }
                int from = Random.Range(2, main.Count - 4); var start = main[from];
                var away = Vector3.Cross(Vector3.up, dir).normalized * Random.Range(-1f, 1f) + dir * Random.Range(-.3f, .3f);
                var end = start + away * Random.Range(.12f, .3f) * (top.y - foot.y) + Vector3.down * Random.Range(.15f, .35f) * (top.y - foot.y);
                Set(lines[i], Channel(start, end, 7, distance * .025f), width * .55f, .6f);
            }
            shownAt = Time.time; strength = s; pulses = pulseCount; daylight = day; Shown++;
        }
        static List<Vector3> Channel(Vector3 a, Vector3 b, int n, float jitter)
        {
            var pts = new List<Vector3>(n + 1); var side = Vector3.Cross(Vector3.up, (b - a).normalized).normalized; if (side.sqrMagnitude < .1f) side = Vector3.right;
            for (int i = 0; i <= n; i++)
            {
                float t = i / (float)n; var p = Vector3.Lerp(a, b, t);
                if (i > 0 && i < n) p += side * Random.Range(-1f, 1f) * jitter + Vector3.up * Random.Range(-.4f, .4f) * jitter;
                pts.Add(p);
            }
            return pts;
        }
        static void Set(LineRenderer l, List<Vector3> pts, float width, float brightness)
        {
            l.positionCount = pts.Count; l.SetPositions(pts.ToArray()); l.widthMultiplier = width;
            var c = new Color(.82f, .88f, 1f, brightness); l.startColor = c; l.endColor = c; l.enabled = true;
        }
        void LateUpdate()
        {
            float age = Time.time - shownAt;
            // 0.76: the channel stays lit through the stroke (0.2 s), flickers back for the return stroke when there is one,
            // and fades as an afterglow by about 0.9 s - long enough to register by day.
            float f = age < 0 ? 0 : age < .03f ? age / .03f : age < .2f ? 1 : 0;
            if (pulses > 1 && age >= .26f && age < .4f) f = Mathf.Max(f, .85f);
            if (age >= .2f && age < 1.1f) f = Mathf.Max(f, .55f * Mathf.Exp(-(age - .2f) / .22f));
            if (TestHold) f = 1;// evidence only: keep the last bolt lit
            bool on = f > .01f;
            // HDR so the bolt blooms; strength carries the distance (a far bolt is dimmer). Brighter by day, against the overcast.
            if (on) material.SetFloat("_Intensity", f * Mathf.Lerp(4f, 10f, strength) * Mathf.Lerp(1f, 1.6f, daylight));
            foreach (var l in lines) if (l.enabled != on && (on ? l.positionCount > 0 : true)) l.enabled = on && l.positionCount > 0;
        }
        void OnDestroy() { if (material) Destroy(material); }
    }

    public sealed class MoonDisc : MonoBehaviour
    {
        Material material; MeshRenderer view;
        public float Phase { get; private set; } = .5f;
        public bool Visible => view && view.enabled;
        void Awake()
        {
            var asset = Resources.Load<Material>("MoonPhase");
            material = asset ? new Material(asset) { name = "Moon" } : null;
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad); go.name = "Moon disc"; Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(transform, false); view = go.GetComponent<MeshRenderer>(); view.sharedMaterial = material;
            view.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; view.receiveShadows = false; view.enabled = false;
        }
        // elevation/azimuth of the moon (degrees, as for the sun light), phase 0..1, visibility 0..1.
        public void Show(Camera cam, float elevation, float azimuth, float phase, float visibility)
        {
            Phase = phase;
            bool on = material && visibility > .01f && elevation > -1;
            view.enabled = on; if (!on) return;
            var toMoon = -(Quaternion.Euler(elevation, azimuth, 0) * Vector3.forward);
            float d = Mathf.Min(900, cam.farClipPlane * .8f);
            var t = view.transform; t.position = cam.transform.position + toMoon * d; t.rotation = Quaternion.LookRotation(toMoon, Vector3.up) * Quaternion.Euler(0, 180, 0);
            t.localScale = Vector3.one * d * .045f;
            material.SetFloat("_Phase", phase); material.SetFloat("_Visibility", visibility);
        }
        void OnDestroy() { if (material) Destroy(material); }
    }

    public sealed class DawnMist : MonoBehaviour
    {
        const int Count = 90; const float Range = 95;
        ParticleSystem ps; ParticleSystemRenderer psr; readonly ParticleSystem.Particle[] parts = new ParticleSystem.Particle[Count];
        readonly Vector3[] spots = new Vector3[Count]; readonly bool[] placed = new bool[Count]; Material material; int next;
        public float Level { get; private set; }
        void Awake()
        {
            var go = new GameObject("Dawn ground mist"); go.transform.SetParent(transform, false);
            ps = go.AddComponent<ParticleSystem>(); ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main; main.loop = false; main.playOnAwake = false; main.simulationSpace = ParticleSystemSimulationSpace.World; main.maxParticles = Count; main.startLifetime = 1e6f; main.startSpeed = 0;
            var em = ps.emission; em.enabled = false;
            psr = go.GetComponent<ParticleSystemRenderer>(); psr.renderMode = ParticleSystemRenderMode.HorizontalBillboard; psr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; psr.receiveShadows = false;
            psr.maxParticleSize = 50; psr.sortingFudge = 10;
            var snow = Resources.Load<Material>("WeatherSnow");
            material = snow ? new Material(snow) { name = "Dawn mist" } : null; psr.sharedMaterial = material;
            ps.Play(); psr.enabled = false;
        }
        public void Set(Material dotSource, Camera cam, float level, Color fog)
        {
            Level = level;
            if (material && dotSource && material.HasProperty("_BaseMap") && !material.GetTexture("_BaseMap")) material.SetTexture("_BaseMap", dotSource.GetTexture("_BaseMap"));
            bool on = level > .02f && material; psr.enabled = on; if (!on) return;
            var c = cam.transform.position;
            // a few spots re-placed every frame: kept only in low ground (lower than the ground 18 m around it)
            for (int k = 0; k < 4; k++)
            {
                int i = next = (next + 1) % Count;
                if (placed[i] && new Vector2(spots[i].x - c.x, spots[i].z - c.z).magnitude < Range) continue;
                var p = c + new Vector3(Random.Range(-Range, Range), 0, Random.Range(-Range, Range));
                if (!Ground(p, out float y)) continue;
                float around = 0; int n = 0;
                for (int a = 0; a < 4; a++) { var q = p + Quaternion.Euler(0, a * 90 + 45, 0) * Vector3.forward * 18; if (Ground(q, out float qy)) { around += qy; n++; } }
                if (n < 3 || y > around / n - .6f) { placed[i] = false; continue; }
                spots[i] = new Vector3(p.x, y + .9f, p.z); placed[i] = true;
            }
            int m = 0; var col = new Color(Mathf.Lerp(fog.r, 1, .35f), Mathf.Lerp(fog.g, 1, .35f), Mathf.Lerp(fog.b, 1, .35f), .2f * level);
            for (int i = 0; i < Count; i++)
            {
                if (!placed[i]) continue;
                parts[m].position = spots[i]; parts[m].startSize = 16 + (i * 7 % 11); parts[m].startColor = col; parts[m].rotation = i * 37;
                parts[m].remainingLifetime = parts[m].startLifetime = 1e6f; m++;
            }
            ps.SetParticles(parts, m);
        }
        static bool Ground(Vector3 p, out float y)
        {
            y = 0;
            if (!Physics.Raycast(new Vector3(p.x, p.y + 120, p.z), Vector3.down, out var h, 300, ~0, QueryTriggerInteraction.Ignore) || h.collider.attachedRigidbody) return false;
            y = h.point.y; return true;
        }
        void OnDestroy() { if (material) Destroy(material); }
    }
}
