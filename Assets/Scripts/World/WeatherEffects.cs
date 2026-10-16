using UnityEngine;

namespace Racer
{
    // 0.72 weather and night sky, driven by the current WorldLook preset (visual and audio only):
    // falling rain / snow in a box around the camera (world-space particles, so they stream past at speed), cut off under
    // cover (caves, tunnels, underpasses: something solid overhead), a rain ambience loop on the Ambience volume, and a
    // star field that fades in at night. Vehicle lamps are set here too (VehicleLights on every vehicle in the scene).
    // 0.73: thunderstorms in Rain - an occasional strike (one every 20-60 s, irregular): one or two short flashes that light
    // the sky, the clouds and the world (strongest at night, subtle by day; never more than two pulses, ~0.1 s each), then
    // thunder after a short varying delay on the Ambience volume. Settings > Display "Lightning flashes: Off" keeps the
    // thunder and removes the flash. Under cover: no flash, thunder muffled. No gameplay effect. The clouds live here too.
    // 0.74: storms with presence - a strike every 8-25 s (irregular, sometimes a close pair), each with a visible forked bolt
    // in a random direction and a flash noticeable at any time of day; near strikes are bright with a loud crack soon after,
    // far ones dimmer with a long low rumble several seconds later; faint distant rumbles (a glow in the clouds) between
    // strikes. Thunder fuller and louder against a softer rain. The rain sound is rebuilt: a low rounded bed with slow
    // variation, occasional heavier gusts and light patter, no hiss; muffled (low-passed) under cover. The moon is drawn with
    // its phase (MoonDisc) and Dawn lays light ground mist in low areas (DawnMist).
    //
    // Chastain Rush, Round 35 (Part C): Racer's WeatherEffects, ported. Edits (commented "Chastain Rush" in place): no RaceFlow
    // (the Ambience volume is SoundMix's, the lightning setting GameSettings'), no split-screen (its second view left behind), the
    // race's moon over the start line from RaceController's route, cover also when the camera is under the terrain (the pipe
    // network), the clouds and bolts lifted to this map's height (SkyClouds.Lift).
    public sealed class WeatherEffects : MonoBehaviour
    {
        ParticleSystem rain, snow, stars; ParticleSystemRenderer starRenderer;
        AudioSource rainAudio, thunderAudio, rumbleAudio; AudioLowPassFilter thunderFilter, rainFilter, rumbleFilter; AudioClip[] thunderClips, farClips;
        LightningBolts bolts; MoonDisc moon; DawnMist mist; float nextRumble = -1, rumbleAt = -100;
        // 0.76: while thunder rolls the engine and the radio dip a little (0 = no dip, 1 = full), so it stands out over them.
        public static float Duck { get; private set; }
        float duckUntil = -1;
        public int Rumbles { get; private set; }
        public int BoltsShown => bolts ? bolts.Shown : 0;
        public float LastStrikeDistance { get; private set; }
        float nextStrike = -1, strikeAt = -100, thunderAt = -1, strikeStrength, thunderDistance; int pulses; bool strikeFlashes;
        public int Strikes { get; private set; }
        // Evidence only (ConditionsBench): no new strikes while fixed-view screenshots and frame times are taken.
        public static bool HoldStrikes;
        // Evidence only: strikes every TestStrikeInterval seconds when > 0 (frame time during strikes); a fixed moon
        // (phase, elevation, azimuth) when TestMoonPhase >= 0 (moon-phase screenshots).
        public static float TestStrikeInterval, TestMoonPhase = -1, TestMoonElevation, TestMoonAzimuth, TestBoltAzimuth = float.NaN;
        // 0.100 Part C: where the moon disc is drawn (the night light itself is unchanged). The chase camera sees up to about 15
        // degrees above the horizon, so the disc is kept low enough to be in the driving view: races, 11 degrees up ahead of the
        // start line; Free Roam, it rises and sets with the calendar but climbs only to about 12.6 degrees, and every phase's
        // highest point is pulled towards midnight so a crescent is up for part of the night too. The thinnest crescents are drawn
        // as a slim bright sliver (never thinner); only the night of new moon (9 hours either side of it) has none.
        // Returns elevation and azimuth (as for the sun light: the disc is opposite the azimuth), the phase drawn, and 0 / 1 shown.
        ChastainRush.RaceRoute moonRace; float moonRaceAzimuth;
        (float elevation, float azimuth, float phase, float shown) ShownMoon(WorldLook look)
        {
            float phase = look.MoonPhase;
            if (look.Mode != "Free Roam")
            {
                var rc = ChastainRush.RaceController.Instance; var route = rc ? rc.Route : null;   // Chastain Rush: RaceController's route (Racer: RaceDirector.road)
                if (route != moonRace) { moonRace = route; moonRaceAzimuth = 0; if (route != null) { route.PointAt(route.GateS, out var f); moonRaceAzimuth = Mathf.Atan2(f.x, f.z) * Mathf.Rad2Deg + 180; } }
                return (11, moonRaceAzimuth, phase, 1);
            }
            float transit = Mathf.Repeat(12 + phase * 24, 24), fromMidnight = Mathf.Repeat(transit + 12, 24) - 12;
            float d = Mathf.Repeat(look.LookHour - fromMidnight * .35f + 12, 24) - 12;
            float e = 52 * Mathf.Cos(d / 24f * 2 * Mathf.PI) - 2, az = 180 + Mathf.Clamp(d / 6f, -1.4f, 1.4f) * 90;
            if (e > 6) e = 6 + (e - 6) * .15f;
            float fromNew = Mathf.Min(phase, 1 - phase);
            return (e, az, Mathf.Clamp(phase, .088f, .912f), fromNew < .0125f ? 0 : 1);
        }
        public int ThunderPlayed { get; private set; }
        public float FlashLevel { get; private set; }
        public float PeakFlash { get; private set; }
        public bool LastStrikeCovered { get; private set; }
        float rainLevel, snowLevel, starLevel, lightsLevel, nextVehicleScan, nextCoverCheck;
        bool covered;
        static Material rainMat, snowMat, starMat;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            WorldLook.Applied -= Attach; WorldLook.Applied += Attach;
        }
        static void Attach(LookPreset p)
        {
            var look = WorldLook.Current; if (look && !look.GetComponent<WeatherEffects>()) look.gameObject.AddComponent<WeatherEffects>();
        }
        void Awake()
        {
            // Materials live in Resources so their shaders are in the build (Shader.Find alone would not include them).
            if (!rainMat) { var a = Resources.Load<Material>("WeatherRain"); rainMat = a ? new Material(a) : Particle(new Color(.78f, .82f, .90f, .55f)); }
            if (!snowMat) { var a = Resources.Load<Material>("WeatherSnow"); snowMat = a ? new Material(a) : Particle(new Color(1, 1, 1, .9f)); }
            // A soft round dot (an untextured particle is a square, which reads as a white block close to the camera).
            var dot = Dot(); foreach (var m in new[] { rainMat, snowMat }) if (m && m.HasProperty("_BaseMap") && !m.GetTexture("_BaseMap")) m.SetTexture("_BaseMap", dot);
            rain = Make("Rain", rainMat, true);
            snow = Make("Snow", snowMat, false);
            stars = Stars();
            // 0.82 Part C: the rain loop and thunder are synthesised once per session (the same seeded sounds), not on every
            // scene load (they were most of the frozen frame when Free Roam or a course loads)
            if (sharedRain == null || !sharedRain) { sharedRain = Keep(RainClip()); sharedThunder = new[] { Keep(ThunderClip(7411, true)), Keep(ThunderClip(7413, true)), Keep(ThunderClip(7415, true)) }; sharedFar = new[] { Keep(ThunderClip(7412, false)), Keep(ThunderClip(7414, false)), Keep(ThunderClip(7416, false)) }; }
            rainAudio = gameObject.AddComponent<AudioSource>(); rainAudio.clip = sharedRain; rainAudio.loop = true; rainAudio.spatialBlend = 0; rainAudio.volume = 0; rainAudio.playOnAwake = false; rainAudio.priority = 170;
            rainFilter = gameObject.AddComponent<AudioLowPassFilter>(); rainFilter.cutoffFrequency = 22000;
            var thunder = new GameObject("Thunder"); thunder.transform.SetParent(transform, false);
            thunderAudio = thunder.AddComponent<AudioSource>(); thunderAudio.spatialBlend = 0; thunderAudio.playOnAwake = false; thunderAudio.priority = 24;
            thunderFilter = thunder.AddComponent<AudioLowPassFilter>(); thunderFilter.cutoffFrequency = 5000;
            thunderClips = sharedThunder;
            farClips = sharedFar;
            var rumble = new GameObject("Distant thunder"); rumble.transform.SetParent(transform, false);
            rumbleAudio = rumble.AddComponent<AudioSource>(); rumbleAudio.spatialBlend = 0; rumbleAudio.playOnAwake = false; rumbleAudio.priority = 30;
            rumbleFilter = rumble.AddComponent<AudioLowPassFilter>(); rumbleFilter.cutoffFrequency = 700;
            bolts = new GameObject("Lightning bolts").AddComponent<LightningBolts>(); bolts.transform.SetParent(transform, false);
            moon = new GameObject("Moon").AddComponent<MoonDisc>(); moon.transform.SetParent(transform, false);
            mist = new GameObject("Mist").AddComponent<DawnMist>(); mist.transform.SetParent(transform, false);
            if (!GetComponentInChildren<SkyClouds>()) { var sky = new GameObject("Sky clouds"); sky.transform.SetParent(transform, false); sky.AddComponent<SkyClouds>(); }
        }
        static Material Particle(Color c)
        {
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            var m = new Material(shader) { name = "Weather particle" };
            m.SetFloat("_Surface", 1); m.SetFloat("_Blend", 0); m.SetColor("_BaseColor", c);
            m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha); m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha); m.SetFloat("_ZWrite", 0);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); m.renderQueue = 3050;
            return m;
        }
        ParticleSystem Make(string name, Material mat, bool isRain)
        {
            var go = new GameObject(name); go.transform.SetParent(transform, false);
            var ps = go.AddComponent<ParticleSystem>(); ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main; main.loop = true; main.simulationSpace = ParticleSystemSimulationSpace.World; main.playOnAwake = false;
            main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;// an empty system has no visible bounds; "automatic" culling would never let it start
            main.maxParticles = isRain ? 6000 : 5000; main.startSpeed = 0; main.gravityModifier = 0;
            main.startLifetime = isRain ? 1.25f : 7f; main.startSize = isRain ? .03f : .09f; main.startColor = Color.white;
            var shape = ps.shape; shape.shapeType = ParticleSystemShapeType.Box; shape.scale = isRain ? new Vector3(56, 1, 56) : new Vector3(64, 1, 64);
            var vel = ps.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.World;
            vel.x = isRain ? new ParticleSystem.MinMaxCurve(-.6f, .6f) : new ParticleSystem.MinMaxCurve(-.7f, .7f);
            vel.y = isRain ? new ParticleSystem.MinMaxCurve(-24f, -20f) : new ParticleSystem.MinMaxCurve(-2.6f, -1.8f);
            vel.z = isRain ? new ParticleSystem.MinMaxCurve(-.6f, .6f) : new ParticleSystem.MinMaxCurve(-.7f, .7f);
            if (!isRain) { var noise = ps.noise; noise.enabled = true; noise.strength = .6f; noise.frequency = .35f; noise.scrollSpeed = .2f; noise.quality = ParticleSystemNoiseQuality.Low; }
            var em = ps.emission; em.rateOverTime = 0;
            var r = go.GetComponent<ParticleSystemRenderer>(); r.sharedMaterial = mat; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
            if (isRain) { r.renderMode = ParticleSystemRenderMode.Stretch; r.velocityScale = .028f; r.lengthScale = 1; } else r.renderMode = ParticleSystemRenderMode.Billboard;
            // Never larger than a small fraction of the screen, however close to the camera (no blocks or bars in view).
            r.maxParticleSize = isRain ? .006f : .012f;
            return ps;
        }
        // Stars: fixed points on a large dome around the camera, drawn with a fog-free additive shader so they show at night.
        ParticleSystem Stars()
        {
            var go = new GameObject("Night sky stars"); go.transform.SetParent(transform, false);
            var ps = go.AddComponent<ParticleSystem>(); ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main; main.loop = false; main.playOnAwake = false; main.simulationSpace = ParticleSystemSimulationSpace.Local; main.maxParticles = 1400; main.startLifetime = 1e6f; main.startSpeed = 0;
            var em = ps.emission; em.enabled = false;
            var r = go.GetComponent<ParticleSystemRenderer>(); r.renderMode = ParticleSystemRenderMode.Billboard; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
            if (!starMat) { var asset = Resources.Load<Material>("NightStars"); starMat = asset ? new Material(asset) { name = "Night stars" } : Particle(Color.white); }
            r.sharedMaterial = starMat;
            var rng = new System.Random(72); var parts = new ParticleSystem.Particle[1400];
            for (int i = 0; i < parts.Length; i++)
            {
                float u = (float)rng.NextDouble(), v = (float)rng.NextDouble();
                float y = Mathf.Lerp(.06f, 1, Mathf.Sqrt(u)); float a = v * Mathf.PI * 2, rr = Mathf.Sqrt(1 - y * y);
                parts[i].position = new Vector3(Mathf.Cos(a) * rr, y, Mathf.Sin(a) * rr) * 600;
                float b = (float)rng.NextDouble(); parts[i].startSize = Mathf.Lerp(1.1f, 3f, b * b);
                parts[i].startColor = Color.Lerp(new Color(.8f, .86f, 1f), new Color(1f, .95f, .85f), (float)rng.NextDouble()) * Mathf.Lerp(.5f, 1, b);
                parts[i].remainingLifetime = parts[i].startLifetime = 1e6f;
            }
            ps.SetParticles(parts, parts.Length); ps.Play(); r.enabled = false;
            return ps;
        }
        // Chastain Rush: Racer's split-screen second view (0.92 Part F) left behind (no split-screen here)
        void LateUpdate()
        {
            var look = WorldLook.Current; if (!look || look.Preset == null) return;
            var p = look.Preset; var cam = Camera.main; if (!cam) return;
            // Chastain Rush, Round 37: Racer's split-screen second view (0.92 Part F), back with split screen
            var cam2 = ChastainRush.SplitScreen.Session ? ChastainRush.SplitScreen.Session.Cam2 : null; bool split = cam2 && cam2.isActiveAndEnabled;
            if (split != splitLayers) { splitLayers = split; foreach (Component c in new Component[] { rain, snow, stars, moon, mist }) Layer(c, split ? ChastainRush.SplitSession.View1Layer : 0); }
            if (split) SecondView(p, look, cam2); else EndSecondView();
            float density = split ? SplitDensity : 1;
            // Under cover: something solid within 45 m straight above the camera.
            if (Time.unscaledTime >= nextCoverCheck)
            {
                nextCoverCheck = Time.unscaledTime + .2f;
                var at = cam.transform.position;
                bool now = Physics.Raycast(at + Vector3.up * .5f, Vector3.up, 45, ~0, QueryTriggerInteraction.Ignore) || UnderCaveMesh(at) || look.Underground > 0f;   // Chastain Rush: or under the terrain
                if (now && !covered) { rain.Clear(); snow.Clear(); }
                covered = now;
            }
            float shelter = covered ? 0 : 1;
            rainLevel = Mathf.MoveTowards(rainLevel, p.rain * shelter, Time.unscaledDeltaTime * 2);
            snowLevel = Mathf.MoveTowards(snowLevel, p.snowfall * shelter, Time.unscaledDeltaTime * 2);
            var pos = cam.transform.position; var fwd = Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up).normalized;
            // The emitter box sits above and a little ahead of the camera, so the road ahead has falling weather at speed.
            rain.transform.position = pos + fwd * 10 + Vector3.up * 17; snow.transform.position = pos + fwd * 12 + Vector3.up * 13;
            Rate(rain, rainLevel * 3600 * density); Rate(snow, snowLevel * 1700 * density);
            // Weather turned off: what is still falling goes too (no snow left in the air of a clear race).
            if (p.rain <= 0 && rainLevel <= 0 && rain.particleCount > 0) rain.Clear();
            if (p.snowfall <= 0 && snowLevel <= 0 && snow.particleCount > 0) snow.Clear();
            // Stars follow the camera (a sky dome); visible only when dark and not overcast.
            starLevel = p.stars * (1 - Mathf.Max(p.rain, p.snowfall));
            starRenderer ??= stars.GetComponent<ParticleSystemRenderer>(); starRenderer.enabled = starLevel > .02f; stars.transform.position = pos;
            if (starMat && starMat.HasProperty("_Visibility")) starMat.SetFloat("_Visibility", starLevel);
            // Rain ambience on the Ambience volume, muffled under cover.
            float volume = SoundMix.Gain(SoundMix.Kind.Ambience);   // Chastain Rush: the Ambience volume (Racer: flow.Save.Settings.ambience)
            float targetAudio = p.rain * (covered ? .45f : 1) * .42f * volume;
            rainFilter.cutoffFrequency = Mathf.MoveTowards(rainFilter.cutoffFrequency, covered ? 650 : 22000, Time.unscaledDeltaTime * 40000);
            if (targetAudio > .001f && !rainAudio.isPlaying) rainAudio.Play();
            rainAudio.volume = Mathf.MoveTowards(rainAudio.volume, targetAudio, Time.unscaledDeltaTime * .5f);
            if (rainAudio.volume <= .001f && targetAudio <= .001f && rainAudio.isPlaying) rainAudio.Stop();
            Storm(p, look);
            // The moon (with its phase) where the night light comes from; visible in a clear dark sky.
            if (look.Mode != "Menu")
            {
                var (mElev, mAz, phase, shown) = ShownMoon(look);
                if (TestMoonPhase >= 0) { phase = TestMoonPhase; mElev = TestMoonElevation; mAz = TestMoonAzimuth; shown = 1; }
                float dark = Mathf.Clamp01(p.stars * 1.2f) * (1 - Mathf.Max(p.rain, p.snowfall));
                moon.Show(cam, mElev, mAz, phase, shown * dark * Mathf.SmoothStep(0, 1, Mathf.InverseLerp(-1, 4, mElev)));
            }
            else moon.Show(cam, -10, 0, .5f, 0);
            mist.Set(snowMat, cam, look.Mode == "Menu" || covered ? 0 : p.mist * (1 - Mathf.Max(p.rain, p.snowfall) * .5f), p.fogColor);
            // Vehicle lamps: every vehicle (player, rivals, traffic) gets its lamps once; their level follows the preset.
            if (Time.unscaledTime >= nextVehicleScan) { nextVehicleScan = Time.unscaledTime + 1; foreach (var v in FindObjectsByType<ArcadeVehicle>(FindObjectsSortMode.None)) if (!v.GetComponent<VehicleLights>()) v.gameObject.AddComponent<VehicleLights>(); }
            lightsLevel = Mathf.MoveTowards(lightsLevel, p.lights, Time.unscaledDeltaTime);
            VehicleLights.Level = lightsLevel;
        }
        // 0.92 Part F: split-screen gives the second view its own falling rain and snow, stars, moon and ground mist (each view's are drawn on its own
        // layer, hidden from the other view, so nothing shows doubled or in one half only); the clouds, lighting, lightning flash and thunder stay one
        // for both. Falling weather is half as dense in split-screen. Chastain Rush, Round 37: ported back (cover: also under the terrain)
        ParticleSystem rain2, snow2, stars2; ParticleSystemRenderer starRenderer2; MoonDisc moon2; DawnMist mist2;
        float rainLevel2, snowLevel2, nextCoverCheck2; bool covered2, splitLayers;
        public const float SplitDensity = .5f;
        static void Layer(Component c, int layer) { if (c) foreach (var t in c.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = layer; }
        void SecondView(LookPreset p, WorldLook look, Camera cam2)
        {
            if (!rain2)
            {
                rain2 = Make("Rain (view 2)", rainMat, true); snow2 = Make("Snow (view 2)", snowMat, false); stars2 = Stars();
                moon2 = new GameObject("Moon (view 2)").AddComponent<MoonDisc>(); moon2.transform.SetParent(transform, false);
                mist2 = new GameObject("Mist (view 2)").AddComponent<DawnMist>(); mist2.transform.SetParent(transform, false);
                foreach (Component c in new Component[] { rain2, snow2, stars2, moon2, mist2 }) Layer(c, ChastainRush.SplitSession.View2Layer);
            }
            if (Time.unscaledTime >= nextCoverCheck2)
            {
                nextCoverCheck2 = Time.unscaledTime + .2f; var at = cam2.transform.position;
                bool now = Physics.Raycast(at + Vector3.up * .5f, Vector3.up, 45, ~0, QueryTriggerInteraction.Ignore) || UnderCaveMesh(at) || WorldLook.UndergroundAt(cam2) > 0f;
                if (now && !covered2) { rain2.Clear(); snow2.Clear(); }
                covered2 = now;
            }
            float shelter = covered2 ? 0 : 1;
            rainLevel2 = Mathf.MoveTowards(rainLevel2, p.rain * shelter, Time.unscaledDeltaTime * 2); snowLevel2 = Mathf.MoveTowards(snowLevel2, p.snowfall * shelter, Time.unscaledDeltaTime * 2);
            var pos = cam2.transform.position; var fwd = Vector3.ProjectOnPlane(cam2.transform.forward, Vector3.up).normalized;
            rain2.transform.position = pos + fwd * 10 + Vector3.up * 17; snow2.transform.position = pos + fwd * 12 + Vector3.up * 13;
            Rate(rain2, rainLevel2 * 3600 * SplitDensity); Rate(snow2, snowLevel2 * 1700 * SplitDensity);
            if (p.rain <= 0 && rainLevel2 <= 0 && rain2.particleCount > 0) rain2.Clear();
            if (p.snowfall <= 0 && snowLevel2 <= 0 && snow2.particleCount > 0) snow2.Clear();
            starRenderer2 ??= stars2.GetComponent<ParticleSystemRenderer>(); starRenderer2.enabled = starLevel > .02f; stars2.transform.position = pos;
            if (look.Mode != "Menu")
            {
                var (mElev, mAz, phase, shown) = ShownMoon(look);
                float dark = Mathf.Clamp01(p.stars * 1.2f) * (1 - Mathf.Max(p.rain, p.snowfall));
                moon2.Show(cam2, mElev, mAz, phase, shown * dark * Mathf.SmoothStep(0, 1, Mathf.InverseLerp(-1, 4, mElev)));
            }
            else moon2.Show(cam2, -10, 0, .5f, 0);
            mist2.Set(snowMat, cam2, look.Mode == "Menu" || covered2 ? 0 : p.mist * (1 - Mathf.Max(p.rain, p.snowfall) * .5f), p.fogColor);
        }
        void EndSecondView()
        {
            if (!rain2 || !rain2.isPlaying && rain2.particleCount == 0 && !(starRenderer2 && starRenderer2.enabled) && !moon2.Visible && mist2.Level <= 0) return;
            rain2.Clear(); snow2.Clear(); Rate(rain2, 0); Rate(snow2, 0); if (starRenderer2) starRenderer2.enabled = false; moon2.Show(Camera.main, -10, 0, .5f, 0); mist2.Set(snowMat, Camera.main, 0, Color.white);
        }
        // ---------- 0.73 lightning and thunder ----------
        void Storm(LookPreset p, WorldLook look)
        {
            bool storm = p.rain > .5f && look.Mode != "Menu";
            float now = Time.time;// scaled: nothing new while paused
            Duck = Mathf.MoveTowards(Duck, now < duckUntil ? 1 : 0, Time.deltaTime * (now < duckUntil ? 6 : .6f));
            if (!storm) { nextStrike = -1; thunderAt = -1; nextRumble = -1; duckUntil = -1; if (FlashLevel > 0) Flash(0, look); if (thunderAudio.isPlaying && p.rain <= .5f) thunderAudio.Stop(); if (rumbleAudio.isPlaying && p.rain <= .5f) rumbleAudio.Stop(); return; }
            if (nextStrike < 0 || HoldStrikes && nextStrike < now + 5) nextStrike = now + Random.Range(4f, 12f);
            if (strikeRequested) { strikeRequested = false; nextStrike = now; }
            if (nextRumble < 0) nextRumble = now + Random.Range(6f, 14f);
            float volume = SoundMix.Gain(SoundMix.Kind.Ambience);   // Chastain Rush: the Ambience volume
            if (now >= nextStrike)
            {
                Strikes++; strikeAt = now;
                // irregular, 8-25 s apart; about one strike in five is followed closely by a second
                nextStrike = Random.value < .2f ? now + Random.Range(1.6f, 4.5f) : now + Random.Range(8f, 25f);
                if (TestStrikeInterval > 0) nextStrike = now + TestStrikeInterval;
                // 0.76: nearer strikes more often (260-1400 m); as strong by day as by night (daylight needs it more).
                pulses = Random.value < .55f ? 2 : 1; thunderDistance = Mathf.Pow(Random.value, 1.3f);
                LastStrikeDistance = Mathf.Lerp(260, 1400, thunderDistance);
                strikeStrength = Mathf.Lerp(1.15f, .7f, thunderDistance);
                LastStrikeCovered = covered; strikeFlashes = !covered && GameSettings.Settings.lightningFlashes;   // Chastain Rush: GameSettings
                // sound at 343 m/s: a near crack within about a second, a far rumble 4-5 s later
                thunderAt = now + LastStrikeDistance / 343f + Random.Range(0f, .3f);
                // 0.76: four bolts in five come down in front of the rider (within 38 degrees of the view), the rest anywhere.
                var cam = Camera.main; float view = cam ? cam.transform.eulerAngles.y : 0;
                float azimuth = float.IsNaN(TestBoltAzimuth) ? (Random.value < .8f ? view + Random.Range(-38f, 38f) : Random.Range(0f, 360f)) : TestBoltAzimuth;
                if (cam && bolts) bolts.Strike(cam.transform.position, azimuth, LastStrikeDistance, 1 - thunderDistance, pulses, 470 + SkyClouds.Lift, 1 - Mathf.Clamp01(p.lights));   // Chastain Rush: + SkyClouds.Lift
                nextRumble = Mathf.Max(nextRumble, now + 5);
            }
            // Flash envelope (0.76): pulse 1 at 0-0.16 s, pulse 2 (weaker) at 0.26-0.40 s, then a soft afterglow fading by about
            // 1 s - long enough to register, still never more than two pulses.
            float age = now - strikeAt, f = 0;
            if (strikeFlashes && age >= 0 && age < 1.2f) f = Envelope(age, pulses) * strikeStrength;
            // distant rumble between strikes: a faint glow inside the clouds and a low roll, no bolt and no screen flash
            float glow = 0, rumbleAge = now - rumbleAt;
            if (rumbleAge >= 0 && rumbleAge < 1.2f && !covered) glow = .16f * Mathf.Sin(Mathf.Clamp01(rumbleAge / 1.2f) * Mathf.PI);
            if (now >= nextRumble)
            {
                nextRumble = now + Random.Range(7f, 16f); rumbleAt = now; Rumbles++;
                rumbleAudio.clip = farClips[Random.Range(0, farClips.Length)]; rumbleAudio.volume = Random.Range(.5f, .65f) * (covered ? .5f : 1) * volume;
                rumbleFilter.cutoffFrequency = covered ? 400 : 1600; rumbleAudio.pitch = Random.Range(.85f, .98f); rumbleAudio.PlayDelayed(Random.Range(1.5f, 3f));
            }
            if (f > 0 || FlashLevel > 0 || glow > 0) { Flash(f, look); SkyClouds.Flash = Mathf.Max(f, glow); }
            PeakFlash = Mathf.Max(PeakFlash, f);
            if (thunderAt > 0 && now >= thunderAt)
            {
                thunderAt = -1; ThunderPlayed++;
                bool near = thunderDistance < .45f;
                thunderAudio.clip = near ? thunderClips[Random.Range(0, thunderClips.Length)] : farClips[Random.Range(0, farClips.Length)];
                // 0.76: full volume near, barely less far; a far roll keeps its body (the low-pass no longer strips it to sub-bass).
                thunderAudio.volume = Mathf.Lerp(1f, .9f, thunderDistance) * (covered ? .45f : 1) * volume;
                thunderFilter.cutoffFrequency = covered ? 520 : Mathf.Lerp(9000, 2800, thunderDistance);
                thunderAudio.pitch = Random.Range(.92f, 1.04f); thunderAudio.Play();
                if (!covered) duckUntil = now + (near ? 3.2f : 4.2f);
            }
        }
        public static float Pulse(float age, float start, float length) { float x = (age - start) / length; return x < 0 || x > 1 ? 0 : x < .15f ? x / .15f : Mathf.Pow(1 - (x - .15f) / .85f, 2); }
        // 0.76 strike light: the stroke (0-0.16 s), an optional weaker return stroke (0.26-0.40 s) and a decaying afterglow.
        public static float Envelope(float age, int pulses)
        {
            if (age < 0 || age > 1.2f) return 0;
            float f = Pulse(age, 0, .16f); if (pulses > 1) f = Mathf.Max(f, .75f * Pulse(age, .26f, .14f));
            return Mathf.Max(f, age < .1f ? 0 : .3f * Mathf.Exp(-(age - .1f) / .3f));
        }
        // 0.77 Trailer Mode: a strike at once (kept until a storm is running, so it also works right after Rain is chosen).
        bool strikeRequested;
        public void StrikeNow() => strikeRequested = true;
        void Flash(float f, WorldLook look) { FlashLevel = f; SkyClouds.Flash = f; look.Lightning(f); }
        // Thunder made at start-up (no audio asset). Near: a sharp crack, a ripping tear and a full rolling rumble with deep
        // swells. Far: no crack, a slower onset and a long, low, softer roll.
        static AudioClip ThunderClip(int seed, bool near)
        {
            const int rate = 22050; float seconds = near ? 8f : 10f; var data = new float[(int)(rate * seconds)]; var rng = new System.Random(seed);
            float b1 = 0, b2 = 0, l1 = 0, l2 = 0, l3 = 0, hp = 0, hpPrev = 0, peak = 0, m1 = 0, m2 = 0, r1 = 0, r2 = 0;
            // rolling swells: a few peaks at random times, each a quick rise and a slow decay
            int swells = near ? 5 : 4; var at = new float[swells]; var amp = new float[swells]; var len = new float[swells];
            for (int k = 0; k < swells; k++) { at[k] = (near ? .05f : .5f) + (float)rng.NextDouble() * seconds * .55f; amp[k] = .45f + (float)rng.NextDouble() * .7f; len[k] = .8f + (float)rng.NextDouble() * 2.2f; }
            if (near) { at[0] = .02f; amp[0] = 1.15f; len[0] = 1.6f; }
            for (int i = 0; i < data.Length; i++)
            {
                float t = i / (float)rate, n = (float)rng.NextDouble() * 2 - 1;
                b1 = b1 * .997f + n * .045f; b2 = b2 * .9993f + n * .012f;// two brown-noise layers (rumble, sub)
                l1 += (b1 - l1) * (near ? .06f : .035f); l2 += (l1 - l2) * .09f; l3 += (b2 - l3) * .02f;
                float env = 0; for (int k = 0; k < swells; k++) { float x = t - at[k]; if (x > 0) env += amp[k] * Mathf.Min(1, x / .12f) * Mathf.Exp(-x / len[k]); }
                env *= Mathf.Clamp01(t / (near ? .01f : .35f)) * Mathf.Clamp01((seconds - t) / 1.2f);
                float v = (l2 * 7f + l3 * 9f) * env;
                // 0.76: most of the old roll was below 150 Hz, which small speakers barely play. A mid-band roll (about 90-450 Hz)
                // and a quieter rattle band (about 300-1200 Hz) under the same swells give it body on any speaker.
                m1 += (n - m1) * .12f; m2 += (m1 - m2) * .025f; r1 += (n - r1) * .35f; r2 += (r1 - r2) * .09f;
                v += ((m1 - m2) * 8f + (r1 - r2) * 2.5f * (near ? 1 : .6f)) * env;
                if (near && t < .5f)
                {
                    hp = .9f * (hp + n - hpPrev); hpPrev = n;// crack: bright noise burst, then a short tearing rattle
                    float crack = Mathf.Exp(-t / .045f) * 1.1f + (t > .05f ? Mathf.Exp(-(t - .05f) / .18f) * .35f * (Mathf.Sin(t * 230) > .2f ? 1 : .3f) : 0);
                    v += hp * crack * .55f;
                }
                data[i] = v; peak = Mathf.Max(peak, Mathf.Abs(v));
            }
            // normalise, then a gentle saturation (fuller and louder at the same peak), normalised again
            if (peak > 0) for (int i = 0; i < data.Length; i++) data[i] *= .95f / peak;
            const float sat = 2.4f; float tk = (float)System.Math.Tanh(sat), top = 0;
            for (int i = 0; i < data.Length; i++) { data[i] = (float)System.Math.Tanh(sat * data[i]) / tk; top = Mathf.Max(top, Mathf.Abs(data[i])); }
            if (top > 0) for (int i = 0; i < data.Length; i++) data[i] *= .97f / top;
            var clip = AudioClip.Create("Thunder " + seed, data.Length, 1, rate, false); clip.SetData(data, 0); return clip;
        }
        void OnDestroy() { if (FlashLevel > 0) SkyClouds.Flash = 0; }
        static AudioClip sharedRain; static AudioClip[] sharedThunder, sharedFar;
        static AudioClip Keep(AudioClip c) { c.hideFlags = HideFlags.DontUnloadUnusedAsset; return c; }
        static Texture2D dotTexture;
        static Texture2D Dot()
        {
            if (dotTexture) return dotTexture;
            const int n = 32; dotTexture = new Texture2D(n, n, TextureFormat.RGBA32, false) { name = "Weather dot", wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < n; y++) for (int x = 0; x < n; x++) { float dx = (x + .5f) / n * 2 - 1, dy = (y + .5f) / n * 2 - 1; float a = Mathf.Clamp01(1 - Mathf.Sqrt(dx * dx + dy * dy)); dotTexture.SetPixel(x, y, new Color(1, 1, 1, a * a)); }
            dotTexture.Apply(); return dotTexture;
        }
        // Cave and tunnel roofs are often visual meshes without colliders: their triangles are tested directly (no physics,
        // so nothing in the game can collide with or sense them). Candidates: cave / tunnel / canopy / ceiling pieces and the
        // underground surface shader.
        System.Collections.Generic.List<(Transform t, Vector3[] v, int[] tri, Renderer r)> caveMeshes;
        public bool CoveredAt(Vector3 at) => UnderCaveMesh(at); // 0.97: the police helicopter cannot see under a roof
        bool UnderCaveMesh(Vector3 at)
        {
            if (caveMeshes == null)
            {
                caveMeshes = new System.Collections.Generic.List<(Transform, Vector3[], int[], Renderer)>();
                foreach (var r in FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
                {
                    var n = r.name.ToLowerInvariant(); var mf = r.GetComponent<MeshFilter>();
                    bool cover = n.Contains("cave") || n.Contains("tunnel") || n.Contains("canopy") && !n.Contains("woodland") || n.Contains("ceiling") || n.Contains("cavern") || n.Contains("enclosed");
                    foreach (var m in r.sharedMaterials) if (m && m.shader && m.shader.name.Contains("Underground")) cover = true;
                    if (cover && mf && mf.sharedMesh && mf.sharedMesh.isReadable) caveMeshes.Add((r.transform, mf.sharedMesh.vertices, mf.sharedMesh.triangles, r));
                }
            }
            var ray = new Ray(at + Vector3.up * .5f, Vector3.up);
            foreach (var (t, v, tri, r) in caveMeshes)
            {
                if (!r || !r.enabled) continue; var b = r.bounds;
                if (at.x < b.min.x || at.x > b.max.x || at.z < b.min.z || at.z > b.max.z || b.max.y < at.y || b.min.y > at.y + 45) continue;
                var o = t.InverseTransformPoint(ray.origin); var d = t.InverseTransformDirection(ray.direction);
                for (int i = 0; i < tri.Length; i += 3)
                {
                    // Moller-Trumbore, either facing
                    Vector3 a = v[tri[i]], e1 = v[tri[i + 1]] - a, e2 = v[tri[i + 2]] - a; var pv = Vector3.Cross(d, e2); float det = Vector3.Dot(e1, pv);
                    if (Mathf.Abs(det) < 1e-7f) continue; float inv = 1 / det; var tv = o - a; float u = Vector3.Dot(tv, pv) * inv; if (u < 0 || u > 1) continue;
                    var q = Vector3.Cross(tv, e1); float w = Vector3.Dot(d, q) * inv; if (w < 0 || u + w > 1) continue;
                    float dist = Vector3.Dot(e2, q) * inv; if (dist > 0 && t.TransformVector(d * dist).magnitude < 45) return true;
                }
            }
            return false;
        }
        static void Rate(ParticleSystem ps, float rate)
        {
            var em = ps.emission; em.rateOverTime = rate;
            if (rate > 1 && !ps.isPlaying) ps.Play(); else if (rate <= 1 && ps.isPlaying && ps.particleCount == 0) ps.Stop();
        }
        // A rain loop made at start-up (no audio asset), 24 s and seamless: a low rounded bed (two softly filtered noise layers),
        // slow swells and two heavier gusts in the loop, and light patter (short soft droplet ticks of varied pitch and size,
        // a few heavier drips); everything is low-passed under 5 kHz, so there is no hiss.
        static AudioClip RainClip()
        {
            const int rate = 22050, seconds = 24, n = rate * seconds; var bed = new float[n]; var rng = new System.Random(7402);
            float w1 = 0, w2 = 0, w3 = 0, r1 = 0, r2 = 0; var raw = new float[n]; for (int i = 0; i < n; i++) raw[i] = (float)rng.NextDouble() * 2 - 1;
            for (int pass = 0; pass < 2; pass++)// the same noise twice round: the first pass only settles the filters, so the loop point matches
                for (int i = 0; i < n; i++)
                {
                    float x = raw[i];
                    w1 += (x - w1) * .16f; w2 += (w1 - w2) * .22f; w3 += (w2 - w3) * .3f;// wash ~1.2 kHz and down
                    r1 += (x - r1) * .025f; r2 += (r1 - r2) * .05f;// rounder low bed ~150-300 Hz
                    if (pass == 1) bed[i] = w3 * .55f + r2 * 2.2f;
                }
            float Wrap(float t, float c) { float d = Mathf.Abs(t - c); return Mathf.Min(d, seconds - d); }
            float[] gusts = { 6.5f, 17.2f };
            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)rate;
                float swell = .82f + .1f * Mathf.Sin(2 * Mathf.PI * t / 12f) + .06f * Mathf.Sin(2 * Mathf.PI * t / 4.8f + 1.1f) + .04f * Mathf.Sin(2 * Mathf.PI * t / 3f + 2.3f);
                foreach (var g in gusts) { float d = Wrap(t, g); swell += .42f * Mathf.Exp(-d * d / 2.4f); }
                data[i] = bed[i] * swell;
            }
            // patter: ~45 droplet ticks a second (damped tones 1.2-3.8 kHz, 3-9 ms), ~4 heavier drips (400-900 Hz)
            void Drop(float freq, float decay, float a, int start)
            {
                int len = (int)(decay * 6 * rate); float ph = (float)rng.NextDouble() * 6.28f, w = 2 * Mathf.PI * freq / rate;
                for (int k = 0; k < len; k++) { int j = (start + k) % n; data[j] += a * Mathf.Sin(ph + w * k) * Mathf.Exp(-k / (decay * rate)) * Mathf.Min(1, k / 12f); }
            }
            for (int k = 0; k < seconds * 45; k++) { float a = (float)rng.NextDouble(); Drop(1200 + (float)rng.NextDouble() * 2600, .003f + (float)rng.NextDouble() * .006f, .035f + a * a * .09f, rng.Next(n)); }
            for (int k = 0; k < seconds * 4; k++) Drop(400 + (float)rng.NextDouble() * 500, .012f + (float)rng.NextDouble() * .012f, .05f + (float)rng.NextDouble() * .06f, rng.Next(n));
            // final soft low-pass (two poles at ~4.5 kHz), run twice around the loop so the ends join
            float o1 = 0, o2 = 0, peak = 0;
            for (int pass = 0; pass < 2; pass++) for (int i = 0; i < n; i++) { o1 += (data[i] - o1) * .62f; o2 += (o1 - o2) * .62f; if (pass == 1) { data[i] = o2; peak = Mathf.Max(peak, Mathf.Abs(o2)); } }
            if (peak > 0) for (int i = 0; i < n; i++) data[i] *= .8f / peak;
            var clip = AudioClip.Create("Rain ambience", n, 1, rate, false); clip.SetData(data, 0);
            return clip;
        }
    }
}
