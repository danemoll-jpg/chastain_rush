using System.IO;
using System.Linq;
using Racer;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace ChastainRush
{
    // Round 57 (F): race replays in the menus. On the finish screen, WATCH REPLAY (X / V) and SAVE REPLAY (Y / K) beside the results (uGUI over the
    // IMGUI panel, as COMPLETE RACE; while they show, the vehicle's own X and Y (view, reset) wait); the race-over pause page has the same rows.
    // The REPLAYS page (title, free roam and race menus): the kept #1 lap and race of each course (watch; replaced only by a new #1) and the saved
    // replays (watch or delete), each marked when recorded on another build or before its course changed.
    public sealed partial class GameMenus
    {
        RectTransform replayRoot; MenuGlyph watchGlyph, saveGlyph; UnityEngine.UI.Text watchKey, saveKey, saveLabel, replayNote; float replayNoteUntil;
        InputAction watchAction, saveAction;
        public static bool ReplayButtonsShowing => Instance && Instance.replayRoot && Instance.replayRoot.gameObject.activeInHierarchy;
        string replayPick;

        void BuildReplayButtons(Transform canvas)
        {
            replayRoot = Rect("Replay buttons", canvas);
            replayRoot.anchorMin = replayRoot.anchorMax = new Vector2(.5f, .065f); replayRoot.sizeDelta = new Vector2(760, 54);   // under the results panel (IMGUI draws over every canvas)
            (MenuGlyph, UnityEngine.UI.Text, UnityEngine.UI.Text) Button(string label, float x, System.Action act)
            {
                var r = Rect(label, replayRoot); r.anchorMin = r.anchorMax = new Vector2(x, .5f); r.sizeDelta = new Vector2(360, 54);
                var image = r.gameObject.AddComponent<UnityEngine.UI.Image>(); image.color = new Color(.08f, .30f, .34f, .98f);
                var b = r.gameObject.AddComponent<UnityEngine.UI.Button>(); b.targetGraphic = image; b.onClick.AddListener(() => act());
                var icon = Rect(label + " binding", r); icon.anchorMin = icon.anchorMax = new(0, .5f); icon.pivot = new(0, .5f); icon.anchoredPosition = new(12, 0); icon.sizeDelta = new(54, 36);
                var g = icon.gameObject.AddComponent<MenuGlyph>(); g.raycastTarget = false;
                var key = Label("Key", icon, 16, 0); Stretch(key.rectTransform, 0, 0, 0, 0); key.alignment = TextAnchor.MiddleCenter;
                var text = Label("Label", r, 21, 0); Stretch(text.rectTransform, 75, 0, -10, 0); text.alignment = TextAnchor.MiddleCenter; text.text = label;
                return (g, key, text);
            }
            (watchGlyph, watchKey, _) = Button("WATCH REPLAY", .25f, WatchLastReplay);
            (saveGlyph, saveKey, saveLabel) = Button("SAVE REPLAY", .75f, SaveLastReplay);
            replayNote = Label("Replay note", replayRoot, 18, 0); replayNote.rectTransform.anchorMin = new(0, 1); replayNote.rectTransform.anchorMax = new(1, 1);
            replayNote.rectTransform.pivot = new(.5f, 0); replayNote.rectTransform.sizeDelta = new(0, 30); replayNote.rectTransform.anchoredPosition = new(0, 4); replayNote.alignment = TextAnchor.MiddleCenter;
            replayRoot.gameObject.SetActive(false);
            watchAction = new InputAction("Watch replay", InputActionType.Button, "<Gamepad>/buttonWest"); watchAction.AddBinding("<Keyboard>/v"); watchAction.Enable();
            saveAction = new InputAction("Save replay", InputActionType.Button, "<Gamepad>/buttonNorth"); saveAction.AddBinding("<Keyboard>/k"); saveAction.Enable();
        }

        bool ReplayShowing => race && !open && race.State == RaceController.Stage.Finished && !WinnerShot.Active && !TrailerMode.HudHidden && !ReplayViewer.Active
                              && ReplayRecorder.Instance && (ReplayRecorder.Instance.Recording || ReplayRecorder.Instance.Last != null);

        // in Update, with COMPLETE RACE: X watches, Y saves while the buttons show
        bool UpdateReplayButtons()
        {
            bool on = ReplayShowing;
            if (replayRoot && replayRoot.gameObject.activeSelf != on) replayRoot.gameObject.SetActive(on);
            if (!on) return false;
            bool pad = MenuInput.Controller;
            watchGlyph.SetPath(pad ? "<Gamepad>/buttonWest" : "<Keyboard>/v"); watchKey.text = MenuGlyph.Label(pad ? "<Gamepad>/buttonWest" : "<Keyboard>/v");
            saveGlyph.SetPath(pad ? "<Gamepad>/buttonNorth" : "<Keyboard>/k"); saveKey.text = MenuGlyph.Label(pad ? "<Gamepad>/buttonNorth" : "<Keyboard>/k");
            replayNote.text = ""; saveLabel.text = Time.unscaledTime < replayNoteUntil ? replayNoteText : "SAVE REPLAY";   // what saving did, on the button itself for a moment
            if (watchAction.WasPressedThisFrame()) { MenuInput.ConsumeThroughRelease(); WatchLastReplay(); return true; }
            if (saveAction.WasPressedThisFrame()) { MenuInput.ConsumeThroughRelease(); SaveLastReplay(); return true; }
            return false;
        }
        string replayNoteText = "";
        void ReplayNote(string s) { replayNoteText = s; replayNoteUntil = Time.unscaledTime + 3f; if (saveLabel) saveLabel.text = s; }

        // the race just run: its recording stops here if it still runs (the AI still racing are classified first)
        public void WatchLastReplay()
        {
            var rec = ReplayRecorder.Instance; if (!rec || ReplayViewer.Active) return;
            if (rec.Recording) { if (race && race.CanSimulateRemaining) race.CompleteRace(); rec.Stop("watched from the finish screen"); }
            if (rec.Last == null) { ReplayNote("NO REPLAY"); return; }
            bool wasOpen = open; if (open) CloseMenu();
            ReplayViewer.Open(rec.Last, () => { if (wasOpen) OpenMenu(false); });
            if (EventSystem.current) EventSystem.current.SetSelectedGameObject(null);
        }
        public void SaveLastReplay() { var rec = ReplayRecorder.Instance; if (!rec) return; ReplayNote(rec.Save()); if (open) Show(); }

        // ---------------------------------------------------------------- the REPLAYS page
        void OpenReplays() => Navigate("replays");
        string Staleness(ReplayData d) { var s = d.Staleness(race ? race.RouteHashOf(d.routeFile) : ""); return s == "" ? "" : "   (" + s + ")"; }
        void RenderReplays()
        {
            var best = ReplayData.List(true); var saved = ReplayData.List(false);
            Clear("REPLAYS", "Every race is recorded. The #1 lap and the #1 race of each course are kept here and replaced by a new #1.\nSAVE REPLAY on the finish screen keeps any race; saved ones stay until you delete them.");
            int n = 0;
            foreach (var (path, d) in best.OrderBy(x => x.head.Title).ThenBy(x => x.head.kind))
            {
                if (n >= 30) break;
                string what = d.kind == "lap" ? "#1 LAP " : "#1 RACE";
                Row(n++, "best" + n, $"{what}   {d.Title}{(d.kind == "lap" ? "" : $", {d.laps} lap{(d.laps == 1 ? "" : "s")}")}   {RaceController.Fmt(d.bestTime)}   {d.When}{Staleness(d)}", () => WatchFile(path));
            }
            if (best.Count == 0) { details.text += "\n\nNo #1 replays yet: set a #1 lap or race on the Top 10."; }
            foreach (var (path, d) in saved)
            {
                if (n >= 60) break;
                string p = path;
                Row(n++, "saved" + n, $"SAVED   {d.label}{Staleness(d)}", () => { replayPick = p; Navigate("replay-one"); });
            }
            Row(n++, "back", "Back", BackPage);
        }
        void RenderReplayOne()
        {
            if (string.IsNullOrEmpty(replayPick) || !File.Exists(replayPick)) { BackPage(); return; }
            ReplayData d = null; try { d = ReplayData.Read(replayPick, true); } catch (System.Exception) { }
            if (d == null) { Clear("REPLAY", "This replay cannot be read."); Row(0, "delete", "DELETE", DeletePick); Row(1, "back", "Back", BackPage); return; }
            long kb = new FileInfo(replayPick).Length / 1024;
            Clear("SAVED REPLAY", $"{d.Title}, {d.laps} lap{(d.laps == 1 ? "" : "s")}, recorded {d.When}   ({kb} KB){(Staleness(d) != "" ? "\n" + Staleness(d).Trim() : "")}");
            Row(0, "watch", "WATCH", () => WatchFile(replayPick));
            Row(1, "delete", "DELETE", () => Confirm("DELETE THIS REPLAY?", d.label, "DELETE", DeletePick));
            Row(2, "back", "Back", BackPage);
        }
        void DeletePick()
        {
            try { if (File.Exists(replayPick)) File.Delete(replayPick); } catch (System.Exception e) { Debug.LogWarning("[Replay] delete failed: " + e.Message); }
            replayPick = null; modal = null; BackPage();
        }
        void WatchFile(string path)
        {
            ReplayData d;
            try { d = ReplayData.Read(path); } catch (System.Exception e) { Debug.LogWarning("[Replay] " + e.Message); return; }
            string back = page; title = false; CloseMenu();
            ReplayViewer.Open(d, () => { OpenMenu(false); if (back == "replay-one" || back == "replays") Navigate("replays"); });
        }
    }
}
