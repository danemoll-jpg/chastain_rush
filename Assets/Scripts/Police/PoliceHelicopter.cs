using ChastainRush;
using System.Collections.Generic;
using UnityEngine;

namespace Racer
{
    // Chastain Rush, Round 39 (Part D): Racer's helicopter as it is; dense canopy from this map's trees (TreeField), a roof from WeatherEffects, and a
    // runner in the pipe network is never seen from the air.
    // 0.97 Part A: the police helicopter (Getaway, from heat 4; one at most). It follows the runner from above at a delay of
    // about two seconds, so a fast runner can outrun its spotlight, and lights a circle of ground. A runner inside the circle is
    // SEEN (GetawayChase counts the helicopter like a cop that sees), unless something is between: dense tree canopy over the runner,
    // a tunnel or cave roof, a tree-top board or any other solid thing. It cannot catch, only see.
    public sealed class PoliceHelicopter : MonoBehaviour
    {
        // 0.98: Dan saw no helicopter at heat 5: it hovered 58 m up, behind the runner, out of the chase camera's view. It now flies low, ahead and to one
        // side of the runner (in view of the camera), is white with flashing lights and loud; the spotlight still lags by Delay.
        public const float Delay = 2.4f, Altitude = 58, ConeRadius = 21, Speed = 62, FlyHeight = 12, FlyAhead = 50, FlySide = 14;
        GetawayChase boss; GetawayChase.Runner target; Transform rotor, tailRotor, beam; Light spot; AudioSource thump, whoosh; Renderer strobeA, strobeB; float side = 1;
        readonly Queue<(float t, Vector3 p)> trail = new(); Vector3 aim, velocity; bool seesCache, started;
        public Vector3 Aim => aim; public bool Covered { get; private set; } public string Why { get; private set; } = "";
        static AudioClip thumpClip;

