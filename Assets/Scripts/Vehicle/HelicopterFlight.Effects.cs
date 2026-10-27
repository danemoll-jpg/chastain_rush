using Racer;
using UnityEngine;

namespace ChastainRush
{
    // Round 63: what the helicopter shows and sounds like. The rotors turn with the spool (the blur disc shows at speed); the rotor's thump and its wash
    // (the police helicopter's clips, PoliceHelicopter.Thump / Whoosh) pitch up with the spool and the load, a wind with the speed; the downwash: a
    // ring of dust (spray over water, bits of grass on a lawn) blown out along the ground under it, stronger the lower it is (under 30 m).
    // The readout (bottom left): height over the ground, speed, heading, boost, and how to lift off while it sits on the ground.
    public sealed partial class HelicopterFlight
    {
        AudioSource thump, wash, wind; ParticleSystem downwash; ParticleSystem.EmissionModule washEmission; ParticleSystem.MainModule washMain;
        static Material discMaterial, dustMaterial;
        float shownAt;

        void StartEffects()
        {
            shownAt = Time.unscaledTime;
            if (!thump)
            {
                AudioSource Src(AudioClip clip, float vol) { var a = gameObject.AddComponent<AudioSource>(); a.clip = clip; a.loop = true; a.spatialBlend = 0; a.dopplerLevel = 0; a.volume = 0; a.priority = 90; a.Play(); return a; }
                thump = Src(PoliceHelicopter.Thump(), 0); wash = Src(PoliceHelicopter.Whoosh(), 0);
                wind = Src(PoliceHelicopter.Whoosh(), 0); wind.pitch = 1.8f;
            }
            if (!downwash)
            {
                var go = new GameObject("Downwash"); go.transform.SetParent(null, false); downwash = go.AddComponent<ParticleSystem>();
                downwash.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                washMain = downwash.main; washMain.loop = true; washMain.startLifetime = new ParticleSystem.MinMaxCurve(.8f, 1.6f); washMain.startSpeed = new ParticleSystem.MinMaxCurve(9f, 16f);
                washMain.startSize = new ParticleSystem.MinMaxCurve(1.2f, 3.2f); washMain.simulationSpace = ParticleSystemSimulationSpace.World; washMain.maxParticles = 1500; washMain.gravityModifier = -.05f;
                var shape = downwash.shape; shape.shapeType = ParticleSystemShapeType.Circle; shape.radius = 2.5f; shape.radiusThickness = 1f; go.transform.rotation = Quaternion.Euler(90, 0, 0);
                var drag = downwash.limitVelocityOverLifetime; drag.enabled = true; drag.drag = 1.6f;
                var fade = downwash.colorOverLifetime; fade.enabled = true; var g = new Gradient(); g.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) }, new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(.55f, .15f), new GradientAlphaKey(0, 1) }); fade.color = g;
                washEmission = downwash.emission; washEmission.rateOverTime = 0;
                var r = go.GetComponent<ParticleSystemRenderer>(); r.renderMode = ParticleSystemRenderMode.Billboard; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
                if (!dustMaterial) { var hydrant = FindAnyObjectByType<HydrantWater>(); dustMaterial = hydrant ? hydrant.sprayMaterial : null; }
                if (dustMaterial) r.sharedMaterial = dustMaterial; else r.enabled = false;
                downwash.Play();
            }
            if (discRenderer)
            {
                if (!discMaterial) { var glass = Resources.Load<Material>("VehicleGlazing"); discMaterial = glass ? new Material(glass) { name = "Rotor disc" } : null; if (discMaterial) { discMaterial.color = new Color(.12f, .12f, .14f, .09f); if (discMaterial.HasProperty("_BaseColor")) discMaterial.SetColor("_BaseColor", new Color(.12f, .12f, .14f, .09f)); discMaterial.SetFloat("_Smoothness", 0); } }
                if (discMaterial) discRenderer.sharedMaterial = discMaterial;
                discRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; discRenderer.enabled = false;
            }
        }
        void StopEffects()
        {
            foreach (var a in new[] { thump, wash, wind }) if (a) Destroy(a);
            thump = wash = wind = null;
            if (downwash) Destroy(downwash.gameObject); downwash = null;
        }

        void Effects(float dt)
        {
            if (!car) return;
            if (car.Puppet) Spool = Mathf.MoveTowards(Spool, 1f, dt / 2.2f);   // Round 74 (A1): a replay's helicopter: its rotors at speed
            float speed = car.Puppet ? car.PuppetSpeed : car.Body.linearVelocity.magnitude;
            // rotors: at full spool about 6.5 turns a second (the blades blur into the disc), the tail rotor faster
            float rps = Spool * 6.5f;
            rotorAngle = (rotorAngle + rps * 360f * dt) % 360f; tailAngle = (tailAngle + rps * 5f * 360f * dt) % 360f;
            if (mainRotor) mainRotor.localRotation = Quaternion.Euler(0, rotorAngle, 0);
            if (tailRotor) tailRotor.localRotation = Quaternion.Euler(tailAngle, 0, 0);
            if (discRenderer) discRenderer.enabled = Spool > .7f;
            // sound (Racer's vehicle volume; muted by CheckRun in a check)
            float vol = VehicleAudio.Volume, load = Landed ? 0 : Mathf.Clamp01(Mathf.Abs(car.Body.linearVelocity.y) / Climb * .5f + (Boosting ? .5f : 0) + speed / BoostSpeed * .4f);
            if (thump) { thump.pitch = Mathf.Lerp(.25f, 1f, Spool) * (1f + .1f * load); thump.volume = vol * Mathf.Lerp(0f, .85f, Spool); }
            if (wash) { wash.pitch = Mathf.Lerp(.4f, 1.05f, Spool) + .1f * load; wash.volume = vol * Spool * (.35f + .3f * load); }
            if (wind) wind.volume = vol * Mathf.Clamp01((speed - 15f) / 90f) * .6f;
            // the downwash on the ground under it
            if (downwash)
            {
                float h = HeightAboveGround, strength = Crashed ? 0 : Spool * Mathf.Clamp01(1f - h / 30f);
                downwash.transform.position = new Vector3(car.Body.position.x, groundPoint.y + .3f, car.Body.position.z);
                if (OverWater) downwash.transform.position = new Vector3(car.Body.position.x, car.Body.position.y - SkidDrop - h + .2f, car.Body.position.z);
                washEmission.rateOverTime = strength * 260f;
                var shape = downwash.shape; shape.radius = 2f + h * .25f;
                washMain.startSpeed = new ParticleSystem.MinMaxCurve(6f + 10f * strength, 10f + 14f * strength);
                Color c = OverWater ? new Color(.92f, .95f, .97f, 1) : groundCollider is TerrainCollider ? new Color(.62f, .60f, .45f, 1) : new Color(.70f, .68f, .64f, 1);
                washMain.startColor = c;
            }
        }

        // ---------- the readout ----------
        static GUIStyle big, small;
        void OnGUI()
        {
            if (Event.current.type != EventType.Repaint || PlayerHeli != this || GameMenus.Open || WorldMap.IsOpen || TrailerMode.HudHidden || !car) return;
            if (big == null)
            {
                big = new GUIStyle(GUI.skin.label) { fontSize = 26, fontStyle = FontStyle.Bold }; big.normal.textColor = Color.white;
                small = new GUIStyle(GUI.skin.label) { fontSize = 17 }; small.normal.textColor = new Color(.9f, .92f, .95f);
            }
            float s = Mathf.Max(1f, Screen.height / 800f); var old = GUI.matrix; GUI.matrix = Matrix4x4.Scale(new Vector3(s, s, 1f));
            float W = Screen.width / s, Hh = Screen.height / s;
            var v = car.Body.linearVelocity; float ground = new Vector2(v.x, v.z).magnitude;
            float heading = Mathf.Repeat(transform.eulerAngles.y, 360f); string[] points = { "N", "NE", "E", "SE", "S", "SW", "W", "NW" };
            var r = new Rect(24, Hh - 150, 470, 126);
            var oldColor = GUI.color; GUI.color = new Color(0, 0, 0, .45f); GUI.DrawTexture(r, Texture2D.whiteTexture); GUI.color = oldColor;
            GUI.Label(new Rect(r.x + 14, r.y + 8, 340, 34), $"{DisplayUnits.Feet(Mathf.Max(0, HeightAboveGround)):0} ft   {DisplayUnits.Mph(ground):0} mph", big);
            string climb = Mathf.Abs(v.y) > .5f ? $"   {(v.y > 0 ? "climbing" : "descending")} {DisplayUnits.Feet(Mathf.Abs(v.y)) * 60:0} ft/min" : "";
            GUI.Label(new Rect(r.x + 14, r.y + 44, 340, 24), $"over the ground · heading {heading:000}° {points[Mathf.RoundToInt(heading / 45f) % 8]}{climb}", small);
            string state = Crashed ? "CRASHED: " + CrashReason : Spool < .95f ? $"rotor {Spool * 100:0}%" : Landed ? "landed" : Boosting ? "BOOST" : "flying";
            GUI.Label(new Rect(r.x + 14, r.y + 70, 340, 24), state, small);
            bool pad = KeyHints.Pad;   // Round 74 (B)
            string hint = Landed && !Crashed ? (pad ? "Left stick up or RT: lift off" : "Space: lift off") : Time.unscaledTime - shownAt < 14f ? (pad ? "Left stick climb / turn · right stick fly · RB boost" : "W/S/Q/E fly · A/D turn · Space/C up / down · Shift boost") : "";
            GUI.Label(new Rect(r.x + 14, r.y + 96, 450, 24), hint, small);
            BoundaryWarning(W);   // Round 66
            GUI.matrix = old;
        }
    }
}
