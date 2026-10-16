using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Racer
{
    // 0.71 lighting and atmosphere. One shared look, applied to every scene at load by code (no per-scene edits):
    // sun, ambient, sky, distance haze, ground-surface response, water and post-processing.
    // Everything a look sets is one named LookPreset; two presets interpolate field by field (LookPreset.Lerp).
    // 0.72 time of day and weather (visual and audio only): Day / Dusk / Night presets, Clear / Rain / Snow applied on top of
    // any of them. Races use the conditions chosen at race setup, fixed for the race; Free Roam runs a live day-night cycle
    // (FreeRoamHoursPerRealMinute) through the same presets.
    // 0.74: a Dawn preset (race option and the Free Roam early morning), the Free Roam clock saved and resumed with a 30-day
    // calendar (Day N), a visible moon with the day's phase (new on day 1, full around day 15; brighter nights near full),
    // a fixed full moon in races.
    // "-lookOff" on the command line leaves the scene's built lighting - used only for evidence.
    //
    // Chastain Rush, Round 35 (Part C): Racer's WorldLook, ported. Edits (commented "Chastain Rush" in place):
    //  - Clear Day is this game's accepted day (MapBuilder.BuildSky: sun, ambient, the SkyGradient sky, fog 100-650 m) and no
    //    grading (post-processing neutral, no tonemapping, no bloom by day); Dusk, Night and Dawn are Racer's, with their fog
    //    distances, sun strength, sky tint and grading taken relative to Racer's own Clear Day (Racer's fog is 180-1600 m).
    //  - the sky is this game's SkyGradient (zenith, horizon = fog, sun glow), not Unity's procedural sky.
    //  - no RaceFlow / RaceDirector: a race (RaceController not Free) shows the race setup's conditions (GameMenus' race
    //    settings: the game layer); everything else is Free Roam (the world: its clock and weather in settings.json). No
    //    "Menu" look, campaign, split-screen or Trailer Mode.
    //  - the ground's wet and snow response: Racer's ground shaders read _RacerWet / _RacerSnow; here ChastainRush/VertexLit
    //    reads them (roads, drives, houses, trees) and the terrain's grass layer is tinted (TerrainGrass below).
    //  - underground (the camera under the terrain: pipes, junctions, the hub) the look eases to Clear Day: the network's
    //    lighting is not changed by the sky (Underground below).
    //  - the race gates' night glow (Racer's Glow) left behind: this game's gate materials are unlit and already read at night.
    [Serializable]
    public sealed class LookPreset
    {
        public string name = "Clear Day";
        [Header("Sun")] public float sunElevation, sunAzimuth, sunIntensity, shadowStrength;
        public Color sunColor;
        [Header("Ambient (trilight)")] public Color ambientSky, ambientEquator, ambientGround; public float ambientIntensity;
        [Header("Sky (procedural)")] public Color skyTint, skyGround; public float skyExposure, atmosphere, sunSize;
        [Header("Distance haze (linear fog)")] public Color fogColor; public float fogStart, fogEnd;
        [Header("Ground surfaces (Racer ground shader)")] public float sunBoost, ambientScale, shadowLift, roadSheen, groundVariation;
        [Header("Water")] public float waterSmoothness;
        [Header("Post-processing")] public float postExposure, contrast, saturation, bloomIntensity, bloomThreshold;
        public Color colorFilter;
        // 0.72: vehicle lamps / course markings glow (0 day .. 1 night), star field, wet surfaces, snow cover, falling rain and snow.
        [Header("Night and weather")] public float lights, stars, wetness, snow, rain, snowfall;
        // 0.74: light ground mist in low areas (Dawn).
        public float mist;

        public static LookPreset Lerp(LookPreset a, LookPreset b, float t)
        {
            float F(float x, float y) => Mathf.Lerp(x, y, t);
            Color C(Color x, Color y) => Color.Lerp(x, y, t);
            return new LookPreset
            {
                name = t < .5f ? a.name : b.name,
                sunElevation = F(a.sunElevation, b.sunElevation), sunAzimuth = Mathf.LerpAngle(a.sunAzimuth, b.sunAzimuth, t),
                sunIntensity = F(a.sunIntensity, b.sunIntensity), shadowStrength = F(a.shadowStrength, b.shadowStrength),
                sunColor = C(a.sunColor, b.sunColor),
                ambientSky = C(a.ambientSky, b.ambientSky), ambientEquator = C(a.ambientEquator, b.ambientEquator), ambientGround = C(a.ambientGround, b.ambientGround), ambientIntensity = F(a.ambientIntensity, b.ambientIntensity),
                skyTint = C(a.skyTint, b.skyTint), skyGround = C(a.skyGround, b.skyGround), skyExposure = F(a.skyExposure, b.skyExposure), atmosphere = F(a.atmosphere, b.atmosphere), sunSize = F(a.sunSize, b.sunSize),
                fogColor = C(a.fogColor, b.fogColor), fogStart = F(a.fogStart, b.fogStart), fogEnd = F(a.fogEnd, b.fogEnd),
                sunBoost = F(a.sunBoost, b.sunBoost), ambientScale = F(a.ambientScale, b.ambientScale), shadowLift = F(a.shadowLift, b.shadowLift), roadSheen = F(a.roadSheen, b.roadSheen), groundVariation = F(a.groundVariation, b.groundVariation),
                postExposure = F(a.postExposure, b.postExposure), contrast = F(a.contrast, b.contrast), saturation = F(a.saturation, b.saturation),
                bloomIntensity = F(a.bloomIntensity, b.bloomIntensity), bloomThreshold = F(a.bloomThreshold, b.bloomThreshold),
                colorFilter = C(a.colorFilter, b.colorFilter), waterSmoothness = F(a.waterSmoothness, b.waterSmoothness),
                lights = F(a.lights, b.lights), stars = F(a.stars, b.stars), wetness = F(a.wetness, b.wetness), snow = F(a.snow, b.snow), rain = F(a.rain, b.rain), snowfall = F(a.snowfall, b.snowfall),
                mist = F(a.mist, b.mist)
            };
        }
        public LookPreset Copy() => Lerp(this, this, 0);
    }

    // Saved as ints: Day 0, Dusk 1, Night 2, Dawn 3 (appended in 0.74; the menus show Dawn / Day / Dusk / Night).
    public enum TimeOfDay { Day, Dusk, Night, Dawn }
    public enum Weather { Clear, Rain, Snow }

    public static class LookPresets
    {
        // Chastain Rush: Racer's presets are made for its own Clear Day (fog 180-1600 m, sun 1.15, sky tint (.42, .55, .78), a
        // graded image); this game's day has fog 100-650 m, sun 1.25, the SkyGradient's zenith (.28, .48, .80) and no grading.
        // The other presets keep their place relative to Racer's day: fog distances and sun scaled, the sky tint scaled per
        // channel, the grading (exposure, contrast, saturation, bloom) as the difference from Racer's day.
        const float FogStartScale = 100f / 180f, FogEndScale = 650f / 1600f, SunScale = 1.25f / 1.15f;
        static readonly Color RacerDayTint = new Color(.42f, .55f, .78f), DayZenith = new Color(.28f, .48f, .80f);
        static LookPreset FromRacer(LookPreset p)
        {
            p.fogStart *= FogStartScale; p.fogEnd *= FogEndScale; p.sunIntensity *= SunScale;
            p.skyTint = new Color(p.skyTint.r * DayZenith.r / RacerDayTint.r, p.skyTint.g * DayZenith.g / RacerDayTint.g, p.skyTint.b * DayZenith.b / RacerDayTint.b);
            p.postExposure -= .1f; p.contrast -= 12; p.saturation -= 12; p.bloomIntensity = Mathf.Max(0, p.bloomIntensity - .2f);
            return p;
        }
        // Chastain Rush: Day is this game's accepted day (MapBuilder.BuildSky), unchanged.
        public static LookPreset ClearDay => new LookPreset
        {
            name = "Clear Day",
            sunElevation = 48, sunAzimuth = 325, sunIntensity = 1.25f, shadowStrength = 1f,
            sunColor = new Color(1f, .96f, .88f),
            ambientSky = new Color(.58f, .68f, .84f), ambientEquator = new Color(.52f, .55f, .55f), ambientGround = new Color(.30f, .28f, .25f), ambientIntensity = 1,
            skyTint = DayZenith, skyGround = new Color(.74f, .80f, .88f), skyExposure = 1f, atmosphere = 1f, sunSize = .035f,
            fogColor = new Color(.74f, .80f, .88f), fogStart = 100, fogEnd = 650,
            sunBoost = .7f, ambientScale = .88f, shadowLift = .15f, roadSheen = .18f, groundVariation = 1,
            postExposure = 0, contrast = 0, saturation = 0, bloomIntensity = 0, bloomThreshold = 1.05f, waterSmoothness = .92f,
            colorFilter = Color.white
        };
        // Warm low sun from the west-north-west, long shadows, amber haze; vehicle lamps on at half.
        public static LookPreset Dusk => FromRacer(new LookPreset
        {
            name = "Dusk",
            sunElevation = 9, sunAzimuth = 286, sunIntensity = .95f, shadowStrength = .8f,
            sunColor = new Color(1f, .64f, .40f),
            ambientSky = new Color(.46f, .46f, .60f), ambientEquator = new Color(.60f, .47f, .40f), ambientGround = new Color(.25f, .21f, .19f), ambientIntensity = .92f,
            skyTint = new Color(.50f, .48f, .60f), skyGround = new Color(.52f, .42f, .38f), skyExposure = 1.05f, atmosphere = 1.35f, sunSize = .05f,
            fogColor = new Color(.66f, .55f, .52f), fogStart = 150, fogEnd = 1350,
            sunBoost = .8f, ambientScale = .82f, shadowLift = .12f, roadSheen = .32f, groundVariation = 1,
            postExposure = .18f, contrast = 13, saturation = 10, bloomIntensity = .35f, bloomThreshold = 1f, waterSmoothness = .92f,
            colorFilter = new Color(1f, .95f, .89f), lights = .55f
        });
        // Moonlit: a pale blue moon (the main light; 0.74: drawn by WeatherEffects with its phase), dark blue sky and haze, stars, lamps on.
        // Kept bright enough to race: lifted exposure and ambient, headlights and glowing markings carry the road ahead.
        public static LookPreset Night => FromRacer(new LookPreset
        {
            name = "Night",
            sunElevation = 38, sunAzimuth = 140, sunIntensity = .36f, shadowStrength = .55f,
            sunColor = new Color(.62f, .73f, 1f),
            ambientSky = new Color(.24f, .30f, .48f), ambientEquator = new Color(.18f, .22f, .32f), ambientGround = new Color(.08f, .09f, .12f), ambientIntensity = 1,
            skyTint = new Color(.12f, .17f, .34f), skyGround = new Color(.06f, .08f, .13f), skyExposure = .28f, atmosphere = .6f, sunSize = 0,
            fogColor = new Color(.07f, .10f, .18f), fogStart = 90, fogEnd = 950,
            sunBoost = .7f, ambientScale = .62f, shadowLift = .1f, roadSheen = .25f, groundVariation = 1,
            postExposure = .55f, contrast = 8, saturation = -8, bloomIntensity = .55f, bloomThreshold = .9f, waterSmoothness = .95f,
            colorFilter = new Color(.86f, .91f, 1f), lights = 1, stars = 1
        });
        // 0.74 Dawn: low sun in the east (the opposite side of the sky from Dusk), cool pink-to-pale-gold light, bluish
        // shadows, a pale lavender-pink haze and light ground mist in the low areas; vehicle lamps on at half, as at Dusk.
        public static LookPreset Dawn => FromRacer(new LookPreset
        {
            name = "Dawn",
            sunElevation = 8, sunAzimuth = 96, sunIntensity = .9f, shadowStrength = .72f,
            sunColor = new Color(1f, .79f, .70f),
            ambientSky = new Color(.48f, .55f, .74f), ambientEquator = new Color(.55f, .52f, .62f), ambientGround = new Color(.21f, .22f, .27f), ambientIntensity = .92f,
            skyTint = new Color(.62f, .50f, .72f), skyGround = new Color(.70f, .56f, .63f), skyExposure = 1.0f, atmosphere = 1.1f, sunSize = .045f,
            fogColor = new Color(.77f, .67f, .74f), fogStart = 110, fogEnd = 1150,
            sunBoost = .75f, ambientScale = .84f, shadowLift = .14f, roadSheen = .26f, groundVariation = 1,
            postExposure = .16f, contrast = 11, saturation = 6, bloomIntensity = .3f, bloomThreshold = 1f, waterSmoothness = .93f,
            colorFilter = new Color(.97f, .96f, 1f), lights = .55f, mist = 1
        });
        public static LookPreset ForTime(TimeOfDay t) => t == TimeOfDay.Dusk ? Dusk : t == TimeOfDay.Night ? Night : t == TimeOfDay.Dawn ? Dawn : ClearDay;
        // Weather on top of any time of day. Brightness follows the base (night rain is dark grey, not daylight grey).
        public static LookPreset WithWeather(LookPreset b, Weather w)
        {
            if (w == Weather.Clear) return b;
            var p = b.Copy();
            float bright = Mathf.Clamp(b.fogColor.grayscale / .76f, .1f, 1.1f);
            if (w == Weather.Rain)
            {
                p.name = b.name + " Rain";
                p.sunIntensity *= .38f; p.shadowStrength *= .35f; p.sunSize *= .25f;
                var grey = new Color(.47f, .50f, .55f) * bright; grey.a = 1;
                p.skyTint = Color.Lerp(b.skyTint, grey, .85f); p.skyGround = Color.Lerp(b.skyGround, grey * .9f, .8f); p.skyExposure *= .85f; p.atmosphere *= .7f;
                p.ambientSky = Color.Lerp(b.ambientSky, grey * 1.15f, .5f); p.ambientEquator = Color.Lerp(b.ambientEquator, grey, .5f);
                // Chastain Rush: Racer's 55 / 560 m, scaled as the presets' fog (FromRacer)
                p.fogColor = Color.Lerp(b.fogColor, grey * 1.05f, .8f); p.fogStart = Mathf.Min(b.fogStart, 55 * FogStartScale); p.fogEnd = Mathf.Min(b.fogEnd, 560 * FogEndScale);
                p.roadSheen = Mathf.Max(b.roadSheen, .6f); p.shadowLift = Mathf.Max(b.shadowLift, .3f);
                p.saturation -= 14; p.contrast -= 2; p.wetness = 1; p.rain = 1;
            }
            else
            {
                p.name = b.name + " Snow";
                p.sunIntensity *= .55f; p.shadowStrength *= .45f; p.sunSize *= .4f;
                var pale = new Color(.80f, .83f, .88f) * bright; pale.a = 1;
                p.skyTint = Color.Lerp(b.skyTint, pale, .8f); p.skyGround = Color.Lerp(b.skyGround, pale, .8f); p.atmosphere *= .7f;
                p.ambientSky = Color.Lerp(b.ambientSky, pale, .45f); p.ambientGround = Color.Lerp(b.ambientGround, pale * .8f, .5f);
                // Chastain Rush: Racer's 50 / 520 m, scaled as the presets' fog (FromRacer)
                p.fogColor = Color.Lerp(b.fogColor, pale, .85f); p.fogStart = Mathf.Min(b.fogStart, 50 * FogStartScale); p.fogEnd = Mathf.Min(b.fogEnd, 520 * FogEndScale);
                p.shadowLift = Mathf.Max(b.shadowLift, .3f); p.saturation -= 8; p.snow = 1; p.snowfall = 1;
            }
            return p;
        }
        public static LookPreset Compose(TimeOfDay t, Weather w) => WithWeather(ForTime(t), w);
        public static readonly string[] Names = { "Clear Day", "Dusk", "Night", "Dawn" };
        public static LookPreset Get(string name) => name == "Dusk" ? Dusk : name == "Night" ? Night : name == "Dawn" ? Dawn : ClearDay;
        // Menu order of the race Time of Day option (saved values: Day 0, Dusk 1, Night 2, Dawn 3).
        public static readonly TimeOfDay[] MenuOrder = { TimeOfDay.Dawn, TimeOfDay.Day, TimeOfDay.Dusk, TimeOfDay.Night };
    }

    public sealed class WorldLook : MonoBehaviour
    {
        // Free Roam: game hours per real minute (1 = a full day in 24 minutes). Dan will tune this one value.
        public const float FreeRoamHoursPerRealMinute = 1f;
        public const float FreeRoamStartHour = 8f;

        public static bool Disabled { get; private set; }
        // Evidence switches (frame-rate cost of each part): -lookNoBloom, -lookNoHDR, -lookNoPost.
        static bool Arg(string a) => Array.IndexOf(Environment.GetCommandLineArgs(), a) >= 0;
        public static WorldLook Current { get; private set; }
        public LookPreset Preset { get; private set; }
        // What is showing now: "Race" (chosen conditions) or "Free Roam" (the live cycle). Chastain Rush: no "Menu" look.
        public string Mode { get; private set; } = "Free Roam";
        public TimeOfDay RaceTime { get; private set; }
        public Weather RaceWeather { get; private set; }
        public Weather RoamWeather { get; private set; }
        public float Hour { get; private set; } = FreeRoamStartHour;
        // 0.74: the Free Roam calendar (1-30, advancing at midnight) and the moon (0 new .. 0.5 full .. 1 new again).
        public const int CalendarDays = 30;
        public int Day { get; private set; } = 1;
        public float MoonPhase => Mode == "Free Roam" ? PhaseOf(Day, Hour) : .5f;// races: a fixed full moon
        // The hour the sky shows now. Chastain Rush: no Trailer Mode
        public float LookHour => Hour;
        public static float PhaseOf(int day, float hour) => Mathf.Repeat((day - 1 + hour / 24f) / CalendarDays, 1);
        public static float Illumination(float phase) => (1 - Mathf.Cos(phase * 2 * Mathf.PI)) * .5f;
        public string Clock => $"{Mathf.FloorToInt(Hour) % 24:00}:{Mathf.FloorToInt(Hour * 60) % 60:00}";
        public string RoamClock => $"Day {Day} {Clock}";
        public string Conditions => Mode == "Free Roam" ? $"Free Roam {RoamClock} ({Preset?.name}) / {RoamWeather}" : $"{RaceTime} / {RaceWeather}";
        // Chastain Rush: how far under the terrain the camera is (0 in the open .. 1 well inside the network)
        public float Underground { get; private set; }
        Light sun; Material sky; Volume volume; ColorAdjustments color; Bloom bloom; Tonemapping tone;
        float nextCameraCheck;
        string session = "Free Roam"; string applied = "";
        public static event Action<LookPreset> Applied;

        // Chastain Rush: made with the first scene (Racer: with every scene that has a RaceDirector)
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            Disabled = Array.IndexOf(Environment.GetCommandLineArgs(), "-lookOff") >= 0;
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += (_, _) => Ensure();
            Application.quitting += Restore;
            Ensure();
        }
        static void Ensure()
        {
            if (Disabled || FindAnyObjectByType<WorldLook>()) return;
            if (!FindAnyObjectByType<ArcadeVehicle>()) return;   // Chastain Rush: the world scene (Racer: a RaceDirector)
            new GameObject("World look").AddComponent<WorldLook>();
        }
        void Awake()
        {
            Current = this;
            var s = GameSettings.Settings; Resume(s);
            Apply(LookPresets.ClearDay); applied = "";
            if (!GetComponent<WeatherEffects>()) gameObject.AddComponent<WeatherEffects>();
            if (!GetComponent<ChastainRush.NightLamps>()) gameObject.AddComponent<ChastainRush.NightLamps>();   // Chastain Rush: street lamps and light poles at night
        }
        void OnDestroy() { if (session == "Free Roam") SaveClock(); if (Current == this) Current = null; if (sky) Destroy(sky); if (volume) Destroy(volume.sharedProfile); foreach (var m in water.Values) if (m) Destroy(m); foreach (var e in iceRenderers) foreach (var m in e.ice) if (m && m.name == "Frozen water (world look)") Destroy(m); grass.Restore(); }

        // ---------- 0.72 conditions: which look shows now ----------
        void Update()
        {
            var s = GameSettings.Settings;
            // Chastain Rush: the race's conditions are the race setup's (game layer); the Free Roam weather and clock are the world's
            var race = ChastainRush.GameMenus.Current;
            RaceTime = (TimeOfDay)Mathf.Clamp(race.timeOfDay, 0, 3); RaceWeather = (Weather)Mathf.Clamp(race.weather, 0, 2);
            RoamWeather = (Weather)Mathf.Clamp(s.roamWeather, 0, 2);
            var rc = ChastainRush.RaceController.Instance;
            string was = session;
            session = rc && rc.State != ChastainRush.RaceController.Stage.Free ? "Race" : "Free Roam";
            if (session == "Free Roam" && was != "Free Roam") Resume(s);
            // 0.74: Free Roam ended (a race) - its clock is saved; it resumes from there next time.
            if (was == "Free Roam" && session != "Free Roam") SaveClock();
            Mode = session;
            Underground = Mathf.MoveTowards(Underground, UndergroundAt(Camera.main), Time.unscaledDeltaTime * 2f);
            if (pinned != null) return;
            if (Mode == "Free Roam")
            {
                float h = Hour + Time.deltaTime / 60f * FreeRoamHoursPerRealMinute;   // scaled time: the clock stands while a menu pauses the game
                if (h >= 24) Day = Day % CalendarDays + 1;// midnight: the next day (30 wraps to 1)
                Hour = Mathf.Repeat(h, 24);
                s.roamHour = Hour; s.roamDay = Day;// kept in the settings in memory; written to disk when Free Roam ends or the game quits
                Apply(Cycle(Hour, RoamWeather, MoonPhase)); applied = "Free Roam";
            }
            else
            {
                string key = $"Race {RaceTime} {RaceWeather}";
                if (key != applied || Underground > 0f || lastUnder > 0f) { Apply(LookPresets.Compose(RaceTime, RaceWeather)); applied = key; }
            }
            if (Time.unscaledTime >= nextSave && Mode == "Free Roam") { nextSave = Time.unscaledTime + 60f; SaveClock(); }   // Chastain Rush: no menu flow to save it at; once a minute
        }
        float nextSave = 60f, lastUnder;
        // The saved Free Roam clock (a missing or unreadable value falls back to day 12, 08:00).
        void Resume(GameSettings.Options s)
        {
            Hour = float.IsNaN(s.roamHour) || s.roamHour < 0 || s.roamHour >= 24 ? FreeRoamStartHour : s.roamHour;
            Day = s.roamDay < 1 || s.roamDay > CalendarDays ? 12 : s.roamDay;
            s.roamHour = Hour; s.roamDay = Day;
        }
        void SaveClock()
        {
            var s = GameSettings.Settings; s.roamHour = Hour; s.roamDay = Day; GameSettings.SaveSettings();
        }
        void OnApplicationQuit() { if (session == "Free Roam") SaveClock(); }
        // Evidence / checks: set the Free Roam calendar directly.
        public void SetClock(int day, float hour) { Day = Mathf.Clamp(day, 1, CalendarDays); Hour = Mathf.Repeat(hour, 24); }
        // Evidence only: show one preset until unpinned.
        LookPreset pinned;
        public void Pin(LookPreset p) { pinned = p; if (p != null) Apply(p); applied = ""; }
        // The Free Roam day: presets blended by hour (night -> dawn -> day -> dusk -> night); the sun and moon move continuously.
        // The main light follows the sun while it is up and the moon otherwise; it fades to nothing at each horizon crossing,
        // so the switch never pops a shadow.
        // 0.74: the early morning passes through the Dawn preset (3), the evening through Dusk (1).
        static readonly (float hour, int preset)[] keys = { (0, 2), (4.6f, 2), (6f, 3), (7.6f, 0), (17.8f, 0), (19.4f, 1), (20.8f, 2), (24, 2) };
        // The sun is up 06:00-20:00 (rising ENE, setting WNW, highest 58 degrees); the moon has the night (highest ~36 degrees).
        public static (float elevation, float azimuth, bool sun) Sky(float hour)
        {
            if (hour >= 6 && hour < 20) return (58 * Mathf.Sin(Mathf.PI * (hour - 6) / 14), 75 + 210 * (hour - 6) / 14, true);
            float n = Mathf.Repeat(hour - 20, 24);
            return (36 * Mathf.Sin(Mathf.PI * n / 10), 105 + 150 * n / 10, false);
        }
        // 0.74 moon: it rises in the east about six hours before it is highest and sets six hours after; highest at noon on
        // day 1 (new, unseen), at midnight around day 15 (full). Returns elevation (negative = below the horizon) and azimuth.
        public static (float elevation, float azimuth) Moon(float hour, float phase)
        {
            float transit = Mathf.Repeat(12 + phase * 24, 24);
            float d = Mathf.Repeat(hour - transit + 12, 24) - 12;// hours from the moon's highest point, -12..12
            return (52 * Mathf.Cos(d / 24f * 2 * Mathf.PI) - 2, 180 + Mathf.Clamp(d / 6f, -1.4f, 1.4f) * 90);
        }
        public static LookPreset Cycle(float hour, Weather weather) => Cycle(hour, weather, .5f);
        public static LookPreset Cycle(float hour, Weather weather, float phase)
        {
            LookPreset P(int i) => i == 0 ? LookPresets.ClearDay : i == 1 ? LookPresets.Dusk : i == 3 ? LookPresets.Dawn : LookPresets.Night;
            int k = 0; while (k < keys.Length - 2 && hour >= keys[k + 1].hour) k++;
            float t = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(keys[k].hour, keys[k + 1].hour, hour));
            var p = LookPreset.Lerp(P(keys[k].preset), P(keys[k + 1].preset), t);
            var (elev, az, isSun) = Sky(hour);
            float fade;
            if (isSun)
            {
                p.sunElevation = Mathf.Max(elev, 1); p.sunAzimuth = az;
                // Off within 2 degrees of the horizon, so the switch between sun and moon never shows.
                fade = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(2, 9, elev));
            }
            else
            {
                // Night light: the moon while it is up (brighter near full), otherwise a dim sky glow from high in the south;
                // both fade out at 6 degrees, so the change of direction never shows. "elev" is the old night arc, kept only
                // for the hand-over from and to the sun (it starts and ends at the horizon at 20:00 and 06:00).
                var (me, ma) = Moon(hour, phase); float lit = Illumination(phase);
                float handover = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(2, 9, elev));
                if (me >= 6) { p.sunElevation = me; p.sunAzimuth = ma; fade = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(6, 14, me)) * Mathf.Lerp(.55f, 1.12f, lit); }
                else { p.sunElevation = 62; p.sunAzimuth = 180; fade = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(6, 0, me)) * .5f; }
                fade *= handover;
                // Moonlit nights a little brighter near full, darker near new, always playable.
                float bright = Mathf.Lerp(.86f, 1.06f, lit) * p.stars + (1 - p.stars);
                p.ambientIntensity *= bright; p.postExposure += Mathf.Lerp(-.06f, .04f, lit) * p.stars;
                p.sunSize = 0;// the moon is drawn by WeatherEffects with its phase, not as the sky's sun disc
            }
            p.sunIntensity *= fade; p.shadowStrength *= fade;
            p.name = hour < 4.6f || hour >= 20.8f ? "Night" : hour < 7.6f ? "Dawn" : hour < 17.8f ? "Day" : "Dusk";
            return LookPresets.WithWeather(p, weather);
        }

        // Chastain Rush: the camera under the terrain (the pipes, junctions and the hub): 0 at the surface, 1 from 6 m down
        public static float UndergroundAt(Camera cam)
        {
            if (!cam) return 0;
            var p = cam.transform.position;
            foreach (var t in Terrain.activeTerrains)
            {
                if (!t || !t.terrainData) continue; var o = t.GetPosition(); var size = t.terrainData.size;
                if (p.x < o.x || p.z < o.z || p.x > o.x + size.x || p.z > o.z + size.z) continue;
                float depth = t.SampleHeight(p) + o.y - p.y;
                return Mathf.SmoothStep(0, 1, Mathf.InverseLerp(1f, 6f, depth));
            }
            return 0;
        }

        public void Apply(LookPreset p)
        {
            // Chastain Rush: underground, the network keeps the day's light (its lamps, the ambient it was built under); the sky,
            // sun and fog outside stay the conditions' (a mouth looks out on them)
            lastUnder = Underground;
            if (Underground > 0f)
            {
                var day = LookPresets.ClearDay; var u = LookPreset.Lerp(p, day, Underground);
                u.sunElevation = p.sunElevation; u.sunAzimuth = p.sunAzimuth; u.sunIntensity = p.sunIntensity; u.sunColor = p.sunColor; u.shadowStrength = p.shadowStrength;
                u.skyTint = p.skyTint; u.skyGround = p.skyGround; u.skyExposure = p.skyExposure; u.sunSize = p.sunSize;
                u.stars = p.stars; u.lights = p.lights; u.rain = p.rain; u.snowfall = p.snowfall; u.mist = p.mist; u.name = p.name;
                u.wetness = p.wetness * (1 - Underground); u.snow = p.snow * (1 - Underground);
                p = u;
            }
            Preset = p;
            // Sun: the scene's main directional light (created if a scene has none).
            if (!sun)
            {
                foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None))
                    if (l.type == LightType.Directional && l.enabled && (!sun || l.intensity > sun.intensity)) sun = l;
                if (!sun) { sun = new GameObject("Sun").AddComponent<Light>(); sun.type = LightType.Directional; }
            }
            sun.transform.rotation = Quaternion.Euler(p.sunElevation, p.sunAzimuth, 0);
            sun.color = p.sunColor; sun.intensity = p.sunIntensity; sun.shadows = LightShadows.Soft; sun.shadowStrength = p.shadowStrength;
            sun.enabled = p.sunIntensity > .001f;
            if (RenderSettings.sun != sun) RenderSettings.sun = sun;
            // Ambient: sky / horizon / ground, so shaded sides keep colour.
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = p.ambientSky * p.ambientIntensity; RenderSettings.ambientEquatorColor = p.ambientEquator * p.ambientIntensity; RenderSettings.ambientGroundColor = p.ambientGround * p.ambientIntensity;
            // Sky. Chastain Rush: this game's SkyGradient (zenith, horizon = the haze, the sun's glow), not the procedural sky
            if (!sky && RenderSettings.skybox && RenderSettings.skybox.HasProperty("_Zenith")) sky = new Material(RenderSettings.skybox) { name = "World look sky" };
            if (sky)
            {
                sky.SetColor("_Zenith", p.skyTint * p.skyExposure); sky.SetColor("_Horizon", p.fogColor);
                sky.SetColor("_SunColor", new Color(1f, .92f, .75f) * Mathf.Clamp01(p.sunSize / .035f) * Mathf.Clamp01(p.sunIntensity / 1.25f));
                var toSun = -sun.transform.forward; sky.SetVector("_SunDir", new Vector4(toSun.x, toSun.y, toSun.z, 0f));
                if (RenderSettings.skybox != sky) RenderSettings.skybox = sky;   // Chastain Rush: set once (not every frame)
            }
            // The environment (ambient probe) is refreshed only when a preset changes noticeably, not every cycle frame.
            if (environmentKey < 0 || Mathf.Abs(p.skyExposure - environmentKey) > .01f || Mathf.Abs(p.ambientSky.grayscale * p.ambientIntensity - environmentTint) > .01f)
            { DynamicGI.UpdateEnvironment(); environmentKey = p.skyExposure; environmentTint = p.ambientSky.grayscale * p.ambientIntensity; }
            // Distance haze, matched to the horizon.
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Linear; RenderSettings.fogColor = p.fogColor;
            RenderSettings.fogStartDistance = p.fogStart; RenderSettings.fogEndDistance = p.fogEnd;
            // Ground response. Chastain Rush: ChastainRush/VertexLit reads _RacerWet and _RacerSnow; the terrain's grass is tinted
            Shader.SetGlobalFloat("_RacerLook", 1);
            Shader.SetGlobalFloat("_RacerWet", p.wetness); Shader.SetGlobalFloat("_RacerSnow", p.snow);
            Shader.SetGlobalFloat("_RacerLamps", Mathf.Clamp01(p.lights * 1.6f) * (1 - Underground));   // Chastain Rush: VertexLit lit by headlights and lamps at night only
            grass.Set(p.wetness, p.snow);
            // Post-processing. Chastain Rush: no tonemapping and a neutral grade by day (this game's day has none); the grade and
            // bloom of the other presets, relative to Racer's day (LookPresets.FromRacer). Nothing else (no blur, grain, vignette...).
            if (!volume)
            {
                volume = new GameObject("World look volume").AddComponent<Volume>(); volume.transform.SetParent(transform, false);
                volume.isGlobal = true; volume.priority = 100;
                var profile = ScriptableObject.CreateInstance<VolumeProfile>(); profile.name = "World look";
                tone = profile.Add<Tonemapping>(true); color = profile.Add<ColorAdjustments>(true); bloom = profile.Add<Bloom>(true);
                volume.sharedProfile = profile;
            }
            tone.mode.Override(TonemappingMode.None);
            color.postExposure.Override(p.postExposure); color.contrast.Override(p.contrast); color.saturation.Override(p.saturation); color.colorFilter.Override(p.colorFilter);
            bloom.intensity.Override(p.bloomIntensity); bloom.threshold.Override(p.bloomThreshold); bloom.scatter.Override(.6f); bloom.highQualityFiltering.Override(false); bloom.active = !Arg("-lookNoBloom") && p.bloomIntensity > .01f;
            // Chastain Rush: post-processing only while a look grades the image (a neutral day draws as before, without its cost:
            // 332 fps by day at the shops without the world look, 208 with post-processing always on; 1280 x 720)
            grading = Mathf.Abs(p.postExposure) > .005f || Mathf.Abs(p.contrast) > .1f || Mathf.Abs(p.saturation) > .1f || bloom.active || p.colorFilter != Color.white;
            Water(p);
            ShallowWater.SetFrozen(p.snow > .5f); Ice();
            Cameras();
            Applied?.Invoke(p);
        }
        float environmentKey = -1, environmentTint;
        bool grading, flashing;
        // 0.73 lightning (WeatherEffects): a brief brightening of the sky and the whole image on top of the current preset;
        // 0 puts the preset back exactly.
        public void Lightning(float f)
        {
            if (Preset == null || !color) return;
            color.postExposure.Override(Preset.postExposure + f * 1.3f);
            if (flashing != f > 0) { flashing = f > 0; Cameras(); }   // Chastain Rush: post-processing for the flash
            if (sky) sky.SetColor("_Zenith", Preset.skyTint * Preset.skyExposure * (1 + f * 2.5f));
        }
        // Water (URP Lit "water" materials): a runtime copy with a smoother surface, and a sky-only reflection probe over each
        // lake rendered once, so lakes and ponds read as water. The authored materials are not changed.
        readonly Dictionary<Material, Material> water = new();
        readonly List<ReflectionProbe> probes = new();
        float probeExposure = -1;
        void Water(LookPreset p)
        {
            if (water.Count == 0)
            {
                foreach (var r in FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
                {
                    var mats = r.sharedMaterials; bool any = false;
                    for (int i = 0; i < mats.Length; i++)
                    {
                        var m = mats[i]; if (!m || m.name.IndexOf("water", StringComparison.OrdinalIgnoreCase) < 0 || !m.HasProperty("_Smoothness") || !m.shader.name.StartsWith("Universal Render Pipeline/Lit")) continue;
                        if (!water.TryGetValue(m, out var copy)) water[m] = copy = new Material(m) { name = m.name + " (world look)" };
                        mats[i] = copy; any = true;
                    }
                    if (!any) continue;
                    r.sharedMaterials = mats;
                    // Chastain Rush: Racer's sky-only reflection probe over each lake left behind: with this map's 22 water areas they cost
                    // about 1.4 ms a frame (331 fps by day at the shops without them, 229 with; 1280 x 720)
                    if (!Arg("-lookWaterProbes")) continue;
                    var b = r.bounds; if (b.size.x < 15 && b.size.z < 15) continue;
                    var probe = new GameObject("World look water reflection").AddComponent<ReflectionProbe>(); probe.transform.SetParent(transform, false);
                    probe.transform.position = b.center; probe.size = b.size + new Vector3(10, 40, 10); probe.mode = ReflectionProbeMode.Realtime;
                    probe.refreshMode = ReflectionProbeRefreshMode.ViaScripting; probe.cullingMask = 0; probe.resolution = 128; probe.clearFlags = ReflectionProbeClearFlags.Skybox; probe.importance = 2;
                    probes.Add(probe);
                }
            }
            foreach (var copy in water.Values) copy.SetFloat("_Smoothness", p.waterSmoothness);
            // The sky reflection is re-rendered when the sky changes (a race's conditions, or every few game minutes of the cycle).
            if (probeExposure < 0 || Mathf.Abs(probeExposure - p.skyExposure) > .04f) { foreach (var probe in probes) if (probe) probe.RenderProbe(); probeExposure = p.skyExposure; }
        }
        // 0.73 Snow: every water surface (lakes, creeks, pools) shows as ice - pale blue-white, matte with a soft sheen, a light
        // dusting of snow, no animation and no sky reflection. Each water renderer gets its own ice copy (the dusting tiles about
        // every 6 m); the unfrozen materials are put back for Clear and Rain.
        // Chastain Rush: never the water underground (the pipes' streams, the hub's pool, the aquariums' glass stretches)
        readonly List<(Renderer r, Material[] water, Material[] ice)> iceRenderers = new();
        bool iceScanned, iceShown;
        static bool IsWater(Material m) => m && (m.name.IndexOf("water", StringComparison.OrdinalIgnoreCase) >= 0 || m.name.StartsWith("Lake blue green") || m.name.StartsWith("Pool turquoise"));
        static bool BelowGround(Bounds b)
        {
            foreach (var t in Terrain.activeTerrains)
            {
                if (!t || !t.terrainData) continue; var o = t.GetPosition(); var size = t.terrainData.size; var c = b.center;
                if (c.x < o.x || c.z < o.z || c.x > o.x + size.x || c.z > o.z + size.z) continue;
                return b.max.y < t.SampleHeight(c) + o.y - 2f;
            }
            return false;
        }
        void Ice()
        {
            bool frozen = ShallowWater.Frozen;
            if (frozen && !iceScanned)
            {
                iceScanned = true;
                var template = Resources.Load<Material>("WaterIce");
                if (template)
                    foreach (var r in FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                    {
                        var mats = r.sharedMaterials; if (!r.GetComponent<ShallowWater>() && !Array.Exists(mats, IsWater)) continue;
                        if (BelowGround(r.bounds)) continue;   // Chastain Rush
                        var m = new Material(template) { name = "Frozen water (world look)" };
                        if (r.GetComponent<MeshFilter>()?.sharedMesh?.name is "Cube" or "Cylinder") m.SetTextureScale("_BaseMap", new Vector2(Mathf.Max(1, Mathf.Round(r.transform.lossyScale.x / 6)), Mathf.Max(1, Mathf.Round(r.transform.lossyScale.z / 6))));
                        bool all = r.GetComponent<ShallowWater>(); var frozenMats = new Material[mats.Length];
                        for (int k = 0; k < mats.Length; k++) frozenMats[k] = all || IsWater(mats[k]) ? m : mats[k];
                        iceRenderers.Add((r, mats, frozenMats));
                    }
            }
            if (frozen == iceShown) return;
            iceShown = frozen;
            foreach (var (r, w, i) in iceRenderers) if (r) r.sharedMaterials = frozen ? i : w;
        }
        // Chastain Rush: the terrain's grass layer (MapBuilder: one flat 4 x 4 colour; URP's terrain reads smoothness from alpha)
        // is darkened and given a sheen when wet and whitened under snow, on a runtime copy (the built layer is put back)
        readonly TerrainGrass grass = new();
        sealed class TerrainGrass
        {
            TerrainLayer layer; Texture2D original, shown; Color dry; float wet = -1, snow = -1;
            public void Set(float w, float s)
            {
                if (Mathf.Abs(w - wet) < .01f && Mathf.Abs(s - snow) < .01f) return;
                if (!layer)
                {
                    var t = Terrain.activeTerrain; if (!t || !t.terrainData || t.terrainData.terrainLayers.Length == 0) return;
                    layer = t.terrainData.terrainLayers[0]; original = layer ? layer.diffuseTexture : null; if (!layer) return;
                    dry = original && original.isReadable ? original.GetPixel(0, 0) : new Color(.36f, .46f, .27f, 0f);   // MapBuilder's grass
                    shown = new Texture2D(4, 4, TextureFormat.RGBA32, false) { name = "Grass (world look)" };
                    layer.diffuseTexture = shown;
                    foreach (var tt in Terrain.activeTerrains) if (tt && tt.terrainData) tt.terrainData.terrainLayers = tt.terrainData.terrainLayers;
                }
                wet = w; snow = s;
                var c = Color.Lerp(dry, new Color(dry.r * .72f, dry.g * .78f, dry.b * .74f, .42f), w);
                c = Color.Lerp(c, new Color(.86f, .88f, .92f, .18f), s);
                var px = new Color[16]; for (int i = 0; i < 16; i++) px[i] = c; shown.SetPixels(px); shown.Apply();
                foreach (var tt in Terrain.activeTerrains) if (tt && tt.terrainData) tt.terrainData.SetBaseMapDirty();   // the far terrain's base map too
            }
            public void Restore() { if (layer && original) { layer.diffuseTexture = original; foreach (var tt in Terrain.activeTerrains) if (tt && tt.terrainData) tt.terrainData.SetBaseMapDirty(); } if (shown) Destroy(shown); }
        }
        // Every camera renders post-processing.
        void Cameras()
        {
            foreach (var c in Camera.allCameras)
            {
                var data = c.GetUniversalAdditionalCameraData();
                if (data && c.targetTexture == null) data.renderPostProcessing = !Arg("-lookNoPost") && (grading || flashing);   // Chastain Rush: only while grading
                if (Arg("-lookNoHDR")) c.allowHDR = false;
            }
        }
        void LateUpdate() { if (Time.unscaledTime >= nextCameraCheck) { nextCameraCheck = Time.unscaledTime + 1; Cameras(); } }

        // Editor play mode: clear the shader globals so edit-mode views show the built look.
        static void Restore() { Shader.SetGlobalFloat("_RacerLamps", 0); Shader.SetGlobalFloat("_RacerLook", 0); Shader.SetGlobalFloat("_RacerWet", 0); Shader.SetGlobalFloat("_RacerSnow", 0); }
    }
}
