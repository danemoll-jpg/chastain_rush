using UnityEngine;

namespace Racer
{
    // 0.77 Parts A and B: the Trailer Mode cameras (TrailerMode owns the mode and reads the controls). Chase (smoother, a
    // little lower and wider), Orbit, Side tracking, Front, Fixed, Flyover, Free camera, Auto (Part B: the game cuts between
    // the others) and First person. Every move is smoothed, and every camera except Free and Fixed is pulled in front of
    // terrain and walls the way the chase camera is; Free cannot fly through them. Choosing a camera by hand blends to it;
    // Auto cuts instantly.
    // Chastain Rush, Round 43 (Part D): copied from Racer for Trailer Mode (ChastainRush.TrailerMode owns the mode and reads the
    // controls); Car is the chase camera's vehicle; Rough is never true here (Round 28: the chosen view is always kept).
    public sealed partial class CameraViews
    {
        public enum Shot { Chase, Orbit, Side, Front, Fixed, Flyover, Free, Auto, FirstPerson }
        public static readonly string[] ShotNames = { "Chase", "Orbit", "Side tracking", "Front", "Fixed", "Flyover", "Free camera", "Auto", "First person" };
        public Shot TrailerCamera { get; private set; } = Shot.Chase;
        Shot? trailerShot;
        public Shot? OnScreen => trailerShot;
        public string ShownTrailer;   // Chastain Rush: the camera on screen, by name (Auto: "Auto: <shot>")
        ArcadeVehicle Car => car;
        public bool Rough => viewWeight < .99f;
        // Field of view (degrees added) and, per camera, distance (scale) and height (metres added): adjusted while filming.
        public float FovOffset { get; private set; }
        readonly float[] distance = { 1, 1, 1, 1, 1, 1, 1, 1, 1 }, height = new float[9];
        int sideSign = 1, shotsSinceFirstPerson = 9;
        float orbitAngle, flyPhase;
        Vector3 shotPosition, shotVelocity, plant; Quaternion shotRotation, heading; bool cut;
        // Free camera: TrailerMode passes the stick / keys each frame (local move, look in degrees, speed).
        public Vector3 FreeMove; public Vector2 FreeLook; public float FreeSpeed = 14;
        float freeYaw, freePitch; Vector3 freeVelocity;
        // Auto (Part B).
        public bool AutoHold;
        bool autoSkip, jumpShot; Shot? autoShot; float shotEnds, airTime, groundTime, blocked;
        public string AutoReason { get; private set; } = "";
        public int AutoCuts { get; private set; }

        public void TrailerStarted() { TrailerCamera = Shot.Chase; trailerShot = null; autoShot = null; FovOffset = 0; StartBlend(.5f); }
        public void TrailerEnded() { trailerShot = null; autoShot = null; StartBlend(.6f); }
        // Number keys / D-pad. Choosing the selected camera again is its action (Fixed re-plants, Side changes side).
        public void SelectTrailerCamera(Shot shot)
        {
            if (shot == TrailerCamera) { CameraAction(); return; }
            // Fixed by hand: the camera stays where it is (the action key then re-plants it ahead).
            if (shot == Shot.Fixed) plant = last.position;
            TrailerCamera = shot; autoShot = null; if (shot != Shot.Auto) StartBlend(.6f);
        }
        public void CameraAction()
        {
            if (TrailerCamera == Shot.Fixed) { PlantAhead(Car, out plant); StartBlend(.4f); }
            else if (TrailerCamera == Shot.Side) { sideSign = -sideSign; StartBlend(.6f); }
            else if (TrailerCamera == Shot.Auto) autoSkip = true;
        }
        public void Adjust(float fov, float far, float up)
        {
            FovOffset = Mathf.Clamp(FovOffset + fov, -40, 30);
            int i = (int)(trailerShot ?? TrailerCamera);
            distance[i] = Mathf.Clamp(distance[i] * (1 + far), .4f, 3f); height[i] = Mathf.Clamp(height[i] + up, -1.5f, 12);
        }

