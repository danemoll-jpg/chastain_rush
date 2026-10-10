using System;
using System.Collections.Generic;
using System.Linq;
using Racer;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace ChastainRush
{
    // Round 16: the garage, from Racer's RaceMenus.GaragePreview, .GarageStats and the garage part of RaceMenus.Show. The
    // vehicle turns in a fixed panel on the left of a wider card (a camera far away on layer 31 into a render texture the
    // panel's real pixel size, so it is sharp at 3840 x 2160), three-quarter view, the right stick / Q and E / a mouse drag
    // turn it, left alone it turns slowly. On the right: the vehicle (left / right through the eleven), its colour, the five
    // stat bars scaled across all eleven (Racer's StatFraction without the campaign upgrades), Done. Choosing a vehicle
    // swaps it where it stands and saves it (VehicleSwitch.Select, as the Round 13 switch did); the colour is saved with it.
    // Left behind: locked vehicles and silhouettes, the shop, the rider page, the Classic / New model toggle.
    public sealed partial class GameMenus
    {
        UnityEngine.UI.RawImage preview; Camera previewCamera; RenderTexture previewTexture; GameObject previewRoot;
        RectTransform garagePanel; Vector2 menuCardSize; bool garageView;
        float previewYaw = 215, lastTurn = -100, previewRadius = 1; Vector3 previewCenter;
        InputAction rotateAction;
        static readonly Vector3 PreviewFocus = new(10000, 10000, 10000);
        const float PreviewPitch = 11, IdleTurnDelay = 3, IdleTurnSpeed = 12, TurnSpeed = 140, PreviewMargin = 1.04f;

        void BuildGarage()
        {
            menuCardSize = card.sizeDelta;
            garagePanel = Rect("Garage preview panel", card);
            garagePanel.gameObject.AddComponent<UnityEngine.UI.Image>().color = new Color(.06f, .1f, .13f);
            var pr = Rect("Vehicle preview", garagePanel); Stretch(pr, 0, 0, 0, 0);
            preview = pr.gameObject.AddComponent<UnityEngine.UI.RawImage>(); preview.raycastTarget = true;
            pr.gameObject.AddComponent<GaragePreviewDrag>().owner = this;
            previewTexture = new RenderTexture(640, 480, 16) { antiAliasing = 4 }; preview.texture = previewTexture;
            previewCamera = new GameObject("Garage preview camera").AddComponent<Camera>(); previewCamera.transform.SetParent(transform, false);
            previewCamera.cullingMask = 1 << 31; previewCamera.clearFlags = CameraClearFlags.SolidColor;
            previewCamera.backgroundColor = new Color(.06f, .1f, .13f); previewCamera.targetTexture = previewTexture; previewCamera.fieldOfView = 36; previewCamera.enabled = false;
            rotateAction = new InputAction("Rotate preview", InputActionType.Value);
            rotateAction.AddBinding("<Gamepad>/rightStick/x");
            rotateAction.AddCompositeBinding("1DAxis").With("Negative", "<Keyboard>/q").With("Positive", "<Keyboard>/e");
            garagePanel.gameObject.SetActive(false);
        }

        void EnterGarageView()
        {
            var parent = (RectTransform)card.parent;
            float width = Mathf.Min(1220, parent.rect.width - 40), height = Mathf.Min(menuCardSize.y, parent.rect.height - 24);
            card.sizeDelta = new Vector2(width, height);
            float panel = Mathf.Clamp(width * .5f, 340, 600);
            garagePanel.gameObject.SetActive(true);
            garagePanel.anchorMin = new Vector2(0, 0); garagePanel.anchorMax = new Vector2(0, 1); garagePanel.pivot = new Vector2(0, .5f);
            garagePanel.offsetMin = new Vector2(32, 78); garagePanel.offsetMax = new Vector2(32 + panel, -82);
            ((RectTransform)scroll.transform).offsetMin = new Vector2(32 + panel + 24, 78);
            if (!garageView) { garageView = true; rotateAction.Enable(); }
            previewCamera.enabled = true;
            MatchPreviewTexture();
        }
        void LeaveGarageView()
        {
            if (!garageView) return;
            garageView = false; rotateAction.Disable(); previewCamera.enabled = false;
            card.sizeDelta = menuCardSize; ((RectTransform)scroll.transform).offsetMin = new Vector2(32, 78);
            garagePanel.gameObject.SetActive(false);
            if (statBlock) statBlock.gameObject.SetActive(false);
            if (previewRoot) { Destroy(previewRoot); previewRoot = null; }
        }

        void RenderGarage()
        {
            var config = car.GetComponent<VehicleConfiguration>(); var profile = config.Profile;
            var all = VehicleProfile.All; int at = Mathf.Max(0, Array.FindIndex(all, v => v.Id == profile.Id));
            Clear(profile.Name + " / " + profile.Class, profile.Description);
            heading.text = profile.Name + " / " + profile.Class;
            bool racing = race && race.State != RaceController.Stage.Free;
            Step(0, "vehicle", $"{profile.Name}   ·   {at + 1} / {all.Length}", d =>
            {
                if (racing) return;   // the vehicle is chosen before a race, not during one
                vehicles.Select(all[(at + d + all.Length) % all.Length].Id); Show();
            });
            int colour = vehicles.Colour;
            Step(1, "colour", "Colour:   " + (colour < 0 ? "Standard" : VehiclePaint.Name(colour)), d =>
            {
                int next = colour < 0 ? (d > 0 ? 0 : VehiclePaint.Count - 1) : colour + d;
                vehicles.SetColour(next < 0 || next >= VehiclePaint.Count ? -1 : next); Show();
            });
            Row(2, "done", "Done", BackPage);
            if (racing) { buttons[0].interactable = false; details.text = profile.Description + "\nThe vehicle can be changed outside a race."; }
            // the stat bars sit under the colour row (display only, no selection)
            ShowGarageStats(profile);
            statBlock.SetSiblingIndex(buttons[1].transform.GetSiblingIndex() + 1);
            EnterGarageView();
            if (previewRoot) Destroy(previewRoot);
            previewRoot = new GameObject("Garage display model"); previewRoot.transform.SetParent(transform, false); previewRoot.transform.position = PreviewFocus;
            config.BuildPreview(previewRoot.transform);
            foreach (var t in previewRoot.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = 31;
            FramePreview();
        }

        // ---------------------------------------------------------------- the preview (Racer's GaragePreview)

        void MatchPreviewTexture()
        {
            Canvas.ForceUpdateCanvases();
            var canvas = preview.canvas ? preview.canvas.rootCanvas : null; float scale = canvas ? canvas.scaleFactor : 1;
            var size = preview.rectTransform.rect.size * scale;
            int w = Mathf.Clamp(Mathf.RoundToInt(size.x), 64, 2400), h = Mathf.Clamp(Mathf.RoundToInt(size.y), 64, 2400);
            if (previewTexture.width == w && previewTexture.height == h) return;
            previewTexture.Release(); previewTexture.width = w; previewTexture.height = h; previewTexture.antiAliasing = 4; previewTexture.Create();
            previewCamera.targetTexture = null; previewCamera.targetTexture = previewTexture; preview.texture = previewTexture;
            if (previewRoot) PlacePreview();
        }
        void FramePreview()
        {
            if (!previewRoot) return;
            var root = previewRoot.transform; root.SetPositionAndRotation(PreviewFocus, Quaternion.identity);
            var shown = previewRoot.GetComponentsInChildren<Renderer>().Where(r => r.enabled && !(r is ParticleSystemRenderer)).ToArray();
            if (shown.Length == 0) return;
            var b = shown[0].bounds; foreach (var r in shown) b.Encapsulate(r.bounds);
            previewCenter = root.InverseTransformPoint(b.center); previewRadius = Mathf.Max(.3f, b.extents.magnitude);
            PlacePreview();
        }
        void PlacePreview()
        {
            if (!previewRoot) return;
            var turn = Quaternion.Euler(0, previewYaw, 0); var root = previewRoot.transform;
            root.SetPositionAndRotation(PreviewFocus - turn * previewCenter, turn);
            float aspect = Mathf.Max(.2f, (float)previewTexture.width / previewTexture.height), half = previewCamera.fieldOfView * .5f * Mathf.Deg2Rad;
            float fit = Mathf.Min(half, Mathf.Atan(Mathf.Tan(half) * aspect)), distance = previewRadius * PreviewMargin / Mathf.Sin(fit);
            var look = Quaternion.Euler(PreviewPitch, 0, 0); previewCamera.transform.SetPositionAndRotation(PreviewFocus - look * Vector3.forward * distance, look);
            previewCamera.aspect = aspect; previewCamera.nearClipPlane = .05f; previewCamera.farClipPlane = distance + previewRadius * 2 + 5;
        }
        void UpdateGaragePreview()
        {
            if (!garageView || !previewRoot) return;
            MatchPreviewTexture();
            float turn = rotateAction.ReadValue<float>();
            if (Mathf.Abs(turn) > .15f) { previewYaw += turn * TurnSpeed * Time.unscaledDeltaTime; lastTurn = Time.unscaledTime; }
            else if (Time.unscaledTime - lastTurn > IdleTurnDelay) previewYaw += IdleTurnSpeed * Time.unscaledDeltaTime;
            previewYaw = Mathf.Repeat(previewYaw, 360); PlacePreview();
        }
        public void DragPreview(float dx) { previewYaw -= dx * .45f; lastTurn = Time.unscaledTime; PlacePreview(); }

        // ---------------------------------------------------------------- the stat bars (Racer's GarageStats)

        static readonly (string label, Func<VehicleProfile, float> value)[] GarageStats =
        {
            ("Top speed", p => p.Speed), ("Acceleration", p => p.Acceleration), ("Grip", p => p.Grip),
            ("Handling", p => p.Response), ("Weight / contact", p => p.Mass),
        };
        RectTransform statBlock; readonly List<RectTransform> statFills = new();
        // Racer's StatFraction: across all the vehicles, the lowest shows a short bar and the highest a full one
        public static float StatFraction(int stat, VehicleProfile p)
        {
            var values = VehicleProfile.All.Select(GarageStats[stat].value).ToArray(); float min = values.Min(), max = values.Max();
            return max > min ? Mathf.Lerp(.12f, 1, (GarageStats[stat].value(p) - min) / (max - min)) : 1;
        }
        void ShowGarageStats(VehicleProfile profile)
        {
            if (!statBlock)
            {
                statBlock = Rect("Vehicle stats", content); statBlock.gameObject.AddComponent<UnityEngine.UI.LayoutElement>().preferredHeight = GarageStats.Length * 24 + 8;
                var layout = statBlock.gameObject.AddComponent<UnityEngine.UI.VerticalLayoutGroup>(); layout.spacing = 2; layout.padding = new RectOffset(10, 10, 4, 4);
                layout.childControlWidth = layout.childControlHeight = true; layout.childForceExpandHeight = false;
                foreach (var (label, _) in GarageStats)
                {
                    var row = Rect(label, statBlock); row.gameObject.AddComponent<UnityEngine.UI.LayoutElement>().preferredHeight = 22;
                    var text = Label("Label", row, 17, 0); text.rectTransform.anchorMin = new Vector2(0, 0); text.rectTransform.anchorMax = new Vector2(.36f, 1); text.rectTransform.offsetMin = text.rectTransform.offsetMax = Vector2.zero;
                    text.alignment = TextAnchor.MiddleLeft; text.text = label; text.color = new Color(.86f, .86f, .8f);
                    var track = Rect("Bar", row); track.anchorMin = new Vector2(.38f, .25f); track.anchorMax = new Vector2(1, .75f); track.offsetMin = track.offsetMax = Vector2.zero;
                    track.gameObject.AddComponent<UnityEngine.UI.Image>().color = new Color(1, 1, 1, .12f);
                    var fill = Rect("Fill", track); fill.anchorMin = Vector2.zero; fill.anchorMax = new Vector2(0, 1); fill.offsetMin = fill.offsetMax = Vector2.zero;
                    fill.gameObject.AddComponent<UnityEngine.UI.Image>().color = new Color(.3f, 1, .88f); statFills.Add(fill);
                }
            }
            statBlock.gameObject.SetActive(true);
            for (int i = 0; i < statFills.Count; i++) statFills[i].anchorMax = new Vector2(StatFraction(i, profile), 1);
        }
    }

    public sealed class GaragePreviewDrag : MonoBehaviour, IDragHandler
    {
        public GameMenus owner;
        public void OnDrag(PointerEventData e) { if (owner) owner.DragPreview(e.delta.x); }
    }
}
