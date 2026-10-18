using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

using Racer;

namespace ChastainRush
{
    // 0.79 Part H: the Trailer Mode controls on screen, so no notes are needed: the nine cameras by number and name (the
    // current one highlighted) and the other controls grouped and labelled with their keys (or controller buttons). Every
    // entry can be clicked with the mouse; on a controller, B gives the panel the focus (D-pad to choose, A to use, B to
    // leave); the keys work as before. Shown when Trailer Mode starts; H (L3) hides it together with the HUD so recordings
    // stay clean; while it is hidden, the line "H: show controls" appears briefly when a key other than the driving
    // controls is pressed. Screenshots (P / F12 / R3) render the camera alone, so nothing of this appears in them.
    // Chastain Rush, Round 43 (Part D): copied from Racer; RaceFlow's menu and map are GameMenus and WorldMap here; "G" is the
    // checkpoint arches and course arrows (no waypoint beacon).
    public sealed partial class TrailerMode
    {
        public bool PanelShown { get; private set; }
        public bool PanelFocused { get; private set; }
        public bool PanelVisible => panelRoot && panelRoot.activeSelf;
        public bool HintVisible => PanelVisible; // (0.77 name, kept for the evidence tools)
        public bool ShowControlsLineVisible => showLineBack && showLineBack.gameObject.activeSelf;
        public RectTransform PanelRect => panelRoot ? (RectTransform)panelRoot.transform : null;
        public IReadOnlyList<(Button button, Func<string> label, Action act)> PanelEntries => entries;
        public void TogglePanel() { PanelShown = !PanelShown; if (!PanelShown) FocusPanel(false); }
        // H / L3: the HUD and this panel go together (both hidden for a clean screen, or both shown)
        public void ToggleScreen() { bool show = !(HudShown || PanelShown); HudShown = show; PanelShown = show; if (!show) { FocusPanel(false); hideFrame = Time.frameCount; lineLeft = 0; } }
        int hideFrame = -1; bool menuLastFrame; float lineLeft;
        public void FocusPanel(bool on)
        {
            PanelFocused = on && PanelShown && entries.Count > 0;
            if (EventSystem.current) EventSystem.current.SetSelectedGameObject(PanelFocused ? entries[0].button.gameObject : null);
        }

        Canvas panelCanvas; GameObject panelRoot; Text panelTitle, panelFooter, showLine; Image showLineBack;
        readonly List<(Button button, Func<string> label, Action act)> entries = new();
        readonly List<(Text text, Func<string> label)> notes = new();
        readonly List<GameObject> conditionRows = new();
        readonly Button[] cameraButtons = new Button[9];
        bool cursorShown;
        static readonly Color Card = new(.025f, .055f, .07f, .88f), Normal = new(.10f, .20f, .25f, .95f), Current = new(.12f, .52f, .50f, 1), Heading = new(.62f, .86f, .86f);
        const float Width = 440, Row = 25;

        void BuildPanel()
        {
            var host = new GameObject("Trailer Mode controls", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            host.transform.SetParent(transform, false);
            panelCanvas = host.GetComponent<Canvas>(); panelCanvas.renderMode = RenderMode.ScreenSpaceOverlay; panelCanvas.sortingOrder = 450;
            var scaler = host.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new(1280, 800); scaler.matchWidthOrHeight = .5f;
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            panelRoot = new GameObject("Panel", typeof(RectTransform), typeof(Image)); var root = (RectTransform)panelRoot.transform; root.SetParent(host.transform, false);
            root.anchorMin = root.anchorMax = root.pivot = new(0, 1); root.anchoredPosition = new(18, -112); panelRoot.GetComponent<Image>().color = Card;
            float y = -8;
            Text Label(string name, Transform parent, float x, float yy, float w, float h, int size, Color c, TextAnchor anchor = TextAnchor.MiddleLeft)
            {
                var t = new GameObject(name, typeof(RectTransform), typeof(Text)).GetComponent<Text>(); t.transform.SetParent(parent, false);
                var r = t.rectTransform; r.anchorMin = r.anchorMax = r.pivot = new(0, 1); r.anchoredPosition = new(x, yy); r.sizeDelta = new(w, h);
                t.font = font; t.fontSize = size; t.color = c; t.alignment = anchor; t.raycastTarget = false; t.horizontalOverflow = HorizontalWrapMode.Overflow; t.verticalOverflow = VerticalWrapMode.Overflow; return t;
            }
            Button Entry(string name, float x, float w, Func<string> label, Action act, GameObject group = null)
            {
                var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button)); go.transform.SetParent(root, false);
                var r = (RectTransform)go.transform; r.anchorMin = r.anchorMax = r.pivot = new(0, 1); r.anchoredPosition = new(x, y); r.sizeDelta = new(w, Row - 3);
                var b = go.GetComponent<Button>(); var colours = b.colors; colours.normalColor = Normal; colours.highlightedColor = new(.16f, .34f, .40f, 1); colours.selectedColor = new(.85f, .66f, .18f, 1); colours.pressedColor = new(.2f, .6f, .6f, 1); b.colors = colours;
                var t = Label("Text", go.transform, 8, 0, w - 12, Row - 3, 14, Color.white); t.rectTransform.anchorMin = t.rectTransform.anchorMax = t.rectTransform.pivot = new(0, 1);
                b.onClick.AddListener(() => { act(); if (!PanelFocused && EventSystem.current) EventSystem.current.SetSelectedGameObject(null); });
                entries.Add((b, label, act)); notes.Add((t, label)); return b;
            }
            void Note(Func<string> label) { var t = Label("Note", root, 14, y, Width - 24, Row - 4, 13, new(.80f, .84f, .86f)); notes.Add((t, label)); y -= Row - 4; }
            void Section(string name) { y -= 4; var h = Label("Heading " + name, root, 12, y, Width - 20, 18, 12, Heading); h.text = name; h.fontStyle = FontStyle.Bold; y -= 18; }

