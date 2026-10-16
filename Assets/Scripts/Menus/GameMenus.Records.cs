using System;
using System.Collections.Generic;
using System.Linq;
using Racer;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace ChastainRush
{
    // Round 34 (C2, C3): from Racer's RaceMenus.
    //   The RECORDS page (RaceMenus.RecordsResults.cs RenderRecords, the LAP and RACE tabs as one value row): left / right on the
    //   first row changes the route and direction, the second the board (best laps / best races, every race length with a Laps
    //   column), the third (when more than one vehicle has times there) the vehicle; then the Top 10 as Racer's table (rank, time,
    //   name, [laps,] vehicle, date; the player's own and this race's new entries lit); B back. Racer's speed traps, jumps,
    //   ghost, stunts and combos tabs are left behind (this game has none of them).
    //   The on-screen keyboard (RaceMenus.Entry.cs OpenKeyboard / OpenNameEntry): 50 keys and Shift, caret, Delete, Space,
    //   SAVE, Cancel; the D-pad / stick moves over the keys, A types, X deletes, Y a space, Start or Enter saves, B cancels;
    //   a keyboard types straight in. Racer's InputField draft (and its paste) is a plain line here with the caret drawn.
    //   Racer's TableRow / LaterGroup (RaceMenus.Later.cs) with them.
    public sealed partial class GameMenus
    {
        // ---------------------------------------------------------------- Racer's table rows and groups
        readonly List<GameObject> laterLayouts = new(), tableCells = new();
        UnityEngine.UI.ColorBlock rowColors;
        RectTransform LaterGroup(string name, Transform parent, bool horizontal, float height = 44)
        {
            var r = Rect(name, parent); laterLayouts.Add(r.gameObject); r.gameObject.AddComponent<UnityEngine.UI.LayoutElement>().preferredHeight = height;
            UnityEngine.UI.HorizontalOrVerticalLayoutGroup layout = horizontal ? r.gameObject.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>() : r.gameObject.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
            layout.spacing = 6; layout.childControlWidth = layout.childControlHeight = true; layout.childForceExpandHeight = false; layout.childForceExpandWidth = true; return r;
        }
        void TableRow(int index, string id, string[] cells, float[] widths, Action click, bool emphasis = false)
        {
            Row(index, id, string.Join(" / ", cells), click); var b = buttons[index]; b.transform.Find("Label").gameObject.SetActive(false);
            var colors = b.colors; colors.normalColor = emphasis ? new(.10f, .33f, .32f) : index % 2 == 0 ? new(.06f, .14f, .19f) : new(.10f, .20f, .25f); b.colors = colors;
            float x = 0; for (int i = 0; i < cells.Length; i++) { var t = Label("Cell", b.transform, 20, 0); tableCells.Add(t.gameObject); t.text = cells[i]; t.alignment = i == cells.Length - 1 ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft; t.rectTransform.anchorMin = new(x, 0); x += widths[i]; t.rectTransform.anchorMax = new(x, 1); t.rectTransform.offsetMin = new(9, 0); t.rectTransform.offsetMax = new(-9, 0); }
        }
        // the tables and groups of the page before go, the rows back under the page as plain rows (Racer's ResetLaterLayout)
        void ResetLaterLayout()
        {
            foreach (var b in buttons)
            {
                if (b.transform.parent != content) b.transform.SetParent(content, false);
                b.transform.Find("Label").gameObject.SetActive(true); b.colors = rowColors;
                var le = b.GetComponent<UnityEngine.UI.LayoutElement>(); le.preferredHeight = 44; le.flexibleWidth = -1;
            }
            foreach (var cell in tableCells) if (cell) { cell.SetActive(false); Destroy(cell); } tableCells.Clear();
            foreach (var g in laterLayouts) if (g) { g.SetActive(false); Destroy(g); } laterLayouts.Clear();
            if (keyGrid) { keyGrid.gameObject.SetActive(false); keyUtility.gameObject.SetActive(false); draftLine.transform.parent.gameObject.SetActive(false); }
            for (int i = 0; i < buttons.Count; i++) buttons[i].transform.SetSiblingIndex(i + 1);   // after the details line, in pool order
        }
        // the route and board controls as one row; the Top 10 as one compact block, so all ten rows fit (Racer's Controls / Table)
        void Controls(int from, int to) { var group = LaterGroup("Record controls", content, true, 48); group.SetSiblingIndex(0); for (int k = from; k < to; k++) { buttons[k].transform.SetParent(group, false); var le = buttons[k].GetComponent<UnityEngine.UI.LayoutElement>(); le.preferredHeight = 48; le.flexibleWidth = k == from ? 2 : 1; } }
        void Table(int from, int to, float height = 28, int font = 18)
        {
            if (to <= from) return; var table = LaterGroup("Top 10", content, false, (to - from) * (height + 2) - 2); table.SetSiblingIndex(laterLayouts.Count >= 2 ? laterLayouts[laterLayouts.Count - 2].transform.GetSiblingIndex() + 1 : 0); table.GetComponent<UnityEngine.UI.VerticalLayoutGroup>().spacing = 2;
            for (int k = from; k < to; k++) { buttons[k].transform.SetParent(table, false); buttons[k].GetComponent<UnityEngine.UI.LayoutElement>().preferredHeight = height; foreach (var cell in buttons[k].GetComponentsInChildren<UnityEngine.UI.Text>(true)) if (cell.name == "Cell") cell.fontSize = font; }
        }

        // up / down follow the rows as drawn: a row inside a group (the records' controls and table, the keyboard) by its group's place
        int DrawnOrder(UnityEngine.UI.Button b)
        {
            var t = b.transform; var top = t; while (top.parent && top.parent != content) top = top.parent;
            return top.GetSiblingIndex() * 1000 + (t.parent == content ? 0 : t.GetSiblingIndex());
        }

        // ---------------------------------------------------------------- the RECORDS page
        int recordTrack = -1, recordBoard; string recordsVehicle = "All vehicles";
        List<(string route, string direction)> RecordTracks() => race ? race.RecordTracks() : new List<(string, string)>();
        static string TrackTitle((string route, string direction) t) => t.route + " - " + (t.direction.StartsWith("reverse") ? "Reverse" : "Forward") + (t.direction.EndsWith("-rain") ? ", rain" : t.direction.EndsWith("-snow") ? ", snow" : "");   // Round 35: the weather's boards
        static bool OnTrack((string route, string direction) t, string category) => RecordBoards.LapCategory(category) == t.route + "|" + t.direction;
        static string RecordDate(string date) => DateTime.TryParse(date, null, System.Globalization.DateTimeStyles.RoundtripKind, out var d) ? d.ToLocalTime().ToString("d MMM yyyy") : "—";
        void OpenRecords() { recordTrack = -1; recordsVehicle = "All vehicles"; Navigate("records"); }
        // Racer's ChooseRecordTrack: the route being raced (or set up), else the first with times
        void ChooseRecordTrack(List<(string route, string direction)> tracks)
        {
            var boards = RaceController.Boards;
            int last = race && race.Route != null ? tracks.FindIndex(t => t.route == race.Route.Name && t.direction == race.Route.Direction) : -1;
            bool Has(int i) => boards != null && (boards.Top(c => OnTrack(tracks[i], c), false).Count > 0 || boards.Top(c => OnTrack(tracks[i], c), true).Count > 0);
            if (last >= 0 && (Has(last) || race.State != RaceController.Stage.Free)) { recordTrack = last; return; }
            for (int i = 0; i < tracks.Count; i++) if (Has(i)) { recordTrack = i; return; }
            recordTrack = Mathf.Max(0, last);
        }
        void RenderRecords()
        {
            var tracks = RecordTracks(); var boards = RaceController.Boards;
            int pagesN = tracks.Count + (StuntPark.Current ? 2 : 0) + (ArcadeActivities.Instance && ArcadeActivities.Instance.Sites.Length > 0 ? 1 : 0);   // Round 36 (A1): the Stunt event Top 10 and the Top 10 Best Combos after the routes; (B) the activities
            if (recordTrack < 0 || recordTrack >= pagesN) ChooseRecordTrack(tracks);
            Clear("RECORDS", "");
            if (recordTrack >= tracks.Count + (StuntPark.Current ? 2 : 0)) { RenderActivityRecords(pagesN); return; }
            if (recordTrack >= tracks.Count) { RenderStuntRecords(recordTrack - tracks.Count, pagesN); return; }
            if (tracks.Count == 0 || boards == null) { details.gameObject.SetActive(true); details.text = "No routes."; Row(0, "back", "Back", BackPage); return; }
            var track = tracks[recordTrack]; bool raceBoard = recordBoard == 1;
            var vehicles = boards.TopVehicles(c => OnTrack(track, c), raceBoard);
            if (recordsVehicle != "All vehicles" && !vehicles.Contains(recordsVehicle)) recordsVehicle = "All vehicles";
            var board = boards.Top(c => OnTrack(track, c), raceBoard, recordsVehicle == "All vehicles" ? null : recordsVehicle);
            int row = 0;
            Step(row++, "record-track", TrackTitle(track) + (vehicles.Length > 0 || boards.TopVehicles(c => OnTrack(track, c), !raceBoard).Length > 0 ? "" : "   (no times yet)"),
                 d => { recordTrack = (recordTrack + d + pagesN) % pagesN; recordsVehicle = "All vehicles"; Show(); });
            Step(row++, "record-board", raceBoard ? "BEST RACES" : "BEST LAPS", d => { recordBoard = 1 - recordBoard; recordsVehicle = "All vehicles"; Show(); });
            if (vehicles.Length > 1 || recordsVehicle != "All vehicles")
            {
                var choices = vehicles.Prepend("All vehicles").ToList();
                Step(row++, "record-vehicle", recordsVehicle == "All vehicles" ? "All vehicles" : VehicleProfile.Find(recordsVehicle).Name, d => { int at = Mathf.Max(0, choices.IndexOf(recordsVehicle)); recordsVehicle = choices[(at + d + choices.Count) % choices.Count]; Show(); });
            }
            Controls(0, row);
            details.gameObject.SetActive(true); details.GetComponent<UnityEngine.UI.LayoutElement>().preferredHeight = 28;
            string with = recordsVehicle == "All vehicles" ? "" : " with the " + VehicleProfile.Find(recordsVehicle).Name;
            details.text = boards.Error ?? (board.Count == 0
                ? $"No {(raceBoard ? "race" : "lap")} times on {TrackTitle(track)}{with} yet. Finish a {(raceBoard ? "race" : "lap")} there to set one."
                : (raceBoard ? "BEST RACES · full-race totals, every race length" : "BEST LAPS · every race length") + (recordsVehicle == "All vehicles" ? " · all vehicles" : " · " + VehicleProfile.Find(recordsVehicle).Name) + " · ‹ › changes");
            var widths = raceBoard ? new[] { .08f, .21f, .17f, .08f, .24f, .22f } : new[] { .09f, .23f, .19f, .27f, .22f };
            int firstRow = row;
            if (board.Count > 0) TableRow(row++, "header", raceBoard ? new[] { "Rank", "Time", "Name", "Laps", "Vehicle", "Date" } : new[] { "Rank", "Time", "Name", "Vehicle", "Date" }, widths, () => { });
            for (int i = 0; i < board.Count; i++)
            {
                var e = board[i]; int laps = RecordBoards.RaceLaps(e.category);
                string time = RaceController.Fmt((float)e.seconds) + (boards.IsNew(e.id) ? "  NEW" : "");
                string vehicle = VehicleProfile.Find(e.vehicle).Name, date = RecordDate(e.date) + (e.legacy ? " · legacy" : "");
                string who = string.IsNullOrEmpty(e.name) ? PlayerNames.Player : e.name; bool own = who == PlayerNames.Player;
                TableRow(row++, "record-" + i, raceBoard ? new[] { (i + 1).ToString(), time, who, laps > 0 ? laps.ToString() : "?", vehicle, date } : new[] { (i + 1).ToString(), time, who, vehicle, date }, widths, () => { }, own || boards.IsNew(e.id));
            }
            Table(firstRow, row);
            Row(row, "back", "Back", BackPage); buttons[row].transform.SetAsLastSibling();
            details.transform.SetSiblingIndex(1);   // the controls, the line saying what is shown, the table, Back
        }

        // Round 36 (A1): Racer's Records > STUNTS (the Stunt event Top 10, Free Roam's best single combo) and > COMBOS (the Top 10 Best Combos), as two
        // more pages of the route row (left / right reaches them after the routes)
        void RenderStuntRecords(int which, int pagesN)
        {
            Step(0, "record-track", which == 0 ? "STUNT EVENT TOP 10" : "TOP 10 BEST COMBOS", d => { recordTrack = (recordTrack + d + pagesN) % pagesN; Show(); });
            Controls(0, 1);
            details.gameObject.SetActive(true); details.GetComponent<UnityEngine.UI.LayoutElement>().preferredHeight = 56;
            details.text = which == 0
                ? "STUNT EVENT · " + StuntProgress.Minutes + " minutes at " + StuntPark.Title + "\nFree Roam's best single combo: " + (StuntProgress.BestCombo > 0 ? $"{StuntProgress.BestCombo:N0} pts ({VehicleProfile.Find(StuntProgress.BestComboVehicle).Name}, {StuntProgress.BestComboDate})" : "none yet")
                : "TOP 10 BEST COMBOS · the Stunt event and Free Roam at " + StuntPark.Title + "\nBF backflip · FF frontflip · R roll · BR barrel roll · 360 spin · LOOP · J jump";
            int first = 1, row = which == 0 ? StuntTopTable(1, 0) : ComboTable(1, null);
            Table(first, row);
            Row(row, "back", "Back", BackPage); buttons[row].transform.SetAsLastSibling();
            details.transform.SetSiblingIndex(1);
        }

        // Round 36 (B): Racer's Records > the activities: every site, your best with the vehicle you drive now and the medal (Racer keys them per vehicle)
        void RenderActivityRecords(int pagesN)
        {
            var a = ArcadeActivities.Instance;
            Step(0, "record-track", "FREE ROAM ACTIVITIES", d => { recordTrack = (recordTrack + d + pagesN) % pagesN; Show(); });
            Controls(0, 1);
            details.gameObject.SetActive(true); details.GetComponent<UnityEngine.UI.LayoutElement>().preferredHeight = 28;
            var cfg = car ? car.GetComponent<VehicleConfiguration>() : null; details.text = "Your bests with the " + (cfg ? cfg.Profile.Name : "vehicle") + " (each vehicle has its own) · jumps, speed traps and smash runs";
            var w = new[] { .40f, .16f, .22f, .22f }; int row = 1, first = 1;
            TableRow(row++, "act-header", new[] { "Activity", "Kind", "Best", "Medal" }, w, () => { });
            foreach (var s in a.Sites)
            {
                var pb = a.PersonalBest(s);
                TableRow(row++, "act-" + s.id, new[] { s.title, s.kind == ActivitySite.Kind.Jump ? "Jump" : s.kind == ActivitySite.Kind.Speed ? "Speed trap" : "Smash", pb != null ? ArcadeActivities.Measurement(s, pb.value) : "—", pb != null ? MedalUi.Name(pb.medal) : "—" }, w, () => { }, pb != null && pb.medal >= 3);
            }
            Table(first, row, 26, 17);
            Row(row, "back", "Back", BackPage); buttons[row].transform.SetAsLastSibling();
            details.transform.SetSiblingIndex(1);
        }

        // ---------------------------------------------------------------- the on-screen keyboard (Racer's RaceMenus.Entry.cs)
        RectTransform keyGrid, keyUtility; UnityEngine.UI.Text draftLine;
        string keyboardDraft = "", keyboardError = "", keyboardTitle = "TEXT ENTRY";
        int keyboardLimit, caret; bool shift;
        Keyboard textKeyboard; Func<string, bool> saveText; Action afterText;
        InputAction deleteAction, spaceAction;
        const string Keys = "1234567890qwertyuiopasdfghjkl;zxcvbnm,./\\:-_@()[]";
        bool KeyboardPage => page == "keyboard";
        public string KeyboardDraft => keyboardDraft;   // for the check

        void OpenKeyboard(string value, int limit, Func<string, bool> save, Action after = null, string head = null, string prompt = null)
        {
            keyboardDraft = value ?? ""; caret = keyboardDraft.Length; keyboardLimit = limit; saveText = save; afterText = after; keyboardTitle = head ?? "TEXT ENTRY"; keyboardError = prompt ?? ""; shift = false; Navigate("keyboard");
        }
        // Racer 0.94 Part A: the player's name, up to 12 characters
        void OpenNameEntry(string value, Func<string, bool> save, Action after = null, string head = "YOUR NAME", string prompt = null) =>
            OpenKeyboard(value, PlayerNames.MaxLength, v => { var name = PlayerNames.Clean(v); if (name.Length == 0) { keyboardError = "Enter a name (up to 12 characters)."; return false; } return save(name); }, after, head, prompt ?? "Up to 12 characters. It goes on the Top 10.");

        void EnsureKeyboard()
        {
            if (keyGrid) return;
            var rect = Rect("Text draft", content); rect.gameObject.AddComponent<UnityEngine.UI.LayoutElement>().preferredHeight = 48;
            rect.gameObject.AddComponent<UnityEngine.UI.Image>().color = new(.08f, .19f, .23f);
            draftLine = Label("Draft", rect, 24, 0); Stretch(draftLine.rectTransform, 12, 4, -12, -4); draftLine.alignment = TextAnchor.MiddleLeft;
            keyGrid = Rect("On-screen keyboard", content);
            var grid = keyGrid.gameObject.AddComponent<UnityEngine.UI.GridLayoutGroup>(); grid.cellSize = new(84, 44); grid.spacing = new(8, 8); grid.constraint = UnityEngine.UI.GridLayoutGroup.Constraint.FixedColumnCount; grid.constraintCount = 10;
            keyGrid.gameObject.AddComponent<UnityEngine.UI.LayoutElement>().preferredHeight = 260;
            keyUtility = Rect("Keyboard utilities", content); var ug = keyUtility.gameObject.AddComponent<UnityEngine.UI.GridLayoutGroup>(); ug.cellSize = new(222, 44); ug.spacing = new(8, 8); ug.constraint = UnityEngine.UI.GridLayoutGroup.Constraint.FixedColumnCount; ug.constraintCount = 4;
            keyUtility.gameObject.AddComponent<UnityEngine.UI.LayoutElement>().preferredHeight = 100;
        }
        void RenderKeyboard()
        {
            EnsureKeyboard(); Clear(keyboardTitle, keyboardError);
            draftLine.gameObject.SetActive(true); keyGrid.gameObject.SetActive(true); keyUtility.gameObject.SetActive(true);
            draftLine.transform.parent.SetSiblingIndex(1); keyGrid.SetSiblingIndex(2); keyUtility.SetSiblingIndex(3); draftLine.transform.parent.gameObject.SetActive(true);
            RefreshDraft();
            for (int i = 0; i < Keys.Length; i++) { char key = shift ? char.ToUpperInvariant(Keys[i]) : Keys[i]; Row(i, "key-" + i, key.ToString(), () => InsertText(key.ToString())); buttons[i].transform.SetParent(keyGrid, false); buttons[i].transform.Find("Label").GetComponent<UnityEngine.UI.Text>().alignment = TextAnchor.MiddleCenter; }
            int n = Keys.Length;
            Row(n, "shift", shift ? "Shift: ON" : "Shift", () => { shift = !shift; Show(); }); Row(n + 1, "caret-left", "Caret ←", () => { caret = Math.Max(0, caret - 1); RefreshDraft(); }); Row(n + 2, "caret-right", "Caret →", () => { caret = Math.Min(keyboardDraft.Length, caret + 1); RefreshDraft(); });
            Row(n + 3, "delete", "Delete", DeleteText); Row(n + 4, "space", "Space", () => InsertText(" ")); Row(n + 5, "save", "SAVE", () => CloseKeyboard(true)); Row(n + 6, "cancel", "Cancel", () => CloseKeyboard(false));
            for (int i = n; i <= n + 6; i++) buttons[i].transform.SetParent(keyUtility, false);
        }
        void RefreshDraft() { if (draftLine) draftLine.text = keyboardDraft.Insert(Mathf.Clamp(caret, 0, keyboardDraft.Length), "|"); }
        void InsertText(string value) { if (keyboardDraft.Length + value.Length > keyboardLimit) return; caret = Mathf.Clamp(caret, 0, keyboardDraft.Length); keyboardDraft = keyboardDraft.Insert(caret, value); caret += value.Length; RefreshDraft(); }
        void DeleteText() { caret = Mathf.Clamp(caret, 0, keyboardDraft.Length); if (caret == 0) return; keyboardDraft = keyboardDraft.Remove(--caret, 1); RefreshDraft(); }
        void CloseKeyboard(bool save)
        {
            if (save && !saveText(keyboardDraft)) { Show(); return; }
            CapturePage(); page = pages.Count > 0 ? pages.Pop() : "root"; MenuInput.ConsumeThroughRelease(); Show(); if (save) afterText?.Invoke();
        }
        // every frame on the keyboard page, before the menu's own keys: true when it used this frame's input
        bool UpdateKeyboard()
        {
            if (!KeyboardPage || modal != null) return false;
            if (deleteAction == null)
            {
                deleteAction = new InputAction("Delete", InputActionType.Button, "<Keyboard>/backspace"); deleteAction.AddBinding("<Gamepad>/buttonWest"); deleteAction.Enable();
                spaceAction = new InputAction("Space", InputActionType.Button, "<Gamepad>/buttonNorth"); spaceAction.Enable();
            }
            if (textKeyboard != Keyboard.current) { if (textKeyboard != null) textKeyboard.onTextInput -= TypedCharacter; textKeyboard = Keyboard.current; if (textKeyboard != null) textKeyboard.onTextInput += TypedCharacter; }
            var k = Keyboard.current;
            if (deleteAction.WasPressedThisFrame()) { DeleteText(); MenuInput.ConsumeThroughRelease(); return true; }
            if (spaceAction.WasPressedThisFrame()) { InsertText(" "); MenuInput.ConsumeThroughRelease(); return true; }
            if (k != null && k.enterKey.wasPressedThisFrame) { CloseKeyboard(true); return true; }
            var g = Gamepad.current; if (g != null && g.startButton.wasPressedThisFrame) { CloseKeyboard(true); return true; }
            if (k != null && k.vKey.wasPressedThisFrame && (k.leftCtrlKey.isPressed || k.rightCtrlKey.isPressed)) { InsertText(PlayerNames.Clean(GUIUtility.systemCopyBuffer)); return true; }
            return false;
        }
        void TypedCharacter(char value)
        {
            if (!open || !KeyboardPage || modal != null || char.IsControl(value) || Keyboard.current?.ctrlKey.isPressed == true) return;
            InsertText(value.ToString()); MenuInput.ConsumeThroughRelease();
        }
        // Racer's ConfigureCoreFocus: the keys a grid of ten across, the utilities four across
        void ConfigureKeyboardFocus()
        {
            if (!KeyboardPage || modal != null) return;
            int n = Keys.Length;
            for (int i = 0; i < n; i++)
                buttons[i].navigation = new UnityEngine.UI.Navigation { mode = UnityEngine.UI.Navigation.Mode.Explicit, selectOnLeft = buttons[Math.Max(0, i - 1)], selectOnRight = buttons[Math.Min(n - 1, i + 1)], selectOnUp = buttons[Math.Max(0, i - 10)], selectOnDown = buttons[i + 10 < n ? i + 10 : n + (i % 10) * 4 / 10] };
            for (int i = n; i <= n + 6; i++)
                buttons[i].navigation = new UnityEngine.UI.Navigation { mode = UnityEngine.UI.Navigation.Mode.Explicit, selectOnLeft = buttons[Math.Max(n, i - 1)], selectOnRight = buttons[Math.Min(n + 6, i + 1)], selectOnUp = i - n < 4 ? buttons[Math.Min(n - 1, (n - 1) / 10 * 10 + (i - n) * 10 / 4)] : buttons[i - 4], selectOnDown = buttons[Math.Min(n + 6, i + 4)] };
        }

        // ---------------------------------------------------------------- the name, once (Racer's OpenWelcome) and in Settings > Gameplay
        void AskNameOnce()
        {
            var s = GameSettings.Settings;
            if (PlayerNames.Named || s.nameAsked) return;
            s.nameAsked = true; GameSettings.SaveSettings();
            OpenNameEntry("", PlayerNames.SetPlayer, null, "WELCOME: WHAT'S YOUR NAME?", "Up to 12 characters: it goes on the Top 10. Change it any time in Settings > Gameplay.");
        }
    }
}
