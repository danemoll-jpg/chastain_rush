using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ChastainRush
{
    // Round 51 (Parts B and C, Dan 2026-10-19: "We will use this screen for loading screens as well"): Racer's LoadingScreen
    // (0.82 Part B, 0.83 Part E), ported. A full-screen loading screen with Dan's cover (Resources/Title/Cover, SourceArt/Title/
    // ChastainRushCover.png unaltered) scaled to cover the screen without stretching (cropped evenly on other aspect ratios), what
    // is loading in a dark band along the bottom (clear of the lettering and the car), the real progress bar, one rotating tip and,
    // for a race, its route inset on the map (bottom right, above the cover's OpenStreetMap credit). It stays up until the work is
    // done and the first frames have rendered, then fades out; no input is needed. While it is up a race's countdown is held
    // (RaceController) and the menus take no input.
    // Chastain Rush is one scene, so where Racer covers a scene load this covers: the launch (the scene's load, then the title menu
    // being made, then the first frames), a race start or restart (RaceController.StartRace: the grid, the AI, the barriers) and
    // Free Roam (from the title menu, or leaving a race). Racer's world-building reports (SceneryWorld) have no counterpart here:
    // the world is baked by Build Map. Never in a check run, except Round 51's (-exeRound51).
    // Left behind: Racer's CoursePreviewCatalog (the route is this game's RaceRoute), the vehicle and conditions lines, LeviathanCourse.
    public sealed class LoadingScreen : MonoBehaviour
    {
        static LoadingScreen instance;
        // up and not yet fading out
        public static bool Holding => instance && instance.holding;
        public static int Shown { get; private set; }
        public static string LastTitle { get; private set; } = "";
        public static float LastSeconds { get; private set; }
        public static float Bar => instance ? instance.bar : 0f;
        public static string TipText => instance && instance.tip ? instance.tip.text : "";
        public static int TipChanges { get; private set; }
        public static bool RouteShown => instance && instance.mapFrame && instance.mapFrame.gameObject.activeSelf;
        public static bool Allowed => !CheckRun.Active || Array.IndexOf(Environment.GetCommandLineArgs(), "-exeRound51") >= 0;

        // the bar: the load itself 0-30 %, the work (title menu, race) 30-88 %, first rendered frames 88-100 %
        const float SceneShare = .30f, WorldEnd = .88f, StartEnd = .92f;
        const float Fade = .35f;
        // Part C: this game's controls and features (each binding checked in VehicleInput, CameraViews, WorldMap, MiniMap,
        // DeveloperHud, TrailerMode, LocalRadio and the menus, Round 51). American English.
        static readonly string[] Tips =
        {
            "Stuck or upside down? R (Y on a controller) puts you back on the road.",
            "V (X on a controller) changes the camera view. Hold the right mouse button or push the right stick to look around.",
            "M (View on a controller) opens the map: F (Y) sets a waypoint to drive to, T (X) travels to a place.",
            "J (B on a controller) shows or hides the minimap.",
            "Worn tire tracks leaving the road mark a shortcut. Pull it off and you save time; fumble it and you lose more.",
            "An official shortcut forgives the checkpoints it skips. A missed checkpoint anywhere else costs 5 seconds.",
            "In the air, W / S (left stick) tips the nose and Q / E (LB / RB) spins you.",
            "100 goose eggs are hidden around the map. GOOSE EGGS on the pause menu shows your count; the map shows the egg areas.",
            "POLICE on the menus: outrun the cops in a Getaway, or catch speeders on Speed Patrol.",
            "The Chastain Corner Stunt Park fills the old Kroger lot: ramps, a loop and the abandoned bank. STUNT EVENT scores your tricks.",
            "Fences, mailboxes, signs, hydrants and small trees break when you hit them. Keep your foot down.",
            "F (LB on a controller) shakes a fist out of the window.",
            "F8 turns on Trailer Mode for filming and screenshots (TRAILER MODE on the pause menu too).",
            "Found a bug? F4 (or click the right stick) captures it with a picture for the report.",
            "The radio: N (D-pad down) changes the station, . (D-pad right) skips the song.",
            "Race setup has Time of Day and Weather; Free Roam has its own weather on the menu.",
        };
        static int tipIndex = -1;

        GameObject canvas; CanvasGroup group; RectTransform fill; Text title, detail, step, tip;
        bool holding; float shown, target, bar, tipAt; bool started;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetProcess() { instance = null; Shown = 0; TipChanges = 0; }
        // the launch: up before the scene's first frame (CheckRun has decided by now: SubsystemRegistration runs first)
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot() { if (Application.isBatchMode || !Allowed) return; Show("CHASTAIN RUSH", "Starting up", null); }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void SceneLoaded() { if (instance && instance.holding) { instance.target = Mathf.Max(instance.target, SceneShare); instance.step.text = "The world"; } }

        // The screen over work in the scene (a race's grid, Free Roam); the caller does the work, then calls Started().
        public static void Cover(string what, string detail, RaceRoute route)
        {
            if (!Allowed || Application.isBatchMode) return;
            Show(what, detail, route); instance.target = SceneShare;
        }
        // the work is done (the title menu is up, the race is on its grid): the first rendered frames, then the fade
        public static void Started() { if (instance && instance.holding) { instance.started = true; instance.target = Mathf.Max(instance.target, WorldEnd); instance.step.text = "Getting ready"; } }

        static void Show(string what, string detail, RaceRoute route)
        {
            if (!instance) { instance = new GameObject("Loading screen (Round 51)").AddComponent<LoadingScreen>(); DontDestroyOnLoad(instance.gameObject); instance.Build(); }
            var s = instance; s.StopAllCoroutines(); s.canvas.SetActive(true); s.group.alpha = 1; s.holding = true; s.started = false;
            s.shown = Time.realtimeSinceStartup; s.target = 0; s.bar = 0; s.title.text = what; s.detail.text = (detail ?? "").Replace("\n", "   ·   "); s.step.text = "Loading";
            s.SetRoute(route); tipIndex = (tipIndex + 1 + UnityEngine.Random.Range(0, Tips.Length - 1)) % Tips.Length; s.tip.text = Tips[tipIndex]; s.tipAt = Time.realtimeSinceStartup;
            Shown++; LastTitle = what; s.StartCoroutine(s.Watch());
        }

        // wait for the work (60 s at most; at the launch, give up after 3 s if there are no menus: a check run), then the first
        // rendered frames, then fade
        IEnumerator Watch()
        {
            float t0 = Time.realtimeSinceStartup;
            while (!started && Time.realtimeSinceStartup - t0 < 60) { if (GameMenus.Instance == null && Time.realtimeSinceStartup - t0 > 3) break; yield return null; }
            step.text = "Getting ready"; target = Mathf.Max(target, StartEnd);
            // the first rendered frames: at least 8, until 4 in a row take under 50 ms (shader compiling and first-use loads
            // show as long frames), 4 s at most
            int frames = 0, smooth = 0; float t1 = Time.realtimeSinceStartup;
            while (frames < 8 || (smooth < 4 && Time.realtimeSinceStartup - t1 < 4)) { yield return null; frames++; smooth = Time.unscaledDeltaTime < .05f ? smooth + 1 : 0; target = Mathf.Max(target, Mathf.Lerp(StartEnd, 1, Mathf.Min(1, frames / 12f))); }
            target = 1; step.text = "Ready"; while (bar < .995f) yield return null;
            holding = false; LastSeconds = Time.realtimeSinceStartup - shown;
            Debug.Log($"[Loading] {LastTitle}: {LastSeconds:0.00} s");
            for (float t = 0; t < Fade; t += Time.unscaledDeltaTime) { group.alpha = 1 - t / Fade; yield return null; }
            canvas.SetActive(false);
        }

        void Update()
        {
            if (!holding) return;
            bar = Mathf.MoveTowards(bar, target, Time.unscaledDeltaTime * 1.6f);
            fill.anchorMax = new Vector2(Mathf.Clamp01(bar), 1);
            if (Time.realtimeSinceStartup - tipAt > 5) { tipIndex = (tipIndex + 1) % Tips.Length; tip.text = Tips[tipIndex]; tipAt = Time.realtimeSinceStartup; TipChanges++; }
        }

        // the race's route on the map's picture (WorldMap's), a small inset in the bottom-right corner
        RectTransform map, mapFrame; RawImage mapImage; WorldMapOverlay routeLines;
        void SetRoute(RaceRoute route)
        {
            var picture = route != null && route.Path != null && route.Path.Length > 1 ? Resources.Load<Texture2D>("WorldMapPicture") : null;
            mapFrame.gameObject.SetActive(picture);
            if (!picture) return;
            var e = GroundQuery.Extent(); var world = e.width > 0 ? e : new Rect(0, 0, 3983, 1918);   // WorldMap's world rectangle
            mapFrame.sizeDelta = new Vector2(110 * world.width / world.height + 8, 118); mapImage.texture = picture;
            Vector2 N(Vector3 p) => new Vector2((p.x - world.xMin) / world.width, (p.z - world.yMin) / world.height);
            Vector2 lo = N(route.Path[0]), hi = lo; foreach (var p in route.Path) { var q = N(p); lo = Vector2.Min(lo, q); hi = Vector2.Max(hi, q); }
            var center = (lo + hi) * .5f; var extent = hi - lo; float zoom = Mathf.Clamp(.85f / Mathf.Max(extent.x, extent.y), 1, 6);
            mapImage.uvRect = new Rect(center - Vector2.one * .5f / zoom, Vector2.one / zoom);
            var lines = new List<WorldMapOverlay.Path> { new WorldMapOverlay.Path { points = route.Path, tint = new Color(.3f, 1f, .88f), closed = true, width = 3f } };
            var bars = new List<WorldMapOverlay.Gate> { new WorldMapOverlay.Gate { at = route.Gate.Position, right = route.Gate.Right, start = true } };
            routeLines.SetView(world, center, zoom, lines, bars);
        }

        // Dan's cover over the whole screen, cropped evenly, never stretched. Where the screen is not the cover's shape the crop can
        // take the painted "(c) OpenStreetMap contributors" off the edge, so the credit is then written in that corner instead.
        // Also the startup title's (StartupTitle).
        public static void AddCover(RectTransform parent, bool creditPlate = false)
        {
            var cover = Resources.Load<Texture2D>("Title/Cover");
            if (!cover) return;
            var art = Rect("Cover", parent); art.anchorMin = art.anchorMax = art.pivot = new Vector2(.5f, .5f);
            var raw = art.gameObject.AddComponent<RawImage>(); raw.texture = cover; raw.raycastTarget = false;
            var fit = art.gameObject.AddComponent<AspectRatioFitter>(); fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent; fit.aspectRatio = cover.width / (float)cover.height;
            var holder = parent;
            if (creditPlate)
            {
                // the loading screen's dark band dims the painted credit: written on a plate over it instead, always
                holder = Rect("OpenStreetMap credit plate", parent); holder.anchorMin = holder.anchorMax = holder.pivot = new Vector2(1, 0); holder.anchoredPosition = Vector2.zero; holder.sizeDelta = new Vector2(200, 24);
                holder.gameObject.AddComponent<Image>().color = new Color(.01f, .016f, .022f, .92f); holder.GetComponent<Image>().raycastTarget = false;
            }
            var r = Rect("OpenStreetMap credit", holder); r.anchorMin = r.anchorMax = r.pivot = new Vector2(1, 0); r.anchoredPosition = new Vector2(-10, 4); r.sizeDelta = new Vector2(320, 20);
            var t = r.gameObject.AddComponent<Text>(); t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); t.fontSize = 13; t.alignment = TextAnchor.LowerRight; t.color = Color.white; t.raycastTarget = false;
            t.text = "© OpenStreetMap contributors"; r.gameObject.AddComponent<Shadow>().effectColor = new Color(0, 0, 0, .8f);
            if (!creditPlate) r.gameObject.AddComponent<CreditWhenCropped>().aspect = fit.aspectRatio;
        }

        // Racer 0.83: the cover behind everything; a gradient band at the bottom with the information, the menus' cyan accent
        void Build()
        {
            canvas = new GameObject("Loading screen", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(CanvasGroup));
            canvas.transform.SetParent(transform, false);
            var c = canvas.GetComponent<Canvas>(); c.renderMode = RenderMode.ScreenSpaceOverlay; c.sortingOrder = 32500; // above the title
            var scaler = canvas.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1280, 720); scaler.matchWidthOrHeight = .5f;
            group = canvas.GetComponent<CanvasGroup>(); group.blocksRaycasts = true;
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var matte = Rect("Matte", canvas.transform); Stretch(matte); matte.gameObject.AddComponent<Image>().color = new Color(.012f, .019f, .025f);
            AddCover(matte, true);
            // the band: transparent at its top edge, dark at the bottom of the screen
            var shade = new Texture2D(1, 64, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, name = "Loading band" };
            for (int y = 0; y < 64; y++) { float u = y / 63f; shade.SetPixel(0, y, new Color(.01f, .016f, .022f, Mathf.Lerp(.9f, 0, Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.35f, 1, u))))); }
            shade.Apply();
            var band = Rect("Band", matte); band.anchorMin = Vector2.zero; band.anchorMax = new Vector2(1, .27f); band.offsetMin = band.offsetMax = Vector2.zero;
            var bandImage = band.gameObject.AddComponent<RawImage>(); bandImage.texture = shade; bandImage.raycastTarget = false;
            band.SetSiblingIndex(1);   // over the cover, under the credit
            var panel = band;
            Text Label(string name, Vector2 min, Vector2 max, int size, Color color, TextAnchor anchor, FontStyle style = FontStyle.Normal)
            {
                var r = Rect(name, panel); r.anchorMin = min; r.anchorMax = max; r.offsetMin = r.offsetMax = Vector2.zero;
                var t = r.gameObject.AddComponent<Text>(); t.font = font; t.fontSize = size; t.color = color; t.alignment = anchor; t.fontStyle = style; t.raycastTarget = false; t.horizontalOverflow = HorizontalWrapMode.Wrap;
                r.gameObject.AddComponent<Shadow>().effectColor = new Color(0, 0, 0, .8f); return t;
            }
            // band coordinates: 0 = bottom of the screen, 1 = 27 % up
            Label("Loading label", new Vector2(.03f, .62f), new Vector2(.5f, .74f), 14, new Color(.3f, 1, .88f), TextAnchor.LowerLeft, FontStyle.Bold).text = "LOADING";
            title = Label("Title", new Vector2(.03f, .40f), new Vector2(.62f, .64f), 32, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            step = Label("Step", new Vector2(.55f, .40f), new Vector2(.80f, .50f), 15, new Color(.86f, .86f, .8f), TextAnchor.LowerRight);
            detail = Label("Detail", new Vector2(.03f, .24f), new Vector2(.80f, .40f), 16, new Color(.9f, .9f, .84f), TextAnchor.MiddleLeft);
            var track = Rect("Bar", panel); track.anchorMin = new Vector2(.03f, .17f); track.anchorMax = new Vector2(.80f, .21f); track.offsetMin = track.offsetMax = Vector2.zero; track.gameObject.AddComponent<Image>().color = new Color(1, 1, 1, .16f);
            fill = Rect("Fill", track); fill.anchorMin = Vector2.zero; fill.anchorMax = new Vector2(0, 1); fill.offsetMin = fill.offsetMax = Vector2.zero; fill.gameObject.AddComponent<Image>().color = new Color(.3f, 1, .88f);
            tip = Label("Tip", new Vector2(.03f, .02f), new Vector2(.80f, .15f), 15, new Color(.8f, .82f, .78f), TextAnchor.MiddleLeft, FontStyle.Italic);
            // the route: a small inset in the bottom-right corner, in a dark frame, above the cover's credit line
            mapFrame = Rect("Route frame", matte); mapFrame.anchorMin = mapFrame.anchorMax = mapFrame.pivot = new Vector2(1, 0); mapFrame.anchoredPosition = new Vector2(-18, 30);
            mapFrame.gameObject.AddComponent<Image>().color = new Color(.02f, .03f, .04f, .9f);
            map = Rect("Route map", mapFrame); Stretch(map); map.offsetMin = new Vector2(4, 4); map.offsetMax = new Vector2(-4, -4);
            mapImage = map.gameObject.AddComponent<RawImage>(); mapImage.raycastTarget = false; map.gameObject.AddComponent<RectMask2D>();
            var overlay = Rect("Route", map); Stretch(overlay); routeLines = overlay.gameObject.AddComponent<WorldMapOverlay>(); routeLines.raycastTarget = false;
            mapFrame.gameObject.SetActive(false);
        }
        static RectTransform Rect(string name, Transform parent) { var r = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); r.SetParent(parent, false); return r; }
        static void Stretch(RectTransform r) { r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero; }
    }
}