            panelTitle = Label("Title", root, 12, y, Width - 20, 24, 16, Color.white); panelTitle.fontStyle = FontStyle.Bold; y -= 26;
            Section(MenuInput.Controller ? "CAMERAS   (D-pad ← →)" : "CAMERAS");
            for (int i = 0; i < 9; i++)
            {
                int shot = i; float colW = (Width - 24) / 3;
                cameraButtons[i] = Entry("Camera " + (i + 1), 12 + (i % 3) * colW, colW - 4, () => (MenuInput.Controller ? "" : (shot + 1) + "  ") + CameraViews.ShotNames[shot], () => Views?.SelectTrailerCamera((CameraViews.Shot)shot));
                if (i % 3 == 2) y -= Row;
            }
            Section("CAMERA");
            Entry("Camera action", 12, Width - 24, () => Keys("Tab", "A") + "Camera action: Fixed re-plant · Side other side · Auto next shot", () => Views?.CameraAction()); y -= Row;
            Note(() => Keys("Left Shift (hold)", "X (hold)") + "Auto: keep this shot   ·   " + Keys("− / =", "D-pad ↑ ↓") + "field of view");
            Section("SPEED");
            Note(() => Keys("Z (hold)", "LB (hold)") + "slow motion 0.25×");
            Entry("Half speed", 12, Width - 24, () => Keys("X", "RB") + "Slow motion 0.5×: " + (HalfSpeed ? "On" : "Off"), ToggleHalfSpeed); y -= Row;
            Section("SCREEN");
            Entry("Hide", 12, Width - 24, () => Keys("H", "L3") + "Hide the HUD and this panel (clean screen)", ToggleScreen); y -= Row;
            Entry("Guides", 12, Width - 24, () => Keys("G", "") + "Checkpoint arches and arrows: " + (GuidesShown ? "Shown" : "Hidden"), ToggleGuides); y -= Row;
            Entry("Screenshot", 12, Width - 24, () => Keys("P / F12", "R3") + "Screenshot (full resolution, no HUD, no panel)", Screenshot); y -= Row;
            float conditionsTop = y; int first = entries.Count;
            Section("TIME AND WEATHER (Free Roam)");
            Entry("Time of day", 12, Width - 24, () => Keys("T", "") + "Time of day: " + TimeLabel, () => CycleTimeOfDay()); y -= Row;
            float half = (Width - 28) / 2;
            Entry("Clock back", 12, half, () => Keys(",", "") + "Clock −1 h", () => NudgeClock(-1));
            Entry("Clock on", 16 + half, half, () => Keys(".", "") + "Clock +1 h   (" + ClockLabel + ")", () => NudgeClock(1)); y -= Row;
            Entry("Clock pause", 12, Width - 24, () => Keys("K", "") + "Clock: " + (ClockPaused ? "Paused" : "Running"), ToggleClock); y -= Row;
            Entry("Weather", 12, Width - 24, () => Keys("B", "") + "Weather: " + WeatherLabel, () => CycleWeather()); y -= Row;
            Entry("Moon", 12, Width - 24, () => Keys("O", "") + "Moon: " + MoonLabel, () => CycleMoon()); y -= Row;
            Entry("Lightning", 12, Width - 24, () => Keys("L", "") + "Lightning strike now", LightningNow); y -= Row;
            conditionsHeight = conditionsTop - y; conditionsFirst = first;
            foreach (Transform c in root) if (((RectTransform)c).anchoredPosition.y <= conditionsTop) conditionRows.Add(c.gameObject);
            y -= 4; panelFooter = Label("Footer", root, 12, y, Width - 20, 20, 12, new(.74f, .80f, .82f)); y -= 22;
            footerFull = y; root.sizeDelta = new(Width, -y + 6);
            for (int i = 0; i < entries.Count; i++) { var nav = new Navigation { mode = Navigation.Mode.Automatic }; entries[i].button.navigation = nav; }
            // the small line shown briefly while the panel is hidden
            var lineHost = new GameObject("Show controls line", typeof(RectTransform), typeof(Image)); lineHost.transform.SetParent(host.transform, false);
            var hr = (RectTransform)lineHost.transform; hr.anchorMin = hr.anchorMax = hr.pivot = new(0, 1); hr.anchoredPosition = new(18, -18); hr.sizeDelta = new(190, 26);
            showLineBack = lineHost.GetComponent<Image>(); showLineBack.color = Card; showLineBack.raycastTarget = false;
            showLine = Label("Text", lineHost.transform, 0, 0, 190, 26, 14, Color.white, TextAnchor.MiddleCenter);
            lineHost.SetActive(false); panelRoot.SetActive(false);
        }
        float conditionsHeight, footerFull; int conditionsFirst;
        static string Keys(string keyboard, string controller) { string k = MenuInput.Controller ? controller : keyboard; return string.IsNullOrEmpty(k) ? "" : k + "   "; }
        string ClockLabel { get { var look = WorldLook.Current; float h = look ? (look.Trailer?.hour ?? look.Hour) : 12; return $"{Mathf.FloorToInt(h) % 24:00}:{Mathf.FloorToInt(h * 60) % 60:00}"; } }

