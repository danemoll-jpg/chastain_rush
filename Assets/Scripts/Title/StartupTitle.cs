using System;
using System.Collections;
using System.Linq;
using Racer;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UI;

namespace ChastainRush
{
    // Round 51 (Part A, Dan 2026-10-19: "Start up polish: here are the assets to make the Startup"): Racer's StartupTitle, ported.
    // At launch, once the loading screen has gone: Dan's cover over the whole screen (LoadingScreen.AddCover: cropped evenly, never
    // stretched), the title spoken once (Resources/Title/Voice, SourceArt/Title/TitleVoice_MonsterTruckGuy.mp3 decoded to WAV),
    // then the theme (Resources/Title/Theme, SourceArt/Title/ChastainRushTheme.mp3 decoded to WAV) looped whole: Racer splits its
    // theme into an opening and a loop, but this one does not split cleanly (its two choruses are alike, not the same: their
    // waveforms match at 0.4 to 0.5), so the whole 34.4 s track loops; it ends on its own fade and starts again. A "press to
    // continue" prompt (a key or click, or A on a controller) goes on to the title menu, which is already open under the cover
    // (GameMenus). The menus take no input until then. Never in a check run, except Round 51's (-exeRound51).
    // Process lifetime. Left behind: Racer's RaceFlow (GameMenus begins it), the fallback AudioListener (one scene here: its
    // camera's listener never goes away) and the validation load delay.
    public sealed class StartupTitle : MonoBehaviour
    {
        static bool completed;
        static StartupTitle instance;
        public static bool Active => instance && !instance.Advancing;
        // the menus wait until the press that left the title has been released
        public static bool Blocking => instance && !completed;
        public static bool SpeechPending => instance && !instance.speechComplete;
        // the radio stays silent while the title's voice or theme can be heard
        public static bool Quiet => instance && (!instance.speechComplete || (instance.Theme && instance.Theme.isPlaying));
        public static StartupTitle Instance => instance;
        bool speechComplete;
        bool voiceStarted;
        bool cancelledBeforeSpeech;
        // after the loading screen has gone: the cover is seen a moment before the voice, and the audio device has settled
        // (Racer padded its voice file with 1 s of silence for the same reason; Dan's file is used as it is)
        public const float ArtworkLeadIn = .8f;
        public bool Armed { get; private set; }
        public bool Advancing { get; private set; }
        public int VoiceStarts { get; private set; }
        public AudioSource Voice { get; private set; }
        public AudioSource Theme { get; private set; }
        GameObject canvas;
        BaseInputModule inputModule;
        bool moduleWasEnabled;
        Text advanceText; MenuGlyph advanceGlyph; Text advanceKey;
        int openedFrame;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetProcess() { completed = false; instance = null; }
        public static bool Begin()
        {
            if (completed || !LoadingScreen.Allowed || Application.isBatchMode) return false;
            if (instance) return !instance.Advancing;
            instance = new GameObject("Chastain Rush startup title").AddComponent<StartupTitle>();
            DontDestroyOnLoad(instance.gameObject); instance.Open(); return true;
        }
        void Open()
        {
            openedFrame = Time.frameCount;
            inputModule = EventSystem.current ? EventSystem.current.GetComponent<BaseInputModule>() : null;
            if (inputModule) { moduleWasEnabled = inputModule.enabled; inputModule.enabled = false; }
            canvas = new GameObject("Title cover", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            canvas.transform.SetParent(transform, false);
            var c = canvas.GetComponent<Canvas>(); c.renderMode = RenderMode.ScreenSpaceOverlay; c.sortingOrder = 32000;   // under the loading screen, over the menus
            var scaler = canvas.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new(1280, 720); scaler.matchWidthOrHeight = .5f;
            var matte = Rect("Matte", canvas.transform); Stretch(matte); matte.gameObject.AddComponent<Image>().color = new(.012f, .019f, .025f);
            LoadingScreen.AddCover(matte);
            // the prompt: low in the middle, over the road, on a dark plate
            var plate = Rect("Prompt plate", matte); plate.anchorMin = plate.anchorMax = plate.pivot = new(.5f, 0); plate.anchoredPosition = new(0, 34); plate.sizeDelta = new(380, 40);
            plate.gameObject.AddComponent<Image>().color = new(.01f, .016f, .022f, .72f);
            var prompt = Rect("Prompt", plate); Stretch(prompt);
            var text = prompt.gameObject.AddComponent<Text>(); text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); text.fontSize = 19; text.alignment = TextAnchor.MiddleCenter; text.color = new(.92f, .92f, .86f); text.text = "Press any key or click"; text.raycastTarget = false; advanceText = text;
            var glyphRect = Rect("Advance glyph", prompt); glyphRect.anchorMin = glyphRect.anchorMax = new(.5f, .5f); glyphRect.sizeDelta = new(30, 30); glyphRect.anchoredPosition = new(-118, 0);
            advanceGlyph = glyphRect.gameObject.AddComponent<MenuGlyph>(); advanceGlyph.raycastTarget = false;
            var key = Rect("Key", glyphRect); Stretch(key); advanceKey = key.gameObject.AddComponent<Text>(); advanceKey.font = text.font; advanceKey.fontSize = 18; advanceKey.alignment = TextAnchor.MiddleCenter; advanceKey.color = Color.white; advanceKey.raycastTarget = false;
            Voice = gameObject.AddComponent<AudioSource>(); Theme = gameObject.AddComponent<AudioSource>();
            foreach (var source in new[] { Voice, Theme }) { source.playOnAwake = false; source.spatialBlend = 0; source.ignoreListenerPause = true; source.priority = 32; }   // the title menu pauses the listener
            Voice.priority = Theme.priority = 0;   // Racer 0.84 (CR-118): never the voice that gets culled; Round 51: the theme too (at 32 the check heard nothing of it: the world's sounds took the real voices)
            Voice.volume = .85f; Theme.volume = .27f; Theme.loop = true;
            StartCoroutine(LoadAudio());
        }
        static RectTransform Rect(string name, Transform parent) { var r = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); r.SetParent(parent, false); return r; }
        static void Stretch(RectTransform r) { r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero; }
        IEnumerator LoadAudio()
        {
            var voice = Resources.LoadAsync<AudioClip>("Title/Voice"); var theme = Resources.LoadAsync<AudioClip>("Title/Theme");
            yield return voice; yield return theme;
            Voice.clip = voice.asset as AudioClip; Theme.clip = theme.asset as AudioClip;
            foreach (var source in new[] { Voice, Theme }) if (source.clip) source.clip.LoadAudioData();
            while (new[] { Voice, Theme }.Any(s => s.clip && s.clip.loadState == AudioDataLoadState.Loading)) yield return null;
            Debug.Log($"[Title] {Time.realtimeSinceStartup:0.0} s: sounds loaded: voice {Voice.clip?.loadState}, theme {Theme.clip?.loadState}");
            Canvas.ForceUpdateCanvases(); yield return null; yield return null;
            while (LoadingScreen.Holding) yield return null;   // Racer 0.82: the voice starts once the loading screen has gone
            // Chastain Rush: timed by the real clock and started with Play, not Racer's PlayScheduled on the audio clock: under the
            // title menu the listener is paused (AudioListener.pause), and the audio clock with it (Round 51's check: dspTime stood
            // still and the voice never began)
            float ready = Time.realtimeSinceStartup;
            while (!Advancing && Time.realtimeSinceStartup - ready < ArtworkLeadIn) yield return null;
            if (Advancing) { speechComplete = true; yield break; }
            // the complete decoded voice; never faded or cut
            if (Voice.clip)
            {
                Voice.timeSamples = 0; Voice.Play(); VoiceStarts++; voiceStarted = true;
                float end = Time.realtimeSinceStartup + (float)Voice.clip.samples / Voice.clip.frequency + .2f;
                while (!cancelledBeforeSpeech && Time.realtimeSinceStartup < end) yield return null;
            }
            speechComplete = true;
            if (Advancing) yield break;
            if (Theme.clip) { Theme.timeSamples = 0; Theme.Play(); yield return null; Debug.Log($"[Title] theme {(Theme.isPlaying ? "playing" : "NOT playing")} ({Theme.clip.length:0.0} s, looped)"); }
        }
        static System.Collections.Generic.IEnumerable<ButtonControl> Buttons() => InputSystem.devices.Where(d => d is Keyboard || d is Gamepad || d is Mouse).SelectMany(d => d.allControls.OfType<ButtonControl>()).Where(b => !b.synthetic && !(b.parent is StickControl));
        public static bool ButtonHeld() => Buttons().Any(b => b.isPressed);
        static bool ButtonPressed() => Buttons().Any(b => b.wasPressedThisFrame);
        // The window may not have focus (or lose it) while the voice and theme play; with Run In Background off an unfocused
        // player pauses its audio. While the title is up the player keeps running in the background; the setting comes back after.
        bool backgroundBefore, backgroundHeld; int focusLosses; bool reported;
        void OnApplicationFocus(bool focus) { if (!focus && !speechComplete) focusLosses++; }
        void LateUpdate()
        {
            bool pending = !speechComplete || (Voice && Voice.isPlaying);
            if (pending && !backgroundHeld) { backgroundHeld = true; backgroundBefore = Application.runInBackground; Application.runInBackground = true; }
            else if (!pending && backgroundHeld) { backgroundHeld = false; Application.runInBackground = backgroundBefore; }
            if (!pending && !reported && VoiceStarts > 0) { reported = true; Debug.Log($"Title voice: {(cancelledBeforeSpeech ? "cancelled before it began" : "played to the end")} ({Voice.clip.length:F2} s clip); window focus lost {focusLosses} times meanwhile; listener volume {AudioListener.volume:F2}"); }
        }
        void Update()
        {
            if (advanceText) { bool pad = MenuInput.Controller; advanceText.text = pad ? "Continue / any button" : "Press any key or click"; advanceGlyph.gameObject.SetActive(pad); advanceGlyph.SetPath("<Gamepad>/buttonSouth"); advanceKey.text = "A"; }
            if (Advancing) return;
            if (!Armed) { if (Time.frameCount > openedFrame + 2 && !LoadingScreen.Holding && !ButtonHeld()) Armed = true; return; }
            if (ButtonPressed())
            {
                Advancing = true; canvas.SetActive(false); Theme.Stop();
                if (!voiceStarted) { cancelledBeforeSpeech = true; Voice.Stop(); speechComplete = true; }
                Debug.Log("[Title] continue: " + (MenuInput.Controller ? "controller" : "keyboard / mouse"));
                StartCoroutine(EnterMenu());
            }
        }
        IEnumerator EnterMenu()
        {
            // wait through the release and another input update: the press that left the title cannot also act on the menu
            do { yield return null; } while (ButtonHeld());
            yield return null; yield return null;
            completed = true;
            if (inputModule) inputModule.enabled = moduleWasEnabled;
            MenuInput.ConsumeThroughRelease();
            while (!speechComplete) yield return null;   // a voice already speaking is heard to its end
            instance = null; Destroy(gameObject);
        }
        void OnDestroy() { if (backgroundHeld) Application.runInBackground = backgroundBefore; StopAllCoroutines(); if (Voice) Voice.Stop(); if (Theme) Theme.Stop(); if (instance == this) instance = null; }
    }
}
