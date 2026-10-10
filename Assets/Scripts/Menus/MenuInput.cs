using System;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace Racer
{
    // One process-wide deliberate-device decision and transition release barrier.
    // Chastain Rush (Round 16): copied from Racer; Racer's DeveloperLocationHud is this game's DeveloperHud (its debug menu,
    // a bug comment being typed or a capture own the input).
    [DefaultExecutionOrder(-1000)]
    public sealed class MenuInput : MonoBehaviour
    {
        static MenuInput instance;
        public static bool Controller { get; private set; }
        public static Gamepad Pad { get; private set; }
        public static bool Blocked => barrier || HudOwnsInput;
        // A debug dialog blocks the game, but still needs the shared EventSystem.
        public static bool UiBlocked => barrier || HudOwnsInput;
        static bool HudOwnsInput { get { var h = ChastainRush.DeveloperHud.Instance; return h != null && (h.MenuOpen || h.CommentOpen || h.Capturing); } }   // Chastain Rush
        static bool barrier;
        static int barrierFrame;
        static readonly System.Collections.Generic.List<ButtonControl> releaseButtons=new();
        Vector2 mouseTravel;
        float mouseWindow, analogAt;
        readonly System.Collections.Generic.Dictionary<int,bool> analogHeld=new();
        public static void Ensure()
        {
            if(instance)return;
            instance=new GameObject("Shared UI input owner").AddComponent<MenuInput>();
            DontDestroyOnLoad(instance.gameObject);
        }
        public static void ConsumeThroughRelease(InputAction action=null)
        {
            if(!barrier)releaseButtons.Clear();
            barrier=true;barrierFrame=Time.frameCount;
            // Only consumed UI controls own the release barrier. Held driving input
            // must not lock pause/map or freeze countdown after a lifecycle transition.
            if(action!=null)foreach(var button in action.controls.OfType<ButtonControl>())
                if(button.isPressed&&!releaseButtons.Contains(button))releaseButtons.Add(button);
        }
        public static string Binding(InputAction action)
        {
            if(action==null)return "";
            // Effective paths include overrides; never infer glyph from the action name.
            var binding=action.bindings.FirstOrDefault(b=>!b.isComposite &&
                (Controller?b.effectivePath.Contains("Gamepad"):b.effectivePath.Contains("Keyboard")));
            return binding.effectivePath??"";
        }
        void Update()
        {
            if(Pad!=null&&!Pad.added){Controller=false;Pad=null;}
            foreach(var device in InputSystem.devices)
            {
                if(!(device is Gamepad || device is Keyboard || device is Mouse))continue;
                if(device.allControls.OfType<ButtonControl>().Any(b=>!b.synthetic && !(b.parent is StickControl) && b.wasPressedThisFrame))
                {Controller=device is Gamepad; if(device is Gamepad pad)Pad=pad;}
                if(device is Gamepad g){
                    bool meaningful=g.leftStick.ReadValue().magnitude>.55f||g.rightStick.ReadValue().magnitude>.55f||g.leftTrigger.ReadValue()>.55f||g.rightTrigger.ReadValue()>.55f;
                    bool previous=analogHeld.TryGetValue(g.deviceId,out var held)&&held;
                    if(meaningful&&!previous&&Time.unscaledTime>analogAt){Controller=true;Pad=g;analogAt=Time.unscaledTime+.2f;}
                    analogHeld[g.deviceId]=meaningful;
                }
            }
            var mouse=Mouse.current;
            if(Time.unscaledTime-mouseWindow>.1f){mouseTravel=Vector2.zero;mouseWindow=Time.unscaledTime;}
            if(mouse!=null && !mouse.synthetic)
            {
                mouseTravel+=mouse.delta.ReadValue();
                if(mouseTravel.magnitude>=8||mouse.scroll.ReadValue().sqrMagnitude>1)Controller=false;
            }
            if(barrier&&Time.frameCount>barrierFrame+1)
            {
                releaseButtons.RemoveAll(button=>!button.device.added||!button.isPressed);
                if(releaseButtons.Count==0)barrier=false;
            }
        }
    }
}
