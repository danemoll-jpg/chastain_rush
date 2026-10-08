using UnityEngine;
using UnityEngine.InputSystem;

namespace Racer
{
    /// <summary>Device input only; physics remains independent of the binding scheme.</summary>
    public sealed class VehicleInput : MonoBehaviour
    {
        public float Throttle { get; private set; }
        public float BrakeReverse { get; private set; }
        public float Steering { get; private set; }
        bool resetRequested;
        InputAction throttle, brake, steering, reset, fist, view; bool viewRequested;
        public InputAction[] CurrentBindings => new[]{throttle,brake,steering,reset,fist};
        public bool UsingGamepad { get; private set; }
        public string ResetControlLabel
        {
            get
            {
                if(!UsingGamepad)return Device is Keyboard?"R":reset.GetBindingDisplayString(1);
                string label=Gamepad.current?.buttonNorth.displayName;
                return string.IsNullOrEmpty(label)||label=="Button North"?"Y":label;
            }
        }
        void Used(InputAction.CallbackContext context) { UsingGamepad=context.control.device is Gamepad; }

        void Awake() => Create(null);
        // 0.90 Part D: in split-screen each player's vehicle answers only its own device (null = every device, as before).
        public InputDevice Device { get; private set; }
        public void Bind(InputDevice device)
        {
            if (device == Device && throttle != null) return;
            bool on = isActiveAndEnabled; if (throttle != null) { OnDisable(); foreach (var a in CurrentBindings) a.Dispose(); view.Dispose(); }
            Create(device); if (on) OnEnable();
        }
        void Create(InputDevice device)
        {
            Device = device; bool pad = device == null || device is Gamepad, keys = device == null || device is Keyboard;
            string P(string path) => device is Gamepad ? path.Replace("<Gamepad>", device.path) : path;
            void Add(InputAction a, string path, string processor = null) { if (path.Contains("<Gamepad>") ? !pad : !keys) return; var b = a.AddBinding(P(path)); if (processor != null) b.WithProcessor(processor); }
            throttle = new InputAction("Throttle", InputActionType.Value);
            // 0.84: a resting trigger reads a few percent on some pads; below 0.1 it is released.
            Add(throttle, "<Gamepad>/rightTrigger", "axisDeadzone(min=0.1,max=1)"); Add(throttle, "<Keyboard>/w"); Add(throttle, "<Keyboard>/upArrow");
            brake = new InputAction("BrakeReverse", InputActionType.Value);
            Add(brake, "<Gamepad>/leftTrigger", "axisDeadzone(min=0.1,max=1)"); Add(brake, "<Keyboard>/s"); Add(brake, "<Keyboard>/downArrow");
            steering = new InputAction("Steering", InputActionType.Value);
            Add(steering, "<Gamepad>/leftStick/x", "axisDeadzone(min=0.12,max=0.95)");
            if (keys) { steering.AddCompositeBinding("1DAxis").With("Negative", "<Keyboard>/a").With("Positive", "<Keyboard>/d"); steering.AddCompositeBinding("1DAxis").With("Negative", "<Keyboard>/leftArrow").With("Positive", "<Keyboard>/rightArrow"); }
            reset = new InputAction("Reset", InputActionType.Button);
            Add(reset, "<Gamepad>/buttonNorth"); Add(reset, "<Keyboard>/r");
            // 0.78: shake a fist (cosmetic, RiderGestures). 0.80: on LB, since the right finger is on the throttle trigger.
            // In Trailer Mode LB keeps its 0.25x hold; F still works there.
            fist = new InputAction("Fist wave", InputActionType.Button);
            Add(fist, "<Gamepad>/leftShoulder"); Add(fist, "<Keyboard>/f");
            // 0.94 Part B: the camera view button, read per player in split-screen (CameraViews)
            view = new InputAction("Change view", InputActionType.Button); Add(view, "<Gamepad>/buttonWest"); Add(view, "<Keyboard>/v");
            if (device != null) UsingGamepad = device is Gamepad;
            throttle.performed+=Used; brake.performed+=Used; steering.performed+=Used; reset.performed+=Used; fist.performed+=Used;
        }

        void OnEnable() { throttle.Enable(); brake.Enable(); steering.Enable(); reset.Enable(); fist.Enable(); view.Enable(); }
        void Update()
        {
            Throttle = throttle.ReadValue<float>();
            BrakeReverse = brake.ReadValue<float>();
            Steering = steering.ReadValue<float>();
            resetRequested |= reset.WasPressedThisFrame(); viewRequested |= view.WasPressedThisFrame();
        }
        public bool ConsumeReset() { bool result = resetRequested; resetRequested = false; return result; }
        public bool ConsumeView() { bool result = viewRequested; viewRequested = false; return result; }
        void OnDisable()
        {
            throttle.Disable(); brake.Disable(); steering.Disable(); reset.Disable(); fist.Disable(); view.Disable(); viewRequested = false;
            Throttle = BrakeReverse = Steering = 0; resetRequested = false;
        }
        void OnDestroy() { throttle.Dispose(); brake.Dispose(); steering.Dispose(); reset.Dispose(); fist.Dispose(); view.Dispose(); }
    }
}
