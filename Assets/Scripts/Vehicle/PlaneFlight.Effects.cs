using Racer;
using UnityEngine;

namespace ChastainRush
{
    // Round 69: what the plane shows and sounds like, the map's edge, and the readout. The propeller turns with the throttle (the blur disc at speed);
    // the engine: a two-blade propeller's buzz made here (EngineClip), its pitch with the throttle and the speed, a wind with the speed (the
    // helicopter's wash clip pitched up). Past the map's edge (Round 66's boundary): LEAVING THE SURVEY AREA from WarnFrom inside it; past it a
    // headwind slows it and it banks round by itself in a wide turn back toward the map (no crash, no teleport). The readout (bottom left): height
    // over the ground, speed, heading, climb, throttle.
    public sealed partial class PlaneFlight
    {
        public float Outside { get; private set; } = -1e9f;
        public float MaxOutside { get; set; } = -1e9f;   // checks
        public bool Leaving => Outside > -HelicopterFlight.WarnFrom && Airborne;

        // past the edge: bank toward the map (35 to 45 degrees, the way that turns it back soonest), the stick's bank overruled
        void Boundary(Vector3 pos, ref float roll)
        {
            var land = MapEdge.Flight; if (land == null) { Outside = -1e9f; return; }   // Round 79: the flight boundary (MapEdge.Flight: the land's outer rectangle and 650 m)
            float o = -land.Distance(pos.x, pos.z);
            Outside = o; MaxOutside = Mathf.Max(MaxOutside, o);
            if (o <= 0f) return;
            var c = MapEdge.Map.center; float want = Mathf.Atan2(c.x - pos.x, c.y - pos.z) * Mathf.Rad2Deg;
            float off = Mathf.DeltaAngle(yawDeg, want);
            if (Mathf.Abs(off) < 8f) { roll = Mathf.MoveTowards(roll, 0f, 40f * Time.fixedDeltaTime); return; }
            float bank = Mathf.Sign(off) * Mathf.Lerp(35f, 45f, Mathf.Clamp01(o / 400f));
            roll = Mathf.MoveTowards(roll, bank, 50f * Time.fixedDeltaTime);
            if (pitchDeg < -5f) pitchDeg = Mathf.MoveTowards(pitchDeg, 0f, 20f * Time.fixedDeltaTime);
        }