        public void Begin(GetawayChase owner, GetawayChase.Runner runner)
        {
            boss = owner; target = runner; var p = runner.body.position; var back = -runner.car.transform.forward; back.y = 0;
            aim = p; transform.position = p + back.normalized * 220 + Vector3.up * (FlyHeight + 25); started = true;
            Build();
        }
        static Material baseMaterial;
        static Material FindBaseMaterial()
        {
            foreach (var car in Object.FindObjectsByType<ArcadeVehicle>(FindObjectsSortMode.None)) foreach (var mr in car.GetComponentsInChildren<MeshRenderer>(true)) { var m = mr.sharedMaterial; if (m && m.shader && m.shader.isSupported && m.HasProperty("_BaseColor") && !m.name.ToLower().Contains("glass") && !m.name.ToLower().Contains("glaz")) return m; }
            return null;
        }
        Renderer Part(string name, PrimitiveType type, Vector3 pos, Vector3 scale, Color colour, Transform parent = null)
        {
            var go = GameObject.CreatePrimitive(type); go.name = name; var c = go.GetComponent<Collider>(); if (c) Destroy(c);
            go.transform.SetParent(parent ? parent : transform, false); go.transform.localPosition = pos; go.transform.localScale = scale;
            var r = go.GetComponent<Renderer>(); r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            // 0.98: a primitive's default material is the pink error shader in the built player (the helicopter was a pink blob): use a lit material the game already ships
            if (baseMaterial == null) baseMaterial = FindBaseMaterial();
            if (baseMaterial) { var m = new Material(baseMaterial); if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", colour); if (m.HasProperty("_Color")) m.SetColor("_Color", colour); r.sharedMaterial = m; } else r.material.color = colour;
            return r;
        }
        void Build()
        {
            var dark = new Color(.93f, .94f, .96f); Part("Body", PrimitiveType.Capsule, Vector3.zero, new Vector3(2.4f, 2.2f, 2.8f), dark).transform.localRotation = Quaternion.Euler(90, 0, 0);
            Part("Cockpit", PrimitiveType.Sphere, new Vector3(0, .25f, 1.9f), new Vector3(2f, 1.8f, 2.4f), new Color(.1f, .22f, .4f));
            Part("Stripe", PrimitiveType.Cube, new Vector3(0, .1f, 0), new Vector3(2.5f, .5f, 3.6f), new Color(.05f, .2f, .75f));
            Part("Tail boom", PrimitiveType.Cube, new Vector3(0, .35f, -4.6f), new Vector3(.45f, .5f, 6.4f), dark);
            Part("Fin", PrimitiveType.Cube, new Vector3(0, 1.2f, -7.4f), new Vector3(.2f, 1.7f, 1.1f), dark);
            Part("Mast", PrimitiveType.Cylinder, new Vector3(0, 1.45f, 0), new Vector3(.25f, .35f, .25f), dark);
            rotor = new GameObject("Rotor").transform; rotor.SetParent(transform, false); rotor.localPosition = new Vector3(0, 1.85f, 0);
            Part("Blade A", PrimitiveType.Cube, Vector3.zero, new Vector3(11f, .06f, .35f), new Color(.08f, .08f, .1f), rotor); Part("Blade B", PrimitiveType.Cube, Vector3.zero, new Vector3(.35f, .06f, 11f), new Color(.08f, .08f, .1f), rotor);
            tailRotor = new GameObject("Tail rotor").transform; tailRotor.SetParent(transform, false); tailRotor.localPosition = new Vector3(.3f, 1.2f, -7.5f);
            Part("Tail blade", PrimitiveType.Cube, Vector3.zero, new Vector3(.06f, 2.1f, .2f), new Color(.1f, .1f, .12f), tailRotor);
            var lamp = new GameObject("Spotlight"); lamp.transform.SetParent(transform, false); lamp.transform.localPosition = new Vector3(0, -1.1f, 1.6f);
            spot = lamp.AddComponent<Light>(); spot.type = LightType.Spot; spot.range = Altitude * 2.4f; spot.spotAngle = 2 * Mathf.Atan(ConeRadius / Altitude) * Mathf.Rad2Deg; spot.intensity = 40; spot.color = new Color(1f, .96f, .85f); spot.shadows = LightShadows.None;
            var beamGo = new GameObject("Beam"); beamGo.transform.SetParent(lamp.transform, false); beam = beamGo.transform;
            var mf = beamGo.AddComponent<MeshFilter>(); mf.sharedMesh = ConeMesh(); var mr = beamGo.AddComponent<MeshRenderer>(); mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var shader = Shader.Find("Sprites/Default"); if (shader) { var m = new Material(shader); m.color = new Color(1, .97f, .85f, .06f); mr.sharedMaterial = m; } else mr.enabled = false;
            thump = gameObject.AddComponent<AudioSource>(); thump.clip = Thump(); thump.loop = true; thump.spatialBlend = 1; thump.minDistance = 70; thump.maxDistance = 600; thump.volume = 1f; thump.dopplerLevel = .4f; thump.Play();
            whoosh = gameObject.AddComponent<AudioSource>(); whoosh.clip = Whoosh(); whoosh.loop = true; whoosh.spatialBlend = 1; whoosh.minDistance = 40; whoosh.maxDistance = 350; whoosh.volume = .7f; whoosh.Play();
        }
        static Mesh ConeMesh()
        {
            var v = new List<Vector3> { Vector3.zero }; var t = new List<int>(); int n = 18;
            for (int i = 0; i < n; i++) { float a = i * Mathf.PI * 2 / n; v.Add(new Vector3(Mathf.Cos(a) * ConeRadius, Mathf.Sin(a) * ConeRadius, Altitude)); }
            for (int i = 0; i < n; i++) { t.Add(0); t.Add(1 + (i + 1) % n); t.Add(1 + i); t.Add(0); t.Add(1 + i); t.Add(1 + (i + 1) % n); }
            var mesh = new Mesh { name = "Helicopter beam" }; mesh.SetVertices(v); mesh.SetTriangles(t, 0); mesh.RecalculateBounds(); return mesh;
        }
        static AudioClip whooshClip;
        static AudioClip Whoosh()
        {
            if (whooshClip) return whooshClip; const int rate = 22050; int n = rate; var d = new float[n]; var rng = new System.Random(9); float lp = 0, lp2 = 0;
            for (int i = 0; i < n; i++) { float t = (float)i / rate; float noise = (float)rng.NextDouble() * 2 - 1; lp += (noise - lp) * .12f; lp2 += (lp - lp2) * .2f; d[i] = lp2 * (.55f + .45f * Mathf.Sin(2 * Mathf.PI * 22 * t)) * 1.6f; }
            float edge = n * .01f; for (int i = 0; i < edge; i++) { float k = i / edge; d[i] *= k; d[n - 1 - i] *= k; }
            whooshClip = AudioClip.Create("Helicopter rotor wash", n, 1, rate, false); whooshClip.SetData(d, 0); return whooshClip;
        }
        static AudioClip Thump()
        {
            if (thumpClip) return thumpClip; const int rate = 22050; int n = rate; var d = new float[n]; var rng = new System.Random(5); float lp = 0;
            for (int i = 0; i < n; i++) { float t = (float)i / rate; float beat = Mathf.Pow(Mathf.Max(0, Mathf.Sin(2 * Mathf.PI * 11 * t)), 6); float noise = (float)rng.NextDouble() * 2 - 1; lp += (noise - lp) * .06f; d[i] = Mathf.Clamp((lp * 2.2f + Mathf.Sin(2 * Mathf.PI * 70 * t) * .4f) * beat * .95f, -1, 1); }
            thumpClip = AudioClip.Create("Helicopter", n, 1, rate, false); thumpClip.SetData(d, 0); return thumpClip;
        }

        void Update()
        {
            if (!started || target == null || target.body == null) return;
            if (target.Done) { var next = boss.Runners.Find(x => !x.Done && x.human); if (next != null) target = next; }
            float now = Time.time; trail.Enqueue((now, target.body.position)); while (trail.Count > 1 && now - trail.Peek().t > Delay + 1.5f) trail.Dequeue();
            Vector3 want = target.body.position; foreach (var (t, p) in trail) if (now - t <= Delay) { want = p; break; }
            aim = Vector3.MoveTowards(aim, want, Speed * 1.4f * Time.deltaTime);
            var lead = target.body.linearVelocity; lead.y = 0; var dir = lead.sqrMagnitude > 4 ? lead.normalized : target.car.transform.forward; dir.y = 0; dir.Normalize();
            var hoverAt = target.body.position + dir * (FlyAhead + lead.magnitude * .9f) +  Vector3.Cross(Vector3.up, dir) * FlySide * side;
            var ground = Physics.Raycast(hoverAt + Vector3.up * 400, Vector3.down, out var hit, 800, ~0, QueryTriggerInteraction.Ignore) ? hit.point.y : target.body.position.y;
            var hover = hoverAt; hover.y = Mathf.Max(ground, target.body.position.y) + FlyHeight;
            transform.position = Vector3.SmoothDamp(transform.position, hover, ref velocity, .9f, Speed * 1.6f);
            var flat = velocity; flat.y = 0; if (flat.sqrMagnitude > 4) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(flat) * Quaternion.Euler(Mathf.Clamp(flat.magnitude * .25f, 0, 10), 0, 0), 2f * Time.deltaTime);
            rotor.Rotate(0, 1500 * Time.deltaTime, 0); tailRotor.Rotate(1800 * Time.deltaTime, 0, 0);
            bool flash = Mathf.Repeat(Time.time * 3.2f, 1) < .5f; if (strobeA) strobeA.enabled = flash; if (strobeB) strobeB.enabled = !flash;
            if (spot) { var from = spot.transform.position; float dist = Mathf.Max(20, Vector3.Distance(from, aim)); spot.transform.rotation = Quaternion.LookRotation(aim - from); spot.range = dist * 1.6f; spot.spotAngle = 2 * Mathf.Atan(ConeRadius / dist) * Mathf.Rad2Deg; spot.intensity = 40 * Mathf.Pow(dist / Altitude, 2); if (beam) beam.localScale = Vector3.one * (dist / Altitude); }
        }