        Pose TrailerPose(Pose chasePose, ArcadeVehicle car, out string shown)
        {
            var shot = TrailerCamera == Shot.Auto ? Direct(car) : TrailerCamera;
            cut = trailerShot != shot;
            if (cut) Enter(shot, car);
            trailerShot = shot;
            float dt = Time.deltaTime;
            heading = cut ? Yaw(car) : Quaternion.Slerp(heading, Yaw(car), 1 - Mathf.Exp(-3 * dt));
            int i = (int)shot;
            chase.offsetScale = shot == Shot.Chase ? new Vector3(1, .8f * distance[i] + height[i] * .3f, distance[i]) : Vector3.one;
            chase.positionSmoothTime = shot == Shot.Chase ? .3f : baseSmooth; chase.headingResponse = shot == Shot.Chase ? 3.2f : baseHeading;
            float d = distance[i], h = height[i];
            Pose pose = shot switch
            {
                Shot.Chase => new Pose(chasePose.position, chasePose.rotation, baseFov + 6, baseNear),
                Shot.Orbit => Orbit(car, d, h, dt),
                Shot.Side => Follow(car, new Vector3(sideSign * 4.2f * d, .9f + h, .6f), .15f, baseFov),
                Shot.Front => Follow(car, new Vector3(0, 1.5f + h, 7.5f * d), .2f, baseFov),
                Shot.Fixed => Fixed(car),
                Shot.Flyover => Flyover(car, d, h, dt),
                Shot.Free => Free(),
                _ => Pose.Lerp(chasePose, FirstPerson(car, chasePose), Mathf.SmoothStep(0, 1, viewWeight)),
            };
            if (cut && TrailerCamera == Shot.Auto) blend = 1;// Auto cuts are instant
            pose.fov = Mathf.Clamp(pose.fov + FovOffset, 15, 110);
            shown = TrailerCamera == Shot.Auto ? "Auto: " + ShotNames[i] : ShotNames[i];
            return pose;
        }
        static Quaternion Yaw(ArcadeVehicle car) => Quaternion.Euler(0, car.transform.eulerAngles.y, 0);
        static Vector3 Focus(ArcadeVehicle car) => car.transform.position + Vector3.up * .8f;
        void Enter(Shot shot, ArcadeVehicle car)
        {
            if (shot == Shot.Chase) chase.Snap();
            if (shot == Shot.FirstPerson) { fpFresh = true; shotsSinceFirstPerson = 0; } else shotsSinceFirstPerson++;
            // Orbit by hand starts from where the camera is; Auto opens it at the angle it checked the view from.
            var away = Vector3.ProjectOnPlane(last.position - car.transform.position, Vector3.up);
            if (shot == Shot.Orbit) orbitAngle = TrailerCamera == Shot.Auto || away.sqrMagnitude < .01f ? car.transform.eulerAngles.y + 120 : Quaternion.LookRotation(-away).eulerAngles.y;
            if (shot == Shot.Free) { var e = last.rotation.eulerAngles; freeYaw = e.y; freePitch = Mathf.DeltaAngle(0, e.x); freeVelocity = Vector3.zero; shotPosition = last.position; shotRotation = last.rotation; }
            flyPhase = Random.Range(0f, 6.28f);
        }
        // Vehicle-relative cameras: offset in the vehicle's smoothed heading, kept in front of terrain and walls.
        Pose Follow(ArcadeVehicle car, Vector3 local, float smooth, float fov)
        {
            var focus = Focus(car);
            var desired = Clear(focus, car.transform.position + heading * local);
            return Track(focus, desired, smooth, fov);
        }
        Pose Track(Vector3 focus, Vector3 desired, float smooth, float fov)
        {
            float dt = Time.deltaTime;
            if (cut) { shotPosition = desired; shotVelocity = Vector3.zero; }
            else shotPosition = Clear(focus, Vector3.SmoothDamp(shotPosition, desired, ref shotVelocity, smooth, Mathf.Infinity, dt));
            var look = Look(focus - shotPosition);
            shotRotation = cut ? look : Quaternion.Slerp(shotRotation, look, 1 - Mathf.Exp(-10 * dt));
            return new Pose(shotPosition, shotRotation, fov, baseNear);
        }
        static Quaternion Look(Vector3 d) => d.sqrMagnitude > 1e-4f ? Quaternion.LookRotation(d, Vector3.up) : Quaternion.identity;
        Pose Orbit(ArcadeVehicle car, float d, float h, float dt)
        {
            orbitAngle += 14 * dt;
            var desired = Clear(Focus(car), car.transform.position + Quaternion.Euler(0, orbitAngle, 0) * new Vector3(0, 1.8f + h, -6.5f * d));
            return Track(Focus(car), desired, .12f, baseFov);
        }
        Pose Flyover(ArcadeVehicle car, float d, float h, float dt)
        {
            flyPhase += .12f * dt;
            var desired = car.transform.position + heading * new Vector3(Mathf.Sin(flyPhase) * 9 * d, 16 + h, (-6 + Mathf.Cos(flyPhase) * 5) * d);
            desired = Clear(Focus(car), desired);
            float dtv = Time.deltaTime;
            if (cut) { shotPosition = desired; shotVelocity = Vector3.zero; } else shotPosition = Vector3.SmoothDamp(shotPosition, desired, ref shotVelocity, .8f, Mathf.Infinity, dtv);
            var look = Look(Focus(car) + heading * Vector3.forward * 4 - shotPosition);
            shotRotation = cut ? look : Quaternion.Slerp(shotRotation, look, 1 - Mathf.Exp(-6 * dtv));
            return new Pose(shotPosition, shotRotation, baseFov - 5, baseNear);
        }
        Pose Fixed(ArcadeVehicle car)
        {
            var look = Look(Focus(car) - plant);
            shotRotation = cut ? look : Quaternion.Slerp(shotRotation, look, 1 - Mathf.Exp(-9 * Time.deltaTime));
            return new Pose(plant, shotRotation, baseFov - 8, baseNear);
        }
        // The detached free camera (the debug inspection controls) with eased movement and look; it keeps moving at normal
        // speed in slow motion and cannot pass through terrain, walls or buildings.
        Pose Free()
        {
            float dt = Time.unscaledDeltaTime;
            freeYaw += FreeLook.x; freePitch = Mathf.Clamp(freePitch - FreeLook.y, -89, 89);
            var target = Quaternion.Euler(freePitch, freeYaw, 0);
            shotRotation = Quaternion.Slerp(shotRotation, target, 1 - Mathf.Exp(-12 * dt));
            var wanted = (shotRotation * new Vector3(FreeMove.x, 0, FreeMove.z) + Vector3.up * FreeMove.y);
            freeVelocity = Vector3.Lerp(freeVelocity, Vector3.ClampMagnitude(wanted, 1) * FreeSpeed, 1 - Mathf.Exp(-6 * dt));
            var step = freeVelocity * dt; float m = step.magnitude;
            if (m > 1e-5f && Physics.SphereCast(shotPosition, .3f, step / m, out var hit, m, ~0, QueryTriggerInteraction.Ignore)) { step = step / m * Mathf.Max(0, hit.distance - .05f); freeVelocity = Vector3.zero; }
            shotPosition += step;
            return new Pose(shotPosition, shotRotation, baseFov, baseNear);
        }
        // Pulls a camera in front of whatever stands between it and the vehicle, and keeps it above the ground.
        Vector3 Clear(Vector3 from, Vector3 to)
        {
            var d = to - from; float m = d.magnitude;
            if (m > .01f && Physics.SphereCast(from, .3f, d / m, out var hit, m, chase.obstructionMask, QueryTriggerInteraction.Ignore)) to = from + d / m * Mathf.Max(.3f, hit.distance - .1f);
            if (Physics.Raycast(to + Vector3.up * 2, Vector3.down, out var ground, 2.4f, chase.obstructionMask, QueryTriggerInteraction.Ignore)) to.y = Mathf.Max(to.y, ground.point.y + .4f);
            return to;
        }

