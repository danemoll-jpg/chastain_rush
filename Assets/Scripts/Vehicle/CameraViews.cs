using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Racer
{
    // Chastain Rush, Round 24: ported from Racer's CameraViews (+ .FreeLook), trimmed. Four views for the player: Chase (default),
    // Far chase, First person, Front; X (controller) / V (keyboard) cycles them and the choice is remembered (PlayerPrefs; a check
    // run neither reads nor writes it). Look around: right stick (pulled straight back = look behind) or the right mouse button held
    // with the mouse moved; it eases back behind the vehicle when let go. ChaseCamera keeps tracking the chase pose underneath every
    // view (including its steady pipe view), so look-around works underground too. Left behind: RaceFlow, split-screen, Trailer Mode,
    // the winner shot, per-player views, R3 look-behind (R3 is bug capture here).
    [DefaultExecutionOrder(50)]
    public sealed class CameraViews : MonoBehaviour
    {
        public enum View { Chase, FarChase, FirstPerson, Front }
        public static readonly string[] Names = { "Chase", "Far chase", "First person", "Front" };
        struct Pose
        {
            public Vector3 position; public Quaternion rotation; public float fov, near;
            public Pose(Vector3 p, Quaternion r, float f, float n) { position = p; rotation = r; fov = f; near = n; }
            public static Pose Lerp(Pose a, Pose b, float t) => new(Vector3.Lerp(a.position, b.position, t), Quaternion.Slerp(a.rotation, b.rotation, t), Mathf.Lerp(a.fov, b.fov, t), Mathf.Lerp(a.near, b.near, t));
        }
        const string Key = "ChastainRush.CameraView";
        Camera cam; ChaseCamera chase; ArcadeVehicle car; InputAction cycle;
        public View Current { get; private set; }
        public string CurrentName => Names[(int)Current];
        float baseFov, baseNear, baseSmooth, baseHeading;
        bool touched; View lastView;
        float viewWeight = 1;
        Pose last, blendFrom; float blend = 1, blendLength = .35f;
        static bool Quiet => ChastainRush.CheckRun.Active;

        void Awake()
        {
            cam = GetComponent<Camera>(); chase = GetComponent<ChaseCamera>();
            baseFov = cam.fieldOfView; baseNear = cam.nearClipPlane; baseSmooth = chase.positionSmoothTime; baseHeading = chase.headingResponse;
            if (!Quiet) Current = (View)Mathf.Clamp(PlayerPrefs.GetInt(Key, 0), 0, Names.Length - 1);
            lastView = Current; last = new Pose(transform.position, transform.rotation, baseFov, baseNear);
            cycle = new InputAction("Change view", InputActionType.Button); cycle.AddBinding("<Keyboard>/v"); cycle.AddBinding("<Gamepad>/buttonWest"); cycle.Enable();
        }
        void OnDestroy() { cycle?.Dispose(); }
        public void SetView(View v)
        {
            if (v == Current) return; Current = v;
            if (!Quiet) { PlayerPrefs.SetInt(Key, (int)v); PlayerPrefs.Save(); }
        }
        public void NextView() => SetView((View)(((int)Current + 1) % Names.Length));
        static bool CanDrive => !MenuInput.Blocked && !ChastainRush.GameMenus.Open && !ChastainRush.WorldMap.OwnsInput;
        void Update() { if (CanDrive && cycle.WasPressedThisFrame()) NextView(); }
        void StartBlend(float length = .35f) { blendFrom = last; blend = 0; blendLength = length; }

        void LateUpdate()
        {
            if (!cam || !chase) return;
            if (!car && chase.target) car = chase.target.GetComponent<ArcadeVehicle>();
            if (!chase.enabled || !car) { Release(); return; }
            Settle(car);
            if (lastView != Current) { lastView = Current; StartBlend(); }
            var chasePose = new Pose(chase.ChasePosition, chase.ChaseRotation, baseFov, baseNear);
            chase.offsetScale = Current == View.FarChase ? new Vector3(1, 1.55f, 1.5f) : Vector3.one;
            chase.positionSmoothTime = baseSmooth; chase.headingResponse = baseHeading;
            bool own = Current == View.FirstPerson || Current == View.Front;
            var pose = own ? Pose.Lerp(chasePose, Current == View.FirstPerson ? FirstPerson(car, chasePose) : FrontView(car, chasePose), Mathf.SmoothStep(0, 1, viewWeight)) : chasePose;
            UpdateFreeLook(car); pose = DriveLook(pose, car, own && viewWeight > .5f);
            if (!own && blend >= 1 && Current == View.Chase && !Looking) { Hide(null, false); if (touched) Release(); last = chasePose; return; }
            if (blend < 1) { blend = Mathf.Min(1, blend + Time.unscaledDeltaTime / blendLength); pose = Pose.Lerp(blendFrom, pose, Mathf.SmoothStep(0, 1, blend)); }
            Hide(car, InsideHead(car, pose.position));
            transform.SetPositionAndRotation(pose.position, pose.rotation);
            cam.fieldOfView = pose.fov; cam.nearClipPlane = pose.near; touched = true; last = pose;
        }
        void Release()
        {
            Hide(null, false);
            if (!touched) return;
            touched = false; cam.fieldOfView = baseFov; cam.nearClipPlane = baseNear;
            chase.offsetScale = Vector3.one; chase.positionSmoothTime = baseSmooth; chase.headingResponse = baseHeading;
        }
        // Chastain Rush, Round 28 (B6, Dan: "I drive in first POV and it keeps switching to a follow cam back to first POV"): Racer eased
        // first person / front back to the chase view while the vehicle was tilted past 70 degrees or wiped out, and every wall ride in a
        // pipe tilts it that far. Standing rule: the view only changes when the player changes it, so the player's view is always kept
        // Chastain Rush, Round 28 (B6, Dan: "I drive in first POV and it keeps switching to a follow cam back to first POV"): Racer eased
        // first person / front to the chase view while the vehicle was tilted past 70 degrees or wiped out, and every wall ride in a pipe
        // tilts it that far. Standing rule: the view only changes when the player changes it, so the chosen view is always kept
        void Settle(ArcadeVehicle car) { viewWeight = 1; }

        // ---------- free look (Racer's CameraViews.FreeLook) ----------
        float lookYaw, lookPitch, lookYawSpeed, lookPitchSpeed, mouseYaw, mousePitch;
        bool Looking => Mathf.Abs(lookYaw) > .3f || Mathf.Abs(lookPitch) > .3f;
        void UpdateFreeLook(ArcadeVehicle car)
        {
            float yaw = 0, pitch = 0; bool held = false;
            var input = car ? car.GetComponent<VehicleInput>() : null;
            if (input && input.enabled && CanDrive)
            {
                var stick = input.Look; float amount = Mathf.Clamp01((stick.magnitude - .15f) / .7f);
                if (amount > 0) { yaw = Mathf.Atan2(stick.x, stick.y) * Mathf.Rad2Deg * amount; held = true; }
                if (input.MouseLook)
                {
                    mouseYaw = Mathf.Clamp(mouseYaw + input.MouseDelta.x * .2f, -180, 180); mousePitch = Mathf.Clamp(mousePitch - input.MouseDelta.y * .12f, -20, 30);
                    if (!held) { yaw = mouseYaw; pitch = mousePitch; held = true; }
                }
                else mouseYaw = mousePitch = 0;
            }
            else mouseYaw = mousePitch = 0;
            float smooth = held ? .1f : .22f;
            lookYaw = Mathf.SmoothDampAngle(lookYaw, yaw, ref lookYawSpeed, smooth, Mathf.Infinity, Time.unscaledDeltaTime);
            lookPitch = Mathf.SmoothDamp(lookPitch, pitch, ref lookPitchSpeed, smooth, Mathf.Infinity, Time.unscaledDeltaTime);
            if (!held && Mathf.Abs(Mathf.DeltaAngle(lookYaw, 0)) < .2f && Mathf.Abs(lookPitch) < .2f) { lookYaw = lookPitch = lookYawSpeed = lookPitchSpeed = 0; }
            lookYaw = Mathf.DeltaAngle(0, lookYaw);
        }
        Pose DriveLook(Pose pose, ArcadeVehicle car, bool head)
        {
            if (!Looking || !car) return pose;
            var turn = Quaternion.AngleAxis(lookYaw, Vector3.up);
            if (head) { pose.rotation = turn * pose.rotation * Quaternion.Euler(lookPitch, 0, 0); return pose; }
            var pivot = car.transform.position + Vector3.up;
            var offset = turn * (pose.position - pivot); float length = offset.magnitude;
            if (length > .01f && Physics.SphereCast(pivot, chase.collisionRadius, offset / length, out var hit, length, chase.obstructionMask, QueryTriggerInteraction.Ignore))
                offset = offset / length * Mathf.Max(0, hit.distance - .1f);
            pose.position = pivot + offset; pose.rotation = turn * pose.rotation * Quaternion.Euler(lookPitch, 0, 0);
            return pose;
        }

        // ---------- first person ----------
        Transform visual; Transform eyeHost; Vector3 eyeLocal; bool classicEyes;
        readonly List<(Transform part, Vector3 centre)> eyeParts = new();
        Quaternion fpRotation; float fpY; bool fpFresh = true; Transform fpFor;
        // The rider's eyes: the Blender rider's "__eyes" part; the classic models' head (merged into their body meshes) is
        // left behind the camera, which sits just in front of the face.
        bool Eyes(ArcadeVehicle car, out Vector3 eye, out Vector3 forward)
        {
            var root = car.transform.Find("Vehicle visual");
            if (root != visual)
            {
                visual = root; eyeParts.Clear(); eyeHost = null; classicEyes = false; hidden.Clear(); fpFresh = true;
                if (root)
                {
                    foreach (var r in root.GetComponentsInChildren<Renderer>(true))
                        // 0.78 fix: only the face's eyes (the Shorts socks share the "eyes" material slot).
                        if (r.name.EndsWith("__eyes") && r.name.Contains("_Base_") && r.GetComponent<MeshFilter>() is MeshFilter f && f.sharedMesh) eyeParts.Add((r.transform, f.sharedMesh.bounds.center));
                    if (eyeParts.Count == 0)
                    {
                        var p = car.GetComponent<VehicleConfiguration>().Profile; classicEyes = true; eyeHost = root;
                        // VehicleVisual.Person: head at hip + (0, .735, .18) * scale; the camera 8 cm ahead of the face.
                        eyeLocal = p.Small ? new Vector3(0, 1.26f, .10f) : new Vector3(-.37f, .72f, .43f);
                    }
                }
            }
            eye = forward = default;
            forward = car.transform.forward;
            if (classicEyes) { if (!eyeHost) return false; eye = eyeHost.TransformPoint(eyeLocal); return true; }
            if (eyeParts.Count == 0 || !eyeParts[0].part) return false;
            foreach (var (part, centre) in eyeParts) eye += part.TransformPoint(centre);
            // Blender rider (Tools/Blender/rider.py): eyes 9 cm ahead of the head centre, the nose tip 12 cm. The camera sits
            // 4.5 cm in front of the eyes, just clear of the face, so the head (one mesh with the hands) is behind it.
            eye = eye / eyeParts.Count + forward * .045f;
            return true;
        }
        Pose FirstPerson(ArcadeVehicle car, Pose fallback)
        {
            if (!Eyes(car, out var eye, out _)) return fallback;
            // Leans a little with the motorcycle (40 %), a small look-ahead into corners; bumps and landings are softened.
            var config = car.GetComponent<VehicleConfiguration>();
            float lean = visual && config.Profile.Motorcycle ? Mathf.DeltaAngle(0, visual.localEulerAngles.z) * .4f : 0;
            float look = Mathf.Clamp(car.Body.angularVelocity.y * Mathf.Rad2Deg * .22f, -10, 10);
            // Looking a little down: the bars, grips, hands and front wheel at the bottom of the view (cars: the wheel and dash).
            // The Trail Four's grips sit lower and nearer under the rider's eyes than the Needle 600's.
            bool atv = config.profileId == "atv", small = config.Profile.Small;
            return Mounted(car, eye, Quaternion.Euler(atv ? 24 : small ? 16 : 5, look, lean), baseFov + (atv ? 15 : small ? 10 : 7));
        }
        // Front: clear of the bodywork - bumper level on the cars, just ahead of the bars on the motorcycle and ATV.
        Pose FrontView(ArcadeVehicle car, Pose fallback)
        {
            var p = car.GetComponent<VehicleConfiguration>().Profile;
            var local = p.Small ? new Vector3(0, p.Motorcycle ? (p.Model == "Scrambler" || p.Id == "stuntbike" ? 1.2f : .98f) : .92f, p.Size.z * .5f - .1f) : new Vector3(0, .3f, p.Size.z * .5f + .06f);
            float look = Mathf.Clamp(car.Body.angularVelocity.y * Mathf.Rad2Deg * .12f, -6, 6);
            return Mounted(car, car.transform.TransformPoint(local), Quaternion.Euler(0, look, 0), baseFov + 5);
        }
        Pose Mounted(ArcadeVehicle car, Vector3 at, Quaternion extra, float fov)
        {
            var target = car.transform.rotation * extra;
            if (fpFresh || fpFor != car.transform) { fpRotation = target; fpY = at.y; fpFresh = false; fpFor = car.transform; }
            float dt = Time.deltaTime;
            fpRotation = Quaternion.Slerp(fpRotation, target, 1 - Mathf.Exp(-12 * dt));
            fpY = Mathf.Lerp(fpY, at.y, 1 - Mathf.Exp(-30 * dt));
            at.y += Mathf.Clamp(fpY - at.y, -.08f, .08f);
            return new Pose(at, fpRotation, fov, .05f);
        }
        // The camera is inside the rider's head (hair and hat included): within 17 cm of the head centre, which is 13.5 cm
        // behind the first-person eye point (Eyes: the camera sits 4.5 cm ahead of the eyes, the eyes 9 cm ahead of the centre).
        bool InsideHead(ArcadeVehicle car, Vector3 camera)
        {
            if (!car || !Eyes(car, out var eye, out var forward) || classicEyes) return false;
            return (camera - (eye - forward * .135f)).sqrMagnitude < .17f * .17f;
        }
        // 0.83 Part F: an outside camera that takes over from this one (the winner shot) shows the whole rider.
        public void ShowHead() => Hide(null, false);
        // The player's own face details, hair, brows and hat cast their shadow but are not drawn in first person.
        readonly List<(Renderer renderer, UnityEngine.Rendering.ShadowCastingMode mode)> hidden = new();
        void Hide(ArcadeVehicle car, bool value)
        {
            if (!value)
            {
                foreach (var (r, mode) in hidden) if (r) r.shadowCastingMode = mode;
                hidden.Clear(); return;
            }
            if (hidden.Count > 0 && hidden[0].renderer) return;
            hidden.Clear();
            if (!Eyes(car, out _, out _) || classicEyes) return;
            foreach (var r in visual.GetComponentsInChildren<Renderer>(true))
            {
                var n = r.name; int cut = n.IndexOf("__"); if (cut < 0) continue;
                string slot = n.Substring(cut + 2);
                bool head = n.Contains("_Hair_") || n.Contains("_HairTop_") || n.Contains("_Hat_") || (n.Contains("_Base_") && (slot == "eyes" || slot == "pupil" || slot == "mouth" || slot == "hair"));
                if (!head) continue;
                hidden.Add((r, r.shadowCastingMode)); r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
            }
        }
    }
}