        // from GetawayChase.Tick: does the beam on the ground hold this runner, with nothing between (recompute = a fresh test)
        public bool Sees(GetawayChase.Runner r, bool recompute)
        {
            if (!started || r == null || r.body == null) return false;
            if (!recompute) return seesCache && r == target;
            var to = r.body.position - aim; to.y = 0; bool seen = to.magnitude <= ConeRadius; Why = ""; Covered = false;
            if (seen)
            {
                var eye = aim + Vector3.up * Altitude; var at = r.body.position + Vector3.up * 1.1f; var dir = at - eye; float dist = dir.magnitude;
                var hits = Physics.RaycastAll(eye, dir / dist, dist - .5f, ~0, QueryTriggerInteraction.Ignore);
                foreach (var h in hits) { if (h.collider.GetComponentInParent<ArcadeVehicle>() || h.collider.GetComponentInParent<PoliceHelicopter>()) continue; seen = false; Covered = true; Why = "solid: " + h.collider.name; break; }
                if (seen && Canopy.Dense(r.body.position)) { seen = false; Covered = true; Why = "dense canopy"; }
                if (seen && DrainPipe.Inside(r.body.position, out _) != null) { seen = false; Covered = true; Why = "in the pipes"; }
                if (seen) { var fx = WorldLook.Current ? WorldLook.Current.GetComponent<WeatherEffects>() : null; if (fx && fx.CoveredAt(r.body.position)) { seen = false; Covered = true; Why = "tunnel / cave roof"; } }
            }
            seesCache = seen && r == target; return seen;
        }

        // dense tree canopy over a point (this map's woods): four or more trunks within 7 m (TreeField's placed trees, in a coarse grid)
        static class Canopy
        {
            static Dictionary<Vector2Int, int> grid; const float Cell = 7;
            public static bool Dense(Vector3 p)
            {
                if (grid == null)
                {
                    grid = new(); var f = Object.FindAnyObjectByType<ChastainRush.TreeField>(); if (!f) return false;
                    for (int i = 0; i < f.count; i++) { var t = ChastainRush.TreeField.Unpack(f.data, i); var k = new Vector2Int(Mathf.FloorToInt(t.x / Cell), Mathf.FloorToInt(t.z / Cell)); grid[k] = grid.TryGetValue(k, out var n) ? n + 1 : 1; }
                }
                int c = 0; var k0 = new Vector2Int(Mathf.FloorToInt(p.x / Cell), Mathf.FloorToInt(p.z / Cell));
                for (int x = -1; x <= 1; x++) for (int z = -1; z <= 1; z++) if (grid.TryGetValue(k0 + new Vector2Int(x, z), out var n)) c += n;
                return c >= 9;
            }
        }
    }
}