        // ---------- Part B: Auto ----------
        static readonly RaycastHit[] sight = new RaycastHit[24];
        // Nothing solid (terrain, trunks, buildings, walls) between this spot and the vehicle; vehicles do not count.
        static bool Visible(Vector3 from, Vector3 to)
        {
            var d = to - from; float m = d.magnitude; if (m < 1.5f) return true;
            int n = Physics.SphereCastNonAlloc(from, .2f, d / m, sight, m - 1.2f, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++) { var body = sight[i].collider.attachedRigidbody; if (body && !body.isKinematic) continue; return false; }
            return true;
        }
        // Where a Fixed camera should stand to watch the vehicle ride past: about 2.4 s ahead, 6-9 m to one side, at eye
        // height above the ground, with a clear view of the vehicle now and along the way.
        bool PlantAhead(ArcadeVehicle car, out Vector3 spot)
        {
            var v = car.Body.linearVelocity; var flat = Vector3.ProjectOnPlane(v, Vector3.up);
            var dir = flat.sqrMagnitude > 4 ? flat.normalized : car.transform.forward;
            float ahead = Mathf.Clamp(flat.magnitude * 2.4f, 10, 70);
            var future = car.transform.position + dir * ahead;
            var side = Vector3.Cross(Vector3.up, dir) * (Random.value < .5f ? -1 : 1) * Random.Range(6f, 9f);
            spot = future + side + Vector3.up * 3;
            if (Physics.Raycast(spot + Vector3.up * 12, Vector3.down, out var ground, 40, ~0, QueryTriggerInteraction.Ignore)) spot.y = ground.point.y + Random.Range(1.2f, 2.2f);
            var now = Focus(car); var then = future + Vector3.up * .8f;
            return Visible(spot, now) && Visible(spot, Vector3.Lerp(now, then, .5f)) && Visible(spot, then);
        }
        // The shot each candidate camera would open with (for the line-of-sight check).
        Vector3 Opening(Shot s, ArcadeVehicle car)
        {
            var p = car.transform.position; var hd = Yaw(car);
            return s switch
            {
                Shot.Orbit => p + Quaternion.Euler(0, car.transform.eulerAngles.y + 120, 0) * new Vector3(0, 1.8f, -6.5f),
                Shot.Side => p + hd * new Vector3(sideSign * 4.2f, .9f, .6f),
                Shot.Front => p + hd * new Vector3(0, 1.5f, 7.5f),
                Shot.Flyover => p + hd * new Vector3(0, 16, -6),
                Shot.Chase => chase.ChasePosition,
                _ => Focus(car)
            };
        }
        Shot Direct(ArcadeVehicle car)
        {
            float dt = Time.deltaTime, now = Time.time;
            bool air = car.GroundedWheels == 0;
            airTime = air ? airTime + dt : 0; groundTime = air ? 0 : groundTime + dt;
            bool skip = autoSkip; autoSkip = false;
            if (autoShot == null) { Choose(car); return autoShot.Value; }
            // Jumps: cut as the vehicle takes off (Flyover or a low Fixed view) and hold that shot until it has landed.
            if (!jumpShot && airTime > .2f && car.Body.linearVelocity.magnitude > 12 && car.Body.linearVelocity.y > -2 && !AutoHold) { jumpShot = true; ChooseJump(car); return autoShot.Value; }
            if (jumpShot) { if (groundTime > .6f) { jumpShot = false; shotEnds = Mathf.Max(shotEnds, now + 1.5f); } return autoShot.Value; }
            // Never mid-air, mid-wipeout or while the player holds the shot.
            if (AutoHold || air || Rough) return autoShot.Value;
            blocked = autoShot == Shot.FirstPerson || Visible(last.position, Focus(car)) ? 0 : blocked + dt;
            bool gone = autoShot == Shot.Fixed && Vector3.Distance(plant, car.transform.position) > 55 && Vector3.Dot(car.Body.linearVelocity, car.transform.position - plant) > 0;
            if (skip || now >= shotEnds || blocked > .6f || gone) Choose(car, skip ? "skip" : blocked > .6f ? "view blocked" : gone ? "rode past" : "time");
            return autoShot.Value;
        }
        void Choose(ArcadeVehicle car, string why = "start")
        {
            bool straight = Mathf.Abs(car.Body.angularVelocity.y) < .2f;
            var options = new System.Collections.Generic.List<(Shot shot, float weight)>();
            void Offer(Shot s, float w) { if (s != autoShot && w > 0) options.Add((s, w)); }
            Vector3 spot = default;
            if (autoShot != Shot.Fixed && PlantAhead(car, out spot)) Offer(Shot.Fixed, 3);
            var focus = Focus(car);
            foreach (var (s, w) in new[] { (Shot.Side, straight ? 2.2f : .8f), (Shot.Orbit, straight ? 1.8f : 1f), (Shot.Front, 1f), (Shot.Flyover, .8f), (Shot.Chase, 1f) })
                if (Visible(Opening(s, car), focus)) Offer(s, w);
            if (shotsSinceFirstPerson >= 5) Offer(Shot.FirstPerson, .5f);// sparingly
            var pick = Shot.Chase; float total = 0; foreach (var o in options) total += o.weight;
            float r = Random.value * total;
            foreach (var o in options) { pick = o.shot; r -= o.weight; if (r <= 0) break; }
            if (options.Count == 0) pick = autoShot == Shot.Chase ? Shot.Orbit : Shot.Chase;
            if (pick == Shot.Fixed) plant = spot;
            if (pick == Shot.Side) sideSign = Random.value < .5f ? -1 : 1;
            autoShot = pick; shotEnds = Time.time + Random.Range(4f, 8f); blocked = 0; AutoCuts++;
            AutoReason = why + " -> " + ShotNames[(int)pick];
        }
        void ChooseJump(ArcadeVehicle car)
        {
            var v = car.Body.linearVelocity; var flat = Vector3.ProjectOnPlane(v, Vector3.up); var dir = flat.sqrMagnitude > 1 ? flat.normalized : car.transform.forward;
            // a low view from beside the flight, about where the vehicle will be in 0.8 s
            var low = car.transform.position + dir * flat.magnitude * .8f + Vector3.Cross(Vector3.up, dir) * 9;
            if (Physics.Raycast(low + Vector3.up * 20, Vector3.down, out var ground, 60, ~0, QueryTriggerInteraction.Ignore)) low.y = ground.point.y + .8f;
            bool lowClear = Visible(low, Focus(car)) && Visible(low, car.transform.position + v * .8f);
            bool flyClear = Visible(Opening(Shot.Flyover, car), Focus(car));
            Shot pick = autoShot != Shot.Flyover && flyClear && (!lowClear || Random.value < .5f) ? Shot.Flyover : lowClear && autoShot != Shot.Fixed ? Shot.Fixed : flyClear ? Shot.Flyover : autoShot.Value;
            if (pick == Shot.Fixed) plant = low;
            autoShot = pick; AutoCuts++; AutoReason = "take-off -> " + ShotNames[(int)pick];
        }
    }
}
