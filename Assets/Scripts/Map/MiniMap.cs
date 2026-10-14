using System;
using System.Collections.Generic;
using System.IO;
using Racer;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ChastainRush
{
    // Round 24 Part A2, item A5 (Dan: "Need to add the minimap from woodstock rush"). Ported from Racer's RacingMiniMap (Woodstock
    // Rush 0.79 to 0.106): the same panel in the top-right corner (236 x 220, a clipped 224 x 184 window, 0.72 px a meter), heading
    // up with the player's arrow below the center, the compass N on the rim with E / S / W ticks, the course in teal and the
    // official shortcuts in gold, the rivals as white diamonds, the key under it; J or B (controller) turns it off and on in free
    // roam and the choice is saved (minimap.json beside the records; on by default; a check run never writes it). Chastain Rush
    // changes: the ground under the lines is this game's map picture (WorldMapPicture, the big map's), turned with the heading;
    // a race also shows its checkpoints (blue bars, the start / finish white); underground (in a pipe or below the ground) the
    // pipes are drawn over it in their colors. In a race the panel sits under the place readout. Left behind: Racer's free-roam
    // road lines (the picture shows the roads), activity sites, the waypoint, the stunt park, acorn areas, Speed Patrol,
    // Getaway, split-screen.
    [RequireComponent(typeof(CanvasRenderer))]
    [DefaultExecutionOrder(-100)]   // Racer's: reads its toggle before the menus and the map act on the same press (B is also Back)
    public sealed class MiniMap : MaskableGraphic
    {
        public const float Scale = .72f;
        public static readonly Vector2 PlayerPoint = new Vector2(0, -48);
        static readonly Color Main = new Color(.2f, .86f, .86f), Optional = new Color(1, .69f, .17f), Gate = new Color(.3f, .6f, 1f), Pipe = new Color(.82f, .82f, .86f);
        const string RaceKey = "<color=#33DBDB>COURSE</color>   <color=#FFB02B>SHORTCUT</color>   • RIVALS";
        const string RoamKey = "J / B: minimap on / off";
        const string UnderKey = "<color=#D1D1DB>PIPES</color>   J / B: on / off";
        const float RimInset = 11;

        public static MiniMap Instance { get; private set; }
        public bool Shown { get; private set; }
        ArcadeVehicle car; RaceRoute route; RaceController race;
        RawImage picture; Text north, key; RectTransform panel; Image panelImage;
        Rect world; InputAction toggle; bool hidden, checkRun, under;
        Vector3 origin, forward, right; Vector2 northDir = Vector2.up; float nextUpdate;
        readonly List<(Vector3 a, Vector3 b, Color c, float w)> raceLines = new List<(Vector3, Vector3, Color, float)>();
        readonly List<(Vector3 a, Vector3 b, Color c)> pipeLines = new List<(Vector3, Vector3, Color)>(); bool pipesCached;

        [Serializable] class Saved { public bool hidden; }
        static string SavePath()
        {
#if UNITY_EDITOR
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", "local", "minimap.json")); // git-ignored
#else
            return Path.Combine(Application.persistentDataPath, "minimap.json");
#endif
        }

        // GameShell.Boot: one per game
        public static void Boot(GameObject host)
        {
            var tex = Resources.Load<Texture2D>("WorldMapPicture");
            var canvasGo = new GameObject("Minimap canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            canvasGo.transform.SetParent(host.transform, false);
            var canvas = canvasGo.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 10;
            var scaler = canvasGo.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1280, 720); scaler.matchWidthOrHeight = 0.5f;
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var p = new GameObject("Minimap", typeof(RectTransform), typeof(Image));
            p.transform.SetParent(canvasGo.transform, false);
            var rect = (RectTransform)p.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1, 1);
            rect.anchoredPosition = new Vector2(-18, -18); rect.sizeDelta = new Vector2(236, 220);
            var img = p.GetComponent<Image>(); img.color = new Color(.025f, .055f, .07f, .9f); img.raycastTarget = false;
            var clip = new GameObject("Clipped window", typeof(RectTransform), typeof(RectMask2D));
            clip.transform.SetParent(rect, false);
            var cr = (RectTransform)clip.transform; cr.sizeDelta = new Vector2(224, 184); cr.anchoredPosition = new Vector2(0, 10);
            RawImage pic = null;
            if (tex)
            {
                pic = new GameObject("Map picture", typeof(RectTransform)).AddComponent<RawImage>();
                pic.transform.SetParent(cr, false); pic.texture = tex; pic.raycastTarget = false; pic.color = new Color(1f, 1f, 1f, .9f);
                pic.rectTransform.anchorMin = pic.rectTransform.anchorMax = new Vector2(.5f, .5f);
            }
            var map = new GameObject("Course, racers and pipes", typeof(RectTransform)).AddComponent<MiniMap>();
            map.transform.SetParent(cr, false); map.rectTransform.sizeDelta = cr.sizeDelta; map.raycastTarget = false;
            var label = new GameObject("Key", typeof(RectTransform)).AddComponent<Text>();
            label.transform.SetParent(rect, false); label.font = font; label.fontSize = 13; label.alignment = TextAnchor.MiddleCenter;
            label.raycastTarget = false; label.supportRichText = true; label.color = Color.white;
            label.rectTransform.sizeDelta = new Vector2(232, 24); label.rectTransform.anchoredPosition = new Vector2(0, -95);
            var n = new GameObject("Compass N", typeof(RectTransform)).AddComponent<Text>();
            n.transform.SetParent(cr, false); n.font = font; n.fontSize = 16; n.fontStyle = FontStyle.Bold; n.alignment = TextAnchor.MiddleCenter;
            n.color = Color.white; n.raycastTarget = false; n.text = "N"; n.rectTransform.sizeDelta = new Vector2(22, 22);
            var edge = n.gameObject.AddComponent<Outline>(); edge.effectColor = new Color(0, 0, 0, .9f); edge.effectDistance = new Vector2(1.2f, -1.2f);
            map.picture = pic; map.north = n; map.key = label; map.panel = rect; map.panelImage = img;
        }

        protected override void Awake()
        {
            base.Awake(); if (!Application.isPlaying) return;
            Instance = this;
            var e = GroundQuery.Extent(); world = e.width > 0 ? e : new Rect(0, 0, 3983, 1918);
            toggle = new InputAction("Minimap on / off (free roam)", InputActionType.Button);
            toggle.AddBinding("<Keyboard>/j"); toggle.AddBinding("<Gamepad>/buttonEast"); toggle.Enable();
            foreach (var a in Environment.GetCommandLineArgs()) if (a.StartsWith("-exe") || a == "-batchmode" || a == "-executeMethod") checkRun = true;
            if (!checkRun) try { if (File.Exists(SavePath())) hidden = JsonUtility.FromJson<Saved>(File.ReadAllText(SavePath())).hidden; } catch (Exception) { }
        }
        protected override void OnDestroy() { toggle?.Dispose(); if (Instance == this) Instance = null; base.OnDestroy(); }

        public void SetRoamShown(bool on)
        {
            hidden = !on;
            if (!checkRun) try { File.WriteAllText(SavePath(), JsonUtility.ToJson(new Saved { hidden = hidden })); } catch (Exception) { }
        }

        void Update()
        {
            race = RaceController.Instance;
            bool roam = !race || race.State == RaceController.Stage.Free;
            if (toggle != null && roam && toggle.WasPressedThisFrame() && !GameMenus.Open && !WorldMap.IsOpen && !MenuInput.Blocked
                && !(DeveloperHud.Instance && (DeveloperHud.Instance.MenuOpen || DeveloperHud.Instance.CommentOpen)))
                SetRoamShown(hidden);
        }

        void CacheRace()
        {
            raceLines.Clear(); route = race ? race.Route : null;
            if (route == null) return;
            Add(route.Path, Main, 5f, true);
            foreach (var sc in route.Shortcuts) Add(sc.Path, Optional, 3f, false);
        }
        void Add(Vector3[] pts, Color c, float w, bool closed)
        {
            if (pts == null || pts.Length < 2) return;
            var prev = pts[0];
            for (int i = 1; i < pts.Length; i++) if ((pts[i] - prev).sqrMagnitude >= 9 || i == pts.Length - 1) { raceLines.Add((prev, pts[i], c, w)); prev = pts[i]; }
            if (closed) raceLines.Add((prev, pts[0], c, w));
        }
        void CachePipes()
        {
            pipesCached = true;
            foreach (var d in FindObjectsByType<DrainPipe>(FindObjectsSortMode.None))
            {
                if (d.floor == null || d.floor.Length < 2) continue;
                var c = d.mapColor.a > 0f ? d.mapColor : Pipe; var prev = d.floor[0];
                for (int i = 1; i < d.floor.Length; i++) if ((d.floor[i] - prev).sqrMagnitude >= 16 || i == d.floor.Length - 1) { pipeLines.Add((prev, d.floor[i], c)); prev = d.floor[i]; }
            }
            // Round 25 (B2): the pipe express's stretches in cyan, over the pipes
            if (PipeExpress.Instance && PipeExpress.Instance.stretches != null)
                foreach (var st in PipeExpress.Instance.stretches)
                {
                    if (st.floor == null || st.floor.Length < 2) continue; var prev = st.floor[0];
                    for (int i = 1; i < st.floor.Length; i++) if ((st.floor[i] - prev).sqrMagnitude >= 16 || i == st.floor.Length - 1) { pipeLines.Add((prev, st.floor[i], PipeExpress.MapColor)); prev = st.floor[i]; }
                }
        }

        void LateUpdate()
        {
            if (!car) car = FindAnyObjectByType<ArcadeVehicle>();
            race = RaceController.Instance;
            bool roam = !race || race.State == RaceController.Stage.Free;
            bool visible = car && !GameMenus.Open && !WorldMap.IsOpen && (!roam || !hidden);
            Shown = visible;
            canvasRenderer.SetAlpha(visible ? 1 : 0);
            panelImage.enabled = visible; key.enabled = visible; north.enabled = visible; if (picture) picture.enabled = visible;
            panel.anchoredPosition = new Vector2(-18, roam ? -18 : -82);   // in a race, under the place readout
            if (!visible) return;
            if (!roam && (race.Route != route || raceLines.Count == 0)) CacheRace();
            if (roam && route != null) { route = null; raceLines.Clear(); }
            if (Time.unscaledTime < nextUpdate) return;
            nextUpdate = Time.unscaledTime + .05f;
            origin = car.Body ? car.Body.position : car.transform.position;
            under = DrainPipe.InsideAny(origin) || origin.y < RaceRoute.Ground(origin.x, origin.z).y - 2.5f;
            if (under && !pipesCached) CachePipes();
            key.text = !roam ? RaceKey : under ? UnderKey : RoamKey;
            forward = Vector3.ProjectOnPlane(car.transform.forward, Vector3.up).normalized;
            if (forward.sqrMagnitude < .5f) forward = Vector3.forward;
            right = Vector3.Cross(Vector3.up, forward);
            northDir = new Vector2(right.z, forward.z).normalized; if (northDir.sqrMagnitude < .5f) northDir = Vector2.up;
            north.rectTransform.anchoredPosition = Rim(northDir);
            if (picture)   // the picture turned about the player's spot, heading up
            {
                var rt = picture.rectTransform;
                rt.sizeDelta = world.size * Scale;
                rt.pivot = new Vector2((origin.x - world.xMin) / world.width, (origin.z - world.yMin) / world.height);
                rt.anchoredPosition = PlayerPoint;
                rt.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg);
                picture.color = under ? new Color(.45f, .45f, .5f, .9f) : new Color(1f, 1f, 1f, .9f);
            }
            SetVerticesDirty();
        }

        Vector2 Rim(Vector2 d)
        {
            var r = rectTransform.rect; float hx = r.width * .5f - RimInset, hy = r.height * .5f - RimInset;
            float t = Mathf.Min(Mathf.Abs(d.x) > 1e-4f ? hx / Mathf.Abs(d.x) : float.MaxValue, Mathf.Abs(d.y) > 1e-4f ? hy / Mathf.Abs(d.y) : float.MaxValue);
            return r.center + d * t;
        }

        public Vector2 Project(Vector3 p)
        {
            var d = p - origin;
            return PlayerPoint + new Vector2(Vector3.Dot(d, right), Vector3.Dot(d, forward)) * Scale;
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear(); if (!Application.isPlaying || !car) return;
            var bounds = rectTransform.rect;
            bool In(Vector2 a, Vector2 b) => !(Mathf.Max(a.x, b.x) < bounds.xMin - 5 || Mathf.Min(a.x, b.x) > bounds.xMax + 5 || Mathf.Max(a.y, b.y) < bounds.yMin - 5 || Mathf.Min(a.y, b.y) > bounds.yMax + 5);
            if (under)
                foreach (var s in pipeLines) { var a = Project(s.a); var b = Project(s.b); if (In(a, b)) { Line(vh, a, b, 6, new Color(.02f, .04f, .05f)); Line(vh, a, b, 4, s.c); } }
            foreach (var s in raceLines) { var a = Project(s.a); var b = Project(s.b); if (In(a, b)) Line(vh, a, b, s.w, s.c); }
            if (route != null && race && race.State != RaceController.Stage.Free)
            {
                // the checkpoints across the course (blue), the start / finish (white)
                foreach (var g in route.Checkpoints) Bar(vh, g.Position, g.Right, Gate);
                Bar(vh, route.Gate.Position, route.Gate.Right, Color.white);
                foreach (var e in race.Entrants)
                {
                    if (e.IsPlayer || !e.Car) continue;
                    var p = Project(e.Car.transform.position);
                    if (!bounds.Contains(p)) continue;
                    Diamond(vh, p, 5, new Color(.03f, .06f, .08f)); Diamond(vh, p, 3.5f, Color.white);
                }
            }
            // E, S and W ticks on the rim (the N is a label), at right angles to north
            var c = bounds.center;
            foreach (var d in new[] { new Vector2(northDir.y, -northDir.x), -northDir, new Vector2(-northDir.y, northDir.x) })
            { var rim = Rim(d); var from = rim + d * 4; var to = rim - d * 5; Line(vh, from, to, 5, new Color(.02f, .04f, .05f)); Line(vh, from, to, 2.5f, Color.white); }
            // the player's arrow, fixed heading up, below the center to favor the road ahead
            Triangle(vh, PlayerPoint + new Vector2(0, 9), PlayerPoint + new Vector2(-7, -6), PlayerPoint + new Vector2(7, -6), new Color(.02f, .04f, .05f));
            Triangle(vh, PlayerPoint + new Vector2(0, 7), PlayerPoint + new Vector2(-4.5f, -4), PlayerPoint + new Vector2(4.5f, -4), new Color(1, .3f, .3f));
        }

        void Bar(VertexHelper vh, Vector3 at, Vector3 rightOfGate, Color col)
        {
            var a = Project(at - rightOfGate * 7f); var b = Project(at + rightOfGate * 7f);
            if (!rectTransform.rect.Contains((a + b) * .5f)) return;
            Line(vh, a, b, 5, new Color(.02f, .04f, .05f)); Line(vh, a, b, 3, col);
        }
        static void Line(VertexHelper vh, Vector2 a, Vector2 b, float width, Color c)
        {
            var d = b - a; if (d.sqrMagnitude < .001f) return;
            var n = new Vector2(-d.y, d.x).normalized * width * .5f;
            int i = vh.currentVertCount;
            vh.AddVert(a - n, c, Vector2.zero); vh.AddVert(a + n, c, Vector2.zero); vh.AddVert(b + n, c, Vector2.zero); vh.AddVert(b - n, c, Vector2.zero);
            vh.AddTriangle(i, i + 1, i + 2); vh.AddTriangle(i, i + 2, i + 3);
        }
        static void Triangle(VertexHelper vh, Vector2 a, Vector2 b, Vector2 c, Color color)
        { int i = vh.currentVertCount; vh.AddVert(a, color, Vector2.zero); vh.AddVert(b, color, Vector2.zero); vh.AddVert(c, color, Vector2.zero); vh.AddTriangle(i, i + 1, i + 2); }
        static void Diamond(VertexHelper vh, Vector2 p, float r, Color c)
        { Triangle(vh, p + Vector2.up * r, p + Vector2.left * r, p + Vector2.down * r, c); Triangle(vh, p + Vector2.up * r, p + Vector2.down * r, p + Vector2.right * r, c); }
    }
}
