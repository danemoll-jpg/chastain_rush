using System.Collections.Generic;
using System.Linq;
using Racer;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ChastainRush
{
    // Round 20, item 2 (Dan: "I also want to look at adding the maps like we have in Woodstock Rush ... The maps will help me also
    // look for shortcut ideas"). Ported from Racer's ExplorationMap (+ .Controls) and WorldMapCourseOverlay (WorldMapOverlay.cs):
    //   kept    the same button (View / Select on the controller, M on the keyboard) opens and closes it; it pauses the game while
    //           it is open, in a race as well (Racer pauses its RaceFlow); left stick / WASD pans, the triggers / = - zoom, the
    //           right stick's click / Home centers on the player, B / Esc closes; mouse wheel zooms and a drag pans; uGUI built in
    //           code on a 1280 x 720 reference (dark backdrop, the picture in a masked RawImage whose uvRect is the view, markers as
    //           Text: the player's heading arrow, the cursor "+", a north arrow); MenuInput's release barrier on open and close
    //   new     the picture is drawn from the map data at Build Map (MapBuilder.MapPicture.cs; Racer photographs its scene); the
    //           readout of map x, z under the cursor and at the player; the other racers in a race; the current route with its
    //           start / finish and numbered checkpoints in travel order; A / Space shows the shortcuts and pipes (off by default);
    //           "© OpenStreetMap contributors" on the map; zoom is by a factor (1 to 16)
    //   left    track browsing, the action sheet (Round 48 brought the rest: exploration fog and its save, destinations and travel,
    //           waypoints, and the acorn areas as the goose egg areas: WorldMap.Explore.cs, Explore/)
    // Bug capture (F4 / ] and its debug-menu row) moved to the right stick's click on the controller (DeveloperHud).
    public sealed partial class WorldMap : MonoBehaviour
    {
        public static WorldMap Instance { get; private set; }
        public static bool IsOpen => Instance && Instance.Opened;
        // open, or closed this frame or the last (the button that closed it must not open a menu or capture a bug as well)
        public static bool OwnsInput => Instance && (Instance.Opened || Time.frameCount - Instance.closedFrame <= 1);
        public bool Opened => panel && panel.activeSelf;
        public bool ShowLayers { get; private set; }
        public float Zoom => zoom;
        public Vector2 Center => center;
        public Rect World => world;
        // Round 23 (Part B): the pipe network proposal's lines, their names and its labeled points, drawn on the map while the
        // proposal markers exist (Debug/ProposalMarkers.cs fills them; TEMPORARY, as Round 20's)
        public sealed class Proposal { public string label; public Vector3[] line; public Color color; public float width = 3f; public bool dashed; }
        public sealed class ProposalMark { public string label; public Vector3 at; public Color color; }
        public static readonly List<Proposal> Proposals = new List<Proposal>();
        public static readonly List<ProposalMark> ProposalMarks = new List<ProposalMark>();
        public static string ProposalLegend = "";
        // Round 36: places drawn on the map in Free Roam (the stunt park, Part B's activities: Racer's ExplorationMap activity markers), with an
        // optional outline
        public sealed class Place { public string label; public Vector3 at; public Color color; public Vector3[] outline; public bool small; }
        public static readonly List<Place> Places = new List<Place>();

        InputActionMap mapActions; InputAction mapToggle, panAction, zoomInAction, zoomOutAction, centerAction, layersAction, backAction;
        GameObject panel; RawImage picture; WorldMapOverlay overlay; Text status, heading, cursor, north, legend; Font font;
        readonly List<Text> racerDots = new List<Text>(), gateLabels = new List<Text>(), proposalLabels = new List<Text>();
        Rect world; float zoom = 1f, savedTimeScale = 1f; Vector2 center = new Vector2(.5f, .5f); int closedFrame = -10;
        ArcadeVehicle car; RaceController race;
        const float MaxZoom = 16f;

        public static void Boot(GameObject host) { if (!Instance) Instance = host.AddComponent<WorldMap>(); }

        void Start()
        {
            MenuInput.Ensure();   // the release barrier needs its owner (the game menus make it too; a check run has none)
            car = FindAnyObjectByType<ArcadeVehicle>();
            var e = GroundQuery.Extent(); world = e.width > 0 ? e : new Rect(0, 0, 3983, 1918);
            BuildMapControls();
        }
        void OnDestroy() { mapActions?.Dispose(); if (panel) Destroy(panel); if (Instance == this) Instance = null; }

        void BuildMapControls()
        {
            mapActions = new InputActionMap("World map");
            InputAction ButtonAction(string name, string keyboard, string controller) { var a = mapActions.AddAction(name, InputActionType.Button); a.AddBinding(keyboard); a.AddBinding(controller); return a; }
            mapToggle = ButtonAction("Open / close", "<Keyboard>/m", "<Gamepad>/select");
            backAction = ButtonAction("Back", "<Keyboard>/escape", "<Gamepad>/buttonEast");
            centerAction = ButtonAction("Center on player", "<Keyboard>/home", "<Gamepad>/rightStickPress");
            layersAction = ButtonAction("Shortcuts and pipes", "<Keyboard>/space", "<Gamepad>/buttonSouth");
            zoomOutAction = ButtonAction("Zoom out", "<Keyboard>/minus", "<Gamepad>/leftTrigger"); zoomInAction = ButtonAction("Zoom in", "<Keyboard>/equals", "<Gamepad>/rightTrigger");
            panAction = mapActions.AddAction("Pan", InputActionType.Value); panAction.AddBinding("<Gamepad>/leftStick");
            panAction.AddCompositeBinding("2DVector").With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s").With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
            panAction.AddCompositeBinding("2DVector").With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow").With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");
            BuildExploreControls();   // Round 48 (WorldMap.Explore.cs)
            mapActions.Enable();
        }

        bool CanOpen()
        {
            var hud = DeveloperHud.Instance;
            if (GameMenus.Open || (hud && (hud.MenuOpen || hud.CommentOpen || hud.Capturing))) return false;
            return car && picture != null || Resources.Load<Texture2D>("WorldMapPicture") != null;
        }

        void Update()
        {
            if (mapActions == null || MenuInput.Blocked) return;
            if (mapToggle.WasPressedThisFrame()) { MenuInput.ConsumeThroughRelease(mapToggle); if (Opened) Close(); else if (CanOpen()) Open(); return; }
            if (!Opened) return;
            if (UpdateExplore()) { ClampView(); Draw(); return; }   // Round 48: places, travel, the waypoint, the egg areas
            if (backAction.WasPressedThisFrame()) { MenuInput.ConsumeThroughRelease(backAction); Close(); return; }
            float dt = Time.unscaledDeltaTime;
            Vector2 pan = panAction.ReadValue<Vector2>(); if (pan.sqrMagnitude > .04f) center += new Vector2(pan.x, pan.y * world.width / world.height) * dt * .4f / zoom;
            zoom = Mathf.Clamp(zoom * Mathf.Exp((zoomInAction.ReadValue<float>() - zoomOutAction.ReadValue<float>()) * dt * 1.6f), 1f, MaxZoom);
            var m = Mouse.current; if (m != null && Mathf.Abs(m.scroll.ReadValue().y) > 0.01f) zoom = Mathf.Clamp(zoom * (m.scroll.ReadValue().y > 0 ? 1.15f : 1f / 1.15f), 1f, MaxZoom);
            if (m != null && m.leftButton.isPressed) { var d = m.delta.ReadValue(); center -= new Vector2(d.x / picture.rectTransform.rect.width, d.y / picture.rectTransform.rect.height) * (720f / Screen.height) / zoom; }
            if (centerAction.WasPressedThisFrame()) CenterOnPlayer();
            if (layersAction.WasPressedThisFrame()) ShowLayers = !ShowLayers;
            ClampView();
            Draw();
        }

        // for the checks (and the debug menu): the same as the buttons
        public void Open()
        {
            MenuInput.ConsumeThroughRelease();
            race = RaceController.Instance; if (!car) car = FindAnyObjectByType<ArcadeVehicle>();
            savedTimeScale = Time.timeScale > 0f ? Time.timeScale : 1f; Time.timeScale = 0f; AudioListener.pause = true;
            if (!panel) BuildUI();
            zoom = Mathf.Max(zoom, 2.5f); CenterOnPlayer();
            panel.SetActive(true); EventSystem.current?.SetSelectedGameObject(null); Draw();
        }
        public void Close()
        {
            MenuInput.ConsumeThroughRelease(); closedFrame = Time.frameCount; CancelTravel();
            if (panel) panel.SetActive(false);
            Time.timeScale = savedTimeScale; AudioListener.pause = false;
        }
        public void CenterOnPlayer() { if (car) center = Normalized(car.Body ? car.Body.position : car.transform.position); ClampView(); }
        public void ZoomBy(float factor) { zoom = Mathf.Clamp(zoom * factor, 1f, MaxZoom); ClampView(); }
        public void PanBy(Vector2 uv) { center += uv; ClampView(); }
        // Round 24 A2 (A4, Dan: "the map not be able to move further than the border of the map"): the view never shows past the
        // world's edges: zoomed out it is the whole map (zoom 1), and the center keeps half the view's width and height from each edge
        void ClampView()
        {
            zoom = Mathf.Clamp(zoom, 1f, MaxZoom);
            float h = 0.5f / zoom;
            center = new Vector2(Mathf.Clamp(center.x, h, 1f - h), Mathf.Clamp(center.y, h, 1f - h));
        }
        public void SetLayers(bool on) => ShowLayers = on;
        public Vector2 Normalized(Vector3 p) => new Vector2((p.x - world.xMin) / world.width, (p.z - world.yMin) / world.height);
        public Vector3 WorldPoint(Vector2 uv) => new Vector3(world.xMin + uv.x * world.width, 0f, world.yMin + uv.y * world.height);

        // ---------------------------------------------------------------- the screen (Racer's BuildUI, trimmed)

        RectTransform RectUI(string name, Transform parent, Vector2 at, Vector2 size)
        {
            var r = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); r.SetParent(parent, false);
            r.anchorMin = r.anchorMax = new Vector2(.5f, .5f); r.anchoredPosition = at; r.sizeDelta = size; return r;
        }
        Text Label(string name, Transform parent, Vector2 at, Vector2 size, int fontSize, TextAnchor align = TextAnchor.MiddleCenter)
        {
            var t = RectUI(name, parent, at, size).gameObject.AddComponent<Text>();
            t.font = font; t.fontSize = fontSize; t.color = Color.white; t.alignment = align; t.raycastTarget = false; t.horizontalOverflow = HorizontalWrapMode.Overflow; t.verticalOverflow = VerticalWrapMode.Overflow;
            var edge = t.gameObject.AddComponent<Outline>(); edge.effectColor = new Color(0, 0, 0, .85f); edge.effectDistance = new Vector2(1, -1);
            return t;
        }

        void BuildUI()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            panel = new GameObject("World map", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = panel.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 100;
            var scaler = panel.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1280, 720); scaler.matchWidthOrHeight = 0.5f;
            var bg = RectUI("Map background", panel.transform, Vector2.zero, new Vector2(4000, 4000)); bg.gameObject.AddComponent<Image>().color = new Color(.025f, .045f, .055f, 1);
            Label("Title", panel.transform, new Vector2(0, 334), new Vector2(1200, 36), 24).text = "CHASTAIN RUSH  -  MAP";
            float w = 1220f, h = w * world.height / world.width; if (h > 590f) { h = 590f; w = h * world.width / world.height; }
            var r = RectUI("Picture", panel.transform, new Vector2(0, 12), new Vector2(w, h));
            picture = r.gameObject.AddComponent<RawImage>(); picture.texture = Resources.Load<Texture2D>("WorldMapPicture"); picture.raycastTarget = false;
            r.gameObject.AddComponent<RectMask2D>();
            var ov = RectUI("Lines", r, Vector2.zero, new Vector2(w, h)); overlay = ov.gameObject.AddComponent<WorldMapOverlay>(); overlay.raycastTarget = false;
            BuildExploreUI(r, w, h);   // Round 48: the fog under the lines, the egg areas' list, the travel question
            heading = Label("Player heading", r, Vector2.zero, new Vector2(35, 35), 27); heading.color = Color.cyan; heading.text = "▲";
            cursor = Label("Cursor", r, Vector2.zero, new Vector2(25, 25), 22); cursor.text = "+";
            north = Label("North", r, new Vector2(w / 2 - 26, h / 2 - 34), new Vector2(40, 52), 18); north.text = "▲\nN"; north.fontStyle = FontStyle.Bold; north.lineSpacing = .8f;
            var credit = Label("Credit", r, new Vector2(w / 2 - 6, -h / 2 + 10), new Vector2(300, 18), 13, TextAnchor.MiddleRight); credit.rectTransform.pivot = new Vector2(1, .5f); credit.text = "© OpenStreetMap contributors";
            legend = Label("Legend", r, new Vector2(-w / 2 + 8, h / 2 - 10), new Vector2(560, 120), 14, TextAnchor.UpperLeft); legend.rectTransform.pivot = new Vector2(0, 1); legend.supportRichText = true;
            status = Label("Map status", panel.transform, new Vector2(0, -298), new Vector2(1240, 24), 17);
            Label("Controls", panel.transform, new Vector2(0, -330), new Vector2(1240, 22), 15).text =
                "Stick / WASD: pan  ·  RT / LT, = / -: zoom  ·  RS click / Home: center  ·  A / Space: shortcuts, pipes  ·  LB / RB, Q / E: places  ·  X / T: travel  ·  Y / F: waypoint  ·  D-pad ↑ ↓: egg areas  ·  B / Esc: close";
        }

        Vector2 ScreenPoint(Vector3 p) { var uv = Normalized(p); return Vector2.Scale((uv - center) * zoom, picture.rectTransform.rect.size); }
        bool Marker(Text t, Vector3 p)
        {
            var q = ScreenPoint(p); t.rectTransform.anchoredPosition = q;
            bool on = Mathf.Abs(q.x) < picture.rectTransform.rect.width / 2 - 8 && Mathf.Abs(q.y) < picture.rectTransform.rect.height / 2 - 8;
            t.gameObject.SetActive(on); return on;
        }
        Text Pooled(List<Text> pool, int i, string name, int size)
        {
            while (pool.Count <= i) pool.Add(Label(name + " " + pool.Count, picture.rectTransform, Vector2.zero, new Vector2(160, 24), size));
            return pool[i];
        }

        void Draw()
        {
            if (!panel) return;
            picture.uvRect = new Rect(center - Vector2.one * .5f / zoom, Vector2.one / zoom);
            var lines = new List<WorldMapOverlay.Path>(); var bars = new List<WorldMapOverlay.Gate>();
            var route = race ? race.Route : null;
            // the current route: its lap (cyan, arrows in travel order), start / finish (white bar), checkpoints (blue bars, numbered)
            if (route != null)
            {
                lines.Add(new WorldMapOverlay.Path { points = route.Path, tint = new Color(.3f, 1f, .88f), closed = true, width = 3f });
                bars.Add(new WorldMapOverlay.Gate { at = route.Gate.Position, right = route.Gate.Right, start = true });
                foreach (var g in route.Checkpoints) bars.Add(new WorldMapOverlay.Gate { at = g.Position, right = g.Right, tint = new Color(.3f, .6f, 1f) });
            }
            int gi = 0;
            if (route != null)
            {
                var s = Pooled(gateLabels, gi++, "Gate", 15); s.text = "START / FINISH"; s.color = Color.white; Marker(s, route.Gate.Position + route.Gate.Right * 0f); s.rectTransform.anchoredPosition += new Vector2(0, 16);
                for (int i = 0; i < route.Checkpoints.Count; i++) { var t = Pooled(gateLabels, gi++, "Gate", 16); t.text = (i + 1).ToString(); t.color = new Color(.55f, .78f, 1f); t.fontStyle = FontStyle.Bold; if (Marker(t, route.Checkpoints[i].Position)) t.rectTransform.anchoredPosition += new Vector2(0, 14); }
            }
            for (int i = gi; i < gateLabels.Count; i++) gateLabels[i].gameObject.SetActive(false);
            // the layer: every route's official shortcuts (gold) and the drive-through pipes (gray, dashed: underground)
            if (ShowLayers)
            {
                if (race != null && race.embeddedRouteJsons != null)
                    foreach (var json in race.embeddedRouteJsons)
                    {
                        if (string.IsNullOrEmpty(json)) continue;
                        var rj = JsonUtility.FromJson<RouteJson>(json);
                        if (rj.official_shortcuts == null) continue;
                        foreach (var sc in rj.official_shortcuts)
                        {
                            var pts = new Vector3[sc.path.Length / 2]; for (int k = 0; k < pts.Length; k++) pts[k] = new Vector3(sc.path[2 * k], 0f, sc.path[2 * k + 1]);
                            lines.Add(new WorldMapOverlay.Path { points = pts, tint = new Color(1f, .72f, .2f), width = 2.5f });
                        }
                    }
            }
            // Round 23 follow-ups (Dan's vision: "the map showing the pipes when you are underground"): the pipes always with the layer,
            // and by themselves whenever you are in one; the network's mains in their stripe's color, its junctions, manholes and exits named
            bool under = car && DrainPipe.InsideAny(car.transform.position);
            if (ShowLayers || under)
            {
                foreach (var p in FindObjectsByType<DrainPipe>(FindObjectsSortMode.None))
                    if (p.floor != null && p.floor.Length > 1) lines.Add(new WorldMapOverlay.Path { points = p.floor, tint = p.mapColor.a > 0f ? p.mapColor : new Color(.82f, .82f, .86f), dashed = true, width = p.mapColor.a > 0f ? 5f : 4f });
                // Round 25 (B2): the pipe express's stretches, solid cyan over the pipes
                if (PipeExpress.Instance && PipeExpress.Instance.stretches != null)
                    foreach (var st in PipeExpress.Instance.stretches) if (st.floor != null && st.floor.Length > 1) lines.Add(new WorldMapOverlay.Path { points = st.floor, tint = PipeExpress.MapColor, width = 8f });
            }
            // Round 23: the pipe network proposal (its lines, the line names at their middles, the labeled points)
            int pi = 0;
            foreach (var pr in Proposals)
            {
                if (pr.line == null || pr.line.Length < 2) continue;
                lines.Add(new WorldMapOverlay.Path { points = pr.line, tint = pr.color, width = pr.width, dashed = pr.dashed });
                if (string.IsNullOrEmpty(pr.label)) continue;
                var t = Pooled(proposalLabels, pi++, "Proposal", 14); t.text = pr.label; t.color = pr.color; t.fontStyle = FontStyle.Bold;
                if (Marker(t, pr.line[pr.line.Length / 2])) t.rectTransform.anchoredPosition += new Vector2(0, 12);
            }
            if (zoom >= 2f)   // the points' names only zoomed in (too many for the whole map)
            foreach (var mk in ProposalMarks)
            {
                var t = Pooled(proposalLabels, pi++, "Proposal", 11); t.text = "◆ " + mk.label; t.color = mk.color; t.fontStyle = FontStyle.Normal;
                t.alignment = TextAnchor.MiddleLeft; t.rectTransform.pivot = new Vector2(0.04f, .5f); Marker(t, mk.at);
            }
            var net = NetworkCells.Instance;
            if ((ShowLayers || under) && net != null && net.labels != null && zoom >= 1.5f)
                for (int i = 0; i < net.labels.Length; i++)
                {
                    var t = Pooled(proposalLabels, pi++, "Proposal", 12); t.text = "● " + net.labels[i]; t.color = new Color(.95f, .88f, .6f); t.fontStyle = FontStyle.Bold;
                    t.alignment = TextAnchor.MiddleLeft; t.rectTransform.pivot = new Vector2(0.04f, .5f); Marker(t, net.labelAt[i]);
                }
            // Round 36: the places (the stunt park, the activities), in Free Roam; the small ones (activity sites) zoomed in a little
            if (race == null || race.State == RaceController.Stage.Free)
                foreach (var pl in Places)
                {
                    if (pl.outline != null && pl.outline.Length > 2) lines.Add(new WorldMapOverlay.Path { points = pl.outline, tint = pl.color, closed = true, width = 2.5f });
                    if (pl.small && zoom < 1.4f) continue;
                    var t = Pooled(proposalLabels, pi++, "Proposal", pl.small ? 12 : 15); t.text = (pl.small ? "◆ " : "★ ") + pl.label; t.color = pl.color; t.fontStyle = FontStyle.Bold;
                    t.alignment = TextAnchor.MiddleLeft; t.rectTransform.pivot = new Vector2(0.04f, .5f); Marker(t, pl.at);
                }
            for (int i = pi; i < proposalLabels.Count; i++) proposalLabels[i].gameObject.SetActive(false);
            DrawExplore(lines, race == null || race.State == RaceController.Stage.Free);   // Round 48 (WorldMap.Explore.cs)
            overlay.SetView(world, center, zoom, lines, bars);
            // the other racers (a race only), then the player on top
            int ri = 0;
            if (race != null && race.State != RaceController.Stage.Free)
                foreach (var e in race.Entrants.Where(x => !x.IsPlayer && x.Car))
                {
                    var t = Pooled(racerDots, ri++, "Racer", 15); t.text = "●  " + e.Name; t.alignment = TextAnchor.MiddleLeft; t.rectTransform.pivot = new Vector2(0.04f, .5f);
                    t.color = ri == 1 ? new Color(1f, .45f, .2f) : ri == 2 ? new Color(1f, .82f, .2f) : new Color(.4f, .6f, 1f);
                    Marker(t, e.Car.Body.position);
                }
            for (int i = ri; i < racerDots.Count; i++) racerDots[i].gameObject.SetActive(false);
            var pp = car && car.Body ? car.Body.position : Vector3.zero;
            Marker(heading, pp); heading.rectTransform.localRotation = Quaternion.Euler(0, 0, car ? -car.transform.eulerAngles.y : 0f); heading.transform.SetAsLastSibling();
            cursor.rectTransform.anchoredPosition = Vector2.zero; cursor.gameObject.SetActive(true);
            var at = WorldPoint(center);
            legend.text = (route != null ? $"<color=#4DFFE0>━</color> {route.Name}{(route.Reverse ? " (reverse)" : "")}   <color=#FFFFFF>▬</color> start / finish   <color=#8CC7FF>1 2 3</color> checkpoints in order\n" : "")
                        + (ShowLayers ? "<color=#FFB833>━</color> shortcuts   <color=#D1D1DB>┅</color> pipes (underground)" : under ? "<color=#D1D1DB>┅</color> you are underground: the pipes (the mains in their colors)   A / Space: shortcuts" : "A / Space: show the shortcuts and pipes")
                        + (Proposals.Count > 0 ? "\n" + ProposalLegend : "") + "\n" + ExploreStatus();
            status.text = $"Cursor  x {at.x:0}  z {at.z:0}          You  x {pp.x:0}  z {pp.z:0}  heading {(car ? car.transform.eulerAngles.y : 0f):0}°          Zoom {zoom:0.0}x          {(race != null && race.State != RaceController.Stage.Free ? "Race paused" : "Paused")}";
        }
    }
}