        void HidePanel()
        {
            PanelFocused = false; if (panelRoot) panelRoot.SetActive(false); if (showLineBack) showLineBack.gameObject.SetActive(false);
            if (cursorShown) { cursorShown = false; Cursor.visible = MenuVisible; }
        }
        void UpdatePanel()
        {
            if (!panelRoot) return;
            bool show = Active && Driving && PanelShown;
            if (panelRoot.activeSelf != show) panelRoot.SetActive(show);
            // the mouse pointer while the panel can be clicked
            if (show) { Cursor.visible = true; cursorShown = true; } else if (cursorShown) { cursorShown = false; Cursor.visible = MenuVisible; }
            if (!show && PanelFocused) FocusPanel(false);
            // "H: show controls" while hidden, after any key other than the driving controls
            bool hiddenHere = Active && Driving && !PanelShown;
            if (hiddenHere && Time.frameCount != hideFrame && AnyNonDrivingPress()) lineLeft = 2.5f;
            bool line = hiddenHere && lineLeft > 0; lineLeft = Mathf.Max(0, lineLeft - Mathf.Min(Time.unscaledDeltaTime, .1f)); // shown time, not wall time
            if (showLineBack.gameObject.activeSelf != line) showLineBack.gameObject.SetActive(line);
            if (line)
            {
                float a = Mathf.Clamp01(lineLeft / .8f);
                showLine.text = (MenuInput.Controller ? "L3" : "H") + ": show controls"; var c = showLine.color; c.a = a; showLine.color = c; var b = showLineBack.color; b.a = Card.a * a; showLineBack.color = b;
            }
            if (!show) return;
            panelTitle.text = "TRAILER / PHOTO MODE   " + (MenuInput.Controller ? "(off: Start > Trailer Mode)" : "(F8: off)");
            var views = Views; int current = views ? (int)views.TrailerCamera : -1;
            for (int i = 0; i < 9; i++) { var colours = cameraButtons[i].colors; colours.normalColor = i == current ? Current : Normal; cameraButtons[i].colors = colours; }
            foreach (var (text, label) in notes) text.text = label();
            // the Free Roam conditions only where they apply (races: Race Setup)
            bool conditions = ConditionsAvailable; foreach (var g in conditionRows) if (g.activeSelf != conditions) g.SetActive(conditions);
            var root = (RectTransform)panelRoot.transform; float height = -footerFull + 6 - (conditions ? 0 : conditionsHeight);
            panelFooter.rectTransform.anchoredPosition = new(12, footerFull + 22 + (conditions ? 0 : conditionsHeight));
            root.sizeDelta = new(Width, height);
            panelFooter.text = MenuInput.Controller ? (PanelFocused ? "D-pad: choose   A: use   B: back to driving" : "B: use this panel   ·   Start > Trailer Mode: everything") : "Click any entry   ·   F1: this panel on / off   ·   F: shake a fist";
        }
        // keys and buttons that mean "I want something" (not throttle, brake, steering or the stick)
        static readonly Key[] DrivingKeys = { Key.W, Key.A, Key.S, Key.D, Key.UpArrow, Key.DownArrow, Key.LeftArrow, Key.RightArrow };
        static bool AnyNonDrivingPress()
        {
            var k = Keyboard.current;
            if (k != null && k.anyKey.wasPressedThisFrame)
            {
                foreach (var key in k.allKeys) if (key.wasPressedThisFrame && Array.IndexOf(DrivingKeys, key.keyCode) < 0) return true;
            }
            var g = Gamepad.current;
            if (g != null && (g.buttonSouth.wasPressedThisFrame || g.buttonEast.wasPressedThisFrame || g.buttonWest.wasPressedThisFrame || g.buttonNorth.wasPressedThisFrame
                || g.leftShoulder.wasPressedThisFrame || g.rightShoulder.wasPressedThisFrame || g.dpad.left.wasPressedThisFrame || g.dpad.right.wasPressedThisFrame
                || g.dpad.up.wasPressedThisFrame || g.dpad.down.wasPressedThisFrame || g.rightStickButton.wasPressedThisFrame || g.selectButton.wasPressedThisFrame)) return true;
            return false;
        }
    }
}
