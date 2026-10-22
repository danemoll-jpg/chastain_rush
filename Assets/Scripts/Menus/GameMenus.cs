using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Racer;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

namespace ChastainRush
{
    // Round 16: the menus and the garage, from Racer's RaceMenus (RaceMenus.cs, .Shell, .Navigation, .Core), trimmed to
    // what this game has. Kept from Racer as it was: uGUI built in code with the legacy font, the dark card, the row style
    // and colours, one page at a time with a page stack, rows that hold a value shown as  ‹ value ›  and changed with left /
    // right (D-pad, stick, A / D or the arrows; held to repeat), A never steps a value, B goes back, the menu moving the
    // focus itself one row per press (RaceMenus.Navigation), the bright focus frame, the button prompts along the bottom
    // drawn by MenuGlyph, the release barrier of MenuInput, and the garage (GameMenus.Garage.cs). Racer's RaceFlow is
    // replaced by the small flow below; its campaign, shop, unlocks, police, split-screen, playlists, courses, radio,
    // records boards and on-screen keyboard are left behind (Round 27 brought the radio and the settings tabs, Round 29 the rider page,
    // Round 34 the records boards, the RECORDS page and the on-screen keyboard for the player's name: GameMenus.Records.cs).
    //   Start / Esc / Enter   open the menu while driving; on the menu's first page they close it again (resume)
    //   B / Esc               back one page (on the first page of the pause menu: resume)
    // Pages: the title menu when the game starts (RACE, FREE ROAM, GARAGE, SETTINGS, QUIT GAME), the pause menu (free driving: RESUME,
    // RACE, GARAGE, DEBUG, SETTINGS, QUIT GAME; in a race: RESUME, RESTART RACE, RACE SETUP, LEAVE RACE, DEBUG, SETTINGS, QUIT GAME), race setup (laps, direction,
    // opponents, difficulty, the opponents' vehicles, START RACE), the garage and (Round 27) Racer's settings pages (GameMenus.Settings.cs). While a menu is up the game is paused.
    // Never in a check run (CheckRun.Active): checks drive straight away as before.
    public sealed partial class GameMenus : MonoBehaviour
    {
        public static GameMenus Instance { get; private set; }
        public static bool Open => Instance && Instance.open;