        AudioSource engine, wind; static AudioClip engineClip; static Material discMaterial; float shownAt;
        // a looping propeller buzz: the firing pulses of a small four-cylinder at idle-ish (pitched up by the throttle), the blades' beat over it
        static AudioClip EngineClip()
        {
            if (engineClip) return engineClip;
            int rate = 22050, n = rate; var d = new float[n]; var rnd = new System.Random(69);
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)rate;
                float fire = Mathf.Pow(Mathf.Abs(Mathf.Sin(Mathf.PI * 50f * t)), 6f), beat = .5f + .5f * Mathf.Sin(2f * Mathf.PI * 25f * t);
                float tone = Mathf.Sin(2f * Mathf.PI * 100f * t) * .35f + Mathf.Sin(2f * Mathf.PI * 200f * t) * .18f + Mathf.Sin(2f * Mathf.PI * 300f * t) * .08f;
                d[i] = (fire * .55f + tone * (.6f + .4f * beat) + ((float)rnd.NextDouble() - .5f) * .12f) * .45f;
            }
            engineClip = AudioClip.Create("Plane engine", n, 1, rate, false); engineClip.SetData(d, 0); return engineClip;
        }
        void StartEffects()
        {
            shownAt = Time.unscaledTime;
            if (!engine)
            {
                // Round 71: another plane (a race's AI pilot, a replay's) is heard where it is, not in the player's ears
                bool mine = Player == this;
                AudioSource Src(AudioClip clip) { var a = gameObject.AddComponent<AudioSource>(); a.clip = clip; a.loop = true; a.spatialBlend = mine ? 0 : 1; a.dopplerLevel = mine ? 0 : .6f; a.minDistance = 25f; a.maxDistance = 700f; a.rolloffMode = AudioRolloffMode.Linear; a.volume = 0; a.priority = mine ? 90 : 140; a.Play(); return a; }
                engine = Src(EngineClip()); wind = Src(PoliceHelicopter.Whoosh()); wind.pitch = 1.9f;
            }
            if (discRenderer)
            {
                if (!discMaterial) { var glass = Resources.Load<Material>("VehicleGlazing"); discMaterial = glass ? new Material(glass) { name = "Prop disc" } : null; if (discMaterial) { var c = new Color(.12f, .12f, .14f, .12f); discMaterial.color = c; if (discMaterial.HasProperty("_BaseColor")) discMaterial.SetColor("_BaseColor", c); discMaterial.SetFloat("_Smoothness", 0); } }
                if (discMaterial) discRenderer.sharedMaterial = discMaterial;
                discRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; discRenderer.enabled = false;
            }
        }
        void StopEffects() { foreach (var a in new[] { engine, wind }) if (a) Destroy(a); engine = wind = null; }

        void Effects(float dt)
        {
            if (!car) return;
            if (car.Puppet) Throttle = Mathf.MoveTowards(Throttle, ReplayViewer.ThrottleOf(car), 2f * dt);   // Round 74 (A1): a replay's plane, from its recorded throttle
            float speed = car.Puppet ? car.PuppetSpeed : car.Body.linearVelocity.magnitude, run = Crashed ? 0f : Mathf.Max(.18f, Throttle);
            float rps = run * 38f; propAngle = (propAngle + rps * 360f * dt) % 360f;
            if (prop) prop.localRotation = Quaternion.Euler(0, 0, propAngle);
            if (discRenderer) discRenderer.enabled = rps > 14f;
            float vol = VehicleAudio.Volume;
            if (engine) { engine.pitch = Crashed ? .3f : .62f + .75f * Throttle + .15f * Mathf.Clamp01(speed / Top); engine.volume = vol * (Crashed ? 0f : .45f + .35f * Throttle); }
            if (wind) wind.volume = vol * Mathf.Clamp01((speed - 20f) / 90f) * .55f;
        }

        // ---------- the readout ----------
        static GUIStyle big, small, warn;
        void OnGUI()
        {
            if (Event.current.type != EventType.Repaint || Player != this || GameMenus.Open || WorldMap.IsOpen || TrailerMode.HudHidden || !car) return;
            if (big == null)
            {
                big = new GUIStyle(GUI.skin.label) { fontSize = 26, fontStyle = FontStyle.Bold }; big.normal.textColor = Color.white;
                small = new GUIStyle(GUI.skin.label) { fontSize = 17 }; small.normal.textColor = new Color(.9f, .92f, .95f);
                warn = new GUIStyle(GUI.skin.label) { fontSize = 30, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter }; warn.normal.textColor = new Color(1f, .82f, .3f);
            }
            float s = Mathf.Max(1f, Screen.height / 800f); var old = GUI.matrix; GUI.matrix = Matrix4x4.Scale(new Vector3(s, s, 1f));
            float W = Screen.width / s, Hh = Screen.height / s;
            var v = car.Body.linearVelocity; float heading = Mathf.Repeat(transform.eulerAngles.y, 360f); string[] points = { "N", "NE", "E", "SE", "S", "SW", "W", "NW" };
            var r = new Rect(24, Hh - 150, 500, 126);
            var oc = GUI.color; GUI.color = new Color(0, 0, 0, .45f); GUI.DrawTexture(r, Texture2D.whiteTexture); GUI.color = oc;
            GUI.Label(new Rect(r.x + 14, r.y + 8, 470, 34), $"{DisplayUnits.Feet(Mathf.Max(0, HeightAboveGround)):0} ft   {DisplayUnits.Mph(v.magnitude):0} mph   throttle {Throttle * 100:0}%", big);
            string climb = Mathf.Abs(v.y) > .5f ? $"   {(v.y > 0 ? "climbing" : "descending")} {DisplayUnits.Feet(Mathf.Abs(v.y)) * 60:0} ft/min" : "";
            GUI.Label(new Rect(r.x + 14, r.y + 44, 470, 24), $"over the ground · heading {heading:000}° {points[Mathf.RoundToInt(heading / 45f) % 8]}{climb}", small);
            string state = Crashed ? "CRASHED: " + CrashReason : !Airborne ? (Airspeed > TakeOffSpeed ? "TAKE-OFF SPEED: pull back" : "on the ground") : Airspeed < StallSpeed + 4f ? "SLOW: nose down for speed" : "flying";
            GUI.Label(new Rect(r.x + 14, r.y + 70, 470, 24), state, small);
            bool pad = KeyHints.Pad;   // Round 74 (B): the device in use (switches at once)
            string hint = !Airborne && !Crashed ? (pad ? "RT throttle · LT brake · left stick steer; past take-off speed pull back" : "W throttle · S brake · A/D steer; past take-off speed press Down")
                        : Time.unscaledTime - shownAt < 14f ? (pad ? "Left stick pitch / bank · RT / LT throttle · LB / RB rudder" : "Arrows pitch / bank · W/S throttle · A/D rudder") : "";
            GUI.Label(new Rect(r.x + 14, r.y + 96, 480, 24), hint, small);
            if (Leaving)
            {
                var b = new Rect(W * .5f - 330, 120, 660, 76);
                GUI.color = new Color(0, 0, 0, .5f); GUI.DrawTexture(b, Texture2D.whiteTexture); GUI.color = oc;
                GUI.Label(new Rect(b.x, b.y + 4, b.width, 40), "LEAVING THE SURVEY AREA", warn);
                var st = new GUIStyle(warn) { fontSize = 18, fontStyle = FontStyle.Normal }; st.normal.textColor = Color.white;
                GUI.Label(new Rect(b.x, b.y + 42, b.width, 28), Outside > 0f ? "strong headwind · turning back" : "the map ends ahead · turn back", st);
            }
            GUI.matrix = old;
        }
    }
}
