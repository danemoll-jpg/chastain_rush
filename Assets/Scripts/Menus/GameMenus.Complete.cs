using Racer;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ChastainRush
{
    // Round 54 (C): Racer's COMPLETE RACE button (RaceMenus.cs "Simulate remaining racers", RaceMenus.Finish.cs). Shown once the player
    // has finished while AI drivers are still racing: its glyph is the confirm binding (A on a pad, Space on the keyboard), pressing it
    // or clicking it classifies them at their estimated finishes (RaceController.CompleteRace) and the results are complete. It sits
    // above the IMGUI results panel (IMGUI draws over every canvas). The pause menu has the same row while it applies.
    public sealed partial class GameMenus
    {
        RectTransform completeRoot; MenuGlyph completeGlyph; UnityEngine.UI.Text completeKey;

        void BuildCompleteButton(Transform canvas)
        {
            completeRoot = Rect("Complete race", canvas);
            completeRoot.anchorMin = completeRoot.anchorMax = new Vector2(.5f, .945f); completeRoot.sizeDelta = new Vector2(430, 54);
            var image = completeRoot.gameObject.AddComponent<UnityEngine.UI.Image>(); image.color = new Color(.08f, .30f, .34f, .98f);
            var button = completeRoot.gameObject.AddComponent<UnityEngine.UI.Button>(); button.targetGraphic = image;
            button.onClick.AddListener(() => { if (race && race.CanSimulateRemaining) race.CompleteRace(); });
            var icon = Rect("Complete binding", completeRoot); icon.anchorMin = icon.anchorMax = new(0, .5f); icon.pivot = new(0, .5f); icon.anchoredPosition = new(12, 0); icon.sizeDelta = new(54, 36);
            completeGlyph = icon.gameObject.AddComponent<MenuGlyph>(); completeGlyph.raycastTarget = false;
            completeKey = Label("Key", icon, 16, 0); Stretch(completeKey.rectTransform, 0, 0, 0, 0); completeKey.alignment = TextAnchor.MiddleCenter;
            var text = Label("Label", completeRoot, 21, 0); Stretch(text.rectTransform, 75, 0, -10, 0); text.alignment = TextAnchor.MiddleCenter; text.text = "COMPLETE RACE";
            completeRoot.gameObject.SetActive(false);
        }

        // checks only (Round20Runner.R54.cs): the title menu shut, as FREE ROAM does, so the race's own screens show
        public void CloseForCheck() { title = false; pages.Clear(); CloseMenu(); }
        public bool CompleteButtonShowing => completeRoot && completeRoot.gameObject.activeInHierarchy;

        bool CompleteShowing => race && !open && race.CanSimulateRemaining && !WinnerShot.Active && !TrailerMode.HudHidden;

        // in Update, before the menus take the keys: the confirm binding completes the race while the button shows
        bool UpdateComplete()
        {
            bool on = CompleteShowing;
            if (completeRoot && completeRoot.gameObject.activeSelf != on) completeRoot.gameObject.SetActive(on);
            if (!on) return false;
            string path = MenuInput.Controller ? "<Gamepad>/buttonSouth" : "<Keyboard>/space";
            completeGlyph.SetPath(path); completeKey.text = MenuGlyph.Label(path);
            if (!submit.WasPressedThisFrame()) return false;
            race.CompleteRace(); MenuInput.ConsumeThroughRelease();
            if (EventSystem.current) EventSystem.current.SetSelectedGameObject(null);
            return true;
        }
    }
}