        // ---------------------------------------------------------------- settings (race setup), saved beside the records
        [Serializable] public sealed class Settings { public int laps = 3; public string direction = "forward"; public bool opponents = true; public int difficulty = 1; public string route = ""; public int timeOfDay = 0, weather = 0; }   // Round 35 (Part C): the race's time of day (0 Day, 1 Dusk, 2 Night, 3 Dawn) and weather (0 Clear, 1 Rain, 2 Snow), Racer's race setup   // Round 18: route (a file name; "" = the Chastain Glen loop)
        public static Settings Current = new Settings();
        static string SettingsPath()
        {
#if UNITY_EDITOR
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", "local", "race_settings.json")); // git-ignored
#else
            return Path.Combine(Application.persistentDataPath, "race_settings.json");
#endif
        }
        static void LoadSettings(RaceController race)
        {
            Current = new Settings();
            if (race && race.RouteData != null) { Current.laps = Mathf.Max(1, race.RouteData.laps); if (!string.IsNullOrEmpty(race.RouteData.direction)) Current.direction = race.RouteData.direction; }
            try { if (!CheckRun.Active && File.Exists(SettingsPath())) JsonUtility.FromJsonOverwrite(File.ReadAllText(SettingsPath()), Current); }
            catch (Exception e) { Debug.LogWarning("[Menus] race_settings.json unreadable: " + e.Message); }
            Current.laps = Mathf.Clamp(Current.laps, 1, 9); Current.difficulty = Mathf.Clamp(Current.difficulty, 0, 2);
            if (Current.direction != "reverse") Current.direction = "forward";
        }
        static void SaveSettings()
        {
            if (CheckRun.Active) return;   // a check never writes Dan's settings
            try { Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath())); File.WriteAllText(SettingsPath(), JsonUtility.ToJson(Current, true)); }
            catch (Exception e) { Debug.LogWarning("[Menus] could not save race_settings.json: " + e.Message); }
        }
        // what the race uses at its start (RaceController.StartRace reads these)
        static void ApplySettings()
        {
            RaceController.LapsOverride = Current.laps;
            RaceController.RouteOverride = Current.route;   // Round 18
            RaceController.DirectionOverride = Current.direction;
            RaceController.AiCount = Current.opponents ? 3 : 0;
            RoadDriver.Difficulty = Current.difficulty;
        }

        // ---------------------------------------------------------------- boot

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (CheckRun.Active) return;
            Create();
        }
        // also used by the menu check (Checks/Round16Runner.cs), the one check run that has the menus
        public static GameMenus Create()
        {
            if (Instance || FindAnyObjectByType<ArcadeVehicle>() == null) return Instance;
            return Instance = new GameObject("GameMenus").AddComponent<GameMenus>();
        }
        public string PageName => open ? (modal != null ? "modal:" + modal : PageKey) : "closed";
        public string Heading => heading ? heading.text : "";
        public string FocusedLabel { get { var b = FocusedRow >= 0 ? buttons[FocusedRow] : null; return b ? b.transform.Find("Label").GetComponent<UnityEngine.UI.Text>().text : ""; } }

        ArcadeVehicle car; RaceController race; VehicleSwitch vehicles;
        bool open, title, started;
        float savedTimeScale = 1f;
        Font font;
        GameObject shade; RectTransform card;
        UnityEngine.UI.Text heading, details;
        readonly List<UnityEngine.UI.Button> buttons = new();
        InputAction submit, back, menuKey, adjustAction;
        InputActionAsset menuActions, ownedUiActions; InputActionReference submitReference;
        InputSystemUIInputModule uiModule;
        UnityEngine.UI.ScrollRect scroll; RectTransform content;

        System.Collections.IEnumerator Start()
        {
            MenuInput.Ensure();
            car = FindAnyObjectByType<ArcadeVehicle>(); race = RaceController.Instance;
            yield return null;   // after VehicleSwitch.Start has put on the saved vehicle
            vehicles = car.GetComponent<VehicleSwitch>();
            LoadSettings(race); ApplySettings();
            if (race) race.Reload();   // Round 18: the saved route
            Build();
            StuntEvent.Finished += ShowStuntResults;   // Round 36 (A1)
            PoliceSession.Finished += ShowPoliceResults;   // Round 39 (Part D)
            OpenMenu(true);
            AskNameOnce();   // Round 34 (C2): Racer's welcome
            StartupTitle.Begin();   // Round 51 (Part A): Dan's cover, the spoken title and the theme over the title menu
            LoadingScreen.Started();   // Round 51 (Part B): the launch's work is done; the loading screen fades after the first frames
        }

        // ---------------------------------------------------------------- open / close (the game pauses under a menu)

        void OpenMenu(bool asTitle)
        {
            if (!open) { savedTimeScale = Time.timeScale > 0f ? Time.timeScale : 1f; Time.timeScale = 0f; AudioListener.pause = true; }
            open = true; title = asTitle; page = "root"; pages.Clear(); modal = null;
            Cursor.visible = true;
            MenuInput.ConsumeThroughRelease();
            Show();
        }
        void CloseMenu()
        {
            if (!open) return;
            CapturePage();
            open = false; modal = null; LeaveGarageView();
            if (StuntEvent.Current && StuntEvent.Current.State == StuntEvent.Phase.Done) StuntEvent.End();   // Round 36: off the results, back in Free Roam
            if (PoliceSession.Running && PoliceSession.Done && page != "police-results") PoliceSession.Stop();   // Round 39: off the results, back in Free Roam
            Time.timeScale = savedTimeScale; AudioListener.pause = false;
            MenuInput.ConsumeThroughRelease(submit);
            Show();
        }

        void Update()
        {
            if (started) UpdateSplitDevices();   // Round 37 (GameMenus.Split.cs): the split setup answers player 1's device (the name keyboard player 2's)
            if (!started || MenuInput.Blocked || WorldMap.OwnsInput || StartupTitle.Blocking || LoadingScreen.Holding) return;   // Round 20: the map has the buttons (B / Esc close it)   // Round 51: not under the title or the loading screen
            var hud = DeveloperHud.Instance;
            if (hud && (hud.CommentOpen || hud.Capturing || hud.MenuOpen)) return;   // Round 17: the bug comment box has the keys
            if (UpdateComplete()) return;   // Round 54 (C): COMPLETE RACE while the menus are closed
            if (open && UpdateKeyboard()) return;   // Round 34: the on-screen keyboard's own keys (Delete, Space, Start / Enter saves)
            if (menuKey.WasPressedThisFrame())
            {
                if (!open) { if (hud && hud.Visible) OpenDebug(); else OpenMenu(false); return; }   // Round 17: Start with the debug panel up opens the debug menu
                if (modal != null) { modal = null; Show(); MenuInput.ConsumeThroughRelease(); return; }
                if (pages.Count == 0 && !title) { CloseMenu(); return; }
                if (pages.Count > 0) { BackPage(); return; }
            }
            if (!open) return;
            UpdateSplitJoin();   // Round 37: A or Start on another controller joins it as player 2
            if (back.WasPressedThisFrame()) { BackPage(); return; }
            UpdateSettingsTabs();   // Round 27 (C1): Q / E, LB / RB
            UpdateNavigation();
            UpdateAdjust();
        }
        void LateUpdate()
        {
            if (!started) return;
            UpdateShell(); UpdateGaragePreview(); UpdateBindingRows();
            if (open && modal == null && EventSystem.current && !EventSystem.current.currentSelectedGameObject)
            { var first = buttons.FirstOrDefault(b => b.gameObject.activeInHierarchy && b.interactable); if (first) EventSystem.current.SetSelectedGameObject(first.gameObject); }
        }
        void OnDestroy()
        {
            if (ownedUiActions) { ownedUiActions.Disable(); Destroy(ownedUiActions); }
            if (menuActions) { menuActions.Disable(); Destroy(menuActions); }
            back?.Dispose(); menuKey?.Dispose(); completeAction?.Dispose(); adjustAction?.Dispose(); rotateAction?.Dispose(); DisposeSettingsInput(); deleteAction?.Dispose(); spaceAction?.Dispose();
            if (textKeyboard != null) textKeyboard.onTextInput -= TypedCharacter;
            StuntEvent.Finished -= ShowStuntResults;
            PoliceSession.Finished -= ShowPoliceResults;
            if (submitReference) Destroy(submitReference);
            if (previewRoot) Destroy(previewRoot); if (previewCamera) Destroy(previewCamera.gameObject); if (previewTexture) { previewTexture.Release(); Destroy(previewTexture); }
        }

        // ---------------------------------------------------------------- building the card (Racer's RaceMenus.Initialize / BuildShell)

        void Build()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var canvasGo = new GameObject("Menu canvas", typeof(RectTransform), typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 50;
            var scaler = canvasGo.GetComponent<UnityEngine.UI.CanvasScaler>(); scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = .5f;
            var es = EventSystem.current;
            if (!es) { es = new GameObject("Menu EventSystem", typeof(EventSystem)).GetComponent<EventSystem>(); es.transform.SetParent(transform, false); }
            uiModule = es.GetComponent<InputSystemUIInputModule>(); if (!uiModule) uiModule = es.gameObject.AddComponent<InputSystemUIInputModule>();
            uiModule.AssignDefaultActions(); uiModule.deselectOnBackgroundClick = false;
            ownedUiActions = Instantiate(uiModule.actionsAsset); uiModule.actionsAsset = ownedUiActions;   // Racer: own a clone
            menuActions = ScriptableObject.CreateInstance<InputActionAsset>();
            var map = new InputActionMap("Game menu"); menuActions.AddActionMap(map);
            submit = map.AddAction("Menu confirm", InputActionType.Button); submit.AddBinding("<Keyboard>/space"); submit.AddBinding("<Gamepad>/buttonSouth");
            submitReference = InputActionReference.Create(submit); uiModule.submit = submitReference; uiModule.cancel = null; submit.Enable();
            back = new InputAction("Back", InputActionType.Button, "<Keyboard>/escape"); back.AddBinding("<Gamepad>/buttonEast"); back.AddBinding("<Keyboard>/backspace"); back.Enable();   // (on the keyboard page Backspace deletes: UpdateKeyboard runs first)
            menuKey = new InputAction("Menu", InputActionType.Button, "<Gamepad>/start"); menuKey.AddBinding("<Keyboard>/enter"); menuKey.AddBinding("<Keyboard>/escape"); menuKey.Enable();
            adjustAction = new InputAction("Adjust", InputActionType.Value);
            adjustAction.AddCompositeBinding("1DAxis").With("Negative", "<Keyboard>/leftArrow").With("Positive", "<Keyboard>/rightArrow"); adjustAction.AddBinding("<Gamepad>/dpad/x");
            adjustAction.AddCompositeBinding("1DAxis").With("Negative", "<Keyboard>/a").With("Positive", "<Keyboard>/d"); adjustAction.AddBinding("<Gamepad>/leftStick/x"); adjustAction.Enable();

            BuildCompleteButton(canvas.transform);   // Round 54 (C): under the menu's shade, over the race
            shade = Rect("Menu shade", canvas.transform).gameObject; Stretch(shade.GetComponent<RectTransform>(), 0, 0, 0, 0);
            shade.AddComponent<UnityEngine.UI.Image>().color = new Color(.015f, .025f, .04f, .78f);
            card = Rect("Menu card", shade.transform); card.anchorMin = card.anchorMax = card.pivot = new Vector2(.5f, .5f); card.sizeDelta = new Vector2(1020, 656);
            card.gameObject.AddComponent<UnityEngine.UI.Image>().color = new Color(.035f, .065f, .085f, .98f);
            heading = Label("Title", card, 32, 0); heading.color = new Color(.3f, .95f, .81f); heading.alignment = TextAnchor.MiddleLeft;
            heading.rectTransform.anchorMin = new(0, 1); heading.rectTransform.anchorMax = new(1, 1); heading.rectTransform.pivot = new(.5f, 1); heading.rectTransform.sizeDelta = new(-64, 50); heading.rectTransform.anchoredPosition = new(0, -20);
            var root = Rect("Scrollable menu", card); root.anchorMin = new(0, 0); root.anchorMax = new(1, 1); root.offsetMin = new(32, 78); root.offsetMax = new(-32, -82);
            scroll = root.gameObject.AddComponent<UnityEngine.UI.ScrollRect>(); scroll.horizontal = false; scroll.scrollSensitivity = 32; scroll.movementType = UnityEngine.UI.ScrollRect.MovementType.Clamped;
            var viewport = Rect("Viewport", root); Stretch(viewport, 0, 0, 0, 0); viewport.gameObject.AddComponent<UnityEngine.UI.Image>().color = new(1, 1, 1, .01f); viewport.gameObject.AddComponent<UnityEngine.UI.Mask>().showMaskGraphic = false;
            content = Rect("Content", viewport); content.anchorMin = new(0, 1); content.anchorMax = new(1, 1); content.pivot = new(.5f, 1); content.sizeDelta = Vector2.zero;
            var layout = content.gameObject.AddComponent<UnityEngine.UI.VerticalLayoutGroup>(); layout.spacing = 8; layout.childControlWidth = layout.childControlHeight = true; layout.childForceExpandHeight = false;
            content.gameObject.AddComponent<UnityEngine.UI.ContentSizeFitter>().verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport = viewport; scroll.content = content;
            details = Label("Details", content, 20, 0); details.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();
            for (int i = 0; i < 64; i++)   // Round 27: 32 (the settings' Controls page has 26 rows); Round 34: 64 (the on-screen keyboard has 57)
            {
                var rect = Rect("Action " + i, content);
                rect.gameObject.AddComponent<UnityEngine.UI.LayoutElement>().preferredHeight = 44;
                var img = rect.gameObject.AddComponent<UnityEngine.UI.Image>(); img.color = Color.white;
                var button = rect.gameObject.AddComponent<UnityEngine.UI.Button>(); button.targetGraphic = img;
                var colors = button.colors; colors.normalColor = new Color(.10f, .20f, .25f); colors.highlightedColor = new Color(.13f, .27f, .32f);
                colors.selectedColor = new Color(.17f, .43f, .46f); colors.pressedColor = new Color(.2f, .6f, .55f); colors.fadeDuration = .06f; button.colors = colors;
                rect.gameObject.AddComponent<MenuHoverSelect>();
                var text = Label("Label", rect, 21, 0); Stretch(text.rectTransform, 10, 0, -10, 0); text.alignment = TextAnchor.MiddleLeft;
                // Racer's ‹ › arrows of a value row (shown only on value rows; the mouse can click them)
                for (int side = -1; side <= 1; side += 2)
                {
                    int d = side; var r = Rect(d < 0 ? "Previous value" : "Next value", rect);
                    r.anchorMin = new(d < 0 ? 0 : 1, 0); r.anchorMax = new(d < 0 ? 0 : 1, 1); r.pivot = new(d < 0 ? 0 : 1, .5f); r.anchoredPosition = Vector2.zero; r.sizeDelta = new(52, 0);
                    var aimg = r.gameObject.AddComponent<UnityEngine.UI.Image>(); aimg.color = new(1, 1, 1, .001f);
                    var arrow = r.gameObject.AddComponent<UnityEngine.UI.Button>(); arrow.targetGraphic = aimg; arrow.navigation = new UnityEngine.UI.Navigation { mode = UnityEngine.UI.Navigation.Mode.None };
                    int index = i; arrow.onClick.AddListener(() => { if (MenuInput.Blocked) return; if (adjustments.TryGetValue(index, out var change)) change(d); });
                    var glyph = Label("Arrow", r, 30, 0); Stretch(glyph.rectTransform, 0, 0, 0, 0); glyph.alignment = TextAnchor.MiddleCenter; glyph.text = d < 0 ? "‹" : "›"; glyph.color = new(.55f, 1, .9f);
                }
                buttons.Add(button);
            }
            rowColors = buttons[0].colors;   // Round 34: a table row's colors are put back after it
            BuildFooter();
            BuildGarage();
            BuildSettingsInput();   // Round 27 (C1)
            started = true;
        }

        RectTransform Rect(string name, Transform parent) { var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); rect.SetParent(parent, false); return rect; }
        static void Stretch(RectTransform rect, float left, float bottom, float right, float top) { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = new Vector2(left, bottom); rect.offsetMax = new Vector2(right, top); }
        UnityEngine.UI.Text Label(string name, Transform parent, int size, float height)
        {
            var rect = Rect(name, parent); var text = rect.gameObject.AddComponent<UnityEngine.UI.Text>();
            text.font = font; text.fontSize = size; text.color = Color.white; text.raycastTarget = false; text.supportRichText = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap; text.verticalOverflow = VerticalWrapMode.Truncate;
            if (height > 0) rect.gameObject.AddComponent<UnityEngine.UI.LayoutElement>().preferredHeight = height;
            return text;
        }

        // ---------------------------------------------------------------- the button prompts (Racer's footer)

        readonly List<(MenuGlyph glyph, UnityEngine.UI.Text key, UnityEngine.UI.Text label, GameObject group)> prompts = new();
        void BuildFooter()
        {
            var footer = Rect("Context actions", card); footer.anchorMin = new(0, 0); footer.anchorMax = new(1, 0); footer.pivot = new(.5f, 0); footer.sizeDelta = new(-64, 48); footer.anchoredPosition = new(0, 16);
            var row = footer.gameObject.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>(); row.spacing = 16; row.childControlWidth = true; row.childControlHeight = false; row.childForceExpandWidth = false;
            foreach (var label in new[] { "Change", "Select", "Back", "Navigate", "Rotate" })
            {
                var group = Rect(label, footer); group.sizeDelta = new(210, 38); group.gameObject.AddComponent<UnityEngine.UI.LayoutElement>().preferredWidth = 210;
                var icon = Rect("Binding graphic", group); icon.anchorMin = icon.anchorMax = new(0, .5f); icon.pivot = new(0, .5f); icon.sizeDelta = new(54, 34);
                var glyph = icon.gameObject.AddComponent<MenuGlyph>(); glyph.raycastTarget = false;
                var key = Label("Binding", icon, 18, 0); Stretch(key.rectTransform, 0, 0, 0, 0); key.alignment = TextAnchor.MiddleCenter;
                var text = Label("Action", group, 20, 0); Stretch(text.rectTransform, 66, 0, 0, 0); text.alignment = TextAnchor.MiddleLeft; text.text = label;
                prompts.Add((glyph, key, text, group.gameObject));
            }
        }
        void UpdateShell()
        {
            if (uiModule) uiModule.enabled = open && !MenuInput.UiBlocked && !StartupTitle.Blocking && !LoadingScreen.Holding;   // Round 51: not under the title or the loading screen
            ModuleMove(!open);
            UpdateFocusFrame();
            if (!open) return;
            bool pad = MenuInput.Controller;
            bool valueRow = adjustments.ContainsKey(FocusedRow);
            void Set(int i, bool on, string path, string label) { var p = prompts[i]; p.group.SetActive(on); if (!on) return; p.glyph.SetPath(path); p.key.text = MenuGlyph.Label(path); p.label.text = label; }
            Set(0, valueRow && modal == null, pad ? "<Gamepad>/dpad" : "<Keyboard>/leftRight", "Change");
            Set(1, !valueRow || modal != null, pad ? "<Gamepad>/buttonSouth" : "<Keyboard>/space", "Select");
            Set(2, !(title && pages.Count == 0 && modal == null), pad ? "<Gamepad>/buttonEast" : "<Keyboard>/escape", pages.Count == 0 && modal == null ? "Resume" : "Back");
            if (KeyboardPage && modal == null) { Set(3, true, pad ? "<Gamepad>/buttonWest" : "<Keyboard>/backspace", "Delete"); Set(4, pad, "<Gamepad>/buttonNorth", "Space"); }   // Round 34: Racer's keyboard prompts
            else
            {
                Set(3, true, pad ? "<Gamepad>/dpad" : "<Keyboard>/arrows", "Navigate");
                Set(4, garageView, pad ? "<Gamepad>/rightStick" : "<Keyboard>/q", pad ? "Rotate" : "/ E  Rotate");
            }
            // keep the focused row in view (Racer's UpdateShell)
            var current = EventSystem.current ? EventSystem.current.currentSelectedGameObject : null;
            if (current && current != lastFocus && current.transform.IsChildOf(content))
            {
                Canvas.ForceUpdateCanvases(); var r = current.GetComponent<RectTransform>();
                var bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(scroll.viewport, r);
                var view = scroll.viewport.rect; float offset = bounds.min.y < view.yMin ? view.yMin - bounds.min.y : bounds.max.y > view.yMax ? view.yMax - bounds.max.y : 0;
                scroll.content.anchoredPosition += new Vector2(0, offset); lastFocus = current;
            }
        }
        GameObject lastFocus;

        // ---------------------------------------------------------------- pages (Racer's RaceMenus.Shell page stack)

        string page = "root";
        readonly Stack<string> pages = new();
        readonly Dictionary<string, string> pageMemory = new();
        readonly Dictionary<int, Action<int>> adjustments = new();
        readonly MenuActionRegistry registry = new();
        string modal, modalMessage, modalLabel; Action modalConfirm;

        void Navigate(string next) { CapturePage(); pages.Push(page); page = next; MenuInput.ConsumeThroughRelease(); Show(); }
        void BackPage()
        {
            if (modal != null) { modal = null; MenuInput.ConsumeThroughRelease(); Show(); return; }
            if (KeyboardPage) { CloseKeyboard(false); return; }   // Round 34
            if (pages.Count > 0) { CapturePage(); page = pages.Pop(); MenuInput.ConsumeThroughRelease(); Show(); return; }
            if (!title) CloseMenu();
        }
        void Confirm(string head, string message, string label, Action commit) { CapturePage(); modal = head; modalMessage = message; modalLabel = label; modalConfirm = commit; MenuInput.ConsumeThroughRelease(); Show(); }
        string PageKey => (title ? "title/" : race && race.State != RaceController.Stage.Free ? "race/" : "free/") + page;
        string renderedKey = "";   // the page on screen (Racer's renderedKey): its focus is remembered under its own key
        void CapturePage() { var cur = EventSystem.current ? EventSystem.current.currentSelectedGameObject : null; if (renderedKey != "" && cur && cur.transform.IsChildOf(content)) pageMemory[renderedKey] = cur.name; }

        void Clear(string head, string summary)
        {
            ResetLaterLayout();   // Round 34
            foreach (var b in buttons) { b.gameObject.SetActive(false); b.interactable = true; }
            adjustments.Clear(); registry.Clear();
            heading.text = head; details.text = summary ?? "";
            details.gameObject.SetActive(!string.IsNullOrEmpty(summary));
            details.GetComponent<UnityEngine.UI.LayoutElement>().preferredHeight = string.IsNullOrEmpty(summary) ? 0 : Mathf.Min(200, 28 * (summary.Count(c => c == '\n') + 1) + 6);
        }
        void Row(int index, string id, string label, Action callback)
        {
            var button = buttons[index]; button.gameObject.SetActive(true); button.name = id;
            var text = button.transform.Find("Label").GetComponent<UnityEngine.UI.Text>(); text.text = label;
            text.rectTransform.offsetMin = new(14, 0); text.rectTransform.offsetMax = new(-10, 0); text.alignment = TextAnchor.MiddleLeft;
            foreach (Transform t in button.transform) if (t.name.EndsWith("value")) t.gameObject.SetActive(false);
            var entry = registry.Register(id, label, submit, callback, () => button.interactable);
            button.onClick.RemoveAllListeners(); button.onClick.AddListener(() => entry.Execute());
        }
        // Racer's Step: a value row,  ‹ value ›, changed with left / right; A does nothing on it
        void Step(int index, string id, string label, Action<int> change)
        {
            Row(index, id, label, () => { });
            adjustments[index] = change;
            var button = buttons[index]; var text = button.transform.Find("Label").GetComponent<UnityEngine.UI.Text>();
            text.rectTransform.offsetMin = new(58, 0); text.rectTransform.offsetMax = new(-58, 0); text.alignment = TextAnchor.MiddleCenter;
            foreach (Transform t in button.transform) if (t.name.EndsWith("value")) t.gameObject.SetActive(true);
        }
        int FocusedRow => EventSystem.current ? buttons.FindIndex(b => EventSystem.current.currentSelectedGameObject == b.gameObject) : -1;
        float adjustNext; int adjustHeld;
        void UpdateAdjust()
        {
            if (modal != null) return;
            float v = adjustAction.ReadValue<float>(); int dir = v > .5f ? 1 : v < -.5f ? -1 : 0;
            if (dir == 0) { adjustHeld = 0; return; }
            bool first = dir != adjustHeld; if (!first && Time.unscaledTime < adjustNext) return;
            adjustHeld = dir; adjustNext = Time.unscaledTime + (first ? .42f : .11f);
            if (adjustments.TryGetValue(FocusedRow, out var change)) { change(dir); if (first) MenuInput.ConsumeThroughRelease(); }
        }

        public void Show()
        {
            if (!started) return;
            CapturePage();
            shade.SetActive(open);
            LeaveGarageView();
            ApplyRiderToVehicle();   // Round 29
            if (EventSystem.current) EventSystem.current.SetSelectedGameObject(null);
            renderedKey = "";
            if (!open) return;
            ResetSplitRowColors();   // Round 37: the split page's player rows are colored
            if (modal != null) { Clear(modal, modalMessage); Row(0, "confirm", modalLabel, () => { var c = modalConfirm; modal = null; c(); }); Row(1, "cancel", "Cancel", () => { modal = null; Show(); }); }
            else Render();
            // up / down follow the rows as drawn (Racer 0.91 Part A)
            var active = buttons.Where(b => b.gameObject.activeSelf && b.interactable).OrderBy(DrawnOrder).ToList();   // Round 34: rows in groups by their group's place
            for (int i = 0; i < active.Count; i++) active[i].navigation = new UnityEngine.UI.Navigation { mode = UnityEngine.UI.Navigation.Mode.Explicit, selectOnUp = active[(i + active.Count - 1) % active.Count], selectOnDown = active[(i + 1) % active.Count] };
            ConfigureKeyboardFocus();   // Round 34
            Canvas.ForceUpdateCanvases();
            GameObject focus = null;
            if (modal == null && pageMemory.TryGetValue(PageKey, out var name)) focus = active.FirstOrDefault(b => b.name == name)?.gameObject;
            if (!focus && active.Count > 0) focus = active[0].gameObject;
            if (focus) EventSystem.current.SetSelectedGameObject(focus);
            scroll.verticalNormalizedPosition = 1; lastFocus = null;
            renderedKey = modal != null ? "" : PageKey;
        }

        void Render()
        {
            var stage = race ? race.State : RaceController.Stage.Free;
            ClearBindingRows();   // Round 27: the Controls page's glyphs
            if (page == "music" || page == "library" || SettingsPage) { RenderSettings(); return; }   // Round 27 (C1)
            switch (page)
            {
                case "race": RenderRaceSetup(); return;
                case "opponents": RenderOpponents(); return;
                case "garage": RenderGarage(); return;
                case "rider": RenderRider(); return;   // Round 29
                case "debug": RenderDebug(); return;
                case "records": RenderRecords(); return;   // Round 34 (C3)
                case "stunt": RenderStuntSetup(); return;   // Round 36 (A1)
                case "stunt-results": RenderStuntResults(); return;
                case "activities": RenderActivities(); return;   // Round 36 (Part B)
                case "keyboard": RenderKeyboard(); return;   // Round 34 (C2)
                case "split": RenderSplit(); return;   // Round 37 (GameMenus.Split.cs)
                case "police": RenderPoliceSetup(); return;   // Round 39 (GameMenus.Police.cs)
                case "police-results": RenderPoliceResults(); return;
                case "trailer": RenderTrailer(); return;   // Round 43 (Part D, GameMenus.Trailer.cs)
                case "trailer-help": RenderTrailerHelp(); return;
                case "eggs": RenderEggs(); return;   // Round 48 (GameMenus.Eggs.cs)
            }
            if (!title && stage == RaceController.Stage.Free && PoliceSession.Running) { RenderPolicePause(); return; }   // Round 39: before the split pause (the mode owns its split)
            if (!title && SplitScreen.Active && !(stage == RaceController.Stage.Free && StuntEvent.Current && StuntEvent.Current.State == StuntEvent.Phase.Done)) { RenderSplitPause(); return; }   // Round 37
            if (title)
            {
                Clear("CHASTAIN RUSH", "Chastain Glen, Marietta, Georgia.   " + (race ? race.Route.Name + ", " + Current.laps + " laps" : ""));
                Row(0, "race", "RACE", () => Navigate("race"));
                Row(1, "roam", "FREE ROAM", () => { title = false; LoadingScreen.Cover("FREE ROAM", "Chastain Glen, Marietta, Georgia", null); CloseMenu(); LoadingScreen.Started(); });   // Round 51 (B): behind the cover
                RoamWeatherRow(2);   // Round 35 (Part C)
                Row(3, "stunt", "STUNT EVENT", OpenStuntSetup);   // Round 36 (A1): Racer's main menu > FREE ROAM > STUNT EVENT
                Row(4, "police", "POLICE", OpenPoliceSetup);   // Round 39 (Part D)
                Row(5, "split", "SPLIT SCREEN", OpenSplitSetup);   // Round 37: Racer's main menu > SPLIT SCREEN
                Row(6, "garage", "GARAGE", () => OpenGarage("all"));
                Row(7, "records", "RECORDS", OpenRecords);   // Round 34 (C3)
                Row(8, "settings", "SETTINGS", () => Navigate("settings-audio"));   // Round 27 (C1): the Screen row is on its Display page
                Row(9, "quit", "QUIT GAME", () => Confirm("QUIT CHASTAIN RUSH?", "", "QUIT", Quit));
                return;
            }
            if (stage == RaceController.Stage.Free && StuntEvent.Current) { RenderStuntPause(); return; }   // Round 36 (A1)
            if (stage == RaceController.Stage.Free)
            {
                Clear("PAUSED", "Free roam" + (StuntPark.Current && car && StuntPark.Inside(car.Body.position, 30) ? "  ·  " + StuntPark.Title : "") + EggLine);   // Round 48: the egg count
                int fr = 0;
                Row(fr++, "resume", "RESUME", CloseMenu);
                RoamWeatherRow(fr++);   // Round 35 (Part C)
                Row(fr++, "race", "RACE", () => Navigate("race"));
                Row(fr++, "stunt", "STUNT EVENT", OpenStuntSetup);   // Round 36 (A1): Racer's Free Roam menu > STUNT EVENT
                Row(fr++, "police", "POLICE", OpenPoliceSetup);   // Round 39 (Part D): Getaway, Speed Patrol, Cop vs Runner
                Row(fr++, "split", "SPLIT SCREEN", OpenSplitSetup);   // Round 37
                fr = ActivityRows(fr);   // Round 36 (Part B)
                if (EggHunt.Instance) Row(fr++, "eggs", "GOOSE EGGS", () => Navigate("eggs"));   // Round 48 (GameMenus.Eggs.cs)
                Row(fr++, "garage", "GARAGE", () => OpenGarage("all"));
                Row(fr++, "records", "RECORDS", OpenRecords);   // Round 34 (C3)
                Row(fr++, "trailer", "TRAILER MODE", () => Navigate("trailer"));   // Round 43 (Part D): Racer's pause menu > Trailer / Photo Mode
                Row(fr++, "debug", "DEBUG", () => Navigate("debug"));   // Round 17
                Row(fr++, "settings", "SETTINGS", () => Navigate("settings-audio"));   // Round 27 (C1)
                Row(fr++, "quit", "QUIT GAME", () => Confirm("QUIT CHASTAIN RUSH?", "", "QUIT", Quit));
                return;
            }
            bool finished = stage == RaceController.Stage.Finished;
            Clear(finished ? "RACE OVER" : "PAUSED", race.Route.Name + ", " + race.Route.Laps + " laps, " + race.Route.Direction + (RaceController.AiCount > 0 ? ", 3 AI drivers" : ", solo"));
            int n = 0;
            if (!finished) Row(n++, "resume", "RESUME", CloseMenu);
            if (race.CanSimulateRemaining) Row(n++, "complete", "COMPLETE RACE", () => { race.CompleteRace(); CloseMenu(); });   // Round 54 (C)
            Row(n++, "restart", finished ? "RACE AGAIN" : "RESTART RACE", finished ? StartRace : () => Confirm("RESTART THE RACE?", "The race starts again from the grid.", "RESTART", StartRace));
            Row(n++, "setup", "RACE SETUP", () => Navigate("race"));
            Row(n++, "leave", finished ? "FREE ROAM" : "LEAVE RACE", () => { LoadingScreen.Cover("FREE ROAM", "Chastain Glen, Marietta, Georgia", null); race.EndRace(); CloseMenu(); LoadingScreen.Started(); });   // Round 51 (B): behind the cover
            Row(n++, "records", "RECORDS", OpenRecords);   // Round 34 (C3)
            Row(n++, "trailer", "TRAILER MODE", () => Navigate("trailer"));   // Round 43 (Part D)
            Row(n++, "debug", "DEBUG", () => Navigate("debug"));   // Round 17
            Row(n++, "settings", "SETTINGS", () => Navigate("settings-audio"));   // Round 27 (C1)
            Row(n++, "quit", "QUIT GAME", () => Confirm("QUIT CHASTAIN RUSH?", "", "QUIT", Quit));
        }

        // ---------------------------------------------------------------- Round 17: the debug menu, controller first
        // Reached from the pause menu's DEBUG row, from Start while the debug panel is up, or from F6 / \ (DeveloperHud).
        // The same rows as every page: A selects, left / right changes the panel row, B goes back (or resumes).
        public void OpenDebug()
        {
            if (!started) return;
            var hud = DeveloperHud.Instance; if (!hud || hud.CommentOpen || hud.Capturing) return;
            if (open) { if (page != "debug" && modal == null) Navigate("debug"); return; }
            OpenMenu(false); page = "debug"; Show();
        }
        void RenderDebug()
        {
            var hud = DeveloperHud.Instance;
            if (!hud) { Clear("DEBUG", "No debug tools in this scene."); Row(0, "game", "Game menu", GameMenuFromDebug); return; }
            Clear("DEBUG", hud.SessionLine + (string.IsNullOrEmpty(hud.Status) ? "" : "\n" + hud.Status));
            Step(0, "panel", "Debug panel:   " + (hud.Visible ? "Shown" : "Hidden"), d => { hud.Visible = !hud.Visible; Show(); });
            Row(1, "capture", "Capture bug", () => { CloseMenu(); hud.CaptureBug(); });
            Row(2, "export", "Export bug report ZIP", () => { hud.ExportReport(); Show(); });
            Row(3, "folder", "Open debug report folder", () => { hud.OpenReportFolder(); Show(); });
            Row(4, "session", "Start new debug session", () => Confirm("START A NEW DEBUG SESSION?", "The current session is closed; its reports are kept.", "START NEW SESSION", () => { hud.StartNewSession(); Show(); }));
            // Round 23 (Part B), TEMPORARY until Dan has chosen: the pipe network proposal's markers (also drawn on the map) and a way
            // to each stop (free roam only), as Round 18 to 21's
            int n = 5;
            var pm = ProposalMarkers.Instance;
            if (pm)
            {
                Step(n++, "proposals", "Proposal markers:   " + ProposalMarkers.ModeNames[pm.Mode], d => { pm.Step(d); Show(); });
                bool free = !race || race.State == RaceController.Stage.Free;
                goTarget = Mathf.Clamp(goTarget, 0, Mathf.Max(0, pm.startNames.Length - 1));
                int row = n++;
                StepAct(row, "goto", free ? "Go to:   " + (pm.startNames.Length > 0 ? pm.startNames[goTarget] : "-") : "Go to: in free roam only",
                    d => { if (pm.startNames.Length > 0) goTarget = (goTarget + d + pm.startNames.Length) % pm.startNames.Length; Show(); },
                    () => { if (pm.GoTo(goTarget)) CloseMenu(); });
                buttons[row].interactable = free;
            }
            // Round 25 (B1, B2): a way to every place in the pipe network (free roam only), and the pipe express's mode
            var pw = PipeWarp.Instance;
            if (pw && pw.Count > 0)
            {
                bool free = !race || race.State == RaceController.Stage.Free;
                warpTarget = Mathf.Clamp(warpTarget, 0, pw.Count - 1);
                int row = n++;
                StepAct(row, "pipewarp", free ? "Pipe warp:   " + pw.names[warpTarget] : "Pipe warp: in free roam only",
                    d => { warpTarget = (warpTarget + d + pw.Count) % pw.Count; Show(); },
                    () => { if (pw.Go(warpTarget)) CloseMenu(); });
                buttons[row].interactable = free;
            }
            if (PipeExpress.Instance)
                Step(n++, "pipeexpress", "Pipe express:   " + PipeExpress.ModeNames[(int)PipeExpress.Setting], d => { PipeExpress.Setting = (PipeExpress.Mode)(((int)PipeExpress.Setting + d + 3) % 3); Show(); });
            Row(n++, "game", "Game menu", GameMenuFromDebug);
            Row(n++, "back", pages.Count > 0 ? "Back" : "Resume", BackPage);
        }
        int goTarget, warpTarget;
        // Round 18: a value row that also acts on A (left / right picks the value, A uses it; A never cycles it)
        void StepAct(int index, string id, string label, Action<int> change, Action act)
        {
            Step(index, id, label, change);
            var button = buttons[index]; var entry = registry.Register(id, label, submit, act, () => button.interactable);
            button.onClick.RemoveAllListeners(); button.onClick.AddListener(() => entry.Execute());
        }
        // the regular menu is never out of reach: back to it if we came from it, else replace this page with it
        void GameMenuFromDebug() { if (pages.Count > 0) BackPage(); else { CapturePage(); page = "root"; MenuInput.ConsumeThroughRelease(); Show(); } }

        static readonly string[] DifficultyNames = { "Easy", "Normal", "Hard" };
        // Round 35 (Part C): Racer's Free Roam "Weather" row (its EXPLORE page; here on the title and the free-roam pause menu):
        // left / right, Clear / Rain / Snow, at once (the world's weather, settings.json)
        void RoamWeatherRow(int index)
        {
            var s = Racer.GameSettings.Settings;
            Step(index, "roam-weather", "Weather:   " + (Racer.Weather)Mathf.Clamp(s.roamWeather, 0, 2), d => { s.roamWeather = (Mathf.Clamp(s.roamWeather, 0, 2) + d + 3) % 3; Racer.GameSettings.SaveSettings(); Show(); });
        }
        void RenderRaceSetup()
        {
            var r = race ? race.Route : null; var rd = race ? race.RouteData : null;
            bool smallOnly = rd != null && rd.vehicles != null && rd.vehicles.Length > 0, oneWay = rd != null && !rd.Allows("reverse");
            Clear("RACE SETUP", (r != null ? $"{r.Name}, {r.LapLength:0} m a lap." : "") + (smallOnly ? "\nA woods trail: motorcycles, the ATV and the mower only." : "") + "\nShortcuts are there to be found.");
            void Changed() { SaveSettings(); ApplySettings(); Show(); }
            // Round 18 (item 5): the route, left / right through Data/routes (the Chastain Glen loop first)
            var routes = race ? race.Routes() : new List<(string file, string name)>();
            int at = race ? Mathf.Max(0, routes.FindIndex(x => x.file == (string.IsNullOrEmpty(Current.route) ? race.routeFileName : Current.route))) : 0;
            Step(0, "route", $"Route:   {(routes.Count > 0 ? routes[at].name : "-")}", d =>
            {
                if (routes.Count < 2) return;
                Current.route = routes[(at + d + routes.Count) % routes.Count].file; SaveSettings(); ApplySettings();
                race.Reload(); EnsureRouteVehicle(); Show();
            });
            Step(1, "laps", $"Laps:   {Current.laps}", d => { Current.laps = Mathf.Clamp(Current.laps + d, 1, 9); Changed(); });
            Step(2, "direction", oneWay ? "Direction:   Forward (one way only)" : $"Direction:   {(Current.direction == "reverse" ? "Reverse" : "Forward")}", d => { if (oneWay) return; Current.direction = Current.direction == "reverse" ? "forward" : "reverse"; Changed(); });
            Step(3, "mode", $"Opponents:   {(Current.opponents ? "3 AI drivers" : "Solo")}", d => { Current.opponents = !Current.opponents; Changed(); });
            Step(4, "difficulty", $"Difficulty:   {DifficultyNames[Current.difficulty]}", d => { Current.difficulty = Mathf.Clamp(Current.difficulty + d, 0, 2); Changed(); });
            // Round 35 (Part C): Racer's Time of Day and Weather rows (left / right; Racer's menu order Dawn, Day, Dusk, Night)
            var order = Racer.LookPresets.MenuOrder; int ti = Mathf.Max(0, Array.IndexOf(order, (Racer.TimeOfDay)Mathf.Clamp(Current.timeOfDay, 0, 3)));
            Step(5, "timeofday", "Time of Day:   " + order[ti], d => { Current.timeOfDay = (int)order[(ti + d + order.Length) % order.Length]; Changed(); });
            Step(6, "weather", "Weather:   " + (Racer.Weather)Mathf.Clamp(Current.weather, 0, 2), d => { Current.weather = (Mathf.Clamp(Current.weather, 0, 2) + d + 3) % 3; Changed(); });
            Row(7, "opponents", "Opponent vehicles…", () => Navigate("opponents"));
            buttons[7].interactable = Current.opponents;
            if (smallOnly)
            {
                // the trail's vehicles only (cars are not offered): left / right picks one, as the garage would
                var cfg = car.GetComponent<VehicleConfiguration>(); var ids = rd.vehicles;
                int vi = Mathf.Max(0, Array.IndexOf(ids, cfg ? cfg.profileId : ids[0]));
                Step(8, "vehicle", $"Vehicle:   {VehicleProfile.Find(ids[vi]).Name}", d => { if (vehicles) vehicles.Select(ids[(vi + d + ids.Length) % ids.Length]); Show(); });
            }
            else Row(8, "garage", "Garage…", () => OpenGarage("race"));
            // Round 43 (B1): Racer's Ghost row (RaceMenus.Core: on / off, saved); its details say what the current route has
            var ghost = race ? race.Ghost : null;
            if (ghost) { ghost.Refresh(); Toggle(9, "ghost", "Ghost", ghost.Enabled, () => { ghost.Toggle(); Show(); }); } else Row(9, "ghost", "Ghost:   -", () => { });
            if (ghost && !SplitScreen.Active) details.text += "\nGhost: " + ghost.Status + " No resets, teleports or missed checkpoints; shortcuts count.";
            if (ghost) details.GetComponent<UnityEngine.UI.LayoutElement>().preferredHeight = Mathf.Min(200, 28 * (details.text.Count(c => c == '\n') + 1) + 6);
            Row(10, "start", "START RACE", StartRace);
            Row(11, "back", "Back", BackPage);
        }

        // Round 18: a route with a vehicle list (Noonday Creek): a player in anything else is put in its first vehicle when the
        // route is chosen (cars are not offered there)
        void EnsureRouteVehicle()
        {
            var rd = race ? race.RouteData : null; var cfg = car ? car.GetComponent<VehicleConfiguration>() : null;
            if (rd == null || cfg == null || rd.AllowsVehicle(cfg.profileId) || !vehicles) return;
            vehicles.Select(rd.vehicles[0]);
        }

        // Racer's Opponents page: each slot random or a vehicle (saved in ai_drivers.json, as before the menus)
        void RenderOpponents()
        {
            var choices = RaceController.SavedAiChoices();
            var rd = race ? race.RouteData : null;   // Round 18: only the route's vehicles
            Clear("OPPONENT VEHICLES", "Random picks a new vehicle every race." + (rd != null && rd.vehicles != null && rd.vehicles.Length > 0 ? "\n" + rd.name + ": motorcycles, the ATV and the mower only." : ""));
            var list = new List<string> { "random" }; list.AddRange(VehicleProfile.All.Where(p => rd == null || rd.AllowsVehicle(p.Id)).Select(p => p.Id));
            for (int i = 0; i < choices.Length; i++)
            {
                int slot = i; string id = choices[i];
                string shown = id == "random" || !list.Contains(id) ? "Random" : VehicleProfile.Find(id).Name;
                Step(i, "slot-" + i, $"{RaceController.AiDriverName(i)}:   {shown}", d =>
                {
                    int at = Mathf.Max(0, list.IndexOf(choices[slot]));
                    choices[slot] = list[(at + d + list.Count) % list.Count];
                    RaceController.SaveAiChoices(choices); Show();
                });
            }
            Row(choices.Length, "all-random", "All random", () => { for (int i = 0; i < choices.Length; i++) choices[i] = "random"; RaceController.SaveAiChoices(choices); Show(); });
            Row(choices.Length + 1, "back", "Back", BackPage);
        }

        void StartRace()
        {
            ApplySettings();
            StuntEvent.End();   // Round 36: a race ends the Stunt event
            title = false; CloseMenu();
            if (race) StartCoroutine(RaceBehindCover());
        }
        // Round 51 (Part B): the race is set up behind the loading screen (its route on the map); its countdown waits for the screen
        System.Collections.IEnumerator RaceBehindCover()
        {
            race.Reload();
            var r = race.Route;
            LoadingScreen.Cover("RACE", r != null ? $"{r.Name}\n{r.Laps} laps, {r.Direction}\n{(RaceController.AiCount > 0 ? "3 AI drivers" : "solo")}" : "", r);
            yield return null; yield return null;   // the screen is drawn before the work
            race.StartRace();
            LoadingScreen.Started();
        }

        static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        // ---------------------------------------------------------------- navigation and focus (Racer's RaceMenus.Navigation)

        Vector2Int navHeld; float navNext;
        Vector2Int HeldDirection()
        {
            float x = 0, y = 0;
            foreach (var pad in Gamepad.all)
            {
                if (!pad.added) continue;
                var v = pad.dpad.ReadValue(); var s = pad.leftStick.ReadValue(); if (s.magnitude > .6f) v += s;
                x += v.x; y += v.y;
            }
            var k = Keyboard.current;
            if (k != null) { if (k.upArrowKey.isPressed || k.wKey.isPressed) y += 1; if (k.downArrowKey.isPressed || k.sKey.isPressed) y -= 1; }
            if (Mathf.Abs(y) >= .5f && Mathf.Abs(y) >= Mathf.Abs(x)) return new Vector2Int(0, y > 0 ? 1 : -1);
            if (KeyboardPage) { if (k != null) { if (k.leftArrowKey.isPressed) x -= 1; if (k.rightArrowKey.isPressed) x += 1; } if (Mathf.Abs(x) >= .5f) return new Vector2Int(x > 0 ? 1 : -1, 0); }   // Round 34: over the keys
            return Vector2Int.zero;   // left / right change a value (UpdateAdjust), they do not move the focus
        }
        void UpdateNavigation()
        {
            if (!EventSystem.current) { navHeld = Vector2Int.zero; return; }
            var dir = HeldDirection();
            if (dir == Vector2Int.zero) { navHeld = Vector2Int.zero; return; }
            bool first = dir != navHeld; if (!first && Time.unscaledTime < navNext) return;
            navHeld = dir; navNext = Time.unscaledTime + (first ? .45f : .11f);
            var current = EventSystem.current.currentSelectedGameObject; var from = current ? current.GetComponent<UnityEngine.UI.Selectable>() : null;
            if (!from || !from.gameObject.activeInHierarchy)
            { var top = buttons.FirstOrDefault(b => b.gameObject.activeInHierarchy && b.interactable); if (top) EventSystem.current.SetSelectedGameObject(top.gameObject); return; }
            var next = dir.x != 0 ? (dir.x > 0 ? from.FindSelectableOnRight() : from.FindSelectableOnLeft()) : dir.y > 0 ? from.FindSelectableOnUp() : from.FindSelectableOnDown();
            if (next && next.gameObject.activeInHierarchy && next.interactable) EventSystem.current.SetSelectedGameObject(next.gameObject);
        }
        InputActionReference moduleMove; bool moduleMoveOn = true;
        void ModuleMove(bool on)
        {
            if (!uiModule || on == moduleMoveOn) return;
            if (!on) { moduleMove = uiModule.move; uiModule.move = null; } else if (moduleMove) uiModule.move = moduleMove;
            moduleMoveOn = on;
        }
        RectTransform focusFrame;
        void UpdateFocusFrame()
        {
            if (!focusFrame)
            {
                focusFrame = Rect("Focus frame", card);
                void Edge(string name, Vector2 min, Vector2 max, Vector2 offMin, Vector2 offMax, Color c) { var r = Rect(name, focusFrame); r.anchorMin = min; r.anchorMax = max; r.offsetMin = offMin; r.offsetMax = offMax; var i = r.gameObject.AddComponent<UnityEngine.UI.Image>(); i.color = c; i.raycastTarget = false; }
                var white = new Color(1, 1, 1, .95f);
                Edge("Top", new(0, 1), new(1, 1), new(0, -3), Vector2.zero, white); Edge("Bottom", Vector2.zero, new(1, 0), Vector2.zero, new(0, 3), white);
                Edge("Left", Vector2.zero, new(0, 1), Vector2.zero, new(3, 0), white); Edge("Right", new(1, 0), Vector2.one, new(-3, 0), Vector2.zero, white);
                Edge("Marker", Vector2.zero, new(0, 1), new(3, 3), new(11, -3), new Color(.3f, .95f, .81f));
            }
            var current = EventSystem.current ? EventSystem.current.currentSelectedGameObject : null;
            bool show = open && current && current.activeInHierarchy && current.transform.IsChildOf(card) && current.GetComponent<UnityEngine.UI.Selectable>();
            if (!show) { if (focusFrame.parent != card) focusFrame.SetParent(card, false); focusFrame.gameObject.SetActive(false); return; }
            focusFrame.gameObject.SetActive(true);
            if (focusFrame.parent != current.transform) { focusFrame.SetParent(current.transform, false); Stretch(focusFrame, 0, 0, 0, 0); }
            focusFrame.SetAsLastSibling();
        }
    }

    // Racer 0.92 Part A: the mouse pointer highlights a row only while the mouse is in use, and then selects it.
    public sealed class MenuHoverSelect : MonoBehaviour, IPointerEnterHandler, IPointerMoveHandler
    {
        public void OnPointerEnter(PointerEventData e) => Pick();
        public void OnPointerMove(PointerEventData e) => Pick();
        void Pick()
        {
            if (MenuInput.Controller || !EventSystem.current) return;
            var s = GetComponent<UnityEngine.UI.Selectable>(); if (s && s.interactable && EventSystem.current.currentSelectedGameObject != gameObject) EventSystem.current.SetSelectedGameObject(gameObject);
        }
    }
}
